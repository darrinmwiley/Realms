using System.Collections.Generic;
using UnityEngine;

namespace Realms.Cacti
{
    /// <summary>
    /// Crosses two cacti and lays the litter out in a row. Parents can be scene <see cref="CactusGrower"/>s
    /// (wild or already hybrid) or, if left empty, the wild species chosen below.
    /// Use the component's context menu (⋮) → "Breed litter", or call <see cref="Breed"/> from game code.
    /// Every offspring is a CactusGrower carrying its genome code, so you can breed it again.
    /// </summary>
    public class CactusBreeder : MonoBehaviour
    {
        [Header("Parents")]
        public CactusGrower parentA;
        public CactusGrower parentB;
        public CactusKind fallbackA = CactusKind.OrganPipe;
        public CactusKind fallbackB = CactusKind.Hedgehog;

        [Header("Breeding")]
        [Range(1, 12)] public int litterSize = 4;
        [Range(0, 1)] public float mutation = .08f;
        [Range(0, 1)] public float blend = .3f;
        public int crossSeed = 1;

        [Header("Layout")]
        public float spacing = 1.5f;
        [Tooltip("Scale each child to this height for side-by-side comparison. 0 = true scale.")]
        public float normaliseHeight = 1f;
        public bool draft = true;

        readonly List<GameObject> litter = new List<GameObject>();
        public IReadOnlyList<GameObject> Litter => litter;

        CactusGenome GenomeOf(CactusGrower g, CactusKind fallback) => g != null ? g.Genome : CactusGenome.Preset(fallback);

        [ContextMenu("Breed litter")]
        public void Breed()
        {
            Clear();
            var A = GenomeOf(parentA, fallbackA); var B = GenomeOf(parentB, fallbackB);
            for (int i = 0; i < litterSize; i++)
            {
                var child = CactusGenome.Cross(A, B, crossSeed * 16 + i + 1, mutation, blend);
                var go = new GameObject($"Hybrid {i + 1} ({A.name} × {B.name})");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3((i - (litterSize - 1) * .5f) * spacing, 0, 0);
                var g = go.AddComponent<CactusGrower>();
                g.draft = draft;
                g.Genome = child;
                g.Rebuild();
                if (normaliseHeight > 0)
                {
                    var b = go.GetComponent<MeshFilter>().sharedMesh.bounds;
                    float size = Mathf.Max(b.size.y, b.size.x * .9f, b.size.z * .9f);
                    if (size > 1e-4f) go.transform.localScale = Vector3.one * (normaliseHeight / size);
                }
                litter.Add(go);
            }
        }

        [ContextMenu("Breed again (next seed)")]
        public void BreedAgain() { crossSeed++; Breed(); }

        [ContextMenu("Clear litter")]
        public void Clear()
        {
            foreach (var go in litter) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            litter.Clear();
        }
    }
}
