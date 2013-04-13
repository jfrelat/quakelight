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
// console.c

namespace quake
{
    public sealed class console
    {
        static int 		con_linewidth;

        static double	con_cursorspeed = 4;

        const int	CON_TEXTSIZE = 16384;

        public static bool 	    con_forcedup;		// because no entities to refresh

        public static int		con_totallines;		// total lines in console scrollback
        public static int		con_backscroll;		// lines up from bottom to display
        static int			con_current;		// where next message will be printed
        static int			con_x;				// offset in current line for next print
        static char[]       con_text = null;

        static cvar_t		con_notifytime = new cvar_t("con_notifytime","3");		//seconds

        const int	NUM_CON_TIMES = 4;
        static double[]	con_times = new double[NUM_CON_TIMES];	// realtime time the line was generated
								        // for transparent notify lines

        static int			con_vislines;

        static bool    	    con_debuglog;

        const int	MAXCMDLINE = 256;

        public static bool	    con_initialized;

        public static int		con_notifylines;		// scan lines to clear for notify lines

        /*
        ================
        Con_ToggleConsole_f
        ================
        */
        public static void Con_ToggleConsole_f ()
        {
            if (keys.key_dest == keys.keydest_t.key_console)
            {
                if (client.cls.state == client.cactive_t.ca_connected)
                {
                    keys.key_dest = keys.keydest_t.key_game;
			        keys.key_lines[keys.edit_line] = keys.key_lines[keys.edit_line].Substring(0, 1);	// clear any typing
			        keys.key_linepos = 1;
                }
                else
                {
                    menu.M_Menu_Main_f();
                }
            }
            else
                keys.key_dest = keys.keydest_t.key_console;

            screen.SCR_EndLoadingPlaque();
            for (int kk = 0; kk < con_times.Length; kk++)
                con_times[kk] = 0;
        }

        /*
        ================
        Con_Clear_f
        ================
        */
        static void Con_Clear_f ()
        {
            if (con_text != null)
                for(int kk = 0; kk < CON_TEXTSIZE; kk++) con_text[kk] = ' ';
        }
        						
        /*
        ================
        Con_ClearNotify
        ================
        */
        public static void Con_ClearNotify ()
        {
	        int		i;
        	
	        for (i=0 ; i<NUM_CON_TIMES ; i++)
		        con_times[i] = 0;
        }
                						
        /*
        ================
        Con_MessageMode_f
        ================
        */
        static void Con_MessageMode_f ()
        {
        }
                						
        /*
        ================
        Con_MessageMode2_f
        ================
        */
        static void Con_MessageMode2_f ()
        {
        }
        						
        /*
        ================
        Con_CheckResize

        If the line width has changed, reformat the buffer.
        ================
        */
        public static void Con_CheckResize ()
        {
	        int		i, j, width, oldwidth, oldtotallines, numlines, numchars;
	        char[]	tbuf = new char[CON_TEXTSIZE];

	        width = (int)((screen.vid.width >> 3) - 2);

	        if (width == con_linewidth)
		        return;

	        if (width < 1)			// video hasn't been initialized yet
	        {
		        width = 38;
		        con_linewidth = width;
		        con_totallines = CON_TEXTSIZE / con_linewidth;
                for (int kk = 0; kk < CON_TEXTSIZE; kk++) con_text[kk] = ' ';
	        }
	        else
	        {
		        oldwidth = con_linewidth;
		        con_linewidth = width;
		        oldtotallines = con_totallines;
		        con_totallines = CON_TEXTSIZE / con_linewidth;
		        numlines = oldtotallines;

		        if (con_totallines < numlines)
			        numlines = con_totallines;

		        numchars = oldwidth;
        	
		        if (con_linewidth < numchars)
			        numchars = con_linewidth;

                Buffer.BlockCopy(con_text, 0, tbuf, 0, CON_TEXTSIZE);
                for (int kk = 0; kk < CON_TEXTSIZE; kk++) con_text[kk] = ' ';

		        for (i=0 ; i<numlines ; i++)
		        {
			        for (j=0 ; j<numchars ; j++)
			        {
				        con_text[(con_totallines - 1 - i) * con_linewidth + j] =
						        tbuf[((con_current - i + oldtotallines) %
							          oldtotallines) * oldwidth + j];
			        }
		        }

		        Con_ClearNotify ();
	        }

	        con_backscroll = 0;
	        con_current = con_totallines - 1;
        }
        
