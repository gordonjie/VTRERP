using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using JN.K3.YDL.Core;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Metadata;

namespace JN.K3.YDL.Business.PlugIn.MFG.BOM
{
    /// <summary>
    /// 物料清单维护插件
    /// </summary>
    [Description("溢多利物料清单定制插件")]
    public class JN_YDL_BomBillEditPlugIn : AbstractBillPlugIn
    {
        /// <summary>
        /// 数据包生成后逻辑处理
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateNewData(EventArgs e)
        {
            
        }

        /// <summary>
        /// 数据包生成后逻辑处理
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateModelData(EventArgs e)
        {
            //var inputParaObj = this.View.OpenParameter.GetCustomParameter("__ParentEntryObject__") as DynamicObject;
            long materialId = Convert.ToInt64(this.View.OpenParameter.GetCustomParameter("materialId"));
            if (materialId != 0)
            {
                this.Model.SetValue("FMATERIALID", materialId);
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

        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.EqualsIgnoreCase("FMATERIALIDCHILD"))
            {
                var entryEntity = this.View.BillBusinessInfo.GetEntity("FTreeEntity");
                var entryRow = this.Model.GetEntityDataObject(entryEntity, e.Row);
                var matId = entryRow["MATERIALIDCHILD_Id"];
                var denominator = entryRow["DENOMINATOR"];
                var sql = string.Format(@"select FConvertDenominator 
                                            from T_BD_MATERIAL t1 
                                            inner join T_BD_UNITCONVERTRATE t2 on t1.FMATERIALID=t2.FMATERIALID
                                            where t1.FMATERIALID= {0}  and t1.FISMEASURE='1' ", matId);
                var objs = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                if (objs != null && objs.Count > 0)
                {
                    var num = Convert.ToDouble(objs[0]["FConvertDenominator"]) * Convert.ToInt64(denominator);
                    this.View.Model.SetValue("FJNCompanyEA", num, e.Row);
                    this.View.UpdateView();
                }
                else
                {
                    this.View.Model.SetValue("FJNCompanyEA", 0, e.Row);
                    this.View.UpdateView();
                }
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
