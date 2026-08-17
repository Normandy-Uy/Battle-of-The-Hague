using UnityEngine;

/// <summary>
/// Keeps the player inside configurable X/Y bounds and locks Z.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class BoundaryLimiter : MonoBehaviour
{
    [Header("Bounds")]
    [SerializeField] float minX = 0f;
    [SerializeField] float maxX = 300f;
    [SerializeField] float minY = -3f;
    [SerializeField] float maxY = 18f;
    [SerializeField] float lockZPosition = 0f;

    Rigidbody body;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void ExtendMaxX(float requiredMaxX)
    {
        maxX = Mathf.Max(maxX, requiredMaxX);
    }

    public void SetMaxX(float value)
    {
        maxX = value;
        if (maxX < minX)
            maxX = minX;
    }

    public void ExtendMaxY(float requiredMaxY)
    {
        maxY = Mathf.Max(maxY, requiredMaxY);
    }

    public Vector3 ClampPosition(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        position.z = lockZPosition;
        return position;
    }

    void FixedUpdate()
    {
        if (body == null)
            return;

        Vector3 position = body.position;
        Vector3 velocity = body.velocity;
        bool changed = false;

        if (position.x < minX)
        {
            position.x = minX;
            if (velocity.x < 0f)
                velocity.x = 0f;
            changed = true;
        }
        else if (position.x > maxX)
        {
            position.x = maxX;
            if (velocity.x > 0f)
                velocity.x = 0f;
            changed = true;
        }

        if (position.y < minY)
        {
            position.y = minY;
            if (velocity.y < 0f)
                velocity.y = 0f;
            changed = true;
        }
        else if (position.y > maxY)
        {
            position.y = maxY;
            if (velocity.y > 0f)
                velocity.y = 0f;
            changed = true;
        }

        if (!Mathf.Approximately(position.z, lockZPosition))
        {
            position.z = lockZPosition;
            velocity.z = 0f;
            changed = true;
        }

        if (!changed)
            return;

        body.position = position;
        body.velocity = velocity;
    }

    void OnValidate()
    {
        if (maxX < minX)
            maxX = minX;
        if (maxY < minY)
            maxY = minY;
    }
}
