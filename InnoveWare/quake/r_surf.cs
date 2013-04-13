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
// r_surf.c: surface-related refresh code

namespace quake
{
    public partial class render
    {
        public static draw.drawsurf_t      r_drawsurf = new draw.drawsurf_t();

        static int                  lightleft, sourcesstep, blocksize, sourcetstep;
        static int                  lightdelta, lightdeltastep;
        static int                  lightright, lightleftstep, lightrightstep, blockdivshift;
        static uint                 blockdivmask;
        static helper.ByteBuffer    prowdestbase;
        static helper.ByteBuffer    pbasesource;
        static int                  surfrowbytes;	// used by ASM files
        static helper.UIntBuffer    r_lightptr;
        static int                  r_stepback;
        static int                  r_lightwidth;
        static int                  r_numhblocks, r_numvblocks;

        static helper.ByteBuffer    r_source, r_sourcemax;

        delegate void blockdrawer();
        static blockdrawer[]    surfmiptable = {
	        R_DrawSurfaceBlock8_mip0,
	        R_DrawSurfaceBlock8_mip1,
	        R_DrawSurfaceBlock8_mip2,
	        R_DrawSurfaceBlock8_mip3
        };

        static uint[]           blocklights = new uint[18*18];

        /*
        ===============
        R_AddDynamicLights
        ===============
        */
        static void R_AddDynamicLights()
        {
	        model.msurface_t    surf;
	        int			        lnum;
	        int			        sd, td;
	        double		        dist, rad, minlight;
	        double[]		    impact = new double[3], local = new double[3];
	        int			        s, t;
	        int			        i;
	        int			        smax, tmax;
	        model.mtexinfo_t	tex;

	        surf = r_drawsurf.surf;
	        smax = (surf.extents[0]>>4)+1;
	        tmax = (surf.extents[1]>>4)+1;
	        tex = surf.texinfo;

	        for (lnum=0 ; lnum<client.MAX_DLIGHTS ; lnum++)
	        {
		        if ( (surf.dlightbits & (1<<lnum) ) == 0 )
			        continue;		// not lit by this light

		        rad = client.cl_dlights[lnum].radius;
                dist = mathlib.DotProduct(client.cl_dlights[lnum].origin, surf.plane.normal) -
				        surf.plane.dist;
		        rad -= Math.Abs(dist);
                minlight = client.cl_dlights[lnum].minlight;
		        if (rad < minlight)
			        continue;
		        minlight = rad - minlight;

		        for (i=0 ; i<3 ; i++)
		        {
                    impact[i] = client.cl_dlights[lnum].origin[i] -
					        surf.plane.normal[i]*dist;
		        }

		        local[0] = mathlib.DotProduct (impact, tex.vecs[0]) + tex.vecs[0][3];
                local[1] = mathlib.DotProduct (impact, tex.vecs[1]) + tex.vecs[1][3];

		        local[0] -= surf.texturemins[0];
		        local[1] -= surf.texturemins[1];
        		
		        for (t = 0 ; t<tmax ; t++)
		        {
			        td = (int)(local[1] - t*16);
			        if (td < 0)
				        td = -td;
			        for (s=0 ; s<smax ; s++)
			        {
				        sd = (int)(local[0] - s*16);
				        if (sd < 0)
					        sd = -sd;
				        if (sd > td)
					        dist = sd + (td>>1);
				        else
					        dist = td + (sd>>1);
				        if (dist < minlight)
					        blocklights[t*smax + s] += (uint)((rad - dist)*256);
			        }
		        }
	        }
        }

