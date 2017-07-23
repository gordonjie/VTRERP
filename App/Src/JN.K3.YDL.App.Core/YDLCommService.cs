using JN.K3.YDL.Contracts;
using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill;
using System.Net;
using System.IO;
using Kingdee.BOS.Workflow.Models.Chart;

namespace JN.K3.YDL.App.Core
{
    public class YDLCommService : IYDLCommService
    {

        /// <summary>
        /// 获取付款和费用申请单所有单据类型说明
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="strFilter">过滤条件</param>
        /// <returns>返回描述</returns>
        public DynamicObjectCollection GetExpenseRequestOrderEditInfo(Context ctx, string strFilter)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" select b.FPKID,b.FCAPTION FVALUE,b.F_JN_YDL_Description_EXT FDESCRIPTION from T_META_FORMENUMITEM a ");
            sb.AppendLine(" inner join T_META_FORMENUMITEM_L b on a.FENUMID = b.FENUMID ");
            sb.AppendFormat(" where a.fid = (select fid from T_META_FORMENUM_L where {0}) ", strFilter);
            sb.AppendLine(" order by a.FSEQ");
            DynamicObjectCollection dyData = DBUtils.ExecuteDynamicObject(ctx, sb.ToString());
            return dyData;
        }

        /// <summary>
        /// 获取订单类型说明
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fname">单据类型分类</param>
        /// <param name="fvalue">单据类型</param>
        /// <returns>返回描述</returns>
        public string GetExpenseRequestOrderEditDescription(Context ctx, string fname, string fvalue)
        {
            string reStr = "";
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("select b.f_jn_ydl_description_ext description  from T_META_FORMENUMITEM a ");
            sql.AppendLine("inner join T_META_FORMENUMITEM_L b on a.FENUMID = b.FENUMID ");
            sql.AppendFormat("where a.fid = (select fid from T_META_FORMENUM_L where fname='{0}') and a.fvalue='{1}' ", fname, fvalue);

            var reData = DBUtils.ExecuteDynamicObject(ctx, sql.ToString());
            if (reData != null && reData.Count > 0)
            {
                reStr = (reData[0]["description"]).ToString();
            }
            return reStr;
        }

        /// <summary>
        /// 获取物料对应批次号的单位酶活量
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materialId"></param>
        /// <param name="lotNo"></param>
        /// <returns></returns> 
        public decimal MaterialUnitEnzymes(Kingdee.BOS.Context ctx, JNQTYRatePara para)
        {
            //批号跟踪里面取
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("select a.FBILLFORMID ,a.FBILLNO,a.FBILLID ,a.FBILLENTRYID ,a.FLOTFIELDKEY,a.flotid,b.FMaterialId ");
            sql.AppendLine("from T_BD_LOTMASTERBILLTRACE a ");
            sql.AppendLine("inner join T_BD_LOTMASTER b on a.flotid=b.flotid ");
            sql.AppendLine("inner join T_BD_Material c on b.FMaterialId=c.FMaterialId ");
            sql.AppendFormat("where a.FSTOCKDIRECT=1 and c.FNumber ='{0}' and b.FNUMBER ='{1}' ", para.MaterialNumber, para.LotNumber);
            if (para.OrgId > 0)
            {
                sql.AppendFormat(" and b.FCreateOrgId={0} ", para.OrgId);
            }
            sql.AppendLine("order by finstockdatetmp desc");



            var traceData = DBUtils.ExecuteDynamicObject(ctx, sql.ToString());
            if (traceData != null && traceData.Count > 0)
            {
                decimal rate = 0;
                foreach (var item in traceData)
                {
                    rate = GetBillDataEntryRate(ctx, item, para);
                    if (rate > 0)
                    {
                        return rate;
                    }
                }
            }

            //找不到就到物料的单位换算里面找
            return GetMatUnitRate(ctx, para.MaterialId);
        }


        /// <summary>
        /// 取物料的单位换算的换算率
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materialId"></param>
        /// <returns></returns>
        private decimal GetMatUnitRate(Kingdee.BOS.Context ctx, long materialId)
        {
            string sql = string.Format(@"select c.FStoreUnitID ,c.FAUXUNITID ,a.FCURRENTUNITID ,a.FDESTUNITID , 
		                            FConvertDenominator,FConvertNumerator, FConvertNumerator / FConvertDenominator as FRate  
                            from T_BD_UNITCONVERTRATE  a
                            inner join t_bd_material b on a.FMASTERID =b.FMASTERID  
                            inner join T_BD_MATERIALSTOCK c on b.FMATERIALID =c.FMATERIALId  and c.FStoreUnitID = a.FDESTUNITID and c.FAUXUNITID = a.FCurrentUnitId
                            where b.FMATERIALID = {0}
                            ", materialId);

            var rateData = DBUtils.ExecuteDynamicObject(ctx, sql);
            if (rateData != null && rateData.Count > 0)
            {
                return Convert.ToDecimal(rateData[0]["FRate"]);
            }

            return 0;
        }

        /// <summary>
        /// 来源单获取单位酶活
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="billData"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        private decimal GetBillDataEntryRate(Kingdee.BOS.Context ctx,
                                            DynamicObject billData,
                                            JNQTYRatePara para)
        {
            var formKey = billData["FBILLFORMID"].ToString();
            var lotFldKey = billData["FLOTFIELDKEY"].ToString();
            var billId = billData["FBILLID"].ToString();
            var matFldKey = "FMaterialId";
            var lotid = billData["flotid"].ToString();
            var matid = billData["FMaterialId"].ToString();

            QueryBuilderParemeter qbPara = new QueryBuilderParemeter();
            qbPara.FormId = formKey;
            qbPara.FilterClauseWihtKey = string.Format(" fid ={0} And {1} ={2} And {3}.FNumber ='{4}' And FAuxPropId={5} ",
                                    billId, lotFldKey, lotid, matFldKey, para.MaterialNumber, para.AuxPropId);
            //放到批号主档里过滤
            //if (para.OrgId > 0)
            //{
            //    qbPara.FilterClauseWihtKey +=string.Format ( " And {0}={1} ", GetOrgFldKey( ctx,formKey),para.OrgId );
            //}
            qbPara.SelectItems = SelectorItemInfo.CreateItems("FJNUnitEnzymes");
            try
            {
                DynamicObjectCollection datas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(ctx, qbPara);
                if (datas != null && datas.Count > 0)
                {
                    return Convert.ToDecimal(datas[0]["FJNUnitEnzymes"]);
                }
            }
            catch (Exception ex)
            {
                return 0;
            }

            return 0;
        }

        private string GetOrgFldKey(Context ctx, string formKey)
        {
            FormMetadata md = FormMetaDataCache.GetCachedFormMetaData(ctx, formKey);

            return md.BusinessInfo.MainOrgField.Key;
        }


        public void UpdateInspectData(Context ctx, List<string> entryPrimaryValues)
        {
            string sql = string.Format(@"/*dialect*/delete from T_QM_INSPECTBILLENTRY where fentryid in ({0}) 
            delete from T_QM_INSPECTBILLENTRY_A where fentryid in ({0}) 
            update t1 set t1.FENTRYID=t2.FENTRYID from T_QM_IBPOLICYDETAIL t1 join T_QM_INSPECTBILLENTRY_LK t2 on t1.FENTRYID=t2.FSID where t2.FSID in ({0}) and t2.FRULEID='JN_YDL_Inspect-Split' 
            update t1 set t1.FENTRYID=t2.FENTRYID from T_QM_IBITEMDETAIL t1 join T_QM_INSPECTBILLENTRY_LK t2 on t1.FENTRYID=t2.FSID where t2.FSID in ({0}) and t2.FRULEID='JN_YDL_Inspect-Split'
            update t1 set t1.FENTRYID=t2.FENTRYID from T_QM_IBDEFECTDETAIL t1 join T_QM_INSPECTBILLENTRY_LK t2 on t1.FENTRYID=t2.FSID where t2.FSID in ({0}) and t2.FRULEID='JN_YDL_Inspect-Split' 
            update t1 set t1.FENTRYID=t2.FENTRYID from T_QM_IBREFERDETAIL t1 join T_QM_INSPECTBILLENTRY_LK t2 on t1.FENTRYID=t2.FSID where t2.FSID in ({0}) and t2.FRULEID='JN_YDL_Inspect-Split' 
            update t1 set t1.FENTRYID=t2.FENTRYID from T_QM_INSPECTBILLENTRY_LK t1 join T_QM_INSPECTBILLENTRY_LK t2 on t1.FENTRYID=t2.FSID where t2.FSID in ({0}) and t2.FRULEID='JN_YDL_Inspect-Split' 
            delete from T_QM_INSPECTBILLENTRY_LK where FSID in ({0}) and FRULEID='JN_YDL_Inspect-Split'
            update t1 set t1.FTID=t2.FTID from T_BF_INSTANCEENTRY t1 join T_BF_INSTANCEENTRY t2 on t1.FTID=t2.FSID where t2.FSID in ({0}) and t2.FSTABLENAME='T_QM_INSPECTBILLENTRY' and t2.FTTABLENAME='T_QM_INSPECTBILLENTRY'
            delete from T_BF_INSTANCEENTRY where FSID in ({0}) and FSTABLENAME='T_QM_INSPECTBILLENTRY' and FTTABLENAME='T_QM_INSPECTBILLENTRY' ", string.Join(",", entryPrimaryValues));

            DBUtils.Execute(ctx, sql);
        }

        /// <summary>
        ///查询价目表的数据
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="goodsid">物料id</param>
        /// <returns></returns>        
        public DynamicObjectCollection GetPriceFormsData(Context ctx, string goodsid)
        {
            string sql = string.Format(@"select Top 1 FPRICE,FDOWNPRICE,F_JN_SALEEXPENSE,F_JN_SALEEXPENSE2,F_JN_SALESPROMOTION from T_SAL_PRICELISTENTRY E 
                                         inner join T_SAL_PRICELIST T  on E.FID=T.FID where E.FMaterialId='{0}' order by T.FAPPROVEDATE desc", goodsid);
            DynamicObjectCollection Data = null;
            Data = DBUtils.ExecuteDynamicObject(ctx, sql);
            return Data;
        }

        /// <summary>
        ///查询价目表的数据详细
        ///增加业务员、部门、销售组和普遍适应的价格查询----赵成杰20171226
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="goodsid">物料id</param>
        /// <param name="custid">客户id</param>
        /// <param name="saleId">销售员</param>
        /// <param name="currencyid">币种</param>
        /// <param name="auxpropid">辅助属性</param>
        /// <returns></returns>        
        public DynamicObjectCollection GetPriceFormsDataByCust(Context ctx, string goodsid, string custid, string saleId, string currencyid, string auxpropid)
        {
            string sql = string.Format(@"select top 1 B.FPRICE,B.FDOWNPRICE,B.F_JN_SALEEXPENSE,B.F_JN_SALEEXPENSE2,B.F_JN_SALESPROMOTION from T_SAL_PRICELIST A 
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
                            ) and A.FCURRENCYID={3} and  B.FAUXPROPID ={4}
                            order by B.FEFFECTIVEDATE desc", goodsid, custid, saleId, currencyid, auxpropid);
            DynamicObjectCollection Data = null;
            Data = DBUtils.ExecuteDynamicObject(ctx, sql);
            return Data;
        }

        /// <summary>
        /// 即时库存明细--增加单位酶活量，标吨
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public List<DynamicObject> GetInventoryAllData(Context ctx)
        {
            List<DynamicObject> obj = new List<DynamicObject>();
            string strSql = @"select t1.FID,t1.FQty,t1.FBASEQTY,t1.FSecQty,t2.FJNTONPROPERTY,t2.FIsMeasure 
                              from T_STK_INVENTORY t1
                              join T_BD_MATERIAL t2 on t1.FMATERIALID=t2.FMATERIALID ";
            DynamicObjectCollection data = DBUtils.ExecuteDynamicObject(ctx, strSql);
            if (data != null) obj = data.ToList();
            return obj;
        }

        /// <summary>
        /// 即时库存汇总数据查询--增加单位酶活量，标吨
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public List<DynamicObject> GetInvSumQueryAllData(Context ctx)
        {
            List<DynamicObject> obj = new List<DynamicObject>();
            string strSql = @"select t1.FID,t1.FQty,t1.FBASEQTY,t1.FSecQty,t2.FJNTONPROPERTY,t2.FIsMeasure 
                              from T_STK_INVSUMQUERY t1
                              join T_BD_MATERIAL t2 on t1.FMATERIALID=t2.FMATERIALID ";
            DynamicObjectCollection data = DBUtils.ExecuteDynamicObject(ctx, strSql);
            if (data != null) obj = data.ToList();
            return obj;
        }

        /// <summary>
        /// 配方单转库存检验单
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="FID">源单ID</param>
        /// <param name="FPKID">源单单据体ID</param>
        /// <param name="row">单据体行号</param>
        public IOperationResult ConvertRule(Context ctx, int FID, int FPKID, int row)
        {
            List<ListSelectedRow> ListSalReSelect = new List<ListSelectedRow>();
            ListSelectedRow convertItem = new ListSelectedRow(
                       Convert.ToString(FID),
                       Convert.ToString(FPKID),
                       Convert.ToInt32(row),
                       "PRD_PPBOM");
            ListSalReSelect.Add(convertItem);
            if (ListSalReSelect.Count <= 0)
            {
                return null;
            }
            BillConvertOption convertOption = new BillConvertOption();
            convertOption.sourceFormId = "PRD_PPBOM";
            convertOption.targetFormId = "QM_STKAPPInspect";
            convertOption.ConvertRuleKey = "UseFormToSTKAPPInspect";
            convertOption.Option = OperateOption.Create();
            convertOption.BizSelectRows = ListSalReSelect.ToArray();
            convertOption.IsDraft = true;
            convertOption.IsSave = false;
            return AppServiceContext.ConvertBills(ctx, convertOption);
        }

        /// <summary>
        ///查询酶活维护信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="goodsid">物料id</param>
        /// <returns></returns>  
        public DynamicObjectCollection GetMATERIALID(Context ctx, Int32 materialId)
        {
            string sql = string.Format(@"select A.FMATERIALID,B.FSTOCKID,B.FSTOCKPLACEID                            
                            from T_BD_MATERIAL A 
                            inner join t_BD_MaterialStock B on A.FMATERIALID=B.FMATERIALID
                            inner join T_BD_MATERIAL_L C on A.FMATERIALID=C.FMATERIALID
                            where A.FDOCUMENTSTATUS='C' and A.FFORBIDSTATUS='A' and A.FISMEASURE=1 and A.F_JN_FUNGUSCLASS={0}", materialId);
            DynamicObjectCollection Data = null;
            Data = DBUtils.ExecuteDynamicObject(ctx, sql);
            return Data;
        }

        /// <summary>
        /// 查询获取的即时库存
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="FID">id集合</param>
        /// <returns></returns>
        public DynamicObjectCollection GetINVENTORY(Context ctx, string FID)
        {
            string sql = string.Format(@"select t1.fid,t1.FSTOCKORGID ,t1.FKEEPERTYPEID ,t1.FKEEPERID ,t1.FOWNERTYPEID ,
                                        t1.FOWNERID ,t1.FSTOCKID ,t1.FSTOCKLOCID ,t1.FAUXPROPID ,t1.FSTOCKSTATUSID ,
                                        t1.flot,t1.fbomid,t1.FMTONO,t1.FPROJECTNO ,t1.FPRODUCEDATE ,t1.FEXPIRYDATE ,
                                        t1.FBASEUNITID ,t1.FBASEQTY ,t1.FBASELOCKQTY ,t1.FSECQTY ,t1.FSECLOCKQTY ,
                                        t1.FSTOCKUNITID ,t1.FMATERIALID,t1.fqty,t1.FLOCKQTY,t1.FSECUNITID,
                                        t1.FOBJECTTYPEID ,t1.FBASEAVBQTY,t1.FAVBQTY,t1.FSECAVBQTY,t1.FUPDATETIME ,
                                        t1.FJNUNITQTY,t2.FJNTONPROPERTY,t2.FIsMeasure,t2.FNUMBER
                                        from T_STK_INVENTORY t1                                         
                                        Inner join T_BD_MATERIAL t2 on t1.FMATERIALID=t2.FMATERIALID    
                                        where t1.FID in ({0}) ", FID);
            DynamicObjectCollection Data = null;
            Data = DBUtils.ExecuteDynamicObject(ctx, sql);
            return Data;
        }

        /// <summary>
        /// 获取物料对应批次号的生产日期、有限期至
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="para"></param>        
        /// <returns></returns> 
        public DynamicObjectCollection GetLotExpiryDate(Context ctx, JNQTYRatePara para)
        {
            //批号跟踪里面取
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("select a.FBILLFORMID ,a.FBILLNO,a.FBILLID ,a.FBILLENTRYID ,a.FLOTFIELDKEY,a.flotid,b.FMaterialId ");
            sql.AppendLine("from T_BD_LOTMASTERBILLTRACE a ");
            sql.AppendLine("inner join T_BD_LOTMASTER b on a.flotid=b.flotid ");
            sql.AppendLine("inner join T_BD_Material c on b.FMaterialId=c.FMaterialId ");
            sql.AppendFormat("where a.FSTOCKDIRECT=1 and c.FNumber ='{0}' and b.FNUMBER ='{1}' ", para.MaterialNumber, para.LotNumber);
            if (para.OrgId > 0)
            {
                sql.AppendFormat(" and b.FCreateOrgId={0} ", para.OrgId);
            }
            sql.AppendLine("order by finstockdatetmp desc");
            var traceData = DBUtils.ExecuteDynamicObject(ctx, sql.ToString());
            DynamicObjectCollection date = null;
            if (traceData != null && traceData.Count > 0)
            {
                foreach (var item in traceData)
                {
                    date = GetBillDataEntryDate(ctx, item, para);
                }
            }
            return date;
        }

        /// <summary>
        /// 来源单获取日期
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="billData"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public DynamicObjectCollection GetBillDataEntryDate(Kingdee.BOS.Context ctx,
                                            DynamicObject billData,
                                            JNQTYRatePara para)
        {
            var formKey = billData["FBILLFORMID"].ToString();
            var lotFldKey = billData["FLOTFIELDKEY"].ToString();
            var billId = billData["FBILLID"].ToString();
            var matFldKey = "FMaterialId";
            var lotid = billData["flotid"].ToString();
            var matid = billData["FMaterialId"].ToString();
            QueryBuilderParemeter qbPara = new QueryBuilderParemeter();
            qbPara.FormId = formKey;
            qbPara.FilterClauseWihtKey = string.Format(" fid ={0} And {1} ={2} And {3}.FNumber ='{4}' And FAuxPropId={5} ",
                                    billId, lotFldKey, lotid, matFldKey, para.MaterialNumber, para.AuxPropId);
            qbPara.SelectItems = SelectorItemInfo.CreateItems("FPRODUCEDATE,FEXPIRYDATE");
            DynamicObjectCollection datas = null;
            try
            {
                datas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(ctx, qbPara);
            }
            catch (Exception ex)
            {
                return datas;
            }
            return datas;
        }

        /// <summary>
        /// 获取酶种bom单位酶活量
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="FID">bom清单ID</param>
        /// <param name="materilId">物料ID</param>
        /// <param name="oper">工序</param>
        /// <returns></returns>
        public DynamicObjectCollection GetBom(Context ctx, int FID, int materilId, int oper)
        {
            DynamicObjectCollection datas = null;
            string sql = string.Format(@"select FJNCOMPANYEA from T_ENG_BOM A 
                                        inner join T_ENG_BOMCHILD B on A.FID=B.FID
                                        where A.FID={0} and B.FMATERIALID={1} and B.FOPERID={2} ",
                                        FID, materilId, oper);
            try
            {
                datas = DBUtils.ExecuteDynamicObject(ctx, sql);
            }
            catch (Exception ex)
            {
                return datas;
            }
            return datas;
        }

        /// <summary>
        /// 获取酶种对应库存明细
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materilId"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public DynamicObjectCollection GetInvStock(Context ctx, int materilId, long orgId)
        {
            string sql = string.Format(@"select t1.fid,t1.FSTOCKORGID ,t1.FKEEPERTYPEID ,t1.FKEEPERID ,t1.FOWNERTYPEID ,
                                        t1.FOWNERID ,t1.FSTOCKID ,t1.FSTOCKLOCID ,t1.FAUXPROPID ,t1.FSTOCKSTATUSID ,
                                        t1.flot,t1.fbomid,t1.FMTONO,t1.FPROJECTNO ,t1.FPRODUCEDATE ,t1.FEXPIRYDATE ,
                                        t1.FBASEUNITID ,t1.FBASEQTY ,ISNULL(t4.FLOCKQTY,0) FBASELOCKQTY ,t1.FSECQTY ,t1.FSECLOCKQTY ,
                                        t1.FSTOCKUNITID ,t3.FMATERIALID,t1.fqty,t1.FLOCKQTY,t1.FSECUNITID,
                                        t1.FOBJECTTYPEID ,t1.FBASEAVBQTY,t1.FAVBQTY,t1.FSECAVBQTY,t1.FUPDATETIME ,
                                        t1.FJNUNITQTY,t2.FJNTONPROPERTY,t2.FIsMeasure,t2.FNUMBER
                                        from T_STK_INVENTORY t1                                         
                                        Inner join T_BD_MATERIAL t2 on t1.FMATERIALID=t2.FMATERIALID   
                                        Inner join (
                                        select FMASTERID,FMATERIALID from T_BD_MATERIAL where FUSEORGID={1}
                                        )t3 on t2.FMASTERID=t3.FMASTERID 
                                        Left join (
                                        select tv.FID,SUM(TKE.FBASEQTY) as FLOCKQTY 
                                        from T_PLN_RESERVELINK TKH
                                        inner join  T_PLN_RESERVELINKENTRY TKE on TKE.FID = TKH.FID
                                        inner join t_stk_inventory tv on TKE.FSUPPLYINTERID = TV.FID 
                                        and TKE.FSUPPLYFORMID = 'STK_Inventory'
                                        and TKH.FRESERVETYPE =3
                                        group by tv.FID
                                        )t4 on t1.FID=t4.FID
                                        where t2.F_JN_FUNGUSCLASS={0} and t1.FSTOCKORGID={1} and t1.FBASEQTY- ISNULL(t4.FLOCKQTY,0) > 0", materilId, orgId);
            DynamicObjectCollection Data = null;
            Data = DBUtils.ExecuteDynamicObject(ctx, sql);
            return Data;
        }



        /// <summary>
        /// 获取采购目录表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materilId"></param>
        /// <param name="supplierId"></param>
        /// <param name="applicationDate"></param>
        /// <returns></returns>
        public DynamicObjectCollection GetPriceListId(Context ctx, long materilId, long supplierId, DateTime applicationDate)
        {
            string strSQL = string.Format(@"select top 1 t2.FID,T2.FPRICE,T2.FTAXPRICE from  t_PUR_PriceListEntry t2   
                      inner join t_PUR_PriceList t3 on t3.FID=t2.FID where t2.FMATERIALID={0}
	              and t3.FEFFECTIVEDATE<= '{1}' and t3.FEXPIRYDATE>='{1}' and t3.FSUPPLIERID={2} 
                  and t3.FDOCUMENTSTATUS='C' and t3.FForbidStatus='A' and t2.FDisableStatus='B' order by t2.FEFFECTIVEDATE DESC ", materilId, applicationDate, supplierId);
            return DBServiceHelper.ExecuteDynamicObject(ctx, strSQL);
        }

        /// <summary>
        /// 获取采购目录表带包装规格
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materilId"></param>
        /// <param name="supplierId"></param>
        /// <param name="auxpropId"></param>
        /// <param name="applicationDate"></param>
        /// <returns></returns>
        public DynamicObjectCollection GetAuxpropPriceListId(Context ctx, long materilId, string auxpropId, long supplierId, DateTime applicationDate)
        {
            string strSQL = string.Format(@"select top 1 t2.FID,T2.FPRICE,T2.FTAXPRICE from  t_PUR_PriceListEntry t2   
                      inner join t_PUR_PriceList t3 on t3.FID=t2.FID 
                      inner join T_BD_FLEXSITEMDETAILV t4 on t4.FID=t2.FAUXPROPID
                      where t2.FMATERIALID={0} and t4.FF100001= '{1}'
	              and t3.FEFFECTIVEDATE<= '{2}' and t3.FEXPIRYDATE>='{2}' and t3.FSUPPLIERID={3} 
                  and t3.FDOCUMENTSTATUS='C' and t3.FForbidStatus='A' and t2.FDisableStatus='B' order by t2.FEFFECTIVEDATE DESC ", materilId, auxpropId, applicationDate, supplierId);
            return DBServiceHelper.ExecuteDynamicObject(ctx, strSQL);
        }

        /// <summary>
        /// 获取价目表信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materilId"></param>
        /// <param name="supplierId"></param>
        /// <returns></returns>

        public DynamicObjectCollection GetPriceListInfo(Context ctx, long materilId, long supplierId = 0)
        {
            if (materilId <= 0)
            {
                return null;
            }
            var sql = string.Format(@"SELECT TOP 1 T2.FPRICE,T2.FTAXPRICE,T2.FTAXRATE FROM  T_PUR_PRICELISTENTRY T2  INNER JOIN T_PUR_PRICELIST T3 ON T3.FID=T2.FID  	 where t2.FMATERIALID={0} ", materilId);
            if (supplierId > 0)
            {
                sql += string.Format(" AND FSUPPLIERID={0}", supplierId);
            }
            sql += " order by t2.FEFFECTIVEDATE DESC";
            return DBServiceHelper.ExecuteDynamicObject(ctx, sql);

        }


        /// <summary>
        /// 获取打开单据信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="info">单据模型</param>
        /// <param name="BillNo">单据编号</param>
        /// <returns></returns>
        public BillShowParameter GetShowParameter(Kingdee.BOS.Context ctx, FormMetadata info, string BillNo)
        {
            BillShowParameter parameter = new BillShowParameter();
            parameter.Status = OperationStatus.EDIT;
            string Billtable = info.BusinessInfo.Entrys[0].TableName;
            string sql = string.Format("select top 1 fid from {0} where FbillNo='{1}'", Billtable, BillNo);
            var Formdatas = DBUtils.ExecuteDynamicObject(ctx, sql);
            string FormId = info.Id;
            parameter.FormId = Convert.ToString(FormId);
            parameter.PKey = Convert.ToString(Formdatas[0][0]);
            return parameter;
        }


        /// <summary>
        /// 更新凭证号到主表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="tableA">主表</param>
        /// <param name="tableB">凭证子表</param>
        /// <returns>更新行数</returns>
        public int updatevoucherNo(Context ctx, string tableA, string tableB)
        {
            string strSQL = string.Format("/*dialect*/update {0} set FvoucherNo =''", tableA);
            DBUtils.Execute(ctx, strSQL);
            string str2 = string.Format("/*dialect*/update {0} set FvoucherNo = t2.FVOUCHERGROUPNO from {1} as t1 ,(SELECT FID,    \r\n       FVOUCHERGROUPNO=( SELECT FVOUCHERGROUPNO +''    \r\n               FROM {2} b    \r\n               WHERE b.FID = a.FID    \r\n               FOR XML PATH(''))   \r\nFROM {3} AS a   \r\nGROUP BY FID)as  t2 where t1.FID=t2.FID", new object[] { tableA, tableA, tableB, tableB });
            return DBUtils.Execute(ctx, str2);
        }


        /// <summary>
        /// 获取销售预测结余表信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="OrgId"></param>
        /// <param name="SDate"></param>
        /// <param name="EDate"></param>
        /// <returns></returns>
        public DynamicObjectCollection GetForecastBalanceInfo(Context ctx, long OrgId, long DeptId, long GroupId, long SalerId, DateTime SDate, DateTime EDate)
        {
            if (OrgId <= 0)
            {
                return null;
            }
            string sql = string.Format(@"select tx.FSaleOrgId,tx.FSalerId,tx.FMaterialId,tx.FAuxPropId
                ,tx.FUnitID,FconsumQty=(case when tm.FconsumQty1 is null  then 0 else tm.FconsumQty1 end) - (case when tm.FconsumQty2 is null then 0 else tm.FconsumQty2 end) 
                ,FreductQty=(case when tm.FreductQty is null  then 0 else tm.FreductQty end),FaddQty=(case when tm.FaddQty is null  then 0 else tm.FaddQty end),FQty=(case when tx.FQty is null  then 0 else tx.FQty end)
                ,tx.FSaleDeptId
                ,FforecastQty=(case when tw.FconsumQty1 is null  then 0 else tw.FconsumQty1 end)-(case when tw.FconsumQty2 is null  then 0 else tw.FconsumQty2 end)
                ,FRate=case when ((case when tw.FconsumQty1 is null  then 0 else tw.FconsumQty1 end)-(case when tw.FconsumQty2 is null  then 0 else tw.FconsumQty2 end)+(case when tm.FaddQty is null  then 0 else tm.FaddQty end))=0 then 0 
                else ((case when tx.FQty is null  then 0 else tx.FQty end)+(case when tm.FreductQty is null  then 0 else tm.FreductQty end))/((case when tw.FconsumQty1 is null  then 0 else tw.FconsumQty1 end)-(case when tw.FconsumQty2 is null  then 0 else tw.FconsumQty2 end)+(case when tm.FaddQty is null  then 0 else tm.FaddQty end))*100 end
                from JN_T_SAL_ForecastBack tx
                left join 
                (select t1.FSaleOrgId,t1.FSalerId,t1.FSaleDeptId,t1.FMaterialId,t1.FAuxPropId,
                FconsumQty1=sum((case when t1.FDirection='B' and t1.FBillType in ('A','B','C') then t1.FAdjustQty else 0 end)),
                FconsumQty2=sum((case when t1.FDirection='A' and t1.FBillType in ('A','B','C') then t1.FAdjustQty else 0 end)),
                FreductQty=sum((case when t1.FDirection='B' and t1.FBillType='E' then t1.FAdjustQty else 0 end)),
                FaddQty=sum((case when t1.FDirection='A' and t1.FBillType='E' then t1.FAdjustQty else 0 end))
                from JN_T_SAL_ForecastLog t1
                where t1.FSaleOrgId={0} ", OrgId);
            if (DeptId > 0)
            {
                sql = sql + string.Format(@" and t1.FSaleDeptId={0}", DeptId);
            }
            if (GroupId > 0)
            {
                sql = sql + string.Format(@" and t1.FSaleGroupId={0}", GroupId);
            }
            if (SalerId > 0)
            {
                sql = sql + string.Format(@" and t1.FSalerId={0}", SalerId);
            }
            sql = sql + string.Format(@" group by t1.FSaleOrgId,t1.FSalerId,t1.FMaterialId,t1.FAuxPropId,t1.FSaleDeptId) tm
                on tx.FSaleOrgId=tm.FSaleOrgId and tx.FSalerId=tm.FSalerId and tx.FMaterialId=tm.FMaterialId and tx.FAuxPropId=tm.FAuxPropId
				and tx.FSaleDeptId=tm.FSaleDeptId 
                left join 
                (select t2.FSaleOrgId,t2.FSalerId,t2.FSaleDeptId,t2.FMaterialId,t2.FAuxPropId,
                         FconsumQty1=sum(case when t2.FDirection='A' and t2.FBillType='D' then t2.FAdjustQty else 0 end)
                        ,FconsumQty2=sum(case when t2.FDirection='B' and t2.FBillType='D' then t2.FAdjustQty else 0 end)
                        ,FconsumQty3=sum(case when t2.FDirection='A' and t2.FBillType='E' then t2.FAdjustQty else 0 end)
                        ,FconsumQty4=sum(case when t2.FDirection='B' and t2.FBillType='E' then t2.FAdjustQty else 0 end)
                 from JN_T_SAL_ForecastLog t2
                 where t2.FSaleOrgId={0} ", OrgId);
            if (DeptId > 0)
            {
                sql = sql + string.Format(@" and t2.FSaleDeptId={0}", DeptId);
            }
            if (GroupId > 0)
            {
                sql = sql + string.Format(@" and t2.FSaleGroupId={0}", GroupId);
            }
            if (SalerId > 0)
            {
                sql = sql + string.Format(@" and t2.FSalerId={0}", SalerId);
            }
            sql = sql + string.Format(@" group by t2.FSaleOrgId,t2.FSalerId,t2.FMaterialId,t2.FAuxPropId,t2.FSaleDeptId) tw
                on tx.FSaleOrgId=tw.FSaleOrgId and tx.FSalerId=tw.FSalerId and tx.FMaterialId=tw.FMaterialId and tx.FAuxPropId=tw.FAuxPropId
				and tx.FSaleDeptId=tw.FSaleDeptId  
                where tx.FSaleOrgId={0} ", OrgId);
            if (DeptId > 0)
            {
                sql = sql + string.Format(@" and tx.FSaleDeptId={0}", DeptId);
            }
            if (GroupId > 0)
            {
                sql = sql + string.Format(@" and tx.FSaleGroupId={0}", GroupId);
            }
            if (SalerId > 0)
            {
                sql = sql + string.Format(@" and tx.FSalerId={0}", SalerId);
            }
            return DBServiceHelper.ExecuteDynamicObject(ctx, sql);

        }

        /// <summary>
        /// 获取单据的审批路径
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="billname">单据名</param>
        /// <param name="billid">单据Id</param>
        /// <returns>返回审批路径</returns>
        public DynamicObjectCollection GetWorkflowChartFlowWay(Context ctx, string billname, string billId)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" select t_WF_ActInst.FActivityId,FResult=case when t_WF_ApprovalAssign.FResult='Consent' then '同意'   when t_WF_ApprovalAssign.FResult='Reject' then '返回'  when t_WF_ApprovalAssign.FResult='Dissent' then '中止' end , ");
            sb.AppendLine(" T_WF_ASSIGN_L.FASSIGNNAME,t_WF_ApprovalItem.FDisposition,t_WF_Receiver.FTITLE,t_WF_Receiver.FReceiverId, t_SEC_User.FName,t_WF_ApprovalItem.FStatus, t_WF_ApprovalItem.FActionResult, t_WF_ApprovalItem.FCompletedTime FROM t_WF_PiBiMap ");
            sb.AppendLine(" INNER JOIN t_WF_ProcInst ON (t_WF_ProcInst.FProcInstId = t_WF_PiBiMap.FProcInstId)");
            sb.AppendLine(" INNER JOIN t_WF_ActInst on (t_WF_ActInst.FProcInstId = t_WF_ProcInst.FProcInstId)");
            sb.AppendLine(" INNER JOIN t_WF_Assign on (t_WF_Assign.FActInstId = t_WF_ActInst.FActInstId)");
            sb.AppendLine("INNER JOIN T_WF_ASSIGN_L on (T_WF_ASSIGN_L.FASSIGNID = t_WF_Assign.FASSIGNID and FLOCALEID=2052)");
            sb.AppendLine(" INNER JOIN t_WF_Receiver on (t_WF_Receiver.FAssignId = t_WF_Assign.FAssignId)");
            sb.AppendLine(" LEFT join T_WF_ADDSIGNRECEIVER on (t_WF_Receiver.FID=T_WF_ADDSIGNRECEIVER.FASSIGNRECEIVERPID)");
            sb.AppendLine(" LEFT join T_WF_ADDSIGNASSIGN on (T_WF_ADDSIGNASSIGN.FADDSIGNASSIGNID=T_WF_ADDSIGNRECEIVER.FADDSIGNASSIGNID)");
            sb.AppendLine(" INNER JOIN t_SEC_User ON (t_SEC_User.FUserId = t_WF_Receiver.FReceiverId)");
            sb.AppendLine(" INNER JOIN t_WF_ApprovalAssign on (t_WF_Assign.FAssignId = t_WF_ApprovalAssign.FAssignId)");
            sb.AppendLine("    LEFT JOIN t_WF_ApprovalItem on (t_WF_ApprovalItem.FApprovalAssignId = t_WF_ApprovalAssign.FApprovalAssignId AND t_WF_ApprovalItem.FReceiverId = t_WF_Receiver.FReceiverId)");
            sb.AppendFormat(" WHERE t_WF_PiBiMap.FObjectTypeId = '{0}' AND t_WF_ApprovalAssign.FKeyValue = {1} order by t_WF_Assign.FCREATETIME,T_WF_ADDSIGNASSIGN.FNEXTADDSIGNASSIGNID*-1 ", billname, billId);
            DynamicObjectCollection dyData = DBUtils.ExecuteDynamicObject(ctx, sb.ToString());
            return dyData;
        }

        /// <summary>
        /// 银行系统对接交互返回报文
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="url">银行服务地址</param>
        /// <param name="xmlMsg">XML报文请求</param>
        /// <param name="timeout">请求时间</param>
        /// <returns>XML返回报文</returns>
        public string BandPost(Context ctx, string url, string xmlMsg, int timeout)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            Stream reqStream = null;

            String strRet = "";

            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "POST";
                request.Timeout = timeout * 1000;


                //设置POST的数据类型和长度
                request.ContentType = "text/xml";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xmlMsg);
                request.ContentLength = data.Length;


                //往服务器写入数据
                reqStream = request.GetRequestStream();
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();

                //获取服务端返回
                response = (HttpWebResponse)request.GetResponse();

                //获取服务端返回数据
                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                strRet = sr.ReadToEnd().Trim();
                sr.Close();
            }
            catch (Exception e)
            {
                return strRet;
            }
            finally
            {
                //关闭连接和流
                if (response != null)
                {
                    response.Close();
                }
                if (request != null)
                {
                    request.Abort();
                }
            }
            return strRet;
        }




        /// <summary>
        /// 是否是对应角色（用于权限判断）
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="quotationRoleId">角色Id</param>
        /// <returns></returns>
        public  bool IsquotationRoleIdRole(Context context, int quotationRoleId)
        {
            string sql = string.Format("select 1 from T_SEC_ROLEUSER where FUSERID = {0} and FROLEID = {1}", context.UserId, quotationRoleId);
            int res = DBUtils.ExecuteScalar<int>(context, sql, 0);
            return res == 1;
        }

    }
}
