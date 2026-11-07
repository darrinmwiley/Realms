# Cacti: L-system cactus generator

Ten cactus species grown from parametric, stochastic, timed L-systems, meshed procedurally.

| File | What it does |
|---|---|
| `CactusLSystem.cs` | `Module`, `LContext`, deterministic `Rng` (mulberry32), `LSystem.Derive` |
| `CactusSpecies.cs` | Style/flower types and `CactusLibrary`: the 10 species' axioms and productions |
| `CactusBuilder.cs` | Turtle interpretation at a continuous time T → organs → one `Mesh` with 4 submeshes |
| `CactusGenome.cs` | Genome (118 slots on 4 chromosomes), wild presets, `ToSpecies`, `Cross`, CG1 text codes |
| `CactusGrower.cs` | MonoBehaviour: pick a species or paste a genome code, scrub `growth` 0→1 or tick `autoGrow` |
| `CactusBreeder.cs` | Crosses two growers (or wild species) and lays out a litter of hybrids |
| `CactusGarden.cs` | Spawns all ten in a grid (optionally normalised to one height) |
| `CactusVertexColor.shader` | URP lit shader that uses vertex colours (body, spines, wool, petals) |

## Use

1. Create an empty GameObject and add **CactusGrower**.
2. Choose `species` and `seed`, then drag `growth`, or tick `autoGrow` in Play mode.
3. Optional: add **CactusGarden** to an empty GameObject to see all ten side by side.

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
   | `K(scale)` | flower |
   | `W(n,lat,scale)` | crown of n flowers around the last apex |
   | `+ −` | yaw |
   | `& ^` | pitch |
   | `/ \` | roll |
   | `[ ]` | branch |
   | `Y(i)` | switch surface style |
   | `Z` | start a new joint (a constriction, as in cholla) |

   Tropism (`CactusStyle.trop`) bends stems up (saguaro arms, organ pipe) or down (Christmas cactus).
3. **Mesh.** Stems are swept along a Catmull-Rom centreline with parallel-transport frames. Cross-sections:
   - **Ribs:** `r(θ) = R·(1 − d·(1 − cᵖ))`, with `c = ½ + ½cos(n(θ − τs))`.
   - **Tubercles:** `r = R·(1 + a·(c₁c₂)ᵖ)`, with c₁ and c₂ the m- and n-parastichy waves. Areoles sit on the
     lattice points `θ = π(i/m + j/n)`, `s = πR(j/n − i/m)`.

   Each stem gets an apex dome, branch necks and basal cork. Pads have an obovate outline (toothed for
   Schlumbergera), lignify with age, and carry an areole lattice on both faces. Areoles carry wool tufts,
   radial and central spines, or hair.

## Genomes and breeding

Every plant can also be grown from a **genome**: the numbers inside one generic L-system. The genome has
85 genes (118 slots, since colours take three) on four chromosomes:

| Chromosome | Controls |
|---|---|
| Habit | Form (column / globe / pads), shoots, segments, arms, joints, pups, pads, tropism, pot |
| Surface | Ribs, tubercles (Fibonacci pair), colour, tip, cork, trunk darkening, flecks, areole spacing |
| Armature | Radial and central spines, colours, wool, hair |
| Flower | Petal colours, size, count, rings, stamens, crown size and position, start time |

`CactusGenome.Preset(kind)` returns each wild species' genome, tuned to grow like its hand-written
L-system. `genome.ToSpecies()` builds a normal `CactusSpecies` whose productions read those numbers:

| Form | Productions |
|---|---|
| Column | `O(n)` emits basal shoots; `C(j,L,d)` grows segments, arms and joints; `E(d)` adds the flower crown |
| Globe | `H(g)` grows a ribbed or tubercled body plus pup generations, using delays `D` |
| Pad | `S(g,l)` grows chains of pads with 1–3 children each |

`CactusGenome.Cross(A, B, seed, mutation = .03, blend = .5)` breeds two genomes so children look like a mix of
their parents rather than a random mosaic:

1. **Body form:** the child takes its form (column / globe / pads) from one parent. The other parent is first
   *expressed* in that form (`Express`), translating its height, radius and stem count. A barrel becomes a short
   fat column, a saguaro a tall barrel, a prickly pear a column of pad-sized joints. That way it passes on
   look-alike genes and not silent ones.
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
- Crowns hold only as many flowers as fit around the apex.
- A plant-wide triangle budget (450k for stem bodies) coarsens ring spacing on huge hybrids.

Clean children went from 28% to about 70% (the rest are mostly light pad grazes). Children that look like
neither parent went from 6% to 0, and about 97% sit between their parents.

Genomes serialise to a ~320-character **CG1 code**: 16 bits per slot, base64url. Codes made in the browser
atlas paste straight into `CactusGrower.genomeCode`.

**In the editor:** add `CactusBreeder` to an empty GameObject, assign two `CactusGrower` parents (or leave
them empty and pick wild species), then use the component menu (⋮) → *Breed litter*. Each child is a
`CactusGrower` carrying its code, so you can make it a parent next.

## Browser twin

The Cactus L-System Atlas artifact runs the same engine in JavaScript, rule for rule and with the same RNG,
so a given seed grows the same plant in both. Checked against the C#: identical module, organ, spine, wool
and flower counts for all ten species. Genome codes and the plants they grow also match bit for bit,
across 46 first- and second-generation crosses. Rounding uses JavaScript's half-up rule (`JsR`) on both sides.

## Adding a species

Add a value to `CactusKind`, then write a factory in `CactusLibrary` that sets the styles, flower, `Axiom`
and `Rules`, and register it in `Build()`. Rules return the replacement modules, or `null` to keep a module
unchanged. Use `ctx.R()`/`ctx.Rnd(a,b)` for randomness, so seeds stay deterministic.
