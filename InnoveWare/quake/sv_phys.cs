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
// sv_phys.c

namespace quake
{
    public partial class server
    {
        /*


        pushmove objects do not obey gravity, and do not interact with each other or trigger fields, but block normal movement and push normal objects when they move.

        onground is set for toss objects when they come to a complete rest.  it is set for steping or walking objects 

        doors, plats, etc are SOLID_BSP, and MOVETYPE_PUSH
        bonus items are SOLID_TRIGGER touch, and MOVETYPE_TOSS
        corpses are SOLID_NOT and MOVETYPE_TOSS
        crates are SOLID_BBOX and MOVETYPE_TOSS
        walking monsters are SOLID_SLIDEBOX and MOVETYPE_STEP
        flying/floating monsters are SOLID_SLIDEBOX and MOVETYPE_FLY

        solid_edge items only clip against bsp models.

        */

        static cvar_t sv_friction = new cvar_t("sv_friction", "4", false, true);
        static cvar_t sv_stopspeed = new cvar_t("sv_stopspeed", "100");
        public static cvar_t sv_gravity = new cvar_t("sv_gravity", "800", false, true);
        static cvar_t sv_maxvelocity = new cvar_t("sv_maxvelocity","2000");
        static cvar_t sv_nostep = new cvar_t("sv_nostep", "0");

        public const double	MOVE_EPSILON	= 0.01;

        /*
        ================
        SV_CheckAllEnts
        ================
        */
        void SV_CheckAllEnts ()
        {
        }

        /*
        ================
        SV_CheckVelocity
        ================
        */
        static void SV_CheckVelocity (prog.edict_t ent)
        {
	        int		i;

        //
        // bound velocity
        //
	        for (i=0 ; i<3 ; i++)
	        {
		        if (double.IsNaN(ent.v.velocity[i]))
		        {
			        console.Con_Printf ("Got a NaN velocity on " + prog.pr_string(ent.v.classname) + "\n");
			        ent.v.velocity[i] = 0;
		        }
		        if (double.IsNaN(ent.v.origin[i]))
		        {
                    console.Con_Printf("Got a NaN origin on " + prog.pr_string(ent.v.classname) + "\n");
			        ent.v.origin[i] = 0;
		        }
		        if (ent.v.velocity[i] > sv_maxvelocity.value)
			        ent.v.velocity[i] = sv_maxvelocity.value;
		        else if (ent.v.velocity[i] < -sv_maxvelocity.value)
			        ent.v.velocity[i] = -sv_maxvelocity.value;
	        }
        }

        /*
        =============
        SV_RunThink

        Runs thinking code if time.  There is some play in the exact time the think
        function will be called, because it is called before any movement is done
        in a frame.  Not used for pushmove objects, because they must be exact.
        Returns false if the entity removed itself.
        =============
        */
        static bool SV_RunThink(prog.edict_t ent)
        {
            double thinktime;

            thinktime = ent.v.nextthink;
            if (thinktime <= 0 || thinktime > sv.time + host.host_frametime)
                return true;

            if (thinktime < sv.time)
                thinktime = sv.time;	// don't let things stay in the past.
            // it is possible to start that way
            // by a trigger with a local time.
            ent.v.nextthink = 0;
            prog.pr_global_struct[0].time = thinktime;
            prog.pr_global_struct[0].self = prog.EDICT_TO_PROG(ent);
            prog.pr_global_struct[0].other = prog.EDICT_TO_PROG(sv.edicts[0]);
            prog.PR_ExecuteProgram(prog.pr_functions[ent.v.think]);
            return !ent.free;
        }

        /*
        ============
        SV_AddGravity

        ============
        */
        static void SV_AddGravity (prog.edict_t ent)
        {
	        double	ent_gravity;

	        /*eval_t	*val;

	        val = GetEdictFieldValue(ent, "gravity");
	        if (val && val._float)
		        ent_gravity = val._float;
	        else*/
		        ent_gravity = 1.0;
	        ent.v.velocity[2] -= ent_gravity * sv_gravity.value * host.host_frametime;
        }

