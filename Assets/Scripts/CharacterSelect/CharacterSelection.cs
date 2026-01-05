using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CharacterSelection : MonoBehaviour
{
    // Singleton para ser fácil aceder de outras cenas
    public static CharacterSelection Instance { get; private set; }

    [Header("Configuração de Personagem")]
    public string selectedPrefabName = "Soldier";
    private const string CHARACTER_KEY = "pChar"; // Chave para o Photon

    [Header("Cenas")]
    [SerializeField] private string multiplayerLobbySceneName = "MultiplayerLobby";

    [Header("Feedback Visual")]
    // Arraste todos os botões que têm o script UISelectionHandler para esta lista
    public UISelectionHandler[] allCharacterButtons;

    private void Awake()
    {
        // Garantir que só existe um CharacterSelection e que sobrevive às cenas
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Chamado pelos botões da CharacterSelect
    public void SetSelectedCharacter(string prefabName)
    {
        selectedPrefabName = prefabName;
        Debug.Log($"[CharacterSelection] Personagem escolhida: {selectedPrefabName}");

        // 1. Guardar a escolha no Photon para Multiplayer (Sincronização)
        if (PhotonNetwork.IsConnected)
        {
            Hashtable props = new Hashtable { { CHARACTER_KEY, prefabName } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            Debug.Log($"[CharacterSelection] Escolha gravada no Photon: {prefabName}");
        }

        // 2. Tocar o som de clique (usando o nosso AudioManager estático)
        AudioManager.PlayClick();

        // 3. Atualizar a marcação visual dos botões
        UpdateVisualSelection(prefabName);
    }

    private void UpdateVisualSelection(string prefabName)
    {
        if (allCharacterButtons == null || allCharacterButtons.Length == 0) return;

        foreach (var handler in allCharacterButtons)
        {
            if (handler == null) continue;

            // Compara o nome do GameObject com o nome do prefab (ex: se o botão se chamar "Btn_Soldier")
            // Usamos ToLower para evitar problemas com letras maiúsculas/minúsculas
            bool isThisOne = handler.gameObject.name.ToLower().Contains(prefabName.ToLower());
            handler.SetSelected(isThisOne);
        }
    }

    // Função para o botão Play chamar (Usado no Training Ground)
    public void LoadScene(string sceneName)
    {
        AudioManager.PlayClick();
        SceneManager.LoadScene(sceneName);
    }

    // Função para o botão PLAY da cena MultiplayerCharacterSelect chamar
    public void LoadMultiplayerLobby()
    {
        AudioManager.PlayClick();

        if (string.IsNullOrEmpty(multiplayerLobbySceneName))
        {
            Debug.LogError("[CharacterSelection] Nome da cena do Lobby não definido!");
            return;
        }

        SceneManager.LoadScene(multiplayerLobbySceneName);
    }
}