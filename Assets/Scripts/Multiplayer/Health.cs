using UnityEngine;
using Photon.Pun;
using TMPro;
using ExitGames.Client.Photon;

public class Health : MonoBehaviourPunCallbacks
{
    [Header("Vida")]
    public int health = 100;
    public bool isLocalPlayer;

    [Header("UI")]
    public TextMeshProUGUI healthText;

    void Start()
    {
        if (healthText != null)
            healthText.text = health.ToString();
    }

    [PunRPC]
    public void TakeDamage(int _damage, int attackerViewID = -1)
    {
        // Aplica o dano
        health -= _damage;

        if (healthText != null)
            healthText.text = health.ToString();

        Debug.Log($"{gameObject.name} recebeu {_damage} de dano. Vida restante: {health}");

        // Verifica se morreu
        if (health <= 0)
        {
            Debug.Log($"{gameObject.name} morreu!");

            // Atualiza apenas no dono local do jogador morto
            if (photonView.IsMine)
            {
                // Respawn e contagem de mortes locais
                if (RoomManager.instance != null)
                {
                    RoomManager.instance.RespawnPlayer();
                    RoomManager.instance.deaths++;
                    RoomManager.instance.SetMashes();
                }

                // Atualiza estatísticas globais (Deaths) no Photon
                ExitGames.Client.Photon.Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
                int currentDeaths = 0;
                if (hash.ContainsKey("Deaths")) currentDeaths = (int)hash["Deaths"];
                hash["Deaths"] = currentDeaths + 1;
                PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

                // Só o dono do objeto morto envia o RPC de kill ao atacante
                if (attackerViewID != -1)
                {
                    PhotonView attackerView = PhotonView.Find(attackerViewID);
                    if (attackerView != null)
                    {
                        attackerView.RPC(nameof(CombatSystem2D.KillConfirmed), attackerView.Owner);
                        Debug.Log($"Kill confirmada para {attackerView.Owner.NickName}");
                    }
                }

                // Destroi o objeto morto (somente o dono o faz)
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}
