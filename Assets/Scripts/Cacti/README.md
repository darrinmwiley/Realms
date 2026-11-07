# Cacti: L-system cactus and succulent generator

Fifty-two cacti and succulents grown from parametric, stochastic, timed L-systems, meshed procedurally: ten
with hand-written productions, and forty-two more grown from genome presets through one generic grammar.
Those forty-two include aloes, agaves, echeverias, haworthias, living stones, euphorbias and tree aloes.

| File | What it does |
|---|---|
| `CactusLSystem.cs` | `Module`, `LContext`, deterministic `Rng` (mulberry32), `LSystem.Derive` |
| `CactusSpecies.cs` | Style/flower/leaf types and `CactusLibrary`: 10 hand-written species plus 42 built from genomes |
| `CactusBuilder.cs` | Turtle interpretation at a continuous time T → organs → one `Mesh` with 4 submeshes |
| `CactusGenome.cs` | Genome (181 slots on 5 chromosomes), wild presets, `ToSpecies`, `Cross`, CG2 text codes (CG1 still decodes) |
| `CactusGrower.cs` | MonoBehaviour: pick a species or paste a genome code, scrub `growth` 0→1 or tick `autoGrow` |
| `CactusBreeder.cs` | Crosses two growers (or wild species) and lays out a litter of hybrids |
| `CactusGarden.cs` | Spawns all fifty-two in a grid (optionally normalised to one height) |
| `CactusVertexColor.shader` | URP lit shader that uses vertex colours (body, spines, wool, petals) |

## Use

1. Create an empty GameObject and add **CactusGrower**.
2. Choose `species` and `seed`, then drag `growth`, or tick `autoGrow` in Play mode.
3. Optional: add **CactusGarden** to an empty GameObject to see all fifty-two side by side.

Materials are created at runtime from `Realms/CactusVertexColor`. For player builds, either assign
`bodyMaterial`/`doubleSidedMaterial` (two materials on that shader, Cull Back and Cull Off), or add the
shader to *Project Settings → Graphics → Always Included Shaders*. `Shader.Find` only sees shaders the build includes.

## How it works

1. **Derive.** Every module records the step it was born in, so one derivation holds the plant's whole life.
   Derivations are cached per species and seed.
