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
// sv_user.c -- server code for moving users

namespace quake
{
    public partial class server
    {
        public static prog.edict_t  sv_player;

        static cvar_t	            sv_edgefriction = new cvar_t("edgefriction", "2");

        static double[]             forward = new double[3], right = new double[3], up = new double[3];

        static double[]	            wishdir = new double[3];
        static double	            wishspeed;

        // world
        static double[]             angles;
        static double[]             origin;
        static double[]             velocity;

        static bool	                onground;

        static client.usercmd_t     cmd = new client.usercmd_t();

        static cvar_t	            sv_idealpitchscale = new cvar_t("sv_idealpitchscale","0.8");

        /*
        ===============
        SV_SetIdealPitch
        ===============
        */
        const int	MAX_FORWARD	= 6;
        static void SV_SetIdealPitch ()
        {
	        double	    angleval, sinval, cosval;
	        //trace_t	tr;
	        double[]    top = new double[3], bottom = new double[3];
	        double[]	z = new double[MAX_FORWARD];
	        int		    i, j;
	        int		    step, dir, steps;

	        if (((int)sv_player.v.flags & FL_ONGROUND) == 0)
		        return;
        		
	        angleval = sv_player.v.angles[quakedef.YAW] * mathlib.M_PI*2 / 360;
	        sinval = Math.Sin(angleval);
	        cosval = Math.Cos(angleval);

	        for (i=0 ; i<MAX_FORWARD ; i++)
	        {
		        top[0] = sv_player.v.origin[0] + cosval*(i+3)*12;
		        top[1] = sv_player.v.origin[1] + sinval*(i+3)*12;
		        top[2] = sv_player.v.origin[2] + sv_player.v.view_ofs[2];
        		
		        bottom[0] = top[0];
		        bottom[1] = top[1];
		        bottom[2] = top[2] - 160;
        		
		        /*tr = SV_Move (top, vec3_origin, vec3_origin, bottom, 1, sv_player);
		        if (tr.allsolid)
			        return;	// looking at a wall, leave ideal the way is was*/

		        /*if (tr.fraction == 1)
			        return;	// near a dropoff
        		
		        z[i] = top[2] + tr.fraction*(bottom[2]-top[2]);*/
	        }
        	
	        dir = 0;
	        steps = 0;
	        for (j=1 ; j<i ; j++)
	        {
		        step = (int)(z[j] - z[j-1]);
                if (step > -quakedef.ON_EPSILON && step < quakedef.ON_EPSILON)
			        continue;

                if (dir != 0 && (step - dir > quakedef.ON_EPSILON || step - dir < -quakedef.ON_EPSILON))
			        return;		// mixed changes

		        steps++;	
		        dir = step;
	        }
        	
	        if (dir == 0)
	        {
		        sv_player.v.idealpitch = 0;
		        return;
	        }
        	
	        if (steps < 2)
		        return;
	        sv_player.v.idealpitch = -dir * sv_idealpitchscale.value;
        }

        /*
        ==================
        SV_UserFriction

        ==================
        */
        static void SV_UserFriction ()
        {
	        double[]    vel;
	        double	    speed, newspeed, control;
	        double[]	start = new double[3], stop = new double[3];
	        double	    friction;
	        //trace_t	trace;
        	
	        vel = velocity;

            speed = Math.Sqrt(vel[0] * vel[0] + vel[1] * vel[1]);
	        if (speed == 0)
		        return;

        // if the leading edge is over a dropoff, increase friction
	        start[0] = stop[0] = origin[0] + vel[0]/speed*16;
	        start[1] = stop[1] = origin[1] + vel[1]/speed*16;
	        start[2] = origin[2] + sv_player.v.mins[2];
	        stop[2] = start[2] - 34;

	        //trace = SV_Move (start, vec3_origin, vec3_origin, stop, true, sv_player);

	        /*if (trace.fraction == 1.0)
		        friction = sv_friction.value*sv_edgefriction.value;
	        else*/
		        friction = sv_friction.value;

        // apply friction	
	        control = speed < sv_stopspeed.value ? sv_stopspeed.value : speed;
	        newspeed = speed - host.host_frametime*control*friction;
        	
	        if (newspeed < 0)
		        newspeed = 0;
	        newspeed /= speed;

	        vel[0] = vel[0] * newspeed;
	        vel[1] = vel[1] * newspeed;
	        vel[2] = vel[2] * newspeed;
        }

