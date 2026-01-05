using UnityEngine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(PhotonView))]
public class EnemyHealth : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    [Header("Vida")]
    public int maxHealth = 50;
    private int currentHealth;
    private bool isDead = false;

    [Header("UI")]
    public Transform healthBar;
    private float originalHealthBarScaleX;

    private PhotonView photonView;
    private EnemyBase enemyBase; 
    private int mySpawnIndex = -1;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        
        // Encontra qualquer script que herde de EnemyBase (EnemyAI ou EnemyAI_BFS)
        enemyBase = GetComponent<EnemyBase>(); 

        if (enemyBase == null)
        {
            Debug.LogError("[EnemyHealth] Erro: Nenhum script de IA encontrado neste objeto!");
        }

        currentHealth = maxHealth;
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // Recupera o índice de spawn enviado pelo RoomManager na criação
        if (info.photonView.InstantiationData != null && info.photonView.InstantiationData.Length > 0)
        {
            this.mySpawnIndex = (int)info.photonView.InstantiationData[0];
        }
    }

    void Start()
    {
        if (healthBar != null)
        {
            originalHealthBarScaleX = healthBar.localScale.x;
            UpdateHealthBar();
        }
    }

    // --- NOVO: VERIFICAÇÃO DE QUEDA DA ARENA ---
    void Update()
    {
        // Apenas o Master Client tem autoridade para matar inimigos por queda
        if (PhotonNetwork.IsMasterClient && !isDead)
        {
            if (EnemyArenaConfig.instance != null)
            {
                // Se o inimigo cair abaixo do limite Y definido na Arena
                if (transform.position.y < EnemyArenaConfig.instance.minY)
                {
                    Debug.Log($"[EnemyHealth] {gameObject.name} morreu por queda (Limite: {EnemyArenaConfig.instance.minY})");
                    Die(-1); // -1 significa que não houve um atacante direto
                }
            }
        }
    }

    [PunRPC]
    public void TakeDamage(int _damage, int attackerViewID = -1)
    {
        if (isDead) return;

        currentHealth -= _damage;
        UpdateHealthBar();

        // Lógica de Knockback (Processada no Master Client)
        if (currentHealth > 0 && PhotonNetwork.IsMasterClient && enemyBase != null)
        {
            PhotonView attackerView = PhotonView.Find(attackerViewID);

            if (attackerView != null)
            {
                Vector2 knockbackDirection = (transform.position - attackerView.transform.position).normalized;

                // Chama o RPC de Knockback definido no EnemyBase
                photonView.RPC("ApplyKnockbackRPC", RpcTarget.MasterClient,
                                knockbackDirection, enemyBase.KnockbackForce, enemyBase.StunTime);
            }
        }

        if (currentHealth <= 0)
        {
            Die(attackerViewID);
        }
    }

    void Die(int attackerViewID)
    {
        if (isDead) return;
        isDead = true;

        // Desativa a IA (EnemyAI ou EnemyAI_BFS)
        if (enemyBase != null) enemyBase.enabled = false;

        // Para a física
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }

        // Se houve um atacante, avisa o sistema de combate dele que matou alguém
        if (attackerViewID != -1)
        {
            PhotonView attackerView = PhotonView.Find(attackerViewID);
            if (attackerView != null)
            {
                attackerView.RPC(nameof(CombatSystem2D.KillConfirmed), attackerView.Owner);
            }
        }

        // O Master Client inicia o processo de destruição e respawn
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DestroyAfterDelay(1.0f));
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            float healthRatio = (float)currentHealth / maxHealth;
            Vector3 newScale = healthBar.localScale;
            newScale.x = originalHealthBarScaleX * healthRatio;
            healthBar.localScale = newScale;
        }
    }

    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (PhotonNetwork.IsMasterClient)
        {
            // Avisa o RoomManager para colocar este spawn index na fila de renascimento
            if (TGRoomManager.instance != null && mySpawnIndex != -1)
            {
                TGRoomManager.instance.RequestEnemyRespawn(mySpawnIndex);
            }
            
            // Remove o objeto da rede Photon
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
