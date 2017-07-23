using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.K3.FIN.IV.Business.PlugIn;

namespace VTR.K3.YDL.BillPlugIn.FIN
{
    public class VTRIVedit : IVEdit
    {
        public override void EntityRowClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.EntityRowClickEventArgs e)
        {
            base.EntityRowClick(e);
            if (e.Row >= 0)
            {
                if ((this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "Z" || this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "A" || this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "B") && this.FormID.Contains("IV_S") && base.View.Model.GetValue("FSRCBILLTYPEID").ToString().Contains("IV_S"))
                {
                    foreach (string str in new List<string> { "tbDeleteLine" })
                    {
                        this.View.GetBarItem(this.EntityKey, str).Enabled=true;
                    }
                }
                if ((this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "Z" || this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "A" || this.View.Model.GetValue("FDOCUMENTSTATUS").ToString() == "B") && this.FormID.Contains("IV_P") && base.View.Model.GetValue("FSOURCETYPE").ToString().Contains("IV_P"))
                {
                    foreach (string str in new List<string> { "tbDeleteLine"})
                    {
                        this.View.GetBarItem(this.EntityKey, str).Enabled = true;
                    }
                }
            }

        }


 

    }
}
