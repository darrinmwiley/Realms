using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshFromSprite : MonoBehaviour
{
    public Sprite sprite;
    void Start()
    {
        GetComponent<MeshCollider>().sharedMesh = GenerateMeshFromSprite(sprite);
    }

    private Mesh GenerateMeshFromSprite(Sprite sprite)
    {
        // Extract the texture from the sprite
        Texture2D texture = sprite.texture;

        // Get sprite dimensions and pixel data
        Rect spriteRect = sprite.rect;
        int textureWidth = (int)spriteRect.width;
        int textureHeight = (int)spriteRect.height;
        Color[] pixels = texture.GetPixels((int)spriteRect.x, (int)spriteRect.y, textureWidth, textureHeight);

        // Create lists to store mesh data
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int index = 0;

        // Loop through each pixel in the texture
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                Color pixel = pixels[y * textureWidth + x];

                // Skip transparent pixels
                if (pixel.a < 0.1f)
                    continue;

                // Add vertices for this pixel's quad (clockwise)
                vertices.Add(new Vector3(x, y, 0));        // Bottom-left
                vertices.Add(new Vector3(x + 1, y, 0));    // Bottom-right
                vertices.Add(new Vector3(x + 1, y + 1, 0)); // Top-right
                vertices.Add(new Vector3(x, y + 1, 0));    // Top-left

                // Define triangles (two per quad)
                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 1);

                triangles.Add(index);
                triangles.Add(index + 3);
                triangles.Add(index + 2);

                // UV mapping
                Vector2 uvBottomLeft = new Vector2((float)x / textureWidth, (float)y / textureHeight);
                Vector2 uvBottomRight = new Vector2((float)(x + 1) / textureWidth, (float)y / textureHeight);
                Vector2 uvTopRight = new Vector2((float)(x + 1) / textureWidth, (float)(y + 1) / textureHeight);
                Vector2 uvTopLeft = new Vector2((float)x / textureWidth, (float)(y + 1) / textureHeight);

                uvs.Add(uvBottomLeft);
                uvs.Add(uvBottomRight);
                uvs.Add(uvTopRight);
                uvs.Add(uvTopLeft);

                index += 4; // Move to the next group of vertices
            }
        }

        // Create the mesh and assign the vertices, triangles, and UVs
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        // Recalculate normals and bounds for correct rendering
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
