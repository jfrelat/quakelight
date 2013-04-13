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
// sv_main.c -- server main program

namespace quake
{
    public partial class server
    {
        public static server_t		    sv = new server_t();
        public static server_static_t   svs = new server_static_t();

        static string[] localmodels = new string[quakedef.MAX_MODELS];			// inline model names for precache

        //============================================================================

        /*
        ===============
        SV_Init
        ===============
        */
        public static void SV_Init ()
        {
	        int		i;

            cvar_t.Cvar_RegisterVariable(sv_maxvelocity);
            cvar_t.Cvar_RegisterVariable(sv_gravity);
            cvar_t.Cvar_RegisterVariable(sv_friction);
            cvar_t.Cvar_RegisterVariable(sv_edgefriction);
            cvar_t.Cvar_RegisterVariable(sv_stopspeed);
            cvar_t.Cvar_RegisterVariable(sv_maxspeed);
            cvar_t.Cvar_RegisterVariable(sv_accelerate);
            cvar_t.Cvar_RegisterVariable(sv_idealpitchscale);
            //cvar_t.Cvar_RegisterVariable(sv_aim);
            cvar_t.Cvar_RegisterVariable(sv_nostep);

	        for (i=0 ; i<quakedef.MAX_MODELS ; i++)
		        localmodels[i] = "*" + i;
        }

        /*
        =============================================================================

        EVENT MESSAGES

        =============================================================================
        */

        /*
        ==============================================================================

        CLIENT SPAWNING

        ==============================================================================
        */

        /*
        ================
        SV_SendServerinfo

        Sends the first message from the server to a connected client.
        This will be sent on the initial connection and upon each server load.
        ================
        */
        static void SV_SendServerinfo (client_t client)
        {
            int             i;
            string          s;
	        string			message;

	        common.MSG_WriteByte (client.message, net.svc_print);
	        message = "\u0002\nVERSION " + quakedef.VERSION + " SERVER (" + prog.pr_crc + " CRC)";
            common.MSG_WriteString(client.message, message);

            common.MSG_WriteByte(client.message, net.svc_serverinfo);
            common.MSG_WriteLong(client.message, net.PROTOCOL_VERSION);
            common.MSG_WriteByte(client.message, svs.maxclients);

            if (host.coop.value == 0 && host.deathmatch.value != 0)
                common.MSG_WriteByte(client.message, net.GAME_DEATHMATCH);
            else
                common.MSG_WriteByte(client.message, net.GAME_COOP);

            //sprintf(message, pr_strings + sv.edicts->v.message);
            message = prog.pr_string(sv.edicts[0].v.message);

            common.MSG_WriteString(client.message, message);

            for (i = 1, s = sv.model_precache[i]; s != null; i++)
            {
                s = sv.model_precache[i];
                common.MSG_WriteString(client.message, s);
            }
            common.MSG_WriteByte(client.message, 0);

            for (i = 1, s = sv.sound_precache[i]; s != null; i++)
            {
                s = sv.sound_precache[i];
                common.MSG_WriteString(client.message, s);
            }
            common.MSG_WriteByte(client.message, 0);

            // send music
            common.MSG_WriteByte(client.message, net.svc_cdtrack);
            common.MSG_WriteByte(client.message, (int)sv.edicts[0].v.sounds);
            common.MSG_WriteByte(client.message, (int)sv.edicts[0].v.sounds);

            // set view	
            common.MSG_WriteByte (client.message, net.svc_setview);
            common.MSG_WriteShort (client.message, prog.NUM_FOR_EDICT(client.edict));

            common.MSG_WriteByte(client.message, net.svc_signonnum);
            common.MSG_WriteByte(client.message, 1);

	        client.sendsignon = true;
	        client.spawned = false;		// need prespawn, spawn, etc
        }

