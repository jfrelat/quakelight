using System;

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
// r_main.c

namespace quake
{
    public partial class render
    {
        static double[]	    viewlightvec = new double[3];
        static alight_t     r_viewlighting = new alight_t { ambientlight=128, shadelight=192, plightvec=viewlightvec };
        static double r_time1;
        static int			r_numallocatededges;
        public static bool      r_drawpolys;
        static bool	        r_drawculledpolys;
        public static bool      r_worldpolysbacktofront;
        public static bool	    r_recursiveaffinetriangles = true;
        public static int		r_pixbytes = 1;
        public static double	r_aliasuvscale = 1.0;
        static int          r_outofsurfaces;
        static int		    r_outofedges;

        public static bool	    r_dowarp, r_dowarpold, r_viewchanged;

        static int			        numbtofpolys;
        static btofpoly_t[]         pbtofpolys;
        static model.mvertex_t[]    r_pcurrentvertbase;

        public static int			c_surf;
        static int			r_maxsurfsseen, r_maxedgesseen, r_cnumsurfs;
        static bool         r_surfsonstack;
        static int			r_clipflags;

        public static byte[]    r_warpbuffer;

        static bool         r_fov_greater_than_90;

        //
        // view origin
        //
        public static double[] vup = new double[3], base_vup = new double[3];
        public static double[] vpn = new double[3], base_vpn = new double[3];
        public static double[] vright = new double[3], base_vright = new double[3];
        public static double[] r_origin = new double[3];

        //
        // screen size info
        //
        public static refdef_t	r_refdef = new refdef_t();
        public static double    xcenter, ycenter;
        public static double    xscale, yscale;
        public static double    xscaleinv, yscaleinv;
        static double       xscaleshrink, yscaleshrink;
        static double       aliasxscale, aliasyscale, aliasxcenter, aliasycenter;

        int		    screenwidth;

        public static double	pixelAspect;
        static double screenAspect;
        static double       verticalFieldOfView;
        static double       xOrigin, yOrigin;

        static model.mplane_t[]	screenedge = new model.mplane_t[4];

        //
        // refresh flags
        //
        public static int	        r_framecount = 1;	// so frame counts initialized to 0 don't match
        static int		    r_visframecount;
        static int		    d_spanpixcount;
        static int		    r_polycount;
        public static int		    r_drawnpolycount;
        static int		    r_wholepolycount;

        string		viewmodname;
        int			modcount;

        static int[]            pfrustum_indexes = new int[4];
        static int[]		    r_frustum_indexes = new int[4*6];

        int		    reinit_surfcache = 1;	// if 1, surface cache is currently empty and
								        // must be reinitialized for current cache size

        static model.mleaf_t	r_viewleaf, r_oldviewleaf;

        public static model.texture_t   r_notexture_mip;

        static double		    r_aliastransition, r_resfudge;

        public static int[]		        d_lightstylevalue = new int[256];	// 8.8 fraction of base light value

        double	    dp_time1, dp_time2, db_time1, db_time2, rw_time1, rw_time2;
        double      se_time1, se_time2, de_time1, de_time2, dv_time1, dv_time2;

        static cvar_t   r_draworder = new cvar_t("r_draworder","0");
        static cvar_t   r_speeds = new cvar_t("r_speeds", "0");
        static cvar_t   r_timegraph = new cvar_t("r_timegraph", "0");
        static cvar_t   r_graphheight = new cvar_t("r_graphheight", "10");
        public static cvar_t   r_clearcolor = new cvar_t("r_clearcolor", "2");
        static cvar_t   r_waterwarp = new cvar_t("r_waterwarp", "1");
        static cvar_t   r_fullbright = new cvar_t("r_fullbright", "0");
        static cvar_t   r_drawentities = new cvar_t("r_drawentities", "1");
        static cvar_t   r_drawviewmodel = new cvar_t("r_drawviewmodel", "1");
        static cvar_t   r_aliasstats = new cvar_t("r_polymodelstats", "0");
        static cvar_t   r_dspeeds = new cvar_t("r_dspeeds", "0");
        public static cvar_t   r_drawflat = new cvar_t("r_drawflat", "0");
        static cvar_t   r_ambient = new cvar_t("r_ambient", "0");
        static cvar_t   r_reportsurfout = new cvar_t("r_reportsurfout", "0");
        static cvar_t   r_maxsurfs = new cvar_t("r_maxsurfs", "0");
        static cvar_t   r_numsurfs = new cvar_t("r_numsurfs", "0");
        static cvar_t   r_reportedgeout = new cvar_t("r_reportedgeout", "0");
        static cvar_t   r_maxedges = new cvar_t("r_maxedges", "0");
        static cvar_t   r_numedges = new cvar_t("r_numedges", "0");
        static cvar_t   r_aliastransbase = new cvar_t("r_aliastransbase", "200");
        static cvar_t   r_aliastransadj = new cvar_t("r_aliastransadj", "100");

