# NEXUM – Claude Çalışma Notları

## Proje
- **Motor:** Unity 2022.3.62f3 (`ProjectSettings/ProjectVersion.txt`)
- **Tür:** 2D mobil (Android, dikey) kimya temalı bulmaca oyunu
- **Özet:** 4x4 grid üzerinde temel elementler (H, O, C, N, Na, Cl, Fe, Ca) kaydırılarak tarif tablosuna göre bileşiklere dönüştürülür. 7 level var; her levelin kendi spawn ağırlıkları ve hedef bileşikleri bulunur. Normal / Sınav / Serbest modları, undo, ipucu asistanı, entropi cezası ve quiz ile canlanma sistemi vardır. Ana menüde kayıt, profil, ansiklopedi, nasıl oynanır ve ayarlar panelleri bulunur.
- **Bağımlılıklar:** DOTween (`Assets/Plugins/Demigiant`), TextMeshPro (`Assets/TextMesh Pro`), uGUI. Girdi: eski Input Manager.
- **Sahneler:** `MainMenu.unity` (build 0), `Game.unity` (build 1). Her ikisinde Canvas → `Background` + `SafeAreaRoot` (+ Game'de tam ekran paneller) yapısı var.

## Klasör yapısı
```
Assets/
  Art/
    Avatars/         Mentor portreleri
    Figures/         UI görselleri
  Audio/
    Voice/           Müzik ve efekt sesleri
  Data/
    Facts/           ChemistryFact asset'leri
    Quizzes/         ChemistryQuiz asset'leri
  Editor/            AutoAnchorTools.cs, FontReplacerWindow.cs, NexumValidation.cs (editör araçları)
  Fonts/             Orbitron / Oxanium TTF + TMP SDF asset'leri
  Plugins/Demigiant/ DOTween
  Prefabs/           Tile_*.prefab (elementler/bileşikler), BeakerGoalPrefab, ComboTextPrefab, ElementCard, MergeParticle
  Resources/         SADECE kodla yüklenenler
    Elements/        ElementData asset'leri (Resources.LoadAll<ElementData>("Elements"))
    DOTweenSettings.asset  (DOTween kendi yükler)
  Scenes/            MainMenu, Game, SampleScene
  Scripts/           Oyun scriptleri (aşağıda)
  TextMesh Pro/      TMP Essentials
NEXUM stuff/         Ham kaynaklar, APK, PDF'ler (git'te ignore edilir, Unity dışı)
```

## Scriptler
| Script | Görevi |
|---|---|
| `GridManager` | Grid, kaydırma, birleşme tarifleri, spawn, undo, ipucu, entropi, game over / kazanma kontrolü |
| `GameManager` | Klavye + swipe girdisi, skor, Joker modu |
| `LevelManager` | Level verileri, ağırlıklı rastgele spawn, hedef sayaçlarını sıfırlama |
| `UIManager` | Oyun içi paneller: hedef beherleri, quiz/revive, pause, tutorial, basınç göstergesi, kombo yazısı |
| `TileInteraction` | Joker modunda dokunulan taşı puan karşılığında siler |
| `AudioManager` | DontDestroyOnLoad müzik/efekt yöneticisi (MainMenu sahnesinde) |
| `MainMenuManager` | Ana menü panelleri arasında DOTween geçişleri |
| `LevelMenuManager` | Level kilitleri ve buton görselleri |
| `RegistrationManager` | İlk kayıt: isim, e-posta doğrulama, mentor seçimi, kimlik kartı animasyonu |
| `ProfileManager` | Profil kartı, istatistikler, e-posta ekleme |
| `SettingsManager` | Oyun modu, ses toggle'ları, ilerleme sıfırlama |
| `EncyclopediaManager` | ElementData'lardan ansiklopedi kartları üretir |
| `HowToPlayManager` | Nasıl oynanır pop-up'ları |
| `ElementData` / `ChemistryFact` / `ChemistryQuiz` | ScriptableObject veri sınıfları |
| `Editor/AutoAnchorTools` | Editör aracı: seçili RectTransform'ların anchor'larını köşelerine taşır |
| `Editor/FontReplacerWindow` | Editör aracı: sahnedeki TMP fontlarını isim kurallarına göre değiştirir |
| `Editor/NexumValidation` | Editör aracı: batch mode sahne/prefab doğrulaması (kaydetmez) |
| `Core/AppBootstrap` | Sahneye eklenmeden çalışır: 60 FPS, sahne yüklenince timeScale=1, arka planda PlayerPrefs.Save |
| `Core/SafeArea` | RectTransform'u Screen.safeArea'ya göre anchor'lar (sahneye Editor'de eklenir) |

