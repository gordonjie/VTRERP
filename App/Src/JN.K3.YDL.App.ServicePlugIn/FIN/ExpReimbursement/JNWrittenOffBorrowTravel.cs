using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;

namespace JN.K3.YDL.App.ServicePlugIn.FIN.ExpReimbursement
{
    /// <summary>
    /// 费用报销单保存反写检查插件
    /// </summary>
    [Description("差旅费报销单冲借款校验插件")]
    public class JNWrittenOffBorrowTravel : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FRequestType");
            e.FieldKeys.Add("FSourceBillType");
            e.FieldKeys.Add("FSourceBillNo");
            e.FieldKeys.Add("FSRCBORROWAMOUNT");
            e.FieldKeys.Add("FExpSubmitAmount");
            e.FieldKeys.Add("FReqSubmitAmount");
            e.FieldKeys.Add("FIsFromBorrow");
            e.FieldKeys.Add("FSrcOffSetAmount");
            e.FieldKeys.Add("FSosurceRowID");
            e.FieldKeys.Add("FSeq");
            e.FieldKeys.Add("FRequestAmount");
            e.FieldKeys.Add("FExpenseAmount");
            base.OnPreparePropertys(e);
        }
        
    }
}
