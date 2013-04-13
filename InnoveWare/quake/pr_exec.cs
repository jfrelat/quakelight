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

namespace quake
{
    public partial class prog
    {
        public struct prstack_t
        {
	        public int			s;
	        public dfunction_t  f;
        };

        public const int	MAX_STACK_DEPTH		= 32;
        static prstack_t[]	pr_stack = new prstack_t[MAX_STACK_DEPTH];
        static int			pr_depth;

        public const int	LOCALSTACK_SIZE		= 2048;
        static int[]		localstack = new int[LOCALSTACK_SIZE];
        static int			localstack_used;

        static bool	        pr_trace;
        static dfunction_t	pr_xfunction;
        static int			pr_xstatement;

        static int		    pr_argc;

        static string[] pr_opnames =
        {
        "DONE",

        "MUL_F",
        "MUL_V", 
        "MUL_FV",
        "MUL_VF",
         
        "DIV",

        "ADD_F",
        "ADD_V", 
          
        "SUB_F",
        "SUB_V",

        "EQ_F",
        "EQ_V",
        "EQ_S", 
        "EQ_E",
        "EQ_FNC",
         
        "NE_F",
        "NE_V", 
        "NE_S",
        "NE_E", 
        "NE_FNC",
         
        "LE",
        "GE",
        "LT",
        "GT", 

        "INDIRECT",
        "INDIRECT",
        "INDIRECT", 
        "INDIRECT", 
        "INDIRECT",
        "INDIRECT", 

        "ADDRESS", 

        "STORE_F",
        "STORE_V",
        "STORE_S",
        "STORE_ENT",
        "STORE_FLD",
        "STORE_FNC",

        "STOREP_F",
        "STOREP_V",
        "STOREP_S",
        "STOREP_ENT",
        "STOREP_FLD",
        "STOREP_FNC",

        "RETURN",
          
        "NOT_F",
        "NOT_V",
        "NOT_S", 
        "NOT_ENT", 
        "NOT_FNC", 
          
        "IF",
        "IFNOT",
          
        "CALL0",
        "CALL1",
        "CALL2",
        "CALL3",
        "CALL4",
        "CALL5",
        "CALL6",
        "CALL7",
        "CALL8",
          
        "STATE",
          
        "GOTO", 
          
        "AND",
        "OR", 

        "BITAND",
        "BITOR"
        };

        //=============================================================================

        /*
        =================
        PR_PrintStatement
        =================
        */
        static void PR_PrintStatement(dstatement_t s)
        {
	        int		i;
        	
	        if (s.op < pr_opnames.Length)
	        {
		        console.Con_Printf (pr_opnames[s.op] + " ");
		        i = pr_opnames[s.op].Length;
		        for ( ; i<10 ; i++)
			        console.Con_Printf (" ");
	        }
        		
	        if (s.op == (ushort)opcode_t.OP_IF || s.op == (ushort)opcode_t.OP_IFNOT)
		        console.Con_Printf (PR_GlobalString(s.a) + "branch " + s.b);
	        else if (s.op == (ushort)opcode_t.OP_GOTO)
	        {
		        console.Con_Printf ("branch " + s.a);
	        }
	        else if ( (uint)(s.op - (ushort)opcode_t.OP_STORE_F) < 6)
	        {
		        console.Con_Printf (PR_GlobalString(s.a));
                console.Con_Printf(PR_GlobalStringNoContents(s.b));
	        }
	        else
	        {
		        if (s.a != 0)
                    console.Con_Printf(PR_GlobalString(s.a));
		        if (s.b != 0)
                    console.Con_Printf(PR_GlobalString(s.b));
		        if (s.c != 0)
                    console.Con_Printf(PR_GlobalStringNoContents(s.c));
	        }
	        console.Con_Printf ("\n");
        }

        /*
        ============
        PR_StackTrace
        ============
        */
        static void PR_StackTrace ()
        {
	        dfunction_t	f;
	        int			i;
        	
	        if (pr_depth == 0)
	        {
		        console.Con_Printf ("<NO STACK>\n");
		        return;
	        }
        	
	        pr_stack[pr_depth].f = pr_xfunction;
	        for (i=pr_depth ; i>=0 ; i--)
	        {
		        f = pr_stack[i].f;
        		
		        if (f == null)
		        {
			        console.Con_Printf ("<NO FUNCTION>\n");
		        }
		        else
                    console.Con_Printf(pr_string(f.s_file) + " : " + pr_string(f.s_name) + "\n");
	        }
        }

