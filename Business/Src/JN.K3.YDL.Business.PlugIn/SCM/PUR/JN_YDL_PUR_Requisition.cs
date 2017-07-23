using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Metadata;
namespace JN.K3.YDL.Business.PlugIn.SCM.PUR
{
    /// <summary>
    /// 采购申请单-表单插件
    /// </summary>
    [Description("采购申请单-表单插件")]
    public class JN_YDL_PUR_Requisition : AbstractBillPlugIn
    {
        private DynamicObject currRow = null;
        private int rowindex = 0;


        /// <summary>
        /// 值更新事件
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            switch (e.Field.Key.ToUpperInvariant())
            {
                case "FSUGGESTSUPPLIERID":
                    DateTime applicationDate = Convert.ToDateTime(base.View.Model.GetValue("FApplicationDate"));

                    DynamicObject materialObj = base.View.Model.GetValue("FMATERIALID", e.Row) as DynamicObject;
                    DynamicObject supplierObj = base.View.Model.GetValue("FSuggestSupplierId", e.Row) as DynamicObject;
                    DynamicObject auxpropObj = base.View.Model.GetValue("FAuxpropId", e.Row) as DynamicObject;
                    DynamicObjectCollection priceObj = null;
                    if (materialObj != null && supplierObj != null)
                    {
                        long materialId = Convert.ToInt64(materialObj["Id"]);
                        long supplierId = Convert.ToInt64(supplierObj["Id"]);
                        string auxpropId = "";
                        if (auxpropObj != null)
                        {
                            auxpropId = Convert.ToString(auxpropObj["F100001_Id"]);
                        }
                            if (auxpropId != "")
                            {
                                priceObj = YDLCommServiceHelper.GetAuxpropPriceListId(this.Context, materialId, auxpropId, supplierId, applicationDate);
                            }
                        
                            else
                            {
                                //更新价目表,单价
                                priceObj = YDLCommServiceHelper.GetPriceListId(this.Context, materialId, supplierId, applicationDate);
                            }
                            if (priceObj != null && priceObj.Count > 0)
                            {
                                this.View.Model.SetValue("FPriceList", priceObj[0]["FID"], e.Row);
                                this.View.Model.SetValue("F_JN_TaxPrice", priceObj[0]["FTAXPRICE"], e.Row);
                                this.View.Model.SetValue("FEvaluatePrice", priceObj[0]["FPRICE"], e.Row);
                            }
                       

                    }
                    else
                    {
                        this.View.Model.SetValue("FPriceList", 0, e.Row);//清空价目表
                    }
                    break;

                case "FAUXPROPID":
                    applicationDate = Convert.ToDateTime(base.View.Model.GetValue("FApplicationDate"));

                    materialObj = base.View.Model.GetValue("FMATERIALID", e.Row) as DynamicObject;
                    supplierObj = base.View.Model.GetValue("FSuggestSupplierId", e.Row) as DynamicObject;
                    auxpropObj = base.View.Model.GetValue("FAuxpropId", e.Row) as DynamicObject;
                    
                    if (materialObj != null && supplierObj != null)
                    {
                        long materialId = Convert.ToInt64(materialObj["Id"]);
                        long supplierId = Convert.ToInt64(supplierObj["Id"]);
                        string auxpropId = "";
                        if (auxpropObj != null)
                        {
                            auxpropId = Convert.ToString(auxpropObj["F100001_Id"]);
                        }
                        if (auxpropId != "")
                        {
                            priceObj = YDLCommServiceHelper.GetAuxpropPriceListId(this.Context, materialId, auxpropId, supplierId, applicationDate);
                         }
                        else
                        {
                            //更新价目表,单价
                            priceObj = YDLCommServiceHelper.GetPriceListId(this.Context, materialId, supplierId, applicationDate);
                        }
                        if (priceObj != null && priceObj.Count > 0)
                        {
                            this.View.Model.SetValue("FPriceList", priceObj[0]["FID"], e.Row);
                            this.View.Model.SetValue("F_JN_TaxPrice", priceObj[0]["FTAXPRICE"], e.Row);
                            this.View.Model.SetValue("FEvaluatePrice", priceObj[0]["FPRICE"], e.Row);
                        }

                    }
                    else
                    {
                        this.View.Model.SetValue("FPriceList", 0, e.Row);//清空价目表
                    }
                    break;
            }
        }

