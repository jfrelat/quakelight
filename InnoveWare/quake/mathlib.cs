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
// mathlib.h
// mathlib.c -- math primitives

namespace quake
{
    public partial class mathlib
    {
        public const double M_PI		= 3.14159265358979323846;	// matches value in gcc v2 math.h

        public static double DotProduct(double[] x, double[] y)
        {
            return x[0]*y[0]+x[1]*y[1]+x[2]*y[2];
        }

        public static double DotProduct(byte[] x, double[] y)
        {
            return x[0] * y[0] + x[1] * y[1] + x[2] * y[2];
        }

        public static void VectorSubtract(double[] a, double[] b, ref double[] c)
        {
            c[0]=a[0]-b[0];c[1]=a[1]-b[1];c[2]=a[2]-b[2];
        }
        
        public static void VectorAdd(double[] a, double[] b, ref double[] c)
        {
            c[0]=a[0]+b[0];c[1]=a[1]+b[1];c[2]=a[2]+b[2];
        }

        public static void VectorCopy(double[] a, ref double[] b)
        {
            b[0]=a[0];b[1]=a[1];b[2]=a[2];
        }

        public static double[] vec3_origin = {0,0,0};

        /*-----------------------------------------------------------------*/

        /*-----------------------------------------------------------------*/

        public static double anglemod(double a)
        {
            a = (360.0 / 65536) * ((int)(a * (65536 / 360.0)) & 65535);
	        return a;
        }

        /*
        ==================
        BOPS_Error

        Split out like this for ASM to call.
        ==================
        */
        static void BOPS_Error ()
        {
	        sys_linux.Sys_Error ("BoxOnPlaneSide:  Bad signbits");
        }

        public static int BOX_ON_PLANE_SIDE(double[] emins, double[] emaxs, model.mplane_t p)
        {
            return
            ((p.type < 3) ?
            (
                (p.dist <= emins[p.type]) ?
                    1
                :
                (
                    (p.dist >= emaxs[p.type]) ?
                        2
                    :
                        3
                )
            )
            :
                BoxOnPlaneSide(emins, emaxs, p));
        }

        /*
        ==================
        BoxOnPlaneSide

        Returns 1, 2, or 1 + 2
        ==================
        */
        static int BoxOnPlaneSide (double[] emins, double[] emaxs, model.mplane_t p)
        {
	        double	dist1, dist2;
	        int		sides;

        /*	// this is done by the BOX_ON_PLANE_SIDE macro before calling this
		        // function
        // fast axial cases
	        if (p.type < 3)
	        {
		        if (p.dist <= emins[p.type])
			        return 1;
		        if (p.dist >= emaxs[p.type])
			        return 2;
		        return 3;
	        }
        */
        	
        // general case
	        switch (p.signbits)
	        {
	        case 0:
        dist1 = p.normal[0]*emaxs[0] + p.normal[1]*emaxs[1] + p.normal[2]*emaxs[2];
        dist2 = p.normal[0]*emins[0] + p.normal[1]*emins[1] + p.normal[2]*emins[2];
		        break;
	        case 1:
        dist1 = p.normal[0]*emins[0] + p.normal[1]*emaxs[1] + p.normal[2]*emaxs[2];
        dist2 = p.normal[0]*emaxs[0] + p.normal[1]*emins[1] + p.normal[2]*emins[2];
		        break;
	        case 2:
        dist1 = p.normal[0]*emaxs[0] + p.normal[1]*emins[1] + p.normal[2]*emaxs[2];
        dist2 = p.normal[0]*emins[0] + p.normal[1]*emaxs[1] + p.normal[2]*emins[2];
		        break;
	        case 3:
        dist1 = p.normal[0]*emins[0] + p.normal[1]*emins[1] + p.normal[2]*emaxs[2];
        dist2 = p.normal[0]*emaxs[0] + p.normal[1]*emaxs[1] + p.normal[2]*emins[2];
		        break;
	        case 4:
        dist1 = p.normal[0]*emaxs[0] + p.normal[1]*emaxs[1] + p.normal[2]*emins[2];
        dist2 = p.normal[0]*emins[0] + p.normal[1]*emins[1] + p.normal[2]*emaxs[2];
		        break;
	        case 5:
        dist1 = p.normal[0]*emins[0] + p.normal[1]*emaxs[1] + p.normal[2]*emins[2];
        dist2 = p.normal[0]*emaxs[0] + p.normal[1]*emins[1] + p.normal[2]*emaxs[2];
		        break;
	        case 6:
        dist1 = p.normal[0]*emaxs[0] + p.normal[1]*emins[1] + p.normal[2]*emins[2];
        dist2 = p.normal[0]*emins[0] + p.normal[1]*emaxs[1] + p.normal[2]*emaxs[2];
		        break;
	        case 7:
        dist1 = p.normal[0]*emins[0] + p.normal[1]*emins[1] + p.normal[2]*emins[2];
        dist2 = p.normal[0]*emaxs[0] + p.normal[1]*emaxs[1] + p.normal[2]*emaxs[2];
		        break;
	        default:
		        dist1 = dist2 = 0;		// shut up compiler
		        BOPS_Error ();
		        break;
	        }

	        sides = 0;
	        if (dist1 >= p.dist)
		        sides = 1;
	        if (dist2 < p.dist)
		        sides |= 2;

	        return sides;
        }

