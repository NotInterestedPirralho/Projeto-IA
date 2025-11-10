using UnityEngine;

public class MouseConfiner : MonoBehaviour
{
    void Start()
    {
        // Começa o jogo com o cursor confinado
        LockCursor();
    }

    void Update()
    {
        // Se o utilizador premir Escape, liberta o cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true; // Garante que fica visível ao ser libertado
        }

        // Se o utilizador clicar na janela (botão esquerdo) E o cursor estiver livre,
        // confina-o novamente.
        // Isto é útil para voltar a prender o rato depois de premir 'Escape'.
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            LockCursor();
        }
    }

    // Função para prender o cursor
    void LockCursor()
    {
        // Confina o cursor à janela do jogo
        Cursor.lockState = CursorLockMode.Confined;
        // Garante que o cursor permanece visível
        Cursor.visible = true;
    }
}
