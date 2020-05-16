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
using System.Threading;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace WaveformGenerator {
    /// <summary>
    /// Generates audio waveform async.
    /// </summary>
    public class WaveformGenerator {

        #region Enums
        public enum ReadyState {
            Started,
            CreatedStream,
            Running,
            Completed,
            ClosedStream,
        }

        public enum WaveformDirection {
            LeftToRight,
            RightToLeft,
            TopToBottom,
            BottomToTop,
        }

        public enum WaveformSideOrientation {
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
        public class ProgressChangedEventArgs : EventArgs {
            public int FrameDoneCount { get; private set; }
            public float PercentageCompleted { get; private set; }

            public ProgressChangedEventArgs(int frameDoneCount, float percentageCompleted) {
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
        public float ProgressPercentageInterval {
            get { return progressPercentageInterval; }
            set {
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
        public float Detail {
            get { return detail; }
            set {
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
        #endregion


        public WaveformGenerator(string filePath) {
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
        }

        #region Public
        /// <summary>
        /// Creates audio stream.
        /// Locks file.
        /// </summary>
        public void CreateStream() {
            if (!(State == ReadyState.Started || State == ReadyState.ClosedStream))
                throw new InvalidOperationException("Not ready.");

            // Create stream.
            stream = Bass.BASS_StreamCreateFile(filePath, 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN);
            if (stream == 0)
                throw new Exception(Bass.BASS_ErrorGetCode().ToString());

            // Set number of frames.
            long trackLengthInBytes = Bass.BASS_ChannelGetLength(stream);
            long frameLengthInBytes = Bass.BASS_ChannelSeconds2Bytes(stream, 0.02d);
            NumFrames = (int) Math.Round(1f * trackLengthInBytes / frameLengthInBytes);
#if DEBUG
            Console.WriteLine(stream);
            BASS_CHANNELINFO info = new BASS_CHANNELINFO();
            Bass.BASS_ChannelGetInfo(stream, info);
            Console.WriteLine(info.ToString());
            Console.WriteLine($"track length in bytes from waveformgenerator.cs: {trackLengthInBytes}");
            Console.WriteLine($"how many bytes in 20 ms from waveformgenerator.cs: {frameLengthInBytes}");

#endif

            // Change state.
            State = ReadyState.CreatedStream;
        }

        /// <summary>
        /// Detects waveform levels of track.
        /// Subscribe to ProgressChanged event to get progress.
        /// </summary>
        public async Task DetectWaveformLevelsAsync() {
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

            // ~~ Completed.

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
        public void CancelDetection() {
            if (State != ReadyState.Running)
                throw new InvalidOperationException("Detection is not in progress.");

            cts.Cancel();
        }

        /// <summary>
        /// Closes stream, cancelling detection if in progress.
        /// Unlocks file.
        /// </summary>
        public void CloseStream() {
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
        public Bitmap CreateWaveform(int width, int height) {
            if (!(State == ReadyState.Running || State == ReadyState.Completed || State == ReadyState.ClosedStream))
                throw new InvalidOperationException("Not ready.");

            float curPeakValue = PeakValue;
            int curFrameDoneCount = FrameDoneCount;

            int lengthInPixels;
            if (Direction == WaveformDirection.LeftToRight || Direction == WaveformDirection.RightToLeft)
                lengthInPixels = width;
            else
                lengthInPixels = height;

            int numFullRenderFrames = Math.Min((int) Math.Round(Detail * lengthInPixels), NumFrames);

            List<float> leftList = new List<float>();
            List<float> rightList = new List<float>();

            float leftTotal = 0f;
            float rightTotal = 0f;
            int leftCount = 0;
            int rightCount = 0;

            int renderFrameDoneCount = 0;

            for (int i = 0; i < curFrameDoneCount; i++) {
                // Get left and right levels.
                float leftLevel = leftLevelList[i];
                float rightLevel = rightLevelList[i];

                leftTotal += leftLevel;
                rightTotal += rightLevel;
                leftCount++;
                rightCount++;

                // Check if reached end of render bin.
                if (i + 1 >= (int) Math.Round(1f * (renderFrameDoneCount + 1) * NumFrames / numFullRenderFrames)) {
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

        public List<List<int>> FindAllVolumeSpurts()
        {
            List<List<int>> myList = new List<List<int>>();

            int i = 0;
            int startIndex = -1;
            // state 0 means current iteration does not involve a volume spurt, state 1 means currently experiencing volume spurt
            int state = 0;
            foreach (float pt in leftLevelList)
            {
                if (pt > 0.1f)
                {
                    if (startIndex == -1)
                    {
                        if (i == 0)
                            startIndex = i;
                        else
                            startIndex = i - 1;
                        state = 1;
                    }
                } else
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

        public string CompareTo(WaveformGenerator other, int mistakeRangeInMiliseconds)
        {
            // first align the start of the two files
            List<List<int>> volSpurtsSelf = this.FindAllVolumeSpurts();
            List<List<int>> volSpurtsOther = other.FindAllVolumeSpurts();
            int numOfVolSpurtsSelf = volSpurtsSelf.Count;
            int numOfVolSpurtsOther = volSpurtsOther.Count;
#if DEBUG
            Console.WriteLine("Before aligning----------------------------------------------------------------");
            Console.WriteLine("Volumn Spurt List:----------------------------------------------------------------");
            foreach (List<int> i in volSpurtsSelf)
            {
                Console.WriteLine($"{i[0]}, {i[1]}");
            }
            Console.WriteLine("leftLevelList:----------------------------------------------------------------");
            foreach (float i in leftLevelList)
            {
                Console.WriteLine($"{i}");
            }
#endif
            if (numOfVolSpurtsOther != numOfVolSpurtsSelf)
                return $"Different number of beats detected, Solution has {numOfVolSpurtsSelf} spurts and Sample has {numOfVolSpurtsOther} spurts";
            else
                this.AlignWith(other, volSpurtsSelf, volSpurtsOther, numOfVolSpurtsSelf, numOfVolSpurtsOther);

            // align finish, identify volume spurts difference in position, since level list is edited, find the spurts again
            volSpurtsSelf = this.FindAllVolumeSpurts();
            volSpurtsOther = other.FindAllVolumeSpurts();
            numOfVolSpurtsSelf = volSpurtsSelf.Count;
            numOfVolSpurtsOther = volSpurtsOther.Count;
            string result = "";
            for (int i = 0; i < numOfVolSpurtsOther; i++)
            {
                int diffInMiliseconds = (volSpurtsOther[i][0] - volSpurtsSelf[i][0]) * 20;
                if (Math.Abs(diffInMiliseconds) < mistakeRangeInMiliseconds)
                {
                    result += $"Beat {i} has no problem!";
                }
                else
                {
                    if (diffInMiliseconds > 0)
                    {
                        result += $"Beat {i} is a little too late!";
                    }
                    else
                    {
                        result += $"Beat {i} is a little too early!";
                    }
                }
            }

            return result;
        }

        public void AlignWith(WaveformGenerator other, List<List<int>> volSpurtsSelf, List<List<int>> volSpurtsOther, int numOfVolSpurtsSelf, int numOfVolSpurtsOther)
        {
            int solutionFrames = this.NumFrames;
            int sampleFrames = other.NumFrames;
            int usefulSolutionFrames = volSpurtsSelf[numOfVolSpurtsSelf - 1][1] - volSpurtsSelf[0][0] + 1;
            int usefulSampleFrames = volSpurtsOther[numOfVolSpurtsOther - 1][1] - volSpurtsOther[0][0] + 1;

            // truncate the frames to only those useful ones
            this.TruncateList(volSpurtsSelf[0][0], volSpurtsSelf[numOfVolSpurtsSelf - 1][1]);
            other.TruncateList(volSpurtsOther[0][0], volSpurtsOther[numOfVolSpurtsOther - 1][1]);

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

        public void TruncateList (int front, int back)
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

        public void FillList (int number)
        {
            for(int i = 0; i < number; i++)
            {
                this.leftLevelList.Add(0f);
                this.rightLevelList.Add(0f);
            }
        }

        public void AdjustNumFrames()
        {
            this.NumFrames = this.leftLevelList.Count;
            this.FrameDoneCount = NumFrames;
        }
        #endregion


        #region Private
        /// <summary>
        /// Detects waveform levels.
        /// </summary>
        private async Task DetectWaveformLevelsInnerAsync() {
            // Create progress reporter.
            IProgress<ProgressChangedEventArgs> prog = new Progress<ProgressChangedEventArgs>(value => {
                // Invoke ProgressChanged event.
                if (ProgressChanged != null)
                    ProgressChanged(this, value);

                // Restart stopwatch.
                progressSw.Restart();
            });

            try {
                await Task.Run(() => {
                    for (int i = 0; i < NumFrames; i++) {
                        cts.Token.ThrowIfCancellationRequested();

                        // Get left and right levels.
                        float[] levels = new float[2];
                        Bass.BASS_ChannelGetLevel(stream, levels);
                        float leftLevel = levels[0];
                        float rightLevel = levels[1];
#if DEBUG
                        Console.WriteLine($"Left level: {leftLevel}, Right level: {rightLevel}");
#endif

                        // Update left and right levels.
                        leftLevelList.Add(leftLevel);
                        rightLevelList.Add(rightLevel);

                        // Update peak value.
                        PeakValue = Math.Max(Math.Max(PeakValue, leftLevel), rightLevel);

                        // Increment frame done count.
                        FrameDoneCount++;

                        PercentageCompleted = 100f * FrameDoneCount / NumFrames;

                        if (progressSw.ElapsedMilliseconds >= 1) {
                            if (ProgressPercentageInterval == 0f ||
                                Math.Floor(PercentageCompleted / ProgressPercentageInterval) > Math.Floor(lastProgressPercentageEventRaised / ProgressPercentageInterval)) {

                                progressSw.Reset();
                                lastProgressPercentageEventRaised = PercentageCompleted;
                                prog.Report(new ProgressChangedEventArgs(FrameDoneCount, PercentageCompleted));
                            }
                        }
                    }
                }, cts.Token);
            } catch (OperationCanceledException) {
                // Do nothing.
            } catch (Exception) {
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
            Brush leftSideBrush, Brush rightSideBrush, Brush centerLineBrush) {

            // Perform argument checks.
            if (width <= 0)
                throw new ArgumentException("Width is not positive.", "width");
            if (height <= 0)
                throw new ArgumentException("Height is not positive.", "height");
            if (leftLevelArr.Length != rightLevelArr.Length)
                throw new ArgumentException("Left and right level array is not of the same length.", "rightRenderLevelArr");

            if (direction == WaveformDirection.LeftToRight || direction == WaveformDirection.RightToLeft) {
                // ~~ Left to right or right to left.
                return CreateHorizontalWaveformImage(direction, sideOrientation,
                        width, height, peakValue, numFullRenderFrames,
                        leftLevelArr, rightLevelArr,
                        leftSideBrush, rightSideBrush, centerLineBrush);
            } else {
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
            Brush leftSideBrush, Brush rightSideBrush, Brush centerLineBrush) {

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
            if (direction == WaveformDirection.LeftToRight) {
                // ~~ Left to right.
                topPointArr[0] = new PointF(0f, (float) centerLineY);
                bottomPointArr[0] = new PointF(0f, (float) centerLineY);
            } else {
                // ~~ Right to left.
                topPointArr[0] = new PointF(width - 1, (float) centerLineY);
                bottomPointArr[0] = new PointF(width - 1, (float) centerLineY);
            }

            double xLocation = -1d;

            // Add main points.
            for (int i = 0; i < numRenderFrames; i++) {
                if (direction == WaveformDirection.LeftToRight) {
                    // ~~ Left to right.
                    xLocation = (i * frameThickness + (i + 1) * frameThickness) / 2;
                } else {
                    // ~~ Right to left.
                    xLocation = width - 1 - ((i * frameThickness + (i + 1) * frameThickness) / 2);
                }

                double topRenderHeight, bottomRenderHeight;

                if (sideOrientation == WaveformSideOrientation.LeftSideOnTopOrLeft) {
                    // ~~ Left side on top, right side on bottom.
                    topRenderHeight = 1d * leftLevelArr[i] / peakValue * sideHeight;
                    bottomRenderHeight = 1d * rightLevelArr[i] / peakValue * sideHeight;
                } else {
                    // ~~ Left side on bottom, right side on top.
                    topRenderHeight = 1d * rightLevelArr[i] / peakValue * sideHeight;
                    bottomRenderHeight = 1d * leftLevelArr[i] / peakValue * sideHeight;
                }

                topPointArr[i + 1] = new PointF((float) xLocation, (float) (centerLineY - topRenderHeight));
                bottomPointArr[i + 1] = new PointF((float) xLocation, (float) (centerLineY + bottomRenderHeight));
            }

            // Add end points.
            if (direction == WaveformDirection.LeftToRight) {
                // ~~ Left to right.
                if (isPartialRender) {
                    // Draw straight towards line, not to end point.
                    topPointArr[numRenderFrames + 1] = new PointF((float) xLocation, (float) centerLineY);
                    bottomPointArr[numRenderFrames + 1] = new PointF((float) xLocation, (float) centerLineY);
                } else {
                    // Draw to end point.
                    topPointArr[numRenderFrames + 1] = new PointF(width - 1, (float) centerLineY);
                    bottomPointArr[numRenderFrames + 1] = new PointF(width - 1, (float) centerLineY);
                }
                topPointArr[numRenderFrames + 2] = new PointF(0, (float) centerLineY);
                bottomPointArr[numRenderFrames + 2] = new PointF(0, (float) centerLineY);
            } else {
                // ~~ Right to left.
                if (isPartialRender) {
                    // Draw straight towards line, not to end point.
                    topPointArr[numRenderFrames + 1] = new PointF((float) xLocation, (float) centerLineY);
                    bottomPointArr[numRenderFrames + 1] = new PointF((float) xLocation, (float) centerLineY);
                } else {
                    // Draw to end point.
                    topPointArr[numRenderFrames + 1] = new PointF(0, (float) centerLineY);
                    bottomPointArr[numRenderFrames + 1] = new PointF(0, (float) centerLineY);
                }
                topPointArr[numRenderFrames + 2] = new PointF(width - 1, (float) centerLineY);
                bottomPointArr[numRenderFrames + 2] = new PointF(width - 1, (float) centerLineY);
            }

            // Create bitmap.
            Bitmap bm = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bm)) {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw left and right waveform.
                if (sideOrientation == WaveformSideOrientation.LeftSideOnTopOrLeft) {
                    // ~~ Left side on top, right side on bottom.
                    g.FillPolygon(leftSideBrush, topPointArr);
                    g.FillPolygon(rightSideBrush, bottomPointArr);
                } else {
                    // ~~ Left side on bottom, right side on top.
                    g.FillPolygon(leftSideBrush, bottomPointArr);
                    g.FillPolygon(rightSideBrush, topPointArr);
                }

                // Draw center line.
                g.FillRectangle(centerLineBrush, 0, (float) (centerLineY - 0.5), width, 1);
            }

            return bm;
        }

        /// <summary>
        /// Creates waveform image.
        /// Length of level arrays can be less then number of render frames for a partial render.
        /// </summary>
        private static Bitmap CreateVerticalWaveformImage(WaveformDirection direction, WaveformSideOrientation sideOrientation,
            int width, int height, float peakValue, int numFullRenderFrames,
            float[] leftLevelArr, float[] rightLevelArr,
            Brush leftSideBrush, Brush rightSideBrush, Brush centerLineBrush) {

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
            if (direction == WaveformDirection.TopToBottom) {
                // ~~ Top to bottom.
                leftPointArr[0] = new PointF((float) centerLineX, 0f);
                rightPointArr[0] = new PointF((float) centerLineX, 0f);
            } else {
                // ~~ Bottom to top.
                leftPointArr[0] = new PointF((float) centerLineX, height - 1);
                rightPointArr[0] = new PointF((float) centerLineX, height - 1);
            }

            double yLocation = -1d;

            // Add main points.
            for (int i = 0; i < numRenderFrames; i++) {
                if (direction == WaveformDirection.TopToBottom) {
                    // ~~ Top to bottom.
                    yLocation = (i * frameThickness + (i + 1) * frameThickness) / 2;
                } else {
                    // ~~ Bottom to top.
                    yLocation = height - 1 - ((i * frameThickness + (i + 1) * frameThickness) / 2);
                }

                double leftRenderWidth, rightRenderWidth;

                if (sideOrientation == WaveformSideOrientation.LeftSideOnTopOrLeft) {
                    // ~~ Left side on left, right side on right.
                    leftRenderWidth = 1d * leftLevelArr[i] / peakValue * sideWidth;
                    rightRenderWidth = 1d * rightLevelArr[i] / peakValue * sideWidth;
                } else {
                    // ~~ Left side on right, right side on left.
                    leftRenderWidth = 1d * rightLevelArr[i] / peakValue * sideWidth;
                    rightRenderWidth = 1d * leftLevelArr[i] / peakValue * sideWidth;
                }

                leftPointArr[i + 1] = new PointF((float) (centerLineX - leftRenderWidth), (float) yLocation);
                rightPointArr[i + 1] = new PointF((float) (centerLineX + leftRenderWidth), (float) yLocation);
            }

            // Add end points.
            if (direction == WaveformDirection.TopToBottom) {
                // ~~ Top to bottom.
                if (isPartialRender) {
                    // Draw straight towards line, not to end point.
                    leftPointArr[numRenderFrames + 1] = new PointF((float) centerLineX, (float) yLocation);
                    rightPointArr[numRenderFrames + 1] = new PointF((float) centerLineX, (float) yLocation);
                } else {
                    // Draw to end point.
                    leftPointArr[numRenderFrames + 1] = new PointF((float) centerLineX, height - 1);
                    rightPointArr[numRenderFrames + 1] = new PointF((float) centerLineX, height - 1);
                }
                leftPointArr[numRenderFrames + 2] = new PointF((float) centerLineX, 0f);
                rightPointArr[numRenderFrames + 2] = new PointF((float) centerLineX, 0f);
            } else {
                // ~~ Bottom to top.
                if (isPartialRender) {
                    // Draw straight towards line, not to end point.
                    leftPointArr[numRenderFrames + 1] = new PointF((float) centerLineX, (float) yLocation);
                    rightPointArr[numRenderFrames + 1] = new PointF((float) centerLineX, (float) yLocation);
                } else {
                    // Draw to end point.
                    leftPointArr[numRenderFrames + 1] = new PointF((float) centerLineX, 0f);
                    rightPointArr[numRenderFrames + 1] = new PointF((float) centerLineX, 0f);
                }
                leftPointArr[numRenderFrames + 2] = new PointF((float) centerLineX, height - 1);
                rightPointArr[numRenderFrames + 2] = new PointF((float) centerLineX, height - 1);
            }

            // Create bitmap.
            Bitmap bm = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bm)) {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw left and right waveform.
                if (sideOrientation == WaveformSideOrientation.LeftSideOnTopOrLeft) {
                    // ~~ Left side on left, right side on right.
                    g.FillPolygon(leftSideBrush, leftPointArr);
                    g.FillPolygon(rightSideBrush, rightPointArr);
                } else {
                    // ~~ Left side on right, right side on left.
                    g.FillPolygon(leftSideBrush, rightPointArr);
                    g.FillPolygon(rightSideBrush, leftPointArr);
                }

                // Draw center line.
                g.FillRectangle(centerLineBrush, (float) (centerLineX - 0.5), 0, 1, height);
            }

            return bm;
        }
        #endregion
    }
}
