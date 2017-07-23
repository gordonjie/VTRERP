using Kingdee.BOS;
using Kingdee.BOS.Contracts.Report;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System.Data;

namespace JN.K3.YDL.Report.PlugIn
{
    /// <summary>
    /// 应付账款逾期表
    /// </summary>
    [Description("应付账款逾期表")]
    public class YDL_PayableOverdue: SysReportBaseService
    {
        /// <summary>
        /// 初始化
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL; //简单账表
            this.ReportProperty.ReportName = new LocaleValue("应付账款逾期表");
            this.ReportProperty.PrimaryKeyFieldName = "FIDENTITYID";
            this.ReportProperty.IsGroupSummary = true;
            this.ReportProperty.IsUIDesignerColumns = false;//列显示方式

        }

        /// <summary>
        /// 取数
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="tableName"></param>
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            //base.BuilderReportSqlAndTempTable(filter, tableName);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" select ROW_NUMBER() OVER(ORDER BY t1.FID ) FIDENTITYID, t1.FBillno FBILLNO,t1.FDate FDATE,'0'FDOMAN, t1.FALLAMOUNTFOR FSUMTAX, ");
            sb.AppendLine(" t5.FWRITTENOFFAMOUNTFOR FPAY,t3.fnumber FNUMBER,t4.fname FNAME,t6.FOPENAMOUNTFOR FOPENAMOUNTFOR ,'0' FONEMONTH ,'0' FTWOMONTHS,'0' FTHREEMONTHS,'0' FMANYMONTHS");
            sb.AppendFormat(" into {0} from T_AP_PAYABLE t1 ", tableName);//应付单单据头
            sb.AppendLine(" left join T_AP_PAYMatchLogENTRY t2 on t2.ftargetbillno=t1.fbillno ");
            sb.AppendLine(" left join t_BD_Supplier t3 on t3.FSUPPLIERID=t1.FSUPPLIERID ");
            sb.AppendLine(" left join T_BD_SUPPLIER_L t4 on t4.FSUPPLIERID=t3.FSUPPLIERID ");
            sb.AppendLine(" left join T_AP_PAYABLEPLAN t5 on t1.fid=t5.fid  ");
            sb.AppendLine(" left join (select fid,sum(FOPENAMOUNTFOR)FOPENAMOUNTFOR from T_AP_PAYABLEENTRY group by fid) t6 on t1.fid=t6.fid   ");
            sb.AppendLine(" where 1=1 ");
            sb.AppendLine(" and t5.FWRITTENOFFAMOUNTFOR>t6.FOPENAMOUNTFOR ");

            DynamicObjectCollection dataCollection= DBUtils.ExecuteDynamicObject(this.Context, sb.ToString());
            sb.Clear();
            sb.AppendFormat("delete {0}", tableName);
            DBUtils.Execute(this.Context, sb.ToString());
            if (dataCollection != null && dataCollection.Count > 0)
            {
                DataTable dt = new DataTable();
                dt.TableName = tableName;
                dt.Columns.Add("FIDENTITYID", typeof(System.Int32)); //主键
                dt.Columns.Add("FBILLNO", typeof(System.String));//应付单号
                dt.Columns.Add("FDATE", typeof(System.String));//付款日期                      
                dt.Columns.Add("FDOMAN", typeof(System.String));//经办人
                dt.Columns.Add("FOPENAMOUNTFOR", typeof(System.Decimal));//预付金额
                dt.Columns.Add("FNAME", typeof(System.String));//供应商名称
                dt.Columns.Add("FONEMONTH", typeof(System.Char));//逾期1个月
                dt.Columns.Add("FTWOMONTHS", typeof(System.Char));//逾期2个月
                dt.Columns.Add("FTHREEMONTHS", typeof(System.Char));//逾期3个月
                dt.Columns.Add("FMANYMONTHS", typeof(System.Char));//逾期3个月以上

                dt.BeginLoadData();
                foreach (DynamicObject dataObject in dataCollection)
                {
                    if (dataObject["dataObject"] != null)
                    {
                        TimeSpan timeDiff = DateTime.Now.Subtract(Convert.ToDateTime(dataObject["FDATE"]));
                        if (timeDiff.TotalDays >= 30 && timeDiff.TotalDays < 60)
                        {
                            dataObject["FONEMONTH"] = '1';
                        }
                        else if (timeDiff.TotalDays >= 60 && timeDiff.TotalDays < 90)
                        {
                            dataObject["FTWOMONTHS"] = '1';
                        }
                        else if (timeDiff.TotalDays >= 90 && timeDiff.TotalDays < 120)
                        {
                            dataObject["FTHREEMONTHS"] = '1';
                        }
                        else
                        {
                            dataObject["FMANYMONTHS"] = '1';
                        }
                    }

                    dt.LoadDataRow(new object[] { dataObject["FIDENTITYID"],dataObject["FBILLNO"], dataObject["FDATE"], dataObject["FDOMAN"], dataObject["FOPENAMOUNTFOR"], dataObject["FNAME"],
                                                  dataObject["FONEMONTH"], dataObject["FTWOMONTHS"],dataObject["FTHREEMONTHS"],dataObject["FMANYMONTHS"] }, true);

                }

                dt.EndLoadData(); 

               // 批量插入到数据库
                if (dt != null && dt.Rows.Count >0)
                {
                    DBServiceHelper.BulkInserts(this.Context, string.Empty, string.Empty, dt);
                }
                
            }
        }

        ReportHeader header = new ReportHeader();
        /// <summary>
        /// 设置列名
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            setField();
            return header;
        }


        /// <summary>
        /// 设置列
        /// </summary>
        public void setField()
        {
            setField("FBILLNO", "应付单号");
            //setField("FDATE", "付款单号");
            setField("FDATE", "付款日期");
            setField("FDOMAN", "经办人");
            setField("FOPENAMOUNTFOR", "预付金额");
            setField("FNAME", "供应商名称");
            setField("FONEMONTH", "逾期1个月");
            setField("FTWOMONTHS", "逾期2个月");
            setField("FTHREEMONTHS", "逾期3个月");
            setField("FMANYMONTHS", "逾期3个月以上");
        }

        /// <summary>
        /// 单表头
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldText"></param>
        private void setField(string fieldName, string fieldText)
        {
            if (fieldName.IndexOf("MONTH") > 0)
            {
                header.AddChild(fieldName, new LocaleValue(fieldText, this.Context.UserLocale.LCID), SqlStorageType.Sqlchar, false);
            }
            else
            {
                header.AddChild(fieldName, new LocaleValue(fieldText, this.Context.UserLocale.LCID));
            }
            
        }

        /// <summary>
        /// 双表头
        /// </summary>
        /// <param name="headerName"></param>
        /// <param name="fieldDrys"></param>
        private void setField(string headerName, ref Dictionary<string, string> fieldDrys)
        {
            //合并表头
            ListHeader lsHeader = header.AddChild();
            lsHeader.Caption = new LocaleValue(headerName);
            foreach (KeyValuePair<string, string> fieldDry in fieldDrys)
            {
                lsHeader.AddChild(fieldDry.Key, new LocaleValue(fieldDry.Value, this.Context.UserLocale.LCID));
            }
            fieldDrys.Clear();
        }
    }
}