        static render()
        {
            int kk;
            int i,j;
            for(kk = 0; kk < 4; kk++) screenedge[kk] = new model.mplane_t();
            for(kk = 0; kk < 4; kk++) view_clipplanes[kk] = new render.clipplane_t();
            for(kk = 0; kk < 16; kk++) world_clipplanes[kk] = new render.clipplane_t();
            for(i = 0; i < 2; i++)
                for(j = 0; j < MAXWORKINGVERTS; j++)
                    clip_verts[i][j] = new double[5];
            for(i = 0; i < 2; i++)
                for(j = 0; j < 8; j++)
                    fva[i][j] = new draw.finalvert_t();
            for(kk = 0; kk < 8; kk++) av[kk] = new auxvert_t();
            r_alias_init();
        }

        /*
        ==================
        R_InitTextures
        ==================
        */
        public static void R_InitTextures()
        {
            int     x, y, m;
            int     dest;

            // create a simple checkerboard texture for the default
            r_notexture_mip = new model.texture_t();
            r_notexture_mip.pixels = new byte[16 * 16 + 8 * 8 + 4 * 4 + 2 * 2];

            r_notexture_mip.width = r_notexture_mip.height = 16;
            r_notexture_mip.offsets[0] = 0;
            r_notexture_mip.offsets[1] = r_notexture_mip.offsets[0] + 16 * 16;
            r_notexture_mip.offsets[2] = r_notexture_mip.offsets[1] + 8 * 8;
            r_notexture_mip.offsets[3] = r_notexture_mip.offsets[2] + 4 * 4;

            for (m = 0; m < 4; m++)
            {
                dest = (int)r_notexture_mip.offsets[m];
                for (y = 0; y < (16 >> m); y++)
                    for (x = 0; x < (16 >> m); x++)
                    {
                        if ((y < (8 >> m)) ^ (x < (8 >> m)))
                            r_notexture_mip.pixels[dest++] = 0;
                        else
                            r_notexture_mip.pixels[dest++] = 0xff;
                    }
            }
        }

