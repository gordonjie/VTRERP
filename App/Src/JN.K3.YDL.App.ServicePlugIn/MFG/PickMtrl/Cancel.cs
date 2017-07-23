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
    [Description("生产领料单解锁库存插件")]
    ///锁库无关逻辑不要写到此文件
    public class Cancel : AbstractOperationServicePlugIn
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
            //处理本单解锁逻辑
            string newsql = string.Format(@"select TKE.FENTRYID as Id,TKE.FSUPPLYINTERID as FInvDetailID,TKH.FDEMANDENTRYID as BillDetailID,TKE.FBASEQTY as FBASEQTY,TKE.FSECQTY as FSECQTY 
                                        from T_PLN_RESERVELINKENTRY TKE 
                                        inner join T_PLN_RESERVELINK TKH on TKE.FID = TKH.FID 
                                        inner join T_STK_INVENTORY TV on TKE.FSUPPLYINTERID = TV.FID and TKE.FSUPPLYFORMID = 'STK_Inventory'
                                        inner join T_PRD_PickMtrlDATA BP on TKH.FDEMANDENTRYID = BP.FEntryID 
                                        inner join T_PRD_PickMtrl TB on BP.FID = TB.FID and TKH.FDEMANDBILLNO = TB.FBILLNO
                                        where TKH.FSRCFORMID='PRD_PickMtrl'
										 AND BP.FID in({0})  ", string.Join(",", lstPkIds));
            DynamicObjectCollection dyUnLockInfo = DBUtils.ExecuteDynamicObject(this.Context, newsql);
            Common.LockCommon.UnLockInventory(this.Context, dyUnLockInfo, "PRD_PickMtrl");
        }

       
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
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
            //当前操作(反审核,撤销)执行时，1.重新锁定上游配方单与本单关联的数量2.重算上游配方单的锁库数量字段
            if (this.FormOperation.Operation.EqualsIgnoreCase("cancel") || this.FormOperation.Operation.EqualsIgnoreCase("unaudit"))
            {
                string sql = string.Format(@"SELECT t.FBILLNO, t.FID,t1.FENTRYID,t1.FSEQ, FSUPPLYORG AS FSTOCKORGID,	'BD_KeeperOrg' AS FKEEPERTYPEID,	t.FPARENTOWNERID  AS FKEEPERID,
	                                        t.FPARENTOWNERTYPEID AS FOWNERTYPEID,	t.FPARENTOWNERID AS FOWNERID,	FSTOCKID  AS FSTOCKID,
	                                        FSTOCKLOCID AS FSTOCKPLACEID,	TM.FMASTERID AS FMATERIALID,	t1.FAUXPROPID AS FAUXPROPERTYID,
	                                        FSTOCKSTATUSID AS FSTOCKSTATUSID,	FLOT AS FLOT,	FJNPRODUCEDATE AS FPRODUCTDATE,
	                                        FJNEXPIRYDATE AS FVALIDATETO,	t1.FBOMID AS FBOMID,	FMTONO AS FMTONO,FPROJECTNO AS FPROJECTNO,
	                                        t4.FBASEACTUALQTY AS FBASEQTY,	t1.FBASEUNITID AS FBASEUNITID,	0 AS FSECQTY,	FJNSECUNITID AS FSECUNITID,
	                                        t1.FUNITID AS FUNITID,t4.FBASEACTUALQTY AS FQTY 
	                                            FROM T_PRD_PPBOM t INNER JOIN T_PRD_PPBOMENTRY t1 ON t.FID=t1.FID
		                                        inner join T_PRD_PPBOMENTRY_Q t2 on t1.FENTRYID=t2.FENTRYID
		                                        INNER JOIN T_PRD_PPBOMENTRY_C t3 ON t1.FENTRYID=t3.FENTRYID
                                              inner join T_BD_MATERIAL TM on t1.FMATERIALID=TM.FMATERIALID 
		                                        inner join (select t2.FSID, sum(t2.FBASEACTUALQTY) as FBASEACTUALQTY FROM T_PRD_PickMtrl t INNER JOIN T_PRD_PickMtrlDATA t1 ON t.FID=t1.FID
                                        INNER JOIN T_PRD_PickMtrlDATA_LK t2 ON t1.FENTRYID=t2.FENTRYID
                                        where t2.FSTABLENAME='T_PRD_PPBOMENTRY' and t.FID in ({0})
                                        group by t2.FSID) t4 on t1.FENTRYID=t4.FSID", string.Join(",", lstPkIds));
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
            }

            this.OperationResult.IsShowMessage = true;
        }
      

        

     
    }
}