        /*
        ===============
        R_BuildLightMap

        Combine and scale multiple lightmaps into the 8.8 format in blocklights
        ===============
        */
        static void R_BuildLightMap()
        {
            int                 smax, tmax;
            int                 t;
            int                 i, size;
            int                 lightmap;
            uint                scale;
            int                 maps;
            model.msurface_t    surf;

            surf = r_drawsurf.surf;

            smax = (surf.extents[0] >> 4) + 1;
            tmax = (surf.extents[1] >> 4) + 1;
            size = smax * tmax;
            lightmap = 0;

            if (r_fullbright.value != 0 || client.cl.worldmodel.lightdata == null)
            {
                for (i = 0; i < size; i++)
                    blocklights[i] = 0;
                return;
            }

            // clear to ambient
            for (i = 0; i < size; i++)
                blocklights[i] = (uint)(r_refdef.ambientlight << 8);


            // add all the lightmaps
            if (surf.samples != null)
                for (maps = 0; maps < bspfile.MAXLIGHTMAPS && surf.styles[maps] != 255;
                     maps++)
                {
                    scale = (uint)r_drawsurf.lightadj[maps];	// 8.8 fraction		
                    for (i = 0; i < size; i++)
                        blocklights[i] += surf.samples.buffer[surf.samples.ofs + lightmap + i] * scale;
                    lightmap += size;	// skip to next lightmap*/
                }

            // add all the dynamic lights
            if (surf.dlightframe == r_framecount)
                R_AddDynamicLights();

            // bound, invert, and shift
            for (i = 0; i < size; i++)
            {
                t = (255 * 256 - (int)blocklights[i]) >> (8 - vid.VID_CBITS);

                if (t < (1 << 6))
                    t = (1 << 6);

                blocklights[i] = (uint)t;
            }
        }

        /*
        ===============
        R_TextureAnimation

        Returns the proper texture for a given time and base texture
        ===============
        */
        public static model.texture_t R_TextureAnimation(model.texture_t @base)
        {
	        int		reletive;
	        int		count;

	        if (currententity.frame != 0)
	        {
		        if (@base.alternate_anims != null)
			        @base = @base.alternate_anims;
	        }
        	
	        if (@base.anim_total == 0)
		        return @base;

	        reletive = (int)(client.cl.time*10) % @base.anim_total;

	        count = 0;
	        while (@base.anim_min > reletive || @base.anim_max <= reletive)
	        {
		        @base = @base.anim_next;
		        if (@base == null)
			        sys_linux.Sys_Error ("R_TextureAnimation: broken cycle");
		        if (++count > 100)
                    sys_linux.Sys_Error("R_TextureAnimation: infinite cycle");
	        }

	        return @base;
        }

        /*
        ===============
        R_DrawSurface
        ===============
        */
        public static void R_DrawSurface()
        {
	        helper.ByteBuffer   basetptr;
	        int				    smax, tmax, twidth;
	        int				    u;
	        int				    soffset, basetoffset, texwidth;
	        int				    horzblockstep;
	        helper.ByteBuffer   pcolumndest;
            blockdrawer         pblockdrawer;
	        model.texture_t	    mt;

        // calculate the lightings
        	R_BuildLightMap ();
        	
	        surfrowbytes = r_drawsurf.rowbytes;

	        mt = r_drawsurf.texture;
        	
	        r_source = new helper.ByteBuffer(mt.pixels, (int)mt.offsets[r_drawsurf.surfmip]);
        	
        // the fractional light values should range from 0 to (VID_GRADES - 1) << 16
        // from a source range of 0 - 255
        	
	        texwidth = (int)(mt.width >> r_drawsurf.surfmip);

	        blocksize = 16 >> r_drawsurf.surfmip;
	        blockdivshift = 4 - r_drawsurf.surfmip;
	        blockdivmask = (uint)((1 << blockdivshift) - 1);
        	
	        r_lightwidth = (r_drawsurf.surf.extents[0]>>4)+1;

	        r_numhblocks = r_drawsurf.surfwidth >> blockdivshift;
	        r_numvblocks = r_drawsurf.surfheight >> blockdivshift;

        //==============================

	        if (r_pixbytes == 1)
	        {
		        pblockdrawer = surfmiptable[r_drawsurf.surfmip];
	        // TODO: only needs to be set when there is a display settings change
		        horzblockstep = blocksize;
	        }
	        else
	        {
		        pblockdrawer = R_DrawSurfaceBlock16;
	        // TODO: only needs to be set when there is a display settings change
		        horzblockstep = blocksize << 1;
	        }

	        smax = (int)(mt.width >> r_drawsurf.surfmip);
	        twidth = texwidth;
	        tmax = (int)(mt.height >> r_drawsurf.surfmip);
	        sourcetstep = texwidth;
	        r_stepback = tmax * twidth;

	        r_sourcemax = r_source + (tmax * smax);

	        soffset = r_drawsurf.surf.texturemins[0];
	        basetoffset = r_drawsurf.surf.texturemins[1];

        // << 16 components are to guarantee positive values for %
	        soffset = ((soffset >> r_drawsurf.surfmip) + (smax << 16)) % smax;
	        basetptr = new helper.ByteBuffer(r_source, ((((basetoffset >> r_drawsurf.surfmip) 
		        + (tmax << 16)) % tmax) * twidth));

	        pcolumndest = new helper.ByteBuffer(r_drawsurf.surfdat);

	        for (u=0 ; u<r_numhblocks; u++)
	        {
		        r_lightptr = new helper.UIntBuffer(blocklights, u);

		        prowdestbase = new helper.ByteBuffer(pcolumndest);

		        pbasesource = new helper.ByteBuffer(basetptr, soffset);

		        pblockdrawer();

		        soffset = soffset + blocksize;
		        if (soffset >= smax)
			        soffset = 0;

		        pcolumndest.Add(horzblockstep);
	        }
        }

