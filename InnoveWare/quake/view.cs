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
// view.c -- player eye positioning

namespace quake
{
    public sealed class view
    {
        /*

        The view is allowed to move slightly from it's true position for bobbing,
        but if it exceeds 8 pixels linear distance (spherical, not box), the list of
        entities sent from the server may not include everything in the pvs, especially
        when crossing a water boudnary.

        */

        public static cvar_t	lcd_x = new cvar_t("lcd_x","0");
        static cvar_t   lcd_yaw = new cvar_t("lcd_yaw", "0");

        static cvar_t   scr_ofsx = new cvar_t("scr_ofsx", "0", false);
        static cvar_t   scr_ofsy = new cvar_t("scr_ofsy", "0", false);
        static cvar_t   scr_ofsz = new cvar_t("scr_ofsz", "0", false);

        static cvar_t   cl_rollspeed = new cvar_t("cl_rollspeed", "200");
        static cvar_t   cl_rollangle = new cvar_t("cl_rollangle", "2.0");

        static cvar_t   cl_bob = new cvar_t("cl_bob", "0.02", false);
        static cvar_t   cl_bobcycle = new cvar_t("cl_bobcycle", "0.6", false);
        static cvar_t   cl_bobup = new cvar_t("cl_bobup", "0.5", false);

        static cvar_t   v_kicktime = new cvar_t("v_kicktime", "0.5", false);
        static cvar_t   v_kickroll = new cvar_t("v_kickroll", "0.6", false);
        static cvar_t   v_kickpitch = new cvar_t("v_kickpitch", "0.6", false);

        static cvar_t   v_iyaw_cycle = new cvar_t("v_iyaw_cycle", "2", false);
        static cvar_t   v_iroll_cycle = new cvar_t("v_iroll_cycle", "0.5", false);
        static cvar_t   v_ipitch_cycle = new cvar_t("v_ipitch_cycle", "1", false);
        static cvar_t   v_iyaw_level = new cvar_t("v_iyaw_level", "0.3", false);
        static cvar_t   v_iroll_level = new cvar_t("v_iroll_level", "0.1", false);
        static cvar_t   v_ipitch_level = new cvar_t("v_ipitch_level", "0.3", false);

        static cvar_t   v_idlescale = new cvar_t("v_idlescale", "0", false);

        static cvar_t   crosshair = new cvar_t("crosshair", "0", true);
        static cvar_t   cl_crossx = new cvar_t("cl_crossx", "0", false);
        static cvar_t   cl_crossy = new cvar_t("cl_crossy", "0", false);

        static cvar_t   gl_cshiftpercent = new cvar_t("gl_cshiftpercent", "100", false);

        static double	v_dmg_time, v_dmg_roll, v_dmg_pitch;

        /*
        ===============
        V_CalcRoll

        Used by view and sv_user
        ===============
        */
        static double[]    forward = new double[3], right = new double[3], up = new double[3];

        public static double V_CalcRoll (double[] angles, double[] velocity)
        {
	        double	sign;
            double  side;
	        double	value;
        	
	        mathlib.AngleVectors (angles, ref forward, ref right, ref up);
            side = mathlib.DotProduct(velocity, right);
	        sign = side < 0 ? -1 : 1;
	        side = Math.Abs(side);
        	
	        value = cl_rollangle.value;
        //	if (cl.inwater)
        //		value *= 6;

	        if (side < cl_rollspeed.value)
		        side = side * value / cl_rollspeed.value;
	        else
		        side = value;
        	
	        return side*sign;
        	
        }

