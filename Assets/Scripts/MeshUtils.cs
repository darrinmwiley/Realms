using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshUtils
{
    public static void Flip(Mesh mesh)
    {
        Vector3[] normals = mesh.normals;

            // Invert each normal
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = -normals[i];
            }
            mesh.normals = normals;

            // Get triangles array from the mesh
            for (int m = 0; m < mesh.subMeshCount; m++)
            {
                int[] triangles = mesh.GetTriangles(m);

                // Reverse triangle winding
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    // Swap order of triangle vertices
                    int temp = triangles[i];
                    triangles[i] = triangles[i + 2];
                    triangles[i + 2] = temp;
                }
                mesh.SetTriangles(triangles, m);
            }
    }

    public static Mesh Combine(params Mesh[] meshes)
    {
        CombineInstance[] combine = new CombineInstance[meshes.Length];
        for (int i = 0; i < meshes.Length; i++)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = Matrix4x4.identity; // Use identity matrix, since we're not transforming the meshes
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);
        return combinedMesh;
    }
}

public class Face{
    public List<Line> lines;
    public int pointsPerLine;

    public Mesh MakeMesh(){
        Vector3[] vertices = new Vector3[lines.Count * pointsPerLine];
        int[] triangles = new int[6 * (pointsPerLine - 1) * (lines.Count - 1)];
        int v = 0;
        foreach(Line line in lines)
        {
            for(int i = 0;i<pointsPerLine;i++)
            {
                vertices[v++] = line.points[i];
            }
        }
        int t = 0;
        for(int i = 0;i<lines.Count - 1;i++)
        {
            for(int j = 0;j<pointsPerLine - 1;j++)
            {
                triangles[t*3] = i * pointsPerLine + j;
                triangles[t*3 + 1] = (i+1) * pointsPerLine + j+1;
                triangles[t*3 + 2] = i * pointsPerLine + j;
             /*   triangles[t*3] = i*2;
                triangles[t*3 + 1] = (i+1) * 2 + 1;
                triangles[t*3 + 2] = i*2 + 1;

                triangles[t*3 + 3] = i*2;
                triangles[t*3 + 4] = (i+1) * 2;
                triangles[t*3 + 5] = (i+1) * 2 + 1;

                t+=2;*/
            }
        }

        for(int i = 0;i<vertices.Length;i++)
        {
            Debug.Log(i+" "+vertices[i]);
        }

        string str = "";
        foreach( int x in triangles)
            str+=x+" ";
        Debug.Log(str);

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}

public class Line{
    public List<Vector3> points;
}