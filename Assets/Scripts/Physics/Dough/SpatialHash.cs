using System.Collections.Generic;
using UnityEngine;

namespace Physics.Dough;

/// <summary>
/// Spatial hash table for efficient collision detection and spatial queries
/// Used for fast neighbor finding in dough physics system
/// </summary>
public class SpatialHash
{
    private readonly Dictionary<int, List<int>> _hashTable;
    private readonly float _cellSize;
    private Bounds _bounds;
    private Vector3Int _gridSize;

    public SpatialHash(Bounds bounds, float cellSize)
    {
        this._bounds = bounds;
        this._cellSize = cellSize;
        this._hashTable = new Dictionary<int, List<int>>();

        // Calculate grid size based on bounds and cell size
        var size = bounds.size;
        _gridSize = new Vector3Int(
            Mathf.CeilToInt(size.x / cellSize),
            Mathf.CeilToInt(size.y / cellSize),
            Mathf.CeilToInt(size.z / cellSize)
        );
    }

    /// <summary>
    /// Clear all entries from the hash table
    /// </summary>
    public void Clear()
    {
        foreach (var bucket in _hashTable.Values)
        {
            bucket.Clear();
        }
    }

    /// <summary>
    /// Insert a point with associated data into the spatial hash
    /// </summary>
    /// <param name="position">World position to insert</param>
    /// <param name="data">Associated data (typically vertex index)</param>
    public void Insert(Vector3 position, int data)
    {
        var hash = GetHash(position);

        if (!_hashTable.ContainsKey(hash))
        {
            _hashTable[hash] = [];
        }

        _hashTable[hash].Add(data);
    }

    /// <summary>
    /// Query all points within a sphere around the given position
    /// </summary>
    /// <param name="position">Center of query sphere</param>
    /// <param name="radius">Radius of query sphere</param>
    /// <param name="results">List to store results in</param>
    public void QuerySphere(Vector3 position, float radius, List<int> results)
    {
        results.Clear();

        // Calculate the range of cells to check
        var minPos = position - Vector3.one * radius;
        var maxPos = position + Vector3.one * radius;

        var minCell = WorldToGrid(minPos);
        var maxCell = WorldToGrid(maxPos);

        var radiusSquared = radius * radius;

        // Check all cells in the range
        for (var x = minCell.x; x <= maxCell.x; x++)
        {
            for (var y = minCell.y; y <= maxCell.y; y++)
            {
                for (var z = minCell.z; z <= maxCell.z; z++)
                {
                    var cell = new Vector3Int(x, y, z);
                    var hash = CellToHash(cell);

                    if (_hashTable.TryGetValue(hash, out var value))
                        results.AddRange(value);
                }
            }
        }
    }

    /// <summary>
    /// Query points within a box region
    /// </summary>
    /// <param name="center">Center of the box</param>
    /// <param name="halfExtents">Half extents of the box</param>
    /// <param name="results">List to store results in</param>
    public void QueryBox(Vector3 center, Vector3 halfExtents, List<int> results)
    {
        results.Clear();

        var minPos = center - halfExtents;
        var maxPos = center + halfExtents;

        var minCell = WorldToGrid(minPos);
        var maxCell = WorldToGrid(maxPos);

        // Check all cells in the range
        for (var x = minCell.x; x <= maxCell.x; x++)
        {
            for (var y = minCell.y; y <= maxCell.y; y++)
            {
                for (var z = minCell.z; z <= maxCell.z; z++)
                {
                    var cell = new Vector3Int(x, y, z);
                    var hash = CellToHash(cell);

                    if (_hashTable.TryGetValue(hash, out var value))
                    {
                        results.AddRange(value);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get the hash value for a world position
    /// </summary>
    private int GetHash(Vector3 position)
    {
        var cell = WorldToGrid(position);
        return CellToHash(cell);
    }

    /// <summary>
    /// Convert world position to grid coordinates
    /// </summary>
    private Vector3Int WorldToGrid(Vector3 worldPos)
    {
        var localPos = worldPos - _bounds.min;

        return new Vector3Int(
            Mathf.FloorToInt(localPos.x / _cellSize),
            Mathf.FloorToInt(localPos.y / _cellSize),
            Mathf.FloorToInt(localPos.z / _cellSize)
        );
    }

    /// <summary>
    /// Convert grid cell coordinates to hash value
    /// Uses a simple hash function for 3D coordinates
    /// </summary>
    private int CellToHash(Vector3Int cell)
    {
        // Clamp to grid bounds
        cell.x = Mathf.Clamp(cell.x, 0, _gridSize.x - 1);
        cell.y = Mathf.Clamp(cell.y, 0, _gridSize.y - 1);
        cell.z = Mathf.Clamp(cell.z, 0, _gridSize.z - 1);

        // Simple 3D hash function
        return cell.x + cell.y * _gridSize.x + cell.z * _gridSize.x * _gridSize.y;
    }

    /// <summary>
    /// Get statistics about the spatial hash table
    /// Useful for debugging and optimization
    /// </summary>
    public SpatialHashStats GetStats()
    {
        var totalEntries = 0;
        var occupiedCells = _hashTable.Count;
        var maxEntriesPerCell = 0;

        foreach (var bucket in _hashTable.Values)
        {
            totalEntries += bucket.Count;
            maxEntriesPerCell = Mathf.Max(maxEntriesPerCell, bucket.Count);
        }

        var totalCells = _gridSize.x * _gridSize.y * _gridSize.z;
        var loadFactor = totalCells > 0 ? (float)occupiedCells / totalCells : 0f;

        return new SpatialHashStats
        {
            TotalEntries = totalEntries,
            OccupiedCells = occupiedCells,
            TotalCells = totalCells,
            MaxEntriesPerCell = maxEntriesPerCell,
            LoadFactor = loadFactor,
            CellSize = _cellSize
        };
    }
}

/// <summary>
/// Statistics for spatial hash table performance monitoring
/// </summary>
public struct SpatialHashStats
{
    public int TotalEntries;
    public int OccupiedCells;
    public int TotalCells;
    public int MaxEntriesPerCell;
    public float LoadFactor;
    public float CellSize;

    public override string ToString()
    {
        return $"SpatialHash Stats: {TotalEntries} entries in {OccupiedCells}/{TotalCells} cells " +
               $"(load: {LoadFactor:F2}, max/cell: {MaxEntriesPerCell}, cellSize: {CellSize})";
    }
}