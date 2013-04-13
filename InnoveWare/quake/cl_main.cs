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
// cl_main.c  -- client main loop

namespace quake
{
    public partial class client
    {
        // we need to declare some mouse variables here, because the menu system
        // references them even when on a unix system.

        // these two are not intended to be set directly
        public static cvar_t	cl_name = new cvar_t("_cl_name", "player", true);
        public static cvar_t    cl_color = new cvar_t("_cl_color", "0", true);

        static cvar_t   cl_shownet = new cvar_t("cl_shownet", "0");	// can be 0, 1, or 2
        static cvar_t   cl_nolerp = new cvar_t("cl_nolerp", "0");

        static cvar_t   lookspring = new cvar_t("lookspring", "0", true);
        static cvar_t   lookstrafe = new cvar_t("lookstrafe", "0", true);
        static cvar_t   sensitivity = new cvar_t("sensitivity", "3", true);

        static cvar_t   m_pitch = new cvar_t("m_pitch", "0.022", true);
        static cvar_t   m_yaw = new cvar_t("m_yaw", "0.022", true);
        static cvar_t   m_forward = new cvar_t("m_forward", "1", true);
        static cvar_t   m_side = new cvar_t("m_side", "0.8", true);


        public static client_static_t	cls = new client_static_t();
        public static client_state_t    cl = new client_state_t();
        // FIXME: put these on hunk?
        static render.efrag_t[]	        cl_efrags = new render.efrag_t[MAX_EFRAGS];
        public static render.entity_t[] cl_entities = new render.entity_t[quakedef.MAX_EDICTS];
        static render.entity_t[]        cl_static_entities = new render.entity_t[MAX_STATIC_ENTITIES];
        public static lightstyle_t[]    cl_lightstyle = new lightstyle_t[quakedef.MAX_LIGHTSTYLES];
        public static dlight_t[]        cl_dlights = new dlight_t[MAX_DLIGHTS];

        public static int				cl_numvisedicts;
        public static render.entity_t[] cl_visedicts = new render.entity_t[MAX_VISEDICTS];

        static client()
        {
            int kk;
            for (kk = 0; kk < MAX_EFRAGS; kk++) cl_efrags[kk] = new render.efrag_t();
            for (kk = 0; kk < quakedef.MAX_EDICTS; kk++) cl_entities[kk] = new render.entity_t();
            for (kk = 0; kk < MAX_STATIC_ENTITIES; kk++) cl_static_entities[kk] = new render.entity_t();
            for (kk = 0; kk < MAX_TEMP_ENTITIES; kk++) cl_temp_entities[kk] = new render.entity_t();
            for (kk = 0; kk < quakedef.MAX_LIGHTSTYLES; kk++) cl_lightstyle[kk] = new lightstyle_t();
            for (kk = 0; kk < MAX_DLIGHTS; kk++) cl_dlights[kk] = new dlight_t();
            for (kk = 0; kk < MAX_BEAMS; kk++) cl_beams[kk] = new beam_t();
        }

        /*
        =====================
        CL_ClearState

        =====================
        */
        public static void CL_ClearState()
        {
            int i;

            if (!server.sv.active)
                host.Host_ClearMemory();

            // wipe the entire cl structure
            cl = new client_state_t();

            common.SZ_Clear(cls.message);

            //
            // allocate the efrags and chain together into a free list
            //
            cl.free_efrags = cl_efrags[0];
            for (i = 0; i < MAX_EFRAGS - 1; i++)
                cl_efrags[i].entnext = cl_efrags[i + 1];
            cl_efrags[i].entnext = null;
        }

