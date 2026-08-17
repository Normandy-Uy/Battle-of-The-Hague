using UnityEngine;

/// <summary>
/// Marks a saved scene position for a Highway Cross Road chaser duplicate (7×6 formation).
/// </summary>
[DisallowMultipleComponent]
public class DutzLevel00CrossroadSpawnSlot : MonoBehaviour
{
    [SerializeField] int row;
    [SerializeField] int column;

    public int Row => row;
    public int Column => column;

    public void SetGridIndex(int gridRow, int gridColumn)
    {
        row = gridRow;
        column = gridColumn;
    }
}
