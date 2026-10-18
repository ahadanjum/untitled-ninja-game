using System;
using System.Collections.Generic;
using System.IO;
using Kaname.Sim;
using NUnit.Framework;
using UnityEngine;

namespace Kaname.Tests
{
    // The proof of the port: traces/*.json were recorded from the JavaScript game (dev/trace.mjs, tag js-prototype-rung7): for every
    // 60 Hz step, what the controls said and the state that came out. This replays the recorded controls through the C# simulation and requires
    // the same state, to within 0.001 u, at every step. If the traces match, the feel matches.
    [Serializable] public class TraceSetup { public double cx, y, vx, vy, camX; public int face; public bool onGround; }

    [Serializable]
    public class Trace
    {
        public int format;
        public string name, rung, note, generator;
        public double step;
        public string[] actions, keys;
        public TraceSetup setup;
        public int frames;
        public int[] held, hit;
        public double[] moveX, rows;
    }

    public class GoldenTraceTests
    {
        // the rungs the C# simulation has reached: add each one here when its port is done
        static readonly HashSet<string> Ported = new HashSet<string> { "U0" };
        const double Tolerance = 0.001;

        static string TraceDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "traces"));

        public static IEnumerable<string> TraceFiles()
        {
            foreach (var f in Directory.GetFiles(TraceDir, "*.json"))
                if (Ported.Contains(JsonUtility.FromJson<Trace>(File.ReadAllText(f)).rung)) yield return Path.GetFileNameWithoutExtension(f);
        }

        [Test]
        public void TheTracesExist()
        {
            Assert.IsTrue(Directory.Exists(TraceDir), "traces/ is missing: run `node dev/trace.mjs` in the repository root");
            Assert.IsNotEmpty(new List<string>(TraceFiles()), "no traces for the ported rungs");
        }

        [Test]
        public void TheActionOrderMatchesTheRecordings()
        {
            var t = Load("u0-stand-camera");
            var names = Enum.GetNames(typeof(Act));
            Assert.AreEqual(names.Length, t.actions.Length);
            for (int i = 0; i < names.Length; i++) Assert.AreEqual(t.actions[i], names[i].ToLowerInvariant(), "action bit " + i);
            Assert.AreEqual(Mathx.Step, t.step, 1e-15);
        }

        [TestCaseSource(nameof(TraceFiles))]
        public void ReplayMatchesTheJavaScriptGame(string name)
        {
            var t = Load(name);
            int n = t.keys.Length;
            Assert.AreEqual(t.frames * n, t.rows.Length, "rows");

            var world = new World(new Tuning());
            var h = world.Hero; var s = t.setup;
            h.Cx = h.Px = s.cx; h.Y = h.Py = s.y; h.Vx = s.vx; h.Vy = s.vy; h.Face = s.face; h.OnGround = s.onGround;
            world.Cam = new Spring { X = s.camX, V = 0 }; world.CamPrev = s.camX;

            var controls = new ControlState();
            double worst = 0; string worstAt = "";
            for (int i = 0; i < t.frames; i++)
            {
                controls.Load(t.held[i], t.hit[i], t.moveX[i]);
                world.Step(Mathx.Step, controls);
                controls.EndStep();
                var got = new[] { h.Cx, h.Y, h.Vx, h.Vy, h.OnGround ? 1 : 0, h.HitX, h.LandVy, world.Cam.X, world.Cam.V, h.Face };
                for (int k = 0; k < n; k++)
                {
                    double err = Math.Abs(got[k] - t.rows[i * n + k]);
                    if (err > worst) { worst = err; worstAt = $"step {i + 1}, {t.keys[k]}: got {got[k]}, JavaScript had {t.rows[i * n + k]}"; }
                }
            }
            Assert.LessOrEqual(worst, Tolerance, $"{name}: the C# simulation drifts from the JavaScript game, worst at {worstAt}");
        }

        static Trace Load(string name) => JsonUtility.FromJson<Trace>(File.ReadAllText(Path.Combine(TraceDir, name + ".json")));
    }
}
