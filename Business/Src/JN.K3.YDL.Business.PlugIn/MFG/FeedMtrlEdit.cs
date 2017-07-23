using JN.K3.YDL.Core;
using Kingdee.BOS.Core.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;


namespace JN.K3.YDL.Business.PlugIn.MFG
{
    [Description("生产补料单-单位数量与酶活量的换算")]
    public class FeedMtrlEdit : JNQTYConversion
    {
        public override List<JNQTYConversionPara> ConvertFldKey
        {
            get
            {
                List<JNQTYConversionPara> para = new List<JNQTYConversionPara>();
                para.Add(new JNQTYConversionPara()
                {
                    ConverRateFldKey = "FJNUnitEnzymes",
                    SrcQtyFldKey = "FBaseActualQty",
                    DestQtyFldKey = "FSecActualQty"
                });


                return para;
            }
        }

        public override void AfterBindData(EventArgs e)
        {

            if (base.View.OpenParameter.CreateFrom == CreateFrom.Push || base.View.OpenParameter.CreateFrom == CreateFrom.Draw)
            {
                int count = this.View.Model.GetEntryRowCount("FEntity");
                for (int i = 0; i < count; i++)
                {
                    base.UpdateRate("FLot", i);
                }

            }
            base.AfterBindData(e);
        }

        /// <summary>
        /// 保存前，取仓库审批流用
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            var store = this.View.Model.GetValue("FStockId", 0);
            this.View.Model.SetValue("F_JNBaseStock", store);
            this.View.UpdateView("F_JNBaseStock");

        }
    }
}
