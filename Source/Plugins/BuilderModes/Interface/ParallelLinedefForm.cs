#region ================== Namespaces

using System;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing;

#endregion

//JBR Parallel Linedef form
namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
    public partial class ParallelLinedefForm : DelayedForm
    {        
        #region ================== Properties

        private bool blockEvents;

        public int CreateAs { get { return createas.SelectedIndex; } }
        public float Distance { get { return distance.GetResultFloat(0); } }
        public bool CloseOpenPath { get { return closeopenpath.Checked; } }
        public bool Backwards { get { return backwards.Checked; } }

        #endregion

        #region ================== Constructor / Disposer

        public ParallelLinedefForm()
        {
            InitializeComponent();

            blockEvents = true;
            createas.SelectedIndex = 0;
            distance.Text = "256";
            blockEvents = false;
        }

        #endregion

        #region ================== Interface

        public void SetNumOpenPaths(int paths)
        {
            if (paths == 0)
            {
                numopentracks.Text = "No Open-Paths";
                numopentracks.ForeColor = SystemColors.GrayText;
                closeopenpath.Enabled = false;
                return;
            }
            numopentracks.ForeColor = SystemColors.ControlText;
            closeopenpath.Enabled = true;
            if (paths == 1)
                numopentracks.Text = "1 Open-Path";
            else
                numopentracks.Text = paths.ToString() + " Open-Paths";
        }

        public void SetNumClosePaths(int paths)
        {
            if (paths == 0)
            {
                numclosetracks.Text = "No Close-Paths";
                numclosetracks.ForeColor = SystemColors.GrayText;
                return;
            }
            numclosetracks.ForeColor = SystemColors.ControlText;
            if (paths == 1)
                numclosetracks.Text = "1 Close-Path";
            else
                numclosetracks.Text = paths.ToString() + " Close-Paths";
        }

        // Window closing
        private void ParallelLinedefForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // User closing the window?
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Just cancel
                General.Editing.CancelMode();
                e.Cancel = true;
            }
        }

        // This shows the window
        public void Show(Form owner)
        {
            // Position at left-top of owner
            this.Location = new Point(owner.Location.X + 20, owner.Location.Y + 90);

            // Show window
            base.Show(owner);
        }

        // Some value got changed
        private void ValueChanged(object sender, EventArgs e)
        {
            if (!blockEvents) General.Interface.RedrawDisplay();
        }

        // Cancel clicked
        private void cancel_Click(object sender, EventArgs e)
        {
            // Cancel now
            General.Editing.CancelMode();
        }

        // Apply clicked
        private void apply_Click(object sender, EventArgs e)
        {
            // Apply now
            General.Editing.AcceptMode();
        }

        private void ParallelLinedefForm_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            General.ShowHelp("e_parallellinedef.html");
        }

        #endregion
    }
}