        /*
        ===============
        V_CalcBob

        ===============
        */
        static double V_CalcBob()
        {
	        double	bob;
            double  cycle;

            cycle = client.cl.time - (int)(client.cl.time / cl_bobcycle.value) * cl_bobcycle.value;
            cycle /= cl_bobcycle.value;
	        if (cycle < cl_bobup.value)
		        cycle = mathlib.M_PI * cycle / cl_bobup.value;
	        else
                cycle = mathlib.M_PI + mathlib.M_PI * (cycle - cl_bobup.value) / (1.0 - cl_bobup.value);

        // bob is proportional to velocity in the xy plane
        // (don't count Z, or jumping messes it up)

            bob = Math.Sqrt(client.cl.velocity[0] * client.cl.velocity[0] + client.cl.velocity[1] * client.cl.velocity[1]) * cl_bob.value;
        //Con_Printf ("speed: %5.1f\n", Length(cl.velocity));
            bob = bob * 0.3 + bob * 0.7 * Math.Sin(cycle);
	        if (bob > 4)
		        bob = 4;
	        else if (bob < -7)
		        bob = -7;
	        return bob;
        }

        //=============================================================================

        static cvar_t	v_centermove = new cvar_t("v_centermove", "0.15", false);
        static cvar_t	v_centerspeed = new cvar_t("v_centerspeed","500");
        
        public static void V_StartPitchDrift ()
        {
            if (client.cl.laststop == client.cl.time)
	        {
		        return;		// something else is keeping it from drifting
	        }

            if (client.cl.nodrift || client.cl.pitchvel == 0)
	        {
                client.cl.pitchvel = v_centerspeed.value;
                client.cl.nodrift = false;
                client.cl.driftmove = 0;
	        }
        }

        public static void V_StopPitchDrift ()
        {
            client.cl.laststop = client.cl.time;
            client.cl.nodrift = true;
            client.cl.pitchvel = 0;
        }

        /*
        ===============
        V_DriftPitch

        Moves the client pitch angle towards cl.idealpitch sent by the server.

        If the user is adjusting pitch manually, either with lookup/lookdown,
        mlook and mouse, or klook and keyboard, pitch drifting is constantly stopped.

        Drifting is enabled when the center view key is hit, mlook is released and
        lookspring is non 0, or when 
        ===============
        */
        static void V_DriftPitch ()
        {
            double delta, move;

            if (host.noclip_anglehack || !client.cl.onground || client.cls.demoplayback)
            {
                client.cl.driftmove = 0;
                client.cl.pitchvel = 0;
                return;
            }

            // don't count small mouse motion
            if (client.cl.nodrift)
            {
                /*if (Math.Abs(client.cl.cmd.forwardmove) < client.cl_forwardspeed.value)
                    client.cl.driftmove = 0;
                else*/
                    client.cl.driftmove += host.host_frametime;

                if (client.cl.driftmove > v_centermove.value)
                {
                    V_StartPitchDrift();
                }
                return;
            }

            delta = client.cl.idealpitch - client.cl.viewangles[quakedef.PITCH];

            if (delta == 0)
            {
                client.cl.pitchvel = 0;
                return;
            }

            move = host.host_frametime * client.cl.pitchvel;
            client.cl.pitchvel += host.host_frametime * v_centerspeed.value;

            //Con_Printf ("move: %f (%f)\n", move, host_frametime);

            if (delta > 0)
            {
                if (move > delta)
                {
                    client.cl.pitchvel = 0;
                    move = delta;
                }
                client.cl.viewangles[quakedef.PITCH] += move;
            }
            else if (delta < 0)
            {
                if (move > -delta)
                {
                    client.cl.pitchvel = 0;
                    move = -delta;
                }
                client.cl.viewangles[quakedef.PITCH] -= move;
            }
        }

        /*
        ============================================================================== 
         
						        PALETTE FLASHES 
         
        ============================================================================== 
        */ 

        static client.cshift_t  cshift_empty = new client.cshift_t( new int[] { 130, 80, 50}, 0);
        static client.cshift_t  cshift_water = new client.cshift_t( new int[] { 130, 80, 50 }, 128);
        static client.cshift_t  cshift_slime = new client.cshift_t( new int[] { 0, 25, 5 }, 150);
        static client.cshift_t  cshift_lava = new client.cshift_t ( new int[] { 255, 80, 0 }, 150);
         
        public static cvar_t		v_gamma = new cvar_t("gamma", "1", true);

