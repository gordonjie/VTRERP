using JN.K3.YDL.Contracts;
using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Bill;

namespace JN.K3.YDL.ServiceHelper
{
    public static class YDLCommServiceHelper
    {

        /// <summary>
        /// 获取付款和费用申请单所有单据类型说明
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="strFilter">过滤条件</param>
        /// <returns>返回描述</returns>
        public static DynamicObjectCollection GetExpenseRequestOrderEditInfo(Context ctx, string strFilter)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetExpenseRequestOrderEditInfo(ctx, strFilter);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        /// 获取订单类型说明
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fname">单据类型分类</param>
        /// <param name="fvalue">单据类型</param>
        /// <returns>返回描述</returns>
        public static string GetExpenseRequestOrderEditDescription(Context ctx, string fname, string fvalue)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetExpenseRequestOrderEditDescription(ctx, fname, fvalue);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }


        /// <summary>
        /// 银行系统对接交互返回报文
        /// </summary>
        /// <param name="url">银行服务地址</param>
        /// <param name="xmlMsg">XML报文请求</param>
        /// <param name="timeout">请求时间</param>
        /// <returns>XML返回报文</returns>
        public static string BandPost(Context ctx, string url, string xmlMsg, int timeout)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.BandPost(ctx, url, xmlMsg, timeout);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        /// 获取物料对应批次号的单位酶活量
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materialId"></param>
        /// <param name="lotId"></param>
        /// <returns></returns> 
        public static decimal MaterialUnitEnzymes(Context ctx, JNQTYRatePara para)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.MaterialUnitEnzymes(ctx, para);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }


        public static void UpdateInspectData(Context ctx, List<string> entryPrimaryValues)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                service.UpdateInspectData(ctx, entryPrimaryValues);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }
        public static DynamicObjectCollection GetPriceFormsData(Context ctx, string goodsid)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetPriceFormsData(ctx, goodsid);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        public static DynamicObjectCollection GetPriceFormsDataByCust(Context ctx, string goodsid, string custid, string saleId, string currencyid, string auxpropid)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetPriceFormsDataByCust(ctx, goodsid, custid, saleId,  currencyid, auxpropid);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }
        /// <summary>
        /// 即时库存明细--增加单位酶活量，标吨
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static List<DynamicObject> GetInventoryAllData(Context ctx)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetInventoryAllData(ctx);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        /// 即时库存汇总数据查询--增加单位酶活量，标吨
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static List<DynamicObject> GetInvSumQueryAllData(Context ctx)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetInvSumQueryAllData(ctx);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        /// 配方单转库存检验单
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="FID">源单ID</param>
        /// <param name="FPKID">源单单据体ID</param>
        /// <param name="row">单据体行号</param>
        public static IOperationResult ConvertRule(Context ctx, int FID, int FPKID, int row)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.ConvertRule(ctx, FID, FPKID, row);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        ///查询酶活维护信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="goodsid">物料id</param>
        /// <returns></returns>  
        public static DynamicObjectCollection GetMATERIALID(Context ctx, Int32 materialId)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetMATERIALID(ctx, materialId);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        /// 查询获取的即时库存
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="FID">id集合</param>
        /// <returns></returns>
        public static DynamicObjectCollection GetINVENTORY(Context ctx, string FID)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetINVENTORY(ctx, FID);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        /// 获取物料对应批次号的生产日期、有限期至
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="para"></param>        
        /// <returns></returns> 
        public static DynamicObjectCollection GetLotExpiryDate(Context ctx, JNQTYRatePara para)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetLotExpiryDate(ctx, para);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        /// 获取酶种bom单位酶活量
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="FID">bom清单ID</param>
        /// <param name="materilId">物料ID</param>
        /// <param name="oper">工序</param>
        /// <returns></returns>
        public static DynamicObjectCollection GetBom(Context ctx, int FID, int materilId, int oper)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetBom(ctx, FID, materilId, oper);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        /// 获取酶种对应库存明细
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materilId"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public static DynamicObjectCollection GetInvStock(Context ctx, int materilId, long orgId)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetInvStock(ctx, materilId, orgId);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }


        /// <summary>
        /// 获取采购目录表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materilId"></param>
        /// <param name="supplierId"></param>
        /// <param name="applicationDate"></param>
        /// <returns></returns>
        public static DynamicObjectCollection GetPriceListId(Context ctx, long materilId, long supplierId, DateTime applicationDate)
        {

            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetPriceListId(ctx, materilId, supplierId, applicationDate);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
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
        public static DynamicObjectCollection GetAuxpropPriceListId(Context ctx, long materilId, string auxpropId, long supplierId, DateTime applicationDate)
        {

            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetAuxpropPriceListId(ctx, materilId, auxpropId, supplierId, applicationDate);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }

        /// <summary>
        /// 获取价目表信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materilId"></param>
        /// <param name="supplierId"></param>
        /// <returns></returns>
        public static DynamicObjectCollection GetPriceListInfo(Context ctx, long materilId, long supplierId = 0)
        {

            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetPriceListInfo(ctx, materilId, supplierId);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }

        }

        /// <summary>
        /// 获取打开单据信息
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="info">单据模型</param>
        /// <param name="BillNo">单据编号</param>
        /// <returns></returns>
        public static BillShowParameter GetShowParameter(Kingdee.BOS.Context ctx, FormMetadata info, string BillNo)
        {

            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetShowParameter(ctx, info, BillNo);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }

        }



        /// <summary>
        /// 更新凭证号到主表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="tableA">主表</param>
        /// <param name="tableB">凭证子表</param>
        /// <returns>更新行数</returns>
        public static int updatevoucherNo(Context ctx, string tableA, string tableB)
        {
            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.updatevoucherNo(ctx, tableA, tableB);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }
        }


        /// <summary>
        /// 获取销售预测结余表信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="OrgId"></param>
        /// <param name="SDate"></param>
        /// <param name="EDate"></param>
        /// <returns></returns>
        public static DynamicObjectCollection GetForecastBalanceInfo(Context ctx, long OrgId, long DeptId, long GroupId, long SalerId, DateTime SDate, DateTime EDate)
        {

            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetForecastBalanceInfo(ctx, OrgId, DeptId, GroupId, SalerId, SDate, EDate);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }

        }


        /// <summary>
        /// 是否是对应角色（用于权限判断）
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="quotationRoleId">角色Id</param>
        /// <returns></returns>
        public static DynamicObjectCollection GetWorkflowChartFlowWay(Context ctx, string billname, string billId)
        {

            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(ctx);
            try
            {
                return service.GetWorkflowChartFlowWay(ctx, billname, billId);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }

        }

        /// <summary>
        /// 获取单据的审批路径
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="info">单据模型</param>
        /// <returns>返回审批路径</returns>
        public static bool IsquotationRoleIdRole(Context context, int quotationRoleId)
        {

            IYDLCommService service = ServiceFactory.GetService<IYDLCommService>(context);
            try
            {
                return service.IsquotationRoleIdRole(context, quotationRoleId);
            }
            finally
            {
                ServiceFactory.CloseService(service);
            }

        }
    }
}

