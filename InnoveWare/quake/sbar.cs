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

// the status bar is only redrawn if something has changed, but if anything
// does, the entire thing will be redrawn for the next vid.numpages frames.
// sbar.c -- status bar code

namespace quake
{
    public sealed class sbar
    {
        const int	    SBAR_HEIGHT = 24;

        static int			    sb_updates;		// if >= vid.numpages, no update needed

        const int       STAT_MINUS = 10;	// num frame for '-' stats digit
        static wad.qpic_t[,]	sb_nums = new wad.qpic_t[2,11];
        static wad.qpic_t	    sb_colon, sb_slash;
        static wad.qpic_t	    sb_ibar;
        static wad.qpic_t	    sb_sbar;
        static wad.qpic_t	    sb_scorebar;

        static wad.qpic_t[,]    sb_weapons = new wad.qpic_t[7,8];   // 0 is active, 1 is owned, 2-5 are flashes
        static wad.qpic_t[]     sb_ammo = new wad.qpic_t[4];
        static wad.qpic_t[]	    sb_sigil = new wad.qpic_t[4];
        static wad.qpic_t[]	    sb_armor = new wad.qpic_t[3];
        static wad.qpic_t[]	    sb_items = new wad.qpic_t[32];

        static wad.qpic_t[,]	sb_faces = new wad.qpic_t[7,2];		// 0 is gibbed, 1 is dead, 2-6 are alive
							        // 0 is static, 1 is temporary animation
        static wad.qpic_t	    sb_face_invis;
        static wad.qpic_t	    sb_face_quad;
        static wad.qpic_t	    sb_face_invuln;
        static wad.qpic_t	    sb_face_invis_invuln;

        static bool	            sb_showscores;

        public static int		    sb_lines;			// scan lines to draw

        static wad.qpic_t[]     rsb_invbar = new wad.qpic_t[2];
        static wad.qpic_t[]     rsb_weapons = new wad.qpic_t[5];
        static wad.qpic_t[]     rsb_items = new wad.qpic_t[2];
        static wad.qpic_t[]     rsb_ammo = new wad.qpic_t[3];
        static wad.qpic_t       rsb_teambord;		// PGM 01/19/97 - team color border

        //MED 01/04/97 added two more weapons + 3 alternates for grenade launcher
        static wad.qpic_t[,]    hsb_weapons = new wad.qpic_t[7,5];   // 0 is active, 1 is owned, 2-5 are flashes
        //MED 01/04/97 added hipnotic items array
        static wad.qpic_t[]     hsb_items = new wad.qpic_t[2];

        /*
        ===============
        Sbar_ShowScores

        Tab key down
        ===============
        */
        static void Sbar_ShowScores ()
        {
	        if (sb_showscores)
		        return;
	        sb_showscores = true;
	        sb_updates = 0;
        }

        /*
        ===============
        Sbar_DontShowScores

        Tab key up
        ===============
        */
        static void Sbar_DontShowScores ()
        {
	        sb_showscores = false;
	        sb_updates = 0;
        }

        /*
        ===============
        Sbar_Changed
        ===============
        */
        public static void Sbar_Changed ()
        {
	        sb_updates = 0;	// update next frame
        }

