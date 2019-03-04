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
        private String tag;

        public Player(String tag, int id)
        {
            this.id = id;
            pos = new Coords(0, 150);
        }

    }
}
