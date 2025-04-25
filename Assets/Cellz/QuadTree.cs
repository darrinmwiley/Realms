using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadTree
{
    // Future spatial-partitioning logic would live here.
}

public class BBox
{
    public Vector2 tl;    // Top-left
    public Vector2 size;  // Width and Height

    public Vector2 BottomRight => tl + new Vector2(size.x, -size.y);

    public bool Inside(BBox other)
    {
        Vector2 thisBR = this.BottomRight;
        Vector2 otherBR = other.BottomRight;

        return tl.x >= other.tl.x &&
               tl.y <= other.tl.y &&
               thisBR.x <= otherBR.x &&
               thisBR.y >= otherBR.y;
    }

    public bool Intersects(BBox other)
    {
        Vector2 thisBR = this.BottomRight;
        Vector2 otherBR = other.BottomRight;

        return !(thisBR.x <= other.tl.x ||
                 tl.x >= otherBR.x ||        
                 thisBR.y <= other.tl.y ||   
                 tl.y >= otherBR.y);     
    }

    public bool Contains(BBox other)
    {
        return other.Inside(this);
    }

    public string ToString()
    {
        return $"BBox(tl: {tl}, size: {size})";
    }
}

public class QuadTreeValue
{
    public BBox bbox { get; set; }
    public LinkedListNode<QuadTreeValue> linkedListNode = null;
    public QuadTreeNode quadTreeNode = null;

    public QuadTreeValue(Vector2 tl, Vector2 size)
    {
        bbox = new BBox { tl = tl, size = size };
    }

    public void UpdateTree()
    {
        if(quadTreeNode != null)
        {
            quadTreeNode.UpdateValue(this);
        }
    }

    public void Draw(Display display, Vector2 displayTopLeft, Vector2 displaySize)
    {
        int texW = display.GetWidth();
        int texH = display.GetHeight();

        float dLeft   = displayTopLeft.x;
        float dTop    = displayTopLeft.y;
        float dBottom = dTop - displaySize.y;

        float nLeft   = bbox.tl.x;
        float nTop    = bbox.tl.y;
        float nRight  = nLeft + bbox.size.x;
        float nBottom = nTop  - bbox.size.y;

        // Early-out if completely off-screen
        if (nRight < dLeft || nLeft > dLeft + displaySize.x ||
            nBottom > dTop || nTop < dBottom){
                return;
        }

        // world→pixel helpers  (y=0 at bottom row)
        float WX2PX(float wx) => (wx - dLeft)   / displaySize.x * (texW - 1);
        float WY2PY(float wy) => (wy - dBottom) * (texH - 1) / displaySize.y;

        // Use FLOOR for left/bottom, CEIL for right/top
        int pxL = Mathf.FloorToInt(WX2PX(nLeft));
        int pxR = Mathf.CeilToInt (WX2PX(nRight));
        int pyB = Mathf.FloorToInt(WY2PY(nBottom));
        int pyT = Mathf.CeilToInt (WY2PY(nTop));

        // Clamp to the texture
        pxL = Mathf.Clamp(pxL, 0, texW - 1);
        pxR = Mathf.Clamp(pxR, 0, texW - 1);
        pyB = Mathf.Clamp(pyB, 0, texH - 1);
        pyT = Mathf.Clamp(pyT, 0, texH - 1);

        // horizontal edges
        for (int x = pxL; x <= pxR; ++x)
        {
            display.SetPixel(x, pyB, Color.blue); // bottom
            display.SetPixel(x, pyT, Color.blue); // top
        }

        // vertical edges
        for (int y = pyB + 1; y <= pyT - 1; ++y)
        {
            display.SetPixel(pxL, y, Color.blue); // left
            display.SetPixel(pxR, y, Color.blue); // right
        }
    }
}

/// <summary>
/// Represents one node in a QuadTree. The node is axis‑aligned and defined by its
/// <c>topLeft</c> corner and its <c>size</c> (width × height) in *world* units.
/// </summary>
public class QuadTreeNode
{
    public Vector2 topLeft;   // world‑space top‑left corner
    public Vector2 size;      // (width , height) in world units
    BBox bbox;

    public LinkedList<QuadTreeValue> values = new LinkedList<QuadTreeValue>(); 

    public BBox[] quadrantBBoxes;

    public int setChildren = 0;
    public QuadTreeNode[] children = {null, null, null, null};

    public QuadTreeNode parent = null;

    public QuadTreeNode root = null;
    public int TOP_LEFT = 0;
    public int TOP_RIGHT = 1;
    public int BOTTOM_LEFT = 2;
    public int BOTTOM_RIGHT = 3;

    public int orientation = -1;

    public QuadTreeNode(Vector2 topLeft, Vector2 size, int orientation = -1, QuadTreeNode parent = null)
    {
        this.parent = parent;
        this.orientation = orientation;
        if(parent == null)
        {
            root = this;
        }else
        {
            root = parent.root;
        }
        this.topLeft = topLeft;
        this.size    = size;
        this.bbox    = new BBox { tl = topLeft, size = size };
        this.quadrantBBoxes = new BBox[4]{
            new BBox { tl = topLeft, size = size / 2 }, // Top Left
            new BBox { tl = topLeft + new Vector2(size.x / 2, 0), size = size / 2 }, // Top Right
            new BBox { tl = topLeft + new Vector2(0, -size.y / 2), size = size / 2 }, // Bottom Left
            new BBox { tl = topLeft + new Vector2(size.x / 2, -size.y / 2), size = size / 2 } // Bottom Right
        };
    }

