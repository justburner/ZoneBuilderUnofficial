
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
using System.Windows.Forms;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Actions;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Insert Things Radially Mode",
			  SwitchAction = "insertthingsradiallymode",
			  ButtonImage = "InsertThingsRadiallyMode.png",	
			  ButtonOrder = int.MinValue + 7,
			  ButtonGroup = "000_drawing",
			  AllowCopyPaste = false,
			  Volatile = true,
			  UseByDefault = true,
			  Optional = false)]

	public class InsertThingsRadiallyMode : BaseClassicMode
	{
        #region ================== Constants
        #endregion

        #region ================== Variables

        protected List<Vector2D> things;

        // Options
        protected int number;
        protected int radius;
		protected bool snaptogrid;		// SHIFT to toggle
        protected int type;
        protected int parameter;

        //interface
        private InsertThingsRadiallyOptionsPanel panel;

        #endregion

        #region ================== Properties

        #endregion

        #region ================== Constructor / Disposer

        // Constructor
        public InsertThingsRadiallyMode()
		{
            things = new List<Vector2D>();

			// No selection in this mode
			General.Map.Map.ClearAllSelected();
			General.Map.Map.ClearAllMarks(false);

            SetupInterface();
			
			// We have no destructor
			GC.SuppressFinalize(this);
		}

		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{			
				// Done
				base.Dispose();
			}
		}

		#endregion

		#region ================== Methods
		
		// Update the thing markers
		protected virtual void Update()
		{
			snaptogrid = General.Interface.ShiftState ^ panel.SnapToGrid;

            UpdateThings();

			// Render things
			if(renderer.StartOverlay(true))
			{
                float vsize = (renderer.VertexSize + 1.0f) / renderer.Scale;

                foreach (Vector2D t in things)
                    renderer.RenderRectangleFilled(new RectangleF(t.x - vsize, t.y - vsize, vsize * 2.0f, vsize * 2.0f), General.Colors.Selection, true);

                // Done
                renderer.Finish();
			}

			// Done
			renderer.Present();
		}
		
		// Update the positions of the thing markers
		public void UpdateThings()
		{
            things = new List<Vector2D>();
            for (int i = 0; i < number; i++)
            {
                float fAngle = i * 2 * (float)Math.PI / number;
                Vector2D pos = mousemappos + new Vector2D(((float)Math.Cos(fAngle) * radius), ((float)Math.Sin(fAngle) * radius));
                if (snaptogrid) pos = General.Map.Grid.SnappedToGrid(pos);
                if (pos.x < General.Map.Config.LeftBoundary) pos.x = General.Map.Config.LeftBoundary;
                if (pos.x > General.Map.Config.RightBoundary) pos.x = General.Map.Config.RightBoundary;
                if (pos.y < General.Map.Config.BottomBoundary) pos.y = General.Map.Config.BottomBoundary;
                if (pos.y > General.Map.Config.TopBoundary) pos.y = General.Map.Config.TopBoundary;
                things.Add(pos);
            }
		}	
		#endregion

		#region ================== Events

		// Engaging
		public override void OnEngage()
		{
			base.OnEngage();
			EnableAutoPanning();
			renderer.SetPresentation(Presentation.Standard);
			
			// Set cursor
			General.Interface.SetCursor(Cursors.Cross);
            AddInterface();
		}

		// Disengaging
		public override void OnDisengage()
		{
			base.OnDisengage();
			DisableAutoPanning();
            RemoveInterface();
		}
		
		// Cancelled
		public override void OnCancel()
		{
			// Cancel base class
			base.OnCancel();
			
			// Return to original mode
			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// Accepted
		public override void OnAccept()
		{
			Cursor.Current = Cursors.AppStarting;
			General.Settings.FindDefaultDrawSettings();

            General.Map.UndoRedo.CreateUndo("Insert things");
            General.Interface.DisplayStatus(StatusType.Action, "Inserted things radially.");
            foreach (Vector2D pos in things)
            {
                Thing t = General.Map.Map.CreateThing();
                if (t != null)
                {
                    General.Settings.ApplyDefaultThingSettings(t);
                    t.SRB2Type = type;
                    t.Parameter = parameter;
                    t.Move(pos);
                    t.UpdateConfiguration();
                }
            }

            //Update Things filter
            General.Map.ThingsFilter.Update();

            // Snap to map format accuracy
            General.Map.Map.SnapAllToAccuracy();

            // Update cached values
            General.Map.Map.Update();

            // Map is changed
            General.Map.IsChanged = true;

			// Done
			Cursor.Current = Cursors.Default;
			
			// Return to original mode
			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// This redraws the display
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

			// Normal update
			Update();
		}
		
		// Mouse moving
		public override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if(panning) return; //mxd. Skip all this jazz while panning
			Update();
		}

		// When a key is released
		public override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			if(snaptogrid != (General.Interface.ShiftState ^ panel.SnapToGrid)) Update();
		}

		// When a key is pressed
		public override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if(snaptogrid != (General.Interface.ShiftState ^ panel.SnapToGrid)) Update();
		}

        public override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            // Mouse inside window?
            if (General.Interface.MouseInDisplay)
            {
                Update();
                General.Editing.AcceptMode();
            }
        }

        private void OptionsPanelOnValueChanged(object sender, EventArgs eventArgs)
        {
            number = panel.Number;
            radius = panel.Radius;
            snaptogrid = panel.SnapToGrid;
            type = panel.Type;
            parameter = panel.Parameter;
            Update();
        }

        #endregion

        #region ================== Settings panel

        protected virtual void SetupInterface()
        {
            //Add options docker
            panel = new InsertThingsRadiallyOptionsPanel();
            panel.OnValueChanged += OptionsPanelOnValueChanged;
        }

        protected virtual void AddInterface()
        {
            panel.Register();
            number = panel.Number;
            radius = panel.Radius;
            snaptogrid = panel.SnapToGrid;
            type = panel.Type;
            parameter = panel.Parameter;
        }

        protected virtual void RemoveInterface()
        {
            panel.Unregister();
        }

        #endregion

        #region ================== Actions

        [BeginAction("increasenumber")]
        protected void IncreaseNumber()
        {
            if (number < panel.MaxNumber)
            {
                number++;
                panel.Number = number;
                Update();
            }
        }

        [BeginAction("decreasenumber")]
        protected void DecreaseNumber()
        {
            if (number > panel.MinNumber)
            {
                number--;
                panel.Number = number;
                Update();
            }
        }

        [BeginAction("increaseradius")]
        protected void IncreaseRadius()
        {
            if (radius < panel.MaxRadius)
            {
                radius = Math.Min(radius + General.Map.Grid.GridSize, panel.MaxRadius);
                panel.Radius = radius;
                Update();
            }
        }

        [BeginAction("decreaseradius")]
        protected void DecreaseRadius()
        {
            if (radius > panel.MinRadius)
            {
                radius = Math.Max(radius - General.Map.Grid.GridSize, panel.MinRadius);
                panel.Radius = radius;
                Update();
            }
        }

        #endregion
    }
}
