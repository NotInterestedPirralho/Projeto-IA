using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement2D : MonoBehaviour
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

    [Header("Knockback")]
    public bool isKnockedBack = false; // Flag para desativar o controle

    // --- PROPRIEDADES DE LEITURA PARA SINCRONIZAÇÃO (PlayerSetup.cs) ---
    public float CurrentHorizontalSpeed => rb.linearVelocity.x;
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

        if (groundCheck == null)
            Debug.LogWarning("GroundCheck não atribuído no inspector! Adicione um objeto filho para o check.");

        // Se este NÃO for o jogador local, desativa o script para prevenir inputs.
        if (pv == null || !pv.IsMine)
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

    void Update()
    {
        // 1. VERIFICAR CHÃO SEMPRE (OverlapCircle)
        if (groundCheck != null)
        {
            grounded = Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );
        }

        // --- LÓGICA DE KNOCKBACK (CORRIGIDA) ---
        // Se estiver em Knockback, bloqueia o controlo, mas permite que a física do knockback mova o Rigidbody.
        if (isKnockedBack)
        {
            if (rb != null && grounded)
            {
                // Opcional: Trava o movimento horizontal imediatamente se estiver no chão, para evitar deslize excessivo
                // Mas, por agora, VAMOS DEIXAR A FORÇA DE IMPULSO DO KNOCKBACK MOVER O PLAYER.
                // Não anular rb.linearVelocity.x aqui!
            }

            if (anim)
            {
                anim.SetFloat("Speed", 0f); // Animação de parado/stunned
                anim.SetBool("Grounded", grounded);
            }
            return; // IGNORA O RESTO DA LÓGICA DE INPUT
        }

        // Se chegámos aqui, o controlo está ATIVO.

        // 2. LÓGICA DE RESET DE SALTO
        if (rb != null && grounded && Mathf.Abs(rb.linearVelocity.y) <= 0.1f)
        {
            jumpCount = 0;
        }

        float move = 0f;
        bool isDefending = (combatSystem != null && combatSystem.isDefending);

        // 3. LÓGICA DE MOVIMENTO E SALTO (SÓ se NÃO estiver a defender)
        if (!isDefending)
        {
            // Movimento horizontal
            move = Input.GetAxisRaw("Horizontal");
            sprinting = Input.GetKey(KeyCode.LeftShift);

            float currentSpeed = sprinting ? sprintSpeed : walkSpeed;

            // Aplica a velocidade de movimento (apenas se houver input)
            if (Mathf.Abs(move) > 0.05f)
            {
                rb.linearVelocity = new Vector2(move * currentSpeed, rb.linearVelocity.y);
            }
            else
            {
                // Se não houver input, define a velocidade horizontal para zero para parar.
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            // Salto com W (duplo salto)
            if (Input.GetKeyDown(KeyCode.W) && jumpCount < maxJumps)
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
        }
        else
        {
            // 4. A defender → Para o movimento horizontal
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            move = 0f;
        }

        // 5. Atualizar Animator
        if (anim)
        {
            anim.SetFloat("Speed", isDefending ? 0f : Mathf.Abs(move));
            anim.SetBool("Grounded", grounded);
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

    // --- Lógica de Colisão ---

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            // Apenas reseta se o ponto de contacto estiver por baixo do jogador
            if (collision.GetContact(0).normal.y > 0.5f)
            {
                grounded = true;
                jumpCount = 0;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (rb != null && rb.linearVelocity.y < 0)
            {
                // Se estiver a cair, sai do chão
                // Mantemos o valor do OverlapCircle (Update) como o principal árbitro
            }
        }
    }
}