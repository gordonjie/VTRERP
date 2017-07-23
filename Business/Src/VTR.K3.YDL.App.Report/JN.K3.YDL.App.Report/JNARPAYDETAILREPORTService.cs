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
using Kingdee.K3.FIN.AR.App.Report;
using Kingdee.BOS.Util;
using Kingdee.BOS.Contracts;
using Kingdee.K3.FIN.App.Core.ARAP.AbstractReport;
using Kingdee.K3.FIN.App.Core;
using Kingdee.K3.FIN.Core;
using Kingdee.BOS.Orm;


namespace VTR.K3.YDL.App.Report
{
    [Description("扩展-应收款明细表服务端插件")]




    public class JNARPAYDETAILREPORTService : ARDetailReportService
    {
        private List<SqlObject> sqlList;
        private string startday;
        private string endday;

        protected override void BuildData(string tableName)
        {
            /*2018年1月28日赵成杰增加已开发票，未开发票
             base.BuildData(tableName);
             this.BuilderReportSql(tableName);
             * */
            this.sqlList = new List<SqlObject>();

            if (base.FilterCondition.ViewFromSumReport)
            {
                //base.BuildData(tableName);
                this.sqlList.AddRange(base.InsertTmpData_FromSumRpt());

            }
            else
            {
                startday = Convert.ToDateTime(base.FilterCondition.StartDate).ToString("yyyy-MM-dd");
                endday = Convert.ToDateTime(base.FilterCondition.EndDate).ToString("yyyy-MM-dd");

                this.sqlList.Add(base.InsertTmpData_AR_OrgEndDate());
                this.sqlList.Add(base.InsertTmpData_AR_ReceivableBill(BusinessTYPEENUM.BeginBalance));
                this.sqlList.Add(base.InsertTmpData_AR_OtherRecAbleBill(BusinessTYPEENUM.BeginBalance));
                this.sqlList.Add(base.InsertTmpData_AR_ReceiveBill(BusinessTYPEENUM.BeginBalance));
                //this.sqlList.Add(this.VTRInsertTmpData_AP_PayableBill(BusinessTYPEENUM.BeginBalance));
                this.sqlList.Add(base.InsertTmpData_AR_RefundBill(BusinessTYPEENUM.BeginBalance));
                this.sqlList.Add(base.InsertTmpData_AP_Match(BusinessTYPEENUM.BeginBalance));
                this.sqlList.Add(base.InsertTmpData_AR_Match(BusinessTYPEENUM.BeginBalance));
                this.sqlList.Add(base.InsertTmpData_AR_InnerRecClear(BusinessTYPEENUM.BeginBalance));
                this.sqlList.Add(base.InsertTmpData_AR_ContactBal(BusinessTYPEENUM.BeginBalance));
                this.sqlList.Add(base.InsertTmpData_AR_ReceivableBill(BusinessTYPEENUM.RPAmount));
                this.sqlList.Add(base.InsertTmpData_AR_OtherRecAbleBill(BusinessTYPEENUM.RPAmount));
                this.sqlList.Add(base.InsertTmpData_AR_ReceiveBill(BusinessTYPEENUM.Amount));
                this.sqlList.Add(base.InsertTmpData_AR_RefundBill(BusinessTYPEENUM.Amount));
                this.sqlList.Add(base.InsertTmpData_AP_Match(BusinessTYPEENUM.ReversedAmount));
                this.sqlList.Add(base.InsertTmpData_AR_Match(BusinessTYPEENUM.ReversedAmount));
                this.sqlList.Add(base.InsertTmpData_AR_InnerRecClear(BusinessTYPEENUM.ReversedAmount));
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
            string addfieldSql = string.Format(@"/*dialect*/ alter table {0} add FNOIVAMOUNTFOR decimal(23,10) DEFAULT 0, FNOIVAMOUNT decimal(23,10) DEFAULT 0, FThisIVAmountFor decimal(23,10) DEFAULT 0, FThisIVAmount decimal(23,10) DEFAULT 0, FTHISNOIVAMOUNT decimal(23,10) DEFAULT 0, FTHISNOIVAMOUNTFOR decimal(23,10) DEFAULT 0, FTempNOIVAMOUNT decimal(23,10) DEFAULT 0, FtempNOIVAMOUNTFOR decimal(23,10) DEFAULT 0, F_JN_Note varchar(250)  DEFAULT ''", tableName);
            DBUtils.Execute(this.Context, addfieldSql);

            string sql = string.Format(@"/*dialect*/ update {0} set FNOIVAMOUNTFOR=FAmountFor-FHADIVAMOUNTFOR,
FNOIVAMOUNT=FAmount-FHADIVAMOUNT, FThisIVAmountFor=0,FThisIVAmount=0,FTempNOIVAMOUNTFOR= FAmountFor,
FTempNOIVAMOUNT=FAmount ", tableName);
            DBUtils.Execute(this.Context, sql);

            sql = string.Format(@"/*dialect*/ update {0}  set FThisIVAmountFor= t3.FIVALLAMOUNTFOR,
FThisIVAmount=t3.FIVALLAMOUNTFOR from
{1} as t1,
(select tb2.FBILLNO,sum(FIVALLAMOUNTFOR) as FIVALLAMOUNTFOR from T_AR_ReceivableENTRY tb1 join T_AR_Receivable tb2 on tb1.FID=tb2.FID and  tb2.FBILLNO <>'' 
group by tb2.FBILLNO) as t3
where t3.FBILLNO=t1.FBILLNO and t1.FDATE<'2016/1/1 0:00:00' and t1.FBILLNO like 'AR%'", tableName, tableName);
            DBUtils.Execute(this.Context, sql);

            sql = string.Format(@"/*dialect*/ update {0}  set FThisIVAmountFor= t3.FIVALLAMOUNTFOR,
FThisIVAmount=t3.FIVALLAMOUNT from
{1} as t1,
(select tb1.FSRCBILLNO, SUM(tb1.FALLAMOUNTFOR) as FIVALLAMOUNTFOR,SUM(tb2.FAllAMOUNT)as FIVALLAMOUNT from T_IV_SALESICENTRY tb1 join T_IV_SALESICENTRY_O tb2  on tb1.FENTRYID=tb2.FENTRYID 
join T_IV_SALESIC tb3 on tb3.FID=tb1.FID where tb3.FDOCUMENTSTATUS='C' and tb3.FDATE>='{2}' and tb3.FDATE<='{3}' and  tb1.FSRCBILLNO <>'' group by tb1.FSRCBILLNO) as t3
where t3.FSRCBILLNO=t1.FBILLNO ", tableName, tableName, startday, endday);
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
(select tb1.FSRCBILLNO, SUM(tb1.FALLAMOUNTFOR) as FIVALLAMOUNTFOR,SUM(tb2.FAllAMOUNT)as FIVALLAMOUNT from T_IV_SALESICENTRY tb1 join T_IV_SALESICENTRY_O tb2  on tb1.FENTRYID=tb2.FENTRYID 
join T_IV_SALESIC tb3 on tb3.FID=tb1.FID where tb3.FDOCUMENTSTATUS='C' and tb3.FDATE<'{2}' and  tb1.FSRCBILLNO <>'' group by tb1.FSRCBILLNO) as t3
where t3.FSRCBILLNO=t1.FBILLNO", tableName, tableName, startday);

            }

            DBUtils.Execute(this.Context, sql);

            sql = string.Format(@"/*dialect*/ update {0} set FTHISNOIVAMOUNTFOR=FTempNOIVAMOUNTFOR-FThisIVAmountFor,
FTHISNOIVAMOUNT=FTempNOIVAMOUNTFOR-FThisIVAmount", tableName);
            DBUtils.Execute(this.Context, sql);

            sql = string.Format(@"/*dialect*/ update {0}  set F_JN_Note= t3.FINVOICENO  from
{1} as t1,
(select t2.fbillno,[FINVOICENO]=stuff((select ','+[FINVOICENO] from (select distinct FSRCBILLNO,FINVOICENO  from T_IV_SALESICENTRY tb1 
join T_IV_SALESIC tb2 on tb1.FID=tb2.FID 
	where tb2.FDOCUMENTSTATUS='C' and tb2.FDATE>='{2}' and tb2.FDATE<='{3}' and  tb1.FSRCBILLNO <>'') t1  
     where t2.fbillno in (t1.FSRCBILLNO) 
     for xml path('')), 1, 1, '') from  T_AR_Receivable t2 group by t2.fbillno ) as t3
where t3.fbillno=t1.FBILLNO", tableName, tableName, startday, endday);
            DBUtils.Execute(this.Context, sql);

            /*2018年1月28日停赵成杰
            string addfieldSql = string.Format(@" alter table {0} add F_JN_Note varchar(250)  DEFAULT ''", tableName);
            DBUtils.Execute(this.Context, addfieldSql);
            

            string sql = string.Format(@" update {0} set F_JN_Note=''", tableName);
            DBUtils.Execute(this.Context, sql);
            

            sql = string.Format(@" update {0}  set F_JN_Note= table3.IVBillno  from
{1} as table1,
(select TB1.FID,TB1.IVBillno,TB2.FSRCBILLNO from 
(select FID,IVBillno=STUFF((select ','+FSRCBILLNO from T_AR_BillingMatchLogENTRY as T2
 where T1.FID=T2.FID and (FSOURCETYPE='1cab58bc33d24e27826be02249f4edac' or FSOURCETYPE='50ea4e69b6144f69961d2e9b44820929')
 FOR XML PATH('')), 1, 1, '')
from T_AR_BillingMatchLogENTRY  as T1
where (FSOURCETYPE='1cab58bc33d24e27826be02249f4edac' or FSOURCETYPE='50ea4e69b6144f69961d2e9b44820929')
group by FID ) TB1
join (select FID,FSRCBILLNO from T_AR_BillingMatchLogENTRY where FSOURCETYPE<>'50ea4e69b6144f69961d2e9b44820929' and FSOURCETYPE<>'1cab58bc33d24e27826be02249f4edac' and FSRCBILLNO not like 'ART%' ) TB2
on TB1.FID=TB2.FID) as table3
where table3.FSRCBILLNO=table1.FBILLNO", tableName, tableName);
            DBUtils.Execute(this.Context, sql);

            sql = string.Format(@"update {0}  set F_JN_Note= table3.IVBillno  from
{1} as table1,
(select TB1.FID,TB1.IVBillno,TB2.FSRCBILLNO from 
(select FID,IVBillno=STUFF((select ','+FSRCBILLNO from T_AR_BillingMatchLogENTRY as T2
 where T1.FID=T2.FID and (FSOURCETYPE='1cab58bc33d24e27826be02249f4edac' or FSOURCETYPE='50ea4e69b6144f69961d2e9b44820929'or FSOURCETYPE='180ecd4afd5d44b5be78a6efe4a7e041')
 FOR XML PATH('')), 1, 1, '')
from T_AR_BillingMatchLogENTRY  as T1
where (FSOURCETYPE='1cab58bc33d24e27826be02249f4edac' or FSOURCETYPE='50ea4e69b6144f69961d2e9b44820929'or FSOURCETYPE='180ecd4afd5d44b5be78a6efe4a7e041')
group by FID ) TB1
join (select FID,FSRCBILLNO from T_AR_BillingMatchLogENTRY where FSOURCETYPE<>'50ea4e69b6144f69961d2e9b44820929' and FSOURCETYPE<>'1cab58bc33d24e27826be02249f4edac' and FSRCBILLNO like 'ART%' ) TB2
on TB1.FID=TB2.FID) as table3
where table3.FSRCBILLNO=table1.FBILLNO", tableName, tableName);
            DBUtils.Execute(this.Context, sql);
            **/
        }



    }
}