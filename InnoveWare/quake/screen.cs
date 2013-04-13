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
// screen.c -- master for refresh, status bar, console, chat, notify, etc

namespace quake
{
    public sealed class screen
    {
        // only the refresh window will be updated unless these variables are flagged 
        public static bool			scr_copytop;
        public static bool		    scr_copyeverything;

        public static double		scr_con_current;
        static double		        scr_conlines;		// lines of console to display

        static double		oldscreensize, oldfov;
        public static cvar_t		scr_viewsize = new cvar_t("viewsize","100", true);
        public static cvar_t		scr_fov = new cvar_t("fov","90");	// 10 - 170
        static cvar_t		scr_conspeed = new cvar_t("scr_conspeed","300");
        static cvar_t       scr_centertime = new cvar_t("scr_centertime", "2");
        static cvar_t		scr_showram = new cvar_t("showram","1");
        static cvar_t		scr_showturtle = new cvar_t("showturtle","0");
        static cvar_t		scr_showpause = new cvar_t("showpause","1");
        static cvar_t       scr_printspeed = new cvar_t("scr_printspeed", "8");

        static bool scr_initialized;		// ready to draw

        static wad.qpic_t scr_ram;
        static wad.qpic_t scr_net;
        static wad.qpic_t scr_turtle;

        public static int	    scr_fullupdate;

        static int			    clearconsole;
        public static int	    clearnotify;

        public static vid.viddef_t  vid = new vid.viddef_t();				// global video state

        public static vid.vrect_t   scr_vrect = new vid.vrect_t();

        public static bool	    scr_disabled_for_loading;
        static bool	    scr_drawloading;
        static double	scr_disabled_time;
        static bool	    scr_skipupdate;

        static bool	    block_drawing;

        /*
        ===============================================================================

        CENTER PRINTING

        ===============================================================================
        */

        static string	scr_centerstring;
        static double      scr_centertime_start;	// for slow victory printing
        public static double	scr_centertime_off;
        static int		scr_center_lines;
        static int		scr_erase_lines;
        static int		scr_erase_center;

        /*
        ==============
        SCR_CenterPrint

        Called for important messages that should stay in the center of the screen
        for a few moments
        ==============
        */
        public static void SCR_CenterPrint (string str)
        {
	        scr_centerstring = str;
	        scr_centertime_off = scr_centertime.value;
		    scr_centertime_start = client.cl.time;

	    // count the number of lines for centering
		    scr_center_lines = 1;
		    for(int i = 0; i < str.Length; i++)
		    {
			    if (str[i] == '\n')
				    scr_center_lines++;
		    }
        }

        static void SCR_EraseCenterString ()
        {
            int y;

            if (scr_erase_center++ > screen.vid.numpages)
            {
                scr_erase_lines = 0;
                return;
            }

            if (scr_center_lines <= 4)
                y = (int)(screen.vid.height * 0.35);
            else
                y = 48;

            scr_copytop = true;
            draw.Draw_TileClear(0, y, (int)screen.vid.width, 8 * scr_erase_lines);
        }

        static void SCR_DrawCenterString ()
        {
	        int	    start;
	        int		l;
	        int		j;
	        int		x, y;
	        int		remaining;

        // the finale prints the characters one at a time
	        if (client.cl.intermission != 0)
		        remaining = (int)(scr_printspeed.value * (client.cl.time - scr_centertime_start));
	        else
		        remaining = 9999;

	        scr_erase_center = 0;
	        start = 0;

	        if (scr_center_lines <= 4)
		        y = (int)(screen.vid.height*0.35);
	        else
		        y = 48;

	        do	
	        {
	        // scan the width of the line
		        for (l=0 ; l<40 ; l++)
                    if (start + l == scr_centerstring.Length || scr_centerstring[start+l] == '\n')
				        break;
		        x = (int)((screen.vid.width - l*8)/2);
		        for (j=0 ; j<l ; j++, x+=8)
		        {
                    draw.Draw_Character(x, y, scr_centerstring[start+j]);	
			        if (remaining-- == 0)
				        return;
		        }
        			
		        y += 8;

                while (start != scr_centerstring.Length && scr_centerstring[start] != '\n')
			        start++;

                if (start == scr_centerstring.Length)
			        break;
		        start++;		// skip the \n
	        } while (true);
        }

