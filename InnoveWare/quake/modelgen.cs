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
// modelgen.h: header file for model generation program
//

// *********************************************************
// * This file must be identical in the modelgen directory *
// * and in the Quake directory, because it's used to      *
// * pass data from one to the other via model files.      *
// *********************************************************

namespace quake
{
    public partial class model
    {
        public const int ALIAS_VERSION	= 6;

        public const int ALIAS_ONSEAM				= 0x0020;

        // must match definition in spritegn.h
        public enum synctype_t {ST_SYNC=0, ST_RAND };

        public enum aliasframetype_t { ALIAS_SINGLE=0, ALIAS_GROUP };

        public enum aliasskintype_t { ALIAS_SKIN_SINGLE=0, ALIAS_SKIN_GROUP };

        public class mdl_t {
	        public int			ident;
	        public int		    version;
            public double[]     scale = new double[3];
            public double[]     scale_origin = new double[3];
            public double       boundingradius;
            public double[]     eyeposition = new double[3];
            public int          numskins;
            public int          skinwidth;
            public int          skinheight;
            public int          numverts;
            public int          numtris;
            public int          numframes;
            public synctype_t   synctype;
            public int          flags;
            public double       size;

            public static implicit operator mdl_t(byte[] buf)
            {
                int kk;
                int ofs = 0;
                mdl_t mdl = new mdl_t();
                mdl.ident = common.parseInt(buf, ref ofs);
                mdl.version = common.parseInt(buf, ref ofs);
                for (kk = 0; kk < 3; kk++)
                {
                    mdl.scale[kk] = BitConverter.ToSingle(buf, ofs);
                    ofs += sizeof(Single);
                }
                for (kk = 0; kk < 3; kk++)
                {
                    mdl.scale_origin[kk] = BitConverter.ToSingle(buf, ofs);
                    ofs += sizeof(Single);
                }
                mdl.boundingradius = BitConverter.ToSingle(buf, ofs);
                ofs += sizeof(Single);
                for (kk = 0; kk < 3; kk++)
                {
                    mdl.eyeposition[kk] = BitConverter.ToSingle(buf, ofs);
                    ofs += sizeof(Single);
                }
                mdl.numskins = common.parseInt(buf, ref ofs);
                mdl.skinwidth = common.parseInt(buf, ref ofs);
                mdl.skinheight = common.parseInt(buf, ref ofs);
                mdl.numverts = common.parseInt(buf, ref ofs);
                mdl.numtris = common.parseInt(buf, ref ofs);
                mdl.numframes = common.parseInt(buf, ref ofs);
                mdl.synctype = (synctype_t)common.parseInt(buf, ref ofs);
                mdl.flags = common.parseInt(buf, ref ofs);
                mdl.size = BitConverter.ToSingle(buf, ofs);
                ofs += sizeof(Single);
                return mdl;
            }
        };
        public const int sizeof_mdl_t = 2 * sizeof(int) + 10 * sizeof(float) + 8 * sizeof(int) + sizeof(float);

        // TODO: could be shorts

        public class stvert_t {
	        public int		onseam;
            public int      s;
            public int      t;

            public static implicit operator stvert_t(bspfile.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                stvert_t stvert = new stvert_t();
                stvert.onseam = common.parseInt(buf.buffer, ref ofs);
                stvert.s = common.parseInt(buf.buffer, ref ofs);
                stvert.t = common.parseInt(buf.buffer, ref ofs);
                return stvert;
            }
        };
        public const int sizeof_stvert_t = 3 * sizeof(int);

        public class dtriangle_t {
	        public int					facesfront;
	        public int[]				vertindex = new int[3];

            public static implicit operator dtriangle_t(bspfile.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                dtriangle_t dtriangle = new dtriangle_t();
                dtriangle.facesfront = common.parseInt(buf.buffer, ref ofs);
                for(int i = 0; i < 3; i++)
                    dtriangle.vertindex[i] = common.parseInt(buf.buffer, ref ofs);
                return dtriangle;
            }
        };
        public const int sizeof_dtriangle_t = 4 * sizeof(int);

