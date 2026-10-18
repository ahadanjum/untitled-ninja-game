#!/usr/bin/env node
// dev/trace.mjs — records the golden traces for the Unity port. No dependencies (Node 22+ and Google Chrome).
//
//   node dev/trace.mjs               record every scenario in dev/trace-scenarios.js into traces/<name>.json
//   node dev/trace.mjs --grep drop   only scenarios whose name contains "drop"
//   node dev/trace.mjs --list        list the scenarios
//   node dev/trace.mjs --check       record again in memory and fail if any file in traces/ would change (the JS game is the spec: it must not drift)
//
// Each scenario runs in the real JS game on a fresh page (index.html?manual, seeded Math.random, no gamepad), one fixed 60 Hz
// step at a time. For every step it records what the controls said (held and pressed, as bit masks of ACTIONS, and the horizontal
// push) and the state that came out. The C# simulation replays the controls and must reproduce the state to within 0.001 u.
// (dev/run.mjs has its own copy of the browser plumbing below: fold the two together when a third tool needs it.)
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import vm from 'node:vm';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const OUT = path.join(ROOT, 'traces');
const args = process.argv.slice(2);
const flag = f => args.includes(f);
const opt = f => { const i = args.indexOf(f); return i >= 0 ? args[i + 1] : null; };
const GREP = opt('--grep'), SEED = 20261017;
if (flag('--grep') && (!GREP || GREP.startsWith('--'))) { console.error('--grep needs a value, e.g. --grep drop'); process.exit(2); }

// ---- the scenarios: run the file in a sandbox just to read the list (bodies are not executed) --------------------------
const scenariosSrc = fs.readFileSync(path.join(ROOT, 'dev/trace-scenarios.js'), 'utf8');
const sandbox = { window: {} }; vm.createContext(sandbox); vm.runInContext(scenariosSrc, sandbox);
let scenarios = sandbox.window.__scenarios;
if (flag('--list')) { for (const s of scenarios) console.log(`${s.rung}  ${s.name}  (${s.frames} steps)  ${s.note}`); process.exit(0); }
if (GREP) scenarios = scenarios.filter(s => s.name.toLowerCase().includes(GREP.toLowerCase()));
if (!scenarios.length) { console.error('no scenarios matched'); process.exit(2); }

