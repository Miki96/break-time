using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LineSegmentIntersection;

namespace Server
{
    class Geometry
    {
        public static void playerColision(Ball ball, Player player, Arena arena)
        {
            Coords dir = new Coords(0, 0);

            // one
            double w = player.width;
            double x = player.pos.X;

            if ((
                    player.index == 0 &&
                    ball.pos.Y + ball.size < (arena.Height - (arena.PlayerOffset - (ball.speed + 1))) &&
                    ball.pos.Y + ball.size > (arena.Height - arena.PlayerOffset)
                ||
                    player.index == 1 &&
                    ball.pos.Y - ball.size > (arena.PlayerOffset - (ball.speed + 1))
                    && ball.pos.Y - ball.size < arena.PlayerOffset
                )
                &&
                (ball.pos.X > (x - w / 2) && ball.pos.X < (x + w / 2)))
            {
                // calculate position of hit
                double p = (ball.pos.X - (x - w / 2)) / w;
                double minAngle = 30;
                double angle = (Math.PI / 180.0) * (p * (180 - 2 * minAngle) + minAngle);

                int mul = (player.index == 0 ? -1 : 1);
                dir.Y = Math.Sin(angle) * mul;
                dir.X = -Math.Cos(angle);

                // apply changes
                ball.dir = dir;
                ball.player = player.index;
            }
        }

        public static void blocksCollision(Ball ball, Player player, Arena arena)
        {
            // blocks area

            int blocksN = arena.BlocksN;
            int blocksM = arena.BlocksM;
            double freeSpace = (arena.Height - arena.BlocksHeight) / 2;


            double h = arena.BlocksHeight * 1.0 / blocksN;
            double w = arena.Width * 1.0 / blocksM;

            int row = Convert.ToInt32((ball.pos.Y - freeSpace) / h);
            int col = Convert.ToInt32(ball.pos.X / w);

            // find 4 closest blocks

            List<Position> targets = new List<Position>();
            int startI, startJ, endI, endJ;

            // set I
            if (row == 0)
            {
                startI = 0;
                endI = 0;
            }
            else if (row == blocksN)
            {
                startI = blocksN - 1;
                endI = blocksN - 1;
            }
            else
            {
                startI = row - 1;
                endI = row;
            }
            // set J
            if (col == 0)
            {
                startJ = 0;
                endJ = 0;
            }
            else if (col == blocksM)
            {
                startJ = blocksM - 1;
                endJ = blocksM - 1;
            }
            else
            {
                startJ = col - 1;
                endJ = col;
            }

            // add potental targets
            for (int ii = startI; ii <= endI; ii++)
            {
                for (int jj = startJ; jj <= endJ; jj++)
                {
                    // check if block
                    if (arena.Blocks[ii, jj] != 0)
                    {
                        targets.Add(new Position(ii, jj));
                    }
                }
            }

            // find hits
            List<List<Vector>> edges = new List<List<Vector>>();
            List<int> side = new List<int>();

            // advanced intersections
            foreach (Position pos in targets)
            {
                double left = pos.J * w;
                double top = pos.I * h;
                double right = (pos.J + 1) * w;
                double bottom = (pos.I + 1) * h;
                // top
                edges.Add(new List<Vector>() { new Vector(left, top), new Vector(right, top) });
                side.Add(0);
                // right
                edges.Add(new List<Vector>() { new Vector(right, top), new Vector(right, bottom) });
                side.Add(1);
                // bottom
                edges.Add(new List<Vector>() { new Vector(left, bottom), new Vector(right, bottom) });
                side.Add(2);
                // left
                edges.Add(new List<Vector>() { new Vector(left, top), new Vector(left, bottom) });
                side.Add(3);
            }

            // find nearrest intersection
            int ni = -1;
            double dist = int.MaxValue;
            Vector start = new Vector(ball.pos.X - (ball.dir.X * ball.speed), (ball.pos.Y - (ball.dir.Y * ball.speed)) - freeSpace);
            Vector end = new Vector(ball.pos.X, (ball.pos.Y) - freeSpace);
            Vector minIntersetion = new Vector(0, 0);
            for (int k = 0; k < edges.Count; k++)
            {
                Vector inter;
                bool crossing = LineSegment.LineSegementsIntersect(
                    edges[k][0],
                    edges[k][1],
                    start,
                    end,
                    out inter
                );
                // check if lesser then minimum
                if (crossing)
                {
                    double currentDist = Math.Sqrt((inter.X - start.X) * (inter.X - start.X) + (inter.Y - start.Y) * (inter.Y - start.Y));
                    if (dist > currentDist && !currentDist.IsZero())
                    {
                        dist = currentDist;
                        ni = k;
                        minIntersetion = inter;
                    }
                }
            }

            // change direction
            if (ni != -1)
            {
                // move ball where it should have stopped
                //ball.X = minIntersetion.X;
                //ball.Y = minIntersetion.Y + freeSpace;

                // decrease block if destroyable
                // arena hit
                int p = arena.blockHit(targets[ni / 4].I, targets[ni / 4].J);
                //  increase player blocks
                player.blockHit(p);

                // change dir
                switch (side[ni])
                {
                    case 0:
                        ball.dir.Y = -Math.Abs(ball.dir.Y);
                        break;
                    case 1:
                        ball.dir.X = Math.Abs(ball.dir.X);
                        break;
                    case 2:
                        ball.dir.Y = Math.Abs(ball.dir.Y);
                        break;
                    case 3:
                        ball.dir.X = -Math.Abs(ball.dir.X);
                        break;
                    default:
                        break;
                }
            }
        }

        public static void sideWallCollision(Ball ball, Arena arena)
        {
            if (ball.pos.X - ball.size < 0)
            {
                ball.dir.X = Math.Abs(ball.dir.X);
            }
            else if (ball.pos.X + ball.size > arena.Width)
            {
                ball.dir.X = -Math.Abs(ball.dir.X);
            }
        }

        public static void shieldCollision(Ball ball, List<Player> players, Arena arena)
        {
            if (ball.pos.Y - ball.size < players[1].shield * 5)
            {
                // lose shield
                players[1].shield--;
                ball.dir.Y *= -1;
            }
            else if (ball.pos.Y + ball.size > arena.Height - (players[0].shield * 5))
            {
                // lose shield
                players[0].shield--;
                ball.dir.Y *= -1;
            }
        }

    }
}
