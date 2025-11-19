using UnityEngine;

public class FaceObjectT : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("FaceObjectT: Nenhuma câmara com a tag 'MainCamera' encontrada.");
            enabled = false;
        }
    }

    void Update()
    {
        if (mainCameraTransform != null)
        {
            // --- CÓDIGO 2D CORRIGIDO PARA EVITAR INVERSÃO ---

            // 1. Obtém a rotação da câmara no espaço global.
            Quaternion cameraRotation = mainCameraTransform.rotation;

            // 2. Cria uma nova rotação que é a rotação da câmara, mas com X e Y bloqueados a 0.
            // A rotação da câmara 2D é tipicamente apenas um Quaternion com Z a 0, 
            // mas o uso de Quaternion.identity (ou Quaternion.Euler(0, 0, 0)) no 
            // espaço local corrige frequentemente o problema de inversão do texto.

            // Tentativa 1 (Mais simples e muitas vezes resolve a inversão):
            transform.rotation = Quaternion.identity;

            // Se o texto parecer estar a "cair" ou a não estar fixo, 
            // usa esta linha em vez da anterior:
            // transform.rotation = Quaternion.Euler(0f, 0f, 0f); 

            // Se o teu objetivo for ser um BILLBOARD COMPLETO (virado para o ecrã):
            // transform.rotation = mainCameraTransform.rotation;

            // Mas, se o teu texto está invertido (180 graus no eixo Y), 
            // é provável que a rotação da câmara não seja perfeita (0, 0, 0)
            // na perspetiva local do objeto.

            // O uso de Quaternion.identity força o objeto a não ter rotação
            // local, o que é o estado correto para texto num jogo 2D, a menos que 
            // a câmara principal esteja a rodar.
        }
    }
}