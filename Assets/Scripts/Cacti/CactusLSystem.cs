using System;
using System.Collections.Generic;

namespace Realms.Cacti
{
    /// <summary>
    /// One L-system module: a symbol, its parameters, and the derivation step it was born in.
    /// The birth step lets one derivation hold the whole life of the plant: interpreting at a
    /// continuous time T skips modules born after T and scales younger ones by a growth function.
    /// </summary>
    public sealed class Module
    {
        public readonly char S;
        public readonly float[] P;
        public int B = -1;

        public Module(char s, params float[] p) { S = s; P = p ?? Array.Empty<float>(); }
        public float this[int i] => i < P.Length ? P[i] : 0f;
        public override string ToString() => P.Length == 0 ? S.ToString() : S + "(" + string.Join(",", P) + ")";
    }

    /// <summary>mulberry32: identical sequence to the browser previewer, so a seed grows the same plant in both.</summary>
    public sealed class Rng
    {
        uint s;
        public Rng(int seed) { s = unchecked((uint)seed); }

        public double Next()
        {
            unchecked
            {
                s += 0x6D2B79F5u;
                uint t = s;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }

    /// <summary>Context handed to production rules: current step, random source and a scratch dictionary.</summary>
    public sealed class LContext
    {
        public int K;
        public readonly Rng Rand;
        public readonly Dictionary<string, int> State = new Dictionary<string, int>();
        public LContext(Rng r) { Rand = r; }

        /// <summary>Uniform in [0,1).</summary>
        public double R() => Rand.Next();
        /// <summary>Uniform in [a,b).</summary>
        public float Rnd(float a = 0f, float b = 1f) => (float)(a + (b - a) * Rand.Next());
    }

    /// <summary>A production: return the replacement modules, or null to keep the module unchanged.</summary>
    public delegate List<Module> Rule(Module m, LContext c);

    public static class LSystem
    {
        /// <summary>Derive the species' string for the given number of steps (default: the species' life span).</summary>
        public static List<Module> Derive(CactusSpecies spec, int seed, int steps = -1)
        {
            var ctx = new LContext(new Rng(seed));
            var str = spec.Axiom(ctx);
            foreach (var m in str) if (m.B < 0) m.B = 0;
            if (steps < 0) steps = spec.Steps;
            for (int k = 1; k <= steps; k++)
            {
                ctx.K = k;
                var next = new List<Module>(str.Count * 2);
                foreach (var m in str)
                {
                    List<Module> res = null;
                    if (spec.Rules.TryGetValue(m.S, out var rule)) res = rule(m, ctx);
                    if (res != null)
                    {
                        foreach (var n in res) { if (n.B < 0) n.B = k; next.Add(n); }
                    }
                    else next.Add(m);
                }
                str = next;
                if (str.Count > 9000) break; // safety for runaway hybrids
            }
            return str;
        }

        /// <summary>Readable form of a string, e.g. for a debug inspector.</summary>
        public static string Format(List<Module> str, int max = 400)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < str.Count && i < max; i++) sb.Append(str[i]).Append(' ');
            if (str.Count > max) sb.Append("… (").Append(str.Count).Append(" modules)");
            return sb.ToString();
        }
    }

    /// <summary>Small helpers so production rules read like the textbook notation.</summary>
    public static class L
    {
        public static Module M(char s, params float[] p) => new Module(s, p);

        /// <summary>Wraps modules in a branch: [ … ]</summary>
        public static Module[] Br(params Module[] inner)
        {
            var a = new Module[inner.Length + 2];
            a[0] = new Module('[');
            Array.Copy(inner, 0, a, 1, inner.Length);
            a[a.Length - 1] = new Module(']');
            return a;
        }

        public static List<Module> List(params object[] items)
        {
            var l = new List<Module>();
            foreach (var it in items)
            {
                if (it is Module m) l.Add(m);
                else if (it is IEnumerable<Module> e) l.AddRange(e);
            }
            return l;
        }

        /// <summary>Same integer hash as the previewer (used for deterministic colour variation).</summary>
        public static double Hash3(long a, long b, long c)
        {
            unchecked
            {
                int h = (int)(a * 374761393L + b * 668265263L + c * 2147483647L);
                uint u = (uint)h;
                u ^= u >> 13;
                u *= 1274126177u;
                u ^= u >> 16;
                return u / 4294967296.0;
            }
        }
    }
}