        static void SCR_CheckDrawCenterString ()
        {
		    scr_copytop = true;
		    if (scr_center_lines > scr_erase_lines)
			    scr_erase_lines = scr_center_lines;

		    scr_centertime_off -= host.host_frametime;
    	
		    if (scr_centertime_off <= 0 && client.cl.intermission == 0)
			    return;
		    if (keys.key_dest != keys.keydest_t.key_game)
			    return;

	        SCR_DrawCenterString ();
        }

        //=============================================================================

        /*
        ====================
        CalcFov
        ====================
        */
        static double CalcFov(double fov_x, double width, double height)
        {
            double a;
            double x;

            if (fov_x < 1 || fov_x > 179)
                    sys_linux.Sys_Error ("Bad fov: " + fov_x);

            x = width / Math.Tan(fov_x / 360 * mathlib.M_PI);

            a = Math.Atan(height / x);

            a = a*360/mathlib.M_PI;

            return a;
        }

        /*
        =================
        SCR_CalcRefdef

        Must be called whenever vid changes
        Internal use only
        =================
        */
        static void SCR_CalcRefdef ()
        {
            vid.vrect_t     vrect = new vid.vrect_t();
            double          size;

            scr_fullupdate = 0;		// force a background redraw
            screen.vid.recalc_refdef = false;

            // force the status bar to redraw
            sbar.Sbar_Changed();

            //========================================

            // bound viewsize
            if (scr_viewsize.value < 30)
                cvar_t.Cvar_Set("viewsize", "30");
            if (scr_viewsize.value > 120)
                cvar_t.Cvar_Set("viewsize", "120");

            // bound field of view
            if (scr_fov.value < 10)
                cvar_t.Cvar_Set("fov", "10");
            if (scr_fov.value > 170)
                cvar_t.Cvar_Set("fov", "170");

	        render.r_refdef.fov_x = scr_fov.value;
            render.r_refdef.fov_y = CalcFov(render.r_refdef.fov_x, render.r_refdef.vrect.width, render.r_refdef.vrect.height);

        // intermission is always full screen	
	        if (client.cl.intermission != 0)
		        size = 120;
	        else
		        size = scr_viewsize.value;

            if (size >= 120)
                sbar.sb_lines = 0;		// no status bar at all
            else if (size >= 110)
                sbar.sb_lines = 24;		// no inventory
            else
                sbar.sb_lines = 24 + 16 + 8;

            // these calculations mirror those in R_Init() for r_refdef, but take no
            // account of water warping
            vrect.x = 0;
            vrect.y = 0;
            vrect.width = (int)screen.vid.width;
            vrect.height = (int)screen.vid.height;

	        render.R_SetVrect (vrect, scr_vrect, sbar.sb_lines);

            // guard against going from one mode to another that's less than half the
            // vertical resolution
            if (scr_con_current > screen.vid.height)
                scr_con_current = screen.vid.height;

            // notify the refresh of the change
            render.R_ViewChanged(vrect, sbar.sb_lines, screen.vid.aspect);
        }

        /*
        =================
        SCR_SizeUp_f

        Keybinding command
        =================
        */
        static void SCR_SizeUp_f ()
        {
	        cvar_t.Cvar_SetValue ("viewsize",scr_viewsize.value+10);
            screen.vid.recalc_refdef = true;
        }

        /*
        =================
        SCR_SizeDown_f

        Keybinding command
        =================
        */
        static void SCR_SizeDown_f ()
        {
	        cvar_t.Cvar_SetValue ("viewsize",scr_viewsize.value-10);
            screen.vid.recalc_refdef = true;
        }

        //============================================================================

