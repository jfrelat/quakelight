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
    public sealed class menu
    {
        enum m_state_t {m_none, m_main, m_singleplayer, m_load, m_save, m_multiplayer, m_setup, m_net, m_options, m_video, m_keys, m_help, m_quit, m_serialconfig, m_modemconfig, m_lanconfig, m_gameoptions, m_search, m_slist};
        static m_state_t m_state;

        static bool	    m_entersound;		// play after drawing a frame, so caching
								        // won't disrupt the sound
        static bool	    m_recursiveDraw;

        int			m_return_state;
        bool	    m_return_onerror;
        string		m_return_reason;

        /*
        ================
        M_DrawCharacter

        Draws one solid graphics character
        ================
        */
        static void M_DrawCharacter (int cx, int line, int num)
        {
            draw.Draw_Character((int)(cx + ((screen.vid.width - 320) >> 1)), line, num);
        }

        static void M_Print (int cx, int cy, string str)
        {
            for(int i = 0; i < str.Length; i++)
            {
                M_DrawCharacter(cx, cy, str[i] + 128);
                cx += 8;
            }
        }

        static void M_PrintWhite (int cx, int cy, string str)
        {
            for(int i = 0; i < str.Length; i++)
            {
                M_DrawCharacter(cx, cy, str[i]);
                cx += 8;
            }
        }

        static void M_DrawTransPic (int x, int y, wad.qpic_t pic)
        {
	        draw.Draw_TransPic ((int)(x + ((screen.vid.width - 320)>>1)), y, pic);
        }

        static void M_DrawPic (int x, int y, wad.qpic_t pic)
        {
            draw.Draw_Pic((int)(x + ((screen.vid.width - 320) >> 1)), y, pic);
        }

        static byte[] identityTable = new byte[256];
        static byte[] translationTable = new byte[256];

        static void M_BuildTranslationTable(int top, int bottom)
        {
	        int		j;
	        byte[]	dest, source;

	        for (j = 0; j < 256; j++)
		        identityTable[j] = (byte)j;
	        dest = translationTable;
	        source = identityTable;
            Buffer.BlockCopy(source, 0, dest, 0, 256);

	        if (top < 128)	// the artists made some backwards ranges.  sigh.
                Buffer.BlockCopy(source, top, dest, render.TOP_RANGE, 16);
	        else
		        for (j=0 ; j<16 ; j++)
			        dest[render.TOP_RANGE+j] = source[top+15-j];

	        if (bottom < 128)
                Buffer.BlockCopy(source, bottom, dest, render.BOTTOM_RANGE, 16);
	        else
		        for (j=0 ; j<16 ; j++)
                    dest[render.BOTTOM_RANGE + j] = source[bottom + 15 - j];
        }

        static void M_DrawTransPicTranslate(int x, int y, wad.qpic_t pic)
        {
	        draw.Draw_TransPicTranslate ((int)(x + ((screen.vid.width - 320)>>1)), y, pic, translationTable);
        }

        static void M_DrawTextBox (int x, int y, int width, int lines)
        {
	        wad.qpic_t	p;
	        int		cx, cy;
	        int		n;

	        // draw left side
	        cx = x;
	        cy = y;
	        p = draw.Draw_CachePic ("gfx/box_tl.lmp");
	        M_DrawTransPic (cx, cy, p);
            p = draw.Draw_CachePic("gfx/box_ml.lmp");
	        for (n = 0; n < lines; n++)
	        {
		        cy += 8;
		        M_DrawTransPic (cx, cy, p);
	        }
            p = draw.Draw_CachePic("gfx/box_bl.lmp");
	        M_DrawTransPic (cx, cy+8, p);

	        // draw middle
	        cx += 8;
	        while (width > 0)
	        {
		        cy = y;
                p = draw.Draw_CachePic("gfx/box_tm.lmp");
		        M_DrawTransPic (cx, cy, p);
                p = draw.Draw_CachePic("gfx/box_mm.lmp");
		        for (n = 0; n < lines; n++)
		        {
			        cy += 8;
			        if (n == 1)
                        p = draw.Draw_CachePic("gfx/box_mm2.lmp");
			        M_DrawTransPic (cx, cy, p);
		        }
                p = draw.Draw_CachePic("gfx/box_bm.lmp");
		        M_DrawTransPic (cx, cy+8, p);
		        width -= 2;
		        cx += 16;
	        }

	        // draw right side
	        cy = y;
            p = draw.Draw_CachePic("gfx/box_tr.lmp");
	        M_DrawTransPic (cx, cy, p);
            p = draw.Draw_CachePic("gfx/box_mr.lmp");
	        for (n = 0; n < lines; n++)
	        {
		        cy += 8;
		        M_DrawTransPic (cx, cy, p);
	        }
            p = draw.Draw_CachePic("gfx/box_br.lmp");
	        M_DrawTransPic (cx, cy+8, p);
        }

        //=============================================================================

        int m_save_demonum;

        /*
        ================
        M_ToggleMenu_f
        ================
        */
        public static void M_ToggleMenu_f ()
        {
            if (keys.key_dest == keys.keydest_t.key_menu)
            {
                if (m_state != m_state_t.m_main)
                {
                    M_Menu_Main_f();
                    return;
                }
                keys.key_dest = keys.keydest_t.key_game;
                m_state = m_state_t.m_none;
                return;
            }
            if (keys.key_dest == keys.keydest_t.key_console)
            {
                console.Con_ToggleConsole_f();
            }
            else
            {
                M_Menu_Main_f();
            }
        }

        //=============================================================================
        /* MAIN MENU */

        static int	m_main_cursor;
        const int	MAIN_ITEMS = 5;
        
        public static void M_Menu_Main_f ()
        {
            if (keys.key_dest != keys.keydest_t.key_menu)
            {
                client.cls.demonum = -1;
            }
            keys.key_dest = keys.keydest_t.key_menu;
            m_state = m_state_t.m_main;
        }
        
        static void M_Main_Draw ()
        {
	        int		    f;
	        wad.qpic_t	p;

            M_DrawTransPic(16, 4, draw.Draw_CachePic("gfx/qplaque.lmp"));
	        p = draw.Draw_CachePic ("gfx/ttl_main.lmp");
	        M_DrawPic ( (320-p.width)/2, 4, p);
            M_DrawTransPic(72, 32, draw.Draw_CachePic("gfx/mainmenu.lmp"));

	        f = (int)(host.host_time * 10)%6;

            M_DrawTransPic(54, 32 + m_main_cursor * 20, draw.Draw_CachePic("gfx/menudot" + (f+1) + ".lmp"));
        }

        static void M_Main_Key (int key)
        {
            switch (key)
            {
                case keys.K_ESCAPE:
                    keys.key_dest = keys.keydest_t.key_game;
                    m_state = m_state_t.m_none;
                    //cls.demonum = m_save_demonum;
                    if (client.cls.demonum != -1 && !client.cls.demoplayback && client.cls.state != client.cactive_t.ca_connected)
                        client.CL_NextDemo();
                    break;

                case keys.K_DOWNARROW:
                    sound.S_LocalSound("misc/menu1.wav");
                    if (++m_main_cursor >= MAIN_ITEMS)
                        m_main_cursor = 0;
                    break;

                case keys.K_UPARROW:
                    sound.S_LocalSound("misc/menu1.wav");
                    if (--m_main_cursor < 0)
                        m_main_cursor = MAIN_ITEMS - 1;
                    break;

                case keys.K_ENTER:
                    switch (m_main_cursor)
                    {
                        case 0:
                            M_Menu_SinglePlayer_f();
                            break;

                        case 1:
                            M_Menu_MultiPlayer_f();
                            break;

                        case 2:
                            M_Menu_Options_f();
                            break;

                        case 3:
                            M_Menu_Help_f();
                            break;

                        case 4:
                            M_Menu_Quit_f();
                            break;
                    }
                    break;
            }
        }

        //=============================================================================
        /* SINGLE PLAYER MENU */

        static int	m_singleplayer_cursor;
        const int	SINGLEPLAYER_ITEMS = 3;
        
        static void M_Menu_SinglePlayer_f ()
        {
            keys.key_dest = keys.keydest_t.key_menu;
            m_state = m_state_t.m_singleplayer;
        }

        static void M_SinglePlayer_Draw ()
        {
	        int		    f;
	        wad.qpic_t	p;

	        M_DrawTransPic (16, 4, draw.Draw_CachePic ("gfx/qplaque.lmp") );
            p = draw.Draw_CachePic("gfx/ttl_sgl.lmp");
	        M_DrawPic ( (320-p.width)/2, 4, p);
            M_DrawTransPic(72, 32, draw.Draw_CachePic("gfx/sp_menu.lmp"));

	        f = (int)(host.host_time * 10)%6;

            M_DrawTransPic(54, 32 + m_singleplayer_cursor * 20, draw.Draw_CachePic("gfx/menudot" + (f + 1) + ".lmp"));
        }

        static void M_SinglePlayer_Key (int key)
        {
            switch (key)
            {
                case keys.K_ESCAPE:
                    M_Menu_Main_f();
                    break;

                case keys.K_DOWNARROW:
                    if (++m_singleplayer_cursor >= SINGLEPLAYER_ITEMS)
                        m_singleplayer_cursor = 0;
                    break;

                case keys.K_UPARROW:
                    if (--m_singleplayer_cursor < 0)
                        m_singleplayer_cursor = SINGLEPLAYER_ITEMS - 1;
                    break;

                case keys.K_ENTER:
                    switch (m_singleplayer_cursor)
                    {
                        case 0:
                            keys.key_dest = keys.keydest_t.key_game;
                            cmd.Cbuf_AddText("maxplayers 1\n");
                            cmd.Cbuf_AddText("map start\n");
                            break;

                        case 1:
                            M_Menu_Load_f();
                            break;

                        case 2:
                            M_Menu_Save_f();
                            break;
                    }
                    break;
            }
        }

        //=============================================================================
        /* LOAD/SAVE MENU */

        static int		load_cursor;		// 0 < load_cursor < MAX_SAVEGAMES

        const int	MAX_SAVEGAMES = 12;
        static string[]	m_filenames = new string[MAX_SAVEGAMES];
        static bool[]	loadable = new bool[MAX_SAVEGAMES];

        static void M_ScanSaves ()
        {
	        int		i, j;
	        string	name;
	        int		version;

	        for (i=0 ; i<MAX_SAVEGAMES ; i++)
	        {
		        m_filenames[i] = "--- UNUSED SLOT ---";
		        loadable[i] = false;
		        name = common.com_gamedir + "/s" + i + ".sav";
	        }
        }

        static void M_Menu_Load_f ()
        {
            m_entersound = true;
            m_state = m_state_t.m_load;
            keys.key_dest = keys.keydest_t.key_menu;
            M_ScanSaves();
        }
        
        static void M_Menu_Save_f ()
        {
            m_entersound = true;
            m_state = m_state_t.m_save;
            keys.key_dest = keys.keydest_t.key_menu;
            M_ScanSaves();
        }
        
        static void M_Load_Draw ()
        {
	        int		    i;
	        wad.qpic_t	p;

	        p = draw.Draw_CachePic ("gfx/p_load.lmp");
	        M_DrawPic ( (320-p.width)/2, 4, p);

	        for (i=0 ; i< MAX_SAVEGAMES; i++)
		        M_Print (16, 32 + 8*i, m_filenames[i]);

        // line cursor
	        M_DrawCharacter (8, 32 + load_cursor*8, 12+((int)(host.realtime*4)&1));
        }
        
        static void M_Save_Draw ()
        {
	        int		    i;
	        wad.qpic_t	p;

	        p = draw.Draw_CachePic ("gfx/p_save.lmp");
	        M_DrawPic ( (320-p.width)/2, 4, p);

	        for (i=0 ; i<MAX_SAVEGAMES ; i++)
		        M_Print (16, 32 + 8*i, m_filenames[i]);

        // line cursor
	        M_DrawCharacter (8, 32 + load_cursor*8, 12+((int)(host.realtime*4)&1));
        }

        static void M_Load_Key (int k)
        {
            switch (k)
            {
                case keys.K_ESCAPE:
                    M_Menu_SinglePlayer_f();
                    break;

                case keys.K_ENTER:
                    sound.S_LocalSound ("misc/menu2.wav");
                    if (!loadable[load_cursor])
                        return;
                    m_state = m_state_t.m_none;
                    keys.key_dest = keys.keydest_t.key_game;

                    // Host_Loadgame_f can't bring up the loading plaque because too much
                    // stack space has been used, so do it now
                    screen.SCR_BeginLoadingPlaque();

                    // issue the load command
                    cmd.Cbuf_AddText("load s" + load_cursor + "\n");
                    return;

                case keys.K_UPARROW:
                case keys.K_LEFTARROW:
                    load_cursor--;
                    if (load_cursor < 0)
                        load_cursor = MAX_SAVEGAMES - 1;
                    break;

                case keys.K_DOWNARROW:
                case keys.K_RIGHTARROW:
                    load_cursor++;
                    if (load_cursor >= MAX_SAVEGAMES)
                        load_cursor = 0;
                    break;
            }
        }

        static void M_Save_Key (int k)
        {
            switch (k)
            {
                case keys.K_ESCAPE:
                    M_Menu_SinglePlayer_f();
                    break;

                case keys.K_ENTER:
                    m_state = m_state_t.m_none;
                    keys.key_dest = keys.keydest_t.key_game;
                    cmd.Cbuf_AddText("save s" + load_cursor + "\n");
                    return;

                case keys.K_UPARROW:
                case keys.K_LEFTARROW:
                    load_cursor--;
                    if (load_cursor < 0)
                        load_cursor = MAX_SAVEGAMES - 1;
                    break;

                case keys.K_DOWNARROW:
                case keys.K_RIGHTARROW:
                    load_cursor++;
                    if (load_cursor >= MAX_SAVEGAMES)
                        load_cursor = 0;
                    break;
            }
        }

        //=============================================================================
        /* MULTIPLAYER MENU */

        static int	m_multiplayer_cursor;
        const int	MULTIPLAYER_ITEMS = 3;
        
        static void M_Menu_MultiPlayer_f ()
        {
            keys.key_dest = keys.keydest_t.key_menu;
            m_state = m_state_t.m_multiplayer;
            m_entersound = true;
        }
        
        static void M_MultiPlayer_Draw ()
        {
	        int		    f;
	        wad.qpic_t	p;

	        M_DrawTransPic (16, 4, draw.Draw_CachePic ("gfx/qplaque.lmp") );
            p = draw.Draw_CachePic("gfx/p_multi.lmp");
	        M_DrawPic ( (320-p.width)/2, 4, p);
            M_DrawTransPic(72, 32, draw.Draw_CachePic("gfx/mp_menu.lmp"));

	        f = (int)(host.host_time * 10)%6;

            M_DrawTransPic(54, 32 + m_multiplayer_cursor * 20, draw.Draw_CachePic("gfx/menudot" + (f + 1) + ".lmp"));

	        M_PrintWhite ((320/2) - ((27*8)/2), 148, "No Communications Available");
        }

        static void M_MultiPlayer_Key (int key)
        {
            switch (key)
            {
                case keys.K_ESCAPE:
                    M_Menu_Main_f();
                    break;

                case keys.K_DOWNARROW:
                    if (++m_multiplayer_cursor >= MULTIPLAYER_ITEMS)
                        m_multiplayer_cursor = 0;
                    break;

                case keys.K_UPARROW:
                    if (--m_multiplayer_cursor < 0)
                        m_multiplayer_cursor = MULTIPLAYER_ITEMS - 1;
                    break;

                case keys.K_ENTER:
                    m_entersound = true;
                    switch (m_multiplayer_cursor)
                    {
                        case 0:
                            break;

                        case 1:
                            break;

                        case 2:
                            M_Menu_Setup_f();
                            break;
                    }
                    break;
            }
        }

        //=============================================================================
        /* SETUP MENU */

        static int		setup_cursor = 4;
        static int[]	setup_cursor_table = {40, 56, 80, 104, 140};

        static string	setup_hostname;
        static string	setup_myname;
        static int		setup_oldtop;
        static int		setup_oldbottom;
        static int		setup_top;
        static int		setup_bottom;

        const int	NUM_SETUP_CMDS = 5;

        static void M_Menu_Setup_f ()
        {
	        keys.key_dest = keys.keydest_t.key_menu;
	        m_state = m_state_t.m_setup;
	        m_entersound = true;
	        setup_top = setup_oldtop = ((int)client.cl_color.value) >> 4;
            setup_bottom = setup_oldbottom = ((int)client.cl_color.value) & 15;
        }
        
        static void M_Setup_Draw ()
        {
	        wad.qpic_t	p;

	        M_DrawTransPic (16, 4, draw.Draw_CachePic ("gfx/qplaque.lmp") );
            p = draw.Draw_CachePic("gfx/p_multi.lmp");
	        M_DrawPic ( (320-p.width)/2, 4, p);

	        M_Print (64, 40, "Hostname");
	        M_DrawTextBox (160, 32, 16, 1);
	        //M_Print (168, 40, setup_hostname);

	        M_Print (64, 56, "Your name");
	        M_DrawTextBox (160, 48, 16, 1);
	        //M_Print (168, 56, setup_myname);

	        M_Print (64, 80, "Shirt color");
	        M_Print (64, 104, "Pants color");

	        M_DrawTextBox (64, 140-8, 14, 1);
	        M_Print (72, 140, "Accept Changes");

            p = draw.Draw_CachePic("gfx/bigbox.lmp");
	        M_DrawTransPic (160, 64, p);
            p = draw.Draw_CachePic("gfx/menuplyr.lmp");
	        M_BuildTranslationTable(setup_top*16, setup_bottom*16);
	        M_DrawTransPicTranslate (172, 72, p);

	        M_DrawCharacter (56, setup_cursor_table [setup_cursor], 12+((int)(host.realtime*4)&1));

/*	        if (setup_cursor == 0)
		        M_DrawCharacter (168 + 8*strlen(setup_hostname), setup_cursor_table [setup_cursor], 10+((int)(realtime*4)&1));

	        if (setup_cursor == 1)
		        M_DrawCharacter (168 + 8*strlen(setup_myname), setup_cursor_table [setup_cursor], 10+((int)(realtime*4)&1));*/
        }

        static void M_Setup_Key (int k)
        {
	        int			l;

	        switch (k)
	        {
	        case keys.K_ESCAPE:
		        M_Menu_MultiPlayer_f ();
		        break;

	        case keys.K_UPARROW:
		        setup_cursor--;
		        if (setup_cursor < 0)
			        setup_cursor = NUM_SETUP_CMDS-1;
		        break;

	        case keys.K_DOWNARROW:
		        setup_cursor++;
		        if (setup_cursor >= NUM_SETUP_CMDS)
			        setup_cursor = 0;
		        break;

	        case keys.K_LEFTARROW:
		        if (setup_cursor < 2)
			        return;
		        sound.S_LocalSound ("misc/menu3.wav");
		        if (setup_cursor == 2)
			        setup_top = setup_top - 1;
		        if (setup_cursor == 3)
			        setup_bottom = setup_bottom - 1;
		        break;
	        case keys.K_RIGHTARROW:
		        if (setup_cursor < 2)
			        return;
        forward:
                sound.S_LocalSound("misc/menu3.wav");
                if (setup_cursor == 2)
			        setup_top = setup_top + 1;
		        if (setup_cursor == 3)
			        setup_bottom = setup_bottom + 1;
		        break;

	        case keys.K_ENTER:
		        if (setup_cursor == 0 || setup_cursor == 1)
			        return;

		        if (setup_cursor == 2 || setup_cursor == 3)
			        goto forward;

		        m_entersound = true;
		        M_Menu_MultiPlayer_f ();
		        break;
	        }

	        if (setup_top > 13)
		        setup_top = 0;
	        if (setup_top < 0)
		        setup_top = 13;
	        if (setup_bottom > 13)
		        setup_bottom = 0;
	        if (setup_bottom < 0)
		        setup_bottom = 13;
        }

        //=============================================================================
        /* NET MENU */

        int	m_net_cursor;
        int m_net_items;
        int m_net_saveHeight;

        string[] net_helpMessage =
        {
        /* .........1.........2.... */
          "                        ",
          " Two computers connected",
          "   through two modems.  ",
          "                        ",

          "                        ",
          " Two computers connected",
          " by a null-modem cable. ",
          "                        ",

          " Novell network LANs    ",
          " or Windows 95 DOS-box. ",
          "                        ",
          "(LAN=Local Area Network)",

          " Commonly used to play  ",
          " over the Internet, but ",
          " also used on a Local   ",
          " Area Network.          "
        };

        void M_Menu_Net_f ()
        {
        }
        
        static void M_Net_Draw ()
        {
        }

        static void M_Net_Key (int k)
        {
        }

        //=============================================================================
        /* OPTIONS MENU */

        const int	OPTIONS_ITEMS = 13;

        const int	SLIDER_RANGE = 10;

        static int		options_cursor;

        static void M_Menu_Options_f ()
        {
	        keys.key_dest = keys.keydest_t.key_menu;
	        m_state = m_state_t.m_options;
	        m_entersound = true;
        }
        
        static void M_AdjustSliders (int dir)
        {
	        switch (options_cursor)
	        {
	        case 3:	// screen size
		        screen.scr_viewsize.value += dir * 10;
		        if (screen.scr_viewsize.value < 30)
			        screen.scr_viewsize.value = 30;
		        if (screen.scr_viewsize.value > 120)
			        screen.scr_viewsize.value = 120;
		        cvar_t.Cvar_SetValue ("viewsize", screen.scr_viewsize.value);
		        break;
	        case 4:	// gamma
                view.v_gamma.value -= dir * 0.05;
                if (view.v_gamma.value < 0.5)
                    view.v_gamma.value = 0.5;
                if (view.v_gamma.value > 1)
                    view.v_gamma.value = 1;
                cvar_t.Cvar_SetValue("gamma", view.v_gamma.value);
		        break;
	        }
        }

        static void M_DrawSlider (int x, int y, double range)
        {
	        int	i;

	        if (range < 0)
		        range = 0;
	        if (range > 1)
		        range = 1;
	        M_DrawCharacter (x-8, y, 128);
	        for (i=0 ; i<SLIDER_RANGE ; i++)
		        M_DrawCharacter (x + i*8, y, 129);
	        M_DrawCharacter (x+i*8, y, 130);
	        M_DrawCharacter ((int)(x + (SLIDER_RANGE-1)*8 * range), y, 131);
        }

        void M_DrawCheckbox (int x, int y, bool on)
        {
	        if (on)
		        M_Print (x, y, "on");
	        else
		        M_Print (x, y, "off");
        }

        static void M_Options_Draw ()
        {
	        double		r;
	        wad.qpic_t	p;

	        M_DrawTransPic (16, 4, draw.Draw_CachePic ("gfx/qplaque.lmp") );
	        p = draw.Draw_CachePic ("gfx/p_option.lmp");
	        M_DrawPic ( (320-p.width)/2, 4, p);

	        M_Print (16, 32, "    Customize controls");
	        M_Print (16, 40, "         Go to console");
	        M_Print (16, 48, "     Reset to defaults");

	        M_Print (16, 56, "           Screen size");
	        r = (screen.scr_viewsize.value - 30) / (120 - 30);
	        M_DrawSlider (220, 56, r);

	        M_Print (16, 64, "            Brightness");
            r = (1.0 - view.v_gamma.value) / 0.5;
	        M_DrawSlider (220, 64, r);

        // cursor
	        M_DrawCharacter (200, 32 + options_cursor*8, 12+((int)(host.realtime*4)&1));
        }

        static void M_Options_Key (int k)
        {
	        switch (k)
	        {
	        case keys.K_ESCAPE:
		        M_Menu_Main_f ();
		        break;

	        case keys.K_ENTER:
		        m_entersound = true;
		        switch (options_cursor)
		        {
		        case 0:
			        M_Menu_Keys_f ();
			        break;
		        case 1:
			        m_state = m_state_t.m_none;
			        console.Con_ToggleConsole_f ();
			        break;
		        case 2:
			        cmd.Cbuf_AddText ("exec default.cfg\n");
			        break;
		        default:
			        M_AdjustSliders (1);
			        break;
		        }
		        return;

            case keys.K_UPARROW:
		        options_cursor--;
		        if (options_cursor < 0)
			        options_cursor = OPTIONS_ITEMS-1;
		        break;

            case keys.K_DOWNARROW:
		        options_cursor++;
		        if (options_cursor >= OPTIONS_ITEMS)
			        options_cursor = 0;
		        break;

            case keys.K_LEFTARROW:
		        M_AdjustSliders (-1);
		        break;

	        case keys.K_RIGHTARROW:
		        M_AdjustSliders (1);
		        break;
	        }

	        if (options_cursor == 12)
	        {
		        if (k == keys.K_UPARROW)
			        options_cursor = 11;
		        else
			        options_cursor = 0;
	        }
        }

        //=============================================================================
        /* KEYS MENU */

        int		bind_grab;

        static void M_Menu_Keys_f ()
        {
        }

        void M_UnbindCommand (string command)
        {
        }
        
        static void M_Keys_Draw ()
        {
        }

        static void M_Keys_Key (int k)
        {
        }

        //=============================================================================
        /* VIDEO MENU */

        static void M_Menu_Video_f ()
        {
        }
        
        static void M_Video_Draw ()
        {
        }
        
        static void M_Video_Key (int key)
        {
        }

        //=============================================================================
        /* HELP MENU */

        static int		help_page;
        const int	NUM_HELP_PAGES = 6;
        
        static void M_Menu_Help_f ()
        {
            keys.key_dest = keys.keydest_t.key_menu;
            m_state = m_state_t.m_help;
            m_entersound = true;
            help_page = 0;
        }
        
        static void M_Help_Draw ()
        {
	        M_DrawPic (0, 0, draw.Draw_CachePic ( "gfx/help" + help_page + ".lmp") );
        }
        
        static void M_Help_Key (int key)
        {
            switch (key)
            {
                case keys.K_ESCAPE:
                    M_Menu_Main_f();
                    break;

                case keys.K_UPARROW:
                case keys.K_RIGHTARROW:
                    m_entersound = true;
                    if (++help_page >= NUM_HELP_PAGES)
                        help_page = 0;
                    break;

                case keys.K_DOWNARROW:
                case keys.K_LEFTARROW:
                    m_entersound = true;
                    if (--help_page < 0)
                        help_page = NUM_HELP_PAGES - 1;
                    break;
            }
        }

        //=============================================================================
        /* QUIT MENU */

        static int		    msgNumber;
        static m_state_t	m_quit_prevstate;
        static bool	        wasInMenus;

        static string[] quitMessage = 
        {
        /* .........1.........2.... */
          "  Are you gonna quit    ",
          "  this game just like   ",
          "   everything else?     ",
          "                        ",
         
          " Milord, methinks that  ",
          "   thou art a lowly     ",
          " quitter. Is this true? ",
          "                        ",

          " Do I need to bust your ",
          "  face open for trying  ",
          "        to quit?        ",
          "                        ",

          " Man, I oughta smack you",
          "   for trying to quit!  ",
          "     Press Y to get     ",
          "      smacked out.      ",
         
          " Press Y to quit like a ",
          "   big loser in life.   ",
          "  Press N to stay proud ",
          "    and successful!     ",
         
          "   If you press Y to    ",
          "  quit, I will summon   ",
          "  Satan all over your   ",
          "      hard drive!       ",
         
          "  Um, Asmodeus dislikes ",
          " his children trying to ",
          " quit. Press Y to return",
          "   to your Tinkertoys.  ",
         
          "  If you quit now, I'll ",
          "  throw a blanket-party ",
          "   for you next time!   ",
          "                        "
        };

        public static void M_Menu_Quit_f ()
        {
            if (m_state == m_state_t.m_quit)
                return;
            wasInMenus = (keys.key_dest == keys.keydest_t.key_menu);
            keys.key_dest = keys.keydest_t.key_menu;
            m_quit_prevstate = m_state;
            m_state = m_state_t.m_quit;
            m_entersound = true;
            msgNumber = Helper.helper.rand() & 7;
        }

        static void M_Quit_Key (int key)
        {
            switch (key)
            {
                case keys.K_ESCAPE:
                case 'n':
                case 'N':
                    if (wasInMenus)
                    {
                        m_state = m_quit_prevstate;
                        m_entersound = true;
                    }
                    else
                    {
                        keys.key_dest = keys.keydest_t.key_game;
                        m_state = m_state_t.m_none;
                    }
                    break;

                case 'Y':
                case 'y':
                    keys.key_dest = keys.keydest_t.key_console;
                    host.Host_Quit_f();
                    break;

                default:
                    break;
            }
        }
        
        static void M_Quit_Draw ()
        {
	        if (wasInMenus)
	        {
                m_state = m_quit_prevstate;
                m_recursiveDraw = true;
                M_Draw();
                m_state = m_state_t.m_quit;
            }

	        M_DrawTextBox (56, 76, 24, 4);
	        M_Print (64, 84,  quitMessage[msgNumber*4+0]);
	        M_Print (64, 92,  quitMessage[msgNumber*4+1]);
	        M_Print (64, 100, quitMessage[msgNumber*4+2]);
	        M_Print (64, 108, quitMessage[msgNumber*4+3]);
        }

        //=============================================================================

        /* SERIAL CONFIG MENU */

        int		serialConfig_cursor;
        int[]	serialConfig_cursor_table = {48, 64, 80, 96, 112, 132};
        const int	NUM_SERIALCONFIG_CMDS = 6;

        static int[] ISA_uarts	= {0x3f8,0x2f8,0x3e8,0x2e8};
        static int[] ISA_IRQs	= {4,3,4,3};
        int[] serialConfig_baudrate = {9600,14400,19200,28800,38400,57600};

        int		serialConfig_comport;
        int		serialConfig_irq ;
        int		serialConfig_baud;
        string	serialConfig_phone;

        void M_Menu_SerialConfig_f ()
        {
        }
        
        static void M_SerialConfig_Draw ()
        {
        }

        static void M_SerialConfig_Key (int key)
        {
        }

        //=============================================================================
        /* MODEM CONFIG MENU */

        int		modemConfig_cursor;
        int[]	modemConfig_cursor_table = {40, 56, 88, 120, 156};
        const int NUM_MODEMCONFIG_CMDS = 5;

        char	modemConfig_dialing;
        string	modemConfig_clear;
        string	modemConfig_init;
        string	modemConfig_hangup;

        void M_Menu_ModemConfig_f ()
        {
        }
        
        static void M_ModemConfig_Draw ()
        {
        }

        static void M_ModemConfig_Key (int key)
        {
        }

        //=============================================================================
        /* LAN CONFIG MENU */

        int		lanConfig_cursor = -1;
        int[]	lanConfig_cursor_table = {72, 92, 124};
        const int NUM_LANCONFIG_CMDS = 3;

        int 	lanConfig_port;
        string	lanConfig_portname;
        string	lanConfig_joinname;

        void M_Menu_LanConfig_f ()
        {
        }

        static void M_LanConfig_Draw ()
        {
        }

        static void M_LanConfig_Key (int key)
        {
        }

        //=============================================================================
        /* GAME OPTIONS MENU */

        struct level_t
        {
	        public string	name;
	        public string	description;

            public level_t(string name, string description)
            {
                this.name = name;
                this.description = description;
            }
        };

        static level_t[]	levels =
        {
            new level_t("start", "Entrance"),	// 0

	        new level_t("e1m1", "Slipgate Complex"),				// 1
	        new level_t("e1m2", "Castle of the Damned"),
	        new level_t("e1m3", "The Necropolis"),
	        new level_t("e1m4", "The Grisly Grotto"),
	        new level_t("e1m5", "Gloom Keep"),
	        new level_t("e1m6", "The Door To Chthon"),
	        new level_t("e1m7", "The House of Chthon"),
	        new level_t("e1m8", "Ziggurat Vertigo"),

	        new level_t("e2m1", "The Installation"),				// 9
	        new level_t("e2m2", "Ogre Citadel"),
	        new level_t("e2m3", "Crypt of Decay"),
	        new level_t("e2m4", "The Ebon Fortress"),
	        new level_t("e2m5", "The Wizard's Manse"),
	        new level_t("e2m6", "The Dismal Oubliette"),
	        new level_t("e2m7", "Underearth"),

	        new level_t("e3m1", "Termination Central"),			// 16
	        new level_t("e3m2", "The Vaults of Zin"),
	        new level_t("e3m3", "The Tomb of Terror"),
	        new level_t("e3m4", "Satan's Dark Delight"),
	        new level_t("e3m5", "Wind Tunnels"),
	        new level_t("e3m6", "Chambers of Torment"),
	        new level_t("e3m7", "The Haunted Halls"),

	        new level_t("e4m1", "The Sewage System"),				// 23
	        new level_t("e4m2", "The Tower of Despair"),
	        new level_t("e4m3", "The Elder God Shrine"),
	        new level_t("e4m4", "The Palace of Hate"),
	        new level_t("e4m5", "Hell's Atrium"),
	        new level_t("e4m6", "The Pain Maze"),
	        new level_t("e4m7", "Azure Agony"),
	        new level_t("e4m8", "The Nameless City"),

	        new level_t("end", "Shub-Niggurath's Pit"),			// 31

	        new level_t("dm1", "Place of Two Deaths"),				// 32
	        new level_t("dm2", "Claustrophobopolis"),
	        new level_t("dm3", "The Abandoned Base"),
	        new level_t("dm4", "The Bad Place"),
	        new level_t("dm5", "The Cistern"),
	        new level_t("dm6", "The Dark Zone")
        };

        //MED 01/06/97 added hipnotic levels
        static level_t[]    hipnoticlevels =
        {
           new level_t("start", "Command HQ"),  // 0

           new level_t("hip1m1", "The Pumping Station"),          // 1
           new level_t("hip1m2", "Storage Facility"),
           new level_t("hip1m3", "The Lost Mine"),
           new level_t("hip1m4", "Research Facility"),
           new level_t("hip1m5", "Military Complex"),

           new level_t("hip2m1", "Ancient Realms"),          // 6
           new level_t("hip2m2", "The Black Cathedral"),
           new level_t("hip2m3", "The Catacombs"),
           new level_t("hip2m4", "The Crypt"),
           new level_t("hip2m5", "Mortum's Keep"),
           new level_t("hip2m6", "The Gremlin's Domain"),

           new level_t("hip3m1", "Tur Torment"),       // 12
           new level_t("hip3m2", "Pandemonium"),
           new level_t("hip3m3", "Limbo"),
           new level_t("hip3m4", "The Gauntlet"),

           new level_t("hipend", "Armagon's Lair"),       // 16

           new level_t("hipdm1", "The Edge of Oblivion")           // 17
        };

        //PGM 01/07/97 added rogue levels
        //PGM 03/02/97 added dmatch level
        static level_t[]	roguelevels =
        {
	        new level_t("start",	"Split Decision"),
	        new level_t("r1m1",	"Deviant's Domain"),
	        new level_t("r1m2",	"Dread Portal"),
	        new level_t("r1m3",	"Judgement Call"),
	        new level_t("r1m4",	"Cave of Death"),
	        new level_t("r1m5",	"Towers of Wrath"),
	        new level_t("r1m6",	"Temple of Pain"),
	        new level_t("r1m7",	"Tomb of the Overlord"),
	        new level_t("r2m1",	"Tempus Fugit"),
	        new level_t("r2m2",	"Elemental Fury I"),
	        new level_t("r2m3",	"Elemental Fury II"),
	        new level_t("r2m4",	"Curse of Osiris"),
	        new level_t("r2m5",	"Wizard's Keep"),
	        new level_t("r2m6",	"Blood Sacrifice"),
	        new level_t("r2m7",	"Last Bastion"),
	        new level_t("r2m8",	"Source of Evil"),
	        new level_t("ctf1",     "Division of Change")
        };

        struct episode_t
        {
	        public string	description;
	        public int		firstLevel;
	        int		levels;

            public episode_t(string description, int firstLevel, int levels)
            {
                this.description = description;
                this.firstLevel = firstLevel;
                this.levels = levels;
            }
        };

        static episode_t[] episodes =
        {
	        new episode_t("Welcome to Quake", 0, 1),
	        new episode_t("Doomed Dimension", 1, 8),
	        new episode_t("Realm of Black Magic", 9, 7),
	        new episode_t("Netherworld", 16, 7),
	        new episode_t("The Elder World", 23, 8),
	        new episode_t("Final Level", 31, 1),
	        new episode_t("Deathmatch Arena", 32, 6)
        };

        //MED 01/06/97  added hipnotic episodes
        static episode_t[]  hipnoticepisodes =
        {
           new episode_t("Scourge of Armagon", 0, 1),
           new episode_t("Fortress of the Dead", 1, 5),
           new episode_t("Dominion of Darkness", 6, 6),
           new episode_t("The Rift", 12, 4),
           new episode_t("Final Level", 16, 1),
           new episode_t("Deathmatch Arena", 17, 1)
        };

        //PGM 01/07/97 added rogue episodes
        //PGM 03/02/97 added dmatch episode
        static episode_t[] rogueepisodes =
        {
	        new episode_t("Introduction", 0, 1),
	        new episode_t("Hell's Fortress", 1, 7),
	        new episode_t("Corridors of Time", 8, 8),
	        new episode_t("Deathmatch Arena", 16, 1)
        };

        static int	startepisode;
        static int	startlevel;
        static int maxplayers;
        static bool m_serverInfoMessage = false;
        static double m_serverInfoMessageTime;

        void M_Menu_GameOptions_f ()
        {
        }
        
        static int[] gameoptions_cursor_table = {40, 56, 64, 72, 80, 88, 96, 112, 120};
        const int	NUM_GAMEOPTIONS = 9;
        static int		gameoptions_cursor;

        static void M_GameOptions_Draw ()
        {
	        wad.qpic_t	p;
	        int		    x;

	        M_DrawTransPic (16, 4, draw.Draw_CachePic ("gfx/qplaque.lmp") );
            p = draw.Draw_CachePic("gfx/p_multi.lmp");
	        M_DrawPic ( (320-p.width)/2, 4, p);

	        M_DrawTextBox (152, 32, 10, 1);
	        M_Print (160, 40, "begin game");

	        M_Print (0, 56, "      Max players");
	        M_Print (160, 56, "" + maxplayers );

	        M_Print (0, 64, "        Game Type");

	        M_Print (0, 72, "        Teamplay");

	        M_Print (0, 80, "            Skill");

	        M_Print (0, 88, "       Frag Limit");

	        M_Print (0, 96, "       Time Limit");

	        M_Print (0, 112, "         Episode");
           //MED 01/06/97 added hipnotic episodes
           if (common.hipnotic)
              M_Print (160, 112, hipnoticepisodes[startepisode].description);
           //PGM 01/07/97 added rogue episodes
           else if (common.rogue)
              M_Print (160, 112, rogueepisodes[startepisode].description);
           else
              M_Print (160, 112, episodes[startepisode].description);

	        M_Print (0, 120, "           Level");
           //MED 01/06/97 added hipnotic episodes
           if (common.hipnotic)
           {
              M_Print (160, 120, hipnoticlevels[hipnoticepisodes[startepisode].firstLevel + startlevel].description);
              M_Print (160, 128, hipnoticlevels[hipnoticepisodes[startepisode].firstLevel + startlevel].name);
           }
           //PGM 01/07/97 added rogue episodes
           else if (common.rogue)
           {
              M_Print (160, 120, roguelevels[rogueepisodes[startepisode].firstLevel + startlevel].description);
              M_Print (160, 128, roguelevels[rogueepisodes[startepisode].firstLevel + startlevel].name);
           }
           else
           {
              M_Print (160, 120, levels[episodes[startepisode].firstLevel + startlevel].description);
              M_Print (160, 128, levels[episodes[startepisode].firstLevel + startlevel].name);
           }

        // line cursor
	        M_DrawCharacter (144, gameoptions_cursor_table[gameoptions_cursor], 12+((int)(host.realtime*4)&1));

	        if (m_serverInfoMessage)
	        {
		        if ((host.realtime - m_serverInfoMessageTime) < 5.0)
		        {
			        x = (320-26*8)/2;
			        M_DrawTextBox (x, 138, 24, 4);
			        x += 8;
			        M_Print (x, 146, "  More than 4 players   ");
			        M_Print (x, 154, " requires using command ");
			        M_Print (x, 162, "line parameters; please ");
			        M_Print (x, 170, "   see techinfo.txt.    ");
		        }
		        else
		        {
			        m_serverInfoMessage = false;
		        }
	        }
        }


        void M_NetStart_Change (int dir)
        {
        }

        static void M_GameOptions_Key (int key)
        {
        }

        //=============================================================================
        /* SEARCH MENU */

        bool	    searchComplete = false;
        double		searchCompleteTime;

        void M_Menu_Search_f ()
        {
        }

        static void M_Search_Draw ()
        {
        }

        static void M_Search_Key (int key)
        {
        }

        //=============================================================================
        /* SLIST MENU */

        int		slist_cursor;
        bool    slist_sorted;

        void M_Menu_ServerList_f ()
        {
        }
        
        static void M_ServerList_Draw ()
        {
        }

        static void M_ServerList_Key (int k)
        {
        }

        //=============================================================================
        /* Menu Subsystem */
        
        public static void M_Init ()
        {
            cmd.Cmd_AddCommand("togglemenu", M_ToggleMenu_f);

            cmd.Cmd_AddCommand("menu_main", M_Menu_Main_f);
            cmd.Cmd_AddCommand("menu_singleplayer", M_Menu_SinglePlayer_f);
            cmd.Cmd_AddCommand("menu_load", M_Menu_Load_f);
            cmd.Cmd_AddCommand("menu_save", M_Menu_Save_f);
            cmd.Cmd_AddCommand("menu_multiplayer", M_Menu_MultiPlayer_f);
            cmd.Cmd_AddCommand("menu_setup", M_Menu_Setup_f);
            cmd.Cmd_AddCommand("menu_options", M_Menu_Options_f);
            cmd.Cmd_AddCommand("menu_keys", M_Menu_Keys_f);
            cmd.Cmd_AddCommand("menu_video", M_Menu_Video_f);
            cmd.Cmd_AddCommand("help", M_Menu_Help_f);
            cmd.Cmd_AddCommand("menu_quit", M_Menu_Quit_f);
        }
        
        public static void M_Draw ()
        {
            if (m_state == m_state_t.m_none)
                return;

            if (!m_recursiveDraw)
            {
                screen.scr_copyeverything = true;

                if (screen.scr_con_current != 0)
                {
                    draw.Draw_ConsoleBackground((int)screen.vid.height);
                    sound.S_ExtraUpdate();
                }
                else
                    draw.Draw_FadeScreen();

                screen.scr_fullupdate = 0;
            }
            else
            {
                m_recursiveDraw = false;
            }

            switch (m_state)
            {
                case m_state_t.m_none:
                    break;

                case m_state_t.m_main:
                    M_Main_Draw();
                    break;

                case m_state_t.m_singleplayer:
                    M_SinglePlayer_Draw();
                    break;

                case m_state_t.m_load:
                    M_Load_Draw();
                    break;

                case m_state_t.m_save:
                    M_Save_Draw();
                    break;

                case m_state_t.m_multiplayer:
                    M_MultiPlayer_Draw();
                    break;

                case m_state_t.m_setup:
                    M_Setup_Draw();
                    break;

                case m_state_t.m_net:
                    M_Net_Draw();
                    break;

                case m_state_t.m_options:
                    M_Options_Draw();
                    break;

                case m_state_t.m_keys:
                    M_Keys_Draw();
                    break;

                case m_state_t.m_video:
                    M_Video_Draw();
                    break;

                case m_state_t.m_help:
                    M_Help_Draw();
                    break;

                case m_state_t.m_quit:
                    M_Quit_Draw();
                    break;

                case m_state_t.m_serialconfig:
                    M_SerialConfig_Draw();
                    break;

                case m_state_t.m_modemconfig:
                    M_ModemConfig_Draw();
                    break;

                case m_state_t.m_lanconfig:
                    M_LanConfig_Draw();
                    break;

                case m_state_t.m_gameoptions:
                    M_GameOptions_Draw();
                    break;

                case m_state_t.m_search:
                    M_Search_Draw();
                    break;

                case m_state_t.m_slist:
                    M_ServerList_Draw();
                    break;
            }

            sound.S_ExtraUpdate();
        }

        public static void M_Keydown (int key)
        {
            switch (m_state)
            {
                case m_state_t.m_none:
                    return;

                case m_state_t.m_main:
                    M_Main_Key(key);
                    return;

                case m_state_t.m_singleplayer:
                    M_SinglePlayer_Key(key);
                    return;

                case m_state_t.m_load:
                    M_Load_Key(key);
                    return;

                case m_state_t.m_save:
                    M_Save_Key(key);
                    return;

                case m_state_t.m_multiplayer:
                    M_MultiPlayer_Key(key);
                    return;

                case m_state_t.m_setup:
                    M_Setup_Key(key);
                    return;

                case m_state_t.m_net:
                    M_Net_Key(key);
                    return;

                case m_state_t.m_options:
                    M_Options_Key(key);
                    return;

                case m_state_t.m_keys:
                    M_Keys_Key(key);
                    return;

                case m_state_t.m_video:
                    M_Video_Key(key);
                    return;

                case m_state_t.m_help:
                    M_Help_Key(key);
                    return;

                case m_state_t.m_quit:
                    M_Quit_Key(key);
                    return;

                case m_state_t.m_serialconfig:
                    M_SerialConfig_Key(key);
                    return;

                case m_state_t.m_modemconfig:
                    M_ModemConfig_Key(key);
                    return;

                case m_state_t.m_lanconfig:
                    M_LanConfig_Key(key);
                    return;

                case m_state_t.m_gameoptions:
                    M_GameOptions_Key(key);
                    return;

                case m_state_t.m_search:
                    M_Search_Key(key);
                    break;

                case m_state_t.m_slist:
                    M_ServerList_Key(key);
                    return;
            }
        }
        
        void M_ConfigureNetSubsystem()
        {
        }
    }
}