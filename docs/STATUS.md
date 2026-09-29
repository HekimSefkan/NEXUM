# NEXUM – Durum Notu

**Tarih:** 29 Eylül 2026 · **Dal:** `core-loop` (13. tur, PR açık — birleştirilmedi)

Bu belge projeye ara verirken bırakılan durumu özetler. Ayrıntılı kurallar ve alınan kararlar için `CLAUDE.md`, yayın adımları için `docs/RELEASE_CHECKLIST.md`.

---

## 1. Bugün itibarıyla tamamlananlar

**Mobil yerleşim ve ölçek**
- CanvasScaler her iki sahnede Scale With Screen Size, 1080×1920, Screen Match Mode = **Expand**.
- Yalnızca Portrait; Android en-boy oranı Custom + max 2.4.
- **SafeArea:** `Core/SafeArea.cs` Canvas altındaki tam ekran köke eklendi, HUD onun çocuğu; arka planlar ve tam ekran karartma katmanları dışarıda.
- HUD kümesi (Goals, Score, Joker, Undo, Hint) GridBoard ile aynı anchor'da → aradaki boşluklar her ekran boyutunda sabit.
- Profil, quiz, bölüm tamamlandı, ayarlar ve "nasıl oynanır" panelleri dört SafeAreaRoot boyutunda (1080×1920 / ~1080×2114 / ~1080×2300 / 1440×1920) taşma ve çakışma olmadan doğrulandı.
- Panel kaydırması her açılışta en üstten başlar (`ResetScrollPositionsNextFrame`); regresyon testi var.

**Dosya düzeni ve altyapı**
- `Assets/Art` (Atlases / Avatars / Figures / Icons), `Assets/Audio`, `Assets/Data`, `Assets/Editor`, `Tools/`, `docs/` ayrımı yapıldı; `Resources` yalnızca kodla yüklenenleri tutuyor.
- `.gitattributes` ile satır sonları LF'e sabitlendi; `Builds/`, paketler ve ekran görüntüleri git dışında.
- `AppBootstrap`: 60 FPS, sahne yüklenince `timeScale = 1`, arka plana geçince `PlayerPrefs.Save`.
- **Doğrulama aracı** `Assets/Editor/NexumValidation.cs`: kopuk referans, null alan, NaN rect, UnityEvent hedefi, bölüm çözülebilirliği, doku/ikon/yayın kontrolleri. Beklenen sonuç: `OK`, 0 uyarı.
- **PlayMode testleri** (`Assets/Tests/PlayMode`): panel kaydırma, girdi kilidi, geri tuşu, kayıt uyarısı, yerleşim ölçümü — toplam 9 test.

**Doku, ses ve boyut**
- Doku import kuralı (kullanımın 1,5 katı, çalışma zamanı atamaları dahil) tüm dokulara uygulandı.
- Sprite Atlas V2: `NexumMenuUI` (25 sprite) + `NexumGameUI` (9 sprite); atlasa girenlerde çift sıkıştırma giderildi.
- Ses import kuralı (süreye göre Streaming / Compressed / Decompress) uygulandı.
- ARMv7 kaldırıldı, Target API 36 sabitlendi → **APK 47,19 → 29,92 MiB**.

**İkon ve mağaza paketi**
- Uygulama ikonu `Tools/generate_icons.py` ile vektör olarak çizildi (master 1024, adaptive ön plan/arka plan 432, legacy ve round 512); Android'in 18 ikon slotu `NexumIconSetup` ile dolduruldu.
- Açılış ekranına NEXUM logosu eklendi (Unity logosu Personal lisansta kalıyor).
- Mağaza ikonu (512×512, 32-bit RGBA) ve öne çıkan görsel (1024×500) üretildi.
- Gizlilik politikası `docs/privacy-policy.html` (TR + EN) yazıldı; yayın kontrol listesi `docs/RELEASE_CHECKLIST.md`.
- Oyun içi logolar yeni görsel dille yeniden üretildi (`NEXUM_logo.png` 1366×320, `NEXUM_logo-.png` 512×519, ikisi de şeffaf).

