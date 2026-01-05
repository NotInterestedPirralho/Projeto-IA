using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D), typeof(PhotonView))]
public class EnemyAI_BFS : EnemyBase
{
    // --- 1. DEFINIÇÃO DE ESTADOS ---
    public enum AIState { Idle, Patrol, Chase, Attack, Stunned }

    [Header("Estado e Configuração")]
    public AIState currentState;
    public float chaseRange = 10f;
    public float attackRange = 1.5f;
    public float jumpForce = 8f;

    [Header("Velocidades")]
    public float patrolSpeed = 1.5f; // Velocidade lenta ao patrulhar
    public float chaseSpeed = 3.5f;  // Velocidade rápida ao perseguir o player
    private float currentMoveSpeed;

    [Header("Patrulha por Distância")]
    public float patrolDistance = 5f; 
    public float waitTimeAtPoints = 1.5f;
    private Vector2 spawnPosition;
    private int patrolDirection = 1; // 1 = Direita, -1 = Esquerda
    private float waitTimer;
    private bool isWaiting;

    [Header("Configuração BFS (Pathfinding)")]
    public float cellSize = 0.5f; 
    public int maxSearchSteps = 200;  
    public float pathUpdateRate = 0.25f; 
    public LayerMask obstacleLayer;   

    [Header("Combate")]
    public float knockbackForce = 10f;
    public float stunTime = 0.5f;
    public int attackDamage = 7;
    public float attackCooldown = 1.5f;
    public Transform attackPoint;
    public LayerMask playerLayer;

    // --- IMPLEMENTAÇÃO OBRIGATÓRIA ENEMYBASE ---
    public override float KnockbackForce => knockbackForce;
    public override float StunTime => stunTime;

    // --- VARIÁVEIS PRIVADAS ---
    private Transform playerTarget;
    private Rigidbody2D rb;
    private PhotonView photonView;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    private float nextAttackTime = 0f;
    