        /*
        ===============
        R_Init
        ===============
        */
        public static void R_Init ()
        {
	        int		dummy;
        	
	        R_InitTurb ();
        
            cmd.Cmd_AddCommand("timerefresh", R_TimeRefresh_f);
            cmd.Cmd_AddCommand("pointfile", R_ReadPointFile_f);

            cvar_t.Cvar_RegisterVariable(r_draworder);
            cvar_t.Cvar_RegisterVariable(r_speeds);
            cvar_t.Cvar_RegisterVariable(r_timegraph);
            cvar_t.Cvar_RegisterVariable(r_graphheight);
            cvar_t.Cvar_RegisterVariable(r_drawflat);
            cvar_t.Cvar_RegisterVariable(r_ambient);
            cvar_t.Cvar_RegisterVariable(r_clearcolor);
            cvar_t.Cvar_RegisterVariable(r_waterwarp);
            cvar_t.Cvar_RegisterVariable(r_fullbright);
            cvar_t.Cvar_RegisterVariable(r_drawentities);
            cvar_t.Cvar_RegisterVariable(r_drawviewmodel);
            cvar_t.Cvar_RegisterVariable(r_aliasstats);
            cvar_t.Cvar_RegisterVariable(r_dspeeds);
            cvar_t.Cvar_RegisterVariable(r_reportsurfout);
            cvar_t.Cvar_RegisterVariable(r_maxsurfs);
            cvar_t.Cvar_RegisterVariable(r_numsurfs);
            cvar_t.Cvar_RegisterVariable(r_reportedgeout);
            cvar_t.Cvar_RegisterVariable(r_maxedges);
            cvar_t.Cvar_RegisterVariable(r_numedges);
            cvar_t.Cvar_RegisterVariable(r_aliastransbase);
            cvar_t.Cvar_RegisterVariable(r_aliastransadj);

            cvar_t.Cvar_SetValue("r_maxedges", (double)NUMSTACKEDGES);
            cvar_t.Cvar_SetValue("r_maxsurfs", (double)NUMSTACKSURFACES);

            view_clipplanes[0].leftedge = true;
            view_clipplanes[1].rightedge = true;
            view_clipplanes[1].leftedge = view_clipplanes[2].leftedge =
                    view_clipplanes[3].leftedge = false;
            view_clipplanes[0].rightedge = view_clipplanes[2].rightedge =
                    view_clipplanes[3].rightedge = false;

	        r_refdef.xOrigin = XCENTERING;
	        r_refdef.yOrigin = YCENTERING;

	        R_InitParticles ();

	        draw.D_Init ();
        }

        /*
        ===============
        R_NewMap
        ===============
        */
        public static void R_NewMap ()
        {
            int i;

            // clear out efrags in case the level hasn't been reloaded
            // FIXME: is this one short?
            for (i = 0; i < client.cl.worldmodel.numleafs; i++)
                client.cl.worldmodel.leafs[i].efrags = null;

            r_viewleaf = null;
            R_ClearParticles();

	        r_cnumsurfs = (int)r_maxsurfs.value;

	        if (r_cnumsurfs <= MINSURFACES)
		        r_cnumsurfs = MINSURFACES;

	        if (r_cnumsurfs <= NUMSTACKSURFACES)
	        {
                surfaces = new surf_t[r_cnumsurfs];
                for (int kk = 0; kk < r_cnumsurfs; kk++) surfaces[kk] = new surf_t();
                surface_p = 0;
                surf_max = r_cnumsurfs;
                r_surfsonstack = false;
                // surface 0 doesn't really exist; it's just a dummy because index 0
                // is used to indicate no edge attached to surface
            }
	        else
	        {
		        r_surfsonstack = true;
	        }

	        r_maxedgesseen = 0;
	        r_maxsurfsseen = 0;

	        r_numallocatededges = (int)r_maxedges.value;

	        if (r_numallocatededges < MINEDGES)
		        r_numallocatededges = MINEDGES;

	        if (r_numallocatededges <= NUMSTACKEDGES)
	        {
                auxedges = new edge_t[r_numallocatededges];
                for (int kk = 0; kk < r_numallocatededges; kk++) auxedges[kk] = new edge_t();
            }

        	r_dowarpold = false;
	        r_viewchanged = false;
        }

        /*
        ===============
        R_SetVrect
        ===============
        */
        public static void R_SetVrect (vid.vrect_t pvrectin, vid.vrect_t pvrect, int lineadj)
        {
	        int		h;
	        double	size;

            size = screen.scr_viewsize.value > 100 ? 100 : screen.scr_viewsize.value;
	        if (client.cl.intermission != 0)
	        {
		        size = 100;
		        lineadj = 0;
	        }
	        size /= 100;

	        h = pvrectin.height - lineadj;
	        pvrect.width = (int)(pvrectin.width * size);
	        if (pvrect.width < 96)
	        {
                size = 96.0 / pvrectin.width;
		        pvrect.width = 96;	// min for icons
	        }
	        pvrect.width &= ~7;
	        pvrect.height = (int)(pvrectin.height * size);
	        if (pvrect.height > pvrectin.height - lineadj)
		        pvrect.height = pvrectin.height - lineadj;

	        pvrect.height &= ~1;

	        pvrect.x = (pvrectin.width - pvrect.width)/2;
	        pvrect.y = (h - pvrect.height)/2;

	        {
		        if (view.lcd_x.value != 0)
		        {
			        pvrect.y >>= 1;
			        pvrect.height >>= 1;
		        }
	        }
        }

