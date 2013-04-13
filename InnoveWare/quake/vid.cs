using System;
using InnoveWare;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
// vid.h -- video driver defs
//
// vid_dos.c: DOS-specific video routines
//

namespace quake
{
    public sealed class vid
    {
        public const int VID_CBITS	= 6;
        public const int VID_GRADES = (1 << VID_CBITS);

        public class vrect_t
        {
	        public int		x,y,width,height;
	        public vrect_t	pnext;
        };

        public class viddef_t
        {
	        public byte[]	buffer;		// invisible buffer
	        public byte[]	colormap;		// 256 * VID_GRADES size
	        ushort[]	    colormap16;	// 256 * VID_GRADES size
	        public int		fullbright;		// index of first fullbright color
	        public uint		rowbytes;	// may be > width if displayed in a window
            public uint     width;
            public uint     height;
            public double   aspect;		// width / height -- < 0 is taller than wide
	        public int		numpages;
            public bool     recalc_refdef;	// if true, recalc vid-based stuff
	        public byte[]	conbuffer;
            public int      conrowbytes;
            public uint     conwidth;
            public uint     conheight;
            public int      maxwarpwidth;
            public int      maxwarpheight;
	        byte[]			direct;		// direct drawing to framebuffer, if not
								        //  NULL
        };

        //static BitmapData surface;
        static WriteableBitmap surface;

        static int			vid_modenum;
        static int			vid_testingmode, vid_realmode;
        double		vid_testendtime;

        static cvar_t		vid_mode = new cvar_t("vid_mode","0", false);
        cvar_t		vid_wait = new cvar_t("vid_wait","0");
        cvar_t		vid_nopageflip = new cvar_t("vid_nopageflip","0", true);
        cvar_t		_vid_wait_override = new cvar_t("_vid_wait_override", "0", true);
        cvar_t		_vid_default_mode = new cvar_t("_vid_default_mode","0", true);
        cvar_t		_vid_default_mode_win = new cvar_t("_vid_default_mode_win","1", true);
        cvar_t		vid_config_x = new cvar_t("vid_config_x","800", true);
        cvar_t		vid_config_y = new cvar_t("vid_config_y","600", true);
        cvar_t		vid_stretch_by_2 = new cvar_t("vid_stretch_by_2","1", true);
        cvar_t		_windowed_mouse = new cvar_t("_windowed_mouse","0", true);
        cvar_t		vid_fullscreen_mode = new cvar_t("vid_fullscreen_mode","3", true);
        cvar_t		vid_windowed_mode = new cvar_t("vid_windowed_mode","0", true);
        cvar_t		block_switch = new cvar_t("block_switch","0", true);
        cvar_t		vid_window_x = new cvar_t("vid_window_x", "0", true);
        cvar_t		vid_window_y = new cvar_t("vid_window_y", "0", true);

        int	d_con_indirect = 0;

        static int		numvidmodes = 1;

        static int	    firstupdate = 1;

        static byte[]	vid_current_palette = new byte[768];	// save for mode changes
        
        static bool	nomodecheck = false;

        /*
        ================
        VID_Init
        ================
        */
        public static void    VID_Init (byte[] palette)
        {
	        vid_testingmode = 0;

	        vid_modenum = (int)vid_mode.value;

	        VID_SetMode (vid_modenum, palette);

	        vid_realmode = vid_modenum;

            draw.d_pzbuffer = new short[screen.vid.width * screen.vid.height];
        }

        /*
        ================
        VID_NumModes
        ================
        */
        int VID_NumModes ()
        {
	        return (numvidmodes);
        }

