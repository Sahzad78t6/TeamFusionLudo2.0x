using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.AI;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;


public enum AIType
{
    Passive,
    Scared,
    Aggresive
}

public enum AIState
{
    Idle,
    Wandering,
    Attacking,
    Fleeing
}
public class NPC : MonoBehaviour, IDamagable
{
    [Header("Stats")]
    public int health;
    public float walkSpeed;
    public float runSpeed;
    public ItemDatabase[] dropOnDeath;

    [Header("AI")] 
    public AIType aiType;
    private AIState aiState;
    public float detectDistance = 15f;
    public float safeDistance = 12f;
    public float chaseDistance = 28f;
    public float reacquireCooldown = 3.5f;
    private float lastLostTargetTime = -10f;
    private Vector3 spawnPosition;

    [Header("Wandering")] 
    public float minWanderDistance;
    public float maxWanderDistance;
    public float minWanderWaitTime;
    public float maxWanderWaitTime;

    [Header("Combat")] 
    public int damage;
    public float attackRate;
    private float lastAttackTime;
    public float attackDistance;
    private float playerDistance;

    [Header("Sound")] 
    public AudioSource audioSource;
    
    //get components

    private NavMeshAgent agent;
    private Animator anim;
    private SkinnedMeshRenderer[] meshRenderers;

    private bool IsAgentValid()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        spawnPosition = transform.position;

