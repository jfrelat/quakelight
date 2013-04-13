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
// r_local.h -- private refresh defs

namespace quake
{
    public partial class render
    {
        public const double ALIAS_BASE_SIZE_RATIO = 1.0 / 11.0;
        // normalizing factor so player model works out to about
        //  1 pixel per triangle

        public const int BMODEL_FULLY_CLIPPED = 0x10; // value returned by R_BmodelCheckBBox ()
        //  if bbox is trivially rejected

        //===========================================================================
        // viewmodel lighting

        public class alight_t
        {
            public int ambientlight;
            public int shadelight;
            public double[] plightvec;
        };

        //===========================================================================
        // clipped bmodel edges

        public class bedge_t
        {
            public model.mvertex_t[]    v = new model.mvertex_t[2];
            public bedge_t              pnext;
        };

        public class auxvert_t
        {
            public double[] fv = new double[3];		// viewspace x, y
        };

        //===========================================================================

        public const double XCENTERING = 1.0 / 2.0;
        public const double YCENTERING = 1.0 / 2.0;

        public const double CLIP_EPSILON = 0.001;

        public const double BACKFACE_EPSILON = 0.01;

        //===========================================================================

        public const int DIST_NOT_SET = 98765;

        // !!! if this is changed, it must be changed in asm_draw.h too !!!
        public class clipplane_t
        {
            public double[]     normal = new double[3];
            public double       dist;
            public clipplane_t  next;
            public bool         leftedge;
            public bool         rightedge;
            byte[]              reserved = new byte[2];
        };

        // !!! if this is changed, it must be changed in asm_draw.h too !!!
        public const double NEAR_CLIP = 0.01;

        public const int MAXBVERTINDEXES = 1000;	// new clipped vertices when clipping bmodels
        //  to the world BSP

        public class btofpoly_t
        {
            public int              clipflags;
            public model.msurface_t psurf;
        };

        public const int MAX_BTOFPOLYS = 5000;	// FIXME: tune this

        //=========================================================
        // Alias models
        //=========================================================

        public const int MAXALIASVERTS = 2000;	// TODO: tune this
        public const int ALIAS_Z_CLIP_PLANE = 5;

        //=========================================================
        // turbulence stuff

        public const int AMP = 8 * 0x10000;
        public const int AMP2 = 3;
        public const int SPEED = 20;
    }
}
