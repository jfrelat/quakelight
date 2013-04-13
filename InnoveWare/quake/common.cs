using System;
using System.Globalization;
using System.Windows;
using System.Windows.Resources;

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
// comndef.h  -- general definitions
// common.c -- misc functions used in client and server

namespace quake
{
    public sealed class common
    {
        public class sizebuf_t
        {
	        public bool	    allowoverflow;	// if false, do a Sys_Error
            public bool     overflowed;		// set to true if the buffer size failed
	        public byte[]	data;
            public int      maxsize;
            public int      cursize;

            public static void Copy(sizebuf_t src, sizebuf_t dst)
            {
                dst.allowoverflow = src.allowoverflow;
                dst.overflowed = src.overflowed;
                dst.data = src.data;
                dst.maxsize = src.maxsize;
                dst.cursize = src.cursize;
            }
        };

        const int NUM_SAFE_ARGVS = 7;

        static cvar_t   registered = new cvar_t("registered","0");
        static cvar_t   cmdline = new cvar_t("cmdline", "0", false, true);

        static bool     com_modified;   // set true if using non-id files

        static bool		proghack;

        static bool     static_registered = true;  // only for startup check, then set

        public static bool		msg_suppress_1 = false;

        // if a packfile directory differs from this, it is assumed to be hacked
        const int PAK0_COUNT = 339;
        const int PAK0_CRC = 32981;

        public static string	com_token;
        public static int		com_argc;
        public static string[]  com_argv;

        static string	com_cmdline;

        public static bool		standard_quake = true, rogue, hipnotic;

        // this graphic needs to be in the pak file to use registered features
        static ushort []pop =
        {
         0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x6600,0x0000,0x0000,0x0000,0x6600,0x0000
        ,0x0000,0x0066,0x0000,0x0000,0x0000,0x0000,0x0067,0x0000
        ,0x0000,0x6665,0x0000,0x0000,0x0000,0x0000,0x0065,0x6600
        ,0x0063,0x6561,0x0000,0x0000,0x0000,0x0000,0x0061,0x6563
        ,0x0064,0x6561,0x0000,0x0000,0x0000,0x0000,0x0061,0x6564
        ,0x0064,0x6564,0x0000,0x6469,0x6969,0x6400,0x0064,0x6564
        ,0x0063,0x6568,0x6200,0x0064,0x6864,0x0000,0x6268,0x6563
        ,0x0000,0x6567,0x6963,0x0064,0x6764,0x0063,0x6967,0x6500
        ,0x0000,0x6266,0x6769,0x6a68,0x6768,0x6a69,0x6766,0x6200
        ,0x0000,0x0062,0x6566,0x6666,0x6666,0x6666,0x6562,0x0000
        ,0x0000,0x0000,0x0062,0x6364,0x6664,0x6362,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0062,0x6662,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0061,0x6661,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0000,0x6500,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0000,0x6400,0x0000,0x0000,0x0000
        };

        /*


        All of Quake's data access is through a hierchal file system, but the contents of the file system can be transparently merged from several sources.

        The "base directory" is the path to the directory holding the quake.exe and all game directories.  The sys_* files pass this to host_init in quakeparms_t->basedir.  This can be overridden with the "-basedir" command line parm to allow code debugging in a different directory.  The base directory is
        only used during filesystem initialization.

        The "game directory" is the first tree on the search path and directory that all generated files (savegames, screenshots, demos, config files) will be saved to.  This can be overridden with the "-game" command line parameter.  The game directory can never be changed while quake is executing.  This is a precacution against having a malicious server instruct clients to write files over areas they shouldn't.

        The "cache directory" is only used during development to save network bandwidth, especially over ISDN / T1 lines.  If there is a cache directory
        specified, when a file is found by the normal search path, it will be mirrored
        into the cache directory, then opened there.



        FIXME:
        The file "parms.txt" will be read out of the game directory and appended to the current command line arguments to allow different games to initialize startup parms differently.  This could be used to add a "-sspeed 22050" for the high quality sound edition.  Because they are added at the end, they will not override an explicit setting on the original command line.
        	
        */

        /*
        ============================================================================

				            LIBRARY REPLACEMENT FUNCTIONS

        ============================================================================
        */

        private static NumberFormatInfo _formatNumber = new CultureInfo("en-US").NumberFormat;

