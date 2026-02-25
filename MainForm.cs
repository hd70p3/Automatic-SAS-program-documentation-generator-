using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.Administration;
using Application = System.Windows.Forms.Application;

namespace Engrafo_1_Installer
{
    public partial class MainForm : Form
    {
        // ── Navigation stack ──────────────────────────
        private readonly Stack<Control> _navStack = new Stack<Control>();

        // ── Panels ─────────────────────────────────────
        private ChoicePanel panelChoice;
        private Panel panelConfirm;
        
        private DockerSetupNewDB dockerSetupNewDb;
        private DockerLaunchPanel dockerLaunchPanel;
        
        private LocalInstallPanel panelLocal;
        private DownloadPanel panelDownload;
        private IisSetupPanel iisSetupPanel;
        private AppSettingsPanel appSettingsPanel;
        private UserOwnerPanel panelOwner;
        private LaunchAppPanel launchPanel;
        private DockerOptionPanel dockerOptionPanel;
        private DockerSetupExistingDB dockerSetupExistingDb;
        private DockerSetupLocalMSSQLPanel dockerSetupLocalMssqlPanel;



// ── Step2: Docker-check controls ────────────────────
        private Panel panelDockerCheck;
        private Label lblDockerCheckTitle;
        
        // ── Confirm controls ───────────────────────────
        private Label lblConfirmText;
        private Button btnConfirmYes;
        private Button btnConfirmNo;
        private bool pendingDockerChoice;
        

        private string _connectionString;
        private string _appfolder;
        private int _iisPort;

        public MainForm()
        {
            InitializeComponent();
            Theme.Apply(this);

            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.SizeGripStyle = SizeGripStyle.Hide;
            _navStack.Push(panelChoice);
        }

