using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaveformGenerator
{
    static class Global
    {
        //static fields  
        public static int LengthOfFrameMS;

        //static constructor  
        static Global()
        {
            LengthOfFrameMS = 10;
        }
    }
}
