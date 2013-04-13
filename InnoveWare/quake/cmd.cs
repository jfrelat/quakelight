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

// cmd.h -- Command buffer and command execution

//===========================================================================

/*

Any number of commands can be added in a frame, from several different sources.
Most commands come from either keybindings or console line input, but remote
servers can also send across commands and entire text files can be execed.

The + command line options are also added to the command buffer.

The game starts with a Cbuf_AddText ("exec quake.rc\n"); Cbuf_Execute ();

*/
// cmd.c -- Quake script command processing module

namespace quake
{
    public sealed class cmd
    {
        //===========================================================================

        /*

        Command execution takes a null terminated string, breaks it into tokens,
        then searches for a command or variable that matches the first token.

        Commands can come from three sources, but the handler functions may choose
        to dissallow the action or forward it to a remote server if the source is
        not apropriate.

        */

        public delegate void xcommand_t();

        public enum cmd_source_t
        {
	        src_client,		// came in over a net connection as a clc_stringcmd
					        // host_client will be valid during this state.
	        src_command		// from the command buffer
        };

        const int	MAX_ALIAS_NAME = 32;

        class cmdalias_t
        {
	        public cmdalias_t	next;
            public string       name = new string(new char[MAX_ALIAS_NAME]);
            public string       value;
        };

        static cmdalias_t	    cmd_alias = new cmdalias_t();

        int             trashtest;
        int[]           trashspot;

        static bool	        cmd_wait;

        //=============================================================================

        /*
        ============
        Cmd_Wait_f

        Causes execution of the remainder of the command buffer to be delayed until
        next frame.  This allows commands like:
        bind g "impulse 5 ; +attack ; wait ; -attack ; impulse 2"
        ============
        */
        static void Cmd_Wait_f ()
        {
	        cmd_wait = true;
        }

        /*
        =============================================================================

						        COMMAND BUFFER

        =============================================================================
        */

        static common.sizebuf_t	cmd_text = new common.sizebuf_t();

        /*
        ============
        Cbuf_Init
        ============
        */
        public static void Cbuf_Init ()
        {
            common.SZ_Alloc(cmd_text, 8192);		// space for commands and script files
        }

        /*
        ============
        Cbuf_AddText

        Adds command text at the end of the buffer
        ============
        */
        public static void Cbuf_AddText (string text)
        {
            int l;

            l = text.Length;

            if (cmd_text.cursize + l >= cmd_text.maxsize)
            {
                console.Con_Printf("Cbuf_AddText: overflow\n");
                return;
            }

            byte[] buf = new byte[text.Length];
            for (int kk = 0; kk < text.Length; kk++)
                buf[kk] = (byte)text[kk];
            common.SZ_Write(cmd_text, buf, text.Length);
        }

        /*
        ============
        Cbuf_InsertText

        Adds command text immediately after the current command
        Adds a \n to the text
        FIXME: actually change the command buffer to do less copying
        ============
        */
        public static void Cbuf_InsertText (string text)
        {
            byte[]  temp;
            int     templen;

            // copy off any commands still remaining in the exec buffer
            templen = cmd_text.cursize;
            if (templen != 0)
            {
                temp = new byte[templen];
                Buffer.BlockCopy(cmd_text.data, 0, temp, 0, templen);
                common.SZ_Clear(cmd_text);
            }
            else
                temp = null;	// shut up compiler

            // add the entire text of the file
            Cbuf_AddText(text);

            // add the copied off data
            if (templen != 0)
            {
                common.SZ_Write(cmd_text, temp, templen);
                temp = null;
            }
        }

