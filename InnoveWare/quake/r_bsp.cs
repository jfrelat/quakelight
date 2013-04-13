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
// r_bsp.c

namespace quake
{
    public partial class render
    {
        //
        // current entity info
        //
        static bool		            insubmodel;
        public static entity_t		currententity;
        public static double[]		modelorg = new double[3], base_modelorg = new double[3];
								        // modelorg is the viewpoint reletive to
								        // the currently rendering entity
        static double[]		        r_entorigin = new double[3];	// the currently rendering entity in world
								        // coordinates

        static double[][]	        entity_rotation = { new double[3], new double[3], new double[3] };

        static double[]		        r_worldmodelorg = new double[3];

        static int				    r_currentbkey;

        public enum solidstate_t {touchessolid, drawnode, nodrawnode};

        public const int MAX_BMODEL_VERTS	= 500;			// 6K
        public const int MAX_BMODEL_EDGES	= 1000;		// 12K

        static model.mvertex_t[]    pbverts;
        static bedge_t[]	        pbedges;
        static int			        numbverts, numbedges;

        static model.mvertex_t	    pfrontenter, pfrontexit;

        static bool     		    makeclippededge;
        
        //===========================================================================

        /*
        ================
        R_EntityRotate
        ================
        */
        static void R_EntityRotate (ref double[] vec)
        {
	        double[]	tvec = new double[3];

	        mathlib.VectorCopy (vec, ref tvec);
	        vec[0] = mathlib.DotProduct (entity_rotation[0], tvec);
	        vec[1] = mathlib.DotProduct (entity_rotation[1], tvec);
	        vec[2] = mathlib.DotProduct (entity_rotation[2], tvec);
        }

        /*
        ================
        R_RotateBmodel
        ================
        */
        public static void R_RotateBmodel ()
        {
            double      angle, s, c;
            double[][]  temp1 = { new double[3], new double[3], new double[3] };
            double[][]  temp2 = { new double[3], new double[3], new double[3] };
            double[][]  temp3 = { new double[3], new double[3], new double[3] };

        // TODO: should use a look-up table
        // TODO: should really be stored with the entity instead of being reconstructed
        // TODO: could cache lazily, stored in the entity
        // TODO: share work with R_SetUpAliasTransform

        // yaw
	        angle = currententity.angles[quakedef.YAW];		
	        angle = angle * mathlib.M_PI*2 / 360;
	        s = Math.Sin(angle);
            c = Math.Cos(angle);

	        temp1[0][0] = c;
	        temp1[0][1] = s;
	        temp1[0][2] = 0;
	        temp1[1][0] = -s;
	        temp1[1][1] = c;
	        temp1[1][2] = 0;
	        temp1[2][0] = 0;
	        temp1[2][1] = 0;
	        temp1[2][2] = 1;
            
        // pitch
	        angle = currententity.angles[quakedef.PITCH];		
	        angle = angle * mathlib.M_PI*2 / 360;
            s = Math.Sin(angle);
            c = Math.Cos(angle);

	        temp2[0][0] = c;
	        temp2[0][1] = 0;
	        temp2[0][2] = -s;
	        temp2[1][0] = 0;
	        temp2[1][1] = 1;
	        temp2[1][2] = 0;
	        temp2[2][0] = s;
	        temp2[2][1] = 0;
	        temp2[2][2] = c;

	        mathlib.R_ConcatRotations (temp2, temp1, ref temp3);

        // roll
	        angle = currententity.angles[quakedef.ROLL];		
	        angle = angle * mathlib.M_PI*2 / 360;
            s = Math.Sin(angle);
            c = Math.Cos(angle);

	        temp1[0][0] = 1;
	        temp1[0][1] = 0;
	        temp1[0][2] = 0;
	        temp1[1][0] = 0;
	        temp1[1][1] = c;
	        temp1[1][2] = s;
	        temp1[2][0] = 0;
	        temp1[2][1] = -s;
	        temp1[2][2] = c;

	        mathlib.R_ConcatRotations (temp1, temp3, ref entity_rotation);

        //
        // rotate modelorg and the transformation matrix
        //
	        R_EntityRotate (ref modelorg);
	        R_EntityRotate (ref vpn);
	        R_EntityRotate (ref vright);
	        R_EntityRotate (ref vup);

	        R_TransformFrustum ();
        }