        /*
        ================
        SV_ConnectClient

        Initializes a client_t for a new net connection.  This will only be called
        once for a player each game, not once for each level change.
        ================
        */
        static void SV_ConnectClient (int clientnum)
        {
            prog.edict_t    ent;
            client_t        client;
	        int				edictnum;
        	net.qsocket_t   netconnection;
	        int				i;
	        double[]		spawn_parms = new double[NUM_SPAWN_PARMS];

	        client = svs.clients[clientnum];

	        console.Con_DPrintf ("Client " + client.netconnection.address + " connected\n");

	        edictnum = clientnum+1;

            ent = prog.EDICT_NUM(edictnum);

        // set up the client_t
	        netconnection = client.netconnection;
        	
	        client.netconnection = netconnection;
	        client.name = "unconnected";
	        client.active = true;
	        client.spawned = false;
            client.edict = ent;
            client.message.data = client.msgbuf;
	        client.message.maxsize = client.msgbuf.Length;
	        client.message.allowoverflow = true;		// we can catch it*/

	        client.privileged = false;

	        {
	        // call the progs to get default spawn parms for the new client
		        prog.PR_ExecuteProgram (prog.pr_functions[prog.pr_global_struct[0].SetNewParms]);
		        for (i=0 ; i<NUM_SPAWN_PARMS ; i++)
                    client.spawn_parms[i] = prog.cast_float(prog.pr_globals_read(43 + i));
	        }

	        SV_SendServerinfo (client);
        }

        /*
        ===================
        SV_CheckForNewClients

        ===================
        */
        public static void SV_CheckForNewClients ()
        {
	        net.qsocket_t	ret;
	        int				i;
        		
        //
        // check for new connections
        //
	        while (true)
	        {
		        ret = net.NET_CheckNewConnections ();
		        if (ret == null)
			        break;

	        // 
	        // init a new client structure
	        //	
		        for (i=0 ; i<svs.maxclients ; i++)
			        if (!svs.clients[i].active)
				        break;
		        if (i == svs.maxclients)
			        sys_linux.Sys_Error ("Host_CheckForNewClients: no free clients");
        		
		        svs.clients[i].netconnection = ret;
		        SV_ConnectClient (i);	
        	
		        net.net_activeconnections++;
	        }
        }

        /*
        ===============================================================================

        FRAME UPDATES

        ===============================================================================
        */

        /*
        ==================
        SV_ClearDatagram

        ==================
        */
        public static void SV_ClearDatagram ()
        {
        	common.SZ_Clear (sv.datagram);
        }

