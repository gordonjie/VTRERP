using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;
using Kingdee.BOS;
using Kingdee.BOS.Util;
namespace JN.K3.YDL.App.ServicePlugIn.MFG.InspectBill
{
    [Description("检验单审核服务插件")]
    public  class Audit: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FLot");
            e.FieldKeys.Add("FSrcBillType0");
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            if (e.DataEntitys == null) //注意是批量操作的。。这里可能是单据也可能是列表 多行
            {
                return;
            }
            List<long> lstEntryIds = new List<long>();
            foreach (var dataEntity in e.DataEntitys)
            {
                DynamicObjectCollection dyObjs = dataEntity["Entity"] as DynamicObjectCollection;
                if (dyObjs == null && dyObjs.Count < 1)
                {
                    continue;//没有检查单明细跳出当前循环
                }
                foreach (var itemObj in dyObjs)
                {
                    if (Convert.ToString(itemObj["SrcBillType"]).EqualsIgnoreCase("SFC_OperationReport"))
                    {
                        lstEntryIds.Add(Convert.ToInt64(itemObj["Id"]));
                    }
           
                }
                
            }
            List<SqlObject> lstSqlObj = new List<SqlObject>();
            if (lstEntryIds.Count > 0)
            {
                List<SqlParam> lstParams = new List<SqlParam>();
                StringBuilder sbSql = new StringBuilder();
                sbSql.AppendLine(" UPDATE T_SFC_OPTRPTENTRY AS T0 SET (FLOT,FLOT_text)=");
                sbSql.AppendLine(" (SELECT T3.FLOT,T3.FLOT_text ");
                sbSql.AppendLine(" FROM T_SFC_OPTRPTENTRY T1");
                sbSql.AppendLine(" INNER JOIN T_QM_INSPECTBILLENTRY_A T2  ");
                sbSql.AppendLine(" ON T2.FSRCENTRYID=T1.FENTRYID ");
                sbSql.AppendLine(" INNER JOIN T_QM_INSPECTBILLENTRY T3  ");
                sbSql.AppendLine(" ON T3.FENTRYID=T2.FENTRYID ");
                sbSql.AppendLine(" INNER JOIN TABLE(fn_StrSplit(@FID,',',1)) t4 on t4.fid=T2.FENTRYID");
                sbSql.AppendLine(" WHERE T0.FENTRYID=T1.FENTRYID )");
                SqlParam param = new SqlParam("@FID", KDDbType.udt_inttable, lstEntryIds.Distinct().ToArray());
                lstParams.Add(param);
                lstSqlObj.Add(new SqlObject(sbSql.ToString(), lstParams));
                DBUtils.ExecuteBatch(this.Context, lstSqlObj);
            }
            base.EndOperationTransaction(e);
        }
    }
}
