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
// d_sprite.c: software top-level rasterization driver module for drawing
// sprites

namespace quake
{
    public partial class draw
    {
        static int		    sprite_height;
        static int		    minindex, maxindex;
        static sspan_t[]	sprite_spans;

        /*
        =====================
        D_SpriteDrawSpans
        =====================
        */
        static void D_SpriteDrawSpans (sspan_t[] span)
        {
            int         pspan = 0;
	        int			count, spancount, izistep;
	        int			izi;
	        byte[]		pbase;
            int         pdest;
	        int	        s, t, snext, tnext, sstep, tstep;
	        double		sdivz, tdivz, zi, z, du, dv, spancountminus1;
	        double		sdivz8stepu, tdivz8stepu, zi8stepu;
	        byte		btemp;
            int         pz;

	        sstep = 0;	// keep compiler happy
	        tstep = 0;	// ditto

	        pbase = cacheblock;

	        sdivz8stepu = d_sdivzstepu * 8;
	        tdivz8stepu = d_tdivzstepu * 8;
	        zi8stepu = d_zistepu * 8;

        // we count on FP exceptions being turned off to avoid range problems
	        izistep = (int)(d_zistepu * 0x8000 * 0x10000);

	        do
	        {
                pdest = (screenwidth * span[pspan].v) + span[pspan].u;
                pz = (int)((d_zwidth * span[pspan].v) + span[pspan].u);

		        count = span[pspan].count;

		        if (count <= 0)
			        goto NextSpan;

	        // calculate the initial s/z, t/z, 1/z, s, and t and clamp
                du = (double)span[pspan].u;
                dv = (double)span[pspan].v;

		        sdivz = d_sdivzorigin + dv*d_sdivzstepv + du*d_sdivzstepu;
		        tdivz = d_tdivzorigin + dv*d_tdivzstepv + du*d_tdivzstepu;
		        zi = d_ziorigin + dv*d_zistepv + du*d_zistepu;
		        z = (double)0x10000 / zi;	// prescale to 16.16 fixed-point
	        // we count on FP exceptions being turned off to avoid range problems
		        izi = (int)(zi * 0x8000 * 0x10000);

		        s = (int)(sdivz * z) + sadjust;
		        if (s > bbextents)
			        s = bbextents;
		        else if (s < 0)
			        s = 0;

		        t = (int)(tdivz * z) + tadjust;
		        if (t > bbextentt)
			        t = bbextentt;
		        else if (t < 0)
			        t = 0;

		        do
		        {
		        // calculate s and t at the far end of the span
			        if (count >= 8)
				        spancount = 8;
			        else
				        spancount = count;

			        count -= spancount;

			        if (count != 0)
			        {
			        // calculate s/z, t/z, zi.fixed s and t at far end of span,
			        // calculate s and t steps across span by shifting
				        sdivz += sdivz8stepu;
				        tdivz += tdivz8stepu;
				        zi += zi8stepu;
				        z = (double)0x10000 / zi;	// prescale to 16.16 fixed-point

				        snext = (int)(sdivz * z) + sadjust;
				        if (snext > bbextents)
					        snext = bbextents;
				        else if (snext < 8)
					        snext = 8;	// prevent round-off error on <0 steps from
								        //  from causing overstepping & running off the
								        //  edge of the texture

				        tnext = (int)(tdivz * z) + tadjust;
				        if (tnext > bbextentt)
					        tnext = bbextentt;
				        else if (tnext < 8)
					        tnext = 8;	// guard against round-off error on <0 steps

				        sstep = (snext - s) >> 3;
				        tstep = (tnext - t) >> 3;
			        }
			        else
			        {
			        // calculate s/z, t/z, zi.fixed s and t at last pixel in span (so
			        // can't step off polygon), clamp, calculate s and t steps across
			        // span by division, biasing steps low so we don't run off the
			        // texture
				        spancountminus1 = (double)(spancount - 1);
				        sdivz += d_sdivzstepu * spancountminus1;
				        tdivz += d_tdivzstepu * spancountminus1;
				        zi += d_zistepu * spancountminus1;
				        z = (double)0x10000 / zi;	// prescale to 16.16 fixed-point
				        snext = (int)(sdivz * z) + sadjust;
				        if (snext > bbextents)
					        snext = bbextents;
				        else if (snext < 8)
					        snext = 8;	// prevent round-off error on <0 steps from
								        //  from causing overstepping & running off the
								        //  edge of the texture

				        tnext = (int)(tdivz * z) + tadjust;
				        if (tnext > bbextentt)
					        tnext = bbextentt;
				        else if (tnext < 8)
					        tnext = 8;	// guard against round-off error on <0 steps

				        if (spancount > 1)
				        {
					        sstep = (snext - s) / (spancount - 1);
					        tstep = (tnext - t) / (spancount - 1);
				        }
			        }

			        do
			        {
				        btemp = pbase[(s >> 16) + (t >> 16) * cachewidth];
				        if (btemp != 255)
				        {
                            if (d_pzbuffer[pz] <= (izi >> 16))
					        {
                                d_pzbuffer[pz] = (short)(izi >> 16);
                                d_viewbuffer[pdest] = btemp;
					        }
				        }

				        izi += izistep;
				        pdest++;
				        pz++;
				        s += sstep;
				        t += tstep;
			        } while (--spancount > 0);

			        s = snext;
			        t = tnext;

		        } while (count > 0);

        NextSpan:
		        pspan++;

	        } while (span[pspan].count != DS_SPAN_LIST_END);
        }

