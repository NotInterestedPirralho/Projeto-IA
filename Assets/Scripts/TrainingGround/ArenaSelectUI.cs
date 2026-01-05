using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class ArenaSelectUI : MonoBehaviour
{
    [Header("Nomes das Cenas das Arenas")]
    public string arena1Scene = "TrainingGround";
    public string arena2Scene = "TrainingGround2";
    public string arena3Scene = "TrainingGround3";
    public string arena4Scene = "TrainingGround4";

    [Header("Arena por defeito")]
    public string defaultArena = "TrainingGround";

    [Header("Feedback Visual")]
    public UISelectionHandler[] arenaButtons; // Element 0=Arena1, 1=Arena2...

    private void UpdateArenaSelection(string sceneName)
    {
        GameSettings.SelectedArenaScene = sceneName;
        Debug.Log("Arena escolhida: " + GameSettings.SelectedArenaScene);

        AudioManager.PlayClick();

        if (arenaButtons == null || arenaButtons.Length == 0) return;

        for (int i = 0; i < arenaButtons.Length; i++)
        {
            if (arenaButtons[i] == null) continue;

            bool isThisOne = false;

            // Comparação por lógica de índice para evitar erros de nomes de objetos
            if (i == 0 && sceneName == arena1Scene) isThisOne = true;
            else if (i == 1 && sceneName == arena2Scene) isThisOne = true;
            else if (i == 2 && sceneName == arena3Scene) isThisOne = true;
            else if (i == 3 && sceneName == arena4Scene) isThisOne = true;

            arenaButtons[i].SetSelected(isThisOne);
        }
    }

    public void SelectArena1() => UpdateArenaSelection(arena1Scene);
    public void SelectArena2() => UpdateArenaSelection(arena2Scene);
    public void SelectArena3() => UpdateArenaSelection(arena3Scene);
    public void SelectArena4() => UpdateArenaSelection(arena4Scene);

    public void OnPlayClicked()
    {
        AudioManager.PlayClick();
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