using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM.STK
{
    [Description("扩展--即时库存列表插件")]
    public class JN_STK_Inventory : AbstractListPlugIn
    {
        bool enzyme = false;
        /// <summary>
        /// 即时库存所有数据集合
        /// </summary>
        List<DynamicObject> invData = new List<DynamicObject>();

        public override void FormatCellValue(Kingdee.BOS.Core.List.PlugIn.Args.FormatCellValueArgs args)
        {
            base.FormatCellValue(args);
            //单位酶活量
            if (args.Header.RealKey.ToUpperInvariant() == "FJNUNITQTY")
            {
                DynamicObject obj = invData.FirstOrDefault(f => Convert.ToString(f["FID"]) == Convert.ToString(args.DataRow["FID"]));
                if (obj == null || Convert.ToDecimal(obj["FBASEQTY"]) == 0 || Convert.ToDecimal(obj["FSecQty"]) == 0)
                {
                    args.FormateValue = "";
                }
                else
                {
                    args.FormateValue = Convert.ToString(Math.Round(Convert.ToDecimal(obj["FSecQty"]) / Convert.ToDecimal(obj["FBASEQTY"]), 4));
                }
                this.ListView.UpdateView("FJNUNITQTY");
                return;
            }
            //标吨
            if (args.Header.RealKey.ToUpperInvariant() == "FJNTONPROPERTY")
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
                this.ListView.UpdateView("FJNTONPROPERTY");
                return;
            }
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            invData = YDLCommServiceHelper.GetInventoryAllData(this.Context);
            enzyme = Convert.ToBoolean(this.View.OpenParameter.GetCustomParameter("enzyme"));
        }

        public override void BarItemClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey == "tbReturnData" && enzyme == true)
            {
                ListSelectedRowCollection selectRows = this.ListView.SelectedRowsInfo;//列表多选 
                if (selectRows == null || selectRows.Count == 0)
                {
                    this.View.ShowMessage("没有选择任何数据，请选择数据!");
                    return;
                }
                StringBuilder lstFID = new StringBuilder();//当前选中行的主键集合
                for (int i = 0; i < selectRows.GetRowKeys().Count(); i++)
                {
                    var entity = selectRows[i];
                    string fid = "'" + entity.PrimaryKeyValue + "'";
                    lstFID.Append(fid + ",");
                }
                string FID = lstFID.ToString();
                DynamicObjectCollection remarkDy = ServiceHelper.YDLCommServiceHelper.GetINVENTORY(this.Context, FID.Substring(0, FID.Length - 1));

                this.View.ReturnToParentWindow(remarkDy);
                this.View.Close();
            }
        }
    }
}
