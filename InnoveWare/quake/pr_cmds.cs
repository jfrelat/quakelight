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

namespace quake
{
    public partial class prog
    {
        static void RETURN_EDICT(edict_t e) { pr_globals_write(OFS_RETURN, EDICT_TO_PROG(e)); }

        /*
        ===============================================================================

						        BUILT-IN FUNCTIONS

        ===============================================================================
        */

        static string @out = new string(new char[256]);
        static string PF_VarString (int	first)
        {
	        int		i;
        	
	        @out = "";
	        for (i=first ; i<pr_argc ; i++)
	        {
		        @out += G_STRING((OFS_PARM0+i*3));
	        }
	        return @out;
        }

        /*
        =================
        PF_errror

        This is a TERMINAL error, which will kill off the entire server.
        Dumps self.

        error(value)
        =================
        */
        static void PF_error ()
        {
            string  s;
            edict_t ed;

            s = PF_VarString(0);
            console.Con_Printf("======SERVER ERROR in " + pr_string(pr_xfunction.s_name) + ":\n" + s + "\n");
            ed = PROG_TO_EDICT(pr_global_struct[0].self);
            //ED_Print(ed);

            host.Host_Error("Program error");
        }

        /*
        =================
        PF_objerror

        Dumps out self, then an error message.  The program is aborted and self is
        removed, but the level can continue.

        objerror(value)
        =================
        */
        static void PF_objerror ()
        {
            string  s;
            edict_t ed;

            s = PF_VarString(0);
            console.Con_Printf("======OBJECT ERROR in " + pr_string(pr_xfunction.s_name) + ":\n" + s + "\n");
            ed = PROG_TO_EDICT(pr_global_struct[0].self);
            //ED_Print(ed);
            ED_Free(ed);

            host.Host_Error("Program error");
        }

        /*
        ==============
        PF_makevectors

        Writes new values for v_forward, v_up, and v_right based on angles
        makevectors(vector)
        ==============
        */
        static void PF_makevectors ()
        {
            mathlib.AngleVectors(G_VECTOR(OFS_PARM0), ref pr_global_struct[0].v_forward, ref pr_global_struct[0].v_right, ref pr_global_struct[0].v_up);
        }

        /*
        =================
        PF_setorigin

        This is the only valid way to move an object without using the physics of the world (setting velocity and waiting).  Directly changing origin will not set internal links correctly, so clipping would be messed up.  This should be called when an object is spawned, and then only if it is teleported.

        setorigin (entity, origin)
        =================
        */
        static void PF_setorigin ()
        {
            edict_t     e;
            double[]    org;

            e = G_EDICT(OFS_PARM0);
            org = G_VECTOR(OFS_PARM1);
            mathlib.VectorCopy(org, ref e.v.origin);
            //SV_LinkEdict(e, false);
        }

        static void SetMinMaxSize (edict_t e, double[] min, double[] max, bool rotate)
        {
	        double[]	angles;
	        double[]	rmin = new double[3], rmax = new double[3];
	        double[][]  bounds = new double[2][];
	        double[]	xvector = new double[2], yvector = new double[2];
	        double	    a;
	        double[]	@base = new double[3], transformed = new double[3];
	        int		    i, j, k, l;

            for (int kk = 0; kk < 2; kk++)
                bounds[kk] = new double[3];

	        for (i=0 ; i<3 ; i++)
		        if (min[i] > max[i])
			        PR_RunError ("backwards mins/maxs");

	        rotate = false;		// FIXME: implement rotation properly again

	        if (!rotate)
	        {
		        mathlib.VectorCopy (min, ref rmin);
		        mathlib.VectorCopy (max, ref rmax);
	        }
	        else
	        {
	        // find min / max for rotations
		        angles = e.v.angles;
        		
		        a = angles[1]/180 * mathlib.M_PI;
        		
		        xvector[0] = Math.Cos(a);
		        xvector[1] = Math.Sin(a);
		        yvector[0] = -Math.Sin(a);
		        yvector[1] = Math.Cos(a);
        		
		        mathlib.VectorCopy (min, ref bounds[0]);
                mathlib.VectorCopy (max, ref bounds[1]);
        		
		        rmin[0] = rmin[1] = rmin[2] = 9999;
		        rmax[0] = rmax[1] = rmax[2] = -9999;
        		
		        for (i=0 ; i<= 1 ; i++)
		        {
			        @base[0] = bounds[i][0];
			        for (j=0 ; j<= 1 ; j++)
			        {
				        @base[1] = bounds[j][1];
				        for (k=0 ; k<= 1 ; k++)
				        {
					        @base[2] = bounds[k][2];
        					
				        // transform the point
					        transformed[0] = xvector[0]*@base[0] + yvector[0]*@base[1];
					        transformed[1] = xvector[1]*@base[0] + yvector[1]*@base[1];
					        transformed[2] = @base[2];
        					
					        for (l=0 ; l<3 ; l++)
					        {
						        if (transformed[l] < rmin[l])
							        rmin[l] = transformed[l];
						        if (transformed[l] > rmax[l])
							        rmax[l] = transformed[l];
					        }
				        }
			        }
		        }
	        }
        	
        // set derived values
	        mathlib.VectorCopy (rmin, ref e.v.mins);
            mathlib.VectorCopy (rmax, ref e.v.maxs);
            mathlib.VectorSubtract (max, min, ref e.v.size);
        	
	        //SV_LinkEdict (e, false);
        }

