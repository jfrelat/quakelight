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
// models.c -- model loading and caching

// models are the only shared resource between a client and server running
// on the same machine.

namespace quake
{
    public partial class model
    {
        /*

        d*_t structures are on-disk representations
        m*_t structures are in-memory

        */

        /*
        ==============================================================================

        BRUSH MODELS

        ==============================================================================
        */

        //
        // in memory representation
        //
        // !!! if this is changed, it must be changed in asm_draw.h too !!!
        public class mvertex_t
        {
	        public double[]		position = new double[3];
        };

        static int[] colors =
        {
            0x10, 0xe0, 0xf0, 0x10, 0xb0, 0x80, 0x10, 0x30
        };
        static int icolor = 0;

        public const int	SIDE_FRONT	= 0;
        public const int	SIDE_BACK	= 1;
        public const int	SIDE_ON		= 2;
        
        // plane_t structure
        // !!! if this is changed, it must be changed in asm_i386.h too !!!
        public class mplane_t
        {
	        public double[]	normal = new double[3];
	        public double	dist;
	        public byte	    type;			// for texture axis selection and fast side tests
            public byte     signbits;		// signx + signy<<1 + signz<<1
	        byte[]	    pad = new byte[2];
        };

        public class texture_t
        {
            public string	    name = new string(new char[16]);
            public uint         width, height;
            public int          anim_total;				// total tenths in sequence ( 0 = no)
            public int          anim_min, anim_max;		// time for this frame min <=time< max
            public texture_t    anim_next;		// in the animation sequence
            public texture_t    alternate_anims;	// bmodels in frmae 1 use these
            public uint[]       offsets = new uint[bspfile.MIPLEVELS];		// four mip maps stored
            public byte[]       pixels;
        };
        public const int sizeof_texture_t = 16 + 2 * sizeof(uint) + 5 * sizeof(int) + bspfile.MIPLEVELS * sizeof(uint);
        
        public const int	SURF_PLANEBACK		= 2;
        public const int	SURF_DRAWSKY		= 4;
        public const int    SURF_DRAWSPRITE		= 8;
        public const int    SURF_DRAWTURB		= 0x10;
        public const int    SURF_DRAWTILED		= 0x20;
        public const int    SURF_DRAWBACKGROUND	= 0x40;

        // !!! if this is changed, it must be changed in asm_draw.h too !!!
        public class medge_t
        {
	        public ushort[]	v = new ushort[2];
            public uint     cachededgeoffset;
        };

        public class mtexinfo_t
        {
	        public double[][]	vecs = { new double[4], new double[4] };
            public double       mipadjust;
            public texture_t    texture;
            public int          flags;
        };

        public class msurface_t
        {
            public int                  visframe;		// should be drawn when node is crossed

            public int                  dlightframe;
            public int                  dlightbits;

            public mplane_t             plane;
            public int                  flags;

	        public int		            firstedge;	// look up in model.surfedges[], negative numbers
            public int                  numedges;	// are backwards edges
        	
        // surface generation data
            public draw.surfcache_t[]   cachespots = new draw.surfcache_t[bspfile.MIPLEVELS];

            public short[]              texturemins = new short[2];
            public short[]              extents = new short[2];

            public mtexinfo_t           texinfo;
        	
        // lighting info
            public byte[]               styles = new byte[bspfile.MAXLIGHTMAPS];
            public bspfile.ByteBuffer   samples;    // [numstyles*surfsize]

        // solid color
            public int                  color;
        };

        public class node_or_leaf_t
        {
            public int              contents;
            public int              visframe;

            public short[]          minmaxs = new short[6];

            public node_or_leaf_t   parent;
        }

        public class mnode_t : node_or_leaf_t
        {
        // node specific
            public mplane_t         plane;
            public node_or_leaf_t[] children = new node_or_leaf_t[2];

            public ushort           firstsurface;
            public ushort           numsurfaces;
        };
        
        public class mleaf_t : node_or_leaf_t
        {
        // leaf specific
            public bspfile.ByteBuffer   compressed_vis;
	        public render.efrag_t		efrags;

            public helper.ObjectBuffer  firstmarksurface;
            public int                  nummarksurfaces;
            public int                  key;   			// BSP sequence number for leaf's contents
            public byte[]               ambient_sound_level = new byte[bspfile.NUM_AMBIENTS];
        };

        // !!! if this is changed, it must be changed in asm_i386.h too !!!
        public class hull_t
        {
	        //dclipnode_t	*clipnodes;
	        mplane_t	planes;
	        public int	    firstclipnode;
            public int      lastclipnode;
	        double[]	clip_mins = new double[3];
	        double[]	clip_maxs = new double[3];
        };

        /*
        ==============================================================================

        SPRITE MODELS

        ==============================================================================
        */
        
        // FIXME: shorten these?
        public class mspriteframe_t
        {
	        public int	                    width;
            public int                      height;
	        //void	*                       pcachespot;			// remove?
	        public double	                up, down, left, right;
	        public byte[]	                pixels;
        };

        public class mspritegroup_t
        {
	        public int				        numframes;
            public double[]                 intervals;
            public object[]                 frames;
        };

        public class mspriteframedesc_t
        {
	        public spriteframetype_t	    type;
	        public object		            frameptr;
        };

        public class msprite_t
        {
	        public int				        type;
	        public int					    maxwidth;
	        public int					    maxheight;
	        public int					    numframes;
	        public double				    beamlength;		// remove?
	        //void				    *       cachespot;		// remove?
	        public mspriteframedesc_t[]	    frames;
        };
        
        /*
        ==============================================================================

        ALIAS MODELS

        Alias models are position independent, so the cache manager can move them.
        ==============================================================================
        */

        public class maliasframedesc_t
        {
	        public aliasframetype_t	        type;
	        public trivertx_t			    bboxmin = new trivertx_t();
            public trivertx_t               bboxmax = new trivertx_t();
	        public object				    frame;
	        public string				    name = new string(new char[16]);
        };

        public class maliasskindesc_t
        {
	        public aliasskintype_t		    type;
	        public object			        pcachespot;
	        public object				    skin;
        };

        public class maliasgroupframedesc_t
        {
	        public trivertx_t			    bboxmin = new trivertx_t();
            public trivertx_t               bboxmax = new trivertx_t();
	        public object				    frame;
        };

        public class maliasgroup_t
        {
	        public int						numframes;
	        public double[]					intervals;
	        public maliasgroupframedesc_t[]	frames;
        };

        public class maliasskingroup_t
        {
	        public int					    numskins;
	        public double[]				    intervals;
	        public maliasskindesc_t[]	    skindescs;
        };

        // !!! if this is changed, it must be changed in asm_draw.h too !!!
        public class mtriangle_t {
	        public int					    facesfront;
	        public int[]				    vertindex = new int[3];
        };

        public class aliashdr_t {
	        public mdl_t				    model;
            public stvert_t[]			    stverts;
            public maliasskindesc_t[]	    skindesc;
            public mtriangle_t[]		    triangles;
            public maliasframedesc_t[]	    frames;
        };

        //===================================================================

        //
        // Whole model
        //

        public enum modtype_t {mod_brush, mod_sprite, mod_alias};

        public const int	EF_ROCKET	= 1;			// leave a trail
        public const int	EF_GRENADE	= 2;			// leave a trail
        public const int	EF_GIB		= 4;			// leave a trail
        public const int	EF_ROTATE	= 8;			// rotate (bonus items)
        public const int	EF_TRACER	= 16;			// green split trail
        public const int	EF_ZOMGIB	= 32;			// small blood trail
        public const int	EF_TRACER2	= 64;			// orange split trail + rotate
        public const int	EF_TRACER3	= 128;			// purple trail

        public class model_t
        {
	        public string	            name;
	        public int	                needload;		// bmodels and sprites don't cache normally

	        public modtype_t	        type;
	        public int			        numframes;
	        public synctype_t	        synctype;
        	
	        public int		            flags;

        //
        // volume occupied by the model
        //		
	        public double[]	            mins = new double[3], maxs = new double[3];
	        public double		        radius;

        //
        // brush model
        //
	        public int			        firstmodelsurface, nummodelsurfaces;

	        public int			        numsubmodels;
	        public bspfile.dmodel_t[]	submodels;

	        public int			        numplanes;
            public mplane_t[]           planes;

	        public int			        numleafs;		// number of visible leafs, not counting 0
	        public mleaf_t[]		    leafs;

