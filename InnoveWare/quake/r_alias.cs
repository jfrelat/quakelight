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
// r_alias.c: routines for setting up to draw alias models

namespace quake
{
    public partial class render
    {
        public const int LIGHT_MIN	= 5;		// lowest light value we'll allow, to avoid the
							                    //  need for inner-loop light clamping

        public static draw.affinetridesc_t     r_affinetridesc = new draw.affinetridesc_t();

        public static byte[]                   acolormap;	// FIXME: should go away

        static model.trivertx_t[]       r_apverts;

        // TODO: these probably will go away with optimized rasterization
        static model.mdl_t		        pmdl;
        static double[]				    r_plightvec = new double[3];
        static int					    r_ambientlight;
        static double				    r_shadelight;
        static model.aliashdr_t	        paliashdr;
        static draw.finalvert_t[]		pfinalverts;
        static auxvert_t[]			    pauxverts;
        static double		            ziscale;
        static model.model_t	        pmodel;

        static double[]                 alias_forward = new double[3], alias_right = new double[3], alias_up = new double[3];

        static model.maliasskindesc_t	pskindesc;

        static int				        r_amodels_drawn;
        static int				        a_skinwidth;
        static int				        r_anumverts;

        static double[][]	            aliastransform = { new double[4], new double[4], new double[4] };

        public class aedge_t {
	        public int	index0;
            public int  index1;

            public aedge_t(int index0, int index1)
            {
                this.index0 = index0;
                this.index1 = index1;
            }
        };

        static aedge_t[]	            aedges = {
            new aedge_t(0, 1), new aedge_t(1, 2), new aedge_t(2, 3), new aedge_t(3, 0),
            new aedge_t(4, 5), new aedge_t(5, 6), new aedge_t(6, 7), new aedge_t(7, 4),
            new aedge_t(0, 5), new aedge_t(1, 4), new aedge_t(2, 7), new aedge_t(3, 6)
        };

        public const int NUMVERTEXNORMALS	= 162;

