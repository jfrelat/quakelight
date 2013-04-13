using Helper;
using System;
using System.Reflection;
using System.Collections.Generic;

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
// sv_edict.c -- entity dictionary

namespace quake
{
    public partial class prog
    {
        static dprograms_t              progs;
        public static dfunction_t[]     pr_functions;
        static byte[]                   pr_strings;
        static ddef_t[]                 pr_fielddefs;
        static ddef_t[]                 pr_globaldefs;
        static dstatement_t[]           pr_statements;
        public static globalvars_t[]    pr_global_struct;
        public static int               pr_edict_size;	// in bytes

        public static ushort            pr_crc;

        static cvar_t	nomonsters = new cvar_t("nomonsters", "0");
        static cvar_t	gamecfg = new cvar_t("gamecfg", "0");
        static cvar_t	scratch1 = new cvar_t("scratch1", "0");
        static cvar_t	scratch2 = new cvar_t("scratch2", "0");
        static cvar_t	scratch3 = new cvar_t("scratch3", "0");
        static cvar_t	scratch4 = new cvar_t("scratch4", "0");
        static cvar_t	savedgamecfg = new cvar_t("savedgamecfg", "0", true);
        static cvar_t	saved1 = new cvar_t("saved1", "0", true);
        static cvar_t	saved2 = new cvar_t("saved2", "0", true);
        static cvar_t	saved3 = new cvar_t("saved3", "0", true);
        static cvar_t	saved4 = new cvar_t("saved4", "0", true);

        private static Dictionary<String, int> stringDictionary = new Dictionary<string, int>();
        private static String[] stringPool = new String[1000];
        private static int strings = 0;

        /*
        =================
        ED_ClearEdict

        Sets everything to NULL
        =================
        */
        static void ED_ClearEdict (edict_t e)
        {
            e.v.clear();
	        //memset (&e->v, 0, progs->entityfields * 4);
	        e.free = false;
        }

        /*
        =================
        ED_Alloc

        Either finds a free edict, or allocates a new one.
        Try to avoid reusing an entity that was recently freed, because it
        can cause the client to think the entity morphed into something else
        instead of being removed and recreated, which can cause interpolated
        angles and bad trails.
        =================
        */
        static edict_t ED_Alloc ()
        {
	        int			i;
	        edict_t		e;

	        for ( i=server.svs.maxclients+1 ; i<server.sv.num_edicts ; i++)
	        {
		        e = EDICT_NUM(i);
		        // the first couple seconds of server time can involve a lot of
		        // freeing and allocating, so relax the replacement policy
		        if (e.free && ( e.freetime < 2 || server.sv.time - e.freetime > 0.5 ) )
		        {
			        ED_ClearEdict (e);
			        return e;
		        }
	        }
        	
	        if (i == quakedef.MAX_EDICTS)
		        sys_linux.Sys_Error ("ED_Alloc: no free edicts");
        		
	        server.sv.num_edicts++;
	        e = EDICT_NUM(i);
	        ED_ClearEdict (e);

	        return e;
        }

        /*
        =================
        ED_Free

        Marks the edict as free
        FIXME: walk all entities and NULL out references to this entity
        =================
        */
        static void ED_Free (edict_t ed)
        {
	        //SV_UnlinkEdict (ed);		// unlink from world bsp

	        ed.free = true;
	        ed.v.model = 0;
	        ed.v.takedamage = 0;
	        ed.v.modelindex = 0;
	        ed.v.colormap = 0;
	        ed.v.skin = 0;
	        ed.v.frame = 0;
	        mathlib.VectorCopy (mathlib.vec3_origin, ref ed.v.origin);
            mathlib.VectorCopy (mathlib.vec3_origin, ref ed.v.angles);
	        ed.v.nextthink = -1;
	        ed.v.solid = 0;
        	
	        ed.freetime = server.sv.time;
        }

        /*
        ============
        ED_FindField
        ============
        */
        public static ddef_t ED_FindField(string name)
        {
            ddef_t  def;
            int     i;

            for (i = 0; i < progs.numfielddefs; i++)
            {
                def = pr_fielddefs[i];
                if (pr_string(def.s_name).CompareTo(name) == 0)
                    return def;
            }
            return null;
        }

