using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.QTYConversion
{
    /// <summary>
    /// 生产订单推检验单转换插件
    /// </summary>
    [Description("生产订单推检验单转换插件")]
    public class Mo2InspectConvert : AbstractConvertPlugIn
    {
        /// <summary>
        /// 单据转换后事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterConvert(Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args.AfterConvertEventArgs e)
        {
            base.AfterConvert(e);
            ExtendedDataEntity[] assemblies = e.Result.FindByEntityKey("FBillHead");//下游单据           
            foreach (ExtendedDataEntity extendedDataEntity in assemblies)
            {
                DynamicObject parents = extendedDataEntity.DataEntity as DynamicObject;
                DynamicObjectCollection Entry = parents["Entity"] as DynamicObjectCollection;//主表体
                if (Entry == null || Entry.Count == 0)
                {
                    continue;
                }
                foreach (DynamicObject item in Entry)
                {
                    long qcschemeId = Convert.ToInt64(item["QCSchemeId_Id"]);//质检方案Id
                    string sql = string.Format(@"select A.FINSPECTITEMID,B.FSAMPLESCHEMEID,B.FANALYSISMETHOD,B.FDEFECTLEVEL,B.FDESTRUCTINSPECT,
                                B.FKEYINSPECT,B.FQUALITYSTDID,B.FINSPECTMETHODID,B.FINSPECTINSTRUMENTID,B.FINSPECTBASISID,B.FUNITID
                                from T_QM_QCSCHEMEENTRY A 
                                inner join T_QM_INSPECTITEM B on A.FINSPECTITEMID=B.FID
                                where A.FID={0}", qcschemeId);
                    DynamicObjectCollection inspectItem = DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, System.Data.CommandType.Text, null);//检验项目
                    if (inspectItem == null || inspectItem.Count == 0) continue;
                    DynamicObjectCollection inspectEntry = item["ItemDetail"] as DynamicObjectCollection;//检验项目子单据体
                    foreach (DynamicObject inspect in inspectItem)
                    {
                        DynamicObject newData = inspectEntry.DynamicCollectionItemPropertyType.CreateInstance() as DynamicObject;//新建子单据体分录
                        newData["InspectItemId_Id"] = inspect["FINSPECTITEMID"];
                        newData["SampleSchemeId_Id"] = inspect["FSAMPLESCHEMEID"];
                        newData["AnalysisMethod"] = inspect["FANALYSISMETHOD"];
                        newData["DefectLevel1"] = inspect["FDEFECTLEVEL"];
                        newData["DestructInspect"] = Convert.ToInt32(inspect["FDESTRUCTINSPECT"]) == 0 ? false : true;
                        newData["KeyInspect"] = Convert.ToInt32(inspect["FKEYINSPECT"]) == 0 ? false : true;
                        newData["QualityStdId_Id"] = inspect["FQUALITYSTDID"];
                        newData["InspectMethodId_Id"] = inspect["FINSPECTMETHODID"];
                        newData["InspectInstrumentId_Id"] = inspect["FINSPECTINSTRUMENTID"];
                        newData["InspectBasisId_Id"] = inspect["FINSPECTBASISID"];
                        newData["UnitId_Id"] = inspect["FUNITID"];
                        inspectEntry.Add(newData);
                    }
                }
                Kingdee.BOS.Contracts.ServiceFactory.GetService<IDBService>(this.Context).LoadReferenceObject(this.Context, new DynamicObject[] { parents }, e.TargetBusinessInfo.GetDynamicObjectType(), false);//重新加载一次信息，刷新出基础资料（当基础资料只有ID有值时）
            }            
        }
    }
}
