using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.EventSystems;

public class GameChat : MonoBehaviourPunCallbacks
{
    // 1. SINGLETON
    public static GameChat instance;

    [Header("Referências")]
    [Tooltip("Onde o texto do chat será exibido.")]
    public TextMeshProUGUI chatText;
    [Tooltip("O campo onde o jogador digita a mensagem.")]
    public TMP_InputField InputField;

    // 2. ESTADO DO CHAT
    private bool isInputFieldToggled;
    public bool IsChatOpen => isInputFieldToggled; 
    
    private PhotonView pv;

    void Awake()
    {
        // Configuração do Singleton
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        pv = GetComponent<PhotonView>();

        // Garante que o chat começa fechado e limpo
        if (InputField != null)
        {
            InputField.text = "";
            InputField.DeactivateInputField();
        }
    }

    /// Quando o LobbyManager desativa este objeto para começar a partida,
    void OnDisable()
    {
        isInputFieldToggled = false;

        // Devolve o foco ao jogo (tira o cursor do campo de texto)
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (InputField != null)
        {
            InputField.DeactivateInputField();
        }
        
        Debug.Log("GameChat: Script desativado. Estado resetado para a partida.");
    }

    void Update()
    {
        // Atalho para abrir o chat (Tecla Y)
        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (!isInputFieldToggled)
            {
                OpenChat();
            }
            else
            {
                CloseChat();
            }
        }

        // Tecla ESC para cancelar a escrita
        if (Input.GetKeyDown(KeyCode.Escape) && isInputFieldToggled)
        {
            CloseChat(); 
            return; 
        }

        // Enviar mensagem (Tecla Enter)
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && isInputFieldToggled)
        {
            if (!string.IsNullOrEmpty(InputField.text))
            {
                string messagetoSend = $"{PhotonNetwork.LocalPlayer.NickName}: {InputField.text}";
                
                if(pv != null)
                {
                    pv.RPC("SendChatMessage", RpcTarget.All, messagetoSend);
                }

                InputField.text = ""; 
                CloseChat();
            }
            else
            {
                CloseChat();
            }
        }
    }

    public void OpenChat()
    {
        isInputFieldToggled = true;
        InputField.ActivateInputField();
        InputField.Select(); // Garante que o teclado foca no texto imediatamente
        Debug.Log("Chat Aberto");
    }

    public void CloseChat()
    {
        isInputFieldToggled = false;
        
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        InputField.DeactivateInputField();
        Debug.Log("Chat Fechado");
    }

    // RPC para sincronizar as mensagens entre todos os jogadores
    [PunRPC]
    void SendChatMessage(string _message)
    {
        if (chatText != null)
        {
            chatText.text += "\n" + _message;
        }
    }
}
