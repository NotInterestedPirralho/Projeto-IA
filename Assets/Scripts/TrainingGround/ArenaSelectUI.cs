using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class ArenaSelectUI : MonoBehaviour
{
    [Header("Nomes das Cenas das Arenas")]
    public string arena1Scene = "TrainingGround";
    public string arena2Scene = "TrainingGround2";

    [Header("Arena por defeito")]
    public string defaultArena = "TrainingGround";

    public void SelectArena1()
    {
        GameSettings.SelectedArenaScene = arena1Scene;
        Debug.Log("Arena escolhida: " + GameSettings.SelectedArenaScene);
    }

    public void SelectArena2()
    {
        GameSettings.SelectedArenaScene = arena2Scene;
        Debug.Log("Arena escolhida: " + GameSettings.SelectedArenaScene);
    }

    public void OnPlayClicked()
    {
        if (string.IsNullOrEmpty(GameSettings.SelectedArenaScene))
            GameSettings.SelectedArenaScene = defaultArena;

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel(GameSettings.SelectedArenaScene);
        }
        else
        {
            SceneManager.LoadScene(GameSettings.SelectedArenaScene);
        }
    }
}
