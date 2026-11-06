using System.Collections.Generic;
using UnityEngine;
using static Realms.Cacti.L;

namespace Realms.Cacti
{
    public enum CactusKind
    {
        Saguaro, GoldenBarrel, PricklyPear, TeddyBearCholla, OrganPipe,
        Pincushion, BishopsCap, ChristmasCactus, OldMan, Hedgehog
    }

    public sealed class SpineSpec
    {
        public int nr, nc;                 // radial / central spines per areole
        public float lr, lc;               // lengths (m)
        public float ang = 60f;            // radial spread from the surface normal (deg)
        public float w = .001f;            // base half-width (m)
        public Color col, tip;
        public Color? cc, ctip;            // central spine colours (optional)
        public float down;                 // central spines bend toward the base
        public float vary;                 // per-areole colour variation
    }
    public sealed class WoolSpec { public float size; public Color col; }
    public sealed class HairSpec { public int n; public float len, w; public Color col; }
    public sealed class TubercleSpec { public int m, n; public float amp, R, pow = 1.4f; }
    public sealed class CorkSpec { public float h; public Color col; }

    /// <summary>Surface style of a stem, body or pad. A species can switch styles mid-string with Y(i).</summary>
    public sealed class CactusStyle
    {
        public int ribs;                   // 0 = no ribs
        public float ribDepth, ribPow = .45f, twist;
        public TubercleSpec tub;           // tubercles instead of ribs
        public Color col;
        public Color? tip, young, trunkCol;
        public CorkSpec cork;
        public float cap = 1f;             // apex dome length, in stem radii
        public float neck;                 // branch neck radius ratio (0 = none)
        public float trop;                 // tropism, per metre (+ up, − down)
        public float areole = .03f;        // areole spacing along ribs / pad lattice (m)
        public float fleck;                // white trichome flecks (Astrophytum)
        public bool teeth;                 // Schlumbergera segment outline
        public Vector2? lignify;           // pads of generation ≤1 turn woody between these ages (steps)
        public WoolSpec wool;
        public SpineSpec spines;
        public HairSpec hair;
    }

    public sealed class PetalRing
    {
        public int n; public float off, len, wid, elev, curl;
        public Color col; public Color? baseCol;
    }
    public sealed class FlowerSpec
    {
        public float r0 = .006f; public int stamens = 30; public float stLen = .012f;
        public Color stCol = new Color(.98f, .93f, .7f), antCol = new Color(.98f, .82f, .25f);
        public PetalRing[] rings;
    }
    public sealed class PotSpec { public float r, h; public Color col; }

    public sealed class CactusSpecies
    {
        public CactusKind Kind;
        public string Id, Name, Latin, Blurb;
        public string[] Facts, RulesText;
        public int Steps;
        public float Dur = 1f, Thicken = 4f, R0 = .55f, BaseY;
        public PotSpec Pot;
        public CactusStyle[] Styles;
        public FlowerSpec Flower;
        public System.Func<LContext, List<Module>> Axiom;
        public Dictionary<char, Rule> Rules = new Dictionary<char, Rule>();
    }

    /// <summary>
    /// Ten cacti as parametric, stochastic L-systems (lengths in metres, angles in degrees; one step ≈ one season).
    /// Kept rule-for-rule identical to the browser previewer so seeds match.
    /// Turtle symbols: F(len,r) stem segment · f(len) move · G(R,H,steps) ribbed body · P(len,wid,thick,gen) pad ·
    /// K(scale) flower · W(n,lat,scale) crown of flowers around the last apex · + − yaw · &amp; ^ pitch · / \ roll ·
    /// [ ] branch · Y(i) surface style · Z start a new joint.
    /// </summary>
    public static class CactusLibrary
    {
        static Color C(float r, float g, float b) => new Color(r, g, b);
        static FlowerSpec Fl(float r0, int stamens, float stLen, params PetalRing[] rings) => new FlowerSpec { r0 = r0, stamens = stamens, stLen = stLen, rings = rings };
        static PetalRing Ring(int n, float off, float len, float wid, float elev, float curl, Color col, Color? b = null) =>
            new PetalRing { n = n, off = off, len = len, wid = wid, elev = elev, curl = curl, col = col, baseCol = b };

