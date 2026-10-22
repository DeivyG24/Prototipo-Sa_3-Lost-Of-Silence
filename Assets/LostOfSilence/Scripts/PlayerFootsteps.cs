
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Footstep Sounds")]
    public AudioClip[] footstepSounds;
    [Range(0f, 1f)] public float volume = 0.6f;
    public float movementThreshold = 0.15f;

    [Header("Step Intervals")]
    public float walkStepInterval = 0.45f;
    public float runStepInterval = 0.30f;

    private CharacterController controller;
    private AudioSource audioSource;
    private float stepTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    void Update()
    {
        if (controller == null || !controller.isGrounded)
        {
            stepTimer = 0f;
            return;
        }

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.magnitude < movementThreshold)
        {
            stepTimer = 0f;
            return;
        }

        bool isRunning = IsRunning();

        float currentInterval = isRunning
            ? runStepInterval
            : walkStepInterval;

        stepTimer += Time.deltaTime;

        if (stepTimer >= currentInterval)
        {
            PlayFootstep();
            stepTimer = 0f;
        }
    }

    bool IsRunning()
    {
        if (Keyboard.current == null)
            return false;

        bool holdingShift = Keyboard.current.leftShiftKey.isPressed;
        bool movingForward = Keyboard.current.wKey.isPressed;

        return holdingShift && movingForward;
    }

    void PlayFootstep()
    {
        if (footstepSounds == null || footstepSounds.Length == 0)
            return;

        AudioClip clip = footstepSounds[
            Random.Range(0, footstepSounds.Length)
        ];

        if (clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
}