// ---- tiny static file server, Chrome and a minimal DevTools-protocol client ---------------------------------------------
const MIME = { '.html': 'text/html', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css', '.json': 'application/json' };
const server = http.createServer((req, res) => {
  let p; try { p = path.join(ROOT, decodeURIComponent(new URL(req.url, 'http://x').pathname)); } catch { res.writeHead(400); return res.end(); }
  if (!p.startsWith(ROOT + path.sep) || !fs.existsSync(p) || fs.statSync(p).isDirectory()) { res.writeHead(404); return res.end(); }
  res.writeHead(200, { 'Content-Type': MIME[path.extname(p)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
  fs.createReadStream(p).pipe(res);
});
await new Promise(r => server.listen(0, '127.0.0.1', r));
const ORIGIN = `http://127.0.0.1:${server.address().port}`;

const chromePath = [process.env.CHROME_PATH, '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome', '/Applications/Chromium.app/Contents/MacOS/Chromium',
  '/usr/bin/google-chrome', '/usr/bin/google-chrome-stable', '/usr/bin/chromium', '/usr/bin/chromium-browser'].find(p => p && fs.existsSync(p));
if (!chromePath) { console.error('Google Chrome not found. Set CHROME_PATH.'); process.exit(2); }
const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'kaname-trace-'));
let chrome;
function cleanup() { try { chrome && chrome.kill(); } catch {} try { server.close(); } catch {} try { fs.rmSync(profile, { recursive: true, force: true }); } catch {} }
process.on('exit', cleanup); process.on('SIGINT', () => process.exit(130)); process.on('SIGTERM', () => process.exit(143));
chrome = spawn(chromePath, ['--headless=new', '--remote-debugging-port=0', `--user-data-dir=${profile}`, '--no-first-run', '--no-default-browser-check', '--mute-audio',
  '--window-size=1280,800', '--disable-background-timer-throttling', '--disable-renderer-backgrounding', 'about:blank'], { stdio: ['ignore', 'ignore', 'pipe'] });
const wsUrl = await new Promise((resolve, reject) => {
  let buf = ''; const t = setTimeout(() => reject(new Error('Chrome did not start')), 15000);
  chrome.stderr.on('data', d => { buf += d; const m = buf.match(/DevTools listening on (ws:\/\/\S+)/); if (m) { clearTimeout(t); resolve(m[1]); } });
  chrome.on('exit', c => reject(new Error('Chrome exited early (' + c + ')')));
});
class CDP {
  constructor(url) {
    this.ws = new WebSocket(url); this.id = 0; this.pending = new Map();
    this.ready = new Promise((r, j) => { this.ws.onopen = r; this.ws.onerror = () => j(new Error('could not connect to Chrome')); });
    this.ws.onmessage = e => { const m = JSON.parse(e.data); const p = m.id && this.pending.get(m.id); if (p) { this.pending.delete(m.id); m.error ? p.rej(new Error(m.error.message)) : p.res(m.result); } };
  }
  send(method, params = {}, sessionId, ms = 60000) {
    const id = ++this.id;
    return new Promise((res, rej) => {
      const t = setTimeout(() => { this.pending.delete(id); rej(new Error(`Chrome did not answer ${method} within ${ms / 1000}s`)); }, ms);
      this.pending.set(id, { res: v => { clearTimeout(t); res(v); }, rej: e => { clearTimeout(t); rej(e); } });
      this.ws.send(JSON.stringify({ id, method, params, sessionId }));
    });
  }
}
const cdp = new CDP(wsUrl); await cdp.ready;
const sleep = ms => new Promise(r => setTimeout(r, ms));

// ---- what runs inside the page ------------------------------------------------------------------------------------------
const preamble = `(() => {
  navigator.getGamepads = () => [];
  let s = ${SEED} >>> 0;
  Math.random = () => { s = (s + 0x6D2B79F5) >>> 0; let t = s; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  window.__holdRender = true;
  window.__ready = () => typeof G !== 'undefined' && !!G.hero && typeof T !== 'undefined';
})();\n`;

// ACTIONS is the order of the bits in "held" and "hit": the C# side (Act in Kaname.Sim) must list them in the same order.
const recorder = `(name) => {
  const ACTIONS = ['left', 'right', 'up', 'down', 'jump', 'dash', 'attack', 'parry'];
  const KEYS = ['cx', 'y', 'vx', 'vy', 'ground', 'hitX', 'landVy', 'camX', 'camV', 'face', 'coyote', 'jumpBuf', 'jumping', 'apex', 'lastJump', 'justLanded', 'dashT', 'dashCd', 'canDash', 'dashBuf', 'dashDir', 'lastDash'];
  const sc = window.__scenarios.find(s => s.name === name);
  dispatchEvent(new Event('blur')); window.__fakePad = null; resetHero(); Input.endStep();      // a clean start, no steps taken
  const P = G.hero, su = sc.setup || {};
  if ('cx' in su) P.cx = P.px = su.cx;
  if ('y' in su) P.y = P.py = su.y;
  P.vx = su.vx || 0; P.vy = su.vy || 0; P.face = su.face || 1;
  P.onGround = P.y === Stage.floorBelow(P.cx, P.y - 1); P.coyote = P.onGround ? K.COYOTE : 0;
  G.cam.x = G.camPrev = 'camX' in su ? su.camX : 0; G.cam.v = 0;
  const setup = { cx: P.cx, y: P.y, vx: P.vx, vy: P.vy, face: P.face, onGround: P.onGround, coyote: P.coyote, camX: G.cam.x };
  const held = [], hit = [], moveX = [], rows = [];
  for (let i = 0; i < sc.frames; i++) {
    if (sc.script) sc.script(i);
    Input.poll();
    let h = 0, e = 0; ACTIONS.forEach((a, b) => { if (Input.held(a)) h |= 1 << b; if (Input.hit(a)) e |= 1 << b; });
    held.push(h); hit.push(e); moveX.push(Input.moveX());
    step(STEP); Input.endStep();
    rows.push(P.cx, P.y, P.vx, P.vy, P.onGround ? 1 : 0, P.hitX, P.landVy, G.cam.x, G.cam.v, P.face, P.coyote, P.jumpBuf, P.jumping ? 1 : 0, P.apex, P.lastJump, P.justLanded ? 1 : 0, P.dashT, P.dashCd, P.canDash ? 1 : 0, P.dashBuf, P.dashDir, P.lastDash);
  }
  return { format: 1, name: sc.name, rung: sc.rung, note: sc.note, generator: 'dev/trace.mjs', step: STEP, actions: ACTIONS, setup, keys: KEYS, frames: sc.frames, held, hit, moveX, rows };
}`;

// one number per field, one step per line, so a changed trace is a readable diff
function format(t) {
  const { rows, held, hit, moveX, ...head } = t, n = t.keys.length;
  const lines = []; for (let i = 0; i < t.frames; i++) lines.push('    ' + rows.slice(i * n, (i + 1) * n).join(', '));
  const flat = a => '[' + a.join(', ') + ']';
  return JSON.stringify(head, null, 2).replace(/\n}$/, `,\n  "held": ${flat(held)},\n  "hit": ${flat(hit)},\n  "moveX": ${flat(moveX)},\n  "rows": [\n${lines.join(',\n')}\n  ]\n}`) + '\n';
}

async function record(sc) {
  const { targetId } = await cdp.send('Target.createTarget', { url: 'about:blank' });
  const { sessionId } = await cdp.send('Target.attachToTarget', { targetId, flatten: true });
  const S = (m, p, ms) => cdp.send(m, p, sessionId, ms);
  try {
    await S('Page.enable'); await S('Runtime.enable');
    await S('Page.addScriptToEvaluateOnNewDocument', { source: preamble + scenariosSrc });
    await S('Page.navigate', { url: `${ORIGIN}/index.html?manual` });
    let ready = false;
    for (let i = 0; i < 100 && !ready; i++) { ready = (await S('Runtime.evaluate', { expression: 'window.__ready && window.__ready()', returnByValue: true })).result.value; if (!ready) await sleep(50); }
    if (!ready) throw new Error('game did not boot');
    const r = await S('Runtime.evaluate', { expression: `(${recorder})(${JSON.stringify(sc.name)})`, returnByValue: true });
    if (r.exceptionDetails) throw new Error((r.exceptionDetails.exception && r.exceptionDetails.exception.description) || r.exceptionDetails.text);
    return r.result.value;
  } finally { try { await cdp.send('Target.closeTarget', { targetId }); } catch {} }
}

// ---- run ---------------------------------------------------------------------------------------------------------------
fs.mkdirSync(OUT, { recursive: true });
let changed = 0;
for (const sc of scenarios) {
  const text = format(await record(sc)), file = path.join(OUT, sc.name + '.json');
  if (flag('--check')) {
    const same = fs.existsSync(file) && fs.readFileSync(file, 'utf8') === text;
    if (!same) changed++;
    console.log(`${same ? '  ✓' : '  ✗'} ${sc.name}${same ? '' : '  (differs from traces/' + sc.name + '.json)'}`);
  } else { fs.writeFileSync(file, text); console.log(`  recorded ${path.relative(process.cwd(), file)}  (${sc.frames} steps)`); }
}
process.exit(changed ? 1 : 0);
