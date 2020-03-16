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
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // L_Description
            // 
            this.L_Description.AutoSize = true;
            this.L_Description.Location = new System.Drawing.Point(155, 9);
            this.L_Description.Name = "L_Description";
            this.L_Description.Size = new System.Drawing.Size(37, 13);
            this.L_Description.TabIndex = 2;
            this.L_Description.Text = "Status";
            // 
            // L_Left
            // 
            this.L_Left.AutoSize = true;
            this.L_Left.Location = new System.Drawing.Point(35, 3);
            this.L_Left.Name = "L_Left";
            this.L_Left.Size = new System.Drawing.Size(47, 26);
            this.L_Left.TabIndex = 3;
            this.L_Left.Text = "IP:\r\nRoutine:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Location = new System.Drawing.Point(3, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(26, 26);
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.ShowImageMargin = false;
            this.contextMenuStrip1.ShowItemToolTips = false;
            this.contextMenuStrip1.Size = new System.Drawing.Size(156, 26);
            // 
            // BotController
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ContextMenuStrip = this.contextMenuStrip1;
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.L_Left);
            this.Controls.Add(this.L_Description);
            this.Name = "BotController";
            this.Size = new System.Drawing.Size(410, 32);
            this.MouseEnter += new System.EventHandler(this.BotController_MouseEnter);
            this.MouseLeave += new System.EventHandler(this.BotController_MouseLeave);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label L_Description;
        private System.Windows.Forms.Label L_Left;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
    }
}
