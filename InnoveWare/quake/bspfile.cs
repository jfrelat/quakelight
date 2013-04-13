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

namespace quake
{
    public partial class bspfile
    {
        // upper design bounds

        public const int	MAX_MAP_HULLS		= 4;

        public const int	MAX_MAP_MODELS		= 256;
        public const int	MAX_MAP_BRUSHES		= 4096;
        public const int	MAX_MAP_ENTITIES	= 1024;
        public const int	MAX_MAP_ENTSTRING	= 65536;
        public const int	MAX_MAP_PLANES		= 32767;
        public const int	MAX_MAP_NODES		= 32767;	// because negative shorts are contents
        public const int	MAX_MAP_CLIPNODES	= 32767;		//
        public const int	MAX_MAP_LEAFS		= 8192;
        public const int	MAX_MAP_VERTS		= 65535;
        public const int	MAX_MAP_FACES		= 65535;
        public const int	MAX_MAP_MARKSURFACES = 65535;
        public const int	MAX_MAP_TEXINFO		= 4096;
        public const int	MAX_MAP_EDGES		= 256000;
        public const int	MAX_MAP_SURFEDGES	= 512000;
        public const int	MAX_MAP_TEXTURES	= 512;
        public const int	MAX_MAP_MIPTEX		= 0x200000;
        public const int	MAX_MAP_LIGHTING	= 0x100000;
        public const int	MAX_MAP_VISIBILITY	= 0x100000;

        public const int	MAX_MAP_PORTALS		= 65536;

        // key / value pair sizes

        public const int	MAX_KEY		= 32;
        public const int	MAX_VALUE	= 1024;

        //=============================================================================


        public const int    BSPVERSION	= 29;
        public const int	TOOLVERSION	= 2;

        public class lump_t
        {
	        public int		fileofs, filelen;
        };

        public const int	LUMP_ENTITIES	= 0;
        public const int	LUMP_PLANES		= 1;
        public const int	LUMP_TEXTURES	= 2;
        public const int	LUMP_VERTEXES	= 3;
        public const int	LUMP_VISIBILITY	= 4;
        public const int	LUMP_NODES		= 5;
        public const int	LUMP_TEXINFO	= 6;
        public const int	LUMP_FACES		= 7;
        public const int	LUMP_LIGHTING	= 8;
        public const int	LUMP_CLIPNODES	= 9;
        public const int	LUMP_LEAFS		= 10;
        public const int	LUMP_MARKSURFACES = 11;
        public const int	LUMP_EDGES		= 12;
        public const int	LUMP_SURFEDGES	= 13;
        public const int	LUMP_MODELS		= 14;

        public const int	HEADER_LUMPS	= 15;

        public class ByteBuffer
        {
            public byte[]   buffer;
            public int      ofs;

            public ByteBuffer(byte[] buffer, int ofs)
            {
                this.buffer = buffer;
                this.ofs = ofs;
            }
        }

        public class dmodel_t
        {
	        public double[]	mins = new double[3], maxs = new double[3];
	        double[]	origin = new double[3];
	        public int[]	headnode = new int[MAX_MAP_HULLS];
	        public int		visleafs;		// not including the solid leaf 0
	        public int		firstface, numfaces;

            public static implicit operator dmodel_t(ByteBuffer buf)
            {
                int i;
                int ofs = buf.ofs;
                dmodel_t dmodel = new dmodel_t();
                for (i = 0; i < 3; i++)
                {
                    dmodel.mins[i] = BitConverter.ToSingle(buf.buffer, ofs);
                    ofs += sizeof(Single);
                }
                for (i = 0; i < 3; i++)
                {
                    dmodel.maxs[i] = BitConverter.ToSingle(buf.buffer, ofs);
                    ofs += sizeof(Single);
                }
                for (i = 0; i < 3; i++)
                {
                    dmodel.origin[i] = BitConverter.ToSingle(buf.buffer, ofs);
                    ofs += sizeof(Single);
                }
                for (i = 0; i < MAX_MAP_HULLS; i++)
                {
                    dmodel.headnode[i] = common.parseInt(buf.buffer, ref ofs);
                }
                dmodel.visleafs = common.parseInt(buf.buffer, ref ofs);
                dmodel.firstface = common.parseInt(buf.buffer, ref ofs);
                dmodel.numfaces = common.parseInt(buf.buffer, ref ofs);
                return dmodel;
            }
        };
        public const int sizeof_dmodel_t = 9 * sizeof(Single) + MAX_MAP_HULLS * sizeof(int) + 3 * sizeof(int);

