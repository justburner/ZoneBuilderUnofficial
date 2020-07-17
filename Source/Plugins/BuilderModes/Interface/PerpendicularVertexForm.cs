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
    public partial class PerpendicularVertexForm : DelayedForm
    {        
        #region ================== Properties

        private bool blockEvents;

        public int CreateAs { get { return createas.SelectedIndex; } }
        public float Distance { get { return distance.GetResultFloat(0f); } }
        public bool ProcessTips { get { return processtips.Checked; } }
        public bool Backwards { get { return backwards.Checked; } }
        public float SnapMP { get { return snapmp.GetResultFloat(0f); } }

        #endregion

        #region ================== Constructor / Disposer

        public PerpendicularVertexForm()
        {
            InitializeComponent();

            blockEvents = true;
            createas.SelectedIndex = 0;
            distance.Text = "256";
            snapmp.Text = "2";
            blockEvents = false;
        }

        #endregion

        #region ================== Interface

        // Window closing
        private void PerpendicularVertexForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void PerpendicularVertexForm_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            General.ShowHelp("e_perpendicularvertex.html");
        }

        #endregion
    }
}
