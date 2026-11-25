# NEXUM – Durum Notu

**Tarih:** 25 Kasım 2026 · **Dal:** `stars` (Faz 2 / Adım 2.2, PR açık — birleştirilmedi)

> **FAZ 2 / 2.2 AÇIK PR.** Üç yıldız sistemi kuruldu: kurallar, hamle
> sınırları (ölçümle kalibre), kayıt sürüm 5, sonuç kartı · hub · bölüm
> haritası. **Hamle sınırı tablosu KULLANICI ONAYI BEKLİYOR**
> (`docs/DESIGN_TOKENS.md` 13.3). Zorluğa dokunulmadı; L4–L7 gözlemi
> 13.5'te.
>
> **FAZ 2 / 2.1 BİRLEŞTİ (PR #31).** XP, rütbe ve rozet sistemi; rütbe
> adları/eşikleri, rozet listesi/görselleri ve Araştırma Puanı tasarımı
> 25 Kasım 2026'da onaylandı (`docs/DESIGN_TOKENS.md` 12,
> `docs/ui-kit/badges.png`, `docs/ECONOMY_PROPOSAL.md` 4). Araştırma
> Puanı için **kod yazılmadı** — uygulama ayrı bir adım.
>
> **FAZ 1 TAMAMLANDI.** Oyuncunun gördüğü bilgi katmanı bitti: tepkime denklemi, tarif defteri, geçersiz birleştirme geri bildirimi, istatistik onarımı, quiz öğrenme katmanı, bölüm sonu sonuç kartı + paylaşım ve ana menü hub'ı.

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
- **PlayMode testleri** (`Assets/Tests/PlayMode`): panel kaydırma, girdi kilidi, geri tuşu, kayıt uyarısı, yerleşim ölçümü, çekirdek döngü, kayıt göçü, lokalizasyon, tepkime denklemi, ok kalibrasyonu, tarif defteri, sonuç kartı, menü hub'ı, ilerleme (XP/rütbe/rozet) — toplam **192 test** (2.1 itibarıyla).

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

## 1b. Ürün dönemi (Faz 0 ve Faz 1)

**Faz 0 — altyapı**
- **SaveService** (`Assets/Scripts/Save`, `Nexum.Save` asmdef): şemalı, sürümlü tek kayıt katmanı. Eski `PlayerPrefs` anahtarlarından göç, alan bazında onarım, kirli bayrakla yazma. `PlayerPrefs` yalnızca bu dosyada kullanılabilir (validator zorunlu kılar); testler için `ExportRaw` / `ImportRaw` kapısı var. **Güncel sürüm: 3** (sürüm 3: quiz yanlış cevap havuzu; eski kayda geriye dönük doldurma YAPILMAZ).
- **Lokalizasyon** (`Assets/Scripts/Localization`, `Nexum.Localization` asmdef): JSON sözlük (`Assets/Data/Localization/tr.json`, `en.json`) → `LocalizationTable` asset'i; `Loc.Get` / `Loc.Format`, `LocalizedText` bileşeni, dil değişince olay. Sözlükte **100 anahtar**; İngilizce çeviri Faz 6'ya bırakıldı (anahtarlar var, metinler Türkçe tohum).
- **UIBuilder** (`Assets/Editor/UIBuilder`): sahne yapısı elle YAML ile değil idempotent kurucularla kurulur. Kayıt dosyası `managed.json`, `-dryRun` desteği, oluşturulan/güncellenen/dokunulmayan raporu. Nokta anchor'lı objelerde **`SetFixed(anchor, size, anchoredPosition)`** kullanılır; `SetFixed` + `SetRectPosition` ikilisi dosyayı değiştirmese de her koşuda "güncellendi" raporluyordu.
- **Tema tokenları:** `NexumTheme` asset'i → `UITokens` → kurucular. Renk/punto/boşluk doğrudan yazılmaz.

