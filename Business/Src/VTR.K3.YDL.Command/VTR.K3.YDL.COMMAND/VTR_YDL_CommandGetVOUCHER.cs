using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.List.PlugIn;

namespace VTR.K3.YDL.COMMAND
{
      [Description("获取凭证号")]

    public class VTR_YDL_CommandGetVOUCHER : AbstractListPlugIn
    {
       

        public void GetvoucherNo()
        { 



        }
        public Int32 updatevoucherNo(string tableA, string tableB)
        {
            string strSql = string.Format(@"/*dialect*/update {0} set FNOTE=t2.FVOUCHERGROUPNO from T_STK_MISDELIVERY as t1 ,(SELECT FID,    
       FVOUCHERGROUPNO=( SELECT FVOUCHERGROUPNO +''    
               FROM {1} b    
               WHERE b.FID = a.FID    
               FOR XML PATH(''))   
FROM {2} AS a   
GROUP BY FID)as  t2 where t1.FID=t2.FID", tableA, tableB, tableB);
           return DBUtils.Execute(this.Context, strSql);
        }

    }
}
