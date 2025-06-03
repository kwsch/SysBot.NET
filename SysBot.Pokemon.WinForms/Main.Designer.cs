using SysBot.Pokemon.WinForms.Properties;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            TC_Main = new TabControl();
            Tab_Bots = new TabPage();
            CB_Theme = new ComboBox();
            CB_Mode = new ComboBox();
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
            B_Restart = new Button();
            B_Update = new Button();
            TC_Main.SuspendLayout();
            Tab_Bots.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)NUD_Port).BeginInit();
            Tab_Hub.SuspendLayout();
            Tab_Logs.SuspendLayout();
            SuspendLayout();
            // 
            // TC_Main
            // 
            TC_Main.Controls.Add(Tab_Bots);
            TC_Main.Controls.Add(Tab_Hub);
            TC_Main.Controls.Add(Tab_Logs);
            TC_Main.Dock = DockStyle.Fill;
            TC_Main.ItemSize = new Size(60, 25);
            TC_Main.Location = new Point(0, 0);
            TC_Main.Margin = new Padding(5, 4, 5, 4);
            TC_Main.Name = "TC_Main";
            TC_Main.RightToLeftLayout = true;
            TC_Main.SelectedIndex = 0;
            TC_Main.Size = new Size(694, 451);
            TC_Main.TabIndex = 3;
            // 
            // Tab_Bots
            // 
            Tab_Bots.Controls.Add(CB_Theme);
            Tab_Bots.Controls.Add(CB_Mode);
            Tab_Bots.Controls.Add(CB_Protocol);
            Tab_Bots.Controls.Add(FLP_Bots);
            Tab_Bots.Controls.Add(TB_IP);
            Tab_Bots.Controls.Add(CB_Routine);
            Tab_Bots.Controls.Add(NUD_Port);
            Tab_Bots.Controls.Add(B_New);
            Tab_Bots.Location = new Point(4, 29);
            Tab_Bots.Margin = new Padding(5, 4, 5, 4);
            Tab_Bots.Name = "Tab_Bots";
            Tab_Bots.Size = new Size(686, 418);
            Tab_Bots.TabIndex = 0;
            Tab_Bots.Text = "Bots";
            Tab_Bots.UseVisualStyleBackColor = true;
            // 
            // CB_Theme
            // 
            CB_Theme.DropDownStyle = ComboBoxStyle.DropDownList;
            CB_Theme.FormattingEnabled = true;
            CB_Theme.Location = new Point(515, 10);
            CB_Theme.Margin = new Padding(5, 4, 5, 4);
            CB_Theme.Name = "CB_Theme";
            CB_Theme.Size = new Size(150, 27);
            CB_Theme.TabIndex = 12;
            CB_Theme.SelectedIndexChanged += CB_Theme_SelectedIndexChanged;
            // 
            // CB_Mode
            // 
            CB_Mode.DropDownStyle = ComboBoxStyle.DropDownList;
            CB_Mode.FormattingEnabled = true;
            CB_Mode.Location = new Point(445, 10);
            CB_Mode.Margin = new Padding(5, 4, 5, 4);
            CB_Mode.Name = "CB_Mode";
            CB_Mode.Size = new Size(65, 27);
            CB_Mode.TabIndex = 11;
            CB_Mode.SelectedIndexChanged += CB_Mode_SelectedIndexChanged;
            // 
            // CB_Protocol
            // 
            CB_Protocol.DropDownStyle = ComboBoxStyle.DropDownList;
            CB_Protocol.FormattingEnabled = true;
            CB_Protocol.Location = new Point(250, 10);
            CB_Protocol.Margin = new Padding(5, 4, 5, 4);
            CB_Protocol.Name = "CB_Protocol";
            CB_Protocol.Size = new Size(65, 27);
            CB_Protocol.TabIndex = 10;
            CB_Protocol.SelectedIndexChanged += CB_Protocol_SelectedIndexChanged;
            // 
            // FLP_Bots
            // 
            FLP_Bots.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            FLP_Bots.BorderStyle = BorderStyle.FixedSingle;
            FLP_Bots.Location = new Point(0, 47);
            FLP_Bots.Margin = new Padding(0);
            FLP_Bots.Name = "FLP_Bots";
            FLP_Bots.Size = new Size(684, 370);
            FLP_Bots.TabIndex = 9;
            FLP_Bots.Resize += FLP_Bots_Resize;
            // 
            // TB_IP
            // 
            TB_IP.BorderStyle = BorderStyle.FixedSingle;
            TB_IP.Font = new Font("Calibri", 11.5F);
            TB_IP.Location = new Point(80, 11);
            TB_IP.Margin = new Padding(5, 4, 5, 4);
            TB_IP.Name = "TB_IP";
            TB_IP.Size = new Size(105, 26);
            TB_IP.TabIndex = 8;
            TB_IP.Text = "192.168.0.1";
            // 
            // CB_Routine
            // 
            CB_Routine.DropDownStyle = ComboBoxStyle.DropDownList;
            CB_Routine.FormattingEnabled = true;
            CB_Routine.Location = new Point(320, 10);
            CB_Routine.Margin = new Padding(5, 4, 5, 4);
            CB_Routine.Name = "CB_Routine";
            CB_Routine.Size = new Size(120, 27);
            CB_Routine.TabIndex = 7;
            // 
            // NUD_Port
            // 
            NUD_Port.BorderStyle = BorderStyle.FixedSingle;
            NUD_Port.Font = new Font("Calibri", 11.5F);
            NUD_Port.Location = new Point(190, 11);
            NUD_Port.Margin = new Padding(4, 3, 4, 3);
            NUD_Port.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            NUD_Port.Name = "NUD_Port";
            NUD_Port.Size = new Size(55, 26);
            NUD_Port.TabIndex = 6;
            NUD_Port.Value = new decimal(new int[] { 6000, 0, 0, 0 });
            // 
            // B_New
            // 
            B_New.Font = new Font("Calibri", 12F, FontStyle.Bold);
            B_New.Image = Resources.add;
            B_New.ImageAlign = ContentAlignment.MiddleLeft;
            B_New.Location = new Point(5, 9);
            B_New.Margin = new Padding(5, 4, 5, 4);
            B_New.Name = "B_New";
            B_New.Size = new Size(70, 29);
            B_New.TabIndex = 0;
            B_New.Text = "Add";
            B_New.TextAlign = ContentAlignment.MiddleRight;
            B_New.UseVisualStyleBackColor = true;
            B_New.Click += B_New_Click;
            // 
            // Tab_Hub
            // 
            Tab_Hub.Controls.Add(PG_Hub);
            Tab_Hub.Location = new Point(4, 29);
            Tab_Hub.Margin = new Padding(5, 4, 5, 4);
            Tab_Hub.Name = "Tab_Hub";
            Tab_Hub.Padding = new Padding(5, 4, 5, 4);
            Tab_Hub.Size = new Size(686, 418);
            Tab_Hub.TabIndex = 2;
            Tab_Hub.Text = "Hub";
            Tab_Hub.UseVisualStyleBackColor = true;
            // 
            // PG_Hub
            // 
            PG_Hub.BackColor = SystemColors.Control;
            PG_Hub.Dock = DockStyle.Fill;
            PG_Hub.Font = new Font("Calibri", 11F);
            PG_Hub.Location = new Point(5, 4);
            PG_Hub.Margin = new Padding(5, 4, 5, 4);
            PG_Hub.Name = "PG_Hub";
            PG_Hub.PropertySort = PropertySort.Categorized;
            PG_Hub.Size = new Size(676, 410);
            PG_Hub.TabIndex = 0;
            // 
            // Tab_Logs
            // 
            Tab_Logs.Controls.Add(RTB_Logs);
            Tab_Logs.Location = new Point(4, 29);
            Tab_Logs.Margin = new Padding(5, 4, 5, 4);
            Tab_Logs.Name = "Tab_Logs";
            Tab_Logs.Size = new Size(686, 418);
            Tab_Logs.TabIndex = 1;
            Tab_Logs.Text = "Logs";
            Tab_Logs.UseVisualStyleBackColor = true;
            // 
            // RTB_Logs
            // 
            RTB_Logs.Dock = DockStyle.Fill;
            RTB_Logs.HideSelection = false;
            RTB_Logs.Location = new Point(0, 0);
            RTB_Logs.Margin = new Padding(5, 4, 5, 4);
            RTB_Logs.Name = "RTB_Logs";
            RTB_Logs.ReadOnly = true;
            RTB_Logs.Size = new Size(686, 418);
            RTB_Logs.TabIndex = 0;
            RTB_Logs.Text = "";
            // 
            // B_Stop
            // 
            B_Stop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            B_Stop.Font = new Font("Calibri", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            B_Stop.Image = Resources.stop;
            B_Stop.ImageAlign = ContentAlignment.MiddleLeft;
            B_Stop.Location = new Point(349, 0);
            B_Stop.Margin = new Padding(5, 4, 5, 4);
            B_Stop.Name = "B_Stop";
            B_Stop.Size = new Size(110, 29);
            B_Stop.TabIndex = 4;
            B_Stop.Text = "Stop All";
            B_Stop.TextAlign = ContentAlignment.MiddleRight;
            B_Stop.UseVisualStyleBackColor = true;
            B_Stop.Click += B_Stop_Click;
            // 
            // B_Start
            // 
            B_Start.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            B_Start.Font = new Font("Calibri", 12F, FontStyle.Bold);
            B_Start.Image = Resources.start;
            B_Start.ImageAlign = ContentAlignment.MiddleLeft;
            B_Start.Location = new Point(234, 0);
            B_Start.Margin = new Padding(5, 4, 5, 4);
            B_Start.Name = "B_Start";
            B_Start.Size = new Size(110, 29);
            B_Start.TabIndex = 3;
            B_Start.Text = "Start All";
            B_Start.TextAlign = ContentAlignment.MiddleRight;
            B_Start.UseVisualStyleBackColor = true;
            B_Start.Click += B_Start_Click;
            // 
            // B_Restart
            // 
            B_Restart.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            B_Restart.Font = new Font("Calibri", 12F, FontStyle.Bold);
            B_Restart.Image = (Image)resources.GetObject("B_Restart.Image");
            B_Restart.ImageAlign = ContentAlignment.MiddleLeft;
            B_Restart.Location = new Point(464, 0);
            B_Restart.Margin = new Padding(5, 4, 5, 4);
            B_Restart.Name = "B_Restart";
            B_Restart.Size = new Size(110, 29);
            B_Restart.TabIndex = 5;
            B_Restart.Text = "Restart All";
            B_Restart.TextAlign = ContentAlignment.MiddleRight;
            B_Restart.UseVisualStyleBackColor = true;
            B_Restart.Click += B_Restart_Click;
            // 
            // B_Update
            // 
            B_Update.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            B_Update.Font = new Font("Calibri", 12F, FontStyle.Bold);
            B_Update.Image = (Image)resources.GetObject("B_Update.Image");
            B_Update.ImageAlign = ContentAlignment.MiddleLeft;
            B_Update.Location = new Point(579, 0);
            B_Update.Margin = new Padding(5, 4, 5, 4);
            B_Update.Name = "B_Update";
            B_Update.Size = new Size(110, 29);
            B_Update.TabIndex = 6;
            B_Update.Text = "Update";
            B_Update.TextAlign = ContentAlignment.MiddleRight;
            B_Update.UseVisualStyleBackColor = true;
            B_Update.Click += B_Update_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(694, 451);
            Controls.Add(B_Update);
            Controls.Add(B_Restart);
            Controls.Add(B_Stop);
            Controls.Add(B_Start);
            Controls.Add(TC_Main);
            Font = new Font("Calibri", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = Resources.icon;
            Margin = new Padding(5, 4, 5, 4);
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SysBot: Pokémon";
            FormClosing += Main_FormClosing;
            TC_Main.ResumeLayout(false);
            Tab_Bots.ResumeLayout(false);
            Tab_Bots.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)NUD_Port).EndInit();
            Tab_Hub.ResumeLayout(false);
            Tab_Logs.ResumeLayout(false);
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
        private System.Windows.Forms.Button B_Restart;
        private System.Windows.Forms.Button B_Update;
        private System.Windows.Forms.ComboBox CB_Theme;
        private System.Windows.Forms.ComboBox CB_Mode;
    }
}
