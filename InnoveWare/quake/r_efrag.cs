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
// r_efrag.c

namespace quake
{
    public partial class render
    {
        static model.node_or_leaf_t r_pefragtopnode;

        //===========================================================================

        /*
        ===============================================================================

					        ENTITY FRAGMENT FUNCTIONS

        ===============================================================================
        */

        static efrag_t  lastlink;
        
        static double[] r_emins = new double[3], r_emaxs = new double[3];

        static entity_t r_addent;

        /*
        ================
        R_RemoveEfrags

        Call when removing an object from the world or moving it to another position
        ================
        */
        public static void R_RemoveEfrags(entity_t ent)
        {
	        efrag_t		ef, old, walk, prev;
        	
	        ef = ent.efrag;
        	
	        while (ef != null)
	        {
		        prev = ef.leaf.efrags;
		        while (true)
		        {
			        walk = prev;
			        if (walk == null)
				        break;
			        if (walk == ef)
			        {	// remove this fragment
				        prev = ef.leafnext;
				        break;
			        }
			        else
				        prev = walk.leafnext;
		        }
        				
		        old = ef;
		        ef = ef.entnext;
        		
	        // put it on the free list
		        old.entnext = client.cl.free_efrags;
		        client.cl.free_efrags = old;
	        }
        	
	        ent.efrag = null;
        }

        /*
        ===================
        R_SplitEntityOnNode
        ===================
        */
        static void R_SplitEntityOnNode(model.node_or_leaf_t node)
        {
            efrag_t         ef;
            model.mplane_t  splitplane;
            model.mleaf_t   leaf;
            int             sides;

            if (node.contents == bspfile.CONTENTS_SOLID)
            {
                return;
            }

            // add an efrag if the node is a leaf

            if (node.contents < 0)
            {
                if (r_pefragtopnode == null)
                    r_pefragtopnode = node;

                leaf = (model.mleaf_t)node;

                // grab an efrag off the free list
                ef = client.cl.free_efrags;
                if (ef == null)
                {
                    console.Con_Printf("Too many efrags!\n");
                    return;		// no free fragments...
                }
                client.cl.free_efrags = client.cl.free_efrags.entnext;

                ef.entity = r_addent;

                // add the entity link
                if (lastlink == null)
                    r_addent.efrag = ef;
                else
                    lastlink.entnext = ef;
                lastlink = ef;
                ef.entnext = null;

                // set the leaf links
                ef.leaf = leaf;
                ef.leafnext = leaf.efrags;
                leaf.efrags = ef;

                return;
            }

            // NODE_MIXED
            model.mnode_t _node = (model.mnode_t)node;
            splitplane = _node.plane;
            sides = mathlib.BOX_ON_PLANE_SIDE(r_emins, r_emaxs, splitplane);

            if (sides == 3)
            {
                // split on this plane
                // if this is the first splitter of this bmodel, remember it
                if (r_pefragtopnode == null)
                    r_pefragtopnode = node;
            }

            // recurse down the contacted sides
            if ((sides & 1) != 0)
                R_SplitEntityOnNode(_node.children[0]);

            if ((sides & 2) != 0)
                R_SplitEntityOnNode(_node.children[1]);
        }

        /*
        ===================
        R_SplitEntityOnNode2
        ===================
        */
        static void R_SplitEntityOnNode2(model.node_or_leaf_t node)
        {
            model.mplane_t splitplane;
            int sides;

            if (node.visframe != r_visframecount)
                return;

            if (node.contents < 0)
            {
                if (node.contents != bspfile.CONTENTS_SOLID)
                    r_pefragtopnode = node; // we've reached a non-solid leaf, so it's
                //  visible and not BSP clipped
                return;
            }

            model.mnode_t _node = (model.mnode_t)node;
            splitplane = _node.plane;
            sides = mathlib.BOX_ON_PLANE_SIDE(r_emins, r_emaxs, splitplane);

            if (sides == 3)
            {
                // remember first splitter
                r_pefragtopnode = node;
                return;
            }

            // not split yet; recurse down the contacted side
            if ((sides & 1) != 0)
                R_SplitEntityOnNode2(_node.children[0]);
            else
                R_SplitEntityOnNode2(_node.children[1]);
        }

        /*
        ===========
        R_AddEfrags
        ===========
        */
        public static void R_AddEfrags(entity_t ent)
        {
            model.model_t entmodel;
            int i;

            if (ent.model == null)
                return;

            if (ent == client.cl_entities[0])
                return;		// never add the world

            r_addent = ent;

            lastlink = null;
            r_pefragtopnode = null;

            entmodel = ent.model;

            for (i = 0; i < 3; i++)
            {
                r_emins[i] = ent.origin[i] + entmodel.mins[i];
                r_emaxs[i] = ent.origin[i] + entmodel.maxs[i];
            }

            R_SplitEntityOnNode(client.cl.worldmodel.nodes[0]);

            ent.topnode = r_pefragtopnode;
        }

        /*
        ================
        R_StoreEfrags

        // FIXME: a lot of this goes away with edge-based
        ================
        */
        static void R_StoreEfrags(ref efrag_t ppefrag)
        {
	        entity_t	    pent;
	        model.model_t	clmodel;
	        efrag_t		    pefrag;

            while ((pefrag = ppefrag) != null)
            {
                pent = pefrag.entity;
                clmodel = pent.model;

                switch (clmodel.type)
                {
                case model.modtype_t.mod_alias:
                case model.modtype_t.mod_brush:
                case model.modtype_t.mod_sprite:
                    pent = pefrag.entity;

                    if ((pent.visframe != r_framecount) &&
                        (client.cl_numvisedicts < client.MAX_VISEDICTS))
                    {
                        client.cl_visedicts[client.cl_numvisedicts++] = pent;

                        // mark that we've recorded this entity for this frame
                        pent.visframe = r_framecount;
                    }

                    ppefrag = pefrag.leafnext;
                    break;

                default:
                    sys_linux.Sys_Error("R_StoreEfrags: Bad entity type " + clmodel.type + "\n");
                    break;
                }
            }
        }
    }
}
