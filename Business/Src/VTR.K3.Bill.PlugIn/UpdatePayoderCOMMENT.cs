using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill.PlugIn;

namespace VTR.K3.Bill.PlugIn.Properties
{
      [System.ComponentModel.Description("更新付款单中的备注等于关联单备注")]
    public class UpdatePayoderCOMMENT : AbstractBillPlugIn
    {
       


          public override void  BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
{
 	 base.BeforeSave(e);



              int Row = this.Model.GetEntryRowCount("FPAYBILLENTRY");
              for (int i = 0; i < Row; i++)
              {

                  string isGo = String.Format("{0}", this.View.Model.GetValue("FCOMMENT", i));
                  

                  if (isGo == "" || isGo == " " || isGo == null)
                  {  
               
                       string COMMENT = " ";
                      if (i == 0)
                      {
                          string isGo2 = String.Format("{0}", this.View.Model.GetValue("FSRCCOMMENT", i));

                          if (isGo2 == " " || isGo2 == null)
                          {COMMENT = String.Format("{0}", this.View.Model.GetValue("F_kk_Text"));}
                          else
                              COMMENT = String.Format("{0}", this.View.Model.GetValue("FSRCCOMMENT", i));
                      }
                      else
                      {
                          COMMENT = String.Format("{0}", this.View.Model.GetValue("FSRCCOMMENT", i));
                      }

                      this.View.Model.SetValue("FCOMMENT", COMMENT, i);

                 }


              }
          }
         

    }
}