        /*
        ================
        Con_Init
        ================
        */
        public static void Con_Init ()
        {
            const int MAXGAMEDIRLEN = 1000;
	        string 	temp;
	        string	t2 = "/qconsole.log";

	        con_debuglog = (common.COM_CheckParm("-condebug") != 0);

            con_text = new char[CON_TEXTSIZE];
            for(int kk = 0; kk < CON_TEXTSIZE; kk++) con_text[kk] = ' ';
	        con_linewidth = -1;
	        Con_CheckResize ();
        	
	        Con_Printf ("Console initialized.\n");

            //
            // register our commands
            //
            cvar_t.Cvar_RegisterVariable(con_notifytime);

            cmd.Cmd_AddCommand("toggleconsole", Con_ToggleConsole_f);
            cmd.Cmd_AddCommand("messagemode", Con_MessageMode_f);
            cmd.Cmd_AddCommand("messagemode2", Con_MessageMode2_f);
            cmd.Cmd_AddCommand("clear", Con_Clear_f);
            con_initialized = true;
        }

        /*
        ===============
        Con_Linefeed
        ===============
        */
        static void Con_Linefeed ()
        {
            con_x = 0;
            con_current++;
            int ofs = (con_current % con_totallines) * con_linewidth;
            for (int kk = 0; kk < con_linewidth; kk++) con_text[ofs + kk] = ' ';
        }

        /*
        ================
        Con_Print

        Handles cursor positioning, line wrapping, etc
        All console printing must go through this in order to be logged to disk
        If no console is visible, the notify window will pop up.
        ================
        */
        static bool cr;
        static void Con_Print(string txt)
        {
	        int		y;
	        int		c, l;
	        int		mask;
            
            int     ofs = 0;
        	
	        con_backscroll = 0;

	        if (txt[0] == 1)
	        {
		        mask = 128;		// go to colored text
	        // play talk wav
                ofs++;
	        }
	        else if (txt[0] == 2)
	        {
		        mask = 128;		// go to colored text
                ofs++;
	        }
	        else
		        mask = 0;

	        while (ofs != txt.Length)
	        {
                c = txt[ofs];
	        // count word length
		        for (l=0 ; l< con_linewidth ; l++)
			        if ((ofs + l) == txt.Length || txt[ofs + l] <= ' ')
				        break;

	        // word wrap
		        if (l != con_linewidth && (con_x + l > con_linewidth) )
			        con_x = 0;

		        ofs++;

		        if (cr)
		        {
			        con_current--;
			        cr = false;
		        }

        		
		        if (con_x == 0)
		        {
			        Con_Linefeed ();
		        // mark time for transparent overlay
			        if (con_current >= 0)
				        con_times[con_current % NUM_CON_TIMES] = host.realtime;
		        }

		        switch (c)
		        {
		        case '\n':
			        con_x = 0;
			        break;

		        case '\r':
			        con_x = 0;
			        cr = true;
			        break;

		        default:	// display character and advance
			        y = con_current % con_totallines;
			        con_text[y*con_linewidth+con_x] = (char) (c | mask);
			        con_x++;
			        if (con_x >= con_linewidth)
				        con_x = 0;
			        break;
		        }
	        }
        }

        /*
        ================
        Con_DebugLog
        ================
        */
        static void Con_DebugLog(string file, string fmt)
        {
        }


        /*
        ================
        Con_Printf

        Handles cursor positioning, line wrapping, etc
        ================
        */
        const int	MAXPRINTMSG	= 4096;
        // FIXME: make a buffer size safe vsprintf?
        static bool inupdate;
        public static void Con_Printf(string fmt)
        {
	        string		msg = fmt;
        	
        // also echo to debugging console
	        sys_linux.Sys_Printf (msg);	// also echo to debugging console

        // log all messages to file
	        if (con_debuglog)
		        Con_DebugLog(common.com_gamedir + "/qconsole.log", msg);

	        if (!con_initialized)
		        return;
        		
		if (client.cls.state == client.cactive_t.ca_dedicated)
			return;		// no graphics mode


        // write it to the scrollable buffer
	        Con_Print (msg);
        	
        // update the screen if the console is displayed
            if (client.cls.signon != client.SIGNONS && !screen.scr_disabled_for_loading)
	        {
	        // protect against infinite loop if something in SCR_UpdateScreen calls
	        // Con_Printd
		        if (!inupdate)
		        {
			        inupdate = true;
			        screen.SCR_UpdateScreen ();
			        inupdate = false;
		        }
	        }
        }

