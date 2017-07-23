using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM
{
    [Description("销售订单下推采购申请后，自动选定供应商，获取价目表")]
    public class JN_SaleOrderToPurRequestion : AbstractConvertPlugIn
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
            Entity entity = e.TargetBusinessInfo.GetEntity("FEntity");
            Entity es = e.SourceBusinessInfo.GetEntity("FSaleOrderEntry");

            // 读取已经生成的付款单
            ExtendedDataEntity[] bills = e.TargetExtendedDataEntities.FindByEntityKey("FBillHead");

            // 定义一个集合，存储新拆分出来的单据体行
            List<ExtendedDataEntity> newRows = new List<ExtendedDataEntity>();
            // 对目标单据进行循环
            foreach (var bill in bills)
            {
                // 取单据体集合
                DynamicObjectCollection rowObjs = entity.DynamicProperty.GetValue(bill.DataEntity)
                    as DynamicObjectCollection;
                // 对单据体进行循环：从后往前循环，新拆分的行，避开循环
                int rowCount = rowObjs.Count;
                int newRowCount = 1;
                DateTime CreateDate =Convert.ToDateTime(bill["CreateDate"]);
                for (int i = rowCount - 1; i >= 0; i--)
                {
                    DynamicObject rowObj = rowObjs[i];
                    //获取ID为149808的供应商（内蒙）
                    DynamicObject[] superIDs = BusinessDataServiceHelper.Load(this.Context,new object[] { 149808 },(MetaDataServiceHelper.Load(this.Context, "BD_Supplier") as FormMetadata).BusinessInfo.GetDynamicObjectType());
                    rowObj["SuggestSupplierId"] = superIDs[0];
                    rowObj["SuggestSupplierId_Id"] = 149808;
                    rowObj["SupplierId"] = superIDs[0];
                    rowObj["SupplierId_Id"] = 149808;

                    long MaterialId = Convert.ToInt64(rowObj["MaterialId_Id"]);
                    long SupplierId = 149808;
                    DynamicObject auxprop= rowObj["AuxpropId"] as DynamicObject;
                    string auxpropId = "";
                    if (auxprop != null)
                    {
                        auxpropId = Convert.ToString(auxprop["F100001_Id"]);
                    }
                    DynamicObjectCollection prices = ServiceHelper.YDLCommServiceHelper.GetAuxpropPriceListId(this.Context, MaterialId, auxpropId, SupplierId, CreateDate);
                    if (prices.Count > 0)
                    {
                        rowObj["FTAXPRICE"] = prices[0][2];
                        rowObj["EvaluatePrice"] = prices[0][1];
                        rowObj["FTAXRATE"] = 17;
                    }
                }
            }

        }
    }
}
