using UnityEngine;

/// <summary>
/// Emits small, allocation-free air-bubble bursts while Player1 is swimming.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public sealed class AirBubbleEmitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] ParticleSystem bubbleParticles;
    [SerializeField] GameManager gameManager;

    [Header("Bubble Bursts")]
    [SerializeField] Vector2 emissionInterval = new Vector2(2.5f, 4f);
    [SerializeField] Vector2Int bubblesPerBurst = new Vector2Int(3, 6);
    [SerializeField] bool emitOnlyDuringGameplay = true;

    float timeUntilNextBurst;

    void Awake()
    {
        if (bubbleParticles == null)
            bubbleParticles = GetComponent<ParticleSystem>();
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }

    void OnEnable()
    {
        ScheduleNextBurst();
    }

    void Update()
    {
        if (emitOnlyDuringGameplay
            && gameManager != null
            && !gameManager.IsGameplayEnabled)
        {
            return;
        }

        timeUntilNextBurst -= Time.deltaTime;
        if (timeUntilNextBurst > 0f)
            return;

        int count = Random.Range(
            bubblesPerBurst.x,
            bubblesPerBurst.y + 1);
        bubbleParticles.Emit(count);
        ScheduleNextBurst();
    }

    void ScheduleNextBurst()
    {
        timeUntilNextBurst = Random.Range(
            emissionInterval.x,
            emissionInterval.y);
    }

    void OnValidate()
    {
        emissionInterval.x = Mathf.Max(0.1f, emissionInterval.x);
        emissionInterval.y = Mathf.Max(emissionInterval.x, emissionInterval.y);
        bubblesPerBurst.x = Mathf.Max(1, bubblesPerBurst.x);
        bubblesPerBurst.y = Mathf.Max(bubblesPerBurst.x, bubblesPerBurst.y);
    }
}
