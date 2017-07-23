using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Rpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace JN.K3.YDL.Contracts
{
    /// <summary>
    /// 通用服务接口
    /// </summary>
    [ServiceContract]
    [RpcServiceError]
    public interface IYDLCommService
    {
        /// <summary>
        /// 获取付款和费用申请单所有单据类型说明
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="strFilter">过滤条件</param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetExpenseRequestOrderEditInfo(Context ctx, string strFilter);

        /// <summary>
        /// 获取订单类型说明
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fname">单据类型分类</param>
        /// <param name="fvalue">单据类型</param>
        /// <returns>返回描述</returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        string GetExpenseRequestOrderEditDescription(Context ctx, string fname, string fvalue);

        /// <summary>
        /// 获取物料对应批次号的单位酶活量
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materialId"></param>
        /// <param name="lotId"></param>
        /// <returns></returns> 
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        decimal MaterialUnitEnzymes(Context ctx, JNQTYRatePara para);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        void UpdateInspectData(Context ctx, List<string> entryPrimaryValues);

        /// <summary>
        /// 即时库存明细--增加单位酶活量，标吨
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        List<DynamicObject> GetInventoryAllData(Context ctx);

        /// <summary>
        /// 即时库存汇总数据查询--增加单位酶活量，标吨
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        List<DynamicObject> GetInvSumQueryAllData(Context ctx);

        /// <summary>
        ///查询价目表的数据
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="goodsid">物料id</param>
        /// <returns></returns>     
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetPriceFormsData(Context ctx, string goodsid);


        /// <summary>
        ///查询价目表的数据详细
        ///增加业务员、部门、销售组和普遍适应的价格查询----赵成杰20171226
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="goodsid">物料id</param>
        /// <param name="custid">客户id</param>
        /// <param name="saleId">销售员</param>
        /// <param name="currencyid">币种</param>
        /// <param name="auxpropid">辅助属性</param>
        /// <returns></returns>       
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetPriceFormsDataByCust(Context ctx, string goodsid, string custid, string saleId,string currencyid, string auxpropid);

        /// <summary>
        /// 配方单转库存检验单
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="FID">源单ID</param>
        /// <param name="FPKID">源单单据体ID</param>
        /// <param name="row">单据体行号</param>
        IOperationResult ConvertRule(Context ctx, int FID, int FPKID, int row);

        /// <summary>
        ///查询酶活维护信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="goodsid">物料id</param>
        /// <returns></returns>     
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetMATERIALID(Context ctx, Int32 materialId);

        /// <summary>
        /// 查询获取的即时库存
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="FID">id集合</param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetINVENTORY(Context ctx, string FID);

        /// <summary>
        /// 获取物料对应批次号的生产日期、有限期至
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="para"></param>        
        /// <returns></returns> 
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetLotExpiryDate(Context ctx, JNQTYRatePara para);

        /// <summary>
        /// 获取酶种bom单位酶活量
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="FID">bom清单ID</param>
        /// <param name="materilId">物料ID</param>
        /// <param name="oper">工序</param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetBom(Context ctx, int FID, int materilId, int oper);

        /// <summary>
        /// 获取酶种对应库存明细
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materilId"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetInvStock(Context ctx, int materilId, long orgId);

        /// <summary>
        /// 获取采购目录表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materilId"></param>
        /// <param name="supplierId"></param>
        /// <param name="applicationDate"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetPriceListId(Context ctx, long materilId, long supplierId, DateTime applicationDate);


        /// <summary>
        /// 获取采购目录表带包装规格
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materilId"></param>
        /// <param name="supplierId"></param>
        /// <param name="auxpropId"></param>
        /// <param name="applicationDate"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetAuxpropPriceListId(Context ctx, long materilId, string auxpropId, long supplierId, DateTime applicationDate);

        /// <summary>
        /// 获取价目表信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="materilId"></param>
        /// <param name="supplierId"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetPriceListInfo(Context ctx, long materilId, long supplierId = 0);


        /// <summary>
        /// 获取打开单据信息
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="info">单据模型</param>
        /// <param name="BillNo">单据编号</param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Kingdee.BOS.Core.Bill.BillShowParameter GetShowParameter(Kingdee.BOS.Context ctx, FormMetadata info, string BillNo);



        /// <summary>
        /// 更新凭证号到主表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="tableA">主表</param>
        /// <param name="tableB">凭证子表</param>
        /// <returns>更新行数</returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        int updatevoucherNo(Context ctx, string tableA, string tableB);


        /// <summary>
        /// 获取销售预测结余表信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="OrgId"></param>
        /// <param name="SDate"></param>
        /// <param name="EDate"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetForecastBalanceInfo(Context ctx, long OrgId, long DeptId, long GroupId, long SalerId, DateTime SDate, DateTime EDate);


        /// <summary>
        /// 获取单据的审批路径
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="info">单据模型</param>
        /// <returns>返回审批路径</returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObjectCollection GetWorkflowChartFlowWay(Context ctx, string billname, string billId);


        /// <summary>
        /// 银行系统对接交互返回报文
        /// </summary>
        ///  <param name="ctx">上下文</param>
        /// <param name="url">银行服务地址</param>
        /// <param name="xmlMsg">XML报文请求</param>
        /// <param name="timeout">请求时间</param>
        /// <returns>XML返回报文</returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        string BandPost(Context ctx, string url, string xmlMsg, int timeout);



        /// <summary>
        /// 是否是对应角色（用于权限判断）
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="quotationRoleId">角色Id</param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        bool IsquotationRoleIdRole(Context context, int quotationRoleId);

    }
}
