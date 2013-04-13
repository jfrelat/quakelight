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
// r_aclip.c: clip routines for drawing Alias models directly to the screen

namespace quake
{
    public partial class render
    {
        static draw.finalvert_t[][]	fva = { new draw.finalvert_t[8], new draw.finalvert_t[8] };
        static auxvert_t[]          av = new auxvert_t[8];
        static int                  av0, av1;

        delegate void clip (draw.finalvert_t pfv0, draw.finalvert_t pfv1, ref draw.finalvert_t @out);

        /*
        ================
        R_Alias_clip_z

        pfv0 is the unclipped vertex, pfv1 is the z-clipped vertex
        ================
        */
        static void R_Alias_clip_z (draw.finalvert_t pfv0, draw.finalvert_t pfv1, ref draw.finalvert_t @out)
        {
	        double		scale;
	        auxvert_t	pav0, pav1, avout = new auxvert_t();

	        pav0 = av[av0];
	        pav1 = av[av1];

	        if (pfv0.v[1] >= pfv1.v[1])
	        {
		        scale = (ALIAS_Z_CLIP_PLANE - pav0.fv[2]) /
				        (pav1.fv[2] - pav0.fv[2]);
        	
		        avout.fv[0] = pav0.fv[0] + (pav1.fv[0] - pav0.fv[0]) * scale;
		        avout.fv[1] = pav0.fv[1] + (pav1.fv[1] - pav0.fv[1]) * scale;
		        avout.fv[2] = ALIAS_Z_CLIP_PLANE;
        	
		        @out.v[2] =	(int)(pfv0.v[2] + (pfv1.v[2] - pfv0.v[2]) * scale);
		        @out.v[3] =	(int)(pfv0.v[3] + (pfv1.v[3] - pfv0.v[3]) * scale);
		        @out.v[4] =	(int)(pfv0.v[4] + (pfv1.v[4] - pfv0.v[4]) * scale);
	        }
	        else
	        {
		        scale = (ALIAS_Z_CLIP_PLANE - pav1.fv[2]) /
				        (pav0.fv[2] - pav1.fv[2]);
        	
		        avout.fv[0] = pav1.fv[0] + (pav0.fv[0] - pav1.fv[0]) * scale;
		        avout.fv[1] = pav1.fv[1] + (pav0.fv[1] - pav1.fv[1]) * scale;
		        avout.fv[2] = ALIAS_Z_CLIP_PLANE;
        	
		        @out.v[2] =	(int)(pfv1.v[2] + (pfv0.v[2] - pfv1.v[2]) * scale);
		        @out.v[3] =	(int)(pfv1.v[3] + (pfv0.v[3] - pfv1.v[3]) * scale);
		        @out.v[4] =	(int)(pfv1.v[4] + (pfv0.v[4] - pfv1.v[4]) * scale);
	        }

	        R_AliasProjectFinalVert (@out, avout);

	        if (@out.v[0] < r_refdef.aliasvrect.x)
		        @out.flags |= ALIAS_LEFT_CLIP;
	        if (@out.v[1] < r_refdef.aliasvrect.y)
		        @out.flags |= ALIAS_TOP_CLIP;
	        if (@out.v[0] > r_refdef.aliasvrectright)
		        @out.flags |= ALIAS_RIGHT_CLIP;
            if (@out.v[1] > r_refdef.aliasvrectbottom)
                @out.flags |= ALIAS_BOTTOM_CLIP;
        }

        static void R_Alias_clip_left (draw.finalvert_t pfv0, draw.finalvert_t pfv1, ref draw.finalvert_t @out)
        {
	        double		scale;
	        int			i;

	        if (pfv0.v[1] >= pfv1.v[1])
	        {
		        scale = (double)(r_refdef.aliasvrect.x - pfv0.v[0]) /
				        (pfv1.v[0] - pfv0.v[0]);
		        for (i=0 ; i<6 ; i++)
			        @out.v[i] = (int)(pfv0.v[i] + (pfv1.v[i] - pfv0.v[i])*scale + 0.5);
	        }
	        else
	        {
		        scale = (double)(r_refdef.aliasvrect.x - pfv1.v[0]) /
				        (pfv0.v[0] - pfv1.v[0]);
		        for (i=0 ; i<6 ; i++)
                    @out.v[i] = (int)(pfv1.v[i] + (pfv0.v[i] - pfv1.v[i]) * scale + 0.5);
	        }
        }
        
