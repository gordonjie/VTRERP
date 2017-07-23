using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.FIN.paybill
{
    /// <summary>
    /// 付款单审核插件
    /// </summary>
    [Description("付款单审核操作插件")]
    public class JN_Audit : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBillNo");
        }
}