        /*
        ============
        ED_FindFunction
        ============
        */
        public static dfunction_t ED_FindFunction(string name)
        {
            dfunction_t func;
            int         i;

            for (i = 0; i < progs.numfunctions; i++)
            {
                func = pr_functions[i];
                if (pr_string(func.s_name).CompareTo(name) == 0)
                    return func;
            }
            return null;
        }

        //============================================================================

        /*
        ============
        ED_GlobalAtOfs
        ============
        */
        static ddef_t ED_GlobalAtOfs(int ofs)
        {
            ddef_t  def;
            int     i;

            for (i = 0; i < progs.numglobaldefs; i++)
            {
                def = pr_globaldefs[i];
                if (def.ofs == ofs)
                    return def;
            }
            return null;
        }

        /*
        ============
        ED_FieldAtOfs
        ============
        */
        static ddef_t ED_FieldAtOfs(int ofs)
        {
            ddef_t  def;
            int     i;

            for (i = 0; i < progs.numfielddefs; i++)
            {
                def = pr_fielddefs[i];
                if (def.ofs == ofs)
                    return def;
            }
            return null;
        }

        /*
        ============
        PR_ValueString

        Returns a string describing *data in a type specific manner
        =============
        */
        static string line = new string(new char[256]);
        static string PR_ValueString(int type, Object val)
        {
	        ddef_t		def;
	        dfunction_t f;
        	
	        type &= ~DEF_SAVEGLOBAL;

	        switch ((etype_t)type)
	        {
	        case etype_t.ev_string:
		        line = pr_string(cast_int(val));
		        break;
	        case etype_t.ev_entity:	
		        line = "entity " + NUM_FOR_EDICT(PROG_TO_EDICT(cast_int(val)));
		        break;
	        case etype_t.ev_function:
		        f = pr_functions[cast_int(val)];
		        line = pr_string(f.s_name) + "()";
		        break;
	        case etype_t.ev_field:
		        def = ED_FieldAtOfs ( cast_int(val) );
		        line = "." + pr_string(def.s_name);
		        break;
	        case etype_t.ev_void:
		        line = "void";
		        break;
	        case etype_t.ev_float:
                line = "" + cast_float(val);
		        break;  
	        case etype_t.ev_vector:
		        line = "'" + cast_float(val) + "'";
		        break;
	        case etype_t.ev_pointer:
		        line = "pointer";
		        break;
	        default:
		        line = "bad type " + type;
		        break;
	        }
        	
	        return line;
        }

        /*
        ============
        PR_GlobalString

        Returns a string with a description and the contents of a global,
        padded to 20 field width
        ============
        */
        static string PR_GlobalString(int ofs)
        {
	        string  s;
	        int		i;
	        ddef_t	def;
	        Object  val;
        	
	        val = pr_globals_read(ofs);
	        def = ED_GlobalAtOfs(ofs);
	        if (def == null)
		        line = ofs + "(?]";
	        else
	        {
		        s = PR_ValueString (def.type, val);
		        line = ofs + "(" + pr_string(def.s_name) + ")" + s;
	        }
        	
	        i = line.Length;
	        for ( ; i<20 ; i++)
		        line += " ";
	        line += " ";
        		
	        return line;
        }

        static string PR_GlobalStringNoContents(int ofs)
        {
	        int		i;
	        ddef_t	def;
        	
	        def = ED_GlobalAtOfs(ofs);
	        if (def == null)
		        line = ofs + "(?]";
	        else
		        line = ofs + "(" + pr_string(def.s_name) + ")";
        	
	        i = line.Length;
	        for ( ; i<20 ; i++)
		        line += " ";
	        line += " ";
        		
	        return line;
        }

        /*
        =============
        ED_NewString
        =============
        */
        public static string ED_NewString (string @string)
        {
	        string  @new = "";
	        int		i,l;
        	
	        l = @string.Length;

	        for (i=0 ; i< l ; i++)
	        {
		        if (@string[i] == '\\' && i < l-1)
		        {
			        i++;
			        if (@string[i] == 'n')
				        @new += '\n';
			        else
				        @new += '\\';
		        }
		        else
			        @new += @string[i];
	        }
        	
	        return @new;
        }

