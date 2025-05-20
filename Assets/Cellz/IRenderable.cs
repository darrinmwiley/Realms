using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRenderable
{
    void Render(RenderTexture rt, Display display);
}