        /*
        ==================
        SCR_Init
        ==================
        */
        public static void SCR_Init ()
        {
            cvar_t.Cvar_RegisterVariable(scr_fov);
            cvar_t.Cvar_RegisterVariable(scr_viewsize);
            cvar_t.Cvar_RegisterVariable(scr_conspeed);
            cvar_t.Cvar_RegisterVariable(scr_showram);
            cvar_t.Cvar_RegisterVariable(scr_showturtle);
            cvar_t.Cvar_RegisterVariable(scr_showpause);
            cvar_t.Cvar_RegisterVariable(scr_centertime);
            cvar_t.Cvar_RegisterVariable(scr_printspeed);

            //
            // register our commands
            //
            cmd.Cmd_AddCommand("screenshot", SCR_ScreenShot_f);
            cmd.Cmd_AddCommand("sizeup", SCR_SizeUp_f);
            cmd.Cmd_AddCommand("sizedown", SCR_SizeDown_f);

            scr_ram = draw.Draw_PicFromWad("ram");
            scr_net = draw.Draw_PicFromWad("net");
            scr_turtle = draw.Draw_PicFromWad("turtle");

	        scr_initialized = true;
        }

        /*
        ==============
        SCR_DrawRam
        ==============
        */
        static void SCR_DrawRam ()
        {
            if (scr_showram.value == 0)
                return;

            return;
            draw.Draw_Pic(scr_vrect.x + 32, scr_vrect.y, scr_ram);
        }

        /*
        ==============
        SCR_DrawTurtle
        ==============
        */
        static int count;
        static void SCR_DrawTurtle()
        {
	        if (scr_showturtle.value == 0)
		        return;

	        if (host.host_frametime < 0.1)
	        {
		        count = 0;
		        return;
	        }

	        count++;
	        if (count < 3)
		        return;

	        draw.Draw_Pic (scr_vrect.x, scr_vrect.y, scr_turtle);
        }

        /*
        ==============
        SCR_DrawNet
        ==============
        */
        static void SCR_DrawNet ()
        {
	        if (host.realtime - client.cl.last_received_message < 0.3)
		        return;
            if (client.cls.demoplayback)
                return;

            draw.Draw_Pic(scr_vrect.x + 64, scr_vrect.y, scr_net);
        }

        /*
        ==============
        DrawPause
        ==============
        */
        static void SCR_DrawPause ()
        {
            wad.qpic_t pic;

            if (scr_showpause.value == 0)		// turn off for screenshots
                return;

	        if (!client.cl.paused)
                return;

            pic = draw.Draw_CachePic("gfx/pause.lmp");
            draw.Draw_Pic((int)(vid.width - pic.width) / 2,
                (int)(vid.height - 48 - pic.height) / 2, pic);
        }

        /*
        ==============
        SCR_DrawLoading
        ==============
        */
        static void SCR_DrawLoading ()
        {
            wad.qpic_t pic;

            if (!scr_drawloading)
                return;

            pic = draw.Draw_CachePic("gfx/loading.lmp");
            draw.Draw_Pic((int)(vid.width - pic.width) / 2,
                (int)(vid.height - 48 - pic.height) / 2, pic);
        }

        //=============================================================================

        /*
        ==================
        SCR_SetUpToDrawConsole
        ==================
        */
        static void SCR_SetUpToDrawConsole ()
        {
            console.Con_CheckResize();

            if (scr_drawloading)
                return;		// never a console with loading plaque

            // decide on the height of the console
            console.con_forcedup = client.cls.signon != client.SIGNONS;

            if (console.con_forcedup)
            {
                scr_conlines = screen.vid.height;		// full screen
                scr_con_current = scr_conlines;
            }
	        else if (keys.key_dest == keys.keydest_t.key_console)
		        scr_conlines = vid.height/2;	// half screen
            else
                scr_conlines = 0;				// none visible

            if (scr_conlines < scr_con_current)
            {
                scr_con_current -= scr_conspeed.value * host.host_frametime;
                if (scr_conlines > scr_con_current)
                    scr_con_current = scr_conlines;
            }
            else if (scr_conlines > scr_con_current)
            {
                scr_con_current += scr_conspeed.value * host.host_frametime;
                if (scr_conlines < scr_con_current)
                    scr_con_current = scr_conlines;
            }

            if (clearconsole++ < screen.vid.numpages)
            {
                scr_copytop = true;
                draw.Draw_TileClear(0, (int)scr_con_current, (int)screen.vid.width, (int)(screen.vid.height - (int)scr_con_current));
                sbar.Sbar_Changed();
            }
            else if (clearnotify++ < screen.vid.numpages)
            {
                scr_copytop = true;
                draw.Draw_TileClear(0, 0, (int)screen.vid.width, console.con_notifylines);
            }
            else
                console.con_notifylines = 0;
        }
        	
