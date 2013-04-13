using System;
using System.IO;
using System.Windows.Controls;
using InnoveWare;
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
// snd_dma.c -- main control for any streaming sound output device

namespace quake
{
    public partial class sound
    {
        // =======================================================================
        // Internal sound data & structures
        // =======================================================================

        static channel_t[]  channels = new channel_t[MAX_CHANNELS];
        static int		    total_channels;

        static int		    snd_blocked = 0;
        static bool	        snd_ambient = true;
        static bool		    snd_initialized = false;

        static double[]	    listener_origin = new double[3];
        static double[]	    listener_forward = new double[3];
        static double[]	    listener_right = new double[3];
        static double[]	    listener_up = new double[3];
        static double	    sound_nominal_clip_dist=1000.0;

        static int		    soundtime;		// sample PAIRS
        static int   	    paintedtime; 	// sample PAIRS

        const int	        MAX_SFX		= 512;
        static sfx_t[]	    known_sfx;		// hunk allocated [MAX_SFX]
        static int		    num_sfx;

        static sfx_t[]	    ambient_sfx = new sfx_t[bspfile.NUM_AMBIENTS];

        static int          sound_started=0;

        static cvar_t       bgmvolume = new cvar_t("bgmvolume", "1", true);
        static cvar_t       volume = new cvar_t("volume", "0.7", true);

        static cvar_t       nosound = new cvar_t("nosound", "0");
        static cvar_t       precache = new cvar_t("precache", "1");
        static cvar_t       loadas8bit = new cvar_t("loadas8bit", "0");
        static cvar_t       bgmbuffer = new cvar_t("bgmbuffer", "4096");
        static cvar_t       ambient_level = new cvar_t("ambient_level", "0.3");
        static cvar_t       ambient_fade = new cvar_t("ambient_fade", "100");
        static cvar_t       snd_noextraupdate = new cvar_t("snd_noextraupdate", "0");
        static cvar_t       snd_show = new cvar_t("snd_show", "0");
        static cvar_t       _snd_mixahead = new cvar_t("_snd_mixahead", "0.1", true);

        // ====================================================================
        // User-setable variables
        // ====================================================================

        public static void S_AmbientOff ()
        {
	        snd_ambient = false;
        }

        public static void S_AmbientOn ()
        {
	        snd_ambient = true;
        }

        public static void S_SoundInfo_f()
        {
        }

        /*
        ================
        S_Startup
        ================
        */

        public static void S_Startup ()
        {
	        int		rc;

	        if (!snd_initialized)
		        return;

	        sound_started = 1;
        }

        /*
        ================
        S_Init
        ================
        */
        public static void S_Init ()
        {
            int kk;

            for (kk = 0; kk < MAX_CHANNELS; kk++) channels[kk] = new channel_t();

	        console.Con_Printf("\nSound Initialization\n");

	        if (common.COM_CheckParm("-nosound") != 0)
		        return;

	        cmd.Cmd_AddCommand("play", S_Play);
	        cmd.Cmd_AddCommand("playvol", S_PlayVol);
	        cmd.Cmd_AddCommand("stopsound", S_StopAllSoundsC);
	        cmd.Cmd_AddCommand("soundlist", S_SoundList);
	        cmd.Cmd_AddCommand("soundinfo", S_SoundInfo_f);

	        cvar_t.Cvar_RegisterVariable(nosound);
	        cvar_t.Cvar_RegisterVariable(volume);
	        cvar_t.Cvar_RegisterVariable(precache);
	        cvar_t.Cvar_RegisterVariable(loadas8bit);
	        cvar_t.Cvar_RegisterVariable(bgmvolume);
	        cvar_t.Cvar_RegisterVariable(bgmbuffer);
	        cvar_t.Cvar_RegisterVariable(ambient_level);
	        cvar_t.Cvar_RegisterVariable(ambient_fade);
	        cvar_t.Cvar_RegisterVariable(snd_noextraupdate);
	        cvar_t.Cvar_RegisterVariable(snd_show);
	        cvar_t.Cvar_RegisterVariable(_snd_mixahead);

	        /*if (host_parms.memsize < 0x800000)
	        {
		        Cvar_Set ("loadas8bit", "1");
		        Con_Printf ("loading all sounds as 8bit\n");
	        }*/

	        snd_initialized = true;

	        S_Startup ();

	        known_sfx = new sfx_t[MAX_SFX];
            for(kk = 0; kk < MAX_SFX; kk++) known_sfx[kk] = new sfx_t();
	        num_sfx = 0;

	        // provides a tick sound until washed clean

        //	if (shm.buffer)
        //		shm.buffer[4] = shm.buffer[5] = 0x7f;	// force a pop for debugging

            for(kk = 0; kk < bspfile.NUM_AMBIENTS; kk++) ambient_sfx[kk] = new sfx_t();
	        ambient_sfx[bspfile.AMBIENT_WATER] = S_PrecacheSound ("ambience/water1.wav");
	        ambient_sfx[bspfile.AMBIENT_SKY] = S_PrecacheSound ("ambience/wind2.wav");

	        S_StopAllSounds (true);
        }

