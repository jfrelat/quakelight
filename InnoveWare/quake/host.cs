using System;
using System.Diagnostics;

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
// host.c -- coordinates spawning and killing of local servers

namespace quake
{
    public class host_abortserver : Exception
    {
        public host_abortserver() { }
    }

    public partial class host
    {
        /*

        A server can allways be started, even if the system started out as a client
        to a remote system.

        A client can NOT be started if the system started as a dedicated server.

        Memory is cleared / released when a server or client begins, not when they end.

        */

        public static bool	    host_initialized;		// true if into command execution

        public static double	host_frametime;
        public static double	host_time;
        public static double	realtime;				// without any filtering or bounding
        static double       oldrealtime;			// last frame run
        public static int		host_framecount;

        int			host_hunklevel;

        static int		minimum_memory;

        public static server.client_t   host_client;			// current client

        public static byte[]	host_basepal;
        public static byte[]    host_colormap;

        static cvar_t	host_framerate = new cvar_t("host_framerate","0");	// set for slow motion
        static cvar_t   host_speeds = new cvar_t("host_speeds", "0");			// set for running times

        static cvar_t   sys_ticrate = new cvar_t("sys_ticrate", "0.05");
        static cvar_t   serverprofile = new cvar_t("serverprofile", "0");

        static cvar_t   fraglimit = new cvar_t("fraglimit", "0", false, true);
        static cvar_t   timelimit = new cvar_t("timelimit", "0", false, true);
        static cvar_t   teamplay = new cvar_t("teamplay", "0", false, true);

        static cvar_t   samelevel = new cvar_t("samelevel", "0");
        static cvar_t   noexit = new cvar_t("noexit", "0", false, true);

        public static cvar_t	developer = new cvar_t("developer","1");

        public static cvar_t    skill = new cvar_t("skill", "1");						// 0 - 3
        public static cvar_t    deathmatch = new cvar_t("deathmatch", "0");			// 0, 1, or 2
        public static cvar_t    coop = new cvar_t("coop", "0");			// 0 or 1

        static cvar_t   pausable = new cvar_t("pausable", "1");

        static cvar_t   temp1 = new cvar_t("temp1", "0");

        /*
        ================
        Host_EndGame
        ================
        */
        public static void Host_EndGame (string message)
        {
	        string      @string;
        	
	        @string = message;
	        console.Con_DPrintf ("Host_EndGame: " + @string + "\n");

            if (server.sv.active)
                host.Host_ShutdownServer(false);

	        if (client.cls.state == client.cactive_t.ca_dedicated)
		        sys_linux.Sys_Error ("Host_EndGame: " + @string + "\n");	// dedicated servers exit
        	
	        if (client.cls.demonum != -1)
		        client.CL_NextDemo ();
	        else
                client.CL_Disconnect();
            throw new host_abortserver();
        }

        /*
        ================
        Host_Error

        This shuts down both the client and server
        ================
        */
        static bool inerror = false;
        public static void Host_Error(string error)
        {
        }

        /*
        ================
        Host_FindMaxClients
        ================
        */
        static void	Host_FindMaxClients ()
        {
            int i;

            server.svs.maxclients = 1;

            i = common.COM_CheckParm("-dedicated");
            if (i != 0)
            {
                client.cls.state = client.cactive_t.ca_dedicated;
                if (i != (common.com_argc - 1))
                {
                    server.svs.maxclients = int.Parse(common.com_argv[i + 1]);
                }
                else
                    server.svs.maxclients = 8;
            }
            else
                client.cls.state = client.cactive_t.ca_disconnected;

            i = common.COM_CheckParm("-listen");
            if (i != 0)
            {
                if (client.cls.state == client.cactive_t.ca_dedicated)
                    sys_linux.Sys_Error("Only one of -dedicated or -listen can be specified");
                if (i != (common.com_argc - 1))
                    server.svs.maxclients = int.Parse(common.com_argv[i + 1]);
                else
                    server.svs.maxclients = 8;
            }
            if (server.svs.maxclients < 1)
                server.svs.maxclients = 8;
            else if (server.svs.maxclients > quakedef.MAX_SCOREBOARD)
                server.svs.maxclients = quakedef.MAX_SCOREBOARD;

            server.svs.maxclientslimit = server.svs.maxclients;
            if (server.svs.maxclientslimit < 4)
                server.svs.maxclientslimit = 4;
            server.svs.clients = new server.client_t[server.svs.maxclientslimit];
            for (int kk = 0; kk < server.svs.maxclientslimit; kk++)
            {
                server.svs.clients[kk] = new server.client_t();
                server.svs.clients[kk].index = kk;
            }

            if (server.svs.maxclients > 1)
                cvar_t.Cvar_SetValue("deathmatch", 1.0);
            else
                cvar_t.Cvar_SetValue("deathmatch", 0.0);
        }


