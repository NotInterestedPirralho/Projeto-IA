using UnityEngine;
using TMPro;
using Photon.Pun;

public class OutOfArenaCountdown : MonoBehaviour
{
    [Header("Zona segura da arena (coordenadas de mundo)")]
    public Vector2 minBounds = new Vector2(-10f, -5f);
    public Vector2 maxBounds = new Vector2(10f, 5f);

    [Header("Countdown")]
    public float countdownTime = 3f;
    public TextMeshProUGUI countdownText;

    [Header("Referências")]
    public Health health;

    [Header("Debug")]
    public bool debugLogs = false;

    float timer;
    bool isOutside = false;
    bool hasBeenInsideOnce = false;

    PhotonView view;

    void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        view = GetComponent<PhotonView>();
    }

    void Start()
    {
        timer = countdownTime;

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (health == null)
            return;

        // só o dono do player trata disto
        if (view != null && !view.IsMine)
            return;

        Vector3 pos = transform.position;

        bool inside =
            pos.x >= minBounds.x && pos.x <= maxBounds.x &&
            pos.y >= minBounds.y && pos.y <= maxBounds.y;

        // PRIMEIRO: garantir que ele já esteve dentro uma vez
        // para não matar logo ao spawn se os bounds estiverem mal
        if (!hasBeenInsideOnce)
        {
            if (inside)
            {
                hasBeenInsideOnce = true;
                if (debugLogs) Debug.Log("[OutOfArena] Player entrou na arena pela primeira vez.");
            }

            // enquanto não tiver estado dentro, não faz mais nada
            return;
        }

        if (!inside)
        {
            if (!isOutside)
            {
                isOutside = true;
                timer = countdownTime;

                if (debugLogs) Debug.Log($"[OutOfArena] Saiu da arena. Pos = {pos}");

                if (countdownText != null)
                    countdownText.gameObject.SetActive(true);
            }

            timer -= Time.deltaTime;

            if (countdownText != null)
            {
                int value = Mathf.CeilToInt(timer);
                countdownText.text = value.ToString();
            }

            if (timer <= 0f)
            {
                KillPlayer();
            }
        }
        else
        {
            // voltou para dentro da zona
            if (isOutside)
            {
                if (debugLogs) Debug.Log("[OutOfArena] Voltou para a arena, countdown cancelado.");

                isOutside = false;
                timer = countdownTime;

                if (countdownText != null)
                    countdownText.gameObject.SetActive(false);
            }
        }
    }

    void KillPlayer()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (health == null)
            return;

        if (debugLogs) Debug.Log("[OutOfArena] Tempo acabou, matar player.");

        int damage = health.health;

        PhotonView hView = health.GetComponent<PhotonView>();
        if (hView != null)
        {
            // -1 = sem atacante (não conta como kill de ninguém)
            hView.RPC(nameof(Health.TakeDamage), RpcTarget.All, damage, -1);
        }
        else
        {
            health.TakeDamage(damage, -1);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) / 2f,
            (minBounds.y + maxBounds.y) / 2f,
            0f
        );
        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y,
            0f
        );
        Gizmos.DrawWireCube(center, size);
    }
}