        static Dictionary<CactusKind, CactusSpecies> cache;
        public static CactusSpecies Get(CactusKind k) { if (cache == null) Build(); return cache[k]; }
        public static IEnumerable<CactusSpecies> All { get { if (cache == null) Build(); return cache.Values; } }

        static void Build()
        {
            cache = new Dictionary<CactusKind, CactusSpecies>();
            foreach (var s in new[] { Saguaro(), Barrel(), Opuntia(), Cholla(), OrganPipe(), Mammillaria(), Astrophytum(), Schlumbergera(), OldMan(), Hedgehog() })
                cache[s.Kind] = s;
        }

        // 1 ───────────────────────────────────────────────────────────
        static CactusSpecies Saguaro()
        {
            var spines = new SpineSpec { nr = 8, nc = 2, lr = .022f, lc = .042f, ang = 72, w = .0011f, col = C(.42f, .38f, .34f), tip = C(.22f, .2f, .18f), down = .3f };
            var wool = new WoolSpec { size = .006f, col = C(.62f, .56f, .46f) };
            var s = new CactusSpecies
            {
                Kind = CactusKind.Saguaro, Id = "saguaro", Name = "Saguaro", Latin = "Carnegiea gigantea",
                Blurb = "A single fluted column that, after decades, throws out arms which curve skyward.",
                Steps = 24, Thicken = 6, R0 = .6f,
                Styles = new[] {
                    new CactusStyle { ribs = 18, ribDepth = .13f, ribPow = .5f, col = C(.27f,.43f,.24f), tip = C(.42f,.58f,.3f), cork = new CorkSpec{ h = .9f, col = C(.42f,.38f,.3f) }, cap = .9f, areole = .05f, wool = wool, spines = spines },
                    new CactusStyle { ribs = 14, ribDepth = .13f, ribPow = .5f, col = C(.28f,.45f,.25f), tip = C(.42f,.58f,.3f), cap = .9f, neck = .62f, trop = 1.6f, areole = .05f, wool = wool, spines = spines } },
                Flower = Fl(.016f, 70, .02f, Ring(9, 0, .062f, .042f, 62, 25, C(.99f,.98f,.93f), C(.95f,.93f,.8f)), Ring(8, .5f, .05f, .036f, 40, 15, C(.99f,.98f,.95f))),
                Axiom = c => List(M('Y', 0), M('T', 0)),
            };
            s.Rules['T'] = (m, c) =>
            {
                int i = (int)m[0];
                if (i >= 20) return List(M('W', 6, 38));
                var o = List(M('F', .34f, .3f));
                c.State.TryGetValue("arms", out int arms);
                if (i >= 9 && i <= 17 && arms < 5 && c.R() < .45)
                {
                    c.State["arms"] = arms + 1;
                    o.AddRange(Br(M('/', i * 137.5f + c.Rnd(-20, 20)), M('&', c.Rnd(72, 88)), M('Y', 1), M('A', 0, Mathf.Floor(c.Rnd(6, 11)))));
                }
                o.Add(M('T', i + 1));
                return o;
            };
            s.Rules['A'] = (m, c) => m[0] < m[1] ? List(M('F', .25f, .21f), M('A', m[0] + 1, m[1])) : (c.K >= 20 ? List(M('W', 3, 40)) : null);
            return s;
        }

