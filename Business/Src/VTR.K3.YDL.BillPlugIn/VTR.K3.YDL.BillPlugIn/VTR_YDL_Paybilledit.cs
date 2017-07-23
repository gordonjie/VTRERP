using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.CN.Business.PlugIn.RecPayRefund;

namespace VTR.K3.YDL.BillPlugIn
{
    public class VTR_YDL_Paybilledit : PayBillEdit
    {


  
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
           // base.BeforeF7Select(e);
            string str = e.FieldKey.ToUpperInvariant();
            if (str != null)
            {
                if (!(str == "FPAYITEM"))
                {
                    if (str == "FPAYPURSE")
                    {
                        //e.ListFilterParameter.set_Filter(this.GetFilterPayPurse(e.get_ListFilterParameter().get_Filter()));
                    }
                }
                else
                {
                    this.OpenPreSalesPurOrderList(e.Row);
                }
            }

        }


 

 

        
    }
}
