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
// d_local.h:  private rasterization driver defs

namespace quake
{
    public partial class draw
    {
        //
        // TODO: fine-tune this; it's based on providing some overage even if there
        // is a 2k-wide scan, with subdivision every 8, for 256 spans of 12 bytes each
        //
        public const int SCANBUFFERPAD		= 0x1000;

        public const int R_SKY_SMASK	= 0x007F0000;
        public const int R_SKY_TMASK	= 0x007F0000;

        public const int DS_SPAN_LIST_END	= -128;

        public const int SURFCACHE_SIZE_AT_320X200	= 600*1024;

        public class surfcache_t
        {
	        public surfcache_t	    next;
            public surfcache_t      owner;		// NULL is an empty chunk of memory
            public int[]            lightadj = new int[bspfile.MAXLIGHTMAPS]; // checked for strobe flush
            public int              dlight;
            public int              size;		// including header
            public uint             width;
            public uint             height;		// DEBUG only needed for debug
            public double           mipscale;
            public model.texture_t	texture;	// checked for animating textures
            public byte[]           data;	// width*height elements
            public int              ofs;
        };

        // !!! if this is changed, it must be changed in asm_draw.h too !!!
        public class sspan_t
        {
	        public int				u, v, count;
        };
    }
}
