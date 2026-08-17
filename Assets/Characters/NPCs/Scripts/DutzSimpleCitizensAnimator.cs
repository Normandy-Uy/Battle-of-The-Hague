using UnityEngine;

/// <summary>
/// Drives SimpleCitizens animator parameters from DutzPlayerController movement.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(DutzPlayerController))]
[DefaultExecutionOrder(350)]
public class DutzSimpleCitizensAnimator : MonoBehaviour
{
    static readonly int SpeedId = Animator.StringToHash("Speed_f");
    static readonly int GroundedId = Animator.StringToHash("Grounded_b");
    static readonly int JumpId = Animator.StringToHash("Jump_b");
    static readonly int DeathId = Animator.StringToHash("Death_b");

    [SerializeField] float walkSpeedParam = 0.45f;
    [SerializeField] float runSpeedParam = 1f;

    Animator animator;
    DutzPlayerController player;
    CharacterController characterController;

    void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponent<DutzPlayerController>();
        characterController = GetComponent<CharacterController>();
        animator.applyRootMotion = false;
    }

    void OnEnable()
    {
        if (player != null)
            player.Jumped += OnJumped;
    }

    void OnDisable()
    {
        if (player != null)
            player.Jumped -= OnJumped;
    }

    void OnJumped()
    {
        if (animator != null)
            animator.SetBool(JumpId, true);
    }

    /// <summary>Forces upright idle — Death_01 has no exit transition in the animator controller.</summary>
    public void ResetToStanding()
    {
        if (animator == null)
            return;

        animator.SetBool(DeathId, false);
        animator.SetBool(JumpId, false);
        animator.SetFloat(SpeedId, 0f);
        animator.SetBool(GroundedId, true);
        animator.Play("Idle", 0, 0f);
        animator.Update(0f);
    }

    void Update()
    {
        if (animator == null || player == null)
            return;

        var grounded = characterController != null && characterController.isGrounded;
        animator.SetBool(GroundedId, grounded);

        if (grounded)
            animator.SetBool(JumpId, false);

        if (animator.GetBool(DeathId))
            return;

        if (IsPunching())
        {
            animator.SetFloat(SpeedId, 0f);
            return;
        }

        if (player.ControlsLocked)
        {
            animator.SetFloat(SpeedId, 0f);
            return;
        }

        if (!player.IsMoving)
        {
            animator.SetFloat(SpeedId, 0f);
            return;
        }

        animator.SetFloat(SpeedId, player.IsRunning ? runSpeedParam : walkSpeedParam);
    }

    bool IsPunching()
    {
        var punch = player != null ? player.GetComponent<DutzPlayerPunch>() : null;
        return punch != null && punch.IsPunchingVisual;
    }
}
