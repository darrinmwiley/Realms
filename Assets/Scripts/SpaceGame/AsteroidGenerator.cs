using UnityEngine;
using System.Collections.Generic;

public class AsteroidGenerator : MonoBehaviour
{
    public Material mat;                     // Material for asteroid
    public float asteroidRarity = 2f;        // Expected time (in seconds) between asteroids
    public float minAsteroidSpeed = 1f;      // Minimum asteroid speed
    public float maxAsteroidSpeed = 5f;      // Maximum asteroid speed
    public float spawnDistance = 50f;        // Distance from the ship to spawn asteroids
    public float areaToMassRatio = 1f;       // Mass per unit area for the asteroids
    public float maxSpin = 10f;              // Maximum initial spin (angular velocity)

    public ShipController ship;              // Reference to the ship

    private float spawnTimer;

    List<GameObject> asteroids = new List<GameObject>();

    void Start()
    {
        spawnTimer = GetRandomSpawnTime();
    }

    void Update()
    {
        if (ship == null) return; // Ensure ship is assigned

        spawnTimer -= Time.deltaTime;
        
        if (spawnTimer <= 0f)
        {
            SpawnAsteroidNearShip();
            spawnTimer = GetRandomSpawnTime(); // Reset spawn timer
        }

        for(int i = asteroids.Count - 1;i>=0;i--)
        {
            GameObject asteroid = asteroids[i];
            float dist = Vector3.Distance(asteroid.transform.position, ship.gameObject.transform.position);
            if(dist > spawnDistance * 10)
            {
                asteroids.RemoveAt(i);
                GameObject.Destroy(asteroid);
            }
        }
    }

    private float GetRandomSpawnTime()
    {
        return Random.Range(asteroidRarity * 0.5f, asteroidRarity * 1.5f); // Randomize interval
    }

    private void SpawnAsteroidNearShip()
    {
        // Calculate random position 30 meters away from the ship
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector3 spawnPosition = ship.transform.position + (Vector3)(randomDirection * spawnDistance);

        // Generate asteroid
        GameObject asteroid = GenerateAsteroid(10, 1, 3);
        asteroids.Add(asteroid);
        asteroid.transform.position = spawnPosition;

        // Assign random velocity in a random direction
        Rigidbody2D rb = asteroid.GetComponent<Rigidbody2D>();
        Vector2 asteroidVelocity = randomDirection * Random.Range(minAsteroidSpeed, maxAsteroidSpeed);
        rb.velocity = asteroidVelocity;

        // Add a random spin
        rb.angularVelocity = Random.Range(-maxSpin, maxSpin);
    }

    // Method to generate an asteroid with a procedural polygon mesh in 2D
    public GameObject GenerateAsteroid(int numPoints, float minRadius, float maxRadius)
    {
        // Create a new GameObject to hold the asteroid mesh
        GameObject asteroid = new GameObject("ProceduralAsteroid");
        asteroid.tag = "Asteroid";

        // Add necessary components
        MeshFilter meshFilter = asteroid.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = asteroid.AddComponent<MeshRenderer>();
        meshRenderer.material = mat;

        // Generate the asteroid mesh and assign it
        Mesh asteroidMesh = GenerateAsteroidMesh(numPoints, minRadius, maxRadius);
        meshFilter.mesh = asteroidMesh;

        // Add a PolygonCollider2D to match the shape of the asteroid
        PolygonCollider2D polygonCollider = asteroid.AddComponent<PolygonCollider2D>();
        polygonCollider.SetPath(0, GenerateColliderPath(asteroidMesh));

        // Add Rigidbody2D for physics interactions
        Rigidbody2D rb = asteroid.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Set gravity scale to 0 for space-like movement

        // Calculate area and set mass based on area
        float area = CalculatePolygonArea(polygonCollider.points);
        rb.mass = area * areaToMassRatio;

        return asteroid;
    }

    // Helper method to generate the asteroid mesh
    private Mesh GenerateAsteroidMesh(int numPoints, float minRadius, float maxRadius)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Generate vertices in a circular pattern with random radii
        float currentRadius = UnityEngine.Random.Range(minRadius, maxRadius);
        List<float> angles = new List<float>();
        float angleBetweenPoints = Mathf.PI * 2 / numPoints;

        for (int i = 0; i < numPoints; i++)
        {
            // Add angles with random variation
            angles.Add(angleBetweenPoints * i + UnityEngine.Random.Range(-angleBetweenPoints / 3, angleBetweenPoints / 3));
        }

        // Center vertex
        vertices.Add(Vector3.zero);

        // Generate vertices for each point in the circle
        for (int i = 0; i < numPoints; i++)
        {
            // Vary radius from previous point
            float radiusVariation = UnityEngine.Random.Range(-0.2f, 0.2f);
            currentRadius = Mathf.Clamp(currentRadius + radiusVariation, minRadius, maxRadius);

            // Calculate position of vertex in 2D (XY plane)
            float x = Mathf.Cos(angles[i]) * currentRadius;
            float y = Mathf.Sin(angles[i]) * currentRadius;
            vertices.Add(new Vector3(x, y, 0));
        }

        // Create triangles for the mesh (center to each pair of adjacent vertices)
        for (int i = 1; i <= numPoints; i++)
        {
            triangles.Add(0);                // Center vertex
            triangles.Add((i % numPoints) + 1); // Next vertex, wraps around
            triangles.Add(i);                // Current vertex
        }

        // Assign vertices and triangles to the mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals(); // Calculate normals for lighting
        mesh.RecalculateBounds();  // Calculate bounds for culling and visibility

        return mesh;
    }

    // Generate a 2D path for the PolygonCollider2D based on the mesh vertices
    private Vector2[] GenerateColliderPath(Mesh mesh)
    {
        Vector3[] meshVertices = mesh.vertices;
        Vector2[] colliderPath = new Vector2[meshVertices.Length - 1];

        // Skip the center vertex (first element), only take outer vertices
        for (int i = 1; i < meshVertices.Length; i++)
        {
            colliderPath[i - 1] = new Vector2(meshVertices[i].x, meshVertices[i].y);
        }

        return colliderPath;
    }

    // Calculate area of a polygon using the Shoelace theorem
    private float CalculatePolygonArea(Vector2[] points)
    {
        float area = 0f;
        int j = points.Length - 1; // The last vertex is the previous one to the first

        for (int i = 0; i < points.Length; i++)
        {
            area += (points[j].x + points[i].x) * (points[j].y - points[i].y);
            j = i;  // j is previous vertex to i
        }

        return Mathf.Abs(area / 2f); // Return absolute value of the area
    }
}
