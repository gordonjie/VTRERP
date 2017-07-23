using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;

namespace VTR.K3.YDL.ServicePlugIn.ConvertPlugIn
{
    public class PurreqToPurreq : AbstractConvertPlugIn
    {
        /// <summary>
        /// 最后触发：单据转换后事件
        /// </summary>
        /// <param name="e"/>
     public override void AfterConvert(AfterConvertEventArgs e)
        {
            ExtendedDataEntity[] data = e.Result.FindByEntityKey("FBillHead");
           

            if (data != null && data.Length > 0)
            {

                foreach (ExtendedDataEntity extendedDataEntity in data)
                {
                    DynamicObject billObj = extendedDataEntity.DataEntity;
                    var crmobj = extendedDataEntity.DataEntity["CRM_AllocationsEntry"] as DynamicObjectCollection;
                    int row =crmobj.Count;
                    //List<Field> FCooperationType = e.SourceBusinessInfo.Entrys[4].;
                    if (crmobj == null)
                    {
                        continue;
                    }

                    for (int i=0;i<row;i++)
                    {
                    

                    crmobj[i]["FCooperationType"] = Convert.ToString(e.SourceBusinessInfo.GetField("FCooperationType"));
                    crmobj[i]["FEmployee"] =  e.SourceBusinessInfo.GetField("FEmployee");
                    crmobj[i]["FDept"] = e.SourceBusinessInfo.GetField("FDept");
                    crmobj[i]["FRead"] = e.SourceBusinessInfo.GetField("FRead");
                    crmobj[i]["FModify"] = e.SourceBusinessInfo.GetField("FModify");
                    crmobj[i]["FDelete"] = e.SourceBusinessInfo.GetField("FDelete");
                    crmobj[i]["FAllocation"] = e.SourceBusinessInfo.GetField("FAllocation");
                    crmobj[i]["FCRMClose"] = e.SourceBusinessInfo.GetField("FCRMClose");
                    crmobj[i]["FCRMUnClose"] = e.SourceBusinessInfo.GetField("FCRMUnClose");
                    crmobj[i]["FAllocUser"] = e.SourceBusinessInfo.GetField("FAllocUser");
                    crmobj[i]["FAllocTime"] = e.SourceBusinessInfo.GetField("FAllocTime");
                    }



                }
            }


        }
    }
}
