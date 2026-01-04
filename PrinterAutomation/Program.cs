using System;
using System.Windows.Forms;
using PrinterAutomation.Forms;
using DevExpress.XtraEditors;
using DevExpress.LookAndFeel;
using DevExpress.Utils;

namespace PrinterAutomation
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                try
                {
                    WindowsFormsSettings.SetDPIAware();
                    // Vektör tabanlı skinleri etkinleştir
                    WindowsFormsSettings.EnableFormSkins();
                    // WXI Skin - Windows 11 stili, modern ve yuvarlatılmış köşeler
                    WindowsFormsSettings.DefaultLookAndFeel.SetSkinStyle("WXI");
                    // Alternatif olarak The Bezier skin'i de kullanılabilir:
                    // WindowsFormsSettings.DefaultLookAndFeel.SetSkinStyle("The Bezier");
                    
                    // Modern ScrollUIMode - Windows 11 stili ince scrollbar'lar (auto-hide)
                    WindowsFormsSettings.ScrollUIMode = DevExpress.XtraEditors.ScrollUIMode.Touch;
                    // Touch modu modern görünüm sağlar (Modern değeri bazı sürümlerde mevcut olmayabilir)
                }
                catch (Exception ex)
                {
                    // DevExpress lisans hatası durumunda varsayılan ayarlarla devam et
                    System.Diagnostics.Debug.WriteLine($"[Program] Skin ayarı hatası: {ex.Message}");
                    System.Console.WriteLine($"[Program] Skin ayarı hatası: {ex.Message}");
                }
                
                // Önce giriş ekranını göster
                System.Diagnostics.Debug.WriteLine("[Program] LoginForm oluşturuluyor...");
                System.Console.WriteLine("[Program] LoginForm oluşturuluyor...");
                
                LoginForm loginForm = null;
                try
                {
                    loginForm = new LoginForm();
                    System.Diagnostics.Debug.WriteLine("[Program] LoginForm oluşturuldu");
                    System.Console.WriteLine("[Program] LoginForm oluşturuldu");
                }
                catch (Exception loginEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Program] LoginForm oluşturulurken hata: {loginEx.Message}");
                    System.Console.WriteLine($"[Program] LoginForm oluşturulurken hata: {loginEx.Message}");
                    
                    MessageBox.Show(
                        $"Giriş ekranı oluşturulurken hata:\n\n{loginEx.Message}",
                        "Form Oluşturma Hatası",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    
                    return; // Programı sonlandır
                }
                
                // Giriş ekranını göster ve kontrol et
                DialogResult loginResult = DialogResult.Cancel;
                try
                {
                    loginResult = loginForm.ShowDialog();
                    System.Diagnostics.Debug.WriteLine($"[Program] LoginForm sonucu: {loginResult}");
                    System.Console.WriteLine($"[Program] LoginForm sonucu: {loginResult}");
                }
                catch (Exception loginShowEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Program] LoginForm gösterilirken hata: {loginShowEx.Message}");
                    System.Console.WriteLine($"[Program] LoginForm gösterilirken hata: {loginShowEx.Message}");
                    
                    MessageBox.Show(
                        $"Giriş ekranı gösterilirken hata:\n\n{loginShowEx.Message}",
                        "Giriş Hatası",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    
                    return; // Programı sonlandır
                }
                finally
                {
                    // LoginForm'u kapat
                    if (loginForm != null)
                    {
                        loginForm.Dispose();
                    }
                }
                
                // Eğer giriş başarısızsa programı kapat
                if (loginResult != DialogResult.OK)
                {
                    System.Diagnostics.Debug.WriteLine("[Program] Giriş başarısız, program kapatılıyor");
                    System.Console.WriteLine("[Program] Giriş başarısız, program kapatılıyor");
                    return;
                }
                
                // Giriş başarılı - MainForm'u göster
                System.Diagnostics.Debug.WriteLine("[Program] MainForm oluşturuluyor...");
                System.Console.WriteLine("[Program] MainForm oluşturuluyor...");
                
                MainForm mainForm = null;
                try
                {
                    mainForm = new MainForm();
                    System.Diagnostics.Debug.WriteLine("[Program] MainForm oluşturuldu");
                    System.Console.WriteLine("[Program] MainForm oluşturuldu");
                }
                catch (Exception formEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Program] MainForm oluşturulurken hata: {formEx.Message}");
                    System.Console.WriteLine($"[Program] MainForm oluşturulurken hata: {formEx.Message}");
                    System.Console.WriteLine($"[Program] StackTrace: {formEx.StackTrace}");
                    
                    MessageBox.Show(
                        $"Form oluşturulurken hata:\n\n{formEx.Message}\n\nStack Trace:\n{formEx.StackTrace}",
                        "Form Oluşturma Hatası",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    
                    return; // Programı sonlandır
                }
                
                if (mainForm == null)
                {
                    System.Diagnostics.Debug.WriteLine("[Program] MainForm null!");
                    System.Console.WriteLine("[Program] MainForm null!");
                    MessageBox.Show("MainForm oluşturulamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("[Program] Application.Run başlatılıyor...");
                System.Console.WriteLine("[Program] Application.Run başlatılıyor...");
                
                try
                {
                    Application.Run(mainForm);
                    System.Diagnostics.Debug.WriteLine("[Program] Application.Run tamamlandı");
                    System.Console.WriteLine("[Program] Application.Run tamamlandı");
                }
                catch (Exception runEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Program] Application.Run hatası: {runEx.Message}");
                    System.Console.WriteLine($"[Program] Application.Run hatası: {runEx.Message}");
                    System.Console.WriteLine($"[Program] StackTrace: {runEx.StackTrace}");
                    
                    MessageBox.Show(
                        $"Uygulama çalıştırılırken hata:\n\n{runEx.Message}\n\nStack Trace:\n{runEx.StackTrace}",
                        "Uygulama Hatası",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Program] KRİTİK HATA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Program] StackTrace: {ex.StackTrace}");
                System.Console.WriteLine($"[Program] KRİTİK HATA: {ex.Message}");
                System.Console.WriteLine($"[Program] StackTrace: {ex.StackTrace}");
                
                MessageBox.Show(
                    $"Program başlatılırken kritik bir hata oluştu:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Kritik Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}