        /*
        ============
        PR_Profile_f

        ============
        */
        static void PR_Profile_f ()
        {
	        dfunction_t f, best;
	        int			max;
	        int			num;
	        int			i;

            num = 0;
	        do
	        {
		        max = 0;
		        best = null;
		        for (i=0 ; i<progs.numfunctions ; i++)
		        {
			        f = pr_functions[i];
			        if (f.profile > max)
			        {
				        max = f.profile;
				        best = f;
			        }
		        }
		        if (best != null)
		        {
			        if (num < 10)
				        console.Con_Printf (best.profile + " " + pr_string(best.s_name) + "\n");
			        num++;
			        best.profile = 0;
		        }
	        } while (best != null);
        }

        /*
        ============
        PR_RunError

        Aborts the currently executing function
        ============
        */
        static void PR_RunError (string error)
        {
	        string		@string = new string(new char[1024]);

	        @string = error;

	        PR_PrintStatement (pr_statements[pr_xstatement]);
	        PR_StackTrace ();
	        console.Con_Printf (@string + "\n");
        	
	        pr_depth = 0;		// dump the stack so host_error can shutdown functions

	        host.Host_Error ("Program error");
        }

        /*
        ============================================================================
        PR_ExecuteProgram

        The interpretation main loop
        ============================================================================
        */

        /*
        ====================
        PR_EnterFunction

        Returns the new program statement counter
        ====================
        */
        public static int PR_EnterFunction(dfunction_t f)
        {
            int i, j, c, o;

            pr_stack[pr_depth].s = pr_xstatement;
            pr_stack[pr_depth].f = pr_xfunction;
            pr_depth++;
            if (pr_depth >= MAX_STACK_DEPTH)
                PR_RunError("stack overflow");

            // save off any locals that the new function steps on
            c = f.locals;
            if (localstack_used + c > LOCALSTACK_SIZE)
                PR_RunError("PR_ExecuteProgram: locals stack overflow\n");

            for (i = 0; i < c; i++)
                localstack[localstack_used + i] = cast_int(pr_globals_read(f.parm_start + i));
            localstack_used += c;

            // copy parameters
            o = f.parm_start;
            for (i = 0; i < f.numparms; i++)
            {
                for (j = 0; j < f.parm_size[i]; j++)
                {
                    pr_globals_write(o, pr_globals_read(OFS_PARM0 + i * 3 + j));
                    o++;
                }
            }

            pr_xfunction = f;
            return f.first_statement - 1;	// offset the s++
        }

        /*
        ====================
        PR_LeaveFunction
        ====================
        */
        static int PR_LeaveFunction ()
        {
	        int		i, c;

	        if (pr_depth <= 0)
		        sys_linux.Sys_Error ("prog stack underflow");

        // restore locals from the stack
	        c = pr_xfunction.locals;
	        localstack_used -= c;
	        if (localstack_used < 0)
		        PR_RunError ("PR_ExecuteProgram: locals stack underflow\n");

            for (i = 0; i < c; i++)
                pr_globals_write(pr_xfunction.parm_start + i, localstack[localstack_used + i]);

        // up stack
	        pr_depth--;
	        pr_xfunction = pr_stack[pr_depth].f;
	        return pr_stack[pr_depth].s;
        }

