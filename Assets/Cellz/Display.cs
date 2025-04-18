using UnityEngine;

public class Display : MonoBehaviour
{
    public int width = 16;
    public int height = 16;
    public Camera mainCamera;

    private Texture2D texture;
    private SpriteRenderer spriteRenderer;

    // We'll create these in Awake so we can blit black:
    private Texture2D blackTex;      // 1×1 black texture
    private RenderTexture rt;        // same size as 'texture'

    void Awake()
    {
        if (mainCamera == null)
        {
            Debug.LogError("Display must be attached to a GameObject with a Camera component.");
            return;
        }

        // Set camera to orthographic mode
        mainCamera.orthographic = true;

        // Create a new Texture2D with specified dimensions
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        // Create a new GameObject to display the texture
        GameObject displayObject = new GameObject("DisplayTexture");
        displayObject.transform.parent = transform;
        spriteRenderer = displayObject.AddComponent<SpriteRenderer>();

        // Create a sprite and set it to the SpriteRenderer
        spriteRenderer.sprite = Sprite.Create(texture,
                                new Rect(0, 0, width, height),
                                new Vector2(0.5f, 0.5f), 1);
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = new Vector2(width, height);

        // Adjust camera to fit the texture exactly
        FitTextureToScreen();

        // ----------------------------------------------------
        // Create our 1×1 black texture
        blackTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        blackTex.SetPixel(0, 0, Color.black);
        blackTex.Apply();

        // Create a RenderTexture of the same dimensions
        rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Clamp;
        // ----------------------------------------------------
    }

    public int GetWidth() => width;
    public int GetHeight() => height;

    /// <summary>
    /// Blits the 1×1 black texture onto rt, then
    /// copies rt back into our main texture, making it fully black.
    /// </summary>
    public void Clear()
    {
        // 1) Remember the old render target
        RenderTexture oldRT = RenderTexture.active;

        // 2) Set our rt as active, then Blit the blackTex onto it
        RenderTexture.active = rt;
        Graphics.Blit(blackTex, rt);

        // 3) Now read back from rt into our Texture2D
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();

        // 4) Restore the old render target
        RenderTexture.active = oldRT;
    }

    public void SetPixel(int x, int y, Color color)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            texture.SetPixel(x, y, color);
        }
    }

    public Vector2Int? TranslateMouseToTextureCoordinates()
    {
        // Get mouse position in screen space
        Vector3 mouseScreenPos = Input.mousePosition;

        // Convert to world coordinates
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, -mainCamera.transform.position.z)
        );

        // Get the bounds of the displayed texture
        Bounds spriteBounds = spriteRenderer.bounds;

        // Check if the mouse is within the bounds of the texture
        if (!spriteBounds.Contains(mouseWorldPos))
        {
            return null; // Mouse is outside the rendered texture
        }

        // Convert world position to local position relative to the sprite's min
        Vector3 localPos = mouseWorldPos - spriteBounds.min;

        // Normalize the local position to the texture size
        float normalizedX = localPos.x / spriteBounds.size.x;
        float normalizedY = localPos.y / spriteBounds.size.y;

        // Convert to texture coordinates
        int texX = Mathf.FloorToInt(normalizedX * width);
        int texY = Mathf.FloorToInt(normalizedY * height);

        // Ensure the result is within bounds
        texX = Mathf.Clamp(texX, 0, width - 1);
        texY = Mathf.Clamp(texY, 0, height - 1);

        return new Vector2Int(texX, texY);
    }

    public void Render()
    {
        texture.Apply();
    }

    void Update()
    {
        // Example usage: just logging the mouse pixel coords
        // Debug.Log(TranslateMouseToTextureCoordinates());
    }

    private void FitTextureToScreen()
    {
        float screenAspect = (float)Screen.width / Screen.height;
        float textureAspect = (float)width / height;

        if (screenAspect >= textureAspect)
        {
            mainCamera.orthographicSize = height / 2f;
        }
        else
        {
            mainCamera.orthographicSize = (width / 2f) / screenAspect;
        }

        mainCamera.transform.position = new Vector3(0, 0, -10);
    }
}
