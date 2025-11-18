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
            TC_Main = new System.Windows.Forms.TabControl();
            Tab_Bots = new System.Windows.Forms.TabPage();
            FLP_Bots = new System.Windows.Forms.FlowLayoutPanel();
            FLP_BotCreator = new System.Windows.Forms.FlowLayoutPanel();
            B_New = new System.Windows.Forms.Button();
            TB_IP = new System.Windows.Forms.TextBox();
            NUD_Port = new System.Windows.Forms.TextBox();
            CB_Protocol = new System.Windows.Forms.ComboBox();
            CB_Routine = new System.Windows.Forms.ComboBox();
            FLP_Line = new System.Windows.Forms.FlowLayoutPanel();
            Tab_Hub = new System.Windows.Forms.TabPage();
            PG_Hub = new System.Windows.Forms.PropertyGrid();
            Tab_Logs = new System.Windows.Forms.TabPage();
            RTB_Logs = new System.Windows.Forms.RichTextBox();
            B_Stop = new System.Windows.Forms.Button();
            B_Start = new System.Windows.Forms.Button();
            TC_Main.SuspendLayout();
            Tab_Bots.SuspendLayout();
            FLP_Bots.SuspendLayout();
            FLP_BotCreator.SuspendLayout();
            Tab_Hub.SuspendLayout();
            Tab_Logs.SuspendLayout();
            SuspendLayout();
            // 
            // TC_Main
            // 
            TC_Main.Controls.Add(Tab_Bots);
            TC_Main.Controls.Add(Tab_Hub);
            TC_Main.Controls.Add(Tab_Logs);
            TC_Main.Dock = System.Windows.Forms.DockStyle.Fill;
            TC_Main.ItemSize = new System.Drawing.Size(96, 32);
            TC_Main.Location = new System.Drawing.Point(0, 0);
            TC_Main.Margin = new System.Windows.Forms.Padding(0);
            TC_Main.Name = "TC_Main";
            TC_Main.SelectedIndex = 0;
            TC_Main.Size = new System.Drawing.Size(684, 281);
            TC_Main.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            TC_Main.TabIndex = 3;
            // 
            // Tab_Bots
            // 
            Tab_Bots.Controls.Add(FLP_Bots);
            Tab_Bots.Location = new System.Drawing.Point(4, 36);
            Tab_Bots.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Tab_Bots.Name = "Tab_Bots";
            Tab_Bots.Size = new System.Drawing.Size(676, 241);
            Tab_Bots.TabIndex = 0;
            Tab_Bots.Text = "Bots";
            Tab_Bots.UseVisualStyleBackColor = true;
            // 
            // FLP_Bots
            // 
            FLP_Bots.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            FLP_Bots.Controls.Add(FLP_BotCreator);
            FLP_Bots.Controls.Add(FLP_Line);
            FLP_Bots.Dock = System.Windows.Forms.DockStyle.Fill;
            FLP_Bots.Location = new System.Drawing.Point(0, 0);
            FLP_Bots.Margin = new System.Windows.Forms.Padding(0);
            FLP_Bots.Name = "FLP_Bots";
            FLP_Bots.Size = new System.Drawing.Size(676, 241);
            FLP_Bots.TabIndex = 9;
            FLP_Bots.Resize += FLP_Bots_Resize;
            // 
            // FLP_BotCreator
            // 
            FLP_BotCreator.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            FLP_BotCreator.BackColor = System.Drawing.SystemColors.Control;
            FLP_BotCreator.Controls.Add(B_New);
            FLP_BotCreator.Controls.Add(TB_IP);
            FLP_BotCreator.Controls.Add(NUD_Port);
            FLP_BotCreator.Controls.Add(CB_Protocol);
            FLP_BotCreator.Controls.Add(CB_Routine);
            FLP_Bots.SetFlowBreak(FLP_BotCreator, true);
            FLP_BotCreator.Location = new System.Drawing.Point(0, 0);
            FLP_BotCreator.Margin = new System.Windows.Forms.Padding(0);
            FLP_BotCreator.Name = "FLP_BotCreator";
            FLP_BotCreator.Size = new System.Drawing.Size(65535, 33);
            FLP_BotCreator.TabIndex = 12;
            // 
            // B_New
            // 
            B_New.Location = new System.Drawing.Point(4, 4);
            B_New.Margin = new System.Windows.Forms.Padding(4);
            B_New.Name = "B_New";
            B_New.Size = new System.Drawing.Size(63, 25);
            B_New.TabIndex = 0;
            B_New.Text = "Add";
            B_New.UseVisualStyleBackColor = true;
            B_New.Click += B_New_Click;
            // 
            // TB_IP
            // 
            TB_IP.Location = new System.Drawing.Point(71, 4);
            TB_IP.Margin = new System.Windows.Forms.Padding(0, 4, 4, 4);
            TB_IP.Name = "TB_IP";
            TB_IP.Size = new System.Drawing.Size(134, 25);
            TB_IP.TabIndex = 8;
            TB_IP.Text = "192.168.0.1";
            // 
            // NUD_Port
            // 
            NUD_Port.Location = new System.Drawing.Point(209, 4);
            NUD_Port.Margin = new System.Windows.Forms.Padding(0, 4, 4, 4);
            NUD_Port.Name = "NUD_Port";
            NUD_Port.ReadOnly = true;
            NUD_Port.Size = new System.Drawing.Size(67, 25);
            NUD_Port.TabIndex = 6;
            NUD_Port.Text = "6000";
            // 
            // CB_Protocol
            // 
            CB_Protocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            CB_Protocol.FormattingEnabled = true;
            CB_Protocol.Location = new System.Drawing.Point(280, 4);
            CB_Protocol.Margin = new System.Windows.Forms.Padding(0, 4, 4, 4);
            CB_Protocol.Name = "CB_Protocol";
            CB_Protocol.Size = new System.Drawing.Size(67, 25);
            CB_Protocol.TabIndex = 10;
            CB_Protocol.SelectedIndexChanged += CB_Protocol_SelectedIndexChanged;
            // 
            // CB_Routine
            // 
            CB_Routine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            CB_Routine.FormattingEnabled = true;
            CB_Routine.Location = new System.Drawing.Point(351, 4);
            CB_Routine.Margin = new System.Windows.Forms.Padding(0, 4, 4, 4);
            CB_Routine.Name = "CB_Routine";
            CB_Routine.Size = new System.Drawing.Size(117, 25);
            CB_Routine.TabIndex = 7;
            // 
            // FLP_Line
            // 
            FLP_Line.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            FLP_Bots.SetFlowBreak(FLP_Line, true);
            FLP_Line.Location = new System.Drawing.Point(0, 33);
            FLP_Line.Margin = new System.Windows.Forms.Padding(0);
            FLP_Line.Name = "FLP_Line";
            FLP_Line.Size = new System.Drawing.Size(65535, 1);
            FLP_Line.TabIndex = 5;
            // 
            // Tab_Hub
            // 
            Tab_Hub.Controls.Add(PG_Hub);
            Tab_Hub.Location = new System.Drawing.Point(4, 36);
            Tab_Hub.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Tab_Hub.Name = "Tab_Hub";
            Tab_Hub.Size = new System.Drawing.Size(676, 241);
            Tab_Hub.TabIndex = 2;
            Tab_Hub.Text = "Hub";
            Tab_Hub.UseVisualStyleBackColor = true;
            // 
            // PG_Hub
            // 
            PG_Hub.BackColor = System.Drawing.SystemColors.Control;
            PG_Hub.Dock = System.Windows.Forms.DockStyle.Fill;
            PG_Hub.Location = new System.Drawing.Point(0, 0);
            PG_Hub.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            PG_Hub.Name = "PG_Hub";
            PG_Hub.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            PG_Hub.Size = new System.Drawing.Size(676, 241);
            PG_Hub.TabIndex = 0;
            // 
            // Tab_Logs
            // 
            Tab_Logs.Controls.Add(RTB_Logs);
            Tab_Logs.Location = new System.Drawing.Point(4, 36);
            Tab_Logs.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Tab_Logs.Name = "Tab_Logs";
            Tab_Logs.Size = new System.Drawing.Size(676, 241);
            Tab_Logs.TabIndex = 1;
            Tab_Logs.Text = "Logs";
            Tab_Logs.UseVisualStyleBackColor = true;
            // 
            // RTB_Logs
            // 
            RTB_Logs.Dock = System.Windows.Forms.DockStyle.Fill;
            RTB_Logs.HideSelection = false;
            RTB_Logs.Location = new System.Drawing.Point(0, 0);
            RTB_Logs.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            RTB_Logs.Name = "RTB_Logs";
            RTB_Logs.ReadOnly = true;
            RTB_Logs.Size = new System.Drawing.Size(676, 241);
            RTB_Logs.TabIndex = 0;
            RTB_Logs.Text = "";
            // 
            // B_Stop
            // 
            B_Stop.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            B_Stop.Location = new System.Drawing.Point(560, 2);
            B_Stop.Margin = new System.Windows.Forms.Padding(0);
            B_Stop.Name = "B_Stop";
            B_Stop.Size = new System.Drawing.Size(80, 32);
            B_Stop.TabIndex = 4;
            B_Stop.Text = "Stop All";
            B_Stop.UseVisualStyleBackColor = true;
            B_Stop.Click += B_Stop_Click;
            // 
            // B_Start
            // 
            B_Start.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            B_Start.Location = new System.Drawing.Point(480, 2);
            B_Start.Margin = new System.Windows.Forms.Padding(0);
            B_Start.Name = "B_Start";
            B_Start.Size = new System.Drawing.Size(80, 32);
            B_Start.TabIndex = 3;
            B_Start.Text = "Start All";
            B_Start.UseVisualStyleBackColor = true;
            B_Start.Click += B_Start_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(684, 281);
            Controls.Add(B_Stop);
            Controls.Add(B_Start);
            Controls.Add(TC_Main);
            Icon = Resources.icon;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MinimumSize = new System.Drawing.Size(520, 320);
            Name = "Main";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "SysBot: Pokémon";
            FormClosing += Main_FormClosing;
            TC_Main.ResumeLayout(false);
            Tab_Bots.ResumeLayout(false);
            FLP_Bots.ResumeLayout(false);
            FLP_BotCreator.ResumeLayout(false);
            FLP_BotCreator.PerformLayout();
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
        private System.Windows.Forms.TextBox NUD_Port;
        private System.Windows.Forms.Button B_New;
        private System.Windows.Forms.FlowLayoutPanel FLP_Bots;
        private System.Windows.Forms.ComboBox CB_Protocol;
        private System.Windows.Forms.FlowLayoutPanel FLP_BotCreator;
        private System.Windows.Forms.FlowLayoutPanel FLP_Line;
    }
}

