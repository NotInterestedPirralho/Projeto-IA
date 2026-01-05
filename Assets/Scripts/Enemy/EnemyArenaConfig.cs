using UnityEngine;

public class EnemyArenaConfig : MonoBehaviour
{
    public static EnemyArenaConfig instance;

    [Header("Limites Horizontais (Paredes)")]
    public float minX = -15f;
    public float maxX = 15f;
    
    [Header("Limites Verticais")]
    [Tooltip("Abaixo desta altura, o inimigo morre.")]
    public float minY = -10f;
    
    [Tooltip("Altura máxima que o inimigo pode atingir (Teto invisível).")]
    public float maxY = 20f;

    [Header("Configuração de Spawn")]
    public GameObject[] enemyPrefabs;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Verifica se a posição está dentro de todos os limites (X e Y).
    /// </summary>
    public bool IsInsideArena(Vector2 position)
    {
        bool horizontalSafe = position.x >= minX && position.x <= maxX;
        bool verticalSafe = position.y >= minY && position.y <= maxY;
        
        return horizontalSafe && verticalSafe;
    }

    private void OnDrawGizmos()
    {
        // Cor para as paredes e teto (Limites de movimento)
        Gizmos.color = Color.green;
        
        // Desenha o retângulo da arena no Editor
        Vector3 topLeft = new Vector3(minX, maxY, 0);
        Vector3 topRight = new Vector3(maxX, maxY, 0);
        Vector3 bottomLeft = new Vector3(minX, minY, 0);
        Vector3 bottomRight = new Vector3(maxX, minY, 0);

        Gizmos.DrawLine(topLeft, topRight);       // Teto
        Gizmos.DrawLine(topLeft, bottomLeft);     // Parede Esquerda
        Gizmos.DrawLine(topRight, bottomRight);   // Parede Direita

        // Cor para a linha de morte (Chão falso/Buraco)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(bottomLeft, bottomRight); // Chão (Morte)
    }
}
