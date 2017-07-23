using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM
{
    [Description("退货通知表单插件")]
    public class JN_SAL_RETURNNOTICE : AbstractBillPlugIn
    {
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

        /// <summary>
        /// 保存前，取审批流属性
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            int rows = this.View.Model.GetEntryRowCount("FEntity");
            bool Isolnytax = false;
            for (int i = 0; i < rows; i++)
            {
                DynamicObject RMTYPEObj = base.View.Model.GetValue("FRMTYPE", i) as DynamicObject;
                string Number = RMTYPEObj["FNumber"].ToString();
                if (Number == "THLX03_SYS")
                {
                    Isolnytax = true;
                 }
            }
            if (Isolnytax == true)
            {
                this.View.Model.SetValue("F_JNApprovalflow", "退票");
            }
            else
            {
                this.View.Model.SetValue("F_JNApprovalflow", "退货");
            }
         }
    }
}
