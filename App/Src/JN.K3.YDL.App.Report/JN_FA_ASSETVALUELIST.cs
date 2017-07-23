using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.ComponentModel;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Util;
using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;

namespace JN.K3.YDL.App.Report
{
    /// <summary>
    /// 扩展-资产价值清单服务插件
    /// </summary>
    [Description("扩展-资产价值清单服务插件")]
    public class JN_FA_ASSETVALUELIST : Kingdee.K3.FIN.FA.App.Report.AssetFinListService
    {

        /// <summary>
        /// 设置表头
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader reportHeader = base.GetReportHeaders(filter);
            reportHeader.AddChild("F_JN_YDL_AllocUseDeptID_EXT", new LocaleValue("使用部门", base.Context.UserLocale.LCID));
            reportHeader.AddChild("F_JN_YDL_AllocUseDeptNumber_EXT", new LocaleValue("使用部门编码", base.Context.UserLocale.LCID));
            return reportHeader;
        }

        //private string[] strTempTables;
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
            sb.AppendFormat("select a.*,e.fname F_JN_YDL_AllocUseDeptID_EXT,d.fnumber F_JN_YDL_AllocUseDeptNumber_EXT into {0} from {1} a ", tableName, strTempTable);
            sb.AppendLine(" left join t_fa_card b on a.fnumber = b.fnumber ");
            sb.AppendLine(" inner join t_fa_allocation c on b.falterid = c.falterid ");
            sb.AppendLine(" left join t_bd_department d on c.fusedeptid = d.fdeptid ");
            sb.AppendLine(" inner join t_bd_department_l e on d.fdeptid = e.fdeptid ");
            DBUtils.Execute(this.Context, sb.ToString());
            //获取报表数据
            DataTable dt = this.GetData(tableName, 1, 100000000);
            if (dt != null && dt.Rows.Count > 0)
            {
                //清空报表数据
                string strSql = string.Format("delete {0}", tableName);
                DBUtils.Execute(this.Context, strSql);
                string temp = "";
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (temp != dt.Rows[i]["FNUMBER"].ToString())
                    {
                        temp = dt.Rows[i]["FNUMBER"].ToString();
                    }
                    else
                    {
                        dt.Rows[i]["FOriginalCost"] = 0;
                        dt.Rows[i]["FInputTax"] = 0;
                        dt.Rows[i]["FExpenseValue"] = 0;
                        dt.Rows[i]["FExpenseTax"] = 0;
                        dt.Rows[i]["FOrgVal"] = 0;
                        dt.Rows[i]["FACCUMDEPR"] = 0;
                        dt.Rows[i]["FNETVALUE"] = 0;
                        dt.Rows[i]["FACCUMDEVALUE"] = 0;
                        dt.Rows[i]["FVALUE"] = 0;
                        dt.Rows[i]["FRESIDUALVALUE"] = 0;
                        dt.Rows[i]["FPurchaseValue"] = 0;
                        dt.Rows[i]["FPurchaseDepr"] = 0;
                        dt.Rows[i]["FDeprValue"] = 0;
                        dt.Rows[i]["FCurYearDepr"] = 0;
                        dt.Rows[i]["FDeprRemain"] = 0;
                        //dt.Rows[i]["FLIFEPERIODS"] = 0;
                        //dt.Rows[i]["FUSEDPERIODS"] = 0;
                        //dt.Rows[i]["FDEPRPERIODS"] = 0;
                    }
                }
                //填充报表数据
                DBServiceHelper.BulkInserts(this.Context, "", "", dt);
            }


            AppServiceContext.DBService.DeleteTemporaryTableName(this.Context, new string[] { strTempTable });

        }
    }
}