        /*
        ================
        Con_DPrintf

        A Con_Printf that only shows up if the "developer" cvar is set
        ================
        */
        public static void Con_DPrintf (string fmt)
        {
	        string		msg;
        		
	        if (host.developer.value == 0)
		        return;			// don't confuse non-developers with techie stuff...

		    msg = fmt;

	        Con_Printf (msg);
        }
        
        /*
        ==================
        Con_SafePrintf

        Okay to call even when the screen can't be updated
        ==================
        */
        void Con_SafePrintf (string fmt)
        {
	        string		msg;
	        bool		temp;

		msg = fmt;

            temp = screen.scr_disabled_for_loading;
            screen.scr_disabled_for_loading = true;
	        Con_Printf (msg);
	        screen.scr_disabled_for_loading = temp;
        }
        
        /*
        ==============================================================================

        DRAWING

        ==============================================================================
        */


        /*
        ================
        Con_DrawInput

        The input line scrolls horizontally if typing goes beyond the right edge
        ================
        */
        static void Con_DrawInput ()
        {
	        int		y;
	        int		i;
	        string	text;
            int     ofs = 0;

            if (keys.key_dest != keys.keydest_t.key_console && !con_forcedup)
		        return;		// don't draw anything

            text = keys.key_lines[keys.edit_line];
        	
        // add the cursor frame
	        text = text.Substring(0, keys.key_linepos) + (char)(10+((int)(host.realtime*con_cursorspeed)&1));
        	
        // fill out remainder with spaces
	        for (i=keys.key_linepos+1 ; i< con_linewidth ; i++)
		        text += ' ';
        		
        //	prestep if horizontally scrolling
	        if (keys.key_linepos >= con_linewidth)
		        ofs += 1 + keys.key_linepos - con_linewidth;
        		
        // draw it
	        y = con_vislines-16;

	        for (i=0 ; i<con_linewidth ; i++)
		        draw.Draw_Character ( (i+1)<<3, con_vislines - 16, text[ofs+i]);

        // remove cursor
        }
        
        /*
        ================
        Con_DrawNotify

        Draws the last few lines of output transparently over the game top
        ================
        */
        public static void Con_DrawNotify ()
        {
	        int		x, v;
	        int	    text;
	        int		i;
	        double	time;

	        v = 0;
	        for (i= con_current-NUM_CON_TIMES+1 ; i<=con_current ; i++)
	        {
		        if (i < 0)
			        continue;
		        time = con_times[i % NUM_CON_TIMES];
		        if (time == 0)
			        continue;
		        time = host.realtime - time;
		        if (time > con_notifytime.value)
			        continue;
		        text = (i % con_totallines)*con_linewidth;
        		
		        screen.clearnotify = 0;
		        screen.scr_copytop = true;

		        for (x = 0 ; x < con_linewidth ; x++)
                    draw.Draw_Character((x + 1) << 3, v, con_text[text + x]);

		        v += 8;
	        }

	        if (v > con_notifylines)
		        con_notifylines = v;
        }

        /*
        ================
        Con_DrawConsole

        Draws the console with the solid background
        The typing input line at the bottom should only be drawn if typing is allowed
        ================
        */
        public static void Con_DrawConsole (int lines, bool drawinput)
        {
	        int				i, x, y;
	        int				rows;
	        int		        text;
	        int				j;
        	
	        if (lines <= 0)
		        return;

        // draw the background
	        draw.Draw_ConsoleBackground (lines);

        // draw the text
	        con_vislines = lines;

	        rows = (lines-16)>>3;		// rows of text to draw
	        y = lines - 16 - (rows<<3);	// may start slightly negative

	        for (i= con_current - rows + 1 ; i<=con_current ; i++, y+=8 )
	        {
		        j = i - con_backscroll;
		        if (j<0)
			        j = 0;
                text = (j % con_totallines) * con_linewidth;

		        for (x=0 ; x<con_linewidth ; x++)
			        draw.Draw_Character ( (x+1)<<3, y, con_text[text + x]);
	        }

        // draw the input prompt, user text, and cursor if desired
	        if (drawinput)
		        Con_DrawInput ();
        }


        /*
        ==================
        Con_NotifyBox
        ==================
        */
        void Con_NotifyBox (string text)
        {
	        double		t1, t2;

        // during startup for sound / cd warnings
	        Con_Printf("\n\n\u001d\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001f\n");

	        Con_Printf (text);

	        Con_Printf ("Press a key.\n");
	        Con_Printf("\u001d\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001f\n");

	        Con_Printf ("\n");
	        host.realtime = 0;				// put the cursor back to invisible
        }

    }
}