        /*
        ==============
        SV_Accelerate
        ==============
        */
        static cvar_t sv_maxspeed = new cvar_t( "sv_maxspeed", "320", false, true );
        static cvar_t sv_accelerate = new cvar_t( "sv_accelerate", "10" );
        static void SV_Accelerate ()
        {
	        int			i;
	        double		addspeed, accelspeed, currentspeed;

	        currentspeed = mathlib.DotProduct (velocity, wishdir);
	        addspeed = wishspeed - currentspeed;
	        if (addspeed <= 0)
		        return;
	        accelspeed = sv_accelerate.value*host.host_frametime*wishspeed;
	        if (accelspeed > addspeed)
		        accelspeed = addspeed;
        	
	        for (i=0 ; i<3 ; i++)
		        velocity[i] += accelspeed*wishdir[i];	
        }

        static void SV_AirAccelerate(double[] wishveloc)
        {
            int     i;
            double  addspeed, wishspd, accelspeed, currentspeed;

            wishspd = mathlib.VectorNormalize(ref wishveloc);
            if (wishspd > 30)
                wishspd = 30;
            currentspeed = mathlib.DotProduct(velocity, wishveloc);
            addspeed = wishspd - currentspeed;
            if (addspeed <= 0)
                return;
            //	accelspeed = sv_accelerate.value * host_frametime;
            accelspeed = sv_accelerate.value * wishspeed * host.host_frametime;
            if (accelspeed > addspeed)
                accelspeed = addspeed;

            for (i = 0; i < 3; i++)
                velocity[i] += accelspeed * wishveloc[i];
        }

        static void DropPunchAngle ()
        {
	        double	len;
        	
	        len = mathlib.VectorNormalize (ref sv_player.v.punchangle);
        	
	        len -= 10*host.host_frametime;
	        if (len < 0)
		        len = 0;
	        mathlib.VectorScale (sv_player.v.punchangle, len, ref sv_player.v.punchangle);
        }

        /*
        ===================
        SV_WaterMove

        ===================
        */
        static void SV_WaterMove ()
        {
	        int		    i;
	        double[]    wishvel = new double[3];
	        double	    speed, newspeed, wishspeed, addspeed, accelspeed;

        //
        // user intentions
        //
	        mathlib.AngleVectors (sv_player.v.v_angle, ref forward, ref right, ref up);

	        for (i=0 ; i<3 ; i++)
		        wishvel[i] = forward[i]*cmd.forwardmove + right[i]*cmd.sidemove;

	        if (cmd.forwardmove == 0 && cmd.sidemove == 0 && cmd.upmove == 0)
		        wishvel[2] -= 60;		// drift towards bottom
	        else
		        wishvel[2] += cmd.upmove;

	        wishspeed = mathlib.Length(wishvel);
	        if (wishspeed > sv_maxspeed.value)
	        {
		        mathlib.VectorScale (wishvel, sv_maxspeed.value/wishspeed, ref wishvel);
		        wishspeed = sv_maxspeed.value;
	        }
	        wishspeed *= 0.7;

        //
        // water friction
        //
	        speed = mathlib.Length (velocity);
	        if (speed != 0)
	        {
		        newspeed = speed - host.host_frametime * speed * sv_friction.value;
		        if (newspeed < 0)
			        newspeed = 0;	
		        mathlib.VectorScale (velocity, newspeed/speed, ref velocity);
	        }
	        else
		        newspeed = 0;
        	
        //
        // water acceleration
        //
	        if (wishspeed == 0)
		        return;

	        addspeed = wishspeed - newspeed;
	        if (addspeed <= 0)
		        return;

	        mathlib.VectorNormalize (ref wishvel);
	        accelspeed = sv_accelerate.value * wishspeed * host.host_frametime;
	        if (accelspeed > addspeed)
		        accelspeed = addspeed;

	        for (i=0 ; i<3 ; i++)
		        velocity[i] += accelspeed * wishvel[i];
        }

        static void SV_WaterJump ()
        {
	        if (sv.time > sv_player.v.teleport_time
	        || sv_player.v.waterlevel == 0)
	        {
		        sv_player.v.flags = (int)sv_player.v.flags & ~FL_WATERJUMP;
		        sv_player.v.teleport_time = 0;
	        }
	        sv_player.v.velocity[0] = sv_player.v.movedir[0];
	        sv_player.v.velocity[1] = sv_player.v.movedir[1];
        }