        // 2 ───────────────────────────────────────────────────────────
        static CactusSpecies Barrel()
        {
            var s = new CactusSpecies
            {
                Kind = CactusKind.GoldenBarrel, Id = "barrel", Name = "Golden barrel", Latin = "Echinocactus grusonii",
                Blurb = "A solitary golden-spined globe with 20–40 sharp ribs and a woolly crown; old plants raise pups.",
                Steps = 14,
                Styles = new[] { new CactusStyle { ribs = 26, ribDepth = .2f, ribPow = .4f, col = C(.24f,.46f,.22f), cap = 1, areole = .028f,
                    wool = new WoolSpec { size = .007f, col = C(.93f,.86f,.62f) },
                    spines = new SpineSpec { nr = 9, nc = 3, lr = .03f, lc = .045f, ang = 62, w = .0011f, col = C(.95f,.78f,.3f), tip = C(.99f,.92f,.62f), down = .15f } } },
                Flower = Fl(.006f, 20, .008f, Ring(12, 0, .025f, .007f, 38, 10, C(.99f,.85f,.22f))),
                Axiom = c => List(M('Y', 0), M('X', 0), M('G', .3f, .32f, 11), M('Q', 0)),
            };
            s.Rules['X'] = (m, c) =>
            {
                int i = (int)m[0]; if (i >= 12) return new List<Module>();
                var o = new List<Module>();
                if ((i == 6 || i == 9) && c.R() < .7) o.AddRange(Br(M('/', c.Rnd(0, 360)), M('&', 90), M('f', .35f), M('^', 90), M('G', .1f, .1f, 4), M('Q', 4)));
                o.Add(M('X', i + 1)); return o;
            };
            s.Rules['Q'] = (m, c) => m[0] < 9 ? List(M('Q', m[0] + 1)) : List(M('W', 12, 16, .9f));
            return s;
        }

        // 3 ───────────────────────────────────────────────────────────
        static CactusSpecies Opuntia()
        {
            var s = new CactusSpecies
            {
                Kind = CactusKind.PricklyPear, Id = "opuntia", Name = "Prickly pear", Latin = "Opuntia engelmannii",
                Blurb = "Chains of flat pads budding from the upper rim; the oldest pads lignify into a woody trunk.",
                Steps = 9,
                Styles = new[] { new CactusStyle { col = C(.36f,.54f,.32f), young = C(.55f,.72f,.36f), trunkCol = C(.47f,.42f,.33f), lignify = new Vector2(4, 8), areole = .032f,
                    wool = new WoolSpec { size = .004f, col = C(.55f,.42f,.25f) },
                    spines = new SpineSpec { nr = 2, nc = 1, lr = .022f, lc = .035f, ang = 40, w = .001f, col = C(.94f,.86f,.5f), tip = C(.99f,.96f,.78f), down = .25f } } },
                Flower = new FlowerSpec { r0 = .01f, stamens = 70, stLen = .012f, stCol = C(.98f,.9f,.5f), rings = new[] {
                    Ring(8, 0, .04f, .03f, 55, 10, C(.99f,.88f,.25f), C(.95f,.7f,.2f)), Ring(7, .5f, .032f, .026f, 30, 8, C(.99f,.9f,.35f)) } },
                Axiom = c => List(M('Y', 0), M('P', .26f, .19f, .016f, 0), M('X', 0, .26f)),
            };
            s.Rules['X'] = (m, c) =>
            {
                int g = (int)m[0]; float Lp = m[1];
                if (g >= 4)
                {
                    if (c.K < 7) return null;
                    var fl = List(Br(M('+', c.Rnd(-20, 20)), M('K', 1)));
                    if (c.R() < .5) fl.AddRange(Br(M('f', -Lp * .25f), M('+', c.Rnd(30, 50) * (c.R() < .5 ? -1 : 1)), M('f', Lp * .05f), M('K', .9f)));
                    return fl;
                }
                var o = new List<Module>();
                int n = 1 + (c.R() < .7 ? 1 : 0) + (c.R() < .25 ? 1 : 0);
                for (int k = 0; k < n; k++)
                {
                    float ang = k == 0 ? c.Rnd(-20, 20) : (k == 1 ? 1 : -1) * c.Rnd(28, 55);
                    float l2 = Lp * c.Rnd(.82f, 1.06f);
                    o.AddRange(Br(M('f', -Lp * .1f), M('+', ang), M('/', c.Rnd(-30, 30)), M('P', l2, l2 * .74f, .014f, g + 1), M('X', g + 1, l2)));
                }
                return o;
            };
            return s;
        }

