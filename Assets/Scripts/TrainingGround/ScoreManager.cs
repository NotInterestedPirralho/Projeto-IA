using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using System.Collections;
using UnityEngine.SceneManagement; // <--- ADICIONADO: Necessário para mudar de cena

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("Configuração da Vitória")]
    public int scoreToWin = 700; // Pontos necessários para ganhar
    public GameObject winPanel;    // O painel com botões (para Arenas 1, 2, 3)

    [Header("Configuração Final (Apenas para a Arena 4)")]
    [Tooltip("Marca esta caixa APENAS se estiveres na Arena 4")]
    public bool isFinalLevel = false;

    [Tooltip("O nome da cena para onde vais quando ganhas (ex: VictoryMessage)")]
    public string victorySceneName = "VictoryMessage";

    private bool gameEnded = false;
    private bool canCheckWin = false;

    private void Awake()
    {
        if (instance == null) instance = this;

        // Garante que o jogo não começa pausado
        Time.timeScale = 1f;

        // Esconde o painel de vitória no início
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
        gameEnded = false;
        canCheckWin = false;

        // Espera 2 segundos antes de começar a verificar (para sincronizar o Photon)
        yield return new WaitForSeconds(2f);

        canCheckWin = true;
    }

    private void Update()
    {
        // Se o jogo já acabou ou ainda não podemos verificar, não faz nada
        if (!canCheckWin || gameEnded) return;

        // Verifica o Score através da Rede
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            int networkScore = PhotonNetwork.LocalPlayer.GetScore();

            // Se atingiu a pontuação, chama a Vitória
            if (networkScore >= scoreToWin)
            {
                WinGame();
            }
        }
    }

    void WinGame()
    {
        gameEnded = true; // Bloqueia para não correr isto várias vezes

        // --- CASO 1: É A ÚLTIMA ARENA? (Arena 4) ---
        if (isFinalLevel)
        {
            Debug.Log("VITÓRIA FINAL! A carregar a mensagem de parabéns...");

            // Carrega automaticamente a cena da mensagem
            SceneManager.LoadScene(victorySceneName);

            return; // IMPORTANTE: Sai da função aqui para NÃO pausar o jogo
        }

        // --- CASO 2: É UMA ARENA NORMAL? (1, 2, 3) ---
        Debug.Log("VITÓRIA NORMAL! A mostrar botões.");

        if (winPanel != null)
            winPanel.SetActive(true);

        // Congela o jogo (Inimigos e boneco param)
        Time.timeScale = 0f;
    }
}