        private void InitializeComponent()
        {
            this.ClientSize = new Size(750, 400);
            this.Text = "Engrafo Installer";
            var margin = 20;
            DisplayHelper.AdjustControlForDpi(this);


            panelConfirm = new Panel
            {
                Size = new Size(400, 150),
                Location = new Point((ClientSize.Width - 400) / 2, (ClientSize.Height - 150) / 2),
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            panelLocal = new LocalInstallPanel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            panelLocal.BackClicked += UniversalBack_Click;
            panelLocal.NextClicked += (s, e) => { NavigateTo(panelDownload); };
            panelDownload = new DownloadPanel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            panelDownload.BackClicked += UniversalBack_Click;
            panelDownload.DownloadCompleted += (s, path) =>
            {
                appSettingsPanel.ConfigurationFolder = path;
            };
            panelDownload.NextClicked += DownloadPanel_NextClicked;
            this.Controls.Add(panelDownload);


            appSettingsPanel = new AppSettingsPanel { Dock = DockStyle.Fill, Visible = false };
            appSettingsPanel.BackClicked += UniversalBack_Click;
            appSettingsPanel.AppSettingsUpdated += (s, args) =>
            {
                _appfolder = args.Folder;
                _connectionString = args.ConnectionString;
                iisSetupPanel.PhysicalFolder = _appfolder;
                iisSetupPanel.ExpectedFolder = _appfolder;
                NavigateTo(iisSetupPanel);
            };
            this.Controls.Add(appSettingsPanel);

            iisSetupPanel = new IisSetupPanel { Dock = DockStyle.Fill, Visible = false };
            iisSetupPanel.BackClicked += UniversalBack_Click;
            iisSetupPanel.CreateClicked += (s, e) =>
            {
                _iisPort = ((IisSetupPanel)s).PortNumber;

                panelOwner.AppFolder = _appfolder;
                panelOwner.ConnectionString = _connectionString;

                NavigateTo(panelOwner);
            };
            this.Controls.Add(iisSetupPanel);

            panelOwner = new UserOwnerPanel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            panelOwner.BackClicked += UniversalBack_Click;
            panelOwner.OwnerChosen += (s, creds) =>
            {
                var (svcUser, svcPassword) = creds;

                iisSetupPanel.SetAppPoolIdentity(svcUser, svcPassword);

                launchPanel.Port = _iisPort;
                launchPanel.AppFolder = _appfolder;
                NavigateTo(launchPanel);
            };
            this.Controls.Add(panelOwner);
            launchPanel = new LaunchAppPanel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            launchPanel.BackClicked += UniversalBack_Click;
            this.Controls.Add(launchPanel);

            // ── Step1: Choice panel ─────────────────────────────────────────────
            panelChoice = new ChoicePanel { Dock = DockStyle.Fill };
            panelChoice.NextClicked += (s, e) =>
            {
                pendingDockerChoice = panelChoice.IsDockerSelected;
                NavigateTo(panelConfirm);
            };
            this.Controls.Add(panelChoice);
            
            
            dockerSetupNewDb = new DockerSetupNewDB
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            dockerSetupNewDb.BackClicked += UniversalBack_Click;
            dockerSetupNewDb.DeployCompleted += (s, e) =>
            {
                int.TryParse(dockerSetupNewDb.AppPort, out int port);
                dockerLaunchPanel.Port = port;
                dockerLaunchPanel.AppFolder = dockerSetupNewDb.DirectoryPath;
                NavigateTo(dockerLaunchPanel);
            };
            this.Controls.Add(dockerSetupNewDb);
            
            
            dockerSetupExistingDb = new DockerSetupExistingDB
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            dockerSetupExistingDb.BackClicked += UniversalBack_Click;
            dockerSetupExistingDb.DeployCompleted += (s, e) =>
            {
                int.TryParse(dockerSetupExistingDb.AppPort, out int port);
                dockerLaunchPanel.Port = port;
                dockerLaunchPanel.AppFolder = dockerSetupExistingDb.DirectoryPath;
                NavigateTo(dockerLaunchPanel);
            };
            this.Controls.Add(dockerSetupExistingDb);
            
            dockerSetupLocalMssqlPanel = new DockerSetupLocalMSSQLPanel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            dockerSetupLocalMssqlPanel.BackClicked += UniversalBack_Click;
            dockerSetupLocalMssqlPanel.DeployCompleted += (s, e) =>
            {
                int.TryParse(dockerSetupLocalMssqlPanel.AppPort, out int port);
                dockerLaunchPanel.Port = port;
                dockerLaunchPanel.AppFolder = dockerSetupLocalMssqlPanel.DirectoryPath;
                NavigateTo(dockerLaunchPanel);
            };
            this.Controls.Add(dockerSetupLocalMssqlPanel);

            
            
            dockerLaunchPanel = new DockerLaunchPanel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            dockerLaunchPanel.BackClicked += UniversalBack_Click;
            this.Controls.Add(dockerLaunchPanel);



            // ──────────────────────────────────────────────────────────────────────
            // ── Step2: Confirm modal ─────────────────────────────────────────────
            lblConfirmText = new Label
            {
                Text = "Are you sure you want to choose this installation option?",
                Location = new Point(20, 20),
                Size = new Size(360, 50),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnConfirmYes = new Button
            {
                Text = "Yes",
                Location = new Point(80, 90),
                Size = new Size(80, 40)
            };
            btnConfirmYes.Click += ConfirmYes_Click;

            btnConfirmNo = new Button
            {
                Text = "No",
                Location = new Point(240, 90),
                Size = new Size(80, 40)
            };
            btnConfirmNo.Click += ConfirmNo_Click;

            panelConfirm.Controls.AddRange(new Control[]
            {
                lblConfirmText,
                btnConfirmYes,
                btnConfirmNo
            });

            // ── Step2: Docker-check ───────────────────────────────
            panelDockerCheck = new Panel { Dock = DockStyle.Fill, Visible = false };
            

// your new status panel
            var dockerStatusPanel = new DockerStatusPanel
            {
                Dock = DockStyle.Fill,   // ← fill the parent so Resize will fire
                Name = "dockerStatusPanel"
            };
            panelDockerCheck.Controls.AddRange(new Control[]
            {
                lblDockerCheckTitle,
                dockerStatusPanel,
            });
            this.Controls.Add(panelDockerCheck);
            dockerStatusPanel.BackClicked += UniversalBack_Click;
            dockerStatusPanel.NextClicked += (s,e) => NavigateTo(dockerOptionPanel);

            // ──────────────────────────────────────────────────────────────────────
            // ── Step3: Docker‐options ────────────────────────────────────────────
            dockerOptionPanel = new DockerOptionPanel();
            dockerOptionPanel.BackClicked += UniversalBack_Click;
            dockerOptionPanel.Option1Selected += (s, e) => NavigateTo(dockerSetupLocalMssqlPanel);
            dockerOptionPanel.Option2Selected += (s, e) => NavigateTo(dockerSetupExistingDb);
            dockerOptionPanel.Option3Selected += (s, e) => NavigateTo(dockerSetupNewDb);
            this.Controls.Add(dockerOptionPanel);
            
            

            // ──────────────────────────────────────────────────────────────────────
            // ── Step4: Docker detail ─────────────────────────────────────────────
            

            this.Controls.Add(panelConfirm);
            this.Controls.Add(panelLocal);
            this.Controls.Add(panelChoice);

            // Ensure the first panel is visible
            panelChoice.BringToFront();
        }

        // ── Universal “Back” pops you one step, unless you’re at the very first panel ─
        private void UniversalBack_Click(object sender, EventArgs e)
        {
            if (_navStack.Count <= 1) return;

            var current = _navStack.Pop();
            current.Visible = false;

            var previous = _navStack.Peek();
            previous.Visible = true;
            previous.BringToFront();
        }

        // ── NavigateTo hides current, shows next, and pushes it onto the stack ───────
        private void NavigateTo(Control next)
        {
            var current = _navStack.Peek();
            current.Visible = false;

            next.Visible = true;
            next.BringToFront();
            _navStack.Push(next);
        }

        private void DownloadPanel_NextClicked(object sender, EventArgs e)
        {
            NavigateTo(appSettingsPanel);
        }

        // ── Step2 Confirm → Either Docker‑options or Local ────────────────────────
        private void ConfirmYes_Click(object sender, EventArgs e)
        {
            panelConfirm.Visible = false;
            if (pendingDockerChoice)
                NavigateTo(panelDockerCheck);
            else
                NavigateTo(panelLocal);
        }


// ── When the page is first shown ──────────────────────────────
        

        private void ConfirmNo_Click(object sender, EventArgs e)
        {
            panelConfirm.Visible = false;
            NavigateTo(panelChoice);
        }
        
    }
}