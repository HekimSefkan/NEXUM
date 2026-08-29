using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

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
            Destroy(gameObject);
        }
    }

    void Start()
    {
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
        if (bgmSource != null)
        {
            int musicOn = PlayerPrefs.GetInt("MusicOn", 1);
            bgmSource.mute = (musicOn == 0); // 0 ise sessize al
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        // YENİ: Efekt çalmadan önce hafızaya bak. Kapalıysa (0), hiç çalmadan geri dön!
        int sfxOn = PlayerPrefs.GetInt("SfxOn", 1);
        if (sfxOn == 0) return; 

        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    public void PlayButtonSound()
    {
        PlaySFX(buttonClip);
    }
}