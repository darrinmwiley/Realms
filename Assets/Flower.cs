using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flower : MonoBehaviour
{
    public Material yellow;
    public Material purple;
    public AnimationCurve petalWidthCurve;

    GameObject center;

    public float centerHeight = .5f;
    public float centerRadius = 5;
    public float petalLength = 4;
    public int numPetals = 4;

    /*petal ring parameters:
        petal length
        number of petals
        petal width curve
        petal thickness curve
        petal colors
        petal X angle
    */

    public class PetalRing{
        public float petalLength;
        public int numPetals;
        public AnimationCurve petalWidthCurve;
        public AnimationCurve petalThicknessCurve;
        public float petalThicknessMultiplier;
        //this is how "open" the flower is. 0 is vertical, 90 is horizontal
        public float angleX;
        //this is how far from perpendicular to radius we tilt
        public float angleY;
    }

    public class PetalConfig{
        public float petalLength;
        public AnimationCurve petalWidthCurve;
        public float petalWidthMultiplier;
        public AnimationCurve petalThicknessCurve;
        public float petalThicknessMultiplier;
        //this is the angle on the flower
        public float centerAngle;
        //normalized - 0 = center, 1 = edge
        public float distanceFromCenter;
        //this is how "open" the flower is. 0 is vertical, 90 is horizontal
        public float angleX;
        //this is how far from perpendicular to radius we tilt
        public float angleY;
    }

    PetalRing defaultPetalRing = new PetalRing(){
        petalLength = 4,
        numPetals = 8,
        petalWidthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),   // Start at time 0 with value 1.
                new Keyframe(1f, 0f)    // End at time 1 with value 0.
        ),
        petalThicknessCurve = new AnimationCurve(
                new Keyframe(0f, 1f),   // Start at time 0 with value 1.
                new Keyframe(1f, 0f)    // End at time 1 with value 0.
        ),
        petalThicknessMultiplier = .15f,
        angleX = 45,
        angleY = 0
    };

    PetalRing secondPetalRing = new PetalRing(){
        petalLength = 8,
        numPetals = 6,
        petalWidthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),   // Start at time 0 with value 1.
                new Keyframe(1f, 0f)    // End at time 1 with value 0.
        ),
        petalThicknessCurve = new AnimationCurve(
                new Keyframe(0f, 1f),   // Start at time 0 with value 1.
                new Keyframe(1f, 1f)    // End at time 1 with value 0.
        ),
        petalThicknessMultiplier = .15f,
        angleX = 75,
        angleY  = 0,
    };

    private List<PetalRing> petalRings = new List<PetalRing>();
    private List<PetalConfig> petals = new List<PetalConfig>();

    // Start is called before the first frame update
    void Start()
    {
        center = new GameObject("center");
        Mesh centerMesh = MakeCenterMesh(centerHeight, centerRadius, 25, 10);
        center.AddComponent<MeshFilter>();
        center.AddComponent<MeshRenderer>().material = yellow;
        petalRings.Add(defaultPetalRing);
        //petalRings.Add(secondPetalRing);
        /*int numPetals = 100;
        float startAngle = 90;
        float endAngle = 0;
        float dtheta = 5;
        for(int i = 0;i<numPetals;i++)
        {
            petals.Add(new PetalConfig(){
            petalLength = 4,
            petalWidthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),   // Start at time 0 with value 1.
                new Keyframe(1f, 0f)    // End at time 1 with value 0.
            ),
            petalWidthMultiplier = 1,
            petalThicknessCurve = new AnimationCurve(
                    new Keyframe(0f, 1f),   // Start at time 0 with value 1.
                    new Keyframe(1f, 1f)    // End at time 1 with value 0.
            ),
            centerAngle = (dtheta * i) % 360,
            petalThicknessMultiplier = .15f,
            distanceFromCenter = Mathf.Lerp(1, 0, i / (numPetals+0f)),
            angleX = 75,
            angleY = 0
        });
        }*/
        RegenerateMeshes();
    }

    void Update()
    {
        //RegenerateMeshes();
    }

    void RegenerateMeshes()
    {
        /*for(int i = petals.Count - 1;i>=0;i--)
        {
            GameObject.Destroy(petals[i]);
        }*/
        center.GetComponent<MeshFilter>().mesh = MakeCenterMesh(centerHeight, centerRadius, 25, 10);
        foreach(PetalConfig config in petals)
        {
            float xNorm = Mathf.Cos(config.centerAngle * Mathf.Deg2Rad) * config.distanceFromCenter;
            float zNorm = Mathf.Sin(config.centerAngle * Mathf.Deg2Rad) * config.distanceFromCenter;
            float yNorm = GetHeightOnCenter(xNorm, zNorm);
            float x = xNorm * centerRadius;
            float z = zNorm * centerRadius;
            GameObject petalObj = new GameObject("petal");
            petalObj.transform.position = new Vector3(x,yNorm * centerHeight,z);
            Petal petal = petalObj.AddComponent<Petal>();
            petal.petalWidthMultiplier = config.petalWidthMultiplier;
            petal.petalThicknessMultiplier = config.petalThicknessMultiplier;
            petal.petalHeight = config.petalLength;
            petal.petalWidthCurve = config.petalWidthCurve;
            petal.petalThicknessCurve = config.petalThicknessCurve;
            petal.RegenerateMesh();
            petal.SetMaterial(purple);
            petal.transform.rotation = Quaternion.Euler(config.angleX,90 - config.centerAngle, 0);

        }
        foreach(PetalRing petalRing in petalRings)
        {
            int numPetals = petalRing.numPetals;
            for(int i = 0;i<numPetals;i++)
            {
                float x1 = centerRadius * Mathf.Cos(Mathf.PI * 2 * (i-.5f) / numPetals);
                float z1 = centerRadius * Mathf.Sin(Mathf.PI * 2 * (i-.5f) / numPetals);
                float x2 = centerRadius * Mathf.Cos(Mathf.PI * 2 * (i+.5f) / numPetals);
                float z2 = centerRadius * Mathf.Sin(Mathf.PI * 2 * (i+.5f) / numPetals);
                float width = Vector2.Distance(new Vector2(x1, z1), new Vector2(x2, z2));
                float middleDeg = 360 * i / numPetals;
                GameObject petalObj = new GameObject("petal "+i);
                petalObj.transform.position = new Vector3((x1 + x2) / 2,0,(z1 + z2) / 2);
                Petal petal = petalObj.AddComponent<Petal>();
                petal.petalWidthMultiplier = width;
                petal.petalThicknessMultiplier = petalRing.petalThicknessMultiplier;
                petal.petalHeight = petalRing.petalLength;
                petal.petalWidthCurve = petalRing.petalWidthCurve;
                petal.petalThicknessCurve = petalRing.petalThicknessCurve;
                petal.RegenerateMesh();
                petal.SetMaterial(purple);
                petal.transform.rotation = Quaternion.Euler(petalRing.angleX,90 - middleDeg, 0);
            }
        }
    }

    public float GetHeightOnCenter(float xNorm, float zNorm)
    {
        return Mathf.Sqrt(1 - xNorm*xNorm - zNorm*zNorm);
    }

    float epsilon = .001f;

    //assume samplesPerSlice at least 2
    public Mesh MakeCenterMesh(float height, float radius, int slices, int samplesPerSlice)
    {
        Vector3[] vertices = new Vector3[slices * (samplesPerSlice - 1) + 1];
        float dxy = radius / (samplesPerSlice - 1);
        float dThetaSlice = 2 * Mathf.PI / slices;
        vertices[0] = new Vector3(0,height,0);
        for(int i = 0;i<samplesPerSlice - 1;i++)
        {
            for(int j = 0;j<slices;j++)
            {
                float x = Mathf.Cos(dThetaSlice * j) * dxy * (i+1);
                float z = Mathf.Sin(dThetaSlice * j) * dxy * (i+1);
                float sphereY2 = radius * radius - x * x - z * z;
                if(sphereY2 < epsilon)
                    sphereY2 = 0;
                float sphereY = Mathf.Sqrt(sphereY2);
                float normalizedY = sphereY * height / radius;
                vertices[i*slices + j + 1] = new Vector3(x,normalizedY,z);
            }
        }
        int[] triangles = new int[3 * slices * (2 * samplesPerSlice - 2)];
        int tri = 0;
        for(int i = 0;i<slices;i++)
        {
            triangles[tri * 3] = 0;
            triangles[tri * 3 + 2] = i + 1;
            triangles[tri * 3 + 1] = (i + 1) % slices + 1;

            tri++; 
        }
        for(int s = 1;s<samplesPerSlice - 1;s++)
        {
            for(int i = 0;i<slices;i++)
            {
                triangles[tri * 3] = i + 1 + (slices * (s - 1));
                triangles[tri * 3 + 1] = (i + 1) % slices + 1 + (slices * (s - 1));
                triangles[tri * 3 + 2] = triangles[tri * 3 + 1] + slices;

                triangles[tri * 3 + 3] = triangles[tri * 3];
                triangles[tri * 3 + 5] = triangles[tri * 3] + slices;
                triangles[tri * 3 + 4] = triangles[tri*3 + 2];

                tri += 2;
            }
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    public Mesh MakePetalMesh(float radius, float angleWidth, float centerAngle, float petalLength, int petalSamples)
    {
        Vector3[] vertices = new Vector3[petalSamples * 3 + 1];
        int[] triangles = new int[3 * (4 * (petalSamples - 1) + 2)];
        float p1x = Mathf.Cos(centerAngle + angleWidth / 2) * radius;
        float p1z = Mathf.Sin(centerAngle + angleWidth / 2) * radius;
        float p2x = Mathf.Cos(centerAngle - angleWidth / 2) * radius;
        float p2z = Mathf.Sin(centerAngle - angleWidth / 2) * radius;
        float tipz = Mathf.Sin(centerAngle) * (petalLength + radius);
        float tipx = Mathf.Cos(centerAngle) * (radius + petalLength);
        float dxz = petalLength / petalSamples;
        float tangentX = Mathf.Cos(centerAngle) * radius;
        float tangentZ = Mathf.Sin(centerAngle) * radius;
        Vector3 directionVector = new Vector3(tangentX, 0, tangentZ);
        Vector3 perpendicular = new Vector3(-directionVector.z, directionVector.y, directionVector.x);
        perpendicular.Normalize();
        vertices[0] = new Vector3(p1x, 0,p1z);
        vertices[1] = directionVector;
        vertices[2] = new Vector3(p2x, 0, p2z);
        float d0 = Vector3.Distance(vertices[0], vertices[2]);
        int v = 3;
        for(int i = 1;i<petalSamples;i++)
        {
            float distance = d0 / 2 * petalWidthCurve.Evaluate((1f / petalSamples)*i);
            float centerX = Mathf.Cos(centerAngle) * (radius + dxz * i);
            float centerZ = Mathf.Sin(centerAngle) * (radius + dxz * i);
            Vector3 center = new Vector3(centerX, 0, centerZ);
            vertices[v++] = center + distance * perpendicular;;
            vertices[v++] = center;
            vertices[v++] = center - distance * perpendicular;
        }
        vertices[vertices.Length - 1] = new Vector3(tipx, 0, tipz);
        int t = 0;
        for(int i = 0;i<petalSamples - 1;i++)
        {
            triangles[t * 3] = 3 * i + 1;
            triangles[t * 3 + 2] = 3 * (i + 1);
            triangles[t * 3 + 1] = 3 * i;

            triangles[t * 3 + 3] = 3 * i + 1;
            triangles[t * 3 + 4] = 3 * (i+1);
            triangles[t * 3 + 5] = 3 * (i + 1) + 1;

            triangles[t * 3 + 6] = 3 * i + 1;
            triangles[t * 3 + 7] = 3 * (i + 1) + 1;
            triangles[t * 3 + 8] = 3 * i + 2;

            triangles[t * 3 + 9] = 3 * (i + 1) + 1;
            triangles[t * 3 + 10] = 3 * (i + 1) + 2;
            triangles[t * 3 + 11] = 3 * i + 2;

            t += 4;
        }

        triangles[t * 3] = vertices.Length - 4;
        triangles[t * 3 + 1] = vertices.Length - 1;
        triangles[t * 3 + 2] = vertices.Length - 3;

        triangles[t * 3 + 3] = vertices.Length - 3;
        triangles[t * 3 + 4] = vertices.Length - 1;
        triangles[t * 3 + 5] = vertices.Length - 2;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
