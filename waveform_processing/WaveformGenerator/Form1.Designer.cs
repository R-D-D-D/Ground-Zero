namespace WaveformGenerator {
    partial class Form1 {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.openSolutionBtn = new System.Windows.Forms.Button();
            this.cancelBtn = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.openSampleBtn = new System.Windows.Forms.Button();
            this.compareBtn = new System.Windows.Forms.Button();
            this.compareResultLabel = new System.Windows.Forms.Label();
            this.ToCSV = new System.Windows.Forms.Button();
            this.LoadRhythm = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // openSolutionBtn
            // 
            this.openSolutionBtn.Location = new System.Drawing.Point(117, 12);
            this.openSolutionBtn.Name = "openSolutionBtn";
            this.openSolutionBtn.Size = new System.Drawing.Size(108, 21);
            this.openSolutionBtn.TabIndex = 0;
            this.openSolutionBtn.Text = "Open Solution";
            this.openSolutionBtn.UseVisualStyleBackColor = true;
            this.openSolutionBtn.Click += new System.EventHandler(this.openSolutionBtn_Click);
            // 
            // cancelBtn
            // 
            this.cancelBtn.Enabled = false;
            this.cancelBtn.Location = new System.Drawing.Point(426, 12);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(75, 21);
            this.cancelBtn.TabIndex = 1;
            this.cancelBtn.Text = "Cancel";
            this.cancelBtn.UseVisualStyleBackColor = true;
            this.cancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Location = new System.Drawing.Point(13, 40);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(1020, 190);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox2.Location = new System.Drawing.Point(12, 255);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(1020, 190);
            this.pictureBox2.TabIndex = 3;
            this.pictureBox2.TabStop = false;
            // 
            // openSampleBtn
            // 
            this.openSampleBtn.Location = new System.Drawing.Point(231, 12);
            this.openSampleBtn.Name = "openSampleBtn";
            this.openSampleBtn.Size = new System.Drawing.Size(108, 21);
            this.openSampleBtn.TabIndex = 4;
            this.openSampleBtn.Text = "Open Sample";
            this.openSampleBtn.UseVisualStyleBackColor = true;
            this.openSampleBtn.Click += new System.EventHandler(this.openSampleBtn_Click);
            // 
            // compareBtn
            // 
            this.compareBtn.Location = new System.Drawing.Point(345, 12);
            this.compareBtn.Name = "compareBtn";
            this.compareBtn.Size = new System.Drawing.Size(75, 21);
            this.compareBtn.TabIndex = 5;
            this.compareBtn.Text = "Compare";
            this.compareBtn.UseVisualStyleBackColor = true;
            this.compareBtn.Click += new System.EventHandler(this.compareBtn_Click);
            // 
            // compareResultLabel
            // 
            this.compareResultLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.compareResultLabel.AutoSize = true;
            this.compareResultLabel.Font = new System.Drawing.Font("SimSun", 14F);
            this.compareResultLabel.Location = new System.Drawing.Point(9, 448);
            this.compareResultLabel.MaximumSize = new System.Drawing.Size(1020, 0);
            this.compareResultLabel.Name = "compareResultLabel";
            this.compareResultLabel.Size = new System.Drawing.Size(79, 19);
            this.compareResultLabel.TabIndex = 6;
            this.compareResultLabel.Text = "Result:";
            this.compareResultLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.compareResultLabel.Click += new System.EventHandler(this.CompareResultLabel_Click);
            // 
            // ToCSV
            // 
            this.ToCSV.Location = new System.Drawing.Point(507, 10);
            this.ToCSV.Name = "ToCSV";
            this.ToCSV.Size = new System.Drawing.Size(75, 23);
            this.ToCSV.TabIndex = 7;
            this.ToCSV.Text = "To CSV";
            this.ToCSV.UseVisualStyleBackColor = true;
            this.ToCSV.Click += new System.EventHandler(this.ToCSV_Click);
            // 
            // LoadRhythm
            // 
            this.LoadRhythm.Location = new System.Drawing.Point(12, 12);
            this.LoadRhythm.Name = "LoadRhythm";
            this.LoadRhythm.Size = new System.Drawing.Size(99, 23);
            this.LoadRhythm.TabIndex = 8;
            this.LoadRhythm.Text = "Load Rhythm";
            this.LoadRhythm.UseVisualStyleBackColor = true;
            this.LoadRhythm.Click += new System.EventHandler(this.LoadRhythm_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1045, 563);
            this.Controls.Add(this.LoadRhythm);
            this.Controls.Add(this.ToCSV);
            this.Controls.Add(this.compareResultLabel);
            this.Controls.Add(this.compareBtn);
            this.Controls.Add(this.openSampleBtn);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.cancelBtn);
            this.Controls.Add(this.openSolutionBtn);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(650, 600);
            this.Name = "Form1";
            this.Text = "Rhythm Master";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.SizeChanged += new System.EventHandler(this.Form1_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button openSolutionBtn;
        private System.Windows.Forms.Button cancelBtn;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button openSampleBtn;
        private System.Windows.Forms.Button compareBtn;
        private System.Windows.Forms.Label compareResultLabel;
        private System.Windows.Forms.Button ToCSV;
        private System.Windows.Forms.Button LoadRhythm;
    }
}

