# NEXUM — Varlık lisansları

Depo **herkese açık**. Üçüncü taraf varlıkların kaynağı ve lisansı burada
kayıtlıdır. Yeni bir dış varlık eklendiğinde **önce** bu dosyaya satırı
eklenir.

---

## 1. Fontlar — SIL Open Font License 1.1

Lisans metninin tamamı `Assets/Fonts/OFL.txt` içindedir (doğrulandı).
Telif satırları fontların kendi `name` tablosundan (nameID 0 ve 13) okundu.

| Font | Telif | Lisans |
|---|---|---|
| Oxanium (Regular / Medium / SemiBold) | Copyright 2019 The Oxanium Project Authors · github.com/sevmeyer/oxanium | SIL OFL 1.1 |
| Orbitron (Medium / SemiBold / Bold) | Copyright 2018 The Orbitron Project Authors, Reserved Font Name "Orbitron" · github.com/theleagueof/orbitron | SIL OFL 1.1 |
| Liberation Sans | Google 2010 · Red Hat 2012 | SIL OFL 1.1 |

> Orbitron metin rollerinden çekildi (TTF'inde `ş Ş ğ Ğ İ` yok) ama dosyalar
> depoda duruyor; lisans satırı bu yüzden korunuyor.

## 2. Kod ve eklentiler

| Varlık | Kaynak | Lisans |
|---|---|---|
| DOTween | Demigiant | DOTween ücretsiz sürüm lisansı (`Assets/Plugins/Demigiant` içindeki lisans dosyası geçerlidir) |
| TextMesh Pro | Unity Technologies | Unity Companion License (Unity paketi) |

## 3. Üretilen görseller — proje telifi

Aşağıdakiler bu depodaki Python üreteçleriyle **sıfırdan çizildi**; dış
kaynak kullanılmadı, telif projeye aittir.

| Varlık | Üreteç |
|---|---|
| Uygulama ikonu ve mağaza görselleri | `Tools/generate_icons.py` |
| Tepkime oku (`arrow_reaction.png`) | `Tools/generate_arrow.py` |
| Kilit ikonu (`lock_icon.png`) | `Tools/generate_lock.py` |
| İlerleme halkası (`ring_progress.png`) | `Tools/generate_ring.py` |
| Metal UI kiti ve taş sprite'ları (`Tools/ui-kit/`) | `Tools/generate_ui_kit.py` |

## 4. ⚠️ Doğrulanması gereken — `hint_assistant.png`

| | |
|---|---|
| Dosya | `Assets/Art/Figures/hint_assistant.png` |
| Eski ad | `vecteezy_detective-in-trench-coat-examining-clue-with-magnifying_65428683 (1).png` |
| Kaynak | **Vecteezy**, varlık no **65428683** (eski dosya adından) |
| Kullanım | İpucu asistanı butonu (`Game.unity` → `HintButton`) |
| Lisans | **BELİRSİZ — doğrulanmadı** |

**Durum:** Dosya adı dışında depoda lisans kanıtı yok; PNG'nin içinde de
lisans üstverisi bulunmuyor (`srgb`, `gamma`, `dpi` dışında alan yok).
Vecteezy'nin iki lisansı var ve ikisi farklı yükümlülük doğuruyor:

- **Free License** — ticari kullanım serbest ama **atıf zorunlu**
  (uygulama içinde veya mağaza açıklamasında "Designed by … from Vecteezy").
- **Pro License** — atıf gerekmez.

**Yapılması gereken (kullanıcıda):** indirme hesabından bu varlığın hangi
lisansla alındığı doğrulanmalı.

- Free ise: atıf metni oyuna eklenmeli (ör. Ayarlar → Hakkında) ve bu
  dosyaya yazılmalı.
- Pro ise: lisans/sipariş numarası bu dosyaya not edilmeli.
- İkisi de değilse görsel değiştirilmeli — yerine kit diliyle üretilecek bir
  asistan ikonu konabilir.

> Bu madde **yayın öncesi kapatılmalıdır**; `docs/RELEASE_CHECKLIST.md`
> listesine de girmiştir.

## 5. Diğer elle çizilmiş görseller

`Assets/Art/Avatars` altındaki 9 mentor portresi ve `Assets/Art/Figures`
altındaki arka planlar, panel çerçeveleri ve molekül çizimleri proje için
hazırlanmıştır. Bunlar için dış kaynak kaydı yoktur; dışarıdan alınmış bir
varlık olduğu sonradan anlaşılırsa bu dosyaya eklenmelidir.
