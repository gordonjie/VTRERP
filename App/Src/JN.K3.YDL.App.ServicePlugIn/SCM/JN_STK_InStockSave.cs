using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM
{
    /// <summary>
    /// 采购入库单保存服务端插件
    /// </summary>
    [Description("采购入库单保存服务端插件")]
    public class JN_STK_InStockSave : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBillNo");
            e.FieldKeys.Add("FStockOrgId");
            e.FieldKeys.Add("FLot");
            e.FieldKeys.Add("FMaterialId");
            e.FieldKeys.Add("FAuxPropId");
            e.FieldKeys.Add("FJNUnitEnzymes");
        }

        /// <summary>
        /// 增加操作校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            e.Validators.Add(new JN_STK_InStockSaveValidator());
        }
    }
}
