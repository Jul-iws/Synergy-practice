using UnityEngine;

public sealed class PlayerMovementParticles : MonoBehaviour
{
    [SerializeField] private ParticleSystem movementParticles;
    [SerializeField] private SimplePlayerController playerController;
    [SerializeField, Min(0f)] private float minimumSpeed = 0.2f;

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<SimplePlayerController>();
    }

    private void Update()
    {
        if (movementParticles == null || playerController == null) return;

        var shouldPlay = playerController.HorizontalVelocity.magnitude >= minimumSpeed;
        if (shouldPlay && !movementParticles.isPlaying) movementParticles.Play();
        if (!shouldPlay && movementParticles.isPlaying) movementParticles.Stop();
    }
}

