using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Engrafo_1_Installer
{
    public partial class DownloadPanel : UserControl
    {
        public event EventHandler BackClicked;
        public event EventHandler NextClicked;
        public event EventHandler<string> DownloadCompleted;

        private RadioButton rbArm;
        private RadioButton rb32;
        private RadioButton rb64;
        private Label lblStatus;
        private Button btnDownload;
        private ProgressBar progressBar;
        private PictureBox helpIcon;
        private Button btnBack;
        private Button btnNext;
        private ToolTip _tip = new ToolTip();

        public DownloadPanel()
        {
            InitializeDownloadComponent();
        }

        private void InitializeDownloadComponent()
        {
            const int margin = 20;
            const int iconSize = 16;
            const int spacingBetween = 10;

            // Dock this panel to fill its parent
            this.Dock = DockStyle.Fill;

            // ── Main table layout ────────────────────────────────────────────────
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(margin),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 7
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: rbArm
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: rb32
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: rb64
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: status
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: download button + icon
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 5: progress bar
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // 6: filler

            // ── Architecture radio buttons ────────────────────────────────────────
            rbArm = new RadioButton { Text = "Engrafo ARM (RISC) - contact@engrafo.dk for installation files", AutoSize = true };
            rb32 = new RadioButton { Text = "Engrafo 32-bit - contact@engrafo.dk for installation files", AutoSize = true };
            rb64 = new RadioButton { Text = "Engrafo 64-bit", AutoSize = true, Checked = true };

            rb32.Enabled = false; // Disable 32-bit option for now
            rbArm.Enabled = false; // Disable ARM option for now

            tbl.Controls.Add(rbArm, 0, 0);
            tbl.Controls.Add(rb32, 0, 1);
            tbl.Controls.Add(rb64, 0, 2);

            // ── Status label ──────────────────────────────────────────────────────
            lblStatus = new Label
            {
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Dock = DockStyle.Top
            };
            tbl.Controls.Add(lblStatus, 0, 3);

            // ── Download button + help icon ──────────────────────────────────────
            btnDownload = new Button
            {
                Text = "Download and extract",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            btnDownload.Click += BtnDownload_Click;

            helpIcon = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(iconSize, iconSize),
                Cursor = Cursors.Default
            };
            // Load question-mark icon from resources or fallback
            Bitmap bmp;
            try
            {
                var asm = typeof(DownloadPanel).Assembly;
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

            helpIcon.Image = new Bitmap(bmp, new Size(iconSize, iconSize));
            _tip.SetToolTip(helpIcon, "Click to download and unzip the Engrafo package into the folder you pick.");

            var pnlDownload = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(0, margin / 2, 0, 0)
            };
            pnlDownload.Controls.Add(btnDownload);
            pnlDownload.Controls.Add(helpIcon);
            tbl.Controls.Add(pnlDownload, 0, 4);

            // ── Progress bar ───────────────────────────────────────────────────────
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Visible = false
            };
            tbl.Controls.Add(progressBar, 0, 5);

            // Add the table layout to this UserControl
            Controls.Add(tbl);

            // ── Footer buttons ────────────────────────────────────────────────────
            btnBack = new Button { Text = "← Back", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnBack.Click += (_, __) => BackClicked?.Invoke(this, EventArgs.Empty);

            btnNext = new Button { Text = "Next →", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnNext.Click += (_, __) => NextClicked?.Invoke(this, EventArgs.Empty);

            Controls.Add(btnBack);
            Controls.Add(btnNext);

            // Make sure buttons appear above the table
            btnBack.BringToFront();
            btnNext.BringToFront();

            // Re-position on resize
            this.Resize += (s, e) =>
                LayoutHelper.PlaceFooterButtons(
                    parent: this,
                    btnNext: btnNext,
                    btnBack: btnBack,
                    btnExtra: null,
                    margin: margin,
                    spacingBetween: spacingBetween
                );

            // Re-position when first shown
            this.VisibleChanged += (s, e) =>
            {
                if (Visible)
                    LayoutHelper.PlaceFooterButtons(
                        parent: this,
                        btnNext: btnNext,
                        btnBack: btnBack,
                        btnExtra: null,
                        margin: margin,
                        spacingBetween: spacingBetween
                    );
            };

            // Initial placement
            LayoutHelper.PlaceFooterButtons(
                parent: this,
                btnNext: btnNext,
                btnBack: btnBack,
                btnExtra: null,
                margin: margin,
                spacingBetween: spacingBetween
            );

            // Adjust for DPI
            // DisplayHelper.AdjustControlForDpi(this);
        }


        private async void BtnDownload_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog { Description = "Select destination folder for Engrafo" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string dest = dlg.SelectedPath;
            string url;
            if (rbArm.Checked)
                url = "Not available in this installer. contact@engrafo.eu for more information.";
            else if (rb32.Checked)
                url = "Not available in this installer. contact@engrafo.eu for more information.";
            else
                url = "Not available in this installer. contact@engrafo.eu for more information.";

            string zipPath = Path.Combine(dest, "engrafo_download.zip");

            btnDownload.Visible = false;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
            lblStatus.Text = "";

            await DownloadAndExtractZipFileAsync(url, zipPath, dest);

            progressBar.Visible = false;
            btnDownload.Enabled = true;
            DownloadCompleted?.Invoke(this, dest); // sender brugeren direkte videre
        }   

        private async Task DownloadAndExtractZipFileAsync(string zipFileUrl, string zipFilePath, string extractPath)
        {
            try
            {
                // --- Download phase ---
                lblStatus.Invoke((Action)(() => lblStatus.Text = "Downloading…"));

                using (var client = new HttpClient())
                using (var response = await client.GetAsync(zipFileUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault(-1L);
                    var canReport = totalBytes > 0;

                    // ensure destination folder exists
                    Directory.CreateDirectory(Path.GetDirectoryName(zipFilePath)!);

                    // **Scope the FileStream so it closes before extraction**
                    using (var zipFs = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None,
                               8192, useAsync: true))
                    using (var content = await response.Content.ReadAsStreamAsync())
                    {
                        var buffer = new byte[8192];
                        long received = 0;
                        int read;

                        while ((read = await content.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await zipFs.WriteAsync(buffer, 0, read);
                            if (canReport)
                            {
                                received += read;
                                int pct = (int)((received * 100L) / totalBytes);
                                progressBar.Invoke((Action)(() => progressBar.Value = Math.Min(pct, 100)));
                            }
                        }
                    }


                    lblStatus.Invoke((Action)(() => lblStatus.Text = "Extracting…"));
                    progressBar.Invoke((Action)(() => progressBar.Style = ProgressBarStyle.Marquee));

                    await Task.Run(() =>
                    {
                        ZipFile.ExtractToDirectory(zipFilePath, extractPath, overwriteFiles: true);
                    });

                    File.Delete(zipFilePath);

                    lblStatus.Invoke((Action)(() => lblStatus.Text = $"Engrafo unpacked to: {extractPath} ✔  \nClick next to continue"));
                    progressBar.Invoke((Action)(() => progressBar.Style = ProgressBarStyle.Continuous));

                }
            }
            catch (Exception ex)
            {
                // ensure bar hidden on error
                lblStatus.Invoke((Action)(() => lblStatus.Text = ""));
                progressBar.Invoke((Action)(() => progressBar.Visible = false));
                MessageBox.Show($"Download/Extract failed:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}