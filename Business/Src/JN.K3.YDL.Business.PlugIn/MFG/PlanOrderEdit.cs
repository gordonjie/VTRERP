using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.MFG
{
    /// <summary>
    /// 计划订单-表单插件
    /// </summary>
    [Description("计划订单-表单插件")]
    public class PlanOrderEdit : AbstractBillPlugIn
    {
        /// <summary>
        /// 表单操作前事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeDoOperationEventArgs e)
        {            
            base.BeforeDoOperation(e);
            if (e.Operation.FormOperation.Operation.ToString() == "Save")//保存操作时
            {

                if (Convert.ToBoolean(e.Operation.View.OpenParameter.GetCustomParameter("isPassed"))) //检验通过再次调用操作判断定制参数
                {
                    e.Operation.View.OpenParameter.SetCustomParameter("isPassed", false);
                    return;
                }
                decimal max = 0;            //上限
                decimal min = 0;            //下限
                decimal sugQty = 0;         //建议订单量
                decimal firmQty = 0;        //确认订单量
                DynamicObject wuLiao = this.View.Model.DataObject["MaterialId"] as DynamicObject;//物料
                if (wuLiao == null) return;
                if (wuLiao["F_JN_adjustMax"] != null) max = Convert.ToDecimal(wuLiao["F_JN_adjustMax"]) / 100;
                if (wuLiao["F_JN_adjustMin"] != null) min = Convert.ToDecimal(wuLiao["F_JN_adjustMin"]) / 100;
                if (this.View.Model.DataObject["SugQty"] != null) sugQty = Convert.ToDecimal(this.View.Model.DataObject["SugQty"]);
                if (this.View.Model.DataObject["FirmQty"] != null) firmQty = Convert.ToDecimal(this.View.Model.DataObject["FirmQty"]);
                decimal minValue = sugQty * (1 - min);//下限值
                decimal maxValue = sugQty * (1 + max);//上限值 
                #region 订单量调整在物料调整下限之间
                if (firmQty >= minValue && firmQty <= maxValue)
                {
                    this.View.Model.DataObject["F_JN_IsOut"] = 0;
                }
                #endregion
                #region 订单量调整低于物料调整下限
                if (firmQty < minValue)
                {
                    this.View.ShowWarnningMessage("确认订单量已经超过该物料的调整上下限，请问是否继续？", "超过范围,请问是否继续？", MessageBoxOptions.YesNoCancel, new Action<MessageBoxResult>((boxresult) =>
                    {
                        if (boxresult == MessageBoxResult.Yes)
                        {
                            this.View.Model.DataObject["F_JN_IsOut"] = 1;
                            e.Cancel = false;
                            e.Operation.View.OpenParameter.SetCustomParameter("isPassed", true); //设置定制参数
                            e.Operation.Execute();
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    }));
                    e.Cancel = true;
                }
                #endregion
                #region 订单量调整超过物料调整下限
                if (firmQty > maxValue)
                {
                    this.View.ShowWarnningMessage("确认订单量已经超过该物料的调整上下限，请问是否继续？", "超过范围,请问是否继续？", MessageBoxOptions.YesNoCancel, new Action<MessageBoxResult>((boxresult) =>
                    {
                        if (boxresult == MessageBoxResult.Yes)
                        {
                            this.View.Model.DataObject["F_JN_IsOut"] = 1;
                            e.Cancel = false;
                            e.Operation.View.OpenParameter.SetCustomParameter("isPassed", true); //设置定制参数
                            e.Operation.Execute();
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    }));
                    e.Cancel = true;
                }
                #endregion
            }
        }        
    }
}