        //=============================================================================

        /*
        ================
        R_DrawSurfaceBlock8_mip0
        ================
        */
        static void R_DrawSurfaceBlock8_mip0()
        {
	        int				    v, i, b, lightstep, lighttemp, light;
	        byte	            pix;
            helper.ByteBuffer   psource, prowdest;

	        psource = new helper.ByteBuffer(pbasesource);
	        prowdest = new helper.ByteBuffer(prowdestbase);

	        for (v=0 ; v<r_numvblocks ; v++)
	        {
	        // FIXME: make these locals?
	        // FIXME: use delta rather than both right and left, like ASM?
		        lightleft = (int)r_lightptr[0];
                lightright = (int)r_lightptr[1];
		        r_lightptr.ofs += r_lightwidth;
                lightleftstep = ((int)r_lightptr[0] - lightleft) >> 4;
                lightrightstep = ((int)r_lightptr[1] - lightright) >> 4;

		        for (i=0 ; i<16 ; i++)
		        {
			        lighttemp = lightleft - lightright;
			        lightstep = lighttemp >> 4;

			        light = lightright;

			        for (b=15; b>=0; b--)
			        {
				        pix = psource[b];
				        prowdest[b] = screen.vid.colormap
						        [(light & 0xFF00) + pix];
				        light += lightstep;
			        }
        	
			        psource.Add(sourcetstep);
			        lightright += lightrightstep;
			        lightleft += lightleftstep;
			        prowdest.Add(surfrowbytes);
		        }

		        if (psource >= r_sourcemax)
			        psource.Sub(r_stepback);
	        }
        }

        /*
        ================
        R_DrawSurfaceBlock8_mip1
        ================
        */
        static void R_DrawSurfaceBlock8_mip1()
        {
	        int				    v, i, b, lightstep, lighttemp, light;
            byte                pix;
	        helper.ByteBuffer   psource, prowdest;

            psource = new helper.ByteBuffer(pbasesource);
            prowdest = new helper.ByteBuffer(prowdestbase);

	        for (v=0 ; v<r_numvblocks ; v++)
	        {
	        // FIXME: make these locals?
	        // FIXME: use delta rather than both right and left, like ASM?
		        lightleft = (int)r_lightptr[0];
                lightright = (int)r_lightptr[1];
		        r_lightptr += r_lightwidth;
                lightleftstep = (int)(r_lightptr[0] - lightleft) >> 3;
                lightrightstep = (int)(r_lightptr[1] - lightright) >> 3;

		        for (i=0 ; i<8 ; i++)
		        {
			        lighttemp = lightleft - lightright;
			        lightstep = lighttemp >> 3;

			        light = lightright;

			        for (b=7; b>=0; b--)
			        {
				        pix = psource[b];
				        prowdest[b] = screen.vid.colormap
						        [(light & 0xFF00) + pix];
				        light += lightstep;
			        }
        	
			        psource.Add(sourcetstep);
			        lightright += lightrightstep;
			        lightleft += lightleftstep;
			        prowdest.Add(surfrowbytes);
		        }

		        if (psource >= r_sourcemax)
			        psource.Sub(r_stepback);
	        }
        }