	        public int			        numvertexes;
            public mvertex_t[]          vertexes;

	        public int			        numedges;
	        public medge_t[]		    edges;

            public int                  numnodes;
	        public mnode_t[]	        nodes;

	        public int			        numtexinfo;
	        public mtexinfo_t[]	        texinfo;

	        public int			        numsurfaces;
	        public msurface_t[]	        surfaces;

            public int                  numsurfedges;
	        public int[]		        surfedges;

	        public int			        numclipnodes;
	        public bspfile.dclipnode_t	clipnodes;

	        public int			        nummarksurfaces;
	        public msurface_t[]	        marksurfaces;

	        public hull_t[]	            hulls = new hull_t[bspfile.MAX_MAP_HULLS];

	        public int			        numtextures;
            public texture_t[]          textures;

	        public byte[]		        visdata;
	        public byte[]		        lightdata;
            public char[]               entities;

        //
        // additional model data
        //
	        public object	    cache;		// only access through Mod_Extradata

            public model_t()
            {
                for (int kk = 0; kk < bspfile.MAX_MAP_HULLS; kk++)
                    hulls[kk] = new hull_t();
            }

            public void Clone(model_t model)
            {
                this.name = model.name;
                this.needload = model.needload;
                this.type = model.type;
                this.numframes = model.numframes;
                this.flags = model.flags;
                this.mins = (double[])model.mins.Clone();
                this.maxs = (double[])model.maxs.Clone();
                this.radius = model.radius;
                this.firstmodelsurface = model.firstmodelsurface;
                this.nummodelsurfaces = model.nummodelsurfaces;
                this.numsubmodels = model.numsubmodels;
                this.submodels = model.submodels;
                this.numplanes = model.numplanes;
                this.planes = model.planes;
                this.numleafs = model.numleafs;
                this.leafs = model.leafs;
                this.numvertexes = model.numvertexes;
                this.vertexes = model.vertexes;
                this.numedges = model.numedges;
                this.edges = model.edges;
                this.numnodes = model.numnodes;
                this.nodes = model.nodes;
                this.numtexinfo = model.numtexinfo;
                this.texinfo = model.texinfo;
                this.numsurfaces = model.numsurfaces;
                this.surfaces = model.surfaces;
                this.numsurfedges = model.numsurfedges;
                this.surfedges = model.surfedges;
                this.numclipnodes = model.numclipnodes;
                this.clipnodes = model.clipnodes;
                this.nummarksurfaces = model.nummarksurfaces;
                this.marksurfaces = model.marksurfaces;
                this.hulls = model.hulls;
                this.numtextures = model.numtextures;
                this.textures = model.textures;
                this.visdata = model.visdata;
                this.lightdata = model.lightdata;
                this.entities = model.entities;
            }
        };

        //============================================================================

        static model_t      loadmodel;
        static string	    loadname;	// for hunk tags

        static byte[]	    mod_novis = new byte[bspfile.MAX_MAP_LEAFS/8];

        public const int	MAX_MOD_KNOWN	= 256;
        static model_t[]	mod_known = new model_t[MAX_MOD_KNOWN];
        static int		    mod_numknown;

        // values for model_t's needload
        public const int NL_PRESENT		    = 0;
        public const int NL_NEEDS_LOADED	= 1;
        public const int NL_UNREFERENCED	= 2;

        static model()
        {
            for(int kk = 0; kk < MAX_MOD_KNOWN; kk++) mod_known[kk] = new model_t();
        }

        /*
        ===============
        Mod_Init
        ===============
        */
        public static void Mod_Init ()
        {
            for (int kk = 0; kk < bspfile.MAX_MAP_LEAFS / 8; kk++)
                mod_novis[kk] = 0xff;
        }

        /*
        ===============
        Mod_Extradata

        Caches the data if needed
        ===============
        */
        public static object Mod_Extradata(model_t mod)
        {
            object r;

            r = mod.cache;
            if (r != null)
                return r;

            Mod_LoadModel(mod, true);

            if (mod.cache == null)
                sys_linux.Sys_Error("Mod_Extradata: caching failed");
            return mod.cache;
        }

        /*
        ===============
        Mod_PointInLeaf
        ===============
        */
        public static mleaf_t Mod_PointInLeaf(double[] p, model_t model)
        {
            node_or_leaf_t  node;
            double          d;
            mplane_t        plane;

            if (model == null || model.nodes == null)
                sys_linux.Sys_Error("Mod_PointInLeaf: bad model");

            node = model.nodes[0];
            while (true)
            {
                if (node.contents < 0)
                    return (mleaf_t)node;
                mnode_t _node = (mnode_t)node;
                plane = _node.plane;
                d = mathlib.DotProduct(p, plane.normal) - plane.dist;
                if (d > 0)
                    node = _node.children[0];
                else
                    node = _node.children[1];
            }

            return null;	// never reached
        }

        /*
        ===================
        Mod_DecompressVis
        ===================
        */
        static byte[]	decompressed = new byte[bspfile.MAX_MAP_LEAFS/8];
        static byte[] Mod_DecompressVis (bspfile.ByteBuffer @in, model_t model)
        {
	        int		c;
	        int	    @out;
	        int		row;
            int     ofs = @in.ofs;

	        row = (model.numleafs+7)>>3;	
	        @out = 0;

	        if (@in.buffer == null)
	        {	// no vis info, so make all visible
		        while (row != 0)
		        {
			        decompressed[@out++] = 0xff;
			        row--;
		        }
		        return decompressed;		
	        }

	        do
	        {
		        if (@in.buffer[ofs] != 0)
		        {
			        decompressed[@out++] = @in.buffer[ofs++];
			        continue;
		        }
        	
		        c = @in.buffer[ofs+1];
		        ofs += 2;
		        while (c != 0)
		        {
			        decompressed[@out++] = 0;
			        c--;
		        }
	        } while (@out < row);
        	
	        return decompressed;
        }

        public static byte[] Mod_LeafPVS (mleaf_t leaf, model_t model)
        {
	        if (leaf == model.leafs[0])
		        return mod_novis;
	        return Mod_DecompressVis (leaf.compressed_vis, model);
        }

        /*
        ===================
        Mod_ClearAll
        ===================
        */
        public static void Mod_ClearAll ()
        {
	        int		i;
	        model_t	mod;

            icolor = 0;
            for (i=0, mod = mod_known[i]; i < mod_numknown; i++)
            {
                mod = mod_known[i];
		        mod.needload = NL_UNREFERENCED;
        //FIX FOR CACHE_ALLOC ERRORS:
		        if (mod.type == modtype_t.mod_sprite) mod.cache = null;
	        }
        }

        /*
        ==================
        Mod_FindName

        ==================
        */
        static model_t Mod_FindName (string name)
        {
	        int		i;
	        model_t	mod = null;
	        model_t	avail = null;

	        if (name.Length == 0)
		        sys_linux.Sys_Error ("Mod_ForName: NULL name");

        //
        // search the currently loaded models
        //
            for (i=0, mod = mod_known[i]; i < mod_numknown; i++, mod = mod_known[i])
	        {
		        if (mod.name != null && mod.name.CompareTo(name) == 0)
			        break;
		        if (mod.needload == NL_UNREFERENCED)
			        if (avail == null || mod.type != modtype_t.mod_alias)
				        avail = mod;
	        }
        			
	        if (i == mod_numknown)
	        {
		        if (mod_numknown == MAX_MOD_KNOWN)
		        {
			        if (avail != null)
			        {
				        mod = avail;
			        }
			        else
				        sys_linux.Sys_Error ("mod_numknown == MAX_MOD_KNOWN");
		        }
		        else
			        mod_numknown++;
		        mod.name = name;
		        mod.needload = NL_NEEDS_LOADED;
	        }

	        return mod;
        }

        /*
        ==================
        Mod_TouchModel

        ==================
        */
        public static void Mod_TouchModel (string name)
        {
	        model_t	mod;
        	
	        mod = Mod_FindName (name);
        	
	        if (mod.needload == NL_PRESENT)
	        {
	        }
        }

