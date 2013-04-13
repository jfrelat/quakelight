/*
Copyright (C) 1996-1997 Id Software, Inc.

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  

See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

*/
// r_light.c

namespace quake
{
    public partial class render
    {
        static int	r_dlightframecount;
        
        /*
        ==================
        R_AnimateLight
        ==================
        */
        static void R_AnimateLight ()
        {
            int i, j, k;

            //
            // light animations
            // 'm' is normal light, 'a' is no light, 'z' is double bright
            i = (int)(client.cl.time * 10);
            for (j = 0; j < quakedef.MAX_LIGHTSTYLES; j++)
            {
                if (client.cl_lightstyle[j].length == 0)
                {
                    d_lightstylevalue[j] = 256;
                    continue;
                }
                k = i % client.cl_lightstyle[j].length;
                k = client.cl_lightstyle[j].map[k] - 'a';
                k = k * 22;
                d_lightstylevalue[j] = k;
            }
        }
        
        /*
        =============================================================================

        DYNAMIC LIGHTS

        =============================================================================
        */

        /*
        =============
        R_MarkLights
        =============
        */
        static void R_MarkLights (client.dlight_t light, int bit, model.node_or_leaf_t _node)
        {
            model.mplane_t      splitplane;
            double              dist;
            model.msurface_t    surf;
            int                 i;

            if (_node.contents < 0)
                return;

            model.mnode_t node = (model.mnode_t)_node;
            splitplane = node.plane;
            dist = mathlib.DotProduct(light.origin, splitplane.normal) - splitplane.dist;

            if (dist > light.radius)
            {
                R_MarkLights(light, bit, node.children[0]);
                return;
            }
            if (dist < -light.radius)
            {
                R_MarkLights(light, bit, node.children[1]);
                return;
            }

            // mark the polygons
            for (i = 0; i < node.numsurfaces; i++)
            {
                surf = client.cl.worldmodel.surfaces[node.firstsurface + i];
                if (surf.dlightframe != r_dlightframecount)
                {
                    surf.dlightbits = 0;
                    surf.dlightframe = r_dlightframecount;
                }
                surf.dlightbits |= bit;
            }

            R_MarkLights(light, bit, node.children[0]);
            R_MarkLights(light, bit, node.children[1]);
        }

        /*
        =============
        R_PushDlights
        =============
        */
        public static void R_PushDlights ()
        {
            int             i;
            client.dlight_t l;

            r_dlightframecount = r_framecount + 1;	// because the count hasn't
            //  advanced yet for this frame

            for (i = 0; i < client.MAX_DLIGHTS; i++)
            {
                l = client.cl_dlights[i];
                if (l.die < client.cl.time || l.radius == 0)
                    continue;
                R_MarkLights(l, 1 << i, client.cl.worldmodel.nodes[0]);
            }
        }
        
        /*
        =============================================================================

        LIGHT SAMPLING

        =============================================================================
        */

        static int RecursiveLightPoint (model.node_or_leaf_t _node, double[] start, double[] end)
        {
	        int			        r;
	        double		        front, back, frac;
	        bool		        side;
	        model.mplane_t	    plane;
	        double[]		    mid = new double[3];
	        model.msurface_t	surf;
	        int			        s, t, ds, dt;
	        int			        i;
	        model.mtexinfo_t	tex;
	        int		            lightmap;
	        uint	            scale;
	        int			        maps;

	        if (_node.contents < 0)
		        return -1;		// didn't hit anything

            model.mnode_t node = (model.mnode_t)_node;
        // calculate mid point

        // FIXME: optimize for axial
	        plane = node.plane;
	        front = mathlib.DotProduct (start, plane.normal) - plane.dist;
            back = mathlib.DotProduct(end, plane.normal) - plane.dist;
	        side = (front < 0);
        	
	        if ( (back < 0) == side)
		        return RecursiveLightPoint (node.children[side ? 1 : 0], start, end);
        	
	        frac = front / (front-back);
	        mid[0] = start[0] + (end[0] - start[0])*frac;
	        mid[1] = start[1] + (end[1] - start[1])*frac;
	        mid[2] = start[2] + (end[2] - start[2])*frac;
        	
        // go down front side	
	        r = RecursiveLightPoint (node.children[side ? 1 : 0], start, mid);
	        if (r >= 0)
		        return r;		// hit something
        		
	        if ( (back < 0) == side )
		        return -1;		// didn't hit anuthing
        		
        // check for impact on this node

	        for (i=0 ; i<node.numsurfaces ; i++)
	        {
                surf = client.cl.worldmodel.surfaces[node.firstsurface + i];
                if ((surf.flags & model.SURF_DRAWTILED) != 0)
			        continue;	// no lightmaps

		        tex = surf.texinfo;

                s = (int)(mathlib.DotProduct(mid, tex.vecs[0]) + tex.vecs[0][3]);
                t = (int)(mathlib.DotProduct(mid, tex.vecs[1]) + tex.vecs[1][3]);

		        if (s < surf.texturemins[0] ||
		        t < surf.texturemins[1])
			        continue;
        		
		        ds = s - surf.texturemins[0];
		        dt = t - surf.texturemins[1];
        		
		        if ( ds > surf.extents[0] || dt > surf.extents[1] )
			        continue;

		        if (surf.samples == null)
			        return 0;

		        ds >>= 4;
		        dt >>= 4;

		        lightmap = surf.samples.ofs;
		        r = 0;
		        if (surf.samples != null)
		        {

			        lightmap += dt * ((surf.extents[0]>>4)+1) + ds;

			        for (maps = 0 ; maps < bspfile.MAXLIGHTMAPS && surf.styles[maps] != 255 ;
					        maps++)
			        {
				        scale = (uint)d_lightstylevalue[surf.styles[maps]];
				        r += (int)(surf.samples.buffer[lightmap] * scale);
				        lightmap += ((surf.extents[0]>>4)+1) *
						        ((surf.extents[1]>>4)+1);
			        }
        			
			        r >>= 8;
		        }
        		
		        return r;
	        }

        // go down back side
            return RecursiveLightPoint(node.children[!side ? 1 : 0], mid, end);
        }

        static int R_LightPoint (double[] p)
        {
	        double[]	end = new double[3];
	        int			r;
        	
	        if (client.cl.worldmodel.lightdata == null)
		        return 255;
        	
	        end[0] = p[0];
	        end[1] = p[1];
	        end[2] = p[2] - 2048;
        	
	        r = RecursiveLightPoint (client.cl.worldmodel.nodes[0], p, end);
        	
	        if (r == -1)
		        r = 0;

	        if (r < r_refdef.ambientlight)
		        r = r_refdef.ambientlight;

	        return r;
        }
    }
}
