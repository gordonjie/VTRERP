using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    [Description("收款单表单插件")]
    public class JN_AR_RECEIVEBILLEdit : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Key.ToUpper() == "FCONTACTUNIT")
            {
                var CONTACTUNIT = this.View.Model.GetValue("FCONTACTUNIT");
            }
        }
    }
}
