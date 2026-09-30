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
    Atlases/         Sprite Atlas V2 asset'leri (NexumAtlasBuilder üretir)
    Avatars/         Mentor portreleri
    Figures/         UI görselleri
    Icons/           Uygulama ikonu görselleri (Tools/generate_icons.py üretir)
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
Tools/               Unity dışı araçlar
  generate_icons.py  Uygulama ikonu + mağaza görselleri üreteci (Python + Pillow)
  store/             Mağaza görselleri; build'e girmez (önizlemeler git'te yok sayılır)
docs/                Belgeler (privacy-policy GitHub Pages ile yayınlanır)
  STATUS.md            Projenin güncel durumu ve açık maddeler — yeni oturumda ilk okunacak
  privacy-policy.html  Gizlilik politikası (TR + EN)
  RELEASE_CHECKLIST.md Yayın kontrol listesi
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
| `Editor/NexumAtlasBuilder` | Editör aracı: Sprite Atlas V2 asset'lerini üretir (tekrar çalıştırılabilir) |
| `Editor/NexumBuild` | Editör aracı: batch mode Android APK + AAB build'i ve BuildReport ölçümü |
| `Editor/NexumIconSetup` | Editör aracı: Android ikon slotlarını PlayerSettings API'siyle doldurur |
| `Editor/NexumSplashSetup` | Editör aracı: açılış ekranına NEXUM logosunu ekler |
| `Editor/NexumScreenshot` | Editör aracı: Play modunda Game view'ı PNG'ye kaydeder (mağaza görüntüleri) |
| `Core/AppBootstrap` | Sahneye eklenmeden çalışır: 60 FPS, sahne yüklenince timeScale=1, arka planda PlayerPrefs.Save |
| `Core/SafeArea` | RectTransform'u Screen.safeArea'ya göre anchor'lar (sahneye Editor'de eklenir) |
| `Core/BackButtonHandler` | Sahneye eklenmez: Android geri tuşunu (Escape) tüm ekranlarda yönetir |

## Durum notu
Projenin bugünkü hâli, tamamlananlar ve açık maddeler (teknik / kapsam dışı oyun mantığı / yayın) **`docs/STATUS.md`** içindedir. Yeni bir oturuma bu dosyayla başlanır; ara verilip dönüldüğünde önce orası okunur ve tur sonunda güncellenir.

## Kesin kurallar
- **Oyun hissini değiştiren değişiklikler önce onaylanır.** GridManager, LevelManager ve diğer oyun mantığı artık **kapsam içindedir**; ama spawn kuralı, kayma davranışı, denge, tarif tabloları ve puanlama gibi oyunun hissini değiştiren her şey için önce **analiz + seçenekler** sunulur, kullanıcı seçer, sonra uygulanır. Hata düzeltmesi (sayaç şişmesi, çift tetikleme gibi) bu onaydan muaf değildir ama seçenek listesi kısa tutulabilir.
- **Sahne yapısı değişiklikleri Editor scriptiyle yapılır.** Obje/bileşen ekleme, batch mode'da çalışan **idempotent UIBuilder scriptleriyle** (Assets/Editor) yapılır; script iki kez çalıştırıldığında aynı sonucu vermeli. **Elle YAML'a obje veya bileşen bloğu eklemek hâlâ yasaktır**; mevcut alanların değerini değiştirmek serbesttir. Bu altyapı **Faz 0 / Adım 5**'te kurulacak; o zamana kadar yapı değişiklikleri kullanıcının Editor işi olarak yazılır.
- **Her adım kabul kriterini ölçümle gösterir.** "Muhtemelen düzeldi" kabul edilmez: düzeltmenin çalıştığı PlayMode ölçümü, validator çıktısı veya test sonucuyla kanıtlanır. Kanıtlanamıyorsa nedeni açıkça yazılır.
- `.unity`, `.prefab` ve `.asset` dosyalarını (ProjectSettings dahil) düzenlemeden önce **Unity Editor'ün kapalı olduğunu kullanıcıya sorarak teyit et**.
- **Hiçbir dosyayı silme**; silinmesi gerekenleri listele.
- Dosya taşırken `.meta` dosyasını da birlikte taşı (GUID korunmalı).
- Her adımı **ayrı commit** olarak kaydet.