        /*
        ===============
        R_ViewChanged

        Called every time the vid structure or r_refdef changes.
        Guaranteed to be called before the first refresh
        ===============
        */
        public static void R_ViewChanged(vid.vrect_t pvrect, int lineadj, double aspect)
        {
            int     i;
            double  res_scale;

            r_viewchanged = true;

            R_SetVrect(pvrect, r_refdef.vrect, lineadj);

            r_refdef.horizontalFieldOfView = 2.0 * Math.Tan(r_refdef.fov_x / 360 * mathlib.M_PI);
            r_refdef.fvrectx = (double)r_refdef.vrect.x;
            r_refdef.fvrectx_adj = (double)r_refdef.vrect.x - 0.5;
            r_refdef.vrect_x_adj_shift20 = (r_refdef.vrect.x << 20) + (1 << 19) - 1;
            r_refdef.fvrecty = (double)r_refdef.vrect.y;
            r_refdef.fvrecty_adj = (double)r_refdef.vrect.y - 0.5;
            r_refdef.vrectright = r_refdef.vrect.x + r_refdef.vrect.width;
            r_refdef.vrectright_adj_shift20 = (r_refdef.vrectright << 20) + (1 << 19) - 1;
            r_refdef.fvrectright = (double)r_refdef.vrectright;
            r_refdef.fvrectright_adj = (double)r_refdef.vrectright - 0.5;
            r_refdef.vrectrightedge = (double)r_refdef.vrectright - 0.99;
            r_refdef.vrectbottom = r_refdef.vrect.y + r_refdef.vrect.height;
            r_refdef.fvrectbottom = (double)r_refdef.vrectbottom;
            r_refdef.fvrectbottom_adj = (double)r_refdef.vrectbottom - 0.5;

            r_refdef.aliasvrect.x = (int)(r_refdef.vrect.x * r_aliasuvscale);
            r_refdef.aliasvrect.y = (int)(r_refdef.vrect.y * r_aliasuvscale);
            r_refdef.aliasvrect.width = (int)(r_refdef.vrect.width * r_aliasuvscale);
            r_refdef.aliasvrect.height = (int)(r_refdef.vrect.height * r_aliasuvscale);
            r_refdef.aliasvrectright = r_refdef.aliasvrect.x +
                    r_refdef.aliasvrect.width;
            r_refdef.aliasvrectbottom = r_refdef.aliasvrect.y +
                    r_refdef.aliasvrect.height;

            pixelAspect = aspect;
            xOrigin = r_refdef.xOrigin;
            yOrigin = r_refdef.yOrigin;

            screenAspect = r_refdef.vrect.width * pixelAspect /
                    r_refdef.vrect.height;
            // 320*200 1.0 pixelAspect = 1.6 screenAspect
            // 320*240 1.0 pixelAspect = 1.3333 screenAspect
            // proper 320*200 pixelAspect = 0.8333333

            verticalFieldOfView = r_refdef.horizontalFieldOfView / screenAspect;

            // values for perspective projection
            // if math were exact, the values would range from 0.5 to to range+0.5
            // hopefully they wll be in the 0.000001 to range+.999999 and truncate
            // the polygon rasterization will never render in the first row or column
            // but will definately render in the [range] row and column, so adjust the
            // buffer origin to get an exact edge to edge fill
            xcenter = ((double)r_refdef.vrect.width * XCENTERING) +
                    r_refdef.vrect.x - 0.5;
            aliasxcenter = xcenter * r_aliasuvscale;
            ycenter = ((double)r_refdef.vrect.height * YCENTERING) +
                    r_refdef.vrect.y - 0.5;
            aliasycenter = ycenter * r_aliasuvscale;

            xscale = r_refdef.vrect.width / r_refdef.horizontalFieldOfView;
            aliasxscale = xscale * r_aliasuvscale;
            xscaleinv = 1.0 / xscale;
            yscale = xscale * pixelAspect;
            aliasyscale = yscale * r_aliasuvscale;
            yscaleinv = 1.0 / yscale;
            xscaleshrink = (r_refdef.vrect.width - 6) / r_refdef.horizontalFieldOfView;
            yscaleshrink = xscaleshrink * pixelAspect;

            // left side clip
            screenedge[0].normal[0] = -1.0 / (xOrigin * r_refdef.horizontalFieldOfView);
            screenedge[0].normal[1] = 0;
            screenedge[0].normal[2] = 1;
            screenedge[0].type = bspfile.PLANE_ANYZ;

            // right side clip
            screenedge[1].normal[0] =
                    1.0 / ((1.0 - xOrigin) * r_refdef.horizontalFieldOfView);
            screenedge[1].normal[1] = 0;
            screenedge[1].normal[2] = 1;
            screenedge[1].type = bspfile.PLANE_ANYZ;

            // top side clip
            screenedge[2].normal[0] = 0;
            screenedge[2].normal[1] = -1.0 / (yOrigin * verticalFieldOfView);
            screenedge[2].normal[2] = 1;
            screenedge[2].type = bspfile.PLANE_ANYZ;

            // bottom side clip
            screenedge[3].normal[0] = 0;
            screenedge[3].normal[1] = 1.0 / ((1.0 - yOrigin) * verticalFieldOfView);
            screenedge[3].normal[2] = 1;
            screenedge[3].type = bspfile.PLANE_ANYZ;

            for (i = 0; i < 4; i++)
                mathlib.VectorNormalize(ref screenedge[i].normal);

            res_scale = Math.Sqrt((double)(r_refdef.vrect.width * r_refdef.vrect.height) /
                              (320.0 * 152.0)) *
                    (2.0 / r_refdef.horizontalFieldOfView);
            r_aliastransition = r_aliastransbase.value * res_scale;
            r_resfudge = r_aliastransadj.value * res_scale;

            if (screen.scr_fov.value <= 90.0)
                r_fov_greater_than_90 = false;
            else
                r_fov_greater_than_90 = true;

            draw.D_ViewChanged();
        }

