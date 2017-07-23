using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.MFG
{
    [Description("检验单列表插件")]
    public class InspectBillList : AbstractListPlugIn
    {
        public override void AfterDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string operation = e.Operation.Operation;
            if (operation != "Splita") return;
            this.SplitBill();
            e.OperationResult.IsShowMessage = false;
        }

        private void SplitBill()
        {
            ListSelectedRowCollection selectedRowsInfo = this.ListView.SelectedRowsInfo;
            string billtype = selectedRowsInfo[0].BillTypeID.ToString();
            string[] entryPrimaryValues= selectedRowsInfo.GetEntryPrimaryKeyValues();
            //string[] allowbilltypes={"565be729cfbfe8","56691f0d4a90f8","565be7accfc117"};
            if (selectedRowsInfo.GetPrimaryKeyValues().Length != 1)
            {
                this.View.ShowErrMessage("不允许同时对多张申请单进行拆分，请重新选择！");
                return;
            }
            if (((entryPrimaryValues.Count<string>() <= 0) || (selectedRowsInfo[0].EntryEntityKey == null)) || selectedRowsInfo[0].EntryPrimaryKeyValue.IsNullOrEmptyOrWhiteSpace())
            {
                this.View.ShowMessage("请选择分录进行拆分！");
                return;
            }
            if (billtype != "565be729cfbfe8" && billtype != "56691f0d4a90f8" && billtype != "565be7accfc117")
            {
                this.View.ShowMessage("非入库检验单、生产过程检验单、样品检验单不能拆分！");
                return;
            }

            BusinessInfo qmBusInfo = (MetaDataServiceHelper.Load(this.Context, "QM_InspectBill") as FormMetadata).BusinessInfo;
            QueryBuilderParemeter para = new QueryBuilderParemeter
                {
                    FormId = "QM_InspectBill",
                    FilterClauseWihtKey = string.Format("FID = {0} ", Convert.ToInt64(selectedRowsInfo[0].PrimaryKeyValue))
                };
            DynamicObject qmBill = BusinessDataServiceHelper.Load(this.Context, qmBusInfo.GetDynamicObjectType(), para).FirstOrDefault();
            if (Convert.ToString(qmBill["DocumentStatus"])!="A" && Convert.ToString(qmBill["DocumentStatus"])!="D")
            {
                this.View.ShowErrMessage("请选择创建或者重新审核的检验单进行拆分，当前选择的单据无效，请重新选择！");
                return;
            }
            DynamicObjectCollection qmBillEntrys = qmBill["Entity"] as DynamicObjectCollection;
            List<DynamicObject> selectBillEntrys = qmBillEntrys.Where(w => entryPrimaryValues.Contains(Convert.ToString(w["Id"]))).ToList();
            if (qmBillEntrys.Count == selectBillEntrys.Count)
            {
                this.View.ShowErrMessage("不允许对一张单据的所有记录行进行拆分!");
                return;
            }
            //下推
            IOperationResult result = this.pushBill(selectedRowsInfo, qmBillEntrys, qmBusInfo);
            if (result.IsSuccess)
            {
                //更新后台表数据
                YDLCommServiceHelper.UpdateInspectData(this.Context, entryPrimaryValues.ToList());
                this.ListView.Refresh();
                this.View.ShowNotificationMessage("单据拆分成功! 新单单据编码是" + Convert.ToString(result.SuccessDataEnity.FirstOrDefault()["BillNo"]));
            }
        }

        private IOperationResult pushBill(ListSelectedRowCollection selectedRowsInfo,DynamicObjectCollection qmBillEntrys, BusinessInfo qmBusInfo)
        {
            IOperationResult result = new OperationResult();
            List<DynamicObject> list = new List<DynamicObject>();
            ConvertRuleElement rule = ConvertServiceHelper.GetConvertRules(this.Context, "QM_InspectBill", "QM_InspectBill").FirstOrDefault<ConvertRuleElement>(t => t.Id == "JN_YDL_Inspect-Split");
            if (rule == null)
            {
                result.IsSuccess = false;
                return result;
            }
            //调用下推服务
            ConvertOperationResult operationResult = null;
            Dictionary<string, object> custParams = new Dictionary<string, object>();
            PushArgs pushArgs = new PushArgs(rule, selectedRowsInfo.ToArray())

            {
                TargetBillTypeId = selectedRowsInfo[0].BillTypeID.ToString(),
            };
            //执行下推操作，并获取下推结果
            operationResult = ConvertServiceHelper.Push(this.Context, pushArgs, OperateOption.Create());
            if (!operationResult.IsSuccess)
            {
                result = operationResult as IOperationResult;
                return result;
            }
            DynamicObject targetData = operationResult.TargetDataEntities.Select(s => s.DataEntity).FirstOrDefault();
            //循环目标主分录，把子分录的值携带过来
            DynamicObjectCollection targetDataEntrys = targetData["Entity"] as DynamicObjectCollection;
            foreach (DynamicObject targetDataEntry in targetDataEntrys)
            {
                DynamicObject sourceDataEntry = qmBillEntrys.FirstOrDefault(f => Convert.ToInt64(f["Id"]) == Convert.ToInt64(targetDataEntry["SrcEntryId"]));
                targetDataEntry["SrcBillType"] = sourceDataEntry["SrcBillType"];
                targetDataEntry["SrcBillNo"] = sourceDataEntry["SrcBillNo"];
                targetDataEntry["SrcInterId"] = sourceDataEntry["SrcInterId"];
                targetDataEntry["SrcEntryId"] = sourceDataEntry["SrcEntryId"];
                targetDataEntry["SrcEntrySeq"] = sourceDataEntry["SrcEntrySeq"];
            }
            list.Add(targetData);
            if (list.Count > 0)
            {
                DBServiceHelper.LoadReferenceObject(this.Context, list.ToArray(), qmBusInfo.GetDynamicObjectType(), false);
            }
            result = BusinessDataServiceHelper.Save(this.Context, qmBusInfo, list.ToArray());
            return result;
        }
    }
}
