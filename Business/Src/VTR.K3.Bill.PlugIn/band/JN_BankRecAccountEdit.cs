using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.Core;
using Kingdee.K3.FIN.WB.ServiceHelper;
using Kingdee.K3.FIN.WB.Common.Core;
using Kingdee.K3.FIN.WB.Business.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.NetworkCtrl;
using Kingdee.K3.FIN.WB.Business.PlugIn.Common;

namespace VTR.K3.Bill.PlugIn.band
{
    public class JN_BankRecAccountEdit : WithEbankServiceEdit
    {
        private List<NetworkCtrlResult> netWorkCtrlResult;
        private List<long> selectPKID;


        /*
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            // base.BarItemClick(e);
            base.SendSMSTitle = base.multiLangeConst.AuditForPayBill;
            base.auditAmountKey = "FREALPAYAMOUNTFOR_H";
            //base.BarItemClick(e);
            if (StringUtils.EqualsIgnoreCase(e.BarItemKey, "tbSplitApprove") || StringUtils.EqualsIgnoreCase(e.BarItemKey, "tbApprove"))
            {
                long num = BillExtension.GetBaseDataID(base.View.Model, "FPAYORGID", 0);
                if (SystemParaHelperWB.GetSendSMSForAudit(base.Context, num))
                {
                    e.Cancel=true;
                    decimal num2 = 0M;
                    num2 = CommonCoreFuncWb.SubDecimalValue(base.View.Model.GetValue(this.auditAmountKey), 2);
                    string funTitle = string.Format(this.SendSMSTitle, num2);
                    CommonFunWB.SendSMSFuncForAudit(base.View, funTitle, base.Context);
                }
            }
            if (!e.Cancel && !this.IsPermissionOperationByBarItemName(e.BarItemKey))
            {
                e.Cancel=true;
                base.View.ShowWarnningMessage(this.sPermissionMessage, "", 0, null, MessageBoxType.Advise);
            }


        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            if (StringUtils.EqualsIgnoreCase(e.Operation.FormOperation.Operation, "SubmitBank"))
            {
                e.Cancel=true;
                this.DoActionUnderNetworkControl(this.ConvertOperationToBarItemKey(e.Operation.FormOperation.Operation), e);
            }
            if (StringUtils.EqualsIgnoreCase(e.Operation.FormOperation.Operation, "CancelWB"))
            {
                //e.Cancel = true;
                //this.DoActionUnderNetworkControl(this.ConvertOperationToBarItemKey(e.Operation.FormOperation.Operation), e);
            }
            if (StringUtils.EqualsIgnoreCase(e.Operation.FormOperation.Operation, "UnAudit"))
            {
                this.DoActionUnderNetworkControl("TBREJECT", e);
            }
        }

        protected override void DoCancelBankAction(List<NetworkCtrlResult> netCtrlResult)
        {
             BillExtension.GetBaseDataID(this.Model, "FACCOUNTSYSTEM", 0);
             long num = Convert.ToInt64(base.View.Model.GetPKValue());
            // CancelFromSubmit cancelobject = new CancelFromSubmit(base.Context, base.View, this.netWorkCtrlResult);
             //cancelobject.FindEntryID(new List<long> { num });
            
        }*/

    }
}
