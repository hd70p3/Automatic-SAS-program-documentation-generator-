using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Security.AccessControl;
using System.DirectoryServices.AccountManagement;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace Engrafo_1_Installer
{
    public partial class UserOwnerPanel : UserControl
    {
        public event EventHandler BackClicked;
        public event EventHandler<(string svcUser, string svcPassword)> OwnerChosen;

        // ── UI ────────────────────────────────────────────────────────────
        private RadioButton rbUseExisting, rbCreateNew;
        private Label lblExistingUser;
        private ComboBox cbExistingUsers;

        private Label lblUsername;
        private TextBox txtUsername;
        private Label lblPassword;
        private TextBox txtPassword;
        private Label lblConfirm;
        private TextBox txtConfirmPassword;

        private Label lblStatus;

        private Button btnBack, btnCreate;
        private Label lblDomain;
        private TextBox txtDomain;

        private CheckBox chkSqlGrant;

        private ToolTip _tip = new ToolTip();
        private readonly Dictionary<Control, PictureBox> _helpIcons = new();

        public string AppFolder { get; set; }


        public string ConnectionString { get; set; }

        public UserOwnerPanel()
        {
            InitializeOwnerComponent();
            LoadLocalAccounts();
            UpdateModeUI();
        }

        private void InitializeOwnerComponent()
        {
            Dock = DockStyle.Fill;
            const int margin = 20;
            const int spacing = 10;
            
            // Instantiate and hook up the nav buttons
            btnBack = new Button { Text = "← Back", AutoSize = true , AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnCreate = new Button { Text = "Create / Use →", AutoSize = true , AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnBack.Click += (_, __) => BackClicked?.Invoke(this, EventArgs.Empty);
            btnCreate.Click += BtnCreate_Click;

            // Create the main table: 1 column, dynamic rows
            var mainTbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20),
                ColumnCount = 1,
                RowCount = 8
            };
            mainTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 7; i++)
                mainTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainTbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // filler

            // 0: Mode selection
            var modeFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top
            };
            rbUseExisting = new RadioButton { Text = "Use existing account", AutoSize = true };
            rbCreateNew = new RadioButton { Text = "Create new service account", AutoSize = true, Checked = true };
            rbUseExisting.CheckedChanged += (_, __) => UpdateModeUI();
            rbCreateNew.CheckedChanged += (_, __) => UpdateModeUI();
            modeFlow.Controls.AddRange(new Control[] { rbUseExisting, rbCreateNew });
            mainTbl.Controls.Add(modeFlow, 0, 0);

            // 1: Existing-account row
            var existTbl = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 10, 0, 10)
            };
            existTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            existTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            lblExistingUser = new Label
                { Text = "Select Windows account:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top };
            cbExistingUsers = new ComboBox
                { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Top,
                MinimumSize = new Size(200, 0), 
                Width = 250
            };
            existTbl.Controls.Add(lblExistingUser, 0, 0);
            existTbl.Controls.Add(cbExistingUsers, 1, 0);
            mainTbl.Controls.Add(existTbl, 0, 1);

            // 2: Domain & new-account fields (all together in one 2-col table; we'll hide/show)
            var newTbl = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 10)
            };
            newTbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            newTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            lblDomain = new Label { Text = "Domain:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtDomain = new TextBox { Anchor = AnchorStyles.Left, Width = 250, Text = Environment.MachineName };
            lblUsername = new Label { Text = "New username:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtUsername = new TextBox { Anchor = AnchorStyles.Left, Width = 250 };
            lblPassword = new Label { Text = "Password:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtPassword = new TextBox { Anchor = AnchorStyles.Left, Width = 250, UseSystemPasswordChar = true };
            lblConfirm = new Label { Text = "Confirm password:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtConfirmPassword = new TextBox { Anchor = AnchorStyles.Left, Width = 250, UseSystemPasswordChar = true };

            newTbl.Controls.Add(lblDomain, 0, 0);
            newTbl.Controls.Add(txtDomain, 1, 0);
            newTbl.Controls.Add(lblUsername, 0, 1);
            newTbl.Controls.Add(txtUsername, 1, 1);
            newTbl.Controls.Add(lblPassword, 0, 2);
            newTbl.Controls.Add(txtPassword, 1, 2);
            newTbl.Controls.Add(lblConfirm, 0, 3);
            newTbl.Controls.Add(txtConfirmPassword, 1, 3);
            mainTbl.Controls.Add(newTbl, 0, 2);

            // 3: SQL-grant checkbox
            chkSqlGrant = new CheckBox
            {
                Text = "Enable SQL account & permissions setup (recommended for local/Docker DB)",
                AutoSize = true,
                Checked = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 10)
            };
            mainTbl.Controls.Add(chkSqlGrant, 0, 3);

            // 4: Status label
            lblStatus = new Label
            {
                Text = "",
                ForeColor = Color.Blue,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 10),
                Visible = false
            };
            mainTbl.Controls.Add(lblStatus, 0, 4);

            // 5: filler (absorbs extra space)
            mainTbl.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 5);

            

            Controls.Add(mainTbl);
            Controls.Add(btnBack);
            Controls.Add(btnCreate);

            LayoutHelper.PlaceFooterButtons(this, btnCreate, btnBack, null, margin, spacing);
            btnBack.BringToFront();  
            btnCreate.BringToFront(); 

            this.VisibleChanged += (s, e) =>
            {
                if (Visible)
                    LayoutHelper.PlaceFooterButtons(this, btnCreate, btnBack, null, margin, spacing);
            };

            AddHelp(rbCreateNew,    "Pick an existing Windows account to run the site under, " +
                                    "or create a brand-new local Windows service account.");


            AddHelp(cbExistingUsers,      "Select which local Windows account should own the files and app pool.");
            AddHelp(txtDomain,            "Domain or machine name for the new account. The pre filled name is your local machine name.");
            AddHelp(txtUsername,          "New service account username.");
            AddHelp(txtPassword,          "Password for that account.");
            AddHelp(txtConfirmPassword,   "Re-enter the password to confirm.");
            AddHelp(
                chkSqlGrant,
                "If enabled, the selected account will be put as the db_owner on the SQL Server for the Engrafo database."
            );

            
            
            // DisplayHelper.AdjustControlForDpi(this);
        }

        private void UpdateModeUI()
        {
            bool existing = rbUseExisting.Checked;
            if (existing) LoadLocalAccounts();

            lblExistingUser.Visible = cbExistingUsers.Visible = existing;
            lblDomain.Visible =
                txtDomain.Visible =
                    lblUsername.Visible =
                        txtUsername.Visible =
                            lblPassword.Visible =
                                txtPassword.Visible =
                                    lblConfirm.Visible =
                                        txtConfirmPassword.Visible
                                            = !existing;
            foreach (var kvp in _helpIcons)
            {
                kvp.Value.Visible = kvp.Key.Visible;
            }
        }
        private void LoadLocalAccounts()
        {
            cbExistingUsers.Items.Clear();

            // hard code the network service option - not recommended
            cbExistingUsers.Items.Add(@"NT AUTHORITY\NETWORK SERVICE");

            // enumerate local SAM accounts
            using var ctx = new PrincipalContext(ContextType.Machine);
            using var search = new PrincipalSearcher(new UserPrincipal(ctx));
            foreach (UserPrincipal u in search.FindAll())
            {
                if (!string.IsNullOrEmpty(u.SamAccountName))
                    cbExistingUsers.Items.Add(Environment.MachineName + "\\" + u.SamAccountName);
            }

            if (cbExistingUsers.Items.Count > 0)
                cbExistingUsers.SelectedIndex = 0;
            using (var g = cbExistingUsers.CreateGraphics())
            {
                int maxWidth = 0;
                foreach (var item in cbExistingUsers.Items)
                {
                    var size = g.MeasureString(item.ToString(), cbExistingUsers.Font);
                    maxWidth = Math.Max(maxWidth, (int)size.Width);
                }

                // Add some padding to account for arrow and margins
                maxWidth += SystemInformation.VerticalScrollBarWidth + 20;
                cbExistingUsers.Width = maxWidth;
            }
        }

        private async void BtnCreate_Click(object sender, EventArgs e)
        {
            // 1) Figure out which Windows account we're using (existing vs. new)
            bool existing = rbUseExisting.Checked;
            string windowsUser;
            string password = null;

            try
            {
                if (existing)
                {
                    // 1) Pick the selected user
                    if (cbExistingUsers.SelectedItem == null)
                    {
                        MessageBox.Show("Please select an existing account.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    windowsUser = cbExistingUsers.SelectedItem.ToString();

                    // 2) If it's NETWORK SERVICE or SQL-grant is disabled, skip password prompt
                    if (windowsUser.Equals(@"NT AUTHORITY\NETWORK SERVICE", StringComparison.OrdinalIgnoreCase)
                        || !chkSqlGrant.Checked)
                    {
                        // nothing to do here—password stays null
                    }
                    else
                    {
                        // 3) Otherwise do the usual validate-credentials loop
                        var parts = windowsUser.Split('\\');
                        if (parts.Length != 2)
                        {
                            MessageBox.Show("Invalid account format.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        var ctxName = parts[0];
                        var userName = parts[1];
                        var ctxType = ctxName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                            ? ContextType.Machine
                            : ContextType.Domain;

                        using var pc = new PrincipalContext(ctxType, ctxName);

                        // disable UI & show status
                        btnCreate.Enabled = btnBack.Enabled = false;
                        lblStatus.Text = "Checking credentials for existing user…";
                        lblStatus.Visible = true;

                        while (true)
                        {
                            var winPwd = Microsoft.VisualBasic.Interaction.InputBox(
                                $"Enter the Windows password for service account:\n{windowsUser}",
                                "Password Required", "");
                            if (string.IsNullOrWhiteSpace(winPwd))
                            {
                                MessageBox.Show("Operation cancelled – password required.", "Cancelled",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                lblStatus.Visible = false;
                                btnCreate.Enabled = btnBack.Enabled = true;
                                return;
                            }

                            bool valid = false;
                            try
                            {
                                // run ValidateCredentials off the UI thread
                                valid = await Task.Run(() =>
                                    pc.ValidateCredentials(userName, winPwd, ContextOptions.Negotiate));
                            }
                            catch
                            {
                                valid = false;
                            }

                            if (valid)
                            {
                                password = winPwd;
                                break;
                            }
                            else
                            {
                                MessageBox.Show("That Windows password is incorrect. Please try again.",
                                    "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }

                        // restore UI
                        lblStatus.Visible = false;
                        btnCreate.Enabled = btnBack.Enabled = true;
                    }
                }
                else // Create new
                {
                    var domain = txtDomain.Text.Trim();
                    var localSam = txtUsername.Text.Trim();
                    var confirm = txtConfirmPassword.Text;
                    password = txtPassword.Text;

                    if (string.IsNullOrEmpty(domain) ||
                        string.IsNullOrEmpty(localSam) ||
                        string.IsNullOrEmpty(password) ||
                        string.IsNullOrEmpty(confirm))
                    {
                        MessageBox.Show("Please fill in all fields.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (password != confirm)
                    {
                        MessageBox.Show("Passwords do not match.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // create the SAM account by localSam only…
                    using var ctx = new PrincipalContext(ContextType.Machine);
                    if (UserPrincipal.FindByIdentity(ctx, localSam) != null)
                        throw new InvalidOperationException($"User '{localSam}' already exists.");

                    var u = new UserPrincipal(ctx)
                    {
                        SamAccountName = localSam,
                        Description = "Engrafo service account",
                        Enabled = true
                    };
                    u.SetPassword(password);
                    u.Save();

                    // full Windows principal:
                    windowsUser = $"{domain}\\{localSam}";
                }
            }
            catch (InvalidOperationException ioe)
            {
                // e.g. “user already exists”
                MessageBox.Show(ioe.Message, "Creation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception ex)
            {
                // any other unexpected failure
                MessageBox.Show($"Unexpected error:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2) Grant NTFS rights on the AppFolder to the full Windows name
            try
            {
                if (string.IsNullOrEmpty(AppFolder) || !Directory.Exists(AppFolder))
                    throw new DirectoryNotFoundException($"App folder not found: {AppFolder}");

                var di = new DirectoryInfo(AppFolder);
                var acl = di.GetAccessControl();
                var rule = new FileSystemAccessRule(
                    windowsUser,
                    FileSystemRights.Modify | FileSystemRights.ReadAndExecute | FileSystemRights.ListDirectory,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);
                acl.AddAccessRule(rule);
                di.SetAccessControl(acl);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting folder permissions:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 3) Grant database permissions — ONLY if enabled
            if (chkSqlGrant.Checked)
            {
                var csb = new SqlConnectionStringBuilder(this.ConnectionString);
                bool windowsAuth = csb.IntegratedSecurity || windowsUser.Equals(@"NT AUTHORITY\NETWORK SERVICE", StringComparison.OrdinalIgnoreCase);;
                string dbName = csb.InitialCatalog.Replace("]", "]]");

                try
                {
                    using var conn = new SqlConnection(this.ConnectionString);
                    conn.Open();
                    using var cmd = conn.CreateCommand();

                    if (!windowsAuth)
                    {
                        // SQL‐auth (Docker): strip off DOMAIN\ to get a valid login name
                        var sam = windowsUser.Contains("\\")
                            ? windowsUser.Substring(windowsUser.IndexOf("\\") + 1)
                            : windowsUser;
                        var safeLog = sam.Replace("]", "]]");
                        var safePwd = password.Replace("'", "''");

                        cmd.CommandText = $@"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{safeLog}')
    CREATE LOGIN [{safeLog}] WITH PASSWORD = N'{safePwd}';

USE [{dbName}];

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{safeLog}')
    CREATE USER [{safeLog}] FOR LOGIN [{safeLog}];

EXEC sp_addrolemember N'db_owner', N'{safeLog}';
";
                    }
                    else
                    {
                        // Windows-auth: can do CREATE LOGIN … FROM WINDOWS
                        var safeUser = windowsUser.Replace("]", "]]");

                        cmd.CommandText = $@"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{safeUser}')
    CREATE LOGIN [{safeUser}] FROM WINDOWS;

USE [{dbName}];

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{safeUser}')
    CREATE USER [{safeUser}] FOR LOGIN [{safeUser}];

EXEC sp_addrolemember N'db_owner', N'{safeUser}';
";
                    }

                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error setting database permissions:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // 4) Success!
            MessageBox.Show(
                existing
                    ? $"Permissions granted for existing account:\n{windowsUser}"
                    : $"Account created and permissions granted:\n{windowsUser}",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Raise your event so host can also configure the IIS App Pool identity
            OwnerChosen?.Invoke(this, (windowsUser, password));
        }


        private void CreateLocalUser(string userName, string password)
        {
            using var ctx = new PrincipalContext(ContextType.Machine);
            if (UserPrincipal.FindByIdentity(ctx, userName) != null)
                throw new InvalidOperationException($"User '{userName}' already exists.");

            var u = new UserPrincipal(ctx)
            {
                SamAccountName = userName,
                Description = "Engrafo service account",
                Enabled = true
            };
            u.SetPassword(password);
            u.Save();
        }

        private void GrantFolderPermissions(string userName)
        {
            if (string.IsNullOrEmpty(AppFolder) || !Directory.Exists(AppFolder))
                throw new DirectoryNotFoundException($"App folder not found: {AppFolder}");

            var di = new DirectoryInfo(AppFolder);
            var acl = di.GetAccessControl();
            var rule = new FileSystemAccessRule(
                userName,
                FileSystemRights.Modify | FileSystemRights.ReadAndExecute | FileSystemRights.ListDirectory,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow);
            acl.AddAccessRule(rule);
            di.SetAccessControl(acl);
        }

        private void GrantDatabasePermissions_New(string userName, string password)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            var db = conn.Database.Replace("]", "]]");
            var safe = userName.Replace("]", "]]");
            var pw = password.Replace("'", "''");

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{safe}')
    CREATE LOGIN [{safe}] WITH PASSWORD = N'{pw}';

USE [{db}];

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{safe}')
    CREATE USER [{safe}] FOR LOGIN [{safe}];

EXEC sp_addrolemember N'db_owner', N'{safe}';";
            cmd.ExecuteNonQuery();
        }

        private void GrantDatabasePermissions_Existing(string userName)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            var db = conn.Database.Replace("]", "]]");
            var safe = userName.Replace("]", "]]");

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{safe}')
    CREATE LOGIN [{safe}] FROM WINDOWS;

USE [{db}];

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{safe}')
    CREATE USER [{safe}] FOR LOGIN [{safe}];

EXEC sp_addrolemember N'db_owner', N'{safe}';";
            cmd.ExecuteNonQuery();
        }
        
        private PictureBox AddHelp(Control anchor, string tooltip, int extraOffset = 4)
        {
            const int iconSize = 16;
            Bitmap bmp;
            try
            {
                var asm  = GetType().Assembly;
                var name = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("Icon-round-Question_mark.svg.png", StringComparison.OrdinalIgnoreCase));
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
                Image    = new Bitmap(bmp, new Size(iconSize, iconSize)),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size     = new Size(iconSize, iconSize),
                Cursor   = Cursors.Help
            };
            _tip.SetToolTip(pic, tooltip);

            void Position()
            {
                if (anchor is CheckBox cb)
                {
                    using (var g = cb.CreateGraphics())
                    {
                        SizeF textSize = g.MeasureString(cb.Text, cb.Font);

                        
                        int iconX = cb.Left + (int)textSize.Width + 2; 

                       
                        int iconY = cb.Top + (cb.Height - iconSize) / 2 - 6; 

                        var screenPt = cb.Parent.PointToScreen(new Point(iconX, iconY));
                        pic.Location = this.PointToClient(screenPt);
                    }
                }
                else
                {
                    // All your previous controls
                    var screenPt = anchor.Parent.PointToScreen(new Point(
                        anchor.Right + extraOffset,
                        anchor.Top + (anchor.Height - iconSize) / 2
                    ));
                    pic.Location = this.PointToClient(screenPt);
                }
            }

            anchor.SizeChanged     += (s, e) => Position();
            anchor.LocationChanged += (s, e) => Position();
            this.Layout            += (s, e) => Position();
            Position();

            Controls.Add(pic);
            pic.BringToFront();

            // remember it so you can toggle Visible later
            _helpIcons[anchor] = pic;
            return pic;
        }

        
    }
}