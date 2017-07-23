using JN.BOS.Contracts;
using JN.K3.YDL.Core;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.SaleQuote
{
    /// <summary>
    /// 审核前校验器
    /// </summary>
    public class JN_AuditValidator:AbstractValidator
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public JN_AuditValidator()
        {
            this.EntityKey = "FQUOTATIONENTRY";
            this.TimingPointString = ",Audit,";
        }

        /// <summary>
        /// 初始化校验器
        /// </summary>
        /// <param name="dataEntities"></param>
        /// <param name="validateContext"></param>
        /// <param name="ctx"></param>
        public override void Initialize(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
        {
            base.Initialize(dataEntities, validateContext, ctx);            
        }

        /// <summary>
        /// 校验逻辑实现
        /// </summary>
        /// <param name="dataEntities"></param>
        /// <param name="validateContext"></param>
        /// <param name="ctx"></param>
        public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
        {
            if (dataEntities == null) return;

            foreach (var dataEntity in dataEntities)
            {
                var billTypeId = this.GetBillTypeId(validateContext.BusinessInfo, dataEntity.DataEntity);
                if (billTypeId.IsNullOrEmptyOrWhiteSpace())
                {
                    continue;
                }
                var billTypeParaObj = AppServiceContext.GetService<ISysProfileService>().LoadBillTypeParameter(ctx, validateContext.BusinessInfo.GetForm().Id, billTypeId);
                if (billTypeParaObj == null) continue;

                bool bSupportNoMtrlQuote = Convert.ToBoolean(billTypeParaObj["F_JN_NoMtrlIdQuotation"]);
                string strCreateMaterialPoint = Convert.ToString(billTypeParaObj["F_JN_MtrlCreateTimePoint"]);
                bool bAutoSyncToPriceLis = Convert.ToBoolean(billTypeParaObj["F_JN_AutoSyncToPriceList"]);
                //销售报价单不检验BOM版本
                //if (billTypeId == "565802889cb9fb")
                //{
                //    if (bAutoSyncToPriceLis && (long)dataEntity["BomId_Id"] == 0)
                //    {
                //        validateContext.AddError(dataEntity.DataEntity, new ValidationErrorInfo("FBomId",
                //                Convert.ToString(((DynamicObject)(dataEntity.DataEntity.Parent))["Id"]),
                //                dataEntity.DataEntityIndex,
                //                dataEntity.RowIndex,
                //                "JN-001",
                //                string.Format("审核失败：第{0}行BOM版本不能为空！", dataEntity.RowIndex + 1),
                //                "金蝶提示"));
                //        continue;
                //    }
                //}
                //如果参数是支持无物编，且物料为非审核时生成的情况下，则要求物料编码字段必须有值
                if(bSupportNoMtrlQuote
                    && !strCreateMaterialPoint.EqualsIgnoreCase("2"))
                {
                    if(dataEntity["MaterialId_Id"].IsEmptyPrimaryKey())
                    {
                        validateContext.AddError(dataEntity.DataEntity, new ValidationErrorInfo("FMaterialId", 
                            Convert.ToString(((DynamicObject)(dataEntity.DataEntity.Parent))["Id"]), 
                            dataEntity.DataEntityIndex, 
                            dataEntity.RowIndex,
                            "JN-002", 
                            string.Format("审核失败：第{0}行物料编码不能为空！",dataEntity.RowIndex+1), 
                            "金蝶提示"));
                        continue;
                    }
                }                
                if (bSupportNoMtrlQuote
                    && strCreateMaterialPoint.EqualsIgnoreCase("2") )
                {
                    //销售报价单不检验产品名称
                    if (dataEntity["F_JN_ProductName"].IsEmptyPrimaryKey() && billTypeId == "565802889cb9fb")
                    {
                        validateContext.AddError(dataEntity.DataEntity, new ValidationErrorInfo("F_JN_ProductName",
                            Convert.ToString(((DynamicObject)(dataEntity.DataEntity.Parent))["Id"]),
                            dataEntity.DataEntityIndex,
                            dataEntity.RowIndex,
                            "JN-003",
                            string.Format("审核失败：第{0}行产品名称不能为空！", dataEntity.RowIndex+1),
                            "金蝶提示"));
                        continue;
                    }
                    //产品组别没有配置对应物料模板
                    var billTypeParaTplMtrlRows = billTypeParaObj["QuoteMtrlTplEntity"] as DynamicObjectCollection;
                    var matchTplMtrlRowObj = billTypeParaTplMtrlRows.FirstOrDefault(o => (long)o["F_JN_MtrlGroupId_Id"] == (long)dataEntity["F_JN_MtrlGroupId_Id"]);
                    if (matchTplMtrlRowObj == null && billTypeId == "565802889cb9fb")
                    {
                        validateContext.AddError(dataEntity.DataEntity, new ValidationErrorInfo("F_JN_ProductName",
                            Convert.ToString(((DynamicObject)(dataEntity.DataEntity.Parent))["Id"]),
                            dataEntity.DataEntityIndex,
                            dataEntity.RowIndex,
                            "JN-004",
                            string.Format("审核失败：第{0}行产品组别未配置对应的模板物料，导致自动生成物料失败！", dataEntity.RowIndex+1),
                            "金蝶提示"));
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// 获取单据类型Id
        /// </summary>
        /// <param name="info"></param>
        /// <param name="dataEntity"></param>
        /// <returns></returns>
        private string GetBillTypeId(BusinessInfo info, DynamicObject dataEntity)
        {
            DynamicObject rootObj=dataEntity;
            do
            {
                rootObj = dataEntity.Parent as DynamicObject;
            } while (rootObj.Parent != null);
            var billTypeField = info.GetBillTypeField();
            if(billTypeField!=null)
            {
                return billTypeField.RefIDDynamicProperty.GetValue<string>(rootObj);
            }
            return null;
        }

    }
}