        /*
        ================
        VID_SetMode 
        ================
        */
        static int VID_SetMode (int modenum, byte[] palette)
        {
	        int		stat;

	        if ((modenum >= numvidmodes) || (modenum < 0))
	        {
                cvar_t.Cvar_SetValue("vid_mode", (double)vid_modenum);

		        nomodecheck = true;
		        console.Con_Printf ("No such video mode: " + modenum);
		        nomodecheck = false;

		        modenum = 0;	// mode hasn't been set yet, so initialize to base
						        //  mode since they gave us an invalid initial mode
		        return 0;
	        }

        // initialize the new mode
            screen.vid.width = (uint)Page.gwidth;
            screen.vid.height = (uint)Page.gheight;
            screen.vid.aspect = ((double)screen.vid.height / (double)screen.vid.width) * (320.0 / 240.0);
            screen.vid.rowbytes = screen.vid.width;
            screen.vid.numpages = 1;
            screen.vid.colormap = host.host_colormap;
            screen.vid.fullbright = 256 - BitConverter.ToInt32(screen.vid.colormap, 2048*4);

            draw.D_InitCaches(656400);

            screen.vid.maxwarpwidth = draw.WARP_WIDTH;
            screen.vid.maxwarpheight = draw.WARP_HEIGHT;

            VID_SetPalette(palette);

            screen.vid.buffer = new byte[screen.vid.width * screen.vid.height];
            //surface = new BitmapData(vid_current_palette, screen.vid.buffer, (int)screen.vid.width, (int)screen.vid.height);
            surface = new WriteableBitmap((int)screen.vid.width, (int)screen.vid.height);
            Page.thePage.image.Source = surface;
            /*Page.thePage.image2.Source = surface;
            Page.thePage.image3.Source = surface;
            Page.thePage.image4.Source = surface;*/

            screen.vid.conbuffer = screen.vid.buffer;
            screen.vid.conrowbytes = (int)screen.vid.rowbytes;
            screen.vid.conwidth = screen.vid.width;
            screen.vid.conheight = screen.vid.height;

            vid_modenum = modenum;
	        cvar_t.Cvar_SetValue ("vid_mode", (double)vid_modenum);

	        screen.vid.recalc_refdef = true;

	        return 1;
        }

        /*
        ================
        VID_SetPalette
        ================
        */
        public static void    VID_SetPalette (byte[] palette)
        {
            if (palette != vid_current_palette)
            {
                Buffer.BlockCopy(palette, 0, vid_current_palette, 0, 768);
                /*if (surface != null)
                    surface.UpdatePalette();*/
            }
        }
        
        /*
        ================
        VID_ShiftPalette
        ================
        */
        public static void    VID_ShiftPalette (byte[] palette)
        {
	        VID_SetPalette (palette);
        }
        
        /*
        ================
        VID_Shutdown
        ================
        */
        void VID_Shutdown ()
        {
	        vid_testingmode = 0;
        }


        /*
        ================
        VID_Update
        ================
        */
        public static void    VID_Update (vrect_t rects)
        {
            //surface.UpdateBitmap(rects.x, rects.y, rects.width, rects.height);
            //surface.Blit(Page.bitmap);

            int ofs = surface.PixelWidth * rects.y + rects.x;
            for (int r = 0; r < rects.height; r++)
            {
                for (int col = 0; col < rects.width; col++)
                {
                    int c = screen.vid.buffer[ofs + col];
                    surface.Pixels[ofs + col] = (vid_current_palette[c * 3 + 0] << 16) | (vid_current_palette[c * 3 + 1] << 8) | vid_current_palette[c * 3 + 2];
                }
                ofs += surface.PixelWidth;
            }

            surface.Invalidate();
            //Page.thePage.image.Source = surface;
        }

        /*
        ================
        D_BeginDirectRect
        ================
        */
        void D_BeginDirectRect (int x, int y, byte[] pbitmap, int width, int height)
        {
        }

        /*
        ================
        D_EndDirectRect
        ================
        */
        void D_EndDirectRect (int x, int y, int width, int height)
        {
        }

        //===========================================================================

        static int	vid_line, vid_wmodes, vid_column_size;

        const int MAX_COLUMN_SIZE = 11;

        /*
        ================
        VID_MenuDraw
        ================
        */
        void VID_MenuDraw ()
        {
        }
    }
}
