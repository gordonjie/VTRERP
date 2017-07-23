using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.NetworkCtrl;
using Kingdee.K3.FIN.CN.Business.PlugIn;
using Kingdee.K3.FIN.CN.Business.PlugIn.BankAcntCheck;
using Kingdee.K3.FIN.CN.ServiceHelper;
using Kingdee.K3.FIN.ServiceHelper;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.K3.FIN.Core;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;
using System.ComponentModel;

namespace VTR.K3.YDL.BusinessPlugIn.VTRBankAcntCheck
{
    public class VTRBankAcntCheck : BankAcntCheck
    {
        [Description("扩展-银行存款对账插件")]

        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            //增加应收票据结算单摘要
            if (base.downDataList!=null)
            {
            var downdata = base.downDataList;

                foreach (var obj in downdata)
                {
                    if (obj["FBILLNOKEY"].ToString().Substring(0, 4) == "BRJS")
                    {
                        string sql = string.Format(@"/*dialect*/select tb1.FBILLNO,FBILLNUMBER=STUFF((select ','+FBILLNUMBER from  t_CN_BILLRECSETTLE tb2
where tb1.FBILLNO=tb2.FBILLNO
 FOR XML PATH('')), 1, 1, '')
from  t_CN_BILLRECSETTLE tb1
 where tb1.FBILLNO='{0}'
group by tb1.FBILLNO", obj["FBILLNOKEY"].ToString());
                        var selectdatas = DBUtils.ExecuteDynamicObject(this.Context, sql);
                        if (selectdatas.Count > 0)
                        {
                            obj["fexplanationkey"] = selectdatas[0]["FBILLNUMBER"];
                        }
                    }
                }
            }
        }
        

    }

 

}
