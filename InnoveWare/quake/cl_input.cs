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
// cl.input.c  -- builds an intended movement command to send to the server

// Quake is a trademark of Id Software, Inc., (c) 1996 Id Software, Inc. All
// rights reserved.

namespace quake
{
    public partial class client
    {
        /*
        ===============================================================================

        KEY BUTTONS

        Continuous button event tracking is complicated by the fact that two different
        input sources (say, mouse button 1 and the control key) can both press the
        same button, but the button should only be released when both of the
        pressing key have been released.

        When a key event issues a button command (+forward, +attack, etc), it appends
        its key number as a parameter to the command so it can be matched up with
        the release.

        state bit 0 is the current state of the key
        state bit 1 is edge triggered on the up to down transition
        state bit 2 is edge triggered on the down to up transition

        ===============================================================================
        */

        static kbutton_t in_mlook = new kbutton_t(), in_klook = new kbutton_t();
        static kbutton_t in_left = new kbutton_t(), in_right = new kbutton_t(), in_forward = new kbutton_t(), in_back = new kbutton_t();
        static kbutton_t in_lookup = new kbutton_t(), in_lookdown = new kbutton_t(), in_moveleft = new kbutton_t(), in_moveright = new kbutton_t();
        static kbutton_t in_strafe = new kbutton_t(), in_speed = new kbutton_t(), in_use = new kbutton_t(), in_jump = new kbutton_t(), in_attack = new kbutton_t();
        static kbutton_t in_up = new kbutton_t(), in_down = new kbutton_t();

        static int in_impulse;

        static void KeyDown(kbutton_t b)
        {
	        int		k;
	        string	c;

	        c = cmd.Cmd_Argv(1);
	        if (c.Length > 0)
		        k = int.Parse(c);
	        else
		        k = -1;		// typed manually at the console for continuous down

	        if (k == b.down[0] || k == b.down[1])
		        return;		// repeating key
        	
	        if (b.down[0] == 0)
		        b.down[0] = k;
	        else if (b.down[1] == 0)
		        b.down[1] = k;
	        else
	        {
		        console.Con_Printf ("Three keys down for a button!\n");
		        return;
	        }
        	
	        if ((b.state & 1) != 0)
		        return;		// still down
	        b.state |= 1 + 2;	// down + impulse down
        }

        static void KeyUp(kbutton_t b)
        {
	        int		k;
	        string	c;

            c = cmd.Cmd_Argv(1);
	        if (c.Length > 0)
		        k = int.Parse(c);
	        else
	        { // typed manually at the console, assume for unsticking, so clear all
		        b.down[0] = b.down[1] = 0;
		        b.state = 4;	// impulse up
		        return;
	        }

	        if (b.down[0] == k)
		        b.down[0] = 0;
	        else if (b.down[1] == k)
		        b.down[1] = 0;
	        else
		        return;		// key up without coresponding down (menu pass through)
	        if (b.down[0] != 0 || b.down[1] != 0)
		        return;		// some other key is still holding it down

	        if ((b.state & 1) == 0)
		        return;		// still up (this should not happen)
	        b.state &= ~1;		// now up
	        b.state |= 4; 		// impulse up
        }