        /*
        ================
        R_RecursiveClipBPoly
        ================
        */
        static void R_RecursiveClipBPoly(bedge_t pedges, model.mnode_t pnode, model.msurface_t psurf)
        {
            bedge_t[]               psideedges = new bedge_t[2];
            bedge_t                 pnextedge, ptedge;
	        int			            i, side, lastside;
	        double		            dist, frac, lastdist;
	        model.mplane_t	        splitplane, tplane = new model.mplane_t();
	        model.mvertex_t	        pvert, plastvert, ptvert;
	        model.node_or_leaf_t	pn;

	        psideedges[0] = psideedges[1] = null;

	        makeclippededge = false;

        // transform the BSP plane into model space
        // FIXME: cache these?
	        splitplane = pnode.plane;
	        tplane.dist = splitplane.dist -
			        mathlib.DotProduct(r_entorigin, splitplane.normal);
            tplane.normal[0] = mathlib.DotProduct(entity_rotation[0], splitplane.normal);
            tplane.normal[1] = mathlib.DotProduct(entity_rotation[1], splitplane.normal);
            tplane.normal[2] = mathlib.DotProduct(entity_rotation[2], splitplane.normal);

        // clip edges to BSP plane
	        for ( ; pedges != null ; pedges = pnextedge)
	        {
		        pnextedge = pedges.pnext;

	        // set the status for the last point as the previous point
	        // FIXME: cache this stuff somehow?
		        plastvert = pedges.v[0];
		        lastdist = mathlib.DotProduct (plastvert.position, tplane.normal) -
				           tplane.dist;

		        if (lastdist > 0)
			        lastside = 0;
		        else
			        lastside = 1;

		        pvert = pedges.v[1];

                dist = mathlib.DotProduct(pvert.position, tplane.normal) - tplane.dist;

		        if (dist > 0)
			        side = 0;
		        else
			        side = 1;

		        if (side != lastside)
		        {
		        // clipped
			        if (numbverts >= MAX_BMODEL_VERTS)
				        return;

		        // generate the clipped vertex
			        frac = lastdist / (lastdist - dist);
			        ptvert = pbverts[numbverts++];
			        ptvert.position[0] = plastvert.position[0] +
					        frac * (pvert.position[0] -
					        plastvert.position[0]);
			        ptvert.position[1] = plastvert.position[1] +
					        frac * (pvert.position[1] -
					        plastvert.position[1]);
			        ptvert.position[2] = plastvert.position[2] +
					        frac * (pvert.position[2] -
					        plastvert.position[2]);

		        // split into two edges, one on each side, and remember entering
		        // and exiting points
		        // FIXME: share the clip edge by having a winding direction flag?
			        if (numbedges >= (MAX_BMODEL_EDGES - 1))
			        {
				        console.Con_Printf ("Out of edges for bmodel\n");
				        return;
			        }

			        ptedge = pbedges[numbedges];
			        ptedge.pnext = psideedges[lastside];
			        psideedges[lastside] = ptedge;
			        ptedge.v[0] = plastvert;
			        ptedge.v[1] = ptvert;

			        ptedge = pbedges[numbedges + 1];
			        ptedge.pnext = psideedges[side];
			        psideedges[side] = ptedge;
			        ptedge.v[0] = ptvert;
			        ptedge.v[1] = pvert;

			        numbedges += 2;

			        if (side == 0)
			        {
			        // entering for front, exiting for back
				        pfrontenter = ptvert;
				        makeclippededge = true;
			        }
			        else
			        {
				        pfrontexit = ptvert;
				        makeclippededge = true;
			        }
		        }
		        else
		        {
		        // add the edge to the appropriate side
			        pedges.pnext = psideedges[side];
			        psideedges[side] = pedges;
		        }
	        }

        // if anything was clipped, reconstitute and add the edges along the clip
        // plane to both sides (but in opposite directions)
	        if (makeclippededge)
	        {
		        if (numbedges >= (MAX_BMODEL_EDGES - 2))
		        {
			        console.Con_Printf ("Out of edges for bmodel\n");
			        return;
		        }

		        ptedge = pbedges[numbedges];
		        ptedge.pnext = psideedges[0];
		        psideedges[0] = ptedge;
		        ptedge.v[0] = pfrontexit;
		        ptedge.v[1] = pfrontenter;

		        ptedge = pbedges[numbedges + 1];
		        ptedge.pnext = psideedges[1];
		        psideedges[1] = ptedge;
		        ptedge.v[0] = pfrontenter;
		        ptedge.v[1] = pfrontexit;

		        numbedges += 2;
	        }

        // draw or recurse further
	        for (i=0 ; i<2 ; i++)
	        {
		        if (psideedges[i] != null)
		        {
		        // draw if we've reached a non-solid leaf, done if all that's left is a
		        // solid leaf, and continue down the tree if it's not a leaf
			        pn = pnode.children[i];

		        // we're done with this branch if the node or leaf isn't in the PVS
			        if (pn.visframe == r_visframecount)
			        {
				        if (pn.contents < 0)
				        {
					        if (pn.contents != bspfile.CONTENTS_SOLID)
					        {
						        r_currentbkey = ((model.mleaf_t)pn).key;
						        R_RenderBmodelFace (psideedges[i], psurf);
					        }
				        }
				        else
				        {
					        R_RecursiveClipBPoly (psideedges[i], (model.mnode_t)pnode.children[i],
									          psurf);
				        }
			        }
		        }
	        }
        }