        /*
        ================
        SV_Physics_Pusher

        ================
        */
        static void SV_Physics_Pusher (prog.edict_t ent)
        {
	        double	thinktime;
	        double	oldltime;
	        double	movetime;

	        oldltime = ent.v.ltime;
        	
	        thinktime = ent.v.nextthink;
	        if (thinktime < ent.v.ltime + host.host_frametime)
	        {
		        movetime = thinktime - ent.v.ltime;
		        if (movetime < 0)
			        movetime = 0;
	        }
	        else
		        movetime = host.host_frametime;

	        if (movetime != 0)
	        {
			        //SV_PushMove (ent, movetime);	// advances ent.v.ltime if not blocked
	        }
        		
	        if (thinktime > oldltime && thinktime <= ent.v.ltime)
	        {
		        ent.v.nextthink = 0;
		        prog.pr_global_struct[0].time = sv.time;
                prog.pr_global_struct[0].self = prog.EDICT_TO_PROG(ent);
                prog.pr_global_struct[0].other = prog.EDICT_TO_PROG(sv.edicts[0]);
                prog.PR_ExecuteProgram(prog.pr_functions[ent.v.think]);
		        if (ent.free)
			        return;
	        }
        }

        /*
        ================
        SV_Physics_Client

        Player character actions
        ================
        */
        static void SV_Physics_Client (prog.edict_t ent, int num)
        {
	        if ( ! svs.clients[num-1].active )
		        return;		// unconnected slot

        //
        // call standard client pre-think
        //	
            prog.pr_global_struct[0].time = sv.time;
            prog.pr_global_struct[0].self = prog.EDICT_TO_PROG(ent);
            prog.PR_ExecuteProgram(prog.pr_functions[prog.pr_global_struct[0].PlayerPreThink]);
        	
        //
        // do a move
        //
	        SV_CheckVelocity (ent);

        //
        // decide which move function to call
        //
	        switch ((int)ent.v.movetype)
	        {
	        case MOVETYPE_NONE:
		        if (!SV_RunThink (ent))
			        return;
		        break;

	        case MOVETYPE_WALK:
		        if (!SV_RunThink (ent))
			        return;
		        /*if (!SV_CheckWater (ent) && ! ((int)ent.v.flags & FL_WATERJUMP) )
			        SV_AddGravity (ent);
		        SV_CheckStuck (ent);
		        SV_WalkMove (ent);*/
		        break;
        		
	        case MOVETYPE_TOSS:
	        case MOVETYPE_BOUNCE:
		        //SV_Physics_Toss (ent);
		        break;

	        case MOVETYPE_FLY:
		        if (!SV_RunThink (ent))
			        return;
		        //SV_FlyMove (ent, host.host_frametime, null);
		        break;
        		
	        case MOVETYPE_NOCLIP:
		        if (!SV_RunThink (ent))
			        return;
		        mathlib.VectorMA (ent.v.origin, host.host_frametime, ent.v.velocity, ref ent.v.origin);
		        break;
        		
	        default:
		        sys_linux.Sys_Error ("SV_Physics_client: bad movetype " + (int)ent.v.movetype);
                break;
	        }

        //
        // call standard player post-think
        //		
	        //SV_LinkEdict (ent, true);

	        prog.pr_global_struct[0].time = sv.time;
            prog.pr_global_struct[0].self = prog.EDICT_TO_PROG(ent);
            prog.PR_ExecuteProgram(prog.pr_functions[prog.pr_global_struct[0].PlayerPostThink]);
        }

        //============================================================================

        /*
        =============
        SV_Physics_None

        Non moving objects can only think
        =============
        */
        static void SV_Physics_None (prog.edict_t ent)
        {
        // regular thinking
	        SV_RunThink (ent);
        }

        /*
        =============
        SV_Physics_Noclip

        A moving object that doesn't obey physics
        =============
        */
        static void SV_Physics_Noclip (prog.edict_t ent)
        {
        // regular thinking
	        if (!SV_RunThink (ent))
		        return;
        	
	        mathlib.VectorMA (ent.v.angles, host.host_frametime, ent.v.avelocity, ref ent.v.angles);
            mathlib.VectorMA(ent.v.origin, host.host_frametime, ent.v.velocity, ref ent.v.origin);

	        //SV_LinkEdict (ent, false);
        }

