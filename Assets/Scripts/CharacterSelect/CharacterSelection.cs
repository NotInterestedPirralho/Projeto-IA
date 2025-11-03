using UnityEngine;

public class CharacterSelection : MonoBehaviour
{
    public static CharacterSelection Instance { get; private set; }

    // nome do prefab em Resources (Soldier, Chef, Thief...)
    public string selectedCharacterName = "Soldier"; // default

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectCharacter(string prefabName)
    {
        selectedCharacterName = prefabName;
        Debug.Log($"[CharacterSelection] Personagem escolhido: {selectedCharacterName}");
    }
}
