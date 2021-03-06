
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
using System.Globalization;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.VisualModes;
using CodeImp.DoomBuilder.Data;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	internal class BaseVisualThing : VisualThing, IVisualEventReceiver
	{
		#region ================== Constants
		
		#endregion
		
		#region ================== Variables

		private readonly BaseVisualMode mode;
		
		private bool isloaded;
		private bool nointeraction; //mxd
		private ImageData sprite;
        private ImageData flippedsprite;
		private float cageradius2;
		private Vector2D pos2d;
		private Vector3D boxp1;
		private Vector3D boxp2;
		private static List<BaseVisualThing> updateList; //mxd
        		
		// Undo/redo
		private int undoticket;

		// If this is set to true, the thing will be rebuilt after the action is performed.
		private bool changed;

		#endregion
		
		#region ================== Properties

		public bool Changed { get { return changed; } set { changed |= value; } }
        private ImageData Sprite { get { return Thing.IsReverse ? flippedsprite : sprite; } set { if (Thing.IsReverse) flippedsprite = value; else sprite = value; } }

		#endregion
		
		#region ================== Constructor / Setup
		
		// Constructor
		public BaseVisualThing(BaseVisualMode mode, Thing t) : base(t)
		{
			this.mode = mode;

			// Find thing information
			info = General.Map.Data.GetThingInfo(Thing.SRB2Type);

			//mxd. When true, the thing can be moved below floor/above ceiling
			nointeraction = (info.Actor != null && info.Actor.GetFlagValue("nointeraction", false));

			// Find sprite texture
			if(info.Sprite.Length > 0)
			{
				sprite = General.Map.Data.GetSpriteImage(info.Sprite);
				if(sprite != null) sprite.AddReference();
                flippedsprite = General.Map.Data.GetSpriteImage(info.Sprite + "_flipped");
                if (flippedsprite != null) flippedsprite.AddReference();
            }

			//mxd
			if(mode.UseSelectionFromClassicMode && t.Selected)
			{
				this.selected = true;
				mode.AddSelectedObject(this);
			}

			// We have no destructor
			GC.SuppressFinalize(this);
		}
		
		// This builds the thing geometry. Returns false when nothing was created.
		public bool Setup()
		{
            // Find the sector in which the thing resides
            if (mode.UseBlockmap)
                Thing.DetermineSector(mode.BlockMap);
            else
                Thing.DetermineSector(mode.BSP);
			
			//mxd. If the thing is inside a sector, apply DECORATE/UDMF alpha/renderstyle overrides
			byte alpha = 255;
			if(Thing.Sector != null)
			{
				string renderstyle = info.RenderStyle;
				alpha = info.AlphaByte;
				
				if(General.Map.UDMF)
				{
					if(Thing.IsFlagSet("translucent"))
					{
						renderstyle = "translucent";
						alpha = 64;
					}
					else if(Thing.IsFlagSet("invisible"))
					{
						renderstyle = "none";
						alpha = 0;
					}
					else if(Thing.Fields.ContainsKey("renderstyle"))
					{
						renderstyle = Thing.Fields.GetValue("renderstyle", renderstyle);
					}

					if((renderstyle == "add" || renderstyle == "translucent" || renderstyle == "subtract" || renderstyle == "stencil") 
						&& Thing.Fields.ContainsKey("alpha"))
					{
						alpha = (byte)(General.Clamp(Thing.Fields.GetValue("alpha", info.Alpha), 0f, 1f) * 255);
					}
				}
				else if(General.Map.HEXEN)
				{
					if(Thing.IsFlagSet("2048"))
					{
						renderstyle = "translucent";
						alpha = 64;
					}
					else if(Thing.IsFlagSet("4096"))
					{
						renderstyle = "none";
						alpha = 0;
					}
				}

				// Set appropriate RenderPass
				switch(renderstyle)
				{
					case "translucent":
					case "subtract":
					case "stencil":
						RenderPass = RenderPass.Alpha;
						break;

					case "add":
						RenderPass = RenderPass.Additive;
						break;

					case "none":
						RenderPass = RenderPass.Mask;
						alpha = 0;
						break;

					// Many render styles are not supported yet...
					default:
						RenderPass = RenderPass.Mask;
						alpha = 255;
						break;
				}
			}

			// Don't bother when alpha is unchanged, unless Additive RenderStyle is used
			if(RenderPass != RenderPass.Additive && alpha == 255) RenderPass = RenderPass.Mask;

			int sectorcolor = new PixelColor(alpha, 255, 255, 255).ToInt();
			fogfactor = 0f; //mxd
			
			//mxd. Check thing size 
			float thingradius = Thing.Size; // Thing.Size has ThingRadius arg override applied
			thingheight = Thing.Height; // Thing.Height has ThingHeight arg override applied

			if(thingradius < 0.1f || thingheight < 0.1f) 
			{
				thingradius = FIXED_RADIUS;
				thingheight = FIXED_RADIUS;
				sizeless = true;
			} 
			else 
			{
				sizeless = false;
			}

			if(Sprite != null)
			{
				Plane floor = new Plane(); //mxd
				Plane ceiling = new Plane(); //mxd
				if(Thing.Sector != null)
				{
					SectorData sd = mode.GetSectorData(Thing.Sector);
					floor = sd.Floor.plane; //mxd
					ceiling = sd.Ceiling.plane; //mxd

					if(!info.Bright)
					{
                        float thingz = Thing.IsFlipped ? sd.Ceiling.plane.GetZ(Thing.Position) - Thing.Position.z - Thing.Height : Thing.Position.z + sd.Floor.plane.GetZ(Thing.Position);
						Vector3D thingpos = new Vector3D(Thing.Position.x, Thing.Position.y, thingz);
						SectorLevel level = sd.GetLevelAboveOrAt(thingpos);

						//mxd. Let's use point on floor plane instead of Thing.Sector.FloorHeight;
						if(nointeraction && level == null && sd.LightLevels.Count > 0) level = sd.LightLevels[sd.LightLevels.Count - 1];

						//mxd. Use the light level of the highest surface when a thing is above highest sector level.
						if(level != null)
						{
							// TECH: In GZDoom, ceiling glow doesn't affect thing brightness 
							// Use sector brightness for color shading
							int brightness = level.brightnessbelow;

							// Level is glowing
							if(level.affectedbyglow && level.type == SectorLevelType.Floor)
							{
                                // Extrafloor glow doesn't affect thing brightness
                                if (level.sector == Thing.Sector)
                                {
                                    float planez = level.plane.GetZ(thingpos);

                                    // Get glow brightness
                                    int glowbrightness = sd.FloorGlow.Brightness / 2;
                                    SectorLevel nexthigher = sd.GetLevelAbove(new Vector3D(thingpos, planez));

                                    // Interpolate thing brightness between glow and regular ones
                                    if (nexthigher != null)
                                    {
                                        float higherz = nexthigher.plane.GetZ(thingpos);
                                        float delta = General.Clamp(1.0f - (thingpos.z - planez) / (higherz - planez), 0f, 1f);
                                        brightness = (int)((glowbrightness + level.sector.Brightness / 2) * delta + nexthigher.sector.Brightness * (1.0f - delta));
                                    }
                                }
                            }
							// Level below this one is glowing. Only possible for floor glow(?)
							else if(level.type == SectorLevelType.Glow)
							{
								// Interpolate thing brightness between glow and regular ones
								if(sd.Floor != null && sd.FloorGlow != null)
								{
									// Get glow brightness
									float glowz = level.plane.GetZ(thingpos);
									float floorz = floor.GetZ(thingpos);
									float delta = General.Clamp((thingpos.z - floorz) / (glowz - floorz), 0f, 1f);

									brightness = (int)((sd.FloorGlow.Brightness / 2 + sd.Floor.sector.Brightness / 2) * (1.0f - delta) + sd.Floor.sector.Brightness * delta);
								}
							}

							PixelColor areabrightness = PixelColor.FromInt(mode.CalculateBrightness(brightness));
							PixelColor areacolor = PixelColor.Modulate(level.colorbelow, areabrightness);
							sectorcolor = areacolor.WithAlpha(alpha).ToInt();

                            //mxd. Calculate fogfactor
                            fogfactor = VisualGeometry.CalculateFogFactor(level.sector.FogMode, brightness);
                        }
					}
					//TECH: even Bright Thing frames are affected by custom fade...
					else
					{
                        float thingz = Thing.IsFlipped ? sd.Ceiling.plane.GetZ(Thing.Position) - Thing.Position.z - Thing.Height : Thing.Position.z + sd.Floor.plane.GetZ(Thing.Position);
                        Vector3D thingpos = new Vector3D(Thing.Position.x, Thing.Position.y, thingz);
                        SectorLevel level = sd.GetLevelAboveOrAt(thingpos);

                        if (level != null && level.sector.FogMode > SectorFogMode.CLASSIC)
                        {
                            //mxd. Calculate fogfactor
                            fogfactor = VisualGeometry.CalculateFogFactor(level.sector.FogMode, level.brightnessbelow);
                        }
					}
				}
				
				// Check if the texture is loaded
				Sprite.LoadImage();
				isloaded = Sprite.IsImageLoaded;
				if(isloaded)
				{
					float offsetx = 0.0f;
					float offsety = 0.0f;
					
					base.Texture = Sprite;

					// Determine sprite size and offset
					float radius = Sprite.ScaledWidth * 0.5f;
					float height = Sprite.ScaledHeight;
					if(Sprite is SpriteImage)
					{
						offsetx = radius - (Sprite as SpriteImage).OffsetX;
						offsety = (Sprite as SpriteImage).OffsetY - height;
					}

					// Scale by thing type/actor scale
					// We do this after the offset x/y determination above, because that is entirely in sprite pixels space
					radius *= info.SpriteScale.Width;
					height *= info.SpriteScale.Height;
					offsetx *= info.SpriteScale.Width;
					offsety *= info.SpriteScale.Height;

					// Make vertices
					WorldVertex[] verts = new WorldVertex[6];

					if(sizeless) //mxd
					{
                        float hh = height / 2;
						verts[0] = new WorldVertex(-radius + offsetx, 0.0f, offsety - hh, sectorcolor, 0.0f, 1.0f);
						verts[1] = new WorldVertex(-radius + offsetx, 0.0f, hh + offsety, sectorcolor, 0.0f, 0.0f);
						verts[2] = new WorldVertex(+radius + offsetx, 0.0f, hh + offsety, sectorcolor, 1.0f, 0.0f);
						verts[3] = verts[0];
						verts[4] = verts[2];
						verts[5] = new WorldVertex(+radius + offsetx, 0.0f, offsety - hh, sectorcolor, 1.0f, 1.0f);
					} 
					else if (info.CenterHitbox)
                    {
                        float hh = height / 2;
                        verts[0] = new WorldVertex(-radius + offsetx, 0.0f, -hh, sectorcolor, 0.0f, 1.0f);
                        verts[1] = new WorldVertex(-radius + offsetx, 0.0f, hh, sectorcolor, 0.0f, 0.0f);
                        verts[2] = new WorldVertex(+radius + offsetx, 0.0f, hh, sectorcolor, 1.0f, 0.0f);
                        verts[3] = verts[0];
                        verts[4] = verts[2];
                        verts[5] = new WorldVertex(+radius + offsetx, 0.0f, -hh, sectorcolor, 1.0f, 1.0f);
                    }
                    else
					{
						verts[0] = new WorldVertex(-radius + offsetx, 0.0f, offsety, sectorcolor, 0.0f, 1.0f);
						verts[1] = new WorldVertex(-radius + offsetx, 0.0f, height + offsety, sectorcolor, 0.0f, 0.0f);
						verts[2] = new WorldVertex(+radius + offsetx, 0.0f, height + offsety, sectorcolor, 1.0f, 0.0f);
						verts[3] = verts[0];
						verts[4] = verts[2];
						verts[5] = new WorldVertex(+radius + offsetx, 0.0f, offsety, sectorcolor, 1.0f, 1.0f);
					}
					SetVertices(verts, floor, ceiling);
				}
				else
				{
					base.Texture = General.Map.Data.Hourglass3D;

					// Determine sprite size
					float radius = Math.Min(thingradius, thingheight / 2f);
					float height = Math.Min(thingradius * 2f, thingheight);

					// Make vertices
					WorldVertex[] verts = new WorldVertex[6];
					verts[0] = new WorldVertex(-radius, 0.0f, 0.0f, sectorcolor, 0.0f, 1.0f);
					verts[1] = new WorldVertex(-radius, 0.0f, height, sectorcolor, 0.0f, 0.0f);
					verts[2] = new WorldVertex(+radius, 0.0f, height, sectorcolor, 1.0f, 0.0f);
					verts[3] = verts[0];
					verts[4] = verts[2];
					verts[5] = new WorldVertex(+radius, 0.0f, 0.0f, sectorcolor, 1.0f, 1.0f);
					SetVertices(verts, floor, ceiling);
				}
			}
			
			// Determine position
			Vector3D pos = Thing.Position;
			if(Thing.SRB2Type == 9501)
			{
				if(Thing.Sector != null) //mxd
				{ 
					// This is a special thing that needs special positioning
					SectorData sd = mode.GetSectorData(Thing.Sector);
					pos.z = sd.Ceiling.sector.CeilHeight + Thing.Position.z;
				}
			}
			else if(Thing.SRB2Type == 9500)
			{
				if(Thing.Sector != null) //mxd
				{ 
					// This is a special thing that needs special positioning
					SectorData sd = mode.GetSectorData(Thing.Sector);
					pos.z = sd.Floor.sector.FloorHeight + Thing.Position.z;
				}
			}
            else if (General.Map.SRB2 && Thing.IsSlopeVertex)
            {
                if (Thing.Sector != null)
                {
                    // This is a special thing that needs special positioning
                    SectorData sd = mode.GetSectorData(Thing.Sector);
                    pos.z = sd.Floor.sector.FloorHeight + Thing.Position.z - Thing.Height/2;
                }
            }
            else if (info.IgnoreZ)
            {
                //Z position is always 0.
                if (Thing.Sector != null)
                {
                    SectorData sd = mode.GetSectorData(Thing.Sector);
                    pos.z = sd.Floor.sector.FloorHeight;
                }
                else pos.z = 0;
            }
            else if (General.Map.SRB2 && Thing.SRB2Type == 1705)
            {
                //Use the flags value instead of the Z position for the Z position. SRB2 is really stupid sometimes.
                pos.z = Thing.GetFlagsValue();
                if (Thing.Sector != null)
                {
                    SectorData sd = mode.GetSectorData(Thing.Sector);
                    pos.z += sd.Floor.sector.FloorHeight;
                }
            }

            else if (info.AbsoluteZ)
			{
				// Absolute Z position
				pos.z = Thing.Position.z;
			}
			else if(Thing.IsFlipped)
			{
				// Hang from ceiling
				if(Thing.Sector != null)
				{
					SectorData sd = mode.GetSectorData(Thing.Sector);
					float maxz = sd.Ceiling.plane.GetZ(Thing.Position) - info.Height;
					pos.z = maxz;

					if(Thing.Position.z > 0 || nointeraction) pos.z -= Thing.Position.z;

					// Check if below floor
					if(!nointeraction)
					{
						float minz = sd.Floor.plane.GetZ(Thing.Position);
						if(pos.z < minz) pos.z = Math.Min(minz, maxz);
					}
				}
			}
			else
			{
				// Stand on floor
				if(Thing.Sector != null)
				{
					SectorData sd = mode.GetSectorData(Thing.Sector);
					float minz = sd.Floor.plane.GetZ(Thing.Position);
					pos.z = minz;

					if(Thing.Position.z > 0 || nointeraction) pos.z += Thing.Position.z;

					// Check if above ceiling
					if(!nointeraction)
					{
						float maxz = sd.Ceiling.plane.GetZ(Thing.Position) - info.Height;
						if(pos.z > maxz) pos.z = Math.Max(minz, maxz);
					}
				}
			}

            if (Thing.IsFlagSet("8")) pos.z += Thing.IsFlipped ? -info.HeightOffset : info.HeightOffset;
			
			// Apply settings
			SetPosition(pos);
			SetCageColor(Thing.Color);

			// Keep info for object picking
			cageradius2 = thingradius * Angle2D.SQRT2;
			cageradius2 = cageradius2 * cageradius2;
			pos2d = pos;

			if(sizeless) //mxd
			{ 
				boxp1 = new Vector3D(pos.x - thingradius, pos.y - thingradius, pos.z - thingradius/2);
				boxp2 = new Vector3D(pos.x + thingradius, pos.y + thingradius, pos.z + thingradius/2);
			} 
			else if (info.CenterHitbox)
			{
				boxp1 = new Vector3D(pos.x - thingradius, pos.y - thingradius, pos.z - thingheight/2);
				boxp2 = new Vector3D(pos.x + thingradius, pos.y + thingradius, pos.z + thingheight/2);
			}
            else
            {
                boxp1 = new Vector3D(pos.x - thingradius, pos.y - thingradius, pos.z);
                boxp2 = new Vector3D(pos.x + thingradius, pos.y + thingradius, pos.z + thingheight);
            }

            // Done
            changed = false;
			return true;
		}
		
		// Disposing
		public override void Dispose()
		{
			if(!IsDisposed)
			{
				if(sprite != null)
				{
					sprite.RemoveReference();
					sprite = null;
				}
                if (flippedsprite != null)
                {
                    flippedsprite.RemoveReference();
                    flippedsprite = null;
                }

                base.Dispose();
			}
		}
		
		#endregion
		
		#region ================== Methods
		
		// This forces to rebuild the whole thing
		public void Rebuild()
		{
			// Find thing information
			info = General.Map.Data.GetThingInfo(Thing.SRB2Type);

			//mxd. When true, the thing can be moved below floor/above ceiling
			nointeraction = (info.Actor != null && info.Actor.GetFlagValue("nointeraction", false));

			// Find sprite texture
			if(info.Sprite.Length > 0)
			{
				sprite = General.Map.Data.GetSpriteImage(info.Sprite);
				if(sprite != null) sprite.AddReference();
                flippedsprite = General.Map.Data.GetSpriteImage(info.Sprite +  "_flipped");
                if (flippedsprite != null) flippedsprite.AddReference();
            }

            // Setup visual thing
            Setup();
		}
		
		// This updates the thing when needed
		public override void Update()
		{
			if(!isloaded)
			{
				// Rebuild sprite geometry when sprite is loaded
				if(Sprite.IsImageLoaded)
				{
					Setup();
				}
			}
			
			// Let the base update
			base.Update();
		}

		// This performs a fast test in object picking
		public override bool PickFastReject(Vector3D from, Vector3D to, Vector3D dir)
		{
			float distance2 = Line2D.GetDistanceToLineSq(from, to, pos2d, false);
			return (distance2 <= cageradius2);
		}

		// This performs an accurate test for object picking
		public override bool PickAccurate(Vector3D from, Vector3D to, Vector3D dir, ref float u_ray)
		{
			Vector3D delta = to - from;
			float tfar = float.MaxValue;
			float tnear = float.MinValue;
			
			// Ray-Box intersection code
			// See http://www.masm32.com/board/index.php?topic=9941.0
			
			// Check X slab
			if(delta.x == 0.0f)
			{
				if(from.x > boxp2.x || from.x < boxp1.x)
				{
					// Ray is parallel to the planes & outside slab
					return false;
				}
			}
			else
			{
				float tmp = 1.0f / delta.x;
				float t1 = (boxp1.x - from.x) * tmp;
				float t2 = (boxp2.x - from.x) * tmp;
				if(t1 > t2) General.Swap(ref t1, ref t2);
				if(t1 > tnear) tnear = t1;
				if(t2 < tfar) tfar = t2;
				if(tnear > tfar || tfar < 0.0f)
				{
					// Ray missed box or box is behind ray
					return false;
				}
			}
			
			// Check Y slab
			if(delta.y == 0.0f)
			{
				if(from.y > boxp2.y || from.y < boxp1.y)
				{
					// Ray is parallel to the planes & outside slab
					return false;
				}
			}
			else
			{
				float tmp = 1.0f / delta.y;
				float t1 = (boxp1.y - from.y) * tmp;
				float t2 = (boxp2.y - from.y) * tmp;
				if(t1 > t2) General.Swap(ref t1, ref t2);
				if(t1 > tnear) tnear = t1;
				if(t2 < tfar) tfar = t2;
				if(tnear > tfar || tfar < 0.0f)
				{
					// Ray missed box or box is behind ray
					return false;
				}
			}
			
			// Check Z slab
			if(delta.z == 0.0f)
			{
				if(from.z > boxp2.z || from.z < boxp1.z)
				{
					// Ray is parallel to the planes & outside slab
					return false;
				}
			}
			else
			{
				float tmp = 1.0f / delta.z;
				float t1 = (boxp1.z - from.z) * tmp;
				float t2 = (boxp2.z - from.z) * tmp;
				if(t1 > t2) General.Swap(ref t1, ref t2);
				if(t1 > tnear) tnear = t1;
				if(t2 < tfar) tfar = t2;
				if(tnear > tfar || tfar < 0.0f)
				{
					// Ray missed box or box is behind ray
					return false;
				}
			}
			
			// Set interpolation point
			u_ray = (tnear > 0.0f) ? tnear : tfar;
			return true;
		}

		//mxd
		public bool IsSelected() 
		{
			return selected;
		}
		
		#endregion

		#region ================== Events

		// Unused
		public void OnSelectBegin() { }
		public void OnEditBegin() { }
		public void OnMouseMove(MouseEventArgs e) { }
		public void OnChangeTargetBrightness(bool up) { }
		public void OnChangeTextureOffset(int horizontal, int vertical, bool doSurfaceAngleCorrection) { }
		public void OnSelectTexture() { }
		public void OnCopyTexture() { }
		public void OnPasteTexture() { }
		public void OnCopyTextureOffsets() { }
		public void OnPasteTextureOffsets() { }
		public void OnTextureAlign(bool alignx, bool aligny) { }
		public void OnToggleUpperUnpegged() { }
		public void OnToggleLowerUnpegged() { }
		public void OnProcess(float deltatime) { }
		public void OnTextureFloodfill() { }
		public void OnInsert() { }
		public void OnTextureFit(FitTextureOptions options) { } //mxd
		public void ApplyTexture(string texture) { }
		public void ApplyUpperUnpegged(bool set) { }
		public void ApplyLowerUnpegged(bool set) { }
		public void SelectNeighbours(bool select, bool withSameTexture, bool withSameHeight) { } //mxd
		
		// Return texture name
		public string GetTextureName() { return ""; }

		// Select or deselect
		public void OnSelectEnd()
		{
			if(this.selected)
			{
				this.selected = false;
				mode.RemoveSelectedObject(this);
			}
			else
			{
				this.selected = true;
				mode.AddSelectedObject(this);
			}
		}

		//mxd. Delete thing
		public void OnDelete() 
		{
			mode.CreateUndo("Delete thing");
			mode.SetActionResult("Deleted a thing.");

			this.Thing.Fields.BeforeFieldsChange();
			this.Thing.Dispose();
			this.Dispose();

			General.Map.IsChanged = true;
			General.Map.ThingsFilter.Update();
		}
		
		// Copy properties
		public void OnCopyProperties()
		{
			BuilderPlug.Me.CopiedThingProps = new ThingProperties(Thing);
			mode.SetActionResult("Copied thing properties.");
		}
		
		// Paste properties
		public void OnPasteProperties(bool usecopysettings)
		{
			if(BuilderPlug.Me.CopiedThingProps != null)
			{
				mode.CreateUndo("Paste thing properties");
				mode.SetActionResult("Pasted thing properties.");
				BuilderPlug.Me.CopiedThingProps.Apply(Thing, usecopysettings); //mxd. Added "usecopysettings"
				Thing.UpdateConfiguration();
				this.Rebuild();
				mode.ShowTargetInfo();
			}
		}
		
		// Edit button released
		public void OnEditEnd()
		{
			if(General.Interface.IsActiveWindow)
			{
				
				List<Thing> things = mode.GetSelectedThings();
				//mxd
				updateList = new List<BaseVisualThing>();
				foreach(Thing t in things)
				{
					VisualThing vt = mode.GetVisualThing(t);
					if(vt != null) updateList.Add(vt as BaseVisualThing);
				}

				General.Interface.OnEditFormValuesChanged += Interface_OnEditFormValuesChanged;
				mode.StartRealtimeInterfaceUpdate(SelectionType.Things);
				General.Interface.ShowEditThings(things);
				mode.StopRealtimeInterfaceUpdate(SelectionType.Things);
				General.Interface.OnEditFormValuesChanged -= Interface_OnEditFormValuesChanged;

				updateList.Clear();
				updateList = null;
			}
		}

		//mxd
		private void Interface_OnEditFormValuesChanged(object sender, EventArgs e) 
		{
			foreach(BaseVisualThing vt in updateList) vt.Changed = true;
		}

		//mxd
		public void OnResetTextureOffset() 
		{
			mode.CreateUndo("Reset thing scale");
			mode.SetActionResult("Thing scale reset.");

			Thing.SetScale(1.0f, 1.0f);

			// Update what must be updated
			this.Changed = true;
		}

		//mxd
		public void OnResetLocalTextureOffset() 
		{
			mode.CreateUndo("Reset thing scale, pitch and roll");
			mode.SetActionResult("Thing scale, pitch and roll reset.");

			Thing.SetScale(1.0f, 1.0f);
			Thing.SetPitch(0);
			Thing.SetRoll(0);

			// Update what must be updated
			this.Changed = true;
		}
		
		// Raise/lower thing
		public void OnChangeTargetHeight(int amount)
		{
            if (info.IgnoreZ) return;

			if(General.Map.FormatInterface.HasThingHeight)
			{
				if((General.Map.UndoRedo.NextUndo == null) || (General.Map.UndoRedo.NextUndo.TicketID != undoticket))
					undoticket = mode.CreateUndo("Change thing height");

                if (General.Map.SRB2 && Thing.SRB2Type == 1705)
                {
                    int newZ = General.Clamp(Thing.GetFlagsValue() + amount,0,0xFFFF);
                    Thing.SetFlagsValue(newZ);

                    int actualZ = newZ >> 4;
                    Vector3D newPosition = new Vector3D(Thing.Position.x, Thing.Position.y, actualZ);
                    Thing.Move(newPosition);

                    mode.SetActionResult("Changed thing height to " + newZ + ".");
                }
                else
                {
                    Vector3D newPosition = Thing.Position + new Vector3D(0.0f, 0.0f, (Thing.IsFlipped ? -amount : amount));
                    newPosition.z = General.Clamp(newPosition.z, General.Map.FormatInterface.MinThingHeight, General.Map.FormatInterface.MaxThingHeight);

                    Thing.Move(newPosition);

                    mode.SetActionResult("Changed thing height to " + Thing.Position.z + ".");
                }

                // Update what must be updated
                ThingData td = mode.GetThingData(this.Thing);
				foreach(KeyValuePair<Sector, bool> s in td.UpdateAlso)
				{
					if(mode.VisualSectorExists(s.Key))
					{
						BaseVisualSector vs = (BaseVisualSector)mode.GetVisualSector(s.Key);
						vs.UpdateSectorGeometry(s.Value);
					}
				}
				
				this.Changed = true;
			}
		}

		//mxd
		public void OnChangeScale(int incrementX, int incrementY)
		{
			if(!General.Map.UDMF || !Sprite.IsImageLoaded) return;
			
			if((General.Map.UndoRedo.NextUndo == null) || (General.Map.UndoRedo.NextUndo.TicketID != undoticket))
				undoticket = mode.CreateUndo("Change thing scale");

			float scaleX = Thing.ScaleX;
			float scaleY = Thing.ScaleY;

			if(incrementX != 0) 
			{
				float pix = (int)Math.Round(Sprite.Width * scaleX) + incrementX;
				float newscaleX = (float)Math.Round(pix / Sprite.Width, 3);
				scaleX = (newscaleX == 0 ? scaleX * -1 : newscaleX);
			}

			if(incrementY != 0) 
			{
				float pix = (int)Math.Round(Sprite.Height * scaleY) + incrementY;
				float newscaleY = (float)Math.Round(pix / Sprite.Height, 3);
				scaleY = (newscaleY == 0 ? scaleY * -1 : newscaleY);
			}

			Thing.SetScale(scaleX, scaleY);
			mode.SetActionResult("Changed thing scale to " + scaleX.ToString("F03", CultureInfo.InvariantCulture) + ", " + scaleY.ToString("F03", CultureInfo.InvariantCulture) + " (" + (int)Math.Round(Sprite.Width * scaleX) + " x " + (int)Math.Round(Sprite.Height * scaleY) + ").");

			// Update what must be updated
			this.Changed = true;
		}

		//mxd
		public void OnMove(Vector3D newposition) 
		{
			if((General.Map.UndoRedo.NextUndo == null) || (General.Map.UndoRedo.NextUndo.TicketID != undoticket))
				undoticket = mode.CreateUndo("Move thing");
			Thing.Move(newposition);
			mode.SetActionResult("Changed thing position to " + Thing.Position + ".");

			// Update what must be updated
			ThingData td = mode.GetThingData(this.Thing);
			foreach(KeyValuePair<Sector, bool> s in td.UpdateAlso) 
			{
				if(mode.VisualSectorExists(s.Key)) 
				{
					BaseVisualSector vs = (BaseVisualSector)mode.GetVisualSector(s.Key);
					vs.UpdateSectorGeometry(s.Value);
				}
			}

			this.Changed = true;
		}

		//mxd
		public void SetAngle(int newangle)
		{
			if((General.Map.UndoRedo.NextUndo == null) || (General.Map.UndoRedo.NextUndo.TicketID != undoticket))
				undoticket = mode.CreateUndo("Change thing angle");
			Thing.Rotate(newangle);
			mode.SetActionResult("Changed thing angle to " + Thing.AngleDoom + ".");
			this.Changed = true;
		}

		//mxd
		public void SetPitch(int newpitch)
		{
			if(!General.Map.UDMF) return;
			if((General.Map.UndoRedo.NextUndo == null) || (General.Map.UndoRedo.NextUndo.TicketID != undoticket))
				undoticket = mode.CreateUndo("Change thing pitch");
			Thing.SetPitch(newpitch);
			mode.SetActionResult("Changed thing pitch to " + Thing.Pitch + ".");
			this.Changed = true;
		}

		//mxd
		public void SetRoll(int newroll)
		{
			if(!General.Map.UDMF) return;
			if((General.Map.UndoRedo.NextUndo == null) || (General.Map.UndoRedo.NextUndo.TicketID != undoticket))
				undoticket = mode.CreateUndo("Change thing roll");
			Thing.SetRoll(newroll);
			mode.SetActionResult("Changed thing roll to " + Thing.Roll + ".");
			this.Changed = true;
		}
		
		#endregion
	}
}
