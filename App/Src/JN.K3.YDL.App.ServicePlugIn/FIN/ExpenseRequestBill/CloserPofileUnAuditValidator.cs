using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.FIN.ExpenseRequestBill
{
    /// <summary>
    /// 发票反审核服务端校验插件
    /// </summary>
    [Description("发票反审核服务端校验插件")]
    public class CloserPofileUnAuditValidator : AbstractValidator
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public CloserPofileUnAuditValidator()
        {
            this.EntityKey = "FBillHead";
            this.TimingPointString = ",UnAudit,";
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
            string billFormKey = dataEntities[0].DataEntity["FFormId"].ToString();
            string billClass = "";
            switch (billFormKey)
            {
                case "IV_PURCHASEOC":
                    billClass = "AP";//采购普通发票
                    break;
                case "IV_PURCHASEIC":
                    billClass = "AP";//采购增值税专用发票
                    break;
                case "IV_SALESOC":
                    billClass = "AR";//销售普通发票
                    break;
                case "IV_SALESIC":
                    billClass = "AR";//销售增值税专用发票
                    break;
                default:
                    break;
            }            
            foreach (var dataEntity in dataEntities)
            {
                int orgId = Convert.ToInt32(dataEntity["SETTLEORGID_Id"]);//结算组织
                DateTime billDate = Convert.ToDateTime(dataEntity["Date"]);//业务日期
                string sql = string.Format(@"SELECT FORGID, FSTARTDATE, FENDDATE 
                            FROM T_AP_CLOSEPROFILE T1 
                            WHERE ((NOT EXISTS (SELECT 1 FROM T_AP_CLOSEPROFILE T2 
                            WHERE (((T2.FID > T1.FID) AND T1.FORGID = T2.FORGID) 
                            AND T1.FCATEGORY = T2.FCATEGORY)) AND FCATEGORY = '{0}') 
                            AND FOrgID ={1} )", billClass, orgId);
                DynamicObjectCollection returnData = DBUtils.ExecuteDynamicObject(ctx, sql);//结帐时间查询
                if (returnData == null || returnData.Count == 0)
                {
                    continue;
                }
                DateTime closeDate = Convert.ToDateTime(returnData[0]["FENDDATE"]);//结帐日期
                if (billDate <= closeDate)
                {
                    validateContext.AddError(dataEntity.DataEntity,
                        new ValidationErrorInfo("FBillNo",
                           Convert.ToString(dataEntity.DataEntity["Id"]),
                           dataEntity.DataEntityIndex,
                           dataEntity.RowIndex,
                           "JN- CloserPofile-002",
                           string.Format("反审核失败：当前单据已结帐，不能进行反审核操作！"),
                           ""));
                    continue;
                }


            }
        }



    }
}