        /*
        ===============
        Sbar_Init
        ===============
        */
        public static void Sbar_Init ()
        {
	        int		i;

	        for (i=0 ; i<10 ; i++)
	        {
		        sb_nums[0,i] = draw.Draw_PicFromWad ("num_" + i);
		        sb_nums[1,i] = draw.Draw_PicFromWad ("anum_" + i);
	        }

	        sb_nums[0,10] = draw.Draw_PicFromWad ("num_minus");
	        sb_nums[1,10] = draw.Draw_PicFromWad ("anum_minus");

	        sb_colon = draw.Draw_PicFromWad ("num_colon");
	        sb_slash = draw.Draw_PicFromWad ("num_slash");

	        sb_weapons[0,0] = draw.Draw_PicFromWad ("inv_shotgun");
	        sb_weapons[0,1] = draw.Draw_PicFromWad ("inv_sshotgun");
	        sb_weapons[0,2] = draw.Draw_PicFromWad ("inv_nailgun");
	        sb_weapons[0,3] = draw.Draw_PicFromWad ("inv_snailgun");
	        sb_weapons[0,4] = draw.Draw_PicFromWad ("inv_rlaunch");
	        sb_weapons[0,5] = draw.Draw_PicFromWad ("inv_srlaunch");
	        sb_weapons[0,6] = draw.Draw_PicFromWad ("inv_lightng");

	        sb_weapons[1,0] = draw.Draw_PicFromWad ("inv2_shotgun");
	        sb_weapons[1,1] = draw.Draw_PicFromWad ("inv2_sshotgun");
	        sb_weapons[1,2] = draw.Draw_PicFromWad ("inv2_nailgun");
	        sb_weapons[1,3] = draw.Draw_PicFromWad ("inv2_snailgun");
	        sb_weapons[1,4] = draw.Draw_PicFromWad ("inv2_rlaunch");
	        sb_weapons[1,5] = draw.Draw_PicFromWad ("inv2_srlaunch");
	        sb_weapons[1,6] = draw.Draw_PicFromWad ("inv2_lightng");

	        for (i=0 ; i<5 ; i++)
	        {
		        sb_weapons[2+i,0] = draw.Draw_PicFromWad ("inva" + (i+1) + "_shotgun");
		        sb_weapons[2+i,1] = draw.Draw_PicFromWad ("inva" + (i+1) + "_sshotgun");
		        sb_weapons[2+i,2] = draw.Draw_PicFromWad ("inva" + (i+1) + "_nailgun");
		        sb_weapons[2+i,3] = draw.Draw_PicFromWad ("inva" + (i+1) + "_snailgun");
		        sb_weapons[2+i,4] = draw.Draw_PicFromWad ("inva" + (i+1) + "_rlaunch");
		        sb_weapons[2+i,5] = draw.Draw_PicFromWad ("inva" + (i+1) + "_srlaunch");
		        sb_weapons[2+i,6] = draw.Draw_PicFromWad ("inva" + (i+1) + "_lightng");
	        }

	        sb_ammo[0] = draw.Draw_PicFromWad ("sb_shells");
	        sb_ammo[1] = draw.Draw_PicFromWad ("sb_nails");
	        sb_ammo[2] = draw.Draw_PicFromWad ("sb_rocket");
	        sb_ammo[3] = draw.Draw_PicFromWad ("sb_cells");

	        sb_armor[0] = draw.Draw_PicFromWad ("sb_armor1");
	        sb_armor[1] = draw.Draw_PicFromWad ("sb_armor2");
	        sb_armor[2] = draw.Draw_PicFromWad ("sb_armor3");

	        sb_items[0] = draw.Draw_PicFromWad ("sb_key1");
	        sb_items[1] = draw.Draw_PicFromWad ("sb_key2");
	        sb_items[2] = draw.Draw_PicFromWad ("sb_invis");
	        sb_items[3] = draw.Draw_PicFromWad ("sb_invuln");
	        sb_items[4] = draw.Draw_PicFromWad ("sb_suit");
	        sb_items[5] = draw.Draw_PicFromWad ("sb_quad");

	        sb_sigil[0] = draw.Draw_PicFromWad ("sb_sigil1");
	        sb_sigil[1] = draw.Draw_PicFromWad ("sb_sigil2");
	        sb_sigil[2] = draw.Draw_PicFromWad ("sb_sigil3");
	        sb_sigil[3] = draw.Draw_PicFromWad ("sb_sigil4");

	        sb_faces[4,0] = draw.Draw_PicFromWad ("face1");
	        sb_faces[4,1] = draw.Draw_PicFromWad ("face_p1");
	        sb_faces[3,0] = draw.Draw_PicFromWad ("face2");
	        sb_faces[3,1] = draw.Draw_PicFromWad ("face_p2");
	        sb_faces[2,0] = draw.Draw_PicFromWad ("face3");
	        sb_faces[2,1] = draw.Draw_PicFromWad ("face_p3");
	        sb_faces[1,0] = draw.Draw_PicFromWad ("face4");
	        sb_faces[1,1] = draw.Draw_PicFromWad ("face_p4");
	        sb_faces[0,0] = draw.Draw_PicFromWad ("face5");
	        sb_faces[0,1] = draw.Draw_PicFromWad ("face_p5");

	        sb_face_invis = draw.Draw_PicFromWad ("face_invis");
	        sb_face_invuln = draw.Draw_PicFromWad ("face_invul2");
	        sb_face_invis_invuln = draw.Draw_PicFromWad ("face_inv2");
	        sb_face_quad = draw.Draw_PicFromWad ("face_quad");

            cmd.Cmd_AddCommand("+showscores", Sbar_ShowScores);
            cmd.Cmd_AddCommand("-showscores", Sbar_DontShowScores);

	        sb_sbar = draw.Draw_PicFromWad ("sbar");
	        sb_ibar = draw.Draw_PicFromWad ("ibar");
	        sb_scorebar = draw.Draw_PicFromWad ("scorebar");

        //MED 01/04/97 added new hipnotic weapons
	        if (common.hipnotic)
	        {
	          hsb_weapons[0,0] = draw.Draw_PicFromWad ("inv_laser");
	          hsb_weapons[0,1] = draw.Draw_PicFromWad ("inv_mjolnir");
	          hsb_weapons[0,2] = draw.Draw_PicFromWad ("inv_gren_prox");
	          hsb_weapons[0,3] = draw.Draw_PicFromWad ("inv_prox_gren");
	          hsb_weapons[0,4] = draw.Draw_PicFromWad ("inv_prox");

	          hsb_weapons[1,0] = draw.Draw_PicFromWad ("inv2_laser");
	          hsb_weapons[1,1] = draw.Draw_PicFromWad ("inv2_mjolnir");
	          hsb_weapons[1,2] = draw.Draw_PicFromWad ("inv2_gren_prox");
	          hsb_weapons[1,3] = draw.Draw_PicFromWad ("inv2_prox_gren");
	          hsb_weapons[1,4] = draw.Draw_PicFromWad ("inv2_prox");

	          for (i=0 ; i<5 ; i++)
	          {
		         hsb_weapons[2+i,0] = draw.Draw_PicFromWad ("inva" + (i+1) + "_laser");
		         hsb_weapons[2+i,1] = draw.Draw_PicFromWad ("inva" + (i+1) + "_mjolnir");
		         hsb_weapons[2+i,2] = draw.Draw_PicFromWad ("inva" + (i+1) + "_gren_prox");
		         hsb_weapons[2+i,3] = draw.Draw_PicFromWad ("inva" + (i+1) + "_prox_gren");
		         hsb_weapons[2+i,4] = draw.Draw_PicFromWad ("inva" + (i+1) + "_prox");
	          }

	          hsb_items[0] = draw.Draw_PicFromWad ("sb_wsuit");
	          hsb_items[1] = draw.Draw_PicFromWad ("sb_eshld");
	        }

	        if (common.rogue)
	        {
		        rsb_invbar[0] = draw.Draw_PicFromWad ("r_invbar1");
		        rsb_invbar[1] = draw.Draw_PicFromWad ("r_invbar2");

		        rsb_weapons[0] = draw.Draw_PicFromWad ("r_lava");
		        rsb_weapons[1] = draw.Draw_PicFromWad ("r_superlava");
		        rsb_weapons[2] = draw.Draw_PicFromWad ("r_gren");
		        rsb_weapons[3] = draw.Draw_PicFromWad ("r_multirock");
		        rsb_weapons[4] = draw.Draw_PicFromWad ("r_plasma");

		        rsb_items[0] = draw.Draw_PicFromWad ("r_shield1");
                rsb_items[1] = draw.Draw_PicFromWad ("r_agrav1");

        // PGM 01/19/97 - team color border
                rsb_teambord = draw.Draw_PicFromWad ("r_teambord");
        // PGM 01/19/97 - team color border

		        rsb_ammo[0] = draw.Draw_PicFromWad ("r_ammolava");
		        rsb_ammo[1] = draw.Draw_PicFromWad ("r_ammomulti");
		        rsb_ammo[2] = draw.Draw_PicFromWad ("r_ammoplasma");
	        }
        }


