using System.Diagnostics;

namespace Kleimenov_Sharp
{
    public partial class Form1 : Form
    {
        Process? childProcess = null;

        System.Threading.EventWaitHandle startEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "StartEvent");
        System.Threading.EventWaitHandle stopEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "StopEvent");
        System.Threading.EventWaitHandle confirmEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "ConfirmEvent");
        System.Threading.EventWaitHandle quitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "QuitEvent");

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!(childProcess == null || childProcess.HasExited))
            {
                quitEvent.Set();
                confirmEvent.WaitOne(100);
                if (!childProcess.HasExited)
                {
                    childProcess.Kill();
                }
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (childProcess == null || childProcess.HasExited)
            {
                childProcess = Process.Start("Kleimenov_CPP.exe");

                listBox1.Items.Clear();
                listBox1.Items.Add("Все потоки");
                listBox1.Items.Add("Главный поток");
            }
            else
            {
                int threadNumbers = (int)numericUpDownCounter.Value;
                
                for (int i = 0; i < threadNumbers; i++)
                {
                    startEvent.Set();
                    confirmEvent.WaitOne();

                    listBox1.Items.Add(i.ToString());
                }
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (!(childProcess == null || childProcess.HasExited))
            {
                stopEvent.Set();
                confirmEvent.WaitOne();

                if (listBox1.Items.Count > 2)
                {
                    listBox1.Items.RemoveAt(listBox1.Items.Count - 1);
                }
            }
        }
    }
}
