
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.BuilderModes.Interface;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.GZBuilder.Geometry;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.Windows;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Linedefs Mode",
			  SwitchAction = "linedefsmode",	// Action name used to switch to this mode
			  ButtonImage = "LinesMode.png",	// Image resource name for the button
			  ButtonOrder = int.MinValue + 100,	// Position of the button (lower is more to the left)
			  ButtonGroup = "000_editing",
			  UseByDefault = true,
			  SafeStartMode = true)]

	public class LinedefsMode : BaseClassicMode
	{
		#region ================== Constants

		#endregion

		#region ================== Variables

		// Highlighted item
		private Linedef highlighted;
		private readonly Association[] association = new Association[Linedef.NUM_ARGS];
		private readonly Association highlightasso = new Association();
		private Vector2D insertpreview = new Vector2D(float.NaN, float.NaN); //mxd
		
		// Interface
		private bool editpressed;
		
		#endregion

		#region ================== Properties

		public override object HighlightedObject { get { return highlighted; } }
		
		#endregion

		#region ================== Constructor / Disposer

		public LinedefsMode()
		{
			//mxd. Associations now requre initializing...
			for(int i = 0; i < association.Length; i++) association[i] = new Association();
		}

		#endregion

		#region ================== Methods

		// This highlights a new item
		private void Highlight(Linedef l)
		{
			bool completeredraw = false;
			LinedefActionInfo action = null;

			// Often we can get away by simply undrawing the previous
			// highlight and drawing the new highlight. But if associations
			// are or were drawn we need to redraw the entire display.
			
			// Previous association highlights something?
			if((highlighted != null) && (highlighted.Tag != 0)) completeredraw = true;
			
			// Set highlight association
			if(l != null && l.Tag != 0)
				highlightasso.Set(new Vector2D((l.Start.Position  + l.End.Position)/2), l.Tags, UniversalType.LinedefTag);
			else
				highlightasso.Set(new Vector2D(), 0, 0);

			// New association highlights something?
			if((l != null) && (l.Tag != 0)) completeredraw = true;

			// Use the line tag to highlight sectors (Doom style)
			if(General.Map.Config.LineTagIndicatesSectors)
			{
				if(l != null)
					association[0].Set(new Vector2D((l.Start.Position  + l.End.Position)/2), l.Tags, UniversalType.SectorTag);
				else
					association[0].Set(new Vector2D(), 0, 0);
			}
			else
			{
				if(l != null)
				{
					// Check if we can find the linedefs action
					if((l.Action > 0) && General.Map.Config.LinedefActions.ContainsKey(l.Action))
						action = General.Map.Config.LinedefActions[l.Action];
				}
				
				// Determine linedef associations
				for(int i = 0; i < Linedef.NUM_ARGS; i++)
				{
					// Previous association highlights something?
					if((association[i].Type == UniversalType.SectorTag) ||
					   (association[i].Type == UniversalType.LinedefTag) ||
					   (association[i].Type == UniversalType.ThingTag)) completeredraw = true;

					// Make new association
					if(action != null)
						association[i].Set(new Vector2D((l.Start.Position  + l.End.Position)/2), l.Args[i], action.Args[i].Type);
					else
						association[i].Set(new Vector2D(), 0, 0);

					// New association highlights something?
					if((association[i].Type == UniversalType.SectorTag) ||
					   (association[i].Type == UniversalType.LinedefTag) ||
					   (association[i].Type == UniversalType.ThingTag)) completeredraw = true;
				}
			}
			
			// If we're changing associations, then we
			// need to redraw the entire display
			if(completeredraw)
			{
				// Set new highlight and redraw completely
				highlighted = l;
				General.Interface.RedrawDisplay();
			}
			else
			{
				// Update display
				if(renderer.StartPlotter(false))
				{
					// Undraw previous highlight
					Linedef possiblecommentline = l ?? highlighted; //mxd
					if((highlighted != null) && !highlighted.IsDisposed)
					{
						renderer.PlotLinedef(highlighted, renderer.DetermineLinedefColor(highlighted));
						renderer.PlotVertex(highlighted.Start, renderer.DetermineVertexColor(highlighted.Start));
						renderer.PlotVertex(highlighted.End, renderer.DetermineVertexColor(highlighted.End));
					}

					// Set new highlight
					highlighted = l;

					// Render highlighted item
					if((highlighted != null) && !highlighted.IsDisposed)
					{
						renderer.PlotLinedef(highlighted, General.Colors.Highlight);
						renderer.PlotVertex(highlighted.Start, renderer.DetermineVertexColor(highlighted.Start));
						renderer.PlotVertex(highlighted.End, renderer.DetermineVertexColor(highlighted.End));
					}

					// Done with highlight
					renderer.Finish();

					//mxd. Update comment highlight?
					if(General.Map.UDMF && General.Settings.RenderComments
						&& possiblecommentline != null && !possiblecommentline.IsDisposed
						&& renderer.StartOverlay(false))
					{
						RenderComment(possiblecommentline);
						renderer.Finish();
					}

					renderer.Present();
				}
			}

			// Show highlight info
			if((highlighted != null) && !highlighted.IsDisposed)
			{
				General.Interface.ShowLinedefInfo(highlighted);
			}
			else
			{
				General.Interface.Display.HideToolTip(); //mxd
				General.Interface.HideInfo();
			}
		}

		//mxd
		private void AlignTextureToLine(bool alignFloors, bool alignToFrontSide) 
		{
			ICollection<Linedef> lines = General.Map.Map.GetSelectedLinedefs(true);

			if(lines.Count == 0 && highlighted != null && !highlighted.IsDisposed)
				lines.Add(highlighted);

			if(lines.Count == 0) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "This action requires a selection");
				return;
			}

			//Create Undo
			string rest = (alignFloors ? "Floors" : "Ceilings") + " to " + (alignToFrontSide ? "Front" : "Back")+ " Side";
			General.Map.UndoRedo.CreateUndo("Align " + rest);
			int counter = 0;

			foreach(Linedef l in lines)
			{
				Sector s = null;

				if(alignToFrontSide) 
				{
					if(l.Front != null && l.Front.Sector != null) s = l.Front.Sector;
				} 
				else 
				{
					if(l.Back != null && l.Back.Sector != null)	s = l.Back.Sector;
				}

				if(s == null) continue;
				counter++;

				s.Fields.BeforeFieldsChange();

				float sourceAngle = (float)Math.Round(General.ClampAngle(alignToFrontSide ? -Angle2D.RadToDeg(l.Angle) + 90 : -Angle2D.RadToDeg(l.Angle) - 90), 1);
				if(!alignToFrontSide) sourceAngle = General.ClampAngle(sourceAngle + 180);

				//update angle
				UniFields.SetFloat(s.Fields, (alignFloors ? "rotationfloor" : "rotationceiling"), sourceAngle, 0f);

				//update offset
				Vector2D offset = (alignToFrontSide ? l.Start.Position : l.End.Position).GetRotated(Angle2D.DegToRad(sourceAngle));
				ImageData texture = General.Map.Data.GetFlatImage(s.LongFloorTexture);

				if((texture == null) || (texture == General.Map.Data.WhiteTexture) ||
				   (texture.Width <= 0) || (texture.Height <= 0) || !texture.IsImageLoaded) 
				{
					//meh...
				}
				else
				{
                    offset.x %= texture.Width / s.Fields.GetValue((alignFloors ? "xscalefloor" : "xscaleceiling"), 1.0f);
					offset.y %= texture.Height / s.Fields.GetValue((alignFloors ? "yscalefloor" : "yscaleceiling"), 1.0f);
				}

				UniFields.SetFloat(s.Fields, (alignFloors ? "xpanningfloor" : "xpanningceiling"), (float)Math.Round(-offset.x), 0f);
				UniFields.SetFloat(s.Fields, (alignFloors ? "ypanningfloor" : "ypanningceiling"), (float)Math.Round(offset.y), 0f);

				//update
				s.UpdateNeeded = true;
				s.UpdateCache();
			}

			General.Interface.DisplayStatus(StatusType.Info, "Aligned " +counter + " " + rest);

			//update
			General.Map.Map.Update();
			General.Interface.RedrawDisplay();
			General.Interface.RefreshInfo();
			General.Map.IsChanged = true;
		}

		//mxd
		private bool IsInSelectionRect(Linedef l, List<Line2D> selectionOutline) 
		{
			if(BuilderPlug.Me.MarqueSelectTouching) 
			{
				bool selected = selectionrect.Contains(l.Start.Position.x, l.Start.Position.y) || selectionrect.Contains(l.End.Position.x, l.End.Position.y);

				//check intersections with outline
				if(!selected) 
				{
					foreach(Line2D line in selectionOutline) 
					{
						if(Line2D.GetIntersection(l.Line, line)) return true;
					}
				}
				return selected;
			}

			return selectionrect.Contains(l.Start.Position.x, l.Start.Position.y) && selectionrect.Contains(l.End.Position.x, l.End.Position.y);
		}
		
		#endregion
		
		#region ================== Events

		public override void OnHelp()
		{
			General.ShowHelp("e_linedefs.html");
		}

		// Cancel mode
		public override void OnCancel()
		{
			base.OnCancel();

			// Return to this mode
			General.Editing.ChangeMode(new LinedefsMode());
		}

		// Mode engages
		public override void OnEngage()
		{
			base.OnEngage();
			renderer.SetPresentation(Presentation.Standard);
			
			// Add toolbar buttons
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.CopyProperties);
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.PasteProperties);
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.PastePropertiesOptions); //mxd
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.SeparatorCopyPaste);
			if(General.Map.UDMF) //mxd
			{
				General.Interface.AddButton(BuilderPlug.Me.MenusForm.MakeGradientBrightness);
				General.Interface.AddButton(BuilderPlug.Me.MenusForm.GradientInterpolationMenu);
			}
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.CurveLinedefs);
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.MarqueSelectTouching); //mxd
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.SyncronizeThingEditButton); //mxd
			if(General.Map.UDMF) General.Interface.AddButton(BuilderPlug.Me.MenusForm.TextureOffsetLock, ToolbarSection.Geometry); //mxd

			General.Interface.AddButton(BuilderPlug.Me.MenusForm.PlaceThings); //JBR
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.FlipLinedefsButton); //JBR
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.FlipSidedefsButton); //JBR
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.PerpendicularVertex); //JBR
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.PerpendicularLinedef); //JBR
			General.Interface.AddButton(BuilderPlug.Me.MenusForm.ParallelLinedef); //JBR

			//mxd. Update the tooltip
			BuilderPlug.Me.MenusForm.SyncronizeThingEditButton.ToolTipText = "Synchronized Things Editing" + Environment.NewLine + BuilderPlug.Me.MenusForm.SyncronizeThingEditLinedefsItem.ToolTipText;

			// Convert geometry selection to linedefs selection
			General.Map.Map.ConvertSelection(SelectionType.Linedefs);
			UpdateSelectionInfo(); //mxd
		}
		
		// Mode disengages
		public override void OnDisengage()
		{
			base.OnDisengage();

			// Remove toolbar buttons
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.CopyProperties);
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.PasteProperties);
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.PastePropertiesOptions); //mxd
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.SeparatorCopyPaste);
			if(General.Map.UDMF) //mxd
			{
				General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.MakeGradientBrightness);
				General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.GradientInterpolationMenu);
			}
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.CurveLinedefs);
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.MarqueSelectTouching); //mxd
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.SyncronizeThingEditButton); //mxd
			if(General.Map.UDMF) General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.TextureOffsetLock); //mxd

			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.PlaceThings); //JBR
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.FlipLinedefsButton); //JBR
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.FlipSidedefsButton); //JBR
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.PerpendicularVertex); //JBR
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.PerpendicularLinedef); //JBR
			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.ParallelLinedef); //JBR

			// Going to EditSelectionMode?
			if (General.Editing.NewMode is EditSelectionMode)
			{
				// Not pasting anything?
				EditSelectionMode editmode = (General.Editing.NewMode as EditSelectionMode);
				if(!editmode.Pasting)
				{
					// No selection made? But we have a highlight!
					if((General.Map.Map.GetSelectedLinedefs(true).Count == 0) && (highlighted != null))
					{
						// Make the highlight the selection
						highlighted.Selected = true;
					}
				}
			}

			// Hide highlight info and tooltip
			General.Interface.HideInfo();
			General.Interface.Display.HideToolTip(); //mxd
		}

		// This redraws the display
		public override void OnRedrawDisplay()
		{
			renderer.RedrawSurface();
			List<Line3D> eventlines = new List<Line3D>(); //mxd

			// Render lines
			if(renderer.StartPlotter(true))
			{
				renderer.PlotLinedefSet(General.Map.Map.Linedefs);
				for(int i = 0; i < Linedef.NUM_ARGS; i++) BuilderPlug.PlotAssociations(renderer, association[i], eventlines);
				
				if((highlighted != null) && !highlighted.IsDisposed)
				{
					BuilderPlug.PlotReverseAssociations(renderer, highlightasso, eventlines);
					renderer.PlotLinedef(highlighted, General.Colors.Highlight);
				}
				renderer.PlotVerticesSet(General.Map.Map.Vertices);
				renderer.Finish();
			}

			// Render things
			if(renderer.StartThings(true))
			{
				renderer.RenderThingSet(General.Map.ThingsFilter.HiddenThings, Presentation.THINGS_HIDDEN_ALPHA);
				renderer.RenderThingSet(General.Map.ThingsFilter.VisibleThings, Presentation.THINGS_ALPHA);
                renderer.RenderNiGHTSPath();
                renderer.Finish();
			}

			// Render selection
			if(renderer.StartOverlay(true))
			{
				if(!selecting) //mxd
				{ 
					for(int i = 0; i < Linedef.NUM_ARGS; i++) BuilderPlug.RenderAssociations(renderer, association[i], eventlines);
					if((highlighted != null) && !highlighted.IsDisposed) BuilderPlug.RenderReverseAssociations(renderer, highlightasso, eventlines); //mxd
				}
				else
				{
					RenderMultiSelection();
				}

				//mxd. Render vertex insert preview
				if(insertpreview.IsFinite())
				{
					float dist = Math.Min(Vector2D.Distance(mousemappos, insertpreview), BuilderPlug.Me.HighlightRange);
					byte alpha = (byte)(255 - (dist / BuilderPlug.Me.HighlightRange) * 128);
					float vsize = (renderer.VertexSize + 1.0f) / renderer.Scale;
					renderer.RenderRectangleFilled(new RectangleF(insertpreview.x - vsize, insertpreview.y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.InfoLine.WithAlpha(alpha), true);
				}

				renderer.RenderArrows(eventlines); //mxd

				//mxd. Render comments
				if(General.Map.UDMF && General.Settings.RenderComments) foreach(Linedef l in General.Map.Map.Linedefs) RenderComment(l);

				renderer.Finish();
			}

			renderer.Present();
		}

		// Selection
		protected override void OnSelectBegin()
		{
			// Item highlighted?
			if((highlighted != null) && !highlighted.IsDisposed)
			{
				// Update display
				if(renderer.StartPlotter(false))
				{
					// Redraw highlight to show selection
					renderer.PlotLinedef(highlighted, renderer.DetermineLinedefColor(highlighted));
					renderer.PlotVertex(highlighted.Start, renderer.DetermineVertexColor(highlighted.Start));
					renderer.PlotVertex(highlighted.End, renderer.DetermineVertexColor(highlighted.End));
					renderer.Finish();
					renderer.Present();
				}
			}

			base.OnSelectBegin();
		}

		// End selection
		protected override void OnSelectEnd()
		{
			// Not stopping from multiselection?
			if(!selecting)
			{
				// Item highlighted?
				if((highlighted != null) && !highlighted.IsDisposed)
				{
					//mxd. Flip selection
					highlighted.Selected = !highlighted.Selected;
					
					// Update display
					if(renderer.StartPlotter(false))
					{
						// Render highlighted item
						renderer.PlotLinedef(highlighted, General.Colors.Highlight);
						renderer.PlotVertex(highlighted.Start, renderer.DetermineVertexColor(highlighted.Start));
						renderer.PlotVertex(highlighted.End, renderer.DetermineVertexColor(highlighted.End));
						renderer.Finish();
						renderer.Present();
					}
				
				} 
				else if(BuilderPlug.Me.AutoClearSelection && General.Map.Map.SelectedLinedefsCount > 0) //mxd
				{
					General.Map.Map.ClearSelectedLinedefs();
					General.Interface.RedrawDisplay();
				}

				//mxd
				UpdateSelectionInfo();
			}

			base.OnSelectEnd();
		}
		
		// Start editing
		protected override void OnEditBegin()
		{
			// Item highlighted?
			if((highlighted != null) && !highlighted.IsDisposed)
			{
				// Edit pressed in this mode
				editpressed = true;

				// Highlighted item not selected?
				if(!highlighted.Selected && (BuilderPlug.Me.AutoClearSelection || (General.Map.Map.SelectedLinedefsCount == 0)))
				{
					// Make this the only selection
					General.Map.Map.ClearSelectedLinedefs();
					highlighted.Selected = true;
					General.Interface.RedrawDisplay();
				}

				// Update display
				if(renderer.StartPlotter(false))
				{
					// Redraw highlight to show selection
					renderer.PlotLinedef(highlighted, renderer.DetermineLinedefColor(highlighted));
					renderer.PlotVertex(highlighted.Start, renderer.DetermineVertexColor(highlighted.Start));
					renderer.PlotVertex(highlighted.End, renderer.DetermineVertexColor(highlighted.End));
					renderer.Finish();
					renderer.Present();
				}
			}
			else if(!selecting && BuilderPlug.Me.AutoDrawOnEdit) //mxd. We don't want to draw while multiselecting
			{
				// Start drawing mode
				DrawGeometryMode drawmode = new DrawGeometryMode();
				bool snaptogrid = General.Interface.ShiftState ^ General.Interface.SnapToGrid;
				bool snaptonearest = General.Interface.CtrlState ^ General.Interface.AutoMerge;
				DrawnVertex v = DrawGeometryMode.GetCurrentPosition(mousemappos, snaptonearest, snaptogrid, false, renderer, new List<DrawnVertex>());

				if(drawmode.DrawPointAt(v))
					General.Editing.ChangeMode(drawmode);
				else
					General.Interface.DisplayStatus(StatusType.Warning, "Failed to draw point: outside of map boundaries.");
			}
			
			base.OnEditBegin();
		}
		
		// Done editing
		protected override void OnEditEnd()
		{
			// Edit pressed in this mode?
			if(editpressed)
			{
				// Anything selected?
				ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);
				if(selected.Count > 0)
				{
					if(General.Interface.IsActiveWindow)
					{
						// Show line edit dialog
						DialogResult result = General.Interface.ShowEditLinedefs(selected);
						General.Map.Map.Update();
						
						// When a single line was selected, deselect it now
						if(selected.Count == 1) 
						{
							General.Map.Map.ClearSelectedLinedefs();
						} 
						else if(result == DialogResult.Cancel) //mxd. Restore selection...
						{ 
							foreach(Linedef l in selected) l.Selected = true;
						}

						// Update entire display
						General.Map.Renderer2D.UpdateExtraFloorFlag(); //mxd
						General.Interface.RedrawDisplay();
					}
				}

				UpdateSelectionInfo(); //mxd
			}

			editpressed = false;
			base.OnEditEnd();
		}
		
		// Mouse moves
		public override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if(panning) return; //mxd. Skip all this jazz while panning

			//mxd
			if(selectpressed && !editpressed && !selecting) 
			{
				// Check if moved enough pixels for multiselect
				Vector2D delta = mousedownpos - mousepos;
				if((Math.Abs(delta.x) > MULTISELECT_START_MOVE_PIXELS) ||
				   (Math.Abs(delta.y) > MULTISELECT_START_MOVE_PIXELS)) 
				{
					// Start multiselecting
					StartMultiSelection();
				}
			}
			else if(paintselectpressed && !editpressed && !selecting)  //mxd. Drag-select
			{
				// Find the nearest thing within highlight range
				Linedef l = General.Map.Map.NearestLinedefRange(mousemappos, BuilderPlug.Me.HighlightRange / renderer.Scale);

				if(l != null) 
				{
					if(l != highlighted) 
					{
						//toggle selected state
						if(General.Interface.ShiftState ^ BuilderPlug.Me.AdditiveSelect)
							l.Selected = true;
						else if(General.Interface.CtrlState)
							l.Selected = false;
						else
							l.Selected = !l.Selected;
						highlighted = l;

						UpdateSelectionInfo(); //mxd

						// Update entire display
						General.Interface.RedrawDisplay();
					}
				} 
				else if(highlighted != null) 
				{
					Highlight(null);

					// Update entire display
					General.Interface.RedrawDisplay();
				}
			}
			else if(e.Button == MouseButtons.None) // Not holding any buttons?
			{
				// Find the nearest linedef within highlight range
				Linedef l = General.Map.Map.NearestLinedefRange(mousemappos, BuilderPlug.Me.StitchRange / renderer.Scale);

				//mxd. Render insert vertex preview
				if(l != null) 
				{
					bool snaptogrid = General.Interface.ShiftState ^ General.Interface.SnapToGrid;
					bool snaptonearest = General.Interface.CtrlState ^ General.Interface.AutoMerge;
					Vector2D v = DrawGeometryMode.GetCurrentPosition(mousemappos, snaptonearest, snaptogrid, false, renderer, new List<DrawnVertex>()).pos;

					if(v != insertpreview)
					{
						insertpreview = v;
						General.Interface.RedrawDisplay();
					}
				} 
				else if(insertpreview.IsFinite()) 
				{
					insertpreview.x = float.NaN;
					General.Interface.RedrawDisplay();
				}

				// Highlight if not the same
				if(l != highlighted) Highlight(l);

				//mxd. Show tooltip?
				if(General.Map.UDMF && General.Settings.RenderComments && mouselastpos != mousepos && highlighted != null && !highlighted.IsDisposed && highlighted.Fields.ContainsKey("comment"))
				{
					string comment = highlighted.Fields.GetValue("comment", string.Empty);
					if(comment.Length > 2)
					{
						string type = comment.Substring(0, 3);
						int index = Array.IndexOf(CommentType.Types, type);
						if(index > 0) comment = comment.TrimStart(type.ToCharArray());
					}
					General.Interface.Display.ShowToolTip("Comment:", comment, (int)(mousepos.x + 32 * MainForm.DPIScaler.Width), (int)(mousepos.y + 8 * MainForm.DPIScaler.Height));
				}
			} 
		}

		// Mouse leaves
		public override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);

			// Highlight nothing
			Highlight(null);
		}

		//mxd
		protected override void BeginViewPan() 
		{
			// We don't want vertex preview while panning
			insertpreview.x = float.NaN;
			base.BeginViewPan();
		}

		//mxd
		protected override void OnPaintSelectBegin() 
		{
			// Highlight nothing
			Highlight(null);

			base.OnPaintSelectBegin();
		}

		// Mouse wants to drag
		protected override void OnDragStart(MouseEventArgs e)
		{
			base.OnDragStart(e);

			// Edit button used?
			if(General.Actions.CheckActionActive(null, "classicedit"))
			{
				// Anything highlighted?
				if((highlighted != null) && !highlighted.IsDisposed)
				{
					// Highlighted item not selected?
					if(!highlighted.Selected)
					{
						// Select only this linedef for dragging
						General.Map.Map.ClearSelectedLinedefs();
						highlighted.Selected = true;
					}

					// Start dragging the selection
					if(!BuilderPlug.Me.DontMoveGeometryOutsideMapBoundary || CanDrag()) //mxd
						General.Editing.ChangeMode(new DragLinedefsMode(mousedownmappos));
				}
			}
		}

		//mxd. Check if any selected linedef is outside of map boundary
		private static bool CanDrag() 
		{
			ICollection<Linedef> selectedlines = General.Map.Map.GetSelectedLinedefs(true);
			int unaffectedCount = 0;

			foreach(Linedef l in selectedlines) 
			{
				// Make sure the linedef is inside the map boundary
				if(l.Start.Position.x < General.Map.Config.LeftBoundary || l.Start.Position.x > General.Map.Config.RightBoundary
					|| l.Start.Position.y > General.Map.Config.TopBoundary || l.Start.Position.y < General.Map.Config.BottomBoundary
					|| l.End.Position.x < General.Map.Config.LeftBoundary || l.End.Position.x > General.Map.Config.RightBoundary
					|| l.End.Position.y > General.Map.Config.TopBoundary || l.End.Position.y < General.Map.Config.BottomBoundary) 
				{

					l.Selected = false;
					unaffectedCount++;
				}
			}

			if(unaffectedCount == selectedlines.Count) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Unable to drag selection: " + (selectedlines.Count == 1 ? "selected linedef is" : "all of selected linedefs are") + " outside of map boundary!");
				General.Interface.RedrawDisplay();
				return false;
			}

			if(unaffectedCount > 0)
				General.Interface.DisplayStatus(StatusType.Warning, unaffectedCount + " of selected linedefs " + (unaffectedCount == 1 ? "is" : "are") + " outside of map boundary!");

			return true;
		}

		// This is called wheh selection ends
		protected override void OnEndMultiSelection()
		{
			bool selectionvolume = ((Math.Abs(selectionrect.Width) > 0.1f) && (Math.Abs(selectionrect.Height) > 0.1f));
			   
			if(selectionvolume)
			{
				List<Line2D> selectionOutline = new List<Line2D> 
				{
					new Line2D(selectionrect.Left, selectionrect.Top, selectionrect.Right, selectionrect.Top),
					new Line2D(selectionrect.Right, selectionrect.Top, selectionrect.Right, selectionrect.Bottom),
					new Line2D(selectionrect.Left, selectionrect.Bottom, selectionrect.Right, selectionrect.Bottom),
					new Line2D(selectionrect.Left, selectionrect.Bottom, selectionrect.Left, selectionrect.Top)
				};
				
				//mxd
				bool selectthings = (marqueSelectionIncludesThings ^ BuilderPlug.Me.SyncronizeThingEdit);
				switch(marqueSelectionMode) 
				{
					case MarqueSelectionMode.SELECT:
						foreach(Linedef l in General.Map.Map.Linedefs)
							l.Selected = IsInSelectionRect(l, selectionOutline);
						if(selectthings)
						{
							foreach(Thing t in General.Map.ThingsFilter.VisibleThings)
								t.Selected = selectionrect.Contains(t.Position.x, t.Position.y);
						}
						break;

					case MarqueSelectionMode.ADD:
						foreach(Linedef l in General.Map.Map.Linedefs)
							l.Selected |= IsInSelectionRect(l, selectionOutline);
						if(selectthings)
						{
							foreach(Thing t in General.Map.ThingsFilter.VisibleThings)
								t.Selected |= selectionrect.Contains(t.Position.x, t.Position.y);
						}
						break;

					case MarqueSelectionMode.SUBTRACT:
						foreach(Linedef l in General.Map.Map.Linedefs)
							if(IsInSelectionRect(l, selectionOutline)) l.Selected = false;
						if(selectthings)
						{
							foreach(Thing t in General.Map.ThingsFilter.VisibleThings)
								if(selectionrect.Contains(t.Position.x, t.Position.y)) t.Selected = false;
						}
						break;

					default:
						foreach(Linedef l in General.Map.Map.Linedefs)
							if(!IsInSelectionRect(l, selectionOutline)) l.Selected = false;
						if(selectthings)
						{
							foreach(Thing t in General.Map.ThingsFilter.VisibleThings)
								if(!selectionrect.Contains(t.Position.x, t.Position.y)) t.Selected = false;
						}
						break;
				}

				//mxd
				UpdateSelectionInfo();
			}
			
			base.OnEndMultiSelection();

			// Clear overlay
			if(renderer.StartOverlay(true)) renderer.Finish();

			// Redraw
			General.Interface.RedrawDisplay();
		}

		// This is called when the selection is updated
		protected override void OnUpdateMultiSelection()
		{
			base.OnUpdateMultiSelection();

			// Render selection
			if(renderer.StartOverlay(true))
			{
				RenderMultiSelection();
				renderer.Finish();
				renderer.Present();
			}
		}

		// When copying
		public override bool OnCopyBegin()
		{
			// No selection made? But we have a highlight!
			if((General.Map.Map.GetSelectedLinedefs(true).Count == 0) && (highlighted != null))
			{
				// Make the highlight the selection
				highlighted.Selected = true;

				//mxd. Actually, we want it marked, not selected
				bool result = base.OnCopyBegin();
				highlighted.Selected = false;
				return result;
			}

			return base.OnCopyBegin();
		}

		//mxd
		private void RenderComment(Linedef l)
		{
			if(l.Fields.ContainsKey("comment"))
			{
				int iconindex = 0;
				string comment = l.Fields.GetValue("comment", string.Empty);
				if(comment.Length > 2)
				{
					string type = comment.Substring(0, 3);
					int index = Array.IndexOf(CommentType.Types, type);
					if(index != -1) iconindex = index;
				}

				Vector2D center = l.GetCenterPoint();
				RectangleF rect = new RectangleF(center.x - 8 / renderer.Scale, center.y + 18 / renderer.Scale, 16 / renderer.Scale, -16 / renderer.Scale);
				PixelColor c = (l == highlighted ? General.Colors.Highlight : (l.Selected ? General.Colors.Selection : PixelColor.FromColor(Color.White)));
				renderer.RenderRectangleFilled(rect, c, true, General.Map.Data.CommentTextures[iconindex]);
			}
		}

		#endregion

		#region ================== Actions

		// This copies the properties
		[BeginAction("classiccopyproperties")]
		public void CopyProperties()
		{
			// Determine source linedefs
			ICollection<Linedef> sel = null;
			if(General.Map.Map.SelectedLinedefsCount > 0) sel = General.Map.Map.GetSelectedLinedefs(true);
			else if(highlighted != null) sel = new List<Linedef> { highlighted };
			
			if(sel != null)
			{
				// Copy properties from first source linedef
				BuilderPlug.Me.CopiedLinedefProps = new LinedefProperties(General.GetByIndex(sel, 0));
				General.Interface.DisplayStatus(StatusType.Action, "Copied linedef properties.");
			}
			else
			{
				//mxd
				General.Interface.DisplayStatus(StatusType.Warning, "This action requires highlight or selection!");
			}
		}

		// This pastes the properties
		[BeginAction("classicpasteproperties")]
		public void PasteProperties()
		{
			if(BuilderPlug.Me.CopiedLinedefProps != null)
			{
				// Determine target linedefs
				ICollection<Linedef> sel = null;
				if(General.Map.Map.SelectedLinedefsCount > 0) sel = General.Map.Map.GetSelectedLinedefs(true);
				else if(highlighted != null) sel = new List<Linedef> { highlighted };
				
				if(sel != null)
				{
					// Apply properties to selection
					string rest = (sel.Count == 1 ? "a single linedef" : sel.Count + " linedefs"); //mxd
					General.Map.UndoRedo.CreateUndo("Paste properties to " + rest);
					foreach(Linedef l in sel)
					{
						BuilderPlug.Me.CopiedLinedefProps.Apply(l, false);
						l.UpdateCache();
					}
					General.Interface.DisplayStatus(StatusType.Action, "Pasted properties to " + rest + ".");
					
					// Update and redraw
					General.Map.IsChanged = true;
					General.Interface.RefreshInfo();
					General.Map.Renderer2D.UpdateExtraFloorFlag(); //mxd
					General.Interface.RedrawDisplay();
				}
				else
				{
					//mxd
					General.Interface.DisplayStatus(StatusType.Warning, "This action requires highlight or selection!");
				}
			}
			else
			{
				//mxd
				General.Interface.DisplayStatus(StatusType.Warning, "Copy linedef properties first!");
			}
		}

		//mxd. This pastes the properties with options
		[BeginAction("classicpastepropertieswithoptions")]
		public void PastePropertiesWithOptions()
		{
			if(BuilderPlug.Me.CopiedLinedefProps != null)
			{
				// Determine target linedefs
				ICollection<Linedef> sel = null;
				if(General.Map.Map.SelectedLinedefsCount > 0) sel = General.Map.Map.GetSelectedLinedefs(true);
				else if(highlighted != null) sel = new List<Linedef> { highlighted };

				if(sel != null)
				{
					PastePropertiesOptionsForm form = new PastePropertiesOptionsForm();
					if(form.Setup(MapElementType.LINEDEF) && form.ShowDialog(General.Interface) == DialogResult.OK)
					{
						// Apply properties to selection
						string rest = (sel.Count == 1 ? "a single linedef" : sel.Count + " linedefs");
						General.Map.UndoRedo.CreateUndo("Paste properties with options to " + rest);
						foreach(Linedef l in sel)
						{
							BuilderPlug.Me.CopiedLinedefProps.Apply(l, true);
							l.UpdateCache();
						}
						General.Interface.DisplayStatus(StatusType.Action, "Pasted properties with options to " + rest + ".");

						// Update and redraw
						General.Map.IsChanged = true;
						General.Interface.RefreshInfo();
						General.Map.Renderer2D.UpdateExtraFloorFlag(); //mxd
						General.Interface.RedrawDisplay();
					}
				}
				else
				{
					General.Interface.DisplayStatus(StatusType.Warning, "This action requires highlight or selection!");
				}
			}
			else
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Copy linedef properties first!");
			}
		}

		// This keeps only the single-sided lines selected
		[BeginAction("selectsinglesided")]
		public void SelectSingleSided()
		{
			int counter = 0;
			ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);
			foreach(Linedef ld in selected)
			{
				if((ld.Front != null) && (ld.Back != null))
					ld.Selected = false;
				else
					counter++;
			}
			
			General.Interface.DisplayStatus(StatusType.Action, "Selected only single-sided linedefs (" + counter + ")");
			General.Interface.RedrawDisplay();
		}

		// This keeps only the double-sided lines selected
		[BeginAction("selectdoublesided")]
		public void SelectDoubleSided()
		{
			int counter = 0;
			ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);
			foreach(Linedef ld in selected)
			{
				if((ld.Front == null) || (ld.Back == null))
					ld.Selected = false;
				else
					counter++;
			}

			General.Interface.DisplayStatus(StatusType.Action, "Selected only double-sided linedefs (" + counter + ")");
			General.Interface.RedrawDisplay();
		}

		// This clears the selection
		[BeginAction("clearselection", BaseAction = true)]
		public void ClearSelection()
		{
			// Clear selection
			General.Map.Map.ClearAllSelected();

			//mxd. Clear selection info
			General.Interface.DisplayStatus(StatusType.Selection, string.Empty);

			// Redraw
			General.Interface.RedrawDisplay();
		}
		
		// This creates a new vertex at the mouse position
		[BeginAction("insertitem", BaseAction = true)]
		public void InsertVertexAction()
		{
			// Start drawing mode
			DrawGeometryMode drawmode = new DrawGeometryMode();
			if(mouseinside)
			{
				bool snaptogrid = General.Interface.ShiftState ^ General.Interface.SnapToGrid;
				bool snaptonearest = General.Interface.CtrlState ^ General.Interface.AutoMerge;
				DrawnVertex v = DrawGeometryMode.GetCurrentPosition(mousemappos, snaptonearest, snaptogrid, false, renderer, new List<DrawnVertex>());
				drawmode.DrawPointAt(v);
			}
			General.Editing.ChangeMode(drawmode);
		}

		[BeginAction("deleteitem", BaseAction = true)]
		public void DeleteItem() 
		{
			// Make list of selected linedefs
			ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);
			if((selected.Count == 0) && (highlighted != null) && !highlighted.IsDisposed) selected.Add(highlighted);
			if(selected.Count == 0) return;

			// Make undo
			if(selected.Count > 1) 
			{
				General.Map.UndoRedo.CreateUndo("Delete " + selected.Count + " linedefs");
				General.Interface.DisplayStatus(StatusType.Action, "Deleted " + selected.Count + " linedefs.");
			} 
			else 
			{
				General.Map.UndoRedo.CreateUndo("Delete linedef");
				General.Interface.DisplayStatus(StatusType.Action, "Deleted a linedef.");
			}

			// Dispose selected linedefs
			foreach(Linedef ld in selected) ld.Dispose();

			// Update cache values
			General.Map.IsChanged = true;
			General.Map.Map.Update();

			// Invoke a new mousemove so that the highlighted item updates
			MouseEventArgs e = new MouseEventArgs(MouseButtons.None, 0, (int)mousepos.x, (int)mousepos.y, 0);
			OnMouseMove(e);

			// Redraw screen
			UpdateSelectionInfo(); //mxd
			General.Map.Renderer2D.UpdateExtraFloorFlag(); //mxd
			General.Interface.RedrawDisplay();
		}

		[BeginAction("dissolveitem", BaseAction = true)] //mxd
		public void DissolveItem()
		{
			// Make list of selected linedefs
			ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);
			if((selected.Count == 0) && (highlighted != null) && !highlighted.IsDisposed) selected.Add(highlighted);

			// Anything to do?
			if(selected.Count > 0)
			{
				// Make undo
				if(selected.Count > 1)
				{
					General.Map.UndoRedo.CreateUndo("Dissolve " + selected.Count + " linedefs");
					General.Interface.DisplayStatus(StatusType.Action, "Dissolved " + selected.Count + " linedefs.");
				}
				else
				{
					General.Map.UndoRedo.CreateUndo("Dissolve linedef");
					General.Interface.DisplayStatus(StatusType.Action, "Dissolved a linedef.");
				}

				//mxd. Find sectors, which will become invalid after linedefs removal.
				Dictionary<Sector, Vector2D> toMerge = new Dictionary<Sector, Vector2D>();

				foreach(Linedef l in selected) 
				{
					if(l.Front != null && l.Front.Sector.Sidedefs.Count < 4 && !toMerge.ContainsKey(l.Front.Sector))
						toMerge.Add(l.Front.Sector, new Vector2D(l.Front.Sector.BBox.Location.X + l.Front.Sector.BBox.Width / 2, l.Front.Sector.BBox.Location.Y + l.Front.Sector.BBox.Height / 2));

					if(l.Back != null && l.Back.Sector.Sidedefs.Count < 4 && !toMerge.ContainsKey(l.Back.Sector))
						toMerge.Add(l.Back.Sector, new Vector2D(l.Back.Sector.BBox.Location.X + l.Back.Sector.BBox.Width / 2, l.Back.Sector.BBox.Location.Y + l.Back.Sector.BBox.Height / 2));
				}

				General.Map.Map.BeginAddRemove(); //mxd
				
				// Dispose selected linedefs
				foreach(Linedef ld in selected) 
				{
					//mxd. If there are different sectors on both sides, join them
					if(ld.Front != null && ld.Front.Sector != null && ld.Back != null 
						&& ld.Back.Sector != null && ld.Front.Sector.Index != ld.Back.Sector.Index) 
					{
						if(ld.Front.Sector.BBox.Width * ld.Front.Sector.BBox.Height > ld.Back.Sector.BBox.Width * ld.Back.Sector.BBox.Height)
							ld.Back.Sector.Join(ld.Front.Sector);
						else
							ld.Front.Sector.Join(ld.Back.Sector);
					}

					ld.Dispose();
				}

				//mxd
				General.Map.Map.EndAddRemove();
				Tools.MergeInvalidSectors(toMerge);
				
				// Update cache values
				General.Map.IsChanged = true;
				General.Map.Map.Update();

				// Invoke a new mousemove so that the highlighted item updates
				MouseEventArgs e = new MouseEventArgs(MouseButtons.None, 0, (int)mousepos.x, (int)mousepos.y, 0);
				OnMouseMove(e);
				
				// Redraw screen
				UpdateSelectionInfo(); //mxd
				General.Map.Renderer2D.UpdateExtraFloorFlag(); //mxd
				General.Interface.RedrawDisplay();
			}
		}
		
		[BeginAction("splitlinedefs")]
		public void SplitLinedefs()
		{
			// Make list of selected linedefs
			ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);
			if((selected.Count == 0) && (highlighted != null) && !highlighted.IsDisposed) selected.Add(highlighted);

			// Anything to do?
			if(selected.Count > 0)
			{
				// Make undo
				if(selected.Count > 1)
				{
					General.Map.UndoRedo.CreateUndo("Split " + selected.Count + " linedefs");
					General.Interface.DisplayStatus(StatusType.Action, "Split " + selected.Count + " linedefs.");
				}
				else
				{
					General.Map.UndoRedo.CreateUndo("Split linedef");
					General.Interface.DisplayStatus(StatusType.Action, "Split a linedef.");
				}
				
				// Go for all linedefs to split
				foreach(Linedef ld in selected)
				{
					Vertex splitvertex;

					// Linedef highlighted?
					if(ld == highlighted)
					{
						// Split at nearest position on the line
						Vector2D nearestpos = ld.NearestOnLine(mousemappos);
						splitvertex = General.Map.Map.CreateVertex(nearestpos);
					}
					else
					{
						// Split in middle of line
						splitvertex = General.Map.Map.CreateVertex(ld.GetCenterPoint());
					}

					if(splitvertex == null)
					{
						General.Map.UndoRedo.WithdrawUndo();
						break;
					}
					
					// Snap to map format accuracy
					splitvertex.SnapToAccuracy();

					// Split the line
					Linedef sld = ld.Split(splitvertex);
					if(sld == null)
					{
						General.Map.UndoRedo.WithdrawUndo();
						break;
					}
					//BuilderPlug.Me.AdjustSplitCoordinates(ld, sld);
				}

				// Update cache values
				General.Map.IsChanged = true;
				General.Map.Map.Update();
				
				// Redraw screen
				General.Interface.RedrawDisplay();
			}
		}
		
		[BeginAction("curvelinesmode")]
		public void CurveLinedefs()
		{
			// No selected lines?
			ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);
			if(selected.Count == 0)
			{
				// Anything highlighted?
				if(highlighted != null)
				{
					// Select the highlighted item
					highlighted.Selected = true;
					selected.Add(highlighted);
				}
			}

			// Any selected lines?
			if(selected.Count > 0)
			{
				// Go into curve linedefs mode
				General.Editing.ChangeMode(new CurveLinedefsMode(new LinedefsMode()));
			}
		}
		
		[BeginAction("fliplinedefs")]
		public void FlipLinedefs()
		{
			// No selected lines?
			ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);
			if(selected.Count == 0)
			{
				// Anything highlighted?
				if(highlighted != null)
				{
					// Select the highlighted item
					highlighted.Selected = true;
					selected.Add(highlighted);
				}
			}

			// Any selected lines?
			if(selected.Count > 0)
			{
				// Make undo
				if(selected.Count > 1)
				{
					General.Map.UndoRedo.CreateUndo("Flip " + selected.Count + " linedefs");
					General.Interface.DisplayStatus(StatusType.Action, "Flipped " + selected.Count + " linedefs.");
				}
				else
				{
					General.Map.UndoRedo.CreateUndo("Flip linedef");
					General.Interface.DisplayStatus(StatusType.Action, "Flipped a linedef.");
				}

				//mxd. Do it sector-wise
				Dictionary<Sector, int> sectors = new Dictionary<Sector, int>();

				foreach(Linedef l in selected) 
				{
					if(l.Front != null && l.Front.Sector != null)
					{
						if(!sectors.ContainsKey(l.Front.Sector)) sectors.Add(l.Front.Sector, 0);
						sectors[l.Front.Sector]++;
					}
						
					if(l.Back != null && l.Back.Sector != null)
					{
						if(!sectors.ContainsKey(l.Back.Sector)) sectors.Add(l.Back.Sector, 0);
						sectors[l.Back.Sector]++;
					}
				}

				//mxd. Sort the collection so sectors with the most selected linedefs go first
				List<KeyValuePair<Sector, int>> sortedlist = sectors.ToList();
				sortedlist.Sort((firstPair, nextPair) => firstPair.Value.CompareTo(nextPair.Value));
				sortedlist.Reverse();

				//mxd. Gather our ordered sectors
				List<Sector> sectorslist = new List<Sector>(sortedlist.Count());
				sectorslist.AddRange(sortedlist.Select(pair => pair.Key));

				//mxd. Flip the lines
				Tools.FlipSectorLinedefs(sectorslist, true);

				// Remove selection if only one linedef was selected
				if(selected.Count == 1)
				{
					foreach(Linedef ld in selected) ld.Selected = false;
					selected.Clear();
				}

				// Redraw
				General.Map.Map.Update();
				General.Map.IsChanged = true;
				General.Interface.RefreshInfo();
				General.Interface.RedrawDisplay();
			}
		}

		[BeginAction("flipsidedefs")]
		public void FlipSidedefs()
		{
			// No selected lines?
			ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);
			if(selected.Count == 0)
			{
				// Anything highlighted?
				if(highlighted != null)
				{
					selected.Add(highlighted);
				}
			}

			//mxd. Do this only with double-sided linedefs
			List<Linedef> validlines = new List<Linedef>();
			foreach(Linedef l in selected) 
			{
				if(l.Front != null && l.Back != null) validlines.Add(l);
			}

			// Any selected lines?
			if(validlines.Count > 0) 
			{
				// Make undo
				if(validlines.Count > 1) 
				{
					General.Map.UndoRedo.CreateUndo("Flip " + validlines.Count + " sidedefs");
					General.Interface.DisplayStatus(StatusType.Action, "Flipped " + validlines.Count + " sidedefs.");
				}
				else
				{
					General.Map.UndoRedo.CreateUndo("Flip sidedef");
					General.Interface.DisplayStatus(StatusType.Action, "Flipped a sidedef.");
				}

				// Flip sidedefs in all selected linedefs
				foreach(Linedef l in validlines) 
				{
					l.FlipSidedefs();
					l.Front.Sector.UpdateNeeded = true;
					l.Back.Sector.UpdateNeeded = true;
				}

				// Redraw
				General.Map.Map.Update();
				General.Map.IsChanged = true;
				General.Interface.RefreshInfo();
				General.Interface.RedrawDisplay();
			} 
			else 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "No sidedefs to flip (only 2-sided linedefs can be flipped)!");
			}
		}

		//mxd. Make gradient brightness
		[BeginAction("gradientbrightness")]
		public void MakeGradientBrightness() 
		{
			if(!General.Map.UDMF) return;

			// Need at least 3 selected linedefs
			// The first and last are not modified
			ICollection<Linedef> orderedselection = General.Map.Map.GetSelectedLinedefs(true);
			if(orderedselection.Count > 2) 
			{
				// Gather values
				Linedef start = General.GetByIndex(orderedselection, 0);
				Linedef end = General.GetByIndex(orderedselection, orderedselection.Count - 1);

				float startbrightness = GetLinedefBrighness(start);
				float endbrightness = GetLinedefBrighness(end);

				if(float.IsNaN(startbrightness))
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Start linedef doesn't have visible parts!");
					return;
				}

				if(float.IsNaN(endbrightness)) 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "End linedef doesn't have visible parts!");
					return;
				}

				//Make undo
				General.Interface.DisplayStatus(StatusType.Action, "Created gradient brightness over selected linedefs.");
				General.Map.UndoRedo.CreateUndo("Linedefs gradient brightness");

				// Apply changes
				InterpolationTools.Mode interpolationmode = (InterpolationTools.Mode)BuilderPlug.Me.MenusForm.GradientInterpolationMenu.SelectedIndex;
				int index = 0;

				// Go for all lines in between first and last
				foreach(Linedef l in orderedselection) 
				{
					float u = index / (float)(orderedselection.Count - 1);
					int b = InterpolationTools.Interpolate(startbrightness, endbrightness, u, interpolationmode);
					if(SidedefHasVisibleParts(l.Front)) ApplySidedefBrighness(l.Front, b);
					if(SidedefHasVisibleParts(l.Back)) ApplySidedefBrighness(l.Back, b);
					index++;
				}

				// Update
				General.Map.Map.Update();
				General.Interface.RedrawDisplay();
				General.Interface.RefreshInfo();
				General.Map.IsChanged = true;
			} 
			else 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Select at least 3 linedefs first!");
			}
		}

		//mxd. gradientbrightness utility
		private static bool SidedefHasVisibleParts(Sidedef side) 
		{
			if(side == null || side.Sector == null) return false;
			return side.HighRequired() || side.LowRequired() || (side.MiddleRequired() || (side.Other != null && side.MiddleTexture != "-"));
		}

		//mxd. gradientbrightness utility
		private static float GetLinedefBrighness(Linedef line) 
		{
			float frontbrightness = (SidedefHasVisibleParts(line.Front) ? GetSidedefBrighness(line.Front) : float.NaN);
			float backbrightness = (SidedefHasVisibleParts(line.Back) ? GetSidedefBrighness(line.Back) : float.NaN);

			if(float.IsNaN(frontbrightness) && float.IsNaN(backbrightness)) return float.NaN;
			if(float.IsNaN(frontbrightness)) return backbrightness;
			if(float.IsNaN(backbrightness)) return frontbrightness;
			return (frontbrightness + backbrightness) / 2;
		}

		//mxd. gradientbrightness utility
		private static float GetSidedefBrighness(Sidedef side) 
		{
			if(side.Fields.GetValue("lightabsolute", false)) return side.Fields.GetValue("light", 0);
			return Math.Min(255, Math.Max(0, (float)side.Sector.Brightness + side.Fields.GetValue("light", 0)));
		}

		//mxd. gradientbrightness utility
		private static void ApplySidedefBrighness(Sidedef side, int brightness) 
		{
			side.Fields.BeforeFieldsChange();

			//absolute flag set?
			if(side.Fields.GetValue("lightabsolute", false))
				side.Fields["light"].Value = brightness;
			else
				UniFields.SetInteger(side.Fields, "light", brightness - side.Sector.Brightness, 0);

			//apply lightfog flag?
			if(General.Map.UDMF) Tools.UpdateLightFogFlag(side);
		}

		[BeginAction("placethings")] //mxd
		public void PlaceThings() 
		{
			// Make list of selected linedefs
			ICollection<Linedef> lines = General.Map.Map.GetSelectedLinedefs(true);
			List<Vector2D> positions = new List<Vector2D>();

			if(lines.Count == 0) 
			{
				if(highlighted != null && !highlighted.IsDisposed) 
				{
					lines.Add(highlighted);
				} 
				else 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "This action requires selection of some description!");
					return;
				}
			}

			// Make list of vertex positions
			foreach(Linedef l in lines) 
			{
				if(!positions.Contains(l.Start.Position)) positions.Add(l.Start.Position);
				if(!positions.Contains(l.End.Position)) positions.Add(l.End.Position);
			}

			if(positions.Count < 1) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Unable to get vertex positions from selection!");
				return;
			}

			PlaceThingsAtPositions(positions);
		}

		//mxd
		[BeginAction("alignfloortofront")]
		public void AlignFloorToFront() 
		{
			if(!General.Map.UDMF) return;
			AlignTextureToLine(true, true);
		}

		//mxd
		[BeginAction("alignfloortoback")]
		public void AlignFloorToBack() 
		{
			if(!General.Map.UDMF) return;
			AlignTextureToLine(true, false);
		}

		//mxd
		[BeginAction("alignceilingtofront")]
		public void AlignCeilingToFront() 
		{
			if(!General.Map.UDMF) return;
			AlignTextureToLine(false, true);
		}

		//mxd
		[BeginAction("alignceilingtoback")]
		public void AlignCeilingToBack() 
		{
			if(!General.Map.UDMF) return;
			AlignTextureToLine(false, false);
		}

		//mxd
		[BeginAction("selectsimilar")]
		public void SelectSimilar() 
		{
			ICollection<Linedef> selection = General.Map.Map.GetSelectedLinedefs(true);

			if(selection.Count == 0) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "This action requires a selection!");
				return;
			}

			var form = new SelectSimilarElementOptionsPanel();
			if(form.Setup(this)) form.ShowDialog();
		}

		//mxd
		[BeginAction("applylightfogflag")]
		private void ApplyLightFogFlag() 
		{
			if(!General.Map.UDMF)return;

			// Make list of selected linedefs
			ICollection<Linedef> lines = General.Map.Map.GetSelectedLinedefs(true);

			if(lines.Count == 0) 
			{
				if(highlighted != null && !highlighted.IsDisposed) 
				{
					lines.Add(highlighted);
				} 
				else 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "This action requires selection of some description!");
					return;
				}
			}

			// Make undo
			General.Map.UndoRedo.CreateUndo("Apply 'lightfog' flag");

			// Apply the flag
			int addedcout = 0;
			int removedcount = 0;
			foreach(Linedef l in lines)
			{
				if(l.Front != null)
				{
					int result = Tools.UpdateLightFogFlag(l.Front);
					switch(result)
					{
						case 1: addedcout++; break;
						case -1: removedcount++; break;
					}
				}
				if(l.Back != null)
				{
					int result = Tools.UpdateLightFogFlag(l.Back);
					switch(result) 
					{
						case 1: addedcout++; break;
						case -1: removedcount++; break;
					}
				}
			}

			// Display info
			General.Interface.DisplayStatus(StatusType.Action, "Added 'lightfog' flag to " + addedcout + " sidedefs, removed it from " + removedcount + " sidedefs.");
		}

		//JBR Perpendicular Vertex
		[BeginAction("perpendicularvertex")]
		public void PerpendicularVertex()
		{
			ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);

			// Nothing selected?
			if (selected.Count == 0)
			{
				// Anything highlighted?
				if (highlighted != null)
				{
					// Select the highlighted item
					highlighted.Selected = true;
					selected.Add(highlighted);
				}
			}

			// Anything selected?
			if (selected.Count > 0)
				General.Editing.ChangeMode(new PerpendicularVertexMode(new LinedefsMode()));
			else
				General.Interface.DisplayStatus(StatusType.Warning, "This action requires a selection!");
		}

		//JBR Perpendicular Linedef
		[BeginAction("perpendicularlinedef")]
		public void PerpendicularLinedef()
		{
			ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);

			// Nothing selected?
			if (selected.Count == 0)
			{
				// Anything highlighted?
				if (highlighted != null)
				{
					// Select the highlighted item
					highlighted.Selected = true;
					selected.Add(highlighted);
				}
			}

			// Anything selected?
			if (selected.Count > 0)
				General.Editing.ChangeMode(new PerpendicularLinedefMode(new LinedefsMode()));
			else
				General.Interface.DisplayStatus(StatusType.Warning, "This action requires a selection!");
		}

		//JBR Parallel Linedef
		[BeginAction("parallellinedef")]
		public void ParallelLinedef()
		{
			ICollection<Linedef> selected = General.Map.Map.GetSelectedLinedefs(true);

			// Nothing selected?
			if (selected.Count == 0)
			{
				// Anything highlighted?
				if (highlighted != null)
				{
					// Select the highlighted item
					highlighted.Selected = true;
					selected.Add(highlighted);
				}
			}

			// Anything selected?
			if (selected.Count > 0)
				General.Editing.ChangeMode(new ParallelLinedefMode(new LinedefsMode()));
			else
				General.Interface.DisplayStatus(StatusType.Warning, "This action requires a selection!");
		}

		#endregion
	}
}
