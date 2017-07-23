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

namespace JN.K3.YDL.App.ServicePlugIn.SCM.Forecast
{
    /// <summary>
    /// 销售预测单反审核校验插件
    /// </summary>
    [Description("销售预测单反审核校验插件")]
    public class JNUnAuditValidator : AbstractValidator
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
                select b.Fid,c.FSeq
                from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_Forecast b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId 
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                inner join JN_T_SAL_ForecastEntity c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID and a.FAUXPROPID=c.FJNAUXPROP 
                where c.FBaseUnitID=a.FUnitID and a.FQTY-c.FBaseUnitQty<0");

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
                        , string.Format(@"第{0}行的结余数出现操作结果小于0,不能反审核!", item["FSeq"]));

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
