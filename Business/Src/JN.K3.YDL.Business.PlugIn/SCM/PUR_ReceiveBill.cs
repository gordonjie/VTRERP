using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;

namespace JN.K3.YDL.Business.PlugIn.SCM
{
    [Description("采购收料单表单插件判断是否检验")]
    public class PUR_ReceiveBill : AbstractBillPlugIn
    {
        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            int rows = this.View.Model.GetEntryRowCount("FDetailEntity");
            bool Ischeck = false;
            string DEPname = "";//首行需求部门
            string DEPname2 = "";//其他行需求部门
            int IsIT = 0;
            for (int i = 0; i < rows; i++)
            { 
                //string needcheck=this.View.Model.GetValue("F_kk_BaseProperty",i).ToString();
                DynamicObject materialObj = base.View.Model.GetValue("FMATERIALID", i) as DynamicObject;
                if (materialObj != null)
                {
                    DynamicObjectCollection materialQM = materialObj["materialQM"] as DynamicObjectCollection;
                    DynamicObjectCollection materialbase = materialObj["materialbase"] as DynamicObjectCollection;
                    string needcheck = materialQM[0]["CheckIncoming"].ToString();
                    string CategoryType = Convert.ToString(materialbase[0]["F_JNAssetCombo"]);
                    if (needcheck == "True")
                    { Ischeck = true; }
                    if (CategoryType == "A")//IT资产
                    { IsIT = 1; }
                }
                if (IsIT == 1)
                {
                    this.View.Model.SetValue("F_JNMaterialflow", "IT");
                }
                else
                {
                    this.View.Model.SetValue("F_JNMaterialflow", "other");
                }

                DynamicObject RequireDept = base.View.Model.GetValue("FDemandDeptId", i) as DynamicObject;
                if (RequireDept != null)
                {
                    if (i == 0)
                    {
                        DEPname = Convert.ToString(RequireDept["Name"]);
                    }
                    else
                    {
                        DEPname2 = Convert.ToString(RequireDept["Name"]);

                        if (DEPname != DEPname2)
                        {
                            this.View.ShowWarnningMessage("同一张采购申请单需求部门需保持一致!");
                            e.Cancel = true;
                            return;
                        }
                    }

                }
            }
            this.View.Model.SetValue("FIscheck", Ischeck);
            this.View.Model.SetValue("F_JNDEPflow", DEPname); 
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
