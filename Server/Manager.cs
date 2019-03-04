using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Server
{
    class Manager
    {
        // redis
        private ConnectionMultiplexer redis;
        private IDatabase db;
        private ISubscriber sub;
        // sync lock
        private Mutex mutex;

        // main window
        public MainWindow window { get; set; }

        // server
        private int count;
        private int capacity;
        private List<Game> games;

        // singleton instance
        private static Manager instance = null;
        // singleton get
        public static Manager getInstance()
        {
            if (instance == null)
                instance = new Manager();
            return instance;
        }

        private Manager()
        {
            // init redis
            initRedis();
            // data
            count = 0;
            capacity = 1;
            mutex = new Mutex();
        }

        private async void initRedis()
        {
            // read file for password
            String s = "";
            using (StreamReader sr = new StreamReader(@"../../../redpass.txt"))
            {
                s = sr.ReadLine();
            }
            // init redis
            redis = await ConnectionMultiplexer.ConnectAsync(s);
            db = redis.GetDatabase();
            sub = redis.GetSubscriber();
            window.updateText("redis: connected");
            // create games with subscribers
            initGames();
        }

        private void initGames()
        {
            // create empty games
            games = new List<Game>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                games.Add(new Game(this, i, redis.GetSubscriber()));
            }
        }

        public async void startServer()
        {
            window.updateText("Server is live.");
            // listen for new players
            await sub.SubscribeAsync("find", (channel, msg) =>
            {
                handlePlayer(msg);
            });
        }

        private void handlePlayer(String id)
        {
            mutex.WaitOne();

            // server is full
            if (count == capacity)
            {
                sub.PublishAsync("game", "full");
                window.updateText("FULL : " + id);
            }
            // save new player
            else
            {
                // find game
                Game game = null;
                for (int i = 0; i < games.Count; i++)
                {
                    if (!games[i].Full)
                    {
                        game = games[i];
                        break;
                    }
                }
                // add player to a game
                if (game != null)
                {
                    window.updateText("PLAYER : " + id);
                    game.addPlayer(id);
                    // increase count if the game has started
                    if (game.Full)
                    {
                        count++;
                    }
                }
            }

            mutex.ReleaseMutex();
        }

        public void gameFinished()
        {
            mutex.WaitOne();

            count--;

            mutex.ReleaseMutex();
        }
    }
}