        /*
        =============
        SV_WriteEntitiesToClient

        =============
        */
        static void SV_WriteEntitiesToClient (prog.edict_t	clent, common.sizebuf_t msg)
        {
	        int		        e, i;
	        int		        bits;
	        double[]        org = new double[3];
	        double	        miss;
	        prog.edict_t	ent;

        // send over all entities (excpet the client) that touch the pvs
	        for (e=1 ; e<sv.num_edicts ; e++)
	        {
                ent = sv.edicts[e];

        // ignore if not touching a PV leaf
		        if (ent != clent)	// clent is ALLWAYS sent
		        {
        // ignore ents without visible models
			        if (ent.v.modelindex == 0 || prog.pr_string(ent.v.model) == null)
				        continue;
		        }

		        if (msg.maxsize - msg.cursize < 16)
		        {
			        console.Con_Printf ("packet overflow\n");
			        return;
		        }

                // send an update
                bits = 0;

                for (i = 0; i < 3; i++)
                {
                    miss = ent.v.origin[i] - ent.baseline.origin[i];
                    if (miss < -0.1 || miss > 0.1)
                        bits |= net.U_ORIGIN1 << i;
                }

                if (ent.v.angles[0] != ent.baseline.angles[0])
                    bits |= net.U_ANGLE1;

                if (ent.v.angles[1] != ent.baseline.angles[1])
                    bits |= net.U_ANGLE2;

                if (ent.v.angles[2] != ent.baseline.angles[2])
                    bits |= net.U_ANGLE3;

                if (ent.v.movetype == MOVETYPE_STEP)
                    bits |= net.U_NOLERP;	// don't mess up the step animation

                if (ent.baseline.colormap != ent.v.colormap)
                    bits |= net.U_COLORMAP;

                if (ent.baseline.skin != ent.v.skin)
                    bits |= net.U_SKIN;

                if (ent.baseline.frame != ent.v.frame)
                    bits |= net.U_FRAME;

                if (ent.baseline.effects != ent.v.effects)
                    bits |= net.U_EFFECTS;

                if (ent.baseline.modelindex != ent.v.modelindex)
                    bits |= net.U_MODEL;

                if (e >= 256)
                    bits |= net.U_LONGENTITY;

                if (bits >= 256)
                    bits |= net.U_MOREBITS;

                //
                // write the message
                //
                common.MSG_WriteByte(msg, bits | net.U_SIGNAL);

                if ((bits & net.U_MOREBITS) != 0)
                    common.MSG_WriteByte(msg, bits >> 8);
                if ((bits & net.U_LONGENTITY) != 0)
                    common.MSG_WriteShort(msg, e);
                else
                    common.MSG_WriteByte(msg, e);

                if ((bits & net.U_MODEL) != 0)
                    common.MSG_WriteByte(msg, (int)ent.v.modelindex);
                if ((bits & net.U_FRAME) != 0)
                    common.MSG_WriteByte(msg, (int)ent.v.frame);
                if ((bits & net.U_COLORMAP) != 0)
                    common.MSG_WriteByte(msg, (int)ent.v.colormap);
                if ((bits & net.U_SKIN) != 0)
                    common.MSG_WriteByte(msg, (int)ent.v.skin);
                if ((bits & net.U_EFFECTS) != 0)
                    common.MSG_WriteByte(msg, (int)ent.v.effects);
                if ((bits & net.U_ORIGIN1) != 0)
                    common.MSG_WriteCoord(msg, ent.v.origin[0]);
                if ((bits & net.U_ANGLE1) != 0)
                    common.MSG_WriteAngle(msg, ent.v.angles[0]);
                if ((bits & net.U_ORIGIN2) != 0)
                    common.MSG_WriteCoord(msg, ent.v.origin[1]);
                if ((bits & net.U_ANGLE2) != 0)
                    common.MSG_WriteAngle(msg, ent.v.angles[1]);
                if ((bits & net.U_ORIGIN3) != 0)
                    common.MSG_WriteCoord(msg, ent.v.origin[2]);
                if ((bits & net.U_ANGLE3) != 0)
                    common.MSG_WriteAngle(msg, ent.v.angles[2]);
            }
        }

        /*
        =============
        SV_CleanupEnts

        =============
        */
        static void SV_CleanupEnts ()
        {
	        int		        e;
	        prog.edict_t    ent;
        	
	        for (e=1 ; e<sv.num_edicts ; e++)
	        {
                ent = sv.edicts[e];
                ent.v.effects = (int)ent.v.effects & ~EF_MUZZLEFLASH;
	        }
        }

