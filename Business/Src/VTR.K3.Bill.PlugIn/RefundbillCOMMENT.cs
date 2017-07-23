using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill.PlugIn;

namespace VTR.K3.Bill.PlugIn.Properties
{
    [System.ComponentModel.Description("更新付款退款单中的备注等于关联单备注")]
    public class RefundbillCOMMENT : AbstractBillPlugIn
    {



        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);



            int Row = this.Model.GetEntryRowCount("FREFUNDBILLENTRY");
            for (int i = 0; i < Row; i++)
            {

                string isGo = String.Format("{0}", this.View.Model.GetValue("FNOTE", i));


                if (isGo == "" || isGo == " " || isGo == null)
                {

                    string NOTE = " ";
                    if (i == 0)
                    {
                        string isGo2 = String.Format("{0}", this.View.Model.GetValue("FSRCNOTE", i));

                        if (isGo2 == " " || isGo2 == null)
                        { NOTE = String.Format("{0}", this.View.Model.GetValue("F_kk_Text")); }
                        else
                            NOTE = String.Format("{0}", this.View.Model.GetValue("FSRCNOTE", i));
                    }
                    else
                    {
                        NOTE = String.Format("{0}", this.View.Model.GetValue("FSRCNOTE", i));
                    }

                    this.View.Model.SetValue("FNOTE", NOTE, i);

                }


            }
        }


    }
}
