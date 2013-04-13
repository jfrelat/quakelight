using System;
using System.Globalization;

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

// draw.h -- these are the only functions outside the refresh allowed
// to touch the vid buffer
// draw.c -- this is the only file outside the refresh that touches the
// vid buffer

namespace quake
{
    public partial class draw
    {
        class rectdesc_t {
	        public vid.vrect_t	rect = new vid.vrect_t();
	        public int          width;
	        public int		    height;
	        public byte[]       ptexbytes;
	        public int		    rowbytes;
        };

        static rectdesc_t	r_rectdesc = new rectdesc_t();

        static byte[]       draw_chars;				// 8*8 graphic characters
        public static wad.qpic_t   draw_disc;
        static wad.qpic_t   draw_backtile;

        //=============================================================================
        /* Support Routines */

        public class cachepic_t
        {
	        public string		name;
	        public wad.qpic_t   cache;
        };

        public const int	MAX_CACHED_PICS		= 128;
        static cachepic_t[]	    menu_cachepics = new cachepic_t[MAX_CACHED_PICS];
        static int			    menu_numcachepics;

        static draw()
        {
            for (int kk = 0; kk < MAX_CACHED_PICS; kk++)
                menu_cachepics[kk] = new cachepic_t();
            d_polyse_init();
        }

        public static wad.qpic_t Draw_PicFromWad(string name)
        {
            return wad.W_GetLumpName (name);
        }

        /*
        ================
        Draw_CachePic
        ================
        */
        public static wad.qpic_t Draw_CachePic (string path)
        {
	        cachepic_t	pic;
	        int			i;
	        wad.qpic_t	dat;

            for (pic = menu_cachepics[0], i = 0; i < menu_numcachepics; i++,pic = menu_cachepics[i])
            {
                if (path.CompareTo(pic.name) == 0)
                    break;
            }

	        if (i == menu_numcachepics)
	        {
		        if (menu_numcachepics == MAX_CACHED_PICS)
			        sys_linux.Sys_Error ("menu_numcachepics == MAX_CACHED_PICS");
		        menu_numcachepics++;
                pic.name = path;
	        }

            dat = pic.cache;

	        if (dat != null)
		        return dat;

        //
        // load the pic from disk
        //
	        pic.cache = common.COM_LoadHunkFile(path);
        	
	        dat = (wad.qpic_t)pic.cache;
	        if (dat == null)
	        {
		        sys_linux.Sys_Error ("Draw_CachePic: failed to load " + path);
	        }

	        return dat;
        }
        
        /*
        ===============
        Draw_Init
        ===============
        */
        public static void Draw_Init ()
        {
            draw_chars = wad.W_GetLumpName("conchars");
            draw_disc = wad.W_GetLumpName("disc");
            draw_backtile = wad.W_GetLumpName("backtile");

	        r_rectdesc.width = draw_backtile.width;
	        r_rectdesc.height = draw_backtile.height;
	        r_rectdesc.ptexbytes = draw_backtile.data;
	        r_rectdesc.rowbytes = draw_backtile.width;
        }

        /*
        ================
        Draw_Character

        Draws one 8*8 graphics character with 0 being transparent.
        It can be clipped to the top of the screen to allow the console to be
        smoothly scrolled off.
        ================
        */
        public static void Draw_Character (int x, int y, int num)
        {
	        int 			dest;
	        int			    source;
	        int				drawline;	
	        int				row, col;

	        num &= 255;
        	
	        if (y <= -8)
		        return;			// totally off screen

	        row = num>>4;
	        col = num&15;
	        source = (row<<10) + (col<<3);

	        if (y < 0)
	        {	// clipped
		        drawline = 8 + y;
		        source -= 128*y;
		        y = 0;
	        }
	        else
		        drawline = 8;

	        dest = y*screen.vid.conrowbytes + x;
    	
	        while (drawline-- != 0)
	        {
		        if (draw_chars[source + 0] != 0)
                    screen.vid.conbuffer[dest + 0] = draw_chars[source + 0];
                if (draw_chars[source + 1] != 0)
                    screen.vid.conbuffer[dest + 1] = draw_chars[source + 1];
                if (draw_chars[source + 2] != 0)
                    screen.vid.conbuffer[dest + 2] = draw_chars[source + 2];
                if (draw_chars[source + 3] != 0)
                    screen.vid.conbuffer[dest + 3] = draw_chars[source + 3];
                if (draw_chars[source + 4] != 0)
                    screen.vid.conbuffer[dest + 4] = draw_chars[source + 4];
                if (draw_chars[source + 5] != 0)
                    screen.vid.conbuffer[dest + 5] = draw_chars[source + 5];
                if (draw_chars[source + 6] != 0)
                    screen.vid.conbuffer[dest + 6] = draw_chars[source + 6];
                if (draw_chars[source + 7] != 0)
                    screen.vid.conbuffer[dest + 7] = draw_chars[source + 7];
		        source += 128;
		        dest += screen.vid.conrowbytes;
	        }
        }

