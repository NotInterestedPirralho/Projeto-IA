using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Health : MonoBehaviourPunCallbacks
{
    [Header("Vida")]
    public int maxHealth = 100;    // Vida m�xima
    public int health = 100;       // Vida actual
    public bool isLocalPlayer;

    public RectTransform healthBar;
    private float originalHealthBarsize;

    [Header("Knockback")]
    // Estes valores s� ser�o usados se o atacante n�o fornecer a for�a (Fallback).
    public float knockbackForceFallback = 10f;
    public float knockbackDurationFallback = 0.3f;

    private Rigidbody2D rb;
    private Movement2D playerMovement;
    private bool isKnockedBack = false; // Flag para evitar knockback sobreposto

    [Header("UI")]
    public TextMeshProUGUI healthText;

    private bool isDead = false;
    private PhotonView view;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<Movement2D>();
        view = GetComponent<PhotonView>();
    }

    private void Start()
    {
        originalHealthBarsize = healthBar.sizeDelta.x;
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthUI();
    }

    // --- 1. L�GICA DE KNOCKBACK ---

    /// <summary>
    /// Aplica a repuls�o ao jogador, utilizando a for�a e dura��o fornecidas pelo atacante.
    /// </summary>
    public void ApplyKnockback(Vector3 attackerPosition, float force, float duration)
    {
        // Se j� estiver em knockback ou morto, ignora.
        if (rb == null || playerMovement == null || isDead || isKnockedBack) return;

        // 1. Calcula a dire��o
        Vector2 direction = (transform.position - attackerPosition).normalized;

        // 2. Garante um Y m�nimo (0.2f) para descolar do ch�o (evita o "ficar no s�tio" devido � fric��o).
        // Isto � o que foi corrigido anteriormente para evitar o problema do "saltar".
        if (direction.y < 0.2f) direction.y = 0.2f;
        direction = direction.normalized;

        // 3. Inicia a corrotina com os valores do atacante
        StartCoroutine(KnockbackRoutine(direction, force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force, float duration)
    {
        isKnockedBack = true;
        playerMovement.SetKnockbackState(true); // Bloqueia o controlo no Movement2D

        // Zera a velocidade atual para garantir que o impulso � aplicado corretamente
        rb.linearVelocity = Vector2.zero;

        // Aplica a for�a de IMPULSO (isto faz o player voar para tr�s)
        rb.AddForce(direction * force, ForceMode2D.Impulse);

        // Espera pela dura��o do knockback
        yield return new WaitForSeconds(duration);

        // Termina o knockback. O Movement2D retoma o controlo.
        playerMovement.SetKnockbackState(false);
        isKnockedBack = false;

        // Opcional: Se quiser parar imediatamente o movimento horizontal ap�s o tempo
        // rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // --- 2. L�GICA DE DANO E SINCRONIZA��O DE REDE ---

    /// <summary>
    /// Recebe dano e, se for o jogador local, aplica knockback.
    /// </summary>
    [PunRPC]
    // O RPC agora aceita a For�a e a Dura��o do Knockback do atacante.
    public void TakeDamage(int _damage, int attackerViewID = -1, float attackerKnockbackForce = 0f, float attackerKnockbackDuration = 0f)
    {
        if (isDead) return;

        health = Mathf.Max(health - _damage, 0);

        UpdateHealthUI();

        Debug.Log($"{gameObject.name} recebeu {_damage} de dano. Vida restante: {health}");

        // --- L�gica de Knockback (apenas para o jogador local) ---
        if (view.IsMine && attackerViewID != -1)
        {
            PhotonView attackerView = PhotonView.Find(attackerViewID);
            if (attackerView != null)
            {
                // Decide qual for�a e dura��o usar (do atacante ou fallback)
                float finalForce = (attackerKnockbackForce > 0) ? attackerKnockbackForce : knockbackForceFallback;
                float finalDuration = (attackerKnockbackDuration > 0) ? attackerKnockbackDuration : knockbackDurationFallback;

                // Aplica o knockback
                ApplyKnockback(
                    attackerView.transform.position,
                    finalForce,
                    finalDuration
                );
            }
        }
        // ---------------------------------------------------------

        if (health <= 0)
        {
            isDead = true;
            Debug.Log($"{gameObject.name} morreu!");

            // L�gica S� DEVE SER EXECUTADA PELO DONO DO OBJETO MORTO
            if (view.IsMine)
            {
                // 1. Notifica o RoomManager sobre a morte
                if (RoomManager.instance != null)
                {
                    RoomManager.instance.OnPlayerDied(view.Owner);
                }

                // 2. Tenta fazer Respawn
                if (RoomManager.instance != null)
                {
                    RoomManager.instance.RespawnPlayer();
                }

                // 3. Atualiza a contagem de Mortes (Deaths)
                int currentDeaths = 0;
                if (view.Owner.CustomProperties.ContainsKey("Deaths"))
                    currentDeaths = (int)view.Owner.CustomProperties["Deaths"];
                currentDeaths++;

                Hashtable props = new Hashtable
                {
                    { "Deaths", currentDeaths }
                };
                view.Owner.SetCustomProperties(props);

                // 4. Notificar o atacante (para KillConfirmed)
                if (attackerViewID != -1)
                {
                    PhotonView attackerView = PhotonView.Find(attackerViewID);
                    if (attackerView != null)
                    {
                        CombatSystem2D attackerCombat = attackerView.GetComponent<CombatSystem2D>();
                        if (attackerCombat != null)
                            attackerView.RPC(nameof(CombatSystem2D.KillConfirmed), attackerView.Owner);
                    }
                }

                // 5. Destr�i o objeto na rede (apenas o dono deve faz�-lo)
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    // --- 3. L�GICA DE CURA E UI ---

    [PunRPC]
    public void Heal(int amount)
    {
        if (isDead) return;

        health = Mathf.Clamp(health + amount, 0, maxHealth);

        UpdateHealthUI();

        Debug.Log($"{gameObject.name} foi curado em {amount}. Vida actual: {health}");
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.sizeDelta = new Vector2(
                originalHealthBarsize * health / (float)maxHealth,
                healthBar.sizeDelta.y
            );
        }

        if (healthText != null)
        {
            healthText.text = health.ToString();
        }
    }
}