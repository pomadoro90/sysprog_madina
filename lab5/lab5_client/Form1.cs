using System;
using System.Text;
using System.Windows.Forms;

namespace Salakhova_Sharp
{
    public partial class Form1 : Form
    {
        const int MT_INIT = 0;
        const int MT_EXIT = 1;
        const int MT_GETDATA = 2;
        const int MT_DATA = 3;
        const int MT_NODATA = 4;
        const int MT_CONFIRM = 5;

        private System.Windows.Forms.Timer pollTimer;

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;

            btnConnect.Text = "Connect";
            btnDisconnect.Text = "Disconnect";
            btnSend.Text = "Send Message";
            this.Text = "Message Client";

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
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                Message.send(MessageRecipients.MR_BROKER, MessageTypes.MT_INIT);
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

            int targetId = ((RecipientItem)comboRecipient.SelectedItem).Id;
            Message.send(targetId, MessageTypes.MT_DATA, textBoxMessage.Text);

            txtOutput.AppendText($"[You -> {comboRecipient.SelectedItem}]: {textBoxMessage.Text}\r\n");
            textBoxMessage.Clear();
        }

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var m = Message.send(MessageRecipients.MR_BROKER, MessageTypes.MT_GETDATA);
                if (m.header.type == MessageTypes.MT_DATA)
                {
                    txtOutput.AppendText($"[From Client #{m.header.from}]: {m.data}\r\n");
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
            try
            {
                Message.send(MessageRecipients.MR_BROKER, MessageTypes.MT_EXIT);
            }
            catch { }

            pollTimer.Stop();
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