using System.Collections;
using UnityEngine;

public class SceneCameraConfig : MonoBehaviour
{
    // --- SINGLETON ---
    public static SceneCameraConfig instance;

    private void Awake()
    {
        instance = this;
    }

    [Header("Zoom desta cena")]
    public float introStartSize = 12f;
    public float normalSize = 6f;
    public float edgeSize = 9f;

    [Header("Limites Horizontais (X)")]
    public float leftTriggerX = -15f;
    public float rightTriggerX = 73f;

    [Header("Limite Vertical (Y) - Câmara")]
    public float cameraMinY = -10f;

    [Header("Limites de Morte (Y) - Altura da Arena")]
    public float bottomLimitY = -25f;
    public float topLimitY = 30f;

    // Margem para o countdown horizontal
    private float buffer = 5f;

    private IEnumerator Start()
    {
        Camera cam = Camera.main;

        // 1. ZOOM
        CameraDynamicZoom dyn = cam.GetComponent<CameraDynamicZoom>();
        if (dyn != null)
        {
            dyn.ConfigureForScene(introStartSize, normalSize, edgeSize, leftTriggerX, rightTriggerX);
        }

        // 2. LIMITES FÍSICOS DA CÂMARA
        CameraFollowLimited follow = cam.GetComponent<CameraFollowLimited>();
        if (follow != null)
        {
            follow.minX = leftTriggerX - 50f;
            follow.maxX = rightTriggerX + 50f;
            follow.minY = cameraMinY;
        }

        yield return null;
    }

    public void ConfigureNewPlayer(OutOfArenaCountdown deathScript)
    {
        if (deathScript != null)
        {
            deathScript.minBounds = new Vector2(leftTriggerX - buffer, bottomLimitY);
            deathScript.maxBounds = new Vector2(rightTriggerX + buffer, topLimitY);

            Debug.Log($"[SceneCameraConfig] Limites aplicados a um novo Player (Respawn)!");
        }
    }

    // --- VISUALIZAÇÃO NO EDITOR (GIZMOS) ---
    private void OnDrawGizmos()
    {
        // 1. Desenhar a Área de Morte (Retângulo que define onde o player vive)
        Gizmos.color = Color.yellow;
        float width = (rightTriggerX + buffer) - (leftTriggerX - buffer);
        float height = topLimitY - bottomLimitY;
        Vector3 center = new Vector3((leftTriggerX - buffer + rightTriggerX + buffer) / 2, (topLimitY + bottomLimitY) / 2, 0);
        
        Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));

        // 2. Desenhar as linhas de Trigger de Zoom (Azul)
        Gizmos.color = Color.cyan;
        // Linha Esquerda
        Gizmos.DrawLine(new Vector3(leftTriggerX, topLimitY, 0), new Vector3(leftTriggerX, bottomLimitY, 0));
        // Linha Direita
        Gizmos.DrawLine(new Vector3(rightTriggerX, topLimitY, 0), new Vector3(rightTriggerX, bottomLimitY, 0));

        // 3. Desenhar o limite mínimo da câmara (Verde)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(leftTriggerX - buffer, cameraMinY, 0), new Vector3(rightTriggerX + buffer, cameraMinY, 0));
    }
}
