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

        public static readonly string[] Chromosomes = { "Habit", "Surface", "Armature", "Flower" };
        public enum Form { Column, Globe, Pad }
        static readonly int[][] TubPairs = { new[] { 3, 5 }, new[] { 5, 8 }, new[] { 8, 13 }, new[] { 13, 21 } };

        static Gene G_(string n, int c, GT t, float a, float b) => new Gene(n, c, t, a, b);
        /// <summary>float literal → the double the browser uses (shortest round-trip decimal, e.g. .6f → 0.6).</summary>
        static double D(float f) => double.Parse(f.ToString("R", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture);
        static double Clamp(double v, double a, double b) => v < a ? a : v > b ? b : v;
        static double JsRound(double v) => Math.Floor(v + .5);   // JavaScript Math.round
        public static readonly Gene[] Genes =
        {
            // Habit
            G_("form",0,GT.E,0,2), G_("steps",0,GT.I,6,26), G_("thicken",0,GT.F,2,7), G_("r0",0,GT.F,.4f,1), G_("trop",0,GT.F,-2,2),
            G_("shoots",0,GT.I,1,14), G_("shootDelay",0,GT.I,0,4), G_("shootP",0,GT.F,0,1), G_("shootLean",0,GT.F,0,45), G_("shootSpread",0,GT.F,0,.03f),
            G_("mainSegs",0,GT.I,2,22), G_("shootMin",0,GT.I,1,14), G_("shootMax",0,GT.I,1,16), G_("segLen",0,GT.F,.03f,.4f), G_("radius",0,GT.F,.02f,.35f),
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
        };

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

        /// <summary>The wild genome for one of the ten species (tuned to grow like its hand-written L-system).</summary>
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
                default: return FromDict("Hedgehog cactus", new Dictionary<string, object> {
                    ["form"]=0f,["steps"]=12f,["thicken"]=3f,["trop"]=.8f,["shoots"]=9f,["shootLean"]=40f,["shootSpread"]=.006f,["mainSegs"]=5f,["shootMin"]=3f,["shootMax"]=6f,["segLen"]=.055f,["radius"]=.038f,
                    ["ribs"]=11f,["ribDepth"]=.2f,["ribPow"]=.45f,["col"]=V(.33f,.5f,.3f),["cap"]=.9f,["areole"]=.022f,
                    ["nr"]=9f,["nc"]=3f,["lr"]=.022f,["lc"]=.045f,["ang"]=66f,["sw"]=.0009f,["scol"]=V(.93f,.84f,.66f),["stip"]=V(.99f,.96f,.9f),["ccol"]=V(.93f,.84f,.66f),["vary"]=.5f,["down"]=.2f,["woolSize"]=.004f,["woolCol"]=V(.85f,.8f,.7f),
                    ["fcol"]=V(.86f,.18f,.56f),["fbase"]=V(.6f,.1f,.3f),["flen"]=.045f,["fwid"]=.022f,["fn"]=10f,["felev"]=48f,["fcurl"]=18f,["frings"]=2f,["stamens"]=50f,["stLen"]=.012f,["crownN"]=2f,["crownLat"]=62f,["fstart"]=.75f,["fr0"]=.008f });
            }
        }

        // ───────────────────────── genome → species ─────────────────────────
        static Color Lc(Color a, Color b, float t) => Color.Lerp(a, b, t);

        /// <summary>One generic grammar whose numbers come from this genome.</summary>
        public CactusSpecies ToSpecies()
        {
            var g = this; var form = FormGene; int steps = I("steps"); int fstep = Mathf.RoundToInt(F("fstart") * steps);
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

            var fc = C("fcol"); var fb = C("fbase");
            var rings = new List<PetalRing> { new PetalRing { n = I("fn"), off = 0, len = F("flen"), wid = F("fwid"), elev = F("felev"), curl = F("fcurl"), col = fc, baseCol = fb } };
            if (I("frings") > 1) rings.Add(new PetalRing { n = Mathf.Max(3, I("fn") - 1), off = .5f, len = F("flen") * .8f, wid = F("fwid") * .85f, elev = F("felev") * .62f, curl = F("fcurl") * .6f, col = Lc(fc, Color.white, .15f), baseCol = fb });
            float fs = F("fscale");

            var spec = new CactusSpecies
            {
                Kind = (CactusKind)(-1), Id = "g:" + Encode(), Name = name, Latin = parents != null ? string.Join(" × ", parents) : "",
                Steps = steps, Dur = 1, Thicken = F("thicken"), R0 = F("r0"),
                Flower = new FlowerSpec { r0 = F("fr0"), stamens = I("stamens"), stLen = F("stLen"), rings = rings.ToArray() },
                Styles = form == Form.Pad ? new[] { padSty } : new[] { trunk, arm },
                Pot = form == Form.Pad && B("pot") ? new PotSpec { r = .12f, h = .14f, col = new Color(.72f, .4f, .26f) } : null,
            };
            spec.BaseY = spec.Pot != null ? .13f : 0;

            if (form == Form.Column)
            {
                int shoots = I("shoots"), delay = I("shootDelay"), main = I("mainSegs"), sMin = I("shootMin"), sMax = Mathf.Max(sMin, I("shootMax"));
                float segLen = F("segLen"), rad = F("radius"); int maxArms = I("maxArms"), maxDepth = I("maxDepth"); bool jointed = B("jointed");
                spec.Axiom = c => List(M('Y', 0), M('O', 0, 0));
                spec.Rules['O'] = (m, c) =>
                {
                    int n = (int)m[0], t = (int)m[1];
                    if (n >= shoots) return new List<Module>();
                    if (t < delay && n > 0) return List(M('O', n, t + 1));
                    var o = new List<Module>();
                    if (n == 0 || c.R() < F("shootP"))
                    {
                        float Lc_ = n == 0 ? main : Mathf.Floor(c.Rnd(sMin, sMax + 1));
                        // basal shoots sit on a Vogel spiral (golden angle, distance ∝ √n) scaled by stem radius, so they never grow through each other
                        o.AddRange(Br(M('/', n * 137.5f + c.Rnd(-3, 3)), M('&', 90), M('f', n == 0 ? 0 : Mathf.Max(F("shootSpread") * (4 + n), 2.15f * F("radius") * Mathf.Sqrt(n))), M('^', 90),
                            M('&', n == 0 ? 0 : c.Rnd(F("shootLean") * .3f, F("shootLean"))), M('C', 0, Lc_, 0)));
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
                        if ((d >= 1 || jointed) && d < maxDepth)
                        {
                            int n = 1 + (c.R() < F("jointBranchP") ? 1 : 0);
                            for (int k = 0; k < n; k++) e.AddRange(Br(M('/', c.Rnd(0, 360)), M('&', c.Rnd(F("jointAng") * .55f, F("jointAng") * 1.4f)), M('C', 0, Mathf.Max(1, Mathf.RoundToInt(I("armSegs") * c.Rnd(.75f, 1.3f))), d + 1)));
                        }
                        e.Add(M('E', d));
                        return e;
                    }
                    var o = new List<Module>();
                    if (jointed && d > 0) o.Add(M('Z'));
                    o.Add(M('F', d == 0 ? segLen : segLen * F("armLenR"), d == 0 ? rad : rad * F("armR")));
                    c.State.TryGetValue("arms", out int arms);
                    for (int w = 0; w < I("armWhorl"); w++)
                        if (d == 0 && arms < maxArms && j >= F("armStart") * Ln && j <= F("armEnd") * Ln && c.R() < F("armP"))
                        {
                            arms++; c.State["arms"] = arms;
                            o.AddRange(Br(M('/', j * 137.5f + w * 160 + c.Rnd(-20, 20)), M('&', F("armPitch") + c.Rnd(-8, 8)), M('Y', 1), M('C', 0, Mathf.Max(1, Mathf.RoundToInt(I("armSegs") * c.Rnd(.75f, 1.3f))), 1)));
                        }
                    o.Add(M('C', j + 1, Ln, d));
                    return o;
                };
                spec.Rules['E'] = (m, c) =>
                {
                    if (c.K < fstep) return null;
                    int d = (int)m[0];
                    if (jointed && d > 0) return c.R() < .35 ? List(M('W', 1, 0, fs * .9f)) : new List<Module>();
                    return List(M('W', d == 0 ? I("crownN") : Mathf.Max(1, Mathf.RoundToInt(I("crownN") / 2f)), F("crownLat"), fs));
                };
            }
            else if (form == Form.Globe)
            {
                int pups = I("pups"), pupGen = I("pupGen"); float sc = F("pupScale");
                int wait = Mathf.Max(2, Mathf.RoundToInt(steps * (1 - F("fstart"))));
                spec.Axiom = c => List(M('Y', 0), M('H', 0));
                spec.Rules['H'] = (m, c) =>
                {
                    int gg = (int)m[0]; float s = Mathf.Pow(sc, gg), R0 = F("globeR") * s; var o = new List<Module>();
                    if (gg < pupGen && pups > 0)
                    {
                        int n = gg == 0 ? pups : Mathf.Max(1, pups - 2);
                        for (int k = 0; k < n; k++) o.AddRange(Br(M('/', c.Rnd(0, 360)), M('&', 90), M('f', R0 * c.Rnd(F("pupDist") * .92f, F("pupDist") * 1.08f)), M('^', c.Rnd(66, 86)), M('D', Mathf.Floor(c.Rnd(1, I("pupDelay") + 1)), gg + 1)));
                    }
                    o.Add(M('G', R0, F("globeH") * s, Mathf.Max(2, Mathf.RoundToInt(I("globeDur") * (gg > 0 ? .5f : 1f))))); o.Add(M('V', 0));
                    return o;
                };
                spec.Rules['D'] = (m, c) => m[0] > 0 ? List(M('D', m[0] - 1, m[1])) : List(M('H', m[1]));
                spec.Rules['V'] = (m, c) => m[0] < wait ? List(M('V', m[0] + 1)) : (c.K >= fstep ? List(M('W', I("crownN"), F("crownLat"), fs)) : null);
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
                        float ang = n == 1 ? c.Rnd(-A * .25f, A * .25f) : (k == 0 ? c.Rnd(-A * .4f, A * .4f) : (k == 1 ? 1 : -1) * c.Rnd(A * .6f, A));
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

        /// <summary>
        /// Breed two genomes. For each chromosome a two-point crossover picks which parent each gene comes from
        /// (linked genes travel together); continuous genes then blend toward the other parent by up to <paramref name="blend"/>;
        /// colour genes stay whole; each slot mutates with probability <paramref name="mutation"/> (N(0, 12% of range), flips for bools, redraws for enums).
        /// </summary>
        public static CactusGenome Cross(CactusGenome A, CactusGenome Bg, int seed, float mutation = .08f, float blend = .3f)
            => Cross(A, Bg, seed, D(mutation), D(blend));

        public static CactusGenome Cross(CactusGenome A, CactusGenome Bg, int seed, double mutation, double blend)
        {
            var R = new Rng(seed); int n = Slots.Length; var g = new double[n]; var origin = new float[n];
            for (int chr = 0; chr < Chromosomes.Length; chr++)
            {
                var idx = new List<int>(); for (int i = 0; i < n; i++) if (Slots[i].Chr == chr) idx.Add(i);
                int a = (int)Math.Floor(R.Next() * (idx.Count + 1)), b = (int)Math.Floor(R.Next() * (idx.Count + 1)); if (a > b) { var t = a; a = b; b = t; }
                bool first = R.Next() < .5;
                for (int k = 0; k < idx.Count; k++)
                {
                    int i = idx[k]; var s = Slots[i]; bool fromB = (k >= a && k < b) ? !first : first;
                    double x = fromB ? Bg.genes[i] : A.genes[i], y = fromB ? A.genes[i] : Bg.genes[i];
                    bool cont = s.Type == GT.F || s.Type == GT.I; double t = cont ? R.Next() * blend : 0;
                    g[i] = x + (y - x) * t;
                    origin[i] = (float)(fromB ? 1 - t : t);
                }
            }
            foreach (var gene in Genes)
            {
                if (gene.Type != GT.C) continue;
                int i = SlotOf[gene.Name];
                for (int k = 1; k < 3; k++)
                {
                    bool fromB = origin[i] > .5f; double t2 = Math.Abs((double)origin[i] - (fromB ? 1 : 0));
                    double x = fromB ? Bg.genes[i + k] : A.genes[i + k], y = fromB ? A.genes[i + k] : Bg.genes[i + k];
                    g[i + k] = x + (y - x) * t2; origin[i + k] = origin[i];
                }
            }
            // An absent trait (gene at the bottom of its range, e.g. no flecks, no hair) can only be gained by a rare
            // mutation (GainRate × the usual rate), and it starts faint. Otherwise absent traits pop up far too often.
            for (int i = 0; i < n; i++)
            {
                var s = Slots[i];
                bool absent = !s.Color && (s.Type == GT.F || s.Type == GT.I) && s.Min == 0 && g[i] <= (s.Max - s.Min) * .01;
                if (!(R.Next() < (absent ? mutation * GainRate : mutation))) continue;
                if (s.Type == GT.B) g[i] = g[i] >= .5 ? 0 : 1;
                else if (s.Type == GT.E) g[i] = Math.Floor(R.Next() * (s.Max + 1));
                else if (absent) g[i] = Clamp(g[i] + Math.Abs(Gauss(R)) * (s.Max - s.Min) * .04, s.Min, s.Max);
                else g[i] = Clamp(g[i] + Gauss(R) * (s.Max - s.Min) * .12, s.Min, s.Max);
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

        // ───────────────────────── text code (CG1) ─────────────────────────
        /// <summary>16 bits per slot over its range, base64url, prefixed "CG1." Same format as the previewer.</summary>
        public string Encode()
        {
            var bytes = new byte[Slots.Length * 2];
            for (int i = 0; i < Slots.Length; i++)
            {
                var s = Slots[i]; int q = (int)JsRound(Clamp((genes[i] - s.Min) / (s.Max - s.Min), 0, 1) * 65535);
                bytes[2 * i] = (byte)(q >> 8); bytes[2 * i + 1] = (byte)(q & 255);
            }
            return "CG1." + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static CactusGenome Decode(string code, string name = "Imported")
        {
            var t = code.Trim(); if (t.StartsWith("CG1.")) t = t.Substring(4);
            t = t.Replace('-', '+').Replace('_', '/'); t += new string('=', (4 - t.Length % 4) % 4);
            var b = Convert.FromBase64String(t);
            if (b.Length < Slots.Length * 2) throw new FormatException("Genome code is too short for this gene table.");
            var g = new double[Slots.Length]; var o = new float[Slots.Length];
            for (int i = 0; i < Slots.Length; i++) { var s = Slots[i]; int q = (b[2 * i] << 8) | b[2 * i + 1]; g[i] = s.Min + (s.Max - s.Min) * q / 65535; o[i] = -1; }
            return new CactusGenome { name = name, genes = g, origin = o };
        }

        public CactusGenome Clone() => new CactusGenome { name = name, genes = (double[])genes.Clone(), origin = origin != null ? (float[])origin.Clone() : null, parents = parents };
    }
}
