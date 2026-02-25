using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Windows.Forms;

namespace Engrafo_1_Installer
{
    public partial class LocalInstallPanel : UserControl
    {
        public event EventHandler BackClicked;
        public event EventHandler NextClicked;

        // ── UI controls ────────────────────────────────────────────
        private Label lblTitle;
        private Label lblAdminStatus, lblIISStatus, /*lblNet8Status,*/ lblHostingStatus;
        private Button btnRefresh, btnBack, btnNext;
        private ToolTip _tip = new ToolTip();

        public LocalInstallPanel()
        {
            InitializeInstallComponent();
            UpdateStatuses();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
                UpdateStatuses();
        }

        private void InitializeInstallComponent()
        {
            const int margin = 20;
            const int iconSize = 16;
            const int indent = 24;
            const int spacingBetween = 10;

            // Fill parent
            this.Dock = DockStyle.Fill;

            // ── Main layout (labels + Refresh button) ──────────────────────────
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 6, // bumped from 6 → 7
                Padding = new Padding(margin)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // title
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // admin
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // IIS
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // hosting
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Refresh button ← NEW
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // filler

            // Title
            lblTitle = new Label
            {
                Text = "Local Installation Prerequisites\r\n" +
                       "The ASP.NET Core Runtime enables you to run existing web/server applications. On Windows, we recommend installing the Hosting Bundle, which includes the .NET Runtime and IIS support.\r\n"+
                       " ",
                Font = new Font(this.Font, FontStyle.Bold),
                Dock = DockStyle.Top,
                AutoSize = true
            };
            tbl.Controls.Add(lblTitle, 0, 0);

            // Status lines
            lblAdminStatus = MakeStatusLabel(indent);
            lblIISStatus = MakeStatusLabel(indent);
            // lblNet8Status = MakeStatusLabel(indent);
            lblHostingStatus = MakeStatusLabel(indent);

            tbl.Controls.Add(lblAdminStatus, 0, 1);
            tbl.Controls.Add(lblIISStatus, 0, 2);
            // tbl.Controls.Add(lblNet8Status,    0, 3);
            tbl.Controls.Add(lblHostingStatus, 0, 4);

            // ── Refresh button in its own row ─────────────────────────
            btnRefresh = new Button
            {
                Text = "Refresh",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Left
            };
            btnRefresh.Click += (_, __) => UpdateStatuses();
            tbl.Controls.Add(btnRefresh, 0, 5); // placed in new 5th row

            Controls.Add(tbl);

            // ── Footer buttons (Back / Next) ─────────────────────────
            btnBack = new Button { Text = "← Back", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnNext = new Button
                { Text = "Next →", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Enabled = true }; //button here is enabled at satart so it does not need the requiremnts to be fulfilled

            btnBack.Click += (_, __) => BackClicked?.Invoke(this, EventArgs.Empty);
            btnNext.Click += (_, __) => NextClicked?.Invoke(this, EventArgs.Empty);

            Controls.Add(btnBack);
            Controls.Add(btnNext);
            
            btnBack   .BringToFront();
            btnNext   .BringToFront();

            LayoutHelper.PlaceFooterButtons(this, btnNext, btnBack, null, margin, spacingBetween);
            // ── Positioning helpers ───────────────────────────────
            void PositionRefresh()
            {
                // place just below the last status label:
                int x = margin;
                int y = lblHostingStatus.Bottom + spacingBetween;
                btnRefresh.Location = new Point(x, y);
                btnRefresh.BringToFront();
            }

            // initial placement
            PositionRefresh();
            LayoutHelper.PlaceFooterButtons(this, btnNext, btnBack, null, margin, spacingBetween);

            // update on resize/visibility
            this.Resize += (s, e) =>
            {
                PositionRefresh();
                LayoutHelper.PlaceFooterButtons(this, btnNext, btnBack, null, margin, spacingBetween);
            };
            this.VisibleChanged += (s, e) =>
            {
                if (!Visible) return;
                PositionRefresh();
                LayoutHelper.PlaceFooterButtons(this, btnNext, btnBack, null, margin, spacingBetween);
            };

            // ── Help icons ────────────────────────────────────────────
            AddHelp(lblAdminStatus,
                "The installation app must be run as a Windows Administrator to update the web server and connection string",
                iconSize);

            AddHelp(lblIISStatus,
                "IIS web-server (W3SVC) service must be installed and running.\n" +
                "See guide: https://engrafo.atlassian.net/wiki/spaces/ED/pages/21168129/Installation+-+server+desktop#Installation-steps-for-Internet-Information-Server(IIS)",
                iconSize,
                "https://engrafo.atlassian.net/wiki/spaces/ED/pages/21168129/Installation+-+server+desktop#Installation-steps-for-Internet-Information-Server(IIS)");

            AddHelp(lblHostingStatus,
                "Requires the ASP.NET Core Hosting Bundle (aspnetcorev2.dll).\n" +
                "Click for Direct download: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.15-windows-hosting-bundle-installer",
                iconSize,
                "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.15-windows-hosting-bundle-installer");
        }


        private Label MakeStatusLabel(int indent)
        {
            return new Label
            {
                Text = "",
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(indent, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private void UpdateStatuses()
        {
            bool isAdmin = IsAdministrator();
            bool iisOn = CheckIISStatus();
            // bool net8 = CheckDotNet8Runtime();
            bool host = CheckHostingBundle();

            lblAdminStatus.Text = FormatStatus(isAdmin, "Running as administrator");
            lblAdminStatus.ForeColor = isAdmin ? Color.Green : Color.Red;

            lblIISStatus.Text = FormatStatus(iisOn, "IIS (W3SVC) is running");
            lblIISStatus.ForeColor = iisOn ? Color.Green : Color.Red;

            //lblNet8Status.Text      = FormatStatus(net8,  ".NET 8.0 runtime is installed");
            //lblNet8Status.ForeColor = net8 ? Color.Green : Color.Red;

            lblHostingStatus.Text = FormatStatus(host, "ASP.NET Core Hosting Bundle is installed");
            lblHostingStatus.ForeColor = host ? Color.Green : Color.Red;

            btnNext.Enabled = true;
        }

        private string FormatStatus(bool ok, string msg) =>
            (ok ? "✔ " : "✖ ") + msg;

        private bool IsAdministrator()
        {
            using var id = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(id)
                .IsInRole(WindowsBuiltInRole.Administrator);
        }

        private bool CheckIISStatus()
        {
            try
            {
                using var sc = new ServiceController("W3SVC");
                return sc.Status == ServiceControllerStatus.Running;
            }
            catch
            {
                return false;
            }
        }

        // private bool CheckDotNet8Runtime()
        // {
        //     try
        //     {
        //         var psi = new ProcessStartInfo("dotnet", "--list-runtimes")
        //         {
        //             UseShellExecute = false,
        //             RedirectStandardOutput = true,
        //             CreateNoWindow = true
        //         };
        //         using var proc = Process.Start(psi);
        //         var output = proc.StandardOutput.ReadToEnd();
        //         proc.WaitForExit();
        //         return output
        //             .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        //             .Any(l => l.StartsWith("Microsoft.NETCore.App ") && l.Contains("8."));
        //     }
        //     catch
        //     {
        //         return false;
        //     }
        // }

        private bool CheckHostingBundle()
        {
            try
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "IIS",
                    "Asp.Net Core Module",
                    "V2",
                    "aspnetcorev2.dll"
                );
                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        private void AddHelp(Control anchor, string tooltip, int iconSize, string url = null)
        {
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
                Cursor = string.IsNullOrEmpty(url) ? Cursors.Default : Cursors.Hand
            };
            _tip.SetToolTip(pic, tooltip);

            if (!string.IsNullOrEmpty(url))
                pic.Click += (_, __) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            void Position()
            {
                var textSize = TextRenderer.MeasureText(
                    anchor.Text,
                    anchor.Font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding
                );
                int yOffset = anchor.Top + (anchor.Height - iconSize) / 2;
                pic.Location = new Point(anchor.Left + textSize.Width + 6, yOffset);
            }

            anchor.TextChanged += (_, __) => Position();
            anchor.SizeChanged += (_, __) => Position();
            Position();

            Controls.Add(pic);
            pic.BringToFront();
        }
    }
}