        static double[][]	            r_avertexnormals = {
            new double[] {-0.525731, 0.000000, 0.850651}, 
            new double[] {-0.442863, 0.238856, 0.864188}, 
            new double[] {-0.295242, 0.000000, 0.955423}, 
            new double[] {-0.309017, 0.500000, 0.809017}, 
            new double[] {-0.162460, 0.262866, 0.951056}, 
            new double[] {0.000000, 0.000000, 1.000000}, 
            new double[] {0.000000, 0.850651, 0.525731}, 
            new double[] {-0.147621, 0.716567, 0.681718}, 
            new double[] {0.147621, 0.716567, 0.681718}, 
            new double[] {0.000000, 0.525731, 0.850651}, 
            new double[] {0.309017, 0.500000, 0.809017}, 
            new double[] {0.525731, 0.000000, 0.850651}, 
            new double[] {0.295242, 0.000000, 0.955423}, 
            new double[] {0.442863, 0.238856, 0.864188}, 
            new double[] {0.162460, 0.262866, 0.951056}, 
            new double[] {-0.681718, 0.147621, 0.716567}, 
            new double[] {-0.809017, 0.309017, 0.500000}, 
            new double[] {-0.587785, 0.425325, 0.688191}, 
            new double[] {-0.850651, 0.525731, 0.000000}, 
            new double[] {-0.864188, 0.442863, 0.238856}, 
            new double[] {-0.716567, 0.681718, 0.147621}, 
            new double[] {-0.688191, 0.587785, 0.425325}, 
            new double[] {-0.500000, 0.809017, 0.309017}, 
            new double[] {-0.238856, 0.864188, 0.442863}, 
            new double[] {-0.425325, 0.688191, 0.587785}, 
            new double[] {-0.716567, 0.681718, -0.147621}, 
            new double[] {-0.500000, 0.809017, -0.309017}, 
            new double[] {-0.525731, 0.850651, 0.000000}, 
            new double[] {0.000000, 0.850651, -0.525731}, 
            new double[] {-0.238856, 0.864188, -0.442863}, 
            new double[] {0.000000, 0.955423, -0.295242}, 
            new double[] {-0.262866, 0.951056, -0.162460}, 
            new double[] {0.000000, 1.000000, 0.000000}, 
            new double[] {0.000000, 0.955423, 0.295242}, 
            new double[] {-0.262866, 0.951056, 0.162460}, 
            new double[] {0.238856, 0.864188, 0.442863}, 
            new double[] {0.262866, 0.951056, 0.162460}, 
            new double[] {0.500000, 0.809017, 0.309017}, 
            new double[] {0.238856, 0.864188, -0.442863}, 
            new double[] {0.262866, 0.951056, -0.162460}, 
            new double[] {0.500000, 0.809017, -0.309017}, 
            new double[] {0.850651, 0.525731, 0.000000}, 
            new double[] {0.716567, 0.681718, 0.147621}, 
            new double[] {0.716567, 0.681718, -0.147621}, 
            new double[] {0.525731, 0.850651, 0.000000}, 
            new double[] {0.425325, 0.688191, 0.587785}, 
            new double[] {0.864188, 0.442863, 0.238856}, 
            new double[] {0.688191, 0.587785, 0.425325}, 
            new double[] {0.809017, 0.309017, 0.500000}, 
            new double[] {0.681718, 0.147621, 0.716567}, 
            new double[] {0.587785, 0.425325, 0.688191}, 
            new double[] {0.955423, 0.295242, 0.000000}, 
            new double[] {1.000000, 0.000000, 0.000000}, 
            new double[] {0.951056, 0.162460, 0.262866}, 
            new double[] {0.850651, -0.525731, 0.000000}, 
            new double[] {0.955423, -0.295242, 0.000000}, 
            new double[] {0.864188, -0.442863, 0.238856}, 
            new double[] {0.951056, -0.162460, 0.262866}, 
            new double[] {0.809017, -0.309017, 0.500000}, 
            new double[] {0.681718, -0.147621, 0.716567}, 
            new double[] {0.850651, 0.000000, 0.525731}, 
            new double[] {0.864188, 0.442863, -0.238856}, 
            new double[] {0.809017, 0.309017, -0.500000}, 
            new double[] {0.951056, 0.162460, -0.262866}, 
            new double[] {0.525731, 0.000000, -0.850651}, 
            new double[] {0.681718, 0.147621, -0.716567}, 
            new double[] {0.681718, -0.147621, -0.716567}, 
            new double[] {0.850651, 0.000000, -0.525731}, 
            new double[] {0.809017, -0.309017, -0.500000}, 
            new double[] {0.864188, -0.442863, -0.238856}, 
            new double[] {0.951056, -0.162460, -0.262866}, 
            new double[] {0.147621, 0.716567, -0.681718}, 
            new double[] {0.309017, 0.500000, -0.809017}, 
            new double[] {0.425325, 0.688191, -0.587785}, 
            new double[] {0.442863, 0.238856, -0.864188}, 
            new double[] {0.587785, 0.425325, -0.688191}, 
            new double[] {0.688191, 0.587785, -0.425325}, 
            new double[] {-0.147621, 0.716567, -0.681718}, 
            new double[] {-0.309017, 0.500000, -0.809017}, 
            new double[] {0.000000, 0.525731, -0.850651}, 
            new double[] {-0.525731, 0.000000, -0.850651}, 
            new double[] {-0.442863, 0.238856, -0.864188}, 
            new double[] {-0.295242, 0.000000, -0.955423}, 
            new double[] {-0.162460, 0.262866, -0.951056}, 
            new double[] {0.000000, 0.000000, -1.000000}, 
            new double[] {0.295242, 0.000000, -0.955423}, 
            new double[] {0.162460, 0.262866, -0.951056}, 
            new double[] {-0.442863, -0.238856, -0.864188}, 
            new double[] {-0.309017, -0.500000, -0.809017}, 
            new double[] {-0.162460, -0.262866, -0.951056}, 
            new double[] {0.000000, -0.850651, -0.525731}, 
            new double[] {-0.147621, -0.716567, -0.681718}, 
            new double[] {0.147621, -0.716567, -0.681718}, 
            new double[] {0.000000, -0.525731, -0.850651}, 
            new double[] {0.309017, -0.500000, -0.809017}, 
            new double[] {0.442863, -0.238856, -0.864188}, 
            new double[] {0.162460, -0.262866, -0.951056}, 
            new double[] {0.238856, -0.864188, -0.442863}, 
            new double[] {0.500000, -0.809017, -0.309017}, 
            new double[] {0.425325, -0.688191, -0.587785}, 
            new double[] {0.716567, -0.681718, -0.147621}, 
            new double[] {0.688191, -0.587785, -0.425325}, 
            new double[] {0.587785, -0.425325, -0.688191}, 
            new double[] {0.000000, -0.955423, -0.295242}, 
            new double[] {0.000000, -1.000000, 0.000000}, 
            new double[] {0.262866, -0.951056, -0.162460}, 
            new double[] {0.000000, -0.850651, 0.525731}, 
            new double[] {0.000000, -0.955423, 0.295242}, 
            new double[] {0.238856, -0.864188, 0.442863}, 
            new double[] {0.262866, -0.951056, 0.162460}, 
            new double[] {0.500000, -0.809017, 0.309017}, 
            new double[] {0.716567, -0.681718, 0.147621}, 
            new double[] {0.525731, -0.850651, 0.000000}, 
            new double[] {-0.238856, -0.864188, -0.442863}, 
            new double[] {-0.500000, -0.809017, -0.309017}, 
            new double[] {-0.262866, -0.951056, -0.162460}, 
            new double[] {-0.850651, -0.525731, 0.000000}, 
            new double[] {-0.716567, -0.681718, -0.147621}, 
            new double[] {-0.716567, -0.681718, 0.147621}, 
            new double[] {-0.525731, -0.850651, 0.000000}, 
            new double[] {-0.500000, -0.809017, 0.309017}, 
            new double[] {-0.238856, -0.864188, 0.442863}, 
            new double[] {-0.262866, -0.951056, 0.162460}, 
            new double[] {-0.864188, -0.442863, 0.238856}, 
            new double[] {-0.809017, -0.309017, 0.500000}, 
            new double[] {-0.688191, -0.587785, 0.425325}, 
            new double[] {-0.681718, -0.147621, 0.716567}, 
            new double[] {-0.442863, -0.238856, 0.864188}, 
            new double[] {-0.587785, -0.425325, 0.688191}, 
            new double[] {-0.309017, -0.500000, 0.809017}, 
            new double[] {-0.147621, -0.716567, 0.681718}, 
            new double[] {-0.425325, -0.688191, 0.587785}, 
            new double[] {-0.162460, -0.262866, 0.951056}, 
            new double[] {0.442863, -0.238856, 0.864188}, 
            new double[] {0.162460, -0.262866, 0.951056}, 
            new double[] {0.309017, -0.500000, 0.809017}, 
            new double[] {0.147621, -0.716567, 0.681718}, 
            new double[] {0.000000, -0.525731, 0.850651}, 
            new double[] {0.425325, -0.688191, 0.587785}, 
            new double[] {0.587785, -0.425325, 0.688191}, 
            new double[] {0.688191, -0.587785, 0.425325}, 
            new double[] {-0.955423, 0.295242, 0.000000}, 
            new double[] {-0.951056, 0.162460, 0.262866}, 
            new double[] {-1.000000, 0.000000, 0.000000}, 
            new double[] {-0.850651, 0.000000, 0.525731}, 
            new double[] {-0.955423, -0.295242, 0.000000}, 
            new double[] {-0.951056, -0.162460, 0.262866}, 
            new double[] {-0.864188, 0.442863, -0.238856}, 
            new double[] {-0.951056, 0.162460, -0.262866}, 
            new double[] {-0.809017, 0.309017, -0.500000}, 
            new double[] {-0.864188, -0.442863, -0.238856}, 
            new double[] {-0.951056, -0.162460, -0.262866}, 
            new double[] {-0.809017, -0.309017, -0.500000}, 
            new double[] {-0.681718, 0.147621, -0.716567}, 
            new double[] {-0.681718, -0.147621, -0.716567}, 
            new double[] {-0.850651, 0.000000, -0.525731}, 
            new double[] {-0.688191, 0.587785, -0.425325}, 
            new double[] {-0.587785, 0.425325, -0.688191}, 
            new double[] {-0.425325, 0.688191, -0.587785}, 
            new double[] {-0.425325, -0.688191, -0.587785}, 
            new double[] {-0.587785, -0.425325, -0.688191}, 
            new double[] {-0.688191, -0.587785, -0.425325}, 
        };

