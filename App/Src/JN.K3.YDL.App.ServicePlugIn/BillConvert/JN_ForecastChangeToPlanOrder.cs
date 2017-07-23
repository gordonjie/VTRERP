using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.BillConvert
{
    [Description("销售预测变更单到计划订单的转换")]
    public class JN_ForecastChangeToPlanOrder : AbstractConvertPlugIn
    {
        public override void AfterConvert(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.AfterConvertEventArgs e)
        {
            base.AfterConvert(e);

            ExtendedDataEntity[] entityArray = e.Result.FindByEntityKey("FbillHead");

            if (entityArray == null || entityArray.Count() <= 0)
            {
                return;
            }

            List<long> lstMaterialID = new List<long>();

            foreach (ExtendedDataEntity entity in entityArray)
            {
                if (!lstMaterialID.Contains(Convert.ToInt64(entity.DataEntity["MaterialId_Id"])))
                {
                    lstMaterialID.Add(Convert.ToInt64(entity.DataEntity["MaterialId_Id"]));
                }
            }

            if (lstMaterialID.Count() <= 0)
            {
                return;
            }

            SqlParam param = new SqlParam("@FID", KDDbType.udt_inttable, lstMaterialID.ToArray());

            string strSql = string.Format(@"select a.FID,a.FMaterialId from T_ENG_BOM a
                                          INNER JOIN TABLE(fn_StrSplit(@FID,',',1)) b on a.FMaterialId=b.FID ");

            DynamicObjectCollection dycBomCollections = DBUtils.ExecuteDynamicObject(this.Context, strSql, null, null, CommandType.Text, new SqlParam[] { param });

            if (dycBomCollections == null || dycBomCollections.Count() <= 0)
            {
                return;
            }

            foreach (ExtendedDataEntity entity in entityArray)
            {
                DynamicObject dycBom = entity["BomId"] as DynamicObject;
                if (dycBom == null || Convert.ToInt64(dycBom["Id"]) == 0)
                {
                    DynamicObject dycSelect = dycBomCollections.Where(o => Convert.ToInt64(o["FMaterialId"]) == Convert.ToInt64(entity.DataEntity["MaterialId_Id"])).FirstOrDefault();
                    if (dycSelect != null)
                    {
                        entity.DataEntity["BomId_Id"] = dycSelect["FID"];
                    }
               
                }
                Kingdee.BOS.ServiceHelper.DBServiceHelper.LoadReferenceObject(this.Context, new DynamicObject[] { entity.DataEntity }, e.TargetBusinessInfo.GetDynamicObjectType());
            }


        }
    }
}
