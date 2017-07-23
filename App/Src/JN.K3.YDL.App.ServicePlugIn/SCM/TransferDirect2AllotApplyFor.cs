using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM
{
    [Description("调拨申请单下推直接调拨单插件--辅单位控制")]
    public class TransferDirect2AllotApplyFor : AbstractConvertPlugIn
    {
        /// <summary>
        /// 目标单单据构建完毕，且已经创建好与源单的关联关系之后，触发此事件
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>
        /// 本事件的时机，刚好能够符合需求，
        /// 而AfterConvert事件，则在执行表单服务策略之后
        /// </remarks>
        public override void OnAfterCreateLink(CreateLinkEventArgs e)
        {
            // 目标单单据体元数据
            Entity entity = e.TargetBusinessInfo.GetEntity("FBillEntry");
            //Entity es = e.SourceBusinessInfo.GetEntity("FEntity");

            // 读取已经生成的付款单
            ExtendedDataEntity[] bills = e.TargetExtendedDataEntities.FindByEntityKey("FBillHead");
            foreach (var bill in bills)
            {
                DynamicObjectCollection rowObjs = entity.DynamicProperty.GetValue(bill.DataEntity)
                      as DynamicObjectCollection;
                foreach (var rowObj in rowObjs)
                {
                    DynamicObject Material = rowObj["MaterialId"] as DynamicObject;
                    DynamicObject AuxUnit = Material["AuxUnitID"] as DynamicObject;
                    rowObj["SecUnitId"] = AuxUnit;
                    rowObj["ExtAuxUnitId"] = AuxUnit;

                }
                
            }
        }
    }
}
