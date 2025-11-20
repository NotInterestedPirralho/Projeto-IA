using System.Collections;
using UnityEngine;

public class SceneCameraConfig : MonoBehaviour
{
    [Header("Zoom desta cena")]
    public float introStartSize = 12f;
    public float normalSize = 6f;
    public float edgeSize = 9f;

    [Header("Triggers de borda (Limites do Mapa)")]
    public float leftTriggerX = -15f;
    public float rightTriggerX = 73f;

    // Margem extra para o countdown não começar exatamente na linha da câmara
    // Se a câmara para no 73, o countdown começa no 78 (73 + 5)
    private float buffer = 5f;

    private IEnumerator Start()
    {
        Debug.Log("[SceneCameraConfig] À espera do Player...");

        // 1. Espera até o Player nascer
        while (GameObject.FindGameObjectWithTag("Player") == null)
        {
            yield return null;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Camera cam = Camera.main; // A câmara deve estar no player

        Debug.Log("[SceneCameraConfig] A configurar tudo para este mapa...");

        // --- 1. CONFIGURAR ZOOM (CameraDynamicZoom) ---
        CameraDynamicZoom dyn = cam.GetComponent<CameraDynamicZoom>();
        if (dyn != null)
        {
            dyn.ConfigureForScene(introStartSize, normalSize, edgeSize, leftTriggerX, rightTriggerX);
        }

        // --- 2. CONFIGURAR LIMITES FÍSICOS DA CÂMARA (CameraFollowLimited) ---
        CameraFollowLimited follow = cam.GetComponent<CameraFollowLimited>();
        if (follow != null)
        {
            // Dá folga física para a câmara não travar antes do tempo
            follow.minX = leftTriggerX - 50f;
            follow.maxX = rightTriggerX + 50f;
        }

        // --- 3. CONFIGURAR MORTE (OutOfArenaCountdown) ---
        // É AQUI QUE RESOLVEMOS O TEU PROBLEMA DO 18.1
        OutOfArenaCountdown deathScript = player.GetComponent<OutOfArenaCountdown>();

        if (deathScript != null)
        {
            // Atualiza o limite ESQUERDO
            // Mantém o Y original, muda só o X para o novo valor (-15 - buffer)
            deathScript.minBounds = new Vector2(leftTriggerX - buffer, deathScript.minBounds.y);

            // Atualiza o limite DIREITO
            // Mantém o Y original, muda só o X para o novo valor (73 + buffer)
            deathScript.maxBounds = new Vector2(rightTriggerX + buffer, deathScript.maxBounds.y);

            Debug.Log($"[SceneCameraConfig] Limites de Morte atualizados para: {deathScript.minBounds.x} e {deathScript.maxBounds.x}");
        }
        else
        {
            Debug.LogWarning("ATENÇÃO: Não encontrei o script 'OutOfArenaCountdown' no Player!");
        }
    }
}