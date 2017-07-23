using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.App.Data;
using Kingdee.K3.FIN.CN.App.Report;

namespace VTR.K3.YDL.App.Report
{
    [Description("扩展-银行日记账服务端插件")]
    public class JNCN_BankJournal : BankJournal
    {
        public override void BuilderReportSqlAndTempTable(Kingdee.BOS.Core.Report.IRptParams filter, string tableName)
        {
            base.BuilderReportSqlAndTempTable(filter, tableName);
            //string sql = "";

            string sql = string.Format(@"/*dialect*/ update {0}  set Fdesc = t2.FBILLNUMBER from {1} as t1,
(select tb1.FBILLNO,FBILLNUMBER=STUFF((select ','+FBILLNUMBER from t_CN_BILLRECSETTLE tb2
where tb1.FBILLNO=tb2.FBILLNO
 FOR XML PATH('')), 1, 1, '')
from t_CN_BILLRECSETTLE tb1
group by tb1.FBILLNO) as t2
where t2.FBILLNO= t1.FBILLNO", tableName, tableName);
            DBUtils.Execute(this.Context, sql);
            sql = string.Format(@"/*dialect*/ update {0}  set Fdesc = t2.FBILLNUMBER from {1} as t1,
(select tb1.FBILLNO,FBILLNUMBER=STUFF((select ','+FBILLNUMBER from  T_CN_BILLPAYSETTLE tb2
where tb1.FBILLNO=tb2.FBILLNO
 FOR XML PATH('')), 1, 1, '')
from  T_CN_BILLPAYSETTLE tb1
group by tb1.FBILLNO) as t2
where t2.FBILLNO= t1.FBILLNO", tableName, tableName);
            DBUtils.Execute(this.Context, sql);
            // T_CN_BILLPAYSETTLE
        }
    }
}
