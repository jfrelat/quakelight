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
// r_sky.c

namespace quake
{
    public partial class render
    {
        static int		iskyspeed = 8;
        static int      iskyspeed2 = 2;
        public static double    skyspeed, skyspeed2;

        public static double	skytime;

        public static byte[]    r_skysource;

        public static int       r_skymade;
        int r_skydirect;		// not used?

        // TODO: clean up these routines

        static byte[]   bottomsky = new byte[128 * 131];
        static byte[]   bottommask = new byte[128 * 131];
        static byte[]	newsky = new byte[128*256];	// newsky and topsky both pack in here, 128 bytes
							        //  of newsky on the left of each scan, 128 bytes
							        //  of topsky on the right, because the low-level
							        //  drawers need 256-byte scan widths

        /*
        =============
        R_InitSky

        A sky texture is 256*128, with the right side being a masked overlay
        ==============
        */
        public static void R_InitSky (model.texture_t mt)
        {
            int     i, j;
            int     src;

            src = (int)mt.offsets[0];

            for (i = 0; i < 128; i++)
            {
                for (j = 0; j < 128; j++)
                {
                    newsky[(i * 256) + j + 128] = mt.pixels[src+i * 256 + j + 128];
                }
            }

            for (i = 0; i < 128; i++)
            {
                for (j = 0; j < 131; j++)
                {
                    if (mt.pixels[src + i * 256 + (j & 0x7F)] != 0)
                    {
                        bottomsky[(i * 131) + j] = mt.pixels[src + i * 256 + (j & 0x7F)];
                        bottommask[(i * 131) + j] = 0;
                    }
                    else
                    {
                        bottomsky[(i * 131) + j] = 0;
                        bottommask[(i * 131) + j] = 0xff;
                    }
                }
            }

            r_skysource = newsky;
        }

        /*
        =================
        R_MakeSky
        =================
        */
        static int xlast = -1, ylast = -1;
        public static void R_MakeSky()
        {
	        int			x, y;
	        int			ofs, baseofs;
	        int			xshift, yshift;
	        int	        pnewsky;

	        xshift = (int)(skytime * skyspeed);
            yshift = (int)(skytime * skyspeed);

	        if ((xshift == xlast) && (yshift == ylast))
		        return;

	        xlast = xshift;
	        ylast = yshift;
        	
	        pnewsky = 0;

	        for (y=0 ; y<draw.SKYSIZE ; y++)
	        {
                baseofs = ((y + yshift) & draw.SKYMASK) * 131;

		        for (x=0 ; x<draw.SKYSIZE ; x++)
		        {
                    ofs = baseofs + ((x + xshift) & draw.SKYMASK);

                    newsky[pnewsky] = (byte)((newsky[pnewsky + 128] &
                            bottommask[ofs]) |
                            bottomsky[ofs]);
			        pnewsky++;
		        }

		        pnewsky += 128;
	        }

	        r_skymade = 1;
        }

        /*
        =============
        R_SetSkyFrame
        ==============
        */
        static void R_SetSkyFrame ()
        {
	        int		g, s1, s2;
	        double	temp;

	        skyspeed = iskyspeed;
	        skyspeed2 = iskyspeed2;

	        g = mathlib.GreatestCommonDivisor (iskyspeed, iskyspeed2);
	        s1 = iskyspeed / g;
	        s2 = iskyspeed2 / g;
	        temp = draw.SKYSIZE * s1 * s2;

	        skytime = client.cl.time - ((int)(client.cl.time / temp) * temp);

	        r_skymade = 0;
        }
    }
}

