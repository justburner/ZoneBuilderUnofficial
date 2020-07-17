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

#endregion

//JBR Parallel Linedef mode
namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Parallel Linedef",
			  AllowCopyPaste = false,
			  Volatile = true)]
	public sealed class ParallelLinedefMode : BaseClassicMode
	{
		#region ================== Constants

		private const float LINE_THICKNESS = 0.6f;
        private const int LINKED_LINEDEFS = 0;
        private const int UNLINKED_LINEDEFS = 1;
        private const int LINEDEFS = 1;
        private const int VERTICES = 2;
        private const int THINGS = 3;

		#endregion

		#region ================== Variables

        private EditMode basemode;
        private ICollection<Linedef> selected;
        private TracksTracer trackstracer;
        private bool clearlinedefs;

		#endregion

		#region ================== Properties

		// Just keep the base mode button checked
		public override string EditModeButtonName { get { return General.Editing.PreviousStableMode.Name; } }

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public ParallelLinedefMode(EditMode basemode)
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

        public List<Linedef> GetSelectedLinedefFromVertex(Vertex v)
        {
            List<Linedef> list = new List<Linedef>();
            foreach (Linedef ld in v.Linedefs)
            {
                if (ld.Selected) list.Add(ld);
            }
            return list;
        }

        #endregion

        #region ================== Events

        public override void OnHelp()
		{
			General.ShowHelp("e_parallellinedef.html");
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

            // Create the tracks tracer
            trackstracer = new TracksTracer(selected);

            // Check if tracing was valid
            if (!trackstracer.IsValid)
            {
                DialogResult dr = MessageBox.Show("Detected intersections while tracing!\nMake sure you're selecting vertices with maximum of 2 linedefs connected to them.\nResults may not be desirable, continue anyway?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr != DialogResult.Yes)
                {
                    OnCancel();
                    return;
                }
            }
            BuilderPlug.Me.ParallelLinedefForm.SetNumOpenPaths(trackstracer.OpenPaths.Count);
            BuilderPlug.Me.ParallelLinedefForm.SetNumClosePaths(trackstracer.ClosePaths.Count);

			// Show toolbox window
			BuilderPlug.Me.ParallelLinedefForm.Show((Form)General.Interface);
		}

		// Disenagaging
		public override void OnDisengage()
		{
			base.OnDisengage();

			// Hide toolbox window
			BuilderPlug.Me.ParallelLinedefForm.Hide();
		}

		// This applies the curves and returns to the base mode
		public override void OnAccept()
		{
			// Create undo
			General.Map.UndoRedo.CreateUndo("Parallel (Linedef)");

            int createas = BuilderPlug.Me.ParallelLinedefForm.CreateAs;
            float distance = BuilderPlug.Me.ParallelLinedefForm.Distance;
            bool closeopenpath = BuilderPlug.Me.ParallelLinedefForm.CloseOpenPath;
            bool backwards = BuilderPlug.Me.ParallelLinedefForm.Backwards;
            ThingBrowser2Form.Result thingresult = new ThingBrowser2Form.Result();

            // Ask for thing type...
            if (createas == THINGS)
            {
                BuilderPlug.Me.ParallelLinedefForm.Hide();

                // Pick the thing to add...
                thingresult = ThingBrowser2Form.BrowseThing(BuilderPlug.Me.ParallelLinedefForm);
                if (!thingresult.OK)
                {
                    General.Map.UndoRedo.WithdrawUndo();
                    General.Map.UndoRedo.WithdrawUndo();
                    General.Interface.DisplayStatus(StatusType.Warning, "Operation aborted.");
                    return;
                }

                BuilderPlug.Me.ParallelLinedefForm.Show();
                BuilderPlug.Me.ParallelLinedefForm.Activate();
            }

			// Go for all selected lines
            foreach (List<Vector2D> path in trackstracer.OpenPaths)
            {
                List<Vector2D> parallelpath = TracksTracer.ParallelizeOpenPath(path, distance, backwards);
                if (createas == LINKED_LINEDEFS)
                {
                    List<DrawnVertex> dvl = new List<DrawnVertex>();
                    if (closeopenpath) TracksTracer.AddPointToDrawnVertex(dvl, path[0], true, true);
                    TracksTracer.AddPathToDrawnVertex(dvl, parallelpath, true, true);
                    if (closeopenpath) TracksTracer.AddPointToDrawnVertex(dvl, path[path.Count - 1], true, true);
                    if (!Tools.DrawLines(dvl, true, BuilderPlug.Me.AutoAlignTextureOffsetsOnCreate))
                    {
                        // Drawing failed
                        // NOTE: I have to call this twice, because the first time only cancels this volatile mode
                        General.Map.UndoRedo.WithdrawUndo();
                        General.Map.UndoRedo.WithdrawUndo();
                        return;
                    }
                }
                else if (createas == UNLINKED_LINEDEFS)
                {
                    // Unlinked linedef
                    Vertex oldv = General.Map.Map.CreateVertex(parallelpath[0]);
                    for (int i = 1; i < parallelpath.Count; i++)
                    {
                        Vertex newv = General.Map.Map.CreateVertex(parallelpath[i]);
                        General.Map.Map.CreateLinedef(oldv, newv);
                        oldv = newv;
                    }
                    if (closeopenpath)
                    {
                        Vertex vtf = General.Map.Map.CreateVertex(path[0]);
                        Vertex vtl = General.Map.Map.CreateVertex(path[path.Count - 1]);
                        Vertex vpf = General.Map.Map.CreateVertex(parallelpath[0]);
                        Vertex vpl = General.Map.Map.CreateVertex(parallelpath[parallelpath.Count - 1]);
                        General.Map.Map.CreateLinedef(vtf, vpf);
                        General.Map.Map.CreateLinedef(vpl, vtl);
                    }
                }
                else if (createas == VERTICES)
                {
                    // Vertex
                    foreach (Vector2D pos in parallelpath)
                    {
                        General.Map.Map.CreateVertex(pos);
                    }
                }
                else if (createas == THINGS)
                {
                    foreach (Vector2D pos in parallelpath)
                    {
                        Thing tp = General.Map.Map.CreateThing();
                        if (tp != null)
                        {
                            General.Settings.ApplyDefaultThingSettings(tp);
                            thingresult.Apply(tp);
                            tp.Move(pos);
                            tp.UpdateConfiguration();
                        }
                    }
                }
            }
            foreach (List<Vector2D> path in trackstracer.ClosePaths)
            {
                List<Vector2D> parallelpath = TracksTracer.ParallelizeClosePath(path, distance, backwards);
                if (createas == LINKED_LINEDEFS)
                {
                    List<DrawnVertex> dvl = new List<DrawnVertex>();
                    TracksTracer.AddPathToDrawnVertex(dvl, parallelpath, true, true);
                    if (!Tools.DrawLines(dvl, true, BuilderPlug.Me.AutoAlignTextureOffsetsOnCreate))
                    {
                        // Drawing failed
                        // NOTE: I have to call this twice, because the first time only cancels this volatile mode
                        General.Map.UndoRedo.WithdrawUndo();
                        General.Map.UndoRedo.WithdrawUndo();
                        return;
                    }
                }
                else if (createas == UNLINKED_LINEDEFS)
                {
                    // Unlinked linedef
                    Vertex oldv = General.Map.Map.CreateVertex(parallelpath[0]);
                    for (int i = 1; i < parallelpath.Count; i++)
                    {
                        Vertex newv = General.Map.Map.CreateVertex(parallelpath[i]);
                        General.Map.Map.CreateLinedef(oldv, newv);
                        oldv = newv;
                    }
                }
                else if (createas == VERTICES)
                {
                    // Vertex
                    for (int i = 0; i < parallelpath.Count - 1; i++)
                    {
                        General.Map.Map.CreateVertex(parallelpath[i]);
                    }
                }
                else if (createas == THINGS)
                {
                    // Thing
                    for (int i = 0; i < parallelpath.Count - 1; i++)
                    {
                        Thing tp = General.Map.Map.CreateThing();
                        if (tp != null)
                        {
                            General.Settings.ApplyDefaultThingSettings(tp);
                            thingresult.Apply(tp);
                            tp.Move(parallelpath[i]);
                            tp.UpdateConfiguration();
                        }
                    }
                }
            }

            // Announce how many we created
            int num = trackstracer.OpenPaths.Count + trackstracer.ClosePaths.Count;
            if (num == 0)
            {
                General.Interface.DisplayStatus(StatusType.Warning, "No parallel tracks created.");
                General.Map.UndoRedo.WithdrawUndo();
                General.Map.UndoRedo.WithdrawUndo();
                return;
            }
            if (num > 1)
                General.Interface.DisplayStatus(StatusType.Action, "Created " + num.ToString() + " parallel tracks.");
            else
                General.Interface.DisplayStatus(StatusType.Action, "Created a parallel track.");

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

            int createas = BuilderPlug.Me.ParallelLinedefForm.CreateAs;
            float distance = BuilderPlug.Me.ParallelLinedefForm.Distance;
            bool closeopenpath = BuilderPlug.Me.ParallelLinedefForm.CloseOpenPath;
            bool backwards = BuilderPlug.Me.ParallelLinedefForm.Backwards;

			// Render overlay
            bool renderlines = (createas <= LINEDEFS);
			if(renderer.StartOverlay(true))
			{
                foreach (List<Vector2D> path in trackstracer.OpenPaths)
                {
                    // Selected path in info color
                    for (int i = 1; i < path.Count; i++)
                        renderer.RenderLine(path[i - 1], path[i], LINE_THICKNESS, General.Colors.InfoLine, true);
                    renderer.RenderRectangleFilled(new RectangleF(path[0].x - vsize, path[0].y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.InfoLine, true);
                    renderer.RenderRectangleFilled(new RectangleF(path[path.Count - 1].x - vsize, path[path.Count - 1].y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.InfoLine, true);

                    // Parallel path
                    List<Vector2D> parallelpath = TracksTracer.ParallelizeOpenPath(path, distance, backwards);
                    renderer.RenderRectangleFilled(new RectangleF(parallelpath[0].x - vsize, parallelpath[0].y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.Highlight, true);
                    for (int i = 1; i < parallelpath.Count; i++)
                    {
                        if (renderlines) renderer.RenderLine(parallelpath[i - 1], parallelpath[i], LINE_THICKNESS, General.Colors.Highlight, true);
                        renderer.RenderRectangleFilled(new RectangleF(parallelpath[i].x - vsize, parallelpath[i].y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.Highlight, true);
                    }
                    if (closeopenpath && renderlines)
                    {
                        renderer.RenderLine(path[0], parallelpath[0], LINE_THICKNESS, General.Colors.Highlight, true);
                        renderer.RenderLine(path[path.Count - 1], parallelpath[parallelpath.Count - 1], LINE_THICKNESS, General.Colors.Highlight, true);
                    }
                }
                foreach (List<Vector2D> path in trackstracer.ClosePaths)
                {
                    // Selected path in info color
                    for (int i = 1; i < path.Count; i++)
                        renderer.RenderLine(path[i - 1], path[i], LINE_THICKNESS, General.Colors.InfoLine, true);
                    renderer.RenderRectangleFilled(new RectangleF(path[0].x - vsize, path[0].y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.InfoLine, true);
                    renderer.RenderRectangleFilled(new RectangleF(path[path.Count - 1].x - vsize, path[path.Count - 1].y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.InfoLine, true);

                    // Parallel path
                    List<Vector2D> parallelpath = TracksTracer.ParallelizeClosePath(path, distance, backwards);
                    renderer.RenderRectangleFilled(new RectangleF(parallelpath[0].x - vsize, parallelpath[0].y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.Highlight, true);
                    for (int i = 1; i < parallelpath.Count; i++)
                    {
                        if (renderlines) renderer.RenderLine(parallelpath[i - 1], parallelpath[i], LINE_THICKNESS, General.Colors.Highlight, true);
                        renderer.RenderRectangleFilled(new RectangleF(parallelpath[i].x - vsize, parallelpath[i].y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.Highlight, true);
                    }
                }
                renderer.Finish();
			}

			renderer.Present();
		}
		
		#endregion
	}
}