        static byte[]		gammatable = new byte[256];	// palette is sent through this

        static void BuildGammaTable (double g)
        {
	        int		i, inf;
        	
	        if (g == 1.0)
	        {
		        for (i=0 ; i<256 ; i++)
			        gammatable[i] = (byte) i;
		        return;
	        }
        	
	        for (i=0 ; i<256 ; i++)
	        {
		        inf = (int) (255 * Math.Pow ( (i+0.5)/255.5 , g ) + 0.5);
		        if (inf < 0)
			        inf = 0;
		        if (inf > 255)
			        inf = 255;
		        gammatable[i] = (byte) inf;
	        }
        }

        /*
        =================
        V_CheckGamma
        =================
        */
        static double oldgammavalue;
        static bool V_CheckGamma()
        {
	        if (v_gamma.value == oldgammavalue)
		        return false;
	        oldgammavalue = v_gamma.value;
        	
	        BuildGammaTable (v_gamma.value);
            screen.vid.recalc_refdef = true;				// force a surface cache flush
      	
	        return true;
        }

        /*
        ===============
        V_ParseDamage
        ===============
        */
        public static void V_ParseDamage ()
        {
	        int		        armor, blood;
	        double[]	    from = new double[3];
	        int		        i;
            double[]        forward = new double[3], right = new double[3], up = new double[3];
	        render.entity_t	ent;
	        double	        side;
	        double	        count;
        	
	        armor = common.MSG_ReadByte ();
            blood = common.MSG_ReadByte();
	        for (i=0 ; i<3 ; i++)
                from[i] = common.MSG_ReadCoord();

            count = blood * 0.5 + armor * 0.5;
	        if (count < 10)
		        count = 10;

            client.cl.faceanimtime = client.cl.time + 0.2;		// but sbar face into pain frame

	        client.cl.cshifts[client.CSHIFT_DAMAGE].percent += (int)(3*count);
            if (client.cl.cshifts[client.CSHIFT_DAMAGE].percent < 0)
                client.cl.cshifts[client.CSHIFT_DAMAGE].percent = 0;
            if (client.cl.cshifts[client.CSHIFT_DAMAGE].percent > 150)
                client.cl.cshifts[client.CSHIFT_DAMAGE].percent = 150;

	        if (armor > blood)		
	        {
                client.cl.cshifts[client.CSHIFT_DAMAGE].destcolor[0] = 200;
                client.cl.cshifts[client.CSHIFT_DAMAGE].destcolor[1] = 100;
                client.cl.cshifts[client.CSHIFT_DAMAGE].destcolor[2] = 100;
	        }
	        else if (armor != 0)
	        {
                client.cl.cshifts[client.CSHIFT_DAMAGE].destcolor[0] = 220;
                client.cl.cshifts[client.CSHIFT_DAMAGE].destcolor[1] = 50;
                client.cl.cshifts[client.CSHIFT_DAMAGE].destcolor[2] = 50;
	        }
	        else
	        {
                client.cl.cshifts[client.CSHIFT_DAMAGE].destcolor[0] = 255;
                client.cl.cshifts[client.CSHIFT_DAMAGE].destcolor[1] = 0;
                client.cl.cshifts[client.CSHIFT_DAMAGE].destcolor[2] = 0;
	        }

        //
        // calculate view angle kicks
        //
	        ent = client.cl_entities[client.cl.viewentity];
        	
	        mathlib.VectorSubtract (from, ent.origin, ref from);
            mathlib.VectorNormalize(ref from);

            mathlib.AngleVectors(ent.angles, ref forward, ref right, ref up);

            side = mathlib.DotProduct(from, right);
	        v_dmg_roll = count*side*v_kickroll.value;

            side = mathlib.DotProduct(from, forward);
	        v_dmg_pitch = count*side*v_kickpitch.value;

	        v_dmg_time = v_kicktime.value;
        }

