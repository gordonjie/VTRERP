using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.Metadata.FieldElement;
using System.ComponentModel;


namespace JN.K3.YDL.Business.Plugin.Fin.Payable.ServicePlugIn
{

    [Description("付款申请-付款单转换插件")]
    public class INVOICEverify : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FINVOICENO");
            e.FieldKeys.Add("FSRCROWID");
        }
        public override void EndOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            string nowinvoicno = "";
            string srcrowno = "";
            foreach (DynamicObject item in e.DataEntitys)
            {
                nowinvoicno= Convert.ToString(item["INVOICENO"]);
                var entryitem = item["PURCHASEICENTRY"] as DynamicObjectCollection;
              
                int rows = entryitem.Count;
                for (int rowindex = 0; rowindex < rows; rowindex++)
                {
                    DynamicObject objsRow = entryitem[rowindex];
                    srcrowno = objsRow["SRCROWID"].ToString();
                    if (srcrowno.Equals("0"))
                    {
                    var sql = string.Format("{0}{1}{2}{3}", @"/*dialect*/ update T_AP_PAYABLEENTRY set FINVOICENO=(select top 1
STUFF((select ','+tb2.FINVOICENO  from T_IV_PURCHASEIC  tb2
join T_IV_PURCHASEICENTRY tb3 on tb2.FID=tb3.FID
join T_AP_PAYABLE tb4 on tb3.FSRCBILLNO=tb4.FBILLNO
join T_AP_PAYABLEENTRY tb5 on tb5.FID=tb4.FID
where tb5.FENTRYID=t1.FENTRYID  and tb3.FSRCROWID=tb5.FENTRYID
and t2.FCANCELSTATUS='A' and t2.FDOCUMENTSTATUS='C'
FOR xml path('')), 1, 1, '') from T_AP_PAYABLEENTRY t1
join T_AP_PAYABLE t2 on t1.FID=t2.FID
join T_IV_PURCHASEICENTRY t3 on  t2.FBILLNO=t3.FSRCBILLNO  
join T_IV_PURCHASEIC t4 on t4.FID=t3.FID
where  t3.FSRCROWID=t1.FENTRYID and t1.FENTRYID=", srcrowno, @" group by t1.FENTRYID,t3.FENTRYID,t2.FCANCELSTATUS,t2.FDOCUMENTSTATUS)
where FENTRYID =", srcrowno);
                    DBServiceHelper.Execute(this.Context, sql);}
                }

               
            }
           
        }
    }
}