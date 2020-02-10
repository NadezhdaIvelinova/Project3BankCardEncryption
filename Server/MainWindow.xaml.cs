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
        public MainWindow()
        {
            InitializeComponent();

            writers = new Dictionary<Thread, BinaryWriter>();
            incomingThread = new Thread(new ThreadStart(RunServer));
            incomingThread.Start();
            connections = new Dictionary<Thread, Socket>();
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
    }
}
