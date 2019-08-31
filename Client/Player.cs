using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Player
    {
        public int ID { get; set; }
        public int Index { get; set; }
        public string Tag { get; set; }

        public Player()
        {
            Index = 0;
            ID = generateID();
            Tag = "Player";
        }

        public PlayerInfo generateInfo()
        {
            PlayerInfo playerInfo = new PlayerInfo()
            {
                ID = this.ID,
                Tag = this.Tag
            };
            return playerInfo;
        }

        private int generateID()
        {
            Random r = new Random();
            int res = r.Next();
            while (res < 10)
            {
                res = r.Next();
            }
            return res;
        }
    }
}
