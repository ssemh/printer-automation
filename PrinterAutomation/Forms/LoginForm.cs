using System;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using PrinterAutomation.Services;
using MongoDB.Driver;
using MongoDB.Bson;

namespace PrinterAutomation.Forms
{
    public partial class LoginForm : System.Windows.Forms.Form
    {
        private TextEdit txtPassword;
        private SimpleButton btnLogin;
        private LabelControl lblTitle;
        private LabelControl lblPassword;
        private LabelControl lblIcon;
        private MongoDbService _mongoDbService;
        private const string CORRECT_PASSWORD = "324434";

        public LoginForm()
        {
            InitializeComponent();
            SetupMongoDb();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form ayarları
            this.Text = "Giriş";
            this.Size = new System.Drawing.Size(450, 530);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);

            // Kilit ikonu (yukarıda, merkezde - üstten yeterli boşluk ile)
            lblIcon = new LabelControl
            {
                Text = "🔐",
                Location = new System.Drawing.Point(0, 60),
                Size = new System.Drawing.Size(450, 130),
                Font = new System.Drawing.Font("Segoe UI", 72F, System.Drawing.FontStyle.Regular),
                ForeColor = System.Drawing.Color.FromArgb(33, 150, 243),
                AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None
            };
            lblIcon.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblIcon.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.Controls.Add(lblIcon);

            // Başlık (kilit ikonunun tam altında, merkezde)
            lblTitle = new LabelControl
            {
                Text = "3D Yazıcı Otomasyon Sistemi",
                Location = new System.Drawing.Point(0, 195),
                Size = new System.Drawing.Size(450, 35),
                Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(33, 33, 33),
                AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None
            };
            lblTitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblTitle.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.Controls.Add(lblTitle);

            // Şifre etiketi
            lblPassword = new LabelControl
            {
                Text = "Şifre:",
                Location = new System.Drawing.Point(75, 245),
                Size = new System.Drawing.Size(100, 20),
                Font = new System.Drawing.Font("Segoe UI", 10F),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66)
            };
            this.Controls.Add(lblPassword);

            // Şifre girişi
            txtPassword = new TextEdit
            {
                Location = new System.Drawing.Point(75, 270),
                Size = new System.Drawing.Size(300, 30),
                Font = new System.Drawing.Font("Segoe UI", 11F)
            };
            txtPassword.Properties.PasswordChar = '●';
            txtPassword.Properties.UseSystemPasswordChar = true;
            txtPassword.KeyDown += TxtPassword_KeyDown;
            this.Controls.Add(txtPassword);

            // Giriş butonu
            btnLogin = new SimpleButton
            {
                Text = "Giriş Yap",
                Location = new System.Drawing.Point(75, 325),
                Size = new System.Drawing.Size(300, 45),
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold)
            };
            btnLogin.Appearance.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
            btnLogin.Appearance.ForeColor = System.Drawing.Color.White;
            btnLogin.Appearance.Options.UseBackColor = true;
            btnLogin.Appearance.Options.UseForeColor = true;
            btnLogin.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(25, 118, 210);
            btnLogin.AppearanceHovered.Options.UseBackColor = true;
            btnLogin.AppearancePressed.BackColor = System.Drawing.Color.FromArgb(21, 101, 192);
            btnLogin.AppearancePressed.Options.UseBackColor = true;
            btnLogin.LookAndFeel.UseDefaultLookAndFeel = false;
            btnLogin.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);

            this.ResumeLayout(false);
        }

        private void SetupMongoDb()
        {
            try
            {
                _mongoDbService = new MongoDbService();
                System.Diagnostics.Debug.WriteLine("[LoginForm] MongoDB bağlantısı başarılı");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoginForm] MongoDB bağlantı hatası: {ex.Message}");
                // MongoDB bağlantısı olmasa bile giriş yapılabilir, sadece kayıt edilemez
                _mongoDbService = null;
            }
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string enteredPassword = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(enteredPassword))
            {
                XtraMessageBox.Show(
                    "Lütfen şifre giriniz.",
                    "Uyarı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            if (enteredPassword == CORRECT_PASSWORD)
            {
                // Giriş başarılı - veritabanına kaydet
                SaveLoginToDatabase();

                // MainForm'u göster
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                XtraMessageBox.Show(
                    "Hatalı şifre! Lütfen tekrar deneyiniz.",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                txtPassword.Text = "";
                txtPassword.Focus();
            }
        }

        private void SaveLoginToDatabase()
        {
            if (_mongoDbService == null || !_mongoDbService.IsConnected())
            {
                System.Diagnostics.Debug.WriteLine("[LoginForm] MongoDB bağlantısı yok, giriş kaydedilemedi");
                return;
            }

            try
            {
                var loginLog = new BsonDocument
                {
                    { "LoginTime", DateTime.Now },
                    { "Success", true },
                    { "Password", "***" } // Güvenlik için şifreyi kaydetme
                };

                var collection = _mongoDbService.GetCollection<BsonDocument>("loginLogs");
                collection.InsertOne(loginLog);

                System.Diagnostics.Debug.WriteLine($"[LoginForm] Giriş kaydı veritabanına kaydedildi: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoginForm] Giriş kaydı veritabanına kaydedilirken hata: {ex.Message}");
                // Hata olsa bile girişe izin ver
            }
        }
    }
}

