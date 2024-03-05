/* 
 * NOTE: I've removed the ability to limit the size of the repetitions from this solution as I didn't think we'd be using the functionality
 * below is linked the main example I used in the case we want to add it back in:
 * https://adrianb.io/2014/08/09/perlinnoise.html
 * https://gist.github.com/Flafla2/1a0b9ebef678bbce3215#file-perlin-cs-L46
 * also a link to the original java implementation by Ken Perlin in his paper
 * https://mrl.cs.nyu.edu/~perlin/noise/
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
namespace Nebula
{

    public class PerlinNoise
    {
        private int[] permutation = { 151,160,137,91,90,15,					
		131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
		190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

        private static readonly float[] Gradients2D =
   {
         0.130526192220052f,  0.99144486137381f,   0.38268343236509f,   0.923879532511287f,  0.608761429008721f,  0.793353340291235f,  0.793353340291235f,  0.608761429008721f,
         0.923879532511287f,  0.38268343236509f,   0.99144486137381f,   0.130526192220051f,  0.99144486137381f,  -0.130526192220051f,  0.923879532511287f, -0.38268343236509f,
         0.793353340291235f, -0.60876142900872f,   0.608761429008721f, -0.793353340291235f,  0.38268343236509f,  -0.923879532511287f,  0.130526192220052f, -0.99144486137381f,
        -0.130526192220052f, -0.99144486137381f,  -0.38268343236509f,  -0.923879532511287f, -0.608761429008721f, -0.793353340291235f, -0.793353340291235f, -0.608761429008721f,
        -0.923879532511287f, -0.38268343236509f,  -0.99144486137381f,  -0.130526192220052f, -0.99144486137381f,   0.130526192220051f, -0.923879532511287f,  0.38268343236509f,
        -0.793353340291235f,  0.608761429008721f, -0.608761429008721f,  0.793353340291235f, -0.38268343236509f,   0.923879532511287f, -0.130526192220052f,  0.99144486137381f,
         0.130526192220052f,  0.99144486137381f,   0.38268343236509f,   0.923879532511287f,  0.608761429008721f,  0.793353340291235f,  0.793353340291235f,  0.608761429008721f,
         0.923879532511287f,  0.38268343236509f,   0.99144486137381f,   0.130526192220051f,  0.99144486137381f,  -0.130526192220051f,  0.923879532511287f, -0.38268343236509f,
         0.793353340291235f, -0.60876142900872f,   0.608761429008721f, -0.793353340291235f,  0.38268343236509f,  -0.923879532511287f,  0.130526192220052f, -0.99144486137381f,
        -0.130526192220052f, -0.99144486137381f,  -0.38268343236509f,  -0.923879532511287f, -0.608761429008721f, -0.793353340291235f, -0.793353340291235f, -0.608761429008721f,
        -0.923879532511287f, -0.38268343236509f,  -0.99144486137381f,  -0.130526192220052f, -0.99144486137381f,   0.130526192220051f, -0.923879532511287f,  0.38268343236509f,
        -0.793353340291235f,  0.608761429008721f, -0.608761429008721f,  0.793353340291235f, -0.38268343236509f,   0.923879532511287f, -0.130526192220052f,  0.99144486137381f,
         0.130526192220052f,  0.99144486137381f,   0.38268343236509f,   0.923879532511287f,  0.608761429008721f,  0.793353340291235f,  0.793353340291235f,  0.608761429008721f,
         0.923879532511287f,  0.38268343236509f,   0.99144486137381f,   0.130526192220051f,  0.99144486137381f,  -0.130526192220051f,  0.923879532511287f, -0.38268343236509f,
         0.793353340291235f, -0.60876142900872f,   0.608761429008721f, -0.793353340291235f,  0.38268343236509f,  -0.923879532511287f,  0.130526192220052f, -0.99144486137381f,
        -0.130526192220052f, -0.99144486137381f,  -0.38268343236509f,  -0.923879532511287f, -0.608761429008721f, -0.793353340291235f, -0.793353340291235f, -0.608761429008721f,
        -0.923879532511287f, -0.38268343236509f,  -0.99144486137381f,  -0.130526192220052f, -0.99144486137381f,   0.130526192220051f, -0.923879532511287f,  0.38268343236509f,
        -0.793353340291235f,  0.608761429008721f, -0.608761429008721f,  0.793353340291235f, -0.38268343236509f,   0.923879532511287f, -0.130526192220052f,  0.99144486137381f,
         0.130526192220052f,  0.99144486137381f,   0.38268343236509f,   0.923879532511287f,  0.608761429008721f,  0.793353340291235f,  0.793353340291235f,  0.608761429008721f,
         0.923879532511287f,  0.38268343236509f,   0.99144486137381f,   0.130526192220051f,  0.99144486137381f,  -0.130526192220051f,  0.923879532511287f, -0.38268343236509f,
         0.793353340291235f, -0.60876142900872f,   0.608761429008721f, -0.793353340291235f,  0.38268343236509f,  -0.923879532511287f,  0.130526192220052f, -0.99144486137381f,
        -0.130526192220052f, -0.99144486137381f,  -0.38268343236509f,  -0.923879532511287f, -0.608761429008721f, -0.793353340291235f, -0.793353340291235f, -0.608761429008721f,
        -0.923879532511287f, -0.38268343236509f,  -0.99144486137381f,  -0.130526192220052f, -0.99144486137381f,   0.130526192220051f, -0.923879532511287f,  0.38268343236509f,
        -0.793353340291235f,  0.608761429008721f, -0.608761429008721f,  0.793353340291235f, -0.38268343236509f,   0.923879532511287f, -0.130526192220052f,  0.99144486137381f,
         0.130526192220052f,  0.99144486137381f,   0.38268343236509f,   0.923879532511287f,  0.608761429008721f,  0.793353340291235f,  0.793353340291235f,  0.608761429008721f,
         0.923879532511287f,  0.38268343236509f,   0.99144486137381f,   0.130526192220051f,  0.99144486137381f,  -0.130526192220051f,  0.923879532511287f, -0.38268343236509f,
         0.793353340291235f, -0.60876142900872f,   0.608761429008721f, -0.793353340291235f,  0.38268343236509f,  -0.923879532511287f,  0.130526192220052f, -0.99144486137381f,
        -0.130526192220052f, -0.99144486137381f,  -0.38268343236509f,  -0.923879532511287f, -0.608761429008721f, -0.793353340291235f, -0.793353340291235f, -0.608761429008721f,
        -0.923879532511287f, -0.38268343236509f,  -0.99144486137381f,  -0.130526192220052f, -0.99144486137381f,   0.130526192220051f, -0.923879532511287f,  0.38268343236509f,
        -0.793353340291235f,  0.608761429008721f, -0.608761429008721f,  0.793353340291235f, -0.38268343236509f,   0.923879532511287f, -0.130526192220052f,  0.99144486137381f,
         0.38268343236509f,   0.923879532511287f,  0.923879532511287f,  0.38268343236509f,   0.923879532511287f, -0.38268343236509f,   0.38268343236509f,  -0.923879532511287f,
        -0.38268343236509f,  -0.923879532511287f, -0.923879532511287f, -0.38268343236509f,  -0.923879532511287f,  0.38268343236509f,  -0.38268343236509f,   0.923879532511287f,
    };

        private int[] p;
        private int repeat;

        public PerlinNoise(string seed = null)
        {
            // generate permutation using seed string
            if (!string.IsNullOrEmpty(seed))
            {
                var s = seed.GetHashCode();
                System.Random random = new System.Random(s);
                var len = permutation.Length;
                while (len > 1)
                {
                    int k = random.Next(len--);
                    int tmp = permutation[len];
                    permutation[len] = permutation[k];
                    permutation[k] = tmp;
                }
            }

            p = new int[512];
            for (int i = 0; i < p.Length; i++)
            {
                p[i] = permutation[i % 256];
            }
        }
        /// <summary>
        /// Get value for a given position inside the noise pattern
        /// </summary>
        /// <param name="x"> x value of our position</param>
        /// <param name="y"> y value of our position</param>
        /// <returns></returns>
        public double Noise(double x, double y)
        {
            // get which cell in the noise pattern we're getting our value from
            int xIndex = (int)x & 255;
            int yIndex = (int)y & 255;
            // get the relative position inside that cell we're sampling
            double xDecimal = x - (int)x;
            double yDecimal = y - (int)y;

            double u = Fade(xDecimal);
            double v = Fade(yDecimal);

            // derive a hash for each cell corner from our permutations table
            int w1 = p[xIndex + p[yIndex]];
            int w2 = p[xIndex + 1 + p[yIndex]];
            int w3 = p[xIndex + p[yIndex + 1]];
            int w4 = p[xIndex + 1 + p[yIndex + 1]];

            // create gradient from the corners of our cell using our relative coordinates, then lerp to smooth out the result using u and v
            double noise = Lerp(
                Lerp(Gradient(w1, xDecimal, yDecimal), Gradient(w2, xDecimal - 1, yDecimal), u),
                Lerp(Gradient(w4, xDecimal, yDecimal - 1), Gradient(w3, xDecimal - 1, yDecimal - 1), u), 
                v );
            // bind results to a 0 - 1 range from its original -1 - 1 range
            return (noise + 1)/2;
        }

        const int PrimeX = 501125321;
        const int PrimeY = 1136930381;

        public float Noise(int seed, float x, float y)
        {
            int x0 = (int)MathF.Floor(x);
            int y0 = (int)MathF.Floor(y);

            float xd0 = (float)(x - 0);
            float yd0 = (float)(y - 0);

            float xd1 = xd0 - 1;
            float yd1 = yd0 - 1;

            float xs = Fade(xd0);
            float ys = Fade(yd0);

            x0 *= PrimeX;
            y0 *= PrimeY;
            int x1 = x0 + PrimeX;
            int y1 = y0 + PrimeY;

            float xf0 = Lerp(Gradient(seed, x0, y0, xd0, yd0),Gradient(seed, x1, y1, xd1, yd1), xs);
            float xf1 = Lerp(Gradient(seed, x0, y1, xd0, yd1),Gradient(seed, x1,y1,xd1,yd1),xs);

            return Lerp(xf0,xf1,ys) * 1.4247691104677813f;
        }

        /// <summary>
        /// Get value for a given position inside the noise pattern, combining a number of octaves
        /// </summary>
        /// <param name="x"> x value of our position</param>
        /// <param name="y"> y value of our position</param>
        /// <param name="octaves"> number of runs to perform</param>
        /// <param name="persistence"> how much each subsiquent run affects the result</param>
        /// <returns></returns>
        public double OctaveNoise(double x, double y, int octaves, double persistence)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;
            double maxValue = 0;
            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency, y * frequency) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }
        private double Gradient(int hash, double x, double y)
        {
            // get the first 3 bits of the hash
            int h = hash & 7; 
            // if left most bit is 1, set our values to x,y otherwise set to y,x
            double u = h < 4 ? x : y;
            double v = h < 4 ? y : x;

            // use the last two bits to determine whether either u or v should be positive or negative
            return ((h&1) == 0 ? u : -u) + ((h&2) == 0 ? v : -v);
        }

        private float Gradient(int seed, int x, int y, float xd, float yd)
        {
            int hash = Hash(seed, x, y);
            hash ^= hash >> 15;
            hash &= 127 << 1;

            float xg = Gradients2D[hash];
            float yg = Gradients2D[hash | 1];

            return xd * xg * yd * yg;
        }

        private int Hash(int seed, int xP, int yP)
        {
            int hash = seed ^ xP ^ yP;
            hash *= 0x27d4eb2d;
            return hash;
        }

        // ease value towards whole numbers
        private double Fade(double t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private float Fade(float t)
        {
            return t * t
                * t * (t * (t * 6 - 15) + 10);
        }

        // linear interpolation
        private double Lerp(double a, double b, double x)
        {
            return a + x * (b - a);
        }

        private float Lerp(float a, float b, float x)
        {
            return a + x * (b - a);
        }
    }
}
