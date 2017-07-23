using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.SCM.Sal.Business.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM.SAL
{
    [Description("更换包装单单据插件")]
    public class SCM_ChangePackageedit : AbstractBillPlugIn
    {
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            //通过当前收货方对应的发货地点
            if (e.Key.ToUpper().ToString() == "FRECEIVEID")
            {
                DynamicObject loc = Common.SetDefaultHeadLoc(this, "FReceiveId", "FHEADLOCID", true);
                Common.SetContact(this, loc, "FReceiveContact", "FReceiveId");
           }
        }
    }
}
