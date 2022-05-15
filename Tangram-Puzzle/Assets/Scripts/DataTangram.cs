using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject for data about possibles positions/rotations/flip and solutions of a tangram shape 
/// </summary>
[CreateAssetMenu(fileName = "TangramData", menuName = "Tangram Data", order = 1)]
public class DataTangram : ScriptableObject
{
    // possibles positions/rotations/flip

    public List<InfoPosition> infoPositionsLT; // Large Triangles
    public List<InfoPosition> infoPositionsST; // Small Triangles
    public List<InfoPosition> infoPositionsMD; // Medium Triangle
    public List<InfoPosition> infoPositionsSq; // Square
    public List<InfoPosition> infoPositionsPl; // Parallelogram
    public List<Solution> solutions;
}

/// <summary>
/// Struct with a one solution information 
/// </summary>
[System.Serializable]
public struct Solution
{
    // List with Indexes of possibles positions/rotations/flip
    // Indices are relative to the indices of the InfoPosition List of each tangram piece

    public List<int> solutionIndexes;
}
