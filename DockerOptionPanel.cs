using System;
using System.Drawing;
using System.Windows.Forms;

namespace Engrafo_1_Installer
{
    public partial class DockerOptionPanel : Panel
    {
        public event EventHandler BackClicked;
        public event EventHandler Option1Selected;
        public event EventHandler Option2Selected;
        public event EventHandler Option3Selected;


        private readonly Label lblDockerChoiceTitle;
        private readonly RadioButton rbOpt1;
        private readonly RadioButton rbOpt2;
        private readonly RadioButton rbOpt3;
        private readonly Button btnNext;
        private readonly Button btnBack;

        public DockerOptionPanel()
        {
            this.Dock = DockStyle.Fill;
            this.Visible = false;

            var margin = 20;

            lblDockerChoiceTitle = new Label
            {
                Text = "Choose your database setup for Docker:",
                Location = new Point(30, 30),
                AutoSize = true
            };

            rbOpt1 = new RadioButton
            {
                Text = "Connect to a MSSQL Server through SQL login.",
                Location = new Point(30, 70),
                AutoSize = true
            };

            rbOpt2 = new RadioButton
            {
                Text = "Connect to a running MSSQL Server in Docker.",
                Location = new Point(30, 110),
                AutoSize = true
            };

            rbOpt3 = new RadioButton
            {
                Text = "(Recommended) Create a fresh database in Docker for Engrafo.",
                Location = new Point(30, 150),
                AutoSize = true,
                Checked = true
            };

            btnNext = new Button
            {
                Text = "Next →",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            btnNext.Click += BtnNext_Click;

            btnBack = new Button
            {
                Text = "← Back",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            btnBack.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);

            this.Controls.AddRange(new Control[]
            {
                lblDockerChoiceTitle,
                rbOpt1, rbOpt2, rbOpt3,
                btnNext, btnBack
            });

            DisplayHelper.AdjustControlForDpi(btnNext);
            DisplayHelper.AdjustControlForDpi(btnBack);

            LayoutHelper.PlaceFooterButtons(this, btnNext, btnBack, null, margin);
            
// Add help icons to the options
            AddHelp(rbOpt1, "Use this to connect to any reachable SQL Server using SQL login credentials.");
            AddHelp(rbOpt2, "Use this to connect to an already running MSSQL Server that is running inside a Docker container.");
            AddHelp(rbOpt3, "Recommended: Let Engrafo create a fresh SQL Server database in a new Docker container for you.");
        }


        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (rbOpt3.Checked)
            {
                Option3Selected?.Invoke(this, EventArgs.Empty);
            }
            else if (rbOpt2.Checked)
            {
                Option2Selected?.Invoke(this, EventArgs.Empty);
            }
            else if (rbOpt1.Checked)
            {
                Option1Selected?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private ToolTip _tip = new ToolTip();
        private void AddHelp(Control anchor, string tooltip)
        {
            const int iconSize = 16;
            Bitmap bmp;
            try
            {
                var asm = GetType().Assembly;
                var name = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("Icon-round-Question_mark.svg.png", StringComparison.OrdinalIgnoreCase));
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

    }
}