        /*
        ================
        Draw_String
        ================
        */
        public static void Draw_String (int x, int y, string str)
        {
            for(int i = 0; i < str.Length; i++)
            {
                Draw_Character(x, y, str[i]);
                x += 8;
            }
        }

        /*
        ================
        Draw_DebugChar

        Draws a single character directly to the upper right corner of the screen.
        This is for debugging lockups by drawing different chars in different parts
        of the code.
        ================
        */
        void Draw_DebugChar (char num)
        {
        }

        /*
        =============
        Draw_Pic
        =============
        */
        public static void Draw_Pic (int x, int y, wad.qpic_t pic)
        {
	        byte[]			dest, source;
	        int				v, u;

	        if ((x < 0) ||
		        (x + pic.width > screen.vid.width) ||
		        (y < 0) ||
		        (y + pic.height > screen.vid.height))
	        {
		        sys_linux.Sys_Error ("Draw_Pic: bad coordinates");
	        }

	        source = pic.data;

	        dest = screen.vid.buffer;

            int srcofs = 0;
            int dstofs = (int)(y * screen.vid.rowbytes + x);
	        for (v=0 ; v<pic.height ; v++)
	        {
                Buffer.BlockCopy(source, srcofs, dest, dstofs, pic.width);
		        dstofs += (int)screen.vid.rowbytes;
		        srcofs += pic.width;
	        }
        }
        