        static Object readentvar(entvars_t entvars, int offset)
        {
            if (offset > 104)
                return entvars.variables[offset - 105];
            switch (offset)
            {
                case 0: return entvars.modelindex;
                case 7: return entvars.ltime;
                case 8: return entvars.movetype;
                case 9: return entvars.solid;
                case 10: return entvars.origin[0];
                case 11: return entvars.origin[1];
                case 12: return entvars.origin[2];
                case 16: return entvars.velocity[0];
                case 17: return entvars.velocity[1];
                case 18: return entvars.velocity[2];
                case 19: return entvars.angles[0];
                case 20: return entvars.angles[1];
                case 21: return entvars.angles[2];
                case 28: return entvars.classname;
                case 29: return entvars.model;
                case 33: return entvars.mins[0];
                case 34: return entvars.mins[1];
                case 35: return entvars.mins[2];
                case 36: return entvars.maxs[0];
                case 37: return entvars.maxs[1];
                case 38: return entvars.maxs[2];
                case 39: return entvars.size[0];
                case 40: return entvars.size[1];
                case 41: return entvars.size[2];
                case 48: return entvars.health;
                case 49: return entvars.frags;
                case 50: return entvars.weapon;
                case 52: return entvars.weaponframe;
                case 53: return entvars.currentammo;
                case 54: return entvars.ammo_shells;
                case 55: return entvars.ammo_nails;
                case 56: return entvars.ammo_rockets;
                case 57: return entvars.ammo_cells;
                case 58: return entvars.items;
                case 59: return entvars.takedamage;
                case 61: return entvars.deadflag;
                case 62: return entvars.view_ofs[0];
                case 63: return entvars.view_ofs[1];
                case 64: return entvars.view_ofs[2];
                case 65: return entvars.button0;
                case 66: return entvars.button1;
                case 67: return entvars.button2;
                case 68: return entvars.impulse;
                case 70: return entvars.v_angle[0];
                case 71: return entvars.v_angle[1];
                case 72: return entvars.v_angle[2];
                case 74: return entvars.netname;
                case 76: return entvars.flags;
                case 78: return entvars.team;
                case 79: return entvars.max_health;
                case 81: return entvars.armortype;
                case 82: return entvars.armorvalue;
                case 83: return entvars.waterlevel;
                case 84: return entvars.watertype;
                case 89: return entvars.spawnflags;
                case 90: return entvars.target;
                case 91: return entvars.targetname;
                case 92: return entvars.dmg_take;
                case 93: return entvars.dmg_save;
                case 95: return entvars.owner;
                case 96: return entvars.movedir[0];
                case 97: return entvars.movedir[1];
                case 98: return entvars.movedir[2];
                case 100: return entvars.sounds;
                case 101: return entvars.noise;
            }
            return null;
        }

        static Object readptr(int address)
        {
            entvars_t entvars = server.sv.edicts[address / pr_edict_size].v;
            int offset = ((address % pr_edict_size) - 96) / 4;
            return readentvar(entvars, offset);
        }

        static void writeptr(int address, Object value)
        {
            entvars_t entvars = server.sv.edicts[address / pr_edict_size].v;
            int offset = ((address % pr_edict_size) - 96) / 4;
            if (offset > 104)
            {
                entvars.variables[offset - 105] = value;
                return;
            }
            switch (offset)
            {
                case 0: entvars.modelindex = cast_float(value); break;
                case 8: entvars.movetype = cast_float(value); break;
                case 9: entvars.solid = cast_float(value); break;
                case 10: entvars.origin[0] = cast_float(value); break;
                case 11: entvars.origin[1] = cast_float(value); break;
                case 12: entvars.origin[2] = cast_float(value); break;
                case 19: entvars.angles[0] = cast_float(value); break;
                case 20: entvars.angles[1] = cast_float(value); break;
                case 21: entvars.angles[2] = cast_float(value); break;
                case 28: entvars.classname = cast_int(value); break;
                case 29: entvars.model = cast_int(value); break;
                case 30: entvars.frame = cast_float(value); break;
                case 32: entvars.effects = cast_float(value); break;
                case 39: entvars.size[0] = cast_float(value); break;
                case 40: entvars.size[1] = cast_float(value); break;
                case 41: entvars.size[2] = cast_float(value); break;
                case 42: entvars.touch = cast_int(value); break;
                case 43: entvars.use = cast_int(value); break;
                case 44: entvars.think = cast_int(value); break;
                case 45: entvars.blocked = cast_int(value); break;
                case 46: entvars.nextthink = cast_float(value); break;
                case 48: entvars.health = cast_float(value); break;
                case 50: entvars.weapon = cast_float(value); break;
                case 51: entvars.weaponmodel = cast_int(value); break;
                case 52: entvars.weaponframe = cast_float(value); break;
                case 53: entvars.currentammo = cast_float(value); break;
                case 54: entvars.ammo_shells = cast_float(value); break;
                case 55: entvars.ammo_nails = cast_float(value); break;
                case 56: entvars.ammo_rockets = cast_float(value); break;
                case 57: entvars.ammo_cells = cast_float(value); break;
                case 58: entvars.items = cast_float(value); break;
                case 59: entvars.takedamage = cast_float(value); break;
                case 61: entvars.deadflag = cast_float(value); break;
                case 62: entvars.view_ofs[0] = cast_float(value); break;
                case 63: entvars.view_ofs[1] = cast_float(value); break;
                case 64: entvars.view_ofs[2] = cast_float(value); break;
                case 69: entvars.fixangle = cast_float(value); break;
                case 76: entvars.flags = cast_float(value); break;
                case 79: entvars.max_health = cast_float(value); break;
                case 81: entvars.armortype = cast_float(value); break;
                case 82: entvars.armorvalue = cast_float(value); break;
                case 89: entvars.spawnflags = cast_float(value); break;
                case 95: entvars.owner = cast_int(value); break;
                case 96: entvars.movedir[0] = cast_float(value); break;
                case 97: entvars.movedir[1] = cast_float(value); break;
                case 98: entvars.movedir[2] = cast_float(value); break;
                case 100: entvars.sounds = cast_float(value); break;
                case 101: entvars.noise = cast_int(value); break;
                case 102: entvars.noise1 = cast_int(value); break;
                case 103: entvars.noise2 = cast_int(value); break;
                case 104: entvars.noise3 = cast_int(value); break;
                default: break;
            }
        }

