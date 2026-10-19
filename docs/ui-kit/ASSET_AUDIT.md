# NEXUM — Görsel varlık denetimi (Faz 1 / Adım 2d-2)

`Assets/Art` altındaki **74 görsel** tek tek değerlendirildi.
Karar: **KORU** (olduğu gibi kalır) · **UYARLA** (mevcut görsel kullanılır ama
Metal kitine göre tonlanır / yeniden çerçevelenir / yeniden ölçeklenir) ·
**YENİDEN ÜRET** (üreteçten çıkacak).

| Karar | Adet |
|---|---|
| KORU | **31** |
| UYARLA | **14** |
| YENİDEN ÜRET | **29** |

---

## 1. Arka planlar — KORU

| Dosya | Nerede | Karar | Gerekçe |
|---|---|---|---|
| `arkaplan.png` 941×1672 | ana menü, tarif defteri | **KORU** + vinyet | Elle çizilmiş laboratuvar derinliği; prosedürel üretim bu kaliteyi yakalayamaz. Vinyet zaten kitte var. |
| `level_selection_arkaplan.png` | bölüm haritası | **KORU** + vinyet | aynı |
| `Registration_arkaplan.png` | kayıt | **KORU** + vinyet | aynı |
| `Nasiloyanir_arkaplan1/2.png` | nasıl oynanır | **UYARLA** | Arka plan değil **çerçeve**; `PreserveAspect` ile içe sığdırılıyor. Panel çerçevesi kitten gelince bunlar yalnızca doku katmanı olarak kalır. |
| `grid.png` 783×783 | oyun tahtası | **UYARLA** | Kaynak çözünürlüğü kullanımın (900 birim) altında — zaten bilinen borç. Kit `panel_lg` çerçevesi tahtayı sarar, `grid.png` iç doku olarak alta düşer. |
| `white-screen.png` | tam ekran karartma | **KORU** | Düz beyaz dolgu; üretmeye değmez. |

## 2. Marka — KORU

| Dosya | Karar | Gerekçe |
|---|---|---|
| `NEXUM_logo.png` 1366×320 | **KORU** | Marka varlığı. Üretilmez. |
| `NEXUM_logo-.png` 512×519 | **KORU** | Kare sürüm (açılış ekranı / ikon). |
| `Assets/Art/Icons/*` (10 dosya) | **KORU** | `Tools/generate_icons.py` üretiyor, zaten vektör ve kit dilinden bağımsız (mağaza/launcher ikonu). |

## 3. Butonlar — YENİDEN ÜRET

| Dosya | Karar | Gerekçe |
|---|---|---|
| `oyna.png`, `nasıloynanır.png`, `ansiklopedi.png`, `ayarlar.png` | **YENİDEN ÜRET** | **Metin görselin içinde basılı** → İngilizce sürüm imkânsız, yeni buton eklenemiyor. Kit: `btn_primary` / `btn_secondary` + TMP etiketi. |
| `game_button.png`, `game_button1.png` | **YENİDEN ÜRET** | Kapsül siluet kit diline uymuyor; iki ayrı görsel tek 9-slice çerçeveye iner. |
| `Nasiloyanir_Buton.png` | **YENİDEN ÜRET** | 2d-1'de geçici olarak tarif defteri butonu yapılmıştı; kit gelince gerek kalmıyor. |
| `SettingsPanel_modsecimkartı.png` | **YENİDEN ÜRET** | 9 yerde kullanılıyor, hepsi `btn_secondary` / `tab` oluyor. |
| `Registration_girişyapbutonu.png` | **YENİDEN ÜRET** | → `btn_primary` |
| `Registration_isim_butonu.png`, `Registration_emailbuton.png` | **YENİDEN ÜRET** | → `input_field` |
| `level_selection_button.png` | **YENİDEN ÜRET** | → `level_node_open` |
| `level_selection_kilitlibutton.png` | **YENİDEN ÜRET** | Kilit ikonu görsele basılı → `level_node_locked` + ayrı kilit ikonu. |
| `SettingsPanel_toogleacık/kapalı.png` | **YENİDEN ÜRET** | → `toggle_on` / `toggle_off` |

## 4. Panel / kart çerçeveleri — YENİDEN ÜRET

| Dosya | Karar | Gerekçe |
|---|---|---|
| `gamepanel.png` | **YENİDEN ÜRET** | **Alt kenarında iki BOYALI buton yuvası var.** Duraklat kartına üçüncü butonu koyamamamızın ölçülmüş sebebi. Kit panelinde yuva yok, butonlar serbest yerleşir. |
| `SettingsPanel_.png` | **YENİDEN ÜRET** | → `panel_lg` |
| `Registration_cartblue.png`, `Registration_avatarkartı.png` | **YENİDEN ÜRET** | → `panel_md` / `avatar_frame` |
| `ProfilPanel_KimlikKartı.png` | **YENİDEN ÜRET** | → `panel_lg` + avatar çerçevesi |
| `ProfilPanel_KeşfedilenBileşik / ToplamSentez / LaboratuvarKazasi / TeorikBasarı` | **YENİDEN ÜRET** | Dört ayrı kart görseli; hepsi aynı `panel_sm` + ikon olur. İçlerindeki simgeler (erlen, kıvılcım) **UYARLA** olarak ikon kütüphanesine girer. |
| `avatar_cerceve.png` | **YENİDEN ÜRET** | → `avatar_frame` |