        public static void AngleVectors(double[] angles, ref double[] forward, ref double[] right, ref double[] up)
        {
            double angle;
            double sr, sp, sy, cr, cp, cy;

            angle = angles[quakedef.YAW] * (M_PI * 2 / 360);
            sy = Math.Sin(angle);
            cy = Math.Cos(angle);
            angle = angles[quakedef.PITCH] * (M_PI * 2 / 360);
            sp = Math.Sin(angle);
            cp = Math.Cos(angle);
            angle = angles[quakedef.ROLL] * (M_PI * 2 / 360);
            sr = Math.Sin(angle);
            cr = Math.Cos(angle);

            forward[0] = cp * cy;
            forward[1] = cp * sy;
            forward[2] = -sp;
            right[0] = (-1 * sr * sp * cy + -1 * cr * -sy);
            right[1] = (-1 * sr * sp * sy + -1 * cr * cy);
            right[2] = -1 * sr * cp;
            up[0] = (cr * sp * cy + -sr * -sy);
            up[1] = (cr * sp * sy + -sr * cy);
            up[2] = cr * cp;
        }

        int VectorCompare (double[] v1, double[] v2)
        {
	        int		i;
        	
	        for (i=0 ; i<3 ; i++)
		        if (v1[i] != v2[i])
			        return 0;
        			
	        return 1;
        }

        public static void VectorMA (double[] veca, double scale, double[] vecb, ref double[] vecc)
        {
	        vecc[0] = veca[0] + scale*vecb[0];
	        vecc[1] = veca[1] + scale*vecb[1];
	        vecc[2] = veca[2] + scale*vecb[2];
        }

        void CrossProduct (double[] v1, double[] v2, ref double[] cross)
        {
	        cross[0] = v1[1]*v2[2] - v1[2]*v2[1];
	        cross[1] = v1[2]*v2[0] - v1[0]*v2[2];
	        cross[2] = v1[0]*v2[1] - v1[1]*v2[0];
        }

        public static double Length(double[] v)
        {
	        int		i;
	        double	length;
        	
	        length = 0;
	        for (i=0 ; i< 3 ; i++)
		        length += v[i]*v[i];
            length = Math.Sqrt(length);		// FIXME

	        return length;
        }

        public static double VectorNormalize(ref double[] v)
        {
	        double	length, ilength;

	        length = v[0]*v[0] + v[1]*v[1] + v[2]*v[2];
            length = Math.Sqrt(length);		// FIXME

	        if (length != 0)
	        {
		        ilength = 1/length;
		        v[0] *= ilength;
		        v[1] *= ilength;
		        v[2] *= ilength;
	        }
        		
	        return length;

        }

        public static void VectorInverse (ref double[] v)
        {
	        v[0] = -v[0];
	        v[1] = -v[1];
	        v[2] = -v[2];
        }

        public static void VectorScale (double[] @in, double scale, ref double[] @out)
        {
	        @out[0] = @in[0]*scale;
	        @out[1] = @in[1]*scale;
	        @out[2] = @in[2]*scale;
        }

