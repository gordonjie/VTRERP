using JN.BOS.Contracts;
using JN.K3.YDL.Contracts.SCM;
using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS.Workflow.Contracts;
using Kingdee.BOS.Workflow.Models.Template;
using Kingdee.BOS.Workflow.Models.EnumStatus;

namespace JN.K3.YDL.App.Core.SCM
{
    /// <summary>
    /// 销售报价相关的服务接口实现
    /// </summary>
    public class SaleQuoteService : IJNSaleQuoteService
    {
        /// <summary>
        /// 根据销售报价明细分录创建产品代码
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="info">销售报价单模型信息</param>
        /// <param name="saleQuoteEntryRows">销售报价单表体分录集合信息</param>
        /// <param name="option"></param>
        /// <returns></returns>
        public IOperationResult CreateProductMaterial(Context ctx, BusinessInfo info, DynamicObject[] saleQuoteEntryRows, OperateOption option, DynamicObject dynamicO = null)
        {
            IOperationResult result = new OperationResult();
            result.IsSuccess = false;

            
            DynamicObject customer = dynamicO["CustId"] as DynamicObject;//客户信息

            //if (customer == null)
            //{
            //    throw new KDBusinessException("", "产品创建失败：请先录入客户信息！");
            //}
            string custName = (customer == null) ? "" : customer["Name"].ToString();
            long userId = (long)dynamicO["SaleOrgId_Id"];//使用组织
            string billno=Convert.ToString(dynamicO["BillNo"]);//单据号

            if (saleQuoteEntryRows == null || saleQuoteEntryRows.Any() == false) return result;

            Dictionary<DynamicObject, DynamicObject> dctQuoteMaterialData = new Dictionary<DynamicObject, DynamicObject>();

            var billTypeField = info.GetBillTypeField();
            if (billTypeField == null) return result;

            FormMetadata mtrlMetadata = AppServiceContext.MetadataService.Load(ctx, "BD_MATERIAL") as FormMetadata;

            var saleQuoteEntryGroups = saleQuoteEntryRows.GroupBy(o => billTypeField.RefIDDynamicProperty.GetValue<string>(o.Parent as DynamicObject));
            foreach (var saleQuoteGroup in saleQuoteEntryGroups)
            {
                var billTypeParaObj = AppServiceContext.GetService<ISysProfileService>().LoadBillTypeParameter(ctx, info.GetForm().Id, saleQuoteGroup.Key);
                if (billTypeParaObj == null) continue;

                //产品组别没有配置对应物料模板
                var billTypeParaTplMtrlRows = billTypeParaObj["QuoteMtrlTplEntity"] as DynamicObjectCollection;
                foreach (var quoteEntryRow in saleQuoteGroup)
                {
                    var matchTplMtrlRowObj = billTypeParaTplMtrlRows.FirstOrDefault(o => (long)o["F_JN_MtrlGroupId_Id"] == (long)quoteEntryRow["F_JN_MtrlGroupId_Id"]);
                    if (matchTplMtrlRowObj == null)
                    {
                        var strMtrlGroupName = "";
                        if (quoteEntryRow["F_JN_MtrlGroupId"] is DynamicObject)
                        {
                            strMtrlGroupName = Convert.ToString((quoteEntryRow["F_JN_MtrlGroupId"] as DynamicObject)["name"]);
                        }
                        throw new KDBusinessException("", string.Format("产品创建失败：产品组别{0}未配置对应的模板物料！", strMtrlGroupName));
                    }

                    var lRefMtrlTplId = (long)matchTplMtrlRowObj["F_JN_TplMtrlId_Id"];
                    DynamicObject refMtrlObject = null;
                    if (lRefMtrlTplId > 0)
                    {
                        refMtrlObject = AppServiceContext.ViewService.LoadWithCache(ctx, new object[] { lRefMtrlTplId }, mtrlMetadata.BusinessInfo.GetDynamicObjectType(), true, null)
                            .FirstOrDefault();
                    }

                    if (refMtrlObject == null)
                    {
                        var strMtrlGroupName = "";
                        if (quoteEntryRow["F_JN_MtrlGroupId"] is DynamicObject)
                        {
                            strMtrlGroupName = Convert.ToString((quoteEntryRow["F_JN_MtrlGroupId"] as DynamicObject)["name"]);
                        }
                        throw new KDBusinessException("", string.Format("产品创建失败：产品组别{0}关联的模板物料不存在！", strMtrlGroupName));
                    }

                    //通过克隆生成新物料数据包
                    var newMtrlObject = refMtrlObject.Clone(false, true) as DynamicObject;
                    dctQuoteMaterialData[quoteEntryRow] = newMtrlObject;

                    //TODO:新物料数据包需要覆盖及重写的属性
                    newMtrlObject["DocumentStatus"] = "Z";
                    newMtrlObject["ForbidStatus"] = "A";
                    string name = quoteEntryRow["F_JN_ProductName"] as string;
                    if (name.IsNullOrEmptyOrWhiteSpace()) name = custName + "特配";
                    newMtrlObject["Name"] = new LocaleValue(name);
                    newMtrlObject["MaterialGroup_Id"] = quoteEntryRow["F_JN_MtrlGroupId_Id"];
                    newMtrlObject["CreateOrgId_Id"] = userId;
                    newMtrlObject["UseOrgId_Id"] = userId;
                    newMtrlObject["F_JNSRCBillNo"] = billno;
                    //统一根据编码规则生成
                    newMtrlObject["Number"] = AppServiceContext.GetService<IBusinessDataService>().GetListBillNO(ctx, "BD_MATERIAL", 1, "565c204c1f5abf")[0];                   
                    //计量单位本可以根据报价分录去重写，但目前可以考虑放在模板物料中设置，一个行业的产品代码特性通常相同的。
                }
            }


            if(dctQuoteMaterialData.Any())
            {
                var saveRet = AppServiceContext.SaveService.Save(ctx, mtrlMetadata.BusinessInfo, dctQuoteMaterialData.Values.ToArray(), option, "Save");
                result.MergeResult(saveRet);

                if (saveRet.SuccessDataEnity != null)
                {
                    //更新msterID--解决工序汇报已添加相同键的项
                    DynamicObject mtrl = saveRet.SuccessDataEnity.FirstOrDefault();

                    string sql = string.Format("update T_BD_MATERIAL set FMASTERID=FMATERIALID where FMATERIALID={0}", mtrl["ID"]);
                    DBUtils.Execute(ctx, sql);
                }
                    /*
                    //启动审批流
                        // 读取单据的工作流配置模板
                        var submitRowObjs = saveRet.SuccessDataEnity.Where(o => Convert.ToString(o["DocumentStatus"]).EqualsIgnoreCase("A")
                               || Convert.ToString(o["DocumentStatus"]).EqualsIgnoreCase("D")).Select(o => o["Id"]).ToArray();
                        string formId = "BD_MATERIAL";
                        String[] Billarray = null;
                        int length = submitRowObjs.GetLength(0);
                        Billarray = new String[length];
                        for (int i = 0; i < length; i++)
                        {
                            Billarray[i] = Convert.ToString(submitRowObjs.GetValue(i));
                        }
                        IWorkflowTemplateService wfTemplateService = Kingdee.BOS.Workflow.Contracts.ServiceFactory.GetWorkflowTemplateService(ctx);
                        List<FindPrcResult> findProcResultList = wfTemplateService.GetPrcListByFormID(
                                        formId, Billarray, ctx);
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
                            submitResult = submitService.Submit(ctx, mtrlMetadata.BusinessInfo,
                                submitRowObjs, "Submit", submitOption);
                        }
                        else
                        {// 走工作流
                            IBOSWorkflowService wfService = Kingdee.BOS.Workflow.Contracts.ServiceFactory.GetBOSWorkflowService(ctx);
                            submitResult = wfService.ListSubmit(ctx, mtrlMetadata.BusinessInfo,
                                0, submitRowObjs, findProcResultList, submitOption);
                            result.MergeResult(submitResult);
                        }
               
                    
                    var submitRet = AppServiceContext.SubmitService.Submit(ctx, mtrlMetadata.BusinessInfo, saveRet.SuccessDataEnity.Select(o => o["Id"]).ToArray(), "Submit", option);
                    result.MergeResult(submitRet);
                    if (submitRet.SuccessDataEnity != null)
                    {
                        var auditResult = AppServiceContext.SetStatusService.SetBillStatus(ctx, mtrlMetadata.BusinessInfo,
                            submitRet.SuccessDataEnity.Select(o => new KeyValuePair<object, object>(o["Id"], 0)).ToList(),
                            new List<object> { "1", "" },
                            "Audit", option);

                        result.MergeResult(auditResult);
                    }
                     
                }*/

                }
                result.IsSuccess = true;
            result.FuncResult = dctQuoteMaterialData;
            return result;
        }


