using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// PlayerMovement — The real SK
// From tutorial videos 05 (movement), 06 (gravity/ground), 07 (jump),
// 08 (footsteps), 21 (health/damage), 35 (health bar slider).
// Adaptations vs tutorial:
//  - touchMoveInput: Android touch joystick writes here (zero on PC, so PC
//    behaviour is unchanged). Phase 11.
//  - null-guards on AudioSources/Slider (tutorial assigns them in Inspector).
//  - Die() is a placeholder log in the tutorial; the DeathScreen wiring is
//    added in Phase 8 (DeathScreen.cs).
public class PlayerMovement : MonoBehaviour
{
    [Header("Player Movement & Gravity")]
    public float movementSpeed = 5f;
    public float gravity = -9.81f;
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance = 0.4f;
    public float jumpForce = 1.5f;

    [Header("Foot Steps")]
    public AudioSource leftFootAudioSource;
    public AudioSource rightFootAudioSource;
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.5f;

    [Header("Player Health & Damage")]
    private int maxHealth = 100;
    public int currentHealth;
    public Slider healthSlider;
    public DeathScreen deathScreen;

    private CharacterController controller;
    private bool isGrounded;
    private Vector3 velocity;
    private float nextFootstepTime;
    private bool isLeftFootstep = true;

    // Android touch input (written by TouchControls; zero on PC)
    public static Vector2 touchMoveInput;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = currentHealth;
            healthSlider.value = currentHealth;
        }
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        HandleMovement();
        HandleGravity();

        //Handle Footsteps
        if (isGrounded && controller.velocity.magnitude > 0.1f && Time.time >= nextFootstepTime)
        {
            PlayerFootstepSound();
            nextFootstepTime = Time.time + footstepInterval;
        }

        //Handle Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2 * gravity);
        }

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        if (touchMoveInput.sqrMagnitude > 0.001f)
        {
            horizontalInput = touchMoveInput.x;
            verticalInput = touchMoveInput.y;
        }

        Vector3 movement = transform.right * horizontalInput + transform.forward * verticalInput;
        movement.y = 0;

        controller.Move(movement * movementSpeed * Time.deltaTime);
    }

    void HandleGravity()
    {
        velocity.y += gravity * Time.deltaTime;
    }

    void PlayerFootstepSound()
    {
        if (footstepSounds == null || footstepSounds.Length == 0)
        {
            return;
        }

        AudioClip footstepClip = footstepSounds[Random.Range(0, footstepSounds.Length)];

        if (isLeftFootstep)
        {
            if (leftFootAudioSource != null) leftFootAudioSource.PlayOneShot(footstepClip);
        }
        else
        {
            if (rightFootAudioSource != null) rightFootAudioSource.PlayOneShot(footstepClip);
        }

        isLeftFootstep = !isLeftFootstep;
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (healthSlider != null)
        {
            healthSlider.value -= damageAmount;
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player has died");
        if (deathScreen != null)
        {
            deathScreen.showDeadScreen = true;
        }
    }
}
