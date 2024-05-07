using SysBot.Pokemon.WinForms.Properties;
using System.Drawing;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms
{
    partial class Main
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
            TC_Main = new TabControl();
            Tab_Bots = new TabPage();
            comboBox2 = new ComboBox();
            comboBox1 = new ComboBox();
            CB_Protocol = new ComboBox();
            FLP_Bots = new FlowLayoutPanel();
            TB_IP = new TextBox();
            CB_Routine = new ComboBox();
            NUD_Port = new NumericUpDown();
            B_New = new Button();
            Tab_Hub = new TabPage();
            PG_Hub = new PropertyGrid();
            Tab_Logs = new TabPage();
            RTB_Logs = new RichTextBox();
            B_Stop = new Button();
            B_Start = new Button();
            B_RebootStop = new Button();
            ButtonPanel = new Panel();
            TC_Main.SuspendLayout();
            Tab_Bots.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)NUD_Port).BeginInit();
            Tab_Hub.SuspendLayout();
            Tab_Logs.SuspendLayout();
            ButtonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // TC_Main
            // 
            TC_Main.Appearance = TabAppearance.Buttons;
            TC_Main.Controls.Add(Tab_Bots);
            TC_Main.Controls.Add(Tab_Hub);
            TC_Main.Controls.Add(Tab_Logs);
            TC_Main.Dock = DockStyle.Fill;
            TC_Main.Location = new Point(0, 0);
            TC_Main.Margin = new Padding(0);
            TC_Main.Name = "TC_Main";
            TC_Main.Padding = new Point(20, 7);
            TC_Main.SelectedIndex = 0;
            TC_Main.Size = new Size(776, 435);
            TC_Main.TabIndex = 3;
            // 
            // Tab_Bots
            // 
            Tab_Bots.Controls.Add(comboBox2);
            Tab_Bots.Controls.Add(comboBox1);
            Tab_Bots.Controls.Add(CB_Protocol);
            Tab_Bots.Controls.Add(FLP_Bots);
            Tab_Bots.Controls.Add(TB_IP);
            Tab_Bots.Controls.Add(CB_Routine);
            Tab_Bots.Controls.Add(NUD_Port);
            Tab_Bots.Controls.Add(B_New);
            Tab_Bots.Location = new Point(4, 38);
            Tab_Bots.Name = "Tab_Bots";
            Tab_Bots.Size = new Size(768, 393);
            Tab_Bots.TabIndex = 0;
            Tab_Bots.Text = "Bots";
            Tab_Bots.UseVisualStyleBackColor = true;
            // 
            // comboBox2
            // 
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.FormattingEnabled = true;
            comboBox2.Location = new Point(626, 7);
            comboBox2.Margin = new Padding(5, 4, 5, 4);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(130, 26);
            comboBox2.TabIndex = 12;
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(541, 7);
            comboBox1.Margin = new Padding(5, 4, 5, 4);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(76, 26);
            comboBox1.TabIndex = 11;
            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            // 
            // CB_Protocol
            // 
            CB_Protocol.DropDownStyle = ComboBoxStyle.DropDownList;
            CB_Protocol.FormattingEnabled = true;
            CB_Protocol.Location = new Point(330, 7);
            CB_Protocol.Margin = new Padding(5, 4, 5, 4);
            CB_Protocol.Name = "CB_Protocol";
            CB_Protocol.Size = new Size(76, 26);
            CB_Protocol.TabIndex = 10;
            CB_Protocol.SelectedIndexChanged += CB_Protocol_SelectedIndexChanged;
            // 
            // FLP_Bots
            // 
            FLP_Bots.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            FLP_Bots.BackColor = Color.Transparent;
            FLP_Bots.BackgroundImageLayout = ImageLayout.Center;
            FLP_Bots.BorderStyle = BorderStyle.FixedSingle;
            FLP_Bots.Location = new Point(0, 44);
            FLP_Bots.Margin = new Padding(0);
            FLP_Bots.Name = "FLP_Bots";
            FLP_Bots.Size = new Size(768, 365);
            FLP_Bots.TabIndex = 9;
            FLP_Bots.Resize += FLP_Bots_Resize;
            // 
            // TB_IP
            // 
            TB_IP.BorderStyle = BorderStyle.FixedSingle;
            TB_IP.Font = new Font("Calibri", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            TB_IP.Location = new Point(85, 10);
            TB_IP.Margin = new Padding(5, 4, 5, 4);
            TB_IP.Name = "TB_IP";
            TB_IP.Size = new Size(153, 23);
            TB_IP.TabIndex = 8;
            TB_IP.Text = "192.168.0.1";
            // 
            // CB_Routine
            // 
            CB_Routine.DropDownStyle = ComboBoxStyle.DropDownList;
            CB_Routine.FormattingEnabled = true;
            CB_Routine.Location = new Point(416, 7);
            CB_Routine.Margin = new Padding(5, 4, 5, 4);
            CB_Routine.Name = "CB_Routine";
            CB_Routine.Size = new Size(115, 26);
            CB_Routine.TabIndex = 7;
            // 
            // NUD_Port
            // 
            NUD_Port.Font = new Font("Calibri", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            NUD_Port.Location = new Point(247, 10);
            NUD_Port.Margin = new Padding(4, 3, 4, 3);
            NUD_Port.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            NUD_Port.Name = "NUD_Port";
            NUD_Port.Size = new Size(74, 23);
            NUD_Port.TabIndex = 6;
            NUD_Port.Value = new decimal(new int[] { 6000, 0, 0, 0 });
            // 
            // B_New
            // 
            B_New.FlatStyle = FlatStyle.Flat;
            B_New.Location = new Point(5, 4);
            B_New.Margin = new Padding(5, 4, 5, 4);
            B_New.Name = "B_New";
            B_New.Size = new Size(72, 37);
            B_New.TabIndex = 0;
            B_New.Text = "Add";
            B_New.UseVisualStyleBackColor = true;
            B_New.Click += B_New_Click;
            // 
            // Tab_Hub
            // 
            Tab_Hub.Controls.Add(PG_Hub);
            Tab_Hub.Location = new Point(4, 35);
            Tab_Hub.Margin = new Padding(5, 4, 5, 4);
            Tab_Hub.Name = "Tab_Hub";
            Tab_Hub.Padding = new Padding(5, 4, 5, 4);
            Tab_Hub.Size = new Size(768, 396);
            Tab_Hub.TabIndex = 2;
            Tab_Hub.Text = "Hub";
            Tab_Hub.UseVisualStyleBackColor = true;
            // 
            // PG_Hub
            // 
            PG_Hub.BackColor = SystemColors.Desktop;
            PG_Hub.Dock = DockStyle.Fill;
            PG_Hub.Font = new Font("Calibri", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            PG_Hub.Location = new Point(5, 4);
            PG_Hub.Margin = new Padding(4, 3, 4, 3);
            PG_Hub.Name = "PG_Hub";
            PG_Hub.PropertySort = PropertySort.Categorized;
            PG_Hub.Size = new Size(758, 388);
            PG_Hub.TabIndex = 0;
            // 
            // Tab_Logs
            // 
            Tab_Logs.Controls.Add(RTB_Logs);
            Tab_Logs.Location = new Point(4, 35);
            Tab_Logs.Name = "Tab_Logs";
            Tab_Logs.Size = new Size(768, 396);
            Tab_Logs.TabIndex = 1;
            Tab_Logs.Text = "Logs";
            Tab_Logs.UseVisualStyleBackColor = true;
            // 
            // RTB_Logs
            // 
            RTB_Logs.BackColor = SystemColors.AppWorkspace;
            RTB_Logs.BorderStyle = BorderStyle.None;
            RTB_Logs.Dock = DockStyle.Fill;
            RTB_Logs.Location = new Point(0, 0);
            RTB_Logs.Margin = new Padding(5, 4, 5, 4);
            RTB_Logs.Name = "RTB_Logs";
            RTB_Logs.ReadOnly = true;
            RTB_Logs.Size = new Size(768, 396);
            RTB_Logs.TabIndex = 0;
            RTB_Logs.Text = "";
            // 
            // B_Stop
            // 
            B_Stop.BackColor = Color.Maroon;
            B_Stop.FlatStyle = FlatStyle.Flat;
            B_Stop.Font = new Font("Calibri", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            B_Stop.ForeColor = SystemColors.ControlLightLight;
            B_Stop.Location = new Point(263, 0);
            B_Stop.Margin = new Padding(3, 4, 3, 4);
            B_Stop.Name = "B_Stop";
            B_Stop.Size = new Size(283, 48);
            B_Stop.TabIndex = 1;
            B_Stop.Text = "Stop All";
            B_Stop.UseVisualStyleBackColor = false;
            B_Stop.Click += B_Stop_Click;
            // 
            // B_Start
            // 
            B_Start.BackColor = Color.ForestGreen;
            B_Start.FlatStyle = FlatStyle.Flat;
            B_Start.Font = new Font("Calibri", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            B_Start.ForeColor = SystemColors.ControlLightLight;
            B_Start.Location = new Point(0, 0);
            B_Start.Margin = new Padding(3, 4, 3, 4);
            B_Start.Name = "B_Start";
            B_Start.Size = new Size(265, 48);
            B_Start.TabIndex = 0;
            B_Start.Text = "Start All";
            B_Start.UseVisualStyleBackColor = false;
            B_Start.Click += B_Start_Click;
            // 
            // B_RebootStop
            // 
            B_RebootStop.BackColor = Color.SteelBlue;
            B_RebootStop.FlatStyle = FlatStyle.Flat;
            B_RebootStop.Font = new Font("Calibri", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            B_RebootStop.ForeColor = SystemColors.ControlLightLight;
            B_RebootStop.Location = new Point(545, 0);
            B_RebootStop.Margin = new Padding(3, 4, 3, 4);
            B_RebootStop.Name = "B_RebootStop";
            B_RebootStop.Size = new Size(231, 48);
            B_RebootStop.TabIndex = 2;
            B_RebootStop.Text = "Reboot And Stop";
            B_RebootStop.UseVisualStyleBackColor = false;
            B_RebootStop.Click += B_RebootStop_Click;
            // 
            // ButtonPanel
            // 
            ButtonPanel.BackColor = Color.Transparent;
            ButtonPanel.Controls.Add(B_RebootStop);
            ButtonPanel.Controls.Add(B_Stop);
            ButtonPanel.Controls.Add(B_Start);
            ButtonPanel.Dock = DockStyle.Bottom;
            ButtonPanel.Location = new Point(0, 435);
            ButtonPanel.Margin = new Padding(3, 4, 3, 4);
            ButtonPanel.Name = "ButtonPanel";
            ButtonPanel.Size = new Size(776, 48);
            ButtonPanel.TabIndex = 0;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(8F, 18F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = SystemColors.Desktop;
            CancelButton = B_Stop;
            ClientSize = new Size(776, 483);
            Controls.Add(TC_Main);
            Controls.Add(ButtonPanel);
            Font = new Font("Calibri", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = Resources.icon;
            Margin = new Padding(5, 4, 5, 4);
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "NotPaldea.net";
            FormClosing += Main_FormClosing;
            TC_Main.ResumeLayout(false);
            Tab_Bots.ResumeLayout(false);
            Tab_Bots.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)NUD_Port).EndInit();
            Tab_Hub.ResumeLayout(false);
            Tab_Logs.ResumeLayout(false);
            ButtonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.TabControl TC_Main;
        private System.Windows.Forms.TabPage Tab_Bots;
        private System.Windows.Forms.TabPage Tab_Logs;
        private System.Windows.Forms.RichTextBox RTB_Logs;
        private System.Windows.Forms.TabPage Tab_Hub;
        private System.Windows.Forms.PropertyGrid PG_Hub;
        private System.Windows.Forms.Button B_Stop;
        private System.Windows.Forms.Button B_Start;
        private System.Windows.Forms.TextBox TB_IP;
        private System.Windows.Forms.ComboBox CB_Routine;
        private System.Windows.Forms.NumericUpDown NUD_Port;
        private System.Windows.Forms.Button B_New;
        private System.Windows.Forms.FlowLayoutPanel FLP_Bots;
        private System.Windows.Forms.ComboBox CB_Protocol;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Button B_RebootStop;
        private System.Windows.Forms.Panel ButtonPanel;
    }
}