        /*
        ==================
        SCR_DrawConsole
        ==================
        */
        static void SCR_DrawConsole ()
        {
            if (scr_con_current != 0)
            {
                scr_copyeverything = true;
                console.Con_DrawConsole((int)scr_con_current, true);
                clearconsole = 0;
            }
            else
            {
                if (keys.key_dest == keys.keydest_t.key_game || keys.key_dest == keys.keydest_t.key_message)
                    console.Con_DrawNotify();	// only draw notify in game
            }
        }


        /* 
        ============================================================================== 
         
						        SCREEN SHOTS 
         
        ============================================================================== 
        */ 
         

        class pcx_t
        {
            char	manufacturer;
            char	version;
            char	encoding;
            char	bits_per_pixel;
            ushort	xmin,ymin,xmax,ymax;
            ushort	hres,vres;
            byte[]	palette = new byte[48];
            char	reserved;
            char	color_planes;
            ushort	bytes_per_line;
            ushort	palette_type;
            char[]	filler = new char[58];
            byte	data;			// unbounded
        };

        /* 
        ============== 
        WritePCXfile 
        ============== 
        */ 
        void WritePCXfile (string filename, byte[] data, int width, int height,
	        int rowbytes, byte[] palette) 
        {
        } 

        /* 
        ================== 
        SCR_ScreenShot_f
        ================== 
        */  
        static void SCR_ScreenShot_f () 
        { 
        } 

        //=============================================================================

        /*
        ===============
        SCR_BeginLoadingPlaque

        ================
        */
        public static void SCR_BeginLoadingPlaque ()
        {
            sound.S_StopAllSounds(true);

            if (client.cls.state != client.cactive_t.ca_connected)
                return;
            if (client.cls.signon != client.SIGNONS)
                return;

            // redraw with no console and the loading plaque
            console.Con_ClearNotify();
            scr_centertime_off = 0;
            scr_con_current = 0;

            scr_drawloading = true;
            scr_fullupdate = 0;
            sbar.Sbar_Changed();
            SCR_UpdateScreen();
            scr_drawloading = false;

            scr_disabled_for_loading = true;
            scr_disabled_time = host.realtime;
            scr_fullupdate = 0;
        }

        /*
        ===============
        SCR_EndLoadingPlaque

        ================
        */
        public static void SCR_EndLoadingPlaque ()
        {
            scr_disabled_for_loading = false;
            scr_fullupdate = 0;
            console.Con_ClearNotify();
        }

        //=============================================================================

        static string scr_notifystring;
        static bool	scr_drawdialog;

        static void SCR_DrawNotifyString ()
        {
	        int	    start;
	        int		l;
	        int		j;
	        int		x, y;

	        start = 0;

	        y = (int)(screen.vid.height*0.35);

	        do	
	        {
	        // scan the width of the line
		        for (l=0 ; l<40 ; l++)
                    if (start + l == scr_notifystring.Length || scr_notifystring[start+l] == '\n')
				        break;
		        x = (int)((screen.vid.width - l*8)/2);
		        for (j=0 ; j<l ; j++, x+=8)
                    draw.Draw_Character(x, y, scr_notifystring[start+j]);	
        			
		        y += 8;

                while (start != scr_notifystring.Length && scr_notifystring[start] != '\n')
			        start++;

                if (start == scr_notifystring.Length)
			        break;
		        start++;		// skip the \n
	        } while (true);
        }

        /*
        ==================
        SCR_ModalMessage

        Displays a text string in the center of the screen and waits for a Y or N
        keypress.  
        ==================
        */
        int SCR_ModalMessage (string text)
        {
            return -1;
        }


        //=============================================================================

        /*
        ===============
        SCR_BringDownConsole

        Brings the console down and fades the palettes back to normal
        ================
        */
        void SCR_BringDownConsole ()
        {
	        int		i;
        	
	        scr_centertime_off = 0;
        	
	        for (i=0 ; i<20 && scr_conlines != scr_con_current ; i++)
		        SCR_UpdateScreen ();

	        quake.vid.VID_SetPalette (host.host_basepal);
        }


