using System.Drawing;
using System.Windows.Forms;

namespace Engrafo_1_Installer
{
    public static class LayoutHelper
    {
        /// <summary>
        /// Anchors and positions up to three buttons in the bottom-right corner of a parent control.
        /// </summary>
        public static void PlaceFooterButtons(
            Control parent,
            Button btnNext,
            Button btnBack,
            Button btnExtra       = null,
            int margin            = 20,
            int spacingBetween    = 10)
        {
            // Anchor them so they stay put on resize/DPI change
            btnNext.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnBack.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            if (btnExtra != null)
                btnExtra.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            // Compute positions relative to parent.ClientSize
            int y = parent.ClientSize.Height - btnNext.Height - margin;
            int xNext = parent.ClientSize.Width - btnNext.Width - margin;
            btnNext.Location = new Point(xNext, y);

            int xBack = xNext - btnBack.Width - spacingBetween;
            btnBack.Location = new Point(xBack, y);

            if (btnExtra != null)
            {
                int xExtra = xBack - btnExtra.Width - spacingBetween;
                btnExtra.Location = new Point(xExtra, y);
            }

            // Re-apply on resize
            parent.Resize += (s, e) =>
            {
                PlaceFooterButtons(parent, btnNext, btnBack, btnExtra, margin, spacingBetween);
            };
        }
    }
}