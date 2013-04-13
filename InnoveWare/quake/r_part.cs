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
    public partial class render
    {
        const int MAX_PARTICLES			    = 2048;	    // default max # of particles at one
										                //  time
        const int ABSOLUTE_MIN_PARTICLES	= 512;		// no fewer than this no matter what's
										                //  on the command line

        static int[]	ramp1 = {0x6f, 0x6d, 0x6b, 0x69, 0x67, 0x65, 0x63, 0x61};
        static int[]	ramp2 = {0x6f, 0x6e, 0x6d, 0x6c, 0x6b, 0x6a, 0x68, 0x66};
        static int[]    ramp3 = { 0x6d, 0x6b, 6, 5, 4, 3, 0, 0 };

        static draw.particle_t      active_particles, free_particles;

        static draw.particle_t[]    particles;
        static int			        r_numparticles;

        public static double[]          r_pright = new double[3], r_pup = new double[3], r_ppn = new double[3];

        /*
        ===============
        R_InitParticles
        ===============
        */
        static void R_InitParticles ()
        {
            int i;

            i = common.COM_CheckParm("-particles");

            if (i != 0)
            {
                /*r_numparticles = (int)(common.Q_atoi(com_argv[i + 1]));
                if (r_numparticles < ABSOLUTE_MIN_PARTICLES)
                    r_numparticles = ABSOLUTE_MIN_PARTICLES;*/
            }
            else
            {
                r_numparticles = MAX_PARTICLES;
            }

            particles = new draw.particle_t[r_numparticles];
            for (int kk = 0; kk < r_numparticles; kk++) particles[kk] = new draw.particle_t();
        }

        /*
        ===============
        R_EntityParticles
        ===============
        */

        static double[,]	avelocities = new double[NUMVERTEXNORMALS,3];
        static double	    beamlength = 16;
        double[]	avelocity = {23, 7, 3};
        double	partstep = 0.01;
        double	timescale = 0.01;

        public static void R_EntityParticles (entity_t ent)
        {
	        int			        count;
	        int			        i;
	        draw.particle_t	    p;
	        double		        angle;
	        double		        sr, sp, sy, cr, cp, cy;
	        double[]	        forward = new double[3];
	        double		        dist;
        	
	        dist = 64;
	        count = 50;

            if (avelocities[0,0] == 0)
            {
                for (i=0 ; i<NUMVERTEXNORMALS*3 ; i++)
                    avelocities[0, i] = (helper.rand() & 255) * 0.01;
            }


	        for (i=0 ; i<NUMVERTEXNORMALS ; i++)
	        {
		        angle = client.cl.time * avelocities[i,0];
                sy = Math.Sin(angle);
                cy = Math.Cos(angle);
                angle = client.cl.time * avelocities[i,1];
                sp = Math.Sin(angle);
                cp = Math.Cos(angle);
                angle = client.cl.time * avelocities[i,2];
                sr = Math.Sin(angle);
                cr = Math.Cos(angle);
        	
		        forward[0] = cp*cy;
		        forward[1] = cp*sy;
		        forward[2] = -sp;

		        if (free_particles == null)
			        return;
		        p = free_particles;
		        free_particles = p.next;
		        p.next = active_particles;
		        active_particles = p;

                p.die = client.cl.time + 0.01;
		        p.color = 0x6f;
		        p.type = draw.ptype_t.pt_explode;
        		
		        p.org[0] = ent.origin[0] + r_avertexnormals[i][0]*dist + forward[0]*beamlength;			
		        p.org[1] = ent.origin[1] + r_avertexnormals[i][1]*dist + forward[1]*beamlength;			
		        p.org[2] = ent.origin[2] + r_avertexnormals[i][2]*dist + forward[2]*beamlength;			
	        }
        }

        /*
        ===============
        R_ClearParticles
        ===============
        */
        static void R_ClearParticles()
        {
            int i;

            free_particles = particles[0];
            active_particles = null;

            for (i = 0; i < r_numparticles - 1; i++)
                particles[i].next = particles[i + 1];
            particles[r_numparticles - 1].next = null;
        }

        static void R_ReadPointFile_f ()
        {
        }

        /*
        ===============
        R_ParseParticleEffect

        Parse an effect out of the server message
        ===============
        */
        public static void R_ParseParticleEffect ()
        {
            double[]    org = new double[3], dir = new double[3];
	        int			i, count, msgcount, color;
        	
	        for (i=0 ; i<3 ; i++)
		        org[i] = common.MSG_ReadCoord ();
	        for (i=0 ; i<3 ; i++)
                dir[i] = common.MSG_ReadChar() * (1.0 / 16);
	        msgcount = common.MSG_ReadByte ();
	        color = common.MSG_ReadByte ();

            if (msgcount == 255)
	            count = 1024;
            else
	            count = msgcount;

            R_RunParticleEffect(org, dir, color, count);
        }

        /*
        ===============
        R_ParticleExplosion

        ===============
        */
        public static void R_ParticleExplosion (double[] org)
        {
	        int			    i, j;
	        draw.particle_t	p;
        	
	        for (i=0 ; i<1024 ; i++)
	        {
		        if (free_particles == null)
			        return;
		        p = free_particles;
		        free_particles = p.next;
		        p.next = active_particles;
		        active_particles = p;

		        p.die = client.cl.time + 5;
		        p.color = ramp1[0];
		        p.ramp = helper.rand()&3;
		        if ((i & 1) != 0)
		        {
                    p.type = draw.ptype_t.pt_explode;
			        for (j=0 ; j<3 ; j++)
			        {
				        p.org[j] = org[j] + ((helper.rand()%32)-16);
                        p.vel[j] = (helper.rand() % 512) - 256;
			        }
		        }
		        else
		        {
                    p.type = draw.ptype_t.pt_explode2;
			        for (j=0 ; j<3 ; j++)
			        {
                        p.org[j] = org[j] + ((helper.rand() % 32) - 16);
                        p.vel[j] = (helper.rand() % 512) - 256;
			        }
		        }
	        }
        }

        /*
        ===============
        R_ParticleExplosion2

        ===============
        */
        public static void R_ParticleExplosion2(double[] org, int colorStart, int colorLength)
        {
	        int			    i, j;
	        draw.particle_t	p;
	        int			    colorMod = 0;

	        for (i=0; i<512; i++)
	        {
		        if (free_particles == null)
			        return;
		        p = free_particles;
		        free_particles = p.next;
		        p.next = active_particles;
		        active_particles = p;

                p.die = client.cl.time + 0.3;
		        p.color = colorStart + (colorMod % colorLength);
		        colorMod++;

		        p.type = draw.ptype_t.pt_blob;
		        for (j=0 ; j<3 ; j++)
		        {
                    p.org[j] = org[j] + ((helper.rand() % 32) - 16);
                    p.vel[j] = (helper.rand() % 512) - 256;
		        }
	        }
        }

        /*
        ===============
        R_BlobExplosion

        ===============
        */
        public static void R_BlobExplosion(double[] org)
        {
	        int			    i, j;
	        draw.particle_t	p;
        	
	        for (i=0 ; i<1024 ; i++)
	        {
		        if (free_particles == null)
			        return;
		        p = free_particles;
		        free_particles = p.next;
		        p.next = active_particles;
		        active_particles = p;

                p.die = client.cl.time + 1 + (helper.rand() & 8) * 0.05;

		        if ((i & 1) != 0)
		        {
                    p.type = draw.ptype_t.pt_blob;
                    p.color = 66 + helper.rand() % 6;
			        for (j=0 ; j<3 ; j++)
			        {
                        p.org[j] = org[j] + ((helper.rand() % 32) - 16);
                        p.vel[j] = (helper.rand() % 512) - 256;
			        }
		        }
		        else
		        {
                    p.type = draw.ptype_t.pt_blob2;
                    p.color = 150 + helper.rand() % 6;
			        for (j=0 ; j<3 ; j++)
			        {
                        p.org[j] = org[j] + ((helper.rand() % 32) - 16);
                        p.vel[j] = (helper.rand() % 512) - 256;
			        }
		        }
	        }
        }

        /*
        ===============
        R_RunParticleEffect

        ===============
        */
        public static void R_RunParticleEffect(double[] org, double[] dir, int color, int count)
        {
            int             i, j;
            draw.particle_t p;

            for (i = 0; i < count; i++)
            {
                if (free_particles == null)
                    return;
                p = free_particles;
                free_particles = p.next;
                p.next = active_particles;
                active_particles = p;

                if (count == 1024)
                {	// rocket explosion
                    p.die = client.cl.time + 5;
                    p.color = ramp1[0];
                    p.ramp = helper.rand() & 3;
                    if ((i & 1) != 0)
                    {
                        p.type = draw.ptype_t.pt_explode;
                        for (j = 0; j < 3; j++)
                        {
                            p.org[j] = org[j] + ((helper.rand() % 32) - 16);
                            p.vel[j] = (helper.rand() % 512) - 256;
                        }
                    }
                    else
                    {
                        p.type = draw.ptype_t.pt_explode2;
                        for (j = 0; j < 3; j++)
                        {
                            p.org[j] = org[j] + ((helper.rand() % 32) - 16);
                            p.vel[j] = (helper.rand() % 512) - 256;
                        }
                    }
                }
                else
                {
                    p.die = client.cl.time + 0.1 * (helper.rand() % 5);
                    p.color = (color & ~7) + (helper.rand() & 7);
                    p.type = draw.ptype_t.pt_slowgrav;
                    for (j = 0; j < 3; j++)
                    {
                        p.org[j] = org[j] + ((helper.rand() & 15) - 8);
                        p.vel[j] = dir[j] * 15;// + (rand()%300)-150;
                    }
                }
            }
        }

        /*
        ===============
        R_LavaSplash

        ===============
        */
        public static void R_LavaSplash(double[] org)
        {
	        int			    i, j, k;
	        draw.particle_t	p;
	        double		    vel;
            double[]         dir = new double[3];

	        for (i=-16 ; i<16 ; i++)
		        for (j=-16 ; j<16 ; j++)
			        for (k=0 ; k<1 ; k++)
			        {
				        if (free_particles == null)
					        return;
				        p = free_particles;
				        free_particles = p.next;
				        p.next = active_particles;
				        active_particles = p;

                        p.die = client.cl.time + 2 + (helper.rand() & 31) * 0.02;
                        p.color = 224 + (helper.rand() & 7);
				        p.type = draw.ptype_t.pt_slowgrav;

                        dir[0] = j * 8 + (helper.rand() & 7);
                        dir[1] = i * 8 + (helper.rand() & 7);
				        dir[2] = 256;
        	
				        p.org[0] = org[0] + dir[0];
				        p.org[1] = org[1] + dir[1];
                        p.org[2] = org[2] + (helper.rand() & 63);
        	
				        mathlib.VectorNormalize (ref dir);
                        vel = 50 + (helper.rand() & 63);
				        mathlib.VectorScale (dir, vel, ref p.vel);
			        }
        }

        /*
        ===============
        R_TeleportSplash

        ===============
        */
        public static void R_TeleportSplash(double[] org)
        {
	        int			    i, j, k;
            draw.particle_t p;
	        double		    vel;
	        double[]	    dir = new double[3];

	        for (i=-16 ; i<16 ; i+=4)
		        for (j=-16 ; j<16 ; j+=4)
			        for (k=-24 ; k<32 ; k+=4)
			        {
				        if (free_particles == null)
					        return;
				        p = free_particles;
				        free_particles = p.next;
				        p.next = active_particles;
				        active_particles = p;

                        p.die = client.cl.time + 0.2 + (helper.rand() & 7) * 0.02;
                        p.color = 7 + (helper.rand() & 7);
				        p.type = draw.ptype_t.pt_slowgrav;
        				
				        dir[0] = j*8;
				        dir[1] = i*8;
				        dir[2] = k*8;

                        p.org[0] = org[0] + i + (helper.rand() & 3);
                        p.org[1] = org[1] + j + (helper.rand() & 3);
                        p.org[2] = org[2] + k + (helper.rand() & 3);
        	
				        mathlib.VectorNormalize (ref dir);
                        vel = 50 + (helper.rand() & 63);
				        mathlib.VectorScale (dir, vel, ref p.vel);
			        }
        }

        static int tracercount;
        public static void R_RocketTrail(double[] start, double[] end, int type)
        {
	        double[]	    vec = new double[3];
	        double		    len;
	        int			    j;
	        draw.particle_t	p;
	        int			    dec;

	        mathlib.VectorSubtract (end, start, ref vec);
            len = mathlib.VectorNormalize(ref vec);
	        if (type < 128)
		        dec = 3;
	        else
	        {
		        dec = 1;
		        type -= 128;
	        }

	        while (len > 0)
	        {
		        len -= dec;

		        if (free_particles == null)
			        return;
		        p = free_particles;
		        free_particles = p.next;
		        p.next = active_particles;
		        active_particles = p;
        		
		        mathlib.VectorCopy (mathlib.vec3_origin, ref p.vel);
		        p.die = client.cl.time + 2;

		        switch (type)
		        {
			        case 0:	// rocket trail
				        p.ramp = (helper.rand()&3);
				        p.color = ramp3[(int)p.ramp];
				        p.type = draw.ptype_t.pt_fire;
				        for (j=0 ; j<3 ; j++)
                            p.org[j] = start[j] + ((helper.rand() % 6) - 3);
				        break;

			        case 1:	// smoke smoke
                        p.ramp = (helper.rand() & 3) + 2;
				        p.color = ramp3[(int)p.ramp];
                        p.type = draw.ptype_t.pt_fire;
				        for (j=0 ; j<3 ; j++)
                            p.org[j] = start[j] + ((helper.rand() % 6) - 3);
				        break;

			        case 2:	// blood
                        p.type = draw.ptype_t.pt_grav;
				        p.color = 67 + (helper.rand()&3);
				        for (j=0 ; j<3 ; j++)
                            p.org[j] = start[j] + ((helper.rand() % 6) - 3);
				        break;

			        case 3:
			        case 5:	// tracer
                        p.die = client.cl.time + 0.5;
                        p.type = draw.ptype_t.pt_static;
				        if (type == 3)
					        p.color = 52 + ((tracercount&4)<<1);
				        else
					        p.color = 230 + ((tracercount&4)<<1);
        			
				        tracercount++;

				        mathlib.VectorCopy (start, ref p.org);
				        if ((tracercount & 1) != 0)
				        {
					        p.vel[0] = 30*vec[1];
					        p.vel[1] = 30*-vec[0];
				        }
				        else
				        {
					        p.vel[0] = 30*-vec[1];
					        p.vel[1] = 30*vec[0];
				        }
				        break;

			        case 4:	// slight blood
                        p.type = draw.ptype_t.pt_grav;
                        p.color = 67 + (helper.rand() & 3);
				        for (j=0 ; j<3 ; j++)
                            p.org[j] = start[j] + ((helper.rand() % 6) - 3);
				        len -= 3;
				        break;

			        case 6:	// voor trail
                        p.color = 9 * 16 + 8 + (helper.rand() & 3);
                        p.type = draw.ptype_t.pt_static;
                        p.die = client.cl.time + 0.3;
				        for (j=0 ; j<3 ; j++)
					        p.org[j] = start[j] + ((helper.rand()&15)-8);
				        break;
		        }
        		

		        mathlib.VectorAdd (start, vec, ref start);
	        }
        }
        	
        /*
        ===============
        R_DrawParticles
        ===============
        */
        static void R_DrawParticles ()
        {
	        draw.particle_t	p, kill;
	        double			grav;
	        int				i;
	        double			time2, time3;
	        double			time1;
	        double			dvel;
	        double			frametime;
        	
	        mathlib.VectorScale (vright, xscaleshrink, ref r_pright);
	        mathlib.VectorScale (vup, yscaleshrink, ref r_pup);
            mathlib.VectorCopy(vpn, ref r_ppn);
	        frametime = client.cl.time - client.cl.oldtime;
	        time3 = frametime * 15;
	        time2 = frametime * 10; // 15;
	        time1 = frametime * 5;
            grav = frametime * server.sv_gravity.value * 0.05;
	        dvel = 4*frametime;
        	
	        for ( ;; ) 
	        {
		        kill = active_particles;
		        if (kill != null && kill.die < client.cl.time)
		        {
			        active_particles = kill.next;
			        kill.next = free_particles;
			        free_particles = kill;
			        continue;
		        }
		        break;
	        }

	        for (p=active_particles ; p != null ; p=p.next)
	        {
		        for ( ;; )
		        {
			        kill = p.next;
			        if (kill != null && kill.die < client.cl.time)
			        {
				        p.next = kill.next;
				        kill.next = free_particles;
				        free_particles = kill;
				        continue;
			        }
			        break;
		        }

		        draw.D_DrawParticle (p);
		        p.org[0] += p.vel[0]*frametime;
		        p.org[1] += p.vel[1]*frametime;
		        p.org[2] += p.vel[2]*frametime;

		        switch (p.type)
		        {
                case draw.ptype_t.pt_static:
			        break;
                case draw.ptype_t.pt_fire:
			        p.ramp += time1;
			        if (p.ramp >= 6)
				        p.die = -1;
			        else
				        p.color = ramp3[(int)p.ramp];
			        p.vel[2] += grav;
			        break;

                case draw.ptype_t.pt_explode:
			        p.ramp += time2;
			        if (p.ramp >=8)
				        p.die = -1;
			        else
				        p.color = ramp1[(int)p.ramp];
			        for (i=0 ; i<3 ; i++)
				        p.vel[i] += p.vel[i]*dvel;
			        p.vel[2] -= grav;
			        break;

                case draw.ptype_t.pt_explode2:
			        p.ramp += time3;
			        if (p.ramp >=8)
				        p.die = -1;
			        else
				        p.color = ramp2[(int)p.ramp];
			        for (i=0 ; i<3 ; i++)
				        p.vel[i] -= p.vel[i]*frametime;
			        p.vel[2] -= grav;
			        break;

                case draw.ptype_t.pt_blob:
			        for (i=0 ; i<3 ; i++)
				        p.vel[i] += p.vel[i]*dvel;
			        p.vel[2] -= grav;
			        break;

                case draw.ptype_t.pt_blob2:
			        for (i=0 ; i<2 ; i++)
				        p.vel[i] -= p.vel[i]*dvel;
			        p.vel[2] -= grav;
			        break;

                case draw.ptype_t.pt_grav:
                case draw.ptype_t.pt_slowgrav:
			        p.vel[2] -= grav;
			        break;
		        }
	        }
        }
    }
}
