using Kingdee.BOS.Core.Base.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.BD
{
    [Description("CRM客户保存校验插件")]
    public class CRMCustContactEdit : AbstractBasePlugIn
    {
        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            DynamicObjectCollection dynamicEntrty = this.View.Model.DataObject["JN_BD_CUSTLOCATIONEntity"] as DynamicObjectCollection;
            decimal sum = 0;
            foreach (DynamicObject dynamicO in dynamicEntrty)
            {
                decimal percent = Convert.ToDecimal(dynamicO["FJNPercent"]);
                sum = sum + percent;
            }
            if (sum != 1)
            {
                this.View.ShowErrMessage("业绩分摊（百分比比例合计必须等于1）");
                e.Cancel = true;
            }
        }
    }
}
