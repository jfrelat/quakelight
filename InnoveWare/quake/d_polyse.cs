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
// d_polyset.c: routines for drawing sets of polygons sharing the same
// texture (used for Alias models)

namespace quake
{
    public partial class draw
    {
        // TODO: put in span spilling to shrink list size
        // !!! if this is changed, it must be changed in d_polysa.s too !!!
        public const int DPS_MAXSPANS			= render.MAXHEIGHT+1;
									        // 1 extra for spanpackage that marks end

        // !!! if this is changed, it must be changed in asm_draw.h too !!!
        public class spanpackage_t {
	        public int		pdest;
	        public int		pz;
	        public int		count;
	        public int		ptex;
            public int      sfrac, tfrac, light, zi;
        };

        public class edgetable {
	        public int		isflattop;
            public int      numleftedges;
            public int[]    pleftedgevert0;
            public int[]    pleftedgevert1;
            public int[]    pleftedgevert2;
            public int      numrightedges;
            public int[]    prightedgevert0;
            public int[]    prightedgevert1;
            public int[]    prightedgevert2;

            public edgetable(int isflattop, int numleftedges, int[] pleftedgevert0, int[] pleftedgevert1, int[] pleftedgevert2, int numrightedges, int[] prightedgevert0, int[] prightedgevert1, int[] prightedgevert2)
            {
                this.isflattop = isflattop;
                this.numleftedges = numleftedges;
                this.pleftedgevert0 = pleftedgevert0;
                this.pleftedgevert1 = pleftedgevert1;
                this.pleftedgevert2 = pleftedgevert2;
                this.numrightedges = numrightedges;
                this.prightedgevert0 = prightedgevert0;
                this.prightedgevert1 = prightedgevert1;
                this.prightedgevert2 = prightedgevert2;
            }
        };

        static int[]	    r_p0 = new int[6], r_p1 = new int[6], r_p2 = new int[6];

        static int          d_pcolormap;

        static int		    d_aflatcolor;
        static int			d_xdenom;

        static edgetable	pedgetable;

        static edgetable[]	edgetables = {
	        new edgetable(0, 1, r_p0, r_p2, null, 2, r_p0, r_p1, r_p2 ),
	        new edgetable(0, 2, r_p1, r_p0, r_p2,   1, r_p1, r_p2, null),
	        new edgetable(1, 1, r_p0, r_p2, null, 1, r_p1, r_p2, null),
	        new edgetable(0, 1, r_p1, r_p0, null, 2, r_p1, r_p2, r_p0 ),
	        new edgetable(0, 2, r_p0, r_p2, r_p1,   1, r_p0, r_p1, null),
	        new edgetable(0, 1, r_p2, r_p1, null, 1, r_p2, r_p0, null),
	        new edgetable(0, 1, r_p2, r_p1, null, 2, r_p2, r_p0, r_p1 ),
	        new edgetable(0, 2, r_p2, r_p1, r_p0,   1, r_p2, r_p0, null),
	        new edgetable(0, 1, r_p1, r_p0, null, 1, r_p1, r_p2, null),
	        new edgetable(1, 1, r_p2, r_p1, null, 1, r_p0, r_p1, null),
	        new edgetable(1, 1, r_p1, r_p0, null, 1, r_p2, r_p0, null),
	        new edgetable(0, 1, r_p0, r_p2, null, 1, r_p0, r_p1, null),
        };

        // FIXME: some of these can become statics
        static int			    a_sstepxfrac, a_tstepxfrac, r_lstepx, a_ststepxwhole;
        static int			    r_sstepx, r_tstepx, r_lstepy, r_sstepy, r_tstepy;
        static int			    r_zistepx, r_zistepy;
        static int              d_aspancount, d_countextrastep;

        static spanpackage_t[]	a_spans;
        static spanpackage_t    d_edgespanpackage;
        static int	            d_pedgespanpackage;
        static int				ystart;
        static int				d_pdest, d_ptex;
        static int				d_pz;
        static int				d_sfrac, d_tfrac, d_light, d_zi;
        static int				d_ptexextrastep, d_sfracextrastep;
        static int				d_tfracextrastep, d_lightextrastep, d_pdestextrastep;
        static int              d_lightbasestep, d_pdestbasestep, d_ptexbasestep;
        static int              d_sfracbasestep, d_tfracbasestep;
        static int              d_ziextrastep, d_zibasestep;
        static int              d_pzextrastep, d_pzbasestep;

        public class adivtab_t {
	        public int	quotient;
            public int  remainder;

            public adivtab_t(int quotient, int remainder)
            {
                this.quotient = quotient;
                this.remainder = remainder;
            }
        };