        // 4 ───────────────────────────────────────────────────────────
        static CactusSpecies Cholla()
        {
            var s = new CactusSpecies
            {
                Kind = CactusKind.TeddyBearCholla, Id = "cholla", Name = "Teddy-bear cholla", Latin = "Cylindropuntia bigelovii",
                Blurb = "A dark trunk with whorls of short tubercled joints, so densely spined they glow silver-gold.",
                Steps = 10,
                Styles = new[] {
                    new CactusStyle { tub = new TubercleSpec{ m = 5, n = 8, amp = .3f, R = .05f }, col = C(.24f,.21f,.17f), cap = .6f, areole = .03f,
                        wool = new WoolSpec { size = .004f, col = C(.4f,.35f,.28f) },
                        spines = new SpineSpec { nr = 8, nc = 1, lr = .02f, lc = .026f, ang = 58, w = .0006f, col = C(.14f,.12f,.1f), tip = C(.28f,.25f,.2f) } },
                    new CactusStyle { tub = new TubercleSpec{ m = 5, n = 8, amp = .32f, R = .028f }, col = C(.42f,.5f,.3f), cap = .7f, neck = .55f, trop = .5f, areole = .03f,
                        wool = new WoolSpec { size = .003f, col = C(.95f,.9f,.75f) },
                        spines = new SpineSpec { nr = 10, nc = 1, lr = .021f, lc = .027f, ang = 55, w = .00055f, col = C(.94f,.9f,.72f), tip = C(1f,.98f,.88f) } } },
                Flower = Fl(.005f, 20, .008f, Ring(8, 0, .022f, .014f, 50, 12, C(.86f,.88f,.55f))),
                Axiom = c => List(M('Y', 0), M('T', 0)),
            };
            s.Rules['T'] = (m, c) =>
            {
                int i = (int)m[0];
                if (i >= 5) return List(M('Y', 1), M('Z'), M('J', 0));
                var o = List(M('F', .17f, .055f));
                if (i >= 2) { int n = 1 + (c.R() < .6 ? 1 : 0); for (int k = 0; k < n; k++) o.AddRange(Br(M('/', c.Rnd(0, 360)), M('&', c.Rnd(48, 78)), M('Y', 1), M('Z'), M('J', 0))); }
                o.Add(M('T', i + 1)); return o;
            };
            s.Rules['J'] = (m, c) =>
            {
                int d = (int)m[0];
                if (d >= 3) return c.K >= 8 && c.R() < .35 ? List(M('K', .9f)) : null;
                var o = List(M('Z'), M('F', .11f * c.Rnd(.8f, 1.25f), .03f));
                int n = 1 + (c.R() < .55 ? 1 : 0);
                for (int k = 0; k < n; k++) o.AddRange(Br(M('/', c.Rnd(0, 360)), M('&', c.Rnd(22, 55)), M('J', d + 1)));
                return o;
            };
            return s;
        }

