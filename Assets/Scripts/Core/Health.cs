using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro;
using ExitGames.Client.Photon;

public class Health : MonoBehaviourPunCallbacks
{
    [Header("Vida")]
    public int maxHealth = 100;   // Vida máxima
    public int health = 100;      // Vida actual
    public bool isLocalPlayer;

    public RectTransform healthBar;
    private float originalHealthBarsize;

    [Header("Knockback")]
    public float knockbackForce;
    public float knockbackDuration;
    private Rigidbody2D rb;
    private Movement2D playerMovement;

    [Header("UI")]
    public TextMeshProUGUI healthText;

    private bool isDead = false; // Flag importante para não levar dano depois de morrer

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<Movement2D>();
    }

    private void Start()
    {
        originalHealthBarsize = healthBar.sizeDelta.x;

        // Garante que a vida inicial não passa do máximo nem fica negativa
        health = Mathf.Clamp(health, 0, maxHealth);

        UpdateHealthUI();
    }

    // ------------------- KNOCKBACK -------------------

    // Método que inicia a Coroutine de Knockback
    public void ApplyKnockback(Vector3 attackerPosition)
    {
        if (rb == null || playerMovement == null) return;

        // Calcula a direção oposta ao atacante
        Vector2 direction = (transform.position - attackerPosition).normalized;

        // Inicia a rotina de knockback
        StartCoroutine(KnockbackRoutine(direction));
    }

    // Coroutine para aplicar a força e controlar o estado do jogador
    private IEnumerator KnockbackRoutine(Vector2 direction)
    {
        // 1. Desativa o controlo do jogador
        playerMovement.SetKnockbackState(true);

        // 2. Aplica a força de impulso
        rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);

        // 3. Espera pela duração do knockback
        yield return new WaitForSeconds(knockbackDuration);

        // 4. Limpa a velocidade horizontal para evitar que o jogador deslize indefinidamente
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // 5. Reativa o controlo do jogador
        playerMovement.SetKnockbackState(false);
    }

    // ------------------- DANO -------------------

    [PunRPC]
    public void TakeDamage(int _damage, int attackerViewID = -1)
    {
        if (isDead) return; // Se já estiver morto, ignora dano adicional

        // Retira vida mas nunca abaixo de 0
        health = Mathf.Max(health - _damage, 0);

        UpdateHealthUI();

        Debug.Log($"{gameObject.name} recebeu {_damage} de dano. Vida restante: {health}");

        // --- Lógica de Knockback (SOMENTE para o Local Player) ---
        if (isLocalPlayer && attackerViewID != -1)
        {
            PhotonView attackerView = PhotonView.Find(attackerViewID);
            if (attackerView != null)
            {
                // ... (Lógica de Knockback permanece a mesma)
                CombatSystem2D attackerCombat = attackerView.GetComponent<CombatSystem2D>();
                EnemyAI attackerAI = attackerView.GetComponent<EnemyAI>();

                if (attackerCombat != null || attackerAI != null)
                {
                    ApplyKnockback(attackerView.transform.position);
                }
            }
        }
        // ---------------------------------------------------------

        if (health <= 0)
        {
            isDead = true; // Marca como morto imediatamente
            Debug.Log($"{gameObject.name} morreu!");

            if (isLocalPlayer) // Apenas o jogador que morreu processa esta lógica
            {
                // >>> ALTERAÇÃO PRINCIPAL: NOTIFICAR ROOM MANAGER <<<

                // 1. Notifica o RoomManager (o MasterClient irá decrementar o contador de respawn)
                if (RoomManager.instance != null)
                {
                    // Passamos o jogador local para o RoomManager processar a morte
                    RoomManager.instance.OnPlayerDied(PhotonNetwork.LocalPlayer);
                }

                // 2. Tenta fazer Respawn (o RoomManager verificará se o contador permite)
                if (RoomManager.instance != null)
                {
                    RoomManager.instance.RespawnPlayer();
                }

                // 3. Lógica de Kills/Deaths (mantida do seu código original)

                // Aumenta a contagem de Mortes (Deaths)
                int currentDeaths = 0;
                if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Deaths"))
                    currentDeaths = (int)PhotonNetwork.LocalPlayer.CustomProperties["Deaths"];
                currentDeaths++;

                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
        { "Deaths", currentDeaths }
        };
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);

                // Notificar o atacante (para KillConfirmed)
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

                // 4. Destroi o objeto morto (somente o dono, após processar a morte/respawn)
                PhotonView view = GetComponent<PhotonView>();
                if (view != null && view.IsMine)
                    PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    // ------------------- CURA -------------------

    [PunRPC]
    public void Heal(int amount)
    {
        if (isDead) return; // Não cura mortos

        // Soma vida mas sem ultrapassar o máximo
        health = Mathf.Clamp(health + amount, 0, maxHealth);

        UpdateHealthUI();

        Debug.Log($"{gameObject.name} foi curado em {amount}. Vida actual: {health}");
    }

    // ------------------- UI -------------------

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