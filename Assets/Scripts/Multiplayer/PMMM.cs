using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class PMMM : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static PMMM instance;

    // --- Variável de Estado Estática (A chave para a sincronização local) ---
    public static bool IsPausedLocally = false;

    [Header("UI Reference")]
    [Tooltip("O painel da UI que contém todos os botões e texto do Menu de Pausa.")]
    public GameObject pausePanel;

    private bool isGameSceneLoaded = false;

    void Awake()
    {
        // Implementação do Singleton
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        // Permite que o Manager persista entre cenas
        DontDestroyOnLoad(this.gameObject);

        // Define o estado inicial como "não pausado"
        IsPausedLocally = false;
        
        // Garante que o painel de UI começa desativado
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Define que a pausa só pode ser ativada nas cenas de jogo
        isGameSceneLoaded = !scene.name.Contains("Menu"); 

        // Se carregarmos uma cena nova, garante que o painel está fechado e o estado redefinido
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        IsPausedLocally = false;
        
        // Garante que o cursor está no estado correto para o novo ambiente
        if (!isGameSceneLoaded)
        {
            // Se for menu/lobby, liberta o cursor (visível e livre)
            UnlockCursor();
        }
        else
        {
            // Se for jogo, confina o cursor (visível e confinado, para jogo 2D)
            LockCursor(); 
        }
    }

    void Update()
    {
        // Apenas processa o input de pausa se estivermos numa cena de jogo E numa sala
        if (!isGameSceneLoaded || !PhotonNetwork.InRoom) return;
        
        // Verifica o input da tecla ESCAPE
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // O GameChat verifica o ESCAPE primeiro, mas se o chat estiver fechado,
            // esta lógica é executada para pausar/retomar o jogo.
            if (IsPausedLocally)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // ------------------------------------
    // --- FUNÇÕES DE PAUSA E RETOMADA ---
    // ------------------------------------

    /// <summary>
    /// Pausa o jogo localmente, abrindo o menu e libertando o cursor.
    /// </summary>
    public void PauseGame()
    {
        if (IsPausedLocally || !isGameSceneLoaded) return;

        IsPausedLocally = true;

        // 1. Ativa a UI do Menu de Pausa
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // 2. Liberta o cursor para que o jogador possa interagir com a UI
        UnlockCursor();

        Debug.Log("Jogo pausado localmente. Inputs bloqueados.");
    }

    /// <summary>
    /// Retoma o jogo localmente, fechando o menu e confinando o cursor.
    /// </summary>
    public void ResumeGame()
    {
        if (!IsPausedLocally) return;

        IsPausedLocally = false;

        // 1. Desativa a UI do Menu de Pausa
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // 2. Confina o cursor novamente para retomar o gameplay
        LockCursor();

        Debug.Log("Jogo retomado. Inputs reativados.");
    }

    // ------------------------------------
    // --- FUNÇÃO DE SAÍDA DO JOGO ---
    // ------------------------------------
    
    /// <summary>
    /// Sai da sala Photon e volta ao menu principal.
    /// </summary>
    public void LeaveGame()
    {
        if (RoomManager.instance != null)
        {
            // Assumimos que a cena do menu principal se chama "MenuPrincipal"
            RoomManager.instance.LeaveGameAndGoToMenu("MenuPrincipal");
        }
        else
        {
            Debug.LogError("RoomManager não encontrado! Não é possível sair da sala.");
        }

        // Garante que o estado de pausa é redefinido e o cursor libertado
        IsPausedLocally = false;
        UnlockCursor();
    }
    
    // ------------------------------------
    // --- FUNÇÕES DE CONTROLO DO CURSOR ---
    // ------------------------------------

    /// <summary>
    /// Confina o cursor à janela do jogo (Visível e Confined).
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Confined; 
        Cursor.visible = true; // Mantém o cursor visível
    }

    /// <summary>
    /// Liberta o cursor para interagir com a UI (Visível e None).
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true; // Mantém o cursor visível
    }
}
