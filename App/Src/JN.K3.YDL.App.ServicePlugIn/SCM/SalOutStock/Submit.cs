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

namespace JN.K3.YDL.App.ServicePlugIn.SCM.SalOutStock
{
    [Description("销售出库单锁库插件专用")]
     
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
             string sql = string.Format(@" SELECT t.FBILLNO, t.FID,t1.FENTRYID,t1.FSEQ, FSTOCKORGID AS FSTOCKORGID,	FKEEPERTYPEID AS FKEEPERTYPEID,	FKEEPERID  AS FKEEPERID,
	                            t1.FOWNERTYPEID AS FOWNERTYPEID,	t1.FOWNERID AS FOWNERID,	FSTOCKID  AS FSTOCKID,
	                            FSTOCKLOCID AS FSTOCKPLACEID,	TM.FMASTERID AS FMATERIALID,	FAUXPROPID AS FAUXPROPERTYID,
	                            FSTOCKSTATUSID AS FSTOCKSTATUSID,	FLOT AS FLOT,	FPRODUCEDATE AS FPRODUCTDATE,
	                            FEXPIRYDATE AS FVALIDATETO,	FBOMID AS FBOMID,	FMTONO AS FMTONO,FPROJECTNO AS FPROJECTNO,
	                            FBASEUNITQTY AS FBASEQTY,	FBASEUNITID AS FBASEUNITID,	FAUXUNITQTY AS FSECQTY,	FAUXUNITID AS FSECUNITID,FUNITID AS FUNITID,t1.FREALQTY AS FQTY 
	                             FROM T_SAL_OUTSTOCK t INNER JOIN T_SAL_OUTSTOCKENTRY t1 ON t.FID=t1.FID
                                inner join T_BD_MATERIAL TM on t1.FMATERIALID=TM.FMATERIALID 
                                 where t.FID in ({0})", string.Join(",", lstPkIds));
             Common.LockCommon.LockInventory(this.Context, this.OperationResult, sql, "SAL_OUTSTOCK");
             this.OperationResult.IsShowMessage = true;

        }

        

     
    }
}
