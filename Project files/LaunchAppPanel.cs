using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.Administration;

namespace Engrafo_1_Installer
{
    public partial class LaunchAppPanel : UserControl
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

        public LaunchAppPanel()
        {
            InitializeAppComponent();
        }

        private void InitializeAppComponent()
        {
            this.Dock = DockStyle.Fill;
            const int btnHeight = 30;
            const int btnWidth = 120;
            const int margin = 20;

            // ── Main layout ─────────────────────────────────────────
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(margin),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Message
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Hint
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Download cards
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Spacer

            // ── Title ────────────────────────────────────────────────
            lblTitle = new Label
            {
                Text = "All Done! \r\n\r\n Click the Launch button. \r\n Come back to the installer for user login and demo data",
                Font = new Font(this.Font, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, margin / 2)
            };
            mainLayout.Controls.Add(lblTitle, 0, 0);

            // ── Message ──────────────────────────────────────────────
            lblMessage = new LinkLabel
            {
                Text = "",
                AutoSize = true,
                Font = this.Font,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, margin / 2)
            };
            mainLayout.Controls.Add(lblMessage, 0, 1);

            // ── Hint for downloads ──────────────────────────────────
            lblDownloadHint = new Label
            {
                Text = "After launch of Engrafo you can download sample data to test Engrafo's API. "+
                       "Click the buttons to download data directly to Engrafo's upload folder. "+ 
                       "The data will create a sample on data catalog, data lineage and more. "+
                       " "+
                       "Or Use the link to download the test data to your desired folder to view the data structure for the API.", 
                AutoSize = true,
                MaximumSize = new Size(600, 0),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, margin / 2),
                Visible = false
            };

            mainLayout.Controls.Add(lblDownloadHint, 0, 2);

            // ── Download cards (stacked) ────────────────────────────
            downloadPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, margin / 2),
                Visible = false
            };

            // checkboxes
            bntSasData = new Button { Text = "Download SAS Metadata Logs", AutoSize = true, Enabled=false, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            bntRegularData = new Button { Text = "Download CSV Metadata", AutoSize = true, Enabled = false, AutoSizeMode = AutoSizeMode.GrowAndShrink };

            // single description strings
            string sasText =
                "Link to SAS metadata samples";

            string regText =

                "Link to CSV metadata samples";


            // helper: builds a small “card” with checkbox on top, bordered text below
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

                // row 0: button
                layout.Controls.Add(chk, 0, 0);

                // row 1: description
                var lbl = new Label
                {
                    Text = text,
                    AutoSize = true,
                    MaximumSize = new Size(600, 0),
                    Margin = new Padding(0, 4, 0, 0)
                };
                layout.Controls.Add(lbl, 0, 1);

                // row 2: clickable link
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
                // DisplayHelper.AdjustControlForDpi(this);
                return card;
            };

            // add both cards
            downloadPanel.Controls.Add(makeCard(bntSasData, sasText, "https://www.engrafo.eu/EngrafoVersions/Deployed SASPrograms.zip"));
            downloadPanel.Controls.Add(makeCard(bntRegularData, regText, "https://www.engrafo.eu/EngrafoVersions/WWISamples.zip"));
            mainLayout.Controls.Add(downloadPanel, 0, 3);

            // ── Add main layout + footer buttons ────────────────────
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            mainLayout.Dock = DockStyle.Top; // allow vertical growth
            scrollPanel.Controls.Add(mainLayout);
            Controls.Add(scrollPanel);

    
            btnLaunch = new Button { Text = "Launch Engrafo", AutoSize=true,/* Size = new Size(btnWidth, btnHeight),*/ AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnBack = new Button { Text = "← Back", AutoSize=true, /*Size = new Size(btnWidth, btnHeight) ,*/ AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnFinish = new Button { Text = "Finish", AutoSize= true, /*Size = new Size(btnWidth, btnHeight),*/ Visible = false , AutoSizeMode = AutoSizeMode.GrowAndShrink };

            btnBack.Click += (s, e) => BackClicked?.Invoke(this, e);
            btnLaunch.Click += OnLaunchClick;
            btnFinish.Click += (s, e) => this.FindForm()?.Close();

            Controls.AddRange(new Control[] { btnLaunch, btnFinish, btnBack });

            // bring to front + position
            btnBack.BringToFront();
            btnLaunch.BringToFront();
            btnFinish.BringToFront();
            this.Resize += (s, e) => LayoutHelper.PlaceFooterButtons(this, btnFinish, btnBack, btnLaunch , margin, 10);
            this.VisibleChanged += (s, e) =>
            {
                if (Visible) LayoutHelper.PlaceFooterButtons(this, btnFinish, btnBack, btnLaunch , margin, 10);
            };
            LayoutHelper.PlaceFooterButtons(this, btnFinish, btnBack, btnLaunch , margin, 10);
        }


        private async void OnLaunchClick(object sender, EventArgs e)
        {
            // 1) Launch browser immediately
            Process.Start(new ProcessStartInfo($"http://localhost:{Port}") { UseShellExecute = true });
            lblMessage.Text = $"✔ Engrafo launched at http://localhost:{Port}\r\n"+
                "Get help on how to use Engrafo here: Engrafo Guide\r\n"+
                "Login to Engrafo with (user,password): admin@admin.com, EngrafoDemopw.1";

            int guideStart = lblMessage.Text.IndexOf("Engrafo Guide");
            lblMessage.Links.Add(guideStart, "Engrafo Guide".Length-1, "https://engrafo.atlassian.net/wiki/spaces/EDV/overview?homepageId=256868611\r\n");

            lblMessage.LinkClicked += (s, e) =>
            {
                Process.Start(new ProcessStartInfo(e.Link.LinkData.ToString()) { UseShellExecute = true });
            };

            // 2) Swap buttons
            btnLaunch.Visible = false;
            btnFinish.Visible = true;

            lblDownloadHint.Visible = true;
            downloadPanel.Visible = true;
            lblTitle.Visible = false;   


            // 3) Delay then download whichever is checked
            await Task.Delay(TimeSpan.FromSeconds(5));

            if (btnLaunch.Visible == false)
            {
                bntSasData.Enabled = true;
                bntRegularData.Enabled = true;
            }

            bntSasData.Click += async (s, e) =>
            {
                await DownloadAndExtract("https://www.engrafo.eu/EngrafoVersions/Deployed%20SASPrograms.zip");
            };

            bntRegularData.Click += async (s, e) =>
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

            string targetDir = Path.Combine(AppFolder, "wwwroot", "uploads_CSVAUTO");
            Directory.CreateDirectory(targetDir);

            // Use a unique temp file so two downloads don't clash
            string tempZip = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");
            try
            {
                using var client = new HttpClient();
                using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                resp.EnsureSuccessStatusCode();

                // Download to tempZip *and* close the stream before extraction
                using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None, 8192,
                           useAsync: true))
                {
                    await resp.Content.CopyToAsync(fs);
                }

                // Now that fs is disposed, we can safely extract
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
                // Reset all UI state
                ResetPanelState();

                // (Re-)place footer buttons
                LayoutHelper.PlaceFooterButtons(this, btnFinish, btnBack, btnLaunch, 20, 10);

                // Optionally: restart the site if needed
                RestartSiteByPort(Port);
            }
        }


        public static void RestartSiteByPort(int port)
        {
            try
            {
                using var mgr = new ServerManager();

                // Find the site
                var site = mgr.Sites
                    .FirstOrDefault(s =>
                        s.Bindings.Any(b =>
                            b.Protocol.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                            b.EndPoint?.Port == port
                        )
                    );

                // Restart the site
                site.Stop();
                site.Start();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Administrator privileges are required to restart the IIS site.",
                    "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restart IIS site:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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