## 5. İkonlar — UYARLA

| Dosya | Karar | Gerekçe |
|---|---|---|
| `geri.png` 497×479 | **UYARLA** | Ok biçimi iyi; kit `icon_button` çerçevesinin içine alınır, rengi temadan tonlanır. |
| `x.png` 485×467 | **UYARLA** | aynı |
| `Tik.png` 1024×1024 | **UYARLA** | 9 vurgu halkası; kit rengine tonlanır, boyutu düşürülür (1024 kaynak, ~100 birim kullanım). |
| `Cember.png` 2287×2287 | **YENİDEN ÜRET** | Dolu daire; radyal dolgu uygulanınca pasta dilimi veriyordu. `ring_progress.png` zaten yerine geçti. |
| `arrow_reaction.png`, `lock_icon.png`, `ring_progress.png` | **KORU** | Bu dönemde üretildiler, zaten kit diliyle uyumlu (tek renk beyaz, tonlanabilir). `lock_overlay` kit sürümü alternatif olarak hazır. |

## 6. Göstergeler — karar ve gerekçe

| Dosya | Karar | Gerekçe |
|---|---|---|
| `Entropitup.png` (altın deney tüpü) 1536×1024 | **UYARLA** | Tek başına kalmıyor: Metal kitinin **ikincil vurgu rengi altın** seçildi, yani tüp artık kitin rengine ait. Ama 600×60 birimlik bir barda 1536×1024 bir tüp görseli kullanmak hem ölçüsüz hem bulanık. Karar: tüp **dekoratif çerçeve** olarak korunur, dolum çubuğu kitin `progress_track` / `progress_fill` sprite'larına taşınır. |
| `Beherglas.png` 1024×1536 (hedef beheri) | **UYARLA** | Hedef göstergesi oyunun kimya kimliğinin parçası; atılmaz. Ama bugün `BeakerGoalPrefab`'ın etiket kutusu kabı 22 birim aşıyor (bilinen borç). Karar: beher görseli korunur, **sayaç kitin `chip` sprite'ına taşınır** — böylece taşma kaynağı (serbest etiket) ortadan kalkar. |

## 7. Karakter / avatar görselleri

| Dosya | Karar | Gerekçe |
|---|---|---|
| 9 mentor portresi (`Avatars/*.png`, 1024×1024, ~2 MB/adet) | **KORU** + çerçeve UYARLA | Portreler elle çizilmiş, üretilemez. Ama kullanım 663×724 birim ve kaynak 1024 — bilinen çözünürlük borcu. Çerçeve `avatar_frame` kitine geçer. |
| `vecteezy_detective...png` 1024×1024 (ipucu asistanı) | **UYARLA — yerleşim düzeltilmeli** | Oyun sahnesinde `HintButton` olarak kullanılıyor ve **sağ kenardan taşıp kırpılıyor**. Ayrıca dosya adı bir stok görsel kimliği taşıyor (`vecteezy_...65428683 (1).png`) — repo PUBLIC, adı sadeleştirilmeli ve lisansı doğrulanmalı. Karar: görsel korunur, **`icon_button` içine alınır ve yeniden çerçevelenir**; dosya adı 2d-3'te düzeltilir. |

## 8. Deney föyü / molekül görselleri — KORU

| Dosya | Karar | Gerekçe |
|---|---|---|
| `H2O.png`, `CO2.png`, `NH3.png`, `NaCl.png`, `CaO.png`, `CaCO3.png`, `F2CO3.png`, `H2CO3_Mavi/Yeşil.png`, `Su damla.png`, `Su_Formul.png`, `CO2_formul.png`, `NaCl_Formul.png` (13 dosya) | **KORU** | Tutorial/deney föyü molekül çizimleri. Bilgi taşıyorlar, çerçeve değiller; kit dilinden bağımsız. `maxTextureSize` 512 istisnası zaten belgelenmiş. |

## 9. Taşlar — YENİDEN ÜRET

| Dosya | Karar | Gerekçe |
|---|---|---|
| `1.seviyetas.png`, `2.seviyetas.png`, `3.seviyetas.png` | **YENİDEN ÜRET** | Merkezleri parçacıklı küre; formülü yutuyor. Yeni taşlar: sakin merkez, seviye çerçeveden. **Ayrıca:** bu üç sprite prefab'lara **elle** atanmış ve 4 taşta türetilen seviyeyle çelişiyor (bkz. rapor). |

---

## Ön görüşe itirazlar

Verilen ön görüşün çoğuna katılıyorum. İki yerde farklı düşünüyorum, gerekçesiyle:

1. **`Nasiloyanir_arkaplan1/2.png` "arka plan" değil.** Bunlar çerçeve
   görselleri (bu yüzden `PreserveAspect` ile sığdırılıyorlar, ARF Envelope
   ile değil). "Arka planları KORU" kuralına girmiyorlar; panel çerçevesi
   kitten gelince doku katmanına inerler.
2. **`Entropitup.png` ve `Beherglas.png` için "ya hep ya hiç" yok.** İkisi de
   oyunun kimya kimliğini taşıyor ve atılırsa arayüz jenerikleşir. Çözüm
   ikisini de korumak ama **ölçülebilir parçalarını** (dolum çubuğu, sayaç
   etiketi) kite taşımak. Böylece hem kimlik kalır hem bugünkü iki ölçüm
   borcu (bulanık tüp, taşan beher etiketi) kapanır.
