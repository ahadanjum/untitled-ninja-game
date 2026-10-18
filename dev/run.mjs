#!/usr/bin/env node
// dev/run.mjs — acceptance-test + evidence runner for the feel ladder.  No dependencies (Node 22+ and Google Chrome).
// Ported from Little Shinobi's runner.
//
//   node dev/run.mjs                 the tests (a few seconds, most of it screenshots)
//   node dev/run.mjs --seed 7        a different random seed (default 20261017): the suite should pass for any seed
//   node dev/run.mjs --grep jump     only tests whose name contains "jump"
//   node dev/run.mjs --list          list tests and exit
//   node dev/run.mjs --no-evidence   skip screenshots
//   node dev/run.mjs --headed        watch it run in a visible Chrome window
//   CHROME_PATH=/path/to/chrome node dev/run.mjs
//
// Every test gets a brand-new page (index.html?manual) with a seeded Math.random, no real gamepad and rendering held, so the
// simulation is deterministic and the order of tests does not matter. Tests live in dev/tests.js; they drive the real game
// through dev/harness.js and call `await snap('label')` where a picture is worth keeping.
// Output: test-results/report.json, test-results/index.html (a contact sheet) and test-results/evidence/*.png.
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import vm from 'node:vm';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const OUT = path.join(ROOT, 'test-results'), EVID = path.join(OUT, 'evidence');
const args = process.argv.slice(2);
const flag = f => args.includes(f);
const opt = f => { const i = args.indexOf(f); return i >= 0 ? args[i + 1] : null; };
const EVIDENCE = !flag('--no-evidence'), GREP = opt('--grep'), SEED = +(opt('--seed') ?? 20261017);
if (flag('--grep') && (!GREP || GREP.startsWith('--'))) { console.error('--grep needs a value, e.g. --grep jump'); process.exit(2); }

// ---- test metadata: run dev/tests.js in a sandbox just to read the list (bodies are not executed) --------------------
const testsSrc = fs.readFileSync(path.join(ROOT, 'dev/tests.js'), 'utf8');
const sandbox = { window: {} }; vm.createContext(sandbox); vm.runInContext(testsSrc, sandbox);
let tests = sandbox.window.__tests;
if (GREP) tests = tests.filter(t => t.name.toLowerCase().includes(GREP.toLowerCase()));
if (flag('--list')) { for (const t of sandbox.window.__tests) console.log(t.name); process.exit(0); }
if (!tests.length) { console.error('no tests matched'); process.exit(2); }

// ---- tiny static file server ------------------------------------------------------------------------------------------
const MIME = { '.html': 'text/html', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.png': 'image/png', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => {
  let p; try { p = path.join(ROOT, decodeURIComponent(new URL(req.url, 'http://x').pathname)); } catch { res.writeHead(400); return res.end(); }
  if (!p.startsWith(ROOT + path.sep) || !fs.existsSync(p) || fs.statSync(p).isDirectory()) { res.writeHead(404); return res.end(); }
  res.writeHead(200, { 'Content-Type': MIME[path.extname(p)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
  fs.createReadStream(p).pipe(res);
});
await new Promise(r => server.listen(0, '127.0.0.1', r));
const ORIGIN = `http://127.0.0.1:${server.address().port}`;

// ---- Chrome + a minimal DevTools-protocol client ------------------------------------------------------------------------
function findChrome() {
  const c = [process.env.CHROME_PATH, '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome', '/Applications/Chromium.app/Contents/MacOS/Chromium',
    '/usr/bin/google-chrome', '/usr/bin/google-chrome-stable', '/usr/bin/chromium', '/usr/bin/chromium-browser'];
  return c.find(p => p && fs.existsSync(p));
}
if (process.env.CHROME_PATH && !fs.existsSync(process.env.CHROME_PATH)) { console.error(`CHROME_PATH does not exist: ${process.env.CHROME_PATH}`); process.exit(2); }
const chromePath = findChrome();
if (!chromePath) { console.error('Google Chrome not found. Set CHROME_PATH.'); process.exit(2); }
const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'kaname-chrome-'));
let chrome;
function cleanup() { try { chrome && chrome.kill(); } catch {} try { server.close(); } catch {} try { fs.rmSync(profile, { recursive: true, force: true }); } catch {} }
process.on('exit', cleanup); process.on('SIGINT', () => process.exit(130)); process.on('SIGTERM', () => process.exit(143));
chrome = spawn(chromePath, [
  ...(flag('--headed') ? [] : ['--headless=new']), '--remote-debugging-port=0', `--user-data-dir=${profile}`,
  '--no-first-run', '--no-default-browser-check', '--mute-audio', '--hide-scrollbars', '--window-size=1280,800',
  '--disable-background-timer-throttling', '--disable-renderer-backgrounding', '--disable-backgrounding-occluded-windows', 'about:blank',
], { stdio: ['ignore', 'ignore', 'pipe'] });
const wsUrl = await new Promise((resolve, reject) => {
  let buf = ''; const t = setTimeout(() => reject(new Error('Chrome did not start')), 15000);
  chrome.stderr.on('data', d => { buf += d; const m = buf.match(/DevTools listening on (ws:\/\/\S+)/); if (m) { clearTimeout(t); resolve(m[1]); } });
  chrome.on('exit', c => reject(new Error('Chrome exited early (' + c + ')')));
});

