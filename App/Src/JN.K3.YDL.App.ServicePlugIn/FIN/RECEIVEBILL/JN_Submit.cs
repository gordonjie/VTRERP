using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.FIN.RECEIVEBILL
{
  /// <summary>
    /// 收款单提交插件
    /// </summary>
    [Description("销售订单提交插件")]
    public class JN_Submit : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FCONTACTUNITTYPE");
            e.FieldKeys.Add("FCONTACTUNIT");
            e.FieldKeys.Add("FJNFistSaler");
         }

        public override void BeginOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);           
            if (e.DataEntitys == null || e.DataEntitys.Count() <= 0)
            {
                return;
            }
            foreach (DynamicObject data in e.DataEntitys)
            {
                string FCONTACTUNITTYPE = Convert.ToString( data["CONTACTUNITTYPE"]);
                int FCONTACTUNIT_Id =  Convert.ToInt32(data["CONTACTUNIT_Id"]);
                DynamicObject FCONTACTUNIT = data["CONTACTUNIT"] as DynamicObject;
                DynamicObject FJNFistSaler = data["FJNFistSaler"] as DynamicObject;
                if (FCONTACTUNITTYPE == "BD_Customer" && FCONTACTUNIT != null && FJNFistSaler == null)
                {
                    FormMetadata formMetadata = MetaDataServiceHelper.Load(this.Context, "BD_Customer") as FormMetadata;
                    DynamicObject CustObject = BusinessDataServiceHelper.LoadSingle(
                                    this.Context,
                                    FCONTACTUNIT_Id,
                                    formMetadata.BusinessInfo.GetDynamicObjectType());
                    if(CustObject!= null)
                    {
                        DynamicObject Saler = CustObject["JN_SalesId"] as DynamicObject;
                        if (Saler != null)
                        {
                            int Saler_Id = Convert.ToInt32(Saler["id"]);
                            int billid = Convert.ToInt32(data["id"]);
                            string strSQL = string.Format("/*dialect*/update T_AR_RECEIVEBILL set FJNFISTSALER ={0} where fid={1}", Saler_Id, billid);
                            DBUtils.Execute(this.Context, strSQL);
                        }
                    }
                }
                //AppServiceContext.SaveService.Save(this.Context, formMetadata.BusinessInfo, dataObjects);

            }
        }

    }
}
