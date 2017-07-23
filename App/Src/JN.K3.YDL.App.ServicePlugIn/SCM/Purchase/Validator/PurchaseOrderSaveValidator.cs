using Kingdee.BOS.Core.Validation;
using System.Collections.Generic;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.Purchase.Validator
{
    /// <summary>
    /// 采购订单-保存服务端校验插件
    /// </summary>
    class PurchaseOrderSaveValidator : AbstractValidator
    {
        /// <summary>
        /// 校验逻辑实现
        /// </summary>
        /// <param name="dataEntities"></param>
        /// <param name="validateContext"></param>
        /// <param name="ctx"></param>
        public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
        {
            if (dataEntities == null) return;
            if (dataEntities != null && dataEntities.Length > 0)
            {
                DynamicObjectCollection dataEntry = dataEntities[0].DataEntity["POOrderEntry"] as DynamicObjectCollection;
                foreach (DynamicObject item in dataEntry)
                {
                    DynamicObject materil = item["MaterialId"] as DynamicObject;
                    if (materil == null) continue;
                    if (Convert.ToBoolean(materil["FIsMeasure"]) && Convert.ToDecimal(item["FJNReqty"]) == 0)
                    {
                        validateContext.AddError(DataEntities[0].DataEntity,
                        new ValidationErrorInfo
                        (
                        "", DataEntities[0].DataEntity["Id"].ToString(), dataEntities[0].DataEntityIndex, 0,
                        "001",
                        string.Format("第{0}行为双计量物料时，需求酶活为必录字段", dataEntry.IndexOf(item) + 1),
                        "保存提示"
                        ));
                    }
                }
            }
        }

    }
}
