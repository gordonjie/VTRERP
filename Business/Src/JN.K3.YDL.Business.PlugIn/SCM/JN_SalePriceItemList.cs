using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.K3.Core.SCM.Args;
using Kingdee.K3.SCM.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM
{
    [Description("销售价目表-列表插件")]
    public class JN_SalePriceItemList : AbstractListPlugIn
    {
        public override void PrepareFilterParameter(Kingdee.BOS.Core.List.PlugIn.Args.FilterArgs e)
        {
            base.PrepareFilterParameter(e);
            string str = string.Empty;
            string orgList = Convert.ToString(e.CustomFilter["OrgList"]);
            BusinessGroupDataIsolationArgs isolationArgs = new BusinessGroupDataIsolationArgs();
            isolationArgs.OrgIdKey = "FCreateOrgId";
            isolationArgs.PurchaseParameterKey = "GroupDataIsolation";
            isolationArgs.PurchaseParameterObject = "SAL_SystemParameter";
            isolationArgs.BusinessGroupKey = "FSaleGroupViewId";
            isolationArgs.OperatorType = "XSY";
            str = SCMCommon.GetfilterGroupDataIsolation(this, orgList, isolationArgs);
            if (!string.IsNullOrWhiteSpace(str))
            {
                e.AppendQueryFilter(str);
            }
        }
    }
}