        // 5 ───────────────────────────────────────────────────────────
        static CactusSpecies OrganPipe()
        {
            var s = new CactusSpecies
            {
                Kind = CactusKind.OrganPipe, Id = "organpipe", Name = "Organ pipe", Latin = "Stenocereus thurberi",
                Blurb = "Many columns from one base, leaning out then curving upward like organ pipes.",
                Steps = 16, Thicken = 5,
                Styles = new[] { new CactusStyle { ribs = 15, ribDepth = .14f, ribPow = .5f, col = C(.3f,.44f,.27f), tip = C(.44f,.55f,.33f), cork = new CorkSpec{ h = .35f, col = C(.4f,.36f,.3f) }, trop = .55f, cap = .8f, areole = .04f,
                    wool = new WoolSpec { size = .0055f, col = C(.34f,.26f,.2f) },
                    spines = new SpineSpec { nr = 9, nc = 2, lr = .012f, lc = .024f, ang = 74, w = .0008f, col = C(.22f,.19f,.17f), tip = C(.5f,.45f,.4f) } } },
                Flower = Fl(.01f, 40, .012f, Ring(8, 0, .035f, .022f, 55, 20, C(.95f,.86f,.94f), C(.75f,.5f,.7f)), Ring(8, .5f, .028f, .02f, 35, 10, C(.97f,.92f,.96f))),
                Axiom = c => List(M('Y', 0), M('O', 0)),
            };
            s.Rules['O'] = (m, c) => m[0] < 14
                ? List(Br(M('/', m[0] * 137.5f), M('&', 90), M('f', m[0] == 0 ? 0 : .05f + .011f * m[0]), M('^', 90), M('&', m[0] == 0 ? 2 : c.Rnd(12, 42)), M('C', 0, Mathf.Floor(c.Rnd(6, 13)))), M('O', m[0] + 1))
                : null;
            s.Rules['C'] = (m, c) => m[0] < m[1] ? List(M('F', .32f, .075f), M('C', m[0] + 1, m[1])) : (c.K >= 14 ? List(M('W', 2, 44)) : null);
            return s;
        }

        // 6 ───────────────────────────────────────────────────────────
        static CactusSpecies Mammillaria()
        {
            var s = new CactusSpecies
            {
                Kind = CactusKind.Pincushion, Id = "mammillaria", Name = "Pincushion", Latin = "Mammillaria spp.",
                Blurb = "Tubercles on (8,13) Fibonacci spirals instead of ribs; offsets form a clump, flowers ring the apex.",
                Steps = 10,
                Styles = new[] { new CactusStyle { tub = new TubercleSpec{ m = 8, n = 13, amp = .55f, R = .05f, pow = 2 }, col = C(.3f,.48f,.3f), cap = 1, areole = .02f,
                    wool = new WoolSpec { size = .0025f, col = C(.97f,.96f,.92f) },
                    spines = new SpineSpec { nr = 18, nc = 1, lr = .0075f, lc = .011f, ang = 84, w = .00032f, col = C(.97f,.97f,.94f), tip = C(1,1,1), cc = C(.55f,.22f,.16f), ctip = C(.75f,.4f,.3f) } } },
                Flower = Fl(.003f, 8, .004f, Ring(9, 0, .012f, .005f, 45, 15, C(.96f,.42f,.66f), C(.85f,.3f,.55f))),
                Axiom = c => List(M('Y', 0), M('H', 0)),
            };
            s.Rules['H'] = (m, c) =>
            {
                int g = (int)m[0]; float R0 = .05f * (1 - .15f * g);
                var o = new List<Module>();
                if (g < 2)
                {
                    int n = g == 0 ? 3 + Mathf.FloorToInt(c.Rnd(0, 3)) : 1 + Mathf.FloorToInt(c.Rnd(0, 2));
                    for (int k = 0; k < n; k++) o.AddRange(Br(M('/', c.Rnd(0, 360)), M('&', 90), M('f', R0 * c.Rnd(1.6f, 1.9f)), M('^', c.Rnd(62, 80)), M('D', Mathf.Floor(c.Rnd(1, 4)), g + 1)));
                }
                o.Add(M('G', R0, .085f * (1 - .12f * g), 4)); o.Add(M('V', 0));
                return o;
            };
            s.Rules['D'] = (m, c) => m[0] > 0 ? List(M('D', m[0] - 1, m[1])) : List(M('H', m[1]));
            s.Rules['V'] = (m, c) => m[0] < 5 ? List(M('V', m[0] + 1)) : List(M('W', 11, 52, .9f));
            return s;
        }

