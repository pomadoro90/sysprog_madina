namespace Kleimenov_Sharp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonStart = new Button();
            buttonStop = new Button();
            buttonSend = new Button();
            textBoxInput = new TextBox();
            listBox1 = new ListBox();
            numericUpDownCounter = new NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)numericUpDownCounter).BeginInit();
            SuspendLayout();
            // 
            // buttonStart
            // 
            buttonStart.Location = new Point(65, 135);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(109, 33);
            buttonStart.TabIndex = 0;
            buttonStart.Text = "Start";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += buttonStart_Click;
            // 
            // buttonStop
            // 
            buttonStop.Location = new Point(65, 183);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(109, 34);
            buttonStop.TabIndex = 1;
            buttonStop.Text = "Stop";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += buttonStop_Click;
            // 
            // buttonSend
            // 
            buttonSend.Location = new Point(65, 233);
            buttonSend.Name = "buttonSend";
            buttonSend.Size = new Size(109, 34);
            buttonSend.TabIndex = 2;
            buttonSend.Text = "Send";
            buttonSend.UseVisualStyleBackColor = true;
            // 
            // textBoxInput
            // 
            textBoxInput.Location = new Point(217, 212);
            textBoxInput.Name = "textBoxInput";
            textBoxInput.Size = new Size(205, 27);
            textBoxInput.TabIndex = 4;
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.Location = new Point(463, 12);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(311, 404);
            listBox1.TabIndex = 5;
            // 
            // numericUpDownCounter
            // 
            numericUpDownCounter.Location = new Point(279, 164);
            numericUpDownCounter.Name = "numericUpDownCounter";
            numericUpDownCounter.Size = new Size(61, 27);
            numericUpDownCounter.TabIndex = 6;
            numericUpDownCounter.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(numericUpDownCounter);
            Controls.Add(listBox1);
            Controls.Add(textBoxInput);
            Controls.Add(buttonSend);
            Controls.Add(buttonStop);
            Controls.Add(buttonStart);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)numericUpDownCounter).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonStart;
        private Button buttonStop;
        private Button buttonSend;
        private TextBox textBoxInput;
        private ListBox listBox1;
        private NumericUpDown numericUpDownCounter;
    }
}