        /*
        ==================
        Mod_LoadModel

        Loads a model into the cache
        ==================
        */
        static model_t Mod_LoadModel (model_t mod, bool crash)
        {
	        //unsigned *buf;
            byte[]      buf;
	        byte[]	    stackbuf = new byte[1024];		// avoid dirtying the cache heap

	        if (mod.type == modtype_t.mod_alias)
	        {
	        }
	        else
	        {
		        if (mod.needload == NL_PRESENT)
			        return mod;
	        }

        //
        // because the world is so huge, load it one piece at a time
        //
        	
        //
        // load the file
        //
	        buf = common.COM_LoadStackFile (mod.name, stackbuf, stackbuf.Length);
	        if (buf == null)
	        {
		        if (crash)
                    sys_linux.Sys_Error("Mod_NumForName: " + mod.name + " not found");
		        return null;
	        }
        	
        //
        // allocate a new model
        //
	        common.COM_FileBase (mod.name, ref loadname);
        	
	        loadmodel = mod;

        //
        // fill it in
        //

        // call the apropriate loader
	        mod.needload = NL_PRESENT;

            int aux = (buf[3] << 24) | (buf[2] << 16) | (buf[1] << 8) | buf[0];
	        switch (aux)
	        {
	        case IDPOLYHEADER:
		        Mod_LoadAliasModel (mod, buf);
		        break;
        		
	        case IDSPRITEHEADER:
		        Mod_LoadSpriteModel (mod, buf);
		        break;
        	
	        default:
		        Mod_LoadBrushModel (mod, buf);
		        break;
	        }

	        return mod;
        }

        /*
        ==================
        Mod_ForName

        Loads in a model for the given name
        ==================
        */
        public static model_t Mod_ForName (string name, bool crash)
        {
	        model_t	mod;

	        mod = Mod_FindName (name);

	        return Mod_LoadModel (mod, crash);
        }


        /*
        ===============================================================================

					        BRUSHMODEL LOADING

        ===============================================================================
        */

        static byte[] mod_base;
        const int ANIM_CYCLE = 2;

        /*
        =================
        Mod_LoadTextures
        =================
        */
        static void Mod_LoadTextures(bspfile.lump_t l)
        {
	        int		                i, j, pixels, num, max, altmax;
	        bspfile.miptex_t	    mt;
	        texture_t	            tx, tx2;
	        texture_t[]	            anims = new texture_t[10];
	        texture_t[]	            altanims = new texture_t[10];
	        bspfile.dmiptexlump_t   m;

            for(int kk = 0; kk < 10; kk++)
            {
                anims[kk] = new texture_t();
                altanims[kk] = new texture_t();
            }

	        if (l.filelen == 0)
	        {
		        loadmodel.textures = null;
		        return;
	        }
            bspfile.ByteBuffer buf = new bspfile.ByteBuffer(mod_base, l.fileofs);
            m = (bspfile.dmiptexlump_t)buf;
        	
	        loadmodel.numtextures = m.nummiptex;
	        loadmodel.textures = new texture_t[m.nummiptex];

	        for (i=0 ; i<m.nummiptex ; i++)
	        {
                loadmodel.textures[i] = new texture_t();
		        if (m.dataofs[i] == -1)
			        continue;
                buf.ofs = l.fileofs + m.dataofs[i];
		        mt = (bspfile.miptex_t)buf;
        		
		        if ( (mt.width & 15) != 0 || (mt.height & 15) != 0 )
			        sys_linux.Sys_Error ("Texture " + mt.name + " is not 16 aligned");
		        pixels = (int)(mt.width*mt.height/64*85);
		        tx = new texture_t();
                tx.pixels = new byte[pixels];
		        loadmodel.textures[i] = tx;

		        tx.name = mt.name;
		        tx.width = mt.width;
		        tx.height = mt.height;
		        for (j=0 ; j<bspfile.MIPLEVELS ; j++)
			        tx.offsets[j] = mt.offsets[j]/* + sizeof_texture_t*/ - bspfile.sizeof_miptex_t;
		        // the pixels immediately follow the structures
                Buffer.BlockCopy(buf.buffer, buf.ofs + bspfile.sizeof_miptex_t, tx.pixels, 0, pixels);

                if (mt.name.StartsWith("sky"))
                    render.R_InitSky(tx);
	        }

        //
        // sequence the animations
        //
	        for (i=0 ; i<m.nummiptex ; i++)
	        {
		        tx = loadmodel.textures[i];
		        if (tx == null || tx.name[0] != '+')
			        continue;
 		        if (tx.anim_next != null)
			        continue;	// allready sequenced

	        // find the number of frames in the animation
		        //memset (anims, 0, sizeof(anims));
		        //memset (altanims, 0, sizeof(altanims));

		        max = tx.name[1];
		        altmax = 0;
		        if (max >= 'a' && max <= 'z')
			        max -= 'a' - 'A';
		        if (max >= '0' && max <= '9')
		        {
			        max -= '0';
			        altmax = 0;
			        anims[max] = tx;
			        max++;
		        }
		        else if (max >= 'A' && max <= 'J')
		        {
			        altmax = max - 'A';
			        max = 0;
			        altanims[altmax] = tx;
 			        altmax++;
		        }
		        else
			        sys_linux.Sys_Error ("Bad animating texture " + tx.name);

		        for (j=i+1 ; j<m.nummiptex ; j++)
		        {
			        tx2 = loadmodel.textures[j];
			        if (tx2 == null || tx2.name[0] != '+')
				        continue;
			        if (tx2.name.Substring(2).CompareTo(tx.name.Substring(2)) != 0)
				        continue;

			        num = tx2.name[1];
			        if (num >= 'a' && num <= 'z')
				        num -= 'a' - 'A';
			        if (num >= '0' && num <= '9')
			        {
				        num -= '0';
				        anims[num] = tx2;
				        if (num+1 > max)
					        max = num + 1;
			        }
			        else if (num >= 'A' && num <= 'J')
			        {
				        num = num - 'A';
				        altanims[num] = tx2;
				        if (num+1 > altmax)
					        altmax = num+1;
			        }
			        else
				        sys_linux.Sys_Error ("Bad animating texture " + tx.name);
		        }
        		
	        // link them all together
		        for (j=0 ; j<max ; j++)
		        {
			        tx2 = anims[j];
			        if (tx2 == null)
				        sys_linux.Sys_Error ("Missing frame " + j + " of " + tx.name);
			        tx2.anim_total = max * ANIM_CYCLE;
			        tx2.anim_min = j * ANIM_CYCLE;
			        tx2.anim_max = (j+1) * ANIM_CYCLE;
			        tx2.anim_next = anims[ (j+1)%max ];
			        if (altmax != 0)
				        tx2.alternate_anims = altanims[0];
		        }
		        for (j=0 ; j<altmax ; j++)
		        {
			        tx2 = altanims[j];
			        if (tx2 == null)
				        sys_linux.Sys_Error ("Missing frame " + j + " of " + tx.name);
			        tx2.anim_total = altmax * ANIM_CYCLE;
			        tx2.anim_min = j * ANIM_CYCLE;
			        tx2.anim_max = (j+1) * ANIM_CYCLE;
			        tx2.anim_next = altanims[ (j+1)%altmax ];
			        if (max != 0)
				        tx2.alternate_anims = anims[0];
		        }
	        }
        }

        /*
        =================
        Mod_LoadLighting
        =================
        */
        static void Mod_LoadLighting(bspfile.lump_t l)
        {
            if (l.filelen == 0)
            {
                loadmodel.lightdata = null;
                return;
            }
            loadmodel.lightdata = new byte[l.filelen];
            Buffer.BlockCopy(mod_base, l.fileofs, loadmodel.lightdata, 0, l.filelen);
        }

        /*
        =================
        Mod_LoadVisibility
        =================
        */
        static void Mod_LoadVisibility(bspfile.lump_t l)
        {
            if (l.filelen == 0)
            {
                loadmodel.visdata = null;
                return;
            }
            loadmodel.visdata = new byte[l.filelen];
            Buffer.BlockCopy(mod_base, l.fileofs, loadmodel.visdata, 0, l.filelen);
        }

        /*
        =================
        Mod_LoadEntities
        =================
        */
        static void Mod_LoadEntities(bspfile.lump_t l)
        {
            if (l.filelen == 0)
            {
                loadmodel.entities = null;
                return;
            }
            loadmodel.entities = new char[l.filelen];
            byte[] tmp = new byte[l.filelen];
            Buffer.BlockCopy(mod_base, l.fileofs, tmp, 0, l.filelen);
            for (int kk = 0; kk < l.filelen; kk++)
                loadmodel.entities[kk] = (char)tmp[kk];
            tmp = null;
        }