        //=============================================================================

        // drawing routines are relative to the status bar location

        /*
        =============
        Sbar_DrawPic
        =============
        */
        static void Sbar_DrawPic (int x, int y, wad.qpic_t pic)
        {
		    if (client.cl.gametype == net.GAME_DEATHMATCH)
                draw.Draw_Pic(x /* + ((screen.vid.width - 320)>>1)*/, (int)(y + (screen.vid.height - SBAR_HEIGHT)), pic);
		    else
                draw.Draw_Pic((int)(x + ((screen.vid.width - 320) >> 1)), (int)(y + (screen.vid.height - SBAR_HEIGHT)), pic);
        }

        /*
        =============
        Sbar_DrawTransPic
        =============
        */
        static void Sbar_DrawTransPic (int x, int y, wad.qpic_t pic)
        {
		    if (client.cl.gametype == net.GAME_DEATHMATCH)
                draw.Draw_TransPic(x /*+ ((screen.vid.width - 320)>>1)*/, (int)(y + (screen.vid.height - SBAR_HEIGHT)), pic);
		    else
                draw.Draw_TransPic((int)(x + ((screen.vid.width - 320) >> 1)), (int)(y + (screen.vid.height - SBAR_HEIGHT)), pic);
        }

        /*
        ================
        Sbar_DrawCharacter

        Draws one solid graphics character
        ================
        */
        static void Sbar_DrawCharacter (int x, int y, int num)
        {
		    if (client.cl.gametype == net.GAME_DEATHMATCH)
                draw.Draw_Character(x /*+ ((screen.vid.width - 320)>>1) */ + 4, (int)(y + screen.vid.height - SBAR_HEIGHT), num);
		    else
                draw.Draw_Character((int)(x + ((screen.vid.width - 320) >> 1) + 4), (int)(y + screen.vid.height - SBAR_HEIGHT), num);
        }