        // =======================================================================
        // Shutdown sound engine
        // =======================================================================

        public static void S_Shutdown()
        {
	        if (sound_started == 0)
		        return;

	        sound_started = 0;
        }

        // =======================================================================
        // Load a sound
        // =======================================================================

        /*
        ==================
        S_FindName

        ==================
        */
        public static sfx_t S_FindName (string name)
        {
	        int		i;
	        sfx_t	sfx;

	        if (name == null)
		        sys_linux.Sys_Error ("S_FindName: NULL\n");

	        if (name.Length >= quakedef.MAX_QPATH)
		        sys_linux.Sys_Error ("Sound name too long: " + name);

        // see if already loaded
	        for (i=0 ; i < num_sfx ; i++)
		        if (known_sfx[i].name.CompareTo(name) == 0)
		        {
			        return known_sfx[i];
		        }

	        if (num_sfx == MAX_SFX)
		        sys_linux.Sys_Error ("S_FindName: out of sfx_t");
        	
	        sfx = known_sfx[i];
	        sfx.name = name;

	        num_sfx++;
        	
	        return sfx;
        }
        
        /*
        ==================
        S_TouchSound

        ==================
        */
        public static void S_TouchSound (string name)
        {
	        sfx_t	sfx;
        	
	        if (sound_started == 0)
		        return;

	        sfx = S_FindName (name);
        }

        /*
        ==================
        S_PrecacheSound

        ==================
        */
        public static sfx_t S_PrecacheSound (string name)
        {
	        sfx_t	sfx;

	        if (sound_started == 0 || nosound.value != 0)
		        return null;

	        sfx = S_FindName (name);
        	
        // cache it in
	        if (precache.value != 0)
		        S_LoadSound (sfx);
        	
	        return sfx;
        }
        
        //=============================================================================