        /*
        =================
        Mod_LoadVertexes
        =================
        */
        static void Mod_LoadVertexes(bspfile.lump_t l)
        {
	        bspfile.dvertex_t[]	@in;
	        mvertex_t[]	        @out;
	        int			        i, count;

	        if ((l.filelen % bspfile.sizeof_dvertex_t) != 0)
		        sys_linux.Sys_Error ("MOD_LoadBmodel: funny lump size in " + loadmodel.name);
	        count = l.filelen / bspfile.sizeof_dvertex_t;
            bspfile.ByteBuffer buf = new bspfile.ByteBuffer(mod_base, l.fileofs);
            @in = new bspfile.dvertex_t[count];
            @out = new mvertex_t[count];
            for (int kk = 0; kk < count; kk++)
            {
                @in[kk] = (bspfile.dvertex_t)buf;
                buf.ofs += bspfile.sizeof_dvertex_t;
                @out[kk] = new mvertex_t();
            }

	        loadmodel.vertexes = @out;
	        loadmodel.numvertexes = count;

	        for ( i=0 ; i<count ; i++)
	        {
		        @out[i].position[0] = @in[i].point[0];
		        @out[i].position[1] = @in[i].point[1];
		        @out[i].position[2] = @in[i].point[2];
	        }
        }

        /*
        =================
        Mod_LoadSubmodels
        =================
        */
        static void Mod_LoadSubmodels(bspfile.lump_t l)
        {
	        bspfile.dmodel_t[]	@out;
	        int			        i, j, count;

	        if ((l.filelen % bspfile.sizeof_dmodel_t) != 0)
		        sys_linux.Sys_Error ("MOD_LoadBmodel: funny lump size in " + loadmodel.name);
	        count = l.filelen / bspfile.sizeof_dmodel_t;
	        @out = new bspfile.dmodel_t[count];
            bspfile.ByteBuffer buf = new bspfile.ByteBuffer(mod_base, l.fileofs);
            for (int kk = 0; kk < count; kk++)
            {
                @out[kk] = (bspfile.dmodel_t)buf;
                buf.ofs += bspfile.sizeof_dmodel_t;
            }

	        loadmodel.submodels = @out;
	        loadmodel.numsubmodels = count;

	        for ( i=0 ; i<count ; i++)
	        {
		        for (j=0 ; j<3 ; j++)
		        {	// spread the mins / maxs by a pixel
			        @out[i].mins[j] -= 1;
			        @out[i].maxs[j] += 1;
		        }
	        }
        }

        /*
        =================
        Mod_LoadEdges
        =================
        */
        static void Mod_LoadEdges(bspfile.lump_t l)
        {
	        bspfile.dedge_t[]   @in;
	        medge_t[]           @out;
	        int 	            i, count;

	        if ((l.filelen % bspfile.sizeof_dedge_t) != 0)
		        sys_linux.Sys_Error ("MOD_LoadBmodel: funny lump size in " + loadmodel.name);
	        count = l.filelen / bspfile.sizeof_dedge_t;
            bspfile.ByteBuffer buf = new bspfile.ByteBuffer(mod_base, l.fileofs);
            @in = new bspfile.dedge_t[count];
            @out = new medge_t[count];
            for (int kk = 0; kk < count; kk++)
            {
                @in[kk] = (bspfile.dedge_t)buf;
                buf.ofs += bspfile.sizeof_dedge_t;
                @out[kk] = new medge_t();
            }

	        loadmodel.edges = @out;
	        loadmodel.numedges = count;

	        for ( i=0 ; i<count ; i++)
	        {
		        @out[i].v[0] = (ushort)@in[i].v[0];
		        @out[i].v[1] = (ushort)@in[i].v[1];
	        }
        }

        /*
        =================
        Mod_LoadTexinfo
        =================
        */
        static void Mod_LoadTexinfo(bspfile.lump_t l)
        {
	        bspfile.texinfo_t[] @in;
	        mtexinfo_t[]        @out;
	        int 	            i, j, count;
	        int		            miptex;
	        double	            len1, len2;

	        if ((l.filelen % bspfile.sizeof_texinfo_t) != 0)
		        sys_linux.Sys_Error ("MOD_LoadBmodel: funny lump size in " + loadmodel.name);
            count = l.filelen / bspfile.sizeof_texinfo_t;
            bspfile.ByteBuffer buf = new bspfile.ByteBuffer(mod_base, l.fileofs);
            @in = new bspfile.texinfo_t[count];
            @out = new mtexinfo_t[count];
            for (int kk = 0; kk < count; kk++)
            {
                @in[kk] = (bspfile.texinfo_t)buf;
                buf.ofs += bspfile.sizeof_texinfo_t;
                @out[kk] = new mtexinfo_t();
            }

	        loadmodel.texinfo = @out;
	        loadmodel.numtexinfo = count;

	        for ( i=0 ; i<count ; i++)
	        {
                for(int k = 0; k < 2; k++)
    		        for (j=0 ; j<4 ; j++)
			            @out[i].vecs[k][j] = @in[i].vecs[k][j];
		        len1 = mathlib.Length (@out[i].vecs[0]);
                len2 = mathlib.Length (@out[i].vecs[1]);
		        len1 = (len1 + len2)/2;
		        if (len1 < 0.32)
			        @out[i].mipadjust = 4;
		        else if (len1 < 0.49)
			        @out[i].mipadjust = 3;
		        else if (len1 < 0.99)
			        @out[i].mipadjust = 2;
		        else
			        @out[i].mipadjust = 1;

                miptex = @in[i].miptex;
                @out[i].flags = @in[i].flags;
        	
		        if (loadmodel.textures == null)
		        {
                    @out[i].texture = render.r_notexture_mip;	// checkerboard texture
			        @out[i].flags = 0;
		        }
		        else
		        {
			        if (miptex >= loadmodel.numtextures)
				        sys_linux.Sys_Error ("miptex >= loadmodel.numtextures");
			        @out[i].texture = loadmodel.textures[miptex];
			        if (@out[i].texture == null)
			        {
                        @out[i].texture = render.r_notexture_mip; // texture not found
                        @out[i].flags = 0;
			        }
		        }
	        }
        }

        /*
        ================
        CalcSurfaceExtents

        Fills in s.texturemins[] and s.extents[]
        ================
        */
        static void CalcSurfaceExtents (msurface_t s)
        {
	        double[]	mins = new double[2], maxs = new double[2];
            double      val;
	        int		    i,j, e;
	        mvertex_t	v;
	        mtexinfo_t	tex;
	        int[]		bmins = new int[2], bmaxs = new int[2];

	        mins[0] = mins[1] = 999999;
	        maxs[0] = maxs[1] = -99999;

	        tex = s.texinfo;
        	
	        for (i=0 ; i<s.numedges ; i++)
	        {
		        e = loadmodel.surfedges[s.firstedge+i];
		        if (e >= 0)
			        v = loadmodel.vertexes[loadmodel.edges[e].v[0]];
		        else
			        v = loadmodel.vertexes[loadmodel.edges[-e].v[1]];
        		
		        for (j=0 ; j<2 ; j++)
		        {
			        val = v.position[0] * tex.vecs[j][0] + 
				        v.position[1] * tex.vecs[j][1] +
				        v.position[2] * tex.vecs[j][2] +
				        tex.vecs[j][3];
			        if (val < mins[j])
				        mins[j] = val;
			        if (val > maxs[j])
				        maxs[j] = val;
		        }
	        }

	        for (i=0 ; i<2 ; i++)
	        {	
		        bmins[i] = (int)Math.Floor(mins[i]/16);
		        bmaxs[i] = (int)Math.Ceiling(maxs[i]/16);

		        s.texturemins[i] = (short)(bmins[i] * 16);
		        s.extents[i] = (short)((bmaxs[i] - bmins[i]) * 16);
		        /*if ( (tex.flags & bspfile.TEX_SPECIAL) == 0 && s.extents[i] > 256)
			        sys_linux.Sys_Error ("Bad surface extents");*/
	        }
        }
        
