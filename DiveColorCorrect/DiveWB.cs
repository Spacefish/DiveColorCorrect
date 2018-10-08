using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;

namespace DiveColorCorrect
{
    public class DiveWB : OpenCvSharp.XPhoto.WhiteBalancer
    {
        public override void BalanceWhite(InputArray src, OutputArray dst)
        {
            throw new NotImplementedException();
        }
    }
}
