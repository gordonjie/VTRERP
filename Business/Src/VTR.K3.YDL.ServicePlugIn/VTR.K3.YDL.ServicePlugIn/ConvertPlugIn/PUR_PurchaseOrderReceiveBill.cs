using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Util;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.App.Core.PlugInProxy;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.App.Core.DefaultValueService;
using Kingdee.BOS.Core.Interaction;


namespace VTR.K3.YDL.ServicePlugIn.ConvertPlugIn
{
    public class PUR_PurchaseOrderReceiveBill : AbstractConvertPlugIn
    {
        public override void AfterConvert(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.AfterConvertEventArgs e)
        {
 	    base.AfterConvert(e);
             ExtendedDataEntity[] data = e.Result.FindByEntityKey("FBillHead");

             foreach (var extendedDataEntity in data)
             {
                 //采购收料单的明细)
                 DynamicObjectCollection entryCollection = null;
                 entryCollection = extendedDataEntity.DataEntity["PUR_ReceiveEntry"] as DynamicObjectCollection;

                 for (int i = 0; i < entryCollection.Count; i++)
                 {
                 
                     double FJNSrcUnitEnzymes =0;
                     double Qty = 0;
                    
                         try{
                             FJNSrcUnitEnzymes = Convert.ToDouble(entryCollection[i]["FJNSrcUnitEnzymes"]);
                             Qty=Convert.ToDouble(entryCollection[i]["BaseUnitQty"]);
                              }
                         catch{
                         }

                     entryCollection[i]["FJNUnitEnzymes"] = FJNSrcUnitEnzymes;
                     entryCollection[i]["ExtAuxUnitQty"] = FJNSrcUnitEnzymes * Qty;

                        


                    
                 }     
                    
                }
            }
        
       

    }
}
