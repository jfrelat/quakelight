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
// wad.h
// wad.c

namespace quake
{
    public sealed class wad
    {
        //===============
        //   TYPES
        //===============

        const int	CMP_NONE        = 0;
        const int	CMP_LZSS        = 1;

        const int	TYP_NONE		= 0;
        const int	TYP_LABEL		= 1;

        const int	TYP_LUMPY		= 64;				// 64 + grab command number
        const int	TYP_PALETTE		= 64;
        const int	TYP_QTEX		= 65;
        const int	TYP_QPIC		= 66;
        const int	TYP_SOUND		= 67;
        const int	TYP_MIPTEX		= 68;

        public class qpic_t
        {
	        public int	    width, height;
	        public byte[]	data;			// variably sized

            public static implicit operator qpic_t(byte[] buf)
            {
                int ofs = 0;
                qpic_t qpic = new qpic_t();
                qpic.width = common.parseInt(buf, ref ofs);
                qpic.height = common.parseInt(buf, ref ofs);
                qpic.data = new byte[buf.Length - ofs];
                Buffer.BlockCopy(buf, ofs, qpic.data, 0, buf.Length - ofs);
                return qpic;
            }
        };
        
        class wadinfo_t
        {
	        public string	identification;		// should be WAD2 or 2DAW
            public int      numlumps;
            public int      infotableofs;
        };
        const int sizeof_wadinfo_t = 4 * 1 + 1 * 4 + 1 * 4;

        class lumpinfo_t
        {
	        public int		filepos;
            public int      disksize;
            public int      size;					// uncompressed
            public char     type;
            public char     compression;
            public char     pad1, pad2;
            public string   name;				// must be null terminated
        };

        static int	        wad_numlumps;
        static lumpinfo_t[] wad_lumps;
        static byte[]	    wad_base;

        /*
        ==================
        W_CleanupName

        Lowercases name and pads with spaces and a terminating 0 to the length of
        lumpinfo_t->name.
        Used so lumpname lookups can proceed rapidly by comparing 4 chars at a time
        Space padding is so names can be printed nicely in tables.
        Can safely be performed in place.
        ==================
        */
        static void W_CleanupName (ref string _str)
        {
            _str = _str.ToLower();
        }

        /*
        ====================
        W_LoadWadFile
        ====================
        */
        public static void W_LoadWadFile (string filename)
        {
            lumpinfo_t  lump_p;
            wadinfo_t   header = new wadinfo_t();
            uint        i;
            int         infotableofs;

            int ofs;
            int kk;

            wad_base = common.COM_LoadHunkFile(filename);
            if (wad_base == null)
                sys_linux.Sys_Error("W_LoadWadFile: couldn't load " + filename);

            ofs = 0;
            header.identification = common.parseString(wad_base, ref ofs, 4);
            header.numlumps = common.parseInt(wad_base, ref ofs);
            header.infotableofs = common.parseInt(wad_base, ref ofs);

            if (header.identification != "WAD2")
                sys_linux.Sys_Error("Wad file " + filename + "doesn't have WAD2 id");

            wad_numlumps = header.numlumps;
            infotableofs = header.infotableofs;

            wad_lumps = new lumpinfo_t[wad_numlumps];
            ofs = infotableofs;
            for (kk = 0; kk < wad_numlumps; kk++)
            {
                wad_lumps[kk] = new lumpinfo_t();
                wad_lumps[kk].filepos = common.parseInt(wad_base, ref ofs);
                wad_lumps[kk].disksize = common.parseInt(wad_base, ref ofs);
                wad_lumps[kk].size = common.parseInt(wad_base, ref ofs);
                wad_lumps[kk].type = common.parseChar(wad_base, ref ofs);
                wad_lumps[kk].compression = common.parseChar(wad_base, ref ofs);
                wad_lumps[kk].pad1 = common.parseChar(wad_base, ref ofs);
                wad_lumps[kk].pad2 = common.parseChar(wad_base, ref ofs);
                wad_lumps[kk].name = common.parseString(wad_base, ref ofs, 16);
            }

            for (i = 0; i < wad_numlumps; i++)
            {
                lump_p = wad_lumps[i];
                W_CleanupName(ref lump_p.name);
            }
        }

        /*
        =============
        W_GetLumpinfo
        =============
        */
        static lumpinfo_t W_GetLumpinfo (string name)
        {
	        int		        i;
	        lumpinfo_t      lump_p;
	        string	        clean;

            clean = name;
	        W_CleanupName (ref clean);
        	
	        for (i=0 ; i<wad_numlumps ; i++)
	        {
                lump_p = wad_lumps[i];
		        if (clean.CompareTo(lump_p.name) == 0)
			        return lump_p;
	        }
        	
	        sys_linux.Sys_Error ("W_GetLumpinfo: " + name + " not found");
	        return null;
        }

        public static byte[] W_GetLumpName (string name)
        {
            lumpinfo_t lump;

            lump = W_GetLumpinfo(name);
            byte[] buf = new byte[lump.size];
            for(int kk = 0; kk < lump.size; kk++)
                buf[kk] = wad_base[lump.filepos + kk];
            return buf;
        }

        byte[] W_GetLumpNum(int num, ref int offset)
        {
            lumpinfo_t lump;

            if (num < 0 || num > wad_numlumps)
                sys_linux.Sys_Error("W_GetLumpNum: bad number: " + num);

            lump = wad_lumps[num];

            byte[] buf = new byte[lump.size];
            for(int kk = 0; kk < lump.size; kk++)
                buf[kk] = wad_base[lump.filepos + kk];
            return buf;
        }
    }
}