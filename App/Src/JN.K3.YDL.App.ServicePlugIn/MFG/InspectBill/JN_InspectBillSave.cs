using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.MFG.InspectBill
{
    /// <summary>
    /// 检验单保存服务端插件
    /// </summary>
    [Description("检验单保存服务端插件")]
    public class JN_InspectBillSave : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 增加操作校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            e.Validators.Add(new JN_InspectBillAduitValidator());
        }
    }
}
