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
    [Description("内蒙采购流程-公共基础-单据转换插件")]
    public abstract class POConvertBaseService : AbstractConvertPlugIn
    {
        public override void AfterConvert(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.AfterConvertEventArgs e)
        {
            ExtendedDataEntity[] dataEntitys = e.Result.FindByEntityKey("FBillHead");
            if (dataEntitys == null || dataEntitys.Count() == 0) { return; }
            //入库单新增视图
            IDynamicFormView targetView = (IDynamicFormView)this.CreateTargetBillView(e.TargetBusinessInfo);
            foreach (var dataEntity in dataEntitys)
            {
                DynamicObject data = dataEntity.DataEntity;
                (targetView as IBillView).OpenParameter.PkValue = 0;
                (targetView as IBillView).OpenParameter.Status = OperationStatus.ADDNEW;
                targetView.Refresh();
                targetView.Model.DataObject = data;
                //执行操作
                DoExecute(targetView);
            }
            targetView.Close();
        }


        //定义
        private object CreateTargetBillView(BusinessInfo oBusinessInfo)
        {
            string formId = oBusinessInfo.GetForm().Id;
            BillOpenParameter param = new BillOpenParameter(formId, null);
            param.SetCustomParameter("formID", formId);
            param.SetCustomParameter("pk", 0);
            param.SetCustomParameter("status", OperationStatus.ADDNEW);
            param.SetCustomParameter("PlugIns", oBusinessInfo.GetForm().CreateFormPlugIns());
            param.Context = this.Context;
            param.FormMetaData = ((FormMetadata)Kingdee.BOS.Contracts.ServiceFactory.GetMetaDataService(this.Context).Load(this.Context, formId, true));
            param.LayoutId = param.FormMetaData.GetLayoutInfo().Id;
            param.PkValue = 0;
            param.Status = OperationStatus.ADDNEW;
            object service = oBusinessInfo.GetForm().GetFormServiceProvider(false).GetService(typeof(IDynamicFormView));
            (service as IBillViewService).Initialize(param, oBusinessInfo.GetForm().GetFormServiceProvider(false));
            (service as IBillView).OpenParameter.Status = OperationStatus.ADDNEW;
            try
            {
                (service as IBillViewService).LoadData();
            }
            catch (Exception ex)
            {
            }

            return service;
        }

        /// <summary>
        /// 获取内蒙出库的数量，批号
        /// </summary>
        /// <param name="poEntryIds"></param>
        /// <returns></returns>
        protected List<DynamicObject> GetOutStockData(List<long> poEntryIds)
        {
            List<DynamicObject> lst = new List<DynamicObject>();
            string strSql = string.Format(@"SELECT t6.FID,t6.FSEQ,t1.FENTRYID,t6.FLOT_TEXT,t6.FREALQTY,t6.FUNITID,t6.FJNUnitEnzymes,t6.FAuxUnitQty
                                            FROM T_PUR_POORDERENTRY t1
                                            JOIN T_SAL_ORDERENTRY_LK t2 ON t1.FENTRYID=t2.FSID AND t1.FID=t2.FSBILLID
                                            JOIN T_SAL_ORDERENTRY t3 ON t2.FENTRYID=t3.FENTRYID
                                            JOIN T_SAL_ORDER t4 ON t3.FID=t4.FID
                                            JOIN T_SAL_OUTSTOCKENTRY_R t5 ON t4.FBILLNO=t5.FSOORDERNO AND t3.FENTRYID=t5.FSOENTRYID
                                            JOIN T_SAL_OUTSTOCKENTRY t6 ON t5.FENTRYID=t6.FENTRYID
                                            WHERE t1.FENTRYID in ({0})
                                            ORDER BY t6.FID,t6.FSEQ
                                            ", string.Join(",", poEntryIds));

          DynamicObjectCollection dt=  DBUtils.ExecuteDynamicObject(this.Context, strSql);
          if (dt != null) { lst = dt.ToList(); }
          return lst;
        }

        public abstract void DoExecute(IDynamicFormView targetView);

    }
}
