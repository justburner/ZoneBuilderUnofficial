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

//JBR Vertex into Shape mode
namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Vertex into Shape",
			  AllowCopyPaste = false,
			  Volatile = true)]
	public sealed class VertexIntoShapeMode : BaseClassicMode
	{
		#region ================== Constants

        private const float LINE_THICKNESS = 0.6f;
        private const int CLOSESHAPE_ORIGIN = 0;
        private const int CLOSESHAPE_VERTICES = 1;
        private const int SHAPE = 2;
        private const int LINEDEFS = 3;
        private const int VERTICES = 4;
        private const int THINGS = 5;

		#endregion

		#region ================== Variables

		// Collections
        private ICollection<Vertex> selected;
		
		#endregion

		#region ================== Properties

		// Just keep the base mode button checked
		public override string EditModeButtonName { get { return General.Editing.PreviousStableMode.Name; } }

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public VertexIntoShapeMode(EditMode basemode)
		{
			// Make collections by selection
            selected = General.Map.Map.GetSelectedVertices(true);
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

        // This generates the shape from a vertex
        private static List<Vector2D> GenerateShape(Vector2D origin, int sides, int spikiness, int spikingmode, float radiusX, float radiusY, float start, float end)
        {
            if (sides <= 0) return new List<Vector2D>();
            float spikeX = (float)spikiness / 100f * radiusX;
            float spikeY = (float)spikiness / 100f * radiusY;
            float spikeDef = (float)spikiness;

            // Make list
            List<Vector2D> points = new List<Vector2D>();

            // Spiking fun!
            int spiketype = spikingmode >> 2;
            int spikematch = spikingmode & 1;
            if ((spikingmode & 2) == 2)
            {
                spikeX = -spikeX;
                spikeY = -spikeY;
            }

            // FEATURE: In some spikingmodes, if spikiness is over 100% the spikes can cross between center for interesting results!

            // Plot each side
            for (int i = 0; i < sides + 1; i++)
            {
                float offX = 0f;
                float offY = 0f;
                if (spiketype == 0)
                {
                    // Spike outside and inside
                    if ((i & 1) == spikematch)
                    {
                        offX = spikeX;
                        offY = spikeY;
                    }
                }
                else if (spiketype == 1)
                {
                    // Spike zig-zag and gear
                    offX = spikeX;
                    offY = spikeY;
                    if ((spikematch == 0) || (i & 1) == spikematch)
                    {
                        spikeX = -spikeX;
                        spikeY = -spikeY;
                    }
                }
                else if (spiketype == 2)
                {
                    // Simulate the DrawEllipse spikiness
                    if ((i & 1) == spikematch)
                    {
                        offX = spikeDef;
                        offY = spikeDef;
                    }
                }
                float delta = (float)i / sides;
                float angle = start + (end - start) * delta;
                float x = (float)Math.Cos(angle) * (radiusX + offX);
                float y = (float)Math.Sin(angle) * (radiusY + offY);
                Vector2D vertex = new Vector2D(x, y);

                points.Add(origin + vertex);
            }

            // Done
            return points;
        }

        // Calculate origin and radius from 2 vectors
        private static void CalculateOrigin(bool boxed, bool ellipse, Vector2D start, Vector2D end, out Vector2D origin, out Vector2D endpoint, out float radiusX, out float radiusY)
        {
            origin = start;
            endpoint = end;
            if (boxed)
            {
                origin = (start + end) / 2f;
                float centerX = (end.x - start.x) / 2f;
                float centerY = (end.y - start.y) / 2f;
                radiusX = Math.Abs(centerX);
                radiusY = Math.Abs(centerY);
                if (!ellipse)
                {
                    if (radiusX > radiusY)
                    {
                        radiusY = radiusX;
                        centerY = (centerY >= 0f) ? Math.Abs(centerX) : -Math.Abs(centerX);
                    }
                    else
                    {
                        radiusX = radiusY;
                        centerX = (centerX >= 0f) ? Math.Abs(centerY) : -Math.Abs(centerY);
                    }
                    origin.x = start.x + centerX;
                    origin.y = start.y + centerY;
                }
            }
            else
            {
                if (ellipse)
                {
                    radiusX = Math.Abs(end.x - start.x);
                    radiusY = Math.Abs(end.y - start.y);
                }
                else
                {
                    float radius = (end - start).GetLength();
                    radiusX = radius;
                    radiusY = radius;
                }
            }
        }

        // This draws a point at a specific location
        private static bool AddPointAt(List<DrawnVertex> shapepts, Vector2D pos, bool stitch, bool stitchline)
        {
            if (pos.x < General.Map.Config.LeftBoundary || pos.x > General.Map.Config.RightBoundary ||
                pos.y > General.Map.Config.TopBoundary || pos.y < General.Map.Config.BottomBoundary)
                return false;

            DrawnVertex newpoint = new DrawnVertex();
            newpoint.pos = pos;
            newpoint.stitch = stitch;
            newpoint.stitchline = stitchline;
            shapepts.Add(newpoint);

            return true;
        }

        #endregion

		#region ================== Events

		public override void OnHelp()
		{
			General.ShowHelp("e_vertexintoshape.html");
		}

		// Cancelled
		public override void OnCancel()
		{
			// Cancel base class
			base.OnCancel();

			// Return to base mode
			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// Mode engages
		public override void OnEngage()
		{
			base.OnEngage();
			renderer.SetPresentation(Presentation.Standard);
			
			// Show toolbox window
			BuilderPlug.Me.VertexIntoShapeForm.Show((Form)General.Interface);
		}

		// Disenagaging
		public override void OnDisengage()
		{
			base.OnDisengage();

			// Hide toolbox window
            BuilderPlug.Me.VertexIntoShapeForm.Hide();
		}

		// This applies the curves and returns to the base mode
		public override void OnAccept()
		{
            BuilderPlug.Me.VertexIntoShapeForm.RestartSeed();
            bool previewreference = BuilderPlug.Me.VertexIntoShapeForm.PreviewReference;
            bool removevertices = BuilderPlug.Me.VertexIntoShapeForm.RemoveVertices;
            bool ellipse = BuilderPlug.Me.VertexIntoShapeForm.Ellipse;
            int createas = BuilderPlug.Me.VertexIntoShapeForm.CreateAs;
            bool frontoutside = BuilderPlug.Me.VertexIntoShapeForm.FrontOutside;
            int spikingmode = BuilderPlug.Me.VertexIntoShapeForm.SpikingMode;
            ThingBrowser2Form.Result thingresult = new ThingBrowser2Form.Result();

            // Ask for thing type...
            if (createas == THINGS)
            {
                BuilderPlug.Me.VertexIntoShapeForm.Hide();

                // Pick the thing to add...
                thingresult = ThingBrowser2Form.BrowseThing(BuilderPlug.Me.PerpendicularVertexForm);
                if (!thingresult.OK)
                {
                    General.Map.UndoRedo.WithdrawUndo();
                    General.Map.UndoRedo.WithdrawUndo();
                    General.Interface.DisplayStatus(StatusType.Warning, "Operation aborted.");
                    return;
                }

                BuilderPlug.Me.VertexIntoShapeForm.Show();
                BuilderPlug.Me.VertexIntoShapeForm.Activate();
            }

            // Create undo
            if (selected.Count == 1)
            {
                General.Map.UndoRedo.CreateUndo(String.Format("Vertex into shape"));
            }
            else
            {
                General.Map.UndoRedo.CreateUndo(String.Format("{0} Vertices into shapes", selected.Count));
            }

			// Go for all selected lines
            foreach (Vertex v in selected)
            {
                // This values can be random...
                float radiusX = BuilderPlug.Me.VertexIntoShapeForm.RadiusX;
                float radiusY = BuilderPlug.Me.VertexIntoShapeForm.RadiusY;
                int sides = BuilderPlug.Me.VertexIntoShapeForm.Sides;
                int spikiness = BuilderPlug.Me.VertexIntoShapeForm.Spikiness;
                float startangle = Angle2D.DegToRad(BuilderPlug.Me.VertexIntoShapeForm.StartAngle);
                float sweepangle = Angle2D.DegToRad(BuilderPlug.Me.VertexIntoShapeForm.SweepAngle);
                BuilderPlug.Me.VertexIntoShapeForm.NextShape();

                // Skip shapes that are smaller than 1 radius.
                if (radiusX < 1f || radiusY < 1f) continue;

                // Generate shape
                Vector2D origin = v.Position;
                List<Vector2D> shapevecs = GenerateShape(origin, sides, spikiness, spikingmode, radiusX, radiusY, startangle, startangle + sweepangle);

                // Delete last vertex if match with first
                bool wasClosedShape = shapevecs[0].CloseTo(shapevecs[shapevecs.Count - 1], 0.1f);
                if (wasClosedShape)
                {
                    shapevecs.RemoveAt(shapevecs.Count - 1);
                }

                // Flip normals by reversing the list
                if (!frontoutside) shapevecs.Reverse();

                // Make the drawing
                if (createas < LINEDEFS) 
                {
                    List<DrawnVertex> shapepts = new List<DrawnVertex>();
                    foreach (Vector2D t in shapevecs) AddPointAt(shapepts, t, true, true);
                    if (!wasClosedShape && createas == CLOSESHAPE_ORIGIN)
                    {
                        AddPointAt(shapepts, origin, true, true); // Go toward origin before closing
                    }
                    if (wasClosedShape || createas <= CLOSESHAPE_VERTICES)
                    {
                        AddPointAt(shapepts, shapevecs[0], true, true); // Close the shape
                    }
                    if (!Tools.DrawLines(shapepts, true, BuilderPlug.Me.AutoAlignTextureOffsetsOnCreate))
                    {
                        // Drawing failed
                        General.Map.UndoRedo.WithdrawUndo();
                        return;
                    }
                }
                else
                {
                    List<Vertex> vertices = new List<Vertex>();

                    // Create each point
                    if (createas != THINGS)
                    {
                        for (int i = 0; i < shapevecs.Count; i++)
                        {
                            Vertex vp = General.Map.Map.CreateVertex(shapevecs[i]);
                            if (vp == null)
                            {
                                General.Map.UndoRedo.WithdrawUndo();
                                return;
                            }
                            vertices.Add(vp);
                        }
                    }

                    // of for things create things....
                    if (createas == THINGS)
                    {
                        for (int i = 0; i < shapevecs.Count; i++)
                        {
                            Thing tp = General.Map.Map.CreateThing();
                            if (tp != null)
                            {
                                General.Settings.ApplyDefaultThingSettings(tp);
                                thingresult.Apply(tp);
                                tp.Move(shapevecs[i]);
                                tp.UpdateConfiguration();
                            }
                        }
                    }

                    // Create linedefs
                    if (createas == LINEDEFS && shapevecs.Count > 1)
                    {
                        for (int i = 1; i < shapevecs.Count; i++)
                        {
                            // Join points together
                            General.Map.Map.CreateLinedef(vertices[i - 1], vertices[i]);
                        }
                        if (wasClosedShape)
                        {
                            // Join first with last vertex for closed shape
                            General.Map.Map.CreateLinedef(vertices[shapevecs.Count - 1], vertices[0]);
                        }
                    }
                }

                if (createas == THINGS)
                {
                    //Update Things filter
                    General.Map.ThingsFilter.Update();
                }

                // Snap to map format accuracy
                General.Map.Map.SnapAllToAccuracy();

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
            }

            if (removevertices)
            {
                // Remove vertices and all attached linedefs
                General.Map.Map.BeginAddRemove();
                foreach (Vertex v in selected)
                {
                    if (v.Linedefs.Count > 0)
                    {
                        List<Linedef> list = new List<Linedef>(v.Linedefs);
                        foreach (Linedef ld in list)
                        {
                            ld.Dispose();
                        }
                    }
                    else
                    {
                        v.Dispose();
                    }
                }
                General.Map.Map.EndAddRemove();
            }

            // Notify user
            if (selected.Count == 1)
            {
                General.Interface.DisplayStatus(StatusType.Action, "Created a shape from vertex.");
            }
            else
            {
                General.Interface.DisplayStatus(StatusType.Action, "Created " + selected.Count + " shapes from vertices.");
            }

			// Return to base mode
			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// Redrawing display
		public override void OnRedrawDisplay()
		{
			renderer.RedrawSurface();

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

            BuilderPlug.Me.VertexIntoShapeForm.RestartSeed();
            bool previewreference = BuilderPlug.Me.VertexIntoShapeForm.PreviewReference;
            bool removevertices = BuilderPlug.Me.VertexIntoShapeForm.RemoveVertices;
            bool ellipse = BuilderPlug.Me.VertexIntoShapeForm.Ellipse;
            int createas = BuilderPlug.Me.VertexIntoShapeForm.CreateAs;
            bool frontoutside = BuilderPlug.Me.VertexIntoShapeForm.FrontOutside;
            int spikingmode = BuilderPlug.Me.VertexIntoShapeForm.SpikingMode;

            float vsize = (renderer.VertexSize + 1.0f) / renderer.Scale;
            PixelColor color = General.Colors.Highlight;

			// Render overlay
			if(renderer.StartOverlay(true))
			{
				// Go for all selected vertices (reference lines)
                if (previewreference)
                {
                    foreach (Vertex v in selected)
                    {
                        // This values can be random...
                        float radiusX = BuilderPlug.Me.VertexIntoShapeForm.RadiusX;
                        float radiusY = BuilderPlug.Me.VertexIntoShapeForm.RadiusY;
                        int sides = BuilderPlug.Me.VertexIntoShapeForm.Sides;
                        int spikiness = BuilderPlug.Me.VertexIntoShapeForm.Spikiness;
                        float startangle = Angle2D.DegToRad(BuilderPlug.Me.VertexIntoShapeForm.StartAngle);
                        float sweepangle = Angle2D.DegToRad(BuilderPlug.Me.VertexIntoShapeForm.SweepAngle);
                        BuilderPlug.Me.VertexIntoShapeForm.NextShape();

                        // Skip shapes that are smaller than 1 radius.
                        if (radiusX < 1f || radiusY < 1f) continue;

                        // Generate shape
                        Vector2D origin = v.Position;
                        List<Vector2D> shapevecs = GenerateShape(origin, sides, spikiness, spikingmode, radiusX, radiusY, startangle, startangle + sweepangle);

                        // render reference
                        if (previewreference)
                        {
                            List<Vector2D> refcircle = GenerateShape(origin, 24, 0, 0, radiusX, radiusY, startangle, startangle + sweepangle);
                            for (int i = 1; i < refcircle.Count; i++)
                                renderer.RenderLine(refcircle[i - 1], refcircle[i], LINE_THICKNESS, General.Colors.Grid, true);
                            renderer.RenderLine(origin, shapevecs[0], LINE_THICKNESS, General.Colors.Grid, true);
                            renderer.RenderLine(origin, shapevecs[shapevecs.Count - 1], LINE_THICKNESS, General.Colors.Grid, true);
                        }
                    }
                }

                BuilderPlug.Me.VertexIntoShapeForm.RestartSeed();

                // Go for all selected vertices (actual shape)
				foreach (Vertex v in selected)
				{
                    // This values can be random...
                    float radiusX = BuilderPlug.Me.VertexIntoShapeForm.RadiusX;
                    float radiusY = BuilderPlug.Me.VertexIntoShapeForm.RadiusY;
                    int sides = BuilderPlug.Me.VertexIntoShapeForm.Sides;
                    int spikiness = BuilderPlug.Me.VertexIntoShapeForm.Spikiness;
                    float startangle = Angle2D.DegToRad(BuilderPlug.Me.VertexIntoShapeForm.StartAngle);
                    float sweepangle = Angle2D.DegToRad(BuilderPlug.Me.VertexIntoShapeForm.SweepAngle);
                    BuilderPlug.Me.VertexIntoShapeForm.NextShape();

                    // Skip shapes that are smaller than 1 radius.
                    if (radiusX < 1f || radiusY < 1f) continue;

                    // Generate shape
                    Vector2D origin = v.Position;
                    List<Vector2D> shapevecs = GenerateShape(origin, sides, spikiness, spikingmode, radiusX, radiusY, startangle, startangle + sweepangle);

                    // Check if the shape is closed
                    bool isShapeClosed = shapevecs[0].CloseTo(shapevecs[shapevecs.Count - 1], 0.001f);

                    // render shape
                    if (createas <= LINEDEFS)
                    {
                        for (int i = 1; i < shapevecs.Count; i++)
                            renderer.RenderLine(shapevecs[i - 1], shapevecs[i], LINE_THICKNESS, color, true);

                        if (!isShapeClosed && createas == CLOSESHAPE_ORIGIN)
                        {
                            renderer.RenderLine(shapevecs[shapevecs.Count - 1], origin, LINE_THICKNESS, color, true);
                            renderer.RenderLine(origin, shapevecs[0], LINE_THICKNESS, color, true);
                        }
                        if (!isShapeClosed && createas == CLOSESHAPE_VERTICES)
                        {
                            renderer.RenderLine(shapevecs[shapevecs.Count - 1], shapevecs[0], LINE_THICKNESS, color, true);
                        }
                    }

                    // render vertices
                    for (int i = 0; i < shapevecs.Count; i++)
                        renderer.RenderRectangleFilled(new RectangleF(shapevecs[i].x - vsize, shapevecs[i].y - vsize, vsize * 2.0f, vsize * 2.0f), color, true);

                    // Gray out origin if is to remove vertex
                    if (removevertices)
                        renderer.RenderRectangleFilled(new RectangleF(origin.x - vsize, origin.y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.Grid, true);
                }
				renderer.Finish();
			}

			renderer.Present();
		}
		
		#endregion
	}
}
