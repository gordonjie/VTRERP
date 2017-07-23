using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.FIN
{
    [Description("应付单-表单发票号码插件")]
    public class YDL_FIN_Payable_INVOICENO : AbstractBillPlugIn
    {
        private DynamicObject currRow = null;
        private int rowindex = 0;
        public override void 
        {
            base.DataChanged(e);
            string str = e.Field.Key.ToUpper();
            if (str == "FMATERIALID")
            {
                this.Model.TryGetEntryCurrentRow("FEntity", out currRow, out rowindex);
                int index = Convert.ToInt32(currRow["SEQ"]) - 1;
                DynamicObject FMATERIALID = this.View.Model.GetValue("FMATERIALID", index) as DynamicObject;
                if (FMATERIALID == null) return;
                var materid = FMATERIALID["id"];
                var sql = string.Format(@"select t3.FBILLNO,INVOICENO =STUFF((select ','+FINVOICENO  from T_IV_SALESIC  tb2
right join T_IV_SALESICENTRY tb3 on tb2.FID=tb3.FID
join t_AR_receivable tb4 on tb3.FSRCBILLNO=tb4.FBILLNO
where tb4.FBILLNO=t3.FBILLNO and tb3.FENTRYID=t2.FENTRYID 
and t1.FCANCELSTATUS='A' and t1.FDOCUMENTSTATUS='C'
FOR xml path('')), 1, 1, '')
from T_IV_SALESIC t1 
join T_IV_SALESICENTRY t2 on t1.FID=t2.FID
join t_AR_receivable t3 on t2.FSRCBILLNO=t3.FBILLNO
group by t3.FBILLNO,t2.FENTRYID,t1.FCANCELSTATUS,t1.FDOCUMENTSTATUS", materid);
                var objs = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                if (objs != null && objs.Count > 0)
                {
                    Entity entitys = this.View.BusinessInfo.GetEntity("FEntity");
                    var FEntityDatas = this.View.Model.GetEntityDataObject(entitys);
                    FEntityDatas[index]["FTaxPrice"] = objs[0]["FTAXPRICE"];
                    FEntityDatas[index]["EvaluatePrice"] = objs[0]["FPRICE"];
                    this.View.UpdateView("FEntity");
                }
            }
        }
    }
}
