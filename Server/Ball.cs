using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Ball
    {
        public Coords pos;
        public Coords dir;
        public int player;
        public int size;
        public double speed;

        public Ball(Coords pos, int player, Coords dir, int size, double speed)
        {
            this.pos = pos;
            this.player = player;
            this.dir = dir;
            this.size = size;
            this.speed = speed;
        }
    }
}
