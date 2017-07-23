using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM.PUR
{
    /// <summary>
    /// 采购订单-表单插件
    /// </summary>
    [Description("采购订单-表单插件")]
    public class JN_YDL_PurchaseOrderEdit : AbstractBillPlugIn
    {
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
                int feiYong = 137541;
                string filter = "";
                switch (typeName)
                {
                    case "标准采购订单":
                        filter = string.Format("FCATEGORYID not in ({0},{1})", zichan, feiYong);
                        break;
                    case "资产采购订单":
                        filter = string.Format("FCATEGORYID={0}", zichan);
                        break;
                    case "费用采购订单":
                        filter = string.Format("FCATEGORYID={0}", feiYong);
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
                    case "标准采购订单":
                        filter = string.Format("FCATEGORYID not in ({0},{1})", zichan, feiYong);
                        break;
                    case "资产采购订单":
                        filter = string.Format("FCATEGORYID={0}", zichan);
                        break;
                    case "费用采购订单":
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
            int rows = this.View.Model.GetEntryRowCount("FPOOrderEntry");
            int Isflows = 0;
            int IsIT = 0;
            string DEPname = "";//首行需求部门
            string DEPname2 = "";//其他行需求部门
            for (int i = 0; i < rows; i++)
            {
                //string needcheck=this.View.Model.GetValue("F_kk_BaseProperty",i).ToString();
                DynamicObject materialObj = base.View.Model.GetValue("FMATERIALID", i) as DynamicObject;
                DynamicObject RequireDept = base.View.Model.GetValue("FRequireDeptId", i) as DynamicObject;
                var billtype = this.View.Model.GetValue("FBillTypeID") as DynamicObject;
                string billtypename = Convert.ToString(billtype["Number"]);
                if (RequireDept != null && billtypename == "CGDD04_SYS")
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

                if (materialObj != null)
                {
                    DynamicObjectCollection materialbase = materialObj["materialbase"] as DynamicObjectCollection;
                    DynamicObject Category = materialbase[0]["CategoryID"] as DynamicObject;
                    string CategoryName = Convert.ToString(Category["Name"]);
                    string CategoryType = Convert.ToString(materialbase[0]["F_JNAssetCombo"]);
                    DynamicObject SupplierObj = base.View.Model.GetValue("FSupplierId") as DynamicObject;
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
            this.View.Model.SetValue("F_JNDEPflow", DEPname); 

        }
    }
}
