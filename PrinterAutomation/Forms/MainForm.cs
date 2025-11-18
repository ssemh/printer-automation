using System;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.Utils;
using PrinterAutomation.Models;
using PrinterAutomation.Services;

namespace PrinterAutomation.Forms
{
    public class MainForm : System.Windows.Forms.Form
    {
        private readonly PrinterService _printerService;
        private readonly OrderService _orderService;
        private readonly JobAssignmentService _jobAssignmentService;
        private System.Windows.Forms.Timer _refreshTimer;

        private GridControl gridControlPrinters;
        private GridView gridViewPrinters;
        private GridControl gridControlOrders;
        private GridView gridViewOrders;
        private GridControl gridControlJobs;
        private GridView gridViewJobs;
        private SimpleButton btnSimulateOrder;
        private LabelControl lblStatus;
        private LabelControl lblTitle;
        private LabelControl lblPrinters;
        private LabelControl lblOrders;
        private LabelControl lblJobs;

        public MainForm()
        {
            _printerService = new PrinterService();
            _orderService = new OrderService();
            _jobAssignmentService = new JobAssignmentService(_printerService, _orderService);
            
            InitializeComponent();
            this.Shown += MainForm_Shown;
            SetupEventHandlers();
            StartRefreshTimer();
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
            this.Size = new System.Drawing.Size(1500, 900);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            this.MinimumSize = new System.Drawing.Size(1200, 700);

            // Başlık Panel (Gradient efekti için)
            var titlePanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(1500, 80),
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

            // Simulate Order Button (Başlık panelinde)
            btnSimulateOrder = new SimpleButton
            {
                Text = "➕ Yeni Sipariş Simüle Et",
                Location = new System.Drawing.Point(1200, 20),
                Size = new System.Drawing.Size(270, 45),
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

            // Printers Grid Başlık Panel
            var printersHeaderPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(20, 100),
                Size = new System.Drawing.Size(450, 35),
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
                    Size = new System.Drawing.Size(450, 345),
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
                
                this.Controls.Add(gridControlPrinters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Printers grid init error: {ex.Message}");
            }

            // Orders Grid Başlık Panel
            var ordersHeaderPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(490, 100),
                Size = new System.Drawing.Size(450, 35),
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
                    Size = new System.Drawing.Size(450, 345),
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
                
                this.Controls.Add(gridControlOrders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Orders grid init error: {ex.Message}");
            }

            // Jobs Grid Başlık Panel
            var jobsHeaderPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(960, 100),
                Size = new System.Drawing.Size(450, 35),
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
                    Size = new System.Drawing.Size(450, 345),
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
                
                this.Controls.Add(gridControlJobs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Jobs grid init error: {ex.Message}");
            }

            SetupGridColumns();
        }

        private void SetupGridColumns()
        {
            // Printers Grid Columns
            var colId = gridViewPrinters.Columns.AddField("Id");
            colId.Caption = "ID";
            colId.VisibleIndex = 0;
            colId.Width = 50;
            colId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colId.AppearanceCell.Options.UseForeColor = true;

            var colName = gridViewPrinters.Columns.AddField("Name");
            colName.Caption = "Yazıcı Adı";
            colName.VisibleIndex = 1;
            colName.Width = 120;
            colName.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colName.AppearanceCell.Options.UseForeColor = true;

            var colStatus = gridViewPrinters.Columns.AddField("Status");
            colStatus.Caption = "Durum";
            colStatus.VisibleIndex = 2;
            colStatus.Width = 100;
            colStatus.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colStatus.AppearanceCell.Options.UseForeColor = true;

            var colJob = gridViewPrinters.Columns.AddField("CurrentJobName");
            colJob.Caption = "Mevcut İş";
            colJob.VisibleIndex = 3;
            colJob.Width = 150;
            colJob.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJob.AppearanceCell.Options.UseForeColor = true;

            var colProgress = gridViewPrinters.Columns.AddField("Progress");
            colProgress.Caption = "İlerleme %";
            colProgress.VisibleIndex = 4;
            colProgress.Width = 100;
            colProgress.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colProgress.DisplayFormat.FormatString = "F1";
            colProgress.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colProgress.AppearanceCell.Options.UseForeColor = true;

            gridViewPrinters.OptionsView.ShowGroupPanel = false;
            gridViewPrinters.OptionsView.ShowIndicator = true;
            gridViewPrinters.OptionsView.ColumnAutoWidth = false;
            gridViewPrinters.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
            gridViewPrinters.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;

            // Orders Grid Columns
            var colOrderId = gridViewOrders.Columns.AddField("Id");
            colOrderId.Caption = "ID";
            colOrderId.VisibleIndex = 0;
            colOrderId.Width = 50;
            colOrderId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colOrderId.AppearanceCell.Options.UseForeColor = true;

            var colOrderNo = gridViewOrders.Columns.AddField("OrderNumber");
            colOrderNo.Caption = "Sipariş No";
            colOrderNo.VisibleIndex = 1;
            colOrderNo.Width = 150;
            colOrderNo.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colOrderNo.AppearanceCell.Options.UseForeColor = true;

            var colCustomer = gridViewOrders.Columns.AddField("CustomerName");
            colCustomer.Caption = "Müşteri";
            colCustomer.VisibleIndex = 2;
            colCustomer.Width = 120;
            colCustomer.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colCustomer.AppearanceCell.Options.UseForeColor = true;

            var colDate = gridViewOrders.Columns.AddField("OrderDate");
            colDate.Caption = "Tarih";
            colDate.VisibleIndex = 3;
            colDate.Width = 120;
            colDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colDate.DisplayFormat.FormatString = "dd.MM.yyyy HH:mm";
            colDate.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colDate.AppearanceCell.Options.UseForeColor = true;

            var colOrderStatus = gridViewOrders.Columns.AddField("Status");
            colOrderStatus.Caption = "Durum";
            colOrderStatus.VisibleIndex = 4;
            colOrderStatus.Width = 100;
            colOrderStatus.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colOrderStatus.AppearanceCell.Options.UseForeColor = true;

            gridViewOrders.OptionsView.ShowGroupPanel = false;
            gridViewOrders.OptionsView.ShowIndicator = true;
            gridViewOrders.OptionsView.ColumnAutoWidth = false;
            gridViewOrders.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
            gridViewOrders.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;

            // Jobs Grid Columns
            var colJobId = gridViewJobs.Columns.AddField("Id");
            colJobId.Caption = "İş ID";
            colJobId.VisibleIndex = 0;
            colJobId.Width = 60;
            colJobId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJobId.AppearanceCell.Options.UseForeColor = true;

            var colModel = gridViewJobs.Columns.AddField("ModelFileName");
            colModel.Caption = "Model Dosyası";
            colModel.VisibleIndex = 1;
            colModel.Width = 150;
            colModel.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colModel.AppearanceCell.Options.UseForeColor = true;

            var colPrinterId = gridViewJobs.Columns.AddField("PrinterId");
            colPrinterId.Caption = "Yazıcı ID";
            colPrinterId.VisibleIndex = 2;
            colPrinterId.Width = 80;
            colPrinterId.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colPrinterId.AppearanceCell.Options.UseForeColor = true;

            var colJobStatus = gridViewJobs.Columns.AddField("Status");
            colJobStatus.Caption = "Durum";
            colJobStatus.VisibleIndex = 3;
            colJobStatus.Width = 100;
            colJobStatus.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJobStatus.AppearanceCell.Options.UseForeColor = true;

            var colJobProgress = gridViewJobs.Columns.AddField("Progress");
            colJobProgress.Caption = "İlerleme %";
            colJobProgress.VisibleIndex = 4;
            colJobProgress.Width = 100;
            colJobProgress.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colJobProgress.DisplayFormat.FormatString = "F1";
            colJobProgress.AppearanceCell.ForeColor = System.Drawing.Color.Black;
            colJobProgress.AppearanceCell.Options.UseForeColor = true;

            var colMaterial = gridViewJobs.Columns.AddField("Material");
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
                // Renk ayarlarını zorla uygula
                gridViewPrinters.Appearance.Row.ForeColor = System.Drawing.Color.Black;
                gridViewPrinters.Appearance.Row.Options.UseForeColor = true;
                gridViewPrinters.EndUpdate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Printers grid error: {ex.Message}");
            }

            try
            {
                gridViewOrders.BeginUpdate();
                gridControlOrders.DataSource = _orderService.GetAllOrders();
                // Renk ayarlarını zorla uygula
                gridViewOrders.Appearance.Row.ForeColor = System.Drawing.Color.Black;
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
                // Renk ayarlarını zorla uygula
                gridViewJobs.Appearance.Row.ForeColor = System.Drawing.Color.Black;
                gridViewJobs.Appearance.Row.Options.UseForeColor = true;
                gridViewJobs.EndUpdate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Jobs grid error: {ex.Message}");
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
            lblStatus.Text = $"✓ Yeni sipariş alındı: {order.OrderNumber}";
            lblStatus.ForeColor = System.Drawing.Color.FromArgb(129, 199, 132);
            
            XtraMessageBox.Show(
                $"Yeni sipariş oluşturuldu!\n\n" +
                $"Sipariş No: {order.OrderNumber}\n" +
                $"Müşteri: {order.CustomerName}\n" +
                $"Ürün Sayısı: {order.Items.Count}",
                "Sipariş Alındı",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }

        private void GridViewPrinters_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(33, 33, 33);
            e.Appearance.BackColor = e.RowHandle % 2 == 0 ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(249, 250, 252);
            e.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        }

        private void GridViewOrders_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(33, 33, 33);
            e.Appearance.BackColor = e.RowHandle % 2 == 0 ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(249, 250, 252);
            e.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
        }

        private void GridViewJobs_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(33, 33, 33);
            e.Appearance.BackColor = e.RowHandle % 2 == 0 ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(249, 250, 252);
            e.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
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

