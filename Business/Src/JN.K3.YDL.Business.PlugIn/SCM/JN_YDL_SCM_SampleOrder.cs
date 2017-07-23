using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM
{
    [Description("样品申请单-表单插件")]
    public class JN_YDL_SCM_SampleOrder : CommonBillEdit
    {
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.Equals("FCUSTID"))
            {
                //DynamicObject Fcustid = this.View.Model.GetValue("FCUSTID") as DynamicObject;
                //System.Collections.Generic.List<Kingdee.BOS.Core.Metadata.EnumItem> items = new System.Collections.Generic.List<Kingdee.BOS.Core.Metadata.EnumItem>();
                //base.View.GetControl<ComboFieldEditor>("FCustLocIds").SetComboItems(items);
                //if (Fcustid != null)
                //{
                //    DynamicObjectCollection objects = Fcustid["BD_CUSTCONTACT"] as DynamicObjectCollection;
                //    if (objects != null)
                //    {
                //        if (objects.Count > 0)
                //        {
                //            int seq = 0;
                //            foreach (var item in objects)
                //            {
                //                 Kingdee.BOS.Core.Metadata.EnumItem enumItem = new Kingdee.BOS.Core.Metadata.EnumItem();
                //                 Kingdee.BOS.LocaleValue lvalue = new Kingdee.BOS.LocaleValue(item["NAME"].ToString());
                //                 enumItem.Value = item["Id"].ToString();
                //                 enumItem.Caption = lvalue;
                //                 enumItem.Seq = seq;
                //                 items.Add(enumItem); 
                //                 seq++;
                //            }
                //            base.View.GetControl<ComboFieldEditor>("FCustLocIds").SetComboItems(items);
                //        }
                //    }
                //}
                DynamicObject Fcustid = this.View.Model.GetValue("FCUSTID") as DynamicObject;
                if (Fcustid == null) return;
                DynamicObjectCollection Object = Fcustid["BD_CUSTCONTACT"] as DynamicObjectCollection;
                if (Object.Count == 0) return;
                var Entryid = Object[0]["id"];
                this.View.Model.SetValue("F_JN_Location", Entryid);
                this.View.UpdateView("F_JN_Location");
            }
        }
    }
}
