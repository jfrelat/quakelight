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
// r_misc.c

namespace quake
{
    public partial class render
    {
        /*
        ===============
        R_CheckVariables
        ===============
        */
        static double oldbright;
        static void R_CheckVariables()
        {
	        if (r_fullbright.value != oldbright)
	        {
		        oldbright = r_fullbright.value;
		        //draw.D_FlushCaches ();	// so all lighting changes
	        }
        }
        
        /*
        ============
        Show

        Debugging use
        ============
        */
        void Show ()
        {
	        vid.vrect_t	vr = new vid.vrect_t();

	        vr.x = vr.y = 0;
	        vr.width = (int)screen.vid.width;
            vr.height = (int)screen.vid.height;
	        vr.pnext = null;
	        vid.VID_Update (vr);
        }
        
        /*
        ====================
        R_TimeRefresh_f

        For program optimization
        ====================
        */
        static void R_TimeRefresh_f ()
        {
	        int			    i;
	        double		    start, stop, time;
	        int			    startangle;
	        vid.vrect_t		vr = new vid.vrect_t();

	        startangle = (int)r_refdef.viewangles[1];
        	
	        start = sys_linux.Sys_FloatTime ();
	        for (i=0 ; i<128 ; i++)
	        {
                r_refdef.viewangles[1] = i / 128.0 * 360.0;

		        R_RenderView ();

		        vr.x = r_refdef.vrect.x;
		        vr.y = r_refdef.vrect.y;
		        vr.width = r_refdef.vrect.width;
		        vr.height = r_refdef.vrect.height;
		        vr.pnext = null;
		        vid.VID_Update (vr);
	        }
	        stop = sys_linux.Sys_FloatTime ();
	        time = stop-start;
	        console.Con_Printf (time + " seconds (" + (128/time) + " fps)\n");
        	
	        r_refdef.viewangles[1] = startangle;
        }


        /*
        ================
        R_LineGraph

        Only called by R_DisplayTime
        ================
        */
        static void R_LineGraph (int x, int y, int h)
        {
            int i;
            int dest;
            int s;

            // FIXME: should be disabled on no-buffer adapters, or should be in the driver

            x += r_refdef.vrect.x;
            y += r_refdef.vrect.y;

            dest = (int)(screen.vid.rowbytes * y + x);

            s = (int)r_graphheight.value;

            if (h > s)
                h = s;

            for (i = 0; i < h; i++, dest -= (int)(screen.vid.rowbytes * 2))
            {
                screen.vid.buffer[dest] = 0xff;
                screen.vid.buffer[dest - screen.vid.rowbytes] = 0x30;
            }
            for (; i < s; i++, dest -= (int)(screen.vid.rowbytes * 2))
            {
                screen.vid.buffer[dest] = 0x30;
                screen.vid.buffer[dest - screen.vid.rowbytes] = 0x30;
            }
        }

        /*
        ==============
        R_TimeGraph

        Performance monitoring tool
        ==============
        */
        public const int	MAX_TIMINGS		= 100;
        static int timex;
        static byte[]	r_timings = new byte[MAX_TIMINGS];
        static void R_TimeGraph()
        {
	        int		a;
	        double	r_time2;
	        int		x;
        	
	        r_time2 = sys_linux.Sys_FloatTime ();

	        a = (int)((r_time2-r_time1)/0.01);
        //a = fabs(mouse_y * 0.05);
        //a = (int)((r_refdef.vieworg[2] + 1024)/1)%(int)r_graphheight.value;
        //a = fabs(velocity[0])/20;
        //a = ((int)fabs(origin[0])/8)%20;
        //a = (cl.idealpitch + 30)/5;
	        r_timings[timex] = (byte)a;
	        a = timex;

	        if (r_refdef.vrect.width <= MAX_TIMINGS)
		        x = r_refdef.vrect.width-1;
	        else
		        x = r_refdef.vrect.width -
				        (r_refdef.vrect.width - MAX_TIMINGS)/2;
	        do
	        {
		        R_LineGraph (x, r_refdef.vrect.height-2, r_timings[a]);
		        if (x==0)
			        break;		// screen too small to hold entire thing
		        x--;
		        a--;
		        if (a == -1)
			        a = MAX_TIMINGS-1;
	        } while (a != timex);

	        timex = (timex+1)%MAX_TIMINGS;
        }


