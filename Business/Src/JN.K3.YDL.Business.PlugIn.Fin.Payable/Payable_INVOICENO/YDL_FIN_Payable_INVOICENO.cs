using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.Plugin.Fin.Payable
{
  [Description("应付单-表单发票号码插件")]
    public class YDL_FIN_Payable_INVOICENO : AbstractBillPlugIn
    {
       


        public override void AfterSave(Kingdee.BOS.Core.Bill.PlugIn.Args.AfterSaveEventArgs e)
        {
 	         base.AfterSave(e);
  
            EntryEntity entity = this.View.BusinessInfo.GetEntryEntity("FPURCHASEICENTRY");
            DynamicObjectCollection dyoCollection = this.Model.GetEntityDataObject(entity);
            string nowINVOICENo =this.View.Model.GetValue("FINVOICENO").ToString();

            int rows = this.Model.GetEntryRowCount("FPURCHASEICENTRY");
            for (int rowindex = 0; rowindex < rows; rowindex++)
            {
                DynamicObject entryRow = dyoCollection[rowindex];
                string Srcbillno=this.View.Model.GetValue("FSRCBILLNO",rowindex).ToString();
                var sql = string.Format("{0}{1}{2}", @"/*dialect*/ select t1.FINVOICENO from T_AP_PAYABLEENTRY t1 join T_AP_PAYABLE t2 on t1.FID=t2.FID join T_IV_PURCHASEICENTRY t3 on t2.FBILLNO='", Srcbillno, "'");
                DynamicObjectCollection objs = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                DynamicObject objsRow = objs[0];
                string oldvalue = objsRow[0].ToString();
                if (objs != null && objs.Count > 0)
                {
                    string newINVOICENo = string.Format("{0}{1}", oldvalue, nowINVOICENo);
                    var secentityid=entryRow[3];

                    var sqlupdate = string.Format("{0}{1}{2}{3}{4}",@"/*dialect*/ update T_AP_PAYABLEENTRY  set FINVOICENO='",newINVOICENo,"' where FENTRYID in (select t1.FENTRYID from T_AP_PAYABLEENTRY t1 join T_AP_PAYABLE t2 on t1.FID=t2.FID join T_IV_PURCHASEICENTRY t3 on t2.FBILLNO=t3.FSRCBILLNO where t1.FENTRYID=",secentityid," )");
                    DBServiceHelper.Execute(this.Context, sqlupdate);
                }


                /*
               // var FMATERIALID = this.Model.GetValue("FENTRYID", rowindex) as DynamicObject;
                //DynamicObject FMATERIALID = this.View.Model.GetValue("FENTRYID", rowindex) as DynamicObject;
                //var materid = FMATERIALID["FINVOICENO"];
                
                string INVOICEEntryid = entryRow[0].ToString();
                var sql = string.Format("{0}{1}{2}", @"/*dialect*//* select top 1 t1.FENTRYID,INVOICENO =STUFF((select ','+ tb2.FINVOICENO  from T_IV_PURCHASEIC  tb2 join T_IV_PURCHASEICENTRY tb3 on tb2.FID=tb3.FID join T_AP_PAYABLE tb4 on tb3.FSRCBILLNO=tb4.FBILLNO join T_AP_PAYABLEENTRY tb5 on tb5.FID=tb4.FID where tb5.FENTRYID=t1.FENTRYID and tb3.FENTRYID=t3.FENTRYID  and t2.FCANCELSTATUS='A' and t2.FDOCUMENTSTATUS='C' FOR xml path('')), 1, 1, '') from T_AP_PAYABLEENTRY t1 join T_AP_PAYABLE t2 on t1.FID=t2.FID join T_IV_PURCHASEICENTRY t3 on  t2.FBILLNO=t3.FSRCBILLNO  join T_IV_PURCHASEIC t4 on t4.FID=t3.FID where t1.FENTRYID=", INVOICEEntryid, @" group by t1.FENTRYID,t3.FENTRYID,t2.FCANCELSTATUS,t2.FDOCUMENTSTATUS");

                var objs = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                if (objs != null && objs.Count > 0)
                {
                    Entity entitys = this.View.BusinessInfo.GetEntity("FEntityDetail");
                    var FEntityDatas = this.View.Model.GetEntityDataObject(entitys);
                    FEntityDatas[rowindex]["FTaxPrice"] = objs[0]["FTAXPRICE"];
                    FEntityDatas[rowindex]["EvaluatePrice"] = objs[0]["FPRICE"];
                    this.View.UpdateView("FEntityDetail");*/
                }
            }
        }
       }

