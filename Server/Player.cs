using System;

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
        public bool online;

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
            online = true;
        }

        public void reset()
        {
            ready = false;
            width = 60;
            shield = 1;
            blocks = 0;
            online = true;
        }

        public void blockHit(int points)
        {
            if (shield == 3) return;
            blocks += points;
            if (blocks == 10)
            {
                blocks = 0;
                shield++;
            }
        }

        public void shieldHit()
        {
            shield--;
        }
    }
}
