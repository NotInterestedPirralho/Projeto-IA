using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using Photon.Pun.UtilityScripts;
using System.Collections; // Necessário para Corrotinas

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager instance;

    // --- Configurações de Jogo ---
    // 1 Vida Inicial + 2 Respawns = 3 Vidas Totais
    private const int MAX_RESPAWNS = 2;
    private const string RESPAWN_COUNT_KEY = "RespawnCount";
    private string sceneToLoadOnLeave = "";

    [Header("Player and Spawn")]
    public GameObject player; // (Opcional, visto que usamos Resources)
    public Transform[] spawnPoints;

    [Header("UI References")]
    public GameObject roomCam;      // Câmara do Lobby/Espectador (ARRASRA A TUA CÂMARA DE CENA AQUI)
    public GameObject nameUI;       // Menu de Nome
    public GameObject connectigUI;  // Texto "Connecting..."

    // ARRASTA O TEU HUDCANVAS PARA AQUI
    public MultiplayerEndScreen endScreen;

    [Header("Room Info")]
    public string mapName = "Noname";
    private string nickName = "Nameless";

    public bool IsNamePanelActive => nameUI != null && nameUI.activeSelf;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void ChangeNickName(string _name) { nickName = _name; }

    // --- CONEXÃO ---
    public void ConnectToMaster()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            if (nameUI != null) nameUI.SetActive(false);
            if (connectigUI != null) connectigUI.SetActive(true);
        }
        else JoinRoomLogic();
    }

    public void JoinRoomButtonPressed()
    {
        if (PhotonNetwork.IsConnectedAndReady) JoinRoomLogic();
        else ConnectToMaster();
    }

    private void JoinRoomLogic()
    {
        RoomOptions ro = new RoomOptions();
        ro.MaxPlayers = 4;

        ro.CustomRoomProperties = new Hashtable() {
            { "mapSceneIndex", SceneManager.GetActiveScene().buildIndex },
            { "mapName", mapName }
        };
        ro.CustomRoomPropertiesForLobby = new[] { "mapSceneIndex", "mapName" };

        string roomName = "Room_" + mapName;
        PhotonNetwork.JoinOrCreateRoom(roomName, ro, typedLobby: null);
    }

    // --- SAIR DO JOGO ---
    public void LeaveGameAndGoToMenu(string menuSceneName)
    {
        Time.timeScale = 1f;
        sceneToLoadOnLeave = menuSceneName;
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        else { SceneManager.LoadScene(menuSceneName); Destroy(this.gameObject); }
    }

    // --- CALLBACKS PHOTON ---
    public override void OnConnectedToMaster() { JoinRoomLogic(); }
    public override void OnDisconnected(DisconnectCause cause) { if (connectigUI != null) connectigUI.SetActive(false); if (nameUI != null) nameUI.SetActive(true); }

    public override void OnJoinedRoom()
    {
        if (connectigUI != null) connectigUI.SetActive(false);

        // 1. Reset Score e Vidas na Rede
        PhotonNetwork.LocalPlayer.SetScore(0);
        SetInitialRespawnCount(PhotonNetwork.LocalPlayer);

        // 2. Chama o Lobby para mostrar o botão START
        if (LobbyManager.instance != null)
        {
            Debug.Log("LobbyManager encontrado. A mostrar sala de espera...");
            LobbyManager.instance.OnRoomEntered();
        }
        else
        {
            Debug.Log("Sem LobbyManager. A iniciar jogo direto.");
            StartGame();
        }
    }

    public override void OnLeftRoom()
    {
        if (!string.IsNullOrEmpty(sceneToLoadOnLeave))
        {
            SceneManager.LoadScene(sceneToLoadOnLeave);
            if (instance == this) Destroy(this.gameObject);
            sceneToLoadOnLeave = "";
        }
    }

    // --- INÍCIO DE JOGO (PvP) ---
    public void StartGame()
    {
        Debug.Log("O Jogo PvP Começou!");

        // Garante que a câmara de sala está ativa antes de spawnar, para evitar flickers
        if (roomCam != null) roomCam.SetActive(true);

        // Faz o spawn do jogador (Guerreiro)
        RespawnPlayer();

        // Desliga a câmara de espera APÓS spawnar
        if (roomCam != null) roomCam.SetActive(false);
    }

    // --- SISTEMA DE MORTE E VIDAS (LOCAL) ---
    public void HandleMyDeath()
    {
        // 1. ATIVA IMEDIATAMENTE A CÂMARA DE ESPECTADOR
        // Isto impede o erro "Display 1 No cameras rendering"
        if (roomCam != null) roomCam.SetActive(true);

        int currentRespawns = GetRespawnCount(PhotonNetwork.LocalPlayer);

        // Retira uma vida
        if (currentRespawns >= 0)
        {
            currentRespawns--;
            Hashtable props = new Hashtable { { RESPAWN_COUNT_KEY, currentRespawns } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        // Verifica se ainda pode fazer respawn
        if (currentRespawns >= 0)
        {
            Debug.Log($"A preparar respawn... Vidas restantes: {currentRespawns}");
            // Inicia o temporizador de respawn (3 segundos)
            StartCoroutine(RespawnCoroutine(3.0f));
        }
        else
        {
            Debug.Log("Vidas esgotadas! GAME OVER.");
            // A câmara já está ativa, mostramos o UI de derrota
            if (endScreen != null) endScreen.ShowDefeat();
        }
    }

    // --- NOVA CORROTINA DE RESPAWN ---
    private IEnumerator RespawnCoroutine(float delay)
    {
        // Espera X segundos
        yield return new WaitForSeconds(delay);

        // Cria o novo boneco
        RespawnPlayer();

        // Desliga a câmara de espectador porque o novo boneco já tem a dele
        if (roomCam != null) roomCam.SetActive(false);
    }

    // --- VERIFICAÇÃO DE VITÓRIA (PvP - Last Man Standing) ---

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        CheckWinCondition();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(RESPAWN_COUNT_KEY))
        {
            CheckWinCondition();
        }
    }

    private void CheckWinCondition()
    {
        if (GetRespawnCount(PhotonNetwork.LocalPlayer) < 0) return;

        int activePlayers = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (GetRespawnCount(p) >= 0) activePlayers++;
        }

        bool gameStarted = false;
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("gs"))
        {
            gameStarted = (bool)PhotonNetwork.CurrentRoom.CustomProperties["gs"];
        }

        if (gameStarted && activePlayers == 1)
        {
            Debug.Log("VITÓRIA! És o único sobrevivente (Last Man Standing).");
            if (endScreen != null) endScreen.ShowVictory();
        }
    }

    // --- SPAWN DO JOGADOR ---
    public void SetInitialRespawnCount(Player player)
    {
        if (!player.CustomProperties.ContainsKey(RESPAWN_COUNT_KEY))
        {
            Hashtable props = new Hashtable { { RESPAWN_COUNT_KEY, MAX_RESPAWNS } };
            player.SetCustomProperties(props);
        }
    }

    public void RespawnPlayer()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[RoomManager] Erro: Sem pontos de spawn!");
            return;
        }

        int playerIndex = GetPlayerIndex(PhotonNetwork.LocalPlayer);
        int spawnIndex = playerIndex % spawnPoints.Length;
        Transform spawnPoint = spawnPoints[spawnIndex];

        // Vai buscar o nome do boneco guardado
        string charName = PlayerPrefs.GetString("SelectedCharacter", "Soldier");
        if (string.IsNullOrEmpty(charName) || charName == "None") charName = "Soldier";

        Debug.Log($"[RoomManager] A fazer spawn de: {charName}");

        // IMPORTANTE: O ficheiro 'charName' (ex: Soldier) TEM de estar numa pasta chamada "Resources"
        GameObject _player = PhotonNetwork.Instantiate(charName, spawnPoint.position, Quaternion.identity);

        _player.GetComponent<PlayerSetup>()?.IsLocalPlayer();

        Health h = _player.GetComponent<Health>();
        if (h != null) h.isLocalPlayer = true;

        if (_player.GetComponent<PhotonView>() != null)
        {
            _player.GetComponent<PhotonView>().RPC("SetNickname", RpcTarget.AllBuffered, nickName);
            PhotonNetwork.LocalPlayer.NickName = nickName;
        }
    }

    // --- UTILITÁRIOS ---
    private int GetRespawnCount(Player player)
    {
        if (player.CustomProperties.TryGetValue(RESPAWN_COUNT_KEY, out object count)) return (int)count;
        return MAX_RESPAWNS;
    }

    private int GetPlayerIndex(Player player)
    {
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++) { if (players[i] == player) return i; }
        return 0;
    }
}