        public static double Q_atof(string str)
        {
            try
            {
                return Single.Parse(str, _formatNumber);
            }
            catch (FormatException)
            {
                return 0;
            }
        }
        
        /*
        ==============================================================================

		            MESSAGE IO FUNCTIONS

        Handles byte ordering and avoids alignment errors
        ==============================================================================
        */

        //
        // writing functions
        //

        public static void MSG_WriteChar (sizebuf_t sb, int c)
        {
	        byte[]    buf;
            int       offset;

	        buf = SZ_GetSpace (sb, 1, out offset);
	        buf[offset] = (byte)c;
        }

        public static void MSG_WriteByte (sizebuf_t sb, int c)
        {
	        byte[]    buf;
            int       offset;

	        buf = SZ_GetSpace (sb, 1, out offset);
            buf[offset] = (byte)c;
        }

        public static void MSG_WriteShort (sizebuf_t sb, int c)
        {
            byte[] buf;
            int offset;

            buf = SZ_GetSpace(sb, 2, out offset);
            buf[offset] = (byte)(c & 0xff);
            buf[offset + 1] = (byte)(c >> 8);
        }

        public static void MSG_WriteLong (sizebuf_t sb, int c)
        {
            byte[] buf;
            int offset;

            buf = SZ_GetSpace(sb, 4, out offset);
            buf[offset] = (byte)(c & 0xff);
            buf[offset + 1] = (byte)((c >> 8) & 0xff);
            buf[offset + 2] = (byte)((c >> 16) & 0xff);
            buf[offset + 3] = (byte)(c >> 24);
        }

        public static void MSG_WriteFloat (sizebuf_t sb, double f)
        {
            byte[] dat;

            dat = BitConverter.GetBytes((float)f);
        	
	        SZ_Write (sb, dat, 4);
        }

        public static void MSG_WriteString (sizebuf_t sb, string s)
        {
            byte[]  buf;
            if (s == null)
            {
                buf = new byte[1];
                buf[0] = 0;
                SZ_Write(sb, buf, 1);
            }
            else
            {
                buf = new byte[s.Length + 1];
                for (int kk = 0; kk < s.Length; kk++)
                    buf[kk] = (byte)s[kk];
                SZ_Write(sb, buf, s.Length + 1);
            }
        }

        public static void MSG_WriteCoord (sizebuf_t sb, double f)
        {
	        MSG_WriteShort (sb, (int)(f*8));
        }

        public static void MSG_WriteAngle(sizebuf_t sb, double f)
        {
	        MSG_WriteByte (sb, ((int)f*256/360) & 255);
        }

        //
        // reading functions
        //
        static int          msg_readcount;
        public static bool         msg_badread;

        public static void MSG_BeginReading ()
        {
	        msg_readcount = 0;
	        msg_badread = false;
        }

        // returns -1 and sets msg_badread if no more characters are available
        public static int MSG_ReadChar ()
        {
	        int     c;
        	
	        if (msg_readcount+1 > net.net_message.cursize)
	        {
		        msg_badread = true;
		        return -1;
	        }
        		
	        c = (sbyte)net.net_message.data[msg_readcount];
	        msg_readcount++;
        	
	        return c;
        }

        public static int MSG_ReadByte ()
        {
	        int     c;
        	
	        if (msg_readcount+1 > net.net_message.cursize)
	        {
		        msg_badread = true;
		        return -1;
	        }

            c = (byte)net.net_message.data[msg_readcount];
	        msg_readcount++;
        	
	        return c;
        }

        public static int MSG_ReadShort ()
        {
	        int     c;

            if (msg_readcount + 2 > net.net_message.cursize)
	        {
		        msg_badread = true;
		        return -1;
	        }

            c = (short)(net.net_message.data[msg_readcount]
            + (net.net_message.data[msg_readcount + 1] << 8));
        	
	        msg_readcount += 2;
        	
	        return c;
        }

        public static int MSG_ReadLong ()
        {
	        int     c;

            if (msg_readcount + 4 > net.net_message.cursize)
	        {
		        msg_badread = true;
		        return -1;
	        }

            c = net.net_message.data[msg_readcount]
            + (net.net_message.data[msg_readcount + 1] << 8)
            + (net.net_message.data[msg_readcount + 2] << 16)
            + (net.net_message.data[msg_readcount + 3] << 24);
        	
	        msg_readcount += 4;
        	
	        return c;
        }

