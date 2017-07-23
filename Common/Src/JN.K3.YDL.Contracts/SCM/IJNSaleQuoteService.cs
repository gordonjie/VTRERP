using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Contracts.SCM
{
    /// <summary>
    /// 销售报价改造增加服务接口
    /// </summary>
    public interface IJNSaleQuoteService
    {
        /// <summary>
        /// 根据销售报价单分录信息自动创建产品物料
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="info"></param>
        /// <param name="saleQuoteEntryRows"></param>
        /// <param name="option"></param>
        /// <param name="dynamicO"></param>
        /// <returns></returns>
        IOperationResult CreateProductMaterial(Context ctx, BusinessInfo info, DynamicObject[] saleQuoteEntryRows, OperateOption option,DynamicObject dynamicO=null);

        /// <summary>
        /// 销售订单根据信息获取销售价目表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="custId"></param>
        /// <param name="saleId"></param>
        /// <param name="materId"></param>
        /// <returns></returns>
        DynamicObjectCollection SelectSALPrice(Context ctx, long custId, long saleId, long materId);


        /// <summary>
        /// 销售订单根据信息获取销售价目表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="custId">客户</param>
        /// <param name="saleId">销售员</param>
        /// <param name="currencyid">币种</param>
        /// <param name="auxpropid">辅助属性</param>
        /// <param name="materId"></param>
        /// <returns></returns>
        DynamicObjectCollection SelectSALPrice(Context ctx, long custId, long saleId, long CurrencyId, string auxpropid, long materId);

        /// <summary>
        /// 根据资产卡片信息获取采购订单
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="billno"></param>
        /// <returns></returns>
        DynamicObjectCollection SelectOrderBillno(Context ctx, string billno);
        ///// <summary>
        ///// 当前登录信息
        ///// </summary>
        ///// <param name="ctx"></param>
        ///// <param name="userid"></param>
        ///// <returns></returns>
        //DynamicObjectCollection SelectUserInspector(Context ctx, long userid);
    }
}