        /*
        ===================
        SV_AirMove

        ===================
        */
        static void SV_AirMove ()
        {
	        int			i;
	        double[]	wishvel = new double[3];
	        double		fmove, smove;

	        mathlib.AngleVectors (sv_player.v.angles, ref forward, ref right, ref up);

	        fmove = cmd.forwardmove;
	        smove = cmd.sidemove;
        	
        // hack to not let you back into teleporter
	        if (sv.time < sv_player.v.teleport_time && fmove < 0)
		        fmove = 0;
        		
	        for (i=0 ; i<3 ; i++)
		        wishvel[i] = forward[i]*fmove + right[i]*smove;

	        if ( (int)sv_player.v.movetype != MOVETYPE_WALK)
		        wishvel[2] = cmd.upmove;
	        else
		        wishvel[2] = 0;

	        mathlib.VectorCopy (wishvel, ref wishdir);
            wishspeed = mathlib.VectorNormalize(ref wishdir);
	        if (wishspeed > sv_maxspeed.value)
	        {
                mathlib.VectorScale(wishvel, sv_maxspeed.value / wishspeed, ref wishvel);
		        wishspeed = sv_maxspeed.value;
	        }
        	
	        if ( sv_player.v.movetype == MOVETYPE_NOCLIP)
	        {	// noclip
                mathlib.VectorCopy(wishvel, ref velocity);
	        }
	        else if ( onground )
	        {
		        SV_UserFriction ();
		        SV_Accelerate ();
	        }
	        else
	        {	// not on ground, so little effect on velocity
		        SV_AirAccelerate (wishvel);
	        }		
        }

        /*
        ===================
        SV_ClientThink

        the move fields specify an intended velocity in pix/sec
        the angle fields specify an exact angular motion in degrees
        ===================
        */
        static void SV_ClientThink ()
        {
            double[] v_angle = new double[3];

            if (sv_player.v.movetype == MOVETYPE_NONE)
                return;

            onground = ((int)sv_player.v.flags & FL_ONGROUND) != 0;

            origin = sv_player.v.origin;
            velocity = sv_player.v.velocity;

            DropPunchAngle();

            //
            // if dead, behave differently
            //
            if (sv_player.v.health <= 0)
                return;

            //
            // angles
            // show 1/3 the pitch angle and all the roll angle
            cmd = host.host_client.cmd;
            angles = sv_player.v.angles;

            mathlib.VectorAdd(sv_player.v.v_angle, sv_player.v.punchangle, ref v_angle);
            angles[quakedef.ROLL] = view.V_CalcRoll(sv_player.v.angles, sv_player.v.velocity) * 4;
            if (sv_player.v.fixangle == 0)
            {
                angles[quakedef.PITCH] = -v_angle[quakedef.PITCH] / 3;
                angles[quakedef.YAW] = v_angle[quakedef.YAW];
            }

            if (((int)sv_player.v.flags & FL_WATERJUMP) != 0)
            {
                SV_WaterJump();
                return;
            }
            //
            // walk
            //
            if ((sv_player.v.waterlevel >= 2)
            && (sv_player.v.movetype != MOVETYPE_NOCLIP))
            {
                SV_WaterMove();
                return;
            }

            SV_AirMove();	
        }
        
        /*
        ===================
        SV_ReadClientMove
        ===================
        */
        static void SV_ReadClientMove (client.usercmd_t move)
        {
	        int		    i;
	        double[]    angle = new double[3];
	        int		    bits;
        	
        // read ping time
            host.host_client.ping_times[host.host_client.num_pings % NUM_PING_TIMES]
		        = sv.time - common.MSG_ReadFloat ();
	        host.host_client.num_pings++;

        // read current angles	
	        for (i=0 ; i<3 ; i++)
		        angle[i] = common.MSG_ReadAngle ();

	        mathlib.VectorCopy (angle, ref host.host_client.edict.v.v_angle);
        		
        // read movement
            move.forwardmove = common.MSG_ReadShort();
            move.sidemove = common.MSG_ReadShort();
            move.upmove = common.MSG_ReadShort();
        	
        // read buttons
            bits = common.MSG_ReadByte();
	        host.host_client.edict.v.button0 = bits & 1;
            host.host_client.edict.v.button2 = (bits & 2) >> 1;

            i = common.MSG_ReadByte();
	        if (i != 0)
		        host.host_client.edict.v.impulse = i;
        }

