using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Collections.Generic;

namespace GoFishClient
{
    // This is the top bar
    [Activity(Label = "ULTIMATE GO FISH HERO BUDOKAI", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        TextView setsTextView;
        TextView statusTextView;
        Spinner playerCardSpinner;
        Spinner playerSpinner;
        TextView receivedCardsTextView;
        Button connectButton;
        Button buttonGoFish;

        static TcpClient client = new TcpClient();

        static List<Card> hand = new List<Card>();

        static bool active = false;

        static bool gameEnded = false;

        static byte points = 0;

        static string localPlayer = "Kenned";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            //Init Components
            setsTextView = FindViewById<TextView>(Resource.Id.SetsTextView);
            statusTextView = FindViewById<TextView>(Resource.Id.StatusTextView);
            playerCardSpinner = FindViewById<Spinner>(Resource.Id.PlayerCardSpinner);
            receivedCardsTextView = FindViewById<TextView>(Resource.Id.ReceivedCardsTextView);
            playerSpinner = FindViewById<Spinner>(Resource.Id.PlayerSpinner);
            buttonGoFish = FindViewById<Button>(Resource.Id.ButtonGoFish);
            connectButton = FindViewById<Button>(Resource.Id.ButtonConnect);

            buttonGoFish.Click += (sender, e) =>
            {
                SendCardRequest();
            };

            // Connect to server
            connectButton.Click += (sender, e) =>
            {
                connectButton.Enabled = false;
                buttonGoFish.Enabled = true;

                IPAddress IP = IPAddress.Parse("172.16.19.10");
                int port = 5000;


                client = new TcpClient();

                client.Connect(IP, port);
                connectButton.Text = "CONNECTED";

                NetworkStream networkStream = client.GetStream();

                Thread thread = new Thread(o => ReceiveData((TcpClient)o));
                thread.Start(client);

                SendData(client);

                setupSpinner();
            };
        }

        void setupSpinner()
        {
            RunOnUiThread(() =>
            {
                String[] items = new String[hand.Count];
                // Create a list of items for the spinner
                for (int i = 0; i < hand.Count; i++)
                {
                    items[i] = hand[i].FullName;
                }

                //create an adapter to describe how the items are displayed, adapters are used in several places in android.
                //There are multiple variations of this, but this is the basic variant.

                ArrayAdapter adapter = new ArrayAdapter(this, (Resource.Layout.support_simple_spinner_dropdown_item), items);
                //set the spinners adapter to the previously created one.
                playerCardSpinner.Adapter = adapter;
            });
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void ReceiveData(TcpClient client)
        {
            // Receive requests
            NetworkStream ns = client.GetStream();

            byte[] receiveBytes = new byte[1024];
            int byteCount;

            while ((byteCount = ns.Read(receiveBytes, 0, receiveBytes.Length)) > 0)
            {
                Request request;

                if (Encoding.UTF8.GetString(receiveBytes, 0, byteCount).Contains("Players"))
                {
                    request = JsonConvert.DeserializeObject<SetupRequest>(Encoding.UTF8.GetString(receiveBytes, 0, byteCount));
                }
                else
                {
                    request = JsonConvert.DeserializeObject<GameRequest>(Encoding.UTF8.GetString(receiveBytes, 0, byteCount));
                }

                Console.WriteLine("RECEIVED REQUEST TYPE: " + request.RequestType);

                GameRequest gameRequest;
                switch (request.RequestType)
                {
                    // End game
                    case 0:
                        statusTextView.Text = "The game has ended!";
                        AnswerPointRequest();
                        break;
                    // Answer on request
                    case 1:
                        gameRequest = (GameRequest)request;

                        if (gameRequest.UserTo == localPlayer)
                        {
                            statusTextView.Text = "It's your turn, boi!";
                        }
                        else
                        {
                            statusTextView.Text = "It's not your turn yet!!";
                        }
                        break;
                    // Receive card(s)
                    case 2:
                        gameRequest = (GameRequest)request;

                        if (gameRequest.UserFrom == "Dealer")
                        {
                            statusTextView.Text = "It's not your turn yet!!";
                        }
                        else if (gameRequest.UserFrom != localPlayer)
                        {
                            statusTextView.Text = "It's your turn, boi!";
                            if (gameRequest.Cardlist.Count == 1)
                            {
                                receivedCardsTextView.Text = "You received " + gameRequest.Cardlist.Count + " card!";
                            }
                            else
                            {
                                receivedCardsTextView.Text = "You received " + gameRequest.Cardlist.Count + " cards!";
                            }
                        }
                        else
                        {
                            statusTextView.Text = "It's not your turn yet!!";
                            receivedCardsTextView.Text = "Go fish!";
                        }

                        Console.WriteLine(gameRequest.CardValue);
                        Console.WriteLine(gameRequest.Cardlist[0].FullName);
                        AddCardsToCollection(gameRequest.Cardlist, gameRequest.UserFrom);
                        CheckForSets();

                        setupSpinner();
                        break;
                    // Give cards away
                    case 3:
                        gameRequest = (GameRequest)request;

                        GiveCardsAway(gameRequest.CardValue, client);

                        setupSpinner();
                        break;
                    case 4:
                        SetupRequest setupRequest = (SetupRequest)request;

                        RunOnUiThread(() =>
                        {
                            String[] players = new String[setupRequest.Players.Count - 1];
                            int counter = 0;
                            foreach (string player in setupRequest.Players)
                            {
                                if (player != localPlayer)
                                {
                                    players[counter] = player;
                                    counter++;
                                }
                            }

                            ArrayAdapter adapter = new ArrayAdapter(this, (Resource.Layout.support_simple_spinner_dropdown_item), players);
                            //set the spinners adapter to the previously created one.
                            playerSpinner.Adapter = adapter;
                        });
                        break;
                    case 10:
                        gameRequest = (GameRequest)request;
                        receivedCardsTextView.Text = (gameRequest.UserFrom + " won with " + gameRequest.CardValue + " sets!");
                        gameEnded = true;
                        break;
                    default:
                        break;
                }

                if (gameEnded == true)
                {
                    break;
                }
            }
            //connectButton.Text = "CONNECT";
            //connectButton.Enabled = true;
            //hand.Clear();
        }

        private void CheckForSets()
        {
            bool setFound = false;

            for (byte i = 1; i < 15; i++)
            {
                byte counter = 0;
                foreach (Card c in hand)
                {
                    if (c.Value == i)
                        counter++;

                    if (counter == 4)
                    {
                        setFound = true;
                        break;
                    }
                }

                if (setFound)
                {
                    points++;

                    setsTextView.Text = "Sets: " + points;

                    for (int j = hand.Count - 1; j >= 0; j--)
                    {
                        if (hand[j].Value == i)
                        {
                            hand.RemoveAt(j);
                        }
                    }

                    if (hand.Count == 0)
                    {
                        GoFish();
                    }

                    setFound = false;
                }
            }
        }

        private void GoFish()
        {
            GameRequest gr = new GameRequest();
            gr.UserFrom = localPlayer;
            gr.UserTo = localPlayer;

            string s;

            NetworkStream ns = client.GetStream();

            gr.RequestType = 3;
            byte[] buffer;

            gr.RequestType = 3;
            gr.CardValue = 0;

            s = JsonConvert.SerializeObject(gr, Formatting.Indented);
            buffer = Encoding.UTF8.GetBytes(s);
            ns.Write(buffer, 0, buffer.Length);
            Console.WriteLine(s);
            active = false;

            statusTextView.Text = "It's not your turn yet!!";
            receivedCardsTextView.Text = "Go fish!";
        }

        private static void AddCardsToCollection(List<Card> cardlist, string opponentName)
        {
            if (cardlist.Count > 0)
            {
                for (int i = 0; i < cardlist.Count; i++)
                {
                    hand.Add(cardlist[i]);
                }
            }
        }

        private void GiveCardsAway(byte cardValue, TcpClient client)
        {
            List<Card> cardsToSend = new List<Card>();

            // Check cards
            for (int i = hand.Count - 1; i >= 0; i--)
            {
                if (hand[i].Value == cardValue)
                {
                    cardsToSend.Add(hand[i]);
                    hand.RemoveAt(i);
                }
            }

            if (hand.Count == 0)
            {
                GoFish();
            }

            string s;
            GameRequest gr = new GameRequest();
            NetworkStream ns = client.GetStream();

            gr.Cardlist = cardsToSend;
            gr.RequestType = 3;
            gr.UserTo = playerSpinner.SelectedItem.ToString();
            gr.UserFrom = localPlayer;
            gr.CardValue = cardValue;

            s = JsonConvert.SerializeObject(gr, Formatting.Indented);
            byte[] buffer = Encoding.UTF8.GetBytes(s);
            ns.Write(buffer, 0, buffer.Length);

            string cardsStolen = "";

            #region Action description
            switch (cardValue)
            {
                case 1:
                    if (gr.Cardlist.Count == 1)
                    {
                        cardsStolen = gr.Cardlist.Count + " ace";
                    }
                    else
                    {
                        cardsStolen = gr.Cardlist.Count + " aces";
                    }
                    break;
                case 11:
                    if (gr.Cardlist.Count == 1)
                    {
                        cardsStolen = gr.Cardlist.Count + " jack";
                    }
                    else
                    {
                        cardsStolen = gr.Cardlist.Count + " jacks";
                    }
                    break;
                case 12:
                    if (gr.Cardlist.Count == 1)
                    {
                        cardsStolen = gr.Cardlist.Count + " queen";
                    }
                    else
                    {
                        cardsStolen = gr.Cardlist.Count + " queens";
                    }
                    break;
                case 13:
                    if (gr.Cardlist.Count == 1)
                    {
                        cardsStolen = gr.Cardlist.Count + " king";
                    }
                    else
                    {
                        cardsStolen = gr.Cardlist.Count + " kings";
                    }
                    break;
                case 14:
                    if (gr.Cardlist.Count == 1)
                    {
                        cardsStolen = gr.Cardlist.Count + " joker";
                    }
                    else
                    {
                        cardsStolen = gr.Cardlist.Count + " jokers";
                    }
                    break;
                default:
                    if (gr.Cardlist.Count == 1)
                    {
                        cardsStolen = gr.Cardlist.Count + " " + cardValue;
                    }
                    else
                    {
                        cardsStolen = gr.Cardlist.Count + " " + cardValue + "s";
                    }
                    break;
            }
            #endregion

            receivedCardsTextView.Text = gr.UserTo + " stole " + cardsStolen;
        }

        private void SendData(TcpClient client)
        {
            GameRequest gr = new GameRequest();
            gr.UserFrom = localPlayer;
            if (playerSpinner.Count != 0)
            {
                gr.UserTo = playerSpinner.SelectedItem.ToString();
            }
            else
            {
                gr.UserTo = null;
            }

            string s;

            NetworkStream ns = client.GetStream();

            ConnectionRequest cr = new ConnectionRequest();
            cr.Username = localPlayer;
            cr.RequestType = 1;

            s = JsonConvert.SerializeObject(cr, Formatting.Indented);
            byte[] buffer = Encoding.UTF8.GetBytes(s);
            ns.Write(buffer, 0, buffer.Length);
        }

        void SendCardRequest()
        {
            NetworkStream ns = client.GetStream();
            int cardValue = 0;
            GameRequest gr = new GameRequest();

            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].FullName == playerCardSpinner.SelectedItem.ToString())
                {
                    gr.CardValue = hand[i].Value;
                }
            }

            gr.RequestType = 1;
            gr.UserFrom = localPlayer;
            gr.UserTo = playerSpinner.SelectedItem.ToString();
            string s = JsonConvert.SerializeObject(gr, Formatting.Indented);
            byte[] buffer = Encoding.UTF8.GetBytes(s);
            buffer = Encoding.UTF8.GetBytes(s);
            ns.Write(buffer, 0, buffer.Length);
        }

        void AnswerPointRequest()
        {
            NetworkStream ns = client.GetStream();
            GameRequest gr = new GameRequest();

            gr.RequestType = 10;
            gr.UserFrom = localPlayer;
            gr.CardValue = points;
            string s = JsonConvert.SerializeObject(gr, Formatting.Indented);
            byte[] buffer = Encoding.UTF8.GetBytes(s);
            buffer = Encoding.UTF8.GetBytes(s);
            ns.Write(buffer, 0, buffer.Length);
        }
    }
}