using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Util;
using Kingdee.BOS.Util;
using Kingdee.BOS.Orm.DataEntity;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using JN.K3.YDL.Core;
using System.ComponentModel;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Metadata.EntityElement;
using JN.BOS.Business.BillHelper;



namespace JN.K3.YDL.Business.PlugIn
{

    /// <summary>
    /// 单位数量与酶活量的换算
    /// </summary>
     [Description("配方单-单位数量与酶活量的换算")]
    public class UseFormQTYConversion : AbstractBillPlugIn
    {
        /// <summary>
        /// 转换的字段参数
        /// </summary>
        public    List<JNQTYConversionPara> ConvertFldKey
        {
            get
            {
                List<JNQTYConversionPara> para = new List<JNQTYConversionPara>();
                para.Add(new JNQTYConversionPara()
                {
                    ConverRateFldKey = "FJNUnitEnzymes",
                    SrcQtyFldKey = "FMustQty",
                    DestQtyFldKey = "F_JN_EnzymeSumQty"
                });
                
                return para;
            }
        }
         
        /// <summary>
        /// 是否正在计算数量
        /// </summary>
        protected bool isCalculateQty = false;
 
        /// <summary>
        /// [物料]字段的标识：单据上的  物料 字段
        /// </summary>
        public virtual string MatFldKey
        {
            get
            {
                return "FMaterialID2";
            }
        }

        /// <summary>
        /// [批次号]字段的标识：单据上的  批次号 字段
        /// </summary>
        public virtual string LotNoFldKey
        {
            get
            {
                return "FLOT";
            }
        }

        /// <summary>
        /// [辅助属性]字段的标识：单据上的  辅助属性 字段
        /// </summary>
        public virtual string AuxPropFldKey
        {
            get
            {
                return "FAuxPropId";
            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);

            UpdateRate(e.Field.Key, e.Row);

            DoConversion(e.Field.Key, e.Row);
        }
         
        /// <summary>
        /// 物料改变或批次号改变时，获取 单位酶活量
        /// </summary>
        /// <param name="row"></param>
        private void UpdateRate(string fldKey, int row)
        {
            decimal rate = GetRate(fldKey, row);             
            if (rate == 0)
            {
                return;
            }
            var rateFld = (from p in ConvertFldKey
                           select p.ConverRateFldKey).Distinct();
            foreach (var item in rateFld)
            {
                this.View.Model.SetValue(item, rate, row);
                this.View.InvokeFieldUpdateService(item, row);
            }
        }

        private decimal GetRate(string fldKey, int row)
        {
            if (!(fldKey.EqualsIgnoreCase(this.MatFldKey)
                || fldKey.EqualsIgnoreCase(this.LotNoFldKey)))
            {
                return 0;
            }
            string matNumber = IsMeasure(row);
            if (matNumber.IsNullOrEmptyOrWhiteSpace())
            {
                return 0;
            }
            long AuxPropId = 0;
            DynamicObject mat = this.View.Model.GetValue(this.MatFldKey, row) as DynamicObject;
            long matId = Convert.ToInt64(mat["Id"]);
            DynamicObject AuxProp = this.View.Model.GetValue("FAuxPropID", row) as DynamicObject;            
            if (AuxProp != null && Convert.ToInt64(AuxProp["Id"]) == 0)//辅助属性
            {
                RelatedFlexGroupField flexField = this.View.BillBusinessInfo.GetField("FAuxPropID") as RelatedFlexGroupField;
                AuxPropId = LinusFlexUtil.GetFlexDataId(this.View.Context, AuxProp, flexField);
                this.View.Model.SetValue("FAuxPropID", AuxPropId, row);
            }
            else if (AuxProp != null && Convert.ToInt64(AuxProp["Id"]) > 0)
            {
                AuxPropId = Convert.ToInt64(AuxProp["Id"]);
            }
            if (Convert.ToBoolean(mat["F_JN_IsEnzyme"]))//酶种物料不需要获取单位酶活
            { return 0; }
            decimal rate = 0;
            if (fldKey.EqualsIgnoreCase(this.MatFldKey))
            {
                DynamicObject bomInfor = this.View.Model.GetValue("FBOMID") as DynamicObject;
                if (bomInfor == null)
                {
                    return 0;
                }
                DynamicObjectCollection subItemInfo = bomInfor["TreeEntity"] as DynamicObjectCollection;
                if (subItemInfo == null || subItemInfo.Count == 0)
                {
                    return 0;
                }

                var subQty = subItemInfo.FirstOrDefault(f => f["MATERIALIDCHILD_Id"].ToString() == matId.ToString());
                if (subQty != null)
                {
                    rate = Convert.ToDecimal(subQty["FJNCompanyEA"]);
                } 
            }
            else
            {
                string lotNo = "";
                var lot = this.View.Model.GetValue(this.LotNoFldKey, row);
                if (lot != null)
                {
                    if (lot.GetType() == typeof(DynamicObject))
                    {
                        lotNo = Convert.ToString((lot as DynamicObject)["Number"]);
                    }
                    else
                    {
                        lotNo = lot.ToString();
                    }
                }
                 
                JNQTYRatePara para = new JNQTYRatePara();
                para.MaterialNumber = matNumber;
                para.MaterialId = matId;
                para.LotNumber = lotNo;
                para.OrgId = GetOrgId(row);
                para.AuxPropId = AuxPropId;
                rate = YDLCommServiceHelper.MaterialUnitEnzymes(this.Context, para);
                DynamicObjectCollection date = YDLCommServiceHelper.GetLotExpiryDate(this.Context, para);
                if (date != null && date.Count > 0)
                {
                    this.View.Model.SetValue("FJNProduceDate", date[0]["FPRODUCEDATE"], row);
                    this.View.Model.SetValue("FJNExpiryDate", date[0]["FEXPIRYDATE"], row);
                }     
            }
            return rate;
        }

