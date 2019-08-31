using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Coords
    {
        public double X {get; set;}
        public double Y { get; set; }

        public Coords(double p1, double p2)
        {
            X = p1;
            Y = p2;
        }
    }

    public class Position
    {
        public int I { get; set; }
        public int J { get; set; }

        public Position(int i, int j)
        {
            I = i;
            J = j;
        }
    }
}
