using JN.BOS.Contracts;
using JN.K3.YDL.Contracts;
using Kingdee.K3.SCM.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.K3.SCM.App.Pur.ServicePlugIn.PurchaseOrder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.SCM.App.Utils;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.PurOrder
{
    [System.ComponentModel.Description("采购订单单据转换设置默认值")]
    public class JNConvertSetDefaultValueService : ConvertSetDefaultValueService
    {
        private BusinessInfo info;

        public override void AfterConvert(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.AfterConvertEventArgs e)
        {
            base.AfterConvert(e);
            //设置采购部门
            this.info = e.TargetBusinessInfo;
            Kingdee.K3.SCM.Contracts.ICommonService commonService = Kingdee.K3.SCM.Contracts.ServiceFactory.GetCommonService(base.Context);
            ExtendedDataEntity[] entityArray = e.Result.FindByEntityKey("FBillHead");
            BaseDataField Deptfield = this.info.GetField("FPurchaseDeptId") as BaseDataField;
            if ((entityArray != null) && (entityArray.Length > 0))
            {
                foreach (ExtendedDataEntity entity in entityArray)
                {
                    DynamicObject dataEntity = entity.DataEntity;
                    long num = Convert.ToInt64(dataEntity["PurchaseDeptId_id"]);
                    long orgId = Convert.ToInt64(dataEntity["PurchaseOrgId_id"]);
                    if (num <= 0L)
                    {
                        long num3 = commonService.GetUserOperatorId(base.Context, base.Context.UserId, orgId, "CGY");
                        long num4 = commonService.GetMyDepartment(base.Context, base.Context.UserId).FirstOrDefault<long>();
                        FieldUtils.SetBaseDataFieldValue(base.Context, Deptfield, dataEntity, num4);
                    }
                }
            }

        }
    }
}
