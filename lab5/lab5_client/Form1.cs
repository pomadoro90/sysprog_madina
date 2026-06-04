using System;
using System.Linq;
using System.Windows.Forms;

namespace Salakhova_Sharp
{
    public partial class Form1 : Form
    {
        private SalakhovaSocketClient client;
        private System.Windows.Forms.Timer pollTimer;

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;

            pollTimer = new System.Windows.Forms.Timer();
            pollTimer.Interval = 100;
            pollTimer.Tick += PollTimer_Tick;

            ToggleUi(false);
        }

        private void ToggleUi(bool isConnected)
        {
            btnConnect.Enabled = !isConnected;
            btnDisconnect.Enabled = isConnected;
            btnSend.Enabled = isConnected;
            comboRecipient.Enabled = isConnected;
            textBoxMessage.Enabled = isConnected;
            txtHost.Enabled = !isConnected;
            numericPort.Enabled = !isConnected;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                client = new SalakhovaSocketClient();
                client.Connect(txtHost.Text, (int)numericPort.Value);

                comboRecipient.Items.Clear();
                comboRecipient.Items.Add(new RecipientItem("All (50)", (int)MessageRecipients.MR_ALL));
                comboRecipient.SelectedIndex = 0;

                pollTimer.Start();
                ToggleUi(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot connect to server!\n" + ex.Message, "Connection Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            DisconnectClient();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxMessage.Text)) return;
            if (comboRecipient.SelectedItem == null) return;

            try
            {
                int targetId = ((RecipientItem)comboRecipient.SelectedItem).Id;
                client.Send(targetId, MessageTypes.MT_DATA, textBoxMessage.Text);

                txtOutput.AppendText($"[You -> {comboRecipient.SelectedItem}]: {textBoxMessage.Text}\r\n");
                textBoxMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Send error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            if (client == null || !client.IsConnected)
            {
                pollTimer.Stop();
                ToggleUi(false);
                return;
            }

            try
            {
                while (client.TryReceive(out Message msg))
                {
                    if (msg.header.type == (int)MessageTypes.MT_INIT)
                    {
                        // New client joined — add to recipient list
                        if (msg.header.from >= (int)MessageRecipients.MR_USER
                            && msg.header.from != client.ClientId)
                        {
                            bool exists = comboRecipient.Items.Cast<RecipientItem>()
                                .Any(item => item.Id == msg.header.from);
                            if (!exists)
                            {
                                string name = !string.IsNullOrEmpty(msg.data)
                                    ? msg.data : $"Client #{msg.header.from}";
                                comboRecipient.Items.Add(new RecipientItem(name, msg.header.from));
                            }
                        }
                    }
                    else if (msg.header.type == (int)MessageTypes.MT_DATA)
                    {
                        txtOutput.AppendText($"[From Client #{msg.header.from}]: {msg.data}\r\n");

                        // Auto-add sender to recipient list if not there yet
                        if (msg.header.from >= (int)MessageRecipients.MR_USER)
                        {
                            bool exists = comboRecipient.Items.Cast<RecipientItem>()
                                .Any(item => item.Id == msg.header.from);
                            if (!exists)
                            {
                                comboRecipient.Items.Add(new RecipientItem($"Client #{msg.header.from}", msg.header.from));
                            }
                        }
                    }
                }
            }
            catch
            {
                pollTimer.Stop();
                ToggleUi(false);
                MessageBox.Show("Connection to server lost!", "Disconnected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DisconnectClient()
        {
            pollTimer.Stop();

            if (client != null)
            {
                try
                {
                    client.Disconnect();
                }
                catch { }
                client = null;
            }

            ToggleUi(false);
            comboRecipient.Items.Clear();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectClient();
        }
    }

    public class RecipientItem
    {
        public string Name { get; }
        public int Id { get; }
        public RecipientItem(string name, int id) { Name = name; Id = id; }
        public override string ToString() => Name;
    }
}
