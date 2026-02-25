using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.Administration;

namespace Engrafo_1_Installer
{
    public partial class IisSetupPanel : UserControl
    {
        public event EventHandler BackClicked;
        public event EventHandler CreateClicked;

        // ── UI controls ──────────────────────────────────────────────────
        private Label lblTitle, lblDesc,lblFolderWarning;
        private GroupBox gbMode;
        private RadioButton rbCreateNewSite, rbUseExistingSite;
        private ComboBox cbExistingSites;

        private Label lblPath, lblSiteName, lblPort;
        private TextBox txtPath, txtSiteName, txtPort;
        private Button btnBrowse;

        private GroupBox gbAuth;
        private RadioButton rbAnonymous, rbWindowsAuth;
        private LinkLabel llIisGuide;

        private Button btnCreate, btnBack;
        private FlowLayoutPanel nav;

        private string _appPoolName;
        private Bitmap questionBmp;
        private ToolTip tt = new ToolTip();
        public string ExpectedFolder { get; set; }  // Set this from MainForm when navigating here


        public int PortNumber => int.TryParse(txtPort.Text.Trim(), out var p) ? p : 0;

        public string PhysicalFolder
        {
            get => txtPath.Text;
            set => txtPath.Text = value;
        }

        public IisSetupPanel()
        {
            InitializeIISComponent();
        }

        private void InitializeIISComponent()
        {
            const int margin = 20;
            const int iconSize = 16;

            this.Dock = DockStyle.Fill;

            // ── Load the question‐mark icon ────────────────────────────
            try
            {
                var asm = typeof(IisSetupPanel).Assembly;
                var res = asm.GetManifestResourceNames()
                    .FirstOrDefault(n =>
                        n.EndsWith("Icon-round-Question_mark.svg.png", StringComparison.OrdinalIgnoreCase));
                using var str = res == null ? null : asm.GetManifestResourceStream(res);
                questionBmp = str == null ? SystemIcons.Question.ToBitmap() : new Bitmap(str);
            }
            catch
            {
                questionBmp = SystemIcons.Question.ToBitmap();
            }

            tt.ShowAlways = true;

            // ── Main layout ─────────────────────────────────────────────
            var mainTbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(margin),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 9
            };
            mainTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            // rows: title, desc, mode, path, site, port, auth, link, filler
            for (int i = 0; i < 8; i++)
                mainTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainTbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // 0: Title
            lblTitle = new Label
            {
                Text = "Configure IIS Website",
                Font = new Font(Font, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Top
            };
            mainTbl.Controls.Add(lblTitle, 0, 0);

            // 1: Description
            lblDesc = new Label
            {
                Text = "Pick an existing site or create a new one, then configure its folder, port and auth.",
                AutoSize = true,
                MaximumSize = new Size(650, 0),
                Dock = DockStyle.Top
            };
            mainTbl.Controls.Add(lblDesc, 0, 1);

            
            
            // 2: Site selection
            gbMode = new GroupBox
            {
                Text = "Site selection",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Margin = new Padding(0, margin / 2, 0, margin / 2)
            };
            var modeTbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(6)
            };
            modeTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            modeTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            modeTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            modeTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            rbCreateNewSite = new RadioButton { Text = "Create new site", AutoSize = true, Checked = true };
            rbUseExistingSite = new RadioButton { Text = "Use existing site", AutoSize = true };
            rbCreateNewSite.CheckedChanged += ModeChanged;
            rbUseExistingSite.CheckedChanged += ModeChanged;

            cbExistingSites = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };

            cbExistingSites = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false,
                // Do not set AutoSize, as it does not affect ComboBox width
            };

            using (var mgr = new ServerManager())
                foreach (var s in mgr.Sites)
                    cbExistingSites.Items.Add(s.Name);
            if (cbExistingSites.Items.Count > 0)
                cbExistingSites.SelectedIndex = 0;

            // adjust dropdown width to longest name
            if (cbExistingSites.Items.Count > 0)
            {
                int maxW = cbExistingSites.Items
                    .Cast<object>()
                    .Select(o => TextRenderer.MeasureText(o.ToString(), cbExistingSites.Font).Width)
                    .Max();
                cbExistingSites.DropDownWidth = maxW + SystemInformation.VerticalScrollBarWidth + 8;
                cbExistingSites.Width = maxW + SystemInformation.VerticalScrollBarWidth + 8;    
            }

            cbExistingSites.SelectedIndexChanged += (_, __) =>
            {
                if (!rbUseExistingSite.Checked || cbExistingSites.SelectedItem == null) return;
                var name = cbExistingSites.SelectedItem.ToString();
                txtSiteName.Text = name;
                using var mgr2 = new ServerManager();
                var site = mgr2.Sites[name];
                txtPath.Text = site.Applications["/"].VirtualDirectories["/"].PhysicalPath;
                
                var binding = site.Bindings.FirstOrDefault(b => b.Protocol == "http" && b.EndPoint != null);
                if (binding != null)
                    txtPort.Text = binding.EndPoint.Port.ToString();
                
                UpdateFolderWarning();
            };

            modeTbl.Controls.Add(rbCreateNewSite, 0, 0);
            modeTbl.Controls.Add(rbUseExistingSite, 1, 0);
            modeTbl.Controls.Add(cbExistingSites, 2, 0);
            //modeTbl.SetColumnSpan(cbExistingSites, 3);
            gbMode.Controls.Add(modeTbl);
            mainTbl.Controls.Add(gbMode, 0, 3);

            // 3: Physical folder
            var pathTbl = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount = 2, 
                Dock = DockStyle.Top,
                Margin = new Padding(0, margin / 2, 0, margin / 2)
            };
            pathTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            

            lblPath = new Label { Text = "Physical folder:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtPath = new TextBox { Width = 300 };
            txtPath.TextChanged += (_, __) => UpdateFolderWarning();
            btnBrowse = new Button { Text = "Browse…", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnBrowse.Click += (_, __) =>
            {
                using var dlg = new FolderBrowserDialog { Description = "Select your Engrafo folder" };
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtPath.Text = dlg.SelectedPath;
            };

            pathTbl.Controls.Add(lblPath, 0, 0);
            pathTbl.Controls.Add(txtPath, 1, 0);
            pathTbl.Controls.Add(btnBrowse, 2, 0);
            
            lblFolderWarning = new Label
            {
                Text = "Warning: The existing site uses a different folder from the one you setup on previous panels",
                AutoSize = true,
                ForeColor = Color.Red,
                Visible = false,
                Margin = new Padding(0, 2, 0, 8)
            };
            pathTbl.Controls.Add(lblFolderWarning, 0, 1);
            pathTbl.SetColumnSpan(lblFolderWarning, 3);
            
            
            mainTbl.Controls.Add(pathTbl, 0, 3);


            // 4: Site name
            var siteTbl = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Dock = DockStyle.Top,
                Margin = new Padding(0, margin / 2, 0, margin / 2)
            };
            siteTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            siteTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblSiteName = new Label { Text = "Site name:", AutoSize = true, Anchor = AnchorStyles.Left };

            txtSiteName = new TextBox
            {
                Width = 200,
                Text = cbExistingSites.SelectedItem != null ? cbExistingSites.SelectedItem.ToString() : "Engrafo"
            };

            siteTbl.Controls.Add(lblSiteName, 0, 0);
            siteTbl.Controls.Add(txtSiteName, 1, 0);
            mainTbl.Controls.Add(siteTbl, 0, 5);

            // 5: Port
            var portTbl = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Dock = DockStyle.Top,
                Margin = new Padding(0, margin / 2, 0, margin / 2)
            };
            portTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            portTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblPort = new Label { Text = "Port:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtPort = new TextBox { Width = 80, Text = "81" };
            portTbl.Controls.Add(lblPort, 0, 0);
            portTbl.Controls.Add(txtPort, 1, 0);
            mainTbl.Controls.Add(portTbl, 0, 6);

            // 6: Authentication
            gbAuth = new GroupBox
            {
                Text = "Authentication",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Margin = new Padding(0, margin / 2, 0, margin / 2)
            };
            var authFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill
            };
            rbAnonymous = new RadioButton { Text = "Anonymous", AutoSize = true, Checked = true };
            rbWindowsAuth = new RadioButton { Text = "Windows Authentication", AutoSize = true };
            bool winAuthAvail;
            try
            {
                using var mgr = new ServerManager();
                mgr.GetApplicationHostConfiguration()
                    .GetSection("system.webServer/security/authentication/windowsAuthentication", "/");
                winAuthAvail = true;
            }
            catch
            {
                winAuthAvail = false;
            }

            if (!winAuthAvail)
            {
                rbWindowsAuth.Enabled = false;
                rbAnonymous.Checked = true;
                rbWindowsAuth.Text += " (not installed)";
                AddHelp(gbAuth,
                    "To use Windows Authentication, enable it in Windows Features → IIS → WWW Services → Security.");
            }

            authFlow.Controls.Add(rbAnonymous);
            authFlow.Controls.Add(rbWindowsAuth);
            gbAuth.Controls.Add(authFlow);
            mainTbl.Controls.Add(gbAuth, 0, 7);

            // 7: IIS install link
            llIisGuide = new LinkLabel
            {
                Text = "How to install IIS",
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, margin / 2, 0, margin / 2)
            };
            llIisGuide.LinkClicked += (_, __) =>
                Process.Start(new ProcessStartInfo(
                        "https://engrafo.atlassian.net/wiki/spaces/ED/pages/21168129/Installation+-+server+desktop#Installation-steps-for-Internet-Information-Server(IIS)")
                    { UseShellExecute = true });
            mainTbl.Controls.Add(llIisGuide, 0, 8);


            // 8: filler
            mainTbl.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 9);

            // add the table to the control
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            scroll.Controls.Add(mainTbl);
            Controls.Add(scroll);

            // ── Footer buttons via LayoutHelper ────────────────────────
            const int spacingBetween = 10;

