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
// client.h

namespace quake
{
    public partial class client
    {
        public class usercmd_t
        {
            public double[] viewangles = new double[3];

        // intended velocities
	        public double	forwardmove;
            public double   sidemove;
            public double   upmove;
        };

        public class lightstyle_t
        {
	        public int		length;
	        public string	map = new string(new char[quakedef.MAX_STYLESTRING]);
        };

        public class scoreboard_t
        {
	        public string	name;
	        double	entertime;
	        public int		frags;
	        public int		colors;			// two 4 bit fields
	        public byte[]	translations = new byte[vid.VID_GRADES*256];
        };

        public class cshift_t
        {
            public int[]    destcolor = new int[3];
	        public int		percent;		// 0-256

            public cshift_t()
            {
            }
            public cshift_t(int[] destcolor, int percent)
            {
                this.destcolor = destcolor;
                this.percent = percent;
            }
        };

        public const int    CSHIFT_CONTENTS = 0;
        public const int    CSHIFT_DAMAGE = 1;
        public const int    CSHIFT_BONUS = 2;
        public const int    CSHIFT_POWERUP = 3;
        public const int	NUM_CSHIFTS = 4;

        const int	NAME_LENGTH = 64;

        //
        // client_state_t should hold all pieces of the client state
        //

        public const int	SIGNONS = 4;			// signon messages to receive before connected

        public const int	MAX_DLIGHTS = 32;
        public class dlight_t
        {
            public double[]     origin = new double[3];
            public double       radius;
            public double       die;				// stop lighting after this time
            public double       decay;				// drop this each second
            public double       minlight;			// don't add when contributing less
	        public int		    key;
        };

        const int	MAX_BEAMS = 24;
        public class beam_t
        {
	        public int		        entity;
            public model.model_t    model;
            public double           endtime;
            public double[]         start = new double[3], end = new double[3];
        };

        const int	MAX_EFRAGS = 640;

        const int	MAX_MAPSTRING = 2048;
        public const int	MAX_DEMOS = 8;
        const int	MAX_DEMONAME = 16;

        public enum cactive_t {
        ca_dedicated, 		// a dedicated server with no ability to start a client
        ca_disconnected, 	// full screen console with no connection
        ca_connected		// valid netcon, talking to a server
        };

        //
        // the client_static_t structure is persistant through an arbitrary number
        // of server connections
        //
        public class client_static_t
        {
	        public cactive_t	state;

        // personalization data sent to server	
	        public string		mapstring;
	        public string		spawnparms;	// to restart a level

        // demo loop control
	        public int		    demonum;		// -1 = don't play demos
	        public string[]	    demos = new string[MAX_DEMOS];		// when not playing

        // demo recording info must be here, because record is started before
        // entering a map (and clearing client_state_t)
	        public bool	    demorecording;
	        public bool	    demoplayback;
	        public bool	    timedemo;
            public int      forcetrack;			// -1 = use normal cd track
	        public Helper.helper.FILE	demofile;
	        public int		td_lastframe;		// to meter out one message a frame
	        public int		td_startframe;		// host_framecount at start
	        public double	td_starttime;		// realtime at second frame of timedemo


        // connection information
	        public int              signon;			// 0 to SIGNONS
            public net.qsocket_t    netcon;
	        public common.sizebuf_t	message = new common.sizebuf_t();		// writing buffer to send to server
        };

        //
        // the client_state_t structure is wiped completely at every
        // server signon
        //
        public class client_state_t
        {
	        public int			    movemessages;	// since connecting to this server
								        // throw out the first couple, so the player
								        // doesn't accidentally do something the 
								        // first frame
	        public usercmd_t	    cmd = new usercmd_t();			// last command sent to the server

        // information for local display
	        public int[]		    stats = new int[quakedef.MAX_CL_STATS];	// health, etc
	        public int			    items;			// inventory bit flags
	        public double[]	        item_gettime = new double[32];	// cl.time of aquiring item, for blinking
	        public double		    faceanimtime;	// use anim frame if cl.time < this

	        public cshift_t[]	    cshifts = new cshift_t[NUM_CSHIFTS];	// color shifts for damage, powerups
            public cshift_t[]       prev_cshifts = new cshift_t[NUM_CSHIFTS];	// and content types

        // the client maintains its own idea of view angles, which are
        // sent to the server each frame.  The server sets punchangle when
        // the view is temporarliy offset, and an angle reset commands at the start
        // of each level and after teleporting.
	        public double[][]	    mviewangles = { new double[3], new double[3] };	// during demo playback viewangles is lerped
								        // between these
	        public double[]	        viewangles = new double[3];
        	
	        public double[][]	    mvelocity = { new double[3], new double[3] };	// update by server, used for lean+bob
								        // (0 is newest)
	        public double[]	        velocity = new double[3];		// lerped between mvelocity[0] and [1]

	        public double[]	        punchangle = new double[3];		// temporary offset

        // pitch drifting vars
	        public double		    idealpitch;
            public double           pitchvel;
            public bool             nodrift;
	        public double		    driftmove;
	        public double		    laststop;

	        public double		    viewheight;
	        double		crouch;			// local amount for smoothing stepups

	        public bool	            paused;			// send over by server
	        public bool	            onground;
	        public bool	            inwater;
        	
	        public int			    intermission;	// don't change view angle, full screen, etc
	        int			completed_time;	// latched at intermission start

            public double[]         mtime = new double[2];		// the timestamp of last two messages	
            public double           time;			// clients view of time, should be between
								                // servertime and oldservertime to generate
								                // a lerp point for other data
	        public double		    oldtime;		// previous cl.time, time-oldtime is used
								                // to decay light values and smooth step ups
        	

	        public double		    last_received_message;	// (realtime) for net trouble icon

        //
        // information that is static for the entire time connected to a server
        //
	        public model.model_t[]	model_precache = new model.model_t[quakedef.MAX_MODELS];
        	public sound.sfx_t[]	sound_precache = new sound.sfx_t[quakedef.MAX_SOUNDS];
	        public string		    levelname = new string(new char[40]);	// for display on solo scoreboard
	        public int			    viewentity;		// cl_entitites[cl.viewentity] = player
	        public int			    maxclients;
	        public int			    gametype;

        // refresh related state
	        public model.model_t	worldmodel;	// cl_entitites[0].model
	        public render.efrag_t	free_efrags;
	        public int			    num_entities;	// held in cl_entities array
	        public int			    num_statics;	// held in cl_staticentities array
	        public render.entity_t	viewent = new render.entity_t();			// the gun model

	        public int			cdtrack, looptrack;	// cd audio

        // frag scoreboard
	        public scoreboard_t[]	scores;		// [cl.maxclients]

            public client_state_t()
            {
                for (int kk = 0; kk < NUM_CSHIFTS; kk++)
                {
                    cshifts[kk] = new cshift_t();
                    prev_cshifts[kk] = new cshift_t();
                }
            }
        };

        const int	MAX_TEMP_ENTITIES = 64;			// lightning bolts, etc
        const int	MAX_STATIC_ENTITIES = 128;		// torches, etc

        //=============================================================================

        public const int	MAX_VISEDICTS = 256;

        //
        // cl_input
        //
        class kbutton_t
        {
	        public int[]	down = new int[2];		// key nums holding it down
	        public int		state;			// low bit is down state
        };
   }
}