using UnityEngine;

public class Display : MonoBehaviour
{
    public int width = 16;
    public int height = 16;
    public Camera mainCamera;

    // CPU‐side texture
    private Texture2D texture;
    // GPU RenderTexture
    public RenderTexture rt;
    // 1×1 black tex for clearing RT
    private Texture2D blackTex;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        if (mainCamera == null)
        {
            Debug.LogError("Display requires a Camera reference.");
            return;
        }

        // 1) Orthographic camera
        mainCamera.orthographic = true;

        // 2) Create CPU Texture2D
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp
        };

        // 3) Create GameObject + SpriteRenderer to show it
        var go = new GameObject("DisplayTexture");
        go.transform.parent = transform;
        spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            1f
        );
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size     = new Vector2(width, height);

        FitTextureToScreen();

        // 4) Black tex for clearing RT
        blackTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        blackTex.SetPixel(0, 0, Color.black);
        blackTex.Apply();

        // 5) Create GPU RenderTexture
        rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            filterMode        = FilterMode.Point,
            wrapMode          = TextureWrapMode.Clamp
        };
        rt.Create();
    }

    public int GetWidth()  => width;
    public int GetHeight() => height;

    // —— CPU path methods —— //

    /// <summary> Zero out the CPU Texture2D. </summary>
    public void Clear()
    {
        var blacks = new Color32[width * height];
        for (int i = 0; i < blacks.Length; i++) blacks[i] = Color.black;
        texture.SetPixels32(blacks);
    }

    /// <summary> Paint a single pixel (for your CPU DrawVoronoiToDisplay). </summary>
    public void SetPixel(int x, int y, Color color)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            texture.SetPixel(x, y, color);
    }

    /// <summary> Push all SetPixel edits to the screen. </summary>
    public void Render()
    {
        texture.Apply();
    }

    // —— GPU path methods —— //

    /// <summary> Clear the GPU RT to black. </summary>
    public void ClearRT()
    {
        var old = RenderTexture.active;
        RenderTexture.active = rt;
        Graphics.Blit(blackTex, rt);
        RenderTexture.active = old;
    }

    /// <summary> Read the GPU RT into our CPU Texture2D. </summary>
    public void PullRTToTexture()
    {
        var old = RenderTexture.active;
        RenderTexture.active = rt;
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        RenderTexture.active = old;
    }

    // —— Shared utility —— //

    /// <summary> Convert mouse pos → texture coords. </summary>
    public Vector2Int? TranslateMouseToTextureCoordinates()
    {
        var ms = Input.mousePosition;
        var mw = mainCamera.ScreenToWorldPoint(
            new Vector3(ms.x, ms.y, -mainCamera.transform.position.z)
        );
        var b = spriteRenderer.bounds;
        if (!b.Contains(mw)) return null;

        var local = mw - b.min;
        float nx = local.x / b.size.x;
        float ny = local.y / b.size.y;
        int px = Mathf.Clamp(Mathf.FloorToInt(nx * width), 0, width - 1);
        int py = Mathf.Clamp(Mathf.FloorToInt(ny * height), 0, height - 1);
        return new Vector2Int(px, py);
    }

    private void FitTextureToScreen()
    {
        float screenAspect  = (float)Screen.width  / Screen.height;
        float textureAspect = (float)width        / height;
        if (screenAspect >= textureAspect)
            mainCamera.orthographicSize = height  / 2f;
        else
            mainCamera.orthographicSize = (width / 2f) / screenAspect;
        mainCamera.transform.position = new Vector3(0, 0, -10);
    }

    void Update()
    {
        // Debug.Log(TranslateMouseToTextureCoordinates());
    }
}
