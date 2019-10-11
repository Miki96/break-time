using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Server
{
    class Game
    {
        // game manager
        private Manager manager;
        private ISubscriber sub;

        // data
        private int gameID;
        private List<Player> players;
        private State gameState;
        private int time;
        private int speedChange;
        private int speedCurrent;

        // score
        private double speedDelta;
        private string scored;
        private int[] score;
        private int round;
        private int scoreScreenTime;
        private int winScreenTime;

        // arena
        Arena arena;

        // ball
        private Ball[] balls;
        private double startSpeed;

        // timer
        private Timer timer;
        private Timer[] timers;
        private Timer[] listeners;
        private int countTime;
        private int scoreTime;
        private int winTime;

        // refresh rate
        private int refreshRate;

        // check if game is full
        public bool Full { get; private set; }
        public bool Half { get; private set; }

        public Game(Manager manager, int gameID, ISubscriber sub)
        {
            this.manager = manager;
            this.gameID = gameID;
            this.sub = sub;

            // settings
            refreshRate = 15; // 15

            // initialize game, players and balls
            initGame();
        }

        // initilize game
        public void initGame()
        {
            // arena
            arena = new Arena();
            // balls
            startSpeed = 3;
            balls = new Ball[] {
                new Ball(new Coords(arena.Width/2, arena.PlayerOffset + 10), 1, new Coords(0.87, 0.5), 5, startSpeed),
                new Ball(new Coords(arena.Width/2, arena.Height - (arena.PlayerOffset + 10)), 0, new Coords(-0.87, -0.5), 5, startSpeed)
            };
            // players
            Full = false;
            Half = false;
            players = new List<Player>();
            score = new int[] { -1, -1, -1, -1, -1 };
            round = 0;
            scored = "";
            // speed
            speedChange = 15 * 1000;
            speedCurrent = 0;
            speedDelta = 0.25;
            // time
            time = 0;
            scoreScreenTime = 3000;
            winScreenTime = 5000;
            winTime = 0;
            scoreTime = 0;
            timers = new Timer[2];
            listeners = new Timer[2];
        }

        // reset game and load next level
        public void nextLevel()
        {
            // reset balls
            balls = new Ball[] {
                new Ball(new Coords(arena.Width/2, arena.PlayerOffset + 10), 1, new Coords(0.87, 0.5), 5, startSpeed),
                new Ball(new Coords(arena.Width/2, arena.Height - (arena.PlayerOffset + 10)), 0, new Coords(-0.87, -0.5), 5, startSpeed)
            };
            // blocks
            round++;
            arena.loadBlocks(0);
            // time
            time = 0;
            // reset players
            players[0].reset();
            players[1].reset();
        }

        // start listening to players actions and send gameplay data
        private async void startGameAsync()
        {
            // listen to players actions
            await startListeningAsync();

            // start gameplay loop
            startLoop();

            // notify server
            manager.window.updateText("Game started.");
        }

        // listen to player inputs
        private async Task startListeningAsync()
        {
            await sub.SubscribeAsync("action" + gameID, (channel, msg) =>
            {
                handlePlayer(msg);
            });
        }

        // handle player actions
        private void handlePlayer(string msg)
        {
            // read actions
            PlayerFeedback actions = JsonConvert.DeserializeObject<PlayerFeedback>(msg);
            int w = players[actions.Index].width;

            switch (actions.type)
            {
                case FeedbackType.MOVE:
                    // fix position
                    if (actions.Position.X < w / 2) actions.Position.X = w / 2;
                    if (actions.Position.X > arena.Width - w / 2) actions.Position.X = arena.Width - w / 2;
                    players[actions.Index].pos = actions.Position;
                    break;
                case FeedbackType.ACTION:
                    players[actions.Index].ready = true;
                    manager.window.updateText("ACTION " + actions.Index);
                    break;
                default:
                    break;
            }
        }

        // start gameplay
        private void startLoop()
        {
            // set initial state to waiting
            gameState = State.WAITING;

            // create timer
            timer = new Timer(refreshRate);
            timer.Elapsed += gameplay;
            timer.Start();
        }

        // move balls and players
        private async void gameplay(Object o, EventArgs e)
        {
            // handle state
            switch (gameState)
            {
                case State.COUNTDOWN:
                    // reduce countdown
                    countTime -= refreshRate;
                    if (countTime < 0)
                    {
                        countTime = 0;
                        gameState = State.GAMEPLAY;
                    }
                    break;
                case State.WAITING:
                    // game waits for players ready input
                    if (players[0].ready && players[1].ready)
                    {
                        // start countdown
                        countTime = 3999;
                        gameState = State.COUNTDOWN;
                    }
                    break;
                case State.GAMEPLAY:
                    // time
                    time += refreshRate;
                    // speed
                    speedCurrent += refreshRate;
                    if (speedCurrent > speedChange)
                    {
                        speedCurrent = 0;
                        for (int i = 0; i < balls.Length; i++)
                        {
                            balls[i].speed += speedDelta;
                        }
                    }
                    // move balls
                    moveBalls();
                    break;
                case State.SCORE:
                    // reduce scoretime
                    scoreTime -= refreshRate;
                    if (scoreTime< 0)
                    {
                        scoreTime = 0;
                        gameState = State.WAITING;
                        // load new level
                        nextLevel();
                    }
                    break;
                case State.OFFLINE:
                    // reduce offline time
                    winTime -= refreshRate;
                    if (winTime < 0)
                    {
                        winTime = 0;
                        gameState = State.GAMEOVER;
                        // end game
                        endGame();
                    }
                    break;
                case State.VICTORY:
                    // reduce win time
                    winTime -= refreshRate;
                    if (winTime < 0)
                    {
                        winTime = 0;
                        gameState = State.GAMEOVER;
                        // end game
                        endGame();
                    }
                    break;
                default:
                    break;
            }

            // stop sending when game is over
            if (gameState == State.GAMEOVER) return;

            // create state
            GameState currentState = new GameState()
            {
                Type = gameState,
                Players = new Coords[] {players[0].pos, players[1].pos},
                Balls = balls,
                Score = score,
                Count = (countTime / 1000),
                Ready = new bool[] { players[0].ready, players[1].ready},
                Blocks = arena.Blocks,
                Shields = new int[] {Math.Max(players[0].shield, 0), Math.Max(players[1].shield, 0) },
                time = time,
                Scored = scored,
            };

            // send state
            string toSend = JsonConvert.SerializeObject(currentState);
            await sub.PublishAsync("game" + gameID, toSend);
        }

        // main ball logic
        public void moveBalls()
        {
            double freeSpace = (arena.Height - arena.BlocksHeight) / 2;

            // move balls
            for (int i = 0; i < balls.Length; i++)
            {
                Coords ball = balls[i].pos;
                Coords dir = balls[i].dir;
                int size = balls[i].size;
                double speed = balls[i].speed;


                if (ball.Y + size > arena.Height - arena.PlayerOffset)
                {
                    // player ONE area
                    Geometry.playerColision(balls[i], players[0], arena);
                }
                else if (ball.Y - size < arena.PlayerOffset)
                {
                    // player TWO area
                    Geometry.playerColision(balls[i], players[1], arena);
                }
                else if (ball.Y - size < (freeSpace + arena.BlocksHeight) && ball.Y + size > freeSpace)
                {
                    // blocks area
                    Geometry.blocksCollision(balls[i], players[balls[i].player], arena);
                }

                // check if side wall is hit
                Geometry.sideWallCollision(balls[i], arena);

                // check if shields are hit
                Geometry.shieldCollision(balls[i], players, arena);

                // check for score
                if (players[0].shield < 0)
                {
                    handleScore(1);
                    manager.window.updateText("playerONE : " + players[0].score + "  |  playerTWO : " + players[1].score);
                    break;
                }
                else if (players[1].shield < 0)
                {
                    handleScore(0);
                    manager.window.updateText("playerONE : " + players[0].score + "  |  playerTWO : " + players[1].score);
                    break;
                }

                // apply direction
                ball.X += dir.X * speed;
                ball.Y += dir.Y * speed;
            }
        }
        
        // player scored
        public void handleScore(int player)
        {
            scoreTime = scoreScreenTime;
            scored = players[player].tag;
            players[0].ready = false;
            players[1].ready = false;
            players[player].score++;
            if (round < 5) score[round] = player;
            gameState = State.SCORE;

            // check if game ended
            if (players[player].score == 3)
            {
                winTime = winScreenTime;
                gameState = State.VICTORY;
            }
        }

        // add new player to a game
        public void addPlayer(PlayerInfo playerInfo)
        {
            // notify player
            int index = players.Count;
            gameInfoNotify(playerInfo.ID.ToString(), index);
            notifyOnline(playerInfo.ID.ToString(), index);

            // listen if player is online
            listenPlayer(index);

            // add to game
            players.Add(new Player(playerInfo.Tag, playerInfo.ID, index, new Coords(arena.Width/2, 0)));
            
            // check if game should start
            if (players.Count == 2)
            {
                Full = true;
                // start game
                startGameAsync();
            }
            else
            {
                Half = true;
            }
        }

        // notify player about game info
        private void gameInfoNotify(string player, int index) 
        {
            GameResponse response = new GameResponse()
            {
                Response = ResponseType.FOUND,
                GameID = gameID,
                Index = index
            };
            string toSend = JsonConvert.SerializeObject(response);

            // notify
            sub.PublishAsync(player, toSend);
        }

        // notify player that server is online
        private void notifyOnline(string player, int index)
        {
            timers[index] = new Timer(2000);
            timers[index].Elapsed += (o, e) => {
                sub.PublishAsync("live:" + player, "live");
            };
            timers[index].Start();
        }

        // listen if player is online
        private async void listenPlayer(int index)
        {
            // listen for player
            await sub.SubscribeAsync("liveGame:" + gameID + "" + index, (channel, msg) =>
            {
                players[index].online = true;
            });

            // check for input
            listeners[index] = new Timer(3000);
            listeners[index].Elapsed += (o, e) => {
                if (!players[index].online && gameState != State.OFFLINE)
                {
                    // break game
                    if (Full)
                    {
                        scored = players[(index == 0) ? 1 : 0].tag;
                        winTime = winScreenTime;
                        gameState = State.OFFLINE;
                    } else
                    {
                        endGame();
                    }
                    // stop listener
                    listeners[index].Stop();
                    listeners[index] = null;
                }
                else
                {
                    players[index].online = false;
                }
            };
            listeners[index].Start();
        }

        // end current game
        public async void endGame()
        {
            // stop timers
            if (timer != null) timer.Stop();
            for (int i = 0; i < timers.Length; i++)
            {
                if (timers[i] != null) timers[i].Stop();
                if (listeners[i] != null) listeners[i].Stop();
            }

            // stop listening
            await sub.UnsubscribeAsync("action" + gameID);
            await sub.UnsubscribeAsync("liveGame:" + gameID + "0");
            await sub.UnsubscribeAsync("liveGame:" + gameID + "1");

            if (players.Count == 2)
            {
                // notify players that game ended
                GameState currentState = new GameState()
                {
                    Type = State.GAMEOVER,
                    Players = new Coords[] { players[0].pos, players[1].pos },
                    Balls = balls,
                    Score = score,
                    Count = (countTime / 1000),
                    Ready = new bool[] { players[0].ready, players[1].ready },
                    Blocks = arena.Blocks,
                    Shields = new int[] { players[0].shield, players[1].shield },
                    time = time,
                    Scored = scored,
                };
                // send state
                string toSend = JsonConvert.SerializeObject(currentState);
                await sub.PublishAsync("game" + gameID, toSend);
            }

            // clear game
            initGame();

            // notify manager
            manager.endGame();
        }
    }
}
