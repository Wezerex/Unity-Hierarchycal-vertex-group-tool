using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[InitializeOnLoad]
public class BillboardVertexRenderer
{
    private static Mesh quadMesh;

    private static int batchSize = 1023;

    private static Material instancedMaterial;
    private static MeshFilter cachedFilter;
    private static Vector3[] cachedVertices;
    private static Dictionary<int, Matrix4x4[]> cachedBatches = new();

    [InitializeOnLoadMethod]
    private static void InitializeResources()
    {
        quadMesh = CreateQuad();

        instancedMaterial = new Material(Shader.Find("Custom/BillboardVertex"));
        instancedMaterial.enableInstancing = true;
    }

    public static void DrawVertexIndices(MeshFilter meshFilter, IEnumerable<int> vertexIndices, Color color, float size)
    {
        if (meshFilter == null || meshFilter.sharedMesh == null || vertexIndices == null 
            || instancedMaterial == null || quadMesh == null)
            return;

        if (cachedFilter != meshFilter)
        {
            cachedFilter = meshFilter;
            cachedVertices = meshFilter.sharedMesh.vertices;
            cachedBatches.Clear();
        }

        List<int> indices = vertexIndices.ToList();
        int totalVertices = indices.Count;
        int batches = Mathf.CeilToInt((float)totalVertices / batchSize);

        RenderParams rp = new RenderParams(instancedMaterial)
        {
            worldBounds = meshFilter.GetComponent<Renderer>().bounds,
            layer = meshFilter.gameObject.layer
        };

        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_Color", color);
        rp.matProps = props;

        for (int i = 0; i < batches; i++)
        {
            int start = i * batchSize;
            int end = Mathf.Min(start + batchSize, totalVertices);
            int count = end - start;

            if (!cachedBatches.TryGetValue(i, out Matrix4x4[] matrices) || matrices.Length != count)
            {
                matrices = new Matrix4x4[count];
                for (int j = 0; j < count; j++)
                {
                    int vertexIndex = indices[start + j];
                    if (vertexIndex >= 0 && vertexIndex < cachedVertices.Length)
                    {
                        Vector3 worldPos = meshFilter.transform.TransformPoint(cachedVertices[vertexIndex]);
                        matrices[j] = Matrix4x4.TRS(worldPos, Quaternion.identity, Vector3.one * size);
                    }
                }
                cachedBatches[i] = matrices;
            }

            Graphics.RenderMeshInstanced(rp, quadMesh, 0, matrices);
        }
    }

    private static Mesh CreateQuad()
    {
        Mesh quad = new Mesh();

        quad.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0)
        };

        quad.triangles = new int[] { 0, 1, 2, 2, 3, 0 };

        quad.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        return quad;
    }
}
