#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Windows;
using System.Drawing;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.BuilderModes.Interface;

#endregion

//JBR Vertex Slope Assistant mode
namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Vertex Slope Assistant",
			  AllowCopyPaste = false,
			  Volatile = true)]
	public sealed class VertexSlopeAssistMode : BaseClassicMode
	{
		#region ================== Constants

        private const float LINE_THICKNESS = 1.2f;
		private const float OVER_THICKNESS = 0.6f;
        private const int MODE_SELECT = 0;
        private const int MODE_ASSIGNGROUPS = 1;
        private const int MODE_NEWPOINTS = 4; // This mode doesn't exist but tell which modes begin to set points
        private const int MODE_NEWTAGGEDTRIANGLE = 4;
        private const int MODE_NEWEMPTYTRIANGLE = 5;
        private const int MODE_NEWGROUP = 6;
        private const int MODE_MODGROUP = 7;

		#endregion

		#region ================== Variables

        public int mode;
        public bool createdUndo;
        public TextLabel[] vertexnames;
        public List<DrawnVertex> points;
        public int expectedPoints;

        // Groups selection
        public List<int> overgroups;
        public int overgroupoffset;
        public int overTagID;

        // Linedef stuff
        public Linedef selectedLinedef;
        public Linedef indexedLinedef;
        public int indexedAction;
        public List<int> indexedTags;
        
        // Current mouse position
        public Vector2D curmousepos;

        public bool snaptogrid;		// SHIFT to toggle
        public bool snaptonearest;	// CTRL to enable
        public bool snaptocardinaldirection; //mxd. ALT-SHIFT to enable

		#endregion

		#region ================== Properties

		// Just keep the base mode button checked
		public override string EditModeButtonName { get { return General.Editing.PreviousStableMode.Name; } }

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public VertexSlopeAssistMode(EditMode basemode)
		{
            vertexnames = new TextLabel[4];
            for (int i = 0; i < 4; i++)
            {
                vertexnames[i] = new TextLabel(1);
                vertexnames[i].TransformCoords = true;
                vertexnames[i].Rectangle = new RectangleF(0f, 0f, 0f, 0f);
                vertexnames[i].AlignX = TextAlignmentX.Center;
                vertexnames[i].AlignY = TextAlignmentY.Middle;
                vertexnames[i].Scale = 16f;
                vertexnames[i].Color = General.Colors.Highlight.WithAlpha(255);
                vertexnames[i].Backcolor = General.Colors.Background.WithAlpha(255);
            }
            points = new List<DrawnVertex>();
            overgroups = new List<int>();
            overgroupoffset = 0;
            curmousepos = new Vector2D();
            overTagID = -1;
            expectedPoints = 3;
            selectedLinedef = null;
            indexedLinedef = null;
            indexedAction = 0;
            indexedTags = new List<int>();
            SetMode(MODE_SELECT);
        }
		
		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Clean up
                foreach (TextLabel tl in vertexnames) tl.Dispose();

				// Done
				base.Dispose();
			}
		}

		#endregion
		
		#region ================== Methods

        public void CreateUndo()
        {
            if (!createdUndo)
            {
                General.Map.UndoRedo.CreateUndo("Vertex Slope Assistant");
                createdUndo = true;
            }
        }

        public void UpdateModeText()
        {
            if (mode == MODE_SELECT)
            {
                if (overTagID != -1)
                    BuilderPlug.Me.VertexSlopeAssistForm.ModeText = string.Format("Select/Edit group, Click to select tag {0}", overTagID);
                else
                    BuilderPlug.Me.VertexSlopeAssistForm.ModeText = string.Format("Select/Edit group");
            }
            if (mode == MODE_ASSIGNGROUPS)
            {
                BuilderPlug.Me.VertexSlopeAssistForm.ModeText = string.Format("Double-Click on Group List to pick Vertex {0}", indexedTags.Count + 1);
            }
            if (mode == MODE_NEWTAGGEDTRIANGLE || mode == MODE_NEWEMPTYTRIANGLE)
            {
                BuilderPlug.Me.VertexSlopeAssistForm.ModeText = string.Format("Creating new triangle... Click to make point {0}", points.Count + 1);
            }
            if (mode == MODE_NEWGROUP)
            {
                BuilderPlug.Me.VertexSlopeAssistForm.ModeText = string.Format("Making new group... Click to make point {0}", points.Count + 1);
            }
            if (mode == MODE_MODGROUP)
            {
                BuilderPlug.Me.VertexSlopeAssistForm.ModeText = string.Format("Remaking group... Click to make point {0}", points.Count + 1);
            }
        }

        public void SetMode(int newmode)
        {
            mode = newmode;
            UpdateModeText();
            points.Clear();
            indexedTags.Clear();
            selectedLinedef = null;
            if (newmode != MODE_ASSIGNGROUPS) indexedLinedef = null;
        }

        public void CB_DrawNewTaggedTriangle(object sender, EventArgs eventArgs)
        {
            expectedPoints = 3;
            SetMode(MODE_NEWTAGGEDTRIANGLE);
        }

        public void CB_DrawNewEmptyTriangle(object sender, EventArgs eventArgs)
        {
            expectedPoints = 3;
            SetMode(MODE_NEWEMPTYTRIANGLE);
        }

        public void CB_TagGroup_Changed(object sender, EventArgs eventArgs)
        {
            if (mode != MODE_ASSIGNGROUPS)
                SetMode(MODE_SELECT);
            else
                General.Interface.RedrawDisplay();
        }

        public void CB_TagGroup_Choosen(object sender, EventArgs eventArgs)
        {
            if (mode == MODE_ASSIGNGROUPS)
            {
                List<Thing> things = BuilderPlug.Me.VertexSlopeAssistForm.GetThingsList();
                if (things != null && things.Count == 1)
                {
                    DrawnVertex dv = new DrawnVertex();
                    dv.stitch = true;
                    dv.stitchline = true;
                    dv.pos = things[0].Position;
                    points.Add(dv);
                    indexedTags.Add(BuilderPlug.Me.VertexSlopeAssistForm.TagID);
                    if (indexedTags.Count >= 3)
                    {
                        if (indexedLinedef != null)
                        {
                            CreateUndo();
                            indexedLinedef.Tag = indexedTags[0];
                            indexedLinedef.SetFlag("8192", true);
                            if (indexedAction < 710) // 704, 705
                            {
                                indexedLinedef.Front.OffsetX = indexedTags[1];
                                indexedLinedef.Front.OffsetY = indexedTags[2];
                            }
                            else // 714, 715
                            {
                                indexedLinedef.Back.OffsetX = indexedTags[1];
                                indexedLinedef.Back.OffsetY = indexedTags[2];
                            }
                            indexedLinedef.Action = indexedAction;
                            General.Interface.DisplayStatus(StatusType.Action, string.Format("Action {0} and Tag {1} set to Linedef {2}.", indexedLinedef.Action, indexedLinedef.Tag, indexedLinedef.Index));
                        }
                        SetMode(MODE_SELECT);
                    }
                }
                else
                {
                    General.Interface.DisplayStatus(StatusType.Warning, "Only single vertex slope groups can be assigned.");
                }
                UpdateModeText();
            }
        }

        public void CB_TagGroup_NewTriangle(object sender, EventArgs eventArgs)
        {
            expectedPoints = 3;
            SetMode(MODE_NEWGROUP);
        }

        public void CB_TagGroup_NewVertex(object sender, EventArgs eventArgs)
        {
            expectedPoints = 1;
            SetMode(MODE_NEWGROUP);
        }

        public void CB_TagGroup_Modify(object sender, EventArgs eventArgs)
        {
            int vertices = BuilderPlug.Me.VertexSlopeAssistForm.VerticesInTag();
            if (vertices == 1)
                expectedPoints = 1;
            else
                expectedPoints = 3;
            SetMode(MODE_MODGROUP);
        }

        public void CB_TagGroup_ModifyAsTriangle(object sender, EventArgs eventArgs)
        {
            expectedPoints = 3;
            SetMode(MODE_MODGROUP);
        }

        public void CB_TagGroup_ModifyAsVertex(object sender, EventArgs eventArgs)
        {
            expectedPoints = 1;
            SetMode(MODE_MODGROUP);
        }

        public void CB_TagGroup_Tag(object sender, EventArgs eventArgs)
        {
            SetMode(MODE_SELECT);
            int oldtag = BuilderPlug.Me.VertexSlopeAssistForm.TagID;
            BuilderPlug.Me.VertexSlopeAssistForm.Hide();
            int newtag = VertexSlopeAssistTagForm.RunDialog(BuilderPlug.Me.VertexSlopeAssistForm, oldtag);
            BuilderPlug.Me.VertexSlopeAssistForm.Show();
            BuilderPlug.Me.VertexSlopeAssistForm.Activate();
            if (newtag != -1 && newtag != oldtag)
            {
                bool exists = BuilderPlug.Me.VertexSlopeAssistForm.VerticesInTag(newtag) > 0;
                if (exists)
                {
                    General.Interface.DisplayStatus(StatusType.Warning, string.Format("Tag {0} already belongs to another group. Aborted retagging.", newtag));
                }
                else
                {
                    CreateUndo();
                    List<Thing> things = BuilderPlug.Me.VertexSlopeAssistForm.GetThingsList();
                    if (things != null)
                    {
                        foreach (Thing t in things)
                        {
                            t.Rotate(newtag);
                            t.UpdateConfiguration();
                        }
                    }
                    General.Map.ThingsFilter.Update();
                    General.Map.Map.Update();
                    General.Map.IsChanged = true;
                    BuilderPlug.Me.VertexSlopeAssistForm.TagID = newtag;
                    BuilderPlug.Me.VertexSlopeAssistForm.CreateList();
                    BuilderPlug.Me.VertexSlopeAssistForm.UpdateForm();
                }
            }
        }

        public void CB_TagGroup_Delete(object sender, EventArgs eventArgs)
        {
            CreateUndo();
            List<Thing> things = BuilderPlug.Me.VertexSlopeAssistForm.GetThingsList();
            if (things != null)
            {
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    things[i].Dispose();
                }
            }
            General.Map.ThingsFilter.Update();
            General.Map.Map.Update();
            General.Map.IsChanged = true;
            BuilderPlug.Me.VertexSlopeAssistForm.CreateList();
            BuilderPlug.Me.VertexSlopeAssistForm.UpdateForm();
            SetMode(MODE_SELECT);
        }

        public void CB_ValueChanged(object sender, EventArgs eventArgs)
        {
            CreateUndo();
            List<Thing> things = BuilderPlug.Me.VertexSlopeAssistForm.GetThingsList();
            if (things != null)
            {
                CreateUndo();
                if (things.Count >= 1)
                {
                    things[0].Move(BuilderPlug.Me.VertexSlopeAssistForm.Vertex1);
                    if (BuilderPlug.Me.VertexSlopeAssistForm.Vertex1AbsZ)
                        ChangeVSlopeFlags(things[0], (int)BuilderPlug.Me.VertexSlopeAssistForm.Vertex1.z);
                    things[0].Parameter = BuilderPlug.Me.VertexSlopeAssistForm.Vertex1AbsZ ? 1 : 0;
                    things[0].UpdateConfiguration();
                }
                if (things.Count >= 2)
                {
                    things[1].Move(BuilderPlug.Me.VertexSlopeAssistForm.Vertex2);
                    if (BuilderPlug.Me.VertexSlopeAssistForm.Vertex2AbsZ)
                        ChangeVSlopeFlags(things[1], (int)BuilderPlug.Me.VertexSlopeAssistForm.Vertex2.z);
                    things[1].Parameter = BuilderPlug.Me.VertexSlopeAssistForm.Vertex2AbsZ ? 1 : 0;
                    things[1].UpdateConfiguration();
                }
                if (things.Count >= 3)
                {
                    things[2].Move(BuilderPlug.Me.VertexSlopeAssistForm.Vertex3);
                    if (BuilderPlug.Me.VertexSlopeAssistForm.Vertex3AbsZ)
                        ChangeVSlopeFlags(things[2], (int)BuilderPlug.Me.VertexSlopeAssistForm.Vertex3.z);
                    things[2].Parameter = BuilderPlug.Me.VertexSlopeAssistForm.Vertex3AbsZ ? 1 : 0;
                    things[2].UpdateConfiguration();
                }
                General.Map.ThingsFilter.Update();
                General.Map.Map.Update();
                General.Map.IsChanged = true;
                BuilderPlug.Me.VertexSlopeAssistForm.UpdateForm();
                SetMode(MODE_SELECT);
            }
        }

        public void CB_Rotate_All_VSlopes(object sender, EventArgs eventArgs)
        {
            int vertices = BuilderPlug.Me.VertexSlopeAssistForm.VerticesInTag();
            if (vertices == 0 || vertices >= 3)
            {
                Vector3D v1 = BuilderPlug.Me.VertexSlopeAssistForm.Vertex1;
                Vector3D v2 = BuilderPlug.Me.VertexSlopeAssistForm.Vertex2;
                Vector3D v3 = BuilderPlug.Me.VertexSlopeAssistForm.Vertex3;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex1 = v2;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex2 = v3;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex3 = v1;
                bool v1z = BuilderPlug.Me.VertexSlopeAssistForm.Vertex1AbsZ;
                bool v2z = BuilderPlug.Me.VertexSlopeAssistForm.Vertex2AbsZ;
                bool v3z = BuilderPlug.Me.VertexSlopeAssistForm.Vertex3AbsZ;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex1AbsZ = v2z;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex2AbsZ = v3z;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex3AbsZ = v1z;
                CB_ValueChanged(sender, eventArgs);
            }
        }

        public void CB_Flip_All_VSlopes(object sender, EventArgs eventArgs)
        {
            int vertices = BuilderPlug.Me.VertexSlopeAssistForm.VerticesInTag();
            if (vertices == 0 || vertices >= 3)
            {
                Vector3D v2 = BuilderPlug.Me.VertexSlopeAssistForm.Vertex2;
                Vector3D v3 = BuilderPlug.Me.VertexSlopeAssistForm.Vertex3;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex2 = v3;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex3 = v2;
                bool v2z = BuilderPlug.Me.VertexSlopeAssistForm.Vertex2AbsZ;
                bool v3z = BuilderPlug.Me.VertexSlopeAssistForm.Vertex3AbsZ;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex2AbsZ = v3z;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex3AbsZ = v2z;
                CB_ValueChanged(sender, eventArgs);
            }
        }

        public void CB_Rotate_Height_VSlopes(object sender, EventArgs eventArgs)
        {
            int vertices = BuilderPlug.Me.VertexSlopeAssistForm.VerticesInTag();
            if (vertices == 0 || vertices >= 3)
            {
                Vector3D v1 = BuilderPlug.Me.VertexSlopeAssistForm.Vertex1;
                Vector3D v2 = BuilderPlug.Me.VertexSlopeAssistForm.Vertex2;
                Vector3D v3 = BuilderPlug.Me.VertexSlopeAssistForm.Vertex3;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex1 = new Vector3D(v1.x, v1.y, v2.z);
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex2 = new Vector3D(v2.x, v2.y, v3.z);
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex3 = new Vector3D(v3.x, v3.y, v1.z);
                bool v1z = BuilderPlug.Me.VertexSlopeAssistForm.Vertex1AbsZ;
                bool v2z = BuilderPlug.Me.VertexSlopeAssistForm.Vertex2AbsZ;
                bool v3z = BuilderPlug.Me.VertexSlopeAssistForm.Vertex3AbsZ;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex1AbsZ = v2z;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex2AbsZ = v3z;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex3AbsZ = v1z;
                CB_ValueChanged(sender, eventArgs);
            }
        }

        public void CB_Flip_Height_VSlopes(object sender, EventArgs eventArgs)
        {
            int vertices = BuilderPlug.Me.VertexSlopeAssistForm.VerticesInTag();
            if (vertices == 0 || vertices >= 3)
            {
                Vector3D v2 = BuilderPlug.Me.VertexSlopeAssistForm.Vertex2;
                Vector3D v3 = BuilderPlug.Me.VertexSlopeAssistForm.Vertex3;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex2 = new Vector3D(v2.x, v2.y, v3.z);
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex3 = new Vector3D(v3.x, v3.y, v2.z);
                bool v2z = BuilderPlug.Me.VertexSlopeAssistForm.Vertex2AbsZ;
                bool v3z = BuilderPlug.Me.VertexSlopeAssistForm.Vertex3AbsZ;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex2AbsZ = v3z;
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex3AbsZ = v2z;
                CB_ValueChanged(sender, eventArgs);
            }
        }

        public void CB_Context_Set(object sender, int action)
        {
            if (selectedLinedef != null && action >= 0)
            {
                if (action != 0)
                {
                    List<Thing> things = BuilderPlug.Me.VertexSlopeAssistForm.GetThingsList();
                    if (things != null)
                    {
                        if (things.Count == 3)
                        {
                            // Set tag and action
                            CreateUndo();
                            selectedLinedef.Tag = BuilderPlug.Me.VertexSlopeAssistForm.TagID;
                            selectedLinedef.SetFlag("8192", false);
                            selectedLinedef.Action = action;
                            General.Interface.DisplayStatus(StatusType.Action, string.Format("Action {0} and Tag {1} set to Linedef {2}.", selectedLinedef.Action, selectedLinedef.Tag, selectedLinedef.Index));
                        }
                        else if (things.Count == 1)
                        {
                            // Prepare for indexing groups
                            indexedLinedef = selectedLinedef;
                            indexedAction = action;
                            SetMode(MODE_ASSIGNGROUPS);
                            DrawnVertex dv = new DrawnVertex();
                            dv.stitch = true;
                            dv.stitchline = true;
                            dv.pos = things[0].Position;
                            points.Add(dv);
                            indexedTags.Add(BuilderPlug.Me.VertexSlopeAssistForm.TagID);
                            UpdateModeText();
                        }
                    }
                }
                else
                {
                    // Clear tag and action
                    CreateUndo();
                    selectedLinedef.Tag = 0;
                    selectedLinedef.Action = 0;
                    General.Interface.DisplayStatus(StatusType.Action, string.Format("Cleared Action and Tag on Linedef {0}.", selectedLinedef.Index));
                }
                selectedLinedef = null;
                General.Interface.RedrawDisplay();
            }
            if (action == -1)
            {
                // For some reason context menu emit a close before the option that user have choosen o_O
                Timer delaytimer = new Timer();
                delaytimer.Tick += (obj, e) =>
                {
                    delaytimer.Stop();
                    selectedLinedef = null;
                    General.Interface.RedrawDisplay();
                    delaytimer.Dispose();
                };
                delaytimer.Interval = 20;
                delaytimer.Start();
            }
        }

        public void ChangeVSlopeFlags(Thing thing, int flags)
        {
            // SetFlagsValue doesn't set flags properly if thing's flags was properly initialized
            thing.SetFlag("1", (flags & 1) == 1);
            thing.SetFlag("2", (flags & 2) == 2);
            thing.SetFlag("4", (flags & 4) == 4);
            thing.SetFlag("8", (flags & 8) == 8);
            thing.SetFlagsValue(flags);
        }

        public bool LineCollision(float p1x, float p1y, float p2x, float p2y, float px, float py, float epsilon)
        {
            float l1 = new Line2D(px, py, p1x, p1y).GetLength();
            float l2 = new Line2D(px, py, p2x, p2y).GetLength();
            float ln = new Line2D(p1x, p1y, p2x, p2y).GetLength();
            return l1 + l2 <= ln + epsilon;
        }

        // http://www.jeffreythompson.org/collision-detection/tri-point.php
        public bool TriangleCollision(float p1x, float p1y, float p2x, float p2y, float p3x, float p3y, float px, float py)
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

        private List<Vector2D> GetAffineThingsPosition(bool insideaffine, int vertices)
        {
            List<Vector2D> affined = new List<Vector2D>();

            // If no need of inside affine just return the rounded points
            if (!insideaffine || vertices != 3)
            {
                for (int i = 0; i < vertices; i++)
                {
                    affined.Add(points[i].pos.GetRounded());
                }
                return affined;
            }

            Vector2D[] corners = new Vector2D[] {
                points[0].pos.GetRounded(),
                points[1].pos.GetRounded(),
                points[2].pos.GetRounded()
            };

            // Move vertex slopes away from edges or corners.
            for (int i = 0; i < 3; i++)
            {
                Vector2D corner = new Vector2D(corners[i].x, corners[i].y);

                // Find best cell... very slow but should get best result
                Vector2D best = new Vector2D(corner);
                float bestlen = 999f;
                Vector2D point = new Vector2D();
                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        // Point to test
                        point = corner + new Vector2D(x - 128, y - 128);

                        if (TriangleCollision(
                            corners[0].x, corners[0].y,
                            corners[1].x, corners[1].y,
                            corners[2].x, corners[2].y,
                            point.x, point.y))
                        {
                            // Good, we are inside the triangle atleast, now avoid the edges...
                            if (!LineCollision(
                                    corners[0].x, corners[0].y,
                                    corners[1].x, corners[1].y,
                                    point.x, point.y, 0.001f) &&
                                !LineCollision(
                                    corners[1].x, corners[1].y,
                                    corners[2].x, corners[2].y,
                                    point.x, point.y, 0.001f) &&
                                !LineCollision(
                                    corners[2].x, corners[2].y,
                                    corners[0].x, corners[0].y,
                                    point.x, point.y, 0.001f))
                            {
                                // Cool, we test if this one is better then...
                                float len = (point - corner).GetLength();
                                if (len < bestlen)
                                {
                                    best.x = point.x;
                                    best.y = point.y;
                                    bestlen = len;
                                }
                            }
                        }
                    }
                }
                affined.Add(best);
            }

            return affined;
        }

        private void ApplyVSlopeChanges()
        {
            float[] heights = {
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex1.z,
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex2.z,
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex3.z
            };
            bool[] absZs = {
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex1AbsZ,
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex2AbsZ,
                BuilderPlug.Me.VertexSlopeAssistForm.Vertex3AbsZ
            };
            CreateUndo();
            if (mode == MODE_NEWTAGGEDTRIANGLE)
            {
                List<Vector2D> newthings = GetAffineThingsPosition(BuilderPlug.Me.VertexSlopeAssistForm.AffineVSlopes, expectedPoints);
                for (int i = 0; i < 3; i++)
                {
                    Thing tp = General.Map.Map.CreateThing();
                    if (tp != null)
                    {
                        General.Settings.ApplyDefaultThingSettings(tp);
                        tp.SRB2Type = 750;
                        tp.Parameter = 0;
                        tp.Rotate(BuilderPlug.Me.VertexSlopeAssistForm.TagID);
                        tp.Move(newthings[i].x, newthings[i].y, heights[i]);
                        if (absZs[i])
                            ChangeVSlopeFlags(tp, (int)heights[i]);
                        tp.Parameter = absZs[i] ? 1 : 0;
                        tp.UpdateConfiguration();
                    }
                }
                DrawnVertex dv = new DrawnVertex();
                dv.stitch = true;
                dv.stitchline = true;
                dv.pos = points[0].pos;
                points.Add(dv);
                if (!Tools.DrawLines(points, true, BuilderPlug.Me.AutoAlignTextureOffsetsOnCreate))
                {
                    General.Map.UndoRedo.WithdrawUndo();
                }
                else
                {
                    // Snap to map format accuracy
                    General.Map.Map.SnapAllToAccuracy();

                    // Clear selection
                    General.Map.Map.ClearAllSelected();

                    // Update cached values
                    General.Map.Map.Update();

                    // Edit new sectors?
                    List<Sector> newsectors = General.Map.Map.GetMarkedSectors(true);
                    if (BuilderPlug.Me.EditNewSector && (newsectors.Count > 0))
                        General.Interface.ShowEditSectors(newsectors);

                    // Update the used textures
                    General.Map.Data.UpdateUsedTextures();

                    //mxd
                    General.Map.Renderer2D.UpdateExtraFloorFlag();
                }
            }
            if (mode == MODE_NEWEMPTYTRIANGLE)
            {
                DrawnVertex dv = new DrawnVertex();
                dv.stitch = true;
                dv.stitchline = true;
                dv.pos = points[0].pos;
                points.Add(dv);
                if (!Tools.DrawLines(points, true, BuilderPlug.Me.AutoAlignTextureOffsetsOnCreate))
                {
                    General.Map.UndoRedo.WithdrawUndo();
                }

                // Snap to map format accuracy
                General.Map.Map.SnapAllToAccuracy();

                // Clear selection
                General.Map.Map.ClearAllSelected();

                // Update cached values
                General.Map.Map.Update();

                // Edit new sectors?
                List<Sector> newsectors = General.Map.Map.GetMarkedSectors(true);
                if (BuilderPlug.Me.EditNewSector && (newsectors.Count > 0))
                    General.Interface.ShowEditSectors(newsectors);

                // Update the used textures
                General.Map.Data.UpdateUsedTextures();

                //mxd
                General.Map.Renderer2D.UpdateExtraFloorFlag();
            }
            if (mode == MODE_NEWGROUP)
            {
                List<Vector2D> newthings = GetAffineThingsPosition(BuilderPlug.Me.VertexSlopeAssistForm.AffineVSlopes, expectedPoints);
                for (int i = 0; i < expectedPoints; i++)
                {
                    Thing tp = General.Map.Map.CreateThing();
                    if (tp != null)
                    {
                        General.Settings.ApplyDefaultThingSettings(tp);
                        tp.SRB2Type = 750;
                        tp.Parameter = 0;
                        tp.Rotate(BuilderPlug.Me.VertexSlopeAssistForm.TagID);
                        tp.Move(newthings[i].x, newthings[i].y, heights[i]);
                        if (absZs[i])
                            ChangeVSlopeFlags(tp, (int)heights[i]);
                        tp.Parameter = absZs[i] ? 1 : 0;
                        tp.UpdateConfiguration();
                    }
                }
            }
            if (mode == MODE_MODGROUP)
            {
                List<Vector2D> newthings = GetAffineThingsPosition(BuilderPlug.Me.VertexSlopeAssistForm.AffineVSlopes, expectedPoints);
                List<Thing> things = BuilderPlug.Me.VertexSlopeAssistForm.GetThingsList();
                for (int i = 0; i < expectedPoints; i++)
                {
                    if (things != null && i < things.Count)
                    {
                        things[i].Move(newthings[i]);
                    }
                    else
                    {
                        // Ops! Missing vertex slopes!
                        Thing tp = General.Map.Map.CreateThing();
                        if (tp != null)
                        {
                            General.Settings.ApplyDefaultThingSettings(tp);
                            tp.SRB2Type = 750;
                            tp.Parameter = 0;
                            tp.Rotate(BuilderPlug.Me.VertexSlopeAssistForm.TagID);
                            tp.Move(newthings[i].x, newthings[i].y, heights[i]);
                            if (absZs[i])
                                ChangeVSlopeFlags(tp, (int)heights[i]);
                            tp.Parameter = absZs[i] ? 1 : 0;
                            tp.UpdateConfiguration();
                        }
                    }
                }
                // Dispose extra vertex slopes
                for (int i = things.Count - 1; i >= expectedPoints; i--)
                {
                    things[i].Dispose();
                }
            }
            General.Map.ThingsFilter.Update();
            General.Map.Map.Update();
            General.Map.IsChanged = true;
            SetMode(MODE_SELECT);
            BuilderPlug.Me.VertexSlopeAssistForm.CreateList();
            BuilderPlug.Me.VertexSlopeAssistForm.UpdateForm();
        }

   		#endregion

		#region ================== Events

		public override void OnHelp()
		{
			General.ShowHelp("e_vertexslopeassist.html");
		}

		// Cancelled
		public override void OnCancel()
		{
			// Cancel base class
			base.OnCancel();

            if (createdUndo)
            {
                General.Map.UndoRedo.WithdrawUndo();
            }

			// Return to base mode
			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// Mode engages
		public override void OnEngage()
		{
			base.OnEngage();
			renderer.SetPresentation(Presentation.Standard);

            createdUndo = false;
            BuilderPlug.Me.VertexSlopeAssistForm.OnDrawNewTaggedTriangle += CB_DrawNewTaggedTriangle;
            BuilderPlug.Me.VertexSlopeAssistForm.OnDrawNewEmptyTriangle += CB_DrawNewEmptyTriangle;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupChanged += CB_TagGroup_Changed;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupChoosen += CB_TagGroup_Choosen;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupNewTriangle += CB_TagGroup_NewTriangle;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupNewVertex += CB_TagGroup_NewVertex;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupModify += CB_TagGroup_Modify;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupModifyAsTriangle += CB_TagGroup_ModifyAsTriangle;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupModifyAsVertex += CB_TagGroup_ModifyAsVertex;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupTag += CB_TagGroup_Tag;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupDelete += CB_TagGroup_Delete;
            BuilderPlug.Me.VertexSlopeAssistForm.OnValueChanged += CB_ValueChanged;
            BuilderPlug.Me.VertexSlopeAssistForm.OnRotateAll += CB_Rotate_All_VSlopes;
            BuilderPlug.Me.VertexSlopeAssistForm.OnFlipAll += CB_Flip_All_VSlopes;
            BuilderPlug.Me.VertexSlopeAssistForm.OnRotateHeight += CB_Rotate_Height_VSlopes;
            BuilderPlug.Me.VertexSlopeAssistForm.OnFlipHeight += CB_Flip_Height_VSlopes;
            BuilderPlug.Me.VertexSlopeAssistForm.OnContextSet += CB_Context_Set;
            selectedLinedef = null;

            // Show toolbox window
            BuilderPlug.Me.VertexSlopeAssistForm.Show((Form)General.Interface);
        }

		// Disenagaging
		public override void OnDisengage()
		{
			base.OnDisengage();
            BuilderPlug.Me.VertexSlopeAssistForm.OnDrawNewTaggedTriangle -= CB_DrawNewTaggedTriangle;
            BuilderPlug.Me.VertexSlopeAssistForm.OnDrawNewEmptyTriangle -= CB_DrawNewEmptyTriangle;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupChanged -= CB_TagGroup_Changed;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupChoosen -= CB_TagGroup_Choosen;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupNewTriangle -= CB_TagGroup_NewTriangle;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupNewVertex -= CB_TagGroup_NewVertex;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupModify -= CB_TagGroup_Modify;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupModifyAsTriangle -= CB_TagGroup_ModifyAsTriangle;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupModifyAsVertex -= CB_TagGroup_ModifyAsVertex;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupTag -= CB_TagGroup_Tag;
            BuilderPlug.Me.VertexSlopeAssistForm.OnGroupDelete -= CB_TagGroup_Delete;
            BuilderPlug.Me.VertexSlopeAssistForm.OnValueChanged -= CB_ValueChanged;
            BuilderPlug.Me.VertexSlopeAssistForm.OnRotateAll -= CB_Rotate_All_VSlopes;
            BuilderPlug.Me.VertexSlopeAssistForm.OnFlipAll -= CB_Flip_All_VSlopes;
            BuilderPlug.Me.VertexSlopeAssistForm.OnRotateHeight -= CB_Rotate_Height_VSlopes;
            BuilderPlug.Me.VertexSlopeAssistForm.OnFlipHeight -= CB_Flip_Height_VSlopes;
            BuilderPlug.Me.VertexSlopeAssistForm.OnContextSet -= CB_Context_Set;

			// Hide toolbox window
            BuilderPlug.Me.VertexSlopeAssistForm.Hide();
		}

		// This applies the curves and returns to the base mode
		public override void OnAccept()
		{
            // Return to base mode
            General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// Redrawing display
		public override void OnRedrawDisplay()
		{
			renderer.RedrawSurface();
            float vsize = (renderer.VertexSize + 1.0f) / renderer.Scale;

            snaptocardinaldirection = General.Interface.ShiftState && General.Interface.AltState; //mxd
            snaptogrid = (snaptocardinaldirection || General.Interface.ShiftState ^ General.Interface.SnapToGrid);
            snaptonearest = General.Interface.CtrlState ^ General.Interface.AutoMerge;

            DrawnVertex curp = DrawGeometryMode.GetCurrentPosition(mousemappos, snaptonearest, snaptogrid, snaptocardinaldirection, renderer, points);
            if (curp.pos.IsFinite()) curmousepos = curp.pos;

            // Get selected tag things
            List<Thing> things = BuilderPlug.Me.VertexSlopeAssistForm.GetThingsList();
            bool tagvalid = (things != null && things.Count == 3);
            PixelColor color = tagvalid ? General.Colors.Highlight : General.Colors.Selection;

			// Render lines
			if(renderer.StartPlotter(true))
			{
                renderer.PlotLinedefSet(General.Map.Map.Linedefs);
                renderer.PlotVerticesSet(General.Map.Map.Vertices);
				renderer.Finish();
			}

			// Render things
			if(renderer.StartThings(true))
			{
				renderer.RenderThingSet(General.Map.Map.Things, Presentation.THINGS_ALPHA);
				renderer.RenderNiGHTSPath();
				renderer.Finish();
			}

			// Render overlay
			if(renderer.StartOverlay(true))
			{
                if (mode == MODE_SELECT && things != null)
                {
                    if (things.Count >= 2)
                    {
                        for (int i = 1; i < things.Count; i++)
                            renderer.RenderLine(things[i - 1].Position, things[i].Position, LINE_THICKNESS, color, true);
                        renderer.RenderLine(things[things.Count - 1].Position, things[0].Position, LINE_THICKNESS, color, true);
                        if (tagvalid)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                Thing t = things[i];
                                TextLabel tl = vertexnames[i];
                                tl.Rectangle = new RectangleF(things[i].Position.x, things[i].Position.y, 0f, 0f);
                                tl.Text = (i + 1).ToString();
                                renderer.RenderRectangleFilled(new RectangleF(t.Position.x - vsize, t.Position.y - vsize, vsize * 2.0f, vsize * 2.0f), color, true);
                                renderer.RenderText(tl);
                            }
                        }
                    }
                    else if (things.Count == 1)
                    {
                        Thing t = things[0];
                        TextLabel tl = vertexnames[0];
                        tl.Rectangle = new RectangleF(things[0].Position.x, things[0].Position.y, 0f, 0f);
                        tl.Text = "1";
                        renderer.RenderRectangleFilled(new RectangleF(t.Position.x - vsize, t.Position.y - vsize, vsize * 2.0f, vsize * 2.0f), color, true);
                        renderer.RenderText(tl);
                    }
                }

                if (mode == MODE_ASSIGNGROUPS)
                {
                    for (int i = 1; i < points.Count; i++)
                        renderer.RenderLine(points[i - 1].pos, points[i].pos, LINE_THICKNESS, color, true);
                    if (points.Count > 0) {
                        if (things != null && things.Count == 1)
                        {
                            renderer.RenderLine(points[points.Count - 1].pos, things[0].Position, LINE_THICKNESS, color, true);
                            renderer.RenderLine(things[0].Position, points[0].pos, LINE_THICKNESS, color, true);
                            renderer.RenderRectangleFilled(new RectangleF(things[0].Position.x - vsize, things[0].Position.y - vsize, vsize * 4.0f, vsize * 4.0f), color, true);
                        }
                    }
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (i >= 3) break;
                        Vector2D v = points[i].pos;
                        TextLabel tl = vertexnames[i];
                        tl.Rectangle = new RectangleF(points[i].pos.x, points[i].pos.y, 0f, 0f);
                        tl.Text = (i + 1).ToString();
                        renderer.RenderRectangleFilled(new RectangleF(v.x - vsize, v.y - vsize, vsize * 2.0f, vsize * 2.0f), color, true);
                        renderer.RenderText(tl);
                    }
                }

                if (mode >= MODE_NEWPOINTS)
                {
                    for (int i = 1; i < points.Count; i++)
                        renderer.RenderLine(points[i - 1].pos, points[i].pos, LINE_THICKNESS, color, true);
                    if (points.Count > 0)
                    {
                        renderer.RenderLine(points[points.Count - 1].pos, curmousepos, LINE_THICKNESS, color, true);
                        renderer.RenderLine(curmousepos, points[0].pos, LINE_THICKNESS, color, true);
                    }
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (i >= 3) break;
                        Vector2D v = points[i].pos;
                        TextLabel tl = vertexnames[i];
                        tl.Rectangle = new RectangleF(points[i].pos.x, points[i].pos.y, 0f, 0f);
                        tl.Text = (i + 1).ToString();
                        renderer.RenderRectangleFilled(new RectangleF(v.x - vsize, v.y - vsize, vsize * 2.0f, vsize * 2.0f), color, true);
                        renderer.RenderText(tl);
                    }
                    renderer.RenderRectangleFilled(new RectangleF(curmousepos.x - vsize, curmousepos.y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.InfoLine, true);
                }
                else
                {
                    foreach (int tag in overgroups)
                    {
                        PixelColor overcolor = General.Colors.Indication.WithAlpha(128);
                        List<Thing> overthings = BuilderPlug.Me.VertexSlopeAssistForm.GetThingsList(tag);
                        if (overthings.Count >= 2)
                        {
                            for (int i = 1; i < overthings.Count; i++)
                                renderer.RenderLine(overthings[i - 1].Position, overthings[i].Position, OVER_THICKNESS, overcolor, true);
                            renderer.RenderLine(overthings[overthings.Count - 1].Position, overthings[0].Position, OVER_THICKNESS, overcolor, true);
                        }
                        else
                        {
                            renderer.RenderThing(overthings[0], overcolor, Presentation.THINGS_ALPHA);
                            renderer.RenderRectangleFilled(new RectangleF(overthings[0].Position.x - vsize, overthings[0].Position.y - vsize, vsize * 2.0f, vsize * 2.0f), overcolor, true);
                        }
                    }
                }

                if (selectedLinedef != null)
                    renderer.RenderLine(selectedLinedef.Start.Position, selectedLinedef.End.Position, LINE_THICKNESS, General.Colors.Linedefs, true);

                if (indexedLinedef != null)
                    renderer.RenderLine(indexedLinedef.Start.Position, indexedLinedef.End.Position, LINE_THICKNESS, General.Colors.Linedefs, true);

				renderer.Finish();
			}

			renderer.Present();
		}
		
		#endregion

        #region ================== Events (Mouse & Keyboard)

        // Mouse moving
        public override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (panning) return; //mxd. Skip all this jazz while panning

            if (mode < MODE_NEWPOINTS)
            {
                overgroups.Clear();
                int newOverTagID = -1;
                if (BuilderPlug.Me.VertexSlopeAssistForm.GetGroupsAt(mousemappos, ref overgroups, true, true) > 0)
                {
                    newOverTagID = overgroups[overgroupoffset % overgroups.Count];
                }
                if (newOverTagID != overTagID)
                {
                    overTagID = newOverTagID;
                    UpdateModeText();
                }
            }

            General.Interface.RedrawDisplay();
        }

        // Mouse press
        public override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (panning) return;

            if (mode == MODE_SELECT)
            {
                if (e.Button == MouseButtons.Left)
                {
                    overgroups.Clear();
                    if (BuilderPlug.Me.VertexSlopeAssistForm.GetGroupsAt(mousemappos, ref overgroups, true, true) > 0)
                    {
                        BuilderPlug.Me.VertexSlopeAssistForm.TagID = overgroups[overgroupoffset % overgroups.Count];
                        overgroupoffset++;
                    }
                }
                if (e.Button == MouseButtons.Right)
                {
                    int vertices = BuilderPlug.Me.VertexSlopeAssistForm.VerticesInTag();
                    Linedef l = General.Map.Map.NearestLinedefRange(mousemappos, BuilderPlug.Me.HighlightRange / renderer.Scale);
                    selectedLinedef = l;
                    if (l != null)
                        BuilderPlug.Me.VertexSlopeAssistForm.OpenContextLinedefMenu(Cursor.Position.X, Cursor.Position.Y, vertices == 1 || vertices == 3);
                    else
                        BuilderPlug.Me.VertexSlopeAssistForm.OpenContextGroupMenu(Cursor.Position.X, Cursor.Position.Y, vertices > 0);
                    General.Interface.RedrawDisplay();
                }
                UpdateModeText();
            }
            else if (mode == MODE_ASSIGNGROUPS)
            {
                if (e.Button == MouseButtons.Left)
                {
                    overgroups.Clear();
                    if (BuilderPlug.Me.VertexSlopeAssistForm.GetGroupsAt(mousemappos, ref overgroups, true, false) > 0)
                    {
                        BuilderPlug.Me.VertexSlopeAssistForm.TagID = overgroups[overgroupoffset % overgroups.Count];
                        overgroupoffset++;
                    }
                }
                if (e.Button == MouseButtons.Right)
                {
                    SetMode(MODE_SELECT);
                }
            }
            else
            {
                if (e.Button == MouseButtons.Left)
                {
                    DrawnVertex dv = new DrawnVertex();
                    dv.stitch = true;
                    dv.stitchline = true;
                    dv.pos = curmousepos;
                    points.Add(dv);
                    if (points.Count == expectedPoints) ApplyVSlopeChanges();
                    General.Interface.RedrawDisplay();
                    UpdateModeText();
                }
                if (e.Button == MouseButtons.Right)
                {
                    SetMode(MODE_SELECT);
                }
            }
        }

        // When a key is released
        public override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if ((snaptogrid != (General.Interface.ShiftState ^ General.Interface.SnapToGrid)) ||
               (snaptonearest != (General.Interface.CtrlState ^ General.Interface.AutoMerge)) ||
               (snaptocardinaldirection != (General.Interface.AltState && General.Interface.ShiftState))) General.Interface.RedrawDisplay();
        }

        // When a key is pressed
        public override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if ((snaptogrid != (General.Interface.ShiftState ^ General.Interface.SnapToGrid)) ||
               (snaptonearest != (General.Interface.CtrlState ^ General.Interface.AutoMerge)) ||
               (snaptocardinaldirection != (General.Interface.AltState && General.Interface.ShiftState))) General.Interface.RedrawDisplay();
        }

        #endregion

        #region ================== Actions

        // Remove a point
        [BeginAction("removepoint")]
        public void RemovePoint()
        {
            if (points.Count > 0) points.RemoveAt(points.Count - 1);
            if (indexedTags.Count > 0) indexedTags.RemoveAt(indexedTags.Count - 1);
            UpdateModeText();
            General.Interface.RedrawDisplay();
        }

        // Increase tag number
        [BeginAction("increasebevel")]
        public void IncreaseBevel()
        {
            if (BuilderPlug.Me.VertexSlopeAssistForm.TagID < General.Map.FormatInterface.MaxTag)
                BuilderPlug.Me.VertexSlopeAssistForm.TagID++;
        }
        [BeginAction("increasesubdivlevel")]
        public void IncreaseSubdivLevel()
        {
            IncreaseBevel();
        }

        // Decrease tag number
        [BeginAction("decreasebevel")]
        public void DecreaseBevel()
        {
            if (BuilderPlug.Me.VertexSlopeAssistForm.TagID > 0)
                BuilderPlug.Me.VertexSlopeAssistForm.TagID--;
        }
        [BeginAction("decreasesubdivlevel")]
        public void DecreaseSubdivLevel()
        {
            DecreaseBevel();
        }

        #endregion

    }
}