        public static double MSG_ReadFloat ()
        {
            double dat;

            dat = BitConverter.ToSingle(net.net_message.data, msg_readcount);
            msg_readcount += 4;

	        return dat;
        }

        public static string MSG_ReadString ()
        {
	        string     @string;
	        int        l,c;
        	
	        l = 0;
            @string = "";
	        do
	        {
		        c = MSG_ReadChar ();
		        if (c == -1 || c == 0)
			        break;
		        @string += (char)c;
		        l++;
	        } while (l < 2048);
        	
	        return @string;
        }

        public static double MSG_ReadCoord ()
        {
            return MSG_ReadShort() * (1.0 / 8);
        }

        public static double MSG_ReadAngle()
        {
            return MSG_ReadChar() * (360.0 / 256);
        }

        //===========================================================================

        public static void SZ_Alloc (sizebuf_t buf, int startsize)
        {
	        if (startsize < 256)
		        startsize = 256;
            buf.data = new byte[startsize];
	        buf.maxsize = startsize;
	        buf.cursize = 0;
        }

        public static void SZ_Free(sizebuf_t buf)
        {
	        buf.cursize = 0;
        }

        public static void SZ_Clear(sizebuf_t buf)
        {
	        buf.cursize = 0;
        }

        static byte[] SZ_GetSpace (sizebuf_t buf, int length, out int offset)
        {
	        byte[]    data;
        	
	        if (buf.cursize + length > buf.maxsize)
	        {
		        if (!buf.allowoverflow)
			        sys_linux.Sys_Error ("SZ_GetSpace: overflow without allowoverflow set");
        		
		        if (length > buf.maxsize)
                    sys_linux.Sys_Error("SZ_GetSpace: " + length + " is > full buffer size");
        			
		        buf.overflowed = true;
		        console.Con_Printf ("SZ_GetSpace: overflow");
		        SZ_Clear (buf); 
	        }

            data = buf.data;
            offset = buf.cursize;
	        buf.cursize += length;
        	
	        return data;
        }

        public static void SZ_Write(sizebuf_t buf, byte[] data, int length)
        {
            int offset;
            byte[] dst = SZ_GetSpace(buf, length, out offset);
            Buffer.BlockCopy(data, 0, dst, offset, length);
        }

        public static void SZ_Write(sizebuf_t buf, byte[] data, int dataofs, int length)
        {
            int offset;
            byte[] dst = SZ_GetSpace(buf, length, out offset);
            Buffer.BlockCopy(data, dataofs, dst, offset, length);
        }

        public static void SZ_Print(sizebuf_t buf, string data)
        {
	        int             len;
        	
	        len = data.Length+1;

        // byte * cast to keep VC++ happy
            if (buf.data[buf.cursize - 1] != 0)
            {
                int offset;
                byte[] dst = SZ_GetSpace(buf, len, out offset);
                for (int kk = 0; kk < data.Length; kk++)
                    dst[offset + kk] = (byte)data[kk]; // no trailing 0
            }
            else
            {
                int offset;
                byte[] dst = SZ_GetSpace(buf, len - 1, out offset);
                for (int kk = 0; kk < data.Length; kk++)
                    dst[offset - 1 + kk] = (byte)data[kk]; // write over trailing 0
            }
        }


        //============================================================================

        /*
        ============
        COM_FileBase
        ============
        */
        public static void COM_FileBase (string @in, ref string @out)
        {
	        int s, s2;
        	
	        s = @in.Length - 1;
        	
	        while (s != 0 && @in[s] != '.')
		        s--;
        	
	        for (s2 = s ; s2 != 0 && @in[s2] != '/' ; s2--)
	        ;
        	
	        if (s-s2 < 2)
		        @out = "?model?";
	        else
	        {
		        s--;
		        @out = @in.Substring(s2+1, s-s2);
	        }
        }

        /*
        ==================
        COM_DefaultExtension
        ==================
        */
        public static void COM_DefaultExtension(ref string path, string extension)
        {
            int src;
            //
            // if path doesn't have a .EXT, append extension
            // (extension should include the .)
            //
            src = path.Length - 1;

            while (path[src] != '/' && src != 0)
            {
                if (path[src] == '.')
                    return;                 // it has an extension
                src--;
            }

            path += extension;
        }

