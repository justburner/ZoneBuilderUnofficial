#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.Map;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Draw Rectangle Mode",
			  SwitchAction = "drawrectanglemode",
			  ButtonImage = "DrawRectangleMode.png", //mxd	
			  ButtonOrder = int.MinValue + 3, //mxd
			  ButtonGroup = "000_drawing", //mxd
			  AllowCopyPaste = false,
			  Volatile = true,
			  Optional = false)]

	public class DrawRectangleMode : DrawGeometryMode
	{
		#region ================== Variables

		protected HintLabel hintlabel;
		protected int bevelWidth;
		protected int currentBevelWidth;
		protected int subdivisions;

		protected int maxSubdivisions;
		protected int minSubdivisions;

		protected string undoName = "Rectangle draw";
		protected string shapeName = "rectangle";

		protected Vector2D start;
		protected Vector2D end;
		protected int width;
		protected int height;

		//interface
		private DrawRectangleOptionsPanel panel;

		#endregion

		#region ================== Constructor/Disposer

		public DrawRectangleMode() 
		{
			snaptogrid = true;
			usefourcardinaldirections = true;
			SetupInterface();
		}

		public override void Dispose() 
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Clean up
				if(hintlabel != null) hintlabel.Dispose();

				// Done
				base.Dispose();
			}
		}

		#endregion

		#region ================== Settings panel

		protected virtual void SetupInterface() 
		{
			maxSubdivisions = 16;

			//Add options docker
			panel = new DrawRectangleOptionsPanel();
			panel.MaxSubdivisions = maxSubdivisions;
			panel.MinSubdivisions = minSubdivisions;
			panel.MaxBevelWidth = (int)General.Map.FormatInterface.MaxCoordinate;
			panel.MinBevelWidth = (int)General.Map.FormatInterface.MinCoordinate;
			panel.OnValueChanged += OptionsPanelOnValueChanged;
		}

		protected virtual void AddInterface() 
		{
			panel.Register();
			bevelWidth = panel.BevelWidth;
			subdivisions = panel.Subdivisions;
		}

		protected virtual void RemoveInterface() 
		{
			panel.Unregister();
		}

		#endregion

		#region ================== Methods

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
				
				if(points.Count == 1) 
				{
					UpdateReferencePoints(points[0], curp);
					Vector2D[] shape = GetShape(start, end);

					//render shape
					for(int i = 1; i < shape.Length; i++)
						renderer.RenderLine(shape[i - 1], shape[i], LINE_THICKNESS, color, true);

					//vertices
					for(int i = 0; i < shape.Length; i++)
						renderer.RenderRectangleFilled(new RectangleF(shape[i].x - vsize, shape[i].y - vsize, vsize * 2.0f, vsize * 2.0f), color, true);

					//and labels
					Vector2D[] labelCoords = new[] { start, new Vector2D(end.x, start.y), end, new Vector2D(start.x, end.y), start };
					for(int i = 1; i < 5; i++) 
					{
						labels[i - 1].Move(labelCoords[i], labelCoords[i - 1]);
						renderer.RenderText(labels[i - 1].TextLabel);
					}

					//got beveled corners? 
					if(shape.Length > 5) 
					{
						//render hint
						if(width > 64 * vsize && height > 16 * vsize) 
						{
							hintlabel.Move(start, end);
							hintlabel.Text = GetHintText();
							renderer.RenderText(hintlabel.TextLabel);
						}
						
						//and shape corners
						for(int i = 0; i < 4; i++)
							renderer.RenderRectangleFilled(new RectangleF(labelCoords[i].x - vsize, labelCoords[i].y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.InfoLine, true);
					}
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

		protected virtual Vector2D[] GetShape(Vector2D pStart, Vector2D pEnd) 
		{
			//no shape
			if(pStart == pEnd) 
			{
				currentBevelWidth = 0;
				return new Vector2D[0];
			}

			//line
			if(pEnd.x == pStart.x || pEnd.y == pStart.y) 
			{
				currentBevelWidth = 0;
				return new[] { pStart, pEnd };
			}

			//no corners
			if(bevelWidth == 0) 
			{
				currentBevelWidth = 0;
				return new[] { pStart, new Vector2D((int)pStart.x, (int)pEnd.y), pEnd, new Vector2D((int)pEnd.x, (int)pStart.y), pStart };
			}

			//got corners. TODO: check point order
			bool reverse = false;
			currentBevelWidth = Math.Min(Math.Abs(bevelWidth), Math.Min(width, height) / 2);
			
			if(bevelWidth < 0) 
			{
				currentBevelWidth *= -1;
				reverse = true;
			}

			List<Vector2D> shape = new List<Vector2D>();

			//top-left corner
			shape.AddRange(GetCornerPoints(pStart, currentBevelWidth, currentBevelWidth, !reverse));

			//top-right corner
			shape.AddRange(GetCornerPoints(new Vector2D(pEnd.x, pStart.y), -currentBevelWidth, currentBevelWidth, reverse));

			//bottom-right corner
			shape.AddRange(GetCornerPoints(pEnd, -currentBevelWidth, -currentBevelWidth, !reverse));

			//bottom-left corner
			shape.AddRange(GetCornerPoints(new Vector2D(pStart.x, pEnd.y), currentBevelWidth, -currentBevelWidth, reverse));

			//closing point
			shape.Add(shape[0]);

			return shape.ToArray();
		}

		private Vector2D[] GetCornerPoints(Vector2D startPoint, int bevel_width, int bevel_height, bool reverse) 
		{
			Vector2D[] points;
			Vector2D center = (bevelWidth > 0 ? new Vector2D(startPoint.x + bevel_width, startPoint.y + bevel_height) : startPoint);
			float curAngle = Angle2D.PI;

			int steps = subdivisions + 2;
			points = new Vector2D[steps];
			float stepAngle = Angle2D.PIHALF / (subdivisions + 1);

			for(int i = 0; i < steps; i++) 
			{
				points[i] = new Vector2D(center.x + (float)Math.Sin(curAngle) * bevel_width, center.y + (float)Math.Cos(curAngle) * bevel_height);
				curAngle += stepAngle;
			}

			if(reverse) Array.Reverse(points);
			return points;
		}

		protected virtual string GetHintText() 
		{
			return "BVL: " + bevelWidth + "; SUB: " + subdivisions;
		}

		//update top-left and bottom-right points, which define drawing shape
		private void UpdateReferencePoints(DrawnVertex p1, DrawnVertex p2) 
		{
			if(!p1.pos.IsFinite() || !p2.pos.IsFinite()) return;

			if(p1.pos.x < p2.pos.x) 
			{
				start.x = p1.pos.x;
				end.x = p2.pos.x;
			} 
			else 
			{
				start.x = p2.pos.x;
				end.x = p1.pos.x;
			}

			if(p1.pos.y < p2.pos.y) 
			{
				start.y = p1.pos.y;
				end.y = p2.pos.y;
			} 
			else 
			{
				start.y = p2.pos.y;
				end.y = p1.pos.y;
			}

			width = (int)(end.x - start.x);
			height = (int)(end.y - start.y);
		}

		// This draws a point at a specific location
		override public bool DrawPointAt(Vector2D pos, bool stitch, bool stitchline) 
		{
			if(pos.x < General.Map.Config.LeftBoundary || pos.x > General.Map.Config.RightBoundary ||
				pos.y > General.Map.Config.TopBoundary || pos.y < General.Map.Config.BottomBoundary)
				return false;

			DrawnVertex newpoint = new DrawnVertex();
			newpoint.pos = pos;
			newpoint.stitch = true; //stitch
			newpoint.stitchline = stitchline;
			points.Add(newpoint);

			if(points.Count == 1) //add point and labels
			{ 
				labels.AddRange(new[] { new LineLengthLabel(false, true), new LineLengthLabel(false, true), new LineLengthLabel(false, true), new LineLengthLabel(false, true) });
				hintlabel = new HintLabel(General.Colors.InfoLine);
				Update();
			} 
			else if(points[0].pos == points[1].pos) //nothing is drawn
			{ 
				points = new List<DrawnVertex>();
				FinishDraw();
			} 
			else 
			{
				//create vertices for final shape. 
				UpdateReferencePoints(points[0], newpoint);
				points = new List<DrawnVertex>(); //clear points
				Vector2D[] shape = GetShape(start, end);

				foreach(Vector2D t in shape) base.DrawPointAt(t, true, true);

				FinishDraw();
			}
			return true;
		}

		override public void RemovePoint() 
		{
			if(points.Count > 0) points.RemoveAt(points.Count - 1);
			if(labels.Count > 0) labels = new List<LineLengthLabel>();
			Update();
		}

		#endregion

		#region ================== Events

		public override void OnEngage() 
		{
			base.OnEngage();
			AddInterface();
		}

		public override void OnDisengage() 
		{
			RemoveInterface();
			base.OnDisengage();
		}
		
		override public void OnAccept() 
		{
			Cursor.Current = Cursors.AppStarting;
			General.Settings.FindDefaultDrawSettings();

			// When we have a rectangle or a line
			if(points.Count > 4 || points.Count == 2) 
			{
				// Make undo for the draw
				General.Map.UndoRedo.CreateUndo(undoName);

				// Make an analysis and show info
				string[] adjectives = new[] { "gloomy", "sad", "unhappy", "lonely", "troubled", "depressed", "heartsick", "glum", "pessimistic", "bitter", "downcast" }; // aaand my english vocabulary ends here :)
				string word = adjectives[new Random().Next(adjectives.Length - 1)];
				string a = (word[0] == 'u' ? "an " : "a ");

				General.Interface.DisplayStatus(StatusType.Action, "Created " + a + word + " " + shapeName + ".");

				// Make the drawing
				if(!Tools.DrawLines(points, true, BuilderPlug.Me.AutoAlignTextureOffsetsOnCreate)) 
				{
					// Drawing failed
					// NOTE: I have to call this twice, because the first time only cancels this volatile mode
					General.Map.UndoRedo.WithdrawUndo();
					General.Map.UndoRedo.WithdrawUndo();
					return;
				}

				// Snap to map format accuracy
				General.Map.Map.SnapAllToAccuracy();

				// Clear selection
				General.Map.Map.ClearAllSelected();

				// Update cached values
				General.Map.Map.Update();

				// Edit new sectors?
				List<Sector> newsectors = General.Map.Map.GetMarkedSectors(true);
				if(BuilderPlug.Me.EditNewSector && (newsectors.Count > 0))
					General.Interface.ShowEditSectors(newsectors);

				// Update the used textures
				General.Map.Data.UpdateUsedTextures();

				//mxd
				General.Map.Renderer2D.UpdateExtraFloorFlag();

				// Map is changed
				General.Map.IsChanged = true;
			}

			// Done
			Cursor.Current = Cursors.Default;

			// Return to original mode
			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		public override void OnHelp() 
		{
			General.ShowHelp("/gzdb/features/classic_modes/mode_drawrect.html");
		}

		private void OptionsPanelOnValueChanged(object sender, EventArgs eventArgs) 
		{
			bevelWidth = panel.BevelWidth;
			subdivisions = panel.Subdivisions;
			Update();
		}

		#endregion

		#region ================== Actions

		[BeginAction("increasesubdivlevel")]
		protected virtual void IncreaseSubdivLevel() 
		{
			if(subdivisions < maxSubdivisions) 
			{
				subdivisions++;
				panel.Subdivisions = subdivisions;
				Update();
			}
		}

		[BeginAction("decreasesubdivlevel")]
		protected virtual void DecreaseSubdivLevel() 
		{
			if(subdivisions > minSubdivisions) 
			{
				subdivisions--;
				panel.Subdivisions = subdivisions;
				Update();
			}
		}

		[BeginAction("increasebevel")]
		protected virtual void IncreaseBevel() 
		{
			if(points.Count < 2 || currentBevelWidth == bevelWidth || bevelWidth < 0) 
			{
				bevelWidth = Math.Min(bevelWidth + General.Map.Grid.GridSize, panel.MaxBevelWidth);
				panel.BevelWidth = bevelWidth;
				Update();
			}
		}

		[BeginAction("decreasebevel")]
		protected virtual void DecreaseBevel() 
		{
			if(currentBevelWidth == bevelWidth || bevelWidth > 0) 
			{
				bevelWidth = Math.Max(bevelWidth - General.Map.Grid.GridSize, panel.MinBevelWidth);
				panel.BevelWidth = bevelWidth;
				Update();
			}
		}

		#endregion

	}
}
