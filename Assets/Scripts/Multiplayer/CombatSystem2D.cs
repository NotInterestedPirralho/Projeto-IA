using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using System.Collections;
using ExitGames.Client.Photon; // importante

[RequireComponent(typeof(PhotonView))]
public class CombatSystem2D : MonoBehaviourPunCallbacks
{
    [Header("Ataque")]
    public int damage = 25;
    public float attackRange = 1f;
    public float attackCooldown = 0.5f;
    public Transform attackPoint;
    public LayerMask enemyLayers;

    [Header("Defesa")]
    public float defenseDuration = 1f;
    public float defenseCooldown = 2f;
    public bool isDefending = false;

    [Header("VFX")]
    public GameObject hitVFX;

    private float nextAttackTime = 0f;
    private float nextDefenseTime = 0f;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();

        if (!photonView.IsMine)
            enabled = false;
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // ATAQUE — botão esquerdo do rato
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime && !isDefending)
        {
            nextAttackTime = Time.time + attackCooldown;
            photonView.RPC(nameof(Attack), RpcTarget.All);
        }

        // DEFESA — botão direito do rato
        if (Input.GetMouseButtonDown(1) && Time.time >= nextDefenseTime && !isDefending)
        {
            nextDefenseTime = Time.time + defenseCooldown;
            photonView.RPC(nameof(Defend), RpcTarget.All);
        }
    }

    [PunRPC]
    void Attack()
    {
        if (anim) anim.SetTrigger("Attack");

        // Cria o efeito visual do ataque
        if (hitVFX != null && attackPoint != null && photonView.IsMine)
        {
            GameObject vfx = PhotonNetwork.Instantiate(hitVFX.name, attackPoint.position, Quaternion.identity);
            StartCoroutine(DestroyVFX(vfx, 1f));
        }

        // Detecta inimigos dentro da área de ataque
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.gameObject == gameObject)
                continue;

            PhotonView targetView = enemy.GetComponent<PhotonView>();
            CombatSystem2D targetCombat = enemy.GetComponent<CombatSystem2D>();
            Health targetHealth = enemy.GetComponent<Health>();

            if (targetView != null && targetView.ViewID != photonView.ViewID && targetHealth != null)
            {
                bool enemyDefending = (targetCombat != null && targetCombat.isDefending);
                int finalDamage = enemyDefending ? damage / 4 : damage;

                // Envia dano ao inimigo
                targetView.RPC(nameof(Health.TakeDamage), RpcTarget.All, finalDamage, photonView.ViewID);

                // Pontos imediatos por dano
                if (photonView.IsMine)
                {
                    PhotonNetwork.LocalPlayer.AddScore(finalDamage);
                    if (RoomManager.instance != null)
                        RoomManager.instance.SetMashes();
                }

                Debug.Log($"{gameObject.name} acertou {enemy.name} com {finalDamage} de dano!");
            }
        }
    }

    [PunRPC]
    public void KillConfirmed()
    {
        if (!photonView.IsMine) return;

        // Ganha pontos por kill
        PhotonNetwork.LocalPlayer.AddScore(100);

        // Atualiza RoomManager
        if (RoomManager.instance != null)
        {
            RoomManager.instance.kills++;
            RoomManager.instance.SetMashes();
        }

        // Atualiza CustomProperties (Kills)
        ExitGames.Client.Photon.Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
        int currentKills = 0;
        if (hash.ContainsKey("Kills")) currentKills = (int)hash["Kills"];
        hash["Kills"] = currentKills + 1;
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

        Debug.Log($"{gameObject.name} matou um inimigo! +100 pontos e +1 kill");
    }

    private IEnumerator DestroyVFX(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (vfx != null)
        {
            PhotonView vfxView = vfx.GetComponent<PhotonView>();
            if (vfxView != null)
                PhotonNetwork.Destroy(vfx);
            else
                Destroy(vfx);
        }
    }

    [PunRPC]
    void Defend()
    {
        if (anim) anim.SetTrigger("Defend");
        StartCoroutine(DefenseCoroutine());
        Debug.Log($"{gameObject.name} está defendendo!");
    }

    private IEnumerator DefenseCoroutine()
    {
        isDefending = true;
        yield return new WaitForSeconds(defenseDuration);
        isDefending = false;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