        /*
        =====================
        CL_Disconnect

        Sends a disconnect message to the server
        This is also called on Host_Error, so it shouldn't cause any errors
        =====================
        */
        public static void CL_Disconnect ()
        {
            // stop sounds (especially looping!)
            sound.S_StopAllSounds(true);

            // bring the console down and fade the colors back to normal
            //	SCR_BringDownConsole ();

            // if running a local server, shut it down
            if (cls.demoplayback)
                CL_StopPlayback();
            else if (cls.state == cactive_t.ca_connected)
            {
                if (cls.demorecording)
                    CL_Stop_f();

                console.Con_DPrintf("Sending clc_disconnect\n");
                common.SZ_Clear(cls.message);
                common.MSG_WriteByte(cls.message, net.clc_disconnect);
                net.NET_SendUnreliableMessage(cls.netcon, cls.message);
                common.SZ_Clear(cls.message);
                net.NET_Close(cls.netcon);

                cls.state = cactive_t.ca_disconnected;
                if (server.sv.active)
                    host.Host_ShutdownServer(false);
            }

            cls.demoplayback = cls.timedemo = false;
            cls.signon = 0;
        }

        public static void CL_Disconnect_f ()
        {
            CL_Disconnect();
            if (server.sv.active)
                host.Host_ShutdownServer(false);
        }

        /*
        =====================
        CL_EstablishConnection

        Host should be either "local" or a net address to be passed on
        =====================
        */
        public static void CL_EstablishConnection (string host)
        {
            if (cls.state == cactive_t.ca_dedicated)
		        return;

	        if (cls.demoplayback)
		        return;

	        CL_Disconnect ();

            cls.netcon = net.NET_Connect(host);
            if (cls.netcon == null)
                quake.host.Host_Error("CL_Connect: connect failed\n");
	        console.Con_DPrintf ("CL_EstablishConnection: connected to " + host + "\n");
        	
	        cls.demonum = -1;			// not in the demo loop now
	        cls.state = cactive_t.ca_connected;
	        cls.signon = 0;				// need all the signon messages before playing
        }

        /*
        =====================
        CL_SignonReply

        An svc_signonnum has been received, perform a client side setup
        =====================
        */
        public static void CL_SignonReply ()
        {
	        string 	str;

            console.Con_DPrintf ("CL_SignonReply: " + cls.signon + "\n");

	        switch (cls.signon)
	        {
	        case 1:
		        common.MSG_WriteByte (cls.message, net.clc_stringcmd);
                common.MSG_WriteString(cls.message, "prespawn");
		        break;
        		
	        case 2:
                common.MSG_WriteByte(cls.message, net.clc_stringcmd);
                common.MSG_WriteString(cls.message, "name \"" + cl_name._string + "\"\n");

                common.MSG_WriteByte(cls.message, net.clc_stringcmd);
                common.MSG_WriteString(cls.message, "color " + (((int)cl_color.value) >> 4) + " " + (((int)cl_color.value) & 15) + "\n");

                common.MSG_WriteByte(cls.message, net.clc_stringcmd);
		        str = "spawn " + cls.spawnparms;
                common.MSG_WriteString(cls.message, str);
		        break;
        		
	        case 3:
                common.MSG_WriteByte(cls.message, net.clc_stringcmd);
                common.MSG_WriteString(cls.message, "begin");
		        break;
        		
	        case 4:
		        screen.SCR_EndLoadingPlaque ();		// allow normal screen updates
		        break;
	        }
        }

        /*
        =====================
        CL_NextDemo

        Called to play the next demo in the demo loop
        =====================
        */
        public static void CL_NextDemo ()
        {
	        string	str;

	        if (cls.demonum == -1)
		        return;		// don't play demos

	        screen.SCR_BeginLoadingPlaque ();

	        if (cls.demos[cls.demonum] == null || cls.demonum == MAX_DEMOS)
	        {
		        cls.demonum = 0;
		        if (cls.demos[cls.demonum] == null)
		        {
                    console.Con_Printf("No demos listed with startdemos\n");
			        cls.demonum = -1;
			        return;
		        }
	        }

	        str = "playdemo " + cls.demos[cls.demonum] + "\n";
	        cmd.Cbuf_InsertText (str);
	        cls.demonum++;
        }

        /*
        ==============
        CL_PrintEntities_f
        ==============
        */
        static void CL_PrintEntities_f ()
        {
	        render.entity_t ent;
	        int			    i;
        	
	        for (i=0 ; i<cl.num_entities ; i++)
	        {
                ent = cl_entities[i];
		        console.Con_Printf (i + ":");
		        if (ent.model == null)
		        {
			        console.Con_Printf ("EMPTY\n");
			        continue;
		        }
		        /*console.Con_Printf ("%s:%2i  (%5.1f,%5.1f,%5.1f) [%5.1f %5.1f %5.1f]\n"
		        ,ent.model.name,ent.frame, ent.origin[0], ent.origin[1], ent.origin[2], ent.angles[0], ent.angles[1], ent.angles[2]);*/
	        }
        }

