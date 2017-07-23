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
  [System.ComponentModel.Description("获取物料分组")]
    public class GetMaterialsClass : AbstractBillPlugIn
    {
      
        public override void DataChanged(DataChangedEventArgs e)
        {
            bool Doget1 = e.Field.Key.EqualsIgnoreCase("FMaterialId");
            bool Doget2 = e.Field.Key.EqualsIgnoreCase("Fstorehouse");

            if (Doget1)
       
             {
                var xxx = this.View.Model.GetValue("FMaterialId", 0) as DynamicObject;
                //var xxx = this.View.Model.DataObject["FMaterialId"] as DynamicObject;
                // var yyy = this.View.Model.GetValue("MaterialGroup_Id", 0);

                //DynamicObject Mclass = xxx["StoreUser"] as DynamicObject;
                if (xxx["StoreUser"] == null)
                {
                    return;
                }
                this.Model.SetValue("FMClass", xxx["StoreUser"].ToString());
               
            }
            else if (Doget2)
            {
                var yyy = this.View.Model.GetValue("FStockId", 0) as DynamicObject;
                var zzz = this.View.Model.GetValue("FSTOCKID_MX", 0) as DynamicObject;

                if (yyy != null)
                {
                    this.Model.SetValue("Fstorehouse", yyy["Name"].ToString());
                }
                if (zzz != null)
                {
                    this.Model.SetValue("Fstorehouse", zzz["Name"].ToString());
                }
                
            }
            }
        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            var yyy = this.View.Model.GetValue("FStockId", 0) as DynamicObject;
            var zzz = this.View.Model.GetValue("FSTOCKID_MX", 0) as DynamicObject;

            if (yyy != null)
            {
                this.Model.SetValue("Fstorehouse", yyy["Name"].ToString());
            }
            if (zzz != null)
            {
                this.Model.SetValue("Fstorehouse", zzz["Name"].ToString());
            }
        }
/*        public override void DataUpdateEnd()
        {
            base.DataUpdateEnd();

            var yyy = this.View.Model.GetValue("FStockId", 0) as DynamicObject;
            var zzz = this.View.Model.GetValue("FSTOCKID_MX", 0) as DynamicObject;

            if (yyy != null)
            {
                this.Model.SetValue("Fstorehouse", yyy["Name"].ToString());
            }
            if (zzz != null)
            {
                this.Model.SetValue("Fstorehouse", zzz["Name"].ToString());
            }

        }
       
*/
    }
}
