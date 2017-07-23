using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    [Description("费用申请单表单插件")]
    public class JN_ER_ExpenseRequest : AbstractBillPlugIn
    {
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.ToUpper() == "FSTAFFID")
            {
                getApplicantId();
            }

        }

        public override void AfterDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string operation = e.Operation.Operation.ToString();
            if (operation.ToUpper() == "ADDYW")
            {
                if (Convert.ToBoolean(this.View.Model.GetValue("F_JNISZX")) == true)
                {
                    int jiarow = this.View.Model.GetEntryRowCount("FYWEntity");
                    int jianrow = this.View.Model.GetEntryRowCount("FYWJIANEntity");
                    for (int i = 0; i < 6 - jiarow; i++)
                    {
                        this.View.Model.CreateNewEntryRow("FYWEntity");
                    }
                    for (int i = 0; i < 4 - jiarow; i++)
                    {
                        this.View.Model.CreateNewEntryRow("FYWJIANEntity");
                    }
                    this.View.Model.SetValue("F_JNprojectText", "直销业务提成", 0);
                    this.View.Model.SetValue("F_JNprojectText", "电话费补贴", 1);
                    this.View.Model.SetValue("F_JNprojectText", "办公费补贴", 2);
                    this.View.Model.SetValue("F_JNprojectText", "办事处及仓库租金", 3);
                    this.View.Model.SetValue("F_JNprojectText", "运费补贴", 4);
                    this.View.Model.SetValue("F_JNprojectText", "全年合同任务奖励", 5);

                    this.View.Model.SetValue("F_JNprojectText1", "绩效考核罚款", 0);
                    this.View.Model.SetValue("F_JNprojectText1", "利息罚款", 1);
                    this.View.Model.SetValue("F_JNprojectText1", "退货费用", 2);
                    this.View.Model.SetValue("F_JNprojectText1", "换领包装费用", 3);


                }

                if (Convert.ToBoolean(this.View.Model.GetValue("F_JNISZX")) == false)
                {
                    int jiarow = this.View.Model.GetEntryRowCount("FYWEntity");
                    int jianrow = this.View.Model.GetEntryRowCount("FYWJIANEntity");
                    for (int i = jiarow-1; i >=0; i--)
                    {
                        this.View.Model.DeleteEntryRow("FYWEntity",i);
                    }
                    for (int i = jianrow - 1; i >= 0; i--)
                    {
                        this.View.Model.DeleteEntryRow("FYWJIANEntity", i);
                    }
                }

                
            }

            if (operation.ToUpper() == "COUNTYW")
            {
                int jiarow = this.View.Model.GetEntryRowCount("FYWEntity");
                int jianrow = this.View.Model.GetEntryRowCount("FYWJIANEntity");
                if (jiarow<1||jianrow<1)return;
                string FprojectText =Convert.ToString(this.View.Model.GetValue("F_JNprojectText", 0));
                for (int i = 0; i < jiarow; i++)
                {
                    this.View.Model.SetValue("F_JNXWTPAMOUNT", Convert.ToInt32(this.View.Model.GetValue("F_JNYWAMOUNT",i)), i);
                }


                if (FprojectText == "直销业务提成")
                {
                    this.View.Model.SetValue("F_JNXWTPAMOUNT", Convert.ToInt32(Convert.ToDouble(this.View.Model.GetValue("F_JNYWAMOUNT",0)) - Convert.ToDouble(this.View.Model.GetValue("F_JNYWJIANSUM"))), 0);
                }
                this.View.InvokeFieldUpdateService("F_JNYWJIASUM", 0);
                this.View.InvokeFieldUpdateService("F_JNYWJIANSUM", 0);
                this.View.UpdateView("F_JNYWTDJIANSUM");
                this.View.InvokeFieldUpdateService("F_JNYWTDJIANSUM", 0);

            }
        }

        public override void AfterCreateModelData(EventArgs e)
        {
            base.AfterCreateModelData(e);
            getApplicantId();
            //动态创建单据类型
             string username = this.Context.UserName.ToString();
             string strsql = string.Format(@"/*dialect*/ select 1 from T_HR_EMPINFO t1 join T_HR_EMPINFO_L t2 on t1.FID=t2.FID join T_BD_STAFFTEMP t3 on t1.FID=t3.FID where Fname='{0}' and FDEPTID='100276'", username);
             var count=DBUtils.ExecuteDynamicObject(this.Context, strsql);
             if (count.Count == 0)
             {
                 EnumItem ei;
                 var lItem = new List<EnumItem>();
                 ei = new EnumItem();
                 ei.Value = " ";
                 ei.Caption = new Kingdee.BOS.LocaleValue("");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "个人电话费、油费补贴";
                 ei.Caption = new Kingdee.BOS.LocaleValue("个人电话费、油费补贴");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "出租的士费";
                 ei.Caption = new Kingdee.BOS.LocaleValue("出租的士费");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "招待费";
                 ei.Caption = new Kingdee.BOS.LocaleValue("招待费");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "综合费用";
                 ei.Caption = new Kingdee.BOS.LocaleValue("综合费用");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "工程付款";
                 ei.Caption = new Kingdee.BOS.LocaleValue("工程付款");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "个人借支";
                 ei.Caption = new Kingdee.BOS.LocaleValue("备用金借支");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "其他付款";
                 ei.Caption = new Kingdee.BOS.LocaleValue("其他付款");
                 lItem.Add(ei);


                 this.View.GetFieldEditor<ComboFieldEditor>("FBILLTYPE", 0).SetComboItems(lItem);
             }
             else
             {
                 EnumItem ei;
                 var lItem = new List<EnumItem>();
                 ei = new EnumItem();
                 ei.Value = " ";
                 ei.Caption = new Kingdee.BOS.LocaleValue("");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "个人电话费、油费补贴";
                 ei.Caption = new Kingdee.BOS.LocaleValue("个人电话费、油费补贴");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "出租的士费";
                 ei.Caption = new Kingdee.BOS.LocaleValue("出租的士费");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "招待费";
                 ei.Caption = new Kingdee.BOS.LocaleValue("招待费");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "综合费用";
                 ei.Caption = new Kingdee.BOS.LocaleValue("综合费用");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "业务费用";
                 ei.Caption = new Kingdee.BOS.LocaleValue("业务费用");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "工程付款";
                 ei.Caption = new Kingdee.BOS.LocaleValue("工程付款");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "个人借支";//个人借支
                ei.Caption = new Kingdee.BOS.LocaleValue("备用金借支");
                 lItem.Add(ei);

                 ei = new EnumItem();
                 ei.Value = "其他付款";
                 ei.Caption = new Kingdee.BOS.LocaleValue("其他付款");
                 lItem.Add(ei);


                 this.View.GetFieldEditor<ComboFieldEditor>("FBILLTYPE", 0).SetComboItems(lItem);
             }
        }



        private void getApplicantId()
        {
            //初始化申请人
            var PROPOSER = this.View.Model.GetValue("FStaffID") as DynamicObject;
            var Org = this.View.Model.GetValue("FOrgID") as DynamicObject;
            if (PROPOSER == null || Org == null) return;
            string PROPOSERID = PROPOSER["ID"].ToString();

            string OrgID = Org["ID"].ToString();
            QueryBuilderParemeter para = new QueryBuilderParemeter();
            para.FormId = "BD_NEWSTAFF";
            para.FilterClauseWihtKey = string.Format(" exists (select top 1 FSTAFFID from T_BD_STAFF where FEmpInfoId= {0} and FUseOrgId={1} )", PROPOSERID, OrgID);
            para.SelectItems = SelectorItemInfo.CreateItems(" FSTAFFID ");
            var employeeDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
            if (employeeDatas != null && employeeDatas.Count > 0)
            {
                this.View.Model.SetValue("FApplicantId", Convert.ToInt64(employeeDatas[0]["FSTAFFID"]));
            }

            //初始化往来单位
            var FTOCONTACTUNIT = this.View.Model.GetValue("FTOCONTACTUNIT") as DynamicObject;
            string FTOCONTACTUNITTYPE = Convert.ToString(this.View.Model.GetValue("FTOCONTACTUNITTYPE"));
            if (PROPOSER != null && FTOCONTACTUNITTYPE == "BD_Empinfo")
            {
                this.View.Model.SetValue("FTOCONTACTUNIT", PROPOSER["ID"]);
            }
        }


    }
}
