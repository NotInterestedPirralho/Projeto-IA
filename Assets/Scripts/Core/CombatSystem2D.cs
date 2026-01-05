using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using System.Collections;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
public class CombatSystem2D : MonoBehaviourPunCallbacks
{
    [Header("Ataque")]
    public int damage = 10;
    public float attackRange = 1f;
    public float attackCooldown = 0.5f;
    public Transform attackPoint;
    public LayerMask enemyLayers;

    [Header("Ataque Carregado")]
    public int chargedDamageMultiplier = 2;
    public float chargeTime = 1f;

    [Header("Defesa")]
    public float defenseCooldown = 2f;
    [HideInInspector] public bool isDefending = false;

    [Header("Knockback (PvP)")]
    public float pvpKnockbackForce = 5f;
    public float pvpKnockbackDuration = 0.2f;

    [Header("VFX")]
    public GameObject hitVFX;

    [Header("UI Defesa")]
    public Image defenseIcon;
    public TextMeshProUGUI defenseText;

    private float nextAttackTime = 0f;
    private float nextDefenseTime = 0f;
    private Animator anim;
    private PhotonView pv;

    private float chargeStartTime = 0f;
    [HideInInspector] public bool isCharging = false;

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (pv != null && !pv.IsMine)
        {
            enabled = false;
        }
    }

    void Start()
    {
        anim = GetComponent<Animator>();

        // Lógica de procura de UI
        if (defenseIcon == null || defenseText == null)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                if (defenseIcon == null)
                    defenseIcon = canvas.transform.Find("DefenseIcon")?.GetComponent<Image>();

                if (defenseText == null && defenseIcon != null)
                    defenseText = defenseIcon.transform.Find("DefenseCooldownText")?.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    void Update()
    {
        if (pv != null && !pv.IsMine) return;

        // ----------------------------------------------------
        // BLOQUEIO POR ESTADO DE JOGO (LOBBY, PAUSA, CHAT)
        // ----------------------------------------------------
        
        // CORREÇÃO: Usar GameChat.instance diretamente para evitar referências mortas
        bool isChatActive = (GameChat.instance != null && GameChat.instance.IsChatOpen);
        bool isPaused = PMMM.IsPausedLocally;
        bool lobbyBlocking = (LobbyManager.instance != null && !LobbyManager.GameStartedAndPlayerCanMove);

        if (isPaused || isChatActive || lobbyBlocking)
        {
            CancelCurrentActions();
            return; 
        }

        // --- LÓGICA DE ATAQUE (Mouse 0) ---
        HandleAttackInput();

        // --- LÓGICA DE DEFESA (Mouse 1) ---
        HandleDefenseInput();

        UpdateDefenseUI();
    }

    private void CancelCurrentActions()
    {
        // Cancela Defesa se estiver ativa durante o bloqueio
        if (isDefending)
        {
            if (pv != null && PhotonNetwork.InRoom)
                pv.RPC(nameof(SetDefenseState), RpcTarget.All, false);
            else
                SetDefenseState(false);
        }

        // Cancela Carregamento de Ataque se estiver ativo durante o bloqueio
        if (isCharging)
        {
            isCharging = false;
            if (pv != null && PhotonNetwork.InRoom)
                pv.RPC(nameof(SetAttackState), RpcTarget.All, false, 1f);
            else
                SetAttackState(false, 1f);
        }
    }

    private void HandleAttackInput()
    {
        // Iniciar Carregamento
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime && !isDefending && !isCharging)
        {
            isCharging = true;
            chargeStartTime = Time.time;

            if (pv != null && PhotonNetwork.InRoom)
                pv.RPC(nameof(SetAttackState), RpcTarget.All, true, 0f);
            else
                SetAttackState(true, 0f);
        }

        // Executar Ataque
        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            isCharging = false;
            float chargeDuration = Time.time - chargeStartTime;
            bool isCharged = chargeDuration >= chargeTime;

            if (pv != null && PhotonNetwork.InRoom)
                pv.RPC(nameof(SetAttackState), RpcTarget.All, true, 1f);
            else
                SetAttackState(true, 1f);

            nextAttackTime = Time.time + attackCooldown;

            if (pv == null || pv.IsMine)
                ApplyDamageAndVFX(isCharged);
        }
    }

    private void HandleDefenseInput()
    {
        if (Input.GetMouseButtonDown(1) && Time.time >= nextDefenseTime && !isDefending && !isCharging)
        {
            if (pv != null && PhotonNetwork.InRoom)
                pv.RPC(nameof(SetDefenseState), RpcTarget.All, true);
            else
                SetDefenseState(true);
        }

        if (Input.GetMouseButtonUp(1) && isDefending)
        {
            if (pv != null && PhotonNetwork.InRoom)
                pv.RPC(nameof(SetDefenseState), RpcTarget.All, false);
            else
                SetDefenseState(false);
            
            nextDefenseTime = Time.time + defenseCooldown;
        }
    }

    [PunRPC]
    void SetAttackState(bool state, float animSpeed)
    {
        if (anim)
        {
            anim.SetBool("IsAttacking", state);
            anim.speed = animSpeed;
        }
        if (!state && anim) anim.speed = 1f;
    }

    public void AttackEnd()
    {
        if (pv != null && PhotonNetwork.InRoom)
            pv.RPC(nameof(SetAttackState), RpcTarget.All, false, 1f);
        else
            SetAttackState(false, 1f);
    }

    void ApplyDamageAndVFX(bool isCharged)
    {
        int currentDamage = isCharged ? damage * chargedDamageMultiplier : damage;

        // VFX Spawn
        if (hitVFX != null && attackPoint != null)
        {
            GameObject vfx = PhotonNetwork.InRoom 
                ? PhotonNetwork.Instantiate(hitVFX.name, attackPoint.position, Quaternion.identity)
                : Instantiate(hitVFX, attackPoint.position, Quaternion.identity);
            
            StartCoroutine(DestroyVFX(vfx, isCharged ? 1.5f : 1f));
        }

        // Hit Detection
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.gameObject == gameObject) continue;

            PhotonView targetView = enemy.GetComponent<PhotonView>();
            CombatSystem2D targetCombat = enemy.GetComponent<CombatSystem2D>();
            Health targetHealth = enemy.GetComponent<Health>();
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

            if (targetHealth != null || enemyHealth != null)
            {
                bool targetDefending = (targetCombat != null && targetCombat.isDefending);
                int finalDamage = targetDefending ? currentDamage / 4 : currentDamage;

                if (targetHealth != null)
                {
                    if (targetView != null && targetView.ViewID != pv.ViewID)
                        targetView.RPC("TakeDamageComplete", RpcTarget.All, finalDamage, pv.ViewID, pvpKnockbackForce, pvpKnockbackDuration);
                    else if (targetView == null && targetHealth.gameObject != gameObject)
                        targetHealth.TakeDamageComplete(finalDamage, 0, pvpKnockbackForce, pvpKnockbackDuration);
                }
                else if (enemyHealth != null)
                {
                    if (targetView != null)
                        targetView.RPC("TakeDamage", RpcTarget.All, finalDamage, pv.ViewID);
                    else
                        enemyHealth.TakeDamage(finalDamage, 0);
                }

                if (pv != null && PhotonNetwork.InRoom)
                    PhotonNetwork.LocalPlayer.AddScore(finalDamage);
            }
        }
    }

    private void UpdateDefenseUI()
    {
        if (defenseIcon == null) return;
        float remaining = nextDefenseTime - Time.time;

        if (remaining > 0f && !isDefending)
        {
            defenseIcon.color = new Color(defenseIcon.color.r, defenseIcon.color.g, defenseIcon.color.b, 0.5f);
            if (defenseText != null) defenseText.text = Mathf.CeilToInt(remaining).ToString();
        }
        else
        {
            defenseIcon.color = new Color(defenseIcon.color.r, defenseIcon.color.g, defenseIcon.color.b, isDefending ? 0.7f : 1f);
            if (defenseText != null) defenseText.text = "";
        }
    }

    [PunRPC]
    public void KillConfirmed()
    {
        if (pv != null && !pv.IsMine) return;
        if (PhotonNetwork.InRoom)
        {
            int currentKills = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Kills") ? (int)PhotonNetwork.LocalPlayer.CustomProperties["Kills"] : 0;
            Hashtable props = new Hashtable { { "Kills", currentKills + 1 } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    private IEnumerator DestroyVFX(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (vfx == null) yield break;
        if (vfx.GetComponent<PhotonView>() != null && vfx.GetComponent<PhotonView>().IsMine) PhotonNetwork.Destroy(vfx);
        else if (vfx.GetComponent<PhotonView>() == null) Destroy(vfx);
    }

    [PunRPC]
    void SetDefenseState(bool state)
    {
        isDefending = state;
        if (anim) anim.SetBool("IsDefending", state);
    }

    [PunRPC]
    public void BoostDamage(float multiplier, float duration) => StartCoroutine(DamageBuffRoutine(multiplier, duration));

    private IEnumerator DamageBuffRoutine(float multiplier, float duration)
    {
        int original = damage;
        damage = Mathf.RoundToInt(damage * multiplier);
        yield return new WaitForSeconds(duration);
        damage = original;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
