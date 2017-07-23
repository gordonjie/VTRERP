using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.SaleOrder
{
    /// <summary>
    /// 销售订单审核校验插件
    /// </summary>
    [Description("销售订单审核校验插件")]
    public class JN_AuditValidator : AbstractValidator
    {
        public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
        {
            Field billNoField = validateContext.BusinessInfo.GetBillNoField();

            if (dataEntities == null || dataEntities.Count() <= 0)
            {
                return;
            }

            List<long> lstFids = new List<long>();

            foreach (var data in dataEntities)
            {
                lstFids.Add(Convert.ToInt64(data.DataEntity["ID"]));
            }

            if (lstFids.Count() <= 0)
            {
                return;
            }

            SqlParam param = new SqlParam("@FID", KDDbType.udt_inttable, lstFids.ToArray());

            string sql = string.Format(@"                
                select a.FID,b.FEntryID,b.FSeq
                from T_SAL_ORDER a
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on a.Fid=tb.Fid
                inner join T_SAL_ORDERENTRY b on a.FID=b.FID 
                inner join t_BD_Stock d on b.FSTOCKID_MX=d.FStockId
                where not exists (select 1 from JN_T_SAL_ForecastBack c where a.FSALEORGID=c.FSALEORGID and c.FSALERID=a.FSALERID 
                                                                        and a.FSaleDeptId=c.FSALEDEPTID 
                                                                        and b.FMATERIALID=c.FMATERIALID and b.FAUXPROPID=c.FAUXPROPID 
                                                                        and b.FBaseUnitID=c.FUnitID)
                and d.FMasterId in (100313,100328)
                union all
                select b.Fid,c.FEntryID,c.FSeq
                from JN_T_SAL_ForecastBack a
                inner join T_SAL_ORDER b on a.FSALEORGID=b.FSALEORGID and a.FSALERID=b.FSALERID 
                and a.FSaleDeptId=b.FSALEDEPTID 
                inner join T_SAL_ORDERENTRY c on b.FID=c.FID and a.FMATERIALID=c.FMATERIALID 
                and a.FAUXPROPID=c.FAUXPROPID and c.FBASEUNITID=a.FUnitID
                inner join t_BD_Stock d on c.FSTOCKID_MX=d.FStockId
                where a.FQTY-c.FBASEUNITQTY<0 and d.FMasterId in (100313,100328)");

            DynamicObjectCollection docChecks = DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });

            if (docChecks == null || docChecks.Count() <= 0)
            {
                return;
            }

            foreach (var data in dataEntities)
            {
                List<DynamicObject> docSelect = docChecks.Where(p => Convert.ToInt64(data.DataEntity["ID"]) == Convert.ToInt64(p["FID"])).ToList();

                if (docSelect == null || docSelect.Count() <= 0)
                {
                    continue;
                }

                foreach (var item in docSelect)
                {
                    AddMsg(validateContext, data, billNoField.Key
                        , string.Format(@"第{0}行的结余数出现操作结果小于0,不能审核,请先做销售预测变更单进行调整结余数!", item["FSeq"]));

                }
            }

        }


        private void AddMsg(ValidateContext validateContext, ExtendedDataEntity entity, string displayToFieldKey, string msg)
        {
            ValidationErrorInfo errorInfo = new ValidationErrorInfo(displayToFieldKey, entity.DataEntity["Id"].ToString(), entity.DataEntityIndex, entity.RowIndex, "???", msg, Convert.ToString(entity.DataEntity["BillNo"]), ErrorLevel.Error);
            validateContext.AddError(entity.DataEntity, errorInfo);
        }
    }
}