        /*
        ================
        R_ConcatRotations
        ================
        */
        public static void R_ConcatRotations (double[][] in1, double[][] in2, ref double[][] @out)
        {
	        @out[0][0] = in1[0][0] * in2[0][0] + in1[0][1] * in2[1][0] +
				        in1[0][2] * in2[2][0];
	        @out[0][1] = in1[0][0] * in2[0][1] + in1[0][1] * in2[1][1] +
				        in1[0][2] * in2[2][1];
	        @out[0][2] = in1[0][0] * in2[0][2] + in1[0][1] * in2[1][2] +
				        in1[0][2] * in2[2][2];
	        @out[1][0] = in1[1][0] * in2[0][0] + in1[1][1] * in2[1][0] +
				        in1[1][2] * in2[2][0];
	        @out[1][1] = in1[1][0] * in2[0][1] + in1[1][1] * in2[1][1] +
				        in1[1][2] * in2[2][1];
	        @out[1][2] = in1[1][0] * in2[0][2] + in1[1][1] * in2[1][2] +
				        in1[1][2] * in2[2][2];
	        @out[2][0] = in1[2][0] * in2[0][0] + in1[2][1] * in2[1][0] +
				        in1[2][2] * in2[2][0];
	        @out[2][1] = in1[2][0] * in2[0][1] + in1[2][1] * in2[1][1] +
				        in1[2][2] * in2[2][1];
            @out[2][2] = in1[2][0] * in2[0][2] + in1[2][1] * in2[1][2] +
				        in1[2][2] * in2[2][2];
        }

        /*
        ================
        R_ConcatTransforms
        ================
        */
        public static void R_ConcatTransforms (double[][] in1, double[][] in2, ref double[][] @out)
        {
	        @out[0][0] = in1[0][0] * in2[0][0] + in1[0][1] * in2[1][0] +
				        in1[0][2] * in2[2][0];
	        @out[0][1] = in1[0][0] * in2[0][1] + in1[0][1] * in2[1][1] +
				        in1[0][2] * in2[2][1];
	        @out[0][2] = in1[0][0] * in2[0][2] + in1[0][1] * in2[1][2] +
				        in1[0][2] * in2[2][2];
	        @out[0][3] = in1[0][0] * in2[0][3] + in1[0][1] * in2[1][3] +
				        in1[0][2] * in2[2][3] + in1[0][3];
	        @out[1][0] = in1[1][0] * in2[0][0] + in1[1][1] * in2[1][0] +
				        in1[1][2] * in2[2][0];
	        @out[1][1] = in1[1][0] * in2[0][1] + in1[1][1] * in2[1][1] +
				        in1[1][2] * in2[2][1];
	        @out[1][2] = in1[1][0] * in2[0][2] + in1[1][1] * in2[1][2] +
				        in1[1][2] * in2[2][2];
	        @out[1][3] = in1[1][0] * in2[0][3] + in1[1][1] * in2[1][3] +
				        in1[1][2] * in2[2][3] + in1[1][3];
	        @out[2][0] = in1[2][0] * in2[0][0] + in1[2][1] * in2[1][0] +
				        in1[2][2] * in2[2][0];
	        @out[2][1] = in1[2][0] * in2[0][1] + in1[2][1] * in2[1][1] +
				        in1[2][2] * in2[2][1];
	        @out[2][2] = in1[2][0] * in2[0][2] + in1[2][1] * in2[1][2] +
				        in1[2][2] * in2[2][2];
	        @out[2][3] = in1[2][0] * in2[0][3] + in1[2][1] * in2[1][3] +
				        in1[2][2] * in2[2][3] + in1[2][3];
        }

        /*
        ===================
        FloorDivMod

        Returns mathematically correct (floor-based) quotient and remainder for
        numer and denom, both of which should contain no fractional part. The
        quotient must fit in 32 bits.
        ====================
        */

        public static void FloorDivMod (double numer, double denom, ref int quotient, ref int rem)
        {
	        int		q, r;
	        double	x;

	        if (denom <= 0.0)
		        sys_linux.Sys_Error ("FloorDivMod: bad denominator " + denom + "\n");

        //	if ((floor(numer) != numer) || (floor(denom) != denom))
        //		Sys_Error ("FloorDivMod: non-integer numer or denom %f %f\n",
        //				numer, denom);

	        if (numer >= 0.0)
	        {

		        x = Math.Floor(numer / denom);
		        q = (int)x;
		        r = (int)Math.Floor(numer - (x * denom));
	        }
	        else
	        {
	        //
	        // perform operations with positive values, and fix mod to make floor-based
	        //
		        x = Math.Floor(-numer / denom);
		        q = -(int)x;
                r = (int)Math.Floor(-numer - (x * denom));
		        if (r != 0)
		        {
			        q--;
			        r = (int)denom - r;
		        }
	        }

	        quotient = q;
	        rem = r;
        }

        /*
        ===================
        GreatestCommonDivisor
        ====================
        */
        public static int GreatestCommonDivisor(int i1, int i2)
        {
            if (i1 > i2)
            {
                if (i2 == 0)
                    return (i1);
                return GreatestCommonDivisor(i2, i1 % i2);
            }
            else
            {
                if (i1 == 0)
                    return (i2);
                return GreatestCommonDivisor(i1, i2 % i1);
            }
        }
    }
}