        static draw.finalvert_t[]   finalverts;
        static auxvert_t[]          auxverts;

        static void r_alias_init()
        {
            finalverts = new draw.finalvert_t[MAXALIASVERTS];
            auxverts = new auxvert_t[MAXALIASVERTS];

            for (int kk = 0; kk < MAXALIASVERTS; kk++)
            {
                finalverts[kk] = new draw.finalvert_t();
                auxverts[kk] = new auxvert_t();
            }
        }

        /*
        ================
        R_AliasCheckBBox
        ================
        */
        static bool R_AliasCheckBBox ()
        {
	        int					    i, flags, frame, numv;
	        model.aliashdr_t	    pahdr;
	        double				    zi, v0, v1, frac;
            double[][]              basepts = new double[8][];
	        draw.finalvert_t		pv0, pv1;
            draw.finalvert_t[]      viewpts = new draw.finalvert_t[16];
	        auxvert_t			    pa0, pa1;
            auxvert_t[]             viewaux = new auxvert_t[16];
	        model.maliasframedesc_t	pframedesc;
	        bool			        zclipped, zfullyclipped;
	        uint    			    anyclip, allclip;
	        int					    minz;
            
            int kk;
            for (kk = 0; kk < 16; kk++) viewpts[kk] = new draw.finalvert_t();
            for (kk = 0; kk < 16; kk++) viewaux[kk] = new auxvert_t();
            for (kk = 0; kk < 8; kk++) basepts[kk] = new double[3];

        // expand, rotate, and translate points into worldspace

	        currententity.trivial_accept = 0;
	        pmodel = currententity.model;
	        pahdr = (model.aliashdr_t)model.Mod_Extradata (pmodel);
            pmdl = (model.mdl_t)pahdr.model;

	        R_AliasSetUpTransform (0);

        // construct the base bounding box for this frame
	        frame = currententity.frame;
        // TODO: don't repeat this check when drawing?
	        if ((frame >= pmdl.numframes) || (frame < 0))
	        {
		        console.Con_DPrintf ("No such frame " + frame + " " + pmodel.name + "\n");
		        frame = 0;
	        }

	        pframedesc = pahdr.frames[frame];

        // x worldspace coordinates
	        basepts[0][0] = basepts[1][0] = basepts[2][0] = basepts[3][0] =
			        (double)pframedesc.bboxmin.v[0];
	        basepts[4][0] = basepts[5][0] = basepts[6][0] = basepts[7][0] =
			        (double)pframedesc.bboxmax.v[0];

        // y worldspace coordinates
	        basepts[0][1] = basepts[3][1] = basepts[5][1] = basepts[6][1] =
			        (double)pframedesc.bboxmin.v[1];
	        basepts[1][1] = basepts[2][1] = basepts[4][1] = basepts[7][1] =
			        (double)pframedesc.bboxmax.v[1];

        // z worldspace coordinates
	        basepts[0][2] = basepts[1][2] = basepts[4][2] = basepts[5][2] =
			        (double)pframedesc.bboxmin.v[2];
	        basepts[2][2] = basepts[3][2] = basepts[6][2] = basepts[7][2] =
			        (double)pframedesc.bboxmax.v[2];

	        zclipped = false;
	        zfullyclipped = true;

	        minz = 9999;
	        for (i=0; i<8 ; i++)
	        {
		        R_AliasTransformVector  (basepts[i], ref viewaux[i].fv);

		        if (viewaux[i].fv[2] < ALIAS_Z_CLIP_PLANE)
		        {
		        // we must clip points that are closer than the near clip plane
			        viewpts[i].flags = ALIAS_Z_CLIP;
			        zclipped = true;
		        }
		        else
		        {
			        if (viewaux[i].fv[2] < minz)
				        minz = (int)viewaux[i].fv[2];
			        viewpts[i].flags = 0;
			        zfullyclipped = false;
		        }
	        }
        	
	        if (zfullyclipped)
	        {
		        return false;	// everything was near-z-clipped
	        }

	        numv = 8;

	        if (zclipped)
	        {
	        // organize points by edges, use edges to get new points (possible trivial
	        // reject)
		        for (i=0 ; i<12 ; i++)
		        {
		        // edge endpoints
			        pv0 = viewpts[aedges[i].index0];
			        pv1 = viewpts[aedges[i].index1];
			        pa0 = viewaux[aedges[i].index0];
			        pa1 = viewaux[aedges[i].index1];

		        // if one end is clipped and the other isn't, make a new point
			        if ((pv0.flags ^ pv1.flags) != 0)
			        {
				        frac = (ALIAS_Z_CLIP_PLANE - pa0.fv[2]) /
					           (pa1.fv[2] - pa0.fv[2]);
				        viewaux[numv].fv[0] = pa0.fv[0] +
						        (pa1.fv[0] - pa0.fv[0]) * frac;
				        viewaux[numv].fv[1] = pa0.fv[1] +
						        (pa1.fv[1] - pa0.fv[1]) * frac;
				        viewaux[numv].fv[2] = ALIAS_Z_CLIP_PLANE;
				        viewpts[numv].flags = 0;
				        numv++;
			        }
		        }
	        }

        // project the vertices that remain after clipping
	        anyclip = 0;
	        allclip = ALIAS_XY_CLIP_MASK;

        // TODO: probably should do this loop in ASM, especially if we use floats
	        for (i=0 ; i<numv ; i++)
	        {
	        // we don't need to bother with vertices that were z-clipped
		        if ((viewpts[i].flags & ALIAS_Z_CLIP) != 0)
			        continue;

		        zi = 1.0 / viewaux[i].fv[2];

	        // FIXME: do with chop mode in ASM, or convert to float
		        v0 = (viewaux[i].fv[0] * xscale * zi) + xcenter;
		        v1 = (viewaux[i].fv[1] * yscale * zi) + ycenter;

		        flags = 0;

		        if (v0 < r_refdef.fvrectx)
			        flags |= ALIAS_LEFT_CLIP;
		        if (v1 < r_refdef.fvrecty)
			        flags |= ALIAS_TOP_CLIP;
		        if (v0 > r_refdef.fvrectright)
			        flags |= ALIAS_RIGHT_CLIP;
		        if (v1 > r_refdef.fvrectbottom)
			        flags |= ALIAS_BOTTOM_CLIP;

		        anyclip |= (uint)flags;
		        allclip &= (uint)flags;
	        }

	        if (allclip != 0)
		        return false;	// trivial reject off one side

	        currententity.trivial_accept = ((anyclip == 0) && !zclipped) ? 1 : 0;

	        if (currententity.trivial_accept != 0)
	        {
		        if (minz > (r_aliastransition + (pmdl.size * r_resfudge)))
		        {
			        currententity.trivial_accept |= 2;
		        }
	        }

	        return true;
        }

