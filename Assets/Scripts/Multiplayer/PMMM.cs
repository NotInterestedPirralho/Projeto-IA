using UnityEngine;
using Photon.Pun; // Necessário para aceder ao RoomManager/TGRoomManager/PhotonNetwork
using Photon.Realtime; // Necessário se precisar de mais informações de estado da sala

public class PMMM : MonoBehaviour
{
    // A UI do Menu de Pausa (configurada no Inspector)
    [SerializeField] private GameObject pausePanel;

    // Variável estática para fácil acesso global por outros scripts (ex: o script do jogador local)
    // Indica se o jogador local (e SÓ ele) está com o menu de pausa aberto.
    public static bool IsPausedLocally { get; private set; } = false;

    // Deve ser chamada sempre que o utilizador prime a tecla de Pausa (ex: Escape)
    void Update()
    {
        // Se a Unity não tiver foco, não queremos registar o input
        if (!Application.isFocused) return;

        // Só se deve permitir a pausa se o jogador estiver conectado e numa sala (i.e., em jogo)
        if (PhotonNetwork.InRoom && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Alterna entre o estado de Pausa e Jogo.
    /// </summary>
    public void TogglePause()
    {
        if (IsPausedLocally)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Mostra o menu de pausa, mas mantém o jogo a correr de fundo.
    /// </summary>
    public void PauseGame()
    {
        // 1. VERIFICAÇÃO DE PRÉ-PAUSA: Bloquear a pausa se estivermos numa fase de conexão/configuração.
        // Se estivermos no modo normal/multiplayer, verifica o painel de nome
        if (RoomManager.instance != null)
        {
            // Se o painel de nome ainda estiver ativo, estamos a configurar. Bloqueia a pausa.
            if (RoomManager.instance.IsNamePanelActive)
            {
                Debug.LogWarning("Não é possível pausar: O painel de nome/conexão ainda está ativo.");
                return;
            }
        }
        else if (TGRoomManager.instance != null)
        {
            // Adicionar aqui qualquer verificação de UI específica do modo de treino, se existir.
        }

        // 2. *** MUDANÇA ESSENCIAL ***
        // NÃO TOCAR EM Time.timeScale! O jogo continua a correr de fundo para os outros jogadores.
        // O jogo só "para" localmente porque o input/movimento do jogador será bloqueado.

        // 3. MOSTRA A UI DO MENU DE PAUSA
        pausePanel.SetActive(true);

        // 4. ATUALIZA O ESTADO ESTÁTICO LOCAL
        IsPausedLocally = true;

        // Opcional: Mostra o cursor do rato para interação com o menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Esconde o painel e permite o input do jogador novamente.
    /// </summary>
    public void ResumeGame()
    {
        // 1. ESCONDE A UI DO MENU DE PAUSA
        pausePanel.SetActive(false);

        // 2. ATUALIZA O ESTADO ESTÁTICO LOCAL
        IsPausedLocally = false;

        // 3. OCULTA O CURSOR (Assumindo que o seu jogo o esconde durante o jogo normal)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Opcional: Se o jogador sair da sala/nível por outro motivo, desativar a pausa
    private void OnDisable()
    {
        // Garante que o estado IsPausedLocally é resetado se este objeto for desativado
        // (i.e., quando a cena é trocada).
        IsPausedLocally = false;
        
        // Garante que o cursor é escondido se o jogo estava pausado quando a cena trocou.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
