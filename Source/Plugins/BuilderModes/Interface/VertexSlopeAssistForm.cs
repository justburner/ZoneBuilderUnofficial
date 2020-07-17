#region ================== Namespaces

using System;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing;
using System.Collections.Generic;
using CodeImp.DoomBuilder.Map;

#endregion

//JBR Vertex Slope Assistant form
namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
    public partial class VertexSlopeAssistForm : DelayedForm
    {        
        #region ================== Properties / Variables

        [Serializable]
        public delegate void ContextEventHandler(object sender, int action);

        private bool blockEvents;

        public event EventHandler OnDrawNewTaggedTriangle;
        public event EventHandler OnDrawNewEmptyTriangle;
        public event EventHandler OnGroupChanged;
        public event EventHandler OnGroupChoosen;
        public event EventHandler OnGroupNewTriangle;
        public event EventHandler OnGroupNewVertex;
        public event EventHandler OnGroupModify;
        public event EventHandler OnGroupModifyAsTriangle;
        public event EventHandler OnGroupModifyAsVertex;
        public event EventHandler OnGroupTag;
        public event EventHandler OnGroupDelete;
        public event EventHandler OnValueChanged;
        public event EventHandler OnRotateAll;
        public event EventHandler OnFlipAll;
        public event EventHandler OnRotateHeight;
        public event EventHandler OnFlipHeight;
        public event ContextEventHandler OnContextSet;

        public bool AffineVSlopes { get { return affinevslopes.Checked; } }
        public int TagID { get { return tagID.GetResult(0); } set { tagID.Text = value.ToString(); UpdateForm(); } }
        public Vector3D Vertex1 { get { return GetVector3DFromUI(vslopeX1, vslopeY1, vslopeZ1); } set { SetVector3DFromUI(vslopeX1, vslopeY1, vslopeZ1, value); } }
        public Vector3D Vertex2 { get { return GetVector3DFromUI(vslopeX2, vslopeY2, vslopeZ2); } set { SetVector3DFromUI(vslopeX2, vslopeY2, vslopeZ2, value); } }
        public Vector3D Vertex3 { get { return GetVector3DFromUI(vslopeX3, vslopeY3, vslopeZ3); } set { SetVector3DFromUI(vslopeX3, vslopeY3, vslopeZ3, value); } }
        public bool Vertex1AbsZ { get { return vslopeAbsZ1.Checked; } set { vslopeAbsZ1.Checked = value; } }
        public bool Vertex2AbsZ { get { return vslopeAbsZ2.Checked; } set { vslopeAbsZ2.Checked = value; } }
        public bool Vertex3AbsZ { get { return vslopeAbsZ3.Checked; } set { vslopeAbsZ3.Checked = value; } }
        public string ModeText { get { return currentmode.Text; } set { currentmode.Text = value; } }

        public const int HEIGHTADJ_STEP = 1;
        public const int HEIGHTADJ_SMALL = 4;
        public const int HEIGHTADJ_BIG = 16;
        // List[GroupIndex] = Linedef Tag
        public List<int> vslopeTags;
        // SortedDictionary<Linedef Tag, List of VSlopes>
        public SortedDictionary<int, List<Thing>> vslopeDic;

        #endregion

        #region ================== Constructor / Disposer

        public VertexSlopeAssistForm()
        {
            InitializeComponent();

            blockEvents = true;
            vslopeX1.Text = "0";
            vslopeY1.Text = "0";
            vslopeZ1.Text = "0";
            vslopeX2.Text = "0";
            vslopeY2.Text = "0";
            vslopeZ2.Text = "0";
            vslopeX3.Text = "0";
            vslopeY3.Text = "0";
            vslopeZ3.Text = "0";
            tooltip.SetToolTip(heightAdj, string.Format("Hold Ctrl to change value by {0}.\nHold Shift to change value by {1}.", HEIGHTADJ_SMALL, HEIGHTADJ_BIG));
            CreateList();
            tagID.Text = General.Map.Map.GetNewTag(vslopeTags).ToString();
            blockEvents = false;
        }

        #endregion

        #region ================== Methods

        private Vector3D GetVector3DFromUI(Controls.ButtonsNumericTextbox X, Controls.ButtonsNumericTextbox Y, Controls.ButtonsNumericTextbox Z)
        {
            return new Vector3D(X.GetResult(0), Y.GetResult(0), Z.GetResult(0));
        }

        private void SetVector3DFromUI(Controls.ButtonsNumericTextbox X, Controls.ButtonsNumericTextbox Y, Controls.ButtonsNumericTextbox Z, Vector3D value)
        {
            blockEvents = true;
            X.Text = value.x.ToString();
            Y.Text = value.y.ToString();
            Z.Text = value.z.ToString();
            blockEvents = false;
        }

        // http://www.jeffreythompson.org/collision-detection/tri-point.php
        private bool TriangleCollision(float p1x, float p1y, float p2x, float p2y, float p3x, float p3y, float px, float py)
        {
            // get the area of the triangle
            float areaOrig = Math.Abs((p2x - p1x) * (p3y - p1y) - (p3x - p1x) * (p2y - p1y));

            // get the area of 3 triangles made between the point and the corners of the triangle
            float area1 = Math.Abs((p1x - px) * (p2y - py) - (p2x - px) * (p1y - py));
            float area2 = Math.Abs((p2x - px) * (p3y - py) - (p3x - px) * (p2y - py));
            float area3 = Math.Abs((p3x - px) * (p1y - py) - (p1x - px) * (p3y - py));

            // if the sum of the three areas equals the original, we're inside the triangle!
            return (area1 + area2 + area3 == areaOrig);
        }

        private bool PointBoxCollision(float px, float py, float left, float bottom, float width, float height)
        {
            // Graphic axis direction of going up being positive
            float right = left + width;
            float top = bottom + height;
            return px >= left && px < right && py >= bottom && py < top;
        }

        public void OpenContextLinedefMenu(int mx, int my, bool tagexist)
        {
            setAction704Item.Enabled = tagexist;
            setAction705Item.Enabled = tagexist;
            setAction714Item.Enabled = tagexist;
            setAction715Item.Enabled = tagexist;
            contextVSlopeLinedef.Show(mx, my);
        }

        public void OpenContextGroupMenu(int mx, int my, bool tagexist)
        {
            if (tagexist)
                contextExistingGroup.Show(mx, my);
            else
                contextNewGroup.Show(mx, my);
        }

        public void CreateList()
        {
            // Create dictionary
            vslopeDic = new SortedDictionary<int, List<Thing>>();
            vslopegroups.Items.Clear();
            foreach (Thing t in General.Map.Map.Things)
            {
                if (t.SRB2Type == 750)  // Vertex Slope
                {
                    if (vslopeDic.ContainsKey(t.AngleDoom))
                    {
                        vslopeDic[t.AngleDoom].Add(t);
                    }
                    else
                    {
                        List<Thing> thingsList = new List<Thing>();
                        thingsList.Add(t);
                        vslopeDic.Add(t.AngleDoom, thingsList);
                    }
                }
            }

            // Generate ListBox items
            vslopeTags = new List<int>();
            vslopegroups.Items.Clear();
            foreach (KeyValuePair<int, List<Thing>> kvp in vslopeDic)
            {
                vslopeTags.Add(kvp.Key);
                if (kvp.Value.Count == 3) {
                    vslopegroups.Items.Add(string.Format("Tag {0} - Triangle", kvp.Key));
                }
                else if (kvp.Value.Count == 1)
                {
                    vslopegroups.Items.Add(string.Format("Tag {0} - Vertex", kvp.Key));
                }
                else
                {
                    vslopegroups.Items.Add(string.Format("Tag {0} - INVALID! {1} Vertices", kvp.Key, kvp.Value.Count));
                }
            }

            // Select tag from list
            int tagid = tagID.GetResult(0);
            vslopegroups.SelectedIndex = vslopeTags.IndexOf(tagid);
        }

        public void UpdateForm()
        {
            blockEvents = true;
            int tagid = tagID.GetResult(0);
            bool tagexist = vslopeTags.Contains(tagid);
            toolNewTriangleGroup.Enabled = !tagexist;
            toolNewVertexGroup.Enabled = !tagexist;
            toolRemakeGroup.Enabled = tagexist;
            toolRemakeAsTriangleGroup.Enabled = tagexist;
            toolRemakeAsVertexGroup.Enabled = tagexist;
            toolChangeTagGroup.Enabled = tagexist;
            toolDeleteGroup.Enabled = tagexist;
            toolDrawNewTaggedTriangle.Enabled = !tagexist;
            if (tagexist)
                vslopegroups.ContextMenuStrip = contextExistingGroup;
            else
                vslopegroups.ContextMenuStrip = contextNewGroup;
            vslopegroups.SelectedIndex = vslopeTags.IndexOf(tagid);
            if (tagexist)
            {
                List<Thing> things = vslopeDic[tagid];
                if (things.Count >= 1)
                {
                    Thing thing = things[0];
                    Vector3D pos = thing.Position;
                    vslopeX1.Text = pos.x.ToString();
                    vslopeY1.Text = pos.y.ToString();
                    if (thing.Parameter == 0)
                    {
                        vslopeAbsZ1.Checked = false;
                        labelVSlope1.Text = "H1:";
                        vslopeZ1.Text = pos.z.ToString();
                    }
                    else
                    {
                        vslopeAbsZ1.Checked = true;
                        labelVSlope1.Text = "Z1:";
                        vslopeZ1.Text = thing.GetFlagsValue().ToString();
                    }
                }
                if (things.Count >= 2)
                {
                    Thing thing = things[1];
                    Vector3D pos = thing.Position;
                    vslopeX2.Text = pos.x.ToString();
                    vslopeY2.Text = pos.y.ToString();
                    if (thing.Parameter == 0)
                    {
                        vslopeAbsZ2.Checked = false;
                        labelVSlope2.Text = "H2:";
                        vslopeZ2.Text = pos.z.ToString();
                    }
                    else
                    {
                        vslopeAbsZ2.Checked = true;
                        labelVSlope2.Text = "Z2:";
                        vslopeZ2.Text = thing.GetFlagsValue().ToString();
                    }
                }
                if (things.Count >= 3)
                {
                    Thing thing = things[2];
                    Vector3D pos = thing.Position;
                    vslopeX3.Text = pos.x.ToString();
                    vslopeY3.Text = pos.y.ToString();
                    if (thing.Parameter == 0)
                    {
                        vslopeAbsZ3.Checked = false;
                        labelVSlope3.Text = "H3:";
                        vslopeZ3.Text = pos.z.ToString();
                    }
                    else
                    {
                        vslopeAbsZ3.Checked = true;
                        labelVSlope3.Text = "Z3:";
                        vslopeZ3.Text = thing.GetFlagsValue().ToString();
                    }
                }
                vslopeX1.Enabled = (things.Count >= 1);
                vslopeY1.Enabled = (things.Count >= 1);
                vslopeZ1.Enabled = (things.Count >= 1);
                vslopeAbsZ1.Enabled = (things.Count >= 1);
                vslopeX2.Enabled = (things.Count >= 2);
                vslopeY2.Enabled = (things.Count >= 2);
                vslopeZ2.Enabled = (things.Count >= 2);
                vslopeAbsZ2.Enabled = (things.Count >= 2);
                vslopeX3.Enabled = (things.Count >= 3);
                vslopeY3.Enabled = (things.Count >= 3);
                vslopeZ3.Enabled = (things.Count >= 3);
                vslopeAbsZ3.Enabled = (things.Count >= 3);
            }
            else
            {
                vslopeX1.Enabled = false;
                vslopeY1.Enabled = false;
                vslopeZ1.Enabled = true;
                vslopeAbsZ1.Enabled = true;
                vslopeX2.Enabled = false;
                vslopeY2.Enabled = false;
                vslopeZ2.Enabled = true;
                vslopeAbsZ2.Enabled = true;
                vslopeX3.Enabled = false;
                vslopeY3.Enabled = false;
                vslopeZ3.Enabled = true;
                vslopeAbsZ3.Enabled = true;
            }
            blockEvents = false;
        }

        public int VerticesInTag()
        {
            int tagid = tagID.GetResult(0);
            bool tagexist = vslopeTags.Contains(tagid);
            if (tagexist)
            {
                return vslopeDic[tagid].Count;
            }
            return 0;
        }

        public int VerticesInTag(int tagid)
        {
            bool tagexist = vslopeTags.Contains(tagid);
            if (tagexist)
            {
                return vslopeDic[tagid].Count;
            }
            return 0;
        }

        public List<Thing> GetThingsList()
        {
            int tagid = tagID.GetResult(0);
            bool tagexist = vslopeTags.Contains(tagid);
            if (tagexist)
            {
                return vslopeDic[tagid];
            }
            return null;
        }

        public List<Thing> GetThingsList(int tagid)
        {
            bool tagexist = vslopeTags.Contains(tagid);
            if (tagexist)
            {
                return vslopeDic[tagid];
            }
            return null;
        }

        public int GetGroupsAt(Vector2D pos, ref List<int> groups, bool vertexgroups, bool trianglegroups)
        {
            int numgroups = 0;
            foreach (KeyValuePair<int, List<Thing>> kvp in vslopeDic)
            {
                if (vertexgroups && kvp.Value.Count == 1)
                {
                    if (PointBoxCollision(pos.x, pos.y, kvp.Value[0].Position.x - 8f, kvp.Value[0].Position.y - 8f, 16f, 16f))
                    {
                        numgroups++;
                        groups.Add(kvp.Key);
                    }
                }
                else if (trianglegroups && kvp.Value.Count == 3)
                {
                    if (TriangleCollision(
                            kvp.Value[0].Position.x, kvp.Value[0].Position.y,
                            kvp.Value[1].Position.x, kvp.Value[1].Position.y,
                            kvp.Value[2].Position.x, kvp.Value[2].Position.y,
                            pos.x, pos.y))
                    {
                        numgroups++;
                        groups.Add(kvp.Key);
                    }
                }
            }
            return numgroups;
        }

        #endregion

        #region ================== Interface

        // Window closing
        private void VertexSlopeAssistForm_FormClosing(object sender, FormClosingEventArgs e)
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

            blockEvents = true;
            CreateList();
            blockEvents = false;
            tagID_ValueChanged(tagID, EventArgs.Empty);

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

        private void VertexSlopeAssistForm_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            General.ShowHelp("e_vertexslopeassist.html");
        }

        private void tagID_ValueChanged(object sender, EventArgs e)
        {
            if (blockEvents) return;

            UpdateForm();
            if (OnGroupChanged != null) OnGroupChanged(sender, e);
            ValueChanged(sender, e);
        }

        private void vslopegroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (blockEvents) return;

            if (vslopegroups.SelectedIndex == -1) return;
            int tagid = vslopeTags[vslopegroups.SelectedIndex];
            tagID.Text = tagid.ToString();

            if (OnGroupChanged != null) OnGroupChanged(sender, e);
            ValueChanged(sender, e);
        }

        private void vslopegroups_DoubleClick(object sender, EventArgs e)
        {
            if (OnGroupChoosen != null) OnGroupChoosen(sender, e);
            ValueChanged(sender, e);
        }

        private void drawNewTaggedTriangle_Click(object sender, EventArgs e)
        {
            if (OnDrawNewTaggedTriangle != null) OnDrawNewTaggedTriangle(sender, e);
            ValueChanged(sender, e);
        }

        private void drawNewEmptyTriangle_Click(object sender, EventArgs e)
        {
            if (OnDrawNewEmptyTriangle != null) OnDrawNewEmptyTriangle(sender, e);
            ValueChanged(sender, e);
        }

        private void newtag_Click(object sender, EventArgs e)
        {
            tagID.Text = General.Map.Map.GetNewTag(vslopeTags).ToString();
            ValueChanged(sender, e);
        }

        private void newTriangleGroup_Click(object sender, EventArgs e)
        {
            if (OnGroupNewTriangle != null) OnGroupNewTriangle(sender, e);
            ValueChanged(sender, e);
        }

        private void newVertexGroup_Click(object sender, EventArgs e)
        {
            if (OnGroupNewVertex != null) OnGroupNewVertex(sender, e);
            ValueChanged(sender, e);
        }

        private void remakeGroup_Click(object sender, EventArgs e)
        {
            if (OnGroupModify != null) OnGroupModify(sender, e);
            ValueChanged(sender, e);
        }

        private void remakeAsTriangleGroup_Click(object sender, EventArgs e)
        {
            if (OnGroupModifyAsTriangle != null) OnGroupModifyAsTriangle(sender, e);
            ValueChanged(sender, e);
        }

        private void remakeAsVertexGroup_Click(object sender, EventArgs e)
        {
            if (OnGroupModifyAsVertex != null) OnGroupModifyAsVertex(sender, e);
            ValueChanged(sender, e);
        }

        private void changeTagGroup_Click(object sender, EventArgs e)
        {
            if (OnGroupTag != null) OnGroupTag(sender, e);
            ValueChanged(sender, e);
        }

        private void deleteGroup_Click(object sender, EventArgs e)
        {
            if (OnGroupDelete != null) OnGroupDelete(sender, e);
            ValueChanged(sender, e);
        }

        private void vslope_ValueChanged(object sender, EventArgs e)
        {
            if (blockEvents) return;

            if (OnValueChanged != null) OnValueChanged(sender, e);
            ValueChanged(sender, e);
        }

        private void rot_all_Click(object sender, EventArgs e)
        {
            if (OnRotateAll != null) OnRotateAll(sender, e);
            ValueChanged(sender, e);
        }

        private void flip_all_Click(object sender, EventArgs e)
        {
            if (OnFlipAll != null) OnFlipAll(sender, e);
            ValueChanged(sender, e);
        }

        private void rot_height_Click(object sender, EventArgs e)
        {
            if (OnRotateHeight != null) OnRotateHeight(sender, e);
            ValueChanged(sender, e);
        }

        private void flip_height_Click(object sender, EventArgs e)
        {
            if (OnFlipHeight != null) OnFlipHeight(sender, e);
            ValueChanged(sender, e);
        }

        private void heightAdj_ValueChanged(object sender, EventArgs e)
        {
            int adjust = -heightAdj.Value;

            // Modifiers
            if ((ModifierKeys & Keys.Control) == Keys.Control)
                adjust *= HEIGHTADJ_SMALL;
            else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                adjust *= HEIGHTADJ_BIG;
            else
                adjust *= HEIGHTADJ_STEP;

            // Adjust heights...
            vslopeZ1.Text = (vslopeZ1.GetResult(0) + adjust).ToString();
            vslopeZ2.Text = (vslopeZ2.GetResult(0) + adjust).ToString();
            vslopeZ3.Text = (vslopeZ3.GetResult(0) + adjust).ToString();
            heightAdj.Value = 0;
        }

        private void setAction704Item_Click(object sender, EventArgs e)
        {
            if (OnContextSet != null) OnContextSet(sender, 704);
            ValueChanged(sender, e);
        }

        private void setAction705Item_Click(object sender, EventArgs e)
        {
            if (OnContextSet != null) OnContextSet(sender, 705);
            ValueChanged(sender, e);
        }

        private void setAction714Item_Click(object sender, EventArgs e)
        {
            if (OnContextSet != null) OnContextSet(sender, 714);
            ValueChanged(sender, e);
        }

        private void setAction715Item_Click(object sender, EventArgs e)
        {
            if (OnContextSet != null) OnContextSet(sender, 715);
            ValueChanged(sender, e);
        }

        private void removeActionItem_Click(object sender, EventArgs e)
        {
            if (OnContextSet != null) OnContextSet(sender, 0);
            ValueChanged(sender, e);
        }

        private void contextVSlopeLinedef_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            if (OnContextSet != null) OnContextSet(sender, -1);
            ValueChanged(sender, e);
        }

        #endregion

    }
}
