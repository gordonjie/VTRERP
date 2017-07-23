using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.K3.FIN.IV.Business.PlugIn;
using Kingdee.BOS.Core.Metadata.EntityElement;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    public class VTRIVedit : IVEdit
    {
        public override void EntityRowClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.EntityRowClickEventArgs e)
        {
            base.EntityRowClick(e);
            if (e.Row >= 0)
            {
                if ((this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "Z" || this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "A" || this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "B") && this.FormID.Contains("IV_S") && base.View.Model.GetValue("FSRCBILLTYPEID").ToString().Contains("IV_S"))
                {
                    foreach (string str in new List<string> { "tbDeleteLine" })
                    {
                        this.View.GetBarItem(this.EntityKey, str).Enabled = true;
                    }
                }
                if ((this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "Z" || this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "A" || this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "B") && this.FormID.Contains("IV_P") && base.View.Model.GetValue("FSOURCETYPE").ToString().Contains("IV_P"))
                {
                    foreach (string str in new List<string> { "tbDeleteLine" })
                    {
                        this.View.GetBarItem(this.EntityKey, str).Enabled = true;
                    }
                }
            }

        }
        
        /*
        /// <summary>
        /// 保存前
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            string operation = e.Operation.ToString();
            if(operation== "Kingdee.BOS.Business.Bill.Operation.Save")
            {
             Entity salesicentity = this.View.BusinessInfo.GetEntity("FSALESICENTRY");
             var entitydata = this.View.Model.GetEntityDataObject(salesicentity);
             int row = entitydata.Count;
             for(int i=0;i<row;i++)
                {
                    this.View.Model.SetValue("FNOTAXAMOUNTVIEW", this.View.Model.GetValue("FNOTAXAMOUNT",i), i);
                    this.View.Model.SetValue("FDISCOUNTAMOUNTVIEW", this.View.Model.GetValue("FDISCOUNTAMOUNT", i), i);
                    this.View.Model.SetValue("FDETAILTAXAMOUNTVIEW", this.View.Model.GetValue("FDETAILTAXAMOUNT", i), i);
                    this.View.Model.SetValue("FALLAMOUNTVIEW", this.View.Model.GetValue("FALLAMOUNT", i), i);
                }
                this.View.Model.Save();
            }
        }*/

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
