using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM
{
    [Description("销售订单-审批流插件")]
    public class JN_YDL_SCM_SaleOrderApproval : AbstractBillPlugIn
    {
        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            //判断首行发货仓库
            DynamicObject STOCKObj = base.View.Model.GetValue("FSTOCKID_MX", 0) as DynamicObject;
            if (STOCKObj == null) return;
           
            string STOCKname = STOCKObj["name"].ToString();
            this.View.Model.SetValue("Fstorehouse", STOCKname);

        }
    }
}
