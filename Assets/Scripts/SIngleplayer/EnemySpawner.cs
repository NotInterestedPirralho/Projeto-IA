using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Prefab do Inimigo")]
    [SerializeField] GameObject enemyPrefab;     // Orc (prefab)

    [Header("Onde nascer")]
    [SerializeField] Transform[] spawnPoints;    // 1 ou vários pontos
    [SerializeField] bool escolherPontoAleatorio = true;

    [Header("Respawn")]
    [SerializeField] float respawnDelay = 1.5f;  // tempo até nascer o próximo
    [SerializeField] bool manterSempre1Vivo = true;

    [SerializeField] int maxAlive = 3;
    int alive = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (manterSempre1Vivo)
        {
            // se não houver nenhum inimigo na cena ao arrancar, cria um
            if (FindObjectsOfType<Enemy>().Length == 0)
                SpawnNow();
        }
    }

    public void OnEnemyDied()
    {
        if (!manterSempre1Vivo) return;
        StartCoroutine(CoRespawn());
    }

    IEnumerator CoRespawn()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnNow();
    }

    public void SpawnNow()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] Falta prefab ou spawnPoints.");
            return;
        }

        Transform p = escolherPontoAleatorio
            ? spawnPoints[Random.Range(0, spawnPoints.Length)]
            : spawnPoints[0];

        Instantiate(enemyPrefab, p.position, Quaternion.identity);
    }
}
