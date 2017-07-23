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
    [Description("配方单-锁库")]
    public class LockStock : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBillNo");
            e.FieldKeys.Add("FDOCUMENTSTATUS");
            e.FieldKeys.Add("FMaterialID2");
            
            
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
  
            foreach (DynamicObject dyn in e.DataEntitys)
            {
                if (dyn == null)
                    continue;
                //单据体按行锁库
                long entryId = this.Option.GetVariableValue<long>("EntryId", 0);
                //列表批量按单锁库
                List<long> entryIds = new List<long>();
                if (entryId > 0)
                {
                    entryIds.Add(entryId);
                }
                long pkId = Convert.ToInt64(dyn["id"]);


                DynamicObjectCollection dynEntrys = dyn["PPBomEntry"] as DynamicObjectCollection;
                if (dynEntrys == null || dynEntrys.Count < 1) return;
                string lockParams = Common.LockCommon.GetLockParams(dynEntrys, entryIds);
                string sql = string.Format(@"SELECT t.FBILLNO, t.FID,t1.FENTRYID,t1.FSEQ, FSUPPLYORG AS FSTOCKORGID,	'BD_KeeperOrg' AS FKEEPERTYPEID,	t.FPARENTOWNERID  AS FKEEPERID,
	                            t.FPARENTOWNERTYPEID AS FOWNERTYPEID,	t.FPARENTOWNERID AS FOWNERID,	FSTOCKID  AS FSTOCKID,
	                            FSTOCKLOCID AS FSTOCKPLACEID,	TM.FMASTERID AS FMATERIALID,	t1.FAUXPROPID AS FAUXPROPERTYID,
	                            FSTOCKSTATUSID AS FSTOCKSTATUSID,	FLOT AS FLOT,	FJNPRODUCEDATE AS FPRODUCTDATE,
	                            FJNEXPIRYDATE AS FVALIDATETO,	t1.FBOMID AS FBOMID,	FMTONO AS FMTONO,FPROJECTNO AS FPROJECTNO,
	                            FBaseMustQty-FBASESELPICKEDQTY AS FBASEQTY,	t1.FBASEUNITID AS FBASEUNITID,	0 AS FSECQTY,	FJNSECUNITID AS FSECUNITID,
								t1.FUNITID AS FUNITID,t1.FMUSTQTY-FSELPICKEDQTY AS FQTY 
	                             FROM T_PRD_PPBOM t INNER JOIN T_PRD_PPBOMENTRY t1 ON t.FID=t1.FID
								 inner join T_PRD_PPBOMENTRY_Q t2 on t1.FENTRYID=t2.FENTRYID
								 INNER JOIN T_PRD_PPBOMENTRY_C t3 ON t1.FENTRYID=t3.FENTRYID
                                inner join T_BD_MATERIAL TM on t1.FMATERIALID=TM.FMATERIALID 
                                            where FBaseMustQty-FBASESELPICKEDQTY>0 and T1.FEntryID in({0})", lockParams);;

                sql = string.Format(@" select ts.*,isnull(tk.FID,'') as FINVENTORYID
                                from ( {0} ) ts
                                LEFT JOIN T_STK_INVENTORY tk on ts.FSTOCKORGID=tk.FSTOCKORGID and tk.FKEEPERTYPEID=ts.FKEEPERTYPEID and tk.FKEEPERID=ts.FKEEPERID
                                        and tk.FOWNERTYPEID=ts.FOWNERTYPEID and tk.FOWNERID=ts.FOWNERID and tk.FSTOCKID=ts.FSTOCKID and tk.FSTOCKLOCID=ts.FSTOCKPLACEID and tk.FAUXPROPID=ts.FAUXPROPERTYID and
                                        tk.FSTOCKSTATUSID=ts.FSTOCKSTATUSID and tk.FLOT=ts.FLOT and tk.FBOMID=ts.FBOMID and tk.FMTONO=ts.FMTONO and tk.FPROJECTNO=ts.FPROJECTNO 
                                       
                                        and  tk.FBASEUNITID=ts.FBASEUNITID  and tk.FMATERIALID=ts.FMATERIALID  
                                inner join T_BD_MATERIALSTOCK MS on MS.FMATERIALID=ts.FMATERIALID and MS.FIsLockStock='1'
                                inner join T_BD_STOCK TSK on TSK.FSTOCKID=ts.FSTOCKID AND TSK.FALLOWLOCK='1'
                               
                                ", sql, "PRD_PPBOM");

                Common.LockCommon.LockInventory(this.Context, this.OperationResult, sql, "PRD_PPBOM");
               
                //更新单据上的锁库数量字段
                sql = string.Format(@"/*dialect*/update t set t.F_JN_BaseLockQty=TE.FQTY
                                            from T_PRD_PPBOMENTRY_Q T INNER join
                                            (select FDEMANDENTRYID,SUM(TKE.FBASEQTY) as FQTY from T_PLN_RESERVELINK TKH
                                              inner join  T_PLN_RESERVELINKENTRY TKE on TKE.FID = TKH.FID
                                            where FDEMANDFORMID='PRD_PPBOM'  
                                            group by FDEMANDENTRYID) TE on T.FEntryID=TE.FDEMANDENTRYID where t.FID={0} ", pkId);
                DBUtils.Execute(this.Context,sql);
                this.OperationResult.IsShowMessage = true;

               
            }
         }

       
    }

   
    
}