        /*
        ===============
        R_MarkLeaves
        ===============
        */
        static void R_MarkLeaves()
        {
            byte[]                  vis;
            model.node_or_leaf_t    node;
            int                     i;

	        if (r_oldviewleaf == r_viewleaf)
		        return;

	        r_visframecount++;
	        r_oldviewleaf = r_viewleaf;

            vis = model.Mod_LeafPVS(r_viewleaf, client.cl.worldmodel);

            for (i = 0; i < client.cl.worldmodel.numleafs; i++)
            {
                if ((vis[i >> 3] & (1 << (i & 7))) != 0)
                {
                    node = client.cl.worldmodel.leafs[i + 1];
                    do
                    {
                        if (node.visframe == r_visframecount)
                            break;
                        node.visframe = r_visframecount;
                        node = node.parent;
                    } while (node != null);
                }
            }
        }
        
        /*
        =============
        R_DrawEntitiesOnList
        =============
        */
        static void R_DrawEntitiesOnList()
        {
	        int			i, j;
	        int			lnum;
	        alight_t	lighting = new alight_t();
        // FIXME: remove and do real lighting
	        double[]	lightvec = {-1, 0, 0};
	        double[]	dist = new double[3];
	        double		add;

	        if (r_drawentities.value == 0)
		        return;

	        for (i=0 ; i<client.cl_numvisedicts ; i++)
	        {
		        currententity = client.cl_visedicts[i];

		        if (currententity == client.cl_entities[client.cl.viewentity])
			        continue;	// don't draw the player

		        switch (currententity.model.type)
		        {
		        case model.modtype_t.mod_sprite:
			        mathlib.VectorCopy (currententity.origin, ref r_entorigin);
                    mathlib.VectorSubtract(r_origin, r_entorigin, ref modelorg);
			        R_DrawSprite ();
			        break;

                case model.modtype_t.mod_alias:
                    /*if (currententity.model.name.CompareTo("progs/flame2.mdl") != 0)
                        continue;*/
                    mathlib.VectorCopy(currententity.origin, ref r_entorigin);
                    mathlib.VectorSubtract(r_origin, r_entorigin, ref modelorg);

                // see if the bounding box lets us trivially reject, also sets
                // trivial accept status
                    if (R_AliasCheckBBox ())
                    {
        				j = R_LightPoint (currententity.origin);
        	
				        lighting.ambientlight = j;
				        lighting.shadelight = j;

				        lighting.plightvec = lightvec;

				        for (lnum=0 ; lnum<client.MAX_DLIGHTS ; lnum++)
				        {
                            if (client.cl_dlights[lnum].die >= client.cl.time)
					        {
						        mathlib.VectorSubtract (currententity.origin,
                                                client.cl_dlights[lnum].origin,
										        ref dist);
                                add = client.cl_dlights[lnum].radius - mathlib.Length(dist);
        	
						        if (add > 0)
							        lighting.ambientlight += (int)add;
					        }
				        }
        	
			        // clamp lighting so it doesn't overbright as much
				        if (lighting.ambientlight > 128)
					        lighting.ambientlight = 128;
				        if (lighting.ambientlight + lighting.shadelight > 192)
					        lighting.shadelight = 192 - lighting.ambientlight;

                        R_AliasDrawModel (lighting);
                    }
                    break;

		        default:
			        break;
		        }
	        }
        }