        /*
        ==================
        SV_WriteClientdataToMessage

        ==================
        */
        public static void SV_WriteClientdataToMessage (prog.edict_t ent, common.sizebuf_t msg)
        {
	        int		        bits;
	        int		        i;
	        prog.edict_t	other;
	        int		        items;

        //
        // send a damage message
        //
	        if (ent.v.dmg_take != 0 || ent.v.dmg_save != 0)
	        {
		        other = prog.PROG_TO_EDICT(ent.v.dmg_inflictor);
		        common.MSG_WriteByte (msg, net.svc_damage);
                common.MSG_WriteByte(msg, (int)ent.v.dmg_save);
                common.MSG_WriteByte(msg, (int)ent.v.dmg_take);
		        for (i=0 ; i<3 ; i++)
                    common.MSG_WriteCoord(msg, other.v.origin[i] + 0.5 * (other.v.mins[i] + other.v.maxs[i]));
        	
		        ent.v.dmg_take = 0;
		        ent.v.dmg_save = 0;
	        }

        //
        // send the current viewpos offset from the view entity
        //
	        SV_SetIdealPitch ();		// how much to look up / down ideally

        // a fixangle might get lost in a dropped packet.  Oh well.
	        if ( ent.v.fixangle != 0)
	        {
		        common.MSG_WriteByte (msg, net.svc_setangle);
		        for (i=0 ; i < 3 ; i++)
                    common.MSG_WriteAngle(msg, ent.v.angles[i]);
		        ent.v.fixangle = 0;
	        }

	        bits = 0;
        	
	        if (ent.v.view_ofs[2] != net.DEFAULT_VIEWHEIGHT)
		        bits |= net.SU_VIEWHEIGHT;
        		
	        if (ent.v.idealpitch != 0)
		        bits |= net.SU_IDEALPITCH;

        // stuff the sigil bits into the high bits of items for sbar, or else
        // mix in items2
	        //val = GetEdictFieldValue(ent, "items2");

	        /*if (val)
		        items = (int)ent->v.items | ((int)val->_float << 23);
	        else*/
		        items = (int)ent.v.items | ((int)prog.pr_global_struct[0].serverflags << 28);

	        bits |= net.SU_ITEMS;

            if (((int)ent.v.flags & FL_ONGROUND) != 0)
                bits |= net.SU_ONGROUND;
        	
	        if ( ent.v.waterlevel >= 2)
		        bits |= net.SU_INWATER;
        	
	        for (i=0 ; i<3 ; i++)
	        {
		        if (ent.v.punchangle[i] != 0)
			        bits |= (net.SU_PUNCH1<<i);
		        if (ent.v.velocity[i] != 0)
			        bits |= (net.SU_VELOCITY1<<i);
	        }

            if (ent.v.weaponframe != 0)
                bits |= net.SU_WEAPONFRAME;

            if (ent.v.armorvalue != 0)
                bits |= net.SU_ARMOR;

            //	if (ent.v.weapon != 0)
                bits |= net.SU_WEAPON;

        // send the data

	        common.MSG_WriteByte (msg, net.svc_clientdata);
            common.MSG_WriteShort(msg, bits);

	        if ((bits & net.SU_VIEWHEIGHT) != 0)
                common.MSG_WriteChar(msg, (int)ent.v.view_ofs[2]);

            if ((bits & net.SU_IDEALPITCH) != 0)
                common.MSG_WriteChar(msg, (int)ent.v.idealpitch);

	        for (i=0 ; i<3 ; i++)
	        {
                if ((bits & (net.SU_PUNCH1 << i)) != 0)
                    common.MSG_WriteChar(msg, (int)ent.v.punchangle[i]);
                if ((bits & (net.SU_VELOCITY1 << i)) != 0)
                    common.MSG_WriteChar(msg, (int)ent.v.velocity[i] / 16);
	        }

            // [always sent]	if (bits & SU_ITEMS)
            common.MSG_WriteLong(msg, items);

            if ((bits & net.SU_WEAPONFRAME) != 0)
                common.MSG_WriteByte(msg, (int)ent.v.weaponframe);
            if ((bits & net.SU_ARMOR) != 0)
                common.MSG_WriteByte(msg, (int)ent.v.armorvalue);
            if ((bits & net.SU_WEAPON) != 0)
                common.MSG_WriteByte(msg, SV_ModelIndex(prog.pr_string(ent.v.weaponmodel)));

            common.MSG_WriteShort(msg, (int)ent.v.health);
            common.MSG_WriteByte(msg, (int)ent.v.currentammo);
            common.MSG_WriteByte(msg, (int)ent.v.ammo_shells);
            common.MSG_WriteByte(msg, (int)ent.v.ammo_nails);
            common.MSG_WriteByte(msg, (int)ent.v.ammo_rockets);
            common.MSG_WriteByte(msg, (int)ent.v.ammo_cells);
            
            if (common.standard_quake)
            {
                common.MSG_WriteByte(msg, (int)ent.v.weapon);
            }
            else
            {
		        for(i=0;i<32;i++)
		        {
			        if ((((int)ent.v.weapon) & (1<<i) ) != 0)
			        {
				        common.MSG_WriteByte (msg, i);
				        break;
			        }
		        }
            }
        }

        /*
        =======================
        SV_SendClientDatagram
        =======================
        */
        static bool SV_SendClientDatagram(client_t client)
        {
	        byte[]		        buf = new byte[quakedef.MAX_DATAGRAM];
	        common.sizebuf_t    msg = new common.sizebuf_t();
        	
	        msg.data = buf;
	        msg.maxsize = buf.Length;
	        msg.cursize = 0;

	        common.MSG_WriteByte (msg, net.svc_time);
            common.MSG_WriteFloat (msg, sv.time);

            // add the client specific data to the datagram
            SV_WriteClientdataToMessage(client.edict, msg);

            SV_WriteEntitiesToClient(client.edict, msg);

        // copy the server datagram if there is space
	        if (msg.cursize + sv.datagram.cursize < msg.maxsize)
                common.SZ_Write(msg, sv.datagram.data, sv.datagram.cursize);

        // send the datagram
	        if (net.NET_SendUnreliableMessage (client.netconnection, msg) == -1)
	        {
		        return false;
	        }
        	
	        return true;
        }