        static void R_Alias_clip_right (draw.finalvert_t pfv0, draw.finalvert_t pfv1, ref draw.finalvert_t @out)
        {
	        double		scale;
	        int			i;

	        if (pfv0.v[1] >= pfv1.v[1])
	        {
		        scale = (double)(r_refdef.aliasvrectright - pfv0.v[0]) /
				        (pfv1.v[0] - pfv0.v[0]);
		        for (i=0 ; i<6 ; i++)
			        @out.v[i] = (int)(pfv0.v[i] + (pfv1.v[i] - pfv0.v[i])*scale + 0.5);
	        }
	        else
	        {
		        scale = (double)(r_refdef.aliasvrectright - pfv1.v[0]) /
				        (pfv0.v[0] - pfv1.v[0]);
		        for (i=0 ; i<6 ; i++)
                    @out.v[i] = (int)(pfv1.v[i] + (pfv0.v[i] - pfv1.v[i]) * scale + 0.5);
	        }
        }
        
        static void R_Alias_clip_top (draw.finalvert_t pfv0, draw.finalvert_t pfv1, ref draw.finalvert_t @out)
        {
	        double		scale;
	        int			i;

	        if (pfv0.v[1] >= pfv1.v[1])
	        {
		        scale = (double)(r_refdef.aliasvrect.y - pfv0.v[1]) /
				        (pfv1.v[1] - pfv0.v[1]);
		        for (i=0 ; i<6 ; i++)
                    @out.v[i] = (int)(pfv0.v[i] + (pfv1.v[i] - pfv0.v[i]) * scale + 0.5);
	        }
	        else
	        {
		        scale = (double)(r_refdef.aliasvrect.y - pfv1.v[1]) /
				        (pfv0.v[1] - pfv1.v[1]);
		        for (i=0 ; i<6 ; i++)
			        @out.v[i] = (int)(pfv1.v[i] + (pfv0.v[i] - pfv1.v[i])*scale + 0.5);
	        }
        }

        static void R_Alias_clip_bottom (draw.finalvert_t pfv0, draw.finalvert_t pfv1, ref draw.finalvert_t @out)
        {
	        double		scale;
	        int			i;

	        if (pfv0.v[1] >= pfv1.v[1])
	        {
		        scale = (double)(r_refdef.aliasvrectbottom - pfv0.v[1]) /
				        (pfv1.v[1] - pfv0.v[1]);

		        for (i=0 ; i<6 ; i++)
			        @out.v[i] = (int)(pfv0.v[i] + (pfv1.v[i] - pfv0.v[i])*scale + 0.5);
	        }
	        else
	        {
		        scale = (double)(r_refdef.aliasvrectbottom - pfv1.v[1]) /
				        (pfv0.v[1] - pfv1.v[1]);

		        for (i=0 ; i<6 ; i++)
                    @out.v[i] = (int)(pfv1.v[i] + (pfv0.v[i] - pfv1.v[i]) * scale + 0.5);
	        }
        }

        static int R_AliasClip(draw.finalvert_t[] @in, draw.finalvert_t[] @out, int flag, int count, clip delegate_clip)
        {
	        int			i,j,k;
	        int			flags, oldflags;
        	
	        j = count-1;
	        k = 0;
	        for (i=0 ; i<count ; j = i, i++)
	        {
		        oldflags = @in[j].flags & flag;
		        flags = @in[i].flags & flag;

		        if (flags != 0 && oldflags != 0)
			        continue;
		        if ((oldflags ^ flags) != 0)
		        {
                    av0 = j;
                    av1 = i;
                    delegate_clip (@in[j], @in[i], ref @out[k]);
			        @out[k].flags = 0;
			        if (@out[k].v[0] < r_refdef.aliasvrect.x)
				        @out[k].flags |= ALIAS_LEFT_CLIP;
			        if (@out[k].v[1] < r_refdef.aliasvrect.y)
				        @out[k].flags |= ALIAS_TOP_CLIP;
			        if (@out[k].v[0] > r_refdef.aliasvrectright)
				        @out[k].flags |= ALIAS_RIGHT_CLIP;
			        if (@out[k].v[1] > r_refdef.aliasvrectbottom)
				        @out[k].flags |= ALIAS_BOTTOM_CLIP;
			        k++;
		        }
		        if (flags == 0)
		        {
                    draw.finalvert_t.Copy(@in[i], @out[k]);
//                    @out[k] = @in[i];
			        k++;
		        }
	        }
        	
	        return k;
        }