2. **Interpret at time T.** Modules born after T are skipped; younger ones are scaled by a growth function.
   Turtle symbols:

   | Symbol | Meaning |
   |---|---|
   | `F(len,r)` | stem segment |
   | `f(len)` | move without drawing |
   | `G(R,H,steps)` | ribbed or tubercled body |
   | `P(len,wid,thick,gen)` | pad |
   | `L(len,wid,thick,open,inner,dur,off,life)` | leaf, unfolding from `inner`° to `open`° off the axis; `life` > 0 drops it after that many steps |
   | `K(scale)` | flower |
   | `W(n,lat,scale)` | crown of n flowers around the last apex |
   | `+ −` | yaw |
   | `& ^` | pitch |
   | `/ \` | roll |
   | `[ ]` | branch |
   | `Y(i)` | switch surface style |
   | `Z` | start a new joint (a constriction, as in cholla) |
   | `$` | level the turtle: heading up, old heading's horizontal as "outward" |

   Tropism (`CactusStyle.trop`) bends stems up (saguaro arms, organ pipe) or down (Christmas cactus).
3. **Mesh.** Stems are swept along a Catmull-Rom centreline with parallel-transport frames. Cross-sections:
   - **Ribs:** `r(θ) = R·(1 − d·(1 − cᵖ))`, with `c = ½ + ½cos(n(θ − τs))`.
   - **Tubercles:** `r = R·(1 + a·(c₁c₂)ᵖ)`, with c₁ and c₂ the m- and n-parastichy waves. Areoles sit on the
     lattice points `θ = π(i/m + j/n)`, `s = πR(j/n − i/m)`.

   Each stem gets an apex dome, branch necks and basal cork. Pads have an obovate outline (toothed for
   Schlumbergera), lignify with age, and carry an areole lattice on both faces. Areoles carry wool tufts,
   radial and central spines, or hair.

   **Flowers** are built from a short floral tube (green below, the petal colour at the rim) topped by a throat disc,
   then rings of cupped tepals. Each tepal is a 5-wide strip, curved like a spoon and twisted 10–18° about its
   midrib, so neighbours overlap like real petals instead of z-fighting. Rings are sorted outermost first. Each inner
   ring starts higher and closer to the axis, and opens less than the ring outside it, so rings never cut through
   each other. Thin stamens with small anthers rise from an annulus on the throat, and a style with six stigma
   lobes stands just above them. On tubercled bodies the crown is lifted to clear the tubercle tips.

   **Leaves** are a closed cross-section swept along a centreline that recurves (`curl` > 0) or incurves. The
   cross-section has a flat or channelled top and a keeled back, or is round when `sect` = 1. The width profile
   blends from lance (aloe, agave) through spoon (echeveria) to bead (string of pearls). Each leaf can carry
   marginal teeth, a terminal spine, white edge lines, a translucent window tip, rows of white tubercles, a
   powdery bloom, and a tinted tip.

   **Stones and oddities** are body options: `topFlat` flattens the dome and widens the body upward, `fissure`
   cuts a slit across the top into two lobes, and `mottle` paints a value-noise window pattern on the top
   (Lithops, Pleiospilos). `bands` adds transverse stripes (Euphorbia obesa), and `ribWave` makes ribs
   undulate (brain cactus). A cephalium is a second, woolly, bristly body raised on a mature globe (Melocactus).

## The species

The first ten (`Saguaro` to `Hedgehog`) have their own hand-written L-systems in `CactusLibrary`. The other
forty-two exist only as genome presets (`CactusGenome.Preset`). `CactusLibrary.Get` grows them through the
generic grammar and adds their names and field notes:

| `CactusKind` | Name | Species |
|---|---|---|
| `FishhookBarrel` | Fishhook barrel | *Ferocactus wislizenii* |
| `SanPedro` | San Pedro | *Echinopsis pachanoi* |
| `Cardon` | Cardón | *Pachycereus pringlei* |
| `Peyote` | Peyote | *Lophophora williamsii* |
| `SilverTorch` | Silver torch | *Cleistocactus strausii* |
| `Espostoa` | Peruvian old lady | *Espostoa lanata* |
| `BlueCandle` | Blue candle | *Myrtillocactus geometrizans* |
| `ChinCactus` | Chin cactus | *Gymnocalycium baldianum* |
| `Rebutia` | Crown cactus | *Rebutia minuscula* |
| `BalloonCactus` | Balloon cactus | *Parodia magnifica* |
| `BunnyEars` | Bunny ears | *Opuntia microdasys* |
| `SantaRita` | Purple prickly pear | *Opuntia santa-rita* |
| `CaneCholla` | Tree cholla | *Cylindropuntia imbricata* |
| `DragonFruit` | Dragon fruit | *Selenicereus undatus* |
| `QueenOfTheNight` | Queen of the night | *Epiphyllum oxypetalum* |
| `RatTail` | Rat-tail cactus | *Disocactus flagelliformis* |
| `SandDollar` | Sand dollar | *Astrophytum asterias* |
| `EasterLily` | Easter lily cactus | *Echinopsis oxygona* |
| `Ladyfinger` | Ladyfinger cactus | *Mammillaria elongata* |
| `OldLady` | Old lady cactus | *Mammillaria hahniana* |

Then 22 succulents and oddities that go beyond the cactus body plan: leaf rosettes, stones, trees and trailers.

| `CactusKind` | Name | Species |
|---|---|---|
| `AloeVera` | Aloe vera | *Aloe vera* |
| `SpiralAloe` | Spiral aloe | *Aloe polyphylla* |
| `CenturyPlant` | Century plant | *Agave americana* |
| `QueenVictoria` | Queen Victoria agave | *Agave victoriae-reginae* |
| `Echeveria` | Echeveria | *Echeveria 'Perle von Nürnberg'* |
| `ZebraHaworthia` | Zebra plant | *Haworthiopsis attenuata* |
| `WindowHaworthia` | Window haworthia | *Haworthia cooperi* |
| `Gasteria` | Ox tongue | *Gasteria carinata var. verrucosa* |
| `HensAndChicks` | Hens and chicks | *Sempervivum tectorum* |
| `BabyToes` | Baby toes | *Fenestraria rhopalophylla* |
| `LivingRock` | Living rock cactus | *Ariocarpus fissuratus* |
| `BuddhasTemple` | Buddha's temple | *Crassula 'Buddha's Temple'* |
| `LivingStones` | Living stones | *Lithops karasmontana* |
| `SplitRock` | Split rock | *Pleiospilos nelii* |
| `BaseballPlant` | Baseball plant | *Euphorbia obesa* |
| `TurksCap` | Turk's cap | *Melocactus matanzanus* |
| `BrainCactus` | Brain cactus | *Stenocactus multicostatus* |
| `QuiverTree` | Quiver tree | *Aloidendron dichotomum* |
| `BlackRose` | Black rose | *Aeonium arboreum 'Zwartkop'* |
| `MadagascarPalm` | Madagascar palm | *Pachypodium lamerei* |
| `StringOfPearls` | String of pearls | *Curio rowleyanus* |
| `MedusasHead` | Medusa's head | *Euphorbia caput-medusae* |

Hanging plants (a column or globe with downward tropism and the `pot` gene, like the rat-tail cactus) get a
pot sized to their clump. Pads use a pot as before.

## Genomes and breeding

Every plant can also be grown from a **genome**: the numbers inside one generic L-system. The genome has
128 genes (181 slots, since colours take three) on five chromosomes:

| Chromosome | Controls |
|---|---|
| Habit | Form (column / globe / pads / rosette), shoots, segments, arms, joints, pups, pads, tropism, pot |
| Surface | Ribs, tubercles (Fibonacci pair), colour, tip, cork, trunk darkening, flecks, areole spacing, wavy ribs, stone top, fissure, window mottle, bands, cephalium |
| Armature | Radial and central spines, colours, wool, hair |
| Flower | Petal colours, size, count, rings, stamens, crown size and position, start time |
| Leaf | Leaf count, size, shape, section, curl, open angle, divergence, internode; colours, bloom, edge, window, tubercles, teeth, tip spine; offsets; flower stalk; rosettes on stem tips, leaves along stems, forking |

`CactusGenome.Preset(kind)` returns each wild species' genome, tuned to grow like its hand-written
L-system. `genome.ToSpecies()` builds a normal `CactusSpecies` whose productions read those numbers:

| Form | Productions |
|---|---|
| Column | `O(n)` emits basal shoots; `C(j,L,d)` grows segments, arms and joints; `E(d)` adds the flower crown |
| Globe | `H(g)` grows a ribbed or tubercled body plus pup generations, using delays `D` |
| Pad | `S(g,l)` grows chains of pads with 1–3 children each |
| Rosette | `R(g)` places offsets, then `A(i,…)` adds a few leaves each step on the divergence angle; at flowering it sends up a stalk `B(j,…)` (three segments a step, flowers on the upper part, side branches), or a crown of flowers |

Columns can also carry leaves. With `tipRos`, a growing tip carries a rosette whose leaves drop after 3.5 steps,
and a finished tip keeps a rosette that flowers on a stalk (quiver tree, Aeonium, Pachypodium). `leafAlong` puts
leaves along every segment (string of pearls). `fork` splits tips in two, tapering each level (tree aloes).
Globes with `ceph` raise a cephalium `X` once mature.

`CactusGenome.Cross(A, B, seed, mutation = .03, blend = .5)` breeds two genomes so children look like a mix of
their parents rather than a random mosaic:

1. **Body form:** the child takes its form (column / globe / pads / rosette) from one parent. The other parent is first
   *expressed* in that form (`Express`), translating its height, radius and stem count. A barrel becomes a short
   fat column, a saguaro a tall barrel, a prickly pear a column of pad-sized joints. An aloe becomes a stem
   crowned with its rosette. A cactus becomes a rosette whose ribs turn into leaves, its spines into marginal
   teeth, and its skin into leaf skin. That way it passes on look-alike genes and not silent ones. Whoever
   gives a child its leaves also decides whether its stems end in rosettes, so cacti can come out crowned with
   agave or echeveria heads.
2. **Trait blocks:** genes that only make sense together (a rib set, a spine set, a flower look, an arm plan) are
   inherited whole from one parent (`Blocks`). The body plan (form, life cycle and the form's architecture
   blocks) comes as one unit from the form donor.
3. **Blending:** each block's numbers and colours then blend toward the other parent by up to `blend`.
4. **Mutation:** each slot mutates with probability `mutation`. Numbers get Gaussian noise of 8% of their range,
   booleans flip, and form never mutates.
   A trait that is absent (its gene at the bottom of its range, e.g. no white flecks or no hair) is only gained by
   a rare mutation (`GainRate` = 15% of the normal rate), and it starts faint. Without that, absent traits popped up in
   about 4% of offspring of parents that had neither.

`origin[]` records where each slot came from: wild, parent A or B, or mutated.

### Audit loop

The grammar was tuned by breeding every pair of wild species (135 children per round), then checking each
child automatically. The checks cover stems or pads intersecting, organs sinking into the ground, flowers
buried or floating, spines dwarfing the stem, missing flowers, and meshes too heavy. Each check also measures
how far the child sits from each parent. Fixes that came out of it:

- Stems are fitted to the remaining growth steps, so every plant finishes and flowers.
- Branches fork below a crown instead of through it.
- Flowers are sized against the spines and the organ they sit on.
- Pups spread at even azimuths, at a clear distance. They are planted with `$`, a turtle command that levels the
  heading to vertical and keeps "outward" as the new up vector.
- Sibling pads splay symmetrically.
- Jointed plants fork from far enough below the tip that the new joint clears the tip flower (at least the
  flower's reach, or 1.8× the joint radius). A new joint leaves at 20° or more.
- Crowns hold only as many flowers as fit around the apex.
- A plant-wide triangle budget (450k for stem bodies) coarsens ring spacing on huge hybrids.

Clean children went from 28% to about 70% (the rest are mostly light pad grazes). With all 30 species
(1305 children per round) it is 72%. Children that look like neither parent went from 6% to under 0.1%,
and 95% sit between their parents. With all 52 (3978 children) 81% pass every check; leaves aren't
intersection-checked yet.

Genomes serialise to a ~480-character **CG2 code**: 16 bits per slot, base64url. Codes made in the browser
atlas paste straight into `CactusGrower.genomeCode`. Older **CG1** codes (118 slots) still decode to the same
plants. The new genes take their defaults, and the three ranges CG2 widened (`form`, `radius`, `segLen`) are
read with their old bounds.

**In the editor:** add `CactusBreeder` to an empty GameObject, assign two `CactusGrower` parents (or leave
them empty and pick wild species), then use the component menu (⋮) → *Breed litter*. Each child is a
`CactusGrower` carrying its code, so you can make it a parent next.

## Browser twin

The Cactus L-System Atlas artifact runs the same engine in JavaScript, rule for rule and with the same RNG,
so a given seed grows the same plant in both. Checked against the C#: identical module, organ, spine, wool
and flower counts for all ten species. Genome codes and the plants they grow also match bit for bit,
across all 52 presets and 72 first- and second-generation crosses, flower geometry included. Rounding uses JavaScript's half-up rule (`JsR`) on both sides.

## Adding a species

Add a value to `CactusKind`, then write a factory in `CactusLibrary` that sets the styles, flower, `Axiom`
and `Rules`, and register it in `Build()`. Rules return the replacement modules, or `null` to keep a module
unchanged. Use `ctx.R()`/`ctx.Rnd(a,b)` for randomness, so seeds stay deterministic.