**Faz 1 — oyuncunun gördüğü bilgi katmanı**
- **Tepkime denklemi** (Adım 1): her birleşmede "H₂ + O → H₂O" birleşmenin olduğu hücrenin üstünde. 8 etiketlik havuz, çakışma hâlinde satır kaydırma, kombo bandına girmeme. *(PR #19 — cihazda onaylandı, birleşti.)*
- **Ok sembolü** (Adım 2): "→" (U+2192) proje fontlarının hiçbirinde yok ve atlaslar statik; ok `Tools/generate_arrow.py` ile çizilip TMP sprite asset'i olarak gömüldü (`Assets/Resources/Sprite Assets/NexumSymbols.asset`). Ölçüm: **punto'nun %40'ı boy, merkez sapması %0,0** (4 canvas boyutu × 2 dil). Sprite yüklenemezse hata loglanıp `->` kalıbına düşülür.
- **Tek kaynak kuralı:** `ReactionText` — denklem gösterimi, tarif defteri ve ansiklopedi kartı aynı kalıbı ve aynı sprite'ı kullanır. Kalıplar sözlükte (`reaction.inputs`, `reaction.equation`).
- **Tarif defteri** (Adım 2): 21 tarifin listesi, keşif sayacı, ilerleme halkası; keşfedilmemiş satırlarda `? + ? → ?` + kilit ikonu (`Tools/generate_lock.py`). Erişim ana menüden ve duraklat menüsünden. Keşif ilk birleşmede kaydedilir; mevcut oyunculara **geriye dönük doldurma** üretim grafiğinden türetilir (bir bileşiği tek bir tarif üretiyorsa o tarif zorunludur; birden fazla üretici varsa hiçbiri sayılmaz).
- **Sistem bildirimi** artık kombo bandını kullanmıyor: yeni tarif keşfi ile kombo yazısı aynı karede doğduğu için bildirim tahtanın altındaki kendi bandına alındı.
- **Ana menü hub'ı** (Adım 1.7): logo · profil şeridi · kahraman kart · ikili kart · mod satırı · bilgi satırı · ikon şeridi. Yerleşim AKAR (`VerticalLayoutGroup` + açık `LayoutElement`): uygulanmamış giriş tamamen gizli, gizli giriş boşluk bırakmıyor. Görünürlüğün tek kaynağı `HubFeatures`; Faz 2/3/5 bir özelliği bitirince orada tek satır değişir. Durum satırları gerçek kayıttan ("Bölüm 4 · kaldığın yerden", "4 / 7 açık", "10 / 21 tepkime") — sahte değer yok. Ses ikonu Ayarlar'la aynı kayıt alanlarını yazar. Ölçüm: bugünkü hâl 1310, tüm bayraklar açık 1514 birim (ikisi de 1920'ye sığıyor, kaydırma yok).
- **Quiz öğrenme katmanı** (Adım 1.5): 36 sorunun tamamında açıklama (sözlükte, anahtar asset adından türetilir); doğru cevap panelinde başlık + açıklama + "dokun" ipucu, bekleme en az 2 sn / dokununca / en çok 10 sn. Yanlış cevaplanan sorular `SaveData.quizWrongPool`'a girer ve sonraki quizlerde **1/3 oranında** öncelik kazanır (`QuizSelector`); aynı soru üst üste sorulmaz, havuz yalnızca kesinleşmiş cevapla değişir. Banka 16 → **36 soru** (`NexumQuizBank`, idempotent), etiketli (konu · zorluk · ilgili bileşik) — etiketler Faz 5 soru modu için, o mod kurulmadı. Soru puntosu pişirildi (auto-size kapalı, 46). **Bilgi Kartları ayarı** duraklat kartındaki "BİLİYOR MUYDUN?" bölümüne bağlandı. Kimya doğruluğu `docs/QUIZ_REVIEW.md` tablosunda onay bekliyor.

**Faz 2 — ilerleme ve ödül katmanı**
- **XP, rütbe ve rozet** (Adım 2.1, PR açık): `Assets/Scripts/Progression`
  (`Nexum.Progression` asmdef) — XP tablosu, yedi rütbe, dokuz rozet.
  Derleme hiçbir oyun tipine bağlı değil; bölüm/tarif/quiz sayıları
  `ProgressionBootstrap` üzerinden parametre olarak geliyor.
  - **XP kaynakları:** bölüm tamamlama (60 + kazanılan/5), tarif keşfi (30),
    quiz ilk doğru cevabı (12). Yıldız / günlük bulmaca / ansiklopedi
    kaynakları `XpSources` tablosunda TANIMLI ama kapalı.
  - **Çiftlik yok:** bölüm XP'si "en iyi sonuç" defteri (`levelXpGranted`);
    aynı bölümü tekrar bitirmek 0 XP, daha iyi bitirmek yalnızca farkı
    veriyor. Quiz soru başına bir kez ödüyor (`quizXpAwarded` +
    `quizXpPaid` tavanı).
  - **Geri alınan hamle XP bırakmaz** — anlık görüntüyle değil YAPIYLA:
    XP'nin hiçbir yazıcısı geri alınabilir bir ana bağlı değil.
  - **Kayıt sürüm 4:** `xp`, `levelXpGranted`, `quizXpAwarded`,
    `quizXpPaid`, `badges`, `rankCelebrated`, `xpBackfillDone`. Geriye
    dönük hesap yalnızca kaydedilmiş veriden (tamamlanmış bölüm, keşfedilen
    tarif, `quizCorrect`); puan payı verilmez çünkü bölüm başına kazanılan
    puan hiç kaydedilmedi.
  - **Kutlama** `RankUpPanel`: iki sahnede de kurulu, ölçülü (tam ekran
    parlama yok, döngü yok, dokununca kapanır, **bir kez** gösterilir).
  - **Görünür yerler:** sonuç kartında "+N XP" satırı (kart 1500 → 1600),
    hub profil şeridinde rütbe adı + XP çubuğu.
  - **Rozet görselleri** `docs/ui-kit/badges.png` — kullanıcı onayı bekliyor;
    glifler henüz `Assets`'e alınmadı (rozet duvarı 2.3'ün işi).
  - **Araştırma Puanı** `docs/ECONOMY_PROPOSAL.md` — tasarım onaylandı,
    kod yazılmadı.
- **Üç yıldız** (Adım 2.2, PR açık): `Progression/StarRules` (kural) +
  `StarService` (muhasebe).
  - **Bit maskesi, sayı değil:** ★2 kaçıp ★3 kazanılabiliyor ve ekranda
    doğru yuva doluyor.
  - Katalizörün **bedava** Joker hakkı ★3'ü bozmuyor; geri alma hamleyi
    geri alıyor ama yardım bayrağını almıyor; **serbest modda yıldız yok**
    (ölçüldü: orada kazanma ekranı zaten gelmiyor).
  - **Hamle sınırları ölçümle** (`Tools/sim_stars.py`, 2000 tohum,
    derinlik-2): kazanan oyunların %75'i. Botun gücü sınırı değiştirmiyor
    (±1 hamle), yani çözücüye duyarsız.
  - Kayıt sürüm 5: `levelStars` (OR ile birikir), `levelBestMoves` (MIN),
    `starBackfillDone`. Geriye dönük hesap yalnızca ★1 verir.
  - Yıldız XP'si yıldız başına bir kez (maskeden bedava çiftlik koruması).
  - Görünür yerler: sonuç kartı (yıldızlar + "37/15" hamle kutusu + tek
    satırlık yönlendirme), hub kahraman kart, bölüm haritası düğümleri.

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

**Faz 2'ye devredilenler:**
- **Yıldızlar (2.2).** Üç yıldızlık yer hem kahraman kartta hem bölüm sonu sonuç kartında KURULU ama `HubFeatures.Stars` / `ResultCard.starsEnabled` ile gizli. Yıldız XP'si (`ProgressionTable.StarXp = 20`) ve "Üç Yıldızlı Bölüm" rozeti de tanımlı ama `XpSources.Stars` / `Badges` ile kapalı; 2.2'de tek satır açılır.
- ~~**XP'nin kaynağı — KURAL.**~~ **2.1'de UYGULANDI:** XP `LevelRun.EarnedScore`'dan besleniyor, `HubFeatures.RankAndXp` açıldı.
- **`totalScore` hâlâ ÖLÜ-KAYIT.** `SaveService.AddLevelScore` yazıyor, hiçbir ekran göstermiyor. XP ondan beslenmiyor. `docs/ECONOMY_PROPOSAL.md` kaldırılmasını ve yerini Araştırma Puanı'nın almasını öneriyor (bkz. `docs/STATS_INVENTORY.md`).
- **L4–L7 denge verisi.** Ölçüm 13. turda alındı ve duruyor: Na ağırlığını yarıya indirmek L4'ü %13→%30, L6'yı %11→%20 çıkarıyor; Ca'yı yarıya indirmek L6'yı %11→%20 çıkarırken L5'i %30→%27 düşürüyor. Uygulanmadı; denge turu bu veriyle başlayabilir.

**2.1'de ONAYLANAN (25 Kasım 2026):**
- **Rütbe adları ve eşikleri** — `docs/DESIGN_TOKENS.md` 12.1 / 12.2.
  Çırak 0 · Stajyer 150 · Laborant 400 · Araştırmacı 800 · Kimyager 1400 ·
  Baş Kimyager 2400 · Nobel Adayı 4000. Üç çelişki de karara bağlandı:
  "Baş Kimyager" merdivende kaldı, "Doçent" alınmadı, "Stajyer Kimyager"
  ölçüm sonucu "Stajyer"e kısaldı.
- **Rozet listesi ve görselleri** — `docs/ui-kit/badges.png`,
  `docs/DESIGN_TOKENS.md` 12.4. Dokuz rozet; üçü açık, altısı tanımlı ama
  kazanılamaz. Glifler `Tools/ui-kit/badges/` altında, **Assets'e 2.3'te
  alınacak** (bugün rozeti gösteren ekran yok).
- **Araştırma Puanı tasarımı** — `docs/ECONOMY_PROPOSAL.md` bölüm 4.
  Üç karar: kozmetik kapsamda ama yalnızca kitten üretilebilenler
  (mentor portre seti listeden düştü), **iade yok**, ve liste bugünkü
  arza sığar (arz 491 RP, liste 470 RP). **KOD YAZILMADI** — uygulama
  ayrı bir adım ve oyun hissini değiştiren kısmı (ipucu bedeli, geri
  alma hakkı, katalizör eşiği, başlangıç taşı yükseltmelere bağlanıyor)
  kendi analiz + onay turunu ister.

**2.1'den ONAY BEKLEYEN TEK ŞEY:**
- **Cihaz testi** — PR #31 hâlâ birleştirilmedi; ekranı etkileyen
  değişiklikler cihazda görülmedi.

**2.2'de UYGULANDI:** üç yıldız. `HubFeatures.Stars`,
`ResultCard.starsEnabled` ve `XpSources.Stars` açıldı; "Üç Yıldızlı
Bölüm" rozeti kazanılabilir oldu. Hamle sınırları 8 / 11 / 15 / 33 /
29 / 28 / 37 — **onay bekliyor** (`docs/DESIGN_TOKENS.md` 13.3).

**2.3'e devredilenler:** profil panelinin yeniden yapımı (rozet duvarı,
kesin XP sayısı, **varsayılan oyuncu adının düzeltilmesi**).
Rozet duvarı için üç somut adım `docs/DESIGN_TOKENS.md` 12.4 sonunda:
glifleri `NexumKitImport` ile içeri al, `NexumAtlasBuilder` listesine
ekle, çerçeve zaten içeride. Rozetler 2.1'den beri sessizce kazanılıyor,
yani duvar açıldığında geçmiş hazır.
Sonuncusu artık bir AÇIK MADDE, tasarım sorusu değil: rütbe merdiveni
"Baş Kimyager"i içerecek şekilde onaylandı, `CodeStrings.ProfileDefaultName`
ise hâlâ aynı metni varsayılan oyuncu adı olarak kullanıyor; adsız
oyuncuda ikisi şeritte alt alta görünüyor
(`docs/device-shots/ana-menu_1080x1920.png`).

