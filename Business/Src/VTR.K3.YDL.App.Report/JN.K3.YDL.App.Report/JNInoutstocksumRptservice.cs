using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Report;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.K3.FIN.HS.App.Report;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.FIN.App.Core;
using Kingdee.K3.FIN.Core;
using Kingdee.K3.FIN.Core.Parameters;
using Kingdee.K3.FIN.Core.Object;
using System.Runtime.InteropServices;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Resource;


namespace VTR.K3.YDL.App.Report
{
    [Description("扩展-存货收发汇总表服务端插件")]
    public class JNInoutstocksumRptservice : SysReportBaseService
    {
        private string groupField;
        private bool chxExpense;
        private int dispalyDimType;
        private int currTable;
        private HSDim hsDim;
        private bool isDisplayPeriod;
        private bool chxNoStockAdj;
        private bool chxNoCostAllot;
        private int lCID;
        private string strTotal;
        private string strSubTotal;
 


        [StructLayout(LayoutKind.Sequential)]
        private struct HSDim
        {
            public long AcctSysId;
            public long AcctOrgId;
            public long AcctPolicyId;
            public int StartYear;
            public int StartPeriod;
            public int EndYear;
            public int EndPeriod;
        }





        public JNInoutstocksumRptservice()
{
    this.strTotal = ResManager.LoadKDString("总计", "003203000001747", Kingdee.BOS.Resource.SubSystemType.HR, new object[0]);
    this.strSubTotal = ResManager.LoadKDString("小计", "003203000001738",Kingdee.BOS.Resource.SubSystemType.HR, new object[0]);
}




        public override void Initialize()
        {
            base.Initialize();
            SetDecimalControl("FJNUnitEnzyme");
        }

        private void SetDecimalControl(string byDecimalControlFieldName)
        {
            DecimalControlField field = new DecimalControlField
            {
                ByDecimalControlFieldName = byDecimalControlFieldName,
                DecimalControlFieldName = "FSECUNITPRE"
            };
            base.ReportProperty.DecimalControlFieldList.Add(field);
        }

        public override Kingdee.BOS.Core.Report.ReportHeader GetReportHeaders(Kingdee.BOS.Core.Report.IRptParams filter)
        {
            ReportHeader reportHeader = base.GetReportHeaders(filter);
            reportHeader.AddChild("FJNUnitEnzyme", new LocaleValue("单位酶活量", base.Context.UserLocale.LCID));
            return reportHeader;
        }

        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            //base.BuilderReportSqlAndTempTable(filter, tableName);
            ReportCommonFunction.Filter(base.Context, filter, null);
            this.VTRSetGroupByColumnVisiable(filter);
            using (new SessionScope())
            {
                this.GetSummaryData(filter, tableName);
            }
        }


       //设置分组可见
        private void VTRSetGroupByColumnVisiable(IRptParams filter)
        {
            if (((filter != null) && (filter.FilterParameter != null)) && (((filter.FilterParameter.CustomFilter != null) && (filter.FilterParameter.ColumnInfo != null)) && (filter.FilterParameter.ColumnInfo.Count != 0)))
            {
                List<ColumnField> list = filter.FilterParameter.ColumnInfo;
                string str = filter.FilterParameter.CustomFilter["COMBOTotalType"].ToString();
                if (string.IsNullOrWhiteSpace(str))
                {
                    str = "0";
                }
                this.groupField = this.GetGroupByField(str, true);
                if ((from o in list
                     where o.Key.Equals(this.groupField, StringComparison.CurrentCultureIgnoreCase)
                     select o).FirstOrDefault<ColumnField>() == null)
                {
                    ColumnField item = new ColumnField();
                    item.Key=this.groupField;
                    item.ColIndex=0;
                    filter.FilterParameter.ColumnInfo.Insert(0, item);
                }
                this.groupField = this.GetGroupByField(str, false);
            }
        }





        //获取过滤分组信息
        private string GetGroupByField(string groupKey, bool isColumnName = true)
        {
            switch (groupKey)
            {
                case "0":
                    return "FMATERIALID";

                case "1":
                    return "FMATERPROPERTY";

                case "2":
                    return "FMATERTYPE";

                case "3":
                    return "FACCTGRANGEID";

                case "4":
                    return (isColumnName ? "FOWNERNAME" : "FOWNERID");

                case "5":
                    return (isColumnName ? "FSTOCKORGNAME" : "FSTOCKORGID");

                case "6":
                    return (isColumnName ? "FSTOCKNAME" : "FSTOCKID");
            }
            return " FMaterialID";
        }


        //总体获取数据
        private void GetSummaryData(IRptParams filter, string tableName)
        {
            string str = DBUtils.CreateSessionTemplateTable(base.Context, "TM_HS_StockInSummaryData", this.CreateRetTableSql());
            new StringBuilder();
            List<SqlObject> list = new List<SqlObject>();
            if (((filter.FilterParameter != null) && (filter.FilterParameter.CustomFilter != null)) && (Convert.ToInt64(filter.FilterParameter.CustomFilter["ACCTGSYSTEMID_Id"]) != 0))
            {
               // filter.get_FilterParameter();
                DynamicObject filterDyo = filter.FilterParameter.CustomFilter;
                this.InitPara(filter);
                this.CreateIndex(str);
                this.CreateMaterialInfo(str, filter);
                list.Add(new SqlObject(this.UpdateInitQty(str), new List<SqlParam>()));
                list.Add(new SqlObject(this.UpdateInitPriceAmount(str), new List<SqlParam>()));
                list.Add(new SqlObject(this.UpdateReceiveSendQty(str), new List<SqlParam>()));
                list.Add(new SqlObject(this.UpdateReceiveSendPriceAmount(str), new List<SqlParam>()));
                list.Add(new SqlObject(this.UpdateEndData(str), new List<SqlParam>()));
                if (this.chxExpense)
                {
                    list.Add(new SqlObject(this.InsertExpenSumData(str), new List<SqlParam>()));
                    list.Add(new SqlObject(this.UpdateExpenSumData(str), new List<SqlParam>()));
                    list.Add(new SqlObject(this.CleanExpQtyPrice(str), new List<SqlParam>()));
                }
                if (!base.ReportProperty.IsGroupSummary)
                {
                    list.Add(new SqlObject(this.InsertSumDataByField(str), new List<SqlParam>()));
                }
                list.Add(new SqlObject(this.CleanAllZeroRow(str), new List<SqlParam>()));
                FINDBUtils.ExecuteBatchNoTimeOut(base.Context, list);
                this.QueryTempTableData(tableName, str, filterDyo);
            }
            else
            {
                this.CreateTmpTableForNoInit(tableName, str);
            }
        }




        //获取过滤信息 GetSummaryData调用
        private void InitPara(IRptParams filterParams)
        {
            DynamicObject obj2 = filterParams.FilterParameter.CustomFilter;
            this.chxExpense = Convert.ToBoolean(obj2["CHXEXPENSE"]);
            this.chxNoStockAdj = Convert.ToBoolean(obj2["CHXNOSTOCKADJ"]);
            this.isDisplayPeriod = Convert.ToBoolean(obj2["IsDisplayPeriod"]);
            this.chxNoCostAllot = Convert.ToBoolean(obj2["CHXNOCOSTALLOT"]);
            this.dispalyDimType = Convert.ToInt32(obj2["FDimType"]);
            if ((filterParams.FilterParameter.SummaryRows != null) && (filterParams.FilterParameter.SummaryRows.Count > 0))
            {
                base.ReportProperty.IsGroupSummary=true;
            }
            else
            {
                base.ReportProperty.IsGroupSummary=false;
            }
            this.lCID = base.Context.UserLocale.LCID;
            this.currTable = -1;
            long num = BillExtension.GetValue<long>(obj2, "ACCTGSYSTEMID_ID");
            long num2 = BillExtension.GetValue<long>(obj2, "ACCTGORGID_ID");
            long num3 = BillExtension.GetValue<long>(obj2, "ACCTPOLICYID_ID");
            int num4 = BillExtension.GetValue<int>(obj2, "Year", 1);
            int num5 = BillExtension.GetValue<int>(obj2, "Period", 1);
            int num6 = BillExtension.GetValue<int>(obj2, "EndYear", 1);
            int num7 = BillExtension.GetValue<int>(obj2, "EndPeriod", 1);
            HSDim dim = new HSDim
            {
                AcctOrgId = num2,
                AcctPolicyId = num3,
                AcctSysId = num,
                StartYear = num4,
                StartPeriod = num5,
                EndYear = num6,
                EndPeriod = num7
            };
            this.hsDim = dim;
        }

