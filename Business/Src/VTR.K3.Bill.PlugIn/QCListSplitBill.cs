using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.List;
using Kingdee.K3.SCM.Purchase.Business.PlugIn;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.Core;
using Kingdee.K3.Core.MFG.PLN.Reserved;
using Kingdee.K3.Core.MFG.PLN.Reserved.ReserveArgs;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.K3.SCM.ServiceHelper;
using Kingdee.K3.SCM.Core;
using Kingdee.BOS.Core.BusinessFlow.ReserveLogic;
using Kingdee.K3.BD.ServiceHelper;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.K3.MFG.ServiceHelper;
using Kingdee.K3.SCM.Contracts;


namespace VTR.K3.Bill.PlugIn
{
    [System.ComponentModel.Description("检验单拆分")]
    public class QCListSplitBill : AbstractListPlugIn
    {
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            string str = e.Operation.Operation;
      
                        if (!(str == "SplitBill"))
                        {
                 
                            return;
                        }
                        else{
                        this.SplitBill();
                        }
                       
          }

        // Nested Types
     
        private struct CancelStatus
        {
            public const string NoInvalid = "A";
            public const string Invalid = "B";
        }

       
        private struct CloseStatus
        {
            public const string NoClose = "A";
            public const string Closeed = "B";
        }

      
        private struct Status
        {
            public const string ZanCun = "Z";
            public const string Create = "A";
            public const string Audit = "B";
            public const string Audited = "C";
            public const string ReAudit = "D";
        }



       
        private void SplitBill()
    {
        ListSelectedRowCollection rows = this.ListView.SelectedRowsInfo;
            if (rows.GetPrimaryKeyValues().Length != 1)
    {
        this.View.ShowErrMessage("", ResManager.LoadKDString("不允许同时对多张申请单进行拆分，请重新选择。", "004015030000121", Kingdee.BOS.Resource.SubSystemType.BOS, new object[0]), 0);
               
    }
    else if (((rows.GetEntryPrimaryKeyValues().Count<string>() <= 0) || (rows[0].EntryEntityKey == null)) || ObjectUtils.IsNullOrEmptyOrWhiteSpace(rows[0].EntryPrimaryKeyValue))
    {
        this.View.ShowMessage(ResManager.LoadKDString("请选择分录进行拆分！", "004015030002767", Kingdee.BOS.Resource.SubSystemType.BOS, new object[0]), 0);
    }
    else
    {
        QueryBuilderParemeter paremeter2 = new QueryBuilderParemeter();
        // paremeter2.set_FormId("QM_InspectBill");
        paremeter2.FormId = "QM_InspectBill";
        //paremeter2.set_SelectItems(SelectorItemInfo.CreateItems("FID,FDocumentStatus,FENTITY_FENTRYID,FIsMergeCancel,FIsSplitCancel "));
        paremeter2.SelectItems = SelectorItemInfo.CreateItems("FID,FDocumentStatus,FENTITY_FENTRYID");
        //paremeter2.set_FilterClauseWihtKey(string.Format("FENTITY_FENTRYID IN ({0}) ", string.Join(",", rows.GetEntryPrimaryKeyValues())));
        paremeter2.FilterClauseWihtKey = string.Format("FENTITY_FENTRYID IN ({0}) ", string.Join(",", rows.GetEntryPrimaryKeyValues()));
        QueryBuilderParemeter paremeter = paremeter2;
        int num = (from p in QueryServiceHelper.GetDynamicObjectCollection(base.Context, paremeter, null)
            where ((Convert.ToString(p["FDocumentStatus"]).Equals("A") || Convert.ToString(p["FDocumentStatus"]).Equals("D")))
            select p).Count<DynamicObject>();
        if (num == 0)
        {
            this.View.ShowErrMessage(ResManager.LoadKDString("请选择创建且正常状态的申请单分录进行拆分，当前选择的单据分录无效，请重新选择！", "004015030002770", Kingdee.BOS.Resource.SubSystemType.BOS, new object[0]), "", 0);
        }
        else
        {
            int pURReqEntryEntryRowNum = 4;
            /*IPurchaseService purchaseService = ServiceFactory.GetPurchaseService(base.Context);
            try
            {
                pURReqEntryEntryRowNum = purchaseService.GetPURReqEntryEntryRowNum(base.Context, Convert.ToInt64(rows[0].PrimaryKeyValue));
            }
            finally
            {
                ServiceFactory.CloseService(purchaseService);
            }*/

          
            if (num >= pURReqEntryEntryRowNum)
            {
                this.View.ShowErrMessage("", ResManager.LoadKDString("不允许对一张单据的所有记录行进行拆分。", "004015030000133", Kingdee.BOS.Resource.SubSystemType.BOS, new object[0]), 0);
            }
            else
            {
                List<string> list = new List<string>{
                rows[0].PrimaryKeyValue
            };
                if (PurchaseServiceHelper.GetNetworkCtrl(base.Context, list, "c5fe6a77fa584c4ea0962d9767d58e7f").Count<string>() > 0)
                {
                    this.View.ShowMessage(ResManager.LoadKDString("当前拆分操作与业务操作-“修改”冲突，请稍后再使用。", "004015030002773", Kingdee.BOS.Resource.SubSystemType.BOS, new object[0]), 0);
                }
                else
                {
                    var rules = ConvertServiceHelper.GetConvertRules(this.View.Context, "QM_InspectBill", "QM_InspectBill");
                    var rule = rules.FirstOrDefault(t =>t.IsDefault);


                    //ConvertRuleElement element = ConvertServiceHelper.GetConvertRules(this.View.Context, "QM_InspectBill", "QM_InspectBill").FirstOrDefault<ConvertRuleElement>(t => t.Key == "SplitBillConvertRule");
                    PurReqMergeOrSplitResult result = PurchaseServiceHelper.MergeOrSplitBill(base.Context, rule, this.ListView.BillBusinessInfo, rows, "S", "T_QM_INSPECTBILLENTRY", "FEntryID");
                    if (!result.IsSuccess)
                    {
                        this.View.ShowErrMessage(ResManager.LoadKDString("单据拆分失败! ", "004015030000136", Kingdee.BOS.Resource.SubSystemType.BOS, new object[0]) + result.ErrorMsg, "", 0);
                    }
                    else
                    {
                        List<ConvertSelectRow> list3 = new List<ConvertSelectRow>();
                        ReserveConvertArgs args = new ReserveConvertArgs();
                        foreach (DynamicObject obj2 in result.ResultEntity)
                        {
                            string str = Convert.ToString(obj2["BillNo"]);
                            string str2 = Convert.ToString(obj2["fFormId"]);
                            string str3 = Convert.ToString(obj2["Id"]);
                            DynamicObjectCollection objects2 = obj2["Reqentry"] as DynamicObjectCollection;
                            DateTime time = Convert.ToDateTime(obj2["ApplicationDate"]);
                            long num3 = Convert.ToInt64(obj2["APPLICATIONORGID_ID"]);
                            foreach (DynamicObject obj3 in objects2)
                            {
                                OriBillInfo info = new OriBillInfo();
                                //info.set_BillNo(str);
                                info.BillNo = str;
                                //info.set_EntryID(Convert.ToString(obj3.get_Item("ID")));
                                info.EntryID = Convert.ToString(obj3["ID"]);
                                info.EntryID = str2;
                                info.InterID = str3;
                                //info.set_InterID(str3);
                                ConvertSelectRow item = new ConvertSelectRow();
                               
                                item.SupplyBillInfo = info;
                                item.SupplyDate = new DateTime?(time);
                                item.SupplyBomID=Convert.ToInt64(obj3["BOMNoId_Id"]);
                                item.SupplyAuxpropID=Convert.ToInt64(obj3["AUXPROPID_Id"]);
                                item.SupplyMtoNO=Convert.ToString(obj3["MTONO"]);
                                item.SupplyOrgID=num3;
                                item.SupplyMaterialID=Convert.ToInt64(obj3["MaterialId_Id"]);
                                item.BaseSupplyUnitID=Convert.ToInt64(obj3["BaseUnitId_Id"]);
                                item.BaseSupplyQty=Convert.ToDecimal(obj3["FBaseUnitQty"]);
                                list3.Add(item);
                            }
                        }
                        args.SelectRows=list3;
                        MFGServiceHelperForSCM.ReserveLinkOnConvert(base.Context, args, null);
                        this.ListView.Refresh();
                        this.View.ShowNotificationMessage(ResManager.LoadKDString("单据拆分成功! 新单单据编码是", "004015030000136", Kingdee.BOS.Resource.SubSystemType.BOS, new object[0]) + result.ResultEntity[0]["BillNo"].ToString(), "", 0);
                    }
                }
            }
        }
        }
    }

 


    }
}