        /*
        ==================
        V_cshift_f
        ==================
        */
        static void V_cshift_f ()
        {
            cshift_empty.destcolor[0] = int.Parse(cmd.Cmd_Argv(1));
            cshift_empty.destcolor[1] = int.Parse(cmd.Cmd_Argv(2));
            cshift_empty.destcolor[2] = int.Parse(cmd.Cmd_Argv(3));
            cshift_empty.percent = int.Parse(cmd.Cmd_Argv(4));
        }


        /*
        ==================
        V_BonusFlash_f

        When you run over an item, the server sends this command
        ==================
        */
        static void V_BonusFlash_f ()
        {
            client.cl.cshifts[client.CSHIFT_BONUS].destcolor[0] = 215;
            client.cl.cshifts[client.CSHIFT_BONUS].destcolor[1] = 186;
            client.cl.cshifts[client.CSHIFT_BONUS].destcolor[2] = 69;
            client.cl.cshifts[client.CSHIFT_BONUS].percent = 50;
        }

        /*
        =============
        V_SetContentsColor

        Underwater, lava, etc each has a color shift
        =============
        */
        public static void V_SetContentsColor (int contents)
        {
	        switch (contents)
	        {
	        case bspfile.CONTENTS_EMPTY:
            case bspfile.CONTENTS_SOLID:
                client.cl.cshifts[client.CSHIFT_CONTENTS] = cshift_empty;
		        break;
            case bspfile.CONTENTS_LAVA:
                client.cl.cshifts[client.CSHIFT_CONTENTS] = cshift_lava;
		        break;
            case bspfile.CONTENTS_SLIME:
                client.cl.cshifts[client.CSHIFT_CONTENTS] = cshift_slime;
		        break;
	        default:
                client.cl.cshifts[client.CSHIFT_CONTENTS] = cshift_water;
                break;
	        }
        }

        /*
        =============
        V_CalcPowerupCshift
        =============
        */
        static void V_CalcPowerupCshift ()
        {
            if ((client.cl.items & quakedef.IT_QUAD) != 0)
	        {
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[0] = 0;
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[1] = 0;
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[2] = 255;
                client.cl.cshifts[client.CSHIFT_POWERUP].percent = 30;
	        }
            else if ((client.cl.items & quakedef.IT_SUIT) != 0)
	        {
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[0] = 0;
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[1] = 255;
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[2] = 0;
                client.cl.cshifts[client.CSHIFT_POWERUP].percent = 20;
	        }
	        else if ((client.cl.items & quakedef.IT_INVISIBILITY) != 0)
	        {
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[0] = 100;
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[1] = 100;
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[2] = 100;
                client.cl.cshifts[client.CSHIFT_POWERUP].percent = 100;
	        }
	        else if ((client.cl.items & quakedef.IT_INVULNERABILITY) != 0)
	        {
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[0] = 255;
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[1] = 255;
                client.cl.cshifts[client.CSHIFT_POWERUP].destcolor[2] = 0;
                client.cl.cshifts[client.CSHIFT_POWERUP].percent = 30;
	        }
	        else
                client.cl.cshifts[client.CSHIFT_POWERUP].percent = 0;
        }