        /*
        ============
        Cbuf_Execute
        ============
        */
        public static void Cbuf_Execute ()
        {
	        int		i;
	        byte[]	text;
	        char[]	line = new char[1024];
	        int		quotes;
        	
	        while (cmd_text.cursize != 0)
	        {
        // find a \n or ; line break
		        text = cmd_text.data;

		        quotes = 0;
		        for (i=0 ; i< cmd_text.cursize ; i++)
		        {
			        if (text[i] == '"')
				        quotes++;
			        if ( (quotes&1) == 0 &&  text[i] == ';')
				        break;	// don't break if inside a quoted string
			        if (text[i] == '\n')
				        break;
		        }

                for (int kk = 0; kk < i; kk++)
                    line[kk] = (char)text[kk];
                line[i] = (char)0;
        		
        // delete the text from the command buffer and move remaining commands down
        // this is necessary because commands (exec, alias) can insert data at the
        // beginning of the text buffer

		        if (i == cmd_text.cursize)
			        cmd_text.cursize = 0;
		        else
		        {
			        i++;
			        cmd_text.cursize -= i;
                    for (int kk = 0; kk < cmd_text.cursize; kk++)
                        text[kk] = text[kk + i];
		        }

        // execute the command line
                Cmd_ExecuteString(line, cmd_source_t.src_command);
        		
		        if (cmd_wait)
		        {	// skip out while text still remains in buffer, leaving it
			        // for next frame
			        cmd_wait = false;
			        break;
		        }
	        }
        }

        /*
        ==============================================================================

						        SCRIPT COMMANDS

        ==============================================================================
        */

        /*
        ===============
        Cmd_StuffCmds_f

        Adds command line parameters as script statements
        Commands lead with a +, and continue until a - or another +
        quake +prog jctest.qp +cmd amlev1
        quake -nosound +cmd amlev1
        ===============
        */
        static void Cmd_StuffCmds_f ()
        {
	        int		i, j;
	        int		s;
	        string	text, build;
        		
	        if (Cmd_Argc () != 1)
	        {
		        console.Con_Printf ("stuffcmds : execute command line parameters\n");
		        return;
	        }

        // build the combined string to parse from
	        s = 0;
	        for (i=1 ; i<common.com_argc ; i++)
	        {
                if (common.com_argv[i] == null)
			        continue;		// NEXTSTEP nulls out -NXHost
                s += common.com_argv[i].Length + 1;
	        }
	        if (s == 0)
		        return;
        		
	        text = "";
            for (i = 1; i < common.com_argc; i++)
	        {
                if (common.com_argv[i] == null)
			        continue;		// NEXTSTEP nulls out -NXHost
                text += common.com_argv[i];
                if (i != common.com_argc - 1)
			        text += " ";
	        }
        	
        // pull out the commands
	        build = "";
        	
	        for (i=0 ; i<s-1 ; i++)
	        {
		        if (text[i] == '+')
		        {
			        i++;

			        for (j=i ; (text[j] != '+') && (text[j] != '-') && (text[j] != 0) ; j++)
				        ;

                    build += text.Substring(i, j + 1 - i);
			        build += "\n";
			        i = j-1;
		        }
	        }
        	
	        if (build.Length != 0)
		        Cbuf_InsertText (build);
        	
	        text = null;
	        build = null;
        }

        /*
        ===============
        Cmd_Exec_f
        ===============
        */
        static void Cmd_Exec_f ()
        {
            string f;
            int mark;

            if (Cmd_Argc() != 2)
            {
                console.Con_Printf("exec <filename> : execute a script file\n");
                return;
            }

            byte[] buf = common.COM_LoadHunkFile(Cmd_Argv(1));
            if (buf == null)
            {
                console.Con_Printf("couldn't exec " + Cmd_Argv(1) + "\n");
                return;
            }
            f = "";
            for (int kk = 0; kk < buf.Length; kk++)
                f += (char)buf[kk];
            console.Con_Printf("execing " + Cmd_Argv(1) + "\n");

            Cbuf_InsertText(f);
        }

        /*
        ===============
        Cmd_Echo_f

        Just prints the rest of the line to the console
        ===============
        */
        static void Cmd_Echo_f ()
        {
	        int		i;
        	
	        for (i=1 ; i<Cmd_Argc() ; i++)
		        console.Con_Printf (Cmd_Argv(i) + " ");
            console.Con_Printf("\n");
        }

        /*
        ===============
        Cmd_Alias_f

        Creates a new command that executes a command string (possibly ; seperated)
        ===============
        */

