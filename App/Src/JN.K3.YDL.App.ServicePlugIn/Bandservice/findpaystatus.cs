using JN.K3.YDL.Core;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.Bandservice
{
    public class findpaystatus : JNBand
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FACCOUNTID");
            e.FieldKeys.Add("FPAYACCOUNTNAME");
            e.FieldKeys.Add("FPAYBANKID");
            e.FieldKeys.Add("FOPPOSITEBANKACCOUNT");
            e.FieldKeys.Add("FOPPOSITECCOUNTNAME");
            e.FieldKeys.Add("FOPPOSITEBANKNAME");
            e.FieldKeys.Add("FOpenAddressRec");//开户行地址
            e.FieldKeys.Add("FCNAPS");//联行号
            e.FieldKeys.Add("FBankTypeRec");//收款银行
            e.FieldKeys.Add("FPAYACCOUNTNAME");

            e.FieldKeys.Add("FPAYORGID");
            e.FieldKeys.Add("FBillTypeID");
            e.FieldKeys.Add("F_JNOBSSID");

        }
        /// <summary>
        /// 操作执行前逻辑
        /// </summary>
        /// <param name="e"></param>
        public override void BeginOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeginOperationTransactionArgs e)
        {
            JNBandPara actband = new JNBandPara();
            var billGroups = e.DataEntitys;
            string token = ""; 
            int i = 0;
            
            foreach (var billGroup in billGroups)
            {
                string result = "";
                DynamicObjectCollection BILLENTRYDATAs = billGroup["PAYBILLENTRY"] as DynamicObjectCollection;
                foreach (var BILLENTRYDATA in BILLENTRYDATAs)
                {
                    DynamicObject FrACCOUNT = BILLENTRYDATA["FACCOUNTID"] as DynamicObject;
                    DynamicObject FrBAND = FrACCOUNT["BANKID"] as DynamicObject;
                    actband.bandid = Convert.ToInt32(BILLENTRYDATA["FACCOUNTID_Id"]);
                    //actband.addr = Convert.ToString(FrACCOUNT["BANKADDRESS"]);
                    actband.name = Convert.ToString(FrACCOUNT["Name"]);
                    actband.bandnum = Convert.ToString(FrACCOUNT["ACNTBRANCHNUMBER"]);
                    actband.cn = Convert.ToString(FrACCOUNT["NUMBER"]);
                    actband.bandname = Convert.ToString(FrBAND["Name"]);
                    if (i == 0)//首单获取令牌
                    {
                        token = checkin(this.Context, actband);
                        i++;
                    }
                    string obssid= Convert.ToString(BILLENTRYDATA["F_JNOBSSID"]);
                    if (obssid.Length > 1)
                    {
                        result = findPay(this.Context, actband, obssid, token);
                    }
                    if (result.Length > 0)
                    {
                        switch (result)
                        {
                            default:
                                BusinessDataServiceHelper.SetState(this.Context, "T_AP_PAYBILLENTRY_B", "FBankStatus", "A", "F_JNOBSSID", new object[] { obssid });
                                break;
                            case "待授权":
                                BusinessDataServiceHelper.SetState(this.Context, "T_AP_PAYBILLENTRY_B", "FBankStatus", "B", "F_JNOBSSID", new object[] { obssid });
                                break;
                            case "ok":
                                BusinessDataServiceHelper.SetState(this.Context, "T_AP_PAYBILLENTRY_B", "FBankStatus", "C", "F_JNOBSSID", new object[] { obssid });
                                break;
                            case "授权拒绝":
                                BusinessDataServiceHelper.SetState(this.Context, "T_AP_PAYBILLENTRY_B", "FBankStatus", "D", "F_JNOBSSID", new object[] { obssid });
                                break;
                            case "交易处理中":
                                BusinessDataServiceHelper.SetState(this.Context, "T_AP_PAYBILLENTRY_B", "FBankStatus", "B", "F_JNOBSSID", new object[] { obssid });
                                break;
                        }
                    }
                }
               
            }
            if (token.Length > 1)
            {
                checkout(this.Context, actband);
            }

        }
        }
}