        /*
        ================
        R_DrawSolidClippedSubmodelPolygons
        ================
        */
        static void R_DrawSolidClippedSubmodelPolygons (model.model_t pmodel)
        {
	        int			        i, j, lindex;
	        double		        dot;
	        model.msurface_t	psurf;
	        int			        numsurfaces;
	        model.mplane_t	    pplane;
	        model.mvertex_t[]	bverts = new model.mvertex_t[MAX_BMODEL_VERTS];
	        bedge_t[]		    bedges = new bedge_t[MAX_BMODEL_EDGES];
            int                 pbedge;
	        model.medge_t		pedge;
            model.medge_t[]     pedges;

            int kk;
            for (kk = 0; kk < MAX_BMODEL_VERTS; kk++) bverts[kk] = new model.mvertex_t();
            for (kk = 0; kk < MAX_BMODEL_EDGES; kk++) bedges[kk] = new bedge_t();

        // FIXME: use bounding-box-based frustum clipping info?

	        numsurfaces = pmodel.nummodelsurfaces;
	        pedges = pmodel.edges;

	        for (i=0 ; i<numsurfaces ; i++)
	        {
                psurf = pmodel.surfaces[pmodel.firstmodelsurface + i];

            // find which side of the node we are on
		        pplane = psurf.plane;

		        dot = mathlib.DotProduct (modelorg, pplane.normal) - pplane.dist;

	        // draw the polygon
		        if (((psurf.flags & model.SURF_PLANEBACK) != 0 && (dot < -BACKFACE_EPSILON)) ||
                    ((psurf.flags & model.SURF_PLANEBACK) == 0 && (dot > BACKFACE_EPSILON)))
		        {
		        // FIXME: use bounding-box-based frustum clipping info?

		        // copy the edges to bedges, flipping if necessary so always
		        // clockwise winding
		        // FIXME: if edges and vertices get caches, these assignments must move
		        // outside the loop, and overflow checking must be done here
			        pbverts = bverts;
			        pbedges = bedges;
			        numbverts = numbedges = 0;

			        if (psurf.numedges > 0)
			        {
                        pbedge = numbedges;
				        numbedges += psurf.numedges;

				        for (j=0 ; j<psurf.numedges ; j++)
				        {
				           lindex = pmodel.surfedges[psurf.firstedge+j];

					        if (lindex > 0)
					        {
						        pedge = pedges[lindex];
						        bedges[pbedge + j].v[0] = r_pcurrentvertbase[pedge.v[0]];
                                bedges[pbedge + j].v[1] = r_pcurrentvertbase[pedge.v[1]];
					        }
					        else
					        {
						        lindex = -lindex;
						        pedge = pedges[lindex];
                                bedges[pbedge + j].v[0] = r_pcurrentvertbase[pedge.v[1]];
                                bedges[pbedge + j].v[1] = r_pcurrentvertbase[pedge.v[0]];
					        }

                            bedges[pbedge + j].pnext = bedges[pbedge + j + 1];
				        }

                        bedges[pbedge + j - 1].pnext = null;	// mark end of edges

				        R_RecursiveClipBPoly (bedges[pbedge], (model.mnode_t)currententity.topnode, psurf);
			        }
			        else
			        {
				        sys_linux.Sys_Error ("no edges in bmodel");
			        }
		        }
	        }
        }

