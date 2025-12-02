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
    public float stompForce = 7f;           // Força de salto do jogador após o Stomp (Bounce) - AJUSTADO PARA UM VALOR MAIS BAIXO
    public int stompDamage = 15;            // Dano que o Stomp causa ao inimigo
    public string enemyTag = "Enemy";       // A Tag que os seus inimigos usam
    public float minStompNormalY = 0.7f;    // Mínimo Y da normal para contar como Stomp
                                            // ===========================================

    // ===========================================
    // VARIÁVEIS PARA O ÍCONE DE SPEED (ID 17)
    // ===========================================
    [Header("UI References")]
    [SerializeField] private Animator speedIconAnimator; // Para ligar/desligar a animação do ícone
    // ===========================================


    // --- VARIÁVEIS INTERNAS DO POWER UP ---
    private float defaultWalkSpeed;
    private float defaultSprintSpeed;
    private float defaultJumpForce;
    private Coroutine currentBuffRoutine; // Para gerir o tempo do buff
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

        // Tenta obter a instância do Chat, se existir.
        // chatInstance = GameChat.instance; 

        // 1. GUARDAR OS VALORES ORIGINAIS NO INÍCIO (Valores base para o Reset)
        defaultWalkSpeed = walkSpeed;
        defaultSprintSpeed = sprintSpeed;
        defaultJumpForce = jumpForce;

        if (groundCheck == null)
            Debug.LogWarning("GroundCheck não atribuído no inspector! Adicione um objeto filho para o check.");

        // Se o Photon View existir E não for o jogador local, desativa.
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
    // --- MÉTODOS DO POWER UP ---
    // ----------------------------------------------------

    /// <summary>
    /// Ativa um buff temporário de velocidade e salto.
    /// Chamado por um PowerUp.cs ou similar.
    /// </summary>
    public void ActivateSpeedJumpBuff(float speedMultiplier, float jumpMultiplier, float duration)
    {
        // Garante que só o dono local ativa o buff
        if (pv != null && !pv.IsMine) return;

        // Se já houver um buff ativo, para o anterior e reseta os stats.
        if (currentBuffRoutine != null)
        {
            StopCoroutine(currentBuffRoutine);
            ResetStats();
        }

        // Inicia a nova corrotina
        currentBuffRoutine = StartCoroutine(BuffRoutine(duration, speedMultiplier, jumpMultiplier));
    }

    private IEnumerator BuffRoutine(float duration, float speedMult, float jumpMult)
    {
        // Aplica os multiplicadores aos valores base
        walkSpeed = defaultWalkSpeed * speedMult;
        sprintSpeed = defaultSprintSpeed * speedMult;
        jumpForce = defaultJumpForce * jumpMult;

        // NOVO: 1. ATIVA o ícone de velocidade (ID 17)
        if (speedIconAnimator != null)
        {
            speedIconAnimator.SetBool("IsBuffActive", true);
        }

        // Opcional: Adicionar efeitos visuais/sonoros aqui

        yield return new WaitForSeconds(duration);

        // O tempo acabou, resetar stats
        ResetStats();
        currentBuffRoutine = null;

        // NOVO: 2. DESATIVA o ícone de velocidade (ID 17)
        if (speedIconAnimator != null)
        {
            speedIconAnimator.SetBool("IsBuffActive", false);
        }
    }

    private void ResetStats()
    {
        // Volta aos valores base
        walkSpeed = defaultWalkSpeed;
        sprintSpeed = defaultSprintSpeed;
        jumpForce = defaultJumpForce;

        // Opcional: Remover efeitos visuais/sonoros aqui
    }
    // ----------------------------------------------------


    void Update()
    {
        // BLOQUEIO 1: Multiplayer (Apenas o jogador local deve controlar)
        if (pv != null && !pv.IsMine) return;

        // B. Verificação de Chão
        if (groundCheck != null)
        {
            grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        bool isDefending = (combatSystem != null && combatSystem.isDefending);

        // Bloqueio do Chat: (Opcional)
        bool isChatOpen = (chatInstance != null && chatInstance.IsChatOpen);

        // Bloqueio da Pausa: (Opcional)
        // bool isPaused = (PMMM.instance != null && PMMM.IsPausedLocally); 
        bool isPaused = false;

        // ==========================================================
        // *** CORREÇÃO: PRIORIDADE AO KNOCKBACK ***
        // Se estiver a sofrer Knockback, sai imediatamente para deixar o impulso do Health.cs dominar a física.
        if (isKnockedBack)
        {
            if (anim) anim.SetBool("Grounded", grounded);
            return;
        }
        // ==========================================================

        // ----------------------------------------------------
        // BLOQUEIO 2: ESTADOS DE JOGO (LOBBY, PAUSA, CHAT, DEFESA)
        // ----------------------------------------------------

        // A. Bloqueio do Lobby
        // bool lobbyBlocking = (LobbyManager.instance != null && !LobbyManager.GameStartedAndPlayerCanMove);
        bool lobbyBlocking = false;

        if (lobbyBlocking || isPaused || isChatOpen || isDefending)
        {
            // Garante que o jogador para horizontalmente
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            if (anim)
            {
                anim.SetFloat("Speed", 0f);
                anim.SetBool("IsSprinting", false);
            }

            // Se for bloqueio total (Lobby, Pausa, Chat), ignora o resto do input
            if (lobbyBlocking || isPaused || isChatOpen)
            {
                if (anim) anim.SetBool("Grounded", grounded);
                return;
            }
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

        float move = 0f;

        // LÓGICA DE MOVIMENTO E SALTO (SÓ se NÃO estiver a defender/bloqueado)

        // Movimento horizontal
        move = Input.GetAxisRaw("Horizontal");
        sprinting = Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = sprinting ? sprintSpeed : walkSpeed;

        // Aplica a velocidade de movimento
        if (Mathf.Abs(move) > 0.05f)
        {
            // Aplica a nova velocidade horizontal, mantendo a vertical
            rb.linearVelocity = new Vector2(move * currentSpeed, rb.linearVelocity.y);
        }
        else
        {
            // Se não houver input, parar o movimento horizontal
            if (!isTouchingWall || grounded)
            {
                // Garante que o jogador para se não houver input
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        // Salto com W (duplo salto) ou barra de espaço
        bool jumpInput = Input.GetKeyDown(KeyCode.W) || Input.GetButtonDown("Jump");

        if (jumpInput && jumpCount < maxJumps)
        {
            // Resetar a velocidade vertical antes de aplicar a nova força de pulo para consistência
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
            anim.SetFloat("Speed", isDefending ? 0f : Mathf.Abs(move));
            anim.SetBool("Grounded", grounded);
            bool isSprintAnim = !isDefending && sprinting && Mathf.Abs(move) > 0.05f;
            anim.SetBool("IsSprinting", isSprintAnim);
        }
    }

    // --- LÓGICA DE COLISÃO (Ground/Wall Check e Stomp) ---
    // ===========================================
    // MÉTODO ONCOLLISIONENTER2D ATUALIZADO (ID 14)
    // ===========================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. **VERIFICAR STOMP CONTRA INIMIGO**
        // Apenas o jogador local deve lidar com a física do Stomp
        if (pv != null && pv.IsMine && collision.gameObject.CompareTag(enemyTag))
        {
            if (collision.contactCount > 0)
            {
                ContactPoint2D contact = collision.GetContact(0);

                // 1.1. Verifica se a colisão veio de CIMA (Normal Y alta)
                if (contact.normal.y > minStompNormalY)
                {
                    // LÓGICA DE STOMP BEM-SUCEDIDA! (JOGADOR GERE O DANO)

                    // A. Causar dano ao inimigo via RPC
                    PhotonView enemyView = collision.gameObject.GetComponent<PhotonView>();
                    if (enemyView != null)
                    {
                        // Chamamos o RPC de dano no inimigo, enviando o nosso ID (pv.ViewID)
                        enemyView.RPC("TakeDamage", RpcTarget.MasterClient, stompDamage, pv.ViewID);
                    }

                    // B. Aplicar o salto (bounce) no jogador
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * stompForce, ForceMode2D.Impulse);

                    // Saímos imediatamente para não processar como colisão normal de chão
                    return;
                }

                // 1.2. Colisão Lateral/Baixo: Se não for stomp, o Player leva dano (assumimos que o inimigo trata disso)
            }
        }

        // 2. **LÓGICA NORMAL DE COLISÃO (CHÃO E PAREDE)**
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
            // Verifica se está realmente a sair do chão/parede (simplificação)
            isTouchingWall = false;
            // Nota: O estado 'grounded' é tratado pelo Physics2D.OverlapCircle no Update()
        }
    }

    // Gizmos
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = grounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}