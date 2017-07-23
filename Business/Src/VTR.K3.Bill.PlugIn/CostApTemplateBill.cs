using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;

namespace VTR.K3.Bill.PlugIn
{
    /// <summary>
    /// 费用申请初始化插件
    /// </summary>
    [System.ComponentModel.Description("解除费用项目锁定")]
    public class CostTemplateBill : AbstractBillPlugIn
    {

        /// <summary>
        /// 解除费用项目锁定
        /// </summary>

        public override void AfterBindData(EventArgs e)
        {

            /*var typevalue = this.Model.GetValue("FBilltype");
            if (typevalue.ToString() == "招待费")
            {
               // this.View.StyleManager.SetVisible("FReceptionTab", "FReceptionTab", true);
                this.View.GetControl("FReceptionTab").Visible = true;
            }
            else
         
                this.View.GetControl("FReceptionTab").Visible =false;

            }
        

            if (flag)
            {*/
            base.AfterBindData(e);
            this.View.LockField("FEXPID", true);

            // }
        }
    }

 

}

    

