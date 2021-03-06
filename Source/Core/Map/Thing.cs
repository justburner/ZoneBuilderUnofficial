
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
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.GZBuilder.Data;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.VisualModes;

#endregion

namespace CodeImp.DoomBuilder.Map
{
	public sealed class Thing : SelectableElement
	{
		#region ================== Constants

		public const int NUM_ARGS = 5;

		#endregion

		#region ================== Enums

		public enum SpriteRenderMode
		{
			NORMAL,
			WALL_SPRITE,
			FLOOR_SPRITE,
			CEILING_SPRITE
		}

		#endregion

		#region ================== Variables

		// Map
		private MapSet map;

		// Sector
		private Sector sector;

		// List items
		private LinkedListNode<Thing> selecteditem;
		
		// Properties
		private int type;
		private Vector3D pos;
		private int angledoom;		// Angle as entered / stored in file
		private float anglerad;		// Angle in radians
		private Dictionary<string, bool> flags;
		private int tag;
		private int action;
		private int[] args;
		private float scaleX; //mxd
		private float scaleY; //mxd
		private SizeF spritescale; //mxd
		private int pitch; //mxd. Used in model rendering
		private int roll; //mxd. Used in model rendering
		private float pitchrad; //mxd
		private float rollrad; //mxd
		private bool ismodel; //mxd
		private bool highlighted; //mxd

		//mxd. GLOOME rendering settings
		private bool rollsprite; //mxd

		// Configuration
		private float size;
		private float height; //mxd
		private PixelColor color;
		private bool fixedsize;
		private bool directional; //mxd. If true, we need to render an arrow

		#endregion

		#region ================== Properties

		public MapSet Map { get { return map; } }
		public int FullType { get { return type; } set { BeforePropsChange(); type = value; } } //mxd
        public int SRB2Type { get { if (General.Map.SRB2) return type % 4096; else return type; } set { BeforePropsChange(); if (General.Map.SRB2) type = (type / 4096) * 4096 + value; else type = value; } }
        public int Parameter { get { if (General.Map.SRB2) return type / 4096; else return 0; } set { BeforePropsChange(); if (General.Map.SRB2) type = value * 4096 + (type % 4096); } }
        public Vector3D Position { get { return pos; } }
		public float ScaleX { get { return scaleX; } } //mxd. This is UDMF property, not actual scale!
		public float ScaleY { get { return scaleY; } } //mxd. This is UDMF property, not actual scale!
		public int Pitch { get { return pitch; } } //mxd
		public float PitchRad { get { return pitchrad; } }
		public int Roll { get { return roll; } } //mxd
		public float RollRad { get { return rollrad; } }
		public SizeF ActorScale { get { return spritescale; } } //mxd. Actor scale set in DECORATE
		public float Angle { get { return anglerad; } }
		public int AngleDoom { get { return angledoom; } }
		internal Dictionary<string, bool> Flags { get { return flags; } }
		public int Action { get { return action; } set { BeforePropsChange(); action = value; } }
		public int[] Args { get { return args; } }
		public float Size { get { return size; } }
		public float Height { get { return height; } } //mxd
		public PixelColor Color { get { return color; } }
		public bool FixedSize { get { return fixedsize; } }
		public int Tag { get { return tag; } set { BeforePropsChange(); tag = value; if((tag < General.Map.FormatInterface.MinTag) || (tag > General.Map.FormatInterface.MaxTag)) throw new ArgumentOutOfRangeException("Tag", "Invalid tag number"); } }
		public Sector Sector { get { return sector; } }
		public bool IsModel { get { return ismodel; } } //mxd
		public bool IsDirectional { get { return directional; } } //mxd
		public bool Highlighted { get { return highlighted; } set { highlighted = value; } } //mxd
        public bool IsSlopeVertex { get { return General.Map.FormatInterface.SlopeVertexType == this.SRB2Type; } }
        public bool IsFlipped
        {
            get
            {
                ThingTypeInfo ti = General.Map.Data.GetThingInfo(SRB2Type);
                return ti.Hangs ^ IsReverse;
            }
        }
        public bool IsReverse { get { return General.Map.SRB2 && !Unflippable && IsFlagSet("2"); } }
        public bool Unflippable { get { return General.Map.Data.GetThingInfo(SRB2Type).IsUnflippable; } }
        #endregion

