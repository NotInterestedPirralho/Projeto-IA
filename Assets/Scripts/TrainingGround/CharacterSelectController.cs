using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class CharacterSelectController : MonoBehaviour
{
    public string arenaSelectSceneName = "ArenaSelect"; // nome da cena da Arena Select

    public void OnPlayButtonClicked()
    {
        // Se estiveres ligado ao Photon dentro de uma sala
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            // só o MasterClient muda de cena, os outros seguem (AutomaticallySyncScene tem de estar ON)
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(arenaSelectSceneName);
            }
        }
        else
        {
            // modo offline / singleplayer
            SceneManager.LoadScene(arenaSelectSceneName);
        }
    }
}
