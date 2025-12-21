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
                    // FormSkins ve LookAndFeel kapatıldı - renk sorunlarını önlemek için
                    // WindowsFormsSettings.EnableFormSkins();
                    // WindowsFormsSettings.DefaultLookAndFeel.SetSkinStyle("Office 2019 Colorful");
                }
                catch
                {
                    // DevExpress lisans hatası durumunda varsayılan ayarlarla devam et
                }
                
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

