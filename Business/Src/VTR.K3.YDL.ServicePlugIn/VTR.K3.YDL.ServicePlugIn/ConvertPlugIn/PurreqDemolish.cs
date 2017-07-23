using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;

namespace VTR.K3.YDL.ServicePlugIn.ConvertPlugIn
{
    public class PurreqDemolish : AbstractConvertPlugIn
    {
        /// <summary>
        /// 主单据体的字段携带完毕，与源单的关联关系创建好之后，触发此事件
        /// </summary>
        /// <param name="e"></param>
        public override void OnAfterCreateLink(CreateLinkEventArgs e)
        {
            // 预先获取一些必要的元数据，后续代码要用到：
            // 源单第二单据体
            Entity srcSecondEntity = e.SourceBusinessInfo.GetEntity("FCRMAllocation");

            // 目标单第一单据体
            Entity mainEntity = e.TargetBusinessInfo.GetEntity("FEntity");

            // 目标单第二单据体
            Entity secondEntity = e.TargetBusinessInfo.GetEntity("FCRMAllocation");

            // 目标单关联子单据体
            Entity linkEntity = null;
            Form form = e.TargetBusinessInfo.GetForm();
            if (form.LinkSet != null
                && form.LinkSet.LinkEntitys != null
                && form.LinkSet.LinkEntitys.Count != 0)
            {
                linkEntity = e.TargetBusinessInfo.GetEntity(
                    form.LinkSet.LinkEntitys[0].Key);
            }

            if (linkEntity == null)
            {
                return;
            }

            // 获取生成的全部下游单据
            ExtendedDataEntity[] billDataEntitys = e.TargetExtendedDataEntities.FindByEntityKey("FBillHead");

            // 对下游单据，逐张单据进行处理
            foreach (var item in billDataEntitys)
            {
                DynamicObject dataObject = item.DataEntity;

                // 定义一个集合，用于收集本单对应的源单内码
                HashSet<long> srcBillIds = new HashSet<long>();
                HashSet<long> crmBillIds = new HashSet<long>();

                // 开始到主单据体中，读取关联的源单内码
                DynamicObjectCollection mainEntryRows =
                    mainEntity.DynamicProperty.GetValue(dataObject) as DynamicObjectCollection;
                foreach (var mainEntityRow in mainEntryRows)
                {
                    DynamicObjectCollection linkRows =
                        linkEntity.DynamicProperty.GetValue(mainEntityRow) as DynamicObjectCollection;
                    foreach (var linkRow in linkRows)
                    {
                        long srcBillId = Convert.ToInt64(linkRow["SBillId"]);
                        if (srcBillId != 0
                            && srcBillIds.Contains(srcBillId) == false)
                        {
                            srcBillIds.Add(srcBillId);
                        }
                    }
                }
                if (srcBillIds.Count == 0)
                {
                    continue;
                }
                // 开始加载源单第二单据体上的字段

                // 确定需要加载的源单字段（仅加载需要携带的字段）
               /* List<SelectorItemInfo> selector = new List<SelectorItemInfo>();
              
                selector.Add(new SelectorItemInfo("FCooperationType"));
                selector.Add(new SelectorItemInfo("FEmployee"));
                selector.Add(new SelectorItemInfo("FDept"));
                selector.Add(new SelectorItemInfo("FRead"));
                selector.Add(new SelectorItemInfo("FModify"));
                selector.Add(new SelectorItemInfo("FDelete"));
                selector.Add(new SelectorItemInfo("FAllocation"));
                selector.Add(new SelectorItemInfo("FCRMClose"));
                selector.Add(new SelectorItemInfo("FCRMUnClose"));
                selector.Add(new SelectorItemInfo("FAllocUser"));
                selector.Add(new SelectorItemInfo("FAllocTime"));*/
                string selector = " FCooperationType, FEmployee, FDept, FRead, FModify, FDelete, FAllocation, FCRMClose, FCRMUnClose, FAllocUser, FAllocTime ";
                // TODO: 继续添加其他需要携带的字段，示例代码略
                // 设置过滤条件


                string filter = string.Format(" FOBJECTBILLID IN ({0}) ",
                    string.Join(",", srcBillIds));
                /*var sql = string.Format(@"select t1.fid from T_CRM_AllocationsEntry t1
join T_CRM_ALLOCATIONS t2 on t1.FID=t2.FID
where t2.FobjectId='PUR_Requisition' and  t2.{0}", filter);
                var CRMids = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                foreach (var CRMid in CRMids)
                {
                    long crmBillId = Convert.ToInt64(CRMid["FID"]);
                    if (crmBillId != 0
                        && crmBillIds.Contains(crmBillId) == false)
                    {
                        crmBillIds.Add(crmBillId);
                    }
                }
               filter = string.Format(" FID IN ({0}) or FID IN ({1}) ",
                    string.Join(",", srcBillIds), string.Join(",", crmBillIds));
                OQLFilter filterObj = OQLFilter.CreateHeadEntityFilter(filter);

                // 读取源单
                IViewService viewService = ServiceHelper.GetService<IViewService>();
                var srcBillObjs = viewService.Load(this.Context,
                    e.SourceBusinessInfo.GetForm().Id,
                    selector,
                    filterObj);*/
               var sql = string.Format(@"select {0} from T_CRM_AllocationsEntry t1
join T_CRM_ALLOCATIONS t2 on t1.FID=t2.FID
where t2.FobjectId='PUR_Requisition' and  t2.{1}", selector, filter);
                var srcBillObjs = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);

                // 开始把源单单据体数据，填写到目标单上
                DynamicObjectCollection secondEntryRows =
                    secondEntity.DynamicProperty.GetValue(dataObject) as DynamicObjectCollection;
                secondEntryRows.Clear();    // 删除空行

                foreach (var srcBillObj in srcBillObjs)
                {
                  
                        // 目标单添加新行，并接受源单字段值
               
                        

                  
                             // 目标单添加新行，并接受源单字段值
                             DynamicObject newRow = new DynamicObject(secondEntity.DynamicObjectType);
                             BaseDataField EmployeeFID = e.TargetBusinessInfo.GetField("FEmployee") as BaseDataField;
                             BaseDataField DeptFID = e.TargetBusinessInfo.GetField("FDept") as BaseDataField;
                             BaseDataField AllocUserFID = e.TargetBusinessInfo.GetField("FAllocUser") as BaseDataField;
                             long EmployeeID = Convert.ToInt64(srcBillObj["FEmployee"]);
                             long DeptID =  Convert.ToInt64(srcBillObj["FDept"]);
                             long AllocUserID = Convert.ToInt64(srcBillObj["FAllocUser"]);
                             IViewService viewService = ServiceHelper.GetService<IViewService>();
                             DynamicObject[] EmployeeObjs = viewService.LoadFromCache(this.Context, new object[] { EmployeeID }, EmployeeFID.RefFormDynamicObjectType);
                             DynamicObject[] DeptObjs = viewService.LoadFromCache(this.Context, new object[] { DeptID }, DeptFID.RefFormDynamicObjectType);
                             DynamicObject[] AllocUserObjs = viewService.LoadFromCache(this.Context, new object[] { AllocUserID }, AllocUserFID.RefFormDynamicObjectType);
                             
                             secondEntryRows.Add(newRow);


                             // 填写字段值
                             newRow["FCooperationType"] = srcBillObj["FCooperationType"];
                             EmployeeFID.RefIDDynamicProperty.SetValue(newRow, EmployeeID);
                             EmployeeFID.DynamicProperty.SetValue(newRow, EmployeeObjs[0]);
                             DeptFID.RefIDDynamicProperty.SetValue(newRow, DeptID);
                             DeptFID.DynamicProperty.SetValue(newRow, DeptObjs[0]);

                             //newRow["FEmployee"] = srcBillObj["FEmployee"];
                             //newRow["FDept"] = srcBillObj["FDept"];
                             if (srcBillObj["FRead"].ToString()=="1")
                              newRow["FRead"] = true;  else newRow["FRead"] = false;
                             if (srcBillObj["FModify"].ToString() == "1")
                                 newRow["FModify"] = true;
                             else newRow["FModify"] = false;
                             if (srcBillObj["FDelete"].ToString() == "1")
                                 newRow["FDelete"] = true;
                             else newRow["FDelete"] = false;
                             if (srcBillObj["FAllocation"].ToString() == "1")
                                 newRow["FAllocation"] = true;
                             else newRow["FAllocation"] = false;
                             if (srcBillObj["FCRMClose"].ToString() == "1")
                                 newRow["FCRMClose"] = true;
                             else newRow["FCRMClose"] = false;
                             if (srcBillObj["FCRMUnClose"].ToString() == "1")
                                 newRow["FCRMUnClose"] = true;
                             else newRow["FCRMUnClose"] = false;

                             //newRow["FAllocUser"] = srcBillObj["FAllocUser"];
                            // AllocUserFID.RefIDDynamicProperty.SetValue(newRow, AllocUserID);
                           // AllocUserFID.DynamicProperty.SetValue(newRow, AllocUserObjs[0]);
                            // newRow["FAllocTime"] = Convert.ToDateTime(srcBillObj["FAllocTime"]);

                             // TODO: 逐个填写其他字段值，示例代码略
                         
                    
                }
            }
        }

    }
}