    private List<Vector2> currentPath = new List<Vector2>();
    private int currentPathIndex = 0;
    private float pathTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        photonView = GetComponent<PhotonView>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            spawnPosition = transform.position; 
            currentState = AIState.Patrol;
            currentMoveSpeed = patrolSpeed;
            FindTarget();
        }
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (playerTarget == null) FindTarget();

        isGrounded = CheckGrounded();

        // Gerir mudanças de velocidade e estado
        HandleStateTransitions();

        switch (currentState)
        {
            case AIState.Patrol:
                HandlePatrol();
                break;
            case AIState.Chase:
                HandleChaseBFS();
                break;
            case AIState.Attack:
                HandleAttack();
                break;
        }

        ApplyArenaLimits();
    }

    private void HandleStateTransitions()
    {
        if (playerTarget == null || currentState == AIState.Stunned) return;

        float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        if (distToPlayer <= attackRange)
        {
            currentState = AIState.Attack;
        }
        else if (distToPlayer < chaseRange)
        {
            if (currentState != AIState.Chase)
            {
                currentState = AIState.Chase;
                currentMoveSpeed = chaseSpeed; // Aumenta a velocidade
                isWaiting = false; 
            }
        }
        else if (distToPlayer > chaseRange * 1.5f && currentState != AIState.Patrol)
        {
            currentState = AIState.Patrol;
            currentMoveSpeed = patrolSpeed; // Volta à velocidade de patrulha
        }
    }

    // --- 2. LÓGICA DE PATRULHA ---
    void HandlePatrol()
    {
        if (isWaiting)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            waitTimer -= Time.deltaTime;
            
            if (waitTimer <= 0)
            {
                isWaiting = false;
                patrolDirection *= -1; 
                FlipSprite(patrolDirection);
            }
            return;
        }

        rb.linearVelocity = new Vector2(patrolDirection * currentMoveSpeed, rb.linearVelocity.y);
        FlipSprite(patrolDirection);

        float currentOffset = transform.position.x - spawnPosition.x;

        if ((patrolDirection > 0 && currentOffset >= patrolDistance) || 
            (patrolDirection < 0 && currentOffset <= -patrolDistance))
        {
            StartWaiting();
        }

        RaycastHit2D wallInfo = Physics2D.Raycast(transform.position, Vector2.right * patrolDirection, 0.8f, obstacleLayer);
        if (wallInfo.collider != null) StartWaiting();
    }

    void StartWaiting()
    {
        isWaiting = true;
        waitTimer = waitTimeAtPoints;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    // --- 3. LÓGICA BFS (PERSEGUIÇÃO) ---
    void HandleChaseBFS()
    {
        if (playerTarget == null) return;

        pathTimer += Time.deltaTime;
        if (pathTimer >= pathUpdateRate)
        {
            pathTimer = 0;
            RunBFS(transform.position, playerTarget.position);
        }

        if (currentPath != null && currentPathIndex < currentPath.Count)
        {
            MoveTowardsNode(currentPath[currentPathIndex]);
            if (Vector2.Distance(transform.position, currentPath[currentPathIndex]) < 0.3f)
                currentPathIndex++;
        }
        else
        {
            MoveTowardsNode(playerTarget.position);
        }
    }

    void RunBFS(Vector2 startPos, Vector2 targetPos)
    {
        Vector2Int startNode = WorldToGrid(startPos);
        Vector2Int targetNode = WorldToGrid(targetPos);
        if (startNode == targetNode) return;

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        frontier.Enqueue(startNode);
        
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        cameFrom[startNode] = startNode; 

        bool found = false;
        int steps = 0;
        while (frontier.Count > 0 && steps < maxSearchSteps)
        {
            Vector2Int current = frontier.Dequeue();
            steps++;
            if (current == targetNode) { found = true; break; }
            foreach (Vector2Int next in GetNeighbors(current))
            {
                if (!cameFrom.ContainsKey(next)) { frontier.Enqueue(next); cameFrom[next] = current; }
            }
        }
        if (found) ReconstructPath(cameFrom, startNode, targetNode);
    }

    List<Vector2Int> GetNeighbors(Vector2Int center)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int neighbor = center + dir;
            Vector2 worldPos = GridToWorld(neighbor);
            if (!Physics2D.OverlapCircle(worldPos, cellSize / 3f, obstacleLayer)) 
                neighbors.Add(neighbor);
        }
        return neighbors;
    }

    void ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int end)
    {
        currentPath.Clear();
        Vector2Int current = end;
        while (current != start) { currentPath.Add(GridToWorld(current)); current = cameFrom[current]; }
        currentPath.Reverse();
        currentPathIndex = 0;
    }

    Vector2Int WorldToGrid(Vector2 pos) => new Vector2Int(Mathf.RoundToInt(pos.x / cellSize), Mathf.RoundToInt(pos.y / cellSize));
    Vector2 GridToWorld(Vector2Int gridPos) => new Vector2(gridPos.x * cellSize, gridPos.y * cellSize);

    void MoveTowardsNode(Vector2 targetPos)
    {
        float dirX = (targetPos.x > transform.position.x + 0.1f) ? 1 : (targetPos.x < transform.position.x - 0.1f) ? -1 : 0;
        rb.linearVelocity = new Vector2(dirX * currentMoveSpeed, rb.linearVelocity.y);
        if (dirX != 0) FlipSprite(dirX);
        
        if (targetPos.y > transform.position.y + 0.7f && isGrounded) 
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    // --- 4. COMBATE ---
    void HandleAttack()
    {
        rb.linearVelocity = Vector2.zero;
        if (Time.time >= nextAttackTime)
        {
            DoAttack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void DoAttack()
    {
        if (attackPoint == null) return;
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);
        foreach (Collider2D hit in hitPlayers)
        {
            PhotonView targetView = hit.GetComponentInParent<PhotonView>();
            if (targetView != null)
                targetView.RPC("TakeDamageComplete", RpcTarget.All, attackDamage, photonView.ViewID, knockbackForce, stunTime);
        }
    }

    [PunRPC]
    public override void ApplyKnockbackRPC(Vector2 direction, float force, float time)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        currentState = AIState.Stunned;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
        StartCoroutine(ResetStun(time));
    }

    IEnumerator ResetStun(float time) { yield return new WaitForSeconds(time); currentState = AIState.Chase; }

    void FindTarget() { GameObject p = GameObject.FindGameObjectWithTag("Player"); if (p != null) playerTarget = p.transform; }

    bool CheckGrounded() => Physics2D.Raycast(transform.position, Vector2.down, 1.2f, obstacleLayer);

    private void FlipSprite(float dir)
    {
        if (spriteRenderer != null) spriteRenderer.flipX = (dir < 0);
        if (attackPoint != null)
        {
            float x = Mathf.Abs(attackPoint.localPosition.x) * (dir > 0 ? 1 : -1);
            attackPoint.localPosition = new Vector3(x, attackPoint.localPosition.y, 0);
        }
    }

    // --- 5. VISUALIZAÇÃO (GIZMOS) ---
    void OnDrawGizmos()
    {
        Vector2 center = Application.isPlaying ? spawnPosition : (Vector2)transform.position;

        // Limites de Patrulha (Branco)
        Gizmos.color = Color.white;
        Vector2 leftL = center + Vector2.left * patrolDistance;
        Vector2 rightL = center + Vector2.right * patrolDistance;
        Gizmos.DrawLine(leftL, rightL);
        Gizmos.DrawWireCube(leftL, new Vector3(0.1f, 1f, 0));
        Gizmos.DrawWireCube(rightL, new Vector3(0.1f, 1f, 0));

        // BFS (Ciano)
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < currentPath.Count - 1; i++)
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
        }

        // Visão (Amarelo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Ataque (Vermelho)
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
