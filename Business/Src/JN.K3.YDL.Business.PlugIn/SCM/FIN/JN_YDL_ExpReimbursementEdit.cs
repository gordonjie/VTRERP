using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    [Description("费用报销单表单插件")]
    public class JN_YDL_ExpReimbursementEdit : AbstractBillPlugIn
    {
        Decimal oldvalue = 0;
  
       public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
       {
           base.DataChanged(e);

           //var PROPOSERID = this.View.Model.GetValue("FPROPOSERID");
           //var OrgID = Convert.ToString(this.View.Model.GetValue("FOrgID"));
           if (e.Field.Key.ToUpper() == "FPROPOSERID")
           {
               getApplicantId();
           }
           if (e.Key.ToUpper().Equals("FREQAMOUNTSUM")|| e.Key.ToUpper().Equals("FEXPAMOUNTSUM") || e.Key.ToUpper().Equals("FSRCPAYEDAMOUNTALL"))
           {
               upatepayorback();
           }
           if (e.Key.ToUpper().Equals("FCONTACTUNIT"))
           {
               this.View.InvokeFieldUpdateService("FCONTACTUNIT", 0);
            }
           if (e.Key.ToUpper().Equals("FCAUSA"))
           {
               string remark=Convert.ToString(this.View.Model.GetValue("FCAUSA"));
               int row = this.View.Model.GetEntryRowCount("FEntity");
               for (int i = 0; i < row; i++)
               {
                   if (Convert.ToString(this.View.Model.GetValue("FRemark", i)) == "")
                   {
                       this.View.Model.SetValue("FRemark", remark, i);
                   }
               }
           }
            if (e.Key.ToUpper().Equals("FEXPENSEAMOUNT"))
            {
                this.setpayamount();

            }

            if (e.Key.ToUpper().Equals("FEXPSUBMITAMOUNT"))//核定报销金额
            {
                this.setpayamount();

                /**增加当单据状态为暂存、新增、重新审核时保存原申请金额**/
                string billstatus= Convert.ToString(this.View.Model.GetValue("FDocumentStatus"));

                if (billstatus == "Z" || billstatus == "A" || billstatus == "D")
                {
                    Decimal oldvalue = Convert.ToDecimal(this.View.Model.GetValue("FReqReimbAmountSum"));
                    this.View.Model.SetValue("F_VTR_OldAmount", oldvalue);
                }
            }


      
           

        }
         /// <summary>
        /// 新增后
        /// </summary>
        /// <param name="e"></param>
       public override void AfterCreateModelData(EventArgs e)
       {
           int row = this.View.Model.GetEntryRowCount("FEntity");          
           string note =Convert.ToString( this.View.Model.GetValue("FCausa"));
           double allpayedAmount = getallpayed();
           double RUFFAmount = 0;//调整退款金额

           this.View.InvokeFieldUpdateService("FCONTACTUNIT", 0);//初始表体往来单位-调用表头更新事件
           this.View.Model.SetValue("FSRCPAYEDAMOUNTALL", allpayedAmount);

            for (int i = 0; i < row; i++)
           {
                //备注=事由
                string remark = Convert.ToString( this.View.Model.GetValue("FRemark", i));
               if (remark == "" || remark == " ")
           {
               this.View.Model.SetValue("FRemark", note, i);
           }
           this.getApplicantId();

           
           
           //费用承担部门=申请部门
           var dept = this.View.Model.GetValue("FRequestDeptID") as DynamicObject;
           if (dept != null)
           {
               this.View.Model.SetValue("FExpenseDeptID", Convert.ToInt32(dept["ID"]));
           }
           }
            this.setpayamount();

       }


        /**
         * 获取历史的付款金额，通过检查统计历史报销单的付款金额
         * */

        private double getallpayed()
        {
            string billentitytable = Convert.ToString(this.View.BusinessInfo.GetEntity("FEntity").TableName);
            int row = this.View.Model.GetEntryRowCount("FEntity");
            double srcborrowamountall = 0;//源单借支支付金额\
            double allpayedAmount = 0;
            List<string> secbillNo =new List<string>();
            for (int i = 0; i < row; i++)
            {
                string secbillNoi = Convert.ToString(this.View.Model.GetValue("FSOURCEBILLNO", i));
                if (secbillNo.Contains(secbillNoi) == false)
                {
                    secbillNo.Add(secbillNoi);
                }

            }
            int lenght = secbillNo.Count;
            for (int i = 0; i < lenght; i++)
            {
                double srcpushborrowamount = 0;
                double payedAmount = 0;
                double allpayedAmounti = 0;
                string sql = string.Format("select sum(FPUSHBORROWAMOUNT) from T_ER_ExpenseRequestEntry  t1 join T_ER_ExpenseRequest t2 on t1.FID=t2.fid where t2.FBILLNO ='{0}'", secbillNo[i]);
                var borrows = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                if (borrows != null && borrows.Count > 0)
                {
                    srcpushborrowamount = Convert.ToDouble(borrows[0][0]);
                }
                sql = string.Format("select sum(case when FRequestType =0 then FReqAmountSum else -1*FReqAmountSum end) from t_ER_ExpenseReimbEntry t1 join t_ER_ExpenseReimb t2 on t1.FID=t2.FID where FSOURCEBILLNO='{0}' group by FSOURCEBILLNO  ", secbillNo);
                var payeds = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                //srcborrowamountall = srcborrowamountall + srcpushborrowamount;
                if (payeds != null && payeds.Count > 0)
                {
                    payedAmount = Convert.ToDouble(payeds[0][0]);
                }
                allpayedAmounti = srcpushborrowamount + payedAmount;
                allpayedAmount = allpayedAmount + allpayedAmounti;
            }

            return allpayedAmount;
        }
       private void getApplicantId()
       {
           //初始化申请人
           var PROPOSER = this.View.Model.GetValue("FPROPOSERID") as DynamicObject;
           var Org = this.View.Model.GetValue("FOrgID") as DynamicObject;
           if (PROPOSER == null || Org == null) return;
           string PROPOSERID = PROPOSER["ID"].ToString();
          
           string OrgID = Org["ID"].ToString();
           QueryBuilderParemeter para = new QueryBuilderParemeter();
           para.FormId = "BD_NEWSTAFF";
           para.FilterClauseWihtKey = string.Format(" exists (select top 1 FSTAFFID from T_BD_STAFF where FEmpInfoId= {0} and FUseOrgId={1} )", PROPOSERID, OrgID);
           para.SelectItems = SelectorItemInfo.CreateItems(" FSTAFFID ");
           var employeeDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
           if (employeeDatas != null && employeeDatas.Count > 0)
           {
               this.View.Model.SetValue("FApplicantId", Convert.ToInt64(employeeDatas[0]["FSTAFFID"]));
           }
       }
       
        /**
         * 设置付退款金额
         * */
       private void setpayamount()
       {
           int row = this.View.Model.GetEntryRowCount("FEntity");
           double RUFFAmount = 0;//调整退款金额
           double allpayedAmount = getallpayed();
           this.View.Model.SetValue("FSRCPAYEDAMOUNTALL", allpayedAmount);

           for (int i = 0; i < row; i++)
           {

               double listpayedAmount = allpayedAmount;
               allpayedAmount = allpayedAmount - Convert.ToDouble(this.View.Model.GetValue("FEXPSUBMITAMOUNT", i));
               RUFFAmount = RUFFAmount + Convert.ToDouble(this.View.Model.GetValue("FEXPSUBMITAMOUNT", i));
               if (allpayedAmount >= 0 && listpayedAmount > 0)
               {
                   this.View.Model.SetValue("FSRCPAYEDAMOUNT", Convert.ToDouble(this.View.Model.GetValue("FExpenseAmount", i)), i);
               }
               else if (allpayedAmount < 0 && listpayedAmount > 0)
               {
                   this.View.Model.SetValue("FSRCPAYEDAMOUNT", listpayedAmount, i);
               }
               else
               {
                   this.View.Model.SetValue("FSRCPAYEDAMOUNT", 0, i);
               }
               if (i == row - 1)
               {
                   if (listpayedAmount >= 0)
                   {
                       this.View.Model.SetValue("FSRCPAYEDAMOUNT", listpayedAmount, i);
                   }
                   else
                   {
                       this.View.Model.SetValue("FSRCPAYEDAMOUNT", 0, i);
                   }

               }
               this.upatepayorback();
               double pay = Convert.ToDouble(this.View.Model.GetValue("FEXPSUBMITAMOUNT", i)) - Convert.ToDouble(this.View.Model.GetValue("FSRCPAYEDAMOUNT", i));
               if (pay > 0)
               {
                   this.View.Model.SetValue("FREQSUBMITAMOUNT", pay, i);
               }
               else
               {
                   this.View.Model.SetValue("FREQSUBMITAMOUNT", -pay, i);
               }


           }
       }

    //自动判断更新申请付款和申请退款
       private void upatepayorback()
       {
           double allpayedAmount = 0;
           int row = this.View.Model.GetEntryRowCount("FEntity");
           for (int i = 0; i < row; i++)
           {
               allpayedAmount = allpayedAmount + Convert.ToDouble(this.View.Model.GetValue("FSRCPAYEDAMOUNT", i));
           }

           double SrcOffSetAmountall = Convert.ToDouble(this.View.Model.GetValue("FExpAmountSum"));

           //pay = allpayedAmount - SrcOffSetAmountall;
          // if (this.View.Model.GetValue("FPayBox").Equals(false) && this.View.Model.GetValue("FrefundBox").Equals(false))
           //{
           if (allpayedAmount - SrcOffSetAmountall < 0)
               {
                   this.View.Model.SetValue("FPayBox", true);
                   this.View.Model.SetValue("FrefundBox", false);
                this.View.Model.SetValue("FRequestType", "1");

               }
           if (allpayedAmount - SrcOffSetAmountall > 0)
               {
                   this.View.Model.SetValue("FrefundBox", true);
                   this.View.Model.SetValue("FPayBox", false);
                   this.View.Model.SetValue("FRequestType", "2");

            }
               double ReqAmountSum = Convert.ToDouble(this.View.Model.GetValue("FReqAmountSum"));//申请付退款金额汇总
               if (ReqAmountSum == 0)
               {
                   this.View.Model.SetValue("FrefundBox", false);
                   this.View.Model.SetValue("FPayBox", false);
                   this.View.Model.SetValue("FRequestType", "0");
            }

          //}
       }
       public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            //oldvalue = Convert.ToDecimal(this.View.Model.GetValue("FReqReimbAmountSum"));
            string payroback = Convert.ToString(this.View.Model.GetValue("FRequestType"));
            switch(payroback)
            {
                case "1":
                    this.View.Model.SetValue("FPayBox", true);
                    this.View.Model.SetValue("FrefundBox", false);
                    break;
                case "2":
                    this.View.Model.SetValue("FrefundBox", true);
                    this.View.Model.SetValue("FPayBox", false);
                    break;
                case "0":
                    this.View.Model.SetValue("FrefundBox", false);
                    this.View.Model.SetValue("FPayBox", false);
                    break;


            }
        }
        /*
       public override void BeforeDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeDoOperationEventArgs e)
       {
           string operation = e.Operation.ToString();
           if (operation == "Kingdee.BOS.Business.Bill.Operation.Save")
           {
               Decimal newvalue = Convert.ToDecimal(this.View.Model.GetValue("FReqReimbAmountSum"));
               if (oldvalue < newvalue)
               {
                   string message = string.Format("核定金额大于申请金额,原申请金额总和值为：{0}", oldvalue);
                   this.View.ShowErrMessage(message, message, Kingdee.BOS.Core.DynamicForm.MessageBoxType.Notice);

               }
               else
               { 
                   base.BeforeDoOperation(e);
               }

           }
       }*/


    }
}
