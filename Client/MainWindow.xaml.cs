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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // redis
        private ConnectionMultiplexer redis;
        IDatabase db;
        ISubscriber sub;
        // game state
        private enum State { lobby, wait, game, win};
        private State state;
        // player
        int id;


        public MainWindow()
        {
            InitializeComponent();
            // init redis
            initRedis();
            // set the state of the game
            state = State.lobby;
            // set random player id
            initID();
        }

        private async void initRedis()
        {
            // conect to server
            String s = "localhost";
            redis = await ConnectionMultiplexer.ConnectAsync(s);
            db = redis.GetDatabase();
            sub = redis.GetSubscriber();
            info.Content = "Press SPACE to find a match";
        }

        private void initID()
        {
            Random r = new Random();
            id = r.Next();
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // lobby
            if (state == State.lobby)
            {
                if (e.Key == Key.Space)
                {
                    showMessage("ID: " + id + "\nLooking for oponents...");
                    // listen to server for changes
                    await sub.SubscribeAsync("game", (channel, msg) =>
                    {
                        handleLobby(msg);
                    });
                    // send id to server
                    await sub.PublishAsync("find", id);
                }
            }
            // game
        }

        private async void handleLobby(String msg)
        {
            String[] parts = msg.Split(' ');
            // check 
            if (parts[0] == "full")
            {
                await sub.UnsubscribeAsync("game");
                showMessage("Server full.\nPress SPACE to try again.");
            }
            else if (parts[0] == "found")
            {
                showMessage("Match found.\nWaiting for oponent...");
                state = State.wait;
            }
            else if (parts[0] == "start")
            {
                await sub.UnsubscribeAsync("game");
                // prepare game
                prepareGame();
            }
        }

        private async void prepareGame()
        {
            // hide text panel
            hidePanel();
            // show ball
            // subscribe
            await sub.SubscribeAsync("moves", (channel, msg) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    string[] s = ((string)msg).Split(' ');
                    int xx = int.Parse(s[0]);
                    int yy = int.Parse(s[1]);
                    info.Content = msg + "\n";
                    // move ball
                    Canvas.SetLeft(ball, xx);
                    Canvas.SetTop(ball, yy);
                }), DispatcherPriority.Background);
            });
        }
        
        private void hidePanel()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                panel.Visibility = Visibility.Hidden;
            }), DispatcherPriority.Background);
        }

        private void showPanel()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                panel.Visibility = Visibility.Visible;
            }), DispatcherPriority.Background);
        }

        private void showMessage(String msg)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                info.Content = msg;
            }), DispatcherPriority.Background);
        }
    }
}
