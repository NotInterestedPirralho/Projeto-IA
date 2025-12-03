using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using ExitGames.Client.Photon; // Necessário para a extensão .IsNullOrEmpty()
using System.Collections;
using System;

// Adiciona isto se estiveres a usar a extensão IsNullOrEmpty() para strings da Photon.
// Se não funcionar, usa 'string.IsNullOrEmpty(InputField.text)'
// Nota: O código original usava WebSocketSharp.IsNullOrEmpty, que pode não estar disponível.
// Vamos garantir que a verificação de texto vazio é segura.

public class GameChat : MonoBehaviourPunCallbacks
{
    // 1. SINGLETON
    // Usar a instância singleton para acesso fácil de outras classes, como o Player.
    public static GameChat instance;

    [Header("Referências")]
    [Tooltip("Onde o texto do chat será exibido.")]
    public TextMeshProUGUI chatText;
    [Tooltip("O campo onde o jogador digita a mensagem.")]
    public TMP_InputField InputField;

    // 2. PROPRIEDADE PÚBLICA
    private bool isInputFiieldToggled;
    public bool IsChatOpen => isInputFiieldToggled; 
    
    // A referência ao PhotonView é necessária para enviar e receber RPCs.
    private PhotonView pv;

    void Awake()
    {
        // Garante que só há uma instância.
        if (instance != null && instance != this)
        {
            // Este caso só deve ocorrer se houver problemas de cena/DontDestroyOnLoad,
            // o que já corrigimos ao transformá-lo num objeto de cena.
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        
        // Garante que o PhotonView é capturado para uso posterior.
        pv = GetComponent<PhotonView>();

        // Desativa a entrada de texto no início
        if (InputField != null)
        {
            InputField.gameObject.SetActive(true); // O input field em si deve estar ativo, mas não selecionado
            InputField.DeactivateInputField();
        }
    }

    void Update()
    {
        // Alternar (Toggle) a abertura do Chat com a tecla Y
        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (!isInputFiieldToggled)
            {
                // Abrir Chat
                isInputFiieldToggled = true;
                InputField.Select();
                InputField.ActivateInputField();
                Debug.Log("InputField ativado");
            }
            else // Se o chat já estiver aberto, Y pode fechar (mas o Escape é prioritário)
            {
                CloseChat();
            }
        }

        // 3. PRIORIDADE ESCAPE: Fecha sempre o chat
        if(Input.GetKeyDown(KeyCode.Escape) && isInputFiieldToggled)
        {
            CloseChat(); 
            return; 
        }

        // Enviar mensagem quando Return/Enter é pressionado
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && isInputFiieldToggled)
        {
            // Usa a verificação de string padrão do C#
            if (!string.IsNullOrEmpty(InputField.text))
            {
                // Envia a mensagem via RPC para todos os clientes
                string messagetoSend = $"{PhotonNetwork.LocalPlayer.NickName}: {InputField.text}";
                
                // Usamos a referência pv capturada no Awake
                if(pv != null)
                {
                    pv.RPC("SendChatMessage", RpcTarget.All, messagetoSend);
                }

                InputField.text = ""; // Limpa o campo de entrada
                CloseChat();          // Fecha o chat após enviar
                Debug.Log("Mensagem enviada");
            }
            else
            {
                // Fecha o chat se o jogador pressionar Enter com o campo vazio
                CloseChat();
            }
        }
    }

    public void CloseChat()
    {
        isInputFiieldToggled = false;
        
        // Limpa a seleção do InputField e desativa-o
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
             UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
        InputField.DeactivateInputField();
        Debug.Log("InputField desativado");
    }

    // PunRPC: Este método será chamado na PhotonView em todos os clientes (RpcTarget.All)
    [PunRPC]
    void SendChatMessage(string _message)
    {
        // Adiciona a nova mensagem ao chatText, seguida de uma nova linha
        chatText.text = chatText.text + "\n" + _message;
    }
}
