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

`CactusGenome.Cross(A, B, seed, mutation, blend)` breeds two genomes:

1. **Crossover:** each chromosome takes a two-point crossover, so neighbouring (linked) genes are inherited together.
2. **Blending:** continuous genes blend toward the other parent by up to `blend`.
3. **Colours:** each colour comes whole from one parent.
4. **Mutation:** each slot mutates with probability `mutation`. Numbers get Gaussian noise of 12% of their range,
   booleans flip, and enums are redrawn.

`origin[]` records where each slot came from: wild, parent A or B, or mutated. Form is a discrete gene, so a
column × pad cross grows as one or the other while carrying the other's architecture genes silently. Those can
resurface in later generations.

Genomes serialise to a ~320-character **CG1 code**: 16 bits per slot, base64url. Codes made in the browser
atlas paste straight into `CactusGrower.genomeCode`.

**In the editor:** add `CactusBreeder` to an empty GameObject, assign two `CactusGrower` parents (or leave
them empty and pick wild species), then use the component menu (⋮) → *Breed litter*. Each child is a
`CactusGrower` carrying its code, so you can make it a parent next.

## Browser twin

The Cactus L-System Atlas artifact runs the same engine in JavaScript, rule for rule and with the same RNG,
so a given seed grows the same plant in both. Checked against the C#: identical module, organ, spine, wool
and flower counts for all ten species. Genome codes and the plants they grow also match bit for bit,
across 30 first-generation and 6 second-generation crosses.

## Adding a species

Add a value to `CactusKind`, then write a factory in `CactusLibrary` that sets the styles, flower, `Axiom`
and `Rules`, and register it in `Build()`. Rules return the replacement modules, or `null` to keep a module
unchanged. Use `ctx.R()`/`ctx.Rnd(a,b)` for randomness, so seeds stay deterministic.
