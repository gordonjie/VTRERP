using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.MFG.MO
{
    /// <summary>
    /// 生产订单-保存服务插件
    /// </summary>
    [Description("生产订单-保存服务插件")]
    public class JN_YDL_MOSave : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBillHead");
            e.FieldKeys.Add("FWorkShopID");
            e.FieldKeys.Add("FWorkShopID0");
        }

        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        /// 
        public override void BeforeExecuteOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            DynamicObject[] DataEntitys = (from p in e.SelectedRows select p.DataEntity).ToArray();
            foreach (var DataEntity in DataEntitys)
            {
                DynamicObjectCollection TreeEntitys = DataEntity["TreeEntity"] as DynamicObjectCollection;
                if (TreeEntitys.Count > 0)
                {
                    DataEntity["WorkShopID"] = TreeEntitys[0]["WorkShopID"];
                    DataEntity["WorkShopID_Id"] = TreeEntitys[0]["WorkShopID_Id"];
                   
                }
            }
        }


    }
}
