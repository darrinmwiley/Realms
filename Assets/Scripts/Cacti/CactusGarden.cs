using UnityEngine;

namespace Realms.Cacti
{
    /// <summary>Spawns all fifty-two species in a grid, each with its own <see cref="CactusGrower"/>. Handy for comparing them.</summary>
    public class CactusGarden : MonoBehaviour
    {
        public int seed = 7;
        [Range(0, 1)] public float growth = 1f;
        public bool autoGrow;
        public float secondsToMature = 20f;
        [Tooltip("Scale every species to this height so they can be compared side by side. 0 = true scale.")]
        public float normaliseHeight = 1.5f;
        public float spacing = 2.5f;
        public int columns = 8;
        public bool draft = true;
        public Material bodyMaterial, doubleSidedMaterial;

        void Start()
        {
            int i = 0;
            foreach (CactusKind kind in System.Enum.GetValues(typeof(CactusKind)))
            {
                var go = new GameObject(kind.ToString());
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3((i % columns - (columns - 1) * .5f) * spacing, 0, (i / columns) * spacing);
                var g = go.AddComponent<CactusGrower>();
                g.species = kind; g.seed = seed; g.draft = draft; g.bodyMaterial = bodyMaterial; g.doubleSidedMaterial = doubleSidedMaterial;
                if (normaliseHeight > 0)
                {
                    g.growth = 1f; g.Rebuild();
                    var b = go.GetComponent<MeshFilter>().sharedMesh.bounds;
                    float size = Mathf.Max(b.size.y, b.size.x * .9f, b.size.z * .9f);
                    if (size > 1e-4f) go.transform.localScale = Vector3.one * (normaliseHeight / size);
                }
                g.growth = autoGrow ? 0f : growth; g.autoGrow = autoGrow; g.secondsToMature = secondsToMature;
                g.Rebuild();
                i++;
            }
        }
    }
}
