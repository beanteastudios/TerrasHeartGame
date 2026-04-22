using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class WaterMeshGenerator : MonoBehaviour
{
    [SerializeField] private float _width = 10f;
    [SerializeField] private float _height = 3f;
    [SerializeField] private int _subdivisionsX = 40;
    [SerializeField] private int _subdivisionsY = 8;

    [ContextMenu("Generate Water Mesh")]
    private void GenerateMesh()
    {
        var mesh = new Mesh { name = "WaterSurfaceMesh" };

        int vertsX = _subdivisionsX + 1;
        int vertsY = _subdivisionsY + 1;

        var vertices = new Vector3[vertsX * vertsY];
        var uvs = new Vector2[vertsX * vertsY];

        for (int y = 0; y < vertsY; y++)
        {
            for (int x = 0; x < vertsX; x++)
            {
                float u = (float)x / _subdivisionsX;
                float v = (float)y / _subdivisionsY;
                vertices[y * vertsX + x] = new Vector3(
                    (u - 0.5f) * _width,
                    (v - 0.5f) * _height,
                    0f);
                uvs[y * vertsX + x] = new Vector2(u, v);
            }
        }

        var triangles = new int[_subdivisionsX * _subdivisionsY * 6];
        int t = 0;
        for (int y = 0; y < _subdivisionsY; y++)
        {
            for (int x = 0; x < _subdivisionsX; x++)
            {
                int bl = y * vertsX + x;
                int br = bl + 1;
                int tl = bl + vertsX;
                int tr = tl + 1;
                triangles[t++] = bl; triangles[t++] = tl; triangles[t++] = tr;
                triangles[t++] = bl; triangles[t++] = tr; triangles[t++] = br;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
        Debug.Log($"[WaterMeshGenerator] Generated {vertsX * vertsY} vertices.");
    }
}