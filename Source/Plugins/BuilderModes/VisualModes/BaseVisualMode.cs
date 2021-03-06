
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
using CodeImp.DoomBuilder.BuilderModes.Interface;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.VisualModes;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.GZBuilder.Data;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.Data;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Visual Mode",
			  SwitchAction = "gzdbvisualmode", // Action name used to switch to this mode
			  ButtonImage = "VisualMode.png",	// Image resource name for the button
			  ButtonOrder = 1,					// Position of the button (lower is more to the left)
			  ButtonGroup = "001_visual",
			  UseByDefault = true)]

	public class BaseVisualMode : VisualMode
	{
		#region ================== Constants
		// Object picking
		private const float PICK_INTERVAL = 80.0f;
		private const float PICK_RANGE = 0.98f;

		// Gravity
		private const float GRAVITY = -0.06f;
		
		#endregion
		
		#region ================== Variables

		// Gravity
		private Vector3D gravity;
		private float cameraflooroffset = 41f;		// same as in doom
		private float cameraceilingoffset = 10f;
		
		// Object picking
		private VisualPickResult target;
		private float lastpicktime;
		private bool locktarget;
		private bool useSelectionFromClassicMode;//mxd
		private readonly Timer selectioninfoupdatetimer; //mxd

		// This keeps extra element info
		private Dictionary<Sector, SectorData> sectordata;
		private Dictionary<Thing, ThingData> thingdata;
		private Dictionary<Vertex, VertexData> vertexdata; //mxd
		//private Dictionary<Thing, EffectDynamicLight> lightdata; //mxd
		
		// This is true when a selection was made because the action is performed
		// on an object that was not selected. In this case the previous selection
		// is cleared and the targeted object is temporarely selected to perform
		// the action on. After the action is completed, the object is deselected.
		private bool singleselection;
		
		// We keep these to determine if we need to make a new undo level
		private bool selectionchanged;
		private int lastundogroup;
		private VisualActionResult actionresult;
		private bool undocreated;

		// List of selected objects when an action is performed
		private List<IVisualEventReceiver> selectedobjects;
		
		//mxd. Used in Cut/PasteSelection actions
		private readonly List<ThingCopyData> copybuffer;
		private Type lasthighlighttype;

        private BSP bsp;
        private bool useblockmap;

        //mxd. Moved here from Tools
        private struct SidedefAlignJob
		{
			public Sidedef sidedef;

			public float offsetx;
			public float scaleX; //mxd
			public float scaleY; //mxd

			private Sidedef controlside; //mxd
			public Sidedef controlSide
			{
				get
				{
					return controlside;
				}
				set
				{
					controlside = value;
					ceilingheight = (controlside.Index != sidedef.Index && controlside.Line.Args[1] == 0 ? controlside.Sector.FloorHeight : controlside.Sector.CeilHeight);
				}
			}

			private int ceilingheight; //mxd
			public int ceilingHeight { get { return ceilingheight; } } //mxd

			// When this is true, the previous sidedef was on the left of
			// this one and the texture X offset of this sidedef can be set
			// directly. When this is false, the length of this sidedef
			// must be subtracted from the X offset first.
			public bool forward;
		}
		
		#endregion
		
		#region ================== Properties

		public override object HighlightedObject
		{
			get
			{
				// Geometry picked?
				if(target.picked is VisualGeometry)
				{
					VisualGeometry pickedgeo = (target.picked as VisualGeometry);

					if(pickedgeo.Sidedef != null) return pickedgeo.Sidedef;
					if(pickedgeo.Sector != null) return pickedgeo.Sector;
					return null;
				}
				// Thing picked?
				if(target.picked is VisualThing)
				{
					VisualThing pickedthing = (target.picked as VisualThing);
					return pickedthing.Thing;
				}

				return null;
			}
		}

		public object HighlightedTarget { get { return target.picked; } } //mxd
		public bool UseSelectionFromClassicMode { get { return useSelectionFromClassicMode; } } //mxd

		new public IRenderer3D Renderer { get { return renderer; } }
		
		public bool IsSingleSelection { get { return singleselection; } }
		public bool SelectionChanged { get { return selectionchanged; } set { selectionchanged |= value; } }
        public BSP BSP { get { return bsp; } }
        public bool UseBlockmap { get { return useblockmap; } }

        #endregion

        #region ================== Constructor / Disposer

        // Constructor
        public BaseVisualMode()
		{
			// Initialize
			this.gravity = new Vector3D(0.0f, 0.0f, 0.0f);
			this.selectedobjects = new List<IVisualEventReceiver>();
			
			//mxd
			this.copybuffer = new List<ThingCopyData>();
			this.selectioninfoupdatetimer = new Timer();
			selectioninfoupdatetimer.Interval = 100;
			selectioninfoupdatetimer.Tick += SelectioninfoupdatetimerOnTick;

            bsp = new BSP(BuilderPlug.Me.DontUseNodes);
            useblockmap = bsp.IsDeactivated;
            if (useblockmap && !String.IsNullOrEmpty(bsp.ErrorMessage))
                MessageBox.Show("Could not load the map's nodes: " + bsp.ErrorMessage + " Defaulting to blockmap.", "Error loading nodes!", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // We have no destructor
            GC.SuppressFinalize(this);
		}

		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
                // Clean up
                bsp.Dispose();
                bsp = null;

                // Done
                base.Dispose();
			}
		}

		#endregion
		
		#region ================== Methods

		// This calculates brightness level
		internal int CalculateBrightness(int level)
		{
			return renderer.CalculateBrightness(level);
		}

		//mxd. This calculates brightness level with doom-style shading
		internal int CalculateBrightness(int level, Sidedef sd) 
		{
			return renderer.CalculateBrightness(level, sd);
		}
		
		// This adds a selected object
		internal void AddSelectedObject(IVisualEventReceiver obj)
		{
			selectedobjects.Add(obj);
			selectionchanged = true;
			selectioninfoupdatetimer.Start(); //mxd
		}
		
		// This removes a selected object
		internal void RemoveSelectedObject(IVisualEventReceiver obj)
		{
			selectedobjects.Remove(obj);
			selectionchanged = true;
			selectioninfoupdatetimer.Start(); //mxd
		}
		
		// This is called before an action is performed
		public void PreAction(int multiselectionundogroup)
		{
			actionresult = new VisualActionResult();
			
			PickTargetUnlocked();
			
			// If the action is not performed on a selected object, clear the
			// current selection and make a temporary selection for the target.
			if((target.picked != null) && !target.picked.Selected && (BuilderPlug.Me.VisualModeClearSelection || (selectedobjects.Count == 0)))
			{
				// Single object, no selection
				singleselection = true;
				ClearSelection();
				undocreated = false;
			}
			else
			{
				singleselection = false;
				
				// Check if we should make a new undo level
				// We don't want to do this if this is the same action with the same
				// selection and the action wants to group the undo levels
				if((lastundogroup != multiselectionundogroup) || (lastundogroup == UndoGroup.None) ||
				   (multiselectionundogroup == UndoGroup.None) || selectionchanged)
				{
					// We want to create a new undo level, but not just yet
					lastundogroup = multiselectionundogroup;
					undocreated = false;
				}
				else
				{
					// We don't want to make a new undo level (changes will be combined)
					undocreated = true;
				}
			}
		}

		// Called before an action is performed. This does not make an undo level
		private void PreActionNoChange()
		{
			actionresult = new VisualActionResult();
			singleselection = false;
			undocreated = false;
		}
		
		// This is called after an action is performed
		private void PostAction()
		{
			if(!string.IsNullOrEmpty(actionresult.displaystatus))
				General.Interface.DisplayStatus(StatusType.Action, actionresult.displaystatus);

			// Reset changed flags
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
			{
				BaseVisualSector bvs = (vs.Value as BaseVisualSector);
				foreach(VisualFloor vf in bvs.ExtraFloors) vf.Changed = false;
				foreach(VisualCeiling vc in bvs.ExtraCeilings) vc.Changed = false;
				foreach(VisualFloor vf in bvs.ExtraBackFloors) vf.Changed = false; //mxd
				foreach(VisualCeiling vc in bvs.ExtraBackCeilings) vc.Changed = false; //mxd
				bvs.Floor.Changed = false;
				bvs.Ceiling.Changed = false;
			}
			
			selectionchanged = false;
			
			if(singleselection) ClearSelection();
			
			UpdateChangedObjects();
			ShowTargetInfo();
		}
		
		// This sets the result for an action
		public void SetActionResult(VisualActionResult result)
		{
			actionresult = result;
		}

		// This sets the result for an action
		public void SetActionResult(string displaystatus)
		{
			actionresult = new VisualActionResult {displaystatus = displaystatus};
		}
		
		// This creates an undo, when only a single selection is made
		// When a multi-selection is made, the undo is created by the PreAction function
		public int CreateUndo(string description, int group, int grouptag)
		{
			if(!undocreated)
			{
				undocreated = true;

				if(singleselection)
					return General.Map.UndoRedo.CreateUndo(description, this, group, grouptag);
				return General.Map.UndoRedo.CreateUndo(description, this, UndoGroup.None, 0);
			}

			return 0;
		}

		// This creates an undo, when only a single selection is made
		// When a multi-selection is made, the undo is created by the PreAction function
		public int CreateUndo(string description)
		{
			return CreateUndo(description, UndoGroup.None, 0);
		}

		// This makes a list of the selected object
		private void RebuildSelectedObjectsList()
		{
			// Make list of selected objects
			selectedobjects = new List<IVisualEventReceiver>();
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
			{
				if(vs.Value != null)
				{
					BaseVisualSector bvs = vs.Value as BaseVisualSector;
					if((bvs.Floor != null) && bvs.Floor.Selected) selectedobjects.Add(bvs.Floor);
					if((bvs.Ceiling != null) && bvs.Ceiling.Selected) selectedobjects.Add(bvs.Ceiling);
					foreach(Sidedef sd in vs.Key.Sidedefs)
					{
						List<VisualGeometry> sidedefgeos = bvs.GetSidedefGeometry(sd);
						foreach(VisualGeometry sdg in sidedefgeos)
						{
							if(sdg.Selected) selectedobjects.Add((sdg as IVisualEventReceiver));
						}
					}
				}
			}

			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
			{
				if(vt.Value != null)
				{
					BaseVisualThing bvt = vt.Value as BaseVisualThing;
					if(bvt.Selected) selectedobjects.Add(bvt);
				}
			}

			//mxd
			if(General.Map.UDMF && General.Settings.GZShowVisualVertices) 
			{
				foreach(KeyValuePair<Vertex, VisualVertexPair> pair in vertices) 
				{
					if(pair.Value.CeilingVertex.Selected)
						selectedobjects.Add((BaseVisualVertex)pair.Value.CeilingVertex);
					if(pair.Value.FloorVertex.Selected)
						selectedobjects.Add((BaseVisualVertex)pair.Value.FloorVertex);
				}
			}

			//mxd
			UpdateSelectionInfo();
		}

		//mxd. Need this to apply changes to 3d-floor even if control sector doesn't exist as BaseVisualSector
		internal BaseVisualSector CreateBaseVisualSector(Sector s) 
		{
			BaseVisualSector vs = new BaseVisualSector(this, s);
			allsectors.Add(s, vs);
			return vs;
		}

		// This creates a visual sector
		protected override VisualSector CreateVisualSector(Sector s)
		{
			BaseVisualSector vs = new BaseVisualSector(this, s);
			allsectors.Add(s, vs); //mxd
			return vs;
		}
		
		// This creates a visual thing
		protected override VisualThing CreateVisualThing(Thing t)
		{
			BaseVisualThing vt = new BaseVisualThing(this, t);
			return vt.Setup() ? vt : null;
		}

		// This locks the target so that it isn't changed until unlocked
		public void LockTarget()
		{
			locktarget = true;
		}
		
		// This unlocks the target so that is changes to the aimed geometry again
		public void UnlockTarget()
		{
			locktarget = false;
		}
		
		// This picks a new target, if not locked
		private void PickTargetUnlocked()
		{
			if(!locktarget) PickTarget();
		}
		
		// This picks a new target
		private void PickTarget()
		{
			// Find the object we are aiming at
			Vector3D start = General.Map.VisualCamera.Position;
			Vector3D delta = General.Map.VisualCamera.Target - General.Map.VisualCamera.Position;
			delta = delta.GetFixedLength(General.Settings.ViewDistance * PICK_RANGE);
			VisualPickResult newtarget = PickObject(start, start + delta);
			
			// Should we update the info on panels?
			bool updateinfo = (newtarget.picked != target.picked);
			
			// Apply new target
			target = newtarget;

			// Show target info
			if(updateinfo) ShowTargetInfo();
		}

		// This shows the picked target information
		public void ShowTargetInfo()
		{
			// Any result?
			if(target.picked != null)
			{
				// Geometry picked?
				if(target.picked is VisualGeometry)
				{
					VisualGeometry pickedgeo = (target.picked as VisualGeometry);
					
					// Sidedef?
					if(pickedgeo is BaseVisualGeometrySidedef)
					{
						BaseVisualGeometrySidedef pickedsidedef = (pickedgeo as BaseVisualGeometrySidedef);
						General.Interface.ShowLinedefInfo(pickedsidedef.GetControlLinedef(), pickedsidedef.Sidedef); //mxd
					}
					// Sector?
					else if(pickedgeo is BaseVisualGeometrySector)
					{
						BaseVisualGeometrySector pickedsector = (pickedgeo as BaseVisualGeometrySector);
						bool isceiling = (pickedsector is VisualCeiling); //mxd
						General.Interface.ShowSectorInfo(pickedsector.Level.sector, isceiling, !isceiling);
					}
					else
					{
						General.Interface.HideInfo();
					}
				} 
				else if(target.picked is VisualThing) 
				{ // Thing picked?
					VisualThing pickedthing = (target.picked as VisualThing);
					General.Interface.ShowThingInfo(pickedthing.Thing);
				} 
				else if(target.picked is VisualVertex) //mxd
				{
					VisualVertex pickedvert = (target.picked as VisualVertex);
					General.Interface.ShowVertexInfo(pickedvert.Vertex);
				}
			}
			else
			{
				General.Interface.HideInfo();
			}
		}
		
		// This updates the VisualSectors and VisualThings that have their Changed property set
		private void UpdateChangedObjects()
		{
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
			{
				if(vs.Value != null)
				{
					BaseVisualSector bvs = vs.Value as BaseVisualSector;
					if(bvs.Changed) bvs.Rebuild();
				}
			}

			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
			{
				if(vt.Value != null)
				{
					BaseVisualThing bvt = vt.Value as BaseVisualThing;
					if(bvt.Changed) bvt.Rebuild();
				}
			}

			//mxd
			if(General.Map.UDMF) 
			{
				foreach(KeyValuePair<Vertex, VisualVertexPair> pair in vertices)
					pair.Value.Update();
			}

			//mxd. Update event lines (still better than updating them on every frame redraw)
			renderer.SetEventLines(LinksCollector.GetThingLinks(General.Map.ThingsFilter.VisibleThings, blockmap, bsp, useblockmap));
		}

		//mxd
		protected override void MoveSelectedThings(Vector2D direction, bool absoluteposition) 
		{
			List<VisualThing> visualthings = GetSelectedVisualThings(true);
			if(visualthings.Count == 0) return;

			PreAction(UndoGroup.ThingMove);

			Vector3D[] coords = new Vector3D[visualthings.Count];
			for(int i = 0; i < visualthings.Count; i++)
				coords[i] = visualthings[i].Thing.Position;

			//move things...
			Vector3D[] translatedcoords = TranslateCoordinates(coords, direction, absoluteposition);
			for(int i = 0; i < visualthings.Count; i++) 
			{
				BaseVisualThing t = visualthings[i] as BaseVisualThing;
				t.OnMove(translatedcoords[i]);
			}

			// Things may've changed sectors...
			FillBlockMap();

			PostAction();
		}

		//mxd
		private static Vector3D[] TranslateCoordinates(Vector3D[] coordinates, Vector2D direction, bool absolutePosition) 
		{
			if(coordinates.Length == 0) return null;

			direction.x = (float)Math.Round(direction.x);
			direction.y = (float)Math.Round(direction.y);

			Vector3D[] translatedCoords = new Vector3D[coordinates.Length];

			//move things...
			if(!absolutePosition) //...relatively (that's easy)
			{ 
				int camAngle = (int)Math.Round(Angle2D.RadToDeg(General.Map.VisualCamera.AngleXY));
				int sector = General.ClampAngle(camAngle - 45) / 90;
				direction = direction.GetRotated(sector * Angle2D.PIHALF);

				for(int i = 0; i < coordinates.Length; i++)
					translatedCoords[i] = coordinates[i] + new Vector3D(direction);

				return translatedCoords;
			}

			//...to specified location preserving relative positioning (that's harder)
			if(coordinates.Length == 1) //just move it there
			{
				translatedCoords[0] = new Vector3D(direction.x, direction.y, coordinates[0].z);
				return translatedCoords;
			}

			//we need some reference
			float minX = coordinates[0].x;
			float maxX = minX;
			float minY = coordinates[0].y;
			float maxY = minY;

			//get bounding coordinates for selected things
			for(int i = 1; i < coordinates.Length; i++) 
			{
				if(coordinates[i].x < minX)
					minX = coordinates[i].x;
				else if(coordinates[i].x > maxX)
					maxX = coordinates[i].x;

				if(coordinates[i].y < minY)
					minY = coordinates[i].y;
				else if(coordinates[i].y > maxY)
					maxY = coordinates[i].y;
			}

			Vector2D selectionCenter = new Vector2D(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2);

			//move them
			for(int i = 0; i < coordinates.Length; i++)
				translatedCoords[i] = new Vector3D((float)Math.Round(direction.x - (selectionCenter.x - coordinates[i].x)), (float)Math.Round(direction.y - (selectionCenter.y - coordinates[i].y)), (float)Math.Round(coordinates[i].z));

			return translatedCoords;
		}

		//mxd
		public override void UpdateSelectionInfo() 
		{
			// Collect info
			int numWalls = 0;
			int numFloors = 0;
			int numCeilings = 0;
			int numThings = 0;
			int numVerts = 0;

			foreach(IVisualEventReceiver obj in selectedobjects) 
			{
				if(!obj.IsSelected()) continue;

				if(obj is BaseVisualThing) numThings++;
				else if(obj is BaseVisualVertex) numVerts++;
				else if(obj is VisualCeiling) numCeilings++;
				else if(obj is VisualFloor)	numFloors++;
				else if(obj is VisualMiddleSingle || obj is VisualMiddleDouble || obj is VisualLower || obj is VisualUpper || obj is VisualMiddle3D || obj is VisualMiddleBack)
					numWalls++;
			}

			List<string> results = new List<string>();
			if(numWalls > 0) results.Add(numWalls + (numWalls > 1 ? " sidedefs" : " sidedef"));
			if(numFloors > 0) results.Add(numFloors + (numFloors > 1 ? " floors" : " floor"));
			if(numCeilings > 0) results.Add(numCeilings + (numCeilings > 1 ? " ceilings" : " ceiling"));
			if(numThings > 0) results.Add(numThings + (numThings > 1 ? " things" : " thing"));
			if(numVerts > 0) results.Add(numVerts + (numVerts > 1 ? " vertices" : " vertex"));

			// Display results
			string result = string.Empty;
			if(results.Count > 0) 
			{
				result = string.Join(", ", results.ToArray());
				int pos = result.LastIndexOf(",", StringComparison.Ordinal);
				if(pos != -1) result = result.Remove(pos, 1).Insert(pos, " and");
				result += " selected.";
			}

			General.Interface.DisplayStatus(StatusType.Selection, result);
		}

		//mxd
		internal void StartRealtimeInterfaceUpdate(SelectionType selectiontype)
		{
			switch(selectiontype)
			{
				case SelectionType.All:
				case SelectionType.Linedefs:
				case SelectionType.Sectors:
					General.Interface.OnEditFormValuesChanged += Interface_OnSectorEditFormValuesChanged;
					break;
				case SelectionType.Things:
					General.Interface.OnEditFormValuesChanged += Interface_OnThingEditFormValuesChanged;
					break;
				default:
					General.Interface.OnEditFormValuesChanged += Interface_OnEditFormValuesChanged;
					break;
			}
		}

		//mxd
		internal void StopRealtimeInterfaceUpdate(SelectionType selectiontype)
		{
			switch(selectiontype)
			{
				case SelectionType.All:
				case SelectionType.Linedefs:
				case SelectionType.Sectors:
					General.Interface.OnEditFormValuesChanged -= Interface_OnSectorEditFormValuesChanged;
					break;
				case SelectionType.Things:
					General.Interface.OnEditFormValuesChanged -= Interface_OnThingEditFormValuesChanged;
					break;
				default:
					General.Interface.OnEditFormValuesChanged -= Interface_OnEditFormValuesChanged;
					break;
			}
		}
		
		#endregion

		#region ================== Extended Methods

		// This requests a sector's extra data
		internal SectorData GetSectorData(Sector s)
		{
			// Make fresh sector data when it doesn't exist yet
			if(!sectordata.ContainsKey(s))
				sectordata[s] = new SectorData(this, s);
			
			return sectordata[s];
		}

        //mxd. This requests a sector's extra data or null if given sector doesn't have it
        internal SectorData GetSectorDataEx(Sector s)
        {
            return (sectordata.ContainsKey(s) ? sectordata[s] : null);
        }

        // This requests a things's extra data
        internal ThingData GetThingData(Thing t)
		{
			// Make fresh sector data when it doesn't exist yet
			if(!thingdata.ContainsKey(t))
				thingdata[t] = new ThingData(this, t);
			
			return thingdata[t];
		}

		//mxd
		internal VertexData GetVertexData(Vertex v) 
		{
			if(!vertexdata.ContainsKey(v))
				vertexdata[v] = new VertexData(this, v);
			return vertexdata[v];
		}

		internal BaseVisualVertex GetVisualVertex(Vertex v, bool floor) 
		{
			if(!vertices.ContainsKey(v))
				vertices.Add(v, new VisualVertexPair(new BaseVisualVertex(this, v, false), new BaseVisualVertex(this, v, true)));

			return (floor ? vertices[v].FloorVertex as BaseVisualVertex : vertices[v].CeilingVertex as BaseVisualVertex);
		}

		//mxd
		internal void UpdateVertexHandle(Vertex v) 
		{
			if(!vertices.ContainsKey(v))
				vertices.Add(v, new VisualVertexPair(new BaseVisualVertex(this, v, false), new BaseVisualVertex(this, v, true)));
			else
				vertices[v].Changed = true;
		}

        // This rebuilds the sector data
        // This requires that the blockmap is up-to-date!
        internal void RebuildElementData()
		{
			//mxd
			Sector[] sectorsWithEffects = null;
            bsp.Build();
            useblockmap = bsp.IsDeactivated;

            if (!General.Settings.GZDoomRenderingEffects)
            {
				//store all sectors with effects
				if(sectordata != null && sectordata.Count > 0) 
				{
					sectorsWithEffects = new Sector[sectordata.Count];
					sectordata.Keys.CopyTo(sectorsWithEffects, 0);
				}

				//remove all vertex handles from selection
				if(vertices != null && vertices.Count > 0) 
				{
					foreach(IVisualEventReceiver i in selectedobjects)
					{
						if(i is BaseVisualVertex) RemoveSelectedObject(i);
					}
				}
			}

			Dictionary<int, List<Sector>> sectortags = new Dictionary<int, List<Sector>>();
			sectordata = new Dictionary<Sector, SectorData>(General.Map.Map.Sectors.Count);
			thingdata = new Dictionary<Thing, ThingData>(General.Map.Map.Things.Count);

            if (bsp.IsDeactivated && !String.IsNullOrEmpty(bsp.ErrorMessage))
                MessageBox.Show("Could not load the map's nodes: " + bsp.ErrorMessage + " Defaulting to blockmap.", "Error loading nodes!", MessageBoxButtons.OK, MessageBoxIcon.Error);


            //mxd. rebuild all sectors with effects
            if (sectorsWithEffects != null) 
			{
				for(int i = 0; i < sectorsWithEffects.Length; i++) 
				{
					// The visual sector associated is now outdated
					if(VisualSectorExists(sectorsWithEffects[i])) 
					{
						BaseVisualSector vs = GetVisualSector(sectorsWithEffects[i]) as BaseVisualSector;
						vs.UpdateSectorGeometry(true);
					}
				}
			}

			if(General.Map.UDMF) 
			{
				vertexdata = new Dictionary<Vertex, VertexData>(General.Map.Map.Vertices.Count); //mxd
				vertices.Clear();
			}

            if (!General.Settings.GZDoomRenderingEffects) return; //mxd

            // Find all sector who's tag is not 0 and hash them so that we can find them quicly
            foreach (Sector s in General.Map.Map.Sectors)
			{
				foreach(int tag in s.Tags)
				{
					if (tag == 0 && General.Settings.NoVisualsTag0) continue;
					if (!sectortags.ContainsKey(tag)) sectortags[tag] = new List<Sector>();
					sectortags[tag].Add(s);
				}
			}

            foreach (Sector s in General.Map.Map.Sectors)
            {
                if (General.Map.SRB2) s.Fields.Clear(); //Reset fake UDMF properties (since the linedef special that set them might no longer be there)
                else
                {
                    // Find sectors with 3 vertices, because they can be sloped
                    // ========== Thing vertex slope, vertices with UDMF vertex offsets ==========
                    if (s.Sidedefs.Count == 3)
                    {
                        if (General.Map.UDMF) GetSectorData(s).AddEffectVertexOffset(); //mxd
                        List<Thing> slopeceilingthings = new List<Thing>(3);
                        List<Thing> slopefloorthings = new List<Thing>(3);

                        foreach (Sidedef sd in s.Sidedefs)
                        {
                            Vertex v = sd.IsFront ? sd.Line.End : sd.Line.Start;

                            // Check if a thing is at this vertex
                            VisualBlockEntry b = blockmap.GetBlock(blockmap.GetBlockCoordinates(v.Position));
                            foreach (Thing t in b.Things)
                            {
                                if ((Vector2D)t.Position == v.Position)
                                {
                                    switch (t.SRB2Type)
                                    {
                                        case 1504: slopefloorthings.Add(t); break;
                                        case 1505: slopeceilingthings.Add(t); break;
                                    }
                                }
                            }
                        }

                        // Slope any floor vertices?
                        if (slopefloorthings.Count > 0)
                        {
                            SectorData sd = GetSectorData(s);
                            sd.AddEffectThingVertexSlope(slopefloorthings, true);
                        }

                        // Slope any ceiling vertices?
                        if (slopeceilingthings.Count > 0)
                        {
                            SectorData sd = GetSectorData(s);
                            sd.AddEffectThingVertexSlope(slopeceilingthings, false);
                        }
                    }

                    // ========== mxd. Glowing flats ==========
                    if (General.Map.Data.GlowingFlats.ContainsKey(s.LongFloorTexture) || General.Map.Data.GlowingFlats.ContainsKey(s.LongCeilTexture))
                    {
                        SectorData sd = GetSectorData(s);
                        sd.AddEffectGlowingFlat(s);
                    }
                }
            }

            // Find interesting linedefs (such as line slopes)
            foreach (Linedef l in General.Map.Map.Linedefs)
            {
                // MascaraSnake: Slope handling
                // ========== Plane Align (see http://zdoom.org/wiki/Plane_Align) ==========
                if (l.IsRegularSlope)
                {
                    if (!General.Map.FormatInterface.HasLinedefParameters) l.SetSlopeArgs();
                    if (((l.Args[0] == 1) || (l.Args[1] == 1)) && (l.Front != null))
                    {
                        SectorData sd = GetSectorData(l.Front.Sector);
                        sd.AddEffectLineSlope(l);
                    }
                    if (((l.Args[0] == 2) || (l.Args[1] == 2)) && (l.Back != null))
                    {
                        SectorData sd = GetSectorData(l.Back.Sector);
                        sd.AddEffectLineSlope(l);
                    }
                }

                // MascaraSnake: Slope handling
                // ========== Plane Copy (mxd) (see http://zdoom.org/wiki/Plane_Copy) ==========
                if (l.IsCopySlope)
                {
                    if (!General.Map.FormatInterface.HasLinedefParameters) l.SetSlopeCopyArgs();
                    //check the flags...
                    bool floorCopyToBack = false;
                    bool floorCopyToFront = false;
                    bool ceilingCopyToBack = false;
                    bool ceilingCopyToFront = false;

                    if (l.Args[4] > 0 && l.Args[4] != 3 && l.Args[4] != 12)
                    {
                        floorCopyToBack = (l.Args[4] & 1) == 1;
                        floorCopyToFront = (l.Args[4] & 2) == 2;
                        ceilingCopyToBack = (l.Args[4] & 4) == 4;
                        ceilingCopyToFront = (l.Args[4] & 8) == 8;
                    }

                    // Copy slope to front sector
                    if (l.Front != null)
                    {
                        if ((l.Args[0] > 0 || l.Args[1] > 0) || (l.Back != null && (floorCopyToFront || ceilingCopyToFront)))
                        {
                            SectorData sd = GetSectorData(l.Front.Sector);
                            sd.AddEffectPlaneClopySlope(l, true);
                        }
                    }

                    // Copy slope to back sector
                    if (l.Back != null)
                    {
                        if ((l.Args[2] > 0 || l.Args[3] > 0) || (l.Front != null && (floorCopyToBack || ceilingCopyToBack)))
                        {
                            SectorData sd = GetSectorData(l.Back.Sector);
                            sd.AddEffectPlaneClopySlope(l, false);
                        }
                    }
                }

                if (General.Map.SRB2)
                {
                    //MascaraSnake: Flat alignment
                    if (l.IsFlatAlignment)
                    {
						//if (General.Map.NewFlatAlignment)
						if (General.Map.NewFlatAlignment)
						{
							//JBR - SRB2 2.2 / SRB2 Kart - Sector Flat Alignment
							int sectortag = l.Tag;
							bool alignonlyceiling = l.IsFlagSet("2048");
							bool alignonlyfloor = l.IsFlagSet("4096");
							bool offsetbytexture = l.IsFlagSet("8192");

							if (sectortags.ContainsKey(sectortag))
							{
								float rotation = General.ClampAngle(90f - l.Angle * Angle2D.PIDEG);
								float xoffset = -l.Start.Position.x;
								float yoffset = -l.Start.Position.y;
								if (offsetbytexture)
								{
									xoffset = l.Front.OffsetX;
									yoffset = -l.Front.OffsetY;
								}

								// Affine offset, this got me a few headaches of why both rotation and offset didn't worked at same time
								float rotationrad = rotation / Angle2D.PIDEG;
								float cos = (float)Math.Cos(rotationrad);
								float sin = (float)Math.Sin(rotationrad);
								float rx = cos * xoffset - sin * yoffset;
								float ry = sin * xoffset + cos * yoffset;
								xoffset = rx;
								yoffset = -ry;

								List<Sector> sectors = sectortags[sectortag];
								foreach (Sector s in sectors)
								{
									s.Fields.BeforeFieldsChange();

									if (!alignonlyceiling)
									{
										if (s.Fields.ContainsKey("rotationfloor")) s.Fields["rotationfloor"] = new UniValue(UniversalType.Float, rotation);
										else s.Fields.Add("rotationfloor", new UniValue(UniversalType.Float, rotation));
										if (s.Fields.ContainsKey("xpanningfloor")) s.Fields["xpanningfloor"] = new UniValue(UniversalType.Float, xoffset);
										else s.Fields.Add("xpanningfloor", new UniValue(UniversalType.Float, xoffset));
										if (s.Fields.ContainsKey("ypanningfloor")) s.Fields["ypanningfloor"] = new UniValue(UniversalType.Float, yoffset);
										else s.Fields.Add("ypanningfloor", new UniValue(UniversalType.Float, yoffset));
									}
									if (!alignonlyfloor)
									{
										if (s.Fields.ContainsKey("rotationceiling")) s.Fields["rotationceiling"] = new UniValue(UniversalType.Float, rotation);
										else s.Fields.Add("rotationceiling", new UniValue(UniversalType.Float, rotation));
										if (s.Fields.ContainsKey("xpanningceiling")) s.Fields["xpanningceiling"] = new UniValue(UniversalType.Float, xoffset);
										else s.Fields.Add("xpanningceiling", new UniValue(UniversalType.Float, xoffset));
										if (s.Fields.ContainsKey("ypanningceiling")) s.Fields["ypanningceiling"] = new UniValue(UniversalType.Float, yoffset);
										else s.Fields.Add("ypanningceiling", new UniValue(UniversalType.Float, yoffset));
									}
								}
							}
						}
						else
						{
							//MascaraSnake: Flat alignment
							int sectortag = l.Tag;
							bool alignonlyceiling = l.IsFlagSet("2");
							bool alignonlyfloor = l.IsFlagSet("64");
							bool rotate = l.IsFlagSet("512");
							bool rotateonlyceiling = l.IsFlagSet("1024");
							bool rotateonlyfloor = l.IsFlagSet("16384");

							if (sectortags.ContainsKey(sectortag))
							{
								List<Sector> sectors = sectortags[sectortag];
								foreach (Sector s in sectors)
								{
									s.Fields.BeforeFieldsChange();
									if (rotate)
									{
										float rotation = General.ClampAngle(l.AngleDeg - 90);

										if (!rotateonlyceiling)
										{
											if (s.Fields.ContainsKey("rotationfloor")) s.Fields["rotationfloor"] = new UniValue(UniversalType.Float, rotation);
											else s.Fields.Add("rotationfloor", new UniValue(UniversalType.Float, rotation));
										}
										if (!rotateonlyfloor)
										{
											if (s.Fields.ContainsKey("rotationceiling")) s.Fields["rotationceiling"] = new UniValue(UniversalType.Float, rotation);
											else s.Fields.Add("rotationceiling", new UniValue(UniversalType.Float, rotation));
										}
									}
									else
									{
										float xoffset = l.Rect.Width;
										float yoffset = l.Rect.Height;

										if (!alignonlyceiling)
										{
											if (s.Fields.ContainsKey("xpanningfloor")) s.Fields["xpanningfloor"] = new UniValue(UniversalType.Float, xoffset);
											else s.Fields.Add("xpanningfloor", new UniValue(UniversalType.Float, xoffset));
											if (s.Fields.ContainsKey("ypanningfloor")) s.Fields["ypanningfloor"] = new UniValue(UniversalType.Float, yoffset);
											else s.Fields.Add("ypanningfloor", new UniValue(UniversalType.Float, yoffset));
										}
										if (!alignonlyfloor)
										{
											if (s.Fields.ContainsKey("xpanningceiling")) s.Fields["xpanningceiling"] = new UniValue(UniversalType.Float, xoffset);
											else s.Fields.Add("xpanningceiling", new UniValue(UniversalType.Float, xoffset));
											if (s.Fields.ContainsKey("ypanningceiling")) s.Fields["ypanningceiling"] = new UniValue(UniversalType.Float, yoffset);
											else s.Fields.Add("ypanningceiling", new UniValue(UniversalType.Float, yoffset));
										}
									}
								}
							}
						}
					}

                    //MascaraSnake: Colormap
                    if (l.IsColormap && l.Front != null && General.Settings.ShowColormaps)
                    {
                        int sectortag = l.Tag;
                        int color;
                        int alpha;
                        l.ParseColor(l.Front.HighTexture, out color, out alpha);
                        if (sectortags.ContainsKey(sectortag) || sectortag == 65535)
                        {
                            List<Sector> sectors = (sectortag == 65535) ? General.Map.Map.Sectors.ToList() : sectortags[sectortag];
                            foreach (Sector s in sectors)
                            {
                                s.Fields.BeforeFieldsChange();
                                if (s.Fields.ContainsKey("lightcolor")) s.Fields["lightcolor"] = new UniValue(UniversalType.Color, color);
                                else s.Fields.Add("lightcolor", new UniValue(UniversalType.Color, color));
                                //TODO: Find out why the alpha value is ignored.
                                /*if (s.Fields.ContainsKey("lightalpha")) s.Fields["lightalpha"] = new UniValue(UniversalType.Integer, alpha);
                                else s.Fields.Add("lightalpha", new UniValue(UniversalType.Integer, alpha));*/
                            }
                        }
                    }

                    // MascaraSnake: Vertex slopes, SRB2-style
                    if (l.IsVertexSlope)
                    {
                        l.SetVertexSlopeArgs();
                        if (l.Args[0] >= 0 && l.Args[0] <= 3)
                        {
                            bool slopefloor = l.Args[0] == 0 || l.Args[0] == 2;
                            List<Thing> slopevertices = new List<Thing>(3);
                            Sidedef side = (l.Args[0] == 0 || l.Args[0] == 1) ? l.Front : l.Back;
                            Sector s = side.Sector;

                            //If NOKNUCKLES is set, use tag, X offset and Y offset to search for slope vertices.
                            if (l.IsFlagSet("8192"))
                            {
                                bool foundtag = false;
                                bool foundxoffset = false;
                                bool foundyoffset = false;
                                foreach (Thing t in General.Map.Map.Things)
                                {
                                    if (t.IsSlopeVertex)
                                    {
                                        if (!foundtag && (int)t.AngleDoom == l.Tag)
                                        {
                                            slopevertices.Add(t);
                                            foundtag = true;
                                        }
                                        if (!foundxoffset && (int)t.AngleDoom == side.OffsetX)
                                        {
                                            slopevertices.Add(t);
                                            foundxoffset = true;
                                        }
                                        if (!foundyoffset && (int)t.AngleDoom == side.OffsetY)
                                        {
                                            slopevertices.Add(t);
                                            foundyoffset = true;
                                        }
                                    }
                                }

                            }
                            //Otherwise, just use tag.
                            else
                            {
                                foreach (Thing t in General.Map.Map.Things)
                                {
                                    if (t.IsSlopeVertex && (int)t.AngleDoom == l.Tag) slopevertices.Add(t);
                                }
                            }
                            if (slopevertices.Count >= 3)
                            {
                                SectorData sd = GetSectorData(s);
                                sd.AddEffectSRB2ThingVertexSlope(slopevertices, slopefloor, blockmap, bsp);
                            }
                        }
                    }
                }
                
                // MascaraSnake: 3D floor handling
                // ========== Sector 3D floor (see http://zdoom.org/wiki/Sector_Set3dFloor) ==========
                if (l.Is3DFloor)
                {
                    if (l.Front != null)
                    {
                        if (!General.Map.FormatInterface.HasLinedefParameters) l.Set3DFloorArgs();
                        //mxd. Added hi-tag/line ID check 
                        int sectortag = (General.Map.UDMF || (l.Args[1] & (int)Effect3DFloor.FloorTypes.HiTagIsLineID) != 0) ? l.Args[0] : l.Args[0] + (l.Args[4] << 8);
                        if (sectortags.ContainsKey(sectortag) || sectortag == 65535)
                        {
                            List<Sector> sectors = (sectortag == 65535) ? General.Map.Map.Sectors.ToList() : sectortags[sectortag];
                            foreach (Sector s in sectors)
                            {
                                SectorData sd = GetSectorData(s);
                                sd.AddEffect3DFloor(l);
                            }
                        }
                    }

                }
                if (!General.Map.SRB2)
                {
                    switch (l.Action)
                    {
                        // ========== Transfer Brightness (see http://zdoom.org/wiki/ExtraFloor_LightOnly) =========
                        case 50:
                            if (l.Front != null && sectortags.ContainsKey(l.Args[0]))
                            {
                                List<Sector> sectors = sectortags[l.Args[0]];
                                foreach (Sector s in sectors)
                                {
                                    SectorData sd = GetSectorData(s);
                                    sd.AddEffectBrightnessLevel(l);
                                }
                            }
                            break;

                        // ========== mxd. Transfer Floor Brightness (see http://www.zdoom.org/w/index.php?title=Transfer_FloorLight) =========
                        case 210:
                            if (l.Front != null && sectortags.ContainsKey(l.Args[0]))
                            {
                                List<Sector> sectors = sectortags[l.Args[0]];
                                foreach (Sector s in sectors)
                                {
                                    SectorData sd = GetSectorData(s);
                                    sd.AddEffectTransferFloorBrightness(l);
                                }
                            }
                            break;

                        // ========== mxd. Transfer Ceiling Brightness (see http://www.zdoom.org/w/index.php?title=Transfer_CeilingLight) =========
                        case 211:
                            if (l.Front != null && sectortags.ContainsKey(l.Args[0]))
                            {
                                List<Sector> sectors = sectortags[l.Args[0]];
                                foreach (Sector s in sectors)
                                {
                                    SectorData sd = GetSectorData(s);
                                    sd.AddEffectTransferCeilingBrightness(l);
                                }
                            }
                            break;
                    }
                }
            }

            if (!General.Map.SRB2)
            {
                // Find interesting things (such as sector slopes)
                foreach (Thing t in General.Map.Map.Things)
                {
                    switch (t.SRB2Type)
                    {
                        // ========== Copy slope ==========
                        case 9511:
                        case 9510:
                            t.DetermineSector(blockmap);
                            if (t.Sector != null)
                            {
                                SectorData sd = GetSectorData(t.Sector);
                                sd.AddEffectCopySlope(t);
                            }
                            break;

                        // ========== Thing line slope ==========
                        case 9501:
                        case 9500:
                            t.DetermineSector(blockmap);
                            if (t.Sector != null)
                            {
                                SectorData sd = GetSectorData(t.Sector);
                                sd.AddEffectThingLineSlope(t);
                            }
                            break;

                        // ========== Thing slope ==========
                        case 9503:
                        case 9502:
                            t.DetermineSector(blockmap);
                            if (t.Sector != null)
                            {
                                SectorData sd = GetSectorData(t.Sector);
                                sd.AddEffectThingSlope(t);
                            }
                            break;
                    }
                }
            }
		}
		
		#endregion

		#region ================== Events

		// Help!
		public override void OnHelp()
		{
			General.ShowHelp("e_visual.html");
		}

		// When entering this mode
		public override void OnEngage()
		{
			base.OnEngage();

			//mxd
			useSelectionFromClassicMode = BuilderPlug.Me.SyncSelection ? !General.Interface.ShiftState : General.Interface.ShiftState;
			if(useSelectionFromClassicMode)	UpdateSelectionInfo();

			// Read settings
			cameraflooroffset = General.Map.Config.ReadSetting("cameraflooroffset", cameraflooroffset);
			cameraceilingoffset = General.Map.Config.ReadSetting("cameraceilingoffset", cameraceilingoffset);

			//mxd. Update fog color (otherwise FogBoundaries won't be setup correctly)
			foreach(Sector s in General.Map.Map.Sectors) s.UpdateFogColor();

			// (Re)create special effects
			RebuildElementData();

			//mxd. Update event lines
			renderer.SetEventLines(LinksCollector.GetThingLinks(General.Map.ThingsFilter.VisibleThings, blockmap, bsp, useblockmap));
		}

		// When returning to another mode
		public override void OnDisengage()
		{
			base.OnDisengage();

			//mxd
			if(BuilderPlug.Me.SyncSelection ? !General.Interface.ShiftState : General.Interface.ShiftState)
			{
				//clear previously selected stuff
				General.Map.Map.ClearAllSelected();
				
				//refill selection
				List<int> selectedsectorindices = new List<int>();
				List<int> selectedlineindices = new List<int>();
				List<int> selectedvertexindices = new List<int>();

				foreach(IVisualEventReceiver obj in selectedobjects)
				{
					if(obj is BaseVisualThing) 
					{
						((BaseVisualThing)obj).Thing.Selected = true;
					}
					else if(obj is VisualFloor || obj is VisualCeiling) 
					{
						VisualGeometry vg = obj as VisualGeometry;
						if(vg.Sector != null && vg.Sector.Sector != null && !selectedsectorindices.Contains(vg.Sector.Sector.Index))
						{
							selectedsectorindices.Add(vg.Sector.Sector.Index);
							vg.Sector.Sector.Selected = true;
						}
					}
					else if(obj is VisualLower || obj is VisualUpper || obj is VisualMiddleDouble 
						|| obj is VisualMiddleSingle || obj is VisualMiddle3D) 
					{
						VisualGeometry vg = obj as VisualGeometry;
						if(vg.Sidedef != null && !selectedlineindices.Contains(vg.Sidedef.Line.Index))
						{
							selectedlineindices.Add(vg.Sidedef.Line.Index);
							vg.Sidedef.Line.Selected = true;
						}
					}
					else if(obj is VisualVertex)
					{
						VisualVertex v = obj as VisualVertex;
						if(!selectedvertexindices.Contains(v.Vertex.Index))
						{
							selectedvertexindices.Add(v.Vertex.Index);
							v.Vertex.Selected = true;
						}
					}
				}
			}

			General.Map.Map.Update();
		}
		
		// Processing
		public override void OnProcess(float deltatime)
		{
			// Process things?
			base.ProcessThings = (BuilderPlug.Me.ShowVisualThings != 0);
			
			// Setup the move multiplier depending on gravity
			Vector3D movemultiplier = new Vector3D(1.0f, 1.0f, 1.0f);
			if(BuilderPlug.Me.UseGravity) movemultiplier.z = 0.0f;
			General.Map.VisualCamera.MoveMultiplier = movemultiplier;
			
			// Apply gravity?
			if(BuilderPlug.Me.UseGravity && (General.Map.VisualCamera.Sector != null))
			{
				SectorData sd = GetSectorData(General.Map.VisualCamera.Sector);
				if(!sd.Updated) sd.Update();

				// Camera below floor level?
				Vector3D feetposition = General.Map.VisualCamera.Position;
				SectorLevel floorlevel = sd.GetFloorBelow(feetposition) ?? sd.Floor;
				float floorheight = floorlevel.plane.GetZ(General.Map.VisualCamera.Position);
				if(General.Map.VisualCamera.Position.z < (floorheight + cameraflooroffset + 0.1f))
				{
					// Stay above floor
					gravity = new Vector3D(0.0f, 0.0f, 0.0f);
					General.Map.VisualCamera.Position = new Vector3D(General.Map.VisualCamera.Position.x,
																	 General.Map.VisualCamera.Position.y,
																	 floorheight + cameraflooroffset);
				}
				else
				{
					// Fall down
					gravity.z += GRAVITY * General.Map.VisualCamera.Gravity * deltatime;
					if(gravity.z > 3.0f) gravity.z = 3.0f;

					// Test if we don't go through a floor
					if((General.Map.VisualCamera.Position.z + gravity.z) < (floorheight + cameraflooroffset + 0.1f))
					{
						// Stay above floor
						gravity = new Vector3D(0.0f, 0.0f, 0.0f);
						General.Map.VisualCamera.Position = new Vector3D(General.Map.VisualCamera.Position.x,
																		 General.Map.VisualCamera.Position.y,
																		 floorheight + cameraflooroffset);
					}
					else
					{
						// Apply gravity vector
						General.Map.VisualCamera.Position += gravity;
					}
				}

				// Camera above ceiling?
				feetposition = General.Map.VisualCamera.Position - new Vector3D(0, 0, cameraflooroffset - 7.0f);
				SectorLevel ceillevel = sd.GetCeilingAbove(feetposition) ?? sd.Ceiling;
				float ceilheight = ceillevel.plane.GetZ(General.Map.VisualCamera.Position);
				if(General.Map.VisualCamera.Position.z > (ceilheight - cameraceilingoffset - 0.01f))
				{
					// Stay below ceiling
					General.Map.VisualCamera.Position = new Vector3D(General.Map.VisualCamera.Position.x,
																	 General.Map.VisualCamera.Position.y,
																	 ceilheight - cameraceilingoffset);
				}
			}
			else
			{
				gravity = new Vector3D(0.0f, 0.0f, 0.0f);
			}
			
			// Do processing
			base.OnProcess(deltatime);

			// Process visible geometry
			foreach(IVisualEventReceiver g in visiblegeometry)
			{
				g.OnProcess(deltatime);
			}
			
			// Time to pick a new target?
			if(Clock.CurrentTime > (lastpicktime + PICK_INTERVAL))
			{
				PickTargetUnlocked();
				lastpicktime = Clock.CurrentTime;
			}
			
			// The mouse is always in motion
			MouseEventArgs args = new MouseEventArgs(General.Interface.MouseButtons, 0, 0, 0, 0);
			OnMouseMove(args);
		}
		
		// This draws a frame
		public override void OnRedrawDisplay()
		{
			// Start drawing
			if(renderer.Start())
			{
				// Use fog!
				renderer.SetFogMode(true);

				// Set target for highlighting
				renderer.ShowSelection = General.Settings.GZOldHighlightMode || BuilderPlug.Me.UseHighlight; //mxd

				if(BuilderPlug.Me.UseHighlight)
					renderer.SetHighlightedObject(target.picked);
				
				// Begin with geometry
				renderer.StartGeometry();

				// Render all visible sectors
				foreach(VisualGeometry g in visiblegeometry)
					renderer.AddSectorGeometry(g);

				if(BuilderPlug.Me.ShowVisualThings != 0)
				{
					// Render things in cages?
					renderer.DrawThingCages = ((BuilderPlug.Me.ShowVisualThings & 2) != 0);
					
					// Render all visible things
					foreach(VisualThing t in visiblethings.Values)
						renderer.AddThingGeometry(t);
				}

				//mxd
				if(General.Map.UDMF && General.Settings.GZShowVisualVertices && vertices.Count > 0) 
				{
					List<VisualVertex> verts = new List<VisualVertex>();

					foreach(KeyValuePair<Vertex, VisualVertexPair> pair in vertices)
						verts.AddRange(pair.Value.Vertices);

					renderer.SetVisualVertices(verts);
				}
				
				// Done rendering geometry
				renderer.FinishGeometry();
				
				// Render crosshair
				renderer.RenderCrosshair();
				
				// Present!
				renderer.Finish();
			}
		}
		
		// After resources were reloaded
		protected override void ResourcesReloaded()
		{
			base.ResourcesReloaded();
			RebuildElementData();
            UpdateChangedObjects(); //mxd
            PickTarget();
		}
		
		// This usually happens when geometry is changed by undo, redo, cut or paste actions
		// and uses the marks to check what needs to be reloaded.
		protected override void ResourcesReloadedPartial()
		{
			bool sectorsmarked = false;
			
			if(General.Map.UndoRedo.GeometryChanged)
			{
				// Let the core do this (it will just dispose the sectors that were changed)
				base.ResourcesReloadedPartial();
			}
			else
			{
				// Neighbour sectors must be updated as well
				foreach(Sector s in General.Map.Map.Sectors)
				{
					if(s.Marked)
					{
						sectorsmarked = true;
						foreach(Sidedef sd in s.Sidedefs)
						{
							sd.Marked = true;
							if(sd.Other != null) sd.Other.Marked = true;
						}
					}
				}
				
				// Go for all sidedefs to update
				foreach(Sidedef sd in General.Map.Map.Sidedefs)
				{
					if(sd.Marked && VisualSectorExists(sd.Sector))
					{
						BaseVisualSector vs = GetVisualSector(sd.Sector) as BaseVisualSector;
						VisualSidedefParts parts = vs.GetSidedefParts(sd);
						parts.SetupAllParts();
					}
				}
				
				// Go for all sectors to update
				foreach(Sector s in General.Map.Map.Sectors)
				{
					if(s.Marked)
					{
						SectorData sd = GetSectorData(s);
						sd.Reset();
						
						// UpdateSectorGeometry for associated sectors (sd.UpdateAlso) as well!
						foreach(KeyValuePair<Sector, bool> us in sd.UpdateAlso)
						{
							if(VisualSectorExists(us.Key))
							{
								BaseVisualSector vs = GetVisualSector(us.Key) as BaseVisualSector;
								vs.UpdateSectorGeometry(us.Value);
							}
						}
						
						// And update for this sector ofcourse
						if(VisualSectorExists(s))
						{
							BaseVisualSector vs = GetVisualSector(s) as BaseVisualSector;
							vs.UpdateSectorGeometry(false);
						}
					}
				}
				
				if(!sectorsmarked)
				{
					// No sectors or geometry changed. So we only have
					// to update things when they have changed.
					foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
						if((vt.Value != null) && vt.Key.Marked) (vt.Value as BaseVisualThing).Rebuild();
				}
				else
				{
					// Things depend on the sector they are in and because we can't
					// easily determine which ones changed, we dispose all things
					foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
						if(vt.Value != null) vt.Value.Dispose();
					
					// Apply new lists
					allthings = new Dictionary<Thing, VisualThing>(allthings.Count);
				}
				
				// Clear visibility collections
				visiblesectors.Clear();
				visibleblocks.Clear();
				visiblegeometry.Clear();
				visiblethings.Clear();
				
				// Make new blockmap
				if(sectorsmarked || General.Map.UndoRedo.PopulationChanged)
					FillBlockMap();
				
				RebuildElementData();
				UpdateChangedObjects();
				
				// Visibility culling (this re-creates the needed resources)
				DoCulling();
			}
			
			// Determine what we're aiming at now
			PickTarget();
		}
		
		// Mouse moves
		public override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			IVisualEventReceiver o = GetTargetEventReceiver(true);
			o.OnMouseMove(e);

			//mxd. Show hints!
			if(o.GetType() != lasthighlighttype) 
			{
				if(General.Interface.ActiveDockerTabName == "Help") 
				{
					if(o is BaseVisualGeometrySidedef) 
					{
						General.Hints.ShowHints(this.GetType(), "sidedefs");
					}
					else if(o is BaseVisualGeometrySector) 
					{
						General.Hints.ShowHints(this.GetType(), "sectors");
					}
					else if(o is BaseVisualThing) 
					{
						General.Hints.ShowHints(this.GetType(), "things");
					}
					else if(o is BaseVisualVertex) 
					{
						General.Hints.ShowHints(this.GetType(), "vertices");
					}
					else 
					{
						General.Hints.ShowHints(this.GetType(), HintsManager.GENERAL);
					}
				}

				lasthighlighttype = o.GetType();
			}
		}
		
		// Undo performed
		public override void OnUndoEnd()
		{
			base.OnUndoEnd();

            //mxd. Effects may've become invalid
            if (General.Settings.GZDoomRenderingEffects && sectordata != null && sectordata.Count > 0)
                RebuildElementData();

			//mxd. As well as geometry...
			foreach(KeyValuePair<Sector, VisualSector> group in visiblesectors)
			{
				if(group.Value is BaseVisualSector)
					(group.Value as BaseVisualSector).Rebuild();
			}

			RebuildSelectedObjectsList();
			
			// We can't group with this undo level anymore
			lastundogroup = UndoGroup.None;
		}
		
		// Redo performed
		public override void OnRedoEnd()
		{
			base.OnRedoEnd();

			//mxd. Effects may've become invalid
			if(sectordata != null && sectordata.Count > 0)
				RebuildElementData();

			//mxd. As well as geometry...
			foreach(KeyValuePair<Sector, VisualSector> group in visiblesectors) 
			{
				if(group.Value is BaseVisualSector) (group.Value as BaseVisualSector).Rebuild();
			}

			RebuildSelectedObjectsList();
		}

		//mxd
		private void Interface_OnSectorEditFormValuesChanged(object sender, EventArgs e) 
		{
			if(allsectors == null) return;

			// Reset changed flags
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors) 
			{
				BaseVisualSector bvs = (vs.Value as BaseVisualSector);
				foreach(VisualFloor vf in bvs.ExtraFloors) vf.Changed = false;
				foreach(VisualCeiling vc in bvs.ExtraCeilings) vc.Changed = false;
				foreach(VisualFloor vf in bvs.ExtraBackFloors) vf.Changed = false;
				foreach(VisualCeiling vc in bvs.ExtraBackCeilings) vc.Changed = false;
				bvs.Floor.Changed = false;
				bvs.Ceiling.Changed = false;
			}

			UpdateChangedObjects();
			ShowTargetInfo();
		}

		//mxd
		private void Interface_OnThingEditFormValuesChanged(object sender, EventArgs e) 
		{
			//update visual sectors, which are affected by certain things
			List<Thing> things = GetSelectedThings();
			foreach(Thing t in things) 
			{
				if(thingdata.ContainsKey(t)) 
				{
					// Update what must be updated
					ThingData td = GetThingData(t);
					foreach(KeyValuePair<Sector, bool> s in td.UpdateAlso) 
					{
						if(VisualSectorExists(s.Key)) 
						{
							BaseVisualSector vs = GetVisualSector(s.Key) as BaseVisualSector;
							vs.UpdateSectorGeometry(s.Value);
						}
					}
				}
			}
			
			UpdateChangedObjects();
			ShowTargetInfo();
		}

		//mxd
		private void Interface_OnEditFormValuesChanged(object sender, EventArgs e) 
		{
			UpdateChangedObjects();
			ShowTargetInfo();
		}

		//mxd
		private void SelectioninfoupdatetimerOnTick(object sender, EventArgs eventArgs) 
		{
			selectioninfoupdatetimer.Stop();
			UpdateSelectionInfo();
		}
		
		#endregion

		#region ================== Action Assist

		// Because some actions can only be called on a single (the targeted) object because
		// they show a dialog window or something, these functions help applying the result
		// to all compatible selected objects.
		
		// Apply texture offsets
		public void ApplyTextureOffsetChange(int dx, int dy)
		{
			List<IVisualEventReceiver> objs = GetSelectedObjects(false, true, false, false);
			
			//mxd. Because Upper/Middle/Lower textures offsets should be threated separately in UDMF
			if(General.Map.UDMF)
			{
				Dictionary<BaseVisualGeometrySidedef, bool> donesides = new Dictionary<BaseVisualGeometrySidedef, bool>(selectedobjects.Count);
				foreach(IVisualEventReceiver i in objs) 
				{
					if(!(i is BaseVisualGeometrySidedef)) continue;
					BaseVisualGeometrySidedef vs = i as BaseVisualGeometrySidedef; //mxd
					if(!donesides.ContainsKey(vs)) 
					{
						//mxd. added scaling by texture scale
						if(vs.Texture.UsedInMap) //mxd. Otherwise it's MissingTexture3D and we probably don't want to drag that
							vs.OnChangeTextureOffset((int)(dx / vs.Texture.Scale.x), (int)(dy / vs.Texture.Scale.y), false);

						donesides.Add(vs, false);
					}
				}
			}
			else
			{
				Dictionary<Sidedef, bool> donesides = new Dictionary<Sidedef, bool>(selectedobjects.Count);
				foreach(IVisualEventReceiver i in objs) 
				{
					if(!(i is BaseVisualGeometrySidedef)) continue;
					BaseVisualGeometrySidedef vs = i as BaseVisualGeometrySidedef; //mxd
					if(!donesides.ContainsKey(vs.Sidedef)) 
					{
						//mxd. added scaling by texture scale
						if(vs.Texture.UsedInMap) //mxd. Otherwise it's MissingTexture3D and we probably don't want to drag that
							vs.OnChangeTextureOffset((int)(dx / vs.Texture.Scale.x), (int)(dy / vs.Texture.Scale.y), false);

						donesides.Add(vs.Sidedef, false);
					}
				}
			}
		}

		// Apply flat offsets
		public void ApplyFlatOffsetChange(int dx, int dy)
		{
			Dictionary<Sector, int> donesectors = new Dictionary<Sector, int>(selectedobjects.Count);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, false, false, false);
			foreach(IVisualEventReceiver i in objs)
			{
				if(i is BaseVisualGeometrySector)
				{
					if(!donesectors.ContainsKey((i as BaseVisualGeometrySector).Sector.Sector))
					{
						i.OnChangeTextureOffset(dx, dy, false);
						donesectors.Add((i as BaseVisualGeometrySector).Sector.Sector, 0);
					}
				}
			}
		}

		// Apply upper unpegged flag
		public void ApplyUpperUnpegged(bool set)
		{
			List<IVisualEventReceiver> objs = GetSelectedObjects(false, true, false, false);
			foreach(IVisualEventReceiver i in objs)
			{
				i.ApplyUpperUnpegged(set);
			}
		}

		// Apply lower unpegged flag
		public void ApplyLowerUnpegged(bool set)
		{
			List<IVisualEventReceiver> objs = GetSelectedObjects(false, true, false, false);
			foreach(IVisualEventReceiver i in objs)
			{
				i.ApplyLowerUnpegged(set);
			}
		}

		// Apply texture change
		public void ApplySelectTexture(string texture, bool flat)
		{
			List<IVisualEventReceiver> objs;
			
			if(General.Map.Config.MixTexturesFlats)
			{
				// Apply on all compatible types
				objs = GetSelectedObjects(true, true, false, false);
			}
			else
			{
				// We don't want to mix textures and flats, so apply only on the appropriate type
				objs = GetSelectedObjects(flat, !flat, false, false);
			}
			
			foreach(IVisualEventReceiver i in objs)
			{
				i.ApplyTexture(texture);
			}
		}

		// This returns all selected objects
		internal List<IVisualEventReceiver> GetSelectedObjects(bool includesectors, bool includesidedefs, bool includethings, bool includevertices)
		{
			List<IVisualEventReceiver> objs = new List<IVisualEventReceiver>();
			foreach(IVisualEventReceiver i in selectedobjects)
            {
                if (includesectors && (i is BaseVisualGeometrySector) /*&& ((BaseVisualGeometrySector)i).Triangles > 0*/) objs.Add(i);
                else if (includesidedefs && (i is BaseVisualGeometrySidedef) /*&& ((BaseVisualGeometrySidedef)i).Triangles > 0*/) objs.Add(i);
                else if (includethings && (i is BaseVisualThing)) objs.Add(i);
                else if (includevertices && (i is BaseVisualVertex)) objs.Add(i); //mxd
            }

            // Add highlight?
            if (selectedobjects.Count == 0)
			{
				IVisualEventReceiver i = (target.picked as IVisualEventReceiver);
                if (includesectors && (i is BaseVisualGeometrySector) /*&& ((BaseVisualGeometrySector)i).Triangles > 0*/) objs.Add(i);
                else if (includesidedefs && (i is BaseVisualGeometrySidedef) /*&& ((BaseVisualGeometrySidedef)i).Triangles > 0*/) objs.Add(i);
                else if (includethings && (i is BaseVisualThing)) objs.Add(i);
                else if (includevertices && (i is BaseVisualVertex)) objs.Add(i); //mxd
            }

            return objs;
		}

		//mxd
		internal List<IVisualEventReceiver> RemoveDuplicateSidedefs(List<IVisualEventReceiver> objs) 
		{
			Dictionary<Sidedef, bool> processed = new Dictionary<Sidedef, bool>();
			List<IVisualEventReceiver> result = new List<IVisualEventReceiver>();

			foreach(IVisualEventReceiver obj in objs)
			{
				if(!(obj is BaseVisualGeometrySidedef))
				{
					result.Add(obj);
				}
				else 
				{
					Sidedef side = (obj as BaseVisualGeometrySidedef).Sidedef;

					if(!processed.ContainsKey(side))
					{
						processed.Add(side, false);
						result.Add(obj);
					}
				}
			}

			return result;
		}

		// This returns all selected sectors, no doubles
		public List<Sector> GetSelectedSectors()
		{
			Dictionary<Sector, int> added = new Dictionary<Sector, int>();
			List<Sector> sectors = new List<Sector>();
			foreach(IVisualEventReceiver i in selectedobjects)
			{
				if(i is BaseVisualGeometrySector)
				{
					Sector s = (i as BaseVisualGeometrySector).Level.sector;
					if(!added.ContainsKey(s))
					{
						sectors.Add(s);
						added.Add(s, 0);
					}
				}
			}

			// Add highlight?
			if((selectedobjects.Count == 0) && (target.picked is BaseVisualGeometrySector))
			{
				Sector s = (target.picked as BaseVisualGeometrySector).Level.sector;
				if(!added.ContainsKey(s)) sectors.Add(s);
			}
			
			return sectors;
		}

		// This returns all selected linedefs, no doubles
		public List<Linedef> GetSelectedLinedefs()
		{
			Dictionary<Linedef, int> added = new Dictionary<Linedef, int>();
			List<Linedef> linedefs = new List<Linedef>();
			foreach(IVisualEventReceiver i in selectedobjects)
			{
				if(i is BaseVisualGeometrySidedef)
				{
					Linedef l = (i as BaseVisualGeometrySidedef).GetControlLinedef(); //mxd
					if(!added.ContainsKey(l))
					{
						linedefs.Add(l);
						added.Add(l, 0);
					}
				}
			}

			// Add highlight?
			if((selectedobjects.Count == 0) && (target.picked is BaseVisualGeometrySidedef))
			{
				Linedef l = (target.picked as BaseVisualGeometrySidedef).GetControlLinedef(); //mxd
				if(!added.ContainsKey(l)) linedefs.Add(l);
			}

			return linedefs;
		}

		// This returns all selected sidedefs, no doubles
		public List<Sidedef> GetSelectedSidedefs()
		{
			Dictionary<Sidedef, int> added = new Dictionary<Sidedef, int>();
			List<Sidedef> sidedefs = new List<Sidedef>();
			foreach(IVisualEventReceiver i in selectedobjects)
			{
				if(i is BaseVisualGeometrySidedef)
				{
					Sidedef sd = (i as BaseVisualGeometrySidedef).Sidedef;
					if(!added.ContainsKey(sd))
					{
						sidedefs.Add(sd);
						added.Add(sd, 0);
					}
				}
			}

			// Add highlight?
			if((selectedobjects.Count == 0) && (target.picked is BaseVisualGeometrySidedef))
			{
				Sidedef sd = (target.picked as BaseVisualGeometrySidedef).Sidedef;
				if(!added.ContainsKey(sd)) sidedefs.Add(sd);
			}

			return sidedefs;
		}

		// This returns all selected things, no doubles
		public List<Thing> GetSelectedThings()
		{
			Dictionary<Thing, int> added = new Dictionary<Thing, int>();
			List<Thing> things = new List<Thing>();
			foreach(IVisualEventReceiver i in selectedobjects)
			{
				if(i is BaseVisualThing)
				{
					Thing t = (i as BaseVisualThing).Thing;
					if(!added.ContainsKey(t))
					{
						things.Add(t);
						added.Add(t, 0);
					}
				}
			}

			// Add highlight?
			if((selectedobjects.Count == 0) && (target.picked is BaseVisualThing))
			{
				Thing t = (target.picked as BaseVisualThing).Thing;
				if(!added.ContainsKey(t)) things.Add(t);
			}

			return things;
		}

		//mxd. This returns all selected vertices, no doubles
		public List<Vertex> GetSelectedVertices() 
		{
			Dictionary<Vertex, int> added = new Dictionary<Vertex, int>();
			List<Vertex> verts = new List<Vertex>();

			foreach(IVisualEventReceiver i in selectedobjects) 
			{
				if(i is BaseVisualVertex) 
				{
					Vertex v = (i as BaseVisualVertex).Vertex;
					
					if(!added.ContainsKey(v)) 
					{
						verts.Add(v);
						added.Add(v, 0);
					}
				}
			}

			// Add highlight?
			if((selectedobjects.Count == 0) && (target.picked is BaseVisualVertex)) 
			{
				Vertex v = (target.picked as BaseVisualVertex).Vertex;
				if(!added.ContainsKey(v))
					verts.Add(v);
			}

			return verts;
		}
		
		// This returns the IVisualEventReceiver on which the action must be performed
		private IVisualEventReceiver GetTargetEventReceiver(bool targetonly)
		{
			if(target.picked != null)
			{
				if(singleselection || target.picked.Selected || targetonly)
				{
					return target.picked as IVisualEventReceiver;
				}

				if(selectedobjects.Count > 0)
				{
					return selectedobjects[0];
				}

				return target.picked as IVisualEventReceiver;
			}

			return new NullVisualEventReceiver();
		}

		//mxd. Copied from BuilderModes.ThingsMode
		// This creates a new thing
		private static Thing CreateThing(Vector2D pos) 
		{
			if(pos.x < General.Map.Config.LeftBoundary || pos.x > General.Map.Config.RightBoundary ||
				pos.y > General.Map.Config.TopBoundary || pos.y < General.Map.Config.BottomBoundary) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Failed to insert thing: outside of map boundaries.");
				return null;
			}

			// Create thing
			Thing t = General.Map.Map.CreateThing();
			if(t != null) 
			{
				General.Settings.ApplyDefaultThingSettings(t);
				t.Move(pos);
				t.UpdateConfiguration();
				General.Map.IsChanged = true;
				
				// Update things filter so that it includes this thing
				General.Map.ThingsFilter.Update();

				// Snap to grid enabled?
				if(General.Interface.SnapToGrid) 
				{
					// Snap to grid
					t.SnapToGrid();
				} 
				else 
				{
					// Snap to map format accuracy
					t.SnapToAccuracy();
				}
			}

			return t;
		}
		
		#endregion

		#region ================== Actions

		[BeginAction("clearselection", BaseAction = true)]
		public void ClearSelection()
		{
			selectedobjects = new List<IVisualEventReceiver>();
			
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
			{
				if(vs.Value != null)
				{
					BaseVisualSector bvs = (BaseVisualSector)vs.Value;
					if(bvs.Floor != null) bvs.Floor.Selected = false;
					if(bvs.Ceiling != null) bvs.Ceiling.Selected = false;
					foreach(VisualFloor vf in bvs.ExtraFloors) vf.Selected = false;
					foreach(VisualCeiling vc in bvs.ExtraCeilings) vc.Selected = false;
					foreach(VisualFloor vf in bvs.ExtraBackFloors) vf.Selected = false; //mxd
					foreach(VisualCeiling vc in bvs.ExtraBackCeilings) vc.Selected = false; //mxd

					foreach(Sidedef sd in vs.Key.Sidedefs)
					{
						//mxd. VisualSidedefParts can contain references to visual geometry, which is not present in VisualSector.sidedefgeometry
						bvs.GetSidedefParts(sd).DeselectAllParts();
					}
				}
			}

			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
			{
				if(vt.Value != null)
				{
					BaseVisualThing bvt = vt.Value as BaseVisualThing;
					bvt.Selected = false;
				}
			}

			//mxd
			if(General.Map.UDMF) 
			{
				foreach(KeyValuePair<Vertex, VisualVertexPair> pair in vertices) pair.Value.Deselect();
			}

			//mxd
			General.Interface.DisplayStatus(StatusType.Selection, string.Empty);
		}

		[BeginAction("visualselect", BaseAction = true)]
		public void BeginSelect()
		{
			PreActionNoChange();
			PickTargetUnlocked();
			GetTargetEventReceiver(true).OnSelectBegin();
			PostAction();
		}

		[EndAction("visualselect", BaseAction = true)]
		public void EndSelect()
		{
			IVisualEventReceiver target = GetTargetEventReceiver(true);
			target.OnSelectEnd();

			//mxd
			if((General.Interface.ShiftState || General.Interface.CtrlState) && selectedobjects.Count > 0) 
			{
				if(General.Interface.AltState)
				{
					target.SelectNeighbours(target.IsSelected(), General.Interface.ShiftState, General.Interface.CtrlState);
				}
				else
				{
					IVisualEventReceiver[] selection = new IVisualEventReceiver[selectedobjects.Count];
					selectedobjects.CopyTo(selection);
					
					foreach(IVisualEventReceiver obj in selection)
						obj.SelectNeighbours(target.IsSelected(), General.Interface.ShiftState, General.Interface.CtrlState);
				}
			}

			Renderer.ShowSelection = true;
			Renderer.ShowHighlight = true;
			PostAction();
		}

		[BeginAction("visualedit", BaseAction = true)]
		public void BeginEdit()
		{
			PreAction(UndoGroup.None);
			GetTargetEventReceiver(false).OnEditBegin();
			PostAction();
		}

		[EndAction("visualedit", BaseAction = true)]
		public void EndEdit()
		{
			PreActionNoChange();
			GetTargetEventReceiver(false).OnEditEnd();
			PostAction();
		}

		[BeginAction("raisesector8")]
		public void RaiseSector8()
		{
			PreAction(UndoGroup.SectorHeightChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, true);
			foreach(IVisualEventReceiver i in objs) i.OnChangeTargetHeight(8);
			PostAction();
		}

		[BeginAction("lowersector8")]
		public void LowerSector8()
		{
			PreAction(UndoGroup.SectorHeightChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, true);
			foreach(IVisualEventReceiver i in objs) i.OnChangeTargetHeight(-8);
			PostAction();
		}

		[BeginAction("raisesector1")]
		public void RaiseSector1()
		{
			PreAction(UndoGroup.SectorHeightChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, true);
			foreach(IVisualEventReceiver i in objs) i.OnChangeTargetHeight(1);
			PostAction();
		}

		[BeginAction("lowersector1")]
		public void LowerSector1()
		{
			PreAction(UndoGroup.SectorHeightChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, true);
			foreach(IVisualEventReceiver i in objs) i.OnChangeTargetHeight(-1);
			PostAction();
		}

		//mxd
		[BeginAction("raisesectortonearest")]
		public void RaiseSectorToNearest() 
		{
			Dictionary<Sector, VisualFloor> floors = new Dictionary<Sector, VisualFloor>();
			Dictionary<Sector, VisualCeiling> ceilings = new Dictionary<Sector, VisualCeiling>();
			List<BaseVisualThing> things = new List<BaseVisualThing>();
			bool withinSelection = General.Interface.CtrlState;

			// Get selection
			if(selectedobjects.Count == 0)
			{
				IVisualEventReceiver i = (target.picked as IVisualEventReceiver);
				if(i is VisualFloor) 
				{
					VisualFloor vf = i as VisualFloor;
					floors.Add(vf.Level.sector, vf);
				} 
				else if(i is VisualCeiling) 
				{
					VisualCeiling vc = i as VisualCeiling;
					ceilings.Add(vc.Level.sector, vc);
				} 
				else if(i is BaseVisualThing) 
				{
					things.Add(i as BaseVisualThing);
				}
			} 
			else 
			{
				foreach(IVisualEventReceiver i in selectedobjects) 
				{
					if(i is VisualFloor) 
					{
						VisualFloor vf = i as VisualFloor;
						floors.Add(vf.Level.sector, vf);
					} 
					else if(i is VisualCeiling) 
					{
						VisualCeiling vc = i as VisualCeiling;
						ceilings.Add(vc.Level.sector, vc);
					} 
					else if(i is BaseVisualThing) 
					{
						things.Add(i as BaseVisualThing);
					}
				}
			}

			// Check what we have
			if(floors.Count + ceilings.Count == 0 && (things.Count == 0 || !General.Map.FormatInterface.HasThingHeight)) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "No suitable objects found!");
				return;
			} 
			
			if(withinSelection) 
			{
				string s = string.Empty;

				if(floors.Count == 1) s = "floors";

				if(ceilings.Count == 1) 
				{
					if(!string.IsNullOrEmpty(s)) s += " and ";
					s += "ceilings";
				}

				if(!string.IsNullOrEmpty(s))
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Can't do: at least 2 selected " + s + " are required!");
					return;
				}
			}

			// Process floors...
			int maxSelectedHeight = int.MinValue;
			int minSelectedCeilingHeight = int.MaxValue;
			int targetCeilingHeight = int.MaxValue;

			// Get highest ceiling height from selection
			foreach(KeyValuePair<Sector, VisualCeiling> group in ceilings) 
			{
				if(group.Key.CeilHeight > maxSelectedHeight) maxSelectedHeight = group.Key.CeilHeight;
			}

			if(withinSelection) 
			{
				// We are raising, so we don't need to check anything
				targetCeilingHeight = maxSelectedHeight;
			} 
			else 
			{
				// Get next higher floor or ceiling from surrounding unselected sectors
				foreach(Sector s in BuilderModesTools.GetSectorsAround(this, ceilings.Keys))
				{
					if(s.FloorHeight < targetCeilingHeight && s.FloorHeight > maxSelectedHeight)
						targetCeilingHeight = s.FloorHeight;
					else if(s.CeilHeight < targetCeilingHeight && s.CeilHeight > maxSelectedHeight)
						targetCeilingHeight = s.CeilHeight;
				}
			}

			// Ceilings...
			maxSelectedHeight = int.MinValue;
			int targetFloorHeight = int.MaxValue;

			// Get maximum floor and minimum ceiling heights from selection
			foreach(KeyValuePair<Sector, VisualFloor> group in floors) 
			{
				if(group.Key.FloorHeight > maxSelectedHeight) maxSelectedHeight = group.Key.FloorHeight;
				if(group.Key.CeilHeight < minSelectedCeilingHeight) minSelectedCeilingHeight = group.Key.CeilHeight;
			}

			if(withinSelection) 
			{
				// Check heights
				if(minSelectedCeilingHeight < maxSelectedHeight) 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Can't do: lowest ceiling is lower than highest floor!");
					return;
				} 
				targetFloorHeight = maxSelectedHeight;
			} 
			else 
			{
				// Get next higher floor or ceiling from surrounding unselected sectors
				foreach(Sector s in BuilderModesTools.GetSectorsAround(this, floors.Keys))
				{
					if(s.FloorHeight > maxSelectedHeight && s.FloorHeight < targetFloorHeight && s.FloorHeight <= minSelectedCeilingHeight)
						targetFloorHeight = s.FloorHeight;
					else if(s.CeilHeight > maxSelectedHeight && s.CeilHeight < targetFloorHeight && s.CeilHeight <= minSelectedCeilingHeight)
						targetFloorHeight = s.CeilHeight;
				}
			}

			//CHECK VALUES
			string alignFailDescription = string.Empty;

			if(floors.Count > 0 && targetFloorHeight == int.MaxValue) 
			{
				// Raise to lowest ceiling?
				if(!withinSelection && minSelectedCeilingHeight > maxSelectedHeight) 
				{
					targetFloorHeight = minSelectedCeilingHeight;
				} 
				else 
				{
					alignFailDescription = floors.Count > 1 ? "floors" : "floor";
				}
			}

			if(ceilings.Count > 0 && targetCeilingHeight == int.MaxValue) 
			{
				if(!string.IsNullOrEmpty(alignFailDescription)) alignFailDescription += " and ";
				alignFailDescription += ceilings.Count > 1 ? "ceilings" : "ceiling";
			}

			if(!string.IsNullOrEmpty(alignFailDescription)) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Unable to align selected " + alignFailDescription + "!");
				return;
			}

			//APPLY VALUES
			PreAction(UndoGroup.SectorHeightChange);

			// Change floors heights
			if(floors.Count > 0) 
			{
				foreach(KeyValuePair<Sector, VisualFloor> group in floors) 
				{
					if(targetFloorHeight != group.Key.FloorHeight)
						group.Value.OnChangeTargetHeight(targetFloorHeight - group.Key.FloorHeight);
				}
			}

			// Change ceilings heights
			if(ceilings.Count > 0) 
			{
				foreach(KeyValuePair<Sector, VisualCeiling> group in ceilings) 
				{
					if(targetCeilingHeight != group.Key.CeilHeight)
						group.Value.OnChangeTargetHeight(targetCeilingHeight - group.Key.CeilHeight);
				}
			}

			// Change things heights. Align to higher 3d floor or actual ceiling.
			if(General.Map.FormatInterface.HasThingHeight) 
			{
				foreach(BaseVisualThing vt in things) 
				{
					if(vt.Thing.Sector == null) continue;
					SectorData sd = GetSectorData(vt.Thing.Sector);
					vt.OnMove(new Vector3D(vt.Thing.Position, BuilderModesTools.GetHigherThingZ(this, sd, vt)));
				}
			}

			PostAction();
		}

		//mxd
		[BeginAction("lowersectortonearest")]
		public void LowerSectorToNearest() 
		{
			Dictionary<Sector, VisualFloor> floors = new Dictionary<Sector, VisualFloor>();
			Dictionary<Sector, VisualCeiling> ceilings = new Dictionary<Sector, VisualCeiling>();
			List<BaseVisualThing> things = new List<BaseVisualThing>();
			bool withinSelection = General.Interface.CtrlState;

			//get selection
			if(selectedobjects.Count == 0) 
			{
				IVisualEventReceiver i = (target.picked as IVisualEventReceiver);
				if(i is VisualFloor) 
				{
					VisualFloor vf = i as VisualFloor;
					floors.Add(vf.Level.sector, vf);
				} 
				else if(i is VisualCeiling) 
				{
					VisualCeiling vc = i as VisualCeiling;
					ceilings.Add(vc.Level.sector, vc);
				} 
				else if(i is BaseVisualThing) 
				{
					things.Add(i as BaseVisualThing);
				}
			}
			else
			{
				foreach(IVisualEventReceiver i in selectedobjects) 
				{
					if(i is VisualFloor) 
					{
						VisualFloor vf = i as VisualFloor;
						floors.Add(vf.Level.sector, vf);
					} 
					else if(i is VisualCeiling) 
					{
						VisualCeiling vc = i as VisualCeiling;
						ceilings.Add(vc.Level.sector, vc);
					} 
					else if(i is BaseVisualThing) 
					{
						things.Add(i as BaseVisualThing);
					}
				}
			}

			// Check what we have
			if(floors.Count + ceilings.Count == 0 && (things.Count == 0 || !General.Map.FormatInterface.HasThingHeight)) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "No suitable objects found!");
				return;
			}

			if(withinSelection) 
			{
				string s = string.Empty;

				if(floors.Count == 1) s = "floors";

				if(ceilings.Count == 1) 
				{
					if(!string.IsNullOrEmpty(s)) s += " and ";
					s += "ceilings";
				}

				if(!string.IsNullOrEmpty(s)) 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Can't do: at least 2 selected " + s + " are required!");
					return;
				}
			}

			// Process floors...
			int minSelectedHeight = int.MaxValue;
			int targetFloorHeight = int.MinValue;

			// Get minimum floor height from selection
			foreach(KeyValuePair<Sector, VisualFloor> group in floors) 
			{
				if(group.Key.FloorHeight < minSelectedHeight) minSelectedHeight = group.Key.FloorHeight;
			}
			
			if(withinSelection) 
			{
				// We are lowering, so we don't need to check anything
				targetFloorHeight = minSelectedHeight;
			} 
			else 
			{
				// Get next lower ceiling or floor from surrounding unselected sectors
				foreach(Sector s in BuilderModesTools.GetSectorsAround(this, floors.Keys))
				{
					if(s.CeilHeight > targetFloorHeight && s.CeilHeight < minSelectedHeight)
						targetFloorHeight = s.CeilHeight;
					else if(s.FloorHeight > targetFloorHeight && s.FloorHeight < minSelectedHeight)
						targetFloorHeight = s.FloorHeight;
				}
			}

			// Ceilings...
			minSelectedHeight = int.MaxValue;
			int maxSelectedFloorHeight = int.MinValue;
			int targetCeilingHeight = int.MinValue;

			// Get minimum ceiling and maximum floor heights from selection
			foreach(KeyValuePair<Sector, VisualCeiling> group in ceilings) 
			{
				if(group.Key.CeilHeight < minSelectedHeight) minSelectedHeight = group.Key.CeilHeight;
				if(group.Key.FloorHeight > maxSelectedFloorHeight) maxSelectedFloorHeight = group.Key.FloorHeight;
			}

			if(withinSelection) 
			{
				if(minSelectedHeight < maxSelectedFloorHeight) 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Can't do: lowest ceiling is lower than highest floor!");
					return;
				} 
				targetCeilingHeight = minSelectedHeight;
			} 
			else 
			{
				// Get next lower ceiling or floor from surrounding unselected sectors
				foreach(Sector s in BuilderModesTools.GetSectorsAround(this, ceilings.Keys))
				{
					if(s.CeilHeight > targetCeilingHeight && s.CeilHeight < minSelectedHeight && s.CeilHeight >= maxSelectedFloorHeight)
						targetCeilingHeight = s.CeilHeight;
					else if(s.FloorHeight > targetCeilingHeight && s.FloorHeight < minSelectedHeight && s.FloorHeight >= maxSelectedFloorHeight)
						targetCeilingHeight = s.FloorHeight;
				}
			}

			//CHECK VALUES:
			string alignFailDescription = string.Empty;

			if(floors.Count > 0 && targetFloorHeight == int.MinValue) 
				alignFailDescription = floors.Count > 1 ? "floors" : "floor";

			if(ceilings.Count > 0 && targetCeilingHeight == int.MinValue) 
			{
				// Drop to highest floor?
				if(!withinSelection && maxSelectedFloorHeight < minSelectedHeight) 
				{
					targetCeilingHeight = maxSelectedFloorHeight;
				} 
				else 
				{
					if(!string.IsNullOrEmpty(alignFailDescription)) alignFailDescription += " and ";
					alignFailDescription += ceilings.Count > 1 ? "ceilings" : "ceiling";
				}
			}

			if(!string.IsNullOrEmpty(alignFailDescription)) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Unable to align selected " + alignFailDescription + "!");
				return;
			}

			//APPLY VALUES:
			PreAction(UndoGroup.SectorHeightChange);

			// Change floor height
			if(floors.Count > 0) 
			{
				foreach(KeyValuePair<Sector, VisualFloor> group in floors) 
				{
					if(targetFloorHeight != group.Key.FloorHeight)
						group.Value.OnChangeTargetHeight(targetFloorHeight - group.Key.FloorHeight);
				}
			}

			// Change ceiling height
			if(ceilings.Count > 0) 
			{
				foreach(KeyValuePair<Sector, VisualCeiling> group in ceilings) 
				{
					if(targetCeilingHeight != group.Key.CeilHeight)
						group.Value.OnChangeTargetHeight(targetCeilingHeight - group.Key.CeilHeight);
				}
			}

			// Change things height. Drop to lower 3d floor or to actual sector's floor.
			if(General.Map.FormatInterface.HasThingHeight)
			{
				foreach(BaseVisualThing vt in things) 
				{
					if(vt.Thing.Sector == null) continue;
					SectorData sd = GetSectorData(vt.Thing.Sector);
					vt.OnMove(new Vector3D(vt.Thing.Position, BuilderModesTools.GetLowerThingZ(this, sd, vt)));
				}
			}

			PostAction();
		}

		//mxd
		[BeginAction("matchbrightness")]
		public void MatchBrightness() 
		{
			//check input
			if(!General.Map.UDMF) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "'Match Brightness' action works only in UDMF map format!");
				return;
			}

			if(selectedobjects.Count == 0) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "'Match Brightness' action requires a selection!");
				return;
			}

			IVisualEventReceiver highlighted = (target.picked as IVisualEventReceiver);

			if(highlighted is BaseVisualThing) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Highlight a surface, to which you want to match the brightness.");
				return;
			}

			//get target brightness
			int targetbrightness;
			if(highlighted is VisualFloor) 
			{
				VisualFloor v = highlighted as VisualFloor;
				targetbrightness = v.Level.sector.Fields.GetValue("lightfloor", 0);
				if(!v.Level.sector.Fields.GetValue("lightfloorabsolute", false)) 
				{
					targetbrightness += v.Level.sector.Brightness;
				}
			} 
			else if(highlighted is VisualCeiling) 
			{
				VisualCeiling v = highlighted as VisualCeiling;
				targetbrightness = v.Level.sector.Fields.GetValue("lightceiling", 0);
				if(!v.Level.sector.Fields.GetValue("lightceilingabsolute", false)) 
				{
					targetbrightness += v.Level.sector.Brightness;
				}
			} 
			else if(highlighted is VisualUpper || highlighted is VisualMiddleSingle || highlighted is VisualMiddleDouble || highlighted is VisualLower) 
			{
				BaseVisualGeometrySidedef v = highlighted as BaseVisualGeometrySidedef;
				targetbrightness = v.Sidedef.Fields.GetValue("light", 0);
				if(!v.Sidedef.Fields.GetValue("lightabsolute", false)) 
				{
					targetbrightness += v.Sidedef.Sector.Brightness;
				}
			} 
			else if(highlighted is VisualMiddle3D) 
			{
				VisualMiddle3D v = highlighted as VisualMiddle3D;
				Sidedef sd = v.GetControlLinedef().Front;
				if(sd == null) 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Highlight a surface, to which you want to match the brightness.");
					return;
				}
				targetbrightness = sd.Fields.GetValue("light", 0);
				if(!sd.Fields.GetValue("lightabsolute", false)) 
				{
					targetbrightness += sd.Sector.Brightness;
				}

			} 
			else 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Highlight a surface, to which you want to match the brightness.");
				return;
			}

			//make undo
			CreateUndo("Match Brightness");
			targetbrightness = General.Clamp(targetbrightness, 0, 255);

			//apply new brightness
			foreach(IVisualEventReceiver obj in selectedobjects) 
			{
				if(obj == highlighted) continue;

				if(obj is VisualFloor) 
				{
					VisualFloor v = obj as VisualFloor;
					v.Level.sector.Fields.BeforeFieldsChange();
					v.Sector.Changed = true;

					if(v.Level.sector.Fields.GetValue("lightfloorabsolute", false)) 
					{
						v.Level.sector.Fields["lightfloor"] = new UniValue(UniversalType.Integer, targetbrightness);
					} 
					else 
					{
						UniFields.SetInteger(v.Level.sector.Fields, "lightfloor", targetbrightness - v.Level.sector.Brightness, 0);
					}

					v.Sector.UpdateSectorGeometry(false);
				} 
				else if(obj is VisualCeiling) 
				{
					VisualCeiling v = obj as VisualCeiling;
					v.Level.sector.Fields.BeforeFieldsChange();
					v.Sector.Changed = true;
					v.Sector.Sector.UpdateNeeded = true;

					if(v.Level.sector.Fields.GetValue("lightceilingabsolute", false)) 
					{
						v.Level.sector.Fields["lightceiling"] = new UniValue(UniversalType.Integer, targetbrightness);
					} 
					else 
					{
						UniFields.SetInteger(v.Level.sector.Fields, "lightceiling", targetbrightness - v.Level.sector.Brightness, 0);
					}

					v.Sector.UpdateSectorGeometry(false);
				} 
				else if(obj is VisualUpper || obj is VisualMiddleSingle || obj is VisualMiddleDouble || obj is VisualLower) 
				{
					BaseVisualGeometrySidedef v = obj as BaseVisualGeometrySidedef;
					v.Sidedef.Fields.BeforeFieldsChange();
					v.Sector.Changed = true;

					if(v.Sidedef.Fields.GetValue("lightabsolute", false)) 
					{
						v.Sidedef.Fields["light"] = new UniValue(UniversalType.Integer, targetbrightness);
					} 
					else 
					{
						UniFields.SetInteger(v.Sidedef.Fields, "light", targetbrightness - v.Sidedef.Sector.Brightness, 0);
					}

					//Update 'lightfog' flag
					Tools.UpdateLightFogFlag(v.Sidedef);
				}
			}

			//Done
			General.Interface.DisplayStatus(StatusType.Action, "Matched brightness for " + selectedobjects.Count + " surfaces.");
			Interface_OnSectorEditFormValuesChanged(this, EventArgs.Empty);
		}

		[BeginAction("showvisualthings")]
		public void ShowVisualThings()
		{
			BuilderPlug.Me.ShowVisualThings++;
			if(BuilderPlug.Me.ShowVisualThings > 2) BuilderPlug.Me.ShowVisualThings = 0;
		}

		[BeginAction("raisebrightness8")]
		public void RaiseBrightness8()
		{
			PreAction(UndoGroup.SectorBrightnessChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			foreach(IVisualEventReceiver i in objs) i.OnChangeTargetBrightness(true);
			PostAction();
		}

		[BeginAction("lowerbrightness8")]
		public void LowerBrightness8()
		{
			PreAction(UndoGroup.SectorBrightnessChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			foreach(IVisualEventReceiver i in objs) i.OnChangeTargetBrightness(false);
			PostAction();
		}

		[BeginAction("movetextureleft")]
		public void MoveTextureLeft1()
		{
			PreAction(UndoGroup.TextureOffsetChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			if(!General.Map.UDMF) objs = RemoveDuplicateSidedefs(objs); //mxd
			foreach(IVisualEventReceiver i in objs) i.OnChangeTextureOffset(-1, 0, true);
			PostAction();
		}

		[BeginAction("movetextureright")]
		public void MoveTextureRight1()
		{
			PreAction(UndoGroup.TextureOffsetChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			if(!General.Map.UDMF) objs = RemoveDuplicateSidedefs(objs); //mxd
			foreach(IVisualEventReceiver i in objs) i.OnChangeTextureOffset(1, 0, true);
			PostAction();
		}

		[BeginAction("movetextureup")]
		public void MoveTextureUp1()
		{
			PreAction(UndoGroup.TextureOffsetChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			if(!General.Map.UDMF) objs = RemoveDuplicateSidedefs(objs); //mxd
			foreach(IVisualEventReceiver i in objs) i.OnChangeTextureOffset(0, -1, true);
			PostAction();
		}

		[BeginAction("movetexturedown")]
		public void MoveTextureDown1()
		{
			PreAction(UndoGroup.TextureOffsetChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			if(!General.Map.UDMF) objs = RemoveDuplicateSidedefs(objs); //mxd
			foreach(IVisualEventReceiver i in objs) i.OnChangeTextureOffset(0, 1, true);
			PostAction();
		}

		[BeginAction("movetextureleft8")]
		public void MoveTextureLeft8()
		{
			PreAction(UndoGroup.TextureOffsetChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			if(!General.Map.UDMF) objs = RemoveDuplicateSidedefs(objs); //mxd
			foreach(IVisualEventReceiver i in objs) i.OnChangeTextureOffset(-General.Map.Grid.GridSize, 0, true);
			PostAction();
		}

		[BeginAction("movetextureright8")]
		public void MoveTextureRight8()
		{
			PreAction(UndoGroup.TextureOffsetChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			if(!General.Map.UDMF) objs = RemoveDuplicateSidedefs(objs); //mxd
			foreach(IVisualEventReceiver i in objs) i.OnChangeTextureOffset(General.Map.Grid.GridSize, 0, true);
			PostAction();
		}

		[BeginAction("movetextureup8")]
		public void MoveTextureUp8()
		{
			PreAction(UndoGroup.TextureOffsetChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			if(!General.Map.UDMF) objs = RemoveDuplicateSidedefs(objs); //mxd
			foreach(IVisualEventReceiver i in objs) i.OnChangeTextureOffset(0, -General.Map.Grid.GridSize, true);
			PostAction();
		}

		[BeginAction("movetexturedown8")]
		public void MoveTextureDown8()
		{
			PreAction(UndoGroup.TextureOffsetChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			if(!General.Map.UDMF) objs = RemoveDuplicateSidedefs(objs); //mxd
			foreach(IVisualEventReceiver i in objs) i.OnChangeTextureOffset(0, General.Map.Grid.GridSize, true);
			PostAction();
		}

		//mxd
		[BeginAction("scaleup")]
		public void ScaleTextureUp() 
		{
			PreAction(UndoGroup.TextureScaleChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, false);
			foreach(IVisualEventReceiver i in objs) i.OnChangeScale(1, 1);
			PostAction();
		}

		//mxd
		[BeginAction("scaledown")]
		public void ScaleTextureDown() 
		{
			PreAction(UndoGroup.TextureScaleChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, false);
			foreach(IVisualEventReceiver i in objs) i.OnChangeScale(-1, -1);
			PostAction();
		}

		//mxd
		[BeginAction("scaleupx")]
		public void ScaleTextureUpX() 
		{
			PreAction(UndoGroup.TextureScaleChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, false);
			foreach(IVisualEventReceiver i in objs) i.OnChangeScale(1, 0);
			PostAction();
		}

		//mxd
		[BeginAction("scaledownx")]
		public void ScaleTextureDownX() 
		{
			PreAction(UndoGroup.TextureScaleChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, false);
			foreach(IVisualEventReceiver i in objs) i.OnChangeScale(-1, 0);
			PostAction();
		}

		//mxd
		[BeginAction("scaleupy")]
		public void ScaleTextureUpY() 
		{
			PreAction(UndoGroup.TextureScaleChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, false);
			foreach(IVisualEventReceiver i in objs) i.OnChangeScale(0, 1);
			PostAction();
		}

		//mxd
		[BeginAction("scaledowny")]
		public void ScaleTextureDownY() 
		{
			PreAction(UndoGroup.TextureScaleChange);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, false);
			foreach(IVisualEventReceiver i in objs) i.OnChangeScale(0, -1);
			PostAction();
		}

		[BeginAction("textureselect")]
		public void TextureSelect()
		{
			PreAction(UndoGroup.None);
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();
			GetTargetEventReceiver(false).OnSelectTexture();
			UpdateChangedObjects();
			RebuildElementData(); //mxd. Extrafloors or Glow effects may've been changed
			renderer.SetCrosshairBusy(false);
			PostAction();
		}

		[BeginAction("texturecopy")]
		public void TextureCopy()
		{
			PreActionNoChange();
			GetTargetEventReceiver(true).OnCopyTexture(); //mxd
			PostAction();
		}

		[BeginAction("texturepaste")]
		public void TexturePaste()
		{
			PreAction(UndoGroup.None);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			foreach(IVisualEventReceiver i in objs) i.OnPasteTexture();
			PostAction();
		}

		//mxd
		[BeginAction("visualautoalign")]
		public void TextureAutoAlign() 
		{
			PreAction(UndoGroup.None);
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();
			GetTargetEventReceiver(false).OnTextureAlign(true, true);
			UpdateChangedObjects();
			renderer.SetCrosshairBusy(false);
			PostAction();
		}

		[BeginAction("visualautoalignx")]
		public void TextureAutoAlignX()
		{
			PreAction(UndoGroup.None);
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();
			GetTargetEventReceiver(false).OnTextureAlign(true, false);
			UpdateChangedObjects();
			renderer.SetCrosshairBusy(false);
			PostAction();
		}

		[BeginAction("visualautoaligny")]
		public void TextureAutoAlignY()
		{
			PreAction(UndoGroup.None);
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();
			GetTargetEventReceiver(false).OnTextureAlign(false, true);
			UpdateChangedObjects();
			renderer.SetCrosshairBusy(false);
			PostAction();
		}

		//mxd
		[BeginAction("visualautoaligntoselection")]
		public void TextureAlignToSelected() 
		{
			PreAction(UndoGroup.None);
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();

			AutoAlignTexturesToSelected(true, true);

			UpdateChangedObjects();
			renderer.SetCrosshairBusy(false);
			PostAction();
		}

		//mxd
		[BeginAction("visualautoaligntoselectionx")]
		public void TextureAlignToSelectedX() 
		{
			PreAction(UndoGroup.None);
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();

			AutoAlignTexturesToSelected(true, false);

			UpdateChangedObjects();
			renderer.SetCrosshairBusy(false);
			PostAction();
		}

		//mxd
		[BeginAction("visualautoaligntoselectiony")]
		public void TextureAlignToSelectedY() 
		{
			PreAction(UndoGroup.None);
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();

			AutoAlignTexturesToSelected(false, true);

			UpdateChangedObjects();
			renderer.SetCrosshairBusy(false);
			PostAction();
		}

		//mxd
		private void AutoAlignTexturesToSelected(bool alignX, bool alignY) 
		{
			string rest;
			if(alignX && alignY) rest = "(X and Y)";
			else if(alignX) rest = "(X)";
			else rest = "(Y)";

			CreateUndo("Auto-align textures to selected sidedefs " + rest);
			SetActionResult("Auto-aligned textures to selected sidedefs " + rest + ".");

			// Clear all marks, this will align everything it can
			General.Map.Map.ClearMarkedSidedefs(false);
			
			//get selection
			List<IVisualEventReceiver> objs = GetSelectedObjects(false, true, false, false);

			//align
			foreach(IVisualEventReceiver i in objs) 
			{
				BaseVisualGeometrySidedef side = i as BaseVisualGeometrySidedef;
				
				// Make sure the texture is loaded (we need the texture size)
				if(!side.Texture.IsImageLoaded) side.Texture.LoadImage();

				//Align textures
				AutoAlignTextures(side, side.Texture, alignX, alignY, false, false);

				// Get the changed sidedefs
				List<Sidedef> changes = General.Map.Map.GetMarkedSidedefs(true);

				foreach(Sidedef sd in changes) 
				{
					// Update the parts for this sidedef!
					if(VisualSectorExists(sd.Sector)) 
					{
						BaseVisualSector vs = (GetVisualSector(sd.Sector) as BaseVisualSector);
						VisualSidedefParts parts = vs.GetSidedefParts(sd);
						parts.SetupAllParts();
					}
				}
			}
		}

		//mxd
		[BeginAction("visualfittextures")]
		private void FitTextures() 
		{
			PreAction(UndoGroup.None);
			
			// Get selection
			List<IVisualEventReceiver> objs = GetSelectedObjects(false, true, false, false);
			List<BaseVisualGeometrySidedef> sides = new List<BaseVisualGeometrySidedef>();
			foreach(IVisualEventReceiver side in objs) 
			{
				if(side is BaseVisualGeometrySidedef) sides.Add(side as BaseVisualGeometrySidedef);
			}

			if(sides.Count == 0)
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Fit Textures action requires selected sidedefs.");
				return;
			}

			// Show form
			FitTexturesForm form = new FitTexturesForm();

			// Undo changes?
			if(form.Setup(sides) && form.ShowDialog((Form)General.Interface) == DialogResult.Cancel)
				General.Map.UndoRedo.WithdrawUndo();

			PostAction();
		}

		[BeginAction("toggleupperunpegged")]
		public void ToggleUpperUnpegged()
		{
			PreAction(UndoGroup.None);
			GetTargetEventReceiver(false).OnToggleUpperUnpegged();
			PostAction();
		}

		[BeginAction("togglelowerunpegged")]
		public void ToggleLowerUnpegged()
		{
			PreAction(UndoGroup.None);
			GetTargetEventReceiver(false).OnToggleLowerUnpegged();
			PostAction();
		}

		[BeginAction("togglegravity")]
		public void ToggleGravity()
		{
			BuilderPlug.Me.UseGravity = !BuilderPlug.Me.UseGravity;
			string onoff = BuilderPlug.Me.UseGravity ? "ON" : "OFF";
			General.Interface.DisplayStatus(StatusType.Action, "Gravity is now " + onoff + ".");
		}

		[BeginAction("togglehighlight")]
		public void ToggleHighlight()
		{
			BuilderPlug.Me.UseHighlight = !BuilderPlug.Me.UseHighlight;
			string onoff = BuilderPlug.Me.UseHighlight ? "ON" : "OFF";
			General.Interface.DisplayStatus(StatusType.Action, "Highlight is now " + onoff + ".");
		}

		[BeginAction("resettexture")]
		public void ResetTexture()
		{
			PreAction(UndoGroup.None);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, false);
			foreach(IVisualEventReceiver i in objs) i.OnResetTextureOffset();
			PostAction();
		}

		[BeginAction("resettextureudmf")]
		public void ResetLocalOffsets() 
		{
			PreAction(UndoGroup.None);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, false);
			foreach(IVisualEventReceiver i in objs) i.OnResetLocalTextureOffset();
			PostAction();
		}

		[BeginAction("floodfilltextures")]
		public void FloodfillTextures()
		{
			PreAction(UndoGroup.None);
			GetTargetEventReceiver(false).OnTextureFloodfill();
			PostAction();
		}

		[BeginAction("texturecopyoffsets")]
		public void TextureCopyOffsets()
		{
			PreActionNoChange();
			GetTargetEventReceiver(true).OnCopyTextureOffsets(); //mxd
			PostAction();
		}

		[BeginAction("texturepasteoffsets")]
		public void TexturePasteOffsets()
		{
			PreAction(UndoGroup.None);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, false, false);
			foreach(IVisualEventReceiver i in objs) i.OnPasteTextureOffsets();
			PostAction();
		}

		[BeginAction("copyproperties")]
		public void CopyProperties()
		{
			PreActionNoChange();
			GetTargetEventReceiver(true).OnCopyProperties(); //mxd
			PostAction();
		}

		[BeginAction("pasteproperties")]
		public void PasteProperties()
		{
			PreAction(UndoGroup.None);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, true);
			foreach(IVisualEventReceiver i in objs) i.OnPasteProperties(false);
			PostAction();
		}

		//mxd
		[BeginAction("pastepropertieswithoptions")]
		public void PastePropertiesWithOptions()
		{
			// Which options to show?
			HashSet<int> added;
			var targettypes = new List<MapElementType>();
			var selection = new List<IVisualEventReceiver>();

			// Sectors selected?
			var obj = GetSelectedObjects(true, false, false, false);
			if(obj.Count > 0)
			{
				targettypes.Add(MapElementType.SECTOR);

				// Don't add duplicates
				added = new HashSet<int>();
				foreach(IVisualEventReceiver receiver in obj)
				{
					VisualGeometry vg = receiver as VisualGeometry;
					if(vg != null && !added.Contains(vg.Sector.GetHashCode()))
					{
						selection.Add(receiver);
						added.Add(vg.Sector.GetHashCode());
					}
				}
			}

			// Sidedefs selected?
			obj = GetSelectedObjects(false, true, false, false);
			if(obj.Count > 0)
			{
				targettypes.Add(MapElementType.SIDEDEF);

				// Don't add duplicates
				added = new HashSet<int>();
				foreach(IVisualEventReceiver receiver in obj)
				{
					VisualGeometry vg = receiver as VisualGeometry;
					if(vg != null && !added.Contains(vg.Sidedef.Line.GetHashCode()))
					{
						selection.Add(receiver);
						added.Add(vg.Sidedef.Line.GetHashCode());
					}
				}
			}

			// Things selected?
			obj = GetSelectedObjects(false, false, true, false);
			if(obj.Count > 0)
			{
				targettypes.Add(MapElementType.THING);

				// Don't add duplicates
				added = new HashSet<int>();
				foreach(IVisualEventReceiver receiver in obj)
				{
					VisualThing vt = receiver as VisualThing;
					if(vt != null && !added.Contains(vt.Thing.GetHashCode()))
					{
						selection.Add(receiver);
						added.Add(vt.Thing.GetHashCode());
					}
				}
			}

			// Vertices selected?
			obj = GetSelectedObjects(false, false, false, true);
			if(obj.Count > 0)
			{
				targettypes.Add(MapElementType.VERTEX);

				// Don't add duplicates
				added = new HashSet<int>();
				foreach(IVisualEventReceiver receiver in obj)
				{
					VisualVertex vv = receiver as VisualVertex;
					if(vv != null && !added.Contains(vv.Vertex.GetHashCode()))
					{
						selection.Add(receiver);
						added.Add(vv.Vertex.GetHashCode());
					}
				}
			}

			// Anything selected?
			if(selection.Count == 0)
			{
				General.Interface.DisplayStatus(StatusType.Warning, "This action requires highlight or selection!");
				return;
			}

			// Show the form
			PastePropertiesOptionsForm form = new PastePropertiesOptionsForm();
			if(form.Setup(targettypes) && form.ShowDialog(General.Interface) == DialogResult.OK)
			{
				// Paste properties
				PreAction(UndoGroup.None);
				foreach(IVisualEventReceiver i in selection)i.OnPasteProperties(true);
				PostAction();
			}
		}

		//mxd. now we can insert things in Visual modes
		[BeginAction("insertitem", BaseAction = true)] 
		public void InsertThing()
		{
			Vector2D hitpos = GetHitPosition();

			if(!hitpos.IsFinite()) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Cannot insert thing here!");
				return;
			}
			
			ClearSelection();
			PreActionNoChange();
			General.Map.UndoRedo.ClearAllRedos();
			General.Map.UndoRedo.CreateUndo("Insert thing");

			Thing t = CreateThing(new Vector2D(hitpos.x, hitpos.y));

			if(t == null) 
			{
				General.Map.UndoRedo.WithdrawUndo();
				return;
			}

			// Edit the thing?
			if(BuilderPlug.Me.EditNewThing) General.Interface.ShowEditThings(new List<Thing> { t });

			//add thing to blockmap
			blockmap.AddThing(t);

			General.Interface.DisplayStatus(StatusType.Action, "Inserted a new thing.");
			PostAction();
		}

		//mxd
		[BeginAction("deleteitem", BaseAction = true)]
		public void Delete()
		{
			PreAction(UndoGroup.None);
			List<IVisualEventReceiver> objs = GetSelectedObjects(true, true, true, true);
			foreach(IVisualEventReceiver i in objs) i.OnDelete();
			PostAction();

			ClearSelection();
		}

		//mxd
		[BeginAction("copyselection", BaseAction = true)]
		public void CopySelection() 
		{
			List<IVisualEventReceiver> objs = GetSelectedObjects(false, false, true, false);
			if(objs.Count == 0) return;

			copybuffer.Clear();
			foreach(IVisualEventReceiver i in objs) 
			{
				VisualThing vt = i as VisualThing;
				if(vt != null) copybuffer.Add(new ThingCopyData(vt.Thing));
			}
			General.Interface.DisplayStatus(StatusType.Info, "Copied " + copybuffer.Count + " Things");
		}

		//mxd
		[BeginAction("cutselection", BaseAction = true)]
		public void CutSelection() 
		{
			CopySelection();

			//Create undo
			string rest = copybuffer.Count + " thing" + (copybuffer.Count > 1 ? "s." : ".");
			CreateUndo("Cut " + rest);
			General.Interface.DisplayStatus(StatusType.Info, "Cut " + rest);

			List<IVisualEventReceiver> objs = GetSelectedObjects(false, false, true, false);
			foreach(IVisualEventReceiver i in objs) 
			{
				BaseVisualThing thing = i as BaseVisualThing;
				thing.Thing.Fields.BeforeFieldsChange();
				thing.Thing.Dispose();
				thing.Dispose();
			}

			General.Map.IsChanged = true;
			General.Map.ThingsFilter.Update();

			// Update event lines
			renderer.SetEventLines(LinksCollector.GetThingLinks(General.Map.ThingsFilter.VisibleThings, blockmap, bsp, useblockmap));
		}

		//mxd. We'll just use currently selected objects 
		[BeginAction("pasteselection", BaseAction = true)]
		public void PasteSelection() 
		{
			if(copybuffer.Count == 0)
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Nothing to paste, cut or copy some Things first!");
				return;
			}
			
			Vector2D hitpos = GetHitPosition();

			if(!hitpos.IsFinite()) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Cannot paste here!");
				return;
			}

			string rest = copybuffer.Count + " thing" + (copybuffer.Count > 1 ? "s" : "");
			General.Map.UndoRedo.CreateUndo("Paste " + rest);
			General.Interface.DisplayStatus(StatusType.Info, "Pasted " + rest);
			
			PreActionNoChange();
			ClearSelection();

			//get translated positions
			Vector3D[] coords = new Vector3D[copybuffer.Count];
			for(int i = 0; i < copybuffer.Count; i++ )
				coords[i] = copybuffer[i].Position;

			Vector3D[] translatedCoords = TranslateCoordinates(coords, hitpos, true);

			//create things from copyBuffer
			for(int i = 0; i < copybuffer.Count; i++) 
			{
				Thing t = CreateThing(new Vector2D());
				if(t != null) 
				{
					copybuffer[i].ApplyTo(t);
					t.Move(translatedCoords[i]);
					//add thing to blockmap
					blockmap.AddThing(t);
				}
			}

			General.Map.IsChanged = true;
			General.Map.ThingsFilter.Update();

			PostAction();
		}

		//mxd. Rotate clockwise
		[BeginAction("rotateclockwise")]
		public void RotateCW() 
		{
			RotateThingsAndTextures(5);
		}

		//mxd. Rotate counterclockwise
		[BeginAction("rotatecounterclockwise")]
		public void RotateCCW() 
		{
			RotateThingsAndTextures(-5);
		}

		//mxd
		private void RotateThingsAndTextures(int increment) 
		{
			PreAction(UndoGroup.ThingAngleChange);

			List<IVisualEventReceiver> selection = GetSelectedObjects(true, false, true, false);
			if(selection.Count == 0) return;

			foreach(IVisualEventReceiver obj in selection) 
			{
				if(obj is BaseVisualThing) 
				{
					BaseVisualThing t = obj as BaseVisualThing;
					t.SetAngle(General.ClampAngle(t.Thing.AngleDoom + increment));
				}
				else if(obj is VisualFloor) 
				{
					VisualFloor vf = obj as VisualFloor;
					vf.OnChangeTextureRotation(General.ClampAngle(vf.GetControlSector().Fields.GetValue("rotationfloor", 0.0f) + increment));
				} 
				else if(obj is VisualCeiling) 
				{
					VisualCeiling vc = obj as VisualCeiling;
					vc.OnChangeTextureRotation(General.ClampAngle(vc.GetControlSector().Fields.GetValue("rotationceiling", 0.0f) + increment));
				}
			}

			PostAction();
		}

		//mxd. Change pitch clockwise
		[BeginAction("pitchclockwise")]
		public void PitchCW() 
		{
			ChangeThingsPitch(-5);
		}

		//mxd. Change pitch counterclockwise
		[BeginAction("pitchcounterclockwise")]
		public void PitchCCW() 
		{
			ChangeThingsPitch(5);
		}

		//mxd
		private void ChangeThingsPitch(int increment) 
		{
			PreAction(UndoGroup.ThingPitchChange);

			List<IVisualEventReceiver> selection = GetSelectedObjects(false, false, true, false);
			if(selection.Count == 0) return;

			foreach(IVisualEventReceiver obj in selection) 
			{
				if(!(obj is BaseVisualThing))  continue;
				BaseVisualThing t = obj as BaseVisualThing;
				t.SetPitch(General.ClampAngle(t.Thing.Pitch + increment));
			}

			PostAction();
		}

		//mxd. Change pitch clockwise
		[BeginAction("rollclockwise")]
		public void RollCW() 
		{
			ChangeThingsRoll(-5);
		}

		//mxd. Change pitch counterclockwise
		[BeginAction("rollcounterclockwise")]
		public void RollCCW() 
		{
			ChangeThingsRoll(5);
		}

		//mxd
		private void ChangeThingsRoll(int increment) 
		{
			PreAction(UndoGroup.ThingRollChange);

			List<IVisualEventReceiver> selection = GetSelectedObjects(false, false, true, false);
			if(selection.Count == 0) return;

			foreach(IVisualEventReceiver obj in selection) 
			{
				if(!(obj is BaseVisualThing)) continue;
				BaseVisualThing t = obj as BaseVisualThing;
				t.SetRoll(General.ClampAngle(t.Thing.Roll + increment));
			}

			PostAction();
		}

		//mxd
		[BeginAction("togglegzdoomgeometryeffects")]
		public void ToggleGZDoomRenderingEffects() 
		{
            General.Settings.GZDoomRenderingEffects = !General.Settings.GZDoomRenderingEffects;
            RebuildElementData();
			UpdateChangedObjects();
            General.Interface.DisplayStatus(StatusType.Info, "(G)ZDoom geometry effects are " + (General.Settings.GZDoomRenderingEffects ? "ENABLED" : "DISABLED"));
        }

        //mxd
        [BeginAction("togglecolormaps")]
        public void ToggleColormaps()
        {
            General.Settings.ShowColormaps = !General.Settings.ShowColormaps;
            RebuildElementData();
            //Ugly hack #1354: Get all Things to update so that their colormap is updated.
            foreach (KeyValuePair<Thing, VisualThing> vt in allthings)
            {
                if (vt.Value != null)
                {
                    BaseVisualThing bvt = vt.Value as BaseVisualThing;
                    bvt.Changed = true;
                }
            }

            UpdateChangedObjects();
            General.Interface.DisplayStatus(StatusType.Info, "Colormap rendering is " + (General.Settings.ShowColormaps ? "ENABLED" : "DISABLED"));
        }

        //mxd
        [BeginAction("thingaligntowall")]
		public void AlignThingsToWall() 
		{
			List<VisualThing> visualThings = GetSelectedVisualThings(true);

			if(visualThings.Count == 0) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "This action requires selected Things!");
				return;
			}

			List<Thing> things = new List<Thing>();

			foreach(VisualThing vt in visualThings)
				things.Add(vt.Thing);

			// Make undo
			if(things.Count > 1) 
			{
				General.Map.UndoRedo.CreateUndo("Align " + things.Count + " things");
				General.Interface.DisplayStatus(StatusType.Action, "Aligned " + things.Count + " things.");
			} 
			else 
			{
				General.Map.UndoRedo.CreateUndo("Align thing");
				General.Interface.DisplayStatus(StatusType.Action, "Aligned a thing.");
			}

			//align things
			int thingsCount = General.Map.Map.Things.Count;

			foreach(Thing t in things) 
			{
				List<Linedef> excludedLines = new List<Linedef>();
				bool aligned;

				do 
				{
					Linedef l = General.Map.Map.NearestLinedef(t.Position, excludedLines);
					aligned = Tools.TryAlignThingToLine(t, l);

					if(!aligned) 
					{
						excludedLines.Add(l);

						if(excludedLines.Count == thingsCount) 
						{
							ThingTypeInfo tti = General.Map.Data.GetThingInfo(t.SRB2Type);
							General.ErrorLogger.Add(ErrorType.Warning, "Unable to align Thing ?" + t.Index + " (" + tti.Title + ") to any linedef in a map!");
							aligned = true;
						}
					}
				} while(!aligned);
			}

			//apply changes to Visual Things
			for(int i = 0; i < visualThings.Count; i++) 
			{
				BaseVisualThing t = visualThings[i] as BaseVisualThing;
				t.Changed = true;

				// Update what must be updated
				ThingData td = GetThingData(t.Thing);
				foreach(KeyValuePair<Sector, bool> s in td.UpdateAlso) 
				{
					if(VisualSectorExists(s.Key)) 
					{
						BaseVisualSector vs = (BaseVisualSector)GetVisualSector(s.Key);
						vs.UpdateSectorGeometry(s.Value);
					}
				}
			}

			UpdateChangedObjects();
			ShowTargetInfo();
		}

		//mxd
		[BeginAction("lookthroughthing")]
		public void LookThroughThing() 
		{
			List<VisualThing> visualThings = GetSelectedVisualThings(true);

			if(visualThings.Count != 1) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Look Through Selection action requires 1 selected Thing!");
				return;
			}

			//set position and angles
			Thing t = visualThings[0].Thing;
			if((t.SRB2Type == 9072 || t.SRB2Type == 9073) && t.Args[3] > 0) //AimingCamera or MovingCamera with target?
			{ 
				//position
				if(t.SRB2Type == 9072 && (t.Args[0] > 0 || t.Args[1] > 0)) //positon MovingCamera at targeted interpolation point
				{ 
					int ipTag = t.Args[0] + (t.Args[1] << 8);
					Thing ip = null;

					//find interpolation point
					foreach(Thing tgt in General.Map.Map.Things) 
					{
						if(tgt.Tag == ipTag && tgt.SRB2Type == 9070) 
						{
							ip = tgt;
							break;
						}
					}

					if(ip != null) 
					{
						VisualThing vTarget = !VisualThingExists(ip) ? CreateVisualThing(ip) : GetVisualThing(ip);
						Vector3D targetPos;
						if(vTarget == null) 
						{
							targetPos = ip.Position;
							if(ip.Sector != null) targetPos.z += ip.Sector.FloorHeight;
						} 
						else 
						{
							targetPos = vTarget.CenterV3D;
						}

						General.Map.VisualCamera.Position = targetPos; //position at interpolation point
					} 
					else 
					{
						General.Map.VisualCamera.Position = visualThings[0].CenterV3D; //position at camera
					}

				}
				else
				{
					General.Map.VisualCamera.Position = visualThings[0].CenterV3D; //position at camera
				}

				//angle
				Thing target = null;

				foreach(Thing tgt in General.Map.Map.Things) 
				{
					if(tgt.Tag == t.Args[3]) 
					{
						target = tgt;
						break;
					}
				}

				if(target == null) 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Camera target with Tag " + t.Args[3] + " does not exist!");
					General.Map.VisualCamera.AngleXY = t.Angle - Angle2D.PI;
					General.Map.VisualCamera.AngleZ = Angle2D.PI;
				} 
				else 
				{
					VisualThing vTarget = !VisualThingExists(target) ? CreateVisualThing(target) : GetVisualThing(target);
					Vector3D targetPos;
					if(vTarget == null) 
					{
						targetPos = target.Position;
						if(target.Sector != null) targetPos.z += target.Sector.FloorHeight;
					} 
					else 
					{
						targetPos = vTarget.CenterV3D;
					}

					bool pitch = (t.Args[2] & 4) != 0;
					Vector3D delta = General.Map.VisualCamera.Position - targetPos;
					General.Map.VisualCamera.AngleXY = delta.GetAngleXY();
					General.Map.VisualCamera.AngleZ = pitch ? -delta.GetAngleZ() : Angle2D.PI;
				}
			} 
			else if((t.SRB2Type == 9025 || t.SRB2Type == 9073 || t.SRB2Type == 9070) && t.Args[0] != 0) //InterpolationPoint, SecurityCamera or AimingCamera with pitch?
			{ 
				General.Map.VisualCamera.Position = visualThings[0].CenterV3D; //position at camera
				General.Map.VisualCamera.AngleXY = t.Angle - Angle2D.PI;
				General.Map.VisualCamera.AngleZ = Angle2D.PI + Angle2D.DegToRad(t.Args[0]);
			} 
			else //nope, just a generic thing
			{ 
				General.Map.VisualCamera.Position = visualThings[0].CenterV3D; //position at thing
				General.Map.VisualCamera.AngleXY = t.Angle - Angle2D.PI;
				General.Map.VisualCamera.AngleZ = Angle2D.PI;
			}
		}

		//mxd
		[BeginAction("toggleslope")]
		public void ToggleSlope() 
		{
			List<VisualGeometry> selection = GetSelectedSurfaces();

			if(selection.Count == 0) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Toggle Slope action requires selected surfaces!");
				return;
			}

			List<BaseVisualSector> toUpdate = new List<BaseVisualSector>();
			General.Map.UndoRedo.CreateUndo("Toggle Slope");

			//check selection
			foreach(VisualGeometry vg in selection) 
			{
				bool update = false;

				//assign/remove action
				if(vg.GeometryType == VisualGeometryType.WALL_LOWER)
				{
                    // MascaraSnake: Slope handling
					if(vg.Sidedef.Line.Action == 0 || (vg.Sidedef.Line.IsRegularSlope && vg.Sidedef.Line.Args[0] == 0))
					{
						//check if the sector already has floor slopes
						foreach(Sidedef side in vg.Sidedef.Sector.Sidedefs) 
						{
							if(side == vg.Sidedef || !side.Line.IsRegularSlope) continue;

							int arg = (side == side.Line.Front ? 1 : 2);

							if(side.Line.Args[0] == arg) 
							{
                                //if only floor is affected, remove action
                                if (side.Line.Args[1] == 0)
                                    side.Line.Action = 0;
                                else { //clear floor alignment
                                    side.Line.Args[0] = 0;
                                    side.Line.SetSlopeTypeFromArgs(); //MascaraSnake: Arguments changed, so update slope type
                                }
							}
						}

						//set action
						vg.Sidedef.Line.Args[0] = (vg.Sidedef == vg.Sidedef.Line.Front ? 1 : 2);
                        vg.Sidedef.Line.SetSlopeTypeFromArgs(); //MascaraSnake: Set slope type
                        update = true;
					}
				} 
				else if(vg.GeometryType == VisualGeometryType.WALL_UPPER) 
				{
                    // MascaraSnake: Slope handling
                    if (vg.Sidedef.Line.Action == 0 || (vg.Sidedef.Line.IsRegularSlope && vg.Sidedef.Line.Args[1] == 0)) 
					{
						//check if the sector already has ceiling slopes
						foreach(Sidedef side in vg.Sidedef.Sector.Sidedefs) 
						{
                            if (side == vg.Sidedef || !side.Line.IsRegularSlope) continue;

							int arg = (side == side.Line.Front ? 1 : 2);

							if(side.Line.Args[1] == arg) 
							{
                                //if only ceiling is affected, remove action
                                if (side.Line.Args[0] == 0)
                                    side.Line.Action = 0;
                                else { //clear ceiling alignment
                                    side.Line.Args[1] = 0;
                                    side.Line.SetSlopeTypeFromArgs(); //MascaraSnake: Arguments changed, so update slope type
                                }
							}
						}

						//set action
						vg.Sidedef.Line.Args[1] = (vg.Sidedef == vg.Sidedef.Line.Front ? 1 : 2);
                        vg.Sidedef.Line.SetSlopeTypeFromArgs(); //MascaraSnake: Set slope type
                        update = true;
					}
				} 
				else if(vg.GeometryType == VisualGeometryType.CEILING) 
				{
					//check if the sector has ceiling slopes
					foreach(Sidedef side in vg.Sector.Sector.Sidedefs) 
					{
                        // MascaraSnake: Slope handling
                        if (!side.Line.IsRegularSlope)	continue;

						int arg = (side == side.Line.Front ? 1 : 2);

						if(side.Line.Args[1] == arg) 
						{
                            //if only ceiling is affected, remove action
                            if (side.Line.Args[0] == 0)
                                side.Line.Action = 0;
                            else { //clear ceiling alignment
                                side.Line.Args[1] = 0;
                                side.Line.SetSlopeTypeFromArgs(); //MascaraSnake: Arguments changed, so update slope type
                            }

                            update = true;
						}
					}
				} 
				else if(vg.GeometryType == VisualGeometryType.FLOOR) 
				{
					//check if the sector has floor slopes
					foreach(Sidedef side in vg.Sector.Sector.Sidedefs) 
					{
                        // MascaraSnake: Slope handling
                        if (!side.Line.IsRegularSlope)	continue;

						int arg = (side == side.Line.Front ? 1 : 2);

						if(side.Line.Args[0] == arg) 
						{
							//if only floor is affected, remove action
							if(side.Line.Args[1] == 0)
								side.Line.Action = 0;
							else { //clear floor alignment
								side.Line.Args[0] = 0;
                                side.Line.SetSlopeTypeFromArgs(); //MascaraSnake: Arguments changed, so update slope type
                            }
                            update = true;
						}
					}
				}

				//add to update list
				if(update) toUpdate.Add(vg.Sector as BaseVisualSector);
			}

			//update changed geometry
			if(toUpdate.Count > 0) 
			{
				RebuildElementData();

				foreach(BaseVisualSector vs in toUpdate)
					vs.UpdateSectorGeometry(true);

				UpdateChangedObjects();
				ClearSelection();
				ShowTargetInfo();
			}

			General.Interface.DisplayStatus(StatusType.Action, "Toggled Slope for " + toUpdate.Count + (toUpdate.Count == 1 ? " surface." : " surfaces."));
		}
		
		#endregion

		#region ================== Texture Alignment

		//mxd. If checkSelectedSidedefParts is set to true, only selected linedef parts will be aligned (when a sidedef has both top and bottom parts, but only bottom is selected, top texture won't be aligned)
		internal void AutoAlignTextures(BaseVisualGeometrySidedef start, ImageData texture, bool alignx, bool aligny, bool resetsidemarks, bool checkSelectedSidedefParts) 
		{
			if(General.Map.UDMF)
				AutoAlignTexturesUDMF(start, texture, alignx, aligny, resetsidemarks, checkSelectedSidedefParts);
			else
				AutoAlignTextures(start, texture, alignx, aligny, resetsidemarks);
		}

		//mxd. Moved here from Tools
		// This performs texture alignment along all walls that match with the same texture
		// NOTE: This method uses the sidedefs marking to indicate which sides have been aligned
		// When resetsidemarks is set to true, all sidedefs will first be marked false (not aligned).
		// Setting resetsidemarks to false is usefull to align only within a specific selection
		// (set the marked property to true for the sidedefs outside the selection)
		private void AutoAlignTextures(BaseVisualGeometrySidedef start, ImageData texture, bool alignx, bool aligny, bool resetsidemarks) 
		{
			Stack<SidedefAlignJob> todo = new Stack<SidedefAlignJob>(50);
			float scalex = (General.Map.Config.ScaledTextureOffsets && !texture.WorldPanning) ? texture.Scale.x : 1.0f;
			float scaley = (General.Map.Config.ScaledTextureOffsets && !texture.WorldPanning) ? texture.Scale.y : 1.0f;

			// Mark all sidedefs false (they will be marked true when the texture is aligned).
			if(resetsidemarks) General.Map.Map.ClearMarkedSidedefs(false);

			// Begin with first sidedef
			SidedefAlignJob first = new SidedefAlignJob();
			first.sidedef = start.Sidedef;
			first.offsetx = start.Sidedef.OffsetX;
			int ystartalign = start.Sidedef.OffsetY; //mxd

			//mxd
			if(start.GeometryType == VisualGeometryType.WALL_MIDDLE_3D) 
			{
				first.controlSide = start.GetControlLinedef().Front;
                if (General.Map.SRB2) ystartalign = first.controlSide.OffsetY;                    
                else
                {
                    first.offsetx += first.controlSide.OffsetX;
                    ystartalign += first.controlSide.OffsetY;
                }
            } 
			else 
			{
				first.controlSide = start.Sidedef;
			}

			first.forward = true;
			todo.Push(first);

			// Continue until nothing more to align
			while(todo.Count > 0) 
			{
				// Get the align job to do
				SidedefAlignJob j = todo.Pop();

				if(j.forward) 
				{
                    // Apply alignment
                    if (alignx)
                    {
                        if (General.Map.SRB2) j.sidedef.OffsetX = (int)j.offsetx;
                        else j.controlSide.OffsetX = (int)j.offsetx;
                    }
                    if (aligny)
                    {
                        int newOffsetY = (int)Math.Round((first.ceilingHeight - j.ceilingHeight) / scaley) + ystartalign;
                        if (General.Map.SRB2) j.controlSide.OffsetY = newOffsetY;
                        else j.sidedef.OffsetY = newOffsetY;
                    }
					int forwardoffset = (int)j.offsetx + (int)Math.Round(j.sidedef.Line.Length / scalex);
					int backwardoffset = (int)j.offsetx;

					j.sidedef.Marked = true;

					// Wrap the value within the width of the texture (to prevent ridiculous values)
					// NOTE: We don't use ScaledWidth here because the texture offset is in pixels, not mappixels
					if(texture.IsImageLoaded && Tools.SidedefTextureMatch(j.sidedef, texture.LongName)) 
					{
                        if (alignx)
                        {
                            int repeatmidtexoffset = General.Map.SRB2 && j.sidedef.Line.IsFlagSet(General.Map.Config.RepeatMidtextureFlag) ? (j.sidedef.OffsetX / 4096) * 4096 : 0;
                            j.sidedef.OffsetX %= texture.Width;
                            j.sidedef.OffsetX += repeatmidtexoffset;
                        }
                        if (aligny)
                        {
                            if (General.Map.SRB2) j.controlSide.OffsetY %= texture.Height;
                            else j.sidedef.OffsetY %= texture.Height;
                        }
					}

					// Add sidedefs forward (connected to the right vertex)
					Vertex v = j.sidedef.IsFront ? j.sidedef.Line.End : j.sidedef.Line.Start;
					AddSidedefsForAlignment(todo, v, true, forwardoffset, 1.0f, texture.LongName, false);

					// Add sidedefs backward (connected to the left vertex)
					v = j.sidedef.IsFront ? j.sidedef.Line.Start : j.sidedef.Line.End;
					AddSidedefsForAlignment(todo, v, false, backwardoffset, 1.0f, texture.LongName, false);
				} 
				else 
				{
                    // Apply alignment
                    if (alignx)
                    {
                        int newOffsetX = (int)j.offsetx - (int)Math.Round(j.sidedef.Line.Length / scalex);
                        if (General.Map.SRB2) j.sidedef.OffsetX = newOffsetX;
                        else j.controlSide.OffsetX = newOffsetX;
                    }
                    if (aligny)
                    {
                        int newOffsetY = (int)Math.Round((first.ceilingHeight - j.ceilingHeight) / scaley) + ystartalign;
                        if (General.Map.SRB2) j.controlSide.OffsetY = newOffsetY;
                        else j.sidedef.OffsetY = newOffsetY;
                    }
					int forwardoffset = (int)j.offsetx;
					int backwardoffset = (int)j.offsetx - (int)Math.Round(j.sidedef.Line.Length / scalex);

					j.sidedef.Marked = true;

					// Wrap the value within the width of the texture (to prevent ridiculous values)
					// NOTE: We don't use ScaledWidth here because the texture offset is in pixels, not mappixels
					if(texture.IsImageLoaded && Tools.SidedefTextureMatch(j.sidedef, texture.LongName)) 
					{
						if(alignx) j.sidedef.OffsetX %= texture.Width;
                        if (aligny)
                        {
                            if (General.Map.SRB2) j.controlSide.OffsetY %= texture.Height;
                            else j.sidedef.OffsetY %= texture.Height;
                        }
					}

					// Add sidedefs backward (connected to the left vertex)
					Vertex v = j.sidedef.IsFront ? j.sidedef.Line.Start : j.sidedef.Line.End;
					AddSidedefsForAlignment(todo, v, false, backwardoffset, 1.0f, texture.LongName, false);

					// Add sidedefs forward (connected to the right vertex)
					v = j.sidedef.IsFront ? j.sidedef.Line.End : j.sidedef.Line.Start;
					AddSidedefsForAlignment(todo, v, true, forwardoffset, 1.0f, texture.LongName, false);
				}
			}
		}

		//mxd. Moved here from GZDoomEditing plugin
		// This performs UDMF texture alignment along all walls that match with the same texture
		// NOTE: This method uses the sidedefs marking to indicate which sides have been aligned
		// When resetsidemarks is set to true, all sidedefs will first be marked false (not aligned).
		// Setting resetsidemarks to false is usefull to align only within a specific selection
		// (set the marked property to true for the sidedefs outside the selection)
		private void AutoAlignTexturesUDMF(BaseVisualGeometrySidedef start, ImageData texture, bool alignx, bool aligny, bool resetsidemarks, bool checkSelectedSidedefParts) 
		{
			// Mark all sidedefs false (they will be marked true when the texture is aligned)
			if(resetsidemarks) General.Map.Map.ClearMarkedSidedefs(false);
			if(!texture.IsImageLoaded) return;

			Stack<SidedefAlignJob> todo = new Stack<SidedefAlignJob>(50);
			float scalex = (General.Map.Config.ScaledTextureOffsets && !texture.WorldPanning) ? texture.Scale.x : 1.0f;
			float scaley = (General.Map.Config.ScaledTextureOffsets && !texture.WorldPanning) ? texture.Scale.y : 1.0f;

			SidedefAlignJob first = new SidedefAlignJob();
			first.sidedef = start.Sidedef;
			first.offsetx = start.Sidedef.OffsetX;

			if(start.GeometryType == VisualGeometryType.WALL_MIDDLE_3D)
				first.controlSide = start.GetControlLinedef().Front;
			else
				first.controlSide = start.Sidedef;

			//mxd
			List<BaseVisualGeometrySidedef> selectedVisualSides = new List<BaseVisualGeometrySidedef>();
			if(checkSelectedSidedefParts && !singleselection) 
			{
				foreach(IVisualEventReceiver i in selectedobjects) 
				{
					if(i is BaseVisualGeometrySidedef) 
					{
						BaseVisualGeometrySidedef sd = i as BaseVisualGeometrySidedef;
						if(!selectedVisualSides.Contains(sd)) selectedVisualSides.Add(sd);
					}
				}
			}
			
			//mxd. Scale
			switch(start.GeometryType) 
			{
				case VisualGeometryType.WALL_UPPER:
					first.scaleX = start.Sidedef.Fields.GetValue("scalex_top", 1.0f);
					first.scaleY = start.Sidedef.Fields.GetValue("scaley_top", 1.0f);
					break;
				case VisualGeometryType.WALL_MIDDLE:
				case VisualGeometryType.WALL_MIDDLE_3D:
					first.scaleX = first.controlSide.Fields.GetValue("scalex_mid", 1.0f);
					first.scaleY = first.controlSide.Fields.GetValue("scaley_mid", 1.0f);
					break;
				case VisualGeometryType.WALL_LOWER:
					first.scaleX = start.Sidedef.Fields.GetValue("scalex_bottom", 1.0f);
					first.scaleY = start.Sidedef.Fields.GetValue("scaley_bottom", 1.0f);
					break;
			}

			// Determine the Y alignment
			float ystartalign = start.Sidedef.OffsetY;
			switch(start.GeometryType) 
			{
				case VisualGeometryType.WALL_UPPER:
					ystartalign += Tools.GetSidedefTopOffsetY(start.Sidedef, start.Sidedef.Fields.GetValue("offsety_top", 0.0f), first.scaleY / scaley, false);//mxd
					break;
				case VisualGeometryType.WALL_MIDDLE:
					ystartalign += Tools.GetSidedefMiddleOffsetY(start.Sidedef, start.Sidedef.Fields.GetValue("offsety_mid", 0.0f), first.scaleY / scaley, false);//mxd
					break;
				case VisualGeometryType.WALL_MIDDLE_3D: //mxd. 3d-floors are not affected by Lower/Upper unpegged flags
					ystartalign += first.controlSide.OffsetY - (start.Sidedef.Sector.CeilHeight - first.ceilingHeight);
					ystartalign += start.Sidedef.Fields.GetValue("offsety_mid", 0.0f);
					ystartalign += first.controlSide.Fields.GetValue("offsety_mid", 0.0f);
					break;
				case VisualGeometryType.WALL_LOWER:
					ystartalign += Tools.GetSidedefBottomOffsetY(start.Sidedef, start.Sidedef.Fields.GetValue("offsety_bottom", 0.0f), first.scaleY / scaley, false);//mxd
					break;
			}

			// Begin with first sidedef
			switch(start.GeometryType) 
			{
				case VisualGeometryType.WALL_UPPER:
					first.offsetx += start.Sidedef.Fields.GetValue("offsetx_top", 0.0f);
					break;
				case VisualGeometryType.WALL_MIDDLE:
					first.offsetx += start.Sidedef.Fields.GetValue("offsetx_mid", 0.0f);
					break;
				case VisualGeometryType.WALL_MIDDLE_3D: //mxd. Yup, 4 sets of texture offsets are used
					first.offsetx += start.Sidedef.Fields.GetValue("offsetx_mid", 0.0f);
					first.offsetx += first.controlSide.OffsetX;
					first.offsetx += first.controlSide.Fields.GetValue("offsetx_mid", 0.0f);
					break;
				case VisualGeometryType.WALL_LOWER:
					first.offsetx += start.Sidedef.Fields.GetValue("offsetx_bottom", 0.0f);
					break;
			}
			first.forward = true;
			todo.Push(first);

			// Continue until nothing more to align
			while(todo.Count > 0) 
			{
				Vertex v;
				float forwardoffset;
				float backwardoffset;

				// Get the align job to do
				SidedefAlignJob j = todo.Pop();

				bool matchtop = (!j.sidedef.Marked && (!singleselection || j.sidedef.LongHighTexture == texture.LongName) && j.sidedef.HighRequired());
				bool matchbottom = (!j.sidedef.Marked && (!singleselection || j.sidedef.LongLowTexture == texture.LongName) && j.sidedef.LowRequired());
				bool matchmid = ((!singleselection || j.controlSide.LongMiddleTexture == texture.LongName) && (j.controlSide.MiddleRequired() || j.controlSide.LongMiddleTexture != MapSet.EmptyLongName)); //mxd

				//mxd. If there's a selection, check if matched part is actually selected
				if(checkSelectedSidedefParts && !singleselection) 
				{
					if(matchtop) matchtop = SidePartIsSelected(selectedVisualSides, j.sidedef, VisualGeometryType.WALL_UPPER);
					if(matchbottom) matchbottom = SidePartIsSelected(selectedVisualSides, j.sidedef, VisualGeometryType.WALL_LOWER);
					if(matchmid) matchmid = SidePartIsSelected(selectedVisualSides, j.sidedef, VisualGeometryType.WALL_MIDDLE) || 
						SidePartIsSelected(selectedVisualSides, j.sidedef, VisualGeometryType.WALL_MIDDLE_3D);
				}

				if(!matchbottom && !matchtop && !matchmid) continue; //mxd

				//mxd. We want to skip realigning of the starting wall part
				if(matchtop) matchtop = (j.sidedef != start.Sidedef || start.GeometryType != VisualGeometryType.WALL_UPPER);
				if(matchmid) matchmid = (j.sidedef != start.Sidedef || (start.GeometryType != VisualGeometryType.WALL_MIDDLE && start.GeometryType != VisualGeometryType.WALL_MIDDLE_3D));
				if(matchbottom) matchbottom = (j.sidedef != start.Sidedef || start.GeometryType != VisualGeometryType.WALL_LOWER);

				if(matchbottom || matchtop || matchmid)
				{
					j.sidedef.Fields.BeforeFieldsChange();
					if(j.sidedef.Index != j.controlSide.Index) j.controlSide.Fields.BeforeFieldsChange(); //mxd
				}
				
				//mxd. Apply Scale
				if(matchtop)
				{
					UniFields.SetFloat(j.sidedef.Fields, "scalex_top", first.scaleX, 1.0f);
					UniFields.SetFloat(j.sidedef.Fields, "scaley_top", j.scaleY, 1.0f);
				}
				if(matchmid)
				{
					UniFields.SetFloat(j.controlSide.Fields, "scalex_mid", first.scaleX, 1.0f);
					UniFields.SetFloat(j.controlSide.Fields, "scaley_mid", j.scaleY, 1.0f);
				}
				if(matchbottom)
				{
					UniFields.SetFloat(j.sidedef.Fields, "scalex_bottom", first.scaleX, 1.0f);
					UniFields.SetFloat(j.sidedef.Fields, "scaley_bottom", j.scaleY, 1.0f);
				}

				if(j.forward) 
				{
					// Apply alignment
					if(alignx) 
					{
						float offset = j.offsetx;
						offset -= j.sidedef.OffsetX;

						if(matchtop)
							j.sidedef.Fields["offsetx_top"] = new UniValue(UniversalType.Float, (float)Math.Round(offset % General.Map.Data.GetTextureImage(j.sidedef.LongHighTexture).Width, General.Map.FormatInterface.VertexDecimals));
						if(matchbottom)
							j.sidedef.Fields["offsetx_bottom"] = new UniValue(UniversalType.Float, (float)Math.Round(offset % General.Map.Data.GetTextureImage(j.sidedef.LongLowTexture).Width, General.Map.FormatInterface.VertexDecimals));
						if(matchmid) 
						{
							if(j.sidedef.Index != j.controlSide.Index) //mxd. if it's a part of 3d-floor 
							{ 
								offset -= j.controlSide.OffsetX;
								offset -= j.controlSide.Fields.GetValue("offsetx_mid", 0.0f);
							}

							j.sidedef.Fields["offsetx_mid"] = new UniValue(UniversalType.Float, (float)Math.Round(offset % General.Map.Data.GetTextureImage(j.controlSide.LongMiddleTexture).Width, General.Map.FormatInterface.VertexDecimals));
						}
					}

					if(aligny) 
					{
						float offset = ((start.Sidedef.Sector.CeilHeight - j.ceilingHeight) / scaley) * j.scaleY + ystartalign; //mxd
						offset -= j.sidedef.OffsetY; //mxd
						
						if(matchtop)
							j.sidedef.Fields["offsety_top"] = new UniValue(UniversalType.Float, (float)Math.Round(Tools.GetSidedefTopOffsetY(j.sidedef, offset, j.scaleY / scaley, true) % General.Map.Data.GetTextureImage(j.sidedef.LongHighTexture).Height, General.Map.FormatInterface.VertexDecimals)); //mxd
						if(matchbottom)
							j.sidedef.Fields["offsety_bottom"] = new UniValue(UniversalType.Float, (float)Math.Round(Tools.GetSidedefBottomOffsetY(j.sidedef, offset, j.scaleY / scaley, true) % General.Map.Data.GetTextureImage(j.sidedef.LongLowTexture).Height, General.Map.FormatInterface.VertexDecimals)); //mxd
						if(matchmid) 
						{
							//mxd. Side is part of a 3D floor?
							if(j.sidedef.Index != j.controlSide.Index) 
							{
								offset -= j.controlSide.OffsetY;
								offset -= j.controlSide.Fields.GetValue("offsety_mid", 0.0f);
								j.sidedef.Fields["offsety_mid"] = new UniValue(UniversalType.Float, (float)Math.Round(offset % General.Map.Data.GetTextureImage(j.controlSide.LongMiddleTexture).Height, General.Map.FormatInterface.VertexDecimals));
							} 
							else
							{
								ImageData midtex = General.Map.Data.GetTextureImage(j.sidedef.LongMiddleTexture);
								offset = Tools.GetSidedefMiddleOffsetY(j.sidedef, offset, j.scaleY / scaley, true);

								bool startisnonwrappedmidtex = (start.Sidedef.Other != null && start.GeometryType == VisualGeometryType.WALL_MIDDLE && !start.Sidedef.IsFlagSet("wrapmidtex") && !start.Sidedef.Line.IsFlagSet("wrapmidtex"));
								bool cursideisnonwrappedmidtex = (j.sidedef.Other != null && !j.sidedef.IsFlagSet("wrapmidtex") && !j.sidedef.Line.IsFlagSet("wrapmidtex"));

								//mxd. Only clamp when the texture is wrapped 
								if(!cursideisnonwrappedmidtex) offset %= midtex.Height;

								if(!startisnonwrappedmidtex && cursideisnonwrappedmidtex)
								{
									//mxd. This should be doublesided non-wrapped line. Find the nearset aligned position
									float curoffset = UniFields.GetFloat(j.sidedef.Fields, "offsety_mid") + j.sidedef.OffsetY;
									offset += midtex.Height * (float)Math.Round(curoffset / midtex.Height - 0.5f * Math.Sign(j.scaleY));

									// Make sure the surface stays between floor and ceiling
									if(j.sidedef.Line.IsFlagSet(General.Map.Config.LowerUnpeggedFlag) || Math.Sign(j.scaleY) == -1)
									{
										if(offset < -midtex.Height)
											offset += midtex.Height;
										else if(offset > j.sidedef.GetMiddleHeight())
											offset -= midtex.Height;
									}
									else 
									{
										if(offset < -(j.sidedef.GetMiddleHeight() + midtex.Height))
											offset += midtex.Height;
										else if(offset > midtex.Height)
											offset -= midtex.Height;
									}
								}

								j.sidedef.Fields["offsety_mid"] = new UniValue(UniversalType.Float, (float)Math.Round(offset, General.Map.FormatInterface.VertexDecimals)); //mxd
							}
						}
					}

					forwardoffset = j.offsetx + (float)Math.Round(j.sidedef.Line.Length / scalex * first.scaleX, General.Map.FormatInterface.VertexDecimals);
					backwardoffset = j.offsetx;

					// Done this sidedef
					j.sidedef.Marked = true;
					j.controlSide.Marked = true;

					// Add sidedefs backward (connected to the left vertex)
					v = j.sidedef.IsFront ? j.sidedef.Line.Start : j.sidedef.Line.End;
					AddSidedefsForAlignment(todo, v, false, backwardoffset, j.scaleY, texture.LongName, true);

					// Add sidedefs forward (connected to the right vertex)
					v = j.sidedef.IsFront ? j.sidedef.Line.End : j.sidedef.Line.Start;
					AddSidedefsForAlignment(todo, v, true, forwardoffset, j.scaleY, texture.LongName, true);
				} 
				else 
				{
					// Apply alignment
					if(alignx) 
					{
						float offset = j.offsetx - (float)Math.Round(j.sidedef.Line.Length / scalex * first.scaleX, General.Map.FormatInterface.VertexDecimals);
						offset -= j.sidedef.OffsetX;

						if(matchtop)
							j.sidedef.Fields["offsetx_top"] = new UniValue(UniversalType.Float, (float)Math.Round(offset % General.Map.Data.GetTextureImage(j.sidedef.LongHighTexture).Width, General.Map.FormatInterface.VertexDecimals));
						if(matchbottom)
							j.sidedef.Fields["offsetx_bottom"] = new UniValue(UniversalType.Float, (float)Math.Round(offset % General.Map.Data.GetTextureImage(j.sidedef.LongLowTexture).Width, General.Map.FormatInterface.VertexDecimals));
						if(matchmid) 
						{
							if(j.sidedef.Index != j.controlSide.Index) //mxd
							{ 
								offset -= j.controlSide.OffsetX;
								offset -= j.controlSide.Fields.GetValue("offsetx_mid", 0.0f);
							}

							j.sidedef.Fields["offsetx_mid"] = new UniValue(UniversalType.Float, (float)Math.Round(offset % General.Map.Data.GetTextureImage(j.controlSide.LongMiddleTexture).Width, General.Map.FormatInterface.VertexDecimals));
						}
					}

					if(aligny) 
					{
						float offset = ((start.Sidedef.Sector.CeilHeight - j.ceilingHeight) / scaley) * j.scaleY + ystartalign; //mxd
						offset -= j.sidedef.OffsetY; //mxd

						if(matchtop)
							j.sidedef.Fields["offsety_top"] = new UniValue(UniversalType.Float, (float)Math.Round(Tools.GetSidedefTopOffsetY(j.sidedef, offset, j.scaleY / scaley, true) % General.Map.Data.GetTextureImage(j.sidedef.LongHighTexture).Height, General.Map.FormatInterface.VertexDecimals)); //mxd
						if(matchbottom)
							j.sidedef.Fields["offsety_bottom"] = new UniValue(UniversalType.Float, (float)Math.Round(Tools.GetSidedefBottomOffsetY(j.sidedef, offset, j.scaleY / scaley, true) % General.Map.Data.GetTextureImage(j.sidedef.LongLowTexture).Height, General.Map.FormatInterface.VertexDecimals)); //mxd
						if(matchmid) 
						{
							//mxd. Side is part of a 3D floor?
							if(j.sidedef.Index != j.controlSide.Index) 
							{
								offset -= j.controlSide.OffsetY;
								offset -= j.controlSide.Fields.GetValue("offsety_mid", 0.0f);
								j.sidedef.Fields["offsety_mid"] = new UniValue(UniversalType.Float, (float)Math.Round(offset % General.Map.Data.GetTextureImage(j.controlSide.LongMiddleTexture).Height, General.Map.FormatInterface.VertexDecimals)); //mxd
							} 
							else 
							{
								ImageData midtex = General.Map.Data.GetTextureImage(j.sidedef.LongMiddleTexture);
								offset = Tools.GetSidedefMiddleOffsetY(j.sidedef, offset, j.scaleY / scaley, true);

								bool startisnonwrappedmidtex = (start.Sidedef.Other != null && start.GeometryType == VisualGeometryType.WALL_MIDDLE && !start.Sidedef.IsFlagSet("wrapmidtex") && !start.Sidedef.Line.IsFlagSet("wrapmidtex"));
								bool cursideisnonwrappedmidtex = (j.sidedef.Other != null && !j.sidedef.IsFlagSet("wrapmidtex") && !j.sidedef.Line.IsFlagSet("wrapmidtex"));

								//mxd. Only clamp when the texture is wrapped 
								if(!cursideisnonwrappedmidtex) offset %= midtex.Height;

								if(!startisnonwrappedmidtex && cursideisnonwrappedmidtex) 
								{
									//mxd. This should be doublesided non-wrapped line. Find the nearset aligned position
									float curoffset = UniFields.GetFloat(j.sidedef.Fields, "offsety_mid") + j.sidedef.OffsetY;
									offset += midtex.Height * (float)Math.Round(curoffset / midtex.Height - 0.5f * Math.Sign(j.scaleY));

									// Make sure the surface stays between floor and ceiling
									if(j.sidedef.Line.IsFlagSet(General.Map.Config.LowerUnpeggedFlag) || Math.Sign(j.scaleY) == -1) 
									{
										if(offset < -midtex.Height)
											offset += midtex.Height;
										else if(offset > j.sidedef.GetMiddleHeight())
											offset -= midtex.Height;
									} 
									else 
									{
										if(offset < -(j.sidedef.GetMiddleHeight() + midtex.Height))
											offset += midtex.Height;
										else if(offset > midtex.Height)
											offset -= midtex.Height;
									}
								}

								j.sidedef.Fields["offsety_mid"] = new UniValue(UniversalType.Float, (float)Math.Round(offset, General.Map.FormatInterface.VertexDecimals)); //mxd
							}
						}
					}

					forwardoffset = j.offsetx;
					backwardoffset = j.offsetx - (float)Math.Round(j.sidedef.Line.Length / scalex * first.scaleX, General.Map.FormatInterface.VertexDecimals);

					// Done this sidedef
					j.sidedef.Marked = true;
					j.controlSide.Marked = true;

					// Add sidedefs forward (connected to the right vertex)
					v = j.sidedef.IsFront ? j.sidedef.Line.End : j.sidedef.Line.Start;
					AddSidedefsForAlignment(todo, v, true, forwardoffset, j.scaleY, texture.LongName, true);

					// Add sidedefs backward (connected to the left vertex)
					v = j.sidedef.IsFront ? j.sidedef.Line.Start : j.sidedef.Line.End;
					AddSidedefsForAlignment(todo, v, false, backwardoffset, j.scaleY, texture.LongName, true);
				}
			}
		}

		// This adds the matching, unmarked sidedefs from a vertex for texture alignment
		private void AddSidedefsForAlignment(Stack<SidedefAlignJob> stack, Vertex v, bool forward, float offsetx, float scaleY, long texturelongname, bool udmf) 
		{
			foreach(Linedef ld in v.Linedefs) 
			{
				Sidedef side1 = forward ? ld.Front : ld.Back;
				Sidedef side2 = forward ? ld.Back : ld.Front;

				if((ld.Start == v) && (side1 != null) && !side1.Marked) 
				{
					List<Sidedef> controlSides = GetControlSides(side1, udmf); //mxd

					foreach(Sidedef s in controlSides) 
					{
						if(!singleselection || Tools.SidedefTextureMatch(s, texturelongname)) 
						{
							SidedefAlignJob nj = new SidedefAlignJob();
							nj.forward = forward;
							nj.offsetx = offsetx;
							nj.scaleY = scaleY; //mxd
							nj.sidedef = side1;
							nj.controlSide = s; //mxd
							stack.Push(nj);
						}
					}
				} 
				else if((ld.End == v) && (side2 != null) && !side2.Marked) 
				{
					List<Sidedef> controlSides = GetControlSides(side2, udmf); //mxd

					foreach(Sidedef s in controlSides) 
					{
						if(!singleselection || Tools.SidedefTextureMatch(s, texturelongname)) 
						{
							SidedefAlignJob nj = new SidedefAlignJob();
							nj.forward = forward;
							nj.offsetx = offsetx;
							nj.scaleY = scaleY; //mxd
							nj.sidedef = side2;
							nj.controlSide = s; //mxd
							stack.Push(nj);
						}
					}
				}
			}
		}

		//mxd
		private static bool SidePartIsSelected(List<BaseVisualGeometrySidedef> selection, Sidedef side, VisualGeometryType geoType) 
		{
			foreach(BaseVisualGeometrySidedef vs in selection) 
				if(vs.GeometryType == geoType && vs.Sidedef.Index == side.Index) return true;
			return false;
		}

		//mxd
		private List<Sidedef> GetControlSides(Sidedef side, bool udmf) 
		{
			if(side.Other == null) return new List<Sidedef>() { side };
			if(side.Other.Sector.Tag == 0) return new List<Sidedef>() { side };

			SectorData data = GetSectorData(side.Other.Sector);
			if(data.ExtraFloors.Count == 0)	return new List<Sidedef>() { side };

			List<Sidedef> sides = new List<Sidedef>();
			foreach(Effect3DFloor ef in data.ExtraFloors)
				sides.Add(ef.Linedef.Front);

			if(udmf)
				sides.Add(side); //UDMF map format
			else
				sides.Insert(0, side); //Doom/Hexen map format: if a sidedef has lower/upper parts, they take predecence in alignment

			return sides;
		}

		#endregion
	}
}
