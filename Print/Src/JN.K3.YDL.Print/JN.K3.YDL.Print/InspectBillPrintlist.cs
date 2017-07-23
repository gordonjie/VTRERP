using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.List.PlugIn;

namespace JN.K3.YDL.Print
{
    [System.ComponentModel.Description("质检单打印数据修正")]
    public class InspectBillPrintList : AbstractListPlugIn
    {
        /// <summary>

        /// 需求：打印时，如果比较符号为“=”不打印,当目标值为上限值是，打印小于等于，当目标值为下限制时，打印大于等于

        /// </summary>

        /// <param name="e"></param>
        /// 
       

        public override void OnPrepareNotePrintData(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePrintDataEventArgs e)
        {



            var qureyObjs = e.DataObjects;

            if (e.DataSourceId.Equals("FItemDetail"))
            {
                EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FEntity");
                DynamicObjectCollection entryRows = this.View.Model.GetEntityDataObject(entryEntity);

                EntryEntity FSerialSubEntity = this.View.BusinessInfo.GetEntryEntity("FItemDetail");

                int rows = this.View.Model.GetEntryRowCount("FEntity");
                
               // foreach (var obj in qureyObjs)
                for (int i = 0; i < rows; i++)
                {
                    //int row = Convert.ToInt16(obj["FEntity_FSeq"]) - 1;
                    //int subrow = Convert.ToInt16(obj["FItemDetail_FSeq"]) - 1;
                    DynamicObjectCollection subEntryRows = FSerialSubEntity.DynamicProperty.GetValue(
                                 entryRows[i]) as DynamicObjectCollection;
                    int subrow = entryRows.Count;
                    for (int j = 0; j < subrow; j++)
                    {
                        // TODO : subEntryRows为每条主单据体行下所挂的子单据体行集合；
                        // TODO : subEntryRows[0] 为第一条子单据体行

                        string uplimit = subEntryRows[j]["UpLimit"].ToString();
                        string downlimit = subEntryRows[j]["DownLimit"].ToString();
                        string analysisMethod = subEntryRows[j]["AnalysisMethod"].ToString();
                        string targetVal = subEntryRows[j]["TargetVal"].ToString();

                        if (analysisMethod == "1")
                        {


                            if (targetVal == downlimit && analysisMethod == "1")
                            {

                                qureyObjs[j]["FCompareSymbol"] = "3";

                            }
                            else if (targetVal == uplimit && analysisMethod == "1")
                            {
                                qureyObjs[j]["FCompareSymbol"] = "5";
                            }
                        }
                        else
                        {
                            qureyObjs[j]["FCompareSymbol"] = " ";
                        }


                    }
                }
                e.DataObjects = qureyObjs;
                base.OnPrepareNotePrintData(e);
            }

        }
    }
}

