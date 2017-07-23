using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.BusinessEntity;
using System;
using System.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.K3.FIN.AP.App.Report;
using Kingdee.BOS.Util;
using Kingdee.BOS.Contracts;
using Kingdee.K3.FIN.App.Core.ARAP.AbstractReport;
using Kingdee.K3.FIN.App.Core;
using Kingdee.K3.FIN.Core;
using Kingdee.BOS.Orm;


namespace VTR.K3.YDL.App.Report
{
    [Description("扩展-应付明细表服务端插件")]


 

    public class JNPAYDETAILREPORTService : APDetailReportService
    {
        private List<SqlObject> sqlList;
        private string startday;
        private string endday;



        protected override void BuildData(string tableName)
        {
             
             
            this.sqlList = new List<SqlObject>();

                if (base.FilterCondition.ViewFromSumReport)
            {
                //base.BuildData(tableName);
                this.sqlList.AddRange(base.InsertTmpData_FromSumRpt());
               
            }
            else
            {
                startday =Convert.ToDateTime(base.FilterCondition.StartDate).ToString("yyyy-MM-dd") ;
                endday = Convert.ToDateTime(base.FilterCondition.EndDate).ToString("yyyy-MM-dd");
        
        this.sqlList.Add(base.InsertTmpData_AP_OrgEndDate());
        this.sqlList.Add(base.InsertTmpData_AP_PayableBill(BusinessTYPEENUM.BeginBalance));
        this.sqlList.Add(base.InsertTmpData_AP_OtherPayAbleBill(BusinessTYPEENUM.BeginBalance));
        this.sqlList.Add(base.InsertTmpData_AP_PayBill(BusinessTYPEENUM.BeginBalance));
        //this.sqlList.Add(this.VTRInsertTmpData_AP_PayableBill(BusinessTYPEENUM.BeginBalance));
        this.sqlList.Add(base.InsertTmpData_AP_RefundBill(BusinessTYPEENUM.BeginBalance));
        this.sqlList.Add(base.InsertTmpData_AP_Match(BusinessTYPEENUM.BeginBalance));
        this.sqlList.Add(base.InsertTmpData_AR_Match(BusinessTYPEENUM.BeginBalance));
        this.sqlList.Add(base.InsertTmpData_AP_InnerPayClear(BusinessTYPEENUM.BeginBalance));
        this.sqlList.Add(base.InsertTmpData_AP_ContactBal(BusinessTYPEENUM.BeginBalance));
        this.sqlList.Add(base.InsertTmpData_AP_PayableBill(BusinessTYPEENUM.RPAmount));
        this.sqlList.Add(base.InsertTmpData_AP_OtherPayAbleBill(BusinessTYPEENUM.RPAmount));
        this.sqlList.Add(base.InsertTmpData_AP_PayBill(BusinessTYPEENUM.Amount));
        this.sqlList.Add(base.InsertTmpData_AP_RefundBill(BusinessTYPEENUM.Amount));
        this.sqlList.Add(base.InsertTmpData_AP_Match(BusinessTYPEENUM.ReversedAmount));
        this.sqlList.Add(base.InsertTmpData_AR_Match(BusinessTYPEENUM.ReversedAmount));
        this.sqlList.Add(base.InsertTmpData_AP_InnerPayClear(BusinessTYPEENUM.ReversedAmount));
        this.sqlList.AddRange(base.InsertTmpData_SumRptData());
        this.sqlList.AddRange(base.DeleteTmpData_DontShowData());
            }

        this.sqlList.Add(base.TransTempDataToBosTable(tableName));

        this.sqlList.RemoveAll(o => o == null);
        DBUtils.ExecuteBatch(this.Context, this.sqlList);
        this.sqlList.Clear();

        this.BuilderReportSql(tableName);
        //this.VTRUpdateRowDataEndBalance(tableName);
        base.UpdateRowDataEndBalance(tableName);
  
        
               
        }


        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            return new List<SummaryField> { new SummaryField("FAMOUNTFOR", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FHadIVAmountFOR", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FREALAMOUNTFOR", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FOFFAMOUNTFOR", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FAMOUNT", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FHADIVAMOUNT", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FREALAMOUNT", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FOFFAMOUNT", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FAMOUNTDIGITSFOR", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.MAX), new SummaryField("FAMOUNTDIGITS", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.MAX), new SummaryField("FNOIVAMOUNTFOR", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FNOIVAMOUNT", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FThisIVAmount", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FThisIVAmountFor", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FTHISNOIVAMOUNT", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM), new SummaryField("FTHISNOIVAMOUNTFOR", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM) };
        }