        /*
        ================
        Sbar_DrawString
        ================
        */
        void Sbar_DrawString (int x, int y, string str)
        {
		    if (client.cl.gametype == net.GAME_DEATHMATCH)
                draw.Draw_String(x /*+ ((screen.vid.width - 320)>>1)*/, (int)(y + screen.vid.height - SBAR_HEIGHT), str);
		    else
                draw.Draw_String((int)(x + ((screen.vid.width - 320) >> 1)), (int)(y + screen.vid.height - SBAR_HEIGHT), str);
        }

        /*
        =============
        Sbar_itoa
        =============
        */
        int Sbar_itoa (int num, string buf)
        {
            return -1;
        }
        
        /*
        =============
        Sbar_DrawNum
        =============
        */
        static void Sbar_DrawNum (int x, int y, int num, int digits, int color)
        {
	        string			str;
	        int			    ptr;
	        int				l, frame;

	        //l = Sbar_itoa (num, str);
            str = "" + num;
            l = str.Length;
	        ptr = 0;
	        if (l > digits)
		        ptr += (l-digits);
	        if (l < digits)
		        x += (digits-l)*24;

	        while (ptr < str.Length)
	        {
		        if (str[ptr] == '-')
			        frame = STAT_MINUS;
		        else
			        frame = str[ptr] -'0';

		        Sbar_DrawTransPic (x,y,sb_nums[color,frame]);
		        x += 24;
		        ptr++;
	        }
        }

