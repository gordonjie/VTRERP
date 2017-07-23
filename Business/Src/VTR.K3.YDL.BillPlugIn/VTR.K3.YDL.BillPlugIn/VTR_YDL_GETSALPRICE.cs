using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;

namespace VTR.K3.YDL.BillPlugIn
{
    public class VTR_YDL_GETSALPRICE : AbstractBillPlugIn
    {
        private DynamicObject currRow = null;
        private int rowindex = 0;
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string str = e.Field.Key;

            if (str == "FCustId" || str == "FSettleCurrId" || str == "FSaleGroupId" || str == "FSalerId" || str == "FMaterialId" || str == "FAuxPropId")
            {
                
                DateTime reqdate = Convert.ToDateTime(this.View.Model.GetValue("FDate"));
                this.Model.TryGetEntryCurrentRow("FSaleOrderEntry", out currRow, out rowindex);              
                int rows= this.Model.GetEntryRowCount("FSaleOrderEntry");
                 if (rows == 0) return;
                 for (int i = 0; i < rows; i++)
                 {
                     //if (currRow == null) return;
                     //int index = Convert.ToInt32(currRow["SEQ"]) - 1;
                     this.View.Model.SetValue("FPriceListEntry", 0, i);

                     DynamicObject FMATERIALID = this.View.Model.GetValue("FMATERIALID", i) as DynamicObject;
                     DynamicObject FAuxPropId = this.View.Model.GetValue("FAuxPropId", i) as DynamicObject;
                     DynamicObject FCustId = this.View.Model.GetValue("FCustId") as DynamicObject;
                     DynamicObject FSettleCurrId = this.View.Model.GetValue("FSettleCurrId") as DynamicObject;
                     DynamicObject FSaleGroupId = this.View.Model.GetValue("FSaleGroupId") as DynamicObject;
                     DynamicObject FSalerId = this.View.Model.GetValue("FSalerId") as DynamicObject;

                     if (FMATERIALID == null || FAuxPropId == null || FCustId == null || FSettleCurrId == null || FSaleGroupId == null || FSalerId == null) return;
                     DynamicObject AuxProp = FAuxPropId["F100001"] as DynamicObject;
                     if (AuxProp == null) return;
                     var materid = FMATERIALID["id"];

                     var AuxPropid = AuxProp["id"];

                     var CustId = FCustId["id"];
                     var SettleCurrId = FSettleCurrId["id"];
                     var SaleGroupId = FSaleGroupId["id"];
                     var SalerId = FSalerId["id"];
                     var sql = string.Format(@"select top 1 t2.FID from  T_SAL_PRICELISTENTRY t1 join T_SAL_PRICELIST t2 on t1.FID=t2.FID 
                  left join T_SAL_APPLYCUSTOMER t3 on t3.FID=t1.FID
                  left join T_SAL_APPLYSALESMAN t4 on t4.FID=t1.FID
                  left join T_BD_FLEXSITEMDETAILV t5 on t5.FID=t1.FAUXPROPID  
where t1.FMATERIALID='{0}' and t5.FF100001='{1}' and  t1.FEFFECTIVEDATE<= '{2}' and t1.FEXPRIYDATE>='{3}' 
and (t3.FCustID='{4}' or t3.FCustID is null) and (t4.FSalerId='{5}' or t4.FSalerId is null) and (t2.FCurrencyId='{6}') order by t2.FEFFECTIVEDATE DESC ", materid, AuxPropid, reqdate, reqdate, CustId, SalerId, SettleCurrId);
                     var objs = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                     if (objs != null && objs.Count > 0)
                     {
                         Entity entitys = this.View.BusinessInfo.GetEntity("FSaleOrderEntry");
                         var FEntityDatas = this.View.Model.GetEntityDataObject(entitys);
                         int pricelist = Convert.ToInt32(objs[0]["FID"]);
                         //FEntityDatas[index]["FPriceList"][""]=pricelist;
                         this.View.Model.SetValue("FPriceListEntry", pricelist, i);
                         //FEntityDatas[index]["EvaluatePrice"] = objs[0]["FPRICE"];
                     }
                 }
                    this.View.UpdateView("FPriceListEntry");
                    this.View.UpdateView("FSaleOrderEntry");
            }
        }
    }
}
