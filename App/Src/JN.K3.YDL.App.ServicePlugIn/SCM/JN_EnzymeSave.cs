using JN.BOS.Contracts;
using JN.K3.YDL.Contracts.SCM;
using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Core.DefaultValueService;
using Kingdee.BOS.App.Core.PlugInProxy;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Const;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.K3.MFG.Common.BusinessEntity.BD;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM
{


    /// <summary>
    /// 入库单保存服务端插件：检查相同组织+物料+辅助属性+批号相同时，是否单位酶活量是否不同
    /// </summary>
    [Description("入库单保存服务端插件：检查相同组织+物料+辅助属性+批号相同时，是否单位酶活量是否不同")]
    public class JN_EnzymeSave : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBillNo");
            e.FieldKeys.Add("FStockOrgId");
            e.FieldKeys.Add("FLot");
            e.FieldKeys.Add("FMaterialId");
            e.FieldKeys.Add("FAuxPropId");
            e.FieldKeys.Add("FJNUnitEnzymes");
        }

        /// <summary>
        /// 增加操作校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            e.Validators.Add(new JN_EnzymeValidator());
        }


    }
}
