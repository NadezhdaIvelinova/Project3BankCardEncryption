﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NetworkStream output; //stream for receiving data
        private BinaryWriter writer;
        private BinaryReader reader;
        private Thread readThread;
        private string message = "";
        public MainWindow()
        {
            InitializeComponent();
            readThread = new Thread(new ThreadStart(RunClient));
            readThread.Start();
        }

        public void RunClient()
        {
            TcpClient client = null;

            //instantiate TcpClient for sending data to server
            try
            {
                DisplayMessage("Attempting connection\r\n");

                //Create TcpClient for sending data to server
                client = new TcpClient();
                client.Connect("127.0.0.1", 50000);

                //Get NetworkStream associated with TctClient
                output = client.GetStream();

                //Create objects for writing and reading across stream
                writer = new BinaryWriter(output);
                reader = new BinaryReader(output);
                

                // loop until server signals termination
                do
                {
                    // Processing phase
                    try
                    {
                        // read message from server        
                        message = reader.ReadString();
                        if(message.Equals("SERVER >>> Successful Authentication"))
                        {
                            HideLoginPanels();
                            ShowOperationsPanel();
                            
                        }
                        if(message.Equals("SERVER >>> Cannot make encryption from guest account."))
                        {
                            MessageBox.Show("Cannot make encryptions from guest account.", "Unauthorized Operation");
                        }
                        if(message.Equals("SERVER >>> Invalid card number"))
                        {
                            MessageBox.Show("The card number you have entered is incorrect. Check if this is your card number.", "Invalid Card Number");
                            
                        }
                        if (message.Equals("SERVER >>> Cannot make more than 12 encryptions"))
                        {
                            MessageBox.Show("You cannot make more than 12 ecryptions per card.", "Invalid Operation");

                        }
                        if(message.Equals("SERVER >>> Cannot decrypt this card."))
                        {
                            MessageBox.Show("You have no permissions to decrypt this card. Check if this is your card encryption.", "Invalid Operation");
                        }
                        if (Regex.IsMatch(message, @"^[0-9]+$"))
                        {
                            DisplayOutput(message);
                        }
                       
                        DisplayMessage("\r\n" + message);
                    } 
                    catch (Exception)
                    {
                        System.Environment.Exit(System.Environment.ExitCode);
                    } 
                } while (message != "SERVER>>> TERMINATE");
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "Connection Error",
                  MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                //Close connection
                writer?.Close();
                reader?.Close();
                output?.Close();
                client?.Close();

                System.Environment.Exit(System.Environment.ExitCode);
            }
        }

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

        private void HideLoginPanels()
        {
            //Check if modifying txtDisplay is not thread safe
            if (!LoginPanel.Dispatcher.CheckAccess())
            {
                // use inherited method Invoke to execute DisplayMessage via a delegate                                       
                LoginPanel.Dispatcher.Invoke(new Action(() => LoginPanel.Visibility = Visibility.Collapsed));
            }
            else
            {
                LoginPanel.Visibility = Visibility.Collapsed;
            }
        }
        private void ShowOperationsPanel()
        {
            //Check if modifying txtDisplay is not thread safe
            if (!OperationsPanel.Dispatcher.CheckAccess())
            {
                // use inherited method Invoke to execute DisplayMessage via a delegate                                       
                OperationsPanel.Dispatcher.Invoke(new Action(() => OperationsPanel.Visibility = Visibility.Visible));
            }
            else
            {
                OperationsPanel.Visibility = Visibility.Visible;
            }
        }

        private void DisplayOutput(string message)
        {
            //Check if modifying txtDisplay is not thread safe
            if (!txtOutput.Dispatcher.CheckAccess())
            {
                // use inherited method Invoke to execute DisplayMessage via a delegate                                       
                txtOutput.Dispatcher.Invoke(new Action(() => txtOutput.Text = message));
            }
            else
            {
                txtOutput.Text = message;
            }
        }

        // close all threads associated with this application
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Environment.Exit(System.Environment.ExitCode);
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                writer.Write("Credentials" + " " + txtUsername.Text + " " + txtPassword.Password);
                txtPassword.Clear();
                txtUsername.Clear();
            }
            catch (SocketException)
            {
                MessageBox.Show("Error writing object");
            }
        }

        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(cmbOperation.SelectedItem.ToString().Contains("Encrypt"))
                {
                    writer.Write("Encrypt" + " " + txtCardNumber.Text);
                }
                else
                {
                    writer.Write("Decrypt" + " " + txtCardNumber.Text);
                }               
            }
            catch (SocketException)
            {
                MessageBox.Show("Error writing object");
            }
        }

 
        private void btnNewOperation_Click(object sender, RoutedEventArgs e)
        {
            txtCardNumber.Clear();
            txtOutput.Clear();
        }
    }
}
