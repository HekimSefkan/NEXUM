# NEXUM – Yayın Kontrol Listesi

Her Google Play sürümünden önce bu listeyi baştan sona uygula. Komutlar **Windows PowerShell** içindir ve proje kökünden (`C:\Projects\NEXUM`) çalıştırılır. Unity Editor **kapalı** olmalı.

---

## 1. Teknik kontroller

### 1.1 Depo durumu
```powershell
Set-Location "C:\Projects\NEXUM"; git status --short; git log --oneline -5
```
Çalışma ağacı temiz olmalı, yayın alınacak dal `main` ile güncel olmalı.

### 1.2 Doğrulama aracı
```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "C:\Projects\NEXUM" -executeMethod NexumValidation.Run -logFile "C:\Projects\NEXUM\Logs\validate.log" | Out-Null
Select-String -Path "C:\Projects\NEXUM\Logs\validate.log" -Pattern "NEXUM_VALIDATION|error CS" | ForEach-Object { $_.Line }
```
Beklenen: `NEXUM_VALIDATION: OK`, **0 uyarı**, `error CS` yok.

Validator'ın yayınla ilgili kontrolleri: Target API "Automatic" mi, ikon slotları boş mu, versionName/versionCode varsayılan mı, `docs/privacy-policy.html` duruyor mu, atlasa giren dokularda çift sıkıştırma var mı, doku çözünürlüğü kullanıma göre yetersiz mi.

### 1.3 PlayMode testleri
```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -runTests -projectPath "C:\Projects\NEXUM" -testPlatform PlayMode -testResults "C:\Projects\NEXUM\Logs\tests.xml" -logFile "C:\Projects\NEXUM\Logs\tests.log" | Out-Null
Select-String -Path "C:\Projects\NEXUM\Logs\tests.xml" -Pattern 'result="(Passed|Failed)"' | Select-Object -First 1
Set-Location "C:\Projects\NEXUM"; git status --short
```
Hepsi geçmeli. **Not:** PlayMode testi `ProjectSettings.asset` içindeki `runInBackground` değerini 1 yapar; commit etmeden `git restore ProjectSettings/ProjectSettings.asset` ile geri al.

### 1.4 Build ayarları (build öncesi doğrula)
| Ayar | Beklenen |
|---|---|
| Scripting backend | IL2CPP |
| Target architectures | **ARM64** (ARMv7 kapalı) |
| Target API Level | **36** (Automatic değil) |
| Minimum API Level | 22 |
| Scripting define | `DOTWEEN` |
| Development Build | kapalı |
| İkon slotları | Adaptive / Round / Legacy → 18/18 dolu |
| Açılış ekranı | NEXUM logosu ekli (Unity logosu Personal lisansta kalır) |
| Ekran yönü | yalnızca Portrait |
| Paket adı | `com.hekimsefkan.nexum` |

Build scripti bu ayarları çalıştırma anında loglar (`NEXUM_BUILD_SETTINGS` satırı) — build çıktısında doğrula.

### 1.5 Sürüm numarası
Her Play yüklemesinde **versionCode benzersiz ve bir öncekinden büyük** olmalı.

1. Unity → *Edit → Project Settings → Player → Other Settings → Identification*
2. **Version** (versionName): kullanıcıya görünen sürüm, örn. `1.0` → `1.1`
3. **Bundle Version Code** (versionCode): tam sayı, örn. `1` → `2` (her yüklemede mutlaka artır)
4. Aynı versionCode ile ikinci kez yükleme yapılamaz; yanlışlıkla atlanan numaralar sorun değildir.

### 1.6 Paketleri üret
```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "C:\Projects\NEXUM" -executeMethod NexumBuild.BuildAndroidApk -logFile "C:\Projects\NEXUM\Logs\build.log" | Out-Null
Select-String -Path "C:\Projects\NEXUM\Logs\build.log" -Pattern "NEXUM_BUILD" | ForEach-Object { $_.Line }
Get-ChildItem "C:\Projects\NEXUM\Builds\*.a*" | Select-Object Name, @{n='MiB';e={[math]::Round($_.Length/1MB,2)}}
```
`.aab` Play'e yüklenir, `.apk` cihazda denemek içindir. `Builds/` git'e girmez.

