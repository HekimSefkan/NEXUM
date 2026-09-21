# NEXUM# 🧪 NEXUM: Kimya Akademisi

**NEXUM**, kimya bilimi ile bulmaca dinamiklerini bir araya getiren, 7 seviyeli ve 4x4 ızgara (grid) tabanlı bir mobil strateji ve sentez oyunudur. Oyuncular, temel elementleri birleştirerek daha karmaşık bileşikleri sentezler ve akademi basamaklarında yükselirler.

## 🚀 Proje Özeti
- **Geliştirici:** Hekim Sefkan Elik
- **Platform:** Android (Mobil Öncelikli - Dikey Ekran)
- **Oyun Motoru:** Unity 2022.3.62f3 (LTS)
- **Dil & Teknolojiler:** C#, DOTween (Animasyonlar), Regex (Veri Doğrulama)
- **Durum:** Geliştirme Aşamasında (Aktif)

## 🧰 Projeyi Açma
- Unity Hub > **Add** ile proje klasörünü ekleyin ve **Unity 2022.3.62f3** ile açın.
- DOTween repoda dahildir (`Assets/Plugins/Demigiant/DOTween`); ayrıca kurulum gerekmez.

## 🎮 Temel Oyun Mekanikleri

* **Grid Tabanlı Sentez (4x4):** 2048 tarzı kaydırma (swipe) mekanikleriyle elementler birleştirilir. Sadece doğru kimyasal reaksiyon tariflerine uyan elementler sentezlenebilir.
* **Tersinir Tepkime (Undo):** Oyuncular stratejik hatalarını, laboratuvar bütçesinden puan harcayarak geri alabilirler.
* **Entropi ve Kaza Sistemi:** Boşa yapılan her kaydırma hamlesi laboratuvardaki basıncı (Entropi) artırır. 5 boş hamle sonucunda bir laboratuvar kazası yaşanır ve ızgaraya ceza elementi eklenir. Kamera sarsıntısı ve cihaz titreşimi (Haptics) ile oyuncuya geri bildirim verilir.
* **Laboratuvar Asistanı (İpucu Sistemi):** Oyuncular, puanları karşılığında asistanlarından sıradaki hamle için görsel (DOTween vurgulu) ve bilimsel metin tabanlı ipuçları alabilir.

## ⚙️ Oyun Modları
1. **Normal Mod:** Standart ilerleme ve puanlama.
2. **Sınav Modu:** İpucu sisteminin devre dışı kaldığı, oyuncunun sadece kendi kimya bilgisine güvendiği yüksek zorluk ve x2 puan modu.
3. **Serbest Mod:** Entropi (Kaza) sisteminin olmadığı, stressiz sandbox laboratuvar deneyimi.

## 🛠️ Teknik Altyapı ve Mimariler

* **Mobil Optimizasyon & UI:** Ekran çözünürlüğünden bağımsız, tüm Android cihazlarda mükemmel oran (1:1 Grid) sunan Aspect Ratio Fitter ve Auto-Anchor destekli Canvas mimarisi.
* **Gelişmiş Girdi (Input) Sistemi:** Bilgisayar testleri için klavye girdileri ve mobil cihazlar için hassas dokunmatik (Swipe) algılama matematiği tek bir `GameManager` üzerinden yönetilir.
* **Kayıt ve Güvenlik Sistemi:** `PlayerPrefs` tabanlı yerel veri tabanı. Kayıt ekranında oyuncu verileri (E-posta), endüstri standardı **Regex** ile doğrulanarak temiz veri akışı sağlanır.
* **Akıcı Animasyonlar:** DOTween kütüphanesi ile obje yaratılma, esneme (PunchScale), birleşme ve UI pop-up animasyonları performanstan ödün vermeden asenkron olarak yönetilir.

## 🪪 NEXUM Kimlik Kartı (Profil Sistemi)
Oyuncular sisteme kayıt olurken bilim tarihindeki ünlü kimyagerleri mentor olarak seçer. Sistem, oyuncuya özel bir `NX-####` ID'si atar ve oyun içi istatistikleri (Toplam Sentez, Keşfedilen Seviye, Kaza Sayısı, Quiz Başarısı) dinamik bir profil kartında sergiler.