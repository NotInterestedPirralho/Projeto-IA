using UnityEngine;
using Photon.Pun;
using System.Collections; // Necessário para Coroutines

// Garante que o jogador tenha um PhotonView
[RequireComponent(typeof(PhotonView))]
public class Movement2D : MonoBehaviourPunCallbacks
{
    [Header("Configurações Base")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    // Componentes
    private Rigidbody2D rb;
    private Animator anim;
    private PhotonView pv;

    private bool isFacingRight = true;
    
    // --- Variáveis de Knockback e Buff ---
    [HideInInspector] public bool isKnockedBack = false; // Estado para bloquear o movimento durante a repulsão

    [Header("Buffs")]
    private float originalMoveSpeed;
    private float originalJumpForce;
    private Coroutine buffCoroutine; // Para gerir o tempo do buff

    // PROPRIEDADES PÚBLICAS PARA SINCRONIZAÇÃO (NOVO E CRÍTICO)
    // O PlayerSetup usa estas propriedades para enviar o estado pela rede.
    public float CurrentHorizontalSpeed => rb.linearVelocity.x;
    public bool IsGrounded => CheckIfGrounded(); 
    // ***************************************************************

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Este script só deve ser ativado para o jogador local.
        enabled = pv.IsMine;
    }

    void Update()
    {
        // BLOQUEIO CRUCIAL DE INPUTS
        // Bloqueia se estiver pausado, no chat, OU A SER REPELIDO (Knocked Back).
        if (PMMM.IsPausedLocally || GameChat.IsChatOpen || isKnockedBack)
        {
            // Se estiver bloqueado devido a pausa/chat, garante que o movimento para.
            if (rb != null && !isKnockedBack)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
            }
            
            if (anim != null)
            {
                anim.SetBool("IsRunning", false);
            }
            return; // IGNORA O RESTO DOS INPUTS
        }

        // --- Lógica de Movimento e Pulo (Apenas para o jogador local) ---
        HandleHorizontalMovement();
        HandleJump();

        // Lógica de Animação (também apenas local)
        UpdateAnimations();
    }

    // ------------------------------------
    // --- LÓGICA DE MOVIMENTO E SALTO ---
    // ------------------------------------

    private void HandleHorizontalMovement()
    {
        float move = Input.GetAxisRaw("Horizontal");
        
        // Aplica a velocidade horizontal
        rb.linearVelocity = new Vector2(move * moveSpeed, rb.linearVelocity.y);

        // Controla o 'Flip' (virar o sprite)
        if (move < 0 && isFacingRight)
        {
            Flip();
        }
        else if (move > 0 && !isFacingRight)
        {
            Flip();
        }
    }

    private void HandleJump()
    {
        // Usa a nova propriedade IsGrounded para verificar o estado no chão
        if (Input.GetButtonDown("Jump") && IsGrounded)
        {
            // Aplica a força de salto (limpando a velocidade Y anterior)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            if (anim != null)
            {
                anim.SetTrigger("Jump");
            }
        }
    }

    private bool CheckIfGrounded()
    {
        // Método privado que a propriedade IsGrounded usa
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        
        // Inverte a escala X do transform para virar o sprite
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;
        
        bool isRunning = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        anim.SetBool("IsRunning", isRunning);

        // Usa a nova propriedade IsGrounded
        anim.SetBool("IsGrounded", IsGrounded);
        
        anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }
    
    // ------------------------------------
    // --- LÓGICA DE KNOCKBACK ---
    // ------------------------------------

    /// <summary>
    /// Define o estado de Knockback. Usado por Health.cs para bloquear inputs
    /// enquanto a força de repulsão está a ser aplicada.
    /// </summary>
    public void SetKnockbackState(bool state) 
    {
        isKnockedBack = state;
        Debug.Log($"Knockback State: {state}");
    }


    // ------------------------------------
    // --- LÓGICA DE POWER-UP ---
    // ------------------------------------
    
    /// <summary>
    /// Aplica e gere um buff de Velocidade e Salto no jogador.
    /// </summary>
    public void ActivateSpeedJumpBuff(float speedMultiplier, float jumpMultiplier, float duration)
    {
        if (!pv.IsMine) return;

        if (buffCoroutine != null)
        {
            StopCoroutine(buffCoroutine);
        }
        
        originalMoveSpeed = moveSpeed;
        originalJumpForce = jumpForce;

        moveSpeed *= speedMultiplier;
        jumpForce *= jumpMultiplier;
        
        buffCoroutine = StartCoroutine(RemoveSpeedJumpBuffAfterTime(duration));
        
        Debug.Log($"Buff ativado! Vel. Atual: {moveSpeed}, Salto Atual: {jumpForce}");
    }

    private IEnumerator RemoveSpeedJumpBuffAfterTime(float duration)
    {
        yield return new WaitForSeconds(duration);

        moveSpeed = originalMoveSpeed;
        jumpForce = originalJumpForce;
        buffCoroutine = null;

        Debug.Log("Buff terminado. Valores originais restaurados.");
    }
    
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
