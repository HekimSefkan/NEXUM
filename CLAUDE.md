# NEXUM – Claude Çalışma Notları

## Proje
- **Motor:** Unity 2022.3.62f3 (`ProjectSettings/ProjectVersion.txt`)
- **Tür:** 2D mobil (Android, dikey) kimya temalı bulmaca oyunu
- **Özet:** 4x4 grid üzerinde temel elementler (H, O, C, N, Na, Cl, Fe, Ca) kaydırılarak tarif tablosuna göre bileşiklere dönüştürülür. 7 level var; her levelin kendi spawn ağırlıkları ve hedef bileşikleri bulunur. Normal / Sınav / Serbest modları, undo, ipucu asistanı, entropi cezası ve quiz ile canlanma sistemi vardır. Ana menüde kayıt, profil, ansiklopedi, nasıl oynanır ve ayarlar panelleri bulunur.
- **Bağımlılıklar:** DOTween (`Assets/Plugins/Demigiant`), TextMeshPro (`Assets/TextMesh Pro`), uGUI. Girdi: eski Input Manager.
- **Sahneler:** `MainMenu.unity` (build 0), `Game.unity` (build 1), `SampleScene.unity` (boş, build'de kapalı)

## Klasör yapısı
```
Assets/
  Editor/            FontReplacerWindow.cs (editör aracı)
  Fonts/             Orbitron / Oxanium TTF + TMP SDF asset'leri
  Plugins/Demigiant/ DOTween
  Prefabs/           Tile_*.prefab (elementler/bileşikler), BeakerGoalPrefab, ComboTextPrefab, ElementCard, MergeParticle
  Resources/
    Avatars/         Mentor portreleri
    Elements/        ElementData asset'leri (Resources.LoadAll ile yüklenir)
    Facts/           ChemistryFact asset'leri
    Quizzes/         ChemistryQuiz asset'leri
    Figures/         UI görselleri
    Voice/           Müzik ve efekt sesleri
    DOTweenSettings.asset
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
| `AutoAnchorTools` | Editör aracı: seçili RectTransform'ların anchor'larını köşelerine taşır (`#if UNITY_EDITOR`) |
| `Editor/FontReplacerWindow` | Editör aracı: sahnedeki TMP fontlarını isim kurallarına göre değiştirir |

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
- **GridBoard (Game.unity):** Sabit **900×900**, nokta anchor (0.5, 0.41666666), pivot (0.5, 0.5), AspectRatioFitter kapalı (`m_Enabled: 0`). GridLayoutGroup: hücre 200, boşluk 20, dolgu 20 → 4×200 + 3×20 + 2×20 = 900. Bu toplam değişirse tahta boyutu da güncellenmeli.
- **Anchor dönüşümleri:** Kenara anchor'lanıp büyük offset'le konumlanan objeler, referans (1080×1920) görünümü koruyan hesapla merkez / üst-orta anchor'a çevrilir (`yeni_pos = eski_anchor × ebeveyn_boyutu + eski_pos − yeni_anchor × ebeveyn_boyutu`).
- **DOTween göreli tween'ler:** `DOShakePosition` / `DOPunchScale` gibi başlangıç değerini yakalayan tween'lerden önce hedefte `DOKill(true)` çağrılır (üst üste binince kalıcı kayma olmasın).
- **IL2CPP / ARM64 / package name:** Kullanıcı Unity Editor'de ayarlayacak; YAML ile dokunulmaz.

## YAML düzenleme kuralları
- Düzenlemeden önce Unity Editor'ün **kapalı** olduğunu kullanıcıya sorarak teyit et.
- Sadece ilgili alanların **değerlerini** değiştir. Blok ekleme / silme yok; `fileID` ve `guid` değerlerine dokunma.
- Obje silme YAML ile yapılmaz; Editor'de kullanıcı yapar.
- Satır sonlarını (sahneler ve ProjectSettings LF, `.cs` dosyaları çalışma kopyasında CRLF) ve girintiyi koru.
- Satır numarası + beklenen eski içerik doğrulamasıyla düzenle; eşleşmezse hiçbir şey yazma.
- Her düzenlemeden sonra `git diff` ile yalnızca hedeflenen satırların değiştiğini doğrula; beklenmeyen satır varsa geri al ve kullanıcıya bildir.
- Float değerleri Unity biçiminde yaz (float32'nin en kısa geri-dönüşümlü gösterimi, örn. `0.41666666`).

## Kapsam dışı (bu temizlik çalışmasında yapılmayacak)
- Spawn kuralı (her geçerli hamlede spawn)
- Taşların duvara kadar kayması
- Aynı element tarifleri (C+C, N+N, Na+Na, Cl+Cl, Ca+Ca)
- Kimya tarifi düzeltmesi (CaO + O₂ → CaCO₃, kopya CO + O tarifi)
- Kazanma ekranının tekrar tetiklenmesi
- Undo sayaçları (hedef sayacı, skor, entropi geri alma, boş hamle snapshot'ları)
- Joker / revive silme hatası (DOKill ile iptal olan Destroy)
- Quiz hakkı (sınırsız revive denemesi)
- Entropi uyarısının yanlış metne yazılması
- TotalAccidents / flashcard / mentor ipucu özellikleri
- `Shift()` refactor'ü