        /*
        =============
        V_UpdatePalette
        =============
        */
        public static void V_UpdatePalette ()
        {
	        int		i, j;
	        bool	@new;
	        int	    basepal, newpal;
	        byte[]	pal = new byte[768];
	        int		r,g,b;
	        bool    force;

	        V_CalcPowerupCshift ();
        	
	        @new = false;
        	
	        for (i=0 ; i<client.NUM_CSHIFTS ; i++)
	        {
                if (client.cl.cshifts[i].percent != client.cl.prev_cshifts[i].percent)
		        {
			        @new = true;
                    client.cl.prev_cshifts[i].percent = client.cl.cshifts[i].percent;
		        }
		        for (j=0 ; j<3 ; j++)
                    if (client.cl.cshifts[i].destcolor[j] != client.cl.prev_cshifts[i].destcolor[j])
			        {
				        @new = true;
                        client.cl.prev_cshifts[i].destcolor[j] = client.cl.cshifts[i].destcolor[j];
			        }
	        }

        // drop the damage value
            client.cl.cshifts[client.CSHIFT_DAMAGE].percent -= (int)(host.host_frametime * 150);
            if (client.cl.cshifts[client.CSHIFT_DAMAGE].percent <= 0)
                client.cl.cshifts[client.CSHIFT_DAMAGE].percent = 0;

        // drop the bonus value
            client.cl.cshifts[client.CSHIFT_BONUS].percent -= (int)(host.host_frametime * 100);
            if (client.cl.cshifts[client.CSHIFT_BONUS].percent <= 0)
                client.cl.cshifts[client.CSHIFT_BONUS].percent = 0;

	        force = V_CheckGamma ();
	        if (!@new && !force)
		        return;
        			
	        basepal = 0;
	        newpal = 0;
        	
	        for (i=0 ; i<256 ; i++)
	        {
		        r = host.host_basepal[basepal+0];
                g = host.host_basepal[basepal + 1];
                b = host.host_basepal[basepal + 2];
		        basepal += 3;
        	
		        for (j=0 ; j<client.NUM_CSHIFTS ; j++)	
		        {
                    r += (client.cl.cshifts[j].percent * (client.cl.cshifts[j].destcolor[0] - r)) >> 8;
                    g += (client.cl.cshifts[j].percent * (client.cl.cshifts[j].destcolor[1] - g)) >> 8;
                    b += (client.cl.cshifts[j].percent * (client.cl.cshifts[j].destcolor[2] - b)) >> 8;
		        }

		        pal[newpal+0] = gammatable[r];
		        pal[newpal+1] = gammatable[g];
                pal[newpal + 2] = gammatable[b];
		        newpal += 3;
	        }

	        vid.VID_ShiftPalette (pal);	
        }

        /* 
        ============================================================================== 
         
						        VIEW RENDERING 
         
        ============================================================================== 
        */

        static double angledelta(double a)
        {
	        a = mathlib.anglemod(a);
	        if (a > 180)
		        a -= 360;
	        return a;
        }

        /*
        ==================
        CalcGunAngle
        ==================
        */
        static double oldyaw = 0;
        static double oldpitch = 0;
        static void CalcGunAngle()
        {
	        double  yaw, pitch, move;
	
	        yaw = render.r_refdef.viewangles[quakedef.YAW];
            pitch = -render.r_refdef.viewangles[quakedef.PITCH];

            yaw = angledelta(yaw - render.r_refdef.viewangles[quakedef.YAW]) * 0.4;
	        if (yaw > 10)
		        yaw = 10;
	        if (yaw < -10)
		        yaw = -10;
            pitch = angledelta(-pitch - render.r_refdef.viewangles[quakedef.PITCH]) * 0.4;
	        if (pitch > 10)
		        pitch = 10;
	        if (pitch < -10)
		        pitch = -10;
	        move = host.host_frametime*20;
	        if (yaw > oldyaw)
	        {
		        if (oldyaw + move < yaw)
			        yaw = oldyaw + move;
	        }
	        else
	        {
		        if (oldyaw - move > yaw)
			        yaw = oldyaw - move;
	        }
        	
	        if (pitch > oldpitch)
	        {
		        if (oldpitch + move < pitch)
			        pitch = oldpitch + move;
	        }
	        else
	        {
		        if (oldpitch - move > pitch)
			        pitch = oldpitch - move;
	        }
        	
	        oldyaw = yaw;
	        oldpitch = pitch;

	        client.cl.viewent.angles[quakedef.YAW] = render.r_refdef.viewangles[quakedef.YAW] + yaw;
            client.cl.viewent.angles[quakedef.PITCH] = -(render.r_refdef.viewangles[quakedef.PITCH] + pitch);

            client.cl.viewent.angles[quakedef.ROLL] -= v_idlescale.value * Math.Sin(client.cl.time * v_iroll_cycle.value) * v_iroll_level.value;
            client.cl.viewent.angles[quakedef.PITCH] -= v_idlescale.value * Math.Sin(client.cl.time * v_ipitch_cycle.value) * v_ipitch_level.value;
            client.cl.viewent.angles[quakedef.YAW] -= v_idlescale.value * Math.Sin(client.cl.time * v_iyaw_cycle.value) * v_iyaw_level.value;
        }

