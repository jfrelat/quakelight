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
// cl_tent.c -- client side temporary entities

namespace quake
{
    public partial class client
    {
        static int                  num_temp_entities;
        static render.entity_t[]	cl_temp_entities = new render.entity_t[MAX_TEMP_ENTITIES];
        static beam_t[]		        cl_beams = new beam_t[MAX_BEAMS];

        static sound.sfx_t			cl_sfx_wizhit;
        static sound.sfx_t          cl_sfx_knighthit;
        static sound.sfx_t          cl_sfx_tink1;
        static sound.sfx_t          cl_sfx_ric1;
        static sound.sfx_t          cl_sfx_ric2;
        static sound.sfx_t          cl_sfx_ric3;
        static sound.sfx_t          cl_sfx_r_exp3;

        /*
        =================
        CL_ParseTEnt
        =================
        */
        static void CL_InitTEnts ()
        {
	        cl_sfx_wizhit = sound.S_PrecacheSound ("wizard/hit.wav");
            cl_sfx_knighthit = sound.S_PrecacheSound("hknight/hit.wav");
            cl_sfx_tink1 = sound.S_PrecacheSound("weapons/tink1.wav");
            cl_sfx_ric1 = sound.S_PrecacheSound("weapons/ric1.wav");
            cl_sfx_ric2 = sound.S_PrecacheSound("weapons/ric2.wav");
            cl_sfx_ric3 = sound.S_PrecacheSound("weapons/ric3.wav");
            cl_sfx_r_exp3 = sound.S_PrecacheSound("weapons/r_exp3.wav");
        }

        /*
        =================
        CL_ParseBeam
        =================
        */
        static void CL_ParseBeam (model.model_t m)
        {
	        int		    ent;
            double[]    start = new double[3], end = new double[3];
	        beam_t	    b;
	        int		    i;
        	
	        ent = common.MSG_ReadShort ();

            start[0] = common.MSG_ReadCoord();
            start[1] = common.MSG_ReadCoord();
            start[2] = common.MSG_ReadCoord();

            end[0] = common.MSG_ReadCoord();
            end[1] = common.MSG_ReadCoord();
            end[2] = common.MSG_ReadCoord();

        // override any beam with the same entity
            for (i = 0, b = cl_beams[i]; i < MAX_BEAMS; i++)
                b = cl_beams[i];
		        if (b.entity == ent)
		        {
			        b.entity = ent;
			        b.model = m;
                    b.endtime = cl.time + 0.2;
			        mathlib.VectorCopy (start, ref b.start);
                    mathlib.VectorCopy(end, ref b.end);
			        return;
		        }

        // find a free beam
	        for (i=0, b=cl_beams[i] ; i< MAX_BEAMS ; i++)
	        {
                b = cl_beams[i];
		        if (b.model == null || b.endtime < cl.time)
		        {
			        b.entity = ent;
			        b.model = m;
                    b.endtime = cl.time + 0.2;
			        mathlib.VectorCopy (start, ref b.start);
                    mathlib.VectorCopy(end, ref b.end);
			        return;
		        }
	        }
	        console.Con_Printf ("beam list overflow!\n");	
        }

