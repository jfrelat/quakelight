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
    public partial class prog
    {
        public class eval_t
        {
	        int		        @string;
	        double			_float;
	        double[]		vector = new double[3];
	        int			    function;
	        int				_int;
	        int				edict;
        };	

        public const int	MAX_ENT_LEAFS	= 16;
        public class edict_t
        {
            public int                      index;
	        public bool	                    free;
            //public link_t		            area;				// linked to a division node or leaf

            public int                      num_leafs;
            public short[]                  leafnums = new short[MAX_ENT_LEAFS];

            public quakedef.entity_state_t  baseline = new quakedef.entity_state_t();

            public double                   freetime;			// sv.time when the object was freed
            public entvars_t	            v = new entvars_t();					// C exported fields from progs
        // other fields from progs come immediately after

            public edict_t(int index)
            {
                this.index = index;
            }
        };
        public const int sizeof_edict_t = 516;

        public static int EDICT_TO_PROG(edict_t e) { return e.index * pr_edict_size; }
        public static edict_t PROG_TO_EDICT(int e) { return server.sv.edicts[e / pr_edict_size]; }

        //============================================================================

        static double G_FLOAT(int o) { return cast_float(pr_globals_read(o)); }
        static int G_INT(int o) { return cast_int(pr_globals_read(o)); }
        static edict_t G_EDICT(int o) { return server.sv.edicts[(int)pr_globals_read(o) / pr_edict_size]; }
        static int G_EDICTNUM(int o) { return NUM_FOR_EDICT(G_EDICT(o)); }
        static double[] G_VECTOR(int o)
        {
            double[] res = new double[3];
            res[0] = cast_float(pr_globals_read(o));
            res[1] = cast_float(pr_globals_read(o + 1));
            res[2] = cast_float(pr_globals_read(o + 2));
            return res;
        }
        static string G_STRING(int o) { return pr_string((int)pr_globals_read(o)); }
        //#define	G_FUNCTION(o) (*(func_t *)&pr_globals[o])

        static string E_STRING(edict_t e, int o) { return pr_string(cast_int(readentvar(e.v, o))); }

        delegate void builtin_t ();
    }
}