// create standalone buttons
            btnCreate = new Button
                { Text = "Create / Continue", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnCreate.Click += BtnCreate_Click;
            btnBack = new Button { Text = "← Back", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnBack.Click += (_, __) => BackClicked?.Invoke(this, EventArgs.Empty);

// add them directly to the control
            Controls.Add(btnBack);
            Controls.Add(btnCreate);

// make sure they float on top
            btnBack.BringToFront();
            btnCreate.BringToFront();

// reposition on resize
            this.Resize += (s, e) =>
                LayoutHelper.PlaceFooterButtons(
                    parent: this,
                    btnNext: btnCreate,
                    btnBack: btnBack,
                    btnExtra: null,
                    margin: margin,
                    spacingBetween: spacingBetween
                );

// reposition when first shown
            this.VisibleChanged += (s, e) =>
            {
                if (Visible)
                    LayoutHelper.PlaceFooterButtons(
                        parent: this,
                        btnNext: btnCreate,
                        btnBack: btnBack,
                        btnExtra: null,
                        margin: margin,
                        spacingBetween: spacingBetween
                    );
            };

// initial placement
            LayoutHelper.PlaceFooterButtons(
                parent: this,
                btnNext: btnCreate,
                btnBack: btnBack,
                btnExtra: null,
                margin: margin,
                spacingBetween: spacingBetween
            );

            // initial enable/disable
            ModeChanged(null, EventArgs.Empty);

            // helper icons
            AddHelp(btnBrowse, "Browse to the folder where you unpacked Engrafo.");
            AddHelp(txtSiteName, "The IIS site name that will host Engrafo.");
            AddHelp(txtPort, "The HTTP port IIS should bind for Engrafo.");
            AddHelp(gbMode, "Either create a new site on IIS, or re-use an existing one.");

            // DisplayHelper.AdjustControlForDpi(this);
        }

        private void ModeChanged(object sender, EventArgs e)
        {
            bool useExisting = rbUseExistingSite.Checked;
            cbExistingSites.Visible = useExisting;

            lblPath.Enabled = txtPath.Enabled = btnBrowse.Enabled =
                lblSiteName.Enabled = txtSiteName.Enabled =
                    lblPort.Enabled = txtPort.Enabled =
                        gbAuth.Enabled =
                            !useExisting;
            //txtSiteName.Text  = cbExistingSites.SelectedItem != null ? cbExistingSites.SelectedItem.ToString() : "Engrafo" ;
            
            if (!useExisting)
            {
                if (!string.IsNullOrWhiteSpace(ExpectedFolder))
                    txtPath.Text = ExpectedFolder;
                lblFolderWarning.Visible = false;
            }
            else
            {
                UpdateFolderWarning();
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (rbUseExistingSite.Checked)
            {
                if (cbExistingSites.SelectedItem == null)
                {
                    MessageBox.Show("Please select an existing site.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var name = cbExistingSites.SelectedItem.ToString();
                using var mgr = new ServerManager();
                var site = mgr.Sites[name];
                _appPoolName = site.ApplicationDefaults.ApplicationPoolName;
                CreateClicked?.Invoke(this, EventArgs.Empty);
                return;
            }

            var siteName = txtSiteName.Text.Trim();
            var port = txtPort.Text.Trim();
            var path = txtPath.Text.Trim();
            if (siteName == "" || port == "" || path == "")
            {
                MessageBox.Show("Please fill in all fields.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CreateIISWebsite(siteName, siteName, path, port);
        }

        private void CreateIISWebsite(string siteName, string appPoolName, string physicalPath, string portNumber)
        {
            try
            {
                using var mgr = new ServerManager();
                if (!int.TryParse(portNumber, out var port))
                {
                    MessageBox.Show($"Invalid port: {portNumber}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (mgr.Sites.Any(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"Site '{siteName}' already exists.", "Duplicate Site",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var inUse = mgr.Sites.SelectMany(s => s.Bindings)
                    .Any(b => b.EndPoint != null && b.EndPoint.Port == port);
                if (inUse)
                {
                    MessageBox.Show($"Port {port} is already in use.", "Port In Use",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _appPoolName = appPoolName;
                var pool = mgr.ApplicationPools[appPoolName] ?? mgr.ApplicationPools.Add(appPoolName);
                pool.ManagedRuntimeVersion = "v4.0";
                pool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
                pool.ProcessModel.IdentityType = ProcessModelIdentityType.NetworkService;

                var site = mgr.Sites.Add(siteName, "http", $"*:{port}:", physicalPath);
                site.ApplicationDefaults.ApplicationPoolName = appPoolName;
                site.ServerAutoStart = true;

                var cfg = mgr.GetApplicationHostConfiguration();
                cfg.GetSection("system.webServer/security/authentication/anonymousAuthentication", siteName)
                    ["enabled"] = rbAnonymous.Checked;
                cfg.GetSection("system.webServer/security/authentication/windowsAuthentication", siteName)
                    ["enabled"] = rbWindowsAuth.Checked;

                mgr.CommitChanges();
                System.Threading.Thread.Sleep(500);
                site.Start();

                MessageBox.Show(
                    $"Site '{siteName}' created on port {port}.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                btnCreate.Enabled = false;
                CreateClicked?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "IIS Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SetAppPoolIdentity(string userName, string password)
        {
            if (string.IsNullOrEmpty(_appPoolName))
                throw new InvalidOperationException("No app-pool has been created or selected yet.");

            using var mgr = new ServerManager();
            var pool = mgr.ApplicationPools[_appPoolName]
                       ?? throw new InvalidOperationException($"AppPool '{_appPoolName}' not found.");

            if (string.Equals(userName, @"NT AUTHORITY\NETWORK SERVICE", StringComparison.OrdinalIgnoreCase))
            {
                pool.ProcessModel.IdentityType = ProcessModelIdentityType.NetworkService;
            }
            else
            {
                pool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                pool.ProcessModel.UserName = userName;
                pool.ProcessModel.Password = password;
            }

            mgr.CommitChanges();
        }

        private void AddHelp(Control anchor, string tooltip)
        {
            const int iconSize = 16;
            var pic = new PictureBox
            {
                Image = new Bitmap(questionBmp, new Size(iconSize, iconSize)),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(iconSize, iconSize),
                Cursor = Cursors.Default
            };
            tt.SetToolTip(pic, tooltip);

            Control scrollContainer = GetScrollableParent(anchor);

            void Position()
            {
                if (scrollContainer == null) return;

                // Translate anchor’s position to scroll container coordinates
                Point location = scrollContainer.PointToClient(anchor.Parent.PointToScreen(anchor.Location));
                int x = location.X + anchor.Width + 4;
                int y = location.Y + (anchor.Height - iconSize) / 2;

                pic.Location = new Point(x, y);
            }

            anchor.SizeChanged += (_, __) => Position();
            anchor.LocationChanged += (_, __) => Position();
            anchor.Parent.Layout += (_, __) => Position();
            scrollContainer?.Controls.Add(pic);
            pic.BringToFront();
            Position();
        }

        private Control GetScrollableParent(Control control)
        {
            while (control != null)
            {
                if (control is Panel p && p.AutoScroll)
                    return p;
                control = control.Parent;
            }

            return null;
        }
        
        private void UpdateFolderWarning()
        {
            if (rbUseExistingSite.Checked &&
                !string.IsNullOrWhiteSpace(ExpectedFolder) &&
                !string.IsNullOrWhiteSpace(txtPath.Text))
            {
                string chosen = Path.GetFullPath(ExpectedFolder).TrimEnd('\\', '/');
                string site = Path.GetFullPath(txtPath.Text).TrimEnd('\\', '/');
                lblFolderWarning.Visible = !string.Equals(chosen, site, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                lblFolderWarning.Visible = false;
            }
        }
    }
}