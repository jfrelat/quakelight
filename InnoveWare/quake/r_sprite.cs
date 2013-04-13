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
// r_sprite.c

namespace quake
{
    public partial class render
    {
        static int				    clip_current;
        static double[][][]         clip_verts = { new double[MAXWORKINGVERTS][], new double[MAXWORKINGVERTS][] };
        static int				    sprite_width, sprite_height;

        public static draw.spritedesc_t r_spritedesc = new draw.spritedesc_t();

        /*
        ================
        R_RotateSprite
        ================
        */
        static void R_RotateSprite (double beamlength)
        {
            double[] vec = new double[3];

            if (beamlength == 0.0)
                return;

            mathlib.VectorScale(r_spritedesc.vpn, -beamlength, ref vec);
            mathlib.VectorAdd(r_entorigin, vec, ref r_entorigin);
            mathlib.VectorSubtract(modelorg, vec, ref modelorg);
        }

        /*
        =============
        R_ClipSpriteFace

        Clips the winding at clip_verts[clip_current] and changes clip_current
        Throws out the back side
        ==============
        */
        static int R_ClipSpriteFace (int nump, clipplane_t pclipplane)
        {
	        int		    i, outcount;
	        double[]	dists = new double[MAXWORKINGVERTS+1];
	        double	    frac, clipdist;
            double[]    pclipnormal;
	        double[][]	@in;
            double[][]  @out;
            double[]    instep, vert2;
            int         outstep;

	        clipdist = pclipplane.dist;
	        pclipnormal = pclipplane.normal;

        // calc dists
	        if (clip_current != 0)
	        {
		        @in = clip_verts[1];
		        @out = clip_verts[0];
		        clip_current = 0;
	        }
	        else
	        {
		        @in = clip_verts[0];
		        @out = clip_verts[1];
		        clip_current = 1;
	        }

	        for (i=0 ; i<nump ; i++)
	        {
                instep = @in[i];
                dists[i] = mathlib.DotProduct(instep, pclipnormal) - clipdist;
            }

        // handle wraparound case
	        dists[nump] = dists[0];
            for(int kk = 0; kk < 5; kk++) @in[nump][kk] = @in[0][kk];

        // clip the winding
            outstep = 0;
	        outcount = 0;

	        for (i=0 ; i<nump ; i++)
	        {
                instep = @in[i];
		        if (dists[i] >= 0)
		        {
                    for (int kk = 0; kk < 5; kk++) @out[outstep][kk] = instep[kk];
			        outstep += 1;
			        outcount++;
		        }

		        if (dists[i] == 0 || dists[i+1] == 0)
			        continue;

		        if ( (dists[i] > 0) == (dists[i+1] > 0) )
			        continue;
        			
	        // split it into a new vertex
		        frac = dists[i] / (dists[i] - dists[i+1]);

                vert2 = @in[i + 1];
        		
		        @out[outstep][0] = instep[0] + frac * (vert2[0] - instep[0]);
		        @out[outstep][1] = instep[1] + frac * (vert2[1] - instep[1]);
		        @out[outstep][2] = instep[2] + frac * (vert2[2] - instep[2]);
		        @out[outstep][3] = instep[3] + frac * (vert2[3] - instep[3]);
                @out[outstep][4] = instep[4] + frac * (vert2[4] - instep[4]);

		        outstep += 1;
		        outcount++;
	        }

	        return outcount;
        }
        
