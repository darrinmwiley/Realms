using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Demonstrates a set of 2D circles with:
///  - Per-circle inner & outer radius
///  - Inner radius (physics collider) displayed as darker color
///  - Outer radius (visual "color influence") displayed as normal color
///  - Collision damping
///  - Click to select a circle (using pixel-click on Display), arrow keys to move
///  - WASD modifies the Voronoi offset
///  - Mouse scroll-wheel zooms in/out around the Voronoi center
///  - Left-click and drag also pans the Voronoi region in world coordinates
///  - Per-pixel raycasts to identify which circle(s) cover a pixel
///  - Repulsive force if two circles are within each other's outer radius
///  - Now: border is exactly 1 pixel wide, determined by adjacency checks
/// 
/// Physics-related code is now placed in FixedUpdate (movement, repulsion).
/// </summary>
public class Circles2DWithDisplay : MonoBehaviour
{
    [Header("Display Reference")]
    public Display display; // Assign your Display component here in the Inspector

    [Header("Circle Count / Spawn")]
    public int circleCount = 10;
    public Vector2 spawnRange = new Vector2(10, 10);

    [Header("Per-Circle Radius Settings")]
    [Tooltip("Each circle gets a random radius in this range, used for BOTH inner and outer radius.")]
    public float minCircleRadius = 0.5f;
    public float maxCircleRadius = 2f;

    [Header("Circle Movement")]
    public float maxSpeed = 5f;
    public float moveForce = 10f;
    public float circleDrag = 1f;
    [Tooltip("Velocity is multiplied by this factor upon collision (0=stop dead, 1=no slowdown)")]
    public float collisionDampFactor = 0.2f;

    [Header("Camera Settings (Now used for Voronoi offset)")]
    [Tooltip("How quickly WASD modifies the Voronoi offset.")]
    public float cameraMoveSpeed = 10f;

    [Header("Zoom Settings")]
    [Tooltip("How quickly the mouse wheel zoom occurs.")]
    public float zoomSpeed = 5f;

    [Header("Voronoi Area")]
    public float voronoiX = -10f;
    public float voronoiY = -10f;
    public float voronoiWidth = 20f;
    public float voronoiHeight = 20f;

    [Tooltip("Background color if no circle covers that pixel.")]
    public Color backgroundColor = Color.black;

    [Tooltip("Inner radius darkening factor. 0=black, 1=same color.")]
    public float innerDarkFactor = 0.5f;

    [Header("Pressure / Repulsion")]
    [Tooltip("Strength of repulsive force when two circles overlap within outerRadius.")]
    public float repulsionStrength = 5f;

    // Border color is always white, for 1-pixel-wide borders.
    private Color borderColor = Color.white;

    private bool isDragging = false;
    private Vector2Int dragStartPixel;
    private Vector2 startVoronoiPosAtDrag;

    // We'll store the user's arrow-key input here, then apply it in FixedUpdate.
    private Vector2 movementInput;

    /// <summary>
    /// Internal data for each circle.
    /// </summary>
    public class CircleData
    {
        public int circleID;         // Unique ID for each circle
        public Transform transform;
        public Rigidbody2D rb;
        public Color color;

        public float innerRadius;
        public float outerRadius;

        public CircleCollider2D outerCollider;  // Used for OverlapCollider in repulsion
    }

    private List<CircleData> circles = new List<CircleData>();
    private Rigidbody2D focusedCircle = null;

    private void Start()
    {
        if (display == null)
        {
            Debug.LogError("No Display assigned to Circles2DWithDisplay. Please link one in the Inspector.");
            return;
        }

        // Spawn circles
        for (int i = 0; i < circleCount; i++)
        {
            SpawnCircle(i);
        }
    }

    /// <summary>
    /// Read input, handle camera offset, and do the Voronoi rendering. 
    /// But do NOT apply physics-based movement here. That will happen in FixedUpdate.
    /// </summary>
    private void Update()
    {
        // Camera / offset
        HandleVoronoiOffset();

        // Left-click selection
        HandleCircleSelection();

        // Read arrow-key input for movement (but don't apply it yet)
        movementInput = Vector2.zero;
        if (focusedCircle != null)
        {
            if (Input.GetKey(KeyCode.UpArrow))    movementInput += Vector2.up;
            if (Input.GetKey(KeyCode.DownArrow))  movementInput += Vector2.down;
            if (Input.GetKey(KeyCode.LeftArrow))  movementInput += Vector2.left;
            if (Input.GetKey(KeyCode.RightArrow)) movementInput += Vector2.right;
        }

        // Draw the Voronoi effect each frame
        DrawVoronoiToDisplay();
    }

