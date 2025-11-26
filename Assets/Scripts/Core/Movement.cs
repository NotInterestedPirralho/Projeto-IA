using UnityEngine;
using Photon.Pun;
using System.Collections; 
using System.Linq; // Necessário para aceder a Collision2D.contacts

// Garante que o jogador tenha um PhotonView
[RequireComponent(typeof(PhotonView))]
public class Movement2D : MonoBehaviourPunCallbacks
{
    [Header("Configurações Base")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    // --- Variáveis de Corrida ---
    [Header("Corrida")]
    public float sprintMultiplier = 1.8f; 
    private float currentMoveSpeed; 

    // --- Variáveis de Double Jump ---
    [Header("Salto")]
    [Tooltip("Número máximo de saltos permitidos.")]
    public int maxJumps = 2; 
    private int jumpsRemaining; 
    
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
    // IsGrounded é mantido para animações/estados, usando OverlapCircle
    public bool IsGrounded => CheckIfGrounded(); 

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        
        jumpsRemaining = maxJumps;
        currentMoveSpeed = moveSpeed;

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
        HandleJump();

        // Lógica de Animação (também apenas local)
        UpdateAnimations();
    }
    
    // ------------------------------------
    // --- LÓGICA DE MOVIMENTO E SALTO ---
    // ------------------------------------

    private void HandleHorizontalMovement()
    {
        // Lógica de Corrida
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentMoveSpeed = moveSpeed * sprintMultiplier;
        }
        else
        {
            currentMoveSpeed = moveSpeed;
        }
        
        float move = Input.GetAxisRaw("Horizontal");
        
        rb.linearVelocity = new Vector2(move * currentMoveSpeed, rb.linearVelocity.y);

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
        // Verifica se o Input de Salto ocorreu (Barra de Espaço OU Tecla W)
        bool jumpInput = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W);

        // AQUI NÃO HÁ RESET DE JUMPS, É SÓ A LÓGICA DE SALTO
        if (jumpInput && jumpsRemaining > 0)
        {
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
        // Este método é usado apenas para a animação "IsGrounded"
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    
    // NOVO: LÓGICA PARA REATIVAR O SALTO APENAS AO TOCAR NO CHÃO
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Verifica se o objeto com que colidiu está na groundLayer
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            // O 0.9f garante que o contacto é quase puramente vertical (chão)
            // Paredes teriam um valor de Y próximo de 0.
            if (collision.contacts.Any(contact => contact.normal.y > 0.9f))
            {
                // Reset de saltos só se for confirmado que é o chão
                jumpsRemaining = maxJumps;
            }
        }
    }
    // ------------------------------------------------------------------

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

        // Usa o IsGrounded antigo (baseado no círculo)
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
