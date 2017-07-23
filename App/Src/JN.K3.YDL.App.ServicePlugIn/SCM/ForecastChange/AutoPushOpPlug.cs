using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Orm.DataEntity;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.ForecastChange
{
    [Description("销售预测变更单审核时，自动下推")]
    public class AutoPushOpPlug : AbstractOperationServicePlugIn
    {
        public override void EndOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (e.DataEntitys == null || e.DataEntitys.Count() <= 0)
            {
                return;
            }

            List<long> lstFids = new List<long>();

            foreach (DynamicObject data in e.DataEntitys)
            {
                lstFids.Add(Convert.ToInt64(data["ID"]));
            }

            if (lstFids.Count() <= 0)
            {
                return;
            }

            ////审核自动生成计划订单
            ////销售预测单变更单-计划订单
            //IOperationResult result = new OperationResult();
            //result = this.DoPushNotAudit("JN_YDL_SAL_ForecastChange", "PLN_PLANORDER",
            //    lstFids);
        }

        /// <summary>
        /// 自动下推并保存
        /// </summary>
        /// <param name="sourceFormId">源单FormId</param>
        /// <param name="targetFormId">目标单FormId</param>
        /// <param name="sourceBillIds">源单内码</param>
        private IOperationResult DoPushNotAudit(string sourceFormId, string targetFormId, List<long> sourceBillIds)
        {
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
            foreach (var rule1 in rules)
            {
                string ruleid = rule1.Id.ToString();

                if (ruleid == "JN_ForecastChangeToPlanOrder")
                {
                    rule = rule1;
                }
            }
            //var rule = rules.
            // 如果无默认规则，则取第一个
            if (rule == null)
            {
                rule = rules[0];
            }
            // 开始构建下推参数：
            // 待下推的源单数据行
            List<ListSelectedRow> srcSelectedRows = new List<ListSelectedRow>();
            foreach (var billId in sourceBillIds)
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
            long targetOrgId = 0;
            // 自定义参数字典：把一些自定义参数，传递到转换插件中；转换插件再根据这些参数，进行特定处理
            Dictionary<string, object> custParams = new Dictionary<string, object>();
            // 组装下推参数对象
            PushArgs pushArgs = new PushArgs(rule, srcSelectedRows.ToArray())
            {
                TargetBillTypeId = targetBillTypeId,
                TargetOrgId = targetOrgId,
                CustomParams = custParams
            };
            // 调用下推服务，生成下游单据数据包
            ConvertOperationResult operationResult = convertService.Push(this.Context, pushArgs, Kingdee.BOS.Orm.OperateOption.Create());
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
            Kingdee.BOS.Orm.OperateOption saveOption = Kingdee.BOS.Orm.OperateOption.Create();
            // 忽略全部需要交互性质的提示，直接保存；
            saveOption.SetIgnoreWarning(true);              // 忽略交互提示
            saveOption.SetInteractionFlag(this.Option.GetInteractionFlag());        // 如果有交互，传入用户选择的交互结果
            // using Kingdee.BOS.Core.Interaction;
            saveOption.SetIgnoreInteractionFlag(this.Option.GetIgnoreInteractionFlag());
            /*

            IDraftService draftSev = Kingdee.BOS.Contracts.ServiceFactory.GetDraftService(this.Context);
            var draftRet = draftSev.Draft(this.Context, targetBillMeta.BusinessInfo, targetBillObjs, saveOption, "Draft");*/

            // 调用保存服务，自动保存
            ISaveService saveService = Kingdee.BOS.App.ServiceHelper.GetService<ISaveService>();
            var saveResult = saveService.Save(this.Context, targetBillMeta.BusinessInfo, targetBillObjs, saveOption, "Save");
            /*
            // 判断自动保存结果：只有操作成功，才会继续
            if (saveResult.SuccessDataEnity != null)
            {
                var submitRet = AppServiceContext.SubmitService.Submit(this.Context, targetBillMeta.BusinessInfo, saveResult.SuccessDataEnity.Select(o => o["Id"]).ToArray(), "Submit", saveOption);
                result.MergeResult(submitRet);
                if (submitRet.SuccessDataEnity != null)
                {
                    var auditResult = AppServiceContext.SetStatusService.SetBillStatus(this.Context, targetBillMeta.BusinessInfo,
                        submitRet.SuccessDataEnity.Select(o => new KeyValuePair<object, object>(o["Id"], 0)).ToList(),
                        new List<object> { "1", "" },
                        "Audit", saveOption);

                    result.MergeResult(auditResult);
                }
            }*/
            if (this.CheckOpResult(saveResult, saveOption))
            {
                return result;
            }
            return result;
        }


        /// <summary>
        /// 判断操作结果是否成功，如果不成功，则直接抛错中断进程
        /// </summary>
        /// <param name="opResult">操作结果</param>
        /// <param name="opOption">操作参数</param>
        /// <returns></returns>
        private bool CheckOpResult(IOperationResult opResult, Kingdee.BOS.Orm.OperateOption opOption)
        {
            bool isSuccess = false;
            if (opResult.IsSuccess == true)
            {
                // 操作成功
                isSuccess = true;
            }
            else
            {
                if (opResult.InteractionContext != null
                    && opResult.InteractionContext.Option.GetInteractionFlag().Count > 0)
                {// 有交互性提示
                    // 传出交互提示完整信息对象
                    this.OperationResult.InteractionContext = opResult.InteractionContext;
                    // 传出本次交互的标识，
                    // 用户在确认继续后，会重新进入操作；
                    // 将以此标识取本交互是否已经确认过，避免重复交互
                    this.OperationResult.Sponsor = opResult.Sponsor;
                    // 抛出交互错误，把交互信息传递给前端
                    new KDInteractionException(opOption, opResult.Sponsor);
                }
                else
                {
                    // 操作失败，拼接失败原因，然后抛出中断
                    opResult.MergeValidateErrors();
                    if (opResult.OperateResult == null)
                    {// 未知原因导致提交失败
                        throw new KDBusinessException("", "未知原因导致自动提交、审核失败！");
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("自动操作失败：");
                        foreach (var operateResult in opResult.OperateResult)
                        {
                            sb.AppendLine(operateResult.Message);
                        }
                        throw new KDBusinessException("", sb.ToString());
                    }
                }
            }
            return isSuccess;
        }
    }
}
