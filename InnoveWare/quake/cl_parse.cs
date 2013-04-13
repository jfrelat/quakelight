using System;
using Helper;

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
// cl_parse.c  -- parse a message received from the server

namespace quake
{
    public partial class client
    {
        string[] svc_strings =
        {
	        "svc_bad",
	        "svc_nop",
	        "svc_disconnect",
	        "svc_updatestat",
	        "svc_version",		// [long] server version
	        "svc_setview",		// [short] entity number
	        "svc_sound",			// <see code>
	        "svc_time",			// [float] server time
	        "svc_print",			// [string] null terminated string
	        "svc_stufftext",		// [string] stuffed into client's console buffer
						        // the string should be \n terminated
	        "svc_setangle",		// [vec3] set the view angle to this absolute value
        	
	        "svc_serverinfo",		// [long] version
						        // [string] signon string
						        // [string]..[0]model cache [string]...[0]sounds cache
						        // [string]..[0]item cache
	        "svc_lightstyle",		// [byte] [string]
	        "svc_updatename",		// [byte] [string]
	        "svc_updatefrags",	// [byte] [short]
	        "svc_clientdata",		// <shortbits + data>
	        "svc_stopsound",		// <see code>
	        "svc_updatecolors",	// [byte] [byte]
	        "svc_particle",		// [vec3] <variable>
	        "svc_damage",			// [byte] impact [byte] blood [vec3] from
        	
	        "svc_spawnstatic",
	        "OBSOLETE svc_spawnbinary",
	        "svc_spawnbaseline",
        	
	        "svc_temp_entity",		// <variable>
	        "svc_setpause",
	        "svc_signonnum",
	        "svc_centerprint",
	        "svc_killedmonster",
	        "svc_foundsecret",
	        "svc_spawnstaticsound",
	        "svc_intermission",
	        "svc_finale",			// [string] music [string] text
	        "svc_cdtrack",			// [byte] track [byte] looptrack
	        "svc_sellscreen",
	        "svc_cutscene"
        };

        //=============================================================================

        /*
        ===============
        CL_EntityNum

        This error checks and tracks the total number of entities
        ===============
        */
        static render.entity_t CL_EntityNum(int num)
        {
            if (num >= cl.num_entities)
            {
                if (num >= quakedef.MAX_EDICTS)
                    host.Host_Error("CL_EntityNum: " + num + " is an invalid number");
                while (cl.num_entities <= num)
                {
                    cl_entities[cl.num_entities].colormap = screen.vid.colormap;
                    cl.num_entities++;
                }
            }

            return cl_entities[num];
        }

        /*
        ==================
        CL_ParseStartSoundPacket
        ==================
        */
        static void CL_ParseStartSoundPacket()
        {
            double[]    pos = new double[3];
            int 	    channel, ent;
            int 	    sound_num;
            int 	    volume;
            int 	    field_mask;
            double 	    attenuation;  
 	        int		    i;
        	           
            field_mask = common.MSG_ReadByte(); 

            if ((field_mask & net.SND_VOLUME) != 0)
		        volume = common.MSG_ReadByte ();
            else
                volume = sound.DEFAULT_SOUND_PACKET_VOLUME;
        	
            if ((field_mask & net.SND_ATTENUATION) != 0)
                attenuation = common.MSG_ReadByte() / 64.0;
            else
                attenuation = sound.DEFAULT_SOUND_PACKET_ATTENUATION;
        	
	        channel = common.MSG_ReadShort ();
            sound_num = common.MSG_ReadByte();

	        ent = channel >> 3;
	        channel &= 7;

	        if (ent > quakedef.MAX_EDICTS)
		        host.Host_Error ("CL_ParseStartSoundPacket: ent = " + ent);
        	
	        for (i=0 ; i<3 ; i++)
		        pos[i] = common.MSG_ReadCoord ();

            sound.S_StartSound(ent, channel, cl.sound_precache[sound_num], pos, volume / 255.0, attenuation);
        }       

