using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Clouds
{
    public class PerlinNoise    //Made only for 2D generation
    {
        private PermutationTable Perm { get; set; }

        public float Frequency { get; set; }
        public float Amplitude { get; set; }

        public Vector2 Offset { get; set; } 

        public PerlinNoise(int seed, float frequency, float amp=1.0f) 
        {
            Frequency = frequency;
            Amplitude = amp;
            Offset = Vector2.Zero;
            Perm = new PermutationTable(1024, 255, seed);
        }

        public void UpdateSeed(int seed)
        {
            Perm.Build(seed);
        }

        public float Sample2D(float x, float y)
        {
            x = (x + Offset.X) * Frequency;
            y = (y + Offset.Y) * Frequency;

            int ix0, iy0;
            float fx0, fy0, fx1, fy1, s, t, nx0, nx1, n0, n1;

            // Integer parts of x,y
            ix0 = (int)MathF.Floor(x);
            iy0 = (int)MathF.Floor(y);

            // Fractional parts of x,y
            fx0 = x - ix0;
            fy0 = y - iy0;

            fx1 = fx0 - 1.0f;
            fy1 = fy0 - 1.0f;

            t = Fade(fy0);
            s = Fade(fx0);

            nx0 = Grad(Perm[ix0,iy0],fx0,fy0);
            nx1 = Grad(Perm[ix0, iy0 + 1], fx0, fy1);

            n0 = Lerp(t,nx0, nx1);

            nx0 = Grad(Perm[ix0 + 1, iy0], fx1, fy0);
            nx1 = Grad(Perm[ix0+1,iy0+1],fx1, fy1);

            n1 = Lerp(t,nx1, nx0);

            return 0.66666f*Lerp(s,n0,n1)*Amplitude;
        }

        private float Fade(float t) { return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f); }
        private float Lerp(float t, float a, float b) { return a + t * (b - a); }

        private float Grad(int hash, float x, float y)
        {
            int h = hash & 7;   //Converting low 3 bits of hash code
            float u = h < 4 ? x : y;    // into 8 simple gradient directions
            float v = h < 4 ? y : x;
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -2.0f * v : 2.0f*v);
        }
    }
}
