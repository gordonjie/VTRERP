using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.K3.Core.SCM.Args;
using Kingdee.K3.SCM.Business;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.GZL
{
    [System.ComponentModel.Description("报告列表")]
    public class ReportList : AbstractListPlugIn
    {
        public override void PrepareFilterParameter(FilterArgs e)

        {
            string str = string.Empty;
            string userid=string.Empty;
            string orgList = Convert.ToString(e.CustomFilter["OrgList"]);
            BusinessGroupDataIsolationArgs isolationArgs = new BusinessGroupDataIsolationArgs
            {
                OrgIdKey = "FCorrespondOrgId",
                PurchaseParameterKey = "GroupDataIsolation",
                PurchaseParameterObject = "PUR_SystemParameter",
                //BusinessGroupKey = "FStockGroupId",
                OperatorType = "WHY"
            };
            str = SCMCommon.GetfilterGroupDataIsolation(this, orgList, isolationArgs);
            userid = base.Context.UserId.ToString();
            var billno = this.ListView.Model.GetValue("Fbillno");
            string customParameter = this.View.OpenParameter.GetCustomParameter("ListSet") as string;
            if (!string.IsNullOrWhiteSpace(customParameter) && customParameter.Equals("My", StringComparison.OrdinalIgnoreCase))
            {

                string str3 = string.Format(" (charindex('{0}',F_VTR_ActorText)>0 or FCREATORID={1})", userid, base.Context.UserId);
                if (!str.IsNullOrEmptyOrWhiteSpace())
                {
                    str = string.Format("{0} and {1}", str, str3);
                }
                else
                {
                    str = str3;
                }
            }
            if (!str.IsNullOrEmptyOrWhiteSpace())
            {
                e.AppendQueryFilter(str);
            }
        }
    }
}
