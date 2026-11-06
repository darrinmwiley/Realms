# Cacti: L-system cactus generator

Ten cactus species grown from parametric, stochastic, timed L-systems, meshed procedurally.

| File | What it does |
|---|---|
| `CactusLSystem.cs` | `Module`, `LContext`, deterministic `Rng` (mulberry32), `LSystem.Derive` |
| `CactusSpecies.cs` | Style/flower types and `CactusLibrary`: the 10 species' axioms and productions |
| `CactusBuilder.cs` | Turtle interpretation at a continuous time T → organs → one `Mesh` with 4 submeshes |
| `CactusGrower.cs` | MonoBehaviour: pick a species, scrub `growth` 0→1 or tick `autoGrow` |
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

## Browser twin

The Cactus L-System Atlas artifact runs the same engine in JavaScript, rule for rule and with the same RNG,
so a given seed grows the same plant in both. Checked against the C#: identical module, organ, spine, wool
and flower counts for all ten species.

## Adding a species

Add a value to `CactusKind`, then write a factory in `CactusLibrary` that sets the styles, flower, `Axiom`
and `Rules`, and register it in `Build()`. Rules return the replacement modules, or `null` to keep a module
unchanged. Use `ctx.R()`/`ctx.Rnd(a,b)` for randomness, so seeds stay deterministic.