        static void Cmd_Alias_f ()
        {
	        cmdalias_t	a;
	        string		cmd;
	        int			i, c;
	        string		s;

	        if (Cmd_Argc() == 1)
	        {
		        console.Con_Printf ("Current alias commands:\n");
		        for (a = cmd_alias ; a != null ; a=a.next)
                    console.Con_Printf(a.name + " : " + a.value + "\n");
		        return;
	        }

	        s = Cmd_Argv(1);
	        if (s.Length >= MAX_ALIAS_NAME)
	        {
                console.Con_Printf("Alias name is too long\n");
		        return;
	        }

	        // if the alias allready exists, reuse it
	        for (a = cmd_alias ; a != null ; a=a.next)
	        {
		        if (s.CompareTo(a.name) == 0)
		        {
                    a.value = null;
			        break;
		        }
	        }

	        if (a == null)
	        {
		        a = new cmdalias_t();
		        a.next = cmd_alias;
		        cmd_alias = a;
	        }
            a.name = s;

        // copy the rest of the command line
	        cmd = "";		// start out with a null string
	        c = Cmd_Argc();
	        for (i=2 ; i< c ; i++)
	        {
		        cmd += Cmd_Argv(i);
		        if (i != c)
			        cmd += " ";
	        }
	        cmd += "\n";
        	
	        a.value = cmd;
        }

        /*
        =============================================================================

					        COMMAND EXECUTION

        =============================================================================
        */

        class cmd_function_t
        {
	        public cmd_function_t   next;
            public string           name;
            public xcommand_t       function;
        };

        const int	MAX_ARGS = 80;

        static	int			    cmd_argc;
        static	string[]	    cmd_argv = new string[MAX_ARGS];
        static	string		    cmd_null_string = "";
        static	string		    cmd_args = null;

        public static cmd_source_t	    cmd_source;


        static	cmd_function_t	cmd_functions;		// possible commands to execute

        /*
        ============
        Cmd_Init
        ============
        */
        public static void Cmd_Init ()
        {
        //
        // register our commands
        //
	        Cmd_AddCommand ("stuffcmds",Cmd_StuffCmds_f);
	        Cmd_AddCommand ("exec",Cmd_Exec_f);
	        Cmd_AddCommand ("echo",Cmd_Echo_f);
	        Cmd_AddCommand ("alias",Cmd_Alias_f);
	        Cmd_AddCommand ("cmd", Cmd_ForwardToServer);
	        Cmd_AddCommand ("wait", Cmd_Wait_f);
        }

        /*
        ============
        Cmd_Argc
        ============
        */
        public static int Cmd_Argc()
        {
	        return cmd_argc;
        }

        /*
        ============
        Cmd_Argv
        ============
        */
        public static string Cmd_Argv (int arg)
        {
	        if ( (uint)arg >= cmd_argc )
		        return cmd_null_string;
            return cmd_argv[arg];
        }

        /*
        ============
        Cmd_Args
        ============
        */
        public static string Cmd_Args ()
        {
	        return cmd_args;
        }


        /*
        ============
        Cmd_TokenizeString

        Parses the given string into command line tokens.
        ============
        */
        static void Cmd_TokenizeString (char[] text)
        {
            int i;

            // clear the args from the last string
            for (i = 0; i < cmd_argc; i++)
                cmd_argv[i] = null;

            cmd_argc = 0;
            cmd_args = null;

            int ofs = 0;
            while (true)
            {
                // skip whitespace up to a /n
                while (text[ofs] != 0 && text[ofs] <= ' ' && text[ofs] != '\n')
                {
                    ofs++;
                }

                if (text[ofs] == '\n')
                {	// a newline seperates commands in the buffer
                    ofs++;
                    break;
                }

                if (text[ofs] == 0)
                    return;

                if (cmd_argc == 1)
                {
                    cmd_args = "";
                    for (int kk = 0; ; kk++)
                    {
                        char ch = text[ofs + kk];
                        if (ch == 0)
                            break;
                        cmd_args += ch;
                    }
                }

                common.COM_Parse(text, ref ofs);
                if (ofs == -1)
                    return;

                if (cmd_argc < MAX_ARGS)
                {
                    cmd_argv[cmd_argc] = common.com_token;
                    cmd_argc++;
                }
            }
        }