        //=============================================================================

        int[]	    fragsort = new int[quakedef.MAX_SCOREBOARD];

        string[]    scoreboardtext = new string[quakedef.MAX_SCOREBOARD];
        int[]       scoreboardtop = new int[quakedef.MAX_SCOREBOARD];
        int[]       scoreboardbottom = new int[quakedef.MAX_SCOREBOARD];
        int[]       scoreboardcount = new int[quakedef.MAX_SCOREBOARD];
        int		    scoreboardlines;

        /*
        ===============
        Sbar_SortFrags
        ===============
        */
        void Sbar_SortFrags ()
        {
        }

        int	Sbar_ColorForMap (int m)
        {
	        return m < 128 ? m + 8 : m + 8;
        }

        /*
        ===============
        Sbar_UpdateScoreboard
        ===============
        */
        void Sbar_UpdateScoreboard ()
        {
        }

        /*
        ===============
        Sbar_SoloScoreboard
        ===============
        */
        static void Sbar_SoloScoreboard ()
        {
        }

        /*
        ===============
        Sbar_DrawScoreboard
        ===============
        */
        static void Sbar_DrawScoreboard ()
        {
	        Sbar_SoloScoreboard ();
        }

        //=============================================================================

        /*
        ===============
        Sbar_DrawInventory
        ===============
        */
        static void Sbar_DrawInventory ()
        {
	        int		i;
	        string	num;
	        double	time;
	        int		flashon;

	        if (common.rogue)
	        {
		        if ( client.cl.stats[quakedef.STAT_ACTIVEWEAPON] >= quakedef.RIT_LAVA_NAILGUN )
			        Sbar_DrawPic (0, -24, rsb_invbar[0]);
		        else
			        Sbar_DrawPic (0, -24, rsb_invbar[1]);
	        }
	        else
	        {
		        Sbar_DrawPic (0, -24, sb_ibar);
	        }

        // weapons
	        for (i=0 ; i<7 ; i++)
	        {
		        if ((client.cl.items & (quakedef.IT_SHOTGUN<<i) ) != 0)
		        {
			        time = client.cl.item_gettime[i];
			        flashon = (int)((client.cl.time - time)*10);
			        if (flashon >= 10)
			        {
                        if (client.cl.stats[quakedef.STAT_ACTIVEWEAPON] == (quakedef.IT_SHOTGUN << i))
					        flashon = 1;
				        else
					        flashon = 0;
			        }
			        else
				        flashon = (flashon%5) + 2;

                    Sbar_DrawPic (i*24, -16, sb_weapons[flashon,i]);

			        if (flashon > 1)
				        sb_updates = 0;		// force update to remove flash
		        }
	        }

	        if (common.rogue)
	        {
            // check for powered up weapon.
                if (client.cl.stats[quakedef.STAT_ACTIVEWEAPON] >= quakedef.RIT_LAVA_NAILGUN)
		        {
			        for (i=0;i<5;i++)
			        {
                        if (client.cl.stats[quakedef.STAT_ACTIVEWEAPON] == (quakedef.RIT_LAVA_NAILGUN << i))
				        {
					        Sbar_DrawPic ((i+2)*24, -16, rsb_weapons[i]);
				        }
			        }
		        }
	        }

        // ammo counts
	        for (i=0 ; i<4 ; i++)
	        {
		        num = "" + client.cl.stats[quakedef.STAT_SHELLS+i];
                if (num.Length == 1)
                    num = "  " + num;
                else if (num.Length == 2)
                    num = " " + num;
		        if (num[0] != ' ')
			        Sbar_DrawCharacter ( (6*i+1)*8 - 2, -24, 18 + num[0] - '0');
		        if (num[1] != ' ')
			        Sbar_DrawCharacter ( (6*i+2)*8 - 2, -24, 18 + num[1] - '0');
		        if (num[2] != ' ')
			        Sbar_DrawCharacter ( (6*i+3)*8 - 2, -24, 18 + num[2] - '0');
	        }

	        flashon = 0;
           // items
           for (i=0 ; i<6 ; i++)
              if ((client.cl.items & (1<<(17+i))) != 0)
              {
                 time = client.cl.item_gettime[17+i];
                 if (time != 0 && time > client.cl.time - 2 && flashon != 0 )
                 {  // flash frame
                    sb_updates = 0;
                 }
                 else
                 {
                 //MED 01/04/97 changed keys
                    if (!common.hipnotic || (i>1))
                    {
                       Sbar_DrawPic (192 + i*16, -16, sb_items[i]);
                    }
                 }
                 if (time != 0 && time > client.cl.time - 2)
                    sb_updates = 0;
              }
           //MED 01/04/97 added hipnotic items
           // hipnotic items
           if (common.hipnotic)
           {
              for (i=0 ; i<2 ; i++)
                 if ((client.cl.items & (1<<(24+i))) != 0)
                 {
                    time = client.cl.item_gettime[24+i];
                    if (time != 0 && time > client.cl.time - 2 && flashon != 0 )
                    {  // flash frame
                       sb_updates = 0;
                    }
                    else
                    {
                       Sbar_DrawPic (288 + i*16, -16, hsb_items[i]);
                    }
                    if (time != 0 && time > client.cl.time - 2)
                       sb_updates = 0;
                 }
           }

	        if (common.rogue)
	        {
	        // new rogue items
		        for (i=0 ; i<2 ; i++)
		        {
			        if ((client.cl.items & (1<<(29+i))) != 0)
			        {
				        time = client.cl.item_gettime[29+i];

				        if (time != 0 &&	time > client.cl.time - 2 && flashon != 0 )
				        {	// flash frame
					        sb_updates = 0;
				        }
				        else
				        {
					        Sbar_DrawPic (288 + i*16, -16, rsb_items[i]);
				        }

				        if (time != 0 && time > client.cl.time - 2)
					        sb_updates = 0;
			        }
		        }
	        }
	        else
	        {
	        // sigils
		        for (i=0 ; i<4 ; i++)
		        {
			        if ((client.cl.items & (1<<(28+i))) != 0)
			        {
				        time = client.cl.item_gettime[28+i];
				        if (time != 0 && time > client.cl.time - 2 && flashon != 0 )
				        {	// flash frame
					        sb_updates = 0;
				        }
				        else
					        Sbar_DrawPic (320-32 + i*8, -16, sb_sigil[i]);
				        if (time != 0 && time > client.cl.time - 2)
					        sb_updates = 0;
			        }
		        }
	        }
        }

