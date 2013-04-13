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
    public partial class client
    {
        /*
        ==============================================================================

        DEMO CODE

        When a demo is playing back, all NET_SendMessages are skipped, and
        NET_GetMessages are read from the demo file.

        Whenever cl.time gets past the last received message, another message is
        read from the demo file.
        ==============================================================================
        */

        /*
        ==============
        CL_StopPlayback

        Called when a demo file runs out, or the user starts a game
        ==============
        */
        public static void CL_StopPlayback ()
        {
	        if (!cls.demoplayback)
		        return;

	        Helper.helper.fclose (cls.demofile);
	        cls.demoplayback = false;
	        cls.demofile = null;
	        cls.state = cactive_t.ca_disconnected;

	        if (cls.timedemo)
		        CL_FinishTimeDemo ();
        }

        /*
        ====================
        CL_WriteDemoMessage

        Dumps the current net message, prefixed by the length and view angles
        ====================
        */
        static void CL_WriteDemoMessage ()
        {
        }

        /*
        ====================
        CL_GetMessage

        Handles recording and playback of demos, on top of NET_ code
        ====================
        */
        static int CL_GetMessage ()
        {
            int     r, i;
            double  f;

            if (cls.demoplayback)
            {
                // decide if it is time to grab the next message		
                if (cls.signon == SIGNONS)	// allways grab until fully connected
                {
                    if (cls.timedemo)
                    {
                        if (host.host_framecount == cls.td_lastframe)
                            return 0;		// allready read this frame's message
                        cls.td_lastframe = host.host_framecount;
                        // if this is the second frame, grab the real td_starttime
                        // so the bogus time on the first frame doesn't count
                        if (host.host_framecount == cls.td_startframe + 1)
                            cls.td_starttime = host.realtime;
                    }
                    else if ( /* cl.time > 0 && */ cl.time <= cl.mtime[0])
                    {
                        return 0;		// don't need another message yet
                    }
                }

                // get the next message
                int cursize;
                Helper.helper.fread(out net.net_message.cursize, 4, 1, cls.demofile);
		        mathlib.VectorCopy (cl.mviewangles[0], ref cl.mviewangles[1]);
                for (i = 0; i < 3; i++)
                {
                    r = Helper.helper.fread(out f, 4, 1, cls.demofile);
        		    cl.mviewangles[0][i] = f;
                }

                if (net.net_message.cursize > quakedef.MAX_MSGLEN)
                    sys_linux.Sys_Error("Demo message > MAX_MSGLEN");
                r = Helper.helper.fread(ref net.net_message.data, net.net_message.cursize, 1, cls.demofile);
                if (r != 1)
                {
                    CL_StopPlayback();
                    return 0;
                }

                return 1;
            }

	        while (true)
	        {
		        r = net.NET_GetMessage (cls.netcon);
        		
		        if (r != 1 && r != 2)
			        return r;
        	
	        // discard nop keepalive message
                if (net.net_message.cursize == 1 && net.net_message.data[0] == net.svc_nop)
			        console.Con_Printf ("<-- server to client keepalive\n");
		        else
			        break;
	        }

            if (cls.demorecording)
                CL_WriteDemoMessage();

            return r;
        }
        
        /*
        ====================
        CL_Stop_f

        stop recording a demo
        ====================
        */
        static void CL_Stop_f()
        {
        }

        /*
        ====================
        CL_Record_f

        record <demoname> <map> [cd track]
        ====================
        */
        static void CL_Record_f ()
        {
        }

        /*
        ====================
        CL_PlayDemo_f

        play [demoname]
        ====================
        */
        static void CL_PlayDemo_f()
        {
	        string	name;
	        int c;
	        bool neg = false;

            if (cmd.cmd_source != cmd.cmd_source_t.src_command)
		        return;

            if (cmd.Cmd_Argc() != 2)
	        {
		        console.Con_Printf ("play <demoname> : plays a demo\n");
		        return;
	        }

        //
        // disconnect from server
        //
	        CL_Disconnect ();
        	
        //
        // open the demo file
        //
	        name = cmd.Cmd_Argv(1);
	        common.COM_DefaultExtension (ref name, ".dem");

	        console.Con_Printf ("Playing demo from " + name + ".\n");
            common.COM_FOpenFile(name, ref cls.demofile);
	        if (cls.demofile == null)
	        {
		        console.Con_Printf ("ERROR: couldn't open.\n");
		        cls.demonum = -1;		// stop demo loop
		        return;
	        }

	        cls.demoplayback = true;
	        cls.state = cactive_t.ca_connected;
	        cls.forcetrack = 0;

	        while ((c = Helper.helper.getc(cls.demofile)) != '\n')
		        if (c == '-')
			        neg = true;
		        else
			        cls.forcetrack = cls.forcetrack * 10 + (c - '0');

	        if (neg)
		        cls.forcetrack = -cls.forcetrack;
        // ZOID, fscanf is evil
        //	fscanf (cls.demofile, "%i\n", &cls.forcetrack);
        }

        /*
        ====================
        CL_FinishTimeDemo

        ====================
        */
        static void CL_FinishTimeDemo()
        {
            int     frames;
            double  time;

            cls.timedemo = false;

            // the first frame didn't count
            frames = (host.host_framecount - cls.td_startframe) - 1;
            time = host.realtime - cls.td_starttime;
            if (time == 0)
                time = 1;
            console.Con_Printf(frames + " frames " + time + " seconds " + (frames/time) + " fps\n");
        }

        /*
        ====================
        CL_TimeDemo_f

        timedemo [demoname]
        ====================
        */
        static void CL_TimeDemo_f()
        {
            if (cmd.cmd_source != cmd.cmd_source_t.src_command)
                return;

            if (cmd.Cmd_Argc() != 2)
            {
                console.Con_Printf("timedemo <demoname> : gets demo speeds\n");
                return;
            }

            CL_PlayDemo_f();

            // cls.td_starttime will be grabbed at the second frame of the demo, so
            // all the loading time doesn't get counted

            cls.timedemo = true;
            cls.td_startframe = host.host_framecount;
            cls.td_lastframe = -1;		// get a new message this frame
        }
    }
}
