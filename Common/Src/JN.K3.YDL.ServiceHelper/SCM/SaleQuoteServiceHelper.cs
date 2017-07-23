using JN.K3.YDL.Contracts;
using JN.K3.YDL.Contracts.SCM;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.ServiceHelper.SCM
{
    /// <summary>
    /// 销售报价服务工具类
    /// </summary>
    public static class SaleQuoteServiceHelper
    {
        /// <summary>
        /// 根据销售报价单明细分录信息创建产品代码
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="info"></param>
        /// <param name="saleQuoteEntryRows"></param>
        /// <param name="option"></param>
        /// <param name="dynamicO"></param>
        /// <returns></returns>
        public static IOperationResult CreateProductMaterial(Context ctx, BusinessInfo info, DynamicObject[] saleQuoteEntryRows, OperateOption option, DynamicObject dynamicO = null)
        {
            IJNSaleQuoteService service = ServiceFactory.GetService<IJNSaleQuoteService>(ctx);
            try
            {
                return service.CreateProductMaterial(ctx, info, saleQuoteEntryRows, option,dynamicO);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        /// 根据资产卡片信息获取采购订单
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="billno"></param>
        /// <returns></returns>
        public static DynamicObjectCollection SelectOrderBillno(Context ctx, string billno)
        {
            IJNSaleQuoteService service = ServiceFactory.GetService<IJNSaleQuoteService>(ctx);
            try
            {
                return service.SelectOrderBillno(ctx, billno);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        /// 销售订单根据信息获取销售价目表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="custId"></param>
        /// <param name="saleId"></param>
        /// <param name="materId"></param>
        /// <returns></returns>
        public static DynamicObjectCollection SelectSALPrice(Context ctx, long custId, long saleId, long materId)
        {
            IJNSaleQuoteService service = ServiceFactory.GetService<IJNSaleQuoteService>(ctx);
            try
            {
                return service.SelectSALPrice(ctx, custId, saleId, materId);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
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
        public static DynamicObjectCollection SelectSALPrice(Context ctx, long custId, long saleId, long CurrencyId, string auxpropid, long materId)
        {
            IJNSaleQuoteService service = ServiceFactory.GetService<IJNSaleQuoteService>(ctx);
            try
            {
                return service.SelectSALPrice(ctx, custId, saleId,CurrencyId,auxpropid, materId);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        ///// <summary>
        ///// 获取质检员信息
        ///// </summary>
        ///// <param name="ctx"></param>
        ///// <param name="userid"></param>
        ///// <returns></returns>
        //public static DynamicObjectCollection SelectUserInspector(Context ctx,long userid)
        //{
        //    IJNSaleQuoteService service = ServiceFactory.GetService<IJNSaleQuoteService>(ctx);
        //    try
        //    {
        //        return service.SelectUserInspector(ctx, userid);
        //    }
        //    finally
        //    {
        //        ServiceFactory.CloseService(service);
        //    }
        //}
    }
}
