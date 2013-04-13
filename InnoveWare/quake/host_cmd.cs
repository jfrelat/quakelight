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
    public partial class host
    {
        public static int	current_skill;

        /*
        ==================
        Host_Quit_f
        ==================
        */

        public static void Host_Quit_f()
        {
            if (keys.key_dest != keys.keydest_t.key_console && client.cls.state != client.cactive_t.ca_dedicated)
            {
                menu.M_Menu_Quit_f();
                return;
            }
            client.CL_Disconnect();
            host.Host_ShutdownServer(false);

            sys_linux.Sys_Quit();
        }

        /*
        ==================
        Host_Status_f
        ==================
        */
        static void Host_Status_f ()
        {
        }

        /*
        ==================
        Host_God_f

        Sets client to godmode
        ==================
        */
        static void Host_God_f()
        {
        }

        static void Host_Notarget_f ()
        {
        }

        public static bool noclip_anglehack;

        static void Host_Noclip_f ()
        {
        }

        /*
        ==================
        Host_Fly_f

        Sets client to flymode
        ==================
        */
        static void Host_Fly_f()
        {
        }
        
        /*
        ==================
        Host_Ping_f

        ==================
        */
        static void Host_Ping_f ()
        {
        }

        /*
        ===============================================================================

        SERVER TRANSITIONS

        ===============================================================================
        */
        
        /*
        ======================
        Host_Map_f

        handle a 
        map <servername>
        command from the console.  Active clients are kicked off.
        ======================
        */
        static void Host_Map_f()
        {
	        int		i;
	        string	name;

            if (cmd.cmd_source != cmd.cmd_source_t.src_command)
		        return;

	        client.cls.demonum = -1;		// stop demo loop in case this fails

	        client.CL_Disconnect ();
	        Host_ShutdownServer(false);		

	        keys.key_dest = keys.keydest_t.key_game;			// remove console or menu
	        screen.SCR_BeginLoadingPlaque ();

	        client.cls.mapstring = "";
	        for (i=0 ; i<cmd.Cmd_Argc() ; i++)
	        {
		        client.cls.mapstring += cmd.Cmd_Argv(i);
		        client.cls.mapstring += " ";
	        }
	        client.cls.mapstring += "\n";

	        server.svs.serverflags = 0;			// haven't completed an episode yet
	        name = cmd.Cmd_Argv(1);
	        server.SV_SpawnServer (name);
	        if (!server.sv.active)
		        return;

	        if (client.cls.state != client.cactive_t.ca_dedicated)
	        {
		        client.cls.spawnparms = "";

		        for (i=2 ; i<cmd.Cmd_Argc() ; i++)
		        {
			        client.cls.spawnparms += cmd.Cmd_Argv(i);
                    client.cls.spawnparms += " ";
		        }

                cmd.Cmd_ExecuteString("connect local\0".ToCharArray(), cmd.cmd_source_t.src_command);
	        }	
        }

        /*
        ==================
        Host_Changelevel_f

        Goes to a new map, taking all clients along
        ==================
        */
        static void Host_Changelevel_f()
        {
        }

        /*
        ==================
        Host_Restart_f

        Restarts the current server for a dead player
        ==================
        */
        static void Host_Restart_f()
        {
        }

        /*
        ==================
        Host_Reconnect_f

        This command causes the client to wait for the signon messages again.
        This is sent just before a server changes levels
        ==================
        */
        static void Host_Reconnect_f()
        {
            screen.SCR_BeginLoadingPlaque();
            client.cls.signon = 0;		// need new connection messages
        }

        /*
        =====================
        Host_Connect_f

        User command to connect to server
        =====================
        */
        static void Host_Connect_f()
        {
	        string	name;
        	
	        client.cls.demonum = -1;		// stop demo loop in case this fails
            if (client.cls.demoplayback)
	        {
                client.CL_StopPlayback();
                client.CL_Disconnect();
	        }
	        name = cmd.Cmd_Argv(1);
	        client.CL_EstablishConnection (name);
	        Host_Reconnect_f ();
        }
        
        /*
        ===============================================================================

        LOAD / SAVE GAME

        ===============================================================================
        */

        const int	SAVEGAME_VERSION = 5;

        /*
        ===============
        Host_SavegameComment

        Writes a SAVEGAME_COMMENT_LENGTH character comment describing the current 
        ===============
        */
        void Host_SavegameComment (string text)
        {
        }
        
        /*
        ===============
        Host_Savegame_f
        ===============
        */
        static void Host_Savegame_f()
        {
        }
        
        /*
        ===============
        Host_Loadgame_f
        ===============
        */
        static void Host_Loadgame_f()
        {
        }

        //============================================================================

        /*
        ======================
        Host_Name_f
        ======================
        */
        static void Host_Name_f()
        {
	        string	newName;

	        if (cmd.Cmd_Argc () == 1)
	        {
		        console.Con_Printf ("\"name\" is \"" + client.cl_name._string + "\"\n");
		        return;
	        }
	        if (cmd.Cmd_Argc () == 2)
		        newName = cmd.Cmd_Argv(1);	
	        else
		        newName = cmd.Cmd_Args();

	        if (cmd.cmd_source == cmd.cmd_source_t.src_command)
	        {
		        if (client.cl_name._string.CompareTo(newName) == 0)
			        return;
		        cvar_t.Cvar_Set ("_cl_name", newName);
		        if (client.cls.state == client.cactive_t.ca_connected)
			        cmd.Cmd_ForwardToServer ();
		        return;
	        }

	        if (host_client.name.Length != 0 && host_client.name.CompareTo("unconnected") != 0 )
		        if (host_client.name.CompareTo(newName) != 0)
			        console.Con_Printf (host_client.name + " renamed to " + newName + "\n");
	        host_client.name = newName;
	        host_client.edict.v.netname = prog.getStringIndex(host_client.name) - 15000;
        	
        // send notification to all clients
        	
	        common.MSG_WriteByte (server.sv.reliable_datagram, net.svc_updatename);
            common.MSG_WriteByte(server.sv.reliable_datagram, host_client.index);
            common.MSG_WriteString(server.sv.reliable_datagram, host_client.name);
        }

        static void Host_Version_f()
        {
	        console.Con_Printf ("Version " + quakedef.VERSION + "\n");
	        //console.Con_Printf ("Exe: " + _TIME_ + " " + __DATE__ + "\n");
        }

        static void Host_Say(bool teamonly)
        {
        }

        static void Host_Say_f()
        {
	        Host_Say(false);
        }

        static void Host_Say_Team_f()
        {
	        Host_Say(true);
        }

        static void Host_Tell_f()
        {
        }
        
        /*
        ==================
        Host_Color_f
        ==================
        */
        static void Host_Color_f()
        {
        }

        /*
        ==================
        Host_Kill_f
        ==================
        */
        static void Host_Kill_f ()
        {
        }
        
        /*
        ==================
        Host_Pause_f
        ==================
        */
        static void Host_Pause_f()
        {
        }

        //===========================================================================
        
        /*
        ==================
        Host_PreSpawn_f
        ==================
        */
        static void Host_PreSpawn_f()
        {
            if (cmd.cmd_source == cmd.cmd_source_t.src_command)
            {
                console.Con_Printf("prespawn is not valid from the console\n");
                return;
            }

            if (host_client.spawned)
            {
                console.Con_Printf("prespawn not valid -- allready spawned\n");
                return;
            }

            common.SZ_Write(host_client.message, server.sv.signon.data, server.sv.signon.cursize);
            common.MSG_WriteByte(host_client.message, net.svc_signonnum);
            common.MSG_WriteByte(host_client.message, 2);
            host_client.sendsignon = true;
        }

        /*
        ==================
        Host_Spawn_f
        ==================
        */
        static void Host_Spawn_f()
        {
            int             i;
            server.client_t client;
            prog.edict_t    ent;

            if (cmd.cmd_source == cmd.cmd_source_t.src_command)
            {
                console.Con_Printf("spawn is not valid from the console\n");
                return;
            }

            if (host_client.spawned)
            {
                console.Con_Printf("Spawn not valid -- allready spawned\n");
                return;
            }

            // run the entrance script
            if (server.sv.loadgame)
            {	// loaded games are fully inited allready
                // if this is the last client to be connected, unpause
                server.sv.paused = false;
            }
            else
            {
                // set up the edict
                ent = host_client.edict;

                ent.v.clear();
                ent.v.colormap = prog.NUM_FOR_EDICT(ent);
                ent.v.team = (host_client.colors & 15) + 1;
                ent.v.netname = prog.getStringIndex(host_client.name) - 15000;

                // copy spawn parms out of the client_t

                for (i=0 ; i< server.NUM_SPAWN_PARMS ; i++)
                    prog.pr_globals_write(43 + i, host_client.spawn_parms[i]);

                // call the spawn function

                prog.pr_global_struct[0].time = server.sv.time;
                prog.pr_global_struct[0].self = prog.EDICT_TO_PROG(server.sv_player);
                prog.PR_ExecuteProgram(prog.pr_functions[prog.pr_global_struct[0].ClientConnect]);

                if ((sys_linux.Sys_FloatTime() - host_client.netconnection.connecttime) <= server.sv.time)
	                sys_linux.Sys_Printf (host_client.name + " entered the game\n");

                prog.PR_ExecuteProgram (prog.pr_functions[prog.pr_global_struct[0].PutClientInServer]);
            }
             
            // send all current names, colors, and frag counts
            common.SZ_Clear(host_client.message);

            // send time of update
            common.MSG_WriteByte(host_client.message, net.svc_time);
            common.MSG_WriteFloat(host_client.message, server.sv.time);

            for (i = 0; i < server.svs.maxclients; i++)
            {
                client = server.svs.clients[i];
                common.MSG_WriteByte(host_client.message, net.svc_updatename);
                common.MSG_WriteByte(host_client.message, i);
                common.MSG_WriteString(host_client.message, client.name);
                common.MSG_WriteByte(host_client.message, net.svc_updatefrags);
                common.MSG_WriteByte(host_client.message, i);
                common.MSG_WriteShort(host_client.message, client.old_frags);
                common.MSG_WriteByte(host_client.message, net.svc_updatecolors);
                common.MSG_WriteByte(host_client.message, i);
                common.MSG_WriteByte(host_client.message, client.colors);
            }

            // send all current light styles
            for (i = 0; i < quakedef.MAX_LIGHTSTYLES; i++)
            {
                common.MSG_WriteByte(host_client.message, net.svc_lightstyle);
                common.MSG_WriteByte(host_client.message, (char)i);
                common.MSG_WriteString(host_client.message, server.sv.lightstyles[i]);
            }

            //
            // send some stats
            //
            common.MSG_WriteByte(host_client.message, net.svc_updatestat);
            common.MSG_WriteByte(host_client.message, quakedef.STAT_TOTALSECRETS);
            common.MSG_WriteLong(host_client.message, (int)prog.pr_global_struct[0].total_secrets);

            common.MSG_WriteByte(host_client.message, net.svc_updatestat);
            common.MSG_WriteByte(host_client.message, quakedef.STAT_TOTALMONSTERS);
            common.MSG_WriteLong(host_client.message, (int)prog.pr_global_struct[0].total_monsters);

            common.MSG_WriteByte(host_client.message, net.svc_updatestat);
            common.MSG_WriteByte(host_client.message, quakedef.STAT_SECRETS);
            common.MSG_WriteLong(host_client.message, (int)prog.pr_global_struct[0].found_secrets);

            common.MSG_WriteByte(host_client.message, net.svc_updatestat);
            common.MSG_WriteByte(host_client.message, quakedef.STAT_MONSTERS);
            common.MSG_WriteLong(host_client.message, (int)prog.pr_global_struct[0].killed_monsters);
            
            //
            // send a fixangle
            // Never send a roll angle, because savegames can catch the server
            // in a state where it is expecting the client to correct the angle
            // and it won't happen if the game was just loaded, so you wind up
            // with a permanent head tilt
            ent = prog.EDICT_NUM(1 + host_client.index);
            common.MSG_WriteByte(host_client.message, net.svc_setangle);
            for (i = 0; i < 2; i++)
                common.MSG_WriteAngle(host_client.message, ent.v.angles[i]);
            common.MSG_WriteAngle(host_client.message, 0);

            server.SV_WriteClientdataToMessage(server.sv_player, host_client.message);

            common.MSG_WriteByte(host_client.message, net.svc_signonnum);
            common.MSG_WriteByte(host_client.message, 3);
            host_client.sendsignon = true;
        }

        /*
        ==================
        Host_Begin_f
        ==================
        */
        static void Host_Begin_f()
        {
            if (cmd.cmd_source == cmd.cmd_source_t.src_command)
            {
                console.Con_Printf("begin is not valid from the console\n");
                return;
            }

            host_client.spawned = true;
        }

        //===========================================================================

        /*
        ==================
        Host_Kick_f

        Kicks a user off of the server
        ==================
        */
        static void Host_Kick_f()
        {
        }

        /*
        ===============================================================================

        DEBUGGING TOOLS

        ===============================================================================
        */

        /*
        ==================
        Host_Give_f
        ==================
        */
        static void Host_Give_f()
        {
        }

        /*
        ==================
        Host_Viewmodel_f
        ==================
        */
        static void Host_Viewmodel_f()
        {
        }

        /*
        ==================
        Host_Viewframe_f
        ==================
        */
        static void Host_Viewframe_f()
        {
        }


        /*
        ==================
        Host_Viewnext_f
        ==================
        */
        static void Host_Viewnext_f()
        {
        }

        /*
        ==================
        Host_Viewprev_f
        ==================
        */
        static void Host_Viewprev_f()
        {
        }

        /*
        ===============================================================================

        DEMO LOOP CONTROL

        ===============================================================================
        */

        /*
        ==================
        Host_Startdemos_f
        ==================
        */
        static void Host_Startdemos_f()
        {
	        int		i, c;

	        if (client.cls.state == client.cactive_t.ca_dedicated)
	        {
		        if (!server.sv.active)
			        cmd.Cbuf_AddText ("map start\n");
		        return;
	        }

	        c = cmd.Cmd_Argc() - 1;
	        if (c > client.MAX_DEMOS)
	        {
                console.Con_Printf("Max " + client.MAX_DEMOS + " demos in demoloop\n");
                c = client.MAX_DEMOS;
	        }
	        console.Con_Printf (c + " demo(s) in loop\n");

	        for (i=1 ; i<c+1 ; i++)
		        client.cls.demos[i-1] = cmd.Cmd_Argv(i);

            if (client.cls.demonum != -1 && !client.cls.demoplayback)
	        {
                client.cls.demonum = 0;
                client.CL_NextDemo();
	        }
	        else
                client.cls.demonum = -1;
        }

        /*
        ==================
        Host_Demos_f

        Return to looping demos
        ==================
        */
        static void Host_Demos_f()
        {
            if (client.cls.state == client.cactive_t.ca_dedicated)
                return;
            if (client.cls.demonum == -1)
                client.cls.demonum = 1;
            client.CL_Disconnect_f();
            client.CL_NextDemo();
        }

        /*
        ==================
        Host_Stopdemo_f

        Return to looping demos
        ==================
        */
        static void Host_Stopdemo_f()
        {
        }

        //=============================================================================

        /*
        ==================
        Host_InitCommands
        ==================
        */
        public static void Host_InitCommands ()
        {
	        cmd.Cmd_AddCommand ("status", Host_Status_f);
            cmd.Cmd_AddCommand("quit", Host_Quit_f);
            cmd.Cmd_AddCommand("god", Host_God_f);
            cmd.Cmd_AddCommand("notarget", Host_Notarget_f);
            cmd.Cmd_AddCommand("fly", Host_Fly_f);
            cmd.Cmd_AddCommand("map", Host_Map_f);
            cmd.Cmd_AddCommand("restart", Host_Restart_f);
            cmd.Cmd_AddCommand("changelevel", Host_Changelevel_f);
            cmd.Cmd_AddCommand("connect", Host_Connect_f);
            cmd.Cmd_AddCommand("reconnect", Host_Reconnect_f);
            cmd.Cmd_AddCommand("name", Host_Name_f);
            cmd.Cmd_AddCommand("noclip", Host_Noclip_f);
            cmd.Cmd_AddCommand("version", Host_Version_f);
            cmd.Cmd_AddCommand("say", Host_Say_f);
            cmd.Cmd_AddCommand("say_team", Host_Say_Team_f);
            cmd.Cmd_AddCommand("tell", Host_Tell_f);
            cmd.Cmd_AddCommand("color", Host_Color_f);
            cmd.Cmd_AddCommand("kill", Host_Kill_f);
            cmd.Cmd_AddCommand("pause", Host_Pause_f);
            cmd.Cmd_AddCommand("spawn", Host_Spawn_f);
            cmd.Cmd_AddCommand("begin", Host_Begin_f);
            cmd.Cmd_AddCommand("prespawn", Host_PreSpawn_f);
            cmd.Cmd_AddCommand("kick", Host_Kick_f);
            cmd.Cmd_AddCommand("ping", Host_Ping_f);
            cmd.Cmd_AddCommand("load", Host_Loadgame_f);
            cmd.Cmd_AddCommand("save", Host_Savegame_f);
            cmd.Cmd_AddCommand("give", Host_Give_f);

            cmd.Cmd_AddCommand("startdemos", Host_Startdemos_f);
            cmd.Cmd_AddCommand("demos", Host_Demos_f);
            cmd.Cmd_AddCommand("stopdemo", Host_Stopdemo_f);

            cmd.Cmd_AddCommand("viewmodel", Host_Viewmodel_f);
            cmd.Cmd_AddCommand("viewframe", Host_Viewframe_f);
            cmd.Cmd_AddCommand("viewnext", Host_Viewnext_f);
            cmd.Cmd_AddCommand("viewprev", Host_Viewprev_f);

            cmd.Cmd_AddCommand("mcache", model.Mod_Print);
        }
    }
}