using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;

namespace VTR.K3.Bill.PlugIn
{
    /// <summary>
    /// 销售订单维护插件
    /// </summary>
     [System.ComponentModel.Description("销售订单维护插件")]
    public class SaleOrderEdit : AbstractBillPlugIn
    {
         private bool isBuyOder = false;
        /// <summary>
        /// 保存前事件
        /// </summary>
        /// <param name="e"></param>
         public override void BeforeSave(BeforeSaveEventArgs e)
         {
            //销售订单地址和关联地址处理
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

            //提成分配比相加到达100%
            double saler1Percent = Convert.ToDouble(this.View.Model.GetValue("FJNSaler1Percent"));
            double saler2Percent = Convert.ToDouble(this.View.Model.GetValue("FJNSALE2PERCENT"));
            double sum = saler1Percent + saler2Percent;
            if (sum != 100d)
            {
                this.View.ShowErrMessage("提成分配比合计必须为100%");
                e.Cancel = true;
                return;
            }
        }

    }
}