        /*
        =======================
        SV_UpdateToReliableMessages
        =======================
        */
        static void SV_UpdateToReliableMessages ()
        {
	        int			i, j;
	        client_t    client;

        // check for changes to be sent over the reliable streams
	        for (i=0 ; i<svs.maxclients ; i++)
	        {
                host.host_client = svs.clients[i];
		        if (host.host_client.old_frags != host.host_client.edict.v.frags)
		        {
			        for (j=0; j<svs.maxclients ; j++)
			        {
                        client = svs.clients[j];
				        if (!client.active)
					        continue;
				        common.MSG_WriteByte (client.message, net.svc_updatefrags);
                        common.MSG_WriteByte (client.message, i);
                        common.MSG_WriteShort(client.message, (int)host.host_client.edict.v.frags);
			        }

                    host.host_client.old_frags = (int)host.host_client.edict.v.frags;
		        }
	        }
        	
	        for (j=0 ; j<svs.maxclients ; j++)
	        {
                client = svs.clients[j];
		        if (!client.active)
			        continue;
		        common.SZ_Write (client.message, sv.reliable_datagram.data, sv.reliable_datagram.cursize);
	        }

	        common.SZ_Clear (sv.reliable_datagram);
        }

        /*
        =======================
        SV_SendNop

        Send a nop message without trashing or sending the accumulated client
        message buffer
        =======================
        */
        static void SV_SendNop(client_t client)
        {
	        common.sizebuf_t	msg = new common.sizebuf_t();
	        byte[]		        buf = new byte[4];
        	
	        msg.data = buf;
	        msg.maxsize = buf.Length;
	        msg.cursize = 0;

	        common.MSG_WriteChar (msg, net.svc_nop);

            net.NET_SendUnreliableMessage (client.netconnection, msg);
	        client.last_message = host.realtime;
        }

        /*
        =======================
        SV_SendClientMessages
        =======================
        */
        public static void SV_SendClientMessages ()
        {
	        int			i;
        	
        // update frags, names, etc
	        SV_UpdateToReliableMessages ();

        // build individual updates
	        for (i=0 ; i<svs.maxclients ; i++)
	        {
                host.host_client = svs.clients[i];
		        if (!host.host_client.active)
			        continue;

                if (host.host_client.spawned)
		        {
			        if (!SV_SendClientDatagram (host.host_client))
				        continue;
		        }
		        else
		        {
		        // the player isn't totally in the game yet
		        // send small keepalive messages if too much time has passed
		        // send a full message when the next signon stage has been requested
		        // some other message data (name changes, etc) may accumulate 
		        // between signon stages
			        if (!host.host_client.sendsignon)
			        {
				        if (host.realtime - host.host_client.last_message > 5)
					        SV_SendNop (host.host_client);
				        continue;	// don't send out non-signon messages
			        }
		        }

		        // check for an overflowed message.  Should only happen
		        // on a very fucked up connection that backs up a lot, then
		        // changes level
		        if (host.host_client.message.overflowed)
		        {
			        host.host_client.message.overflowed = false;
			        continue;
		        }

                if (host.host_client.message.cursize != 0 || host.host_client.dropasap)
		        {
			        if (!net.NET_CanSendMessage (host.host_client.netconnection))
			        {
        //				I_Printf ("can't write\n");
				        continue;
			        }

                    if (!host.host_client.dropasap)
			        {
                        net.NET_SendMessage (host.host_client.netconnection
                        , host.host_client.message);
                        common.SZ_Clear (host.host_client.message);
                        host.host_client.last_message = host.realtime;
                        host.host_client.sendsignon = false;
			        }
		        }
	        }
        	
        	
        // clear muzzle flashes
	        SV_CleanupEnts ();
        }

