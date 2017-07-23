using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using JN.K3.YDL.Core;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;

namespace JN.K3.YDL.Business.PlugIn.BD
{
    /// <summary>
    /// 溢多利质检方案定制插件
    /// </summary>
    [Description("溢多利质检方案定制插件")]
    public class JN_QM_QCSchemeEdit : AbstractBillPlugIn
    {
        /// <summary>
        /// 菜单打开源单据
        /// </summary>
        /// <param name="e"></param>
        /// 
        public override void AfterBarItemClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            var barkey = e.BarItemKey.ToUpper();
            if (barkey == "TBVIEWSRC")
            {

                var ENGBOMMeta = AppServiceContext.MetadataService.Load(this.Context, "SAL_QUOTATION") as FormMetadata;
                string billNo = Convert.ToString(this.View.Model.GetValue("F_JNSRCBillNo"));
                if (billNo != null || billNo != "")
                {
                    BillShowParameter parameter = YDLCommServiceHelper.GetShowParameter(this.Context, ENGBOMMeta, billNo);
                    this.View.ShowForm(parameter);
                }


            }


        }
    }
}