        /*
        ================
        R_AliasTransformVector
        ================
        */
        static void R_AliasTransformVector (double[] @in, ref double[] @out)
        {
	        @out[0] = mathlib.DotProduct(@in, aliastransform[0]) + aliastransform[0][3];
	        @out[1] = mathlib.DotProduct(@in, aliastransform[1]) + aliastransform[1][3];
	        @out[2] = mathlib.DotProduct(@in, aliastransform[2]) + aliastransform[2][3];
        }

        /*
        ================
        R_AliasPreparePoints

        General clipped case
        ================
        */
        static void R_AliasPreparePoints ()
        {
	        int			        i;
	        model.stvert_t[]    pstverts;
	        draw.finalvert_t	fv;
	        auxvert_t	        av;
	        model.mtriangle_t	ptri;
	        draw.finalvert_t[]	pfv = new draw.finalvert_t[3];

	        pstverts = (model.stvert_t[])paliashdr.stverts;
	        r_anumverts = pmdl.numverts;

	        for (i=0 ; i<r_anumverts ; i++)
	        {
                fv = pfinalverts[i];
                av = pauxverts[i];

		        R_AliasTransformFinalVert (fv, av, r_apverts[i], pstverts[i]);
		        if (av.fv[2] < ALIAS_Z_CLIP_PLANE)
			        fv.flags |= ALIAS_Z_CLIP;
		        else
		        {
			         R_AliasProjectFinalVert (fv, av);

			        if (fv.v[0] < r_refdef.aliasvrect.x)
				        fv.flags |= ALIAS_LEFT_CLIP;
			        if (fv.v[1] < r_refdef.aliasvrect.y)
				        fv.flags |= ALIAS_TOP_CLIP;
			        if (fv.v[0] > r_refdef.aliasvrectright)
				        fv.flags |= ALIAS_RIGHT_CLIP;
			        if (fv.v[1] > r_refdef.aliasvrectbottom)
				        fv.flags |= ALIAS_BOTTOM_CLIP;	
		        }
	        }

        //
        // clip and draw all triangles
        //
	        r_affinetridesc.numtriangles = 1;

	        for (i=0 ; i<pmdl.numtris ; i++)
	        {
                ptri = (model.mtriangle_t)paliashdr.triangles[i];
                pfv[0] = pfinalverts[ptri.vertindex[0]];
		        pfv[1] = pfinalverts[ptri.vertindex[1]];
		        pfv[2] = pfinalverts[ptri.vertindex[2]];

		        if (( pfv[0].flags & pfv[1].flags & pfv[2].flags & (ALIAS_XY_CLIP_MASK | ALIAS_Z_CLIP) ) != 0)
			        continue;		// completely clipped
        		
		        if ( ( (pfv[0].flags | pfv[1].flags | pfv[2].flags) &
			        (ALIAS_XY_CLIP_MASK | ALIAS_Z_CLIP) ) == 0 )
		        {	// totally unclipped
			        r_affinetridesc.pfinalverts = pfinalverts;
			        r_affinetridesc.ptriangles = new model.mtriangle_t[] { ptri };
			        draw.D_PolysetDraw ();
		        }
		        else		
		        {	// partially clipped
			        R_AliasClipTriangle (ptri);
		        }
	        }
        }

