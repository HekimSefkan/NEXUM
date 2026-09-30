using UnityEngine;

namespace Nexum.Core
{
    /// <summary>
    /// Android'in donanımsal/jest geri hareketini (Escape) yönetir.
    /// Sahneye eklenmez: AppBootstrap gibi RuntimeInitializeOnLoadMethod ile
    /// gizli ve kalıcı bir obje olarak kurulur.
    ///
    /// Davranış:
    ///  - Ana menü: açık alt panel kapanır (iç içe panelde önce en üstteki),
    ///    kökte iken 2 saniyelik "çıkmak için tekrar bas" penceresi.
    ///  - Kayıt ekranı: kaydı iptal etmez, yalnızca açık alt paneli kapatır.
    ///  - Oyun: açık modal panel kapanır / uygun işlemi yapar; modal yoksa duraklat açılır,
    ///    duraklat açıkken geri basılırsa oyuna dönülür.
    ///
    /// GameManager.IsInputBlocked ile çakışmaz: swipe kilidi olduğu gibi kalır,
    /// geri tuşu burada ayrıca işlenir (modal açıkken de çalışır).
    ///
    /// Karar mantığı <see cref="HandleBackPress"/> içinde toplanmıştır; PlayMode testi
    /// tuşa basmadan bu metodu çağırarak davranışı doğrular.
    /// </summary>
    public static class BackButtonHandler
    {
        public const float ExitWindowSeconds = 2f;

        private static float lastBackPressTime = -100f;

        /// <summary>Geri basışını işler. Uygulamadan çıkış istendiyse true döner.</summary>
        public static bool HandleBackPress()
        {
            if (HandleGameScene()) return false;
            if (HandleRegistration()) return false;
            if (HandleMainMenu()) return false;

            return RequestExit();
        }

        /// <summary>Test yardımcısı: çıkış penceresini sıfırlar.</summary>
        public static void ResetExitWindow()
        {
            lastBackPressTime = -100f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            ResetExitWindow();

            GameObject host = new GameObject("BackButtonHandler")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            host.AddComponent<BackButtonRunner>();
            Object.DontDestroyOnLoad(host);
        }

        private class BackButtonRunner : MonoBehaviour
        {
            private void Update()
            {
                // Android'de geri hareketi Escape olarak gelir
                if (!Input.GetKeyDown(KeyCode.Escape)) return;

                if (HandleBackPress()) Application.Quit();
            }
        }

        // ---------- Oyun sahnesi ----------

        private static bool HandleGameScene()
        {
            UIManager ui = UIManager.Instance;
            if (ui == null) return false;

            // 1) Quiz: hak zaten tüketildi, geri basınca oyun sonu ekranına dönülür
            if (IsOpen(ui.quizPanel))
            {
                ui.quizPanel.SetActive(false);
                ui.ShowGameOver();
                return true;
            }

            // 2) Oyun sonu ve bölüm tamamlandı: ana menüye dön
            if (IsOpen(ui.gameOverPanel) || IsOpen(ui.winPanel))
            {
                ui.GoToMainMenu();
                return true;
            }

            // 3) Deney föyü: bölüm henüz başlamadı, menüye dön
            if (IsOpen(ui.tutorialPanel))
            {
                ui.GoToMainMenu();
                return true;
            }

            // 4) Küçük pop-up'lar
            if (IsOpen(ui.undoPanel)) { ui.HideUndoPanel(); return true; }
            if (IsOpen(ui.hypothesisPanel)) { ui.HideHypothesisPanel(); return true; }
            if (IsOpen(ui.assistantPanel)) { ui.HideHintMessage(); return true; }

            // 5) Duraklat: açıksa oyuna dön, kapalıysa aç
            ui.TogglePause(!IsOpen(ui.pausePanel));
            return true;
        }

        // ---------- Kayıt ekranı ----------

        private static bool HandleRegistration()
        {
            RegistrationManager reg = Object.FindObjectOfType<RegistrationManager>();
            if (reg == null || !IsOpen(reg.registrationPanel)) return false;

            // Mentor ayrıntı kartı açıksa yalnızca onu kapat; kayıt iptal edilmez
            if (IsOpen(reg.mentorDetailPanel))
            {
                reg.CloseMentorDetails();
            }

            // Kayıt ekranındayken geri tuşu uygulamayı kapatmaz
            return true;
        }

        // ---------- Ana menü ----------

        private static bool HandleMainMenu()
        {
            MainMenuManager menu = Object.FindObjectOfType<MainMenuManager>();
            if (menu == null) return false;

            // Panel geçişi sürerken basışı yok say (tween yarıda kalmasın)
            if (menu.IsSwitching) return true;

            // İç içe paneller: önce en üstteki kapanır
            ProfileManager profile = Object.FindObjectOfType<ProfileManager>();
            if (profile != null && IsOpen(profile.emailUpdatePanel))
            {
                profile.CloseEmailPopup();
                return true;
            }

            EncyclopediaManager encyclopedia = Object.FindObjectOfType<EncyclopediaManager>();
            if (encyclopedia != null && IsOpen(encyclopedia.detailPanel))
            {
                encyclopedia.CloseDetailPanel();
                return true;
            }

            HowToPlayManager howToPlay = Object.FindObjectOfType<HowToPlayManager>();
            if (howToPlay != null && IsOpen(howToPlay.guidePopupPanel))
            {
                howToPlay.CloseGuidePopup();
                return true;
            }

            if (!menu.IsAtMainMenuRoot)
            {
                menu.BackToMainMenu();
                return true;
            }

            return false;   // kökteyiz: çıkış akışına düş
        }

        // ---------- Çıkış ----------

        private static bool RequestExit()
        {
            if (Time.unscaledTime - lastBackPressTime <= ExitWindowSeconds)
            {
                lastBackPressTime = -100f;
                return true;   // ikinci basış: çık
            }

            lastBackPressTime = Time.unscaledTime;
            ShowExitHint();
            return false;
        }

        private static void ShowExitHint()
        {
            string message = Loc.Get(CodeStrings.BackPressAgain);

#if UNITY_ANDROID && !UNITY_EDITOR
            // Yeni sahne objesi gerektirmeyen tek geri bildirim yolu: Android'in kendi Toast'ı
            try
            {
                using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        using (AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast"))
                        using (AndroidJavaObject text = new AndroidJavaObject("java.lang.String", message))
                        {
                            AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>(
                                "makeText", activity, text, 0);
                            toast.Call("show");
                        }
                    }));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Toast gösterilemedi: " + e.Message);
            }

            Handheld.Vibrate();
#else
            Debug.Log("NEXUM_BACK: " + message);
#endif
        }

        private static bool IsOpen(GameObject panel)
        {
            return panel != null && panel.activeInHierarchy;
        }
    }
}
