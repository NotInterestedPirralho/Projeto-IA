using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Necessário para controlar o foco

public class GameChat : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI chatText;
    public TMP_InputField inputField;

    // NOVO: Acesso estático para outros scripts (movimento/pausa) verificarem se o chat está aberto.
    public static bool IsChatOpen = false;

    // Estado interno
    private bool isInputFieldToggled = false;
    private RoomManager roomManager;
    // NOVO: Referência ao PMMM (Menu de Pausa)
    private PMMM pauseManager;

    void Start()
    {
        // 🛑 Assumindo que PMMM é um Singleton e a instância existe 🛑
        roomManager = RoomManager.instance;
        pauseManager = PMMM.instance; 
        
        // Garante que o input começa escondido
        if (inputField != null)
        {
            inputField.gameObject.SetActive(false);
        }

        if (chatText != null) chatText.text = "";
        IsChatOpen = false; // Resetar o estado estático
    }

    void Update()
    {
        // 1. BLOQUEIOS DE SEGURANÇA (Lobby, Menu ou PAUSA)
        // Se estiver no menu de nome OU o jogo ainda não começou OU se o menu de pausa estiver aberto...
        if ((roomManager != null && roomManager.IsNamePanelActive) || 
            !LobbyManager.GameStartedAndPlayerCanMove || 
            (pauseManager != null && PMMM.IsPausedLocally)) 
        {
            if (isInputFieldToggled) ForceCloseChat();
            return;
        }

        // 2. ABRIR O CHAT (Tecla T)
        if (Input.GetKeyDown(KeyCode.T) && !isInputFieldToggled)
        {
            OpenChat();
        }

        // 3. FECHAR O CHAT (Tecla ESC)
        if (Input.GetKeyDown(KeyCode.Escape) && isInputFieldToggled)
        {
            CloseChat();
        }

        // 4. ENVIAR MENSAGEM (Tecla ENTER)
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && isInputFieldToggled)
        {
            SendMessageLogic();
        }
    }

    // Lógica separada para enviar mensagem para garantir que funciona
    void SendMessageLogic()
    {
        if (!string.IsNullOrWhiteSpace(inputField.text))
        {
            string messageToSend = $"<b>{PhotonNetwork.LocalPlayer.NickName}:</b> {inputField.text}";
            
            // Verifica se o PhotonView existe no objeto do chat (ESSENCIAL)
            PhotonView pv = GetComponent<PhotonView>();
            if (pv != null)
            {
                // Envia o RPC para todos os clientes
                pv.RPC("SendChatMessage", RpcTarget.All, messageToSend);
            }
            else
            {
                Debug.LogError("PhotonView não encontrado no objeto GameChat! A mensagem não será enviada pela rede.");
            }
        }

        // Limpa o campo e fecha o chat depois do Enter
        inputField.text = "";
        CloseChat();
    }

    void OpenChat()
    {
        if (pauseManager != null && PMMM.IsPausedLocally) return;

        isInputFieldToggled = true;
        IsChatOpen = true; // Seta o estado estático
        inputField.gameObject.SetActive(true); 
        inputField.Select();
        inputField.ActivateInputField(); // Força o cursor a aparecer

        // Liberta o cursor
        if (pauseManager != null) 
        {
            pauseManager.UnlockCursor(); 
        }
    }

    void CloseChat()
    {
        isInputFieldToggled = false;
        IsChatOpen = false; // Seta o estado estático

        // Remove o foco e desativa o input
        inputField.DeactivateInputField();
        inputField.gameObject.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
        
        // Confina o cursor novamente
        if (pauseManager != null) 
        {
            pauseManager.LockCursor();
        }
    }

    void ForceCloseChat()
    {
        inputField.text = ""; // Limpa rascunhos
        CloseChat();
    }

    [PunRPC]
    public void SendChatMessage(string _message)
    {
        // Adiciona a nova mensagem e uma quebra de linha
        chatText.text += _message + "\n";
        
        // Se precisar de auto-scroll para a última linha, adicione aqui:
        // chatText.GetComponent<ScrollRect>()?.verticalNormalizedPosition = 0f;
    }
}
