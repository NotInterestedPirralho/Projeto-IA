using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class ArenaSelectUI : MonoBehaviour
{
    [Header("Nomes das Cenas das Arenas")]
    public string arena1Scene = "TrainingGround";
    public string arena2Scene = "TrainingGround2";
    public string arena3Scene = "TrainingGround3"; // <--- NOVA VARIÁVEL
    public string arena4Scene = "TrainingGround4"; // <--- NOVA VARIÁVEL

    [Header("Arena por defeito")]
    public string defaultArena = "TrainingGround";

    // Função para Arena 1 (TG1)
    public void SelectArena1()
    {
        GameSettings.SelectedArenaScene = arena1Scene;
        Debug.Log("Arena escolhida: " + GameSettings.SelectedArenaScene);
    }

    // Função para Arena 2 (TG2)
    public void SelectArena2()
    {
        GameSettings.SelectedArenaScene = arena2Scene;
        Debug.Log("Arena escolhida: " + GameSettings.SelectedArenaScene);
    }

    // <--- NOVAS FUNÇÕES: Arena 3 (TG3)
    public void SelectArena3()
    {
        GameSettings.SelectedArenaScene = arena3Scene;
        Debug.Log("Arena escolhida: " + GameSettings.SelectedArenaScene);
    }

    // <--- NOVAS FUNÇÕES: Arena 4 (TG4)
    public void SelectArena4()
    {
        GameSettings.SelectedArenaScene = arena4Scene;
        Debug.Log("Arena escolhida: " + GameSettings.SelectedArenaScene);
    }

    public void OnPlayClicked()
    {
        if (string.IsNullOrEmpty(GameSettings.SelectedArenaScene))
            GameSettings.SelectedArenaScene = defaultArena;

        // Lógica de carregamento de cena (Multiplayer ou Local)
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel(GameSettings.SelectedArenaScene);
        }
        else
        {
            // Carregamento local (Training Ground)
            SceneManager.LoadScene(GameSettings.SelectedArenaScene);
        }
    }
}