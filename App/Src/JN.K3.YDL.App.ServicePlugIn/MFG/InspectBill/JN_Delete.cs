using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.MFG.InspectBill
{
    /// <summary>
    /// 检验单删除服务插件
    /// </summary>
    [Description("检验单删除服务插件")]   
    public class JN_Delete : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 上游单据（采购收料单）编号
        /// </summary>
        DynamicObjectCollection billNoDynamic = null;

        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("Id");
        }

        /// <summary>
        /// 调用操作事物前触发
        /// </summary>
        /// <param name="e"></param>
        public override void BeginOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            List<DynamicObject> data = e.DataEntitys.ToList();
            foreach (var item in data)
            {
                long fid = Convert.ToInt64(item["Id"]);
                string sql = string.Format(@"select  FSRCBILLNO from T_QM_INSPECTBILLENTRY_A 
	                        where FID={0} and FSRCBILLTYPE='PUR_ReceiveBill'
	                        group by FSRCBILLNO", fid);
                billNoDynamic = DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text,null);
            }
        }

        /// <summary>
        /// 调用操作事物后触发
        /// </summary>
        /// <param name="e"/>
        public override void EndOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            foreach(var obj in billNoDynamic)
            {
                string billNo = obj["FSRCBILLNO"].ToString();
                string sql = string.Format(@"select a.FROUTEID,a.FTID from t_bf_Instanceentry a
	                        inner join T_PUR_ReceiveEntry b on a.fsid =b.FENTRYID 
	                        inner join T_PUR_Receive c on c.fid =b.fid 
	                        where a.FSTABLENAME='T_PUR_ReceiveEntry' 
	                        and c.FBILLNO like '{0}' and FTTABLENAME='T_QM_INSPECTBILLENTRY_A' ", billNo);
                DynamicObjectCollection billDynamic = DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, null);//单据转换联系
                foreach (var item in billDynamic)//不存在联系即删除
                {
                    string FROUTEID = item["FROUTEID"].ToString();
                    string FTID = item["FTID"].ToString();
                    string deleteSql = string.Format(@"if not exists( select 1 from T_QM_INSPECTBILLENTRY_A where FENTRYID={0}  )
                                       delete  t_bf_Instanceentry  where FROUTEID='{1}'", FTID, FROUTEID);
                    DBUtils.Execute(this.Context, deleteSql);
                }
            }           
        }
    }
}