**Faz 3'e devredilenler:** günlük bulmaca kartı ve gün serisi rozeti (kurulu, `HubFeatures.DailyPuzzle` / `Streak` ile gizli). Günlük bulmaca, `docs/ECONOMY_PROPOSAL.md`'ye göre Araştırma Puanı'nın tek YENİLENEBİLİR kaynağı olacak. **Kabul kriteri (2.1'de karara bağlandı):** günlük bulmaca turu RP ARZINI ve YENİ YÜKSELTME MADDELERİNİ birlikte getirir. Tek başına arz eklemek, %100 tamamlayan oyuncuda zaten susan para birimini daha hızlı biriktirmek demek olur (ECONOMY_PROPOSAL 2.4, madde 3).

**Faz 5'e devredilenler:** KAYNAMA NOKTASI · SONSUZ LAB · HAFTANIN DENEYİ mod kartları ve mentor ikonu (kurulu, gizli). Quiz bankasındaki 36 sorunun etiketleri (konu · zorluk · ilgili bileşik) KAYNAMA NOKTASI'nın süzme verisi olarak hazır.

**Faz 6'ya ertelenenler:** İngilizce çeviri (sözlükte 184 anahtar, 165'i çevrilmedi) ve `docs/NEXT_3B.md`'deki görsel/temizlik notları.

