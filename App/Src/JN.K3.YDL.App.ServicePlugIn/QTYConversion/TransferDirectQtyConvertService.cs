using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Util;
using Kingdee.BOS.Util;
using Kingdee.BOS.Orm.DataEntity;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using JN.K3.YDL.Core;
using System.ComponentModel;



namespace JN.K3.YDL.App.ServicePlugIn.QTYConversion
{

    /// <summary>
    /// 单位数量与酶活量的换算
    /// </summary>
     [Description("直接调拨单-单位数量与酶活量的换算")]
    public class TransferDirectQtyConvertService : JNQtyConvertService
    {

         

        /// <summary>
        /// 转换的字段参数
        /// </summary>
        public override  List<JNQTYConversionPara> ConvertFldKey
        {
            get
            {
                List<JNQTYConversionPara> para = new List<JNQTYConversionPara>();
                para.Add(new JNQTYConversionPara()
                {
                    ConverRateFldKey = "FJNUnitEnzymes",
                    SrcQtyFldKey = "FQty",
                    DestQtyFldKey = "FExtAuxUnitQty"
                });
                
                return para;
            }
        }

        /// <summary>
        /// 转换的组织字段参数
        /// </summary>

        public override string OrgIdFldKey
        {
            get
            {
                return "FStockOutOrgId";
            }
        }




    }


}
