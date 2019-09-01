using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Newtonsoft.Json;

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
        private bool live;

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
            // data
            count = 0;
            capacity = 2;
            mutex = new Mutex();
            live = false;
        }

        // connect to redis database
        private async Task initRedis()
        {
            // conect to server
            String s = window.inputServer.Text;
            // init redis
            redis = await ConnectionMultiplexer.ConnectAsync(s);
            db = redis.GetDatabase();
            sub = redis.GetSubscriber();
            window.updateText("redis: connected");
            // create games with subscribers
            initGames();
        }

        // create empty games connected to redis
        private void initGames()
        {
            // create empty games
            games = new List<Game>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                games.Add(new Game(this, i, redis.GetSubscriber()));
            }
        }

        // subscribe to channel for players
        public async void startServer()
        {
            if (!live)
            {
                // connect to redis
                await initRedis();

                live = true;
                // listen for new players
                await sub.SubscribeAsync("find", (channel, msg) =>
                {
                    handlePlayer(msg);
                });
                window.updateText("Server is live.");
            } else
            {
                window.updateText("Server is already live.");
            }
        }

        // check for available games
        private void handlePlayer(String msg)
        {
            // lock
            mutex.WaitOne();

            // read message
            PlayerInfo player = JsonConvert.DeserializeObject<PlayerInfo>(msg);

            if (count == capacity)
            {
                // server is full, notify player
                fullServerResponse(player.ID.ToString());
                // log player info
                window.updateText("FULL : " + player.ID);
            }
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
                window.updateText("PLAYER : " + player.ID + " " + player.Tag);
                game.addPlayer(player);
                // increase count if the game has started
                if (game.Full)
                {
                    count++;
                }
            }

            // unlock
            mutex.ReleaseMutex();
        }

        // server full response
        private void fullServerResponse(string player)
        {
            GameResponse response = new GameResponse()
            {
                Response = ResponseType.FULL
            };
            string toSend = JsonConvert.SerializeObject(response);

            // notify
            sub.PublishAsync(player, toSend);
        }

        public void endGame()
        {
            mutex.WaitOne();

            count--;

            mutex.ReleaseMutex();

            window.updateText("Game ended.");
        }
    }
}