        /*
        =============
        ED_ParseEval

        Can parse either fields or globals
        returns false if error
        =============
        */
        public static bool ED_ParseEpair (object @base, ddef_t key, string keyname, string s)
        {
	        int		                    i;
	        /*ddef_t	*def;
	        char	*v, *w;*/
	        FieldInfo	                d;
	        //dfunction_t	*func;
            Object[]                    variables = null;
            double[]                    values = new double[3];
        	
	        //d = (void *)((int *)base + key->ofs);
            d = @base.GetType().GetField(keyname);
            if (d == null)
                variables = ((entvars_t)@base).variables;

	        switch ((etype_t)(key.type & ~DEF_SAVEGLOBAL))
	        {
	        case etype_t.ev_string:
                if (d != null)
                    d.SetValue(@base, getStringIndex(ED_NewString(s)) - 15000);
                else
                    variables[key.ofs - 105] = getStringIndex(ED_NewString(s)) - 15000;
		        break;
        		
	        case etype_t.ev_float:
                if (d != null)
                    d.SetValue(@base, common.Q_atof(s));
                else
                    variables[key.ofs - 105] = common.Q_atof(s);
		        break;
        		
	        case etype_t.ev_vector:
                int w = 0;
                int v = 0;
		        for (i=0 ; i<3 ; i++)
		        {
			        while (v < s.Length && s[v] != ' ')
				        v++;

			        values[i] = common.Q_atof (s.Substring(w, v - w));
			        w = v = v+1;
		        }
                if (d != null)
                    d.SetValue(@base, values);
                else
                {
                    for (i=0; i<3; i++)
                        variables[key.ofs - 105 + i] = values[i];
                }
		        break;
        		
/*	        case ev_entity:
		        *(int *)d = EDICT_TO_PROG(EDICT_NUM(atoi (s)));
		        break;
        		
	        case ev_field:
		        def = ED_FindField (s);
		        if (!def)
		        {
			        Con_Printf ("Can't find field %s\n", s);
			        return false;
		        }
		        *(int *)d = G_INT(def->ofs);
		        break;
        	
	        case ev_function:
		        func = ED_FindFunction (s);
		        if (!func)
		        {
			        Con_Printf ("Can't find function %s\n", s);
			        return false;
		        }
		        *(func_t *)d = func - pr_functions;
		        break;*/
        		
	        default:
                sys_linux.Sys_Error("ERROR");
		        break;
	        }
	        return true;
        }

        /*
        ====================
        ED_ParseEdict

        Parses an edict out of the given string, returning the new position
        ed should be a properly initialized empty edict.
        Used for initial level load and for savegames.
        ====================
        */
        public static void ED_ParseEdict (char[] data, ref int ofs, edict_t ent)
        {
	        ddef_t		key;
	        bool	    anglehack;
	        bool	    init;
	        string		keyname = new string(new char[256]);
	        int			n;

	        init = false;

        // clear it
            if (ent != server.sv.edicts[0])	// hack
                ent.v.clear();
		        //memset (&ent->v, 0, progs->entityfields * 4);

        // go through all the dictionary pairs
	        while (true)
	        {	
	        // parse key
		        common.COM_Parse (data, ref ofs);
		        if (common.com_token[0] == '}')
			        break;
		        if (ofs == -1)
			        sys_linux.Sys_Error ("ED_ParseEntity: EOF without closing brace");
        		
                // anglehack is to allow QuakeEd to write single scalar angles
                // and allow them to be turned into vectors. (FIXME...)
                if (common.com_token.CompareTo("angle") == 0)
                {
	                common.com_token = "angles";
	                anglehack = true;
                }
                else
	                anglehack = false;

                // FIXME: change light to _light to get rid of this hack
                if (common.com_token.CompareTo("light") == 0)
	                common.com_token = "light_lev";	// hack for single light def

		        keyname = common.com_token;

		        // another hack to fix heynames with trailing spaces
                keyname.TrimEnd(new char[] {' '});

	        // parse value	
		        common.COM_Parse (data, ref ofs);
		        if (ofs == -1)
			        sys_linux.Sys_Error ("ED_ParseEntity: EOF without closing brace");

		        if (common.com_token[0] == '}')
			        sys_linux.Sys_Error ("ED_ParseEntity: closing brace without data");

		        init = true;	

        // keynames with a leading underscore are used for utility comments,
        // and are immediately discarded by quake
		        if (keyname[0] == '_')
			        continue;
        		
		        key = ED_FindField (keyname);
		        if (key == null)
		        {
			        console.Con_Printf ("'" + keyname + "' is not a field\n");
			        continue;
		        }

                if (anglehack)
                {
                    string temp = new string(new char[32]);
                    temp = common.com_token;
                    common.com_token = "0 " + temp + " 0";
                }

		        if (!ED_ParseEpair (ent.v, key, keyname, common.com_token))
			        host.Host_Error ("ED_ParseEdict: parse error");
	        }

	        if (!init)
		        ent.free = true;
        }

