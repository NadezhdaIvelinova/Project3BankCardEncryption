using System;
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
        private Thread incomingThread; //Thread for processing incoming queries
        private int counter;
        private Dictionary<Thread, Socket> connections;
        private Dictionary<Thread, BinaryWriter> writers;
        private Users registeredUsers;

        public MainWindow()
        {
            InitializeComponent();

            writers = new Dictionary<Thread, BinaryWriter>();
            incomingThread = new Thread(new ThreadStart(RunServer));
            incomingThread.Start();
            connections = new Dictionary<Thread, Socket>();

            //Deserialize all user registered in the system
            XmlSerializer serializer = new XmlSerializer(typeof(Users));           
            using (Stream reader = new FileStream("Users.xml", FileMode.Open))
            {
                registeredUsers = (Users)serializer.Deserialize(reader);
            }
        }

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
                while(true)
                {
                    DisplayMessage("Waiting for connection...\r\n");
                    //Accept incoming connection
                    Socket connection = listener.AcceptSocket();

                    ThreadPool.QueueUserWorkItem(new WaitCallback(RunInThread), connection);
                }                
            }
            catch(Exception exception)
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

            lock(this)
            {
                counter++;
                DisplayMessage("Connection " + counter + " received.\r\n");
            }

            //Inform client that connection was successful
            writer.Write("SERVER>>> Connection successful");

            string clientReply = "";

            //Read string data sent from client
            do
            {
                try
                {
                    clientReply = reader.ReadString();

                    DisplayMessage("\r\n" + clientReply);
                }
                catch (Exception)
                {
                    break;
                }
            } while (clientReply != "CLIENT>>> TERMINATE" && connection.Connected);
            lock(this)
            {
                counter--;
                DisplayMessage("\r\nUser terminated connection\r\n");
                writers.Remove(Thread.CurrentThread);
                connections.Remove(Thread.CurrentThread);
            }
            //Close connection
            writer?.Close();
            reader?.Close();
            socketStream?.Close();
            connection?.Close();
        }

        private void DisplayMessage(string message)
        {
            //Check if modifying txtDisplay is not thread safe
            if(!txtDisplay.Dispatcher.CheckAccess())
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

        private void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (addCardToUser.Visibility == Visibility.Visible) addCardToUser.Visibility = Visibility.Hidden;
            if (btnAddCardToUser.Visibility == Visibility.Visible) btnAddCardToUser.Visibility = Visibility.Hidden;
            txtInfo.Text = "CREATE NEW USER ACCOUNT"; 
            txtDisplay.Visibility = Visibility.Hidden;
            addUserForm.Visibility = Visibility.Visible;
            btnCreateUser.Visibility = Visibility.Visible;
        }

        private void btnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            if(Validation.ValidateCredentials(txtUsername.Text, txtPassword.Password))
            {
                
                if(cmbPermissions.SelectedItem.ToString().Contains("Guest") && !String.IsNullOrEmpty(txtCardNumber.Text))
                {
                    MessageBox.Show("You cannot add bank card number to type of user who is guest", "Invalid credentials");
                }
                else if(registeredUsers.User.Any(x => x.Username == txtUsername.Text))
                {
                    MessageBox.Show("This usernamne is taken. Please try another one. ", "Invalid credentials");
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

                    registeredUsers.User.Add(user);

                    txtInfo.Text = "INFORMATION LOGGER";
                    txtDisplay.Visibility = Visibility.Visible;
                    addUserForm.Visibility = Visibility.Hidden;
                    btnCreateUser.Visibility = Visibility.Hidden;
                }
              
            }
            else
            {
                MessageBox.Show("Username and Password must contain only letters, digits and underscore.", "Invalid credentials");
            }

                    
        }

        private void btnAddCardToUser_Click(object sender, RoutedEventArgs e)
        {
            txtInfo.Text = "INFORMATION LOGGER";
            txtDisplay.Visibility = Visibility.Visible;
            addCardToUser.Visibility = Visibility.Hidden;
            btnAddCardToUser.Visibility = Visibility.Hidden;
        }

        private void btnAddCard_Click(object sender, RoutedEventArgs e)
        {
            if(addUserForm.Visibility == Visibility.Visible) addUserForm.Visibility = Visibility.Hidden;
            if (btnCreateUser.Visibility == Visibility.Visible) btnCreateUser.Visibility = Visibility.Hidden;
            txtInfo.Text = "ADD CARD TO USER";
            txtDisplay.Visibility = Visibility.Hidden;
            addCardToUser.Visibility = Visibility.Visible;
            btnAddCardToUser.Visibility = Visibility.Visible;

        }

        private void btnSortByEncryptionNumber_Click(object sender, RoutedEventArgs e)
        {
            if (addUserForm.Visibility == Visibility.Visible) addUserForm.Visibility = Visibility.Hidden;
            if (btnCreateUser.Visibility == Visibility.Visible) btnCreateUser.Visibility = Visibility.Hidden;
            if (addCardToUser.Visibility == Visibility.Visible) addCardToUser.Visibility = Visibility.Hidden;
            if (btnAddCardToUser.Visibility == Visibility.Visible) btnAddCardToUser.Visibility = Visibility.Hidden;
            txtInfo.Text = "INFORMATION LOGGER";
            txtDisplay.Visibility = Visibility.Visible;
        }

        private void btnSortByCardNumber_Click(object sender, RoutedEventArgs e)
        {
            if (addUserForm.Visibility == Visibility.Visible) addUserForm.Visibility = Visibility.Hidden;
            if (btnCreateUser.Visibility == Visibility.Visible) btnCreateUser.Visibility = Visibility.Hidden;
            if (addCardToUser.Visibility == Visibility.Visible) addCardToUser.Visibility = Visibility.Hidden;
            if (btnAddCardToUser.Visibility == Visibility.Visible) btnAddCardToUser.Visibility = Visibility.Hidden;
            txtInfo.Text = "INFORMATION LOGGER";
            txtDisplay.Visibility = Visibility.Visible;        
        }
    }
}
