using System;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.Utils;
using PrinterAutomation.Models;
using PrinterAutomation.Services;
using MongoDB.Driver;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace PrinterAutomation.Forms
{
    public enum ThemeMode
    {
        Light,
        Dark
    }

    public class MainForm : System.Windows.Forms.Form
    {
        private readonly PrinterService _printerService;
        private readonly OrderService _orderService;
        private readonly JobAssignmentService _jobAssignmentService;
        private readonly MongoDbService? _mongoDbService;
        private System.Windows.Forms.Timer _refreshTimer;
        private ThemeMode _currentTheme = ThemeMode.Light;
        private bool _mongoDbConnected = false;

        private GridControl gridControlPrinters;
        private GridView gridViewPrinters;
        private GridControl gridControlOrders;
        private GridView gridViewOrders;
        private GridControl gridControlJobs;
        private GridView gridViewJobs;
        private SimpleButton btnSimulateOrder;
        private SimpleButton btnToggleTheme;
        private SimpleButton btnAddPrinter;
        private SimpleButton btnSettings;
        private System.Windows.Forms.Panel settingsPanel;
        private bool _settingsPanelVisible = false;
        private SimpleButton btnDeleteCompletedOrders;
        private SimpleButton btnDeleteCompletedJobs;
        private SimpleButton btnShowEarnings;
        private SimpleButton btnShowModels;
        private LabelControl lblStatus;
        private LabelControl lblTitle;
        private LabelControl lblPrinters;
        private LabelControl lblOrders;
        private LabelControl lblJobs;
        private LabelControl lblStats;
        private LabelControl lblTotalPrinters;
        private LabelControl lblActivePrinters;
        private LabelControl lblTotalOrders;
        private LabelControl lblPendingJobs;
        private System.Windows.Forms.Label lblTotalEarnings;
        private System.Windows.Forms.Panel titlePanel;
        private System.Windows.Forms.Panel printersHeaderPanel;
        private System.Windows.Forms.Panel ordersHeaderPanel;
        private System.Windows.Forms.Panel jobsHeaderPanel;
        private System.Windows.Forms.Panel statsPanel;
        private System.Windows.Forms.FlowLayoutPanel printersIconPanel;
        private System.Collections.Generic.Dictionary<int, System.Windows.Forms.Panel> printerIconPanels;
        private System.Collections.Generic.Dictionary<int, System.EventHandler> printerPanelClickHandlers;
        private bool _isDetailsFormOpen = false;
        private System.Windows.Forms.Panel contentPanel;

        public MainForm()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainForm] Constructor başladı");
                System.Console.WriteLine("[MainForm] Constructor başladı");
                
                // ÖNCE InitializeComponent çağrılmalı ki MessageBox çalışsın
                System.Diagnostics.Debug.WriteLine("[MainForm] InitializeComponent çağrılıyor...");
                System.Console.WriteLine("[MainForm] InitializeComponent çağrılıyor...");
                
                try
                {
                    InitializeComponent();
                    System.Diagnostics.Debug.WriteLine("[MainForm] InitializeComponent tamamlandı");
                    System.Console.WriteLine("[MainForm] InitializeComponent tamamlandı");
                }
                catch (Exception initEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainForm] InitializeComponent hatası: {initEx.Message}");
                    System.Console.WriteLine($"[MainForm] InitializeComponent hatası: {initEx.Message}");
                    System.Console.WriteLine($"[MainForm] InitializeComponent StackTrace: {initEx.StackTrace}");
                    throw; // InitializeComponent hatası kritik, programı durdur
                }
                
                // MongoDB servisini başlat
                MongoDbService mongoDbService = null;
                bool mongoDbConnected = false;
                
                try
                {
                    System.Diagnostics.Debug.WriteLine("[MainForm] MongoDbService oluşturuluyor...");
                    System.Console.WriteLine("[MainForm] MongoDbService oluşturuluyor...");
                    mongoDbService = new MongoDbService();
                    mongoDbConnected = mongoDbService.IsConnected();
                    System.Diagnostics.Debug.WriteLine($"[MainForm] MongoDbService oluşturuldu, bağlantı: {mongoDbConnected}");
                    System.Console.WriteLine($"[MainForm] MongoDbService oluşturuldu, bağlantı: {mongoDbConnected}");
                }
                catch (Exception ex)
                {
                    mongoDbConnected = false;
                    System.Diagnostics.Debug.WriteLine($"[MainForm] MongoDB bağlantı hatası: {ex.Message}");
                    System.Console.WriteLine($"[MainForm] MongoDB bağlantı hatası: {ex.Message}");
                    System.Console.WriteLine($"[MainForm] MongoDB StackTrace: {ex.StackTrace}");
                }
                
                // MongoDB servisini sakla
                _mongoDbService = mongoDbService;
                
                // MongoDB durumunu sakla (status label'da göstermek için)
                _mongoDbConnected = mongoDbConnected;
                
                System.Diagnostics.Debug.WriteLine($"[MainForm] MongoDB servisi durumu: {(mongoDbService != null ? "MEVCUT" : "NULL")}");
                System.Diagnostics.Debug.WriteLine($"[MainForm] MongoDB bağlantı durumu: {(mongoDbConnected ? "BAĞLI" : "BAĞLI DEĞİL")}");
                
                try
                {
                    _printerService = new PrinterService(mongoDbService);
                    System.Diagnostics.Debug.WriteLine("[MainForm] PrinterService oluşturuldu");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainForm] PrinterService oluşturulurken hata: {ex.Message}");
                    System.Console.WriteLine($"[MainForm] PrinterService oluşturulurken hata: {ex.Message}");
                    XtraMessageBox.Show(
                        $"PrinterService oluşturulurken hata oluştu:\n{ex.Message}\n\nProgram devam edecek ancak bazı özellikler çalışmayabilir.",
                        "Uyarı",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                }
                
                try
                {
                    _orderService = new OrderService(mongoDbService);
                    System.Diagnostics.Debug.WriteLine("[MainForm] OrderService oluşturuldu");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainForm] OrderService oluşturulurken hata: {ex.Message}");
                    System.Console.WriteLine($"[MainForm] OrderService oluşturulurken hata: {ex.Message}");
                    System.Console.WriteLine($"[MainForm] OrderService StackTrace: {ex.StackTrace}");
                    XtraMessageBox.Show(
                        $"OrderService oluşturulurken hata oluştu:\n{ex.Message}\n\nProgram devam edecek ancak bazı özellikler çalışmayabilir.",
                        "Uyarı",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                }
                
                try
                {
                    if (_printerService != null && _orderService != null)
                    {
                        _jobAssignmentService = new JobAssignmentService(_printerService, _orderService, mongoDbService);
                        System.Diagnostics.Debug.WriteLine("[MainForm] JobAssignmentService oluşturuldu");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainForm] JobAssignmentService oluşturulurken hata: {ex.Message}");
                    System.Console.WriteLine($"[MainForm] JobAssignmentService oluşturulurken hata: {ex.Message}");
                    XtraMessageBox.Show(
                        $"JobAssignmentService oluşturulurken hata oluştu:\n{ex.Message}\n\nProgram devam edecek ancak bazı özellikler çalışmayabilir.",
                        "Uyarı",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                }
                
                // Vektör tabanlı skin ayarını uygula (WXI veya The Bezier)
                try
                {
                    // WXI Skin - Windows 11 stili, modern ve yuvarlatılmış köşeler
                    UserLookAndFeel.Default.SetSkinStyle("WXI");
                    // Alternatif: The Bezier skin'i için aşağıdaki satırı kullanabilirsiniz:
                    // UserLookAndFeel.Default.SetSkinStyle("The Bezier");
                    System.Diagnostics.Debug.WriteLine("[MainForm] WXI Skin uygulandı");
                }
                catch (Exception skinEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainForm] Skin ayarı hatası: {skinEx.Message}");
                    System.Console.WriteLine($"[MainForm] Skin ayarı hatası: {skinEx.Message}");
                }

                this.Shown += MainForm_Shown;
                SetupEventHandlers();
                StartRefreshTimer();
                // İlk temayı uygula
                ApplyTheme();
                
                // Formun görünür olduğundan emin ol
                this.Visible = true;
                this.ShowInTaskbar = true;
                this.WindowState = System.Windows.Forms.FormWindowState.Normal;
                
                System.Diagnostics.Debug.WriteLine($"[MainForm] Form görünür: {this.Visible}, Taskbar'da: {this.ShowInTaskbar}");
                System.Console.WriteLine($"[MainForm] Form görünür: {this.Visible}, Taskbar'da: {this.ShowInTaskbar}");
                System.Diagnostics.Debug.WriteLine("[MainForm] Constructor tamamlandı!");
                System.Console.WriteLine("[MainForm] Constructor tamamlandı!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainForm] Constructor'da kritik hata: {ex.Message}");
                System.Console.WriteLine($"[MainForm] Constructor'da kritik hata: {ex.Message}");
                System.Console.WriteLine($"[MainForm] StackTrace: {ex.StackTrace}");
                
                try
                {
                    XtraMessageBox.Show(
                        $"Program başlatılırken kritik bir hata oluştu:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                        "Kritik Hata",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error);
                }
                catch
                {
                    // MessageBox bile gösterilemiyorsa, en azından konsola yaz
                    System.Console.WriteLine("MessageBox gösterilemedi!");
                }
                
                // Hata olsa bile formu göster
                try
                {
                    this.Visible = true;
                    this.ShowInTaskbar = true;
                    this.WindowState = System.Windows.Forms.FormWindowState.Normal;
                    System.Console.WriteLine("Form görünürlüğü ayarlandı (hata durumunda)");
                }
                catch
                {
                    System.Console.WriteLine("Form görünürlüğü ayarlanamadı!");
                }
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            try
            {
                // İlk yükleme
                InitializeData();
                
                // JobAssignmentService'den yazıcıları manuel olarak güncelle
                // (Event handler'lar kurulduktan sonra)
                if (_jobAssignmentService != null)
                {
                    System.Diagnostics.Debug.WriteLine("[MainForm] MainForm_Shown: Yazıcılar manuel olarak güncelleniyor...");
                    System.Console.WriteLine("[MainForm] MainForm_Shown: Yazıcılar manuel olarak güncelleniyor...");
                    
                    // RefreshData() çağırarak yazıcıları güncelle
                    RefreshData();
                }
                
                // Yazıcıların ve işlerin tam yüklenmesi için birkaç kez güncelle
                var refreshTimer1 = new System.Windows.Forms.Timer();
                refreshTimer1.Interval = 1000; // 1 saniye bekle
                refreshTimer1.Tick += (s, args) =>
                {
                    refreshTimer1.Stop();
                    refreshTimer1.Dispose();
                    RefreshData();
                    System.Diagnostics.Debug.WriteLine("[MainForm] 1. RefreshData() çağrıldı (1 saniye sonra)");
                    
                    // Sipariş durumlarını kontrol et (program başlatıldığında)
                    CheckOrderStatusesOnStartup();
                    
                    // Bir kez daha güncelle
                    var refreshTimer2 = new System.Windows.Forms.Timer();
                    refreshTimer2.Interval = 2000; // 2 saniye daha bekle
                    refreshTimer2.Tick += (s2, args2) =>
                    {
                        refreshTimer2.Stop();
                        refreshTimer2.Dispose();
                        RefreshData();
                        System.Diagnostics.Debug.WriteLine("[MainForm] 2. RefreshData() çağrıldı (3 saniye sonra)");
                    };
                    refreshTimer2.Start();
                };
                refreshTimer1.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Form shown error: {ex.Message}");
                // Hata durumunda bile formu göster
            }
        }

        private void InitializeComponent()
        {
            this.Text = "3D Yazıcı Otomasyon Sistemi";
            this.Size = new System.Drawing.Size(1500, 650);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.FromArgb(243, 243, 243); // Windows 11 arka plan rengi
            this.MinimumSize = new System.Drawing.Size(1200, 650);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.WindowState = System.Windows.Forms.FormWindowState.Normal;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Resize += MainForm_Resize;
            
            // Modern Fluent Design özellikleri (manuel)
            try
            {
                // Form'a modern görünüm için özel ayarlar
                // WXI skin zaten aktif, ScrollUIMode.Fluent de aktif
                // Form'un kendisi için ek modernleştirmeler
            }
            catch
            {
                // Hata durumunda devam et
            }

            // Başlık Panel (Modern gradient efekti için)
            titlePanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(this.ClientSize.Width, 80),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right,
                BackColor = System.Drawing.Color.Transparent,
                Padding = new System.Windows.Forms.Padding(0, 0, 0, 5)
            };
            // Gradient arka plan için Paint event'i (tema kontrolü ile)
            titlePanel.Paint += (s, e) =>
            {
                var panel = s as System.Windows.Forms.Panel;
                if (panel == null) return;
                
                System.Drawing.Color color1, color2;
                if (_currentTheme == ThemeMode.Dark)
                {
                    color1 = System.Drawing.Color.FromArgb(40, 40, 40);
                    color2 = System.Drawing.Color.FromArgb(25, 25, 25);
                }
                else
                {
                    color1 = System.Drawing.Color.FromArgb(0, 120, 215); // Windows 11 mavi (soldan)
                    color2 = System.Drawing.Color.FromArgb(177, 70, 194); // Mor (sağa)
                }
                
                // Gradient brush'i panel'in tam boyutunda oluştur
                var rect = new System.Drawing.Rectangle(0, 0, panel.Width, panel.Height);
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new System.Drawing.Point(0, 0),
                    new System.Drawing.Point(panel.Width, 0),
                    color1,
                    color2)) // Soldan sağa gradient (maviden mora)
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            };
            this.Controls.Add(titlePanel);

            // Başlık (Daha modern görünüm)
            lblTitle = new LabelControl
            {
                Text = "🖨️ 3D YAZICI OTOMASYON SİSTEMİ",
                Location = new System.Drawing.Point(30, 22),
                Size = new System.Drawing.Size(600, 42),
                Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };
            // Gölge efekti için
            lblTitle.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.EllipsisCharacter;
            lblTitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            lblTitle.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            lblTitle.Appearance.BackColor = System.Drawing.Color.Transparent;
            lblTitle.Appearance.Options.UseBackColor = true;
            titlePanel.Controls.Add(lblTitle);

            // Status Label (Başlık panelinde - daha modern) - Gizli
            lblStatus = new LabelControl
            {
                Text = "● Sistem Hazır",
                Location = new System.Drawing.Point(30, 55),
                Size = new System.Drawing.Size(400, 28),
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(200, 230, 255),
                Visible = false // Gizli
            };
            lblStatus.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            lblStatus.Appearance.BackColor = System.Drawing.Color.Transparent;
            lblStatus.Appearance.Options.UseBackColor = true;
            titlePanel.Controls.Add(lblStatus);

            // Ayarlar Butonu (Modern yuvarlatılmış köşeli buton)
            btnSettings = new SimpleButton
            {
                Text = "⚙️",
                Size = new System.Drawing.Size(50, 50),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right,
                Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold),
                ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False
            };
            // Modern Windows 11 stili buton
            btnSettings.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnSettings.Appearance.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnSettings.Appearance.BorderColor = System.Drawing.Color.FromArgb(200, 200, 200);
            btnSettings.Appearance.Options.UseBackColor = true;
            btnSettings.Appearance.Options.UseForeColor = true;
            btnSettings.Appearance.Options.UseBorderColor = true;
            btnSettings.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            btnSettings.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnSettings.AppearanceHovered.Options.UseBackColor = true;
            btnSettings.AppearanceHovered.Options.UseBorderColor = true;
            btnSettings.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(230, 230, 230);
            btnSettings.AppearancePressed.Options.UseBackColor = true;
            // Vektör tabanlı skin kullan (WXI)
            btnSettings.LookAndFeel.UseDefaultLookAndFeel = true;
            btnSettings.Click += BtnSettings_Click;
            titlePanel.Controls.Add(btnSettings);
            btnSettings.Location = new System.Drawing.Point(titlePanel.Width - btnSettings.Width - 20, 20);

            // Ayarlar Paneli (Popup)
            settingsPanel = new System.Windows.Forms.Panel
            {
                Size = new System.Drawing.Size(200, 100),
                BackColor = System.Drawing.Color.FromArgb(245, 247, 250),
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                Visible = false
            };
            this.Controls.Add(settingsPanel);
            settingsPanel.BringToFront();

            // Ayarlar Paneli Başlık
            var lblSettingsTitle = new LabelControl
            {
                Text = "⚙️ Ayarlar",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(180, 25),
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(33, 33, 33)
            };
            settingsPanel.Controls.Add(lblSettingsTitle);

            // Tema Değiştirme Butonu (Ayarlar panelinde)
            btnToggleTheme = new SimpleButton
            {
                Text = "🌙 Koyu Tema",
                Location = new System.Drawing.Point(10, 40),
                Size = new System.Drawing.Size(180, 35),
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            };
            btnToggleTheme.Appearance.BackColor = System.Drawing.Color.FromArgb(33, 33, 33);
            btnToggleTheme.Appearance.ForeColor = System.Drawing.Color.White;
            btnToggleTheme.Appearance.Options.UseBackColor = true;
            btnToggleTheme.Appearance.Options.UseForeColor = true;
            btnToggleTheme.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(66, 66, 66);
            btnToggleTheme.AppearanceHovered.Options.UseBackColor = true;
            btnToggleTheme.LookAndFeel.UseDefaultLookAndFeel = false;
            btnToggleTheme.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            btnToggleTheme.Click += BtnToggleTheme_Click;
            settingsPanel.Controls.Add(btnToggleTheme);

            // Yeni Yazıcı Ekle Button (Modern tasarım)
            btnAddPrinter = new SimpleButton
            {
                Text = "🖨️ Yeni Yazıcı Ekle",
                Size = new System.Drawing.Size(210, 48),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right,
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold)
            };
            btnAddPrinter.Appearance.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnAddPrinter.Appearance.ForeColor = System.Drawing.Color.White;
            btnAddPrinter.Appearance.BorderColor = System.Drawing.Color.FromArgb(0, 100, 180);
            btnAddPrinter.Appearance.Options.UseBackColor = true;
            btnAddPrinter.Appearance.Options.UseForeColor = true;
            btnAddPrinter.Appearance.Options.UseBorderColor = true;
            btnAddPrinter.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(0, 100, 180);
            btnAddPrinter.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(0, 80, 160);
            btnAddPrinter.AppearanceHovered.Options.UseBackColor = true;
            btnAddPrinter.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(0, 80, 160);
            btnAddPrinter.AppearancePressed.Options.UseBackColor = true;
            // Vektör tabanlı skin kullan (WXI)
            btnAddPrinter.LookAndFeel.UseDefaultLookAndFeel = true;
            btnAddPrinter.Click += BtnAddPrinter_Click;
            titlePanel.Controls.Add(btnAddPrinter);

            // Simulate Order Button (Modern tasarım)
            btnSimulateOrder = new SimpleButton
            {
                Text = "➕ Yeni Sipariş Simüle Et",
                Size = new System.Drawing.Size(280, 48),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right,
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold)
            };
            btnSimulateOrder.Appearance.BackColor = System.Drawing.Color.FromArgb(16, 124, 16);
            btnSimulateOrder.Appearance.ForeColor = System.Drawing.Color.White;
            btnSimulateOrder.Appearance.BorderColor = System.Drawing.Color.FromArgb(12, 100, 12);
            btnSimulateOrder.Appearance.Options.UseBackColor = true;
            btnSimulateOrder.Appearance.Options.UseForeColor = true;
            btnSimulateOrder.Appearance.Options.UseBorderColor = true;
            btnSimulateOrder.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(20, 140, 20);
            btnSimulateOrder.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(16, 120, 16);
            btnSimulateOrder.AppearanceHovered.Options.UseBackColor = true;
            btnSimulateOrder.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(12, 100, 12);
            btnSimulateOrder.AppearancePressed.Options.UseBackColor = true;
            // Vektör tabanlı skin kullan (WXI)
            btnSimulateOrder.LookAndFeel.UseDefaultLookAndFeel = true;
            btnSimulateOrder.Click += BtnSimulateOrder_Click;
            titlePanel.Controls.Add(btnSimulateOrder);
            btnAddPrinter.Location = new System.Drawing.Point(btnSettings.Left - btnAddPrinter.Width - 10, 20);
            btnSimulateOrder.Location = new System.Drawing.Point(btnAddPrinter.Left - btnSimulateOrder.Width - 10, 20);

            // Modelleri Göster Butonu (Modern tasarım)
            btnShowModels = new SimpleButton
            {
                Text = "📦 Modelleri Göster",
                Size = new System.Drawing.Size(200, 48),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right,
                Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold)
            };
            btnShowModels.Appearance.BackColor = System.Drawing.Color.FromArgb(177, 70, 194);
            btnShowModels.Appearance.ForeColor = System.Drawing.Color.White;
            btnShowModels.Appearance.BorderColor = System.Drawing.Color.FromArgb(150, 50, 170);
            btnShowModels.Appearance.Options.UseBackColor = true;
            btnShowModels.Appearance.Options.UseForeColor = true;
            btnShowModels.Appearance.Options.UseBorderColor = true;
            btnShowModels.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(190, 90, 210);
            btnShowModels.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(170, 70, 190);
            btnShowModels.AppearanceHovered.Options.UseBackColor = true;
            btnShowModels.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(150, 50, 170);
            btnShowModels.AppearancePressed.Options.UseBackColor = true;
            // Vektör tabanlı skin kullan (WXI)
            btnShowModels.LookAndFeel.UseDefaultLookAndFeel = true;
            btnShowModels.Click += BtnShowModels_Click;
            titlePanel.Controls.Add(btnShowModels);
            btnShowModels.Location = new System.Drawing.Point(btnSimulateOrder.Left - btnShowModels.Width - 10, 20);

            // Content Panel (Tüm içerik - Modern gradient arka plan)
            contentPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(0, 80),
                Size = new System.Drawing.Size(this.ClientSize.Width, this.ClientSize.Height - 80),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right,
                BackColor = System.Drawing.Color.Transparent
            };
            contentPanel.Paint += (s, e) =>
            {
                System.Drawing.Color color1, color2;
                if (_currentTheme == ThemeMode.Dark)
                {
                    color1 = System.Drawing.Color.FromArgb(35, 35, 35);
                    color2 = System.Drawing.Color.FromArgb(30, 30, 30);
                }
                else
                {
                    color1 = System.Drawing.Color.FromArgb(250, 250, 250);
                    color2 = System.Drawing.Color.FromArgb(243, 243, 243);
                }
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    contentPanel.ClientRectangle,
                    color1,
                    color2,
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(brush, contentPanel.ClientRectangle);
                }
            };
            this.Controls.Add(contentPanel);
            contentPanel.SendToBack();

            // Printers Grid Başlık Panel (Modern gradient)
            printersHeaderPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(20, 15),
                Size = new System.Drawing.Size(450, 40),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left,
                BackColor = System.Drawing.Color.Transparent
            };
            printersHeaderPanel.Paint += (s, e) =>
            {
                System.Drawing.Color color1, color2;
                if (_currentTheme == ThemeMode.Dark)
                {
                    color1 = System.Drawing.Color.FromArgb(50, 70, 150);
                    color2 = System.Drawing.Color.FromArgb(40, 50, 120);
                }
                else
                {
                    color1 = System.Drawing.Color.FromArgb(0, 120, 215);
                    color2 = System.Drawing.Color.FromArgb(0, 100, 180);
                }
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    printersHeaderPanel.ClientRectangle,
                    color1,
                    color2,
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    int radius = 8;
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                        path.AddArc(printersHeaderPanel.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                        path.AddLine(printersHeaderPanel.Width, printersHeaderPanel.Height, 0, printersHeaderPanel.Height);
                        path.CloseAllFigures();
                        e.Graphics.FillPath(brush, path);
                    }
                }
            };
            contentPanel.Controls.Add(printersHeaderPanel);

            lblPrinters = new LabelControl
            {
                Text = "🖨️ 3D YAZICILAR",
                Location = new System.Drawing.Point(15, 8),
                Size = new System.Drawing.Size(430, 25),
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };
            lblPrinters.Appearance.BackColor = System.Drawing.Color.Transparent;
            lblPrinters.Appearance.Options.UseBackColor = true;
            printersHeaderPanel.Controls.Add(lblPrinters);

            // Printers Grid
            try
            {
                gridControlPrinters = new GridControl
                {
                    Location = new System.Drawing.Point(20, 55),
                    Size = new System.Drawing.Size(450, 280),
                    Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left,
                    Visible = false
                };
                gridViewPrinters = new GridView(gridControlPrinters);
                gridControlPrinters.MainView = gridViewPrinters;
                gridControlPrinters.UseEmbeddedNavigator = false;
                gridViewPrinters.OptionsBehavior.Editable = false;
                gridViewPrinters.PaintStyleName = "Flat";
                gridViewPrinters.OptionsView.EnableAppearanceEvenRow = false;
                gridViewPrinters.OptionsView.EnableAppearanceOddRow = false;
                // Modern görünüm için satır yüksekliğini artır
                gridViewPrinters.RowHeight = 35; // Varsayılan 20'den 35'e çıkarıldı
                // Padding için
                gridViewPrinters.OptionsView.RowAutoHeight = false;
                // Tüm satırlar için siyah yazı
                gridViewPrinters.Appearance.Row.ForeColor = System.Drawing.Color.Black;
                gridViewPrinters.Appearance.Row.BackColor = System.Drawing.Color.White;
                gridViewPrinters.Appearance.Row.Options.UseForeColor = true;
                gridViewPrinters.Appearance.Row.Options.UseBackColor = true;
                gridViewPrinters.Appearance.Row.Options.UseTextOptions = true;
                
                // Başlık paneli
                gridViewPrinters.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(48, 63, 159);
                gridViewPrinters.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.White;
                gridViewPrinters.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
                gridViewPrinters.Appearance.HeaderPanel.Options.UseBackColor = true;
                gridViewPrinters.Appearance.HeaderPanel.Options.UseForeColor = true;
                gridViewPrinters.Appearance.HeaderPanel.Options.UseFont = true;
                gridViewPrinters.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                
                // CustomDrawCell event'i ile renkleri zorla uygula
                gridViewPrinters.RowCellStyle += GridViewPrinters_RowCellStyle;
                // Durum kolonuna sembol eklemek için custom display text event'i
                gridViewPrinters.CustomColumnDisplayText += GridViewPrinters_CustomColumnDisplayText;
                // Çift tıklama ile filament değiştirme
                gridViewPrinters.DoubleClick += GridViewPrinters_DoubleClick;
                // Filament sütununa tıklama ile yenileme
                gridViewPrinters.RowCellClick += GridViewPrinters_RowCellClick;
                // Filtre paneli için paint event'i
                gridControlPrinters.Paint += GridControl_Paint;
                
                contentPanel.Controls.Add(gridControlPrinters);
                // Grid'i arka plana gönder (printersIconPanel önde görünsün)
                gridControlPrinters.SendToBack();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Printers grid init error: {ex.Message}");
            }
            
            // Yazıcı Icon Paneli (Küçük, scroll olmayacak şekilde)
            printersIconPanel = new System.Windows.Forms.FlowLayoutPanel
            {
                Location = new System.Drawing.Point(20, 325),
                Size = new System.Drawing.Size(contentPanel.Width - 40, 100),
                Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right,
                AutoScroll = false,
                FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = _currentTheme == ThemeMode.Dark ? 
                    System.Drawing.Color.FromArgb(30, 30, 30) : 
                    System.Drawing.Color.White,
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                Padding = new System.Windows.Forms.Padding(10, 10, 10, 10)
            };
            contentPanel.Controls.Add(printersIconPanel);
            // printersIconPanel'i öne getir (grid'lerin üstünde görünsün)
            printersIconPanel.BringToFront();
            printerIconPanels = new System.Collections.Generic.Dictionary<int, System.Windows.Forms.Panel>();
            printerPanelClickHandlers = new System.Collections.Generic.Dictionary<int, System.EventHandler>();

            // Orders Grid Başlık Panel
            ordersHeaderPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(490, 15),
                Size = new System.Drawing.Size(450, 40),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left,
                BackColor = System.Drawing.Color.Transparent
            };
            ordersHeaderPanel.Paint += (s, e) =>
            {
                System.Drawing.Color color1, color2;
                if (_currentTheme == ThemeMode.Dark)
                {
                    color1 = System.Drawing.Color.FromArgb(180, 120, 0);
                    color2 = System.Drawing.Color.FromArgb(150, 90, 0);
                }
                else
                {
                    color1 = System.Drawing.Color.FromArgb(255, 185, 0);
                    color2 = System.Drawing.Color.FromArgb(255, 140, 0);
                }
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    ordersHeaderPanel.ClientRectangle,
                    color1,
                    color2,
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    int radius = 8;
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                        path.AddArc(ordersHeaderPanel.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                        path.AddLine(ordersHeaderPanel.Width, ordersHeaderPanel.Height, 0, ordersHeaderPanel.Height);
                        path.CloseAllFigures();
                        e.Graphics.FillPath(brush, path);
                    }
                }
            };
            contentPanel.Controls.Add(ordersHeaderPanel);

            lblOrders = new LabelControl
            {
                Text = "📦 SİPARİŞLER",
                Location = new System.Drawing.Point(15, 8),
                Size = new System.Drawing.Size(150, 25),
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };
            lblOrders.Appearance.BackColor = System.Drawing.Color.Transparent;
            lblOrders.Appearance.Options.UseBackColor = true;
            ordersHeaderPanel.Controls.Add(lblOrders);
            
            // Tamamlananları Sil butonunu siparişler başlık paneline ekle
            btnDeleteCompletedOrders = new SimpleButton
            {
                Text = "🗑️ Tamamlananları Sil",
                Size = new System.Drawing.Size(200, 28),
                Location = new System.Drawing.Point(250, 3),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                Visible = true,
                Enabled = true
            };
            btnDeleteCompletedOrders.Appearance.BackColor = System.Drawing.Color.FromArgb(244, 67, 54);
            btnDeleteCompletedOrders.Appearance.ForeColor = System.Drawing.Color.White;
            btnDeleteCompletedOrders.Appearance.BorderColor = System.Drawing.Color.FromArgb(211, 47, 47);
            btnDeleteCompletedOrders.Appearance.Options.UseBackColor = true;
            btnDeleteCompletedOrders.Appearance.Options.UseForeColor = true;
            btnDeleteCompletedOrders.Appearance.Options.UseBorderColor = true;
            btnDeleteCompletedOrders.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(229, 57, 53);
            btnDeleteCompletedOrders.AppearanceHovered.Options.UseBackColor = true;
            btnDeleteCompletedOrders.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(198, 40, 40);
            btnDeleteCompletedOrders.AppearancePressed.Options.UseBackColor = true;
            btnDeleteCompletedOrders.LookAndFeel.UseDefaultLookAndFeel = false;
            btnDeleteCompletedOrders.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            btnDeleteCompletedOrders.Click += BtnDeleteCompletedOrders_Click;
            ordersHeaderPanel.Controls.Add(btnDeleteCompletedOrders);
            btnDeleteCompletedOrders.BringToFront();


            // Orders Grid
            try
            {
                gridControlOrders = new GridControl
                {
                    Location = new System.Drawing.Point(490, 55),
                    Size = new System.Drawing.Size(450, 280),
                    Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left,
                    Visible = false
                };
                gridViewOrders = new GridView(gridControlOrders);
                gridControlOrders.MainView = gridViewOrders;
                gridControlOrders.UseEmbeddedNavigator = false;
                gridViewOrders.OptionsBehavior.Editable = false;
                gridViewOrders.PaintStyleName = "Flat";
                gridViewOrders.OptionsView.EnableAppearanceEvenRow = false;
                gridViewOrders.OptionsView.EnableAppearanceOddRow = false;
                // Modern görünüm için satır yüksekliğini artır
                gridViewOrders.RowHeight = 35;
                gridViewOrders.OptionsView.RowAutoHeight = false;
                // Modern görünüm için çizgileri ince yatay çizgiler yap
                gridViewOrders.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;
                gridViewOrders.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
                gridViewOrders.Appearance.HorzLine.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
                gridViewOrders.Appearance.HorzLine.Options.UseBackColor = true;
                
                // Tüm satırlar için siyah yazı
                gridViewOrders.Appearance.Row.ForeColor = System.Drawing.Color.Black;
                gridViewOrders.Appearance.Row.BackColor = System.Drawing.Color.White;
                gridViewOrders.Appearance.Row.Options.UseForeColor = true;
                gridViewOrders.Appearance.Row.Options.UseBackColor = true;
                gridViewOrders.Appearance.Row.Options.UseTextOptions = true;
                
                // Başlık paneli
                gridViewOrders.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(230, 126, 34);
                gridViewOrders.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.White;
                gridViewOrders.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
                gridViewOrders.Appearance.HeaderPanel.Options.UseBackColor = true;
                gridViewOrders.Appearance.HeaderPanel.Options.UseForeColor = true;
                gridViewOrders.Appearance.HeaderPanel.Options.UseFont = true;
                gridViewOrders.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                
                // CustomDrawCell event'i ile renkleri zorla uygula
                gridViewOrders.RowCellStyle += GridViewOrders_RowCellStyle;
                // Filtre paneli için paint event'i
                gridControlOrders.Paint += GridControl_Paint;
                
                contentPanel.Controls.Add(gridControlOrders);
                // Grid'i arka plana gönder (printersIconPanel önde görünsün)
                gridControlOrders.SendToBack();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Orders grid init error: {ex.Message}");
            }

            // Jobs Grid Başlık Panel
            jobsHeaderPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(960, 15),
                Size = new System.Drawing.Size(450, 40),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right,
                BackColor = System.Drawing.Color.Transparent
            };
            jobsHeaderPanel.Paint += (s, e) =>
            {
                System.Drawing.Color color1, color2;
                if (_currentTheme == ThemeMode.Dark)
                {
                    color1 = System.Drawing.Color.FromArgb(130, 40, 150);
                    color2 = System.Drawing.Color.FromArgb(100, 20, 120);
                }
                else
                {
                    color1 = System.Drawing.Color.FromArgb(177, 70, 194);
                    color2 = System.Drawing.Color.FromArgb(150, 50, 170);
                }
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    jobsHeaderPanel.ClientRectangle,
                    color1,
                    color2,
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    int radius = 8;
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                        path.AddArc(jobsHeaderPanel.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                        path.AddLine(jobsHeaderPanel.Width, jobsHeaderPanel.Height, 0, jobsHeaderPanel.Height);
                        path.CloseAllFigures();
                        e.Graphics.FillPath(brush, path);
                    }
                }
            };
            contentPanel.Controls.Add(jobsHeaderPanel);

            lblJobs = new LabelControl
            {
                Text = "⚙️ YAZDIRMA İŞLERİ",
                Location = new System.Drawing.Point(15, 8),
                Size = new System.Drawing.Size(430, 25),
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };
            lblJobs.Appearance.BackColor = System.Drawing.Color.Transparent;
            lblJobs.Appearance.Options.UseBackColor = true;
            jobsHeaderPanel.Controls.Add(lblJobs);

            // Tamamlananları Sil butonunu yazdırma işleri başlık paneline ekle
            btnDeleteCompletedJobs = new SimpleButton
            {
                Text = "🗑️ Tamamlananları Sil",
                Size = new System.Drawing.Size(180, 25),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right
            };
            btnDeleteCompletedJobs.Appearance.BackColor = System.Drawing.Color.FromArgb(244, 67, 54);
            btnDeleteCompletedJobs.Appearance.ForeColor = System.Drawing.Color.White;
            btnDeleteCompletedJobs.Appearance.BorderColor = System.Drawing.Color.FromArgb(211, 47, 47);
            btnDeleteCompletedJobs.Appearance.Options.UseBackColor = true;
            btnDeleteCompletedJobs.Appearance.Options.UseForeColor = true;
            btnDeleteCompletedJobs.Appearance.Options.UseBorderColor = true;
            btnDeleteCompletedJobs.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(229, 57, 53);
            btnDeleteCompletedJobs.AppearanceHovered.Options.UseBackColor = true;
            btnDeleteCompletedJobs.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(198, 40, 40);
            btnDeleteCompletedJobs.AppearancePressed.Options.UseBackColor = true;
            btnDeleteCompletedJobs.LookAndFeel.UseDefaultLookAndFeel = false;
            btnDeleteCompletedJobs.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            btnDeleteCompletedJobs.Click += BtnDeleteCompletedJobs_Click;
            jobsHeaderPanel.Controls.Add(btnDeleteCompletedJobs);
            btnDeleteCompletedJobs.BringToFront();

            // Jobs Grid
            try
            {
                gridControlJobs = new GridControl
                {
                    Location = new System.Drawing.Point(960, 55),
                    Size = new System.Drawing.Size(450, 280),
                    Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right,
                    Visible = false
                };
                gridViewJobs = new GridView(gridControlJobs);
                gridControlJobs.MainView = gridViewJobs;
                gridControlJobs.UseEmbeddedNavigator = false;
                gridViewJobs.OptionsBehavior.Editable = false;
                gridViewJobs.PaintStyleName = "Flat";
                gridViewJobs.OptionsView.EnableAppearanceEvenRow = false;
                gridViewJobs.OptionsView.EnableAppearanceOddRow = false;
                // Modern görünüm için satır yüksekliğini artır
                gridViewJobs.RowHeight = 35;
                gridViewJobs.OptionsView.RowAutoHeight = false;
                // Tüm satırlar için siyah yazı
                gridViewJobs.Appearance.Row.ForeColor = System.Drawing.Color.Black;
                gridViewJobs.Appearance.Row.BackColor = System.Drawing.Color.White;
                gridViewJobs.Appearance.Row.Options.UseForeColor = true;
                gridViewJobs.Appearance.Row.Options.UseBackColor = true;
                gridViewJobs.Appearance.Row.Options.UseTextOptions = true;
                
                // Başlık paneli
                gridViewJobs.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(123, 31, 162);
                gridViewJobs.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.White;
                gridViewJobs.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
                gridViewJobs.Appearance.HeaderPanel.Options.UseBackColor = true;
                gridViewJobs.Appearance.HeaderPanel.Options.UseForeColor = true;
                gridViewJobs.Appearance.HeaderPanel.Options.UseFont = true;
                gridViewJobs.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                
                // CustomDrawCell event'i ile renkleri zorla uygula
                gridViewJobs.RowCellStyle += GridViewJobs_RowCellStyle;
                // Filtre paneli için paint event'i
                gridControlJobs.Paint += GridControl_Paint;
                
                contentPanel.Controls.Add(gridControlJobs);
                // Grid'i arka plana gönder (printersIconPanel önde görünsün)
                gridControlJobs.SendToBack();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Jobs grid init error: {ex.Message}");
            }

            SetupStatisticsPanel();
            SetupGridColumns();
        }

        private void ShowSection(string sectionName)
        {
            // Tüm grid'leri gizle
            gridControlPrinters.Visible = false;
            gridControlOrders.Visible = false;
            gridControlJobs.Visible = false;
            printersHeaderPanel.Visible = false;
            ordersHeaderPanel.Visible = false;
            jobsHeaderPanel.Visible = false;

            // Seçilen bölümü göster
            switch (sectionName)
            {
                case "Printers":
                    gridControlPrinters.Visible = true;
                    printersHeaderPanel.Visible = true;
                    break;
                case "Orders":
                    gridControlOrders.Visible = true;
                    ordersHeaderPanel.Visible = true;
                    break;
                case "Jobs":
                    gridControlJobs.Visible = true;
                    jobsHeaderPanel.Visible = true;
                    break;
            }
        }

        private void SetupStatisticsPanel()
        {
            // İstatistikler Paneli
            // Başlangıç konumunu contentPanel'e göre ayarla (alt kısımdan 1 piksel margin ile)
            int statsPanelHeight = 130; // Yüksekliği artırdık
            int statsPanelTop = contentPanel.Height - statsPanelHeight - 1; // Panel yüksekliği, margin 1
            statsPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(20, statsPanelTop),
                Size = new System.Drawing.Size(contentPanel.Width - 40, statsPanelHeight),
                Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right, // Alt kısımda sabit kalacak
                BackColor = System.Drawing.Color.White,
                BorderStyle = System.Windows.Forms.BorderStyle.None
            };
            // Modern gölge efekti için Paint event'i
            statsPanel.Paint += (s, e) =>
            {
                // Yuvarlatılmış köşeler için path oluştur
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    int radius = 12;
                    path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                    path.AddArc(statsPanel.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                    path.AddArc(statsPanel.Width - radius * 2, statsPanel.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(0, statsPanel.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseAllFigures();
                    
                    // Gölge efekti (koyu temada daha belirgin)
                    int shadowAlpha = _currentTheme == ThemeMode.Dark ? 40 : 20;
                    using (var shadowBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(shadowAlpha, 0, 0, 0)))
                    {
                        var shadowRect = new System.Drawing.Rectangle(2, 2, statsPanel.Width, statsPanel.Height);
                        e.Graphics.FillPath(shadowBrush, path);
                    }
                    
                    // Ana panel (tema kontrolü ile)
                    System.Drawing.Color panelColor = _currentTheme == ThemeMode.Dark 
                        ? System.Drawing.Color.FromArgb(50, 50, 50) 
                        : System.Drawing.Color.White;
                    using (var brush = new System.Drawing.SolidBrush(panelColor))
                    {
                        var mainRect = new System.Drawing.Rectangle(0, 0, statsPanel.Width, statsPanel.Height);
                        e.Graphics.FillPath(brush, path);
                    }
                    
                    // Border (tema kontrolü ile)
                    System.Drawing.Color borderColor = _currentTheme == ThemeMode.Dark 
                        ? System.Drawing.Color.FromArgb(70, 70, 70) 
                        : System.Drawing.Color.FromArgb(230, 230, 230);
                    using (var pen = new System.Drawing.Pen(borderColor, 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            contentPanel.Controls.Add(statsPanel);

            lblStats = new LabelControl
            {
                Text = "📊 İSTATİSTİKLER",
                Location = new System.Drawing.Point(20, 32),
                Size = new System.Drawing.Size(200, 25),
                Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(0, 120, 215)
            };
            lblStats.Appearance.BackColor = System.Drawing.Color.Transparent;
            lblStats.Appearance.Options.UseBackColor = true;
            statsPanel.Controls.Add(lblStats);
            
            // Alt çizgi (modern gradient)
            var separatorLine = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(20, 57),
                Size = new System.Drawing.Size(statsPanel.Width - 40, 2),
                BackColor = System.Drawing.Color.Transparent,
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right
            };
            separatorLine.Paint += (s, e) =>
            {
                System.Drawing.Color color1, color2;
                if (_currentTheme == ThemeMode.Dark)
                {
                    color1 = System.Drawing.Color.FromArgb(80, 80, 80);
                    color2 = System.Drawing.Color.FromArgb(60, 60, 60);
                }
                else
                {
                    color1 = System.Drawing.Color.FromArgb(0, 120, 215);
                    color2 = System.Drawing.Color.FromArgb(0, 100, 180);
                }
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    separatorLine.ClientRectangle,
                    color1,
                    color2,
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, separatorLine.ClientRectangle);
                }
            };
            statsPanel.Controls.Add(separatorLine);

            // Toplam Yazıcı
            var lblTotalPrintersLabel = new LabelControl
            {
                Text = "Toplam Yazıcı:",
                Location = new System.Drawing.Point(25, 67),
                Size = new System.Drawing.Size(100, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(100, 100, 100)
            };
            lblTotalPrintersLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            statsPanel.Controls.Add(lblTotalPrintersLabel);

            lblTotalPrinters = new LabelControl
            {
                Text = "10",
                Location = new System.Drawing.Point(135, 62),
                Size = new System.Drawing.Size(50, 20),
                Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(63, 81, 181)
            };
            lblTotalPrinters.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            statsPanel.Controls.Add(lblTotalPrinters);

            // Aktif Yazıcı
            var lblActivePrintersLabel = new LabelControl
            {
                Text = "Aktif Yazıcı:",
                Location = new System.Drawing.Point(225, 67),
                Size = new System.Drawing.Size(100, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(100, 100, 100)
            };
            lblActivePrintersLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            statsPanel.Controls.Add(lblActivePrintersLabel);

            lblActivePrinters = new LabelControl
            {
                Text = "0",
                Location = new System.Drawing.Point(315, 62),
                Size = new System.Drawing.Size(50, 20),
                Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(76, 175, 80)
            };
            lblActivePrinters.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            statsPanel.Controls.Add(lblActivePrinters);

            // Toplam Sipariş
            var lblTotalOrdersLabel = new LabelControl
            {
                Text = "Toplam Sipariş:",
                Location = new System.Drawing.Point(425, 67),
                Size = new System.Drawing.Size(100, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(100, 100, 100)
            };
            lblTotalOrdersLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            statsPanel.Controls.Add(lblTotalOrdersLabel);

            lblTotalOrders = new LabelControl
            {
                Text = "0",
                Location = new System.Drawing.Point(535, 62),
                Size = new System.Drawing.Size(50, 20),
                Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(255, 152, 0)
            };
            lblTotalOrders.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            statsPanel.Controls.Add(lblTotalOrders);

            // Bekleyen İşler
            var lblPendingJobsLabel = new LabelControl
            {
                Text = "Bekleyen İşler:",
                Location = new System.Drawing.Point(625, 67),
                Size = new System.Drawing.Size(100, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(100, 100, 100)
            };
            lblPendingJobsLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            statsPanel.Controls.Add(lblPendingJobsLabel);

            lblPendingJobs = new LabelControl
            {
                Text = "0",
                Location = new System.Drawing.Point(735, 62),
                Size = new System.Drawing.Size(50, 20),
                Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(156, 39, 176)
            };
            lblPendingJobs.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            statsPanel.Controls.Add(lblPendingJobs);

            // Toplam Tamamlanan İş (Bekleyen İşler yanına alındı)
            var lblCompletedJobsLabel = new LabelControl
            {
                Text = "Tamamlanan İş:",
                Location = new System.Drawing.Point(825, 67),
                Size = new System.Drawing.Size(120, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(100, 100, 100),
                Name = "lblCompletedJobsLabel"
            };
            lblCompletedJobsLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            statsPanel.Controls.Add(lblCompletedJobsLabel);

            var lblCompletedJobs = new LabelControl
            {
                Text = "0",
                Location = new System.Drawing.Point(945, 62),
                Size = new System.Drawing.Size(50, 20),
                Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(76, 175, 80),
                Name = "lblCompletedJobs"
            };
            lblCompletedJobs.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            statsPanel.Controls.Add(lblCompletedJobs);

            // Toplam Kazanç (Butonun üzerinde - mesafe artırıldı)
            var lblTotalEarningsLabel = new LabelControl
            {
                Text = "Toplam Kazanç:",
                Size = new System.Drawing.Size(100, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(100, 100, 100),
                Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right,
                Name = "lblTotalEarningsLabel"
            };
            lblTotalEarningsLabel.Appearance.BackColor = System.Drawing.Color.Transparent;
            lblTotalEarningsLabel.Appearance.Options.UseBackColor = true;
            lblTotalEarningsLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            lblTotalEarningsLabel.Location = new System.Drawing.Point(statsPanel.Width - 205, 64);
            statsPanel.Controls.Add(lblTotalEarningsLabel);

            lblTotalEarnings = new System.Windows.Forms.Label
            {
                Text = "0,00 TL",
                Size = new System.Drawing.Size(90, 20),
                Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(255, 193, 7),
                BackColor = System.Drawing.Color.Transparent,
                Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Name = "lblTotalEarnings"
            };
            lblTotalEarnings.Location = new System.Drawing.Point(statsPanel.Width - 95, 62);
            statsPanel.Controls.Add(lblTotalEarnings);

            // Kazanç Detayları Butonu (Modern)
            btnShowEarnings = new SimpleButton
            {
                Text = "💰 Kazanç Detayları",
                Size = new System.Drawing.Size(200, 32),
                Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold),
                Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right,
                ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False
            };
            // Modern Windows 11 stili buton
            btnShowEarnings.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 185, 0);
            btnShowEarnings.Appearance.ForeColor = System.Drawing.Color.White;
            btnShowEarnings.Appearance.BorderColor = System.Drawing.Color.FromArgb(255, 140, 0);
            btnShowEarnings.Appearance.Options.UseBackColor = true;
            btnShowEarnings.Appearance.Options.UseForeColor = true;
            btnShowEarnings.Appearance.Options.UseBorderColor = true;
            btnShowEarnings.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(255, 200, 0);
            btnShowEarnings.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(255, 160, 0);
            btnShowEarnings.AppearanceHovered.Options.UseBackColor = true;
            btnShowEarnings.AppearanceHovered.Options.UseBorderColor = true;
            btnShowEarnings.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(255, 140, 0);
            btnShowEarnings.AppearancePressed.Options.UseBackColor = true;
            // Vektör tabanlı skin kullan (WXI)
            btnShowEarnings.LookAndFeel.UseDefaultLookAndFeel = true;
            btnShowEarnings.Click += BtnShowEarnings_Click;
            statsPanel.Controls.Add(btnShowEarnings);
            // Butonu sağa hizala ve label'ları öne getir
            btnShowEarnings.Location = new System.Drawing.Point(statsPanel.Width - btnShowEarnings.Width - 10, 85);
            lblTotalEarningsLabel.BringToFront();
            lblTotalEarnings.BringToFront();
        }

        private void SetupGridColumns()
        {
            // Printers Grid Columns
            GridColumn colId = gridViewPrinters.Columns.AddField("Id");
            colId.Caption = "ID";
            colId.VisibleIndex = 0;
            colId.Width = 29;
            colId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colId.AppearanceCell.Options.UseForeColor = true;
            colId.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colName = gridViewPrinters.Columns.AddField("Name");
            colName.Caption = "Yazıcı Adı";
            colName.VisibleIndex = 1;
            colName.Width = 79;
            colName.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colName.AppearanceCell.Options.UseForeColor = true;
            colName.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colStatus = gridViewPrinters.Columns.AddField("Status");
            colStatus.Caption = "Durum";
            colStatus.VisibleIndex = 2;
            colStatus.Width = 59;
            colStatus.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colStatus.AppearanceCell.Options.UseForeColor = true;
            colStatus.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colJob = gridViewPrinters.Columns.AddField("CurrentJobName");
            colJob.Caption = "Mevcut İş";
            colJob.VisibleIndex = 3;
            colJob.Width = 89;
            colJob.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJob.AppearanceCell.Options.UseForeColor = true;
            colJob.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colProgress = gridViewPrinters.Columns.AddField("Progress");
            colProgress.Caption = "İlerleme %";
            colProgress.VisibleIndex = 4;
            colProgress.Width = 80;
            colProgress.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colProgress.DisplayFormat.FormatString = "F1";
            colProgress.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colProgress.AppearanceCell.Options.UseForeColor = true;
            colProgress.AppearanceCell.Options.UseTextOptions = true;
            colProgress.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colFilament = gridViewPrinters.Columns.AddField("FilamentRemaining");
            colFilament.Caption = "Filament %";
            colFilament.VisibleIndex = 5;
            colFilament.Width = 54;
            colFilament.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colFilament.DisplayFormat.FormatString = "F1";
            colFilament.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colFilament.AppearanceCell.Options.UseForeColor = true;
            colFilament.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colFilamentType = gridViewPrinters.Columns.AddField("FilamentType");
            colFilamentType.Caption = "Filament Tipi";
            colFilamentType.VisibleIndex = 6;
            colFilamentType.Width = 64;
            colFilamentType.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colFilamentType.AppearanceCell.Options.UseForeColor = true;
            colFilamentType.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            gridViewPrinters.OptionsView.ShowGroupPanel = false;
            gridViewPrinters.OptionsView.ShowIndicator = true;
            gridViewPrinters.OptionsView.ColumnAutoWidth = false;
            // Modern görünüm için çizgileri kaldır veya ince yatay çizgiler bırak
            gridViewPrinters.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
            gridViewPrinters.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.False; // Çizgileri kaldır
            // İnce yatay çizgiler için alternatif (eğer istersen True yapabilirsin)
            gridViewPrinters.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;
            gridViewPrinters.Appearance.HorzLine.BackColor = System.Drawing.Color.FromArgb(240, 240, 240); // İnce gri çizgi
            gridViewPrinters.Appearance.HorzLine.Options.UseBackColor = true;
            
            // Grid genişliğini ayarla
            gridControlPrinters.Size = new System.Drawing.Size(450, 320);

            // Orders Grid Columns
            GridColumn colOrderId = gridViewOrders.Columns.AddField("Id");
            colOrderId.Caption = "ID";
            colOrderId.VisibleIndex = 0;
            colOrderId.Width = 28;
            colOrderId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colOrderId.AppearanceCell.Options.UseForeColor = true;
            colOrderId.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colOrderNo = gridViewOrders.Columns.AddField("OrderNumber");
            colOrderNo.Caption = "Sipariş No";
            colOrderNo.VisibleIndex = 1;
            colOrderNo.Width = 78;
            colOrderNo.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colOrderNo.AppearanceCell.Options.UseForeColor = true;
            colOrderNo.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colCustomer = gridViewOrders.Columns.AddField("CustomerName");
            colCustomer.Caption = "Müşteri";
            colCustomer.VisibleIndex = 2;
            colCustomer.Width = 68;
            colCustomer.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colCustomer.AppearanceCell.Options.UseForeColor = true;
            colCustomer.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colDate = gridViewOrders.Columns.AddField("OrderDate");
            colDate.Caption = "Tarih";
            colDate.VisibleIndex = 3;
            colDate.Width = 78;
            colDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colDate.DisplayFormat.FormatString = "dd.MM.yyyy HH:mm";
            colDate.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colDate.AppearanceCell.Options.UseForeColor = true;
            colDate.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colOrderStatus = gridViewOrders.Columns.AddField("Status");
            colOrderStatus.Caption = "Durum";
            colOrderStatus.VisibleIndex = 4;
            colOrderStatus.Width = 53;
            colOrderStatus.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colOrderStatus.AppearanceCell.Options.UseForeColor = true;
            colOrderStatus.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colTotalPrice = gridViewOrders.Columns.AddField("TotalPrice");
            colTotalPrice.Caption = "Toplam Fiyat";
            colTotalPrice.VisibleIndex = 5;
            colTotalPrice.Width = 63;
            colTotalPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colTotalPrice.DisplayFormat.FormatString = "C2";
            colTotalPrice.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colTotalPrice.AppearanceCell.Options.UseForeColor = true;
            colTotalPrice.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            // Silme sütunu ekle (unbound column)
            GridColumn colDelete = new GridColumn();
            colDelete.FieldName = "DeleteAction";
            colDelete.Caption = "İşlem";
            colDelete.VisibleIndex = 6;
            colDelete.Width = 48;
            colDelete.UnboundType = DevExpress.Data.UnboundColumnType.String;
            colDelete.OptionsColumn.AllowEdit = false;
            colDelete.OptionsColumn.ReadOnly = true;
            colDelete.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            colDelete.OptionsColumn.AllowGroup = DevExpress.Utils.DefaultBoolean.False;
            colDelete.OptionsFilter.AllowFilter = false;
            colDelete.Visible = true;
            colDelete.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colDelete.AppearanceCell.Options.UseForeColor = true;
            colDelete.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            gridViewOrders.Columns.Add(colDelete);
            
            // Unbound column için veri sağlama
            gridViewOrders.CustomUnboundColumnData += GridViewOrders_CustomUnboundColumnData;
            
            // Silme butonu tıklama olayı
            gridViewOrders.MouseDown += GridViewOrders_MouseDown;

            gridViewOrders.OptionsView.ShowGroupPanel = false;
            gridViewOrders.OptionsView.ShowIndicator = true;
            gridViewOrders.OptionsView.ColumnAutoWidth = false;
            gridViewOrders.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
            gridViewOrders.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;
            // İnce yatay çizgiler
            gridViewOrders.Appearance.HorzLine.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            gridViewOrders.Appearance.HorzLine.Options.UseBackColor = true;

            // Jobs Grid Columns
            GridColumn colJobId = gridViewJobs.Columns.AddField("Id");
            colJobId.Caption = "İş ID";
            colJobId.VisibleIndex = 0;
            colJobId.Width = 42;
            colJobId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJobId.AppearanceCell.Options.UseForeColor = true;
            colJobId.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colModel = gridViewJobs.Columns.AddField("ModelFileName");
            colModel.Caption = "Model Dosyası";
            colModel.VisibleIndex = 1;
            colModel.Width = 107;
            colModel.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colModel.AppearanceCell.Options.UseForeColor = true;
            colModel.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colPrinterId = gridViewJobs.Columns.AddField("PrinterId");
            colPrinterId.Caption = "Yazıcı ID";
            colPrinterId.VisibleIndex = 2;
            colPrinterId.Width = 52;
            colPrinterId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colPrinterId.AppearanceCell.Options.UseForeColor = true;
            colPrinterId.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colJobStatus = gridViewJobs.Columns.AddField("Status");
            colJobStatus.Caption = "Durum";
            colJobStatus.VisibleIndex = 3;
            colJobStatus.Width = 62;
            colJobStatus.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJobStatus.AppearanceCell.Options.UseForeColor = true;
            colJobStatus.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colJobProgress = gridViewJobs.Columns.AddField("Progress");
            colJobProgress.Caption = "İlerleme %";
            colJobProgress.VisibleIndex = 4;
            colJobProgress.Width = 80;
            colJobProgress.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colJobProgress.DisplayFormat.FormatString = "F1";
            colJobProgress.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJobProgress.AppearanceCell.Options.UseForeColor = true;
            colJobProgress.AppearanceCell.Options.UseTextOptions = true;
            colJobProgress.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            GridColumn colMaterial = gridViewJobs.Columns.AddField("Material");
            colMaterial.Caption = "Malzeme";
            colMaterial.VisibleIndex = 5;
            colMaterial.Width = 52;
            colMaterial.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colMaterial.AppearanceCell.Options.UseForeColor = true;
            colMaterial.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            // Silme sütunu ekle (unbound column)
            GridColumn colJobDelete = new GridColumn();
            colJobDelete.FieldName = "DeleteAction";
            colJobDelete.Caption = "İşlem";
            colJobDelete.VisibleIndex = 6;
            colJobDelete.Width = 52;
            colJobDelete.UnboundType = DevExpress.Data.UnboundColumnType.String;
            colJobDelete.OptionsColumn.AllowEdit = false;
            colJobDelete.OptionsColumn.ReadOnly = true;
            colJobDelete.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            colJobDelete.OptionsColumn.AllowGroup = DevExpress.Utils.DefaultBoolean.False;
            colJobDelete.OptionsFilter.AllowFilter = false;
            colJobDelete.Visible = true;
            colJobDelete.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJobDelete.AppearanceCell.Options.UseForeColor = true;
            colJobDelete.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            gridViewJobs.Columns.Add(colJobDelete);
            
            // Unbound column için veri sağlama
            gridViewJobs.CustomUnboundColumnData += GridViewJobs_CustomUnboundColumnData;
            
            // Silme butonu tıklama olayı
            gridViewJobs.MouseDown += GridViewJobs_MouseDown;

            gridViewJobs.OptionsView.ShowGroupPanel = false;
            gridViewJobs.OptionsView.ShowIndicator = true;
            gridViewJobs.OptionsView.ColumnAutoWidth = false;
            gridViewJobs.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
            gridViewJobs.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;
            // İnce yatay çizgiler
            gridViewJobs.Appearance.HorzLine.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            gridViewJobs.Appearance.HorzLine.Options.UseBackColor = true;
        }

        private void SetupEventHandlers()
        {
            _jobAssignmentService.JobAssigned += (s, e) =>
            {
                this.Invoke(new Action(() =>
                {
                    RefreshData();
                    lblStatus.Text = $"● İş atandı: {e.Job.ModelFileName} -> Yazıcı {e.Job.PrinterId}";
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(255, 235, 59);
                }));
            };

            _jobAssignmentService.JobCompleted += (s, e) =>
            {
                this.Invoke(new Action(() =>
                {
                    RefreshData();
                    
                    // Sipariş tamamlandı mı kontrol et ve kazancı güncelle
                    var order = _orderService.GetOrder(e.Job.OrderId);
                    if (order != null && order.Status == OrderStatus.Completed)
                    {
                        // Kazancı güncelle
                        UpdateStatistics();
                        System.Diagnostics.Debug.WriteLine($"[MainForm] Sipariş #{order.Id} tamamlandı, kazanç güncellendi: {order.TotalPrice} TL");
                    }
                    
                    lblStatus.Text = $"✓ İş tamamlandı: {e.Job.ModelFileName}";
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(129, 199, 132);
                }));
            };

            _jobAssignmentService.FilamentDepleted += (s, e) =>
            {
                this.Invoke(new Action(() =>
                {
                    RefreshData();
                    XtraMessageBox.Show(
                        $"⚠️ FİLAMENT BİTTİ!\n\n" +
                        $"Yazıcı: {e.Printer.Name}\n" +
                        $"İş: {e.Job.ModelFileName}\n" +
                        $"Filament: {e.Printer.FilamentType}\n\n" +
                        $"İşlem durduruldu. Filament yenilendikten sonra iş devam edecek.",
                        "Filament Bitti",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                    lblStatus.Text = $"⚠ Filament bitti: {e.Printer.Name}";
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(244, 67, 54);
                }));
            };
            
            _jobAssignmentService.PrintersUpdated += (s, e) =>
            {
                this.Invoke(new Action(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[MainForm] PrintersUpdated event alındı, RefreshData() çağrılıyor");
                    RefreshData();
                }));
            };
        }

        private void InitializeData()
        {
            // Grid'leri görünür yap
            if (gridControlPrinters != null) gridControlPrinters.Visible = true;
            if (gridControlOrders != null) gridControlOrders.Visible = true;
            if (gridControlJobs != null) gridControlJobs.Visible = true;
            
            // Tema uygulamasını yenile
            ApplyTheme();
            
            RefreshData();
        }

        private void RefreshData()
        {
            if (gridControlPrinters == null || gridViewPrinters == null) return;
            if (gridControlOrders == null || gridViewOrders == null) return;
            if (gridControlJobs == null || gridViewJobs == null) return;

            try
            {
                var printers = _printerService.GetAllPrinters();
                System.Diagnostics.Debug.WriteLine($"[MainForm] RefreshData() - {printers.Count} yazıcı yüklendi");
                
                // Yazıcı durumlarını kontrol et ve logla
                int printingCount = 0;
                foreach (var printer in printers)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainForm] Yazıcı #{printer.Id}: Status={printer.Status}, Job={printer.CurrentJobName ?? "(null)"}, Progress={printer.Progress:F1}%");
                    if (printer.Status == PrinterStatus.Printing && !string.IsNullOrEmpty(printer.CurrentJobName))
                    {
                        printingCount++;
                        System.Console.WriteLine($"[MainForm] ✓ Yazıcı #{printer.Id} Printing: Job={printer.CurrentJobName}, Progress={printer.Progress:F1}%");
                    }
                }
                System.Console.WriteLine($"[MainForm] Toplam {printingCount} yazıcı Printing durumunda");
                
                // Grid'i güncelle
                gridViewPrinters.BeginUpdate();
                try
                {
                    // DataSource'u null yap ve tekrar ayarla - bu grid'in tam yenilenmesini sağlar
                    gridControlPrinters.DataSource = null;
                    gridControlPrinters.DataSource = printers;
                }
                finally
                {
                    gridViewPrinters.EndUpdate();
                }
                
                // Grid'i yenile - yazıcı durumlarının görünmesi için
                gridControlPrinters.RefreshDataSource();
                gridViewPrinters.RefreshData();
                
                // Yazıcı iconlarını güncelle
                UpdatePrinterIcons();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Printers grid error: {ex.Message}");
                System.Console.WriteLine($"[MainForm] ✗ Printers grid error: {ex.Message}");
            }

            try
            {
                gridViewOrders.BeginUpdate();
                gridControlOrders.DataSource = _orderService.GetAllOrders();
                
                // Silme sütununun görünür olduğundan emin ol
                var deleteColumn = gridViewOrders.Columns["DeleteAction"];
                if (deleteColumn != null)
                {
                    deleteColumn.Visible = true;
                    deleteColumn.VisibleIndex = 6;
                }
                // Tema renklerini uygula
                if (_currentTheme == ThemeMode.Dark)
                {
                    gridViewOrders.Appearance.Row.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                    gridViewOrders.Appearance.Row.BackColor = System.Drawing.Color.FromArgb(35, 35, 35);
                    gridViewOrders.Appearance.Row.Options.UseBackColor = true;
                    if (gridControlOrders != null)
                        gridControlOrders.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
                }
                else
                {
                    gridViewOrders.Appearance.Row.ForeColor = System.Drawing.Color.Black;
                    gridViewOrders.Appearance.Row.BackColor = System.Drawing.Color.White;
                    gridViewOrders.Appearance.Row.Options.UseBackColor = true;
                    gridViewOrders.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
                    gridViewOrders.Appearance.Empty.Options.UseBackColor = true;
                    if (gridControlOrders != null)
                        gridControlOrders.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
                }
                gridViewOrders.Appearance.Row.Options.UseForeColor = true;
                gridViewOrders.EndUpdate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Orders grid error: {ex.Message}");
            }

            try
            {
                gridViewJobs.BeginUpdate();
                gridControlJobs.DataSource = _jobAssignmentService.GetAllJobs();
                // Tema renklerini uygula
                if (_currentTheme == ThemeMode.Dark)
                {
                    gridViewJobs.Appearance.Row.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                    gridViewJobs.Appearance.Row.BackColor = System.Drawing.Color.FromArgb(35, 35, 35);
                    gridViewJobs.Appearance.Row.Options.UseBackColor = true;
                    if (gridControlJobs != null)
                        gridControlJobs.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
                }
                else
                {
                    gridViewJobs.Appearance.Row.ForeColor = System.Drawing.Color.Black;
                    gridViewJobs.Appearance.Row.BackColor = System.Drawing.Color.White;
                    gridViewJobs.Appearance.Row.Options.UseBackColor = true;
                    gridViewJobs.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
                    gridViewJobs.Appearance.Empty.Options.UseBackColor = true;
                    if (gridControlJobs != null)
                        gridControlJobs.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
                }
                gridViewJobs.Appearance.Row.Options.UseForeColor = true;
                gridViewJobs.EndUpdate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Jobs grid error: {ex.Message}");
            }

            // İstatistikleri güncelle
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            if (lblTotalPrinters == null || lblActivePrinters == null || lblTotalOrders == null || lblPendingJobs == null)
                return;

            var printers = _printerService.GetAllPrinters();
            var orders = _orderService.GetAllOrders();
            var jobs = _jobAssignmentService.GetAllJobs();

            lblTotalPrinters.Text = printers.Count.ToString();
            lblActivePrinters.Text = printers.Count(p => p.Status == PrinterStatus.Printing).ToString();
            lblTotalOrders.Text = orders.Count.ToString();
            lblPendingJobs.Text = jobs.Count(j => j.Status == JobStatus.Queued).ToString();
            
            // Tamamlanan iş sayısını güncelle
            var completedJobsCount = jobs.Count(j => j.Status == JobStatus.Completed);
            var statsPanel = this.Controls.OfType<System.Windows.Forms.Panel>()
                .FirstOrDefault(p => p.Controls.OfType<LabelControl>().Any(l => l.Text.Contains("İSTATİSTİKLER")));
            if (statsPanel != null)
            {
                var completedLabel = statsPanel.Controls.OfType<LabelControl>()
                    .FirstOrDefault(l => l.Name == "lblCompletedJobs");
                if (completedLabel != null)
                {
                    completedLabel.Text = completedJobsCount.ToString();
                }
            }

            // Toplam kazancı güncelle
            if (lblTotalEarnings != null)
            {
                var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
                decimal totalEarnings = completedOrders.Sum(o => o.TotalPrice);
                lblTotalEarnings.Text = $"{totalEarnings:N2} TL";
            }
        }

        private void CheckOrderStatusesOnStartup()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainForm] Program başlatıldığında sipariş durumları kontrol ediliyor...");
                
                var orders = _orderService.GetAllOrders();
                var jobs = _jobAssignmentService.GetAllJobs();
                int updatedCount = 0;
                
                foreach (var order in orders)
                {
                    // Sadece Pending veya Processing durumundaki siparişleri kontrol et
                    if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Processing)
                    {
                        // Bu siparişe ait tüm işleri bul
                        var orderJobs = jobs.Where(j => j.OrderId == order.Id).ToList();
                        
                        if (orderJobs.Count > 0)
                        {
                            // Tüm işler tamamlandı mı kontrol et
                            bool allCompleted = orderJobs.All(j => j.Status == JobStatus.Completed);
                            
                            if (allCompleted)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MainForm] Sipariş #{order.Id} - Tüm {orderJobs.Count} iş tamamlandı, durum Completed olarak güncelleniyor");
                                _orderService.UpdateOrderStatus(order.Id, OrderStatus.Completed);
                                updatedCount++;
                            }
                        }
                    }
                }
                
                if (updatedCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainForm] ✓ {updatedCount} sipariş durumu Completed olarak güncellendi");
                    System.Console.WriteLine($"[MainForm] ✓ {updatedCount} sipariş durumu Completed olarak güncellendi");
                    // Kazancı güncelle
                    UpdateStatistics();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainForm] Sipariş durumları kontrol edilirken hata: {ex.Message}");
            }
        }

        private void StartRefreshTimer()
        {
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 2000; // 2 saniyede bir güncelle
            _refreshTimer.Tick += (s, e) => RefreshData();
            _refreshTimer.Start();
        }

        private void BtnSimulateOrder_Click(object sender, EventArgs e)
        {
            var order = _orderService.SimulateECommerceOrder();
            _jobAssignmentService.ProcessNewOrder(order);
            
            RefreshData();
            
            // MongoDB durumunu göster
            string mongoStatus = _mongoDbConnected ? "✓ MongoDB'ye kaydedildi" : "⚠ MongoDB'ye kaydedilemedi (sadece bellek)";
            lblStatus.Text = $"✓ Yeni sipariş alındı: {order.OrderNumber} - {mongoStatus}";
            lblStatus.ForeColor = _mongoDbConnected ? System.Drawing.Color.FromArgb(129, 199, 132) : System.Drawing.Color.FromArgb(255, 193, 7);
            
            // Model setini belirle (ilk item'ın klasör adından)
            string modelSet = "Bilinmeyen";
            if (order.Items.Count > 0)
            {
                var firstItem = order.Items[0].ModelFileName;
                if (firstItem.Contains("/"))
                {
                    modelSet = firstItem.Split('/')[0];
                }
            }

            int totalQuantity = order.Items.Sum(item => item.Quantity);

            string message = $"Yeni sipariş oluşturuldu!\n\n" +
                $"Sipariş No: {order.OrderNumber}\n" +
                $"Müşteri: {order.CustomerName}\n" +
                $"Model Seti: {modelSet}\n" +
                $"Model Dosyası Sayısı: {order.Items.Count}\n" +
                $"Toplam Adet: {totalQuantity}\n" +
                $"Toplam Fiyat: {order.TotalPrice:C2}\n\n" +
                $"{mongoStatus}";

            XtraMessageBox.Show(
                message,
                "Sipariş Alındı",
                System.Windows.Forms.MessageBoxButtons.OK,
                _mongoDbConnected ? System.Windows.Forms.MessageBoxIcon.Information : System.Windows.Forms.MessageBoxIcon.Warning);
        }

        private void BtnShowModels_Click(object sender, EventArgs e)
        {
            try
            {
                // Modelleri gösteren form oluştur
                var modelsForm = new System.Windows.Forms.Form
                {
                    Text = "📦 Modeller",
                    Size = new System.Drawing.Size(800, 600),
                    StartPosition = System.Windows.Forms.FormStartPosition.CenterParent,
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(30, 30, 30) : 
                        System.Drawing.Color.FromArgb(245, 247, 250)
                };

                // Model listesi için ListBox
                var listBoxModels = new System.Windows.Forms.ListBox
                {
                    Location = new System.Drawing.Point(20, 20),
                    Size = new System.Drawing.Size(750, 450),
                    Font = new System.Drawing.Font("Segoe UI", 10F),
                    BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                    BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(40, 40, 40) : 
                        System.Drawing.Color.White,
                    ForeColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.White : 
                        System.Drawing.Color.Black
                };
                
                modelsForm.Controls.Add(listBoxModels);

                // Modelleri yükle - dosya yollarını saklamak için Dictionary kullan
                var modelFilePaths = new Dictionary<string, string>(); // Görünen metin -> Tam dosya yolu
                
                try
                {
                    var modelPath = GetModelFolderPath();
                    if (!string.IsNullOrEmpty(modelPath) && Directory.Exists(modelPath))
                    {
                        var subfolders = Directory.GetDirectories(modelPath);
                        foreach (var subfolder in subfolders)
                        {
                            var folderName = Path.GetFileName(subfolder);
                            var stlFiles = Directory.GetFiles(subfolder, "*.stl");
                            
                            if (stlFiles.Length > 0)
                            {
                                listBoxModels.Items.Add($"📁 {folderName}/");
                                foreach (var stlFile in stlFiles)
                                {
                                    var fileName = Path.GetFileName(stlFile);
                                    var displayText = $"   └─ {fileName}";
                                    listBoxModels.Items.Add(displayText);
                                    // Dosya yolunu sakla
                                    modelFilePaths[displayText] = stlFile;
                                    System.Diagnostics.Debug.WriteLine($"[MainForm] Model eklendi: {displayText} -> {stlFile}");
                                }
                            }
                        }
                    }
                    else
                    {
                        // Varsayılan modeller (dosya yolu yok, sadece gösterim)
                        listBoxModels.Items.Add("📁 octo/");
                        listBoxModels.Items.Add("   └─ articulatedcuteoctopus.stl");
                        listBoxModels.Items.Add("📁 shark/");
                        listBoxModels.Items.Add("   └─ body.stl");
                        listBoxModels.Items.Add("   └─ head_easy_press_in.stl");
                        listBoxModels.Items.Add("   └─ head_hard_press_in.stl");
                        listBoxModels.Items.Add("📁 whist/");
                        listBoxModels.Items.Add("   └─ v29d_engraved.stl");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Modeller yüklenirken hata: {ex.Message}");
                    listBoxModels.Items.Add("⚠ Modeller yüklenirken hata oluştu: " + ex.Message);
                }

                // AI Model Analiz Butonu
                var btnAIAnalysis = new SimpleButton
                {
                    Text = "🤖 AI ile Model Analiz Et",
                    Location = new System.Drawing.Point(20, 490),
                    Size = new System.Drawing.Size(250, 50),
                    Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold)
                };
                btnAIAnalysis.Appearance.BackColor = System.Drawing.Color.FromArgb(63, 81, 181);
                btnAIAnalysis.Appearance.ForeColor = System.Drawing.Color.White;
                btnAIAnalysis.Appearance.Options.UseBackColor = true;
                btnAIAnalysis.Appearance.Options.UseForeColor = true;
                btnAIAnalysis.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(92, 107, 192);
                btnAIAnalysis.AppearanceHovered.Options.UseBackColor = true;
                btnAIAnalysis.LookAndFeel.UseDefaultLookAndFeel = false;
                btnAIAnalysis.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
                btnAIAnalysis.Click += (s, args) =>
                {
                    try
                    {
                        // Seçili modeli al
                        if (listBoxModels.SelectedItem == null)
                        {
                            XtraMessageBox.Show(
                                "Lütfen analiz etmek için bir model seçin!",
                                "Uyarı",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Warning);
                            return;
                        }

                        string selectedItem = listBoxModels.SelectedItem.ToString();
                        if (selectedItem.StartsWith("📁") || !selectedItem.Contains("└─"))
                        {
                            XtraMessageBox.Show(
                                "Lütfen bir model dosyası seçin (klasör değil)!",
                                "Uyarı",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Warning);
                            return;
                        }

                        // Dictionary'den dosya yolunu al
                        string fullPath = null;
                        if (modelFilePaths.ContainsKey(selectedItem))
                        {
                            fullPath = modelFilePaths[selectedItem];
                            System.Diagnostics.Debug.WriteLine($"[MainForm] Dictionary'den dosya yolu bulundu: {fullPath}");
                            System.Console.WriteLine($"[MainForm] Dictionary'den dosya yolu bulundu: {fullPath}");
                        }
                        else
                        {
                            // Dictionary'de yoksa, dosya adından ve model path'den oluştur
                            string modelFileName = selectedItem.Replace("   └─ ", "").Trim();
                            string modelPath = GetModelFolderPath();
                            
                            System.Diagnostics.Debug.WriteLine($"[MainForm] Dictionary'de bulunamadı, arama yapılıyor: {modelFileName}");
                            System.Console.WriteLine($"[MainForm] Dictionary'de bulunamadı, arama yapılıyor: {modelFileName}");
                            
                            if (!string.IsNullOrEmpty(modelPath) && Directory.Exists(modelPath))
                            {
                                // Tüm klasörlerde ara
                                var subfolders = Directory.GetDirectories(modelPath);
                                foreach (var subfolder in subfolders)
                                {
                                    var stlFile = Path.Combine(subfolder, modelFileName);
                                    System.Diagnostics.Debug.WriteLine($"[MainForm] Kontrol ediliyor: {stlFile}");
                                    if (File.Exists(stlFile))
                                    {
                                        fullPath = stlFile;
                                        System.Diagnostics.Debug.WriteLine($"[MainForm] Dosya bulundu: {fullPath}");
                                        System.Console.WriteLine($"[MainForm] Dosya bulundu: {fullPath}");
                                        break;
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                        {
                            XtraMessageBox.Show(
                                $"Model dosyası bulunamadı!\n\nSeçili: {selectedItem}\n\nLütfen dosya yolunu kontrol edin.",
                                "Hata",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Error);
                            return;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[MainForm] Analiz için dosya yolu: {fullPath}");
                        System.Console.WriteLine($"[MainForm] Analiz için dosya yolu: {fullPath}");

                        // AI analiz servisi
                        var analysisService = new Services.ModelAnalysisService();
                        
                        // Analiz yap (async işlem olduğu için biraz zaman alabilir)
                        System.Diagnostics.Debug.WriteLine($"[MainForm] Model analizi başlatılıyor: {fullPath}");
                        var result = analysisService.AnalyzeModel(fullPath);
                        System.Diagnostics.Debug.WriteLine($"[MainForm] Model analizi tamamlandı. UsedAI: {result.UsedAI}");

                        // Sonuçları göster
                        string message = $"🤖 AI MODEL ANALİZ SONUÇLARI\n\n" +
                            $"📦 Model: {result.ModelName}\n\n" +
                            $"📊 TAHMİNLER:\n" +
                            $"   • Filament: {result.EstimatedFilamentGrams:F1} g ({result.EstimatedFilamentMeters:F1} m)\n" +
                            $"   • Baskı Süresi: {result.EstimatedPrintTimeHours:F2} saat\n\n" +
                            $"💰 MALİYET ANALİZİ:\n" +
                            $"   • Filament Maliyeti: {result.FilamentCost:F2} TL\n" +
                            $"   • Toplam Maliyet: {result.TotalCost:F2} TL\n\n" +
                            $"💵 ÖNERİLEN SATIŞ FİYATI:\n" +
                            $"   🎯 {result.RecommendedPrice:F2} TL\n\n" +
                            $"📈 Kar Marjı: %50 ({result.ProfitMargin:F2} TL)";
                        
                        if (!string.IsNullOrEmpty(result.GeminiAnalysis))
                        {
                            string wrappedAnalysis = WrapText(result.GeminiAnalysis, 70);
                            if (result.UsedAI)
                            {
                                message += $"\n\n📋 DETAYLI ANALİZ:\n{wrappedAnalysis}";
                            }
                            else
                            {
                                message += $"\n\n⚠️ Gemini AI Durumu:\n{wrappedAnalysis}";
                            }
                        }
                        else if (!result.UsedAI)
                        {
                            message += $"\n\n💡 Not: Gemini AI kullanmak için App.config dosyasına 'GeminiApiKey' ekleyin.";
                        }

                        // Her iki temada da aynı XtraMessageBox kullan
                        XtraMessageBox.Show(
                            message,
                            "🤖 AI Model Analizi",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show(
                            $"Model analiz edilirken hata oluştu:\n\n{ex.Message}",
                            "Hata",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Error);
                    }
                };
                modelsForm.Controls.Add(btnAIAnalysis);

                // Blender AI ile Model Oluştur butonu
                var btnBlenderAI = new SimpleButton
                {
                    Text = "🎨 Blender AI ile Model Oluştur",
                    Location = new System.Drawing.Point(280, 490),
                    Size = new System.Drawing.Size(250, 50),
                    Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold)
                };
                btnBlenderAI.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 152, 0);
                btnBlenderAI.Appearance.ForeColor = System.Drawing.Color.White;
                btnBlenderAI.Appearance.Options.UseBackColor = true;
                btnBlenderAI.Appearance.Options.UseForeColor = true;
                btnBlenderAI.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(255, 167, 38);
                btnBlenderAI.AppearanceHovered.Options.UseBackColor = true;
                btnBlenderAI.LookAndFeel.UseDefaultLookAndFeel = false;
                btnBlenderAI.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
                btnBlenderAI.Click += (s, args) =>
                {
                    try
                    {
                        var blenderPath = @"C:\Users\semih\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Blender\Blender 4.5.lnk";
                        
                        if (File.Exists(blenderPath))
                        {
                            // .lnk dosyasını açmak için Shell32 kullan
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = blenderPath,
                                UseShellExecute = true
                            });
                            
                            XtraMessageBox.Show(
                                "Blender başlatılıyor...",
                                "Blender",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Information);
                        }
                        else
                        {
                            XtraMessageBox.Show(
                                $"Blender bulunamadı!\n\nYol: {blenderPath}\n\nLütfen Blender'ın kurulu olduğundan emin olun.",
                                "Hata",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show(
                            $"Blender başlatılırken hata oluştu:\n\n{ex.Message}",
                            "Hata",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Error);
                    }
                };
                modelsForm.Controls.Add(btnBlenderAI);

                // Kapat butonu
                var btnClose = new SimpleButton
                {
                    Text = "Kapat",
                    Location = new System.Drawing.Point(650, 490),
                    Size = new System.Drawing.Size(120, 50),
                    Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold)
                };
                if (_currentTheme == ThemeMode.Dark)
                {
                    btnClose.Appearance.BackColor = System.Drawing.Color.FromArgb(66, 66, 66);
                    btnClose.Appearance.ForeColor = System.Drawing.Color.White;
                    btnClose.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(80, 80, 80);
                }
                else
                {
                btnClose.Appearance.BackColor = System.Drawing.Color.FromArgb(158, 158, 158);
                btnClose.Appearance.ForeColor = System.Drawing.Color.White;
                    btnClose.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(189, 189, 189);
                }
                btnClose.Appearance.Options.UseBackColor = true;
                btnClose.Appearance.Options.UseForeColor = true;
                btnClose.AppearanceHovered.Options.UseBackColor = true;
                btnClose.LookAndFeel.UseDefaultLookAndFeel = false;
                btnClose.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
                btnClose.Click += (s, args) => modelsForm.Close();
                modelsForm.Controls.Add(btnClose);

                // Formu göster
                modelsForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    $"Modeller gösterilirken hata oluştu:\n\n{ex.Message}",
                    "Hata",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private string GetModelFolderPath()
        {
            try
            {
                var paths = new[]
                {
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "model")),
                    Path.Combine(Directory.GetCurrentDirectory(), "model"),
                    Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? "", "model")
                };

                return paths.FirstOrDefault(Directory.Exists);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Model klasörü bulunurken hata: {ex.Message}");
                return null;
            }
        }

        private void BtnAddPrinter_Click(object sender, EventArgs e)
        {
            try
            {
                // Yazıcı modeli ve filament seçim dialog'u oluştur
                using (var dialog = new System.Windows.Forms.Form())
                {
                    dialog.Text = "Yeni Yazıcı Ekle";
                    dialog.Size = new System.Drawing.Size(500, 250);
                    dialog.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
                    dialog.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                    dialog.MaximizeBox = false;
                    dialog.MinimizeBox = false;
                    dialog.ShowInTaskbar = false;
                    dialog.BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(40, 40, 40) : 
                        System.Drawing.Color.White;

                    // Yazıcı Modeli Label
                    var lblModel = new LabelControl
                    {
                        Text = "Yazıcı Modeli:",
                        Location = new System.Drawing.Point(20, 30),
                        Size = new System.Drawing.Size(120, 20),
                        Font = new System.Drawing.Font("Segoe UI", 10F),
                        ForeColor = _currentTheme == ThemeMode.Dark ? 
                            System.Drawing.Color.FromArgb(230, 230, 230) : 
                            System.Drawing.Color.Black
                    };
                    dialog.Controls.Add(lblModel);

                    // Yazıcı Modeli ComboBox
                    var comboModel = new ComboBoxEdit
                    {
                        Location = new System.Drawing.Point(150, 27),
                        Size = new System.Drawing.Size(300, 25),
                        Font = new System.Drawing.Font("Segoe UI", 10F)
                    };
                    
                    // Yazıcı modellerini yükle
                    var models = PrinterService.GetAvailablePrinterModels();
                    comboModel.Properties.Items.AddRange(models);
                    if (comboModel.Properties.Items.Count > 0)
                        comboModel.SelectedIndex = 0;
                    
                    // Tema renkleri
                    if (_currentTheme == ThemeMode.Dark)
                    {
                        comboModel.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
                        comboModel.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                    }
                    
                    dialog.Controls.Add(comboModel);

                    // Filament Label
                    var lblFilament = new LabelControl
                    {
                        Text = "Filament Tipi:",
                        Location = new System.Drawing.Point(20, 80),
                        Size = new System.Drawing.Size(120, 20),
                        Font = new System.Drawing.Font("Segoe UI", 10F),
                        ForeColor = _currentTheme == ThemeMode.Dark ? 
                            System.Drawing.Color.FromArgb(230, 230, 230) : 
                            System.Drawing.Color.Black
                    };
                    dialog.Controls.Add(lblFilament);

                    // Filament ComboBox
                    var comboFilament = new ComboBoxEdit
                    {
                        Location = new System.Drawing.Point(150, 77),
                        Size = new System.Drawing.Size(300, 25),
                        Font = new System.Drawing.Font("Segoe UI", 10F)
                    };
                    
                    // Filament çeşitlerini yükle
                    var filamentTypes = PrinterService.GetAvailableFilamentTypes();
                    comboFilament.Properties.Items.AddRange(filamentTypes);
                    if (comboFilament.Properties.Items.Count > 0)
                        comboFilament.SelectedIndex = 0;
                    
                    // Tema renkleri
                    if (_currentTheme == ThemeMode.Dark)
                    {
                        comboFilament.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
                        comboFilament.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                    }
                    
                    dialog.Controls.Add(comboFilament);

                    // Butonlar
                    var btnOK = new SimpleButton
                    {
                        Text = "Ekle",
                        Location = new System.Drawing.Point(280, 130),
                        Size = new System.Drawing.Size(80, 35),
                        DialogResult = System.Windows.Forms.DialogResult.OK,
                        Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
                    };
                    btnOK.Appearance.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
                    btnOK.Appearance.ForeColor = System.Drawing.Color.White;
                    btnOK.Appearance.Options.UseBackColor = true;
                    btnOK.Appearance.Options.UseForeColor = true;
                    btnOK.LookAndFeel.UseDefaultLookAndFeel = false;
                    btnOK.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
                    dialog.Controls.Add(btnOK);
                    dialog.AcceptButton = btnOK;

                    var btnCancel = new SimpleButton
                    {
                        Text = "İptal",
                        Location = new System.Drawing.Point(370, 130),
                        Size = new System.Drawing.Size(80, 35),
                        DialogResult = System.Windows.Forms.DialogResult.Cancel,
                        Font = new System.Drawing.Font("Segoe UI", 10F)
                    };
                    btnCancel.Appearance.BackColor = System.Drawing.Color.FromArgb(158, 158, 158);
                    btnCancel.Appearance.ForeColor = System.Drawing.Color.White;
                    btnCancel.Appearance.Options.UseBackColor = true;
                    btnCancel.Appearance.Options.UseForeColor = true;
                    btnCancel.LookAndFeel.UseDefaultLookAndFeel = false;
                    btnCancel.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
                    dialog.Controls.Add(btnCancel);
                    dialog.CancelButton = btnCancel;

                    if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        string selectedModel = comboModel.Text;
                        string selectedFilament = comboFilament.Text;

                        if (string.IsNullOrWhiteSpace(selectedModel) || string.IsNullOrWhiteSpace(selectedFilament))
                        {
                            XtraMessageBox.Show(
                                "Lütfen bir yazıcı modeli ve filament tipi seçin!",
                                "Uyarı",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Warning);
                            return;
                        }

                        var newPrinter = _printerService.AddNewPrinter(selectedModel, selectedFilament);
                        RefreshData();
                        lblStatus.Text = $"✓ Yeni yazıcı eklendi: {newPrinter.Name}";
                        lblStatus.ForeColor = System.Drawing.Color.FromArgb(129, 199, 132);
                        
                        XtraMessageBox.Show(
                            $"Yeni yazıcı başarıyla eklendi!\n\n" +
                            $"Yazıcı Adı: {newPrinter.Name}\n" +
                            $"Yazıcı ID: {newPrinter.Id}\n" +
                            $"Durum: Boşta\n" +
                            $"Filament Tipi: {newPrinter.FilamentType}",
                            "Yazıcı Eklendi",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    $"Yazıcı eklenirken hata oluştu:\n{ex.Message}",
                    "Hata",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            _settingsPanelVisible = !_settingsPanelVisible;
            settingsPanel.Visible = _settingsPanelVisible;
            
            if (_settingsPanelVisible)
            {
                // Panel konumunu ayarla - butonun sağ altına hizala
                int panelX = btnSettings.Right - settingsPanel.Width;
                int panelY = btnSettings.Bottom + 5;
                settingsPanel.Location = new System.Drawing.Point(panelX, panelY);
                settingsPanel.BringToFront();
            }
        }

        private void BtnToggleTheme_Click(object sender, EventArgs e)
        {
            _currentTheme = _currentTheme == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
            ApplyTheme();
            // Ayarlar panelini kapat
            _settingsPanelVisible = false;
            settingsPanel.Visible = false;
        }

        private void BtnDeleteCompletedOrders_Click(object sender, EventArgs e)
        {
            try
            {
                // Tamamlanan sipariş sayısını kontrol et
                var completedOrders = _orderService.GetAllOrders().Where(o => o.Status == OrderStatus.Completed).ToList();
                int completedCount = completedOrders.Count;
                
                if (completedCount == 0)
                {
                    XtraMessageBox.Show(
                        "Tamamlanan sipariş bulunmuyor.",
                        "Bilgi",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                    return;
                }
                
                // Onay mesajı
                var result = XtraMessageBox.Show(
                    $"{completedCount} adet tamamlanan sipariş silinecek.\n\nBu işlem geri alınamaz. Devam etmek istiyor musunuz?",
                    "Tamamlanan Siparişleri Sil",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    // Siparişleri sil
                    int deletedCount = _orderService.DeleteCompletedOrders();
                    
                    // Verileri yenile
                    RefreshData();
                    
                    // Başarı mesajı
                    XtraMessageBox.Show(
                        $"{deletedCount} adet tamamlanan sipariş başarıyla silindi.",
                        "Başarılı",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                    
                    // MongoDB durumunu göster
                    string mongoStatus = _mongoDbConnected ? "✓ MongoDB'den de silindi" : "⚠ Sadece bellekten silindi";
                    lblStatus.Text = $"✓ {deletedCount} tamamlanan sipariş silindi - {mongoStatus}";
                    lblStatus.ForeColor = _mongoDbConnected ? System.Drawing.Color.FromArgb(129, 199, 132) : System.Drawing.Color.FromArgb(255, 193, 7);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    $"Siparişler silinirken bir hata oluştu:\n\n{ex.Message}",
                    "Hata",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                
                System.Diagnostics.Debug.WriteLine($"[MainForm] Siparişler silinirken hata: {ex.Message}");
            }
        }

        private void BtnDeleteCompletedJobs_Click(object sender, EventArgs e)
        {
            try
            {
                // Tamamlanan iş sayısını kontrol et
                var completedJobs = _jobAssignmentService.GetAllJobs().Where(j => j.Status == JobStatus.Completed).ToList();
                int completedCount = completedJobs.Count;
                
                if (completedCount == 0)
                {
                    XtraMessageBox.Show(
                        "Tamamlanan iş bulunmuyor.",
                        "Bilgi",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                    return;
                }
                
                // Onay mesajı
                var result = XtraMessageBox.Show(
                    $"{completedCount} adet tamamlanan iş silinecek.\n\nBu işlem geri alınamaz. Devam etmek istiyor musunuz?",
                    "Tamamlanan İşleri Sil",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    // İşleri sil
                    int deletedCount = _jobAssignmentService.DeleteCompletedJobs();
                    
                    // Verileri yenile
                    RefreshData();
                    
                    // Başarı mesajı
                    XtraMessageBox.Show(
                        $"{deletedCount} adet tamamlanan iş başarıyla silindi.",
                        "Başarılı",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                    
                    // MongoDB durumunu göster
                    string mongoStatus = _mongoDbConnected ? "✓ MongoDB'den de silindi" : "⚠ Sadece bellekten silindi";
                    lblStatus.Text = $"✓ {deletedCount} tamamlanan iş silindi - {mongoStatus}";
                    lblStatus.ForeColor = _mongoDbConnected ? System.Drawing.Color.FromArgb(129, 199, 132) : System.Drawing.Color.FromArgb(255, 193, 7);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    $"Tamamlanan işler silinirken hata oluştu:\n{ex.Message}",
                    "Hata",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void BtnShowEarnings_Click(object sender, EventArgs e)
        {
            try
            {
                // Tamamlanan siparişleri al
                var completedOrders = _orderService.GetAllOrders()
                    .Where(o => o.Status == OrderStatus.Completed)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                // Hesaplamalar
                decimal totalRevenue = completedOrders.Sum(o => o.TotalPrice);
                int orderCount = completedOrders.Count;
                
                // Maliyet hesaplama (sipariş başına ortalama %40 maliyet varsayıyoruz)
                decimal totalCost = totalRevenue * 0.40m;
                decimal netProfit = totalRevenue - totalCost;
                decimal profitMargin = totalRevenue > 0 ? (netProfit / totalRevenue) * 100 : 0;

                // DevExpress XtraForm oluştur
                var earningsForm = new XtraForm
                {
                    Text = "💰 Kazanç Detayları",
                    Size = new System.Drawing.Size(900, 650),
                    StartPosition = System.Windows.Forms.FormStartPosition.CenterParent,
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(30, 30, 30) : 
                        System.Drawing.Color.FromArgb(245, 247, 250)
                };

                // Ana Panel - Gradient arka plan
                var mainPanel = new System.Windows.Forms.Panel
                {
                    Dock = System.Windows.Forms.DockStyle.Fill,
                    Padding = new System.Windows.Forms.Padding(20),
                    BackColor = System.Drawing.Color.Transparent
                };
                mainPanel.Paint += (s, e) =>
                {
                    var panel = s as System.Windows.Forms.Panel;
                    if (panel == null) return;
                    
                    System.Drawing.Color color1, color2;
                    if (_currentTheme == ThemeMode.Dark)
                    {
                        color1 = System.Drawing.Color.FromArgb(40, 40, 40);
                        color2 = System.Drawing.Color.FromArgb(25, 25, 25);
                    }
                    else
                    {
                        color1 = System.Drawing.Color.FromArgb(250, 250, 250);
                        color2 = System.Drawing.Color.FromArgb(240, 242, 245);
                    }
                    
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        panel.ClientRectangle,
                        color1,
                        color2,
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillRectangle(brush, panel.ClientRectangle);
                    }
                };
                earningsForm.Controls.Add(mainPanel);

                // Başlık Panel - Gradient (Mavi-Mor)
                var titlePanel = new System.Windows.Forms.Panel
                {
                    Location = new System.Drawing.Point(0, 0),
                    Size = new System.Drawing.Size(860, 50),
                    BackColor = System.Drawing.Color.Transparent
                };
                titlePanel.Paint += (s, e) =>
                {
                    var panel = s as System.Windows.Forms.Panel;
                    if (panel == null) return;
                    
                    // Altın renginden siyaha yakın sarı rengine gradient
                    System.Drawing.Color color1 = System.Drawing.Color.FromArgb(255, 215, 0); // Altın rengi
                    System.Drawing.Color color2 = System.Drawing.Color.FromArgb(120, 100, 0); // Çok koyu sarı (siyaha yakın)
                    
                    int radius = 10;
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                        path.AddArc(panel.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                        path.AddArc(panel.Width - radius * 2, panel.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                        path.AddArc(0, panel.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                        path.CloseAllFigures();
                        
                        using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                            panel.ClientRectangle,
                            color1,
                            color2,
                            System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                        {
                            e.Graphics.FillPath(brush, path);
                        }
                    }
                };
                mainPanel.Controls.Add(titlePanel);

                // Başlık
                var lblTitle = new LabelControl
                {
                    Text = "💰 KAZANÇ DETAYLARI",
                    Location = new System.Drawing.Point(0, 0),
                    Size = new System.Drawing.Size(860, 50),
                    Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold),
                    ForeColor = System.Drawing.Color.White
                };
                lblTitle.Appearance.BackColor = System.Drawing.Color.Transparent;
                lblTitle.Appearance.Options.UseBackColor = true;
                lblTitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                lblTitle.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                titlePanel.Controls.Add(lblTitle);

                // Özet Kartları (Üst kısım - ortalanmış)
                int cardY = 70;
                int cardWidth = 200;
                int cardHeight = 120;
                int cardSpacing = 15;
                int totalCardsWidth = (cardWidth * 4) + (cardSpacing * 3);
                int cardsStartX = (860 - totalCardsWidth) / 2; // Kartları ortala

                // Toplam Gelir Kartı (ortalanmış)
                var revenueCard = CreateSummaryCard("Toplam Gelir", totalRevenue.ToString("N2") + " TL", 
                    System.Drawing.Color.FromArgb(33, 150, 243), cardsStartX, cardY, cardWidth, cardHeight);
                mainPanel.Controls.Add(revenueCard);

                // Toplam Maliyet Kartı (ortalanmış)
                var costCard = CreateSummaryCard("Toplam Maliyet", totalCost.ToString("N2") + " TL", 
                    System.Drawing.Color.FromArgb(244, 67, 54), cardsStartX + cardWidth + cardSpacing, cardY, cardWidth, cardHeight);
                mainPanel.Controls.Add(costCard);

                // Net Kazanç Kartı (ortalanmış)
                var profitCard = CreateSummaryCard("Net Kazanç", netProfit.ToString("N2") + " TL", 
                    System.Drawing.Color.FromArgb(76, 175, 80), cardsStartX + (cardWidth + cardSpacing) * 2, cardY, cardWidth, cardHeight);
                mainPanel.Controls.Add(profitCard);

                // Kar/Zarar Kartı (ortalanmış)
                var profitMarginCard = CreateSummaryCard("Kar Marjı", profitMargin.ToString("F1") + " %", 
                    netProfit >= 0 ? System.Drawing.Color.FromArgb(27, 94, 32) : System.Drawing.Color.FromArgb(244, 67, 54), // Daha koyu yeşil
                    cardsStartX + (cardWidth + cardSpacing) * 3, cardY, cardWidth, cardHeight);
                mainPanel.Controls.Add(profitMarginCard);

                // Sipariş Sayısı Bilgisi
                var lblOrderCount = new LabelControl
                {
                    Text = $"📦 Tamamlanan Sipariş Sayısı: {orderCount}",
                    Location = new System.Drawing.Point(0, cardY + cardHeight + 20),
                    Size = new System.Drawing.Size(860, 25),
                    Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold),
                    ForeColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(200, 200, 200) : 
                        System.Drawing.Color.FromArgb(100, 100, 100)
                };
                lblOrderCount.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                mainPanel.Controls.Add(lblOrderCount);

                // Grid için Panel (ortalanmış)
                int gridPanelWidth = 840;
                int gridPanelHeight = 350;
                int gridPanelX = (860 - gridPanelWidth) / 2; // Ortala
                var gridPanel = new System.Windows.Forms.Panel
                {
                    Location = new System.Drawing.Point(gridPanelX, cardY + cardHeight + 55),
                    Size = new System.Drawing.Size(gridPanelWidth, gridPanelHeight),
                    Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | 
                             System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right,
                    BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(30, 30, 30) : 
                        System.Drawing.Color.Transparent
                };
                mainPanel.Controls.Add(gridPanel);

                // Siparişler Grid
                var gridControl = new GridControl
                {
                    Dock = System.Windows.Forms.DockStyle.Fill,
                    DataSource = completedOrders.Select(o => new
                    {
                        SiparişNo = o.OrderNumber,
                        Müşteri = o.CustomerName,
                        Tarih = o.OrderDate.ToString("dd.MM.yyyy HH:mm"),
                        Tutar = o.TotalPrice,
                        ÜrünSayısı = o.Items.Count,
                        Durum = o.Status.ToString()
                    }).ToList()
                };
                gridPanel.Controls.Add(gridControl);

                var gridView = new GridView(gridControl);
                gridControl.MainView = gridView;
                gridView.OptionsBehavior.Editable = false;
                gridView.OptionsView.ShowGroupPanel = false;
                gridView.OptionsView.ShowIndicator = true;
                gridView.PaintStyleName = "Flat";
                // Otomatik sütun oluşturmayı tamamen kapat
                gridView.OptionsView.ShowAutoFilterRow = false;
                gridView.OptionsCustomization.AllowQuickHideColumns = false;
                gridView.OptionsCustomization.AllowColumnMoving = false;
                gridView.OptionsCustomization.AllowColumnResizing = true;
                gridView.OptionsCustomization.AllowSort = true;
                // Sütunların toplam genişliğini form genişliğine uydur
                gridView.OptionsView.ColumnAutoWidth = true;
                // Modern görünüm
                gridView.RowHeight = 42; // Satırları daha geniş yap
                gridView.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
                gridView.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;
                gridView.Appearance.HorzLine.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
                gridView.Appearance.HorzLine.Options.UseBackColor = true;
                
                // İlk açılışta hiçbir satır seçili olmasın
                gridView.OptionsSelection.EnableAppearanceFocusedRow = true;
                gridView.FocusedRowHandle = DevExpress.XtraGrid.GridControl.InvalidRowHandle;
                
                // Grid tema ayarları
                if (_currentTheme == ThemeMode.Dark)
                {
                    if (gridView.GridControl != null)
                    {
                        gridView.GridControl.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
                    }
                    gridView.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
                    gridView.Appearance.Empty.Options.UseBackColor = true;
                    gridView.Appearance.Row.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                    gridView.Appearance.Row.BackColor = System.Drawing.Color.FromArgb(35, 35, 35);
                    gridView.Appearance.Row.Options.UseForeColor = true;
                    gridView.Appearance.Row.Options.UseBackColor = true;
                }
                else
                {
                    if (gridView.GridControl != null)
                    {
                        gridView.GridControl.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
                    }
                    gridView.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
                    gridView.Appearance.Empty.Options.UseBackColor = true;
                    gridView.Appearance.Row.ForeColor = System.Drawing.Color.Black;
                    gridView.Appearance.Row.BackColor = System.Drawing.Color.White;
                    gridView.Appearance.Row.Options.UseForeColor = true;
                    gridView.Appearance.Row.Options.UseBackColor = true;
                }
                
                // Grid Header - Gradient (Mavi-Mor)
                gridView.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.White;
                gridView.Appearance.HeaderPanel.Options.UseForeColor = true;
                gridView.Appearance.HeaderPanel.BackColor = System.Drawing.Color.Transparent;
                gridView.Appearance.HeaderPanel.Options.UseBackColor = true;
                // Header için CustomDraw event - Düz mavi renk
                gridView.CustomDrawColumnHeader += (s, e) =>
                {
                    // Düz mavi arka plan (gradient değil)
                    System.Drawing.Color blueColor = System.Drawing.Color.FromArgb(0, 120, 215);
                    
                    using (var brush = new System.Drawing.SolidBrush(blueColor))
                    {
                        e.Graphics.FillRectangle(brush, e.Bounds);
                    }
                    
                    // Metni çiz - Column.Caption kullan
                    string captionText = e.Column != null ? e.Column.Caption : "";
                    e.Appearance.DrawString(e.Cache, captionText, e.Bounds);
                    e.Handled = true;
                };
                
                // Seçili satır için CustomDrawCell event - Tüm satır için tek gradient (mavi-mor)
                gridView.CustomDrawCell += (s, e) =>
                {
                    // Sadece seçili/focused satır için gradient uygula
                    if (e.RowHandle == gridView.FocusedRowHandle)
                    {
                        int rowTop = e.Bounds.Top;
                        int rowHeight = e.Bounds.Height;
                        
                        // Grid'in görünür alanını al
                        var gridControl = gridView.GridControl;
                        if (gridControl != null)
                        {
                            // GridView'in görünür sütun alanının genişliğini al
                            var viewRect = gridView.ViewRect;
                            int columnsWidth = viewRect.Width;
                            int firstColumnLeft = viewRect.Left;
                            
                            // Tüm satır için gradient bounds (çizgileri kapsamak için daha yüksek)
                            var fullRowBounds = new System.Drawing.Rectangle(
                                firstColumnLeft,
                                rowTop,
                                columnsWidth,
                                rowHeight + 5); // Alt çizgiyi tamamen kapsamak için daha fazla alan
                            
                            // Gradient arka plan (mavi-mor) - tüm satır için
                            System.Drawing.Color color1 = System.Drawing.Color.FromArgb(0, 120, 215);
                            System.Drawing.Color color2 = System.Drawing.Color.FromArgb(177, 70, 194);
                            
                            // Hücreyi 5 piksel daha yüksek çiz ki alt çizgiyi tamamen kapsasın
                            var cellBounds = new System.Drawing.Rectangle(
                                e.Bounds.Left,
                                e.Bounds.Top,
                                e.Bounds.Width,
                                e.Bounds.Height + 5); // Alt çizgiyi tamamen kapsamak için
                            
                            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                                fullRowBounds,
                                color1,
                                color2,
                                System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                            {
                                e.Graphics.FillRectangle(brush, cellBounds);
                            }
                            
                            // Alt çizgiyi gradient ile kapatmak için 3 piksel daha çiz
                            var lineBounds = new System.Drawing.Rectangle(
                                firstColumnLeft,
                                e.Bounds.Bottom,
                                columnsWidth,
                                3); // Alt çizgiyi tamamen kapatmak için
                            
                            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                                fullRowBounds,
                                color1,
                                color2,
                                System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                            {
                                e.Graphics.FillRectangle(brush, lineBounds);
                            }
                        }
                        
                        // Her hücre için metni çiz - beyaz renkte
                        e.Appearance.ForeColor = System.Drawing.Color.White;
                        e.Appearance.DrawString(e.Cache, e.DisplayText, e.Bounds);
                        e.Handled = true;
                    }
                };
                
                // Seçili satırın altındaki çizgiyi gizlemek için CustomDrawRowIndicator
                // Seçili satırın altındaki çizgiyi gradient ile kapat
                gridView.CustomDrawRowIndicator += (s, e) =>
                {
                    if (e.RowHandle == gridView.FocusedRowHandle)
                    {
                        // Seçili satırın altındaki çizgiyi gradient ile kapat
                        var viewRect = gridView.ViewRect;
                        int rowBottom = e.Bounds.Bottom;
                        
                        System.Drawing.Color color1 = System.Drawing.Color.FromArgb(0, 120, 215);
                        System.Drawing.Color color2 = System.Drawing.Color.FromArgb(177, 70, 194);
                        
                        var lineBounds = new System.Drawing.Rectangle(
                            viewRect.Left,
                            rowBottom,
                            viewRect.Width,
                            3); // Alt çizgiyi tamamen kapatmak için
                        
                        using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                            lineBounds,
                            color1,
                            color2,
                            System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                        {
                            e.Graphics.FillRectangle(brush, lineBounds);
                        }
                        
                        e.Handled = true;
                    }
                };

                // Tüm sütunları temizle ve sadece istediğimiz sütunları ekle
                gridView.Columns.Clear();
                
                // Sadece istediğimiz sütunları ekle
                var colSiparisNo = gridView.Columns.AddField("SiparişNo");
                colSiparisNo.Caption = "Sipariş No";
                colSiparisNo.VisibleIndex = 0;
                colSiparisNo.Width = 120;
                colSiparisNo.Visible = true;
                colSiparisNo.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                colSiparisNo.AppearanceCell.Options.UseTextOptions = true;
                
                var colMusteri = gridView.Columns.AddField("Müşteri");
                colMusteri.Caption = "Müşteri";
                colMusteri.VisibleIndex = 1;
                colMusteri.Width = 180;
                colMusteri.Visible = true;
                colMusteri.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                colMusteri.AppearanceCell.Options.UseTextOptions = true;
                
                var colTarih = gridView.Columns.AddField("Tarih");
                colTarih.Caption = "Tarih";
                colTarih.VisibleIndex = 2;
                colTarih.Width = 150;
                colTarih.Visible = true;
                colTarih.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                colTarih.AppearanceCell.Options.UseTextOptions = true;
                
                var colTutar = gridView.Columns.AddField("Tutar");
                colTutar.Caption = "Tutar (TL)";
                colTutar.VisibleIndex = 3;
                colTutar.Width = 120;
                colTutar.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                colTutar.DisplayFormat.FormatString = "N2";
                colTutar.AppearanceCell.ForeColor = System.Drawing.Color.FromArgb(33, 150, 243);
                colTutar.AppearanceCell.Options.UseForeColor = true;
                colTutar.Visible = true;
                colTutar.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                colTutar.AppearanceCell.Options.UseTextOptions = true;
                
                var colUrunSayisi = gridView.Columns.AddField("ÜrünSayısı");
                colUrunSayisi.Caption = "Ürün Sayısı";
                colUrunSayisi.VisibleIndex = 4;
                colUrunSayisi.Width = 100;
                colUrunSayisi.Visible = true;
                colUrunSayisi.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                colUrunSayisi.AppearanceCell.Options.UseTextOptions = true;
                
                var colDurum = gridView.Columns.AddField("Durum");
                colDurum.Caption = "Durum";
                colDurum.VisibleIndex = 5;
                colDurum.Width = 100;
                colDurum.Visible = true;
                colDurum.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                colDurum.AppearanceCell.Options.UseTextOptions = true;
                
                // Sütun başlıkları için görünüm
                foreach (DevExpress.XtraGrid.Columns.GridColumn col in gridView.Columns)
                {
                    col.AppearanceHeader.BackColor = System.Drawing.Color.Transparent;
                    col.AppearanceHeader.ForeColor = System.Drawing.Color.White;
                    col.AppearanceHeader.Options.UseBackColor = true;
                    col.AppearanceHeader.Options.UseForeColor = true;
                    col.AppearanceHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
                }

                // Kapat Butonu - Modern Gradient
                var closeButton = new SimpleButton
                {
                    Text = "Kapat",
                    Size = new System.Drawing.Size(140, 45),
                    Location = new System.Drawing.Point(earningsForm.Width - 160, earningsForm.Height - 80),
                    Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right,
                    DialogResult = System.Windows.Forms.DialogResult.OK,
                    ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False,
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
                };
                closeButton.Appearance.BackColor = System.Drawing.Color.Transparent;
                closeButton.Appearance.ForeColor = System.Drawing.Color.White;
                closeButton.Appearance.Options.UseBackColor = true;
                closeButton.Appearance.Options.UseForeColor = true;
                closeButton.AppearanceHovered.ForeColor = System.Drawing.Color.White;
                closeButton.AppearanceHovered.Options.UseForeColor = true;
                closeButton.AppearancePressed.ForeColor = System.Drawing.Color.White;
                closeButton.AppearancePressed.Options.UseForeColor = true;
                // Gradient Paint event
                closeButton.Paint += (s, e) =>
                {
                    var button = s as SimpleButton;
                    if (button == null) return;

                    // Mavi'den mora gradient
                    System.Drawing.Color color1 = System.Drawing.Color.FromArgb(0, 120, 215);
                    System.Drawing.Color color2 = System.Drawing.Color.FromArgb(177, 70, 194);

                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        button.ClientRectangle,
                        color1,
                        color2,
                        System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                    {
                        int radius = 8;
                        using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                        {
                            path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                            path.AddArc(button.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                            path.AddArc(button.Width - radius * 2, button.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                            path.AddArc(0, button.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                            path.CloseAllFigures();

                            e.Graphics.FillPath(brush, path);
                        }
                    }

                    // Metni çiz
                    using (var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                    {
                        var stringFormat = new System.Drawing.StringFormat
                        {
                            Alignment = System.Drawing.StringAlignment.Center,
                            LineAlignment = System.Drawing.StringAlignment.Center
                        };
                        e.Graphics.DrawString(button.Text, button.Font, textBrush, button.ClientRectangle, stringFormat);
                    }
                };
                earningsForm.Controls.Add(closeButton);
                earningsForm.AcceptButton = closeButton;

                earningsForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    $"Kazanç detayları gösterilirken hata oluştu:\n{ex.Message}",
                    "Hata",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                
                System.Diagnostics.Debug.WriteLine($"[MainForm] Kazanç detayları gösterilirken hata: {ex.Message}");
            }
        }

        private System.Windows.Forms.Panel CreateSummaryCard(string title, string value, System.Drawing.Color color, int x, int y, int width, int height)
        {
            var card = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, height),
                BackColor = System.Drawing.Color.Transparent,
                Padding = new System.Windows.Forms.Padding(5)
            };

            // Gradient arka plan ve yuvarlatılmış köşeler
            card.Paint += (s, e) =>
            {
                var panel = s as System.Windows.Forms.Panel;
                if (panel == null) return;
                
                // Gradient renkler (ana renkten daha açık/koyu tonlara)
                System.Drawing.Color color1, color2;
                if (_currentTheme == ThemeMode.Dark)
                {
                    // Koyu temada daha koyu tonlar
                    color1 = System.Drawing.Color.FromArgb(
                        Math.Max(0, color.R - 30),
                        Math.Max(0, color.G - 30),
                        Math.Max(0, color.B - 30));
                    color2 = System.Drawing.Color.FromArgb(
                        Math.Max(0, color.R - 60),
                        Math.Max(0, color.G - 60),
                        Math.Max(0, color.B - 60));
                }
                else
                {
                    // Açık temada daha açık tonlar
                    color1 = System.Drawing.Color.FromArgb(
                        Math.Min(255, color.R + 40),
                        Math.Min(255, color.G + 40),
                        Math.Min(255, color.B + 40));
                    color2 = System.Drawing.Color.FromArgb(
                        Math.Min(255, color.R + 20),
                        Math.Min(255, color.G + 20),
                        Math.Min(255, color.B + 20));
                }
                
                int radius = 12;
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                    path.AddArc(panel.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                    path.AddArc(panel.Width - radius * 2, panel.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(0, panel.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseAllFigures();
                    
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        panel.ClientRectangle,
                        color1,
                        color2,
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    
                    // Gölge efekti
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(30, 0, 0, 0), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            // Başlık
            var lblTitle = new LabelControl
            {
                Text = title,
                Location = new System.Drawing.Point(10, 15),
                Size = new System.Drawing.Size(width - 20, 20),
                Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold),
                ForeColor = _currentTheme == ThemeMode.Dark ? 
                    System.Drawing.Color.FromArgb(160, 160, 160) : 
                    System.Drawing.Color.FromArgb(40, 40, 40)
            };
            lblTitle.Appearance.BackColor = System.Drawing.Color.Transparent;
            lblTitle.Appearance.Options.UseBackColor = true;
            card.Controls.Add(lblTitle);

            // Değer
            // Açık temada renkleri daha koyu yap
            System.Drawing.Color valueColor;
            if (_currentTheme == ThemeMode.Dark)
            {
                valueColor = System.Drawing.Color.White;
            }
            else
            {
                // Renkleri daha koyu yap (RGB değerlerini %30 azalt)
                valueColor = System.Drawing.Color.FromArgb(
                    Math.Max(0, color.R - (int)(color.R * 0.3)),
                    Math.Max(0, color.G - (int)(color.G * 0.3)),
                    Math.Max(0, color.B - (int)(color.B * 0.3)));
            }
            
            var lblValue = new LabelControl
            {
                Text = value,
                Location = new System.Drawing.Point(10, 40),
                Size = new System.Drawing.Size(width - 20, 60),
                Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold),
                ForeColor = valueColor
            };
            lblValue.Appearance.BackColor = System.Drawing.Color.Transparent;
            lblValue.Appearance.Options.UseBackColor = true;
            lblValue.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            card.Controls.Add(lblValue);

            return card;
        }

        private void ApplyTheme()
        {
            // Vektör tabanlı skin ayarını koru (WXI veya The Bezier)
            try
            {
                // WXI Skin - Windows 11 stili, modern ve yuvarlatılmış köşeler
                UserLookAndFeel.Default.SetSkinStyle("WXI");
                // Alternatif: The Bezier skin'i için aşağıdaki satırı kullanabilirsiniz:
                // UserLookAndFeel.Default.SetSkinStyle("The Bezier");
            }
            catch (Exception skinEx)
            {
                System.Diagnostics.Debug.WriteLine($"[MainForm] Skin ayarı hatası: {skinEx.Message}");
            }

            if (_currentTheme == ThemeMode.Dark)
            {
                ApplyDarkTheme();
            }
            else
            {
                ApplyLightTheme();
            }
            // Grid verilerini yenile (tema renklerinin uygulanması için)
            RefreshData();
        }

        private void ApplyDarkTheme()
        {
            // Vektör tabanlı skin ayarını koru
            try
            {
                UserLookAndFeel.Default.SetSkinStyle("WXI");
            }
            catch { }

            // Form arka planı
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);

            // Başlık paneli (Paint event'i tema kontrolü yapıyor, sadece refresh)
            if (titlePanel != null)
            {
                titlePanel.Invalidate();
            }
            
            // Content panel (Paint event'i tema kontrolü yapıyor, sadece refresh)
            if (contentPanel != null)
            {
                contentPanel.Invalidate();
            }

            // Ayarlar butonu (koyu tema)
            if (btnSettings != null)
            {
                btnSettings.Appearance.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
                btnSettings.Appearance.ForeColor = System.Drawing.Color.White;
                btnSettings.Appearance.BorderColor = System.Drawing.Color.FromArgb(80, 80, 80);
                btnSettings.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(80, 80, 80);
                btnSettings.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(100, 100, 100);
            }

            // Ayarlar paneli (koyu tema)
            if (settingsPanel != null)
            {
                settingsPanel.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
                foreach (System.Windows.Forms.Control control in settingsPanel.Controls)
                {
                    if (control is LabelControl lbl)
                    {
                        lbl.ForeColor = System.Drawing.Color.White;
                    }
                }
            }

            // Tema butonu
            if (btnToggleTheme != null)
            {
                btnToggleTheme.Text = "☀️ Açık Tema";
                btnToggleTheme.Appearance.BackColor = System.Drawing.Color.FromArgb(66, 66, 66);
                btnToggleTheme.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(80, 80, 80);
            }

            // Yeni yazıcı ekle butonu (koyu tema)
            if (btnAddPrinter != null)
            {
                btnAddPrinter.Appearance.BackColor = System.Drawing.Color.FromArgb(0, 100, 180);
                btnAddPrinter.Appearance.BorderColor = System.Drawing.Color.FromArgb(0, 80, 160);
                btnAddPrinter.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(0, 120, 200);
                btnAddPrinter.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(0, 100, 180);
                btnAddPrinter.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(0, 80, 160);
            }
            
            // Yeni sipariş simüle et butonu (koyu tema)
            if (btnSimulateOrder != null)
            {
                btnSimulateOrder.Appearance.BackColor = System.Drawing.Color.FromArgb(12, 100, 12);
                btnSimulateOrder.Appearance.BorderColor = System.Drawing.Color.FromArgb(10, 80, 10);
                btnSimulateOrder.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(16, 120, 16);
                btnSimulateOrder.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(12, 100, 12);
                btnSimulateOrder.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(10, 80, 10);
            }
            
            // Modelleri göster butonu (koyu tema)
            if (btnShowModels != null)
            {
                btnShowModels.Appearance.BackColor = System.Drawing.Color.FromArgb(150, 50, 170);
                btnShowModels.Appearance.BorderColor = System.Drawing.Color.FromArgb(130, 40, 150);
                btnShowModels.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(170, 70, 190);
                btnShowModels.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(150, 50, 170);
                btnShowModels.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(130, 40, 150);
            }
            
            // Kazanç Detayları butonu (koyu tema)
            if (btnShowEarnings != null)
            {
                btnShowEarnings.Appearance.BackColor = System.Drawing.Color.FromArgb(200, 150, 0);
                btnShowEarnings.Appearance.BorderColor = System.Drawing.Color.FromArgb(180, 130, 0);
                btnShowEarnings.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(220, 170, 0);
                btnShowEarnings.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(200, 150, 0);
                btnShowEarnings.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(180, 130, 0);
            }

            // Tamamlanan siparişleri sil butonu (koyu tema)
            if (btnDeleteCompletedOrders != null)
            {
                btnDeleteCompletedOrders.Appearance.BackColor = System.Drawing.Color.FromArgb(200, 50, 50);
                btnDeleteCompletedOrders.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(220, 60, 60);
                btnDeleteCompletedOrders.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(180, 40, 40);
            }

            // Tamamlanan işleri sil butonu (koyu tema)
            if (btnDeleteCompletedJobs != null)
            {
                btnDeleteCompletedJobs.Appearance.BackColor = System.Drawing.Color.FromArgb(200, 50, 50);
                btnDeleteCompletedJobs.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(220, 60, 60);
                btnDeleteCompletedJobs.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(180, 40, 40);
            }

            // Header panelleri (Paint event'leri tema kontrolü yapıyor, sadece refresh)
            if (printersHeaderPanel != null)
                printersHeaderPanel.Invalidate();
            if (ordersHeaderPanel != null)
                ordersHeaderPanel.Invalidate();
            if (jobsHeaderPanel != null)
                jobsHeaderPanel.Invalidate();

            // İstatistikler paneli (Paint event'i tema kontrolü yapıyor, sadece refresh)
            if (statsPanel != null)
            {
                statsPanel.Invalidate();
                // Separator line'ı da refresh et
                foreach (System.Windows.Forms.Control ctrl in statsPanel.Controls)
                {
                    if (ctrl is System.Windows.Forms.Panel && ctrl.Name == "")
                    {
                        ctrl.Invalidate();
                    }
                }
            }

            // Yazıcı icon paneli arka planı
            if (printersIconPanel != null)
            {
                printersIconPanel.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            }

            
            // Grid'leri görünür yap
            if (gridControlPrinters != null) gridControlPrinters.Visible = true;
            if (gridControlOrders != null) gridControlOrders.Visible = true;
            if (gridControlJobs != null) gridControlJobs.Visible = true;

            // Grid'ler
            ApplyDarkThemeToGrid(gridViewPrinters, System.Drawing.Color.FromArgb(35, 35, 35), System.Drawing.Color.FromArgb(45, 45, 45));
            ApplyDarkThemeToGrid(gridViewOrders, System.Drawing.Color.FromArgb(35, 35, 35), System.Drawing.Color.FromArgb(45, 45, 45));
            ApplyDarkThemeToGrid(gridViewJobs, System.Drawing.Color.FromArgb(35, 35, 35), System.Drawing.Color.FromArgb(45, 45, 45));

            // Filtre panellerini güncelle
            UpdateFilterPanelsForDarkTheme();

            // Yazıcı iconlarını güncelle
            UpdatePrinterIcons();

            // Grid'leri yenile
            if (gridControlPrinters != null) gridControlPrinters.Refresh();
            if (gridControlOrders != null) gridControlOrders.Refresh();
            if (gridControlJobs != null) gridControlJobs.Refresh();

            // Label'lar
            if (lblStats != null)
                lblStats.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);

            // İstatistik label'ları
            UpdateStatisticsLabelsForDarkTheme();
        }

        private void ApplyLightTheme()
        {
            // Vektör tabanlı skin ayarını koru
            try
            {
                UserLookAndFeel.Default.SetSkinStyle("WXI");
            }
            catch { }

            // Form arka planı
            this.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);

            // Başlık paneli (Paint event'i tema kontrolü yapıyor, sadece refresh)
            if (titlePanel != null)
            {
                titlePanel.Invalidate();
            }
            
            // Content panel (Paint event'i tema kontrolü yapıyor, sadece refresh)
            if (contentPanel != null)
            {
                contentPanel.Invalidate();
            }

            // Ayarlar butonu (açık tema)
            if (btnSettings != null)
            {
                btnSettings.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 255, 255);
                btnSettings.Appearance.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
                btnSettings.Appearance.BorderColor = System.Drawing.Color.FromArgb(200, 200, 200);
                btnSettings.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
                btnSettings.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(0, 120, 215);
            }

            // Ayarlar paneli (açık tema)
            if (settingsPanel != null)
            {
                settingsPanel.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
                foreach (System.Windows.Forms.Control control in settingsPanel.Controls)
                {
                    if (control is LabelControl lbl)
                    {
                        lbl.ForeColor = System.Drawing.Color.FromArgb(33, 33, 33);
                    }
                }
            }

            // Tema butonu
            if (btnToggleTheme != null)
            {
                btnToggleTheme.Text = "🌙 Koyu Tema";
                btnToggleTheme.Appearance.BackColor = System.Drawing.Color.FromArgb(33, 33, 33);
                btnToggleTheme.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(66, 66, 66);
            }

            // Yeni yazıcı ekle butonu (açık tema)
            if (btnAddPrinter != null)
            {
                btnAddPrinter.Appearance.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
                btnAddPrinter.Appearance.BorderColor = System.Drawing.Color.FromArgb(0, 100, 180);
                btnAddPrinter.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(0, 100, 180);
                btnAddPrinter.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(0, 80, 160);
                btnAddPrinter.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(0, 80, 160);
            }
            
            // Yeni sipariş simüle et butonu (açık tema)
            if (btnSimulateOrder != null)
            {
                btnSimulateOrder.Appearance.BackColor = System.Drawing.Color.FromArgb(16, 124, 16);
                btnSimulateOrder.Appearance.BorderColor = System.Drawing.Color.FromArgb(12, 100, 12);
                btnSimulateOrder.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(20, 140, 20);
                btnSimulateOrder.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(16, 120, 16);
                btnSimulateOrder.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(12, 100, 12);
            }
            
            // Modelleri göster butonu (açık tema)
            if (btnShowModels != null)
            {
                btnShowModels.Appearance.BackColor = System.Drawing.Color.FromArgb(177, 70, 194);
                btnShowModels.Appearance.BorderColor = System.Drawing.Color.FromArgb(150, 50, 170);
                btnShowModels.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(190, 90, 210);
                btnShowModels.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(170, 70, 190);
                btnShowModels.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(150, 50, 170);
            }

            // Tamamlanan siparişleri sil butonu (açık tema)
            if (btnDeleteCompletedOrders != null)
            {
                btnDeleteCompletedOrders.Appearance.BackColor = System.Drawing.Color.FromArgb(244, 67, 54);
                btnDeleteCompletedOrders.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(229, 57, 53);
                btnDeleteCompletedOrders.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(198, 40, 40);
            }

            // Tamamlanan işleri sil butonu (açık tema)
            if (btnDeleteCompletedJobs != null)
            {
                btnDeleteCompletedJobs.Appearance.BackColor = System.Drawing.Color.FromArgb(244, 67, 54);
                btnDeleteCompletedJobs.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(229, 57, 53);
                btnDeleteCompletedJobs.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(198, 40, 40);
            }
            
            // Kazanç Detayları butonu (açık tema)
            if (btnShowEarnings != null)
            {
                btnShowEarnings.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 185, 0);
                btnShowEarnings.Appearance.BorderColor = System.Drawing.Color.FromArgb(255, 140, 0);
                btnShowEarnings.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(255, 200, 0);
                btnShowEarnings.AppearanceHovered.BorderColor = System.Drawing.Color.FromArgb(255, 160, 0);
                btnShowEarnings.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(255, 140, 0);
            }

            // Header panelleri (Paint event'leri tema kontrolü yapıyor, sadece refresh)
            if (printersHeaderPanel != null)
                printersHeaderPanel.Invalidate();
            if (ordersHeaderPanel != null)
                ordersHeaderPanel.Invalidate();
            if (jobsHeaderPanel != null)
                jobsHeaderPanel.Invalidate();

            // İstatistikler paneli (Paint event'i tema kontrolü yapıyor, sadece refresh)
            if (statsPanel != null)
            {
                statsPanel.Invalidate();
                // Separator line'ı da refresh et
                foreach (System.Windows.Forms.Control ctrl in statsPanel.Controls)
                {
                    if (ctrl is System.Windows.Forms.Panel && ctrl.Name == "")
                    {
                        ctrl.Invalidate();
                    }
                }
            }

            // Yazıcı icon paneli arka planı
            if (printersIconPanel != null)
            {
                printersIconPanel.BackColor = System.Drawing.Color.White;
            }

            
            // Grid'leri görünür yap
            if (gridControlPrinters != null) gridControlPrinters.Visible = true;
            if (gridControlOrders != null) gridControlOrders.Visible = true;
            if (gridControlJobs != null) gridControlJobs.Visible = true;

            // Grid'ler
            ApplyLightThemeToGrid(gridViewPrinters, System.Drawing.Color.White, System.Drawing.Color.FromArgb(249, 250, 252));
            ApplyLightThemeToGrid(gridViewOrders, System.Drawing.Color.White, System.Drawing.Color.FromArgb(249, 250, 252));
            ApplyLightThemeToGrid(gridViewJobs, System.Drawing.Color.White, System.Drawing.Color.FromArgb(249, 250, 252));

            // Filtre panellerini güncelle
            UpdateFilterPanelsForLightTheme();

            // Yazıcı iconlarını güncelle
            UpdatePrinterIcons();

            // Grid'leri yenile
            if (gridControlPrinters != null) gridControlPrinters.Refresh();
            if (gridControlOrders != null) gridControlOrders.Refresh();
            if (gridControlJobs != null) gridControlJobs.Refresh();

            // Label'lar
            if (lblStats != null)
                lblStats.ForeColor = System.Drawing.Color.FromArgb(63, 81, 181);

            // İstatistik label'ları
            UpdateStatisticsLabelsForLightTheme();
        }

        private void ApplyDarkThemeToGrid(GridView gridView, System.Drawing.Color evenRowColor, System.Drawing.Color oddRowColor)
        {
            if (gridView == null) return;

            // Grid kontrol arka planı (öncelikle)
            if (gridView.GridControl != null)
            {
                gridView.GridControl.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
                // Vektör tabanlı skin kullan (WXI)
                gridView.GridControl.LookAndFeel.UseDefaultLookAndFeel = true;
            }

            // Empty area (boş alan) arka planı
            gridView.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            gridView.Appearance.Empty.Options.UseBackColor = true;

            // Satır renkleri
            gridView.Appearance.Row.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            gridView.Appearance.Row.BackColor = evenRowColor;
            gridView.Appearance.Row.Options.UseForeColor = true;
            gridView.Appearance.Row.Options.UseBackColor = true;

            // Grid görünüm ayarları - Even/Odd satırları aktif et
            gridView.OptionsView.EnableAppearanceEvenRow = true;
            gridView.OptionsView.EnableAppearanceOddRow = true;
            gridView.Appearance.EvenRow.BackColor = evenRowColor;
            gridView.Appearance.EvenRow.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            gridView.Appearance.EvenRow.Options.UseBackColor = true;
            gridView.Appearance.EvenRow.Options.UseForeColor = true;
            gridView.Appearance.OddRow.BackColor = oddRowColor;
            gridView.Appearance.OddRow.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            gridView.Appearance.OddRow.Options.UseBackColor = true;
            gridView.Appearance.OddRow.Options.UseForeColor = true;

            // Başlık paneli (koyu tema için özel renkler)
            if (gridView == gridViewPrinters)
                gridView.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(35, 45, 110);
            else if (gridView == gridViewOrders)
                gridView.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(140, 85, 0);
            else if (gridView == gridViewJobs)
                gridView.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(90, 20, 110);
            
            gridView.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.White;
            gridView.Appearance.HeaderPanel.Options.UseBackColor = true;
            gridView.Appearance.HeaderPanel.Options.UseForeColor = true;

            // Hücre renkleri
            foreach (GridColumn column in gridView.Columns)
            {
                column.AppearanceCell.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                column.AppearanceCell.BackColor = System.Drawing.Color.Transparent;
                column.AppearanceCell.Options.UseForeColor = true;
                column.AppearanceCell.Options.UseBackColor = true;
            }

            // Filtre paneli görünümü (koyu tema) - Daha agresif ayarlama
            try
            {
                var filterPanelAppearance = gridView.Appearance.FilterPanel;
                filterPanelAppearance.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
                filterPanelAppearance.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                filterPanelAppearance.Options.UseBackColor = true;
                filterPanelAppearance.Options.UseForeColor = true;
                filterPanelAppearance.Options.UseTextOptions = true;
            }
            catch { }

            // Grid'in genel görünümü
            gridView.PaintStyleName = "Flat";
        }

        private void ApplyLightThemeToGrid(GridView gridView, System.Drawing.Color evenRowColor, System.Drawing.Color oddRowColor)
        {
            if (gridView == null) return;

            // Grid kontrol arka planı (öncelikle)
            if (gridView.GridControl != null)
            {
                gridView.GridControl.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
                // Vektör tabanlı skin kullan (WXI)
                gridView.GridControl.LookAndFeel.UseDefaultLookAndFeel = true;
            }

            // Empty area (boş alan) arka planı
            gridView.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            gridView.Appearance.Empty.Options.UseBackColor = true;

            // Satır renkleri
            gridView.Appearance.Row.ForeColor = System.Drawing.Color.Black;
            gridView.Appearance.Row.BackColor = evenRowColor;
            gridView.Appearance.Row.Options.UseForeColor = true;
            gridView.Appearance.Row.Options.UseBackColor = true;

            // Grid görünüm ayarları - Even/Odd satırları aktif et
            gridView.OptionsView.EnableAppearanceEvenRow = true;
            gridView.OptionsView.EnableAppearanceOddRow = true;
            gridView.Appearance.EvenRow.BackColor = evenRowColor;
            gridView.Appearance.EvenRow.ForeColor = System.Drawing.Color.Black;
            gridView.Appearance.EvenRow.Options.UseBackColor = true;
            gridView.Appearance.EvenRow.Options.UseForeColor = true;
            gridView.Appearance.OddRow.BackColor = oddRowColor;
            gridView.Appearance.OddRow.ForeColor = System.Drawing.Color.Black;
            gridView.Appearance.OddRow.Options.UseBackColor = true;
            gridView.Appearance.OddRow.Options.UseForeColor = true;

            // Başlık paneli
            if (gridView == gridViewPrinters)
                gridView.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(48, 63, 159);
            else if (gridView == gridViewOrders)
                gridView.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(230, 126, 34);
            else if (gridView == gridViewJobs)
                gridView.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(123, 31, 162);

            gridView.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.White;
            gridView.Appearance.HeaderPanel.Options.UseBackColor = true;
            gridView.Appearance.HeaderPanel.Options.UseForeColor = true;

            // Hücre renkleri
            foreach (GridColumn column in gridView.Columns)
            {
                column.AppearanceCell.ForeColor = System.Drawing.Color.Black;
                column.AppearanceCell.BackColor = System.Drawing.Color.Transparent;
                column.AppearanceCell.Options.UseForeColor = true;
                column.AppearanceCell.Options.UseBackColor = true;
            }

            // Filtre paneli görünümü (açık tema)
            try
            {
                gridView.Appearance.FilterPanel.BackColor = System.Drawing.Color.FromArgb(249, 250, 252);
                gridView.Appearance.FilterPanel.ForeColor = System.Drawing.Color.Black;
                gridView.Appearance.FilterPanel.Options.UseBackColor = true;
                gridView.Appearance.FilterPanel.Options.UseForeColor = true;
            }
            catch { }

            // Grid'in genel görünümü
            gridView.PaintStyleName = "Flat";
        }

        private System.Drawing.Color DarkenColor(System.Drawing.Color color)
        {
            // Renkleri koyulaştır
            int r = Math.Max(0, color.R - 30);
            int g = Math.Max(0, color.G - 30);
            int b = Math.Max(0, color.B - 30);
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        private void UpdateStatisticsLabelsForDarkTheme()
        {
            if (lblTotalPrinters != null)
                lblTotalPrinters.ForeColor = System.Drawing.Color.FromArgb(100, 181, 246);
            if (lblActivePrinters != null)
                lblActivePrinters.ForeColor = System.Drawing.Color.FromArgb(129, 199, 132);
            if (lblTotalOrders != null)
                lblTotalOrders.ForeColor = System.Drawing.Color.FromArgb(255, 183, 77);
            if (lblPendingJobs != null)
                lblPendingJobs.ForeColor = System.Drawing.Color.FromArgb(186, 104, 200);

            // Label'ları güncelle
            foreach (var label in statsPanel?.Controls.OfType<LabelControl>())
            {
                if (label != lblStats && label != lblTotalPrinters && label != lblActivePrinters && 
                    label != lblTotalOrders && label != lblPendingJobs && label.Name != "lblCompletedJobs" &&
                    label.Name != "lblTotalEarnings")
                {
                    label.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
                }
            }

            var completedLabel = statsPanel?.Controls.OfType<LabelControl>()
                .FirstOrDefault(l => l.Name == "lblCompletedJobs");
            if (completedLabel != null)
                completedLabel.ForeColor = System.Drawing.Color.FromArgb(129, 199, 132);

            // Toplam kazanç label'ı rengini güncelle
            if (lblTotalEarnings != null)
                lblTotalEarnings.ForeColor = System.Drawing.Color.FromArgb(255, 193, 7);
        }

        private void UpdateStatisticsLabelsForLightTheme()
        {
            if (lblTotalPrinters != null)
                lblTotalPrinters.ForeColor = System.Drawing.Color.FromArgb(63, 81, 181);
            if (lblActivePrinters != null)
                lblActivePrinters.ForeColor = System.Drawing.Color.FromArgb(76, 175, 80);
            if (lblTotalOrders != null)
                lblTotalOrders.ForeColor = System.Drawing.Color.FromArgb(255, 152, 0);
            if (lblPendingJobs != null)
                lblPendingJobs.ForeColor = System.Drawing.Color.FromArgb(156, 39, 176);

            // Label'ları güncelle
            foreach (var label in statsPanel?.Controls.OfType<LabelControl>())
            {
                if (label != lblStats && label != lblTotalPrinters && label != lblActivePrinters && 
                    label != lblTotalOrders && label != lblPendingJobs && label.Name != "lblCompletedJobs" &&
                    label.Name != "lblTotalEarnings")
                {
                    label.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
                }
            }

            var completedLabel = statsPanel?.Controls.OfType<LabelControl>()
                .FirstOrDefault(l => l.Name == "lblCompletedJobs");
            if (completedLabel != null)
                completedLabel.ForeColor = System.Drawing.Color.FromArgb(76, 175, 80);

            // Toplam kazanç label'ı rengini güncelle
            if (lblTotalEarnings != null)
                lblTotalEarnings.ForeColor = System.Drawing.Color.FromArgb(255, 193, 7);
        }

        private void GridViewPrinters_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (_currentTheme == ThemeMode.Dark)
            {
                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                e.Appearance.BackColor = e.RowHandle % 2 == 0 ? System.Drawing.Color.FromArgb(35, 35, 35) : System.Drawing.Color.FromArgb(45, 45, 45);
            }
            else
            {
                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(33, 33, 33);
                e.Appearance.BackColor = e.RowHandle % 2 == 0 ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(249, 250, 252);
            }
            e.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            
            // Yazıcı durumuna göre renk ve sembol ekle
            if (e.Column != null && e.Column.FieldName == "Status")
            {
                var printer = gridViewPrinters.GetRow(e.RowHandle) as Printer;
                if (printer != null)
                {
                    switch (printer.Status)
                    {
                        case PrinterStatus.Printing:
                            // Çalışır durumda - Yeşil
                            if (_currentTheme == ThemeMode.Dark)
                            {
                                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(129, 199, 132);
                                e.Appearance.BackColor = System.Drawing.Color.FromArgb(30, 60, 30);
                            }
                            else
                            {
                                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(76, 175, 80);
                                e.Appearance.BackColor = System.Drawing.Color.FromArgb(232, 245, 233);
                            }
                            e.Appearance.Options.UseBackColor = true;
                            break;
                        case PrinterStatus.Error:
                            // Hata durumunda - Kırmızı
                            if (_currentTheme == ThemeMode.Dark)
                            {
                                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(255, 138, 128);
                                e.Appearance.BackColor = System.Drawing.Color.FromArgb(60, 30, 30);
                            }
                            else
                            {
                                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(244, 67, 54);
                                e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 235, 238);
                            }
                            e.Appearance.Options.UseBackColor = true;
                            break;
                        case PrinterStatus.Idle:
                            // Boşta - Gri
                            if (_currentTheme == ThemeMode.Dark)
                            {
                                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
                            }
                            else
                            {
                                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(158, 158, 158);
                            }
                            break;
                        case PrinterStatus.Paused:
                            // Duraklatıldı - Sarı/Turuncu
                            if (_currentTheme == ThemeMode.Dark)
                            {
                                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(255, 183, 77);
                                e.Appearance.BackColor = System.Drawing.Color.FromArgb(60, 50, 30);
                            }
                            else
                            {
                                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(255, 152, 0);
                                e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 243, 224);
                            }
                            e.Appearance.Options.UseBackColor = true;
                            break;
                        case PrinterStatus.Maintenance:
                            // Bakımda - Turuncu
                            if (_currentTheme == ThemeMode.Dark)
                            {
                                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(255, 183, 77);
                                e.Appearance.BackColor = System.Drawing.Color.FromArgb(60, 50, 30);
                            }
                            else
                            {
                                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(255, 152, 0);
                                e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 243, 224);
                            }
                            e.Appearance.Options.UseBackColor = true;
                            break;
                    }
                }
            }
            
            // Filament durumuna göre renk değiştir
            if (e.Column != null && e.Column.FieldName == "FilamentRemaining")
            {
                var printer = gridViewPrinters.GetRow(e.RowHandle) as Printer;
                if (printer != null)
                {
                    if (printer.FilamentRemaining < 20)
                    {
                        if (_currentTheme == ThemeMode.Dark)
                        {
                            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(255, 138, 128); // Açık kırmızı
                            e.Appearance.BackColor = System.Drawing.Color.FromArgb(60, 30, 30);
                        }
                        else
                        {
                            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(244, 67, 54);
                            e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 235, 238);
                        }
                    }
                    else if (printer.FilamentRemaining < 40)
                    {
                        if (_currentTheme == ThemeMode.Dark)
                        {
                            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(255, 183, 77); // Açık turuncu
                            e.Appearance.BackColor = System.Drawing.Color.FromArgb(60, 50, 30);
                        }
                        else
                        {
                            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(255, 152, 0);
                            e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 243, 224);
                        }
                    }
                    else
                    {
                        if (_currentTheme == ThemeMode.Dark)
                        {
                            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(129, 199, 132); // Açık yeşil
                        }
                        else
                        {
                            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(76, 175, 80);
                        }
                    }
                }
            }
        }

        private void UpdatePrinterIcons()
        {
            if (printersIconPanel == null) return;

            var printers = _printerService.GetAllPrinters();
            
            // Performans için layout'u askıya al
            printersIconPanel.SuspendLayout();

            // Mevcut yazıcı ID'lerini topla
            var existingPrinterIds = new System.Collections.Generic.HashSet<int>(printerIconPanels.Keys);
            var currentPrinterIds = new System.Collections.Generic.HashSet<int>(printers.Select(p => p.Id));

            // Artık olmayan yazıcıları kaldır
            var printersToRemove = existingPrinterIds.Except(currentPrinterIds).ToList();
            foreach (var printerId in printersToRemove)
            {
                if (printerIconPanels.ContainsKey(printerId))
                {
                    var panelToRemove = printerIconPanels[printerId];
                    
                    // Event handler'ı kaldır
                    if (printerPanelClickHandlers.ContainsKey(printerId))
                    {
                        panelToRemove.Click -= printerPanelClickHandlers[printerId];
                        printerPanelClickHandlers.Remove(printerId);
                    }
                    
                    printersIconPanel.Controls.Remove(panelToRemove);
                    panelToRemove.Dispose();
                    printerIconPanels.Remove(printerId);
                }
            }

            // Her yazıcı için icon panelini güncelle veya oluştur
            foreach (var printer in printers)
            {
                System.Windows.Forms.Panel iconPanel;
                bool isNew = false;

                if (printerIconPanels.ContainsKey(printer.Id))
                {
                    // Mevcut paneli kullan
                    iconPanel = printerIconPanels[printer.Id];
                }
                else
                {
                    // Yeni panel oluştur (daha küçük, yazılar tam gözüksün)
                    isNew = true;
                    iconPanel = new System.Windows.Forms.Panel
                    {
                        Size = new System.Drawing.Size(120, 75),
                        Margin = new System.Windows.Forms.Padding(6, 4, 6, 4),
                        BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                        BackColor = System.Drawing.Color.Transparent, // Gradient için transparent
                        Padding = new System.Windows.Forms.Padding(3),
                        Cursor = System.Windows.Forms.Cursors.Hand
                    };
                    // Gradient arka plan için Paint event'i
                    iconPanel.Paint += (s, e) =>
                    {
                        System.Drawing.Color gradColor1, gradColor2;
                        if (_currentTheme == ThemeMode.Dark)
                        {
                            gradColor1 = System.Drawing.Color.FromArgb(50, 50, 50);
                            gradColor2 = System.Drawing.Color.FromArgb(40, 40, 40);
                        }
                        else
                        {
                            gradColor1 = System.Drawing.Color.FromArgb(255, 255, 255);
                            gradColor2 = System.Drawing.Color.FromArgb(248, 248, 248);
                        }
                        using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                            iconPanel.ClientRectangle,
                            gradColor1,
                            gradColor2,
                            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                        {
                            e.Graphics.FillRectangle(brush, iconPanel.ClientRectangle);
                        }
                    };
                    printerIconPanels[printer.Id] = iconPanel;
                }
                
                // Event handler'ı düzgün yönet - önce eski handler'ı kaldır, sonra yenisini ekle
                if (printerPanelClickHandlers.ContainsKey(printer.Id))
                {
                    iconPanel.Click -= printerPanelClickHandlers[printer.Id];
                }
                
                // Yeni handler oluştur ve sakla
                var currentPrinter = printer; // Closure için local copy
                System.EventHandler clickHandler = (s, e) => 
                {
                    if (!_isDetailsFormOpen)
                    {
                        ShowPrinterDetails(currentPrinter);
                    }
                };
                printerPanelClickHandlers[printer.Id] = clickHandler;
                iconPanel.Click += clickHandler;
                iconPanel.Cursor = System.Windows.Forms.Cursors.Hand;

                // Duruma göre renk belirle
                System.Drawing.Color iconColor;
                switch (printer.Status)
                {
                    case PrinterStatus.Printing:
                        iconColor = System.Drawing.Color.FromArgb(76, 175, 80); // Yeşil
                        break;
                    case PrinterStatus.Error:
                        iconColor = System.Drawing.Color.FromArgb(244, 67, 54); // Kırmızı
                        break;
                    case PrinterStatus.Idle:
                    default:
                        iconColor = System.Drawing.Color.FromArgb(158, 158, 158); // Gri
                        break;
                }

                // Durum bilgisi metni
                string statusText = "";
                string iconText = "";
                switch (printer.Status)
                {
                    case PrinterStatus.Printing:
                        statusText = $"Yazdırıyor %{printer.Progress:F0}";
                        iconText = "🖨️"; // Aktif 3D yazıcı ikonu
                        break;
                    case PrinterStatus.Error:
                        statusText = "Hata";
                        iconText = "⚠️"; // Hata ikonu
                        break;
                    case PrinterStatus.Idle:
                        statusText = "Boşta";
                        iconText = "🖨️"; // Pasif 3D yazıcı ikonu
                        break;
                    case PrinterStatus.Paused:
                        statusText = "Duraklatıldı";
                        iconText = "⏸️"; // Duraklatma ikonu
                        break;
                    case PrinterStatus.Maintenance:
                        statusText = "Bakımda";
                        iconText = "🔧"; // Bakım ikonu
                        break;
                    default:
                        iconText = "🖨️";
                        break;
                }

                if (isNew)
                {
                    // Yeni panel için kontrolleri oluştur - resim ikonu kullan
                    // İkon seçimi: Printing durumunda green.png, koyu temada white.png, diğer durumlarda print.png
                    System.Windows.Forms.PictureBox iconPictureBox = null;
                    try
                    {
                        string imageFileName;
                        if (printer.Status == PrinterStatus.Printing)
                        {
                            // Aktif yazıcılar için her iki temada da green.png
                            imageFileName = "green.png";
                        }
                        else if (_currentTheme == ThemeMode.Dark)
                        {
                            // Koyu temada white.png
                            imageFileName = "white.png";
                        }
                        else
                        {
                            // Açık temada print.png
                            imageFileName = "print.png";
                        }
                        
                        string imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", imageFileName);
                        if (!System.IO.File.Exists(imagePath))
                        {
                            // Alternatif yol dene
                            imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image", imageFileName);
                        }
                        if (System.IO.File.Exists(imagePath))
                        {
                            iconPictureBox = new System.Windows.Forms.PictureBox
                            {
                                Location = new System.Drawing.Point(35, 2),
                                Size = new System.Drawing.Size(50, 30),
                                SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                                Name = "iconPictureBox"
                            };
                            iconPictureBox.Image = System.Drawing.Image.FromFile(imagePath);
                            iconPanel.Controls.Add(iconPictureBox);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Resim yüklenirken hata: {ex.Message}");
                    }
                    
                    // Eğer resim yüklenemediyse, eski emoji ikonunu kullan
                    if (iconPictureBox == null)
                    {
                        var iconLabel = new LabelControl
                        {
                            Text = iconText,
                            Location = new System.Drawing.Point(45, 2),
                            Size = new System.Drawing.Size(30, 22),
                            Font = new System.Drawing.Font("Segoe UI", 14F),
                            ForeColor = iconColor,
                            Name = "iconLabel"
                        };
                        iconLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                        iconLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                        iconPanel.Controls.Add(iconLabel);
                    }

                    var nameLabel = new LabelControl
                    {
                        Text = printer.Name,
                        Location = new System.Drawing.Point(2, 28),
                        Size = new System.Drawing.Size(116, 16),
                        Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                        ForeColor = _currentTheme == ThemeMode.Dark ? 
                            System.Drawing.Color.White : 
                            System.Drawing.Color.FromArgb(30, 30, 30), // Gradient üzerinde görünmesi için koyu gri
                        Name = "nameLabel"
                    };
                    nameLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    nameLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
                    iconPanel.Controls.Add(nameLabel);

                    var statusLabel = new LabelControl
                    {
                        Text = statusText,
                        Location = new System.Drawing.Point(2, 46),
                        Size = new System.Drawing.Size(116, 25),
                        Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular),
                        ForeColor = _currentTheme == ThemeMode.Dark ? 
                            System.Drawing.Color.White : 
                            System.Drawing.Color.FromArgb(50, 50, 50), // Gradient üzerinde görünmesi için
                        Name = "statusLabel"
                    };
                    statusLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    statusLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
                    statusLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
                    iconPanel.Controls.Add(statusLabel);

                    printersIconPanel.Controls.Add(iconPanel);
                }
                else
                {
                    // Mevcut panelin boyutunu güncelle (daha küçük boyutlar)
                    if (iconPanel.Height > 75 || iconPanel.Width > 120)
                    {
                        iconPanel.Size = new System.Drawing.Size(120, 75);
                        iconPanel.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
                        iconPanel.Padding = new System.Windows.Forms.Padding(3);
                        
                        // Mevcut kontrollerin konumlarını güncelle - resim ikonu kullan
                        var iconPictureBox = iconPanel.Controls.OfType<System.Windows.Forms.PictureBox>().FirstOrDefault(c => c.Name == "iconPictureBox");
                        var iconLabel = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "iconLabel");
                        
                        // İkon seçimi: Printing durumunda green.png, koyu temada white.png, diğer durumlarda print.png
                        string imageFileName;
                        if (printer.Status == PrinterStatus.Printing)
                        {
                            imageFileName = "green.png";
                        }
                        else if (_currentTheme == ThemeMode.Dark)
                        {
                            imageFileName = "white.png";
                        }
                        else
                        {
                            imageFileName = "print.png";
                        }
                        
                        // Eğer PictureBox yoksa ve Label varsa, PictureBox'a dönüştür
                        if (iconPictureBox == null && iconLabel != null)
                        {
                            iconPanel.Controls.Remove(iconLabel);
                            iconLabel.Dispose();
                            
                            try
                            {
                                string imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", imageFileName);
                                if (!System.IO.File.Exists(imagePath))
                                {
                                    imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image", imageFileName);
                                }
                                if (System.IO.File.Exists(imagePath))
                                {
                                    iconPictureBox = new System.Windows.Forms.PictureBox
                                    {
                                        Location = new System.Drawing.Point(40, 2),
                                        Size = new System.Drawing.Size(40, 22),
                                        SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                                        Name = "iconPictureBox"
                                    };
                                    iconPictureBox.Image = System.Drawing.Image.FromFile(imagePath);
                                    iconPanel.Controls.Add(iconPictureBox);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Resim yüklenirken hata: {ex.Message}");
                            }
                        }
                        // Eğer PictureBox varsa, resmi güncelle
                        else if (iconPictureBox != null)
                        {
                            try
                            {
                                string imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", imageFileName);
                                if (!System.IO.File.Exists(imagePath))
                                {
                                    imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image", imageFileName);
                                }
                                if (System.IO.File.Exists(imagePath))
                                {
                                    // Eski resmi dispose et
                                    if (iconPictureBox.Image != null)
                                    {
                                        iconPictureBox.Image.Dispose();
                                    }
                                    iconPictureBox.Image = System.Drawing.Image.FromFile(imagePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Resim güncellenirken hata: {ex.Message}");
                            }
                            iconPictureBox.Location = new System.Drawing.Point(35, 2);
                            iconPictureBox.Size = new System.Drawing.Size(50, 30);
                        }
                        // Eğer hala Label varsa (resim yüklenemediyse), güncelle
                        else if (iconLabel != null)
                        {
                            iconLabel.Location = new System.Drawing.Point(45, 2);
                            iconLabel.Size = new System.Drawing.Size(30, 22);
                            iconLabel.Font = new System.Drawing.Font("Segoe UI", 14F);
                            iconLabel.ForeColor = iconColor;
                            iconLabel.Text = iconText;
                        }

                        var nameLabel = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "nameLabel");
                        if (nameLabel != null)
                        {
                            nameLabel.Location = new System.Drawing.Point(2, 28);
                            nameLabel.Size = new System.Drawing.Size(116, 16);
                            nameLabel.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
                            nameLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                            nameLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
                            nameLabel.Text = printer.Name;
                            nameLabel.ForeColor = _currentTheme == ThemeMode.Dark ? 
                                System.Drawing.Color.FromArgb(240, 240, 240) : 
                                System.Drawing.Color.Black;
                        }

                        var statusLabel = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "statusLabel");
                        if (statusLabel != null)
                        {
                            statusLabel.Location = new System.Drawing.Point(2, 46);
                            statusLabel.Size = new System.Drawing.Size(116, 25);
                            statusLabel.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular);
                            statusLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                            statusLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
                            statusLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
                            statusLabel.Text = statusText;
                            statusLabel.ForeColor = _currentTheme == ThemeMode.Dark ? 
                                System.Drawing.Color.White : 
                                System.Drawing.Color.FromArgb(50, 50, 50); // Gradient üzerinde görünmesi için
                        }
                    }
                    else
                    {
                        // Mevcut kontrolleri güncelle - resim ikonu kullan
                        var iconPictureBoxUpdate = iconPanel.Controls.OfType<System.Windows.Forms.PictureBox>().FirstOrDefault(c => c.Name == "iconPictureBox");
                        var iconLabelUpdate = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "iconLabel");
                        
                        // İkon seçimi: Printing durumunda green.png, koyu temada white.png, diğer durumlarda print.png
                        string imageFileName;
                        if (printer.Status == PrinterStatus.Printing)
                        {
                            imageFileName = "green.png";
                        }
                        else if (_currentTheme == ThemeMode.Dark)
                        {
                            imageFileName = "white.png";
                        }
                        else
                        {
                            imageFileName = "print.png";
                        }
                        
                        // Eğer PictureBox yoksa ve Label varsa, PictureBox'a dönüştür
                        if (iconPictureBoxUpdate == null && iconLabelUpdate != null)
                        {
                            iconPanel.Controls.Remove(iconLabelUpdate);
                            iconLabelUpdate.Dispose();
                            
                            try
                            {
                                string imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", imageFileName);
                                if (!System.IO.File.Exists(imagePath))
                                {
                                    imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image", imageFileName);
                                }
                                if (System.IO.File.Exists(imagePath))
                                {
                                    iconPictureBoxUpdate = new System.Windows.Forms.PictureBox
                                    {
                                        Location = new System.Drawing.Point(40, 2),
                                        Size = new System.Drawing.Size(40, 22),
                                        SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                                        Name = "iconPictureBox"
                                    };
                                    iconPictureBoxUpdate.Image = System.Drawing.Image.FromFile(imagePath);
                                    iconPanel.Controls.Add(iconPictureBoxUpdate);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Resim yüklenirken hata: {ex.Message}");
                            }
                        }
                        // Eğer PictureBox varsa, resmi güncelle
                        else if (iconPictureBoxUpdate != null)
                        {
                            try
                            {
                                string imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "image", imageFileName);
                                if (!System.IO.File.Exists(imagePath))
                                {
                                    imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image", imageFileName);
                                }
                                if (System.IO.File.Exists(imagePath))
                                {
                                    // Eski resmi dispose et
                                    if (iconPictureBoxUpdate.Image != null)
                                    {
                                        iconPictureBoxUpdate.Image.Dispose();
                                    }
                                    iconPictureBoxUpdate.Image = System.Drawing.Image.FromFile(imagePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Resim güncellenirken hata: {ex.Message}");
                            }
                            iconPictureBoxUpdate.Location = new System.Drawing.Point(35, 2);
                            iconPictureBoxUpdate.Size = new System.Drawing.Size(50, 30);
                        }
                        // Eğer hala Label varsa (resim yüklenemediyse), güncelle
                        else if (iconLabelUpdate != null)
                        {
                            iconLabelUpdate.Location = new System.Drawing.Point(45, 2);
                            iconLabelUpdate.Size = new System.Drawing.Size(30, 22);
                            iconLabelUpdate.Font = new System.Drawing.Font("Segoe UI", 14F);
                            iconLabelUpdate.ForeColor = iconColor;
                            iconLabelUpdate.Text = iconText;
                        }

                        var nameLabelUpdate = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "nameLabel");
                        if (nameLabelUpdate != null)
                        {
                            nameLabelUpdate.Location = new System.Drawing.Point(2, 28);
                            nameLabelUpdate.Size = new System.Drawing.Size(116, 16);
                            nameLabelUpdate.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
                            nameLabelUpdate.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                            nameLabelUpdate.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
                            nameLabelUpdate.Text = printer.Name;
                            nameLabelUpdate.ForeColor = _currentTheme == ThemeMode.Dark ? 
                                System.Drawing.Color.White : 
                                System.Drawing.Color.FromArgb(30, 30, 30); // Gradient üzerinde görünmesi için koyu gri
                        }

                        var statusLabelUpdate = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "statusLabel");
                        if (statusLabelUpdate != null)
                        {
                            statusLabelUpdate.Location = new System.Drawing.Point(2, 46);
                            statusLabelUpdate.Size = new System.Drawing.Size(116, 25);
                            statusLabelUpdate.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular);
                            statusLabelUpdate.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                            statusLabelUpdate.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
                            statusLabelUpdate.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
                            statusLabelUpdate.Text = statusText;
                            statusLabelUpdate.ForeColor = _currentTheme == ThemeMode.Dark ? 
                                System.Drawing.Color.White : 
                                System.Drawing.Color.FromArgb(50, 50, 50); // Gradient üzerinde görünmesi için
                        }
                    }

                    // Panel arka plan rengini güncelle (gradient için transparent)
                    iconPanel.BackColor = System.Drawing.Color.Transparent;
                    iconPanel.Invalidate(); // Gradient'i yeniden çiz
                    iconPanel.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
                    iconPanel.Padding = new System.Windows.Forms.Padding(3);
                    iconPanel.Cursor = System.Windows.Forms.Cursors.Hand;
                    // Panel boyutunu küçült
                    if (iconPanel.Height > 75 || iconPanel.Width > 120)
                    {
                        iconPanel.Size = new System.Drawing.Size(120, 75);
                    }
                    
                    // Event handler zaten yukarıda eklenmiş, sadece cursor'ı ayarla
                    iconPanel.Cursor = System.Windows.Forms.Cursors.Hand;
                }
            }

            // Layout'u devam ettir
            printersIconPanel.ResumeLayout(true);
        }

        private void GridViewPrinters_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            // Durum kolonuna sembol ekle
            if (e.Column != null && e.Column.FieldName == "Status")
            {
                var printer = e.Value as PrinterStatus?;
                if (printer.HasValue)
                {
                    var printerStatus = printer.Value;
                    string statusSymbol = "";
                    switch (printerStatus)
                    {
                        case PrinterStatus.Printing:
                            statusSymbol = "🟢 "; // Yeşil daire
                            break;
                        case PrinterStatus.Error:
                            statusSymbol = "🔴 "; // Kırmızı daire
                            break;
                        case PrinterStatus.Idle:
                            statusSymbol = "⚫ "; // Siyah daire (gri görünecek)
                            break;
                        case PrinterStatus.Paused:
                            statusSymbol = "🟡 "; // Sarı daire
                            break;
                        case PrinterStatus.Maintenance:
                            statusSymbol = "🟠 "; // Turuncu daire
                            break;
                    }
                    
                    // Durum metnini al
                    string statusText = "";
                    // Türkçe çeviri
                    switch (printerStatus)
                    {
                        case PrinterStatus.Printing:
                            statusText = "Yazdırıyor";
                            break;
                        case PrinterStatus.Error:
                            statusText = "Hata";
                            break;
                        case PrinterStatus.Idle:
                            statusText = "Boşta";
                            break;
                        case PrinterStatus.Paused:
                            statusText = "Duraklatıldı";
                            break;
                        case PrinterStatus.Maintenance:
                            statusText = "Bakımda";
                            break;
                    }
                    
                    // Sembol ve metni birleştir
                    e.DisplayText = statusSymbol + statusText;
                }
            }
        }

        private void GridViewOrders_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (_currentTheme == ThemeMode.Dark)
            {
                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                e.Appearance.BackColor = e.RowHandle % 2 == 0 ? System.Drawing.Color.FromArgb(35, 35, 35) : System.Drawing.Color.FromArgb(45, 45, 45);
            }
            else
            {
                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(33, 33, 33);
                e.Appearance.BackColor = e.RowHandle % 2 == 0 ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(249, 250, 252);
            }
            e.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        }

        private void GridViewOrders_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column != null && e.Column.FieldName == "DeleteAction")
            {
                if (e.IsGetData)
                {
                    var order = e.Row as Order;
                    // Sadece tamamlanan siparişler için silme butonu göster
                    if (order != null && order.Status == OrderStatus.Completed)
                    {
                        e.Value = "🗑️ Sil";
                    }
                    else
                    {
                        e.Value = string.Empty;
                    }
                }
            }
        }

        private void GridViewOrders_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            var view = sender as GridView;
            if (view == null) return;

            var hitInfo = view.CalcHitInfo(e.Location);
            if (hitInfo.InRowCell && hitInfo.Column != null && hitInfo.Column.FieldName == "DeleteAction")
            {
                var order = view.GetRow(hitInfo.RowHandle) as Order;
                if (order != null)
                {
                    // Sadece tamamlanan siparişler silinebilir
                    if (order.Status != OrderStatus.Completed)
                    {
                        XtraMessageBox.Show(
                            "Sadece tamamlanan siparişler silinebilir.",
                            "Bilgi",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Information);
                        return;
                    }

                    var result = XtraMessageBox.Show(
                        $"Tamamlanan sipariş #{order.OrderNumber} silinecek.\n\nBu işlem geri alınamaz. Devam etmek istiyor musunuz?",
                        "Siparişi Sil",
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                    
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        bool deleted = _orderService.DeleteOrder(order.Id);
                        if (deleted)
                        {
                            RefreshData();
                            XtraMessageBox.Show(
                                "Sipariş başarıyla silindi.",
                                "Başarılı",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Information);
                        }
                        else
                        {
                            XtraMessageBox.Show(
                                "Sipariş silinirken bir hata oluştu.",
                                "Hata",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void GridViewJobs_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column != null && e.Column.FieldName == "DeleteAction")
            {
                if (e.IsGetData)
                {
                    var job = e.Row as PrintJob;
                    // Sadece tamamlanan işler için silme butonu göster
                    if (job != null && job.Status == JobStatus.Completed)
                    {
                        e.Value = "🗑️ Sil";
                    }
                    else
                    {
                        e.Value = string.Empty;
                    }
                }
            }
        }

        private void GridViewJobs_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            var view = sender as GridView;
            if (view == null) return;

            var hitInfo = view.CalcHitInfo(e.Location);
            if (hitInfo.InRowCell && hitInfo.Column != null && hitInfo.Column.FieldName == "DeleteAction")
            {
                var job = view.GetRow(hitInfo.RowHandle) as PrintJob;
                if (job != null)
                {
                    // Sadece tamamlanan işler silinebilir
                    if (job.Status != JobStatus.Completed)
                    {
                        XtraMessageBox.Show(
                            "Sadece tamamlanan işler silinebilir.",
                            "Bilgi",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Information);
                        return;
                    }

                    var result = XtraMessageBox.Show(
                        $"Tamamlanan iş #{job.Id} silinecek.\n\nBu işlem geri alınamaz. Devam etmek istiyor musunuz?",
                        "İşi Sil",
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                    
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        bool deleted = _jobAssignmentService.DeleteJob(job.Id);
                        if (deleted)
                        {
                            RefreshData();
                            XtraMessageBox.Show(
                                "İş başarıyla silindi.",
                                "Başarılı",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Information);
                        }
                        else
                        {
                            XtraMessageBox.Show(
                                "İş silinirken bir hata oluştu.",
                                "Hata",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }


        private void GridViewJobs_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (_currentTheme == ThemeMode.Dark)
            {
                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                e.Appearance.BackColor = e.RowHandle % 2 == 0 ? System.Drawing.Color.FromArgb(35, 35, 35) : System.Drawing.Color.FromArgb(45, 45, 45);
            }
            else
            {
                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(33, 33, 33);
                e.Appearance.BackColor = e.RowHandle % 2 == 0 ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(249, 250, 252);
            }
            e.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        }

        private void GridViewPrinters_DoubleClick(object sender, EventArgs e)
        {
            var view = sender as GridView;
            if (view == null) return;

            var focusedRowHandle = view.FocusedRowHandle;
            if (focusedRowHandle < 0) return;

            var printer = view.GetRow(focusedRowHandle) as Printer;
            if (printer == null) return;

            // Filament değiştirme dialog'unu aç
            OpenFilamentChangeDialog(printer);
        }

        private void GridViewPrinters_RowCellClick(object sender, DevExpress.XtraGrid.Views.Grid.RowCellClickEventArgs e)
        {
            try
            {
                // Sadece FilamentRemaining sütununa tıklanırsa
                if (e.Column != null && e.Column.FieldName == "FilamentRemaining")
                {
                    var printer = gridViewPrinters.GetRow(e.RowHandle) as Printer;
                    if (printer != null)
                    {
                        // Filament yenileme dialog'u aç
                        OpenFilamentRefillDialog(printer);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainForm] Filament yenileme dialog açılırken hata: {ex.Message}");
            }
        }

        private void OpenFilamentRefillDialog(Printer printer)
        {
            try
            {
                var dialog = new XtraForm
                {
                    Text = "Filament Yenile",
                    Size = new System.Drawing.Size(400, 250),
                    StartPosition = System.Windows.Forms.FormStartPosition.CenterParent,
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(30, 30, 30) : 
                        System.Drawing.Color.FromArgb(245, 247, 250)
                };

                var mainPanel = new System.Windows.Forms.Panel
                {
                    Dock = System.Windows.Forms.DockStyle.Fill,
                    Padding = new System.Windows.Forms.Padding(20),
                    BackColor = dialog.BackColor
                };
                dialog.Controls.Add(mainPanel);

                // Bilgi Label
                var lblInfo = new LabelControl
                {
                    Text = $"Yazıcı: {printer.Name}\nMevcut Filament: {printer.FilamentRemaining:F1}%",
                    Location = new System.Drawing.Point(10, 10),
                    Size = new System.Drawing.Size(360, 50),
                    Font = new System.Drawing.Font("Segoe UI", 10F),
                    ForeColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(230, 230, 230) : 
                        System.Drawing.Color.FromArgb(100, 100, 100)
                };
                mainPanel.Controls.Add(lblInfo);

                // Miktar Label
                var lblAmount = new LabelControl
                {
                    Text = "Yenileme Miktarı (%):",
                    Location = new System.Drawing.Point(10, 70),
                    Size = new System.Drawing.Size(150, 20),
                    Font = new System.Drawing.Font("Segoe UI", 9F),
                    ForeColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(230, 230, 230) : 
                        System.Drawing.Color.FromArgb(100, 100, 100)
                };
                mainPanel.Controls.Add(lblAmount);

                // Miktar SpinEdit
                var spinAmount = new SpinEdit
                {
                    Location = new System.Drawing.Point(170, 68),
                    Size = new System.Drawing.Size(200, 24),
                    Value = 100
                };
                spinAmount.Properties.MinValue = 0;
                spinAmount.Properties.MaxValue = 100;
                spinAmount.Properties.Increment = 5;
                if (_currentTheme == ThemeMode.Dark)
                {
                    spinAmount.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
                    spinAmount.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                }
                mainPanel.Controls.Add(spinAmount);

                // Tamam Butonu
                var btnOk = new SimpleButton
                {
                    Text = "Yenile",
                    Size = new System.Drawing.Size(120, 35),
                    Location = new System.Drawing.Point(100, 120),
                    DialogResult = System.Windows.Forms.DialogResult.OK
                };
                btnOk.Appearance.BackColor = System.Drawing.Color.FromArgb(76, 175, 80);
                btnOk.Appearance.ForeColor = System.Drawing.Color.White;
                btnOk.Appearance.Options.UseBackColor = true;
                btnOk.Appearance.Options.UseForeColor = true;
                btnOk.LookAndFeel.UseDefaultLookAndFeel = false;
                btnOk.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
                mainPanel.Controls.Add(btnOk);

                // İptal Butonu
                var btnCancel = new SimpleButton
                {
                    Text = "İptal",
                    Size = new System.Drawing.Size(120, 35),
                    Location = new System.Drawing.Point(230, 120),
                    DialogResult = System.Windows.Forms.DialogResult.Cancel
                };
                btnCancel.Appearance.BackColor = System.Drawing.Color.FromArgb(158, 158, 158);
                btnCancel.Appearance.ForeColor = System.Drawing.Color.White;
                btnCancel.Appearance.Options.UseBackColor = true;
                btnCancel.Appearance.Options.UseForeColor = true;
                btnCancel.LookAndFeel.UseDefaultLookAndFeel = false;
                btnCancel.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
                mainPanel.Controls.Add(btnCancel);

                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancel;

                if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    double amount = (double)spinAmount.Value;
                    bool success = _printerService.RefillFilament(printer.Id, amount);

                    if (success)
                    {
                        RefreshData();
                        lblStatus.Text = $"✓ Filament yenilendi: {printer.Name} -> {amount:F1}%";
                        lblStatus.ForeColor = System.Drawing.Color.FromArgb(76, 175, 80);
                        XtraMessageBox.Show(
                            $"Filament başarıyla yenilendi!\n\n" +
                            $"Yazıcı: {printer.Name}\n" +
                            $"Yeni Filament: {amount:F1}%",
                            "Filament Yenilendi",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Information);
                    }
                    else
                    {
                        XtraMessageBox.Show(
                            "Filament yenilenirken hata oluştu!",
                            "Hata",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    $"Filament yenilenirken hata oluştu:\n{ex.Message}",
                    "Hata",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void ShowPrinterDetails(Printer printer)
        {
            // Eğer zaten bir detay formu açıksa, yeni form açma
            if (_isDetailsFormOpen)
            {
                return;
            }

            try
            {
                _isDetailsFormOpen = true;
                
                // Yazıcı detayları formu oluştur (daha kompakt - boşluklar azaltıldı)
                var detailsForm = new XtraForm
                {
                    Text = $"🖨️ {printer.Name} - Detaylar",
                    Size = new System.Drawing.Size(780, 720), // Genişlik daha da küçültüldü (795 -> 780)
                    StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen, // Tam ortada
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(30, 30, 30) : 
                        System.Drawing.Color.FromArgb(245, 247, 250)
                };

                // Ana Panel (padding azaltıldı)
                var mainPanel = new System.Windows.Forms.Panel
                {
                    Dock = System.Windows.Forms.DockStyle.Fill,
                    Padding = new System.Windows.Forms.Padding(10), // 20'den 10'a düşürüldü
                    BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(30, 30, 30) : 
                        System.Drawing.Color.FromArgb(245, 247, 250)
                };
                detailsForm.Controls.Add(mainPanel);

                int yPos = 10; // 20'den 10'a düşürüldü
                int contentWidth = 650; // İçerik genişliği aynı kaldı
                int availableWidth = mainPanel.Width - (mainPanel.Padding.Left + mainPanel.Padding.Right);
                int startX = (availableWidth - contentWidth) / 2; // Ortala

                // Başlık
                var lblTitle = new LabelControl
                {
                    Text = $"🖨️ {printer.Name}",
                    Location = new System.Drawing.Point(startX, yPos),
                    Size = new System.Drawing.Size(contentWidth, 35),
                    Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold),
                    ForeColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(240, 240, 240) : 
                        System.Drawing.Color.Black
                };
                lblTitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                mainPanel.Controls.Add(lblTitle);
                yPos += 40; // 50'den 40'a düşürüldü (boşluk azaltıldı)

                // Durum bilgisi
                string statusText = "";
                System.Drawing.Color statusColor = System.Drawing.Color.Gray;
                switch (printer.Status)
                {
                    case PrinterStatus.Printing:
                        statusText = $"🟢 Yazdırıyor - %{printer.Progress:F1}";
                        statusColor = System.Drawing.Color.FromArgb(76, 175, 80);
                        break;
                    case PrinterStatus.Error:
                        statusText = "🔴 Hata";
                        statusColor = System.Drawing.Color.FromArgb(244, 67, 54);
                        break;
                    case PrinterStatus.Idle:
                        statusText = "⚫ Boşta";
                        statusColor = System.Drawing.Color.FromArgb(158, 158, 158);
                        break;
                    case PrinterStatus.Paused:
                        statusText = "⏸️ Duraklatıldı";
                        statusColor = System.Drawing.Color.FromArgb(255, 193, 7);
                        break;
                    case PrinterStatus.Maintenance:
                        statusText = "🔧 Bakımda";
                        statusColor = System.Drawing.Color.FromArgb(255, 152, 0);
                        break;
                }

                var lblStatus = new LabelControl
                {
                    Text = $"Durum: {statusText}",
                    Location = new System.Drawing.Point(startX, yPos),
                    Size = new System.Drawing.Size(contentWidth, 25),
                    Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                    ForeColor = statusColor
                };
                lblStatus.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                mainPanel.Controls.Add(lblStatus);
                yPos += 35; // 40'tan 35'e düşürüldü (boşluk azaltıldı)

                // Detay bilgileri paneli (Scrollbar'ı kaldırmak için yükseklik artırıldı)
                // Form yüksekliği 750, başlık ve durum için ~110px, padding için 40px, kapat butonu için ~80px
                // Kalan alan: 750 - 110 - 40 - 80 = 520px
                int scrollPanelHeight = detailsForm.Height - yPos - 100; // Alt boşluk artırıldı (80 -> 100)
                var scrollPanel = new System.Windows.Forms.Panel
                {
                    Location = new System.Drawing.Point(startX, yPos),
                    Size = new System.Drawing.Size(contentWidth, scrollPanelHeight),
                    BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(30, 30, 30) : 
                        System.Drawing.Color.FromArgb(245, 247, 250),
                    AutoScroll = false // Scrollbar'ı kapat - içerik yüksekliğine göre ayarlanacak
                };
                mainPanel.Controls.Add(scrollPanel);

                var detailsPanel = new System.Windows.Forms.Panel
                {
                    Location = new System.Drawing.Point(0, 0),
                    Size = new System.Drawing.Size(contentWidth, scrollPanelHeight), // Başlangıçta scrollPanel yüksekliği
                    BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(40, 40, 40) : 
                        System.Drawing.Color.White,
                    BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                    Padding = new System.Windows.Forms.Padding(10) // 15'ten 10'a düşürüldü
                };
                scrollPanel.Controls.Add(detailsPanel);

                int detailY = 10; // Padding azaldığı için 15'ten 10'a düşürüldü

                // Yazıcı ID
                CreateDetailRow(detailsPanel, "Yazıcı ID:", printer.Id.ToString(), detailY);
                detailY += 30;

                // Durum Detayı
                string statusDetail = "";
                if (printer.Status == PrinterStatus.Error)
                {
                    statusDetail = "⚠️ Arıza Tespit Edildi - Acil Müdahale Gerekli";
                }
                else if (printer.Status == PrinterStatus.Maintenance)
                {
                    statusDetail = "🔧 Bakım Modunda - Kullanılamaz";
                }
                else if (printer.Status == PrinterStatus.Paused)
                {
                    statusDetail = "⏸️ İş Duraklatıldı - Devam Ettirilebilir";
                }
                else if (printer.Status == PrinterStatus.Printing)
                {
                    statusDetail = $"🟢 Aktif Yazdırma - %{printer.Progress:F1} Tamamlandı";
                }
                else
                {
                    statusDetail = "⚫ Hazır - Yeni İş Alabilir";
                }
                CreateDetailRow(detailsPanel, "Durum Detayı:", statusDetail, detailY);
                detailY += 30;

                // Arıza Göstergesi
                if (printer.Status == PrinterStatus.Error)
                {
                    int errorPanelWidth = detailsPanel.Width - 20; // Padding için
                    var errorPanel = new System.Windows.Forms.Panel
                    {
                        Location = new System.Drawing.Point(10, detailY),
                        Size = new System.Drawing.Size(errorPanelWidth, 50),
                        BackColor = System.Drawing.Color.FromArgb(60, 30, 30),
                        BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                        Padding = new System.Windows.Forms.Padding(10)
                    };
                    detailsPanel.Controls.Add(errorPanel);

                    var lblError = new LabelControl
                    {
                        Text = "🔴 ARIZA TESPİT EDİLDİ",
                        Location = new System.Drawing.Point(10, 10),
                        Size = new System.Drawing.Size(errorPanelWidth - 20, 30),
                        Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                        ForeColor = System.Drawing.Color.FromArgb(255, 138, 128)
                    };
                    lblError.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    errorPanel.Controls.Add(lblError);
                    detailY += 60;
                }

                // Mevcut İş
                CreateDetailRow(detailsPanel, "Mevcut İş:", printer.CurrentJobName ?? "Yok", detailY);
                detailY += 30;


                // Filament Bilgileri
                CreateDetailRow(detailsPanel, "Filament Tipi:", printer.FilamentType, detailY);
                detailY += 30;
                
                // Filament Durumu (Görsel)
                string filamentStatus = "";
                System.Drawing.Color filamentColor = System.Drawing.Color.Gray;
                if (printer.FilamentRemaining > 50)
                {
                    filamentStatus = $"🟢 Yeterli - %{printer.FilamentRemaining:F1}";
                    filamentColor = System.Drawing.Color.FromArgb(76, 175, 80);
                }
                else if (printer.FilamentRemaining > 20)
                {
                    filamentStatus = $"🟡 Düşük - %{printer.FilamentRemaining:F1}";
                    filamentColor = System.Drawing.Color.FromArgb(255, 193, 7);
                }
                else
                {
                    filamentStatus = $"🔴 Kritik - %{printer.FilamentRemaining:F1}";
                    filamentColor = System.Drawing.Color.FromArgb(244, 67, 54);
                }
                CreateDetailRowColored(detailsPanel, "Filament Durumu:", filamentStatus, filamentColor, detailY);
                detailY += 30;

                // İş Zamanları
                if (printer.JobStartTime.HasValue)
                {
                    CreateDetailRow(detailsPanel, "İş Başlangıcı:", printer.JobStartTime.Value.ToString("dd.MM.yyyy HH:mm:ss"), detailY);
                    detailY += 30;
                    
                    // Geçen Süre
                    var elapsed = DateTime.Now - printer.JobStartTime.Value;
                    CreateDetailRow(detailsPanel, "Geçen Süre:", $"{elapsed.Hours} saat {elapsed.Minutes} dakika", detailY);
                    detailY += 30;
                }
                if (printer.JobEndTime.HasValue)
                {
                    CreateDetailRow(detailsPanel, "Tahmini Bitiş:", printer.JobEndTime.Value.ToString("dd.MM.yyyy HH:mm:ss"), detailY);
                    detailY += 30;
                    
                    // Kalan Süre
                    var remaining = printer.JobEndTime.Value - DateTime.Now;
                    if (remaining.TotalMinutes > 0)
                    {
                        CreateDetailRow(detailsPanel, "Kalan Süre:", $"{(int)remaining.TotalMinutes} dakika", detailY);
                    }
                    else
                    {
                        CreateDetailRow(detailsPanel, "Kalan Süre:", "Süre doldu", detailY);
                    }
                    detailY += 30;
                }

                // Yazıcı İstatistikleri Başlığı
                int headerWidth = detailsPanel.Width - 20; // Padding için
                var statsHeader = new LabelControl
                {
                    Text = "📊 YAZICI İSTATİSTİKLERİ",
                    Location = new System.Drawing.Point(10, detailY),
                    Size = new System.Drawing.Size(headerWidth, 25),
                    Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold),
                    ForeColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(200, 200, 200) : 
                        System.Drawing.Color.FromArgb(63, 81, 181)
                };
                statsHeader.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                detailsPanel.Controls.Add(statsHeader);
                detailY += 35;

                // İstatistikler
                CreateDetailRow(detailsPanel, "Tamamlanan İş Sayısı:", printer.TotalJobsCompleted.ToString(), detailY);
                detailY += 30;
                CreateDetailRow(detailsPanel, "Toplam Yazdırma Süresi:", $"{printer.TotalPrintTime:F1} saat", detailY);
                detailY += 30;
                
                // Ortalama İş Süresi
                if (printer.TotalJobsCompleted > 0)
                {
                    double avgTime = printer.TotalPrintTime / printer.TotalJobsCompleted;
                    CreateDetailRow(detailsPanel, "Ortalama İş Süresi:", $"{avgTime:F2} saat", detailY);
                    detailY += 30;
                }

                // Mevcut Hata Durumu Başlığı
                detailY += 10;
                var errorStatusHeader = new LabelControl
                {
                    Text = "⚠️ MEVCUT HATA DURUMU",
                    Location = new System.Drawing.Point(10, detailY),
                    Size = new System.Drawing.Size(headerWidth, 25),
                    Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold),
                    ForeColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(200, 200, 200) : 
                        System.Drawing.Color.FromArgb(63, 81, 181)
                };
                errorStatusHeader.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                detailsPanel.Controls.Add(errorStatusHeader);
                detailY += 35;

                // Mevcut Hatalar
                CreateDetailRow(detailsPanel, "Mevcut Hatalar:", "Yok", detailY);
                detailY += 30;

                // Son İşler (JobAssignmentService'den al)
                if (_jobAssignmentService != null)
                {
                    var printerJobs = _jobAssignmentService.GetAllJobs()
                        .Where(j => j.PrinterId == printer.Id)
                        .OrderByDescending(j => j.CreatedAt)
                        .Take(5)
                        .ToList();
                    
                    if (printerJobs.Any())
                    {
                        detailY += 10;
                        var jobsHeader = new LabelControl
                        {
                            Text = "📋 SON İŞLER",
                            Location = new System.Drawing.Point(10, detailY),
                            Size = new System.Drawing.Size(headerWidth, 25),
                            Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold),
                            ForeColor = _currentTheme == ThemeMode.Dark ? 
                                System.Drawing.Color.FromArgb(200, 200, 200) : 
                                System.Drawing.Color.FromArgb(63, 81, 181)
                        };
                        jobsHeader.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                        detailsPanel.Controls.Add(jobsHeader);
                        detailY += 35;

                        foreach (var job in printerJobs)
                        {
                            string jobStatus = job.Status.ToString();
                            string jobInfo = $"{job.ModelFileName} - {jobStatus}";
                            if (job.Status == JobStatus.Completed && job.CompletedAt.HasValue)
                            {
                                jobInfo += $" ({job.CompletedAt.Value:dd.MM.yyyy HH:mm})";
                            }
                            CreateDetailRow(detailsPanel, $"İş #{job.Id}:", jobInfo, detailY);
                            detailY += 25;
                        }
                    }
                }

                // Panel yüksekliğini içeriğe göre ayarla
                int calculatedHeight = detailY + 20;
                
                // Eğer içerik scrollPanel'den büyükse, scrollPanel'i büyüt (scrollbar olmaması için)
                if (calculatedHeight > scrollPanelHeight)
                {
                    scrollPanel.Height = calculatedHeight + 10; // 10px padding
                    detailsPanel.Height = calculatedHeight;
                }
                else
                {
                    detailsPanel.Height = calculatedHeight;
                }

                // Kapat Butonu
                var closeButton = new SimpleButton
                {
                    Text = "Kapat",
                    Size = new System.Drawing.Size(120, 40),
                    Location = new System.Drawing.Point(detailsForm.Width - 150, detailsForm.Height - 80),
                    Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right,
                    DialogResult = System.Windows.Forms.DialogResult.OK,
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
                };
                closeButton.Appearance.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
                closeButton.Appearance.ForeColor = System.Drawing.Color.White;
                closeButton.Appearance.Options.UseBackColor = true;
                closeButton.Appearance.Options.UseForeColor = true;
                closeButton.LookAndFeel.UseDefaultLookAndFeel = false;
                closeButton.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
                detailsForm.Controls.Add(closeButton);
                detailsForm.AcceptButton = closeButton;

                // Form kapanırken flag'i sıfırla
                detailsForm.FormClosed += (s, e) => 
                {
                    _isDetailsFormOpen = false;
                };

                detailsForm.ShowDialog(this);
                
                // Dialog kapandıktan sonra flag'i sıfırla (güvenlik için)
                _isDetailsFormOpen = false;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    $"Yazıcı detayları gösterilirken hata oluştu:\n{ex.Message}",
                    "Hata",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void CreateDetailRow(System.Windows.Forms.Panel panel, string label, string value, int y)
        {
            int labelWidth = 180;
            int valueWidth = panel.Width - labelWidth - 30; // 30 = padding + spacing
            
            var lblLabel = new LabelControl
            {
                Text = label,
                Location = new System.Drawing.Point(10, y),
                Size = new System.Drawing.Size(labelWidth, 20),
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                ForeColor = _currentTheme == ThemeMode.Dark ? 
                    System.Drawing.Color.FromArgb(180, 180, 180) : 
                    System.Drawing.Color.FromArgb(100, 100, 100)
            };
            panel.Controls.Add(lblLabel);

            var lblValue = new LabelControl
            {
                Text = value,
                Location = new System.Drawing.Point(10 + labelWidth + 10, y),
                Size = new System.Drawing.Size(valueWidth, 20),
                Font = new System.Drawing.Font("Segoe UI", 10F),
                ForeColor = _currentTheme == ThemeMode.Dark ? 
                    System.Drawing.Color.FromArgb(240, 240, 240) : 
                    System.Drawing.Color.Black
            };
            panel.Controls.Add(lblValue);
        }

        private void CreateDetailRowColored(System.Windows.Forms.Panel panel, string label, string value, System.Drawing.Color valueColor, int y)
        {
            int labelWidth = 180;
            int valueWidth = panel.Width - labelWidth - 30; // 30 = padding + spacing
            
            var lblLabel = new LabelControl
            {
                Text = label,
                Location = new System.Drawing.Point(10, y),
                Size = new System.Drawing.Size(labelWidth, 20),
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                ForeColor = _currentTheme == ThemeMode.Dark ? 
                    System.Drawing.Color.FromArgb(180, 180, 180) : 
                    System.Drawing.Color.FromArgb(100, 100, 100)
            };
            panel.Controls.Add(lblLabel);

            var lblValue = new LabelControl
            {
                Text = value,
                Location = new System.Drawing.Point(10 + labelWidth + 10, y),
                Size = new System.Drawing.Size(valueWidth, 20),
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                ForeColor = valueColor
            };
            panel.Controls.Add(lblValue);
        }

        private void OpenFilamentChangeDialog(Printer printer)
        {
            try
            {
                using (var dialog = new System.Windows.Forms.Form())
                {
                    dialog.Text = "Filament Değiştir";
                    dialog.Size = new System.Drawing.Size(450, 200);
                    dialog.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
                    dialog.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                    dialog.MaximizeBox = false;
                    dialog.MinimizeBox = false;
                    dialog.ShowInTaskbar = false;
                    dialog.BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(40, 40, 40) : 
                        System.Drawing.Color.White;

                    // Yazıcı Bilgisi Label
                    var lblPrinterInfo = new LabelControl
                    {
                        Text = $"Yazıcı: {printer.Name}\nMevcut Filament: {printer.FilamentType}",
                        Location = new System.Drawing.Point(20, 20),
                        Size = new System.Drawing.Size(400, 40),
                        Font = new System.Drawing.Font("Segoe UI", 10F),
                        ForeColor = _currentTheme == ThemeMode.Dark ? 
                            System.Drawing.Color.FromArgb(230, 230, 230) : 
                            System.Drawing.Color.Black
                    };
                    dialog.Controls.Add(lblPrinterInfo);

                    // Filament Label
                    var lblFilament = new LabelControl
                    {
                        Text = "Yeni Filament Tipi:",
                        Location = new System.Drawing.Point(20, 70),
                        Size = new System.Drawing.Size(120, 20),
                        Font = new System.Drawing.Font("Segoe UI", 10F),
                        ForeColor = _currentTheme == ThemeMode.Dark ? 
                            System.Drawing.Color.FromArgb(230, 230, 230) : 
                            System.Drawing.Color.Black
                    };
                    dialog.Controls.Add(lblFilament);

                    // Filament ComboBox
                    var comboFilament = new ComboBoxEdit
                    {
                        Location = new System.Drawing.Point(150, 67),
                        Size = new System.Drawing.Size(250, 25),
                        Font = new System.Drawing.Font("Segoe UI", 10F)
                    };
                    
                    // Filament çeşitlerini yükle
                    var filamentTypes = PrinterService.GetAvailableFilamentTypes();
                    comboFilament.Properties.Items.AddRange(filamentTypes);
                    
                    // Mevcut filament tipini seçili yap
                    int currentIndex = filamentTypes.IndexOf(printer.FilamentType);
                    if (currentIndex >= 0)
                        comboFilament.SelectedIndex = currentIndex;
                    else if (comboFilament.Properties.Items.Count > 0)
                        comboFilament.SelectedIndex = 0;
                    
                    // Tema renkleri
                    if (_currentTheme == ThemeMode.Dark)
                    {
                        comboFilament.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
                        comboFilament.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                    }
                    
                    dialog.Controls.Add(comboFilament);

                    // Butonlar
                    var btnOK = new SimpleButton
                    {
                        Text = "Değiştir",
                        Location = new System.Drawing.Point(230, 110),
                        Size = new System.Drawing.Size(80, 35),
                        DialogResult = System.Windows.Forms.DialogResult.OK,
                        Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
                    };
                    btnOK.Appearance.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
                    btnOK.Appearance.ForeColor = System.Drawing.Color.White;
                    btnOK.Appearance.Options.UseBackColor = true;
                    btnOK.Appearance.Options.UseForeColor = true;
                    btnOK.LookAndFeel.UseDefaultLookAndFeel = false;
                    btnOK.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
                    dialog.Controls.Add(btnOK);
                    dialog.AcceptButton = btnOK;

                    var btnCancel = new SimpleButton
                    {
                        Text = "İptal",
                        Location = new System.Drawing.Point(320, 110),
                        Size = new System.Drawing.Size(80, 35),
                        DialogResult = System.Windows.Forms.DialogResult.Cancel,
                        Font = new System.Drawing.Font("Segoe UI", 10F)
                    };
                    btnCancel.Appearance.BackColor = System.Drawing.Color.FromArgb(158, 158, 158);
                    btnCancel.Appearance.ForeColor = System.Drawing.Color.White;
                    btnCancel.Appearance.Options.UseBackColor = true;
                    btnCancel.Appearance.Options.UseForeColor = true;
                    btnCancel.LookAndFeel.UseDefaultLookAndFeel = false;
                    btnCancel.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
                    dialog.Controls.Add(btnCancel);
                    dialog.CancelButton = btnCancel;

                    if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        if (comboFilament.SelectedIndex < 0)
                        {
                            XtraMessageBox.Show(
                                "Lütfen bir filament tipi seçin!",
                                "Uyarı",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Warning);
                            return;
                        }

                        string newFilamentType = comboFilament.Text;

                        // Yazıcı yazdırma yapıyorsa veya ilerleme varsa uyarı ver
                        if (printer.Status == PrinterStatus.Printing || printer.Progress > 0)
                        {
                            string statusMessage = printer.Status == PrinterStatus.Printing 
                                ? "Yazıcı şu anda yazdırma yapıyor!" 
                                : $"Yazıcıda aktif bir iş var (İlerleme: %{printer.Progress:F1})!";
                            
                            XtraMessageBox.Show(
                                $"{statusMessage}\n\n" +
                                $"Yazıcı: {printer.Name}\n" +
                                $"Mevcut İş: {printer.CurrentJobName ?? "Yok"}\n" +
                                $"İlerleme: %{printer.Progress:F1}\n\n" +
                                $"Filament değiştirmek için yazdırmanın tamamlanmasını bekleyin.",
                                "Uyarı",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Warning);
                            return;
                        }

                        // Filament değiştir
                        string oldFilamentType = printer.FilamentType;
                        bool success = _printerService.ChangeFilamentType(printer.Id, newFilamentType);
                        if (success)
                        {
                            RefreshData();
                            lblStatus.Text = $"✓ Filament değiştirildi: {printer.Name} -> {newFilamentType}";
                            lblStatus.ForeColor = System.Drawing.Color.FromArgb(129, 199, 132);
                            
                            XtraMessageBox.Show(
                                $"Filament başarıyla değiştirildi!\n\n" +
                                $"Yazıcı: {printer.Name}\n" +
                                $"Eski Filament: {oldFilamentType}\n" +
                                $"Yeni Filament: {newFilamentType}",
                                "Filament Değiştirildi",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Information);
                        }
                        else
                        {
                            XtraMessageBox.Show(
                                "Filament değiştirilemedi!",
                                "Hata",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    $"Filament değiştirilirken hata oluştu:\n{ex.Message}",
                    "Hata",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void GridControl_Paint(object sender, PaintEventArgs e)
        {
            // GridControl'un paint event'i - filtre paneli görünümünü güncellemek için
            var gridControl = sender as GridControl;
            if (gridControl == null) return;

            var gridView = gridControl.MainView as GridView;
            if (gridView == null) return;

            // Filtre paneli görünümünü tema değişikliğinde güncelle
            try
            {
                if (_currentTheme == ThemeMode.Dark)
                {
                    gridView.Appearance.FilterPanel.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
                    gridView.Appearance.FilterPanel.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                    gridView.Appearance.FilterPanel.Options.UseBackColor = true;
                    gridView.Appearance.FilterPanel.Options.UseForeColor = true;
                }
                else
                {
                    gridView.Appearance.FilterPanel.BackColor = System.Drawing.Color.FromArgb(249, 250, 252);
                    gridView.Appearance.FilterPanel.ForeColor = System.Drawing.Color.Black;
                    gridView.Appearance.FilterPanel.Options.UseBackColor = true;
                    gridView.Appearance.FilterPanel.Options.UseForeColor = true;
                }
            }
            catch { }
        }

        private void UpdateFilterPanelsForDarkTheme()
        {
            // Tüm grid'lerin filtre panellerini koyu temaya uygun hale getir
            UpdateFilterPanelTheme(gridViewPrinters, true);
            UpdateFilterPanelTheme(gridViewOrders, true);
            UpdateFilterPanelTheme(gridViewJobs, true);
        }

        private void UpdateFilterPanelsForLightTheme()
        {
            // Tüm grid'lerin filtre panellerini açık temaya uygun hale getir
            UpdateFilterPanelTheme(gridViewPrinters, false);
            UpdateFilterPanelTheme(gridViewOrders, false);
            UpdateFilterPanelTheme(gridViewJobs, false);
        }

        private void UpdateFilterPanelTheme(GridView gridView, bool isDark)
        {
            if (gridView == null) return;

            try
            {
                if (isDark)
                {
                    gridView.Appearance.FilterPanel.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
                    gridView.Appearance.FilterPanel.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                    gridView.Appearance.FilterPanel.Options.UseBackColor = true;
                    gridView.Appearance.FilterPanel.Options.UseForeColor = true;
                    gridView.Appearance.FilterPanel.Options.UseTextOptions = true;
                }
                else
                {
                    gridView.Appearance.FilterPanel.BackColor = System.Drawing.Color.FromArgb(249, 250, 252);
                    gridView.Appearance.FilterPanel.ForeColor = System.Drawing.Color.Black;
                    gridView.Appearance.FilterPanel.Options.UseBackColor = true;
                    gridView.Appearance.FilterPanel.Options.UseForeColor = true;
                    gridView.Appearance.FilterPanel.Options.UseTextOptions = true;
                }
                // Grid'i yenile
                if (gridView.GridControl != null)
                {
                    gridView.GridControl.Invalidate();
                }
            }
            catch { }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            // Başlık panelini güncelle
            if (titlePanel != null)
            {
                titlePanel.Width = this.ClientSize.Width;
                titlePanel.Invalidate(); // Gradient'i yeniden çiz (tam ekranda düzgün görünmesi için)
            }

            // contentPanel boyutlarını güncelle
            if (contentPanel != null)
            {
                contentPanel.Location = new System.Drawing.Point(0, 80);
                contentPanel.Size = new System.Drawing.Size(this.ClientSize.Width, this.ClientSize.Height - 80);
            }

            // Buton konumlarını güncelle
            if (btnSimulateOrder != null && btnAddPrinter != null && btnSettings != null && titlePanel != null)
            {
                btnSettings.Left = titlePanel.Width - btnSettings.Width - 20;
                btnAddPrinter.Left = btnSettings.Left - btnAddPrinter.Width - 10;
                btnSimulateOrder.Left = btnAddPrinter.Left - btnSimulateOrder.Width - 10;
                
                // Ayarlar panelinin konumunu güncelle
                if (settingsPanel != null && settingsPanel.Visible)
                {
                    int panelX = btnSettings.Right - settingsPanel.Width;
                    int panelY = btnSettings.Bottom + 5;
                    settingsPanel.Location = new System.Drawing.Point(panelX, panelY);
                }
                
                if (btnShowModels != null)
                {
                    btnShowModels.Left = btnSimulateOrder.Left - btnShowModels.Width - 10;
                }
            }
            
            // Tamamlanan siparişleri sil butonunu siparişler başlık panelinde güncelle
            if (btnDeleteCompletedOrders != null && ordersHeaderPanel != null)
            {
                btnDeleteCompletedOrders.Left = ordersHeaderPanel.Width - btnDeleteCompletedOrders.Width - 10;
                btnDeleteCompletedOrders.Top = 3;
                btnDeleteCompletedOrders.Visible = true;
                
                if (btnDeleteCompletedJobs != null && jobsHeaderPanel != null)
                {
                    btnDeleteCompletedJobs.Left = jobsHeaderPanel.Width - btnDeleteCompletedJobs.Width - 10;
                    btnDeleteCompletedJobs.Top = 3;
                    btnDeleteCompletedJobs.Visible = true;
                }
            }

            // İstatistikler panelini bul
            var statsPanel = this.Controls.OfType<System.Windows.Forms.Panel>()
                .FirstOrDefault(p => p.Controls.OfType<LabelControl>().Any(l => l.Text.Contains("İSTATİSTİKLER")));
            
            // Grid'lerin genişliğini ayarla (eşit genişlikte)
            if (gridControlPrinters != null && gridControlOrders != null && gridControlJobs != null)
            {
                int availableWidth = this.ClientSize.Width - 60; // 20px margin her iki tarafta
                int spacing = 20;
                // Üç grid için eşit genişlik hesapla
                int gridWidth = (availableWidth - (spacing * 2)) / 3; // İki spacing arasındaki alanı 3'e böl

                gridControlPrinters.Width = gridWidth;
                gridControlOrders.Left = gridControlPrinters.Right + spacing;
                gridControlOrders.Width = gridWidth;
                gridControlJobs.Left = gridControlOrders.Right + spacing;
                gridControlJobs.Width = gridWidth; // Eşit genişlik

                // Sütun genişliklerini grid genişliğine göre ayarla
                UpdateGridColumnWidths();

                // Header panellerini güncelle
                if (printersHeaderPanel != null)
                {
                    printersHeaderPanel.Width = gridControlPrinters.Width;
                }

                if (ordersHeaderPanel != null)
                {
                    ordersHeaderPanel.Left = gridControlOrders.Left;
                    ordersHeaderPanel.Width = gridControlOrders.Width;
                    
                    // Tamamlananları sil butonunu güncelle
                    if (btnDeleteCompletedOrders != null)
                    {
                        btnDeleteCompletedOrders.Left = ordersHeaderPanel.Width - btnDeleteCompletedOrders.Width - 10;
                    }
                }

                if (jobsHeaderPanel != null)
                {
                    jobsHeaderPanel.Left = gridControlJobs.Left;
                    jobsHeaderPanel.Width = gridControlJobs.Width;
                    
                    // Tamamlananları sil butonunu güncelle
                    if (btnDeleteCompletedJobs != null)
                    {
                        btnDeleteCompletedJobs.Left = jobsHeaderPanel.Width - btnDeleteCompletedJobs.Width - 10;
                    }
                }
            }

            // İstatistikler panelini önce güncelle (diğer kontrollerin konumlandırması için gerekli)
            if (statsPanel != null && contentPanel != null)
            {
                // statsPanel Anchor=Bottom|Left|Right olduğu için, sadece genişlik ve sol konumu güncelle
                // Top değeri Anchor tarafından otomatik olarak ayarlanacak
                statsPanel.Width = this.ClientSize.Width - 40; // Doğrudan form genişliğini kullan
                statsPanel.Left = 20;
                // Yüksekliği 130 olarak sabit tut
                statsPanel.Height = 130;
                // statsPanel'i öne getir (siparişler formlarının üstünde görünsün)
                statsPanel.BringToFront();
            }

            // Yazıcı icon paneli (küçük, scroll olmayacak)
            if (printersIconPanel != null && contentPanel != null && statsPanel != null)
            {
                // statsPanel'in üstünde konumlandır
                // statsPanel Anchor=Bottom olduğu için, contentPanel.Height kullanarak hesapla
                int statsPanelTop = contentPanel.Height - statsPanel.Height - 1;
                int iconPanelTop = statsPanelTop - 110; // statsPanel'in üstünde 110 piksel margin ile
                printersIconPanel.Left = 20;
                printersIconPanel.Width = this.ClientSize.Width - 40; // Doğrudan form genişliğini kullan
                printersIconPanel.Top = iconPanelTop;
                printersIconPanel.Height = 100;
                printersIconPanel.AutoScroll = false; // Scroll'u kapat
                // printersIconPanel'i öne getir (grid'lerin üstünde görünsün)
                printersIconPanel.BringToFront();
            }

            // Grid yüksekliklerini ayarla
            if (gridControlPrinters != null && contentPanel != null && statsPanel != null)
            {
                int gridTop = 60; // Header panel yüksekliği 40 + margin 20
                // statsPanel'in üstünde printersIconPanel var, onun üstünde grid'ler olmalı
                int statsPanelTop = contentPanel.Height - statsPanel.Height - 1;
                int iconPanelTop = printersIconPanel != null ? printersIconPanel.Top : statsPanelTop - 110;
                // Grid ile printersIconPanel arasında daha fazla boşluk bırak (30 piksel)
                int gridHeight = iconPanelTop - gridTop - 30;
                
                // Minimum yükseklik kontrolü
                if (gridHeight > 100)
                {
                    gridControlPrinters.Height = gridHeight;
                    if (gridControlOrders != null) gridControlOrders.Height = gridHeight;
                    if (gridControlJobs != null) gridControlJobs.Height = gridHeight;
                }
                else
                {
                    // Minimum yükseklik ayarla
                    int minHeight = 100;
                    gridControlPrinters.Height = minHeight;
                    if (gridControlOrders != null) gridControlOrders.Height = minHeight;
                    if (gridControlJobs != null) gridControlJobs.Height = minHeight;
                }
            }
        }

        private void UpdateGridColumnWidths()
        {
            // Printers Grid sütun genişliklerini ayarla
            if (gridViewPrinters != null && gridControlPrinters != null)
            {
                int gridWidth = gridControlPrinters.Width;
                int minTotalWidth = 428; // Minimum toplam genişlik (29+79+59+89+54+54+64 = 428)
                int indicatorWidth = 20; // Grid indicator genişliği
                int availableWidth = gridWidth - indicatorWidth;

                if (availableWidth > 0)
                {
                    // Sütunları grid genişliğine göre orantılı olarak ayarla
                    double scaleFactor = (double)availableWidth / minTotalWidth;
                    
                    if (gridViewPrinters.Columns["Id"] != null)
                        gridViewPrinters.Columns["Id"].Width = Math.Max(20, (int)(29 * scaleFactor));
                    if (gridViewPrinters.Columns["Name"] != null)
                        gridViewPrinters.Columns["Name"].Width = Math.Max(50, (int)(79 * scaleFactor));
                    if (gridViewPrinters.Columns["Status"] != null)
                        gridViewPrinters.Columns["Status"].Width = Math.Max(40, (int)(59 * scaleFactor));
                    if (gridViewPrinters.Columns["CurrentJobName"] != null)
                        gridViewPrinters.Columns["CurrentJobName"].Width = Math.Max(60, (int)(89 * scaleFactor));
                    if (gridViewPrinters.Columns["Progress"] != null)
                        gridViewPrinters.Columns["Progress"].Width = Math.Max(40, (int)(54 * scaleFactor));
                    if (gridViewPrinters.Columns["FilamentRemaining"] != null)
                        gridViewPrinters.Columns["FilamentRemaining"].Width = Math.Max(40, (int)(54 * scaleFactor));
                    if (gridViewPrinters.Columns["FilamentType"] != null)
                        gridViewPrinters.Columns["FilamentType"].Width = Math.Max(45, (int)(64 * scaleFactor));
                }
            }

            // Orders Grid sütun genişliklerini ayarla
            if (gridViewOrders != null && gridControlOrders != null)
            {
                int gridWidth = gridControlOrders.Width;
                int minTotalWidth = 417; // Minimum toplam genişlik (28+78+68+78+53+63+48 = 416, yuvarlama ile 417)
                int indicatorWidth = 20; // Grid indicator genişliği
                int availableWidth = gridWidth - indicatorWidth;

                if (availableWidth > 0)
                {
                    // Sütunları grid genişliğine göre orantılı olarak ayarla
                    double scaleFactor = (double)availableWidth / minTotalWidth;
                    
                    if (gridViewOrders.Columns["Id"] != null)
                        gridViewOrders.Columns["Id"].Width = Math.Max(20, (int)(28 * scaleFactor));
                    if (gridViewOrders.Columns["OrderNumber"] != null)
                        gridViewOrders.Columns["OrderNumber"].Width = Math.Max(50, (int)(78 * scaleFactor));
                    if (gridViewOrders.Columns["CustomerName"] != null)
                        gridViewOrders.Columns["CustomerName"].Width = Math.Max(45, (int)(68 * scaleFactor));
                    if (gridViewOrders.Columns["OrderDate"] != null)
                        gridViewOrders.Columns["OrderDate"].Width = Math.Max(50, (int)(78 * scaleFactor));
                    if (gridViewOrders.Columns["Status"] != null)
                        gridViewOrders.Columns["Status"].Width = Math.Max(35, (int)(53 * scaleFactor));
                    if (gridViewOrders.Columns["TotalPrice"] != null)
                        gridViewOrders.Columns["TotalPrice"].Width = Math.Max(45, (int)(63 * scaleFactor));
                    if (gridViewOrders.Columns["DeleteAction"] != null)
                        gridViewOrders.Columns["DeleteAction"].Width = Math.Max(35, (int)(48 * scaleFactor));
                }
            }

            // Jobs Grid sütun genişliklerini ayarla
            if (gridViewJobs != null && gridControlJobs != null)
            {
                int gridWidth = gridControlJobs.Width;
                int minTotalWidth = 429; // Minimum toplam genişlik (42+107+52+62+62+52+52 = 429)
                int indicatorWidth = 20; // Grid indicator genişliği
                int availableWidth = gridWidth - indicatorWidth;

                if (availableWidth > 0)
                {
                    // Sütunları grid genişliğine göre orantılı olarak ayarla
                    double scaleFactor = (double)availableWidth / minTotalWidth;
                    
                    if (gridViewJobs.Columns["Id"] != null)
                        gridViewJobs.Columns["Id"].Width = Math.Max(30, (int)(42 * scaleFactor));
                    if (gridViewJobs.Columns["ModelFileName"] != null)
                        gridViewJobs.Columns["ModelFileName"].Width = Math.Max(70, (int)(107 * scaleFactor));
                    if (gridViewJobs.Columns["PrinterId"] != null)
                        gridViewJobs.Columns["PrinterId"].Width = Math.Max(35, (int)(52 * scaleFactor));
                    if (gridViewJobs.Columns["Status"] != null)
                        gridViewJobs.Columns["Status"].Width = Math.Max(45, (int)(62 * scaleFactor));
                    if (gridViewJobs.Columns["Progress"] != null)
                        gridViewJobs.Columns["Progress"].Width = Math.Max(45, (int)(62 * scaleFactor));
                    if (gridViewJobs.Columns["Material"] != null)
                        gridViewJobs.Columns["Material"].Width = Math.Max(35, (int)(52 * scaleFactor));
                    if (gridViewJobs.Columns["DeleteAction"] != null)
                        gridViewJobs.Columns["DeleteAction"].Width = Math.Max(35, (int)(52 * scaleFactor));
                }
            }
        }

        protected override void OnFormClosing(System.Windows.Forms.FormClosingEventArgs e)
        {
            // Timer'ı durdur
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
            }
            
            // Program kapanırken tüm yazıcıların durumlarını veritabanına kaydet
            // ÖNCE yazıcı durumlarını al (timer durmadan önce)
            if (_printerService != null && _mongoDbService != null && _mongoDbService.IsConnected())
            {
                try
                {
                    var printers = _printerService.GetAllPrinters();
                    var printerCollection = _mongoDbService.GetCollection<Printer>("printers");
                    
                    System.Diagnostics.Debug.WriteLine($"[MainForm] Program kapanıyor, {printers.Count} yazıcının durumu kaydediliyor...");
                    System.Console.WriteLine($"[MainForm] Program kapanıyor, {printers.Count} yazıcının durumu kaydediliyor...");
                    
                    foreach (var printer in printers)
                    {
                        try
                        {
                            // Yazıcı durumunu console'a yazdır (debug için)
                            System.Diagnostics.Debug.WriteLine($"[MainForm] Yazıcı #{printer.Id} durumu: Status={printer.Status}, Job={printer.CurrentJobName ?? "(null)"}, Progress={printer.Progress:F1}%");
                            System.Console.WriteLine($"[MainForm] Yazıcı #{printer.Id} durumu: Status={printer.Status}, Job={printer.CurrentJobName ?? "(null)"}, Progress={printer.Progress:F1}%");
                            
                            var filter = Builders<Printer>.Filter.Eq(p => p.Id, printer.Id);
                            var update = Builders<Printer>.Update
                                .Set(p => p.Status, printer.Status)
                                .Set(p => p.CurrentJobName, printer.CurrentJobName)
                                .Set(p => p.JobStartTime, printer.JobStartTime)
                                .Set(p => p.JobEndTime, printer.JobEndTime)
                                .Set(p => p.Progress, printer.Progress)
                                .Set(p => p.FilamentRemaining, printer.FilamentRemaining)
                                .Set(p => p.JobStartFilament, printer.JobStartFilament);
                            var result = printerCollection.UpdateOne(filter, update);
                            
                            if (result.ModifiedCount > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MainForm] ✓ Yazıcı #{printer.Id} durumu kaydedildi: Status={printer.Status}");
                                System.Console.WriteLine($"[MainForm] ✓ Yazıcı #{printer.Id} durumu kaydedildi: Status={printer.Status}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[MainForm] ⚠ Yazıcı #{printer.Id} durumu kaydedilemedi (ModifiedCount=0)");
                                System.Console.WriteLine($"[MainForm] ⚠ Yazıcı #{printer.Id} durumu kaydedilemedi (ModifiedCount=0)");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainForm] Yazıcı #{printer.Id} durumu kaydedilirken hata: {ex.Message}");
                            System.Console.WriteLine($"[MainForm] Yazıcı #{printer.Id} durumu kaydedilirken hata: {ex.Message}");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[MainForm] Tüm yazıcı durumları veritabanına kaydedildi");
                    System.Console.WriteLine($"[MainForm] Tüm yazıcı durumları veritabanına kaydedildi");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainForm] Yazıcı durumları kaydedilirken genel hata: {ex.Message}");
                    System.Console.WriteLine($"[MainForm] Yazıcı durumları kaydedilirken genel hata: {ex.Message}");
                }
            }
            
            base.OnFormClosing(e);
        }

        private string WrapText(string text, int maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var lines = new System.Collections.Generic.List<string>();
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                // Eğer kelime tek başına maxWidth'den uzunsa, zorla böl
                if (word.Length > maxWidth)
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }
                    // Uzun kelimeyi parçalara böl
                    for (int i = 0; i < word.Length; i += maxWidth)
                    {
                        int length = Math.Min(maxWidth, word.Length - i);
                        lines.Add(word.Substring(i, length));
                    }
                }
                else
                {
                    // Mevcut satıra eklenebilir mi kontrol et
                    int potentialLength = currentLine.Length + (currentLine.Length > 0 ? 1 : 0) + word.Length;
                    if (potentialLength > maxWidth && currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }
                    if (currentLine.Length > 0)
                        currentLine.Append(" ");
                    currentLine.Append(word);
                }
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine.ToString());

            return string.Join("\n", lines);
        }
    }
}