        /*
        =============
        R_PrintTimes
        =============
        */
        void R_PrintTimes ()
        {
        }


        /*
        =============
        R_PrintDSpeeds
        =============
        */
        void R_PrintDSpeeds ()
        {
        }


        /*
        =============
        R_PrintAliasStats
        =============
        */
        void R_PrintAliasStats ()
        {
        }


        void WarpPalette ()
        {
	        int		i,j;
	        byte[]	newpalette = new byte[768];
	        int[]	basecolor = new int[3];
        	
	        basecolor[0] = 130;
	        basecolor[1] = 80;
	        basecolor[2] = 50;

        // pull the colors halfway to bright brown
	        for (i=0 ; i<256 ; i++)
	        {
		        for (j=0 ; j<3 ; j++)
		        {
			        newpalette[i*3+j] = (byte)((host.host_basepal[i*3+j] + basecolor[j])/2);
		        }
	        }
        	
	        vid.VID_ShiftPalette (newpalette);
        }


        /*
        ===================
        R_TransformFrustum
        ===================
        */
        public static void R_TransformFrustum ()
        {
	        int		    i;
	        double[]	v = new double[3], v2 = new double[3];
        	
	        for (i=0 ; i<4 ; i++)
	        {
		        v[0] = screenedge[i].normal[2];
		        v[1] = -screenedge[i].normal[0];
		        v[2] = screenedge[i].normal[1];

		        v2[0] = v[1]*vright[0] + v[2]*vup[0] + v[0]*vpn[0];
		        v2[1] = v[1]*vright[1] + v[2]*vup[1] + v[0]*vpn[1];
		        v2[2] = v[1]*vright[2] + v[2]*vup[2] + v[0]*vpn[2];

		        mathlib.VectorCopy (v2, ref view_clipplanes[i].normal);

                view_clipplanes[i].dist = mathlib.DotProduct(modelorg, v2);
            }
        }

        /*
        ================
        TransformVector
        ================
        */
        public static void TransformVector (double[] @in, ref double[] @out)
        {
	        @out[0] = mathlib.DotProduct(@in, vright);
            @out[1] = mathlib.DotProduct(@in, vup);
            @out[2] = mathlib.DotProduct(@in, vpn);		
        }

        /*
        ================
        R_TransformPlane
        ================
        */
        void R_TransformPlane (model.mplane_t p, ref double normal, ref double dist)
        {
        }
        
        /*
        ===============
        R_SetUpFrustumIndexes
        ===============
        */
        static void R_SetUpFrustumIndexes ()
        {
	        int		i, j, pindex;

	        pindex = 0;

	        for (i=0 ; i<4 ; i++)
	        {
		        for (j=0 ; j<3 ; j++)
		        {
			        if (view_clipplanes[i].normal[j] < 0)
			        {
				        r_frustum_indexes[pindex+j] = j;
				        r_frustum_indexes[pindex+j+3] = j+3;
			        }
			        else
			        {
				        r_frustum_indexes[pindex+j] = j+3;
				        r_frustum_indexes[pindex+j+3] = j;
			        }
		        }

	        // FIXME: do just once at start
		        pfrustum_indexes[i] = pindex;
		        pindex += 6;
	        }
        }
        