        /*
        ================
        R_SetupAndDrawSprite
        ================
        */
        static void R_SetupAndDrawSprite ()
        {
	        int			        i, nump;
	        double		        dot, scale;
            double[]            pv;
        	double[][]	        pverts;
            double[]            left = new double[3], up = new double[3], right = new double[3], down = new double[3], transformed = new double[3], local = new double[3];
	        draw.emitpoint_t[]	outverts = new draw.emitpoint_t[MAXWORKINGVERTS+1];

            for (int kk = 0; kk < MAXWORKINGVERTS + 1; kk++) outverts[kk] = new draw.emitpoint_t();
	        dot = mathlib.DotProduct (r_spritedesc.vpn, modelorg);

        // backface cull
	        if (dot >= 0)
		        return;

        // build the sprite poster in worldspace
	        mathlib.VectorScale (r_spritedesc.vright, r_spritedesc.pspriteframe.right, ref right);
            mathlib.VectorScale (r_spritedesc.vup, r_spritedesc.pspriteframe.up, ref up);
            mathlib.VectorScale (r_spritedesc.vright, r_spritedesc.pspriteframe.left, ref left);
            mathlib.VectorScale (r_spritedesc.vup, r_spritedesc.pspriteframe.down, ref down);

        	pverts = clip_verts[0];

	        pverts[0][0] = r_entorigin[0] + up[0] + left[0];
	        pverts[0][1] = r_entorigin[1] + up[1] + left[1];
	        pverts[0][2] = r_entorigin[2] + up[2] + left[2];
	        pverts[0][3] = 0;
	        pverts[0][4] = 0;

	        pverts[1][0] = r_entorigin[0] + up[0] + right[0];
	        pverts[1][1] = r_entorigin[1] + up[1] + right[1];
	        pverts[1][2] = r_entorigin[2] + up[2] + right[2];
	        pverts[1][3] = sprite_width;
	        pverts[1][4] = 0;

	        pverts[2][0] = r_entorigin[0] + down[0] + right[0];
	        pverts[2][1] = r_entorigin[1] + down[1] + right[1];
	        pverts[2][2] = r_entorigin[2] + down[2] + right[2];
	        pverts[2][3] = sprite_width;
	        pverts[2][4] = sprite_height;

	        pverts[3][0] = r_entorigin[0] + down[0] + left[0];
	        pverts[3][1] = r_entorigin[1] + down[1] + left[1];
	        pverts[3][2] = r_entorigin[2] + down[2] + left[2];
	        pverts[3][3] = 0;
	        pverts[3][4] = sprite_height;

        // clip to the frustum in worldspace
	        nump = 4;
	        clip_current = 0;

	        for (i=0 ; i<4 ; i++)
	        {
		        nump = R_ClipSpriteFace (nump, view_clipplanes[i]);
		        if (nump < 3)
			        return;
		        if (nump >= MAXWORKINGVERTS)
			        sys_linux.Sys_Error("R_SetupAndDrawSprite: too many points");
	        }

        // transform vertices into viewspace and project
	        r_spritedesc.nearzi = -999999;

	        for (i=0 ; i<nump ; i++)
	        {
                pv = clip_verts[clip_current][i];
		        mathlib.VectorSubtract (pv, r_origin, ref local);
		        render.TransformVector (local, ref transformed);

		        if (transformed[2] < NEAR_CLIP)
			        transformed[2] = NEAR_CLIP;

                outverts[i].zi = 1.0 / transformed[2];
                if (outverts[i].zi > r_spritedesc.nearzi)
                    r_spritedesc.nearzi = outverts[i].zi;

                outverts[i].s = pv[3];
                outverts[i].t = pv[4];

                scale = xscale * outverts[i].zi;
                outverts[i].u = (xcenter + scale * transformed[0]);

                scale = yscale * outverts[i].zi;
                outverts[i].v = (ycenter - scale * transformed[1]);
	        }

        // draw it
	        r_spritedesc.nump = nump;
	        r_spritedesc.pverts = outverts;

            draw.D_DrawSprite ();
        }

        /*
        ================
        R_GetSpriteframe
        ================
        */
        static model.mspriteframe_t R_GetSpriteframe(model.msprite_t psprite)
        {
            model.mspritegroup_t    pspritegroup;
            model.mspriteframe_t    pspriteframe;
            int                     i, numframes, frame;
            //double* pintervals, fullinterval, targettime, time;

            frame = currententity.frame;

            if ((frame >= psprite.numframes) || (frame < 0))
            {
                console.Con_Printf("R_DrawSprite: no such frame " + frame + "\n");
                frame = 0;
            }

            if (psprite.frames[frame].type == model.spriteframetype_t.SPR_SINGLE)
            {
                pspriteframe = (model.mspriteframe_t)psprite.frames[frame].frameptr;
            }
            else
            {
                pspriteframe = null;
            }

            return pspriteframe;
        }

