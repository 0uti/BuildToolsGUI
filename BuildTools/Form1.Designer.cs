﻿namespace BuildTools
{
    partial class BuildTools
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BuildTools));
            this.runBT = new System.Windows.Forms.Button();
            this.updateBT = new System.Windows.Forms.Button();
            this.autoUpdateCB = new System.Windows.Forms.CheckBox();
            this.outputTB = new System.Windows.Forms.RichTextBox();
            this.clearBT = new System.Windows.Forms.Button();
            this.undoBT = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // runBT
            // 
            this.runBT.Location = new System.Drawing.Point(13, 13);
            this.runBT.Name = "runBT";
            this.runBT.Size = new System.Drawing.Size(107, 23);
            this.runBT.TabIndex = 0;
            this.runBT.Text = "Run BuildTools";
            this.runBT.UseVisualStyleBackColor = true;
            this.runBT.Click += new System.EventHandler(this.runBT_Click);
            // 
            // updateBT
            // 
            this.updateBT.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.updateBT.Location = new System.Drawing.Point(498, 13);
            this.updateBT.Name = "updateBT";
            this.updateBT.Size = new System.Drawing.Size(124, 23);
            this.updateBT.TabIndex = 1;
            this.updateBT.Text = "Update BuildTools";
            this.updateBT.UseVisualStyleBackColor = true;
            this.updateBT.Click += new System.EventHandler(this.updateBT_Click);
            // 
            // autoUpdateCB
            // 
            this.autoUpdateCB.Checked = true;
            this.autoUpdateCB.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoUpdateCB.Location = new System.Drawing.Point(126, 13);
            this.autoUpdateCB.Name = "autoUpdateCB";
            this.autoUpdateCB.Size = new System.Drawing.Size(187, 24);
            this.autoUpdateCB.TabIndex = 2;
            this.autoUpdateCB.Text = "Automatically check for updates";
            this.autoUpdateCB.UseVisualStyleBackColor = true;
            // 
            // outputTB
            // 
            this.outputTB.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.outputTB.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.outputTB.Location = new System.Drawing.Point(13, 43);
            this.outputTB.Name = "outputTB";
            this.outputTB.ReadOnly = true;
            this.outputTB.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedBoth;
            this.outputTB.Size = new System.Drawing.Size(609, 477);
            this.outputTB.TabIndex = 3;
            this.outputTB.Text = "";
            this.outputTB.WordWrap = false;
            // 
            // clearBT
            // 
            this.clearBT.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.clearBT.Location = new System.Drawing.Point(547, 526);
            this.clearBT.Name = "clearBT";
            this.clearBT.Size = new System.Drawing.Size(75, 23);
            this.clearBT.TabIndex = 4;
            this.clearBT.Text = "Clear Log";
            this.clearBT.UseVisualStyleBackColor = true;
            this.clearBT.Click += new System.EventHandler(this.clearBT_Click);
            // 
            // undoBT
            // 
            this.undoBT.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.undoBT.Location = new System.Drawing.Point(466, 526);
            this.undoBT.Name = "undoBT";
            this.undoBT.Size = new System.Drawing.Size(75, 23);
            this.undoBT.TabIndex = 5;
            this.undoBT.Text = "Undo Clear";
            this.undoBT.UseVisualStyleBackColor = true;
            this.undoBT.Click += new System.EventHandler(this.undoBT_Click);
            // 
            // BuildTools
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(634, 561);
            this.Controls.Add(this.undoBT);
            this.Controls.Add(this.clearBT);
            this.Controls.Add(this.outputTB);
            this.Controls.Add(this.autoUpdateCB);
            this.Controls.Add(this.updateBT);
            this.Controls.Add(this.runBT);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(650, 85);
            this.Name = "BuildTools";
            this.Text = "BuildTools";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button runBT;
        private System.Windows.Forms.Button updateBT;
        private System.Windows.Forms.CheckBox autoUpdateCB;
        private System.Windows.Forms.RichTextBox outputTB;
        private System.Windows.Forms.Button clearBT;
        private System.Windows.Forms.Button undoBT;
    }
}