### Adım şablonu (her iş için standart)
1. `main`'den dal aç (`git switch -c <konu>`).
2. **Başlangıç durumu:** validator + PlayMode testleri.
3. Uygula (her alt adım ayrı commit).
4. **4 canvas boyutunda kontrol:** 1080×1920, ~1080×2114, ~1080×2300, 1440×1920 — taşma ve çakışma.
5. Validator + PlayMode testleri yeniden; ikisi de temiz olmalı.
6. Push + PR. **Görsel etkisi olmayan adımlar** (belge, ayar, ölçüm aracı) kurallara uyuyorsa kendin birleştirilir; **ekranı veya oyun hissini etkileyen adımlarda** kullanıcı cihazda test edip karar verir.

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
  - **Bölüm çözülebilirliği (FAIL):** Game sahnesindeki `GridManager.recipes` ve `LevelManager.levels` okunur; her bölüm için spawn havuzundan (ağırlık > 0) başlayıp tarifler kapanana kadar uygulanır. Bir bölümün hedefi üretilemiyorsa FAIL. Aynı anahtardan ikinci tarif WARN (GridManager'da ilk tanım geçerli).
  - **Doku / yayın WARN'ları (EH):** atlasa giren bir dokunun importer sıkıştırması açıksa (çift sıkıştırma); Android Target API Level "Automatic" ise; bir dokunun `maxTextureSize` değeri ekrandaki en büyük kullanımının 1,5 katının altındaysa. Sonuncusu yalnızca **ayarın** sınırladığı durumu bildirir (kaynak görselin kendisi küçükse uyarmaz) ve 13 deney föyü görselini muaf tutar. Çalışma zamanında atanan sprite'lar ile prefab köklerinin (taş, beher, ansiklopedi kartı) kullanım boyutları `RuntimeSpriteUsage` tablosunda elle tanımlıdır; yeni böyle bir kullanım eklenirse tabloya da eklenmeli.
  - **Yayın WARN'ları:** `versionName` boş ya da Unity varsayılanı ("0.1") ise; `versionCode` 1'den küçükse; `docs/privacy-policy.html` yoksa.
  - **İkon WARN'ı:** Android Adaptive / Round / Legacy ikon türlerinden birinde boş slot varsa.
  - **Diğer WARN'lar:** ElementData'da boş/çok kısa metin alanları (temel elementlerde boş recipe hariç); ses import kuralına uymayan dosyalar; sahnede kaydırılmış kaydedilmiş ScrollRect içeriği.
  - **FAIL:** ProjectSettings'te çözülemeyen asset referansı (silinen ikon gibi).
- **GridBoard (Game.unity):** Sabit **900×900**, nokta anchor (0.5, 0.41666666), pivot (0.5, 0.5), AspectRatioFitter kapalı (`m_Enabled: 0`). GridLayoutGroup: hücre 200, boşluk 20, dolgu 20 → 4×200 + 3×20 + 2×20 = 900. Bu toplam değişirse tahta boyutu da güncellenmeli.
- **Anchor dönüşümleri:** Kenara anchor'lanıp büyük offset'le konumlanan objeler, referans (1080×1920) görünümü koruyan hesapla merkez / üst-orta anchor'a çevrilir (`yeni_pos = eski_anchor × ebeveyn_boyutu + eski_pos − yeni_anchor × ebeveyn_boyutu`).
- **DOTween göreli tween'ler:** `DOShakePosition` / `DOPunchScale` gibi başlangıç değerini yakalayan tween'lerden önce hedefte `DOKill(true)` çağrılır (üst üste binince kalıcı kayma olmasın).
- **Uygulama ikonu:** Projede 512×512 kare logo olmadığı için ikon `Tools/generate_icons.py` ile **vektör olarak çizilir** (hiçbir görsel büyütülmez; içeride 4× süperörnekleme + LANCZOS). Tasarım HUD'daki azot karosunu temel alır: yuvarlatılmış koyu mavi karo (#0E3B63 → #0A2949), camgöbeği kenarlık (#35D0F0) ve dış parlama, ortada Orbitron-Bold "N", sol üstte "7", altta "14.007". Renk/metin değişikliği için scriptin başındaki `PARAMS` sözlüğü düzenlenip script yeniden çalıştırılır. Çıktılar `Assets/Art/Icons` altındadır; mağaza görseli `Tools/store/icon_store_512.png` (Assets dışında, build'e girmez). Adaptive ön planın içeriği ortadaki 288×288 güvenli alana sığar (Android maskeleri kenarları kırpar). Slotlar `NexumIconSetup.Apply` ile doldurulur — **ProjectSettings.asset elle düzenlenmez**. Not: `AndroidPlatformIconKind` `UnityEditor.Android.Extensions.dll` içinde olduğu ve Assembly-CSharp-Editor onu referans almadığı için tür `GetSupportedIconKinds` içinden ada göre bulunur; adlar API sürümü taşır ("Adaptive (API 26)", "Round (API 25)", "Legacy").
- **Mağaza görselleri:** `Tools/generate_icons.py` ikonların yanında Play Console görsellerini de üretir. Mağaza ikonu (`Tools/store/icon_store_512.png`) tamamen opak ama **32-bit RGBA** yazılır (Play alfa kanallı PNG istiyor). Öne çıkan görsel (`Tools/store/feature_graphic_1024x500.png`) ikonla aynı dili kullanır; alt başlık `FEATURE` sözlüğünden değiştirilir, içerik kenarlardan en az %6 içeride durur. Bu dosyalar `Assets` dışında olduğu için build'e girmez.
- **Gizlilik ve veri:** Uygulama **hiçbir veriyi dışarı göndermez**: scriptlerde ağ çağrısı yok, `UnityConnectSettings` içinde analytics/ads/crash reporting/purchasing/performance reporting kapalı, üretilen manifestte tek izin `VIBRATE` (INTERNET izni yok). Tüm veriler `PlayerPrefs` ile cihazda kalır. Politika metni `docs/privacy-policy.html` (TR + EN); **veri toplayan bir özellik eklenirse önce bu dosya güncellenmeli**. Yayın adımları `docs/RELEASE_CHECKLIST.md` içinde.
- **Açılış ekranı:** Unity Personal lisansında Unity logosu kaldırılamaz; yanına NEXUM logosu (master ikon, 2 sn) `PlayerSettings.SplashScreen` API'siyle eklendi. Süre ve stil varsayılan.
- **İmzalama:** `androidUseCustomKeystore: 1`, keystore yolu ve alias ProjectSettings'te kayıtlı; **yol bu makineye özeldir**. Keystore dosyası depo dışında, **şifreler ProjectSettings'e yazılmaz** (`keystorePass`/`keyaliasPass` alanları dosyada yok) ve Unity her açılışta yeniden istenir. Bu yüzden **yayın paketi batch mode'da alınmaz**: imzalı AAB Unity Editor'de, `docs/RELEASE_CHECKLIST.md` 1.6'daki adımlarla üretilir. `NexumBuild` ile alınan paketler yalnızca ölçüm içindir.
- **Ekran görüntüleri:** *NEXUM → Ekran Görüntüsü Al (1080×1920)* (`Ctrl+Shift+S`) Play modunda Game view'ı `Tools/store/screenshots/` altına yazar (klasör git'te yok sayılır). Game view farklı çözünürlükteyse konsola uyarı düşer.
- **Android yayın ayarları:** IL2CPP, **sadece ARM64** (`AndroidTargetArchitectures: 2`), **Target API 36** (`AndroidTargetSdkVersion: 36`, artık Automatic değil), Minimum API 22, paket adı `com.hekimsefkan.nexum`. Enum değerleri build sırasında çalışma anında doğrulandı (`mimari=ARM64 (2)`, `targetSdk=AndroidApiLevel36 (36)`). ARMv7 kaldırıldığı için APK'dan 13,37 MiB düştü. Keystore ve imza kullanıcıya ait; YAML ile dokunulmaz.