        /*
        ==============================================================================

        SERVER SPAWNING

        ==============================================================================
        */

        /*
        ================
        SV_ModelIndex

        ================
        */
        public static int SV_ModelIndex (string name)
        {
	        int		i;
        	
	        if (name == null || name.Length == 0)
		        return 0;

	        for (i=0 ; i<quakedef.MAX_MODELS && sv.model_precache[i] != null ; i++)
		        if (sv.model_precache[i].CompareTo(name) == 0)
			        return i;
	        if (i==quakedef.MAX_MODELS || sv.model_precache[i] == null)
		        sys_linux.Sys_Error ("SV_ModelIndex: model " + name + " not precached");
	        return i;
        }

        /*
        ================
        SV_CreateBaseline

        ================
        */
        static void SV_CreateBaseline ()
        {
	        int			    i;
	        prog.edict_t	svent;
	        int				entnum;	
        		
	        for (entnum = 0; entnum < sv.num_edicts ; entnum++)
	        {
	        // get the current server version
		        svent = prog.EDICT_NUM(entnum);
		        if (svent.free)
			        continue;
		        if (entnum > svs.maxclients && svent.v.modelindex == 0)
			        continue;

	        //
	        // create entity baseline
	        //
		        mathlib.VectorCopy (svent.v.origin, ref svent.baseline.origin);
                mathlib.VectorCopy (svent.v.angles, ref svent.baseline.angles);
		        svent.baseline.frame = (int)svent.v.frame;
                svent.baseline.skin = (int)svent.v.skin;
		        if (entnum > 0 && entnum <= svs.maxclients)
		        {
			        svent.baseline.colormap = entnum;
			        svent.baseline.modelindex = SV_ModelIndex("progs/player.mdl");
		        }
		        else
		        {
			        svent.baseline.colormap = 0;
			        svent.baseline.modelindex =
				        SV_ModelIndex(prog.pr_string(svent.v.model));
		        }
        		
	        //
	        // add to the message
	        //
		        common.MSG_WriteByte (sv.signon,net.svc_spawnbaseline);
                common.MSG_WriteShort(sv.signon, entnum);

                common.MSG_WriteByte(sv.signon, svent.baseline.modelindex);
                common.MSG_WriteByte(sv.signon, svent.baseline.frame);
                common.MSG_WriteByte(sv.signon, svent.baseline.colormap);
                common.MSG_WriteByte(sv.signon, svent.baseline.skin);
		        for (i=0 ; i<3 ; i++)
		        {
                    common.MSG_WriteCoord(sv.signon, svent.baseline.origin[i]);
                    common.MSG_WriteAngle(sv.signon, svent.baseline.angles[i]);
		        }
	        }
        }

        /*
        ================
        SV_SendReconnect

        Tell all the clients that the server is changing levels
        ================
        */
        static void SV_SendReconnect ()
        {
            byte[]              data = new byte[128];
	        common.sizebuf_t	msg = new common.sizebuf_t();

	        msg.data = data;
	        msg.cursize = 0;
	        msg.maxsize = data.Length;

	        common.MSG_WriteChar (msg, net.svc_stufftext);
	        common.MSG_WriteString (msg, "reconnect\n");
	        net.NET_SendToAll (msg, 5);
        	
	        if (client.cls.state != client.cactive_t.ca_dedicated)
                quake.cmd.Cmd_ExecuteString("reconnect\n\0".ToCharArray(), quake.cmd.cmd_source_t.src_command);
        }

