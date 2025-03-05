using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

// AI 상태를 구별하기 위한 enum 선언
public enum AIState
{
    Idle,
    Wandering,
    Attacking
}

public class NPC : MonoBehaviour, IDamagable
{
    [Header("Stats")] public int health;
    public float walkSpeed;
    public float runSpeed;
    public ItemData[] dropOnDeath;

    [Header("AI")] private NavMeshAgent agent;
    public float detectDistance;
    private AIState aiState;

    // Wandering 상태에 필요한 정보
    // min-max 사이의 대기 시간마다 min-max 사이의 거리에 있는 
    // 랜덤한 곳까지 돌아다니는 기능을 만들 때 필요한 정보들
    [Header("Wandering")] public float minWanderDistance;
    public float maxWanderDistance;
    public float minWanderWaitTime;
    public float maxWanderWaitTime;

    // Attacking 상태에 필요한 정보들
    [Header("Combat")] public int damage;
    public float attackRate;
    private float lastAttackTime;
    public float attackDistance;

    private float playerDistance;   // player와의 거리를 담아 둘 변수

    public float fieldOfView = 120f;

    private Animator animator;
    // NPC모델의 meshRenderer를 담아둘 배열 → 공격 받을 때 색 변경 예정
    private SkinnedMeshRenderer[] meshRenderers;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    private void Start()
    {
        // 최초 상태는 Wandering으로 설정
        SetState(AIState.Wandering);
    }

    private void Update()
    {
        // player와의 거리를 매 프레임마다 계산
        playerDistance = Vector3.Distance(transform.position, CharacterManager.Instance.Player.transform.position);

        animator.SetBool("Moving", aiState != AIState.Idle);

        switch (aiState)
        {
            case AIState.Idle:
            case AIState.Wandering:
                PassiveUpdate();
                break;
            case AIState.Attacking:
                AttackingUpdate();
                break;
        }
    }

    // 상태에 따른 agent의 이동속도, 정지여부를 설정
    public void SetState(AIState state)
    {
        aiState = state;

        switch (aiState)
        {
            case AIState.Idle:
                agent.speed = walkSpeed;
                agent.isStopped = true;
                break;
            case AIState.Wandering:
                agent.speed = runSpeed;
                agent.isStopped = false;
                break;
            case AIState.Attacking:
                agent.speed = runSpeed;
                agent.isStopped = false;
                break;
        }

        // 기본 값(walkSpeed)에 대한 비율로 재설정
        animator.speed = agent.speed / walkSpeed;
    }

    private void PassiveUpdate()
    {
        // Wandering 상태이고, 목표한 지점에 거의 다 왔을 때
        if (aiState == AIState.Wandering && agent.remainingDistance < 0.1f)
        {
            SetState(AIState.Idle);
            Invoke("WanderToNewLocation", Random.Range(minWanderWaitTime, maxWanderWaitTime));
        }

        // 플레이어와의 거리가 감지 범위 안에 있을 때
        if (playerDistance < detectDistance)
        {
            SetState(AIState.Attacking);
        }
    }

    // 새로운 Wander 목표지점 찾기
    private void WanderToNewLocation()
    {
        if (aiState != AIState.Idle)
        {
            return;
        }

        SetState(AIState.Wandering);
        agent.SetDestination(GetWanderLocation());
    }

    private Vector3 GetWanderLocation()
    {
        NavMeshHit hit;

        // Unity 공식문서 확인해서 각 매개변수의 뜻과 반환 데이터 찾아보기
        NavMesh.SamplePosition(
            transform.position + (Random.onUnitSphere * Random.Range(minWanderDistance, maxWanderDistance)), out hit,
            maxWanderDistance, NavMesh.AllAreas);

        // 원하는 값이 안나왔을 때
        // 해당 로직을 C# 문법을 활용해 개선해보세요. (do-while)
        int i = 0;
        while (Vector3.Distance(transform.position, hit.position) < detectDistance)
        {
            NavMesh.SamplePosition(
                transform.position + (Random.onUnitSphere * Random.Range(minWanderDistance, maxWanderDistance)),
                out hit,
                maxWanderDistance, NavMesh.AllAreas);
            i++;
            if (i == 30)
            {
                break;
            }
        }

        return hit.position;
    }

    // 플레이어와의 거리가 공격범위 안에 있고 시야각 안에 있을 때
    private void AttackingUpdate()
    {
        if (playerDistance < attackDistance && IsPlayerInFieldOfView())
        {
            agent.isStopped = true;
            if (Time.time - lastAttackTime > attackRate)
            {
                lastAttackTime = Time.time;
                CharacterManager.Instance.Player.controller.GetComponent<IDamagable>().TakePhysicalDamage(damage);
                animator.speed = 1;
                animator.SetTrigger("Attack");
            }
        }
        else
        {
            // 공격범위 안에는 없지만 감지범위 안에는 있을 때
            if (playerDistance < detectDistance)
            {
                agent.isStopped = false;
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(CharacterManager.Instance.Player.transform.position, path))
                {
                    agent.SetDestination(CharacterManager.Instance.Player.transform.position);
                }
                else
                {
                    agent.SetDestination(transform.position);
                    agent.isStopped = true;
                    SetState(AIState.Wandering);
                }
            }
            // 감지범위 밖으로 나갔을 때
            else
            {
                agent.SetDestination(transform.position);
                agent.isStopped = true;
                SetState(AIState.Wandering);
            }
        }
    }

    private bool IsPlayerInFieldOfView()
    {
        // 뱡향 구하기 (타겟 - 내 위치) -- ⓐ
        Vector3 directionToPlayer = CharacterManager.Instance.Player.transform.position - transform.position;
        // 내 정면 방향과 ⓐ 사이의 각도 구하기
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        // 설정한 시야각의 1/2 보다 작다면 시야각 안에 있는 것.
        // 시야각(ex.120도) = 내 정면 방향으로 좌우로 60도씩
        return angle < fieldOfView * 0.5f;
    }

    public void TakePhysicalDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }

        StartCoroutine(DamageFlash());
    }

    private void Die()
    {
        for (int i = 0; i < dropOnDeath.Length; i++)
        {
            Instantiate(dropOnDeath[i].dropPrefab, transform.position + Vector3.up, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }

    private IEnumerator DamageFlash()
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].material.color = new Color(1.0f, 0.6f, 0.6f);
        }

        yield return new WaitForSeconds(0.1f);
        
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].material.color = Color.white;
        }
    }
}