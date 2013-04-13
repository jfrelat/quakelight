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
// d_sky.c

namespace quake
{
    public partial class draw
    {
        public const int SKY_SPAN_SHIFT	= 5;
        public const int SKY_SPAN_MAX   = (1 << SKY_SPAN_SHIFT);
        
        /*
        =================
        D_Sky_uv_To_st
        =================
        */
        static void D_Sky_uv_To_st (int u, int v, ref int s, ref int t)
        {
	        double	    wu, wv, temp;
	        double[]    end = new double[3];

            if (render.r_refdef.vrect.width >= render.r_refdef.vrect.height)
                temp = (double)render.r_refdef.vrect.width;
	        else
		        temp = (double)render.r_refdef.vrect.height;

	        wu = 8192.0 * (double)(u-((int)screen.vid.width>>1)) / temp;
            wv = 8192.0 * (double)(((int)screen.vid.height >> 1) - v) / temp;

            end[0] = 4096 * render.vpn[0] + wu * render.vright[0] + wv * render.vup[0];
            end[1] = 4096 * render.vpn[1] + wu * render.vright[1] + wv * render.vup[1];
            end[2] = 4096 * render.vpn[2] + wu * render.vright[2] + wv * render.vup[2];
	        end[2] *= 3;
	        mathlib.VectorNormalize (ref end);

            temp = render.skytime * render.skyspeed;	// TODO: add D_SetupFrame & set this there
	        s = (int)((temp + 6*(SKYSIZE/2-1)*end[0]) * 0x10000);
	        t = (int)((temp + 6*(SKYSIZE/2-1)*end[1]) * 0x10000);
        }
        
        /*
        =================
        D_DrawSkyScans8
        =================
        */
        static void D_DrawSkyScans8 (render.espan_t pspan)
        {
	        int				count, spancount, u, v;
	        int             pdest;
	        int		        s = 0, t = 0, snext = 0, tnext = 0, sstep, tstep;
	        int				spancountminus1;

	        sstep = 0;	// keep compiler happy
	        tstep = 0;	// ditto

	        do
	        {
		        pdest = (screenwidth * pspan.v) + pspan.u;

		        count = pspan.count;

	        // calculate the initial s & t
		        u = pspan.u;
		        v = pspan.v;
		        D_Sky_uv_To_st (u, v, ref s, ref t);

		        do
		        {
			        if (count >= SKY_SPAN_MAX)
				        spancount = SKY_SPAN_MAX;
			        else
				        spancount = count;

			        count -= spancount;

			        if (count != 0)
			        {
				        u += spancount;

			        // calculate s and t at far end of span,
			        // calculate s and t steps across span by shifting
				        D_Sky_uv_To_st (u, v, ref snext, ref tnext);

				        sstep = (snext - s) >> SKY_SPAN_SHIFT;
				        tstep = (tnext - t) >> SKY_SPAN_SHIFT;
			        }
			        else
			        {
			        // calculate s and t at last pixel in span,
			        // calculate s and t steps across span by division
				        spancountminus1 = (int)((double)(spancount - 1));

				        if (spancountminus1 > 0)
				        {
					        u += spancountminus1;
					        D_Sky_uv_To_st (u, v, ref snext, ref tnext);

					        sstep = (snext - s) / spancountminus1;
					        tstep = (tnext - t) / spancountminus1;
				        }
			        }

			        do
			        {
                        d_viewbuffer[pdest++] = render.r_skysource[((t & R_SKY_TMASK) >> 8) +
						        ((s & R_SKY_SMASK) >> 16)];
				        s += sstep;
				        t += tstep;
			        } while (--spancount > 0);

			        s = snext;
			        t = tnext;

		        } while (count > 0);

	        } while ((pspan = pspan.pnext) != null);
        }
    }
}