        /*
        ==================
        SCR_UpdateScreen

        This is called every frame, and can also be called explicitly to flush
        text to the screen.

        WARNING: be very careful calling this from elsewhere, because the refresh
        needs almost the entire 256k of stack space!
        ==================
        */
        static double oldscr_viewsize;
        static double oldlcd_x;
        public static void SCR_UpdateScreen()
        {
            vid.vrect_t vrect = new vid.vrect_t();

            if (scr_skipupdate || block_drawing)
                return;

            scr_copytop = false;
            scr_copyeverything = false;

            if (scr_disabled_for_loading)
            {
                if (host.realtime - scr_disabled_time > 60)
                {
                    scr_disabled_for_loading = false;
                    console.Con_Printf("load failed.\n");
                }
                else
                    return;
            }

            if (client.cls.state == client.cactive_t.ca_dedicated)
                return;				// stdout only

            if (!scr_initialized || !console.con_initialized)
                return;				// not initialized yet

	        if (scr_viewsize.value != oldscr_viewsize)
	        {
		        oldscr_viewsize = scr_viewsize.value;
                screen.vid.recalc_refdef = true;
            }
        	
        //
        // check for vid changes
        //
	        if (oldfov != scr_fov.value)
	        {
		        oldfov = scr_fov.value;
                vid.recalc_refdef = true;
            }
        	
	        if (oldscreensize != scr_viewsize.value)
	        {
		        oldscreensize = scr_viewsize.value;
                vid.recalc_refdef = true;
            }

            if (screen.vid.recalc_refdef)
            {
                // something changed, so reorder the screen
                SCR_CalcRefdef();
            }

            if (scr_fullupdate++ < screen.vid.numpages)
            {	// clear the entire screen
                scr_copyeverything = true;
                draw.Draw_TileClear(0, 0, (int)screen.vid.width, (int)screen.vid.height);
                sbar.Sbar_Changed();
            }

	        SCR_SetUpToDrawConsole ();
	        SCR_EraseCenterString ();

	        view.V_RenderView ();

	        if (scr_drawdialog)
	        {
                sbar.Sbar_Draw();
                draw.Draw_FadeScreen();
                SCR_DrawNotifyString();
                scr_copyeverything = true;
            }
	        else if (scr_drawloading)
	        {
                SCR_DrawLoading();
		        sbar.Sbar_Draw ();
	        }
            else if (client.cl.intermission == 1 && keys.key_dest == keys.keydest_t.key_game)
	        {
                sbar.Sbar_IntermissionOverlay();
	        }
            else if (client.cl.intermission == 2 && keys.key_dest == keys.keydest_t.key_game)
	        {
                sbar.Sbar_FinaleOverlay();
		        SCR_CheckDrawCenterString ();
	        }
            else if (client.cl.intermission == 3 && keys.key_dest == keys.keydest_t.key_game)
	        {
		        SCR_CheckDrawCenterString ();
            }
	        else
	        {
                SCR_DrawRam();
                SCR_DrawNet();
                SCR_DrawTurtle();
                SCR_DrawPause();
                SCR_CheckDrawCenterString();
                sbar.Sbar_Draw();
                SCR_DrawConsole();
                menu.M_Draw();
            }

        	view.V_UpdatePalette ();

        //
        // update one of three areas
        //

            if (scr_copyeverything)
            {
                vrect.x = 0;
                vrect.y = 0;
                vrect.width = (int)screen.vid.width;
                vrect.height = (int)screen.vid.height;
                vrect.pnext = null;

                quake.vid.VID_Update(vrect);
            }
            else if (scr_copytop)
            {
                vrect.x = 0;
                vrect.y = 0;
                vrect.width = (int)screen.vid.width;
                vrect.height = (int)(screen.vid.height - sbar.sb_lines);
                vrect.pnext = null;

                quake.vid.VID_Update(vrect);
            }
            else
            {
                vrect.x = scr_vrect.x;
                vrect.y = scr_vrect.y;
                vrect.width = scr_vrect.width;
                vrect.height = scr_vrect.height;
                vrect.pnext = null;

                quake.vid.VID_Update(vrect);
            }
        }


        /*
        ==================
        SCR_UpdateWholeScreen
        ==================
        */
        void SCR_UpdateWholeScreen ()
        {
	        scr_fullupdate = 0;
	        SCR_UpdateScreen ();
        }
    }
}