using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.Bandservice
{
    public class cancelsubmit : JNBand
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {

            e.FieldKeys.Add("FBankStatus");
            e.FieldKeys.Add("FSubmitStatus");
            e.FieldKeys.Add("F_JNOBSSID");

        }

        public override void BeginOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeginOperationTransactionArgs e)
        {
            var billGroups = e.DataEntitys;
            foreach (var billGroup in billGroups)
            {
                DynamicObjectCollection BILLENTRYDATAs = billGroup["PAYBILLENTRY"] as DynamicObjectCollection;
                foreach (var BILLENTRYDATA in BILLENTRYDATAs)
                {

                    string FBandStatus = Convert.ToString(BILLENTRYDATA["BankStatus"]);

                    string FSubmitStatus = Convert.ToString(BILLENTRYDATA["SubmitStatus"]);

                    Int32 EntryID = Convert.ToInt32(BILLENTRYDATA["Id"]);

                    if ((FBandStatus == "D" || FBandStatus == "E") && FSubmitStatus == "B")
                    {
                        BusinessDataServiceHelper.SetState(this.Context, "T_AP_PAYBILLENTRY_B", "FBankStatus", "A", "FEntryID", new object[] { EntryID });


                        BusinessDataServiceHelper.SetState(this.Context, "T_AP_PAYBILLENTRY_B", "FSubmitStatus", "A", "FEntryID", new object[] { EntryID });


                    }


                }


            }
        }
    }
}
