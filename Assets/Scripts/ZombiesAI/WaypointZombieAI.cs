using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// WaypointZombieAI — The real SK (Zombie2 type: patrol -> chase -> attack)
// From tutorial videos 26 (waypoint structure), 27 (Walk/patrol),
// 28 (ChasePlayer/AttackPlayer), 29 (animations, lowercase-i params),
// 22-23 (blood screen, TakeDamage/Die — copied from ZombieAI).
// Differences vs ZombieAI (tutorial's own): Idle state is named Walk;
// patrol picks random NavMesh points; ChasePlayer speed 3f, Walk speed
// 0.3f; damage to the player is applied in AttackPlayer() itself
// (not inside AttackWithDelay); animator params are lowercase-i
// (isChasing/isAttacking/isDead) — case must match the controller.
public class WaypointZombieAI : MonoBehaviour
{
    public enum ZombieState { Walk, Chase, Attack, Dead }

    public ZombieState currentState = ZombieState.Walk;
    public Transform player;
    public float chaseDistance = 10f;
    public float attackDistance = 2f;
    public float attackCooldown = 2f;
    public float attackDelay = 1.5f;
    public int damage = 10;
    public int health = 100;
    public NavMeshAgent navAgent;
    public GameObject bloodScreenEffect;

    private CapsuleCollider capsuleCollider;
    private Animator animator;
    private GameObject instantiatedObject;
    private bool isAttacking;
    private bool isMoving = false;
    private float lastAttackTime;

    void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        navAgent = GetComponent<NavMeshAgent>();
        lastAttackTime = -attackCooldown;
        animator = GetComponent<Animator>();

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
            case ZombieState.Walk:
                if (!isMoving || navAgent.remainingDistance < 0.1f)
                {
                    Walk();
                }
                if (IsPlayerInRange(chaseDistance))
                {
                    currentState = ZombieState.Chase;
                }
                break;

            case ZombieState.Chase:
                ChasePlayer();
                if (IsPlayerInRange(attackDistance))
                {
                    currentState = ZombieState.Attack;
                }
                break;

            case ZombieState.Attack:
                AttackPlayer();
                if (!IsPlayerInRange(attackDistance))
                {
                    currentState = ZombieState.Chase;
                }
                break;

            case ZombieState.Dead:
                //animations
                if (animator != null)
                {
                    animator.SetBool("isChasing", false);
                    animator.SetBool("isAttacking", false);
                    animator.SetBool("isDead", true);
                }
                navAgent.enabled = false;
                capsuleCollider.enabled = false;
                enabled = false;
                //increase the score
                Debug.Log("Dead");
                break;
        }
    }

    private bool IsPlayerInRange(float range)
    {
        return player != null && Vector3.Distance(transform.position, player.position) <= range;
    }

    private Vector3 RandomNavMeshPosition()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 10f;
        randomDirection += transform.position;

        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, 10f, NavMesh.AllAreas);

        return hit.position;
    }

    private void Walk()
    {
        navAgent.speed = 0.3f;
        Vector3 randomPosition = RandomNavMeshPosition();
        navAgent.SetDestination(randomPosition);
        isMoving = true;
    }

    private void ChasePlayer()
    {
        navAgent.speed = 3f;
        //animations
        if (animator != null)
        {
            animator.SetBool("isChasing", true);
            animator.SetBool("isAttacking", false);
        }
        navAgent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        //animations
        if (animator != null)
        {
            animator.SetBool("isChasing", false);
            animator.SetBool("isAttacking", true);
        }
        navAgent.SetDestination(transform.position);
        if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
        {
            StartCoroutine(AttackWithDelay());
            StartCoroutine(ActivateBloodScreenEffect());
            lastAttackTime = Time.time;

            //damage the player
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.TakeDamage(damage);
            }
        }
    }

    private IEnumerator AttackWithDelay()
    {
        isAttacking = true;
        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;
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
