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
    public sealed class keys
    {
        //
        // these are the key numbers that should be passed to Key_Event
        //
        public const int	K_TAB			= 9;
        public const int	K_ENTER			= 13;
        public const int	K_ESCAPE		= 27;
        public const int	K_SPACE			= 32;

        // normal keys should be passed as lowercased ascii

        public const int	K_BACKSPACE		= 127;
        public const int	K_UPARROW		= 128;
        public const int	K_DOWNARROW		= 129;
        public const int	K_LEFTARROW		= 130;
        public const int	K_RIGHTARROW	= 131;

        public const int	K_ALT			= 132;
        public const int	K_CTRL			= 133;
        public const int	K_SHIFT			= 134;
        public const int	K_F1			= 135;
        public const int	K_F2			= 136;
        public const int	K_F3			= 137;
        public const int	K_F4			= 138;
        public const int	K_F5			= 139;
        public const int	K_F6			= 140;
        public const int	K_F7			= 141;
        public const int	K_F8			= 142;
        public const int	K_F9			= 143;
        public const int	K_F10			= 144;
        public const int	K_F11			= 145;
        public const int	K_F12			= 146;
        public const int	K_INS			= 147;
        public const int	K_DEL			= 148;
        public const int	K_PGDN			= 149;
        public const int	K_PGUP			= 150;
        public const int	K_HOME			= 151;
        public const int	K_END			= 152;

        public const int K_PAUSE			= 255;

        //
        // mouse buttons generate virtual keys
        //
        public const int	K_MOUSE1		= 200;
        public const int	K_MOUSE2		= 201;
        public const int	K_MOUSE3		= 202;

        //
        // joystick buttons
        //
        public const int	K_JOY1			= 203;
        public const int	K_JOY2			= 204;
        public const int	K_JOY3			= 205;
        public const int	K_JOY4			= 206;

        //
        // aux keys are for multi-buttoned joysticks to generate so they can use
        // the normal binding process
        //
        public const int    K_AUX1          = 207;
        public const int	K_AUX2			= 208;
        public const int	K_AUX3			= 209;
        public const int	K_AUX4			= 210;
        public const int	K_AUX5			= 211;
        public const int	K_AUX6			= 212;
        public const int	K_AUX7			= 213;
        public const int	K_AUX8			= 214;
        public const int	K_AUX9			= 215;
        public const int	K_AUX10			= 216;
        public const int	K_AUX11			= 217;
        public const int	K_AUX12			= 218;
        public const int	K_AUX13			= 219;
        public const int	K_AUX14			= 220;
        public const int	K_AUX15			= 221;
        public const int	K_AUX16			= 222;
        public const int	K_AUX17			= 223;
        public const int	K_AUX18			= 224;
        public const int	K_AUX19			= 225;
        public const int	K_AUX20			= 226;
        public const int	K_AUX21			= 227;
        public const int	K_AUX22			= 228;
        public const int	K_AUX23			= 229;
        public const int	K_AUX24			= 230;
        public const int	K_AUX25			= 231;
        public const int	K_AUX26			= 232;
        public const int	K_AUX27			= 233;
        public const int	K_AUX28			= 234;
        public const int	K_AUX29			= 235;
        public const int	K_AUX30			= 236;
        public const int	K_AUX31			= 237;
        public const int	K_AUX32			= 238;

        // JACK: Intellimouse(c) Mouse Wheel Support

        public const int K_MWHEELUP		    = 239;
        public const int K_MWHEELDOWN	    = 240;

        public enum keydest_t {key_game, key_console, key_message, key_menu};

        /*

        key up events are sent even if in console mode

        */
        
        public const int	    MAXCMDLINE	= 256;
        public static string[]  key_lines = new string[32];
        public static int		key_linepos;

        public static int		edit_line=0;
        static int		    history_line=0;

        public static keydest_t	key_dest;

        static int		    key_count;			// incremented every key event

        static string[]	    keybindings = new string[256];
        static bool[]	    consolekeys = new bool[256];	// if true, can't be rebound while in console

        class keyname_t
        {
	        public string   name;
            public int      keynum;

            public keyname_t(string name, int keynum)
            {
                this.name = name;
                this.keynum = keynum;
            }
        };

        static keyname_t[] keynames =
        {
	        new keyname_t("TAB", K_TAB),
	        new keyname_t("ENTER", K_ENTER),
	        new keyname_t("ESCAPE", K_ESCAPE),
	        new keyname_t("SPACE", K_SPACE),
	        new keyname_t("BACKSPACE", K_BACKSPACE),
	        new keyname_t("UPARROW", K_UPARROW),
	        new keyname_t("DOWNARROW", K_DOWNARROW),
	        new keyname_t("LEFTARROW", K_LEFTARROW),
	        new keyname_t("RIGHTARROW", K_RIGHTARROW),

	        new keyname_t("ALT", K_ALT),
	        new keyname_t("CTRL", K_CTRL),
	        new keyname_t("SHIFT", K_SHIFT),
        	
	        new keyname_t("F1", K_F1),
	        new keyname_t("F2", K_F2),
	        new keyname_t("F3", K_F3),
	        new keyname_t("F4", K_F4),
	        new keyname_t("F5", K_F5),
	        new keyname_t("F6", K_F6),
	        new keyname_t("F7", K_F7),
	        new keyname_t("F8", K_F8),
	        new keyname_t("F9", K_F9),
	        new keyname_t("F10", K_F10),
	        new keyname_t("F11", K_F11),
	        new keyname_t("F12", K_F12),

	        new keyname_t("INS", K_INS),
	        new keyname_t("DEL", K_DEL),
	        new keyname_t("PGDN", K_PGDN),
	        new keyname_t("PGUP", K_PGUP),
	        new keyname_t("HOME", K_HOME),
	        new keyname_t("END", K_END),

	        new keyname_t("MOUSE1", K_MOUSE1),
	        new keyname_t("MOUSE2", K_MOUSE2),
	        new keyname_t("MOUSE3", K_MOUSE3),

	        new keyname_t("JOY1", K_JOY1),
	        new keyname_t("JOY2", K_JOY2),
	        new keyname_t("JOY3", K_JOY3),
	        new keyname_t("JOY4", K_JOY4),

	        new keyname_t("AUX1", K_AUX1),
	        new keyname_t("AUX2", K_AUX2),
	        new keyname_t("AUX3", K_AUX3),
	        new keyname_t("AUX4", K_AUX4),
	        new keyname_t("AUX5", K_AUX5),
	        new keyname_t("AUX6", K_AUX6),
	        new keyname_t("AUX7", K_AUX7),
	        new keyname_t("AUX8", K_AUX8),
	        new keyname_t("AUX9", K_AUX9),
	        new keyname_t("AUX10", K_AUX10),
	        new keyname_t("AUX11", K_AUX11),
	        new keyname_t("AUX12", K_AUX12),
	        new keyname_t("AUX13", K_AUX13),
	        new keyname_t("AUX14", K_AUX14),
	        new keyname_t("AUX15", K_AUX15),
	        new keyname_t("AUX16", K_AUX16),
	        new keyname_t("AUX17", K_AUX17),
	        new keyname_t("AUX18", K_AUX18),
	        new keyname_t("AUX19", K_AUX19),
	        new keyname_t("AUX20", K_AUX20),
	        new keyname_t("AUX21", K_AUX21),
	        new keyname_t("AUX22", K_AUX22),
	        new keyname_t("AUX23", K_AUX23),
	        new keyname_t("AUX24", K_AUX24),
	        new keyname_t("AUX25", K_AUX25),
	        new keyname_t("AUX26", K_AUX26),
	        new keyname_t("AUX27", K_AUX27),
	        new keyname_t("AUX28", K_AUX28),
	        new keyname_t("AUX29", K_AUX29),
	        new keyname_t("AUX30", K_AUX30),
	        new keyname_t("AUX31", K_AUX31),
	        new keyname_t("AUX32", K_AUX32),

	        new keyname_t("PAUSE", K_PAUSE),

	        new keyname_t("MWHEELUP", K_MWHEELUP),
	        new keyname_t("MWHEELDOWN", K_MWHEELDOWN),

	        new keyname_t("SEMICOLON", ';'),	// because a raw semicolon seperates commands

	        new keyname_t(null,0)
        };

        /*
        ==============================================================================

			        LINE TYPING INTO THE CONSOLE

        ==============================================================================
        */


        /*
        ====================
        Key_Console

        Interactive line editing and console scrollback
        ====================
        */
        static void Key_Console (int key)
        {
	        string	cmd;
        	
	        if (key == K_ENTER)
	        {
		        quake.cmd.Cbuf_AddText (key_lines[edit_line].Substring(1));	// skip the >
                quake.cmd.Cbuf_AddText("\n");
		        console.Con_Printf (key_lines[edit_line] + "\n");
		        edit_line = (edit_line + 1) & 31;
		        history_line = edit_line;
		        key_lines[edit_line] = "]";
		        key_linepos = 1;
		        if (client.cls.state == client.cactive_t.ca_disconnected)
			        screen.SCR_UpdateScreen ();	// force an update, because the command
									        // may take some time
		        return;
	        }

	        if (key == K_TAB)
	        {	// command completion
		        cmd = quake.cmd.Cmd_CompleteCommand (key_lines[edit_line].Substring(1));
		        if (cmd == null)
			        cmd = cvar_t.Cvar_CompleteVariable (key_lines[edit_line]+1);
		        if (cmd != null)
		        {
			        key_lines[edit_line] = key_lines[edit_line].Substring(0,1) + cmd;
			        key_linepos = cmd.Length+1;
			        key_lines[edit_line] += ' ';
			        key_linepos++;
			        return;
		        }
	        }
        	
	        if (key == K_BACKSPACE || key == K_LEFTARROW)
	        {
		        if (key_linepos > 1)
			        key_linepos--;
		        return;
	        }

	        if (key == K_UPARROW)
	        {
		        do
		        {
			        history_line = (history_line - 1) & 31;
		        } while (history_line != edit_line
				        && key_lines[history_line].Length == 1);
		        if (history_line == edit_line)
			        history_line = (edit_line+1)&31;
		        key_lines[edit_line] = key_lines[history_line];
		        key_linepos = key_lines[edit_line].Length;
		        return;
	        }

	        if (key == K_DOWNARROW)
	        {
		        if (history_line == edit_line) return;
		        do
		        {
			        history_line = (history_line + 1) & 31;
		        }
		        while (history_line != edit_line
			        && key_lines[history_line].Length == 1);
		        if (history_line == edit_line)
		        {
			        key_lines[edit_line] = "]";
			        key_linepos = 1;
		        }
		        else
		        {
			        key_lines[edit_line] = key_lines[history_line];
			        key_linepos = key_lines[edit_line].Length;
		        }
		        return;
	        }

	        if (key == K_PGUP || key==K_MWHEELUP)
	        {
		        console.con_backscroll += 2;
                if (console.con_backscroll > console.con_totallines - (screen.vid.height >> 3) - 1)
                    console.con_backscroll = (int)(console.con_totallines - (screen.vid.height >> 3) - 1);
		        return;
	        }

	        if (key == K_PGDN || key==K_MWHEELDOWN)
	        {
		        console.con_backscroll -= 2;
                if (console.con_backscroll < 0)
                    console.con_backscroll = 0;
		        return;
	        }

	        if (key == K_HOME)
	        {
                console.con_backscroll = (int)(console.con_totallines - (screen.vid.height >> 3) - 1);
		        return;
	        }

	        if (key == K_END)
	        {
                console.con_backscroll = 0;
		        return;
	        }
        	
	        if (key < 32 || key > 127)
		        return;	// non printable
        		
	        if (key_linepos < MAXCMDLINE-1)
	        {
		        key_lines[edit_line] = key_lines[edit_line].Substring(0, key_linepos) + (char)key;
		        key_linepos++;
	        }
        }

        //============================================================================

        static void Key_Message (int key)
        {
        }

        //============================================================================

        /*
        ===================
        Key_StringToKeynum

        Returns a key number to be used to index keybindings[] by looking at
        the given string.  Single ascii characters return themselves, while
        the K_* names are matched up.
        ===================
        */
        static int Key_StringToKeynum (string str)
        {
            int kn;

            if (str == null || str.Length == 0)
                return -1;
            if (str.Length == 1)
                return str[0];

            for (kn = 0; keynames[kn].name != null; kn++)
            {
                if (str.CompareTo(keynames[kn].name) == 0)
                    return keynames[kn].keynum;
            }
            return -1;
        }

        /*
        ===================
        Key_KeynumToString

        Returns a string (either a single ascii char, or a K_* name) for the
        given keynum.
        FIXME: handle quote special (general escape sequence?)
        ===================
        */
        string Key_KeynumToString (int keynum)
        {
            return null;
        }
        
        /*
        ===================
        Key_SetBinding
        ===================
        */
        static void Key_SetBinding (int keynum, string binding)
        {
	        string	@new;
	        int		l;
        			
	        if (keynum == -1)
		        return;

        // free old bindings
	        if (keybindings[keynum] != null)
		        keybindings[keynum] = null;
        			
        // allocate memory for new binding
	        l = binding.Length;
	        @new = binding;
	        keybindings[keynum] = @new;
        }

        /*
        ===================
        Key_Unbind_f
        ===================
        */
        static void Key_Unbind_f()
        {
        }

        static void Key_Unbindall_f()
        {
        }

        /*
        ===================
        Key_Bind_f
        ===================
        */
        static void Key_Bind_f ()
        {
	        int			i, c, b;
	        string		cmd = new string(new char[1024]);
        	
	        c = quake.cmd.Cmd_Argc();

	        if (c != 2 && c != 3)
	        {
		        console.Con_Printf ("bind <key> [command] : attach a command to a key\n");
		        return;
	        }
            b = Key_StringToKeynum(quake.cmd.Cmd_Argv(1));
	        if (b==-1)
	        {
                console.Con_Printf("\"" + quake.cmd.Cmd_Argv(1) + "\" isn't a valid key\n");
		        return;
	        }

	        if (c == 2)
	        {
		        if (keybindings[b] != null)
                    console.Con_Printf("\"" + quake.cmd.Cmd_Argv(1) + "\" = \"" + keybindings[b] + "\"\n");
		        else
                    console.Con_Printf("\"" + quake.cmd.Cmd_Argv(1) + "\" is not bound\n");
		        return;
	        }
        	
        // copy the rest of the command line
	        cmd = "";		// start out with a null string
	        for (i=2 ; i< c ; i++)
	        {
		        if (i > 2)
			        cmd += " ";
                cmd += quake.cmd.Cmd_Argv(i);
	        }

	        Key_SetBinding (b, cmd);
        }

        /*
        ===================
        Key_Init
        ===================
        */
        public static void Key_Init ()
        {
	        int		i;

	        for (i=0 ; i<32 ; i++)
	        {
		        key_lines[i] = "]";
	        }
	        key_linepos = 1;

        //
        // init ascii characters in console mode
        //
	        for (i=32 ; i<128 ; i++)
		        consolekeys[i] = true;
	        consolekeys[K_ENTER] = true;
	        consolekeys[K_TAB] = true;
	        consolekeys[K_LEFTARROW] = true;
	        consolekeys[K_RIGHTARROW] = true;
	        consolekeys[K_UPARROW] = true;
	        consolekeys[K_DOWNARROW] = true;
	        consolekeys[K_BACKSPACE] = true;
	        consolekeys[K_PGUP] = true;
	        consolekeys[K_PGDN] = true;
	        consolekeys[K_SHIFT] = true;
	        consolekeys[K_MWHEELUP] = true;
	        consolekeys[K_MWHEELDOWN] = true;
	        consolekeys['`'] = false;
	        consolekeys['~'] = false;

        //
        // register our functions
        //
	        cmd.Cmd_AddCommand ("bind",Key_Bind_f);
            cmd.Cmd_AddCommand("unbind", Key_Unbind_f);
            cmd.Cmd_AddCommand("unbindall", Key_Unbindall_f);
        }

        /*
        ===================
        Key_Event

        Called by the system between frames for both key up and key down events
        Should NOT be called during an interrupt!
        ===================
        */
        public static void Key_Event (int key, bool down)
        {
            string  kb;
        	string  cmd = new string(new char[1024]);
            
            key_count++;
	        if (key_count <= 0)
	        {
		        return;		// just catching keys for Con_NotifyBox
	        }

        //
        // handle escape specialy, so the user can never unbind it
        //
	        if (key == K_ESCAPE)
	        {
		        if (!down)
			        return;
		        switch (key_dest)
		        {
                case keydest_t.key_message:
			        Key_Message (key);
			        break;
                case keydest_t.key_menu:
                    menu.M_Keydown(key);
			        break;
                case keydest_t.key_game:
                case keydest_t.key_console:
			        menu.M_ToggleMenu_f ();
			        break;
		        default:
			        sys_linux.Sys_Error ("Bad key_dest");
                    break;
		        }
		        return;
	        }

        //
        // key up events only generate commands if the game key binding is
        // a button command (leading + sign).  These will occur even in console mode,
        // to keep the character from continuing an action started before a console
        // switch.  Button commands include the kenum as a parameter, so multiple
        // downs can be matched with ups
        //
            if (!down)
            {
                kb = keybindings[key];
                if (kb != null && kb[0] == '+')
                {
                    cmd = "-" + kb.Substring(1) + " " + key + "\n";
                    quake.cmd.Cbuf_AddText(cmd);
                }
                return;
            }

        //
        // during demo playback, most keys bring up the main menu
        //
	        if (client.cls.demoplayback && down && consolekeys[key] && key_dest == keydest_t.key_game)
	        {
		        menu.M_ToggleMenu_f ();
		        return;
	        }

        //
        // if not a consolekey, send to the interpreter no matter what mode is
        //
            if ((key_dest == keydest_t.key_console && !consolekeys[key])
            || (key_dest == keydest_t.key_game && (!console.con_forcedup || !consolekeys[key])))
            {
                kb = keybindings[key];
                if (kb != null)
                {
                    if (kb[0] == '+')
                    {	// button commands add keynum as a parm
                        cmd = kb + " " + key + "\n";
                        quake.cmd.Cbuf_AddText(cmd);
                    }
                    else
                    {
                        quake.cmd.Cbuf_AddText(kb);
                        quake.cmd.Cbuf_AddText("\n");
                    }
                }
                return;
            }

	        if (!down)
		        return;		// other systems only care about key down events

	        switch (key_dest)
	        {
            case keydest_t.key_message:
		        Key_Message (key);
		        break;
            case keydest_t.key_menu:
		        menu.M_Keydown (key);
		        break;
            case keydest_t.key_game:
            case keydest_t.key_console:
		        Key_Console (key);
		        break;
	        default:
		        sys_linux.Sys_Error ("Bad key_dest");
                break;
	        }
        }
        
        /*
        ===================
        Key_ClearStates
        ===================
        */
        void Key_ClearStates ()
        {
        }
    }
}
