using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;



namespace VTR.K3.Bill.PlugIn
{
    [System.ComponentModel.Description("我的列表")]
    public class MyRequestList: AbstractListPlugIn

    {
        public override void PrepareFilterParameter(FilterArgs e)
        {
            base.PrepareFilterParameter(e);
            //string str = string.Format("FOBJECTTYPEID='{0}'", this.View.OpenParameter.FormId);
            string str = "";
            string customParameter = this.View.OpenParameter.GetCustomParameter("ListSet") as string;
            if (!string.IsNullOrWhiteSpace(customParameter) && customParameter.Equals("My", StringComparison.OrdinalIgnoreCase))
            {
                string str3 = string.Format(" FCREATORID={0}", base.Context.UserId);
                str = string.Format("{0}", str3);
                
            }
            /*if (!string.IsNullOrWhiteSpace(e.FilterString))
            {
                e.AppendQueryFilter(e.FilterString + " AND ");
                
            }*/
            //e.AppendQueryFilter(e.FilterString + str);
            e.AppendQueryFilter( str);

        }
    }
}
