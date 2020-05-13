using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Un4seen.Bass;

namespace WaveformGenerator {
    public partial class Form1 : Form {
        private WaveformGenerator wg;

        public Form1() {
            InitializeComponent();

            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
        }

        private async void openBtn_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                openBtn.Enabled = false;
                cancelBtn.Enabled = true;

                wg = new WaveformGenerator(ofd.FileName);

                // Change settings.
                wg.Direction = WaveformGenerator.WaveformDirection.LeftToRight;
                wg.Orientation = WaveformGenerator.WaveformSideOrientation.LeftSideOnTopOrLeft;
                wg.Detail = 1.5f;
                wg.LeftSideBrush = new SolidBrush(Color.Orange);
                wg.RightSideBrush = new SolidBrush(Color.Gray);

                wg.ProgressChanged += wg_ProgressChanged;
                wg.Completed += wg_Completed;
                wg.CreateStream();
                 
                await wg.DetectWaveformLevelsAsync();

                // Add code to execute after completion. (Alternatively, add it in the wg_Completed method)
                // ...
            }
        }

        private void cancelBtn_Click(object sender, EventArgs e) {
            wg.CancelDetection();
        }

        private void wg_Completed(object sender, EventArgs e) {
            Console.WriteLine("Completed");

            // Add code to execute after completion. (Alternatively, add it after "await wg.DetectWaveformLevelsAsync();")
            // ...

            wg.CloseStream();
            openBtn.Enabled = true;
            cancelBtn.Enabled = false;

            ReloadWaveform();

            //pictureBox1.Image.Save("Waveform.jpg");
        }

        private void wg_ProgressChanged(object sender, WaveformGenerator.ProgressChangedEventArgs e) {
            ReloadWaveform();
        }

        private void Form1_SizeChanged(object sender, EventArgs e) {
            if (pictureBox1.Image != null)
                ReloadWaveform();
        }

        private void ReloadWaveform() {
            if (pictureBox1.Width > 0 && pictureBox1.Height > 0) {
                if (pictureBox1.Image != null)
                    pictureBox1.Image.Dispose();
                pictureBox1.Image = wg.CreateWaveform(pictureBox1.Width, pictureBox1.Height);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
