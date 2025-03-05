using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Sprite sprite;            // Sprite for the layer
        public Vector3 center;           // Center position for the sprite
        [Range(0, 1)]
        public float movementFactor;     // Movement factor (<1)
        public float depth;              // Depth for ordering layers (Z-position)

        private GameObject gameObject;
        private SpriteRenderer spriteRenderer;

        public float repeatAfter;

        public void Init(Transform parent)
        {
            gameObject = new GameObject("ParallaxLayer_" + sprite.name);
            gameObject.transform.parent = parent;
            gameObject.transform.localPosition = new Vector3(center.x, center.y, depth);

            // Set up SpriteRenderer and assign the sprite
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = (int)(-depth * 100); // Optional: Adjust based on depth for layer ordering
        }

        public void Update()
        {
            int x = 0;
            int y = 0;
            if(repeatAfter != 0)
            {
                x = (int)((gameObject.transform.parent.position.x + (repeatAfter * .5f)) / repeatAfter);
                y = (int)((gameObject.transform.parent.position.y + (repeatAfter * .5f)) / repeatAfter);  
            }
            
            for(int i = -1;i<=1;i++)
            {
                for(int j = -1;j<=1;j++)
                {
                    Vector3 centerCandidate = new Vector3(center.x, center.y, center.z);
                    Vector3 offset = (gameObject.transform.parent.position - centerCandidate) * -movementFactor; 
                    if(offset.x > -.5f && offset.x < .5f && offset.x > -.5f && offset.x < .5f){
                        //Debug.Log(x+" "+y+" "+i+" "+j+" "+offset);
                        gameObject.transform.localPosition = new Vector3(offset.x, offset.y, depth);
                    }
                }
            }
        }
    }

    public ParallaxLayer[] layers;

    private List<ParallaxLayer> layersInternal;
    public Sprite[] stars;
    public int additionalStarCount = 50;   // Number of additional star layers
    public float starDepth = -1f;          // Depth for star layers
    public float movementFactor = 0.1f;    // Movement factor for star layers
    public Vector2 randomRange = new Vector2(-.5f, .5f); // Range for random star positions

    // Start is called before the first frame update
    void Start()
    {
        // Initialize defined layers
        layersInternal = new List<ParallaxLayer>();
        foreach (ParallaxLayer layer in layers)
        {
            layersInternal.Add(layer);
        }

        // Create additional star layers with random positions
        for (int i = 0; i < additionalStarCount; i++)
        {
            ParallaxLayer starLayer = new ParallaxLayer
            {
                sprite = stars[Random.Range(0, stars.Length)],
                center = new Vector3(
                    Random.Range(randomRange.x, randomRange.y),
                    Random.Range(randomRange.x, randomRange.y),
                    starDepth
                ),
                movementFactor = movementFactor,
                depth = starDepth,
                repeatAfter = randomRange.y - randomRange.x
            };

            layersInternal.Add(starLayer);
        }

        foreach(ParallaxLayer layer in layersInternal)
        {
            layer.Init(transform);
        }
    }

    void Update()
    {
        foreach (ParallaxLayer layer in layersInternal)
        {
            layer.Update();
        }
    }
}
