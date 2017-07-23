using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using JN.K3.YDL.ServiceHelper;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    [Description("银企对接接口设置-表单插件")]
    public class JN_BANDPAYSET : AbstractBillPlugIn
    {
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string operation=e.Operation.Operation.ToString();
            if (operation == "TestSet")
            {
                string url = Convert.ToString( this.View.Model.GetValue("F_JN_ServiceURL"));
                string F_JNtermid= Convert.ToString(this.View.Model.GetValue("F_JNtermid"));
                string F_JNCustid= Convert.ToString(this.View.Model.GetValue("F_JNCustid"));
                string F_JNCusopr = Convert.ToString(this.View.Model.GetValue("F_JNCusopr"));
                string F_JNOprpwd = Convert.ToString(this.View.Model.GetValue("F_JNOprpwd"));
                DateTime currentTime = new System.DateTime();
                currentTime = System.DateTime.Now;
                string F_JNcustdt = string.Format("{0:yyyyMMddHHmmss}", currentTime);
                string xmlMsg = string.Format(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<bocb2e version=""100"" security=""true"" lang=""chs"">
<head>
<termid>{0}</termid>
<trnid>20060704001</trnid>
<custid>{1}</custid>
<cusopr>{2}</cusopr>
<trncod>b2e0001</trncod>
</head>
<trans>
<trn-b2e0001-rq>
<b2e0001-rq>
<custdt>{3}</custdt>
<oprpwd>{4}</oprpwd>
</b2e0001-rq>
</trn-b2e0001-rq>
</trans>
</bocb2e>
", F_JNtermid, F_JNCustid, F_JNCusopr, F_JNcustdt, F_JNOprpwd); 
                string Xmldata = YDLCommServiceHelper.BandPost(this.Context, url, xmlMsg, 600);
                this.View.Model.SetValue("F_JNTestRemark", Xmldata);
                this.View.UpdateView("F_JNTestRemark");
            }
        }
    }
}