        /*
        ================
        ED_LoadFromFile

        The entities are directly placed in the array, rather than allocated with
        ED_Alloc, because otherwise an error loading the map would have entity
        number references out of order.

        Creates a server's entity / program execution context by
        parsing textual entity definitions out of an ent file.

        Used for both fresh maps and savegame loads.  A fresh map would also need
        to call ED_CallSpawnFunctions () to let the objects initialize themselves.
        ================
        */
        public static void ED_LoadFromFile(char[] data)
        {
            edict_t     ent;
            int         inhibit;
            dfunction_t func;
            int         ofs = 0;

            ent = null;
            inhibit = 0;
            pr_global_struct[0].time = server.sv.time;

            // parse ents
            while (true)
            {
                // parse the opening brace	
                common.COM_Parse(data, ref ofs);
                if (ofs == -1)
                    break;
                if (common.com_token[0] != '{')
                    sys_linux.Sys_Error("ED_LoadFromFile: found " + common.com_token + " when expecting {");

                if (ent == null)
                    ent = EDICT_NUM(0);
		        else
			        ent = ED_Alloc ();
                if (ent.index == 49)
                    ent.index = ent.index;
		        ED_ParseEdict (data, ref ofs, ent);

        // remove things from different skill levels or deathmatch
		        if (host.deathmatch.value != 0)
		        {
			        if (((int)ent.v.spawnflags & server.SPAWNFLAG_NOT_DEATHMATCH) != 0)
			        {
				        ED_Free (ent);
				        inhibit++;
				        continue;
			        }
		        }
		        else if ((host.current_skill == 0 && ((int)ent.v.spawnflags & server.SPAWNFLAG_NOT_EASY) != 0)
                        || (host.current_skill == 1 && ((int)ent.v.spawnflags & server.SPAWNFLAG_NOT_MEDIUM) != 0)
                        || (host.current_skill >= 2 && ((int)ent.v.spawnflags & server.SPAWNFLAG_NOT_HARD) != 0))
		        {
			        ED_Free (ent);	
			        inhibit++;
			        continue;
		        }

        //
        // immediately call spawn function
        //
		        if (ent.v.classname == 0)
		        {
			        console.Con_Printf ("No classname for:\n");
			        //ED_Print (ent);
			        ED_Free (ent);
			        continue;
		        }

	        // look for the spawn function
                func = ED_FindFunction( pr_string(ent.v.classname) );

		        if (func == null)
		        {
			        console.Con_Printf ("No spawn function for:\n");
			        //ED_Print (ent);
			        ED_Free (ent);
			        continue;
		        }

                if (pr_string(ent.v.classname) == "trigger_teleport")
                    inhibit = inhibit;
		        pr_global_struct[0].self = EDICT_TO_PROG(ent);
		        PR_ExecuteProgram (func);
            }

            console.Con_DPrintf(inhibit + " entities inhibited\n");
        }

        public static string pr_string(int offset)
        {
            if (offset < 0)
                return stringPool[offset + 15000];
            return common.parseString(pr_strings, progs.ofs_strings + offset);
        }