        /*
        ============
        Cmd_AddCommand
        ============
        */
        public static void	Cmd_AddCommand (string cmd_name, xcommand_t function)
        {
	        cmd_function_t	cmd;
        	
	        if (host.host_initialized)	// because hunk allocation would get stomped
		        sys_linux.Sys_Error ("Cmd_AddCommand after host_initialized");

            // fail if the command is a variable name
            if (cvar_t.Cvar_VariableString(cmd_name).Length != 0)
            {
                console.Con_Printf("Cmd_AddCommand: " + cmd_name + " already defined as a var\n");
                return;
            }

        // fail if the command already exists
	        for (cmd=cmd_functions ; cmd != null ; cmd=cmd.next)
	        {
		        if (cmd_name.CompareTo(cmd.name) == 0)
		        {
			        console.Con_Printf ("Cmd_AddCommand: " + cmd_name + " already defined\n");
			        return;
		        }
	        }

	        cmd = new cmd_function_t();
	        cmd.name = cmd_name;
	        cmd.function = function;
	        cmd.next = cmd_functions;
	        cmd_functions = cmd;
        }

        /*
        ============
        Cmd_Exists
        ============
        */
        public static bool	Cmd_Exists (string cmd_name)
        {
	        cmd_function_t	cmd;

	        for (cmd=cmd_functions ; cmd != null ; cmd=cmd.next)
	        {
		        if (cmd_name.CompareTo(cmd.name) == 0)
			        return true;
	        }

	        return false;
        }
        
        /*
        ============
        Cmd_CompleteCommand
        ============
        */
        public static string Cmd_CompleteCommand (string partial)
        {
            cmd_function_t  cmd;
            int len;

            len = partial.Length;

            if (len == 0)
                return null;

            // check functions
            for (cmd = cmd_functions; cmd != null; cmd = cmd.next)
                if (partial.CompareTo(cmd.name.Substring(0, len)) == 0)
                    return cmd.name;

            return null;
        }

        /*
        ============
        Cmd_ExecuteString

        A complete command line has been parsed, so try to execute it
        FIXME: lookupnoadd the token to speed search?
        ============
        */
        public static void	Cmd_ExecuteString (char[] text, cmd_source_t src)
        {
            cmd_function_t  cmd;
            cmdalias_t      a;

            cmd_source = src;
            Cmd_TokenizeString(text);

            // execute the command line
            if (Cmd_Argc() == 0)
                return;		// no tokens

            // check functions
            for (cmd = cmd_functions; cmd != null; cmd = cmd.next)
            {
                if (cmd_argv[0].CompareTo(cmd.name) == 0)
                {
                    cmd.function();
                    return;
                }
            }

            // check alias
            for (a = cmd_alias; a != null; a = a.next)
            {
                if (cmd_argv[0].CompareTo(a.name) == 0)
                {
                    Cbuf_InsertText(a.value);
                    return;
                }
            }

            // check cvars
            if (!cvar_t.Cvar_Command())
                console.Con_Printf("Unknown command \"" + Cmd_Argv(0) + "\"\n");
        }

        /*
        ===================
        Cmd_ForwardToServer

        Sends the entire command line over to the server
        ===================
        */
        public static void Cmd_ForwardToServer ()
        {
            if (client.cls.state != client.cactive_t.ca_connected)
            {
                console.Con_Printf("Can't \"" + Cmd_Argv(0) + "\", not connected\n");
                return;
            }

            if (client.cls.demoplayback)
                return;		// not really connected

            common.MSG_WriteByte(client.cls.message, net.clc_stringcmd);
            if (Cmd_Argv(0).CompareTo("cmd") != 0)
            {
                common.SZ_Print(client.cls.message, Cmd_Argv(0));
                common.SZ_Print(client.cls.message, " ");
            }
            if (Cmd_Argc() > 1)
                common.SZ_Print(client.cls.message, Cmd_Args());
            else
                common.SZ_Print(client.cls.message, "\n");
        }

        /*
        ================
        Cmd_CheckParm

        Returns the position (1 to argc-1) in the command's argument list
        where the given parameter apears, or 0 if not present
        ================
        */

        int Cmd_CheckParm (string parm)
        {
	        return 0;
        }
    }
}