        /*
        =============
        R_DrawViewModel
        =============
        */
        static void R_DrawViewModel()
        {
        // FIXME: remove and do real lighting
	        double[]	    lightvec = {-1, 0, 0};
	        int			    j;
	        int			    lnum;
	        double[]	    dist = new double[3];
	        double		    add;
	        client.dlight_t	dl;

	        if (r_drawviewmodel.value == 0 || r_fov_greater_than_90)
		        return;

	        if ((client.cl.items & quakedef.IT_INVISIBILITY) != 0)
		        return;

            if (client.cl.stats[quakedef.STAT_HEALTH] <= 0)
		        return;

            currententity = client.cl.viewent;
	        if (currententity.model == null)
		        return;

            mathlib.VectorCopy(currententity.origin, ref r_entorigin);
            mathlib.VectorSubtract(r_origin, r_entorigin, ref modelorg);

	        mathlib.VectorCopy (vup, ref viewlightvec);
            mathlib.VectorInverse(ref viewlightvec);

	        j = R_LightPoint (currententity.origin);

	        if (j < 24)
		        j = 24;		// allways give some light on gun
	        r_viewlighting.ambientlight = j;
	        r_viewlighting.shadelight = j;

        // add dynamic lights		
	        for (lnum=0 ; lnum<client.MAX_DLIGHTS ; lnum++)
	        {
		        dl = client.cl_dlights[lnum];
		        if (dl.radius == 0)
			        continue;
		        if (dl.die < client.cl.time)
			        continue;

		        mathlib.VectorSubtract (currententity.origin, dl.origin, ref dist);
		        add = dl.radius - mathlib.Length(dist);
		        if (add > 0)
			        r_viewlighting.ambientlight += (int)add;
	        }

        // clamp lighting so it doesn't overbright as much
	        if (r_viewlighting.ambientlight > 128)
		        r_viewlighting.ambientlight = 128;
	        if (r_viewlighting.ambientlight + r_viewlighting.shadelight > 192)
		        r_viewlighting.shadelight = 192 - r_viewlighting.ambientlight;

	        r_viewlighting.plightvec = lightvec;

            R_AliasDrawModel(r_viewlighting);
        }