        /*
        ====================
        PR_ExecuteProgram
        ====================
        */
        public static void PR_ExecuteProgram (dfunction_t fnum)
        {
/*	        eval_t	*a, *b, *c;*/
	        int			    s;
	        dstatement_t    st;
            dfunction_t     newf;
	        /*dfunction_t	*f;*/
	        int		        runaway;
	        int		        i;
	        edict_t	        ed;
	        int		        exitdepth;
	        //eval_t	*ptr;

	        /*if (!fnum || fnum >= progs.numfunctions)
	        {
		        if (pr_global_struct.self)
			        ED_Print (PROG_TO_EDICT(pr_global_struct.self));
		        Host_Error ("PR_ExecuteProgram: NULL function");
	        }
        	
	        f = &pr_functions[fnum];*/

	        runaway = 100000;
	        pr_trace = false;

        // make a stack frame
	        exitdepth = pr_depth;

	        s = PR_EnterFunction (fnum);
        	
            while (true)
            {
	            s++;	// next statement

	            st = pr_statements[s];

                if (--runaway == 0)
                    PR_RunError("runaway loop error");

                pr_xfunction.profile++;
                pr_xstatement = s;

                if (pr_trace)
                    PR_PrintStatement(st);

                if (st.c == 7505)
                    st.c = st.c;

                bool eval;
	            switch ((opcode_t)st.op)
	            {
                    case opcode_t.OP_ADD_F:
                        //c->_float = a->_float + b->_float;
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) + cast_float(pr_globals_read(st.b))));
                        break;
                    case opcode_t.OP_ADD_V:
                        /*c->vector[0] = a->vector[0] + b->vector[0];
                        c->vector[1] = a->vector[1] + b->vector[1];
                        c->vector[2] = a->vector[2] + b->vector[2];*/
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) + cast_float(pr_globals_read(st.b))));
                        pr_globals_write(st.c + 1, (double)(cast_float(pr_globals_read(st.a + 1)) + cast_float(pr_globals_read(st.b + 1))));
                        pr_globals_write(st.c + 2, (double)(cast_float(pr_globals_read(st.a + 2)) + cast_float(pr_globals_read(st.b + 2))));
                        break;

                    case opcode_t.OP_SUB_F:
                        //c->_float = a->_float - b->_float;
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) - cast_float(pr_globals_read(st.b))));
                        break;
                    case opcode_t.OP_SUB_V:
                        /*c->vector[0] = a->vector[0] - b->vector[0];
                        c->vector[1] = a->vector[1] - b->vector[1];
                        c->vector[2] = a->vector[2] - b->vector[2];*/
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) - cast_float(pr_globals_read(st.b))));
                        pr_globals_write(st.c + 1, (double)(cast_float(pr_globals_read(st.a + 1)) - cast_float(pr_globals_read(st.b + 1))));
                        pr_globals_write(st.c + 2, (double)(cast_float(pr_globals_read(st.a + 2)) - cast_float(pr_globals_read(st.b + 2))));
                        break;

                    case opcode_t.OP_MUL_F:
                        //c->_float = a->_float * b->_float;
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) * cast_float(pr_globals_read(st.b))));
                        break;
                    case opcode_t.OP_MUL_V:
                        /*c->_float = a->vector[0] * b->vector[0]
                                + a->vector[1] * b->vector[1]
                                + a->vector[2] * b->vector[2];*/
                        double res = (double)(cast_float(pr_globals_read(st.a)) * cast_float(pr_globals_read(st.b))
                                                + cast_float(pr_globals_read(st.a + 1)) * cast_float(pr_globals_read(st.b + 1))
                                                + cast_float(pr_globals_read(st.a + 2)) * cast_float(pr_globals_read(st.b + 2)));
                        pr_globals_write(st.c, res);
                        break;
                    /*case opcode_t.OP_MUL_FV:
                        c->vector[0] = a->_float * b->vector[0];
                        c->vector[1] = a->_float * b->vector[1];
                        c->vector[2] = a->_float * b->vector[2];
                        break;*/
                    case opcode_t.OP_MUL_VF:
                        /*c->vector[0] = b->_float * a->vector[0];
                        c->vector[1] = b->_float * a->vector[1];
                        c->vector[2] = b->_float * a->vector[2];*/
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.b)) * cast_float(pr_globals_read(st.a))));
                        pr_globals_write(st.c + 1, (double)(cast_float(pr_globals_read(st.b + 1)) * cast_float(pr_globals_read(st.a + 1))));
                        pr_globals_write(st.c + 2, (double)(cast_float(pr_globals_read(st.b + 2)) * cast_float(pr_globals_read(st.a + 2))));
                        break;

                    case opcode_t.OP_BITAND:
                        //c->_float = (int)a->_float & (int)b->_float;
                        pr_globals_write(st.c, (double)((int)cast_float(pr_globals_read(st.a)) & (int)cast_float(pr_globals_read(st.b))));
                        break;

                    case opcode_t.OP_BITOR:
                        //c->_float = (int)a->_float | (int)b->_float;
                        pr_globals_write(st.c, (double)((int)cast_float(pr_globals_read(st.a)) | (int)cast_float(pr_globals_read(st.b))));
                        break;

                    case opcode_t.OP_GE:
                        //c->_float = a->_float >= b->_float;
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) >= cast_float(pr_globals_read(st.b)) ? 1 : 0));
                        break;
                    case opcode_t.OP_LE:
                        //c->_float = a->_float <= b->_float;
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) <= cast_float(pr_globals_read(st.b)) ? 1 : 0));
                        break;
                    case opcode_t.OP_GT:
                        //c->_float = a->_float > b->_float;
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) > cast_float(pr_globals_read(st.b)) ? 1 : 0));
                        break;
                    case opcode_t.OP_LT:
                        //c->_float = a->_float < b->_float;
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) < cast_float(pr_globals_read(st.b)) ? 1 : 0));
                        break;
                    case opcode_t.OP_AND:
                        //c->_float = a->_float && b->_float;
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) != 0 && cast_float(pr_globals_read(st.b)) != 0 ? 1 : 0));
                        break;
                    case opcode_t.OP_OR:
                        //c->_float = a->_float || b->_float;
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) != 0 || cast_float(pr_globals_read(st.b)) != 0 ? 1 : 0));
                        break;

	                case opcode_t.OP_NOT_F:
		                //c->_float = !a->_float;
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) == 0 ? 1 : 0));
		                break;
	                case opcode_t.OP_NOT_S:
                        //c->_float = !a->string || !pr_strings[a->string];
                        int astring = cast_int(pr_globals_read(st.a));
                        pr_globals_write(st.c, (double)(((astring == 0) || (pr_string(astring) == null)) ? 1 : 0));
		                break;
	                case opcode_t.OP_NOT_FNC:
		                //c->_float = !a->function;
                        pr_globals_write(st.c, (double)(cast_int(pr_globals_read(st.a)) == 0 ? 1 : 0));
                        break;
	                case opcode_t.OP_NOT_ENT:
		                //c->_float = (PROG_TO_EDICT(a->edict) == sv.edicts);
                        pr_globals_write(st.c, (double)(PROG_TO_EDICT(cast_int(pr_globals_read(st.a))) == server.sv.edicts[0] ? 1 : 0));
                        break;

                    case opcode_t.OP_EQ_F:
                        //c->_float = a->_float == b->_float;
                        pr_globals_write(st.c, (double)(cast_float(pr_globals_read(st.a)) == cast_float(pr_globals_read(st.b)) ? 1 : 0));
                        break;
                    case opcode_t.OP_EQ_V:
                        /*c->_float = (a->vector[0] == b->vector[0]) &&
                                    (a->vector[1] == b->vector[1]) &&
                                    (a->vector[2] == b->vector[2]);*/
                        eval = (cast_float(pr_globals_read(st.a)) == cast_float(pr_globals_read(st.b))) &&
                               (cast_float(pr_globals_read(st.a + 1)) == cast_float(pr_globals_read(st.b + 1))) &&
                               (cast_float(pr_globals_read(st.a + 2)) == cast_float(pr_globals_read(st.b + 2)));
                        pr_globals_write(st.c, (double)(eval ? 1 : 0));
                        break;
	                case opcode_t.OP_EQ_S:
		                //c->_float = !strcmp(pr_strings+a->string,pr_strings+b->string);
                        pr_globals_write(st.c, (double)(pr_string(cast_int(pr_globals_read(st.a))).CompareTo(pr_string(cast_int(pr_globals_read(st.b)))) == 0 ? 1 : 0));
		                break;

                    case opcode_t.OP_NE_V:
                        /*c->_float = (a->vector[0] != b->vector[0]) ||
                                    (a->vector[1] != b->vector[1]) ||
                                    (a->vector[2] != b->vector[2]);*/
                        eval = (cast_float(pr_globals_read(st.a)) != cast_float(pr_globals_read(st.b))) ||
                               (cast_float(pr_globals_read(st.a + 1)) != cast_float(pr_globals_read(st.b + 1))) ||
                               (cast_float(pr_globals_read(st.a + 2)) != cast_float(pr_globals_read(st.b + 2)));
                        pr_globals_write(st.c, (double)(eval ? 1 : 0));
                        break;

                    //==================
                    case opcode_t.OP_STORE_F:
                    case opcode_t.OP_STORE_ENT:
                    case opcode_t.OP_STORE_FLD:		// integers
                    case opcode_t.OP_STORE_S:
                    case opcode_t.OP_STORE_FNC:		// pointers
                        pr_globals_write(st.b, pr_globals_read(st.a));
                        break;
                    case opcode_t.OP_STORE_V:
                        /*b->vector[0] = a->vector[0];
                        b->vector[1] = a->vector[1];
                        b->vector[2] = a->vector[2];*/
                        pr_globals_write(st.b, (double)cast_float(pr_globals_read(st.a)));
                        pr_globals_write(st.b + 1, (double)cast_float(pr_globals_read(st.a + 1)));
                        pr_globals_write(st.b + 2, (double)cast_float(pr_globals_read(st.a + 2)));
                        break;

                    case opcode_t.OP_STOREP_F:
                    case opcode_t.OP_STOREP_ENT:
                    case opcode_t.OP_STOREP_FLD:		// integers
                    case opcode_t.OP_STOREP_S:
                    case opcode_t.OP_STOREP_FNC:		// pointers
                        //ptr = (eval_t*)((byte*)sv.edicts + b->_int);
                        //ptr->_int = a->_int;
                        writeptr(cast_int(pr_globals_read(st.b)), cast_int(pr_globals_read(st.a)));
                        break;
                    case opcode_t.OP_STOREP_V:
                        //ptr = (eval_t*)((byte*)sv.edicts + b->_int);
                        //ptr->vector[0] = a->vector[0];
                        //ptr->vector[1] = a->vector[1];
                        //ptr->vector[2] = a->vector[2];
                        writeptr(cast_int(pr_globals_read(st.b)), cast_int(pr_globals_read(st.a)));
                        writeptr(cast_int(pr_globals_read(st.b)) + 1, cast_int(pr_globals_read(st.a + 1)));
                        writeptr(cast_int(pr_globals_read(st.b)) + 2, cast_int(pr_globals_read(st.a + 2)));
                        break;

	                case opcode_t.OP_ADDRESS:
		                ed = PROG_TO_EDICT(cast_int(pr_globals_read(st.a)));
		                if (ed == server.sv.edicts[0] && server.sv.state == server.server_state_t.ss_active)
			                PR_RunError ("assignment to world entity");
		                //c->_int = (byte *)((int *)&ed->v + b->_int) - (byte *)sv.edicts;
                        pr_globals_write(st.c, ed.index * pr_edict_size + 96 + cast_int(pr_globals_read(st.b)) * 4);
		                break;

	                case opcode_t.OP_LOAD_F:
	                case opcode_t.OP_LOAD_FLD:
	                case opcode_t.OP_LOAD_ENT:
	                case opcode_t.OP_LOAD_S:
	                case opcode_t.OP_LOAD_FNC:
		                ed = PROG_TO_EDICT(cast_int(pr_globals_read(st.a)));
		                //a = (eval_t *)((int *)&ed->v + b->_int);
		                //c->_int = a->_int;
                        pr_globals_write(st.c, cast_int(readptr(ed.index * pr_edict_size + 96 + cast_int(pr_globals_read(st.b)) * 4)));
		                break;

	                case opcode_t.OP_LOAD_V:
		                ed = PROG_TO_EDICT(cast_int(pr_globals_read(st.a)));
		                //a = (eval_t *)((int *)&ed->v + b->_int);
		                //c->vector[0] = a->vector[0];
		                //c->vector[1] = a->vector[1];
		                //c->vector[2] = a->vector[2];
                        pr_globals_write(st.c, (double)cast_float(readptr(ed.index * pr_edict_size + 96 + cast_int(pr_globals_read(st.b)) * 4)));
                        pr_globals_write(st.c + 1, (double)cast_float(readptr(ed.index * pr_edict_size + 96 + (cast_int(pr_globals_read(st.b)) + 1) * 4)));
                        pr_globals_write(st.c + 2, (double)cast_float(readptr(ed.index * pr_edict_size + 96 + (cast_int(pr_globals_read(st.b)) + 2) * 4)));
                        break;
		
                    //==================

                    case opcode_t.OP_IFNOT:
                        if (cast_int(pr_globals_read(st.a)) == 0)
                            s += st.b - 1;	// offset the s++
                        break;

                    case opcode_t.OP_IF:
                        if (cast_int(pr_globals_read(st.a)) != 0)
                            s += st.b - 1;	// offset the s++
                        break;

                    case opcode_t.OP_GOTO:
                        s += st.a - 1;	// offset the s++
                        break;

                    case opcode_t.OP_CALL0:
                    case opcode_t.OP_CALL1:
                    case opcode_t.OP_CALL2:
                    case opcode_t.OP_CALL3:
                    case opcode_t.OP_CALL4:
                    case opcode_t.OP_CALL5:
                    case opcode_t.OP_CALL6:
                    case opcode_t.OP_CALL7:
                    case opcode_t.OP_CALL8:
                        pr_argc = st.op - (int)opcode_t.OP_CALL0;
                        int afunction = cast_int(pr_globals_read(st.a));
                        if (afunction == 0)
                            PR_RunError("NULL function");

                        newf = pr_functions[afunction];

                        if (newf.first_statement < 0)
                        {	// negative statements are built in functions
                            i = -newf.first_statement;
                            if (i >= pr_numbuiltins)
                                PR_RunError("Bad builtin call number");
                            pr_builtins[i]();
                            break;
                        }

                        s = PR_EnterFunction(newf);
                        break;

                    case opcode_t.OP_DONE:
                    case opcode_t.OP_RETURN:
                        pr_globals_write(OFS_RETURN, pr_globals_read(st.a));
                        pr_globals_write(OFS_RETURN + 1, pr_globals_read(st.a + 1));
                        pr_globals_write(OFS_RETURN + 2, pr_globals_read(st.a + 2));

                        s = PR_LeaveFunction();
                        if (pr_depth == exitdepth)
                            return;		// all done
                        break;

	                case opcode_t.OP_STATE:
		                ed = PROG_TO_EDICT(pr_global_struct[0].self);
		                ed.v.nextthink = pr_global_struct[0].time + 0.1;
		                if (cast_float(pr_globals_read(st.a)) != ed.v.frame)
		                {
			                ed.v.frame = cast_float(pr_globals_read(st.a));
		                }
		                ed.v.think = cast_int(pr_globals_read(st.b));
		                break;

                    default:
                        break;
                }
            }
        }
    }
}