        /*
        =================
        Mod_LoadFaces
        =================
        */
        static void Mod_LoadFaces(bspfile.lump_t l)
        {
	        bspfile.dface_t[]   @in;
	        msurface_t[] 	    @out;
	        int			        i, count, surfnum;
	        int			        planenum, side;

	        if ((l.filelen % bspfile.sizeof_dface_t) != 0)
		        sys_linux.Sys_Error ("MOD_LoadBmodel: funny lump size in " + loadmodel.name);
	        count = l.filelen / bspfile.sizeof_dface_t;
            bspfile.ByteBuffer buf = new bspfile.ByteBuffer(mod_base, l.fileofs);
            @in = new bspfile.dface_t[count];
            @out = new msurface_t[count];
            for (int kk = 0; kk < count; kk++)
            {
                @in[kk] = (bspfile.dface_t)buf;
                buf.ofs += bspfile.sizeof_dface_t;
                @out[kk] = new msurface_t();
            }

	        loadmodel.surfaces = @out;
	        loadmodel.numsurfaces = count;

            int color = colors[(icolor++) % 8];
	        for ( surfnum=0 ; surfnum<count ; surfnum++)
	        {
		        @out[surfnum].firstedge = @in[surfnum].firstedge;
                @out[surfnum].numedges = @in[surfnum].numedges;		
		        @out[surfnum].flags = 0;

		        planenum = @in[surfnum].planenum;
		        side = @in[surfnum].side;
		        if (side != 0)
			        @out[surfnum].flags |= SURF_PLANEBACK;			

		        @out[surfnum].plane = loadmodel.planes[planenum];
                
                @out[surfnum].color = color;
                color += 0x40;

		        @out[surfnum].texinfo = loadmodel.texinfo[@in[surfnum].texinfo];

		        CalcSurfaceExtents (@out[surfnum]);

	        // lighting info

		        for (i=0 ; i<bspfile.MAXLIGHTMAPS ; i++)
			        @out[surfnum].styles[i] = @in[surfnum].styles[i];
		        i = @in[surfnum].lightofs;
		        if (i == -1)
			        @out[surfnum].samples = null;
		        else
			        @out[surfnum].samples = new bspfile.ByteBuffer(loadmodel.lightdata, i);

            // set the drawing flags flag
        		
	            if (@out[surfnum].texinfo.texture.name.StartsWith("sky"))	// sky
	            {
		            @out[surfnum].flags |= (SURF_DRAWSKY | SURF_DRAWTILED);
		            continue;
	            }
        		
	            if (@out[surfnum].texinfo.texture.name.StartsWith("*"))		// turbulent
	            {
		            @out[surfnum].flags |= (SURF_DRAWTURB | SURF_DRAWTILED);
		            for (i=0 ; i<2 ; i++)
		            {
			            @out[surfnum].extents[i] = 16384;
			            @out[surfnum].texturemins[i] = -8192;
		            }
		            continue;
	            }
	        }
        }

        /*
        =================
        Mod_SetParent
        =================
        */
        static void Mod_SetParent(node_or_leaf_t node, mnode_t parent)
        {
            node.parent = parent;
            if (node.contents < 0)
                return;
            mnode_t _node = (mnode_t)node;
            Mod_SetParent((node_or_leaf_t)_node.children[0], _node);
            Mod_SetParent((node_or_leaf_t)_node.children[1], _node);
        }

        /*
        =================
        Mod_LoadNodes
        =================
        */
        static void Mod_LoadNodes(bspfile.lump_t l)
        {
	        int			        i, j, count, p;
	        bspfile.dnode_t[]   @in;
	        mnode_t[] 	        @out;

	        if ((l.filelen % bspfile.sizeof_dnode_t) != 0)
		        sys_linux.Sys_Error ("MOD_LoadBmodel: funny lump size in " + loadmodel.name);
	        count = l.filelen / bspfile.sizeof_dnode_t;
            bspfile.ByteBuffer buf = new bspfile.ByteBuffer(mod_base, l.fileofs);
            @in = new bspfile.dnode_t[count];
            @out = new mnode_t[count];
            for (int kk = 0; kk < count; kk++)
            {
                @in[kk] = (bspfile.dnode_t)buf;
                buf.ofs += bspfile.sizeof_dnode_t;
                @out[kk] = new mnode_t();
            }

	        loadmodel.nodes = @out;
	        loadmodel.numnodes = count;

	        for ( i=0 ; i<count ; i++)
	        {
		        for (j=0 ; j<3 ; j++)
		        {
			        @out[i].minmaxs[j] = @in[i].mins[j];
			        @out[i].minmaxs[3+j] = @in[i].maxs[j];
		        }
        	
		        p = @in[i].planenum;
		        @out[i].plane = loadmodel.planes[p];

		        @out[i].firstsurface = @in[i].firstface;
		        @out[i].numsurfaces = @in[i].numfaces;
        		
		        for (j=0 ; j<2 ; j++)
		        {
			        p = @in[i].children[j];
			        if (p >= 0)
				        @out[i].children[j] = loadmodel.nodes[p];
			        else
				        @out[i].children[j] = loadmodel.leafs[-1 - p];
		        }
	        }
        	
	        Mod_SetParent (loadmodel.nodes[0], null);	// sets nodes and leafs
        }

        /*
        =================
        Mod_LoadLeafs
        =================
        */
        static void Mod_LoadLeafs(bspfile.lump_t l)
        {
	        bspfile.dleaf_t[] 	@in;
	        mleaf_t[] 	        @out;
	        int			        i, j, count, p;

	        if ((l.filelen % bspfile.sizeof_dleaf_t) != 0)
		        sys_linux.Sys_Error ("MOD_LoadBmodel: funny lump size in " + loadmodel.name);
	        count = l.filelen / bspfile.sizeof_dleaf_t;
            bspfile.ByteBuffer buf = new bspfile.ByteBuffer(mod_base, l.fileofs);
            @in = new bspfile.dleaf_t[count];
            @out = new mleaf_t[count];
            for (int kk = 0; kk < count; kk++)
            {
                @in[kk] = (bspfile.dleaf_t)buf;
                buf.ofs += bspfile.sizeof_dleaf_t;
                @out[kk] = new mleaf_t();
            }

	        loadmodel.leafs = @out;
	        loadmodel.numleafs = count;

	        for ( i=0 ; i<count ; i++)
	        {
		        for (j=0 ; j<3 ; j++)
		        {
			        @out[i].minmaxs[j] = @in[i].mins[j];
			        @out[i].minmaxs[3+j] = @in[i].maxs[j];
		        }

		        p = @in[i].contents;
		        @out[i].contents = p;

		        @out[i].firstmarksurface = new helper.ObjectBuffer(loadmodel.marksurfaces, @in[i].firstmarksurface);
		        @out[i].nummarksurfaces = @in[i].nummarksurfaces;
        		
		        p = @in[i].visofs;
                if (p == -1)
			        @out[i].compressed_vis = null;
		        else
			        @out[i].compressed_vis = new bspfile.ByteBuffer(loadmodel.visdata, p);
		        @out[i].efrags = null;
        		
		        for (j=0 ; j<4 ; j++)
                    @out[i].ambient_sound_level[j] = @in[i].ambient_level[j];
	        }
        }

        /*
        =================
        Mod_LoadClipnodes
        =================
        */
        static void Mod_LoadClipnodes(bspfile.lump_t l)
        {
        }

        /*
        =================
        Mod_MakeHull0

        Deplicate the drawing hull structure as a clipping hull
        =================
        */
        static void Mod_MakeHull0()
        {
        }

        /*
        =================
        Mod_LoadMarksurfaces
        =================
        */
        static void Mod_LoadMarksurfaces(bspfile.lump_t l)
        {	
	        int		        i, j, count;
	        short[]		    @in;
	        msurface_t[]    @out;
        	
	        if ((l.filelen % sizeof(short)) != 0)
		        sys_linux.Sys_Error ("MOD_LoadBmodel: funny lump size in " + loadmodel.name);
	        count = l.filelen / sizeof(short);
            @in = new short[count];
            @out = new msurface_t[count];
            int ofs = l.fileofs;
            for (int kk = 0; kk < count; kk++)
            {
                @in[kk] = BitConverter.ToInt16(mod_base, ofs); ofs += sizeof(short);
            }

	        loadmodel.marksurfaces = @out;
	        loadmodel.nummarksurfaces = count;

	        for ( i=0 ; i<count ; i++)
	        {
		        j = @in[i];
		        if (j >= loadmodel.numsurfaces)
			        sys_linux.Sys_Error ("Mod_ParseMarksurfaces: bad surface number");
		        @out[i] = loadmodel.surfaces[j];
	        }
        }