        /*
        =================
        SND_PickChannel
        =================
        */
        public static channel_t SND_PickChannel(int entnum, int entchannel)
        {
            int ch_idx;
            int first_to_die;
            TimeSpan life_left;

        // Check for replacement sound, or find the best one to replace
            first_to_die = -1;
            life_left = new TimeSpan(TimeSpan.MaxValue.Ticks);
            for (ch_idx=bspfile.NUM_AMBIENTS ; ch_idx < bspfile.NUM_AMBIENTS + MAX_DYNAMIC_CHANNELS ; ch_idx++)
            {
		        if (entchannel != 0		// channel 0 never overrides
		        && channels[ch_idx].entnum == entnum
		        && (channels[ch_idx].entchannel == entchannel || entchannel == -1) )
		        {	// allways override sound from same entity

                    if (entnum == 195)
                        console.Con_Printf("REPLACE " + channels[ch_idx].sfx.name + "\n");

			        first_to_die = ch_idx;
			        break;
		        }

		        // don't let monster sounds override player sounds
		        if (channels[ch_idx].entnum == client.cl.viewentity && entnum != client.cl.viewentity && channels[ch_idx].sfx != null)
			        continue;

                if (channels[ch_idx].media != null)
                {
                    // compute left time
                    TimeSpan end = channels[ch_idx].media.NaturalDuration.TimeSpan.Subtract(channels[ch_idx].media.Position);
                    if (end.CompareTo(life_left) < 0)
                    {
                        life_left = end;
                        first_to_die = ch_idx;
                    }
                }
                else
                    first_to_die = ch_idx;
                
/*                if (channels[ch_idx].sfx == null)
                {
                    first_to_die = ch_idx;
                    break;
                }*/
                
//                first_to_die = ch_idx;
           }

	        if (first_to_die == -1)
		        return null;

            if (channels[first_to_die].sfx != null)
            {
                if (channels[first_to_die].media != null && channels[first_to_die].media.CurrentState != System.Windows.Media.MediaElementState.Playing)
                {
                    /*if (channels[first_to_die].looping == 1)
                        channels[first_to_die].media.MediaEnded -= media_MediaEnded;*/
                    channels[first_to_die].media.Stop();
                    Page.thePage.parentCanvas.Children.Remove(channels[first_to_die].media);
                    channels[first_to_die].media = null;
                }
                channels[first_to_die].sfx = null;
            }
            return channels[first_to_die];
        }       

        /*
        =================
        SND_Spatialize
        =================
        */
        public static void SND_Spatialize(channel_t ch)
        {
            double      dot;
            double      ldist, rdist, dist;
            double      lscale, rscale, scale;
            double[]    source_vec = new double[3];
	        sfx_t       snd;

        // anything coming from the view entity will allways be full volume
	        if (ch.entnum == client.cl.viewentity)
	        {
		        ch.leftvol = ch.master_vol;
		        ch.rightvol = ch.master_vol;
		        return;
	        }

        // calculate stereo seperation and distance attenuation

	        snd = ch.sfx;
	        mathlib.VectorSubtract(ch.origin, listener_origin, ref source_vec);

            dist = mathlib.VectorNormalize(ref source_vec) * ch.dist_mult;

            dot = mathlib.DotProduct(listener_right, source_vec);

	        rscale = 1.0 + dot;
	        lscale = 1.0 - dot;

        // add in distance effect
	        scale = (1.0 - dist) * rscale;
	        ch.rightvol = (int) (ch.master_vol * scale);
	        if (ch.rightvol < 0)
		        ch.rightvol = 0;

	        scale = (1.0 - dist) * lscale;
	        ch.leftvol = (int) (ch.master_vol * scale);
	        if (ch.leftvol < 0)
		        ch.leftvol = 0;
        }           
        
        // =======================================================================
        // Start a sound effect
        // =======================================================================

        public static void S_StartSound(int entnum, int entchannel, sfx_t sfx, double[] origin, double fvol, double attenuation)
        {
	        channel_t   target_chan, check;
	        sfxcache_t	sc;
	        int		    vol;
	        int		    ch_idx;
	        int		    skip;

            /*if (entnum != 195)
                return;*/

	        if (sound_started == 0)
		        return;

	        if (sfx == null)
		        return;

	        if (nosound.value != 0)
		        return;

	        vol = (int)(fvol*255);

        // pick a channel to play on
	        target_chan = SND_PickChannel(entnum, entchannel);
	        if (target_chan == null)
		        return;
        		
        // spatialize
	        mathlib.VectorCopy(origin, ref target_chan.origin);
	        target_chan.dist_mult = attenuation / sound_nominal_clip_dist;
	        target_chan.master_vol = vol;
	        target_chan.entnum = entnum;
	        target_chan.entchannel = entchannel;
	        SND_Spatialize(target_chan);

            if (target_chan.leftvol == 0 && target_chan.rightvol == 0)
                return;		// not audible at all

        // new channel
	        sc = S_LoadSound (sfx);
	        if (sc == null)
	        {
		        target_chan.sfx = null;
		        return;		// couldn't load the sound's data
	        }

	        target_chan.sfx = sfx;

/*            if (sc.loopstart != -1)
                console.Con_Printf(sfx.name + " " + entnum + " " + entchannel + "\n");*/

            MediaElement media = new MediaElement();
            target_chan.media = media;
            media.AutoPlay = true;
            media.SetSource(new MemoryStream(sc.data));
            media.Tag = target_chan;
            /*if (sc.loopstart != -1)
            {
                media.MediaEnded += media_MediaEnded;
                target_chan.looping = 1;
            }
            else*/
                media.MediaEnded += media_MediaEnded2;
            SetVolume(target_chan);
            Page.thePage.parentCanvas.Children.Add(media);
        }

