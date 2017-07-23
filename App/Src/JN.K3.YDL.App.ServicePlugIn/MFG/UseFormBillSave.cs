using Kingdee.BOS;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity; 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.BOS.App.Data;
using System.Text;
using Kingdee.BOS.App.Core;
using Kingdee.BOS.Core.Metadata.FormElement; 
using Kingdee.BOS.Util;
using Kingdee.BOS.Orm.Metadata.DataEntity;


namespace JN.K3.YDL.App.ServicePlugIn.MFG
{
    /// <summary>
    /// 配方单保存：取子项的单位酶活量
    /// </summary>
    [Description("配方单保存：取子项的单位酶活量")]
    public class UseFormBillSave : AbstractOperationServicePlugIn
    {

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            e.FieldKeys.Add("FMATERIALID");
            e.FieldKeys.Add("FBOMID");
            e.FieldKeys.Add("FMaterialID2");
            e.FieldKeys.Add("FMTONO");
            e.FieldKeys.Add("FAuxPropID");
            e.FieldKeys.Add("FLot");
            e.FieldKeys.Add("F_JN_EnzymeSumQty");
            e.FieldKeys.Add("F_JN_BOMQty"); 
        }

        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);

            IEnumerable<ExtendedDataEntity> extendedDataEntities = e.SelectedRows;
            foreach (ExtendedDataEntity extendedDataEntity in extendedDataEntities)
            {
                DynamicObject billInfo = extendedDataEntity.DataEntity;

                DynamicObject bomInfor = billInfo["FBOMID"] as DynamicObject;
                if (bomInfor == null)
                {
                    continue;
                }

                DynamicObjectCollection subItemInfo = bomInfor["TreeEntity"] as DynamicObjectCollection;
                if (subItemInfo == null || subItemInfo.Count ==0)
                {
                    continue;
                }

                DynamicObjectCollection entryRows = billInfo["PPBomEntry"] as DynamicObjectCollection;
                if (entryRows == null || entryRows.Count == 0)
                {
                    continue;
                }

                foreach (var item in entryRows)
                {
                    decimal qty = Convert.ToDecimal(item["FJNUnitEnzymes"]);
                    if (qty > 0)
                    {
                        continue;
                    }

                    var subQty = subItemInfo.FirstOrDefault(f => f["MATERIALIDCHILD_Id"].ToString() == item["MaterialID_Id"].ToString()
                                                            && f["AuxPropID_Id"].ToString() == item["AuxPropId_Id"].ToString());
                    if (subQty != null)
                    {
                        var jnqty =Convert.ToDecimal ( subQty["FJNCompanyEA"]);
                        var bomQty = Convert.ToDecimal(item["F_JN_BOMQty"]);
                        
                        item["FJNUnitEnzymes"] = jnqty;
                        item["F_JN_EnzymeSumQty"] = jnqty * bomQty;

                        continue;
                    }
                    
                    subQty = subItemInfo.FirstOrDefault(f => f["MATERIALIDCHILD_Id"].ToString() == item["MaterialID_Id"].ToString() );
                    if (subQty != null)
                    {
                        var jnqty = Convert.ToDecimal(subQty["FJNCompanyEA"]);
                        var bomQty = Convert.ToDecimal(item["F_JN_BOMQty"]);

                        item["FJNUnitEnzymes"] = jnqty;
                        item["F_JN_EnzymeSumQty"] = jnqty * bomQty;
                    }
                }
            }
        }

        
    
    
    }
}
