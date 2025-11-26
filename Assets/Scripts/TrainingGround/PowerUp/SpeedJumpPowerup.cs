using UnityEngine;
using Photon.Pun;

public class SpeedJumpPowerup : MonoBehaviour
{
    [Header("Configuração do Buff")]
    public float duration = 5f; // Duração do efeito em segundos
    public float speedMultiplier = 1.5f; // 50% mais rápido
    public float jumpMultiplier = 1.3f; // 30% mais alto

    [HideInInspector] public SpeedJumpPowerupSpawner spawner;

    // NOVO: Referência ao PhotonView para destruição na rede
    private PhotonView pv; 

    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Tenta pegar o script de movimento
        // Vamos usar GetComponent<Movement2D> e depois GetComponentInParent<Movement2D>
        // para cobrir ambos os casos de colisão (objeto raiz ou filho do jogador).
        Movement2D movement = other.GetComponent<Movement2D>();
        if (movement == null)
        {
            movement = other.GetComponentInParent<Movement2D>();
        }

        if (movement == null)
            return;

        // 2. Verifica se quem apanhou é o dono do jogador
        PhotonView targetView = movement.GetComponent<PhotonView>();

        // Só ativamos se o jogador for o dono (IsMine)
        if (targetView != null && targetView.IsMine)
        {
            // CORREÇÃO CRÍTICA AQUI: Ordem dos argumentos
            // A ordem deve ser (speedMultiplier, jumpMultiplier, duration)
            movement.ActivateSpeedJumpBuff(speedMultiplier, jumpMultiplier, duration);

            // 3. Avisa o spawner que foi apanhado (Apenas se o Powerup for o MasterClient)
            // Isto depende de como o seu Spawner está configurado para o cooldown.
            if (PhotonNetwork.IsMasterClient && spawner != null)
            {
                spawner.PowerupApanhado();
            }

            // 4. Destroi o Power-Up na Rede (Apenas MasterClient)
            if (PhotonNetwork.IsMasterClient)
            {
                // Destroi o objeto para todos os jogadores via Photon
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                // Se não for o MasterClient, apenas o desativa localmente
                // e deixa o MasterClient lidar com a destruição
                gameObject.SetActive(false);
            }
        }
    }
}