        #region ================== Constructor / Disposer

        // Constructor
        internal Thing(MapSet map, int listindex)
		{
			// Initialize
			this.elementtype = MapElementType.THING; //mxd
			this.map = map;
			this.listindex = listindex;
			this.flags = new Dictionary<string, bool>(StringComparer.Ordinal);
			this.args = new int[NUM_ARGS];
			this.scaleX = 1.0f;
			this.scaleY = 1.0f;
			this.spritescale = new SizeF(1.0f, 1.0f);
			
			if(map == General.Map.Map)
				General.Map.UndoRedo.RecAddThing(this);
			
			// We have no destructor
			GC.SuppressFinalize(this);
		}

		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				if(map == General.Map.Map)
					General.Map.UndoRedo.RecRemThing(this);

				// Remove from main list
				map.RemoveThing(listindex);
				
				// Clean up
				map = null;
				sector = null;

				// Dispose base
				base.Dispose();
			}
		}

		#endregion

		#region ================== Management
		
		// Call this before changing properties
		protected override void BeforePropsChange()
		{
			if(map == General.Map.Map)
				General.Map.UndoRedo.RecPrpThing(this);
		}
		
		// Serialize / deserialize
		new internal void ReadWrite(IReadWriteStream s)
		{
			if(!s.IsWriting) BeforePropsChange();
			
			base.ReadWrite(s);

			if(s.IsWriting)
			{
				s.wInt(flags.Count);
				
				foreach(KeyValuePair<string, bool> f in flags)
				{
					s.wString(f.Key);
					s.wBool(f.Value);
				}
			}
			else
			{
				int c; s.rInt(out c);

				flags = new Dictionary<string, bool>(c, StringComparer.Ordinal);
				for(int i = 0; i < c; i++)
				{
					string t; s.rString(out t);
					bool b; s.rBool(out b);
					flags.Add(t, b);
				}
			}
			
			s.rwInt(ref type);
			s.rwVector3D(ref pos);
			s.rwInt(ref angledoom);
			s.rwInt(ref pitch); //mxd
			s.rwInt(ref roll); //mxd
			s.rwFloat(ref scaleX); //mxd
			s.rwFloat(ref scaleY); //mxd
			s.rwInt(ref tag);
			s.rwInt(ref action);
			for(int i = 0; i < NUM_ARGS; i++) s.rwInt(ref args[i]);

			if(!s.IsWriting) 
			{
				anglerad = Angle2D.DoomToReal(angledoom);
				UpdateCache(); //mxd
			}
		}

		// This copies all properties to another thing
		public void CopyPropertiesTo(Thing t)
		{
			t.BeforePropsChange();
			
			// Copy properties
			t.type = type;
			t.anglerad = anglerad;
			t.angledoom = angledoom;
			t.roll = roll; //mxd
			t.pitch = pitch; //mxd
			t.rollrad = rollrad; //mxd
			t.pitchrad = pitchrad; //mxd
			t.scaleX = scaleX; //mxd
			t.scaleY = scaleY; //mxd
			t.spritescale = spritescale; //mxd
			t.pos = pos;
			t.flags = new Dictionary<string,bool>(flags);
			t.tag = tag;
			t.action = action;
			t.args = (int[])args.Clone();
			t.size = size;
			t.height = height; //mxd
			t.color = color;
			t.directional = directional;
			t.fixedsize = fixedsize;
			t.ismodel = ismodel; //mxd
			t.rollsprite = rollsprite; //mxd

			base.CopyPropertiesTo(t);
		}

		// This determines which sector the thing is in and links it
		public void DetermineSector()
		{
			//mxd
			sector = map.GetSectorByCoordinates(pos);
		}

		// This determines which sector the thing is in by looking at the blockmap and links it
		public void DetermineSector(VisualBlockMap blockmap)
		{
			// Find nearest sectors using the blockmap
			List<Sector> possiblesectors = blockmap.GetBlock(blockmap.GetBlockCoordinates(pos)).Sectors;

			// Check in which sector we are
			sector = null;
			foreach(Sector s in possiblesectors)
			{
				if(s.Intersect(pos))
				{
					sector = s;
					break;
				}
			}
		}

        // This determines which sector the thing is in by looking at the BSP tree and links it
        public void DetermineSector(BSP bsp)
        {
            sector = bsp.GetSector((Vector2D)pos);
        }

        // This translates the flags into UDMF fields
        internal void TranslateToUDMF()
		{
			// First make a single integer with all flags
			int bits = 0;
			int flagbit;
			foreach(KeyValuePair<string, bool> f in flags)
				if(int.TryParse(f.Key, out flagbit) && f.Value) bits |= flagbit;

			// Now make the new flags
			flags.Clear();
			foreach(FlagTranslation f in General.Map.Config.ThingFlagsTranslation)
			{
				// Flag found in bits?
				if((bits & f.Flag) == f.Flag)
				{
					// Add fields and remove bits
					bits &= ~f.Flag;
					for(int i = 0; i < f.Fields.Count; i++)
						flags[f.Fields[i]] = f.FieldValues[i];
				}
				else
				{
					// Add fields with inverted value
					for(int i = 0; i < f.Fields.Count; i++)
						flags[f.Fields[i]] = !f.FieldValues[i];
				}
			}
		}

		// This translates UDMF fields back into the normal flags
		internal void TranslateFromUDMF()
		{
			//mxd. Clear UDMF-related properties
			this.Fields.Clear();
			scaleX = 1.0f;
			scaleY = 1.0f;
			pitch = 0;
			pitchrad = 0;
			roll = 0;
			rollrad = 0;
			
			// Make copy of the flags
			Dictionary<string, bool> oldfields = new Dictionary<string, bool>(flags);

			// Make the flags
			flags.Clear();
			foreach(KeyValuePair<string, string> f in General.Map.Config.ThingFlags)
			{
				// Flag must be numeric
				int flagbit;
				if(int.TryParse(f.Key, out flagbit))
				{
					foreach(FlagTranslation ft in General.Map.Config.ThingFlagsTranslation)
					{
						if(ft.Flag == flagbit)
						{
							// Only set this flag when the fields match
							bool fieldsmatch = true;
							for(int i = 0; i < ft.Fields.Count; i++)
							{
								if(!oldfields.ContainsKey(ft.Fields[i]) || (oldfields[ft.Fields[i]] != ft.FieldValues[i]))
								{
									fieldsmatch = false;
									break;
								}
							}

							// Field match? Then add the flag.
							if(fieldsmatch)
							{
								flags.Add(f.Key, true);
								break;
							}
						}
					}
				}
			}
		}

		// Selected
		protected override void DoSelect()
		{
			base.DoSelect();
			selecteditem = map.SelectedThings.AddLast(this);
		}

		// Deselect
		protected override void DoUnselect()
		{
			base.DoUnselect();
			if(selecteditem.List != null) selecteditem.List.Remove(selecteditem);
			selecteditem = null;
		}
		#endregion
		
		#region ================== Changes

		// This moves the thing
		// NOTE: This does not update sector! (call DetermineSector)
		public void Move(Vector3D newpos)
		{
			BeforePropsChange();
			
			// Change position
			this.pos = newpos;
			
			if(SRB2Type != General.Map.Config.Start3DModeThingType)
				General.Map.IsChanged = true;
		}

		// This moves the thing
		// NOTE: This does not update sector! (call DetermineSector)
		public void Move(Vector2D newpos)
		{
			BeforePropsChange();
			
			// Change position
			this.pos = new Vector3D(newpos.x, newpos.y, pos.z);
			
			if(SRB2Type != General.Map.Config.Start3DModeThingType)
				General.Map.IsChanged = true;
		}

		// This moves the thing
		// NOTE: This does not update sector! (call DetermineSector)
		public void Move(float x, float y, float zoffset)
		{
			BeforePropsChange();
			
			// Change position
			this.pos = new Vector3D(x, y, zoffset);
			
			if(SRB2Type != General.Map.Config.Start3DModeThingType)
				General.Map.IsChanged = true;
		}
		
		// This rotates the thing
		public void Rotate(float newangle)
		{
			BeforePropsChange();
			
			// Change angle
			this.anglerad = newangle;
			this.angledoom = Angle2D.RealToDoom(newangle);
			
			if(SRB2Type != General.Map.Config.Start3DModeThingType)
				General.Map.IsChanged = true;
		}
		
		// This rotates the thing
		public void Rotate(int newangle)
		{
			BeforePropsChange();
			
			// Change angle
			anglerad = Angle2D.DoomToReal(newangle);
			angledoom = newangle;
			
			if(SRB2Type != General.Map.Config.Start3DModeThingType)
				General.Map.IsChanged = true;
		}

		//mxd
		public void SetPitch(int newpitch)
		{
			BeforePropsChange();

			pitch = General.ClampAngle(newpitch);
			pitchrad = ((ismodel && General.Map.Data.ModeldefEntries[SRB2Type].InheritActorPitch) ? Angle2D.DegToRad(pitch) : 0);

			if(SRB2Type != General.Map.Config.Start3DModeThingType)
				General.Map.IsChanged = true;
		}

		//mxd
		public void SetRoll(int newroll)
		{
			BeforePropsChange();

			roll = General.ClampAngle(newroll);
			rollrad = ( (rollsprite || (ismodel && General.Map.Data.ModeldefEntries[SRB2Type].InheritActorRoll)) ? Angle2D.DegToRad(roll) : 0);

			if(SRB2Type != General.Map.Config.Start3DModeThingType)
				General.Map.IsChanged = true;
		}

		//mxd
		public void SetScale(float scalex, float scaley)
		{
			BeforePropsChange();

			scaleX = scalex;
			scaleY = scaley;

			if(SRB2Type != General.Map.Config.Start3DModeThingType)
				General.Map.IsChanged = true;
		}
		
		// This updates all properties
		// NOTE: This does not update sector! (call DetermineSector)
		public void Update(int type, float x, float y, float zoffset, int angle, int pitch, int roll, float scaleX, float scaleY,
						   Dictionary<string, bool> flags, int tag, int action, int[] args)
		{
			// Apply changes
			this.type = type;
			this.anglerad = Angle2D.DoomToReal(angle);
			this.angledoom = angle;
			this.pitch = pitch; //mxd
			this.roll = roll; //mxd
			this.scaleX = (scaleX == 0 ? 1.0f : scaleX); //mxd
			this.scaleY = (scaleY == 0 ? 1.0f : scaleY); //mxd
			this.flags = new Dictionary<string, bool>(flags);
			this.tag = tag;
			this.action = action;
			this.args = new int[NUM_ARGS];
			args.CopyTo(this.args, 0);
			this.Move(x, y, zoffset);

			UpdateCache(); //mxd
		}
		
		// This updates the settings from configuration
		public void UpdateConfiguration()
		{
			// Lookup settings
			ThingTypeInfo ti = General.Map.Data.GetThingInfo(SRB2Type);
			
			// Apply size
			size = ti.Radius;
			height = ti.Height; //mxd
			fixedsize = ti.FixedSize;
			spritescale = ti.SpriteScale; //mxd

			//mxd. Apply radius and height overrides?
			for(int i = 0; i < ti.Args.Length; i++)
			{
				if(ti.Args[i] == null) continue;
				if(ti.Args[i].Type == (int)UniversalType.ThingRadius && args[i] > 0)
					size = args[i];
				else if(ti.Args[i].Type == (int)UniversalType.ThingHeight && args[i] > 0)
					height = args[i];
			}
			
			// Color valid?
			if((ti.Color >= 0) && (ti.Color < ColorCollection.NUM_THING_COLORS))
			{
				// Apply color
				color = General.Colors.Colors[ti.Color + ColorCollection.THING_COLORS_OFFSET];
			}
			else
			{
				// Unknown thing color
				color = General.Colors.Colors[ColorCollection.THING_COLORS_OFFSET];
			}
			
			directional = ti.Arrow; //mxd
			rollsprite = ti.RollSprite; //mxd
			UpdateCache(); //mxd
		}

		//mxd. This checks if the thing has model override
		internal void UpdateCache()
		{
			if(General.Map.Data == null)
			{
				ismodel = false;
				return;
			}

			ismodel = General.Map.Data.ModeldefEntries.ContainsKey(SRB2Type);
			if(ismodel && General.Map.Data.ModeldefEntries[SRB2Type].LoadState == ModelLoadState.None)
				ismodel = General.Map.Data.ProcessModel(SRB2Type);

			if(ismodel) 
			{
				rollrad = (General.Map.Data.ModeldefEntries[SRB2Type].InheritActorRoll ? Angle2D.DegToRad(roll) : 0);
				pitchrad = (General.Map.Data.ModeldefEntries[SRB2Type].InheritActorPitch ? Angle2D.DegToRad(pitch) : 0);
			}
			else if(rollsprite)
			{
				rollrad = Angle2D.DegToRad(roll);
				pitchrad = 0;
			}
			else
			{
				rollrad = 0;
				pitchrad = 0;
			}
		}
		
		#endregion

		#region ================== Methods
		
		// This checks and returns a flag without creating it
		public bool IsFlagSet(string flagname)
		{
			if(flags.ContainsKey(flagname))
				return flags[flagname];
			else
				return false;
		}
		
		// This sets a flag
		public void SetFlag(string flagname, bool value)
		{
			if(!flags.ContainsKey(flagname) || (IsFlagSet(flagname) != value))
			{
				BeforePropsChange();

				flags[flagname] = value;
			}
		}
		
		// This returns a copy of the flags dictionary
		public Dictionary<string, bool> GetFlags()
		{
			return new Dictionary<string,bool>(flags);
		}

        public int GetFlagsValue()
        {
            int flags = 0;
            foreach (KeyValuePair<string, bool> f in Flags)
            {
                int fnum;
                if (f.Value && int.TryParse(f.Key, out fnum)) flags |= fnum;
            }

            //SRB2 stores Z position in upper 12 bits of flags. Add Z position to flags.
            if (General.Map.SRB2) flags |= (UInt16)Position.z << 4;

            return flags;
        }

        public void SetFlagsValue(int value)
        {
            Dictionary<string, bool> newflags = new Dictionary<string, bool>(flags);
            foreach (KeyValuePair<string, bool> f in flags)
            {
                int fnum;
                if (int.TryParse(f.Key, out fnum)) newflags[f.Key] = ((value & fnum) == fnum);
            }
            flags = newflags;

            //SRB2 stores Z position in upper 12 bits of flags. Get Z position to flags.
            if (General.Map.SRB2) pos.z = value >> 4;
        }

        // This clears all flags
        public void ClearFlags()
		{
			BeforePropsChange();
			
			flags.Clear();
		}
		
		// This snaps the vertex to the grid
		public void SnapToGrid()
		{
			// Calculate nearest grid coordinates
			this.Move(General.Map.Grid.SnappedToGrid(pos));
		}

		// This snaps the vertex to the map format accuracy
		public void SnapToAccuracy()
		{
			SnapToAccuracy(true);
		}

		// This snaps the vertex to the map format accuracy
		public void SnapToAccuracy(bool usepreciseposition)
		{
			// Round the coordinates
			Vector3D newpos = new Vector3D((float)Math.Round(pos.x, (usepreciseposition ? General.Map.FormatInterface.VertexDecimals : 0)),
										   (float)Math.Round(pos.y, (usepreciseposition ? General.Map.FormatInterface.VertexDecimals : 0)),
										   (float)Math.Round(pos.z, (usepreciseposition ? General.Map.FormatInterface.VertexDecimals : 0)));
			this.Move(newpos);
		}
		
		// This returns the distance from given coordinates
		public float DistanceToSq(Vector2D p)
		{
			return Vector2D.DistanceSq(p, pos);
		}

		// This returns the distance from given coordinates
		public float DistanceTo(Vector2D p)
		{
			return Vector2D.Distance(p, pos);
		}

		#endregion
	}
}
