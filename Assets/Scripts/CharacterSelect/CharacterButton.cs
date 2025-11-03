using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : MonoBehaviour
{
    [Tooltip("Nome do prefab em Resources (Soldier, Chef, Thief...)")]
    public string prefabName;

    [Tooltip("Imagem usada como highlight/contorno quando este botão está selecionado")]
    public Image highlight;

    public void OnClickSelect()
    {
        if (CharacterSelection.Instance == null)
        {
            Debug.LogWarning("CharacterSelection ainda não existe na cena.");
            return;
        }

        CharacterSelection.Instance.SelectCharacter(prefabName);
        HighlightThisButton();
    }

    private void HighlightThisButton()
    {
        if (highlight == null) return;

        // Desliga o highlight nos outros botões irmãos
        Transform parent = transform.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var btn = parent.GetChild(i).GetComponent<CharacterButton>();
            if (btn != null && btn.highlight != null)
                btn.highlight.enabled = false;
        }

        // Liga o highlight deste
        highlight.enabled = true;
    }
}
