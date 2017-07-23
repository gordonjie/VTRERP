using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.BOS.Orm.DataEntity;

namespace VTR.K3.Bill.PlugIn
{
    [System.ComponentModel.Description("判断是否跨部门移仓")]
    public class IsspanDepartment : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            bool Doget1 = e.Field.Key.EqualsIgnoreCase("FSRCSTOCKID");
            bool Doget2 = e.Field.Key.EqualsIgnoreCase("FDESTSTOCKID");
            bool ISspan1 = false;
            bool ISspan2 = false;

            if (Doget1 || Doget2)
            {
                var yyy = this.View.Model.GetValue("FSRCSTOCKID", 0) as DynamicObject;
                var zzz = this.View.Model.GetValue("FDESTSTOCKID", 0) as DynamicObject;
                string[] store = { "成品一车间现场仓","印刷车间"}; 
                string yyyname =yyy["Name"].ToString();

                if (Array.IndexOf(store,yyy["Name"].ToString()) !=-1)
                {
                     ISspan1 = true;
                }

                   if (Array.IndexOf(store,zzz["Name"].ToString()) !=-1)
                {
                     ISspan2 = true;
                }

                    if(ISspan1 && ISspan2){

                        this.View.Model.SetValue("FISspan",false);
                    }
               
            }


        }
    }
}
