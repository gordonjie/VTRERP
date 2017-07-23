using JN.K3.YDL.Core;
using JN.K3.YDL.App.ServicePlugIn;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;


namespace JN.K3.YDL.App.ServicePlugIn.Bandservice
{
    /// <summary>

    /// 自定义查找银行状态服务

    /// </summary>

    public class autofindbandstatus : IScheduleService
    {

        /// <summary>

        /// 实际运行的Run 方法

        /// </summary>

        /// <param name="ctx"></param>

        /// <param name="schedule"></param>

        public void Run(Context ctx, Schedule schedule)
        {

            DateTime currentTime = new System.DateTime();
            JNBandPara actband = new JNBandPara();
            JNBand findbandservice = new JNBand();
            currentTime =TimeServiceHelper.GetSystemDateTime(ctx);
            string sql = "";
            sql = string.Format(@"select t1.FENTRYID,t1.F_JNOBSSID,t2.FACCOUNTID from T_AP_PAYBILLENTRY_B t1 
join T_AP_PAYBILLENTRY t2 on t2.fid=t1.fid
join T_AP_PAYBILL t3 on t1.FID=t3.FID where  DATEDIFF(DAY,GETDATE(),FAPPROVEDATE)>-10
and (t1.F_JNOBSSID is not null and  t1.F_JNOBSSID <>'')");
            DynamicObjectCollection rundatas=  DBUtils.ExecuteDynamicObject(ctx, sql);
            string token = ""; 
            int i = 0;
            
            foreach (var rundata in rundatas)
            {
                FormMetadata formMetadata = MetaDataServiceHelper.Load(ctx, "CN_BANKACNT") as FormMetadata;
                DynamicObject FrACCOUNT = BusinessDataServiceHelper.LoadSingle(
                                ctx,
                                rundata["FACCOUNTID"],
                                formMetadata.BusinessInfo.GetDynamicObjectType());
                DynamicObject FrBAND = FrACCOUNT["BANKID"] as DynamicObject;
                actband.bandid = Convert.ToInt32(FrACCOUNT["Id"]);
                //actband.addr = Convert.ToString(FrACCOUNT["BANKADDRESS"]);
                actband.name = Convert.ToString(FrACCOUNT["Name"]);
                actband.bandnum = Convert.ToString(FrACCOUNT["ACNTBRANCHNUMBER"]);
                actband.cn = Convert.ToString(FrACCOUNT["NUMBER"]);
                actband.bandname = Convert.ToString(FrBAND["Name"]);
                string obssid = Convert.ToString(rundata["F_JNOBSSID"]);
                if (i == 0)//首行获取令牌
                {
                   token= findbandservice.checkin(ctx, actband);
                   i++;
                }
                string result = "";
                if (obssid.Length > 1)
                {
                    result = findbandservice.findPay(ctx, actband, obssid, token);
                }
                if (result.Length > 0)
                {
                    switch (result)
                    {
                        default:
                            BusinessDataServiceHelper.SetState(ctx, "T_AP_PAYBILLENTRY_B", "FBankStatus", "A", "F_JNOBSSID", new object[] { obssid });
                            break;
                        case "待授权":
                            BusinessDataServiceHelper.SetState(ctx, "T_AP_PAYBILLENTRY_B", "FBankStatus", "B", "F_JNOBSSID", new object[] { obssid });
                            break;
                        case "ok":
                            BusinessDataServiceHelper.SetState(ctx, "T_AP_PAYBILLENTRY_B", "FBankStatus", "C", "F_JNOBSSID", new object[] { obssid });
                            break;
                        case "授权拒绝":
                            BusinessDataServiceHelper.SetState(ctx, "T_AP_PAYBILLENTRY_B", "FBankStatus", "D", "F_JNOBSSID", new object[] { obssid });
                            break;
                        case "交易处理中":
                            BusinessDataServiceHelper.SetState(ctx, "T_AP_PAYBILLENTRY_B", "FBankStatus", "B", "F_JNOBSSID", new object[] { obssid });
                            break;
                    }
                }
               
                //Thread.Sleep(5000);
     

            }
            //throw new NotImplementedException();
            if (token.Length > 1)
            {
                findbandservice.checkout(ctx, actband);
            }
            

        }


        
    }
}
