using System;
using System.Collections.Generic;
using UnityEngine;
using static Realms.Cacti.L;

namespace Realms.Cacti
{
    /// <summary>
    /// A cactus genome: the numbers inside one generic L-system, laid out on four chromosomes
    /// (Habit, Surface, Armature, Flower). <see cref="ToSpecies"/> turns a genome into an ordinary
    /// <see cref="CactusSpecies"/> (axiom + productions + styles + flower), so it grows through the same
    /// derive → interpret → mesh pipeline. <see cref="Cross"/> breeds two genomes.
    /// Kept identical to the browser previewer (gene table, order, RNG use, CG1 encoding), so codes and seeds match.
    /// </summary>
    [Serializable]
    public sealed class CactusGenome
    {
        public string name;
        /// <summary>Gene values in double precision so codes and crosses are bit-identical with the browser previewer.</summary>
        public double[] genes;
        /// <summary>Per slot: −1 wild · 0..1 share from parent B · ≥2 mutated.</summary>
        public float[] origin;
        public string[] parents;

        // ───────────────────────── gene table ─────────────────────────
        public enum GT { F, I, B, E, C }
        public readonly struct Gene
        {
            public readonly string Name; public readonly int Chr; public readonly GT Type; public readonly double Min, Max;
            public Gene(string n, int c, GT t, float a, float b) { Name = n; Chr = c; Type = t; Min = D(a); Max = D(b); }
        }
        public readonly struct Slot
        {
            public readonly string Name, Gene; public readonly int Chr; public readonly GT Type; public readonly double Min, Max; public readonly bool Color;
            public Slot(string n, string g, int c, GT t, double a, double b, bool col) { Name = n; Gene = g; Chr = c; Type = t; Min = a; Max = b; Color = col; }
        }

        public static readonly string[] Chromosomes = { "Habit", "Surface", "Armature", "Flower", "Leaf" };
        public enum Form { Column, Globe, Pad, Rosette }
        static readonly int[][] TubPairs = { new[] { 3, 5 }, new[] { 5, 8 }, new[] { 8, 13 }, new[] { 13, 21 } };

