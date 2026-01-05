using UnityEngine;
using UnityEngine.UI;

public class UISelectionHandler : MonoBehaviour
{
    [Header("Configuração Visual")]
    public Image buttonImage;
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0f, 0.9f, 1f, 1f); // Ciano/Azul
    public Vector3 selectedScale = new Vector3(1.1f, 1.1f, 1.1f);

    private bool isSelected = false;
    private Vector3 originalScale;

    void Awake()
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        originalScale = transform.localScale;
    }

    public void SetSelected(bool state)
    {
        if (isSelected == state) return;
        isSelected = state;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (buttonImage == null) return;

        buttonImage.color = isSelected ? selectedColor : normalColor;

        if (isSelected)
            transform.localScale = Vector3.Scale(originalScale, selectedScale);
        else
            transform.localScale = originalScale;
    }
}