### 1.7 Cihazda duman testi
- Uygulama açılıyor, açılış logosu görünüyor.
- Launcher ikonu doğru (adaptive maske düzgün kırpıyor).
- Bir bölüm baştan sona oynanıyor; kazanma ve oyun sonu ekranları çalışıyor.
- Ses açma/kapama, ilerleme sıfırlama çalışıyor.
- Profil ekranındaki sayaçlar güncelleniyor.

---

## 2. Google Play Console adımları

### 2.1 İlk sürümde bir kez
- [ ] Geliştirici hesabı doğrulaması tamam.
- [ ] Uygulama oluştur: ad, varsayılan dil, uygulama/oyun ve ücretsiz/ücretli seçimi.
- [ ] **Mağaza ikonu:** `Tools/store/icon_store_512.png` (512×512, 32-bit PNG).
- [ ] **Öne çıkan görsel:** `Tools/store/feature_graphic_1024x500.png` (1024×500).
- [ ] **Ekran görüntüleri:** en az 2, en fazla 8 telefon görüntüsü.
- [ ] Kısa açıklama (en çok 80 karakter) ve tam açıklama.
- [ ] **Gizlilik politikası URL'si:** `https://hekimsefkan.github.io/NEXUM/privacy-policy.html`
- [ ] **Data safety** formu: veri toplanmıyor, paylaşılmıyor (uygulamanın internet izni yok).
- [ ] İçerik derecelendirme anketi.
- [ ] Hedef kitle ve içerik beyanı.
- [ ] Reklam beyanı: uygulama reklam içermiyor.
- [ ] Uygulama erişimi: giriş gerekmiyor.
- [ ] **İmzalama:** release keystore ile imzalanmış AAB (aşağıdaki uyarıyı oku).
- [ ] Bireysel geliştirici hesabıysa: kapalı test şartı (12 test kullanıcısı / 14 gün) tamam.

### 2.2 Her sürümde
- [ ] versionCode artırıldı (1.5).
- [ ] Yeni `.aab` yüklendi.
- [ ] Sürüm notları (tüm diller için) yazıldı.
- [ ] Test kanalında denendi, sonra üretime yükseltildi.
- [ ] Gizlilik politikası hâlâ doğru mu? (Yeni veri toplayan bir özellik eklendiyse önce politikayı güncelle.)

---

## 3. Keystore uyarısı

> **Release keystore dosyanı ve şifrelerini kaybedersen bu uygulamaya bir daha güncelleme yayınlayamazsın.**
> Google Play, güncellemenin ilk sürümle aynı anahtarla imzalanmasını şart koşar.

- Keystore dosyasını **depoya koyma** (`.gitignore` `*.keystore` ve `*.jks` dosyalarını engeller).
- En az iki ayrı güvenli yerde yedekle (ör. şifreli disk + parola yöneticisi).
- Şifreleri parola yöneticisinde sakla; düz metin dosyada tutma.
- Play App Signing'i açarsan Google imzalama anahtarını saklar; yükleme anahtarı kaybolursa sıfırlama talep edilebilir. Yine de yedek şart.

---

## 4. Mağaza görsellerini yeniden üretme

```powershell
Set-Location "C:\Projects\NEXUM"; python Tools\generate_icons.py
```
İkon ve mağaza görselleri `Tools/generate_icons.py` ile üretilir; renk, harf ve alt başlık scriptin başındaki `PARAMS` ve `FEATURE` sözlüklerinden değiştirilir. Uygulama ikonu görselleri değişirse slotları yeniden uygula:

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "C:\Projects\NEXUM" -executeMethod NexumIconSetup.Apply -logFile "C:\Projects\NEXUM\Logs\icons.log" | Out-Null
```
