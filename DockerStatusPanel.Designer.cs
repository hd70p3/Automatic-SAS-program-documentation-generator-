using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Engrafo_1_Installer
{
    partial class DockerStatusPanel
    {
        private IContainer components = null;

        /// <summary> 
        /// Required method for Designer support — do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new Container();
            this.SuspendLayout();
            // 
            // DockerStatusPanel
            // 
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Name = "DockerStatusPanel";
            this.Size = new System.Drawing.Size(200, 70);
            this.ResumeLayout(false);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                refreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}