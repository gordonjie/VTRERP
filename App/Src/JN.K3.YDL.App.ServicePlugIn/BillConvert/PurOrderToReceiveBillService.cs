using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.BillConvert
{
    [Description("内蒙采购流程-采购订单至收料通知单-单据转换插件")]
    public class PurOrderToReceiveBillService : POConvertBaseService
    {
        public override void DoExecute(Kingdee.BOS.Core.DynamicForm.IDynamicFormView targetView)
        {
            DynamicObjectCollection dataEntrys = targetView.Model.DataObject["PUR_ReceiveEntry"] as DynamicObjectCollection;
            if (dataEntrys == null || dataEntrys.Count == 0) { return; }
            List<long> poEntryIds = dataEntrys.Select(s => Convert.ToInt64(s["POORDERENTRYID"])).ToList();
            //内蒙出库的数量批号
            List<DynamicObject> outStockData = base.GetOutStockData(poEntryIds);
            //倒序搜索
            for (int row = dataEntrys.Count - 1; row >= 0; row--)
            {
                long poEntryId = Convert.ToInt64(dataEntrys[row]["POORDERENTRYID"]);
                List<DynamicObject> srcOutData = outStockData.Where(w => Convert.ToInt64(w["FENTRYID"]) == poEntryId).ToList();
                if (srcOutData == null || srcOutData.Count == 0) { continue; }
                //给默认携带行赋值
                targetView.Model.SetValue("FActReceiveQty", srcOutData[0]["FREALQTY"], row);
                targetView.Model.SetValue("FUNITID", srcOutData[0]["FUNITID"], row);
                targetView.Model.SetValue("FLot", srcOutData[0]["FLOT_TEXT"], row);
                targetView.Model.SetValue("FJNUnitEnzymes", srcOutData[0]["FJNUnitEnzymes"], row);
                targetView.Model.SetValue("FExtAuxUnitQty", srcOutData[0]["FAuxUnitQty"], row); 
                targetView.InvokeFieldUpdateService("FActReceiveQty", row);
                targetView.InvokeFieldUpdateService("FLot", row);
                //出库数据排除第一行，按照序号倒排
                srcOutData.RemoveAt(0);
                if (srcOutData == null || srcOutData.Count == 0) { continue; }
                srcOutData = srcOutData.OrderByDescending(o => o["FID"]).ThenByDescending(o => o["FSEQ"]).ToList();
                for (int i = 0; i < srcOutData.Count; i++)
                {
                    targetView.Model.CopyEntryRowFollowCurrent("FDetailEntity", row, row, true);
                    targetView.Model.SetValue("FActReceiveQty", srcOutData[i]["FREALQTY"], row + 1);
                    targetView.Model.SetValue("FUNITID", srcOutData[i]["FUNITID"], row + 1);
                    targetView.Model.SetValue("FLot", srcOutData[i]["FLOT_TEXT"], row + 1);
                    targetView.Model.SetValue("FJNUnitEnzymes", srcOutData[i]["FJNUnitEnzymes"], row + 1);
                    targetView.Model.SetValue("FExtAuxUnitQty", srcOutData[i]["FAuxUnitQty"], row + 1);
                    //其他字段赋值
                    targetView.Model.SetValue("FSrcFormId", targetView.Model.GetValue("FSrcFormId", row), row + 1);
                    targetView.Model.SetValue("FSRCBillNo", targetView.Model.GetValue("FSRCBillNo", row), row + 1);
                    targetView.Model.SetValue("FOrderBillNo", targetView.Model.GetValue("FOrderBillNo", row), row + 1);
                    targetView.Model.SetValue("FSrcId", targetView.Model.GetValue("FSrcId", row), row + 1);
                    targetView.Model.SetValue("FSrcEntryId", targetView.Model.GetValue("FSrcEntryId", row), row + 1);
                    targetView.Model.SetValue("FPOORDERENTRYID", targetView.Model.GetValue("FPOORDERENTRYID", row), row + 1);
                    targetView.Model.SetValue("FBFLowId", targetView.Model.GetValue("FBFLowId", row), row + 1);
                    targetView.InvokeFieldUpdateService("FActReceiveQty", row + 1);
                    targetView.InvokeFieldUpdateService("FLot", row + 1);
                    //保质期为空，默认当前日期
                    if (targetView.Model.GetValue("FProduceDate", row + 1) == null)
                    {
                        targetView.Model.SetValue("FProduceDate", DateTime.Now.Date, row + 1);
                        targetView.InvokeFieldUpdateService("FProduceDate", row + 1);
                    }
                }
            }
        }

    }
}