        /*
        ==============
        V_BoundOffsets
        ==============
        */
        static void V_BoundOffsets ()
        {
	        render.entity_t ent;

            ent = client.cl_entities[client.cl.viewentity];

        // absolutely bound refresh reletive to entity clipping hull
        // so the view can never be inside a solid wall

	        if (render.r_refdef.vieworg[0] < ent.origin[0] - 14)
                render.r_refdef.vieworg[0] = ent.origin[0] - 14;
            else if (render.r_refdef.vieworg[0] > ent.origin[0] + 14)
                render.r_refdef.vieworg[0] = ent.origin[0] + 14;
            if (render.r_refdef.vieworg[1] < ent.origin[1] - 14)
                render.r_refdef.vieworg[1] = ent.origin[1] - 14;
            else if (render.r_refdef.vieworg[1] > ent.origin[1] + 14)
                render.r_refdef.vieworg[1] = ent.origin[1] + 14;
            if (render.r_refdef.vieworg[2] < ent.origin[2] - 22)
                render.r_refdef.vieworg[2] = ent.origin[2] - 22;
            else if (render.r_refdef.vieworg[2] > ent.origin[2] + 30)
                render.r_refdef.vieworg[2] = ent.origin[2] + 30;
        }

        /*
        ==============
        V_AddIdle

        Idle swaying
        ==============
        */
        static void V_AddIdle ()
        {
            render.r_refdef.viewangles[quakedef.ROLL] += v_idlescale.value * Math.Sin(client.cl.time * v_iroll_cycle.value) * v_iroll_level.value;
            render.r_refdef.viewangles[quakedef.PITCH] += v_idlescale.value * Math.Sin(client.cl.time * v_ipitch_cycle.value) * v_ipitch_level.value;
            render.r_refdef.viewangles[quakedef.YAW] += v_idlescale.value * Math.Sin(client.cl.time * v_iyaw_cycle.value) * v_iyaw_level.value;
        }
        
        /*
        ==============
        V_CalcViewRoll

        Roll is induced by movement and damage
        ==============
        */
        static void V_CalcViewRoll ()
        {
	        double		side;

            side = V_CalcRoll(client.cl_entities[client.cl.viewentity].angles, client.cl.velocity);
	        render.r_refdef.viewangles[quakedef.ROLL] += side;

	        if (v_dmg_time > 0)
	        {
                render.r_refdef.viewangles[quakedef.ROLL] += v_dmg_time / v_kicktime.value * v_dmg_roll;
                render.r_refdef.viewangles[quakedef.PITCH] += v_dmg_time / v_kicktime.value * v_dmg_pitch;
                v_dmg_time -= host.host_frametime;
	        }

	        if (client.cl.stats[quakedef.STAT_HEALTH] <= 0)
	        {
                render.r_refdef.viewangles[quakedef.ROLL] = 80;	// dead view angle
		        return;
	        }
        }
        
        /*
        ==================
        V_CalcIntermissionRefdef

        ==================
        */
        static void V_CalcIntermissionRefdef ()
        {
	        render.entity_t	ent, view;
	        double		    old;

        // ent is the player model (visible when out of body)
            ent = client.cl_entities[client.cl.viewentity];
        // view is the weapon model (only visible from inside body)
            view = client.cl.viewent;

	        mathlib.VectorCopy (ent.origin, ref render.r_refdef.vieworg);
            mathlib.VectorCopy(ent.angles, ref render.r_refdef.viewangles);
	        view.model = null;

        // allways idle in intermission
	        old = v_idlescale.value;
	        v_idlescale.value = 1;
	        V_AddIdle ();
	        v_idlescale.value = old;
        }

