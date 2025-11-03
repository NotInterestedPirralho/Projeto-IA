using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectPlayButton : MonoBehaviour
{
    [Tooltip("Nome da cena para onde vais depois de escolher o personagem")]
    public string sceneToLoad = "TrainingGround";

    public void OnClickPlay()
    {
        if (CharacterSelection.Instance == null)
        {
            Debug.LogWarning("CharacterSelection não existe, mas vou carregar a cena na mesma.");
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}