        /*
        ==================
        CL_KeepaliveMessage

        When the client is taking a long time to load stuff, send keepalive messages
        so the server doesn't disconnect.
        ==================
        */
        static double lastmsg;
        static void CL_KeepaliveMessage()
        {
	        double	            time;
	        int		            ret;
	        common.sizebuf_t	old = new common.sizebuf_t();
	        byte[]		        olddata = new byte[8192];

            if (server.sv.active)
                return;		// no need if server is local
            if (client.cls.demoplayback)
		        return;

        // read messages from server, should just be nops
	        common.sizebuf_t.Copy(net.net_message, old);
            Buffer.BlockCopy(net.net_message.data, 0, olddata, 0, net.net_message.cursize);
        	
	        do
	        {
		        ret = CL_GetMessage ();
		        switch (ret)
		        {
		        default:
                    host.Host_Error("CL_KeepaliveMessage: CL_GetMessage failed");
                    break;
		        case 0:
			        break;	// nothing waiting
		        case 1:
                    host.Host_Error("CL_KeepaliveMessage: received a message");
			        break;
		        case 2:
			        if (common.MSG_ReadByte() != net.svc_nop)
				        host.Host_Error ("CL_KeepaliveMessage: datagram wasn't a nop");
			        break;
		        }
	        } while (ret != 0);

            common.sizebuf_t.Copy(old, net.net_message);
            Buffer.BlockCopy(olddata, 0, net.net_message.data, 0, net.net_message.cursize);

        // check time
	        time = sys_linux.Sys_FloatTime ();
	        if (time - lastmsg < 5)
		        return;
	        lastmsg = time;

        // write out a nop
	        console.Con_Printf ("--> client to server keepalive\n");

	        common.MSG_WriteByte (cls.message, net.clc_nop);
	        net.NET_SendMessage (cls.netcon, cls.message);
            common.SZ_Clear (cls.message);
        }

        /*
        ==================
        CL_ParseServerInfo
        ==================
        */
        static void CL_ParseServerInfo ()
        {
	        string	    str;
	        int		    i;
	        int		    nummodels, numsounds;
	        string[]    model_precache = new string[quakedef.MAX_MODELS];
	        string[]	sound_precache = new string[quakedef.MAX_SOUNDS];
        	
	        console.Con_DPrintf ("Serverinfo packet received.\n");
        //
        // wipe the client_state_t struct
        //
	        CL_ClearState ();

        // parse protocol version number
	        i = common.MSG_ReadLong ();
	        if (i != net.PROTOCOL_VERSION)
	        {
		        console.Con_Printf ("Server returned version " + i + ", not " + net.PROTOCOL_VERSION);
		        return;
	        }

        // parse maxclients
	        cl.maxclients = common.MSG_ReadByte ();
	        if (cl.maxclients < 1 || cl.maxclients > quakedef.MAX_SCOREBOARD)
	        {
		        console.Con_Printf("Bad maxclients (" + cl.maxclients + ") from server\n");
		        return;
	        }
	        cl.scores = new scoreboard_t[cl.maxclients];
            for (int kk = 0; kk < cl.maxclients; kk++)
                cl.scores[kk] = new scoreboard_t();

        // parse gametype
	        cl.gametype = common.MSG_ReadByte ();

        // parse signon message
	        str = common.MSG_ReadString ();
	        cl.levelname = str;

        // seperate the printfs so the server message can have a color
	        console.Con_Printf("\n\n\u001d\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001e\u001f\n\n");
	        console.Con_Printf ((char)2 + str + "\n");

        //
        // first we go through and touch all of the precache data that still
        // happens to be in the cache, so precaching something else doesn't
        // needlessly purge it
        //

        // precache models
	        //memset (cl.model_precache, 0, sizeof(cl.model_precache));
	        for (nummodels=1 ; ; nummodels++)
	        {
		        str = common.MSG_ReadString ();
		        if (str.Length == 0)
			        break;
		        if (nummodels==quakedef.MAX_MODELS)
		        {
			        console.Con_Printf ("Server sent too many model precaches\n");
			        return;
		        }
		        model_precache[nummodels] = str;
		        model.Mod_TouchModel (str);
	        }

        // precache sounds
	        //memset (cl.sound_precache, 0, sizeof(cl.sound_precache));
	        for (numsounds=1 ; ; numsounds++)
	        {
		        str = common.MSG_ReadString ();
		        if (str.Length == 0)
			        break;
		        if (numsounds==quakedef.MAX_SOUNDS)
		        {
			        console.Con_Printf ("Server sent too many sound precaches\n");
			        return;
		        }
		        sound_precache[numsounds] = str;
		        sound.S_TouchSound (str);
	        }

            //
            // now we try to load everything else until a cache allocation fails
            //

            for (i = 1; i < nummodels; i++)
            {
                cl.model_precache[i] = model.Mod_ForName(model_precache[i], false);
                if (cl.model_precache[i] == null)
                {
                    console.Con_Printf("Model " + model_precache[i] + " not found\n");
                    return;
                }
                CL_KeepaliveMessage();
            }

            for (i = 1; i < numsounds; i++)
            {
                cl.sound_precache[i] = sound.S_PrecacheSound(sound_precache[i]);
                CL_KeepaliveMessage();
            }

            // local state
            cl_entities[0].model = cl.worldmodel = cl.model_precache[1];

            render.R_NewMap();

	        host.noclip_anglehack = false;		// noclip is turned off at start	
        }

