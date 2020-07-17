#region ================== Namespaces

using System;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing;

#endregion

//JBR Perpendicular Linedef form
namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
    public partial class PerpendicularLinedefForm : DelayedForm
    {
        #region ================== Properties

        private bool blockEvents;

        public int CreateAs { get { return createas.SelectedIndex; } }
        public float Distance { get { return distance.GetResultFloat(0f); } }
        public float OffsetPerc { get { return offsetperc.GetResultFloat(0f); } }
        public bool Backwards { get { return backwards.Checked; } }
        public float SnapMP { get { return snapmp.GetResultFloat(0f); } }

        #endregion

        #region ================== Constructor / Disposer

        public PerpendicularLinedefForm()
        {
            InitializeComponent();

            blockEvents = true;
            createas.SelectedIndex = 0;
            distance.Text = "256";
            offsetperc.Text = "50";
            snapmp.Text = "2";
            blockEvents = false;
        }

        #endregion

        #region ================== Interface

        // Window closing
        private void PerpendicularLinedefForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void offsetperc_WhenTextChanged(object sender, EventArgs e)
        {
            float perc = offsetperc.GetResultFloat(0f);
            if (perc > 100f)
                offsetperc.Text = "100";
            ValueChanged(this, e);
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

        private void PerpendicularLinedefForm_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            General.ShowHelp("e_perpendicularlinedef.html");
        }

        #endregion
    }
}