        /*
        ===============
        R_SetupFrame
        ===============
        */
        static void R_SetupFrame ()
        {
	        int				edgecount;
	        vid.vrect_t		vrect = new vid.vrect_t();
	        double			w, h;

        // don't allow cheats in multiplayer
	        if (client.cl.maxclients > 1)
	        {
		        cvar_t.Cvar_Set ("r_draworder", "0");
                cvar_t.Cvar_Set("r_fullbright", "0");
                cvar_t.Cvar_Set("r_ambient", "0");
                cvar_t.Cvar_Set("r_drawflat", "0");
	        }

	        if (r_numsurfs.value != 0)
	        {
	        }

	        if (r_numedges.value != 0)
	        {
	        }

	        r_refdef.ambientlight = (int)r_ambient.value;

	        if (r_refdef.ambientlight < 0)
		        r_refdef.ambientlight = 0;

	        if (!server.sv.active)
		        r_draworder.value = 0;	// don't let cheaters look behind walls
        		
	        R_CheckVariables ();
        	
	        R_AnimateLight ();

	        r_framecount++;

	        numbtofpolys = 0;

        // build the transformation matrix for the given view angles
            mathlib.VectorCopy(r_refdef.vieworg, ref modelorg);
	        mathlib.VectorCopy (r_refdef.vieworg, ref r_origin);

	        mathlib.AngleVectors (r_refdef.viewangles, ref vpn, ref vright, ref vup);

        // current viewleaf
	        r_oldviewleaf = r_viewleaf;
	        r_viewleaf = model.Mod_PointInLeaf (r_origin, client.cl.worldmodel);

	        r_dowarpold = r_dowarp;
	        r_dowarp = r_waterwarp.value != 0 && (r_viewleaf.contents <= bspfile.CONTENTS_WATER);

	        if ((r_dowarp != r_dowarpold) || r_viewchanged || view.lcd_x.value != 0)
	        {
		        if (r_dowarp)
		        {
                    if ((screen.vid.width <= screen.vid.maxwarpwidth) &&
                        (screen.vid.height <= screen.vid.maxwarpheight))
			        {
				        vrect.x = 0;
				        vrect.y = 0;
                        vrect.width = (int)screen.vid.width;
                        vrect.height = (int)screen.vid.height;

				        R_ViewChanged (vrect, sbar.sb_lines, screen.vid.aspect);
			        }
			        else
			        {
                        w = screen.vid.width;
                        h = screen.vid.height;

                        if (w > screen.vid.maxwarpwidth)
				        {
                            h *= (double)screen.vid.maxwarpwidth / w;
                            w = screen.vid.maxwarpwidth;
				        }

                        if (h > screen.vid.maxwarpheight)
				        {
                            h = screen.vid.maxwarpheight;
					        w *= (double)screen.vid.maxwarpheight / h;
				        }

				        vrect.x = 0;
				        vrect.y = 0;
				        vrect.width = (int)w;
				        vrect.height = (int)h;

				        R_ViewChanged (vrect,
							           (int)((double)sbar.sb_lines * (h/(double)screen.vid.height)),
							           screen.vid.aspect * (h / w) *
								         ((double)screen.vid.width / (double)screen.vid.height));
			        }
		        }
		        else
		        {
			        vrect.x = 0;
			        vrect.y = 0;
                    vrect.width = (int)screen.vid.width;
                    vrect.height = (int)screen.vid.height;

			        R_ViewChanged (vrect, sbar.sb_lines, screen.vid.aspect);
		        }

		        r_viewchanged = false;
	        }

        // start off with just the four screen edge clip planes
	        R_TransformFrustum ();

        // save base values
	        mathlib.VectorCopy (vpn, ref base_vpn);
            mathlib.VectorCopy(vright, ref base_vright);
            mathlib.VectorCopy(vup, ref base_vup);
            mathlib.VectorCopy(modelorg, ref base_modelorg);

            R_SetSkyFrame ();

	        R_SetUpFrustumIndexes ();

            //r_cache_thrash = false;

        // clear frame counts
            c_faceclip = 0;
	        d_spanpixcount = 0;
	        r_polycount = 0;
	        r_drawnpolycount = 0;
	        r_wholepolycount = 0;
            //r_amodels_drawn = 0;
	        r_outofsurfaces = 0;
	        r_outofedges = 0;

	        draw.D_SetupFrame ();
        }
    }
}
