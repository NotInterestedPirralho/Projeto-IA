using UnityEngine;
using Photon.Pun;
using System.Collections; 

// Garante que o jogador tenha um PhotonView
[RequireComponent(typeof(PhotonView))]
public class Movement2D : MonoBehaviourPunCallbacks
{
    [Header("Configurações Base")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    
    // --- Variáveis de Double Jump ---
    [Header("Salto")]
    [Tooltip("Número máximo de saltos permitidos (2 para Double Jump, 3 para Triple Jump, etc.).")]
    public int maxJumps = 2; 
    private int jumpsRemaining; // Contador de saltos disponíveis
    
    // --- Variáveis de Verificação de Chão ---
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    // Componentes
    private Rigidbody2D rb;
    private Animator anim;
    private PhotonView pv;

    private bool isFacingRight = true;
    
    // --- Variáveis de Knockback e Buff ---
    [HideInInspector] public bool isKnockedBack = false; 

    [Header("Buffs")]
    private float originalMoveSpeed;
    private float originalJumpForce;
    private Coroutine buffCoroutine; 

    // PROPRIEDADES PÚBLICAS PARA SINCRONIZAÇÃO
    public float CurrentHorizontalSpeed => rb.linearVelocity.x;
    public bool IsGrounded => CheckIfGrounded(); 

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        
        jumpsRemaining = maxJumps;

        enabled = pv.IsMine;
    }

    void Update()
    {
        // BLOQUEIO CRUCIAL DE INPUTS
        if (PMMM.IsPausedLocally || GameChat.IsChatOpen || isKnockedBack)
        {
            if (rb != null && !isKnockedBack)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
            }
            
            if (anim != null)
            {
                anim.SetBool("IsRunning", false);
            }
            return; 
        }

        // --- Lógica de Movimento e Pulo (Apenas para o jogador local) ---
        HandleHorizontalMovement();
        HandleJump(); // Contém a nova lógica para W e Jump

        // Lógica de Animação (também apenas local)
        UpdateAnimations();
    }
    
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

    // LÓGICA DE SALTO COM TECLA W E JUMP (ESPAÇO)
    private void HandleJump()
    {
        // 1. Resetar o contador de saltos se estiver no chão.
        if (IsGrounded)
        {
            jumpsRemaining = maxJumps;
        }

        // Verifica se o Input de Salto ocorreu (Barra de Espaço OU Tecla W)
        bool jumpInput = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W);

        // 2. Se o Input for pressionado e o jogador tiver saltos disponíveis.
        if (jumpInput && jumpsRemaining > 0)
        {
            // Decrementa o contador de saltos disponíveis.
            jumpsRemaining--;

            // Aplica a força de salto
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            if (anim != null)
            {
                anim.SetTrigger("Jump");
            }
        }
    }

    private bool CheckIfGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;
        
        bool isRunning = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        anim.SetBool("IsRunning", isRunning);

        anim.SetBool("IsGrounded", IsGrounded);
        
        anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }
    
    // ------------------------------------
    // --- LÓGICA DE KNOCKBACK E POWER-UP (Inalterada) ---
    // ------------------------------------

    public void SetKnockbackState(bool state) 
    {
        isKnockedBack = state;
        Debug.Log($"Knockback State: {state}");
    }
    
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
