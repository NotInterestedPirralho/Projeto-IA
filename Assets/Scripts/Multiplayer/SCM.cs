using UnityEngine;
using UnityEngine.SceneManagement;

public class SCM : MonoBehaviour
{
    // Variável estática para armazenar a escolha do personagem.
    // Estática para que possa ser acessada de qualquer lugar.
    public static string selectedCharacter = "None";

    // Enum para melhor organização e tipagem dos personagens
    public enum CharacterType
    {
        None,
        Soldier,
        Skeleton,
        Knight,
        Orc
    }

    // Método público para ser chamado pelos botões de seleção de personagem
    public void SelectCharacter(string characterName)
    {
        selectedCharacter = characterName;
        Debug.Log("Personagem selecionado: " + selectedCharacter);

        // Opcional: Aqui você pode adicionar lógica para destacar o botão selecionado na UI.
    }
    
    // Sobrecarga usando o enum para flexibilidade (Recomendado)
    public void SelectCharacter(CharacterType character)
    {
        if (character == CharacterType.None)
        {
            selectedCharacter = "None";
        }
        else
        {
            // Converte o enum para string e armazena
            selectedCharacter = character.ToString();
        }
        
        Debug.Log("Personagem selecionado: " + selectedCharacter);

        // Opcional: Adicionar lógica visual de seleção aqui
    }


    // Método público para ser chamado pelo botão "Play"
    public void GoToLobby()
    {
        // Verifica se um personagem foi selecionado antes de prosseguir
        if (selectedCharacter == "None" || string.IsNullOrEmpty(selectedCharacter))
        {
            Debug.LogError("Por favor, selecione um personagem antes de clicar em Play.");
            // Opcional: Mostrar uma mensagem de aviso na tela para o usuário.
            return;
        }

        // Carrega a próxima cena
        // **IMPORTANTE**: Certifique-se de que a cena "MultiplayerLobby" está adicionada
        // nas configurações de Build Settings (File > Build Settings).
        SceneManager.LoadScene("MultiplayerLobby");
    }

    // Exemplo de como você acessaria o personagem escolhido na cena MultiplayerLobby:
    // string meuPersonagem = SCM.selectedCharacter;
}
