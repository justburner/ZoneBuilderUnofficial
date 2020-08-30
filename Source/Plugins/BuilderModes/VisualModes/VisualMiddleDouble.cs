
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
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.VisualModes;
using CodeImp.DoomBuilder.Data;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	internal sealed class VisualMiddleDouble : BaseVisualGeometrySidedef
	{
		#region ================== Constants

		#endregion

		#region ================== Variables

		private bool repeatmidtex;
		private Plane topclipplane;
		private Plane bottomclipplane;
		
		#endregion

		#region ================== Properties

		#endregion

		#region ================== Constructor / Setup

		// Constructor
		public VisualMiddleDouble(BaseVisualMode mode, VisualSector vs, Sidedef s) : base(mode, vs, s)
		{
			//mxd
			geometrytype = VisualGeometryType.WALL_MIDDLE;
			partname = "mid";
			
			// Set render pass
			this.RenderPass = RenderPass.Mask;
			
			// We have no destructor
			GC.SuppressFinalize(this);
		}
		
		// This builds the geometry. Returns false when no geometry created.
		public override bool Setup()
		{
			//mxd
			if(Sidedef.LongMiddleTexture == MapSet.EmptyLongName) return false;
			
			Vector2D vl, vr;

			//mxd. lightfog flag support
			int lightvalue;
			bool lightabsolute;
			GetLightValue(out lightvalue, out lightabsolute);
			
			Vector2D tscale = new Vector2D(Sidedef.Fields.GetValue("scalex_mid", 1.0f),
										   Sidedef.Fields.GetValue("scaley_mid", 1.0f));
			Vector2D toffset = new Vector2D(Sidedef.Fields.GetValue("offsetx_mid", 0.0f),
											Sidedef.Fields.GetValue("offsety_mid", 0.0f));
			
			// Left and right vertices for this sidedef
			if(Sidedef.IsFront) 
			{
				vl = new Vector2D(Sidedef.Line.Start.Position.x, Sidedef.Line.Start.Position.y);
				vr = new Vector2D(Sidedef.Line.End.Position.x, Sidedef.Line.End.Position.y);
			} 
			else 
			{
				vl = new Vector2D(Sidedef.Line.End.Position.x, Sidedef.Line.End.Position.y);
				vr = new Vector2D(Sidedef.Line.Start.Position.x, Sidedef.Line.Start.Position.y);
			}

			// Load sector data
			SectorData sd = mode.GetSectorData(Sidedef.Sector);
			SectorData osd = mode.GetSectorData(Sidedef.Other.Sector);
			if(!osd.Updated) osd.Update();

			// Load texture
			if(Sidedef.LongMiddleTexture != MapSet.EmptyLongName) 
			{
				base.Texture = General.Map.Data.GetTextureImage(Sidedef.LongMiddleTexture);
				if(base.Texture == null || base.Texture is UnknownImage) 
				{
					base.Texture = General.Map.Data.UnknownTexture3D;
					setuponloadedtexture = Sidedef.LongMiddleTexture;
				} 
				else if(!base.Texture.IsImageLoaded) 
				{
					setuponloadedtexture = Sidedef.LongMiddleTexture;
				}
			} 
			else 
			{
				// Use missing texture
				base.Texture = General.Map.Data.MissingTexture3D;
				setuponloadedtexture = 0;
			}

			// Get texture scaled size
			Vector2D tsz = new Vector2D(base.Texture.ScaledWidth, base.Texture.ScaledHeight);
			tsz = tsz / tscale;

			// Get texture offsets
			Vector2D tof = new Vector2D(Sidedef.OffsetX, Sidedef.OffsetY);
			tof = tof + toffset;
			tof = tof / tscale;
			if(General.Map.Config.ScaledTextureOffsets && !base.Texture.WorldPanning)
				tof = tof * base.Texture.Scale;

            // Determine texture coordinates plane as they would be in normal circumstances.
            // We can then use this plane to find any texture coordinate we need.
            // The logic here is the same as in the original VisualMiddleSingle (except that
            // the values are stored in a TexturePlane)
            // NOTE: I use a small bias for the floor height, because if the difference in
            // height is 0 then the TexturePlane doesn't work!
            Vector3D vlt, vlb, vrt, vrb;
            Vector2D tlt, tlb, trt, trb;
            float floorbias = (Sidedef.Sector.CeilHeight == Sidedef.Sector.FloorHeight) ? 1.0f : 0.0f;
            float geotop = Math.Min(Sidedef.Sector.CeilHeight, Sidedef.Other.Sector.CeilHeight);
            float geobottom = Math.Max(Sidedef.Sector.FloorHeight, Sidedef.Other.Sector.FloorHeight);
            float geoplanetop = Math.Min(sd.Ceiling.plane.GetZ(vl), osd.Ceiling.plane.GetZ(vl));
			float geoplanebottom = Math.Max(sd.Floor.plane.GetZ(vl), osd.Floor.plane.GetZ(vl));
            float textop = geoplanetop;
            float texbottom = geoplanebottom;
            float repetitions = 1;

            // Determine if we should repeat the middle texture
            bool srb2repeat = General.Map.SRB2 && Sidedef.Line.IsFlagSet(General.Map.Config.RepeatMidtextureFlag);
            bool doomrepeat = !General.Map.SRB2 && Sidedef.IsFlagSet("wrapmidtex") || Sidedef.Line.IsFlagSet("wrapmidtex");
            repeatmidtex = srb2repeat || doomrepeat; //JBR was declared as local bool when there's a global bool that never got assigned, it was a mistake?

            //A little redundant, but having a separate boolean value for each case makes the code a little more readable
            bool srb2repeatfixed = srb2repeat && Sidedef.OffsetX >= 4096;
            bool srb2repeatindefinite = srb2repeat && !srb2repeatfixed;

            float topheight = !General.Map.SRB2 || Sidedef.Line.IsFlagSet("128") ? geotop : geoplanetop;
            float bottomheight = !General.Map.SRB2 || Sidedef.Line.IsFlagSet("128") ? geobottom : geoplanebottom;
            if (!repeatmidtex || srb2repeatfixed) //Tile the texture a fixed number of times
            {
                // How often is it tiled?
                repetitions = srb2repeatfixed ? (Sidedef.OffsetX / 4096) + 1 : 1;

                // Determine top portion height
                if (IsLowerUnpegged())
                    textop = bottomheight + tof.y + repetitions * Math.Abs(tsz.y);
                else
                    textop = topheight + tof.y;

                // Calculate bottom portion height
                texbottom = textop - repetitions * Math.Abs(tsz.y);
            }
            else //Fill up the whole gap
            {
                repetitions = (geotop - geobottom) / Math.Abs(tsz.y);
                if ((geotop - geobottom) % Math.Abs(tsz.y) != 0)
                    repetitions++; // tile an extra time to fill the gap

                if (srb2repeatindefinite) //Still consider offsets
                {
                    if (IsLowerUnpegged())
                        texbottom = bottomheight + tof.y;
                    else
                        textop = topheight + tof.y;
                }
            }

            float h = Math.Min(textop, geoplanetop);
            float l = Math.Max(texbottom, geoplanebottom);
            float texturevpeg;

            // When lower unpegged is set, the middle texture is bound to the bottom
            if (IsLowerUnpegged())
                texturevpeg = repetitions * Math.Abs(tsz.y) - h + texbottom;
            else
                texturevpeg = textop - h;

            tlt.x = tlb.x = tof.x;
            trt.x = trb.x = tof.x + Sidedef.Line.Length;
            tlt.y = trt.y = texturevpeg;
            tlb.y = trb.y = texturevpeg + h - (l + floorbias);

            float rh = h;
            float rl = l;

            // Correct to account for slopes
            if (General.Map.SRB2)
            {
                float midtextureslant;

                if (Sidedef.Line.IsFlagSet("128"))
                    midtextureslant = 0;
                else if (IsLowerUnpegged())
                    midtextureslant = osd.Floor.plane.GetZ(vl) < sd.Floor.plane.GetZ(vl)
                              ? sd.Floor.plane.GetZ(vr) - sd.Floor.plane.GetZ(vl)
                              : osd.Floor.plane.GetZ(vr) - osd.Floor.plane.GetZ(vl);
                else
                    midtextureslant = sd.Ceiling.plane.GetZ(vl) < osd.Ceiling.plane.GetZ(vl)
                              ? sd.Ceiling.plane.GetZ(vr) - sd.Ceiling.plane.GetZ(vl)
                              : osd.Ceiling.plane.GetZ(vr) - osd.Ceiling.plane.GetZ(vl);

                float newtextop = textop + midtextureslant;
                float newtexbottom = texbottom + midtextureslant;

                float highcut = geotop + (sd.Ceiling.plane.GetZ(vl) < osd.Ceiling.plane.GetZ(vl)
                              ? sd.Ceiling.plane.GetZ(vr) - sd.Ceiling.plane.GetZ(vl)
                              : osd.Ceiling.plane.GetZ(vr) - osd.Ceiling.plane.GetZ(vl));
                float lowcut = geobottom + (osd.Floor.plane.GetZ(vl) < sd.Floor.plane.GetZ(vl)
                              ? sd.Floor.plane.GetZ(vr) - sd.Floor.plane.GetZ(vl)
                              : osd.Floor.plane.GetZ(vr) - osd.Floor.plane.GetZ(vl));

                // Texture stuff
                rh = Math.Min(highcut, newtextop);
                rl = Math.Max(newtexbottom, lowcut);

                float newtexturevpeg;

                // When lower unpegged is set, the middle texture is bound to the bottom
                if (IsLowerUnpegged())
                    newtexturevpeg = repetitions * Math.Abs(tsz.y) - rh + newtexbottom;
                else
                    newtexturevpeg = newtextop - rh;

                trt.y = newtexturevpeg;
                trb.y = newtexturevpeg + rh - (rl + floorbias);
            }

            // Transform pixel coordinates to texture coordinates
            tlt /= tsz;
            tlb /= tsz;
            trb /= tsz;
            trt /= tsz;

            // Geometry coordinates
            vlt = new Vector3D(vl.x, vl.y, h);
            vlb = new Vector3D(vl.x, vl.y, l);
            vrb = new Vector3D(vr.x, vr.y, rl);
            vrt = new Vector3D(vr.x, vr.y, rh);

            TexturePlane tp = new TexturePlane();
            tp.tlt = IsLowerUnpegged() ? tlb : tlt;
            tp.trb = trb;
            tp.trt = trt;
            tp.vlt = IsLowerUnpegged() ? vlb : vlt;
            tp.vrb = vrb;
            tp.vrt = vrt;

			// Keep top and bottom planes for intersection testing
			top = sd.Ceiling.plane;
			bottom = sd.Floor.plane;

			// Create initial polygon, which is just a quad between floor and ceiling
			WallPolygon poly = new WallPolygon();
			poly.Add(new Vector3D(vl.x, vl.y, sd.Floor.plane.GetZ(vl)));
			poly.Add(new Vector3D(vl.x, vl.y, sd.Ceiling.plane.GetZ(vl)));
			poly.Add(new Vector3D(vr.x, vr.y, sd.Ceiling.plane.GetZ(vr)));
			poly.Add(new Vector3D(vr.x, vr.y, sd.Floor.plane.GetZ(vr)));

			// Determine initial color
			int lightlevel = lightabsolute ? lightvalue : sd.Ceiling.brightnessbelow + lightvalue;

			//mxd. This calculates light with doom-style wall shading
			PixelColor wallbrightness = PixelColor.FromInt(mode.CalculateBrightness(lightlevel, Sidedef));
			PixelColor wallcolor = PixelColor.Modulate(sd.Ceiling.colorbelow, wallbrightness);
            fogfactor = CalculateFogFactor(lightlevel);
            poly.color = wallcolor.WithAlpha(255).ToInt();

			// Cut off the part below the other floor and above the other ceiling
			CropPoly(ref poly, osd.Ceiling.plane, true);
			CropPoly(ref poly, osd.Floor.plane, true);

            //Repeat middle texture
            if (!doomrepeat) //Need to crop at least some part of the texture
			{
                // Create crop planes (we also need these for intersection testing)
                if (!Sidedef.Line.IsFlagSet("128")) //Skew the texture according to the slope
                {
                    if (IsLowerUnpegged()) //Skew according to floor
                    {
                        bottomclipplane = sd.Floor.plane.GetZ(vl) > osd.Floor.plane.GetZ(vl) ? sd.Floor.plane : osd.Floor.plane;
                        Vector3D up = new Vector3D(0f, 0f, texbottom - bottomclipplane.GetZ(vl));
                        bottomclipplane.Offset -= Vector3D.DotProduct(up, bottomclipplane.Normal);

                        topclipplane = bottomclipplane.GetInverted();
                        Vector3D up2 = new Vector3D(0f, 0f, textop - texbottom);
                        topclipplane.Offset -= Vector3D.DotProduct(up2, topclipplane.Normal);

                    }
                    else //Skew according to ceiling
                    {
                        topclipplane = sd.Ceiling.plane.GetZ(vl) < osd.Ceiling.plane.GetZ(vl) ? sd.Ceiling.plane : osd.Ceiling.plane;
                        Vector3D up = new Vector3D(0f, 0f, textop - topclipplane.GetZ(vl));
                        topclipplane.Offset -= Vector3D.DotProduct(up, topclipplane.Normal);

                        bottomclipplane = topclipplane.GetInverted();
                        Vector3D down = new Vector3D(0f, 0f, texbottom - textop);
                        bottomclipplane.Offset -= Vector3D.DotProduct(down, bottomclipplane.Normal);
                    }
                }
                else //Don't skew textures
                {
                    topclipplane = new Plane(new Vector3D(0, 0, -1), textop);
                    bottomclipplane = new Plane(new Vector3D(0, 0, 1), -texbottom);
                }

                // Crop polygon by these heights
                // In SRB2, if the texture is tiled indefinitely, only crop towards the plane it's pegged to
                if (!srb2repeatindefinite || !IsLowerUnpegged())
                    CropPoly(ref poly, topclipplane, true);
                if (!srb2repeatindefinite || IsLowerUnpegged())
				    CropPoly(ref poly, bottomclipplane, true);
			}

			//mxd. In(G)ZDoom, middle sidedef parts are not clipped by extrafloors of any type...
			List<WallPolygon> polygons = new List<WallPolygon> { poly };
			//ClipExtraFloors(polygons, sd.ExtraFloors, true); //mxd
			//ClipExtraFloors(polygons, osd.ExtraFloors, true); //mxd

			//if(polygons.Count > 0) 
			//{
				// Keep top and bottom planes for intersection testing
				top = osd.Ceiling.plane;
				bottom = osd.Floor.plane;

				// Process the polygon and create vertices
				List<WorldVertex> verts = CreatePolygonVertices(polygons, tp, sd, lightvalue, lightabsolute);
				if(verts.Count > 2) 
				{
					// Apply alpha to vertices
					byte alpha = SetLinedefRenderstyle(true);
					if(alpha < 255) 
					{
						for(int i = 0; i < verts.Count; i++) 
						{
							WorldVertex v = verts[i];
							v.c = PixelColor.FromInt(v.c).WithAlpha(alpha).ToInt();
							verts[i] = v;
						}
					}

					base.SetVertices(verts);
					return true;
				}
			//}
			
			base.SetVertices(null); //mxd
			return false;
		}

        #endregion

        #region ================== Methods

        protected override bool IsLowerUnpegged()
        {
            bool lowerunpeggedflag = Sidedef.Line.IsFlagSet(General.Map.Config.LowerUnpeggedFlag);
            bool pegmidtextureflag = General.Map.SRB2 && Sidedef.Line.IsFlagSet(General.Map.Config.PegMidtextureFlag);
            return lowerunpeggedflag ^ pegmidtextureflag;
        }

        // This performs a fast test in object picking
        public override bool PickFastReject(Vector3D from, Vector3D to, Vector3D dir)
		{
			if(!repeatmidtex)
			{
				// When the texture is not repeated, leave when outside crop planes
				if((pickintersect.z < bottomclipplane.GetZ(pickintersect)) ||
				   (pickintersect.z > topclipplane.GetZ(pickintersect)))
				   return false;
			}
			
			return base.PickFastReject(from, to, dir);
		}

		//mxd. Alpha based picking
		public override bool PickAccurate(Vector3D from, Vector3D to, Vector3D dir, ref float u_ray) 
		{
            if (!Texture.IsImageLoaded || (!Texture.IsTranslucent && !Texture.IsMasked)) return base.PickAccurate(from, to, dir, ref u_ray);

            float u;
			new Line2D(from, to).GetIntersection(Sidedef.Line.Line, out u);
			if(Sidedef != Sidedef.Line.Front) u = 1.0f - u;

			// Get correct offset to texture space...
			float zoffset;
			int ox = (int)Math.Floor((u * Sidedef.Line.Length * UniFields.GetFloat(Sidedef.Fields, "scalex_mid", 1.0f) / Texture.Scale.x + Sidedef.OffsetX + UniFields.GetFloat(Sidedef.Fields, "offsetx_mid")) % Texture.Width);
			int oy;

			if(repeatmidtex)
			{
				if(IsLowerUnpegged())
					zoffset = Sidedef.Sector.FloorHeight;
				else
					zoffset = Sidedef.Sector.CeilHeight;

				oy = (int)Math.Floor(((pickintersect.z - zoffset) * UniFields.GetFloat(Sidedef.Fields, "scaley_mid", 1.0f) / Texture.Scale.y - Sidedef.OffsetY - UniFields.GetFloat(Sidedef.Fields, "offsety_mid")) % Texture.Height);
			}
			else
			{
				zoffset = bottomclipplane.GetZ(pickintersect);
				oy = (int)Math.Ceiling(((pickintersect.z - zoffset) * UniFields.GetFloat(Sidedef.Fields, "scaley_mid", 1.0f) / Texture.Scale.y) % Texture.Height);
			}

            // Make sure offsets are inside of texture dimensions...
            if (ox < 0) ox += Texture.Width;
            if (oy < 0) oy += Texture.Height;

            // Check pixel alpha
            if (Texture.GetBitmap().GetPixel(General.Clamp(ox, 0, Texture.Width - 1), General.Clamp(Texture.Height - oy, 0, Texture.Height - 1)).A > 0)
			{
				return base.PickAccurate(from, to, dir, ref u_ray);
			}

			return false;
		}
		
		// Return texture name
		public override string GetTextureName()
		{
			return this.Sidedef.MiddleTexture;
		}

		// This changes the texture
		protected override void SetTexture(string texturename)
		{
			this.Sidedef.SetTextureMid(texturename);
			General.Map.Data.UpdateUsedTextures();
			this.Setup();
		}

		protected override void SetTextureOffsetX(int x)
		{
			Sidedef.Fields.BeforeFieldsChange();
			Sidedef.Fields["offsetx_mid"] = new UniValue(UniversalType.Float, (float)x);
		}

		protected override void SetTextureOffsetY(int y)
		{
			Sidedef.Fields.BeforeFieldsChange();
			Sidedef.Fields["offsety_mid"] = new UniValue(UniversalType.Float, (float)y);
		}

		protected override void MoveTextureOffset(Point xy)
		{
			Sidedef.Fields.BeforeFieldsChange();
			float oldx = Sidedef.Fields.GetValue("offsetx_mid", 0.0f);
			float oldy = Sidedef.Fields.GetValue("offsety_mid", 0.0f);
			float scalex = Sidedef.Fields.GetValue("scalex_mid", 1.0f);
			float scaley = Sidedef.Fields.GetValue("scaley_mid", 1.0f);
			Sidedef.Fields["offsetx_mid"] = new UniValue(UniversalType.Float, GetRoundedTextureOffset(oldx, xy.X, scalex, Texture != null ? Texture.Width : -1)); //mxd

			//mxd. Don't clamp offsetY of clipped mid textures
			bool dontClamp = (Texture == null || (!Sidedef.IsFlagSet("wrapmidtex") && !Sidedef.Line.IsFlagSet("wrapmidtex")));
			Sidedef.Fields["offsety_mid"] = new UniValue(UniversalType.Float, GetRoundedTextureOffset(oldy, xy.Y, scaley, dontClamp ? -1 : Texture.Height));
		}

		protected override Point GetTextureOffset()
		{
			float oldx = Sidedef.Fields.GetValue("offsetx_mid", 0.0f);
			float oldy = Sidedef.Fields.GetValue("offsety_mid", 0.0f);
			return new Point((int)oldx, (int)oldy);
		}

		//mxd
		protected override void ResetTextureScale() 
		{
			Sidedef.Fields.BeforeFieldsChange();
			if(Sidedef.Fields.ContainsKey("scalex_mid")) Sidedef.Fields.Remove("scalex_mid");
			if(Sidedef.Fields.ContainsKey("scaley_mid")) Sidedef.Fields.Remove("scaley_mid");
		}

		//mxd
		public override void OnTextureFit(FitTextureOptions options) 
		{
			if(!General.Map.UDMF) return;
			if(string.IsNullOrEmpty(Sidedef.MiddleTexture) || Sidedef.MiddleTexture == "-" || !Texture.IsImageLoaded) return;
			FitTexture(options);
			Setup();
		}
		
		#endregion
	}
}