        /*
        ================
        R_AliasSetUpTransform
        ================
        */
        static double[][]	tmatrix = { new double[4], new double[4], new double[4] };
        static double[][]	viewmatrix = { new double[4], new double[4], new double[4] };
        static void R_AliasSetUpTransform (int trivial_accept)
        {
	        int				i;
            double[][]	    rotationmatrix = { new double[4], new double[4], new double[4] };
            double[][]	    t2matrix = { new double[4], new double[4], new double[4] };
	        double[]		angles = new double[3];
            int kk;

        // TODO: should really be stored with the entity instead of being reconstructed
        // TODO: should use a look-up table
        // TODO: could cache lazily, stored in the entity

            angles[quakedef.ROLL] = currententity.angles[quakedef.ROLL];
            angles[quakedef.PITCH] = -currententity.angles[quakedef.PITCH];
            angles[quakedef.YAW] = currententity.angles[quakedef.YAW];
	        mathlib.AngleVectors (angles, ref alias_forward, ref alias_right, ref alias_up);

	        tmatrix[0][0] = pmdl.scale[0];
	        tmatrix[1][1] = pmdl.scale[1];
	        tmatrix[2][2] = pmdl.scale[2];

	        tmatrix[0][3] = pmdl.scale_origin[0];
	        tmatrix[1][3] = pmdl.scale_origin[1];
	        tmatrix[2][3] = pmdl.scale_origin[2];

        // TODO: can do this with simple matrix rearrangement

	        for (i=0 ; i<3 ; i++)
	        {
		        t2matrix[i][0] = alias_forward[i];
		        t2matrix[i][1] = -alias_right[i];
		        t2matrix[i][2] = alias_up[i];
	        }

	        t2matrix[0][3] = -modelorg[0];
	        t2matrix[1][3] = -modelorg[1];
	        t2matrix[2][3] = -modelorg[2];

        // FIXME: can do more efficiently than full concatenation
	        mathlib.R_ConcatTransforms (t2matrix, tmatrix, ref rotationmatrix);

        // TODO: should be global, set when vright, etc., set
	        mathlib.VectorCopy (vright, ref viewmatrix[0]);
            mathlib.VectorCopy(vup, ref viewmatrix[1]);
            mathlib.VectorInverse(ref viewmatrix[1]);
            mathlib.VectorCopy(vpn, ref viewmatrix[2]);

            /*sys_linux.Sys_Printf("-----------------");
            for (kk = 0; kk < 3; kk++)
            {
                sys_linux.Sys_Printf("viewmatrix[kk][0] = " + viewmatrix[kk][0]);
                sys_linux.Sys_Printf("viewmatrix[kk][1] = " + viewmatrix[kk][1]);
                sys_linux.Sys_Printf("viewmatrix[kk][2] = " + viewmatrix[kk][2]);
            }*/

        //	viewmatrix[0][3] = 0;
        //	viewmatrix[1][3] = 0;
        //	viewmatrix[2][3] = 0;

	        mathlib.R_ConcatTransforms (viewmatrix, rotationmatrix, ref aliastransform);

            /*sys_linux.Sys_Printf("trivial_accept = " + trivial_accept);
            for (kk = 0; kk < 4; kk++)
            {
                sys_linux.Sys_Printf("aliastransform[0][kk] = " + aliastransform[0][kk]);
                sys_linux.Sys_Printf("aliastransform[1][kk] = " + aliastransform[1][kk]);
                sys_linux.Sys_Printf("aliastransform[2][kk] = " + aliastransform[2][kk]);
            }*/

        // do the scaling up of x and y to screen coordinates as part of the transform
        // for the unclipped case (it would mess up clipping in the clipped case).
        // Also scale down z, so 1/z is scaled 31 bits for free, and scale down x and y
        // correspondingly so the projected x and y come out right
        // FIXME: make this work for clipped case too?
	        if (trivial_accept != 0)
	        {
		        for (i=0 ; i<4 ; i++)
		        {
			        aliastransform[0][i] *= aliasxscale *
					        (1.0 / ((double)0x8000 * 0x10000));
			        aliastransform[1][i] *= aliasyscale *
                            (1.0 / ((double)0x8000 * 0x10000));
                    aliastransform[2][i] *= 1.0 / ((double)0x8000 * 0x10000);

		        }
	        }
        }