        //增加数据表字段 GetSummaryData调用
        private string CreateRetTableSql()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(" (FDIMID int, FACCTGSYSTEMID int, FACCTGSYSTEMNAME nvarchar(255),FACCTGORGID int,FACCTGORGNAME nvarchar(255),FACCTPOLICYID int,FACCTPOLICYNAME nvarchar(255), FYear int,FPeriod int,");
            builder.Append(" FMATERIALBASEID int not null default 0,FMATERIALID nvarchar(255), FMATERIALNAME nvarchar(255),FMODEL nvarchar(510),FLOTNO varchar(255),FASSIPROPERTYID nvarchar(510),");
            builder.Append(" FMATERPROPERTY nvarchar(255), FMATERTYPE nvarchar(255),FBOMNO varchar(255),FPLANNO varchar(255), FSEQUENCENO varchar(255),FPROJECTNO varchar(255),FOWNERID int,FOWNERNAME nvarchar(255),");
            builder.Append(" FSTOCKORGID int ,FSTOCKORGNAME nvarchar(100),FSTOCKID int, FSTOCKNAME nvarchar(255) , FSTOCKPLACEID int,FSTOCKPLACENAME varchar(255),FACCTGRANGEID nvarchar(255) ,FACCTGRANGENAME nvarchar(255),");
            builder.Append(" FEXPENSEID nvarchar(255),FUNITNAME nvarchar(255),FVALUATION nvarchar(255),FINITQTY decimal(23,10),FINITPRICE decimal(23,10), FINITAMOUNT decimal(23,10),FRECEIVEQTY decimal(23,10), ");
            builder.Append(" FRECEIVEPRICE decimal(23,10),FRECEIVEAMOUNT decimal(23,10),FEXPENSENAME nvarchar(255),FSENDQTY decimal(23,10), FSENDPRICE decimal(23,10), FSENDAMOUNT decimal(23,10),FENDQTY decimal(23,10),");
            builder.Append(" FENDPRICE decimal(23,10),FENDAMOUNT decimal(23,10),FISTOTAL int,FASSIPROPNAME nvarchar(510),FSTOCKSTATUSID nvarchar(255),FGroupByField nvarchar(255),FDetailBillFormID nvarchar(255),FUnitID int )");
            return builder.ToString();
        }

        //增加索引 GetSummaryData调用
        private void CreateIndex(string tableName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" create clustered index {0} ON {1} ", CommonFunction.GetTempTableIndexName(tableName), tableName);
            builder.AppendFormat(" ( FDIMID {0},FYEAR,FPERIOD  )", this.chxExpense ? ",FEXPENSEID" : "");
            DBUtils.CreateSessionTemplateTableIndex(base.Context, builder.ToString());
        }


        //增加物料信息
        private void CreateMaterialInfo(string tableName, IRptParams filter)
        {
            long num = CommonFunction.GetDimensionIDByAcctPolicy(base.Context, this.hsDim.AcctSysId, this.hsDim.AcctOrgId, this.hsDim.AcctPolicyId);
            string str = (this.dispalyDimType == 0) ? "HSDIM.FMASTERID" : "STOCKDIM.FMATERIALID";
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" insert /*+append*/ into {0} ", tableName);
            builder.AppendFormat(" (FDIMID ,FACCTGSYSTEMID, FACCTGORGID,FACCTPOLICYID, FYear,FPeriod,FMATERIALBASEID,FMATERIALID,FMATERIALNAME, FMODEL, FLOTNO,", new object[0]);
            builder.AppendFormat("  FASSIPROPERTYID,FMATERPROPERTY,FMATERTYPE,FBOMNO,FPLANNO,FSEQUENCENO,FPROJECTNO,FOWNERID,FSTOCKORGID,", new object[0]);
            builder.AppendFormat("  FSTOCKSTATUSID,FSTOCKID,FSTOCKPLACEID,FACCTGRANGEID,FUNITID,FVALUATION,FDetailBillFormID,FISTOTAL {0}) ", this.GetExpenField(""));
            builder.AppendFormat(this.GetFieldByDisPlayDimType(false), new object[0]);
            builder.AppendFormat(" from {0} AG ", "T_HS_OUTACCTG");
            builder.AppendFormat(" inner join {0} IV ", this.GetTable("T_HS_InivBalance", "T_HS_InivBalance_H", "V_HS_InivBalance"));
            builder.AppendFormat("  on AG.FID = IV.FID ", new object[0]);
            builder.AppendFormat(" inner join {0} CALD ", "T_HS_CALDIMENSIONS");
            builder.AppendFormat("  on AG.FDIMENSIONID = CALD.FDIMENSIONID and AG.FDIMENSIONID = {0} ", num);
            if (this.dispalyDimType == 1)
            {
                builder.AppendFormat(" inner join {0} STOCKDIM ", "T_HS_InivStockDimension");
                builder.AppendFormat("  on IV.FDimeEntryId = STOCKDIM.FENTRYID ", new object[0]);
                builder.AppendFormat(" left join {0} LOT on STOCKDIM.FLOT = LOT.FLOTID ", "T_BD_LOTMASTER");
            }
            builder.AppendFormat(" inner join {0} HSDIM ", "T_HS_StockDimension");
            builder.AppendFormat("  on HSDIM.FENTRYID=IV.FACCTGDIMEENTRYID ", new object[0]);
            if (this.chxExpense)
            {
                builder.AppendFormat(" left join {0} IEXP ", this.GetTable("T_HS_InivBalanceExp", "T_HS_InivBalanceExp_H", "V_HS_InivBalanceExp"));
                builder.AppendFormat(" on IV.FENTRYID = IEXP.FENTRYID ", new object[0]);
                builder.AppendFormat(" left join {0} EXP ", "T_BD_EXPENSE");
                builder.AppendFormat(" on IEXP.FEXPENSESITEMID = EXP.FEXPID ", new object[0]);
                builder.AppendFormat(" left join {0} EXP_L ", "T_BD_EXPENSE_L");
                builder.AppendFormat(" on IEXP.FEXPENSESITEMID = EXP_L.FEXPID ", new object[0]);
                builder.AppendFormat(" and EXP_L.FLOCALEID = {0} ", this.lCID);
            }
            builder.AppendFormat(" inner join {0} MAT ", "t_bd_Material");
            builder.AppendFormat(" on {0} = MAT.FMATERIALID ", str);
            builder.AppendFormat(" inner join {0} MAT_T ", "T_BD_MATERIALBASE");
            builder.AppendFormat(" on MAT.FMATERIALID = MAT_T.FMATERIALID ", new object[0]);
            builder.AppendFormat(" left join {0} STOCK on STOCK.FSTOCKID={1}.FSTOCKID ", "t_BD_Stock", (this.dispalyDimType == 0) ? "HSDIM" : "STOCKDIM");
            builder.AppendFormat(" left join {0} MAT_L ", "t_bd_Material_l");
            builder.AppendFormat(" on MAT.FMATERIALID = MAT_L.FMATERIALID ", new object[0]);
            builder.AppendFormat(" and MAT_L.FLOCALEID = {0} ", this.lCID);
            builder.AppendFormat(" where  IV.FENDINITKEY = '0'", new object[0]);
            if (!this.isDisplayPeriod)
            {
                builder.AppendFormat(" and AG.FYEAR = {0} and AG.FPERIOD = {1}", this.hsDim.StartYear, this.hsDim.StartPeriod);
            }
            builder.AppendFormat(this.BindFilterSQL(filter, false), new object[0]);
            builder.AppendLine(" union ");
            builder.AppendFormat(this.GetFieldByDisPlayDimType(false), new object[0]);
            builder.AppendFormat(" from {0} AG ", "T_HS_OUTACCTG");
            builder.AppendFormat(" inner join {0} CALD ", "T_HS_CALDIMENSIONS");
            builder.AppendFormat("  on AG.FDIMENSIONID = CALD.FDIMENSIONID ", new object[0]);
            builder.AppendFormat("  and AG.FDIMENSIONID = {0} ", num);
            builder.AppendFormat(" inner join {0} SEQ ", this.GetTable("T_HS_OutInStockSeq", "T_HS_OutInStockSeq_H", "V_HS_OutInStockSeq"));
            builder.AppendFormat("  on AG.FID = SEQ.FACCTGID and SEQ.FDOCUMENTSTATUS = '{0}' ", (DocumentStatus)3);
            if (this.dispalyDimType == 1)
            {
                builder.AppendFormat(" inner join {0} STOCKDIM ", "T_HS_InivStockDimension");
                builder.AppendFormat("  on SEQ.FDIMEENTRYID = STOCKDIM.FENTRYID ", new object[0]);
                builder.AppendFormat(" left join {0} LOT on STOCKDIM.FLOT = LOT.FLOTID ", "T_BD_LOTMASTER");
            }
            builder.AppendFormat(" inner join {0} HSDIM ", "T_HS_StockDimension");
            builder.AppendFormat("  on HSDIM.FENTRYID=SEQ.FACCTGDIMEENTRYID ", new object[0]);
            if (this.chxExpense)
            {
                builder.AppendFormat("  left join {0} T4 ", this.GetTable("T_HS_Expenses", "T_HS_Expenses_H", "V_HS_Expenses"));
                builder.AppendFormat("  on SEQ.FENTRYID = T4.FSEQENTRYID ", new object[0]);
                builder.AppendFormat("  left join {0} EXP ", "T_BD_EXPENSE");
                builder.AppendFormat("  on T4.FEXPENSESITEMID = EXP.FEXPID ", new object[0]);
                builder.AppendFormat("  left join {0} EXP_L ", "T_BD_EXPENSE_L");
                builder.AppendFormat("  on T4.FEXPENSESITEMID = EXP_L.FEXPID and EXP_L.FLOCALEID = {0} ", this.lCID);
            }
            builder.AppendFormat("  inner join {0} MAT ", "t_bd_Material");
            builder.AppendFormat("  on {0} = MAT.FMATERIALID ", str);
            builder.AppendFormat("  inner join {0} MAT_T ", "T_BD_MATERIALBASE");
            builder.AppendFormat("  on MAT.FMATERIALID = MAT_T.FMATERIALID ", new object[0]);
            builder.AppendFormat("  left join {0} STOCK on STOCK.FSTOCKID={1}.FSTOCKID ", "t_BD_Stock", (this.dispalyDimType == 0) ? "HSDIM" : "STOCKDIM");
            builder.AppendFormat("  left join {0} MAT_L ", "t_bd_Material_l");
            builder.AppendFormat("  on MAT.FMATERIALID = MAT_L.FMATERIALID and MAT_L.FLOCALEID = {0} ", this.lCID);
            builder.AppendFormat("  where 1=1 {0} ", this.GetNoStatistical("SEQ"));
            builder.AppendFormat(this.BindFilterSQL(filter, false), new object[0]);
            builder.AppendLine(" union ");
            builder.AppendFormat(this.GetFieldByDisPlayDimType(true), new object[0]);
            builder.AppendFormat(" from {0} CALD ", "T_HS_AdjustmentBill");
            builder.AppendFormat("  inner join {0} ADJENTRY on CALD.FID = ADJENTRY.FID ", "T_HS_AdjustmentBillEntry");
            if (this.dispalyDimType == 1)
            {
                builder.AppendFormat(" inner join {0} STOCKDIM ", "T_HS_InivStockDimension");
                builder.AppendFormat("  on ADJENTRY.FDIMEENTRYID = STOCKDIM.FENTRYID ", new object[0]);
                builder.AppendFormat(" left join {0} LOT on STOCKDIM.FLOT = LOT.FLOTID ", "T_BD_LOTMASTER");
            }
            builder.AppendFormat(" inner join {0} HSDIM ", "T_HS_StockDimension");
            builder.AppendFormat("  on HSDIM.FENTRYID=ADJENTRY.FACCTGDIMEENTRYID ", new object[0]);
            builder.AppendFormat(" inner join {0} MAT on {1} = MAT.FMATERIALID ", "t_bd_Material", str);
            builder.AppendFormat(" inner join {0} MAT_T on MAT.FMATERIALID = MAT_T.FMATERIALID  ", "T_BD_MATERIALBASE");
            builder.AppendFormat(" inner join {0} AG  on CALD.FDATE between AG.FPERIODSTARTDATE and AG.FPERIODENDDATE ", "T_BD_ACCOUNTPERIOD");
            builder.AppendFormat(" left join {0} STOCK on STOCK.FSTOCKID={1}.FSTOCKID ", "t_BD_Stock", (this.dispalyDimType == 0) ? "HSDIM" : "STOCKDIM");
            builder.AppendFormat(" left join {0} MAT_L on ( MAT.FMATERIALID = MAT_L.FMATERIALID ", "t_bd_Material_l");
            builder.AppendFormat("  and MAT_L.FLOCALEID = {0} ) ", this.lCID);
            if (this.chxExpense)
            {
                builder.AppendFormat("  inner join T_HS_ADJUSTEXPENSEENTRY ADJEX ", new object[0]);
                builder.AppendFormat("  on ADJEX.FENTRYID = ADJENTRY.FENTRYID ", new object[0]);
                builder.AppendFormat("  left join {0} EXP ", "T_BD_EXPENSE");
                builder.AppendFormat("  on ADJEX.FEXPENSEID = EXP.FEXPID ", new object[0]);
                builder.AppendFormat("  left join {0} EXP_L ", "T_BD_EXPENSE_L");
                builder.AppendFormat("  on ADJEX.FEXPENSEID = EXP_L.FEXPID ", new object[0]);
                builder.AppendFormat("  and EXP_L.FLOCALEID = {0} ", this.lCID);
            }
            builder.AppendFormat(" where ( CALD.FBUSINESSTYPE = '0' or CALD.FISACCTGGENERATE = '1') ", new object[0]);
            builder.AppendFormat("  and CALD.FDOCUMENTSTATUS = '{0}' ", (DocumentStatus)3);
            builder.AppendFormat("  and CALD.FFORBIDSTATUS = '{0}'  ", (ForbidStatus)0);
            builder.AppendFormat(this.BindFilterSQL(filter, true), new object[0]);
            FINDBUtils.ExecuteWithTime(base.Context, builder.ToString(), null);
            CommonFunction.GetAnalyzeTableStat(base.Context, tableName);
        }

        //更新期初数量 GetSummaryData调用
        private string UpdateInitQty(string tableName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" merge into {0} T using ( ", tableName);
            builder.AppendFormat("  select DIM.FENTRYID FDIMEENTRYID,Sum(T1.FQty) FINITQTY,T0.FYEAR,T0.FPERIOD ", new object[0]);
            builder.AppendFormat("  from  {0} T0 ", "T_HS_OUTACCTG");
            builder.AppendFormat("  inner join {0} T1 ", this.GetTable("T_HS_InivBalance", "T_HS_InivBalance_H", "V_HS_InivBalance"));
            builder.AppendFormat("  on T0.FID = T1.FID ", new object[0]);
            builder.AppendFormat("  inner join {0} CALD ", "T_HS_CALDIMENSIONS");
            builder.AppendFormat("  on T0.FDIMENSIONID = CALD.FDIMENSIONID ", new object[0]);
            builder.AppendFormat(this.JoinDimTable("T1"), new object[0]);
            builder.AppendFormat("  where T1.FENDINITKEY='0' and CALD.FACCTSYSTEMID={0} ", this.hsDim.AcctSysId);
            builder.AppendFormat("  and CALD.FFINORGID={0} and CALD.FACCTPOLICYID={1}", this.hsDim.AcctOrgId, this.hsDim.AcctPolicyId);
            if (this.isDisplayPeriod)
            {
                builder.AppendFormat(this.GetYearPeriodFilter("T0"), new object[0]);
            }
            else
            {
                builder.AppendFormat(" and T0.FYEAR = {0} and T0.FPERIOD = {1} ", this.hsDim.StartYear, this.hsDim.StartPeriod);
            }
            builder.AppendFormat(" group by DIM.FENTRYID,T0.FYEAR,T0.FPERIOD ) RET ", new object[0]);
            builder.AppendFormat(" on ( T.FDIMID = RET.FDimeEntryId and T.FYEAR = RET.fyear and T.FPERIOD = RET.fperiod )", new object[0]);
            builder.AppendFormat(" when matched then update set T.FINITQTY = RET.FINITQTY ", new object[0]);
            return builder.ToString();
        }

        //更新期初价格和金额  GetSummaryData调用
        private string UpdateInitPriceAmount(string tableName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" merge into {0} t using ( ", tableName);
            if (this.chxExpense)
            {
                builder.AppendFormat("  select DIM.FENTRYID FDIMEENTRYID,isnull(exp.FNUMBER,N' ') as FEXPENSEID,", new object[0]);
                builder.AppendFormat("  Sum(T2.FEXPENSESAMOUNT) FINITAMOUNT,T0.FYEAR,T0.FPERIOD ", new object[0]);
                builder.AppendFormat("  from {0} T0 ", "T_HS_OUTACCTG");
                builder.AppendFormat("  inner join {0} T1 ", this.GetTable("T_HS_InivBalance", "T_HS_InivBalance_H", "V_HS_InivBalance"));
                builder.AppendFormat("  on T0.FID = T1.FID ", new object[0]);
                builder.AppendFormat("  inner join {0} CALD ", "T_HS_CALDIMENSIONS");
                builder.AppendFormat("  on T0.FDIMENSIONID = CALD.FDIMENSIONID ", new object[0]);
                builder.AppendFormat(this.JoinDimTable("T1"), new object[0]);
                builder.AppendFormat("  left join {0} t2 ", this.GetTable("T_HS_InivBalanceExp", "T_HS_InivBalanceExp_H", "V_HS_InivBalanceExp"));
                builder.AppendFormat("   on t1.FEntryId=t2.FEntryId ", new object[0]);
                builder.AppendFormat("  left join {0} exp on t2.FExpensesItemId = exp.FEXPID  ", "T_BD_EXPENSE");
                builder.AppendFormat("    where  T1.FENDINITKEY  = '0' ", new object[0]);
                builder.AppendFormat("     and CALD.FACCTSYSTEMID = {0} and CALD.FFINORGID = {1}  ", this.hsDim.AcctSysId, this.hsDim.AcctOrgId);
                builder.AppendFormat("     and   CALD.FACCTPOLICYID = {0} ", this.hsDim.AcctPolicyId);
                if (this.isDisplayPeriod)
                {
                    builder.AppendFormat(this.GetYearPeriodFilter("T0"), new object[0]);
                }
                else
                {
                    builder.AppendFormat(" and T0.FYEAR = {0} and T0.FPERIOD = {1} ", this.hsDim.StartYear, this.hsDim.StartPeriod);
                }
                builder.AppendFormat("  group by DIM.FENTRYID,EXP.FNUMBER,T0.FYEAR,T0.FPERIOD) RET  ", new object[0]);
                builder.AppendFormat(" on ( T.FDIMID = RET.FDimeEntryId ", new object[0]);
                builder.AppendFormat("     and t.FEXPENSEID =RET.FEXPENSEID ", new object[0]);
                builder.AppendFormat("     and T.FYEAR = RET.fyear ", new object[0]);
                builder.AppendFormat("     and T.FPERIOD = RET.fperiod ) ", new object[0]);
            }
            else
            {
                builder.AppendFormat("  select DIM.FENTRYID FDIMEENTRYID, ", new object[0]);
                builder.AppendFormat("   Sum(t1.FAMOUNT) FINITAMOUNT, T0.FYEAR,T0.FPERIOD ", new object[0]);
                builder.AppendFormat("   from {0} T0 ", "T_HS_OUTACCTG");
                builder.AppendFormat("  inner join {0} T1 ", this.GetTable("T_HS_InivBalance", "T_HS_InivBalance_H", "V_HS_InivBalance"));
                builder.AppendFormat("    on T0.FID = T1.FID ", new object[0]);
                builder.AppendFormat("   inner join {0} CALD ", "T_HS_CALDIMENSIONS");
                builder.AppendFormat("   on T0.FDIMENSIONID = CALD.FDIMENSIONID ", new object[0]);
                builder.AppendFormat(this.JoinDimTable("T1"), new object[0]);
                builder.AppendFormat("    where  T1.FENDINITKEY  = '0' ", new object[0]);
                builder.AppendFormat("     and CALD.FACCTSYSTEMID = {0} and CALD.FFINORGID = {1}  ", this.hsDim.AcctSysId, this.hsDim.AcctOrgId);
                builder.AppendFormat("     and   CALD.FACCTPOLICYID = {0} ", this.hsDim.AcctPolicyId);
                if (this.isDisplayPeriod)
                {
                    builder.AppendFormat(this.GetYearPeriodFilter("T0"), new object[0]);
                }
                else
                {
                    builder.AppendFormat(" and T0.FYEAR = {0} and T0.FPERIOD = {1} ", this.hsDim.StartYear, this.hsDim.StartPeriod);
                }
                builder.AppendFormat(" group by DIM.FENTRYID,T0.FYEAR,T0.FPERIOD ) RET ", new object[0]);
                builder.AppendFormat(" on ( T.FDIMID = RET.FDimeEntryId and T.FYEAR = RET.fyear ", new object[0]);
                builder.AppendFormat("   and T.FPERIOD = RET.fperiod ) ", new object[0]);
            }
            builder.AppendFormat("when matched then update set ", new object[0]);
            builder.AppendFormat("   T.FINITPRICE = case T.FINITQTY when  0 then 0 ", new object[0]);
            builder.AppendFormat("   else Round(RET.FINITAMOUNT / T.FINITQTY, 10) end, ", new object[0]);
            builder.AppendFormat("   T.FINITAMOUNT = RET.FINITAMOUNT ", new object[0]);
            return builder.ToString();
        }

        //更新收入发出数量  GetSummaryData调用
        private string UpdateReceiveSendQty(string tableName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" merge into {0} T ", tableName);
            builder.AppendFormat("using (select T0.FYEAR,T0.FPERIOD,DIM.FENTRYID FDIMEENTRYID,", new object[0]);
            builder.AppendFormat("   Sum(case when T1.FINOUTINDEX = '1' then T1.FQTY else 0 end) as FRECEIVEQTY,", new object[0]);
            builder.AppendFormat("   Sum(case when T1.FINOUTINDEX = '0' then T1.FQTY else 0 end) as FSENDQTY  ", new object[0]);
            builder.AppendFormat("    from  {0} T0 ", "T_HS_OUTACCTG");
            builder.AppendFormat("    inner join {0} CALD ", "T_HS_CALDIMENSIONS");
            builder.AppendFormat("      on T0.FDIMENSIONID = CALD.FDIMENSIONID ", new object[0]);
            builder.AppendFormat("    inner join {0} T1 ", this.GetTable("T_HS_OutInStockSeq", "T_HS_OutInStockSeq_H", "V_HS_OutInStockSeq"));
            builder.AppendFormat("      on T0.FID =T1.FACCTGID and T1.FDOCUMENTSTATUS = '{0}' ", (DocumentStatus)3);
            builder.AppendFormat(this.JoinDimTable("T1"), new object[0]);
            builder.AppendFormat(" where FACCTSYSTEMID={0} and FFINORGID={1} and FACCTPOLICYID={2} ", this.hsDim.AcctSysId, this.hsDim.AcctOrgId, this.hsDim.AcctPolicyId);
            builder.AppendFormat(" {0} {1} ", this.GetYearPeriodFilter("T0"), this.GetNoStatistical("T1"));
            builder.AppendFormat("   group by FYEAR,FPERIOD,DIM.FENTRYID ) RET ", new object[0]);
            builder.AppendFormat(" on ( T.FDIMID = RET.FDimeEntryId  and T.FYEAR = RET.fyear and T.FPERIOD = RET.fperiod ) ", new object[0]);
            builder.AppendFormat(" when matched then ", new object[0]);
            builder.AppendFormat("  update set T.FRECEIVEQTY = RET.FRECEIVEQTY, T.FSENDQTY = RET.FSENDQTY ", new object[0]);
            return builder.ToString();
        }

        //更新收入发出价格，金额 GetSummaryData调用
        private string UpdateReceiveSendPriceAmount(string tableName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" merge into {0} T using ( ", tableName);
            if (this.chxExpense)
            {
                builder.AppendFormat(" select sum(rt2.FRECEIVEAMOUNT) as FRECEIVEAMOUNT,sum(rt2.FSENDAMOUNT) as FSENDAMOUNT, ", new object[0]);
                builder.AppendFormat("   fyear, fperiod,FDimeEntryId,FEXPENSEID ", new object[0]);
                builder.AppendFormat(" from ( select t0.fyear, t0.fperiod, DIM.FENTRYID FDIMEENTRYID,isnull(exp.FNUMBER,N' ') FEXPENSEID,", new object[0]);
                builder.AppendFormat("  case when t1.FInOutIndex='1' and t1.fbilltypeid <> '{0}' then t2.FAcctgAmount else 0 end  as FRECEIVEAMOUNT,", "d040f688e69b406186aecfb5d9a86101");
                builder.AppendFormat("  case when t1.FInOutIndex='0' then t2.FAcctgAmount when t1.fbilltypeid = '{0}' then 0-t2.FAcctgAmount else 0 end as FSENDAMOUNT ", "d040f688e69b406186aecfb5d9a86101");
                builder.AppendFormat("   from {0} t0 ", "T_HS_OUTACCTG");
                builder.AppendFormat("  inner join {0} cald on t0.FDIMENSIONID=cald.FDIMENSIONID ", "T_HS_CALDIMENSIONS");
                builder.AppendFormat("  inner join {0} t1", this.GetTable("T_HS_OutInStockSeq", "T_HS_OutInStockSeq_H", "V_HS_OutInStockSeq"));
                builder.AppendFormat("   on t0.FID =t1.FACCTGID  and t1.FDocumentStatus = '{0}' ", (DocumentStatus)3);
                builder.AppendFormat(this.JoinDimTable("T1"), new object[0]);
                builder.AppendFormat("  left join {0} t2", this.GetTable("T_HS_Expenses", "T_HS_Expenses_H", "V_HS_Expenses"));
                builder.AppendFormat("    on t1.FEntryId =t2.FSeqEntryId ", new object[0]);
                builder.AppendFormat("  left join {0} exp on t2.FExpensesItemId = exp.FEXPID  ", "T_BD_EXPENSE");
                builder.AppendFormat(" where cald.FACCTSYSTEMID={0} and cald.FFINORGID={1} and cald.FACCTPOLICYID={2} ", this.hsDim.AcctSysId, this.hsDim.AcctOrgId, this.hsDim.AcctPolicyId);
                builder.AppendFormat(" {0} {1} ", this.GetYearPeriodFilter("T0"), this.GetNoStatistical("T1"));
                builder.AppendFormat(" union all {0} ) rt2 ", this.GetAdjustDataByPeriod());
                builder.AppendFormat(" group by FDimeEntryId,FEXPENSEID,fyear, fperiod) ret ", new object[0]);
                builder.AppendFormat(" on (T.FDIMID =RET.FDIMEENTRYID AND T.FYEAR =RET.FYEAR AND ", new object[0]);
                builder.AppendFormat("     T.FPERIOD =RET.FPERIOD and t.FEXPENSEID =ret.FEXPENSEID )", new object[0]);
            }
            else
            {
                builder.AppendFormat(" select sum(rt2.FRECEIVEAMOUNT) as FRECEIVEAMOUNT,sum(rt2.FSENDAMOUNT) as FSENDAMOUNT, ", new object[0]);
                builder.AppendFormat("   fyear, fperiod,FDimeEntryId  ", new object[0]);
                builder.AppendFormat(" from ( select t0.fyear, t0.fperiod,DIM.FENTRYID FDIMEENTRYID ,", new object[0]);
                builder.AppendFormat("  case when t1.FInOutIndex='1' and t1.fbilltypeid <> '{0}' then t1.FAcctgAmount else 0 end  as FRECEIVEAMOUNT,", "d040f688e69b406186aecfb5d9a86101");
                builder.AppendFormat("  case when t1.FInOutIndex='0' then t1.FAcctgAmount when t1.fbilltypeid = '{0}' then 0-t1.FAcctgAmount else 0 end as FSENDAMOUNT ", "d040f688e69b406186aecfb5d9a86101");
                builder.AppendFormat("   from {0} t0 ", "T_HS_OUTACCTG");
                builder.AppendFormat("  inner join {0} cald on t0.FDIMENSIONID=cald.FDIMENSIONID ", "T_HS_CALDIMENSIONS");
                builder.AppendFormat("  inner join {0} t1", this.GetTable("T_HS_OutInStockSeq", "T_HS_OutInStockSeq_H", "V_HS_OutInStockSeq"));
                builder.AppendFormat("   on t0.FID =t1.FACCTGID  and t1.FDocumentStatus = '{0}' ", (DocumentStatus)3);
                builder.AppendFormat(this.JoinDimTable("T1"), new object[0]);
                builder.AppendFormat(" where cald.FACCTSYSTEMID={0} and cald.FFINORGID={1} and cald.FACCTPOLICYID={2} ", this.hsDim.AcctSysId, this.hsDim.AcctOrgId, this.hsDim.AcctPolicyId);
                builder.AppendFormat(" {0} {1} ", this.GetYearPeriodFilter("T0"), this.GetNoStatistical("T1"));
                builder.AppendFormat(" union all {0} ) rt2 ", this.GetAdjustDataByPeriod());
                builder.AppendFormat(" group by FDimeEntryId,fyear, fperiod ) ret ", new object[0]);
                builder.AppendFormat("  on (T.FDIMID =RET.FDIMEENTRYID AND T.FYEAR =RET.FYEAR AND T.FPERIOD =RET.FPERIOD )", new object[0]);
            }
            builder.AppendFormat(" when matched then update set ", new object[0]);
            builder.AppendFormat("  t.FRECEIVEPRICE=case t.FRECEIVEQTY when 0 then 0 else round(ret.FRECEIVEAMOUNT/t.FRECEIVEQTY,10) end, ", new object[0]);
            builder.AppendFormat("  t.FSENDPRICE=case t.FSENDQTY when 0 then 0 else round(ret.FSENDAMOUNT/t.FSENDQTY,10) end, ", new object[0]);
            builder.AppendFormat("  t.FRECEIVEAMOUNT =ret.FRECEIVEAMOUNT,t.FSENDAMOUNT=ret.FSENDAMOUNT ", new object[0]);
            return builder.ToString();
        }

        //更新计算数据 GetSummaryData调用
        private string UpdateEndData(string tableName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" update {0} set ", tableName);
            builder.Append(" FENDQTY = isnull(FINITQTY,0)+ isnull(FRECEIVEQTY,0)-isnull(FSENDQTY,0),");
            builder.Append(" FENDAMOUNT = isnull(FINITAMOUNT,0)+isnull(FRECEIVEAMOUNT,0)-isnull(FSENDAMOUNT,0),");
            builder.Append(" FENDPRICE = case when isnull(FINITQTY,0)+ isnull(FRECEIVEQTY,0)-isnull(FSENDQTY,0) =0 then 0 ");
            builder.Append(" else round((isnull(FINITAMOUNT,0)+isnull(FRECEIVEAMOUNT,0)-isnull(FSENDAMOUNT,0))/ ");
            builder.Append(" (isnull(FINITQTY,0)+ isnull(FRECEIVEQTY,0)-isnull(FSENDQTY,0)),10) end ");
            return builder.ToString();
        }

        //计算支出合计  GetSummaryData调用
        private string InsertExpenSumData(string tableName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" insert /*+append*/ into {0} ", tableName);
            builder.Append(" (FDIMID,FACCTGSYSTEMID, FACCTGORGID,FACCTPOLICYID, FYear,FPeriod,FMATERIALBASEID,FMATERIALID,FMATERIALNAME,FMODEL, FLOTNO,");
            builder.Append("  FASSIPROPERTYID,FMATERPROPERTY,FMATERTYPE,FBOMNO,FPLANNO,FSEQUENCENO,FPROJECTNO,FOWNERID,FSTOCKORGID, ");
            builder.Append("  FSTOCKID,FSTOCKPLACEID,FACCTGRANGEID,FEXPENSEID , FEXPENSENAME,");
            builder.Append("  FUnitID,FSTOCKSTATUSID,FINITAMOUNT,FRECEIVEAMOUNT,FSENDAMOUNT,FENDAMOUNT,FISTOTAL)");
            builder.Append(" SELECT ");
            builder.Append("  FDIMID,FACCTGSYSTEMID, FACCTGORGID,FACCTPOLICYID, FYear,FPeriod,FMATERIALBASEID,FMATERIALID,FMATERIALNAME,FMODEL, FLOTNO,");
            builder.Append("  FASSIPROPERTYID,FMATERPROPERTY,FMATERTYPE,FBOMNO,FPLANNO,FSEQUENCENO,FPROJECTNO ,FOWNERID,FSTOCKORGID, ");
            builder.AppendFormat("  FSTOCKID,FSTOCKPLACEID,FACCTGRANGEID,N'' as FEXPENSEID,N'{0}' as FEXPENSENAME,", this.strSubTotal);
            builder.Append("  FUnitID,FSTOCKSTATUSID,sum(FINITAMOUNT) as FINITAMOUNT,sum(FRECEIVEAMOUNT) as FRECEIVEAMOUNT, ");
            builder.Append("  sum(FSENDAMOUNT) as FSENDAMOUNT,sum(FENDAMOUNT) as FENDAMOUNT,1 as FISTOTAL");
            builder.AppendFormat(" FROM {0} where FISTOTAL=0 ", tableName);
            builder.Append(" GROUP BY ");
            builder.Append(" FDIMID,FACCTGSYSTEMID, FACCTGORGID,FACCTPOLICYID, FYear,FPeriod,FMATERIALBASEID,FMATERIALID,FMATERIALNAME,");
            builder.Append(" FMODEL, FLOTNO, FASSIPROPERTYID,FMATERPROPERTY,FMATERTYPE,FBOMNO,FPLANNO,FSEQUENCENO,FPROJECTNO, ");
            builder.Append(" FOWNERID,FSTOCKORGID, FSTOCKID,FSTOCKPLACEID,FACCTGRANGEID,FUnitID,FSTOCKSTATUSID");
            return builder.ToString();
        }

        //更新支出合计数  GetSummaryData调用
        private string UpdateExpenSumData(string tableName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" merge into {0} T using ( ", tableName);
            builder.AppendFormat("  select distinct FYear,FPeriod,FDIMID,FINITQTY,FRECEIVEQTY,FSENDQTY,FENDQTY from {0} where FISTOTAL=0) RET ", tableName);
            builder.AppendFormat("  on (T.FISTOTAL=1 and T.FDIMID=RET.FDIMID and T.FYEAR=RET.FYEAR and T.FPERIOD=RET.FPERIOD) ", new object[0]);
            builder.AppendFormat(" WHEN MATCHED THEN UPDATE SET T.FINITQTY=RET.FINITQTY,T.FINITPRICE=case when isnull(RET.FINITQTY,0)=0 then 0  ", new object[0]);
            builder.AppendFormat("  else round(isnull(T.FINITAMOUNT,0)/RET.FINITQTY,10) end ,T.FRECEIVEQTY=RET.FRECEIVEQTY, ", new object[0]);
            builder.AppendFormat("  T.FRECEIVEPRICE=case when isnull(RET.FRECEIVEQTY,0)=0 then 0 else round(isnull(T.FRECEIVEAMOUNT,0)/RET.FRECEIVEQTY,10) end,", new object[0]);
            builder.AppendFormat("  T.FSENDQTY=RET.FSENDQTY,T.FSENDPRICE=case when isnull(RET.FSENDQTY,0)=0 then 0 else round(isnull(T.FSENDAMOUNT,0)/RET.FSENDQTY,10) end, ", new object[0]);
            builder.AppendFormat("  T.FENDQTY=RET.FENDQTY,T.FENDPRICE=case when isnull(RET.FENDQTY,0)=0 then 0 else round(isnull(T.FENDAMOUNT,0)/RET.FENDQTY,10) end ", new object[0]);
            return builder.ToString();
        }


        //清除支出数量和价格 GetSummaryData调用
        private string CleanExpQtyPrice(string tableName)
        {
            return string.Format("update {0} set FINITQTY=0,FINITPRICE=0,FRECEIVEQTY=0,FRECEIVEPRICE=0,\r\n                                   FSENDQTY=0, FSENDPRICE=0,FENDQTY=0,FENDPRICE=0 where FISTOTAL = 0 ", tableName);
        }

        //增加列合计数  GetSummaryData调用
        private string InsertSumDataByField(string tableName)
        {
            StringBuilder builder = new StringBuilder();
            string str = this.groupField.Equals("FMATERIALID") ? "FMATERIALID,Min(FMATERIALNAME) FMATERIALNAME,Min(FMODEL) FMODEL " : this.groupField;
            string str2 = this.groupField.Equals("FMATERIALID") ? "FMATERIALID,FMATERIALNAME,FMODEL " : this.groupField;
            string str3 = this.isDisplayPeriod ? "FYEAR,FPERIOD," : "";
            builder.AppendFormat(" insert /*+append*/ into {0}", tableName);
            builder.AppendFormat(" ({0}{1},FDIMID,FISTOTAL,FINITQTY,FINITPRICE, FINITAMOUNT,FRECEIVEQTY,", str3, str2);
            builder.AppendFormat("  FRECEIVEPRICE, FRECEIVEAMOUNT,FSENDQTY,FSENDPRICE, FSENDAMOUNT,FENDQTY,FENDPRICE,FENDAMOUNT) ", new object[0]);
            builder.AppendFormat("  SELECT {0}{1},max(FDIMID)+1 as FDIMID,3 as FISTOTAL,sum(FINITQTY) as FINITQTY, ", str3, str, this.strTotal);
            builder.AppendFormat("  case when sum(FINITQTY)=0 then 0 else round(sum(FINITAMOUNT)/sum(FINITQTY),10) end as FINITPRICE,", new object[0]);
            builder.AppendFormat("  sum(FINITAMOUNT) as FINITAMOUNT, sum(FRECEIVEQTY) as FRECEIVEQTY,case when sum(FRECEIVEQTY)=0 then 0 else round(sum(FRECEIVEAMOUNT)/sum(FRECEIVEQTY),10) end as FRECEIVEPRICE,", new object[0]);
            builder.AppendFormat("  sum(FRECEIVEAMOUNT) as FRECEIVEAMOUNT,sum(FSENDQTY) as FSENDQTY,case when sum(FSENDQTY)=0 then 0 else round(sum(FSENDAMOUNT)/sum(FSENDQTY),10) end as FSENDPRICE,", new object[0]);
            builder.AppendFormat("  sum(FSENDAMOUNT) as FSENDAMOUNT,sum(FENDQTY) as FENDQTY,case when sum(FENDQTY)=0 then 0 else round(sum(FENDAMOUNT)/sum(FENDQTY),10) end as FENDPRICE,sum(FENDAMOUNT) as FENDAMOUNT ", new object[0]);
            builder.AppendFormat("  FROM {0} t0  ", tableName);
            if (this.chxExpense)
            {
                builder.AppendFormat(" where FISTOTAL = 1 ", new object[0]);
            }
            builder.AppendFormat(" group by {0}{1}", str3, this.groupField);
            return builder.ToString();
        }

        //清除空行  GetSummaryData调用
        private string CleanAllZeroRow(string tableName)
        {
            return string.Format(" delete {0} where isnull(FINITQTY,0) = 0 and isnull(FINITAmount,0)=0 and isnull(FRECEIVEQTY,0)=0  \r\n                                    and isnull(FRECEIVEamount,0)=0 and isnull(FSENDQTY,0)=0 and isnull(FSENDamount,0)=0 ", tableName);
        }

        //清除临时表  GetSummaryData调用
        protected virtual void QueryTempTableData(string tableName, string retTableName, DynamicObject filterDyo)
        {
            int num2;
            long currencyIDByAcctPolicy = CommonFunction.GetCurrencyIDByAcctPolicy(base.Context, this.hsDim.AcctPolicyId);
            //int num3 = CommonFunction.GetCurrencyDecimal(base.Context, currencyIDByAcctPolicy, ref num2);
            int num3 = CommonFunction.GetCurrencyDecimal(base.Context, currencyIDByAcctPolicy, out num2);
            StringBuilder builder = new StringBuilder();
            List<string> list = new List<string>();
            FlexParameters parameters = new FlexParameters("", 1);
            CommonFunction.GetFlexShowName(base.Context, parameters, retTableName, "FASSIPROPERTYID", "FASSIPROPNAME");
            FlexParameters parameters2 = new FlexParameters("", 2);
            CommonFunction.GetFlexShowName(base.Context, parameters2, retTableName, "FSTOCKPLACEID", "FSTOCKPLACENAME");
            string str = string.Format("{0} T.{1} asc ,T.FDIMID asc,T.FISTOTAL desc {2} ", this.isDisplayPeriod ? " T.FYEAR asc,T.FPERIOD asc ," : "", this.groupField, this.chxExpense ? ",T.FExpenseID asc" : "");
            builder.AppendFormat(" SELECT {0} ", this.isDisplayPeriod ? "TOCHAR(fyear)||'.'||(case when len(fperiod)=1 then '0'||TOCHAR(fperiod) else TOCHAR(fperiod) end) FYearPeriod ," : "");
            builder.AppendFormat(" T.FMATERIALBASEID,T.FMATERIALID,FMATERIALNAME,GROUPL.FNAME FMATERIALGROUP,FMODEL, FLOTNO, FASSIPROPERTYID,ENUML.FCAPTION FMATERPROPERTY,MT.FNAME FMATERTYPE,", new object[0]);
            builder.AppendFormat(" BOM.FNUMBER FBOMNO,FPLANNO,FSEQUENCENO,FPROJECTNO,FOWNERID,OWN.FNAME FOWNERNAME,FSTOCKORGID,SORG.FNAME FSTOCKORGNAME,T.FSTOCKID,STOCK.FNAME FSTOCKNAME,", new object[0]);
            builder.AppendFormat(" FSTOCKPLACEID,FSTOCKPLACENAME,RANG.FNUMBER FACCTGRANGEID,RANGL.FNAME FACCTGRANGENAME,UNITL.FNAME FUNITNAME,t.FUnitID {0},", this.GetExpenField(""));
            builder.AppendFormat(" FINITQTY,FINITPRICE, FINITAMOUNT,FRECEIVEQTY, FRECEIVEPRICE, FRECEIVEAMOUNT,FSENDQTY, FSENDPRICE, FSENDAMOUNT,FENDQTY,FENDPRICE, ", new object[0]);
            builder.AppendFormat(" FENDAMOUNT, t.FSTOCKSTATUSID,sts.FNAME FSTOCKSTATUSNAME,FDIMID,FDIMID as FAcctgDimID,FASSIPROPNAME,{0} as FDIGITS,", num3);
            builder.AppendFormat(" {0} as FPRICEDIGITS,unit.FPRECISION as FQtyDigits,ENUML1.FCAPTION FVALUATION,FIsTotal,FGroupByFIeld,FDetailBillFormId FDetailReportFormId, ", num2);
            builder.AppendFormat(" {0} into {1} from {2} T ", string.Format(base.KSQL_SEQ, str), tableName, retTableName);
            builder.AppendFormat("  left join {0} sts on t.FSTOCKSTATUSID = sts.FSTOCKSTATUSID and sts.FLOCALEID={1}", "T_BD_STOCKSTATUS_L", this.lCID);
            builder.AppendFormat("  left join {0} unit on unit.funitid = t.FUnitID ", "T_BD_UNIT");
            builder.AppendFormat("  left join {0} RANG ", "T_HS_ACCTGRANGE");
            builder.AppendFormat("    on T.FACCTGRANGEID = RANG.FACCTGRANGEID ", new object[0]);
            builder.AppendFormat("  left join {0} SORG ", "T_ORG_ORGANIZATIONS_L");
            builder.AppendFormat("    on T.FSTOCKORGID = SORG.FORGID and SORG.FLOCALEID = {0} ", this.lCID);
            builder.AppendFormat("  left join {0}  OWN ", "T_ORG_ORGANIZATIONS_L");
            builder.AppendFormat("    on T.FOWNERID = OWN.FORGID  and OWN.FLOCALEID = {0} ", this.lCID);
            builder.AppendFormat("  left join {0}  STOCK ", "t_BD_Stock_L");
            builder.AppendFormat("    on T.FSTOCKID = STOCK.FSTOCKID  and STOCK.FLOCALEID = {0} ", this.lCID);
            builder.AppendFormat("  left join {0}  RANGL ", "T_HS_ACCTGRANGE_L");
            builder.AppendFormat("    on T.FACCTGRANGEID = RANGL.FACCTGRANGEID and RANGL.FLOCALEID = {0} ", this.lCID);
            builder.AppendFormat("  left join {0}  UNITL ", "T_BD_UNIT_L");
            builder.AppendFormat("    on T.FUNITID = UNITL.FUNITID and UNITL.FLOCALEID = {0} ", this.lCID);
            builder.AppendFormat("  left join {0}  MT ", "T_BD_MATERIALCATEGORY_L");
            builder.AppendFormat("    on T.FMATERTYPE = MT.FCATEGORYID and MT.FLOCALEID = {0} ", this.lCID);
            builder.AppendFormat("  left join {0} BOM ", "t_eng_bom");
            builder.AppendFormat("    on BOM.FID = T.FBOMNO ", new object[0]);
            builder.AppendFormat("  left join {0} ENUM ", "T_META_FORMENUMITEM");
            builder.AppendFormat("    on ENUM.FID = '{0}' and ENUM.FVALUE = T.FMATERPROPERTY ", "ac14913e-bd72-416d-a50b-2c7432bbff63");
            builder.AppendFormat("  left join {0} ENUML ", "T_META_FORMENUMITEM_L");
            builder.AppendFormat("    on ENUM.FENUMID = ENUML.FENUMID and ENUML.FLOCALEID = {0} ", this.lCID);
            builder.AppendFormat("  left join {0} ENUM1 ", "T_META_FORMENUMITEM");
            builder.AppendFormat("   on ENUM1.FID = '{0}' and ENUM1.FValue  = t.FVALUATION  ", "eca675f6-d296-4ba9-b9df-170b7b286a73");
            builder.AppendFormat("  left join {0} ENUML1  ", "T_META_FORMENUMITEM_L");
            builder.AppendFormat("   on ENUM1.FENUMID = ENUML1.FENUMID and ENUML1.FLOCALEID = {0} ", this.lCID);
            builder.AppendFormat("  left join {0} MAT ON T.FMATERIALBASEID = MAT.FMATERIALID ", "t_bd_Material");
            builder.AppendFormat("  left join T_BD_MATERIALGROUP_L GROUPL ON MAT.FMATERIALGROUP=GROUPL.FID AND GROUPL.FLOCALEID= {0} ", this.lCID);
            builder.AppendFormat(" where 1 = 1 ", new object[0]);
            if (!base.ReportProperty.IsGroupSummary && Convert.ToBoolean(filterDyo["CHXTotal"]))
            {
                builder.Append(" and ( FISTOTAL = 3 ) ");
            }
            if (Convert.ToBoolean(filterDyo["CHXNOINOUT"]))
            {
                builder.Append(" and (FRECEIVEQTY<>0 OR FSENDQTY<>0) ");
            }
            list.Add(builder.ToString());
            builder.Clear();
            if (!base.ReportProperty.IsGroupSummary)
            {
                this.groupField = this.GetGroupByField(filterDyo["COMBOTotalType"].ToString(), true);
                builder.AppendFormat(" update {0} set FQtyDigits=(select max(unit.FPRECISION) from {1} t ", tableName, retTableName);
                builder.AppendFormat("  left join {0} unit on unit.funitid = t.FUnitID where t.FISTOTAL=0 ), ", "T_BD_UNIT");
                builder.AppendFormat("  {0}={0}||N'-{1}' where FISTOTAL = 3 ", this.groupField, this.strTotal);
                list.Add(builder.ToString());
            }
            FINDBUtils.ExecuteBatchWithTime(base.Context, list, list.Count);
        }

        //增加临时表（无期初） GetSummaryData调用
        private void CreateTmpTableForNoInit(string tableName, string retTableName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" SELECT FISTOTAL,FINITQTY,FINITAMOUNT,FRECEIVEQTY,FRECEIVEAMOUNT,FSENDQTY,FSENDAMOUNT,FENDQTY,", new object[0]);
            builder.AppendFormat("  FENDAMOUNT,{0} into {1} from {2} ", string.Format(base.KSQL_SEQ, "FGROUPBYFIELD"), tableName, retTableName);
            DBUtils.Execute(base.Context, builder.ToString());
        }


        //获取表

        private string GetTable(string curTable, string hisTable, string view)
{
    string str = string.Empty;
    switch (this.currTable)
    {
        case -1:
        {
            YearPeriod period3 = new YearPeriod();
            period3.Year=this.hsDim.StartYear;
            period3.Period=this.hsDim.StartPeriod;
            YearPeriod startPeriod = period3;
            YearPeriod period4 = new YearPeriod();
            period4.Year=this.hsDim.EndYear;
            period4.Period=this.hsDim.EndPeriod;
            YearPeriod endPeriod = period4;
            Tuple<string, string, string> tables = new Tuple<string, string, string>(curTable, hisTable, view);
            str = ReportCommonFunction.ExchangeHistoryTable(base.Context, this.hsDim.AcctSysId, this.hsDim.AcctOrgId, this.hsDim.AcctPolicyId, startPeriod, endPeriod, tables);
            if (str != tables.Item1)
            {
                if (str == tables.Item2)
                {
                    this.currTable = 1;
                    return str;
                }
                this.currTable = 2;
                return str;
            }
            this.currTable = 0;
            return str;
        }
        case 0:
            return curTable;

        case 1:
            return hisTable;

        case 2:
            return view;
    }
    return str;
}

        //获取物料显示方式  CreateMaterialInfo 调用
        private string GetFieldByDisPlayDimType(bool isAdjust = false)
        {
            StringBuilder builder = new StringBuilder();
            string str = string.Empty;
            if (isAdjust)
            {
                str = "CALD.FACCTGSYSTEMID FACCTGSYSTEMID,CALD.FACCTORGID FACCTGORGID";
            }
            else
            {
                str = "CALD.FACCTSYSTEMID FACCTGSYSTEMID,CALD.FFINORGID FACCTGORGID";
            }
            if (this.dispalyDimType == 0)
            {
                builder.AppendFormat(" select HSDIM.FENTRYID FDIMID,{0},CALD.FACCTPOLICYID FACCTPOLICYID,AG.FYEAR FYEAR,AG.FPERIOD  FPERIOD, ", str);
                builder.AppendFormat("  HSDIM.FMASTERID FMATERIALBASEID,MAT.FNUMBER FMATERIALID,MAT_L.FNAME FMATERIALNAME,MAT_L.FSPECIFICATION FMODEL,HSDIM.FLOTNUMBER FLOTNO,", new object[0]);
                builder.AppendFormat("  HSDIM.FAUXPROPID FASSIPROPERTYID, MAT_T.FERPCLSID  FMATERPROPERTY,MAT_T.FCATEGORYID  FMATERTYPE, ", new object[0]);
                builder.AppendFormat("  HSDIM.FBOMID FBOMNO,HSDIM.FMTONO  FPLANNO,N'' FSEQUENCENO,HSDIM.FPROJECTNO  FPROJECTNO, 0  FOWNERID,0  FSTOCKORGID, ", new object[0]);
                builder.AppendFormat("  0  FSTOCKSTATUSID,HSDIM.FSTOCKID  FSTOCKID,HSDIM.FSTOCKLOCID FSTOCKPLACEID,HSDIM.FACCTGRANGEID  FACCTGRANGEID, ", new object[0]);
                builder.AppendFormat("  MAT_T.FBASEUNITID FUNITID,HSDIM.FVALUATIONMETHOD FVALUATIO,'HS_INOUTSTOCKDETAILRPT',0 ", new object[0]);
            }
            else
            {
                builder.AppendFormat(" select STOCKDIM.FENTRYID FDIMID,{0},CALD.FACCTPOLICYID FACCTPOLICYID,AG.FYEAR FYEAR,AG.FPERIOD  FPERIOD, ", str);
                builder.AppendFormat("  STOCKDIM.FMATERIALID FMATERIALBASEID,MAT.FNUMBER FMATERIALID,MAT_L.FNAME FMATERIALNAME,MAT_L.FSPECIFICATION FMODEL,LOT.FNUMBER FLOTNO,", new object[0]);
                builder.AppendFormat("  STOCKDIM.FAUXPROPID FASSIPROPERTYID,MAT_T.FERPCLSID  FMATERPROPERTY,MAT_T.FCATEGORYID  FMATERTYPE,STOCKDIM.FBOMID FBOMNO,", new object[0]);
                builder.AppendFormat("  STOCKDIM.FMTONO  FPLANNO,  N'' FSEQUENCENO,STOCKDIM.FPROJECTNO  FPROJECTNO,STOCKDIM.FCargoOwnerId FOWNERID, ", new object[0]);
                builder.AppendFormat("  STOCKDIM.FStockOrgId FSTOCKORGID,STOCKDIM.FSTOCKSTATUSID  FSTOCKSTATUSID,STOCKDIM.FSTOCKID  FSTOCKID,STOCKDIM.FSTOCKLOCID FSTOCKPLACEID,", new object[0]);
                builder.AppendFormat("  HSDIM.FACCTGRANGEID  FACCTGRANGEID,MAT_T.FBASEUNITID FUNITID,HSDIM.FVALUATIONMETHOD FVALUATIO ,'HS_INOUTSTOCKDETAILRPT',0 ", new object[0]);
            }
            if (this.chxExpense)
            {
                builder.Append(" ,Isnull(EXP.FNUMBER, N' ') as FEXPENSEID,EXP_L.FNAME as FEXPENSENAME ");
            }
            return builder.ToString();
        }


        private string BindFilterSQL(IRptParams filter, bool isAdjust = false)
        {
            DynamicObject obj2 = filter.FilterParameter.CustomFilter;
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" and CAlD.{0}={1} ", isAdjust ? "FACCTGSYSTEMID" : "FACCTSYSTEMID", this.hsDim.AcctSysId);
            builder.AppendFormat(" and CAlD.{0}={1} ", isAdjust ? "FACCTORGID" : "FFINORGID", this.hsDim.AcctOrgId);
            builder.AppendFormat(" and CAlD.FACCTPOLICYID={0} ", this.hsDim.AcctPolicyId);
            builder.AppendFormat(this.GetYearPeriodFilter("AG"), new object[0]);
            DynamicObject obj3 = obj2["MATERIALID"] as DynamicObject;
            DynamicObject obj4 = obj2["ENDMATERIALID"] as DynamicObject;
            if (obj3 != null)
            {
                builder.AppendFormat(" and MAT.FNUMBER >= '{0}'", obj3["Number"]);
            }
            if (obj4 != null)
            {
                builder.AppendFormat(" and MAT.FNUMBER <= '{0}'", obj4["Number"]);
            }
            DynamicObject obj5 = obj2["ACCTGRANGEID"] as DynamicObject;
            if (obj5 != null)
            {
                builder.AppendFormat(" and HSDIM.FACCTGRANGEID = {0}", Convert.ToInt64(obj5["Id"]));
            }
            if (this.dispalyDimType == 1)
            {
                DynamicObject obj6 = obj2["STOCKORGID"] as DynamicObject;
                if (obj6 != null)
                {
                    builder.AppendFormat(" and STOCKDIM.FSTOCKORGID = {0}", Convert.ToInt64(obj6["Id"]));
                }
                DynamicObject obj7 = obj2["OWNERID"] as DynamicObject;
                if (obj7 != null)
                {
                    builder.AppendFormat(" and STOCKDIM.FCARGOOWNERID = {0}", Convert.ToInt64(obj7["Id"]));
                }
                DynamicObjectCollection objects = obj2["STOCKSTATUSID"] as DynamicObjectCollection;
                if ((objects != null) && (objects.Count > 0))
                {
                    string str = string.Join<object>(",", from a in objects select a["STOCKSTATUSID_Id"]);
                    builder.AppendFormat(" and STOCKDIM.FSTOCKSTATUSID in ({0})", str);
                }
            }
            DynamicObject obj8 = obj2["STOCKID"] as DynamicObject;
            if (obj8 != null)
            {
                builder.AppendFormat(" and STOCK.FNUMBER >= '{0}'", Convert.ToString(obj8["Number"]));
            }
            DynamicObject obj9 = obj2["ENDSTOCKID"] as DynamicObject;
            if (obj9 != null)
            {
                builder.AppendFormat(" and STOCK.FNUMBER <= '{0}'", Convert.ToString(obj9["Number"]));
            }
            DynamicObject obj10 = obj2["EXPENID"] as DynamicObject;
            DynamicObject obj11 = obj2["ENDEXPENID"] as DynamicObject;
            if (obj10 != null)
            {
                builder.AppendFormat(" and EXP.FNUMBER >= '{0}'", obj10["Number"]);
            }
            if (obj11 != null)
            {
                builder.AppendFormat(" and EXP.FNUMBER <= '{0}'", obj11["Number"]);
            }
            DynamicObject obj12 = BillExtension.GetValue<DynamicObject>(obj2, "MATERTYPE");
            if (obj12 != null)
            {
                builder.AppendFormat(" and  MAT_T.FCATEGORYID={0}", BillExtension.GetValue<long>(obj12, "Id"));
            }
            if (filter.FilterParameter.FilterRows.Count > 0)
            {
                string str2;
                if (this.dispalyDimType == 0)
                {
                    str2 = "HSDIM.FLOTNUMBER";
                }
                else
                {
                    str2 = "LOT.FNUMBER";
                }
                builder.AppendFormat(" and {0}", filter.FilterParameter.FilterString.Replace("FLOTNO", str2));
            }
            return builder.ToString();
        }

        private string JoinDimTable(string tableName = "T1")
        {
            StringBuilder builder = new StringBuilder();
            if (this.dispalyDimType == 0)
            {
                builder.AppendFormat("  inner join {0} DIM ", "T_HS_StockDimension");
                builder.AppendFormat("  on {0}.FACCTGDIMEENTRYID = DIM.FENTRYID ", tableName);
            }
            else
            {
                builder.AppendFormat("  inner join {0} DIM ", "T_HS_InivStockDimension");
                builder.AppendFormat("  on {0}.FDIMEENTRYID = DIM.FENTRYID ", tableName);
            }
            return builder.ToString();
        }

        private string GetYearPeriodFilter(string tableName = "T0")
        {
            StringBuilder builder = new StringBuilder();
            if (this.hsDim.StartYear == this.hsDim.EndYear)
            {
                if (this.hsDim.StartPeriod == this.hsDim.EndPeriod)
                {
                    builder.AppendFormat(" and {0}.FYEAR = {1} and {0}.FPERIOD = {2} ", tableName, this.hsDim.StartYear, this.hsDim.StartPeriod);
                }
                else
                {
                    builder.AppendFormat(" and {0}.FYEAR = {1} and {0}.FPERIOD >= {2} ", tableName, this.hsDim.StartYear, this.hsDim.StartPeriod);
                    builder.AppendFormat(" and {0}.FPERIOD <= {1} ", tableName, this.hsDim.EndPeriod);
                }
            }
            else if (this.hsDim.EndYear == (this.hsDim.StartYear + 1))
            {
                builder.AppendFormat(" and (({0}.FYEAR = {1} and {0}.FPERIOD >= {2}) ", tableName, this.hsDim.StartYear, this.hsDim.StartPeriod);
                builder.AppendFormat(" or ({0}.FYEAR = {1} and {0}.FPERIOD <= {2})) ", tableName, this.hsDim.EndYear, this.hsDim.EndPeriod);
            }
            else
            {
                builder.AppendFormat(" and (({0}.FYEAR > {1} and {0}.FYEAR < {2})  ", tableName, this.hsDim.StartYear, this.hsDim.EndYear);
                builder.AppendFormat(" or ({0}.FYEAR = {1} and {0}.FPERIOD >= {2}) ", tableName, this.hsDim.StartYear, this.hsDim.StartPeriod);
                builder.AppendFormat(" or ({0}.FYEAR = {1} and {0}.FPERIOD <= {2})) ", tableName, this.hsDim.EndYear, this.hsDim.EndPeriod);
            }
            return builder.ToString();
        }


        private string GetNoStatistical(string tableName = "T1")
        {
            StringBuilder builder = new StringBuilder();
            if (this.chxNoCostAllot)
            {
                builder.AppendFormat(" and ({0}.FISSETTLE = '1' or {0}.FBILLFROMID not in ('STK_TRANSFERIN','STK_TRANSFEROUT','PRD_INSTOCK')  or ({0}.FBILLFROMID <> 'STK_TRANSFERIN' and {0}.FBILLFROMID <> 'STK_TRANSFEROUT' and {0}.FISGENFORIOS = '0')) ", tableName);
                builder.AppendFormat(" and {0}.FOUTINSTOCKTYPE<>'{1}' ", tableName, 12);
            }
            if (this.chxNoStockAdj)
            {
                builder.AppendFormat(" and {0}.FBILLFROMID not in ('{1}',", tableName, "STK_LOTADJUST");
                builder.AppendFormat(" '{0}','{1}') ", "STK_StockConvert", "STK_StatusConvert");
            }
            return builder.ToString();
        }





        private string GetExpenField(string tableName = "")
        {
            if (!this.chxExpense)
            {
                return string.Empty;
            }
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return ",FEXPENSEID , FEXPENSENAME ";
            }
            return string.Format(" ,{0}.FEXPENSEID , {0}.FEXPENSENAME ", tableName);
        }


        private string GetAdjustDataByPeriod()
        {
            StringBuilder builder = new StringBuilder();
            DateTime time = DateTime.Parse(CommonFunction.GetPeriodDate(base.Context, this.hsDim.AcctPolicyId, this.hsDim.StartYear, this.hsDim.StartPeriod)["startDate"].ToString());
            DateTime time2 = DateTime.Parse(CommonFunction.GetPeriodDate(base.Context, this.hsDim.AcctPolicyId, this.hsDim.EndYear, this.hsDim.EndPeriod)["endDate"].ToString());
            builder.AppendFormat(" select OUTACCT.FYEAR,OUTACCT.FPERIOD, DIM.FENTRYID FDIMEENTRYID ,", new object[0]);
            if (this.chxExpense)
            {
                builder.AppendFormat(" Isnull(BDEXP.FNUMBER, N' ') as FEXPENSEID, ", new object[0]);
                builder.AppendFormat(" case when T0.FBILLTYPEID = '{0}' then EXP.FEXPENSEAMOUNT  else 0  end as FRECEIVEAMOUNT,", "4d20243e32b14834aae8a96525354bcd");
                builder.AppendFormat(" case when T0.FBILLTYPEID = '{0}' then 0 else EXP.FEXPENSEAMOUNT end as FSENDAMOUNT ", "4d20243e32b14834aae8a96525354bcd");
                builder.AppendFormat(" from {0} T0 ", "T_HS_AdjustmentBill");
                builder.AppendFormat("  inner join {0} ADJENTRY ", "T_HS_AdjustmentBillEntry");
                builder.AppendFormat("  on T0.FID = ADJENTRY.FID ", new object[0]);
                builder.AppendFormat("  inner join {0} OUTACCT ", "T_HS_OUTACCTG");
                builder.AppendFormat("  on OUTACCT.FID = T0.FACCTGID ", new object[0]);
                builder.AppendFormat(this.JoinDimTable("ADJENTRY"), new object[0]);
                builder.AppendFormat("  left join {0} EXP ", "T_HS_ADJUSTEXPENSEENTRY");
                builder.AppendFormat("  on ADJENTRY.FENTRYID = EXP.FENTRYID ", new object[0]);
                builder.AppendFormat("  left join {0} BDEXP ", "T_BD_EXPENSE");
                builder.AppendFormat("  on EXP.FEXPENSEID = BDEXP.FEXPID ", new object[0]);
            }
            else
            {
                builder.AppendFormat(" case when T0.FBILLTYPEID = '{0}' then ADJENTRY.FADJUSTMENTAMOUNT else 0 end as FRECEIVEAMOUNT,", "4d20243e32b14834aae8a96525354bcd");
                builder.AppendFormat(" case when T0.FBILLTYPEID = '{0}' then 0  else ADJENTRY.FADJUSTMENTAMOUNT end as FSENDAMOUNT ", "4d20243e32b14834aae8a96525354bcd");
                builder.AppendFormat(" from {0} T0 ", "T_HS_AdjustmentBill");
                builder.AppendFormat("  inner join {0} ADJENTRY ", "T_HS_AdjustmentBillEntry");
                builder.AppendFormat("   on T0.FID = ADJENTRY.FID ", new object[0]);
                builder.AppendFormat("  inner join {0} OUTACCT ", "T_HS_OUTACCTG");
                builder.AppendFormat("   on OUTACCT.FID = T0.FACCTGID ", new object[0]);
                builder.AppendFormat(this.JoinDimTable("ADJENTRY"), new object[0]);
            }
            builder.AppendFormat(" where T0.FACCTGSYSTEMID = {0} ", this.hsDim.AcctSysId);
            builder.AppendFormat("   and T0.FACCTORGID = {0} ", this.hsDim.AcctOrgId);
            builder.AppendFormat("   and T0.FACCTPOLICYID = {0} ", this.hsDim.AcctPolicyId);
            builder.AppendFormat("  and ( T0.FBUSINESSTYPE = '0' or T0.FISACCTGGENERATE = '1' )", new object[0]);
            builder.AppendFormat("  and T0.FDATE >= {0} ", DateTimeFormatUtils.ToKSQlFormat(time));
            builder.AppendFormat("  and T0.FDATE <= {0} ", DateTimeFormatUtils.ToKSQlFormat(time2));
            builder.AppendFormat("  and T0.FDOCUMENTSTATUS = '{0}' and T0.FFORBIDSTATUS = '{1}' ", (DocumentStatus)3, (ForbidStatus)0);
            return builder.ToString();
        }







 

 

        


    }
}
