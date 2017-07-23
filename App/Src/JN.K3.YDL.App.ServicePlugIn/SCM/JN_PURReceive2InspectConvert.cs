using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.ControlElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.FIN.App.Core;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.MFG;
using Kingdee.K3.MFG.QM.App.BillConvertServicePlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core;
using Kingdee.K3.MFG.Contracts.QM;
using Kingdee.K3.MFG.Common.BusinessEntity.QM;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.K3.MFG.App;
using Kingdee.BOS.Orm.Metadata.DataEntity;



namespace JN.K3.YDL.App.ServicePlugIn.SCM
{
    /// <summary>
    /// 采购收料单-检验单
    /// </summary>
    [Description("采购收料单-检验单")]
    public class JN_PURReceive2InspectConvert : PURReceive2InspectConvert
    {
        private bool IsControlSerialNo;
        private bool QCSNSplitEntry;



  



        public override void OnBeforeGroupBy(BeforeGroupByEventArgs e)
        {
            base.OnBeforeGroupBy(e);
            string variableValue = base.Option.GetVariableValue<string>("TargetBillTypeId");
            DynamicObject obj2 = Kingdee.K3.MFG.App.AppServiceContext.BusinessDataService.LoadBillTypePara(base.Context, "InspectBillTPS", variableValue, true);
            this.IsControlSerialNo = base.Option.GetVariableValue<DynamicObjectCollection>("SourceData").Any<DynamicObject>(a => a.GetDynamicValue<long>("FSNUNITID", 0L) > 0L);
            if (!obj2.IsNullOrEmpty())
            {
                this.QCSNSplitEntry = obj2.GetDynamicValue<bool>("PRDQCSNSplitEntry", false);
            }
            if ((obj2.IsNullOrEmpty() || obj2.GetDynamicValue<bool>("EnableAux", false)) || !obj2.GetDynamicObjectItemValue<bool>("EnableMaterialMergeInspectOfSampleProvider", false))
            {
                e.EntryGroupKey = "FDetailEntity_FEntryID";
            }
            else if (this.IsControlSerialNo)
            {
                (from s in e.SourceData select s.GetDynamicValue<long>("FID", 0L)).Distinct<long>().ToList<long>();
                e.EntryGroupKey = "FDetailEntity_FEntryID";
            }
        }




        public override void AfterConvert(AfterConvertEventArgs e)
        {
            //base.AfterConvert(e);

            ExtendedDataEntity[] source = e.Result.FindByEntityKey("FEntity");
            string billTypeId = (source.FirstOrDefault<ExtendedDataEntity>().DataEntity.Parent as DynamicObject).GetDynamicObjectItemValue<string>("FBillTypeID_Id", null);
            DynamicObject billTypePara = Kingdee.K3.MFG.App.AppServiceContext.BusinessDataService.LoadBillTypePara(base.Context, "InspectBillTPS", billTypeId, true);
            this.SetPolicyQty(source, billTypePara);
            this.SetEntitySSInfoByQty(source, billTypePara);
            this.SetInspectItem(source, billTypePara);
            this.FillReferDetail(source, billTypePara);
            this.SplitData(e);

            ExtendedDataEntity[] entryDataes = e.Result.FindByEntityKey("FEntity");
            this.BuildSerial(entryDataes);
            if (this.QCSNSplitEntry)
            {
                base.SplitSNEntrys(e.Result);
            }
        }


