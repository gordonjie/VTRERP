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
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.EntityElement;



namespace JN.K3.YDL.Business.PlugIn
{

    /// <summary>
    /// 计量单位与酶活量的换算
    /// </summary>
    public abstract class JNQTYConversion : AbstractBillPlugIn
    {


        /// <summary>
        /// 是否正在计算数量
        /// </summary>
        protected bool isCalculateQty = false;

        /// <summary>
        /// 转换的字段参数
        /// </summary>
        public abstract List<JNQTYConversionPara> ConvertFldKey
        {
            get;
        }

        


        /// <summary>
        /// [物料]字段的标识：单据上的  物料 字段
        /// </summary>
        public virtual string MatFldKey
        {
            get
            {
                return "FMaterialId";
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
        protected void UpdateRate(string fldKey, int row)
        {
            if (!(fldKey.EqualsIgnoreCase(this.MatFldKey)
                || fldKey.EqualsIgnoreCase(this.LotNoFldKey)
                || fldKey.EqualsIgnoreCase(this.AuxPropFldKey)))
            {
                return;
            }

            string matNumber = IsMeasure(row);
            if (matNumber.IsNullOrEmptyOrWhiteSpace ())
            {
                var rateFld0 = (from p in ConvertFldKey
                               select p.ConverRateFldKey).Distinct();
                foreach (var item in rateFld0)
                {
                    this.View.Model.SetValue(item, 0, row);
                    this.View.InvokeFieldUpdateService(item, row);
                }
            }
            if (this.View.BusinessInfo.GetField(this.AuxPropFldKey) == null)
            {
                return;
            }
            if (this.View.BusinessInfo.MainOrgField == null)
            {
                return;
            }

            DynamicObject mat = this.View.Model.GetValue(this.MatFldKey, row) as DynamicObject;
            long matId = Convert.ToInt64(mat["Id"]);
            string  lotNo = "";
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
            RelatedFlexGroupField flexField = this.View.BusinessInfo.GetField(this.AuxPropFldKey) as RelatedFlexGroupField;
            if (flexField != null)
            {
                var aux = this.View.Model.GetValue(this.AuxPropFldKey, row) as DynamicObject;
                if (aux != null)
                {
                    long id = FlexServiceHelper.GetFlexDataId(this.Context, aux, flexField.BDFlexType.FormId);
                    if (id != 0)
                    {
                        para.AuxPropId = id;
                    }
                }
            }

            decimal rate = YDLCommServiceHelper.MaterialUnitEnzymes(this.Context, para);
            
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

        private long GetOrgId(int row)
        { 
            int index = 0;
            var fld = this.View.BusinessInfo.GetField(this.View.BusinessInfo.MainOrgField.Key );
            if (!(fld.Entity is HeadEntity))
            {
                index = row;
            }
            DynamicObject obj = this.View.Model.GetValue(this.View.BusinessInfo.MainOrgField.Key , index) as DynamicObject;
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
                return string.Empty ;
            }
            if (mat["FIsMeasure"] == null || Convert.ToBoolean(mat["FIsMeasure"]) == false)
            {
                //未启用双计量单位的
                return string.Empty;
            }

            string matId = Convert.ToString (mat["Number"]);

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
            if (IsMeasure(row).IsNullOrEmptyOrWhiteSpace ())
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
            else if (ConvertFldKey.Any(f => f.DestQtyFldKey.EqualsIgnoreCase(fldKey)))
            {
                var fld = ConvertFldKey.Where(f => f.DestQtyFldKey.EqualsIgnoreCase(fldKey)).ToList();
                if (IsStockIn)
                {
                    //入库： 酶活总量发生改变，反算单位酶活量
                    decimal destQty = Convert.ToDecimal(this.View.Model.GetValue(fldKey, row));
                    foreach (var item in fld)
                    {
                        decimal srcQty = Convert.ToDecimal(this.View.Model.GetValue(item.SrcQtyFldKey, row));
                        if (destQty != 0)
                        {
                            this.View.Model.SetValue(item.ConverRateFldKey, destQty / srcQty, row);
                        }
                    }
                }
                else
                {
                    //出库： 酶活总量发生改变，反算单位数量
                    decimal destQty = Convert.ToDecimal(this.View.Model.GetValue(fldKey, row));
                    foreach (var item in fld)
                    {
                        decimal rate = Convert.ToDecimal(this.View.Model.GetValue(item.ConverRateFldKey, row));
                        if (rate > 0)
                        {
                            this.View.Model.SetValue(item.SrcQtyFldKey, destQty / rate, row);
                            this.View.InvokeFieldUpdateService(item.SrcQtyFldKey, row);
                        }
                    }
                }
            }

            isCalculateQty = false;
        }

        /// <summary>
        /// 是否入库类型单据
        /// </summary>
        public bool IsStockIn
        {
            get
            {
                /*
                 select a.* ,b.FNAME 
                from T_STK_OUTINSTOCKBILL a 
                inner join  t_meta_objecttype_L b on a.FFORMID =b.fid   and FLOCALEID =2052 
                where a.FPARENTID like 'Group_In%'
                 */
                 
                var formKey = this.View.BillBusinessInfo.GetForm().Id;
                if (formKey.EqualsIgnoreCase("PRD_INSTOCK")     //生产入库单
                    ||formKey.EqualsIgnoreCase("PRD_ReturnMtrl")     //生产退料单
                    ||formKey.EqualsIgnoreCase("PUR_ReceiveBill")     //采购收料单
                    ||formKey.EqualsIgnoreCase("REM_INSTOCK")     //生产线产品入库单
                    ||formKey.EqualsIgnoreCase("REM_ReturnMtrl")     //生产线退料单
                    ||formKey.EqualsIgnoreCase("SAL_RETURNSTOCK")     //销售退货单
                    ||formKey.EqualsIgnoreCase("SP_InStock")     //简单生产入库单
                    ||formKey.EqualsIgnoreCase("SP_ReturnMtrl")     //简单生产退料单
                    ||formKey.EqualsIgnoreCase("STK_InStock")     //采购入库单
                    ||formKey.EqualsIgnoreCase("STK_InvInit")     //初始库存
                    ||formKey.EqualsIgnoreCase("STK_MISCELLANEOUS")     //其他入库单
                    ||formKey.EqualsIgnoreCase("STK_OEMInStock")     //受托加工材料入库单
                    ||formKey.EqualsIgnoreCase("STK_StockCountGain")     //盘盈单
                    ||formKey.EqualsIgnoreCase("SUB_RETURNMTRL")     //委外退料单
                    ||formKey.EqualsIgnoreCase("QM_InspectBill")     //检验单
                    )
                {
                    return true;
                }

                return false;
            }
        }


    }


}
