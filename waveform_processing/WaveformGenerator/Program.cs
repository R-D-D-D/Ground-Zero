using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WaveformGenerator {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            foreach (string str in args)
            {
                Console.WriteLine(str);
            }
            Console.WriteLine($"Length of args: {args.Length}");
            if (args.Length != 3)
            {
                Console.WriteLine("Please enter 3 arguments: rhythm, solution, sample.");
                return;
            }
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Prepare form1
            Form1 temp = new Form1(args[0]);
            temp.generate_Image_Cmdline(Form1.FileType.Solution, args[1]);
            temp.generate_Image_Cmdline(Form1.FileType.Sample, args[2]);

            // Do comparison
            //WaveformGenerator.Compare(temp.wgs[0], temp.wgs[1], 100);
            //temp.ReloadWaveform(Form1.FileType.Solution);
            //temp.ReloadWaveform(Form1.FileType.Sample);
            Application.Run(temp);
        }
    }
}
