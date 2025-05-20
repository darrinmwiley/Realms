// IRenderable.cs

using UnityEngine;

/// <summary>
/// Something that can write its own pixels into an ID render-texture for the
/// Voronoi pass.  The Field passes down screen + window parameters each frame.
/// </summary>
public interface IRenderable
{
    /// <param name="idRT">Integer RenderTexture where each pixel stores the “winning” cell index.</param>
    /// <param name="mappedIdPlusOne">0 = background, 1–N = compact cell index.</param>
    /// <param name="vorX">World-space left edge of the Voronoi window.</param>
    /// <param name="vorY">World-space bottom edge of the Voronoi window.</param>
    /// <param name="vorW">World-space width  of the Voronoi window.</param>
    /// <param name="vorH">World-space height of the Voronoi window.</param>
    /// <param name="texW">RenderTexture width  in pixels.</param>
    /// <param name="texH">RenderTexture height in pixels.</param>
    void Render(
        RenderTexture idRT,
        int           mappedIdPlusOne,
        float         vorX, float vorY,
        float         vorW, float vorH,
        int           texW, int texH
    );
}