        /*
        =================
        Mod_LoadSurfedges
        =================
        */
        static void Mod_LoadSurfedges(bspfile.lump_t l)
        {	
	        int		    i, count;
	        int[]		@out;
        	
	        if ((l.filelen % sizeof(int)) != 0)
		        sys_linux.Sys_Error ("MOD_LoadBmodel: funny lump size in " + loadmodel.name);
	        count = l.filelen / sizeof(int);
            @out = new int[count];
            int ofs = l.fileofs;
            for (int kk = 0; kk < count; kk++)
            {
                @out[kk] = BitConverter.ToInt32(mod_base, ofs); ofs += sizeof(int);
            }

	        loadmodel.surfedges = @out;
	        loadmodel.numsurfedges = count;
        }

        /*
        =================
        Mod_LoadPlanes
        =================
        */
        static void Mod_LoadPlanes(bspfile.lump_t l)
        {
	        int			        i, j;
	        mplane_t[]	        @out;
	        bspfile.dplane_t[]  @in;
	        int			        count;
	        int			        bits;

	        if ((l.filelen % bspfile.sizeof_dplane_t) != 0)
		        sys_linux.Sys_Error ("MOD_LoadBmodel: funny lump size in " + loadmodel.name);
	        count = l.filelen / bspfile.sizeof_dplane_t;
            bspfile.ByteBuffer buf = new bspfile.ByteBuffer(mod_base, l.fileofs);
            @in = new bspfile.dplane_t[count];
            @out = new mplane_t[count];
            for (int kk = 0; kk < count; kk++)
            {
                @in[kk] = (bspfile.dplane_t)buf;
                buf.ofs += bspfile.sizeof_dplane_t;
                @out[kk] = new mplane_t();
            }
        	
	        loadmodel.planes = @out;
	        loadmodel.numplanes = count;

	        for ( i=0 ; i<count ; i++)
	        {
		        bits = 0;
		        for (j=0 ; j<3 ; j++)
		        {
			        @out[i].normal[j] = @in[i].normal[j];
			        if (@out[i].normal[j] < 0)
				        bits |= 1<<j;
		        }

		        @out[i].dist = @in[i].dist;
		        @out[i].type = (byte)@in[i].type;
		        @out[i].signbits = (byte)bits;
	        }
        }

        /*
        =================
        RadiusFromBounds
        =================
        */
        static double RadiusFromBounds(double[] mins, double[] maxs)
        {
            int         i;
            double[]    corner = new double[3];

            for (i = 0; i < 3; i++)
            {
                corner[i] = Math.Abs(mins[i]) > Math.Abs(maxs[i]) ? Math.Abs(mins[i]) : Math.Abs(maxs[i]);
            }

            return mathlib.Length(corner);
        }

        /*
        =================
        Mod_LoadBrushModel
        =================
        */
        static void Mod_LoadBrushModel (model_t mod, byte[] buffer)
        {
	        int			        i, j;
	        bspfile.dheader_t	header;
	        bspfile.dmodel_t 	bm;
        	
	        loadmodel.type = modtype_t.mod_brush;
        	
	        header = (bspfile.dheader_t)buffer;

	        i = header.version;
            if (i != bspfile.BSPVERSION)
		        sys_linux.Sys_Error ("Mod_LoadBrushModel: " + mod.name + " has wrong version number (" + i + " should be " + bspfile.BSPVERSION + ")");

        // swap all the lumps
	        mod_base = buffer;

        // load into heap
        	
	        Mod_LoadVertexes (header.lumps[bspfile.LUMP_VERTEXES]);
            Mod_LoadEdges (header.lumps[bspfile.LUMP_EDGES]);
            Mod_LoadSurfedges (header.lumps[bspfile.LUMP_SURFEDGES]);
            Mod_LoadTextures (header.lumps[bspfile.LUMP_TEXTURES]);
            Mod_LoadLighting (header.lumps[bspfile.LUMP_LIGHTING]);
            Mod_LoadPlanes (header.lumps[bspfile.LUMP_PLANES]);
            Mod_LoadTexinfo (header.lumps[bspfile.LUMP_TEXINFO]);
            Mod_LoadFaces (header.lumps[bspfile.LUMP_FACES]);
            Mod_LoadMarksurfaces (header.lumps[bspfile.LUMP_MARKSURFACES]);
            Mod_LoadVisibility (header.lumps[bspfile.LUMP_VISIBILITY]);
            Mod_LoadLeafs (header.lumps[bspfile.LUMP_LEAFS]);
            Mod_LoadNodes (header.lumps[bspfile.LUMP_NODES]);
            Mod_LoadClipnodes (header.lumps[bspfile.LUMP_CLIPNODES]);
            Mod_LoadEntities (header.lumps[bspfile.LUMP_ENTITIES]);
            Mod_LoadSubmodels (header.lumps[bspfile.LUMP_MODELS]);

	        Mod_MakeHull0 ();
        	
	        mod.numframes = 2;		// regular and alternate animation
	        mod.flags = 0;
        	
        //
        // set up the submodels (FIXME: this is confusing)
        //
	        for (i=0 ; i<mod.numsubmodels ; i++)
	        {
		        bm = mod.submodels[i];

		        mod.hulls[0].firstclipnode = bm.headnode[0];
		        for (j=1 ; j<bspfile.MAX_MAP_HULLS ; j++)
		        {
			        mod.hulls[j].firstclipnode = bm.headnode[j];
			        mod.hulls[j].lastclipnode = mod.numclipnodes-1;
		        }
        		
		        mod.firstmodelsurface = bm.firstface;
		        mod.nummodelsurfaces = bm.numfaces;
        		
		        mathlib.VectorCopy (bm.maxs, ref mod.maxs);
		        mathlib.VectorCopy (bm.mins, ref mod.mins);
		        mod.radius = RadiusFromBounds (mod.mins, mod.maxs);
        		
		        mod.numleafs = bm.visleafs;

		        if (i < mod.numsubmodels-1)
		        {	// duplicate the basic information
			        string	name;

			        name = "*" + (i+1);
			        loadmodel = Mod_FindName (name);
//			        *loadmodel = *mod;
                    loadmodel.Clone(mod);
			        loadmodel.name = name;
			        mod = loadmodel;
		        }
	        }
        }

        /*
        ==============================================================================

        ALIAS MODELS

        ==============================================================================
        */

        /*
        =================
        Mod_LoadAliasFrame
        =================
        */
        static void Mod_LoadAliasFrame(bspfile.ByteBuffer pin, ref object pframeindex, int numv,
            trivertx_t pbboxmin, trivertx_t pbboxmax, aliashdr_t pheader, string name)
        {
	        trivertx_t[]	pframe;
            trivertx_t      pinframe;
	        int				i, j;
	        daliasframe_t	pdaliasframe;

	        pdaliasframe = (daliasframe_t)pin;

	        name = pdaliasframe.name;

	        for (i=0 ; i<3 ; i++)
	        {
	        // these are byte values, so we don't have to worry about
	        // endianness
		        pbboxmin.v[i] = pdaliasframe.bboxmin.v[i];
		        pbboxmax.v[i] = pdaliasframe.bboxmax.v[i];
	        }

            pin.ofs += sizeof_daliasframe_t;
            pframe = new trivertx_t[numv];

	        pframeindex = (object)pframe;

	        for (j=0 ; j<numv ; j++)
	        {
		        int		k;

                pinframe = (trivertx_t)pin;

	        // these are all byte values, so no need to deal with endianness
                pframe[j] = new trivertx_t();
                pframe[j].lightnormalindex = pinframe.lightnormalindex;

		        for (k=0 ; k<3 ; k++)
		        {
			        pframe[j].v[k] = pinframe.v[k];
		        }
                pin.ofs += sizeof_trivertx_t;
            }
        }

