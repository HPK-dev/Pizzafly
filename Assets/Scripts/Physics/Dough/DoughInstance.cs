using System.Linq;
using Networking;
using UnityEngine;

namespace Physics.Dough;

[System.Serializable]
public class DoughInstance
{
    public DoughVertex[] vertices;
    public int[] triangles;
    public Bounds bounds;
    public LodLevel lodLevel;

    /// Average stretch progress [0, 100]
    public float averageProgress { get; private set; }

    /// Spatial Hash for collision detection
    public SpatialHash SpatialHash;

    // Grid dimensions for the mesh
    public int gridWidth;
    public int gridHeight;


    public DoughInstance(int width, int height, Vector3 center, float size)
    {
        gridWidth = width;
        gridHeight = height;
        lodLevel = 0;

        InitializeVertices(center, size);
        InitializeTriangles();
        UpdateBounds();

        SpatialHash = new SpatialHash(bounds, 1.0f);

    }

    private void InitializeVertices(Vector3 center, float size)
    {
        vertices = new DoughVertex[gridWidth * gridHeight];

        for (var y = 0; y < gridHeight; y++)
        {
            for (var x = 0; x < gridWidth; x++)
            {
                var index = y * gridWidth + x;

                // Calculate position in grid space
                var xPos = (x / (float)(gridWidth - 1) - 0.5f) * size;
                var zPos = (y / (float)(gridHeight - 1) - 0.5f) * size;

                var position = center + new Vector3(xPos, 0, zPos);
                var gridIndex = new Vector2Int(x, y);

                vertices[index] = new DoughVertex(position, gridIndex);
            }
        }
    }

    private void InitializeTriangles()
    {
        // Create triangle indices for a quad mesh
        var quadCount = (gridWidth - 1) * (gridHeight - 1);
        triangles = new int[quadCount * 6]; // 2 triangles per quad, 3 indices per triangle

        var triIndex = 0;
        for (var y = 0; y < gridHeight - 1; y++)
        {
            for (var x = 0; x < gridWidth - 1; x++)
            {
                var bottomLeft = y * gridWidth + x;
                var bottomRight = bottomLeft + 1;
                var topLeft = (y + 1) * gridWidth + x;
                var topRight = topLeft + 1;

                // First triangle (bottom-left, top-left, bottom-right)
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomRight;

                // Second triangle (bottom-right, top-left, top-right)
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
            }
        }
    }

    public void UpdateBounds()
    {
        if (vertices.Length == 0) return;

        var min = vertices[0].currentPosition;
        var max = vertices[0].currentPosition;

        foreach (var vertex in vertices)
        {
            min = Vector3.Min(min, vertex.initialPosition);
            max = Vector3.Max(max, vertex.initialPosition);
        }

        bounds = new Bounds((min + max) * 0.5f, max - min);
    }

    public void UpdateAverageProgress()
    {
        if (vertices.Length == 0) return;
        
        float sum = 0;
        for (var i = 0; i < vertices.Length; i++) {
            sum += vertices[i].stretchProgress;
        }
        averageProgress = sum / vertices.Length;
    }

    public int GetVertexIndex(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            return -1;

        return y * gridWidth + x;
    }

    public Vector2Int? GetGridPosition(int vertexIndex)
    {
        if (vertexIndex < 0 || vertexIndex >= vertices.Length)
            return null;

        return vertices[vertexIndex].gridIndex;
    }

    public void UpdateSpatialHash()
    {
        SpatialHash.Clear();
        for (var i = 0; i < vertices.Length; i++)
        {
            SpatialHash.Insert(vertices[i].initialPosition, i);
        }
    }
}