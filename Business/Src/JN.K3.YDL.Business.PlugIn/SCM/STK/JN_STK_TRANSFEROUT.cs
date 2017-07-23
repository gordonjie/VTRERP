using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM.STK
{
    [Description("分步式调出单-表单插件")]
    public class JN_STK_TRANSFEROUT : AbstractBillPlugIn
    {
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.Equals("F_JN_Customer"))
            {
                DynamicObject Customerid = this.View.Model.GetValue("F_JN_Customer") as DynamicObject;
                if (Customerid == null) return;
                DynamicObjectCollection Object = Customerid["BD_CUSTLOCATION"] as DynamicObjectCollection;
                DynamicObjectCollection obj = Customerid["BD_CUSTCONTACT"] as DynamicObjectCollection;
                if (Object.Count == 0) return;
                if (obj.Count == 0) return;
                var ContactId = Object[0]["ContactId"];
                var Adder = obj[0]["ADDRESS"];
                this.View.Model.SetValue("F_JN_CustContact", ContactId);
                this.View.Model.SetValue("FReceiveAddress", Adder);
                this.View.UpdateView("F_JN_CustContact");
                this.View.UpdateView("FReceiveAddress");
            }
        }
    }
}