        /*
        =====================
        D_SpriteScanLeftEdge
        =====================
        */
        static void D_SpriteScanLeftEdge ()
        {
	        int			    i, v, itop, ibottom, lmaxindex;
	        emitpoint_t	    pvert, pnext;
	        int		        pspan;
	        double		    du, dv, vtop, vbottom, slope;
	        int	            u, u_step;

	        pspan = 0;
	        i = minindex;
	        if (i == 0)
		        i = render.r_spritedesc.nump;

	        lmaxindex = maxindex;
	        if (lmaxindex == 0)
                lmaxindex = render.r_spritedesc.nump;

            vtop = Math.Ceiling(render.r_spritedesc.pverts[i].v);

	        do
	        {
                pvert = render.r_spritedesc.pverts[i];
		        pnext = render.r_spritedesc.pverts[i - 1];

                vbottom = Math.Ceiling(pnext.v);

		        if (vtop < vbottom)
		        {
			        du = pnext.u - pvert.u;
			        dv = pnext.v - pvert.v;
			        slope = du / dv;
			        u_step = (int)(slope * 0x10000);
		        // adjust u to ceil the integer portion
			        u = (int)((pvert.u + (slope * (vtop - pvert.v))) * 0x10000) +
					        (0x10000 - 1);
			        itop = (int)vtop;
			        ibottom = (int)vbottom;


                    if (ibottom - itop < render.MAXHEIGHT)
                    {
                        for (v = itop; v < ibottom; v++)
                        {
                            sprite_spans[pspan].u = u >> 16;
                            sprite_spans[pspan].v = v;
                            u += u_step;
                            pspan++;
                        }
                    }
                    else
                        ibottom = ibottom;
		        }

		        vtop = vbottom;

		        i--;
		        if (i == 0)
                    i = render.r_spritedesc.nump;

	        } while (i != lmaxindex);
        }
        
        /*
        =====================
        D_SpriteScanRightEdge
        =====================
        */
        static void D_SpriteScanRightEdge ()
        {
	        int			i, v, itop, ibottom;
	        emitpoint_t	pvert, pnext;
	        int		    pspan;
	        double		du, dv, vtop, vbottom, slope, uvert, unext, vvert, vnext;
	        int     	u, u_step;

	        pspan = 0;
	        i = minindex;

	        vvert = render.r_spritedesc.pverts[i].v;
            if (vvert < render.r_refdef.fvrecty_adj)
                vvert = render.r_refdef.fvrecty_adj;
            if (vvert > render.r_refdef.fvrectbottom_adj)
                vvert = render.r_refdef.fvrectbottom_adj;

            vtop = Math.Ceiling(vvert);

	        do
	        {
		        pvert = render.r_spritedesc.pverts[i];
                pnext = render.r_spritedesc.pverts[i + 1];

		        vnext = pnext.v;
		        if (vnext < render.r_refdef.fvrecty_adj)
                    vnext = render.r_refdef.fvrecty_adj;
                if (vnext > render.r_refdef.fvrectbottom_adj)
                    vnext = render.r_refdef.fvrectbottom_adj;

                vbottom = Math.Ceiling(vnext);

		        if (vtop < vbottom)
		        {
			        uvert = pvert.u;
                    if (uvert < render.r_refdef.fvrectx_adj)
                        uvert = render.r_refdef.fvrectx_adj;
                    if (uvert > render.r_refdef.fvrectright_adj)
                        uvert = render.r_refdef.fvrectright_adj;

			        unext = pnext.u;
                    if (unext < render.r_refdef.fvrectx_adj)
                        unext = render.r_refdef.fvrectx_adj;
                    if (unext > render.r_refdef.fvrectright_adj)
                        unext = render.r_refdef.fvrectright_adj;

			        du = unext - uvert;
			        dv = vnext - vvert;
			        slope = du / dv;
			        u_step = (int)(slope * 0x10000);
		        // adjust u to ceil the integer portion
			        u = (int)((uvert + (slope * (vtop - vvert))) * 0x10000) +
					        (0x10000 - 1);
			        itop = (int)vtop;
			        ibottom = (int)vbottom;

			        for (v=itop ; v<ibottom ; v++)
			        {
                        sprite_spans[pspan].count = (u >> 16) - sprite_spans[pspan].u;
				        u += u_step;
				        pspan++;
			        }
		        }

		        vtop = vbottom;
		        vvert = vnext;

		        i++;
		        if (i == render.r_spritedesc.nump)
			        i = 0;

	        } while (i != maxindex);

            sprite_spans[pspan].count = DS_SPAN_LIST_END;	// mark the end of the span list 
        }
        
