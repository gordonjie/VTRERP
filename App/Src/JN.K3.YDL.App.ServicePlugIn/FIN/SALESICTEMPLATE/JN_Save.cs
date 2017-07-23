using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

namespace JN.K3.YDL.App.ServicePlugIn.FIN.SALESICTEMPLATE
{

    [Description("销售发票保存服务插件")]
    public class JNSave : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBillNo");

        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (e.DataEntitys == null) return;
            var billGroups = e.DataEntitys;
            //List<string> sql = new List<string>();
            foreach (var billGroup in billGroups)
            {
                string billno = Convert.ToString(billGroup["Billno"]);
                string sql = string.Format(@"/*dialect*/update T_IV_SALESICENTRY set FALLAMOUNTVIEW=t2.FALLAMOUNT,FDISCOUNTAMOUNTVIEW=t2.FDISCOUNTAMOUNT,FDETAILTAXAMOUNTVIEW=t2.FDETAILTAXAMOUNT,FNOTAXAMOUNTVIEW=t2.FNOTAXAMOUNT from T_IV_SALESICENTRY t1 join T_IV_SALESICENTRY_o t2 on t2.FENTRYID=t1.FENTRYID join T_IV_SALESIC t3 on t3.fid =t1.fid where t3.Fbillno='{0}'", billno);
                DBUtils.Execute(this.Context, sql);
            }
        }
        /*
        public override void BeginOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeginOperationTransactionArgs e)
        {
            if (e.DataEntitys == null) return;
            var billGroups = e.DataEntitys;
            //List<string> sql = new List<string>();
            foreach (var billGroup in billGroups)
            {
                string billno = Convert.ToString(billGroup["Billno"]);
                string sql = string.Format(@"update T_IV_SALESICENTRY set FALLAMOUNTVIEW=t2.FALLAMOUNT,FDISCOUNTAMOUNTVIEW=t2.FDISCOUNTAMOUNT,FDETAILTAXAMOUNTVIEW=t2.FDETAILTAXAMOUNT,FNOTAXAMOUNTVIEW=t2.FNOTAXAMOUNT
from T_IV_SALESICENTRY t1
join T_IV_SALESICENTRY_o t2 on t2.FENTRYID=t1.FENTRYID
join T_IV_SALESIC t3 on t3.fid =t1.fid where t3.Fbillno={0}", billno);
                DBUtils.Execute(this.Context, sql);
            }
        }*/
    }
}