        /*
        =============
        R_BmodelCheckBBox
        =============
        */
        static int R_BmodelCheckBBox(model.model_t clmodel, double[] minmaxs)
        {
	        int			i, pindex, clipflags;
            double[]    acceptpt = new double[3], rejectpt = new double[3];
	        double		d;

	        clipflags = 0;

	        if (currententity.angles[0] != 0 || currententity.angles[1] != 0
		        || currententity.angles[2] != 0)
	        {
		        for (i=0 ; i<4 ; i++)
		        {
			        d = mathlib.DotProduct (currententity.origin, view_clipplanes[i].normal);
			        d -= view_clipplanes[i].dist;

			        if (d <= -clmodel.radius)
				        return BMODEL_FULLY_CLIPPED;

			        if (d <= clmodel.radius)
				        clipflags |= (1<<i);
		        }
	        }
	        else
	        {
		        for (i=0 ; i<4 ; i++)
		        {
		        // generate accept and reject points
		        // FIXME: do with fast look-ups or integer tests based on the sign bit
		        // of the floating point values

			        pindex = pfrustum_indexes[i];

                    rejectpt[0] = minmaxs[r_frustum_indexes[pindex + 0]];
                    rejectpt[1] = minmaxs[r_frustum_indexes[pindex + 1]];
                    rejectpt[2] = minmaxs[r_frustum_indexes[pindex + 2]];
        			
			        d = mathlib.DotProduct (rejectpt, view_clipplanes[i].normal);
			        d -= view_clipplanes[i].dist;

			        if (d <= 0)
				        return BMODEL_FULLY_CLIPPED;

                    acceptpt[0] = minmaxs[r_frustum_indexes[pindex + 3 + 0]];
                    acceptpt[1] = minmaxs[r_frustum_indexes[pindex + 3 + 1]];
                    acceptpt[2] = minmaxs[r_frustum_indexes[pindex + 3 + 2]];

			        d = mathlib.DotProduct (acceptpt, view_clipplanes[i].normal);
			        d -= view_clipplanes[i].dist;

			        if (d <= 0)
				        clipflags |= (1<<i);
		        }
	        }

	        return clipflags;
        }

        /*
        =============
        R_DrawBEntitiesOnList
        =============
        */
        static void R_DrawBEntitiesOnList ()
        {
	        int			    i, j, k, clipflags;
	        double[]	    oldorigin = new double[3];
	        model.model_t	clmodel;
	        double[]    	minmaxs = new double[6];

	        if (r_drawentities.value == 0)
		        return;

	        mathlib.VectorCopy (modelorg, ref oldorigin);
	        insubmodel = true;
	        r_dlightframecount = r_framecount;

	        for (i=0 ; i<client.cl_numvisedicts ; i++)
	        {
		        currententity = client.cl_visedicts[i];

		        switch (currententity.model.type)
		        {
		        case model.modtype_t.mod_brush:

			        clmodel = currententity.model;

		        // see if the bounding box lets us trivially reject, also sets
		        // trivial accept status
			        for (j=0 ; j<3 ; j++)
			        {
				        minmaxs[j] = currententity.origin[j] +
						        clmodel.mins[j];
				        minmaxs[3+j] = currententity.origin[j] +
						        clmodel.maxs[j];
			        }

			        clipflags = R_BmodelCheckBBox (clmodel, minmaxs);

			        if (clipflags != BMODEL_FULLY_CLIPPED)
			        {
				        mathlib.VectorCopy (currententity.origin, ref r_entorigin);
                        mathlib.VectorSubtract(r_origin, r_entorigin, ref modelorg);
			        // FIXME: is this needed?
                        mathlib.VectorCopy(modelorg, ref r_worldmodelorg);
        		
				        r_pcurrentvertbase = clmodel.vertexes;
        		
			        // FIXME: stop transforming twice
				        R_RotateBmodel ();

			        // calculate dynamic lighting for bmodel if it's not an
			        // instanced model
				        if (clmodel.firstmodelsurface != 0)
				        {
					        for (k=0 ; k<client.MAX_DLIGHTS ; k++)
					        {
						        if ((client.cl_dlights[k].die < client.cl.time) ||
                                    (client.cl_dlights[k].radius == 0))
						        {
							        continue;
						        }

                                R_MarkLights(client.cl_dlights[k], 1 << k,
							        clmodel.nodes[clmodel.hulls[0].firstclipnode]);
					        }
				        }

			        // if the driver wants polygons, deliver those. Z-buffering is on
			        // at this point, so no clipping to the world tree is needed, just
			        // frustum clipping
				        if (r_drawpolys | r_drawculledpolys)
				        {
					        R_ZDrawSubmodelPolys (clmodel);
				        }
				        else
				        {
					        r_pefragtopnode = null;

					        for (j=0 ; j<3 ; j++)
					        {
						        r_emins[j] = minmaxs[j];
						        r_emaxs[j] = minmaxs[3+j];
					        }

					        R_SplitEntityOnNode2 (client.cl.worldmodel.nodes[0]);

					        if (r_pefragtopnode != null)
					        {
						        currententity.topnode = r_pefragtopnode;
        	
						        if (r_pefragtopnode.contents >= 0)
						        {
						        // not a leaf; has to be clipped to the world BSP
							        r_clipflags = clipflags;
							        R_DrawSolidClippedSubmodelPolygons (clmodel);
						        }
						        else
						        {
						        // falls entirely in one leaf, so we just put all the
						        // edges in the edge list and let 1/z sorting handle
						        // drawing order
							        R_DrawSubmodelPolygons (clmodel, clipflags);
						        }
        	
						        currententity.topnode = null;
					        }
				        }

			        // put back world rotation and frustum clipping		
			        // FIXME: R_RotateBmodel should just work off base_vxx
                        mathlib.VectorCopy(base_vpn, ref vpn);
                        mathlib.VectorCopy(base_vup, ref vup);
                        mathlib.VectorCopy(base_vright, ref vright);
                        mathlib.VectorCopy(base_modelorg, ref modelorg);
				        mathlib.VectorCopy (oldorigin, ref modelorg);
				        R_TransformFrustum ();
			        }
			        break;

		        default:
			        break;
		        }
	        }

	        insubmodel = false;
        }
        
