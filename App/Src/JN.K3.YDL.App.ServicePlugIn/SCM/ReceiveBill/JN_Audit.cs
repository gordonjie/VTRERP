using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS;
using JN.K3.YDL.Core;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.DynamicForm.Operation;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.ReceiveBill
{
    [Description("采购收料单审核服务插件")]
     
    public class JN_Audit : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>

        private string billtype = "";
        private static long OrgId = 0;
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBillTypeID");
            e.FieldKeys.Add("FJNUnitEnzymes");
            e.FieldKeys.Add("FExtAuxUnitQty");
            e.FieldKeys.Add("FDeliverEnzymes");
            e.FieldKeys.Add("FStockOrgId");
            
        }

       
        /// <summary>
        /// 调用操作事物后触发
        /// </summary>
        /// <param name="e"/>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            List<DynamicObject> data = e.DataEntitys.ToList();
            foreach (var item in data)
            {
                //DynamicObject entry = item["PUR_ReceiveEntry"] as DynamicObject;
                DynamicObjectCollection Receive = item["PUR_ReceiveEntry"] as DynamicObjectCollection;

                OrgId = Convert.ToInt32(item["StockOrgId_Id"]);
                //foreach (var entrys in Receive)
                //{
                //    entrys["FDeliverEnzymes"] = entrys["FJNUnitEnzymes"];
                //}
                AppServiceContext.SaveService.Save(this.Context, data.ToArray());
                DynamicObject billTypeObj = item["BillTypeId"] as DynamicObject;
                billtype = billTypeObj["Number"].ToString(); 
                bool BillType = (billTypeObj["Number"].ToString() == "SLD04_SYS" || billTypeObj["NumBer"].ToString() == "SLD05_SYS");//只有资产、费用收料单生成应付单
                if (!BillType) continue;
                IOperationResult salseresult = ToTransferSalseBill(this.Context, item ,billtype);
                OperateResult result2;
                if (!salseresult.IsSuccess)
                {
                    string strMessage = "生成应付单失败。失败原因：";
                    foreach (Kingdee.BOS.Core.Validation.ValidationErrorInfo vr in salseresult.ValidationErrors)
                    {
                        strMessage += vr.Message + " ";
                    }
                    result2 = new OperateResult
                    {
                        SuccessStatus = false,
                        Message = strMessage,
                        MessageType = MessageType.Normal,
                        Name = "生成应付单失败",
                        PKValue = item
                    };
                    base.OperationResult.OperateResult.Add(result2);
                }
                else
                {
                    if (salseresult.SuccessDataEnity == null)
                    {
                        string strMessage = "生成应付单失败。失败原因：单据转换配置错误！";                        
                        result2 = new OperateResult
                        {
                            SuccessStatus = false,
                            Message = strMessage,
                            MessageType = MessageType.Normal,
                            Name = "生成应付单失败",
                            PKValue = item
                        };
                        base.OperationResult.OperateResult.Add(result2);
                        return;
                    }
                    foreach (var dyhead in salseresult.SuccessDataEnity)
                    {
                        result2 = new OperateResult
                        {
                            SuccessStatus = true,
                            Message = "生成应付单成功：" + dyhead["BillNo"],
                            MessageType = MessageType.Normal,
                            Name = "生成应付单成功",
                            PKValue = item
                        };
                        base.OperationResult.OperateResult.Add(result2);
                    }
                }
            }
        }

        /// <summary>
        /// 生成应付单
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="souObj"></param>
        /// <returns></returns>
        private static IOperationResult ToTransferSalseBill(Context ctx, DynamicObject souObj,string billtype)
        {
            IOperationResult convertResult = new OperationResult();
            DynamicObjectCollection dyCollection = souObj["PUR_ReceiveEntry"] as DynamicObjectCollection;

          

            List<ListSelectedRow> ListSalReSelect = new List<ListSelectedRow>();
            dyCollection.ToList().ForEach(entiyItem =>
            {
                ListSelectedRow convertItem = new ListSelectedRow(
                       Convert.ToString(souObj["Id"]),
                       Convert.ToString(entiyItem["Id"]),
                       Convert.ToInt32(entiyItem["Seq"]),
                       "PUR_ReceiveBill");
                ListSalReSelect.Add(convertItem); 
            });

            if (ListSalReSelect.Count <= 0)
            {
                return convertResult;
            }
            BillConvertOption convertOption = new BillConvertOption();
            convertOption.sourceFormId = "PUR_ReceiveBill";
            convertOption.targetFormId = "AP_Payable";
            convertOption.ConvertRuleKey = "AP_ReceiveToPayableMap";
            convertOption.Option = OperateOption.Create();
            convertOption.Option.SetIgnoreWarning(false);
            convertOption.BizSelectRows = ListSalReSelect.ToArray();
            convertOption.IsDraft = false;
            convertOption.IsSave = true;
            convertOption.IsAudit = true;
            convertResult = ConvertBills(ctx, convertOption,billtype);
            return convertResult;
        }

        /// <summary>
        /// 后台调用单据转换生成目标单
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static IOperationResult ConvertBills(Context ctx, BillConvertOption option,string billtype)
        {
            //return AppServiceContext.ConvertBills(ctx, option);
            //判断单据类型资产接收单转化为资产应付；费用物料转为办公应付
            string billtypeid = "";
            if (billtype == "SLD04_SYS")
            {
                 billtypeid = "56e224fd183867";
                }
            if (billtype == "SLD05_SYS")
            {
                 billtypeid = "777f5fd25084498a9e77e1ef2a72e645";
            }
            IOperationResult result = new OperationResult();
            List<DynamicObject> list = new List<DynamicObject>();
            ConvertRuleElement rule = AppServiceContext.ConvertService.GetConvertRules(ctx, option.sourceFormId, option.targetFormId)
               .FirstOrDefault(w => w.Id == option.ConvertRuleKey);
            FormMetadata metaData = (FormMetadata)AppServiceContext.MetadataService.Load(ctx, option.targetFormId, true);
            if ((rule != null) && option.BizSelectRows != null && option.BizSelectRows.Count() > 0)
            {
                
                    PushArgs serviceArgs = new PushArgs(rule, option.BizSelectRows)
                    {

                        TargetBillTypeId = billtypeid,
                    };
                
                serviceArgs.CustomParams.Add("CustomConvertOption", option);
                serviceArgs.CustomParams.Add("CustomerTransParams", option.customParams);
                OperateOption operateOption = OperateOption.Create();
                operateOption.SetVariableValue("ValidatePermission", true);
                ConvertOperationResult convertOperationResult = AppServiceContext.ConvertService.Push(ctx, serviceArgs, operateOption);
                if (!convertOperationResult.IsSuccess)
                {
                    result = convertOperationResult as IOperationResult;
                    return result;
                }
                DynamicObject[] collection = convertOperationResult.TargetDataEntities
                    .Select(s => s.DataEntity).ToArray();
                list.AddRange(collection);
            }
            if (list.Count > 0)
            {
                AppServiceContext.DBService.LoadReferenceObject(ctx, list.ToArray(), metaData.BusinessInfo.GetDynamicObjectType(), false);
            }
            if (option.IsDraft && list.Count > 0)
            {
                result = AppServiceContext.DraftService.Draft(ctx, metaData.BusinessInfo, list.ToArray());
            }
            if (!result.IsSuccess)
            {
                return result;
            }
            if (option.IsSave && !option.IsDraft && list.Count > 0)
            {
                OperateOption operateOption = OperateOption.Create();
                operateOption.SetVariableValue("ValidatePermission", true);
                operateOption.SetIgnoreWarning(true);
                result = AppServiceContext.SaveService.Save(ctx, metaData.BusinessInfo, list.ToArray(), operateOption);

                //result = AppServiceContext.SaveService.Save(ctx, metaData.BusinessInfo, list.ToArray());
            }
            if (!result.IsSuccess)
            {
                return result;
            }
            if (option.IsSubmit && list.Count > 0)
            {
                result = AppServiceContext.SubmitService.Submit(ctx, metaData.BusinessInfo,
                    list.Select(item => ((Object)(Convert.ToInt64(item["Id"])))).ToArray(), "Submit");
            }
            if (!result.IsSuccess)
            {
                return result;
            }

            bool systemParamter = Convert.ToBoolean(Kingdee.BOS.ServiceHelper.SystemParameterServiceHelper.GetParamter(ctx, OrgId, 0, "AP_SystemParameter", "F_VTRAutoReceiveBillPayable"));//获取系统参数生成应付单是否创建状态

            if (option.IsAudit && list.Count > 0 && systemParamter==false)
            {
                result = AppServiceContext.SubmitService.Submit(ctx, metaData.BusinessInfo,
                    list.Select(item => ((Object)(Convert.ToInt64(item["Id"])))).ToArray(), "Submit");
                if (!result.IsSuccess)
                {
                    return result;
                }
                List<KeyValuePair<object, object>> keyValuePairs = new List<KeyValuePair<object, object>>();
                list.ForEach(item =>
                {
                    keyValuePairs.Add(new KeyValuePair<object, object>(item.GetPrimaryKeyValue(), item));
                }
                );
                List<object> auditObjs = new List<object>();
                auditObjs.Add("1");
                auditObjs.Add("");
                //Kingdee.BOS.Util.OperateOptionUtils oou = null;
                OperateOption ooption = OperateOption.Create();
                ooption.SetIgnoreWarning(false);
                ooption.SetIgnoreInteractionFlag(true);
                ooption.SetIsThrowValidationInfo(false);
                result = AppServiceContext.SetStatusService.SetBillStatus(ctx, metaData.BusinessInfo,
                  keyValuePairs, auditObjs, "Audit", ooption);

                option.BillStatusOptionResult = ooption;
                if (!result.IsSuccess)
                {
                    return result;
                }
            }

            return result;
        }
    }
}