        public static void S_StopSound(int entnum, int entchannel)
        {
	        int i;

	        for (i=0 ; i<MAX_DYNAMIC_CHANNELS ; i++)
	        {
		        if (channels[i].entnum == entnum
			        && channels[i].entchannel == entchannel)
		        {
                    if (channels[i].media != null)
                    {
                        channels[i].media.Stop();
                        Page.thePage.parentCanvas.Children.Remove(channels[i].media);
                    }
			        channels[i].sfx = null;
			        return;
		        }
	        }
        }

        public static void S_StopAllSounds(bool clear)
        {
	        int		i;

	        if (sound_started == 0)
		        return;

	        total_channels = MAX_DYNAMIC_CHANNELS + bspfile.NUM_AMBIENTS;	// no statics

	        for (i=0 ; i<MAX_CHANNELS ; i++)
                if (channels[i].sfx != null)
                {
                    if (channels[i].media != null)
                    {
                        channels[i].media.Stop();
                        Page.thePage.parentCanvas.Children.Remove(channels[i].media);
                    }
                    channels[i].sfx = null;
                }
        }

        public static void S_StopAllSoundsC ()
        {
	        S_StopAllSounds (true);
        }

        public static void SetVolume(channel_t ch)
        {
            if (ch.leftvol == 0 && ch.rightvol == 0)
                ch.media.Volume = 0;
            else
            {
                double max = ch.rightvol;
                if (ch.leftvol > max) max = ch.leftvol;

                ch.media.Volume = ch.master_vol / 255.0;
                ch.media.Balance = (ch.rightvol - ch.leftvol) / max;
            }
        }
        
        /*
        =================
        S_StaticSound
        =================
        */
        public static void S_StaticSound (sfx_t sfx, double[] origin, double vol, double attenuation)
        {
	        channel_t	ss;
	        sfxcache_t  sc;

	        if (sfx == null)
		        return;

	        if (total_channels == MAX_CHANNELS)
	        {
		        console.Con_Printf ("total_channels == MAX_CHANNELS\n");
		        return;
	        }

	        ss = channels[total_channels];
	        total_channels++;

	        sc = S_LoadSound (sfx);
	        if (sc == null)
		        return;

	        if (sc.loopstart == -1)
	        {
		        console.Con_Printf ("Sound " + sfx.name + " not looped\n");
		        return;
	        }
        	
	        ss.sfx = sfx;
	        mathlib.VectorCopy (origin, ref ss.origin);
	        ss.master_vol = (int)vol;
	        ss.dist_mult = (attenuation/64) / sound_nominal_clip_dist;
        	
	        SND_Spatialize (ss);

            MediaElement media = new MediaElement();
            ss.media = media;
            media.AutoPlay = true;
            media.SetSource(new MemoryStream(sc.data));
            media.Tag = ss;
            media.MediaEnded += media_MediaEnded;
            SetVolume(ss);
            Page.thePage.parentCanvas.Children.Add(media);
        }

        static void media_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            MediaElement media = (MediaElement)sender;
            media.Stop();
            media.Play();
        }

        static void media_MediaEnded2(object sender, System.Windows.RoutedEventArgs e)
        {
            MediaElement media = (MediaElement)sender;
            media.Stop();
            channel_t ch = (channel_t)media.Tag;
            Page.thePage.parentCanvas.Children.Remove(media);
            ch.sfx = null;
        }

        //=============================================================================