        /*
        ================
        SV_SpawnServer

        This is called at the start of each level
        ================
        */
        public static void SV_SpawnServer (string server)
        {
        	prog.edict_t    ent;
	        int			    i;

	        // let's not have any servers with no name
	        if (net.hostname._string.Length == 0)
		        cvar_t.Cvar_Set ("hostname", "UNNAMED");
	        screen.scr_centertime_off = 0;

	        console.Con_DPrintf ("SpawnServer: " + server + "\n");
	        svs.changelevel_issued = false;		// now safe to issue another

        //
        // tell all connected clients that we are going to a new level
        //
            if (sv.active)
            {
	            SV_SendReconnect ();
            }

        //
        // make cvars consistant
        //
	        if (host.coop.value != 0)
		        cvar_t.Cvar_SetValue ("deathmatch", 0);
            host.current_skill = (int)(host.skill.value + 0.5);
            if (host.current_skill < 0)
                host.current_skill = 0;
            if (host.current_skill > 3)
                host.current_skill = 3;

            cvar_t.Cvar_SetValue("skill", (double)host.current_skill);
	
        //
        // set up the new server
        //
	        host.Host_ClearMemory ();

	        sv.name = server;

        // load progs to get entity field count
	        prog.PR_LoadProgs ();

        // allocate server memory
	        sv.max_edicts = quakedef.MAX_EDICTS;

	        sv.edicts = new prog.edict_t[sv.max_edicts];
            for (int kk = 0; kk < sv.max_edicts; kk++)
            {
                sv.edicts[kk] = new prog.edict_t(kk);
                sv.edicts[kk].v.variables = new Object[prog.pr_edict_size - prog.sizeof_edict_t];
            }

            sv.datagram.maxsize = sv.datagram_buf.Length;
            sv.datagram.cursize = 0;
            sv.datagram.data = sv.datagram_buf;

            sv.reliable_datagram.maxsize = sv.reliable_datagram_buf.Length;
            sv.reliable_datagram.cursize = 0;
            sv.reliable_datagram.data = sv.reliable_datagram_buf;

            sv.signon.maxsize = sv.signon_buf.Length;
            sv.signon.cursize = 0;
            sv.signon.data = sv.signon_buf;

        // leave slots at start for clients only
	        sv.num_edicts = svs.maxclients+1;
	        for (i=0 ; i<svs.maxclients ; i++)
	        {
		        ent = prog.EDICT_NUM(i+1);
		        svs.clients[i].edict = ent;
	        }

            sv.state = server_state_t.ss_loading;
	        sv.paused = false;

	        sv.time = 1.0;
        	
	        sv.name = server;
	        sv.modelname = "maps/" + server + ".bsp";
	        sv.worldmodel = model.Mod_ForName (sv.modelname, false);
	        if (sv.worldmodel == null)
	        {
		        console.Con_Printf ("Couldn't spawn server " + sv.modelname + "\n");
		        sv.active = false;
		        return;
	        }
	        sv.models[1] = sv.worldmodel;

            //
            // clear world interaction links
            //
            //SV_ClearWorld();

            //sv.model_precache[0] = pr_strings;
            sv.model_precache[0] = "";
            sv.model_precache[1] = sv.modelname;
            for (i = 1; i < sv.worldmodel.numsubmodels; i++)
            {
                sv.model_precache[1 + i] = localmodels[i];
                sv.models[i + 1] = model.Mod_ForName(localmodels[i], false);
            }

            //
            // load the rest of the entities
            //	
            ent = prog.EDICT_NUM(0);
            ent.v.clear();
            ent.free = false;
            ent.v.model = prog.getStringIndex(sv.worldmodel.name) - 15000;
            ent.v.modelindex = 1;		// world model
            ent.v.solid = SOLID_BSP;
            ent.v.movetype = MOVETYPE_PUSH;

	        if (host.coop.value != 0)
                prog.pr_global_struct[0].coop = host.coop.value;
	        else
                prog.pr_global_struct[0].deathmatch = host.deathmatch.value;

            prog.pr_global_struct[0].mapname = prog.getStringIndex(sv.name) - 15000;

        // serverflags are for cross level information (sigils)
	        prog.pr_global_struct[0].serverflags = svs.serverflags;

            prog.ED_LoadFromFile(sv.worldmodel.entities);

	        sv.active = true;

        // all setup is completed, any further precache statements are errors
            sv.state = server_state_t.ss_active;
        	
        // run two frames to allow everything to settle
	        host.host_frametime = 0.1;
	        SV_Physics ();
	        SV_Physics ();

        // create a baseline for more efficient communications
	        SV_CreateBaseline ();

        // send serverinfo to all connected clients
            for (i = 0; i < svs.maxclients; i++)
            {
                host.host_client = svs.clients[i];
                if (host.host_client.active)
                    SV_SendServerinfo(host.host_client);
            }
        	
	        console.Con_DPrintf ("Server spawned.\n");
        }
    }
}