        /*
        ===============
        CL_AllocDlight

        ===============
        */
        static dlight_t CL_AllocDlight (int key)
        {
	        int		    i;
	        dlight_t    dl;

        // first look for an exact key match
	        if (key != 0)
	        {
		        for (i=0 ; i<MAX_DLIGHTS ; i++)
		        {
    		        dl = cl_dlights[i];
			        if (dl.key == key)
			        {
//				        memset (dl, 0, sizeof(*dl));
				        dl.key = key;
				        return dl;
			        }
		        }
	        }

        // then look for anything else
	        for (i=0 ; i<MAX_DLIGHTS ; i++)
	        {
    	        dl = cl_dlights[i];
		        if (dl.die < cl.time)
		        {
//			        memset (dl, 0, sizeof(*dl));
			        dl.key = key;
			        return dl;
		        }
	        }

	        dl = cl_dlights[0];
//	        memset (dl, 0, sizeof(*dl));
	        dl.key = key;
	        return dl;
        }


        /*
        ===============
        CL_DecayLights

        ===============
        */
        public static void CL_DecayLights ()
        {
	        int			i;
	        dlight_t	dl;
	        double		time;
        	
	        time = cl.time - cl.oldtime;

	        for (i=0 ; i<MAX_DLIGHTS ; i++)
	        {
                dl = cl_dlights[i];
                if (dl.die < cl.time || dl.radius == 0)
			        continue;
        		
		        dl.radius -= time*dl.decay;
		        if (dl.radius < 0)
			        dl.radius = 0;
	        }
        }

        /*
        ===============
        CL_LerpPoint

        Determines the fraction between the last two messages that the objects
        should be put at.
        ===============
        */
        static double	CL_LerpPoint ()
        {
            double f, frac;

            f = cl.mtime[0] - cl.mtime[1];

            if (f == 0 || cl_nolerp.value != 0 || cls.timedemo || server.sv.active)
            {
                cl.time = cl.mtime[0];
                return 1;
            }

            if (f > 0.1)
            {	// dropped packet, or start of demo
                cl.mtime[1] = cl.mtime[0] - 0.1;
                f = 0.1;
            }
            frac = (cl.time - cl.mtime[1]) / f;
            //Con_Printf ("frac: %f\n",frac);
            if (frac < 0)
            {
                if (frac < -0.01)
                {
                    cl.time = cl.mtime[1];
                    //				Con_Printf ("low frac\n");
                }
                frac = 0;
            }
            else if (frac > 1)
            {
                if (frac > 1.01)
                {
                    cl.time = cl.mtime[0];
                    //				Con_Printf ("high frac\n");
                }
                frac = 1;
            }

            return frac;
        }
        
