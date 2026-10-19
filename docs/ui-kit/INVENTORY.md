# NEXUM — Arayüz envanteri (Faz 1 / Adım 2d-1)

İki sahne (`MainMenu`, `Game`) ve 30 prefab tarandı. **Bu belge 2d-2'nin iş
listesidir.** Sayılar ölçümdür, tahmin değil.

---

## 1. Buton görselleri

| Dosya | Boyut | Metin basılı mı | Nerede | 2d-2'de |
|---|---|---|---|---|
| `oyna.png` | 901×265 | **EVET** ("OYNA") | PlayButton | değiştirilecek |
| `nasıloynanır.png` | 897×201 | **EVET** | HowToPlayButton | değiştirilecek |
| `ansiklopedi.png` | 898×204 | **EVET** | EncyclopediaButton | değiştirilecek |
| `ayarlar.png` | 894×219 | **EVET** | SettingButton | değiştirilecek |
| `Nasiloyanir_Buton.png` | 1255×311 | hayır | 7 kullanım (nasıl oynanır içi, tarif defteri menü butonu) | kit ile değişir |
| `level_selection_button.png` | 832×288 | hayır | 7 bölüm düğümü | kit: `level_node_open` |
| `level_selection_kilitlibutton.png` | 837×282 | kilit ikonu basılı | kilitli bölüm | kit: `level_node_locked` |
| `SettingsPanel_modsecimkartı.png` | 1365×548 | hayır | 9 kullanım (mod seçimi, oyun sonu butonları) | kit ile değişir |
| `game_button.png` | 640×234 | hayır | ScoreButton, UndoButton | kit: `chip` / `btn_secondary` |
| `game_button1.png` | 641×235 | hayır | JokerButton | kit: `btn_primary` |
| `Registration_girişyapbutonu.png` | 1456×292 | hayır | SinginButton | kit: `btn_primary` |
| `Registration_isim_butonu.png` | 1453×571 | hayır | NameInputField | kit: `input_field` |
| `Registration_emailbuton.png` | 1423×427 | hayır | EmailInputField | kit: `input_field` |
| `geri.png` | 497×479 | ikon | 6 geri butonu | kit: `icon_button` + ikon |
| `x.png` | 485×467 | ikon | 3 kapat butonu | kit: `icon_button` + ikon |
| `Tik.png` | 1024×1024 | ikon | 9 vurgu halkası | gözden geçirilecek |
| `avatar_cerceve.png` | 778×736 | hayır | ProfileButton | kit: `avatar_frame` |

**17 ayrı buton görseli; 4'ünde metin basılı.** Kit bunların yerine **tek**
9-slice çerçeve ailesi koyar (4 rol × 4 durum = 16 sprite, hepsi aynı
geometriden).

> Metin basılı olmasının bedeli ölçüldü: "TARİF DEFTERİ" butonu için aynı dili
> kullanan bir görsel üretilemedi, siluet olarak en yakın metinsiz çerçeve
> kullanıldı. İngilizce sürüm bu dört görselle **imkânsız**.

## 2. Panel / kart çerçeveleri

| Dosya | Boyut | Nerede |
|---|---|---|
| `gamepanel.png` | 1024×1536 | PausePanel, HypothesisPanel, UndoPanel (3) |
| `SettingsPanel_.png` | 796×1280 | Ayarlar cam kartı |
| `Nasiloyanir_arkaplan1.png` | 768×1376 | HowToPlayPanel |
| `Nasiloyanir_arkaplan2.png` | 687×1315 | 4 kullanım (pop-up, element detayı) |
| `Registration_cartblue.png` | 464×777 | 2 kullanım |
| `Registration_avatarkartı.png` | 467×781 | mentor kartı |
| `ProfilPanel_KimlikKartı.png` | 1024×1536 | profil HeroCard |
| `ProfilPanel_*.png` (4 adet) | ~380×500 | istatistik kartları |
| `arkaplan.png` | 941×1672 | ana menü + tarif defteri zemini |
| `level_selection_arkaplan.png` | 941×1672 | bölüm haritası |
| `Registration_arkaplan.png` | 941×1672 | kayıt ekranı |
| `grid.png` | 783×783 | oyun tahtası |
| `Entropitup.png` | 1536×1024 | katalizör göstergesi çerçevesi |

**13 ayrı panel/kart çerçevesi** (tam ekran arka planlar hariç 9 adet).
Kit bunların yerine **3 ölçek + modal** koyar (`panel_lg/md/sm`, `modal`).

