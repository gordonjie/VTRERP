using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;

namespace VTR.K3.Bill.PlugIn
{
    /// <summary>
    /// 付款和费用申请单插件
    /// </summary>
    [Description("付款和费用申请单插件")]
    public class ExpenseRequestOrderEdit : AbstractBillPlugIn
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="e"></param>
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            //禁止启用
            this.View.GetControl("F_JN_YDL_BTExplain").Enabled = false;
        }

        bool isTrue = false;
        /// <summary>
        /// 值更新事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            //十六进制颜色
            string strColor = "";
            string strName = "ForeColor";
            //base.BeforeUpdateValue(e);
            switch (e.Key.ToUpperInvariant())
            {
                case "FBILLTYPE"://订单类型
                    string fname = "费用申请单类型";
                    string fvalue = e.Value.ToString(); //当前变更的值
                    string description = YDLCommServiceHelper.GetExpenseRequestOrderEditDescription(this.Context, fname, fvalue);
                    this.View.Model.SetValue("F_JN_YDL_BTExplain", description);
                    if (isTrue)
                    {
                        strColor = "#FF0000";//红色
                        this.View.GetControl("F_JN_YDL_BTExplain").SetCustomPropertyValue(strName, strColor);
                        isTrue = false;
                    }
                    else
                    {
                        strColor = "#0000FF"; //蓝色
                        this.View.GetControl("F_JN_YDL_BTExplain").SetCustomPropertyValue(strName, strColor);
                        isTrue = true;
                    }
                    break;
                case "F_JNSALESZX": //销售经理
                    //更新往来单位
                    string strType = this.View.Model.GetValue("FTOCONTACTUNITTYPE").ToString();
                    if (strType.ToUpperInvariant() == "BD_EMPINFO")//员工
                    {
                        this.Model.SetItemValueByNumber("FTOCONTACTUNIT", ((DynamicObject)e.Value)[4].ToString(), 1);
                        DynamicObject actunit = this.View.Model.GetValue("FTOCONTACTUNIT") as DynamicObject;
                        actunit[0] = ((DynamicObject)e.Value)[0];
                        actunit[1] = ((DynamicObject)e.Value)[1];
                        actunit[3] = ((DynamicObject)e.Value)[3];
                        actunit[4] = ((DynamicObject)e.Value)[4];
                        this.View.Model.SetValue("FTOCONTACTUNIT", actunit);
                        this.View.UpdateView("FTOCONTACTUNIT"); //执行更新
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
