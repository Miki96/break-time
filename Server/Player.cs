using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Player
    {
        public Coords pos;
        private int id;
        public String tag;
        public int index;
        public int score;
        public bool ready;
        public int width;
        public int blocks;
        public int shield;

        public Player(String tag, int id, int index, Coords pos)
        {
            this.tag = tag;
            this.id = id;
            this.index = index;
            this.pos = pos;
            score = 0;
            ready = false;
            width = 60;
            blocks = 0;
            shield = 1;
        }

        public void reset()
        {
            ready = false;
            width = 60;
            shield = 1;
            blocks = 0;
        }

        public void blockHit(int points)
        {
            blocks += points;
            if (blocks == 10)
            {
                blocks = 0;
                shield++;
            }
        }
    }
}
