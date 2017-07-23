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

namespace JN.K3.YDL.App.ServicePlugIn.SCM.TransferDirect
{
    [Description("直接调拨单解锁库存插件")]
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
             string sql = string.Format(@"select TKE.FENTRYID as Id,TKE.FSUPPLYINTERID as FInvDetailID,TKH.FDEMANDENTRYID as BillDetailID,TKE.FBASEQTY as FBASEQTY,TKE.FSECQTY as FSECQTY 
                                        from T_PLN_RESERVELINKENTRY TKE 
                                        inner join T_PLN_RESERVELINK TKH on TKE.FID = TKH.FID 
                                        inner join T_STK_INVENTORY TV on TKE.FSUPPLYINTERID = TV.FID and TKE.FSUPPLYFORMID = 'STK_Inventory'
                                        inner join T_STK_STKTRANSFERINENTRY BP on TKH.FDEMANDENTRYID = BP.FEntryID 
                                        inner join T_STK_STKTRANSFERIN TB on BP.FID = TB.FID and TKH.FDEMANDBILLNO = TB.FBILLNO
                                        where TKH.FSRCFORMID='STK_TransferDirect'
										 AND BP.FID in({0}) ", string.Join(",", lstPkIds));
             DynamicObjectCollection dyUnLockInfo= DBUtils.ExecuteDynamicObject(this.Context, sql);
             Common.LockCommon.UnLockInventory(this.Context, dyUnLockInfo, "STK_TransferDirect");
             this.OperationResult.IsShowMessage = true;

        }

        

     
    }
}