        static void IN_KLookDown() { KeyDown(in_klook); }
        static void IN_KLookUp() { KeyUp(in_klook); }
        static void IN_MLookDown() { KeyDown(in_mlook); }
        static void IN_MLookUp()
        {
        KeyUp(in_mlook);
        if ( (in_mlook.state&1) == 0 &&  lookspring.value != 0)
	        view.V_StartPitchDrift();
        }
        static void IN_UpDown() {KeyDown(in_up);}
        static void IN_UpUp() { KeyUp(in_up); }
        static void IN_DownDown() { KeyDown(in_down); }
        static void IN_DownUp() { KeyUp(in_down); }
        static void IN_LeftDown() { KeyDown(in_left); }
        static void IN_LeftUp() { KeyUp(in_left); }
        static void IN_RightDown() { KeyDown(in_right); }
        static void IN_RightUp() { KeyUp(in_right); }
        static void IN_ForwardDown() { KeyDown(in_forward); }
        static void IN_ForwardUp() { KeyUp(in_forward); }
        static void IN_BackDown() { KeyDown(in_back); }
        static void IN_BackUp() { KeyUp(in_back); }
        static void IN_LookupDown() { KeyDown(in_lookup); }
        static void IN_LookupUp() { KeyUp(in_lookup); }
        static void IN_LookdownDown() { KeyDown(in_lookdown); }
        static void IN_LookdownUp() { KeyUp(in_lookdown); }
        static void IN_MoveleftDown() { KeyDown(in_moveleft); }
        static void IN_MoveleftUp() { KeyUp(in_moveleft); }
        static void IN_MoverightDown() { KeyDown(in_moveright); }
        static void IN_MoverightUp() { KeyUp(in_moveright); }

        static void IN_SpeedDown() { KeyDown(in_speed); }
        static void IN_SpeedUp() { KeyUp(in_speed); }
        static void IN_StrafeDown() { KeyDown(in_strafe); }
        static void IN_StrafeUp() { KeyUp(in_strafe); }

        static void IN_AttackDown() { KeyDown(in_attack); }
        static void IN_AttackUp() { KeyUp(in_attack); }

        static void IN_UseDown() { KeyDown(in_use); }
        static void IN_UseUp() { KeyUp(in_use); }
        static void IN_JumpDown() { KeyDown(in_jump); }
        static void IN_JumpUp() { KeyUp(in_jump); }

        static void IN_Impulse() { in_impulse = int.Parse(cmd.Cmd_Argv(1)); }

        /*
        ===============
        CL_KeyState

        Returns 0.25 if a key was pressed and released during the frame,
        0.5 if it was pressed and held
        0 if held then released, and
        1.0 if held for the entire time
        ===============
        */
        static double CL_KeyState (kbutton_t key)
        {
	        double		val;
	        bool	    impulsedown, impulseup, down;
        	
	        impulsedown = (key.state & 2) != 0;
	        impulseup = (key.state & 4) != 0;
	        down = (key.state & 1) != 0;
	        val = 0;
        	
	        if (impulsedown && !impulseup)
		        if (down)
			        val = 0.5;	// pressed and held this frame
		        else
			        val = 0;	//	I_Error ();
	        if (impulseup && !impulsedown)
		        if (down)
			        val = 0;	//	I_Error ();
		        else
			        val = 0;	// released this frame
	        if (!impulsedown && !impulseup)
		        if (down)
			        val = 1.0;	// held the entire frame
		        else
			        val = 0;	// up the entire frame
	        if (impulsedown && impulseup)
		        if (down)
			        val = 0.75;	// released and re-pressed this frame
		        else
			        val = 0.25;	// pressed and released this frame

	        key.state &= 1;		// clear impulses
        	
	        return val;
        }




        //==========================================================================

        static cvar_t cl_upspeed = new cvar_t("cl_upspeed","200");
        static cvar_t cl_forwardspeed = new cvar_t("cl_forwardspeed", "200", true);
        static cvar_t cl_backspeed = new cvar_t("cl_backspeed", "200", true);
        static cvar_t cl_sidespeed = new cvar_t("cl_sidespeed", "350");

        static cvar_t cl_movespeedkey = new cvar_t("cl_movespeedkey", "2.0");

        static cvar_t cl_yawspeed = new cvar_t("cl_yawspeed", "140");
        static cvar_t cl_pitchspeed = new cvar_t("cl_pitchspeed", "150");

        static cvar_t cl_anglespeedkey = new cvar_t("cl_anglespeedkey", "1.5");


