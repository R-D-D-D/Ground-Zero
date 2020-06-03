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
using Newtonsoft.Json;
using System.IO;

namespace WaveformGenerator
{
    public partial class Form1 : Form
    {
        public WaveformGenerator[] wgs;
        private RhythmPattern rp;
        private Action<FileType> generate_Image;
        public Func<FileType, string, Task> generate_Image_Cmdline;
        /// <summary>
        /// This is to distinguish between solution and file
        /// </summary>
        public enum FileType
        {
            Solution,
            Sample
        }

        public Form1(string rhythmFilename)
        {
            InitializeComponent();

            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            generate_Image = generate_Waveform;
            generate_Image_Cmdline = generate_Waveform_Cmdline;
            wgs = new WaveformGenerator[2];

            // Load rhythm
            StreamReader r = File.OpenText(rhythmFilename);
            string json = r.ReadToEnd();
            RhythmPattern item = JsonConvert.DeserializeObject<RhythmPattern>(json);
            rp = item;
            rp.TimeOfBarSeconds = 60 / rp.BPM * rp.TimeSignitureTop;
            rp.TimeOfBeatSeconds = 60 / rp.BPM;
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

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK && rp != null)
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
                wgs[index] = new WaveformGenerator(ofd.FileName, rp);

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
            }
        }

        public async Task generate_Waveform_Cmdline(FileType type, string filename)
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
            this.wgs[index] = new WaveformGenerator(filename, rp);
            WaveformGenerator wg = this.wgs[index];
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
                Console.WriteLine($"Peak value: {wgs[0].PeakValue}");
            } else
            {
                ReloadWaveform(FileType.Sample);
                Console.WriteLine($"Peak value: {wgs[1].PeakValue}");
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
            compareResultLabel.MaximumSize = new Size(pictureBox2.Width, 0);
        }

        public void ReloadWaveform(FileType fileType)
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
                    pictureBox2.Image = wgs[1].CreateWaveform(pictureBox2.Width, pictureBox2.Height);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void compareBtn_Click(object sender, EventArgs e)
        {
            if (wgs[0] == null || wgs[1] == null || rp == null)
                compareResultLabel.Text = "Result: Please input all the three files first!";
            else
                compareResultLabel.Text = WaveformGenerator.Compare(wgs[0], wgs[1], 100);
            if (pictureBox1.Image != null)
                ReloadWaveform(FileType.Solution);
            if (pictureBox2.Image != null)
                ReloadWaveform(FileType.Sample);
        }

        private void CompareResultLabel_Click(object sender, EventArgs e)
        {

        }

        private void ToCSV_Click(object sender, EventArgs e)
        {
            if (wgs[0] == null || wgs[1] == null)
                compareResultLabel.Text = "Result: Please input both files first!";
            else
            {
                wgs[0].ToCSV("solution.csv");
                wgs[1].ToCSV("sample.csv");
            }
        }

        private void LoadRhythm_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filename = ofd.FileName;
                StreamReader r = File.OpenText(filename);
                string json = r.ReadToEnd();
                RhythmPattern item = JsonConvert.DeserializeObject<RhythmPattern>(json);
                rp = item;
                rp.TimeOfBarSeconds = 60 / rp.BPM * rp.TimeSignitureTop;
                rp.TimeOfBeatSeconds = 60 / rp.BPM;
            }
        }
    }
}
