using UnityEngine;

namespace Realms.Cacti
{
    /// <summary>
    /// Grows one cactus from its L-system. Add to an empty GameObject, pick a species, then drag
    /// <see cref="growth"/> (0 = seed, 1 = mature and flowering) or tick <see cref="autoGrow"/>.
    /// The derivation is cached per species+seed, so scrubbing growth only re-interprets and re-meshes.
    /// Submeshes: 0 body · 1 spines/hair · 2 wool · 3 flowers.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class CactusGrower : MonoBehaviour
    {
        public CactusKind species = CactusKind.Saguaro;
        public int seed = 7;
        [Range(0, 1)] public float growth = 1f;

        [Header("Animation")]
        public bool autoGrow;
        [Tooltip("Seconds from seed to flowering when auto-growing.")]
        public float secondsToMature = 20f;
        public bool loop;

        [Header("Detail")]
        public bool spines = true;
        [Tooltip("Half the areoles and one hair per areole. Use for distant plants or while animating.")]
        public bool draft;
        [Tooltip("While auto-growing, rebuild at most this often (s). Large plants take tens of ms to mesh.")]
        public float rebuildInterval = .1f;

        [Header("Materials (optional)")]
        [Tooltip("Single-sided vertex-colour material for body and wool. Defaults to Realms/CactusVertexColor.")]
        public Material bodyMaterial;
        [Tooltip("Double-sided vertex-colour material for spines, hair and petals.")]
        public Material doubleSidedMaterial;

        public CactusBuilder.BuildResult LastBuild { get; private set; }

        Mesh mesh;
        bool dirty = true;
        float lastRebuild;
        static Material defaultBody, defaultDouble;

        void OnEnable() { dirty = true; }
        void OnValidate() { dirty = true; }

        void OnDestroy()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }

        void Update()
        {
            if (autoGrow && Application.isPlaying)
            {
                growth += Time.deltaTime / Mathf.Max(.01f, secondsToMature);
                if (growth >= 1f) { growth = loop ? 0f : 1f; if (!loop) autoGrow = false; }
                if (Time.time - lastRebuild >= rebuildInterval) dirty = true;
            }
            if (dirty) Rebuild();
        }

        /// <summary>Re-interpret and re-mesh at the current growth value.</summary>
        public void Rebuild()
        {
            dirty = false; lastRebuild = Time.time;
            var spec = CactusLibrary.Get(species);
            if (mesh == null) mesh = new Mesh { name = spec.Name };
            bool animating = autoGrow && Application.isPlaying && growth < 1f;
            LastBuild = CactusBuilder.Build(spec, seed, growth * spec.Steps, spines, draft || animating, mesh);
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = GetComponent<MeshRenderer>();
            var b = bodyMaterial != null ? bodyMaterial : DefaultMaterial(false);
            var d = doubleSidedMaterial != null ? doubleSidedMaterial : DefaultMaterial(true);
            r.sharedMaterials = new[] { b, d, b, d };
        }

        /// <summary>The derived L-system string, for inspection.</summary>
        public string DescribeString(int max = 300) => LSystem.Format(LSystem.Derive(CactusLibrary.Get(species), seed), max);

        static Material DefaultMaterial(bool doubleSided)
        {
            ref Material m = ref doubleSided ? ref defaultDouble : ref defaultBody;
            if (m != null) return m;
            var sh = Shader.Find("Realms/CactusVertexColor");
            if (sh == null)
            {
                Debug.LogWarning("CactusGrower: shader 'Realms/CactusVertexColor' not found; falling back to URP/Lit (no vertex colours). Assign materials manually.");
                sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            }
            m = new Material(sh) { name = doubleSided ? "Cactus (double-sided)" : "Cactus", hideFlags = HideFlags.DontSave };
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", doubleSided ? 0f : 2f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", doubleSided ? .35f : .2f);
            return m;
        }
    }
}
