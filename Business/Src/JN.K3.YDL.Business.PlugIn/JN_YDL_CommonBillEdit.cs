using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using JN.K3.YDL.ServiceHelper;

namespace JN.K3.YDL.Business.PlugIn
{
    /// <summary>
    /// 取辅助资料属性值
    /// </summary>
    [Description("取辅助资料属性值")]
   public class JN_YDL_CommonBillEdit: AbstractBillPlugIn
    {
        /// <summary>
        /// 数据更新事件后
        /// </summary>
        public override void DataChanged(DataChangedEventArgs e)
        {
            switch (e.Field.Key.ToUpperInvariant())
            {
                case "FAUXPROPID":
                    updateAuxPropValue();
                    break;
            }
                    base.DataChanged(e);
            
        }

        private void updateAuxPropValue()
        {
            string entryKey = "";
            switch (this.View.UserParameterKey.ToUpperInvariant())
            {
                //销售订单
                case "SAL_SALEORDER":
                    entryKey = "SaleOrderEntry";
                    break;
                //采购订单
                case "PUR_PURCHASEORDER":
                    entryKey = "POOrderEntry";
                    break;
                //采购申请单
                case "PUR_REQUISITION":
                    entryKey = "ReqEntry";
                    break;
                //销售出库单
                case "SAL_OUTSTOCK":
                    entryKey = "SAL_OUTSTOCKENTRY";
                    break;
                //其他出库单
                case "STK_MISDELIVERY":
                    entryKey = "BillEntry";
                    break;
                //采购收料单
                case "PUR_RECEIVEBILL":
                    entryKey = "PUR_ReceiveEntry";
                    break;
                //直接调拨单
                case "STK_TRANSFERDIRECT":
                    entryKey = "TransferDirectEntry";
                    break;
                //销售退货单
                case "SAL_RETURNSTOCK":
                    entryKey = "SAL_RETURNSTOCKENTRY";
                    break;

                //报告
                case "VTR_REPORT":
                    entryKey = "FEntity";
                    break;
                default:
                    return;
            }

            DynamicObject billData = this.View.Model.DataObject;
            DynamicObjectCollection entryData = billData[entryKey] as DynamicObjectCollection;
            if (entryData != null && entryData.Count > 0)
            {
                for (int i = 0; i < entryData.Count; i++)
                {
                    string auxpropValue = "";
                    DynamicObject rowData = entryData[i]["AuxPropId"] as DynamicObject;
                    if (rowData != null)
                    {
                        //包装规格
                        DynamicObject valueData = rowData["F100001"] as DynamicObject;
                        if (valueData != null)
                        {
                            if (valueData["FDataValue"] != null)
                            {
                                auxpropValue = string.Format("包装规格:{0}", valueData["FDataValue"].ToString());
                            }
                        }

                        //具体名称描述
                        if (rowData["F100002"] != null)
                        {
                            if (auxpropValue != "")
                            {
                                auxpropValue += ",";
                            }
                            auxpropValue += string.Format("具体名称描述:{0}", rowData["F100002"].ToString());
                        }
                    }

                    this.View.Model.SetValue("F_JN_YDL_AuxPropValue", auxpropValue, i);
                }
            }
        }
        /*
        public override void DataUpdateEnd()
        {
            base.DataUpdateEnd();
            string entryKey = "";
            switch (this.View.UserParameterKey.ToUpperInvariant())
            {
                //销售订单
                case "SAL_SALEORDER":
                    entryKey = "SaleOrderEntry";
                    break;
                //采购订单
                case "PUR_PURCHASEORDER":
                    entryKey = "POOrderEntry";
                    break;
                //采购申请单
                case "PUR_REQUISITION":
                    entryKey = "ReqEntry";
                    break;
                //销售出库单
                case "SAL_OUTSTOCK":
                    entryKey = "SAL_OUTSTOCKENTRY";
                    break;
                //其他出库单
                case "STK_MISDELIVERY":
                    entryKey = "BillEntry";
                    break;
                //采购收料单
                case "PUR_RECEIVEBILL":
                    entryKey = "PUR_ReceiveEntry";
                    break;
                //直接调拨单
                case "STK_TRANSFERDIRECT":
                    entryKey = "TransferDirectEntry";
                    break;
                //销售退货单
                case "SAL_RETURNSTOCK":
                    entryKey = "SAL_RETURNSTOCKENTRY";
                    break;
                default:
                    return;
            }
            
            DynamicObject billData = this.View.Model.DataObject;
            DynamicObjectCollection entryData = billData[entryKey] as DynamicObjectCollection;
            if (entryData != null && entryData.Count > 0)
            {
                for (int i = 0; i < entryData.Count; i++)
                {
                    string auxpropValue = "";
                    DynamicObject rowData = entryData[i]["AuxPropId"] as DynamicObject;
                    if (rowData != null)
                    {
                        //包装规格
                        DynamicObject valueData = rowData["F100001"] as DynamicObject;
                        if (valueData != null)
                        {
                            if (valueData["FDataValue"] != null)
                            {
                                auxpropValue = string.Format("包装规格:{0}", valueData["FDataValue"].ToString());
                            }                            
                        }

                        //具体名称描述
                        if (rowData["F100002"] != null)
                        {
                            if (auxpropValue != "")
                            {
                                auxpropValue += ",";
                            }
                            auxpropValue += string.Format("具体名称描述:{0}", rowData["F100002"].ToString());
                        }                      
                    }

                    this.View.Model.SetValue("F_JN_YDL_AuxPropValue", auxpropValue, i);
                }
            }
        }*/
    }
}
