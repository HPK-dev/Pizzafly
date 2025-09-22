using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Physics.Dough;

[Serializable]
public struct DoughVertex(Vector3 initialPosition, Vector2Int gridIndex)
{
    /// Start position
    public Vector3 initialPosition = initialPosition; 
    /// Position offset when deforming
    public Vector3 deformOffset = Vector3.zero;
    /// The velocity of this vertex (for dynamic response)
    public Vector3 velocity = Vector3.zero;
    /// Progress of stretch [0, 100]
    public float stretchProgress = 0f;
    /// The thickness of this vertex (height)
    public float thickness = 1f;
    /// Index in the whole dough mesh
    public Vector2Int gridIndex = gridIndex;


    public bool isLocked => stretchProgress >= 98.7f; // Secret Magic Number
    public Vector3 currentPosition => initialPosition + deformOffset;
}