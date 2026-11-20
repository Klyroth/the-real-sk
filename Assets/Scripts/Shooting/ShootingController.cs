using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ShootingController — The real SK
// From tutorial videos 12 (raycast shooting), 13 (rifle modes),
// 14 (ammo/reload), 15 (animator), 16 (muzzle flash), 17 (sounds),
// 23 + 31 (damage to ZombieAI and WaypointZombieAI), 33 (weapon stats),
// 38 (ammo UI text).
// Adaptations vs tutorial:
//  - Only 2 rifles in our build (Scar-H ~ M4 stats, Sniper ~ M82 stats);
//    per-weapon values are set in the Inspector/setup, not 4 rifles.
//  - Animator trigger is "reload" (lowercase) everywhere — the tutorial
//    itself is inconsistent ("reload" in video 15, "Reload" in video 17),
//    so our controllers define the parameter as "reload".
//  - Muzzle flash: the tutorial's Legacy Particle Pack is deprecated, so
//    our flash is a built-in ParticleSystem made in the setup script.
//  - Reload sound: no reload clip in our approved audio; the field is
//    null-guarded and may stay empty.
//  - Touch fire (Phase 11): TouchControls sets touchFireHeld /
//    touchFirePressed; zero effect on PC.
//  - Blood particle: spawned at the hit point on a zombie hit (tutorial
//    video 24); the prefab is made in the setup script.
public class ShootingController : MonoBehaviour
{
    [Header("Shooting")]
    public Transform firePoint;
    public float fireRate = 10f;
    public float fireRange = 25f;
    public bool isAuto = false;
    public int damagePerShot = 10;
    private float nextFireTime = 0f;

    [Header("Ammo & Reload")]
    public int maxAmmo = 30;
    public int currentAmmo;
    public float reloadTime = 3f;
    private bool isReloading = false;

    [Header("Animation & Effects")]
    public Animator animator;
    public ParticleSystem muzzleFlash;
    public GameObject bloodParticlePrefab;

    [Header("Sound Effects")]
    public AudioSource soundAudioSource;
    public AudioClip shootingSoundClip;
    public AudioClip reloadSoundClip;

    [Header("UI")]
    public Text currentAmmoText;

    // Android touch fire (written by TouchControls; unused on PC)
    public static bool touchFireHeld;
    public static bool touchFirePressed;

    void Start()
    {
        currentAmmo = maxAmmo;
        UpdateAmmoText();
    }

    void Update()
    {
        if (isReloading)
        {
            return;
        }

        // Manual reload
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
        {
            Reload();
        }

        bool fireHeld = Input.GetButton("Fire1") || touchFireHeld;
        bool firePressed = Input.GetButtonDown("Fire1") || touchFirePressed;
        touchFirePressed = false;

        if (isAuto)
        {
            if (fireHeld && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }
            else if (animator != null)
            {
                animator.SetBool("shoot", false);
            }
        }
        else
        {
            if (firePressed && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }
            else if (animator != null)
            {
                animator.SetBool("shoot", false);
            }
        }
    }

    private void Shoot()
    {
        if (currentAmmo > 0)
        {
            if (animator != null)
            {
                animator.SetBool("shoot", true);
            }

            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            if (soundAudioSource != null && shootingSoundClip != null)
            {
                soundAudioSource.PlayOneShot(shootingSoundClip);
            }

            RaycastHit hit;
            if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, fireRange))
            {
                // Damage the zombie (both zombie AI types — video 31 fix)
                ZombieAI zombieAI = hit.collider.GetComponent<ZombieAI>();
                if (zombieAI != null)
                {
                    zombieAI.TakeDamage(damagePerShot);
                    SpawnBlood(hit);
                }
                else
                {
                    WaypointZombieAI waypointZombie = hit.collider.GetComponent<WaypointZombieAI>();
                    if (waypointZombie != null)
                    {
                        waypointZombie.TakeDamage(damagePerShot);
                        SpawnBlood(hit);
                    }
                }
            }

            currentAmmo--;
            UpdateAmmoText();
        }
        else
        {
            //Reload
            Reload();
        }
    }

    private void SpawnBlood(RaycastHit hit)
    {
        if (bloodParticlePrefab != null)
        {
            GameObject blood = Instantiate(bloodParticlePrefab, hit.point, Quaternion.LookRotation(hit.normal));
            ParticleSystem ps = blood.GetComponent<ParticleSystem>();
            Destroy(blood, ps != null ? ps.main.duration : 2f);
        }
    }

    private void Reload()
    {
        if (!isReloading && currentAmmo < maxAmmo)
        {
            if (animator != null)
            {
                animator.SetTrigger("reload");
            }
            isReloading = true;

            if (soundAudioSource != null && reloadSoundClip != null)
            {
                soundAudioSource.PlayOneShot(reloadSoundClip);
            }

            Invoke("FinishReloading", reloadTime);
        }
    }

    private void FinishReloading()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
        UpdateAmmoText();

        if (animator != null)
        {
            animator.ResetTrigger("reload");
        }
    }

    private void UpdateAmmoText()
    {
        if (currentAmmoText != null)
        {
            currentAmmoText.text = currentAmmo + " / " + maxAmmo;
        }
    }
}
