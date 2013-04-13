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
// d_surf.c: rasterization driver surface heap manager

namespace quake
{
    public partial class draw
    {
        static double       surfscale;
        static bool         r_cache_thrash;         // set if surface cache is thrashing

        static int          sc_size;

        public const int GUARDSIZE       = 4;

        /*
        ================
        D_InitCaches

        ================
        */
        public static void D_InitCaches (int size)
        {
	        if (!common.msg_suppress_1)
		        console.Con_Printf ((size/1024) + "k surface cache\n");

	        sc_size = size - GUARDSIZE;
        }

        /*
        ==================
        D_FlushCaches
        ==================
        */
        static void D_FlushCaches ()
        {
        }

        /*
        =================
        D_SCAlloc
        =================
        */
        static surfcache_t     D_SCAlloc (int width, int size)
        {
	        surfcache_t             @new;

	        if ((width < 0) || (width > 256))
		        sys_linux.Sys_Error ("D_SCAlloc: bad cache width " + width + "\n");

	        if ((size <= 0) || (size > 0x10000))
		        sys_linux.Sys_Error ("D_SCAlloc: bad cache size " + size + "\n");
        	
	        if (size > sc_size)
		        sys_linux.Sys_Error ("D_SCAlloc: " + size + " > cache size");

        // create a fragment out of any leftovers
            @new = new surfcache_t();
	        @new.size = size;
            @new.data = new byte[size];
	        @new.width = (uint)width;
	        @new.owner = null;              // should be set properly after return
	        return @new;
        }

        //=============================================================================

        /*
        ================
        D_CacheSurface
        ================
        */
        static surfcache_t D_CacheSurface (model.msurface_t surface, int miplevel)
        {
	        surfcache_t     cache;

        //
        // if the surface is animating or flashing, flush the cache
        //
            render.r_drawsurf.texture = render.R_TextureAnimation(surface.texinfo.texture);
	        render.r_drawsurf.lightadj[0] = render.d_lightstylevalue[surface.styles[0]];
            render.r_drawsurf.lightadj[1] = render.d_lightstylevalue[surface.styles[1]];
            render.r_drawsurf.lightadj[2] = render.d_lightstylevalue[surface.styles[2]];
            render.r_drawsurf.lightadj[3] = render.d_lightstylevalue[surface.styles[3]];
        
        //
        // see if the cache holds apropriate data
        //
	        cache = surface.cachespots[miplevel];

            if (cache != null && cache.dlight == 0 && surface.dlightframe != render.r_framecount
                    && cache.texture == render.r_drawsurf.texture
			        && cache.lightadj[0] == render.r_drawsurf.lightadj[0]
                    && cache.lightadj[1] == render.r_drawsurf.lightadj[1]
                    && cache.lightadj[2] == render.r_drawsurf.lightadj[2]
                    && cache.lightadj[3] == render.r_drawsurf.lightadj[3])
		        return cache;
	
        //
        // determine shape of surface
        //
	        surfscale = 1.0 / (1<<miplevel);
            render.r_drawsurf.surfmip = miplevel;
            render.r_drawsurf.surfwidth = surface.extents[0] >> miplevel;
            render.r_drawsurf.rowbytes = render.r_drawsurf.surfwidth;
            render.r_drawsurf.surfheight = surface.extents[1] >> miplevel;

        //
        // allocate memory if needed
        //
	        if (cache == null)     // if a texture just animated, don't reallocate it
	        {
		        cache = D_SCAlloc (render.r_drawsurf.surfwidth,
                                   render.r_drawsurf.surfwidth * render.r_drawsurf.surfheight);
		        surface.cachespots[miplevel] = cache;
		        cache.owner = surface.cachespots[miplevel];
		        cache.mipscale = surfscale;
	        }

	        if (surface.dlightframe == render.r_framecount)
		        cache.dlight = 1;
	        else
		        cache.dlight = 0;

            render.r_drawsurf.surfdat = cache.data;

            cache.texture = render.r_drawsurf.texture;
	        cache.lightadj[0] = render.r_drawsurf.lightadj[0];
            cache.lightadj[1] = render.r_drawsurf.lightadj[1];
            cache.lightadj[2] = render.r_drawsurf.lightadj[2];
            cache.lightadj[3] = render.r_drawsurf.lightadj[3];

        //
        // draw and light the surface texture
        //
            render.r_drawsurf.surf = surface;

            render.c_surf++;
            render.R_DrawSurface();

	        return surface.cachespots[miplevel];
        }
    }
}