        /*
        ================
        CL_AdjustAngles

        Moves the local angle positions
        ================
        */
        static void CL_AdjustAngles ()
        {
	        double	speed;
	        double	up, down;
        	
	        if ((in_speed.state & 1) != 0)
		        speed = host.host_frametime * cl_anglespeedkey.value;
	        else
                speed = host.host_frametime;

	        if ((in_strafe.state & 1) == 0)
	        {
		        cl.viewangles[quakedef.YAW] -= speed*cl_yawspeed.value*CL_KeyState (in_right);
                cl.viewangles[quakedef.YAW] += speed * cl_yawspeed.value * CL_KeyState(in_left);
                cl.viewangles[quakedef.YAW] = mathlib.anglemod(cl.viewangles[quakedef.YAW]);
	        }
	        if ((in_klook.state & 1) != 0)
	        {
		        view.V_StopPitchDrift ();
                cl.viewangles[quakedef.PITCH] -= speed * cl_pitchspeed.value * CL_KeyState(in_forward);
                cl.viewangles[quakedef.PITCH] += speed * cl_pitchspeed.value * CL_KeyState(in_back);
	        }
        	
	        up = CL_KeyState (in_lookup);
	        down = CL_KeyState(in_lookdown);

            cl.viewangles[quakedef.PITCH] -= speed * cl_pitchspeed.value * up;
            cl.viewangles[quakedef.PITCH] += speed * cl_pitchspeed.value * down;

	        if (up != 0 || down != 0)
		        view.V_StopPitchDrift ();
        		
	        if (cl.viewangles[quakedef.PITCH] > 80)
                cl.viewangles[quakedef.PITCH] = 80;
            if (cl.viewangles[quakedef.PITCH] < -70)
                cl.viewangles[quakedef.PITCH] = -70;

            if (cl.viewangles[quakedef.ROLL] > 50)
                cl.viewangles[quakedef.ROLL] = 50;
            if (cl.viewangles[quakedef.ROLL] < -50)
                cl.viewangles[quakedef.ROLL] = -50;
        }

        /*
        ================
        CL_BaseMove

        Send the intended movement message to the server
        ================
        */
        static void CL_BaseMove (usercmd_t cmd)
        {	
	        if (cls.signon != SIGNONS)
		        return;
        			
	        CL_AdjustAngles ();
        	
	        //Q_memset (cmd, 0, sizeof(*cmd));
        	
	        if ((in_strafe.state & 1) != 0)
	        {
		        cmd.sidemove += cl_sidespeed.value * CL_KeyState (in_right);
		        cmd.sidemove -= cl_sidespeed.value * CL_KeyState (in_left);
	        }

	        cmd.sidemove += cl_sidespeed.value * CL_KeyState (in_moveright);
	        cmd.sidemove -= 350/*cl_sidespeed.value*/ * CL_KeyState (in_moveleft);

	        cmd.upmove += cl_upspeed.value * CL_KeyState (in_up);
	        cmd.upmove -= cl_upspeed.value * CL_KeyState (in_down);

	        if ( (in_klook.state & 1) == 0 )
	        {	
		        cmd.forwardmove += cl_forwardspeed.value * CL_KeyState (in_forward);
		        cmd.forwardmove -= cl_backspeed.value * CL_KeyState (in_back);
	        }	

        //
        // adjust for speed key
        //
	        if ((in_speed.state & 1) != 0)
	        {
		        cmd.forwardmove *= cl_movespeedkey.value;
		        cmd.sidemove *= cl_movespeedkey.value;
		        cmd.upmove *= cl_movespeedkey.value;
	        }
        }