class CDP {
  constructor(url) {
    this.ws = new WebSocket(url); this.id = 0; this.pending = new Map(); this.handlers = new Set();
    this.ready = new Promise((r, j) => { this.ws.onopen = r; this.ws.onerror = () => j(new Error('could not connect to Chrome')); });
    this.ws.onmessage = e => this.onMessage(JSON.parse(e.data));
    this.ws.onclose = () => { for (const p of this.pending.values()) p.rej(new Error('Chrome closed the connection')); this.pending.clear(); };
  }
  onMessage(m) {
    if (m.id) { const p = this.pending.get(m.id); if (p) { this.pending.delete(m.id); m.error ? p.rej(new Error(m.error.message)) : p.res(m.result); } }
    else for (const h of this.handlers) h(m);
  }
  send(method, params = {}, sessionId, ms = 30000) {
    const id = ++this.id;
    return new Promise((res, rej) => {
      const t = setTimeout(() => { this.pending.delete(id); rej(new Error(`Chrome did not answer ${method} within ${ms / 1000}s`)); }, ms);
      this.pending.set(id, { res: v => { clearTimeout(t); res(v); }, rej: e => { clearTimeout(t); rej(e); } });
      this.ws.send(JSON.stringify({ id, method, params, sessionId }));
    });
  }
  on(h) { this.handlers.add(h); return () => this.handlers.delete(h); }
}
const cdp = new CDP(wsUrl); await cdp.ready;

// ---- what gets injected into every page before the game's own scripts --------------------------------------------------
const preamble = seed => `(() => {
  navigator.getGamepads = () => [];                             // a real controller must never leak input into a test (tests inject their own)
  let s = ${seed} >>> 0;
  Math.random = () => { s = (s + 0x6D2B79F5) >>> 0; let t = s; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  window.__rng = { get: () => s, set: v => { s = v >>> 0; } };
  window.__holdRender = true;                                   // the test decides when a frame is drawn
  window.snap = label => new Promise(res => {                   // evidence: draw the current state and have the runner photograph it
    if (!window.__snapBinding) return res();
    const st = window.__rng.get(); try { render(); } finally { window.__rng.set(st); }
    window.__snapRes = res; window.__snapBinding(String(label));
  });
  window.__ready = () => typeof G !== 'undefined' && !!G.hero && typeof T !== 'undefined';
})();\n`;

const safe = s => s.replace(/[^a-z0-9]+/gi, '-').replace(/^-|-$/g, '').toLowerCase();
const sleep = ms => new Promise(r => setTimeout(r, ms));

