
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM.STK
{
    [Description("采购入库表单插件")]
    public class JN_STK_InStock:AbstractBillPlugIn
    {
        
        public override void AfterCreateModelData(EventArgs e)
        {
            base.AfterCreateModelData(e);
            DynamicObject DataObjs = this.Model.DataObject as DynamicObject;
            var Data = DataObjs["InStockEntry"] as DynamicObjectCollection;
            var srcbill = Data[0]["SRCBillNo"];
            string sql = string.Format(@"select t4.FTaxPrice,t4.FPRICE,t2.FENTRYID from T_PUR_Receive t1 inner join T_PUR_ReceiveEntry t2 on t1.fid=t2.fid
                                         inner join T_PUR_ReceiveEntry_LK t3 on t3.fentryid=t2.fentryid inner join t_PUR_POOrderEntry_F t4 on t3.FSID=t4.FENTRYID  where t1.fbillno='{0}'", srcbill);
            var objs = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
            if (objs != null && objs.Count > 0)
            {
                int i = 0;
                foreach (var item in objs)
                {
                    this.View.Model.SetValue("FPRICE", item["FPRICE"], i);
                    this.View.Model.SetValue("FTaxPrice", item["FTaxPrice"], i);                  
                    i++;
                }
                this.View.UpdateView();
            }
        }

    }
}