        /*
        =================
        PF_setsize

        the size box is rotated by the current angle

        setsize (entity, minvector, maxvector)
        =================
        */
        static void PF_setsize ()
        {
	        edict_t	    e;
	        double[]	min, max;
        	
	        e = G_EDICT(OFS_PARM0);
	        min = G_VECTOR(OFS_PARM1);
	        max = G_VECTOR(OFS_PARM2);
	        SetMinMaxSize (e, min, max, false);
        }

        /*
        =================
        PF_setmodel

        setmodel(entity, model)
        =================
        */
        static void PF_setmodel ()
        {
	        edict_t         e;
	        string          m, check;
	        model.model_t	mod;
	        int		        i;

	        e = G_EDICT(OFS_PARM0);
	        m = G_STRING(OFS_PARM1);

        // check to see if model was properly precached
            for (i = 0, check = server.sv.model_precache[i]; check != null; i++)
            {
                check = server.sv.model_precache[i];
                if (check.CompareTo(m) == 0)
                    break;
            }
        			
	        if (check == null)
		        PR_RunError ("no precache: " + m + "\n");
        		
	        e.v.model = getStringIndex(m) - 15000;
	        e.v.modelindex = i; //SV_ModelIndex (m);

	        mod = server.sv.models[ (int)e.v.modelindex];  // Mod_ForName (m, true);
        	
/*	        if (mod != null)
		        SetMinMaxSize (e, mod->mins, mod->maxs, true);
	        else
		        SetMinMaxSize (e, vec3_origin, vec3_origin, true);*/
        }

        /*
        =================
        PF_bprint

        broadcast print to everyone on server

        bprint(value)
        =================
        */
        static void PF_bprint ()
        {
        }

        /*
        =================
        PF_sprint

        single print to a specific client

        sprint(clientent, value)
        =================
        */
        static void PF_sprint ()
        {
        }

        /*
        =================
        PF_centerprint

        single print to a specific client

        centerprint(clientent, value)
        =================
        */
        static void PF_centerprint ()
        {
        }

        /*
        =================
        PF_normalize

        vector normalize(vector)
        =================
        */
        static void PF_normalize ()
        {
        }

        /*
        =================
        PF_vlen

        scalar vlen(vector)
        =================
        */
        static void PF_vlen ()
        {
        }

        /*
        =================
        PF_vectoyaw

        float vectoyaw(vector)
        =================
        */
        static void PF_vectoyaw ()
        {
        }

        /*
        =================
        PF_vectoangles

        vector vectoangles(vector)
        =================
        */
        static void PF_vectoangles ()
        {
        }

        /*
        =================
        PF_Random

        Returns a number from 0<= num < 1

        random()
        =================
        */
        static void PF_random ()
        {
	        double		num;
        		
	        num = (helper.rand ()&0x7fff) / ((double)0x7fff);
        	
	        pr_globals_write(OFS_RETURN, num);
        }

        /*
        =================
        PF_particle

        particle(origin, color, count)
        =================
        */
        static void PF_particle ()
        {
        }
        
        /*
        =================
        PF_ambientsound

        =================
        */
        static void PF_ambientsound ()
        {
        }

        /*
        =================
        PF_sound

        Each entity can have eight independant sound sources, like voice,
        weapon, feet, etc.

        Channel 0 is an auto-allocate channel, the others override anything
        allready running on that entity/channel pair.

        An attenuation of 0 will play full volume everywhere in the level.
        Larger attenuations will drop off.

        =================
        */
        static void PF_sound ()
        {
        }

