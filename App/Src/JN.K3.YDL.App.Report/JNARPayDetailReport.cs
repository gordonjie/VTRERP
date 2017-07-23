using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Util;
using JN.K3.YDL.Core;
using Kingdee.BOS;
using JN.K3.YDL.Core;


namespace JN.K3.YDL.App.Report
{
    /// <summary>
    /// 应收款明细表服务端插件
    /// </summary>
    [Description("扩展-应收款明细表服务端插件")]
    public class JNARPayDetailReport: Kingdee.K3.FIN.AR.App.Report.ARDetailReportService
    {
        /// <summary>
        ///// 设置表头
        ///// </summary>
        ///// <param name="filter"></param>
        ///// <returns></returns>
        //public override ReportHeader GetReportHeaders(IRptParams filter)
        //{
        //    ReportHeader reportHeader = base.GetReportHeaders(filter);
        //    reportHeader.AddChild("F_JN_YDL_FDATAVALUE_EXT", new LocaleValue("地区", base.Context.UserLocale.LCID));
        //    reportHeader.AddChild("F_JN_YDL_FGROUPCUSTID_EXT", new LocaleValue("集团编码", base.Context.UserLocale.LCID));
        //    reportHeader.AddChild("F_JN_YDL_FNAME_EXT", new LocaleValue("集团名称", base.Context.UserLocale.LCID));
        //    return reportHeader;
        //}

        /// <summary>
        /// 扩展取值逻辑
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="tableName"></param>
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            //创建临时表,用于存放扩展字段数据           
            string strTempTable = AppServiceContext.DBService.CreateTemporaryTableName(this.Context);

            //获取原报表数据
            base.BuilderReportSqlAndTempTable(filter, strTempTable);

            //组合数据,回写原报表
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" select a.*,c.FNUMBER F_JN_YDL_FGROUPCUSTID_EXT,d.FNAME F_JN_YDL_FNAME_EXT,e.FDATAVALUE F_JN_YDL_FDATAVALUE_EXT ");
            sb.AppendFormat(" into {0} from {1} a ", tableName, strTempTable);
            sb.AppendLine(" left join T_BD_CUSTOMER b on  a.FCONTACTUNITID =b.FCUSTID and FCONTACTUNITTYPE='BD_Customer' ");
            sb.AppendLine(" left join T_BD_CUSTOMER c on  b.FGROUPCUSTID =c.FCUSTID ");
            sb.AppendLine(" left join T_BD_CUSTOMER_L d on c.FCUSTID = d.FCUSTID ");
            sb.AppendLine(" left join T_BAS_ASSISTANTDATAENTRY_L e on b.FPROVINCIAL = e.FENTRYID ");
            DBUtils.Execute(this.Context, sb.ToString());

            AppServiceContext.DBService.DeleteTemporaryTableName(this.Context, new string[] { strTempTable });
        }


    }
}
