using JN.K3.YDL.App.ServicePlugIn.Common;
using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Util;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;



namespace JN.K3.YDL.App.ServicePlugIn.SCM.ForecastChange
{
    /// <summary>
    /// 销售预测单变更单审核插件
    /// </summary>
    [Description("销售预测单变更单审核插件")]
    public class JN_Audit : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FBillNo");
            e.FieldKeys.Add("FJNMaterialId");
            e.FieldKeys.Add("FDirection");
        }

        /// <summary>
        /// 增加操作校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            JNAuditValidator aduitValidator = new JNAuditValidator();
            aduitValidator.EntityKey = "FBillHead";
            e.Validators.Add(aduitValidator);
        }

        public override void EndOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);

            if (e.DataEntitys == null || e.DataEntitys.Count() <= 0)
            {
                return;
            }

            List<long> lstFids = new List<long>();

            List<ListSelectedRow> lstSelect = new List<ListSelectedRow>();
            List<long> AddSelect = new List<long>();//下推增加预测选择
           
            foreach (DynamicObject data in e.DataEntitys)
            {
                lstFids.Add(Convert.ToInt64(data["ID"]));
                DynamicObjectCollection dycEntitys = data["FEntity"] as DynamicObjectCollection;
                string FDirection=Convert.ToString(data["FDirection"]);
                if (dycEntitys == null || dycEntitys.Count() <= 0)
                {
                    continue;
                }
                foreach (var dycEntity in dycEntitys)
                {
                    ListSelectedRow convertItem = new ListSelectedRow(
                     Convert.ToString(data["ID"]),
                     Convert.ToString(dycEntity["ID"]),
                     Convert.ToInt32(dycEntity["ID"]),
                     "BillHead");
                    convertItem.EntryEntityKey = "FEntity";
                    lstSelect.Add(convertItem);

                    if (FDirection == "A")
                    {
                        AddSelect.Add(Convert.ToInt32(convertItem.PrimaryKeyValue));
                    }

                    //   更新销售结余后台表 12月20日赵成杰             
                    string entityid = Convert.ToString(dycEntity["ID"]);
                    string sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                 set (FQTY)=(select case when b.FDirection='A' then a.FQTY+c.FJNBaseUnitQty
                                       else a.FQTY-c.FJNBaseUnitQty end
                    from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_ForecastChange b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId  
                inner join JN_T_SAL_ForecastChangeEntry c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID  and c.FEntryID={0}
                and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FJNBASEUNITID 
                where a.FID=t0.FID )", entityid);
                    DBUtils.Execute(this.Context, sql);
                }
                

            }

            if (lstFids.Count() <= 0)
            {
                return;
            }

            SqlParam param = new SqlParam("@FID", KDDbType.udt_inttable, lstFids.ToArray());

            //更新销售结余后台表
            DynamicObjectCollection dycInsertForecastBack = UpdateForecastBack(param);

            //插入销售结余日志表
            DynamicObjectCollection dycInsertForecastLog = UpdateForecastLog(param);

            //调用插入方法
            JNCommonServices.UpdateForecastBackAndLog(this.Context, dycInsertForecastBack, dycInsertForecastLog);

            if (AddSelect == null || AddSelect.Count <= 0)
            {
                return;
            }

            
            //审核自动生成预测单
            //销售预测单变更单-预测单
            List<IOperationResult> results = new List<IOperationResult>();
            IOperationResult result = new OperationResult();
            result = this.DoPushNotAudit("JN_YDL_SAL_ForecastChange", "PLN_FORECAST",
                AddSelect);
            results.Add(result);
            InItOperateResult(results, "生成预测单成功", "生成预测单失败");

        }


        public override void AfterExecuteOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            ////审核自动生成计划订单
            ////销售预测单变更单-计划订单
            //if (e.DataEntitys == null || e.DataEntitys.Count() <= 0)
            //{
            //    return;
            //}

            //List<ListSelectedRow> lstSelect = new List<ListSelectedRow>();

            //foreach (DynamicObject item in e.DataEntitys)
            //{
            //    DynamicObjectCollection dycEntitys = item["FEntity"] as DynamicObjectCollection;
            //    if (dycEntitys == null || dycEntitys.Count() <= 0)
            //    {
            //        continue;
            //    }
            //    foreach (var dycEntity in dycEntitys)
            //    {
            //        ListSelectedRow convertItem = new ListSelectedRow(
            //         Convert.ToString(item["ID"]),
            //         Convert.ToString(dycEntity["ID"]),
            //         Convert.ToInt32(dycEntity["ID"]),
            //         "BillHead");
            //        convertItem.EntryEntityKey = "FEntity";
            //        lstSelect.Add(convertItem);
            //    }
            //}

            //if (lstSelect == null || lstSelect.Count <= 0)
            //{
            //    return;
            //}

            ////审核自动生成计划订单
            ////销售预测单变更单-计划订单
            //List<IOperationResult> results = new List<IOperationResult>();
            //IOperationResult result = new OperationResult();
            //result = this.DoPushNotAudit("JN_YDL_SAL_ForecastChange", "PLN_PLANORDER",
            //    lstSelect);
            //results.Add(result);
            //InItOperateResult(results, "生成计划订单成功", "生成计划订单失败");
        }


        //更新销售结余后台表
        private DynamicObjectCollection UpdateForecastBack(SqlParam param)
        {
            string sql = string.Empty;
            /*存在出现同一张单出现表体出现相同物料时导致重复创建结余后台表，停用
            sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                    set (FQTY)=(select case when b.FDirection='A' then a.FQTY+c.FJNBaseUnitQty
                                       else a.FQTY-c.FJNBaseUnitQty end
                    from JN_T_SAL_ForecastBack a
                    inner join JN_T_SAL_ForecastChange b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                    and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
                    inner join JN_T_SAL_ForecastChangeEntry c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID 
                    and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FJNBaseUnitID
                    inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                    where a.FID=t0.FID ) ");

            DBUtils.Execute(this.Context, sql, param);*/

            sql = string.Format(@"select t1.FJNSALEORGID as FSALEORGID,t1.FJNSALERID as FSALERID,t1.FJNSaleDeptId as FSaleDeptId
                        ,newid() as FBILLNO,t2.FJNMATERIALID as FMATERIALID
                        ,t2.FJNBaseUnitQty as FQTY,t2.FJNBaseUnitID as FUNITID,t2.FJNAUXPROP as FAUXPROPID,getdate() as FDATE
                        from JN_T_SAL_ForecastChange t1
                        inner join TABLE(fn_StrSplit(@FID,',',1)) tb on t1.Fid=tb.Fid
                        inner join JN_T_SAL_ForecastChangeEntry t2 on t1.FID=t2.FID
                        where not exists(select 1  from JN_T_SAL_ForecastBack tm where tm.FSALEORGID=t1.FJNSALEORGID 
                                        and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                                        and tm.FAUXPROPID=t2.FJNAUXPROP and tm.FSaleDeptId=t1.FJNSaleDeptId 
                                        and tm.FUnitID=t2.FJNBaseUnitID )
	                    and  t1.FDirection='A' 
	                    ");

            return DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });

        }

        //插入销售结余日志表
        private DynamicObjectCollection UpdateForecastLog(SqlParam param)
        {
            string sql = string.Empty;

            sql = string.Format(@"select newid() as FBILLNO,t2.FJNAUXPROP as FAUXPROPID,tm.FUNITID,t2.FJNBaseUnitQty as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FJNSALERID as FSALERID,t1.FJNSALEORGID as FSALEORGID,t1.FJNSaleDeptId as FSaleDeptId
                    ,t2.FJNMATERIALID as FMATERIALID,tm.FID as FFORECASTID,'E' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,(case when t1.FDirection='A' then tm.FQTY-t2.FJNBaseUnitQty 
                                                          else tm.FQTY+t2.FJNBaseUnitQty end )as FBEFOREQTY,tm.FQTY as FAFTERQTY
                    ,(case when t1.FDirection='A' then 'A' else 'B' end) as FDirection 
                    from JN_T_SAL_ForecastChange t1
                    inner join TABLE(fn_StrSplit(@FID,',',1)) tb on t1.Fid=tb.Fid
                    inner join JN_T_SAL_ForecastChangeEntry t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FJNSALEORGID 
                    and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                    and tm.FUNITID=t2.FJNBaseUnitID and tm.FAUXPROPID=t2.FJNAUXPROP
                    and tm.FSaleDeptId=t1.FJNSaleDeptId 
                    ");

            return DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });
        }


        private void InItOperateResult(List<IOperationResult> results, string successError, string fatalError)
        {
            OperateResult result2;
            foreach (IOperationResult result in results)
            {
                if (!result.IsSuccess)
                {
                    string strMessage = fatalError + "失败原因：";

                    if (result.ValidationErrors.Count == 0)
                    {
                        if (result.OperateResult != null && result.OperateResult.Count > 0)
                        {
                            OperateResultCollection operateResultCollection = result.OperateResult;
                            strMessage = operateResultCollection.Aggregate(strMessage, (current, operateResult) => current + (operateResult.Message + " "));
                        }
                        else
                        {
                            IInteractionResult interresult = result as IInteractionResult;
                            if (interresult.InteractionContext != null)
                                if (interresult.InteractionContext.SimpleMessage != null)
                                    strMessage = strMessage + (interresult.InteractionContext.SimpleMessage + " ");

                        }
                    }
                    else
                    {
                        strMessage = result.ValidationErrors.Aggregate(strMessage, (current, vr) => current + (vr.Message + " "));
                    }
                    throw new Exception(strMessage);
                }
                if (result.SuccessDataEnity == null)
                    return;
                foreach (var dyhead in result.SuccessDataEnity)
                {
                    result2 = new OperateResult
                    {
                        SuccessStatus = true,
                        Message = successError + "：" + dyhead["BillNo"],
                        MessageType = MessageType.Normal,
                        Name = successError,
                        PKValue = dyhead["FFormId"]
                    };
                    base.OperationResult.OperateResult.Add(result2);
                }
            }
            base.OperationResult.IsShowMessage = true;
        }



        /// <summary>
        /// 自动下推并保存
        /// </summary>
        /// <param name="sourceFormId">源单FormId</param>
        /// <param name="targetFormId">目标单FormId</param>
        /// <param name="sourceBillIds">源单内码</param>
        private IOperationResult DoPushNotAudit(string sourceFormId, string targetFormId, List<long> lstSelect)
        {

            /*IOperationResult convertResult = new OperationResult();
            
            BillConvertOption convertOption = new BillConvertOption();
            convertOption.sourceFormId = sourceFormId;
            convertOption.targetFormId = targetFormId;
            convertOption.ConvertRuleKey = "VTR_ForecastChangeToPrediction";
            convertOption.Option = OperateOption.Create();
            convertOption.BizSelectRows = lstSelect.ToArray();
            convertOption.IsDraft = true;
            convertOption.IsSave = true;
            convertOption.IsAudit = false;

            convertResult = AppServiceContext.ConvertBills(this.Context, convertOption);
            return convertResult;*/
            IOperationResult result = new OperationResult();
            result.IsSuccess = false;
            // 获取源单与目标单的转换规则
            IConvertService convertService = Kingdee.BOS.App.ServiceHelper.GetService<IConvertService>();
            var rules = convertService.GetConvertRules(this.Context, sourceFormId, targetFormId);
            if (rules == null || rules.Count == 0)
            {
                throw new KDBusinessException("", string.Format("未找到{0}到{1}之间，启用的转换规则，无法自动下推！", sourceFormId, targetFormId));
            }
            // 取勾选了默认选项的规则
            var rule = rules.FirstOrDefault(t => t.IsDefault);
            //var rule = rules.
            // 如果无默认规则，则取第一个
            if (rule == null)
            {
                rule = rules[0];
            }
            // 开始构建下推参数：
            // 待下推的源单数据行
            List<ListSelectedRow> srcSelectedRows = new List<ListSelectedRow>();
            foreach (var billId in lstSelect)
            {// 把待下推的源单内码，逐个创建ListSelectedRow对象，添加到集合中
                srcSelectedRows.Add(new ListSelectedRow(billId.ToString(), string.Empty, 0, sourceFormId));
                // 特别说明：上述代码，是整单下推；
                // 如果需要指定待下推的单据体行，请参照下句代码，在ListSelectedRow中，指定EntryEntityKey以及EntryId
                //srcSelectedRows.Add(new ListSelectedRow(billId.ToString(), entityId, 0, sourceFormId) { EntryEntityKey = "FEntity" });
            }

            // 指定目标单单据类型:情况比较复杂，没有合适的案例做参照，示例代码暂略，直接留空，会下推到默认的单据类型
            string targetBillTypeId = string.Empty;
            // 指定目标单据主业务组织：情况更加复杂，需要涉及到业务委托关系，缺少合适案例，示例代码暂略
            // 建议在转换规则中，配置好主业务组织字段的映射关系：运行时，由系统根据映射关系，自动从上游单据取主业务组织，避免由插件指定
            //long targetOrgId = 0;
            // 自定义参数字典：把一些自定义参数，传递到转换插件中；转换插件再根据这些参数，进行特定处理
            Dictionary<string, object> custParams = new Dictionary<string, object>();
            // 组装下推参数对象
            PushArgs pushArgs = new PushArgs(rule, srcSelectedRows.ToArray())
            {
                TargetBillTypeId = targetBillTypeId,
                //TargetOrgId = targetOrgId,
                CustomParams = custParams
            };
            // 调用下推服务，生成下游单据数据包
            ConvertOperationResult operationResult = convertService.Push(this.Context, pushArgs, OperateOption.Create());
            // 开始处理下推结果:
            // 获取下推生成的下游单据数据包
            DynamicObject[] targetBillObjs = (from p in operationResult.TargetDataEntities select p.DataEntity).ToArray();
            if (targetBillObjs.Length == 0)
            {
                // 未下推成功目标单，抛出错误，中断审核
                throw new KDBusinessException("", string.Format("由{0}自动下推{1}，没有成功生成数据包，自动下推失败！", sourceFormId, targetFormId));
            }
            // 对下游单据数据包，进行适当的修订，以避免关键字段为空，自动保存失败
            // 示例代码略
            // 读取目标单据元数据
            IMetaDataService metaService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();
            var targetBillMeta = metaService.Load(this.Context, targetFormId) as FormMetadata;
            // 构建保存操作参数：设置操作选项值，忽略交互提示
            OperateOption saveOption = OperateOption.Create();
            // 忽略全部需要交互性质的提示，直接保存；
            saveOption.SetIgnoreWarning(true);              // 忽略交互提示
            saveOption.SetInteractionFlag(this.Option.GetInteractionFlag());        // 如果有交互，传入用户选择的交互结果
            // using Kingdee.BOS.Core.Interaction;
            saveOption.SetIgnoreInteractionFlag(this.Option.GetIgnoreInteractionFlag());
            // 调用保存服务，自动保存
            ISaveService saveService = Kingdee.BOS.App.ServiceHelper.GetService<ISaveService>();
            var saveResult = saveService.Save(this.Context, targetBillMeta.BusinessInfo, targetBillObjs, saveOption, "Save");


            return saveResult;
            
        }




    }
}