    /// <summary>
    /// FixedUpdate is called on a fixed timestep. 
    /// This is where we apply forces and do physics-based repulsion.
    /// </summary>
    private void FixedUpdate()
    {
        HandleCircleMovement();
        ApplyRepulsionForces();
    }

    /// <summary>
    /// Creates a new circle with:
    ///  - One CircleCollider2D for collisions (innerRadius)
    ///  - One CircleCollider2D for outer radius detection (outerRadius, trigger)
    ///  - A CircleRefs component linking to its CircleData
    ///  - A Rigidbody2D
    ///  - Collision damping
    /// </summary>
    private void SpawnCircle(int circleIndex)
    {
        GameObject circleObj = new GameObject("Circle_" + circleIndex);
        circleObj.transform.position = new Vector3(
            Random.Range(-spawnRange.x, spawnRange.x),
            Random.Range(-spawnRange.y, spawnRange.y),
            0f
        );

        // Create the circle data
        CircleData data = new CircleData();
        data.circleID = circleIndex;   // Unique ID
        data.transform = circleObj.transform;
        data.rb = circleObj.AddComponent<Rigidbody2D>();
        data.rb.gravityScale = 0f; // top-down, no gravity
        data.rb.drag = circleDrag;
        data.rb.angularDrag = 0f;
        data.rb.mass = 10f;

        // Randomly assign a radius for inner/outer.
        float randomRadius = Random.Range(minCircleRadius, maxCircleRadius);
        data.innerRadius = randomRadius * 2f;
        data.outerRadius = randomRadius * 3f;
        data.color = Random.ColorHSV(); // random color

        // 1) Inner collider for collisions
        CircleCollider2D innerColl = circleObj.AddComponent<CircleCollider2D>();
        innerColl.radius = data.innerRadius;

        // 2) Outer collider for Voronoi detection
        CircleCollider2D outerColl = circleObj.AddComponent<CircleCollider2D>();
        outerColl.radius = data.outerRadius;
        outerColl.isTrigger = true; // doesn't physically collide
        data.outerCollider = outerColl;

        // 3) Collision-damping behavior
        CircleCollision collisionHandler = circleObj.AddComponent<CircleCollision>();
        collisionHandler.collisionDampFactor = collisionDampFactor;

        // 4) CircleRefs component to link this GameObject to its CircleData
        CircleRefs refs = circleObj.AddComponent<CircleRefs>();
        refs.Data = data;

        circles.Add(data);
    }

