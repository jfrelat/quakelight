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
// d_part.c: software driver module for drawing particles

namespace quake
{
    public partial class draw
    {
        /*
        ==============
        D_DrawParticle
        ==============
        */
        public static void D_DrawParticle (particle_t pparticle)
        {
            double[]    local = new double[3], transformed = new double[3];
	        double	    zi;
	        int	        pdest;
	        int	        pz;
	        int		    i, izi, pix, count, u, v;

        // transform point
	        mathlib.VectorSubtract (pparticle.org, render.r_origin, ref local);

	        transformed[0] = mathlib.DotProduct(local, render.r_pright);
            transformed[1] = mathlib.DotProduct(local, render.r_pup);
            transformed[2] = mathlib.DotProduct(local, render.r_ppn);		

	        if (transformed[2] < PARTICLE_Z_CLIP)
		        return;

        // project the point
        // FIXME: preadjust xcenter and ycenter
            zi = 1.0 / transformed[2];
	        u = (int)(render.xcenter + zi * transformed[0] + 0.5);
	        v = (int)(render.ycenter - zi * transformed[1] + 0.5);

	        if ((v > d_vrectbottom_particle) || 
		        (u > d_vrectright_particle) ||
		        (v < d_vrecty) ||
		        (u < d_vrectx))
	        {
		        return;
	        }

	        pz = (int)((d_zwidth * v) + u);
	        pdest = d_scantable[v] + u;
	        izi = (int)(zi * 0x8000);

	        pix = izi >> d_pix_shift;

	        if (pix < d_pix_min)
		        pix = d_pix_min;
	        else if (pix > d_pix_max)
		        pix = d_pix_max;

	        switch (pix)
	        {
	        case 1:
		        count = 1 << d_y_aspect_shift;

		        for ( ; count != 0 ; count--, pz += (int)d_zwidth, pdest += screenwidth)
		        {
                    if (d_pzbuffer[pz+0] <= izi)
			        {
                        d_pzbuffer[pz+0] = (short)izi;
				        d_viewbuffer[pdest+0] = (byte)pparticle.color;
			        }
		        }
		        break;

	        case 2:
		        count = 2 << d_y_aspect_shift;

		        for ( ; count != 0 ; count--, pz += (int)d_zwidth, pdest += screenwidth)
		        {
			        if (d_pzbuffer[pz+0] <= izi)
			        {
				        d_pzbuffer[pz+0] = (short)izi;
				        d_viewbuffer[pdest+0] = (byte)pparticle.color;
			        }

			        if (d_pzbuffer[pz+1] <= izi)
			        {
				        d_pzbuffer[pz+1] = (short)izi;
                        d_viewbuffer[pdest+1] = (byte)pparticle.color;
			        }
		        }
		        break;

	        case 3:
		        count = 3 << d_y_aspect_shift;

		        for ( ; count != 0 ; count--, pz += (int)d_zwidth, pdest += screenwidth)
		        {
			        if (d_pzbuffer[pz+0] <= izi)
			        {
				        d_pzbuffer[pz+0] = (short)izi;
				        d_viewbuffer[pdest+0] = (byte)pparticle.color;
			        }

			        if (d_pzbuffer[pz+1] <= izi)
			        {
				        d_pzbuffer[pz+1] = (short)izi;
				        d_viewbuffer[pdest+1] = (byte)pparticle.color;
			        }

			        if (d_pzbuffer[pz+2] <= izi)
			        {
				        d_pzbuffer[pz+2] = (short)izi;
				        d_viewbuffer[pdest+2] = (byte)pparticle.color;
			        }
		        }
		        break;

	        case 4:
		        count = 4 << d_y_aspect_shift;

		        for ( ; count != 0 ; count--, pz += (int)d_zwidth, pdest += screenwidth)
		        {
			        if (d_pzbuffer[pz+0] <= izi)
			        {
				        d_pzbuffer[pz+0] = (short)izi;
                        d_viewbuffer[pdest+0] = (byte)pparticle.color;
			        }

			        if (d_pzbuffer[pz+1] <= izi)
			        {
                        d_pzbuffer[pz+1] = (short)izi;
                        d_viewbuffer[pdest+1] = (byte)pparticle.color;
			        }

			        if (d_pzbuffer[pz+2] <= izi)
			        {
                        d_pzbuffer[pz+2] = (short)izi;
                        d_viewbuffer[pdest+2] = (byte)pparticle.color;
			        }

			        if (d_pzbuffer[pz+3] <= izi)
			        {
                        d_pzbuffer[pz+3] = (short)izi;
				        d_viewbuffer[pdest+3] = (byte)pparticle.color;
			        }
		        }
		        break;

	        default:
		        count = pix << d_y_aspect_shift;

		        for ( ; count != 0 ; count--, pz += (int)d_zwidth, pdest += screenwidth)
		        {
			        for (i=0 ; i<pix ; i++)
			        {
				        if (d_pzbuffer[pz+i] <= izi)
				        {
					        d_pzbuffer[pz+i] = (short)izi;
					        d_viewbuffer[pdest+i] = (byte)pparticle.color;
				        }
			        }
		        }
		        break;
	        }
        }
    }
}