        /*
        ================
        R_DrawSubmodelPolygons
        ================
        */
        static void R_DrawSubmodelPolygons (model.model_t pmodel, int clipflags)
        {
            int                 i;
            double              dot;
            model.msurface_t    psurf;
            int                 numsurfaces;
            model.mplane_t      pplane;

            // FIXME: use bounding-box-based frustum clipping info?

            numsurfaces = pmodel.nummodelsurfaces;

            for (i = 0; i < numsurfaces; i++)
            {
                psurf = pmodel.surfaces[pmodel.firstmodelsurface + i];

                // find which side of the node we are on
                pplane = psurf.plane;

                dot = mathlib.DotProduct(modelorg, pplane.normal) - pplane.dist;

                // draw the polygon
                if (((psurf.flags & model.SURF_PLANEBACK) != 0 && (dot < -BACKFACE_EPSILON)) ||
                    ((psurf.flags & model.SURF_PLANEBACK) == 0 && (dot > BACKFACE_EPSILON)))
                {
                    r_currentkey = ((model.mleaf_t)currententity.topnode).key;

                    // FIXME: use bounding-box-based frustum clipping info?
                    R_RenderFace(psurf, clipflags);
                }
            }
        }

        /*
        ================
        R_RecursiveWorldNode
        ================
        */
        static void R_RecursiveWorldNode (model.node_or_leaf_t node, int clipflags)
        {
	        int			        i, c, side, pindex;
            double[]            acceptpt = new double[3], rejectpt = new double[3];
	        model.mplane_t	    plane;
	        model.msurface_t	surf, mark;
	        model.mleaf_t		pleaf;
	        double		        d, dot;
            int                 surfofs;

	        if (node.contents == bspfile.CONTENTS_SOLID)
		        return;		// solid

	        if (node.visframe != r_visframecount)
		        return;

        // cull the clipping planes if not trivial accept
        // FIXME: the compiler is doing a lousy job of optimizing here; it could be
        //  twice as fast in ASM
	        if (clipflags != 0)
	        {
		        for (i=0 ; i<4 ; i++)
		        {
			        if ((clipflags & (1<<i)) == 0)
				        continue;	// don't need to clip against it

		        // generate accept and reject points
		        // FIXME: do with fast look-ups or integer tests based on the sign bit
		        // of the floating point values

			        pindex = pfrustum_indexes[i];

                    rejectpt[0] = (double)node.minmaxs[r_frustum_indexes[pindex + 0]];
                    rejectpt[1] = (double)node.minmaxs[r_frustum_indexes[pindex + 1]];
                    rejectpt[2] = (double)node.minmaxs[r_frustum_indexes[pindex + 2]];
        			
			        d = mathlib.DotProduct (rejectpt, view_clipplanes[i].normal);
			        d -= view_clipplanes[i].dist;

			        if (d <= 0)
				        return;

                    acceptpt[0] = (double)node.minmaxs[r_frustum_indexes[pindex + 3 + 0]];
                    acceptpt[1] = (double)node.minmaxs[r_frustum_indexes[pindex + 3 + 1]];
                    acceptpt[2] = (double)node.minmaxs[r_frustum_indexes[pindex + 3 + 2]];

                    d = mathlib.DotProduct(acceptpt, view_clipplanes[i].normal);
			        d -= view_clipplanes[i].dist;

			        if (d >= 0)
				        clipflags &= ~(1<<i);	// node is entirely on screen
		        }
	        }

        // if a leaf node, draw stuff
	        if (node.contents < 0)
	        {
		        pleaf = (model.mleaf_t)node;

                helper.ObjectBuffer _mark = pleaf.firstmarksurface;
                int ofs = _mark.ofs;
                mark = (model.msurface_t)_mark.buffer[ofs];
		        c = pleaf.nummarksurfaces;

		        if (c != 0)
		        {
			        do
			        {
                        mark.visframe = r_framecount;
                        mark = (model.msurface_t)_mark.buffer[++ofs];
			        } while (--c != 0);
		        }

	        // deal with model fragments in this leaf
		        if (pleaf.efrags != null)
		        {
			        R_StoreEfrags (ref pleaf.efrags);
		        }

		        pleaf.key = r_currentkey;
		        r_currentkey++;		// all bmodels in a leaf share the same key
	        }
	        else
	        {
                model.mnode_t _node = (model.mnode_t)node;
            // node is just a decision point, so go down the apropriate sides

	        // find which side of the node we are on
		        plane = _node.plane;

		        switch (plane.type)
		        {
		        case bspfile.PLANE_X:
			        dot = modelorg[0] - plane.dist;
			        break;
		        case bspfile.PLANE_Y:
			        dot = modelorg[1] - plane.dist;
			        break;
                case bspfile.PLANE_Z:
			        dot = modelorg[2] - plane.dist;
			        break;
		        default:
			        dot = mathlib.DotProduct (modelorg, plane.normal) - plane.dist;
			        break;
		        }
        	
		        if (dot >= 0)
			        side = 0;
		        else
			        side = 1;

	        // recurse down the children, front side first
		        R_RecursiveWorldNode (_node.children[side], clipflags);

	        // draw stuff
		        c = _node.numsurfaces;

		        if (c != 0)
		        {
                    surf = client.cl.worldmodel.surfaces[_node.firstsurface];
                    surfofs = _node.firstsurface;

			        if (dot < -BACKFACE_EPSILON)
			        {
				        do
				        {
					        if ((surf.flags & model.SURF_PLANEBACK) != 0 &&
						        (surf.visframe == r_framecount))
					        {
						        if (r_drawpolys)
						        {
							        if (r_worldpolysbacktofront)
							        {
								        if (numbtofpolys < MAX_BTOFPOLYS)
								        {
									        pbtofpolys[numbtofpolys].clipflags =
											        clipflags;
									        pbtofpolys[numbtofpolys].psurf = surf;
									        numbtofpolys++;
								        }
							        }
							        else
							        {
								        R_RenderPoly (surf, clipflags);
							        }
						        }
						        else
						        {
							        R_RenderFace (surf, clipflags);
						        }
					        }

                            surf = client.cl.worldmodel.surfaces[++surfofs];
				        } while (--c != 0);
			        }
			        else if (dot > BACKFACE_EPSILON)
			        {
				        do
				        {
					        if ((surf.flags & model.SURF_PLANEBACK) == 0 &&
						        (surf.visframe == r_framecount))
					        {
						        if (r_drawpolys)
						        {
							        if (r_worldpolysbacktofront)
							        {
								        if (numbtofpolys < MAX_BTOFPOLYS)
								        {
									        pbtofpolys[numbtofpolys].clipflags =
											        clipflags;
									        pbtofpolys[numbtofpolys].psurf = surf;
									        numbtofpolys++;
								        }
							        }
							        else
							        {
								        R_RenderPoly (surf, clipflags);
							        }
						        }
						        else
						        {
							        R_RenderFace (surf, clipflags);
						        }
					        }

                            surf = client.cl.worldmodel.surfaces[++surfofs];
                        } while (--c != 0);
			        }

		        // all surfaces on the same node share the same sequence number
			        r_currentkey++;
		        }

	        // recurse down the back side
		        R_RecursiveWorldNode (_node.children[side == 0 ? 1 : 0], clipflags);
	        }
        }

        /*
        ================
        R_RenderWorld
        ================
        */
        static btofpoly_t[] btofpolys;
        static void R_RenderWorld ()
        {
	        int			    i;
	        model.model_t	clmodel;

            if (btofpolys == null)
            {
                btofpolys = new btofpoly_t[MAX_BTOFPOLYS];
                for (int kk = 0; kk < MAX_BTOFPOLYS; kk++) btofpolys[kk] = new btofpoly_t();
                pbtofpolys = btofpolys;
            }

	        currententity = client.cl_entities[0];
	        mathlib.VectorCopy (r_origin, ref modelorg);
	        clmodel = currententity.model;
	        r_pcurrentvertbase = clmodel.vertexes;

	        R_RecursiveWorldNode (clmodel.nodes[0], 15);

        // if the driver wants the polygons back to front, play the visible ones back
        // in that order
	        if (r_worldpolysbacktofront)
	        {
		        for (i=numbtofpolys-1 ; i>=0 ; i--)
		        {
			        R_RenderPoly (btofpolys[i].psurf, btofpolys[i].clipflags);
		        }
	        }
        }
    }
}
