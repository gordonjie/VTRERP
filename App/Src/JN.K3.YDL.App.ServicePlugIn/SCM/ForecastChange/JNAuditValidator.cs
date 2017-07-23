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

namespace JN.K3.YDL.App.ServicePlugIn.SCM.ForecastChange
{
    /// <summary>
    /// 销售预测单变更单审核校验插件
    /// </summary>
    [Description("销售预测单变更单审核校验插件")]
    public class JNAuditValidator : AbstractValidator
    {
        public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
        {
            if (dataEntities == null)
            {
                return;
            }
            Field billNoField = validateContext.BusinessInfo.GetBillNoField();

            if (dataEntities != null && dataEntities.Count() > 0)
            {
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
                from JN_T_SAL_ForecastChange a
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on a.Fid=tb.Fid
                inner join JN_T_SAL_ForecastChangeEntry b on a.FID=b.FID 
                where not exists (select 1 from JN_T_SAL_ForecastBack c where a.FJNSALEORGID=c.FSALEORGID and a.FJNSALERID=c.FSALERID
                                                                        and a.FJNSaleDeptId=c.FSaleDeptId
                                                                        and b.FJNMATERIALID=c.FMATERIALID and b.FJNAUXPROP=c.FAUXPROPID
                                                                        and b.FJNBaseUnitID=c.FUnitID)
                and a.FDirection='B'
                union all
                select b.FID,c.FEntryID,c.FSeq
                from JN_T_SAL_ForecastBack a          
                inner join JN_T_SAL_ForecastChange b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId 
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                inner join JN_T_SAL_ForecastChangeEntry c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID 
                and a.FAUXPROPID=c.FJNAUXPROP and c.FJNBaseUnitID=a.FUnitID
                where b.FDirection='B' and a.FQTY-c.FJNBaseUnitQty<0");

                DynamicObjectCollection docForecast = DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });

                if (docForecast == null || docForecast.Count() <= 0)
                {
                    return;
                }

                foreach (var data in dataEntities)
                {
                    List<DynamicObject> docSelect = docForecast.Where(p => Convert.ToInt64(data.DataEntity["ID"]) == Convert.ToInt64(p["FID"])).ToList();

                    if (docSelect == null || docSelect.Count() <= 0)
                    {
                        continue;
                    }

                    foreach (var item in docSelect)
                    {
                        AddMsg(validateContext, data, billNoField.Key
                            , string.Format(@"第{0}行审核之后结余数小于0,不能审核!", item["FSeq"]));
                    }
                }
            }
        }

        private void AddMsg(ValidateContext validateContext, ExtendedDataEntity entity, string displayToFieldKey, string msg)
        {
            ValidationErrorInfo errorInfo = new ValidationErrorInfo(displayToFieldKey, entity.DataEntity["Id"].ToString(), entity.DataEntityIndex, entity.RowIndex, "???", msg, Convert.ToString(entity.DataEntity["FBillNo"]), ErrorLevel.Error);
            validateContext.AddError(entity.DataEntity, errorInfo);
        }


    }
}
