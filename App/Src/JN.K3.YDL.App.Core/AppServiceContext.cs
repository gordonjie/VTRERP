using Kingdee.BOS;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Contracts.StateTracker;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Orm.DataEntity;
using JN.K3.YDL.Core;


namespace JN.K3.YDL.Core
{
    public class AppServiceContext
    {  

        public static T GetService<T>()
        { 
            if (Kingdee.BOS.Contracts.ServiceFactory._mapServer == null)
            {
                Kingdee.BOS.Contracts.ServiceFactory.RegisterService();
            }

            if(JN.BOS.Contracts.JNServiceFactory._mapServer==null)
            {
                JN.BOS.Contracts.JNServiceFactory.RegisterService();
            }
            return Kingdee.BOS.App.ServiceHelper.GetService<T>();
        }


        public static IConvertService ConvertService
        {
            get
            {
                return GetService<IConvertService>();
            }
        }

        public static IDeleteService DeleteService
        {
            get
            {
                return GetService<IDeleteService>();
            }
        }

        public static ILogService LogService
        {
            get
            {
                return GetService<ILogService>();
            }
        }

        public static IMetaDataService MetadataService
        {
            get
            {
                return GetService<IMetaDataService>();
            }
        }

        public static INetworkCtrlService NetworkCtrlService
        {
            get
            {
                return GetService<INetworkCtrlService>();
            }
        }

        public static IOrganizationService OrganizationService
        {
            get
            {
                return GetService<IOrganizationService>();
            }
        }

        public static IPermissionService PermissionService
        {
            get
            {
                return GetService<IPermissionService>();
            }
        }

        public static IQueryService QueryService
        {
            get
            {
                return GetService<IQueryService>();
            }
        }

        public static ISaveService SaveService
        {
            get
            {
                return GetService<ISaveService>();
            }
        }

        public static ISetStatusService SetStatusService
        {
            get
            {
                return GetService<ISetStatusService>();
            }
        }

        public static IStateTrackerApplyService StateTrackerApplyService
        {
            get
            {
                return GetService<IStateTrackerApplyService>();
            }
        }

        public static ISubmitService SubmitService
        {
            get
            {
                return GetService<ISubmitService>();
            }
        }

        public static IDraftService DraftService
        {
            get
            {
                return GetService<IDraftService>();
            }
        }

        public static ISystemParameterService SystemParameterService
        {
            get
            {
                return GetService<ISystemParameterService>();
            }
        }

        public static ITimeService TimeService
        {
            get
            {
                return GetService<ITimeService>();
            }
        }

        public static IViewService ViewService
        {
            get
            {
                return GetService<IViewService>();
            }
        }


        public static IDBService DBService
        {
            get
            {
                return GetService<IDBService>();
            }
        }












        /// <summary>
        /// 后台调用单据转换生成目标单
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static IOperationResult ConvertBills(Context ctx, BillConvertOption option)
        {
            IOperationResult result = new OperationResult();
            List<DynamicObject> list = new List<DynamicObject>();
            ConvertRuleElement rule = AppServiceContext.ConvertService.GetConvertRules(ctx, option.sourceFormId, option.targetFormId)
               .FirstOrDefault(w => w.Id == option.ConvertRuleKey);
            FormMetadata metaData = (FormMetadata)AppServiceContext.MetadataService.Load(ctx, option.targetFormId, true);
            if ((rule != null) && option.BizSelectRows != null && option.BizSelectRows.Count() > 0)
            {
                PushArgs serviceArgs = new PushArgs(rule, option.BizSelectRows);
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
            if (option.IsAudit && list.Count > 0)
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
