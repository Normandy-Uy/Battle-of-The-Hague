using UnityEngine;

/// <summary>
/// Randomises pipe-slot gap centres on scene load. Does not instantiate pipe prefabs.
/// </summary>
[DisallowMultipleComponent]
public class PipeGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameManager gameManager;
    [SerializeField] PipeSlot[] slots;

    [Header("Random Centre Height")]
    [SerializeField] float minCentreY = 4f;
    [SerializeField] float maxCentreY = 14f;
    [SerializeField] bool generateOnStart = false;

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (slots == null || slots.Length == 0)
            slots = GetComponentsInChildren<PipeSlot>(true);
    }

    void Start()
    {
        if (generateOnStart)
            GenerateLayout();
    }

    public void GenerateLayout()
    {
        if (slots == null || slots.Length == 0)
            return;

        float gap = gameManager != null
            ? gameManager.GetGapForCurrentLevel()
            : 6f;

        float halfGap = gap * 0.5f;
        float usableMin = minCentreY + halfGap;
        float usableMax = maxCentreY - halfGap;

        if (usableMax < usableMin)
        {
            float mid = (minCentreY + maxCentreY) * 0.5f;
            usableMin = mid;
            usableMax = mid;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            PipeSlot slot = slots[i];
            if (slot == null)
                continue;

            float centreY = Random.Range(usableMin, usableMax);
            slot.ApplyGap(centreY, gap);
        }
    }

    public void SetSlots(PipeSlot[] newSlots)
    {
        slots = newSlots;
    }

    void OnValidate()
    {
        if (maxCentreY < minCentreY)
            maxCentreY = minCentreY;
    }
}