        public class dheader_t
        {
	        public int      version;	
	        public lump_t[]	lumps = new lump_t[HEADER_LUMPS];

            public dheader_t()
            {
                for (int kk = 0; kk < HEADER_LUMPS; kk++)
                    lumps[kk] = new lump_t();
            }

            public static implicit operator dheader_t(byte[] buf)
            {
                int ofs = 0;
                dheader_t dheader = new dheader_t();
                dheader.version = common.parseInt(buf, ref ofs);
                for (int i = 0; i < HEADER_LUMPS; i++)
                {
                    dheader.lumps[i].fileofs = common.parseInt(buf, ref ofs);
                    dheader.lumps[i].filelen = common.parseInt(buf, ref ofs);
                }
                return dheader;
            }
        };

        public class dmiptexlump_t
        {
	        public int		nummiptex;
            public int[]    dataofs;		// [nummiptex]

            public static implicit operator dmiptexlump_t(ByteBuffer buf)
            {
                int i;
                int ofs = buf.ofs;
                dmiptexlump_t dmiptexlump = new dmiptexlump_t();
                dmiptexlump.nummiptex = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                dmiptexlump.dataofs = new int[dmiptexlump.nummiptex];
                for (i = 0; i < dmiptexlump.nummiptex; i++)
                {
                    dmiptexlump.dataofs[i] = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                }
                return dmiptexlump;
            }
        };

        public const int	MIPLEVELS	= 4;
        public class miptex_t
        {
	        public string	name = new string(new char[16]);
            public uint     width, height;
            public uint[]   offsets = new uint[MIPLEVELS];		// four mip maps stored

            public static implicit operator miptex_t(ByteBuffer buf)
            {
                int i;
                int ofs = buf.ofs;
                miptex_t miptex = new miptex_t();
                miptex.name = common.parseString(buf.buffer, ref ofs, 16);
                miptex.width = BitConverter.ToUInt32(buf.buffer, ofs); ofs += sizeof(uint);
                miptex.height = BitConverter.ToUInt32(buf.buffer, ofs); ofs += sizeof(uint);
                for (i = 0; i < MIPLEVELS; i++)
                {
                    miptex.offsets[i] = BitConverter.ToUInt32(buf.buffer, ofs); ofs += sizeof(uint);
                }
                return miptex;
            }
        };
        public const int sizeof_miptex_t = 16 + 2 * sizeof(uint) + MIPLEVELS * sizeof(uint);

        public class dvertex_t
        {
	        public double[]	point = new double[3];

            public static implicit operator dvertex_t(ByteBuffer buf)
            {
                int i;
                int ofs = buf.ofs;
                dvertex_t dvertex = new dvertex_t();
                for (i = 0; i < 3; i++)
                {
                    dvertex.point[i] = BitConverter.ToSingle(buf.buffer, ofs);
                    ofs += sizeof(Single);
                }
                return dvertex;
            }
        };
        public const int sizeof_dvertex_t = 3 * sizeof(Single);
        
        // 0-2 are axial planes
        public const int	PLANE_X			= 0;
        public const int	PLANE_Y			= 1;
        public const int	PLANE_Z			= 2;

        // 3-5 are non-axial planes snapped to the nearest
        public const int	PLANE_ANYX		= 3;
        public const int	PLANE_ANYY		= 4;
        public const int	PLANE_ANYZ		= 5;

        public class dplane_t
        {
	        public double[]	normal = new double[3];
            public double   dist;
            public int      type;		// PLANE_X - PLANE_ANYZ ?remove? trivial to regenerate