        static Gene G_(string n, int c, GT t, float a, float b) => new Gene(n, c, t, a, b);
        /// <summary>float literal → the double the browser uses (shortest round-trip decimal, e.g. .6f → 0.6).</summary>
        static double D(float f) => double.Parse(f.ToString("R", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture);
        static double Clamp(double v, double a, double b) => v < a ? a : v > b ? b : v;
        static double JsRound(double v) => Math.Floor(v + .5);   // JavaScript Math.round
        public static readonly Gene[] Genes =
        {
            // Habit
            G_("form",0,GT.E,0,3), G_("steps",0,GT.I,6,26), G_("thicken",0,GT.F,2,7), G_("r0",0,GT.F,.4f,1), G_("trop",0,GT.F,-2,2),
            G_("shoots",0,GT.I,1,14), G_("shootDelay",0,GT.I,0,4), G_("shootP",0,GT.F,0,1), G_("shootLean",0,GT.F,0,45), G_("shootSpread",0,GT.F,0,.03f),
            G_("mainSegs",0,GT.I,2,22), G_("shootMin",0,GT.I,1,14), G_("shootMax",0,GT.I,1,16), G_("segLen",0,GT.F,.01f,.4f), G_("radius",0,GT.F,.002f,.35f),
            G_("armP",0,GT.F,0,1), G_("armWhorl",0,GT.I,1,3), G_("armStart",0,GT.F,0,1), G_("armEnd",0,GT.F,0,1), G_("maxArms",0,GT.I,0,10), G_("armPitch",0,GT.F,15,90),
            G_("armSegs",0,GT.I,1,12), G_("armLenR",0,GT.F,.3f,1.2f), G_("armR",0,GT.F,.3f,1), G_("armTrop",0,GT.F,-2,2), G_("neck",0,GT.F,0,1),
            G_("jointed",0,GT.B,0,1), G_("maxDepth",0,GT.I,1,4), G_("jointBranchP",0,GT.F,0,1), G_("jointAng",0,GT.F,10,60),
            G_("globeR",0,GT.F,.03f,.4f), G_("globeH",0,GT.F,.03f,.5f), G_("globeDur",0,GT.I,2,12), G_("pups",0,GT.I,0,6), G_("pupGen",0,GT.I,0,2),
            G_("pupScale",0,GT.F,.2f,1), G_("pupDelay",0,GT.I,1,8), G_("pupDist",0,GT.F,1,2.2f),
            G_("padLen",0,GT.F,.03f,.35f), G_("padRatio",0,GT.F,.4f,1), G_("padThick",0,GT.F,.002f,.025f), G_("kidP1",0,GT.F,0,1), G_("kidP2",0,GT.F,0,1),
            G_("padAng",0,GT.F,5,60), G_("padRoll",0,GT.F,0,40), G_("padGen",0,GT.I,2,8), G_("padBack",0,GT.F,0,.2f), G_("lignify",0,GT.B,0,1),
            G_("teeth",0,GT.B,0,1), G_("baseStems",0,GT.I,1,5), G_("basePitch",0,GT.F,0,60), G_("pot",0,GT.B,0,1),
            // Surface
            G_("ribs",1,GT.I,0,30), G_("ribDepth",1,GT.F,0,.45f), G_("ribPow",1,GT.F,.2f,.8f), G_("twist",1,GT.F,-3,3),
            G_("tubAmp",1,GT.F,0,.7f), G_("tubPair",1,GT.E,0,3), G_("tubPow",1,GT.F,1,3), G_("tubR",1,GT.F,.02f,.08f),
            G_("col",1,GT.C,0,1), G_("tip",1,GT.C,0,1), G_("tipAmt",1,GT.F,0,1), G_("cork",1,GT.C,0,1), G_("corkAmt",1,GT.F,0,1), G_("corkH",1,GT.F,0,1.2f),
            G_("trunkDark",1,GT.F,0,1), G_("fleck",1,GT.F,0,.4f), G_("areole",1,GT.F,.008f,.06f), G_("cap",1,GT.F,.5f,1.2f),
            // Armature
            G_("nr",2,GT.I,0,20), G_("nc",2,GT.I,0,5), G_("lr",2,GT.F,0,.04f), G_("lc",2,GT.F,0,.06f), G_("ang",2,GT.F,30,90), G_("sw",2,GT.F,.0003f,.0014f),
            G_("scol",2,GT.C,0,1), G_("stip",2,GT.C,0,1), G_("ccol",2,GT.C,0,1), G_("vary",2,GT.F,0,.6f), G_("down",2,GT.F,0,.4f),
            G_("woolSize",2,GT.F,0,.008f), G_("woolCol",2,GT.C,0,1), G_("hairN",2,GT.I,0,5), G_("hairLen",2,GT.F,0,.12f),
            // Flower
            G_("fcol",3,GT.C,0,1), G_("fbase",3,GT.C,0,1), G_("flen",3,GT.F,.008f,.07f), G_("fwid",3,GT.F,.004f,.045f), G_("fn",3,GT.I,4,14),
            G_("felev",3,GT.F,10,70), G_("fcurl",3,GT.F,0,100), G_("frings",3,GT.I,1,2), G_("stamens",3,GT.I,0,80), G_("stLen",3,GT.F,.003f,.035f),
            G_("crownN",3,GT.I,1,14), G_("crownLat",3,GT.F,0,75), G_("fstart",3,GT.F,.4f,1), G_("fscale",3,GT.F,.6f,1.4f), G_("fr0",3,GT.F,.002f,.02f),
            // ── appended in CG2 (CG1 codes stop here and decode with these at their defaults) ──
            // Surface extras: wavy ribs, living-stone tops, euphorbia bands, cephalium
            G_("ribWave",1,GT.F,0,1), G_("topFlat",1,GT.F,0,1), G_("fissure",1,GT.F,0,1), G_("mottle",1,GT.F,0,1), G_("mottleCol",1,GT.C,0,1),
            G_("bands",1,GT.F,0,1), G_("bandCol",1,GT.C,0,1), G_("ceph",1,GT.F,0,1), G_("cephCol",1,GT.C,0,1),
            // Leaf: rosettes, leafy stems, flower stalks
            G_("leafN",4,GT.I,0,160), G_("leafLen",4,GT.F,.005f,1.2f), G_("leafWid",4,GT.F,.002f,.25f), G_("leafThick",4,GT.F,.1f,1), G_("leafShape",4,GT.F,0,1),
            G_("leafSect",4,GT.F,0,1), G_("leafCurl",4,GT.F,-60,120), G_("leafOpen",4,GT.F,5,110), G_("leafInner",4,GT.F,0,90), G_("diverg",4,GT.F,80,180),
            G_("internode",4,GT.F,0,.06f), G_("leafCol",4,GT.C,0,1), G_("leafTip",4,GT.C,0,1), G_("leafTipAmt",4,GT.F,0,1), G_("bloom",4,GT.F,0,1),
            G_("edge",4,GT.F,0,1), G_("edgeCol",4,GT.C,0,1), G_("window",4,GT.F,0,1), G_("spots",4,GT.F,0,1), G_("spotCol",4,GT.C,0,1),
            G_("teethN",4,GT.I,0,30), G_("teethLen",4,GT.F,0,.02f), G_("teethCol",4,GT.C,0,1), G_("tipSpine",4,GT.F,0,.06f),
            G_("offsets",4,GT.I,0,10), G_("offScale",4,GT.F,.2f,1), G_("offDist",4,GT.F,.5f,2.5f), G_("offGen",4,GT.I,0,2),
            G_("stalkLen",4,GT.F,0,3), G_("stalkTilt",4,GT.F,0,80), G_("stalkBranch",4,GT.I,0,6), G_("stalkFl",4,GT.I,1,40), G_("stalkCol",4,GT.C,0,1),
            G_("tipRos",4,GT.B,0,1), G_("leafAlong",4,GT.I,0,6), G_("fork",4,GT.B,0,1),
        };
        /// <summary>CG1 codes hold the first 118 slots; these ranges were widened in CG2.</summary>
        const int LegacySlots = 118;
        static readonly Dictionary<string, double[]> LegacyRange = new Dictionary<string, double[]> { ["form"] = new[] { 0.0, 2 }, ["radius"] = new[] { .02, .35 }, ["segLen"] = new[] { .03, .4 } };

        public static readonly Slot[] Slots;
        public static readonly Dictionary<string, int> SlotOf = new Dictionary<string, int>();
        static readonly Dictionary<string, GT> TypeOf = new Dictionary<string, GT>();

        static CactusGenome()
        {
            var l = new List<Slot>();
            foreach (var g in Genes)
            {
                SlotOf[g.Name] = l.Count; TypeOf[g.Name] = g.Type;
                if (g.Type == GT.C) foreach (var ch in "rgb") l.Add(new Slot(g.Name + "." + ch, g.Name, g.Chr, GT.F, 0, 1, true));
                else l.Add(new Slot(g.Name, g.Name, g.Chr, g.Type, g.Min, g.Max, false));
            }
            Slots = l.ToArray();
        }

        // ───────────────────────── gene access ─────────────────────────
        public float F(string n) { int i = SlotOf[n]; var s = Slots[i]; return (float)Clamp(genes[i], s.Min, s.Max); }
        public int I(string n) { int i = SlotOf[n]; var s = Slots[i]; return (int)JsRound(Clamp(genes[i], s.Min, s.Max)); }
        public bool B(string n) => genes[SlotOf[n]] >= .5;
        public Color C(string n) { int i = SlotOf[n]; return new Color((float)Clamp(genes[i], 0, 1), (float)Clamp(genes[i + 1], 0, 1), (float)Clamp(genes[i + 2], 0, 1)); }
        public Form FormGene => (Form)I("form");

        // ───────────────────────── presets ─────────────────────────
        static Dictionary<string, object> Defaults() => new Dictionary<string, object>
        {
            ["form"] = 0f, ["steps"] = 14f, ["thicken"] = 4f, ["r0"] = .55f, ["trop"] = 0f, ["shoots"] = 1f, ["shootDelay"] = 0f, ["shootP"] = 1f, ["shootLean"] = 0f, ["shootSpread"] = 0f,
            ["mainSegs"] = 10f, ["shootMin"] = 3f, ["shootMax"] = 6f, ["segLen"] = .2f, ["radius"] = .08f, ["armP"] = 0f, ["armWhorl"] = 1f, ["armStart"] = .4f, ["armEnd"] = .9f, ["maxArms"] = 0f,
            ["armPitch"] = 60f, ["armSegs"] = 4f, ["armLenR"] = .8f, ["armR"] = .7f, ["armTrop"] = 1f, ["neck"] = 0f, ["jointed"] = 0f, ["maxDepth"] = 1f, ["jointBranchP"] = 0f, ["jointAng"] = 30f,
            ["globeR"] = .12f, ["globeH"] = .14f, ["globeDur"] = 6f, ["pups"] = 0f, ["pupGen"] = 0f, ["pupScale"] = .5f, ["pupDelay"] = 3f, ["pupDist"] = 1.6f,
            ["padLen"] = .2f, ["padRatio"] = .7f, ["padThick"] = .012f, ["kidP1"] = .5f, ["kidP2"] = .2f, ["padAng"] = 40f, ["padRoll"] = 20f, ["padGen"] = 4f, ["padBack"] = .08f,
            ["lignify"] = 0f, ["teeth"] = 0f, ["baseStems"] = 1f, ["basePitch"] = 0f, ["pot"] = 0f,
            ["ribs"] = 12f, ["ribDepth"] = .15f, ["ribPow"] = .5f, ["twist"] = 0f, ["tubAmp"] = 0f, ["tubPair"] = 1f, ["tubPow"] = 1.4f, ["tubR"] = .05f,
            ["col"] = V(.3f, .48f, .28f), ["tip"] = V(.42f, .58f, .3f), ["tipAmt"] = 0f, ["cork"] = V(.42f, .38f, .3f), ["corkAmt"] = 0f, ["corkH"] = .5f, ["trunkDark"] = 0f, ["fleck"] = 0f,
            ["areole"] = .03f, ["cap"] = .9f,
            ["nr"] = 8f, ["nc"] = 1f, ["lr"] = .02f, ["lc"] = .03f, ["ang"] = 65f, ["sw"] = .0009f, ["scol"] = V(.9f, .85f, .7f), ["stip"] = V(.98f, .95f, .88f), ["ccol"] = V(.9f, .85f, .7f),
            ["vary"] = 0f, ["down"] = .2f, ["woolSize"] = .004f, ["woolCol"] = V(.85f, .8f, .7f), ["hairN"] = 0f, ["hairLen"] = 0f,
            ["fcol"] = V(.95f, .4f, .6f), ["fbase"] = V(.8f, .3f, .5f), ["flen"] = .03f, ["fwid"] = .015f, ["fn"] = 9f, ["felev"] = 50f, ["fcurl"] = 15f, ["frings"] = 1f, ["stamens"] = 30f,
            ["stLen"] = .01f, ["crownN"] = 3f, ["crownLat"] = 45f, ["fstart"] = .8f, ["fscale"] = 1f, ["fr0"] = .006f,
            ["ribWave"] = 0f, ["topFlat"] = 0f, ["fissure"] = 0f, ["mottle"] = 0f, ["mottleCol"] = V(.75f, .7f, .6f), ["bands"] = 0f, ["bandCol"] = V(.5f, .4f, .45f), ["ceph"] = 0f, ["cephCol"] = V(.75f, .2f, .2f),
            ["leafN"] = 0f, ["leafLen"] = .15f, ["leafWid"] = .04f, ["leafThick"] = .35f, ["leafShape"] = .15f, ["leafSect"] = .2f, ["leafCurl"] = 10f, ["leafOpen"] = 55f, ["leafInner"] = 15f, ["diverg"] = 137.5f, ["internode"] = 0f,
            ["leafCol"] = V(.35f, .55f, .3f), ["leafTip"] = V(.7f, .3f, .25f), ["leafTipAmt"] = 0f, ["bloom"] = 0f, ["edge"] = 0f, ["edgeCol"] = V(.95f, .95f, .9f), ["window"] = 0f, ["spots"] = 0f, ["spotCol"] = V(.95f, .95f, .92f),
            ["teethN"] = 0f, ["teethLen"] = 0f, ["teethCol"] = V(.8f, .7f, .5f), ["tipSpine"] = 0f, ["offsets"] = 0f, ["offScale"] = .5f, ["offDist"] = 1.2f, ["offGen"] = 0f,
            ["stalkLen"] = 0f, ["stalkTilt"] = 10f, ["stalkBranch"] = 0f, ["stalkFl"] = 12f, ["stalkCol"] = V(.45f, .5f, .35f), ["tipRos"] = 0f, ["leafAlong"] = 0f, ["fork"] = 0f,
        };
        static float[] V(float r, float g, float b) => new[] { r, g, b };

        static CactusGenome FromDict(string name, Dictionary<string, object> o)
        {
            var d = Defaults(); foreach (var kv in o) d[kv.Key] = kv.Value;
            var g = new double[Slots.Length];
            foreach (var gene in Genes)
            {
                int i = SlotOf[gene.Name];
                if (gene.Type == GT.C) { var c = (float[])d[gene.Name]; g[i] = D(c[0]); g[i + 1] = D(c[1]); g[i + 2] = D(c[2]); }
                else g[i] = D(Convert.ToSingle(d[gene.Name]));
            }
            var o2 = new float[Slots.Length]; for (int k = 0; k < o2.Length; k++) o2[k] = -1;
            return new CactusGenome { name = name, genes = g, origin = o2 };
        }

        /// <summary>The wild genome for a species: the first ten are tuned to grow like their hand-written L-systems, the other twenty exist only as genomes.</summary>
        public static CactusGenome Preset(CactusKind k)
        {
            switch (k)
            {
                case CactusKind.Saguaro: return FromDict("Saguaro", new Dictionary<string, object> {
                    ["form"]=0f,["steps"]=24f,["thicken"]=6f,["r0"]=.6f,["mainSegs"]=20f,["shootMin"]=1f,["shootMax"]=1f,["segLen"]=.34f,["radius"]=.3f,
                    ["armP"]=.45f,["armStart"]=.45f,["armEnd"]=.85f,["maxArms"]=5f,["armPitch"]=80f,["armSegs"]=8f,["armLenR"]=.74f,["armR"]=.7f,["armTrop"]=1.6f,["neck"]=.62f,
                    ["ribs"]=18f,["ribDepth"]=.13f,["ribPow"]=.5f,["col"]=V(.27f,.43f,.24f),["tip"]=V(.42f,.58f,.3f),["tipAmt"]=1f,["cork"]=V(.42f,.38f,.3f),["corkAmt"]=1f,["corkH"]=.9f,["areole"]=.05f,["cap"]=.9f,
                    ["nr"]=8f,["nc"]=2f,["lr"]=.022f,["lc"]=.042f,["ang"]=72f,["sw"]=.0011f,["scol"]=V(.42f,.38f,.34f),["stip"]=V(.22f,.2f,.18f),["ccol"]=V(.42f,.38f,.34f),["down"]=.3f,["woolSize"]=.006f,["woolCol"]=V(.62f,.56f,.46f),
                    ["fcol"]=V(.99f,.98f,.93f),["fbase"]=V(.95f,.93f,.8f),["flen"]=.062f,["fwid"]=.042f,["fn"]=9f,["felev"]=62f,["fcurl"]=25f,["frings"]=2f,["stamens"]=70f,["stLen"]=.02f,["crownN"]=6f,["crownLat"]=38f,["fstart"]=.84f,["fr0"]=.016f });
                case CactusKind.GoldenBarrel: return FromDict("Golden barrel", new Dictionary<string, object> {
                    ["form"]=1f,["steps"]=14f,["globeR"]=.3f,["globeH"]=.32f,["globeDur"]=11f,["pups"]=2f,["pupGen"]=1f,["pupScale"]=.34f,["pupDelay"]=6f,["pupDist"]=1.15f,
                    ["ribs"]=26f,["ribDepth"]=.2f,["ribPow"]=.4f,["col"]=V(.24f,.46f,.22f),["cap"]=1f,["areole"]=.028f,
                    ["nr"]=9f,["nc"]=3f,["lr"]=.03f,["lc"]=.045f,["ang"]=62f,["sw"]=.0011f,["scol"]=V(.95f,.78f,.3f),["stip"]=V(.99f,.92f,.62f),["ccol"]=V(.95f,.78f,.3f),["down"]=.15f,["woolSize"]=.007f,["woolCol"]=V(.93f,.86f,.62f),
                    ["fcol"]=V(.99f,.85f,.22f),["fbase"]=V(.99f,.85f,.22f),["flen"]=.025f,["fwid"]=.007f,["fn"]=12f,["felev"]=38f,["fcurl"]=10f,["frings"]=1f,["stamens"]=20f,["stLen"]=.008f,["crownN"]=12f,["crownLat"]=16f,["fstart"]=.7f,["fscale"]=.9f,["fr0"]=.006f });
                case CactusKind.PricklyPear: return FromDict("Prickly pear", new Dictionary<string, object> {
                    ["form"]=2f,["steps"]=9f,["padLen"]=.26f,["padRatio"]=.74f,["padThick"]=.015f,["kidP1"]=.7f,["kidP2"]=.25f,["padAng"]=50f,["padRoll"]=30f,["padGen"]=5f,["padBack"]=.1f,["lignify"]=1f,
                    ["ribs"]=0f,["col"]=V(.36f,.54f,.32f),["areole"]=.032f,
                    ["nr"]=2f,["nc"]=1f,["lr"]=.022f,["lc"]=.035f,["ang"]=40f,["sw"]=.001f,["scol"]=V(.94f,.86f,.5f),["stip"]=V(.99f,.96f,.78f),["ccol"]=V(.94f,.86f,.5f),["down"]=.25f,["woolSize"]=.004f,["woolCol"]=V(.55f,.42f,.25f),
                    ["fcol"]=V(.99f,.88f,.25f),["fbase"]=V(.95f,.7f,.2f),["flen"]=.04f,["fwid"]=.03f,["fn"]=8f,["felev"]=55f,["fcurl"]=10f,["frings"]=2f,["stamens"]=70f,["stLen"]=.012f,["fstart"]=.78f,["fr0"]=.01f });
                case CactusKind.TeddyBearCholla: return FromDict("Teddy-bear cholla", new Dictionary<string, object> {
                    ["form"]=0f,["steps"]=10f,["mainSegs"]=5f,["shootMin"]=1f,["shootMax"]=1f,["segLen"]=.17f,["radius"]=.055f,
                    ["armP"]=.8f,["armWhorl"]=2f,["armStart"]=.4f,["armEnd"]=1f,["maxArms"]=8f,["armPitch"]=63f,["armSegs"]=1f,["armLenR"]=.65f,["armR"]=.55f,["armTrop"]=.5f,["neck"]=.55f,["jointed"]=1f,["maxDepth"]=4f,["jointBranchP"]=.55f,["jointAng"]=38f,
                    ["ribs"]=0f,["tubAmp"]=.32f,["tubPair"]=1f,["tubPow"]=1.4f,["tubR"]=.03f,["col"]=V(.42f,.5f,.3f),["trunkDark"]=.9f,["cap"]=.7f,["areole"]=.03f,
                    ["nr"]=10f,["nc"]=1f,["lr"]=.021f,["lc"]=.027f,["ang"]=55f,["sw"]=.00055f,["scol"]=V(.94f,.9f,.72f),["stip"]=V(1f,.98f,.88f),["ccol"]=V(.94f,.9f,.72f),["woolSize"]=.003f,["woolCol"]=V(.95f,.9f,.75f),
                    ["fcol"]=V(.86f,.88f,.55f),["fbase"]=V(.86f,.88f,.55f),["flen"]=.022f,["fwid"]=.014f,["fn"]=8f,["felev"]=50f,["fcurl"]=12f,["stamens"]=20f,["stLen"]=.008f,["crownN"]=2f,["fstart"]=.8f,["fscale"]=.9f,["fr0"]=.005f });
                case CactusKind.OrganPipe: return FromDict("Organ pipe", new Dictionary<string, object> {
                    ["form"]=0f,["steps"]=16f,["thicken"]=5f,["trop"]=.55f,["shoots"]=14f,["shootLean"]=42f,["shootSpread"]=.011f,["mainSegs"]=12f,["shootMin"]=6f,["shootMax"]=12f,["segLen"]=.32f,["radius"]=.075f,
                    ["ribs"]=15f,["ribDepth"]=.14f,["ribPow"]=.5f,["col"]=V(.3f,.44f,.27f),["tip"]=V(.44f,.55f,.33f),["tipAmt"]=1f,["cork"]=V(.4f,.36f,.3f),["corkAmt"]=1f,["corkH"]=.35f,["cap"]=.8f,["areole"]=.04f,
                    ["nr"]=9f,["nc"]=2f,["lr"]=.012f,["lc"]=.024f,["ang"]=74f,["sw"]=.0008f,["scol"]=V(.22f,.19f,.17f),["stip"]=V(.5f,.45f,.4f),["ccol"]=V(.22f,.19f,.17f),["down"]=0f,["woolSize"]=.0055f,["woolCol"]=V(.34f,.26f,.2f),
                    ["fcol"]=V(.95f,.86f,.94f),["fbase"]=V(.75f,.5f,.7f),["flen"]=.035f,["fwid"]=.022f,["fn"]=8f,["felev"]=55f,["fcurl"]=20f,["frings"]=2f,["stamens"]=40f,["crownN"]=2f,["crownLat"]=44f,["fstart"]=.88f,["fr0"]=.01f });
                case CactusKind.Pincushion: return FromDict("Pincushion", new Dictionary<string, object> {
                    ["form"]=1f,["steps"]=10f,["globeR"]=.05f,["globeH"]=.085f,["globeDur"]=4f,["pups"]=4f,["pupGen"]=2f,["pupScale"]=.85f,["pupDelay"]=3f,["pupDist"]=1.75f,
                    ["ribs"]=0f,["tubAmp"]=.55f,["tubPair"]=2f,["tubPow"]=2f,["tubR"]=.05f,["col"]=V(.3f,.48f,.3f),["cap"]=1f,["areole"]=.02f,
                    ["nr"]=18f,["nc"]=1f,["lr"]=.0075f,["lc"]=.011f,["ang"]=84f,["sw"]=.00032f,["scol"]=V(.97f,.97f,.94f),["stip"]=V(1f,1f,1f),["ccol"]=V(.55f,.22f,.16f),["down"]=0f,["woolSize"]=.0025f,["woolCol"]=V(.97f,.96f,.92f),
                    ["fcol"]=V(.96f,.42f,.66f),["fbase"]=V(.85f,.3f,.55f),["flen"]=.012f,["fwid"]=.005f,["fn"]=9f,["felev"]=45f,["fcurl"]=15f,["stamens"]=8f,["stLen"]=.004f,["crownN"]=11f,["crownLat"]=52f,["fstart"]=.6f,["fscale"]=.9f,["fr0"]=.003f });
                case CactusKind.BishopsCap: return FromDict("Bishop's cap", new Dictionary<string, object> {
                    ["form"]=1f,["steps"]=10f,["globeR"]=.075f,["globeH"]=.09f,["globeDur"]=9f,["pups"]=0f,
                    ["ribs"]=5f,["ribDepth"]=.42f,["ribPow"]=.3f,["col"]=V(.38f,.46f,.36f),["fleck"]=.16f,["cap"]=1f,["areole"]=.011f,
                    ["nr"]=0f,["nc"]=0f,["lr"]=0f,["lc"]=0f,["woolSize"]=.0042f,["woolCol"]=V(.86f,.8f,.7f),
                    ["fcol"]=V(.99f,.93f,.45f),["fbase"]=V(.9f,.45f,.2f),["flen"]=.028f,["fwid"]=.012f,["fn"]=10f,["felev"]=50f,["fcurl"]=15f,["frings"]=2f,["stamens"]=30f,["stLen"]=.007f,["crownN"]=1f,["crownLat"]=0f,["fstart"]=.8f,["fscale"]=1.1f,["fr0"]=.004f });
                case CactusKind.ChristmasCactus: return FromDict("Christmas cactus", new Dictionary<string, object> {
                    ["form"]=2f,["steps"]=9f,["trop"]=-1.4f,["padLen"]=.045f,["padRatio"]=.58f,["padThick"]=.0035f,["kidP1"]=.45f,["kidP2"]=0f,["padAng"]=32f,["padRoll"]=25f,["padGen"]=7f,["padBack"]=0f,
                    ["teeth"]=1f,["baseStems"]=4f,["basePitch"]=40f,["pot"]=1f,["ribs"]=0f,["col"]=V(.28f,.52f,.28f),["areole"]=.03f,["nr"]=0f,["nc"]=0f,["lr"]=0f,["lc"]=0f,["woolSize"]=0f,
                    ["fcol"]=V(.95f,.25f,.52f),["fbase"]=V(.96f,.85f,.9f),["flen"]=.045f,["fwid"]=.011f,["fn"]=5f,["felev"]=12f,["fcurl"]=90f,["frings"]=2f,["stamens"]=14f,["stLen"]=.03f,["fstart"]=.78f,["fr0"]=.003f });
                case CactusKind.OldMan: return FromDict("Old man cactus", new Dictionary<string, object> {
                    ["form"]=0f,["steps"]=14f,["thicken"]=4f,["shoots"]=3f,["shootDelay"]=3f,["shootP"]=.6f,["shootLean"]=22f,["shootSpread"]=.01f,["mainSegs"]=12f,["shootMin"]=3f,["shootMax"]=5f,["segLen"]=.18f,["radius"]=.085f,
                    ["ribs"]=24f,["ribDepth"]=.06f,["ribPow"]=.6f,["col"]=V(.44f,.55f,.4f),["cap"]=.9f,["areole"]=.018f,
                    ["nr"]=0f,["nc"]=1f,["lr"]=0f,["lc"]=.016f,["ang"]=60f,["sw"]=.0006f,["scol"]=V(.9f,.82f,.5f),["stip"]=V(.98f,.92f,.7f),["ccol"]=V(.9f,.82f,.5f),["down"]=0f,["woolSize"]=.004f,["woolCol"]=V(.96f,.95f,.92f),["hairN"]=3f,["hairLen"]=.09f,
                    ["fcol"]=V(.96f,.8f,.86f),["fbase"]=V(.96f,.8f,.86f),["flen"]=.025f,["fwid"]=.012f,["fn"]=9f,["felev"]=45f,["fcurl"]=15f,["stamens"]=25f,["stLen"]=.008f,["crownN"]=3f,["crownLat"]=70f,["fstart"]=.86f,["fscale"]=.9f,["fr0"]=.006f });
                case CactusKind.Hedgehog: return FromDict("Hedgehog cactus", new Dictionary<string, object> {
                    ["form"]=0f,["steps"]=12f,["thicken"]=3f,["trop"]=.8f,["shoots"]=9f,["shootLean"]=40f,["shootSpread"]=.006f,["mainSegs"]=5f,["shootMin"]=3f,["shootMax"]=6f,["segLen"]=.055f,["radius"]=.038f,
                    ["ribs"]=11f,["ribDepth"]=.2f,["ribPow"]=.45f,["col"]=V(.33f,.5f,.3f),["cap"]=.9f,["areole"]=.022f,
                    ["nr"]=9f,["nc"]=3f,["lr"]=.022f,["lc"]=.045f,["ang"]=66f,["sw"]=.0009f,["scol"]=V(.93f,.84f,.66f),["stip"]=V(.99f,.96f,.9f),["ccol"]=V(.93f,.84f,.66f),["vary"]=.5f,["down"]=.2f,["woolSize"]=.004f,["woolCol"]=V(.85f,.8f,.7f),
                    ["fcol"]=V(.86f,.18f,.56f),["fbase"]=V(.6f,.1f,.3f),["flen"]=.045f,["fwid"]=.022f,["fn"]=10f,["felev"]=48f,["fcurl"]=18f,["frings"]=2f,["stamens"]=50f,["stLen"]=.012f,["crownN"]=2f,["crownLat"]=62f,["fstart"]=.75f,["fr0"]=.008f });
                // ── 20 more, defined only as genomes (generated from the atlas's species20 table) ──
                case CactusKind.FishhookBarrel: return FromDict("Fishhook barrel", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=16.0f,["globeR"]=.2f,["globeH"]=.5f,["globeDur"]=12.0f,["pups"]=.0f,["ribs"]=24.0f,["ribDepth"]=.22f,["ribPow"]=.4f,["col"]=V(.3f,.45f,.26f),["cap"]=1.0f,["areole"]=.035f,["nr"]=14.0f,["nc"]=4.0f,["lr"]=.026f,["lc"]=.056f,["ang"]=60.0f,["sw"]=.0012f,["scol"]=V(.86f,.8f,.68f),["stip"]=V(.9f,.62f,.45f),["ccol"]=V(.66f,.22f,.12f),["down"]=.3f,["woolSize"]=.005f,["woolCol"]=V(.7f,.6f,.45f),["fcol"]=V(.96f,.45f,.16f),["fbase"]=V(.8f,.24f,.1f),["flen"]=.03f,["fwid"]=.01f,["fn"]=14.0f,["felev"]=40.0f,["fcurl"]=10.0f,["frings"]=2.0f,["stamens"]=30.0f,["stLen"]=.008f,["crownN"]=12.0f,["crownLat"]=14.0f,["fstart"]=.75f,["fr0"]=.008f });
                case CactusKind.SanPedro: return FromDict("San Pedro", new Dictionary<string, object> { ["form"]=.0f,["steps"]=20.0f,["thicken"]=4.0f,["trop"]=.5f,["shoots"]=6.0f,["shootDelay"]=1.0f,["shootP"]=.8f,["shootLean"]=18.0f,["shootSpread"]=.008f,["mainSegs"]=14.0f,["shootMin"]=6.0f,["shootMax"]=12.0f,["segLen"]=.22f,["radius"]=.08f,["ribs"]=7.0f,["ribDepth"]=.2f,["ribPow"]=.55f,["col"]=V(.3f,.5f,.38f),["cap"]=.9f,["areole"]=.045f,["nr"]=5.0f,["nc"]=1.0f,["lr"]=.008f,["lc"]=.012f,["ang"]=60.0f,["sw"]=.0008f,["scol"]=V(.45f,.35f,.25f),["stip"]=V(.65f,.55f,.4f),["ccol"]=V(.45f,.35f,.25f),["down"]=.1f,["woolSize"]=.005f,["woolCol"]=V(.85f,.82f,.75f),["fcol"]=V(.99f,.98f,.95f),["fbase"]=V(.9f,.9f,.8f),["flen"]=.07f,["fwid"]=.03f,["fn"]=12.0f,["felev"]=55.0f,["fcurl"]=25.0f,["frings"]=2.0f,["stamens"]=70.0f,["stLen"]=.022f,["crownN"]=2.0f,["crownLat"]=50.0f,["fstart"]=.85f,["fr0"]=.014f });
                case CactusKind.Cardon: return FromDict("Cardón", new Dictionary<string, object> { ["form"]=.0f,["steps"]=24.0f,["thicken"]=6.0f,["r0"]=.6f,["mainSegs"]=20.0f,["shootMin"]=1.0f,["shootMax"]=1.0f,["segLen"]=.32f,["radius"]=.32f,["armP"]=.6f,["armWhorl"]=1.0f,["armStart"]=.2f,["armEnd"]=.5f,["maxArms"]=6.0f,["armPitch"]=82.0f,["armSegs"]=10.0f,["armLenR"]=.8f,["armR"]=.72f,["armTrop"]=1.8f,["neck"]=.3f,["ribs"]=13.0f,["ribDepth"]=.16f,["ribPow"]=.5f,["col"]=V(.38f,.5f,.38f),["tip"]=V(.46f,.58f,.42f),["tipAmt"]=.5f,["cork"]=V(.45f,.42f,.36f),["corkAmt"]=1.0f,["corkH"]=.8f,["cap"]=.9f,["areole"]=.05f,["nr"]=8.0f,["nc"]=1.0f,["lr"]=.01f,["lc"]=.02f,["ang"]=70.0f,["sw"]=.001f,["scol"]=V(.35f,.32f,.3f),["stip"]=V(.15f,.14f,.13f),["ccol"]=V(.3f,.28f,.26f),["down"]=.2f,["woolSize"]=.007f,["woolCol"]=V(.55f,.5f,.45f),["fcol"]=V(.97f,.97f,.9f),["fbase"]=V(.85f,.88f,.7f),["flen"]=.05f,["fwid"]=.025f,["fn"]=10.0f,["felev"]=60.0f,["fcurl"]=20.0f,["frings"]=2.0f,["stamens"]=60.0f,["stLen"]=.015f,["crownN"]=5.0f,["crownLat"]=40.0f,["fstart"]=.85f,["fr0"]=.012f });
                case CactusKind.Peyote: return FromDict("Peyote", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=10.0f,["globeR"]=.045f,["globeH"]=.045f,["globeDur"]=8.0f,["pups"]=3.0f,["pupGen"]=1.0f,["pupScale"]=.7f,["pupDelay"]=4.0f,["pupDist"]=1.5f,["ribs"]=8.0f,["ribDepth"]=.3f,["ribPow"]=.5f,["col"]=V(.38f,.52f,.5f),["cap"]=1.1f,["areole"]=.015f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f,["woolSize"]=.006f,["woolCol"]=V(.92f,.88f,.8f),["fcol"]=V(.96f,.72f,.8f),["fbase"]=V(.85f,.5f,.6f),["flen"]=.012f,["fwid"]=.005f,["fn"]=12.0f,["felev"]=40.0f,["fcurl"]=10.0f,["frings"]=2.0f,["stamens"]=20.0f,["stLen"]=.005f,["crownN"]=1.0f,["crownLat"]=.0f,["fstart"]=.7f,["fr0"]=.004f });
                case CactusKind.SilverTorch: return FromDict("Silver torch", new Dictionary<string, object> { ["form"]=.0f,["steps"]=18.0f,["thicken"]=4.0f,["trop"]=.9f,["shoots"]=8.0f,["shootDelay"]=1.0f,["shootP"]=.75f,["shootLean"]=10.0f,["shootSpread"]=.008f,["mainSegs"]=14.0f,["shootMin"]=7.0f,["shootMax"]=13.0f,["segLen"]=.12f,["radius"]=.035f,["ribs"]=25.0f,["ribDepth"]=.05f,["ribPow"]=.5f,["col"]=V(.4f,.52f,.38f),["cap"]=.9f,["areole"]=.011f,["nr"]=20.0f,["nc"]=4.0f,["lr"]=.011f,["lc"]=.018f,["ang"]=85.0f,["sw"]=.0003f,["scol"]=V(.97f,.97f,.95f),["stip"]=V(1.0f,1.0f,1.0f),["ccol"]=V(.9f,.88f,.8f),["down"]=.0f,["woolSize"]=.0025f,["woolCol"]=V(.98f,.98f,.96f),["fcol"]=V(.86f,.1f,.16f),["fbase"]=V(.7f,.06f,.1f),["flen"]=.035f,["fwid"]=.006f,["fn"]=6.0f,["felev"]=10.0f,["fcurl"]=5.0f,["frings"]=1.0f,["stamens"]=10.0f,["stLen"]=.03f,["crownN"]=3.0f,["crownLat"]=60.0f,["fstart"]=.7f,["fr0"]=.004f });
                case CactusKind.Espostoa: return FromDict("Peruvian old lady", new Dictionary<string, object> { ["form"]=.0f,["steps"]=18.0f,["thicken"]=4.0f,["mainSegs"]=14.0f,["shootMin"]=1.0f,["shootMax"]=1.0f,["segLen"]=.2f,["radius"]=.07f,["armP"]=.4f,["armStart"]=.4f,["armEnd"]=.9f,["maxArms"]=4.0f,["armPitch"]=55.0f,["armSegs"]=5.0f,["armLenR"]=.8f,["armR"]=.8f,["armTrop"]=1.5f,["neck"]=.2f,["ribs"]=22.0f,["ribDepth"]=.06f,["ribPow"]=.6f,["col"]=V(.4f,.52f,.38f),["cap"]=.9f,["areole"]=.015f,["nr"]=12.0f,["nc"]=1.0f,["lr"]=.008f,["lc"]=.02f,["ang"]=70.0f,["sw"]=.0004f,["scol"]=V(.85f,.75f,.5f),["stip"]=V(.95f,.9f,.75f),["ccol"]=V(.8f,.6f,.3f),["down"]=.0f,["woolSize"]=.005f,["woolCol"]=V(.97f,.96f,.94f),["hairN"]=4.0f,["hairLen"]=.05f,["fcol"]=V(.97f,.9f,.9f),["fbase"]=V(.85f,.7f,.72f),["flen"]=.022f,["fwid"]=.01f,["fn"]=9.0f,["felev"]=45.0f,["fcurl"]=15.0f,["stamens"]=25.0f,["stLen"]=.008f,["crownN"]=2.0f,["crownLat"]=60.0f,["fstart"]=.85f,["fr0"]=.006f });
                case CactusKind.BlueCandle: return FromDict("Blue candle", new Dictionary<string, object> { ["form"]=.0f,["steps"]=18.0f,["thicken"]=4.0f,["mainSegs"]=8.0f,["shootMin"]=1.0f,["shootMax"]=1.0f,["segLen"]=.22f,["radius"]=.055f,["armP"]=.8f,["armWhorl"]=2.0f,["armStart"]=.4f,["armEnd"]=1.0f,["maxArms"]=10.0f,["armPitch"]=70.0f,["armSegs"]=6.0f,["armLenR"]=.9f,["armR"]=.9f,["armTrop"]=2.0f,["neck"]=.1f,["ribs"]=6.0f,["ribDepth"]=.28f,["ribPow"]=.45f,["col"]=V(.36f,.52f,.56f),["tip"]=V(.45f,.6f,.62f),["tipAmt"]=.5f,["cap"]=.9f,["areole"]=.035f,["nr"]=5.0f,["nc"]=1.0f,["lr"]=.006f,["lc"]=.014f,["ang"]=65.0f,["sw"]=.0007f,["scol"]=V(.3f,.25f,.2f),["stip"]=V(.15f,.12f,.1f),["ccol"]=V(.25f,.2f,.15f),["down"]=.1f,["woolSize"]=.004f,["woolCol"]=V(.75f,.7f,.62f),["fcol"]=V(.95f,.95f,.82f),["fbase"]=V(.8f,.85f,.6f),["flen"]=.014f,["fwid"]=.006f,["fn"]=7.0f,["felev"]=60.0f,["fcurl"]=25.0f,["stamens"]=15.0f,["stLen"]=.006f,["crownN"]=5.0f,["crownLat"]=55.0f,["fstart"]=.8f,["fr0"]=.004f });
                case CactusKind.ChinCactus: return FromDict("Chin cactus", new Dictionary<string, object> { ["fscale"]=.8f,["form"]=1.0f,["steps"]=10.0f,["globeR"]=.045f,["globeH"]=.06f,["globeDur"]=7.0f,["pups"]=1.0f,["pupGen"]=1.0f,["pupScale"]=.5f,["pupDelay"]=4.0f,["pupDist"]=1.4f,["ribs"]=9.0f,["ribDepth"]=.28f,["ribPow"]=.5f,["tubAmp"]=.18f,["tubPair"]=.0f,["tubPow"]=1.5f,["tubR"]=.04f,["col"]=V(.33f,.4f,.36f),["cap"]=1.0f,["areole"]=.02f,["nr"]=5.0f,["nc"]=.0f,["lr"]=.009f,["lc"]=.0f,["ang"]=30.0f,["sw"]=.0006f,["scol"]=V(.8f,.75f,.65f),["stip"]=V(.9f,.85f,.75f),["ccol"]=V(.8f,.75f,.65f),["down"]=.4f,["woolSize"]=.003f,["woolCol"]=V(.85f,.8f,.7f),["fcol"]=V(.75f,.1f,.22f),["fbase"]=V(.5f,.05f,.12f),["flen"]=.025f,["fwid"]=.01f,["fn"]=12.0f,["felev"]=40.0f,["fcurl"]=10.0f,["frings"]=2.0f,["stamens"]=30.0f,["stLen"]=.006f,["crownN"]=2.0f,["crownLat"]=25.0f,["fstart"]=.7f,["fr0"]=.006f });
                case CactusKind.Rebutia: return FromDict("Crown cactus", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=10.0f,["globeR"]=.025f,["globeH"]=.05f,["globeDur"]=5.0f,["pups"]=5.0f,["pupGen"]=2.0f,["pupScale"]=.9f,["pupDelay"]=3.0f,["pupDist"]=1.8f,["ribs"]=.0f,["tubAmp"]=.35f,["tubPair"]=2.0f,["tubPow"]=1.6f,["tubR"]=.03f,["col"]=V(.36f,.52f,.32f),["cap"]=1.0f,["areole"]=.012f,["nr"]=18.0f,["nc"]=.0f,["lr"]=.004f,["lc"]=.0f,["ang"]=80.0f,["sw"]=.0003f,["scol"]=V(.95f,.93f,.85f),["stip"]=V(1.0f,1.0f,.95f),["ccol"]=V(.95f,.93f,.85f),["down"]=.0f,["woolSize"]=.002f,["woolCol"]=V(.97f,.96f,.92f),["fcol"]=V(.98f,.35f,.12f),["fbase"]=V(.9f,.25f,.1f),["flen"]=.02f,["fwid"]=.007f,["fn"]=10.0f,["felev"]=35.0f,["fcurl"]=15.0f,["frings"]=2.0f,["stamens"]=15.0f,["stLen"]=.005f,["crownN"]=6.0f,["crownLat"]=72.0f,["fstart"]=.6f,["fscale"]=1.2f,["fr0"]=.004f });
                case CactusKind.BalloonCactus: return FromDict("Balloon cactus", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=12.0f,["globeR"]=.08f,["globeH"]=.1f,["globeDur"]=9.0f,["pups"]=3.0f,["pupGen"]=1.0f,["pupScale"]=.5f,["pupDelay"]=4.0f,["pupDist"]=1.5f,["ribs"]=14.0f,["ribDepth"]=.3f,["ribPow"]=.35f,["col"]=V(.38f,.55f,.55f),["cap"]=1.0f,["areole"]=.012f,["nr"]=14.0f,["nc"]=1.0f,["lr"]=.01f,["lc"]=.014f,["ang"]=70.0f,["sw"]=.0003f,["scol"]=V(.98f,.88f,.45f),["stip"]=V(1.0f,.96f,.7f),["ccol"]=V(.95f,.8f,.4f),["down"]=.2f,["woolSize"]=.003f,["woolCol"]=V(.98f,.97f,.9f),["fcol"]=V(.99f,.95f,.35f),["fbase"]=V(.95f,.8f,.2f),["flen"]=.03f,["fwid"]=.012f,["fn"]=12.0f,["felev"]=45.0f,["fcurl"]=10.0f,["frings"]=2.0f,["stamens"]=40.0f,["stLen"]=.007f,["crownN"]=3.0f,["crownLat"]=12.0f,["fstart"]=.75f,["fr0"]=.006f });
                case CactusKind.BunnyEars: return FromDict("Bunny ears", new Dictionary<string, object> { ["form"]=2.0f,["steps"]=9.0f,["padLen"]=.1f,["padRatio"]=.78f,["padThick"]=.009f,["kidP1"]=.85f,["kidP2"]=.1f,["padAng"]=45.0f,["padRoll"]=20.0f,["padGen"]=4.0f,["padBack"]=.06f,["lignify"]=.0f,["ribs"]=.0f,["col"]=V(.4f,.6f,.32f),["areole"]=.015f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f,["woolSize"]=.0045f,["woolCol"]=V(.98f,.9f,.55f),["fcol"]=V(.99f,.92f,.4f),["fbase"]=V(.9f,.85f,.3f),["flen"]=.025f,["fwid"]=.016f,["fn"]=8.0f,["felev"]=55.0f,["fcurl"]=10.0f,["frings"]=2.0f,["stamens"]=40.0f,["stLen"]=.008f,["fstart"]=.75f,["fr0"]=.007f });
                case CactusKind.SantaRita: return FromDict("Purple prickly pear", new Dictionary<string, object> { ["form"]=2.0f,["steps"]=9.0f,["padLen"]=.18f,["padRatio"]=.95f,["padThick"]=.012f,["kidP1"]=.6f,["kidP2"]=.2f,["padAng"]=45.0f,["padRoll"]=25.0f,["padGen"]=4.0f,["padBack"]=.1f,["lignify"]=1.0f,["ribs"]=.0f,["col"]=V(.48f,.38f,.52f),["areole"]=.035f,["nr"]=1.0f,["nc"]=.0f,["lr"]=.02f,["lc"]=.0f,["ang"]=40.0f,["sw"]=.0009f,["scol"]=V(.5f,.35f,.3f),["stip"]=V(.7f,.55f,.4f),["ccol"]=V(.5f,.35f,.3f),["down"]=.2f,["woolSize"]=.004f,["woolCol"]=V(.55f,.4f,.25f),["fcol"]=V(.99f,.88f,.3f),["fbase"]=V(.85f,.3f,.15f),["flen"]=.04f,["fwid"]=.03f,["fn"]=8.0f,["felev"]=55.0f,["fcurl"]=10.0f,["frings"]=2.0f,["stamens"]=70.0f,["stLen"]=.012f,["fstart"]=.78f,["fr0"]=.01f });
                case CactusKind.CaneCholla: return FromDict("Tree cholla", new Dictionary<string, object> { ["form"]=.0f,["steps"]=12.0f,["mainSegs"]=4.0f,["shootMin"]=1.0f,["shootMax"]=1.0f,["segLen"]=.2f,["radius"]=.04f,["armP"]=.7f,["armWhorl"]=1.0f,["armStart"]=.2f,["armEnd"]=1.0f,["maxArms"]=7.0f,["armPitch"]=62.0f,["armSegs"]=1.0f,["armLenR"]=.7f,["armR"]=.7f,["armTrop"]=.8f,["neck"]=.4f,["jointed"]=1.0f,["maxDepth"]=4.0f,["jointBranchP"]=.5f,["jointAng"]=42.0f,["ribs"]=.0f,["tubAmp"]=.45f,["tubPair"]=1.0f,["tubPow"]=2.2f,["tubR"]=.035f,["col"]=V(.38f,.5f,.3f),["trunkDark"]=.6f,["cap"]=.7f,["areole"]=.035f,["nr"]=6.0f,["nc"]=1.0f,["lr"]=.012f,["lc"]=.02f,["ang"]=55.0f,["sw"]=.0005f,["scol"]=V(.85f,.72f,.6f),["stip"]=V(.95f,.88f,.8f),["ccol"]=V(.85f,.72f,.6f),["down"]=.1f,["woolSize"]=.003f,["woolCol"]=V(.9f,.85f,.7f),["fcol"]=V(.82f,.15f,.5f),["fbase"]=V(.6f,.1f,.35f),["flen"]=.03f,["fwid"]=.018f,["fn"]=9.0f,["felev"]=55.0f,["fcurl"]=15.0f,["frings"]=2.0f,["stamens"]=50.0f,["stLen"]=.01f,["crownN"]=2.0f,["fstart"]=.78f,["fr0"]=.007f });
                case CactusKind.DragonFruit: return FromDict("Dragon fruit", new Dictionary<string, object> { ["fscale"]=1.4f,["form"]=.0f,["steps"]=18.0f,["thicken"]=3.0f,["trop"]=-.7f,["shoots"]=4.0f,["shootDelay"]=2.0f,["shootP"]=.8f,["shootLean"]=40.0f,["shootSpread"]=.02f,["mainSegs"]=3.0f,["shootMin"]=6.0f,["shootMax"]=9.0f,["segLen"]=.18f,["radius"]=.06f,["armSegs"]=3.0f,["armLenR"]=1.0f,["armR"]=.9f,["armTrop"]=-.5f,["jointed"]=1.0f,["maxDepth"]=2.0f,["jointBranchP"]=.4f,["jointAng"]=48.0f,["ribs"]=3.0f,["ribDepth"]=.45f,["ribPow"]=.8f,["col"]=V(.36f,.55f,.3f),["cap"]=.8f,["areole"]=.03f,["nr"]=3.0f,["nc"]=.0f,["lr"]=.004f,["lc"]=.0f,["ang"]=60.0f,["sw"]=.0006f,["scol"]=V(.4f,.35f,.3f),["stip"]=V(.25f,.22f,.2f),["ccol"]=V(.4f,.35f,.3f),["down"]=.0f,["woolSize"]=.003f,["woolCol"]=V(.6f,.55f,.45f),["fcol"]=V(.99f,.98f,.92f),["fbase"]=V(.85f,.88f,.6f),["flen"]=.07f,["fwid"]=.028f,["fn"]=12.0f,["felev"]=50.0f,["fcurl"]=35.0f,["frings"]=2.0f,["stamens"]=80.0f,["stLen"]=.025f,["crownN"]=1.0f,["crownLat"]=.0f,["fstart"]=.7f,["fr0"]=.016f });
                case CactusKind.QueenOfTheNight: return FromDict("Queen of the night", new Dictionary<string, object> { ["form"]=2.0f,["steps"]=10.0f,["trop"]=-.6f,["padLen"]=.3f,["padRatio"]=.4f,["padThick"]=.004f,["kidP1"]=.5f,["kidP2"]=.1f,["padAng"]=30.0f,["padRoll"]=15.0f,["padGen"]=4.0f,["padBack"]=.05f,["teeth"]=1.0f,["baseStems"]=3.0f,["basePitch"]=25.0f,["pot"]=1.0f,["ribs"]=.0f,["col"]=V(.32f,.55f,.28f),["areole"]=.03f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f,["woolSize"]=.0f,["fcol"]=V(.99f,.98f,.95f),["fbase"]=V(.85f,.6f,.5f),["flen"]=.07f,["fwid"]=.02f,["fn"]=12.0f,["felev"]=45.0f,["fcurl"]=30.0f,["frings"]=2.0f,["stamens"]=60.0f,["stLen"]=.03f,["fstart"]=.8f,["fscale"]=1.2f,["fr0"]=.01f });
                case CactusKind.RatTail: return FromDict("Rat-tail cactus", new Dictionary<string, object> { ["form"]=.0f,["steps"]=22.0f,["thicken"]=2.0f,["trop"]=-2.0f,["shoots"]=8.0f,["shootDelay"]=1.0f,["shootP"]=.9f,["shootLean"]=45.0f,["shootSpread"]=.01f,["mainSegs"]=2.0f,["shootMin"]=12.0f,["shootMax"]=16.0f,["segLen"]=.08f,["radius"]=.02f,["pot"]=1.0f,["ribs"]=10.0f,["ribDepth"]=.1f,["ribPow"]=.5f,["col"]=V(.4f,.55f,.32f),["cap"]=.8f,["areole"]=.012f,["nr"]=12.0f,["nc"]=2.0f,["lr"]=.005f,["lc"]=.007f,["ang"]=75.0f,["sw"]=.0003f,["scol"]=V(.85f,.6f,.35f),["stip"]=V(.95f,.8f,.55f),["ccol"]=V(.75f,.45f,.25f),["down"]=.0f,["woolSize"]=.0015f,["woolCol"]=V(.85f,.75f,.6f),["fcol"]=V(.9f,.15f,.45f),["fbase"]=V(.85f,.2f,.4f),["flen"]=.04f,["fwid"]=.009f,["fn"]=8.0f,["felev"]=20.0f,["fcurl"]=40.0f,["frings"]=2.0f,["stamens"]=20.0f,["stLen"]=.02f,["crownN"]=2.0f,["crownLat"]=60.0f,["fstart"]=.75f,["fr0"]=.004f });
                case CactusKind.SandDollar: return FromDict("Sand dollar", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=10.0f,["globeR"]=.07f,["globeH"]=.05f,["globeDur"]=9.0f,["pups"]=.0f,["ribs"]=8.0f,["ribDepth"]=.12f,["ribPow"]=.5f,["col"]=V(.3f,.4f,.33f),["fleck"]=.22f,["cap"]=1.2f,["areole"]=.02f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f,["woolSize"]=.006f,["woolCol"]=V(.95f,.92f,.85f),["fcol"]=V(.99f,.92f,.45f),["fbase"]=V(.85f,.25f,.15f),["flen"]=.03f,["fwid"]=.013f,["fn"]=12.0f,["felev"]=55.0f,["fcurl"]=10.0f,["frings"]=2.0f,["stamens"]=30.0f,["stLen"]=.006f,["crownN"]=1.0f,["crownLat"]=.0f,["fstart"]=.8f,["fscale"]=.9f,["fr0"]=.005f });
                case CactusKind.EasterLily: return FromDict("Easter lily cactus", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=12.0f,["globeR"]=.07f,["globeH"]=.11f,["globeDur"]=8.0f,["pups"]=4.0f,["pupGen"]=1.0f,["pupScale"]=.5f,["pupDelay"]=3.0f,["pupDist"]=1.5f,["ribs"]=13.0f,["ribDepth"]=.28f,["ribPow"]=.4f,["col"]=V(.3f,.46f,.28f),["cap"]=1.0f,["areole"]=.025f,["nr"]=10.0f,["nc"]=3.0f,["lr"]=.012f,["lc"]=.02f,["ang"]=65.0f,["sw"]=.0007f,["scol"]=V(.6f,.5f,.4f),["stip"]=V(.3f,.25f,.2f),["ccol"]=V(.4f,.32f,.25f),["down"]=.15f,["woolSize"]=.004f,["woolCol"]=V(.85f,.82f,.75f),["fcol"]=V(.98f,.85f,.9f),["fbase"]=V(.9f,.75f,.8f),["flen"]=.07f,["fwid"]=.02f,["fn"]=12.0f,["felev"]=30.0f,["fcurl"]=30.0f,["frings"]=2.0f,["stamens"]=50.0f,["stLen"]=.025f,["crownN"]=2.0f,["crownLat"]=40.0f,["fstart"]=.75f,["fscale"]=1.2f,["fr0"]=.006f });
                case CactusKind.Ladyfinger: return FromDict("Ladyfinger cactus", new Dictionary<string, object> { ["form"]=.0f,["steps"]=14.0f,["thicken"]=2.0f,["trop"]=.6f,["shoots"]=12.0f,["shootDelay"]=.0f,["shootP"]=.85f,["shootLean"]=45.0f,["shootSpread"]=.008f,["mainSegs"]=6.0f,["shootMin"]=3.0f,["shootMax"]=7.0f,["segLen"]=.03f,["radius"]=.02f,["ribs"]=.0f,["tubAmp"]=.35f,["tubPair"]=1.0f,["tubPow"]=1.5f,["tubR"]=.025f,["col"]=V(.36f,.5f,.3f),["cap"]=.9f,["areole"]=.012f,["nr"]=18.0f,["nc"]=.0f,["lr"]=.006f,["lc"]=.0f,["ang"]=85.0f,["sw"]=.0003f,["scol"]=V(.98f,.85f,.45f),["stip"]=V(1.0f,.95f,.7f),["ccol"]=V(.98f,.85f,.45f),["down"]=.0f,["woolSize"]=.0015f,["woolCol"]=V(.95f,.93f,.85f),["fcol"]=V(.97f,.92f,.7f),["fbase"]=V(.9f,.8f,.5f),["flen"]=.01f,["fwid"]=.004f,["fn"]=9.0f,["felev"]=45.0f,["fcurl"]=10.0f,["stamens"]=8.0f,["stLen"]=.004f,["crownN"]=3.0f,["crownLat"]=55.0f,["fstart"]=.7f,["fr0"]=.003f });
                case CactusKind.OldLady: return FromDict("Old lady cactus", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=11.0f,["globeR"]=.06f,["globeH"]=.1f,["globeDur"]=6.0f,["pups"]=2.0f,["pupGen"]=1.0f,["pupScale"]=.8f,["pupDelay"]=4.0f,["pupDist"]=1.7f,["ribs"]=.0f,["tubAmp"]=.45f,["tubPair"]=2.0f,["tubPow"]=1.8f,["tubR"]=.045f,["col"]=V(.32f,.48f,.32f),["cap"]=1.0f,["areole"]=.016f,["nr"]=20.0f,["nc"]=1.0f,["lr"]=.006f,["lc"]=.008f,["ang"]=85.0f,["sw"]=.0003f,["scol"]=V(.98f,.98f,.96f),["stip"]=V(1.0f,1.0f,1.0f),["ccol"]=V(.6f,.3f,.25f),["down"]=.0f,["woolSize"]=.004f,["woolCol"]=V(.99f,.99f,.97f),["hairN"]=2.0f,["hairLen"]=.03f,["fcol"]=V(.85f,.15f,.5f),["fbase"]=V(.65f,.1f,.35f),["flen"]=.012f,["fwid"]=.005f,["fn"]=10.0f,["felev"]=45.0f,["fcurl"]=15.0f,["stamens"]=8.0f,["stLen"]=.004f,["crownN"]=12.0f,["crownLat"]=45.0f,["fstart"]=.6f,["fscale"]=.9f,["fr0"]=.003f });
                // ── 22 succulents and oddities (rosettes, stones, trees and trailers) ──
                case CactusKind.AloeVera: return FromDict("Aloe vera", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=14.0f,["leafN"]=22.0f,["leafLen"]=.45f,["leafWid"]=.07f,["leafThick"]=.45f,["leafShape"]=.05f,["leafSect"]=.35f,["leafCurl"]=15.0f,["leafOpen"]=40.0f,["leafInner"]=8.0f,["leafCol"]=V(.45f,.58f,.45f),["bloom"]=.25f,["spots"]=.25f,["spotCol"]=V(.9f,.92f,.85f),["teethN"]=14.0f,["teethLen"]=.003f,["teethCol"]=V(.85f,.8f,.6f),["tipSpine"]=.002f,["offsets"]=3.0f,["offScale"]=.5f,["offDist"]=1.0f,["offGen"]=1.0f,["stalkLen"]=.8f,["stalkTilt"]=5.0f,["stalkBranch"]=1.0f,["stalkFl"]=30.0f,["stalkCol"]=V(.5f,.55f,.4f),["fcol"]=V(.98f,.82f,.22f),["fbase"]=V(.95f,.7f,.2f),["fn"]=6.0f,["flen"]=.022f,["fwid"]=.005f,["felev"]=12.0f,["fcurl"]=15.0f,["frings"]=1.0f,["stamens"]=6.0f,["stLen"]=.02f,["fr0"]=.003f,["fstart"]=.7f,["tipRos"]=1.0f,["col"]=V(.45f,.58f,.45f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=9.0f,["nc"]=1.0f,["lr"]=.0075f,["lc"]=.003f,["scol"]=V(.85f,.8f,.6f),["stip"]=V(.85f,.8f,.6f),["ccol"]=V(.85f,.8f,.6f) });
                case CactusKind.SpiralAloe: return FromDict("Spiral aloe", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=16.0f,["leafN"]=120.0f,["leafLen"]=.28f,["leafWid"]=.07f,["leafThick"]=.4f,["leafShape"]=.1f,["leafSect"]=.3f,["leafCurl"]=-10.0f,["leafOpen"]=60.0f,["leafInner"]=20.0f,["diverg"]=142.5f,["leafCol"]=V(.42f,.55f,.42f),["bloom"]=.3f,["leafTip"]=V(.42f,.3f,.25f),["leafTipAmt"]=.6f,["teethN"]=10.0f,["teethLen"]=.003f,["teethCol"]=V(.9f,.88f,.8f),["tipSpine"]=.004f,["stalkLen"]=.6f,["stalkTilt"]=10.0f,["stalkBranch"]=2.0f,["stalkFl"]=24.0f,["stalkCol"]=V(.5f,.45f,.35f),["fcol"]=V(.95f,.5f,.45f),["fbase"]=V(.9f,.4f,.38f),["fn"]=6.0f,["flen"]=.025f,["fwid"]=.006f,["felev"]=12.0f,["fcurl"]=15.0f,["frings"]=1.0f,["stamens"]=6.0f,["stLen"]=.02f,["fr0"]=.004f,["fstart"]=.75f,["tipRos"]=1.0f,["col"]=V(.42f,.55f,.42f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=7.0f,["nc"]=1.0f,["lr"]=.0075f,["lc"]=.006f,["scol"]=V(.9f,.88f,.8f),["stip"]=V(.9f,.88f,.8f),["ccol"]=V(.9f,.88f,.8f) });
                case CactusKind.CenturyPlant: return FromDict("Century plant", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=18.0f,["leafN"]=30.0f,["leafLen"]=1.1f,["leafWid"]=.2f,["leafThick"]=.25f,["leafShape"]=.05f,["leafSect"]=.3f,["leafCurl"]=35.0f,["leafOpen"]=55.0f,["leafInner"]=10.0f,["leafCol"]=V(.42f,.56f,.6f),["bloom"]=.3f,["teethN"]=12.0f,["teethLen"]=.012f,["teethCol"]=V(.35f,.25f,.18f),["tipSpine"]=.045f,["offsets"]=2.0f,["offScale"]=.3f,["offDist"]=1.0f,["offGen"]=1.0f,["stalkLen"]=3.0f,["stalkTilt"]=.0f,["stalkBranch"]=6.0f,["stalkFl"]=36.0f,["stalkCol"]=V(.45f,.5f,.32f),["fcol"]=V(.95f,.85f,.3f),["fbase"]=V(.7f,.75f,.3f),["fn"]=6.0f,["flen"]=.03f,["fwid"]=.008f,["felev"]=40.0f,["fcurl"]=10.0f,["frings"]=1.0f,["stamens"]=6.0f,["stLen"]=.035f,["fr0"]=.005f,["fstart"]=.8f,["tipRos"]=1.0f,["col"]=V(.42f,.56f,.6f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=8.0f,["nc"]=1.0f,["lr"]=.03f,["lc"]=.06f,["scol"]=V(.35f,.25f,.18f),["stip"]=V(.35f,.25f,.18f),["ccol"]=V(.35f,.25f,.18f) });
                case CactusKind.QueenVictoria: return FromDict("Queen Victoria agave", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=16.0f,["leafN"]=90.0f,["leafLen"]=.18f,["leafWid"]=.045f,["leafThick"]=.5f,["leafShape"]=.05f,["leafSect"]=.45f,["leafCurl"]=-15.0f,["leafOpen"]=55.0f,["leafInner"]=15.0f,["leafCol"]=V(.18f,.32f,.22f),["edge"]=.9f,["edgeCol"]=V(.97f,.97f,.95f),["tipSpine"]=.012f,["teethCol"]=V(.12f,.1f,.08f),["stalkLen"]=1.5f,["stalkTilt"]=.0f,["stalkFl"]=40.0f,["stalkCol"]=V(.4f,.4f,.3f),["fcol"]=V(.9f,.85f,.7f),["fbase"]=V(.55f,.3f,.4f),["fn"]=6.0f,["flen"]=.02f,["fwid"]=.006f,["felev"]=40.0f,["fcurl"]=10.0f,["frings"]=1.0f,["stamens"]=6.0f,["stLen"]=.03f,["fr0"]=.004f,["fstart"]=.8f,["tipRos"]=1.0f,["col"]=V(.18f,.32f,.22f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=2.0f,["nc"]=1.0f,["lr"]=.0f,["lc"]=.018000000000000002f,["scol"]=V(.12f,.1f,.08f),["stip"]=V(.12f,.1f,.08f),["ccol"]=V(.12f,.1f,.08f) });
                case CactusKind.Echeveria: return FromDict("Echeveria", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=14.0f,["leafN"]=40.0f,["leafLen"]=.06f,["leafWid"]=.035f,["leafThick"]=.35f,["leafShape"]=.5f,["leafSect"]=.25f,["leafCurl"]=10.0f,["leafOpen"]=70.0f,["leafInner"]=25.0f,["internode"]=.0008f,["leafCol"]=V(.65f,.55f,.65f),["bloom"]=.55f,["leafTip"]=V(.85f,.5f,.6f),["leafTipAmt"]=.6f,["tipSpine"]=.001f,["teethCol"]=V(.6f,.4f,.45f),["offsets"]=2.0f,["offScale"]=.5f,["offDist"]=1.1f,["offGen"]=1.0f,["stalkLen"]=.25f,["stalkTilt"]=45.0f,["stalkFl"]=10.0f,["stalkCol"]=V(.75f,.45f,.5f),["fcol"]=V(.98f,.5f,.4f),["fbase"]=V(.98f,.85f,.4f),["fn"]=5.0f,["flen"]=.012f,["fwid"]=.006f,["felev"]=20.0f,["fcurl"]=10.0f,["frings"]=1.0f,["stamens"]=10.0f,["stLen"]=.006f,["fr0"]=.003f,["fstart"]=.7f,["tipRos"]=1.0f,["col"]=V(.65f,.55f,.65f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=2.0f,["nc"]=1.0f,["lr"]=.0f,["lc"]=.0015f,["scol"]=V(.6f,.4f,.45f),["stip"]=V(.6f,.4f,.45f),["ccol"]=V(.6f,.4f,.45f) });
                case CactusKind.ZebraHaworthia: return FromDict("Zebra plant", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=14.0f,["leafN"]=40.0f,["leafLen"]=.07f,["leafWid"]=.016f,["leafThick"]=.55f,["leafShape"]=.05f,["leafSect"]=.5f,["leafCurl"]=-20.0f,["leafOpen"]=35.0f,["leafInner"]=8.0f,["leafCol"]=V(.15f,.3f,.18f),["spots"]=.9f,["spotCol"]=V(.96f,.96f,.94f),["offsets"]=5.0f,["offScale"]=.7f,["offDist"]=1.3f,["offGen"]=2.0f,["stalkLen"]=.3f,["stalkTilt"]=10.0f,["stalkFl"]=10.0f,["stalkCol"]=V(.5f,.5f,.35f),["fcol"]=V(.95f,.93f,.9f),["fbase"]=V(.6f,.65f,.5f),["fn"]=6.0f,["flen"]=.008f,["fwid"]=.003f,["felev"]=15.0f,["fcurl"]=30.0f,["frings"]=1.0f,["stamens"]=6.0f,["stLen"]=.005f,["fr0"]=.002f,["fstart"]=.7f,["tipRos"]=1.0f,["col"]=V(.15f,.3f,.18f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f });
                case CactusKind.WindowHaworthia: return FromDict("Window haworthia", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=12.0f,["leafN"]=18.0f,["leafLen"]=.035f,["leafWid"]=.016f,["leafThick"]=.9f,["leafShape"]=.45f,["leafSect"]=.8f,["leafCurl"]=-15.0f,["leafOpen"]=30.0f,["leafInner"]=10.0f,["leafCol"]=V(.4f,.6f,.4f),["window"]=.9f,["offsets"]=6.0f,["offScale"]=.8f,["offDist"]=1.0f,["offGen"]=2.0f,["stalkLen"]=.18f,["stalkTilt"]=10.0f,["stalkFl"]=8.0f,["stalkCol"]=V(.5f,.5f,.35f),["fcol"]=V(.95f,.93f,.9f),["fbase"]=V(.6f,.65f,.5f),["fn"]=6.0f,["flen"]=.008f,["fwid"]=.003f,["felev"]=15.0f,["fcurl"]=30.0f,["frings"]=1.0f,["stamens"]=6.0f,["stLen"]=.005f,["fr0"]=.002f,["fstart"]=.7f,["tipRos"]=1.0f,["col"]=V(.4f,.6f,.4f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f });
                case CactusKind.Gasteria: return FromDict("Ox tongue", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=14.0f,["leafN"]=16.0f,["leafLen"]=.14f,["leafWid"]=.04f,["leafThick"]=.4f,["leafShape"]=.25f,["leafSect"]=.35f,["leafCurl"]=5.0f,["leafOpen"]=55.0f,["leafInner"]=15.0f,["diverg"]=180.0f,["leafCol"]=V(.25f,.38f,.25f),["spots"]=.8f,["spotCol"]=V(.92f,.92f,.88f),["offsets"]=2.0f,["offScale"]=.5f,["offDist"]=.8f,["offGen"]=1.0f,["stalkLen"]=.45f,["stalkTilt"]=25.0f,["stalkFl"]=16.0f,["stalkCol"]=V(.55f,.45f,.35f),["fcol"]=V(.85f,.35f,.4f),["fbase"]=V(.55f,.7f,.45f),["fn"]=6.0f,["flen"]=.02f,["fwid"]=.006f,["felev"]=10.0f,["fcurl"]=20.0f,["frings"]=1.0f,["stamens"]=6.0f,["stLen"]=.01f,["fr0"]=.004f,["fstart"]=.7f,["tipRos"]=1.0f,["col"]=V(.25f,.38f,.25f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f });
                case CactusKind.HensAndChicks: return FromDict("Hens and chicks", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=14.0f,["leafN"]=50.0f,["leafLen"]=.045f,["leafWid"]=.016f,["leafThick"]=.35f,["leafShape"]=.2f,["leafSect"]=.3f,["leafCurl"]=-15.0f,["leafOpen"]=60.0f,["leafInner"]=15.0f,["leafCol"]=V(.4f,.55f,.35f),["leafTip"]=V(.7f,.2f,.25f),["leafTipAmt"]=.8f,["tipSpine"]=.0015f,["teethCol"]=V(.6f,.25f,.25f),["offsets"]=7.0f,["offScale"]=.5f,["offDist"]=1.4f,["offGen"]=2.0f,["stalkLen"]=.2f,["stalkTilt"]=.0f,["stalkFl"]=12.0f,["stalkCol"]=V(.55f,.45f,.4f),["fcol"]=V(.9f,.45f,.6f),["fbase"]=V(.8f,.3f,.5f),["fn"]=12.0f,["flen"]=.01f,["fwid"]=.003f,["felev"]=70.0f,["fcurl"]=.0f,["frings"]=1.0f,["stamens"]=10.0f,["stLen"]=.004f,["fr0"]=.003f,["fstart"]=.75f,["tipRos"]=1.0f,["col"]=V(.4f,.55f,.35f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=2.0f,["nc"]=1.0f,["lr"]=.0f,["lc"]=.0022500000000000003f,["scol"]=V(.6f,.25f,.25f),["stip"]=V(.6f,.25f,.25f),["ccol"]=V(.6f,.25f,.25f) });
                case CactusKind.BabyToes: return FromDict("Baby toes", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=12.0f,["leafN"]=12.0f,["leafLen"]=.03f,["leafWid"]=.009f,["leafThick"]=1.0f,["leafShape"]=.5f,["leafSect"]=1.0f,["leafCurl"]=.0f,["leafOpen"]=20.0f,["leafInner"]=5.0f,["leafCol"]=V(.45f,.6f,.42f),["window"]=1.0f,["offsets"]=6.0f,["offScale"]=.85f,["offDist"]=.9f,["offGen"]=2.0f,["stalkLen"]=.0f,["crownN"]=1.0f,["crownLat"]=.0f,["fcol"]=V(.98f,.98f,.96f),["fbase"]=V(.98f,.95f,.75f),["flen"]=.015f,["fwid"]=.0025f,["stLen"]=.004f,["fr0"]=.004f,["fstart"]=.7f,["fscale"]=.75f,["fn"]=14.0f,["frings"]=2.0f,["felev"]=70.0f,["fcurl"]=.0f,["stamens"]=30.0f,["tipRos"]=1.0f,["col"]=V(.45f,.6f,.42f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f });
                case CactusKind.LivingRock: return FromDict("Living rock cactus", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=12.0f,["leafN"]=22.0f,["leafLen"]=.045f,["leafWid"]=.03f,["leafThick"]=.7f,["leafShape"]=.05f,["leafSect"]=.6f,["leafCurl"]=-10.0f,["leafOpen"]=60.0f,["leafInner"]=35.0f,["leafCol"]=V(.6f,.62f,.52f),["spots"]=.35f,["spotCol"]=V(.85f,.82f,.72f),["stalkLen"]=.0f,["crownN"]=1.0f,["crownLat"]=.0f,["fcol"]=V(.95f,.6f,.75f),["fbase"]=V(.85f,.4f,.6f),["fn"]=12.0f,["frings"]=2.0f,["flen"]=.02f,["fwid"]=.005f,["felev"]=55.0f,["fcurl"]=10.0f,["stamens"]=30.0f,["stLen"]=.005f,["fr0"]=.004f,["fstart"]=.7f,["tipRos"]=1.0f,["col"]=V(.6f,.62f,.52f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f });
                case CactusKind.BuddhasTemple: return FromDict("Buddha's temple", new Dictionary<string, object> { ["form"]=3.0f,["steps"]=16.0f,["leafN"]=60.0f,["leafLen"]=.035f,["leafWid"]=.045f,["leafThick"]=.25f,["leafShape"]=.35f,["leafSect"]=.1f,["leafCurl"]=-10.0f,["leafOpen"]=88.0f,["leafInner"]=70.0f,["diverg"]=90.0f,["internode"]=.004f,["leafCol"]=V(.42f,.55f,.45f),["bloom"]=.35f,["edge"]=.4f,["edgeCol"]=V(.85f,.88f,.85f),["offsets"]=3.0f,["offScale"]=.8f,["offDist"]=.8f,["offGen"]=1.0f,["stalkLen"]=.0f,["crownN"]=4.0f,["crownLat"]=15.0f,["fcol"]=V(.95f,.75f,.75f),["fbase"]=V(.9f,.6f,.6f),["fn"]=5.0f,["flen"]=.008f,["fwid"]=.003f,["felev"]=60.0f,["fcurl"]=5.0f,["frings"]=1.0f,["stamens"]=5.0f,["stLen"]=.003f,["fr0"]=.002f,["fstart"]=.8f,["tipRos"]=1.0f,["col"]=V(.42f,.55f,.45f),["ribs"]=8.0f,["ribDepth"]=.2f,["woolSize"]=.0f,["areole"]=.03f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f });
                case CactusKind.LivingStones: return FromDict("Living stones", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=10.0f,["globeR"]=.018f,["globeH"]=.04f,["globeDur"]=6.0f,["pups"]=4.0f,["pupGen"]=1.0f,["pupScale"]=.9f,["pupDelay"]=3.0f,["pupDist"]=2.2f,["ribs"]=.0f,["col"]=V(.62f,.55f,.46f),["topFlat"]=.9f,["fissure"]=.9f,["mottle"]=.9f,["mottleCol"]=V(.45f,.37f,.32f),["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f,["woolSize"]=.0f,["areole"]=.03f,["fcol"]=V(.98f,.85f,.2f),["fbase"]=V(.98f,.95f,.85f),["flen"]=.012f,["fwid"]=.0022f,["stLen"]=.003f,["crownN"]=1.0f,["crownLat"]=.0f,["fstart"]=.75f,["fr0"]=.003f,["fscale"]=1.2f,["fn"]=14.0f,["frings"]=2.0f,["felev"]=70.0f,["fcurl"]=.0f,["stamens"]=30.0f });
                case CactusKind.SplitRock: return FromDict("Split rock", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=10.0f,["globeR"]=.035f,["globeH"]=.05f,["globeDur"]=7.0f,["pups"]=1.0f,["pupGen"]=1.0f,["pupScale"]=.8f,["pupDelay"]=4.0f,["pupDist"]=1.8f,["ribs"]=.0f,["col"]=V(.55f,.56f,.5f),["topFlat"]=.35f,["fissure"]=1.0f,["mottle"]=.55f,["mottleCol"]=V(.3f,.32f,.3f),["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f,["woolSize"]=.0f,["areole"]=.03f,["fcol"]=V(.98f,.6f,.35f),["fbase"]=V(.98f,.92f,.8f),["flen"]=.025f,["fwid"]=.004f,["stLen"]=.005f,["crownN"]=1.0f,["crownLat"]=.0f,["fstart"]=.75f,["fr0"]=.005f,["fscale"]=1.1f,["fn"]=14.0f,["frings"]=2.0f,["felev"]=70.0f,["fcurl"]=.0f,["stamens"]=30.0f });
                case CactusKind.BaseballPlant: return FromDict("Baseball plant", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=12.0f,["globeR"]=.045f,["globeH"]=.07f,["globeDur"]=8.0f,["pups"]=.0f,["ribs"]=8.0f,["ribDepth"]=.12f,["ribPow"]=.6f,["col"]=V(.5f,.58f,.48f),["bands"]=.7f,["bandCol"]=V(.45f,.32f,.38f),["areole"]=.012f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f,["woolSize"]=.0f,["fcol"]=V(.6f,.6f,.3f),["fbase"]=V(.5f,.5f,.25f),["fn"]=5.0f,["flen"]=.008f,["fwid"]=.004f,["felev"]=50.0f,["fcurl"]=.0f,["frings"]=1.0f,["stamens"]=.0f,["crownN"]=3.0f,["crownLat"]=5.0f,["fstart"]=.7f,["fr0"]=.002f });
                case CactusKind.TurksCap: return FromDict("Turk's cap", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=14.0f,["globeR"]=.1f,["globeH"]=.12f,["globeDur"]=10.0f,["pups"]=.0f,["ribs"]=12.0f,["ribDepth"]=.28f,["ribPow"]=.4f,["col"]=V(.3f,.5f,.38f),["areole"]=.03f,["nr"]=9.0f,["nc"]=1.0f,["lr"]=.02f,["lc"]=.025f,["ang"]=65.0f,["sw"]=.001f,["scol"]=V(.6f,.4f,.32f),["stip"]=V(.8f,.6f,.5f),["ccol"]=V(.45f,.25f,.2f),["down"]=.2f,["woolSize"]=.004f,["woolCol"]=V(.85f,.8f,.7f),["ceph"]=.5f,["cephCol"]=V(.75f,.15f,.12f),["fcol"]=V(.95f,.35f,.55f),["fbase"]=V(.8f,.2f,.4f),["fn"]=7.0f,["flen"]=.012f,["fwid"]=.004f,["felev"]=40.0f,["fcurl"]=10.0f,["frings"]=1.0f,["stamens"]=8.0f,["stLen"]=.004f,["crownN"]=6.0f,["crownLat"]=20.0f,["fstart"]=.7f,["fr0"]=.003f });
                case CactusKind.BrainCactus: return FromDict("Brain cactus", new Dictionary<string, object> { ["form"]=1.0f,["steps"]=12.0f,["globeR"]=.06f,["globeH"]=.07f,["globeDur"]=8.0f,["pups"]=.0f,["ribs"]=30.0f,["ribDepth"]=.35f,["ribPow"]=.3f,["ribWave"]=1.0f,["col"]=V(.35f,.5f,.35f),["areole"]=.035f,["nr"]=4.0f,["nc"]=2.0f,["lr"]=.006f,["lc"]=.018f,["ang"]=60.0f,["sw"]=.0008f,["scol"]=V(.85f,.8f,.7f),["stip"]=V(.95f,.9f,.8f),["ccol"]=V(.85f,.75f,.6f),["down"]=.1f,["woolSize"]=.002f,["woolCol"]=V(.9f,.88f,.8f),["fcol"]=V(.95f,.9f,.95f),["fbase"]=V(.6f,.3f,.55f),["fn"]=12.0f,["frings"]=2.0f,["flen"]=.015f,["fwid"]=.004f,["felev"]=50.0f,["fcurl"]=10.0f,["stamens"]=20.0f,["stLen"]=.005f,["crownN"]=4.0f,["crownLat"]=8.0f,["fstart"]=.75f,["fr0"]=.004f });
                case CactusKind.QuiverTree: return FromDict("Quiver tree", new Dictionary<string, object> { ["form"]=.0f,["steps"]=26.0f,["thicken"]=7.0f,["r0"]=.6f,["mainSegs"]=6.0f,["shootMin"]=1.0f,["shootMax"]=1.0f,["segLen"]=.35f,["radius"]=.2f,["fork"]=1.0f,["maxDepth"]=3.0f,["jointAng"]=32.0f,["armSegs"]=2.0f,["armLenR"]=.65f,["armR"]=.76f,["armTrop"]=.6f,["ribs"]=.0f,["col"]=V(.75f,.68f,.5f),["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f,["woolSize"]=.0f,["tipRos"]=1.0f,["leafN"]=28.0f,["leafLen"]=.45f,["leafWid"]=.07f,["leafThick"]=.35f,["leafShape"]=.05f,["leafSect"]=.35f,["leafCurl"]=20.0f,["leafOpen"]=50.0f,["leafInner"]=10.0f,["leafCol"]=V(.55f,.65f,.6f),["bloom"]=.3f,["teethN"]=10.0f,["teethLen"]=.002f,["teethCol"]=V(.8f,.7f,.5f),["stalkLen"]=.35f,["stalkTilt"]=5.0f,["stalkBranch"]=2.0f,["stalkFl"]=10.0f,["stalkCol"]=V(.5f,.5f,.35f),["fcol"]=V(.98f,.85f,.2f),["fbase"]=V(.95f,.75f,.2f),["fn"]=6.0f,["flen"]=.02f,["fwid"]=.005f,["felev"]=12.0f,["fcurl"]=15.0f,["frings"]=1.0f,["stamens"]=6.0f,["stLen"]=.02f,["fr0"]=.003f,["fstart"]=.75f });
                case CactusKind.BlackRose: return FromDict("Black rose", new Dictionary<string, object> { ["form"]=.0f,["steps"]=22.0f,["mainSegs"]=4.0f,["shootMin"]=1.0f,["shootMax"]=1.0f,["segLen"]=.08f,["radius"]=.015f,["fork"]=1.0f,["maxDepth"]=3.0f,["jointAng"]=30.0f,["armSegs"]=3.0f,["armLenR"]=1.0f,["armR"]=.9f,["armTrop"]=1.2f,["ribs"]=.0f,["col"]=V(.45f,.38f,.3f),["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f,["woolSize"]=.0f,["tipRos"]=1.0f,["leafN"]=50.0f,["leafLen"]=.1f,["leafWid"]=.045f,["leafThick"]=.2f,["leafShape"]=.45f,["leafSect"]=.1f,["leafCurl"]=-5.0f,["leafOpen"]=70.0f,["leafInner"]=25.0f,["leafCol"]=V(.18f,.05f,.1f),["edge"]=.25f,["edgeCol"]=V(.3f,.1f,.15f),["stalkLen"]=.35f,["stalkTilt"]=.0f,["stalkBranch"]=4.0f,["stalkFl"]=30.0f,["stalkCol"]=V(.4f,.45f,.25f),["fcol"]=V(.98f,.88f,.3f),["fbase"]=V(.95f,.8f,.25f),["fn"]=8.0f,["flen"]=.008f,["fwid"]=.003f,["felev"]=70.0f,["fcurl"]=.0f,["frings"]=1.0f,["stamens"]=10.0f,["stLen"]=.004f,["fr0"]=.002f,["fstart"]=.75f });
                case CactusKind.MadagascarPalm: return FromDict("Madagascar palm", new Dictionary<string, object> { ["form"]=.0f,["steps"]=18.0f,["thicken"]=5.0f,["r0"]=.7f,["mainSegs"]=10.0f,["shootMin"]=1.0f,["shootMax"]=1.0f,["segLen"]=.15f,["radius"]=.08f,["ribs"]=.0f,["tubAmp"]=.15f,["tubPair"]=1.0f,["tubR"]=.05f,["col"]=V(.6f,.6f,.52f),["areole"]=.03f,["nr"]=3.0f,["nc"]=.0f,["lr"]=.03f,["lc"]=.0f,["ang"]=60.0f,["sw"]=.0012f,["scol"]=V(.4f,.38f,.35f),["stip"]=V(.2f,.18f,.16f),["ccol"]=V(.4f,.38f,.35f),["woolSize"]=.0f,["tipRos"]=1.0f,["leafN"]=26.0f,["leafLen"]=.3f,["leafWid"]=.03f,["leafThick"]=.15f,["leafShape"]=.1f,["leafSect"]=.0f,["leafCurl"]=25.0f,["leafOpen"]=70.0f,["leafInner"]=20.0f,["leafCol"]=V(.2f,.42f,.2f),["stalkLen"]=.0f,["crownN"]=4.0f,["crownLat"]=30.0f,["fcol"]=V(.98f,.98f,.95f),["fbase"]=V(.95f,.9f,.6f),["fn"]=5.0f,["flen"]=.04f,["fwid"]=.02f,["felev"]=60.0f,["fcurl"]=15.0f,["frings"]=1.0f,["stamens"]=5.0f,["stLen"]=.006f,["fr0"]=.006f,["fstart"]=.8f });
                case CactusKind.StringOfPearls: return FromDict("String of pearls", new Dictionary<string, object> { ["form"]=.0f,["steps"]=18.0f,["thicken"]=2.0f,["trop"]=-2.0f,["mainSegs"]=2.0f,["shootMin"]=1.0f,["shootMax"]=1.0f,["segLen"]=.03f,["radius"]=.0025f,["pot"]=1.0f,["armP"]=1.0f,["armWhorl"]=3.0f,["armStart"]=.0f,["armEnd"]=1.0f,["maxArms"]=8.0f,["armPitch"]=80.0f,["armSegs"]=13.0f,["armLenR"]=1.0f,["armR"]=1.0f,["armTrop"]=-2.0f,["ribs"]=.0f,["col"]=V(.35f,.5f,.3f),["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f,["woolSize"]=.0f,["leafAlong"]=2.0f,["leafLen"]=.009f,["leafWid"]=.008f,["leafThick"]=1.0f,["leafShape"]=1.0f,["leafSect"]=1.0f,["leafCurl"]=.0f,["leafOpen"]=70.0f,["leafInner"]=40.0f,["leafCol"]=V(.38f,.6f,.32f),["window"]=.4f,["tipSpine"]=.002f,["teethCol"]=V(.35f,.5f,.3f),["fcol"]=V(.97f,.95f,.9f),["fbase"]=V(.9f,.85f,.8f),["fn"]=10.0f,["flen"]=.008f,["fwid"]=.002f,["felev"]=60.0f,["fcurl"]=5.0f,["frings"]=1.0f,["stamens"]=20.0f,["stLen"]=.006f,["crownN"]=1.0f,["crownLat"]=.0f,["fstart"]=.75f,["fr0"]=.003f });
                case CactusKind.MedusasHead: return FromDict("Medusa's head", new Dictionary<string, object> { ["form"]=.0f,["steps"]=16.0f,["thicken"]=3.0f,["mainSegs"]=4.0f,["shootMin"]=1.0f,["shootMax"]=1.0f,["segLen"]=.03f,["radius"]=.1f,["armP"]=1.0f,["armWhorl"]=3.0f,["armStart"]=.0f,["armEnd"]=1.0f,["maxArms"]=10.0f,["armPitch"]=72.0f,["armSegs"]=8.0f,["armLenR"]=1.2f,["armR"]=.25f,["armTrop"]=-.4f,["neck"]=.3f,["ribs"]=.0f,["tubAmp"]=.3f,["tubPair"]=1.0f,["tubPow"]=1.4f,["tubR"]=.03f,["col"]=V(.4f,.5f,.38f),["cap"]=.7f,["areole"]=.03f,["nr"]=.0f,["nc"]=.0f,["lr"]=.0f,["lc"]=.0f,["woolSize"]=.0f,["fcol"]=V(.95f,.95f,.85f),["fbase"]=V(.7f,.75f,.4f),["fn"]=6.0f,["flen"]=.008f,["fwid"]=.003f,["felev"]=50.0f,["fcurl"]=.0f,["frings"]=1.0f,["stamens"]=.0f,["crownN"]=2.0f,["crownLat"]=10.0f,["fstart"]=.7f,["fr0"]=.003f });
                default: throw new ArgumentOutOfRangeException(nameof(k));
            }
        }

        // ───────────────────────── genome → species ─────────────────────────
        static Color Lc(Color a, Color b, float t) => Color.Lerp(a, b, t);
        /// <summary>Round half up, like JavaScript's Math.round (Unity's RoundToInt rounds half to even), so seeds match the previewer.</summary>
        static int JsR(float v) => (int)Math.Floor(v + .5);
        double Fd(string n) { int i = SlotOf[n]; var s = Slots[i]; return Clamp(genes[i], s.Min, s.Max); }
        /// <summary>Pot sized to the plant: a Christmas cactus gets the classic 12 cm pot, bigger-padded hybrids a bigger one.</summary>
        static PotSpec PotFor(double padLen) => PotR(padLen * 2.7);
        static PotSpec PotR(double rr) { float r = (float)Clamp(rr, .12, .4); return new PotSpec { r = r, h = r * 1.17f, col = new Color(.72f, .4f, .26f) }; }
        /// <summary>Pads in a pot get one sized to the pad; hanging (downward-tropic) columns and globes get one sized to their clump.</summary>
        PotSpec PotOf(Form form)
        {
            if (!B("pot")) return null;
            if (form == Form.Pad) return PotFor(Fd("padLen"));
            if (Fd("trop") >= 0) return null;
            if (form == Form.Column) return PotR(Math.Max(Fd("shootSpread") * (3 + I("shoots")), 2.15 * Fd("radius") * Math.Sqrt(Math.Max(0, I("shoots") - 1))) + Fd("radius") * 2 + .02);
            return PotR(Fd("globeR") * (1 + (I("pups") > 0 && I("pupGen") > 0 ? Fd("pupDist") + Fd("pupScale") : 0)) + .02);
        }

        /// <summary>One generic grammar whose numbers come from this genome.</summary>
        public CactusSpecies ToSpecies()
        {
            var g = this; var form = FormGene; int steps = I("steps"); int fstep = JsR(F("fstart") * steps);
            var col = C("col"); float td = F("trunkDark"); var pair = TubPairs[I("tubPair")];
            SpineSpec spines = I("nr") + I("nc") > 0 && (F("lr") > .001f || F("lc") > .001f)
                ? new SpineSpec { nr = I("nr"), nc = I("nc"), lr = F("lr"), lc = F("lc"), ang = F("ang"), w = F("sw"), col = C("scol"), tip = C("stip"), cc = C("ccol"), ctip = C("stip"), vary = F("vary"), down = F("down") }
                : null;
            CactusStyle Surface()
            {
                return new CactusStyle
                {
                    ribs = I("ribs"), ribDepth = F("ribDepth"), ribPow = F("ribPow"), twist = F("twist"),
                    tub = F("tubAmp") > .04f ? new TubercleSpec { m = pair[0], n = pair[1], amp = F("tubAmp"), R = F("tubR"), pow = F("tubPow") } : null,
                    col = col, tip = F("tipAmt") > .05f ? Lc(col, C("tip"), F("tipAmt")) : (Color?)null,
                    cork = F("corkAmt") > .05f ? new CorkSpec { h = F("corkH") * F("corkAmt"), col = C("cork") } : null,
                    fleck = F("fleck") > .01f ? F("fleck") : 0, cap = F("cap"), areole = F("areole"),
                    wool = F("woolSize") > .0005f ? new WoolSpec { size = F("woolSize"), col = C("woolCol") } : null,
                    spines = spines,
                    hair = I("hairN") > 0 && F("hairLen") > .01f ? new HairSpec { n = I("hairN"), len = F("hairLen"), w = .0007f, col = new Color(.97f, .97f, .95f) } : null,
                    ribWave = F("ribWave") > .02f ? F("ribWave") : 0, topFlat = F("topFlat") > .02f ? F("topFlat") : 0, fissure = F("fissure") > .02f ? F("fissure") : 0,
                    mottle = F("mottle") > .02f ? F("mottle") : 0, mottleCol = C("mottleCol"), bands = F("bands") > .02f ? F("bands") : 0, bandCol = C("bandCol"),
                };
            }
            var trunk = Surface(); trunk.trop = F("trop");
            if (td > .05f)
            {
                trunk.col = Lc(col, new Color(.24f, .21f, .17f), td);
                if (spines != null)
                    trunk.spines = new SpineSpec { nr = spines.nr, nc = spines.nc, lr = spines.lr, lc = spines.lc, ang = spines.ang, w = spines.w, vary = spines.vary, down = spines.down,
                        col = Lc(spines.col, new Color(.14f, .12f, .1f), td), tip = Lc(spines.tip, new Color(.28f, .25f, .2f), td) };
            }
            var arm = Surface(); arm.trop = F("armTrop"); arm.neck = F("neck") > .05f ? F("neck") : 0;
            var padSty = Surface(); padSty.ribs = 0; padSty.tub = null; padSty.trop = F("trop");
            padSty.young = new Color(Mathf.Min(1, col.r * 1.25f + .08f), Mathf.Min(1, col.g * 1.25f + .08f), Mathf.Min(1, col.b * 1.25f + .08f));
            padSty.trunkCol = new Color(.47f, .42f, .33f);
            padSty.lignify = B("lignify") ? new Vector2(steps * .45f, steps * .9f) : (Vector2?)null;
            padSty.teeth = B("teeth"); padSty.areole = B("teeth") ? 0 : F("areole");

            // leaf, flower stalk and cephalium styles (indices 3, 2, 4 in every species)
            CactusStyle Plain(Color c, float trop, float areole) { var st = Surface(); st.ribs = 0; st.tub = null; st.tip = null; st.cork = null; st.fleck = 0; st.wool = null; st.spines = null; st.hair = null;
                st.ribWave = 0; st.topFlat = 0; st.fissure = 0; st.mottle = 0; st.bands = 0; st.col = c; st.trop = trop; st.areole = areole; return st; }
            var lc = Lc(C("leafCol"), new Color(.78f, .8f, .85f), F("bloom") * .55f);
            var leafSty = Plain(lc, 0, 0);
            leafSty.leaf = new LeafSpec { shape = F("leafShape"), sect = F("leafSect"), curl = F("leafCurl"), teethN = F("teethLen") > .0003f ? I("teethN") : 0, teethLen = F("teethLen"), teethCol = C("teethCol"),
                tipSpine = F("tipSpine"), edge = F("edge"), edgeCol = C("edgeCol"), window = F("window"), spots = F("spots"), spotCol = C("spotCol"), tipCol = C("leafTip"), tipAmt = F("leafTipAmt") };
            var stalkSty = Plain(C("stalkCol"), .9f, 0); stalkSty.cap = .6f;
            var cc = C("cephCol");
            var cephSty = Plain(cc * .8f, 0, .0065f); cephSty.ribs = 26; cephSty.ribDepth = .04f; cephSty.cap = 1; cephSty.col.a = 1;
            cephSty.wool = new WoolSpec { size = .0045f, col = Lc(cc, Color.white, .45f) };
            cephSty.spines = new SpineSpec { nr = 7, nc = 0, lr = .007f, lc = 0, ang = 75, w = .00035f, col = cc * .75f, tip = cc, vary = 0, down = 0 };

            var fc = C("fcol"); var fb = C("fbase");
            var rings = new List<PetalRing> { new PetalRing { n = I("fn"), off = 0, len = F("flen"), wid = F("fwid"), elev = F("felev"), curl = F("fcurl"), col = fc, baseCol = fb } };
            if (I("frings") > 1) rings.Add(new PetalRing { n = Mathf.Max(3, I("fn") - 1), off = .5f, len = F("flen") * .8f, wid = F("fwid") * .85f, elev = F("felev") * .62f, curl = F("fcurl") * .6f, col = Lc(fc, Color.white, .15f), baseCol = fb });
            // flowers grow out past the spines (up to 1.6×), but never dwarf the organ they sit on
            double spineL = spines != null ? Math.Max(Fd("lr"), Fd("lc")) : 0;
            double organR = form == Form.Column ? Fd("radius") * (I("maxArms") > 0 && Fd("armP") > .05 ? Fd("armR") : 1)
                          : form == Form.Globe ? Fd("globeR") * Math.Pow(Fd("pupScale"), I("pupGen") > 0 && I("pups") > 0 ? 1 : 0)
                          : form == Form.Pad ? Fd("padLen") * Fd("padRatio") * .5 : Fd("leafLen") * .4;
            float fs = (float)Math.Min(Math.Max(Fd("fscale"), Math.Min(spineL * .9 / Math.Max(.001, Fd("flen")), Fd("fscale") * 1.6)), Math.Max(.35, organR * 2.6 / (Fd("fr0") + Fd("flen"))));

            var spec = new CactusSpecies
            {
                Kind = (CactusKind)(-1), Id = "g:" + Encode(), Name = name, Latin = parents != null ? string.Join(" × ", parents) : "",
                Steps = steps, Dur = 1, Thicken = F("thicken"), R0 = F("r0"),
                Flower = new FlowerSpec { r0 = F("fr0"), stamens = I("stamens"), stLen = F("stLen"), rings = rings.ToArray() },
                Styles = form == Form.Pad ? new[] { padSty, padSty, stalkSty, leafSty, cephSty } : new[] { trunk, arm, stalkSty, leafSty, cephSty },
                Pot = PotOf(form),
            };
            spec.BaseY = spec.Pot != null ? spec.Pot.h - .01f : 0;

            // rosette apex A(i, g, flowered, scale, mature): a few leaves per step on the divergence angle, each unfolding from the
            // inner angle to the open one (younger leaves sit upright in the centre); once old enough it sends up a flower stalk
            int Nl = I("leafN"); float stalkL = F("stalkLen"); const int segsS = 9; int per = Mathf.Max(1, Mathf.CeilToInt(Nl / (float)Mathf.Max(2, JsR(steps * .55f))));
            int fstepR = Mathf.Max(1, Mathf.Min(JsR(F("fstart") * steps), steps - (stalkL > .01f ? 4 : 1)));   // a stalk needs 3 steps to grow and one to open
            spec.Rules['A'] = (m, c) =>
            {
                int i = (int)m[0], gg = (int)m[1]; float scA = m[3]; int N = Mathf.Max(1, JsR(Nl * (gg > 0 ? .75f : 1))); var o = new List<Module>();
                float fl = m[2];
                for (int q = 0; q < per && i + q < N; q++)
                {
                    int idx = i + q; float juv = m.P.Length > 4 && m[4] != 0 ? 1 : Mathf.Lerp(.5f, 1, Mathf.Min(1, idx / (N * .35f + 1)));   // a seedling's first leaves are small
                    o.AddRange(Br(M('Y', 3), M('/', idx * F("diverg")), M('L', F("leafLen") * scA * juv, F("leafWid") * scA * juv, F("leafThick"), F("leafOpen"), F("leafInner"), 2.5f, F("leafWid") * scA * .25f)));
                    if (F("internode") > 0) o.Add(M('f', F("internode") * scA));
                }
                if (fl == 0 && c.K >= fstepR && scA >= .6f && (gg == 0 || c.R() < .5))   // small offsets don't flower
                {
                    fl = 1;
                    if (stalkL > .01f) o.AddRange(Br(M('Y', 2), M('/', c.Rnd(0, 360)), M('&', F("stalkTilt")), M('B', 0, scA, 0)));
                    else { int n = I("crownN"); for (int q = 0; q < n; q++) o.AddRange(Br(M('/', q * 360f / n + c.Rnd(-10, 10)), M('&', n > 1 ? F("crownLat") : 0), M('f', F("leafWid") * scA * .2f), M('K', fs))); }
                }
                if (i + per >= N && fl != 0) return o;
                o.Add(M('A', i + per, gg, fl, scA, m.P.Length > 4 ? m[4] : 0));
                return o;
            };
            // flower stalk B(j, scale, lateral): three segments a step; flowers along the upper part (pendant when tubular), side branches
            float pend = F("felev") < 25 ? 105 : 70;
            spec.Rules['B'] = (m, c) =>
            {
                int j = (int)m[0]; float scB = m[1]; bool lat = m[2] != 0; int segs = lat ? 4 : segsS;
                float seg = stalkL * scB / segsS * (lat ? .55f : 1), sr = Mathf.Max(.0015f, stalkL * scB * .012f * (lat ? .6f : 1));
                if (j >= segs) return new List<Module>();
                var o = new List<Module>(); int num = I("stalkFl") * (lat ? 1 : 5), den = lat ? 2 * segs : 3 * segs, nfl = Mathf.Max(1, (num + den - 1) / den);   // integer maths: identical in JS
                for (int q = 0; q < 3 && j + q < segs; q++)
                {
                    int jj = j + q; o.Add(M('F', seg, sr));
                    if (lat || jj >= segs * .4f) for (int k = 0; k < nfl; k++) o.AddRange(Br(M('/', c.Rnd(0, 360)), M('&', pend + c.Rnd(-12, 12)), M('f', sr * 1.5f), M('K', fs * .8f)));
                    if (!lat && I("stalkBranch") > 0 && jj >= segs * .3f && jj < segs * .3f + I("stalkBranch")) o.AddRange(Br(M('/', jj * 137.5f), M('&', 40), M('B', 0, scB, 1)));
                    if (jj == segs - 1) o.Add(M('K', fs * .8f));
                }
                o.Add(M('B', j + 3, scB, lat ? 1 : 0));
                return o;
            };

            if (form == Form.Column)
            {
                int shoots = I("shoots"), delay = I("shootDelay"), main = I("mainSegs"), sMin = I("shootMin"), sMax = Mathf.Max(sMin, I("shootMax"));
                float segLen = F("segLen"), rad = F("radius"); int maxArms = I("maxArms"), maxDepth = I("maxDepth"); bool jointed = B("jointed"), fork = B("fork") && !jointed;
                // segments a stem started at step k can still grow (one per step) and still flower; never cut a stem below 3
                // jointed plants fork from below the tip, far enough back that the new joint clears the tip flower
                double segA = Fd("segLen") * Fd("armLenR"), need = Math.Max((Fd("fr0") + Fd("flen")) * fs * .8, Fd("radius") * Fd("armR") * 1.8);
                float jointBack = (float)Math.Min(segA * .6, Math.Max(segA * .3, need));
                int Fit(int L, int k) => Math.Max(Math.Min(L, steps - k - 3), Math.Min(L, 3));
                spec.Axiom = c => List(M('Y', 0), M('O', 0, 0));
                spec.Rules['O'] = (m, c) =>
                {
                    int n = (int)m[0], t = (int)m[1];
                    if (n >= shoots) return new List<Module>();
                    if (t < delay && n > 0) return List(M('O', n, t + 1));
                    var o = new List<Module>();
                    if (n == 0 || c.R() < F("shootP"))
                    {
                        float Lc_ = Fit(n == 0 ? main : (int)Mathf.Floor(c.Rnd(sMin, sMax + 1)), c.K);
                        // basal shoots sit on a Vogel spiral (golden angle, distance ∝ √n) scaled by stem radius, so they never grow through each other
                        o.AddRange(Br(M('/', n * 137.5f + c.Rnd(-3, 3)), M('&', 90), M('f', n == 0 ? 0 : Mathf.Max(F("shootSpread") * (4 + n), 2.15f * F("radius") * Mathf.Sqrt(n))), M('^', 90),
                            M('&', n == 0 ? 0 : F("shootLean") * Mathf.Sqrt(n / (float)Math.Max(1, shoots - 1)) * c.Rnd(.85f, 1)), M('C', 0, Lc_, 0)));   // outer shoots lean more: the clump fans out
                    }
                    o.Add(M('O', n + 1, 0));
                    return o;
                };
                spec.Rules['C'] = (m, c) =>
                {
                    int j = (int)m[0], Ln = (int)m[1], d = (int)m[2];
                    if (j >= Ln)
                    {
                        var e = new List<Module>();
                        if ((d >= 1 || jointed || fork) && d < maxDepth)
                        {
                            double r0n = c.R(); int n = fork ? 2 : 1 + (r0n < F("jointBranchP") ? 1 : 0); float az0 = fork ? c.Rnd(0, 360) : 0;   // forking trees split in two, opposite ways
                            for (int k = 0; k < n; k++)
                            {
                                var br = new List<Module>();
                                if (jointed) br.Add(M('f', -jointBack));
                                br.Add(M('/', fork ? az0 + 180 * k : c.Rnd(0, 360))); br.Add(M('&', Mathf.Max(20, c.Rnd(F("jointAng") * .55f, F("jointAng") * 1.4f))));   // a new joint leaves at ≥20° so the tip flower stays clear
                                br.Add(M('C', 0, Fit(Mathf.Max(1, JsR(I("armSegs") * c.Rnd(.75f, 1.3f))), c.K), d + 1));
                                e.AddRange(Br(br.ToArray()));
                            }
                        }
                        // a tip either forks or flowers (a crown under new branches would be buried); jointed plants fork from
                        // just below the tip, out to the side, so their terminal crown stays clear
                        if (e.Count == 0 || jointed) e.Add(B("tipRos") ? M('A', 0, 0, 0, Mathf.Pow(.85f, d), 1) : M('E', d));   // leafy tips end in a rosette
                        return e;
                    }
                    var o = new List<Module>();
                    if (jointed && d > 0) o.Add(M('Z'));
                    float segL = d == 0 ? segLen : (j == 0 && d == 1 && !jointed ? Mathf.Max(segLen * F("armLenR"), (rad + rad * F("armR")) * 1.3f) : segLen * F("armLenR"));   // first arm segment clears the trunk
                    o.Add(M('F', segL, d == 0 ? rad : rad * (fork ? Mathf.Pow(F("armR"), d) : F("armR"))));
                    float rr = d == 0 ? rad : rad * F("armR");
                    if (B("tipRos"))   // a growing leafy tip carries its rosette with it: each step's leaves live 3.5 steps, then drop
                    {
                        int nT = Mathf.Max(2, Mathf.CeilToInt(I("leafN") / 3f)); float scT = Mathf.Pow(.85f, d);
                        for (int q = 0; q < nT; q++) o.AddRange(Br(M('Y', 3), M('/', (j * nT + q) * F("diverg")), M('L', F("leafLen") * scT, F("leafWid") * scT, F("leafThick"), F("leafOpen"), F("leafInner"), 1.2f, rr * .3f, 3.5f)));
                    }
                    for (int q = 0; q < I("leafAlong"); q++) o.AddRange(Br(M('Y', 3), M('/', j * 137.5f + q * 360f / I("leafAlong") + c.Rnd(-15, 15)), M('L', F("leafLen"), F("leafWid"), F("leafThick"), F("leafOpen"), F("leafInner"), 2, rr * .8f)));
                    c.State.TryGetValue("arms", out int arms);
                    for (int w = 0; w < I("armWhorl"); w++)
                        if (d == 0 && arms < maxArms && j >= F("armStart") * Ln && j <= F("armEnd") * Ln && c.R() < F("armP"))
                        {
                            arms++; c.State["arms"] = arms;
                            o.AddRange(Br(M('/', j * 137.5f + w * 160 + c.Rnd(-20, 20)), M('&', F("armPitch") + c.Rnd(-8, 8)), M('Y', 1), M('C', 0, Fit(Mathf.Max(1, JsR(I("armSegs") * c.Rnd(.75f, 1.3f))), c.K), 1)));
                        }
                    o.Add(M('C', j + 1, Ln, d));
                    return o;
                };
                spec.Rules['E'] = (m, c) =>
                {
                    if (c.K < fstep) return null;
                    int d = (int)m[0];
                    if (jointed && d > 0) return c.R() < .35 ? List(M('W', 1, 0, fs * .9f)) : new List<Module>();
                    return List(M('W', d == 0 ? I("crownN") : Mathf.Max(1, JsR(I("crownN") / 2f)), F("crownLat"), fs));
                };
            }
            else if (form == Form.Globe)
            {
                int pups = I("pups"), pupGen = I("pupGen"); float sc = F("pupScale");
                int wait = Mathf.Max(2, JsR(steps * (1 - F("fstart"))));
                spec.Axiom = c => List(M('Y', 0), M('H', 0));
                spec.Rules['H'] = (m, c) =>
                {
                    int gg = (int)m[0]; float s = Mathf.Pow(sc, gg), R0 = F("globeR") * s; var o = new List<Module>();
                    if (gg < pupGen && pups > 0)
                    {
                        int n = gg == 0 ? pups : Mathf.Max(1, pups - 2);
                        // pups: evenly spread around the mother (first generation) or fanned outward away from the clump (later ones),
                        // and far enough out that the pup's body clears the mother's ($ levels the turtle, keeping 'outward')
                        float clear = (1 + sc) * 1.08f;
                        for (int k = 0; k < n; k++)
                        {
                            float az = gg == 0 ? k * 360f / n + c.Rnd(-15, 15) : c.Rnd(-65, 65);
                            o.AddRange(Br(M('$'), M('/', az), M('&', 90), M('f', R0 * Mathf.Max(c.Rnd(F("pupDist") * .92f, F("pupDist") * 1.08f), clear)), M('^', c.Rnd(70, 86)), M('D', Mathf.Floor(c.Rnd(1, I("pupDelay") + 1)), gg + 1)));
                        }
                    }
                    o.Add(M('G', R0, F("globeH") * s, Mathf.Max(2, JsR(I("globeDur") * (gg > 0 ? .5f : 1f)))));
                    if (gg == 0 && F("ceph") > .02f) o.Add(M('X', R0));   // Melocactus: a woolly, bristly cephalium caps the mature body
                    o.Add(M('V', 0));
                    return o;
                };
                spec.Rules['D'] = (m, c) => m[0] > 0 ? List(M('D', m[0] - 1, m[1])) : List(M('H', m[1]));
                int cephStep = Mathf.Max(2, JsR(steps * .45f));
                spec.Rules['X'] = (m, c) => c.K >= cephStep ? List(M('f', -m[0] * .15f), M('Y', 4), M('G', m[0] * .6f, m[0] * (.2f + 1.1f * F("ceph")), 3), M('Y', 0)) : null;
                spec.Rules['V'] = (m, c) => m[0] < wait ? List(M('V', m[0] + 1)) : (c.K >= fstep ? List(M('W', I("crownN"), F("crownLat"), fs)) : null);
            }
            else if (form == Form.Rosette)
            {
                // rosette R(g): offsets (pups) on a ring around the mother, then the leaf apex
                int offs = I("offsets"); float osc = F("offScale"), rad0 = F("leafLen") * Mathf.Sin(F("leafOpen") * Mathf.Deg2Rad) + F("leafWid") * .3f;
                spec.Axiom = c => List(M('Y', 0), M('f', .018f), M('R', 0));   // leaves start at the soil, not 2 cm under it
                spec.Rules['R'] = (m, c) =>
                {
                    int gg = (int)m[0]; float sc = Mathf.Pow(osc, gg); var o = new List<Module>();
                    if (gg < I("offGen") && offs > 0)
                    {
                        int n = gg == 0 ? offs : Mathf.Max(1, offs - 2);
                        for (int k = 0; k < n; k++)
                        {
                            float az = gg == 0 ? k * 360f / n + c.Rnd(-20, 20) : c.Rnd(-60, 60);
                            o.AddRange(Br(M('$'), M('/', az), M('&', 90), M('f', rad0 * sc * c.Rnd(F("offDist") * .9f, F("offDist") * 1.1f) + rad0 * sc * osc * .6f), M('^', 90), M('D', Mathf.Floor(c.Rnd(2, 5)), gg + 1)));
                        }
                    }
                    o.Add(M('A', 0, gg, 0, sc));
                    return o;
                };
                spec.Rules['D'] = (m, c) => m[0] > 0 ? List(M('D', m[0] - 1, m[1])) : List(M('R', m[1]));
            }
            else
            {
                int gen = I("padGen");
                spec.Axiom = c =>
                {
                    var o = List(M('Y', 0)); int n = I("baseStems");
                    for (int i = 0; i < n; i++) o.AddRange(Br(M('/', i * 360f / n + c.Rnd(0, 40)), M('&', n > 1 ? c.Rnd(F("basePitch") * .7f, F("basePitch") * 1.25f) : 0), M('S', 0, F("padLen"))));
                    return o;
                };
                spec.Rules['S'] = (m, c) =>
                {
                    int gg = (int)m[0]; float l = m[1];
                    if (gg >= gen)
                    {
                        if (c.K < fstep) return null;
                        var f = List(Br(M('+', c.Rnd(-20, 20)), M('K', fs)));
                        if (!B("teeth") && c.R() < .5) f.AddRange(Br(M('f', -l * .25f), M('+', c.Rnd(30, 50) * (c.R() < .5 ? -1 : 1)), M('f', l * .05f), M('K', fs * .9f)));
                        return f;
                    }
                    var o = List(M('P', l, l * F("padRatio"), F("padThick"), gg));
                    int n = 1 + (c.R() < F("kidP1") ? 1 : 0) + (c.R() < F("kidP2") ? 1 : 0); float A = F("padAng");
                    for (int k = 0; k < n; k++)
                    {
                        // siblings fan out with a guaranteed gap: two split to opposite sides, three take left / centre / right
                        float ang = n == 1 ? c.Rnd(-A * .25f, A * .25f) : n == 2 ? (k == 0 ? -1 : 1) * c.Rnd(A * .55f, A) : (k == 0 ? c.Rnd(-A * .12f, A * .12f) : (k == 1 ? 1 : -1) * c.Rnd(A * .75f, A));
                        o.AddRange(Br(M('f', -l * F("padBack")), M('+', ang), M('/', c.Rnd(-F("padRoll"), F("padRoll"))), M('S', gg + 1, l * c.Rnd(.82f, 1.06f))));
                    }
                    return o;
                };
            }
            return spec;
        }

        // ───────────────────────── breeding ─────────────────────────
        /// <summary>Gaining an absent trait happens at this fraction of the normal mutation rate.</summary>
        public const double GainRate = .15;

        static double Gauss(Rng R)
        {
            double u = 0, v = 0; while (u == 0) u = R.Next(); while (v == 0) v = R.Next();
            return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
        }

        /// <summary>Trait blocks: genes that only make sense together are inherited as a unit (a rib set, a spine set, a flower look).</summary>
        public static readonly string[][] Blocks =
        {
            new[] { "form" }, new[] { "steps", "thicken", "r0", "trop", "fstart" },
            new[] { "shoots", "shootDelay", "shootP", "shootLean", "shootSpread" }, new[] { "mainSegs", "shootMin", "shootMax", "segLen", "radius" },
            new[] { "armP", "armWhorl", "armStart", "armEnd", "maxArms", "armPitch", "armSegs", "armLenR", "armR", "armTrop", "neck" },
            new[] { "jointed", "maxDepth", "jointBranchP", "jointAng" },
            new[] { "globeR", "globeH", "globeDur" }, new[] { "pups", "pupGen", "pupScale", "pupDelay", "pupDist" },
            new[] { "padLen", "padRatio", "padThick", "kidP1", "kidP2", "padAng", "padRoll", "padGen", "padBack", "lignify", "teeth" },
            new[] { "baseStems", "basePitch", "pot" },
            new[] { "ribs", "ribDepth", "ribPow", "twist" }, new[] { "tubAmp", "tubPair", "tubPow", "tubR" },
            new[] { "col", "tip", "tipAmt", "cork", "corkAmt", "corkH", "trunkDark", "fleck" }, new[] { "areole", "cap" },
            new[] { "nr", "nc", "lr", "lc", "ang", "sw", "scol", "stip", "ccol", "vary", "down" }, new[] { "woolSize", "woolCol", "hairN", "hairLen" },
            new[] { "fcol", "fbase" }, new[] { "flen", "fwid", "fn", "felev", "fcurl", "frings", "stamens", "stLen", "fr0", "fscale" }, new[] { "crownN", "crownLat" },
            new[] { "leafN", "leafLen", "leafWid", "leafThick", "leafShape", "leafSect", "leafCurl", "leafOpen", "leafInner", "diverg", "internode", "tipRos" },   // 20 leaf architecture
            new[] { "leafCol", "leafTip", "leafTipAmt", "bloom", "edge", "edgeCol", "window", "spots", "spotCol" },                                               // 21 leaf skin
            new[] { "teethN", "teethLen", "teethCol", "tipSpine" }, new[] { "offsets", "offScale", "offDist", "offGen" },                                         // 22 leaf armature, 23 offsets
            new[] { "stalkLen", "stalkTilt", "stalkBranch", "stalkFl", "stalkCol" }, new[] { "leafAlong", "fork" },                                             // 24 stalk, 25 leafy stems
            new[] { "topFlat", "fissure", "mottle", "mottleCol" }, new[] { "bands", "bandCol", "ribWave" }, new[] { "ceph", "cephCol" },                         // 26 stone, 27 pattern, 28 cephalium
        };
        /// <summary>Which blocks build each form's architecture.</summary>
        static readonly int[][] FormBlocks = { new[] { 2, 3, 4, 5, 25 }, new[] { 6, 7 }, new[] { 8, 9 }, new[] { 20, 21, 22, 23 } };

        /// <summary>
        /// Express a genome in another body form, so a parent of a different form still passes on look-alike genes:
        /// a barrel becomes a short fat column, a saguaro a tall barrel, a prickly pear a column of jointed pad-sized segments.
        /// </summary>
        public CactusGenome Express(Form toForm)
        {
            var from = FormGene;
            if (from == toForm) return this;
            var g = (double[])genes.Clone();
            void Set(string n, double v) { int i = SlotOf[n]; g[i] = Clamp(v, Slots[i].Min, Slots[i].Max); }
            double H, R; int n;   // common currency: height, radius, number of stems
            if (from == Form.Column) { R = Fd("radius"); H = Fd("segLen") * I("mainSegs"); n = I("shoots") + (int)JsRound(I("maxArms") * Fd("armP")); }
            else if (from == Form.Globe) { R = Fd("globeR"); H = Fd("globeH"); n = 1 + I("pups"); }
            else if (from == Form.Pad) { R = Math.Max(.02, Fd("padLen") * Fd("padRatio") * .4); H = Math.Max(.06, Fd("padLen") * Math.Min(I("padGen"), 4) * .8); n = I("baseStems") + (int)JsRound(I("padGen") * Fd("kidP1")); }
            else { double o = Fd("leafOpen") * (Math.PI / 180); R = Math.Max(.02, Fd("leafLen") * Math.Sin(o) * .4); H = Math.Max(.04, Fd("leafLen") * Math.Cos(o) + Fd("internode") * I("leafN")); n = 1 + I("offsets"); }
            if (toForm == Form.Column)
            {
                double segs = Clamp(JsRound(H / Math.Max(R * .7, .03)), 2, 18);
                Set("radius", R * .9); Set("mainSegs", segs); Set("segLen", H / segs); Set("shoots", Clamp(from == Form.Pad ? I("baseStems") : n, 1, 14));
                Set("shootMin", Math.Max(1, JsRound(segs * .5))); Set("shootMax", segs); Set("shootLean", from == Form.Globe ? 25 : 30); Set("shootSpread", 0); Set("shootDelay", 1); Set("shootP", .8);
                if (from == Form.Pad)
                {
                    Set("jointed", 1); Set("maxDepth", 3); Set("jointBranchP", Fd("kidP2") + .3); Set("jointAng", Fd("padAng"));
                    Set("armP", Fd("kidP1")); Set("maxArms", Clamp(JsRound(I("padGen") * Fd("kidP1") * 1.5), 1, 8)); Set("armWhorl", 1); Set("armPitch", Fd("padAng") + 15);
                    Set("armSegs", 1); Set("armLenR", .9); Set("armR", .8); Set("neck", .5); Set("armStart", .2); Set("armEnd", 1);
                }
                else { Set("armP", 0); Set("maxArms", 0); Set("jointed", 0); }
                Set("tipRos", from == Form.Rosette ? 1 : 0); Set("leafAlong", 0); Set("fork", 0);   // a rosette plant as a column: a stem crowned by its rosette
            }
            else if (toForm == Form.Rosette)
            {
                // a cactus as a rosette: ribs become leaves, spines become marginal teeth, skin becomes leaf skin
                int ribs = I("ribs"); bool spiny = I("nr") + I("nc") > 0 && Fd("lr") + Fd("lc") > .002;
                Set("leafN", Clamp(from == Form.Pad ? I("padGen") * 4 : (ribs > 0 ? ribs : 8) * 2 + 8, 8, 60)); Set("leafLen", Clamp(Math.Max(R * 1.6, H * .7), .03, .8));
                Set("leafWid", from == Form.Pad ? Fd("padLen") * Fd("padRatio") * .55 : Clamp(R * .5, .01, .15)); Set("leafThick", from == Form.Pad ? .3 : .55);
                Set("leafShape", from == Form.Pad ? .5 : .15); Set("leafSect", from == Form.Pad ? .1 : .35); Set("leafCurl", 10); Set("leafOpen", 55); Set("leafInner", 15); Set("diverg", 137.5); Set("internode", 0);
                Set("offsets", Clamp(n - 1, 0, 6)); Set("offScale", .55); Set("offDist", 1.2); Set("offGen", n > 1 ? 1 : 0);
                string[] dst = { "leafCol", "leafTip", "teethCol" }, src = { "col", "tip", "scol" };
                for (int k = 0; k < 3; k++) for (int j = 0; j < 3; j++) g[SlotOf[dst[k]] + j] = Clamp(genes[SlotOf[src[k]] + j], 0, 1);
                Set("leafTipAmt", Fd("tipAmt") * .5); Set("bloom", 0); Set("edge", 0); Set("window", 0); Set("spots", Clamp(Fd("fleck") * 2.5, 0, 1));
                Set("teethN", spiny ? Clamp(JsRound(I("nr") * .8), 2, 30) : 0); Set("teethLen", spiny ? Fd("lr") * .35 : 0); Set("tipSpine", spiny ? Fd("lc") * .5 : 0);
            }
            else if (toForm == Form.Globe)
            {
                Set("globeR", R * 1.05); Set("globeH", Math.Min(H, R * 3)); Set("globeDur", Clamp(JsRound(I("steps") * .7), 2, 12));
                Set("pups", Clamp(n - 1, 0, 6)); Set("pupGen", n > 1 ? 1 : 0); Set("pupScale", .6); Set("pupDelay", 3); Set("pupDist", 1.6);
            }
            else
            {
                double L = Clamp(Math.Max(H / 3, R * 2.4), .03, .35);
                Set("padLen", L); Set("padRatio", Clamp(R * 2.4 / L, .4, 1)); Set("padThick", R * .3); Set("padGen", Clamp(JsRound(H / L) + 1, 2, 8));
                Set("kidP1", from == Form.Column ? Clamp(Fd("armP") + (I("shoots") > 1 ? .3 : 0), .2, .9) : .4); Set("kidP2", .1); Set("padAng", 35); Set("padRoll", 25); Set("padBack", .08);
                Set("lignify", 0); Set("teeth", 0); Set("baseStems", Clamp(n, 1, 5)); Set("basePitch", n > 1 ? 30 : 0); Set("pot", 0);
            }
            return new CactusGenome { name = name, genes = g, origin = origin, parents = parents };
        }

        /// <summary>
        /// Breed two genomes.
        /// 1. The child's body form comes from one parent; both parents are expressed in that form (see <see cref="Express"/>).
        /// 2. The body plan (form, life span, architecture) comes from that parent as one unit; every other trait block
        ///    (ribs, tubercles, skin, spines, wool, flower colour, flower shape, crown) comes whole from either parent.
        ///    Numbers and colours in each block then blend toward the other parent by up to <paramref name="blend"/>,
        ///    so children land between their parents rather than as a random mosaic.
        /// 3. A few genes mutate (N(0, 8% of range)); an absent trait is only gained rarely (<see cref="GainRate"/>), and faintly.
        /// </summary>
        public static CactusGenome Cross(CactusGenome A, CactusGenome Bg, int seed, float mutation = .03f, float blend = .5f)
            => Cross(A, Bg, seed, D(mutation), D(blend));

        public static CactusGenome Cross(CactusGenome A, CactusGenome Bg, int seed, double mutation, double blend)
        {
            var R = new Rng(seed); int n = Slots.Length; var g = new double[n]; var origin = new float[n];
            bool formFromB = R.Next() < .5; var childForm = (formFromB ? Bg : A).FormGene;
            var Ax = A.Express(childForm); var Bx = Bg.Express(childForm); var arch = new HashSet<int>(FormBlocks[(int)childForm]);
            double planT = blend * (.25 + .75 * R.Next());   // the body plan always takes something from the other parent
            for (int bi = 0; bi < Blocks.Length; bi++)
            {
                bool plan = bi <= 1 || arch.Contains(bi); double r1 = R.Next(), r2 = R.Next();
                bool fromB = plan ? formFromB : r1 < .5; double t = bi == 0 ? 0 : (plan ? planT : r2 * blend);
                var s0 = arch.Contains(bi) ? Ax : A; var s1 = arch.Contains(bi) ? Bx : Bg;
                foreach (var nm in Blocks[bi])
                {
                    int i0 = SlotOf[nm], w = TypeOf[nm] == GT.C ? 3 : 1;
                    for (int i = i0; i < i0 + w; i++)
                    {
                        var s = Slots[i]; double x = (fromB ? s1 : s0).genes[i], y = (fromB ? s0 : s1).genes[i];
                        bool cont = s.Type == GT.F || s.Type == GT.I;
                        g[i] = cont ? x + (y - x) * t : x;
                        origin[i] = (float)(cont ? (fromB ? 1 - t : t) : (fromB ? 1 : 0));   // 0 = all A, 1 = all B
                    }
                }
            }
            for (int i = 0; i < n; i++)
            {
                var s = Slots[i];
                bool absent = !s.Color && (s.Type == GT.F || s.Type == GT.I) && s.Min == 0 && g[i] <= (s.Max - s.Min) * .01;
                if (!(R.Next() < (absent ? mutation * GainRate : mutation))) continue;
                if (s.Type == GT.B) g[i] = g[i] >= .5 ? 0 : 1;
                else if (s.Type == GT.E) { if (s.Gene == "form") continue; g[i] = Math.Floor(R.Next() * (s.Max + 1)); }
                else if (absent) g[i] = Clamp(g[i] + Math.Abs(Gauss(R)) * (s.Max - s.Min) * .04, s.Min, s.Max);
                else g[i] = Clamp(g[i] + Gauss(R) * (s.Max - s.Min) * .08, s.Min, s.Max);
                origin[i] = origin[i] < 0 ? 2 : origin[i] + 10;
            }
            return new CactusGenome { name = Short(A.name) + " × " + Short(Bg.name), genes = g, origin = origin, parents = new[] { A.name, Bg.name } };
        }
        static string Short(string s)
        {
            if (s.Length <= 18) return s;
            var head = s.Split(new[] { " × " }, StringSplitOptions.None)[0];
            return head.Substring(0, Math.Min(14, head.Length)) + "…";
        }

        // ───────────────────────── text code (CG2; CG1 still decodes) ─────────────────────────
        /// <summary>16 bits per slot over its range, base64url, prefixed "CG2." Same format as the previewer.</summary>
        public string Encode()
        {
            var bytes = new byte[Slots.Length * 2];
            for (int i = 0; i < Slots.Length; i++)
            {
                var s = Slots[i]; int q = (int)JsRound(Clamp((genes[i] - s.Min) / (s.Max - s.Min), 0, 1) * 65535);
                bytes[2 * i] = (byte)(q >> 8); bytes[2 * i + 1] = (byte)(q & 255);
            }
            return "CG2." + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static CactusGenome Decode(string code, string name = "Imported")
        {
            var t = code.Trim(); bool legacy = t.StartsWith("CG1.");
            if (t.StartsWith("CG1.") || t.StartsWith("CG2.")) t = t.Substring(4);
            t = t.Replace('-', '+').Replace('_', '/'); t += new string('=', (4 - t.Length % 4) % 4);
            var b = Convert.FromBase64String(t); int nSlots = legacy ? LegacySlots : Slots.Length;
            if (b.Length < nSlots * 2) throw new FormatException("Genome code is too short for this gene table.");
            var g = FromDict(name, new Dictionary<string, object>()).genes; var o = new float[Slots.Length];   // slots a CG1 code does not hold keep their defaults
            for (int i = 0; i < Slots.Length; i++)
            {
                o[i] = -1; if (i >= nSlots) continue;
                var s = Slots[i]; double mn = s.Min, mx = s.Max; if (legacy && LegacyRange.TryGetValue(s.Name, out var lr)) { mn = lr[0]; mx = lr[1]; }
                int q = (b[2 * i] << 8) | b[2 * i + 1]; g[i] = mn + (mx - mn) * q / 65535;
            }
            return new CactusGenome { name = name, genes = g, origin = o };
        }

        public CactusGenome Clone() => new CactusGenome { name = name, genes = (double[])genes.Clone(), origin = origin != null ? (float[])origin.Clone() : null, parents = parents };
    }
}
