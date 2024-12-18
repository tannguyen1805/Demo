using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatAppClient
{
    public partial class Form1 : Form
    {
        private string ipAddress;
        private int portNumber;
        private TcpClient tcpClient;
        private string userName;
        private NetworkStream networkStream;
        private volatile bool isConnected;

        public Form1()
        {
            InitializeComponent();
        }

        private async void buttonConnect_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                ipAddress = textBoxIp.Text;
                portNumber = int.Parse(textBoxPort.Text);
                userName = textBoxUserName.Text;

                try
                {
                    tcpClient = new TcpClient(ipAddress, portNumber);
                    networkStream = tcpClient.GetStream();
                    isConnected = true;

                    // Gửi thông báo kết nối
                    await SendMessageAsync($"{userName} has connected.");

                    // Cập nhật UI
                    textBoxIp.Enabled = false;
                    textBoxUserName.Enabled = false;
                    textBoxPort.Enabled = false;
                    button1.Enabled = true;
                    buttonConnect.Enabled = false;
                    buttonSend.Enabled = true;

                    // Bắt đầu nhận dữ liệu từ server
                    _ = ReceiveDataAsync();
                }
                catch (Exception ex)
                {
                    textBoxChat.AppendText($"Failed to connect: {ex.Message}{Environment.NewLine}");
                }
            }
            else
            {
                textBoxChat.AppendText("Already Connected" + Environment.NewLine);
            }
        }

        private async Task ReceiveDataAsync()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (isConnected)
                {
                    int byteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    if (byteCount == 0)
                    {
                        break; // Server đóng kết nối
                    }

                    string receivedMessage = Encoding.ASCII.GetString(buffer, 0, byteCount);

                    // Cập nhật giao diện
                    textBoxChat.Invoke(new Action(() =>
                    {
                        textBoxChat.AppendText(receivedMessage + Environment.NewLine);
                    }));
                }
            }
            catch (Exception ex)
            {
                textBoxChat.Invoke(new Action(() =>
                {
                    textBoxChat.AppendText($"Error receiving data: {ex.Message}{Environment.NewLine}");
                }));
            }
            finally
            {
                DisconnectAndClose();
            }
        }

        private async Task SendMessageAsync(string message)
        {
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                await networkStream.WriteAsync(buffer, 0, buffer.Length);
                await networkStream.FlushAsync();
            }
            catch (Exception ex)
            {
                textBoxChat.AppendText($"Error sending message: {ex.Message}{Environment.NewLine}");
            }
        }

        private async void buttonSend_Click(object sender, EventArgs e)
        {
            string message = $"{userName}: {textBoxMessage.Text}";
            await SendMessageAsync(message);
            textBoxMessage.Text = string.Empty;
        }

        private void DisconnectAndClose()
        {
            if (isConnected)
            {
                isConnected = false;
                try
                {
                    networkStream?.Close();
                    tcpClient?.Close();
                    textBoxChat.AppendText("Disconnected from server." + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    textBoxChat.AppendText($"Error during disconnect: {ex.Message}{Environment.NewLine}");
                }

                // Cập nhật giao diện
                textBoxIp.Enabled = true;
                textBoxUserName.Enabled = true;
                textBoxPort.Enabled = true;
                button1.Enabled = false;
                buttonConnect.Enabled = true;
                buttonSend.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DisconnectAndClose();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            DisconnectAndClose();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void textBoxChat_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
