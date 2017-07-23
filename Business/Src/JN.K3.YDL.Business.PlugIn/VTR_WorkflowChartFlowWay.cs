using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn
{
    public class VTR_WorkflowChartFlowWay : AbstractBillPlugIn
    {
        private string billname="";
        private string billId = "";

        public override void OnInitialize(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.InitializeEventArgs e)
        {
            base.OnInitialize(e);
            billname = e.Paramter.FormId;
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            billId = this.View.Model.DataObject["Id"].ToString();
            DynamicObjectCollection FlowWay = YDLCommServiceHelper.GetWorkflowChartFlowWay(this.Context, billname, billId);
            DynamicObjectCollection WorkflowChartFlowWaydatas = this.View.Model.DataObject["F_VTR_WorkflowChartFlowWay"] as DynamicObjectCollection;
            Entity WorkflowChartFlowWay = this.View.BillBusinessInfo.GetEntity("F_VTR_WorkflowChartFlowWay");
            WorkflowChartFlowWaydatas.Clear();
            for (int i = 0; i < FlowWay.Count; i++)
            {

                DynamicObject newdata = new DynamicObject(WorkflowChartFlowWay.DynamicObjectType);             
                newdata["F_VTR_WorkflowUser"]=  Convert.ToString(FlowWay[i]["FName"]);
                newdata["F_VTR_WorkflowDo"]= Convert.ToString(FlowWay[i]["FResult"]);
                newdata["F_VTR_WorkflowText"]= Convert.ToString(FlowWay[i]["FDisposition"]);
                newdata["F_VTR_WORKFLOWASSIGNNAME"] = Convert.ToString(FlowWay[i]["FASSIGNNAME"]);
                newdata["F_VTR_WORKFLOWTITLE"] = Convert.ToString(FlowWay[i]["FTITLE"]);
                WorkflowChartFlowWaydatas.Add(newdata);            
            }
            this.View.UpdateView("F_VTR_WorkflowChartFlowWay");
        }
    }
}
