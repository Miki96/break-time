using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Arena
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double BlocksHeight { get; set; }
        public double PlayerOffset { get; set; }
        public int BlocksN { get; set; }
        public int BlocksM { get; set; }

        public Arena()
        {
            Width = 400;
            Height = 600;
            PlayerOffset = 30;
            BlocksHeight = 200;
            BlocksN = 11;
            BlocksM = 11;
        }

        public void correctCoordinates(ref double x, ref double y, int player)
        {
            if (player == 1)
            {
                // invert coordinates
                x = Width - x;
                y = Height - y;
            }
        }
    }
}