        /*
        ================
        R_AliasTransformFinalVert
        ================
        */
        static void R_AliasTransformFinalVert(draw.finalvert_t fv, auxvert_t av,
            model.trivertx_t pverts, model.stvert_t pstverts)
        {
	        int		    temp;
	        double      lightcos;
            double[]    plightnormal;

	        av.fv[0] = mathlib.DotProduct(pverts.v, aliastransform[0]) +
			        aliastransform[0][3];
            av.fv[1] = mathlib.DotProduct(pverts.v, aliastransform[1]) +
			        aliastransform[1][3];
            av.fv[2] = mathlib.DotProduct(pverts.v, aliastransform[2]) +
			        aliastransform[2][3];

	        fv.v[2] = pstverts.s;
	        fv.v[3] = pstverts.t;

	        fv.flags = pstverts.onseam;

        // lighting
	        plightnormal = r_avertexnormals[pverts.lightnormalindex];
            lightcos = mathlib.DotProduct(plightnormal, r_plightvec);
	        temp = r_ambientlight;

	        if (lightcos < 0)
	        {
		        temp += (int)(r_shadelight * lightcos);

	        // clamp; because we limited the minimum ambient and shading light, we
	        // don't have to clamp low light, just bright
		        if (temp < 0)
			        temp = 0;
	        }

	        fv.v[4] = temp;
        }