        if (agent != null)
        {
            agent.enabled = false;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                spawnPosition = hit.position;
            }
            agent.enabled = true;
        }
    }

    private void Start()
    {
        if (agent != null && !agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
        SetState(AIState.Wandering);
    }

    private void Update()
    {
        if (PlayerController.instance == null) return;

        //get Player distance
        playerDistance = Vector3.Distance(transform.position, PlayerController.instance.transform.position);
        anim.SetBool("Moving", aiState != AIState.Idle);

        switch (aiState)
        {
            case AIState.Idle: PassiveUpdate();
                break;
            case AIState.Wandering: PassiveUpdate();
                break;
            case AIState.Attacking: AttackingUpdate();
                break;
            case AIState.Fleeing: FleeingUpdate();
                break;
        }
    }

    void PassiveUpdate()
    {
        if (aiState == AIState.Wandering && IsAgentValid() && !agent.pathPending && agent.remainingDistance < 0.1f)
        {
            SetState(AIState.Idle);
            Invoke("WanderToNewLocation", Random.Range(minWanderWaitTime, maxWanderWaitTime));
        }
        
        //begin the attack to player if npc detect him and reacquire cooldown has passed
        if (aiType == AIType.Aggresive && playerDistance < detectDistance && Time.time - lastLostTargetTime > reacquireCooldown)
        {
            SetState(AIState.Attacking);
        }
        //run away from the player if we detect him
        else if (aiType == AIType.Scared && playerDistance < detectDistance)
        {
            SetState(AIState.Fleeing);
            if (IsAgentValid())
                agent.SetDestination(GetFleeLocation());
        }
    }

    void AttackingUpdate()
    {
        if (!IsAgentValid()) return;

        // AI Leash: Player moved beyond chase distance, stop following and return to wandering/home
        if (playerDistance > chaseDistance)
        {
            lastLostTargetTime = Time.time;
            SetState(AIState.Wandering);
            if (IsAgentValid())
            {
                agent.SetDestination(spawnPosition);
            }
            return;
        }

        if (playerDistance > attackDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(PlayerController.instance.transform.position);
        }
        else
        {
            agent.isStopped = true;
          
            if (Time.time - lastAttackTime > attackRate)
            {
                lastAttackTime = Time.time;
                if (PlayerController.instance != null)
                {
                    PlayerController.instance.GetComponent<IDamagable>().TakePhysicDamage(damage);
                }
                anim.SetTrigger("Attack");
            }
        }
    }

    void FleeingUpdate()
    {
        if (playerDistance < safeDistance && IsAgentValid() && !agent.pathPending && agent.remainingDistance < 0.1f)
        {
            if (IsAgentValid())
                agent.SetDestination(GetFleeLocation());
        }
        else if (playerDistance > safeDistance)
        {
            SetState(AIState.Wandering);
        }
    }

    void SetState(AIState newState)
    {
        aiState = newState;
        if (!IsAgentValid()) return;

        switch (aiState)
        {
            case AIState.Idle:
            {
                agent.speed = walkSpeed;
                agent.isStopped = true;
                break;
            }
            case AIState.Wandering:
            {
                agent.speed = walkSpeed;
                agent.isStopped = false;
                break;
            }
            case AIState.Attacking:
            {
                agent.speed = runSpeed;
                agent.isStopped = false;
                break;
            }
            case AIState.Fleeing:
            {
                agent.speed = runSpeed;
                agent.isStopped = false;
                break;
            }
        }
    }

    void WanderToNewLocation()
    {
        // if npc is not in idle state dont call for new destination
        if (aiState != AIState.Idle)
            return;
        
        SetState(AIState.Wandering);
        if (IsAgentValid())
            agent.SetDestination(GetWanderLocation());
    }

    Vector3 GetWanderLocation()
    {
        NavMeshHit hit;
        bool found = NavMesh.SamplePosition(transform.position +
                               (Random.onUnitSphere * Random.Range(minWanderDistance, maxWanderDistance)), out hit, maxWanderDistance, NavMesh.AllAreas);

        int i = 0;

        while (found && Vector3.Distance(transform.position, hit.position) < detectDistance)
        {
            found = NavMesh.SamplePosition(transform.position +
                                   (Random.onUnitSphere * Random.Range(minWanderDistance, maxWanderDistance)), out hit, maxWanderDistance, NavMesh.AllAreas);
            i++;
            if (i == 30)
                break;
        }

        return found ? hit.position : transform.position;
    }

    Vector3 GetFleeLocation()
    {
        NavMeshHit hit;
        bool found = NavMesh.SamplePosition(transform.position + (Random.onUnitSphere * safeDistance), out hit, safeDistance, NavMesh.AllAreas);
        int i = 0;
        while (found && (GetDestinationAngel(hit.position) > 90 || playerDistance < safeDistance))
        {
            found = NavMesh.SamplePosition(transform.position + (Random.onUnitSphere * safeDistance), out hit, safeDistance, NavMesh.AllAreas);
            i++;
            if (i == 30)
                break;
        }

        return found ? hit.position : transform.position;
    }

    float GetDestinationAngel(Vector3 targetPos)
    {
        return Vector3.Angle(transform.position - PlayerController.instance.transform.position,
            transform.position + targetPos);
    }

    public void TakePhysicDamage(int damageAmount)
    {
        health -= damageAmount;

        if (health <= 0)
            Die();

        StartCoroutine(DamageFlash());
        if (aiType == AIType.Passive)
            SetState(AIState.Fleeing);
    }

    private float delay = 0.0f;

    void Die()
    {
        if (agent != null)
            agent.enabled = false;

        if (dropOnDeath != null)
        {
            for (int x = 0; x < dropOnDeath.Length; x++)
            {
                if (dropOnDeath[x] != null && dropOnDeath[x].dropPrefab != null)
                {
                    Instantiate(dropOnDeath[x].dropPrefab, transform.position, Quaternion.identity);
                }
            }
        }

        if (anim != null)
        {
            anim.SetTrigger("Die");
            float animLength = anim.GetCurrentAnimatorStateInfo(0).length;
            Destroy(gameObject, animLength + delay);
        }
        else
        {
            Destroy(gameObject, 1.0f + delay);
        }
    }

    IEnumerator DamageFlash()
    {
        if (audioSource != null) audioSource.Play();
        if (meshRenderers != null)
        {
            for (int x = 0; x < meshRenderers.Length; x++)
            {
                if (meshRenderers[x] != null && meshRenderers[x].material != null)
                    meshRenderers[x].material.color = new Color(1.0f, 0.5f, 0.5f);
            }
            yield return new WaitForSeconds(0.1f);
            for (int x = 0; x < meshRenderers.Length; x++)
            {
                if (meshRenderers[x] != null && meshRenderers[x].material != null)
                    meshRenderers[x].material.color = Color.white;
            }
        }
    }
}
