using UnityEngine;
using UnityEngine.SceneManagement;

// Podes dar a este script o nome 'BackToMenu.cs'
public class BackToMenu : MonoBehaviour
{
    // Variavel para pores o nome da tua Scene do menu principal no Inspector
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    /// <summary>
    /// Esta e a funcao publica que vais ligar ao teu botao.
    /// </summary>
    public void GoToMainMenu()
    {
        // Boa pratica: garantir que o tempo volta ao normal 
        // (caso este botao esteja num menu de pausa, por exemplo)
        Time.timeScale = 1f;

        // Carrega a scene do menu principal
        SceneManager.LoadScene(mainMenuSceneName);
    }
}