        /*
        ================
        R_AliasTransformAndProjectFinalVerts
        ================
        */
        static void R_AliasTransformAndProjectFinalVerts (draw.finalvert_t[] pfv, model.stvert_t[] pstverts)
        {
	        int			        i, temp;
	        double		        lightcos;
            double[]            plightnormal;
            double              zi;
	        model.trivertx_t	pverts;

	        for (i=0 ; i<r_anumverts ; i++)
	        {
                draw.finalvert_t    fv = pfv[i];
                model.stvert_t stverts = pstverts[i];
                pverts = r_apverts[i];

                // transform and project
		        zi = 1.0 / (mathlib.DotProduct(pverts.v, aliastransform[2]) +
				        aliastransform[2][3]);

	        // x, y, and z are scaled down by 1/2**31 in the transform, so 1/z is
	        // scaled up by 1/2**31, and the scaling cancels out for x and y in the
	        // projection
		        fv.v[5] = (int)zi;

                fv.v[0] = (int)(((mathlib.DotProduct(pverts.v, aliastransform[0]) +
				        aliastransform[0][3]) * zi) + aliasxcenter);
                fv.v[1] = (int)(((mathlib.DotProduct(pverts.v, aliastransform[1]) +
				        aliastransform[1][3]) * zi) + aliasycenter);

		        fv.v[2] = stverts.s;
		        fv.v[3] = stverts.t;
		        fv.flags = stverts.onseam;

	        // lighting
		        plightnormal = r_avertexnormals[pverts.lightnormalindex];
                lightcos = mathlib.DotProduct(plightnormal, r_plightvec);
		        temp = r_ambientlight;

		        if (lightcos < 0)
		        {
			        temp += (int)(r_shadelight * lightcos);

		        // clamp; because we limited the minimum ambient and shading light, we
		        // don't have to clamp low light, just bright
			        if (temp < 0)
				        temp = 0;
		        }

		        fv.v[4] = temp;
	        }
        }

        /*
        ================
        R_AliasProjectFinalVert
        ================
        */
        static void R_AliasProjectFinalVert (draw.finalvert_t fv, auxvert_t av)
        {
	        double	zi;

        // project points
	        zi = 1.0 / av.fv[2];

	        fv.v[5] = (int)(zi * ziscale);

	        fv.v[0] = (int)((av.fv[0] * aliasxscale * zi) + aliasxcenter);
	        fv.v[1] = (int)((av.fv[1] * aliasyscale * zi) + aliasycenter);
        }

        /*
        ================
        R_AliasPrepareUnclippedPoints
        ================
        */
        static void R_AliasPrepareUnclippedPoints ()
        {
            model.stvert_t[]    pstverts;
            draw.finalvert_t[]  fv;

            pstverts = (model.stvert_t[])paliashdr.stverts;
            r_anumverts = pmdl.numverts;
            // FIXME: just use pfinalverts directly?
            fv = pfinalverts;

            R_AliasTransformAndProjectFinalVerts(fv, pstverts);

            if (r_affinetridesc.drawtype)
                draw.D_PolysetDrawFinalVerts(fv, r_anumverts);

            r_affinetridesc.pfinalverts = pfinalverts;
            r_affinetridesc.ptriangles = (model.mtriangle_t[])paliashdr.triangles;
            r_affinetridesc.numtriangles = pmdl.numtris;

            draw.D_PolysetDraw();
        }

        /*
        ===============
        R_AliasSetupSkin
        ===============
        */
        static void R_AliasSetupSkin ()
        {
            int                     skinnum;
            int                     i, numskins;
            model.maliasskingroup_t paliasskingroup;
            double[]                pskinintervals;
            double                  fullskininterval;
            double                  skintargettime, skintime;

            skinnum = currententity.skinnum;
            if ((skinnum >= pmdl.numskins) || (skinnum < 0))
            {
                console.Con_DPrintf("R_AliasSetupSkin: no such skin # " + skinnum + "\n");
                skinnum = 0;
            }

            pskindesc = (model.maliasskindesc_t)paliashdr.skindesc[skinnum];
            a_skinwidth = pmdl.skinwidth;

            if (pskindesc.type == model.aliasskintype_t.ALIAS_SKIN_GROUP)
            {
                paliasskingroup = (model.maliasskingroup_t)pskindesc.skin;
                pskinintervals = (double[])paliasskingroup.intervals;
                numskins = paliasskingroup.numskins;
                fullskininterval = pskinintervals[numskins - 1];

                skintime = client.cl.time + currententity.syncbase;

                // when loading in Mod_LoadAliasSkinGroup, we guaranteed all interval
                // values are positive, so we don't have to worry about division by 0
                skintargettime = skintime -
                        ((int)(skintime / fullskininterval)) * fullskininterval;

                for (i = 0; i < (numskins - 1); i++)
                {
                    if (pskinintervals[i] > skintargettime)
                        break;
                }

                pskindesc = paliasskingroup.skindescs[i];
            }

            r_affinetridesc.pskindesc = pskindesc;
            r_affinetridesc.pskin = (byte[])pskindesc.skin;
            r_affinetridesc.skinwidth = a_skinwidth;
            r_affinetridesc.seamfixupX16 = (a_skinwidth >> 1) << 16;
            r_affinetridesc.skinheight = pmdl.skinheight;
        }