        /*
        =================
        CL_ParseTEnt
        =================
        */
        static void CL_ParseTEnt ()
        {
	        int		    type;
	        double[]	pos = new double[3];
            dlight_t    dl;
            int         rnd;
	        int		    colorStart, colorLength;

	        type = common.MSG_ReadByte ();
	        switch (type)
	        {
	        case net.TE_WIZSPIKE:			// spike hitting wall
		        pos[0] = common.MSG_ReadCoord ();
		        pos[1] = common.MSG_ReadCoord ();
		        pos[2] = common.MSG_ReadCoord ();
		        render.R_RunParticleEffect (pos, mathlib.vec3_origin, 20, 30);
		        sound.S_StartSound (-1, 0, cl_sfx_wizhit, pos, 1, 1);
		        break;

            case net.TE_KNIGHTSPIKE:			// spike hitting wall
		        pos[0] = common.MSG_ReadCoord ();
                pos[1] = common.MSG_ReadCoord();
                pos[2] = common.MSG_ReadCoord();
		        render.R_RunParticleEffect (pos, mathlib.vec3_origin, 226, 20);
		        sound.S_StartSound (-1, 0, cl_sfx_knighthit, pos, 1, 1);
		        break;

            case net.TE_SPIKE:			// spike hitting wall
		        pos[0] = common.MSG_ReadCoord ();
                pos[1] = common.MSG_ReadCoord();
                pos[2] = common.MSG_ReadCoord();
		        render.R_RunParticleEffect (pos, mathlib.vec3_origin, 0, 10);
		        if ( (helper.rand() % 5) != 0 )
			        sound.S_StartSound (-1, 0, cl_sfx_tink1, pos, 1, 1);
		        else
		        {
			        rnd = helper.rand() & 3;
			        if (rnd == 1)
                        sound.S_StartSound(-1, 0, cl_sfx_ric1, pos, 1, 1);
			        else if (rnd == 2)
                        sound.S_StartSound(-1, 0, cl_sfx_ric2, pos, 1, 1);
			        else
                        sound.S_StartSound(-1, 0, cl_sfx_ric3, pos, 1, 1);
		        }
		        break;

            case net.TE_SUPERSPIKE:			// super spike hitting wall
		        pos[0] = common.MSG_ReadCoord ();
		        pos[1] = common.MSG_ReadCoord ();
		        pos[2] = common.MSG_ReadCoord ();
		        render.R_RunParticleEffect (pos, mathlib.vec3_origin, 0, 20);

		        if ( (helper.rand() % 5) != 0 )
			        sound.S_StartSound (-1, 0, cl_sfx_tink1, pos, 1, 1);
		        else
		        {
			        rnd = helper.rand() & 3;
			        if (rnd == 1)
                        sound.S_StartSound(-1, 0, cl_sfx_ric1, pos, 1, 1);
			        else if (rnd == 2)
                        sound.S_StartSound(-1, 0, cl_sfx_ric2, pos, 1, 1);
			        else
                        sound.S_StartSound(-1, 0, cl_sfx_ric3, pos, 1, 1);
		        }
		        break;
        		
	        case net.TE_GUNSHOT:			// bullet hitting wall
		        pos[0] = common.MSG_ReadCoord ();
                pos[1] = common.MSG_ReadCoord();
                pos[2] = common.MSG_ReadCoord();
		        render.R_RunParticleEffect (pos, mathlib.vec3_origin, 0, 20);
		        break;
        		
	        case net.TE_EXPLOSION:			// rocket explosion
		        pos[0] = common.MSG_ReadCoord ();
                pos[1] = common.MSG_ReadCoord();
                pos[2] = common.MSG_ReadCoord();
		        render.R_ParticleExplosion (pos);
		        dl = CL_AllocDlight (0);
		        mathlib.VectorCopy (pos, ref dl.origin);
		        dl.radius = 350;
		        dl.die = cl.time + 0.5;
		        dl.decay = 300;
		        sound.S_StartSound (-1, 0, cl_sfx_r_exp3, pos, 1, 1);
		        break;
        		
	        case net.TE_TAREXPLOSION:			// tarbaby explosion
                pos[0] = common.MSG_ReadCoord();
                pos[1] = common.MSG_ReadCoord();
                pos[2] = common.MSG_ReadCoord();
		        render.R_BlobExplosion (pos);

                sound.S_StartSound(-1, 0, cl_sfx_r_exp3, pos, 1, 1);
		        break;

	        case net.TE_LIGHTNING1:				// lightning bolts
		        CL_ParseBeam (model.Mod_ForName("progs/bolt.mdl", true));
		        break;
        	
	        case net.TE_LIGHTNING2:				// lightning bolts
                CL_ParseBeam(model.Mod_ForName("progs/bolt2.mdl", true));
		        break;
        	
	        case net.TE_LIGHTNING3:				// lightning bolts
                CL_ParseBeam(model.Mod_ForName("progs/bolt3.mdl", true));
		        break;
        	
        // PGM 01/21/97 
	        case net.TE_BEAM:				// grappling hook beam
                CL_ParseBeam(model.Mod_ForName("progs/beam.mdl", true));
		        break;
        // PGM 01/21/97

	        case net.TE_LAVASPLASH:
                pos[0] = common.MSG_ReadCoord();
                pos[1] = common.MSG_ReadCoord();
                pos[2] = common.MSG_ReadCoord();
		        render.R_LavaSplash (pos);
		        break;
        	
	        case net.TE_TELEPORT:
                pos[0] = common.MSG_ReadCoord();
                pos[1] = common.MSG_ReadCoord();
                pos[2] = common.MSG_ReadCoord();
		        render.R_TeleportSplash (pos);
		        break;
        		
	        case net.TE_EXPLOSION2:				// color mapped explosion
                pos[0] = common.MSG_ReadCoord();
                pos[1] = common.MSG_ReadCoord();
                pos[2] = common.MSG_ReadCoord();
                colorStart = common.MSG_ReadByte();
                colorLength = common.MSG_ReadByte();
		        render.R_ParticleExplosion2 (pos, colorStart, colorLength);
		        dl = CL_AllocDlight (0);
		        mathlib.VectorCopy (pos, ref dl.origin);
		        dl.radius = 350;
		        dl.die = cl.time + 0.5;
		        dl.decay = 300;
		        sound.S_StartSound (-1, 0, cl_sfx_r_exp3, pos, 1, 1);
		        break;

	        default:
		        sys_linux.Sys_Error ("CL_ParseTEnt: bad type");
                break;
	        }
        }

