using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // Sahne yeniden yüklendiğinde oluşan kopya yok edilmez; sahnedeki butonların çağrılarını gerçek Instance'a iletir
    private bool isProxy = false;

    // Buton sesi kare koruması (aynı karede ikinci çağrı yok sayılır)
    private int lastButtonSoundFrame = -1;
    public bool IsProxy => isProxy;

    [Header("Ses Kaynakları (Audio Sources)")]
    public AudioSource bgmSource; 
    public AudioSource sfxSource; 

    [Header("Temel Ses Dosyaları (Clips)")]
    public AudioClip bgmClip;       
    public AudioClip shiftClip;     
    public AudioClip mergeClip;     
    public AudioClip errorClip;     
    public AudioClip comboClip;     
    public AudioClip buttonClip;    

    [Header("Oyun Durumu Sesleri (Clips)")]
    public AudioClip winClip;         
    public AudioClip gameOverClip;    
    public AudioClip quizCorrectClip; 
    public AudioClip quizWrongClip;   

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            BecomeProxy();
        }
    }

    // Proxy: kendi seslerini susturur, müzik başlatmaz, DontDestroyOnLoad'a girmez ve Instance'ı değiştirmez
    private void BecomeProxy()
    {
        isProxy = true;
        foreach (AudioSource source in GetComponents<AudioSource>())
        {
            source.Stop();
            source.mute = true;
        }
    }

    void Start()
    {
        if (isProxy) return;

        if (bgmClip != null && bgmSource != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
            
            // YENİ: Oyun başlarken müziğin açık/kapalı durumunu kontrol et
            UpdateMusicState(); 
        }
    }

    // YENİ: Ayarlar menüsünden butona basıldığı an müziği anında kesen/açan fonksiyon
    public void UpdateMusicState()
    {
        if (isProxy)
        {
            if (Instance != null && Instance != this) Instance.UpdateMusicState();
            return;
        }

        if (bgmSource != null)
        {
            int musicOn = SaveService.Data.musicOn;
            bgmSource.mute = (musicOn == 0); // 0 ise sessize al
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (isProxy)
        {
            if (Instance != null && Instance != this) Instance.PlaySFX(clip, volume);
            return;
        }

        // YENİ: Efekt çalmadan önce hafızaya bak. Kapalıysa (0), hiç çalmadan geri dön!
        int sfxOn = SaveService.Data.sfxOn;
        if (sfxOn == 0) return; 

        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    public void PlayButtonSound()
    {
        if (isProxy)
        {
            if (Instance != null && Instance != this) Instance.PlayButtonSound();
            return;
        }

        // Aynı karede ikinci çağrı yok sayılır: bir butonun sesi hem sahnedeki
        // OnClick listesinden hem de koddan gelebiliyor. Koruma burada durur ki
        // MainMenu sahnesi de kapsansın; UIManager yalnızca Game sahnesinde var.
        if (lastButtonSoundFrame == Time.frameCount) return;
        lastButtonSoundFrame = Time.frameCount;

        PlaySFX(buttonClip);
    }
}