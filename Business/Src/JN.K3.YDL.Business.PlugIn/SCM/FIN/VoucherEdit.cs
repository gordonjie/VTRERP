using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    /// <summary>
    /// 凭证维护插件
    /// </summary>
    [System.ComponentModel.Description("凭证审核更新审核人")]
   public  class VoucherEdit: AbstractBillPlugIn
    {
        /// <summary>
        /// 菜单按钮点击后事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            //设置审核标签值
            string strCheckUser = "";
            base.AfterBarItemClick(e);
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "TBAPPROVE": //审核
                case "TBSPLITAPPROVE":
                    DynamicObject fChecker = this.View.Model.GetValue("FCHECKERID") as DynamicObject;
                    strCheckUser = fChecker["Name"].ToString();
                    break;
                default:
                    break;
            }
            this.View.GetControl("FOrderCheckerId").Text = strCheckUser;
        }
    }
}
