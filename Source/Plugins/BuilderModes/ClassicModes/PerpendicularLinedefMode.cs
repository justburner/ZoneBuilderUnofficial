#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Editing;
using System.Drawing;
using CodeImp.DoomBuilder.Windows;

#endregion

//JBR Perpendicular Linedef mode
namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Perpendicular Linedef",
			  AllowCopyPaste = false,
			  Volatile = true)]
	public sealed class PerpendicularLinedefMode : BaseClassicMode
	{
		#region ================== Constants

		private const float LINE_THICKNESS = 0.6f;
        private const int LINKED_LINEDEFS = 0;
        private const int UNLINKED_LINEDEFS = 1;
        private const int LINEDEFS = 1;
        private const int VERTICES_PERPENDICULAR = 2;
        private const int VERTICES_BOTH = 3;
        private const int THINGS = 4;

		#endregion

		#region ================== Variables

        private EditMode basemode;
		private ICollection<Linedef> selected;
        private bool clearlinedefs;
		
		#endregion

		#region ================== Properties

		// Just keep the base mode button checked
		public override string EditModeButtonName { get { return General.Editing.PreviousStableMode.Name; } }

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public PerpendicularLinedefMode(EditMode basemode)
		{
            this.basemode = basemode;
        }
		
		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Clean up

				// Done
				base.Dispose();
			}
		}

		#endregion

        #region ================== Methods

        /// Join into a near vertex on map
        private Vector2D JoinIntoNearMapVertex(Vector2D pos, float epsilon)
        {
            foreach (Vertex va in General.Map.Map.Vertices)
            {
                if (va.IsDisposed) continue;
                if (pos.CloseTo(va.Position, epsilon))
                {
                    return va.Position;
                }
            }
            return pos;
        }

        // Join into a near vertex on map
        private bool JoinIntoNearMapVertex(Vertex v, float epsilon)
        {
            foreach (Vertex va in General.Map.Map.Vertices)
            {
                if (va == v || va.IsDisposed) continue;
                if (v.Position.CloseTo(va.Position, epsilon))
                {
                    v.Join(va);
                    return true;
                }
            }
            return false;
        }

        #endregion

		#region ================== Events

		public override void OnHelp()
		{
			General.ShowHelp("e_perpendicularlinedef.html");
		}

		// Cancelled
		public override void OnCancel()
		{
			// Cancel base class
			base.OnCancel();

            // Clear selection that is not the current mode
            if (clearlinedefs) General.Map.Map.ClearSelectedLinedefs();

			// Return to base mode
			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// Mode engages
		public override void OnEngage()
		{
			base.OnEngage();
			renderer.SetPresentation(Presentation.Standard);

            // Make collections by selection
            if (basemode is VerticesMode)
            {
                selected = new List<Linedef>();
                ICollection<Vertex> selectedvertices = General.Map.Map.GetSelectedVertices(true);
                foreach (Vertex v in selectedvertices)
                {
                    foreach (Linedef ld in v.Linedefs)
                    {
                        if (ld.Start.Selected && ld.End.Selected)
                        {
                            ld.Selected = true;
                            if (!selected.Contains(ld)) selected.Add(ld);
                        }
                    }
                }
                clearlinedefs = true;
            }
            if (basemode is LinedefsMode)
            {
                selected = General.Map.Map.GetSelectedLinedefs(true);
            }
            if (basemode is SectorsMode)
            {
                selected = new List<Linedef>();
                ICollection<Sector> selectedsectors = General.Map.Map.GetSelectedSectors(true);
                foreach (Sector sc in selectedsectors)
                {
                    foreach (Sidedef sd in sc.Sidedefs)
                    {
                        sd.Line.Selected = true;
                        if (!selected.Contains(sd.Line)) selected.Add(sd.Line);
                    }
                }
                clearlinedefs = true;
            }

			// Show toolbox window
			BuilderPlug.Me.PerpendicularLinedefForm.Show((Form)General.Interface);
		}

		// Disenagaging
		public override void OnDisengage()
		{
			base.OnDisengage();

			// Hide toolbox window
			BuilderPlug.Me.PerpendicularLinedefForm.Hide();
		}

		// This applies the curves and returns to the base mode
		public override void OnAccept()
		{
			// Create undo
			General.Map.UndoRedo.CreateUndo("Perpendicular (Linedef)");

            int createas = BuilderPlug.Me.PerpendicularLinedefForm.CreateAs;
            float distance = BuilderPlug.Me.PerpendicularLinedefForm.Distance;
            float offset = BuilderPlug.Me.PerpendicularLinedefForm.OffsetPerc / 100.0f;
            bool backwards = BuilderPlug.Me.PerpendicularLinedefForm.Backwards;
            ThingBrowser2Form.Result thingresult = new ThingBrowser2Form.Result();
            float snapmp = BuilderPlug.Me.PerpendicularVertexForm.SnapMP;

            // Ask for thing type...
            if (createas == THINGS)
            {
                BuilderPlug.Me.PerpendicularLinedefForm.Hide();

                // Pick the thing to add...
                thingresult = ThingBrowser2Form.BrowseThing(BuilderPlug.Me.PerpendicularVertexForm);
                if (!thingresult.OK)
                {
                    General.Map.UndoRedo.WithdrawUndo();
                    General.Map.UndoRedo.WithdrawUndo();
                    General.Interface.DisplayStatus(StatusType.Warning, "Operation aborted.");
                    return;
                }

                BuilderPlug.Me.PerpendicularLinedefForm.Show();
                BuilderPlug.Me.PerpendicularLinedefForm.Activate();
            }

			// Go for all selected lines
            int numcreated = 0;
            foreach (Linedef ld in selected)
			{
                Vector2D centerpos = ld.Start.Position.LinearInto(ld.End.Position, offset);
                Vector2D deltapos = ld.Start.Position - ld.End.Position;
				if (backwards) deltapos = -deltapos;
				Vector2D normalunit = deltapos.GetPerpendicular().GetNormal();
                Vector2D perppos = centerpos + normalunit * distance;

                // Check if is near some other vertex on map
                //perppos = JoinIntoNearVertex(perppos, 0.1f);

                numcreated++;
                if (createas == LINKED_LINEDEFS)
                {
                    // Linked linedef between 2 vertices
                    List<DrawnVertex> dvl = new List<DrawnVertex>();
                    DrawnVertex dv = new DrawnVertex(); // struct
                    dv.pos = centerpos;
                    dv.stitch = true;
                    dv.stitchline = true;
                    dvl.Add(dv);
                    dv.pos = perppos;
                    dvl.Add(dv);
                    if (!Tools.DrawLines(dvl, true, BuilderPlug.Me.AutoAlignTextureOffsetsOnCreate))
                    {
                        // Drawing failed
                        // NOTE: I have to call this twice, because the first time only cancels this volatile mode
                        General.Map.UndoRedo.WithdrawUndo();
                        General.Map.UndoRedo.WithdrawUndo();
                        return;
                    }
                    if (snapmp > 0f)
                    {
                        // Manually join with vertices already on map since DrawLines fail to do so...
                        List<Vertex> marked = General.Map.Map.GetMarkedVertices(true);
                        foreach (Vertex vm in marked)
                            JoinIntoNearMapVertex(vm, snapmp);
                    }
                }
                else if (createas == UNLINKED_LINEDEFS)
                {
                    // Unlinked linedef
                    Vertex perpv = General.Map.Map.CreateVertex(perppos);
                    Vertex centerv = General.Map.Map.CreateVertex(centerpos);
                    General.Map.Map.CreateLinedef(centerv, perpv);
                }
                else if (createas == VERTICES_PERPENDICULAR)
                {
                    // Vertex
                    Vertex perpv = General.Map.Map.CreateVertex(perppos);
                    if (snapmp > 0f)
                        JoinIntoNearMapVertex(perpv, snapmp);
                }
                else if (createas == VERTICES_BOTH)
                {
                    Vertex perpv = General.Map.Map.CreateVertex(perppos);
                    if (snapmp > 0f)
                        JoinIntoNearMapVertex(perpv, snapmp);
                    if (offset > 0f && offset < 1f)
                    {
                        // Split linedef
                        Vertex centerv = General.Map.Map.CreateVertex(centerpos);
                        ld.Split(centerv);
                    }
                }
                else if (createas == THINGS)
                {
                    Thing tp = General.Map.Map.CreateThing();
                    if (tp != null)
                    {
                        // Check if is near some other vertex on map
                        if (snapmp > 0f)
                            perppos = JoinIntoNearMapVertex(perppos, snapmp);

                        General.Settings.ApplyDefaultThingSettings(tp);
                        thingresult.Apply(tp);
                        tp.Move(perppos);
                        tp.UpdateConfiguration();
                    }
                }
			}

            if (numcreated == 0)
            {
                // If nothing created then abort
                General.Map.UndoRedo.WithdrawUndo();
                General.Map.UndoRedo.WithdrawUndo();
                General.Interface.DisplayStatus(StatusType.Warning, "No perpendiculars created.");
                return;
            }

            // Announce how many we created
            General.Interface.DisplayStatus(StatusType.Action, "Created " + numcreated.ToString() + " perpendicular(s).");

            if (createas == THINGS)
            {
                //Update Things filter
                General.Map.ThingsFilter.Update();
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

            // Map is changed
            General.Map.IsChanged = true;

			// Return to base mode
			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// Redrawing display
		public override void OnRedrawDisplay()
		{
			renderer.RedrawSurface();
            float vsize = (renderer.VertexSize + 1.0f) / renderer.Scale;

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

            int createas = BuilderPlug.Me.PerpendicularLinedefForm.CreateAs;
            float distance = BuilderPlug.Me.PerpendicularLinedefForm.Distance;
            float offset = BuilderPlug.Me.PerpendicularLinedefForm.OffsetPerc / 100.0f;
			bool backwards = BuilderPlug.Me.PerpendicularLinedefForm.Backwards;

			// Render overlay
			if(renderer.StartOverlay(true))
			{
				// Go for all selected lines
				foreach (Linedef ld in selected)
				{
                    Vector2D centerpos = ld.Start.Position.LinearInto(ld.End.Position, offset);
                    Vector2D deltapos = ld.Start.Position - ld.End.Position;
					if (backwards) deltapos = -deltapos;
					Vector2D normalunit = deltapos.GetPerpendicular().GetNormal();
					Vector2D perppos = centerpos + normalunit * distance;
                    if (createas <= LINEDEFS)
                    {
                        renderer.RenderLine(centerpos, perppos, LINE_THICKNESS, General.Colors.Highlight, true);
                    }
                    else
                    {
                        renderer.RenderRectangleFilled(new RectangleF(perppos.x - vsize, perppos.y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.Highlight, true);
                    }
                    if (createas == LINKED_LINEDEFS || createas == VERTICES_BOTH)
                    {
                        renderer.RenderRectangleFilled(new RectangleF(centerpos.x - vsize, centerpos.y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.Highlight, true);
                    }
                }
				renderer.Finish();
			}

			renderer.Present();
		}
		
		#endregion
	}
}
