using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // redis subscriber
        ISubscriber sub;

        // game state
        private PlayerState playerState;
        private State currentState;

        // player
        Player player;

        // game
        int gameID;

        // arena
        Arena arena;

        // blocks
        Rectangle[,] rectBlocks;


        // INITIATION

        public MainWindow()
        {
            InitializeComponent();
            // custom data
            initData();
            // initiate blocks
            initBlocks();
        }

        private void initData()
        {
            // create player
            player = new Player();

            // set the state of the game
            playerState = PlayerState.LOBBY;
            currentState = State.SCORE;

            // arena
            arena = new Arena();

        }

        private void initBlocks()
        {
            int n = arena.BlocksN;
            int m = arena.BlocksM;
            rectBlocks = new Rectangle[n, m];
            double height = arena.BlocksHeight;
            double w = arena.Width / m;
            double h = height / n;
            double startY = (arena.Height - height) / 2;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    Rectangle r = new Rectangle();
                    r.Height = h;
                    r.Width = w;
                    r.Opacity = 0;
                    canvas.Children.Add(r);
                    runAction(positionElement(r, j * w + w/2, startY + i * h + h/2));
                    rectBlocks[i, j] = r;
                }
            }
        }

        private void hideBlocks()
        {
            int n = arena.BlocksN;
            int m = arena.BlocksM;
            Dispatcher.Invoke(new Action(() =>
            {    
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < m; j++)
                    {
                        rectBlocks[i, j].Opacity = 0;
                    }
                }
            }), DispatcherPriority.Normal);
        }
        
        private async Task connectToServer()
        {
            if (playerState != PlayerState.SEARCHING)
            {
                playerState = PlayerState.SEARCHING;

                // get data 
                string server = inputServer.Text;
                player.Tag = inputTag.Text;

                // get server subscriber
                sub = await RedisServer.GetSubscriber(server);

                // prepare message to send
                string toSend = JsonConvert.SerializeObject(player.generateInfo());

                // ui update
                runAction(setLabelText(info, "ID: " + player.ID + "\nLooking for oponents..."));

                // listen to server for changes
                await sub.SubscribeAsync(player.ID.ToString(), (channel, msg) =>
                {
                    handleLobby(msg);
                });

                // send request to server
                await sub.PublishAsync("find", toSend);
            }
        }

        private async void handleLobby(String msg)
        {
            // read response
            GameResponse response = JsonConvert.DeserializeObject<GameResponse>(msg);

            // unsubscribe from channel
            await sub.UnsubscribeAsync(player.ID.ToString());

            // read state
            switch (response.Response)
            {
                case ResponseType.FULL:
                    // full server
                    playerState = PlayerState.LOBBY;
                    runAction(setLabelText(info, "Server full.\nPress CONNECT to try again."));
                    break;
                case ResponseType.FOUND:
                    // match found
                    playerState = PlayerState.PLAYING;
                    runAction(setLabelText(info, "Match found.\nWaiting for oponent..."));
                    prepareGame(response.GameID, response.Index);
                    break;
                case ResponseType.ERROR:
                    // server error
                    playerState = PlayerState.LOBBY;
                    runAction(setLabelText(info, "Server ERROR.\nPress CONNECT to try again."));
                    break;
                default:
                    break;
            }
        }

        private async void prepareGame(int id, int index)
        {
            // set game and player data
            gameID = id;
            player.Index = index;
            // subscribe to server
            await sub.SubscribeAsync("game" + gameID, (channel, msg) =>
            {
                handleGame(msg);
            });
        }

        private async void handleGame(string msg)
        {
            GameState gameState = JsonConvert.DeserializeObject<GameState>(msg);

            // update game state
            updateGame(gameState);

            switch (gameState.Type)
            {
                case State.WAITING:
                    if (currentState != State.WAITING)
                    {
                        // show waiting
                        runAction(showPanel(panelWaiting));
                        currentState = State.WAITING;
                    }

                    // change wait text
                    if (gameState.Ready[player.Index])
                    {
                        runAction(setLabelText(labelWaiting, "WAITING FOR OPPONENT"));
                    }
                    else
                    {
                        runAction(setLabelText(labelWaiting, "CLICK TO START"));
                    }

                    break;

                case State.COUNTDOWN:
                    if (currentState != State.COUNTDOWN)
                    {
                        // show countdown
                        runAction(showPanel(panelCountdown));
                        currentState = State.COUNTDOWN;
                    }

                    // change countdown number
                    runAction(setLabelText(labelCountdown, gameState.Count > 0 ? "" + gameState.Count : "GO"));
                    break;

                case State.GAMEPLAY:
                    if (currentState != State.GAMEPLAY)
                    {
                        // hide all
                        runAction(showPanel(null));

                        currentState = State.GAMEPLAY;
                    }
                    break;

                case State.SCORE:
                    if (currentState != State.SCORE)
                    {
                        // show
                        runAction(showPanel(panelScore));

                        // set text
                        runAction(setLabelText(labelScorePanel, gameState.Scored));
                        runAction(setLabelText(labelScoreAction, "SCORED"));
                        currentState = State.SCORE;
                    }
                    break;

                case State.VICTORY:
                    if (currentState != State.VICTORY)
                    {
                        // show
                        runAction(showPanel(panelScore));

                        // set text
                        runAction(setLabelText(labelScorePanel, gameState.Scored));
                        runAction(setLabelText(labelScoreAction, "IS VICTORIOUS"));
                        currentState = State.VICTORY;
                    }
                    break;

                case State.GAMEOVER:
                    // unsubscribe
                    await sub.UnsubscribeAsync("game" + gameID);

                    currentState = State.GAMEOVER;
                    // show menu
                    runAction(showPanel(panelMenu));

                    // change state
                    runAction(setLabelText(info, ""));

                    // reset game
                    initData();
                    hideBlocks();
                    break;
                default:
                    break;
            }
        }


        // UI CHANGES

        private void updateGame(GameState gameState)
        {
            Action[] actions = new Action[] {
                positionElement(ball1, gameState.Balls[0].pos.x, gameState.Balls[0].pos.y),
                positionElement(ball2, gameState.Balls[1].pos.x, gameState.Balls[1].pos.y),
                positionElement(playerRect, gameState.Players[0].x, arena.Height - arena.PlayerOffset),
                positionElement(enemyRect, gameState.Players[1].x, arena.PlayerOffset),
                recolorBalls(gameState.Balls),
                showShields(gameState.Shields),
                recolorBlocks(gameState.Blocks, gameState.time),
                updateUI(gameState.time, gameState.Score, gameState.Shields[player.Index]),
            };

            Dispatcher.Invoke(new Action(() =>
            {
                for (int i = 0; i < actions.Length; i++)
                {
                    actions[i]();
                }
            }), DispatcherPriority.Normal);
        }

        private void runAction(Action action)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                action();
            }), DispatcherPriority.Normal);
        }

        private Action updateUI(int time, int[] score, int shield)
        {
            int index = player.Index;
            int seconds = time / 1000;
            string timeUIMin = "" + (seconds / 60);
            string timeUISec = ((seconds % 60 < 10) ? "0" + (seconds % 60) : "" + (seconds % 60));
            string scoreUI = score[index] + " - " + score[index == 0 ? 1 : 0];
            string shieldUI = "SHIELD  " + shield;
            Color red = (Color)ColorConverter.ConvertFromString("#FFEA3F24");
            Color blue = (Color)ColorConverter.ConvertFromString("#FF3D3DFF");
            Color gray = (Color)ColorConverter.ConvertFromString("#FFE0E0E0");

            return new Action(() =>
            {
                labelShield.Content = shieldUI;
                labelTimeMinutes.Content = timeUIMin;
                labelTimeSeconds.Content = timeUISec;
                // score rectangles
                for (int i = 0; i < topScore.Children.Count; i++)
                {
                    switch (score[i])
                    {
                        case -1:
                            ((Rectangle)topScore.Children[i]).Fill = new SolidColorBrush(gray);
                            ((Rectangle)panelScoreBlocks.Children[i]).Fill = new SolidColorBrush(gray);
                            break;
                        case 0:
                            ((Rectangle)topScore.Children[i]).Fill = new SolidColorBrush(blue);
                            ((Rectangle)panelScoreBlocks.Children[i]).Fill = new SolidColorBrush(blue);
                            break;
                        case 1:
                            ((Rectangle)topScore.Children[i]).Fill = new SolidColorBrush(red);
                            ((Rectangle)panelScoreBlocks.Children[i]).Fill = new SolidColorBrush(red);
                            break;
                        default:
                            break;
                    }
                }
            });
        }

        private Action showShields(int[] shields)
        {
            Rectangle[] shieldsOne = new Rectangle[] { shield1, shield2, shield3 };
            Rectangle[] shieldsTwo = new Rectangle[] { shield4, shield5, shield6 };

            return new Action(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    // one
                    positionElement(shieldsOne[i], arena.Width/2, arena.Height - (i * 5 + 2.5))();
                    shieldsOne[i].Opacity = (i < shields[0]) ? 1 : 0;

                    // two
                    positionElement(shieldsTwo[i], arena.Width/2, (i * 5 + 2.5))();
                    shieldsTwo[i].Opacity = (i < shields[1]) ? 1 : 0;
                }
            });
        }

        private Action recolorBalls(Ball[] balls)
        {
            Color red = (Color)ColorConverter.ConvertFromString("#FFEA3F24");
            Color blue = (Color)ColorConverter.ConvertFromString("#FF3D3DFF");
            Color b1 = (balls[0].player == 0) ? blue : red;
            Color b2 = (balls[1].player == 0) ? blue : red;

            return new Action(() =>
            {
                // ball 1
                ball1.Fill = new SolidColorBrush(b1);
                // ball 2
                ball2.Fill = new SolidColorBrush(b2);
            });
        }

        // update blocks color
        private Action recolorBlocks(int[,] blocks, int time)
        {
            int n = blocks.GetLength(0);
            int m = blocks.GetLength(1);

            return new Action(() =>
            {
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < m; j++)
                    {
                        int v = blocks[i, j];

                        // in case of player
                        int row = (player.Index == 0) ? i : (n - 1) - i;
                        int col = (player.Index == 0) ? j : (m - 1) - j;
                        Rectangle r = rectBlocks[row, col];

                        r.Opacity = 1;
                        switch (v)
                        {
                            case 0:
                                // empty block
                                r.Opacity = 0;
                                break;
                            case 1:
                                // normal block
                                r.Fill = new SolidColorBrush(Colors.LightGray);
                                break;
                            case 2:
                                // double block
                                r.Fill = new SolidColorBrush(Color.FromRgb(96, 125, 139));
                                break;
                            case 3:
                                // unbreakable block
                                r.Fill = new SolidColorBrush(Colors.Black);
                                break;
                            case 4:
                                // reward block
                                r.Fill = new SolidColorBrush(ColorConverterRGB.HSBtoRGB(((time / 20) % 360) / 360f, 1f, 1f));
                                break;
                            default:
                                break;
                        }
                    }
                }

            });
        }

        // set element position on canvas
        private Action positionElement(Shape shape, double x, double y)
        {
            arena.correctCoordinates(ref x, ref y, player.Index);

            return new Action(() =>
            {
                Canvas.SetLeft(shape, x - shape.Width/2);
                Canvas.SetTop(shape, y - shape.Height/2);
            });
        }

        // change label content
        private Action setLabelText(Label label, string text)
        {
            return new Action(() =>
            {
                label.Content = text;
            });
        }

        // show only one panel
        private Action showPanel(Grid panel)
        {
            Grid[] panels = new Grid[] 
            {
                panelMenu,
                panelCountdown,
                panelScore,
                panelWaiting
            };

            Visibility[] visibilities = new Visibility[panels.Length];

            for (int i = 0; i < panels.Length; i++)
            {
                visibilities[i] = (panel == panels[i]) ? Visibility.Visible : Visibility.Hidden;
            }

            return new Action(() =>
            {
                for (int i = 0; i < panels.Length; i++)
                {
                    panels[i].Visibility = visibilities[i];
                }
            });
        }


        // EVENT HANDLERS

        // find games
        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            await connectToServer();
        }

        // player movement
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (playerState == PlayerState.PLAYING)
            {
                sendAction(e.GetPosition(this).X, e.GetPosition(this).Y, FeedbackType.MOVE);
            }
        }

        // player action
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (playerState == PlayerState.PLAYING)
            {
                sendAction(0, 0, FeedbackType.ACTION);
            }
        }

        // send action to server
        private void sendAction(double x, double y, FeedbackType type)
        {
            arena.correctCoordinates(ref x, ref y, player.Index);

            PlayerFeedback action = new PlayerFeedback()
            {
                Index = player.Index,
                Position = new Coords(x, y),
                type = type
            };

            string toSend = JsonConvert.SerializeObject(action);

            sub.PublishAsync("action" + gameID, toSend);
        }

        // drag app
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        // close app
        private void BtnClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        // minimize app
        private void BtnMinimize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
