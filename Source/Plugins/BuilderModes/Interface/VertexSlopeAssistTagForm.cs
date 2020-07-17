#region ================== Namespaces

using System;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing;
using System.Collections.Generic;
using CodeImp.DoomBuilder.Map;

#endregion

//JBR Vertex Slope Assistant, Tag form
namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
    public partial class VertexSlopeAssistTagForm : DelayedForm
    {        
        #region ================== Properties

        public int TagID { get { return tagID.GetResult(0); } set { tagID.Text = value.ToString(); } }

        #endregion

        #region ================== Constructor / Disposer

        public VertexSlopeAssistTagForm()
        {
            InitializeComponent();
            tagID.Text = "0";
        }

        #endregion

        #region ================== Methods

        // Returns selected tag number, -1 if cancel
        public static int RunDialog(IWin32Window owner, int tag)
        {
            VertexSlopeAssistTagForm f = new VertexSlopeAssistTagForm();
            f.tagID.Text = tag.ToString();
            DialogResult result = f.ShowDialog(owner);
            int newtag = -1;
            if (result == DialogResult.OK)
            {
                newtag = f.tagID.GetResult(tag);
            }
            f.Dispose();
            return newtag;
        }

        #endregion

        #region ================== Interface

        // Window closing
        private void VertexSlopeAssistRetagForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // User closing the window?
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Just cancel
                e.Cancel = true;
            }
        }

        private void newtag_Click(object sender, EventArgs e)
        {
            tagID.Text = General.Map.Map.GetNewTag().ToString();
        }

        // This shows the window
        public void Show(Form owner)
        {
            // Position at left-top of owner
            this.Location = new Point(owner.Location.X + 20, owner.Location.Y + 90);

            // Show window
            base.Show(owner);
        }

        private void VertexSlopeAssistRetagForm_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            General.ShowHelp("e_vertexslopeassist.html");
        }

        #endregion
    }
}