        /*
        =======================
        Host_InitLocal
        ======================
        */
        static void Host_InitLocal ()
        {
            host.Host_InitCommands();

            cvar_t.Cvar_RegisterVariable(host_framerate);
            cvar_t.Cvar_RegisterVariable(host_speeds);

            cvar_t.Cvar_RegisterVariable(sys_ticrate);
            cvar_t.Cvar_RegisterVariable(serverprofile);

            cvar_t.Cvar_RegisterVariable(fraglimit);
            cvar_t.Cvar_RegisterVariable(timelimit);
            cvar_t.Cvar_RegisterVariable(teamplay);
            cvar_t.Cvar_RegisterVariable(samelevel);
            cvar_t.Cvar_RegisterVariable(noexit);
            cvar_t.Cvar_RegisterVariable(skill);
            cvar_t.Cvar_RegisterVariable(developer);
            cvar_t.Cvar_RegisterVariable(deathmatch);
            cvar_t.Cvar_RegisterVariable(coop);

            cvar_t.Cvar_RegisterVariable(pausable);

            cvar_t.Cvar_RegisterVariable(temp1);

            Host_FindMaxClients();
        	
	        host_time = 1.0;		// so a think at time 0 won't get called
        }


        /*
        ===============
        Host_WriteConfiguration

        Writes key bindings and archived cvars to config.cfg
        ===============
        */
        void Host_WriteConfiguration ()
        {
        }


        /*
        =================
        SV_ClientPrintf

        Sends text across to be displayed 
        FIXME: make this just a stuffed echo?
        =================
        */
        void SV_ClientPrintf (string fmt)
        {
        }

        /*
        =================
        SV_BroadcastPrintf

        Sends text to all active clients
        =================
        */
        void SV_BroadcastPrintf (string fmt)
        {
        }

        /*
        =================
        Host_ClientCommands

        Send text over to the client to be executed
        =================
        */
        void Host_ClientCommands (string fmt)
        {
        }

        /*
        =====================
        SV_DropClient

        Called when the player is getting totally kicked off the host
        if (crash = true), don't bother sending signofs
        =====================
        */
        void SV_DropClient (bool crash)
        {
        }

        /*
        ==================
        Host_ShutdownServer

        This only happens at the end of a game, not between levels
        ==================
        */
        public static void Host_ShutdownServer(bool crash)
        {
            if (!server.sv.active)
                return;

            server.sv.active = false;

            // stop all client sounds immediately
            if (client.cls.state == client.cactive_t.ca_connected)
                client.CL_Disconnect();
        }


        /*
        ================
        Host_ClearMemory

        This clears all the memory used by both the client and server, but does
        not reinitialize anything.
        ================
        */
        public static void Host_ClearMemory ()
        {
	        console.Con_DPrintf ("Clearing memory\n");
            model.Mod_ClearAll();

	        client.cls.signon = 0;
        }
        
        //============================================================================
        
        /*
        ===================
        Host_FilterTime

        Returns false if the time is too short to run a frame
        ===================
        */
        static bool Host_FilterTime (double time)
        {
            realtime += time;

            if (oldrealtime > realtime)
                return false;

            if (!client.cls.timedemo && realtime - oldrealtime < 1.0 / 72.0)
                return false;		// framerate is too high

            host_frametime = realtime - oldrealtime;
            oldrealtime = realtime;

            if (host_framerate.value > 0)
                host_frametime = host_framerate.value;
            else
            {	// don't allow really long or short frames
                if (host_frametime > 0.1)
                    host_frametime = 0.1;
                if (host_frametime < 0.001)
                    host_frametime = 0.001;
            }

            return true;
        }


        /*
        ===================
        Host_GetConsoleCommands

        Add them exactly as if they had been typed at the console
        ===================
        */
        void Host_GetConsoleCommands ()
        {
        }


        /*
        ==================
        Host_ServerFrame

        ==================
        */
        static void Host_ServerFrame ()
        {
        // set the time and clear the general datagram
	        server.SV_ClearDatagram ();
        	
        // check for new clients
            server.SV_CheckForNewClients();

        // read client messages
            server.SV_RunClients();
        	
        // move things around and think
        // always pause in single player if in console or menus
            if (!server.sv.paused && (server.svs.maxclients > 1 || keys.key_dest == keys.keydest_t.key_game))
                server.SV_Physics();

        // send all messages to the clients
            server.SV_SendClientMessages();
        }


