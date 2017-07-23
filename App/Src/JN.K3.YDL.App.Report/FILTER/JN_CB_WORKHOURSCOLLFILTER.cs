using Kingdee.K3.FIN.CB.Business.PlugIn.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.App.Data;
using System.ComponentModel;

namespace JN.K3.YDL.App.Report.FILTER
{
    [Description("扩展-实际工时过滤表单插件")]
    public class JN_CB_WORKHOURSCOLLFILTER : CBCommonFilterEdit
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            string buttonkey = e.Key;
            if (buttonkey == "FBTNOK")
            {
                string sql = string.Format(@"/*dialect*/UPDATE t1 SET 
t1.ftotaltime=t2.fmacworktime+t2.FHRPREPARETIME+t2.FHRWORKTIME+t2.FMACPREPARETIME,t1.fmacworktime=t2.fmacworktime,t1.FHRWORKTIME=t2.FHRWORKTIME
FROM T_CB_WORKHOURSENTRY t1 
INNER JOIN T_PRD_MORPTENTRY t2 ON t1.FSRCENTRYID=t2.FENTRYID
where t1.FSRCBILLFORMID='SFC_OperationReport'");
                DBUtils.Execute(this.Context, sql);
            }
            base.AfterButtonClick(e);
        }
    }
}