        public static void pr_globals_write(int address, Object value)
        {
            globalvars_t globalvars = pr_global_struct[address * 4 / sizeof_globalvars_t ];
            int offset = address % (sizeof_globalvars_t / 4);
            switch (offset)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 17:
                case 18:
                case 19:
                case 20:
                case 21:
                case 22:
                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                    globalvars.pad[offset] = cast_int(value); break;
                case 28: globalvars.self = cast_int(value); break;
                case 29: globalvars.other = cast_int(value); break;
                case 30: globalvars.world = cast_int(value); break;
                case 31: globalvars.time = cast_float(value); break;
                case 32: globalvars.frametime = cast_float(value); break;
                case 33: globalvars.force_retouch = cast_float(value); break;
                case 34: globalvars.mapname = cast_int(value); break;
                case 35: globalvars.deathmatch = cast_float(value); break;
                case 36: globalvars.coop = cast_float(value); break;
                case 37: globalvars.teamplay = cast_float(value); break;
                case 38: globalvars.serverflags = cast_float(value); break;
                case 40: globalvars.total_monsters = cast_float(value); break;
                case 41: globalvars.found_secrets = cast_float(value); break;
                case 42: globalvars.killed_monsters = cast_float(value); break;
                case 43: globalvars.parm1 = cast_float(value); break;
                case 44: globalvars.parm2 = cast_float(value); break;
                case 45: globalvars.parm3 = cast_float(value); break;
                case 46: globalvars.parm4 = cast_float(value); break;
                case 47: globalvars.parm5 = cast_float(value); break;
                case 48: globalvars.parm6 = cast_float(value); break;
                case 49: globalvars.parm7 = cast_float(value); break;
                case 50: globalvars.parm8 = cast_float(value); break;
                case 51: globalvars.parm9 = cast_float(value); break;
                case 52: globalvars.parm10 = cast_float(value); break;
                case 53: globalvars.parm11 = cast_float(value); break;
                case 54: globalvars.parm12 = cast_float(value); break;
                case 55: globalvars.parm13 = cast_float(value); break;
                case 56: globalvars.parm14 = cast_float(value); break;
                case 57: globalvars.parm15 = cast_float(value); break;
                case 58: globalvars.parm16 = cast_float(value); break;
                case 59: globalvars.v_forward[0] = cast_float(value); break;
                case 60: globalvars.v_forward[1] = cast_float(value); break;
                case 61: globalvars.v_forward[2] = cast_float(value); break;
                case 62: globalvars.v_up[0] = cast_float(value); break;
                case 63: globalvars.v_up[1] = cast_float(value); break;
                case 64: globalvars.v_up[2] = cast_float(value); break;
                case 65: globalvars.v_right[0] = cast_float(value); break;
                case 66: globalvars.v_right[1] = cast_float(value); break;
                case 67: globalvars.v_right[2] = cast_float(value); break;
                case 68: globalvars.trace_allsolid = cast_float(value); break;
                case 69: globalvars.trace_startsolid = cast_float(value); break;
                case 70: globalvars.trace_fraction = cast_float(value); break;
                case 71: globalvars.trace_endpos[0] = cast_float(value); break;
                case 72: globalvars.trace_endpos[1] = cast_float(value); break;
                case 73: globalvars.trace_endpos[2] = cast_float(value); break;
                case 74: globalvars.trace_plane_normal[0] = cast_float(value); break;
                case 75: globalvars.trace_plane_normal[1] = cast_float(value); break;
                case 76: globalvars.trace_plane_normal[2] = cast_float(value); break;
                case 77: globalvars.trace_plane_dist = cast_float(value); break;
                case 78: globalvars.trace_ent = cast_int(value); break;
                case 79: globalvars.trace_inopen = cast_float(value); break;
                case 80: globalvars.trace_inwater = cast_float(value); break;
                case 81: globalvars.msg_entity = cast_int(value); break;
                case 82: globalvars.main = cast_int(value); break;
                case 83: globalvars.StartFrame = cast_int(value); break;
                case 84: globalvars.PlayerPreThink = cast_int(value); break;
                case 85: globalvars.PlayerPostThink = cast_int(value); break;
                case 86: globalvars.ClientKill = cast_int(value); break;
                case 87: globalvars.ClientConnect = cast_int(value); break;
                case 88: globalvars.PutClientInServer = cast_int(value); break;
                case 89: globalvars.ClientDisconnect = cast_int(value); break;
                case 90: globalvars.SetNewParms = cast_int(value); break;
                case 91: globalvars.SetChangeParms = cast_int(value); break;
                default: break;
            }
        }

