using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;   // necessário para LoadScene

public class PauseManager : MonoBehaviour
{
    public GameObject painelPause;
    private bool jogoPausado = false;

    void Start()
    {
        if (painelPause != null)
            painelPause.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (jogoPausado) RetomarJogo();
            else PausarJogo();
        }
    }

    public void PausarJogo()
    {
        Time.timeScale = 0f;
        jogoPausado = true;
        if (painelPause != null) painelPause.SetActive(true);
    }

    public void RetomarJogo()
    {
        Time.timeScale = 1f;
        jogoPausado = false;
        if (painelPause != null) painelPause.SetActive(false);
    }

    // 👉 Esta é a função que deves ligar ao botão Exit
    public void SairParaMenu()
    {
        Time.timeScale = 1f;  // repõe o tempo
        jogoPausado = false;
        SceneManager.LoadScene("MainMenu"); // usa exatamente o nome da tua cena de menu
    }
}
