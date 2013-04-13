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
// d_modech.c: called when mode has just changed

namespace quake
{
    public partial class draw
    {
        static int	    d_vrectx, d_vrecty, d_vrectright_particle, d_vrectbottom_particle;

        static int	    d_y_aspect_shift, d_pix_min, d_pix_max, d_pix_shift;

        static int[]	d_scantable = new int[render.MAXHEIGHT];
        static int[]    zspantable = new int[render.MAXHEIGHT];

        /*
        ================
        D_ViewChanged
        ================
        */
        public static void D_ViewChanged ()
        {
	        int rowbytes;

	        if (render.r_dowarp)
		        rowbytes = WARP_WIDTH;
	        else
		        rowbytes = (int)screen.vid.rowbytes;

	        scale_for_mip = render.xscale;
	        if (render.yscale > render.xscale)
		        scale_for_mip = render.yscale;

	        d_zrowbytes = screen.vid.width * 2;
            d_zwidth = screen.vid.width;

	        d_pix_min = render.r_refdef.vrect.width / 320;
	        if (d_pix_min < 1)
		        d_pix_min = 1;

	        d_pix_max = (int)((double)render.r_refdef.vrect.width / (320.0 / 4.0) + 0.5);
	        d_pix_shift = 8 - (int)((double)render.r_refdef.vrect.width / 320.0 + 0.5);
	        if (d_pix_max < 1)
		        d_pix_max = 1;

	        if (render.pixelAspect > 1.4)
		        d_y_aspect_shift = 1;
	        else
		        d_y_aspect_shift = 0;

	        d_vrectx = render.r_refdef.vrect.x;
            d_vrecty = render.r_refdef.vrect.y;
            d_vrectright_particle = render.r_refdef.vrectright - d_pix_max;
	        d_vrectbottom_particle =
                    render.r_refdef.vrectbottom - (d_pix_max << d_y_aspect_shift);

	        {
		        int		i;

		        for (i=0 ; i<screen.vid.height; i++)
		        {
			        d_scantable[i] = i*rowbytes;
			        zspantable[i] = (int)(i*d_zwidth);
		        }
	        }
        }
    }
}
