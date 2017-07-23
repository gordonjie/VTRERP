using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kingdee.BOS.Core;
using Kingdee.BOS;
using System.Data;
using Kingdee.BOS.App.Data;

namespace JN.K3.YDL.App.ServicePlugIn.MFG.InspectBill
{
    /// <summary>
    /// 检验单提交服务端校验插件
    /// </summary>
    [Description("检验单提交服务端校验插件")]
    public class JN_InspectBillSubmitValidator : AbstractValidator
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public JN_InspectBillSubmitValidator()
        {
            this.EntityKey = "FBillHead";
            this.TimingPointString = ",Submit,";
        }

        /// <summary>
        /// [物料]字段的标识：单据上的  物料 字段
        /// </summary>
        string matFldKey = "FMaterialId";

        /// <summary>
        /// [单位酶活]字段的标识：单据上的  单位酶活 字段
        /// </summary>
        string EnzNoFldKey = "FJNUnitEnzymes";

        public override void Validate(ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
        {
            if (dataEntities == null || dataEntities.Length == 0)
            {
                return;
            }

            List<long> dictErrMaterialId = new List<long>();

            //取所有物料
            List<long> listMaterialId = new List<long>();
            foreach (ExtendedDataEntity entityObj in dataEntities)
            {
                DynamicObjectCollection collection = (DynamicObjectCollection)entityObj["Entity"];

                //取酶活量小于等于于零的
                var checkRows = collection.Where(f => Convert.ToDecimal(f["FJNUnitEnzymes"]) <= 0).ToList();
                if (checkRows == null || checkRows.Count() <= 0)
                {
                    continue;
                }

                foreach (DynamicObject rowObj in checkRows)
                {
                    listMaterialId.Add((long)rowObj["MaterialId_Id"]);
                }
            }
            if (listMaterialId.Count > 0)
            {
                string sql = "   select a.FMATERIALID from T_BD_MATERIAL a where exists (select 1 from TABLE(fn_StrSplit(@FMATERIALID, ',',1)) t where t.FID=a. FMATERIALID  and a.FISMEASURE='1' )  ";
                SqlParam param = new SqlParam("@FMATERIALID", KDDbType.udt_inttable, listMaterialId.Distinct().ToArray());
                using (IDataReader dr = DBUtils.ExecuteReader(this.Context, sql, param))
                {
                    while (dr.Read())
                    {
                        if (dr != null)
                            dictErrMaterialId.Add(Convert.ToInt64(dr["FMATERIALID"]));
                    }
                }
            }
            foreach (ExtendedDataEntity entityObj in dataEntities)
            {
                DynamicObjectCollection collection = (DynamicObjectCollection)entityObj["Entity"];
                foreach (DynamicObject rowObj in collection)
                {
                    if (dictErrMaterialId.Contains((long)rowObj["MaterialId_Id"]))
                    {
                        ValidationErrorInfo errinfo = new ValidationErrorInfo("FMATERIALID", Convert.ToString(entityObj.DataEntity["Id"]),
                            entityObj.DataEntityIndex, Convert.ToInt32(rowObj["Id"]), "SubmitValidator", "第" + Convert.ToString(rowObj["Seq"]) + "行启用双计量，单位酶活必须大于0", "校验失败", ErrorLevel.Error);
                        validateContext.AddError(entityObj, errinfo);
                    }
                }
            }
        }
    }
}