        private void BuildSerial(ExtendedDataEntity[] entryDataes)
        {
            List<long> col = (from s in entryDataes
                              from ss in s.DataEntity["FEntity_Link"] as DynamicObjectCollection
                              select ss.GetDynamicObjectItemValue<long>("SId", 0L)).Distinct<long>().ToList<long>();
            if (!col.IsEmpty<long>())
            {
                IEnumerable<DynamicObject> purSNInfo = Kingdee.K3.MFG.App.AppServiceContext.GetService<IInspectSrcService>().GetPurSNInfo(base.Context, col);
                if (purSNInfo.IsEmpty<DynamicObject>())
                {
                    foreach (ExtendedDataEntity entity in entryDataes)
                    {
                        entity.DataEntity.SetDynamicObjectItemValue("SNUnitID_Id", 0);
                        entity.DataEntity.SetDynamicObjectItemValue("SNUnitID", null);
                    }
                }
                else
                {
                    Dictionary<long, IGrouping<long, DynamicObject>> dictionary = (from g in purSNInfo
                                                                                   where g.GetDynamicValue<long>("FIBENTRYID", 0L) <= 0L
                                                                                   group g by g.GetDynamicValue<long>("FENTRYID", 0L)).ToDictionary<IGrouping<long, DynamicObject>, long>(d => d.Key);
                    FormMetadata metadata = (FormMetadata)Kingdee.K3.MFG.App.AppServiceContext.MetadataService.Load(base.Context, "QM_InspectBill", true);
                    Entity entity2 = metadata.BusinessInfo.GetEntity("FEntity");
                    foreach (ExtendedDataEntity entity3 in entryDataes)
                    {
                        DynamicObject dataEntity = entity3.DataEntity;
                        DynamicObjectCollection objects = dataEntity.GetDynamicValue<DynamicObjectCollection>("FEntity_Link", null);
                        dataEntity.GetDynamicValue<long>("SrcEntryId", 0L);
                        DynamicObjectCollection source = dataEntity.GetDynamicValue<DynamicObjectCollection>("PolicyDetail", null);
                        DynamicObject obj3 = source.First<DynamicObject>() as DynamicObject;
                        List<DynamicObject> list2 = new List<DynamicObject>();
                        foreach (DynamicObject obj4 in objects)
                        {
                            long key = obj4.GetDynamicValue<long>("SId", 0L);
                            IGrouping<long, DynamicObject> grouping = null;
                            if (dictionary.TryGetValue(key, out grouping))
                            {
                                foreach (DynamicObject obj5 in grouping)
                                {
                                    list2.Add(obj5);
                                }
                            }
                        }
                        if (list2.All<DynamicObject>(a => a.GetDynamicValue<long>("FSERIALID", 0L) <= 0L))
                        {
                            dataEntity.SetDynamicObjectItemValue("SNUnitID_Id", 0);
                            dataEntity.SetDynamicObjectItemValue("SNUnitID", null);
                        }
                        else
                        {
                            source.Clear();
                            List<DynamicObject> list3 = (from s in list2
                                                         orderby s.GetDynamicValue<string>("FSERIALNO", null)
                                                         select s).ToList<DynamicObject>();
                            List<DynamicObject> list4 = (from s in list3
                                                         where !s.GetDynamicValue<string>("FSERIALNO", null).IsNullOrEmptyOrWhiteSpace()
                                                         select s).ToList<DynamicObject>();
                            List<DynamicObject> collection = (from s in list3
                                                              where s.GetDynamicValue<string>("FSERIALNO", null).IsNullOrEmptyOrWhiteSpace()
                                                              select s).ToList<DynamicObject>();
                            list4.AddRange(collection);
                            int num2 = 1;
                            DynamicObject baseUnitInfo = dataEntity.GetDynamicValue<DynamicObject>("BaseUnitId", null);
                            DynamicObject unitInfo = dataEntity.GetDynamicValue<DynamicObject>("SNUnitID", null);
                            foreach (DynamicObject obj8 in list4)
                            {
                                DynamicObject dynamicObject = obj3 as DynamicObject;
                                dynamicObject.SetDynamicObjectItemValue("PolicyQty", 1);
                                decimal num3 = MFGQtyConvertUtil.getToBasePrecisionQty(base.Context, dynamicObject.GetDynamicValue<long>("PolicyMaterialId_Id", 0L), unitInfo, baseUnitInfo, 1M);
                                dynamicObject.SetDynamicObjectItemValue("BasePolicyQty", num3);
                                dynamicObject.SetDynamicObjectItemValue("Seq", num2++);
                                if (obj8.GetDynamicValue<int>("FSERIALID", 0) != 0)
                                {
                                    dynamicObject.SetDynamicObjectItemValue("SerialId_Id", obj8.GetDynamicValue<int>("FSERIALID", 0));
                                }
                                source.Add(dynamicObject);
                            }
                        }
                    }
                    Kingdee.K3.MFG.App.AppServiceContext.DBService.LoadReferenceObject(base.Context, (from s in entryDataes select s.DataEntity).ToArray<DynamicObject>(), entity2.DynamicObjectType, true);
                    foreach (ExtendedDataEntity entity4 in entryDataes)
                    {
                        DynamicObject obj10 = entity4.DataEntity;
                        DynamicObject obj11 = obj10.GetDynamicValue<DynamicObject>("BaseUnitId", null);
                        DynamicObject obj12 = obj10.GetDynamicValue<DynamicObject>("SNUnitID", null);
                        if (!obj12.IsNullOrEmpty())
                        {
                            DynamicObjectCollection objects3 = obj10.GetDynamicValue<DynamicObjectCollection>("PolicyDetail", null);
                            decimal num4 = MFGQtyConvertUtil.getToBasePrecisionQty(base.Context, objects3.FirstOrDefault<DynamicObject>().GetDynamicValue<long>("PolicyMaterialId_Id", 0L), obj12, obj11, 1M);
                            foreach (DynamicObject obj13 in objects3)
                            {
                                obj13.SetDynamicObjectItemValue("BasePolicyQty", num4);
                            }
                        }
                    }
                }
            }
        }


