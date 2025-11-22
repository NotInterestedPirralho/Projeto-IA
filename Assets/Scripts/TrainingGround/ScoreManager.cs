using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("Configuração da Vitória")]
    public int scoreToWin = 700; // Este valor tu mudas no Inspector de cada Arena!
    public GameObject winPanel;

    private bool gameEnded = false;
    private bool canCheckWin = false;

    private void Awake()
    {
        if (instance == null) instance = this;

        // Sempre que a cena carrega, garantimos que o jogo se mexe.
        Time.timeScale = 1f;

        // Desliga o painel à força no início
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
        gameEnded = false;
        canCheckWin = false;

        // Espera inicial para sincronizar score
        yield return new WaitForSeconds(2f);

        canCheckWin = true;
    }

    private void Update()
    {
        if (!canCheckWin || gameEnded) return;

        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            int networkScore = PhotonNetwork.LocalPlayer.GetScore();

            if (networkScore >= scoreToWin)
            {
                WinGame();
            }
        }
    }

    void WinGame()
    {
        gameEnded = true;
        Debug.Log("VITÓRIA! Jogo Pausado.");

        if (winPanel != null)
            winPanel.SetActive(true);

        // --- CORREÇÃO 2: CONGELAR O TEMPO ---
        // Isto faz com que inimigos, física e animações parem.
        // O UI (botões) continua a funcionar.
        Time.timeScale = 0f;
    }
}