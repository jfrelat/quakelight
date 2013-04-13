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
// cvar.h
// cvar.c -- dynamic variable tracking

/*

cvar_t variables are used to hold scalar or string variables that can be changed or displayed at the console or prog code as well as accessed directly
in C code.

it is sufficient to initialize a cvar_t with just the first two fields, or
you can add a ,true flag for variables that you want saved to the configuration
file when the game is quit:

cvar_t	r_draworder = {"r_draworder","1"};
cvar_t	scr_screensize = {"screensize","1",true};

Cvars must be registered before use, or they will have a 0 value instead of the double interpretation of the string.  Generally, all cvar_t declarations should be registered in the apropriate init function before any console commands are executed:
Cvar_RegisterVariable (&host_framerate);


C code usually just references a cvar in place:
if ( r_draworder.value )

It could optionally ask for the value to be looked up for a string name:
if (Cvar_VariableValue ("r_draworder"))

Interpreted prog code can access cvars with the cvar(name) or
cvar_set (name, value) internal functions:
teamplay = cvar("teamplay");
cvar_set ("registered", "1");

The user can access cvars from the console in two ways:
r_draworder			prints the current value
r_draworder 0		sets the current value to 0
Cvars are restricted from having the same names as commands to keep this
interface from being ambiguous.
*/

namespace quake
{
    public class cvar_t
    {
        string name;
        public string _string;
        bool archive;		// set to true to cause it to be saved to vars.rc
        bool server;		// notifies players when changed
        public double value;
        cvar_t next;

        #region Constructors
        public cvar_t(string name, string _string)
            : this(name, _string, false, false)
        { }

        public cvar_t(string name, string _string, bool archive)
            : this(name, _string, archive, false)
        { }

        public cvar_t(string name, string _string, bool archive, bool server)
        {
            this.name = name;
            this._string = _string;
            this.archive = archive;
            this.server = server;
        }
        #endregion

        static cvar_t	cvar_vars;
        static string	cvar_null_string = "";

        /*
        ============
        Cvar_FindVar
        ============
        */
        static cvar_t Cvar_FindVar (string var_name)
        {
	        cvar_t	var;
        	
	        for (var=cvar_vars ; var != null ; var=var.next)
		        if (var_name.CompareTo(var.name) == 0)
			        return var;

	        return null;
        }

        /*
        ============
        Cvar_VariableValue
        ============
        */
        public static double Cvar_VariableValue (string var_name)
        {
	        cvar_t	var;
        	
	        var = Cvar_FindVar (var_name);
	        if (var == null)
		        return 0;
            return double.Parse(var._string);
        }

        /*
        ============
        Cvar_VariableString
        ============
        */
        public static string Cvar_VariableString (string var_name)
        {
	        cvar_t var;
        	
	        var = Cvar_FindVar (var_name);
	        if (var == null)
		        return cvar_null_string;
	        return var._string;
        }

        /*
        ============
        Cvar_CompleteVariable
        ============
        */
        public static string Cvar_CompleteVariable (string partial)
        {
	        cvar_t		cvar;
	        int			len;
        	
	        len = partial.Length;
        	
	        if (len == 0)
		        return null;
        		
        // check functions
	        for (cvar=cvar_vars ; cvar != null ; cvar=cvar.next)
                if (partial.CompareTo(cvar.name.Substring(0, len)) == 0)
			        return cvar.name;

	        return null;
        }


        /*
        ============
        Cvar_Set
        ============
        */
        public static void Cvar_Set (string var_name, string value)
        {
	        cvar_t	    var;
	        bool        changed;
        	
	        var = Cvar_FindVar (var_name);
	        if (var == null)
	        {	// there is an error in C code if this happens
		        console.Con_Printf ("Cvar_Set: variable " + var_name + " not found\n");
		        return;
	        }

	        changed = (var._string.CompareTo(value) != 0);
        	
	        var._string = null;	// free the old value string
        	
	        var._string = value;
	        var.value = common.Q_atof(var._string);
	        if (var.server && changed)
	        {
	        }
        }

        /*
        ============
        Cvar_SetValue
        ============
        */
        public static void Cvar_SetValue (string var_name, double value)
        {
	        string	val;

            val = System.Convert.ToString(value);
            val = val.Replace(',', '.');
	        Cvar_Set (var_name, val);
        }
        
        /*
        ============
        Cvar_RegisterVariable

        Adds a freestanding variable to the variable list.
        ============
        */
        public static void Cvar_RegisterVariable (cvar_t variable)
        {
	        string	oldstr;
        	
        // first check to see if it has allready been defined
	        if (Cvar_FindVar (variable.name) != null)
	        {
		        console.Con_Printf ("Can't register variable " + variable.name + ", allready defined\n");
		        return;
	        }
        	
        // check for overlap with a command
	        if (cmd.Cmd_Exists (variable.name))
	        {
		        console.Con_Printf ("Cvar_RegisterVariable: " + variable.name + " is a command\n");
		        return;
	        }
        		
        // copy the value off, because future sets will Z_Free it
	        oldstr = variable._string;
	        variable._string = oldstr;
            variable.value = common.Q_atof(variable._string);
        	
        // link the variable in
	        variable.next = cvar_vars;
	        cvar_vars = variable;
        }

        /*
        ============
        Cvar_Command

        Handles variable inspection and changing from the console
        ============
        */
        public static bool	Cvar_Command ()
        {
	        cvar_t			v;

        // check variables
	        v = Cvar_FindVar (cmd.Cmd_Argv(0));
	        if (v == null)
		        return false;
        		
        // perform a variable print or set
	        if (cmd.Cmd_Argc() == 1)
	        {
		        console.Con_Printf ("\"" + v.name + "\" is \"" + v._string + "\"\n");
		        return true;
	        }

	        Cvar_Set (v.name, cmd.Cmd_Argv(1));
	        return true;
        }
   }
}