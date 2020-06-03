using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaveformGenerator
{
    /// <summary>
    /// A class that contains information about a pattern. A standard pattern would be,
    /// key name/octave number/duration. For example,
    /// a/4/q means a quarter A4 note. The sequence of duration in descending order will be:
    /// f, h, q, 8, 16, 32, 64; for rest will be: fr, hr, qr, 8r, 16r, 32r, 64r
    /// </summary>
    public class RhythmPattern
    {
        public int TimeSignitureTop { get; set; }
        public int TimeSignitureBottom { get; set; }
        public int BPM { get; set; }
        public int NumberOfBars { get; set; }
        public float TimeOfBarSeconds { get; set; }
        public float TimeOfBeatSeconds { get; set; }
        public List<List<string>> Pattern { get; set; }

        public RhythmPattern(int top, int bottom, int bpm, int bars, List<List<string>> pattern)
        {
            BPM = bpm;
            TimeSignitureTop = top;
            NumberOfBars = bars;
            Pattern = pattern;
        }

        public override string ToString()
        {
            string result = $"Time signiture: {TimeSignitureTop}/{TimeSignitureBottom}, BPM: {BPM}, Pattern: ";
            foreach (string str in Pattern[0])
            {
                result += str;
                result += " ";
            }
            result += TimeOfBarSeconds.ToString();
            return result;
        }

        /// <summary>
        /// This is to calculte how much time is a note worth relative to the duration of a beat
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        public float GetTimeFractionOfNote(string note)
        {
            char[] spearator = { '/' };

            // using the method 
            String[] strlist = note.Split(spearator);
            bool isRest = false;
            bool isDot = false;
            if (strlist[2].Contains("r"))
                isRest = true;
            else if (strlist[2].Contains("d"))
                isDot = true;
            if (isDot && isRest)
            {
                strlist[2] = strlist[2].Remove(strlist[2].Length - 2, 2);
            }
            else if (isRest || isDot)
            {
                strlist[2] = strlist[2].Remove(strlist[2].Length - 1, 1);
            }

            try
            {
                float divider = (float)int.Parse(strlist[2]);
                if (isDot)
                    divider /= 1.5f;
                return divider;
            }
            catch (FormatException e)
            {
                float ans = 0;

                switch (strlist[2])
                {
                    case "q":
                        ans = 4f;
                        break;
                    case "h":
                        ans = 2f;
                        break;
                    case "f":
                        ans = 1f;
                        break;
                    default:
                        ans = -1f;
                        break;
                }

                if (isDot)
                    ans /= 1.5f;
                return ans;
            }
        }

        /// <summary>
        /// Do some math to get the time value of a note
        /// </summary>
        /// <param name="note">it is in 1, 2, 4, 8, 16, representing what kind of note it is</param>
        /// <returns></returns>
        public float GetPhysicalTimeOfNote(float note)
        {
            return (float)TimeOfBeatSeconds * ((float)TimeSignitureBottom / note);
        }

        /// <summary>
        /// Given a bar of notes, turn them into volume spurt offset index
        /// </summary>
        /// <param name="bar"></param>
        /// <returns>A list with correct indices on where should there be a sound,
        /// the last entry in the list is the end of bar, not counted</returns>
        public List<int> ParseBarIntoVolSpurts(int barNum)
        {
            List<int> result = new List<int>();
            int offsetFrames = 0;
            float beatNum = 0;
            foreach (string note in Pattern[barNum])
            {
                if (IsRest(note))
                {
                    // a rest on a beat should still have a sound
                    if (beatNum % 1 == 0)
                        result.Add(offsetFrames);
                }
                else
                {
                    result.Add(offsetFrames);
                }
                float timeValue = GetTimeFractionOfNote(note);
                beatNum += (float)TimeSignitureBottom / timeValue;
                offsetFrames += (int)Math.Floor((float)GetPhysicalTimeOfNote(timeValue) * 1000f / Global.LengthOfFrameMS);

                Console.WriteLine($"offsetFrames: {offsetFrames} --------------------------------------");
            }
            result.Add(offsetFrames);
            return result;
        }

        public bool IsRest(string note)
        {
            return note.Contains("r");
        }

    }
}
