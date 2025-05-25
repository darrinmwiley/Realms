// Bullet.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A simple projectile that moves in a straight line and can stamp itself
/// into the Voronoi ID texture just like a Cell.
/// </summary>
public class Bullet : MonoBehaviour, IRenderable
{
    /* ─────────── Static compute‑shader cache (shared with Cell) ─────────── */

    private const string CS_PATH   = "ComputeShaders/Voronoi";
    private const string CS_KERNEL = "CSPerCell";
    private const int    TG_SIZE   = 8;

    private static ComputeShader _cs;
    private static int           _kernel;
    private static bool          _ready;
    private static ComputeBuffer _dummyNeighborBuf;

    private static void EnsureShaderLoaded()
    {
        if (_ready) return;

        _cs = Resources.Load<ComputeShader>(CS_PATH);
        if (_cs == null)
        {
            Debug.LogError($"Bullet: failed to load ComputeShader at Resources/{CS_PATH}.compute");
            return;
        }

        _kernel           = _cs.FindKernel(CS_KERNEL);
        _dummyNeighborBuf = new ComputeBuffer(1, sizeof(float) * 4);
        _dummyNeighborBuf.SetData(new Vector4[] { Vector4.zero });

        _ready = true;
    }

    /* ─────────── Bullet instance state ─────────── */

    private Rigidbody2D       rb;
    private CircleCollider2D  circleCollider;

    public Color color = Color.red;

    private float lifetime = 5f;
    private float timer;

    /* ─────────── API ─────────── */

    public void Initialize(Vector2 position, Vector2 velocity, float radius, Color bulletColor)
    {
        transform.position = position;
        color              = bulletColor;

        rb              = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.velocity     = velocity;

        circleCollider              = gameObject.AddComponent<CircleCollider2D>();
        circleCollider.radius       = radius;
        circleCollider.isTrigger    = true;

        timer = lifetime;
    }

    /* ─────────── IRenderable implementation ─────────── */

    public void Render(
        RenderTexture idRT,
        int           mappedIdPlusOne,
        float         vorX, float vorY,
        float         vorW, float vorH,
        int           texW, int texH)
    {
        EnsureShaderLoaded();
        if (!_ready) return;

        float radius = circleCollider ? circleCollider.radius : 0.5f;

        float invW = texW / vorW;
        float invH = texH / vorH;

        Vector2 pos  = transform.position;
        Vector2 wMin = pos - Vector2.one * radius;
        Vector2 wMax = pos + Vector2.one * radius;

        int minX = Mathf.Clamp(Mathf.FloorToInt((wMin.x - vorX) * invW), 0, texW);
        int minY = Mathf.Clamp(Mathf.FloorToInt((wMin.y - vorY) * invH), 0, texH);
        int maxX = Mathf.Clamp(Mathf.CeilToInt ((wMax.x - vorX) * invW), 0, texW);
        int maxY = Mathf.Clamp(Mathf.CeilToInt ((wMax.y - vorY) * invH), 0, texH);

        int w = maxX - minX;
        int h = maxY - minY;
        if (w <= 0 || h <= 0) return; // off‑screen

        /* bullets don't worry about neighbours – supply an empty buffer */
        _cs.SetInt   ("neighborCount", 0);
        _cs.SetBuffer(_kernel, "Neighbors", _dummyNeighborBuf);

        _cs.SetVector("cellCenter", new Vector4(pos.x, pos.y, 0f, 0f));
        _cs.SetFloat ("invRadius", 1f / radius);
        _cs.SetInt   ("cellID",    mappedIdPlusOne);

        _cs.SetInts  ("minPixel",  minX, minY);
        _cs.SetInts  ("maxPixel",  maxX, maxY);
        _cs.SetFloat ("invW",      invW);
        _cs.SetFloat ("invH",      invH);
        _cs.SetFloat ("originX",   vorX);
        _cs.SetFloat ("originY",   vorY);
        _cs.SetTexture(_kernel, "IDResult", idRT);

        int gx = Mathf.CeilToInt(w / (float)TG_SIZE);
        int gy = Mathf.CeilToInt(h / (float)TG_SIZE);
        _cs.Dispatch(_kernel, gx, gy, 1);
    }

    /* ─────────── Lifetime + (future) collisions ─────────── */

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            var field = FindObjectOfType<Field>();
            if (field) field.RemoveBullet(this);
            else       Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Implement damage / effects here if desired
    }
}
