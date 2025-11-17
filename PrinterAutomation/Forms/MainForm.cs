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
            this.Size = new System.Drawing.Size(1400, 800);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            // Status Label
            lblStatus = new LabelControl
            {
                Text = "Sistem Hazır",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(200, 20)
            };
            this.Controls.Add(lblStatus);

            // Simulate Order Button
            btnSimulateOrder = new SimpleButton
            {
                Text = "Yeni Sipariş Simüle Et",
                Location = new System.Drawing.Point(220, 8),
                Size = new System.Drawing.Size(200, 30)
            };
            btnSimulateOrder.Click += BtnSimulateOrder_Click;
            this.Controls.Add(btnSimulateOrder);

            // Printers Grid
            try
            {
                gridControlPrinters = new GridControl
                {
                    Location = new System.Drawing.Point(10, 50),
                    Size = new System.Drawing.Size(450, 300),
                    Visible = false // Başlangıçta gizli
                };
                gridViewPrinters = new GridView(gridControlPrinters);
                gridControlPrinters.MainView = gridViewPrinters;
                gridControlPrinters.UseEmbeddedNavigator = false;
                gridViewPrinters.OptionsBehavior.Editable = false;
                this.Controls.Add(gridControlPrinters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Printers grid init error: {ex.Message}");
            }

            // Orders Grid
            try
            {
                gridControlOrders = new GridControl
                {
                    Location = new System.Drawing.Point(470, 50),
                    Size = new System.Drawing.Size(450, 300),
                    Visible = false // Başlangıçta gizli
                };
                gridViewOrders = new GridView(gridControlOrders);
                gridControlOrders.MainView = gridViewOrders;
                gridControlOrders.UseEmbeddedNavigator = false;
                gridViewOrders.OptionsBehavior.Editable = false;
                this.Controls.Add(gridControlOrders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Orders grid init error: {ex.Message}");
            }

            // Jobs Grid
            try
            {
                gridControlJobs = new GridControl
                {
                    Location = new System.Drawing.Point(930, 50),
                    Size = new System.Drawing.Size(450, 300),
                    Visible = false // Başlangıçta gizli
                };
                gridViewJobs = new GridView(gridControlJobs);
                gridControlJobs.MainView = gridViewJobs;
                gridControlJobs.UseEmbeddedNavigator = false;
                gridViewJobs.OptionsBehavior.Editable = false;
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

            var colName = gridViewPrinters.Columns.AddField("Name");
            colName.Caption = "Yazıcı Adı";
            colName.VisibleIndex = 1;
            colName.Width = 120;

            var colStatus = gridViewPrinters.Columns.AddField("Status");
            colStatus.Caption = "Durum";
            colStatus.VisibleIndex = 2;
            colStatus.Width = 100;

            var colJob = gridViewPrinters.Columns.AddField("CurrentJobName");
            colJob.Caption = "Mevcut İş";
            colJob.VisibleIndex = 3;
            colJob.Width = 150;

            var colProgress = gridViewPrinters.Columns.AddField("Progress");
            colProgress.Caption = "İlerleme %";
            colProgress.VisibleIndex = 4;
            colProgress.Width = 100;
            colProgress.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colProgress.DisplayFormat.FormatString = "F1";

            gridViewPrinters.OptionsView.ShowGroupPanel = false;
            gridViewPrinters.OptionsView.ShowIndicator = false;
            gridViewPrinters.OptionsView.ColumnAutoWidth = false;

            // Orders Grid Columns
            var colOrderId = gridViewOrders.Columns.AddField("Id");
            colOrderId.Caption = "ID";
            colOrderId.VisibleIndex = 0;
            colOrderId.Width = 50;

            var colOrderNo = gridViewOrders.Columns.AddField("OrderNumber");
            colOrderNo.Caption = "Sipariş No";
            colOrderNo.VisibleIndex = 1;
            colOrderNo.Width = 150;

            var colCustomer = gridViewOrders.Columns.AddField("CustomerName");
            colCustomer.Caption = "Müşteri";
            colCustomer.VisibleIndex = 2;
            colCustomer.Width = 120;

            var colDate = gridViewOrders.Columns.AddField("OrderDate");
            colDate.Caption = "Tarih";
            colDate.VisibleIndex = 3;
            colDate.Width = 120;
            colDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colDate.DisplayFormat.FormatString = "dd.MM.yyyy HH:mm";

            var colOrderStatus = gridViewOrders.Columns.AddField("Status");
            colOrderStatus.Caption = "Durum";
            colOrderStatus.VisibleIndex = 4;
            colOrderStatus.Width = 100;

            gridViewOrders.OptionsView.ShowGroupPanel = false;
            gridViewOrders.OptionsView.ShowIndicator = false;
            gridViewOrders.OptionsView.ColumnAutoWidth = false;

            // Jobs Grid Columns
            var colJobId = gridViewJobs.Columns.AddField("Id");
            colJobId.Caption = "İş ID";
            colJobId.VisibleIndex = 0;
            colJobId.Width = 60;

            var colModel = gridViewJobs.Columns.AddField("ModelFileName");
            colModel.Caption = "Model Dosyası";
            colModel.VisibleIndex = 1;
            colModel.Width = 150;

            var colPrinterId = gridViewJobs.Columns.AddField("PrinterId");
            colPrinterId.Caption = "Yazıcı ID";
            colPrinterId.VisibleIndex = 2;
            colPrinterId.Width = 80;

            var colJobStatus = gridViewJobs.Columns.AddField("Status");
            colJobStatus.Caption = "Durum";
            colJobStatus.VisibleIndex = 3;
            colJobStatus.Width = 100;

            var colJobProgress = gridViewJobs.Columns.AddField("Progress");
            colJobProgress.Caption = "İlerleme %";
            colJobProgress.VisibleIndex = 4;
            colJobProgress.Width = 100;
            colJobProgress.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colJobProgress.DisplayFormat.FormatString = "F1";

            var colMaterial = gridViewJobs.Columns.AddField("Material");
            colMaterial.Caption = "Malzeme";
            colMaterial.VisibleIndex = 5;
            colMaterial.Width = 80;

            gridViewJobs.OptionsView.ShowGroupPanel = false;
            gridViewJobs.OptionsView.ShowIndicator = false;
            gridViewJobs.OptionsView.ColumnAutoWidth = false;
        }

        private void SetupEventHandlers()
        {
            _jobAssignmentService.JobAssigned += (s, e) =>
            {
                this.Invoke(new Action(() =>
                {
                    RefreshData();
                    lblStatus.Text = $"İş atandı: {e.Job.ModelFileName} -> Yazıcı {e.Job.PrinterId}";
                }));
            };

            _jobAssignmentService.JobCompleted += (s, e) =>
            {
                this.Invoke(new Action(() =>
                {
                    RefreshData();
                    lblStatus.Text = $"İş tamamlandı: {e.Job.ModelFileName}";
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
            lblStatus.Text = $"Yeni sipariş alındı: {order.OrderNumber}";
            
            XtraMessageBox.Show(
                $"Yeni sipariş oluşturuldu!\n\n" +
                $"Sipariş No: {order.OrderNumber}\n" +
                $"Müşteri: {order.CustomerName}\n" +
                $"Ürün Sayısı: {order.Items.Count}",
                "Sipariş Alındı",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
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

