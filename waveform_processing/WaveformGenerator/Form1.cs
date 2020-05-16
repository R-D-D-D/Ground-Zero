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

namespace WaveformGenerator
{
    public partial class Form1 : Form
    {
        private WaveformGenerator[] wgs;
        private Action<FileType> generate_Image;
        private enum FileType
        {
            Solution,
            Sample
        }

        public Form1()
        {
            InitializeComponent();

            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            generate_Image = generate_Waveform;
            wgs = new WaveformGenerator[2];
        }

        private void openSampleBtn_Click(object sender, EventArgs e)
        {
            generate_Image(FileType.Sample);
        }

        private void openSolutionBtn_Click(object sender, EventArgs e)
        {
            generate_Image(FileType.Solution);
        }

        private async void generate_Waveform (FileType type)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int index;
                switch (type)
                {
                    case FileType.Solution:
                        index = 0;
                        break;
                    case FileType.Sample:
                        index = 1;
                        break;
                    default:
                        index = 0;
                        break;
                }
                wgs[index] = new WaveformGenerator(ofd.FileName);

                WaveformGenerator wg = wgs[index];
                openSolutionBtn.Enabled = false;
                openSolutionBtn.Enabled = false;
                cancelBtn.Enabled = true;

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
#if DEBUG
                List<float> lst = wg.leftLevelList;
                //foreach (float fp in lst)
                //{
                //    Console.WriteLine(fp);
                //}
#endif
            }
        }


        private void cancelBtn_Click(object sender, EventArgs e)
        {
            ((WaveformGenerator)sender).CancelDetection();
        }

        private void wg_Completed(object sender, EventArgs e)
        {
            Console.WriteLine("Completed");

            // Add code to execute after completion. (Alternatively, add it after "await wg.DetectWaveformLevelsAsync();")
            // ...
            //Console.WriteLine(Object.ReferenceEquals(sender, wgs[0]));
            ((WaveformGenerator)sender).CloseStream();
            openSolutionBtn.Enabled = true;
            openSampleBtn.Enabled = true;
            cancelBtn.Enabled = false;
            if (Object.ReferenceEquals(sender, wgs[0]))
            {
                ReloadWaveform(FileType.Solution);
            } else
            {
                ReloadWaveform(FileType.Sample);
            }
            //pictureBox1.Image.Save("Waveform.jpg");
        }

        private void wg_ProgressChanged(object sender, WaveformGenerator.ProgressChangedEventArgs e)
        {
            if (Object.ReferenceEquals(sender, wgs[0]))
            {
                ReloadWaveform(FileType.Solution);
            }
            else
            {
                ReloadWaveform(FileType.Sample);
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
                ReloadWaveform(FileType.Solution);
            if (pictureBox2.Image != null)
                ReloadWaveform(FileType.Sample);
        }

        private void ReloadWaveform(FileType fileType)
        {
            if (pictureBox1.Width > 0 && pictureBox1.Height > 0 && pictureBox2.Width > 0 && pictureBox2.Height > 0)
            {
                if (fileType == FileType.Solution)
                {
                    if (pictureBox1.Image != null)
                        pictureBox1.Image.Dispose();
                    pictureBox1.Image = wgs[0].CreateWaveform(pictureBox1.Width, pictureBox1.Height);
                } else
                {
                    if (pictureBox2.Image != null)
                        pictureBox2.Image.Dispose();
                    pictureBox2.Image = wgs[1].CreateWaveform(pictureBox1.Width, pictureBox1.Height);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void compareBtn_Click(object sender, EventArgs e)
        {
            if (wgs[0] == null || wgs[1] == null)
                compareResultLabel.Text = "Result: Please input both files first!";
            else
                compareResultLabel.Text = wgs[0].CompareTo(wgs[1], 100);
            if (pictureBox1.Image != null)
                ReloadWaveform(FileType.Solution);
            if (pictureBox2.Image != null)
                ReloadWaveform(FileType.Sample);
        }

        private void CompareResultLabel_Click(object sender, EventArgs e)
        {

        }
    }
}