        /*
        ==============
        COM_Parse

        Parse a token out of a string
        ==============
        */
        public static void COM_Parse(char[] data, ref int ofs)
        {
            int c;
            int len;

            len = 0;
            com_token = "";

            if (data == null)
            {
                ofs = -1;
                return;
            }

        // skip whitespace
        skipwhite:
            while ((c = data[ofs]) <= ' ')
            {
                if (c == 0)
                {
                    ofs = -1;
                    return;                    // end of file;
                }
                ofs++;
            }

            // skip // comments
            if (c == '/' && data[ofs + 1] == '/')
            {
                while (data[ofs] != 0 && data[ofs] != '\n')
                    ofs++;
                goto skipwhite;
            }
            
            // handle quoted strings specially
            if (c == '\"')
            {
                ofs++;
                while (true)
                {
                    c = data[ofs++];
                    if (c == '\"' || c == 0)
                        return;
                    com_token += (char)c;
                    len++;
                }
            }

            // parse single characters
            if (c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':')
            {
                com_token += (char)c;
                len++;
                ofs++;
                return;
            }

            // parse a regular word
            do
            {
                com_token += (char)c;
                ofs++;
                len++;
                c = data[ofs];
                if (c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':')
                    break;
            } while (c > 32);
        }

        /*
        ================
        COM_CheckParm

        Returns the position (1 to argc-1) in the program's argument list
        where the given parameter apears, or 0 if not present
        ================
        */
        public static int COM_CheckParm (string parm)
        {
            int i;

            for (i = 1; i < com_argc; i++)
            {
                if (com_argv[i] == null)
                    continue;               // NEXTSTEP sometimes clears appkit vars.
                if (parm.CompareTo(com_argv[i]) == 0)
                    return i;
            }

	        return 0;
        }

        /*
        ================
        COM_CheckRegistered

        Looks for the pop.txt file and verifies it.
        Sets the "registered" cvar.
        Immediately exits out if an alternate game was attempted to be started without
        being registered.
        ================
        */
        static void COM_CheckRegistered ()
        {
	        int             h = -1;
	        ushort[]        check = new ushort[128];
	        int             i;

            byte[] buf;
            int kk;
            int ofs;

	        COM_OpenFile("gfx/pop.lmp", ref h);
	        static_registered = false;

	        if (h == -1)
	        {
/*		        Con_Printf ("Playing shareware version.\n");
		        if (com_modified)
			        Sys_Error ("You must have the registered version to use modified games");*/
		        return;
	        }

            buf = new byte[128 * sizeof(ushort)];
	        sys_linux.Sys_FileRead (h, buf, buf.Length);
            ofs = 0;
            for (kk = 0; kk < 128; kk++)
            {
                check[kk] = parseUshort(buf, ref ofs);
            }
	        COM_CloseFile (h);
        	
	        for (i=0 ; i<128 ; i++)
		        if (pop[i] != check[i])
			        sys_linux.Sys_Error ("Corrupted data file.");
        	
	        cvar_t.Cvar_Set ("cmdline", com_cmdline);
            cvar_t.Cvar_Set("registered", "1");
	        static_registered = true;
	        console.Con_Printf ("Playing registered version.");
        }

        /*
        ================
        COM_Init
        ================
        */
        public static void COM_Init (string basedir)
        {
            cvar_t.Cvar_RegisterVariable (registered);
            cvar_t.Cvar_RegisterVariable (cmdline);
            cmd.Cmd_AddCommand ("path", COM_Path_f);

            COM_InitFilesystem ();
            COM_CheckRegistered ();
        }

        /*
        =============================================================================

        QUAKE FILESYSTEM

        =============================================================================
        */

        public static int     com_filesize;


        //
        // in memory
        //

        class packfile_t
        {
	        public string          name;
	        public int             filepos, filelen;
        };

        class pack_t
        {
	        public string          filename;
	        public int             handle;
	        public int             numfiles;
	        public packfile_t[]    files;
        };

        //
        // on disk
        //
        class dpackfile_t
        {
	        public string          name;
	        public int             filepos, filelen;
        };
        const int sizeof_dpackfile_t = 56 * 1 + 1 * 4 + 1 * 4;

        class dpackheader_t
        {
	        public string   id;
            public int      dirofs;
            public int      dirlen;
        };
        const int sizeof_dpackheader_t = 4 * 1 + 1 * 4 + 1 * 4;