async function runOne(t) {
  const started = Date.now(), pics = [], errors = [];
  const { targetId } = await cdp.send('Target.createTarget', { url: 'about:blank' });
  const { sessionId } = await cdp.send('Target.attachToTarget', { targetId, flatten: true });
  const S = (m, p, ms) => cdp.send(m, p, sessionId, ms);
  const off = cdp.on(async m => {
    if (m.sessionId !== sessionId) return;
    if (m.method === 'Runtime.exceptionThrown') { const d = m.params.exceptionDetails; errors.push((d.exception && d.exception.description) || d.text); }
    else if (m.method === 'Runtime.consoleAPICalled' && m.params.type === 'error') errors.push('console.error: ' + m.params.args.map(a => a.value ?? a.description).join(' '));
    else if (m.method === 'Runtime.bindingCalled' && m.params.name === '__snapBinding') {
      try {
        if (EVIDENCE) {
          const { data } = await S('Page.captureScreenshot', { format: 'png' });
          const file = `${String(t.n).padStart(2, '0')}-${safe(t.name).slice(0, 40)}__${safe(m.params.payload)}.png`; fs.writeFileSync(path.join(EVID, file), Buffer.from(data, 'base64')); pics.push({ label: m.params.payload, file });
        }
      } catch (e) { errors.push('evidence capture failed: ' + (e.message || e)); }
      finally { try { await S('Runtime.evaluate', { expression: 'window.__snapRes && window.__snapRes()' }); } catch {} }
    }
  });
  let result;
  try {
    await S('Page.enable'); await S('Runtime.enable');
    await S('Emulation.setDeviceMetricsOverride', { width: 1280, height: 720, deviceScaleFactor: 1, mobile: false });
    await S('Storage.clearDataForOrigin', { origin: ORIGIN, storageTypes: 'local_storage,indexeddb,cache_storage' });
    await S('Runtime.addBinding', { name: '__snapBinding' });
    await S('Page.addScriptToEvaluateOnNewDocument', { source: preamble(SEED) + testsSrc });
    await S('Page.navigate', { url: `${ORIGIN}/index.html${t.params}` });
    let ready = false;
    for (let i = 0; i < 100 && !ready; i++) { ready = (await S('Runtime.evaluate', { expression: 'window.__ready && window.__ready()', returnByValue: true })).result.value; if (!ready) await sleep(50); }
    if (!ready) throw new Error('game did not boot (see page errors)');
    const limit = t.timeout || 30000;
    const run = S('Runtime.evaluate', { expression: `window.__runTest(${JSON.stringify(t.name)})`, awaitPromise: true, returnByValue: true }, limit + 10000);
    const r = await Promise.race([run, sleep(limit).then(() => { throw new Error(`timed out after ${limit / 1000}s`); })]);
    result = r.exceptionDetails ? { ok: false, message: (r.exceptionDetails.exception && r.exceptionDetails.exception.description) || r.exceptionDetails.text } : r.result.value;
  } catch (e) { result = { ok: false, message: String(e.message || e) }; }
  off();
  if (errors.length && result.ok) result = { ...result, ok: false, message: 'uncaught page error:\n' + errors.slice(0, 3).join('\n') };
  else if (errors.length) result.message += '\n(page errors: ' + errors.slice(0, 2).join(' | ') + ')';
  try { await cdp.send('Target.closeTarget', { targetId }); } catch {}
  return { name: t.name, ms: Date.now() - started, pics, ...result };
}

// ---- run ---------------------------------------------------------------------------------------------------------------
fs.rmSync(OUT, { recursive: true, force: true }); fs.mkdirSync(EVID, { recursive: true });
console.log(`Kaname — feel ladder acceptance tests  (${tests.length} test${tests.length === 1 ? '' : 's'})\n`);
const results = [];
for (const [i, t] of tests.entries()) {
  const r = await runOne({ ...t, n: i + 1 }); results.push(r);
  console.log(`${r.ok ? '  ✓' : '  ✗'} ${r.name}  (${(r.ms / 1000).toFixed(1)}s)`);
  if (r.notes && Object.keys(r.notes).length) console.log('      ' + Object.entries(r.notes).map(([k, v]) => `${k}=${typeof v === 'number' ? +v.toFixed(3) : JSON.stringify(v)}`).join('  '));
  if (!r.ok) console.log(r.message.split('\n').map(l => '      ' + l).join('\n'));
}
const failed = results.filter(r => !r.ok);
console.log(`\n${results.length - failed.length}/${results.length} passed${EVIDENCE ? `   evidence: ${path.relative(process.cwd(), path.join(OUT, 'index.html'))}` : ''}`);

fs.writeFileSync(path.join(OUT, 'report.json'), JSON.stringify({ when: new Date().toISOString(), passed: results.length - failed.length, failed: failed.length, results }, null, 2));
const esc = s => String(s).replace(/[&<>]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c]));
fs.writeFileSync(path.join(OUT, 'index.html'), `<!doctype html><meta charset=utf-8><title>Kaname — test evidence</title>
<style>body{font:14px/1.4 system-ui;background:#0d1117;color:#d6dde6;margin:24px}h1{font-size:18px}.t{margin:18px 0}.ok{color:#5fd38d}.bad{color:#ff6b6b}
pre{background:#161b22;padding:8px;border-radius:6px;white-space:pre-wrap}.g{display:flex;flex-wrap:wrap;gap:10px}.g figure{margin:0;width:320px}.g img{width:320px;border-radius:4px;display:block}figcaption{font-size:12px;opacity:.7}</style>
<h1>Kaname — ${results.length - failed.length}/${results.length} passed · ${new Date().toLocaleString()}</h1>
${results.map(r => `<div class=t><b class=${r.ok ? 'ok' : 'bad'}>${r.ok ? '✓' : '✗'}</b> ${esc(r.name)} <small>(${(r.ms / 1000).toFixed(1)}s)</small>
${r.ok ? '' : `<pre>${esc(r.message)}</pre>`}${r.notes && Object.keys(r.notes).length ? `<small>${esc(Object.entries(r.notes).map(([k, v]) => k + '=' + (typeof v === 'number' ? +v.toFixed(3) : JSON.stringify(v))).join('  '))}</small>` : ''}
<div class=g>${r.pics.map(p => `<figure><img src="evidence/${p.file}" loading=lazy><figcaption>${esc(p.label)}</figcaption></figure>`).join('')}</div></div>`).join('\n')}`);
process.exit(failed.length ? 1 : 0);
