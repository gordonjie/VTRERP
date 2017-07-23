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
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.MFG.InspectBill
{
    /// <summary>
    /// 检验单服务端校验插件
    /// </summary>
    [Description("检验单服务端校验插件")]
    public class JN_InspectBillAduitValidator : AbstractValidator
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public JN_InspectBillAduitValidator()
        {
            this.EntityKey = "FBillHead";
            this.TimingPointString = ",Save,";
        }

        /// <summary>
        /// [物料]字段的标识：单据上的  物料 字段
        /// </summary>
        string matFldKey = "FMaterialId";
        /// <summary>
        /// [批次号]字段的标识：单据上的  批次号 字段
        /// </summary>
        string lotNoFldKey = "FLOT";
        /// <summary>
        /// [辅助属性]字段的标识：单据上的  辅助属性 字段
        /// </summary>
        string auxPropFldKey = "FAuxPropId";       

        
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
          
            foreach (var dataEntity in dataEntities)
            {
                string billType = dataEntity["FBillTypeID_Id"].ToString();
                if (!(billType == "005056c00008baa211e3097d9bc0332e" || billType == "005056945db184ed11e3af2dcda7ee49")) continue;//产品检验单\自制工序检验单
                DynamicObjectCollection rows = dataEntity[enProp] as DynamicObjectCollection;   
                if (rows == null || rows.Count <= 0)
                {
                    continue;
                }

                //取酶活量大于零的
                var checkRows = rows.Where(f => Convert.ToDecimal(f["FJNUnitEnzymes"]) > 0).ToList();
                if (checkRows == null || checkRows.Count() <= 0)
                {
                    continue;
                }

                //先查本单的数据
                var grp1 = ( from p in checkRows 
                            select new
                            {
                                OrgID = long.Parse(dataEntity[validateContext.BusinessInfo.MainOrgField.PropertyName + "_Id"].ToString()),
                                MatId = long.Parse(p["MaterialId_Id"].ToString()),
                                AuxId = long.Parse(p["AuxPropId_Id"].ToString()),
                                LotNumber = Convert.ToString (p["Lot_Text"] ),
                                UnitEnzymes = Convert.ToDecimal(p["FJNUnitEnzymes"]) 
                            }).Distinct ().ToList();

                var grp2 = (from p in checkRows
                            select new
                            {
                                OrgID = long.Parse(dataEntity[validateContext.BusinessInfo.MainOrgField.PropertyName + "_Id"].ToString()),
                                MatId = long.Parse(p["MaterialId_Id"].ToString()),
                                AuxId = long.Parse(p["AuxPropId_Id"].ToString()),
                                LotNumber = Convert.ToString(p["Lot_Text"]) 
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
                           "金蝶提示", ErrorLevel.Warning));
                    continue;
                }

                //查数据库后台的数据
                foreach (var item in checkRows)
                {
                    var matVal = item["MaterialId"] as DynamicObject;
                    if (matVal == null)
                    {
                        continue;
                    }                   
                    CheckPara para = new CheckPara();
                    para.lotNumber = item["Lot_Text"].ToString();
                    if (para.lotNumber.Length == 0)
                    {
                        continue;
                    }
                    DynamicObjectCollection linkData = item["FEntity_Link"] as DynamicObjectCollection;                  
                    para.tableName = linkData[0]["sTableName"].ToString();
                    para.linkId = string.Join(",", linkData.Select(s => Convert.ToInt32(s["SBillId"])));
                    para.currEnFldName = validateContext.BusinessInfo.GetField(matFldKey).Entity.EntryPkFieldName;
                    para.orgId = Convert.ToInt64(dataEntity.DataEntity[validateContext.BusinessInfo.MainOrgField.PropertyName + "_Id"]);
                    para.matId = Convert.ToInt64(matVal["Id"]);                                     
                    var aux = item["AuxPropId"] as DynamicObject;
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
                           "金蝶提示", ErrorLevel.Warning));
                        continue;
                    }
                }

            }
        }
        
        private bool CheckExists(CheckPara para, out string errInfor)
        {
            errInfor = "";
            if (Check(para, "PUR_ReceiveBill", "采购收料单", "T_PUR_ReceiveEntry", "FDetailEntity", out errInfor))
            {
                return true;
            }
            if (Check(para, "PRD_INSTOCK", "生产入库单", "T_PRD_INSTOCKENTRY", "FEntity", out errInfor))
            {
                return true;
            }
            if (Check(para, "PRD_MORPT", "生产汇报单", "T_PRD_MORPTENTRY", "FEntity", out errInfor))
            {
                return true;
            }
            if (Check(para, "SFC_OperationReport", "工序汇报单", "T_SFC_OPTRPTENTRY", "FEntity", out errInfor))
            {
                return true;
            }
            //if (Check(para, "PRD_ReturnMtrl", "生产退料单", "T_PRD_RETURNMTRLENTRY", "FEntity", out errInfor))
            //{
            //    return true;
            //}

            if (Check(para, "SAL_RETURNSTOCK", "销售退货单", "T_SAL_RETURNSTOCKENTRY", "FEntity", out errInfor))
            {
                return true;
            }
            //if (Check(para, "SP_ReturnMtrl", "简单生产退料单", "T_SP_RETURNMTRLENTRY", out errInfor))
            //{
            //    return true;
            //}
            //if (Check(para, "SP_InStock", "简单生产入库单", "T_SP_INSTOCKENTRY", out errInfor))
            //{
            //    return true;
            //}

            if (Check(para, "STK_MISCELLANEOUS", "其他入库单", "T_STK_MISCELLANEOUSENTRY", "FEntity", out errInfor))
            {
                return true;
            }

            if (Check(para, "STK_InStock", "采购入库单", "T_STK_INSTOCKENTRY", "FInStockEntry", out errInfor))
            {
                return true;
            }
            if (Check(para, "QM_InspectBill", "检验单", "T_QM_INSPECTBILLENTRY", "FEntity", out errInfor))
            {
                return true;
            }
            return false;
        }

        private bool Check(CheckPara para,string  formKey,string formCaption, string tableName, string entryFormKey, out string errInfor)
        {
            errInfor = "";
            QueryBuilderParemeter qbPara = GetQBPara(para, formKey, tableName, entryFormKey);
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

        private QueryBuilderParemeter GetQBPara(CheckPara para,string formKey, string tableName, string entryFormKey)
        {
            FormMetadata formMetadata = FormMetaDataCache.GetCachedFormMetaData(this.Context, formKey);
            QueryBuilderParemeter qbPara = new QueryBuilderParemeter();
            qbPara.FormId = formKey;            
            qbPara.FilterClauseWihtKey = string.Format(" FJNUnitEnzymes <> {0} And {1} ={2} And {3} = {4} And {5}={6} And {7} = '{8}' ",
                                    para.unitEnzymes, formMetadata.BusinessInfo.MainOrgField.FieldName, para.orgId, matFldKey, para.matId, "FAuxPropId", para.auxPropId, "FLOT_TEXT", para.lotNumber);            
            if (para.currFormKey.EqualsIgnoreCase (formKey ) )
            {
                qbPara.FilterClauseWihtKey += string.Format(" And {2}_{0} <> {1} ", para.currEnFldName, para.currentEntryId, entryFormKey);
            }
            if (para.tableName == tableName)
            {
                qbPara.FilterClauseWihtKey += string.Format(" And FID not in ({0})", para.linkId);
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
            public string tableName;
            public string linkId;
        }

       
    }
}