        /*
        ================
        R_AliasSetupLighting
        ================
        */
        static void R_AliasSetupLighting (alight_t plighting)
        {
            // guarantee that no vertex will ever be lit below LIGHT_MIN, so we don't have
            // to clamp off the bottom
            r_ambientlight = plighting.ambientlight;

            if (r_ambientlight < LIGHT_MIN)
                r_ambientlight = LIGHT_MIN;

            r_ambientlight = (255 - r_ambientlight) << vid.VID_CBITS;

            if (r_ambientlight < LIGHT_MIN)
                r_ambientlight = LIGHT_MIN;

            r_shadelight = plighting.shadelight;

            if (r_shadelight < 0)
                r_shadelight = 0;

            r_shadelight *= vid.VID_GRADES;

            // rotate the lighting vector into the model's frame of reference
            r_plightvec[0] = mathlib.DotProduct(plighting.plightvec, alias_forward);
            r_plightvec[1] = -mathlib.DotProduct(plighting.plightvec, alias_right);
            r_plightvec[2] = mathlib.DotProduct(plighting.plightvec, alias_up);
        }

        /*
        =================
        R_AliasSetupFrame

        set r_apverts
        =================
        */
        static void R_AliasSetupFrame ()
        {
            int                 frame;
            int                 i, numframes;
            model.maliasgroup_t paliasgroup;
            double[]            pintervals;
            double              fullinterval, targettime, time;

            frame = currententity.frame;
            if ((frame >= pmdl.numframes) || (frame < 0))
            {
                console.Con_DPrintf("R_AliasSetupFrame: no such frame " + frame + "\n");
                frame = 0;
            }

            if (paliashdr.frames[frame].type == model.aliasframetype_t.ALIAS_SINGLE)
            {
                r_apverts = (model.trivertx_t[])paliashdr.frames[frame].frame;
                return;
            }

            paliasgroup = (model.maliasgroup_t)paliashdr.frames[frame].frame;
            pintervals = (double[])paliasgroup.intervals;
            numframes = paliasgroup.numframes;
            fullinterval = pintervals[numframes - 1];

            time = client.cl.time + currententity.syncbase;

            //
            // when loading in Mod_LoadAliasGroup, we guaranteed all interval values
            // are positive, so we don't have to worry about division by 0
            //
            targettime = time - ((int)(time / fullinterval)) * fullinterval;

            for (i = 0; i < (numframes - 1); i++)
            {
                if (pintervals[i] > targettime)
                    break;
            }

            r_apverts = (model.trivertx_t[])paliasgroup.frames[i].frame;
        }

        /*
        ================
        R_AliasDrawModel
        ================
        */
        static void R_AliasDrawModel (alight_t plighting)
        {
	        r_amodels_drawn++;

        // cache align
	        pfinalverts = (draw.finalvert_t[])finalverts;
	        pauxverts = auxverts;

	        paliashdr = (model.aliashdr_t)model.Mod_Extradata (currententity.model);
	        pmdl = (model.mdl_t)paliashdr.model;

	        R_AliasSetupSkin ();
	        R_AliasSetUpTransform (currententity.trivial_accept);
	        R_AliasSetupLighting (plighting);
	        R_AliasSetupFrame ();

	        if (currententity.colormap == null)
		        sys_linux.Sys_Error ("R_AliasDrawModel: !currententity.colormap");

	        r_affinetridesc.drawtype = (currententity.trivial_accept == 3) &&
			        r_recursiveaffinetriangles;

	        if (r_affinetridesc.drawtype)
	        {
		        draw.D_PolysetUpdateTables ();		// FIXME: precalc...
	        }
	        else
	        {
	        }

	        acolormap = currententity.colormap;

	        if (currententity != client.cl.viewent)
                ziscale = (double)0x8000 * (double)0x10000;
	        else
                ziscale = (double)0x8000 * (double)0x10000 * 3.0;

	        if (currententity.trivial_accept != 0)
		        R_AliasPrepareUnclippedPoints ();
	        else
		        R_AliasPreparePoints ();
        }
    }
}