        /*
        ===============
        CL_RelinkEntities
        ===============
        */
        static void CL_RelinkEntities ()
        {
	        render.entity_t ent;
            int             i, j;
            double          frac, f, d;
            double[]        delta = new double[3];
            double          bobjrotate;
            double[]        oldorg = new double[3];
            dlight_t        dl;

            // determine partial update time	
            frac = CL_LerpPoint();

	        cl_numvisedicts = 0;

            //
            // interpolate player info
            //
	            for (i=0 ; i<3 ; i++)
		            cl.velocity[i] = cl.mvelocity[1][i] + 
			            frac * (cl.mvelocity[0][i] - cl.mvelocity[1][i]);

            if (cls.demoplayback)
            {
                // interpolate the angles	
                for (j = 0; j < 3; j++)
                {
                    d = cl.mviewangles[0][j] - cl.mviewangles[1][j];
                    if (d > 180)
                        d -= 360;
                    else if (d < -180)
                        d += 360;
                    cl.viewangles[j] = cl.mviewangles[1][j] + frac * d;
                }
            }

	        bobjrotate = mathlib.anglemod(100*cl.time);

        // start on the entity after the world
	        for (i=1 ; i<cl.num_entities ; i++)
	        {
                ent = cl_entities[i];
		        if (ent.model == null)
		        {	// empty slot
			        if (ent.forcelink)
				        render.R_RemoveEfrags (ent);	// just became empty
			        continue;
		        }

        // if the object wasn't included in the last packet, remove it
		        if (ent.msgtime != cl.mtime[0])
		        {
			        ent.model = null;
			        continue;
		        }

		        mathlib.VectorCopy (ent.origin, ref oldorg);

		        if (ent.forcelink)
		        {	// the entity was not updated in the last message
			        // so move to the final spot
			        mathlib.VectorCopy (ent.msg_origins[0], ref ent.origin);
                    mathlib.VectorCopy (ent.msg_angles[0], ref ent.angles);
		        }
		        else
		        {	// if the delta is large, assume a teleport and don't lerp
			        f = frac;
			        for (j=0 ; j<3 ; j++)
			        {
				        delta[j] = ent.msg_origins[0][j] - ent.msg_origins[1][j];
				        if (delta[j] > 100 || delta[j] < -100)
					        f = 1;		// assume a teleportation, not a motion
			        }

		        // interpolate the origin and angles
			        for (j=0 ; j<3 ; j++)
			        {
				        ent.origin[j] = ent.msg_origins[1][j] + f*delta[j];

				        d = ent.msg_angles[0][j] - ent.msg_angles[1][j];
				        if (d > 180)
					        d -= 360;
				        else if (d < -180)
					        d += 360;
				        ent.angles[j] = ent.msg_angles[1][j] + f*d;
			        }
        			
		        }

        // rotate binary objects locally
		        if ((ent.model.flags & model.EF_ROTATE) != 0)
			        ent.angles[1] = bobjrotate;

                if ((ent.effects & server.EF_BRIGHTFIELD) != 0)
                    render.R_EntityParticles (ent);
		        if ((ent.effects & server.EF_MUZZLEFLASH) != 0)
		        {
                    double[]        fv = new double[3], rv = new double[3], uv = new double[3];

			        dl = CL_AllocDlight (i);
			        mathlib.VectorCopy (ent.origin,  ref dl.origin);
			        dl.origin[2] += 16;
                    mathlib.AngleVectors(ent.angles, ref fv, ref rv, ref uv);

                    mathlib.VectorMA(dl.origin, 18, fv, ref dl.origin);
			        dl.radius = 200 + (helper.rand()&31);
			        dl.minlight = 32;
			        dl.die = cl.time + 0.1;
		        }
		        if ((ent.effects & server.EF_BRIGHTLIGHT) != 0)
		        {			
			        dl = CL_AllocDlight (i);
                    mathlib.VectorCopy(ent.origin, ref dl.origin);
			        dl.origin[2] += 16;
			        dl.radius = 400 + (helper.rand()&31);
			        dl.die = cl.time + 0.001;
		        }
                if ((ent.effects & server.EF_DIMLIGHT) != 0)
		        {			
			        dl = CL_AllocDlight (i);
                    mathlib.VectorCopy(ent.origin, ref dl.origin);
			        dl.radius = 200 + (helper.rand()&31);
			        dl.die = cl.time + 0.001;
		        }

                if ((ent.model.flags & model.EF_GIB) != 0)
			        render.R_RocketTrail (oldorg, ent.origin, 2);
                else if ((ent.model.flags & model.EF_ZOMGIB) != 0)
                    render.R_RocketTrail(oldorg, ent.origin, 4);
                else if ((ent.model.flags & model.EF_TRACER) != 0)
                    render.R_RocketTrail(oldorg, ent.origin, 3);
                else if ((ent.model.flags & model.EF_TRACER2) != 0)
                    render.R_RocketTrail(oldorg, ent.origin, 5);
                else if ((ent.model.flags & model.EF_ROCKET) != 0)
		        {
			        render.R_RocketTrail (oldorg, ent.origin, 0);
			        dl = CL_AllocDlight (i);
			        mathlib.VectorCopy (ent.origin, ref dl.origin);
			        dl.radius = 200;
			        dl.die = cl.time + 0.01;
		        }
                else if ((ent.model.flags & model.EF_GRENADE) != 0)
                    render.R_RocketTrail(oldorg, ent.origin, 1);
		        else if ((ent.model.flags & model.EF_TRACER3) != 0)
			        render.R_RocketTrail (oldorg, ent.origin, 6);

		        ent.forcelink = false;

		        if (i == cl.viewentity && chase.chase_active.value == 0)
			        continue;

		        if (cl_numvisedicts < MAX_VISEDICTS)
		        {
			        cl_visedicts[cl_numvisedicts] = ent;
			        cl_numvisedicts++;
		        }
	        }
        }

