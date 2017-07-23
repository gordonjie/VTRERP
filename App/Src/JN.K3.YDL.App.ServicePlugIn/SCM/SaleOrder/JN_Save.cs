using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.SaleOrder
{
    [Description("销售订单保存服务插件")]
    public class JN_Save : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FTaxPrice");
            e.FieldKeys.Add("FPriceListEntry");
        }
        
        /// <summary>
        /// 事务后事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeginOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            if (e.DataEntitys == null) return;
            var billGroups = e.DataEntitys;
            //List<string> sql = new List<string>();
            foreach (var billGroup in billGroups)
            {
                bool flag = false;
                int Id = Convert.ToInt32(billGroup["Id"]);
                DateTime date = Convert.ToDateTime(billGroup["Date"]);
                DynamicObjectCollection dynamicOs = billGroup["SaleOrderEntry"] as DynamicObjectCollection;//分录明细
                foreach (var dynamicO in dynamicOs)
                {
                    decimal taxPrice = Convert.ToDecimal(dynamicO["TaxPrice"]);//含税单价
                    int auxPropId = Convert.ToInt32(dynamicO["AuxPropId_Id"]);//辅助属性
                    int materialId = Convert.ToInt32(dynamicO["MaterialId_Id"]);//当前物料
                    DynamicObject PriceListEntry = dynamicO["PriceListEntry"] as DynamicObject;//行价目表
                    if (PriceListEntry == null) continue;
                    DynamicObjectCollection salPriceListtrys = PriceListEntry["SAL_PRICELISTENTRY"] as DynamicObjectCollection;//价目表明细 
                    var priceList = salPriceListtrys.Where(f => Convert.ToInt32(f["MaterialId_Id"]) == materialId && Convert.ToDateTime(f["EffectiveDate"]) <= date && Convert.ToDateTime(f["ExpiryDate"]) > date);//物料、生效（失效）日期判断,
                    if (priceList.Count() == 1)
                    {
                        //string sqlUpdate = string.Format("update T_SAL_ORDER set FJNPRICECHANGE='1' where FID={0}", Id);
                        //sql.Add(sqlUpdate); 
                        foreach(var obj in priceList)
                        {
                            //当分录中存在含税单价低于最低限价时，限价标识自动勾选上
                            if (taxPrice < Convert.ToDecimal(obj["DownPrice"]))
                            {
                                flag = true;
                                break;
                            }                           
                        }                        
                    }
                    if (priceList.Count() > 1)
                    {
                        //辅助属性判断
                        var priceEntry = priceList.Where(f => Convert.ToInt32(f["AuxPropId_Id"]) == auxPropId);
                        if (priceEntry.Count() == 1)
                        {
                            //string sqlUpdate = string.Format("update T_SAL_ORDER set FJNPRICECHANGE='1' where FID={0}", Id);
                            //sql.Add(sqlUpdate);
                            foreach (var obj in priceEntry)
                            {
                                //当分录中存在含税单价低于最低限价时，限价标识自动勾选上
                                if (taxPrice < Convert.ToDecimal(obj["DownPrice"]))
                                {
                                    flag = true;
                                    break;
                                }
                            }   
                        }
                    }                                                      
                }
                if (flag)
                {
                    billGroup["FJNPRICECHANGE"] = true;
                    //JN.K3.YDL.Core.AppServiceContext.SaveService.Save(this.Context, new DynamicObject[] { billGroup });//保存数据包
                }
            }            
            //if (sql.Count > 0)
            //{
            //    DBUtils.ExecuteBatch (this.Context, sql,50);//批量处理sql，50表示一次提交50条
            //}
        }
    }
}
