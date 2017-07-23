using JN.K3.YDL.ServiceHelper.SCM;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    [Description("资产卡片表单插件")]
    public class JN_YDL_FA_CARD : AbstractBillPlugIn
    {
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            switch (e.Field.Key.ToUpper())
            {
                case "FQUANTITY":
                    var num = this.View.Model.GetValue("FQuantity");
                    var obj =this.View.Model.DataObject;
                    DynamicObjectCollection card = obj["CardDetail"] as DynamicObjectCollection;
                    DynamicObject data = card[0] as DynamicObject;
                    var billnos =data["SourceBillNo"] as string;
                    DynamicObjectCollection OrderBillno = SaleQuoteServiceHelper.SelectOrderBillno(this.Context, billnos);
                    if (OrderBillno != null && OrderBillno.Count > 0)
                    {
                        long FPRICE = Convert.ToInt64(OrderBillno[0]["FPRICE"]) * Convert.ToInt64(num);
                        long TAXAMOUNT = Convert.ToInt64(OrderBillno[0]["TAXAMOUNT"]) * Convert.ToInt64(num);
                        this.View.Model.SetValue("FOriginalCost", FPRICE);
                        this.View.Model.SetValue("FInputTax", TAXAMOUNT);
                        this.View.UpdateView("FOriginalCost");
                        this.View.UpdateView("FInputTax");
                        
                    }
                    break;
            }
        }
    }
}
