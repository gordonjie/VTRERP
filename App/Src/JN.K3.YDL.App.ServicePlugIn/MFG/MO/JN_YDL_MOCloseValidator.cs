using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.MFG.MO
{
    /// <summary>
    /// 生产订单-执行至结案校验插件
    /// </summary>
    [Description("生产订单-执行至结案校验插件")]
    class JN_YDL_MOCloseValidator : AbstractValidator
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public JN_YDL_MOCloseValidator()
        {
            this.EntityKey = "FBillHead";
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
            foreach (var dataEntity in dataEntities)
            {
                string billNo = dataEntity["BillNo"].ToString();//单据编号
                string pickMtrl = string.Format(@"select t1.FBILLNO,FDOCUMENTSTATUS from  T_PRD_PICKMTRL t1
                                inner join T_PRD_PICKMTRLDATA t2 on t1.FID=t2.FID 
                                where FMOBILLNO='{0}'  
                                group by t1.FBILLNO,FDOCUMENTSTATUS", billNo);//生产领料单
                DynamicObjectCollection pickMtrlData = DBUtils.ExecuteDynamicObject(this.Context, pickMtrl);
                List<string> pickMtrlList = pickMtrlData.Where(o => o["FDOCUMENTSTATUS"].ToString() != "C").Select(o => o["FBILLNO"].ToString()).ToList();
                if (pickMtrlList.Count() > 0)
                {
                    string pickMtrlNo = string.Join(",", pickMtrlList);
                    validateContext.AddError(dataEntity.DataEntity,
                            new ValidationErrorInfo("FBillNo",
                               Convert.ToString(((DynamicObject)(dataEntity.DataEntity))["Id"]),
                               dataEntity.DataEntityIndex,
                               dataEntity.RowIndex,
                               "JN-YDL-MOClose",
                               string.Format("执行至结案失败：下游单据生产领料单：{0}还未审核！", pickMtrlNo.Substring(0, pickMtrlNo.Length - 1)),
                               "金蝶提示"));
                    continue;
                }
                string inStock = string.Format(@"select t1.FBILLNO,t1.FDOCUMENTSTATUS from T_PRD_INSTOCK t1 
                                inner join T_PRD_INSTOCKENTRY t2 on t1.FID=t2.FID 
                                where t2.FMOBILLNO='{0}'
                                group by t1.FBILLNO,t1.FDOCUMENTSTATUS", billNo);//生产入库单
                DynamicObjectCollection inStockData = DBUtils.ExecuteDynamicObject(this.Context, inStock);
                List<string> inStockList = inStockData.Where(o => o["FDOCUMENTSTATUS"].ToString() != "C").Select(o => o["FBILLNO"].ToString()).ToList();
                if (inStockList.Count() > 0)
                {
                    string inStockNo = string.Join(",", inStockList);
                    validateContext.AddError(dataEntity.DataEntity,
                            new ValidationErrorInfo("FBillNo",
                               Convert.ToString(((DynamicObject)(dataEntity.DataEntity))["Id"]),
                               dataEntity.DataEntityIndex,
                               dataEntity.RowIndex,
                               "JN-YDL-MOClose",
                               string.Format("执行至结案失败：下游单据生产入库单：{0}还未审核！", inStockNo.Substring(0, inStockNo.Length - 1)),
                               "金蝶提示"));
                    continue;
                }
            }
        }


    }
}
