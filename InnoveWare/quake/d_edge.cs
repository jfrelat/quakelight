using System.Threading;

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
// d_edge.c

namespace quake
{
    public partial class draw
    {
        static int	    miplevel;

        static double	scale_for_mip;
        static int	    screenwidth;
        static int		ubasestep, errorterm, erroradjustup, erroradjustdown;
        int			vstartscan;

        static double[]	transformed_modelorg = new double[3];

        /*
        =============
        D_MipLevelForScale
        =============
        */
        static int D_MipLevelForScale (double scale)
        {
	        int		lmiplevel;

	        if (scale >= d_scalemip[0] )
		        lmiplevel = 0;
	        else if (scale >= d_scalemip[1] )
		        lmiplevel = 1;
	        else if (scale >= d_scalemip[2] )
		        lmiplevel = 2;
	        else
		        lmiplevel = 3;

	        if (lmiplevel < d_minmip)
		        lmiplevel = d_minmip;

	        return lmiplevel;
        }

        /*
        ==============
        D_DrawSolidSurface
        ==============
        */

        // FIXME: clean this up

        static void D_DrawSolidSurface (render.surf_t surf, int color)
        {
            render.espan_t  span;
            int             pdest;
            int             u, u2, pix;

            pix = (color << 24) | (color << 16) | (color << 8) | color;
            for (span = surf.spans; span != null; span = span.pnext)
            {
                pdest = screenwidth * span.v;
                u = span.u;
                u2 = span.u + span.count - 1;

                d_viewbuffer[pdest + u] = (byte)pix;

                if (u2 - u < 8)
                {
                    for (u++; u <= u2; u++)
                        d_viewbuffer[pdest + u] = (byte)pix;
                }
                else
                {
                    for (u++; (u & 3) != 0; u++)
                        d_viewbuffer[pdest + u] = (byte)pix;

                    u2 -= 4;
                    for (; u <= u2; u += 4)
                    {
                        d_viewbuffer[pdest + u] = (byte)(pix >> 24);
                        d_viewbuffer[pdest + u + 1] = (byte)(pix >> 16);
                        d_viewbuffer[pdest + u + 2] = (byte)(pix >> 8);
                        d_viewbuffer[pdest + u + 3] = (byte)pix;
                    }
                    u2 += 4;
                    for (; u <= u2; u++)
                        d_viewbuffer[pdest + u] = (byte)pix;
                }
            }
        }

        /*
        ==============
        D_CalcGradients
        ==============
        */
        static void D_CalcGradients (model.msurface_t pface)
        {
            model.mplane_t  pplane;
            double          mipscale;
            double[]        p_temp1 = new double[3];
            double[]        p_saxis = new double[3], p_taxis = new double[3];
            double          t;

            pplane = pface.plane;

            mipscale = 1.0 / (double)(1 << miplevel);

            render.TransformVector(pface.texinfo.vecs[0], ref p_saxis);
            render.TransformVector(pface.texinfo.vecs[1], ref p_taxis);

            t = render.xscaleinv * mipscale;
            d_sdivzstepu = p_saxis[0] * t;
            d_tdivzstepu = p_taxis[0] * t;

            t = render.yscaleinv * mipscale;
            d_sdivzstepv = -p_saxis[1] * t;
            d_tdivzstepv = -p_taxis[1] * t;

            d_sdivzorigin = p_saxis[2] * mipscale - render.xcenter * d_sdivzstepu -
                    render.ycenter * d_sdivzstepv;
            d_tdivzorigin = p_taxis[2] * mipscale - render.xcenter * d_tdivzstepu -
                    render.ycenter * d_tdivzstepv;

            mathlib.VectorScale(transformed_modelorg, mipscale, ref p_temp1);

            t = 0x10000 * mipscale;
            sadjust = ((int)(mathlib.DotProduct(p_temp1, p_saxis) * 0x10000 + 0.5)) -
                    ((pface.texturemins[0] << 16) >> miplevel)
                    + (int)(pface.texinfo.vecs[0][3] * t);
            tadjust = ((int)(mathlib.DotProduct(p_temp1, p_taxis) * 0x10000 + 0.5)) -
                    ((pface.texturemins[1] << 16) >> miplevel)
                    + (int)(pface.texinfo.vecs[1][3] * t);

            //
            // -1 (-epsilon) so we never wander off the edge of the texture
            //
            bbextents = ((pface.extents[0] << 16) >> miplevel) - 1;
            bbextentt = ((pface.extents[1] << 16) >> miplevel) - 1;
        }

