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

    public static Mesh Adjust(Mesh mesh, Vector3 newOrigin, Quaternion newRotation, float scale)
    {
        Vector3[] vertices = mesh.vertices;
        Matrix4x4 matrix = Matrix4x4.TRS(newOrigin, newRotation, Vector3.one * scale);

        for (int i = 0; i < vertices.Length; i++)
        {
            // Apply the matrix transformation to each vertex
            vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        return mesh;
    }

    public static void PrintMeshDebugInfo(Mesh mesh)
    {
        string meshInfo = "Mesh Information:\n";

        // Vertices
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            meshInfo += "Vertex " + i + ": " + vertices[i] + "\n";
        }

        // Triangles
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int triIndex = i / 3;
            int vertexIndex1 = triangles[i];
            int vertexIndex2 = triangles[i + 1];
            int vertexIndex3 = triangles[i + 2];
            meshInfo += "Triangle " + triIndex + ": " + vertexIndex1 + ", " + vertexIndex2 + ", " + vertexIndex3 + "\n";
        }
    }

    //TODO: meshes need to be destroyed after combining - this may be a source of the memory leak
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

    //for now we will assume that both meshes have the same number of vertices and triangles. We can fix this later if it can't be expected.
    public static void UpdateMesh(Mesh mesh, List<Line> lines, bool looped = false)
    {
        // Assuming 'lines' and 'pointsPerLine' are class fields that are already set
        int pointsPerLine = lines[0].points.Count;
        int vertexCount = lines.Count * pointsPerLine;
        int triangleCount = (lines.Count - 1) * (pointsPerLine - (looped ? 0 : 1)) * 2 * 3;

        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[triangleCount];
        
        // Fill vertices
        int v = 0;
        foreach (Line line in lines)
        {
            for (int i = 0; i < pointsPerLine; i++)
            {
                vertices[v++] = line.points[i];
            }
        }
        
        // Fill triangles
        int t = 0;
        for (int i = 0; i < lines.Count - 1; i++)
        {
            for (int j = 0; j < pointsPerLine - (looped ? 0 : 1); j++)
            {
                triangles[t * 3] = i * pointsPerLine + j;
                triangles[t * 3 + 1] = (i + 1) * pointsPerLine + (j + 1) % pointsPerLine;
                triangles[t * 3 + 2] = i * pointsPerLine + (j + 1) % pointsPerLine;

                triangles[t * 3 + 3] = i * pointsPerLine + j;
                triangles[t * 3 + 4] = (i + 1) * pointsPerLine + j;
                triangles[t * 3 + 5] = (i + 1) * pointsPerLine + (j + 1) % pointsPerLine;

                t += 2;
            }
        }

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}

public class Face{
    public List<Line> lines;
    public int pointsPerLine;

    public Mesh mesh;

    public Mesh MakeMesh(bool looped = false){
        Vector3[] vertices = new Vector3[lines.Count * pointsPerLine];
        int[] triangles = new int[6 * (pointsPerLine - (looped ? 0 : 1)) * (lines.Count - 1)];
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
            for(int j = 0;j<pointsPerLine - (looped ? 0 : 1);j++)
            {
                triangles[t*3] = i * pointsPerLine + j;
                triangles[t*3 + 1] = (i+1) * pointsPerLine + (j+1) % pointsPerLine;
                triangles[t*3 + 2] = i * pointsPerLine + (j + 1) % pointsPerLine;
                
                triangles[t*3 + 3] = i * pointsPerLine + j;
                triangles[t*3 + 4] = (i+1) * pointsPerLine + j;
                triangles[t*3 + 5] = (i+1) * pointsPerLine + (j + 1) % pointsPerLine;

                t += 2;
            }
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}

public class Line{
    public List<Vector3> points;
    public Line(List<Vector3> points){
        this.points = points;
    }
    public Line(){}
}