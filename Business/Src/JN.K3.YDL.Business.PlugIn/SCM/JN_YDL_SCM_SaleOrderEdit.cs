using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using JN.K3.YDL.ServiceHelper.SCM;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SystemParameter.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Orm;
using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.DynamicForm;

namespace JN.K3.YDL.Business.PlugIn.SCM
{
    [Description("销售订单-表单插件")]
    public class JN_YDL_SCM_SaleOrderEdit : AbstractBillPlugIn
    {

        /// <summary>
        /// 新增后
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateModelData(EventArgs e)
        {
            base.AfterCreateModelData(e);
            //this.View.Model.SetValue("FSaleOrgId", Convert.ToInt32(this.Context.CurrentOrganizationInfo.ID));
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

        private bool isBuyOder = false;
        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
 	        base.BeforeSave(e);
            isBuyOder = this.View.Model.GetValue("FSrcType", 0).Equals("PUR_PurchaseOrder");
            if (isBuyOder)
            {
                this.View.Model.SetValue("FHEADLOCID", this.View.Model.GetValue("FrelationHEADLOCID"));
                this.View.Model.SetValue("FReceiveAddress", this.View.Model.GetValue("FrelationAddress"));

            }
            else
            {
                this.View.Model.SetValue("FrelationHEADLOCID", this.View.Model.GetValue("FHEADLOCID"));
                this.View.Model.SetValue("FrelationAddress", this.View.Model.GetValue("FReceiveAddress"));
            }
        }

        /// <summary>
        /// 值更新事件
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            DynamicObject dynamicCust = this.View.Model.DataObject["CustId"] as DynamicObject;
            DynamicObject dynamicSaler = this.View.Model.DataObject["SalerId"] as DynamicObject;
            
            DynamicObjectCollection SaleOrderFinance = this.View.Model.DataObject["SaleOrderFinance"] as DynamicObjectCollection;
            DynamicObject dynamicCurr = SaleOrderFinance[0]["SettleCurrId"] as DynamicObject;
          
            DynamicObjectCollection dynamicEntry = this.View.Model.DataObject["SaleOrderEntry"] as DynamicObjectCollection;
            if (dynamicEntry.Count == 0) return;
            DynamicObject dynamicMater = dynamicEntry[e.Row]["MaterialId"] as DynamicObject;
            DynamicObject dynamicAuxPro = dynamicEntry[e.Row]["AuxPropId"] as DynamicObject;
            if ((e.Field.Key.ToUpper() == "FCUSTID" || e.Field.Key.ToUpper() == "FSALERID" || e.Field.Key.ToUpper() == "FMATERIALID" || e.Field.Key.ToUpper() == "FSETTLECURRID" || e.Field.Key.ToUpper() == "FAUXPROPID")
                && (dynamicCust != null && dynamicSaler != null && dynamicMater != null && dynamicAuxPro != null  && dynamicCurr != null))
            { 
                long custId = Convert.ToInt64(dynamicCust["Id"]);
                long saleId = Convert.ToInt64(dynamicSaler["Id"]);
                long materId = Convert.ToInt64(dynamicMater["Id"]);
                long CurrId = Convert.ToInt64(dynamicCurr["Id"]);
                string AuxPropId = Convert.ToString(dynamicAuxPro["F100001_Id"]);
                DynamicObjectCollection dynamicObj = SaleQuoteServiceHelper.SelectSALPrice(this.Context, custId, saleId, CurrId, AuxPropId, materId);
                if (dynamicObj.Count == 0) return;
    
                int saleorderId = Convert.ToInt32(dynamicObj[0]["FID"]);
                decimal price = Convert.ToDecimal(dynamicObj[0]["FPRICE"]);



                //dynamicEntry[e.Row]["PriceListEntry_Id"] = saleorderId;
                //Kingdee.BOS.ServiceHelper.DBServiceHelper.LoadReferenceObject(this.Context, new DynamicObject[] { this.View.Model.DataObject }, this.View.BusinessInfo.GetDynamicObjectType(), false);
                this.View.Model.SetValue("FPriceListEntry", saleorderId, e.Row);
                this.View.Model.SetValue("FTaxPrice", price, e.Row);
                this.View.UpdateView("FSaleOrderEntry");
                this.View.InvokeFieldUpdateService("FTaxPrice", e.Row);
            }           
        }

        /// <summary>
        /// 保存事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterSave(Kingdee.BOS.Core.Bill.PlugIn.Args.AfterSaveEventArgs e)
        {
            base.AfterSave(e);
            this.View.UpdateView("FJNPriceChange");
      
        }





        /// F7事件
        /// </summary>
        /// <param name="e"></param>

        
        public override void BeforeF7Select(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.FieldKey == "FMaterialId")
            {
                DynamicObject obj = this.View.Model.GetValue("FBILLTYPEID") as DynamicObject;
                string billType = Convert.ToString(obj["id"]);
                if (billType == "572ff34c466a39")  //特配
                {
                    if (!string.IsNullOrWhiteSpace(e.ListFilterParameter.Filter))
                    {
                        e.ListFilterParameter.Filter += " And ";
                    }
                    e.ListFilterParameter.Filter += " F_JN_SpecialMap=1 "; 
                }
                else if (billType == "572ff335466873" || billType == "eacb50844fc84a10b03d7b841f3a6278") //插单
                {
                    if (!string.IsNullOrWhiteSpace(e.ListFilterParameter.Filter))
                    {
                        e.ListFilterParameter.Filter += " And ";
                    }
                    e.ListFilterParameter.Filter += " F_JN_SpecialMap=0 ";
                }
            }
        }
        
        /// <summary>
        /// 模糊查询
        /// </summary>
        /// <param name="e"></param>
        
        public override void BeforeSetItemValueByNumber(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeSetItemValueByNumberArgs e)
        {
            base.BeforeSetItemValueByNumber(e);
            if (e.BaseDataFieldKey == "FMaterialId")
            {
                DynamicObject obj = this.View.Model.GetValue("FBILLTYPEID") as DynamicObject;
                string billType = Convert.ToString(obj["id"]);
                if (billType == "572ff34c466a39")
                {
                    if (!string.IsNullOrWhiteSpace(e.Filter))
                    {
                        e.Filter += " And ";
                    }
                    e.Filter += " F_JN_SpecialMap=1 "; 
                }
                else if (billType == "572ff335466873" || billType == "eacb50844fc84a10b03d7b841f3a6278")
                {
                    if (!string.IsNullOrWhiteSpace(e.Filter))
                    {
                        e.Filter += " And ";
                    }
                    e.Filter += " F_JN_SpecialMap=0 ";
                }
            }
        }
        
    }
}
