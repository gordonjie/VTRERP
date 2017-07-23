using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill.PlugIn;

namespace JN.K3.YDL.Business.PlugIn.SCM.STK
{
     [Description("采购入库表单插件--保存表头仓库")]
    public class JN_SKT_InStockEdit : AbstractBillPlugIn
    {
        /// <summary>
        /// 保存前，取仓库审批流用
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            var store = this.View.Model.GetValue("FStockId", 0);
            this.View.Model.SetValue("F_JNBaseStock", store);
            this.View.UpdateView("F_JNBaseStock");

        }

    }
}
