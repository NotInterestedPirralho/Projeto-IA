using UnityEngine;
using System.Collections;
using Photon.Pun;
using ExitGames.Client.Photon;

[RequireComponent(typeof(Rigidbody2D), typeof(PhotonView))]
public class EnemyAI : EnemyBase 
{
    // --- 1. DEFINIÇÃO DE ESTADOS ---
    public enum AIState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Stunned
    }

    // --- 2. VARIÁVEIS DE CONFIGURAÇÃO ---
    [Header("Geral")]
    public AIState currentState;
    public float chaseRange = 8f;
    public float attackRange = 1.5f;
    public float moveSpeed = 3f;

    [Header("Patrulha")]
    public float patrolSpeed = 1.5f;
    public float patrolDistance = 5f;
    public float edgeCheckDistance = 1f;
    public float wallCheckDistancePatrol = 0.5f;
    public Transform groundCheckPoint;
    public LayerMask groundLayer;

    [Header("Perseguição / Salto")]
    public float jumpForce = 8f;
    public float jumpHeightTolerance = 1.5f;
    public float minJumpDistance = 0.5f;
    public float wallCheckDistanceChase = 0.5f;

    [Header("Combate / Knockback")]
    public float knockbackForce = 10f;
    public float stunTime = 0.5f;
    public int attackDamage = 7;
    public float attackCooldown = 1.5f;
    public float attackOffsetDistance = 0.5f;

    public Transform attackPoint;
    public LayerMask playerLayer;

    public override float KnockbackForce => knockbackForce;
    public override float StunTime => stunTime;

    // --- 3. VARIÁVEIS PRIVADAS ---
    private Transform playerTarget;
    private Rigidbody2D rb;
    private float nextAttackTime = 0f;
    private PhotonView photonView;
    private int direction = 1;
    private Vector2 patrolOrigin;
    private bool isGrounded = false;
    private SpriteRenderer spriteRenderer;

    // --- 4. FUNÇÕES BASE ---
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
            currentState = AIState.Patrol;
            patrolOrigin = transform.position;
            FindTarget();
        }
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        isGrounded = CheckGrounded();

        switch (currentState)
        {
            case AIState.Patrol:
                HandlePatrol();
                break;
            case AIState.Chase:
                HandleChase();
                break;
            case AIState.Attack:
                HandleAttack();
                break;
            case AIState.Stunned:
                HandleStunned();
                break;
        }

        ApplyArenaLimits(); 
    }

    // --- 5. LÓGICA DE ESTADOS ---

    void HandlePatrol()
    {
        // Movimento lateral
        rb.linearVelocity = new Vector2(patrolSpeed * direction, rb.linearVelocity.y);

        // Verificações de ambiente
        Vector2 checkDir = new Vector2(direction, 0);
        RaycastHit2D edgeHit = Physics2D.Raycast(groundCheckPoint.position, Vector2.down, edgeCheckDistance, groundLayer);
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, checkDir, wallCheckDistancePatrol, groundLayer);

        // Verificação de limites de Arena
        bool hitArenaLimit = false;
        if (EnemyArenaConfig.instance != null)
        {
            if (direction == 1 && transform.position.x >= EnemyArenaConfig.instance.maxX - 0.5f) hitArenaLimit = true;
            if (direction == -1 && transform.position.x <= EnemyArenaConfig.instance.minX + 0.5f) hitArenaLimit = true;
        }

        // Verificação de distância máxima da patrulha
        float distanceToOrigin = Mathf.Abs(transform.position.x - patrolOrigin.x);
        bool reachedMaxPatrol = distanceToOrigin >= patrolDistance;

        // Inverter direção se: Bater parede, chegar ao fim do chão, limite da arena ou fim do raio de patrulha
        if (edgeHit.collider == null || wallHit.collider != null || hitArenaLimit || reachedMaxPatrol)
        {
            // Se foi por distância, garante que vira para o lado correto (centro)
            if (reachedMaxPatrol)
            {
                direction = (transform.position.x > patrolOrigin.x) ? -1 : 1;
            }
            else
            {
                direction *= -1;
            }
        }

        FlipSprite(direction);

        // Se ver o player, persegue
        if (CanSeePlayer()) currentState = AIState.Chase;
    }

    void HandleChase()
    {
        if (playerTarget == null) { currentState = AIState.Patrol; return; }

        float distance = Vector2.Distance(transform.position, playerTarget.position);
        float directionX = (playerTarget.position.x > transform.position.x) ? 1 : -1;

        // Se perder o player (distância ou visão), volta a patrulhar
        if (distance > chaseRange * 1.2f || !CanSeePlayer())
        {
            currentState = AIState.Patrol;
            // Opcional: define a nova origem de patrulha para onde ele está agora
            patrolOrigin = transform.position; 
            return;
        }

        FlipSprite(directionX);

        if (distance <= attackRange)
        {
            currentState = AIState.Attack;
            return;
        }

        rb.linearVelocity = new Vector2(directionX * moveSpeed, rb.linearVelocity.y);

        // Lógica de salto
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, new Vector2(directionX, 0), wallCheckDistanceChase, groundLayer);
        if (wallHit.collider != null)
        {
            RaycastHit2D heightHit = Physics2D.Raycast(transform.position + new Vector3(directionX * wallCheckDistanceChase, 0), Vector2.up, jumpHeightTolerance, groundLayer);
            if (heightHit.collider == null) TryJump();
        }
        else if (playerTarget.position.y > transform.position.y + 0.5f && distance > minJumpDistance)
        {
            TryJump();
        }
    }

    void HandleAttack()
    {
        if (playerTarget == null) { currentState = AIState.Patrol; return; }

        float directionX = (playerTarget.position.x > transform.position.x) ? 1 : -1;
        FlipSprite(directionX);
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (Time.time >= nextAttackTime)
        {
            DoAttack();
            nextAttackTime = Time.time + attackCooldown;
            StartCoroutine(WaitAndTransitionTo(AIState.Chase, 0.5f));
        }
    }

    void HandleStunned() { /* RB controlado pelo AddForce no RPC */ }

    // --- 6. MÉTODOS AUXILIARES ---

    private void FlipSprite(float currentDirection)
    {
        if (spriteRenderer == null || attackPoint == null) return;
        spriteRenderer.flipX = (currentDirection < 0);
        float newLocalX = attackOffsetDistance * Mathf.Sign(currentDirection);
        attackPoint.localPosition = new Vector3(newLocalX, attackPoint.localPosition.y, attackPoint.localPosition.z);
    }

    void DoAttack()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);
        foreach (Collider2D hit in hitPlayers)
        {
            PhotonView targetView = hit.GetComponentInParent<PhotonView>();
            CombatSystem2D playerCombat = hit.GetComponentInParent<CombatSystem2D>();

            if (targetView != null)
            {
                bool playerDefending = (playerCombat != null && playerCombat.isDefending);
                int finalDamage = playerDefending ? attackDamage / 4 : attackDamage;

                targetView.RPC("TakeDamageComplete", RpcTarget.All, finalDamage, photonView.ViewID, knockbackForce, stunTime);
            }
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

    bool CanSeePlayer()
    {
        if (playerTarget == null) return false;
        Vector2 selfPos = transform.position;
        Vector2 targetPos = playerTarget.position;
        if (Vector2.Distance(selfPos, targetPos) > chaseRange) return false;

        RaycastHit2D hit = Physics2D.Linecast(selfPos, targetPos, groundLayer);
        return hit.collider == null;
    }

    bool CheckGrounded() => Physics2D.Raycast(transform.position, Vector2.down, 0.2f, groundLayer);

    void TryJump() { if (isGrounded) rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); }

    void FindTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0) playerTarget = players[0].transform;
    }

    IEnumerator ResetStun(float time)
    {
        yield return new WaitForSeconds(time);
        if (currentState == AIState.Stunned) currentState = AIState.Chase;
    }

    IEnumerator WaitAndTransitionTo(AIState newState, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentState == AIState.Attack) currentState = newState;
    }

    // --- DEBUG VISUAL ---
    void OnDrawGizmosSelected()
    {
        // 1. Área de Ataque (Esfera Vermelha)
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }

        // 2. Raio de Visão/Perseguição (Esfera Amarela)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // 3. DISTÂNCIA DE PATRULHA (Linha Branca)
        // Só desenha se estivermos no modo Play ou se já tivermos uma origem definida
        Vector2 center = Application.isPlaying ? patrolOrigin : (Vector2)transform.position;
        
        Gizmos.color = Color.white;
        Vector3 leftLimit = new Vector3(center.x - patrolDistance, center.y, 0);
        Vector3 rightLimit = new Vector3(center.x + patrolDistance, center.y, 0);

        Gizmos.DrawLine(leftLimit, rightLimit);
        
        // Desenha "travões" nas pontas da linha de patrulha
        Gizmos.DrawLine(leftLimit + Vector3.up * 0.5f, leftLimit + Vector3.down * 0.5f);
        Gizmos.DrawLine(rightLimit + Vector3.up * 0.5f, rightLimit + Vector3.down * 0.5f);

        // 4. Raycast de Chão e Parede (Linhas Verdes)
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(groundCheckPoint.position, Vector2.down * edgeCheckDistance);
            Gizmos.DrawRay(transform.position, new Vector2(direction, 0) * wallCheckDistancePatrol);
        }
    }
}