        /*
        ===============
        CL_ReadFromServer

        Read all incoming data from the server
        ===============
        */
        public static int CL_ReadFromServer ()
        {
            int ret;

            cl.oldtime = cl.time;
            cl.time += host.host_frametime;

            do
            {
                ret = CL_GetMessage();
                if (ret == -1)
                    host.Host_Error("CL_ReadFromServer: lost server connection");
                if (ret == 0)
                    break;

                cl.last_received_message = host.realtime;
                CL_ParseServerMessage();
            } while (ret != 0 && cls.state == cactive_t.ca_connected);

            if (cl_shownet.value != 0)
                console.Con_Printf("\n");

            CL_RelinkEntities();
            CL_UpdateTEnts();

            //
            // bring the links up to date
            //
            return 0;
        }

        /*
        =================
        CL_SendCmd
        =================
        */
        public static void CL_SendCmd ()
        {
            usercmd_t cmd = new usercmd_t();

	        if (cls.state != cactive_t.ca_connected)
		        return;

	        if (cls.signon == SIGNONS)
	        {
                // get basic movement from keyboard
                CL_BaseMove(cmd);

                // allow mice or other external controllers to add to the move
//                IN_Move(cmd);

                // send the unreliable message
                CL_SendMove(cmd);
            }

	        if (cls.demoplayback)
	        {
		        common.SZ_Clear (cls.message);
		        return;
	        }
        	
        // send the reliable message
	        if (cls.message.cursize == 0)
		        return;		// no message at all
        	
	        if (!net.NET_CanSendMessage (cls.netcon))
	        {
		        console.Con_DPrintf ("CL_WriteToServer: can't send\n");
		        return;
	        }

	        if (net.NET_SendMessage (cls.netcon, cls.message) == -1)
		        host.Host_Error ("CL_WriteToServer: lost server connection");

	        common.SZ_Clear (cls.message);
        }

        /*
        =================
        CL_Init
        =================
        */
        public static void CL_Init ()
        {
            common.SZ_Alloc(cls.message, 1024);

            CL_InitInput();
            CL_InitTEnts();

        //
        // register our commands
        //
	        cvar_t.Cvar_RegisterVariable(cl_name);
            cvar_t.Cvar_RegisterVariable(cl_color);
            cvar_t.Cvar_RegisterVariable(cl_shownet);
            cvar_t.Cvar_RegisterVariable(cl_nolerp);
            cvar_t.Cvar_RegisterVariable(lookspring);
            cvar_t.Cvar_RegisterVariable(lookstrafe);
            cvar_t.Cvar_RegisterVariable(sensitivity);

            cvar_t.Cvar_RegisterVariable(m_pitch);
            cvar_t.Cvar_RegisterVariable(m_yaw);
            cvar_t.Cvar_RegisterVariable(m_forward);
            cvar_t.Cvar_RegisterVariable(m_side);

        //	Cvar_RegisterVariable (cl_autofire);
        	
	        cmd.Cmd_AddCommand("entities", CL_PrintEntities_f);
            cmd.Cmd_AddCommand("disconnect", CL_Disconnect_f);
            cmd.Cmd_AddCommand("record", CL_Record_f);
            cmd.Cmd_AddCommand("stop", CL_Stop_f);
            cmd.Cmd_AddCommand("playdemo", CL_PlayDemo_f);
            cmd.Cmd_AddCommand("timedemo", CL_TimeDemo_f);
        }
    }
}
