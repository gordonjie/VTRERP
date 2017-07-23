using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VTR.K3.Bill.PlugIn
{
    public class GetVOUCHER :  AbstractListPlugIn

    {
        [System.ComponentModel.Description("获取凭证号")]
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            if (!(e.Operation.Operation != "GetvoucherNo"))
            {
                string tableA = this.View.Model.BillBusinessInfo.Entrys[0].TableName.ToString();
                string tableB = tableA + "_VH";
                int num = 0;
                num=YDLCommServiceHelper.updatevoucherNo(this.Context,tableA,tableB);
                this.View.ShowErrMessage("共更新" + num.ToString() + "条数据!", "", MessageBoxType.Notice);
                e.OperationResult.IsShowMessage = false;
            }
        }



    }
}