        /*
        =================
        Mod_LoadAliasGroup
        =================
        */
        static void Mod_LoadAliasGroup(bspfile.ByteBuffer pin, ref object pframeindex, int numv,
            trivertx_t pbboxmin, trivertx_t pbboxmax, aliashdr_t pheader, string name)
        {
	        daliasgroup_t		pingroup;
	        maliasgroup_t		paliasgroup;
	        int					i, numframes;
	        double[]			poutintervals;
        	
	        pingroup = (daliasgroup_t)pin;

	        numframes = pingroup.numframes;

	        paliasgroup = new maliasgroup_t();
            paliasgroup.frames = new maliasgroupframedesc_t[numframes];
            for (int kk = 0; kk < numframes; kk++) paliasgroup.frames[kk] = new maliasgroupframedesc_t();

	        paliasgroup.numframes = numframes;

	        for (i=0 ; i<3 ; i++)
	        {
	        // these are byte values, so we don't have to worry about endianness
		        pbboxmin.v[i] = pingroup.bboxmin.v[i];
		        pbboxmax.v[i] = pingroup.bboxmax.v[i];
	        }

	        pframeindex = (object)paliasgroup;

            pin.ofs += sizeof_daliasgroup_t;

            poutintervals = new double[numframes];

            paliasgroup.intervals = poutintervals;

            for (i = 0; i < numframes; i++)
            {
                poutintervals[i] = BitConverter.ToSingle(pin.buffer, pin.ofs); pin.ofs += sizeof(Single);
                if (poutintervals[i] <= 0.0)
                    sys_linux.Sys_Error("Mod_LoadAliasGroup: interval<=0");
            }

	        for (i=0 ; i<numframes ; i++)
	        {
		        Mod_LoadAliasFrame (pin,
							        ref paliasgroup.frames[i].frame,
							        numv,
							        paliasgroup.frames[i].bboxmin,
							        paliasgroup.frames[i].bboxmax,
							        pheader, name);
	        }
        }

        /*
        =================
        Mod_LoadAliasSkin
        =================
        */
        static void Mod_LoadAliasSkin(bspfile.ByteBuffer pin, ref object pskinindex, int skinsize, aliashdr_t pheader)
        {
	        int		i;
	        byte[]	pskin;

            pskin = new byte[skinsize * render.r_pixbytes];
	        pskinindex = (object)pskin;

	        if (render.r_pixbytes == 1)
	        {
                Buffer.BlockCopy(pin.buffer, pin.ofs, pskin, 0, skinsize);
	        }
            else if (render.r_pixbytes == 2)
	        {
	        }
	        else
	        {
                sys_linux.Sys_Error("Mod_LoadAliasSkin: driver set invalid r_pixbytes: " + render.r_pixbytes + "\n");
	        }

	        pin.ofs += skinsize;
        }

        /*
        =================
        Mod_LoadAliasSkinGroup
        =================
        */
        static void Mod_LoadAliasSkinGroup(bspfile.ByteBuffer pin, ref object pskinindex, int skinsize, aliashdr_t pheader)
        {
	        daliasskingroup_t		pinskingroup;
	        maliasskingroup_t		paliasskingroup;
	        int						i, numskins;
            double[]                poutskinintervals;

	        pinskingroup = (daliasskingroup_t)pin;

	        numskins = pinskingroup.numskins;

	        paliasskingroup = new maliasskingroup_t();
            paliasskingroup.skindescs = new maliasskindesc_t[numskins];
            for(int kk = 0; kk < numskins; kk++) paliasskingroup.skindescs[kk] = new maliasskindesc_t();

	        paliasskingroup.numskins = numskins;

	        pskinindex = (object)paliasskingroup;

            pin.ofs += sizeof_daliasskingroup_t;

            poutskinintervals = new double[numskins];

            paliasskingroup.intervals = poutskinintervals;

            for (i = 0; i < numskins; i++)
            {
                poutskinintervals[i] = BitConverter.ToSingle(pin.buffer, pin.ofs); pin.ofs += sizeof(Single);
                if (poutskinintervals[i] <= 0.0)
                    sys_linux.Sys_Error("Mod_LoadAliasSkinGroup: interval<=0");
            }

	        for (i=0 ; i<numskins ; i++)
	        {
		        Mod_LoadAliasSkin (pin, ref paliasskingroup.skindescs[i].skin, skinsize, pheader);
	        }
        }

        /*
        =================
        Mod_LoadAliasModel
        =================
        */
        static void Mod_LoadAliasModel(model_t mod, byte[] buffer)
        {
	        int					i;
        	mdl_t				pmodel, pinmodel;
	        stvert_t[]			pstverts;
            stvert_t            pinstverts;
	        aliashdr_t			pheader;
            mtriangle_t[]       ptri;
            dtriangle_t         pintriangles;
            int                 version, numframes, numskins;
            daliasframetype_t   pframetype;
            daliasskintype_t    pskintype;
            maliasskindesc_t[]  pskindesc;
            int                 skinsize;
	        int					start, end, total;
            bspfile.ByteBuffer  aux = new bspfile.ByteBuffer(buffer, 0);
        	
	        pinmodel = (mdl_t)buffer;

	        version = pinmodel.version;
	        if (version != ALIAS_VERSION)
		        sys_linux.Sys_Error (mod.name + " has wrong version number (" + version + " should be " + ALIAS_VERSION + ")");

        //
        // allocate space for a working header, plus all the data except the frames,
        // skin and group info
        //
	        pheader = new aliashdr_t();
            pmodel = new mdl_t();

        //	mod.cache = pheader;
	        mod.flags = pinmodel.flags;

            //
            // endian-adjust and copy the data, starting with the alias model header
            //
            pmodel.boundingradius = pinmodel.boundingradius;
            pmodel.numskins = pinmodel.numskins;
            pmodel.skinwidth = pinmodel.skinwidth;
            pmodel.skinheight = pinmodel.skinheight;

            if (pmodel.skinheight > draw.MAX_LBM_HEIGHT)
                sys_linux.Sys_Error("model " + mod.name + " has a skin taller than " + draw.MAX_LBM_HEIGHT);

            pmodel.numverts = pinmodel.numverts;

            if (pmodel.numverts <= 0)
                sys_linux.Sys_Error("model " + mod.name + " has no vertices");

            if (pmodel.numverts > render.MAXALIASVERTS)
                sys_linux.Sys_Error("model " + mod.name + " has too many vertices");

            pmodel.numtris = pinmodel.numtris;

            if (pmodel.numtris <= 0)
                sys_linux.Sys_Error("model " + mod.name + " has no triangles");

            pmodel.numframes = pinmodel.numframes;
            pmodel.size = pinmodel.size * render.ALIAS_BASE_SIZE_RATIO;
            mod.synctype = pinmodel.synctype;
            mod.numframes = pmodel.numframes;

            for (i = 0; i < 3; i++)
            {
                pmodel.scale[i] = pinmodel.scale[i];
                pmodel.scale_origin[i] = pinmodel.scale_origin[i];
                pmodel.eyeposition[i] = pinmodel.eyeposition[i];
            }

            numskins = pmodel.numskins;
            numframes = pmodel.numframes;

            if ((pmodel.skinwidth & 0x03) != 0)
                sys_linux.Sys_Error("Mod_LoadAliasModel: skinwidth not multiple of 4");

            pheader.model = pmodel;

            //
            // load the skins
            //
            skinsize = pmodel.skinheight * pmodel.skinwidth;

            if (numskins < 1)
                sys_linux.Sys_Error("Mod_LoadAliasModel: Invalid # of skins: " + numskins + "\n");

            aux.ofs += sizeof_mdl_t;

            pskindesc = new maliasskindesc_t[numskins];
            for(int kk = 0; kk < numskins; kk++) pskindesc[kk] = new maliasskindesc_t();

            pheader.skindesc = pskindesc;

            for (i = 0; i < numskins; i++)
            {
                aliasskintype_t skintype;

                pskintype = (daliasskintype_t)aux;
                skintype = pskintype.type;
                pskindesc[i].type = skintype;

                if (skintype == aliasskintype_t.ALIAS_SKIN_SINGLE)
                {
                    aux.ofs += sizeof_daliasskintype_t;
                    Mod_LoadAliasSkin(aux, ref pskindesc[i].skin, skinsize, pheader);
                }
                else
                {
                    aux.ofs += sizeof_daliasskintype_t;
                    Mod_LoadAliasSkinGroup(aux, ref pskindesc[i].skin, skinsize, pheader);
                }
            }

            //
            // set base s and t vertices
            //
            pstverts = new stvert_t[pmodel.numverts];

            pheader.stverts = pstverts;

            for (i = 0; i < pmodel.numverts; i++)
            {
                pinstverts = (stvert_t)aux;
                pstverts[i] = new stvert_t();
                pstverts[i].onseam = pinstverts.onseam;
                // put s and t in 16.16 format
                pstverts[i].s = pinstverts.s << 16;
                pstverts[i].t = pinstverts.t << 16;
                aux.ofs += sizeof_stvert_t;
            }

            //
            // set up the triangles
            //
            ptri = new mtriangle_t[pmodel.numtris];

            pheader.triangles = ptri;

            for (i = 0; i < pmodel.numtris; i++)
            {
                int j;

                pintriangles = (dtriangle_t)aux;
                ptri[i] = new mtriangle_t();
                ptri[i].facesfront = pintriangles.facesfront;

                for (j = 0; j < 3; j++)
                {
                    ptri[i].vertindex[j] =
                            pintriangles.vertindex[j];
                }
                aux.ofs += sizeof_dtriangle_t;
            }

            //
            // load the frames
            //
            if (numframes < 1)
                sys_linux.Sys_Error("Mod_LoadAliasModel: Invalid # of frames: " + numframes + "\n");

            pheader.frames = new maliasframedesc_t[numframes];
            for (i = 0; i < numframes; i++)
            {
                aliasframetype_t frametype;

                pframetype = (daliasframetype_t)aux;
                frametype = pframetype.type;
                pheader.frames[i] = new maliasframedesc_t();
                pheader.frames[i].type = frametype;

                if (frametype == aliasframetype_t.ALIAS_SINGLE)
                {
                    aux.ofs += sizeof_daliasframetype_t;
                    Mod_LoadAliasFrame(aux,
                                        ref pheader.frames[i].frame,
                                        pmodel.numverts,
                                        pheader.frames[i].bboxmin,
                                        pheader.frames[i].bboxmax,
                                        pheader, pheader.frames[i].name);
                }
                else
                {
                    aux.ofs += sizeof_daliasframetype_t;
                    Mod_LoadAliasGroup(aux,
                                        ref pheader.frames[i].frame,
                                        pmodel.numverts,
                                        pheader.frames[i].bboxmin,
                                        pheader.frames[i].bboxmax,
                                        pheader, pheader.frames[i].name);
                }
            }

	        mod.type = modtype_t.mod_alias;

        // FIXME: do this right
	        mod.mins[0] = mod.mins[1] = mod.mins[2] = -16;
	        mod.maxs[0] = mod.maxs[1] = mod.maxs[2] = 16;

        //
        // move the complete, relocatable alias model to the cache
        //	
	        mod.cache = pheader;
        }