        /*
        ==================
        CL_ParseUpdate

        Parse an entity update message from the server
        If an entities model or origin changes from frame to frame, it must be
        relinked.  Other attributes can change without relinking.
        ==================
        */
        static void CL_ParseUpdate (int bits)
        {
	        int			    i;
        	model.model_t	model;
	        int			    modnum;
	        bool	        forcelink;
        	render.entity_t ent;
	        int			    num;
	        int			    skin;

	        if (cls.signon == SIGNONS - 1)
	        {	// first update is the final signon stage
		        cls.signon = SIGNONS;
		        CL_SignonReply ();
	        }

	        if ((bits & net.U_MOREBITS) != 0)
	        {
		        i = common.MSG_ReadByte ();
		        bits |= (i<<8);
	        }

	        if ((bits & net.U_LONGENTITY) != 0)
		        num = common.MSG_ReadShort ();
	        else
		        num = common.MSG_ReadByte ();

            if (num == -1)
                return;
	        ent = CL_EntityNum (num);

	        if (ent.msgtime != cl.mtime[1])
		        forcelink = true;	// no previous frame to lerp from
	        else
		        forcelink = false;

	        ent.msgtime = cl.mtime[0];

	        if ((bits & net.U_MODEL) != 0)
	        {
		        modnum = common.MSG_ReadByte ();
		        if (modnum >= quakedef.MAX_MODELS)
			        host.Host_Error ("CL_ParseModel: bad modnum");
	        }
	        else
		        modnum = ent.baseline.modelindex;

	        model = cl.model_precache[modnum];
	        if (model != ent.model)
	        {
		        ent.model = model;
	        // automatic animation (torches, etc) can be either all together
	        // or randomized
		        if (model != null)
		        {
                    if (model.synctype == quake.model.synctype_t.ST_RAND)
				        ent.syncbase = (double)(helper.rand()&0x7fff) / 0x7fff;
			        else
				        ent.syncbase = 0.0;
		        }
		        else
			        forcelink = true;	// hack to make null model players work
	        }
        		
	        if ((bits & net.U_FRAME) != 0)
		        ent.frame = common.MSG_ReadByte ();
	        else
		        ent.frame = ent.baseline.frame;

	        if ((bits & net.U_COLORMAP) != 0)
		        i = common.MSG_ReadByte();
	        else
		        i = ent.baseline.colormap;
	        if (i == 0)
		        ent.colormap = screen.vid.colormap;
	        else
	        {
		        if (i > cl.maxclients)
			        sys_linux.Sys_Error ("i >= cl.maxclients");
		        ent.colormap = cl.scores[i-1].translations;
	        }

	        if ((bits & net.U_SKIN) != 0)
		        ent.skinnum = common.MSG_ReadByte();
	        else
		        ent.skinnum = ent.baseline.skin;

	        if ((bits & net.U_EFFECTS) != 0)
                ent.effects = common.MSG_ReadByte();
	        else
		        ent.effects = ent.baseline.effects;

        // shift the known values for interpolation
	        mathlib.VectorCopy (ent.msg_origins[0], ref ent.msg_origins[1]);
            mathlib.VectorCopy (ent.msg_angles[0], ref ent.msg_angles[1]);

            if ((bits & net.U_ORIGIN1) != 0)
		        ent.msg_origins[0][0] = common.MSG_ReadCoord ();
	        else
                ent.msg_origins[0][0] = ent.baseline.origin[0];
	        if ((bits & net.U_ANGLE1) != 0)
                ent.msg_angles[0][0] = common.MSG_ReadAngle();
	        else
		        ent.msg_angles[0][0] = ent.baseline.angles[0];

	        if ((bits & net.U_ORIGIN2) != 0)
                ent.msg_origins[0][1] = common.MSG_ReadCoord();
	        else
		        ent.msg_origins[0][1] = ent.baseline.origin[1];
	        if ((bits & net.U_ANGLE2) != 0)
                ent.msg_angles[0][1] = common.MSG_ReadAngle();
	        else
		        ent.msg_angles[0][1] = ent.baseline.angles[1];

            if ((bits & net.U_ORIGIN3) != 0)
                ent.msg_origins[0][2] = common.MSG_ReadCoord();
	        else
		        ent.msg_origins[0][2] = ent.baseline.origin[2];
	        if ((bits & net.U_ANGLE3) != 0)
                ent.msg_angles[0][2] = common.MSG_ReadAngle();
	        else
		        ent.msg_angles[0][2] = ent.baseline.angles[2];

	        if (( bits & net.U_NOLERP ) != 0)
		        ent.forcelink = true;

	        if ( forcelink )
	        {	// didn't have an update last message
		        mathlib.VectorCopy (ent.msg_origins[0], ref ent.msg_origins[1]);
                mathlib.VectorCopy (ent.msg_origins[0], ref ent.origin);
                mathlib.VectorCopy (ent.msg_angles[0], ref ent.msg_angles[1]);
                mathlib.VectorCopy (ent.msg_angles[0], ref ent.angles);
		        ent.forcelink = true;
	        }
        }

