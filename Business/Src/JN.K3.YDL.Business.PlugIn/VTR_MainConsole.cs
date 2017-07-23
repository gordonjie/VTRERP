using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn
{
    public class VTR_MainConsole : AbstractDynamicFormPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            
            base.AfterBindData(e);
            //this.View.GetControl("F_VTR_Label").ControlAppearance.TextColor = "255,255,255";
        }
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Key.ToString() == "F_VTR_Combo")
            {
                if (e.NewValue.ToString() == "B")
                {
                    this.View.AddAction("ShowWebURL", "http://mail.vtrbio.com");
                    //System.Diagnostics.Process.Start("http://www.baidu.com");  
                }
                else if(e.NewValue.ToString() == "C")
                {
                    this.View.AddAction("ShowWebURL", "http://218.104.198.194:8086/rap/");
                    //System.Diagnostics.Process.Start("http://218.104.198.194/rap/");  
                }

            }
        }
    }
}
