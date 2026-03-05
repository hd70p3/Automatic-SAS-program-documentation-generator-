using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Engrafo_1_Installer
{
    public class ChoicePanel : UserControl
    {
        public event EventHandler NextClicked;
        public bool IsDockerSelected => rbDocker.Checked;

        // ── Controls ───────────────────────────────
        private RadioButton rbLocal, rbDocker;
        private Label lblLocalDesc, lblDockerDesc;
        private LinkLabel lblChoiceWarning;
        private Button btnNext;

        public ChoicePanel()
        {
            InitializeComponent();
            this.AutoScroll = true;

        }

        private void InitializeComponent()
        {
            const int margin = 20;
            const int indent = 24;

            // Top-level TableLayout: 1 column, 9 rows
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 9,
                Padding = new Padding(margin)
            };
            this.Controls.Add(tbl);
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 0: Local radio
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 1: Local desc
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute) { Height = 20 }); // 2: spacer
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 3: Docker radio
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 4: Docker desc
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute) { Height = 10 }); // 5: spacer
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 6: Warning
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // 7: filler
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 8: Buttons

            // ── Local option (indented)
            rbLocal = new RadioButton
            {
                Text   = "Windows Server/Desktop installation",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(indent, 0, 0, 0),
                Enabled = false // Disabled in this installer, contact@engrafo.eu for updated installer
            };
            tbl.Controls.Add(rbLocal, 0, 0);

            lblLocalDesc = new Label
            {
                Text = "(Disabled in this installer. Contact@engrafo.eu for more information.)\r\nThis will install Engrafo on a Windows server or desktop.\r\n" +
                       "Choosing this option, you will need to have access to an MS SQL database " +
                       "and a running Internet Information Server (IIS)\r\n"+
                       "If you don't have a MS SQL Server or a running IIS use the links below to download a MS SQL Server and get help on enabling IIS on windows",
                AutoSize = true,
                MaximumSize = new Size(650, 0),
                Margin = new Padding(indent, 0, 0, 0)
            };
            tbl.Controls.Add(lblLocalDesc, 0, 1);

            // ── Docker option (indented)
            rbDocker = new RadioButton
            {
                Text   = "Docker installation",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(indent, 0, 0, 0),
                Enabled = true 
            };
            tbl.Controls.Add(rbDocker, 0, 3);

            lblDockerDesc = new Label
            {
                Text = "This will install Engrafo using a Docker image\r\n" +
           "You can choose a complete installation with download of a SQL Server " +
           "or use your own SQL database\r\n" +
           "Docker Desktop must be running",
                AutoSize = true,
                MaximumSize = new Size(650, 0),
                Margin = new Padding(indent, 0, 0, 0),
                Enabled = true
            };
            tbl.Controls.Add(lblDockerDesc, 0, 4);

            // ── Warning (indented)
            lblChoiceWarning = new LinkLabel
            {
                AutoSize = true,
                MaximumSize = new Size(690, 0),
                ForeColor = Color.Black,
                Margin = new Padding(indent, 40, 0, 0)
            };
            lblChoiceWarning.Text =
                "Microsoft SQL Servers\r\n" +
                "How to enable IIS on Windows\r\n" +
                "Docker Desktop\r\n"+
                "Complete quide to manual installation of Engrafo";



            // Add links over the display text
            int sqlStart = lblChoiceWarning.Text.IndexOf("Microsoft SQL Servers");
            lblChoiceWarning.Links.Add(sqlStart, "Microsoft SQL Servers".Length, "https://www.microsoft.com/en-us/sql-server/sql-server-downloads\r\n");

            int iisStart = lblChoiceWarning.Text.IndexOf("How to enable IIS on Windows");
            lblChoiceWarning.Links.Add(iisStart-1, "How to enable IIS on Windows".Length, "https://engrafo.atlassian.net/wiki/spaces/ED/pages/21168129/Installation+-+server+desktop#Installation-steps-for-Internet-Information-Server(IIS)\r\n");

            int dockerStart = lblChoiceWarning.Text.IndexOf("Docker Desktop");
            lblChoiceWarning.Links.Add(dockerStart-2, "Docker Desktop".Length, "https://www.docker.com/products/docker-desktop\r\n");

            int guideStart = lblChoiceWarning.Text.IndexOf("Complete quide to manual installation of Engrafo");
            lblChoiceWarning.Links.Add(guideStart-3, "Complete quide to manual installation of Engrafo".Length, "https://engrafo.atlassian.net/wiki/x/AQBDAQ");

            lblChoiceWarning.LinkClicked += (s, e) =>
            {
                Process.Start(new ProcessStartInfo(e.Link.LinkData.ToString()) { UseShellExecute = true });
            };
            tbl.Controls.Add(lblChoiceWarning, 0, 6);
            // ── Next button row (also aligned under indent)
            btnNext = new Button
            {
                Text = "Next →",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            btnNext.Click += (s, e) => NextClicked?.Invoke(this, EventArgs.Empty);

            var flp = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                

                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(indent, 0, 0, 0)
            };
         
            tbl.Controls.Add(flp, 0, 8);
              flp.Controls.Add(btnNext);
        }
    }
}