        //=============================================================================

        /*
        =================
        Mod_LoadSpriteFrame
        =================
        */
        static void Mod_LoadSpriteFrame(bspfile.ByteBuffer pin, ref object ppframe)
        {
	        dspriteframe_t		pinframe;
	        mspriteframe_t		pspriteframe;
	        int					width, height, size;
            int[]               origin = new int[2];

	        pinframe = (dspriteframe_t)pin;

	        width = pinframe.width;
	        height = pinframe.height;
	        size = width * height;

            pspriteframe = new mspriteframe_t();
            pspriteframe.pixels = new byte[size*render.r_pixbytes];

	        ppframe = pspriteframe;

	        pspriteframe.width = width;
	        pspriteframe.height = height;
	        origin[0] = pinframe.origin[0];
	        origin[1] = pinframe.origin[1];

	        pspriteframe.up = origin[1];
	        pspriteframe.down = origin[1] - height;
	        pspriteframe.left = origin[0];
	        pspriteframe.right = width + origin[0];

	        if (render.r_pixbytes == 1)
	        {
                pin.ofs += sizeof_dspriteframe_t;
                for(int kk = 0; kk < size; kk++)
                    pspriteframe.pixels[kk] = pin.buffer[pin.ofs + kk];
	        }
	        else if (render.r_pixbytes == 2)
	        {
	        }
	        else
	        {
		        sys_linux.Sys_Error ("Mod_LoadSpriteFrame: driver set invalid r_pixbytes: " + render.r_pixbytes + "\n");
	        }

            pin.ofs += size;
        }

        /*
        =================
        Mod_LoadSpriteGroup
        =================
        */
        static void Mod_LoadSpriteGroup(bspfile.ByteBuffer pin, ref object ppframe)
        {
	        dspritegroup_t		pingroup;
	        mspritegroup_t		pspritegroup;
	        int					i, numframes;
	        double[]			poutintervals;

	        pingroup = (dspritegroup_t)pin;

	        numframes = pingroup.numframes;

	        pspritegroup = new mspritegroup_t();
            pspritegroup.frames = new mspriteframe_t[numframes];
            for(int kk = 0; kk < numframes; kk++)
                pspritegroup.frames[kk] = new mspriteframe_t();

	        pspritegroup.numframes = numframes;

	        ppframe = pspritegroup;

            pin.ofs += sizeof_dspritegroup_t;
	        poutintervals = new double[numframes];

	        pspritegroup.intervals = poutintervals;

	        for (i=0 ; i<numframes ; i++)
	        {
                poutintervals[i] = BitConverter.ToSingle(pin.buffer, pin.ofs); pin.ofs += sizeof(Single);
		        if (poutintervals[i] <= 0.0)
			        sys_linux.Sys_Error ("Mod_LoadSpriteGroup: interval<=0");
	        }

	        for (i=0 ; i<numframes ; i++)
	        {
		        Mod_LoadSpriteFrame (pin, ref pspritegroup.frames[i]);
	        }
        }

        /*
        =================
        Mod_LoadSpriteModel
        =================
        */
        static void Mod_LoadSpriteModel(model_t mod, byte[] buffer)
        {
	        int					i;
	        int					version;
	        dsprite_t			pin;
	        msprite_t			psprite;
	        int					numframes;
	        dspriteframetype_t	pframetype;
            bspfile.ByteBuffer  aux = new bspfile.ByteBuffer(buffer, 0);

	        pin = (dsprite_t)buffer;

	        version = pin.version;
	        if (version != SPRITE_VERSION)
		        sys_linux.Sys_Error (mod.name + " has wrong version number (" + version + " should be " + SPRITE_VERSION + ")");

	        numframes = pin.numframes;

            psprite = new msprite_t();
            psprite.frames = new mspriteframedesc_t[numframes];
            for(int kk = 0; kk < numframes; kk++)
                psprite.frames[kk] = new mspriteframedesc_t();

	        mod.cache = psprite;

	        psprite.type = pin.type;
	        psprite.maxwidth = pin.width;
	        psprite.maxheight = pin.height;
	        psprite.beamlength = pin.beamlength;
	        mod.synctype = pin.synctype;
	        psprite.numframes = numframes;

	        mod.mins[0] = mod.mins[1] = -psprite.maxwidth/2;
	        mod.maxs[0] = mod.maxs[1] = psprite.maxwidth/2;
	        mod.mins[2] = -psprite.maxheight/2;
	        mod.maxs[2] = psprite.maxheight/2;
        	
        //
        // load the frames
        //
	        if (numframes < 1)
		        sys_linux.Sys_Error ("Mod_LoadSpriteModel: Invalid # of frames: " + numframes + "\n");

	        mod.numframes = numframes;
	        mod.flags = 0;

            aux.ofs += sizeof_dsprite_t;

	        for (i=0 ; i<numframes ; i++)
	        {
		        spriteframetype_t	frametype;

                pframetype = (dspriteframetype_t)aux;
                frametype = pframetype.type;
		        psprite.frames[i].type = frametype;

                if (frametype == spriteframetype_t.SPR_SINGLE)
		        {
                    aux.ofs += sizeof_dspriteframetype_t;
                    Mod_LoadSpriteFrame(aux, ref psprite.frames[i].frameptr);
		        }
		        else
		        {
                    aux.ofs += sizeof_dspriteframetype_t;
    		        Mod_LoadSpriteGroup(aux, ref psprite.frames[i].frameptr);
		        }
	        }

	        mod.type = modtype_t.mod_sprite;
        }

        //=============================================================================

        /*
        ================
        Mod_Print
        ================
        */
        public static void Mod_Print ()
        {
	        int		i;
            int     cached;
	        model_t	mod;

	        console.Con_Printf ("Cached models:\n");
            cached = 0;
	        for (i=0 ; i < mod_numknown ; i++)
	        {
                mod = mod_known[i];
                if (mod.cache != null)
                    console.Con_Printf(String.Format("CAC4ED{0:00} : {1}", cached++, mod.name));
                else
                    console.Con_Printf("00000000 : " + mod.name);
                if ((mod.needload & NL_UNREFERENCED) != 0)
                    console.Con_Printf(" (!R)");
		        if ((mod.needload & NL_NEEDS_LOADED) != 0)
                    console.Con_Printf(" (!P)");
                console.Con_Printf("\n");
	        }
        }
    }
}