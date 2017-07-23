using JN.BOS.Core;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM.SAL
{
    /// <summary>
    /// 销售预测单结余表动态表单插件
    /// </summary>
    [Description("销售预测单结余表动态表单插件")]
    public class ForecastBalanceEdit : AbstractDynamicFormPlugIn
    {
        public override void BeforeDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            //减少结余，增加结余
            if (e.Operation.FormOperation.Id.Equals("DoReduce") || e.Operation.FormOperation.Id.Equals("DoIncrease"))
            {
                Entity forecastEntity = this.View.BusinessInfo.GetEntity("FEntity");
                DynamicObjectCollection dycForecastEntrys = this.View.Model.GetEntityDataObject(forecastEntity);
                if (dycForecastEntrys == null || dycForecastEntrys.Count() <= 0)
                {
                    e.Cancel = true;
                    return;
                }
                List<DynamicObject> lstForecastEntrys = dycForecastEntrys.Where(p => Convert.ToBoolean(p["FIsSelect"]) == true).ToList();
                if (lstForecastEntrys == null || lstForecastEntrys.Count() <= 0)
                {
                    this.View.ShowMessage("没有选择任何数据，请先选择!", Kingdee.BOS.Core.DynamicForm.MessageBoxType.Advise);
                    e.Cancel = true;
                    return;
                }
                List<DynamicObject> lstQtyEntrys = dycForecastEntrys.Where(p => Convert.ToDecimal(p["FAdjustQty"]) <= 0 && Convert.ToBoolean(p["FIsSelect"]) == true).ToList();
                if (lstQtyEntrys != null && lstQtyEntrys.Count() > 0)
                {
                    List<int> lstSeq = new List<int>();
                    foreach (var item in lstQtyEntrys)
                    {
                        lstSeq.Add(Convert.ToInt32(item["Seq"]));
                    }
                    this.View.ShowMessage(string.Format("第{0}行的调整数量必须大于0，否则不能进行结余操作!", string.Join(",", lstSeq)), Kingdee.BOS.Core.DynamicForm.MessageBoxType.Advise);
                    e.Cancel = true;
                    return;
                }
                else
                {
                    e.Option.SetVariableValue("DynamicObjectParams", lstForecastEntrys);
                }
                //向服务端传调整方向参数
                if (e.Operation.FormOperation.Id.Equals("DoReduce"))
                {
                    e.Option.SetVariableValue("AdjustTypeParams", "A");
                }
                else
                {
                    e.Option.SetVariableValue("AdjustTypeParams", "B");
                }
                //var lstObjGroups = dycForecastEntrys.GroupBy(o => new DataEntityGroupKey(new string[] { "FSaleOrgId", "FSaleDeptId", "FSaleGroupId", "FSalerId", "FIsSelect" }, dycForecastEntrys.DynamicCollectionItemPropertyType).GetKey(o));
            }
        }

        public override void AfterDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            //刷新
            if (e.OperationResult.IsSuccess && (e.Operation.Id.Equals("DoRefresh") ||
                e.Operation.Id.Equals("DoReduce") || e.Operation.Id.Equals("DoIncrease")))
            {
                SetEntityData();
            }
        }


        public override void AfterBarItemClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            ////刷新
            //if (e.BarItemKey.Equals("tbRefresh"))
            //{
            //    SetEntityData();
            //}
        }

        /// <summary>
        /// 填充数据
        /// </summary>
        private void SetEntityData()
        {
            //清空数据
            int row = this.View.Model.GetEntryRowCount("FEntity");
            if (row > 0)
            {
                for (int i = row - 1; i >= 0; i--)
                {
                    this.View.Model.DeleteEntryRow("FEntity", i);
                }
            }
            //销售组织
            long OrgId = GetDynamicObjectID("FJNSaleOrgId");
            //销售部门
            long DeptId = GetDynamicObjectID("FJNSaleDeptId");
            //销售组
            long GroupId = GetDynamicObjectID("FJNSaleGroupId");
            //销售员
            long SalerId = GetDynamicObjectID("FJNSalerId");
            //起始日期
            DateTime SDate = Convert.ToDateTime(this.View.Model.GetValue("FSDate"));
            //结束日期
            DateTime EDate = Convert.ToDateTime(this.View.Model.GetValue("FEDate")).AddDays(1);
            //销售预测结余信息
            DynamicObjectCollection dycResult = YDLCommServiceHelper.GetForecastBalanceInfo(this.Context, OrgId, DeptId, GroupId, SalerId, SDate, EDate);
            if (dycResult == null || dycResult.Count() <= 0)
            {
                return;
            }
            this.View.Model.BeginIniti();
            this.View.Model.BatchCreateNewEntryRow("FEntity", dycResult.Count());
            int rowIndex = 0;
            foreach (DynamicObject item in dycResult)
            {
                this.View.Model.SetValue("FSalerId", item["FSalerId"], rowIndex);
                this.View.Model.SetValue("FMaterialId", item["FMaterialId"], rowIndex);
                this.View.Model.SetValue("FconsumQty", item["FconsumQty"], rowIndex);
                this.View.Model.SetValue("FreductQty", item["FreductQty"], rowIndex);
                this.View.Model.SetValue("FaddQty", item["FaddQty"], rowIndex);
                this.View.Model.SetValue("FQty", item["FQty"], rowIndex);
                this.View.Model.SetValue("FSaleOrgId", item["FSaleOrgId"], rowIndex);
                this.View.Model.SetValue("FAuxPropId", item["FAuxPropId"], rowIndex);
                this.View.Model.SetValue("FUnitID", item["FUnitID"], rowIndex);
                this.View.Model.SetValue("FSaleDeptId", item["FSaleDeptId"], rowIndex);
                //this.View.Model.SetValue("FSaleGroupId", item["FSaleGroupId"], rowIndex);
                this.View.Model.SetValue("FforecastQty", item["FforecastQty"], rowIndex);
                this.View.Model.SetValue("FRate", item["FRate"], rowIndex);
                //this.View.Model.SetValue("FJNSUBDATE", item["FJNSUBDATE"], rowIndex);
               
                rowIndex++;
            }
            this.View.Model.EndIniti();
            this.View.UpdateView("FEntity");
        }

        /// <summary>
        /// 获取基础资料的ID
        /// </summary>
        /// <param name="skey"></param>
        /// <returns></returns>
        private long GetDynamicObjectID(string skey)
        {
            long id = 0;
            DynamicObject doc = this.View.Model.GetValue(skey) as DynamicObject;
            if (doc != null)
            {
                id = Convert.ToInt64(doc["ID"]);
            }
            return id;
        }


       
    }
}
