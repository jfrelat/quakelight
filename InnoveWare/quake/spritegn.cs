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
//
// spritegn.h: header file for sprite generation program
//

namespace quake
{
    public partial class model
    {
        // **********************************************************
        // * This file must be identical in the spritegen directory *
        // * and in the Quake directory, because it's used to       *
        // * pass data from one to the other via .spr files.        *
        // **********************************************************

        //-------------------------------------------------------
        // This program generates .spr sprite package files.
        // The format of the files is as follows:
        //
        // dsprite_t file header structure
        // <repeat dsprite_t.numframes times>
        //   <if spritegroup, repeat dspritegroup_t.numframes times>
        //     dspriteframe_t frame header structure
        //     sprite bitmap
        //   <else (single sprite frame)>
        //     dspriteframe_t frame header structure
        //     sprite bitmap
        // <endrepeat>
        //-------------------------------------------------------

        public const int SPRITE_VERSION	= 1;

        // TODO: shorten these?
        public class dsprite_t {
	        int			ident;
	        public int			version;
	        public int			type;
	        double		boundingradius;
	        public int			width;
	        public int			height;
            public int          numframes;
	        public double		beamlength;
            public synctype_t   synctype;

            public static implicit operator dsprite_t(byte[] buf)
            {
                int ofs = 0;
                dsprite_t dsprite = new dsprite_t();
                dsprite.ident = common.parseInt(buf, ref ofs);
                dsprite.version = common.parseInt(buf, ref ofs);
                dsprite.type = common.parseInt(buf, ref ofs);
                dsprite.boundingradius = BitConverter.ToSingle(buf, ofs);
                ofs += sizeof(Single);
                dsprite.width = common.parseInt(buf, ref ofs);
                dsprite.height = common.parseInt(buf, ref ofs);
                dsprite.numframes = common.parseInt(buf, ref ofs);
                dsprite.beamlength = BitConverter.ToSingle(buf, ofs);
                ofs += sizeof(Single);
                dsprite.synctype = (synctype_t)common.parseInt(buf, ref ofs);
                return dsprite;
            }
        };
        public const int sizeof_dsprite_t = 3 * sizeof(int) + sizeof(Single) + 3 * sizeof(int) + sizeof(Single) + sizeof(int);

        public const int SPR_VP_PARALLEL_UPRIGHT	= 0;
        public const int SPR_FACING_UPRIGHT			= 1;
        public const int SPR_VP_PARALLEL			= 2;
        public const int SPR_ORIENTED				= 3;
        public const int SPR_VP_PARALLEL_ORIENTED	= 4;

        public class dspriteframe_t {
	        public int[]	origin = new int[2];
	        public int		width;
            public int      height;

            public static implicit operator dspriteframe_t(bspfile.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                dspriteframe_t dspriteframe = new dspriteframe_t();
                dspriteframe.origin[0] = common.parseInt(buf.buffer, ref ofs);
                dspriteframe.origin[1] = common.parseInt(buf.buffer, ref ofs);
                dspriteframe.width = common.parseInt(buf.buffer, ref ofs);
                dspriteframe.height = common.parseInt(buf.buffer, ref ofs);
                return dspriteframe;
            }
        };
        public const int sizeof_dspriteframe_t = 4 * sizeof(int);

        public class dspritegroup_t {
	        public int		numframes;

            public static implicit operator dspritegroup_t(bspfile.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                dspritegroup_t dspritegroup = new dspritegroup_t();
                dspritegroup.numframes = common.parseInt(buf.buffer, ref ofs);
                return dspritegroup;
            }
        };
        public const int sizeof_dspritegroup_t = sizeof(int);

        public class dspriteinterval_t {
	        double	interval;
        };

        public enum spriteframetype_t { SPR_SINGLE=0, SPR_GROUP };

        public class dspriteframetype_t {
	        public spriteframetype_t	type;

            public static implicit operator dspriteframetype_t(bspfile.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                dspriteframetype_t dspriteframetype = new dspriteframetype_t();
                dspriteframetype.type = (spriteframetype_t)common.parseInt(buf.buffer, ref ofs);
                return dspriteframetype;
            }
        };
        public const int sizeof_dspriteframetype_t = sizeof(spriteframetype_t);

        public const int IDSPRITEHEADER = (('P' << 24) + ('S' << 16) + ('D' << 8) + 'I');
														        // little-endian "IDSP"
    }
}
