using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using JN.K3.YDL.Core;
using System.Linq;
using System.Text;
using Kingdee.BOS.ServiceHelper;

namespace JN.K3.YDL.App.ServicePlugIn.GZL
{
    /// <summary>
    /// 报告审核插件
    /// </summary>
    [Description("报告审核插件")]
    public class VTR_ReportAudit: AbstractOperationServicePlugIn
    {
        //老板的ID号
        //private string _bossid = "131545";

        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {

             e.FieldKeys.Add("F_JNAssetCombo");
             e.FieldKeys.Add("FBillTypeID");
        }

        public override void BeginOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
           /* DynamicObject billTypePara = BusinessDataServiceHelper.LoadBillTypePara(this.Context, this.BusinessInfo.GetForm().BillTypePara, "VTR_ReportParam", true);
            bool docheck = Convert.ToBoolean(billTypePara["F_VTR_CheckBox"]);

            if (this.Context.UserId.ToString().Equals(_bossid) == false && docheck && this.Context.ClientType.ToString()!="Mobile")
           
            {
                this.ShowK3DisplayMessage();
               

            }*/

        }

        /// <summary>
        /// 审核操作完成，单据状态已经更改，但是还没有提交事务时，触发此事件：
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>
        /// 因为此事件触发时，还在事务保护中，因此适合进行数据同步；
        /// 审核后自动下推，如果下推失败，需要放弃审核，因此，放在此事件中处理（事务中）
        /// </remarks>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            /*
            int num = e.DataEntitys.Length;
           
            for (int i = 0; i < num; i++)
            {

                    DynamicObject BillType = e.DataEntitys[i]["FBillTypeID"] as DynamicObject;
                    string billtypename=Convert.ToString( BillType["Name"]);
                    if (billtypename == "资产采购报告单")
                    {
                        IOperationResult result = new OperationResult();
                        List<long> sourceBillIdsbase = new List<long>();
                        sourceBillIdsbase.Add(Convert.ToInt64(e.DataEntitys[i][0]));
                        result = this.DoPush(this.BusinessInfo.GetForm().Id, "PUR_Requisition", 0,
                            sourceBillIdsbase);
                    }
                    
            }*/
        }

        /// <summary>
        /// 利用K/3 Cloud标准的交互消息界面，显示消息：通常用于给用户选择是、否
        /// </summary>
        /// <remarks>
        /// 本案例，向用户提示负库存，如果用户选择是，则继续操作；否则中断操作
        /// </remarks>
        private void ShowK3DisplayMessage()
        {

            // 定义交互消息标识，以与其他交互消息区分开
            string spensorKey = "VTR_ReportAudit.ServicePlugIn.Operation.S160425ShowInteractionOpPlug.ShowK3DisplayMessage";

            // 判断用户是否已经确认过本交互信息
            if (this.Option.HasInteractionFlag(spensorKey))
            {
                // this.Option.HasInteractionFlag()在如下两种情况下，返回true:
                // 1. 用户已经确认过本交互信息，并选择了继续
                // 2. 外围代码，在调用本操作前，通过如下代码，强制要求不显示交互消息：
                // this.Option.SetIgnoreInteractionFlag(true);
                // 因此，如果 this.Option.HasInteractionFlag() == true, 表示需要忽略本交互
                return;
            }
            // 提示信息的列标题，以“~|~”分开两列
            string titleMsg = "本节点为最后节点，是否继续完成审批？";
            // 对应的提示信息格式，以"~|~"分开两列，以{n}进行占位
            string errMsg = "最后节点,是否继续完成审批？";
            // 构建消息模型K3DisplayerModel，在此对象中，添加消息内容
            K3DisplayerModel model = K3DisplayerModel.Create(Context, titleMsg);
            // 消息内容：可以添加多行
            string rowMsg = string.Format(errMsg,"本节点为最后节点，是否继续完成审批？");
            ((K3DisplayerModel)model).AddMessage(rowMsg);
            // 消息抬头
           
            model.Option.SetVariableValue(K3DisplayerModel.CST_FormTitle, "本节点为最后节点，是否继续完成审批？");
            // 是否继续按钮
            model.FieldAppearances[1].Width = new LocaleValue("300");
         
            
            model.OKButton.Visible = true;
            model.OKButton.Caption = new LocaleValue("继续", Context.UserLocale.LCID);
            model.CancelButton.Visible = true;
            model.CancelButton.Caption = new LocaleValue("取消", Context.UserLocale.LCID);
            // 创建一个交互提示错误对象KDInteractionException：
            // 通过throw new KDInteractionException()的方式，向操作调用者，输出交互信息
            KDInteractionException ie = new KDInteractionException(this.Option, spensorKey);
            // 提示信息显示界面
            ie.InteractionContext.InteractionFormId =Kingdee.BOS.Core.FormIdConst.BOS_K3Displayer;
            // 提示内容
            ie.InteractionContext.K3DisplayerModel = model;
            // 是否需要交互
            ie.InteractionContext.IsInteractive = true;
            // 抛出错误，终止流程
            throw ie;
        }


        /// <summary>
        /// 自动下推并保存、提交
        /// </summary>
        /// <param name="sourceFormId">源单FormId</param>
        /// <param name="targetFormId">目标单FormId</param>
        /// <param name="sourceBillIds">源单内码</param>
        private IOperationResult DoPush(string sourceFormId, string targetFormId, long targetOrgId, List<long> sourceBillIds)
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
            //long targetOrgId = 0;
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

            // 判断自动保存结果：只有操作成功，才会继续
            if (saveResult.SuccessDataEnity != null)
            {
                var submitRet = AppServiceContext.SubmitService.Submit(this.Context, targetBillMeta.BusinessInfo, saveResult.SuccessDataEnity.Select(o => o["Id"]).ToArray(), "Submit", saveOption);
                result.MergeResult(submitRet);
            }
            if (this.CheckOpResult(saveResult, saveOption))
            {
                return result;
            }
            return result;
        }

        /// 判断操作结果是否成功，如果不成功，则直接抛错中断进程
        /// </summary>
        /// <param name="opResult">操作结果</param>
        /// <param name="opOption">操作参数</param>
        /// <returns></returns>
        private bool CheckOpResult(IOperationResult opResult, OperateOption opOption)
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