## Kesin kurallar
- **Oyun mantığına DOKUNMA:** GridManager'daki kaydırma / birleşme / spawn / undo / entropi / kazanma-kaybetme akışı; LevelManager'daki level verileri ve spawn ağırlıkları; tarif tabloları; GameManager'daki skor ve Joker kuralları; quiz / revive mantığı.
- Bir düzeltme bu alanlara dokunmayı gerektiriyorsa **uygulamadan önce kullanıcıya sor**.
- `.unity`, `.prefab` ve `.asset` dosyalarını (ProjectSettings dahil) düzenlemeden önce **Unity Editor'ün kapalı olduğunu kullanıcıya sorarak teyit et**.
- **Hiçbir dosyayı silme**; silinmesi gerekenleri listele.
- Dosya taşırken `.meta` dosyasını da birlikte taşı (GUID korunmalı).
- Her adımı **ayrı commit** olarak kaydet.
- Çalışma branch'i: `tech-cleanup`.

## Alınan kararlar
- **CanvasScaler:** İki sahnede de Scale With Screen Size, 1080×1920, **Screen Match Mode = Expand** (`m_ScreenMatchMode: 1`). 1080×1920 tasarım alanı her ekranda tamamen görünür; fazla alan uzun eksene eklenir. Yeni UI bu varsayıma göre kurulmalı: canvas genişliği hiçbir zaman 1080'in, yüksekliği 1920'nin altına düşmez.
- **Ekran yönü:** Sadece Portrait (`defaultScreenOrientation: 0`, diğer autorotate yönleri 0). Enum değerleri Unity 2022.3.62f3 DLL'inden doğrulandı: Portrait=0 … AutoRotation=4.
- **Android en-boy oranı:** Custom (`androidSupportedAspectRatio: 2`) + `androidMaxAspectRatio: 2.4`. `2` = Custom, AndroidPlayerBuildProgram IL'inden doğrulandı (mode==2 iken max değer kullanılır).
- **Kare hızı:** 60 FPS, vSync kapalı. `Assets/Scripts/Core/AppBootstrap.cs` `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` ile ayarlar; sahneye eklenmez. Başka yerde `targetFrameRate` / `vSyncCount` yazılmamalı.
- **Uygulama yaşam döngüsü (AppBootstrap):** `SceneManager.sceneLoaded` her sahne yüklemesinde `Time.timeScale = 1f` yapar (pause'dan menüye dönünce donma). Bu yüzden hiçbir script `Awake` / `OnEnable` içinde timeScale'i 0'a çekmemeli (sceneLoaded bunlardan sonra çalışır). Gizli `AppLifecycle` objesi (HideAndDontSave + DontDestroyOnLoad) `OnApplicationPause(true)` ve `OnApplicationQuit`'te `PlayerPrefs.Save()` çağırır; Editor'de Play modundan çıkınca kendini yok eder.
- **Swipe girdisi (GameManager):** Eşik = dpi > 0 ise `max(swipeThreshold, dpi × 0.25)`, değilse `max(swipeThreshold, Screen.width × 0.05)`. Dokunuş başladığında `EventSystem.RaycastAll` ile en üstteki hedef bir `Selectable` içindeyse o dokunuş swipe sayılmaz. `IsPointerOverGameObject` kullanılmaz (grid hücreleri ve taşlar da UI Image). Grid üzerinde raycast yakalayan bir Selectable veya tam ekran obje eklenirse swipe bozulur.
- **Game HUD anchor kuralı (şu anki durum):** Üst HUD (Hint, Undo, Joker, Score, Pause, logo, GoalsContainer) yatay konumuna göre (0,1) / (0.5,1) / (1,1). Grid'le görsel grup oluşturan objeler (PressureMeter) GridBoard ile aynı anchor (0.5, 0.41666666). Alt panel (AssistantPanel) (0.5,0). Popup'lar (Hypothesis, Pause, Undo) (0.5,0.5) sabit boyut; tam ekran karartma katmanları stretch kalır. Yüzdelik (her iki eksende stretch) anchor yeni HUD objelerinde kullanılmaz. Sonuç: uzun telefonlarda grid ile üst HUD arasındaki boşluk büyür (1080×2400'de ScoreButton–grid 403 birim).
- **HUD kümesinin grid anchor'ı (UYGULANDI, alternatif A):** GoalsContainer, ScoreButton, JokerButton, UndoButton ve HintButton GridBoard ile aynı anchor'da (0.5, 0.41666666); logo ve PauseButton üst kenarda. GoalsContainer 21,2 ve ScoreButton 2 birim aşağı alınarak GoalsContainer × logo örtüşmesi giderildi. Dört safe area boyutunda (1080×1920 / 1080×2114 / 1080×2300 / 1440×1920) taşma ve çakışma yok; HUD–grid boşluğu her ekranda sabit (58 / Score 121).
- ~~**HUD kümesinin grid anchor'ı (hedef karar, UYGULANMADI):**~~ GoalsContainer, ScoreButton, JokerButton, UndoButton ve HintButton'ın GridBoard ile aynı anchor'a (0.5, 0.41666666) alınması kararlaştırıldı; logo ve PauseButton üst kenarda kalır. Uygulanmadı çünkü referansta var olan GoalsContainer × logo 21,2 birimlik örtüşme, GoalsContainer tek başına aşağı alınınca ScoreButton ile 1,9 birim çakışmaya dönüşüyor. Kullanıcının seçmesi bekleniyor: (A) Goals 21,2 aşağı + ScoreButton 2 aşağı, (B) Goals 11 aşağı + logo 10,2 yukarı. İkisi de 1080×1920 / 1080×2114 / 1080×2300 / 1440×1920 safe area boyutlarında çakışmasız.
- **MainMenu menü butonları:** PlayButton, HowToPlayButton, EncyclopediaButton, SettingButton merkez anchor (0.5, 0.5); y = 395 / 121 / -137 / -402. ProfileButton sol üstte (0,1). Uzun ekranlarda grup ortada kalır.
- **LevelMap:** `LevelSelectionPanel/LevelMap` (merkez, 1080×1920) → `MapBG` (stretch, level_selection_arkaplan). Level1–7 butonları `MapBG`'nin çocuğu (LevelMap değil); MapBG LevelMap'i tamamen kapladığı için görsel/işlevsel fark yok — **kabul edildi, düzeltilmeyecek**.
- **Arka planlar:** `Canvas/Background` ve panel `BG` çocuklarında AspectRatioFitter Envelope Parent; oran = sprite genişlik/yükseklik (arkaplan ve Registration_arkaplan: 941×1672 → 0.56279904). ARF anchor/pozisyon/boyutu çalışma zamanında sürdüğü için YAML'daki rect değerleri (özellikle kapalı panellerde) bayat görünebilir; bu normaldir. Panelin kendi Image'ı: enabled, sprite None, alpha 0, raycast açık (tıklamayı arkaya geçirmemek için).
- **GridManager.mergeParticlePrefab:** None. Overlay canvas'ta particle görünmediği için efekt kapalı; `GridManager.cs:403` null kontrolü var.
- **AudioManager proxy:** Sahne yeniden yüklenince oluşan kopya yok edilmez, `IsProxy` olur: AudioSource'larını durdurup susturur, müzik başlatmaz, DontDestroyOnLoad'a girmez, Instance'ı değiştirmez; `UpdateMusicState` / `PlaySFX` / `PlayButtonSound` çağrılarını gerçek Instance'a iletir. MainMenu butonları (32 çağrı) sahnedeki kopyayı hedeflemeye devam eder. AudioManager'a yeni public metot eklenirse proxy iletimi de eklenmeli.
- **Renk uzayı Linear:** Görsellerdeki çok düşük alfalı (ör. 2/255) katmanlar Linear uzayda koyu arka plan üzerinde gri kutu olarak görünür. Yeni UI görsellerinde şeffaf alanların alfası tam 0 olmalı.
- **Değer yuvarlama:** Anchor dönüşümlerinde tam sayıya 0,01 birimden yakın değerler tam sayıya yuvarlanır (referans görünüm hatası ≤ 0,004 birim).
- **SafeArea:** `Assets/Scripts/Core/SafeArea.cs`, Canvas altındaki tam ekran bir kök objeye eklenir ve HUD onun çocuğu olur; Background ve tam ekran karartma katmanları dışarıda kalır. Güvenli alan, ekran boyutu ve yön değişmedikçe RectTransform'a yazmaz.
- **Doğrulama:** `Assets/Editor/NexumValidation.cs`. Build Settings sahneleri + `Assets/Prefabs` için missing script, kopuk (Missing) referans, proje scriptlerinde null GameObject referansı ve RectTransform NaN/Infinity kontrolü; hiçbir şeyi kaydetmez. Sahne/prefab değişikliklerinden sonra çalıştır:
  `"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -nographics -quit -projectPath C:\Projects\NEXUM -executeMethod NexumValidation.Run -logFile <log>`
  Log'da `NEXUM_VALIDATION: OK|FAIL`, `NEXUM_VALIDATION_WARN`, `error CS` aranır. Beklenen: OK, 0 uyarı.
  - **İstisnalar:** dosya başındaki `AllowedNullFields` (bilerek boş alanlar, `Tip.alan`; şu an `GridManager.mergeParticlePrefab`) ve `SceneLoadMethods` (sahne yükleyen metotlar, `Tip.Metot`). Yeni bilerek-boş alan veya sahne yükleyen metot eklenirse bu listeler güncellenmeli.
  - **UnityEvent FAIL:** hedef null/Missing; metot hedef tipte dinleyici moduna uygun parametreyle yok.
  - **UnityEvent WARN (sonucu FAIL yapmaz):** aynı olayda birden fazla sahne yükleme; ScrollRect.onValueChanged'e bağlı ses; script'inde `DontDestroyOnLoad` geçen, kendi tipinden static `Instance` taşıyan ve `IsProxy` üyesi olmayan singleton'ı hedefleme.
  - **Diğer WARN'lar:** ElementData'da boş/çok kısa metin alanları (temel elementlerde boş recipe hariç); ses import kuralına uymayan dosyalar; sahnede kaydırılmış kaydedilmiş ScrollRect içeriği.
  - **FAIL:** ProjectSettings'te çözülemeyen asset referansı (silinen ikon gibi).
- **GridBoard (Game.unity):** Sabit **900×900**, nokta anchor (0.5, 0.41666666), pivot (0.5, 0.5), AspectRatioFitter kapalı (`m_Enabled: 0`). GridLayoutGroup: hücre 200, boşluk 20, dolgu 20 → 4×200 + 3×20 + 2×20 = 900. Bu toplam değişirse tahta boyutu da güncellenmeli.
- **Anchor dönüşümleri:** Kenara anchor'lanıp büyük offset'le konumlanan objeler, referans (1080×1920) görünümü koruyan hesapla merkez / üst-orta anchor'a çevrilir (`yeni_pos = eski_anchor × ebeveyn_boyutu + eski_pos − yeni_anchor × ebeveyn_boyutu`).
- **DOTween göreli tween'ler:** `DOShakePosition` / `DOPunchScale` gibi başlangıç değerini yakalayan tween'lerden önce hedefte `DOKill(true)` çağrılır (üst üste binince kalıcı kayma olmasın).
- **IL2CPP / ARM64 / package name:** Kullanıcı Unity Editor'de ayarlayacak; YAML ile dokunulmaz.

- **Terminal komutları:** Kullanıcıya verilen tüm komutlar **Windows PowerShell** uyumlu olmalı (`&` çağrı operatörü, Windows yolları, `$env:` değişkenleri). Bash/POSIX sözdizimi kullanma. Batch mode Unity komutlarının sonuna `| Out-Null` eklenir.
- **Satır sonları (.gitattributes):** Depo kökündeki `.gitattributes` `* text=auto eol=lf` ile hem depoyu hem çalışma kopyasını LF'e sabitler; `core.autocrlf` ayarı artık sonucu değiştirmez. Unity YAML ve kod uzantıları metin, görsel/font/ses/dll/apk ikili olarak işaretlidir. (Sahne birleştirme için UnityYAMLMerge ayrıca kurulabilir; şu an yapılandırılmadı.)
- **Ses import kuralı:** 10 sn üstü → Streaming + Vorbis + quality ~0.7 + loadInBackground; 3 sn altı → Decompress On Load + ADPCM + forceToMono; arası → Compressed In Memory + Vorbis. Süre MP3 başlığından tahmin edilmez, Unity'nin `AudioClip.length` değeri esas alınır (validator bunu kontrol eder). Yalnızca mevcut alanların değeri değiştirilir; yeni platform bloğu Editor işidir.
- **İkinci şans (quiz):** Hak, quiz paneli açıldığı anda tüketilir (`UIManager.ShowQuizPanel` içinde `GridManager.Instance.hasUsedRevive = true`). Yanlış cevapta oyun sonu ekranına dönülür ve buton bir daha görünmez; doğru cevapta oyun devam eder. Bayrak GridManager örneğinde tutulur, sahne yeniden yüklenince (Yeniden Başlat / Sonraki Bölüm / menüden bölüme giriş) sıfırlanır.
- **Panel kaydırması:** `MainMenuManager.SwitchPanel`, açılan panelin altındaki tüm ScrollRect'leri en üste alır (`LayoutRebuilder.ForceRebuildLayoutImmediate` + `Canvas.ForceUpdateCanvases` sonrası). Yeni ScrollRect'li panel eklenirse bu davranış kendiliğinden geçerli olur; sahnede içerik kaydırılmış kaydedilmişse validator uyarır.
- **Quiz paneli yerleşimi:** QuizPanel tam ekran karartma (stretch) kalır; içindeki çerçeve 900×1100 merkezde, soru 800×300 @ y=330 (TMP otomatik boyut 34-50), şıklar y = 60 / -80 / -220 / -360. Geri bildirim sırasında soru metni koda göre ortaya alınır (800×700 @ y=0) ve quiz tekrar açılınca eski yerine döner.
- **Doku import kuralı:** Kullanım boyutu hesaplanırken **çalışma zamanında atanan sprite'lar da** dikkate alınır (ör. avatarlar kayıt ekranında 663×724 gösteriliyor; yalnızca sahnedeki Image boyutuna bakmak yanıltır). `maxTextureSize` = dokunun en büyük kullanım boyutunun 1,5 katından büyük en küçük 2'nin kuvveti (en az 256, en çok 2048, kaynak boyutu aşmadan). UI dokularında mipmap kapalı, Read/Write kapalı. Değişiklik yalnızca `TextureImporter.maxTextureSize` ve `DefaultTexturePlatform` bloğunda yapılır; Android blokları `overridden: 0` olduğu için Default değeri geçerlidir. Yeni platform bloğu eklemek Editor işidir.
- **Görsel kuralı (Linear renk uzayı):** Şeffaf alanların alfası tam 0 olmalı. Alfası 1–9 olan geniş katmanlar koyu arka planda gri kutu olarak görünür. Tarama aracı: `scratchpad/AlphaScan.ps1`; temizleme + Lanczos küçültme: `scratchpad/CleanSprite.ps1` (spriteMode Single ve border yoksa boyut değiştirilebilir).
- **Resources kuralı:** `Assets/Resources` altında yalnızca kodla yüklenenler durur (şu an Elements ve DOTweenSettings). Yeni bir şey eklenirse `NexumValidation.AllowedResources` listesi de güncellenmeli; aksi hâlde validator uyarı verir. Görseller `Assets/Art`, sesler `Assets/Audio`, ScriptableObject verileri `Assets/Data` altına konur.
- **TMP font önbelleği:** `Assets/TextMesh Pro` altındaki font asset'lerinde (özellikle `LiberationSans SDF - Fallback.asset`) yalnızca dinamik atlas / glif önbelleği değişmişse bu sorun sayılmaz; sorulmadan `git restore` ile geri alınır ve raporda tek satırla belirtilir. Bu dosyalarda başka türden bir değişiklik olursa durulup kullanıcıya sorulur.
- **Boyut tahminleri:** Build boyutu tahminleri yalnızca gerçekten referanssız asset'ler ve doku import ayarları üzerinden yapılır. Sahnede kullanılan bir asset klasörü değişse de build'e girmeye devam eder.

## YAML düzenleme kuralları
- Düzenlemeden önce Unity Editor'ün **kapalı** olduğunu kullanıcıya sorarak teyit et.
- Sadece ilgili alanların **değerlerini** değiştir. Blok ekleme / silme yok; `fileID` ve `guid` değerlerine dokunma.
- Obje silme YAML ile yapılmaz; Editor'de kullanıcı yapar.
- Satır sonlarını ve girintiyi koru. Depoda her şey LF; çalışma kopyasında `core.autocrlf=true` yüzünden dosyalar CRLF olabilir (dal değiştirdikten sonra sahneler de CRLF olur). Düzenleme araçları satır sonunu satır satır korumalı, karşılaştırma yaparken `
` kırpmalı.
- Satır numarası + beklenen eski içerik doğrulamasıyla düzenle; eşleşmezse hiçbir şey yazma.
- Her düzenlemeden sonra `git diff` ile yalnızca hedeflenen satırların değiştiğini doğrula; beklenmeyen satır varsa geri al ve kullanıcıya bildir.
- Float değerleri Unity biçiminde yaz (float32'nin en kısa geri-dönüşümlü gösterimi, örn. `0.41666666`).

## Kapsam (5. turda güncellendi)
- **Kapsam içi:** quiz / ikinci şans akışı ve ansiklopedi veri alanları (ElementData'nın görünen metinleri). Bunlar dışında GridManager ve LevelManager'a dokunulmaz.
- ElementData'da **puan, silme maliyeti ve GridManager birleşme tablosu** hâlâ kapsam dışıdır.

## Kapsam dışı (bu temizlik çalışmasında yapılmayacak)
- Spawn kuralı (her geçerli hamlede spawn)
- Taşların duvara kadar kayması
- Aynı element tarifleri (C+C, N+N, Na+Na, Cl+Cl, Ca+Ca)
- Kimya tarifi düzeltmesi (CaO + O₂ → CaCO₃, kopya CO + O tarifi)
- Kazanma ekranının tekrar tetiklenmesi
- Undo sayaçları (hedef sayacı, skor, entropi geri alma, boş hamle snapshot'ları)
- Joker / revive silme hatası (DOKill ile iptal olan Destroy)
- Entropi uyarısının yanlış metne yazılması
- TotalAccidents / flashcard / mentor ipucu özellikleri
- `Shift()` refactor'ü
