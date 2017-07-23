using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.BillConvert
{
    [Description("应收单至收款单-单据转换插件")]
    public class ReceivableToReceiveBill : AbstractConvertPlugIn
    {
        public override void AfterConvert(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.AfterConvertEventArgs e)
        {
            ExtendedDataEntity[] dataEntitys = e.Result.FindByEntityKey("FBillHead");
            if (dataEntitys == null || dataEntitys.Count() == 0) { return; }
            foreach (var dataEntity in dataEntitys)
            {
                DynamicObject data = dataEntity.DataEntity;
                //往来单位类型=客户，一级业务员=往来单位.一级业务员
                if (Convert.ToString(data["CONTACTUNITTYPE"]).EqualsIgnoreCase("BD_Customer"))
                {
                    DynamicObject contact = data["CONTACTUNIT"] as DynamicObject;

                    BusinessInfo customerBusinfo = ((FormMetadata)Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>().Load(this.Context, "BD_Customer")).BusinessInfo;

                    DynamicObject customer = Kingdee.BOS.App.ServiceHelper.GetService<IViewService>().LoadSingle(this.Context, contact["Id"], customerBusinfo.GetDynamicObjectType());
                    if (customer != null)
                    {
                        data["FJNFistSaler_Id"] = customer["JN_SalesId_Id"];
                    }
                    Kingdee.BOS.ServiceHelper.DBServiceHelper.LoadReferenceObject(this.Context, new DynamicObject[] { data }, e.TargetBusinessInfo.GetDynamicObjectType());
                }
            }
        }
    }
}