        /*
        =================
        PF_break

        break()
        =================
        */
        static void PF_break ()
        {
        }

        /*
        =================
        PF_traceline

        Used for use tracing and shot targeting
        Traces are blocked by bbox and exact bsp entityes, and also slide box entities
        if the tryents flag is set.

        traceline (vector1, vector2, tryents)
        =================
        */
        static void PF_traceline ()
        {
        }

        /*
        =================
        PF_checkpos

        Returns true if the given entity can move to the given position from it's
        current position by walking or rolling.
        FIXME: make work...
        scalar checkpos (entity, vector)
        =================
        */
        static void PF_checkpos ()
        {
        }

        //============================================================================

        /*
        =================
        PF_checkclient

        Returns a client (or object that has a client enemy) that would be a
        valid target.

        If there are more than one valid options, they are cycled each frame

        If (self.origin + self.viewofs) is not in the PVS of the current target,
        it is not returned at all.

        name checkclient ()
        =================
        */
        static void PF_checkclient ()
        {
        }

        //============================================================================

        /*
        =================
        PF_stuffcmd

        Sends text over to the client's execution buffer

        stuffcmd (clientent, value)
        =================
        */
        static void PF_stuffcmd ()
        {
        }

        /*
        =================
        PF_localcmd

        Sends text over to the client's execution buffer

        localcmd (string)
        =================
        */
        static void PF_localcmd ()
        {
        }

        /*
        =================
        PF_cvar

        float cvar (string)
        =================
        */
        static void PF_cvar ()
        {
	        string	str;
        	
	        str = G_STRING(OFS_PARM0);
        	
	        pr_globals_write(OFS_RETURN, cvar_t.Cvar_VariableValue (str));
        }

        /*
        =================
        PF_cvar_set

        float cvar (string)
        =================
        */
        static void PF_cvar_set ()
        {
	        string	var, val;
        	
	        var = G_STRING(OFS_PARM0);
	        val = G_STRING(OFS_PARM1);
        	
	        cvar_t.Cvar_Set (var, val);
        }

        /*
        =================
        PF_findradius

        Returns a chain of entities that have origins within a spherical area

        findradius (origin, radius)
        =================
        */
        static void PF_findradius ()
        {
        }

        /*
        =========
        PF_dprint
        =========
        */
        static void PF_dprint ()
        {
        }

        static void PF_ftos ()
        {
        }

        static void PF_fabs ()
        {
            double v;
            v = G_FLOAT(OFS_PARM0);
            pr_globals_write(OFS_RETURN, Math.Abs(v));
        }

        static void PF_vtos ()
        {
        }

        static void PF_Spawn ()
        {
	        edict_t ed;
	        ed = ED_Alloc();
	        RETURN_EDICT(ed);
        }

        static void PF_Remove ()
        {
	        edict_t	ed;
        	
	        ed = G_EDICT(OFS_PARM0);
	        ED_Free (ed);
        }

        // entity (entity start, .string field, string match) find = #5;
        static void PF_Find ()
        {
	        int		e;	
	        int		f;
	        string	s, t;
	        edict_t	ed;

	        e = G_EDICTNUM(OFS_PARM0);
	        f = G_INT(OFS_PARM1);
	        s = G_STRING(OFS_PARM2);
	        if (s == null)
		        PR_RunError ("PF_Find: bad search string");
        		
	        for (e++ ; e < server.sv.num_edicts ; e++)
	        {
		        ed = EDICT_NUM(e);
		        if (ed.free)
			        continue;
		        t = E_STRING(ed,f);
		        if (t == null)
			        continue;
		        if (t.CompareTo(s) == 0)
		        {
			        RETURN_EDICT(ed);
			        return;
		        }
	        }

	        RETURN_EDICT(server.sv.edicts[0]);
        }

        static void PR_CheckEmptyString(string s)
        {
            if (s[0] <= ' ')
                PR_RunError("Bad string");
        }

        static void PF_precache_file ()
        {
        }

        static void PF_precache_sound ()
        {
            string  s;
            int     i;

            if (server.sv.state != server.server_state_t.ss_loading)
                PR_RunError("PF_Precache_*: Precache can only be done in spawn functions");

            s = G_STRING(OFS_PARM0);
            pr_globals_write(OFS_RETURN, G_INT(OFS_PARM0));
            PR_CheckEmptyString(s);

            for (i = 0; i < quakedef.MAX_SOUNDS; i++)
            {
                if (server.sv.sound_precache[i] == null)
                {
                    server.sv.sound_precache[i] = s;
                    return;
                }
                if (server.sv.sound_precache[i].CompareTo(s) == 0)
                    return;
            }
            PR_RunError("PF_precache_sound: overflow");
        }