        /*
        ==================
        CL_ParseBaseline
        ==================
        */
        static void CL_ParseBaseline(render.entity_t ent)
        {
            int i;

	        ent.baseline.modelindex = common.MSG_ReadByte ();
            ent.baseline.frame = common.MSG_ReadByte();
            ent.baseline.colormap = common.MSG_ReadByte();
            ent.baseline.skin = common.MSG_ReadByte();
	        for (i=0 ; i<3 ; i++)
	        {
                ent.baseline.origin[i] = common.MSG_ReadCoord();
                ent.baseline.angles[i] = common.MSG_ReadAngle();
	        }
        }

        /*
        ==================
        CL_ParseClientdata

        Server information pertaining to this client only
        ==================
        */
        static void CL_ParseClientdata(int bits)
        {
            int i, j;

            if ((bits & net.SU_VIEWHEIGHT) != 0)
                cl.viewheight = common.MSG_ReadChar();
            else
                cl.viewheight = net.DEFAULT_VIEWHEIGHT;

            if ((bits & net.SU_IDEALPITCH) != 0)
                cl.idealpitch = common.MSG_ReadChar();
            else
                cl.idealpitch = 0;

	        mathlib.VectorCopy (cl.mvelocity[0], ref cl.mvelocity[1]);
            for (i = 0; i < 3; i++)
            {
                if ((bits & (net.SU_PUNCH1 << i)) != 0)
                {
                    cl.punchangle[i] = common.MSG_ReadChar();
                    //sys_linux.Sys_Printf(String.Format("cl.punchangle[{0}]={1:0.######}\n", i, cl.punchangle[i]));
                }
                else
                    cl.punchangle[i] = 0;
                if ((bits & (net.SU_VELOCITY1 << i)) != 0)
                    cl.mvelocity[0][i] = common.MSG_ReadChar()*16;
		        else
			        cl.mvelocity[0][i] = 0;
            }

            // [always sent]	if (bits & SU_ITEMS)
            i = common.MSG_ReadLong();

            if (cl.items != i)
            {	// set flash times
                sbar.Sbar_Changed();
                for (j = 0; j < 32; j++)
                    if ((i & (1 << j)) != 0 && (cl.items & (1 << j)) == 0)
                        cl.item_gettime[j] = cl.time;
                cl.items = i;
            }

            cl.onground = (bits & net.SU_ONGROUND) != 0;
            cl.inwater = (bits & net.SU_INWATER) != 0;

            if ((bits & net.SU_WEAPONFRAME) != 0)
                cl.stats[quakedef.STAT_WEAPONFRAME] = common.MSG_ReadByte();
            else
                cl.stats[quakedef.STAT_WEAPONFRAME] = 0;

            if ((bits & net.SU_ARMOR) != 0)
                i = common.MSG_ReadByte();
            else
                i = 0;
            if (cl.stats[quakedef.STAT_ARMOR] != i)
            {
                cl.stats[quakedef.STAT_ARMOR] = i;
                sbar.Sbar_Changed();
            }

            if ((bits & net.SU_WEAPON) != 0)
                i = common.MSG_ReadByte();
            else
                i = 0;
            if (cl.stats[quakedef.STAT_WEAPON] != i)
            {
                cl.stats[quakedef.STAT_WEAPON] = i;
                sbar.Sbar_Changed();
            }

            i = common.MSG_ReadShort();
            if (cl.stats[quakedef.STAT_HEALTH] != i)
            {
                cl.stats[quakedef.STAT_HEALTH] = i;
                sbar.Sbar_Changed();
            }

            i = common.MSG_ReadByte();
            if (cl.stats[quakedef.STAT_AMMO] != i)
            {
                cl.stats[quakedef.STAT_AMMO] = i;
                sbar.Sbar_Changed();
            }

            for (i = 0; i < 4; i++)
            {
                j = common.MSG_ReadByte();
                if (cl.stats[quakedef.STAT_SHELLS + i] != j)
                {
                    cl.stats[quakedef.STAT_SHELLS + i] = j;
                    sbar.Sbar_Changed();
                }
            }

            i = common.MSG_ReadByte();

            if (common.standard_quake)
            {
                if (cl.stats[quakedef.STAT_ACTIVEWEAPON] != i)
                {
                    cl.stats[quakedef.STAT_ACTIVEWEAPON] = i;
                    sbar.Sbar_Changed();
                }
            }
            else
            {
                if (cl.stats[quakedef.STAT_ACTIVEWEAPON] != (1 << i))
                {
                    cl.stats[quakedef.STAT_ACTIVEWEAPON] = (1 << i);
                    sbar.Sbar_Changed();
                }
            }
        }