            public static implicit operator dplane_t(ByteBuffer buf)
            {
                int i;
                int ofs = buf.ofs;
                dplane_t dplane = new dplane_t();
                for (i = 0; i < 3; i++)
                {
                    dplane.normal[i] = BitConverter.ToSingle(buf.buffer, ofs);
                    ofs += sizeof(Single);
                }
                dplane.dist = BitConverter.ToSingle(buf.buffer, ofs);
                ofs += sizeof(Single);
                dplane.type = common.parseInt(buf.buffer, ref ofs);
                return dplane;
            }
        };
        public const int sizeof_dplane_t = 4 * sizeof(Single) + sizeof(int);
        
        public const int	CONTENTS_EMPTY		= -1;
        public const int	CONTENTS_SOLID		= -2;
        public const int	CONTENTS_WATER		= -3;
        public const int	CONTENTS_SLIME		= -4;
        public const int	CONTENTS_LAVA		= -5;
        public const int	CONTENTS_SKY		= -6;
        public const int	CONTENTS_ORIGIN		= -7;		// removed at csg time
        public const int	CONTENTS_CLIP		= -8;		// changed to contents_solid

        public const int	CONTENTS_CURRENT_0		= -9;
        public const int	CONTENTS_CURRENT_90		= -10;
        public const int	CONTENTS_CURRENT_180	= -11;
        public const int	CONTENTS_CURRENT_270	= -12;
        public const int	CONTENTS_CURRENT_UP		= -13;
        public const int	CONTENTS_CURRENT_DOWN	= -14;
        
        // !!! if this is changed, it must be changed in asm_i386.h too !!!
        public class dnode_t
        {
            public int      planenum;
            public short[]  children = new short[2];	// negative numbers are -(leafs+1), not nodes
	        public short[]	mins = new short[3];		// for sphere culling
            public short[]  maxs = new short[3];
            public ushort   firstface;
            public ushort   numfaces;	// counting both sides

            public static implicit operator dnode_t(ByteBuffer buf)
            {
                int i;
                int ofs = buf.ofs;
                dnode_t dnode = new dnode_t();
                dnode.planenum = common.parseInt(buf.buffer, ref ofs);
                for (i = 0; i < 2; i++)
                {
                    dnode.children[i] = BitConverter.ToInt16(buf.buffer, ofs);
                    ofs += sizeof(short);
                }
                for (i = 0; i < 3; i++)
                {
                    dnode.mins[i] = BitConverter.ToInt16(buf.buffer, ofs);
                    ofs += sizeof(short);
                }
                for (i = 0; i < 3; i++)
                {
                    dnode.maxs[i] = BitConverter.ToInt16(buf.buffer, ofs);
                    ofs += sizeof(short);
                }
                dnode.firstface = BitConverter.ToUInt16(buf.buffer, ofs);
                ofs += sizeof(short);
                dnode.numfaces = BitConverter.ToUInt16(buf.buffer, ofs);
                ofs += sizeof(short);
                return dnode;
            }
        };
        public const int sizeof_dnode_t = sizeof(int) + 8 * sizeof(short) + 2 * sizeof(ushort);

        public class dclipnode_t
        {
	        int			planenum;
	        short[]		children = new short[2];	// negative numbers are contents
        };
        
        public class texinfo_t
        {
	        public double[][]	vecs = { new double[4], new double[4] };		// [s/t][xyz offset]
            public int          miptex;
            public int          flags;

            public static implicit operator texinfo_t(ByteBuffer buf)
            {
                int i,j;
                int ofs = buf.ofs;
                texinfo_t texinfo = new texinfo_t();
                for(j = 0; j < 2; j++)
                    for (i = 0; i < 4; i++)
                    {
                        texinfo.vecs[j][i] = BitConverter.ToSingle(buf.buffer, ofs); ofs += sizeof(Single);
                    }
                texinfo.miptex = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                texinfo.flags = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                return texinfo;
            }
        };
        public const int sizeof_texinfo_t = 2 * 4 * sizeof(float) + 2 * sizeof(int);
        public const int	TEX_SPECIAL		= 1;		// sky or slime, no lightmap or 256 subdivision

        // note that edge 0 is never used, because negative edge nums are used for
        // counterclockwise use of the edge in a face
        public class dedge_t
        {
	        public ushort[]	v = new ushort[2];		// vertex numbers

