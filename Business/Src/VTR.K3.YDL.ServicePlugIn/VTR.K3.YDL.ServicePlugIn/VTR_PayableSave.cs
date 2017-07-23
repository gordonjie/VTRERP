using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.AP.App.ServicePlugIn;
using Kingdee.K3.FIN.AP.App.ServicePlugIn.Validator;
using Kingdee.K3.FIN.App.Core.Validation;




namespace VTR.K3.YDL.ServicePlugIn
{
    public class VTR_PayableSave : PayableSave
    {
        [Description("扩展-修改应付单合计金额为0时可以保存")]
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {

            DynamicObject obj = e.DataEntities[0];
            var ALLAMOUNTFOR = obj["FALLAMOUNTFOR"];
            if (ALLAMOUNTFOR.ToString() == "0")
            { return; }
            else
            {
                DateOrderValidator item = new DateOrderValidator();
                //item.set_AlwaysValidate(true);
                item.AlwaysValidate = true;
                //item.set_EntityKey("FBillHead");
                item.EntityKey = "FBillHead";
                e.Validators.Add(item);
                APSaveValidator validator2 = new APSaveValidator();
                validator2.EntityKey = "FBillHead";
                e.Validators.Add(validator2);
                ControlByHSCloseDate date = new ControlByHSCloseDate();
                date.EntityKey = "FBillHead";
                e.Validators.Add(date);

                CommonMaterialAuxPtyItemsValueValidator validator3 = new CommonMaterialAuxPtyItemsValueValidator();
                validator3.EntityKey = "FEntityDetail";
                validator3.MaterialName = "MATERIALID";
                validator3.AuxPtyName = "FAUXPROPID";
                e.Validators.Add(validator3);
            }
        }







    }
}
