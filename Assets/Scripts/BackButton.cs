using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButtonToMenu : MonoBehaviour
{
    // O nome da cena que será carregada (o Menu Principal)
    [Tooltip("Insira o nome exato da cena do Main Menu.")]
    public string mainMenuSceneName = "MainMenu";

    /// <summary>
    /// Função chamada ao clicar no botão.
    /// Carrega a cena do Menu Principal no modo Single, descarregando as restantes.
    /// </summary>
    public void OnClickBack()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("O nome da cena 'Main Menu' não está definido!");
            return;
        }

        // --- PONTO CHAVE: LoadSceneMode.Single ---
        // Este modo garante que todas as cenas atualmente carregadas
        // (exceto objetos marcados com DontDestroyOnLoad) 
        // são descarregadas antes de carregar a nova cena.
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);

        Debug.Log("Voltando para o Menu Principal e descarregando cenas anteriores: " + mainMenuSceneName);
    }
}