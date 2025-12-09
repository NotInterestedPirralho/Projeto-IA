using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomList : MonoBehaviourPunCallbacks
{
    public static RoomList Instance;

    [Header("UI (Listagem)")]
    public Transform roomListParent;
    public GameObject roomListItemPrefab;

    [Header("UI (Paineis)")]
    public GameObject lobbyPanel; 
    public GameObject createRoomPanel; 

    // --- NOVAS REFERENCIAS ---
    [Header("UI (Criacao de Sala)")]
    public TMP_InputField roomNameInputField;
    public Button[] createRoomButtons;
    // -------------------------

    private List<RoomInfo> cachedRoomList = new List<RoomInfo>();
    private string cachedRoomNameToCreate = "";

    public void ChangeRoomToCreateName(string _roomName)
    {
        // 1. LER DIRETAMENTE DO INPUT FIELD (CORREÇÃO)
        if (roomNameInputField != null)
        {
            cachedRoomNameToCreate = roomNameInputField.text.Trim();
        }
        else
        {
            // Se a referência faltar, tratamos o nome como vazio
            cachedRoomNameToCreate = "";
        }
        
        // 2. DEBUG: Mostra o nome capturado e o seu comprimento
        Debug.Log("--- INPUT EVENT (ChangeRoomToCreateName) ---");
        Debug.Log("Nome Capturado: [" + cachedRoomNameToCreate + "]. Comprimento: " + cachedRoomNameToCreate.Length);
        
        // 3. Verifica se o nome é válido e ativa/desativa os botões
        UpdateCreateRoomButtonsState();
    }
    
    public void OnRoomNameEndEdit(string _roomName)
    {
        // Garante que o nome armazenado esta atualizado, lendo diretamente do campo
        if (roomNameInputField != null)
        {
            cachedRoomNameToCreate = roomNameInputField.text.Trim();
        }
        else
        {
            cachedRoomNameToCreate = _roomName.Trim(); // Fallback
        }
        
        UpdateCreateRoomButtonsState(); 

        bool isValidName = !string.IsNullOrEmpty(cachedRoomNameToCreate);

        // Acao opcional: Se o nome for valido, simula um clique no primeiro botao (Arena 1)
        if (isValidName && createRoomButtons != null && createRoomButtons.Length > 0)
        {
            Debug.Log("ENTER PRESSIONADO. Nome válido. Tentando criar sala na Arena 1 (Índice 1).");
            CreateRoomByIndex(1); 
        }
    }

    private void UpdateCreateRoomButtonsState()
    {
        // Verifica se o nome tem conteudo (comprimento > 0)
        bool isValidName = !string.IsNullOrEmpty(cachedRoomNameToCreate);

        // DEBUG: Mostra o resultado da validação e as referências
        Debug.Log("--- UPDATE BUTTONS STATE ---");
        Debug.Log("Resultado da Validação: isValidName = " + isValidName);
        Debug.Log("Botões referenciados no Inspector: " + (createRoomButtons != null ? createRoomButtons.Length.ToString() : "0"));

        if (createRoomButtons == null || createRoomButtons.Length == 0)
        {
            Debug.LogError("ERRO: O array 'createRoomButtons' está vazio ou nulo! Arraste os 4 botões para o Inspector.");
            return;
        }

        // Ativa ou desativa cada botão de criacao de sala
        foreach (Button button in createRoomButtons)
        {
            if (button != null)
            {
                // Os botoes so sao clicaveis se o nome for valido
                button.interactable = isValidName;
            }
            else
            {
                Debug.LogWarning("AVISO: Um dos botões no array 'createRoomButtons' é nulo. Verifique as referências.");
            }
        }
    }

    public void CreateRoomByIndex(int sceneIndex)
    {
        // Esta função está perfeita para os teus botões "Create Room in Arena 1/2"
        JoinRoomByName(cachedRoomNameToCreate, sceneIndex);
    }

    private void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        // Boa prática: garantir que o painel de lobby está visível
        // e o de criar sala está escondido ao iniciar.
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (createRoomPanel != null) createRoomPanel.SetActive(false);

        // Desativa os botões ao iniciar, pois o nome da sala está vazio.
        UpdateCreateRoomButtonsState();

        // Precautions
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.Disconnect();
        }

        yield return new WaitUntil(() => !PhotonNetwork.IsConnected);

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        Debug.Log("Connected to Master Server");
        PhotonNetwork.JoinLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        if (cachedRoomList.Count <= 0)
        {
            cachedRoomList = roomList;
        }
        else
        {
            foreach (var room in roomList)
            {
                for (int i = 0; i < cachedRoomList.Count; i++)
                {
                    if (cachedRoomList[i].Name == room.Name)
                    {
                        List<RoomInfo> newList = cachedRoomList;

                        if (room.RemovedFromList)
                        {
                            newList.Remove(newList[i]);
                        }
                        else
                        {
                            newList[i] = room;
                        }

                        cachedRoomList = newList;
                    }
                }
            }
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        foreach (Transform roomItem in roomListParent)
        {
            Destroy(roomItem.gameObject);
        }

        foreach (var room in cachedRoomList)
        {
            GameObject roomItem = Instantiate(roomListItemPrefab, roomListParent);

            string roomMapName = "Unknown";

            object mapNameObject;
            if (room.CustomProperties.TryGetValue("mapName", out mapNameObject))
            {
                roomMapName = (string)mapNameObject;
            }

            roomItem.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = room.Name + "(" + roomMapName + ")";
            roomItem.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = room.PlayerCount + " /4";

            roomItem.GetComponent<RoomItemButton>().RoomName = room.Name;

            int roomSceneIndex = 1;

            object sceneIndexObject;
            if (room.CustomProperties.TryGetValue("mapSceneIndex", out sceneIndexObject))
            {
                roomSceneIndex = (int)sceneIndexObject;
            }

            roomItem.GetComponent<RoomItemButton>().SceneIndex = roomSceneIndex;
        }
    }

    public void JoinRoomByName(string _name, int _sceneIndex)
    {
        PlayerPrefs.SetString("RoomNameToJoin", _name);

        // Esta linha pode dar problemas se o script estiver no mesmo objeto
        // que os painéis. Se o menu desaparecer, remove a linha abaixo.
        // gameObject.SetActive(false); 

        SceneManager.LoadScene(_sceneIndex);
        // Load the relavant room 
    }


    // Esta função é para o "Back" do Lobby -> Menu Principal
    public void GoBackToMainMenu()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        SceneManager.LoadScene("MainMenu"); // Continua correta
    }
    
    // FUNÇÕES DE CONTROLE DE PAINEL

    public void ShowCreateRoomPanel()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (createRoomPanel != null) createRoomPanel.SetActive(true);

        // Garante que o estado dos botões é verificado quando o painel é mostrado.
        UpdateCreateRoomButtonsState();
    }

    public void GoBackToLobbyPanel()
    {
        if (createRoomPanel != null) createRoomPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
    }
}