        private long GetOrgId(int row)
        {
            int index = 0;
            var fld = this.View.BusinessInfo.GetField(this.View.BusinessInfo.MainOrgField.Key);
            if (!(fld.Entity is HeadEntity))
            {
                index = row;
            }
            DynamicObject obj = this.View.Model.GetValue(this.View.BusinessInfo.MainOrgField.Key, index) as DynamicObject;
            long id = 0;
            if (obj != null)
            {
                id = Convert.ToInt64(obj["Id"]);
            }

            return id;
        }

        private string IsMeasure(int row)
        {
            DynamicObject mat = this.View.Model.GetValue(this.MatFldKey, row) as DynamicObject;
            if (mat == null || mat["Number"] == null || mat["Number"].ToString() == "")
            {
                return string.Empty;
            }
            if (mat["FIsMeasure"] == null || Convert.ToBoolean(mat["FIsMeasure"]) == false)
            {
                //未启用双计量单位的
                return string.Empty;
            }

            string matId = Convert.ToString(mat["Number"]);

            return matId;
        }

        /// <summary>
        /// 单位数量转换
        /// </summary>
        /// <param name="fldKey">哪个字段引起的</param>
        private void DoConversion(string fldKey, int row)
        {
            if (isCalculateQty == true)
            {
                return;
            }

            //未录入物料或物料未启用双计量单位的，不做计算 
            if (IsMeasure(row).IsNullOrEmptyOrWhiteSpace())
            {
                return;
            }

            isCalculateQty = true;

            if (ConvertFldKey.Any(f => f.SrcQtyFldKey.EqualsIgnoreCase(fldKey)))
            {
                var fld = ConvertFldKey.Where(f => f.SrcQtyFldKey.EqualsIgnoreCase(fldKey)).ToList();
                //源数量发生改变或单位酶活量发生改变，计算酶活总量
                decimal srcQty = Convert.ToDecimal(this.View.Model.GetValue(fldKey, row));
                foreach (var item in fld)
                {
                    decimal rate = Convert.ToDecimal(this.View.Model.GetValue(item.ConverRateFldKey, row));
                    this.View.Model.SetValue(item.DestQtyFldKey, srcQty * rate, row);
                    this.View.InvokeFieldUpdateService(item.DestQtyFldKey, row);
                }

            }
            else if (ConvertFldKey.Any(f => f.ConverRateFldKey.EqualsIgnoreCase(fldKey)))
            {
                var fld = ConvertFldKey.Where(f => f.ConverRateFldKey.EqualsIgnoreCase(fldKey)).ToList();
                //源数量发生改变或单位酶活量发生改变，计算酶活总量
                decimal rate = Convert.ToDecimal(this.View.Model.GetValue(fldKey, row));
                foreach (var item in fld)
                {
                    decimal srcQty = Convert.ToDecimal(this.View.Model.GetValue(item.SrcQtyFldKey, row));
                    this.View.Model.SetValue(item.DestQtyFldKey, srcQty * rate, row);
                    this.View.InvokeFieldUpdateService(item.DestQtyFldKey, row);
                }

            }
             

            isCalculateQty = false;
        }
 
    }

}
