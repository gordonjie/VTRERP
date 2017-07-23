using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.SCM.SAL;
using Kingdee.K3.SCM.App.Pur.ServicePlugIn;
using Kingdee.K3.SCM.App.Utils;
using Kingdee.K3.SCM.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM
{
    [Description("采购订单下推内蒙销售订单")]
    public class JN_PurchaseOrderToSaleOrder : PurchaseOrderToSaleOrder
    {


        public override void OnAfterFieldMapping(AfterFieldMappingEventArgs e)
        {
            //base.OnAfterFieldMapping(e);
            double payplanamount = 0;
            ExtendedDataEntity[] entityArray = e.TargetExtendDataEntitySet.FindByEntityKey("FBillHead");
            ExtendedDataEntity[] entityArray2 = e.TargetExtendDataEntitySet.FindByEntityKey("FSaleOrderEntry");
            ExtendedDataEntity[] entityArray3 = e.TargetExtendDataEntitySet.FindByEntityKey("FSaleOrderFinance");
            foreach (ExtendedDataEntity entity in entityArray2)
            {
                Convert.ToInt64(entityArray[entity.DataEntityIndex].DataEntity["SaleOrgId_Id"]);
                object obj1 = entity.DataEntity["SrcType"];
                object srcBillNo = entity.DataEntity["SrcBillNo"];
                /*long num = (from p in this.custList
                            where p.OrderBillNo == Convert.ToString(srcBillNo)
                            select p.CustId).FirstOrDefault<long>();*/
                long num = 137293;
                BaseDataField field = e.TargetBusinessInfo.GetField("FCustId") as BaseDataField;
                FieldUtils.SetBaseDataFieldValue(base.Context, field, entityArray[entity.DataEntityIndex].DataEntity, num);
                DynamicObject obj2 = entityArray[entity.DataEntityIndex].DataEntity["CustId"] as DynamicObject;
                long num2 = Convert.ToInt64(obj2["SETTLETYPEID_Id"]);
                BaseDataField field2 = e.TargetBusinessInfo.GetField("FSettleModeId") as BaseDataField;
                FieldUtils.SetBaseDataFieldValue(base.Context, field2, entityArray3[entity.DataEntityIndex].DataEntity, num2);
                long num3 = Convert.ToInt64(obj2["RECCONDITIONID_Id"]);
                BaseDataField field3 = e.TargetBusinessInfo.GetField("FRecConditionId") as BaseDataField;
                FieldUtils.SetBaseDataFieldValue(base.Context, field3, entityArray3[entity.DataEntityIndex].DataEntity, num3);
                long num4 = (obj2 == null) ? 0 : Convert.ToInt64(obj2["Id"]);
                DynamicObject obj3 = null;
                payplanamount = payplanamount + (Convert.ToDouble(entity.DataEntity["BaseUnitQty"])* Convert.ToDouble(entity.DataEntity["TaxPrice"]));
     
                if (num4 > 0L)
                {
                    DynamicObjectCollection source = obj2["BD_CUSTCONTACT"] as DynamicObjectCollection;
                    for (int i = source.Count<DynamicObject>() - 1; i >= 0; i--)
                    {
                        if (Convert.ToBoolean(source[i]["IsUsed"]))
                        {
                            obj3 = source[i];
                            if (Convert.ToBoolean(source[i]["IsDefaultConsignee"]))
                            {
                                break;
                            }
                        }
                    }
                }
                long num6 = (obj3 == null) ? 0L : Convert.ToInt64(obj3["Id"]);
                BaseDataField field4 = e.TargetBusinessInfo.GetField("FHEADLOCID") as BaseDataField;
                FieldUtils.SetBaseDataFieldValue(base.Context, field4, entityArray[entity.DataEntityIndex].DataEntity, num6);
                DynamicObject obj4 = entityArray[entity.DataEntityIndex].DataEntity["HeadLocId"] as DynamicObject;
                if (obj4 != null)
                {
                    entityArray[entity.DataEntityIndex].DataEntity["ReceiveAddress"] = obj4["ADDRESS"];
                }
            }
            /*复制模板，增加付款计划*/
            if (payplanamount > 0L)
            {
                FormMetadata SAL_SaleOrderForm = AppServiceContext.MetadataService.Load(this.Context, "SAL_SaleOrder") as FormMetadata;
                DynamicObject templateBillObj = AppServiceContext.ViewService.LoadWithCache(this.Context, new object[] { 130701 }, SAL_SaleOrderForm.BusinessInfo.GetDynamicObjectType(), true, null)
                            .FirstOrDefault();
                DynamicObjectCollection orderplan = entityArray[0].DataEntity["SaleOrderPlan"] as DynamicObjectCollection;
                
                //orderplan[0]["RecAdvanceRate"] = 59;
                //orderplan[0]["RecAdvanceAmount"] = 69;
            }
            this.SetDefaultExchange(e);
            this.SetRelativeCodeByMaterialId(e);
        }

            private void SetDefaultExchange(AfterFieldMappingEventArgs e)
        {
            ExtendedDataEntity[] entityArray = e.TargetExtendDataEntitySet.FindByEntityKey("FBillHead");
            e.TargetExtendDataEntitySet.FindByEntityKey("FSaleOrderEntry");
            ExtendedDataEntity[] entityArray2 = e.TargetExtendDataEntitySet.FindByEntityKey("FSaleOrderFinance");
            ICommonService commonService = ServiceFactory.GetCommonService(base.Context);
            foreach (ExtendedDataEntity entity in entityArray2)
            {
                ExtendedDataEntity entity2 = entityArray[entity.DataEntityIndex];
                long num = Convert.ToInt64(entity2.DataEntity["SaleOrgId_Id"]);
                long num2 = 0;
                long num3 = 0;
                JSONObject defCurrencyAndExchangeTypeByBizOrgID = commonService.GetDefCurrencyAndExchangeTypeByBizOrgID(base.Context, num);
                if (defCurrencyAndExchangeTypeByBizOrgID != null)
                {
                    num2 = Convert.ToInt64(defCurrencyAndExchangeTypeByBizOrgID["FCyForID"]);
                    num3 = Convert.ToInt64(defCurrencyAndExchangeTypeByBizOrgID["FRateType"]);
                }
                BaseDataField field = e.TargetBusinessInfo.GetField("FExchangeTypeId") as BaseDataField;
                FieldUtils.SetBaseDataFieldValue(base.Context, field, entity.DataEntity, num3);
                long num4 = Convert.ToInt64(entity.DataEntity["SettleCurrId_Id"]);
                DateTime time = Convert.ToDateTime(entityArray[entity.DataEntityIndex].DataEntity["Date"]);
                if ((num2 == num4) || (time == DateTime.MinValue))
                {
                    entity.DataEntity["ExchangeRate"] = 1;
                }
                else
                {
                    KeyValuePair<decimal, int> pair = commonService.GetExchangeRateAndDecimal(base.Context, num4, num2, num3, time, time);
                    entity.DataEntity["ExchangeRate"] = pair.Key;
                }
            }
        }

        private void SetRelativeCodeByMaterialId(AfterFieldMappingEventArgs e)
        {
            ExtendedDataEntity[] entityArray = e.TargetExtendDataEntitySet.FindByEntityKey("FSaleOrderEntry");
            ExtendedDataEntity[] entityArray2 = e.TargetExtendDataEntitySet.FindByEntityKey("FBillHead");
            Kingdee.K3.SCM.Contracts.ICommonService service = Kingdee.K3.SCM.App.ServiceHelper.GetService<ICommonService>();
            Dictionary<long, bool> dictionary = new Dictionary<long, bool>();
            foreach (ExtendedDataEntity entity in entityArray)
            {
                if (!entity.DataEntity["MapId_Id"].IsNullOrEmptyOrWhiteSpace())
                {
                    continue;
                }
                bool flag = false;
                long key = Convert.ToInt64(entityArray2[entity.DataEntityIndex].DataEntity["SaleOrgId_Id"]);
                long num2 = Convert.ToInt64(entityArray2[entity.DataEntityIndex].DataEntity["CustId_Id"]);
                long num3 = Convert.ToInt64(entity.DataEntity["MaterialId_Id"]);
                if (!dictionary.ContainsKey(key))
                {
                    object obj2 = service.GetSystemProfile(base.Context, key, "SAL_SystemParameter", "UseCustMatMapping", false);
                    flag = (obj2 != null) && Convert.ToBoolean(obj2);
                    dictionary.Add(key, flag);
                }
                if (dictionary[key])
                {
                    List<CustomerMaterialResult> list = service.GetRelativeCodeByMaterial(base.Context, num3, num2, key);
                    if (list.Count > 0)
                    {
                        string str = "";
                        foreach (CustomerMaterialResult result in list)
                        {
                            if (result.FCustId > 0)
                            {
                                str = result.Fid;
                                break;
                            }
                        }
                        if (str.IsNullOrEmptyOrWhiteSpace())
                        {
                            str = list[0].Fid;
                        }
                        BaseDataField field = e.TargetBusinessInfo.GetField("FMapId") as BaseDataField;
                        FieldUtils.SetBaseDataFieldValue(base.Context, field, entity.DataEntity, str);
                    }
                }
            }
        }




        public override void OnInSelectedRow(InSelectedRowEventArgs e)
        {
           
        }
        
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
            Entity entity = e.TargetBusinessInfo.GetEntity("FSaleOrderEntry");
            //源单单单据体元数据
            //Entity es = e.SourceBusinessInfo.GetEntity("FSaleOrderEntry");

            // 读取已经生成的销售订单
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

                for (int i = rowCount - 1; i >= 0; i--)
                {
                    
                    DynamicObject rowObj = rowObjs[i];
                    DynamicObject FMaterialId = rowObj["MaterialId"] as DynamicObject;
                    DynamicObjectCollection MaterialBase = FMaterialId["MaterialBase"] as DynamicObjectCollection;
                    DynamicObject FCategoryID = MaterialBase[0]["CategoryID"] as DynamicObject;
                    string FCategoryname = Convert.ToString(FCategoryID["Name"]);
                    if (FCategoryname == "产成品")
                    {
                        //获取ID为100440的仓库
                        DynamicObject[] STOCKIDs = BusinessDataServiceHelper.Load(this.Context, new object[] { 100440 }, (MetaDataServiceHelper.Load(this.Context, "BD_STOCK") as FormMetadata).BusinessInfo.GetDynamicObjectType());
                        rowObj["FSTOCKID_MX"] = STOCKIDs[0];
                    }

                        if (FCategoryname == "半成品")
                    {
                        //获取ID为100424的仓库
                        DynamicObject[] STOCKIDs = BusinessDataServiceHelper.Load(this.Context, new object[] { 100424 }, (MetaDataServiceHelper.Load(this.Context, "BD_STOCK") as FormMetadata).BusinessInfo.GetDynamicObjectType());
                        rowObj["FSTOCKID_MX"] = STOCKIDs[0];
                    }
                    

                }
            }
           }
        }
}
