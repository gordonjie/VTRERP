using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.MFG
{
    /// <summary>
    /// 生产订单-审核插件
    /// </summary>
    [Description("生产订单-审核插件")]
    public class PrdMoAudit : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("Id");
            e.FieldKeys.Add("FBillHead");
            e.FieldKeys.Add("FTreeEntity");
        }

        public override void AfterExecuteOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            List<DynamicObject> data = e.DataEntitys.ToList();
            foreach (var item in data)//同步生产批号到配方单
            {
                long fid = Convert.ToInt64(item["Id"]);//主健值
                DynamicObjectCollection entry = item["TreeEntity"] as DynamicObjectCollection;
                if (entry == null || entry.Count == 0) continue;
                foreach (DynamicObject dataEntry in entry)
                {
                    long fentryId = Convert.ToInt64(dataEntry["Id"]);//分录Id
                    string sql = string.Format(@"/*dialect*/update A set A.FJNLOT=B.FLOT,A.FJNLOT_Text=B.FLOT_Text
                                 from T_PRD_PPBOM A inner join T_PRD_MOENTRY B on A.FMOENTRYID = B.FENTRYID
                                 where A.FMOID=B.FID and FMOID={0} and FMOENTRYID={1} ", fid, fentryId);
                    DBUtils.Execute(this.Context, sql);
                }
            }
        }
    }
}
