using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM
{
    /// <summary>
    /// 采购入库单保存校验器
    /// </summary>
    [Description("采购入库单保存校验器")]
    public class JN_STK_InStockSaveValidator : AbstractValidator
    {
        string matFldKey = "FMaterialId";

        /// <summary>
        /// 关联标示
        /// </summary>
        private string linkKey = "FInStockEntry_Link";

        /// <summary>
        /// 构造函数
        /// </summary>
        public JN_STK_InStockSaveValidator()
        {
            this.EntityKey = "FBillHead";
            this.TimingPointString = ",Save,";
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
            foreach (var dataEntity in dataEntities)
            {
                DynamicObjectCollection rows = dataEntity[enProp] as DynamicObjectCollection;
                DateTime rkDate = Convert.ToDateTime(dataEntity["Date"]);//入库日期
                bool isSave = true;
                string billNo = "";
                foreach (DynamicObject item in rows)
                {
                    DynamicObjectCollection linkData = item[linkKey] as DynamicObjectCollection;
                    if (linkData == null || linkData.Count == 0) continue;
                    if (linkData[0]["sTableName"].ToString() != "T_PUR_ReceiveEntry") continue;//跳过非收料单下推
                    billNo = dateValitor(rkDate, linkData);
                    if (billNo.Length > 0)
                    {
                        isSave = false;
                        break;
                    }
                }
                if (!isSave)
                {
                    validateContext.AddError(dataEntity.DataEntity,
                            new ValidationErrorInfo("FBillNo",
                               Convert.ToString(((DynamicObject)(dataEntity.DataEntity))["Id"]),
                               dataEntity.DataEntityIndex,
                               dataEntity.RowIndex,
                               "JN-InStockCheck-002",
                               string.Format("保存失败：单据由编号为：\"{0}\"采购入库单关联生存，入库日期必须大于或等于收料日期！", billNo),
                               "金蝶提示"));
                    continue;
                }               
            }
        }

        /// <summary>
        /// 入库日期校验
        /// </summary>
        /// <param name="date"></param>
        /// <param name="linkData"></param>
        private string dateValitor(DateTime date, DynamicObjectCollection linkData)
        {
            string billNo = "";
            string sql = string.Format("select FDATE,FBILLNO from T_PUR_Receive where FID in ({0})", string.Join(",", linkData.Select(s => Convert.ToInt32(s["SBillId"]))));
            DynamicObjectCollection reData = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (reData.Any(a => date < Convert.ToDateTime(a["FDATE"])))
            {
                billNo = Convert.ToString(reData.FirstOrDefault(f => date < Convert.ToDateTime(f["FDATE"]))["FBILLNO"]);
            }
            //foreach (DynamicObject item in linkData)
            //{
            //    int receiveId = Convert.ToInt32(item["SBillId"]);
            //    if (item["sTableName"].ToString() != "T_PUR_ReceiveEntry") continue;//跳过非收料单下推
            //    string sql = string.Format("select FDATE,FBILLNO from T_PUR_Receive where FID={0}", receiveId);
            //    DynamicObjectCollection reData = DBUtils.ExecuteDynamicObject(this.Context, sql);
            //    if (reData == null || reData.Count == 0) continue;
            //    DateTime receiveDate = Convert.ToDateTime(reData[0]["FDATE"]);                
            //    if (date < receiveDate)
            //    {
            //        billNo = reData[0]["FBILLNO"].ToString();
            //        break;
            //    }
            //}
            return billNo;
        }
    }
}
