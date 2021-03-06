﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
using System.Xml.Linq;
using System.Xml.Serialization;
using wox.serial;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Data members
        private Thread incomingThread; //Thread for processing incoming queries
        private int counter; //Count number of connections
        private Dictionary<Thread, Socket> connections;
        private Dictionary<Thread, BinaryWriter> writers;
        private Dictionary<Thread, int> numberOfEcryptionsPerUser; //Collect number of encryptions for each user
        private Dictionary<string, List<string>> cardAndEncryptions; //Collect all card and their encryptions
        private Users registeredUsers;
        private static int KEY = 5; //Key for encryption 
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            writers = new Dictionary<Thread, BinaryWriter>();
            incomingThread = new Thread(new ThreadStart(RunServer));
            incomingThread.Start();
            connections = new Dictionary<Thread, Socket>();
            numberOfEcryptionsPerUser = new Dictionary<Thread, int>();
            cardAndEncryptions = new Dictionary<string, List<string>>();

            //Deserialize all user registered in the system
            XmlSerializer serializer = new XmlSerializer(typeof(Users));
            using (Stream reader = new FileStream("Users.xml", FileMode.Open))
            {
                registeredUsers = (Users)serializer.Deserialize(reader);
            }

            //Add all deserialized cards to collector of card and encryptions
            foreach (var user in registeredUsers.User)
            {
                cardAndEncryptions.Add(user.Card, new List<string>());
            }
        }

        #region Connection methods
        public void RunServer()
        {
            TcpListener listener;
            //wait for a client connection
            try
            {
                //create TcpListener
                IPAddress local = IPAddress.Parse("127.0.0.1");
                listener = new TcpListener(local, 50000);

                //TcpListener waits for connection request
                listener.Start();

                //Establish connection upon client request
                while (true)
                {
                    DisplayMessage("Waiting for connection...\r\n");
                    //Accept incoming connection
                    Socket connection = listener.AcceptSocket();

                    ThreadPool.QueueUserWorkItem(new WaitCallback(RunInThread), connection);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void RunInThread(object socket)
        {
            Socket connection = (Socket)socket;
            NetworkStream socketStream = new NetworkStream(connection);

            //Create objects for transferring data across stream
            BinaryWriter writer = new BinaryWriter(socketStream);
            BinaryReader reader = new BinaryReader(socketStream);
            writers.Add(Thread.CurrentThread, writer);
            connections.Add(Thread.CurrentThread, connection);
            numberOfEcryptionsPerUser.Add(Thread.CurrentThread, 0);

            lock (this)
            {
                counter++;
                DisplayMessage("Connection " + counter + " received.\r\n");
            }

            //Inform client that connection was successful
            writer.Write("SERVER>>> Connection successful");

            string clientReply = "";
            string username = null;

            //Read string data sent from client
            do
            {
                try
                {
                    clientReply = reader.ReadString();
                    //Check if client request is for authentication
                    if (clientReply.Contains("Credentials"))
                    {
                        username = GetCredentials(clientReply).Item1;
                        string password = GetCredentials(clientReply).Item2;

                        DisplayMessage("\r\nUser " + username + " has entered in his account");

                        //Authenticate
                        if (registeredUsers.User.Any(x => x.Username == username && x.Password == password))
                        {
                            writer.Write("SERVER >>> Successful Authentication");
                        }
                        else
                        {
                            break;
                        }
                    }
                    //Check if the client request is for encryption
                    else if (clientReply.Contains("Encrypt"))
                    {
                        string[] tokens = clientReply.Split(' ');
                        string cardToEncrypt = tokens[1];

                        //Check if the client has permissions to encrypt
                        if (registeredUsers.User.Any(x => x.Username == username && x.Permission == User.Permissions.GUEST))
                        {
                            writer.Write("SERVER >>> Cannot make encryption from guest account.");
                        }
                        //Chack if this card belongs to the client
                        else if (!registeredUsers.User.Any(x => x.Username == username && x.Card == cardToEncrypt))
                        {
                            writer.Write("SERVER >>> Invalid card number");
                        }
                        else
                        {
                            //Check number of encryptions
                            if (numberOfEcryptionsPerUser[Thread.CurrentThread] == 11)
                            {
                                writer.Write("SERVER >>> Cannot make more than 12 encryptions");
                            }
                            else
                            {
                                string encryption = Encrypt(cardToEncrypt, numberOfEcryptionsPerUser[Thread.CurrentThread]); //Create encryption
                                cardAndEncryptions[cardToEncrypt].Add(encryption); //Add encryption to the collector of cards and their encryptions
                                writer.Write(encryption); //Return the encryption to the client
                                numberOfEcryptionsPerUser[Thread.CurrentThread]++;
                            }
                        }
                    }
                    //Check if the client request is for decryption
                    else if (clientReply.Contains("Decrypt"))
                    {
                        string[] tokens = clientReply.Split(' ');
                        string cardToDecrypt = tokens[1];
                        User user = registeredUsers.User.Where(x => x.Username == username).First();

                        //Check user permissions for decrypting this card
                        if (cardAndEncryptions[user.Card].Contains(cardToDecrypt))
                        {
                            writer.Write(user.Card);
                        }
                        else
                        {
                            writer.Write("SERVER >>> Cannot decrypt this card.");
                        }
                    }
                }
                catch (Exception)
                {
                    break;
                }
            } while (clientReply != "CLIENT>>> TERMINATE" && connection.Connected);

            lock (this)
            {
                counter--;
                DisplayMessage("\r\nUser " + username + " terminated connection\r\n");
                writers.Remove(Thread.CurrentThread);
                connections.Remove(Thread.CurrentThread);
            }
            //Close connection
            writer?.Close();
            reader?.Close();
            socketStream?.Close();
            connection?.Close();
        } 
        #endregion

        #region Methods for managing user interface and user actions
        //Display messages in the information logger
        private void DisplayMessage(string message)
        {
            //Check if modifying txtDisplay is not thread safe
            if (!txtDisplay.Dispatcher.CheckAccess())
            {
                // use inherited method Invoke to execute DisplayMessage via a delegate                                       
                txtDisplay.Dispatcher.Invoke(new Action(() => txtDisplay.Text += message));
            }
            else
            {
                txtDisplay.Text += message;
            }
        }

        // close all threads associated with this application
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Environment.Exit(System.Environment.ExitCode);
        }

        //Add new user in the system
        private void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            //Manage GUI
            txtInfo.Text = "CREATE NEW USER ACCOUNT";
            txtDisplay.Visibility = Visibility.Hidden;
            addUserForm.Visibility = Visibility.Visible;
            btnCreateUser.Visibility = Visibility.Visible;
        }

        //Create new user account
        private void btnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            //Check for valid input
            if (!Validation.ValidateCredentials(txtUsername.Text, txtPassword.Password))
            {
                MessageBox.Show("Username and Password must contain only letters, digits and underscore.", "Invalid credentials");
            }
            else if (registeredUsers.User.Any(x => x.Username == txtUsername.Text))
            {
                MessageBox.Show("This usernamne is taken. Please try another one. ", "Invalid credentials");
            }
            else if (cmbPermissions.SelectedItem.ToString().Contains("Guest") && !String.IsNullOrEmpty(txtCardNumber.Text))
            {
                MessageBox.Show("You cannot add bank card number to type of user who is guest.", "Invalid credentials");
            }
            else if (!Validation.ValidateCardNumber(txtCardNumber.Text))
            {
                MessageBox.Show("The number of card you have entered is incorrect. Please check it again", "Invalid card number");
            }
            else
            {
                //Add user to XML file
                User user = new User(txtUsername.Text, txtPassword.Password, cmbPermissions.SelectedItem.ToString(), txtCardNumber.Text);
                XDocument xmlDoc = XDocument.Load("Users.xml");
                xmlDoc.Element("Users").Add(
                    new XElement("User",
                       new XElement("Username", user.Username),
                       new XElement("Password", user.Password),
                       new XElement("Permissions", user.Permission),
                       new XElement("Card", user.Card)
                    ));
                xmlDoc.Save("Users.xml");

                registeredUsers.User.Add(user); //Add user to registered users in the system
                cardAndEncryptions.Add(user.Card, new List<string>()); //Add user card in card collector

                //Manage GUI
                txtInfo.Text = "INFORMATION LOGGER";
                txtDisplay.Visibility = Visibility.Visible;
                addUserForm.Visibility = Visibility.Hidden;
                btnCreateUser.Visibility = Visibility.Hidden;
            }
        }

        //Sort encryptions and cards by encryption and save them to file
        private void btnSortByEncryptionNumber_Click(object sender, RoutedEventArgs e)
        {
            //Manage GUI
            if (addUserForm.Visibility == Visibility.Visible) addUserForm.Visibility = Visibility.Hidden;
            if (btnCreateUser.Visibility == Visibility.Visible) btnCreateUser.Visibility = Visibility.Hidden;
            txtInfo.Text = "INFORMATION LOGGER";
            txtDisplay.Visibility = Visibility.Visible;

            //Helping method for sorting and saving
            WriteSortedByEncryption();
            DisplayMessage("Successfully sorted by encryption\n");
        }

        //Sort encryptions and cards by card number and save them to file
        private void btnSortByCardNumber_Click(object sender, RoutedEventArgs e)
        {
            //Manage GUI
            if (addUserForm.Visibility == Visibility.Visible) addUserForm.Visibility = Visibility.Hidden;
            if (btnCreateUser.Visibility == Visibility.Visible) btnCreateUser.Visibility = Visibility.Hidden;
            txtInfo.Text = "INFORMATION LOGGER";
            txtDisplay.Visibility = Visibility.Visible;

            //Helping method for sorting and saving
            WriteSortedByCardNumber();
            DisplayMessage("Successfully sorted by card number\n");
        }
        #endregion

        #region Helping methods
        //Helping method for getting user credentials
        private (string, string) GetCredentials(string reply)
        {
            string[] tokens = reply.Split(' ');
            string username = tokens[1];
            string password = tokens[2];
            return (username, password);
        }

        //Helping method for encrypting 
        private string Encrypt(string card, int numberOfEcryptions)
        {
            CardManipulation encryption;
            if (numberOfEcryptions == 0)
            {
                encryption = new CardManipulation(KEY);
                return encryption.Encrypt(card);

            }
            else
            {
                encryption = new CardManipulation(KEY + numberOfEcryptions);
                return encryption.Encrypt(card);
            }
        }

        //Helping method for sorting by encryption and saving
        private void WriteSortedByEncryption()
        {

            var tupleList = new List<(string, string)>();
            foreach (var item in cardAndEncryptions)
            {
                string card = item.Key;
                foreach (var encryption in cardAndEncryptions[card])
                {
                    tupleList.Add((card, encryption));
                }
            }
            var sortedTuples = tupleList.OrderBy((x => x.Item2));
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("./SortedByCardEncryption.txt"))
            {
                file.WriteLine("Sorted By Encryption records:\n");
                foreach (var record in sortedTuples)
                {
                    file.WriteLine(record);
                }
            }
        }

        //Helping method for sorting by card number and saving
        private void WriteSortedByCardNumber()
        {
            var tupleList = new List<(string, string)>();
            foreach (var item in cardAndEncryptions)
            {
                string card = item.Key;
                foreach (var encryption in cardAndEncryptions[card])
                {
                    tupleList.Add((card, encryption));
                }
            }
            var sortedTuples = tupleList.OrderBy((x => x.Item1));
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("./SortedByCardNumber.txt"))
            {
                file.WriteLine("Sorted By Card Number records:\n");
                foreach (var record in sortedTuples)
                {
                    file.WriteLine(record);
                }
            }
        } 
        #endregion
    }
}