- **Çekirdek döngü (13. turda karara bağlandı ve uygulandı):**
  - **Kayma:** Taşlar duvara / önlerindeki engele kadar kayar. `Shift()` tek hücrelik taramayı hiçbir taş ilerlemeyene kadar tekrarlar (en fazla 24 tur güvenlik sınırı). **Aynı hamlede üretilen bileşik o hamlede ikinci kez birleşmez** (2048 kuralı; `mergedThisShift` kümesi). Kayan taşın animasyonu tek `DOLocalMove` ile oynatılır (hücre başına ayrı tween yok).
  - **Spawn (melez):** Birleşme olunca her zaman bir taş gelir. Birleşme olmayan hamlelerde `MergelessMovesPerSpawn = 2` — yani **2 boş hamlede bir taş**. Bu kural **her modda** çalışır. Entropi ceza taşı (5 boş hamle) bundan **bağımsızdır** ve yalnızca Normal + Sınav modunda; **serbest modda (`currentGameMode == 2`) ceza taşı gelmez ve basınç sayacı işlemez**.
  - **Kazanma:** `GridManager.hasWon` bayrağı. `CheckWinCondition` her birleşmede çalıştığı için kombo sırasında kazanma ekranı tekrar tetikleniyordu. Bayrak sahne örneğinde durur, sahne yeniden yüklenince sıfırlanır.
  - **Geri alma:** Snapshot artık hedef sayaçlarını, entropiyi (`emptyShiftCount`), melez spawn sayacını (`movesSinceSpawn`) ve `TotalSynthesis`'i de tutar. `ExecuteUndo` önce snapshot skoruna döner, **sonra** `undoCost` düşer (bedel skoru eksiye düşürmez). Hiçbir şeyi değiştirmeyen kaydırmada alınan snapshot geri alınır (yığın şişmesi).
  - **`Shift()` yapısı:** Dört yön için kopya blok yok; `ScanUp/ScanDown/ScanLeft/ScanRight` tabloları + `GetScanOrder` / `GetStepOffset`. Yeni yön veya farklı grid boyutu gelirse yalnızca bu tablolar değişir.