        //=============================================================================

        /*
        ===============
        Sbar_DrawFrags
        ===============
        */
        static void Sbar_DrawFrags ()
        {
        }

        //=============================================================================

        /*
        ===============
        Sbar_DrawFace
        ===============
        */
        static void Sbar_DrawFace ()
        {
	        int		f, anim;

            if ((client.cl.items & (quakedef.IT_INVISIBILITY | quakedef.IT_INVULNERABILITY))
            == (quakedef.IT_INVISIBILITY | quakedef.IT_INVULNERABILITY))
	        {
		        Sbar_DrawPic (112, 0, sb_face_invis_invuln);
		        return;
	        }
	        if ((client.cl.items & quakedef.IT_QUAD) != 0)
	        {
		        Sbar_DrawPic (112, 0, sb_face_quad );
		        return;
	        }
            if ((client.cl.items & quakedef.IT_INVISIBILITY) != 0)
	        {
		        Sbar_DrawPic (112, 0, sb_face_invis );
		        return;
	        }
            if ((client.cl.items & quakedef.IT_INVULNERABILITY) != 0)
	        {
		        Sbar_DrawPic (112, 0, sb_face_invuln);
		        return;
	        }

            if (client.cl.stats[quakedef.STAT_HEALTH] >= 100)
		        f = 4;
	        else
                f = client.cl.stats[quakedef.STAT_HEALTH] / 20;

	        if (client.cl.time <= client.cl.faceanimtime)
	        {
		        anim = 1;
		        sb_updates = 0;		// make sure the anim gets drawn over
	        }
	        else
		        anim = 0;
	        Sbar_DrawPic (112, 0, sb_faces[f,anim]);
        }

