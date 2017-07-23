using JN.K3.YDL.App.ServicePlugIn.SCM.Purchase.Validator;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.Purchase
{
    /// <summary>
    /// 采购申请单-保存服务端插件
    /// </summary>
    [Description("采购申请单-保存服务端插件")]
    public class JN_RequisitionSave : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FMaterialId");
            e.FieldKeys.Add("FJNREQTY");
            e.FieldKeys.Add("FEntity");
        }

        /// <summary>
        /// 增加操作校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            RequisitionSaveValidator saveValidator = new RequisitionSaveValidator();
            saveValidator.EntityKey = "FBillHead";
            e.Validators.Add(saveValidator);
        }
    }
}
