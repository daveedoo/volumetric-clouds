using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clouds
{
    public class PermutationTable
    {
        public int Size { get; set; }

        public int Seed { get; set; }

        public int Max {  get; set; }
        public float Inverse { get; set; }

        private int Wrap;

        private int[] Table;

        public PermutationTable(int size, int max, int seed)
        {
            Size = size;
            Wrap = Size - 1;
            Max = Math.Max(1, max);
            Inverse = 1.0f / Max;
            Build(seed);
        }

        public void Build(int seed)
        {
            if (Seed == seed && Table != null) return;

            Seed = seed;
            Table = new int[Size];  

            System.Random rnd = new System.Random(Seed);

            for(int i=0; i<Size; i++)
            {
                Table[i] = rnd.Next();
            }
        }
        

        public int this[int i]
        {
            get
            {
                return Table[i & Wrap] & Max;
            }
        }

        public int this[int i, int j]
        {
            get
            {
                return Table[(j + Table[i & Wrap]) & Wrap] & Wrap;
            }
        }
    }
}
