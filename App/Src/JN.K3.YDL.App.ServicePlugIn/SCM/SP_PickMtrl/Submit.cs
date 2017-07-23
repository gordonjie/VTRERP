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

namespace JN.K3.YDL.App.ServicePlugIn.SCM.SP_PickMtrl
{
    [Description("简单生产领料单锁库插件专用")]
     
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
           

            //处理本单的锁库逻辑
             string sql = string.Format(@"  SELECT t.FBILLNO, t.FID,t1.FENTRYID,t1.FSEQ, FStockOrgId AS FSTOCKORGID,	FKeeperTypeId AS FKEEPERTYPEID,	FKEEPERID  AS FKEEPERID,
	                            t1.FOWNERTYPEID AS FOWNERTYPEID,	t1.FOWNERID AS FOWNERID,	FStockId  AS FSTOCKID,
	                            FStockLocId AS FSTOCKPLACEID,	TM.FMASTERID AS FMATERIALID,	FAUXPROPID AS FAUXPROPERTYID,
	                            FSTOCKSTATUSID AS FSTOCKSTATUSID,	FLOT AS FLOT,	FPRODUCEDATE AS FPRODUCTDATE,
	                            FEXPIRYDATE AS FVALIDATETO,	FBOMID AS FBOMID,	FMTONO AS FMTONO,'' AS FPROJECTNO,
	                            FBaseActualQty AS FBASEQTY,	FBASEUNITID AS FBASEUNITID,	FSecActualQty AS FSECQTY,	FSECUNITID AS FSECUNITID,FUNITID AS FUNITID,t1.FACTUALQTY AS FQTY 
	                             FROM T_SP_PICKMTRL t INNER JOIN T_SP_PICKMTRLDATA t1 ON t.FID=t1.FID
								 inner join T_BD_MATERIAL TM on t1.FMATERIALID=TM.FMATERIALID 
                                WHERE t.FID in ({0})  ", string.Join(",", lstPkIds));
             Common.LockCommon.LockInventory(this.Context, this.OperationResult, sql, "SP_PickMtrl");
             this.OperationResult.IsShowMessage = true;

        }

        

     
    }
}
