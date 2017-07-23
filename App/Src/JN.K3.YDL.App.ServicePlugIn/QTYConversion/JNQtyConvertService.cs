using JN.K3.YDL.Core;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace JN.K3.YDL.App.ServicePlugIn.QTYConversion
{


    /// <summary>
    /// 单据转换：自动拣货的时候，自动获取 单位酶活量及计算酶活总量
    /// </summary>
    public abstract class JNQtyConvertService : AbstractConvertPlugIn
    {
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

        /// <summary>
        /// [组织属性]字段的标识：单据上的  组织属性 字段
        /// </summary>
        public virtual string OrgIdFldKey
        {
            get
            {
                return "FStockOrgId";
            }
        }

        public override void AfterConvert( AfterConvertEventArgs e)
        {
            base.AfterConvert(e);

            if (ConvertFldKey == null || ConvertFldKey.Count == 0)
            {
                return;
            }

            Field lotFld = e.TargetBusinessInfo.GetField(this.LotNoFldKey);
            Field matFld = e.TargetBusinessInfo.GetField(this.MatFldKey);
            Field auxFld = e.TargetBusinessInfo.GetField(this.AuxPropFldKey);
            Field OrgFld = e.TargetBusinessInfo.GetField(this.OrgIdFldKey);//增加组织属性
            if (lotFld == null || matFld ==null )
            {
                return;
            }

             
           
            ExtendedDataEntity[] HandDatas = e.Result.FindByEntityKey(OrgFld.EntityKey);//获取表单字段
            long OrgVal = 0;
            foreach (ExtendedDataEntity HandData in HandDatas)
            {
                if (OrgFld != null)//取组织值
                {
                    var Org = OrgFld.GetFieldValue(HandData.DataEntity) as DynamicObject;
                    if (Org != null)
                    {
                        OrgVal = Convert.ToInt64(Org["Id"]);
                    }
                }
                ExtendedDataEntity[] dataEntities = e.Result.FindByEntityKey(lotFld.EntityKey);
                foreach (ExtendedDataEntity item in dataEntities)
            { 
                var lotVal = lotFld.GetFieldValue(item.DataEntity);
                if(lotVal ==null || string.IsNullOrEmpty (lotVal.ToString ()))
                {
                    continue;
                }
                var matVal = matFld.GetFieldValue(item.DataEntity) as DynamicObject;
                if (matVal == null)
                {
                    continue;
                }
               

                JNQTYRatePara para = new JNQTYRatePara();
                para.MaterialId = Convert.ToInt64 ( matVal["Id"]);
                para.MaterialNumber = Convert.ToString (matVal["Number"]);
                para.LotNumber = lotVal.ToString();
                para.OrgId = OrgVal;


                if (auxFld != null)
                {
                    var aux = auxFld.GetFieldValue(item.DataEntity) as DynamicObject;
                    if (aux != null)
                    {
                        para.AuxPropId = Convert.ToInt64(aux["Id"]);
                    }
                }



                decimal rate = YDLCommServiceHelper.MaterialUnitEnzymes(this.Context, para);
                if (rate == 0)
                {
                    continue;
                }

                foreach (var fld in ConvertFldKey)
                {
                    Field srcQtyFld = e.TargetBusinessInfo.GetField(fld.SrcQtyFldKey );
                    Field destQtyFld = e.TargetBusinessInfo.GetField(fld.DestQtyFldKey );
                    Field rateFld = e.TargetBusinessInfo.GetField(fld.ConverRateFldKey );

                    if (srcQtyFld == null || destQtyFld == null || rateFld == null)
                    {
                        continue;
                    }

                    decimal srcQty = Convert.ToDecimal(srcQtyFld.DynamicProperty.GetValue(item.DataEntity));
                    rateFld.DynamicProperty.SetValue(item.DataEntity , rate);
                    destQtyFld.DynamicProperty.SetValue(item.DataEntity, rate * srcQty);
                }
             }
            }
        }

       
        


    }





}
