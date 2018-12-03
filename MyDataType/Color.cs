using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace IsolineEditing
{
    public static class ColorHelper
    {
        public static float[] byte2float(Color c)
        {
            float[] cf = new float[3];
            cf[0] = (float)((int)c.R)/255; 
            cf[1] = (float)((int)c.G)/255; 
            cf[2] = (float)((int)c.B)/255;
            return cf;
        }
        public static float byte2float(byte c)
        {
            return (float)c/255;
        }
    }
}
