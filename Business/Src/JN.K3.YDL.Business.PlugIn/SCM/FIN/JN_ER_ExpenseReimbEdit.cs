using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.DependencyRules;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.FIN.ER.Business.PlugIn;
using Kingdee.K3.FIN.ER.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    /// <summary>
    /// 费用报销单不能携带带银行修改插件
    /// </summary>
    [Description("费用报销单不能携带带银行修改插件")]
    public class JN_ER_ExpenseReimbEdit :ER_ExpenseReimbEdit
    {
        private DynamicObject MainOrg ;
        private bool isHassourceBill;
        private object paySettlleTypeIDOldValue;
        private DynamicObject ContactBankInfo { get; set; }
        private bool _dataChanged;




        //重写Kingdee.K3.FIN.ER.Business.PlugIn.ER_ExpenseReimbEdit的AfterBindData 
        public override void AfterBindData(EventArgs e)
        {
            //base.AfterBindData(e);
            this.MainOrg = base.View.Model.GetValue("FORGID") as DynamicObject;
            if (this.MainOrg != null)
            {
                if ((base.View.OpenParameter.Status != OperationStatus.VIEW) && (base.View.Model.GetValue("FLOCCURRENCYID") == null))
                {
                    Kingdee.K3.FIN.ER.Business.PlugIn.CommonUtil.SetDefLocalCurrencyAndExchangeType(base.View, "FORGID", "FLOCCURRENCYID", "FExchangeTypeID", 0);
                }
                if (base.View.OpenParameter.Status == OperationStatus.ADDNEW)
                {
                    if (base.View.Model.GetValue("FEXPENSEORGID") == null)
                    {
                        this.SetDefaultExpenseOrg();
                    }
                    this.Setdefaultfsettletypeidvalue();
                    if ((base.View.OpenParameter.CreateFrom == CreateFrom.Default) || (base.View.OpenParameter.CreateFrom == CreateFrom.Workflow))
                    {
                        this.SetDefaultValues(long.Parse(this.MainOrg["ID"].ToString()));
                    }
                    else
                    {
                        DynamicObject obj2 = base.View.Model.DataObject["PayOrgId"] as DynamicObject;
                        long num = (obj2 == null) ? 0L : Convert.ToInt64(obj2["Id"]);
                        BOSActionExecuteContext executeContext = new BOSActionExecuteContext(base.View);
                        if (base.View.OpenParameter.CreateFrom != CreateFrom.Copy)
                        {
                            base.View.RuleContainer.RaiseDataChanged("FPayBox", base.View.Model.DataObject, executeContext);
                        }
                        base.View.RuleContainer.RaiseDataChanged("FCombinedPay", base.View.Model.DataObject, executeContext);
                        base.View.Model.SetValue("FPayOrgId", num);
                        this.SetProposerPhone();
                    }
                }
                if (base.View.OpenParameter.CreateFrom == CreateFrom.Copy)
                {
                    decimal num2 = Convert.ToDecimal(base.View.Model.DataObject["ExchangeRate"]);
                    DynamicObjectCollection objects = base.View.Model.DataObject["ER_ExpenseReimbEntry"] as DynamicObjectCollection;
                    decimal num3 = 0M;
                    int row = 0;
                    foreach (DynamicObject obj3 in objects)
                    {
                        obj3["ExpSubmitAmount"] = obj3["ExpenseAmount"];
                        decimal num5 = Convert.ToDecimal(obj3["ExpSubmitAmount"]);
                        obj3["LocExpSubmitAmount"] = num2 * num5;
                        num3 += num5;
                        base.View.UpdateView("FExpSubmitAmount", row);
                        row++;
                    }
                    base.View.Model.SetValue("FExpAmountSum", num3);
                    base.View.Model.SetValue("FLocExpAmountSum", num3 * num2);
                    this.UpdateEntrySrcOffSetAmount();
                }
                this.SetRequestTypeSelector();
                DynamicObjectCollection entityDataObject = base.View.Model.GetEntityDataObject(base.View.BusinessInfo.GetEntryEntity("FEntity"));
                bool flag = entityDataObject.Any<DynamicObject>(p => !string.IsNullOrWhiteSpace(Convert.ToString(p["SourceBillNo"])));
                this.isHassourceBill = flag;
                bool flag2 = entityDataObject.Sum<DynamicObject>(((Func<DynamicObject, int>)(p => Convert.ToInt32(p["IsFromBorrow"])))) > 0;
                if (flag && flag2)
                {
                    base.View.GetControl("FISCOSTOFOUTORG").Enabled = false;
                    base.View.GetControl("FCONTACTUNIT").Enabled = false;
                    base.View.GetControl("FCONTACTUNITTYPE").Enabled = false;
                    base.View.GetControl("FSRCBORROWAMOUNT").Visible = true;
                }
                else
                {
                    base.View.GetControl("FSRCBORROWAMOUNT").Visible = false;
                }
                if (flag)
                {
                    base.View.GetControl("FEXPID").Enabled = false;
                }
                if (flag && bool.Parse(base.View.Model.DataObject["SPLITENTRY"].ToString()))
                {
                    base.View.GetControl("FEXPID").Enabled = true;
                }
                if (flag2)
                {
                    base.View.Model.SetValue("FRealPay", 0);
                    base.View.GetControl("FRealPay").Enabled = false;
                }
                else
                {
                    base.View.GetControl("FRealPay").Enabled = true;
                }
                if (this.isHassourceBill)
                {
                    DynamicObjectCollection source = this.Model.GetEntityDataObject(this.Model.BusinessInfo.GetEntity("FEntity"));
                    EntryEntity entryEntity = this.Model.BusinessInfo.GetEntryEntity("FEntity");
                    DBServiceHelper.LoadReferenceObject(base.Context, source.ToArray<DynamicObject>(), entryEntity.DynamicObjectType, false);
                    base.View.UpdateView("FEntity");
                }
                this.SetFieldTitleByRequestType();
            }
        }

        private void SetRequestTypeSelector()
        {
            string str2 = base.View.Model.GetValue("FRequestType").ToString();
            if (str2 != null)
            {
                if (str2 != "1")
                {
                    if (str2 != "2")
                    {
                        return;
                    }
                }
                else
                {
                    base.View.Model.BeginIniti();
                    base.View.Model.SetValue("FPayBox", true);
                    base.View.Model.EndIniti();
                    base.View.UpdateView("FPayBox");
                    return;
                }
                base.View.Model.BeginIniti();
                base.View.Model.SetValue("FrefundBox", true);
                base.View.Model.EndIniti();
                base.View.UpdateView("FrefundBox");
            }
        }

        private void SetDefaultExpenseOrg()
        {
            if ((this.MainOrg != null) && Kingdee.K3.FIN.ER.Business.PlugIn.CommonUtil.CheckOrgFunction(this.MainOrg, "107"))
            {
                base.View.Model.SetValue("FEXPENSEORGID", this.MainOrg);
            }
            else
            {
                base.View.Model.SetValue("FEXPENSEORGID", null);
            }
        }


        private void Setdefaultfsettletypeidvalue()
        {
            long num = Kingdee.K3.FIN.ER.Business.PlugIn.CommonUtil.SetDefaultSettletypeId(base.Context);
            if (num != 0L)
            {
                switch (base.View.Model.GetValue("FRequestType").ToString())
                {
                    case "1":
                    case "2":
                        base.View.Model.SetValue("FPaySettlleTypeID", num);
                        return;
                       // goto Label_0079;不更新银行帐号
                }
                base.View.Model.SetValue("FPaySettlleTypeID", null);
            }
        /*不更新银行帐号
        Label_0079:
            this.ResetBankInfo();
            this.UpdateControlBankInfo();*/
        }


        private void SetDefaultValues(long orgId)
        {
            DynamicObject obj2 = CommonServiceHelper.GetUserLinkEmployee(base.Context, base.Context.UserId, orgId);
            if (obj2 != null)
            {
                base.View.Model.SetValue("FPROPOSERID", obj2["FEmpID"]);
                base.View.Model.SetValue("FREQUESTDEPTID", obj2["FDeptID"]);
                if (base.View.Model.GetValue("FEXPENSEORGID") != null)
                {
                    base.View.Model.SetValue("FCONTACTUNIT", obj2["FEmpID"]);
                }
            }
            base.View.Model.SetValue("FCURRENCYID", base.View.Model.GetValue("FLOCCURRENCYID"));
        }


        private void SetProposerPhone()
        {
            long userId = Convert.ToInt64(this.Model.DataObject["ProposerID_Id"]);
            string staffLinkTel = CommonServiceHelper.GetStaffLinkTel(base.Context, userId);
            if (!string.IsNullOrWhiteSpace(staffLinkTel))
            {
                this.Model.SetValue("FContactPhoneNo", staffLinkTel);
            }
        }


        private void UpdateEntrySrcOffSetAmount()
        {
            string str = base.View.Model.GetValue("FRequestType").ToString();
            DynamicObjectCollection entityDataObject = base.View.Model.GetEntityDataObject(base.View.BusinessInfo.GetEntryEntity("FEntity"));
            string str2 = base.View.Model.GetValue("FDocumentStatus").ToString();
            foreach (DynamicObject obj2 in entityDataObject)
            {
                obj2["SrcOffsetAmount"] = 0;
            }
            List<long> list = (from p in entityDataObject select Convert.ToInt64(p["SosurceRowID"])).Distinct<long>().ToList<long>();
            decimal num = 0M;
            using (List<long>.Enumerator enumerator2 = list.GetEnumerator())
            {
                while (enumerator2.MoveNext())
                {
                    Func<DynamicObject, bool> predicate = null;
                    long srcRowID = enumerator2.Current;
                    if (predicate == null)
                    {
                        predicate = p => Convert.ToInt64(p["SosurceRowID"]) == srcRowID;
                    }
                    List<DynamicObject> source = entityDataObject.Where<DynamicObject>(predicate).ToList<DynamicObject>();
                    decimal num2 = source.Sum<DynamicObject>((Func<DynamicObject, decimal>)(p => Convert.ToDecimal(p["SrcBorrowAmount"])));
                    for (int i = 0; i < source.Count; i++)
                    {
                        DynamicObject obj3 = source[i];
                        int num4 = Convert.ToInt32(obj3["Seq"]);
                        bool flag = Convert.ToBoolean(obj3["isFromBorrow"]);
                        decimal num5 = 0M;
                        decimal num6 = 0M;
                        if (source.Any<DynamicObject>(p => !string.IsNullOrWhiteSpace(Convert.ToString(p["SourceBillNo"]))))
                        {
                            if (!flag)
                            {
                                num5 = Convert.ToDecimal(obj3["ExpSubmitAmount"]);
                                if (str.Equals("1") || str.Equals("2"))
                                {
                                    num6 = num5;
                                }
                                base.View.Model.SetValue("FSrcOffsetAmount", num5, num4 - 1);
                                continue;
                            }
                            decimal num7 = Convert.ToDecimal(obj3["ExpSubmitAmount"]);
                            Convert.ToDecimal(obj3["SrcOffsetAmount"]);
                            decimal num8 = source.Sum<DynamicObject>((Func<DynamicObject, decimal>)(p => Convert.ToDecimal(p["SrcOffsetAmount"])));
                            switch (str)
                            {
                                case "2":
                                    if ((num2 - num8) > num7)
                                    {
                                        if (i < (source.Count - 1))
                                        {
                                            num5 = num7;
                                        }
                                        else
                                        {
                                            num5 = num2 - num8;
                                        }
                                    }
                                    else
                                    {
                                        num5 = num2 - num8;
                                    }
                                    num6 = num5 - num7;
                                    break;

                                case "1":
                                    if ((num2 - num8) > num7)
                                    {
                                        if (i < (source.Count - 1))
                                        {
                                            num5 = num7;
                                        }
                                        else
                                        {
                                            num5 = num2 - num8;
                                        }
                                    }
                                    else
                                    {
                                        num5 = num2 - num8;
                                    }
                                    num6 = num7 - num5;
                                    break;

                                default:
                                    if ((num2 - num8) > num7)
                                    {
                                        num5 = num7;
                                    }
                                    else
                                    {
                                        num5 = num2 - num8;
                                    }
                                    break;
                            }
                            base.View.Model.SetValue("FSrcOffsetAmount", num5, num4 - 1);
                            base.View.Model.SetValue("FReqSubmitAmount", num6, num4 - 1);
                            if (!str2.Equals("B"))
                            {
                                base.View.Model.SetValue("FRequestAmount", num6, num4 - 1);
                            }
                            num += num6;
                            continue;
                        }
                        num5 = Convert.ToDecimal(obj3["ExpSubmitAmount"]);
                        if (str.Equals("1") || str.Equals("2"))
                        {
                            num6 = num5;
                        }
                        base.View.Model.SetValue("FReqSubmitAmount", num6, num4 - 1);
                        if (!str2.Equals("B"))
                        {
                            base.View.Model.SetValue("FRequestAmount", num6, num4 - 1);
                        }
                    }
                }
            }
        }


        private void SetFieldTitleByRequestType()
        {
            string str2 = base.View.Model.GetValue("FRequestType").ToString();
            if (str2 != null)
            {
                if (str2 != "1")
                {
                    if (str2 != "2")
                    {
                        return;
                    }
                }
                else
                {
                    base.View.GetControl<FieldEditor>("FRequestAmount").Text = ResManager.LoadKDString("申请付款金额", "003832000011862", SubSystemType.FIN, new object[0]);
                    base.View.GetControl<FieldEditor>("FReqSubmitAmount").Text = ResManager.LoadKDString("核定付款金额", "003832000011863", SubSystemType.FIN, new object[0]);
                    base.View.GetControl("FReqAmountSum").SetCustomPropertyValue("Title", ResManager.LoadKDString("核定付款金额汇总", "003832000011864", SubSystemType.FIN, new object[0]));
                    base.View.GetControl("FLocReqAmountSum").SetCustomPropertyValue("Title", ResManager.LoadKDString("核定付款金额本位币", "003832000011865", SubSystemType.FIN, new object[0]));
                    return;
                }
                base.View.GetControl<FieldEditor>("FRequestAmount").Text = ResManager.LoadKDString("申请退款金额", "003832000011866", SubSystemType.FIN, new object[0]);
                base.View.GetControl<FieldEditor>("FReqSubmitAmount").Text = ResManager.LoadKDString("核定退款金额", "003832000011867", SubSystemType.FIN, new object[0]);
                base.View.GetControl("FReqAmountSum").SetCustomPropertyValue("Title", ResManager.LoadKDString("核定退款金额汇总", "003832000011868", SubSystemType.FIN, new object[0]));
                base.View.GetControl("FLocReqAmountSum").SetCustomPropertyValue("Title", ResManager.LoadKDString("核定退款金额本位币", "003832000011869", SubSystemType.FIN, new object[0]));
            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            //base.DataChanged(e);
            switch (e.Field.Key.ToUpperInvariant())
            {
                case "FORGID":
                    if (this.MainOrg != null)
                    {
                        Kingdee.K3.FIN.ER.Business.PlugIn.CommonUtil.SetDefLocalCurrencyAndExchangeType(base.View, "FORGID", "FLOCCURRENCYID", "FExchangeTypeID", 0);
                        this.SetDefaultValues(long.Parse(this.MainOrg["ID"].ToString()));
                        this.SetDefaultExpenseOrg();
                        return;
                    }
                    return;

                case "FCURRENCYID":
                case "FDATE":
                    Kingdee.K3.FIN.ER.Business.PlugIn.CommonUtil.SetExchangeRate(base.View, "FDATE", "FCURRENCYID", "FLOCCURRENCYID", "FExchangeTypeID", "FEXCHANGERATE", 0);
                    return;

                case "FCONTACTUNIT":
                case "FPAYSETTLLETYPEID":
                case "FREFUNDSETTLLETYPEID":
                    this.paySettlleTypeIDOldValue = e.OldValue;
                    //this.ResetBankInfo();
                    //this.UpdateControlBankInfo();取消更新银行信息
                    return;

                case "FEXPENSEORGID":
                    {
                        DynamicObject org = base.View.Model.GetValue("FEXPENSEORGID") as DynamicObject;
                        base.View.Model.SetValue("FExpenseDeptID", null);
                        if (((base.View.Model.GetValue("FPayOrgId") is DynamicObject) || (org == null)) || !Kingdee.K3.FIN.ER.Business.PlugIn.CommonUtil.CheckOrgFunction(org, "110"))
                        {
                            break;
                        }
                        base.View.Model.SetValue("FPayOrgId", Convert.ToInt64(org["ID"]));
                        return;
                    }
                case "FPROPOSERID":
                    this.SetProposerPhone();
                    this.SetRequestDept();
                    return;

                case "FEXPID":
                case "FINVOICETYPE":
                    this.SetTaxRateValue(e);
                    return;

                case "FSHOWLOCAMOUNT":
                    base.View.Model.DataChanged = this._dataChanged;
                    return;

                case "FREQUESTTYPE":
                    this.SetFieldTitleByRequestType();
                    this.Setdefaultfsettletypeidvalue();
                    //this.UpdateEntrySrcOffSetAmount();已在JN_YDL_ExpReimbursementEdit有控制
                    return;

                case "FSPLITENTRY":
                    if (!this.isHassourceBill)
                    {
                        break;
                    }
                    if (!bool.Parse(base.View.Model.DataObject["SPLITENTRY"].ToString()))
                    {
                        base.View.GetControl("FEXPID").Enabled = false;
                        return;
                    }
                    base.View.GetControl("FEXPID").Enabled = true;
                    return;
                /*
                case "FEXPSUBMITAMOUNT":
                    this.UpdateEntrySrcOffSetAmount();
                    break;*/

                default:
                    return;
            }
        }

        private void ResetBankInfo()
        {
            DynamicObject obj2 = base.View.Model.DataObject["CONTACTUNIT"] as DynamicObject;
            string unitType = base.View.Model.GetValue("FCONTACTUNITTYPE").ToString();
            if (obj2 != null)
            {
                long unitId = Convert.ToInt64(obj2["Id"]);
                DynamicObjectCollection source = Kingdee.K3.FIN.ER.Business.PlugIn.CommonUtil.GetUniterBankInfo(base.Context, unitType, unitId);
                this.ContactBankInfo = null;
                if ((source != null) && (source.Count != 0))
                {
                    foreach (DynamicObject obj3 in source)
                    {
                        if (Convert.ToBoolean(obj3["FBANKISDEFAULT"]))
                        {
                            this.ContactBankInfo = obj3;
                            return;
                        }
                    }
                    if (this.ContactBankInfo == null)
                    {
                        this.ContactBankInfo = source.First<DynamicObject>();
                    }
                }
            }
        }

        private void SetRequestDept()
{
    if (!this.isHassourceBill)
    {
        long num = Convert.ToInt64(base.View.Model.DataObject["ProposerID_Id"]);
        if (num != 0L)
        {
            string strSQL = string.Format( "select FDEPTID from  T_BD_STAFF where FEMPINFOID={0} AND FUSEORGID={1} AND FFORBIDSTATUS='A' AND FDOCUMENTSTATUS='C' ORDER BY FSTAFFID ",num,base.View.Model.DataObject["OrgID_Id"].ToString());
            DynamicObjectCollection col = DBServiceHelper.ExecuteDynamicObject(base.Context, strSQL, null, null, CommandType.Text, new SqlParam[0]);
            if (col.IsEmpty<DynamicObject>())
            {
                base.View.Model.SetValue("FRequestDeptID", null);
            }
            else
            {
                base.View.Model.SetItemValueByID("FRequestDeptID", col[0]["FDEPTID"], -1);
            }
        }
    }
}

 
        private void SetTaxRateValue(DataChangedEventArgs e)
{
    if (Convert.ToString(base.View.Model.GetValue("FINVOICETYPE", e.Row)).Equals("1"))
    {
        DynamicObject obj2 = base.View.Model.GetValue("FEXPID", e.Row) as DynamicObject;
        if (obj2 != null)
        {
            DynamicObject obj3 = obj2["rate"] as DynamicObject;
            if (obj3 != null)
            {
                QueryBuilderParemeter para = new QueryBuilderParemeter {
                    FormId = "BD_TaxRate",
                    SelectItems = SelectorItemInfo.CreateItems("FTaxRate"),
                    FilterClauseWihtKey = string.Format(" FID={0} ",Convert.ToInt64(obj3["ID"]))
                };
                DynamicObject obj4 = QueryServiceHelper.GetDynamicObjectCollection(base.Context, para, null).FirstOrDefault<DynamicObject>();
                if (obj4 != null)
                {
                    base.View.Model.SetValue("FTaxRate", Convert.ToDecimal(obj4["FTaxRate"]), e.Row);
                }
            }
        }
    }
}

 

 




    }

}