        /*
        ==============
        CL_SendMove
        ==============
        */
        static void CL_SendMove (usercmd_t cmd)
        {
	        int		i;
	        int		bits;
	        common.sizebuf_t	buf = new common.sizebuf_t();
	        byte[]	data = new byte[128];
        	
	        buf.maxsize = 128;
	        buf.cursize = 0;
	        buf.data = data;
        	
	        cl.cmd = cmd;

        //
        // send the movement message
        //
            common.MSG_WriteByte (buf, net.clc_move);

            common.MSG_WriteFloat(buf, cl.mtime[0]);	// so server can get ping times

	        for (i=0 ; i<3 ; i++)
                common.MSG_WriteAngle(buf, cl.viewangles[i]);

            common.MSG_WriteShort(buf, (int)cmd.forwardmove);
            common.MSG_WriteShort(buf, (int)cmd.sidemove);
            common.MSG_WriteShort(buf, (int)cmd.upmove);

        //
        // send button bits
        //
	        bits = 0;
        	
	        if (( in_attack.state & 3 ) != 0)
		        bits |= 1;
	        in_attack.state &= ~2;
        	
	        if ((in_jump.state & 3) != 0)
		        bits |= 2;
	        in_jump.state &= ~2;

            common.MSG_WriteByte(buf, bits);

            common.MSG_WriteByte(buf, in_impulse);
	        in_impulse = 0;

        //
        // deliver the message
        //
	        if (cls.demoplayback)
		        return;

        //
        // allways dump the first two message, because it may contain leftover inputs
        // from the last level
        //
	        if (++cl.movemessages <= 2)
		        return;
        	
	        if (net.NET_SendUnreliableMessage (cls.netcon, buf) == -1)
	        {
		        console.Con_Printf ("CL_SendMove: lost server connection\n");
		        CL_Disconnect ();
	        }
        }

        /*
        ============
        CL_InitInput
        ============
        */
        static void CL_InitInput ()
        {
	        cmd.Cmd_AddCommand ("+moveup",IN_UpDown);
            cmd.Cmd_AddCommand("-moveup", IN_UpUp);
            cmd.Cmd_AddCommand("+movedown", IN_DownDown);
            cmd.Cmd_AddCommand("-movedown", IN_DownUp);
            cmd.Cmd_AddCommand("+left", IN_LeftDown);
            cmd.Cmd_AddCommand("-left", IN_LeftUp);
            cmd.Cmd_AddCommand("+right", IN_RightDown);
            cmd.Cmd_AddCommand("-right", IN_RightUp);
            cmd.Cmd_AddCommand("+forward", IN_ForwardDown);
            cmd.Cmd_AddCommand("-forward", IN_ForwardUp);
            cmd.Cmd_AddCommand("+back", IN_BackDown);
            cmd.Cmd_AddCommand("-back", IN_BackUp);
            cmd.Cmd_AddCommand("+lookup", IN_LookupDown);
            cmd.Cmd_AddCommand("-lookup", IN_LookupUp);
            cmd.Cmd_AddCommand("+lookdown", IN_LookdownDown);
            cmd.Cmd_AddCommand("-lookdown", IN_LookdownUp);
            cmd.Cmd_AddCommand("+strafe", IN_StrafeDown);
            cmd.Cmd_AddCommand("-strafe", IN_StrafeUp);
            cmd.Cmd_AddCommand("+moveleft", IN_MoveleftDown);
            cmd.Cmd_AddCommand("-moveleft", IN_MoveleftUp);
            cmd.Cmd_AddCommand("+moveright", IN_MoverightDown);
            cmd.Cmd_AddCommand("-moveright", IN_MoverightUp);
            cmd.Cmd_AddCommand("+speed", IN_SpeedDown);
            cmd.Cmd_AddCommand("-speed", IN_SpeedUp);
            cmd.Cmd_AddCommand("+attack", IN_AttackDown);
            cmd.Cmd_AddCommand("-attack", IN_AttackUp);
            cmd.Cmd_AddCommand("+use", IN_UseDown);
            cmd.Cmd_AddCommand("-use", IN_UseUp);
            cmd.Cmd_AddCommand("+jump", IN_JumpDown);
            cmd.Cmd_AddCommand("-jump", IN_JumpUp);
            cmd.Cmd_AddCommand("impulse", IN_Impulse);
            cmd.Cmd_AddCommand("+klook", IN_KLookDown);
            cmd.Cmd_AddCommand("-klook", IN_KLookUp);
            cmd.Cmd_AddCommand("+mlook", IN_MLookDown);
            cmd.Cmd_AddCommand("-mlook", IN_MLookUp);
        }
    }
}