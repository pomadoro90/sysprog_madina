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
                comboRecipient.Items.Add(new RecipientItem("All (Broadcast)", (int)MessageRecipients.MR_ALL));
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
                    if (msg.header.type == (int)MessageTypes.MT_CONFIRM)
                    {
                        ParseClientList(msg.data);
                    }
                    else if (msg.header.type == (int)MessageTypes.MT_DATA)
                    {
                        txtOutput.AppendText($"[From Client #{msg.header.from}]: {msg.data}\r\n");
                    }
                    else if (msg.header.type == (int)MessageTypes.MT_INIT)
                    {
                        // Server confirmed our connection — ignore, list comes via MT_CONFIRM
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

        private void ParseClientList(string payload)
        {
            int prevId = -999;
            if (comboRecipient.SelectedItem != null)
                prevId = ((RecipientItem)comboRecipient.SelectedItem).Id;

            comboRecipient.Items.Clear();
            comboRecipient.Items.Add(new RecipientItem("All (Broadcast)", (int)MessageRecipients.MR_ALL));

            if (!string.IsNullOrEmpty(payload))
            {
                string[] clients = payload.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var c in clients)
                {
                    string[] parts = c.Split(':');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int id))
                    {
                        if (id != client.ClientId) // Don't show self
                        {
                            string name = parts.Length > 1 ? parts[1] : $"Client #{id}";
                            comboRecipient.Items.Add(new RecipientItem($"{name} (#{id})", id));
                        }
                    }
                }
            }

            // Restore previous selection if possible
            bool found = false;
            foreach (RecipientItem item in comboRecipient.Items)
            {
                if (item.Id == prevId) { comboRecipient.SelectedItem = item; found = true; break; }
            }
            if (!found && comboRecipient.Items.Count > 0) comboRecipient.SelectedIndex = 0;
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