        static void PF_precache_model ()
        {
            string  s;
            int     i;

            if (server.sv.state != server.server_state_t.ss_loading)
                PR_RunError("PF_Precache_*: Precache can only be done in spawn functions");

            s = G_STRING(OFS_PARM0);
            pr_globals_write(OFS_RETURN, G_INT(OFS_PARM0));
            PR_CheckEmptyString(s);

            for (i = 0; i < quakedef.MAX_MODELS; i++)
            {
                if (server.sv.model_precache[i] == null)
                {
                    server.sv.model_precache[i] = s;
                    server.sv.models[i] = model.Mod_ForName(s, true);
                    return;
                }
                if (server.sv.model_precache[i].CompareTo(s) == 0)
                    return;
            }
            PR_RunError("PF_precache_model: overflow");
        }

        static void PF_coredump ()
        {
        }

        static void PF_traceon ()
        {
        }

        static void PF_traceoff ()
        {
        }

        static void PF_eprint ()
        {
        }

        /*
        ===============
        PF_walkmove

        float(float yaw, float dist) walkmove
        ===============
        */
        static void PF_walkmove ()
        {
        }

        /*
        ===============
        PF_droptofloor

        void() droptofloor
        ===============
        */
        static void PF_droptofloor ()
        {
        }

        /*
        ===============
        PF_lightstyle

        void(float style, string value) lightstyle
        ===============
        */
        static void PF_lightstyle ()
        {
            int             style;
            string          val;
            server.client_t client;
            int             j;

            style = (int)G_FLOAT(OFS_PARM0);
            val = G_STRING(OFS_PARM1);

            // change the string in sv
            server.sv.lightstyles[style] = val;

            // send message to all clients on this server
            if (server.sv.state != server.server_state_t.ss_active)
                return;

            for (j = 0; j < server.svs.maxclients; j++)
            {
                client = server.svs.clients[j];
                if (client.active || client.spawned)
                {
                    common.MSG_WriteChar(client.message, net.svc_lightstyle);
                    common.MSG_WriteChar(client.message, style);
                    common.MSG_WriteString(client.message, val);
                }
            }
        }

        static void PF_rint ()
        {
            double f;
            f = G_FLOAT(OFS_PARM0);
            if (f > 0)
                pr_globals_write(OFS_RETURN, (int)(f + 0.5));
            else
                pr_globals_write(OFS_RETURN, (int)(f - 0.5));
        }
        static void PF_floor ()
        {
            pr_globals_write(OFS_RETURN, Math.Floor(G_FLOAT(OFS_PARM0)));
        }
        static void PF_ceil ()
        {
            pr_globals_write(OFS_RETURN, Math.Ceiling(G_FLOAT(OFS_PARM0)));
        }

        /*
        =============
        PF_checkbottom
        =============
        */
        static void PF_checkbottom ()
        {
        }

        /*
        =============
        PF_pointcontents
        =============
        */
        static void PF_pointcontents ()
        {
        }

        /*
        =============
        PF_nextent

        entity nextent(entity)
        =============
        */
        static void PF_nextent ()
        {
        }

        /*
        =============
        PF_aim

        Pick a vector for the player to shoot along
        vector aim(entity, missilespeed)
        =============
        */
        static void PF_aim ()
        {
        }

        /*
        ==============
        PF_changeyaw

        This was a major timewaster in progs, so it was converted to C
        ==============
        */
        static void PF_changeyaw ()
        {
        }

        /*
        ===============================================================================

        MESSAGE WRITING

        ===============================================================================
        */

        static void PF_WriteByte ()
        {
        }

        static void PF_WriteChar ()
        {
        }

        static void PF_WriteShort ()
        {
        }

        static void PF_WriteLong ()
        {
        }

        static void PF_WriteAngle ()
        {
        }

        static void PF_WriteCoord ()
        {
        }

        static void PF_WriteString ()
        {
        }
        
        static void PF_WriteEntity ()
        {
        }

        //=============================================================================

