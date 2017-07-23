using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.List.PlugIn;


namespace VTR.K3.YDL.BillPlugIn.SCM
{
    [Description("获取凭证号")]
    public class VTR_YDL_STK_GetVOUCHER : AbstractListPlugIn
    {
        public override void AfterDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string operation = e.Operation.Operation;
            if (operation != "GetvoucherNo") return;
            this.GetvoucherNo();
            e.OperationResult.IsShowMessage = false;
        }

        public  void GetvoucherNo()
        {
            //string tablename = this.View.Model.BusinessInfo.Entrys[0].TableName.ToString();
           // var tablename = this.View.Model.BillBusinessInfo;
           // var table = this.View.Model.BillBusinessInfo.Entrys[0];
            string basetable = this.View.Model.BillBusinessInfo.Entrys[0].TableName.ToString();
            string vouchertable = basetable+"_VH";
            Int32 row = 0;
            row = this.updatevoucherNo(basetable, vouchertable);
            this.View.ShowErrMessage("共更新" + row.ToString() + "条数据!");

        }

        private Int32 updatevoucherNo(string tableA, string tableB)
        {
            string clearsql = string.Format(@"/*dialect*/update {0} set FvoucherNo =''",tableA);
            DBUtils.Execute(this.Context, clearsql);

            string strSql = string.Format(@"/*dialect*/update {0} set FvoucherNo = t2.FVOUCHERGROUPNO from {1} as t1 ,(SELECT FID,    
       FVOUCHERGROUPNO=( SELECT FVOUCHERGROUPNO +''    
               FROM {2} b    
               WHERE b.FID = a.FID    
               FOR XML PATH(''))   
FROM {3} AS a   
GROUP BY FID)as  t2 where t1.FID=t2.FID", tableA, tableA, tableB, tableB);
            return DBUtils.Execute(this.Context, strSql);
        }
    }
}
