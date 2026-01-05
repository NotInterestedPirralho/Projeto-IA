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
    public GameObject jumpVFXPrefab; 

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;

    [Header("Wall Check")]
    public LayerMask groundLayer;

    [Header("Knockback")]
    public bool isKnockedBack = false;

    // --- VARIÁVEIS INTERNAS ---
    private float defaultWalkSpeed;
    private float defaultSprintSpeed;
    private float defaultJumpForce;
    private Coroutine currentBuffRoutine;

    public float CurrentHorizontalSpeed => rb != null ? rb.linearVelocity.x : 0f;
    public bool IsGrounded => grounded;

    private Rigidbody2D rb;
    private bool sprinting;
    private bool grounded;
    private int jumpCount;
    private CombatSystem2D combatSystem;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private PhotonView pv;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        combatSystem = GetComponent<CombatSystem2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        pv = GetComponent<PhotonView>();

        // Guardar valores originais
        defaultWalkSpeed = walkSpeed;
        defaultSprintSpeed = sprintSpeed;
        defaultJumpForce = jumpForce;

        if (groundCheck == null)
            Debug.LogWarning("GroundCheck não atribuído no inspector!");

        if (pv != null && !pv.IsMine)
        {
            enabled = false;
            return;
        }

        isKnockedBack = false;
    }

    void Update()
    {
        if (pv != null && !pv.IsMine) return;

        // 1. VERIFICAÇÃO DE BLOQUEIOS (A CORREÇÃO ESTÁ AQUI)
        bool isChatOpen = (GameChat.instance != null && GameChat.instance.IsChatOpen);
        bool isPaused = PMMM.IsPausedLocally; 
        bool lobbyBlocking = (LobbyManager.instance != null && !LobbyManager.GameStartedAndPlayerCanMove);
        
        // Bloqueios internos de combate
        bool isDefending = (combatSystem != null && combatSystem.isDefending);
        bool isChargingAttack = (combatSystem != null && combatSystem.isCharging);
        bool isMovementBlocked = isDefending || isChargingAttack; 

        // ==========================================================
        // PRIORIDADE AO KNOCKBACK
        // ==========================================================
        if (isKnockedBack)
        {
            if (anim) anim.SetBool("Grounded", grounded);
            return;
        }

        // ==========================================================
        // BLOQUEIOS (CHAT, PAUSA, LOBBY, DEFESA, CARREGAMENTO)
        // ==========================================================

        // 2. APLICAÇÃO DOS BLOQUEIOS
        if (lobbyBlocking || isPaused || isChatOpen || isMovementBlocked)
        {
            // Para o movimento horizontal mas mantém a gravidade (velocidade Y)
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (anim)
            {
                anim.SetFloat("Speed", 0f);
                anim.SetBool("IsSprinting", false);
            }

            // Se for um bloqueio externo (Menu/Chat/Lobby), interrompe o Update aqui (não pula, não vira)
            if (lobbyBlocking || isPaused || isChatOpen)
            {
                if (anim) anim.SetBool("Grounded", grounded);
                return; 
            }
        }

        // ==================================================================
        // INPUT DE MOVIMENTO (Só é processado se não houver Bloqueio Total)
        // ==================================================================
        // 3. LÓGICA DE MOVIMENTO NORMAL
        HandleGroundCheck();
        HandleJumpReset();
        HandleHorizontalMovement(isMovementBlocked);
        HandleJump(isMovementBlocked);
        HandleFlip();
        HandleAnimations(isMovementBlocked);
    }

    // RESET SALTO
    private void HandleGroundCheck()
    {
        if (groundCheck != null)
        {
            grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
    }

    private void HandleJumpReset()
    {
        if (grounded)
        {
            jumpCount = 0;
        }
    }

    private void HandleHorizontalMovement(bool blocked)
    {
        if (blocked) return;

        float move = Input.GetAxisRaw("Horizontal");
        sprinting = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = sprinting ? sprintSpeed : walkSpeed;

        if (Mathf.Abs(move) > 0.05f)
        {
            rb.linearVelocity = new Vector2(move * currentSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void HandleJump(bool blocked)
    {
        if (blocked) return;

        bool jumpInput = Input.GetKeyDown(KeyCode.W) || Input.GetButtonDown("Jump");

        if (jumpInput && jumpCount < maxJumps)
        {
            // Aplica a força de salto
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            
            if (jumpCount == 1) SpawnJumpVFX();
            
            jumpCount++;
        }
    }

    private void HandleFlip()
    {
        float directionInput = Input.GetAxisRaw("Horizontal"); 
        if (directionInput > 0.05f) spriteRenderer.flipX = false;
        else if (directionInput < -0.05f) spriteRenderer.flipX = true;
    }

    private void HandleAnimations(bool blocked)
    {
        if (!anim) return;

        float moveInput = Input.GetAxisRaw("Horizontal");
        anim.SetFloat("Speed", blocked ? 0f : Mathf.Abs(moveInput));
        anim.SetBool("Grounded", grounded);
        
        bool isSprintAnim = !blocked && sprinting && Mathf.Abs(moveInput) > 0.05f; 
        anim.SetBool("IsSprinting", isSprintAnim);
    }

    // --- MÉTODOS PHOTON / VFX ---

    private void SpawnJumpVFX()
    {
        if (pv != null && pv.IsMine)
            pv.RPC("SpawnJumpVFX_RPC", RpcTarget.All);
    }

    [PunRPC]
    private void SpawnJumpVFX_RPC()
    {
        if (jumpVFXPrefab != null && groundCheck != null)
        {
            GameObject vfx = Instantiate(jumpVFXPrefab, groundCheck.position, Quaternion.identity);
            Destroy(vfx, 2f); 
        }
    }

    public void SetKnockbackState(bool state) => isKnockedBack = state;

    // --- GIZMOS ---
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            // Muda a cor do Gizmo no Editor: Verde se estiver no chão, Vermelho se estiver no ar
            Gizmos.color = grounded ? Color.green : Color.red;
            
            // Desenha a esfera que representa o raio de detecção do chão
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    // --- MÉTODOS DE BUFF (POWER-UPS) ---
    public void ActivateSpeedJumpBuff(float speedMultiplier, float jumpMultiplier, float duration)
    {
        // Só aplica o buff se este for o nosso player local
        if (pv != null && !pv.IsMine) return;

        if (currentBuffRoutine != null)
        {
            StopCoroutine(currentBuffRoutine);
            ResetStats();
        }

        currentBuffRoutine = StartCoroutine(BuffRoutineMultipliers(duration, speedMultiplier, jumpMultiplier));
    }

    private IEnumerator BuffRoutineMultipliers(float duration, float speedMult, float jumpMult)
    {
        // Aplica os multiplicadores
        walkSpeed = defaultWalkSpeed * speedMult;
        sprintSpeed = defaultSprintSpeed * speedMult;
        jumpForce = defaultJumpForce * jumpMult;

        Debug.Log($"Buff Ativado: Velocidade x{speedMult}, Pulo x{jumpMult}");

        yield return new WaitForSeconds(duration);

        ResetStats();
        currentBuffRoutine = null;
        Debug.Log("Buff Terminou: Stats resetados.");
    }

    private void ResetStats()
    {
        walkSpeed = defaultWalkSpeed;
        sprintSpeed = defaultSprintSpeed;
        jumpForce = defaultJumpForce;
    }

    // Caso uses o outro powerup de Speed simples via RPC
    [PunRPC]
    public void BoostSpeed(float boostAmount, float duration)
    {
        if (currentBuffRoutine != null)
        {
            StopCoroutine(currentBuffRoutine);
            ResetStats();
        }
        currentBuffRoutine = StartCoroutine(SpeedBuffRoutine(boostAmount, duration));
    }

    private IEnumerator SpeedBuffRoutine(float boostAmount, float duration)
    {
        walkSpeed += boostAmount;
        sprintSpeed += boostAmount;

        yield return new WaitForSeconds(duration);

        ResetStats();
        currentBuffRoutine = null;
    }
}