        /// <summary>
        /// F7选择前事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeF7Select(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.BaseDataField == null) return;
            if (e.BaseDataField.Key == "FMaterialId")
            {
                //根据单据类型选择不同存货类别的物料               
                DynamicObject billType = this.View.Model.GetValue("FBillTypeID") as DynamicObject;
                string typeName = billType["Name"].ToString();
                int zichan = 40040;
                //int feiYong = 137541;
                int feiyongshuxing = 11;
                string filter = "";
                switch (typeName)
                {
                    case "标准采购申请":
                        filter = string.Format("FCATEGORYID not in ({0}) and FERPCLSID not in ({1})", zichan, feiyongshuxing);
                        break;
                    case "资产采购申请单":
                        filter = string.Format("FCATEGORYID={0}", zichan);
                        break;
                    case "费用采购申请":
                        filter = string.Format("FERPCLSID ={0}", feiyongshuxing);
                        break;
                }
                if (string.IsNullOrWhiteSpace(e.ListFilterParameter.Filter))
                {
                    e.ListFilterParameter.Filter = filter;
                }
                else
                {
                    e.ListFilterParameter.Filter += string.Format(" And ({0}) ", filter);
                }
            }
        }

        /// <summary>
        /// 模糊查询前事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeSetItemValueByNumber(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeSetItemValueByNumberArgs e)
        {
            base.BeforeSetItemValueByNumber(e);
            if (e.BaseDataField == null) return;
            if (e.BaseDataField.Key == "FMaterialId")
            {
                //根据单据类型选择不同存货类别的物料               
                DynamicObject billType = this.View.Model.GetValue("FBillTypeID") as DynamicObject;
                string typeName = billType["Name"].ToString();
                int zichan = 40040;
                int feiYong = 137541;
                string filter = "";
                switch (typeName)
                {
                    case "标准采购申请":
                        filter = string.Format("FCATEGORYID not in ({0},{1})", zichan, feiYong);
                        break;
                    case "资产采购申请单":
                        filter = string.Format("FCATEGORYID={0}", zichan);
                        break;
                    case "费用采购申请":
                        filter = string.Format("FCATEGORYID={0}", feiYong);
                        break;
                }
                if (string.IsNullOrWhiteSpace(e.Filter))
                {
                    e.Filter = filter;
                }
                else
                {
                    e.Filter += string.Format(" And ({0}) ", filter);
                }
            }
        }

        /// <summary>
        /// 审批流属性设置
        /// </summary>
        /// <param name="e"></param>

        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            //内蒙古成品、半成品
            int rows = this.View.Model.GetEntryRowCount("FEntity");
            int Isflows = 0;
            int IsIT = 0;
            string DEPname = "";//首行需求部门
            string DEPname2 = "";//其他行需求部门
            int Isboss = 0;
            var billtype = this.View.Model.GetValue("FBillTypeID") as DynamicObject;
            string billtypename = Convert.ToString(billtype["Number"]);
            for (int i = 0; i < rows; i++)
            {
                //string needcheck=this.View.Model.GetValue("F_kk_BaseProperty",i).ToString();
                DynamicObject materialObj = base.View.Model.GetValue("FMATERIALID", i) as DynamicObject;
                DynamicObject RequireDept = base.View.Model.GetValue("FRequireDeptId", i) as DynamicObject;
                double ReferPrice = Convert.ToDouble(this.View.Model.GetValue("F_JNReferPrice", i));
                if (ReferPrice >= 5000 && billtypename == "CGSQD03_SYS")
                {
                    Isboss = 1;
                }
                if (RequireDept != null && billtypename == "CGSQD03_SYS")
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
                        e.Cancel=true;
                        return;
                    }
                    }

                }

                if (materialObj != null)
                {
                    DynamicObjectCollection materialbase = materialObj["materialbase"] as DynamicObjectCollection;
                    DynamicObject Category = materialbase[0]["CategoryID"] as DynamicObject;
                    string CategoryName = Convert.ToString(Category["Name"]);
                    string CategoryType = Convert.ToString(materialbase[0]["F_JNAssetCombo"]);
                    DynamicObject SupplierObj = base.View.Model.GetValue("FSuggestSupplierId", i) as DynamicObject;
                    if (SupplierObj != null)
                    {
                        // DynamicObjectCollection Supplierbase = materialObj["Supplierbase"] as DynamicObjectCollection;
                        string Supplier = SupplierObj["name"].ToString();
                        if ((CategoryName == "产成品" || CategoryName == "外购内蒙-内蒙" || CategoryName == "半成品") && Supplier == "内蒙古溢多利生物科技有限公司")//供应商为内蒙古的成品和半成品
                        { Isflows = 1; }
                    }
                    if (CategoryType == "A")//IT资产
                    { IsIT = 1; }
                }
            }
            if (Isflows == 1)
            {
                this.View.Model.SetValue("F_JNApprovalflow", "999");
            }
            if (IsIT == 1)
            {
                this.View.Model.SetValue("F_JNMaterialflow", "IT");
            }
            else
            { 
                this.View.Model.SetValue("F_JNMaterialflow", "other"); 
            }
            if (Isboss == 1)
            {
                this.View.Model.SetValue("F_JNApprovalflow", "001");
            }
            this.View.Model.SetValue("F_JNDEPflow", DEPname); 

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
            para.FilterClauseWihtKey = string.Format(" exists (select 1 from t_sec_User where FLinkObject=FPERSONID and FUSERID={0} ) and FUSEORGID={1}", this.Context.UserId, this.Context.CurrentOrganizationInfo.ID);
            para.SelectItems = SelectorItemInfo.CreateItems(" FSTAFFID ");
            var employeeDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
            if (employeeDatas != null && employeeDatas.Count > 0)
            {
                this.View.Model.SetValue("FApplicantId", Convert.ToInt64(employeeDatas[0]["FSTAFFID"]));
            }

            //根据单据类型选择不同申请类别的物料               
            DynamicObject billType = this.View.Model.GetValue("FBillTypeID") as DynamicObject;
            string typeName = billType["Name"].ToString();
            int zichan = 40040;
            int feiYong = 137541;
            string filter = "";
            switch (typeName)
            {
                case "标准采购申请":
                    this.View.Model.SetValue("FRequestType", "Material");
                    break;
                case "资产采购申请单":
                    this.View.Model.SetValue("FRequestType", "Property");
                    break;
                case "费用采购申请":
                    this.View.Model.SetValue("FRequestType", "Expense");
                    break;
            }

        }


    }
}
