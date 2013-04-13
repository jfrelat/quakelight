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
// d_iface.h: interface header file for rasterization driver modules

namespace quake
{
    public partial class draw
    {
        public const int WARP_WIDTH		= 320;
        public const int WARP_HEIGHT	= 200;

        public const int MAX_LBM_HEIGHT	= 480;

        public class emitpoint_t
        {
            public double   u, v;
            public double   s, t;
	        public double	zi;
        };

        public enum ptype_t {
	        pt_static, pt_grav, pt_slowgrav, pt_fire, pt_explode, pt_explode2, pt_blob, pt_blob2
        };

        // !!! if this is changed, it must be changed in d_ifacea.h too !!!
        public class particle_t
        {
        // driver-usable fields
	        public double[]	    org = new double[3];
	        public double	    color;
        // drivers never touch the following fields
	        public particle_t	next;
            public double[]     vel = new double[3];
	        public double		ramp;
	        public double	    die;
	        public ptype_t		type;
        };

        public const double PARTICLE_Z_CLIP	= 8.0;

        public class polyvert_t {
	        double	u, v, zi, s, t;
        };

        public class polydesc_t {
	        int			    numverts;
	        double		    nearzi;
	        //msurface_t	*pcurrentface;
	        polyvert_t[]    pverts;
        };

        // !!! if this is changed, it must be changed in d_ifacea.h too !!!
        public class finalvert_t {
	        public int[]	v = new int[6];		// u, v, s, t, l, 1/z
            public int      flags;
            public double   reserved;

            public static void Copy(finalvert_t src, finalvert_t dst)
            {
                for (int kk = 0; kk < 6; kk++) dst.v[kk] = src.v[kk];
                dst.flags = src.flags;
                dst.reserved = src.reserved;
            }
        };

        // !!! if this is changed, it must be changed in d_ifacea.h too !!!
        public class affinetridesc_t
        {
            public byte[]                   pskin;
            public model.maliasskindesc_t   pskindesc;
            public int                      skinwidth;
            public int                      skinheight;
            public model.mtriangle_t[]      ptriangles;
            public finalvert_t[]            pfinalverts;
            public int                      numtriangles;
	        public bool				        drawtype;
            public int                      seamfixupX16;
        };

        // !!! if this is changed, it must be changed in d_ifacea.h too !!!
        public class screenpart_t {
	        double	u, v, zi, color;
        };

        public class spritedesc_t
        {
            public int                  nump;
	        public emitpoint_t[]	    pverts;	// there's room for an extra element at [nump], 
							        //  if the driver wants to duplicate element [0] at
							        //  element [nump] to avoid dealing with wrapping
	        public model.mspriteframe_t	pspriteframe;
	        public double[]		        vup = new double[3], vright = new double[3], vpn = new double[3];	// in worldspace
	        public double			    nearzi;
        };

        public class zpointdesc_t
        {
	        int		u, v;
	        double	zi;
	        int		color;
        };

        // transparency types for D_DrawRect ()
        public const int DR_SOLID		= 0;
        public const int DR_TRANSPARENT	= 1;

        // !!! must be kept the same as in quakeasm.h !!!
        public const int TRANSPARENT_COLOR	= 0xFF;

        //=======================================================================//

        // callbacks to Quake

        public class drawsurf_t
        {
	        public byte[]		    surfdat;	// destination for generated surface
            public int              surfofs;
	        public int			    rowbytes;	// destination logical width in bytes
            public model.msurface_t surf;		// description for surface to generate
	        public int[]	        lightadj = new int[bspfile.MAXLIGHTMAPS];
							        // adjust for lightmap levels for dynamic lighting
	        public model.texture_t	texture;	// corrected for animating textures
	        public int			    surfmip;	// mipmapped ratio of surface texels / world pixels
            public int              surfwidth;	// in mipmapped texels
            public int              surfheight;	// in mipmapped texels
        };

        // !!! if this is changed, it must be changed in d_ifacea.h too !!!
        public const int TURB_TEX_SIZE	= 64;		// base turbulent texture size

        // !!! if this is changed, it must be changed in d_ifacea.h too !!!
        public const int CYCLE			= 128;		// turbulent cycle size

        public const int TILE_SIZE		= 128;		// size of textures generated by R_GenTiledSurf

        public const int SKYSHIFT		= 7;
        public const int SKYSIZE		= (1 << SKYSHIFT);
        public const int SKYMASK		= (SKYSIZE - 1);
    }
}