            public static implicit operator dedge_t(ByteBuffer buf)
            {
                int i;
                int ofs = buf.ofs;
                dedge_t dedge = new dedge_t();
                dedge.v[0] = BitConverter.ToUInt16(buf.buffer, ofs); ofs += sizeof(ushort);
                dedge.v[1] = BitConverter.ToUInt16(buf.buffer, ofs); ofs += sizeof(ushort);
                return dedge;
            }
        };
        public const int sizeof_dedge_t = 2 * sizeof(ushort);

        public const int	MAXLIGHTMAPS	= 4;
        public class dface_t
        {
	        public short	planenum;
            public short    side;

            public int      firstedge;		// we must support > 64k edges
            public short    numedges;
            public short    texinfo;

        // lighting info
            public byte[]   styles = new byte[MAXLIGHTMAPS];
            public int      lightofs;		// start of [numstyles*surfsize] samples

            public static implicit operator dface_t(ByteBuffer buf)
            {
                int i;
                int ofs = buf.ofs;
                dface_t dface = new dface_t();
                dface.planenum = BitConverter.ToInt16(buf.buffer, ofs); ofs += sizeof(short);
                dface.side = BitConverter.ToInt16(buf.buffer, ofs); ofs += sizeof(short);
                dface.firstedge = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                dface.numedges = BitConverter.ToInt16(buf.buffer, ofs); ofs += sizeof(short);
                dface.texinfo = BitConverter.ToInt16(buf.buffer, ofs); ofs += sizeof(short);
                for (i = 0; i < MAXLIGHTMAPS; i++) dface.styles[i] = buf.buffer[ofs++];
                dface.lightofs = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                return dface;
            }
        };
        public const int sizeof_dface_t = 2 * sizeof(short) + sizeof(int) + 2 * sizeof(short) + MAXLIGHTMAPS * sizeof(byte) + sizeof(int);

        public const int	AMBIENT_WATER	= 0;
        public const int	AMBIENT_SKY		= 1;
        public const int	AMBIENT_SLIME	= 2;
        public const int	AMBIENT_LAVA	= 3;

        public const int	NUM_AMBIENTS			= 4;		// automatic ambient sounds

        // leaf 0 is the generic CONTENTS_SOLID leaf, used for all solid areas
        // all other leafs need visibility info
        public class dleaf_t
        {
            public int      contents;
            public int      visofs;				// -1 = no visibility info

	        public short[]	mins = new short[3];			// for frustum culling
            public short[]  maxs = new short[3];

            public ushort   firstmarksurface;
            public ushort   nummarksurfaces;

            public byte[]   ambient_level = new byte[NUM_AMBIENTS];

            public static implicit operator dleaf_t(ByteBuffer buf)
            {
                int i;
                int ofs = buf.ofs;
                dleaf_t dleaf = new dleaf_t();
                dleaf.contents = common.parseInt(buf.buffer, ref ofs);
                dleaf.visofs = common.parseInt(buf.buffer, ref ofs);
                for (i = 0; i < 3; i++)
                {
                    dleaf.mins[i] = BitConverter.ToInt16(buf.buffer, ofs);
                    ofs += sizeof(short);
                }
                for (i = 0; i < 3; i++)
                {
                    dleaf.maxs[i] = BitConverter.ToInt16(buf.buffer, ofs);
                    ofs += sizeof(short);
                }
                dleaf.firstmarksurface = BitConverter.ToUInt16(buf.buffer, ofs);
                ofs += sizeof(ushort);
                dleaf.nummarksurfaces = BitConverter.ToUInt16(buf.buffer, ofs);
                ofs += sizeof(ushort);
                for (i = 0; i < NUM_AMBIENTS; i++)
                    dleaf.ambient_level[i] = buf.buffer[ofs++];
                return dleaf;
            }
        };
        public const int sizeof_dleaf_t = 2 * sizeof(int) + 6 * sizeof(short) + 2 * sizeof(ushort) + NUM_AMBIENTS * sizeof(byte);
        
        //============================================================================
    }
}