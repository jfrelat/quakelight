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

// r_shared.h: general refresh-related stuff shared between the refresh and the
// driver

// FIXME: clean up and move into d_iface.h

namespace quake
{
    public partial class render
    {
        public const int	MAXVERTS	    = 16;					// max points in a surface polygon
        public const int    MAXWORKINGVERTS	= (MAXVERTS+4);	// max points in an intermediate
										        //  polygon (while processing)
        // !!! if this is changed, it must be changed in d_ifacea.h too !!!
        public const int	MAXHEIGHT		= 1024;
        public const int	MAXWIDTH		= 1280;
        public const int    MAXDIMENSION	= ((MAXHEIGHT > MAXWIDTH) ? MAXHEIGHT : MAXWIDTH);

        public const int    SIN_BUFFER_SIZE	= (MAXDIMENSION+draw.CYCLE);

        public const int    INFINITE_DISTANCE	= 0x10000;		// distance that's always guaranteed to
										        //  be farther away than anything in
										        //  the scene

        //===================================================================

        public const int    NUMSTACKEDGES		= 2400;
        public const int	MINEDGES			= NUMSTACKEDGES;
        public const int    NUMSTACKSURFACES	= 800;
        public const int    MINSURFACES			= NUMSTACKSURFACES;
        public const int	MAXSPANS			= 3000;

        // !!! if this is changed, it must be changed in asm_draw.h too !!!
        public class espan_t
        {
	        public int	    u, v, count;
            public espan_t  pnext;
        };

        // FIXME: compress, make a union if that will help
        // insubmodel is only 1, flags is fewer than 32, spanstate could be a byte
        public class surf_t
        {
	        public surf_t	next;			// active surface stack in r_edge.c
            public surf_t   prev;			// used in r_edge.c for active surf stack
	        public espan_t	spans;			// pointer to linked list of spans to draw
            public int      key;				// sorting key (BSP order)
            public int      last_u;				// set during tracing
            public int      spanstate;			// 0 = not in span
									        // 1 = in span
									        // -1 = in inverted span (end before
									        //  start)
            public int      flags;				// currentface flags
	        public object	data;				// associated data like msurface_t
            public entity_t entity;
            public double   nearzi;				// nearest 1/z on surface, for mipmapping
            public bool     insubmodel;
            public double   d_ziorigin, d_zistepu, d_zistepv;

            public int[]    pad = new int[2];				// to 64 bytes
        };

        // flags in finalvert_t.flags
        public const int ALIAS_LEFT_CLIP			= 0x0001;
        public const int ALIAS_TOP_CLIP				= 0x0002;
        public const int ALIAS_RIGHT_CLIP			= 0x0004;
        public const int ALIAS_BOTTOM_CLIP			= 0x0008;
        public const int ALIAS_Z_CLIP				= 0x0010;
        // !!! if this is changed, it must be changed in d_ifacea.h too !!!
        public const int ALIAS_ONSEAM				= 0x0020;	// also defined in modelgen.h;
											        //  must be kept in sync
        public const int ALIAS_XY_CLIP_MASK			= 0x000F;

        // !!! if this is changed, it must be changed in asm_draw.h too !!!
        public class edge_t
        {
	        public int     		    u;
            public int              u_step;
            public edge_t           prev, next;
            public ushort[]         surfs = new ushort[2];
            public edge_t           nextremove;
            public double           nearzi;
            public model.medge_t    owner;
        };
    }
}