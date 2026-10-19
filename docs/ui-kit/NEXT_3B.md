# 2d-3b iş listesi — yerleşim adımı

2d-3a **malzeme** geçişiydi: sprite, renk, font. Hiçbir nesnenin konumu veya
boyutu değişmedi. Yerleşimi değiştiren her şey buraya bırakıldı.

---

## 1. Adlandırma kararı — "KAYNAMA NOKTASI"

TYT/AYT soru modunun menüdeki adı **KAYNAMA NOKTASI** olacak.
Menüde **"sınav", "deneme", "simülasyon" kelimeleri kullanılmayacak.**
Ayarlar'daki oyun modu olan **"SINAV" adını koruyor**; ikisi farklı şeyler ve
artık çakışmıyorlar.

Loc anahtarları **hazır** (tr + en, `NexumLocalize` ile derlendi):

| Anahtar | tr | en |
|---|---|---|
| `menu.boiling_point` | KAYNAMA NOKTASI | BOILING POINT |
| `menu.boiling_point_status` | En iyi: {0}/{1} doğru | Best: {0}/{1} correct |
| `menu.daily_puzzle` | GÜNLÜK BULMACA | DAILY PUZZLE |
| `menu.daily_puzzle_undone` | Bugün çözülmedi | Not solved today |
| `menu.endless_lab` | SONSUZ LAB | ENDLESS LAB |
| `menu.endless_lab_best` | En iyi: {0} birleşme | Best: {0} merges |
| `menu.weekly_experiment` | HAFTANIN DENEYİ | EXPERIMENT OF THE WEEK |
| `menu.weekly_experiment_this` | Bu hafta: {0} | This week: {0} |
| `menu.level_select` | BÖLÜM SEÇİMİ | LEVEL SELECT |
| `menu.coming_soon` | Yakında | Coming soon |

## 2. Ana menü hub düzeni

- **Logo EN ÜSTTE ince bant** (profil şeridinin altında değil).
- **Mod kartları ve günlük bulmaca kartı boş kalmayacak**; her biri tek satır
  durum taşıyacak — anahtarlar yukarıda hazır.
- **Bilgi satırı (Ansiklopedi, Tarif Defteri) en zayıf ağırlıkta olmayacak**;
  mod kartlarıyla eşit ağırlık alacak.
- Gizleme bayrağı: yeni oyuncuya kilitli girişler gösterilmez.

## 3. Yerleşim düzeltmeleri — 3a'da yapılamayanlar

| Konu | Neden 3a'da yapılmadı | 3b'de ne yapılacak |
|---|---|---|
| **Dört menü butonu** (`oyna`, `nasıloynanır`, `ansiklopedi`, `ayarlar`) | Metin **görselin içinde basılı**; kit çerçevesi koymak metni siler | Kit çerçevesi + **TMP etiketi eklenecek** (yapı değişikliği, UIBuilder) |
| **Beher etiketi** | Formül beherin **içinde** ve çok küçük, okunmuyor | Formül beherin **YANINDA** duracak; sayaç `chip` sprite'ına taşınacak |
| **Katalizör tüpü** | 3-slice uygulaması yeni oran gerektiriyor | Tüp yatayda 3-slice, dolum çubuğu camın içine |
| **İpucu butonu** | HUD'da yok | `GERİ AL / PARÇALA / İPUCU` üçlüsü, rozet, devre dışı durumu |
| **Karakter (hint_assistant)** | Sağ kenardan taşıyor | `icon_button` / `avatar_frame` içine alınıp yeniden çerçevelenecek |
| **Taş durum sprite'ları** | `TileVisual` bağlı ama yalnızca `newborn` tetikleniyor | `selected` / `merging` / `discovered` çağrı noktaları eklenecek |
| **Toggle ve kilitli bölüm sprite'ları** | Sahnede `Image.sprite` olarak değil, Button geçişinde tutuluyor olabilir — swap tablosunda eşleşme bulunamadı | Nerede tutulduğu bulunup kite bağlanacak |

## 4. Silinecek dosyalar (silme yetkim yok)

Aşağıdaki **22 doku artık hiçbir sahnede referanslı değil**. Unity referanssız
asset'i build'e koymadığı için APK'ya girmiyorlar; yalnızca depoda yer
kaplıyorlar (**18,17 MiB**).

```
Assets/Art/Figures/arkaplan.png
Assets/Art/Figures/level_selection_arkaplan.png
Assets/Art/Figures/Registration_arkaplan.png
Assets/Art/Figures/NEXUM_logo.png
Assets/Art/Figures/gamepanel.png
Assets/Art/Figures/SettingsPanel_.png
Assets/Art/Figures/Nasiloyanir_arkaplan2.png
Assets/Art/Figures/Registration_cartblue.png
Assets/Art/Figures/Registration_avatarkartı.png
Assets/Art/Figures/avatar_cerceve.png
Assets/Art/Figures/SettingsPanel_modsecimkartı.png
Assets/Art/Figures/Nasiloyanir_Buton.png
Assets/Art/Figures/game_button.png
Assets/Art/Figures/game_button1.png
Assets/Art/Figures/Registration_girişyapbutonu.png
Assets/Art/Figures/Registration_isim_butonu.png
Assets/Art/Figures/Registration_emailbuton.png
Assets/Art/Figures/level_selection_button.png
Assets/Art/Figures/Cember.png
Assets/Art/Figures/1.seviyetas.png
Assets/Art/Figures/2.seviyetas.png
Assets/Art/Figures/3.seviyetas.png
```

> **Silmeden önce:** `NEXUM_logo.png` splash ekranında ve ikon üretiminde de
> geçiyor olabilir; `ProjectSettings` ve `Tools/generate_icons.py` kontrol
> edilmeli. Diğerleri için sahne taraması temiz.
>
> `oyna.png`, `nasıloynanır.png`, `ansiklopedi.png`, `ayarlar.png` **hâlâ
> kullanılıyor** — 3b'de TMP etiketine geçilene kadar silinmemeli.

## 5. Atlas

72 kit sprite'ı şu an **atlasta değil** ve sıkıştırmalı import ediliyor.
Draw call kazancı için `NexumAtlasBuilder` listelerine eklenmeleri
değerlendirilmeli; eklenirlerse import formatı **Uncompressed**'a dönmeli
(çift sıkıştırma olmasın — validator bunu kontrol ediyor).

## 6. APK ölçümü

2d-3a'da **ölçülemedi**: `NexumBuild` paket imzalama için keystore şifresi
istiyor, şifrelere dokunulmuyor. Doku bütçesi tahmini **+2,55 MiB**
(net doku +0,29 MiB, font atlasları +2,25 MiB). Gerçek APK farkı Editor'de
alınacak build ile ölçülmeli.
