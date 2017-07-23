using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.ServiceHelper;


namespace JN.K3.YDL.App.ServicePlugIn.MFG.InspectBill
{
    /// <summary>
    /// 检验单审核的服务插件：总酶活数量更新到收料单分录对应的即时库存的日志及即时库存
    /// 总酶活数量已在反写规则中通过覆盖模式更新到收料单。检验单反审核时总酶活数量不会回滚。所以检验单反审核不重新处理库存数
    /// 检验单再次审核时自动调整
    /// </summary>
    [Description("检验单审核的服务插件：总酶活数量更新到收料单分录对应的即时库存的日志及即时库存")]
    public class JN_InspectBillJustForAudit : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 在检验单的保存或审核时，会把酶活相关数据反写到收料单，导致收料单上的酶活量跟批次号主档资料里面的跟踪信息对不上，这里重新保存一下收料单以便刷新批号主档资料
        /// </summary>
        /// <param name="e"></param>
        public override void AfterExecuteOperationTransaction( AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            if (e.DataEntitys.IsEmpty())
            {
                return;
            }
            List<long> lstEntrys = new List<long>();
            foreach (var item in e.DataEntitys)
            {
                
                DynamicObjectCollection dyCollection = item["Entity"] as DynamicObjectCollection;
                foreach (DynamicObject dyEntry in dyCollection)
                {
                    lstEntrys.Add(Convert.ToInt64(dyEntry["Id"]));
                }
            }
            List<string> sqlLst = new List<string>();
            //先根据检验单与收料单的酶活总量的差异更新即时库存主表
            string sql = string.Format(@"/*dialect*/   
                        update t set t.FSECQTY=t.FSECQTY-(FUPDATESECQTY-FAUXUNITQTY)
                        from T_STK_INVENTORY t inner join (
                        select b.FJNQTY,x.FAUXUNITQTY,x.FENTRYID,tg.FUPDATESECQTY,tg.FLOGID,tg.FINVENTORYID
                         from  T_QM_INSPECTBILLENTRY_LK c  
                        inner join T_QM_INSPECTBILLENTRY b on c.FENTRYID=b.FENTRYID
                        inner join  T_PUR_ReceiveEntry x on x.FENTRYID =c.fsid and x.fid =c.FSBILLID
                        inner join (select MAX(FUPDATETIME) as FLastTime,FSOURENTRYID,FSOURFORMID from T_STK_INVENTORYLOG where FSOURFORMID='PUR_ReceiveBill' and FOPERATIONNUMBER='Audit'
                        group by FSOURENTRYID,FSOURFORMID) tgg on tgg.FSOURENTRYID=x.FENTRYID
                        inner join  T_STK_INVENTORYLOG tg on tg.FSOURFORMID=tgg.FSOURFORMID and tg.FSOURENTRYID=tgg.FSOURENTRYID and tg.FUPDATETIME=tgg.FLastTime
                        where  c.FRULEID ='QM_PURReceive2Inspect' and  c.FENTRYID in ({0}) 
                        and tg.FUPDATESECQTY<>x.FAUXUNITQTY) t1 on t.FID=t1.FINVENTORYID ;", string.Join(",", lstEntrys));
            sqlLst.Add(sql);
            //先根据检验单与收料单的酶活总量的差异更新即时库存日志表
            sql = string.Format(@"/*dialect*/   
                        update tg set tg.FUPDATESECQTY=x.FAUXUNITQTY
                         from  T_QM_INSPECTBILLENTRY_LK c  
                        inner join T_QM_INSPECTBILLENTRY b on c.FENTRYID=b.FENTRYID
                        inner join  T_PUR_ReceiveEntry x on x.FENTRYID =c.fsid and x.fid =c.FSBILLID
                        inner join (select MAX(FUPDATETIME) as FLastTime,FSOURENTRYID,FSOURFORMID from T_STK_INVENTORYLOG where FSOURFORMID='PUR_ReceiveBill' and FOPERATIONNUMBER='Audit'
                        group by FSOURENTRYID,FSOURFORMID) tgg on tgg.FSOURENTRYID=x.FENTRYID
                        inner join  T_STK_INVENTORYLOG tg on tg.FSOURFORMID=tgg.FSOURFORMID and tg.FSOURENTRYID=tgg.FSOURENTRYID and tg.FUPDATETIME=tgg.FLastTime
                        where  c.FRULEID ='QM_PURReceive2Inspect' and c.FENTRYID in ({0}) 
                        and tg.FUPDATESECQTY<>x.FAUXUNITQTY ;", string.Join(",", lstEntrys));
            sqlLst.Add(sql);
            
         
            if (sqlLst.Count > 0)
            {
                DBUtils.ExecuteBatch(this.Context, sqlLst, 2);
            }

        }




    }
}
