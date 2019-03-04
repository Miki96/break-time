using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using StackExchange.Redis;

namespace Server
{
    public struct Coords
    {
        public int x, y;

        public Coords(int p1, int p2)
        {
            x = p1;
            y = p2;
        }
    }

    class Game
    {
        // game manager
        private Manager manager;
        private ISubscriber sub;

        // data
        private int gameID;
        private List<Player> players;

        // arena
        private int width;
        private int height;
        private Coords ball;
        private Coords speed;
        private int size;

        // timer
        private Timer timer;


        // check if game is full
        public bool Full { get; private set; }

        public Game(Manager manager, int gameID, ISubscriber sub)
        {
            this.manager = manager;
            this.gameID = gameID;
            this.sub = sub;
            // arena
            width = 300;
            height = 400;
            ball = new Coords(100, 100);
            speed = new Coords(5, 5);
            size = 10;
            // players
            Full = false;
            players = new List<Player>();
        }

        private void startGame()
        {
            // notify server
            manager.window.updateText("Game started.");
            // notify players
            sub.PublishAsync("game", "start ");
            // start gameplay loop
            timer = new Timer(15);
            timer.Elapsed += moveBall;
            timer.Start();
        }

        private async void moveBall(Object o, EventArgs e)
        {
            // change position
            if (ball.x < 0 || ball.x + size > width) speed.x *= -1;
            if (ball.y < 0 || ball.y + size > height) speed.y *= -1;
            ball.x += speed.x;
            ball.y += speed.y;
            // notify players
            // manager.window.updateText("X : " + ball.x + "Y : " + ball.y);
            await sub.PublishAsync("moves", ball.x + " " + ball.y);
        }

        public void addPlayer(String tag)
        {
            // add new player
            int id = players.Count;
            players.Add(new Player(tag, id));
            // notify new player
            sub.PublishAsync("game", "found " + gameID + " " + id);
            // check if game should start
            if (players.Count == 2)
            {
                Full = true;
                // start game
                startGame();
            }
        }
    }
}