        const int MAX_FILES_IN_PACK = 2048;

        static string    com_cachedir;
        public static string    com_gamedir;

        class searchpath_t
        {
	        public string          filename;
	        public pack_t          pack;          // only one of filename / pack will be used
	        public searchpath_t    next;
        };

        static searchpath_t    com_searchpaths;

        /*
        ============
        COM_Path_f

        ============
        */
        static void COM_Path_f ()
        {
	        searchpath_t    s;
        	
	        console.Con_Printf ("Current search path:\n");
	        for (s=com_searchpaths ; s != null ; s=s.next)
	        {
		        if (s.pack != null)
		        {
                    console.Con_Printf(s.pack.filename + " (" + s.pack.numfiles + " files)\n");
		        }
		        else
                    console.Con_Printf(s.filename + "\n");
	        }
        }

        /*
        ============
        COM_WriteFile

        The filename will be prefixed by the current game directory
        ============
        */
        void COM_WriteFile (string filename, byte[] data, int len)
        {
	        int             handle;
	        string          name;
        	
	        name = com_gamedir + "/" + filename;

            handle = sys_linux.Sys_FileOpenWrite(name);
	        if (handle == -1)
	        {
		        sys_linux.Sys_Printf ("COM_WriteFile: failed on " + name);
		        return;
	        }

            sys_linux.Sys_Printf("COM_WriteFile: " + name);
            sys_linux.Sys_FileWrite(handle, data, len);
            sys_linux.Sys_FileClose(handle);
        }


        /*
        ============
        COM_CreatePath

        Only used for CopyFile
        ============
        */
        void    COM_CreatePath (string path)
        {
        }
        
        /*
        ===========
        COM_CopyFile

        Copies a file over from the net to the local cache, creating any directories
        needed.  This is for the convenience of developers using ISDN from home.
        ===========
        */
        void COM_CopyFile (string netpath, string cachepath)
        {
        }

        /*
        ===========
        COM_FindFile

        Finds the file in the search path.
        Sets com_filesize and one of handle or file
        ===========
        */
        static int COM_FindFile (string filename, ref int handle, ref Helper.helper.FILE file)
        {
	        searchpath_t    search;
	        string          netpath;
	        string          cachepath;
	        pack_t          pak;
	        int             i;
	        int             findtime, cachetime;

/*	        if (file && handle)
		        Sys_Error ("COM_FindFile: both handle and file set");
	        if (!file && !handle)
		        Sys_Error ("COM_FindFile: neither handle or file set");*/
        		
        //
        // search through the path, one element at a time
        //
	        search = com_searchpaths;
	        if (proghack)
	        {	// gross hack to use quake 1 progs with quake 2 maps
		        if (filename.CompareTo("progs.dat") == 0)
			        search = search.next;
	        }

	        for ( ; search != null ; search = search.next)
	        {
	        // is the element a pak file?
		        if (search.pack != null)
		        {
		        // look through all the pak file elements
			        pak = search.pack;
			        for (i=0 ; i<pak.numfiles ; i++)
				        if (pak.files[i].name.CompareTo(filename) == 0)
				        {       // found it!
					        sys_linux.Sys_Printf ("PackFile: " + pak.filename + " : " + filename + "\n");
                            if (handle != -1)
                            {
                                handle = pak.handle;
                                sys_linux.Sys_FileSeek(pak.handle, pak.files[i].filepos);
                            }
                            else
                            {       // open a new file on the pakfile
                                string pf = pak.filename;
                                if (pf.StartsWith("./"))
                                    pf = pf.Substring(2);

                                StreamResourceInfo si = Application.GetResourceStream(new Uri("InnoveWare;component/" + pf, UriKind.Relative));
                                if (si != null)
                                {
                                    file = new Helper.helper.FILE();
                                    file.stream = si.Stream;
                                }
                                //*file = fopen(pak->filename, "rb");
                                if (file != null)
                                    Helper.helper.fseek(file, pak.files[i].filepos, Helper.helper.SEEK_SET);
                            }

					        com_filesize = pak.files[i].filelen;
					        return com_filesize;
				        }
		        }
	        }
        	
	        sys_linux.Sys_Printf ("FindFile: can't find " + filename + "\n");
        	
	        if (handle != -1)
		        handle = -1;
	        else
		        file = null;
	        com_filesize = -1;
	        return -1;
        }

