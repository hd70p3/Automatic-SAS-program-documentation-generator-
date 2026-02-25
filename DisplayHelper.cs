using System.Drawing;
using System.Windows.Forms;

public static class DisplayHelper
{
    public static Font GetAdjustedFont(Font baseFont, Control control)
    {
        using (Graphics g = control.CreateGraphics())
        {
            float dpiScaleFactor = g.DpiX / g.DpiX; // 96 DPI is the default
            return new Font(baseFont.FontFamily, baseFont.Size * dpiScaleFactor, baseFont.Style);
        }
    }

    public static void AdjustControlForDpi(Control control)
    {
        using (Graphics g = control.CreateGraphics())
        {
       
            float dpiScaleFactor = g.DpiX / 96f;

            control.Size = new Size(
                (int)(control.Size.Width * dpiScaleFactor),
                (int)(control.Size.Height * dpiScaleFactor)
            );
       
            control.Location = new Point(
                (int)(control.Location.X * dpiScaleFactor),
                (int)(control.Location.Y * dpiScaleFactor)
            );

            if (control.Font != null)
            {
                control.Font = new Font(
                    control.Font.FontFamily,
                    control.Font.Size * dpiScaleFactor,
                    control.Font.Style
                );
            }

            if (control is Control)
            {
                control.Margin = ScalePaddingOrMargin(control.Margin, dpiScaleFactor);
                control.Padding = ScalePaddingOrMargin(control.Padding, dpiScaleFactor);
            }
            // Til multiple settings
            foreach (Control child in control.Controls)
            {
                AdjustControlForDpi(child);
            }
        }
    }

    private static Padding ScalePaddingOrMargin(Padding original, float dpiScaleFactor)
    {
        return new Padding(
            (int)(original.Left * dpiScaleFactor),
            (int)(original.Top * dpiScaleFactor),
            (int)(original.Right * dpiScaleFactor),
            (int)(original.Bottom * dpiScaleFactor)
        );
    }
}
