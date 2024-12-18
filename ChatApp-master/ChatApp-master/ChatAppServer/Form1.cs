using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatAppServer
{
    public partial class Form1 : Form
    {
        private static readonly object clientLock = new object();
        private static readonly Dictionary<int, TcpClient> connectedClients = new Dictionary<int, TcpClient>();
        private volatile bool isServerRunning;
        private TcpListener serverListener;
        private int clientCounter;

        public Form1()
        {
            InitializeComponent();
        }

        private async void buttonConnect_Click(object sender, EventArgs e)
        {
            if (!isServerRunning)
            {
                clientCounter = 1;

                string serverIpAddress = textBoxIP.Text;
                int serverPortNumber = Int32.Parse(textBoxPort.Text);

                serverListener = new TcpListener(IPAddress.Parse(serverIpAddress), serverPortNumber);
                isServerRunning = true;

                try
                {
                    serverListener.Start();
                    textBoxChat.Text += @"Server started!" + Environment.NewLine;

                    buttonConnect.Enabled = false;
                    textBoxIP.Enabled = false;
                    textBoxPort.Enabled = false;

                    // Start accepting clients asynchronously
                    _ = AcceptClientsAsync();
                }
                catch
                {
                    textBoxChat.Text += @"Failed to start the server, please try again!" + Environment.NewLine;
                }
            }
            else
            {
                textBoxChat.AppendText(@"Server is already running." + Environment.NewLine);
            }
        }

        private async Task AcceptClientsAsync()
        {
            while (isServerRunning)
            {
                try
                {
                    TcpClient newClient = await serverListener.AcceptTcpClientAsync();
                    lock (clientLock)
                    {
                        connectedClients.Add(clientCounter, newClient);
                    }

                    int clientId = clientCounter;
                    clientCounter++;

                    textBoxChat.Invoke(new Action(() =>
                    {
                        textBoxChat.AppendText($"Client {clientId} connected!" + Environment.NewLine);
                    }));

                    _ = HandleClientAsync(clientId, newClient);
                }
                catch (Exception ex)
                {
                    textBoxChat.Invoke(new Action(() =>
                    {
                        textBoxChat.AppendText($"Error accepting client: {ex.Message}" + Environment.NewLine);
                    }));
                }
            }
        }

        private async Task HandleClientAsync(int clientId, TcpClient client)
        {
            try
            {
                NetworkStream clientStream = client.GetStream();
                byte[] buffer = new byte[1024];

                while (true)
                {
                    int receivedBytes = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                    if (receivedBytes == 0)
                    {
                        break; // Client disconnected
                    }

                    string receivedMessage = Encoding.ASCII.GetString(buffer, 0, receivedBytes);

                    textBoxChat.Invoke(new Action(() =>
                    {
                        textBoxChat.AppendText($"Client {clientId}: {receivedMessage}" + Environment.NewLine);
                    }));

                    // Broadcast the message to all clients
                    await BroadcastMessageAsync(receivedMessage);
                }
            }
            catch (Exception ex)
            {
                textBoxChat.Invoke(new Action(() =>
                {
                    textBoxChat.AppendText($"Error handling client {clientId}: {ex.Message}" + Environment.NewLine);
                }));
            }
            finally
            {
                lock (clientLock)
                {
                    connectedClients.Remove(clientId);
                }

                client.Client.Shutdown(SocketShutdown.Both);
                client.Close();

                textBoxChat.Invoke(new Action(() =>
                {
                    textBoxChat.AppendText($"Client {clientId} disconnected." + Environment.NewLine);
                }));
            }
        }

        private async Task BroadcastMessageAsync(string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message + Environment.NewLine);

            lock (clientLock)
            {
                foreach (var client in connectedClients.Values)
                {
                    try
                    {
                        NetworkStream clientStream = client.GetStream();
                        clientStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    catch
                    {
                        // Ignore failed clients
                    }
                }
            }
        }
    }
}
