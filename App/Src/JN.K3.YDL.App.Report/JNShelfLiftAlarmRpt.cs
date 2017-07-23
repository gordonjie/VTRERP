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
    [Description("扩展-保质期预警服务端插件")]
    public class JNShelfLiftAlarmRpt : Kingdee.K3.SCM.App.Stock.Report.ShelfLiftAlarmRpt
    {
        //public override void Initialize()
        //{
        //    base.Initialize();
        //    DecimalControlField field = new DecimalControlField
        //    {
        //        ByDecimalControlFieldName = "FJNUnitEnzyme",
        //        DecimalControlFieldName = "FPRECISION"
        //    };
        //    base.ReportProperty.DecimalControlFieldList.Add(field);
        //    DecimalControlField field2 = new DecimalControlField
        //    {
        //        ByDecimalControlFieldName = "FJNTonProperty",
        //        DecimalControlFieldName = "FPRECISION"
        //    };
        //    base.ReportProperty.DecimalControlFieldList.Add(field2);
        //}

        public override Kingdee.BOS.Core.Report.ReportHeader GetReportHeaders(Kingdee.BOS.Core.Report.IRptParams filter)
        {
            ReportHeader reportHeader = base.GetReportHeaders(filter);
            reportHeader.AddChild("FJNUnitEnzyme", new LocaleValue("单位酶活量", base.Context.UserLocale.LCID));
            reportHeader.AddChild("FJNTonProperty", new LocaleValue("标吨", base.Context.UserLocale.LCID));
            return reportHeader;
        }

        public override void BuilderReportSqlAndTempTable(Kingdee.BOS.Core.Report.IRptParams filter, string tableName)
        {
            //把之前账表的数据放入新建的临时表
            string strTempTable = AppServiceContext.DBService.CreateTemporaryTableName(this.Context);
            base.BuilderReportSqlAndTempTable(filter, strTempTable);
            string strSql = string.Format(@"/*dialect*/select t1.*,case when ISNULL(t1.FBASEQTY,0)<>0 and t2.FIsMeasure='1'  then CONVERT(varchar(50),CONVERT(float,CAST(t1.FSECQTY/t1.FBASEQTY as decimal(18,4)))) else '' end FJNUnitEnzyme,
                                          case when ISNULL(t2.FJNTonProperty,0)<>0  and t2.FIsMeasure='1' then CONVERT(varchar(50),CONVERT(float,CAST(t1.FSECQTY/(t2.FJNTonProperty*1000) as decimal(18,4)))) else '' end FJNTonProperty
                                          into {0} from {1} as t1
                                          join T_BD_MATERIAL t2 on t1.FMaterialId=t2.FMaterialId", tableName, strTempTable);
            DBUtils.Execute(this.Context, strSql);
            AppServiceContext.DBService.DeleteTemporaryTableName(this.Context, new string[] { strTempTable });
        }
    }
}
