//using quake.common;
/*#include "common.h"
#include "bspfile.h"
#include "vid.h"
#include "sys.h"
#include "zone.h"
#include "mathlib.h"*/

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
// quakedef.h -- primary header for client

#define	QUAKE_GAME			// as opposed to utilities

namespace quake
{
    public sealed class quakedef
    {
        public const double VERSION = 1.09;
        public const double LINUX_VERSION = 1.30;

        public const string GAMENAME = "id1";		// directory to look in by default

        // !!! if this is changed, it must be changed in d_ifacea.h too !!!
        const int CACHE_SIZE = 32;		// used to align key data structures

        public const int MINIMUM_MEMORY = 0x550000;
        public const int MINIMUM_MEMORY_LEVELPAK = (MINIMUM_MEMORY + 0x100000);

        const int MAX_NUM_ARGVS = 50;

        // up / down
        public const int PITCH = 0;

        // left / right
        public const int YAW = 1;

        // fall over
        public const int ROLL = 2;
        
        public const int MAX_QPATH = 64;			// max length of a quake game pathname
        public const int MAX_OSPATH = 128;			// max length of a filesystem pathname

        public const double ON_EPSILON = 0.1;		// point on plane side epsilon

        public const int MAX_MSGLEN = 8000;		// max length of a reliable message
        public const int MAX_DATAGRAM = 1024;		// max length of unreliable message

        //
        // per-level limits
        //
        public const int MAX_EDICTS = 600;			// FIXME: ouch! ouch! ouch!
        public const int MAX_LIGHTSTYLES = 64;
        public const int MAX_MODELS = 256;			// these are sent over the net as bytes
        public const int MAX_SOUNDS = 256;			// so they cannot be blindly increased

        const int SAVEGAME_COMMENT_LENGTH = 39;

        public const int MAX_STYLESTRING = 64;

        //
        // stats are integers communicated to the client by the server
        //
        public const int    MAX_CL_STATS        = 32;
        public const int	STAT_HEALTH			= 0;
        public const int	STAT_FRAGS			= 1;
        public const int	STAT_WEAPON			= 2;
        public const int	STAT_AMMO			= 3;
        public const int	STAT_ARMOR			= 4;
        public const int	STAT_WEAPONFRAME	= 5;
        public const int	STAT_SHELLS			= 6;
        public const int	STAT_NAILS			= 7;
        public const int	STAT_ROCKETS		= 8;
        public const int	STAT_CELLS			= 9;
        public const int	STAT_ACTIVEWEAPON	= 10;
        public const int	STAT_TOTALSECRETS	= 11;
        public const int	STAT_TOTALMONSTERS	= 12;
        public const int	STAT_SECRETS		= 13;		// bumped on client side by svc_foundsecret
        public const int	STAT_MONSTERS		= 14;		// bumped by svc_killedmonster

        // stock defines

        public const int	IT_SHOTGUN			= 1;
        public const int	IT_SUPER_SHOTGUN	= 2;
        public const int	IT_NAILGUN			= 4;
        public const int	IT_SUPER_NAILGUN	= 8;
        public const int	IT_GRENADE_LAUNCHER	= 16;
        public const int	IT_ROCKET_LAUNCHER	= 32;
        public const int	IT_LIGHTNING		= 64;
        public const int    IT_SUPER_LIGHTNING  = 128;
        public const int    IT_SHELLS           = 256;
        public const int    IT_NAILS            = 512;
        public const int    IT_ROCKETS          = 1024;
        public const int    IT_CELLS            = 2048;
        public const int    IT_AXE              = 4096;
        public const int    IT_ARMOR1           = 8192;
        public const int    IT_ARMOR2           = 16384;
        public const int    IT_ARMOR3           = 32768;
        public const int    IT_SUPERHEALTH      = 65536;
        public const int    IT_KEY1             = 131072;
        public const int    IT_KEY2             = 262144;
        public const int	IT_INVISIBILITY		= 524288;
        public const int	IT_INVULNERABILITY	= 1048576;
        public const int	IT_SUIT				= 2097152;
        public const int	IT_QUAD				= 4194304;
        public const int    IT_SIGIL1           = (1<<28);
        public const int    IT_SIGIL2           = (1<<29);
        public const int    IT_SIGIL3           = (1<<30);
        public const int    IT_SIGIL4           = (1<<31);

        //===========================================
        //rogue changed and added defines

        public const int    RIT_SHELLS              = 128;
        public const int    RIT_NAILS               = 256;
        public const int    RIT_ROCKETS             = 512;
        public const int    RIT_CELLS               = 1024;
        public const int    RIT_AXE                 = 2048;
        public const int    RIT_LAVA_NAILGUN        = 4096;
        public const int    RIT_LAVA_SUPER_NAILGUN  = 8192;
        public const int    RIT_MULTI_GRENADE       = 16384;
        public const int    RIT_MULTI_ROCKET        = 32768;
        public const int    RIT_PLASMA_GUN          = 65536;
        public const int    RIT_ARMOR1              = 8388608;
        public const int    RIT_ARMOR2              = 16777216;
        public const int    RIT_ARMOR3              = 33554432;
        public const int    RIT_LAVA_NAILS          = 67108864;
        public const int    RIT_PLASMA_AMMO         = 134217728;
        public const int    RIT_MULTI_ROCKETS       = 268435456;
        public const int    RIT_SHIELD              = 536870912;
        public const int    RIT_ANTIGRAV            = 1073741824;
        public const int    RIT_SUPERHEALTH         = unchecked((int)2147483648);

        //MED 01/04/97 added hipnotic defines
        //===========================================
        //hipnotic added defines
        public const int    HIT_PROXIMITY_GUN_BIT   = 16;
        public const int    HIT_MJOLNIR_BIT         = 7;
        public const int    HIT_LASER_CANNON_BIT    = 23;
        public const int    HIT_PROXIMITY_GUN       = (1<<HIT_PROXIMITY_GUN_BIT);
        public const int    HIT_MJOLNIR             = (1<<HIT_MJOLNIR_BIT);
        public const int    HIT_LASER_CANNON        = (1<<HIT_LASER_CANNON_BIT);
        public const int    HIT_WETSUIT             = (1<<(23+2));
        public const int    HIT_EMPATHY_SHIELDS     = (1<<(23+3));

        //===========================================

        public const int MAX_SCOREBOARD = 16;
        const int MAX_SCOREBOARDNAME = 32;

        const int SOUND_CHANNELS = 8;

        public class entity_state_t
        {
	        public double[]	origin = new double[3];
            public double[] angles = new double[3];
	        public int		modelindex;
	        public int		frame;
	        public int		colormap;
	        public int		skin;
	        public int		effects;
        };

        //=============================================================================

        // the host system specifies the base of the directory tree, the
        // command line parms passed to the program, and the amount of memory
        // available for the program to use

        public class quakeparms_t
        {
	        public string	    basedir;
	        string	    cachedir;		// for development over ISDN lines
	        int		    argc;
	        string[]    argv;
	        byte[]	    membase;
	        public int		    memsize;
        };
   }
}