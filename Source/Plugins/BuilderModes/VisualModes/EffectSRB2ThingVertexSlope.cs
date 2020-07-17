#region === Copyright (c) 2010 Pascal van der Heiden ===

using System.Collections.Generic;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;

#endregion

#region ================== Namespaces
using CodeImp.DoomBuilder.VisualModes;
#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	internal class EffectSRB2ThingVertexSlope : SectorEffect
	{
		// Things used to create this effect.
		private List<Thing> things;
		
		// Floor or ceiling?
		private bool slopefloor;

        private VisualBlockMap blockmap;
        private BSP bsp;
		
		// Constructor
        public EffectSRB2ThingVertexSlope(SectorData data, List<Thing> sourcethings, bool floor, VisualBlockMap blockmap, BSP bsp)
            : base(data)
		{
			things = sourcethings;
			slopefloor = floor;
            this.blockmap = blockmap;
            this.bsp = bsp;

            // New effect added: This sector needs an update!
            if (data.Mode.VisualSectorExists(data.Sector))
			{
				BaseVisualSector vs = (BaseVisualSector)data.Mode.GetVisualSector(data.Sector);
				vs.UpdateSectorGeometry(true);
			}
		}
		
		// This makes sure we are updated with the source linedef information
		public override void Update()
		{
			// Create vertices in clockwise order
			Vector3D[] verts = new Vector3D[3];
			int index = 0;
            foreach (Thing t in things)
            {
                ThingData td = data.Mode.GetThingData(t);
                td.AddUpdateSector(data.Sector, true);
                Vector3D position = t.Position;
                if (data.Mode.UseBlockmap)
                    t.DetermineSector(blockmap);
                else
                    t.DetermineSector(bsp);
                if (t.Parameter != 0) // JBR Absolute Z
                    position.z = t.GetFlagsValue();
                else
                    position.z += t.Sector.FloorHeight;
                verts[index] = position;
                index++;
                if (index > 2) break; //Only the first three vertices are used
            }
			
			// Make new plane
			if(slopefloor)
				data.Floor.plane = new Plane(verts[0], verts[1], verts[2], true);
			else
				data.Ceiling.plane = new Plane(verts[0], verts[2], verts[1], false);
		}
	}
}
