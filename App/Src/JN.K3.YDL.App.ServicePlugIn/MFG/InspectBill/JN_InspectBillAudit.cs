using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.ServiceHelper;


namespace JN.K3.YDL.App.ServicePlugIn.MFG.InspectBill
{
    /// <summary>
    /// 检验单保存或审核的服务插件：调用采购收料单的保存操作以便刷新批号主档的跟踪记录
    /// </summary>
    [Description("检验单保存或审核的服务插件：调用采购收料单的保存操作以便刷新批号主档的跟踪记录")]
    public class JN_InspectBillAudit : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 在检验单的保存或审核时，会把酶活相关数据反写到收料单，导致收料单上的酶活量跟批次号主档资料里面的跟踪信息对不上，这里重新保存一下收料单以便刷新批号主档资料
        /// </summary>
        /// <param name="e"></param>
        public override void AfterExecuteOperationTransaction( AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            if (e.DataEntitys.IsEmpty())
            {
                return;
            }

            string sql = @"/*dialect*/   
                        update t set t.FSECQTY =x.FACTRECEIVEQTY * x.FJNUnitEnzymes 
	                    from  T_QM_INSPECTBILL a   
	                    inner join T_QM_INSPECTBILLENTRY b on a.fid=b.fid
	                    inner join  T_QM_INSPECTBILLENTRY_LK c on b.FENTRYID =c.FENTRYID  and c.FRULEID ='QM_PURReceive2Inspect'  
	                    inner join  T_PUR_ReceiveEntry x on x.FENTRYID =c.fsid and x.fid =c.FSBILLID 
	                    inner join  T_PUR_Receive y on x.FID =y.fid  
	                    inner join T_BD_LOTMASTERBILLTRACE t on FBILLFORMID ='PUR_ReceiveBill'  and t.fbillid=x.FID and t.FBILLENTRYID =x.FENTRYID  
	                    inner join T_BD_LOTMASTER tt on t.FLOTID =tt.FLOTID 
	                    where  a.fid ={0} ;";

            List<string> sqlLst = new List<string>();
            FormMetadata  billTypeFormMeta =FormMetaDataCache.GetCachedFormMetaData(this.Context, "PUR_ReceiveBill");
            var dyType= billTypeFormMeta.BusinessInfo.GetDynamicObjectType();
            foreach (var item in e.DataEntitys)
            {
                sqlLst.Add(string.Format(sql, item["Id"])); 
            }
            if (sqlLst.Count > 0)
            {
                DBUtils.ExecuteBatch(this.Context, sqlLst, 50);
            }

            CacheUtil.ClearCache(this.Context.DBId, "PUR_ReceiveBill");
            CacheUtil.ClearCache(this.Context.DBId, "BD_BatchMainFile");
        }

        /// <summary>
        /// fid
        /// </summary>
        int id;
        /// <summary>
        /// 业务类型ID
        /// </summary>
        int typeid;
        //操作事物后触发
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (e.DataEntitys == null) return;
            foreach (var dataEntity in e.DataEntitys)
            {
                id = Convert.ToInt32(dataEntity["id"]);
                typeid = Convert.ToInt32(dataEntity["BUSINESSTYPE"]);
            }
            string sql = string.Format(@"/*dialect*/
                                       update T_QM_INSPECTBILLENTRY_A set FQCBUSINESSTYPE={0} where FID in ({1})", typeid, id);
            var data = DBUtils.Execute(this.Context, sql);
        }

    }
}