- **Tarif tablosu 21 satır (13. tur):** `N+N→N₂`, `Cl+Cl→Cl₂`, `C+C→C₂` (10 puan) ve tüketicileri `N₂+H₂→NH₃` (Haber-Bosch), `Cl₂+Na→NaCl`, `C₂+O₂→CO` (30 puan). **Na₂ ve Ca₂ bilerek eklenmedi.** Kural: **her yeni bileşiğin en az bir tüketici tarifi olmalı** (çıkmaz sokak yok). Yeni bileşik taşları, ElementData asset'leri ve tarif kayıtları elle YAML ile değil `Assets/Editor/NexumNewCompounds.cs` (idempotent) ile kurulur; metinleri `Assets/Editor/NexumCompoundTexts.cs` yazar.
- **TotalScore:** `Assets/Scripts/Core/TotalScoreService.cs` toplam skorun tek sahibi; bölüm kazanılınca o bölümün skoru eklenir (PlayerPrefs). **Hiçbir ekranda gösterilmiyor** — gösterim kararı Faz 2 profil işinde verilecek. Kalıcı depolama değişirse yalnızca bu sınıfın içi değişir.
- **Çekirdek döngü regresyon testi:** `Assets/Tests/PlayMode/CoreLoopTests.cs` (kazanma bir kez, geri alma sayaçları, duvara kadar kayma, aynı hamlede tekrar birleşmeme, melez spawn sıklığı, serbest modda ceza yok, TotalScore birikimi). `Assets/Tests/PlayMode/ShiftBehaviorTests.cs` kaydırma davranışının parmak izini loglar: **`Shift()` refactor'lerinde önce/sonra imza birebir aynı kalmalı** (davranış bilerek değişiyorsa commit mesajına yeni imza yazılır).
- **Ölçüm aracı:** `Tools/sim_core_loop.py` tarifleri, spawn havuzlarını ve hedefleri `Game.unity`'den okur. `python Tools/sim_core_loop.py [oyun_sayisi] [derinlik]`; `NEXUM_SCENE` ortam değişkeniyle başka bir sahne dosyası (ör. depodan çıkarılmış eski sürüm) okunabilir. Derin bot ileriye bakarken spawn'i yok sayar; **mutlak kazanma oranı değil, senaryolar arası fark anlamlıdır**.
- **Terminal komutları:** Kullanıcıya verilen tüm komutlar **Windows PowerShell** uyumlu olmalı (`&` çağrı operatörü, Windows yolları, `$env:` değişkenleri). Bash/POSIX sözdizimi kullanma. Batch mode Unity komutlarının sonuna `| Out-Null` eklenir.
- **Satır sonları (.gitattributes):** Depo kökündeki `.gitattributes` `* text=auto eol=lf` ile hem depoyu hem çalışma kopyasını LF'e sabitler; `core.autocrlf` ayarı artık sonucu değiştirmez. Unity YAML ve kod uzantıları metin, görsel/font/ses/dll/apk ikili olarak işaretlidir. (Sahne birleştirme için UnityYAMLMerge ayrıca kurulabilir; şu an yapılandırılmadı.)
- **Ses import kuralı:** 10 sn üstü → Streaming + Vorbis + quality ~0.7 + loadInBackground; 3 sn altı → Decompress On Load + ADPCM + forceToMono; arası → Compressed In Memory + Vorbis. Süre MP3 başlığından tahmin edilmez, Unity'nin `AudioClip.length` değeri esas alınır (validator bunu kontrol eder). Yalnızca mevcut alanların değeri değiştirilir; yeni platform bloğu Editor işidir.
- **İkinci şans (quiz):** Hak, quiz paneli açıldığı anda tüketilir (`UIManager.ShowQuizPanel` içinde `GridManager.Instance.hasUsedRevive = true`). Yanlış cevapta oyun sonu ekranına dönülür ve buton bir daha görünmez; doğru cevapta oyun devam eder. Bayrak GridManager örneğinde tutulur, sahne yeniden yüklenince (Yeniden Başlat / Sonraki Bölüm / menüden bölüme giriş) sıfırlanır.
- **Geri tuşu (Android):** `Assets/Scripts/Core/BackButtonHandler.cs` sahneye eklenmez, `RuntimeInitializeOnLoadMethod` ile gizli kalıcı obje olarak kurulur. Ana menüde açık panel kapanır (iç içe panelde önce en üstteki: profil e-posta pop-up → ansiklopedi detayı → nasıl oynanır pop-up), kökte 2 saniyelik "çıkmak için tekrar bas" (Android Toast + titreşim). Kayıt ekranında kaydı iptal etmez, yalnızca mentor kartını kapatır. Oyunda: quiz → oyun sonu ekranı, oyun sonu/bölüm tamamlandı/deney föyü → ana menü, undo/hipotez/ipucu → kapanır, modal yoksa duraklat açılır/kapanır. Karar mantığı `HandleBackPress()` içinde toplanır; PlayMode testi bunu çağırarak doğrular. **Yeni panel eklenirse bu zincire de eklenmeli.** `MainMenuManager.IsSwitching` panel geçişi sürerken hem geri tuşunu hem yeni geçişi bloklar (panel ölçek 0'da kalmasın).
- **Buton sesi:** `UIManager.PlayButtonSound` kare koruması taşır (aynı karede ikinci çağrı yok sayılır). Bu sayede sahne OnClick listesinde sesi olan ve olmayan butonlar birlikte çalışır: `TogglePause`, `GoToMainMenu`, `RestartGame` ve `NextLevel` sesi koddan çalar, çift ses olmaz. Quiz şıkları bilerek sessiz (doğru/yanlış sesi var). `AudioManager.PlaySFX` "SfxOn" ayarına baktığı için ses kapalıyken hiçbiri çalmaz.
- **Kayıt doğrulaması:** İsim `Trim` edilir, 2–20 karakter; hata durumunda uyarı metni kırmızıya döner, kutu `DOKill(true)` sonrası sarsılır, `errorClip` çalar. Mentor seçilmemişse "Önce bir mentor seç". **İki ayrı uyarı alanı var:** isim ve mentor hataları `nameWarningText` (isim kutusunun altında, 11. turda eklendi), e-posta hataları `emailWarningText`. E-posta **isteğe bağlıdır** (tasarım kararı).
- **Kombo yazısı:** `UIManager.ShowComboText` yazıyı birleşmenin olduğu yerde değil, grid üstü (290) ile ScoreButton altı (411) arasındaki banda sabitler (merkez 328, yükselme 45 birim); yatayda birleşmenin sütununu korur ama ekran dışına taşmaz.
- **Panel arka planları (PreserveAspect):** `HowToPlayPanel` ve `SettingsPanel/GlassBackground` arka plan görselini tam ekran stretch kökte taşıyor ve AspectRatioFitter'ları yok; uzun ekranlarda görsel dikeyde %21'e kadar geriliyordu. Çözüm: `Image.PreserveAspect` açık (gerilme yerine içe sığdırma). **AspectRatioFitter (Envelope Parent) bilerek kullanılmadı:** bu iki görsel çerçeve tipi olduğu için Envelope Parent kenarlarındaki çerçeveyi kırpardı. Yeni panel arka planı eklenirken aynı ayrım geçerli: dolgu/doku görselleri ARF Envelope ile, çerçeveli görseller PreserveAspect ile kullanılır.
- **Girdi kilidi (modal paneller):** `GameManager.Update` ilk satırda `IsInputBlocked()` çağırır; true ise swipe ve klavye girdisi hiç işlenmez. Kilit iki koşuldan biriyle açılır: `GridManager.Instance == null` ya da `GridManager.Instance.enabled == false` (tutorial), veya `UIManager.Instance.IsModalPanelOpen`. Modal panel listesi `UIManager.IsModalPanelOpen` içinde: tutorial, pause, quiz, gameOver, win, hypothesis, undo. `assistantPanel` ve `focusPanel` bilerek dışarıda (ipucu oyunu durdurmuyor). **Yeni tam ekran panel eklenirse bu listeye de eklenmeli.** Özellik her karede çalıştığı için arama yapmaz, yalnızca `activeInHierarchy` okur. Regresyon testi: `Assets/Tests/PlayMode/InputLockTests.cs`.
- **Sprite Atlas (V2):** `EditorSettings.spritePackerMode = SpriteAtlasV2` (5; değer çalışma anında enum adıyla doğrulanır). Atlas'lar `Assets/Art/Atlases` altında, `Assets/Editor/NexumAtlasBuilder.cs` ile üretilir (menüden ya da `-executeMethod NexumAtlasBuilder.Run`). İki atlas: `NexumMenuUI` (menü/profil/ayarlar/kayıt UI'ı) ve `NexumGameUI` (oyun HUD'u + taş görselleri). Ayarlar V2'de asset'te değil `.meta` içindeki `SpriteAtlasImporter`'da tutulur: padding 4, rotation kapalı, tight packing kapalı, mipmap kapalı, max 2048, Include in Build açık. Atlas'a alınmayanlar: tam ekran arka planlar, büyük paneller, 9 avatar ve tutorial molekül görselleri (aynı anda tek görünüyorlar; draw call kazancı yok, bellek/boyut maliyeti var). **Yeni sprite eklenince listeye eklenip script yeniden çalıştırılmalı.** Ölçüm (7. turda tam veri build'iyle): atlas'lı 49.480.340 bayt, atlas'sız 49.490.235 bayt → **boyut etkisi ~0**; kazanç draw call tarafında. Atlas'a giren dokuların importer formatı **Uncompressed** olmalı (çift sıkıştırma olmasın); validator bunu kontrol eder.
- **Build ölçümü:** `Assets/Editor/NexumBuild.cs` (`-executeMethod NexumBuild.BuildAndroidApk`) hem `Builds/NEXUM_polish8.apk` hem `Builds/NEXUM_polish8.aab` üretir (yayın paketi AAB'dir). Player ayarlarını **değiştirmez**, yalnızca okur ve loglar. `Builds/` git'te yok sayılır. Kategori dağılımı ve en büyük asset'ler için Unity'nin kendi "Build Report" bölümü (editor log) okunur; `BuildReport.packedAssets` artımlı build'de boş gelir.
- **CaCO₃ tarifi:** `CaO + CO₂ → CaCO₃` (7. turda düzeltildi; önceki `CaO + O₂` kimyasal olarak yanlıştı). Yinelenen `CO + O → CO₂` tarifi kaldırıldı, tablo 15 satır. `CaCO3_Data.recipe` metni de eşlendi. Tarif tablosunun geri kalanı hâlâ kapsam dışı.
- **LevelCompletePanel:** başlık ve iki buton dikeyde ortalı (y = 199 / 49 / −159). Üçü de merkez anchor'lı, grup yarı yüksekliği 224.
- **Profil istatistik kartları:** "TOPLAM SENTEZ" = `TotalSynthesis` (GridManager yazıyor). "AÇILAN BÖLÜM" = `MaxLevelUnlocked` (etiket 8. turda veriye uyduruldu). "QUİZ BAŞARISI" = `QuizCorrect / QuizAttempts`; hiç quiz çözülmediyse "—" yazılır. "LABORATUVAR KAZASI" = `TotalAccidents`; `UIManager.ShowGameOver` oyun başına **bir kez** artırır (`accidentCounted` bayrağı), böylece ikinci şans quizi yanlış cevaplanıp oyun sonu ekranı tekrar açıldığında aynı oyun iki kez sayılmaz. Bayrak sahne yüklenince sıfırlanır.
- **Metin uzunluğu kuralı (%85):** Oyun içi metinler kutusunun **%85'ini geçmez**. Gerekçe: Faz 6'da İngilizce çeviri ve büyük yazı boyutu seçeneği gelecek, ikisi de metni uzatır; bugün tam dolan kutu o zaman taşar. Yeni metin eklenirken ya da değiştirilirken ilgili sığma testi çalıştırılır.
- **Tarif ipuçları:** 21 tarifin `scientificHint` alanı dolu. Tek kaynak `Assets/Editor/NexumCompoundTexts.cs` (idempotent; menü: *NEXUM → Kimya Metinlerini Yaz*, batch: `-executeMethod NexumCompoundTexts.Run`); script sahnede karşılığı olmayan anahtarı ve tabloda karşılığı olmayan tarifi hata olarak bildirir. Metin `GridManager.ActivateHint` içinde alt bilgi satırıyla birleştirilip `HintMessageText` (807×350, font 35) kutusunda gösterilir. **Sınır ölçümle korunuyor:** `Assets/Tests/PlayMode/HintFitTests.cs` her tarifin metnini gerçek kutuda TMP'ye hesaplatır; **%85'i aşan metin testi düşürür** (sığıyor olsa bile). Yükseklik satır sayısına göre basamaklı: ana metin 2 satır = 268 birim (%77), 3 satır = 309 birim (%88). Yani kural pratikte **ana metin en fazla 2 satır** demek; 13. turda 21 metnin hepsi %77'ye indirildi. Karakter sınırı sabit değil (satır kırılmasına bağlı), ama ~80 karakter güvenli.
- **Ansiklopedi detay satırı:** "Formül:" değil **"Sentez:"**; tarifi boş olan temel elementlerde "Sentez: Temel element" yazılır (`EncyclopediaManager.OpenDetailPanel`). ElementData'daki `recipe` alanı tarif metnidir, formül değil.
- **ContentSizeFitter kuralı:** Boyutu `ContentSizeFitter` tarafından sürülen RectTransform'larda **`LayoutRebuilder.ForceRebuildLayoutImmediate` kullanma**. Gerekiyorsa `LayoutRebuilder.MarkLayoutForRebuild` ya da bir kare bekleyip (coroutine, `yield return null`) doğrudan değer yazma tercih edilir.
- **Panel kaydırması:** `MainMenuManager.SwitchPanel`, açılan panel için `ResetScrollPositionsNextFrame` coroutine'ini başlatır: iki kare boyunca `StopMovement()` + `content.anchoredPosition = (0,0)`. `normalizedPosition` ataması KULLANILMAZ — panel aktif edildiği karede ScrollRect'in viewport/content sınırları güncel olmadığı için içerik dibe kayıyordu (5. turda ölçüldü: profil 897, ansiklopedi 1170 birim). İçerik pivotu üstte olduğu için `anchoredPosition.y = 0` her ekranda en üst demektir ve içerik boyutundan bağımsızdır. Sahnede içerik kaydırılmış kaydedilmişse validator uyarır.
- **Panel regresyon testi:** `Assets/Tests/PlayMode/PanelScrollTests.cs` panelleri 5'er kez açıp kapatır; içerik yüksekliği, kart sayısı ve kaydırma konumu sabit kalmalı. Panel / ScrollRect / layout değişikliklerinden sonra çalıştırılır (`-batchmode -runTests -testPlatform PlayMode`). Not: PlayMode testi `ProjectSettings.asset` içindeki `runInBackground` değerini 1 yapar; bu istem dışı değişiklik commit edilmeden `git restore` ile geri alınır.
- **Profil paneli yerleşimi:** Content (VerticalLayoutGroup, spacing 40, UpperCenter) → HeroCard 1063×1536 + StatsSection 884×1400; toplam 2976. StatsSection içinde: `SectionFrame` (stretch, Registration_cartblue, beyaz alfa 0.6, raycast kapalı), `StatsHeaderText` (860×110 @ y −24, Orbitron SemiBold 36, #8FD8FF), `StatsGrid` (884×1224 @ y −154; hücre 430×600, boşluk 24, 2 sütun). Kart başlıkları 380×85 font 36, değerler 400×70 font 64. İçerik viewport'tan ~1056 birim uzun olduğu için kaydırma gereklidir.
- **Quiz paneli yerleşimi:** QuizPanel tam ekran karartma (stretch) kalır; içindeki çerçeve 900×1100 merkezde, soru 800×300 @ y=330 (TMP otomatik boyut 34-50), şıklar y = 60 / -80 / -220 / -360. Geri bildirim sırasında soru metni koda göre ortaya alınır (800×700 @ y=0) ve quiz tekrar açılınca eski yerine döner.
- **Doku import kuralı:** Kullanım boyutu hesaplanırken **çalışma zamanında atanan sprite'lar da** dikkate alınır (ör. avatarlar kayıt ekranında 663×724 gösteriliyor; yalnızca sahnedeki Image boyutuna bakmak yanıltır). `maxTextureSize` = dokunun en büyük kullanım boyutunun 1,5 katından büyük en küçük 2'nin kuvveti (en az 256, en çok 2048, kaynak boyutu aşmadan). UI dokularında mipmap kapalı, Read/Write kapalı. Değişiklik yalnızca `TextureImporter.maxTextureSize` ve `DefaultTexturePlatform` bloğunda yapılır; Android blokları `overridden: 0` olduğu için Default değeri geçerlidir. Yeni platform bloğu eklemek Editor işidir. **Tekil istisna:** 13 deney föyü molekül/formül görseli 512'de tutulur (kural 1024 derdi); 400 birimde çiziliyorlar, 1440 genişlikte bile 533 piksel, yani hâlâ küçültme. APK'da ~3,9 MiB kazandırdı. Bulanık görünürlerse tek değer değişikliğiyle 1024'e dönülür; validator'daki bulanıklık kontrolünde bu 13 dosya muaftır.
- **Görsel kuralı (Linear renk uzayı):** Şeffaf alanların alfası tam 0 olmalı. Alfası 1–9 olan geniş katmanlar koyu arka planda gri kutu olarak görünür. Tarama aracı: `scratchpad/AlphaScan.ps1`; temizleme + Lanczos küçültme: `scratchpad/CleanSprite.ps1` (spriteMode Single ve border yoksa boyut değiştirilebilir).
- **Resources kuralı:** `Assets/Resources` altında yalnızca kodla yüklenenler durur (şu an Elements ve DOTweenSettings). Yeni bir şey eklenirse `NexumValidation.AllowedResources` listesi de güncellenmeli; aksi hâlde validator uyarı verir. Görseller `Assets/Art`, sesler `Assets/Audio`, ScriptableObject verileri `Assets/Data` altına konur.
- **TMP font önbelleği:** `Assets/TextMesh Pro` altındaki font asset'lerinde (özellikle `LiberationSans SDF - Fallback.asset`) yalnızca dinamik atlas / glif önbelleği değişmişse bu sorun sayılmaz; sorulmadan `git restore` ile geri alınır ve raporda tek satırla belirtilir. Bu dosyalarda başka türden bir değişiklik olursa durulup kullanıcıya sorulur.
- **Boyut tahminleri:** Build boyutu tahminleri yalnızca gerçekten referanssız asset'ler ve doku import ayarları üzerinden yapılır. Sahnede kullanılan bir asset klasörü değişse de build'e girmeye devam eder.

## YAML düzenleme kuralları
- Düzenlemeden önce Unity Editor'ün **kapalı** olduğunu kullanıcıya sorarak teyit et.
- Sadece ilgili alanların **değerlerini** değiştir. Elle blok ekleme / silme yok; `fileID` ve `guid` değerlerine dokunma. (Mevcut bir bileşene tek satırlık referans alanı yazmak, hedef obje zaten sahnedeyse serbesttir.)
- Obje ve bileşen ekleme/silme YAML ile yapılmaz: **Faz 0 / Adım 5'ten itibaren idempotent UIBuilder Editor scriptiyle**, o zamana kadar kullanıcının Editor işi olarak.
- Satır sonlarını ve girintiyi koru. Depoda her şey LF; çalışma kopyasında `core.autocrlf=true` yüzünden dosyalar CRLF olabilir (dal değiştirdikten sonra sahneler de CRLF olur). Düzenleme araçları satır sonunu satır satır korumalı, karşılaştırma yaparken `
` kırpmalı.
- Satır numarası + beklenen eski içerik doğrulamasıyla düzenle; eşleşmezse hiçbir şey yazma.
- Her düzenlemeden sonra `git diff` ile yalnızca hedeflenen satırların değiştiğini doğrula; beklenmeyen satır varsa geri al ve kullanıcıya bildir.
- Float değerleri Unity biçiminde yaz (float32'nin en kısa geri-dönüşümlü gösterimi, örn. `0.41666666`).

## Kapsam (12. turda güncellendi — ürün dönemi)
- **Her şey kapsam içidir:** GridManager, LevelManager, tarif tabloları, spawn ağırlıkları, puanlama, ElementData alanları ve sahne yapısı.
- **Kapsam kuralı yerine onay kuralı:** oyunun hissini değiştiren değişiklikler önce analiz + seçenek olarak sunulur, kullanıcı onaylar, sonra uygulanır (bkz. Kesin kurallar).
- Değişmeyenler: keystore / imza / şifreler (dokunulmaz), Unity Editor kapalıyken çalışma, dosya silmeme, `.meta` ile taşıma.

## Bilinen durumlar (sonraki turlarda ele alınacak)
- **Arka plan ve avatar çözünürlüğü:** Bazı görsellerin kaynağı ekrandaki kullanımın 1,5 katından küçük (avatarlar 1024 kaynak / 724 birim kullanım, `arkaplan` 941×1672, `grid.png` 783, `gamepanel` 1536). Import ayarıyla çözülmez, daha büyük kaynak görsel gerekir; validator bu durumda bilerek uyarmaz.
- **Beher etiketi kabı aşıyor:** `BeakerGoalPrefab`'ın `AmountText` kutusu 120×80 ama TMP iki satır için 91 birim istiyor; etiket `GoalsContainer`'ın 22 birim altına taşıyor. Skor göstergesi yatayda kaydırılarak çakışma giderildi, prefab'a dokunulmadı.
- **Yayın öncesi kalanlar:** release keystore ve imza (kullanıcıya ait; `ProjectSettings` içindeki `AndroidKeystoreName` / `AndroidKeyaliasName` / `androidUseCustomKeystore` alanlarına **dokunulmaz**, şifreler depoya girmez), Play Console ekran görüntüleri, içerik derecelendirme anketi, Data safety formu ve gizlilik politikası URL'sinin GitHub Pages'te yayınlanması.
- **ProjectSettings'teki şifre benzeri alanlar:** `ps4Passcode` Unity'nin her yeni projeye koyduğu sabit varsayılandır (projenin ilk commit'inden beri depoda, Android ile ilgisi yok), `metroCertificatePassword` boştur. Keystore şifreleri ProjectSettings'e yazılmaz. Bu alanlara dokunulmaz; yeni bir şifre alanı dolu görülürse iş durdurulup kullanıcıya sorulur.

## Açık oyun mantığı konuları (artık kapsam içi, sırayla ele alınacak)
Bunlar önceki dönemde "kapsam dışı" idi; ürün döneminde her biri analiz + onay akışıyla ele alınır. Güncel liste ve ayrıntılar `docs/STATUS.md` içindedir.
**13. turda kapandı:** spawn kuralı (melez), duvara kadar kayma, aynı element tarifleri (N₂ / Cl₂ / C₂), undo sayaçları, kazanma ekranının tekrar tetiklenmesi, `TotalScore`, `Shift()` refactor'ü.

Açık kalanlar:
- Joker / revive silme hatası (DOKill ile iptal olan Destroy)
- Entropi uyarısının yanlış metne yazılması
- TotalAccidents / flashcard / mentor ipucu özelliklerinin anlamı
- **L4–L7 dengesi** — 13. turda bilerek ele alınmadı. Ölçüm var: Na ağırlığını yarıya indirmek L4'ü %13→%30, L6'yı %11→%20 çıkarıyor; Ca'yı yarıya indirmek L6'yı %11→%20 çıkarırken L5'i %30→%27 düşürüyor (L5/L7 hedeflerinde CaO ve CaCO₃ var). Uygulanmadı, denge turuna bırakıldı.
- **Na₂ / Ca₂ tarifleri** — bilerek eklenmedi (karar).
