using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Core
{
    public class JNBandBalance
    {
        /// <summary>
        /// 账面余额 
        /// </summary>
        public double bokbal
        {
            get;
            set;
        }

        /// <summary>
        /// 有效余额 
        /// </summary>
        public double avabal
        {
            get;
            set;
        }

        /// <summary>
        /// 圈存余额 
        /// </summary>
        public double stpamt
        {
            get;
            set;
        }

        /// <summary>
        /// 透资限额 
        /// </summary>
        public double ovramt
        {
            get;
            set;
        }
    }
}