        /*
        =====================
        CL_NewTranslation
        =====================
        */
        static void CL_NewTranslation (int slot)
        {
	        int		i, j;
	        int		top, bottom;
	        int	    dest, source;
        	
	        if (slot > cl.maxclients)
		        sys_linux.Sys_Error ("CL_NewTranslation: slot > cl.maxclients");
	        dest = 0;
	        source = 0;
            Buffer.BlockCopy(screen.vid.colormap, 0, cl.scores[slot].translations, 0, cl.scores[slot].translations.Length);
	        top = cl.scores[slot].colors & 0xf0;
	        bottom = (cl.scores[slot].colors &15)<<4;

	        for (i=0 ; i<vid.VID_GRADES ; i++, dest += 256, source+=256)
	        {
		        if (top < 128)	// the artists made some backwards ranges.  sigh.
                    Buffer.BlockCopy(screen.vid.colormap, source + top, cl.scores[slot].translations, dest + render.TOP_RANGE, 16);
		        else
			        for (j=0 ; j<16 ; j++)
                        cl.scores[slot].translations[dest + render.TOP_RANGE + j] = screen.vid.colormap[source+top + 15 - j];
        				
		        if (bottom < 128)
                    Buffer.BlockCopy(screen.vid.colormap, source + bottom, cl.scores[slot].translations, dest + render.BOTTOM_RANGE, 16);
		        else
			        for (j=0 ; j<16 ; j++)
                        cl.scores[slot].translations[dest + render.BOTTOM_RANGE + j] = screen.vid.colormap[source+bottom + 15 - j];
	        }
        }

