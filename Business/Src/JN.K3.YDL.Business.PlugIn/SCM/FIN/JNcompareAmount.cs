using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    [Description("审核过程审批金额不能大于之前的金额-表单插件")]
    public class JNcompareAmount : AbstractBillPlugIn
    {
        /// <summary>
        /// [合计金额]字段的标识：单据上的  合计金额 字段
        /// </summary>
        public virtual string AllAmountKey
        {
            get
            {
                return "FEXPAMOUNTSUM";//核定报销金额汇总
            }
        }

        /// <summary>
        /// [状态]字段的标识：单据上的  状态 字段
        /// </summary>
        public virtual string AllAmountKey
        {
            get
            {
                return "FEXPAMOUNTSUM";//核定报销金额汇总
            }
        }
        public virtual double oldAmount = 0;//旧金额
        public virtual double newAmount = 0;//新金额

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            oldAmount = Convert.ToDouble(this.View.Model.GetValue(AllAmountKey));//获取初始化金额


        }

        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            if(e.Key.Equals(AllAmountKey))
            {
                if(this.View.
            oldAmount=Convert.ToDouble(e.OldValue);
            }
            base.DataChanged(e);
            pkAmount
        }


    }
}
