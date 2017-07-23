using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Metadata;

namespace JN.K3.YDL.Business.PlugIn.SCM
{
    [Description("销售退货单-表单插件")]
    public class JN_SAL_RETURNSTOCK_Extend : CommonBillEdit
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

        /// <summary>
        /// 新增后
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateModelData(EventArgs e)
        {
            base.AfterCreateModelData(e);
            //通过当前用户对应的联系对象找到员工
            QueryBuilderParemeter para = new QueryBuilderParemeter();
            para.FormId = "BD_NEWSTAFF";
            para.FilterClauseWihtKey = string.Format(" exists (select 1 from t_sec_User where FLinkObject=FPERSONID and FUSERID={0} )", this.Context.UserId);
            para.SelectItems = SelectorItemInfo.CreateItems(" FSTAFFID ");
            var employeeDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
            if (employeeDatas != null && employeeDatas.Count > 0)
            {
                this.View.Model.SetValue("FApplicantId", Convert.ToInt64(employeeDatas[0]["FSTAFFID"]));
            }
        }
    }
}