        /*
        ===========
        COM_OpenFile

        filename never has a leading slash, but may contain directory walks
        returns a handle and a length
        it may actually be inside a pak file
        ===========
        */
        public static int COM_OpenFile (string filename, ref int handle)
        {
            Helper.helper.FILE file = null;
            return COM_FindFile(filename, ref handle, ref file);
        }

        /*
        ===========
        COM_FOpenFile

        If the requested file is inside a packfile, a new FILE * will be opened
        into the file.
        ===========
        */
        public static int COM_FOpenFile(string filename, ref Helper.helper.FILE file)
        {
            int handle = -1;
            return COM_FindFile(filename, ref handle, ref file);
        }

        /*
        ============
        COM_CloseFile

        If it is a pak file handle, don't really close it
        ============
        */
        static void COM_CloseFile (int h)
        {
	        searchpath_t    s;
        	
	        for (s = com_searchpaths ; s != null ; s=s.next)
		        if (s.pack != null && s.pack.handle == h)
			        return;
        			
	        sys_linux.Sys_FileClose (h);
        }


        /*
        ============
        COM_LoadFile

        Filename are reletive to the quake directory.
        Allways appends a 0 byte.
        ============
        */
        public static byte[] COM_LoadFile (string path, int usehunk)
        {
	        int     h = 0;
	        byte[]  buf;
	        string  _base;
	        int     len;

	        buf = null;     // quiet compiler warning

        // look for it in the filesystem or pack files
	        len = COM_OpenFile (path, ref h);
	        if (h == -1)
		        return null;
        	
        // extract the filename base name for hunk tag
	        //COM_FileBase (path, base);

        	buf = new byte[len+1];
	        if (buf == null)
		        sys_linux.Sys_Error ("COM_LoadFile: not enough space for " + path);
        		
	        buf[len] = 0;

	        draw.Draw_BeginDisc ();
	        sys_linux.Sys_FileRead (h, buf, len);                     
	        COM_CloseFile (h);
	        draw.Draw_EndDisc ();

	        return buf;
        }

        public static byte[] COM_LoadHunkFile (string path)
        {
	        return COM_LoadFile (path, 1);
        }

        byte[] COM_LoadTempFile (string path)
        {
	        return COM_LoadFile (path, 2);
        }

        // uses temp hunk if larger than bufsize
        public static byte[] COM_LoadStackFile(string path, byte[] buffer, int bufsize)
        {
            byte[] buf;

            //loadbuf = (byte*)buffer;
            //loadsize = bufsize;
            buf = COM_LoadFile(path, 4);

            return buf;
        }

        public static string parseString(byte[] buffer, int offset)
        {
            string str = "";
            for(;;)
            {
                char ch = (char)buffer[offset++];
                if (ch == 0)
                    break;
                str += ch;
            }
            return str;
        }

        public static string parseString(byte[] buffer, ref int offset, int count)
        {
            string str = "";
            for (int pos = 0; pos < count; pos++)
            {
                char ch = (char) buffer[offset + pos];
                if (ch == 0)
                    break;
                str += ch;
            }
            offset += count;
            return str;
        }

        public static ushort parseUshort(byte[] buffer, ref int offset)
        {
            ushort num = (ushort)((buffer[offset + 1] << 8) | buffer[offset]);
            offset += 2;
            return num;
        }

        public static int parseInt(byte[] buffer, ref int offset)
        {
            int num = (buffer[offset + 3] << 24) | (buffer[offset + 2] << 16) | (buffer[offset + 1] << 8) | buffer[offset];
            offset += 4;
            return num;
        }

        public static char parseChar(byte[] buffer, ref int offset)
        {
            char num = (char)buffer[offset];
            offset += 1;
            return num;
        }