        /*
        ==============
        D_DrawSurfaces
        ==============
        */
        public static void D_DrawSurfaces ()
        {
	        render.surf_t		s;
	        model.msurface_t	pface;
            surfcache_t         pcurrentcache;
            double[]            world_transformed_modelorg = new double[3];
	        double[]		    local_modelorg = new double[3];

	        render.currententity = client.cl_entities[0];
	        render.TransformVector (render.modelorg, ref transformed_modelorg);
	        mathlib.VectorCopy (transformed_modelorg, ref world_transformed_modelorg);

        // TODO: could preset a lot of this at mode set time
            if (render.r_drawflat.value != 0)
            {
                for (int i = 0; i < render.surface_p; i++)
                {
                    s = render.surfaces[i];
                    if (s.spans == null)
                        continue;

                    d_zistepu = s.d_zistepu;
                    d_zistepv = s.d_zistepv;
                    d_ziorigin = s.d_ziorigin;

                    int color = 0;
                    if (s.data != null)
                        color = ((model.msurface_t)s.data).color;
                    D_DrawSolidSurface(s, (int)color & 0xFF);
                    D_DrawZSpans(s.spans);
                }
            }
            else
            {
//                sys_linux.Sys_Printf("surface_p = " + render.surface_p);
                for (int i = 0; i < render.surface_p; i++)
                {
                    s = render.surfaces[i];
                    if (s.spans == null)
                        continue;

                    render.r_drawnpolycount++;

                    d_zistepu = s.d_zistepu;
                    d_zistepv = s.d_zistepv;
                    d_ziorigin = s.d_ziorigin;

                    if ((s.flags & model.SURF_DRAWSKY) != 0)
                    {
                        if (render.r_skymade == 0)
                        {
                            render.R_MakeSky();
                        }

                        D_DrawSkyScans8(s.spans);
                        D_DrawZSpans(s.spans);
                    }
                    else if ((s.flags & model.SURF_DRAWBACKGROUND) != 0)
                    {
                        // set up a gradient for the background surface that places it
                        // effectively at infinity distance from the viewpoint
                        d_zistepu = 0;
                        d_zistepv = 0;
                        d_ziorigin = -0.9;

                        D_DrawSolidSurface(s, (int)render.r_clearcolor.value & 0xFF);
                        D_DrawZSpans(s.spans);
                    }
                    else if ((s.flags & model.SURF_DRAWTURB) != 0)
                    {
                        pface = (model.msurface_t)s.data;
                        miplevel = 0;
                        cacheblock = pface.texinfo.texture.pixels;
                        cacheofs = (int)pface.texinfo.texture.offsets[0];
                        cachewidth = 64;

                        if (s.insubmodel)
                        {
                            // FIXME: we don't want to do all this for every polygon!
                            // TODO: store once at start of frame
                            render.currententity = s.entity;	//FIXME: make this passed in to
                            // R_RotateBmodel ()
                            mathlib.VectorSubtract(render.r_origin, render.currententity.origin,
                                    ref local_modelorg);
                            render.TransformVector(local_modelorg, ref transformed_modelorg);

                            render.R_RotateBmodel();	// FIXME: don't mess with the frustum,
                            // make entity passed in
                        }

                        D_CalcGradients(pface);
                        Turbulent8(s.spans);
                        D_DrawZSpans(s.spans);

                        if (s.insubmodel)
                        {
                            //
                            // restore the old drawing state
                            // FIXME: we don't want to do this every time!
                            // TODO: speed up
                            //
                            render.currententity = client.cl_entities[0];
                            mathlib.VectorCopy(world_transformed_modelorg,
                                        ref transformed_modelorg);
                            mathlib.VectorCopy(render.base_vpn, ref render.vpn);
                            mathlib.VectorCopy(render.base_vup, ref render.vup);
                            mathlib.VectorCopy(render.base_vright, ref render.vright);
                            mathlib.VectorCopy(render.base_modelorg, ref render.modelorg);
                            render.R_TransformFrustum();
                        }
                    }
                    else
                    {
                        if (s.insubmodel)
                        {
                            // FIXME: we don't want to do all this for every polygon!
                            // TODO: store once at start of frame
                            render.currententity = s.entity;	//FIXME: make this passed in to
                            // R_RotateBmodel ()
                            mathlib.VectorSubtract(render.r_origin, render.currententity.origin, ref local_modelorg);
                            render.TransformVector(local_modelorg, ref transformed_modelorg);

                            render.R_RotateBmodel();	// FIXME: don't mess with the frustum,
                            // make entity passed in
                        }

                        pface = (model.msurface_t)s.data;
                        miplevel = D_MipLevelForScale(s.nearzi * scale_for_mip
                        * pface.texinfo.mipadjust);

                        // FIXME: make this passed in to D_CacheSurface
                        pcurrentcache = D_CacheSurface(pface, miplevel);

                        cacheblock = pcurrentcache.data;
                        cachewidth = (int)pcurrentcache.width;

                        D_CalcGradients(pface);

                        d_pdrawspans(s.spans);

                        D_DrawZSpans(s.spans);

                        if (s.insubmodel)
                        {
                            //
                            // restore the old drawing state
                            // FIXME: we don't want to do this every time!
                            // TODO: speed up
                            //
                            render.currententity = client.cl_entities[0];
                            mathlib.VectorCopy(world_transformed_modelorg,
                                        ref transformed_modelorg);
                            mathlib.VectorCopy(render.base_vpn, ref render.vpn);
                            mathlib.VectorCopy(render.base_vup, ref render.vup);
                            mathlib.VectorCopy(render.base_vright, ref render.vright);
                            mathlib.VectorCopy(render.base_modelorg, ref render.modelorg);
                            render.R_TransformFrustum();
                        }
                    }
                }
            }
        }
    }
}
