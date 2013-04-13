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
// d_init.c: rasterization driver initialization

namespace quake
{
    public partial class draw
    {
        public const int NUM_MIPS	= 4;

        static cvar_t	d_subdiv16 = new cvar_t("d_subdiv16", "1");
        static cvar_t	d_mipcap = new cvar_t("d_mipcap", "0");
        static cvar_t	d_mipscale = new cvar_t("d_mipscale", "1");

        surfcache_t		d_initial_rover;
        static bool		d_roverwrapped;
        static int      d_minmip;
        static double[]	d_scalemip = new double[NUM_MIPS-1];

        static double[]	basemip = {1.0, 0.5*0.8, 0.25*0.8};

        delegate void d_drawspans (render.espan_t pspan);
        static d_drawspans d_pdrawspans;

        /*
        ===============
        D_Init
        ===============
        */
        public static void D_Init ()
        {
//	        r_skydirect = 1;

	        cvar_t.Cvar_RegisterVariable (d_subdiv16);
	        cvar_t.Cvar_RegisterVariable (d_mipcap);
	        cvar_t.Cvar_RegisterVariable (d_mipscale);

            render.r_drawpolys = false;
            render.r_worldpolysbacktofront = false;
            render.r_recursiveaffinetriangles = true;
	        render.r_pixbytes = 1;
            render.r_aliasuvscale = 1.0;
        }

        /*
        ===============
        D_SetupFrame
        ===============
        */
        public static void D_SetupFrame ()
        {
	        int		i;

	        if (render.r_dowarp)
		        d_viewbuffer = render.r_warpbuffer;
	        else
		        d_viewbuffer = screen.vid.buffer;

	        if (render.r_dowarp)
		        screenwidth = WARP_WIDTH;
	        else
		        screenwidth = (int)screen.vid.rowbytes;

	        d_roverwrapped = false;

	        d_minmip = (int)d_mipcap.value;
	        if (d_minmip > 3)
		        d_minmip = 3;
	        else if (d_minmip < 0)
		        d_minmip = 0;

	        for (i=0 ; i<(NUM_MIPS-1) ; i++)
		        d_scalemip[i] = basemip[i] * d_mipscale.value;

			d_pdrawspans = D_DrawSpans8;
        }
    }
}