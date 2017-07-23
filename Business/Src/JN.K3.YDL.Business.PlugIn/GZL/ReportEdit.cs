using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.GZL
{
    [Description("报告单据插件")]
      
    public class ReportEdit : AbstractBillPlugIn
    {
        private Boolean _Isupateactor = false;
        private DynamicObjectCollection oldactors;
        
        public override void AfterCreateNewEntryRow(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.CreateNewEntryEventArgs e)
        {
            base.AfterCreateNewEntryRow(e);
            //Entity reportEntity =this.View.BusinessInfo.GetEntity("FEntity");
            DynamicObjectCollection reportRows = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            foreach (var reportRow in reportRows)
            {
                if (reportRows.Count == 1)
                {
                    this.View.Model.SetValue("F_VTR_ReportNumber", "方案", 0);
                    this.View.Model.SetValue("F_VTR_Choice",1, 0);//一个方案时默认选择
                }
                if (reportRows.Count > 1)
                {
                    int row = Convert.ToInt16(reportRow["Seq"]);
                    string zhongwenrow = this.NumberToChinese(row);
                    string fangan = "方案" + zhongwenrow;
                    reportRow["F_VTR_ReportNumber"] = fangan;
                    this.View.Model.SetValue("F_VTR_Choice", 0, 0);//一个方案时默认选择
                }

            }
            this.View.UpdateView("F_VTR_ReportNumber");
        }

        public override void AfterDeleteRow(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterDeleteRowEventArgs e)
        {
            base.AfterDeleteRow(e);
            DynamicObjectCollection reportRows = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            foreach (var reportRow in reportRows)
            {
                if (reportRows.Count == 1)
                {
                    this.View.Model.SetValue("F_VTR_ReportNumber", "方案", 0);
                    this.View.Model.SetValue("F_VTR_Choice", 1, 0);//一个方案时默认选择
                }
                if (reportRows.Count > 1)
                {
                    int row = Convert.ToInt16(reportRow["Seq"]);
                    string zhongwenrow = this.NumberToChinese(row);
                    string fangan = "方案" + zhongwenrow;
                    this.View.Model.SetValue("F_VTR_ReportNumber", fangan, row - 1);
                    this.View.Model.SetValue("F_VTR_Choice", 0, 0);//一个方案时默认选择
                }

            }
            
        }
        public override void BeforeDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            string option=e.Operation.FormOperation.Operation.ToString();
            if (option == "Save")
            {
                this.updateF_VTR_AmountF_JNAssetCombo();
            }
      
        }

        //更新表头审批金额和产品分类，审批流用
        private void updateF_VTR_AmountF_JNAssetCombo()
        {
            double Amount = 0;
            string AssetComboset = "B";
            DynamicObjectCollection reportRows = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            foreach (var reportRow in reportRows)
            {
                Amount = Math.Max(Amount, Convert.ToDouble(reportRow["F_VTR_ApplyAmount"]));

                /* 不更新物料类别哦--2018年1月16日
                DynamicObject MaterialId = reportRow["F_VTR_MaterialId"] as DynamicObject;
                if (MaterialId != null )
                {
                    DynamicObject BillType = this.View.Model.GetValue("FBillTypeID") as DynamicObject;
                    string BillName = Convert.ToString(BillType["Name"]);
                    if (BillName == "资产采购报告单")
                    {
                        DynamicObjectCollection MaterialBase = MaterialId["MaterialBase"] as DynamicObjectCollection;
                        if (MaterialBase != null || MaterialBase.Count > 0)
                        {
                            string AssetCombo = Convert.ToString(MaterialBase[0]["F_JNAssetCombo"]);
                            if (AssetCombo.Equals("A"))
                            {
                                AssetComboset = "A";                               
                            }
                        }
                    }
                }
            }
            this.View.Model.SetValue("F_JNAssetCombo", AssetComboset);*/
            }
            this.View.Model.SetValue("F_VTR_Amount", Amount);
        }



        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            
            base.DataChanged(e);
            /*
            if (e.Key.ToUpper() == "F_VTR_CHOICE")
            {
                int row=e.Row;
                DynamicObjectCollection reportRows = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
                for (int i = 0; i < reportRows.Count; i++)
                { 
                    if (i!=row)
                    {
                        reportRows[i]["F_VTR_Choice"] = false;
                        reportRows[i]["F_VTR_ApprovalAmount"] = 0;
                        this.View.UpdateView("F_VTR_Choice", i);
                        this.View.UpdateView("F_VTR_ApprovalAmount", i);
                    }
                }
            }*/
            if (e.Key.ToUpper() == "F_VTR_APPLYAMOUNT")
            {
                this.updateF_VTR_AmountF_JNAssetCombo();
            }

            if (e.Key.ToUpper() == "F_VTR_ACTOR" )//选用参与者设置用户权限
            {
                DynamicObjectCollection newactors = this.View.Model.GetValue("F_VTR_ACTOR") as DynamicObjectCollection;

                    //多选基础资料主键集合

                    MulBaseDataField mulField = this.View.BusinessInfo.GetField("F_VTR_ACTOR") as MulBaseDataField;

                    string[] oldValues = oldactors.Select(p => p[mulField.RefIDDynamicProperty.Name].ToString()).Distinct().ToArray();
                    string[] newValues = newactors.Select(p => p[mulField.RefIDDynamicProperty.Name].ToString()).Distinct().ToArray();

                    //合并数值
                    string[] result = oldValues.Union(newValues).ToArray();
                    //给单据上面的多选基础资料字段B赋值
                   _Isupateactor = true;
                    this.View.Model.SetValue("F_VTR_ACTOR", result);
                    this.View.UpdateView("F_VTR_ACTOR");
                    _Isupateactor = false;
                



                DynamicObjectCollection actors = this.View.Model.GetValue("F_VTR_actor") as DynamicObjectCollection;
                if (actors.Count<1) return;

                string str = string.Empty;
                foreach (var actor in actors)
                {
                    DynamicObject F_VTR_actor = actor["F_VTR_actor"] as DynamicObject;
                    string actorid = Convert.ToString(F_VTR_actor["Id"]);
                    //通过参与者对应的联系对象找到用户
                    QueryBuilderParemeter para = new QueryBuilderParemeter();
                    para.FormId = "SEC_User";
                    para.FilterClauseWihtKey = string.Format(" exists (select 1 from T_BD_STAFF where FPERSONID=FLinkObject and FSTAFFID={0} )", actorid, this.Context.CurrentOrganizationInfo.ID);
                    string[] userparams = new string[] { "FUSERID" };
                    para.SelectItems = SelectorItemInfo.CreateItems(userparams);

                    var userDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
                    if (userDatas.Count > 0)
                    {
                        str = string.Format("{0},{1}", str, Convert.ToString(userDatas[0]["FUSERID"]));
                    }

                  }
                this.Model.SetValue("F_VTR_ActorText", str);
            }


            if (e.Key.ToUpper() == "F_VTR_ISBUDGET" || e.Key.ToUpper() == "F_VTR_ISSCHEME")
            { 

                Panel P1 = this.View.GetControl<Panel>("F_VTR_Panel1");
                Panel P2 = this.View.GetControl<Panel>("F_VTR_Panel2");
                P1.SetHeight(190);
                P2.SetTop(190);
                
                bool ISBUDGET = Convert.ToBoolean(this.View.Model.GetValue("F_VTR_ISBUDGET"));
                bool ISSCHEME = Convert.ToBoolean(this.View.Model.GetValue("F_VTR_ISSCHEME"));
                if (ISBUDGET == true && ISSCHEME == false)//有预算只有一个方案
                {
                    DynamicObjectCollection reportRows = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
                    int rows=reportRows.Count;
                    this.View.Model.SetValue("F_VTR_ReportNumber", "预算", 0);
                    this.View.Model.SetValue("F_VTR_Choice", 1, 0);//一个方案时默认选择
                    for(int i=1;i<rows;i++)
                    {
                        this.Model.DeleteEntryRow("FEntity", 1);
                    }
                    if (rows == 0)
                    {
                        this.Model.CreateNewEntryRow("FEntity");
                    }
                }
                if (ISBUDGET == false && ISSCHEME == true)//无预算方案不止一个
                {
                    DynamicObjectCollection reportRows = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
                    int rows = reportRows.Count;
                    this.View.Model.SetValue("F_VTR_ReportNumber", "方案", 0);
                    this.View.Model.SetValue("F_VTR_Choice", 0, 0);//一个方案时默认选择
                    for (int i = 0; i < rows; i++)
                    {
                        this.View.Model.SetValue("F_VTR_ApplyAmount", 0, i);
                        this.View.Model.SetValue("F_VTR_ApprovalAmount", 0, i);
                    }
                }
                if (ISBUDGET == false && ISSCHEME == false)//无预算无方案
                {
                    DynamicObjectCollection reportRows = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
                    int rows = reportRows.Count;
                    for (int i = 0; i < rows; i++)
                    {
                        this.Model.DeleteEntryRow("FEntity", 0);
                    }
                    P1.SetHeight(0);
                    P2.SetTop(0);
                   
                    
                }

            }
        }

        public override void BeforeUpdateValue(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeUpdateValueEventArgs e)
        {
            base.BeforeUpdateValue(e);
            if (_Isupateactor == false)
            {
                if (e.Key.ToUpper() == "F_VTR_ACTOR")//选用参与者设置用户权限
                {
                    oldactors = this.View.Model.GetValue("F_VTR_ACTOR") as DynamicObjectCollection;

                }
            }
        }


        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            bool ISBUDGET = Convert.ToBoolean(this.View.Model.GetValue("F_VTR_ISBUDGET"));
            bool ISSCHEME = Convert.ToBoolean(this.View.Model.GetValue("F_VTR_ISSCHEME"));
            if (ISBUDGET == false && ISSCHEME == false)//无预算无方案
            {
                Panel P1 = this.View.GetControl<Panel>("F_VTR_Panel1");
                Panel P2 = this.View.GetControl<Panel>("F_VTR_Panel2");
                P1.SetHeight(0);
                P2.SetTop(0);
            }
        }
        /// <summary>
        /// 新增后带出申请人
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateModelData(EventArgs e)
        {
            base.AfterCreateModelData(e);
            //通过当前用户对应的联系对象找到员工
            QueryBuilderParemeter para = new QueryBuilderParemeter();
            para.FormId = "BD_NEWSTAFF";
            para.FilterClauseWihtKey = string.Format(" exists (select 1 from t_sec_User where FLinkObject=FPERSONID and FUSERID={0} ) and FUSEORGID={1}", this.Context.UserId, this.Context.CurrentOrganizationInfo.ID);
            string[] cusparams=new string[]{"FSTAFFID","FEmpInfoId","FDept"};
            para.SelectItems = SelectorItemInfo.CreateItems(cusparams);
            
            var employeeDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
            if (employeeDatas != null && employeeDatas.Count > 0)
            {
                this.View.Model.SetValue("FApplicantId", Convert.ToInt64(employeeDatas[0]["FSTAFFID"]));
                this.View.Model.SetValue("FStaffID", Convert.ToInt64(employeeDatas[0]["FEmpInfoId"]));
                this.View.Model.SetValue("FDeptID", Convert.ToInt64(employeeDatas[0]["FDept"]));

            }


            //带出当前组织
            int zhuzhi = Convert.ToInt32(this.Context.CurrentOrganizationInfo.ID);
            this.View.Model.SetValue("FCorrespondOrgId", zhuzhi);



        }

        public override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.View.GetControl<EntryGrid>("FEntity").SetRowHeight(30);
        }
        
        /// <summary>
        /// 数字转中文
        /// </summary>
        /// <param name="number">eg: 22</param>
        /// <returns></returns>
        private string NumberToChinese(int number)
        {
            string res = string.Empty;
            string str = number.ToString();
            string schar = str.Substring(0, 1);
            switch (schar)
            {
                case "1":
                    res = "一";
                    break;
                case "2":
                    res = "二";
                    break;
                case "3":
                    res = "三";
                    break;
                case "4":
                    res = "四";
                    break;
                case "5":
                    res = "五";
                    break;
                case "6":
                    res = "六";
                    break;
                case "7":
                    res = "七";
                    break;
                case "8":
                    res = "八";
                    break;
                case "9":
                    res = "九";
                    break;
                default:
                    res = "零";
                    break;
            }
            if (str.Length > 1)
            {
                switch (str.Length)
                {
                    case 2:
                    case 6:
                        res += "十";
                        break;
                    case 3:
                    case 7:
                        res += "百";
                        break;
                    case 4:
                        res += "千";
                        break;
                    case 5:
                        res += "万";
                        break;
                    default:
                        res += "";
                        break;
                }
                res += NumberToChinese(int.Parse(str.Substring(1, str.Length - 1)));
            }
            return res;
        }
    }
}