        /*
        ===================
        SV_ReadClientMessage

        Returns false if the client should be killed
        ===================
        */
        static bool SV_ReadClientMessage ()
        {
	        int		ret;
	        int		cmd;
	        string  s;
        	
	        do
	        {
        nextmsg:
                ret = net.NET_GetMessage (host.host_client.netconnection);
		        if (ret == -1)
		        {
                    sys_linux.Sys_Printf("SV_ReadClientMessage: NET_GetMessage failed\n");
			        return false;
		        }
		        if (ret == 0)
			        return true;
        					
		        common.MSG_BeginReading ();
        		
		        while (true)
		        {
                    if (!host.host_client.active)
				        return false;	// a command caused an error

			        if (common.msg_badread)
			        {
				        sys_linux.Sys_Printf ("SV_ReadClientMessage: badread\n");
				        return false;
			        }	
        	
			        cmd = common.MSG_ReadChar ();
        			
			        switch (cmd)
			        {
			        case -1:
				        goto nextmsg;		// end of message
        				
			        default:
				        sys_linux.Sys_Printf ("SV_ReadClientMessage: unknown command char\n");
				        return false;
        							
			        case net.clc_nop:
        //				Sys_Printf ("clc_nop\n");
				        break;
        				
			        case net.clc_stringcmd:	
				        s = common.MSG_ReadString ();
				        if (host.host_client.privileged)
					        ret = 2;
				        else
					        ret = 0;
				        if (s.StartsWith("status"))
					        ret = 1;
                        else if (s.StartsWith("god"))
					        ret = 1;
                        else if (s.StartsWith("notarget"))
					        ret = 1;
                        else if (s.StartsWith("fly"))
					        ret = 1;
                        else if (s.StartsWith("name"))
					        ret = 1;
                        else if (s.StartsWith("noclip"))
					        ret = 1;
                        else if (s.StartsWith("say"))
					        ret = 1;
                        else if (s.StartsWith("say_team"))
					        ret = 1;
                        else if (s.StartsWith("tell"))
					        ret = 1;
                        else if (s.StartsWith("color"))
					        ret = 1;
                        else if (s.StartsWith("kill"))
					        ret = 1;
                        else if (s.StartsWith("pause"))
					        ret = 1;
                        else if (s.StartsWith("spawn"))
					        ret = 1;
                        else if (s.StartsWith("begin"))
					        ret = 1;
                        else if (s.StartsWith("prespawn"))
					        ret = 1;
                        else if (s.StartsWith("kick"))
					        ret = 1;
                        else if (s.StartsWith("ping"))
					        ret = 1;
                        else if (s.StartsWith("give"))
					        ret = 1;
                        else if (s.StartsWith("ban"))
					        ret = 1;
				        if (ret == 2)
                            quake.cmd.Cbuf_InsertText(s);
				        else if (ret == 1)
					        quake.cmd.Cmd_ExecuteString ((s + "\0").ToCharArray(), quake.cmd.cmd_source_t.src_client);
				        else
					        console.Con_DPrintf(host.host_client.name + " tried to " + s + "\n");
				        break;
        				
			        case net.clc_disconnect:
        //				Sys_Printf ("SV_ReadClientMessage: client disconnected\n");
				        return false;
        			
			        case net.clc_move:
                        SV_ReadClientMove(host.host_client.cmd);
                        break;
			        }
		        }
	        } while (ret == 1);
        	
	        return true;
        }
        
        /*
        ==================
        SV_RunClients
        ==================
        */
        public static void SV_RunClients ()
        {
	        int				i;
        	
	        for (i=0 ; i<svs.maxclients ; i++)
	        {
                host.host_client = svs.clients[i];
		        if (!host.host_client.active)
			        continue;
        	
		        sv_player = host.host_client.edict;

		        if (!SV_ReadClientMessage ())
		        {
			        continue;
		        }

                if (!host.host_client.spawned)
		        {
			        continue;
		        }

        // always pause in single player if in console or menus
		        if (!sv.paused && (svs.maxclients > 1 || keys.key_dest == keys.keydest_t.key_game) )
			        SV_ClientThink ();
	        }
        }
    }
}