        static void PF_makestatic ()
        {
	        edict_t	ent;
	        int		i;
        	
	        ent = G_EDICT(OFS_PARM0);

	        common.MSG_WriteByte (server.sv.signon,net.svc_spawnstatic);

            common.MSG_WriteByte(server.sv.signon, server.SV_ModelIndex(pr_string(ent.v.model)));

            common.MSG_WriteByte(server.sv.signon, (int)ent.v.frame);
            common.MSG_WriteByte(server.sv.signon, (int)ent.v.colormap);
            common.MSG_WriteByte(server.sv.signon, (int)ent.v.skin);
	        for (i=0 ; i<3 ; i++)
	        {
                common.MSG_WriteCoord(server.sv.signon, ent.v.origin[i]);
                common.MSG_WriteAngle(server.sv.signon, ent.v.angles[i]);
	        }

        // throw the entity away now
	        ED_Free (ent);
        }

        //=============================================================================

        /*
        ==============
        PF_setspawnparms
        ==============
        */
        static void PF_setspawnparms ()
        {
        }

        /*
        ==============
        PF_changelevel
        ==============
        */
        static void PF_changelevel ()
        {
        }

        static void PF_Fixme ()
        {
            PR_RunError("unimplemented bulitin");
        }

        static builtin_t[] pr_builtin =
        {
        PF_Fixme,
        PF_makevectors,	// void(entity e)	makevectors 		= #1;
        PF_setorigin,	// void(entity e, vector o) setorigin	= #2;
        PF_setmodel,	// void(entity e, string m) setmodel	= #3;
        PF_setsize,	// void(entity e, vector min, vector max) setsize = #4;
        PF_Fixme,	// void(entity e, vector min, vector max) setabssize = #5;
        PF_break,	// void() break						= #6;
        PF_random,	// float() random						= #7;
        PF_sound,	// void(entity e, float chan, string samp) sound = #8;
        PF_normalize,	// vector(vector v) normalize			= #9;
        PF_error,	// void(string e) error				= #10;
        PF_objerror,	// void(string e) objerror				= #11;
        PF_vlen,	// float(vector v) vlen				= #12;
        PF_vectoyaw,	// float(vector v) vectoyaw		= #13;
        PF_Spawn,	// entity() spawn						= #14;
        PF_Remove,	// void(entity e) remove				= #15;
        PF_traceline,	// float(vector v1, vector v2, float tryents) traceline = #16;
        PF_checkclient,	// entity() clientlist					= #17;
        PF_Find,	// entity(entity start, .string fld, string match) find = #18;
        PF_precache_sound,	// void(string s) precache_sound		= #19;
        PF_precache_model,	// void(string s) precache_model		= #20;
        PF_stuffcmd,	// void(entity client, string s)stuffcmd = #21;
        PF_findradius,	// entity(vector org, float rad) findradius = #22;
        PF_bprint,	// void(string s) bprint				= #23;
        PF_sprint,	// void(entity client, string s) sprint = #24;
        PF_dprint,	// void(string s) dprint				= #25;
        PF_ftos,	// void(string s) ftos				= #26;
        PF_vtos,	// void(string s) vtos				= #27;
        PF_coredump,
        PF_traceon,
        PF_traceoff,
        PF_eprint,	// void(entity e) debug print an entire entity
        PF_walkmove, // float(float yaw, float dist) walkmove
        PF_Fixme, // float(float yaw, float dist) walkmove
        PF_droptofloor,
        PF_lightstyle,
        PF_rint,
        PF_floor,
        PF_ceil,
        PF_Fixme,
        PF_checkbottom,
        PF_pointcontents,
        PF_Fixme,
        PF_fabs,
        PF_aim,
        PF_cvar,
        PF_localcmd,
        PF_nextent,
        PF_particle,
        PF_changeyaw,
        PF_Fixme,
        PF_vectoangles,

        PF_WriteByte,
        PF_WriteChar,
        PF_WriteShort,
        PF_WriteLong,
        PF_WriteCoord,
        PF_WriteAngle,
        PF_WriteString,
        PF_WriteEntity,

        PF_Fixme,
        PF_Fixme,
        PF_Fixme,
        PF_Fixme,
        PF_Fixme,
        PF_Fixme,
        PF_Fixme,

        null/*server.SV_MoveToGoal*/,
        PF_precache_file,
        PF_makestatic,

        PF_changelevel,
        PF_Fixme,

        PF_cvar_set,
        PF_centerprint,

        PF_ambientsound,

        PF_precache_model,
        PF_precache_sound,		// precache_sound2 is different only for qcc
        PF_precache_file,

        PF_setspawnparms
        };

        static builtin_t[] pr_builtins = pr_builtin;
        static int pr_numbuiltins = pr_builtin.Length;
    }
}