        /*
        =============
        SV_Physics_Toss

        Toss, bounce, and fly movement.  When onground, do nothing.
        =============
        */
        static void SV_Physics_Toss (prog.edict_t ent)
        {
	        //trace_t	trace;
	        double[]	move = new double[3];
	        double      backoff;

            // regular thinking
	        if (!SV_RunThink (ent))
		        return;

        // if onground, return without moving
	        if ( ((int)ent.v.flags & FL_ONGROUND) != 0 )
		        return;

	        SV_CheckVelocity (ent);

        // add gravity
	        if (ent.v.movetype != MOVETYPE_FLY
	        && ent.v.movetype != MOVETYPE_FLYMISSILE)
		        SV_AddGravity (ent);

        // move angles
	        mathlib.VectorMA (ent.v.angles, host.host_frametime, ent.v.avelocity, ref ent.v.angles);

        // move origin
	        mathlib.VectorScale (ent.v.velocity, host.host_frametime, ref move);
	        /*trace = SV_PushEntity (ent, move);
	        if (trace.fraction == 1)
		        return;*/
	        if (ent.free)
		        return;
        	
	        if (ent.v.movetype == MOVETYPE_BOUNCE)
		        backoff = 1.5;
	        else
		        backoff = 1;

	        //ClipVelocity (ent.v.velocity, trace.plane.normal, ent.v.velocity, backoff);

        // stop if on ground
	        /*if (trace.plane.normal[2] > 0.7)
	        {		
		        if (ent.v.velocity[2] < 60 || ent.v.movetype != MOVETYPE_BOUNCE)
		        {
			        ent.v.flags = (int)ent.v.flags | FL_ONGROUND;
			        ent.v.groundentity = EDICT_TO_PROG(trace.ent);
			        VectorCopy (vec3_origin, ent.v.velocity);
			        VectorCopy (vec3_origin, ent.v.avelocity);
		        }
	        }*/
        	
        // check for in water
	        //SV_CheckWaterTransition (ent);
        }

        /*
        ================
        SV_Physics

        ================
        */
        public static void SV_Physics ()
        {
	        int		        i;
	        prog.edict_t    ent;

        // let the progs know that a new frame has started
            prog.pr_global_struct[0].self = prog.EDICT_TO_PROG(sv.edicts[0]);
            prog.pr_global_struct[0].other = prog.EDICT_TO_PROG(sv.edicts[0]);
            prog.pr_global_struct[0].time = sv.time;
	        prog.PR_ExecuteProgram (prog.pr_functions[prog.pr_global_struct[0].StartFrame]);

        //SV_CheckAllEnts ();

        //
        // treat each object in turn
        //
	        for (i=0 ; i<sv.num_edicts ; i++)
	        {
    	        ent = sv.edicts[i];
		        if (ent.free)
			        continue;

		        if (prog.pr_global_struct[0].force_retouch != 0)
		        {
			        //SV_LinkEdict (ent, true);	// force retouch even for stationary
		        }

		        if (i > 0 && i <= svs.maxclients)
			        SV_Physics_Client (ent, i);
		        else if (ent.v.movetype == MOVETYPE_PUSH)
			        SV_Physics_Pusher (ent);
		        else if (ent.v.movetype == MOVETYPE_NONE)
			        SV_Physics_None (ent);
		        else if (ent.v.movetype == MOVETYPE_NOCLIP)
			        SV_Physics_Noclip (ent);
		        /*else if (ent.v.movetype == MOVETYPE_STEP)
			        SV_Physics_Step (ent);*/
		        else if (ent.v.movetype == MOVETYPE_TOSS 
		        || ent.v.movetype == MOVETYPE_BOUNCE
		        || ent.v.movetype == MOVETYPE_FLY
		        || ent.v.movetype == MOVETYPE_FLYMISSILE)
			        SV_Physics_Toss (ent);
		        else
			        sys_linux.Sys_Error ("SV_Physics: bad movetype " + (int)ent.v.movetype);
	        }

	        if (prog.pr_global_struct[0].force_retouch != 0)
		        prog.pr_global_struct[0].force_retouch--;	

	        sv.time += host.host_frametime;
        }
    }
}