using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Core
{
   public  class JNQTYConversionPara
    {

        /// <summary>
        /// [源数量]字段的标识 ：单据上的 数量 字段标识
        /// </summary>
       public   string SrcQtyFldKey
       {
           get;
           set;
       }




        /// <summary>
        /// [目标数量]字段的标识：单据上的  辅助数量（即总酶活量）  字段
        /// </summary>
        public   string DestQtyFldKey
        {
            get;
            set;
        }






        /// <summary>
        /// [换算率]字段的标识：单据上的  单位酶活量 字段
        /// </summary>
        public   string ConverRateFldKey
        {
            get;
            set;
        }





    }
}
