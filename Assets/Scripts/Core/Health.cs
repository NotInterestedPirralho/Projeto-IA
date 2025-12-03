using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;

public class Health : MonoBehaviourPunCallbacks
{
    [Header("Vida")]
    public int maxHealth = 100;
    public int health = 100;
    public bool isLocalPlayer;

    [Header("UI")]
    public RectTransform healthBar;
    private float originalHealthBarsize;
    public TextMeshProUGUI healthText;

    [Header("Knockback")]
    public float knockbackForceFallback = 10f;
    public float knockbackDurationFallback = 0.3f;

    private Rigidbody2D rb;
    private Movement2D playerMovement;
    private bool isKnockedBack = false;
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
        if (healthBar != null) originalHealthBarsize = healthBar.sizeDelta.x;
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthUI();
    }

    // --- DANO (RPCs) ---
    [PunRPC]
    public void TakeDamage(int _damage, int attackerViewID) { TakeDamageComplete(_damage, attackerViewID, 0f, 0f); }

    [PunRPC]
    public void TakeDamageComplete(int _damage, int attackerViewID, float attackerKnockbackForce, float attackerKnockbackDuration)
    {
        if (isDead) return;
        health = Mathf.Max(health - _damage, 0);
        UpdateHealthUI();

        // Knockback (apenas local)
        if (view.IsMine && attackerViewID != -1)
        {
            PhotonView attackerView = PhotonView.Find(attackerViewID);
            if (attackerView != null)
            {
                float force = (attackerKnockbackForce > 0) ? attackerKnockbackForce : knockbackForceFallback;
                float duration = (attackerKnockbackDuration > 0) ? attackerKnockbackDuration : knockbackDurationFallback;
                ApplyKnockback(attackerView.transform.position, force, duration);
            }
        }

        if (health <= 0)
        {
            isDead = true;
            HandleDeath(attackerViewID);
        }
    }

    // --- KNOCKBACK ---
    public void ApplyKnockback(Vector3 attackerPosition, float force, float duration)
    {
        if (rb == null || playerMovement == null || isDead || isKnockedBack) return;
        Vector2 direction = (transform.position - attackerPosition).normalized;
        if (direction.y < 0.2f) direction.y = 0.2f;
        StartCoroutine(KnockbackRoutine(direction.normalized, force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force, float duration)
    {
        isKnockedBack = true;
        if (playerMovement != null) playerMovement.SetKnockbackState(true);
        rb.linearVelocity = Vector2.zero; // Nota: Se der erro aqui em versões antigas do Unity, usa rb.velocity
        rb.AddForce(direction * force, ForceMode2D.Impulse);
        yield return new WaitForSeconds(duration);
        if (playerMovement != null) playerMovement.SetKnockbackState(false);
        isKnockedBack = false;
    }

    // --- MORTE (LÓGICA HÍBRIDA SEGURA) ---
    private void HandleDeath(int attackerViewID)
    {
        Debug.Log($"{gameObject.name} morreu!");

        // Só o dono trata da lógica
        if (!view.IsMine) return;

        // --- 1. TENTA ENCONTRAR O ROOM MANAGER (Prioridade: Multiplayer PvP) ---
        RoomManager roomManager = RoomManager.instance;
        if (roomManager == null) roomManager = FindObjectOfType<RoomManager>();

        if (roomManager != null)
        {
            Debug.Log("RoomManager encontrado: A executar morte Multiplayer.");

            // Avisa o RoomManager para ligar a câmara de espectador e iniciar Respawn
            roomManager.HandleMyDeath();

            // Atualiza Stats
            UpdateDeathStats(attackerViewID);

            // Destrói usando Photon (Multiplayer sempre usa PhotonNetwork.Destroy)
            PhotonNetwork.Destroy(gameObject);
            return; // Sai da função para não executar código de baixo
        }

        // --- 2. TENTA ENCONTRAR O GAME MANAGER (Fallback: Single Player / Offline) ---
        var spGameManager = GameObject.Find("GameManager");

        if (spGameManager != null)
        {
            Debug.Log("GameManager encontrado: A executar morte Single Player.");

            // Lógica de UI do Single Player
            DeathMenu dm = FindObjectOfType<DeathMenu>();
            if (dm != null)
            {
                dm.Show();
            }
            else
            {
                Debug.LogWarning("DeathMenu não encontrado na cena Single Player!");
            }

            // Destruição segura (Funciona mesmo se o SP usar Photon Offline Mode)
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            return;
        }

        // Fallback final de segurança
        Debug.LogWarning("Nenhum Manager encontrado! A destruir objeto.");
        if (PhotonNetwork.IsConnected) PhotonNetwork.Destroy(gameObject);
        else Destroy(gameObject);
    }

    private void UpdateDeathStats(int attackerViewID)
    {
        // Minhas Mortes
        int currentDeaths = 0;
        if (view.Owner.CustomProperties.ContainsKey("Deaths"))
            currentDeaths = (int)view.Owner.CustomProperties["Deaths"];
        Hashtable props = new Hashtable { { "Deaths", currentDeaths + 1 } };
        view.Owner.SetCustomProperties(props);

        // Kills do Atacante
        if (attackerViewID != -1)
        {
            PhotonView attackerView = PhotonView.Find(attackerViewID);
            if (attackerView != null) attackerView.RPC("KillConfirmed", attackerView.Owner);
        }
    }

    // --- CURA E UI ---
    [PunRPC]
    public void Heal(int amount) { if (!isDead) { health = Mathf.Clamp(health + amount, 0, maxHealth); UpdateHealthUI(); } }

    private void UpdateHealthUI()
    {
        if (healthBar != null && originalHealthBarsize > 0)
            healthBar.sizeDelta = new Vector2(originalHealthBarsize * health / (float)maxHealth, healthBar.sizeDelta.y);
        if (healthText != null) healthText.text = health.ToString();
    }
}