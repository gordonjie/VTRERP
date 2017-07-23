using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Metadata.EntityElement;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.Requisition
{
    [Description("采购申请单到采购订单")]
    public class RequisitionToOrder : AbstractConvertPlugIn
    {
        //源单数据
        Dictionary<long, DynamicObject> dicSrcLot = null;

        //下推获取源单数据
        public override void OnGetSourceData(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.GetSourceDataEventArgs e)
        {
            base.OnGetSourceData(e);
            GetSrcData(e.SourceData);
        }

        //选单获取源单数据
        public override void OnGetDrawSourceData(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.GetDrawSourceDataEventArgs e)
        {
            base.OnGetDrawSourceData(e);
            GetSrcData(e.SourceData);
        }

        //获取源单F_JN_Lot_Text
        private void GetSrcData(DynamicObjectCollection srcRows)
        {
            List<long> srcFIds = new List<long>();
            foreach (DynamicObject srcRow in srcRows)
            {
                if (srcRow == null)
                {
                    continue;
                }
                srcFIds.Add(Convert.ToInt64(srcRow["FEntity_FEntryID"]));
            }
            if (srcFIds.Count <= 0)
            {
                return;
            }
            string sql = string.Format(@" SELECT FENTRYID,F_JN_Lot_Text FROM T_PUR_ReqEntry  
                                          WHERE FENTRYID IN ({0})", string.Join<long>(",", srcFIds));
            DynamicObjectCollection dyn = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (dyn == null || dyn.Count <= 0)
            {
                return;
            }
            dicSrcLot = dyn.ToDictionary<DynamicObject, long, DynamicObject>(p => Convert.ToInt64(p["FENTRYID"]), p => p);
        }


        /// <summary>
        /// 转换完之后对目标单FLot_Text赋值源单FLot_Text
        /// </summary>
        /// <param name="e"></param>
        public override void AfterConvert(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.AfterConvertEventArgs e)
        {
            base.AfterConvert(e);
            if (dicSrcLot == null)
            {
                return;
            }
            // 取下推生成的全部单据
            ExtendedDataEntity[] billDataEntitys = e.Result.FindByEntityKey("FBillHead");
            if (billDataEntitys == null)
            {
                return;
            }
            foreach (ExtendedDataEntity billDataEntity in billDataEntitys)
            {
                DynamicObject dyBillHead = billDataEntity.DataEntity;
                if (dyBillHead == null)
                {
                    continue;
                }
                // 取单据体明细行集合
                DynamicObjectCollection entryRows = dyBillHead["POOrderEntry"] as DynamicObjectCollection;
                if (entryRows == null)
                {
                    continue;
                }
                foreach (DynamicObject entryRow in entryRows)
                {
                    if (entryRow == null)
                    {
                        continue;
                    }
                    // 取每行的源单及源单单据体内码
                    DynamicObjectCollection linkRows = entryRow["FPOOrderEntry_Link"] as DynamicObjectCollection;
                    if (linkRows == null || linkRows.Count <= 0)
                    {
                        continue;
                    }
                    // TODO: 根据目标单的来源单，进行后续处理
                    if (dicSrcLot.ContainsKey(Convert.ToInt64(linkRows[0]["SId"])))
                    {
                        entryRow["FLot_Text"] = dicSrcLot[Convert.ToInt64(linkRows[0]["SId"])]["F_JN_Lot_Text"];
                    }
                    #region
                    //List<long> srcBillIds = new List<long>();
                    //List<long> srcEntryIds = new List<long>();
                    //foreach (var linkRow in linkRows)
                    //{
                    //    long srcBillId = Convert.ToInt64(linkRow["SBillId"]);
                    //    long srcEntryId = Convert.ToInt64(linkRow["SId"]);
                    //    srcBillIds.Add(srcBillId);
                    //    srcEntryIds.Add(srcEntryId);
                    //}   
                    #endregion
                }
            }
        }
    }
}
