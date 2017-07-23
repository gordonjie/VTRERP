using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;




namespace VTR.K3.YDL.ServicePlugIn
{
    public class SaleBillToPurreq : AbstractConvertPlugIn
    {
        /// <summary>
        /// 最后触发：单据转换后事件
        /// </summary>
        /// <param name="e"/>
        public override void AfterConvert(AfterConvertEventArgs e)
        {
            ExtendedDataEntity[] array = e.Result.FindByEntityKey("FBillHead");

            foreach (ExtendedDataEntity extendedDataEntity in array)
            {


                long orgid = Convert.ToInt32(extendedDataEntity.DataEntity["PurchaseOrgId_Id"]);
                DynamicObjectCollection dynamicObjectCollection = (extendedDataEntity.DataEntity["POOrderEntry"] as DynamicObjectCollection);

                if (dynamicObjectCollection == null)
                {
                    continue;
                }

                BaseDataField priceListDataField = e.TargetBusinessInfo.GetField("FPriceListId") as BaseDataField;

                foreach (DynamicObject dyentry in dynamicObjectCollection)
                {
                    long materialid = Convert.ToInt32(dyentry["MaterialId_Id"]);

                    List<long> lMaterList = new List<long>();
                    lMaterList.Add(materialid);
                    var sql = string.Format(@"select top 1 t3.FMODIFYDATE,t2.FPRICE,t2.FTAXPRICE,t2.FTAXRATE from T_PUR_ReqEntry t1 inner join t_PUR_PriceListEntry t2 on t1.FMATERIALID=t2.FMATERIALID
                                        inner join t_PUR_PriceList t3 on t3.FID=t2.FID where t2.FMATERIALID='{0}' order by t3.FMODIFYDATE DESC", materialid);
                    
                    //DynamicObjectCollection bomDataCollection = CommonServiceHelper.GetFlexValues(this.Context, orgid, lMaterList);
                    var bomDataCollection = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);

                    DynamicObject listdObjects = null;
                    if (bomDataCollection == null)
                    {
                        continue;
                    }

                    listdObjects = bomDataCollection.FirstOrDefault();
                    if (listdObjects != null)
                    {
                        decimal amount, Qty, Price;
                        Qty = Convert.ToDecimal(dyentry["Qty"]);
                        Price = Convert.ToDecimal(listdObjects["FPRICE"]);
                        amount = Qty * Price;
                        dyentry["Amount"] = amount;
                        dyentry["Price"] = listdObjects["FPRICE"];
                        dyentry["TaxPrice"] = listdObjects["FPRICE"];
                    }
                }
            }
        }
    }
}
