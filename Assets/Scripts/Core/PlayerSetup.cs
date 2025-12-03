using Photon.Pun;
using TMPro;
using UnityEngine;

// Implementa IPunObservable para sincronização de dados de animação
public class PlayerSetup : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Componentes Obrigatórios")]
    public Movement2D movement;
    public GameObject camara; // ARRASTA A CÂMARA DO BONECO PARA AQUI NO PREFAB
    public CombatSystem2D combat;

    [Header("UI")]
    public string nickname;
    public TextMeshPro nicknameText;

    // Referências Privadas
    private SpriteRenderer spriteRenderer;
    private PhotonView photonView;
    private Animator anim;

    // Variáveis de Sincronização
    private float syncSpeed;
    private bool syncGrounded;
    private bool syncFlipX;
    private bool syncIsDefending;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // Se for o meu boneco...
        if (photonView.IsMine)
        {
            // Chamamos a configuração local
            IsLocalPlayer();
        }
        else // Se for boneco de outro jogador...
        {
            DisableRemotePlayer();
        }
    }

    // Chamado para configurar o JOGADOR LOCAL (EU)
    public void IsLocalPlayer()
    {
        Debug.Log($"[PlayerSetup] Configurando jogador local: {gameObject.name}");

        if (movement != null) movement.enabled = true;
        if (combat != null) combat.enabled = true;

        // ATIVAÇÃO DA CÂMARA
        if (camara != null)
        {
            Debug.Log("[PlayerSetup] A ativar Câmara do Jogador.");
            camara.SetActive(true);

            // Garante que o AudioListener está ligado para ouvires o jogo
            AudioListener listener = camara.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;

            // Zoom Dinâmico
            var zoomDynamic = camara.GetComponent<CameraDynamicZoom>();
            if (zoomDynamic != null) zoomDynamic.enabled = true;
        }
        else
        {
            Debug.LogError("[PlayerSetup] ERRO CRÍTICO: A variável 'camara' não está associada no Inspector do Prefab!");
        }
    }

    // Chamado para configurar JOGADORES REMOTOS (OUTROS)
    public void DisableRemotePlayer()
    {
        if (movement != null) movement.enabled = false;
        if (combat != null) combat.enabled = false;

        // Garante que a câmara dos outros está DESLIGADA
        if (camara != null)
        {
            camara.SetActive(false);

            // Desliga também o AudioListener dos outros para não ouvires o mundo da posição deles
            AudioListener listener = camara.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }

    // --- SINCRONIZAÇÃO (Mantive igual ao teu, está correto) ---

    void Update()
    {
        if (!photonView.IsMine)
        {
            if (anim)
            {
                anim.SetFloat("Speed", syncSpeed);
                anim.SetBool("Grounded", syncGrounded);
                anim.SetBool("IsDefending", syncIsDefending);

                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = syncFlipX;
                }
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // ENVIA DADOS
            stream.SendNext(movement.CurrentHorizontalSpeed);
            stream.SendNext(movement.IsGrounded);
            stream.SendNext(combat != null && combat.isDefending);

            if (spriteRenderer != null) stream.SendNext(spriteRenderer.flipX);
        }
        else
        {
            // RECEBE DADOS
            this.syncSpeed = (float)stream.ReceiveNext();
            this.syncGrounded = (bool)stream.ReceiveNext();
            this.syncIsDefending = (bool)stream.ReceiveNext();

            if (spriteRenderer != null) this.syncFlipX = (bool)stream.ReceiveNext();
        }
    }

    [PunRPC]
    public void SetNickname(string _nickname)
    {
        nickname = _nickname;
        if (nicknameText != null) nicknameText.text = nickname;
    }
}