        /*
        =====================
        CL_ParseStatic
        =====================
        */
        static void CL_ParseStatic ()
        {
        	render.entity_t ent;
	        int		        i;
        		
	        i = cl.num_statics;
	        if (i >= client.MAX_STATIC_ENTITIES)
		        host.Host_Error ("Too many static entities");
	        ent = cl_static_entities[i];
	        cl.num_statics++;
	        CL_ParseBaseline (ent);

        // copy it to the current state
	        ent.model = cl.model_precache[ent.baseline.modelindex];
	        ent.frame = ent.baseline.frame;
	        ent.colormap = screen.vid.colormap;
	        ent.skinnum = ent.baseline.skin;
	        ent.effects = ent.baseline.effects;

	        mathlib.VectorCopy (ent.baseline.origin, ref ent.origin);
            mathlib.VectorCopy(ent.baseline.angles, ref ent.angles);
            render.R_AddEfrags(ent);
        }

        /*
        ===================
        CL_ParseStaticSound
        ===================
        */
        static void CL_ParseStaticSound ()
        {
            double[]    org = new double[3];
	        int			sound_num, vol, atten;
	        int			i;
        	
	        for (i=0 ; i<3 ; i++)
		        org[i] = common.MSG_ReadCoord ();
            sound_num = common.MSG_ReadByte();
            vol = common.MSG_ReadByte();
            atten = common.MSG_ReadByte();

            //sound.S_StaticSound(cl.sound_precache[sound_num], org, vol, atten);
        }