        /*
        =============
        Draw_TransPic
        =============
        */
        public static void Draw_TransPic(int x, int y, wad.qpic_t pic)
        {
            int         dest, source;
            byte        tbyte;
	        int			v, u;

	        if (x < 0 || (uint)(x + pic.width) > screen.vid.width || y < 0 ||
		         (uint)(y + pic.height) > screen.vid.height)
	        {
		        sys_linux.Sys_Error ("Draw_TransPic: bad coordinates");
	        }
        		
	        source = 0;

	        dest = (int)(y * screen.vid.rowbytes + x);

	        if ((pic.width & 7) != 0)
	        {	// general
		        for (v=0 ; v<pic.height ; v++)
		        {
			        for (u=0 ; u<pic.width ; u++)
				        if ( (tbyte=pic.data[source + u]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest + u] = tbyte;
    	
			        dest += (int)screen.vid.rowbytes;
			        source += pic.width;
		        }
	        }
	        else
	        {	// unwound
		        for (v=0 ; v<pic.height ; v++)
		        {
			        for (u=0 ; u<pic.width ; u+=8)
			        {
				        if ( (tbyte=pic.data[source + u]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest + u] = tbyte;
				        if ( (tbyte=pic.data[source + u+1]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest + u+1] = tbyte;
				        if ( (tbyte=pic.data[source + u+2]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest + u+2] = tbyte;
				        if ( (tbyte=pic.data[source + u+3]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest + u+3] = tbyte;
				        if ( (tbyte=pic.data[source + u+4]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest + u+4] = tbyte;
				        if ( (tbyte=pic.data[source + u+5]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest + u+5] = tbyte;
				        if ( (tbyte=pic.data[source + u+6]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest + u+6] = tbyte;
				        if ( (tbyte=pic.data[source + u+7]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest + u+7] = tbyte;
			        }
			        dest += (int)screen.vid.rowbytes;
			        source += pic.width;
		        }
	        }
        }

        /*
        =============
        Draw_TransPicTranslate
        =============
        */
        public static void Draw_TransPicTranslate(int x, int y, wad.qpic_t pic, byte[] translation)
        {
	        int         	dest, source;
            byte            tbyte;
	        int				v, u;

	        if (x < 0 || (uint)(x + pic.width) > screen.vid.width || y < 0 ||
                 (uint)(y + pic.height) > screen.vid.height)
	        {
		        sys_linux.Sys_Error ("Draw_TransPic: bad coordinates");
	        }
        		
	        source = 0;

            dest = (int)(y * screen.vid.rowbytes + x);

	        if ((pic.width & 7) != 0)
	        {	// general
		        for (v=0 ; v<pic.height ; v++)
		        {
			        for (u=0 ; u<pic.width ; u++)
				        if ( (tbyte=pic.data[source+u]) != TRANSPARENT_COLOR)
                            screen.vid.buffer[dest+u] = translation[tbyte];

			        dest += (int)screen.vid.rowbytes;
			        source += pic.width;
		        }
	        }
	        else
	        {	// unwound
		        for (v=0 ; v<pic.height ; v++)
		        {
			        for (u=0 ; u<pic.width ; u+=8)
			        {
				        if ( (tbyte=pic.data[source+u]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest+u] = translation[tbyte];
				        if ( (tbyte=pic.data[source+u+1]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest+u+1] = translation[tbyte];
				        if ( (tbyte=pic.data[source+u+2]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest+u+2] = translation[tbyte];
				        if ( (tbyte=pic.data[source+u+3]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest+u+3] = translation[tbyte];
				        if ( (tbyte=pic.data[source+u+4]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest+u+4] = translation[tbyte];
				        if ( (tbyte=pic.data[source+u+5]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest+u+5] = translation[tbyte];
				        if ( (tbyte=pic.data[source+u+6]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest+u+6] = translation[tbyte];
				        if ( (tbyte=pic.data[source+u+7]) != TRANSPARENT_COLOR)
					        screen.vid.buffer[dest+u+7] = translation[tbyte];
			        }
                    dest += (int)screen.vid.rowbytes;
			        source += pic.width;
		        }
	        }
        }
        
        static void Draw_CharToConback (int num, byte[] dest, int ofs)
        {
	        int		row, col;
	        int	    source;
	        int		drawline;
	        int		x;

	        row = num>>4;
	        col = num&15;
	        source = (row<<10) + (col<<3);

	        drawline = 8;

	        while (drawline-- != 0)
	        {
		        for (x=0 ; x<8 ; x++)
			        if (draw_chars[source + x] != 0)
                        dest[x + ofs] = (byte)(0x60 + draw_chars[source + x]);
		        source += 128;
		        ofs += 320;
	        }
        }

        /*
        ================
        Draw_ConsoleBackground

        ================
        */

        private static NumberFormatInfo _formatNumber = new CultureInfo("en-US").NumberFormat;

        public static void Draw_ConsoleBackground (int lines)
        {
	        int				x, y, v;
	        int			    src, dest;
	        int				f, fstep;
	        wad.qpic_t	    conback;
	        string			ver;

	        conback = Draw_CachePic ("gfx/conback.lmp");

            ver = String.Format(_formatNumber, "(QuakeLight {0:##.00}) {1:####.00}", quakedef.LINUX_VERSION, quakedef.VERSION);
//	        ver = "(Linux Quake " + quakedef.LINUX_VERSION + ") " + quakedef.VERSION;
	        dest = 320*186 + 320 - 11 - 8*ver.Length;

	        for (x=0 ; x<ver.Length ; x++)
		        Draw_CharToConback (ver[x], conback.data, dest+(x<<3));
        	
        // draw the pic
            dest = 0;

	        for (y=0 ; y<lines ; y++, dest += screen.vid.conrowbytes)
	        {
		        v = (int)((screen.vid.conheight - lines + y)*200/screen.vid.conheight);
		        src = v*320;
		        if (screen.vid.conwidth == 320)
                    Buffer.BlockCopy(conback.data, src, screen.vid.conbuffer, dest, (int)screen.vid.conwidth);
		        else
		        {
			        f = 0;
			        fstep = (int)(320*0x10000/screen.vid.conwidth);
			        for (x=0 ; x<screen.vid.conwidth ; x+=4)
			        {
				        screen.vid.conbuffer[dest+x] = conback.data[src+(f>>16)];
				        f += fstep;
				        screen.vid.conbuffer[dest+x+1] = conback.data[src+(f>>16)];
				        f += fstep;
				        screen.vid.conbuffer[dest+x+2] = conback.data[src+(f>>16)];
				        f += fstep;
				        screen.vid.conbuffer[dest+x+3] = conback.data[src+(f>>16)];
				        f += fstep;
			        }
		        }
	        }
        }

        /*
        ==============
        R_DrawRect8
        ==============
        */
        static void R_DrawRect8 (vid.vrect_t prect, int rowbytes, int psrc, int transparent)
        {
	        byte	t;
	        int		i, j, srcdelta, destdelta;
	        int	    pdest;

	        pdest = (int)(/*vid.buffer + */(prect.y * screen.vid.rowbytes) + prect.x);

	        srcdelta = rowbytes - prect.width;
	        destdelta = (int)(screen.vid.rowbytes - prect.width);

	        if (transparent != 0)
	        {
		        for (i=0 ; i<prect.height ; i++)
		        {
			        for (j=0 ; j<prect.width ; j++)
			        {
                        t = r_rectdesc.ptexbytes[psrc];
				        if (t != TRANSPARENT_COLOR)
				        {
					        screen.vid.buffer[pdest] = t;
				        }

				        psrc++;
				        pdest++;
			        }

			        psrc += srcdelta;
			        pdest += destdelta;
		        }
	        }
	        else
	        {
		        for (i=0 ; i<prect.height ; i++)
		        {
                    Buffer.BlockCopy(r_rectdesc.ptexbytes, psrc, screen.vid.buffer, pdest, prect.width);
			        psrc += rowbytes;
			        pdest += (int)screen.vid.rowbytes;
		        }
	        }
        }

        /*
        =============
        Draw_TileClear

        This repeats a 64*64 tile graphic to fill the screen around a sized down
        refresh window.
        =============
        */
        public static void Draw_TileClear (int x, int y, int w, int h)
        {
	        int				width, height, tileoffsetx, tileoffsety;
	        int			    psrc;
	        vid.vrect_t		vr = new vid.vrect_t();

	        r_rectdesc.rect.x = x;
	        r_rectdesc.rect.y = y;
	        r_rectdesc.rect.width = w;
	        r_rectdesc.rect.height = h;

	        vr.y = r_rectdesc.rect.y;
	        height = r_rectdesc.rect.height;

	        tileoffsety = vr.y % r_rectdesc.height;

	        while (height > 0)
	        {
		        vr.x = r_rectdesc.rect.x;
		        width = r_rectdesc.rect.width;

		        if (tileoffsety != 0)
			        vr.height = r_rectdesc.height - tileoffsety;
		        else
			        vr.height = r_rectdesc.height;

		        if (vr.height > height)
			        vr.height = height;

		        tileoffsetx = vr.x % r_rectdesc.width;

		        while (width > 0)
		        {
			        if (tileoffsetx != 0)
				        vr.width = r_rectdesc.width - tileoffsetx;
			        else
				        vr.width = r_rectdesc.width;

			        if (vr.width > width)
				        vr.width = width;

			        psrc = (tileoffsety * r_rectdesc.rowbytes) + tileoffsetx;

			        if (render.r_pixbytes == 1)
			        {
				        R_DrawRect8 (vr, r_rectdesc.rowbytes, psrc, 0);
			        }

			        vr.x += vr.width;
			        width -= vr.width;
			        tileoffsetx = 0;	// only the left tile can be left-clipped
		        }

		        vr.y += vr.height;
		        height -= vr.height;
		        tileoffsety = 0;		// only the top tile can be top-clipped
	        }
        }
        
        /*
        =============
        Draw_Fill

        Fills a box of pixels with a single color
        =============
        */
        void Draw_Fill (int x, int y, int w, int h, int c)
        {
        }

        //=============================================================================

        /*
        ================
        Draw_FadeScreen

        ================
        */
        public static void Draw_FadeScreen ()
        {
	        int			x,y;
	        int		    pbuf;

	        for (y=0 ; y<screen.vid.height ; y++)
	        {
		        int	t;

		        pbuf = (int)(screen.vid.rowbytes*y);
		        t = (y & 1) << 1;

		        for (x=0 ; x<screen.vid.width ; x++)
		        {
			        if ((x & 3) != t)
				        screen.vid.buffer[pbuf+x] = 0;
		        }
	        }
        }

        //=============================================================================

        /*
        ================
        Draw_BeginDisc

        Draws the little blue disc in the corner of the screen.
        Call before beginning any disc IO.
        ================
        */
        public static void Draw_BeginDisc ()
        {
        }

        /*
        ================
        Draw_EndDisc

        Erases the disc icon.
        Call after completing any disc IO
        ================
        */
        public static void Draw_EndDisc()
        {
        }
    }
}