**Oyun içi davranış düzeltmeleri**
- **Girdi kilidi:** modal panel (tutorial, pause, quiz, oyun sonu, kazanma, hipotez, undo) açıkken swipe ve klavye girdisi işlenmiyor.
- **Geri tuşu:** Android geri hareketi tüm ekranlarda ele alınıyor; menüde panel kapanır, kökte "çıkmak için tekrar bas", oyunda duraklat/modal davranışı.
- **Kayıt doğrulaması:** isim kırpılır, 2–20 karakter; hatalar `nameWarningText`, e-posta hataları `emailWarningText` alanında; sarsıntı + hata sesi.
- **Ansiklopedi verisi:** `Fe₂O₃`, `NH₃` ve `NH` metinleri düzeltildi; satır "Formül:" yerine "Sentez:", temel elementlerde "Sentez: Temel element".
- **İkinci şans akışı:** hak quiz paneli açıldığı anda tükeniyor; yanlış cevapta oyun sonu ekranına dönülüyor, sınırsız canlanma kapandı.
- **Kimya:** `CaO + CO₂ → CaCO₃` düzeltmesi ve yinelenen `CO + O` tarifinin kaldırılması; 15 tarifin bilimsel ipucu metni yazıldı.
- **Ses:** duraklat menüsündeki "Devam Et" / "Ana Menü" ve "Yeniden Başlat" butonları artık ses veriyor (kare korumasıyla çift ses yok).
- **Profil istatistikleri:** etiketler veriye uyduruldu, "LABORATUVAR KAZASI" gerçek veri gösteriyor (oyun başına bir kez artar).
- **Skor × hedef ve kombo yazısı** çakışmaları ölçümle giderildi.

---

## 2. Son ölçülen paket ve build ayarları

