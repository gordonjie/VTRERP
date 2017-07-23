using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;

namespace VTR.K3.YDL.BillPlugIn.SCM
{
    [Description("更新质检单让步接收数据插件")]

    public class VTR_YDL_InspectBillReAccept : AbstractBillPlugIn
    {
           
        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
 	         base.BeforeSave(e);

                EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FEntity");
                DynamicObjectCollection entryRows = this.View.Model.GetEntityDataObject(entryEntity);
                // var Entitydata = this.View.Model.GetValue("FEntity");
                int Row = this.Model.GetEntryRowCount("FEntity");
                EntryEntity FSerialSubEntity = this.View.BusinessInfo.GetEntryEntity("FPolicyDetail");
                int isConcession = 0;

                for (int i = 0; i < Row; i++)
                {
                    //int row = Convert.ToInt16(obj["FEntity_FSeq"]) - 1;
                    //int subRow = this.Model.GetEntryRowCount("FPolicyDetail");
                    DynamicObjectCollection subEntryRows = FSerialSubEntity.DynamicProperty.GetValue(
                                                   entryRows[i]) as DynamicObjectCollection;
                    int subRow = subEntryRows.Count<DynamicObject>();
                    double num = 0;
                   
                    for (int j = 0; j < subRow; j++)
                    {
                        if (subEntryRows[j]["UsePolicy"].ToString() == "B")
                        {
                            num =num+Convert.ToDouble(subEntryRows[j]["BasePolicyQty"]);
                            isConcession = isConcession + 1;
                        }
                        if (subEntryRows[j]["UsePolicy"].ToString() == "j")
                        {
                            num = num + Convert.ToDouble(subEntryRows[j]["BasePolicyQty"]);
                            isConcession = isConcession + 1;
                        }

                    }
                  
                    entryRows[i]["BaseReAcceptQty"] = num;


                
            }
                if (isConcession > 0)
                { this.View.Model.SetValue("F_JN_Concession", true); }
        }
    }
}
