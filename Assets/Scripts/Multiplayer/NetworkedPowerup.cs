using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class NetworkedPowerup : MonoBehaviourPun
{
    // Adicionamos o "Smart" à lista de tipos que já tinhas
    public enum PowerupType { Health, Speed, Smart }

    [Header("Tipo de Powerup")]
    public PowerupType type;

    [Header("Configuração Geral")]
    public float amount = 30f;   // Valor da cura (para modo Health)
    public float duration = 5f;  // Duração (para Speed)

    [Header("Configuração Smart (Só usado se Type for Smart)")]
    [Tooltip("Dobra o dano se a vida estiver cheia")]
    public float damageMultiplier = 2.0f;
    public float damageDuration = 10f;
    [Tooltip("Abaixo desta percentagem (0.3 = 30%), cura tudo.")]
    [Range(0f, 1f)] public float healthThreshold = 0.3f;

    private bool isCollected = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            PhotonView targetView = collision.GetComponent<PhotonView>();
            Health playerHealth = collision.GetComponent<Health>();

            // Verifica se é o jogador local
            if (targetView != null && targetView.IsMine)
            {
                isCollected = true;

                // --- 1. LÓGICA ANTIGA (MANTIDA) ---
                if (type == PowerupType.Health)
                {
                    targetView.RPC("Heal", RpcTarget.All, (int)amount);
                    Debug.Log("Powerup: Cura Fixa Aplicada.");
                }
                else if (type == PowerupType.Speed)
                {
                    // Usa o valor 'amount' como multiplicador de velocidade
                    targetView.RPC("BoostSpeed", RpcTarget.All, amount, duration); // OU "ActivateSpeedJumpBuff" se preferires o novo
                    Debug.Log("Powerup: Velocidade Fixa Aplicada.");
                }
                // --- 2. LÓGICA NOVA (SMART) ---
                else if (type == PowerupType.Smart && playerHealth != null)
                {
                    float hpPercent = (float)playerHealth.health / playerHealth.maxHealth;

                    if (hpPercent <= healthThreshold)
                    {
                        // Vida Baixa -> Cura Total (999 para encher tudo)
                        targetView.RPC("Heal", RpcTarget.All, 999);
                        Debug.Log("Smart Powerup: Cura Total (Vida Crítica!)");
                    }
                    else
                    {
                        // Vida Alta -> Dano Extra
                        targetView.RPC("BoostDamage", RpcTarget.All, damageMultiplier, damageDuration);
                        Debug.Log("Smart Powerup: Dano Extra (Modo Ataque!)");
                    }
                }

                // --- DESTRUIÇÃO (IGUAL AO QUE TINHAS) ---
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
                else
                {
                    GetComponent<Renderer>().enabled = false;
                    GetComponent<Collider2D>().enabled = false;
                    photonView.RPC("DestroyMe", RpcTarget.MasterClient);
                }
            }
        }
    }

    [PunRPC]
    public void DestroyMe()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}