        /*
        ===============
        Sbar_Draw
        ===============
        */
        public static void Sbar_Draw ()
        {
            if (screen.scr_con_current == screen.vid.height)
                return;		// console is full screen

            if (sb_updates >= screen.vid.numpages)
                return;

            screen.scr_copyeverything = true;

            sb_updates++;

            if (sb_lines != 0 && screen.vid.width > 320)
                draw.Draw_TileClear(0, (int)(screen.vid.height - sb_lines), (int)screen.vid.width, sb_lines);

            if (sb_lines > 24)
            {
                Sbar_DrawInventory();
                if (client.cl.maxclients != 1)
			        Sbar_DrawFrags ();
            }

            if (sb_showscores || client.cl.stats[quakedef.STAT_HEALTH] <= 0)
	        {
		        Sbar_DrawPic (0, 0, sb_scorebar);
		        Sbar_DrawScoreboard ();
		        sb_updates = 0;
	        }
            if (sb_lines != 0)
            {
                Sbar_DrawPic(0, 0, sb_sbar);

           // keys (hipnotic only)
              //MED 01/04/97 moved keys here so they would not be overwritten
              if (common.hipnotic)
              {
                 if ((client.cl.items & quakedef.IT_KEY1) != 0)
                    Sbar_DrawPic (209, 3, sb_items[0]);
                 if ((client.cl.items & quakedef.IT_KEY2) != 0)
                    Sbar_DrawPic (209, 12, sb_items[1]);
              }
           // armor
              if ((client.cl.items & quakedef.IT_INVULNERABILITY) != 0)
		        {
			        Sbar_DrawNum (24, 0, 666, 3, 1);
			        Sbar_DrawPic (0, 0, draw.draw_disc);
		        }
		        else
		        {
			        if (common.rogue)
			        {
				        Sbar_DrawNum (24, 0, client.cl.stats[quakedef.STAT_ARMOR], 3,
                            (client.cl.stats[quakedef.STAT_ARMOR] <= 25) ? 1 : 0);
                        if ((client.cl.items & quakedef.RIT_ARMOR3) != 0)
					        Sbar_DrawPic (0, 0, sb_armor[2]);
                        else if ((client.cl.items & quakedef.RIT_ARMOR2) != 0)
					        Sbar_DrawPic (0, 0, sb_armor[1]);
                        else if ((client.cl.items & quakedef.RIT_ARMOR1) != 0)
					        Sbar_DrawPic (0, 0, sb_armor[0]);
			        }
			        else
			        {
                        Sbar_DrawNum(24, 0, client.cl.stats[quakedef.STAT_ARMOR], 3
                            , (client.cl.stats[quakedef.STAT_ARMOR] <= 25) ? 1 : 0);
                        if ((client.cl.items & quakedef.IT_ARMOR3) != 0)
					        Sbar_DrawPic (0, 0, sb_armor[2]);
                        else if ((client.cl.items & quakedef.IT_ARMOR2) != 0)
					        Sbar_DrawPic (0, 0, sb_armor[1]);
                        else if ((client.cl.items & quakedef.IT_ARMOR1) != 0)
					        Sbar_DrawPic (0, 0, sb_armor[0]);
			        }
		        }

                // face
                Sbar_DrawFace();

	        // health
		        Sbar_DrawNum (136, 0, client.cl.stats[quakedef.STAT_HEALTH], 3
                    , (client.cl.stats[quakedef.STAT_HEALTH] <= 25) ? 1 : 0);

	        // ammo icon
		        if (common.rogue)
		        {
                    if ((client.cl.items & quakedef.RIT_SHELLS) != 0)
				        Sbar_DrawPic (224, 0, sb_ammo[0]);
                    else if ((client.cl.items & quakedef.RIT_NAILS) != 0)
				        Sbar_DrawPic (224, 0, sb_ammo[1]);
                    else if ((client.cl.items & quakedef.RIT_ROCKETS) != 0)
				        Sbar_DrawPic (224, 0, sb_ammo[2]);
                    else if ((client.cl.items & quakedef.RIT_CELLS) != 0)
				        Sbar_DrawPic (224, 0, sb_ammo[3]);
                    else if ((client.cl.items & quakedef.RIT_LAVA_NAILS) != 0)
				        Sbar_DrawPic (224, 0, rsb_ammo[0]);
                    else if ((client.cl.items & quakedef.RIT_PLASMA_AMMO) != 0)
				        Sbar_DrawPic (224, 0, rsb_ammo[1]);
                    else if ((client.cl.items & quakedef.RIT_MULTI_ROCKETS) != 0)
				        Sbar_DrawPic (224, 0, rsb_ammo[2]);
		        }
		        else
		        {
                    if ((client.cl.items & quakedef.IT_SHELLS) != 0)
				        Sbar_DrawPic (224, 0, sb_ammo[0]);
                    else if ((client.cl.items & quakedef.IT_NAILS) != 0)
				        Sbar_DrawPic (224, 0, sb_ammo[1]);
                    else if ((client.cl.items & quakedef.IT_ROCKETS) != 0)
				        Sbar_DrawPic (224, 0, sb_ammo[2]);
                    else if ((client.cl.items & quakedef.IT_CELLS) != 0)
				        Sbar_DrawPic (224, 0, sb_ammo[3]);
		        }

                Sbar_DrawNum(248, 0, client.cl.stats[quakedef.STAT_AMMO], 3,
                    (client.cl.stats[quakedef.STAT_AMMO] <= 10) ? 1 : 0);
            }

            if (screen.vid.width > 320)
            {
                if (client.cl.gametype == net.GAME_DEATHMATCH)
			        Sbar_MiniDeathmatchOverlay ();
            }
        }

