using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DependencyRules;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Permission.Objects;
using Kingdee.BOS.Core.Report;
using Kingdee.K3.SCM.App.Stock.Report;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.Report
{
    [Description("扩展-物料收发汇总表服务端插件")]
    public class JNStockSumRptService : Kingdee.K3.SCM.App.Stock.Report.StockSummaryRpt
    {
        protected string tmpRptTbl;
        protected string tmpQcpTbl;
        public override void Initialize()
        {
            base.Initialize();
            SetDecimalControl("FJNUnitEnzyme");
            SetDecimalControl("FJNTonProperty_QC");
            SetDecimalControl("FJNTonProperty_SR");
            SetDecimalControl("FJNTonProperty_FC");
            SetDecimalControl("FJNTonProperty_JC");
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
            reportHeader.AddChild("FJNTonProperty_QC", new LocaleValue("期初&标吨", base.Context.UserLocale.LCID));
            reportHeader.AddChild("FJNTonProperty_SR", new LocaleValue("收入&标吨", base.Context.UserLocale.LCID));
            reportHeader.AddChild("FJNTonProperty_FC", new LocaleValue("发出&标吨", base.Context.UserLocale.LCID));
            reportHeader.AddChild("FJNTonProperty_JC", new LocaleValue("结存&标吨", base.Context.UserLocale.LCID));
            return reportHeader;
        }

        public override void BuilderReportSqlAndTempTable(Kingdee.BOS.Core.Report.IRptParams filter, string tableName)
        {
            //把之前账表的数据放入新建的临时表
            string strTempTable = AppServiceContext.DBService.CreateTemporaryTableName(this.Context);
           /* List<BaseDataTempTable> baseDataTempTable = filter.BaseDataTempTable;
            
            if ((base.CacheDataList != null) && (base.CacheDataList.Count != 0))
            {
                base.ReBuildCurPageTable();
                string strAnd = string.Empty;
                base.SetFilter(filter);
                DataRow row = base.CacheDataList[filter.CurrentPosition];
                if (base.isSplitPageByOwner)
                {
                    base.pagerParm = new StockRptPager(Convert.ToInt64(row["FSTOCKORGID"]), row["FOWNERTYPEID"].ToString(), Convert.ToInt64(row["FOWNERID"]));
                }
                else
                {
                    base.pagerParm = new StockRptPager(Convert.ToInt64(row["FSTOCKORGID"]));
                }
                string invGroupField = this.GetInvGroupField(filter, ref strAnd);
                List<SqlObject> lstSql = new List<SqlObject>();
                List<SqlObject> updateIOPriceSql = this.GetUpdateIOPriceSql(filter);
                if ((updateIOPriceSql != null) && (updateIOPriceSql.Count > 0))
                {
                    lstSql.AddRange(updateIOPriceSql);
                }
                this.GetInsertDataSql(lstSql, invGroupField, baseDataTempTable);
                this.GetUpdatePricePrecSql(lstSql);
                this.GetUpdateExtDataDataSql(lstSql);
                base.SetMoreFilterFormat();
                foreach (SqlObject obj2 in lstSql)
                {
                    DBUtils.Execute(base.Context, obj2.Sql, obj2.Param);
                }
                lstSql.Clear();
                base.GetUpdateStkQtySql(lstSql);
                updateIOPriceSql = this.GetUpdateQcPriceSql(filter);
                if ((updateIOPriceSql != null) && (updateIOPriceSql.Count > 0))
                {
                    lstSql.AddRange(updateIOPriceSql);
                }
                this.GetSumRptUpdteQcAmount(lstSql);
                this.GetUpdateIoBaseAmountSql(lstSql);
                base.GetUpdateSql(lstSql);
                this.GetJcQtyPriceAmountSql(lstSql);
                this.GetDeleteSql(lstSql);
                base.GetPreFormatSql(lstSql);
                foreach (SqlObject obj3 in lstSql)
                {
                    DBUtils.Execute(base.Context, obj3.Sql, obj3.Param);
                }
                this.SetRptDate(tableName);

            }*/

            this.baseBuilderReportSqlAndTempTable(filter, strTempTable);
            string strSql = string.Format(@"/*dialect*/select t1.*,
                                          case when (ISNULL(t1.FBaseJCQty,0)<>0 or ISNULL(t1.FBaseOutQty,0)<>0 or ISNULL(t1.FBaseInQty,0)<>0) and t2.FIsMeasure='1' then 
                                          (case when ISNULL(t1.FBaseJCQty,0)<>0 then t1.FSecJCQty/t1.FBaseJCQty else
                                           (case when ISNULL(t1.FSecOutQty,0)<>0 then t1.FSecOutQty/t1.FBaseOutQty else 
                                           (case when ISNULL(t1.FBaseInQty,0)<>0 then t1.FSecInQty/t1.FBaseInQty end ) end) end )
                                          else 0 end FJNUnitEnzyme,
                                          case when ISNULL(t2.FJNTonProperty,0)<>0  and t2.FIsMeasure='1' then t1.FSECQCQTY/(t2.FJNTonProperty*1000) else 0 end FJNTonProperty_QC,
                                          case when ISNULL(t2.FJNTonProperty,0)<>0  and t2.FIsMeasure='1' then t1.FSECINQTY/(t2.FJNTonProperty*1000) else 0 end FJNTonProperty_SR,
                                          case when ISNULL(t2.FJNTonProperty,0)<>0  and t2.FIsMeasure='1' then t1.FSECOUTQTY/(t2.FJNTonProperty*1000) else 0 end FJNTonProperty_FC,
                                          case when ISNULL(t2.FJNTonProperty,0)<>0  and t2.FIsMeasure='1' then t1.FSECJCQTY/(t2.FJNTonProperty*1000) else 0 end FJNTonProperty_JC
                                          into {0} from {1} as t1
                                          join T_BD_MATERIAL t2 on t1.FMaterialId=t2.FMaterialId", tableName, strTempTable);
            DBUtils.Execute(this.Context, strSql);
            this.dropTemplateTable(this.Context, strTempTable);
            //AppServiceContext.DBService.DeleteTemporaryTableName(this.Context, new string[] { strTempTable });
        }

        private void baseBuilderReportSqlAndTempTable(IRptParams filter, string strTempTable)
        {
            List<BaseDataTempTable> baseDataTempTable = filter.BaseDataTempTable;
            if ((base.CacheDataList != null) && (base.CacheDataList.Count != 0))
            {
                //base.ReBuildCurPageTable();--重新
                this.JNReBuildCurPageTable();
                string sql = string.Format("/*dialect*/ \r\n alter table {0} alter column  FSECINQTY   decimal(33, 10) ", this.tmpRptTbl);
                DBUtils.Execute(base.Context, sql);
                sql = string.Format("/*dialect*/ \r\n alter table {0} alter column  FSECJCQTY   decimal(33, 10) ", this.tmpRptTbl);
                DBUtils.Execute(base.Context, sql);
                sql = string.Format("/*dialect*/ \r\n alter table {0} alter column  FSECOUTQTY   decimal(33, 10) ", this.tmpRptTbl);
                DBUtils.Execute(base.Context, sql);
                sql = string.Format("/*dialect*/ \r\n alter table {0} alter column  FSECQCQTY   decimal(33, 10) ", this.tmpRptTbl);
                DBUtils.Execute(base.Context, sql);
                string strAnd = string.Empty;
                base.SetFilter(filter);
                DataRow row = base.CacheDataList[filter.CurrentPosition];
                if (base.isSplitPageByOwner)
                {
                    base.pagerParm = new StockRptPager(Convert.ToInt64(row["FSTOCKORGID"]), row["FOWNERTYPEID"].ToString(), Convert.ToInt64(row["FOWNERID"]));
                }
                else
                {
                    base.pagerParm = new StockRptPager(Convert.ToInt64(row["FSTOCKORGID"]));
                }
                string invGroupField = this.GetInvGroupField(filter, ref strAnd);
                List<SqlObject> lstSql = new List<SqlObject>();
                List<SqlObject> updateIOPriceSql = this.GetUpdateIOPriceSql(filter);
                if ((updateIOPriceSql != null) && (updateIOPriceSql.Count > 0))
                {
                    lstSql.AddRange(updateIOPriceSql);
                }
                this.GetInsertDataSql(lstSql, invGroupField, baseDataTempTable);
                this.GetUpdatePricePrecSql(lstSql);
                this.GetUpdateExtDataDataSql(lstSql);
                base.SetMoreFilterFormat();
                foreach (SqlObject obj2 in lstSql)
                {
                    DBUtils.Execute(base.Context, obj2.Sql, obj2.Param);
                }
                lstSql.Clear();
                base.GetUpdateStkQtySql(lstSql);
                updateIOPriceSql = this.GetUpdateQcPriceSql(filter);
                if ((updateIOPriceSql != null) && (updateIOPriceSql.Count > 0))
                {
                    lstSql.AddRange(updateIOPriceSql);
                }
                this.GetSumRptUpdteQcAmount(lstSql);
                this.GetUpdateIoBaseAmountSql(lstSql);
                base.GetUpdateSql(lstSql);
                this.GetJcQtyPriceAmountSql(lstSql);
                this.GetDeleteSql(lstSql);
                base.GetPreFormatSql(lstSql);
                foreach (SqlObject obj3 in lstSql)
                {
                    DBUtils.Execute(base.Context, obj3.Sql, obj3.Param);
                }
                this.SetRptDate(strTempTable);

            }
        }

        private string GetInvGroupField(IRptParams filter, ref string strAnd)
        {
            List<string> list = new List<string>();
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("FSTOCKORGID", "T0.FSTOCKORGID");
            dictionary.Add("FOWNERTYPEID", "T0.FOWNERTYPEID");
            dictionary.Add("FOWNERID", "T0.FOWNERID");
            dictionary.Add("FMATERIALID", "T0.FMATERIALID");
            dictionary.Add("FMATERIALNAME", "T0.FMATERIALID");
            dictionary.Add("FAUXPROPID", "T0.FAUXPROPID");
            dictionary.Add("FLOTNO", "T0.FLOTNO");
            dictionary.Add("FSTOCKID", "T0.FSTOCKID");
            dictionary.Add("FSTOCKLOC", "T0.FSTOCKLOCID");
            dictionary.Add("FSTOCKSTATUSID", "T0.FSTOCKSTATUSID");
            dictionary.Add("FKEEPERTYPEID", "T0.FKEEPERTYPEID");
            dictionary.Add("FKEEPERID", "T0.FKEEPERID");
            dictionary.Add("FPRODUCEDATE", "T0.FPRODUCEDATE");
            dictionary.Add("FEXPIRYDATE", "T0.FEXPIRYDATE");
            dictionary.Add("FBOMID", "T0.FBOMID");
            dictionary.Add("FMTONO", "T0.FMTONO");
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            List<Field> dspColumnFieldList = filter.FilterFieldInfo.DspColumnFieldList;
            string str = string.Empty;
            foreach (ColumnField field in filter.FilterParameter.ColumnInfo)
            {
                str = field.Key.ToUpperInvariant();
                if (dictionary.Keys.Contains<string>(str) && !list.Contains(dictionary[str]))
                {
                    builder.Append(dictionary[str]);
                    builder.Append(",");
                    builder2.Append(" AND T0.");
                    builder2.Append(dictionary[str]);
                    builder2.Append("=T1.");
                    builder2.Append(dictionary[str]);
                    list.Add(dictionary[str]);
                }
            }
            if (builder.Length > 0)
            {
                return builder.ToString().Substring(0, builder.Length - 1);
            }
            builder2.Append(" AND T0.FMATERIALID=T1.FMATERIALID ");
            return "T0.FMATERIALID";
        }

        private void GetUpdatePricePrecSql(List<SqlObject> lstSql)
        {
            List<SqlParam> list;
            string sql = "";
            if (base.Context.DatabaseType == DatabaseType.MS_SQL_Server)
            {
                sql = string.Format("/*dialect*/ \r\nMERGE INTO {0} IT \r\nUSING (SELECT TCU.FPRICEDIGITS,TCU.FAMOUNTDIGITS FROM (SELECT TOP 1 FCURRID FROM {1} WHERE FCURRID > 0) T1 \r\n        INNER JOIN T_BD_CURRENCY TCU ON T1.FCURRID = TCU.FCURRENCYID) IT2\r\nON (1 = 1)\r\nWHEN MATCHED THEN UPDATE \r\nSET IT.FPRICEPRE = IT2.FPRICEDIGITS,IT.FAMOUNTPRE = IT2.FAMOUNTDIGITS;", base.tmpRptTbl, base.tmpFullTbl);
            }
            else
            {
                sql = string.Format("/*dialect*/ \r\nMERGE INTO {0} IT \r\nUSING (SELECT TCU.FPRICEDIGITS,TCU.FAMOUNTDIGITS FROM (SELECT FCURRID FROM {1} WHERE FCURRID > 0 AND ROWNUM <= 1) T1 \r\n        INNER JOIN T_BD_CURRENCY TCU ON T1.FCURRID = TCU.FCURRENCYID) IT2\r\nON (1 = 1)\r\nWHEN MATCHED THEN UPDATE \r\nSET IT.FPRICEPRE = IT2.FPRICEDIGITS,IT.FAMOUNTPRE = IT2.FAMOUNTDIGITS", base.tmpRptTbl, base.tmpFullTbl);
            }
            //List<SqlObject> lstSqlObj = new List<SqlObject>();
            list = new List<SqlParam>(); 
                //list.Add(param);

                lstSql.Add(new SqlObject(sql, list));
           
            
        }

        private void GetSumRptUpdteQcAmount(List<SqlObject> lstSql)
        {
            if (base.canViewAmount)
            {
                string sql = string.Format(" UPDATE {0} SET FQCAMOUNT = TO_DECIMAL(FBASEQCPRICE*FBASEQCQTY,23,10)", base.tmpRptTbl);
                if ((!base.isNoRemainAmout && (base.qcSrcPrice != StockRptEnums.Enu_QcSrcPrice.KdQcAvgPrice)) && (base.qcSrcPrice != StockRptEnums.Enu_QcSrcPrice.KdQmAvgPrice))
                {
                    sql = sql + " WHERE  FBASEQCQTY<>0 ";
                }
                lstSql.Add(new SqlObject(sql, new List<SqlParam>()));
            }
        }

        private void GetUpdateIoBaseAmountSql(List<SqlObject> lstSql)
        {
            if (base.canViewAmount && (base.ioSrcPrice == StockRptEnums.Enu_IoSrcPrice.KdQcPrice))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine(string.Format(" UPDATE {0} SET  ", base.tmpRptTbl));
                builder.AppendLine(" FINAMOUNT = TO_DECIMAL(FBASEINQTY*FBASEQCPRICE,23,10), ");
                builder.AppendLine(" FBASEINPRICE = FBASEQCPRICE, ");
                builder.AppendLine(" FOUTAMOUNT  = TO_DECIMAL(FBASEOUTQTY*FBASEQCPRICE,23,10), ");
                builder.AppendLine(" FBASEOUTPRICE = FBASEQCPRICE ");
                lstSql.Add(new SqlObject(builder.ToString(), new List<SqlParam>()));
            }
        }

        private void GetJcQtyPriceAmountSql(List<SqlObject> lstSql)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format(" UPDATE {0} SET ", base.tmpRptTbl));
            builder.AppendLine(" FBASEJCQTY = FBASEQCQTY + FBASEINQTY - FBASEOUTQTY, ");
            builder.AppendLine(" FSTOCKJCQTY = FSTOCKQCQTY + FSTOCKINQTY - FSTOCKOUTQTY, ");
            builder.AppendLine(" FSECJCQTY = FSECQCQTY + FSECINQTY - FSECOUTQTY ");
            lstSql.Add(new SqlObject(builder.ToString(), new List<SqlParam>()));
            if (base.canViewAmount)
            {
                builder.Clear();
                builder.AppendFormat(" UPDATE {0} SET FJCAMOUNT = FQCAMOUNT + FINAMOUNT - FOUTAMOUNT ", base.tmpRptTbl);
                lstSql.Add(new SqlObject(builder.ToString(), new List<SqlParam>()));
                if (base.isNoRemainAmout)
                {
                    builder.Clear();
                    builder.AppendFormat(" UPDATE {0} SET FJCAMOUNT = 0 WHERE FBASEJCQTY=0 ", this.tmpRptTbl);
                    lstSql.Add(new SqlObject(builder.ToString(), new List<SqlParam>()));
                }
                builder.Clear();
                builder.AppendLine(string.Format(" UPDATE {0} SET  ", base.tmpRptTbl));
                builder.AppendLine(" FBASEJCPRICE = CASE WHEN FBASEJCQTY = 0 THEN 0 ELSE TO_DECIMAL(FJCAMOUNT/FBASEJCQTY,23,10) END, ");
                builder.AppendLine(" FSTOCKJCPRICE = CASE WHEN FSTOCKJCQTY = 0 THEN 0 ELSE TO_DECIMAL(FJCAMOUNT/FSTOCKJCQTY,23,10) END, ");
                builder.AppendLine(" FSECJCPRICE = CASE WHEN FSECJCQTY = 0 THEN 0 ELSE TO_DECIMAL(FJCAMOUNT/FSECJCQTY,23,10) END ");
                lstSql.Add(new SqlObject(builder.ToString(), new List<SqlParam>()));
            }
        }

        private void SetRptDate(string tableName)
        {
            List<SqlObject> lstSqlObj = new List<SqlObject>();
            StringBuilder builder = new StringBuilder();
            base.KSQL_SEQ = string.Format("ROW_NUMBER() OVER(ORDER BY {0} ) FIDENTITYID", " FMATERIALNO ASC ");
            builder.AppendFormat(" SELECT {0},", base.GetListPageFieldSql());
            builder.AppendLine(this.GetSumRptFieldSql());
            builder.AppendLine(string.Format(" , {0} INTO {1} FROM {2} {3}", new object[] { base.KSQL_SEQ, tableName, base.tmpRptTbl, base.moreFilterFormat }));
            lstSqlObj.Add(new SqlObject(builder.ToString(), new List<SqlParam>()));
            builder.Clear();
            DBUtils.ExecuteBatch(base.Context, lstSqlObj);
        }

        private string GetSumRptFieldSql()
        {
            return "\r\n FBASEUNITPRE,FSTOCKUNITPRE,FSECUNITPRE,FPRICEPRE,FAMOUNTPRE,\r\n FMATERIALID,FMATERIALNUMBER,FMATERIALNAME,FMATERIALMODEL,FERPCLSID,FMATERIALGROUP,FAUXPROPID,FAUXPROP,FLOTNO, \r\n FMATERIALTYPENAME,FMATERIALTYPEID, \r\n FSTOCKID,FSTOCKNUMBER,FSTOCKNAME,FSTOCKLOCID,FSTOCKLOC, \r\n FSTOCKSTATUSID,FSTOCKSTATUSNUMBER,FSTOCKSTATUSNAME,\r\n FKEEPERTYPEID,FKEEPERTYPENAME,FKEEPERID,FKEEPERNAME,FPRODUCEDATE,FEXPIRYDATE,FBOMID,FBOMNO,FMTONO,\r\n FBASEUNITID,FBASEUNITNAME,FSTOCKUNITID,FSTOCKUNITNAME,FSECUNITID,FSECUNITNAME, \r\n FBASEQCQTY,FBASEQCPRICE,FSTOCKQCQTY,FSTOCKQCPRICE,FSECQCQTY,FSECQCPRICE,FQCAMOUNT, \r\n FBASEINQTY,FBASEINPRICE,FSTOCKINQTY,FSTOCKINPRICE,FSECINQTY,FSECINPRICE,FINAMOUNT, \r\n FBASEOUTQTY,FBASEOUTPRICE,FSTOCKOUTQTY,FSTOCKOUTPRICE,FSECOUTQTY,FSECOUTPRICE,FOUTAMOUNT, \r\n FBASEJCQTY,FBASEJCPRICE,FSTOCKJCQTY,FSTOCKJCPRICE,FSECJCQTY,FSECJCPRICE,FJCAMOUNT ";
        }

        private void JNReBuildCurPageTable()
        {
            this.JNCreateDataTbl();
            this.JNCreateQcpTbl();

        }

        protected void JNCreateDataTbl()
        {
            this.tmpRptTbl = "TM_STK_RPTCOMMONRPT";
            string str = " ( \r\n     FGUID VARCHAR(36) NOT NULL DEFAULT(NEWID()) , \r\n     FISDELETE CHAR(1) NULL,\r\n     FORDERBY  INT NOT NULL DEFAULT(0), \r\n     FBASEUNITPRE INT NOT NULL DEFAULT(0), \r\n     FSTOCKUNITPRE INT NOT NULL DEFAULT(0), \r\n     FSECUNITPRE INT NOT NULL DEFAULT(0), \r\n     FPRICEPRE INT NOT NULL DEFAULT(0), \r\n     FAMOUNTPRE INT NOT NULL DEFAULT(0), \r\n     FSTOCKIO CHAR(1) NULL, \r\n     FIOPRICE DECIMAL(23,10)  NOT NULL DEFAULT(0), \r\n     FIOAMOUNT DECIMAL(23,10)  NOT NULL DEFAULT(0), \r\n     FSTOCKORGID  INT NOT NULL DEFAULT(0), \r\n     FSTOCKORGNUMBER  NVARCHAR(100)  NULL, \r\n     FSTOCKORGNAME  NVARCHAR(100)  NULL, \r\n     FOWNERTYPEID  VARCHAR(36) NOT NULL DEFAULT(' '), \r\n     FOWNERTYPENAME  NVARCHAR(100)  NULL, \r\n     FOWNERID  INT NOT  NULL DEFAULT(0), \r\n     FOWNERNUMBER  NVARCHAR(100)  NULL, \r\n     FOWNERNAME  NVARCHAR(255)  NULL, \r\n     FKEEPERTYPEID  VARCHAR(36) NOT NULL DEFAULT(' '), \r\n     FKEEPERTYPENAME  NVARCHAR(100)  NULL, \r\n     FKEEPERID  INT NOT  NULL DEFAULT(0), \r\n     FKEEPERNUMBER  NVARCHAR(100)  NULL, \r\n     FKEEPERNAME  NVARCHAR(255)  NULL, \r\n     FPRODUCEDATE  DATETIME  NULL, \r\n     FEXPIRYDATE  DATETIME  NULL, \r\n     FBOMID INT  NULL DEFAULT(0), \r\n     FBOMNO NVARCHAR(100)  NULL, \r\n\r\n     FMATERIALTYPEID INT NOT NULL DEFAULT(0), \r\n     FMATERIALTYPENAME  NVARCHAR(100)  NULL, \r\n     FMATERIALID  INT NOT  NULL DEFAULT(0), \r\n     FMATERIALNO  NVARCHAR(100)  NULL,    \r\n     FMATERIALNUMBER  NVARCHAR(100)  NULL, \r\n     FMATERIALNAME  NVARCHAR(255)  NULL, \r\n     FMATERIALMODEL  NVARCHAR(510)  NULL, \r\n     FMATERIALGROUP  NVARCHAR(255)  NULL,   \r\n     FERPCLSID  NVARCHAR(100)  NULL,       \r\n     FAUXPROPID  INT NOT NULL DEFAULT(0),   \r\n     FAUXPROP  NVARCHAR(1000)  NOT NULL DEFAULT(' '),    \r\n     FDATE  DATETIME  NULL, \r\n     FCREATEDATE  DATETIME  NULL,   \r\n     FLOTNO  NVARCHAR(255) NOT NULL DEFAULT(' '), \r\n     \r\n     FSTOCKID  INT NOT  NULL DEFAULT(0), \r\n     FSTOCKNUMBER NVARCHAR(100) NULL, \r\n     FSTOCKNAME  NVARCHAR(100)  NULL, \r\n\r\n     FDEPARTMENTID  INT NOT  NULL DEFAULT(0), --#部门ID\r\n     FDEPARTMENTNAME NVARCHAR(100) NULL,    --#部门名称\r\n     FSTOCKSTATUSID INT NOT NULL DEFAULT(0), \r\n     FSTOCKSTATUSNUMBER NVARCHAR(100) NULL, \r\n     FSTOCKSTATUSNAME NVARCHAR(100) NULL, \r\n     FSTOCKLOCID INT NOT NULL DEFAULT(0),   \r\n     FSTOCKLOC NVARCHAR(100) NULL,  \r\n     FSTOCKPOSNUMBER NVARCHAR(100) NULL,  \r\n     FSTOREURNUM DECIMAL(23,10) NULL DEFAULT(0),   \r\n     FSTOREURNOM DECIMAL(23,10) NULL DEFAULT(1),   \r\n\r\n     FFORMID  VARCHAR(36)  NULL,   \r\n     FBILLNAME  NVARCHAR(255)  NULL, \r\n     FBILLID INT NOT NULL DEFAULT(0), \r\n     FBILLSEQID INT NULL,\r\n     FBILLNO  NVARCHAR(100)  NULL, \r\n     FBILLTYPE  VARCHAR(36)  NULL, \r\n     FBILLTYPENAME  NVARCHAR(100)  NULL, \r\n     FENTRYTABLE VARCHAR(36)  NULL, \r\n     FBILLENTRYID  INT NOT NULL DEFAULT(0), \r\n     FMTONO NVARCHAR(255) NULL,     \r\n\r\n     FBASEUNITID  INT NOT NULL DEFAULT(0), \r\n     FSTOCKUNITID  INT NOT NULL DEFAULT(0), \r\n     FSECUNITID  INT NOT NULL DEFAULT(0),  \r\n     FBASEUNITNAME  NVARCHAR(100)  NULL, \r\n     FSTOCKUNITNAME  NVARCHAR(100)  NULL, \r\n     FSECUNITNAME  NVARCHAR(100)  NULL, \r\n\r\n     FBASEQCQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FBASEQCPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSTOCKQCQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSTOCKQCPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSECQCQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSECQCPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FQCAMOUNT  DECIMAL(23,10) NOT NULL DEFAULT(0),  \r\n\r\n     FBASEINQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FBASEINPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSTOCKINQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSTOCKINPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSECINQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSECINPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FINAMOUNT  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n\r\n     FBASEOUTQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FBASEOUTPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSTOCKOUTQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSTOCKOUTPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSECOUTQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSECOUTPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FOUTAMOUNT  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n\r\n     FBASEJCQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FBASEJCPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSTOCKJCQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSTOCKJCPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSECJCQTY  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FSECJCPRICE  DECIMAL(23,10) NOT NULL DEFAULT(0), \r\n     FJCAMOUNT  DECIMAL(23,10) NOT NULL DEFAULT(0),\r\n     FNOTE  NVARCHAR(1000) NOT NULL DEFAULT(' '),\r\n     FAPPROVEDATE  DATETIME  NULL) ";
            this.tmpRptTbl = this.CreateTemplateTable(this.Context, this.tmpRptTbl, str);
            base.tmpRptTbl = this.tmpRptTbl;
        }


        protected void JNCreateQcpTbl()
        {
            this.tmpQcpTbl = "TM_STK_RPTCOMMONQCP";
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(" ( ");
            builder.AppendLine("     FGUID VARCHAR(36) NULL, ");
            builder.AppendLine("     FIOPRICE DECIMAL(23,10) NULL DEFAULT(0), ");
            builder.AppendLine("     FQCAMOUNT DECIMAL(23,10) NULL DEFAULT(0), ");
            builder.AppendLine("     FDATE VARCHAR(36) NULL, ");
            builder.AppendLine("     FMATERIALID INT NULL DEFAULT(0), ");
            builder.AppendLine("     FLOTNO NVARCHAR(255) NULL DEFAULT(' '), ");
            builder.AppendLine("     FOWNERTYPEID VARCHAR(36) NULL DEFAULT(' '), ");
            builder.AppendLine("     FOWNERID INT NULL DEFAULT(0), ");
            builder.AppendLine("     FKEEPERTYPEID VARCHAR(36) NULL DEFAULT(' '), ");
            builder.AppendLine("     FKEEPERID INT NULL DEFAULT(0), ");
            builder.AppendLine("     FSTOCKORGID INT NULL DEFAULT(0), ");
            builder.AppendLine("     FSTOCKSTATUSID INT NULL DEFAULT(0), ");
            builder.AppendLine("     FAUXPROPID INT NULL DEFAULT(0), ");
            builder.AppendLine("     FSTOCKID INT NULL DEFAULT(0), ");
            builder.AppendLine("     FSTOCKLOCID INT NULL DEFAULT(0), ");
            builder.AppendLine("     FBOMID INT NULL DEFAULT(0), ");
            builder.AppendLine("     FPRODUCEDATE VARCHAR(36) NULL, ");
            builder.AppendLine("     FEXPIRYDATE VARCHAR(36) NULL ");
            builder.AppendLine(" ) ");
            this.tmpQcpTbl = this.CreateTemplateTable(this.Context, this.tmpQcpTbl, builder.ToString());
            base.tmpQcpTbl = this.tmpQcpTbl;
        }

        protected string CreateTemplateTable(Context cont, string tmpRptTbl, string str)
        {
            this.dropTemplateTable(cont, tmpRptTbl);
            string sql = string.Format("/*dialect*/ Create Table {0} {1}", tmpRptTbl, str);
            DBUtils.Execute(cont, sql);
            return tmpRptTbl;
        }

        protected void dropTemplateTable(Context cont, string tmpRptTbl)
        {
            string sql = string.Format("/*dialect*/ IF  EXISTS (SELECT * FROM SYSOBJECTS WHERE NAME='{0}')  \r\n drop Table {0} ", tmpRptTbl);
            DBUtils.Execute(cont, sql);
            
        }







    }
}