**Açık kalanlar:**
- **L4–L7 dengesi** — 13. turda bilerek ele alınmadı. Simülatör ölçümü: Na ağırlığını yarıya indirmek L4 %13→%30, L6 %11→%20; Ca'yı yarıya indirmek L6 %11→%20 ama L5 %30→%27 (L5/L7 hedeflerinde CaO ve CaCO₃ var).
- **Joker / revive silme hatası** — `DOKill` ile iptal olan `Destroy`.
- **Entropi uyarısının yanlış metne yazılması.**
- **İstatistiklerin anlamı** — "TEORİK BAŞARI" yalnızca ikinci şans quizi cevaplanınca değişiyor; "LABORATUVAR KAZASI" oyuncuya ne anlattığı tasarım kararı bekliyor.
- **İçerik işleri** — mentor ipucu özelliği ve ansiklopedi metinlerinin genişletilmesi. (Bilgi kartları ayarı 1.5'te bağlandı, quiz bankası 36 soruya çıktı.)

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
   Beklenen: `NEXUM_VALIDATION: OK` + 0 uyarı, PlayMode **192/192** (2.1 ölçümü). PlayMode testi `ProjectSettings.asset` içindeki `runInBackground` değerini 1 yapar; commit etmeden `git restore` ile geri alınır.
4. **Dal ve PR akışı:** `main`'den yeni bir dal aç (`git switch -c <konu>`), her adımı ayrı commit et, push edip PR aç. Cihazda görsel doğrulama gerektiren değişiklikler kullanıcı test etmeden birleştirilmez; belge/ayar değişiklikleri validator + testler temizse birleştirilebilir. `main`'e doğrudan commit, force push ve geçmiş yeniden yazma yok.
5. **Ölçmeden düzeltme yok:** yerleşim sorunlarında `Assets/Tests/PlayMode/LayoutProbeTests.cs` çalışma zamanı dikdörtgenlerini referans birimiyle loglar; sahnedeki kayıtlı değerler TMP ve layout grupları yüzünden yanıltabilir.
