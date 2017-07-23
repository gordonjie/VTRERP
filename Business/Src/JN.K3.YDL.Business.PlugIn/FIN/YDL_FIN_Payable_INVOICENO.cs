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

namespace JN.K3.YDL.Business.PlugIn.FIN.PAY
{
    [Description("应付单-表单发票号码插件")]
    public class YDL_FIN_Payable_INVOICENO : AbstractBillPlugIn
    {
        private DynamicObject currRow = null;
        private int rowindex = 0;
        public override void OnLoad(EventArgs e)
        {
 	             base.OnLoad(e);
                this.Model.TryGetEntryCurrentRow("FEntityDetail", out currRow, out rowindex);
                int index = Convert.ToInt32(currRow["SEQ"]) - 1;
                DynamicObject FMATERIALID = this.View.Model.GetValue("FEntityDetail", index) as DynamicObject;
                if (FMATERIALID == null) return;
                var materid = FMATERIALID["id"];
                var sql = string.Format("{0}{1}{2}",@"select top 1 t1.FENTRYID,INVOICENO =STUFF((select ','+FINVOICENO  from T_IV_PURCHASEIC  tb2
join T_IV_PURCHASEICENTRY tb3 on tb2.FID=tb3.FID
join T_AP_PAYABLE tb4 on tb3.FSRCBILLNO=tb4.FBILLNO
join T_AP_PAYABLEENTRY tb5 on tb5.FID=tb4.FID
where tb5.FENTRYID=t1.FENTRYID and tb3.FENTRYID=t3.FENTRYID 
and t2.FCANCELSTATUS='A' and t2.FDOCUMENTSTATUS='C'
FOR xml path('')), 1, 1, '')
from T_AP_PAYABLEENTRY t1
join T_AP_PAYABLE t2 on t1.FID=t2.FID
join T_IV_PURCHASEICENTRY t3 on  t2.FBILLNO=t3.FSRCBILLNO  
join T_IV_PURCHASEIC t4 on t4.FID=t3.FID
where t1.FENTRYID=",materid,@"
group by t1.FENTRYID,t3.FENTRYID,t2.FCANCELSTATUS,t2.FDOCUMENTSTATUS");
            
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

