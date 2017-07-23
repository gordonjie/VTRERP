using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.ServiceHelper;

namespace JN.K3.VTR.Fin.ServicePlugIn
{
    [Description("付款申请-付款单转换插件")]
    public class PAYAPPLYOrderToPAYBILL : AbstractConvertPlugIn
    {
        /// <summary>
        /// 最后触发：单据转换后事件
        /// </summary>
        /// <param name="e"/>
        public override void AfterConvert(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.AfterConvertEventArgs e)
        {
            ExtendedDataEntity[] array = e.Result.FindByEntityKey("FBillHead");

            foreach (ExtendedDataEntity extendedDataEntity in array)
            {
                long orgid = Convert.ToInt32(extendedDataEntity.DataEntity["FPAYORGID"]);
                DynamicObjectCollection dynamicObjectCollection = (extendedDataEntity.DataEntity["PAYBILLENTRY"] as DynamicObjectCollection);

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
                    DynamicObjectCollection bomDataCollection = CommonServiceHelper.GetAcctBookData(this.Context, orgid, materialid, materialid);
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