        /*
        ==================
        Host_Frame

        Runs all active servers
        ==================
        */
        static double time1 = 0;
        static double time2 = 0;
        static double time3 = 0;
        static void _Host_Frame(double time)
        {
	        int			pass1, pass2, pass3;

            try
            {
                // decide the simulation time
                if (!Host_FilterTime(time))
                    return;			// don't run too fast, or packets will flood out

                // process console commands
                cmd.Cbuf_Execute();

	            net.NET_Poll();

            // if running the server locally, make intentions now
	            if (server.sv.active)
                    client.CL_SendCmd();

            //-------------------
            //
            // server operations
            //
            //-------------------

                if (server.sv.active)
                    Host_ServerFrame();

            //-------------------
            //
            // client operations
            //
            //-------------------

            // if running the server remotely, send intentions now after
            // the incoming messages have been read
	            if (!server.sv.active)
		            client.CL_SendCmd ();

                host_time += host_frametime;

                // fetch results from server
                if (client.cls.state == client.cactive_t.ca_connected)
                {
                    client.CL_ReadFromServer();
                }

                screen.SCR_UpdateScreen();

                // update audio
                if (client.cls.signon == client.SIGNONS)
                {
                    sound.S_Update(render.r_origin, render.vpn, render.vright, render.vup);
                    client.CL_DecayLights();
                }
                else
                    sound.S_Update(mathlib.vec3_origin, mathlib.vec3_origin, mathlib.vec3_origin, mathlib.vec3_origin);

                host_framecount++;
            }
            catch (host_abortserver)
            {
                return;
            }
        }

        static double timetotal;
        static int timecount;
        public static void Host_Frame(double time)
        {
	        double	time1, time2;
	        int		i, c, m;

	        _Host_Frame (time);
        }

        //============================================================================


        const int VCR_SIGNATURE = 0x56435231;
        // "VCR1"

        static void Host_InitVCR(quakedef.quakeparms_t parms)
        {
            int i, len, n;

            if (common.COM_CheckParm("-playback") != 0)
            {
            }

            if ((n = common.COM_CheckParm("-record")) != 0)
            {
            }
        }

        /*
        ====================
        Host_Init
        ====================
        */
        public static void Host_Init(quakedef.quakeparms_t parms)
        {
	        if (common.standard_quake)
		        minimum_memory = quakedef.MINIMUM_MEMORY;
	        else
                minimum_memory = quakedef.MINIMUM_MEMORY_LEVELPAK;

            cmd.Cbuf_Init();
            cmd.Cmd_Init();
            view.V_Init();
            chase.Chase_Init();
            Host_InitVCR(parms);
            common.COM_Init(null);
            Host_InitLocal();
            wad.W_LoadWadFile("gfx.wad");
            keys.Key_Init();
            console.Con_Init();
            prog.PR_Init();
            menu.M_Init();
            model.Mod_Init();
            net.NET_Init();
            server.SV_Init();

            render.R_InitTextures();		// needed even for dedicated servers

		    host_basepal = common.COM_LoadHunkFile ("gfx/palette.lmp");
		    if (host_basepal == null)
			    sys_linux.Sys_Error ("Couldn't load gfx/palette.lmp");
		    host_colormap = common.COM_LoadHunkFile ("gfx/colormap.lmp");
		    if (host_colormap == null)
			    sys_linux.Sys_Error ("Couldn't load gfx/colormap.lmp");

            // on non win32, mouse comes before video for security reasons
		    //IN_Init ();
		    vid.VID_Init (host_basepal);

		    draw.Draw_Init ();
		    screen.SCR_Init ();
		    render.R_Init ();
    	    // on Win32, sound initialization has to come before video initialization, so we
	        // can put up a popup if the sound hardware is in use
		    sound.S_Init ();
		    //CDAudio_Init ();
            sbar.Sbar_Init();
		    client.CL_Init ();

            cmd.Cbuf_InsertText("exec quake.rc\n");

	        host_initialized = true;
        }


        /*
        ===============
        Host_Shutdown

        FIXME: this is a callback from Sys_Quit and Sys_Error.  It would be better
        to run quit through here before the final handoff to the sys code.
        ===============
        */
        static bool isdown = false;
        void Host_Shutdown()
        {
	        if (isdown)
	        {
		        Debug.WriteLine ("recursive shutdown");
		        return;
	        }
	        isdown = true;
        }
    }
}
