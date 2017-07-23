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
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.ServiceHelper;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    /// <summary>
    /// 付款和费用申请单插件
    /// </summary>
    [Description("付款和费用申请单单据类型说明,(员工)往来单位=销售经理")]
    public class ExpenseRequestOrderEdit : AbstractBillPlugIn
    {

        //十六进制颜色
        string strColor = "#FF0000";//红色
        string strName = "ForeColor";
        bool isTrue = false;
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="e"></param>
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            //禁止启用
            this.View.GetControl("F_JN_YDL_BTExplain").Enabled = false;
            this.View.GetControl("F_JN_YDL_BTExplain").SetCustomPropertyValue(strName, strColor);
        }
        
       
        /// <summary>
        /// 值更新事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            
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
                case "F_JNSALESMANAGERS": //销售经理(旧)
                    if (this.View.Model.GetValue("FBilltype").ToString() == "业务费用")
                    {
                        string strType = this.View.Model.GetValue("FTOCONTACTUNITTYPE").ToString();
                        if (strType.ToUpperInvariant() == "BD_EMPINFO")//员工
                        {
                            DynamicObject sales = (DynamicObject)e.Value;                           
                            this.Model.SetItemValueByID("FTOCONTACTUNIT", sales["Id"].ToString(),1);//更新往来单位
                            //销售经理的基础资料为任岗信息,根据员工ID关联任岗ID再更新
                            string sqlStr = string.Format("select top 1 FSTAFFID from T_BD_STAFF where FEMPINFOID={0} order by fstaffid asc ", sales["Id"].ToString());
                            DynamicObjectCollection dataCollection = DBServiceHelper.ExecuteDynamicObject(this.Context, sqlStr);
                            if (dataCollection != null && dataCollection.Count > 0)
                            {
                                this.Model.SetItemValueByID("F_JNSALESZX", dataCollection[0]["FSTAFFID"].ToString(),1);//更新销售经理
                            }
                        }
                    }               

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 菜单按钮点击事件
        /// </summary>
        /// <param name="e"></param>
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            //base.BarItemClick(e);
            switch (e.BarItemKey.ToUpper())
            {
                case "TBSETBTYPE":
                    //动态表单
                    DynamicFormShowParameter dynPara = new DynamicFormShowParameter();
                    //打开方式
                    dynPara.OpenStyle.ShowType = ShowType.Modal;
                    //业务对象ID
                    dynPara.FormId = "JN_YDL_ERODescription";

                    dynPara.Width = 800;
                    dynPara.Height = 600;
                    this.View.ShowForm(dynPara);
                    break;
                default:
                    break;
            }
        }
    }
}
