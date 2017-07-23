using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS;
using JN.K3.YDL.Core;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.App.Data;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.PickMtrl
{
    [Description("生产领料单锁库插件专用")]
     
    ///锁库无关逻辑不要写到此文件
    public class Submit : AbstractOperationServicePlugIn
    {
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
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
            //处理上游配方当的锁库逻辑
            //当前操作执行时，1.解锁上游配方单与本单关联的数量2.重算上游配方单的锁库数量字段
             string sql = string.Format(@"select TKE.FENTRYID as Id,TKE.FSUPPLYINTERID as FInvDetailID,TKH.FDEMANDENTRYID as BillDetailID,t4.FBASEACTUALQTY as FBASEQTY,0 as FSECQTY 
                                        from T_PLN_RESERVELINKENTRY TKE 
                                        inner join T_PLN_RESERVELINK TKH on TKE.FID = TKH.FID 
                                        inner join T_STK_INVENTORY TV on TKE.FSUPPLYINTERID = TV.FID and TKE.FSUPPLYFORMID = 'STK_Inventory'
                                        inner join T_PRD_PPBOMENTRY BP on TKH.FDEMANDENTRYID = BP.FEntryID 
                                        inner join T_PRD_PPBOM TB on BP.FID = TB.FID and TKH.FDEMANDBILLNO = TB.FBILLNO
										inner join (select t2.FSID, sum(t2.FBASEACTUALQTY) as FBASEACTUALQTY
										           FROM T_PRD_PickMtrl t INNER JOIN T_PRD_PickMtrlDATA t1 ON t.FID=t1.FID
													INNER JOIN T_PRD_PickMtrlDATA_LK t2 ON t1.FENTRYID=t2.FENTRYID
													where t2.FSTABLENAME='T_PRD_PPBOMENTRY' and t.FID in ({0}) 
													group by t2.FSID) t4 on BP.FENTRYID=t4.FSID
                                        where TKH.FSRCFORMID='PRD_PPBOM' ", string.Join(",", lstPkIds));
             
             DynamicObjectCollection dyUnLockInfo = DBUtils.ExecuteDynamicObject(this.Context, sql);
             //调用解锁接口
             Common.LockCommon.UnLockInventory(this.Context, dyUnLockInfo, "PRD_PPBOM");
             //重算单据上的锁库数量字段
             sql = string.Format(@"/*dialect*/update t set t.F_JN_BaseLockQty=isnull(TE.FQTY,0)
                                            from T_PRD_PPBOMENTRY_Q T left join 
                                            (select FDEMANDENTRYID,SUM(TKE.FBASEQTY) as FQTY from T_PLN_RESERVELINK TKH
                                              inner join  T_PLN_RESERVELINKENTRY TKE on TKE.FID = TKH.FID
                                            where FDEMANDFORMID='PRD_PPBOM'  
                                            group by FDEMANDENTRYID) TE on T.FEntryID=TE.FDEMANDENTRYID
                                            inner join (select t2.FSID
										           FROM T_PRD_PickMtrl t INNER JOIN T_PRD_PickMtrlDATA t1 ON t.FID=t1.FID
													INNER JOIN T_PRD_PickMtrlDATA_LK t2 ON t1.FENTRYID=t2.FENTRYID
													where t2.FSTABLENAME='T_PRD_PPBOMENTRY' and t.FID in ({0}) 
													) t4 on T.FENTRYID=t4.FSID ", string.Join(",", lstPkIds));

             DBUtils.Execute(this.Context, sql);

            //处理本单的锁库逻辑
              sql = string.Format(@"  SELECT t.FBILLNO, t.FID,t1.FENTRYID,t1.FSEQ, FStockOrgId AS FSTOCKORGID,	FKeeperTypeId AS FKEEPERTYPEID,	FKEEPERID  AS FKEEPERID,
	                            t2.FOwnerTypeId AS FOWNERTYPEID,	t2.FOWNERID AS FOWNERID,	FStockId  AS FSTOCKID,
	                            FStockLocId AS FSTOCKPLACEID,	TM.FMASTERID AS FMATERIALID,	FAUXPROPID AS FAUXPROPERTYID,
	                            FSTOCKSTATUSID AS FSTOCKSTATUSID,	FLOT AS FLOT,	FPRODUCEDATE AS FPRODUCTDATE,
	                            FEXPIRYDATE AS FVALIDATETO,	FBOMID AS FBOMID,	FMTONO AS FMTONO,FPROJECTNO AS FPROJECTNO,
	                            FBaseActualQty AS FBASEQTY,	FBASEUNITID AS FBASEUNITID,	FSecActualQty AS FSECQTY,	FSECUNITID AS FSECUNITID,FUNITID AS FUNITID,t1.FACTUALQTY AS FQTY 
	                             FROM T_PRD_PickMtrl t INNER JOIN T_PRD_PickMtrlDATA t1 ON t.FID=t1.FID
								 INNER JOIN T_PRD_PickMtrlDATA_A t2 ON t1.FENTRYID=t2.FENTRYID
                                inner join T_BD_MATERIAL TM on t1.FMATERIALID=TM.FMATERIALID 
                                WHERE t.FID in ({0})  ", string.Join(",", lstPkIds));
             Common.LockCommon.LockInventory(this.Context, this.OperationResult, sql, "PRD_PickMtrl");
             this.OperationResult.IsShowMessage = true;

        }

        

     
    }
}
