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

// refresh.h -- public interface to refresh functions

namespace quake
{
    public partial class render
    {
        public const int	MAXCLIPPLANES	= 11;

        public const int	TOP_RANGE		= 16;			// soldier uniform colors
        public const int	BOTTOM_RANGE	= 96;

        //=============================================================================

        public class efrag_t
        {
	        public model.mleaf_t	leaf;
            public efrag_t          leafnext;
            public entity_t         entity;
            public efrag_t          entnext;
        };
        
        public class entity_t
        {
	        public bool				        forcelink;		// model changed

	        int						update_type;

	        public quakedef.entity_state_t	baseline = new quakedef.entity_state_t();		// to fill in defaults in updates

	        public double				    msgtime;		// time of last update
	        public double[][]			    msg_origins = { new double[3], new double[3] };	// last two updates (0 is newest)	
	        public double[]				    origin = new double[3];
            public double[][]               msg_angles = { new double[3], new double[3] };	// last two updates (0 is newest)
            public double[]                 angles = new double[3];	
	        public model.model_t		    model;			// NULL = no model
	        public efrag_t			        efrag;			// linked list of efrags
	        public int						frame;
	        public double					syncbase;		// for client-side animations
	        public byte[]				    colormap;
	        public int					    effects;		// light, particals, etc
	        public int						skinnum;		// for Alias models
	        public int						visframe;		// last frame this entity was
											                //  found in an active leaf
        											
	        int						dlightframe;	// dynamic lighting
	        int						dlightbits;
        	
        // FIXME: could turn these into a union
	        public int						trivial_accept;
	        public model.node_or_leaf_t		topnode;		// for bmodels, first world node
											        //  that splits bmodel, or NULL if
											        //  not split
        };

        // !!! if this is changed, it must be changed in asm_draw.h too !!!
        public class refdef_t
        {
	        public vid.vrect_t	vrect = new vid.vrect_t();				// subwindow in video for refresh
									        // FIXME: not need vrect next field here?
	        public vid.vrect_t	aliasvrect = new vid.vrect_t();			// scaled Alias version
	        public int			vrectright, vrectbottom;	// right & bottom screen coords
	        public int			aliasvrectright, aliasvrectbottom;	// scaled Alias versions
	        public double		vrectrightedge;			// rightmost right edge we care about,
										        //  for use in edge list
	        public double		fvrectx, fvrecty;		// for floating-point compares
            public double       fvrectx_adj, fvrecty_adj; // left and top edges, for clamping
            public int          vrect_x_adj_shift20;	// (vrect.x + 0.5 - epsilon) << 20
            public int          vrectright_adj_shift20;	// (vrectright + 0.5 - epsilon) << 20
            public double       fvrectright_adj, fvrectbottom_adj;
										        // right and bottom edges, for clamping
	        public double		fvrectright;			// rightmost edge, for Alias clamping
            public double       fvrectbottom;			// bottommost edge, for Alias clamping
	        public double		horizontalFieldOfView;	// at Z = 1.0, this many X is visible 
										        // 2.0 = 90 degrees
	        public double	    xOrigin;			// should probably allways be 0.5
            public double       yOrigin;			// between be around 0.3 to 0.5

	        public double[]	    vieworg = new double[3];
	        public double[]	    viewangles = new double[3];
        	
	        public double		fov_x, fov_y;

	        public int			ambientlight;
        };
    }
}