        /*
        =================
        CL_NewTempEntity
        =================
        */
        static render.entity_t CL_NewTempEntity ()
        {
	        render.entity_t ent;

            if (cl_numvisedicts == client.MAX_VISEDICTS)
		        return null;
	        if (num_temp_entities == MAX_TEMP_ENTITIES)
                return null;
	        ent = cl_temp_entities[num_temp_entities];
	        //memset (ent, 0, sizeof(*ent));
	        num_temp_entities++;
	        cl_visedicts[cl_numvisedicts] = ent;
	        cl_numvisedicts++;

	        ent.colormap = screen.vid.colormap;
	        return ent;
        }

        /*
        =================
        CL_UpdateTEnts
        =================
        */
        static void CL_UpdateTEnts ()
        {
	        int			    i;
	        beam_t		    b;
            double[]        dist = new double[3], org = new double[3];
	        double		    d;
	        render.entity_t	ent;
            double          yaw, pitch;
	        double		    forward;

	        num_temp_entities = 0;

        // update lightning
	        for (i=0, b=cl_beams[i] ; i< MAX_BEAMS ; i++)
	        {
                b = cl_beams[i];
		        if (b.model == null || b.endtime < cl.time)
			        continue;

	        // if coming from the player, update the start position
		        if (b.entity == cl.viewentity)
		        {
			        mathlib.VectorCopy (cl_entities[cl.viewentity].origin, ref b.start);
		        }

	        // calculate pitch and yaw
		        mathlib.VectorSubtract (b.end, b.start, ref dist);

		        if (dist[1] == 0 && dist[0] == 0)
		        {
			        yaw = 0;
			        if (dist[2] > 0)
				        pitch = 90;
			        else
				        pitch = 270;
		        }
		        else
		        {
			        yaw = (int) (Math.Atan2(dist[1], dist[0]) * 180 / mathlib.M_PI);
			        if (yaw < 0)
				        yaw += 360;

                    forward = Math.Sqrt(dist[0] * dist[0] + dist[1] * dist[1]);
                    pitch = (int)(Math.Atan2(dist[2], forward) * 180 / mathlib.M_PI);
			        if (pitch < 0)
				        pitch += 360;
		        }

	        // add new entities for the lightning
		        mathlib.VectorCopy (b.start, ref org);
		        d = mathlib.VectorNormalize(ref dist);
		        while (d > 0)
		        {
			        ent = CL_NewTempEntity ();
			        if (ent == null)
				        return;
			        mathlib.VectorCopy (org, ref ent.origin);
			        ent.model = b.model;
			        ent.angles[0] = pitch;
			        ent.angles[1] = yaw;
			        ent.angles[2] = Helper.helper.rand()%360;

			        for (i=0 ; i<3 ; i++)
				        org[i] += dist[i]*30;
			        d -= 30;
		        }
	        }
        }
    }
}

