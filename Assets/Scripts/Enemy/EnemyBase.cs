using UnityEngine;
using Photon.Pun;

// Esta classe herda de MonoBehaviourPunCallbacks, tal como os teus inimigos faziam
public abstract class EnemyBase : MonoBehaviourPunCallbacks
{
    // --- PROPRIEDADES ABSTRATAS (Obrigatórias nos filhos) ---
    public abstract float KnockbackForce { get; }
    public abstract float StunTime { get; }

    // --- MÉTODOS RPC (Obrigatórios nos filhos) ---
    [PunRPC]
    public abstract void ApplyKnockbackRPC(Vector2 direction, float force, float time);

    // --- LÓGICA COMPARTILHADA: LIMITES DA ARENA ---
    /// <summary>
    /// Força o inimigo a permanecer dentro dos limites horizontais (X) da arena.
    /// Deve ser chamado no Update() dos scripts que herdam desta classe.
    /// </summary>
    protected void ApplyArenaLimits()
    {
        if (EnemyArenaConfig.instance == null) return;

        Vector3 pos = transform.position;
        
        // Limita o X (Esquerda e Direita)
        float clampedX = Mathf.Clamp(pos.x, EnemyArenaConfig.instance.minX, EnemyArenaConfig.instance.maxX);
        
        // NOVO: Limita o Y (Apenas o topo/maxY)
        // Não usamos Clamp no Y porque o minY é para MORRER (EnemyHealth), não para bloquear.
        float clampedY = Mathf.Min(pos.y, EnemyArenaConfig.instance.maxY);

        if (pos.x != clampedX || pos.y != clampedY)
        {
            transform.position = new Vector3(clampedX, clampedY, pos.z);
            
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Se bater no teto (maxY), zera a velocidade vertical para ele cair imediatamente
                if (pos.y != clampedY && rb.linearVelocity.y > 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                }
            }
        }
    }
}
