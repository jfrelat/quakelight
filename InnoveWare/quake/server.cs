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
// server.h

namespace quake
{
    public partial class server
    {
        public class server_static_t
        {
	        public int		    maxclients;
	        public int		    maxclientslimit;
	        public client_t[]   clients;		// [maxclients]
	        public int		    serverflags;		// episode completion information
	        public bool	        changelevel_issued;	// cleared when at SV_SpawnServer
        };

        //=============================================================================

        public enum server_state_t { ss_loading, ss_active };

        public class server_t
        {
	        public bool	            active;				// false if only a net client

	        public bool	            paused;
	        public bool	            loadgame;			// handle connections specially

            public double           time;
        	
	        int			lastcheck;			// used by PF_checkclient
	        double		lastchecktime;
        	
	        public string		    name;			    // map name
	        public string		    modelname;		    // maps/<name>.bsp, for model_precache[0]
        	public model.model_t    worldmodel;
	        public string[]	        model_precache = new string[quakedef.MAX_MODELS];	// NULL terminated
	        public model.model_t[]	models = new model.model_t[quakedef.MAX_MODELS];
            public string[]         sound_precache = new string[quakedef.MAX_SOUNDS];	// NULL terminated
            public string[]         lightstyles = new string[quakedef.MAX_LIGHTSTYLES];
	        public int			    num_edicts;
	        public int			    max_edicts;
	        public prog.edict_t[]	edicts;			// can NOT be array indexed, because
									                // edict_t is variable sized, but can
									                // be used to reference the world ent
	        public server_state_t	state = new server_state_t();			// some actions are only valid during load

	        public common.sizebuf_t	datagram = new common.sizebuf_t();
	        public byte[]		    datagram_buf = new byte[quakedef.MAX_DATAGRAM];

	        public common.sizebuf_t	reliable_datagram = new common.sizebuf_t();	// copied to all clients at end of frame
            public byte[]           reliable_datagram_buf = new byte[quakedef.MAX_DATAGRAM];

	        public common.sizebuf_t	signon = new common.sizebuf_t();
            public byte[]           signon_buf = new byte[8192];

            public server_t()
            {
                for(int kk = 0; kk < quakedef.MAX_MODELS; kk++) models[kk] = new model.model_t();
            }
        };

        public const int	NUM_PING_TIMES		= 16;
        public const int	NUM_SPAWN_PARMS		= 16;

        public class client_t
        {
	        public bool		        active;				// false = client is free
	        public bool		        spawned;			// false = don't send datagrams
	        public bool		        dropasap;			// has been told to go to another level
	        public bool		        privileged;			// can execute any host command
	        public bool		        sendsignon;			// only valid before spawned

	        public double			last_message;		// reliable messages must be sent
										        // periodically

            public net.qsocket_t    netconnection;	// communications handle

            public client.usercmd_t cmd = new client.usercmd_t();				// movement
            double[] wishdir = new double[3];			// intended motion calced from cmd

	        public common.sizebuf_t	message = new common.sizebuf_t();			// can be added to at any time,
										// copied and clear once per frame
	        public byte[]			msgbuf = new byte[quakedef.MAX_MSGLEN];
	        public prog.edict_t		edict;				// EDICT_NUM(clientnum+1)
	        public string			name;			// for printing to other people
	        public int				colors;
        		
	        public double[]		    ping_times = new double[NUM_PING_TIMES];
	        public int				num_pings;			// ping_times[num_pings%NUM_PING_TIMES]

        // spawn parms are carried from level to level
	        public double[]		    spawn_parms = new double[NUM_SPAWN_PARMS];

        // client known data for deltas	
	        public int              old_frags;

            public int              index;
        };

        //=============================================================================

        // edict->movetype values
        public const int	MOVETYPE_NONE			= 0;		// never moves
        public const int	MOVETYPE_ANGLENOCLIP	= 1;
        public const int	MOVETYPE_ANGLECLIP		= 2;
        public const int	MOVETYPE_WALK			= 3;		// gravity
        public const int	MOVETYPE_STEP			= 4;		// gravity, special edge handling
        public const int	MOVETYPE_FLY			= 5;
        public const int	MOVETYPE_TOSS			= 6;		// gravity
        public const int	MOVETYPE_PUSH			= 7;		// no clip to world, push and crush
        public const int	MOVETYPE_NOCLIP			= 8;
        public const int	MOVETYPE_FLYMISSILE		= 9;		// extra size to monsters
        public const int	MOVETYPE_BOUNCE			= 10;

        // edict->solid values
        public const int	SOLID_NOT				= 0;		// no interaction with other objects
        public const int    SOLID_TRIGGER           = 1;		// touch on edge, but not blocking
        public const int	SOLID_BBOX				= 2;		// touch on edge, block
        public const int	SOLID_SLIDEBOX			= 3;		// touch on edge, but not an onground
        public const int	SOLID_BSP				= 4;		// bsp clip, touch on edge, block

        // edict->flags
        public const int	FL_FLY					= 1;
        public const int    FL_SWIM                 = 2;
        //public const int	FL_GLIMPSE				= 4;
        public const int	FL_CONVEYOR				= 4;
        public const int	FL_CLIENT				= 8;
        public const int	FL_INWATER				= 16;
        public const int	FL_MONSTER				= 32;
        public const int	FL_GODMODE				= 64;
        public const int	FL_NOTARGET				= 128;
        public const int	FL_ITEM					= 256;
        public const int	FL_ONGROUND				= 512;
        public const int	FL_PARTIALGROUND		= 1024;	// not all corners are valid
        public const int	FL_WATERJUMP			= 2048;	// player jumping out of water
        public const int	FL_JUMPRELEASED			= 4096;	// for jump debouncing

        // entity effects

        public const int    EF_BRIGHTFIELD          = 1;
        public const int	EF_MUZZLEFLASH 			= 2;
        public const int	EF_BRIGHTLIGHT 			= 4;
        public const int	EF_DIMLIGHT 			= 8;

        public const int    SPAWNFLAG_NOT_EASY          = 256;
        public const int	SPAWNFLAG_NOT_MEDIUM		= 512;
        public const int	SPAWNFLAG_NOT_HARD			= 1024;
        public const int	SPAWNFLAG_NOT_DEATHMATCH	= 2048;
    }
}