using UnityEngine;
using System.Collections;
using Photon.Pun;
using ExitGames.Client.Photon;

[RequireComponent(typeof(Rigidbody2D), typeof(PhotonView))]
public class EnemyAI : MonoBehaviourPunCallbacks
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
    public float edgeCheckDistance = 0.6f;
    public float wallCheckDistancePatrol = 0.5f;
    public Transform groundCheckPoint;
    public LayerMask groundLayer;

    [Header("Perseguição / Salto")]
    public float jumpForce = 8f;
    public float jumpHeightTolerance = 1.5f;
    public float minJumpDistance = 0.5f;
    public float wallCheckDistanceChase = 0.5f;

    [Header("Combate / Knockback")]
    public float knockbackForce = 15f;
    public float stunTime = 0.5f;
    public int attackDamage = 10;           // Dano que o inimigo causa
    public float attackCooldown = 1.0f;     // Tempo entre ataques do inimigo
    public Transform attackPoint;           // Ponto de origem do ataque do inimigo
    public LayerMask playerLayer;         // Camada do Jogador (NOVA CAMADA NECESSÁRIA)

    // --- NOVO: Propriedades para acesso externo (EnemyHealth) ---
    public float KnockbackForce => knockbackForce;
    public float StunTime => stunTime;

    // --- 3. VARIÁVEIS PRIVADAS ---

    private Transform playerTarget;
    private Rigidbody2D rb;
    private float nextAttackTime = 0f;
    private PhotonView photonView;
    private int direction = 1;
    private bool isGrounded = false;

    // --- 4. FUNÇÕES BASE ---

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        photonView = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            currentState = AIState.Patrol;
            // A IA deve encontrar o jogador. Pode ser o mais próximo ou o primeiro.
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
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
            default:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    // --- 5. LÓGICA DE ESTADOS (INALTERADA) ---

    void HandlePatrol()
    {
        // ... (lógica de patrulha) ...
        rb.linearVelocity = new Vector2(patrolSpeed * direction, rb.linearVelocity.y);

        Vector3 checkPos = groundCheckPoint.position;
        Vector2 checkDir = new Vector2(direction, 0);

        RaycastHit2D edgeHit = Physics2D.Raycast(
            checkPos + new Vector3(direction * wallCheckDistancePatrol, 0, 0),
            Vector2.down,
            edgeCheckDistance,
            groundLayer
        );

        RaycastHit2D wallHit = Physics2D.Raycast(
            transform.position,
            checkDir,
            wallCheckDistancePatrol,
            groundLayer
        );

        if (edgeHit.collider == null || wallHit.collider != null)
        {
            direction *= -1;
            transform.localScale = new Vector3(direction, 1, 1);
        }

        if (playerTarget != null && Vector2.Distance(transform.position, playerTarget.position) < chaseRange)
        {
            currentState = AIState.Chase;
        }
    }

    void HandleChase()
    {
        // ... (lógica de perseguição e salto) ...
        if (playerTarget == null) return;

        Vector2 targetPos = playerTarget.position;
        Vector2 selfPos = transform.position;
        float distance = Vector2.Distance(selfPos, targetPos);
        float directionX = (targetPos.x > selfPos.x) ? 1 : -1;

        transform.localScale = new Vector3(directionX, 1, 1);

        // Transições
        if (distance <= attackRange)
        {
            currentState = AIState.Attack;
            return;
        }
        else if (distance > chaseRange * 1.5f)
        {
            currentState = AIState.Patrol;
            return;
        }

        // Movimento e Salto
        rb.linearVelocity = new Vector2(directionX * moveSpeed, rb.linearVelocity.y);

        RaycastHit2D wallHit = Physics2D.Raycast(
            selfPos,
            new Vector2(directionX, 0),
            wallCheckDistanceChase,
            groundLayer
        );

        if (wallHit.collider != null)
        {
            RaycastHit2D heightHit = Physics2D.Raycast(
                selfPos + new Vector2(directionX * wallCheckDistanceChase, 0),
                Vector2.up,
                jumpHeightTolerance,
                groundLayer
            );

            if (heightHit.collider == null)
            {
                TryJump();
            }
        }
        else if (targetPos.y > selfPos.y + 0.5f && distance > minJumpDistance)
        {
            TryJump();
        }
    }

    void HandleAttack()
    {
        if (playerTarget == null) return;

        float directionX = (playerTarget.position.x > transform.position.x) ? 1 : -1;
        transform.localScale = new Vector3(directionX, 1, 1);

        rb.linearVelocity = Vector2.zero;

        if (Time.time >= nextAttackTime)
        {
            // O ataque só é executado no Master Client (Autoridade)
            DoAttack();
            nextAttackTime = Time.time + attackCooldown;
            StartCoroutine(WaitAndTransitionTo(AIState.Chase, 0.5f));
        }
    }

    void HandleStunned()
    {
        // ...
    }


    // --- 6. FUNÇÕES DE COMBATE (CORRIGIDO) ---

    void DoAttack()
    {
        // 1. Deteção de acerto
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);

        foreach (Collider2D player in hitPlayers)
        {
            PhotonView targetView = player.GetComponent<PhotonView>();
            // Procura pelo script de vida do jogador (Health.cs)
            Health playerHealth = player.GetComponent<Health>();
            CombatSystem2D playerCombat = player.GetComponent<CombatSystem2D>(); // Para verificar defesa

            if (targetView != null && playerHealth != null)
            {
                // Determinar se o jogador está a defender (reduzindo o dano)
                bool playerDefending = (playerCombat != null && playerCombat.isDefending);
                int finalDamage = playerDefending ? attackDamage / 4 : attackDamage;

                // O Inimigo causa dano ao Jogador. Chama TakeDamage do Health.cs.
                // NOTE: O EnemyAI passa o SEU ViewID (do inimigo) como atacante.
                targetView.RPC(nameof(Health.TakeDamage), RpcTarget.All, finalDamage, photonView.ViewID);
                Debug.Log($"Inimigo atacou {player.name} com {finalDamage} de dano!");
            }
        }
    }

    /// <summary>
    /// Chamado por RPC (do EnemyHealth) no Master Client para aplicar Knockback e Stun.
    /// </summary>
    [PunRPC]
    public void ApplyKnockbackRPC(Vector2 direction, float force, float time)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        currentState = AIState.Stunned;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);

        StartCoroutine(ResetStun(time));
    }

    // --- 7. UTILS & COROUTINES (INALTERADA) ---

    bool CheckGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, 0.1f, groundLayer);
    }

    void TryJump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    IEnumerator ResetStun(float stunTime)
    {
        yield return new WaitForSeconds(stunTime);

        if (currentState == AIState.Stunned)
        {
            currentState = AIState.Chase;
        }
    }

    IEnumerator WaitAndTransitionTo(AIState newState, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentState == AIState.Attack)
        {
            currentState = newState;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
