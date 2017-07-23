using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.BOS.Orm.DataEntity;
 


namespace VTR.K3.Bill.PlugIn
{
    public class SOApplyEditPlugIn : AbstractBillPlugIn
    {
        [System.ComponentModel.Description("采购申请单客户带联系人")]
        public override void DataChanged(DataChangedEventArgs e)
        {

            if (!e.Field.Key.EqualsIgnoreCase("FCUSTID"))
            {
                return;
            }
            DynamicObject custDynamicObject = View.Model.DataObject["FCUSTID"] as DynamicObject;
            if (custDynamicObject == null)
            {
                return;
            }
            DynamicObjectCollection address = custDynamicObject["BD_CUSTLOCATION"] as DynamicObjectCollection;
            if (address == null || address.Count == 0)
            {
                return;
            }

            var add = address.FirstOrDefault(f => f["ContactId"] != null && (bool)f["ISDEFAULT"] == true);
            if (add != null)
            {
                this.View.Model.SetValue("FReceiveContact", add["ContactId"]);
            }
            else
            {
                this.View.Model.SetValue("FReceiveContact", address[0]["ContactId"]);
            }
        }

        
    }




     
}
