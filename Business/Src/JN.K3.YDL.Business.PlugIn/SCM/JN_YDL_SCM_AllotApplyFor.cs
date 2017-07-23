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
    [Description("调拨申请单-表单插件")]
    public class JN_YDL_SCM_AllotApplyFor : CommonBillEdit
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

        /// <summary>
        /// 保存前，取仓库审批流用
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            var store=this.View.Model.GetValue("FDESTSTOCKID",0) ;
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
            this.View.Model.SetValue("FSaleOrgId", Convert.ToInt32(this.Context.CurrentOrganizationInfo.ID));
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
