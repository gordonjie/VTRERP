using JN.BOS.Contracts;
using JN.K3.YDL.Contracts.SCM;
using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Core.DefaultValueService;
using Kingdee.BOS.App.Core.PlugInProxy;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.K3.MFG.Common.BusinessEntity.BD;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.SaleQuote
{
    /// <summary>
    /// 销售报价单失效操作插件
    /// </summary>
    [Description("销售报价单失效操作插件")]
    public class JN_Invalid : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 定义加载的属性
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBillNo");
            e.FieldKeys.Add("FBillTypeID");

            e.FieldKeys.Add("F_JN_ProductName");
            e.FieldKeys.Add("F_JN_MtrlGroupId");
            e.FieldKeys.Add("FMaterialId");
            e.FieldKeys.Add("FBomId");
        }

        /// <summary>
        /// 操作执行后逻辑处理
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.EndOperationTransactionArgs e)
        {
            var billGroups = e.DataEntitys.GroupBy(o => Convert.ToString(o["BillTypeId_Id"]));
            List<long> lstNewMtrlId = new List<long>();
            List<long> lstNewBomId = new List<long>();
            foreach (var billGroup in billGroups)
            {
                var billTypeParaObj = AppServiceContext.GetService<ISysProfileService>().LoadBillTypeParameter(this.Context, this.BusinessInfo.GetForm().Id, billGroup.Key);
                if (billTypeParaObj == null) continue;

                bool bSupportNoMtrlQuote = Convert.ToBoolean(billTypeParaObj["F_JN_NoMtrlIdQuotation"]);

                if (bSupportNoMtrlQuote)
                {
                    ExtendedDataEntitySet dataEntitySet = new ExtendedDataEntitySet();
                    dataEntitySet.Parse(billGroup.ToArray(), this.BusinessInfo);

                    var quoteEntryRows = dataEntitySet.FindByEntityKey("FQUOTATIONENTRY")
                        .Where(o => !o["F_JN_ProductName"].IsEmptyPrimaryKey())
                        .ToArray();
                    if (quoteEntryRows.Length > 0)
                    {
                        var number = quoteEntryRows.Select(o => o["MaterialId"]).ToArray();
                        DynamicObject obj = number[0] as DynamicObject;
                        var num = Convert.ToString(obj["Number"]);
                        string[] Sarray = num.Split(new char[1]{'.'});
                        if (Sarray[0] != "99") return;
                    }                    
                    if (quoteEntryRows.Any() == false) continue;
                    lstNewMtrlId.AddRange(quoteEntryRows.Select(o => (long)o["MaterialId_Id"])
                        .Distinct()
                        .Where(o => o > 0));
                    lstNewBomId.AddRange(quoteEntryRows.Select(o => (long)o["BomId_Id"])
                        .Distinct()
                        .Where(o => o > 0));
                }
            }
            if (lstNewMtrlId.Count > 0)
            {
                //禁用关联的物料及物料清单
                OperateOption mtrlOption = OperateOption.Create();
                mtrlOption.SetVariableValue("IsList", true);
                FormMetadata mtrlMetadata = AppServiceContext.MetadataService.Load(this.Context, "BD_MATERIAL") as FormMetadata;
                var mtrlOpRet = AppServiceContext.SetStatusService.SetBillStatus(this.Context, mtrlMetadata.BusinessInfo,
                    lstNewMtrlId.Select(o => new KeyValuePair<object, object>(o, null)).ToList(),
                    null, "Forbid", mtrlOption);
                this.OperationResult.MergeResult(mtrlOpRet);
            }
            if (lstNewBomId.Count > 0)
            {
                OperateOption bomOption = OperateOption.Create();
                bomOption.SetVariableValue("IsList", true);
                FormMetadata bomMetadata = AppServiceContext.MetadataService.Load(this.Context, "ENG_BOM") as FormMetadata;
                var bomOpRet = AppServiceContext.SetStatusService.SetBillStatus(this.Context, bomMetadata.BusinessInfo,
                    lstNewBomId.Select(o => new KeyValuePair<object, object>(o, null)).ToList(),
                    null, "Forbid", bomOption);
                this.OperationResult.MergeResult(bomOpRet);
            }
        }
    }
}
