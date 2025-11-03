// CombatSystem2D.cs
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using System.Collections;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using UnityEngine.UI;      // <- NOVO (para Image)
using TMPro;               // <- NOVO (para TextMeshPro)

[RequireComponent(typeof(PhotonView))]
public class CombatSystem2D : MonoBehaviourPunCallbacks
{
    [Header("Ataque")]
    public int damage;
    public float attackRange = 1f;
    public float attackCooldown = 0.5f;
    public Transform attackPoint;
    public LayerMask enemyLayers;

    [Header("Defesa")]
    public float defenseCooldown = 2f;
    [HideInInspector] public bool isDefending = false;

    [Header("VFX")]
    public GameObject hitVFX;

    [Header("UI Defesa")]
    public Image defenseIcon;            // Ícone do shield
    public TextMeshProUGUI defenseText; // Texto com o tempo

    private float nextAttackTime = 0f;
    private float nextDefenseTime = 0f;
    private Animator anim;


    void Awake()
    {
        // Continua igual: só é ativado no PlayerSetup para o jogador local
        enabled = false;
    }

    void Start()
    {
        anim = GetComponent<Animator>();

        // Tentativa de encontrar automaticamente os elementos de UI
        if (defenseIcon == null || defenseText == null)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                if (defenseIcon == null)
                    defenseIcon = canvas.transform.Find("DefenseIcon")?.GetComponent<Image>();

                if (defenseText == null && defenseIcon != null)
                    defenseText = defenseIcon.transform.Find("DefenseCooldownText")?.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    void Update()
    {
        // --- LÓGICA DE ATAQUE (igual ao teu) ---
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime && !isDefending)
        {
            nextAttackTime = Time.time + attackCooldown;
            // Chama o RPC (todos veem a animação, mas só o atacante calcula o dano)
            photonView.RPC(nameof(Attack), RpcTarget.All);
        }

        // --- LÓGICA DE DEFESA (igual à tua) ---

        // Quando o jogador CARREGA no botão de defesa
        if (Input.GetMouseButtonDown(1) && Time.time >= nextDefenseTime && !isDefending)
        {
            // RPC para sincronizar o estado de defesa em todos
            photonView.RPC(nameof(SetDefenseState), RpcTarget.All, true);
        }

        // Quando o jogador LARGA o botão de defesa
        if (Input.GetMouseButtonUp(1) && isDefending)
        {
            // RPC para sincronizar o fim do estado de defesa
            photonView.RPC(nameof(SetDefenseState), RpcTarget.All, false);
            nextDefenseTime = Time.time + defenseCooldown;
        }

        // --- ATUALIZAR UI DO COOLDOWN ---
        UpdateDefenseUI();
    }

    // NOVO: Atualiza ícone + número do cooldown
    private void UpdateDefenseUI()
    {
        if (defenseIcon == null && defenseText == null) return;

        float remaining = nextDefenseTime - Time.time;

        // Em cooldown (e não está a defender)
        if (remaining > 0f && !isDefending)
        {
            if (defenseIcon != null)
            {
                var c = defenseIcon.color;
                c.a = 0.5f; // mais transparente quando está em cooldown
                defenseIcon.color = c;
            }

            if (defenseText != null)
            {
                int seconds = Mathf.CeilToInt(remaining);
                defenseText.text = seconds.ToString();
            }
        }
        else
        {
            // Cooldown pronto
            if (defenseIcon != null)
            {
                var c = defenseIcon.color;
                c.a = 1f; // totalmente visível
                defenseIcon.color = c;
            }

            if (defenseText != null)
                defenseText.text = ""; // limpa o número
        }
    }

    [PunRPC]
    void Attack()
    {
        if (anim) anim.SetTrigger("Attack");

        // LÓGICA AUTORITÁRIA: Usa 'photonView.IsMine' para garantir que SÓ O ATACANTE (dono do view)
        if (photonView.IsMine)
        {
            // Instanciar VFX
            if (hitVFX != null && attackPoint != null)
            {
                GameObject vfx = PhotonNetwork.Instantiate(hitVFX.name, attackPoint.position, Quaternion.identity);
                StartCoroutine(DestroyVFX(vfx, 1f));
            }

            // Deteção e Cálculo de dano
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
            foreach (Collider2D enemy in hitEnemies)
            {
                if (enemy.gameObject == gameObject) continue;

                PhotonView targetView = enemy.GetComponent<PhotonView>();
                CombatSystem2D targetCombat = enemy.GetComponent<CombatSystem2D>();
                Health targetHealth = enemy.GetComponent<Health>();

                if (targetView != null && targetView.ViewID != photonView.ViewID && targetHealth != null)
                {
                    bool enemyDefending = (targetCombat != null && targetCombat.isDefending);
                    int finalDamage = enemyDefending ? damage / 4 : damage;

                    // Chama TakeDamage no alvo (Todos veem o dano)
                    targetView.RPC(nameof(Health.TakeDamage), RpcTarget.All, finalDamage, photonView.ViewID);

                    // Adicionar pontuação pelo dano (Continua a pontuar pelo dano)
                    PhotonNetwork.LocalPlayer.AddScore(finalDamage);

                    Debug.Log($"{gameObject.name} acertou {enemy.name} com {finalDamage} de dano!");
                }
            }
        }
    }

    [PunRPC]
    public void KillConfirmed()
    {
        // Usa 'photonView.IsMine' para garantir que só o jogador que fez a Kill
        // atualiza o score e CustomProperties.
        if (!photonView.IsMine) return;

        int currentKills = 0;
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Kills"))
            currentKills = (int)PhotonNetwork.LocalPlayer.CustomProperties["Kills"];
        currentKills++;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "Kills", currentKills } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"{gameObject.name} matou um inimigo! +1 kill (Sem pontos bónus)");
    }

    private IEnumerator DestroyVFX(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (vfx != null)
        {
            PhotonView vfxView = vfx.GetComponent<PhotonView>();
            // O atacante (IsMine) é o dono do VFX e deve destruí-lo
            if (vfxView != null && vfxView.IsMine)
                PhotonNetwork.Destroy(vfx);
            else if (vfxView == null)
                Destroy(vfx);
        }
    }

    [PunRPC]
    void SetDefenseState(bool state)
    {
        // Sincroniza o estado de defesa em todos os clientes
        isDefending = state;

        if (state)
        {
            if (anim) anim.SetBool("IsDefending", true);
            Debug.Log($"{gameObject.name} está defendendo!");
        }
        else
        {
            if (anim) anim.SetBool("IsDefending", false);
            Debug.Log($"{gameObject.name} parou de defender.");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
