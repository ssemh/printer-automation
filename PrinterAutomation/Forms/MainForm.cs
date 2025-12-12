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
using DevExpress.Utils;
using PrinterAutomation.Models;
using PrinterAutomation.Services;

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
        private System.Windows.Forms.Panel titlePanel;
        private System.Windows.Forms.Panel printersHeaderPanel;
        private System.Windows.Forms.Panel ordersHeaderPanel;
        private System.Windows.Forms.Panel jobsHeaderPanel;
        private System.Windows.Forms.Panel statsPanel;
        private System.Windows.Forms.FlowLayoutPanel printersIconPanel;
        private System.Collections.Generic.Dictionary<int, System.Windows.Forms.Panel> printerIconPanels;

        public MainForm()
        {
            // ÖNCE InitializeComponent çağrılmalı ki MessageBox çalışsın
            InitializeComponent();
            
            // MongoDB servisini başlat - BASIT TEST
            MongoDbService mongoDbService = null;
            bool mongoDbConnected = false;
            
            try { mongoDbService = new MongoDbService(); mongoDbConnected = mongoDbService.IsConnected(); } catch (Exception ex) { mongoDbConnected = false; System.Diagnostics.Debug.WriteLine($"[MainForm] MongoDB bağlantı hatası: {ex.Message}"); }
            try
            {
                mongoDbService = new MongoDbService();
                mongoDbConnected = mongoDbService.IsConnected();
            }
            catch (Exception ex)
            {
                mongoDbConnected = false;
                System.Diagnostics.Debug.WriteLine($"[MainForm] MongoDB bağlantı hatası: {ex.Message}");
            }
            
            // MongoDB durumunu sakla (status label'da göstermek için)
            _mongoDbConnected = mongoDbConnected;
            
            System.Diagnostics.Debug.WriteLine($"[MainForm] MongoDB servisi durumu: {(mongoDbService != null ? "MEVCUT" : "NULL")}");
            System.Diagnostics.Debug.WriteLine($"[MainForm] MongoDB bağlantı durumu: {(mongoDbConnected ? "BAĞLI" : "BAĞLI DEĞİL")}");
            
            _printerService = new PrinterService(mongoDbService);
            System.Diagnostics.Debug.WriteLine("[MainForm] PrinterService oluşturuldu");
            
            _orderService = new OrderService(mongoDbService);
            System.Diagnostics.Debug.WriteLine("[MainForm] OrderService oluşturuldu");
            
            _jobAssignmentService = new JobAssignmentService(_printerService, _orderService, mongoDbService);
            System.Diagnostics.Debug.WriteLine("[MainForm] JobAssignmentService oluşturuldu");
            this.Shown += MainForm_Shown;
            SetupEventHandlers();
            StartRefreshTimer();
            // İlk temayı uygula
            ApplyTheme();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            try
            {
                InitializeData();
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
            this.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            this.MinimumSize = new System.Drawing.Size(1200, 650);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.WindowState = System.Windows.Forms.FormWindowState.Normal;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Resize += MainForm_Resize;

            // Başlık Panel (Gradient efekti için)
            titlePanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(this.ClientSize.Width, 80),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right,
                BackColor = System.Drawing.Color.FromArgb(30, 136, 229)
            };
            this.Controls.Add(titlePanel);

            // Başlık
            lblTitle = new LabelControl
            {
                Text = "🖨️ 3D YAZICI OTOMASYON SİSTEMİ",
                Location = new System.Drawing.Point(30, 20),
                Size = new System.Drawing.Size(600, 40),
                Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };
            titlePanel.Controls.Add(lblTitle);

            // Status Label (Başlık panelinde)
            lblStatus = new LabelControl
            {
                Text = "● Sistem Hazır",
                Location = new System.Drawing.Point(30, 50),
                Size = new System.Drawing.Size(400, 25),
                Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };
            titlePanel.Controls.Add(lblStatus);

            // Tema Değiştirme Butonu (önce ekleniyor, sağda olacak)
            btnToggleTheme = new SimpleButton
            {
                Text = "🌙 Koyu Tema",
                Size = new System.Drawing.Size(140, 45),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            };
            btnToggleTheme.Appearance.BackColor = System.Drawing.Color.FromArgb(33, 33, 33);
            btnToggleTheme.Appearance.ForeColor = System.Drawing.Color.White;
            btnToggleTheme.Appearance.BorderColor = System.Drawing.Color.FromArgb(66, 66, 66);
            btnToggleTheme.Appearance.Options.UseBackColor = true;
            btnToggleTheme.Appearance.Options.UseForeColor = true;
            btnToggleTheme.Appearance.Options.UseBorderColor = true;
            btnToggleTheme.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(66, 66, 66);
            btnToggleTheme.AppearanceHovered.Options.UseBackColor = true;
            btnToggleTheme.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
            btnToggleTheme.AppearancePressed.Options.UseBackColor = true;
            btnToggleTheme.LookAndFeel.UseDefaultLookAndFeel = false;
            btnToggleTheme.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            btnToggleTheme.Click += BtnToggleTheme_Click;
            titlePanel.Controls.Add(btnToggleTheme);
            btnToggleTheme.Location = new System.Drawing.Point(titlePanel.Width - btnToggleTheme.Width - 20, 20);

            // Yeni Yazıcı Ekle Button
            btnAddPrinter = new SimpleButton
            {
                Text = "🖨️ Yeni Yazıcı Ekle",
                Size = new System.Drawing.Size(200, 45),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right,
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold)
            };
            btnAddPrinter.Appearance.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
            btnAddPrinter.Appearance.ForeColor = System.Drawing.Color.White;
            btnAddPrinter.Appearance.BorderColor = System.Drawing.Color.FromArgb(25, 118, 210);
            btnAddPrinter.Appearance.Options.UseBackColor = true;
            btnAddPrinter.Appearance.Options.UseForeColor = true;
            btnAddPrinter.Appearance.Options.UseBorderColor = true;
            btnAddPrinter.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(30, 136, 229);
            btnAddPrinter.AppearanceHovered.Options.UseBackColor = true;
            btnAddPrinter.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(25, 118, 210);
            btnAddPrinter.AppearancePressed.Options.UseBackColor = true;
            btnAddPrinter.LookAndFeel.UseDefaultLookAndFeel = false;
            btnAddPrinter.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            btnAddPrinter.Click += BtnAddPrinter_Click;
            titlePanel.Controls.Add(btnAddPrinter);

            // Simulate Order Button (yeni yazıcı butonunun solunda)
            btnSimulateOrder = new SimpleButton
            {
                Text = "➕ Yeni Sipariş Simüle Et",
                Size = new System.Drawing.Size(270, 45),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right,
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold)
            };
            btnSimulateOrder.Appearance.BackColor = System.Drawing.Color.FromArgb(76, 175, 80);
            btnSimulateOrder.Appearance.ForeColor = System.Drawing.Color.White;
            btnSimulateOrder.Appearance.BorderColor = System.Drawing.Color.FromArgb(56, 142, 60);
            btnSimulateOrder.Appearance.Options.UseBackColor = true;
            btnSimulateOrder.Appearance.Options.UseForeColor = true;
            btnSimulateOrder.Appearance.Options.UseBorderColor = true;
            btnSimulateOrder.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(69, 160, 73);
            btnSimulateOrder.AppearanceHovered.Options.UseBackColor = true;
            btnSimulateOrder.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(56, 142, 60);
            btnSimulateOrder.AppearancePressed.Options.UseBackColor = true;
            btnSimulateOrder.LookAndFeel.UseDefaultLookAndFeel = false;
            btnSimulateOrder.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            btnSimulateOrder.Click += BtnSimulateOrder_Click;
            titlePanel.Controls.Add(btnSimulateOrder);
            btnAddPrinter.Location = new System.Drawing.Point(btnToggleTheme.Left - btnAddPrinter.Width - 10, 20);
            btnSimulateOrder.Location = new System.Drawing.Point(btnAddPrinter.Left - btnSimulateOrder.Width - 10, 20);

            // Printers Grid Başlık Panel
            printersHeaderPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(20, 100),
                Size = new System.Drawing.Size(450, 35),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left,
                BackColor = System.Drawing.Color.FromArgb(63, 81, 181)
            };
            this.Controls.Add(printersHeaderPanel);

            lblPrinters = new LabelControl
            {
                Text = "🖨️ 3D YAZICILAR",
                Location = new System.Drawing.Point(10, 5),
                Size = new System.Drawing.Size(430, 25),
                Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };
            printersHeaderPanel.Controls.Add(lblPrinters);

            // Printers Grid
            try
            {
                gridControlPrinters = new GridControl
                {
                    Location = new System.Drawing.Point(20, 135),
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
                // Filtre paneli için paint event'i
                gridControlPrinters.Paint += GridControl_Paint;
                
                this.Controls.Add(gridControlPrinters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Printers grid init error: {ex.Message}");
            }
            
            // Yazıcı Icon Paneli (Altta geniş bir satır - daha büyük ve kalın)
            printersIconPanel = new System.Windows.Forms.FlowLayoutPanel
            {
                Location = new System.Drawing.Point(20, 410),
                Size = new System.Drawing.Size(this.ClientSize.Width - 40, 110),
                Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right,
                AutoScroll = true,
                FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = System.Drawing.Color.White,
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                Padding = new System.Windows.Forms.Padding(10, 10, 10, 10)
            };
            this.Controls.Add(printersIconPanel);
            printerIconPanels = new System.Collections.Generic.Dictionary<int, System.Windows.Forms.Panel>();

            // Orders Grid Başlık Panel
            ordersHeaderPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(490, 100),
                Size = new System.Drawing.Size(450, 35),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left,
                BackColor = System.Drawing.Color.FromArgb(255, 152, 0)
            };
            this.Controls.Add(ordersHeaderPanel);

            lblOrders = new LabelControl
            {
                Text = "📦 SİPARİŞLER",
                Location = new System.Drawing.Point(10, 5),
                Size = new System.Drawing.Size(430, 25),
                Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };
            ordersHeaderPanel.Controls.Add(lblOrders);

            // Orders Grid
            try
            {
                gridControlOrders = new GridControl
                {
                    Location = new System.Drawing.Point(490, 135),
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
                
                this.Controls.Add(gridControlOrders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Orders grid init error: {ex.Message}");
            }

            // Jobs Grid Başlık Panel
            jobsHeaderPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(960, 100),
                Size = new System.Drawing.Size(450, 35),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right,
                BackColor = System.Drawing.Color.FromArgb(156, 39, 176)
            };
            this.Controls.Add(jobsHeaderPanel);

            lblJobs = new LabelControl
            {
                Text = "⚙️ YAZDIRMA İŞLERİ",
                Location = new System.Drawing.Point(10, 5),
                Size = new System.Drawing.Size(430, 25),
                Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };
            jobsHeaderPanel.Controls.Add(lblJobs);

            // Jobs Grid
            try
            {
                gridControlJobs = new GridControl
                {
                    Location = new System.Drawing.Point(960, 135),
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
                
                this.Controls.Add(gridControlJobs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Jobs grid init error: {ex.Message}");
            }

            SetupStatisticsPanel();
            SetupGridColumns();
        }

        private void SetupStatisticsPanel()
        {
            // İstatistikler Paneli
            statsPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(20, 495),
                Size = new System.Drawing.Size(this.ClientSize.Width - 40, 100),
                Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right,
                BackColor = System.Drawing.Color.White,
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            };
            this.Controls.Add(statsPanel);

            lblStats = new LabelControl
            {
                Text = "📊 İSTATİSTİKLER",
                Location = new System.Drawing.Point(10, 5),
                Size = new System.Drawing.Size(200, 25),
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(63, 81, 181)
            };
            statsPanel.Controls.Add(lblStats);

            // Toplam Yazıcı
            var lblTotalPrintersLabel = new LabelControl
            {
                Text = "Toplam Yazıcı:",
                Location = new System.Drawing.Point(20, 35),
                Size = new System.Drawing.Size(100, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(100, 100, 100)
            };
            statsPanel.Controls.Add(lblTotalPrintersLabel);

            lblTotalPrinters = new LabelControl
            {
                Text = "10",
                Location = new System.Drawing.Point(130, 35),
                Size = new System.Drawing.Size(50, 25),
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(63, 81, 181)
            };
            statsPanel.Controls.Add(lblTotalPrinters);

            // Aktif Yazıcı
            var lblActivePrintersLabel = new LabelControl
            {
                Text = "Aktif Yazıcı:",
                Location = new System.Drawing.Point(220, 35),
                Size = new System.Drawing.Size(100, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(100, 100, 100)
            };
            statsPanel.Controls.Add(lblActivePrintersLabel);

            lblActivePrinters = new LabelControl
            {
                Text = "0",
                Location = new System.Drawing.Point(330, 35),
                Size = new System.Drawing.Size(50, 25),
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(76, 175, 80)
            };
            statsPanel.Controls.Add(lblActivePrinters);

            // Toplam Sipariş
            var lblTotalOrdersLabel = new LabelControl
            {
                Text = "Toplam Sipariş:",
                Location = new System.Drawing.Point(420, 35),
                Size = new System.Drawing.Size(100, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(100, 100, 100)
            };
            statsPanel.Controls.Add(lblTotalOrdersLabel);

            lblTotalOrders = new LabelControl
            {
                Text = "0",
                Location = new System.Drawing.Point(530, 35),
                Size = new System.Drawing.Size(50, 25),
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(255, 152, 0)
            };
            statsPanel.Controls.Add(lblTotalOrders);

            // Bekleyen İşler
            var lblPendingJobsLabel = new LabelControl
            {
                Text = "Bekleyen İşler:",
                Location = new System.Drawing.Point(620, 35),
                Size = new System.Drawing.Size(100, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(100, 100, 100)
            };
            statsPanel.Controls.Add(lblPendingJobsLabel);

            lblPendingJobs = new LabelControl
            {
                Text = "0",
                Location = new System.Drawing.Point(730, 35),
                Size = new System.Drawing.Size(50, 25),
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(156, 39, 176)
            };
            statsPanel.Controls.Add(lblPendingJobs);

            // Toplam Tamamlanan İş
            var lblCompletedJobsLabel = new LabelControl
            {
                Text = "Tamamlanan İş:",
                Location = new System.Drawing.Point(20, 65),
                Size = new System.Drawing.Size(120, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(100, 100, 100),
                Name = "lblCompletedJobsLabel"
            };
            statsPanel.Controls.Add(lblCompletedJobsLabel);

            var lblCompletedJobs = new LabelControl
            {
                Text = "0",
                Location = new System.Drawing.Point(150, 65),
                Size = new System.Drawing.Size(50, 25),
                Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(76, 175, 80),
                Name = "lblCompletedJobs"
            };
            statsPanel.Controls.Add(lblCompletedJobs);
        }

        private void SetupGridColumns()
        {
            // Printers Grid Columns
            GridColumn colId = gridViewPrinters.Columns.AddField("Id");
            colId.Caption = "ID";
            colId.VisibleIndex = 0;
            colId.Width = 50;
            colId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colId.AppearanceCell.Options.UseForeColor = true;

            GridColumn colName = gridViewPrinters.Columns.AddField("Name");
            colName.Caption = "Yazıcı Adı";
            colName.VisibleIndex = 1;
            colName.Width = 120;
            colName.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colName.AppearanceCell.Options.UseForeColor = true;

            GridColumn colStatus = gridViewPrinters.Columns.AddField("Status");
            colStatus.Caption = "Durum";
            colStatus.VisibleIndex = 2;
            colStatus.Width = 120;
            colStatus.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colStatus.AppearanceCell.Options.UseForeColor = true;

            GridColumn colJob = gridViewPrinters.Columns.AddField("CurrentJobName");
            colJob.Caption = "Mevcut İş";
            colJob.VisibleIndex = 3;
            colJob.Width = 150;
            colJob.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJob.AppearanceCell.Options.UseForeColor = true;

            GridColumn colProgress = gridViewPrinters.Columns.AddField("Progress");
            colProgress.Caption = "İlerleme %";
            colProgress.VisibleIndex = 4;
            colProgress.Width = 90;
            colProgress.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colProgress.DisplayFormat.FormatString = "F1";
            colProgress.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colProgress.AppearanceCell.Options.UseForeColor = true;

            GridColumn colFilament = gridViewPrinters.Columns.AddField("FilamentRemaining");
            colFilament.Caption = "Filament %";
            colFilament.VisibleIndex = 5;
            colFilament.Width = 90;
            colFilament.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colFilament.DisplayFormat.FormatString = "F1";
            colFilament.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colFilament.AppearanceCell.Options.UseForeColor = true;

            GridColumn colFilamentType = gridViewPrinters.Columns.AddField("FilamentType");
            colFilamentType.Caption = "Filament Tipi";
            colFilamentType.VisibleIndex = 6;
            colFilamentType.Width = 100;
            colFilamentType.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colFilamentType.AppearanceCell.Options.UseForeColor = true;

            gridViewPrinters.OptionsView.ShowGroupPanel = false;
            gridViewPrinters.OptionsView.ShowIndicator = true;
            gridViewPrinters.OptionsView.ColumnAutoWidth = false;
            gridViewPrinters.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
            gridViewPrinters.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;
            
            // Grid genişliğini ayarla
            gridControlPrinters.Size = new System.Drawing.Size(450, 320);

            // Orders Grid Columns
            GridColumn colOrderId = gridViewOrders.Columns.AddField("Id");
            colOrderId.Caption = "ID";
            colOrderId.VisibleIndex = 0;
            colOrderId.Width = 50;
            colOrderId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colOrderId.AppearanceCell.Options.UseForeColor = true;

            GridColumn colOrderNo = gridViewOrders.Columns.AddField("OrderNumber");
            colOrderNo.Caption = "Sipariş No";
            colOrderNo.VisibleIndex = 1;
            colOrderNo.Width = 150;
            colOrderNo.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colOrderNo.AppearanceCell.Options.UseForeColor = true;

            GridColumn colCustomer = gridViewOrders.Columns.AddField("CustomerName");
            colCustomer.Caption = "Müşteri";
            colCustomer.VisibleIndex = 2;
            colCustomer.Width = 120;
            colCustomer.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colCustomer.AppearanceCell.Options.UseForeColor = true;

            GridColumn colDate = gridViewOrders.Columns.AddField("OrderDate");
            colDate.Caption = "Tarih";
            colDate.VisibleIndex = 3;
            colDate.Width = 120;
            colDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colDate.DisplayFormat.FormatString = "dd.MM.yyyy HH:mm";
            colDate.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colDate.AppearanceCell.Options.UseForeColor = true;

            GridColumn colOrderStatus = gridViewOrders.Columns.AddField("Status");
            colOrderStatus.Caption = "Durum";
            colOrderStatus.VisibleIndex = 4;
            colOrderStatus.Width = 100;
            colOrderStatus.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colOrderStatus.AppearanceCell.Options.UseForeColor = true;

            GridColumn colTotalPrice = gridViewOrders.Columns.AddField("TotalPrice");
            colTotalPrice.Caption = "Toplam Fiyat";
            colTotalPrice.VisibleIndex = 5;
            colTotalPrice.Width = 100;
            colTotalPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colTotalPrice.DisplayFormat.FormatString = "C2";
            colTotalPrice.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colTotalPrice.AppearanceCell.Options.UseForeColor = true;

            gridViewOrders.OptionsView.ShowGroupPanel = false;
            gridViewOrders.OptionsView.ShowIndicator = true;
            gridViewOrders.OptionsView.ColumnAutoWidth = false;
            gridViewOrders.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
            gridViewOrders.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;

            // Jobs Grid Columns
            GridColumn colJobId = gridViewJobs.Columns.AddField("Id");
            colJobId.Caption = "İş ID";
            colJobId.VisibleIndex = 0;
            colJobId.Width = 60;
            colJobId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJobId.AppearanceCell.Options.UseForeColor = true;

            GridColumn colModel = gridViewJobs.Columns.AddField("ModelFileName");
            colModel.Caption = "Model Dosyası";
            colModel.VisibleIndex = 1;
            colModel.Width = 150;
            colModel.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colModel.AppearanceCell.Options.UseForeColor = true;

            GridColumn colPrinterId = gridViewJobs.Columns.AddField("PrinterId");
            colPrinterId.Caption = "Yazıcı ID";
            colPrinterId.VisibleIndex = 2;
            colPrinterId.Width = 80;
            colPrinterId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colPrinterId.AppearanceCell.Options.UseForeColor = true;

            GridColumn colJobStatus = gridViewJobs.Columns.AddField("Status");
            colJobStatus.Caption = "Durum";
            colJobStatus.VisibleIndex = 3;
            colJobStatus.Width = 100;
            colJobStatus.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJobStatus.AppearanceCell.Options.UseForeColor = true;

            GridColumn colJobProgress = gridViewJobs.Columns.AddField("Progress");
            colJobProgress.Caption = "İlerleme %";
            colJobProgress.VisibleIndex = 4;
            colJobProgress.Width = 100;
            colJobProgress.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colJobProgress.DisplayFormat.FormatString = "F1";
            colJobProgress.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJobProgress.AppearanceCell.Options.UseForeColor = true;

            GridColumn colMaterial = gridViewJobs.Columns.AddField("Material");
            colMaterial.Caption = "Malzeme";
            colMaterial.VisibleIndex = 5;
            colMaterial.Width = 80;
            colMaterial.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colMaterial.AppearanceCell.Options.UseForeColor = true;

            gridViewJobs.OptionsView.ShowGroupPanel = false;
            gridViewJobs.OptionsView.ShowIndicator = true;
            gridViewJobs.OptionsView.ColumnAutoWidth = false;
            gridViewJobs.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
            gridViewJobs.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;
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
                    lblStatus.Text = $"✓ İş tamamlandı: {e.Job.ModelFileName}";
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(129, 199, 132);
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
                gridViewPrinters.BeginUpdate();
                gridControlPrinters.DataSource = _printerService.GetAllPrinters();
                gridViewPrinters.EndUpdate();
                
                // Yazıcı iconlarını güncelle
                UpdatePrinterIcons();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Printers grid error: {ex.Message}");
            }

            try
            {
                gridViewOrders.BeginUpdate();
                gridControlOrders.DataSource = _orderService.GetAllOrders();
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

        private void BtnToggleTheme_Click(object sender, EventArgs e)
        {
            _currentTheme = _currentTheme == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
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
            // Form arka planı
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);

            // Başlık paneli
            if (titlePanel != null)
            {
                titlePanel.BackColor = System.Drawing.Color.FromArgb(25, 25, 25);
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
                btnAddPrinter.Appearance.BackColor = System.Drawing.Color.FromArgb(40, 120, 200);
                btnAddPrinter.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(50, 130, 210);
            }

            // Header panelleri (daha koyu tonlar)
            if (printersHeaderPanel != null)
                printersHeaderPanel.BackColor = System.Drawing.Color.FromArgb(40, 50, 120);
            if (ordersHeaderPanel != null)
                ordersHeaderPanel.BackColor = System.Drawing.Color.FromArgb(150, 90, 0);
            if (jobsHeaderPanel != null)
                jobsHeaderPanel.BackColor = System.Drawing.Color.FromArgb(100, 20, 120);

            // İstatistikler paneli
            if (statsPanel != null)
            {
                statsPanel.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
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
            // Form arka planı
            this.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);

            // Başlık paneli
            if (titlePanel != null)
            {
                titlePanel.BackColor = System.Drawing.Color.FromArgb(30, 136, 229);
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
                btnAddPrinter.Appearance.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
                btnAddPrinter.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(30, 136, 229);
            }

            // Header panelleri
            if (printersHeaderPanel != null)
                printersHeaderPanel.BackColor = System.Drawing.Color.FromArgb(63, 81, 181);
            if (ordersHeaderPanel != null)
                ordersHeaderPanel.BackColor = System.Drawing.Color.FromArgb(255, 152, 0);
            if (jobsHeaderPanel != null)
                jobsHeaderPanel.BackColor = System.Drawing.Color.FromArgb(156, 39, 176);

            // İstatistikler paneli
            if (statsPanel != null)
            {
                statsPanel.BackColor = System.Drawing.Color.White;
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
                gridView.GridControl.LookAndFeel.UseDefaultLookAndFeel = false;
                gridView.GridControl.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
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
                gridView.GridControl.LookAndFeel.UseDefaultLookAndFeel = false;
                gridView.GridControl.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
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
                    label != lblTotalOrders && label != lblPendingJobs && label.Name != "lblCompletedJobs")
                {
                    label.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
                }
            }

            var completedLabel = statsPanel?.Controls.OfType<LabelControl>()
                .FirstOrDefault(l => l.Name == "lblCompletedJobs");
            if (completedLabel != null)
                completedLabel.ForeColor = System.Drawing.Color.FromArgb(129, 199, 132);
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
                    label != lblTotalOrders && label != lblPendingJobs && label.Name != "lblCompletedJobs")
                {
                    label.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
                }
            }

            var completedLabel = statsPanel?.Controls.OfType<LabelControl>()
                .FirstOrDefault(l => l.Name == "lblCompletedJobs");
            if (completedLabel != null)
                completedLabel.ForeColor = System.Drawing.Color.FromArgb(76, 175, 80);
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
                    // Yeni panel oluştur (daha yüksek ve geniş - yazıların tam görünmesi için)
                    isNew = true;
                    iconPanel = new System.Windows.Forms.Panel
                    {
                        Size = new System.Drawing.Size(140, 90),
                        Margin = new System.Windows.Forms.Padding(8, 5, 8, 5),
                        BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                        BackColor = _currentTheme == ThemeMode.Dark ? 
                            System.Drawing.Color.FromArgb(40, 40, 40) : 
                            System.Drawing.Color.White
                    };
                    printerIconPanels[printer.Id] = iconPanel;
                }

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
                switch (printer.Status)
                {
                    case PrinterStatus.Printing:
                        statusText = $"Yazdırıyor %{printer.Progress:F0}";
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

                if (isNew)
                {
                    // Yeni panel için kontrolleri oluştur (ikon üstte, yazılar altta - dikey yerleşim)
                    var iconLabel = new LabelControl
                    {
                        Text = "🖨️",
                        Location = new System.Drawing.Point(50, 5),
                        Size = new System.Drawing.Size(40, 40),
                        Font = new System.Drawing.Font("Segoe UI", 28F),
                        ForeColor = iconColor,
                        Name = "iconLabel"
                    };
                    iconLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    iconLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                    iconPanel.Controls.Add(iconLabel);

                    var nameLabel = new LabelControl
                    {
                        Text = printer.Name,
                        Location = new System.Drawing.Point(5, 50),
                        Size = new System.Drawing.Size(130, 18),
                        Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                        ForeColor = _currentTheme == ThemeMode.Dark ? 
                            System.Drawing.Color.FromArgb(230, 230, 230) : 
                            System.Drawing.Color.Black,
                        Name = "nameLabel"
                    };
                    nameLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    iconPanel.Controls.Add(nameLabel);

                    var statusLabel = new LabelControl
                    {
                        Text = statusText,
                        Location = new System.Drawing.Point(5, 70),
                        Size = new System.Drawing.Size(130, 18),
                        Font = new System.Drawing.Font("Segoe UI", 8F),
                        ForeColor = iconColor,
                        Name = "statusLabel"
                    };
                    statusLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    statusLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
                    iconPanel.Controls.Add(statusLabel);

                    printersIconPanel.Controls.Add(iconPanel);
                }
                else
                {
                    // Mevcut panelin boyutunu güncelle (yazıların tam görünmesi için)
                    if (iconPanel.Height < 90 || iconPanel.Width < 140)
                    {
                        iconPanel.Size = new System.Drawing.Size(140, 90);
                        
                        // Mevcut kontrollerin konumlarını güncelle (ikon üstte, yazılar altta)
                        var iconLabel = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "iconLabel");
                        if (iconLabel != null)
                        {
                            iconLabel.Location = new System.Drawing.Point(50, 5);
                            iconLabel.ForeColor = iconColor;
                        }

                        var nameLabel = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "nameLabel");
                        if (nameLabel != null)
                        {
                            nameLabel.Location = new System.Drawing.Point(5, 50);
                            nameLabel.Size = new System.Drawing.Size(130, 18);
                            nameLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                            nameLabel.Text = printer.Name;
                            nameLabel.ForeColor = _currentTheme == ThemeMode.Dark ? 
                                System.Drawing.Color.FromArgb(230, 230, 230) : 
                                System.Drawing.Color.Black;
                        }

                        var statusLabel = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "statusLabel");
                        if (statusLabel != null)
                        {
                            statusLabel.Location = new System.Drawing.Point(5, 70);
                            statusLabel.Size = new System.Drawing.Size(130, 18);
                            statusLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                            statusLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
                            statusLabel.Text = statusText;
                            statusLabel.ForeColor = iconColor;
                        }
                    }
                    else
                    {
                        // Mevcut kontrolleri güncelle (yanıp sönmeyi önlemek için)
                        var iconLabel = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "iconLabel");
                        if (iconLabel != null)
                        {
                            iconLabel.ForeColor = iconColor;
                        }

                        var nameLabel = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "nameLabel");
                        if (nameLabel != null)
                        {
                            nameLabel.Text = printer.Name;
                            nameLabel.ForeColor = _currentTheme == ThemeMode.Dark ? 
                                System.Drawing.Color.FromArgb(230, 230, 230) : 
                                System.Drawing.Color.Black;
                            nameLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                        }

                        var statusLabel = iconPanel.Controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "statusLabel");
                        if (statusLabel != null)
                        {
                            statusLabel.Text = statusText;
                            statusLabel.ForeColor = iconColor;
                            statusLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                        }
                    }

                    // Panel arka plan rengini güncelle
                    iconPanel.BackColor = _currentTheme == ThemeMode.Dark ? 
                        System.Drawing.Color.FromArgb(40, 40, 40) : 
                        System.Drawing.Color.White;
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

                        // Yazıcı yazdırma yapıyorsa uyarı ver
                        if (printer.Status == PrinterStatus.Printing)
                        {
                            XtraMessageBox.Show(
                                $"Yazıcı şu anda yazdırma yapıyor!\n\n" +
                                $"Yazıcı: {printer.Name}\n" +
                                $"Mevcut İş: {printer.CurrentJobName}\n\n" +
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
            var titlePanel = this.Controls.OfType<System.Windows.Forms.Panel>()
                .FirstOrDefault(p => p.BackColor == System.Drawing.Color.FromArgb(30, 136, 229));
            if (titlePanel != null)
            {
                titlePanel.Width = this.ClientSize.Width;
            }

            // Buton konumlarını güncelle
            if (btnSimulateOrder != null && btnAddPrinter != null && btnToggleTheme != null && titlePanel != null)
            {
                btnToggleTheme.Left = titlePanel.Width - btnToggleTheme.Width - 20;
                btnAddPrinter.Left = btnToggleTheme.Left - btnAddPrinter.Width - 10;
                btnSimulateOrder.Left = btnAddPrinter.Left - btnSimulateOrder.Width - 10;
            }

            // İstatistikler panelini bul
            var statsPanel = this.Controls.OfType<System.Windows.Forms.Panel>()
                .FirstOrDefault(p => p.Controls.OfType<LabelControl>().Any(l => l.Text.Contains("İSTATİSTİKLER")));
            
            // Grid'lerin genişliğini ayarla
            if (gridControlPrinters != null && gridControlOrders != null && gridControlJobs != null)
            {
                int availableWidth = this.ClientSize.Width - 60; // 20px margin her iki tarafta
                int gridWidth = availableWidth / 3;
                int spacing = 20;

                gridControlPrinters.Width = gridWidth;
                gridControlOrders.Left = gridControlPrinters.Right + spacing;
                gridControlOrders.Width = gridWidth;
                gridControlJobs.Left = gridControlOrders.Right + spacing;
                gridControlJobs.Width = this.ClientSize.Width - gridControlJobs.Left - 20;

                // Header panellerini güncelle
                if (printersHeaderPanel != null)
                {
                    printersHeaderPanel.Width = gridControlPrinters.Width;
                }

                if (ordersHeaderPanel != null)
                {
                    ordersHeaderPanel.Left = gridControlOrders.Left;
                    ordersHeaderPanel.Width = gridControlOrders.Width;
                }

                if (jobsHeaderPanel != null)
                {
                    jobsHeaderPanel.Left = gridControlJobs.Left;
                    jobsHeaderPanel.Width = gridControlJobs.Width;
                }
            }

            // Yazıcı icon paneli (altta geniş satır - daha büyük)
            if (printersIconPanel != null)
            {
                int iconPanelTop = statsPanel != null ? statsPanel.Top - 120 : this.ClientSize.Height - 220;
                printersIconPanel.Left = 20;
                printersIconPanel.Width = this.ClientSize.Width - 40;
                printersIconPanel.Top = iconPanelTop;
                printersIconPanel.Height = 110;
            }

            // Grid yüksekliklerini ayarla
            if (gridControlPrinters != null)
            {
                int gridTop = 135;
                int iconPanelTop = printersIconPanel != null ? printersIconPanel.Top : this.ClientSize.Height - 220;
                int gridHeight = iconPanelTop - gridTop - 20;
                
                if (gridHeight > 100)
                {
                    gridControlPrinters.Height = gridHeight;
                    if (gridControlOrders != null) gridControlOrders.Height = gridHeight;
                    if (gridControlJobs != null) gridControlJobs.Height = gridHeight;
                }
            }

            // İstatistikler panelini güncelle
            if (statsPanel != null)
            {
                statsPanel.Width = this.ClientSize.Width - 40;
            }
        }

        protected override void OnFormClosing(System.Windows.Forms.FormClosingEventArgs e)
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