    /// <summary>
    /// Handles panning (WASD, left-mouse drag) and zoom (mouse wheel) of the Voronoi region.
    /// This is purely input/camera code, so it's fine to do in Update.
    /// </summary>
    private void HandleVoronoiOffset()
    {
        // 1) WASD Panning (only if not currently dragging)
        if (!isDragging)
        {
            if (Input.GetKey(KeyCode.W)) voronoiY += cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) voronoiY -= cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.A)) voronoiX -= cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) voronoiX += cameraMoveSpeed * Time.deltaTime;
        }

        // 2) Left Mouse Drag
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int? pix = display.TranslateMouseToTextureCoordinates();
            if (pix.HasValue)
            {
                isDragging = true;
                dragStartPixel = pix.Value;
                startVoronoiPosAtDrag = new Vector2(voronoiX, voronoiY);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        else if (isDragging && Input.GetMouseButton(0))
        {
            // While dragging, figure out how far we've moved in pixel space
            Vector2Int? pix = display.TranslateMouseToTextureCoordinates();
            if (pix.HasValue)
            {
                int dx = pix.Value.x - dragStartPixel.x;
                int dy = pix.Value.y - dragStartPixel.y;

                // Convert pixel delta to world-space delta
                float worldPerPixelX = voronoiWidth / display.GetWidth();
                float worldPerPixelY = voronoiHeight / display.GetHeight();

                // Typical "drag to pan" approach is to subtract dx from X
                voronoiX = startVoronoiPosAtDrag.x - dx * worldPerPixelX;
                voronoiY = startVoronoiPosAtDrag.y - dy * worldPerPixelY;
            }
        }

        // 3) Mouse Wheel Zoom (only if not currently dragging)
        if (!isDragging)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                float centerX = voronoiX + voronoiWidth * 0.5f;
                float centerY = voronoiY + voronoiHeight * 0.5f;

                float scaleFactor = 1f - scroll * zoomSpeed;
                scaleFactor = Mathf.Max(scaleFactor, 0.01f);

                float newWidth = voronoiWidth * scaleFactor;
                float newHeight = voronoiHeight * scaleFactor;

                voronoiX = centerX - newWidth * 0.5f;
                voronoiY = centerY - newHeight * 0.5f;
                voronoiWidth = newWidth;
                voronoiHeight = newHeight;
            }
        }
    }

    /// <summary>
    /// Left-click to select/focus a circle (if within its innerRadius).
    /// This is input-based, so we do it in Update.
    /// </summary>
    private void HandleCircleSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int? pixelCoords = display.TranslateMouseToTextureCoordinates();
            if (!pixelCoords.HasValue) return;

            Vector2 clickWorldPos = PixelToWorld(pixelCoords.Value.x, pixelCoords.Value.y);

            float closestDistance = float.MaxValue;
            CircleData closestCircle = null;

            foreach (CircleData c in circles)
            {
                float dist = Vector2.Distance(clickWorldPos, c.transform.position);
                // Check if within this circle's inner radius
                if (dist < c.innerRadius && dist < closestDistance)
                {
                    closestDistance = dist;
                    closestCircle = c;
                }
            }

            if (closestCircle != null)
            {
                focusedCircle = closestCircle.rb;
            }
            else
            {
                focusedCircle = null;
            }
        }
    }

    /// <summary>
    /// Uses the movementInput (populated in Update) to move the focused circle, 
    /// and clamps velocity. Called from FixedUpdate to ensure physics consistency.
    /// </summary>
    private void HandleCircleMovement()
    {
        if (focusedCircle == null) return;

        // Apply force
        if (movementInput != Vector2.zero)
        {
            focusedCircle.AddForce(movementInput * moveForce);
        }

        // Clamp velocity
        if (focusedCircle.velocity.magnitude > maxSpeed)
        {
            focusedCircle.velocity = focusedCircle.velocity.normalized * maxSpeed;
        }
    }

    /// <summary>
    /// Adds a gentle repulsive force between overlapping circles, using their outer colliders.
    /// This is a physics operation, so we call it from FixedUpdate.
    /// </summary>
    private void ApplyRepulsionForces()
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true,   // so we catch trigger-collider overlaps
            useLayerMask = false,
            useDepth = false
        };

        List<Collider2D> overlaps = new List<Collider2D>(16);

        for (int i = 0; i < circles.Count; i++)
        {
            CircleData cA = circles[i];
            CircleCollider2D outerA = cA.outerCollider;
            if (!outerA) continue;

            overlaps.Clear();
            int count = outerA.OverlapCollider(filter, overlaps);

            for (int k = 0; k < count; k++)
            {
                Collider2D otherColl = overlaps[k];
                if (!otherColl) continue;

                CircleRefs otherRefs = otherColl.GetComponentInParent<CircleRefs>();
                if (!otherRefs) continue;

                CircleData cB = otherRefs.Data;
                if (cB == null || cB == cA) continue;

                // Avoid doubling forces if we see the pair from both sides
                if (cA.transform.GetInstanceID() >= cB.transform.GetInstanceID())
                    continue;

                Vector2 posA = cA.transform.position;
                Vector2 posB = cB.transform.position;
                Vector2 delta = posA - posB;
                float dist = delta.magnitude;

                float combinedOuter = cA.outerRadius + cB.outerRadius;
                if (dist < combinedOuter && dist > 0.001f)
                {
                    Vector2 pushDir = delta.normalized;
                    float overlapFactor = 1f - (dist / combinedOuter);
                    float strength = overlapFactor * repulsionStrength;

                    Vector2 force = pushDir * strength;
                    cA.rb.AddForce(force);
                    cB.rb.AddForce(-force);
                }
            }
        }
    }

    /// <summary>
    /// 1) First pass: determine which circle "wins" each pixel (store circleID or -1),
    ///    and store the pixel's color in a Color array.
    /// 2) Second pass: any pixel whose winning circle differs from at least one neighbor
    ///    is flagged as a border => color it white.
    /// 3) Render the final colors to the display.
    /// </summary>
    private void DrawVoronoiToDisplay()
    {
        int texWidth = display.GetWidth();
        int texHeight = display.GetHeight();

        // We'll store both the "winning ID" and the "pixel color" for each pixel
        int[] winners = new int[texWidth * texHeight];
        Color[] colors = new Color[texWidth * texHeight];

        float left = voronoiX;
        float right = voronoiX + voronoiWidth;
        float bottom = voronoiY;
        float top = voronoiY + voronoiHeight;

        int layerMask = Physics2D.DefaultRaycastLayers;

        // ---- First Pass ----
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = y * texWidth + x;

                float nx = x / (float)(texWidth - 1);
                float ny = y / (float)(texHeight - 1);

                float worldX = Mathf.Lerp(left, right, nx);
                float worldY = Mathf.Lerp(bottom, top, ny);
                Vector2 pixelWorldPos = new Vector2(worldX, worldY);

                // Raycast to find which circles cover this pixel
                Vector2 rayStart = pixelWorldPos + Vector2.up * 0.001f;
                Vector2 rayDir = Vector2.down;
                float rayDist = 0.002f;

                RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, rayDir, rayDist, layerMask);

                // Among hits, pick the circle with the smallest fractionDist = dist / outerRadius
                float minFractionDist = float.MaxValue;
                float winningDist = float.MaxValue;
                CircleData winningCircle = null;

                for (int h = 0; h < hits.Length; h++)
                {
                    CircleRefs circleRefs = hits[h].collider.GetComponentInParent<CircleRefs>();
                    if (circleRefs == null) continue;

                    CircleData cData = circleRefs.Data;
                    if (cData == null) continue;

                    float distToCenter = Vector2.Distance(pixelWorldPos, cData.transform.position);
                    if (distToCenter < cData.outerRadius)
                    {
                        float fractionDist = distToCenter / cData.outerRadius;
                        if (fractionDist < minFractionDist)
                        {
                            minFractionDist = fractionDist;
                            winningDist = distToCenter;
                            winningCircle = cData;
                        }
                    }
                }

                // Decide color & record winner
                if (winningCircle == null)
                {
                    winners[index] = -1;  // no circle
                    colors[index] = backgroundColor;
                }
                else
                {
                    winners[index] = winningCircle.circleID;

                    // If within the circle's inner radius => darker color
                    if (winningDist < winningCircle.innerRadius)
                    {
                        Color origColor = winningCircle.color;
                        colors[index] = new Color(
                            origColor.r * innerDarkFactor,
                            origColor.g * innerDarkFactor,
                            origColor.b * innerDarkFactor,
                            1f
                        );
                    }
                    else
                    {
                        // Otherwise, normal circle color
                        colors[index] = winningCircle.color;
                    }
                }
            }
        }

        // ---- Second Pass ----
        // If a pixel's winner differs from *any* neighbor's winner, color this pixel white.
        // We'll check 8-direction adjacency (N, NE, E, SE, S, SW, W, NW).
        int[] neighborOffsets =
        {
            -1,  0,  // up
            1,   0,  // down
            0,  -1,  // left
            0,   1//,  // right
            //-1, -1,  // up-left
            //-1,  1,  // up-right
            //1,  -1,  // down-left
            //1,   1   // down-right
        };

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = y * texWidth + x;
                int myWinner = winners[index];

                if (myWinner == -1) 
                    continue; // background pixel; we only color border if it's part of a circle

                // Check neighbors
                bool isBorder = false;
                for (int n = 0; n < neighborOffsets.Length; n += 2)
                {
                    int dy = neighborOffsets[n];
                    int dx = neighborOffsets[n + 1];

                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= texWidth || ny < 0 || ny >= texHeight)
                        continue; // out of bounds => skip

                    int neighborIdx = ny * texWidth + nx;
                    int neighborWinner = winners[neighborIdx];
                    if (neighborWinner != myWinner)
                    {
                        isBorder = true;
                        break;
                    }
                }

                if (isBorder)
                {
                    colors[index] = borderColor;
                }
            }
        }

        // ---- Render final colors ----
        display.Clear();
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = y * texWidth + x;
                display.SetPixel(x, y, colors[index]);
            }
        }
        display.Render();
    }

    /// <summary>
    /// Convert from a pixel coordinate [0..(texWidth - 1)] x [0..(texHeight - 1)]
    /// to a world position in [voronoiX..(X+Width), voronoiY..(Y+Height)].
    /// </summary>
    private Vector2 PixelToWorld(int px, int py)
    {
        float texWidth = display.GetWidth();
        float texHeight = display.GetHeight();

        float nx = px / (texWidth - 1f);
        float ny = py / (texHeight - 1f);

        float worldX = Mathf.Lerp(voronoiX, voronoiX + voronoiWidth, nx);
        float worldY = Mathf.Lerp(voronoiY, voronoiY + voronoiHeight, ny);

        return new Vector2(worldX, worldY);
    }
}

/// <summary>
/// A tiny component to link a circle GameObject to its CircleData.
/// </summary>
public class CircleRefs : MonoBehaviour
{
    public Circles2DWithDisplay.CircleData Data;
}

/// <summary>
/// Dampens velocities on collision.
/// </summary>
public class CircleCollision : MonoBehaviour
{
    public float collisionDampFactor = 0.2f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb != null)
        {
            rb.velocity *= collisionDampFactor;
            Rigidbody2D otherRb = collision.rigidbody;
            if (otherRb != null)
            {
                otherRb.velocity *= collisionDampFactor;
            }
        }
    }
}