        /// <summary>
        /// 销售订单根据信息获取销售价目表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="custId">客户</param>
        /// <param name="saleId">销售员</param>
        /// <param name="materId">物料</param>
        /// <returns></returns>
        public DynamicObjectCollection SelectSALPrice(Context ctx, long custId, long saleId, long materId)
        {
            DynamicObjectCollection Dynamic = null;
            try
            {
                string sql = string.Format(@"select top 1 A.FID,B.FPRICE from T_SAL_PRICELIST A 
                            inner join T_SAL_PRICELISTENTRY B on A.FID=B.FID
                            where A.FAUDITSTATUS='A' and A.FDOCUMENTSTATUS='C' and A.FEXPIRYDATE > GETDATE() and A.FEFFECTIVEDATE <= GETDATE()
                            and A.FFORBIDSTATUS='A' and B.FFORBIDSTATUS='A' and B.FEXPRIYDATE> GETDATE() and B.FEFFECTIVEDATE <= GETDATE() 
                            and B.FMATERIALID={0} and
                            (
                            (isnull(A.FLIMITCUSTOMER,'')='' and isnull(A.FLIMITSALESMAN,'')='') or 
                            (A.FLIMITCUSTOMER='1' and exists (select 1  from T_SAL_APPLYCUSTOMER C where C.FID=A.FID and C.FCUSTID={1})) or
                            (A.FLIMITCUSTOMER='2' and exists (select 1 from T_BD_CUSTOMER where FCUSTTYPEID 
                            in(select FCUSTTYPEID  from T_SAL_APPLYCUSTOMER C where C.FID=A.FID and C.FCUSTID={1}))) or
                            (A.FLIMITSALESMAN='1' and exists (select FSALERID from T_SAL_APPLYSALESMAN D where D.FID=A.FID and FSALERID={2} )) or
                            (A.FLIMITSALESMAN='2' and exists (select 1 from V_BD_SALESMAN E inner join V_BD_SALESMANENTRY F on E.fid=F.fid where 
                            F.FOPERATORGROUPID in (select FSALEGROUPID from T_SAL_APPLYSALESMAN D where D.FID=A.FID and FSALERID={2}))) or
                            (A.FLIMITSALESMAN='3' and exists (select 1 from V_BD_SALESMAN E where 
                            e.FDEPTID in (select FDEPTID from T_SAL_APPLYSALESMAN D where D.FID=A.FID and FSALERID={2}))) 
                            ) 
                            order by B.FEFFECTIVEDATE desc", materId, custId, saleId);
                Dynamic = DBUtils.ExecuteDynamicObject(ctx, sql, null, null, CommandType.Text, null);
            }
            catch (Exception ex)
            {

            }
            return Dynamic;
        }


        /// <summary>
        /// 销售订单根据信息获取销售价目表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="custId"></param>
        /// <param name="saleId">销售员</param>
        /// <param name="currencyid">币种</param>
        /// <param name="auxpropid">辅助属性</param>
        /// <param name="materId"></param>
        /// <returns></returns>
        public DynamicObjectCollection SelectSALPrice(Context ctx, long custId, long saleId, long CurrencyId, string auxpropid, long materId)
        {
            DynamicObjectCollection Dynamic = null;
            try
            {
                string sql = string.Format(@"select top 1 A.FID,B.FPRICE from T_SAL_PRICELIST A 
                            inner join T_SAL_PRICELISTENTRY B on A.FID=B.FID
                            inner join T_BD_FLEXSITEMDETAILV C on B.FAUXPROPID=C.Fid
                            where A.FAUDITSTATUS='A' and A.FDOCUMENTSTATUS='C' and A.FEXPIRYDATE > GETDATE() and A.FEFFECTIVEDATE <= GETDATE()
                            and A.FFORBIDSTATUS='A' and B.FFORBIDSTATUS='A' and B.FEXPRIYDATE> GETDATE() and B.FEFFECTIVEDATE <= GETDATE() 
                            and B.FMATERIALID={0} and
                            (
                            (isnull(A.FLIMITCUSTOMER,'')='' and isnull(A.FLIMITSALESMAN,'')='') or 
                            (A.FLIMITCUSTOMER='1' and exists (select 1  from T_SAL_APPLYCUSTOMER C where C.FID=A.FID and C.FCUSTID={1})) or
                            (A.FLIMITCUSTOMER='2' and exists (select 1 from T_BD_CUSTOMER where FCUSTTYPEID 
                            in(select FCUSTTYPEID  from T_SAL_APPLYCUSTOMER C where C.FID=A.FID and C.FCUSTID={1}))) or
                            (A.FLIMITSALESMAN='1' and exists (select FSALERID from T_SAL_APPLYSALESMAN D where D.FID=A.FID and FSALERID={2} )) or
                            (A.FLIMITSALESMAN='2' and exists (select 1 from V_BD_SALESMAN E inner join V_BD_SALESMANENTRY F on E.fid=F.fid where 
                            F.FOPERATORGROUPID in (select FSALEGROUPID from T_SAL_APPLYSALESMAN D where D.FID=A.FID and FSALERID={2}))) or
                            (A.FLIMITSALESMAN='3' and exists (select 1 from V_BD_SALESMAN E where 
                            e.FDEPTID in (select FDEPTID from T_SAL_APPLYSALESMAN D where D.FID=A.FID and FSALERID={2}))) 
                            ) and A.FCURRENCYID={3} and C.FF100001 ='{4}'
                            order by B.FEFFECTIVEDATE desc", materId, custId, saleId, CurrencyId, auxpropid);
                Dynamic = DBUtils.ExecuteDynamicObject(ctx, sql, null, null, CommandType.Text, null);
            }
            catch (Exception ex)
            {

            }
            return Dynamic;
        }
        /// <summary>
        /// 根据资产卡片信息获取采购订单
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="billno"></param>
        /// <returns></returns>
        public DynamicObjectCollection SelectOrderBillno(Context ctx, string billno)
        {
            string sql = string.Format(@"select t8.FPRICE,t8.FTAXAMOUNT,t7.FQty,(t8.FTAXAMOUNT/t7.FQty)TAXAMOUNT from t_fa_card t1 inner join T_FA_CARDDETAIL t2 on t1.FALTERID=t2.FALTERID
                                        inner join T_FA_FINANCE t3 on t3.FALTERID=t2.FALTERID 
                                        inner join T_PUR_Receive t4 on t4.FBILLNO=t2.FSOURCEBILLNO
                                        inner join T_PUR_ReceiveEntry t5 on t5.FID=t4.FID
                                        inner join t_PUR_POOrder t6 on t6.FBILLNO=t5.FSRCBILLNO
                                        inner join t_PUR_POOrderEntry t7 on t7.FID=t6.FID
                                        inner join T_PUR_POORDERENTRY_F t8 on t7.FID=t8.FID
                                        where t2.FSOURCEBILLNO='{0}'", billno);
            DynamicObjectCollection doc = DBUtils.ExecuteDynamicObject(ctx, sql);
            return doc;
        }

        //public DynamicObjectCollection SelectUserInspector(Context ctx, long userid)
        //{
        //    string sql = string.Format("select FLINKOBJECT from T_SEC_USER where FUSERID='{0}'", userid);
        //    DynamicObjectCollection docs = DBUtils.ExecuteDynamicObject(ctx, sql);
        //    if (docs != null && docs.Count > 0 && docs[0]["FLinkObject"] != null)
        //    {
        //        long persionid = Convert.ToInt64(docs[0]["FLinkObject"]);
        //        sql = string.Format("select FSTAFFID from T_HR_EMPINFO where FPERSONID='{0}'", persionid);
        //        DynamicObjectCollection obj = DBUtils.ExecuteDynamicObject(ctx, sql);
        //        if (obj != null && obj.Count > 0 && obj[0]["fstaffid"] != null)
        //        {
        //            long staffid = Convert.ToInt64(obj[0]["fstaffid"]);
        //            sql = string.Format("select FENTRYID from T_BD_OPERATORENTRY where FSTAFFID='{0}' and FOPERATORTYPE='ZJY'", staffid);
        //            DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(ctx, sql);
                    
        //            if (objs != null && objs.Count > 0)
        //            {
        //                long fentryid = Convert.ToInt64(objs[0]["FENTRYID"]);
        //                sql = string.Format("select fid,FDEPTID from V_BD_INSPECTOR where fid='{0}'", fentryid);
        //                DynamicObjectCollection coll = DBUtils.ExecuteDynamicObject(ctx, sql);
        //                return coll;
        //            }
                    
        //        }
        //    }
        //    //如果一行都没有，用户自己填。不用默认
        //    sql = string.Format(@"select top 1 fid from V_BD_INSPECTOR  where  FBIZORGID=-1");
        //    docs = DBUtils.ExecuteDynamicObject(ctx, sql);
        //    return docs;
        //}
    }
}
