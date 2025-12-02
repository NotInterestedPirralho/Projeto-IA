using UnityEngine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement2D : MonoBehaviourPunCallbacks
{
    [Header("Movimento")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;

    [Header("Pulo")]
    public float jumpForce = 10f;
    public int maxJumps = 2;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Wall Check")]
    private bool isTouchingWall = false;

    [Header("Knockback")]
    public bool isKnockedBack = false;

    // ===========================================
    // VARIÁVEIS PARA O STOMP (ID 14)
    // ===========================================
    [Header("Stomp")]
    public float stompForce = 7f; 
    public int stompDamage = 15; 
    public string enemyTag = "Enemy"; 
    public float minStompNormalY = 0.7f; 
    // ===========================================

    // ===========================================
    // VARIÁVEIS PARA O ÍCONE DE SPEED (ID 17)
    // ===========================================
    [Header("UI References")]
    [SerializeField] private Animator speedIconAnimator; 
    // ===========================================


    // --- VARIÁVEIS INTERNAS DO POWER UP ---
    private float defaultWalkSpeed;
    private float defaultSprintSpeed;
    private float defaultJumpForce;
    private Coroutine currentBuffRoutine; 
    // -------------------------------------

    // Propriedades de Acesso
    public float CurrentHorizontalSpeed => rb != null ? rb.linearVelocity.x : 0f;
    public bool IsGrounded => grounded;

    // --- Referências de Componentes e Singletons ---
    private Rigidbody2D rb;
    private bool sprinting;
    private bool grounded;
    private int jumpCount;
    private CombatSystem2D combatSystem;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private PhotonView pv;

    // Referências estáticas/Singleton (Opções)
    private GameChat chatInstance;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        combatSystem = GetComponent<CombatSystem2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        pv = GetComponent<PhotonView>();

        // Tenta obter a instância do Chat, se existir. (Agora Ativo)
        chatInstance = GameChat.instance; 

        // 1. GUARDAR OS VALORES ORIGINAIS NO INÍCIO
        defaultWalkSpeed = walkSpeed;
        defaultSprintSpeed = sprintSpeed;
        defaultJumpForce = jumpForce;

        if (groundCheck == null)
            Debug.LogWarning("GroundCheck não atribuído no inspector! Adicione um objeto filho para o check.");

        if (pv != null && !pv.IsMine)
        {
            enabled = false;
            return;
        }

        isKnockedBack = false;
    }

    // Método público para ser chamado pelo Health.cs
    public void SetKnockbackState(bool state)
    {
        isKnockedBack = state;
    }

    // ----------------------------------------------------
    // --- MÉTODOS DO POWER UP (Inalterados) ---
    // ----------------------------------------------------
    public void ActivateSpeedJumpBuff(float speedMultiplier, float jumpMultiplier, float duration)
    {
        if (pv != null && !pv.IsMine) return;

        if (currentBuffRoutine != null)
        {
            StopCoroutine(currentBuffRoutine);
            ResetStats();
        }

        currentBuffRoutine = StartCoroutine(BuffRoutine(duration, speedMultiplier, jumpMultiplier));
    }

    private IEnumerator BuffRoutine(float duration, float speedMult, float jumpMult)
    {
        walkSpeed = defaultWalkSpeed * speedMult;
        sprintSpeed = defaultSprintSpeed * speedMult;
        jumpForce = defaultJumpForce * jumpMult;

        if (speedIconAnimator != null)
        {
            speedIconAnimator.SetBool("IsBuffActive", true);
        }

        yield return new WaitForSeconds(duration);

        ResetStats();
        currentBuffRoutine = null;

        if (speedIconAnimator != null)
        {
            speedIconAnimator.SetBool("IsBuffActive", false);
        }
    }

    private void ResetStats()
    {
        walkSpeed = defaultWalkSpeed;
        sprintSpeed = defaultSprintSpeed;
        jumpForce = defaultJumpForce;
    }
    // ----------------------------------------------------


    void Update()
    {
        // BLOQUEIO 1: Multiplayer
        if (pv != null && !pv.IsMine) return;

        // B. Verificação de Chão
        if (groundCheck != null)
        {
            grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // ==========================================================
        // *** PRIORIDADE AO KNOCKBACK ***
        if (isKnockedBack)
        {
            if (anim) anim.SetBool("Grounded", grounded);
            return; // Bloqueia todo o input
        }
        // ==========================================================

        // ----------------------------------------------------
        // BLOQUEIO 2: ESTADOS DE JOGO (LOBBY, PAUSA, CHAT, DEFESA)
        // ----------------------------------------------------

        // A. Flag de Defesa (se o CombatSystem existir)
        bool isDefending = (combatSystem != null && combatSystem.isDefending);

        // B. Bloqueio do Chat
        bool isChatOpen = (chatInstance != null && chatInstance.IsChatOpen);

        // C. Bloqueio da Pausa (Usa a flag estática do PMMM)
        bool isPaused = PMMM.IsPausedLocally; 

        // D. Bloqueio do Lobby (Usa a flag estática do LobbyManager)
        bool lobbyBlocking = (LobbyManager.instance != null && !LobbyManager.GameStartedAndPlayerCanMove);

        // Se QUALQUER uma das condições de BLOQUEIO TOTAL (Lobby, Pausa, Chat) OU DE DEFESA estiver ativa
        if (lobbyBlocking || isPaused || isChatOpen || isDefending)
        {
            // O jogador deve PARAR de se mover horizontalmente
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            if (anim)
            {
                anim.SetFloat("Speed", 0f);
                anim.SetBool("IsSprinting", false);
            }

            // Se for bloqueio TOTAL (Lobby, Pausa, Chat), ignora o resto do input (incluindo o salto)
            if (lobbyBlocking || isPaused || isChatOpen)
            {
                if (anim) anim.SetBool("Grounded", grounded);
                return; 
            }
            
            // NOTA: Se for apenas isDefending, o jogador pode continuar o fluxo para poder saltar.
        }

        // LÓGICA DE RESET DE SALTO
        if (rb != null)
        {
            if (grounded && Mathf.Abs(rb.linearVelocity.y) <= 0.1f)
            {
                jumpCount = 0;
                isTouchingWall = false;
            }
            else if (isTouchingWall && !grounded)
            {
                jumpCount = 0;
            }
        }

        // --- LÓGICA DE MOVIMENTO E SALTO (SÓ se NÃO estiver sob Knockback, Bloqueio Total ou a defender) ---
        float move = 0f;

        // Movimento horizontal (Bloqueado se isDefending=true e o bloqueio total=false)
        if (!isDefending)
        {
            move = Input.GetAxisRaw("Horizontal");
            sprinting = Input.GetKey(KeyCode.LeftShift);

            float currentSpeed = sprinting ? sprintSpeed : walkSpeed;

            // Aplica a velocidade de movimento
            if (Mathf.Abs(move) > 0.05f)
            {
                rb.linearVelocity = new Vector2(move * currentSpeed, rb.linearVelocity.y);
            }
            else
            {
                // Se não houver input, parar o movimento horizontal
                if (!isTouchingWall || grounded)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
            }
        }

        // Salto com W (duplo salto) ou barra de espaço (Permitido mesmo durante a Defesa)
        bool jumpInput = Input.GetKeyDown(KeyCode.W) || Input.GetButtonDown("Jump");

        if (jumpInput && jumpCount < maxJumps)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCount++;
        }

        // Flip do sprite
        if (spriteRenderer != null)
        {
            if (move > 0.05f)
                spriteRenderer.flipX = false;
            else if (move < -0.05f)
                spriteRenderer.flipX = true;
        }

        // Atualizar Animator
        if (anim)
        {
            // O Speed deve ser 0 se estiver a defender, mesmo que o isDefending não tenha sido o bloqueio principal
            anim.SetFloat("Speed", isDefending ? 0f : Mathf.Abs(move));
            anim.SetBool("Grounded", grounded);
            bool isSprintAnim = !isDefending && sprinting && Mathf.Abs(move) > 0.05f;
            anim.SetBool("IsSprinting", isSprintAnim);
        }
    }

    // --- LÓGICA DE COLISÃO (OnCollisionEnter2D, OnCollisionStay2D, OnCollisionExit2D e Gizmos inalterados) ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (pv != null && pv.IsMine && collision.gameObject.CompareTag(enemyTag))
        {
            if (collision.contactCount > 0)
            {
                ContactPoint2D contact = collision.GetContact(0);

                if (contact.normal.y > minStompNormalY)
                {
                    PhotonView enemyView = collision.gameObject.GetComponent<PhotonView>();
                    if (enemyView != null)
                    {
                        enemyView.RPC("TakeDamage", RpcTarget.MasterClient, stompDamage, pv.ViewID);
                    }

                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * stompForce, ForceMode2D.Impulse);

                    return;
                }
            }
        }

        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (collision.contactCount > 0)
            {
                ContactPoint2D contact = collision.GetContact(0);

                if (contact.normal.y > 0.5f)
                {
                    grounded = true;
                    jumpCount = 0;
                    isTouchingWall = false;
                }
                else if (Mathf.Abs(contact.normal.x) > 0.5f && contact.normal.y < 0.5f)
                {
                    isTouchingWall = true;
                }
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (collision.contactCount > 0)
            {
                ContactPoint2D contact = collision.GetContact(0);

                if (contact.normal.y > 0.5f)
                {
                    grounded = true;
                    isTouchingWall = false;
                }

                if (Mathf.Abs(contact.normal.x) > 0.5f && contact.normal.y < 0.5f)
                {
                    isTouchingWall = true;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isTouchingWall = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = grounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