        /*
        ==================
        V_CalcRefdef

        ==================
        */
        static double oldz = 0;
        static void V_CalcRefdef ()
        {
	        render.entity_t	ent, view;
	        int			    i;
            double[]        forward = new double[3], right = new double[3], up = new double[3];
            double[]        angles = new double[3];
            double          bob;

            V_DriftPitch();

        // ent is the player model (visible when out of body)
            ent = client.cl_entities[client.cl.viewentity];
        // view is the weapon model (only visible from inside body)
            view = client.cl.viewent;
	

        // transform the view offset by the model's matrix to get the offset from
        // model origin for the view
            ent.angles[quakedef.YAW] = client.cl.viewangles[quakedef.YAW];	// the model should face
										        // the view dir
            ent.angles[quakedef.PITCH] = -client.cl.viewangles[quakedef.PITCH];	// the model should face
										        // the view dir

        	bob = V_CalcBob ();

        // refresh position
            mathlib.VectorCopy(ent.origin, ref render.r_refdef.vieworg);
	        render.r_refdef.vieworg[2] += client.cl.viewheight + bob;

        // never let it sit exactly on a node line, because a water plane can
        // dissapear when viewed with the eye exactly on it.
        // the server protocol only specifies to 1/16 pixel, so add 1/32 in each axis
            render.r_refdef.vieworg[0] += 1.0 / 32;
            render.r_refdef.vieworg[1] += 1.0 / 32;
            render.r_refdef.vieworg[2] += 1.0 / 32;

            mathlib.VectorCopy(client.cl.viewangles, ref render.r_refdef.viewangles);
            V_CalcViewRoll();
            V_AddIdle();

        // offsets
	        angles[quakedef.PITCH] = -ent.angles[quakedef.PITCH];	// because entity pitches are
											        //  actually backward
	        angles[quakedef.YAW] = ent.angles[quakedef.YAW];
	        angles[quakedef.ROLL] = ent.angles[quakedef.ROLL];

            mathlib.AngleVectors(angles, ref forward, ref right, ref up);

	        for (i=0 ; i<3 ; i++)
		        render.r_refdef.vieworg[i] += scr_ofsx.value*forward[i]
			        + scr_ofsy.value*right[i]
			        + scr_ofsz.value*up[i];

	        V_BoundOffsets ();

        // set up gun position
            mathlib.VectorCopy(client.cl.viewangles, ref view.angles);

	        CalcGunAngle ();

            mathlib.VectorCopy(ent.origin, ref view.origin);
            view.origin[2] += client.cl.viewheight;

	        for (i=0 ; i<3 ; i++)
	        {
                view.origin[i] += forward[i] * bob * 0.4;
        //		view.origin[i] += right[i]*bob*0.4;
        //		view.origin[i] += up[i]*bob*0.8;
	        }
	        view.origin[2] += bob;

        // fudge position around to keep amount of weapon visible
        // roughly equal with different FOV

	        if (screen.scr_viewsize.value == 110)
		        view.origin[2] += 1;
            else if (screen.scr_viewsize.value == 100)
		        view.origin[2] += 2;
            else if (screen.scr_viewsize.value == 90)
		        view.origin[2] += 1;
            else if (screen.scr_viewsize.value == 80)
		        view.origin[2] += 0.5;

            view.model = client.cl.model_precache[client.cl.stats[quakedef.STAT_WEAPON]];
            view.frame = client.cl.stats[quakedef.STAT_WEAPONFRAME];
	        view.colormap = screen.vid.colormap;

        // set up the refresh position
            mathlib.VectorAdd(render.r_refdef.viewangles, client.cl.punchangle, ref render.r_refdef.viewangles);

            // smooth out stair step ups
            if (client.cl.onground && ent.origin[2] - oldz > 0)
            {
	            double steptime;
            	
	            steptime = client.cl.time - client.cl.oldtime;
	            if (steptime < 0)
            //FIXME		I_Error ("steptime < 0");
		            steptime = 0;

	            oldz += steptime * 80;
	            if (oldz > ent.origin[2])
		            oldz = ent.origin[2];
	            if (ent.origin[2] - oldz > 12)
		            oldz = ent.origin[2] - 12;
	            render.r_refdef.vieworg[2] += oldz - ent.origin[2];
	            view.origin[2] += oldz - ent.origin[2];
            }
            else
	            oldz = ent.origin[2];

            if (chase.chase_active.value != 0)
                chase.Chase_Update();
        }