        /*
        ================
        R_DrawSurfaceBlock8_mip2
        ================
        */
        static void R_DrawSurfaceBlock8_mip2()
        {
            int v, i, b, lightstep, lighttemp, light;
            byte pix;
            helper.ByteBuffer psource, prowdest;

            psource = new helper.ByteBuffer(pbasesource);
            prowdest = new helper.ByteBuffer(prowdestbase);

            for (v = 0; v < r_numvblocks; v++)
            {
                // FIXME: make these locals?
                // FIXME: use delta rather than both right and left, like ASM?
                lightleft = (int)r_lightptr[0];
                lightright = (int)r_lightptr[1];
                r_lightptr += r_lightwidth;
                lightleftstep = (int)(r_lightptr[0] - lightleft) >> 2;
                lightrightstep = (int)(r_lightptr[1] - lightright) >> 2;

                for (i = 0; i < 4; i++)
                {
                    lighttemp = lightleft - lightright;
                    lightstep = lighttemp >> 2;

                    light = lightright;

                    for (b = 3; b >= 0; b--)
                    {
                        pix = psource[b];
                        prowdest[b] = screen.vid.colormap
                                [(light & 0xFF00) + pix];
                        light += lightstep;
                    }

                    psource.Add(sourcetstep);
                    lightright += lightrightstep;
                    lightleft += lightleftstep;
                    prowdest.Add(surfrowbytes);
                }

                if (psource >= r_sourcemax)
                    psource.Sub(r_stepback);
            }
        }

        /*
        ================
        R_DrawSurfaceBlock8_mip3
        ================
        */
        static void R_DrawSurfaceBlock8_mip3()
        {
            int v, i, b, lightstep, lighttemp, light;
            byte pix;
            helper.ByteBuffer psource, prowdest;

            psource = new helper.ByteBuffer(pbasesource);
            prowdest = new helper.ByteBuffer(prowdestbase);

            for (v = 0; v < r_numvblocks; v++)
            {
                // FIXME: make these locals?
                // FIXME: use delta rather than both right and left, like ASM?
                lightleft = (int)r_lightptr[0];
                lightright = (int)r_lightptr[1];
                r_lightptr += r_lightwidth;
                lightleftstep = (int)(r_lightptr[0] - lightleft) >> 1;
                lightrightstep = (int)(r_lightptr[1] - lightright) >> 1;

                for (i = 0; i < 2; i++)
                {
                    lighttemp = lightleft - lightright;
                    lightstep = lighttemp >> 1;

                    light = lightright;

                    for (b = 1; b >= 0; b--)
                    {
                        pix = psource[b];
                        prowdest[b] = screen.vid.colormap
                                [(light & 0xFF00) + pix];
                        light += lightstep;
                    }

                    psource.Add(sourcetstep);
                    lightright += lightrightstep;
                    lightleft += lightleftstep;
                    prowdest.Add(surfrowbytes);
                }

                if (psource >= r_sourcemax)
                    psource.Sub(r_stepback);
            }
        }

        /*
        ================
        R_DrawSurfaceBlock16

        FIXME: make this work
        ================
        */
        static void R_DrawSurfaceBlock16()
        {
        }

        //============================================================================
    }
}
