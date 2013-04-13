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
// snd_mem.c: sound caching

namespace quake
{
    public partial class sound
    {
        //=============================================================================

        static string[] loops =
        {
            "ambience/drip1.wav",
            "ambience/fire1.wav",
            "ambience/hum1.wav",
            "ambience/swamp1.wav",
            "ambience/swamp2.wav",
            "ambience/water1.wav",
            "ambience/wind2.wav",
            "doors/basesec1.wav",
            "doors/doormv1.wav",
            "doors/stndr1.wav",
            "doors/winch2.wav",
            "plats/medplat1.wav",
            "plats/train1.wav",
        };

        /*
        ==============
        S_LoadSound
        ==============
        */
        public static sfxcache_t S_LoadSound (sfx_t s)
        {
            string	        namebuffer = new string(new char[256]);
            byte[]          data;
	        sfxcache_t	    sc;

            // see if still in memory
            sc = s.cache;
            if (sc != null)
                return sc;

        //Con_Printf ("S_LoadSound: %x\n", (int)stackbuf);
        // load it in
            namebuffer = "sound/";
            namebuffer += s.name;
            namebuffer += ".mp3";
            
        //	Con_Printf ("loading %s\n",namebuffer);

	        data = common.COM_LoadStackFile(namebuffer, null, 0);
	        if (data == null)
	        {
		        console.Con_Printf ("Couldn't load " + namebuffer + "\n");
		        return null;
	        }

	        sc = new sfxcache_t ();
            s.cache = sc;
	        if (sc == null)
		        return null;

	        sc.loopstart = -1;
            for(int kk = 0; kk < loops.Length; kk++)
            {
                if (s.name.CompareTo(loops[kk]) == 0)
                {
                    sc.loopstart = 1;
                    break;
                }
            }
            sc.data = data;

	        return sc;
        }
    }
}
