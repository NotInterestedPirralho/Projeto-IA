using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager instance;

    // Constante para o número máximo de respawns (vidas)
    private const int MAX_RESPAWNS = 3;

    // Chave para a propriedade personalizada do Photon para rastrear respawns restantes
    private const string RESPAWN_COUNT_KEY = "RespawnCount";

    public GameObject player;

    [Space]
    public Transform[] spawnPoints;

    [Space]
    public GameObject roomCam;

    [Space]
    public GameObject nameUI;
    public GameObject connectigUI;

    private string nickName = "Nameless";

    public string mapName = "Noname";

    void Awake()
    {
        // Garante que haja apenas uma instância do RoomManager
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject); // Opcional, dependendo da sua gestão de cenas
    }

    public void ChangeNickName(string _name)
    {
        nickName = _name;
    }

    public void JoinRoomButtonPressed()
    {
        Debug.Log("Connecting...");

        RoomOptions ro = new RoomOptions();

        // 1. Limita a sala a 4 jogadores
        ro.MaxPlayers = 4;

        ro.CustomRoomProperties = new Hashtable()
        {
            { "mapSceneIndex", SceneManager.GetActiveScene().buildIndex },
            { "mapName", mapName }

        };

        ro.CustomRoomPropertiesForLobby = new[]
        {
            "mapSceneIndex",
            "mapName"
        };

        // Entra ou cria a sala com as opções definidas
        PhotonNetwork.JoinOrCreateRoom(roomName: PlayerPrefs.GetString(key: "RoomNameToJoin"), ro, typedLobby: null);

        nameUI.SetActive(false);
        connectigUI.SetActive(true);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        Debug.Log("Joined room!");

        roomCam.SetActive(false);

        // 2. Define a contagem inicial de respawn para o jogador local ao entrar na sala
        SetInitialRespawnCount(PhotonNetwork.LocalPlayer);

        RespawnPlayer();
    }

    // --- LÓGICA DE RESPAWN E MORTE ---

    public void SetInitialRespawnCount(Player player)
    {
        // Define o número inicial de respawns se ainda não estiver definido
        if (!player.CustomProperties.ContainsKey(RESPAWN_COUNT_KEY))
        {
            Hashtable props = new Hashtable();
            // O jogador começa com 3 respawns
            props.Add(RESPAWN_COUNT_KEY, MAX_RESPAWNS);
            player.SetCustomProperties(props);
            Debug.Log($"Jogador {player.NickName} inicializado com {MAX_RESPAWNS} respawns.");
        }
    }

    public void RespawnPlayer()
    {
        // Obtém a contagem atual de respawns do jogador local
        int respawnsLeft = GetRespawnCount(PhotonNetwork.LocalPlayer);

        // Verifica se o jogador ainda tem respawns disponíveis
        if (respawnsLeft > 0)
        {
            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

            // Instancia o jogador na rede
            GameObject _player = PhotonNetwork.Instantiate(player.name, spawnPoint.position, Quaternion.identity);
            _player.GetComponent<PlayerSetup>().IsLocalPlayer();
            _player.GetComponent<Health>().isLocalPlayer = true;

            _player.GetComponent<PhotonView>().RPC("SetNickname", RpcTarget.AllBuffered, nickName);
            PhotonNetwork.LocalPlayer.NickName = nickName;

            Debug.Log($"Respawn realizado. Respawn(s) restante(s) antes da próxima morte: {respawnsLeft}");
        }
        else
        {
            // O jogador atingiu o limite de respawns
            Debug.Log($"Limite de respawn atingido! Jogador {PhotonNetwork.LocalPlayer.NickName} não pode mais dar respawn.");
            // Lógica para "Game Over" ou ecrã de espectador
        }
    }

    // Chamado pelo script Health.cs quando um jogador morre
    public void OnPlayerDied(Player playerWhoDied)
    {
        // APENAS o MasterClient deve manipular e sincronizar o estado da sala para evitar conflitos
        if (!PhotonNetwork.IsMasterClient) return;

        int currentRespawnCount = GetRespawnCount(playerWhoDied);

        if (currentRespawnCount > 0)
        {
            // Decrementa a contagem de respawns
            currentRespawnCount--;

            Hashtable props = new Hashtable();
            props.Add(RESPAWN_COUNT_KEY, currentRespawnCount);
            // Sincroniza a nova contagem para todos os jogadores
            playerWhoDied.SetCustomProperties(props);

            Debug.Log($"Jogador {playerWhoDied.NickName} morreu. Restam {currentRespawnCount} respawn(s).");
        }
        else
        {
            Debug.Log($"Jogador {playerWhoDied.NickName} foi eliminado do jogo.");
        }
    }

    // Função auxiliar para obter a contagem de respawns de forma segura
    private int GetRespawnCount(Player player)
    {
        if (player.CustomProperties.TryGetValue(RESPAWN_COUNT_KEY, out object count))
        {
            return (int)count;
        }
        // Se a propriedade não estiver definida, assume-se o valor máximo
        return MAX_RESPAWNS;
    }

    // --- LÓGICA DE CONEXÃO E DESCONEXÃO ---

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Debug.LogFormat("OnPlayerLeftRoom() {0}", otherPlayer.NickName);
    }
}