using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Configuracoes de Audio")]
    public AudioSource audioSource;
    public AudioClip clickSound;

    void Awake()
    {
        // Padrao Singleton: Garante que so existe um e sobrevive entre cenas
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Configuracao automatica do AudioSource
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0; // Garante som 2D para UI
    }

    // Funcao estatica que pode ser chamada de qualquer script sem precisar de referencia
    public static void PlayClick()
    {
        if (instance != null && instance.clickSound != null)
        {
            instance.audioSource.PlayOneShot(instance.clickSound);
        }
    }
}