using Kingdee.BOS.Core.Validation;
using System.Collections.Generic;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.Purchase.Validator
{
    /// <summary>
    /// 采购申请单-保存服务端校验插件
    /// </summary>
    public class RequisitionSaveValidator : AbstractValidator
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
                DynamicObjectCollection dataEntry = dataEntities[0].DataEntity["ReqEntry"] as DynamicObjectCollection;
                foreach (DynamicObject item in dataEntry)
                {
                    DynamicObject materil = item["MaterialId"] as DynamicObject;
                    if (materil == null) continue;
                    if (Convert.ToBoolean(materil["FIsMeasure"]) && Convert.ToDecimal(item["FJNREQTY"]) == 0)
                    {
                        validateContext.AddError(DataEntities[0].DataEntity,
                        new ValidationErrorInfo
                        (
                        "", DataEntities[0].DataEntity["Id"].ToString(), dataEntities[0].DataEntityIndex, 0,
                        "001",
                        string.Format("第{0}行为双计量物料时，需求酶活为必录字段",dataEntry.IndexOf(item)+1),
                        "保存提示"
                        ));
                    }
                    //辅助属性“具体描述名称”是否为空判断
                    DynamicObjectCollection auxMaterial = materil["MaterialAuxPty"] as DynamicObjectCollection;//物料辅助信息
                    foreach (DynamicObject item1 in auxMaterial)
                    {
                        string key = item1["AuxPropertyId_Id"].ToString();
                        if (key == "100002")
                        {
                            bool isUse = Convert.ToBoolean(item1["IsEnable1"]);
                            if (isUse)
                            {
                                key = "F" + key;
                                DynamicObject auxData = item["AuxpropId"] as DynamicObject;//辅助信息
                                if (auxData != null)
                                {
                                    if (auxData[key] == null || string.IsNullOrWhiteSpace(auxData[key].ToString()))
                                    {
                                        validateContext.AddError(DataEntities[0].DataEntity,
                                        new ValidationErrorInfo
                                        (
                                        "", DataEntities[0].DataEntity["Id"].ToString(), dataEntities[0].DataEntityIndex, 0,
                                        "001",
                                        string.Format("第{0}行为物料，启用了辅助属性'具体描述名称',该字段必填！", dataEntry.IndexOf(item) + 1),
                                        "保存提示"
                                        ));
                                    }
                                }
                            }
                        }  
                    }
                }               
            }
        }

    }
}
