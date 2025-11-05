using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TGRoomManager : MonoBehaviourPunCallbacks
{
    public static TGRoomManager instance;

    [Header("Player")]
    public GameObject player;
    public Transform[] spawnPoints;

    [Header("Enemy Setup")]
    public GameObject enemyPrefab;
    public Transform[] enemySpawnPoints;
    public int enemyCount = 3;
    public float enemyRespawnDelay = 5f;

    [Space]
    public GameObject tgRoomCam;

    private List<GameObject> activeEnemies = new List<GameObject>();

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        Debug.Log("Connecting...");

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        Debug.Log("Connected to Master");

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();

        Debug.Log("Joined Training Ground");

        PhotonNetwork.JoinOrCreateRoom("TrainingGroundRoom", new Photon.Realtime.RoomOptions { MaxPlayers = 1 }, null);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        Debug.Log("Player has joined the Training Ground");

        tgRoomCam.SetActive(false);

        // Player
        RespawnPlayer();

        // Enemy
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnInitialEnemies();
        }
    }

    // --- Player Spawn ---

    public void RespawnPlayer()
    {
        Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

        GameObject _player = PhotonNetwork.Instantiate(player.name, spawnPoint.position, Quaternion.identity);
        _player.GetComponent<PlayerSetup>().IsLocalPlayer();
        _player.GetComponent<Health>().isLocalPlayer = true;
    }

    // --- Enemy Spawn ---
    private void SpawnInitialEnemies()
    {
        activeEnemies.Clear();

        for (int i = 0; i < enemyCount; i++)
        {
            Transform spawnPoint = enemySpawnPoints[i % enemySpawnPoints.Length];

            SpawnSingleEnemy(spawnPoint.position);
        }
        Debug.Log($"Master Client spawnou {enemyCount} inimigos.");
    }

    private void SpawnSingleEnemy(Vector3 position)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (enemyPrefab != null)
        {
            GameObject newEnemy = PhotonNetwork.Instantiate(enemyPrefab.name, position, Quaternion.identity);

            activeEnemies.Add(newEnemy);
        }
        else
        {
            Debug.LogError("Enemy Prefab não está atribuído no Room Manager!");
        }
    }

    public void RequestEnemyRespawn(Vector3 deathPosition)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"Inimigo foi destruído. Respawn agendado em {enemyRespawnDelay} segundos.");

        Transform spawnPoint = enemySpawnPoints[UnityEngine.Random.Range(0, enemySpawnPoints.Length)];

        StartCoroutine(EnemyRespawnRoutine(enemyRespawnDelay, spawnPoint.position));
    }

    private IEnumerator EnemyRespawnRoutine(float delay, Vector3 position)
    {
        yield return new WaitForSeconds(delay);

        if (PhotonNetwork.IsMasterClient)
        {
            SpawnSingleEnemy(position);
            Debug.Log("Inimigo respawnado.");
        }
    }
}