        public static Object pr_globals_read(int address)
        {
            globalvars_t globalvars = pr_global_struct[address * 4 / sizeof_globalvars_t];
            int offset = address % (sizeof_globalvars_t / 4);
            switch(offset)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 17:
                case 18:
                case 19:
                case 20:
                case 21:
                case 22:
                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                    return globalvars.pad[offset];
                case 28: return globalvars.self;
                case 29: return globalvars.other;
                case 30: return globalvars.world;
                case 31: return globalvars.time;
                case 32: return globalvars.frametime;
                case 33: return globalvars.force_retouch;
                case 34: return globalvars.mapname;
                case 35: return globalvars.deathmatch;
                case 36: return globalvars.coop;
                case 37: return globalvars.teamplay;
                case 38: return globalvars.serverflags;
                case 39: return globalvars.total_secrets;
                case 40: return globalvars.total_monsters;
                case 41: return globalvars.found_secrets;
                case 42: return globalvars.killed_monsters;
                case 43: return globalvars.parm1;
                case 44: return globalvars.parm2;
                case 45: return globalvars.parm3;
                case 46: return globalvars.parm4;
                case 47: return globalvars.parm5;
                case 48: return globalvars.parm6;
                case 49: return globalvars.parm7;
                case 50: return globalvars.parm8;
                case 51: return globalvars.parm9;
                case 52: return globalvars.parm10;
                case 53: return globalvars.parm11;
                case 54: return globalvars.parm12;
                case 55: return globalvars.parm13;
                case 56: return globalvars.parm14;
                case 57: return globalvars.parm15;
                case 58: return globalvars.parm16;
                case 59: return globalvars.v_forward[0];
                case 60: return globalvars.v_forward[1];
                case 61: return globalvars.v_forward[2];
                case 62: return globalvars.v_up[0];
                case 63: return globalvars.v_up[1];
                case 64: return globalvars.v_up[2];
                case 65: return globalvars.v_right[0];
                case 66: return globalvars.v_right[1];
                case 67: return globalvars.v_right[2];
                case 68: return globalvars.trace_allsolid;
                case 69: return globalvars.trace_startsolid;
                case 70: return globalvars.trace_fraction;
                case 71: return globalvars.trace_endpos[0];
                case 72: return globalvars.trace_endpos[1];
                case 73: return globalvars.trace_endpos[2];
                case 74: return globalvars.trace_plane_normal[0];
                case 75: return globalvars.trace_plane_normal[1];
                case 76: return globalvars.trace_plane_normal[2];
                case 77: return globalvars.trace_plane_dist;
                case 78: return globalvars.trace_ent;
                case 79: return globalvars.trace_inopen;
                case 80: return globalvars.trace_inwater;
                case 81: return globalvars.msg_entity;
                case 82: return globalvars.main;
                case 83: return globalvars.StartFrame;
                case 84: return globalvars.PlayerPreThink;
                case 85: return globalvars.PlayerPostThink;
                case 86: return globalvars.ClientKill;
                case 87: return globalvars.ClientConnect;
                case 88: return globalvars.PutClientInServer;
                case 89: return globalvars.ClientDisconnect;
                case 90: return globalvars.SetNewParms;
                case 91: return globalvars.SetChangeParms;
            }
            return null;
        }

        public static float cast_float(Object value)
        {
            if (value == null)
                return 0;
            if (value.GetType() == typeof(int))
                return BitConverter.ToSingle(BitConverter.GetBytes((int)value), 0);
            else
                return (float)(double)value;
        }

        public static int cast_int(Object value)
        {
            if (value == null)
                return 0;
            if (value.GetType() == typeof(Double))
                return BitConverter.ToInt32(BitConverter.GetBytes((float)(double)value), 0);
            else
                return (int)value;
        }

        public static int getStringIndex(String str)
        {
            int index;
            if (stringDictionary.TryGetValue(str, out index))
                return index;
            stringDictionary.Add(str, strings);
            stringPool[strings] = str;
            return strings++;
        }


        /*
        ===============
        PR_LoadProgs
        ===============
        */
        public static void PR_LoadProgs ()
        {
	        int		            i;
            byte[]              buf;
            int                 kk;
            helper.ByteBuffer   bbuf;

	        crc.CRC_Init (ref pr_crc);

	        buf = common.COM_LoadHunkFile ("progs.dat");
            progs = (dprograms_t)buf;
	        if (progs == null)
		        sys_linux.Sys_Error ("PR_LoadProgs: couldn't load progs.dat");
	        console.Con_DPrintf ("Programs occupy " + (common.com_filesize/1024) + "K.\n");

	        for (i=0 ; i<common.com_filesize ; i++)
		        crc.CRC_ProcessByte (ref pr_crc, buf[i]);

            if (progs.version != PROG_VERSION)
                sys_linux.Sys_Error("progs.dat has wrong version number (" + progs.version + " should be " + PROG_VERSION + ")");
            if (progs.crc != PROGHEADER_CRC)
                sys_linux.Sys_Error("progs.dat system vars have been modified, progdefs.h is out of date");

            bbuf = new helper.ByteBuffer(buf);
            //pr_functions = (dfunction_t*)((byte*)progs + progs.ofs_functions);
            bbuf.ofs = progs.ofs_functions;
            pr_functions = new dfunction_t[progs.numfunctions];
            for (kk = 0; kk < progs.numfunctions; kk++)
            {
                pr_functions[kk] = (dfunction_t)bbuf;
                bbuf.ofs += sizeof_dfunction_t;
            }
            //pr_strings = (char*)progs + progs.ofs_strings;
            pr_strings = buf;
            //pr_globaldefs = (ddef_t*)((byte*)progs + progs.ofs_globaldefs);
            bbuf.ofs = progs.ofs_globaldefs;
            pr_globaldefs = new ddef_t[progs.numglobaldefs];
            for (kk = 0; kk < progs.numglobaldefs; kk++)
            {
                pr_globaldefs[kk] = (ddef_t)bbuf;
                bbuf.ofs += sizeof_ddef_t;
            }
            //pr_fielddefs = (ddef_t*)((byte*)progs + progs.ofs_fielddefs);
            bbuf.ofs = progs.ofs_fielddefs;
            pr_fielddefs = new ddef_t[progs.numfielddefs];
            for (kk = 0; kk < progs.numfielddefs; kk++)
            {
                pr_fielddefs[kk] = (ddef_t)bbuf;
                bbuf.ofs += sizeof_ddef_t;
            }
            //pr_statements = (dstatement_t*)((byte*)progs + progs.ofs_statements);
            bbuf.ofs = progs.ofs_statements;
            pr_statements = new dstatement_t[progs.numstatements];
            for (kk = 0; kk < progs.numstatements; kk++)
            {
                pr_statements[kk] = (dstatement_t)bbuf;
                bbuf.ofs += sizeof_dstatement_t;
            }

            //pr_global_struct = (globalvars_t*)((byte*)progs + progs.ofs_globals);
            bbuf.ofs = progs.ofs_globals;
            pr_global_struct = new globalvars_t[progs.numglobals * 4 / 368];
            for (kk = 0; kk < progs.numglobals * 4 / 368; kk++)
            {
                pr_global_struct[kk] = (globalvars_t)bbuf;
                bbuf.ofs += sizeof_globalvars_t;
            }

            pr_edict_size = progs.entityfields * 4 + sizeof_edict_t - sizeof_entvars_t;
        }

        /*
        ===============
        PR_Init
        ===============
        */
        public static void PR_Init ()
        {
	        //Cmd_AddCommand ("edict", ED_PrintEdict_f);
            //Cmd_AddCommand ("edicts", ED_PrintEdicts);
            //Cmd_AddCommand ("edictcount", ED_Count);
            //Cmd_AddCommand ("profile", PR_Profile_f);
            cvar_t.Cvar_RegisterVariable(nomonsters);
            cvar_t.Cvar_RegisterVariable(gamecfg);
            cvar_t.Cvar_RegisterVariable(scratch1);
            cvar_t.Cvar_RegisterVariable(scratch2);
            cvar_t.Cvar_RegisterVariable(scratch3);
            cvar_t.Cvar_RegisterVariable(scratch4);
            cvar_t.Cvar_RegisterVariable(savedgamecfg);
            cvar_t.Cvar_RegisterVariable(saved1);
            cvar_t.Cvar_RegisterVariable(saved2);
            cvar_t.Cvar_RegisterVariable(saved3);
	        cvar_t.Cvar_RegisterVariable (saved4);
        }

        public static edict_t EDICT_NUM(int n)
        {
            if (n < 0 || n >= server.sv.max_edicts)
                sys_linux.Sys_Error("EDICT_NUM: bad number " + n);
            return (edict_t)server.sv.edicts[n];
        }

        public static int NUM_FOR_EDICT(edict_t e)
        {
            int b;

            b = e.index;

            if (b < 0 || b >= server.sv.num_edicts)
                sys_linux.Sys_Error("NUM_FOR_EDICT: bad pointer");
            return b;
        }
    }
}