        /*
        ==================
        V_RenderView

        The player's clipping box goes from (-16 -16 -24) to (16 16 32) from
        the entity origin, so any view position inside that will be valid
        ==================
        */
        public static void V_RenderView ()
        {
            if (console.con_forcedup)
                return;

            // don't allow cheats in multiplayer
            if (client.cl.maxclients > 1)
            {
                cvar_t.Cvar_Set("scr_ofsx", "0");
                cvar_t.Cvar_Set("scr_ofsy", "0");
                cvar_t.Cvar_Set("scr_ofsz", "0");
            }

            if (client.cl.intermission != 0)
            {	// intermission / finale rendering
                V_CalcIntermissionRefdef();
            }
            else
            {
                if (!client.cl.paused /* && (sv.maxclients > 1 || key_dest == key_game) */ )
                    V_CalcRefdef();
            }

	        render.R_PushDlights ();

	        render.R_RenderView ();

	        if (crosshair.value != 0)
                draw.Draw_Character((int)(screen.scr_vrect.x + screen.scr_vrect.width / 2 + cl_crossx.value),
                     (int)(screen.scr_vrect.y + screen.scr_vrect.height / 2 + cl_crossy.value), '+');
        }

        //============================================================================

        /*
        =============
        V_Init
        =============
        */
        public static void V_Init ()
        {
            cmd.Cmd_AddCommand("v_cshift", V_cshift_f);
            cmd.Cmd_AddCommand("bf", V_BonusFlash_f);
            cmd.Cmd_AddCommand("centerview", V_StartPitchDrift);

            cvar_t.Cvar_RegisterVariable(lcd_x);
            cvar_t.Cvar_RegisterVariable(lcd_yaw);

            cvar_t.Cvar_RegisterVariable(v_centermove);
            cvar_t.Cvar_RegisterVariable(v_centerspeed);

            cvar_t.Cvar_RegisterVariable(v_iyaw_cycle);
            cvar_t.Cvar_RegisterVariable(v_iroll_cycle);
            cvar_t.Cvar_RegisterVariable(v_ipitch_cycle);
            cvar_t.Cvar_RegisterVariable(v_iyaw_level);
            cvar_t.Cvar_RegisterVariable(v_iroll_level);
            cvar_t.Cvar_RegisterVariable(v_ipitch_level);

            cvar_t.Cvar_RegisterVariable(v_idlescale);
            cvar_t.Cvar_RegisterVariable(crosshair);
            cvar_t.Cvar_RegisterVariable(cl_crossx);
            cvar_t.Cvar_RegisterVariable(cl_crossy);
            cvar_t.Cvar_RegisterVariable(gl_cshiftpercent);

            cvar_t.Cvar_RegisterVariable(scr_ofsx);
            cvar_t.Cvar_RegisterVariable(scr_ofsy);
            cvar_t.Cvar_RegisterVariable(scr_ofsz);
            cvar_t.Cvar_RegisterVariable(cl_rollspeed);
            cvar_t.Cvar_RegisterVariable(cl_rollangle);
            cvar_t.Cvar_RegisterVariable(cl_bob);
            cvar_t.Cvar_RegisterVariable(cl_bobcycle);
            cvar_t.Cvar_RegisterVariable(cl_bobup);

            cvar_t.Cvar_RegisterVariable(v_kicktime);
            cvar_t.Cvar_RegisterVariable(v_kickroll);
            cvar_t.Cvar_RegisterVariable(v_kickpitch);

            BuildGammaTable(1.0);	// no gamma yet
            cvar_t.Cvar_RegisterVariable(v_gamma);
        }
    }
}