        public const int DT_FACES_FRONT				= 0x0010;

        // This mirrors trivert_t in trilib.h, is present so Quake knows how to
        // load this data

        public class trivertx_t {
	        public byte[]	v = new byte[3];
	        public byte	    lightnormalindex;

            public static implicit operator trivertx_t(bspfile.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                trivertx_t trivertx = new trivertx_t();
                for(int i = 0; i < 3; i++)
                    trivertx.v[i] = buf.buffer[ofs++];
                trivertx.lightnormalindex = buf.buffer[ofs];
                return trivertx;
            }
        };
        public const int sizeof_trivertx_t = 4 * sizeof(byte);

        public class daliasframe_t {
	        public trivertx_t	bboxmin = new trivertx_t();	// lightnormal isn't used
	        public trivertx_t	bboxmax = new trivertx_t();	// lightnormal isn't used
	        public string		name = new string(new char[16]);	// frame name from grabbing

            public static implicit operator daliasframe_t(bspfile.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                daliasframe_t daliasframe = new daliasframe_t();
                daliasframe.bboxmin = (trivertx_t)buf; buf.ofs += sizeof_trivertx_t;
                daliasframe.bboxmax = (trivertx_t)buf; buf.ofs += sizeof_trivertx_t;
                daliasframe.name = common.parseString(buf.buffer, ref buf.ofs, 16);
                buf.ofs = ofs;
                return daliasframe;
            }
        };
        public const int sizeof_daliasframe_t = 2 * sizeof_trivertx_t + 16;

        public class daliasgroup_t {
	        public int			numframes;
	        public trivertx_t	bboxmin = new trivertx_t();	// lightnormal isn't used
	        public trivertx_t	bboxmax = new trivertx_t();	// lightnormal isn't used

            public static implicit operator daliasgroup_t(bspfile.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                daliasgroup_t daliasgroup = new daliasgroup_t();
                daliasgroup.numframes = common.parseInt(buf.buffer, ref buf.ofs);
                daliasgroup.bboxmin = (trivertx_t)buf; buf.ofs += sizeof_trivertx_t;
                daliasgroup.bboxmax = (trivertx_t)buf; buf.ofs += sizeof_trivertx_t;
                buf.ofs = ofs;
                return daliasgroup;
            }
        };
        public const int sizeof_daliasgroup_t = sizeof(int) + 2 * sizeof_trivertx_t;

        public class daliasskingroup_t {
	        public int			numskins;

            public static implicit operator daliasskingroup_t(bspfile.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                daliasskingroup_t daliasskingroup = new daliasskingroup_t();
                daliasskingroup.numskins = common.parseInt(buf.buffer, ref ofs);
                return daliasskingroup;
            }
        };
        public const int sizeof_daliasskingroup_t = sizeof(int);

        public class daliasframetype_t {
	        public aliasframetype_t	type;

            public static implicit operator daliasframetype_t(bspfile.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                daliasframetype_t daliasframetype = new daliasframetype_t();
                daliasframetype.type = (aliasframetype_t)common.parseInt(buf.buffer, ref ofs);
                return daliasframetype;
            }
        };
        public const int sizeof_daliasframetype_t = sizeof(aliasframetype_t);

        public class daliasskintype_t {
	        public aliasskintype_t	type;

            public static implicit operator daliasskintype_t(bspfile.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                daliasskintype_t daliasskintype = new daliasskintype_t();
                daliasskintype.type = (aliasskintype_t)common.parseInt(buf.buffer, ref ofs);
                return daliasskintype;
            }
        };
        public const int sizeof_daliasskintype_t = sizeof(aliasskintype_t);

        public const int IDPOLYHEADER = (('O' << 24) + ('P' << 16) + ('D' << 8) + 'I');
														        // little-endian "IDPO"
    }
}
