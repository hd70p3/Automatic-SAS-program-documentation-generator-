using System.Drawing;
using System.Security.Principal;
using System.Windows.Forms;
namespace Engrafo_1_Installer;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        if (!IsAdministrator())
        {
            MessageBox.Show(
                "This installer must be run as Administrator.\n" +
                "Right-click the EXE and choose “Run as administrator”, then try again.\n" +
                "You can also contact us at: contact@engrafo.dk\nFor any questions you may have",
                "Administrator Privileges Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return;  // quit immediately
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}



public static class Theme
{
    public static readonly Font   DefaultFont        = new Font("Segoe UI", 9F);
    public static readonly Color  BackgroundColor    = Color.WhiteSmoke;
    public static readonly Color  ForegroundColor    = Color.FromArgb(50,  50,  50);
    public static readonly Color  ButtonBackColor    = Color.FromArgb(26, 179, 148);
    public static readonly Color  ButtonForeColor    = Color.White;
    public static readonly FlatStyle ButtonFlatStyle = FlatStyle.Standard;

    public static void Apply(Control c)
    {
        // base font & fore/back colors
        c.Font = DisplayHelper.GetAdjustedFont(DefaultFont, c);
        c.ForeColor = c.Enabled ? ForegroundColor : SystemColors.GrayText;
        c.BackColor = c.Enabled ? BackgroundColor : SystemColors.Control;
        
 

        // buttons
        if (c is Button btn)
        {
            btn.FlatStyle = ButtonFlatStyle;
            btn.BackColor = ButtonBackColor;
            btn.ForeColor = ButtonForeColor;
            btn.Font = DisplayHelper.GetAdjustedFont(DefaultFont, btn);

        }
        // input controls: white when enabled, grey when disabled
        else if (c is TextBox 
                 || c is ComboBox 
                 || c is NumericUpDown 
                 || c is DateTimePicker)
        {
            c.BackColor = c.Enabled 
                ? Color.White 
                : SystemColors.ControlLight;    // light grey
        }
 
        // recurse
        foreach (Control child in c.Controls)
        {
            c.Font = DisplayHelper.GetAdjustedFont(DefaultFont, child);
            Apply(child);
        }
    }
}

