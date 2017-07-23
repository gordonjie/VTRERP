
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
using Kingdee.BOS.Workflow.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Workflow.Models.Template;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Workflow.Models.EnumStatus;
using JN.K3.YDL.ServiceHelper.SCM;
using Kingdee.BOS.App.Data;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.SaleQuote
{
    /// <summary>
    /// 销售报价单审核插件
    /// </summary>
    public class JN_Audit : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBillNo");
            e.FieldKeys.Add("FBillTypeID");
            e.FieldKeys.Add("FSaleOrgId");
            e.FieldKeys.Add("FSaleDeptId");
            e.FieldKeys.Add("FInvaliderId");
            e.FieldKeys.Add("FApproverId");
            e.FieldKeys.Add("FModifierId");
            e.FieldKeys.Add("FCreatorId");
            e.FieldKeys.Add("FCustlocId");
            e.FieldKeys.Add("FNote");
            e.FieldKeys.Add("FApplicantId");
            //FApplicantId
            
            
            e.FieldKeys.Add("FCUSTID");
            e.FieldKeys.Add("F_JN_ApplyCustIds");
            e.FieldKeys.Add("FSalerId");
            //增加销售组
            e.FieldKeys.Add("FSaleGroupId");
            e.FieldKeys.Add("FIsIncludedTax");
            e.FieldKeys.Add("FSettleCurrId");
            e.FieldKeys.Add("FEFFECTIVEDATE");
            e.FieldKeys.Add("FExpiryDate");
            e.FieldKeys.Add("F_JN_ApplyCustIds");

            e.FieldKeys.Add("F_JN_ApplyPrice");
            e.FieldKeys.Add("F_JN_ProductName");
            e.FieldKeys.Add("F_JN_MtrlGroupId");
            e.FieldKeys.Add("FMaterialId");
            e.FieldKeys.Add("FMapId");
            e.FieldKeys.Add("FAuxPropId");
            e.FieldKeys.Add("FBomId");
            e.FieldKeys.Add("FIsFree");
            e.FieldKeys.Add("FPrice");
            e.FieldKeys.Add("FTaxPrice");
            e.FieldKeys.Add("F_JN_SettlementPrice");
            e.FieldKeys.Add("F_JNWorkShopId");
            

            e.FieldKeys.Add("FTaxRate");
            e.FieldKeys.Add("FStartQty");
            e.FieldKeys.Add("FPriceUnitId");
            e.FieldKeys.Add("F_JN_SaleExpense");
            e.FieldKeys.Add("F_JN_SaleExpense2");
            e.FieldKeys.Add("F_JN_AFE1");
            e.FieldKeys.Add("F_JN_AFE2");//申请销售费用
            e.FieldKeys.Add("F_JN_SALESPROMOTION");//销售促销
            e.FieldKeys.Add("FPriceUnitQty");
            e.FieldKeys.Add("FLimitDownPrice");
            e.FieldKeys.Add("F_JN_EffectiveDate");
            e.FieldKeys.Add("F_JN_ExpiryDate");
            //销售员2
            e.FieldKeys.Add("FSalesStaffTwo");

            //物料有效成份
            e.FieldKeys.Add("F_JN_ENZYMEMATERIAL");
            //生产指标/酶活性
            e.FieldKeys.Add("FJNCompanyEA");
            //标签指标/酶活性
            e.FieldKeys.Add("F_JN_LABEL");
            e.FieldKeys.Add("F_JN_LargeText");
            e.FieldKeys.Add("F_JN_NewProduct");
            e.FieldKeys.Add("F_JNLabelRemarks");
           
        }

        /// <summary>
        /// 增加操作校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            e.Validators.Add(new JN_AuditValidator());
        }


        public override void BeforeExecuteOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);


        }
        /// <summary>
        /// 操作执行前逻辑
        /// </summary>
        /// <param name="e"></param>
        public override void BeginOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeginOperationTransactionArgs e)
        {
            if (e.DataEntitys == null) return;

            var salePriceMeta = AppServiceContext.MetadataService.Load(this.Context, "BD_SAL_PriceList") as FormMetadata;
            //var ENGBOMMeta = AppServiceContext.MetadataService.Load(this.Context, "ENG_BOM") as FormMetadata;
            var ENGBOMMeta = AppServiceContext.MetadataService.Load(this.Context, "ENG_BOM") as FormMetadata;
            var MaterialMeta = AppServiceContext.MetadataService.Load(this.Context, "BD_MATERIAL") as FormMetadata;
            var SchemeMeta = AppServiceContext.MetadataService.Load(this.Context, "QM_QCScheme") as FormMetadata;

            //TODO:判断是否要自动创建物料
            var billGroups = e.DataEntitys.GroupBy(o => Convert.ToString(o["BillTypeId_Id"]));
            foreach (var billGroup in billGroups)
            {
            
                var billTypeParaObj = AppServiceContext.GetService<ISysProfileService>().LoadBillTypeParameter(this.Context, this.BusinessInfo.GetForm().Id, billGroup.Key);
                if (billTypeParaObj == null) continue;
           
                bool bSupportNoMtrlQuote = Convert.ToBoolean(billTypeParaObj["F_JN_NoMtrlIdQuotation"]);
                string strCreateMaterialPoint = Convert.ToString(billTypeParaObj["F_JN_MtrlCreateTimePoint"]);
                
                if (bSupportNoMtrlQuote
                    && strCreateMaterialPoint.EqualsIgnoreCase("2"))
                {
                    ExtendedDataEntitySet dataEntitySet = new ExtendedDataEntitySet();
                    dataEntitySet.Parse(billGroup.ToArray(), this.BusinessInfo);
                    DynamicObject dynamic0 = billGroup.FirstOrDefault() as DynamicObject;//全部单据信息
                
                        var quoteEntryRows = dataEntitySet.FindByEntityKey("FQUOTATIONENTRY")
                            .Where(o => !o["F_JN_ProductName"].IsEmptyPrimaryKey() && (long)o["MaterialId_Id"] == 0L)
                            .Select(o => o.DataEntity)
                            .ToArray();
                        if (quoteEntryRows.Any() == false) continue;

                        //var result = AppServiceContext.GetService<IJNSaleQuoteService>().CreateProductMaterial(this.Context, this.BusinessInfo, quoteEntryRows, this.Option);
                        var result = SaleQuoteServiceHelper.CreateProductMaterial(this.Context,
                            this.BusinessInfo,
                            quoteEntryRows,
                            this.Option, dynamic0);
                        this.OperationResult.MergeResult(result);

                        if (result.IsSuccess)
                        {
                            var dctRetMtrlData = result.FuncResult as Dictionary<DynamicObject, DynamicObject>;
                            if (dctRetMtrlData != null)
                            {
                                var mtrlField = this.BusinessInfo.GetField("FMaterialId") as BaseDataField;
                                //var isNewMtrlField = this.BusinessInfo.GetField("F_JN_IsNewMtrl");
                                foreach (var kvpItem in dctRetMtrlData)
                                {
                                    var lMtrlId = (long)kvpItem.Value["Id"];
                                    string entityindex = Convert.ToString(kvpItem.Key["id"]);
                                    string sql = string.Format("update T_SAL_QUOTATIONENTRY set FMATERIALID={0} where FENTRYID={1}", lMtrlId, entityindex);
                                    DBUtils.Execute(this.Context, sql);
                                    mtrlField.RefIDDynamicProperty.SetValue(kvpItem.Key, lMtrlId);
                                    //mtrlField.
                                }
                            }
                        }
                    

                    //针对创建的物料进行重新加载引用数据
                       AppServiceContext.GetService<IDBService>().LoadReferenceObject(this.Context, billGroup.ToArray(), this.BusinessInfo.GetDynamicObjectType(), false);
                       

                }
            

                bool bAutoSyncToPriceList = Convert.ToBoolean(billTypeParaObj["F_JN_AutoSyncToPriceList"]);
                if (!bAutoSyncToPriceList) continue;

                //TODO:同步信息至价目表

                var billdata = billGroup.ToArray();
                var custID_Id = billdata[0]["CUSTID_Id"];
                var custID = billdata[0]["CUSTID"];
                var ApplyCustIds = billdata[0]["F_JN_ApplyCustIds"] as DynamicObjectCollection;
                int Custcount = ApplyCustIds.Count;
                if (Custcount > 0)
                {
                    for (int i = 0; i < Custcount; i++)
                    {
                        billdata[0]["CUSTID_Id"] = ApplyCustIds[i]["F_JN_ApplyCustIds_Id"];
                        billdata[0]["CUSTID"] = ApplyCustIds[i]["F_JN_ApplyCustIds"];
                        this.CreateOrUpdatePriceList(this.Context, salePriceMeta.BusinessInfo, billdata);
                    }

                }
                billdata[0]["CUSTID_Id"] = custID_Id;
                billdata[0]["CUSTID"] = custID;
                this.SubmitUpdateMaterial(this.Context, MaterialMeta.BusinessInfo, billGroup.ToArray());

                this.CreateOrUpdatePriceList(this.Context, salePriceMeta.BusinessInfo, billGroup.ToArray());

               
                
               bool bomok=this.CreateOrUpdateENGBOM(this.Context, ENGBOMMeta.BusinessInfo, billGroup.ToArray());
               if (bomok = false)
               {
                   e.CancelOperation = true;
               }

               this.CreateOrUpdateQCScheme(this.Context, SchemeMeta.BusinessInfo, billGroup.ToArray());
               
               


            }

            CacheUtil.ClearCache(this.Context.DBId, "BD_SAL_PriceList");
        }

        /// <summary>
        /// 创建或更新销售价目表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="salePriceInfo"></param>
        /// <param name="quoteBills"></param>
        private void CreateOrUpdatePriceList(Context ctx, BusinessInfo salePriceInfo, DynamicObject[] quoteBills)
        {

            var billGroups = quoteBills.GroupBy(o => new DynamicObjectGroupKey(new string[] { "FCUSTID", "FSalerId", "FSettleCurrId", "FIsIncludedTax", "FEFFECTIVEDATE", "FExpiryDate", "FSaleGroupId" }, this.SubBusinessInfo, "FBillHead").GetKey(o));
            List<DynamicObject> lstSaleQuoteBills = new List<DynamicObject>();
            foreach (var billGroup in billGroups)
            {
                //var allCustIds = billGroup.SelectMany(o => (DynamicObjectCollection)o["F_JN_ApplyCustIds"])
                //    .Select(o => new KeyValuePair<object, object>(o["F_JN_ApplyCustIds_Id"], ((DynamicObject)o["F_JN_ApplyCustIds"])["Name"]))
                //    .Union(new KeyValuePair<object, object>[] { new KeyValuePair<object, object>(billGroup.Key.Values[0], (billGroup.First()["CustId"] as DynamicObject)["Name"]) })
                //    .Distinct()
                //    .ToArray();
                DynamicObject custObj = (billGroup.First()["CustId"] as DynamicObject);
                var allCustIds = new KeyValuePair<object, object>[] { new KeyValuePair<object, object>(billGroup.Key.Values[0], (custObj == null) ? "" : custObj["Name"]) }
                    .Distinct()
                    .ToArray();
                foreach (var kvpItem in allCustIds)
                {
                    var salePriceListObj = AppServiceContext.GetService<IOrmDataManagerService>().LoadWithCache(ctx,
                        string.Format(@"select distinct t0.fid from T_SAL_PRICELIST t0 
                                                    left join T_SAL_APPLYCUSTOMER t1 on t0.fid=t1.fid 
                                                    left join T_SAL_APPLYSALESMAN t2 on t0.fid=t2.fid
                                                    where ((t0.FLIMITCUSTOMER='1' and t1.fcustid={0}) and
                                                          ( t0.FLIMITSALESMAN='1' and t2.FSALERID={1}) 
                                                            and t0.FSaleGroupViewId={2})
                                                            and t0.FCurrencyId={3} 
                                                            and t0.FIsIncludedTax='{4}'
                                                            and t0.FEFFECTIVEDATE>={5} and t0.FEXPIRYDATE<={6}",
                                        kvpItem.Key,
                                        billGroup.Key.Values[1],
                                        billGroup.Key.Values[6],
                                        billGroup.Key.Values[2],
                                        (bool)billGroup.Key.Values[3] ? "1" : "0",
                                        ((DateTime?)billGroup.Key.Values[4]).Value.ToKSQlFormat(),
                                        ((DateTime?)billGroup.Key.Values[5]).Value.ToKSQlFormat()),
                        salePriceInfo.GetDynamicObjectType())
                        .FirstOrDefault();

                    var modelProxy = DynamicFormModelHelper.CreateModelProxy(ctx, salePriceInfo, new DefaultValueCalculator());

                    if (salePriceListObj == null)
                    {
                        modelProxy.CreateNewData();
                        modelProxy.SetValue("FCreateOrgId", billGroup.First()["SaleOrgId_Id"]);
                        modelProxy.SetValue("FName", string.Format("{0}价目表", kvpItem.Value));
                        modelProxy.SetValue("FPriceObject", "A");
                        modelProxy.SetValue("FCurrencyId", billGroup.Key.Values[2]);
                        modelProxy.SetValue("FIsIncludedTax", billGroup.Key.Values[3]);
                        modelProxy.SetValue("FEffectiveDate", billGroup.First()["EffectiveDate"]);
                        modelProxy.SetValue("FExpiryDate", billGroup.First()["ExpiryDate"]);
                        //携带销售组
                        modelProxy.SetValue("FSaleGroupviewId", billGroup.Key.Values[6], 0);

                        if (Convert.ToInt64(billGroup.Key.Values[0]) > 0)
                        {
                            modelProxy.SetValue("FLimitCustomer", "1");
                            modelProxy.CreateNewEntryRow("FEntity2");
                            modelProxy.SetValue("FCustId", kvpItem.Key, 0);
                            modelProxy.SetValue("FIsDefList", true, 0);
                        }

                        if (Convert.ToInt64(billGroup.Key.Values[1]) > 0)
                        {
                            modelProxy.SetValue("FLimitSalesMan", "1");
                            modelProxy.CreateNewEntryRow("FEntity1");
                            modelProxy.SetValue("FSalerId", billGroup.Key.Values[1], 0);

                        }

                        salePriceListObj = modelProxy.DataObject;

                    }
                    else
                    {
                        modelProxy.CreateNewData();
                        //避免默认值覆盖
                        modelProxy.DataObject = salePriceListObj;
                    }

                    //TODO:同步报价物料信息至价目表
                    var priceListEntryRows = salePriceListObj["SAL_PRICELISTENTRY"] as DynamicObjectCollection;

                    ExtendedDataEntitySet dataEntitySet = new ExtendedDataEntitySet();
                    dataEntitySet.Parse(billGroup.ToArray(), this.BusinessInfo);
                    var quoteEntryRows = dataEntitySet.FindByEntityKey("FQUOTATIONENTRY");

                    var priceEntryEntity = salePriceInfo.GetEntity("FEntity");
                    foreach (var entryRow in quoteEntryRows)
                    {
                        if ((bool)entryRow["IsFree"]) continue;
                        var existMatchRowObj = priceListEntryRows.FirstOrDefault(o =>
                        {
                            bool bMatch = (long)o["MaterialId_Id"] == (long)entryRow["MaterialId_Id"]
                                && ((long)o["AuxPropId_Id"] == 0 || (long)o["AuxPropId_Id"] > 0 && (long)o["AuxPropId_Id"] == (long)entryRow["AuxPropId_Id"])
                                && (long)o["UnitId_Id"] == (long)entryRow["UnitId_Id"]
                                && (string)o["ForbidStatus"] == "A";
                            return bMatch;
                        });
                        if (existMatchRowObj != null && (long)existMatchRowObj["F_JN_SrcEntryId"] != (long)entryRow["Id"])
                        {
                            //原行置为失效状态
                            existMatchRowObj["ForbidStatus"] = "B";
                            existMatchRowObj["ForbiderId_Id"] = ctx.UserId;
                            existMatchRowObj["ForbidDate"] = DateTime.Now;
                            existMatchRowObj = null;
                        }

                        int newRowIndex = -1;

                        if (existMatchRowObj == null)
                        {
                            var dyEntry = modelProxy.CreateNewEntryRow(priceEntryEntity, -1, out newRowIndex);
                            //existMatchRowObj = modelProxy.GetEntityDataObject(priceEntryEntity, newRowIndex);

                            modelProxy.SetValue("FMaterialId", entryRow["MaterialId_Id"], newRowIndex);
                            modelProxy.SetValue("FMaterialTypeId", ((MaterialView)(entryRow["MaterialId"] as DynamicObject)).MaterialBaseList.First().CategoryID_Id, newRowIndex);
                            modelProxy.SetValue("FPriceUnitId", entryRow["PriceUnitId_Id"], newRowIndex);
                            modelProxy.SetValue("FUnitId", entryRow["UnitId_Id"], newRowIndex);
                            modelProxy.SetValue("FPriceBase", 1, newRowIndex);
                            modelProxy.SetValue("FAuxPropId", entryRow["AuxPropId_Id"], newRowIndex);

                            dyEntry["AuxPropId"] = entryRow["AuxPropId"];

                            modelProxy.SetValue("FRowAuditStatus", "A", newRowIndex);
                        }
                        else
                        {
                            newRowIndex = modelProxy.GetRowIndex(priceEntryEntity, existMatchRowObj);
                        }

                        modelProxy.SetValue("FPrice", entryRow["F_JN_ApplyPrice"], newRowIndex);

                        if ((bool)billGroup.Key.Values[3])
                        {
                            // modelProxy.SetValue("FDownPrice", entryRow["TaxPrice"], newRowIndex);
                            modelProxy.SetValue("FDownPrice", entryRow["F_JN_SettlementPrice"], newRowIndex);
                        }
                        else
                        {
                            //modelProxy.SetValue("FDownPrice", entryRow["Price"], newRowIndex);
                            modelProxy.SetValue("FDownPrice", entryRow["F_JN_SettlementPrice"], newRowIndex);
                        }


                        modelProxy.SetValue("F_JN_BomId", entryRow["BomId_Id"], newRowIndex);
                        modelProxy.SetValue("F_JN_PromiseQty", entryRow["FQty"], newRowIndex);

                        modelProxy.SetValue("FFromQty", entryRow["StartQty"], newRowIndex);
                        modelProxy.SetValue("FToQty", entryRow["FQty"], newRowIndex);


                        modelProxy.SetValue("F_JN_SaleExpense", entryRow["F_JN_AFE1"], newRowIndex);//销售费用1
                        modelProxy.SetValue("F_JN_SaleExpense2", entryRow["F_JN_AFE2"], newRowIndex);//销售费用2

                        modelProxy.SetValue("FEntryEffectiveDate", entryRow["F_JN_EffectiveDate"], newRowIndex);
                        modelProxy.SetValue("FEntryExpiryDate", entryRow["F_JN_ExpiryDate"], newRowIndex);

                        modelProxy.SetValue("F_JN_SrcFormId", this.BusinessInfo.GetForm().Id, newRowIndex);
                        modelProxy.SetValue("F_JN_SrcInterId", (entryRow.DataEntity.Parent as DynamicObject)["Id"], newRowIndex);
                        modelProxy.SetValue("F_JN_SrcEntryId", entryRow["Id"], newRowIndex);
                        modelProxy.SetValue("F_JN_SrcBillNo", (entryRow.DataEntity.Parent as DynamicObject)["BillNo"], newRowIndex);
                        //增加同步销售员2
                        modelProxy.SetValue("FSalesStaffTwo", entryRow["FSalesStaffTwo_Id"], newRowIndex);
                        //同步销售促销
                        modelProxy.SetValue("F_JN_SALESPROMOTION", entryRow["F_JN_SALESPROMOTION"], newRowIndex);
                        

                    }
                    modelProxy.ClearNoDataRow();
                    lstSaleQuoteBills.Add(salePriceListObj);
                }
            }

            if (lstSaleQuoteBills.Any())
            {
                var ret = AppServiceContext.GetService<ISaveService>().Save(this.Context, salePriceInfo, lstSaleQuoteBills.ToArray(), this.Option, "Save");
                this.OperationResult.MergeResult(ret);

                if (ret.SuccessDataEnity != null)
                {

                    var submitRowObjs = ret.SuccessDataEnity.Where(o => Convert.ToString(o["DocumentStatus"]).EqualsIgnoreCase("A")
                        || Convert.ToString(o["DocumentStatus"]).EqualsIgnoreCase("D")).Select(o => o["Id"]).ToArray();

                    var submitRet = AppServiceContext.SubmitService.Submit(this.Context, salePriceInfo, submitRowObjs, "Submit", this.Option);
                    this.OperationResult.MergeResult(submitRet);

                    if (submitRet.SuccessDataEnity != null)
                    {
                        var auditRowObjs = submitRet.SuccessDataEnity.Select(o => new KeyValuePair<object, object>(o["Id"], null)).ToList();
                        var auditRet = AppServiceContext.SetStatusService.SetBillStatus(this.Context, salePriceInfo, auditRowObjs, new List<object> { 1, "" }, "Audit", this.Option);
                        this.OperationResult.MergeResult(auditRet);
                    }
                }
            }
        }

  



        /// <summary>
        /// 创建物料清单
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="ENGBOMInfo"></param>
        /// <param name="quoteBills"></param>
        private bool CreateOrUpdateENGBOM(Context ctx, BusinessInfo ENGBOMInfo, DynamicObject[] quoteBills)
        {

            var ENGBOMGroups = quoteBills[0]["SAL_QUOTATIONENTRY"] as DynamicObjectCollection;
            bool Isnew = Convert.ToBoolean(quoteBills[0]["F_JN_NewProduct"]);
            int Iscreate = 0;
            if (Isnew == false) return true;
            foreach (var ENGBOMGroup in ENGBOMGroups)
            {
                var quoteEntryRows = ENGBOMGroup["T_SCM_Components"] as DynamicObjectCollection;
                foreach (var quoteEntryRow in quoteEntryRows)
                {
                    if (Convert.ToInt32(quoteEntryRow["F_JN_ENZYMEMATERIAL_Id"]) != 0)
                    {
                        Iscreate = Iscreate + 1;
                    }
                }
            }
            if (Iscreate == 0) return true;


            string SaleOrgId = quoteBills[0]["SaleOrgId_Id"].ToString();
            var SaleOrg = quoteBills[0]["SaleOrgId"];
            
            string QuoteBillno = Convert.ToString(quoteBills[0]["BillNo"]);
           
      
            List<DynamicObject> lstSaleQuoteBills = new List<DynamicObject>();

            foreach (var ENGBOMGroup in ENGBOMGroups)
            {
                string EntityNo = Convert.ToString(ENGBOMGroup["Id"]);
                string MaterialID = ENGBOMGroup["MaterialId_Id"].ToString();
                string LargeText = Convert.ToString(ENGBOMGroup["F_JNLabelRemarks"]);
                DynamicObject Materialdata = ENGBOMGroup["MaterialId"] as DynamicObject;
                DynamicObjectCollection Materialbase = Materialdata["MaterialBase"] as DynamicObjectCollection;
                DynamicObjectCollection MaterialProduce = Materialdata["MaterialProduce"] as DynamicObjectCollection;
                string F_JNWorkShopId = Convert.ToString( ENGBOMGroup["F_JNWorkShopId_Id"]);
                DynamicObject F_JNWorkShopdata = ENGBOMGroup["F_JNWorkShopId"] as DynamicObject;


                int ProduceUnit = Convert.ToInt32(MaterialProduce[0]["ProduceUnitId_Id"]);
                int BaseUnit = Convert.ToInt32(Materialbase[0]["BaseUnitId_Id"]);

                DynamicObject ENGBOMListObj;

                var modelProxy = DynamicFormModelHelper.CreateModelProxy(ctx, ENGBOMInfo, new DefaultValueCalculator());
                modelProxy.CreateNewData();
                modelProxy.SetValue("FCreateOrgID_Id", SaleOrgId);
                modelProxy.SetValue("FCreateOrgID", SaleOrg);
                modelProxy.SetValue("FUseOrgID_Id", SaleOrgId);
                modelProxy.SetValue("FUseOrgID", SaleOrg);
                modelProxy.SetValue("FMATERIALID_Id", MaterialID);
                modelProxy.SetValue("FMATERIALID", Materialdata);
                modelProxy.SetValue("FUNITID", ProduceUnit);
                modelProxy.SetValue("FBaseUnitId", BaseUnit);
                //modelProxy.SetValue("FChildBaseUnitID", BaseUnit);     
                modelProxy.SetValue("FDESCRIPTION", LargeText);
                modelProxy.SetValue("F_JNSRCBillNo", QuoteBillno);
                modelProxy.SetValue("F_JNSRCENTITYNo", EntityNo);
                modelProxy.SetValue("F_JNWorkShopId_Id", F_JNWorkShopId);
                modelProxy.SetValue("F_JNWorkShopId", F_JNWorkShopdata);



                ENGBOMListObj = modelProxy.DataObject;

                var quoteEntryRows = ENGBOMGroup["T_SCM_Components"] as DynamicObjectCollection;
                int quoteEntryRowscount = quoteEntryRows.Count;

                var BOMEntryEntity = ENGBOMInfo.GetEntity("FTreeEntity");
                int IScreate = 0;



                for (int i = 0; i < quoteEntryRowscount; i++)
                {
                    int newrowindex = 0;
                    IScreate = 0;
                    string rowid = System.Guid.NewGuid().ToString();
                    string FMATERIALIDCHILD = quoteEntryRows[i]["F_JN_ENZYMEMATERIAL_Id"].ToString();
                    DynamicObject MaterialCHILDdata = quoteEntryRows[i]["F_JN_ENZYMEMATERIAL"] as DynamicObject;
                    DynamicObjectCollection MaterialCHILDbase = MaterialCHILDdata["MaterialBase"] as DynamicObjectCollection;
                    DynamicObjectCollection MaterialCHILDStock = MaterialCHILDdata["MaterialStock"] as DynamicObjectCollection;
                    DynamicObject MaterialCHILDStockAuxUnit = MaterialCHILDStock[0]["AuxUnitID"] as DynamicObject;
                    int BaseCHILDUnit = Convert.ToInt32(MaterialCHILDbase[0]["BaseUnitId_Id"]);
                    DynamicObject FBaseUnit = MaterialCHILDbase[0]["BaseUnitId"] as DynamicObject;
                    string FBaseUnitname = Convert.ToString(FBaseUnit["Number"]);
                    string StockAuxUnitNumber = Convert.ToString(MaterialCHILDStockAuxUnit["Number"]);

                    if (FMATERIALIDCHILD != "" && FMATERIALIDCHILD != " " && FMATERIALIDCHILD != null)//申请配方物料未空
                    {
                        if (i > 0)
                        {
                            var newrow = modelProxy.CreateNewEntryRow(BOMEntryEntity, -1, out newrowindex);
                        }
                        modelProxy.SetValue("FMATERIALIDCHILD", FMATERIALIDCHILD, newrowindex);
                        modelProxy.SetValue("FCHILDUNITID", BaseCHILDUnit, newrowindex);
                        modelProxy.SetValue("FChildBaseUnitID", BaseCHILDUnit, newrowindex);  
                        modelProxy.SetValue("FDENOMINATOR", 1, newrowindex);
                        modelProxy.SetValue("FNUMERATOR", 1, newrowindex);
                        modelProxy.SetValue("FBaseNumerator", 1, newrowindex);
                        modelProxy.SetValue("FBaseDenominator", 1, newrowindex);
                        //modelProxy.SetValue("FJNCompanyEA", quoteEntryRows[i]["FJNCompanyEA"], newrowindex);
                        modelProxy.SetValue("F_JN_Clientlabel", quoteEntryRows[i]["F_JN_LABEL"], newrowindex);
                        modelProxy.SetValue("FROWID", rowid, newrowindex);
                       
                        if (StockAuxUnitNumber == "%")
                        {
                            modelProxy.SetValue("FNUMERATOR", quoteEntryRows[i]["FJNCompanyEA"], newrowindex);
                            modelProxy.SetValue("FDENOMINATOR", 100, newrowindex);
                        }
                        else
                        {
                            modelProxy.SetValue("FJNCompanyEA", quoteEntryRows[i]["FJNCompanyEA"], newrowindex);
                        }
                        IScreate = IScreate + 1;
                    }

                }




                modelProxy.ClearNoDataRow();
                if (IScreate > 0)
                {
                    lstSaleQuoteBills.Add(ENGBOMListObj);
                }

            }


            if (lstSaleQuoteBills.Any())
            {
                var ret = AppServiceContext.GetService<ISaveService>().Save(this.Context, ENGBOMInfo, lstSaleQuoteBills.ToArray(), this.Option, "Save");
                //var ret = AppServiceContext.GetService<ISaveService>().Save(this.Context, ENGBOMInfo, lstSaleQuoteBills.ToArray(), this.Option, "Save");
                this.OperationResult.MergeResult(ret);
                //var rat =AppServiceContext.GetService<ISaveService>().Save(

                if (ret.SuccessDataEnity != null)
                {

                    var dctRetBomData = ret.SuccessDataEnity.ToArray();
                    if (dctRetBomData != null)
                    {
                        foreach (var kvpItem in dctRetBomData)
                        {
                            string BOMId = Convert.ToString(kvpItem["Id"]);
                            string lMtrlId = Convert.ToString(kvpItem["MATERIALID_Id"]);
                            DynamicObject lMtrl = kvpItem["MATERIALID"] as DynamicObject;
                            string F_JNSRCBillNo = Convert.ToString(lMtrl["F_JNSRCBillNo"]);
                            var quotedata = quoteBills.Where(o => o["FMaterialId"] == lMtrlId);

                            string sql = string.Format("update T_SAL_QUOTATIONENTRY set FBOMID={0} where FMaterialId={1} and Fid in (select top 1 fid from T_SAL_QUOTATION where FBillNo='{2}' )", BOMId, lMtrlId, F_JNSRCBillNo);
                            DBUtils.Execute(this.Context, sql);


                        }
                    }
                    var submitRowObjs = ret.SuccessDataEnity.Where(o => Convert.ToString(o["DocumentStatus"]).EqualsIgnoreCase("A")
                        || Convert.ToString(o["DocumentStatus"]).EqualsIgnoreCase("D")).Select(o => o["Id"]).ToArray();




                    string formId = "ENG_BOM";
                    String[] Billarray = null;
                    int length = submitRowObjs.GetLength(0);
                    Billarray = new String[length];
                    for (int i = 0; i < length; i++)
                    {
                        Billarray[i] = Convert.ToString(submitRowObjs.GetValue(i));
                    }



                    // 读取单据的工作流配置模板
                    IWorkflowTemplateService wfTemplateService = Kingdee.BOS.Workflow.Contracts.ServiceFactory.GetWorkflowTemplateService(this.Context);
                    List<FindPrcResult> findProcResultList = wfTemplateService.GetPrcListByFormID(
                                    formId, Billarray, this.Context);
                    if (findProcResultList == null || findProcResultList.Count == 0)
                    {
                        throw new KDBusinessException("AutoSubmit-002", "查找单据适用的流程模板失败，不允许提交工作流！");
                    }

                    // 设置提交参数：忽略操作过程中的警告，避免与用户交互
                    OperateOption submitOption = OperateOption.Create();
                    submitOption.SetIgnoreWarning(true);
                    IOperationResult submitResult = null;

                    FindPrcResult findProcResult = findProcResultList[0];
                    if (findProcResult.Result == TemplateResultType.Error)
                    {
                        throw new KDBusinessException("AutoSubmit-003", "单据不符合流程启动条件，不允许提交工作流！");
                    }
                    else if (findProcResult.Result != TemplateResultType.Normal)
                    {// 本单无适用的流程图，直接走传统审批
                        ISubmitService submitService = Kingdee.BOS.App.ServiceHelper.GetService<ISubmitService>();
                        submitResult = submitService.Submit(this.Context, ENGBOMInfo,
                            submitRowObjs, "Submit", submitOption);
                    }
                    else
                    {// 走工作流
                        IBOSWorkflowService wfService = Kingdee.BOS.Workflow.Contracts.ServiceFactory.GetBOSWorkflowService(this.Context);
                        submitResult = wfService.ListSubmit(this.Context, ENGBOMInfo,
                            0, submitRowObjs, findProcResultList, submitOption);
                        this.OperationResult.MergeResult(submitResult);
                    }
                    return true;

                }
                else
                {
                    return false;
                }
            }
            else return false;
        }


        /// <summary>
        /// 修改并提交物料
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="QCSchemeInfo"></param>
        /// <param name="quoteBills"></param>
        ///
        private void SubmitUpdateMaterial(Context ctx, BusinessInfo MaterialInfo, DynamicObject[] quoteBills)
        {
            var MaterialGroups = quoteBills[0]["SAL_QUOTATIONENTRY"] as DynamicObjectCollection;
            bool Isnew = Convert.ToBoolean(quoteBills[0]["F_JN_NewProduct"]);
            int Iscreate = 0;
            if (Isnew == false) return;

            string SaleOrgId = Convert.ToString(quoteBills[0]["SaleOrgId_Id"]);
            var SaleOrg = quoteBills[0]["SaleOrgId"];
            string QuoteBillno = Convert.ToString(quoteBills[0]["BillNo"]);
            List<DynamicObject> lstMaterialBills = new List<DynamicObject>();
            foreach (var MaterialGroup in MaterialGroups)
            {
                
                string Materialdata_ID =Convert.ToString( MaterialGroup["MaterialId_ID"]) ;
                string MaterialName = Convert.ToString(MaterialGroup["F_JN_ProductName"]);
                string LabelRemarks = Convert.ToString(MaterialGroup["F_JNLabelRemarks"]);
                // DynamicObject mtrl = ret.SuccessDataEnity.FirstOrDefault();

                string sql = string.Format("update T_BD_MATERIAL set FMASTERID=FMATERIALID where FMATERIALID={0}", Materialdata_ID);
                string sql1 = string.Format("update T_BD_MATERIAL_L set FName='{0}',FDESCRIPTION='{1}' where FMATERIALID={2}",  MaterialName, LabelRemarks, Materialdata_ID);
                DBUtils.Execute(ctx, sql);
                DBUtils.Execute(ctx, sql1);



                var MaterialListObj = AppServiceContext.GetService<IOrmDataManagerService>().LoadWithCache(ctx,
    string.Format(@"select distinct FMATERIALID from T_BD_MATERIAL 
                                                    where (FMATERIALID='{0}')",

                   Materialdata_ID),
                    MaterialInfo.GetDynamicObjectType())
    .FirstOrDefault();
                if (MaterialListObj != null)
                {

                    var modelProxy = DynamicFormModelHelper.CreateModelProxy(ctx, MaterialInfo, new DefaultValueCalculator());


                    modelProxy.CreateNewData();
                    //避免默认值覆盖
                    modelProxy.DataObject = MaterialListObj;
                    modelProxy.SetValue("Name", MaterialName);
                    MaterialListObj = modelProxy.DataObject;

                    modelProxy.ClearNoDataRow();

                    lstMaterialBills.Add(MaterialListObj);
                }



                if (lstMaterialBills.Any())
                {
                    var ret = AppServiceContext.GetService<ISaveService>().Save(this.Context, MaterialInfo, lstMaterialBills.ToArray(), this.Option, "Save");
                    this.OperationResult.MergeResult(ret);

                    if (ret.SuccessDataEnity != null)
                    {
                        //更新msterID--解决工序汇报已添加相同键的项
                       // DynamicObject mtrl = ret.SuccessDataEnity.FirstOrDefault();

                       // string sql = string.Format("update T_BD_MATERIAL set FMASTERID=FMATERIALID where FMATERIALID={0}", mtrl["ID"]);
                       // DBUtils.Execute(ctx, sql);

                        var submitRowObjs = ret.SuccessDataEnity.Where(o => Convert.ToString(o["DocumentStatus"]).EqualsIgnoreCase("A")
                            || Convert.ToString(o["DocumentStatus"]).EqualsIgnoreCase("D")).Select(o => o["Id"]).ToArray();

                        string formId = "BD_MATERIAL";
                        String[] Billarray = null;
                        int length = submitRowObjs.GetLength(0);
                        Billarray = new String[length];
                        for (int i = 0; i < length; i++)
                        {
                            Billarray[i] = Convert.ToString(submitRowObjs.GetValue(i));
                        }


                        if (length > 0)
                        {
                            // 读取单据的工作流配置模板
                            IWorkflowTemplateService wfTemplateService = Kingdee.BOS.Workflow.Contracts.ServiceFactory.GetWorkflowTemplateService(this.Context);
                            List<FindPrcResult> findProcResultList = wfTemplateService.GetPrcListByFormID(
                                            formId, Billarray, this.Context);

                            if (findProcResultList == null || findProcResultList.Count == 0)
                            {
                                throw new KDBusinessException("AutoSubmit-002", "查找单据适用的流程模板失败，不允许提交工作流！");
                            }

                            // 设置提交参数：忽略操作过程中的警告，避免与用户交互
                            OperateOption submitOption = OperateOption.Create();
                            submitOption.SetIgnoreWarning(true);
                            IOperationResult submitResult = null;

                            FindPrcResult findProcResult = findProcResultList[0];
                            if (findProcResult.Result == TemplateResultType.Error)
                            {
                                throw new KDBusinessException("AutoSubmit-003", "单据不符合流程启动条件，或者已经提交工作流！");
                            }
                            else if (findProcResult.Result != TemplateResultType.Normal)
                            {// 本单无适用的流程图，直接走传统审批
                                ISubmitService submitService = Kingdee.BOS.App.ServiceHelper.GetService<ISubmitService>();
                                submitResult = submitService.Submit(this.Context, MaterialInfo,
                                    submitRowObjs, "Submit", submitOption);
                            }
                            else
                            {// 走工作流
                                IBOSWorkflowService wfService = Kingdee.BOS.Workflow.Contracts.ServiceFactory.GetBOSWorkflowService(this.Context);
                                submitResult = wfService.ListSubmit(this.Context, MaterialInfo,
                                    0, submitRowObjs, findProcResultList, submitOption);
                                this.OperationResult.MergeResult(submitResult);
                            }
                        }
                    }
                }

            }

        }


        /// <summary>
        /// 创建质检方案
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="QCSchemeInfo"></param>
        /// <param name="quoteBills"></param>
        ///
        private void CreateOrUpdateQCScheme(Context ctx, BusinessInfo SchemeInfo, DynamicObject[] quoteBills)
        {
            var SchemeGroups = quoteBills[0]["SAL_QUOTATIONENTRY"] as DynamicObjectCollection;
            bool Isnew = Convert.ToBoolean(quoteBills[0]["F_JN_NewProduct"]);
            int Iscreate = 0;
            if (Isnew == false) return;
            foreach (var SchemeGroup in SchemeGroups)
            {
                var quoteEntryRows = SchemeGroup["T_SCM_Components"] as DynamicObjectCollection;
                foreach (var quoteEntryRow in quoteEntryRows)
                {
                    if (Convert.ToInt32(quoteEntryRow["F_JN_ENZYMEMATERIAL_Id"]) != 0)
                    {
                        Iscreate = Iscreate + 1;
                    }
                }
            }
            if (Iscreate == 0) return;


            string SaleOrgId = Convert.ToString(quoteBills[0]["SaleOrgId_Id"]);
            var SaleOrg = quoteBills[0]["SaleOrgId"];
            string QuoteBillno = Convert.ToString(quoteBills[0]["BillNo"]);
            List<DynamicObject> lstSchemeBills = new List<DynamicObject>();
            foreach (var SchemeGroup in SchemeGroups)
            {
                DynamicObject Materialdata = SchemeGroup["MaterialId"] as DynamicObject;
                DynamicObjectCollection Materialbase = Materialdata["MaterialBase"] as DynamicObjectCollection;
                string MaterialName = Convert.ToString(SchemeGroup["F_JN_ProductName"]);//物料名称
                string EntityNo = Convert.ToString(SchemeGroup["Id"]);
                string LabelRemarks = Convert.ToString(SchemeGroup["F_JNLabelRemarks"]);


                var SchemeListObj = AppServiceContext.GetService<IOrmDataManagerService>().LoadWithCache(ctx,
    string.Format(@"select distinct t0.fid from T_QM_QCSCHEME t0 
                                    join T_QM_QCSCHEME_L t1 on t0.FID=t1.FID                                              
                                                    where (t0.F_JNSRCBillNo='{0}' and t0.F_JNSRCENTITYNo='{1}')",

                    QuoteBillno, EntityNo),
                    SchemeInfo.GetDynamicObjectType())
    .FirstOrDefault();
                var modelProxy = DynamicFormModelHelper.CreateModelProxy(ctx, SchemeInfo, new DefaultValueCalculator());



                if (SchemeListObj == null)
                {
                    modelProxy.CreateNewData();
                    modelProxy.SetValue("FCreateOrgID", SaleOrg);
                    modelProxy.SetValue("FUseOrgID_Id", SaleOrgId);
                    modelProxy.SetValue("FUseOrgID", SaleOrg);
                    modelProxy.SetValue("Fname", MaterialName);
                    modelProxy.SetValue("F_JNSRCBillNo", QuoteBillno);
                    modelProxy.SetValue("F_JNSRCENTITYNo", EntityNo);
                    modelProxy.SetValue("FDESCRIPTION", LabelRemarks);

                    SchemeListObj = modelProxy.DataObject;

                    //TODO:同步产品配方信息至检验项目
                    var quoteEntryRows = SchemeGroup["T_SCM_Components"] as DynamicObjectCollection;
                    int quoteEntryRowscount = quoteEntryRows.Count;
                    var SchemeEntryEntity = SchemeInfo.GetEntity("FEntity");

                    // var SchemeListEntryRows = SchemeListObj["ENTRY"] as DynamicObjectCollection;

                    int IScreate = 0;



                    for (int i = 0; i < quoteEntryRowscount; i++)
                    {
                        int newrowindex = 0;
                        //IScreate = 0;
                        string rowid = System.Guid.NewGuid().ToString();
                        string FMATERIALIDCHILD = quoteEntryRows[i]["F_JN_ENZYMEMATERIAL_Id"].ToString();
                        double FTargetVal = Convert.ToDouble( quoteEntryRows[i]["FJNCompanyEA"]);
                        DynamicObject MaterialCHILDdata = quoteEntryRows[i]["F_JN_ENZYMEMATERIAL"] as DynamicObject;
                        int MaterialInspectItem_Id = Convert.ToInt32(MaterialCHILDdata["F_JNInspectItem_Id"]);
                        DynamicObject MaterialInspectItem = MaterialCHILDdata["F_JNInspectItem"] as DynamicObject;
                        //int BaseCHILDUnit = Convert.ToInt32(MaterialInspectItem[0]["BaseUnitId_Id"]);
                        if (FMATERIALIDCHILD != "" && FMATERIALIDCHILD != " " && FMATERIALIDCHILD != null && MaterialInspectItem != null)//申请配方物料未空
                        {
                            if (i > 0)
                            {
                                var newrow = modelProxy.CreateNewEntryRow(SchemeEntryEntity, -1, out newrowindex);
                            }

                            modelProxy.SetValue("FInspectItemId_Id", MaterialInspectItem_Id, newrowindex);
                            modelProxy.SetValue("FInspectItemId", MaterialInspectItem, newrowindex);
                            modelProxy.SetValue("FAnalysisMethod", 1, newrowindex);
                            modelProxy.SetValue("FTargetVal", FTargetVal, newrowindex);
                            modelProxy.SetValue("FTargetValQ", FTargetVal, newrowindex);
                            modelProxy.SetValue("FCompareSymbol", 3, newrowindex);


                            IScreate = IScreate + 1;
                        }
                        //if(


                        //dyEntry.SetDynamicObjectItemValue("FMATERIALIDCHILD", FMATERIALIDCHILD);
                    }




                    modelProxy.ClearNoDataRow();
                    if (IScreate > 0)
                    {
                        lstSchemeBills.Add(SchemeListObj);
                    }
                }
                else
                {
                    modelProxy.CreateNewData();
                    //避免默认值覆盖
                    modelProxy.DataObject = SchemeListObj;
                }



                if (lstSchemeBills.Any())
                {
                    var ret = AppServiceContext.GetService<ISaveService>().Save(this.Context, SchemeInfo, lstSchemeBills.ToArray(), this.Option, "Save");
                    this.OperationResult.MergeResult(ret);

                    if (ret.SuccessDataEnity != null)
                    {
                        var dctRetQCSchemeData = ret.SuccessDataEnity.ToArray();
                        if (dctRetQCSchemeData != null)
                        {
                            foreach (var kvpItem in dctRetQCSchemeData)
                            {
                                var QCSchemeId = (long)kvpItem["Id"];
                                string billNo = Convert.ToString(kvpItem["F_JNSRCBillNo"]);
                                string entityNo = Convert.ToString(kvpItem["F_JNSRCENTITYNo"]);

                                string sql = string.Format("update T_BD_MATERIALQUALITY set FIncQcSchemeId ={0} where FMATERIALID in (select  top 1 FMATERIALID from T_SAL_QUOTATIONENTRY t1 join T_SAL_QUOTATION t2 on t1.fid=t2.fid where t1.FENTRYID={1} and t2.fbillno='{2}')", QCSchemeId, entityNo, billNo);
                                DBUtils.Execute(this.Context, sql);
                            }
                        }
                        var submitRowObjs = ret.SuccessDataEnity.Where(o => Convert.ToString(o["DocumentStatus"]).EqualsIgnoreCase("A")
                            || Convert.ToString(o["DocumentStatus"]).EqualsIgnoreCase("D")).Select(o => o["Id"]).ToArray();

                        string formId = "QM_QCScheme";
                        String[] Billarray = null;
                        int length = submitRowObjs.GetLength(0);
                        Billarray = new String[length];
                        for (int i = 0; i < length; i++)
                        {
                            Billarray[i] = Convert.ToString(submitRowObjs.GetValue(i));
                        }



                        // 读取单据的工作流配置模板
                        IWorkflowTemplateService wfTemplateService = Kingdee.BOS.Workflow.Contracts.ServiceFactory.GetWorkflowTemplateService(this.Context);
                        List<FindPrcResult> findProcResultList = wfTemplateService.GetPrcListByFormID(
                                        formId, Billarray, this.Context);
                        if (findProcResultList == null || findProcResultList.Count == 0)
                        {
                            throw new KDBusinessException("AutoSubmit-002", "查找单据适用的流程模板失败，不允许提交工作流！");
                        }

                        // 设置提交参数：忽略操作过程中的警告，避免与用户交互
                        OperateOption submitOption = OperateOption.Create();
                        submitOption.SetIgnoreWarning(true);
                        IOperationResult submitResult = null;

                        FindPrcResult findProcResult = findProcResultList[0];
                        if (findProcResult.Result == TemplateResultType.Error)
                        {
                            throw new KDBusinessException("AutoSubmit-003", "单据不符合流程启动条件，不允许提交工作流！");
                        }
                        else if (findProcResult.Result != TemplateResultType.Normal)
                        {// 本单无适用的流程图，直接走传统审批
                            ISubmitService submitService = Kingdee.BOS.App.ServiceHelper.GetService<ISubmitService>();
                            submitResult = submitService.Submit(this.Context, SchemeInfo,
                                submitRowObjs, "Submit", submitOption);
                        }
                        else
                        {// 走工作流
                            IBOSWorkflowService wfService = Kingdee.BOS.Workflow.Contracts.ServiceFactory.GetBOSWorkflowService(this.Context);
                            submitResult = wfService.ListSubmit(this.Context, SchemeInfo,
                                0, submitRowObjs, findProcResultList, submitOption);
                            this.OperationResult.MergeResult(submitResult);
                        }
                    }
                }

            }

        }

    }
}