        /*
        =================
        COM_LoadPackFile

        Takes an explicit (not game tree related) path to a pak file.

        Loads the header and directory, adding the files at the beginning
        of the list so they override previous pack files.
        =================
        */
        static pack_t COM_LoadPackFile(string packfile)
        {
	        dpackheader_t           header = new dpackheader_t();
	        int                     i;
	        packfile_t[]            newfiles;
	        int                     numpackfiles;
	        pack_t                  pack = null;
	        int                     packhandle = -1;
	        dpackfile_t[]           info = new dpackfile_t[MAX_FILES_IN_PACK];
	        ushort                  crc;

            int     kk;
            byte[]  buf;
            int     ofs;

	        if (sys_linux.Sys_FileOpenRead (packfile, ref packhandle) == -1)
		        return null;
            buf = new byte[sizeof_dpackheader_t];
            sys_linux.Sys_FileRead(packhandle, buf, buf.Length);
            ofs = 0;
            header.id = parseString(buf, ref ofs, 4);
	        if (header.id != "PACK")
		        sys_linux.Sys_Error (packfile + " is not a packfile");
            header.dirofs = parseInt(buf, ref ofs);
            header.dirlen = parseInt(buf, ref ofs);

            numpackfiles = header.dirlen / sizeof_dpackfile_t;

            if (numpackfiles > MAX_FILES_IN_PACK)
                sys_linux.Sys_Error (packfile + " has " + numpackfiles + " files");

            if (numpackfiles != PAK0_COUNT)
                com_modified = true;    // not the original file

            newfiles = new packfile_t[numpackfiles];
            for(kk = 0; kk < numpackfiles; kk++) newfiles[kk] = new packfile_t();

            sys_linux.Sys_FileSeek (packhandle, header.dirofs);
            buf = new byte[header.dirlen];
            sys_linux.Sys_FileRead (packhandle, buf, header.dirlen);
            ofs = 0;
            for (kk = 0; kk < numpackfiles; kk++)
            {
                info[kk] = new dpackfile_t();
                info[kk].name = parseString(buf, ref ofs, 56);
                info[kk].filepos = parseInt(buf, ref ofs);
                info[kk].filelen = parseInt(buf, ref ofs);
            }


        // crc the directory to check for modifications
            /*CRC_Init (&crc);
            for (i=0 ; i<header.dirlen ; i++)
                CRC_ProcessByte (&crc, ((byte *)info)[i]);
            if (crc != PAK0_CRC)
                com_modified = true;*/

        // parse the directory
            for (i=0 ; i<numpackfiles ; i++)
            {
                newfiles[i].name = info[i].name;
                newfiles[i].filepos = info[i].filepos;
                newfiles[i].filelen = info[i].filelen;
            }

            pack = new pack_t();
            pack.filename = packfile;
            pack.handle = packhandle;
            pack.numfiles = numpackfiles;
            pack.files = newfiles;
        	
            /*Con_Printf ("Added packfile %s (%i files)\n", packfile, numpackfiles);*/
	        return pack;
        }


        /*
        ================
        COM_AddGameDirectory

        Sets com_gamedir, adds the directory to the head of the path,
        then loads and adds pak1.pak pak2.pak ... 
        ================
        */
        static void COM_AddGameDirectory (string dir)
        {
	        int             i;
	        searchpath_t    search;
	        pack_t          pak;
	        string          pakfile;

	        com_gamedir = dir;

        //
        // add the directory to the search path
        //
	        search = new searchpath_t();
	        search.filename = dir;
	        search.next = com_searchpaths;
	        com_searchpaths = search;

        //
        // add any pak files in the format pak0.pak pak1.pak, ...
        //
	        for (i=0 ; ; i++)
	        {
		        pakfile = dir + "/pak" + i + ".pak";
		        pak = COM_LoadPackFile (pakfile);
		        if (pak == null)
			        break;
		        search = new searchpath_t();
		        search.pack = pak;
		        search.next = com_searchpaths;
		        com_searchpaths = search;               
	        }

        //
        // add the contents of the parms.txt file to the end of the command line
        //

        }

        /*
        ================
        COM_InitFilesystem
        ================
        */
        static void COM_InitFilesystem ()
        {
	        int             i, j;
	        string          basedir = ".";
	        searchpath_t    search;

        //
        // start up with GAMENAME by default (id1)
        //
	        COM_AddGameDirectory (basedir + "/" + quakedef.GAMENAME);

	        if (COM_CheckParm ("-rogue") != 0)
		        COM_AddGameDirectory (basedir + "/rogue");
            if (COM_CheckParm("-hipnotic") != 0)
		        COM_AddGameDirectory (basedir + "/hipnotic");

	        if (COM_CheckParm ("-proghack") != 0)
		        proghack = true;
        }
   }
}