    public void Add(QuadTreeValue value)
    {
        // Check if the value is outside the bounds of this node
        if (!value.bbox.Inside(bbox))
        {
            if(parent == null)
            {
                Debug.Log("need to resize, "+ value.bbox.ToString() + " not inside " + bbox.ToString());
                return; // Value is outside the bounds, do nothing
            }
            parent.Add(value);
        }
        for(int i = 0;i<4;i++)
        {
            if(value.bbox.Inside(quadrantBBoxes[i]))
            {
                if(children[i] == null)
                {
                    children[i] = new QuadTreeNode(quadrantBBoxes[i].tl, quadrantBBoxes[i].size, i, this);
                    setChildren++;
                }
                children[i].Add(value);
                return;
            }
        }
        value.linkedListNode = values.AddLast(value);
        value.quadTreeNode = this;
    }

    public void UpdateValue(QuadTreeValue value)
    {
        Remove(value);
        root.Add(value);
        if(setChildren == 0 && values.Count == 0)
        {
            Destroy();
        }
        /*if(!bbox.Contains(value.bbox))
        {
            Remove(value);
            if(parent != null)
                parent.Add(value);
            //else need to resize - TODO
            if(setChildren == 0 && values.Count == 0)
            {
                Destroy();
            }
        }
        for(int i = 0;i<4;i++)
        {
            if(value.bbox.Inside(quadrantBBoxes[i]))
            {
                if(children[i] == null)
                {
                    children[i] = new QuadTreeNode(quadrantBBoxes[i].tl, quadrantBBoxes[i].size, i, this);
                    setChildren++;
                }
                Remove(value);
                children[i].Add(value);
            }
        }*/
    }

    public void Remove(QuadTreeValue value)
    {
        if(value.linkedListNode != null)
        {
            values.Remove(value.linkedListNode);
            value.linkedListNode = null;
        }
        if(value.quadTreeNode != null)
        {
            value.quadTreeNode = null;
        }
    }

    public void Destroy()
    {
        for(int i = 0;i<4;i++)
        {
            if(children[i] != null)
            {
                children[i].Destroy();
                children[i] = null;
            }
        }
        if(parent != null)
        {
            parent.children[TOP_LEFT] = null;
            parent.children[TOP_RIGHT] = null;
            parent.children[BOTTOM_LEFT] = null;
            parent.children[BOTTOM_RIGHT] = null;
        }
        parent.children[orientation] = null;
        parent.setChildren--;
        if(parent.setChildren == 0 && parent.values.Count == 0)
        {
            parent.Destroy();
        }
    }

    /// <summary>
    /// Draws a 1‑pixel‑wide white outline that shows the bounds of this node on the
    /// supplied <paramref name="display"/>.
    /// </summary>
    /// <param name="display">Target Display to draw pixels on.</param>
    /// <param name="displayTopLeft">World‑space position of the display’s top‑left corner.</param>
    /// <param name="displaySize">World‑space size (width, height) that the display covers.</param>
    public void Draw(Display display, Vector2 displayTopLeft, Vector2 displaySize)
    {
        foreach(QuadTreeValue value in values)
        {
            value.Draw(display, displayTopLeft, displaySize);
        }
        for(int i = 0;i<4;i++)
        {
            if(children[i] != null)
            {
                children[i].Draw(display, displayTopLeft, displaySize);
            }
        }
        int texW = display.GetWidth();
        int texH = display.GetHeight();

        float dLeft   = displayTopLeft.x;
        float dTop    = displayTopLeft.y;
        float dBottom = dTop - displaySize.y;

        float nLeft   = topLeft.x;
        float nTop    = topLeft.y;
        float nRight  = nLeft + size.x;
        float nBottom = nTop  - size.y;

        // Early-out if completely off-screen
        if (nRight < dLeft || nLeft > dLeft + displaySize.x ||
            nBottom > dTop || nTop < dBottom) return;

        // world→pixel helpers  (y=0 at bottom row)
        float WX2PX(float wx) => (wx - dLeft)   / displaySize.x * (texW - 1);
        float WY2PY(float wy) => (wy - dBottom) * (texH - 1) / displaySize.y;

        // Use FLOOR for left/bottom, CEIL for right/top
        int pxL = Mathf.FloorToInt(WX2PX(nLeft));
        int pxR = Mathf.CeilToInt (WX2PX(nRight));
        int pyB = Mathf.FloorToInt(WY2PY(nBottom));
        int pyT = Mathf.CeilToInt (WY2PY(nTop));

        // Clamp to the texture
        pxL = Mathf.Clamp(pxL, 0, texW - 1);
        pxR = Mathf.Clamp(pxR, 0, texW - 1);
        pyB = Mathf.Clamp(pyB, 0, texH - 1);
        pyT = Mathf.Clamp(pyT, 0, texH - 1);

        // horizontal edges
        for (int x = pxL; x <= pxR; ++x)
        {
            display.SetPixel(x, pyB, Color.white); // bottom
            display.SetPixel(x, pyT, Color.white); // top
        }

        // vertical edges
        for (int y = pyB + 1; y <= pyT - 1; ++y)
        {
            display.SetPixel(pxL, y, Color.white); // left
            display.SetPixel(pxR, y, Color.white); // right
        }
    }

}
