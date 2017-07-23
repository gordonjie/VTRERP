using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.Metadata;
using JN.K3.YDL.Core;
using Kingdee.BOS.Core.Bill;


namespace JN.K3.YDL.Business.PlugIn.BD.Material
{
    /// <summary>
    /// 溢多利物料定制插件
    /// </summary>
    [Description("溢多利物料定制插件")]
    public class JN_YDL_MaterialEditPlugIn : AbstractBillPlugIn
    {
        /// <summary>
        /// 数据包生成后逻辑处理
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateNewData(EventArgs e)
        {
            var inputParaObj = this.View.OpenParameter.GetCustomParameter("__ParentEntryObject__") as DynamicObject;
            if (inputParaObj != null)
            {
                this.Model.SetValue("FName", new LocaleValue(Convert.ToString(inputParaObj["F_JN_ProductName"])));
                this.Model.SetValue("FMaterialGroup", inputParaObj["F_JN_MtrlGroupId_Id"]);
                this.Model.SetValue("FBaseUnitId", inputParaObj["UnitId_Id"]);
                this.Model.SetValue("FSaleUnitId", inputParaObj["UnitId_Id"]);
                this.Model.SetValue("FSalePriceUnitId", inputParaObj["UnitId_Id"]);
            }
        }


        /// <summary>
        /// 表单关闭前，返回表单数据
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeClosed(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeClosedEventArgs e)
        {
            base.BeforeClosed(e);
            if (this.View.ParentFormView != null && this.View.ParentFormView.BillBusinessInfo.GetForm().Id.EqualsIgnoreCase("SAL_QUOTATION"))
            {
                this.View.ReturnToParentWindow(this.Model.DataObject);
            }
        }

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
                string billNo=Convert.ToString( this.View.Model.GetValue("F_JNSRCBillNo"));
                if (billNo != null || billNo != "")
                {
                BillShowParameter parameter=YDLCommServiceHelper.GetShowParameter(this.Context, ENGBOMMeta, billNo);
                this.View.ShowForm(parameter);
                }

               
                }
      
           
        }
    }
}
