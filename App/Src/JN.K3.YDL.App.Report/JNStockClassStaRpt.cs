using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Report;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.Report
{
    [Description("扩展-物料收发分类统计表服务端插件")]
    public class JNStockClassStaRpt : Kingdee.K3.SCM.App.Stock.Report.StockClassStaRpt
    {
        public override void Initialize()
        {
            base.Initialize();
            //SetDecimalControl("FJNUnitEnzyme");
            //SetDecimalControl("FJNTonProperty_RK");
            //SetDecimalControl("FJNTonProperty_CK");
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
            reportHeader.AddChild("FJNTonProperty_RK", new LocaleValue("标吨&入库合计", base.Context.UserLocale.LCID));
            reportHeader.AddChild("FJNTonProperty_CK", new LocaleValue("标吨&出库合计", base.Context.UserLocale.LCID));
            return reportHeader;
        }

        public override void BuilderReportSqlAndTempTable(Kingdee.BOS.Core.Report.IRptParams filter, string tableName)
        {
            //把之前账表的数据放入新建的临时表
            string strTempTable = AppServiceContext.DBService.CreateTemporaryTableName(this.Context);
            base.BuilderReportSqlAndTempTable(filter, strTempTable);
            string strSql = string.Format(@"/*dialect*/select t1.*,
                                          case when (ISNULL(t1.FBaseQty_rk,0)<>0 or ISNULL(t1.FBaseQty_ck,0)<>0) and t2.FIsMeasure='1' then 
                                          (case when ISNULL(t1.FBaseQty_rk,0)<>0 then CONVERT(varchar(50),CONVERT(float,CAST(t1.FAuxQty_rk/t1.FBaseQty_rk as decimal(18,4)))) 
                                          else CONVERT(varchar(50),CONVERT(float,CAST(t1.FAuxQty_ck/t1.FBaseQty_ck as decimal(18,4)))) end)
                                          else '' end FJNUnitEnzyme,
                                          case when ISNULL(t2.FJNTonProperty,0)<>0  and t2.FIsMeasure='1' then CONVERT(varchar(50),CONVERT(float,CAST(t1.FAuxQty_rk/(t2.FJNTonProperty*1000) as decimal(18,4)))) else '' end FJNTonProperty_RK,
                                          case when ISNULL(t2.FJNTonProperty,0)<>0  and t2.FIsMeasure='1' then CONVERT(varchar(50),CONVERT(float,CAST(t1.FAuxQty_ck/(t2.FJNTonProperty*1000) as decimal(18,4)))) else '' end FJNTonProperty_CK
                                          into {0} from {1} as t1
                                          join T_BD_MATERIAL t2 on t1.FMaterialId=t2.FMaterialId", tableName, strTempTable);
            DBUtils.Execute(this.Context, strSql);
            AppServiceContext.DBService.DeleteTemporaryTableName(this.Context, new string[] { strTempTable });
        }
    }
}
