using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Report;
using Kingdee.K3.FIN.HS.App.Report;

namespace VTR.K3.YDL.App.Report.HS
{
    public class JNInoutstocksumRptservice :InOutStockSummaryService
    {
        public override void BuilderReportSqlAndTempTable(Kingdee.BOS.Core.Report.IRptParams filter, string tableName)
        {
             string teamptablename = tableName;
 	         base.BuilderReportSqlAndTempTable(filter, tableName);
             List<SummaryField> SummaryField = base.GetSummaryColumnInfo(filter);
             string sql = base.GetSummaryColumsSQL(SummaryField);
             var fss = filter.CustomParams;
        }
        
    }
}