        private void FillReferDetail(ExtendedDataEntity[] entityDataes, DynamicObject billTypePara)
        {
            FormMetadata metadata = (FormMetadata)Kingdee.K3.MFG.App.AppServiceContext.MetadataService.Load(base.Context, "QM_InspectBill", true);
            EntryEntity entryEntity = metadata.BusinessInfo.GetEntryEntity("FReferDetail");
            List<long> srcEntryIds = (from s in entityDataes
                                      from ss in s.DataEntity["FEntity_Link"] as DynamicObjectCollection
                                      select ss.GetDynamicObjectItemValue<long>("SId", 0L)).Distinct<long>().ToList<long>();
            string businessType = billTypePara.GetDynamicObjectItemValue<string>("FInspectType", null);
            DynamicObjectCollection col = this.GetSrcInfo(base.Context, srcEntryIds, businessType);
            if (!col.IsEmpty<DynamicObject>())
            {
                foreach (ExtendedDataEntity entity2 in entityDataes)
                {
                    DynamicObject dataEntity = entity2.DataEntity;
                    DynamicObjectCollection objects2 = entity2.DataEntity.GetDynamicObjectItemValue<DynamicObjectCollection>("ReferDetail", null);
                    objects2.Clear();
                    DynamicObjectCollection objects3 = entity2.DataEntity.GetDynamicObjectItemValue<DynamicObjectCollection>("FEntity_Link", null);
                    int num = 1;
                    foreach (DynamicObject obj2 in objects3)
                    {
                        InspectBillView.FEntity_Link link = obj2;
                        foreach (DynamicObject obj3 in (from w in col
                                                        where w.GetDynamicObjectItemValue<string>("FTEID", null) == link.SId
                                                        select w).ToList<DynamicObject>())
                        {
                            InspectBillView.ReferDetail detail = new Kingdee.K3.MFG.Common.BusinessEntity.QM.InspectBillView.ReferDetail(new DynamicObject(entryEntity.DynamicObjectType))
                            {
                                Seq = num++,
                                SrcBillType = obj3.GetDynamicObjectItemValue<string>("FSRCBILLTYPE", null),
                                SrcBillNo = obj3.GetDynamicObjectItemValue<string>("FSRCBILLNO", null),
                                SrcInterId = Convert.ToInt64(link.SBillId),
                                SrcEntryId = Convert.ToInt64(link.SId),
                                SrcEntrySeq = (long)obj3.GetDynamicObjectItemValue<int>("FSRCENTRYSEQ", 0),
                                OrderType_Id = obj3.GetDynamicObjectItemValue<string>("FORDERBILLTYPE", null),
                                OrderBillNo = obj3.GetDynamicObjectItemValue<string>("FORDERBILLNO", null),
                                OrderId = obj3.GetDynamicObjectItemValue<long>("FORDERID", 0L),
                                OrderEntryId = obj3.GetDynamicObjectItemValue<long>("FORDERENTRYID", 0L),
                                OrderEntrySeq = (long)obj3.GetDynamicObjectItemValue<int>("FORDERENTRYSEQ", 0)
                            };
                            objects2.Add((DynamicObject)detail);
                        }
                    }
                }
            }
        }




