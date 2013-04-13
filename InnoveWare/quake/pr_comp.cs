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

// this file is shared by quake and qcc

namespace quake
{
    public partial class prog
    {
        public enum etype_t {ev_void, ev_string, ev_float, ev_vector, ev_entity, ev_field, ev_function, ev_pointer};

        public const int	OFS_NULL		= 0;
        public const int	OFS_RETURN		= 1;
        public const int	OFS_PARM0		= 4;	// leave 3 ofs for each parm to hold vectors
        public const int	OFS_PARM1		= 7;
        public const int	OFS_PARM2		= 10;
        public const int	OFS_PARM3		= 13;
        public const int	OFS_PARM4		= 16;
        public const int	OFS_PARM5		= 19;
        public const int	OFS_PARM6		= 22;
        public const int	OFS_PARM7		= 25;
        public const int	RESERVED_OFS	= 28;
        
        public enum opcode_t {
	        OP_DONE,
	        OP_MUL_F,
	        OP_MUL_V,
	        OP_MUL_FV,
	        OP_MUL_VF,
	        OP_DIV_F,
	        OP_ADD_F,
	        OP_ADD_V,
	        OP_SUB_F,
	        OP_SUB_V,
        	
	        OP_EQ_F,
	        OP_EQ_V,
	        OP_EQ_S,
	        OP_EQ_E,
	        OP_EQ_FNC,
        	
	        OP_NE_F,
	        OP_NE_V,
	        OP_NE_S,
	        OP_NE_E,
	        OP_NE_FNC,
        	
	        OP_LE,
	        OP_GE,
	        OP_LT,
	        OP_GT,

	        OP_LOAD_F,
	        OP_LOAD_V,
	        OP_LOAD_S,
	        OP_LOAD_ENT,
	        OP_LOAD_FLD,
	        OP_LOAD_FNC,

	        OP_ADDRESS,

	        OP_STORE_F,
	        OP_STORE_V,
	        OP_STORE_S,
	        OP_STORE_ENT,
	        OP_STORE_FLD,
	        OP_STORE_FNC,

	        OP_STOREP_F,
	        OP_STOREP_V,
	        OP_STOREP_S,
	        OP_STOREP_ENT,
	        OP_STOREP_FLD,
	        OP_STOREP_FNC,

	        OP_RETURN,
	        OP_NOT_F,
	        OP_NOT_V,
	        OP_NOT_S,
	        OP_NOT_ENT,
	        OP_NOT_FNC,
	        OP_IF,
	        OP_IFNOT,
	        OP_CALL0,
	        OP_CALL1,
	        OP_CALL2,
	        OP_CALL3,
	        OP_CALL4,
	        OP_CALL5,
	        OP_CALL6,
	        OP_CALL7,
	        OP_CALL8,
	        OP_STATE,
	        OP_GOTO,
	        OP_AND,
	        OP_OR,
        	
	        OP_BITAND,
	        OP_BITOR
        };
        
        public class dstatement_t
        {
	        public ushort	op;
	        public short	a,b,c;

            public static implicit operator dstatement_t(helper.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                dstatement_t dstatement = new dstatement_t();
                dstatement.op = BitConverter.ToUInt16(buf.buffer, ofs); ofs += sizeof(ushort);
                dstatement.a = BitConverter.ToInt16(buf.buffer, ofs); ofs += sizeof(short);
                dstatement.b = BitConverter.ToInt16(buf.buffer, ofs); ofs += sizeof(short);
                dstatement.c = BitConverter.ToInt16(buf.buffer, ofs); ofs += sizeof(short);
                return dstatement;
            }
        };
        public const int sizeof_dstatement_t = sizeof(ushort) + 3 * sizeof(short);

        public class ddef_t
        {
	        public ushort	type;		// if DEF_SAVEGLOBGAL bit is set
								        // the variable needs to be saved in savegames
	        public ushort	ofs;
	        public int		s_name;

            public static implicit operator ddef_t(helper.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                ddef_t ddef = new ddef_t();
                ddef.type = BitConverter.ToUInt16(buf.buffer, ofs); ofs += sizeof(ushort);
                ddef.ofs = BitConverter.ToUInt16(buf.buffer, ofs); ofs += sizeof(ushort);
                ddef.s_name = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                return ddef;
            }
        };
        public const int sizeof_ddef_t = 2 * sizeof(ushort) + sizeof(int);
        public const int	DEF_SAVEGLOBAL	= (1<<15);

        public const int	MAX_PARMS	= 8;

        public class dfunction_t
        {
	        public int		first_statement;	// negative numbers are builtins
	        public int		parm_start;
	        public int		locals;				// total ints of parms + locals
        	
	        public int		profile;		// runtime
        	
	        public int		s_name;
	        public int		s_file;			// source file defined in
        	
	        public int		numparms;
	        public byte[]	parm_size = new byte[MAX_PARMS];

            public static implicit operator dfunction_t(helper.ByteBuffer buf)
            {
                int ofs = buf.ofs;
                dfunction_t dfunction = new dfunction_t();
                dfunction.first_statement = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                dfunction.parm_start = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                dfunction.locals = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                dfunction.profile = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                dfunction.s_name = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                dfunction.s_file = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                dfunction.numparms = BitConverter.ToInt32(buf.buffer, ofs); ofs += sizeof(int);
                for (int kk = 0; kk < MAX_PARMS; kk++)
                {
                    dfunction.parm_size[kk] = buf.buffer[ofs++];
                }
                return dfunction;
            }
        };
        public const int sizeof_dfunction_t = 7 * sizeof(int) + MAX_PARMS;
        
        public const int	PROG_VERSION	= 6;
        public class dprograms_t
        {
	        public int		version;
	        public int		crc;			// check of header file
        	
	        public int		ofs_statements;
	        public int		numstatements;	// statement 0 is an error

	        public int		ofs_globaldefs;
	        public int		numglobaldefs;
        	
	        public int		ofs_fielddefs;
	        public int		numfielddefs;
        	
	        public int		ofs_functions;
	        public int		numfunctions;	// function 0 is an empty
        	
	        public int		ofs_strings;
	        public int		numstrings;		// first string is a null string

	        public int		ofs_globals;
	        public int		numglobals;
        	
	        public int		entityfields;

            public static implicit operator dprograms_t(byte[] buf)
            {
                int ofs = 0;
                dprograms_t dprograms = new dprograms_t();
                dprograms.version = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.crc = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.ofs_statements = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.numstatements = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.ofs_globaldefs = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.numglobaldefs = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.ofs_fielddefs = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.numfielddefs = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.ofs_functions = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.numfunctions = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.ofs_strings = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.numstrings = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.ofs_globals = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.numglobals = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                dprograms.entityfields = BitConverter.ToInt32(buf, ofs); ofs += sizeof(int);
                return dprograms;
            }
        };
    }
}