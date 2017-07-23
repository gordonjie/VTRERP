using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.App.Core;
using Kingdee.BOS.Core;
using Kingdee.BOS;
using Kingdee.BOS.Util;


namespace JN.K3.YDL.App.ServicePlugIn.UseForm
{
    [Description("配方单-反锁库")]
    public class UnLockStock : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBillNo");
      
            e.FieldKeys.Add("FDOCUMENTSTATUS");
 
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            if (e.DataEntitys == null) return;
            List<long> lstPkIds = new List<long>();
            foreach (DynamicObject billDataEntity in e.DataEntitys)
            {
                lstPkIds.Add(Convert.ToInt64(billDataEntity["Id"]));
            }
            if (lstPkIds == null || lstPkIds.Count == 0)
            {
                return;
            }
            //单据体按行锁库
            long entryId = this.Option.GetVariableValue<long>("EntryId", 0);
            //列表批量按单锁库
            List<long> entryIds = new List<long>();
            if (entryId > 0)
            {
                entryIds.Add(entryId);
            }

            string sql = string.Format(@"select TKE.FENTRYID as Id,TKE.FSUPPLYINTERID as FInvDetailID,TKH.FDEMANDENTRYID as BillDetailID,TKE.FBASEQTY as FBASEQTY,TKE.FSECQTY as FSECQTY 
                                        from T_PLN_RESERVELINKENTRY TKE 
                                        inner join T_PLN_RESERVELINK TKH on TKE.FID = TKH.FID 
                                        inner join T_STK_INVENTORY TV on TKE.FSUPPLYINTERID = TV.FID and TKE.FSUPPLYFORMID = 'STK_Inventory'
                                        inner join T_PRD_PPBOMENTRY BP on TKH.FDEMANDENTRYID = BP.FEntryID 
                                        inner join T_PRD_PPBOM TB on BP.FID = TB.FID and TKH.FDEMANDBILLNO = TB.FBILLNO
                                        where TKH.FSRCFORMID='PRD_PPBOM' ");
            //按行处理
            if (entryId > 0)
            {
                //entryIds.Add(entryId);
                sql = sql + string.Format(" AND BP.FENTRYID ={0}", entryId);
            }
            else
            {
                sql = sql + string.Format(" AND  BP.FID in({0}) ", string.Join(",", lstPkIds));
            }
            DynamicObjectCollection dyUnLockInfo = DBUtils.ExecuteDynamicObject(this.Context, sql);
            //调用解锁接口
            Common.LockCommon.UnLockInventory(this.Context, dyUnLockInfo, "PRD_PPBOM");
            //重算单据上的锁库数量字段
            sql = string.Format(@"/*dialect*/update t set t.F_JN_BaseLockQty=isnull(TE.FQTY,0)
                                            from T_PRD_PPBOMENTRY_Q T left join 
                                            (select FDEMANDENTRYID,SUM(TKE.FBASEQTY) as FQTY from T_PLN_RESERVELINK TKH
                                              inner join  T_PLN_RESERVELINKENTRY TKE on TKE.FID = TKH.FID
                                            where FDEMANDFORMID='PRD_PPBOM'  
                                            group by FDEMANDENTRYID) TE on T.FEntryID=TE.FDEMANDENTRYID");
            if (entryId > 0)
            {
                sql = sql + string.Format(" where t.FEntryID = {0};", entryId);
            }
            else
            {
                sql = sql + string.Format(" where t.FID in ({0});", string.Join(",", lstPkIds));
            }
            DBUtils.Execute(this.Context, sql);
        
            this.OperationResult.IsShowMessage = true;

         
         }

       
    }

   
    
}
