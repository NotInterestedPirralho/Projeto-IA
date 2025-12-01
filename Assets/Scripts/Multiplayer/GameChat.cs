using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic; // Necessário para List
using System.Linq; 

public class GameChat : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static GameChat instance;

    // --- Configuração de Limite de Histórico ---
    private const int MAX_LINES = 20; // Máximo de linhas a manter no histórico
    private List<string> chatHistory = new List<string>(); // Armazena as últimas mensagens

    // --- Propriedade de Acesso ---
    /// <summary>
    /// Informa outros scripts (Movement, Combat) se o jogador está a escrever.
    /// </summary>
    public bool IsChatOpen => isInputFiieldToggled;

    [Header("Referências")]
    [Tooltip("O componente TextMeshPro que exibe as mensagens.")]
    public TextMeshProUGUI chatText;
    [Tooltip("O componente TMP_InputField para introduzir a mensagem.")]
    public TMP_InputField InputField;

    private bool isInputFiieldToggled = false;

    // Implementação do Singleton em Awake
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // O chat deve persistir entre cenas (se for carregada outra cena de jogo).
            // NOTA: Se o seu chat for destruído ao voltar ao menu principal,
            // remova esta linha se causar problemas de re-instanciação.
            DontDestroyOnLoad(this.gameObject); 
        }
        else if (instance != this)
        {
            Destroy(gameObject); 
        }

        // Garante que o input field está desativado no início
        if (InputField != null)
        {
            InputField.DeactivateInputField();
        }
        
        // ** CRUCIAL: Desativa o Canvas do Chat INTEIRO no início **
        gameObject.SetActive(false); 
        
        // Mensagem inicial
        if (chatText != null)
        {
            chatText.text = "Chat inicializado. Pressione 'Y' para começar a escrever.";
        }
    }

    // --- NOVO MÉTODO PÚBLICO ---
    /// <summary>
    /// Ativa o Canvas do Chat. Chamado pelo LobbyManager quando o jogo começa.
    /// </summary>
    public void ActivateChatUI()
    {
        gameObject.SetActive(true);
        Debug.Log("[GameChat] Chat UI Ativado! O jogo começou.");
    }

    void Update()
    {
        // Se o Chat não estiver ativo no jogo, ignorar o input.
        if (!gameObject.activeSelf) return;

        // ----------------------------------------------------
        // BLOQUEIO 1: NO LOBBY
        // ----------------------------------------------------
        // Se o jogo ainda não tiver começado (GameStartedAndPlayerCanMove == false), 
        // bloqueia o input do chat para evitar que se abra no lobby.
        bool lobbyBlocking = (LobbyManager.instance != null && !LobbyManager.GameStartedAndPlayerCanMove);
        if (lobbyBlocking)
        {
            if (isInputFiieldToggled) CloseChatInput();
            return; 
        }

        // ----------------------------------------------------
        // BLOQUEIO 2: NO PAUSE MENU (OPCIONAL)
        // ----------------------------------------------------
        // Se tiver uma classe PMMM com lógica de pausa:
        bool isPaused = (PMMM.instance != null && PMMM.IsPausedLocally);
        if (isPaused)
        {
            // Se estivermos pausados, garantimos que o input do chat não está ativo.
            if (isInputFiieldToggled)
            {
                CloseChatInput();
            }
            // E bloqueamos qualquer input de abrir o chat.
            return; 
        }

        // ----------------------------------------------------
        // LÓGICA DE ATIVAÇÃO / DESATIVAÇÃO / ENVIO
        // ----------------------------------------------------
        
        // Ativar o Input Field com 'Y'
        if (Input.GetKeyDown(KeyCode.Y) && !isInputFiieldToggled)
        {
            OpenChatInput();
            return;
        }

        // Desativar o Input Field com 'Escape'
        if(Input.GetKeyDown(KeyCode.Escape) && isInputFiieldToggled)
        {
            CloseChatInput();
            return;
        }

        // Enviar Mensagem com 'Enter'
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && isInputFiieldToggled && !string.IsNullOrEmpty(InputField.text))
        {
            SendCurrentMessage();
        }
    }

    private void OpenChatInput()
    {
        isInputFiieldToggled = true;
        InputField.Select();
        InputField.ActivateInputField();

        Debug.Log("InputField ativado");
    }

    private void CloseChatInput()
    {
        isInputFiieldToggled = false;
        InputField.DeactivateInputField();
        
        // Remove o foco do InputField para permitir input de movimento do jogador.
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        
        Debug.Log("InputField desativado");
    }
    
    private void SendCurrentMessage()
    {
        // Se estiver conectado, usa o NickName; senão, usa "LocalPlayer" (para testes SP)
        string senderName = PhotonNetwork.IsConnected ? PhotonNetwork.LocalPlayer.NickName : "LocalPlayer";
        string messagetoSend = $"{senderName}: {InputField.text}";

        // O PhotonView deve estar anexado ao GameObject onde este script está.
        PhotonView pv = GetComponent<PhotonView>();
        
        if (pv != null && PhotonNetwork.InRoom)
        {
            // Envia a mensagem a todos os clientes
            pv.RPC("SendChatMessage", RpcTarget.All, messagetoSend);
        }
        else
        {
            // Chamada local (Single Player ou não em sala)
            SendChatMessage(messagetoSend); 
        }

        // Limpa o input e fecha
        InputField.text = "";
        CloseChatInput();
    }

    // Método RPC (ou local) chamado para distribuir a mensagem por todos os clientes
    [PunRPC]
    void SendChatMessage(string _message)
    {
        if (chatText == null) return;
        
        // 1. Adiciona a nova mensagem ao histórico
        chatHistory.Add(_message);

        // 2. Limita o histórico ao tamanho máximo (remove a mais antiga)
        if (chatHistory.Count > MAX_LINES)
        {
            chatHistory.RemoveAt(0); 
        }
        
        // 3. Reconstrói o texto de exibição com quebras de linha
        // O ScrollRect da UI garante que apenas as últimas linhas são visíveis.
        chatText.text = string.Join("\n", chatHistory);
    }
}
