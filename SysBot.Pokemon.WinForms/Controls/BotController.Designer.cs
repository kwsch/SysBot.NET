namespace SysBot.Pokemon.WinForms
{
    partial class BotController
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.L_Description = new System.Windows.Forms.Label();
            this.L_Left = new System.Windows.Forms.Label();
            this.PB_Lamp = new System.Windows.Forms.PictureBox();
            this.RCMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.PB_Lamp)).BeginInit();
            this.SuspendLayout();
            // 
            // L_Description
            // 
            this.L_Description.AutoSize = true;
            this.L_Description.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.L_Description.Location = new System.Drawing.Point(150, 9);
            this.L_Description.Name = "L_Description";
            this.L_Description.Size = new System.Drawing.Size(49, 14);
            this.L_Description.TabIndex = 2;
            this.L_Description.Text = "Status";
            // 
            // L_Left
            // 
            this.L_Left.AutoSize = true;
            this.L_Left.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.L_Left.Location = new System.Drawing.Point(31, 2);
            this.L_Left.Name = "L_Left";
            this.L_Left.Size = new System.Drawing.Size(112, 28);
            this.L_Left.TabIndex = 3;
            this.L_Left.Text = "192.168.123.123\r\nEncounterBot";
            // 
            // pictureBox1
            // 
            this.PB_Lamp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PB_Lamp.Location = new System.Drawing.Point(3, 3);
            this.PB_Lamp.Name = "PB_Lamp";
            this.PB_Lamp.Size = new System.Drawing.Size(26, 26);
            this.PB_Lamp.TabIndex = 4;
            this.PB_Lamp.TabStop = false;
            // 
            // contextMenuStrip1
            // 
            this.RCMenu.Name = "RCMenu";
            this.RCMenu.ShowImageMargin = false;
            this.RCMenu.ShowItemToolTips = false;
            this.RCMenu.Size = new System.Drawing.Size(36, 4);
            // 
            // BotController
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ContextMenuStrip = this.RCMenu;
            this.Controls.Add(this.PB_Lamp);
            this.Controls.Add(this.L_Left);
            this.Controls.Add(this.L_Description);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "BotController";
            this.Size = new System.Drawing.Size(410, 32);
            this.MouseEnter += new System.EventHandler(this.BotController_MouseEnter);
            this.MouseLeave += new System.EventHandler(this.BotController_MouseLeave);
            ((System.ComponentModel.ISupportInitialize)(this.PB_Lamp)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label L_Description;
        private System.Windows.Forms.Label L_Left;
        private System.Windows.Forms.PictureBox PB_Lamp;
        private System.Windows.Forms.ContextMenuStrip RCMenu;
    }
}
