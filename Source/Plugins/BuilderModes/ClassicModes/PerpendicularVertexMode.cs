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

//JBR Perpendicular Vertex mode
namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Perpendicular Vertex",
			  AllowCopyPaste = false,
			  Volatile = true)]
	public sealed class PerpendicularVertexMode : BaseClassicMode
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
        private bool clearvertices;
        private bool clearlinedefs;

		// Collections
        private ICollection<Vertex> selected;
		
		#endregion

		#region ================== Properties

		// Just keep the base mode button checked
		public override string EditModeButtonName { get { return General.Editing.PreviousStableMode.Name; } }

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public PerpendicularVertexMode(EditMode basemode)
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
			General.ShowHelp("e_perpendicularvertex.html");
		}

		// Cancelled
		public override void OnCancel()
		{
			// Cancel base class
			base.OnCancel();

            // Clear selection that is not the current mode
            if (clearlinedefs) General.Map.Map.ClearSelectedLinedefs();
            if (clearvertices) General.Map.Map.ClearSelectedVertices();

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
                selected = General.Map.Map.GetSelectedVertices(true);
                foreach (Vertex v in selected)
                {
                    foreach (Linedef ld in v.Linedefs)
                    {
                        if (ld.Start.Selected && ld.End.Selected) ld.Selected = true;
                    }
                }
                clearvertices = false;
                clearlinedefs = true;
            }
            if (basemode is LinedefsMode)
            {
                selected = new List<Vertex>();
                ICollection<Linedef> selectedlines = General.Map.Map.GetSelectedLinedefs(true);
                foreach (Linedef ld in selectedlines)
                {
                    ld.Start.Selected = true;
                    ld.End.Selected = true;
                    if (!selected.Contains(ld.Start)) selected.Add(ld.Start);
                    if (!selected.Contains(ld.End)) selected.Add(ld.End);
                }
                clearvertices = true;
                clearlinedefs = false;
            }
            if (basemode is SectorsMode)
            {
                selected = new List<Vertex>();
                ICollection<Sector> selectedsectors = General.Map.Map.GetSelectedSectors(true);
                foreach (Sector sc in selectedsectors)
                {
                    foreach (Sidedef sd in sc.Sidedefs)
                    {
                        sd.Line.Start.Selected = true;
                        sd.Line.End.Selected = true;
                        if (!selected.Contains(sd.Line.Start)) selected.Add(sd.Line.Start);
                        if (!selected.Contains(sd.Line.End)) selected.Add(sd.Line.End);
                        sd.Line.Selected = true;
                    }
                }
                clearvertices = true;
                clearlinedefs = true;
            }

            // Show toolbox window
            BuilderPlug.Me.PerpendicularVertexForm.Show((Form)General.Interface);
        }

		// Disenagaging
		public override void OnDisengage()
		{
			base.OnDisengage();

			// Hide toolbox window
			BuilderPlug.Me.PerpendicularVertexForm.Hide();
		}

		// This applies the curves and returns to the base mode
		public override void OnAccept()
		{
			// Create undo
			General.Map.UndoRedo.CreateUndo("Perpendicular (Vertex)");

            int createas = BuilderPlug.Me.PerpendicularVertexForm.CreateAs;
            float distance = BuilderPlug.Me.PerpendicularVertexForm.Distance;
            bool processtips = BuilderPlug.Me.PerpendicularVertexForm.ProcessTips;
            bool backwards = BuilderPlug.Me.PerpendicularVertexForm.Backwards;
            ThingBrowser2Form.Result thingresult = new ThingBrowser2Form.Result();
            float snapmp = BuilderPlug.Me.PerpendicularVertexForm.SnapMP;

            // Ask for thing type...
            if (createas == THINGS)
            {
                BuilderPlug.Me.PerpendicularVertexForm.Hide();

                // Pick the thing to add...
                thingresult = ThingBrowser2Form.BrowseThing(BuilderPlug.Me.PerpendicularVertexForm);
                if (!thingresult.OK)
                {
                    General.Map.UndoRedo.WithdrawUndo();
                    General.Map.UndoRedo.WithdrawUndo();
                    General.Interface.DisplayStatus(StatusType.Warning, "Operation aborted.");
                    return;
                }
                /*
                ThingBrowserForm f = new ThingBrowserForm(thingtype);
                if (f.ShowDialog(BuilderPlug.Me.PerpendicularVertexForm) != DialogResult.OK)
                {
                    f.Dispose();
                    General.Map.UndoRedo.WithdrawUndo();
                    General.Map.UndoRedo.WithdrawUndo();
                    General.Interface.DisplayStatus(StatusType.Warning, "Operation aborted.");
                    return;
                }
                thingtype = f.SelectedType;
                f.Dispose();
                */

                BuilderPlug.Me.PerpendicularVertexForm.Show();
                BuilderPlug.Me.PerpendicularVertexForm.Activate();
            }

			// Go for all selected lines
            int numcreated = 0;
            foreach (Vertex v in selected)
            {
                Vector2D centerpos = v.Position;
                Vector2D deltapos;

                // List selected linedefs that belong to the vertex
                List<Linedef> linedefs = new List<Linedef>();
                foreach (Linedef ld in v.Linedefs)
                {
                    if (ld.Selected && !linedefs.Contains(ld)) linedefs.Add(ld);
                }

                if (processtips && linedefs.Count == 1) // If the vertex only has 1 linedef attached, find the other end and create a perpendicular with it
                {
                    Linedef ld1 = linedefs[0];
                    Vertex v1 = (ld1.Start == v) ? ld1.End : ld1.Start;
                    Vertex v2 = v;
                    if (ld1.Start == v)
                    {
                        // Flip vertex
                        Vertex tmp = v1;
                        v1 = v2;
                        v2 = tmp;
                    }
                    deltapos = (v1.Position - centerpos).GetNormal() - (v2.Position - centerpos).GetNormal();
                }
                else if (linedefs.Count == 2) // If the vertex only has 2 linedefs attached, find both opposite ends then create a perpendicular with them
                {
                    Linedef ld1 = linedefs[0];
                    Linedef ld2 = linedefs[1];
                    Vertex v1 = (ld1.Start == v) ? ld1.End : ld1.Start;
                    Vertex v2 = (ld2.Start == v) ? ld2.End : ld2.Start;
                    if (ld1.Start == v && ld2.End == v)
                    {
                        // Flip vertex
                        Vertex tmp = v1;
                        v1 = v2;
                        v2 = tmp;
                    }
                    else if (!(ld2.Start == v && ld1.End == v))
                    {
                        // Linedefs are facing opposite directions
                        continue;
                    }
                    deltapos = (v1.Position - centerpos).GetNormal() - (v2.Position - centerpos).GetNormal();
                }
                else // If the vertex has more than 2 linedefs attached then is no good!
                {
                    continue;
                }
                if (backwards) deltapos = -deltapos;
                Vector2D normalunit = deltapos.GetPerpendicular().GetNormal();
                Vector2D perppos = centerpos + normalunit * distance;

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
                    Vertex centerv = General.Map.Map.CreateVertex(centerpos);
                    Vertex perpv = General.Map.Map.CreateVertex(perppos);
                    General.Map.Map.CreateLinedef(centerv, perpv);
                }
                else if (createas == VERTICES)
                {
                    // Vertex
                    Vertex perpv = General.Map.Map.CreateVertex(perppos);
                    if (snapmp > 0f)
                        JoinIntoNearMapVertex(perpv, snapmp);
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
                        //tp.SRB2Type = thingtype;
                        //tp.Parameter = 0;
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

            int createas = BuilderPlug.Me.PerpendicularVertexForm.CreateAs;
            float distance = BuilderPlug.Me.PerpendicularVertexForm.Distance;
            bool processtips = BuilderPlug.Me.PerpendicularVertexForm.ProcessTips;
            bool backwards = BuilderPlug.Me.PerpendicularVertexForm.Backwards;

			// Render overlay
			if(renderer.StartOverlay(true))
			{
				// Go for all selected lines
				foreach (Vertex v in selected)
				{
                    Vector2D centerpos = v.Position;
                    Vector2D deltapos;

                    // List selected linedefs that belong to the vertex
                    List<Linedef> linedefs = new List<Linedef>();
                    foreach (Linedef ld in v.Linedefs)
                    {
                        if (ld.Selected && !linedefs.Contains(ld)) linedefs.Add(ld);
                    }

                    if (processtips && linedefs.Count == 1) // If the vertex only has 1 linedef, find the other end and create a perpendicular with it
                    {
                        Linedef ld1 = linedefs[0];
                        Vertex v1 = (ld1.Start == v) ? ld1.End : ld1.Start;
                        Vertex v2 = v;
                        if (ld1.Start == v)
                        {
                            // Flip vertex
                            Vertex tmp = v1;
                            v1 = v2;
                            v2 = tmp;
                        }
                        deltapos = (v1.Position - centerpos).GetNormal() - (v2.Position - centerpos).GetNormal();
                    }
                    else if (linedefs.Count == 2) // If the vertex only has 2 linedefs, find both opposite ends then create a perpendicular with them
                    {
                        Linedef ld1 = linedefs[0]; // General.GetByIndex(v.Linedefs, 0);
                        Linedef ld2 = linedefs[1]; // General.GetByIndex(v.Linedefs, 1);
                        Vertex v1 = (ld1.Start == v) ? ld1.End : ld1.Start;
                        Vertex v2 = (ld2.Start == v) ? ld2.End : ld2.Start;
                        if (ld1.Start == v && ld2.End == v)
                        {
                            // Flip vertex
                            Vertex tmp = v1;
                            v1 = v2;
                            v2 = tmp;
                        }
                        else if (!(ld2.Start == v && ld1.End == v))
                        {
                            // Linedefs are facing opposite directions
                            continue;
                        }
                        deltapos = (v1.Position - centerpos).GetNormal() - (v2.Position - centerpos).GetNormal();
                    }
                    else // If the vertex has more than 2 linedefs then is no good!
                    {
                        continue;
                    }
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
                }
				renderer.Finish();
			}

			renderer.Present();
		}
		
		#endregion
	}
}
