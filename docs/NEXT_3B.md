# Sonraki tura kalanlar

Bu dosya, bir turda **görülen ama o turun kapsamına girmeyen** bulguları
tutar. Tur açmak için değil, unutmamak için.

---

## 3c turundan (son görsel tur)

### Kullanılmaz hâle gelen dosyalar (silinmedi, listelendi)

| Dosya | Neden |
|---|---|
| `Assets/Art/Figures/hint_assistant.png` | dedektif karakteri sahneden çıktı (3c/3) |
| `Assets/Prefabs/BeakerGoalPrefab.prefab` | hedef kutusu artık hedefin kendi taşı (3c/3) |
| `Assets/Editor/NexumBeakerGoal.cs` | yukarıdaki prefab'ın kurucusu |
| `Assets/Art/Figures/Beherglas.png` | yalnızca beher prefab'ında kullanılıyordu |
| `Assets/Art/Figures/background_c_main.png` | tek arka plana geçildi (3c/5) |
| `Assets/Art/Figures/background_c_level.png` | aynı |
| `Assets/Art/Figures/background_c_register.png` | aynı |
| `Assets/Art/Figures/arkaplan.png` | aynı (zaten sahnede değildi) |
| `Assets/Art/Figures/Registration_arkaplan.png` | aynı |
| `Assets/Art/Figures/Nasiloyanir_arkaplan1.png` | aynı |
| `Assets/Art/Figures/Nasiloyanir_arkaplan2.png` | aynı |
| `Assets/Art/Figures/level_selection_arkaplan.png` | aynı |
| `Assets/Art/Figures/ring_progress.png` (kart kullanımı) | ansiklopedi çemberi kit üretecine geçti; tarif defterinin ilerleme halkasında HÂLÂ kullanılıyor, silinmez |

Referanssız asset build'e girmez; bunlar APK boyutunu şu an şişirmiyor.
Silme kararı kullanıcınındır.

### Cihaz görüntüsünde görünmeyen çalışma zamanı içeriği

`mentor-karti` ve ansiklopedi ayrıntı kartı görüntülerinde metinler
`New Text`, avatar beyaz kutu görünüyor: bu içerik yalnızca çalışma
zamanında doluyor. 3c'de hedef satırı ve ansiklopedi listesi için edit
mode doldurucu yazıldı (`NexumDeviceShot.HedefleriDoldur` /
`AnsiklopediyiDoldur`); mentor kartı ve kimlik kartı için aynısı
yazılabilir.

### Dokunma alanı çakışması (3c'de yan etki olarak düzeldi)

`ScoreButton` ile `TipButton`'ın dokunma alanları 1080 genişlikte
kesişiyordu (x 275..353, y 439..522). Skor göstergesi karakterin
boşalttığı sol alana inince çakışma kalmadı. Çakışmayı hangi koşulda
testin kaçırdığı incelenmedi.

### Quiz paneli zemini

Şık kutuları kit sprite'ına geçti ve tinti beyaza alındı (3c/7 düzeltmesi),
ama panelin kendi zemini (`QuizPanel/Image`) hâlâ eski gri yüzey. Kit
`modal_normal`'a geçmesi ayrı bir iş.

### Ana menüde logo yok

`MainMenuPanel` içinde NEXUM logosu yok; logo yalnızca alt panellerde
(ansiklopedi, nasıl oynanır, tarif defteri, kayıt) duruyor. 3c/8'de avatar
çerçevesi buton bandına hizalandı — logoya hizalanacak bir logo yoktu.
