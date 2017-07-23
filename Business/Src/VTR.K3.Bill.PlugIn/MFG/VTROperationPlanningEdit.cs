using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.K3.MFG.SFC.Business.PlugIn.Bill;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VTR.K3.Bill.PlugIn.MFG
{
    public class VTROperationPlanningEdit : OperationPlanningEdit
    {

        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);
            int row = this.View.Model.GetEntryRowCount("FSubEntity");
            DynamicObject WorkCenter = this.View.Model.GetValue("FWorkCenterId") as DynamicObject;
            for (int i = 0; i < row; i++)
            {
                this.VTRWorkCenterIdDataChanged(WorkCenter,i);
            }
        }




        private void VTRWorkCenterIdDataChanged(DynamicObject dynamicObject,int row)
        {

            for (int i = 0; i < 6; i++)
            {
                this.Model.SetValue(string.Format("{0}{1}{2}", "FActivity", Convert.ToString((int)(i + 1)), "Id"), null, row);
                this.Model.SetValue(string.Format("{0}{1}{2}", "FActivity", Convert.ToString((int)(i + 1)), "UnitId"), null, row);
                this.Model.SetValue(string.Format("{0}{1}{2}", "FActivity", Convert.ToString((int)(i + 1)), "FormulaId"), null, row);
                this.Model.SetValue(string.Format("{0}{1}{2}", "FActivity", Convert.ToString((int)(i + 1)), "RepFormulaId"), null, row);
                this.Model.SetValue(string.Format("{0}{1}{2}", "FActivity", Convert.ToString((int)(i + 1)), "BaseQty"), null, row);
                this.Model.SetValue(string.Format("{0}{1}{2}", "FActivity", Convert.ToString((int)(i + 1)), "Qty"), null, row);
            }
           
                //FormMetadata metadata = MetaDataServiceHelper.Load(base.Context, "ENG_WorkCenter", true) as FormMetadata;
                //DynamicObject dynamicObject= BusinessDataServiceHelper.LoadSingle()
                //DynamicObject dynamicObject = BusinessDataServiceHelper.LoadSingle(base.Context, e.NewValue, metadata.BusinessInfo.GetDynamicObjectType(), null);
                DynamicObjectCollection objects = dynamicObject.GetDynamicObjectItemValue<DynamicObjectCollection>("WorkCenterBaseActivity", null);
                DynamicObjectCollection objects2 = dynamicObject.GetDynamicObjectItemValue<DynamicObjectCollection>("WorkCenterScheduling", null);
                DynamicObjectCollection objects3 = dynamicObject.GetDynamicObjectItemValue<DynamicObjectCollection>("WorkCenterCapacity", null);
                foreach (DynamicObject obj3 in objects)
                {
                    long num2 = obj3.GetDynamicObjectItemValue<long>("BaseActivityID_Id", 0L);
                    decimal num3 = obj3.GetDynamicObjectItemValue<decimal>("DefaultValue", 0M);
                    long num4 = obj3.GetDynamicObjectItemValue<long>("TimeUnit_Id", 0L);
                    long num5 = obj3.GetDynamicObjectItemValue<long>("ActFormula_Id", 0L);
                    long num6 = obj3.GetDynamicObjectItemValue<long>("ActRepFormula_Id", 0L);
                    int index = objects.IndexOf(obj3);
                    this.Model.SetValue(string.Format("{0}{1}{2}", "FActivity", Convert.ToString((int)(index + 1)), "Id"), num2, row);
                    this.Model.SetValue(string.Format("{0}{1}{2}", "FActivity", Convert.ToString((int)(index + 1)), "UnitId"), num4, row);
                    this.Model.SetValue(string.Format("{0}{1}{2}", "FActivity", Convert.ToString((int)(index + 1)), "FormulaId"), num5, row);
                    this.Model.SetValue(string.Format("{0}{1}{2}", "FActivity", Convert.ToString((int)(index + 1)), "RepFormulaId"), num6, row);
                    this.Model.SetValue(string.Format("{0}{1}{2}", "FActivity", Convert.ToString((int)(index + 1)), "BaseQty"), num3, row);
                }
                if (objects2.Count > 0)
                {
                    DynamicObject obj4 = objects2[0];
                    decimal num8 = obj4.GetDynamicObjectItemValue<decimal>("StdQueueTime", 0M);
                    decimal num9 = obj4.GetDynamicObjectItemValue<decimal>("MinQueueTime", 0M);
                    decimal num10 = obj4.GetDynamicObjectItemValue<decimal>("StdWaitTime", 0M);
                    decimal num11 = obj4.GetDynamicObjectItemValue<decimal>("MinWaitTime", 0M);
                    decimal num12 = obj4.GetDynamicObjectItemValue<decimal>("StdCarryTime", 0M);
                    decimal num13 = obj4.GetDynamicObjectItemValue<decimal>("MinCarryTime", 0M);
                    this.Model.SetValue("FStdQueueTime", num8);
                    this.Model.SetValue("FMinQueueTime", num9);
                    this.Model.SetValue("FStdWaitTime", num10);
                    this.Model.SetValue("FMinWaitTime", num11);
                    this.Model.SetValue("FStdMoveTime", num12);
                    this.Model.SetValue("FMinMoveTime", num13);
                }
                DynamicObject obj5 = null;
                if (objects3.Count > 0)
                {
                    obj5 = (from w in objects3
                            where w.GetDynamicObjectItemValue<bool>("JoinScheduling", false)
                            select w).FirstOrDefault<DynamicObject>();
                }
                if (obj5 != null)
                {
                    this.Model.SetValue("FResourceId", obj5["RESOURCEID"], row);
                }
                else
                {
                    this.Model.SetValue("FResourceId", null, row);
                }
            
        }

  


}
}