        private void BuilderReportSql(string tableName)
        {

            string addfieldSql = string.Format(@"/*dialect*/ alter table {0} add FNOIVAMOUNTFOR decimal(23,10) DEFAULT 0, FNOIVAMOUNT decimal(23,10) DEFAULT 0, FThisIVAmountFor decimal(23,10) DEFAULT 0, FThisIVAmount decimal(23,10) DEFAULT 0, FTHISNOIVAMOUNT decimal(23,10) DEFAULT 0, FTHISNOIVAMOUNTFOR decimal(23,10) DEFAULT 0, FTempNOIVAMOUNT decimal(23,10) DEFAULT 0, FtempNOIVAMOUNTFOR decimal(23,10) DEFAULT 0, F_JNIVNUMBER varchar(250)  DEFAULT ''", tableName);
             DBUtils.Execute(this.Context, addfieldSql);

             string sql = string.Format(@"/*dialect*/ update {0} set FNOIVAMOUNTFOR=FAmountFor-FHADIVAMOUNTFOR,
FNOIVAMOUNT=FAmount-FHADIVAMOUNT, FThisIVAmountFor=0,FThisIVAmount=0,FTempNOIVAMOUNTFOR= FAmountFor,
FTempNOIVAMOUNT=FAmount ", tableName);
             DBUtils.Execute(this.Context,sql);

             sql = string.Format(@"/*dialect*/ update {0}  set FThisIVAmountFor= t3.FIVALLAMOUNTFOR,
FThisIVAmount=t3.FIVALLAMOUNTFOR from
{1} as t1,
(select tb2.FBILLNO,sum(FIVALLAMOUNTFOR) as FIVALLAMOUNTFOR from T_AP_PAYABLEENTRY tb1 join T_AP_PAYABLE tb2 on tb1.FID=tb2.FID and  tb2.FBILLNO <>'' 
group by tb2.FBILLNO) as t3
where t3.FBILLNO=t1.FBILLNO and t1.FDATE<'2016/1/1 0:00:00' and t1.FBILLNO like 'AP%'", tableName, tableName);
             DBUtils.Execute(this.Context, sql);

             sql = string.Format(@"/*dialect*/ update {0}  set FThisIVAmountFor= t3.FIVALLAMOUNTFOR,
FThisIVAmount=t3.FIVALLAMOUNT from
{1} as t1,
(select tb1.FSRCBILLNO, SUM(tb1.FALLAMOUNTFOR) as FIVALLAMOUNTFOR,SUM(tb2.FAllAMOUNT)as FIVALLAMOUNT from T_IV_PURCHASEICENTRY tb1 join T_IV_PURCHASEICENTRY_O tb2  on tb1.FENTRYID=tb2.FENTRYID 
join T_IV_PURCHASEIC tb3 on tb3.FID=tb1.FID where tb3.FDOCUMENTSTATUS='C' and tb3.FDATE>='{2}' and tb3.FDATE<='{3}' and  tb1.FSRCBILLNO <>'' group by tb1.FSRCBILLNO) as t3
where t3.FSRCBILLNO=t1.FBILLNO ", tableName, tableName,startday, endday);
             DBUtils.Execute(this.Context, sql);

             if (startday == "1900/1/1 0:00:00")
             {
                 sql = string.Format(@"/*dialect*/ update {0}  set FTempNOIVAMOUNTFOR= FAmountFor,
FTempNOIVAMOUNT=FAmount ", tableName);
             }
             else
             {
                 sql = string.Format(@"/*dialect*/ update {0}  set FTempNOIVAMOUNTFOR= FAmountFor-t3.FIVALLAMOUNTFOR,
FTempNOIVAMOUNT=FAmount-t3.FIVALLAMOUNT from
{1} as t1,
(select tb1.FSRCBILLNO, SUM(tb1.FALLAMOUNTFOR) as FIVALLAMOUNTFOR,SUM(tb2.FAllAMOUNT)as FIVALLAMOUNT from T_IV_PURCHASEICENTRY tb1 join T_IV_PURCHASEICENTRY_O tb2  on tb1.FENTRYID=tb2.FENTRYID 
join T_IV_PURCHASEIC tb3 on tb3.FID=tb1.FID where tb3.FDOCUMENTSTATUS='C' and tb3.FDATE<'{2}' and  tb1.FSRCBILLNO <>'' group by tb1.FSRCBILLNO) as t3
where t3.FSRCBILLNO=t1.FBILLNO", tableName, tableName, startday);

             }

             DBUtils.Execute(this.Context, sql);

             sql = string.Format(@"/*dialect*/ update {0} set FTHISNOIVAMOUNTFOR=FTempNOIVAMOUNTFOR-FThisIVAmountFor,
FTHISNOIVAMOUNT=FTempNOIVAMOUNTFOR-FThisIVAmount", tableName);
             DBUtils.Execute(this.Context, sql);

             sql = string.Format(@"/*dialect*/ update {0}  set F_JNIVNUMBER= t3.FINVOICENO  from
{1} as t1,
(select t2.fbillno,[FINVOICENO]=stuff((select ','+[FINVOICENO] from (select distinct FSRCBILLNO,FINVOICENO  from T_IV_PURCHASEICENTRY tb1 
join T_IV_PURCHASEIC tb2 on tb1.FID=tb2.FID 
	where tb2.FDOCUMENTSTATUS='C' and tb2.FDATE>='{2}' and tb2.FDATE<='{3}' and  tb1.FSRCBILLNO <>'') t1  
     where t2.fbillno in (t1.FSRCBILLNO) 
     for xml path('')), 1, 1, '') from  T_AP_PAYABLE t2 group by t2.fbillno ) as t3
where t3.fbillno=t1.FBILLNO", tableName, tableName, startday, endday);
             DBUtils.Execute(this.Context, sql);
            
        }



    }
}