        private void SplitData(AfterConvertEventArgs e)
        {
            ExtendedDataEntity[] source = e.Result.FindByEntityKey("FEntity");
            string billTypeId = (source.FirstOrDefault<ExtendedDataEntity>().DataEntity.Parent as DynamicObject).GetDynamicObjectItemValue<string>("FBillTypeID_Id", null);
            DynamicObject dynamicObject = Kingdee.K3.MFG.App.AppServiceContext.BusinessDataService.LoadBillTypePara(base.Context, "InspectBillTPS", billTypeId, true);
            string str2 = dynamicObject.GetDynamicObjectItemValue<string>("SPLITBY", null);
            if ((!dynamicObject.IsNullOrEmpty() && !str2.EqualsIgnoreCase("A")) && !str2.IsNullOrEmptyOrWhiteSpace())
            {
                List<long> materialIds = (from s in source select s.DataEntity.GetDynamicValue<long>("MaterialId_Id", 0L)).ToList<long>();
                Dictionary<long, Tuple<long, long>> inspectorInfo = Kingdee.K3.MFG.App.AppServiceContext.GetService<IInspectService>().GetInspectorInfo(base.Context, materialIds);
                if (!inspectorInfo.IsEmpty<KeyValuePair<long, Tuple<long, long>>>())
                {
                    ExtendedDataEntity[] entityArray2 = e.Result.FindByEntityKey("FBillHead");
                    List<ExtendedDataEntity> list2 = new List<ExtendedDataEntity>();
                    foreach (ExtendedDataEntity entity in entityArray2)
                    {
                        Dictionary<long, ExtendedDataEntity> dictionary2 = new Dictionary<long, ExtendedDataEntity>();
                        foreach (DynamicObject obj3 in entity.DataEntity.GetDynamicValue<DynamicObjectCollection>("Entity", null))
                        {
                            Tuple<long, long> tuple;
                            long key = obj3.GetDynamicValue<long>("MaterialId_Id", 0L);
                            if (inspectorInfo.TryGetValue(key, out tuple))
                            {
                                long num2 = str2.EqualsIgnoreCase("B") ? tuple.Item1 : tuple.Item2;
                                if (dictionary2.Keys.Contains<long>(num2))
                                {
                                    dictionary2[num2].DataEntity.GetDynamicValue<DynamicObjectCollection>("Entity", null).Add(obj3);
                                }
                                else
                                {
                                    ExtendedDataEntity entity2 = (ExtendedDataEntity)entity.Clone();
                                    DynamicObjectCollection objects3 = entity2.DataEntity.GetDynamicValue<DynamicObjectCollection>("Entity", null);
                                    objects3.Clear();
                                    objects3.Add(obj3);
                                    if (str2.EqualsIgnoreCase("B"))
                                    {
                                        entity2.DataEntity.SetDynamicObjectItemValue("InspectGroupId_Id", tuple.Item1);
                                    }
                                    else if (str2.EqualsIgnoreCase("C"))
                                    {
                                        entity2.DataEntity.SetDynamicObjectItemValue("InspectorId_Id", tuple.Item2);
                                    }
                                    dictionary2.Add(num2, entity2);
                                }
                            }
                        }
                        list2.AddRange(dictionary2.Values);
                        dictionary2.Clear();
                    }
                    DynamicObject[] col = (from s in list2 select s.DataEntity).ToArray<DynamicObject>();
                    if (!col.IsEmpty<DynamicObject>())
                    {
                        Kingdee.K3.MFG.App.AppServiceContext.DBService.LoadReferenceObject(base.Context, col, col.FirstOrDefault<DynamicObject>().DynamicObjectType, true);
                        for (int i = 0; i < entityArray2.Length; i++)
                        {
                            e.Result.RemoveExtendedDataEntity("FBillHead", 0);//原来e.Result.RemoveExtendedDataEntity("FBillHead",i)
                            
                        }
                        e.Result.AddExtendedDataEntities("FBillHead", list2.ToArray());
                    }
                }
            }
        }




 

    }
}
