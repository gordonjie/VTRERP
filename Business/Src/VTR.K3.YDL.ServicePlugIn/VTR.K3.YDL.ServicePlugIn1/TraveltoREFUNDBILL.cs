using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.CN.App.Core;
using Kingdee.K3.FIN.ServiceHelper;


namespace VTR.K3.YDL.ServicePlugIn
{
    [Description("扩展-差旅费报销单下推退款单备注下推服务端插件")]
    public class TraveltoREFUNDBILL : AbstractConvertPlugIn
    {
        /// <summary>
        /// 最后触发：单据转换后事件
        /// </summary>
        /// <param name="e"/>
        public override void AfterConvert(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.AfterConvertEventArgs e)
        {
            base.AfterConvert(e);
            foreach (ExtendedDataEntity entity in e.Result.FindByEntityKey("FBillHead"))
            {
                decimal num = Convert.ToDecimal(entity["EXCHANGERATE"]);
                entity.DataEntity["REFUNDTOTALAMOUNT"] = Convert.ToDecimal(entity["REFUNDTOTALAMOUNTFOR"]) * num;
                entity.DataEntity["REALREFUNDAMOUNT"] = Convert.ToDecimal(entity["REALREFUNDAMOUNTFOR"]) * num;
                DynamicObjectCollection objects = entity["REFUNDBILLENTRY"] as DynamicObjectCollection;
                if (objects == null)
                {
                    break;
                }
                foreach (DynamicObject obj2 in objects)
                {
                    int rows = Convert.ToInt32(obj2["seq"]) - 1;
                    ExtendedDataEntity rowdata = e.Result.FindByEntityKey("FRefundBillSrcEntity")[rows];
                    obj2["PURPOSEID_Id"] = 20595;
                    DynamicObject obj3 = CNCommonFunction.GetDynamicObjectByID(base.Context, "CN_RECPAYPURPOSE", 20595);
                    obj2["PURPOSEID"]=obj3;
                    obj2["NOTE"] = rowdata["FSRCNOTE"];
                }
            }

        }
    } 
}