        /*
        ================
        R_AliasClipTriangle
        ================
        */
        static void R_AliasClipTriangle (model.mtriangle_t ptri)
        {
	        int				    i, k, pingpong;
	        model.mtriangle_t   mtri = new model.mtriangle_t();
	        uint		        clipflags;

        // copy vertexes and fix seam texture coordinates
	        if (ptri.facesfront != 0)
	        {
		        draw.finalvert_t.Copy(pfinalverts[ptri.vertindex[0]], fva[0][0]);
                draw.finalvert_t.Copy(pfinalverts[ptri.vertindex[1]], fva[0][1]);
                draw.finalvert_t.Copy(pfinalverts[ptri.vertindex[2]], fva[0][2]);
	        }
	        else
	        {
		        for (i=0 ; i<3 ; i++)
		        {
                    draw.finalvert_t.Copy(pfinalverts[ptri.vertindex[i]], fva[0][i]);
        	
			        if (ptri.facesfront == 0 && (fva[0][i].flags & ALIAS_ONSEAM) != 0 )
				        fva[0][i].v[2] += r_affinetridesc.seamfixupX16;
		        }
	        }

        // clip
	        clipflags = (uint)(fva[0][0].flags | fva[0][1].flags | fva[0][2].flags);

	        if ((clipflags & ALIAS_Z_CLIP) != 0)
	        {
		        for (i=0 ; i<3 ; i++)
			        av[i] = pauxverts[ptri.vertindex[i]];

		        k = R_AliasClip (fva[0], fva[1], ALIAS_Z_CLIP, 3, R_Alias_clip_z);
		        if (k == 0)
			        return;

		        pingpong = 1;
		        clipflags = (uint)(fva[1][0].flags | fva[1][1].flags | fva[1][2].flags);
	        }
	        else
	        {
		        pingpong = 0;
		        k = 3;
	        }

	        if ((clipflags & ALIAS_LEFT_CLIP) != 0)
	        {
		        k = R_AliasClip (fva[pingpong], fva[pingpong ^ 1],
							        ALIAS_LEFT_CLIP, k, R_Alias_clip_left);
		        if (k == 0)
			        return;

		        pingpong ^= 1;
	        }

	        if ((clipflags & ALIAS_RIGHT_CLIP) != 0)
	        {
		        k = R_AliasClip (fva[pingpong], fva[pingpong ^ 1],
							        ALIAS_RIGHT_CLIP, k, R_Alias_clip_right);
		        if (k == 0)
			        return;

		        pingpong ^= 1;
	        }

	        if ((clipflags & ALIAS_BOTTOM_CLIP) != 0)
	        {
		        k = R_AliasClip (fva[pingpong], fva[pingpong ^ 1],
							        ALIAS_BOTTOM_CLIP, k, R_Alias_clip_bottom);
		        if (k == 0)
			        return;

		        pingpong ^= 1;
	        }

	        if ((clipflags & ALIAS_TOP_CLIP) != 0)
	        {
		        k = R_AliasClip (fva[pingpong], fva[pingpong ^ 1],
							        ALIAS_TOP_CLIP, k, R_Alias_clip_top);
		        if (k == 0)
			        return;

		        pingpong ^= 1;
	        }

	        for (i=0 ; i<k ; i++)
	        {
		        if (fva[pingpong][i].v[0] < r_refdef.aliasvrect.x)
			        fva[pingpong][i].v[0] = r_refdef.aliasvrect.x;
		        else if (fva[pingpong][i].v[0] > r_refdef.aliasvrectright)
			        fva[pingpong][i].v[0] = r_refdef.aliasvrectright;

		        if (fva[pingpong][i].v[1] < r_refdef.aliasvrect.y)
			        fva[pingpong][i].v[1] = r_refdef.aliasvrect.y;
		        else if (fva[pingpong][i].v[1] > r_refdef.aliasvrectbottom)
			        fva[pingpong][i].v[1] = r_refdef.aliasvrectbottom;

		        fva[pingpong][i].flags = 0;
	        }

        // draw triangles
	        mtri.facesfront = ptri.facesfront;
	        r_affinetridesc.ptriangles = new model.mtriangle_t[] { mtri };
	        r_affinetridesc.pfinalverts = fva[pingpong];

        // FIXME: do all at once as trifan?
	        mtri.vertindex[0] = 0;
	        for (i=1 ; i<k-1 ; i++)
	        {
		        mtri.vertindex[1] = i;
		        mtri.vertindex[2] = i+1;
		        draw.D_PolysetDraw ();
	        }
        }
    }
}
