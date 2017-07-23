using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.SystemParameter.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM.SAL
{
    [Description("销售预测变更单表单插件")]
    public class ForecastChangeBillEdit : AbstractBillPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            //初始化供应组织和生产车间
            DynamicObjectCollection entityRows = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            for (int row = 0; row < entityRows.Count - 1; row++)
            {
                DynamicObject Material = this.View.Model.GetValue("FJNMaterialId", row) as DynamicObject;
                DynamicObject SupplyOrg = this.View.Model.GetValue("FJNSupplyOrg", row) as DynamicObject;
                if (Material != null && SupplyOrg != null)
                {
                    int Materialid = Convert.ToInt32(Material["Id"]);
                    int SupplyOrgid = Convert.ToInt32(SupplyOrg["Id"]);
                    SetF_VTR_PrdDeptId(this.Context, Materialid, SupplyOrgid, row);
                }
                if (Material != null)
                {
                    string Materialname = Material["Name"].ToString();
                    if (Materialname.IndexOf("(内蒙）") > 0 || Materialname.IndexOf("(内蒙)") > 0 || Materialname.IndexOf("（内蒙）") > 0)
                    {
                        this.View.Model.SetValue("FJNSupplyOrg", 100063, row);
                    }
                    else
                    {
                        this.View.Model.SetValue("FJNSupplyOrg", 100062, row);
                    }
                }
             }

        }
        public override void BeforeF7Select(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            //过滤成品根据MRP计划订单单据类型过滤物料（取消过滤）
            /*
            switch (e.BaseDataField.Key)
            {
                case "FJNMaterialId":
                    e.ListFilterParameter.Filter = this.GetMaterialFilterSql(e.ListFilterParameter.Filter);
                    return;
            }*/
        }

        public override void BeforeSetItemValueByNumber(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeSetItemValueByNumberArgs e)
        {
            base.BeforeSetItemValueByNumber(e);

            //过滤成品根据MRP计划订单单据类型过滤物料（取消过滤）
            /*
            switch (e.BaseDataField.Key)
            {
                case "FJNMaterialId":
                    e.Filter = this.GetMaterialFilterSql(e.Filter);
                    return;
            }*/
        }

        private string GetMaterialFilterSql(string sysFilter)
        {
            string str = "1=0";
            string str3 = Convert.ToString(BusinessDataServiceHelper.LoadBillTypePara(base.Context, "PLN_PlanOrderBillTypeParam", "77ddfb5c4daa44d288b4e9efbd768e94", true)["PlanStrategy"]);
            str = string.Format("FPlanningStrategy = '{0}'", str3);
            if (!string.IsNullOrWhiteSpace(sysFilter))
            {
                return (sysFilter + " AND " + str);
            }
            return str;
        }

        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);

            /*
            if (e.Field.Key.Equals("FJNDate"))
            {
                DateTime FJNDate = Convert.ToDateTime(this.View.Model.GetValue("FJNDate"));
                DateTime now = DateTime.Now;
                DateTime compareDate = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                if (FJNDate != null && FJNDate < compareDate)
                {
                    this.View.ShowErrMessage("预测日期必须从下个月1号开始！");
                    return;
                }
            }*/
            if (e.Field.Key.Equals("FJNSalerId"))
            {
                DynamicObject Saler = this.View.Model.GetValue("FJNSalerId", e.Row) as DynamicObject;
                DynamicObjectCollection salerentity = Saler["BD_SALESMANENTRY"] as DynamicObjectCollection;
                DynamicObject salergroup = salerentity.FirstOrDefault<DynamicObject>(f => Convert.ToBoolean(f["IsDefault"]) == true);
                int salergroupid = Convert.ToInt32(salergroup["OPERATORGROUPID_Id"]);
                this.View.Model.SetValue("FJNSaleGroupId", salergroupid);

            }

            if (e.Field.Key.Equals("FJNMaterialId") || e.Field.Key.Equals("FJNSupplyOrg"))
            {
                int row = e.Row;
                DynamicObject Material = this.View.Model.GetValue("FJNMaterialId", row) as DynamicObject;
                DynamicObject SupplyOrg = this.View.Model.GetValue("FJNSupplyOrg", row) as DynamicObject;
                if (Material != null && SupplyOrg != null)
                {
                    int Materialid = Convert.ToInt32(Material["Id"]);
                    int SupplyOrgid = Convert.ToInt32(SupplyOrg["Id"]);
                    SetF_VTR_PrdDeptId(this.Context, Materialid, SupplyOrgid, row);
                }
                if (Material != null)
                {
                    string Materialname = Material["Name"].ToString();
                    if (Materialname.IndexOf("(内蒙）") > 0 || Materialname.IndexOf("(内蒙)") > 0 || Materialname.IndexOf("（内蒙）") > 0)
                    {
                        this.View.Model.SetValue("FJNSupplyOrg", 100063, row);
                    }
                    else
                    {
                        this.View.Model.SetValue("FJNSupplyOrg", 100062, row);
                    }
                }



            }
        }

        private void SetF_VTR_PrdDeptId(Context ctx, int Materialid, int SupplyOrgid, int row)
        {
            string sql = string.Format(@"select t2.FWORKSHOPID  as FWORKSHOPID from T_BD_MATERIAL  t1
join T_BD_MATERIALPRODUCE t2 on t1.FMATERIALID=t2.FMATERIALID
where t1.FMASTERID in(
select FMASTERID from T_BD_MATERIAL where FMATERIALID={0})
and t1.FUSEORGID={1}", Materialid, SupplyOrgid);
            DynamicObjectCollection FWORKSHOPID = DBServiceHelper.ExecuteDynamicObject(ctx, sql);
            if (FWORKSHOPID.Count > 0)
            {
                int WORKSHOP = Convert.ToInt32(FWORKSHOPID[0]["FWORKSHOPID"]);
                /*FormMetadata formMetadata = MetaDataServiceHelper.Load(this.Context, "BD_Department") as FormMetadata;
                DynamicObject WORKSHOPObject = BusinessDataServiceHelper.LoadSingle(
                                this.Context,
                                WORKSHOP,
                                formMetadata.BusinessInfo.GetDynamicObjectType());*/
                this.View.Model.SetValue("F_VTR_PrdDeptId", WORKSHOP, row);
            }
            else
            {
                this.View.Model.SetValue("F_VTR_PrdDeptId", 0, row);
            }
        }



        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            //判断要货日期
            DateTime endsubdate = getendsubdate();
            DateTime beginsubdate = getbeginsubdate();
            DynamicObjectCollection entityRows = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            foreach (var entityRow in entityRows)
            {
                DateTime FJNDate = Convert.ToDateTime(entityRow["FJNSubDate"]);
                if (FJNDate > endsubdate)
                {
                    string messages = string.Format("要货日期不能大于{0}", Convert.ToString(endsubdate));
                    this.View.ShowMessage(messages);
                    e.Cancel = true;
                    return;
                }
                if (FJNDate < beginsubdate)
                {
                    string messages = string.Format("要货日期需要大于{0}", Convert.ToString(beginsubdate));
                    this.View.ShowMessage(messages);
                    e.Cancel = true;
                    return;
                }

            }

            //启动审批流设置创建人不等于销售员
            DynamicObject FJNSaler = this.View.Model.GetValue("FJNSalerId") as DynamicObject;
            DynamicObject FCreatorId = this.View.Model.GetValue("FCreatorId") as DynamicObject;
            string FJNSalerName = Convert.ToString(FJNSaler["Name"]);
            string FCreatorName = Convert.ToString(FCreatorId["Name"]);
            if (FJNSalerName == FCreatorName)
            {
                this.View.Model.SetValue("F_JNApprovalflow", FJNSalerName);
            }
            else
            {
                this.View.Model.SetValue("F_JNApprovalflow", "启用");
            }
        }


        //结算最晚要货日期
        private DateTime getendsubdate()
        {
            DateTime FJNDate = Convert.ToDateTime(this.View.Model.GetValue("FJNDate"));
            DateTime compareDate = Convert.ToDateTime(this.View.Model.GetValue("FJNDate"));
            DateTime now = TimeServiceHelper.GetSystemDateTime(this.Context);
            int date = now.Day;
            if (date > 25)
            {
                compareDate = new DateTime(now.AddMonths(2).Year, now.AddMonths(2).Month, 1).AddDays(-1);
            }
            if (date <= 25)
            {
                compareDate = new DateTime(now.AddMonths(1).Year, now.AddMonths(1).Month, 1).AddDays(-1);
            }
            return compareDate;


        }


        //结算最早要货日期
        private DateTime getbeginsubdate()
        {
            DateTime FJNDate = Convert.ToDateTime(this.View.Model.GetValue("FJNDate"));
            DateTime compareDate = Convert.ToDateTime(this.View.Model.GetValue("FJNDate"));
            DateTime now = TimeServiceHelper.GetSystemDateTime(this.Context);
            int date = now.Day;
            if (date > 25)
            {
                compareDate = new DateTime(now.Year, now.Month, 26);
            }
            if (date <= 25)
            {
                compareDate = new DateTime(now.Year, now.Month, 1);
            }
            if (compareDate < now.Date)
            {
                compareDate = now.Date;
            }
            return compareDate;


        }


    }
}
