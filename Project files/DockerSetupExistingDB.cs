using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace Engrafo_1_Installer
{
    public partial class DockerSetupExistingDB : UserControl
    {
        private Label lblTitle, lblDBPort, lblUser, lblPassword, lblDbName, lblAppPort, lblDirectory;
        private TextBox textBoxDBPort, textBoxUser, textBoxPassword, textBoxDbName, textBoxAppPort, textBoxDirectory;
        private Button buttonBrowse, buttonDeploy, btnBack;
        private ProgressBar progressBar;
        private ToolTip _tip = new ToolTip();

        public string AppPort => textBoxAppPort.Text.Trim();
        public string DirectoryPath => textBoxDirectory.Text.Trim();

        private TableLayoutPanel tbl;

        private string TemplateFilePath =>
            Path.Combine(Directory.GetCurrentDirectory(), "docker-compose.existingdb.template.yml");
        private string ComposeFilePath =>
            Path.Combine(Directory.GetCurrentDirectory(), "docker-compose.yml");

        public event EventHandler BackClicked;
        public event EventHandler DeployCompleted;

        public DockerSetupExistingDB()
        {
            InitializeDockerComponent();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                progressBar.Visible = false;
                buttonDeploy.Enabled = true;
            }
        }

        private void InitializeDockerComponent()
        {
            const int margin = 20, spacingBetween = 10;
            this.Dock = DockStyle.Fill;
            DisplayHelper.AdjustControlForDpi(this);

            tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(margin),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 8
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < tbl.RowCount; i++)
                tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Title
            lblTitle = new Label
            {
                Text = "Docker App (Existing MSSQL)\r\nConfigure Engrafo to use your running MSSQL Server in Docker.",
                Font = new Font(this.Font, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            tbl.Controls.Add(lblTitle, 0, 0);
            tbl.SetColumnSpan(lblTitle, 2);

            // Database Port
            lblDBPort = new Label { Text = "SQL Server Port:", AutoSize = true };
            textBoxDBPort = new TextBox { PlaceholderText = "1433", Width = 120, Text = "1433" };
            tbl.Controls.Add(lblDBPort, 0, 1);
            tbl.Controls.Add(textBoxDBPort, 1, 1);

            // User
            lblUser = new Label { Text = "SQL User:", AutoSize = true };
            textBoxUser = new TextBox { PlaceholderText = "e.g. sa", Width = 120, Text = "sa" };
            tbl.Controls.Add(lblUser, 0, 2);
            tbl.Controls.Add(textBoxUser, 1, 2);

            // Password
            lblPassword = new Label { Text = "SQL Password:", AutoSize = true };
            textBoxPassword = new TextBox
            {
                PlaceholderText = "Your password", Width = 120, UseSystemPasswordChar = true,
                Text = "yourStrong(!)Password"
            };
            tbl.Controls.Add(lblPassword, 0, 3);
            tbl.Controls.Add(textBoxPassword, 1, 3);

            // Database Name (NEW)
            lblDbName = new Label { Text = "Database Name:", AutoSize = true };
            textBoxDbName = new TextBox { PlaceholderText = "Database Name", Width = 150, Text = "Engrafo_DB" };
            tbl.Controls.Add(lblDbName, 0, 4);
            tbl.Controls.Add(textBoxDbName, 1, 4);

            // Application Port
            lblAppPort = new Label { Text = "Engrafo App Port:", AutoSize = true };
            textBoxAppPort = new TextBox { PlaceholderText = "App Host Port", Width = 120, Text = "81" };
            tbl.Controls.Add(lblAppPort, 0, 5);
            tbl.Controls.Add(textBoxAppPort, 1, 5);

            // Directory + Browse
            lblDirectory = new Label { Text = "Directory Path:", AutoSize = true };
            var dirPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0)
            };
            textBoxDirectory = new TextBox { PlaceholderText = "Directory Path", Width = 220 };
            buttonBrowse = new Button { Text = "Browse…", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            buttonBrowse.Click += buttonBrowse_Click;
            dirPanel.Controls.Add(textBoxDirectory);
            dirPanel.Controls.Add(buttonBrowse);

            tbl.Controls.Add(lblDirectory, 0, 6);
            tbl.Controls.Add(dirPanel, 1, 6);

            // Progress bar
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Size = new Size(550, 23),
                Style = ProgressBarStyle.Marquee,
                Visible = false,
                Margin = new Padding(0, spacingBetween, 0, 0)
            };
            tbl.Controls.Add(progressBar, 0, 7);
            tbl.SetColumnSpan(progressBar, 2);

            Controls.Add(tbl);

            // Footer Buttons
            btnBack = new Button { Text = "← Back", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnBack.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);

            buttonDeploy = new Button { Text = "Deploy", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            buttonDeploy.Click += buttonDeploy_Click;

            Controls.Add(btnBack);
            Controls.Add(buttonDeploy);
            btnBack.BringToFront();
            buttonDeploy.BringToFront();

            LayoutHelper.PlaceFooterButtons(this, buttonDeploy, btnBack, null, margin, spacingBetween);

            this.Resize += (s, e) =>
                LayoutHelper.PlaceFooterButtons(this, buttonDeploy, btnBack, null, margin, spacingBetween);
            this.VisibleChanged += (s, e) =>
            {
                if (Visible)
                    LayoutHelper.PlaceFooterButtons(this, buttonDeploy, btnBack, null, margin, spacingBetween);
            };

            // Help icons
            AddHelp(textBoxDBPort, "The TCP port on your host mapped to the SQL Server in Docker (default 1433).");
            AddHelp(textBoxUser, "Your SQL Server username (default for Docker is 'sa').");
            AddHelp(textBoxPassword, "Your SQL Server password. Default is usually 'yourStrong(!)Password'.");
            AddHelp(textBoxDbName, "The database that Engrafo should use (it will be created automatically if it doesn't exist).");
            AddHelp(textBoxAppPort, "The TCP port on your host to map to the Engrafo web app inside Docker.");
            AddHelp(buttonBrowse,
                "The local folder on your computer to mount into Docker as your auto uploads folder.");
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
                using var st = name == null ? null : asm.GetManifestResourceStream(name);
                bmp = st == null ? SystemIcons.Question.ToBitmap() : new Bitmap(st);
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
                Cursor = Cursors.Hand
            };
            _tip.SetToolTip(pic, tooltip);

            void Position()
            {
                var parent = anchor.Parent;
                var absoluteX = parent.Left + anchor.Left + anchor.Width + 4;
                var absoluteY = parent.Top + anchor.Top + (anchor.Height - iconSize) / 2;
                pic.Location = new Point(absoluteX, absoluteY);
            }

            anchor.SizeChanged += (_, __) => Position();
            anchor.LocationChanged += (_, __) => Position();
            anchor.ParentChanged += (_, __) => Position();
            this.Layout += (_, __) => Position();
            if (anchor.Parent != null)
                anchor.Parent.Layout += (_, __) => Position();

            Position();

            Controls.Add(pic);
            pic.BringToFront();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                textBoxDirectory.Text = dlg.SelectedPath;
        }

        private bool IsDockerInstalled()
        {
            try
            {
                var psi = new ProcessStartInfo("docker", "--version")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                string outp = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return p.ExitCode == 0 && !string.IsNullOrWhiteSpace(outp);
            }
            catch
            {
                return false;
            }
        }

        private async void buttonDeploy_Click(object sender, EventArgs e)
        {
            
            if (!int.TryParse(textBoxAppPort.Text.Trim(), out int appPort))
            {
                MessageBox.Show("Please enter a valid port number for the Engrafo App.", "Invalid Port", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (IsPortInUse(appPort))
            {
                MessageBox.Show($"The port {appPort} is already in use. Please choose another port.", "Port In Use", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (IsPortInUseByDocker(appPort))
            {
                MessageBox.Show($"The port {appPort} is already in use by a running Docker container. Please choose another port.", "Port In Use (Docker)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Validate input fields
            if (string.IsNullOrWhiteSpace(textBoxDBPort.Text) ||
                string.IsNullOrWhiteSpace(textBoxUser.Text) ||
                string.IsNullOrWhiteSpace(textBoxPassword.Text) ||
                string.IsNullOrWhiteSpace(textBoxDbName.Text) ||
                string.IsNullOrWhiteSpace(textBoxAppPort.Text) ||
                string.IsNullOrWhiteSpace(textBoxDirectory.Text))
            {
                MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!IsDockerInstalled())
            {
                MessageBox.Show("Docker not installed or not on PATH.", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            string tplcheck;
            try
            {
                tplcheck = ReadEmbeddedResource("docker-compose.existingdb.template.yml");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Template not found in embedded resources: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            var userDb = textBoxDbName.Text.Trim();

            // --- 1. SILENT SQL CONNECTION CHECK using master ---
            var testConnString =
                $"Server=localhost,{textBoxDBPort.Text.Trim()};Database=master;User ID={textBoxUser.Text.Trim()};Password={textBoxPassword.Text};TrustServerCertificate=True;Connection Timeout=4;";

            // Optional: show a message under the Directory field
            Label lblStatus = null;
            if (!tbl.Controls.ContainsKey("lblStatus"))
            {
                lblStatus = new Label
                {
                    Name = "lblStatus",
                    Text = "Checking SQL login...",
                    AutoSize = true,
                    ForeColor = Color.Gray
                };
                tbl.Controls.Add(lblStatus, 1, 7);
                tbl.SetColumnSpan(lblStatus, 1);
                tbl.Controls.SetChildIndex(lblStatus, tbl.Controls.Count - 1);
                lblStatus.Visible = true;
            }
            else
            {
                lblStatus = (Label)tbl.Controls["lblStatus"];
                lblStatus.Text = "Checking SQL login...";
                lblStatus.Visible = true;
            }

            try
            {
                using (var conn = new SqlConnection(testConnString))
                {
                    await conn.OpenAsync();
                    await conn.CloseAsync();
                }

                lblStatus.Text = "✔ SQL login succeeded.";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "SQL login failed!";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show(
                    "Could not connect to SQL Server with the provided details:\n\n" + ex.Message,
                    "SQL Connection Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                return;
            }

            // --- 2. For Docker Compose, use the actual database ---
            var appConnString =
                $"Server=host.docker.internal,{textBoxDBPort.Text.Trim()};Database={userDb};User ID={textBoxUser.Text.Trim()};Password={textBoxPassword.Text};TrustServerCertificate=True;";

            // --- 3. Create the Docker Compose file ---
            try
            {
                var tpl = ReadEmbeddedResource("docker-compose.existingdb.template.yml");
                var dir = textBoxDirectory.Text.Replace("\\", "/");
                var outp = tpl
                    .Replace("${MY_HOSTPORT}", textBoxAppPort.Text.Trim())
                    .Replace("${MY_DIRECTORY}", dir)
                    .Replace("${MY_CONNECTIONS_STRING}", appConnString);

                File.WriteAllText(ComposeFilePath, outp);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing template:\n" + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // --- 4. Deploy via Docker Compose ---
            progressBar.Visible = true;
            buttonDeploy.Enabled = false;

            try
            {
                await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo("docker", "compose up -d")
                    {
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc == null) throw new Exception("Failed to start Docker");
                    string o = proc.StandardOutput.ReadToEnd();
                    string e2 = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                    if (proc.ExitCode != 0) throw new Exception(o + "\n" + e2);
                });

                //MessageBox.Show(
                //    "Installation complete! Open Engrafo at http://localhost:" + textBoxAppPort.Text.Trim(),
                //    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DeployCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Docker Compose failed:\n" + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Visible = false;
                buttonDeploy.Enabled = true;
                if (lblStatus != null)
                {
                    lblStatus.Visible = false;
                }
            }
        }
        
        
        private string ReadEmbeddedResource(string resourceFileName)
        {
            var asm = GetType().Assembly;
            var resName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(resourceFileName, StringComparison.OrdinalIgnoreCase));

            if (resName == null)
                throw new FileNotFoundException("Embedded resource not found: " + resourceFileName);

            using (var stream = asm.GetManifestResourceStream(resName))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
        
        private bool IsPortInUse(int port)
        {
            try
            {
                // Try to open a TcpListener on the port
                var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return false; // Port is not in use
            }
            catch (System.Net.Sockets.SocketException)
            {
                return true; // Port is in use
            }
        }
        
        private bool IsPortInUseByDocker(int port)
        {
            try
            {
                var psi = new ProcessStartInfo("docker", "ps --format \"{{.ID}}: {{.Ports}}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Look for "0.0.0.0:PORT" or ":::PORT"
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains($"0.0.0.0:{port}->") || line.Contains($"127.0.0.1:{port}->") || line.Contains($"::{port}->"))
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }


        
    }
}