        /*
        =====================
        D_SpriteCalculateGradients
        =====================
        */
        static void D_SpriteCalculateGradients ()
        {
            double[]    p_normal = new double[3], p_saxis = new double[3], p_taxis = new double[3], p_temp1 = new double[3];
	        double		distinv;

	        render.TransformVector (render.r_spritedesc.vpn, ref p_normal);
            render.TransformVector (render.r_spritedesc.vright, ref p_saxis);
            render.TransformVector (render.r_spritedesc.vup, ref p_taxis);
            mathlib.VectorInverse (ref p_taxis);

            distinv = 1.0 / (-mathlib.DotProduct(render.modelorg, render.r_spritedesc.vpn));

	        d_sdivzstepu = p_saxis[0] * render.xscaleinv;
            d_tdivzstepu = p_taxis[0] * render.xscaleinv;

            d_sdivzstepv = -p_saxis[1] * render.yscaleinv;
            d_tdivzstepv = -p_taxis[1] * render.yscaleinv;

            d_zistepu = p_normal[0] * render.xscaleinv * distinv;
            d_zistepv = -p_normal[1] * render.yscaleinv * distinv;

	        d_sdivzorigin = p_saxis[2] - render.xcenter * d_sdivzstepu -
                    render.ycenter * d_sdivzstepv;
            d_tdivzorigin = p_taxis[2] - render.xcenter * d_tdivzstepu -
                    render.ycenter * d_tdivzstepv;
            d_ziorigin = p_normal[2] * distinv - render.xcenter * d_zistepu -
                    render.ycenter * d_zistepv;

	        render.TransformVector (render.modelorg, ref p_temp1);

            sadjust = ((int)(mathlib.DotProduct(p_temp1, p_saxis) * 0x10000 + 0.5)) -
			        (-(cachewidth >> 1) << 16);
	        tadjust = ((int)(mathlib.DotProduct (p_temp1, p_taxis) * 0x10000 + 0.5)) -
			        (-(sprite_height >> 1) << 16);

        // -1 (-epsilon) so we never wander off the edge of the texture
	        bbextents = (cachewidth << 16) - 1;
	        bbextentt = (sprite_height << 16) - 1;
        }
        
        /*
        =====================
        D_DrawSprite
        =====================
        */
        public static void D_DrawSprite ()
        {
	        int			    i, nump;
	        double		    ymin, ymax;
	        emitpoint_t[]	pverts;
	        sspan_t[]	    spans = new sspan_t[render.MAXHEIGHT+1];

            for (int kk = 0; kk < render.MAXHEIGHT + 1; kk++)
                spans[kk] = new sspan_t();

	        sprite_spans = spans;

        // find the top and bottom vertices, and make sure there's at least one scan to
        // draw
            ymin = 999999.9;
	        ymax = -999999.9;
	        pverts = render.r_spritedesc.pverts;

	        for (i=0 ; i<render.r_spritedesc.nump ; i++)
	        {
		        if (pverts[i].v < ymin)
		        {
                    ymin = pverts[i].v;
			        minindex = i;
		        }

                if (pverts[i].v > ymax)
		        {
                    ymax = pverts[i].v;
			        maxindex = i;
		        }
	        }

            ymin = Math.Ceiling(ymin);
            ymax = Math.Ceiling(ymax);

	        if (ymin >= ymax)
		        return;		// doesn't cross any scans at all

        	cachewidth = render.r_spritedesc.pspriteframe.width;
	        sprite_height = render.r_spritedesc.pspriteframe.height;
            cacheblock = (byte[])render.r_spritedesc.pspriteframe.pixels;

        // copy the first vertex to the last vertex, so we don't have to deal with
        // wrapping
	        nump = render.r_spritedesc.nump;
            pverts = render.r_spritedesc.pverts;
	        pverts[nump] = pverts[0];

	        D_SpriteCalculateGradients ();
	        D_SpriteScanLeftEdge ();
	        D_SpriteScanRightEdge ();
	        D_SpriteDrawSpans (sprite_spans);
        }
    }
}