**Önemli not:** `gamepanel.png` alt kenarında **iki boyalı buton yuvası**
taşıyor — bu yüzden duraklat kartına üçüncü bir buton yuva satırına
eklenemedi. 9-slice çerçeveye geçince bu kısıt kalkar.

## 3. Font kullanımı

| Font | Bileşen |
|---|---|
| **LiberationSans SDF** | **72** |
| Oxanium-Regular SDF | 52 |
| Oxanium-SemiBold SDF | 15 |
| Oxanium-Medium SDF | 9 |
| Orbitron-Bold SDF | 8 |
| Orbitron-Medium SDF | 8 |
| Orbitron-SemiBold SDF | 6 |

- **72 bileşen hâlâ TMP varsayılanı** (`LiberationSans SDF`) kullanıyor —
  marka fontu hiç atanmamış. Tek kalem en büyük görsel borç.
- **22 bileşen Orbitron** kullanıyor; Orbitron'un TTF'inde `ş Ş ğ Ğ İ` **yok**
  (bkz. `docs/DESIGN_TOKENS.md` §6). Bu 22 bileşen Oxanium'a taşınacak.
- Hiçbir statik atlasta Türkçe harfler yok; bugün dinamik fallback devrede.

## 4. Tema dışı renkler

### Kodda (`Assets/Scripts`)

| Renk | Kullanım | Yer |
|---|---|---|
| `#F1C40F` | 2 | `CodeStrings.cs:88,89` (ipucu alt bilgisi) |
| `#33CC33` | 1 | `UIManager.cs:635` (kombo 1) |
| `#FF9900` | 1 | `UIManager.cs:640` (kombo 2) |
| `#FF3333` | 1 | `UIManager.cs:645` (kombo 3) |
| `#2ECC70` | 1 | `LevelMenuManager.cs:16` (success'in 1 birim yanlış yazımı) |
| `#00B2D6` | 1 | `LevelMenuManager.cs:17` (primary'nin yanlış yazımı) |
| `#8FD9FF` | 1 | `UIManager.cs:382` (primaryBright'ın yanlış yazımı) |
| `#808080` | 1 | `SettingsManager.cs:14` |

### Sözlükte (`tr.json` / `en.json`)

| Renk | Kullanım |
|---|---|
| `#F1C40F` | 2 |
| `#48CAE4` | 1 |

### Sahnelerde (TMP yazı rengi)

| Renk | Bileşen | Not |
|---|---|---|
| `#F8F9FA` | 10 | beyaza çok yakın → TextPrimary |
| `#515961` | 6 | → TextSecondary |
| `#000000` | 6 | gölge/kontur |
| `#007DD8` | 4 | → Primary |
| `#C4FFB9` | 3 | açık yeşil |
| `#E4E1E1`, `#44E453`, `#D9D7D7`, `#5B5B5B`, `#C3FFB8`, `#48CAE4` | 1'er | tekil |

**Toplam 19 tema dışı renk değeri.** Üçü mevcut token'ın 1–2 birim yanlış
yazımı (`#2ECC70` ≠ `#2ECC71`, `#00B2D6` ≠ `#00B4D8`, `#8FD9FF` ≠ `#8FD8FF`).

---

## 5. 2d-2 tahmini

| İş | Kapsam |
|---|---|
| Buton görsellerinin kit ile değişmesi | **17 görsel → 1 çerçeve ailesi**, ~40 bileşen |
| Panel çerçevelerinin kit ile değişmesi | 9 çerçeve → 4 (panel_lg/md/sm + modal), ~14 panel |
| Font taşıma | **94 bileşen** (72 LiberationSans + 22 Orbitron) |
| Oxanium atlaslarının TR+EN ile yeniden üretimi | 3 asset |
| Tema dışı renklerin token'a çekilmesi | **19 değer**, 8 dosya |
| Dokunma hedefi 144 birime tamamlama | sayılacak (ikon butonları ve çipler) |
| Metni görselden çıkarma | 4 menü butonu → TMP etiketi |
| Lisans metni | `Assets/Fonts/OFL.txt` |

**Etkilenecek ekran:** ana menü, kayıt, profil, ansiklopedi, nasıl oynanır,
ayarlar, bölüm seçimi, oyun HUD'u, duraklat, quiz, oyun sonu, bölüm
tamamlandı, tarif defteri — **13 ekran**.