        /*
        =====================
        CL_ParseServerMessage
        =====================
        */
        static void CL_ParseServerMessage ()
        {
	        int			cmd;
	        int			i;
        	
        //
        // if recording demos, copy the message out
        //
	        if (cl_shownet.value == 1)
		        console.Con_Printf (net.net_message.cursize + " ");
	        else if (cl_shownet.value == 2)
                console.Con_Printf("------------------\n");
        	
	        cl.onground = false;	// unless the server says otherwise	
        //
        // parse the message
        //
	        common.MSG_BeginReading ();
        	
	        while (true)
	        {
		        if (common.msg_badread)
			        host.Host_Error ("CL_ParseServerMessage: Bad server message");

		        cmd = common.MSG_ReadByte ();

		        if (cmd == -1)
		        {
			        return;		// end of message
		        }

	        // if the high bit of the command byte is set, it is a fast update
		        if ((cmd & 128) != 0)
		        {
                    CL_ParseUpdate(cmd & 127);
                    continue;
		        }

	        // other commands
		        switch (cmd)
		        {
		        default:
			        host.Host_Error ("CL_ParseServerMessage: Illegible server message\n");
			        break;

                case net.svc_nop:
                    //			Con_Printf ("svc_nop\n");
                    break;

                case net.svc_time:
                    cl.mtime[1] = cl.mtime[0];
                    cl.mtime[0] = common.MSG_ReadFloat();
                    break;

                case net.svc_clientdata:
                    i = common.MSG_ReadShort();
                    CL_ParseClientdata(i);
                    break;

                case net.svc_disconnect:
                    host.Host_EndGame("Server disconnected\n");
                    break;

                case net.svc_print:
                    console.Con_Printf(common.MSG_ReadString());
                    break;

                case net.svc_centerprint:
                    screen.SCR_CenterPrint(common.MSG_ReadString());
                    break;

                case net.svc_stufftext:
                    quake.cmd.Cbuf_AddText(common.MSG_ReadString());
                    break;

                case net.svc_damage:
                    view.V_ParseDamage();
                    break;

                case net.svc_serverinfo:
                    CL_ParseServerInfo();
                    screen.vid.recalc_refdef = true;	// leave intermission full screen
                    break;

                case net.svc_setangle:
                    for (i = 0; i < 3; i++)
                        cl.viewangles[i] = common.MSG_ReadAngle();
                    break;

                case net.svc_setview:
                    cl.viewentity = common.MSG_ReadShort();
                    break;

                case net.svc_lightstyle:
                    i = common.MSG_ReadByte();
                    if (i >= quakedef.MAX_LIGHTSTYLES)
                        sys_linux.Sys_Error("svc_lightstyle > MAX_LIGHTSTYLES");
                    cl_lightstyle[i].map = common.MSG_ReadString();
                    cl_lightstyle[i].length = cl_lightstyle[i].map.Length;
                    break;

                case net.svc_sound:
                    CL_ParseStartSoundPacket();
                    break;

                case net.svc_updatename:
                    sbar.Sbar_Changed();
                    i = common.MSG_ReadByte();
                    if (i >= cl.maxclients)
                        host.Host_Error("CL_ParseServerMessage: svc_updatename > MAX_SCOREBOARD");
                    cl.scores[i].name = common.MSG_ReadString();
                    break;

                case net.svc_updatefrags:
                    sbar.Sbar_Changed();
                    i = common.MSG_ReadByte();
                    if (i >= cl.maxclients)
                        host.Host_Error("CL_ParseServerMessage: svc_updatefrags > MAX_SCOREBOARD");
                    cl.scores[i].frags = common.MSG_ReadShort();
                    break;

                case net.svc_updatecolors:
                    sbar.Sbar_Changed();
                    i = common.MSG_ReadByte();
                    if (i >= cl.maxclients)
                        host.Host_Error("CL_ParseServerMessage: svc_updatecolors > MAX_SCOREBOARD");
                    cl.scores[i].colors = common.MSG_ReadByte();
                    CL_NewTranslation(i);
                    break;

                case net.svc_particle:
                    render.R_ParseParticleEffect();
                    break;

                case net.svc_spawnbaseline:
                    i = common.MSG_ReadShort();
                    // must use CL_EntityNum() to force cl.num_entities up
                    CL_ParseBaseline(CL_EntityNum(i));
                    break;

                case net.svc_spawnstatic:
                    CL_ParseStatic();
                    break;

                case net.svc_temp_entity:
                    CL_ParseTEnt();
                    break;

                case net.svc_signonnum:
                    i = common.MSG_ReadByte();
                    if (i <= cls.signon)
                        host.Host_Error("Received signon " + i + " when at " + cls.signon);
                    cls.signon = i;
                    CL_SignonReply();
                    break;

                case net.svc_killedmonster:
                    cl.stats[quakedef.STAT_MONSTERS]++;
                    break;

                case net.svc_foundsecret:
                    cl.stats[quakedef.STAT_SECRETS]++;
                    break;

                case net.svc_updatestat:
                    i = common.MSG_ReadByte();
                    if (i < 0 || i >= quakedef.MAX_CL_STATS)
                        sys_linux.Sys_Error("svc_updatestat: " + i + " is invalid");
                    cl.stats[i] = common.MSG_ReadLong(); ;
                    break;

                case net.svc_spawnstaticsound:
                    CL_ParseStaticSound();
                    break;

                case net.svc_cdtrack:
                    cl.cdtrack = common.MSG_ReadByte();
                    cl.looptrack = common.MSG_ReadByte();
                    break;
                }
	        }
        }
    }
}
