#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Windows;

#endregion

//JBR Draw Shape Mode
namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Draw Shape Mode",
			  SwitchAction = "drawshapemode",
              ButtonImage = "DrawShapeMode.png",
			  ButtonOrder = int.MinValue + 6,
			  ButtonGroup = "000_drawing",
			  AllowCopyPaste = false,
			  Volatile = true,
			  Optional = false)]

	public class DrawShapeMode : DrawGeometryMode
	{
		#region ================== Variables

        public const int CLOSESHAPE_ORIGIN = 0;
        public const int CLOSESHAPE_VERTICES = 1;
        public const int SHAPE = 2;
        public const int LINEDEFS = 3;
        public const int VERTICES = 4;
        public const int THINGS = 5;

        protected static int thingtype = 1; // Changed when adding things

        protected static bool previewreference = true;
        protected static bool ellipse = true;
        protected static int firstpointtype = 0;
        protected static int sides = 8;
        protected static int spikiness = 0;
        protected static int spikingmode = 0;
        protected static int createas = 0;
        protected static bool frontoutside = false;
        protected static float startangle = 0f;
        protected static float sweepangle = Angle2D.PI2;
        protected static bool limitonquad = true;
        protected static int transformonquad = 0; // Leave as always 0 for now

        protected Vector2D start;
        protected Vector2D end;
        protected Vector2D origin;

		//interface
		private DrawShapeOptionsPanel panel;
		private Docker docker;

		#endregion

		#region ================== Constructor

		public DrawShapeMode() 
		{
        }

		#endregion

		#region ================== Methods

        // This generates the shape from a vertex
        protected static List<Vector2D> GenerateShape(Vector2D origin, int sides, int spikiness, int spikingmode, float radiusX, float radiusY, float start, float end)
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
            if ((spikingmode & 2) == 2) {
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
        protected static void CalculateOrigin(int firstpointtype, bool ellipse, Vector2D start, Vector2D end, out Vector2D origin, out Vector2D endpoint, out float radiusX, out float radiusY)
        {
            origin = start;
            endpoint = end;
            if (firstpointtype == 1)
            {
                // 1st point in corner like DrawEllipse
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
            else if (firstpointtype == 2)
            {
                // 1st point in side/corner towards origin
                radiusX = Math.Abs(end.x - start.x);
                radiusY = Math.Abs(end.y - start.y);
                origin = end;
                endpoint = start;
                if (!ellipse)
                {
                    if (radiusX > radiusY)
                        radiusY = radiusX;
                    else
                        radiusX = radiusY;
                }
            }
            else
            {
                // 1st point in center towards radius
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

        // Recalculate angle from quadrant
        protected static float ReCalculateAngleFromQuadrant(int transformonquad, float angle, bool inverse, Vector2D start, Vector2D end)
        {
            Vector2D delta = end - start;
            if (transformonquad == 0) // top-right quadrant (1)
            {
                if (delta.x < 0)
                {
                    if (delta.y < 0) // quadrant 3
                        angle += inverse ? -Angle2D.PI : Angle2D.PI;
                    else             // quadrant 2
                        angle += inverse ? -Angle2D.PIHALF : Angle2D.PIHALF;
                }
                else
                {
                    if (delta.y < 0) // quadrant 4
                        angle += inverse ? -Angle2D.PIANDHALF : Angle2D.PIANDHALF;
                    else             // quadrant 1
                        angle += inverse ? -0 : 0;
                }
            }
            else if (transformonquad == 1) // top-left quadrant (2)
            {
                if (delta.x < 0)
                {
                    if (delta.y < 0) // quadrant 3
                        angle += inverse ? -Angle2D.PIHALF : Angle2D.PIHALF;
                    else             // quadrant 2
                        angle += inverse ? -0 : 0;
                }
                else
                {
                    if (delta.y < 0) // quadrant 4
                        angle += inverse ? -Angle2D.PI : Angle2D.PI;
                    else             // quadrant 1
                        angle += inverse ? Angle2D.PIHALF : -Angle2D.PIHALF;
                }
            }
            else if (transformonquad == 2) // bottom-left quadrant (3)
            {
                if (delta.x < 0)
                {
                    if (delta.y < 0) // quadrant 3
                        angle += inverse ? -0 : 0;
                    else             // quadrant 2
                        angle += inverse ? Angle2D.PIHALF : -Angle2D.PIHALF;
                }
                else
                {
                    if (delta.y < 0) // quadrant 4
                        angle += inverse ? -Angle2D.PIHALF : Angle2D.PIHALF;
                    else             // quadrant 1
                        angle += inverse ? Angle2D.PI : -Angle2D.PI;
                }
            }
            else if (transformonquad == 3) // bottom-right quadrant (4)
            {
                if (delta.x < 0)
                {
                    if (delta.y < 0) // quadrant 3
                        angle += inverse ? Angle2D.PIHALF : -Angle2D.PIHALF;
                    else             // quadrant 2
                        angle += inverse ? Angle2D.PI : -Angle2D.PI;
                }
                else
                {
                    if (delta.y < 0) // quadrant 4
                        angle += inverse ? -0 : 0;
                    else             // quadrant 1
                        angle += inverse ? Angle2D.PIANDHALF : -Angle2D.PIANDHALF;
                }
            }
            else if (transformonquad == 4) // Flip around (disabled!)
            {
                if (delta.x < 0)
                {
                    // Flip horizontally
                    if (angle < Angle2D.PI)
                        angle = Angle2D.PI - angle;
                    else
                        angle = Angle2D.PI2 + Angle2D.PI - angle;
                }
                if (delta.y < 0)
                {
                    // Flip vertically
                    angle = Angle2D.PI2 - angle;
                }
            }
            if (angle < 0f) angle += Angle2D.PI2;
            if (angle >= Angle2D.PI2) angle -= Angle2D.PI2;
            return angle;
        }

        // Allow user to modify angles by using the mouse
        protected void EndpointDrawShapeAngle(bool doSweepAngle)
        {
            Vector2D delta = end - start;
            float angle = -(float)Math.Atan2(-delta.y, delta.x);
            if (angle < 0f) angle = Angle2D.PI2 + angle;
            if (doSweepAngle)
            {
                float restartang = startangle;
                if (limitonquad) restartang = ReCalculateAngleFromQuadrant(transformonquad, restartang, false, start, end);
                sweepangle = angle - restartang;
                if (sweepangle < 0f) sweepangle += Angle2D.PI2;
                if (sweepangle == 0f || sweepangle > Angle2D.PI2) sweepangle = Angle2D.PI2;
                panel.SweepAngle = Angle2D.RadToDeg(sweepangle);
            }
            else
            {
                startangle = angle;
                if (limitonquad) startangle = ReCalculateAngleFromQuadrant(transformonquad, startangle, true, start, end);
                panel.StartAngle = Angle2D.RadToDeg(startangle);
            }
        }

        // This draws a point at a specific location
        protected virtual bool AddPointAt(List<DrawnVertex> shapepts, Vector2D pos, bool stitch, bool stitchline)
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

		override protected void Update() 
		{
            PixelColor stitchcolor = General.Colors.Highlight;
            PixelColor losecolor = General.Colors.Selection;

            snaptocardinaldirection = General.Interface.ShiftState && General.Interface.AltState; //mxd
            snaptogrid = (snaptocardinaldirection || General.Interface.ShiftState ^ General.Interface.SnapToGrid);
            snaptonearest = General.Interface.CtrlState ^ General.Interface.AutoMerge;

            DrawnVertex curp = GetCurrentPosition();
            float vsize = (renderer.VertexSize + 1.0f) / renderer.Scale;

			// Render drawing lines
			if(renderer.StartOverlay(true)) 
			{
                PixelColor color = snaptonearest ? stitchcolor : losecolor;

                if (points.Count == 1)
                {
                    // Update ending point
                    if (curp.pos.IsFinite()) end = curp.pos;

                    // Generate shape
                    Vector2D endpoint;
                    float radiusX, radiusY;
                    CalculateOrigin(firstpointtype, ellipse, start, end, out origin, out endpoint, out radiusX, out radiusY);
                    float angle = startangle;
                    if (limitonquad) angle = ReCalculateAngleFromQuadrant(transformonquad, startangle, false, start, end);
                    List<Vector2D> shapevecs = GenerateShape(origin, sides, spikiness, spikingmode, radiusX, radiusY, angle, angle + sweepangle);

                    // Check if the shape is closed
                    bool isShapeClosed = shapevecs[0].CloseTo(shapevecs[shapevecs.Count - 1], 0.1f);

                    // render reference
                    if (previewreference)
                    {
                        List<Vector2D> refcircle = GenerateShape(origin, 32, 0, 0, radiusX, radiusY, angle, angle + sweepangle);
                        for (int i = 1; i < refcircle.Count; i++)
                            renderer.RenderLine(refcircle[i - 1], refcircle[i], LINE_THICKNESS, General.Colors.Grid, true);
                        renderer.RenderLine(origin, shapevecs[0], LINE_THICKNESS, General.Colors.Grid, true);
                        renderer.RenderLine(origin, shapevecs[shapevecs.Count - 1], LINE_THICKNESS, General.Colors.Grid, true);
                    }
                    renderer.RenderRectangleFilled(new RectangleF(start.x - vsize, start.y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.InfoLine, true);
                    renderer.RenderRectangleFilled(new RectangleF(endpoint.x - vsize, endpoint.y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.InfoLine, true);

                    // render labels
                    Vector2D[] labelCoords = new[] {
                        new Vector2D(origin.x - radiusX, origin.y - radiusY),
                        new Vector2D(origin.x + radiusX, origin.y - radiusY),
                        new Vector2D(origin.x + radiusX, origin.y + radiusY),
                        new Vector2D(origin.x - radiusX, origin.y + radiusY),
                        new Vector2D(origin.x - radiusX, origin.y - radiusY)
                    };
                    for (int i = 1; i < 5; i++)
                    {
                        labels[i - 1].Move(labelCoords[i], labelCoords[i - 1]);
                        renderer.RenderText(labels[i - 1].TextLabel);
                    }
                    labels[4].Move(origin, shapevecs[0]);
                    renderer.RenderText(labels[4].TextLabel);
                    labels[5].Move(shapevecs[0], shapevecs[1]);
                    renderer.RenderText(labels[5].TextLabel);
                    if (sweepangle < Angle2D.PI2)
                    {
                        labels[5].Move(origin, shapevecs[shapevecs.Count - 1]);
                        renderer.RenderText(labels[5].TextLabel);
                    }

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
                }
                else
                {
                    // Render vertex at cursor
                    renderer.RenderRectangleFilled(new RectangleF(curp.pos.x - vsize, curp.pos.y - vsize, vsize * 2.0f, vsize * 2.0f), color, true);
                }

				// Done
				renderer.Finish();
			}

			// Done
			renderer.Present();
		}

        // This draws a point at a specific location
        override public bool DrawPointAt(Vector2D pos, bool stitch, bool stitchline)
        {
            if (pos.x < General.Map.Config.LeftBoundary || pos.x > General.Map.Config.RightBoundary ||
                pos.y > General.Map.Config.TopBoundary || pos.y < General.Map.Config.BottomBoundary)
                return false;

            DrawnVertex newpoint = new DrawnVertex();
            newpoint.pos = pos;
            newpoint.stitch = true; //stitch
            newpoint.stitchline = stitchline;
            points.Add(newpoint);

            if (points.Count == 1) //add point and labels
            {
                start = newpoint.pos;
                labels.AddRange(new[] {
                    new LineLengthLabel(false, true),
                    new LineLengthLabel(false, true),
                    new LineLengthLabel(false, true),
                    new LineLengthLabel(false, true),
                    new LineLengthLabel(true, true),
                    new LineLengthLabel(true, true) 
                });
                Update();
            }
            else if (points[0].pos == points[1].pos) //nothing is drawn
            {
                points = new List<DrawnVertex>();
                FinishDraw();
            }
            else
            {
                end = newpoint.pos;
                FinishDraw();
            }
            return true;
        }

        override public void RemovePoint()
        {
            if (points.Count > 0) points.RemoveAt(points.Count - 1);
            if (labels.Count > 0) labels = new List<LineLengthLabel>();
            Update();
        }

		#endregion

        #region ================== Events

        public override void OnEngage()
        {
            base.OnEngage();

            // Create and setup settings panel
            panel = new DrawShapeOptionsPanel();
            panel.PreviewReference = previewreference;
            panel.Ellipse = ellipse;
            panel.FirstPointType = firstpointtype;
            panel.CreateAs = createas;
            panel.FrontOutside = frontoutside;
            panel.Sides = sides;
            panel.Spikiness = spikiness;
            panel.SpikingMode = spikingmode;
            panel.StartAngle = Angle2D.RadToDeg(startangle);
            panel.SweepAngle = Angle2D.RadToDeg(sweepangle);
            panel.LimitAngleQuad = limitonquad;
            panel.OnValueChanged += OptionsPanelOnValueChanged;

            // Add docker
            docker = new Docker("drawshape", "Draw Shape", panel);
            General.Interface.AddDocker(docker);
            General.Interface.SelectDocker(docker);
        }

        public override void OnDisengage()
        {
            base.OnDisengage();

            // Remove docker
            General.Interface.RemoveDocker(docker);
            panel.Dispose();
            panel = null;
        }

        override public void OnAccept()
        {
            Cursor.Current = Cursors.AppStarting;
            General.Settings.FindDefaultDrawSettings();

            if (points.Count == 2)
            {
                // Make undo for the draw
                General.Map.UndoRedo.CreateUndo(String.Format("{0} sides shape draw", sides));

                // Generate shape
                Vector2D endpoint;
                float radiusX, radiusY;
                CalculateOrigin(firstpointtype, ellipse, start, end, out origin, out endpoint, out radiusX, out radiusY);
                float angle = startangle;
                if (limitonquad) angle = ReCalculateAngleFromQuadrant(transformonquad, startangle, false, start, end);
                List<Vector2D> shapevecs = GenerateShape(origin, sides, spikiness, spikingmode, radiusX, radiusY, angle, angle + sweepangle);

                if (points.Count > 0)
                {
                    // Delete last vertex if match with first
                    bool wasClosedShape = shapevecs[0].CloseTo(shapevecs[shapevecs.Count - 1], 0.001f);
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
                            // NOTE: I have to call this twice, because the first time only cancels this volatile mode
                            General.Map.UndoRedo.WithdrawUndo();
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
                            // Pick the thing to add...
                            ThingBrowserForm f = new ThingBrowserForm(thingtype);
                            if (f.ShowDialog(BuilderPlug.Me.MenusForm) != DialogResult.OK)
                            {
                                f.Dispose();
                                General.Map.UndoRedo.WithdrawUndo();
                                return;
                            }
                            thingtype = f.SelectedType;
                            f.Dispose();

                            // ... and place them
                            for (int i = 0; i < shapevecs.Count; i++)
                            {
                                Thing tp = General.Map.Map.CreateThing();
                                if (tp == null)
                                {
                                    General.Map.UndoRedo.WithdrawUndo();
                                    return;
                                }
                                General.Settings.ApplyDefaultThingSettings(tp);
                                tp.SRB2Type = thingtype;
                                tp.Parameter = 0;
                                tp.Move(shapevecs[i]);
                                tp.UpdateConfiguration();
                            }
                            
                            //Update Things filter
                            General.Map.ThingsFilter.Update();
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

                // Show info
                General.Interface.DisplayStatus(StatusType.Action, String.Format("Created {0} sides shape.", sides));
            }

            // Done
            Cursor.Current = Cursors.Default;

            // Return to original mode
            General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
        }

        private void OptionsPanelOnValueChanged(object sender, EventArgs eventArgs)
        {
            previewreference = panel.PreviewReference;
            ellipse = panel.Ellipse;
            firstpointtype = panel.FirstPointType;
            createas = panel.CreateAs;
            frontoutside = panel.FrontOutside;
            sides = panel.Sides;
            spikiness = panel.Spikiness;
            spikingmode = panel.SpikingMode;
            startangle = Angle2D.DegToRad(panel.StartAngle);
            sweepangle = Angle2D.DegToRad(panel.SweepAngle);
            limitonquad = panel.LimitAngleQuad;
            Update();
        }

        // Mouse moves
        public override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Middle)
            {
                EndpointDrawShapeAngle(General.Interface.CtrlState);
            }
        }

        public override void OnHelp()
        {
            General.ShowHelp("mode_drawshape.html");
        }

        #endregion

		#region ================== Actions

        [BeginAction("increasebevel")]
        protected void IncreaseBevel()
        {
            if (spikiness < 4096)
            {
                spikiness++;
                panel.Spikiness = spikiness;
                Update();
            }
        }

        [BeginAction("decreasebevel")]
        protected void DecreaseBevel()
        {
            if (spikiness > 0)
            {
                spikiness--;
                panel.Spikiness = spikiness;
                Update();
            }
        }

        [BeginAction("increasesubdivlevel")]
        protected void IncreaseSubdivLevel()
        {
            if (sides < 1024)
            {
                sides++;
                panel.Sides = sides;
                Update();
            }
        }

        [BeginAction("decreasesubdivlevel")]
        protected void DecreaseSubdivLevel()
        {
            if (sides > 2)
            {
                sides--;
                panel.Sides = sides;
                Update();
            }
        }

		#endregion

	}
}
