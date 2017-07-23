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
    /// 采购订单-保存服务端插件
    /// </summary>
    [Description("采购订单-保存服务端插件")]
    public class JN_PurchaseOrderSave : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FMaterialId");
            e.FieldKeys.Add("FJNReqty");
            e.FieldKeys.Add("FPOOrderEntry");
        }

        /// <summary>
        /// 增加操作校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            PurchaseOrderSaveValidator saveValidator = new PurchaseOrderSaveValidator();
            saveValidator.EntityKey = "FBillHead";
            e.Validators.Add(saveValidator);
        }
    }
}
