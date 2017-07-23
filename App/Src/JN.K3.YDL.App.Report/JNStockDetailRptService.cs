using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Report;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.K3.FIN.App.Core;

namespace JN.K3.YDL.App.Report
{
    [Description("扩展-物料收发明细表服务端插件")]
    public class JNStockDetailRptService : Kingdee.K3.SCM.App.Stock.Report.StockDetailRpt
    {
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
            string strTempTable = Kingdee.K3.FIN.App.Core.AppServiceContext.DBService.CreateTemporaryTableName(this.Context);
            base.BuilderReportSqlAndTempTable(filter, strTempTable);
            string strSql = string.Format(@"/*dialect*/select t1.*,
                                          case when (ISNULL(t1.FBaseJCQty,0)<>0 or ISNULL(t1.FBaseOutQty,0)<>0 or ISNULL(t1.FBaseInQty,0)<>0) and t2.FIsMeasure='1' then 
                                          (case when ISNULL(t1.FBaseJCQty,0)<>0 then t1.FSecJCQty/t1.FBaseJCQty else
                                           (case when ISNULL(t1.FBaseOutQty,0)<>0 then t1.FSecOutQty/t1.FBaseOutQty else 
                                            (case when ISNULL(t1.FBaseInQty,0)<>0 then t1.FSecInQty/t1.FBaseInQty end ) end) end )
                                          else 0 end FJNUnitEnzyme,
                                          case when ISNULL(t2.FJNTonProperty,0)<>0  and t2.FIsMeasure='1' then t1.FSECQCQTY/(t2.FJNTonProperty*1000) else 0 end FJNTonProperty_QC,
                                          case when ISNULL(t2.FJNTonProperty,0)<>0  and t2.FIsMeasure='1' then t1.FSECINQTY/(t2.FJNTonProperty*1000) else 0 end FJNTonProperty_SR,
                                          case when ISNULL(t2.FJNTonProperty,0)<>0  and t2.FIsMeasure='1' then t1.FSECOUTQTY/(t2.FJNTonProperty*1000) else 0 end FJNTonProperty_FC,
                                          case when ISNULL(t2.FJNTonProperty,0)<>0  and t2.FIsMeasure='1' then t1.FSECJCQTY/(t2.FJNTonProperty*1000) else 0 end FJNTonProperty_JC
                                          into {0} from {1} as t1
                                          join T_BD_MATERIAL t2 on t1.FMaterialId=t2.FMaterialId", tableName, strTempTable);
            DBUtils.Execute(this.Context, strSql);
            Kingdee.K3.FIN.App.Core.AppServiceContext.DBService.DeleteTemporaryTableName(this.Context, new string[] { strTempTable });
        }
    }
}
