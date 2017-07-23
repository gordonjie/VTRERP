using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.DynamicForm;

namespace JN.K3.YDL.Business.PlugIn.SCM
{

    /// <summary>
    /// 出库单出库日期校验
    /// </summary>
    [Description("出库单出库日期校验")]
    public class OutStockDateValidator : AbstractBillPlugIn
    {
        long[] orgIds = new long[7] { 195814, 195815, 195816, 2781316, 2781329, 2781360, 2781361 };//二期的组织
        /// <summary>
        /// 表单操作前事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            if (e.Operation.FormOperation.Operation.ToString() == "Save")//保存操作时
            {
                if (Convert.ToBoolean(e.Operation.View.OpenParameter.GetCustomParameter("isPassed"))) //检验通过再次调用操作判断定制参数
                {
                    e.Operation.View.OpenParameter.SetCustomParameter("isPassed", false);
                    return;
                }
                string formKey = this.View.OpenParameter.FormId; //单据标示
                DynamicObjectCollection entryData = null;
                long orgId = 0;//组织Id
                switch (formKey)
                {
                    case "SAL_OUTSTOCK":
                        entryData = this.View.Model.DataObject["SAL_OUTSTOCKENTRY"] as DynamicObjectCollection;//销售出库单
                        orgId = Convert.ToInt64(this.View.Model.DataObject["StockOrgId_Id"]);//发货组织
                        break;
                    case "STK_MisDelivery":
                        entryData = this.View.Model.DataObject["BillEntry"] as DynamicObjectCollection;//其他出库单
                        orgId = Convert.ToInt64(this.View.Model.DataObject["StockOrgId_Id"]);//库存组织
                        break;
                    case "SP_PickMtrl":
                        entryData = this.View.Model.DataObject["Entity"] as DynamicObjectCollection;//简单生产领料单
                        orgId = Convert.ToInt64(this.View.Model.DataObject["StockOrgId_Id"]);//发料组织
                        break;
                    case "PRD_PickMtrl":
                        entryData = this.View.Model.DataObject["Entity"] as DynamicObjectCollection;//生产领料单
                        orgId = Convert.ToInt64(this.View.Model.DataObject["StockOrgId_Id"]);//发料组织
                        break;
                    case "STK_TransferDirect":
                        entryData = this.View.Model.DataObject["TransferDirectEntry"] as DynamicObjectCollection;//直接调拨单
                        orgId = Convert.ToInt64(this.View.Model.DataObject["StockOutOrgId_Id"]);//调出库存组织
                        break;
                    default:
                        break;
                }
                if (orgIds.Contains(orgId)==false)//新合新跳过
                {
                DateTime outStockDate = Convert.ToDateTime(this.View.Model.DataObject["Date"]);//出库日期
                StringBuilder erroeText = new StringBuilder();
                if (entryData == null || entryData.Count == 0)
                {
                    return;
                }
                foreach (DynamicObject item in entryData)
                {
                    DynamicObject material = item["MaterialID"] as DynamicObject;//物料
                    if (material == null)
                    {
                        continue;
                    }
                    string lot = "";//批号
                    if (item["Lot_Text"] == null)
                    {
                        continue;
                    }
                    else
                    {
                        lot = item["Lot_Text"].ToString();
                    }
                    long materialId = Convert.ToInt64(material["Id"]);
                    QueryBuilderParemeter para = new QueryBuilderParemeter();
                    para.FormId = "BD_BatchMainFile";//批号主档
                    para.FilterClauseWihtKey = string.Format("FMATERIALID={0} and FNUMBER='{1}' and FCREATEORGID={2} and FINSTOCKDATETMP is not null and FStockDirect=1 "//2017年6月增加入库方向条件
                                                , materialId, lot, orgId);//查询条件：物料编码、批号、组织
                    para.SelectItems = SelectorItemInfo.CreateItems(" FINSTOCKDATETMP ");//入库日期
                    DynamicObjectCollection employeeDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
                    if (employeeDatas == null || employeeDatas.Count == 0)
                    {
                        continue;
                    }
                    if (outStockDate <= Convert.ToDateTime(employeeDatas[0]["FINSTOCKDATETMP"]))//出库日期小于或等于入库日期时
                    {
                        erroeText.AppendFormat("{0},", entryData.IndexOf(item) + 1);
                    }
                }
                if (erroeText.Length > 0)
                {
                    string errorLog = erroeText.ToString().Substring(0, erroeText.Length - 1);
                    this.View.ShowErrMessage(string.Format("明细分录中{0}行入库日期大于出库日期，不允许保存！", errorLog));
                    e.Cancel = true;
                }
              }
            }
        }

    }
}
