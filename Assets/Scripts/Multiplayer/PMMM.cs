using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Collections;

public class PMMM : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static PMMM instance;

    // --- Variável de Estado Estática ---
    public static bool IsPausedLocally = false;

    [Header("UI Reference")]
    public GameObject pausePanel;

    private bool isGameSceneLoaded = false;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);
        
        IsPausedLocally = false;
        if (pausePanel != null) pausePanel.SetActive(false);
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
        // Verifica se não estamos no Menu Principal
        isGameSceneLoaded = !scene.name.Contains("Menu"); 

        if (pausePanel != null) pausePanel.SetActive(false);
        IsPausedLocally = false;
        
        if (isGameSceneLoaded) LockCursor();
        else UnlockCursor();
    }

    void Update()
    {
        if (!isGameSceneLoaded) return;
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 1. PRIORIDADE DO CHAT
            if (GameChat.instance != null && GameChat.instance.IsChatOpen) 
            {
                return; 
            }

            // 2. PRIORIDADE DO LOBBY
            // Impede abrir o menu de pausa se o jogador ainda estiver na tela de Lobby
            bool lobbyBlocking = (LobbyManager.instance != null && !LobbyManager.GameStartedAndPlayerCanMove);
            if (lobbyBlocking)
            {
                return;
            }

            // 3. Alternar Pausa
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

    public void PauseGame()
    {
        if (IsPausedLocally || !isGameSceneLoaded) return;

        IsPausedLocally = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        
        UnlockCursor();
        Debug.Log("[PMMM] Jogo pausado. Movimento bloqueado via IsPausedLocally.");
    }

    public void ResumeGame()
    {
        if (!IsPausedLocally) return;

        IsPausedLocally = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        LockCursor();
    }

    public void LeaveGame()
    {
        IsPausedLocally = false;
        UnlockCursor();
        
        if (RoomManager.instance != null)
        {
            RoomManager.instance.LeaveGameAndGoToMenu("MenuPrincipal"); 
        }
        else
        {
            if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("MenuPrincipal"); 
        }

        Destroy(gameObject);
    }
    
    public void LockCursor()
    {
        // Ajusta conforme a necessidade do teu jogo 2D
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true; 
    }
}