        /*
        ================
        R_DrawSprite
        ================
        */
        static void R_DrawSprite ()
        {
            int             i;
            model.msprite_t psprite;
            double[]        tvec = new double[3];
            double          dot, angle, sr, cr;

            psprite = (model.msprite_t)currententity.model.cache;

            r_spritedesc.pspriteframe = R_GetSpriteframe(psprite);

            if (r_spritedesc.pspriteframe == null)
                return;
            sprite_width = r_spritedesc.pspriteframe.width;
            sprite_height = r_spritedesc.pspriteframe.height;

        // TODO: make this caller-selectable
	        if (psprite.type == model.SPR_FACING_UPRIGHT)
	        {
	        // generate the sprite's axes, with vup straight up in worldspace, and
	        // r_spritedesc.vright perpendicular to modelorg.
	        // This will not work if the view direction is very close to straight up or
	        // down, because the cross product will be between two nearly parallel
	        // vectors and starts to approach an undefined state, so we don't draw if
	        // the two vectors are less than 1 degree apart
		        tvec[0] = -modelorg[0];
		        tvec[1] = -modelorg[1];
		        tvec[2] = -modelorg[2];
		        mathlib.VectorNormalize (ref tvec);
		        dot = tvec[2];	// same as DotProduct (tvec, r_spritedesc.vup) because
						        //  r_spritedesc.vup is 0, 0, 1
		        if ((dot > 0.999848) || (dot < -0.999848))	// cos(1 degree) = 0.999848
			        return;
		        r_spritedesc.vup[0] = 0;
		        r_spritedesc.vup[1] = 0;
		        r_spritedesc.vup[2] = 1;
		        r_spritedesc.vright[0] = tvec[1];
								        // CrossProduct(r_spritedesc.vup, -modelorg,
		        r_spritedesc.vright[1] = -tvec[0];
								        //              r_spritedesc.vright)
		        r_spritedesc.vright[2] = 0;
		        mathlib.VectorNormalize (ref r_spritedesc.vright);
		        r_spritedesc.vpn[0] = -r_spritedesc.vright[1];
		        r_spritedesc.vpn[1] = r_spritedesc.vright[0];
		        r_spritedesc.vpn[2] = 0;
					        // CrossProduct (r_spritedesc.vright, r_spritedesc.vup,
					        //  r_spritedesc.vpn)
	        }
            else if (psprite.type == model.SPR_VP_PARALLEL)
	        {
	        // generate the sprite's axes, completely parallel to the viewplane. There
	        // are no problem situations, because the sprite is always in the same
	        // position relative to the viewer
		        for (i=0 ; i<3 ; i++)
		        {
			        r_spritedesc.vup[i] = vup[i];
			        r_spritedesc.vright[i] = vright[i];
			        r_spritedesc.vpn[i] = vpn[i];
		        }
	        }
            else if (psprite.type == model.SPR_VP_PARALLEL_UPRIGHT)
	        {
	        // generate the sprite's axes, with vup straight up in worldspace, and
	        // r_spritedesc.vright parallel to the viewplane.
	        // This will not work if the view direction is very close to straight up or
	        // down, because the cross product will be between two nearly parallel
	        // vectors and starts to approach an undefined state, so we don't draw if
	        // the two vectors are less than 1 degree apart
		        dot = vpn[2];	// same as DotProduct (vpn, r_spritedesc.vup) because
						        //  r_spritedesc.vup is 0, 0, 1
		        if ((dot > 0.999848) || (dot < -0.999848))	// cos(1 degree) = 0.999848
			        return;
		        r_spritedesc.vup[0] = 0;
		        r_spritedesc.vup[1] = 0;
		        r_spritedesc.vup[2] = 1;
		        r_spritedesc.vright[0] = vpn[1];
										        // CrossProduct (r_spritedesc.vup, vpn,
		        r_spritedesc.vright[1] = -vpn[0];	//  r_spritedesc.vright)
		        r_spritedesc.vright[2] = 0;
                mathlib.VectorNormalize(ref r_spritedesc.vright);
		        r_spritedesc.vpn[0] = -r_spritedesc.vright[1];
		        r_spritedesc.vpn[1] = r_spritedesc.vright[0];
		        r_spritedesc.vpn[2] = 0;
					        // CrossProduct (r_spritedesc.vright, r_spritedesc.vup,
					        //  r_spritedesc.vpn)
	        }
            else if (psprite.type == model.SPR_ORIENTED)
	        {
	        // generate the sprite's axes, according to the sprite's world orientation
                mathlib.AngleVectors(currententity.angles, ref r_spritedesc.vpn,
					          ref r_spritedesc.vright, ref r_spritedesc.vup);
	        }
            else if (psprite.type == model.SPR_VP_PARALLEL_ORIENTED)
	        {
	        // generate the sprite's axes, parallel to the viewplane, but rotated in
	        // that plane around the center according to the sprite entity's roll
	        // angle. So vpn stays the same, but vright and vup rotate
		        angle = currententity.angles[quakedef.ROLL] * (mathlib.M_PI*2 / 360);
                sr = Math.Sin(angle);
                cr = Math.Cos(angle);

		        for (i=0 ; i<3 ; i++)
		        {
			        r_spritedesc.vpn[i] = vpn[i];
			        r_spritedesc.vright[i] = vright[i] * cr + vup[i] * sr;
			        r_spritedesc.vup[i] = vright[i] * -sr + vup[i] * cr;
		        }
	        }
	        else
	        {
		        sys_linux.Sys_Error ("R_DrawSprite: Bad sprite type " + psprite.type);
	        }

            R_RotateSprite(psprite.beamlength);

            R_SetupAndDrawSprite();
        }
    }
}
