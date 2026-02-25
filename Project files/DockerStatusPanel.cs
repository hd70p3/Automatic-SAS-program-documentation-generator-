using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Engrafo_1_Installer
{
    public partial class DockerStatusPanel : UserControl
    {
        private Label lblStatus;
        private Button btnBack;
        private Button btnNext;
        private Button btnRefresh;
        private Timer refreshTimer;
        private bool isChecking = false; // Prevent overlapping checks
        private bool isFirstAttempt = true; // Longer timeout on first check
        
        private Timer statusAnimTimer;
        private int animDotCount = 1;
        private string baseStatusText = "Checking Docker";

        public event EventHandler BackClicked;
        public event EventHandler NextClicked;

        public DockerStatusPanel()
        {
            InitializeDockerStatusComponent();
        }

        private void InitializeDockerStatusComponent()
        {
            // ── Status label ───────────────────────────
            lblStatus = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10, 5, 10, 5),
                Text = "Checking Docker…",
                TextAlign = ContentAlignment.MiddleLeft,
            };

            // ── Back button ────────────────────────────
            btnBack = new Button
            {
                Text = "← Back",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            btnBack.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);

            // ── Next button ────────────────────────────
            btnNext = new Button
            {
                Text = "Next →",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            btnNext.Click += (s, e) => NextClicked?.Invoke(this, EventArgs.Empty);

            // ── Refresh button ─────────────────────────
            btnRefresh = new Button
            {
                Text = "Refresh",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            btnRefresh.Click += (s, e) => UpdateStatus();

            // ── Apply DPI scaling to buttons ───────────
            DisplayHelper.AdjustControlForDpi(btnBack);
            DisplayHelper.AdjustControlForDpi(btnNext);
            DisplayHelper.AdjustControlForDpi(btnRefresh);

            // ── Add controls ───────────────────────────
            Controls.Add(lblStatus);
            Controls.Add(btnBack);
            Controls.Add(btnNext);
            Controls.Add(btnRefresh);

            // ── Timer for periodic checking ────────────
            refreshTimer = new Timer
            {
                Interval = 2000 // 2 seconds between checks
            };
            refreshTimer.Tick += (s, e) => UpdateStatus();

            // ── Final tweaks ───────────────────────────
            Name = "DockerStatusPanel";
            MinimumSize = new Size(250, 100);
            
            statusAnimTimer = new Timer
            {
                Interval = 400 // ms, adjust for faster/slower animation
            };
            statusAnimTimer.Tick += (s, e) => UpdateStatusAnimation();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Layout buttons now
            LayoutHelper.PlaceFooterButtons(this, btnNext, btnBack, btnRefresh);

            // Ensure buttons are shown early
            btnBack.Visible = true;
            btnNext.Visible = true;
            btnRefresh.Visible = true;
        }

        public async void UpdateStatus()
        {
            if (isChecking) return;
            isChecking = true;

            animDotCount = 1;

            animDotCount = 1;
            baseStatusText = "Checking Docker";
            lblStatus.Text = baseStatusText + ".";
            lblStatus.ForeColor = Color.Black;
            statusAnimTimer.Start();
            // btnRefresh.Enabled = false; // No longer disabling refresh button

            bool up = await IsDockerAvailableAndRunningAsync();

            if (!this.IsHandleCreated || this.IsDisposed)
            {
                isChecking = false;
                return;
            }

            statusAnimTimer.Stop();

            
            if (up)
            {
                lblStatus.Text = "✔ Docker is installed and the daemon is running.";
                lblStatus.ForeColor = Color.Green;
                refreshTimer.Stop();
                // btnRefresh.Enabled = true;
            }
            else
            {
                lblStatus.Text = "❌ Docker not available or daemon not running.";
                lblStatus.ForeColor = Color.Red;
                // btnRefresh.Enabled = true;
            }
            isChecking = false;
        }

        private async Task<bool> IsDockerAvailableAndRunningAsync()
        {
            try
            {
                var psi = new ProcessStartInfo("docker", "info")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    return false;
                }

                var timeoutMs = isFirstAttempt ? 15000 : 5000;
                isFirstAttempt = false;
                var waitTask = proc.WaitForExitAsync();
                var finished = await Task.WhenAny(waitTask, Task.Delay(timeoutMs));
                if (finished != waitTask)
                {
                    try { proc.Kill(); } catch { }
                    return false;
                }

                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            LayoutHelper.PlaceFooterButtons(this, btnNext, btnBack, btnRefresh);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                refreshTimer.Stop();  // Defensive: stop if already running
                refreshTimer.Start(); // Start periodic status checking
                UpdateStatus();       // Do an immediate check on show
            }
            else
            {
                refreshTimer.Stop(); // Stop checking if panel is hidden
            }
        }
        private void UpdateStatusAnimation()
        {
            animDotCount = (animDotCount % 3) + 1; // Cycle through 1-3
            lblStatus.Text = baseStatusText + new string('.', animDotCount);
        }

    }
}
