using System;
using System.Windows.Forms;
using PrinterAutomation.Forms;
using DevExpress.XtraEditors;

namespace PrinterAutomation
{
    internal static class Program
    {
        [STAThread]
        static void Main()
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
            
            Application.Run(new MainForm());
        }
    }
}

