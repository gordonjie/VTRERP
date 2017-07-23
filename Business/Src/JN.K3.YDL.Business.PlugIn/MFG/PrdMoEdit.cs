using JN.K3.YDL.ServiceHelper.SCM;
using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Util;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.DynamicForm;


namespace JN.K3.YDL.Business.PlugIn.MFG
{
    [Description("生产订单单据插件")]
    public class PrdMoEdit : AbstractBillPlugIn
    {
        public override void BeforeDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            if (e.Operation.FormOperation.Operation == "ToClose")
            {
                if (Convert.ToBoolean(e.Operation.View.OpenParameter.GetCustomParameter("isPassed")))
                {
                    e.Operation.View.OpenParameter.SetCustomParameter("isPassed", false);
                    return;
                }
                string strBillNo = Convert.ToString(this.View.Model.GetValue("FBillNo"));
                int rowIndex = this.View.Model.GetEntryCurrentRowIndex("FTreeEntity");
                string strEntryId = Convert.ToString(this.View.Model.GetEntryPKValue("FTreeEntity", rowIndex));
                if (strBillNo.IsNullOrEmptyOrWhiteSpace() || strEntryId.IsNullOrEmptyOrWhiteSpace())
                {
                    e.Cancel = true;
                    return;
                }
                string checkResult = string.Empty;
                //检查是否领料
                QueryBuilderParemeter par = new QueryBuilderParemeter();
                par.FormId = "PRD_PPBOM";
                par.SelectItems = SelectorItemInfo.CreateItems("FID");
                par.FilterClauseWihtKey = string.Format(" FDocumentStatus='C' AND FMOBillNO='{0}' and FMOENTRYID={1} and FPickedQty>0 ", strBillNo, strEntryId);
                DynamicObjectCollection bomDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, par);
                //未做领料
                if (bomDatas == null || bomDatas.Count == 0)
                {
                    checkResult = "未领料";
                }
                par.FormId = "PRD_INSTOCK";
                par.SelectItems = SelectorItemInfo.CreateItems("FID");
                par.FilterClauseWihtKey = string.Format(" FDocumentStatus='C' AND FMOBILLNO='{0}' and FMOENTRYID={1}  ", strBillNo, strEntryId);
                DynamicObjectCollection stockDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, par);
                //未做入库
                if (stockDatas == null || stockDatas.Count == 0)
                {
                    checkResult += "未入库";
                }
                e.Cancel = true;
                if (!checkResult.IsNullOrEmptyOrWhiteSpace())
                {
                    this.View.ShowMessage(string.Format("当前行物料{0},请问是否继续？", checkResult), MessageBoxOptions.YesNo, new Action<MessageBoxResult>((result) =>
                     {
                         if (result == MessageBoxResult.Yes)
                         {
                             e.Cancel = false;
                             e.Operation.View.OpenParameter.SetCustomParameter("isPassed", true);
                             e.Operation.Execute();
                             
                         }
                     }
                     ));
                }
                else
                {
                    e.Cancel = false;
                }
                   
                
                
            }
        }   

        
    }
}
