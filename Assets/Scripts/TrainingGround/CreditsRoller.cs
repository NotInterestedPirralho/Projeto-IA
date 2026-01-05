using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Necessário para Corrotinas

public class CreditsRoller : MonoBehaviour
{
    [Header("Objetos UI")]
    [Tooltip("Arrasta aqui o Texto da Mensagem de Vitória")]
    public GameObject victoryMessageObject;

    [Tooltip("Arrasta aqui o Texto dos Créditos (o gigante)")]
    public GameObject creditsObject;

    [Header("Configuração")]
    public float messageDuration = 4f; 
    public float scrollSpeed = 50f;    
    public string mainMenuSceneName = "MainMenu";

    [Header("Limite")]
    public float endYPosition = 1500f; 
    private RectTransform creditsRect;
    private bool isScrolling = false;

    void Start()
    {
        if (creditsObject != null)
        {
            creditsRect = creditsObject.GetComponent<RectTransform>();
            creditsObject.SetActive(false); 
        }

        if (victoryMessageObject != null) victoryMessageObject.SetActive(true);

        // Inicia a sequência
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // 1. Fica parado a mostrar a mensagem de vitória
        yield return new WaitForSeconds(messageDuration);

        // 2. Troca os textos
        if (victoryMessageObject != null) victoryMessageObject.SetActive(false);
        if (creditsObject != null) creditsObject.SetActive(true);

        // 3. Autoriza o movimento
        isScrolling = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            LoadMenu();
        }

        // Lógica de Rolar o Texto (só acontece depois da mensagem desaparecer)
        if (isScrolling && creditsObject != null)
        {
            creditsObject.transform.Translate(Vector3.up * scrollSpeed * Time.deltaTime);

            // Verifica se já subiu tudo
            if (creditsRect.anchoredPosition.y > endYPosition)
            {
                LoadMenu();
            }
        }
    }

    void LoadMenu()
    {
        Debug.Log("Fim. A carregar menu...");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}