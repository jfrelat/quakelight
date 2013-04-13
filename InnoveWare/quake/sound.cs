using System;
using System.Windows.Controls;

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
// sound.h -- client sound i/o functions

namespace quake
{
    public partial class sound
    {
        public const int    DEFAULT_SOUND_PACKET_VOLUME = 255;
        public const double DEFAULT_SOUND_PACKET_ATTENUATION = 1.0;

        public class sfx_t
        {
	        public string       name = new string(new char[quakedef.MAX_QPATH]);
            public sfxcache_t   cache;
        };

        // !!! if this is changed, it much be changed in asm_i386.h too !!!
        public class sfxcache_t
        {
            public int          loopstart;
            public byte[]       data;
        };

        // !!! if this is changed, it much be changed in asm_i386.h too !!!
        public class channel_t
        {
            public sfx_t        sfx;			// sfx number
            public int          leftvol;		// 0-255 volume
            public int          rightvol;		// 0-255 volume
            public int          looping;		// where to loop, -1 = no looping
            public int          entnum;			// to allow overriding a specific sound
            public int          entchannel;		//
            public double[]     origin = new double[3];			// origin of sound effect
            public double       dist_mult;		// distance multiplier (attenuation/clipK)
            public int          master_vol;		// 0-255 master volume
            public int          skip;
            public MediaElement media;
        };

        // ====================================================================
        // User-setable variables
        // ====================================================================

        public const int MAX_CHANNELS			= 128;
        public const int MAX_DYNAMIC_CHANNELS	= 8;
    }
}
