using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Util;

namespace VTR.K3.Bill.PlugIn
{
    [System.ComponentModel.Description("检验单子表复选")]
    public class EntitySelectManyRow : AbstractBillPlugIn
    {
        //public static int[] selectRow;
        public override void DataChanged(DataChangedEventArgs e)
        {
            if (!e.Field.Key.EqualsIgnoreCase("FQECheckBox"))
            {
                return;
            }
            else
            {
                    int Row = this.Model.GetEntryRowCount("FEntity");
                    int[] selectRow = new int[Row];
                    int lastselect = -1;
                    bool selectkey = false;
                    for (int i = 0; i < Row; i++)
                    {
                        selectkey = this.View.Model.GetValue("FQECheckBox", i).Equals(true);
                        if (selectkey)
                        { //
                            selectRow[i] = i;
                            lastselect = i;

                        }
                        else { selectRow[i] = -1;
                       
                        }

                    }

                    this.View.GetControl<EntryGrid>("FEntity").SelectRows(selectRow);
                    this.View.GetControl<EntryGrid>("FEntity").SetFocusRowIndex(lastselect);
                   
            }
                
            }

        }
}


