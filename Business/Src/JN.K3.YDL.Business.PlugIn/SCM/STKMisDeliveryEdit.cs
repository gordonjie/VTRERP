using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Bill.PlugIn.Args;

namespace JN.K3.YDL.Business.PlugIn.SCM
{
    /// <summary>
    /// 其他出库单表单插件
    /// </summary>
    [Description("其他出库单单据类型,领料类型不能为空")]
    public class STKMisDeliveryEdit: AbstractBillPlugIn
    {
        /// <summary>
        /// 保存前事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            string strTemp = "";
            //单据类型不能为空
            DynamicObject typeObject = this.View.Model.GetValue("FBillTypeID") as DynamicObject;
            if (typeObject != null)
            {
                strTemp = typeObject["Name"].ToString();
                if (strTemp == "空")
                {
                    this.View.ShowErrMessage("单据类型不能为空!");
                    e.Cancel = true;
                    return;
                }
            }

            // 领料类型不能为空
            strTemp = this.View.Model.GetValue("FReceiveType").ToString();
            if (strTemp == "空")
            {
                this.View.ShowErrMessage("领料类型不能为空!");
                e.Cancel = true;
                return;
            }

            
        }
    }
}
