using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class FitBackgroundToCamera : MonoBehaviour
{
    private SpriteRenderer sr;
    private Camera cam;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private IEnumerator Start()
    {
        // Espera até existir uma MainCamera (como a tua está no player)
        while (Camera.main == null)
            yield return null;

        cam = Camera.main;

        AjustarEscalaAoEcrã();
    }

    private void AjustarEscalaAoEcrã()
    {
        if (sr == null || cam == null) return;

        // Fundo sempre atrás de tudo
        sr.sortingOrder = -10;

        // Tamanho visível da câmara
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        // Tamanho do sprite
        float spriteHeight = sr.sprite.bounds.size.y;
        float spriteWidth = sr.sprite.bounds.size.x;

        // Fatores de escala para preencher o ecrã
        float scaleY = camHeight / spriteHeight;
        float scaleX = camWidth / spriteWidth;

        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        // Faz o fundo seguir sempre a posição da câmara
        Vector3 camPos = cam.transform.position;
        transform.position = new Vector3(
            camPos.x,
            camPos.y,
            transform.position.z // mantém o Z como está
        );
    }
}
