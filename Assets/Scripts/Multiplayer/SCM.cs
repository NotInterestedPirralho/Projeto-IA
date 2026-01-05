using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SCM : MonoBehaviour
{
    // Mantemos a estática para acesso rápido
    public static string selectedCharacter = "None";

    [Header("Feedback Visual")]
    // Arraste os 4 botões (Soldier, Skeleton, Knight, Orc) por ordem para aqui
    public UISelectionHandler[] allCharacterButtons;

    public enum CharacterType
    {
        None,
        Soldier,
        Skeleton,
        Knight,
        Orc
    }

    // Método chamado pelos botões (String)
    public void SelectCharacter(string characterName)
    {
        selectedCharacter = characterName;

        // Guarda a escolha na memória permanente do jogo
        PlayerPrefs.SetString("SelectedCharacter", characterName);

        Debug.Log("Personagem selecionado e salvo: " + selectedCharacter);

        // --- ADICIONADO: Atualiza a marcação visual ---
        UpdateVisualSelection(characterName);
    }

    // Método para gerir as cores dos botões
    private void UpdateVisualSelection(string charName)
    {
        if (allCharacterButtons == null || allCharacterButtons.Length == 0) return;

        for (int i = 0; i < allCharacterButtons.Length; i++)
        {
            if (allCharacterButtons[i] == null) continue;

            bool isSelected = false;
            // Compara a string guardada com o personagem correspondente ao botão
            if (i == 0 && charName == "Soldier") isSelected = true;
            else if (i == 1 && charName == "Skeleton") isSelected = true;
            else if (i == 2 && charName == "Knight") isSelected = true;
            else if (i == 3 && charName == "Orc") isSelected = true;

            allCharacterButtons[i].SetSelected(isSelected);
        }
    }

    // Método chamado pelos botões (Enum - Opcional)
    public void SelectCharacter(CharacterType character)
    {
        string charName = character.ToString();
        if (character == CharacterType.None) charName = "None";
        SelectCharacter(charName);
    }

    public void GoToLobby()
    {
        if (selectedCharacter == "None" || string.IsNullOrEmpty(selectedCharacter))
        {
            Debug.LogError("Por favor, selecione um personagem antes de clicar em Play.");
            return;
        }

        SceneManager.LoadScene("MultiplayerLobby");
    }
}