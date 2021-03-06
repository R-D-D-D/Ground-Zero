/*
WaveGenerator.cs
A C# class for generating an audio waveform asynchronously.

Copyright 2014 Project Sfx
Apache License, Version 2.0

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace WaveformGenerator
{
    /// <summary>
    /// Generates audio waveform async.
    /// </summary>
    public class WaveformGenerator
    {

        #region Enums
        public enum ReadyState
        {
            Started,
            CreatedStream,
            Running,
            Completed,
            ClosedStream,
        }

        public enum WaveformDirection
        {
            LeftToRight,
            RightToLeft,
            TopToBottom,
            BottomToTop,
        }

        public enum WaveformSideOrientation
        {
            LeftSideOnTopOrLeft,
            LeftSideOnBottomOrRight,
        }

        public enum FileType
        {
            Solution,
            Sample
        }
        #endregion


        #region Events
        /// <summary>
        /// Invokes when progress of detection is changed.
        /// Interval between each invoke is at least 1ms.
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Invokes when detection completes.
        /// </summary>
        public event EventHandler Completed;
        #endregion


        #region EventArgs
        public class ProgressChangedEventArgs : EventArgs
        {
            public int FrameDoneCount { get; private set; }
            public float PercentageCompleted { get; private set; }

            public ProgressChangedEventArgs(int frameDoneCount, float percentageCompleted)
            {
                FrameDoneCount = frameDoneCount;
                PercentageCompleted = percentageCompleted;
            }
        }

        //public class FileTypeEventArgs : EventArgs
        //{
        //    public FileType FileCharacteristic { get; private set; }

        //    public FileTypeEventArgs(FileType fileCharacteristic)
        //    {
        //        FileCharacteristic = fileCharacteristic;
        //    }
        //}
        #endregion


        #region Fields
        private string filePath;
        private int stream;
        private float lastProgressPercentageEventRaised;
        public List<float> leftLevelList { get; private set; }
        public List<float> rightLevelList { get; private set; }
        private CancellationTokenSource cts;
        private Stopwatch progressSw;
        #endregion

        #region Properties
        // Defaults.
        private const float DefaultProgressPercentageInterval = 1f;
        private const float DefaultDetail = 1.5f;
        private const WaveformDirection DefaultDirection = WaveformDirection.LeftToRight;
        private const WaveformSideOrientation DefaultOrientation = WaveformSideOrientation.LeftSideOnTopOrLeft;
        private readonly Brush DefaultLeftSideBrush = new SolidBrush(Color.Green);
        private readonly Brush DefaultRightSideBrush = new SolidBrush(Color.SkyBlue);
        private readonly Brush DefaultCenterLineBrush = new SolidBrush(Color.FromArgb(128, Color.Black));

        public ReadyState State { get; private set; }

        /// <summary>
        /// Default = WaveformDirection.LeftToRight.
        /// </summary>
        public WaveformDirection Direction { get; set; }
        /// <summary>
        /// Default = WaveformSideOrientation.LeftSideOnTopOrLeft.
        /// </summary>
        public WaveformSideOrientation Orientation { get; set; }
        /// <summary>
        /// Default = SolidBrush(Color.Green).
        /// </summary>
        public Brush LeftSideBrush { get; set; }
        /// <summary>
        /// Default = SolidBrush(Color.SkyBlue).
        /// </summary>
        public Brush RightSideBrush { get; set; }
        /// <summary>
        /// Default = SolidBrush(Color.FromArgb(128, Color.Black)).
        /// </summary>
        public Brush CenterLineBrush { get; set; }

        private float progressPercentageInterval;
        /// <summary>
        /// Interval between invokes of ProgressChanged events, in %.
        /// Default = 1.
        /// Set to 0 to always invoke event.
        /// Bound [0, 100].
        /// </summary>
        public float ProgressPercentageInterval
        {
            get { return progressPercentageInterval; }
            set
            {
                if (value < 0)
                    value = 0f;
                else if (value > 100f)
                    value = 100f;

                progressPercentageInterval = value;
            }
        }

        private float detail;
        /// <summary>
        /// The detail of the waveform. Higher value = more points rendered.
        /// Default = 1.5.
        /// Bound [1, 2].
        /// </summary>
        public float Detail
        {
            get { return detail; }
            set
            {
                if (value < 1f)
                    value = 1f;
                else if (value > 2f)
                    value = 2f;

                detail = value;
            }
        }

        /// <summary>
        /// To be used during detection.
        /// Value = 100 * FrameDoneCount / NumFrames.
        /// </summary>
        public float PercentageCompleted { get; private set; }

        /// <summary>
        /// To be used during detection.
        /// </summary>
        public int FrameDoneCount { get; private set; }

        public int NumFrames { get; private set; }
        public float PeakValue { get; private set; }
        public RhythmPattern Pattern { get; private set; }
        public List<List<int>> VolumeSpurts { get; private set; }
        #endregion


        public WaveformGenerator(string filePath, RhythmPattern pattern)
        {
            this.filePath = filePath;

            // Set properties.
            ProgressPercentageInterval = DefaultProgressPercentageInterval;
            Detail = DefaultDetail;
            Direction = DefaultDirection;
            Orientation = DefaultOrientation;
            LeftSideBrush = DefaultLeftSideBrush;
            RightSideBrush = DefaultRightSideBrush;
            CenterLineBrush = DefaultCenterLineBrush;

            // Init fields.
            leftLevelList = new List<float>();
            rightLevelList = new List<float>();
            progressSw = new Stopwatch();

            // Change state.
            State = ReadyState.Started;
            Pattern = pattern;
        }

        #region Public
        /// <summary>
        /// Creates audio stream.
        /// Locks file.
        /// </summary>
        public void CreateStream()
        {
            if (!(State == ReadyState.Started || State == ReadyState.ClosedStream))
                throw new InvalidOperationException("Not ready.");

            // Create stream.
            stream = Bass.BASS_StreamCreateFile(filePath, 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN);
            if (stream == 0)
                throw new Exception(Bass.BASS_ErrorGetCode().ToString());

            // Set number of frames.
            long trackLengthInBytes = Bass.BASS_ChannelGetLength(stream);
            long frameLengthInBytes = Bass.BASS_ChannelSeconds2Bytes(stream, 0.01d);
            NumFrames = (int)Math.Round(1f * trackLengthInBytes / frameLengthInBytes);
#if DEBUG
            Console.WriteLine(stream);
            BASS_CHANNELINFO info = new BASS_CHANNELINFO();
            Bass.BASS_ChannelGetInfo(stream, info);
            Console.WriteLine(info.ToString());
            Console.WriteLine($"track length in bytes from waveformgenerator.cs: {trackLengthInBytes}");
            Console.WriteLine($"how many bytes in 20 ms from waveformgenerator.cs: {frameLengthInBytes}");
            Console.WriteLine($"NumFrames: {NumFrames}");
#endif

            // Change state.
            State = ReadyState.CreatedStream;
        }

        /// <summary>
        /// Detects waveform levels of track.
        /// Subscribe to ProgressChanged event to get progress.
        /// </summary>
        public async Task DetectWaveformLevelsAsync()
        {
            if (!(State == ReadyState.CreatedStream || State == ReadyState.Completed))
                throw new InvalidOperationException("Not ready.");

            // Reset properties and fields.
            PercentageCompleted = 0f;
            FrameDoneCount = 0;
            PeakValue = 0f;
            lastProgressPercentageEventRaised = 0f;
            leftLevelList.Clear();
            rightLevelList.Clear();
            cts = new CancellationTokenSource();

            // Rewind stream.
            Bass.BASS_ChannelSetPosition(stream, 0);

            // Change state.
            State = ReadyState.Running;

            // Start stopwatch.
            progressSw.Restart();


            // Start task.
            await DetectWaveformLevelsInnerAsync();

            // Stop stopwatch.
            progressSw.Stop();

            State = ReadyState.Completed;

            if (Completed != null)
                Completed(this, EventArgs.Empty);
        }

        /// <summary>
        /// Cancels waveform levels detection.
        /// Does not end task instantly.
        /// </summary>
        public void CancelDetection()
        {
            if (State != ReadyState.Running)
                throw new InvalidOperationException("Detection is not in progress.");

            cts.Cancel();
        }

        /// <summary>
        /// Closes stream, cancelling detection if in progress.
        /// Unlocks file.
        /// </summary>
        public void CloseStream()
        {
            if (!(State == ReadyState.CreatedStream || State == ReadyState.Running || State == ReadyState.Completed))
                throw new InvalidOperationException("Not ready.");

            if (State == ReadyState.Running)
                cts.Cancel();

            if (stream != 0)
                Bass.BASS_StreamFree(stream);

            // Change state.
            State = ReadyState.ClosedStream;
        }

        /// <summary>
        /// Creates waveform image.
        /// Can be invoked during or after detection.
        /// </summary>
        /// <returns></returns>
        public Bitmap CreateWaveform(int width, int height)
        {
            if (!(State == ReadyState.Running || State == ReadyState.Completed || State == ReadyState.ClosedStream))
                throw new InvalidOperationException("Not ready.");

            float curPeakValue = PeakValue;
            int curFrameDoneCount = FrameDoneCount;

            int lengthInPixels;
            if (Direction == WaveformDirection.LeftToRight || Direction == WaveformDirection.RightToLeft)
                lengthInPixels = width;
            else
                lengthInPixels = height;

            int numFullRenderFrames = Math.Min((int)Math.Round(Detail * lengthInPixels), NumFrames);

            List<float> leftList = new List<float>();
            List<float> rightList = new List<float>();

            float leftTotal = 0f;
            float rightTotal = 0f;
            int leftCount = 0;
            int rightCount = 0;

            int renderFrameDoneCount = 0;

            for (int i = 0; i < curFrameDoneCount; i++)
            {
                // Get left and right levels.
                float leftLevel = leftLevelList[i];
                float rightLevel = rightLevelList[i];

                leftTotal += leftLevel;
                rightTotal += rightLevel;
                leftCount++;
                rightCount++;

                // Check if reached end of render bin.
                if (i + 1 >= (int)Math.Round(1f * (renderFrameDoneCount + 1) * NumFrames / numFullRenderFrames))
                {
                    // Update left and right render points.
                    leftList.Add(leftTotal / leftCount);
                    rightList.Add(rightTotal / rightCount);

                    // Reset total and count.
                    leftTotal = 0f;
                    rightTotal = 0f;
                    leftCount = 0;
                    rightCount = 0;

                    // Increment render frame done count.
                    renderFrameDoneCount++;
                }
            }

            return WaveformGenerator.CreateWaveformImage(Direction, Orientation, width, height, curPeakValue, numFullRenderFrames,
                leftList.ToArray(), rightList.ToArray(), LeftSideBrush, RightSideBrush, CenterLineBrush);
        }

        /// <summary>
        /// Find the volume spurts in the current audio track
        /// </summary>
        /// <returns></returns>
        public List<List<int>> FindAllVolumeSpurts()
        {
            List<List<int>> myList = new List<List<int>>();

            int i = 0;
            int startIndex = -1;
            // state 0 means current iteration does not involve a volume spurt, state 1 means currently experiencing volume spurt
            int state = 0;
            foreach (float pt in leftLevelList)
            {
                if (pt > this.PeakValue * 0.3)
                {
                    if (startIndex == -1)
                    {
                        if (i == 0)
                            startIndex = i;
                        else
                            startIndex = i - 1;
                        state = 1;
                    }
                }
                else
                {
                    if (state == 1)
                    {
                        myList.Add(new List<int> { startIndex, i });
                        startIndex = -1;
                        state = 0;
                    }
                }

                if (i == (leftLevelList.Count - 1) && startIndex != -1 && state == 1)
                {
                    myList.Add(new List<int> { startIndex, i });
                }

                i++;
            }
            return myList;
        }

        /// <summary>
        /// This method is called under the assumption that the first arg is solution and second is student sample
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="mistakeRangeInMiliseconds"></param>
        /// <returns></returns>
        public static string Compare(WaveformGenerator solution, WaveformGenerator sample, int mistakeRangeInMiliseconds)
        {
            // first align the start of the two files
            // Check if solution tallies with the rhythm pattern, minus away the starting beats
            solution.AlignWith(sample, solution.VolumeSpurts, sample.VolumeSpurts);

            // align finish, identify volume spurts difference in position, since level list is edited, find the spurts again
            solution.VolumeSpurts = solution.FindAllVolumeSpurts();
            sample.VolumeSpurts = sample.FindAllVolumeSpurts();
            List<List<int>> volSpurtsSolution = solution.VolumeSpurts;
            List<List<int>> volSpurtsSample = sample.VolumeSpurts;

            // Further split the volume spurts into bars
            List<List<List<int>>> volSpurtsInBarsSample = sample.SplitIntoBars();
            PrintListOfListOfInt(volSpurtsSolution, "volume spurts solution");
            PrintListOfListOfInt(volSpurtsSample, "volume spurts sample");

            Console.WriteLine("=================================================================");
            Console.WriteLine(solution.leftLevelList.Count);
            Console.WriteLine(sample.leftLevelList.Count);

            string result = "";
            float levelOfMetronomeSpecial = 0f;
            float levelOfMetronome = 0f;
            for (int i = 0; i < solution.Pattern.NumberOfBars + 1; i++)
            {
                if (i == 0)
                {
                    if (volSpurtsInBarsSample[0].Count != solution.Pattern.TimeSignitureTop)
                        return "Please begin with exactly one bar of nothing";

                    if (sample.PeakOf(volSpurtsInBarsSample[0][0]) > sample.PeakOf(volSpurtsInBarsSample[0][1]) * 1.3)
                    {
                        levelOfMetronomeSpecial = sample.PeakOf(volSpurtsInBarsSample[0][0]);
                    }
                    else
                    {
                        levelOfMetronome = sample.PeakOf(volSpurtsInBarsSample[0][1]);
                    }
                }
                else
                {
                    // minus 1 because the pattern doesn't include one bar of nothing
                    List<int> correctIdxPos = solution.Pattern.ParseBarIntoVolSpurts(i - 1);
                    List<List<int>> sampleBar = volSpurtsInBarsSample[i];

                    // correctIdxPos starts from 0 so need to align with the sample's frame number
                    for (int j = 0; j < correctIdxPos.Count; j++)
                    {
                        correctIdxPos[j] = correctIdxPos[j] + sampleBar[0][0];
                    }

                    PrintListOfInt(correctIdxPos, "correctIdxPos");
                    PrintListOfListOfInt(sampleBar, "sample bar");

                    //minus 1 because correctIdxPos include the first beat of next bar
                    List<int> correctNotesIdxInSample = new List<int>();
                    List<int> incorrectNotes = new List<int>();
                    // First find out all correctly played notes
                    for (int j = 0; j < correctIdxPos.Count - 1; j++)
                    {
                        bool success = false;
                        Console.WriteLine($"Finding the {j}th note---------------------------------------------");
                        for (int k = 0; k < sampleBar.Count; k++)
                        {
                            Console.WriteLine($"Math.Abs(sampleBar[{k}][0] - correctIdxPos[{j}]): {Math.Abs(sampleBar[k][0] - correctIdxPos[j])}");
                            Console.WriteLine($"(correctIdxPos[{j + 1}] - correctIdxPos[{j}]): {(correctIdxPos[j + 1] - correctIdxPos[j])}");
                            if (Math.Abs(sampleBar[k][0] - correctIdxPos[j]) < 0.1 * (correctIdxPos[j + 1] - correctIdxPos[j]))
                            {
                                correctNotesIdxInSample.Add(k);
                                success = true;
                                break;
                            }
                        }
                        if (!success)
                            incorrectNotes.Add(j);
                    }
                    PrintListOfInt(incorrectNotes, "incorrect notes' indices");
                    foreach (int note in incorrectNotes)
                    {
                        result += $"The {note + 1}th note is not played correctly";
                    }

                }
            }

            return result;
        }

        public static void PrintListOfListOfListOfInt(List<List<List<int>>> volSpurtsBar, string name)
        {
            Console.WriteLine($"Printing ListOfListOfListOfInt: {name} -----------------------------------------------------------------------------");
            foreach (List<List<int>> bar in volSpurtsBar)
            {
                foreach (List<int> note in bar)
                {
                    Console.WriteLine($"[{note[0]}, {note[1]}]");
                }
            }
        }

        public static void PrintListOfListOfInt(List<List<int>> volSpurts, string name)
        {
            Console.WriteLine($"Printing ListOfListOfInt: {name} -----------------------------------------------------------------------------");
            foreach (List<int> note in volSpurts)
            {
                Console.WriteLine($"[{note[0]}, {note[1]}]");
            }
        }

        public static void PrintListOfInt(List<int> list, string name)
        {
            Console.WriteLine($"Printing ListOfInt: {name} -----------------------------------------------------------------------------");
            foreach (int note in list)
            {
                Console.WriteLine(note);
            }
        }

        /// <summary>
        /// Get the peak value of a volume spurt
        /// </summary>
        /// <param name="volSpurt">the index of volume spurt</param>
        /// <returns></returns>
        public float PeakOf(List<int> volSpurt)
        {
            float largest = 0;
            foreach (int i in volSpurt)
            {
                largest = Math.Max(largest, leftLevelList[i]);
            }
            return largest;
        }

        /// <summary>
        /// Separate volume spurts into music bars
        /// </summary>
        /// <returns></returns>
        public List<List<List<int>>> SplitIntoBars()
        {
            float durationOfBarInTenMS = Pattern.TimeOfBarSeconds * 100;
            List<List<List<int>>> result = new List<List<List<int>>>();
            List<List<int>> tempBar = new List<List<int>>();
            int endIdxOfBar = 0;
            for (int i = 0; i < VolumeSpurts.Count; i++)
            {
                if (VolumeSpurts[i][0] - VolumeSpurts[endIdxOfBar][0] >= durationOfBarInTenMS)
                {
                    endIdxOfBar = i;
                    result.Add(new List<List<int>>(tempBar));
                    tempBar.Clear();
                    tempBar.Add(VolumeSpurts[i]);
                }
                else
                {
                    tempBar.Add(VolumeSpurts[i]);
                }
            }
            result.Add(tempBar);
            return result;
        }

        ///// <summary>
        ///// Split a student sample track into bars base on time
        ///// </summary>
        ///// <param name="solution"></param>
        ///// <param name="notesInBarsSolution"></param>
        ///// <param name="missingNotes"></param>
        ///// <returns></returns>
        //public List<List<List<int>>> SplitIntoBarsSample(WaveformGenerator solution, List<List<List<int>>> notesInBarsSolution, int missingNotes)
        //{
        //    List<List<List<int>>> result = new List<List<List<int>>>();
        //    List<List<int>> tempBar = new List<List<int>>();
        //    if (missingNotes == 0)
        //    {
        //        int endIdx = 0;
        //        foreach (List<List<int>> bar in notesInBarsSolution)
        //        {
        //            for(int i = endIdx; i < endIdx + bar.Count; i++)
        //            {
        //                tempBar.Add(VolumeSpurts[i]);
        //            }
        //            result.Add(new List<List<int>>(tempBar));
        //            endIdx += bar.Count;
        //            tempBar.Clear();
        //        }
        //    } else
        //    {
        //        float barTime = Pattern.TimeOfBarSeconds / 1000; // in miliseconds
        //        int startIdx = 0;
        //        int endIdx = 0;
        //        for (int i = 0; i < Pattern.NumberOfBars; i++)
        //        {
        //            while (true)
        //            {
        //                tempBar.Add(VolumeSpurts[endIdx]);
        //                endIdx++;
        //                if ((VolumeSpurts[endIdx][0] - VolumeSpurts[startIdx][0]) * 10 > barTime)
        //                {
        //                    startIdx = endIdx;
        //                    break;
        //                }
        //            }
        //            result.Add(new List<List<int>>(tempBar));
        //            tempBar.Clear();
        //        }
        //    }
        //    return result;
        //}

        /// <summary>
        /// Align the first beat of the two audio tracks
        /// </summary>
        /// <param name="other"> Sample WaveformGenerator </param>
        /// <param name="volSpurtsSelf"> Identified list of volume spurts of solution </param>
        /// <param name="volSpurtsOther"> Identified list of volume spurts of sample </param>
        public void AlignWith(WaveformGenerator other, List<List<int>> volSpurtsSelf, List<List<int>> volSpurtsOther)
        {
            int solutionFrames = this.NumFrames;
            int sampleFrames = other.NumFrames;
            int usefulSolutionFrames = volSpurtsSelf[volSpurtsSelf.Count - 1][1] - volSpurtsSelf[0][0] + 1;
            int usefulSampleFrames = volSpurtsOther[volSpurtsOther.Count - 1][1] - volSpurtsOther[0][0] + 1;

            // truncate the frames to only those useful ones
            this.TruncateList(volSpurtsSelf[0][0], volSpurtsSelf[volSpurtsSelf.Count - 1][1]);
            other.TruncateList(volSpurtsOther[0][0], volSpurtsOther[volSpurtsOther.Count - 1][1]);

            int diff = usefulSolutionFrames - usefulSampleFrames;
            if (diff > 0)
            {
                other.FillList(diff);
            }
            else if (diff < 0)
            {
                this.FillList(diff);
            }
            this.AdjustNumFrames();
            other.AdjustNumFrames();
        }

        /// <summary>
        /// Cut the audio tracks to include from the first volume spurt to the last volume spurt
        /// </summary>
        /// <param name="front"> Index of first volume spurt </param>
        /// <param name="back"> Index of last volume spurt </param>
        public void TruncateList(int front, int back)
        {
            if (back != this.leftLevelList.Count - 1)
            {
                this.leftLevelList.RemoveRange(back + 1, this.leftLevelList.Count - back - 1);
                this.rightLevelList.RemoveRange(back + 1, this.leftLevelList.Count - back - 1);
            }
            if (front != 0)
            {
                this.leftLevelList.RemoveRange(0, front);
                this.rightLevelList.RemoveRange(0, front);
            }
        }

        /// <summary>
        /// If the two audio tracks are of different length, fill the shorter one with trailing zeros
        /// </summary>
        /// <param name="number"></param>
        public void FillList(int number)
        {
            for (int i = 0; i < Math.Abs(number); i++)
            {
                this.leftLevelList.Add(0f);
                this.rightLevelList.Add(0f);
            }
        }

        /// <summary>
        /// Allow the change of NumFrames field
        /// </summary>
        public void AdjustNumFrames()
        {
            this.NumFrames = this.leftLevelList.Count;
            this.FrameDoneCount = NumFrames;
        }

        /// <summary>
        /// Put the detected levels of audio track to csv file
        /// </summary>
        /// <param name="path"></param>
        public void ToCSV(string path)
        {
            //using (System.IO.StreamWriter sw = File.AppendText(path))
            //{
            //    string result = "";
            //    foreach (float obj in leftLevelList)
            //    {
            //        result += obj.ToString();
            //        result += ",\n";
            //        sw.WriteLine(obj.ToString() + ",");
            //    }
            //    File.AppendAllText(path, appendText);
            //    Console.WriteLine("In ToCSV--------------------------------------------------------------------------------------------");                
            //}
            string result = "";
            foreach (float obj in leftLevelList)
            {
                result += obj.ToString();
                result += "\n";
            }
            File.AppendAllText(path, result);
            Console.WriteLine(result);
            Console.WriteLine("In ToCSV--------------------------------------------------------------------------------------------");
        }
        #endregion


        #region Private
        /// <summary>
        /// Detects waveform levels.
        /// </summary>
        private async Task DetectWaveformLevelsInnerAsync()
        {
            // Create progress reporter.
            IProgress<ProgressChangedEventArgs> prog = new Progress<ProgressChangedEventArgs>(value =>
            {
                // Invoke ProgressChanged event.
                if (ProgressChanged != null)
                    ProgressChanged(this, value);

                // Restart stopwatch.
                progressSw.Restart();
            });

            try
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < NumFrames; i++)
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        // Get left and right levels.
                        float[] levels = new float[2];
                        //float[] fft = new float[4096];
                        Bass.BASS_ChannelGetLevel(stream, levels, 0.01f, BASSLevel.BASS_LEVEL_STEREO);
#if DEBUG
                        //Console.WriteLine($"Position of the playback after getlevel: {Bass.BASS_ChannelGetPosition(stream)}");
#endif
                        //Bass.BASS_ChannelGetData(stream, fft, (int)BASSData.BASS_DATA_FFT4096);
#if DEBUG
                        //Console.WriteLine($"Position of the playback after getdata: {Bass.BASS_ChannelGetPosition(stream)}");
#endif
                        float leftLevel = levels[0];
                        float rightLevel = levels[1];

                        // Update left and right levels.
                        leftLevelList.Add(leftLevel);
                        rightLevelList.Add(rightLevel);

                        // Update peak value.
                        PeakValue = Math.Max(Math.Max(PeakValue, leftLevel), rightLevel);

                        // Increment frame done count.
                        FrameDoneCount++;

                        PercentageCompleted = 100f * FrameDoneCount / NumFrames;

                        if (progressSw.ElapsedMilliseconds >= 1)
                        {
                            if (ProgressPercentageInterval == 0f ||
                                Math.Floor(PercentageCompleted / ProgressPercentageInterval) > Math.Floor(lastProgressPercentageEventRaised / ProgressPercentageInterval))
                            {

                                progressSw.Reset();
                                lastProgressPercentageEventRaised = PercentageCompleted;
                                prog.Report(new ProgressChangedEventArgs(FrameDoneCount, PercentageCompleted));
                            }
                        }
                    }
                    // ~~ Completed. Identify the volume spurts next
                    VolumeSpurts = this.FindAllVolumeSpurts();
                }, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Do nothing.
            }
            catch (Exception)
            {
                // Do nothing.
            }
        }
        #endregion


        #region Private Static
        /// <summary>
        /// Creates waveform image.
        /// Length of level arrays can be less then number of render frames for a partial render.
        /// </summary>
        private static Bitmap CreateWaveformImage(WaveformDirection direction, WaveformSideOrientation sideOrientation,
            int width, int height, float peakValue, int numFullRenderFrames,
            float[] leftLevelArr, float[] rightLevelArr,
            Brush leftSideBrush, Brush rightSideBrush, Brush centerLineBrush)
        {

            // Perform argument checks.
            if (width <= 0)
                throw new ArgumentException("Width is not positive.", "width");
            if (height <= 0)
                throw new ArgumentException("Height is not positive.", "height");
            if (leftLevelArr.Length != rightLevelArr.Length)
                throw new ArgumentException("Left and right level array is not of the same length.", "rightRenderLevelArr");

            if (direction == WaveformDirection.LeftToRight || direction == WaveformDirection.RightToLeft)
            {
                // ~~ Left to right or right to left.
                return CreateHorizontalWaveformImage(direction, sideOrientation,
                        width, height, peakValue, numFullRenderFrames,
                        leftLevelArr, rightLevelArr,
                        leftSideBrush, rightSideBrush, centerLineBrush);
            }
            else
            {
                // ~~ Top to bottom or bottom to top.
                return CreateVerticalWaveformImage(direction, sideOrientation,
                     width, height, peakValue, numFullRenderFrames,
                     leftLevelArr, rightLevelArr,
                     leftSideBrush, rightSideBrush, centerLineBrush);
            }
        }

        /// <summary>
        /// Creates waveform image.
        /// Length of level arrays can be less then number of render frames for a partial render.
        /// </summary>
        private static Bitmap CreateHorizontalWaveformImage(WaveformDirection direction, WaveformSideOrientation sideOrientation,
            int width, int height, float peakValue, int numFullRenderFrames,
            float[] leftLevelArr, float[] rightLevelArr,
            Brush leftSideBrush, Brush rightSideBrush, Brush centerLineBrush)
        {

            int numRenderFrames = leftLevelArr.Length;
            bool isPartialRender = numRenderFrames < numFullRenderFrames;
            double frameThickness = 1d * width / numFullRenderFrames;
            double sideHeight = height / 2d;
            double centerLineY = (height - 1) / 2;

            PointF[] topPointArr = new PointF[numRenderFrames + 3];
            PointF[] bottomPointArr = new PointF[numRenderFrames + 3];

            // Change peak value if partial render.
            if (isPartialRender)
                peakValue = Math.Max(1f, peakValue);

            // Make sure peakValue != 0 to avoid division by zero.
            if (peakValue == 0)
                peakValue = 1f;

            // Add start points.
            if (direction == WaveformDirection.LeftToRight)
            {
                // ~~ Left to right.
                topPointArr[0] = new PointF(0f, (float)centerLineY);
                bottomPointArr[0] = new PointF(0f, (float)centerLineY);
            }
            else
            {
                // ~~ Right to left.
                topPointArr[0] = new PointF(width - 1, (float)centerLineY);
                bottomPointArr[0] = new PointF(width - 1, (float)centerLineY);
            }

            double xLocation = -1d;

            // Add main points.
            for (int i = 0; i < numRenderFrames; i++)
            {
                if (direction == WaveformDirection.LeftToRight)
                {
                    // ~~ Left to right.
                    xLocation = (i * frameThickness + (i + 1) * frameThickness) / 2;
                }
                else
                {
                    // ~~ Right to left.
                    xLocation = width - 1 - ((i * frameThickness + (i + 1) * frameThickness) / 2);
                }

                double topRenderHeight, bottomRenderHeight;

                if (sideOrientation == WaveformSideOrientation.LeftSideOnTopOrLeft)
                {
                    // ~~ Left side on top, right side on bottom.
                    topRenderHeight = 1d * leftLevelArr[i] / peakValue * sideHeight;
                    bottomRenderHeight = 1d * rightLevelArr[i] / peakValue * sideHeight;
                }
                else
                {
                    // ~~ Left side on bottom, right side on top.
                    topRenderHeight = 1d * rightLevelArr[i] / peakValue * sideHeight;
                    bottomRenderHeight = 1d * leftLevelArr[i] / peakValue * sideHeight;
                }

                topPointArr[i + 1] = new PointF((float)xLocation, (float)(centerLineY - topRenderHeight));
                bottomPointArr[i + 1] = new PointF((float)xLocation, (float)(centerLineY + bottomRenderHeight));
            }

            // Add end points.
            if (direction == WaveformDirection.LeftToRight)
            {
                // ~~ Left to right.
                if (isPartialRender)
                {
                    // Draw straight towards line, not to end point.
                    topPointArr[numRenderFrames + 1] = new PointF((float)xLocation, (float)centerLineY);
                    bottomPointArr[numRenderFrames + 1] = new PointF((float)xLocation, (float)centerLineY);
                }
                else
                {
                    // Draw to end point.
                    topPointArr[numRenderFrames + 1] = new PointF(width - 1, (float)centerLineY);
                    bottomPointArr[numRenderFrames + 1] = new PointF(width - 1, (float)centerLineY);
                }
                topPointArr[numRenderFrames + 2] = new PointF(0, (float)centerLineY);
                bottomPointArr[numRenderFrames + 2] = new PointF(0, (float)centerLineY);
            }
            else
            {
                // ~~ Right to left.
                if (isPartialRender)
                {
                    // Draw straight towards line, not to end point.
                    topPointArr[numRenderFrames + 1] = new PointF((float)xLocation, (float)centerLineY);
                    bottomPointArr[numRenderFrames + 1] = new PointF((float)xLocation, (float)centerLineY);
                }
                else
                {
                    // Draw to end point.
                    topPointArr[numRenderFrames + 1] = new PointF(0, (float)centerLineY);
                    bottomPointArr[numRenderFrames + 1] = new PointF(0, (float)centerLineY);
                }
                topPointArr[numRenderFrames + 2] = new PointF(width - 1, (float)centerLineY);
                bottomPointArr[numRenderFrames + 2] = new PointF(width - 1, (float)centerLineY);
            }

            // Create bitmap.
            Bitmap bm = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bm))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw left and right waveform.
                if (sideOrientation == WaveformSideOrientation.LeftSideOnTopOrLeft)
                {
                    // ~~ Left side on top, right side on bottom.
                    g.FillPolygon(leftSideBrush, topPointArr);
                    g.FillPolygon(rightSideBrush, bottomPointArr);
                }
                else
                {
                    // ~~ Left side on bottom, right side on top.
                    g.FillPolygon(leftSideBrush, bottomPointArr);
                    g.FillPolygon(rightSideBrush, topPointArr);
                }
                // Create pen.
                Pen blackPen = new Pen(Color.Black, 3);

                // Create rectangle.
                Rectangle rect = new Rectangle(0, 0, 30, 30);

                // Draw rectangle to screen.
                g.DrawRectangle(blackPen, rect);

                // Draw center line.
                g.FillRectangle(centerLineBrush, 0, (float)(centerLineY - 0.5), width, 1);
            }

            bm.Save("TEST.JPG");
            return bm;
        }

        /// <summary>
        /// Creates waveform image.
        /// Length of level arrays can be less then number of render frames for a partial render.
        /// </summary>
        private static Bitmap CreateVerticalWaveformImage(WaveformDirection direction, WaveformSideOrientation sideOrientation,
            int width, int height, float peakValue, int numFullRenderFrames,
            float[] leftLevelArr, float[] rightLevelArr,
            Brush leftSideBrush, Brush rightSideBrush, Brush centerLineBrush)
        {

            int numRenderFrames = leftLevelArr.Length;
            bool isPartialRender = numRenderFrames < numFullRenderFrames;
            double frameThickness = 1d * height / numFullRenderFrames;
            double sideWidth = width / 2d;
            double centerLineX = (width - 1) / 2;

            PointF[] leftPointArr = new PointF[numRenderFrames + 3];
            PointF[] rightPointArr = new PointF[numRenderFrames + 3];

            // Change peak value if partial render.
            if (isPartialRender)
                peakValue = Math.Max(1f, peakValue);

            // Make sure peakValue != 0 to avoid division by zero.
            if (peakValue == 0)
                peakValue = 1f;

            // Add start points.
            if (direction == WaveformDirection.TopToBottom)
            {
                // ~~ Top to bottom.
                leftPointArr[0] = new PointF((float)centerLineX, 0f);
                rightPointArr[0] = new PointF((float)centerLineX, 0f);
            }
            else
            {
                // ~~ Bottom to top.
                leftPointArr[0] = new PointF((float)centerLineX, height - 1);
                rightPointArr[0] = new PointF((float)centerLineX, height - 1);
            }

            double yLocation = -1d;

            // Add main points.
            for (int i = 0; i < numRenderFrames; i++)
            {
                if (direction == WaveformDirection.TopToBottom)
                {
                    // ~~ Top to bottom.
                    yLocation = (i * frameThickness + (i + 1) * frameThickness) / 2;
                }
                else
                {
                    // ~~ Bottom to top.
                    yLocation = height - 1 - ((i * frameThickness + (i + 1) * frameThickness) / 2);
                }

                double leftRenderWidth, rightRenderWidth;

                if (sideOrientation == WaveformSideOrientation.LeftSideOnTopOrLeft)
                {
                    // ~~ Left side on left, right side on right.
                    leftRenderWidth = 1d * leftLevelArr[i] / peakValue * sideWidth;
                    rightRenderWidth = 1d * rightLevelArr[i] / peakValue * sideWidth;
                }
                else
                {
                    // ~~ Left side on right, right side on left.
                    leftRenderWidth = 1d * rightLevelArr[i] / peakValue * sideWidth;
                    rightRenderWidth = 1d * leftLevelArr[i] / peakValue * sideWidth;
                }

                leftPointArr[i + 1] = new PointF((float)(centerLineX - leftRenderWidth), (float)yLocation);
                rightPointArr[i + 1] = new PointF((float)(centerLineX + leftRenderWidth), (float)yLocation);
            }

            // Add end points.
            if (direction == WaveformDirection.TopToBottom)
            {
                // ~~ Top to bottom.
                if (isPartialRender)
                {
                    // Draw straight towards line, not to end point.
                    leftPointArr[numRenderFrames + 1] = new PointF((float)centerLineX, (float)yLocation);
                    rightPointArr[numRenderFrames + 1] = new PointF((float)centerLineX, (float)yLocation);
                }
                else
                {
                    // Draw to end point.
                    leftPointArr[numRenderFrames + 1] = new PointF((float)centerLineX, height - 1);
                    rightPointArr[numRenderFrames + 1] = new PointF((float)centerLineX, height - 1);
                }
                leftPointArr[numRenderFrames + 2] = new PointF((float)centerLineX, 0f);
                rightPointArr[numRenderFrames + 2] = new PointF((float)centerLineX, 0f);
            }
            else
            {
                // ~~ Bottom to top.
                if (isPartialRender)
                {
                    // Draw straight towards line, not to end point.
                    leftPointArr[numRenderFrames + 1] = new PointF((float)centerLineX, (float)yLocation);
                    rightPointArr[numRenderFrames + 1] = new PointF((float)centerLineX, (float)yLocation);
                }
                else
                {
                    // Draw to end point.
                    leftPointArr[numRenderFrames + 1] = new PointF((float)centerLineX, 0f);
                    rightPointArr[numRenderFrames + 1] = new PointF((float)centerLineX, 0f);
                }
                leftPointArr[numRenderFrames + 2] = new PointF((float)centerLineX, height - 1);
                rightPointArr[numRenderFrames + 2] = new PointF((float)centerLineX, height - 1);
            }

            // Create bitmap.
            Bitmap bm = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bm))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw left and right waveform.
                if (sideOrientation == WaveformSideOrientation.LeftSideOnTopOrLeft)
                {
                    // ~~ Left side on left, right side on right.
                    g.FillPolygon(leftSideBrush, leftPointArr);
                    g.FillPolygon(rightSideBrush, rightPointArr);
                }
                else
                {
                    // ~~ Left side on right, right side on left.
                    g.FillPolygon(leftSideBrush, rightPointArr);
                    g.FillPolygon(rightSideBrush, leftPointArr);
                }

                // Draw center line.
                g.FillRectangle(centerLineBrush, (float)(centerLineX - 0.5), 0, 1, height);
            }

            return bm;
        }
        #endregion
    }
}
