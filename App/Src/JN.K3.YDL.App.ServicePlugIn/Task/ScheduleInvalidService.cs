using JN.BOS.Contracts;
using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS.Util;
namespace JN.K3.YDL.App.ServicePlugIn.Task
{
    [Description("自动失效未审核延期十天")]
    public class ScheduleInvalidService : IScheduleService
    {
        public void Run(Context ctx, Schedule schedule)
        {
            if (schedule == null) { return; }
            AutomationInvalid(ctx);
        }

        public void AutomationInvalid(Context ctx)
        {
            DateTime Date = new DateTime();
            Date = DateTime.Now;
            string sql = string.Format(@"select * from T_SAL_QUOTATION t1 inner join T_SAL_QUOTATIONENTRY t2 on t1.FID=t2.FID 
                                         where datediff(day,FMODIFYDATE,'{0}')>=10 and FDOCUMENTSTATUS <>'C'",Date);
            //未审核超过十天的数据
            DynamicObjectCollection obj = DBUtils.ExecuteDynamicObject(ctx, sql, null, null, CommandType.Text, null);
            if (obj.Count <= 0) return;
            List<int> list=new List<int>();
            int id=0;
            foreach (DynamicObject item in obj)
            {
                //单据类型为定价单
                if (Convert.ToString(item["FBILLTYPEID"]) == "dde3ab8a6b714e9fa7039d065a169f92") continue;
                id=Convert.ToInt32(item["fid"]);
                list.Add(id);
            }
            string Sql = string.Format("update T_SAL_QUOTATION set FINVALIDSTATUS='B' where fid in ({0})", string.Join(",", list.ToArray()));
            DBUtils.Execute(ctx,Sql);
            
            FormMetadata metaData = (FormMetadata)AppServiceContext.MetadataService.Load(ctx, "SAL_QUOTATION", true);
            var Buinfo=metaData.BusinessInfo;
            var billGroups = obj.GroupBy(o => Convert.ToString(o["FBillTypeId"]));
            List<int> lstNewMtrlId = new List<int>();
            List<int> lstNewBomId = new List<int>();
            foreach (var billGroup in billGroups)
            {

                var billTypeParaObj = AppServiceContext.GetService<ISysProfileService>().LoadBillTypeParameter(ctx, "SAL_QUOTATION", billGroup.Key);
                if (billTypeParaObj == null) continue;

                bool bSupportNoMtrlQuote = Convert.ToBoolean(billTypeParaObj["F_JN_NoMtrlIdQuotation"]);

                    if (bSupportNoMtrlQuote)
                    {
                        ExtendedDataEntitySet dataEntitySet = new ExtendedDataEntitySet();
                        dataEntitySet.Parse(billGroup.ToArray(), Buinfo);
                        var quoteEntryRows = dataEntitySet.FindByEntityKey("FBillHead");
                        var Entryobj = quoteEntryRows.Where(o => !o["F_JN_ProductName"].IsEmptyPrimaryKey()).ToArray();
                        if (Entryobj.Any() == false) continue;

                        lstNewMtrlId.AddRange(Entryobj.Select(o => (int)o["FMATERIALID"])
                            .Distinct()
                            .Where(o => o > 0));
                        lstNewBomId.AddRange(Entryobj.Select(o => (int)o["FBOMID"])
                            .Distinct()
                            .Where(o => o > 0));
                    }
                }
                    //禁用关联的物料及物料清单
                if (lstNewMtrlId.Count > 0)
                {                    
                    OperateOption mtrlOption = OperateOption.Create();
                    mtrlOption.SetVariableValue("IsList", true);
                    FormMetadata mtrlMetadata = AppServiceContext.MetadataService.Load(ctx, "BD_MATERIAL") as FormMetadata;
                    var mtrlOpRet = AppServiceContext.SetStatusService.SetBillStatus(ctx, mtrlMetadata.BusinessInfo,
                        lstNewMtrlId.Select(o => new KeyValuePair<object, object>(o, null)).ToList(),
                        null, "Forbid", mtrlOption);
                }
                if (lstNewBomId.Count > 0)
                {
                    OperateOption bomOption = OperateOption.Create();
                    bomOption.SetVariableValue("IsList", true);
                    FormMetadata bomMetadata = AppServiceContext.MetadataService.Load(ctx, "ENG_BOM") as FormMetadata;
                    var bomOpRet = AppServiceContext.SetStatusService.SetBillStatus(ctx, bomMetadata.BusinessInfo,
                        lstNewBomId.Select(o => new KeyValuePair<object, object>(o, null)).ToList(),
                        null, "Forbid", bomOption);
                }
            }
        }
    }
