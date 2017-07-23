using JN.K3.YDL.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.QTYConversion
{
    /// <summary>
    /// 单位数量与酶活量的换算
    /// </summary>
    [Description("采购退料单-单位数量与酶活量的换算")]
    public class MRBQTYConversion : JNQTYConversion
    {
        /// <summary>
        /// 转换的字段参数
        /// </summary>
        public override List<JNQTYConversionPara> ConvertFldKey
        {
            get
            {
                List<JNQTYConversionPara> para = new List<JNQTYConversionPara>();
                para.Add(new JNQTYConversionPara()
                {
                    ConverRateFldKey = "FJNUnitEnzymes",
                    SrcQtyFldKey = "FRMREALQTY",
                    DestQtyFldKey = "FExtAuxUnitQty"
                });
               

                return para;
            }
        }
    }
}
