using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Engrafo_1_Installer
{
    public partial class DockerLaunchPanel : UserControl
    {
        public event EventHandler BackClicked;
        public int Port { get; set; }
        public string AppFolder { get; set; }

        private Label lblTitle;
        private LinkLabel lblMessage;
        private Label lblDownloadHint;
        private Button bntSasData;
        private Button bntRegularData;
        private Button btnLaunch;
        private Button btnFinish;
        private Button btnBack;
        private FlowLayoutPanel downloadPanel;

        public DockerLaunchPanel()
        {
            InitializeAppComponent();
        }

        private void InitializeAppComponent()
        {
            this.Dock = DockStyle.Fill;
            const int btnHeight = 30;
            const int btnWidth = 120;
            const int margin = 20;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(margin),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblTitle = new Label
            {
                Text = "All Done! \r\n\r\n Click the Launch button. \r\n Come back to the installer app for user login and demo data\r\n"+
                "On launch - Please allow the browser a short time to connect to the containers in Docker.\r\n"+
                "You can also start the application directly from Docker Desktop.",
                Font = new Font(this.Font, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, margin / 2)
            };
            mainLayout.Controls.Add(lblTitle, 0, 0);

            lblMessage = new LinkLabel
            {
                Text = "",
                AutoSize = true,
                Font = this.Font,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, margin / 2)
            };
            mainLayout.Controls.Add(lblMessage, 0, 1);

            lblDownloadHint = new Label
            {
                Text = "After launch of Engrafo you can download sample data to test Engrafo's API. " +
                       "Click the buttons to download data directly to Engrafo's upload folder. " +
                       "The data will create a sample on data catalog, data lineage and more. " +
                       " " +
                       "Or Use the link to download the test data to your desired folder to view the data structure for the API.",
                AutoSize = true,
                MaximumSize = new Size(600, 0),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, margin / 2),
                Visible = false
            };

            mainLayout.Controls.Add(lblDownloadHint, 0, 2);

            downloadPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, margin / 2),
                Visible = false
            };

            bntSasData = new Button { Text = "Download SAS Metadata Logs", AutoSize = true, Enabled = false, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            bntRegularData = new Button { Text = "Download CSV Metadata", AutoSize = true, Enabled = false, AutoSizeMode = AutoSizeMode.GrowAndShrink };

            string sasText = "Link to SAS metadata samples";
            string regText = "Link to CSV metadata samples";

            Func<Button, string, string, Panel> makeCard = (chk, text, url) =>
            {
                var card = new Panel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    BorderStyle = BorderStyle.None,
                    Margin = new Padding(0, 0, 0, margin / 2)
                };

                var layout = new TableLayoutPanel
                {
                    AutoSize = true,
                    ColumnCount = 1,
                    RowCount = 3,
                    Margin = new Padding(8)
                };
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                layout.Controls.Add(chk, 0, 0);

                var lbl = new Label
                {
                    Text = text,
                    AutoSize = true,
                    MaximumSize = new Size(600, 0),
                    Margin = new Padding(0, 4, 0, 0)
                };
                layout.Controls.Add(lbl, 0, 1);

                var link = new LinkLabel
                {
                    Text = url,
                    AutoSize = true,
                    MinimumSize = new Size(600, 0),
                    Margin = new Padding(0, 4, 0, 0)
                };
                link.Links.Add(0, url.Length, url);
                link.LinkClicked += (s, e) =>
                {
                    Process.Start(new ProcessStartInfo(e.Link.LinkData.ToString()) { UseShellExecute = true });
                };
                layout.Controls.Add(link, 0, 2);

                card.Controls.Add(layout);
                return card;
            };

            downloadPanel.Controls.Add(makeCard(bntSasData, sasText, "https://www.engrafo.eu/EngrafoVersions/Deployed SASPrograms.zip"));
            downloadPanel.Controls.Add(makeCard(bntRegularData, regText, "https://www.engrafo.eu/EngrafoVersions/WWISamples.zip"));
            mainLayout.Controls.Add(downloadPanel, 0, 3);

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            mainLayout.Dock = DockStyle.Top;
            scrollPanel.Controls.Add(mainLayout);
            Controls.Add(scrollPanel);

            btnLaunch = new Button { Text = "Launch Engrafo", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnBack = new Button { Text = "← Back", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnFinish = new Button { Text = "Finish", AutoSize = true, Visible = false, AutoSizeMode = AutoSizeMode.GrowAndShrink };

            btnBack.Click += (s, e) => BackClicked?.Invoke(this, e);
            btnLaunch.Click += OnLaunchClick;
            btnFinish.Click += (s, e) => this.FindForm()?.Close();

            Controls.AddRange(new Control[] { btnLaunch, btnFinish, btnBack });

            btnBack.BringToFront();
            btnLaunch.BringToFront();
            btnFinish.BringToFront();
            this.Resize += (s, e) => LayoutHelper.PlaceFooterButtons(this, btnFinish, btnBack, btnLaunch, margin, 10);
            this.VisibleChanged += (s, e) =>
            {
                if (Visible) LayoutHelper.PlaceFooterButtons(this, btnFinish, btnBack, btnLaunch, margin, 10);
            };
            LayoutHelper.PlaceFooterButtons(this, btnFinish, btnBack, btnLaunch, margin, 10);
        }

        private async void OnLaunchClick(object sender, EventArgs e)
        {
            // 1) Launch browser immediately
            Process.Start(new ProcessStartInfo($"http://localhost:{Port}") { UseShellExecute = true });
            lblMessage.Text = $"✔ Engrafo launched at http://localhost:{Port}\r\n" +
                "IMPORTANT: Follow these steps to get started with Engrafo:\r\n" +
                "1) Login to Engrafo with (user,password): admin@admin.com, EngrafoDemopw.1\r\n" +
                "2) Get a license for Engrafo (you'll recieve a mail with license-file) \r\n" +
                "3) Upload the license in the license section of Engrafo\r\n" +
                "4) Optional: Download sample data to test Engrafo's features. You can use the sample data for testing or upload your own metadata\r\n\r\n" +

                "Get help on how to use Engrafo here: Engrafo Guide\r\n";

            int guideStart = lblMessage.Text.IndexOf("Engrafo Guide");
            int licenseStart = lblMessage.Text.IndexOf("Get a license");
            lblMessage.Links.Clear();
            if (guideStart >= 0)
                lblMessage.Links.Add(guideStart-3, "Engrafo Guide".Length, "https://engrafo.atlassian.net/wiki/spaces/EDV/overview?homepageId=256868611");
            if (licenseStart >= 1)
                lblMessage.Links.Add(licenseStart-3, "Get a license".Length, "https://www.engrafo.eu/sasanalyzerpricingmodels/");

            lblMessage.LinkClicked += (s, e) =>
            {
                Process.Start(new ProcessStartInfo(e.Link.LinkData.ToString()) { UseShellExecute = true });
            };

            btnLaunch.Visible = false;
            btnFinish.Visible = true;

            lblDownloadHint.Visible = true;
            downloadPanel.Visible = true;
            lblTitle.Visible = false;

            await Task.Delay(TimeSpan.FromSeconds(5));

            if (!btnLaunch.Visible)
            {
                bntSasData.Enabled = true;
                bntRegularData.Enabled = true;
            }

            bntSasData.Click += async (s, e2) =>
            {
                await DownloadAndExtract("https://www.engrafo.eu/EngrafoVersions/Deployed%20SASPrograms.zip");
            };

            bntRegularData.Click += async (s, e2) =>
            {
                await DownloadAndExtract("https://www.engrafo.eu/EngrafoVersions/WWISamples.zip");
            };
        }

        private async Task DownloadAndExtract(string url)
        {
            if (string.IsNullOrWhiteSpace(AppFolder))
            {
                MessageBox.Show("App folder not set – cannot download.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string targetDir = AppFolder;
            //Path.Combine(AppFolder, "wwwroot", "uploads_CSVAUTO");
            //Directory.CreateDirectory(targetDir);

            string tempZip = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");
            try
            {
                using var client = new HttpClient();
                using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                resp.EnsureSuccessStatusCode();

                using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
                {
                    await resp.Content.CopyToAsync(fs);
                }

                ZipFile.ExtractToDirectory(tempZip, targetDir, overwriteFiles: true);

                MessageBox.Show($"Data downloaded & extracted to:\n{targetDir} \r\n You can use this folder for your own metadata",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to download/extract:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (File.Exists(tempZip))
                    File.Delete(tempZip);
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                ResetPanelState();
                LayoutHelper.PlaceFooterButtons(this, btnFinish, btnBack, btnLaunch, 20, 10);
            }
        }

        private void ResetPanelState()
        {
            btnLaunch.Visible = true;
            btnFinish.Visible = false;

            lblTitle.Visible = true;
            lblMessage.Text = "";
            lblMessage.Links.Clear();
            lblMessage.Visible = true;

            lblDownloadHint.Visible = false;
            downloadPanel.Visible = false;

            bntSasData.Enabled = false;
            bntRegularData.Enabled = false;
        }
    }
}