        /*
        ===================
        S_UpdateAmbientSounds
        ===================
        */
        public static void S_UpdateAmbientSounds ()
        {
	        model.mleaf_t	l;
	        double		    vol;
	        int			    ambient_channel;
	        channel_t	    chan;

	        if (!snd_ambient)
		        return;

        // calc ambient sound levels
	        if (client.cl.worldmodel == null)
		        return;

	        l = model.Mod_PointInLeaf (listener_origin, client.cl.worldmodel);
	        if (l == null || ambient_level.value == 0)
	        {
		        for (ambient_channel = 0 ; ambient_channel< bspfile.NUM_AMBIENTS ; ambient_channel++)
			        channels[ambient_channel].sfx = null;
		        return;
	        }

	        for (ambient_channel = 0 ; ambient_channel< bspfile.NUM_AMBIENTS ; ambient_channel++)
	        {
		        chan = channels[ambient_channel];	
		        chan.sfx = ambient_sfx[ambient_channel];
        	
		        vol = ambient_level.value * l.ambient_sound_level[ambient_channel];
		        if (vol < 8)
			        vol = 0;

	        // don't adjust volume too fast
		        if (chan.master_vol < vol)
		        {
			        chan.master_vol += (int)(host.host_frametime * ambient_fade.value);
			        if (chan.master_vol > vol)
				        chan.master_vol = (int)vol;
		        }
		        else if (chan.master_vol > vol)
		        {
			        chan.master_vol -= (int)(host.host_frametime * ambient_fade.value);
			        if (chan.master_vol < vol)
				        chan.master_vol = (int)vol;
		        }
        		
		        chan.leftvol = chan.rightvol = chan.master_vol;
	        }
        }
        
        /*
        ============
        S_Update

        Called once each time through the main loop
        ============
        */
        public static void S_Update(double[] origin, double[] forward, double[] right, double[] up)
        {
	        int			i, j;
	        int			total;
	        channel_t	ch;
	        channel_t	combine;

	        if (sound_started == 0 || (snd_blocked > 0))
		        return;

	        mathlib.VectorCopy(origin, ref listener_origin);
            mathlib.VectorCopy(forward, ref listener_forward);
            mathlib.VectorCopy(right, ref listener_right);
            mathlib.VectorCopy(up, ref listener_up);
        	
        // update general area ambient sound sources
	        S_UpdateAmbientSounds ();

	        combine = null;

        // update spatialization for static and dynamic sounds	
	        for (i=bspfile.NUM_AMBIENTS ; i<total_channels; i++)
	        {
                ch = channels[i];
		        if (ch.sfx == null)
			        continue;
		        SND_Spatialize(ch);         // respatialize channel
		        if (ch.leftvol == 0 && ch.rightvol == 0)
			        continue;

	        // try to combine static sounds with a previous channel of the same
	        // sound effect so we don't mix five torches every frame
        	
		        if (i >= MAX_DYNAMIC_CHANNELS + bspfile.NUM_AMBIENTS)
		        {
		        // see if it can just use the last one
			        if (combine != null && combine.sfx == ch.sfx)
			        {
				        combine.leftvol += ch.leftvol;
				        combine.rightvol += ch.rightvol;
				        ch.leftvol = ch.rightvol = 0;
				        continue;
			        }
		        // search for one
                    combine = channels[MAX_DYNAMIC_CHANNELS + bspfile.NUM_AMBIENTS];
                    for (j = MAX_DYNAMIC_CHANNELS + bspfile.NUM_AMBIENTS; j < i; j++)
                    {
                        combine = channels[j];
                        if (combine.sfx == ch.sfx)
                            break;
                    }
        					
			        if (j == total_channels)
			        {
				        combine = null;
			        }
			        else
			        {
				        if (combine != ch && combine != null)
				        {
					        combine.leftvol += ch.leftvol;
					        combine.rightvol += ch.rightvol;
					        ch.leftvol = ch.rightvol = 0;
				        }
				        continue;
			        }
		        }
	        }

        //
        // debugging output
        //
	        if (snd_show.value != 0)
	        {
		        total = 0;
                for (i = 0; i < total_channels; i++)
                {
                    ch = channels[i];
                    if (ch.sfx != null && (ch.leftvol != 0 || ch.rightvol != 0))
                    {
                        //Con_Printf ("%3i %3i %s\n", ch.leftvol, ch.rightvol, ch.sfx.name);
                        total++;
                    }
                }

		        console.Con_Printf ("----(" + total + ")----\n");
	        }

        // mix some sound
	        S_Update_();
        }

