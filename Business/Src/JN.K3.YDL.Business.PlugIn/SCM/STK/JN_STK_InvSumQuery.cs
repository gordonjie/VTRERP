using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM.STK
{
    [Description("扩展--即时库存汇总数据查询列表插件")]
    public class JN_STK_InvSumQuery : AbstractListPlugIn
    {
        /// <summary>
        /// 即时库存汇总数据查询所有数据集合
        /// </summary>
        List<DynamicObject> invData = new List<DynamicObject>();

        public override void FormatCellValue(Kingdee.BOS.Core.List.PlugIn.Args.FormatCellValueArgs args)
        {
            base.FormatCellValue(args);
            //单位酶活量
            if (args.Header.RealKey.ToUpperInvariant() == "FJN_UNITQTY")
            {
                DynamicObject obj = invData.FirstOrDefault(f => Convert.ToString(f["FID"]) == Convert.ToString(args.DataRow["FID"]));
                if (obj == null || Convert.ToDecimal(obj["FQty"]) == 0 || Convert.ToDecimal(obj["FSecQty"]) == 0)
                {
                    args.FormateValue = "";
                }
                else
                {
                    args.FormateValue = Convert.ToString(Math.Round(Convert.ToDecimal(obj["FSecQty"]) / Convert.ToDecimal(obj["FQty"]), 4));
                }
                this.ListView.UpdateView("FJN_UNITQTY");
                return;
            }
            //标吨
            if (args.Header.RealKey.ToUpperInvariant() == "FJN_TONPROPERTY")
            {
                DynamicObject obj = invData.FirstOrDefault(f => Convert.ToString(f["FID"]) == Convert.ToString(args.DataRow["FID"]));
                if (obj != null && Convert.ToString(obj["FIsMeasure"]) == "1"
                     && Convert.ToDecimal(obj["FJNTONPROPERTY"]) != 0
                     && Convert.ToDecimal(obj["FSecQty"]) != 0)
                {
                    args.FormateValue = Convert.ToString(Math.Round(Convert.ToDecimal(obj["FSecQty"]) / (Convert.ToDecimal(obj["FJNTONPROPERTY"]) * 1000), 4));
                }
                else
                {
                    args.FormateValue = "";
                }
                this.ListView.UpdateView("FJN_TONPROPERTY");
                return;
            }
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            invData = YDLCommServiceHelper.GetInvSumQueryAllData(this.Context);
        }

    }
}