        /*
        ================
        R_EdgeDrawing
        ================
        */
        static void R_EdgeDrawing ()
        {
	        if (auxedges != null)
	        {
		        r_edges = auxedges;
	        }
	        else
	        {
	        }

	        if (r_surfsonstack)
	        {
	        }

	        R_BeginEdgeFrame ();

            R_RenderWorld();

	        if (r_drawculledpolys)
		        R_ScanEdges ();

            R_DrawBEntitiesOnList ();

            if (r_dspeeds.value == 0)
            {
                sound.S_ExtraUpdate();	// don't let sound get messed up if going slow
            }

	        if (!(r_drawpolys | r_drawculledpolys))
		        R_ScanEdges ();
        }
        
        /*
        ================
        R_RenderView

        r_refdef must be set before the first call
        ================
        */
        static void R_RenderView_ ()
        {
	        byte[]	warpbuffer = new byte[draw.WARP_WIDTH * draw.WARP_HEIGHT];

	        r_warpbuffer = warpbuffer;

            if (r_timegraph.value != 0 || r_speeds.value != 0 || r_dspeeds.value != 0)
                r_time1 = sys_linux.Sys_FloatTime();

        	R_SetupFrame ();

	        R_MarkLeaves ();	// done here so we know if we're in water

	        if (client.cl_entities[0].model == null || client.cl.worldmodel == null)
		        sys_linux.Sys_Error ("R_RenderView: NULL worldmodel");

            if (r_dspeeds.value == 0)
            {
                sound.S_ExtraUpdate();	// don't let sound get messed up if going slow
            }

	        R_EdgeDrawing ();

            if (r_dspeeds.value == 0)
            {
                sound.S_ExtraUpdate();	// don't let sound get messed up if going slow
            }

	        R_DrawEntitiesOnList ();

	        R_DrawViewModel ();

	        R_DrawParticles ();

	        if (r_dowarp)
		        draw.D_WarpScreen ();

            view.V_SetContentsColor(r_viewleaf.contents);

            if (r_timegraph.value != 0)
                R_TimeGraph();
        }

        public static void R_RenderView ()
        {
            R_RenderView_();
        }

        /*
        ================
        R_InitTurb
        ================
        */
        static void R_InitTurb ()
        {
            int i;

            for (i = 0; i < (SIN_BUFFER_SIZE); i++)
            {
                sintable[i] = (int)(AMP + Math.Sin(i * 3.14159 * 2 / draw.CYCLE) * AMP);
                intsintable[i] = (int)(AMP2 + Math.Sin(i * 3.14159 * 2 / draw.CYCLE) * AMP2);	// AMP2, not 20
            }
        }
    }
}
