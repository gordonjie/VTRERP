using JN.BOS.Contracts;
using JN.K3.YDL.Core;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.MFG.EntityHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM
{
    /// <summary>
    /// 入库单保存校验器：检查相同组织+物料+辅助属性+批号相同时，是否单位酶活量是否不同
    /// </summary>
    public class JN_EnzymeValidator:AbstractValidator
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public JN_EnzymeValidator()
        {
            this.EntityKey = "FBillHead";
            this.TimingPointString = ",Save,";
        }
        
        string matFldKey = "FMaterialId";
        /// <summary>
        /// [物料]字段的标识：单据上的  物料 字段
        /// </summary>
        public   string MatFldKey
        {
            get
            {
                return matFldKey;
            }
            set
            {
                matFldKey = value;
            }
        }
        
        string lotNoFldKey = "FLOT";
        /// <summary>
        /// [批次号]字段的标识：单据上的  批次号 字段
        /// </summary>
        public   string LotNoFldKey
        {
            get
            {
                return lotNoFldKey;
            }
            set
            {
                lotNoFldKey = value;
            }
        }

        string auxPropFldKey = "FAuxPropId";
        /// <summary>
        /// [辅助属性]字段的标识：单据上的  辅助属性 字段
        /// </summary>
        public   string AuxPropFldKey
        {
            get
            {
                return auxPropFldKey;
            }
            set
            {
                auxPropFldKey = value;
            }
        }

        
        /// <summary>
        /// 校验逻辑实现
        /// </summary>
        /// <param name="dataEntities"></param>
        /// <param name="validateContext"></param>
        /// <param name="ctx"></param>
        public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
        {
            if (dataEntities == null) return;

            var enProp = validateContext.BusinessInfo.GetField(matFldKey).Entity.EntryName;
            this.EntityKey = enProp;

            var orgFldKey = validateContext.BusinessInfo.MainOrgField.Key;
            Field lotFld = validateContext.BusinessInfo.GetField(this.LotNoFldKey);
            Field matFld = validateContext.BusinessInfo.GetField(this.MatFldKey);
            Field auxFld = validateContext.BusinessInfo.GetField(this.AuxPropFldKey);
            foreach (var dataEntity in dataEntities)
            {
                DynamicObjectCollection rows = dataEntity[enProp] as DynamicObjectCollection;               
                if (rows == null || rows.Count <= 0)
                {
                    continue;
                }

                //取酶活量大于零的
                var checkRows = rows.Where(f =>Convert.ToDecimal ( f["FJNUnitEnzymes"] )> 0).ToList ();
                if (checkRows == null || checkRows.Count() <= 0)
                {
                    continue;
                }

                //先查本单的数据
                var grp1 = ( from p in checkRows 
                            select new
                            {
                                OrgID = long.Parse(dataEntity[validateContext.BusinessInfo.MainOrgField.PropertyName + "_Id"].ToString()),
                                MatId = long.Parse(p[matFld.PropertyName+ "_Id"].ToString() ),
                                AuxId = long.Parse(p[auxFld.PropertyName+ "_Id"].ToString() ),
                                LotNumber = Convert.ToString (p[lotFld.PropertyName + "_Text"] ),
                                UnitEnzymes = Convert.ToDecimal(p["FJNUnitEnzymes"]) 
                            }).Distinct ().ToList();

                var grp2 = (from p in checkRows
                            select new
                            {
                                OrgID = long.Parse(dataEntity[validateContext.BusinessInfo.MainOrgField.PropertyName + "_Id"].ToString()),
                                MatId = long.Parse(p[matFld.PropertyName + "_Id"].ToString()),
                                AuxId = long.Parse(p[auxFld.PropertyName + "_Id"].ToString()),
                                LotNumber = Convert.ToString(p[lotFld.PropertyName + "_Text"] ) 
                            }).Distinct().ToList();

                if (grp1.Count  != grp2.Count )
                {
                    validateContext.AddError(dataEntity.DataEntity, 
                        new ValidationErrorInfo("FBillNo",
                           Convert.ToString(((DynamicObject)(dataEntity.DataEntity))["Id"]),
                           dataEntity.DataEntityIndex,
                           dataEntity.RowIndex,
                           "JN-InStockCheck-002",
                           string.Format("保存失败：启用双计量单位的物料，公司+物料+辅助属性+批号所对应的单位酶活量有多个！", dataEntity.BillNo),
                           "金蝶提示"));
                    continue;
                }

                //查数据库后台的数据
                foreach (var item in checkRows)
                {
                    var matVal = matFld.GetFieldValue(item) as DynamicObject;
                    if (matVal == null)
                    {
                        continue;
                    }

                    CheckPara para = new CheckPara();
                    para.currEnFldName = validateContext.BusinessInfo.GetField(matFldKey).Entity.EntryPkFieldName;
                    para.orgId = Convert.ToInt64(dataEntity.DataEntity[validateContext.BusinessInfo.MainOrgField.PropertyName + "_Id"]);
                    para.matId = Convert.ToInt64(matVal["Id"]);
                    var lotVal = lotFld.GetFieldValue(item);
                    if (lotVal != null && !lotVal.IsNullOrEmptyOrWhiteSpace())
                    {
                        para.lotNumber = lotVal.ToString();
                    }
                    else
                    {
                        para.lotNumber = "";
                    }
                    var aux = auxFld.GetFieldValue(item) as DynamicObject;
                    if (aux != null)
                    {
                        para.auxPropId = Convert.ToInt64(aux["Id"]);
                    }

                    para.unitEnzymes = Convert.ToDecimal(item["FJNUnitEnzymes"]);
                    para.currentEntryId   = Convert.ToInt64(item["Id"]);
                    para.currFormKey = validateContext.BusinessInfo.GetForm().Id ;
                    para.ctx = ctx;

                    string errInfor = "";
                    if (CheckExists(para, out errInfor))
                    {
                        validateContext.AddError(dataEntity.DataEntity,
                        new ValidationErrorInfo("FBillNo",
                           Convert.ToString( dataEntity.DataEntity["Id"]),
                           dataEntity.DataEntityIndex,
                           dataEntity.RowIndex,
                           "JN-InStockCheck-002",
                           string.Format("保存失败：第{1}行物料，启用双计量单位，公司+物料+辅助属性+批号所对应的单位酶活量有多个({0})！", errInfor, rows.IndexOf(item) + 1),
                           "金蝶提示"));
                        continue;
                    }
                }

            }
        }
        
        private bool CheckExists(CheckPara para, out string errInfor)
        {
            errInfor = "";
            if (Check(para, "PUR_ReceiveBill", "采购收料单", out errInfor))
            {
                return true;
            }
            if (Check(para, "PRD_INSTOCK", "生产入库单", out errInfor))
            {
                return true;
            }
            if (Check(para, "PRD_MORPT", "生产汇报单", out errInfor))
            {
                return true;
            }
            if (Check(para, "SFC_OperationReport", "工序汇报单", out errInfor))
            {
                return true;
            }
            if (Check(para, "PRD_ReturnMtrl", "生产退料单", out errInfor))
            {
                return true;
            }

            if (Check(para, "SAL_RETURNSTOCK", "销售退货单", out errInfor))
            {
                return true;
            }

            if (Check(para, "SP_ReturnMtrl", "简单生产退料单", out errInfor))
            {
                return true;
            }
            if (Check(para, "SP_InStock", "简单生产入库单", out errInfor))
            {
                return true;
            }

            if (Check(para, "STK_MISCELLANEOUS", "其他入库单", out errInfor))
            {
                return true;
            }
             
            if (Check(para, "STK_InStock", "采购入库单", out errInfor))
            {
                return true;
            }

            if (Check(para, "STK_OEMInStock", "受托加工材料入库单", out errInfor))
            {
                return true;
            }

            if (Check(para, "SUB_RETURNMTRL", "委外退料单", out errInfor))
            {
                return true;
            }
            //增加直接调拨单验证
            if (TraCheck(para, "STK_TransferDirect", "直接调拨单", out errInfor))
            {
                return true;
            } 
            return false;
        }

        private bool Check(CheckPara para,string  formKey,string formCaption, out string errInfor)
        {
            errInfor = "";
            QueryBuilderParemeter qbPara = GetQBPara(para, formKey);
            try
            {
                DynamicObjectCollection datas = QueryServiceHelper.GetDynamicObjectCollection(para.ctx, qbPara);
                if (datas != null && datas.Count > 0)
                {
                    errInfor = string.Format("{4} 单号 {1} 物料编码 {2} 物料名称 {3}  单位酶活量{0} ", datas[0]["FJNUnitEnzymes"],
                        datas[0]["FBillNo"],
                        datas[0]["FMatNumber"],
                        datas[0]["FMatName"], formCaption);

                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return false;
        }

        private bool TraCheck(CheckPara para, string formKey, string formCaption, out string errInfor)
        {
            errInfor = "";

            string sql = string.Format(@" select FJNUnitEnzymes,FBILLNO,FauxPropId,t3.FNUMBER as FMatNumber,t4.FNAME as FMatName,t5.FNUMBER from T_STK_STKTRANSFERIN t1 
                                    join T_STK_STKTRANSFERINENTRY t2 on t1.FID=t2.FID
                                    join T_BD_MATERIAL t3 on t2.FMATERIALID=t3.FMATERIALID
                                    join T_BD_MATERIAL_L t4 on t3.FMATERIALID=t4.FMATERIALID
                                    join T_BD_LOTMASTER t5 on t2.FLOT=t5.FLOTID where
                                    FJNUnitEnzymes <> {0} And FSTOCKORGID ={1} And  t2.FMATERIALID = {2} And FauxPropId={3} And t5.FNUMBER = '{4}' ",
                                        para.unitEnzymes, para.orgId, para.matId,  para.auxPropId,  para.lotNumber);
           
           
            try
            {
                DynamicObjectCollection datas = DBUtils.ExecuteDynamicObject(para.ctx, sql);
                   // QueryServiceHelper.GetDynamicObjectCollection(para.ctx, qbPara);
                if (datas != null && datas.Count > 0)
                {
                    errInfor = string.Format("{4} 单号 {1} 物料编码 {2} 物料名称 {3}  单位酶活量{0} ", datas[0]["FJNUnitEnzymes"],
                        datas[0]["FBillNo"],
                        datas[0]["FMatNumber"],
                        datas[0]["FMatName"], formCaption);

                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return false;
        }

        private QueryBuilderParemeter GetQBPara(CheckPara para,string formKey)
        {
            FormMetadata formMetadata = FormMetaDataCache.GetCachedFormMetaData(this.Context, formKey);
            QueryBuilderParemeter qbPara = new QueryBuilderParemeter();
            qbPara.FormId = formKey;
            if (formKey == "STK_TransferDirect")
            {
                qbPara.FilterClauseWihtKey = string.Format(" FJNUnitEnzymes <> {0} And FSTOCKORGID ={1} And {2} = {3} And {4}={5} And {6} = '{7}' ",
                                        para.unitEnzymes,para.orgId, matFldKey, para.matId, "FAuxPropId", para.auxPropId, "FLot.FNumber", para.lotNumber);
            }
            else
            {
                qbPara.FilterClauseWihtKey = string.Format(" FJNUnitEnzymes <> {0} And {1} ={2} And {3} = {4} And {5}={6} And {7} = '{8}' ",
                                        para.unitEnzymes, formMetadata.BusinessInfo.MainOrgField.FieldName, para.orgId, matFldKey, para.matId, "FAuxPropId", para.auxPropId, "FLot.FNumber", para.lotNumber);
            }
            
            if (para.currFormKey.EqualsIgnoreCase (formKey ) )
            {
                qbPara.FilterClauseWihtKey += string.Format(" And {0} <> {1} ", para.currEnFldName, para.currentEntryId);
            }

            qbPara.SelectItems = SelectorItemInfo.CreateItems("FBillNo,FMaterialId.FNumber as FMatNumber,FMaterialId.FName as FMatName,FJNUnitEnzymes");

            return qbPara;
        }



        
        internal class CheckPara
        {
            public ValidateContext validateContext;
            public Kingdee.BOS.Context ctx;
            public string currFormKey;
            public string currEnFldName;
            public long currentEntryId;
            public long orgId;
            public long matId;
            public long auxPropId;
            public string lotNumber;
            public decimal unitEnzymes;

        }
               
    }
}
   