using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ZombieAI — The real SK (Zombie1 type: idle -> chase -> attack -> dead)
// From tutorial videos 19 (state machine), 20 (attack delay),
// 21 (damage to player), 22 (blood screen effect),
// 23 (TakeDamage / Die), 25 (animations, capital-I params).
// Adaptations vs tutorial:
//  - attackDelay default 3.9f (video 25 tuning: attack clip is 4 s long;
//    the original 1.5 s default is kept in comments for reference).
//  - player auto-find via tag "Player" if not assigned (video 31 fix:
//    spawned zombies otherwise have a missing player reference).
//  - Score: Die() adds +1 via GameManager when present (video 34).
//  - Blood screen effect is started once, in the Attack-case gate
//    (frame-verified in video 23), not also inside AttackWithDelay.
public class ZombieAI : MonoBehaviour
{
    public enum ZombieState { Idle, Chase, Attack, Dead }

    public ZombieState currentState = ZombieState.Idle;
    public Transform player;
    public float chaseDistance = 10f;
    public float attackDistance = 2f;
    public float attackCooldown = 2f;
    public float attackDelay = 3.9f;
    public int damage = 10;
    public int health = 100;
    public NavMeshAgent navAgent;
    public GameObject bloodScreenEffect;

    private CapsuleCollider capsuleCollider;
    private Animator animator;
    private GameObject instantiatedObject;
    private bool isAttacking;
    private float lastAttackTime;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        animator = GetComponent<Animator>();
        lastAttackTime = -attackCooldown;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case ZombieState.Idle:
                //animations
                if (animator != null)
                {
                    animator.SetBool("IsWalking", false);
                    animator.SetBool("IsAttacking", false);
                }
                if (player != null && Vector3.Distance(transform.position, player.position) <= chaseDistance)
                {
                    currentState = ZombieState.Chase;
                }
                break;

            case ZombieState.Chase:
                //animations
                if (animator != null)
                {
                    animator.SetBool("IsWalking", true);
                    animator.SetBool("IsAttacking", false);
                }
                navAgent.SetDestination(player.position);
                if (Vector3.Distance(transform.position, player.position) <= attackDistance)
                {
                    currentState = ZombieState.Attack;
                }
                break;

            case ZombieState.Attack:
                //animations
                if (animator != null)
                {
                    animator.SetBool("IsAttacking", true);
                }
                navAgent.SetDestination(transform.position);
                if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
                {
                    StartCoroutine(AttackWithDelay());
                    Debug.Log("Attack Player");
                    StartCoroutine(ActivateBloodScreenEffect());
                }
                if (Vector3.Distance(transform.position, player.position) > attackDistance)
                {
                    currentState = ZombieState.Chase;
                }
                break;

            case ZombieState.Dead:
                //animations
                if (animator != null)
                {
                    animator.SetBool("IsWalking", false);
                    animator.SetBool("IsAttacking", false);
                    animator.SetBool("IsDead", true);
                }
                navAgent.enabled = false;
                capsuleCollider.enabled = false;
                enabled = false;
                //increase the score
                Debug.Log("Dead");
                break;
        }
    }

    private IEnumerator AttackWithDelay()
    {
        isAttacking = true;

        //damage the player
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.TakeDamage(damage);
        }

        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;
        lastAttackTime = Time.time;
    }

    private void InstantiateObject()
    {
        instantiatedObject = Instantiate(bloodScreenEffect);
    }

    private void DeleteObject()
    {
        if (instantiatedObject != null)
        {
            Destroy(instantiatedObject);
            instantiatedObject = null;
        }
    }

    private IEnumerator ActivateBloodScreenEffect()
    {
        InstantiateObject();
        yield return new WaitForSeconds(attackDelay);
        DeleteObject();
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentState == ZombieState.Dead)
        {
            return;
        }

        health -= damageAmount;

        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }

    private void Die()
    {
        currentState = ZombieState.Dead;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1);
        }
    }
}
