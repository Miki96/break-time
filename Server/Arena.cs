using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Arena
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double BlocksHeight { get; set; }
        public double PlayerOffset { get; set; }
        public int BlocksN { get; set; }
        public int BlocksM { get; set; }
        public int[,] Blocks { get; set; }

        public Arena()
        {
            Width = 400;
            Height = 600;
            PlayerOffset = 35;
            BlocksHeight = 200;
            BlocksN = 11;
            BlocksM = 11;
            loadBlocks(0);
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

        // create blocks
        public void loadBlocks(int level)
        {
            int n = BlocksN;
            int m = BlocksM;
            int[,] res;

            switch (level)
            {
                case 0:
                    res = new int[,]
                    {
                        { 3, 2, 1, 0, 0, 3, 0, 0, 1, 2, 3 },
                        { 4, 1, 0, 0, 1, 4, 1, 0, 0, 1, 4 },
                        { 1, 0, 0, 1, 2, 0, 2, 1, 0, 0, 1 },
                        { 0, 0, 1, 2, 0, 0, 0, 2, 1, 0, 0 },
                        { 0, 1, 2, 0, 1, 2, 1, 0, 2, 1, 0 },
                        { 2, 0, 3, 0, 2, 4, 2, 0, 3, 0, 2 },
                        { 0, 1, 2, 0, 1, 2, 1, 0, 2, 1, 0 },
                        { 0, 0, 1, 2, 0, 0, 0, 2, 1, 0, 0 },
                        { 1, 0, 0, 1, 2, 0, 2, 1, 0, 0, 1 },
                        { 4, 1, 0, 0, 1, 4, 1, 0, 0, 1, 4 },
                        { 3, 2, 1, 0, 0, 3, 0, 0, 1, 2, 3 },
                    };
                    break;
                default:
                    res = new int[n, m];
                    Random r = new Random();
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < m; j++)
                        {
                            //res[i, j] = (i % 2) + (j % 2);
                            res[i, j] = r.Next(0, 3);

                        }
                    }
                    break;
            }

            Blocks = res;
        }

        public int blockHit(int i, int j)
        {
            if (Blocks[i, j] < 3)
            {
                Blocks[i, j]--;
                return 1;
            }
            else if (Blocks[i, j] == 4)
            {
                Blocks[i, j] = 0;
                return 2;
            }
            return 0;
        }
    }
}