        // 7 ───────────────────────────────────────────────────────────
        static CactusSpecies Astrophytum()
        {
            var s = new CactusSpecies
            {
                Kind = CactusKind.BishopsCap, Id = "astrophytum", Name = "Bishop's cap", Latin = "Astrophytum myriostigma",
                Blurb = "A spineless five-ribbed star covered in white flecks, with woolly areoles on the crests.",
                Steps = 10,
                Styles = new[] { new CactusStyle { ribs = 5, ribDepth = .42f, ribPow = .3f, col = C(.38f,.46f,.36f), fleck = .16f, cap = 1, areole = .011f,
                    wool = new WoolSpec { size = .0042f, col = C(.86f,.8f,.7f) } } },
                Flower = Fl(.004f, 30, .007f, Ring(10, 0, .028f, .012f, 50, 15, C(.99f,.93f,.45f), C(.9f,.45f,.2f)), Ring(9, .5f, .022f, .01f, 30, 10, C(.99f,.95f,.55f))),
                Axiom = c => List(M('Y', 0), M('G', .075f, .09f, 9), M('V', 0)),
            };
            s.Rules['V'] = (m, c) => m[0] < 7 ? List(M('V', m[0] + 1)) : List(M('W', 1, 0, 1.1f));
            return s;
        }

        // 8 ───────────────────────────────────────────────────────────
        static CactusSpecies Schlumbergera()
        {
            var s = new CactusSpecies
            {
                Kind = CactusKind.ChristmasCactus, Id = "schlumbergera", Name = "Christmas cactus", Latin = "Schlumbergera truncata",
                Blurb = "Flat toothed segments that branch in twos and arch over, with tubular magenta flowers at the tips.",
                Steps = 9, Pot = new PotSpec { r = .12f, h = .14f, col = C(.72f,.4f,.26f) }, BaseY = .13f,
                Styles = new[] { new CactusStyle { col = C(.28f,.52f,.28f), young = C(.5f,.7f,.36f), teeth = true, trop = -1.4f, areole = 0 } },
                Flower = new FlowerSpec { r0 = .003f, stamens = 14, stLen = .03f, stCol = C(.98f,.9f,.95f), antCol = C(.95f,.9f,.5f), rings = new[] {
                    Ring(4, 0, .03f, .01f, 18, 70, C(.95f,.25f,.52f), C(.96f,.85f,.9f)), Ring(5, .5f, .045f, .011f, 10, 95, C(.92f,.2f,.48f), C(.97f,.9f,.93f)) } },
                Axiom = c => { var o = List(M('Y', 0)); for (int i = 0; i < 4; i++) o.AddRange(Br(M('/', i * 90 + c.Rnd(0, 40)), M('&', c.Rnd(28, 50)), M('S', 0))); return o; },
            };
            s.Rules['S'] = (m, c) =>
            {
                int d = (int)m[0];
                if (d >= 6) return c.K >= 7 ? List(M('K', 1)) : null;
                var o = List(M('P', .045f, .026f, .0035f, d + 1));
                if (c.R() < .45) { o.AddRange(Br(M('+', c.Rnd(20, 32)), M('S', d + 1))); o.AddRange(Br(M('-', c.Rnd(20, 32)), M('S', d + 1))); }
                else o.AddRange(Br(M('+', c.Rnd(-8, 8)), M('/', c.Rnd(-25, 25)), M('S', d + 1)));
                return o;
            };
            return s;
        }