        public static void S_ExtraUpdate ()
        {
	        if (snd_noextraupdate.value != 0)
		        return;		// don't pollute timings
	        S_Update_();
        }

        public static void S_Update_()
        {
            int         i;
            channel_t   ch;

            // paint in the channels.
            for (i = 0; i < total_channels; i++)
            {
                ch = channels[i];
                if (ch.sfx == null || ch.media == null)
                    continue;

                SetVolume(ch);
                /*if (ch.entnum == 195)
                    console.Con_Printf("UPDATE " + ch.sfx.name + " " + ch.master_vol + " " + ch.leftvol + " " + ch.rightvol + "\n");*/

                /*if (ch.media.CurrentState == System.Windows.Media.MediaElementState.Playing)
                {
                    TimeSpan end = ch.media.NaturalDuration.TimeSpan.Subtract(new TimeSpan(5000000));
                    if (ch.sfx.cache.loopstart != -1 && ch.media.Position.CompareTo(end) >= 0)
                    {
                        ch.media.Position = new TimeSpan(ch.media.Position.Subtract(end).Ticks);
                    }
                }
                else */
                /*if (ch.media.CurrentState == System.Windows.Media.MediaElementState.Paused)
                {
                    if (ch.sfx.cache.loopstart != -1)
                    {
                        //ch.media.Position = new TimeSpan(0);
                        ch.media.Stop();
                        ch.media.Play();
                    }
                    else
                    {
                        ch.media.Stop();
                        Page.thePage.parentCanvas.Children.Remove(ch.media);
                        ch.media = null;
                        ch.sfx = null;
                    }
                }*/
            }
        }

        /*
        ===============================================================================

        console functions

        ===============================================================================
        */

        static int hashS_Play = 345;
        public static void S_Play()
        {
	        int 	i;
	        string  name = new string(new char[256]);
	        sfx_t	sfx;
        	
	        i = 1;
	        while (i<cmd.Cmd_Argc())
	        {
		        if (cmd.Cmd_Argv(i).IndexOf('.') == -1)
		        {
			        name = cmd.Cmd_Argv(i);
			        name += ".wav";
		        }
		        else
			        name = cmd.Cmd_Argv(i);
		        sfx = S_PrecacheSound(name);
                S_StartSound(hashS_Play++, 0, sfx, listener_origin, 1.0, 1.0);
		        i++;
	        }
        }

        public static void S_PlayVol()
        {
        }

        public static void S_SoundList()
        {
	        int		    i;
	        sfx_t	    sfx;
	        sfxcache_t	sc;
	        int		    size, total;

	        total = 0;
	        for (i=0 ; i<num_sfx ; i++)
	        {
                sfx = known_sfx[i];
		        sc = sfx.cache;
		        if (sc == null)
			        continue;
		        //size = sc.length*sc.width*(sc.stereo+1);
		        //total += size;
		        if (sc.loopstart >= 0)
			        console.Con_Printf ("L");
		        else
                    console.Con_Printf(" ");
                console.Con_Printf(sfx.name);
                //"(%2db) %6i : %s\n", sc.width * 8, size, sfx.name);
	        }
            console.Con_Printf("Total resident: " + total + "\n");
        }

        public static void S_LocalSound (string sound)
        {
	        sfx_t	sfx;

	        if (nosound.value != 0)
		        return;
	        if (sound_started == 0)
		        return;
        		
	        sfx = S_PrecacheSound (sound);
	        if (sfx == null)
	        {
		        console.Con_Printf ("S_LocalSound: can't cache " + sound + "\n");
		        return;
	        }
	        S_StartSound (client.cl.viewentity, -1, sfx, mathlib.vec3_origin, 1, 1);
        }
    }
}
