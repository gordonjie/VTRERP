using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Metadata.EntityElement;

namespace JN.K3.YDL.Print
{
    [System.ComponentModel.Description("质检单打印数据修正")]
    public class InspectBillPrint : AbstractBillPlugIn
    {
        /// <summary>

        /// 需求：打印时，如果比较符号为“=”不打印,当目标值为上限值是，打印小于等于，当目标值为下限制时，打印大于等于

        /// </summary>

        /// <param name="e"></param>

        private int row = 0;
      
        public override void OnPrepareNotePrintData(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePrintDataEventArgs e)
        {


            var aaa=this.View.BillBusinessInfo.GetField("FInspectValQ");
            this.View.BillBusinessInfo.GetField("FInspectValQ").DisplayOrder = 1;
            var qureyObjs = e.DataObjects;
            //int row = 0;
            if (e.DataSourceId.Equals("FItemDetail"))
            {

                //int row1 = qureyObjs.Count<Object>();
                EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FEntity");
                DynamicObjectCollection entryRows = this.View.Model.GetEntityDataObject(entryEntity);
                EntryEntity FSerialSubEntity = this.View.BusinessInfo.GetEntryEntity("FItemDetail");

                int rows = this.View.Model.GetEntryRowCount("FEntity");
                int subrow = 0;
                
                foreach (var obj in qureyObjs)
                //for (int i = 0; i < rows; i++)
                {
                    //int row = Convert.ToInt16(obj["FEntity_FSeq"]) - 1;
                    //int subrow = Convert.ToInt16(obj["FItemDetail_FSeq"]) - 1;
             
                        DynamicObjectCollection subEntryRows = FSerialSubEntity.DynamicProperty.GetValue(
                                     entryRows[row]) as DynamicObjectCollection;
                   
                    // TODO : subEntryRows为每条主单据体行下所挂的子单据体行集合；
                    // TODO : subEntryRows[0] 为第一条子单据体行
                    //int subrow2 = subEntryRows.Count;
                    //if Convert.ToInt16(obj["FItemDetail_FSeq"])
                  
                    string uplimit = subEntryRows[subrow]["UpLimit"].ToString();
                    string downlimit = subEntryRows[subrow]["DownLimit"].ToString();
                    string analysisMethod = subEntryRows[subrow]["AnalysisMethod"].ToString();
                    string targetVal = subEntryRows[subrow]["TargetVal"].ToString();
                    subrow = subrow + 1;


                    
                    if (analysisMethod == "1")
                    {


                        if (targetVal == downlimit && analysisMethod == "1")
                        {

                           obj["FCompareSymbol"] = "3";

                        }
                        else if (targetVal == uplimit && analysisMethod == "1")
                        {
                           obj["FCompareSymbol"] = "5";
                        }
                    }
                    else
                    {
                           obj["FCompareSymbol"] = " ";
                    }
                    
                }
                row = row + 1;
                if (row == rows )
                {
                    row = 0; 
                }
                e.DataObjects = qureyObjs;
                base.OnPrepareNotePrintData(e);
                }

        }
    }
}