        // 9 ───────────────────────────────────────────────────────────
        static CactusSpecies OldMan()
        {
            var s = new CactusSpecies
            {
                Kind = CactusKind.OldMan, Id = "oldman", Name = "Old man cactus", Latin = "Cephalocereus senilis",
                Blurb = "A pale column wrapped in long white hairs; branches only from the base, and only when old.",
                Steps = 14, Thicken = 4,
                Styles = new[] { new CactusStyle { ribs = 24, ribDepth = .06f, ribPow = .6f, col = C(.44f,.55f,.4f), cap = .9f, areole = .018f,
                    wool = new WoolSpec { size = .004f, col = C(.96f,.95f,.92f) },
                    hair = new HairSpec { n = 3, len = .09f, w = .0007f, col = C(.97f,.97f,.95f) },
                    spines = new SpineSpec { nr = 0, nc = 1, lr = 0, lc = .016f, ang = 60, w = .0006f, col = C(.9f,.82f,.5f), tip = C(.98f,.92f,.7f) } } },
                Flower = Fl(.006f, 25, .008f, Ring(9, 0, .025f, .012f, 45, 15, C(.96f,.8f,.86f))),
                Axiom = c => List(M('Y', 0), M('T', 0), M('O', 0)),
            };
            s.Rules['T'] = (m, c) => m[0] < 12 ? List(M('F', .14f, .085f), M('T', m[0] + 1)) : List(M('W', 3, 70, .9f));
            s.Rules['O'] = (m, c) =>
            {
                int i = (int)m[0]; if (i >= 12) return new List<Module>();
                var o = new List<Module>();
                if ((i == 6 || i == 9) && c.R() < .6) o.AddRange(Br(M('/', c.Rnd(0, 360)), M('&', c.Rnd(15, 25)), M('C', 0, Mathf.Floor(c.Rnd(3, 6)))));
                o.Add(M('O', i + 1)); return o;
            };
            s.Rules['C'] = (m, c) => m[0] < m[1] ? List(M('F', .14f, .075f), M('C', m[0] + 1, m[1])) : null;
            return s;
        }

        // 10 ──────────────────────────────────────────────────────────
        static CactusSpecies Hedgehog()
        {
            var s = new CactusSpecies
            {
                Kind = CactusKind.Hedgehog, Id = "hedgehog", Name = "Hedgehog cactus", Latin = "Echinocereus engelmannii",
                Blurb = "Clumps of short soft-ribbed stems with long straw-to-pink spines and huge magenta flowers.",
                Steps = 12, Thicken = 3,
                Styles = new[] { new CactusStyle { ribs = 11, ribDepth = .2f, ribPow = .45f, col = C(.33f,.5f,.3f), cap = .9f, trop = .8f, areole = .022f,
                    wool = new WoolSpec { size = .004f, col = C(.85f,.8f,.7f) },
                    spines = new SpineSpec { nr = 9, nc = 3, lr = .022f, lc = .045f, ang = 66, w = .0009f, col = C(.93f,.84f,.66f), tip = C(.99f,.96f,.9f), vary = .5f, down = .2f } } },
                Flower = new FlowerSpec { r0 = .008f, stamens = 50, stLen = .012f, stCol = C(.98f,.9f,.6f), antCol = C(.98f,.85f,.3f), rings = new[] {
                    Ring(10, 0, .045f, .022f, 48, 18, C(.86f,.18f,.56f), C(.6f,.1f,.3f)), Ring(9, .5f, .038f, .02f, 30, 12, C(.9f,.25f,.6f)) } },
                Axiom = c => List(M('Y', 0), M('O', 0)),
            };
            s.Rules['O'] = (m, c) => m[0] < 9
                ? List(Br(M('/', m[0] * 137.5f + c.Rnd(-20, 20)), M('&', m[0] == 0 ? 0 : c.Rnd(12, 40)), M('C', 0, Mathf.Floor(c.Rnd(3, 7)))), M('O', m[0] + 1))
                : null;
            s.Rules['C'] = (m, c) => m[0] < m[1] ? List(M('F', .055f, .038f), M('C', m[0] + 1, m[1])) : (c.K >= 9 ? List(M('W', 2, 62, 1)) : null);
            return s;
        }
    }
}