        //=============================================================================

        /*
        ==================
        Sbar_IntermissionNumber

        ==================
        */
        void Sbar_IntermissionNumber (int x, int y, int num, int digits, int color)
        {
        }

        /*
        ==================
        Sbar_DeathmatchOverlay

        ==================
        */
        void Sbar_DeathmatchOverlay ()
        {
        }

        /*
        ==================
        Sbar_DeathmatchOverlay

        ==================
        */
        static void Sbar_MiniDeathmatchOverlay ()
        {
        }

        /*
        ==================
        Sbar_IntermissionOverlay

        ==================
        */
        public static void Sbar_IntermissionOverlay ()
        {
	        wad.qpic_t	pic;
	        int		    dig;
	        int		    num;

	        screen.scr_copyeverything = true;
	        screen.scr_fullupdate = 0;

	        pic = draw.Draw_CachePic ("gfx/complete.lmp");
	        draw.Draw_Pic (64, 24, pic);

            pic = draw.Draw_CachePic("gfx/inter.lmp");
	        draw.Draw_TransPic (0, 56, pic);
        }


        /*
        ==================
        Sbar_FinaleOverlay

        ==================
        */
        public static void Sbar_FinaleOverlay ()
        {
	        wad.qpic_t	pic;

	        screen.scr_copyeverything = true;

	        pic = draw.Draw_CachePic ("gfx/finale.lmp");
	        draw.Draw_TransPic ( (int)((screen.vid.width-pic.width)/2), 16, pic);
        }
    }
}