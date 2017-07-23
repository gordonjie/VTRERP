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

namespace JN.K3.YDL.App.ServicePlugIn.MFG.PPBOM
{
    [Description("配方单到生产退料单")]
    public class PPBOMToReturnMtrl : AbstractConvertPlugIn
    {
        public override void AfterConvert(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.AfterConvertEventArgs e)
        {


            ExtendedDataEntity[] dataEntits = e.Result.FindByEntityKey("FBillHead");//取单据数据包
            List<long> lstMoEntryIds = new List<long>();//生产订单分录id
            if (dataEntits != null && dataEntits.Count() > 0)
            {
                foreach (var entity in dataEntits)
                {
                    DynamicObjectCollection entry = entity["Entity"] as DynamicObjectCollection;
                    if (entry == null || entry.Count <= 0)
                    {
                        continue;
                    }
                    foreach (var item in entry)
                    {
                        lstMoEntryIds.Add(Convert.ToInt64(item["MoEntryId"]));//生产订单分录Id
                    }

                }

            }

            if (lstMoEntryIds.Count > 0)
            {
                Entity enitty = e.TargetBusinessInfo.GetEntity("FEntity");
                List<DynamicObject> lstEntry = new List<DynamicObject>();
                string sql = string.Format(@" SELECT DISTINCT FENTRYID,FLOT,FLOT_TEXT FROM T_PRD_MOENTRY 
                           WHERE FENTRYID IN ({0})", string.Join<long>(",", lstMoEntryIds));
                DynamicObjectCollection dyn = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (dyn != null && dyn.Count() > 0)
                {
                    Dictionary<long, DynamicObject> dicLot = dyn.ToDictionary<DynamicObject, long, DynamicObject>(p => Convert.ToInt64(p["FENTRYID"]), p => p);
                    if (dicLot.Count() > 0)
                    {
                        foreach (var entity in dataEntits)
                        {
                            DynamicObjectCollection entry = entity["Entity"] as DynamicObjectCollection;
                            if (entry == null || entry.Count <= 0)
                            {
                                continue;
                            }
                            foreach (var item in entry)
                            {
                                long entryId = Convert.ToInt64(item["MoEntryId"]);//生产订单分录Id
                                if (dicLot.ContainsKey(entryId))
                                {
                                    DynamicObject lot = dicLot[entryId];
                                    item["F_JN_MaterialLot_Id"] = Convert.ToInt64(lot["FLOT"]);
                                    item["F_JN_MaterialLot_Text"] = Convert.ToString(lot["FLOT_TEXT"]);
                                    lstEntry.Add(item);
                                }

                            }
                        }

                    }
                }
                if (lstEntry.Count() > 0)
                {
                    Kingdee.BOS.Contracts.ServiceFactory.GetService<IDBService>(this.Context).LoadReferenceObject(base.Context, lstEntry.ToArray(), enitty.DynamicObjectType, false);
                }

            }

            base.AfterConvert(e);
        }
    }
}
