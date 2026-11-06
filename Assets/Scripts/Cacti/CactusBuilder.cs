using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Realms.Cacti
{
    // ───────────────────────────── organs ─────────────────────────────
    public struct StemPoint { public Vector3 p; public float r, g; }

    public sealed class StemOrgan
    {
        public int style; public bool body, branch; public int born;
        public readonly List<StemPoint> pts = new List<StemPoint>();
    }
    public sealed class PadOrgan
    {
        public int style, gen; public Vector3 basePos, H, L, U; public float len, wid, thick, age, g;
    }
    public sealed class FlowerOrgan { public Vector3 pos, dir; public float g, scale; }

    public sealed class Organs
    {
        public readonly List<StemOrgan> stems = new List<StemOrgan>();
        public readonly List<PadOrgan> pads = new List<PadOrgan>();
        public readonly List<FlowerOrgan> flowers = new List<FlowerOrgan>();
    }

    /// <summary>
    /// Turns an L-system string into organs at continuous time T (in derivation steps), then into a mesh with four
    /// submeshes: 0 body · 1 spines/hair · 2 wool · 3 flowers.
    /// </summary>
    public static class CactusBuilder
    {
        const float DEG = Mathf.Deg2Rad, TAU = Mathf.PI * 2f;

        static float SStep(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }
        static Vector3 AnyPerp(Vector3 d) { var a = Mathf.Abs(d.y) < .9f ? Vector3.up : Vector3.right; return Vector3.Cross(d, a).normalized; }
        static Vector3 Rot(Vector3 v, Vector3 k, float ang)
        {
            float c = Mathf.Cos(ang), s = Mathf.Sin(ang);
            return v * c + Vector3.Cross(k, v) * s + k * (Vector3.Dot(k, v) * (1 - c));
        }

        // ─────────────────────────── 2. interpretation ───────────────────────────
        struct Turtle
        {
            public Vector3 pos, H, L, U; public int style; public StemOrgan stem, parent; public bool fresh; public object last;
        }

        public static Organs Interpret(List<Module> str, CactusSpecies spec, float T)
        {
            var o = new Organs();
            var st = new Turtle { pos = new Vector3(0, spec.BaseY - .02f, 0), H = Vector3.up, L = Vector3.left, U = Vector3.forward, fresh = true };
            var stack = new Stack<Turtle>();
            float Grow(int b, float dur) => SStep(0, 1, (T - b) / (dur <= 0 ? 1 : dur));

            void Tropism(float len)
            {
                float e = spec.Styles[st.style].trop; if (e == 0 || len <= 0) return;
                var tgt = e > 0 ? Vector3.up : Vector3.down;
                var ax = Vector3.Cross(st.H, tgt); float s = ax.magnitude; if (s < 1e-6f) return;
                var k = ax / s; float a = Mathf.Abs(e) * len * s;
                st.H = Rot(st.H, k, a).normalized; st.L = Rot(st.L, k, a).normalized; st.U = Rot(st.U, k, a).normalized;
            }
            void Turn(ref Vector3 x, ref Vector3 y, float ang)
            {
                float c = Mathf.Cos(ang), s = Mathf.Sin(ang);
                var nx = (x * c + y * s).normalized; var ny = (y * c - x * s).normalized; x = nx; y = ny;
            }

            foreach (var m in str)
            {
                if (m.B > T) continue; // not yet born
                switch (m.S)
                {
                    case 'F':
                    {
                        float g = Grow(m.B, spec.Dur); if (g <= 0) break;
                        float thick = SStep(0, spec.Thicken, T - m.B);
                        float r = m[1] * Mathf.Lerp(spec.R0, 1, thick) * (.35f + .65f * g);
                        if (st.stem == null || st.fresh)
                        {
                            st.stem = new StemOrgan { style = st.style, branch = st.parent != null, born = m.B };
                            st.stem.pts.Add(new StemPoint { p = st.pos, r = r, g = g });
                            o.stems.Add(st.stem); st.fresh = false;
                        }
                        st.pos += st.H * (m[0] * g);
                        st.stem.pts.Add(new StemPoint { p = st.pos, r = r, g = g });
                        st.last = st.stem;
                        Tropism(m[0] * g);
                        break;
                    }
                    case 'f': st.pos += st.H * m[0]; break;
                    case 'G':
                    {
                        float g = Grow(m.B, m[2]); if (g <= 0) break;
                        float R = m[0] * (.15f + .85f * g), Hh = m[1] * (.15f + .85f * g);
                        var s = new StemOrgan { style = st.style, body = true, born = m.B };
                        s.pts.Add(new StemPoint { p = st.pos, r = R, g = g });
                        s.pts.Add(new StemPoint { p = st.pos + st.H * Hh, r = R, g = g });
                        o.stems.Add(s); st.last = s; st.pos += st.H * Hh; st.fresh = true;
                        break;
                    }
                    case 'P':
                    {
                        float g = Grow(m.B, spec.Dur); if (g <= 0) break;
                        var pad = new PadOrgan { style = st.style, basePos = st.pos, H = st.H, L = st.L, U = st.U,
                            len = m[0] * (.25f + .75f * g), wid = m[1] * (.25f + .75f * g), thick = m[2], gen = (int)m[3], age = T - m.B, g = g };
                        o.pads.Add(pad); st.last = pad;
                        st.pos += st.H * (pad.len * .94f);
                        Tropism(pad.len);
                        break;
                    }
                    case 'K':
                    {
                        float g = Grow(m.B, 1); if (g <= 0) break;
                        o.flowers.Add(new FlowerOrgan { pos = st.pos, dir = st.H, g = g, scale = m.P.Length > 0 && m[0] != 0 ? m[0] : 1 });
                        break;
                    }
                    case 'W':
                    {
                        float g = Grow(m.B, 1);
                        if (g <= 0 || !(st.last is StemOrgan so)) break;
                        var a = so.pts[so.pts.Count - 2].p; var b = so.pts[so.pts.Count - 1].p; var dir = (b - a).normalized;
                        var sty = spec.Styles[so.style];
                        float r = so.pts[so.pts.Count - 1].r * (sty.ribDepth != 0 ? 1 - sty.ribDepth * .3f : 1);
                        float capL = so.body ? Mathf.Min((b - a).magnitude * .8f, r * 1.05f) : r * sty.cap;
                        var e1 = AnyPerp(dir); var e2 = Vector3.Cross(dir, e1);
                        int n = (int)m[0]; float lat = m[1] * DEG;
                        for (int i = 0; i < n; i++)
                        {
                            float az = TAU * i / n + (float)L.Hash3(i, m.B, 7) * .6f;
                            var rad = e1 * Mathf.Cos(az) + e2 * Mathf.Sin(az);
                            var pos = b + dir * (-capL + capL * Mathf.Cos(lat)) + rad * (r * Mathf.Sin(lat) * .97f);
                            var fd = (dir * Mathf.Cos(lat * .75f) + rad * Mathf.Sin(lat * .75f)).normalized;
                            o.flowers.Add(new FlowerOrgan { pos = pos, dir = fd, g = SStep(0, 1, T - (m.B + (float)L.Hash3(i, m.B, 3) * .5f)), scale = m.P.Length > 2 && m[2] != 0 ? m[2] : 1 });
                        }
                        break;
                    }
                    case '+': Turn(ref st.H, ref st.L, m[0] * DEG); break;
                    case '-': Turn(ref st.H, ref st.L, -m[0] * DEG); break;
                    case '&': Turn(ref st.H, ref st.U, m[0] * DEG); break;
                    case '^': Turn(ref st.H, ref st.U, -m[0] * DEG); break;
                    case '/': Turn(ref st.L, ref st.U, m[0] * DEG); break;
                    case '\\': Turn(ref st.L, ref st.U, -m[0] * DEG); break;
                    case 'Y': st.style = (int)m[0]; st.fresh = true; break;
                    case 'Z': st.fresh = true; break;
                    case '[': stack.Push(st); st.parent = st.stem ?? st.parent; st.fresh = true; break;
                    case ']': st = stack.Pop(); break;
                }
            }
            return o;
        }

        // ─────────────────────────── 3. meshing ───────────────────────────
        sealed class Buf
        {
            public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Color> c = new List<Color>(); public readonly List<int> i = new List<int>();
            public int V(Vector3 p, Color col) { v.Add(p); c.Add(col); return v.Count - 1; }
            public void T(int a, int b, int d) { i.Add(a); i.Add(b); i.Add(d); }
        }
        sealed class Bufs { public readonly Buf body = new Buf(), spine = new Buf(), wool = new Buf(), flower = new Buf(); public bool draft; }

        public sealed class BuildResult
        {
            public Mesh mesh; public Organs organs; public int modules, areoles, spines;
        }

        static readonly Dictionary<string, List<Module>> deriveCache = new Dictionary<string, List<Module>>();

        /// <summary>Derives (cached per species+seed), interprets at time T and meshes. Reuses <paramref name="mesh"/> if given.</summary>
        public static BuildResult Build(CactusSpecies spec, int seed, float T, bool spines = true, bool draft = false, Mesh mesh = null)
        {
            string key = spec.Id + ":" + seed;
            if (!deriveCache.TryGetValue(key, out var str)) { str = LSystem.Derive(spec, seed); deriveCache[key] = str; }
            var org = Interpret(str, spec, T);
            var R = new Rng(seed * 31 + 7);
            var B = new Bufs { draft = draft };
            var res = new BuildResult { organs = org, modules = str.Count };
            BuildPot(B, spec);
            foreach (var s in org.stems) BuildStem(B, s, spec, R, res);
            foreach (var p in org.pads) BuildPad(B, p, spec, R, res);
            foreach (var f in org.flowers) BuildFlower(B, f, spec.Flower, R);
            if (!spines) { B.spine.v.Clear(); B.spine.c.Clear(); B.spine.i.Clear(); }
            res.mesh = ToMesh(B, mesh);
            return res;
        }

        public static void ClearCache() => deriveCache.Clear();

        static Mesh ToMesh(Bufs B, Mesh mesh)
        {
            if (mesh == null) mesh = new Mesh { name = "Cactus" };
            mesh.Clear();
            mesh.indexFormat = IndexFormat.UInt32;
            var verts = new List<Vector3>(); var cols = new List<Color>();
            var parts = new[] { B.body, B.spine, B.wool, B.flower };
            var offs = new int[parts.Length];
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            for (int p = 0; p < parts.Length; p++)
            {
                offs[p] = verts.Count; verts.AddRange(parts[p].v);
                foreach (var c in parts[p].c) cols.Add(linear ? c.linear : c);
            }
            mesh.SetVertices(verts); mesh.SetColors(cols);
            mesh.subMeshCount = parts.Length;
            for (int p = 0; p < parts.Length; p++)
            {
                var idx = new List<int>(parts[p].i.Count);
                foreach (var k in parts[p].i) idx.Add(k + offs[p]);
                mesh.SetTriangles(idx, p, false);
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ── surface profile: ribs or tubercles ──
        static float Profile(CactusStyle sty, float th, float s, float r, float rRef, out float crest)
        {
            if (sty.tub != null)
            {
                float u = s / (sty.tub.R > 0 ? sty.tub.R : rRef);
                float c1 = .5f + .5f * Mathf.Cos(sty.tub.m * th - sty.tub.m * u), c2 = .5f + .5f * Mathf.Cos(sty.tub.n * th + sty.tub.n * u);
                float q = Mathf.Pow(c1 * c2, sty.tub.pow);
                crest = Mathf.Sqrt(q);
                return r * (1 + sty.tub.amp * q);
            }
            if (sty.ribs == 0) { crest = .5f; return r; }
            float c = .5f + .5f * Mathf.Cos(sty.ribs * (th - sty.twist * s));
            crest = Mathf.Pow(c, sty.ribPow);
            return r * (1 - sty.ribDepth * (1 - crest));
        }

        static void AddSpine(Buf b, Vector3 basePos, Vector3 dir, float len, float w, Color c0, Color c1)
        {
            var a = AnyPerp(dir); var bb = Vector3.Cross(dir, a); var tip = basePos + dir * len;
            int i0 = b.V(basePos + a * w, c0), i1 = b.V(basePos - a * w, c0), it = b.V(tip, c1), i2 = b.V(basePos + bb * w, c0), i3 = b.V(basePos - bb * w, c0);
            b.T(i0, i1, it); b.T(i2, i3, it);
        }
        static void AddTuft(Buf b, Vector3 p, Vector3 n, float r, Color c)
        {
            var a = AnyPerp(n); var bb = Vector3.Cross(n, a); var q = p + n * (r * .2f); float h = r * .45f;
            int top = b.V(q + n * h, c), bot = b.V(q - n * (h * .5f), c);
            int[] ring = { b.V(q + a * r, c), b.V(q + bb * r, c), b.V(q - a * r, c), b.V(q - bb * r, c) };
            for (int k = 0; k < 4; k++) { b.T(ring[k], ring[(k + 1) % 4], top); b.T(ring[(k + 1) % 4], ring[k], bot); }
        }
        static void AddHair(Buf b, Vector3 p, Vector3 n, float len, float w, Color c, Rng R)
        {
            var d = (n + Vector3.down * .25f).normalized; var q = p; const int segs = 4;
            for (int i = 0; i < segs; i++)
            {
                var jit = new Vector3((float)R.Next() - .5f, (float)R.Next() - .5f, (float)R.Next() - .5f);
                var nd = (d + Vector3.down * .32f + jit * .25f).normalized;
                var q2 = q + nd * (len / segs);
                var a = AnyPerp(nd); var bb = Vector3.Cross(nd, a);
                foreach (var e in new[] { a, bb })
                {
                    int i0 = b.V(q + e * w, c), i1 = b.V(q - e * w, c), i2 = b.V(q2 - e * w, c), i3 = b.V(q2 + e * w, c);
                    b.T(i0, i1, i2); b.T(i0, i2, i3);
                }
                q = q2; d = nd;
            }
        }

        static void Areole(Bufs B, CactusStyle sty, Vector3 p, Vector3 nrm, Vector3 tng, float g, long key, Rng R, BuildResult res)
        {
            var sp = sty.spines;
            if (B.draft && (key & 1) != 0) return; // draft LOD: half the areoles
            res.areoles++;
            if (sty.wool != null) AddTuft(B.wool, p, nrm, sty.wool.size * (.5f + .5f * g), sty.wool.col);
            if (sty.hair != null && g > .2f)
                for (int h = 0; h < (B.draft ? 1 : sty.hair.n); h++) { AddHair(B.spine, p, nrm, sty.hair.len * (.6f + .6f * (float)R.Next()) * g, sty.hair.w, sty.hair.col, R); res.spines++; }
            if (sp == null || g < .15f) return;
            var e1 = Vector3.Cross(nrm, tng).normalized; var e2 = Vector3.Cross(e1, nrm);
            float cvar = sp.vary > 0 ? (float)L.Hash3(key, 1, 9) * sp.vary : 0;
            var col0 = new Color(Mathf.Clamp01(sp.col.r - cvar * .3f + cvar * .25f), Mathf.Clamp01(sp.col.g - cvar * .3f), Mathf.Clamp01(sp.col.b - cvar * .3f));
            var col1 = sp.tip;
            for (int k = 0; k < sp.nr; k++)
            {
                float az = TAU * k / sp.nr + (float)R.Next() * .4f, a = (sp.ang + ((float)R.Next() - .5f) * 14) * DEG;
                var d = (nrm * Mathf.Cos(a) + (e1 * Mathf.Cos(az) + e2 * Mathf.Sin(az)) * Mathf.Sin(a)).normalized;
                AddSpine(B.spine, p, d, sp.lr * (.7f + .6f * (float)R.Next()) * g, sp.w, col0, col1); res.spines++;
            }
            for (int k = 0; k < sp.nc; k++)
            {
                float az = TAU * k / Mathf.Max(1, sp.nc) + (float)R.Next(), a = (sp.nc > 1 ? 24 : 6) * DEG;
                var d = (nrm * Mathf.Cos(a) + (e1 * Mathf.Cos(az) + e2 * Mathf.Sin(az)) * Mathf.Sin(a)).normalized;
                d = (d - tng * sp.down).normalized;
                AddSpine(B.spine, p, d, sp.lc * (.75f + .5f * (float)R.Next()) * g, sp.w * 1.35f, sp.cc ?? col0, sp.ctip ?? col1); res.spines++;
            }
        }

        struct Row { public float s, r0, r, g; public Vector3 c, t, n, b; }

        static void BuildStem(Bufs B, StemOrgan stem, CactusSpecies spec, Rng R, BuildResult res)
        {
            var sty = spec.Styles[stem.style];
            if (stem.pts.Count < 2) return;
            float rEnd = stem.pts[stem.pts.Count - 1].r, rRef = sty.tub != null ? (sty.tub.R > 0 ? sty.tub.R : .05f) : rEnd;
            // ring spacing follows the finest surface detail: tubercle pitch, fleck size, or radius
            float ds = sty.tub != null ? Mathf.Clamp(rRef * .08f, .0015f, .02f) : sty.fleck > 0 ? .002f : stem.body ? Mathf.Clamp(rEnd * .2f, .0025f, .03f) : Mathf.Clamp(rEnd * .4f, .004f, .05f);

            // dense Catmull-Rom centreline
            var C = new List<StemPoint>(); var P = stem.pts;
            for (int i = 0; i < P.Count - 1; i++)
            {
                var p0 = P[Mathf.Max(0, i - 1)]; var p1 = P[i]; var p2 = P[i + 1]; var p3 = P[Mathf.Min(P.Count - 1, i + 2)];
                int n = Mathf.Max(1, Mathf.CeilToInt((p2.p - p1.p).magnitude / ds));
                for (int j = 0; j < n; j++)
                {
                    float t = (float)j / n, t2 = t * t, t3 = t2 * t;
                    var w = .5f * (2 * p1.p + (-p0.p + p2.p) * t + (2 * p0.p - 5 * p1.p + 4 * p2.p - p3.p) * t2 + (-p0.p + 3 * p1.p - 3 * p2.p + p3.p) * t3);
                    C.Add(new StemPoint { p = w, r = Mathf.Lerp(p1.r, p2.r, t), g = Mathf.Lerp(p1.g, p2.g, t) });
                }
            }
            C.Add(P[P.Count - 1]);
            var S = new float[C.Count];
            for (int i = 1; i < C.Count; i++) S[i] = S[i - 1] + (C[i].p - C[i - 1].p).magnitude;
            float Ltot = S[S.Length - 1]; if (Ltot < 1e-4f) return;
            float capL = stem.body ? Mathf.Min(Ltot * .8f, rEnd * 1.05f) : Mathf.Min(Ltot * .9f, rEnd * sty.cap);
            float capStart = Ltot - capL;

            var rows = new List<Row>();
            void PushRow(float s)
            {
                int i = 1; while (i < S.Length - 1 && S[i] < s) i++;
                float t = (s - S[i - 1]) / Mathf.Max(1e-9f, S[i] - S[i - 1]);
                rows.Add(new Row { s = s, c = Vector3.Lerp(C[i - 1].p, C[i].p, t), r0 = Mathf.Lerp(C[i - 1].r, C[i].r, t), g = Mathf.Lerp(C[i - 1].g, C[i].g, t) });
            }
            for (int i = 0; i < S.Length; i++) if (S[i] < capStart) PushRow(S[i]);
            int nCap = Mathf.Max(6, Mathf.CeilToInt(capL / ds * 1.6f));
            for (int k = 0; k <= nCap; k++) PushRow(capStart + capL * Mathf.Sin((float)k / nCap * Mathf.PI / 2));

            // parallel-transport frames, radius with neck, body rounding and apex dome
            Vector3 N = Vector3.zero; bool hasN = false;
            for (int i = 0; i < rows.Count; i++)
            {
                var a = rows[Mathf.Max(0, i - 1)].c; var bpt = rows[Mathf.Min(rows.Count - 1, i + 1)].c;
                var t = bpt - a; t = t.sqrMagnitude < 1e-18f ? (i > 0 ? rows[i - 1].t : Vector3.up) : t.normalized;
                if (!hasN) { N = Vector3.right - t * Vector3.Dot(Vector3.right, t); if (N.magnitude < .1f) N = AnyPerp(t); N.Normalize(); hasN = true; }
                else N = (N - t * Vector3.Dot(N, t)).normalized;
                var row = rows[i]; row.t = t; row.n = N; row.b = Vector3.Cross(t, N);
                float r = row.r0, s = row.s;
                if (stem.branch && sty.neck > 0) r *= Mathf.Lerp(sty.neck, 1, SStep(0, rEnd * 1.6f, s));
                if (stem.body) r *= Mathf.Lerp(.86f, 1, SStep(0, Mathf.Min(Ltot * .3f, rEnd * .5f), s));
                if (s > capStart) { float x = (s - capStart) / capL; r *= Mathf.Sqrt(Mathf.Max(0, 1 - x * x)); }
                row.r = r; rows[i] = row;
            }

            int Mr = sty.tub != null ? 96 : sty.fleck > 0 ? 160 : Mathf.Max(48, (sty.ribs > 0 ? sty.ribs : 6) * 6);
            var body = B.body; int baseIdx = body.v.Count; var col = sty.col;
            for (int i = 0; i < rows.Count; i++)
            {
                var w = rows[i];
                for (int k = 0; k < Mr; k++)
                {
                    float th = TAU * k / Mr; float rr = Profile(sty, th, w.s, w.r, rRef, out float crest);
                    var d = w.n * Mathf.Cos(th) + w.b * Mathf.Sin(th);
                    float shade = .74f + .3f * crest + ((float)L.Hash3(k, i, 11) - .5f) * .05f;
                    var c = new Color(col.r * shade, col.g * shade, col.b * shade);
                    if (sty.cork != null) { float f = SStep(sty.cork.h, 0, w.s) * (stem.branch ? 0 : 1) * SStep(.3f, 1, w.g); c = Color.Lerp(c, sty.cork.col, f * .85f); }
                    if (sty.tip.HasValue) { float f = SStep(Ltot - .14f, Ltot, w.s); c = Color.Lerp(c, sty.tip.Value, f * .6f); }
                    if (sty.fleck > 0) { float f = (float)L.Hash3(k, Mathf.FloorToInt(w.s / .0025f), 5); if (f < sty.fleck) c = Color.Lerp(c, new Color(.92f, .92f, .92f), .55f + .3f * f / sty.fleck); }
                    body.V(w.c + d * rr, c);
                }
            }
            for (int i = 0; i < rows.Count - 1; i++)
                for (int k = 0; k < Mr; k++)
                {
                    int a = baseIdx + i * Mr + k, b = baseIdx + i * Mr + (k + 1) % Mr, c2 = a + Mr, d2 = b + Mr;
                    body.T(a, b, c2); body.T(b, d2, c2);
                }
            if (stem.body || !stem.branch)
            {
                int cb = body.V(rows[0].c, col * .6f);
                for (int k = 0; k < Mr; k++) body.T(baseIdx + k, cb, baseIdx + (k + 1) % Mr);
            }

            // areoles on rib crests or tubercle tips
            Row At(float s)
            {
                int i = 1; while (i < rows.Count - 1 && rows[i].s < s) i++;
                var A = rows[i - 1]; var Bb = rows[i]; float t = Mathf.Clamp01((s - A.s) / Mathf.Max(1e-9f, Bb.s - A.s));
                return new Row { s = s, c = Vector3.Lerp(A.c, Bb.c, t), n = Vector3.Lerp(A.n, Bb.n, t).normalized, t = Vector3.Lerp(A.t, Bb.t, t).normalized, r = Mathf.Lerp(A.r, Bb.r, t), g = Mathf.Lerp(A.g, Bb.g, t) };
            }
            void Place(float th, float s, long key)
            {
                if (s <= 0 || s >= Ltot - .002f) return;
                var f = At(s); if (f.r < .002f) return;
                var bvec = Vector3.Cross(f.t, f.n); var d = f.n * Mathf.Cos(th) + bvec * Mathf.Sin(th);
                float rr = Profile(sty, th, s, f.r, rRef, out _);
                var f2 = At(Mathf.Min(Ltot, s + .002f)); float dr = (f2.r - f.r) / .002f;
                var nrm = (d - f.t * dr).normalized;
                Areole(B, sty, f.c + d * rr, nrm, f.t, f.g, key, R, res);
            }
            float sp = sty.areole > 0 ? sty.areole : .03f;
            if (sty.tub != null)
            {
                int m = sty.tub.m, n = sty.tub.n; float Rr = sty.tub.R > 0 ? sty.tub.R : rRef;
                for (int i = 0; i < m; i++)
                {
                    int j0 = Mathf.CeilToInt(n * ((float)i / m)), j1 = Mathf.FloorToInt(n * (Ltot / (Mathf.PI * Rr) + (float)i / m));
                    for (int j = j0; j <= j1; j++) Place(Mathf.PI * ((float)i / m + (float)j / n), Mathf.PI * Rr * ((float)j / n - (float)i / m), i * 997 + j);
                }
            }
            else if (sty.ribs > 0)
            {
                for (int j = 0; j < sty.ribs; j++)
                    for (float s = sp * (.6f + .5f * (j % 2)); s < Ltot; s += sp)
                        Place(TAU * j / sty.ribs + sty.twist * s, s, j * 7919 + Mathf.RoundToInt(s * 1000));
            }
        }

        static float PadWidth(CactusStyle sty, float u)
        {
            if (sty.teeth)
            {
                float w = (.42f + .5f * u) * (u > .78f ? Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((u - .78f) / .22f, 2))) : 1);
                w += .26f * Mathf.Exp(-Mathf.Pow((u - .5f) / .06f, 2)) + .34f * Mathf.Exp(-Mathf.Pow((u - .86f) / .05f, 2));
                return w * Mathf.Min(1, u * 6 + .15f);
            }
            return Mathf.Pow(Mathf.Max(0, Mathf.Sin(Mathf.PI * Mathf.Clamp01(u))), .62f) * (.62f + .5f * u); // float sin(π) < 0 would NaN the Pow
        }

        static void BuildPad(Bufs B, PadOrgan pad, CactusSpecies spec, Rng R, BuildResult res)
        {
            var sty = spec.Styles[pad.style];
            float lig = sty.lignify.HasValue ? SStep(sty.lignify.Value.x, sty.lignify.Value.y, pad.age) * (pad.gen <= 1 ? 1 : 0) : 0;
            float Lp = pad.len, W = pad.wid * Mathf.Lerp(1, .55f, lig), Th = pad.thick * Mathf.Lerp(1, 3.2f, lig);
            const int nu = 22, nv = 14; var body = B.body; int baseIdx = body.v.Count;
            var col = sty.trunkCol.HasValue ? Color.Lerp(sty.col, sty.trunkCol.Value, lig) : sty.col;
            Vector3 PosAt(float u, float v, float side)
            {
                float w = W * .5f * PadWidth(sty, u), t = Th * Mathf.Sqrt(Mathf.Max(0, 1 - v * v)) * (.55f + .45f * Mathf.Sin(Mathf.PI * u)) * side;
                return pad.basePos + pad.H * (u * Lp) + pad.L * (v * w) + pad.U * t;
            }
            foreach (var side in new[] { 1, -1 })
                for (int i = 0; i <= nu; i++)
                    for (int j = 0; j <= nv; j++)
                    {
                        float u = (float)i / nu, v = -1 + 2f * j / nv, shade = .8f + .2f * (1 - Mathf.Abs(v)) + ((float)L.Hash3(i, j, side) - .5f) * .05f;
                        float young = sty.young.HasValue ? SStep(.6f, 0, pad.age) : 0;
                        var c = new Color(col.r * shade, col.g * shade, col.b * shade);
                        if (sty.young.HasValue) c = Color.Lerp(c, sty.young.Value, young * .5f);
                        body.V(PosAt(u, v, side), c);
                    }
            int row = nv + 1, half = (nu + 1) * row;
            for (int s = 0; s < 2; s++)
                for (int i = 0; i < nu; i++)
                    for (int j = 0; j < nv; j++)
                    {
                        int a = baseIdx + s * half + i * row + j, b = a + 1, c = a + row, d = c + 1;
                        if (s == 0) { body.T(a, c, b); body.T(b, c, d); } else { body.T(a, b, c); body.T(b, d, c); }
                    }
            float sp = sty.areole; if (sp <= 0) return;
            for (int i = 0; (i + .5f) * sp < Lp; i++)
            {
                float u = (i + .5f) * sp / Lp, w = W * .5f * PadWidth(sty, u);
                for (float x = ((i % 2) * .5f - Mathf.Floor(w / sp)) * sp; x < w * .86f; x += sp)
                {
                    if (Mathf.Abs(x) > w * .86f) continue;
                    foreach (var side in new[] { 1, -1 })
                    {
                        float v = x / Mathf.Max(w, 1e-6f); var p = PosAt(u, v, side); var n = (pad.U * side + pad.L * (v * .35f)).normalized;
                        Areole(B, sty, p, n, pad.H, pad.g, i * 131 + Mathf.RoundToInt(x * 1e4f) + (side > 0 ? 7 : 0), R, res);
                    }
                }
            }
        }

        static void BuildFlower(Bufs B, FlowerOrgan f, FlowerSpec s, Rng R)
        {
            float g = f.g, sc = f.scale * (.45f + .55f * g);
            var d = f.dir.normalized; var e1 = AnyPerp(d); var e2 = Vector3.Cross(d, e1); var fb = B.flower;
            foreach (var ring in s.rings)
            {
                for (int k = 0; k < ring.n; k++)
                {
                    float az = TAU * (k + ring.off) / ring.n + ((float)R.Next() - .5f) * .15f;
                    var rad = e1 * Mathf.Cos(az) + e2 * Mathf.Sin(az); var tan = Vector3.Cross(d, rad);
                    float elev = Mathf.Lerp(4, ring.elev, g) * DEG; int baseIdx = fb.v.Count; const int segs = 6;
                    var q = f.pos + rad * (s.r0 * sc);
                    for (int i = 0; i <= segs; i++)
                    {
                        float u = (float)i / segs, a = elev + ring.curl * DEG * u * u * g;
                        var dir = d * Mathf.Cos(a) + rad * Mathf.Sin(a);
                        if (i > 0) q += dir * (ring.len * sc / segs);
                        float w = ring.wid * sc * Mathf.Pow(Mathf.Max(0, Mathf.Sin(Mathf.PI * Mathf.Clamp01(u * .92f + .04f))), .7f);
                        var c = Color.Lerp(ring.baseCol ?? ring.col, ring.col, SStep(0, .5f, u));
                        fb.V(q + tan * (w * .5f), c); fb.V(q - tan * (w * .5f), c);
                    }
                    for (int i = 0; i < segs; i++) { int a = baseIdx + 2 * i; fb.T(a, a + 1, a + 2); fb.T(a + 1, a + 3, a + 2); }
                }
            }
            for (int k = 0; k < s.stamens; k++)
            {
                float az = (float)R.Next() * TAU, rr = s.r0 * sc * (.2f + .8f * (float)R.Next());
                var rad = e1 * Mathf.Cos(az) + e2 * Mathf.Sin(az);
                var p = f.pos + rad * rr; var dir = (d + rad * (.35f + .3f * (float)R.Next())).normalized;
                AddSpine(fb, p, dir, s.stLen * sc * (.7f + .5f * (float)R.Next()), .0006f * sc * 30, s.stCol, s.antCol);
            }
        }

        static void BuildPot(Bufs B, CactusSpecies spec)
        {
            var P = spec.Pot; if (P == null) return;
            const int n = 40; var body = B.body;
            var prof = new[] { new Vector2(P.r * .78f, 0), new Vector2(P.r * .8f, .005f), new Vector2(P.r, P.h - .02f), new Vector2(P.r * 1.08f, P.h - .02f), new Vector2(P.r * 1.08f, P.h), new Vector2(P.r * .96f, P.h), new Vector2(P.r * .96f, P.h - .015f) };
            int baseIdx = body.v.Count;
            foreach (var q in prof)
                for (int k = 0; k < n; k++)
                {
                    float a = TAU * k / n; float sh = .92f + .08f * (float)L.Hash3(k, Mathf.RoundToInt(q.y * 100), 2);
                    body.V(new Vector3(q.x * Mathf.Cos(a), q.y, q.x * Mathf.Sin(a)), P.col * sh);
                }
            for (int i = 0; i < prof.Length - 1; i++)
                for (int k = 0; k < n; k++) { int a = baseIdx + i * n + k, b = baseIdx + i * n + (k + 1) % n; body.T(a, a + n, b); body.T(b, a + n, b + n); }
            int cen = body.V(new Vector3(0, P.h - .015f, 0), new Color(.24f, .17f, .12f)); int sb = cen;
            for (int k = 0; k < n; k++) { float a = TAU * k / n; body.V(new Vector3(P.r * .96f * Mathf.Cos(a), P.h - .015f, P.r * .96f * Mathf.Sin(a)), new Color(.28f, .2f, .14f)); }
            for (int k = 0; k < n; k++) body.T(cen, sb + 1 + k, sb + 1 + (k + 1) % n);
        }
    }
}
