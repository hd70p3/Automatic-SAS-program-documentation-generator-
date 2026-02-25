using System;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace Engrafo_1_Installer
{
    public partial class AppSettingsPanel : UserControl
    {
        public event EventHandler<AppSettingsEventArgs> AppSettingsUpdated;
        public event EventHandler BackClicked;

        // UI
        private Label lblFolder, lblServer, lblDatabase, lblUser, lblPassword;
        private TextBox txtFolder, txtServer, txtDatabase, txtUser, txtPassword;
        private Button btnBrowse, btnTest, btnUpdate, btnBack, btnNext, btnApply ;
        private RadioButton rbLocalDb, rbDockerDb, rbSqlLogin;
        private ToolTip _tip = new ToolTip();
        private Label lblStatus; 

        public string AppFolder => txtFolder.Text.Trim();
        public string ConnectionString { get; private set; }

        public string ConfigurationFolder
        {
            get => txtFolder.Text;
            set => txtFolder.Text = value;
        }

        public AppSettingsPanel()
        {
            InitializeAppsettingsComponent();
        }

        private void InitializeAppsettingsComponent()
        {
            const int margin = 20;
            const int spacingBetween = 10;

            this.Dock = DockStyle.Fill;

            // ── Main layout ─────────────────────────────────────────
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(margin),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 8 // remove nav row here
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            // Rows: folder, auth, server, database, user, password, actions, filler
            for (int i = 0; i < 7; i++)
                tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // filler

            // ── Folder row ──────────────────────────────────────────
            lblFolder = new Label { Text = "Configuration folder:", AutoSize = true };
            txtFolder = new TextBox { Width = 400 };
            btnBrowse = new Button { Text = "Browse…", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };

            btnBrowse.Click += (_, __) =>
            {
                using var d = new FolderBrowserDialog();
                if (d.ShowDialog() == DialogResult.OK)
                    txtFolder.Text = d.SelectedPath;
            };
            var folderPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            folderPanel.Controls.Add(txtFolder);
            folderPanel.Controls.Add(btnBrowse);
            tbl.Controls.Add(lblFolder, 0, 0);
            tbl.Controls.Add(folderPanel, 1, 0);

            // ── DB mode row ─────────────────────────────────────────
            rbLocalDb = new RadioButton { Text = "Integrated Security", Checked = true, AutoSize = true };
            rbDockerDb = new RadioButton { Text = "Docker-DB", AutoSize = true };
            rbSqlLogin = new RadioButton { Text = "SQL Login (user/password)", AutoSize = true };
            rbLocalDb.CheckedChanged += Radio_CheckedChanged;
            rbDockerDb.CheckedChanged += Radio_CheckedChanged;
            rbSqlLogin.CheckedChanged += Radio_CheckedChanged;
            var authPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            authPanel.Controls.Add(rbLocalDb);
            authPanel.Controls.Add(rbDockerDb);
            authPanel.Controls.Add(rbSqlLogin);
            tbl.Controls.Add(authPanel, 0, 1);
            tbl.SetColumnSpan(authPanel, 2);

            // ── Server row ──────────────────────────────────────────
            lblServer = new Label { Text = "Server:", AutoSize = true };
            txtServer = new TextBox { Width = 200 };
            tbl.Controls.Add(lblServer, 0, 2);
            tbl.Controls.Add(txtServer, 1, 2);

            // ── Database row ───────────────────────────────────────
            lblDatabase = new Label { Text = "Database:", AutoSize = true };
            txtDatabase = new TextBox { Width = 200 };
            tbl.Controls.Add(lblDatabase, 0, 3);
            tbl.Controls.Add(txtDatabase, 1, 3);

            // ── User ID row ────────────────────────────────────────
            lblUser = new Label { Text = "User ID:", AutoSize = true };
            txtUser = new TextBox { Width = 200, Enabled = false };
            tbl.Controls.Add(lblUser, 0, 4);
            tbl.Controls.Add(txtUser, 1, 4);

            // ── Password row ───────────────────────────────────────
            lblPassword = new Label { Text = "Password:", AutoSize = true };
            txtPassword = new TextBox { Width = 200, UseSystemPasswordChar = true, Enabled = false };
            tbl.Controls.Add(lblPassword, 0, 5);
            tbl.Controls.Add(txtPassword, 1, 5);

            // ── Action buttons row ─────────────────────────────────
            btnApply = new Button
            {
                Text = "Test & Save Settings",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            btnApply.Click += BtnApply_Click;
            var actionPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2
            };
            actionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            actionPanel.Controls.Add(btnApply, 0, 0);
            lblStatus = new Label
            {
                Text = "Checking connection...",
                AutoSize = true,
                ForeColor = Color.Gray,
                Visible = false
            };
            actionPanel.Controls.Add(lblStatus, 0, 1);

            tbl.Controls.Add(actionPanel, 0, 6);
            tbl.SetColumnSpan(actionPanel, 2);

            
            
            // Add the table to this panel
            Controls.Add(tbl);

            // ── Footer buttons via LayoutHelper ─────────────────────
            btnBack = new Button { Text = "← Back", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnBack.Click += (_, __) => BackClicked?.Invoke(this, EventArgs.Empty);

            btnNext = new Button { Text = "Next →", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnNext.Click += BtnNext_Click;

            Controls.Add(btnBack);
            Controls.Add(btnNext);

            // Ensure buttons are on top
            btnBack.BringToFront();
            btnNext.BringToFront();

            // Re-position on resize & first show
            this.Resize += (s, e) =>
                LayoutHelper.PlaceFooterButtons(
                    parent: this,
                    btnNext: btnNext,
                    btnBack: btnBack,
                    margin: margin,
                    spacingBetween: spacingBetween
                );
            this.VisibleChanged += (s, e) =>
            {
                if (Visible)
                    LayoutHelper.PlaceFooterButtons(
                        parent: this,
                        btnNext: btnNext,
                        btnBack: btnBack,
                        margin: margin,
                        spacingBetween: spacingBetween
                    );
            };

            // Initial placement
            LayoutHelper.PlaceFooterButtons(
                parent: this,
                btnNext: btnNext,
                btnBack: btnBack,
                margin: margin,
                spacingBetween: spacingBetween
            );


            // ── Help icons ─────────────────────────────────────────
            AddHelp(btnBrowse,
                "Browse to choose the folder where you downloaded the files needed to run Engrafo");
            AddHelp(rbSqlLogin,
                "Choose between whether to use a locally installed database, one that runs in docker, or using a regular sql login");
            AddHelp(txtServer,
                "Enter the DB server name or address. You can find the your servername under connection properties for your database");
            AddHelp(txtDatabase, "Enter the name of the database that you created (e.g. engrafodb)");
            AddHelp(txtUser, "If you are using SQL Server credentials enter the SQL User ID");
            AddHelp(txtPassword, "Enter the SQL password for the above login");
            AddHelp(btnApply,
                "Test Connection verifies that the connection parameters are valid and working. " +
                "Update AppSettings writes them to appsettings.json. This will enable Engrafo to communicate with the database");
        }


        private void Radio_CheckedChanged(object sender, EventArgs e)
        {
            bool sqlAuth = rbDockerDb.Checked || rbSqlLogin.Checked;
            txtUser.Enabled = sqlAuth;
            txtPassword.Enabled = sqlAuth;

            if (rbLocalDb.Checked)
            {
                txtUser.Text = "";
                txtPassword.Text = "";
            }

            Theme.Apply(lblUser);
            Theme.Apply(txtUser);
            Theme.Apply(lblPassword);
            Theme.Apply(txtPassword);
        }

        private async void BtnApply_Click(object sender, EventArgs e)
        {
            btnApply.Enabled = false;
            lblStatus.Visible = true;
            lblStatus.Text = "Checking connection...";
            try
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = txtServer.Text.Trim(),
                    InitialCatalog = txtDatabase.Text.Trim(),
                    TrustServerCertificate = true
                };

                if (rbLocalDb.Checked)
                {
                    builder.IntegratedSecurity = true;
                    builder.MultipleActiveResultSets = true;
                }
                else
                {
                    builder.UserID = txtUser.Text.Trim();
                    builder.Password = txtPassword.Text;
                }
                ConnectionString = builder.ConnectionString
                    .Replace("Trust Server Certificate", "TrustServerCertificate")
                    .Replace("Multiple Active Result Sets", "MultipleActiveResultSets");
                
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();
                await conn.CloseAsync();

                // Connection succeeded → update settings
                var folder = txtFolder.Text.Trim();
                var path = Path.Combine(folder, "appsettings.json");

                if (!File.Exists(path))
                {
                    MessageBox.Show($"appsettings.json not found in:\n{folder}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var jsonText = File.ReadAllText(path);
                var root = JsonNode.Parse(jsonText)?.AsObject();
                if (root == null)
                {
                    MessageBox.Show("Failed to parse appsettings.json", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var csNode = root["ConnectionStrings"]?.AsObject();
                if (csNode == null)
                {
                    MessageBox.Show("No \"ConnectionStrings\" section found", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                csNode["DefaultConnection"] = ConnectionString;

                File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                MessageBox.Show("✔ Connection succeeded and appsettings.json updated!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"Connection failed:\n{sqlEx.Message}", "Connection Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lblStatus.Visible = false;
                btnApply.Enabled = true;
            }
        }


        private async void BtnNext_Click(object sender, EventArgs e)
        {
            lblStatus.Visible = true;

            var folder = txtFolder.Text.Trim();
            if (string.IsNullOrEmpty(folder))
            {
                MessageBox.Show("Please select a configuration folder.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var configPath = Path.Combine(folder, "appsettings.json");
            if (!File.Exists(configPath))
            {
                MessageBox.Show($"appsettings.json not found in:\n{folder}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtServer.Text) ||
                string.IsNullOrWhiteSpace(txtDatabase.Text) ||
                ((rbDockerDb.Checked || rbSqlLogin.Checked) &&
                 (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))))
            {
                MessageBox.Show("Please fill in all database fields.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnNext.Enabled = false;
            try
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = txtServer.Text.Trim(),
                    InitialCatalog = txtDatabase.Text.Trim(),
                    TrustServerCertificate = true
                };

                if (rbLocalDb.Checked)
                {
                    builder.IntegratedSecurity = true;
                    builder.MultipleActiveResultSets = true;
                }
                else
                {
                    builder.UserID = txtUser.Text.Trim();
                    builder.Password = txtPassword.Text;
                }
                ConnectionString = builder.ConnectionString
                    .Replace("Trust Server Certificate", "TrustServerCertificate")
                    .Replace("Multiple Active Result Sets", "MultipleActiveResultSets");

                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();
                await conn.CloseAsync();

                // Silent update of appsettings.json
                var jsonText = File.ReadAllText(configPath);
                var root = JsonNode.Parse(jsonText)?.AsObject();
                var csNode = root?["ConnectionStrings"]?.AsObject();
                if (csNode != null)
                {
                    csNode["DefaultConnection"] = ConnectionString;
                    File.WriteAllText(configPath,
                        root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                }

                ConnectionString = builder.ConnectionString;
                AppSettingsUpdated?.Invoke(this, new AppSettingsEventArgs(folder, ConnectionString));
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"Connection test failed:\n{sqlEx.Message}",
                    "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lblStatus.Visible = false;

                btnNext.Enabled = true;
            }
        }


        private void AddHelp(Control anchor, string tooltip)
        {
            const int iconSize = 16;
            Bitmap bmp;
            try
            {
                var asm = GetType().Assembly;
                var name = asm.GetManifestResourceNames()
                    .FirstOrDefault(n =>
                        n.EndsWith("Icon-round-Question_mark.svg.png", StringComparison.OrdinalIgnoreCase));
                using var st = name == null
                    ? null
                    : asm.GetManifestResourceStream(name);
                bmp = st == null
                    ? SystemIcons.Question.ToBitmap()
                    : new Bitmap(st);
            }
            catch
            {
                bmp = SystemIcons.Question.ToBitmap();
            }

            var pic = new PictureBox
            {
                Image = new Bitmap(bmp, new Size(iconSize, iconSize)),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(iconSize, iconSize),
                Cursor = Cursors.Default
            };
            _tip.SetToolTip(pic, tooltip);

            void Position()
            {
                // Compute anchor’s absolute position in this control:
                var parent = anchor.Parent;
                var absoluteX = parent.Left + anchor.Left + anchor.Width + 4;
                var absoluteY = parent.Top + anchor.Top + (anchor.Height - iconSize) / 2;
                pic.Location = new Point(absoluteX, absoluteY);
            }

            // Reposition whenever layout changes anywhere
            anchor.SizeChanged += (_, __) => Position();
            anchor.LocationChanged += (_, __) => Position();
            anchor.Parent.Layout += (_, __) => Position();
            this.Layout += (_, __) => Position();

            // initial
            Position();

            Controls.Add(pic);
            pic.BringToFront();
        }
    }

    public class AppSettingsEventArgs : EventArgs
    {
        public string Folder { get; }
        public string ConnectionString { get; }

        public AppSettingsEventArgs(string folder, string connectionString)
        {
            Folder = folder;
            ConnectionString = connectionString;
        }
    }
}