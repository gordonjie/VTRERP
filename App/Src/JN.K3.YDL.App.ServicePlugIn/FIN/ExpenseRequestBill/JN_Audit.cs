using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Orm.DataEntity;

namespace JN.K3.YDL.App.ServicePlugIn.FIN.ExpenseRequestBill
{
  
        /// <summary>
        /// 费用报销单审核插件
        /// </summary>
        public class JN_Audit : AbstractOperationServicePlugIn
        {
            /// <summary>
            /// 添加服务插件可能操作到的字段
            /// </summary>
            /// <param name="e"></param>
            public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
            {
                e.FieldKeys.Add("FBillNo");
                e.FieldKeys.Add("FBilltype");
                e.FieldKeys.Add("FIsBorrow");
                e.FieldKeys.Add("F_JN_OrgBorrowAmount");
                e.FieldKeys.Add("F_JN_checkedBorrowAmount");
                e.FieldKeys.Add("FIsBorrow");


                e.FieldKeys.Add("FExpenseItemID");
                e.FieldKeys.Add("FOrgAmount");
                e.FieldKeys.Add("FCheckedOrgAmount");
                e.FieldKeys.Add("F_JN_LISTCHECKEDBORROWAMOUNT");

            }


            /// <summary>
            /// 更新批准价格
            /// </summary>
            /// <param name="e"></param>
            public override void BeginOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeginOperationTransactionArgs e)
            {
                base.BeginOperationTransaction(e);
                if (e.DataEntitys == null) return;
              
                var billdata = e.DataEntitys[0];
                if (billdata["IsBorrow"].ToString()=="False") return;
                DynamicObjectCollection Entitydata = billdata["FEntity"] as DynamicObjectCollection;
                double checkedBorrowAmount =Convert.ToDouble(billdata["F_JN_checkedBorrowAmount"]);
                double BorrowAmount = checkedBorrowAmount;
                double listBorrowAmount=0;
                int countrows=Entitydata.Count;
                for (int i = 0; i < countrows; i++)
                {
                    //计算明细的借支金额，表头借支金额自上而下分摊到明细借支金额
                    double CheckedOrgAmount = Convert.ToDouble(Entitydata[i]["CheckedOrgAmount"]);
                    if (BorrowAmount > 0 && BorrowAmount<=CheckedOrgAmount) 
                    {listBorrowAmount=BorrowAmount;}
                    else if(BorrowAmount <= 0 )
                    {listBorrowAmount=0;}
                    else
                    {
                    listBorrowAmount=CheckedOrgAmount;}
                    //当明细为最后一项时，明细借支金额=余额
                    if(i == countrows-1)
                    { listBorrowAmount = BorrowAmount; }
                    BorrowAmount = BorrowAmount - CheckedOrgAmount;
                    
                    string fid = Convert.ToString(Entitydata[i]["id"]);
                    string sql=string.Format(@"update T_ER_ExpenseRequestEntry set F_JN_LISTCHECKEDBORROWAMOUNT ={0}
                                                    where FENTRYID={1} ", listBorrowAmount.ToString(), fid);
                    DBUtils.Execute(this.Context, sql);
                    

                }

     
            }
        }
     
       
 }