        static adivtab_t[]	adivtab = {
            // table of quotients and remainders for [-15...16] / [-15...16]

            // numerator = -15
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(1, -2),
            new adivtab_t(1, -3),
            new adivtab_t(1, -4),
            new adivtab_t(1, -5),
            new adivtab_t(1, -6),
            new adivtab_t(1, -7),
            new adivtab_t(2, -1),
            new adivtab_t(2, -3),
            new adivtab_t(3, 0),
            new adivtab_t(3, -3),
            new adivtab_t(5, 0),
            new adivtab_t(7, -1),
            new adivtab_t(15, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-15, 0),
            new adivtab_t(-8, 1),
            new adivtab_t(-5, 0),
            new adivtab_t(-4, 1),
            new adivtab_t(-3, 0),
            new adivtab_t(-3, 3),
            new adivtab_t(-3, 6),
            new adivtab_t(-2, 1),
            new adivtab_t(-2, 3),
            new adivtab_t(-2, 5),
            new adivtab_t(-2, 7),
            new adivtab_t(-2, 9),
            new adivtab_t(-2, 11),
            new adivtab_t(-2, 13),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            // numerator = -14
            new adivtab_t(0, -14),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(1, -2),
            new adivtab_t(1, -3),
            new adivtab_t(1, -4),
            new adivtab_t(1, -5),
            new adivtab_t(1, -6),
            new adivtab_t(2, 0),
            new adivtab_t(2, -2),
            new adivtab_t(2, -4),
            new adivtab_t(3, -2),
            new adivtab_t(4, -2),
            new adivtab_t(7, 0),
            new adivtab_t(14, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-14, 0),
            new adivtab_t(-7, 0),
            new adivtab_t(-5, 1),
            new adivtab_t(-4, 2),
            new adivtab_t(-3, 1),
            new adivtab_t(-3, 4),
            new adivtab_t(-2, 0),
            new adivtab_t(-2, 2),
            new adivtab_t(-2, 4),
            new adivtab_t(-2, 6),
            new adivtab_t(-2, 8),
            new adivtab_t(-2, 10),
            new adivtab_t(-2, 12),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            // numerator = -13
            new adivtab_t(0, -13),
            new adivtab_t(0, -13),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(1, -2),
            new adivtab_t(1, -3),
            new adivtab_t(1, -4),
            new adivtab_t(1, -5),
            new adivtab_t(1, -6),
            new adivtab_t(2, -1),
            new adivtab_t(2, -3),
            new adivtab_t(3, -1),
            new adivtab_t(4, -1),
            new adivtab_t(6, -1),
            new adivtab_t(13, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-13, 0),
            new adivtab_t(-7, 1),
            new adivtab_t(-5, 2),
            new adivtab_t(-4, 3),
            new adivtab_t(-3, 2),
            new adivtab_t(-3, 5),
            new adivtab_t(-2, 1),
            new adivtab_t(-2, 3),
            new adivtab_t(-2, 5),
            new adivtab_t(-2, 7),
            new adivtab_t(-2, 9),
            new adivtab_t(-2, 11),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            // numerator = -12
            new adivtab_t(0, -12),
            new adivtab_t(0, -12),
            new adivtab_t(0, -12),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(1, -2),
            new adivtab_t(1, -3),
            new adivtab_t(1, -4),
            new adivtab_t(1, -5),
            new adivtab_t(2, 0),
            new adivtab_t(2, -2),
            new adivtab_t(3, 0),
            new adivtab_t(4, 0),
            new adivtab_t(6, 0),
            new adivtab_t(12, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-12, 0),
            new adivtab_t(-6, 0),
            new adivtab_t(-4, 0),
            new adivtab_t(-3, 0),
            new adivtab_t(-3, 3),
            new adivtab_t(-2, 0),
            new adivtab_t(-2, 2),
            new adivtab_t(-2, 4),
            new adivtab_t(-2, 6),
            new adivtab_t(-2, 8),
            new adivtab_t(-2, 10),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            // numerator = -11
            new adivtab_t(0, -11),
            new adivtab_t(0, -11),
            new adivtab_t(0, -11),
            new adivtab_t(0, -11),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(1, -2),
            new adivtab_t(1, -3),
            new adivtab_t(1, -4),
            new adivtab_t(1, -5),
            new adivtab_t(2, -1),
            new adivtab_t(2, -3),
            new adivtab_t(3, -2),
            new adivtab_t(5, -1),
            new adivtab_t(11, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-11, 0),
            new adivtab_t(-6, 1),
            new adivtab_t(-4, 1),
            new adivtab_t(-3, 1),
            new adivtab_t(-3, 4),
            new adivtab_t(-2, 1),
            new adivtab_t(-2, 3),
            new adivtab_t(-2, 5),
            new adivtab_t(-2, 7),
            new adivtab_t(-2, 9),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            new adivtab_t(-1, 5),
            // numerator = -10
            new adivtab_t(0, -10),
            new adivtab_t(0, -10),
            new adivtab_t(0, -10),
            new adivtab_t(0, -10),
            new adivtab_t(0, -10),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(1, -2),
            new adivtab_t(1, -3),
            new adivtab_t(1, -4),
            new adivtab_t(2, 0),
            new adivtab_t(2, -2),
            new adivtab_t(3, -1),
            new adivtab_t(5, 0),
            new adivtab_t(10, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-10, 0),
            new adivtab_t(-5, 0),
            new adivtab_t(-4, 2),
            new adivtab_t(-3, 2),
            new adivtab_t(-2, 0),
            new adivtab_t(-2, 2),
            new adivtab_t(-2, 4),
            new adivtab_t(-2, 6),
            new adivtab_t(-2, 8),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            new adivtab_t(-1, 5),
            new adivtab_t(-1, 6),
            // numerator = -9
            new adivtab_t(0, -9),
            new adivtab_t(0, -9),
            new adivtab_t(0, -9),
            new adivtab_t(0, -9),
            new adivtab_t(0, -9),
            new adivtab_t(0, -9),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(1, -2),
            new adivtab_t(1, -3),
            new adivtab_t(1, -4),
            new adivtab_t(2, -1),
            new adivtab_t(3, 0),
            new adivtab_t(4, -1),
            new adivtab_t(9, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-9, 0),
            new adivtab_t(-5, 1),
            new adivtab_t(-3, 0),
            new adivtab_t(-3, 3),
            new adivtab_t(-2, 1),
            new adivtab_t(-2, 3),
            new adivtab_t(-2, 5),
            new adivtab_t(-2, 7),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            new adivtab_t(-1, 5),
            new adivtab_t(-1, 6),
            new adivtab_t(-1, 7),
            // numerator = -8
            new adivtab_t(0, -8),
            new adivtab_t(0, -8),
            new adivtab_t(0, -8),
            new adivtab_t(0, -8),
            new adivtab_t(0, -8),
            new adivtab_t(0, -8),
            new adivtab_t(0, -8),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(1, -2),
            new adivtab_t(1, -3),
            new adivtab_t(2, 0),
            new adivtab_t(2, -2),
            new adivtab_t(4, 0),
            new adivtab_t(8, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-8, 0),
            new adivtab_t(-4, 0),
            new adivtab_t(-3, 1),
            new adivtab_t(-2, 0),
            new adivtab_t(-2, 2),
            new adivtab_t(-2, 4),
            new adivtab_t(-2, 6),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            new adivtab_t(-1, 5),
            new adivtab_t(-1, 6),
            new adivtab_t(-1, 7),
            new adivtab_t(-1, 8),
            // numerator = -7
            new adivtab_t(0, -7),
            new adivtab_t(0, -7),
            new adivtab_t(0, -7),
            new adivtab_t(0, -7),
            new adivtab_t(0, -7),
            new adivtab_t(0, -7),
            new adivtab_t(0, -7),
            new adivtab_t(0, -7),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(1, -2),
            new adivtab_t(1, -3),
            new adivtab_t(2, -1),
            new adivtab_t(3, -1),
            new adivtab_t(7, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-7, 0),
            new adivtab_t(-4, 1),
            new adivtab_t(-3, 2),
            new adivtab_t(-2, 1),
            new adivtab_t(-2, 3),
            new adivtab_t(-2, 5),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            new adivtab_t(-1, 5),
            new adivtab_t(-1, 6),
            new adivtab_t(-1, 7),
            new adivtab_t(-1, 8),
            new adivtab_t(-1, 9),
            // numerator = -6
            new adivtab_t(0, -6),
            new adivtab_t(0, -6),
            new adivtab_t(0, -6),
            new adivtab_t(0, -6),
            new adivtab_t(0, -6),
            new adivtab_t(0, -6),
            new adivtab_t(0, -6),
            new adivtab_t(0, -6),
            new adivtab_t(0, -6),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(1, -2),
            new adivtab_t(2, 0),
            new adivtab_t(3, 0),
            new adivtab_t(6, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-6, 0),
            new adivtab_t(-3, 0),
            new adivtab_t(-2, 0),
            new adivtab_t(-2, 2),
            new adivtab_t(-2, 4),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            new adivtab_t(-1, 5),
            new adivtab_t(-1, 6),
            new adivtab_t(-1, 7),
            new adivtab_t(-1, 8),
            new adivtab_t(-1, 9),
            new adivtab_t(-1, 10),
            // numerator = -5
            new adivtab_t(0, -5),
            new adivtab_t(0, -5),
            new adivtab_t(0, -5),
            new adivtab_t(0, -5),
            new adivtab_t(0, -5),
            new adivtab_t(0, -5),
            new adivtab_t(0, -5),
            new adivtab_t(0, -5),
            new adivtab_t(0, -5),
            new adivtab_t(0, -5),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(1, -2),
            new adivtab_t(2, -1),
            new adivtab_t(5, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-5, 0),
            new adivtab_t(-3, 1),
            new adivtab_t(-2, 1),
            new adivtab_t(-2, 3),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            new adivtab_t(-1, 5),
            new adivtab_t(-1, 6),
            new adivtab_t(-1, 7),
            new adivtab_t(-1, 8),
            new adivtab_t(-1, 9),
            new adivtab_t(-1, 10),
            new adivtab_t(-1, 11),
            // numerator = -4
            new adivtab_t(0, -4),
            new adivtab_t(0, -4),
            new adivtab_t(0, -4),
            new adivtab_t(0, -4),
            new adivtab_t(0, -4),
            new adivtab_t(0, -4),
            new adivtab_t(0, -4),
            new adivtab_t(0, -4),
            new adivtab_t(0, -4),
            new adivtab_t(0, -4),
            new adivtab_t(0, -4),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(2, 0),
            new adivtab_t(4, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-4, 0),
            new adivtab_t(-2, 0),
            new adivtab_t(-2, 2),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            new adivtab_t(-1, 5),
            new adivtab_t(-1, 6),
            new adivtab_t(-1, 7),
            new adivtab_t(-1, 8),
            new adivtab_t(-1, 9),
            new adivtab_t(-1, 10),
            new adivtab_t(-1, 11),
            new adivtab_t(-1, 12),
            // numerator = -3
            new adivtab_t(0, -3),
            new adivtab_t(0, -3),
            new adivtab_t(0, -3),
            new adivtab_t(0, -3),
            new adivtab_t(0, -3),
            new adivtab_t(0, -3),
            new adivtab_t(0, -3),
            new adivtab_t(0, -3),
            new adivtab_t(0, -3),
            new adivtab_t(0, -3),
            new adivtab_t(0, -3),
            new adivtab_t(0, -3),
            new adivtab_t(1, 0),
            new adivtab_t(1, -1),
            new adivtab_t(3, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-3, 0),
            new adivtab_t(-2, 1),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            new adivtab_t(-1, 5),
            new adivtab_t(-1, 6),
            new adivtab_t(-1, 7),
            new adivtab_t(-1, 8),
            new adivtab_t(-1, 9),
            new adivtab_t(-1, 10),
            new adivtab_t(-1, 11),
            new adivtab_t(-1, 12),
            new adivtab_t(-1, 13),
            // numerator = -2
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(0, -2),
            new adivtab_t(1, 0),
            new adivtab_t(2, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-2, 0),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            new adivtab_t(-1, 5),
            new adivtab_t(-1, 6),
            new adivtab_t(-1, 7),
            new adivtab_t(-1, 8),
            new adivtab_t(-1, 9),
            new adivtab_t(-1, 10),
            new adivtab_t(-1, 11),
            new adivtab_t(-1, 12),
            new adivtab_t(-1, 13),
            new adivtab_t(-1, 14),
            // numerator = -1
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(0, -1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 0),
            new adivtab_t(-1, 0),
            new adivtab_t(-1, 1),
            new adivtab_t(-1, 2),
            new adivtab_t(-1, 3),
            new adivtab_t(-1, 4),
            new adivtab_t(-1, 5),
            new adivtab_t(-1, 6),
            new adivtab_t(-1, 7),
            new adivtab_t(-1, 8),
            new adivtab_t(-1, 9),
            new adivtab_t(-1, 10),
            new adivtab_t(-1, 11),
            new adivtab_t(-1, 12),
            new adivtab_t(-1, 13),
            new adivtab_t(-1, 14),
            new adivtab_t(-1, 15),
            // numerator = 0
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            new adivtab_t(0, 0),
            // numerator = 1
            new adivtab_t(-1, -14),
            new adivtab_t(-1, -13),
            new adivtab_t(-1, -12),
            new adivtab_t(-1, -11),
            new adivtab_t(-1, -10),
            new adivtab_t(-1, -9),
            new adivtab_t(-1, -8),
            new adivtab_t(-1, -7),
            new adivtab_t(-1, -6),
            new adivtab_t(-1, -5),
            new adivtab_t(-1, -4),
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(0, 0),
            new adivtab_t(1, 0),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            new adivtab_t(0, 1),
            // numerator = 2
            new adivtab_t(-1, -13),
            new adivtab_t(-1, -12),
            new adivtab_t(-1, -11),
            new adivtab_t(-1, -10),
            new adivtab_t(-1, -9),
            new adivtab_t(-1, -8),
            new adivtab_t(-1, -7),
            new adivtab_t(-1, -6),
            new adivtab_t(-1, -5),
            new adivtab_t(-1, -4),
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, 0),
            new adivtab_t(0, 0),
            new adivtab_t(2, 0),
            new adivtab_t(1, 0),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            new adivtab_t(0, 2),
            // numerator = 3
            new adivtab_t(-1, -12),
            new adivtab_t(-1, -11),
            new adivtab_t(-1, -10),
            new adivtab_t(-1, -9),
            new adivtab_t(-1, -8),
            new adivtab_t(-1, -7),
            new adivtab_t(-1, -6),
            new adivtab_t(-1, -5),
            new adivtab_t(-1, -4),
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -1),
            new adivtab_t(-3, 0),
            new adivtab_t(0, 0),
            new adivtab_t(3, 0),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            new adivtab_t(0, 3),
            // numerator = 4
            new adivtab_t(-1, -11),
            new adivtab_t(-1, -10),
            new adivtab_t(-1, -9),
            new adivtab_t(-1, -8),
            new adivtab_t(-1, -7),
            new adivtab_t(-1, -6),
            new adivtab_t(-1, -5),
            new adivtab_t(-1, -4),
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -2),
            new adivtab_t(-2, 0),
            new adivtab_t(-4, 0),
            new adivtab_t(0, 0),
            new adivtab_t(4, 0),
            new adivtab_t(2, 0),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 4),
            new adivtab_t(0, 4),
            new adivtab_t(0, 4),
            new adivtab_t(0, 4),
            new adivtab_t(0, 4),
            new adivtab_t(0, 4),
            new adivtab_t(0, 4),
            new adivtab_t(0, 4),
            new adivtab_t(0, 4),
            new adivtab_t(0, 4),
            new adivtab_t(0, 4),
            new adivtab_t(0, 4),
            // numerator = 5
            new adivtab_t(-1, -10),
            new adivtab_t(-1, -9),
            new adivtab_t(-1, -8),
            new adivtab_t(-1, -7),
            new adivtab_t(-1, -6),
            new adivtab_t(-1, -5),
            new adivtab_t(-1, -4),
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -3),
            new adivtab_t(-2, -1),
            new adivtab_t(-3, -1),
            new adivtab_t(-5, 0),
            new adivtab_t(0, 0),
            new adivtab_t(5, 0),
            new adivtab_t(2, 1),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 5),
            new adivtab_t(0, 5),
            new adivtab_t(0, 5),
            new adivtab_t(0, 5),
            new adivtab_t(0, 5),
            new adivtab_t(0, 5),
            new adivtab_t(0, 5),
            new adivtab_t(0, 5),
            new adivtab_t(0, 5),
            new adivtab_t(0, 5),
            new adivtab_t(0, 5),
            // numerator = 6
            new adivtab_t(-1, -9),
            new adivtab_t(-1, -8),
            new adivtab_t(-1, -7),
            new adivtab_t(-1, -6),
            new adivtab_t(-1, -5),
            new adivtab_t(-1, -4),
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -4),
            new adivtab_t(-2, -2),
            new adivtab_t(-2, 0),
            new adivtab_t(-3, 0),
            new adivtab_t(-6, 0),
            new adivtab_t(0, 0),
            new adivtab_t(6, 0),
            new adivtab_t(3, 0),
            new adivtab_t(2, 0),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 6),
            new adivtab_t(0, 6),
            new adivtab_t(0, 6),
            new adivtab_t(0, 6),
            new adivtab_t(0, 6),
            new adivtab_t(0, 6),
            new adivtab_t(0, 6),
            new adivtab_t(0, 6),
            new adivtab_t(0, 6),
            new adivtab_t(0, 6),
            // numerator = 7
            new adivtab_t(-1, -8),
            new adivtab_t(-1, -7),
            new adivtab_t(-1, -6),
            new adivtab_t(-1, -5),
            new adivtab_t(-1, -4),
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -5),
            new adivtab_t(-2, -3),
            new adivtab_t(-2, -1),
            new adivtab_t(-3, -2),
            new adivtab_t(-4, -1),
            new adivtab_t(-7, 0),
            new adivtab_t(0, 0),
            new adivtab_t(7, 0),
            new adivtab_t(3, 1),
            new adivtab_t(2, 1),
            new adivtab_t(1, 3),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 7),
            new adivtab_t(0, 7),
            new adivtab_t(0, 7),
            new adivtab_t(0, 7),
            new adivtab_t(0, 7),
            new adivtab_t(0, 7),
            new adivtab_t(0, 7),
            new adivtab_t(0, 7),
            new adivtab_t(0, 7),
            // numerator = 8
            new adivtab_t(-1, -7),
            new adivtab_t(-1, -6),
            new adivtab_t(-1, -5),
            new adivtab_t(-1, -4),
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -6),
            new adivtab_t(-2, -4),
            new adivtab_t(-2, -2),
            new adivtab_t(-2, 0),
            new adivtab_t(-3, -1),
            new adivtab_t(-4, 0),
            new adivtab_t(-8, 0),
            new adivtab_t(0, 0),
            new adivtab_t(8, 0),
            new adivtab_t(4, 0),
            new adivtab_t(2, 2),
            new adivtab_t(2, 0),
            new adivtab_t(1, 3),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 8),
            new adivtab_t(0, 8),
            new adivtab_t(0, 8),
            new adivtab_t(0, 8),
            new adivtab_t(0, 8),
            new adivtab_t(0, 8),
            new adivtab_t(0, 8),
            new adivtab_t(0, 8),
            // numerator = 9
            new adivtab_t(-1, -6),
            new adivtab_t(-1, -5),
            new adivtab_t(-1, -4),
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -7),
            new adivtab_t(-2, -5),
            new adivtab_t(-2, -3),
            new adivtab_t(-2, -1),
            new adivtab_t(-3, -3),
            new adivtab_t(-3, 0),
            new adivtab_t(-5, -1),
            new adivtab_t(-9, 0),
            new adivtab_t(0, 0),
            new adivtab_t(9, 0),
            new adivtab_t(4, 1),
            new adivtab_t(3, 0),
            new adivtab_t(2, 1),
            new adivtab_t(1, 4),
            new adivtab_t(1, 3),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 9),
            new adivtab_t(0, 9),
            new adivtab_t(0, 9),
            new adivtab_t(0, 9),
            new adivtab_t(0, 9),
            new adivtab_t(0, 9),
            new adivtab_t(0, 9),
            // numerator = 10
            new adivtab_t(-1, -5),
            new adivtab_t(-1, -4),
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -8),
            new adivtab_t(-2, -6),
            new adivtab_t(-2, -4),
            new adivtab_t(-2, -2),
            new adivtab_t(-2, 0),
            new adivtab_t(-3, -2),
            new adivtab_t(-4, -2),
            new adivtab_t(-5, 0),
            new adivtab_t(-10, 0),
            new adivtab_t(0, 0),
            new adivtab_t(10, 0),
            new adivtab_t(5, 0),
            new adivtab_t(3, 1),
            new adivtab_t(2, 2),
            new adivtab_t(2, 0),
            new adivtab_t(1, 4),
            new adivtab_t(1, 3),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 10),
            new adivtab_t(0, 10),
            new adivtab_t(0, 10),
            new adivtab_t(0, 10),
            new adivtab_t(0, 10),
            new adivtab_t(0, 10),
            // numerator = 11
            new adivtab_t(-1, -4),
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -9),
            new adivtab_t(-2, -7),
            new adivtab_t(-2, -5),
            new adivtab_t(-2, -3),
            new adivtab_t(-2, -1),
            new adivtab_t(-3, -4),
            new adivtab_t(-3, -1),
            new adivtab_t(-4, -1),
            new adivtab_t(-6, -1),
            new adivtab_t(-11, 0),
            new adivtab_t(0, 0),
            new adivtab_t(11, 0),
            new adivtab_t(5, 1),
            new adivtab_t(3, 2),
            new adivtab_t(2, 3),
            new adivtab_t(2, 1),
            new adivtab_t(1, 5),
            new adivtab_t(1, 4),
            new adivtab_t(1, 3),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 11),
            new adivtab_t(0, 11),
            new adivtab_t(0, 11),
            new adivtab_t(0, 11),
            new adivtab_t(0, 11),
            // numerator = 12
            new adivtab_t(-1, -3),
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -10),
            new adivtab_t(-2, -8),
            new adivtab_t(-2, -6),
            new adivtab_t(-2, -4),
            new adivtab_t(-2, -2),
            new adivtab_t(-2, 0),
            new adivtab_t(-3, -3),
            new adivtab_t(-3, 0),
            new adivtab_t(-4, 0),
            new adivtab_t(-6, 0),
            new adivtab_t(-12, 0),
            new adivtab_t(0, 0),
            new adivtab_t(12, 0),
            new adivtab_t(6, 0),
            new adivtab_t(4, 0),
            new adivtab_t(3, 0),
            new adivtab_t(2, 2),
            new adivtab_t(2, 0),
            new adivtab_t(1, 5),
            new adivtab_t(1, 4),
            new adivtab_t(1, 3),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 12),
            new adivtab_t(0, 12),
            new adivtab_t(0, 12),
            new adivtab_t(0, 12),
            // numerator = 13
            new adivtab_t(-1, -2),
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -11),
            new adivtab_t(-2, -9),
            new adivtab_t(-2, -7),
            new adivtab_t(-2, -5),
            new adivtab_t(-2, -3),
            new adivtab_t(-2, -1),
            new adivtab_t(-3, -5),
            new adivtab_t(-3, -2),
            new adivtab_t(-4, -3),
            new adivtab_t(-5, -2),
            new adivtab_t(-7, -1),
            new adivtab_t(-13, 0),
            new adivtab_t(0, 0),
            new adivtab_t(13, 0),
            new adivtab_t(6, 1),
            new adivtab_t(4, 1),
            new adivtab_t(3, 1),
            new adivtab_t(2, 3),
            new adivtab_t(2, 1),
            new adivtab_t(1, 6),
            new adivtab_t(1, 5),
            new adivtab_t(1, 4),
            new adivtab_t(1, 3),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 13),
            new adivtab_t(0, 13),
            new adivtab_t(0, 13),
            // numerator = 14
            new adivtab_t(-1, -1),
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -12),
            new adivtab_t(-2, -10),
            new adivtab_t(-2, -8),
            new adivtab_t(-2, -6),
            new adivtab_t(-2, -4),
            new adivtab_t(-2, -2),
            new adivtab_t(-2, 0),
            new adivtab_t(-3, -4),
            new adivtab_t(-3, -1),
            new adivtab_t(-4, -2),
            new adivtab_t(-5, -1),
            new adivtab_t(-7, 0),
            new adivtab_t(-14, 0),
            new adivtab_t(0, 0),
            new adivtab_t(14, 0),
            new adivtab_t(7, 0),
            new adivtab_t(4, 2),
            new adivtab_t(3, 2),
            new adivtab_t(2, 4),
            new adivtab_t(2, 2),
            new adivtab_t(2, 0),
            new adivtab_t(1, 6),
            new adivtab_t(1, 5),
            new adivtab_t(1, 4),
            new adivtab_t(1, 3),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 14),
            new adivtab_t(0, 14),
            // numerator = 15
            new adivtab_t(-1, 0),
            new adivtab_t(-2, -13),
            new adivtab_t(-2, -11),
            new adivtab_t(-2, -9),
            new adivtab_t(-2, -7),
            new adivtab_t(-2, -5),
            new adivtab_t(-2, -3),
            new adivtab_t(-2, -1),
            new adivtab_t(-3, -6),
            new adivtab_t(-3, -3),
            new adivtab_t(-3, 0),
            new adivtab_t(-4, -1),
            new adivtab_t(-5, 0),
            new adivtab_t(-8, -1),
            new adivtab_t(-15, 0),
            new adivtab_t(0, 0),
            new adivtab_t(15, 0),
            new adivtab_t(7, 1),
            new adivtab_t(5, 0),
            new adivtab_t(3, 3),
            new adivtab_t(3, 0),
            new adivtab_t(2, 3),
            new adivtab_t(2, 1),
            new adivtab_t(1, 7),
            new adivtab_t(1, 6),
            new adivtab_t(1, 5),
            new adivtab_t(1, 4),
            new adivtab_t(1, 3),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
            new adivtab_t(0, 15),
            // numerator = 16
            new adivtab_t(-2, -14),
            new adivtab_t(-2, -12),
            new adivtab_t(-2, -10),
            new adivtab_t(-2, -8),
            new adivtab_t(-2, -6),
            new adivtab_t(-2, -4),
            new adivtab_t(-2, -2),
            new adivtab_t(-2, 0),
            new adivtab_t(-3, -5),
            new adivtab_t(-3, -2),
            new adivtab_t(-4, -4),
            new adivtab_t(-4, 0),
            new adivtab_t(-6, -2),
            new adivtab_t(-8, 0),
            new adivtab_t(-16, 0),
            new adivtab_t(0, 0),
            new adivtab_t(16, 0),
            new adivtab_t(8, 0),
            new adivtab_t(5, 1),
            new adivtab_t(4, 0),
            new adivtab_t(3, 1),
            new adivtab_t(2, 4),
            new adivtab_t(2, 2),
            new adivtab_t(2, 0),
            new adivtab_t(1, 7),
            new adivtab_t(1, 6),
            new adivtab_t(1, 5),
            new adivtab_t(1, 4),
            new adivtab_t(1, 3),
            new adivtab_t(1, 2),
            new adivtab_t(1, 1),
            new adivtab_t(1, 0),
        };

        static int[]    skintable = new int[MAX_LBM_HEIGHT];
        static int      skinwidth;
        static byte[]   skinstart;

	    static spanpackage_t[]	spans;

        static void d_polyse_init()
        {
    	    spans = new spanpackage_t[DPS_MAXSPANS + 1]; // one extra because of cache line pretouching

            for (int kk = 0; kk < DPS_MAXSPANS + 1; kk++) spans[kk] = new spanpackage_t();
	        a_spans = (spanpackage_t[])spans;
        }

        /*
        ================
        D_PolysetDraw
        ================
        */
        public static void D_PolysetDraw ()
        {
	        if (render.r_affinetridesc.drawtype)
	        {
		        D_DrawSubdiv ();
	        }
	        else
	        {
		        D_DrawNonSubdiv ();
	        }
        }


        /*
        ================
        D_PolysetDrawFinalVerts
        ================
        */
        public static void D_PolysetDrawFinalVerts (finalvert_t[] pfv, int numverts)
        {
	        int		i, z;
	        int	    zbuf;

	        for (i=0 ; i<numverts ; i++)
	        {
                finalvert_t fv = pfv[i];
	        // valid triangle coordinates for filling can include the bottom and
	        // right clip edges, due to the fill rule; these shouldn't be drawn
		        if ((fv.v[0] < render.r_refdef.vrectright) &&
			        (fv.v[1] < render.r_refdef.vrectbottom))
		        {
			        z = fv.v[5]>>16;
			        zbuf = zspantable[fv.v[1]] + fv.v[0];
			        if (z >= d_pzbuffer[zbuf])
			        {
				        int		pix;
        				
				        d_pzbuffer[zbuf] = (short)z;
				        pix = skinstart[skintable[fv.v[3]>>16] + (fv.v[2]>>16)];
				        pix = render.acolormap[pix + (fv.v[4] & 0xFF00) ];
				        d_viewbuffer[d_scantable[fv.v[1]] + fv.v[0]] = (byte)pix;
			        }
		        }
	        }
        }

        /*
        ================
        D_DrawSubdiv
        ================
        */
        static void D_DrawSubdiv ()
        {
	        model.mtriangle_t[] ptri;
	        finalvert_t[]		pfv;
            finalvert_t         index0, index1, index2;
	        int				    i;
	        int				    lnumtriangles;

	        pfv = render.r_affinetridesc.pfinalverts;
            ptri = render.r_affinetridesc.ptriangles;
            lnumtriangles = render.r_affinetridesc.numtriangles;

	        for (i=0 ; i<lnumtriangles ; i++)
	        {
		        index0 = pfv[ptri[i].vertindex[0]];
		        index1 = pfv[ptri[i].vertindex[1]];
		        index2 = pfv[ptri[i].vertindex[2]];

		        if (((index0.v[1]-index1.v[1]) *
			         (index0.v[0]-index2.v[0]) -
			         (index0.v[0]-index1.v[0]) * 
			         (index0.v[1]-index2.v[1])) >= 0)
		        {
			        continue;
		        }

		        d_pcolormap = index0.v[4] & 0xFF00;

		        if (ptri[i].facesfront != 0)
		        {
			        D_PolysetRecursiveTriangle(index0.v, index1.v, index2.v);
		        }
		        else
		        {
			        int		s0, s1, s2;

			        s0 = index0.v[2];
			        s1 = index1.v[2];
			        s2 = index2.v[2];

                    if ((index0.flags & render.ALIAS_ONSEAM) != 0)
				        index0.v[2] += render.r_affinetridesc.seamfixupX16;
                    if ((index1.flags & render.ALIAS_ONSEAM) != 0)
                        index1.v[2] += render.r_affinetridesc.seamfixupX16;
                    if ((index2.flags & render.ALIAS_ONSEAM) != 0)
                        index2.v[2] += render.r_affinetridesc.seamfixupX16;

			        D_PolysetRecursiveTriangle(index0.v, index1.v, index2.v);

			        index0.v[2] = s0;
			        index1.v[2] = s1;
			        index2.v[2] = s2;
		        }
	        }
        }

        /*
        ================
        D_DrawNonSubdiv
        ================
        */
        static void D_DrawNonSubdiv ()
        {
	        model.mtriangle_t[] ptri;
	        finalvert_t[]		pfv;
            finalvert_t         index0, index1, index2;
	        int				    i;
	        int				    lnumtriangles;

	        pfv = render.r_affinetridesc.pfinalverts;
	        ptri = render.r_affinetridesc.ptriangles;
	        lnumtriangles = render.r_affinetridesc.numtriangles;

	        for (i=0 ; i<lnumtriangles ; i++)
	        {
		        index0 = pfv[ptri[i].vertindex[0]];
		        index1 = pfv[ptri[i].vertindex[1]];
		        index2 = pfv[ptri[i].vertindex[2]];

		        d_xdenom = (index0.v[1]-index1.v[1]) *
				        (index0.v[0]-index2.v[0]) -
				        (index0.v[0]-index1.v[0])*(index0.v[1]-index2.v[1]);

		        if (d_xdenom >= 0)
		        {
			        continue;
		        }

		        r_p0[0] = index0.v[0];		// u
		        r_p0[1] = index0.v[1];		// v
		        r_p0[2] = index0.v[2];		// s
		        r_p0[3] = index0.v[3];		// t
		        r_p0[4] = index0.v[4];		// light
		        r_p0[5] = index0.v[5];		// iz

		        r_p1[0] = index1.v[0];
		        r_p1[1] = index1.v[1];
		        r_p1[2] = index1.v[2];
		        r_p1[3] = index1.v[3];
		        r_p1[4] = index1.v[4];
		        r_p1[5] = index1.v[5];

		        r_p2[0] = index2.v[0];
		        r_p2[1] = index2.v[1];
		        r_p2[2] = index2.v[2];
		        r_p2[3] = index2.v[3];
		        r_p2[4] = index2.v[4];
		        r_p2[5] = index2.v[5];

		        if (ptri[i].facesfront == 0)
		        {
			        if ((index0.flags & render.ALIAS_ONSEAM) != 0)
				        r_p0[2] += render.r_affinetridesc.seamfixupX16;
			        if ((index1.flags & render.ALIAS_ONSEAM) != 0)
				        r_p1[2] += render.r_affinetridesc.seamfixupX16;
			        if ((index2.flags & render.ALIAS_ONSEAM) != 0)
				        r_p2[2] += render.r_affinetridesc.seamfixupX16;
		        }

		        D_PolysetSetEdgeTable ();
		        D_RasterizeAliasPolySmooth ();
	        }
        }

        /*
        ================
        D_PolysetRecursiveTriangle
        ================
        */
        static void D_PolysetRecursiveTriangle (int[] lp1, int[] lp2, int[] lp3)
        {
	        int[]   temp;
	        int		d;
	        int[]	@new = new int[6];
	        int		z;
	        int     zbuf;

	        d = lp2[0] - lp1[0];
	        if (d < -1 || d > 1)
		        goto split;
	        d = lp2[1] - lp1[1];
	        if (d < -1 || d > 1)
		        goto split;

	        d = lp3[0] - lp2[0];
	        if (d < -1 || d > 1)
		        goto split2;
	        d = lp3[1] - lp2[1];
	        if (d < -1 || d > 1)
		        goto split2;

	        d = lp1[0] - lp3[0];
	        if (d < -1 || d > 1)
		        goto split3;
	        d = lp1[1] - lp3[1];
	        if (d < -1 || d > 1)
	            goto split3;

            return;			// entire tri is filled

        split3:
            temp = lp1;
	        lp1 = lp3;
	        lp3 = lp2;
	        lp2 = temp;

	        goto split;

        split2:
	        temp = lp1;
	        lp1 = lp2;
	        lp2 = lp3;
	        lp3 = temp;

        split:
        // split this edge
	        @new[0] = (lp1[0] + lp2[0]) >> 1;
	        @new[1] = (lp1[1] + lp2[1]) >> 1;
	        @new[2] = (lp1[2] + lp2[2]) >> 1;
	        @new[3] = (lp1[3] + lp2[3]) >> 1;
	        @new[5] = (lp1[5] + lp2[5]) >> 1;

        // draw the point if splitting a leading edge
	        if (lp2[1] > lp1[1])
		        goto nodraw;
	        if ((lp2[1] == lp1[1]) && (lp2[0] < lp1[0]))
		        goto nodraw;


	        z = @new[5]>>16;
	        zbuf = zspantable[@new[1]] + @new[0];
	        if (z >= d_pzbuffer[zbuf])
	        {
		        int		pix;
        		
		        d_pzbuffer[zbuf] = (short)z;
		        pix = render.acolormap[d_pcolormap + skinstart[skintable[@new[3]>>16] + (@new[2]>>16)]];
		        d_viewbuffer[d_scantable[@new[1]] + @new[0]] = (byte)pix;
	        }

        nodraw:
        // recursively continue
            D_PolysetRecursiveTriangle (lp3, lp1, @new);
	        D_PolysetRecursiveTriangle (lp3, @new, lp2);
        }

        /*
        ================
        D_PolysetUpdateTables
        ================
        */
        public static void D_PolysetUpdateTables ()
        {
	        int		i;
	        int     s;
        	
	        if (render.r_affinetridesc.skinwidth != skinwidth ||
                render.r_affinetridesc.pskin != skinstart)
	        {
                skinwidth = render.r_affinetridesc.skinwidth;
                skinstart = render.r_affinetridesc.pskin;
		        s = 0;
		        for (i=0 ; i<MAX_LBM_HEIGHT ; i++, s+=skinwidth)
			        skintable[i] = s;
	        }
        }

        /*
        ===================
        D_PolysetScanLeftEdge
        ====================
        */
        static void D_PolysetScanLeftEdge (int height)
        {
	        do
            {
                d_edgespanpackage = a_spans[d_pedgespanpackage];

                d_edgespanpackage.pdest = d_pdest;
		        d_edgespanpackage.pz = d_pz;
		        d_edgespanpackage.count = d_aspancount;
		        d_edgespanpackage.ptex = d_ptex;

		        d_edgespanpackage.sfrac = d_sfrac;
		        d_edgespanpackage.tfrac = d_tfrac;

	        // FIXME: need to clamp l, s, t, at both ends?
		        d_edgespanpackage.light = d_light;
		        d_edgespanpackage.zi = d_zi;

		        d_pedgespanpackage++;

		        errorterm += erroradjustup;
		        if (errorterm >= 0)
		        {
			        d_pdest += d_pdestextrastep;
			        d_pz += d_pzextrastep;
			        d_aspancount += d_countextrastep;
			        d_ptex += d_ptexextrastep;
			        d_sfrac += d_sfracextrastep;
			        d_ptex += d_sfrac >> 16;

			        d_sfrac &= 0xFFFF;
			        d_tfrac += d_tfracextrastep;
			        if ((d_tfrac & 0x10000) != 0)
			        {
                        d_ptex += render.r_affinetridesc.skinwidth;
				        d_tfrac &= 0xFFFF;
			        }
			        d_light += d_lightextrastep;
			        d_zi += d_ziextrastep;
			        errorterm -= erroradjustdown;
		        }
		        else
		        {
			        d_pdest += d_pdestbasestep;
			        d_pz += d_pzbasestep;
			        d_aspancount += ubasestep;
			        d_ptex += d_ptexbasestep;
			        d_sfrac += d_sfracbasestep;
			        d_ptex += d_sfrac >> 16;
			        d_sfrac &= 0xFFFF;
			        d_tfrac += d_tfracbasestep;
			        if ((d_tfrac & 0x10000) != 0)
			        {
				        d_ptex += render.r_affinetridesc.skinwidth;
				        d_tfrac &= 0xFFFF;
			        }
			        d_light += d_lightbasestep;
			        d_zi += d_zibasestep;
		        }
	        } while (--height != 0);
        }

        /*
        ===================
        D_PolysetSetUpForLineScan
        ====================
        */
        static void D_PolysetSetUpForLineScan(int startvertu, int startvertv,
		        int endvertu, int endvertv)
        {
	        double		dm, dn;
	        int			tm, tn;
	        adivtab_t	ptemp;

        // TODO: implement x86 version

	        errorterm = -1;

	        tm = endvertu - startvertu;
	        tn = endvertv - startvertv;

	        if (((tm <= 16) && (tm >= -15)) &&
		        ((tn <= 16) && (tn >= -15)))
	        {
		        ptemp = adivtab[((tm+15) << 5) + (tn+15)];
		        ubasestep = ptemp.quotient;
		        erroradjustup = ptemp.remainder;
		        erroradjustdown = tn;
	        }
	        else
	        {
		        dm = (double)tm;
		        dn = (double)tn;

		        mathlib.FloorDivMod (dm, dn, ref ubasestep, ref erroradjustup);

		        erroradjustdown = (int)dn;
	        }
        }

        /*
        ================
        D_PolysetCalcGradients
        ================
        */
        static void D_PolysetCalcGradients (int skinwidth)
        {
	        double	xstepdenominv, ystepdenominv, t0, t1;
            double  p01_minus_p21, p11_minus_p21, p00_minus_p20, p10_minus_p20;

	        p00_minus_p20 = r_p0[0] - r_p2[0];
	        p01_minus_p21 = r_p0[1] - r_p2[1];
	        p10_minus_p20 = r_p1[0] - r_p2[0];
	        p11_minus_p21 = r_p1[1] - r_p2[1];

            xstepdenominv = 1.0 / (double)d_xdenom;

	        ystepdenominv = -xstepdenominv;

        // ceil () for light so positive steps are exaggerated, negative steps
        // diminished,  pushing us away from underflow toward overflow. Underflow is
        // very visible, overflow is very unlikely, because of ambient lighting
	        t0 = r_p0[4] - r_p2[4];
	        t1 = r_p1[4] - r_p2[4];
	        r_lstepx = (int)
			        Math.Ceiling((t1 * p01_minus_p21 - t0 * p11_minus_p21) * xstepdenominv);
	        r_lstepy = (int)
                    Math.Ceiling((t1 * p00_minus_p20 - t0 * p10_minus_p20) * ystepdenominv);

	        t0 = r_p0[2] - r_p2[2];
	        t1 = r_p1[2] - r_p2[2];
	        r_sstepx = (int)((t1 * p01_minus_p21 - t0 * p11_minus_p21) *
			        xstepdenominv);
	        r_sstepy = (int)((t1 * p00_minus_p20 - t0* p10_minus_p20) *
			        ystepdenominv);

	        t0 = r_p0[3] - r_p2[3];
	        t1 = r_p1[3] - r_p2[3];
	        r_tstepx = (int)((t1 * p01_minus_p21 - t0 * p11_minus_p21) *
			        xstepdenominv);
	        r_tstepy = (int)((t1 * p00_minus_p20 - t0 * p10_minus_p20) *
			        ystepdenominv);

	        t0 = r_p0[5] - r_p2[5];
	        t1 = r_p1[5] - r_p2[5];
	        r_zistepx = (int)((t1 * p01_minus_p21 - t0 * p11_minus_p21) *
			        xstepdenominv);
	        r_zistepy = (int)((t1 * p00_minus_p20 - t0 * p10_minus_p20) *
			        ystepdenominv);

	        a_sstepxfrac = r_sstepx & 0xFFFF;
	        a_tstepxfrac = r_tstepx & 0xFFFF;

	        a_ststepxwhole = skinwidth * (r_tstepx >> 16) + (r_sstepx >> 16);
        }

        /*
        ================
        D_PolysetDrawSpans8
        ================
        */
        static void D_PolysetDrawSpans8 (int ofs)
        {
	        int		        lcount;
	        int	            lpdest;
	        int             lptex;
	        int		        lsfrac, ltfrac;
	        int		        llight;
	        int		        lzi;
	        int             lpz;
            spanpackage_t   pspanpackage = a_spans[ofs];

	        do
	        {
		        lcount = d_aspancount - pspanpackage.count;

		        errorterm += erroradjustup;
		        if (errorterm >= 0)
		        {
			        d_aspancount += d_countextrastep;
			        errorterm -= erroradjustdown;
		        }
		        else
		        {
			        d_aspancount += ubasestep;
		        }

		        if (lcount != 0)
		        {
			        lpdest = pspanpackage.pdest;
			        lptex = pspanpackage.ptex;
			        lpz = pspanpackage.pz;
			        lsfrac = pspanpackage.sfrac;
			        ltfrac = pspanpackage.tfrac;
			        llight = pspanpackage.light;
			        lzi = pspanpackage.zi;

			        do
			        {
				        if ((lzi >> 16) >= d_pzbuffer[lpz])
				        {
/*                            if (lptex < 0)
                                lptex = lptex;*/
//                            if (lptex >= 0 && lptex < render.r_affinetridesc.pskin.Length)
                            d_viewbuffer[lpdest] = render.acolormap[render.r_affinetridesc.pskin[lptex] + (llight & 0xFF00)];
        // gel mapping					*lpdest = gelmap[*lpdest];
					        d_pzbuffer[lpz] = (short)(lzi >> 16);
				        }
				        lpdest++;
				        lzi += r_zistepx;
				        lpz++;
				        llight += r_lstepx;
				        lptex += a_ststepxwhole;
				        lsfrac += a_sstepxfrac;
				        lptex += lsfrac >> 16;
				        lsfrac &= 0xFFFF;
				        ltfrac += a_tstepxfrac;
				        if ((ltfrac & 0x10000) != 0)
				        {
					        lptex += render.r_affinetridesc.skinwidth;
					        ltfrac &= 0xFFFF;
				        }
			        } while (--lcount != 0);
		        }

                pspanpackage = a_spans[++ofs];
	        } while (pspanpackage.count != -999999);
        }

        /*
        ================
        D_RasterizeAliasPolySmooth
        ================
        */
        static void D_RasterizeAliasPolySmooth ()
        {
	        int				initialleftheight, initialrightheight;
	        int[]			plefttop, prighttop, pleftbottom, prightbottom;
	        int				working_lstepx, originalcount;

	        plefttop = pedgetable.pleftedgevert0;
	        prighttop = pedgetable.prightedgevert0;

	        pleftbottom = pedgetable.pleftedgevert1;
	        prightbottom = pedgetable.prightedgevert1;

	        initialleftheight = pleftbottom[1] - plefttop[1];
	        initialrightheight = prightbottom[1] - prighttop[1];

        //
        // set the s, t, and light gradients, which are consistent across the triangle
        // because being a triangle, things are affine
        //
	        D_PolysetCalcGradients (render.r_affinetridesc.skinwidth);

        //
        // rasterize the polygon
        //

        //
        // scan out the top (and possibly only) part of the left edge
        //
            d_pedgespanpackage = 0;

	        ystart = plefttop[1];
	        d_aspancount = plefttop[0] - prighttop[0];

	        d_ptex = (plefttop[2] >> 16) +
			        (plefttop[3] >> 16) * render.r_affinetridesc.skinwidth;
            if (d_ptex > render.r_affinetridesc.pskin.Length)
                d_ptex = d_ptex;
	        d_sfrac = plefttop[2] & 0xFFFF;
	        d_tfrac = plefttop[3] & 0xFFFF;
	        d_light = plefttop[4];
	        d_zi = plefttop[5];

	        d_pdest = ystart * screenwidth + plefttop[0];
	        d_pz = (int)(ystart * d_zwidth + plefttop[0]);

	        if (initialleftheight == 1)
	        {
                d_edgespanpackage = a_spans[d_pedgespanpackage];
                d_edgespanpackage.pdest = d_pdest;
		        d_edgespanpackage.pz = d_pz;
		        d_edgespanpackage.count = d_aspancount;
		        d_edgespanpackage.ptex = d_ptex;

		        d_edgespanpackage.sfrac = d_sfrac;
		        d_edgespanpackage.tfrac = d_tfrac;

	        // FIXME: need to clamp l, s, t, at both ends?
		        d_edgespanpackage.light = d_light;
		        d_edgespanpackage.zi = d_zi;

		        d_pedgespanpackage++;
	        }
	        else
	        {
		        D_PolysetSetUpForLineScan(plefttop[0], plefttop[1],
							          pleftbottom[0], pleftbottom[1]);

		        d_pzbasestep = (int)(d_zwidth + ubasestep);
		        d_pzextrastep = d_pzbasestep + 1;

		        d_pdestbasestep = screenwidth + ubasestep;
		        d_pdestextrastep = d_pdestbasestep + 1;

	        // TODO: can reuse partial expressions here

	        // for negative steps in x along left edge, bias toward overflow rather than
	        // underflow (sort of turning the floor () we did in the gradient calcs into
	        // ceil (), but plus a little bit)
		        if (ubasestep < 0)
			        working_lstepx = r_lstepx - 1;
		        else
			        working_lstepx = r_lstepx;

		        d_countextrastep = ubasestep + 1;
		        d_ptexbasestep = ((r_sstepy + r_sstepx * ubasestep) >> 16) +
				        ((r_tstepy + r_tstepx * ubasestep) >> 16) *
				        render.r_affinetridesc.skinwidth;
		        d_sfracbasestep = (r_sstepy + r_sstepx * ubasestep) & 0xFFFF;
		        d_tfracbasestep = (r_tstepy + r_tstepx * ubasestep) & 0xFFFF;
		        d_lightbasestep = r_lstepy + working_lstepx * ubasestep;
		        d_zibasestep = r_zistepy + r_zistepx * ubasestep;

		        d_ptexextrastep = ((r_sstepy + r_sstepx * d_countextrastep) >> 16) +
				        ((r_tstepy + r_tstepx * d_countextrastep) >> 16) *
                        render.r_affinetridesc.skinwidth;
		        d_sfracextrastep = (r_sstepy + r_sstepx*d_countextrastep) & 0xFFFF;
		        d_tfracextrastep = (r_tstepy + r_tstepx*d_countextrastep) & 0xFFFF;
		        d_lightextrastep = d_lightbasestep + working_lstepx;
		        d_ziextrastep = d_zibasestep + r_zistepx;

		        D_PolysetScanLeftEdge (initialleftheight);
	        }

        //
        // scan out the bottom part of the left edge, if it exists
        //
	        if (pedgetable.numleftedges == 2)
	        {
		        int		height;

		        plefttop = pleftbottom;
		        pleftbottom = pedgetable.pleftedgevert2;

		        height = pleftbottom[1] - plefttop[1];

        // TODO: make this a function; modularize this function in general

		        ystart = plefttop[1];
		        d_aspancount = plefttop[0] - prighttop[0];
		        d_ptex = (plefttop[2] >> 16) +
				        (plefttop[3] >> 16) * render.r_affinetridesc.skinwidth;
		        d_sfrac = 0;
		        d_tfrac = 0;
		        d_light = plefttop[4];
		        d_zi = plefttop[5];

		        d_pdest = ystart * screenwidth + plefttop[0];
		        d_pz = (int)(ystart * d_zwidth + plefttop[0]);

		        if (height == 1)
		        {
                    d_edgespanpackage = a_spans[d_pedgespanpackage];
                    d_edgespanpackage.pdest = d_pdest;
			        d_edgespanpackage.pz = d_pz;
			        d_edgespanpackage.count = d_aspancount;
			        d_edgespanpackage.ptex = d_ptex;

			        d_edgespanpackage.sfrac = d_sfrac;
			        d_edgespanpackage.tfrac = d_tfrac;

		        // FIXME: need to clamp l, s, t, at both ends?
			        d_edgespanpackage.light = d_light;
			        d_edgespanpackage.zi = d_zi;

			        d_pedgespanpackage++;
		        }
		        else
		        {
			        D_PolysetSetUpForLineScan(plefttop[0], plefttop[1],
								          pleftbottom[0], pleftbottom[1]);

			        d_pdestbasestep = screenwidth + ubasestep;
			        d_pdestextrastep = d_pdestbasestep + 1;

			        d_pzbasestep = (int)(d_zwidth + ubasestep);
			        d_pzextrastep = d_pzbasestep + 1;

			        if (ubasestep < 0)
				        working_lstepx = r_lstepx - 1;
			        else
				        working_lstepx = r_lstepx;

			        d_countextrastep = ubasestep + 1;
			        d_ptexbasestep = ((r_sstepy + r_sstepx * ubasestep) >> 16) +
					        ((r_tstepy + r_tstepx * ubasestep) >> 16) *
					        render.r_affinetridesc.skinwidth;
    		        d_sfracbasestep = (r_sstepy + r_sstepx * ubasestep) & 0xFFFF;
			        d_tfracbasestep = (r_tstepy + r_tstepx * ubasestep) & 0xFFFF;
			        d_lightbasestep = r_lstepy + working_lstepx * ubasestep;
			        d_zibasestep = r_zistepy + r_zistepx * ubasestep;

			        d_ptexextrastep = ((r_sstepy + r_sstepx * d_countextrastep) >> 16) +
					        ((r_tstepy + r_tstepx * d_countextrastep) >> 16) *
                            render.r_affinetridesc.skinwidth;
			        d_sfracextrastep = (r_sstepy+r_sstepx*d_countextrastep) & 0xFFFF;
			        d_tfracextrastep = (r_tstepy+r_tstepx*d_countextrastep) & 0xFFFF;
			        d_lightextrastep = d_lightbasestep + working_lstepx;
			        d_ziextrastep = d_zibasestep + r_zistepx;

			        D_PolysetScanLeftEdge (height);
		        }
	        }

        // scan out the top (and possibly only) part of the right edge, updating the
        // count field
            d_pedgespanpackage = 0;

	        D_PolysetSetUpForLineScan(prighttop[0], prighttop[1],
						          prightbottom[0], prightbottom[1]);
	        d_aspancount = 0;
	        d_countextrastep = ubasestep + 1;
	        originalcount = a_spans[initialrightheight].count;
	        a_spans[initialrightheight].count = -999999; // mark end of the spanpackages
	        D_PolysetDrawSpans8 (0);

        // scan out the bottom part of the right edge, if it exists
	        if (pedgetable.numrightedges == 2)
	        {
		        int				height;
                int	            pstart;

		        pstart = initialrightheight;
                a_spans[pstart].count = originalcount;

		        d_aspancount = prightbottom[0] - prighttop[0];

		        prighttop = prightbottom;
		        prightbottom = pedgetable.prightedgevert2;

		        height = prightbottom[1] - prighttop[1];

		        D_PolysetSetUpForLineScan(prighttop[0], prighttop[1],
							          prightbottom[0], prightbottom[1]);

		        d_countextrastep = ubasestep + 1;
		        a_spans[initialrightheight + height].count = -999999;
											        // mark end of the spanpackages
		        D_PolysetDrawSpans8 (pstart);
	        }
        }

        /*
        ================
        D_PolysetSetEdgeTable
        ================
        */
        static void D_PolysetSetEdgeTable ()
        {
	        int			edgetableindex;

	        edgetableindex = 0;	// assume the vertices are already in
						        //  top to bottom order

        //
        // determine which edges are right & left, and the order in which
        // to rasterize them
        //
	        if (r_p0[1] >= r_p1[1])
	        {
		        if (r_p0[1] == r_p1[1])
		        {
			        if (r_p0[1] < r_p2[1])
				        pedgetable = edgetables[2];
			        else
				        pedgetable = edgetables[5];

			        return;
		        }
		        else
		        {
			        edgetableindex = 1;
		        }
	        }

	        if (r_p0[1] == r_p2[1])
	        {
		        if (edgetableindex != 0)
			        pedgetable = edgetables[8];
		        else
			        pedgetable = edgetables[9];

		        return;
	        }
	        else if (r_p1[1] == r_p2[1])
	        {
		        if (edgetableindex != 0)
			        pedgetable = edgetables[10];
		        else
			        pedgetable = edgetables[11];

		        return;
	        }

	        if (r_p0[1] > r_p2[1])
		        edgetableindex += 2;

	        if (r_p1[1] > r_p2[1])
		        edgetableindex += 4;

	        pedgetable = edgetables[edgetableindex];
        }
    }
}
