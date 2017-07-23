using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    [Description("付款单-表单插件")]
    public class JN_CN_PAYBILLEDIT : AbstractBillPlugIn
    {
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string option = Convert.ToString(e.Operation.Operation);
            if (option == "Audit" || option == "Synchronism" || option == "CancelWB")
            {
                this.View.UpdateView();
            }
        }
    }
}