| | Değer |
|---|---|
| APK (ölçüm build'i) | 31.533.648 bayt (**30,07 MiB**) |
| AAB (ölçüm build'i) | 31.444.764 bayt (**29,99 MiB**) |
| Karşılaştırma: v1 | 46.535.160 bayt (44,38 MiB) |
| Dokular | toplam kullanıcı asset'inin ~%90'ı |

| Ayar | Değer |
|---|---|
| Scripting backend | IL2CPP |
| Target architectures | **ARM64** (`AndroidTargetArchitectures: 2`) |
| Target API Level | **36** |
| Minimum API Level | 22 |
| Paket adı | `com.hekimsefkan.nexum` |
| versionName / versionCode | 1.0 / 1 |
| Ekran yönü | Portrait |
| İmzalama | Custom keystore (yol + alias ProjectSettings'te, **şifreler depoda değil**) |

> Yayın paketi batch mode'da alınmaz: imzalı AAB Unity Editor'de üretilir (`RELEASE_CHECKLIST.md` 1.6). `NexumBuild` ile alınan paketler yalnızca ölçüm içindir.

---

## 3. Açık maddeler

### (a) Teknik
- **`extractNativeLibs` / custom gradle template** — AAB indirme boyutu için `false` tercih edilir; custom gradle template Unity sürüm yükseltmelerinde elle bakım gerektirdiği için uygulanmadı.
- **Kaynak çözünürlüğü yetersiz görseller** — avatarlar (1024 kaynak / 724 birim kullanım), `arkaplan` (941×1672), `grid.png` (783), `gamepanel` (1536). Import ayarıyla çözülmez; daha büyük kaynak görsel gerekir. Validator bu durumda bilerek uyarmaz.
- **Kombo yazısının konumu** — şu an grid ile skor arasındaki sabit bantta (merkez y = 328). Oyun hissi bozuk bulunursa hazır alternatif: `UIManager.ShowComboText` içinde sabit `posY` yerine `spawnPosition.y`'yi `Mathf.Clamp` ile banda sıkıştırmak; yazı birleşmenin yüksekliğinde doğar, yalnızca sınırı aşmaz.
- **Beher etiketi** — `BeakerGoalPrefab`'ın `AmountText` kutusu 120×80 ama TMP iki satır için 91 birim istiyor; etiket kabın 22 birim altına taşıyor. Skor yatayda kaydırılarak çözüldü, prefab'a dokunulmadı.

### (b) Oyun mantığı

**13. turda kapandı (dal: `core-loop`, PR açık — cihaz testi bekliyor):**
- **Kayma** — taşlar duvara / engele kadar kayıyor; aynı hamlede üretilen bileşik ikinci kez birleşmiyor.
- **Spawn** — melez kural: birleşmesiz her 2 hamlede bir taş, her modda. Entropi ceza taşı yalnızca Normal + Sınav modunda.
- **Aynı element tarifleri** — N+N→N₂, Cl+Cl→Cl₂, C+C→C₂ ve tüketicileri eklendi (tablo 21 satır). Na₂ / Ca₂ bilerek eklenmedi.
- **Undo** — skor, hedef sayaçları, entropi, spawn sayacı ve toplam sentez geri alınıyor; geçersiz hamle snapshot bırakmıyor.
- **Kazanma ekranı** — `hasWon` bayrağıyla bir kez tetikleniyor.
- **`TotalScore`** — `Core/TotalScoreService.cs` ile canlandırıldı (gösterim Faz 2'ye bırakıldı).
- **`Shift()` refactor'ü** — dört kopya blok tek döngüde; parmak izi testiyle davranışın aynı kaldığı kanıtlandı.

**Açık kalanlar:**
- **L4–L7 dengesi** — 13. turda bilerek ele alınmadı. Simülatör ölçümü: Na ağırlığını yarıya indirmek L4 %13→%30, L6 %11→%20; Ca'yı yarıya indirmek L6 %11→%20 ama L5 %30→%27 (L5/L7 hedeflerinde CaO ve CaCO₃ var).
- **Joker / revive silme hatası** — `DOKill` ile iptal olan `Destroy`.
- **Entropi uyarısının yanlış metne yazılması.**
- **İstatistiklerin anlamı** — "TEORİK BAŞARI" yalnızca ikinci şans quizi cevaplanınca değişiyor; "LABORATUVAR KAZASI" oyuncuya ne anlattığı tasarım kararı bekliyor.
- **İçerik işleri** — flashcard ve mentor ipucu özellikleri, ek quiz soruları, ansiklopedi metinlerinin genişletilmesi.

### (c) Yayın
- **Play Console adımları** — mağaza listesi, içerik derecelendirme anketi, Data safety formu, hedef kitle ve reklam beyanı, gizlilik politikası URL'si. Tüm adımlar `docs/RELEASE_CHECKLIST.md` bölüm 2'de.
- **Gizlilik politikası yayını** — `docs/privacy-policy.html` depoda hazır; GitHub Pages **henüz açılmadı**. Açıldığında adres: `https://hekimsefkan.github.io/NEXUM/privacy-policy.html`
- **Ekran görüntüleri** — en az 2, en çok 8 telefon görüntüsü gerekiyor; henüz alınmadı. Editor'den almak için *NEXUM → Ekran Görüntüsü Al (1080×1920)* (`Ctrl+Shift+S`).
- **Mağaza görselleri** — ikon (512×512, 32-bit) ve öne çıkan görsel (1024×500) hazır: `Tools/store/`.
- ⚠️ **Keystore uyarısı** — release keystore dosyası ve şifreleri kaybedilirse bu uygulamaya **bir daha güncelleme yayınlanamaz**. Keystore depoya konmaz, en az iki güvenli yerde yedeklenir, şifreler parola yöneticisinde tutulur. Unity her açılışta şifreleri yeniden ister.

---

## 4. Yeniden başlarken

1. **Önce oku:** proje kökündeki `CLAUDE.md` (kurallar, alınan kararlar, kapsam dışı listesi) ve bu dosya.
2. **Unity Editor kapalı olmalı** — `.unity`, `.prefab`, `.asset` ve `ProjectSettings` düzenlemelerinden önce teyit edilir.
3. **Doğrulama komutları** (Windows PowerShell, proje kökünden):
   ```powershell
   & "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "C:\Projects\NEXUM" -executeMethod NexumValidation.Run -logFile "C:\Projects\NEXUM\Logs\validate.log" | Out-Null
   Select-String -Path "C:\Projects\NEXUM\Logs\validate.log" -Pattern "NEXUM_VALIDATION|error CS" | ForEach-Object { $_.Line }
   ```
   ```powershell
   & "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -runTests -projectPath "C:\Projects\NEXUM" -testPlatform PlayMode -testResults "C:\Projects\NEXUM\Logs\tests.xml" -logFile "C:\Projects\NEXUM\Logs\tests.log" | Out-Null
   Set-Location "C:\Projects\NEXUM"; git status --short
   ```
   Beklenen: `NEXUM_VALIDATION: OK` + 0 uyarı, PlayMode 18/18. PlayMode testi `ProjectSettings.asset` içindeki `runInBackground` değerini 1 yapar; commit etmeden `git restore` ile geri alınır.
4. **Dal ve PR akışı:** `main`'den yeni bir dal aç (`git switch -c <konu>`), her adımı ayrı commit et, push edip PR aç. Cihazda görsel doğrulama gerektiren değişiklikler kullanıcı test etmeden birleştirilmez; belge/ayar değişiklikleri validator + testler temizse birleştirilebilir. `main`'e doğrudan commit, force push ve geçmiş yeniden yazma yok.
5. **Ölçmeden düzeltme yok:** yerleşim sorunlarında `Assets/Tests/PlayMode/LayoutProbeTests.cs` çalışma zamanı dikdörtgenlerini referans birimiyle loglar; sahnedeki kayıtlı değerler TMP ve layout grupları yüzünden yanıltabilir.
