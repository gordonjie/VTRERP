using JN.K3.YDL.App.ServicePlugIn.Common;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.ForecastChange
{
    /// <summary>
    /// 销售预测单变更单反审核插件
    /// </summary>
    [Description("销售预测单变更单反审核插件")]
    public class JN_UnAudit : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            //e.FieldKeys.Add("FBillNo");
        }


        /// <summary>
        /// 增加操作校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            JNUnAuditValidator unAduitValidator = new JNUnAuditValidator();
            unAduitValidator.EntityKey = "FBillHead";
            e.Validators.Add(unAduitValidator);
        }

        public override void EndOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);

            if (e.DataEntitys == null || e.DataEntitys.Count() <= 0)
            {
                return;
            }

            List<long> lstFids = new List<long>();

            foreach (DynamicObject data in e.DataEntitys)
            {
                lstFids.Add(Convert.ToInt64(data["ID"]));
                DynamicObjectCollection dycEntitys = data["FEntity"] as DynamicObjectCollection;
                foreach (var dycEntity in dycEntitys)
                {
                    //   更新销售结余后台表 12月20日赵成杰             
                    string entityid = Convert.ToString(dycEntity["ID"]);
                    string sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                 set (FQTY)=(select case when b.FDirection='A' then a.FQTY-c.FJNBaseUnitQty
                                       else a.FQTY+c.FJNBaseUnitQty end
                    from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_ForecastChange b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId  
                inner join JN_T_SAL_ForecastChangeEntry c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID  and c.FEntryID={0}
                and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FJNBASEUNITID 
                where a.FID=t0.FID )", entityid);
                    DBUtils.Execute(this.Context, sql);
                }
            }

            if (lstFids.Count() <= 0)
            {
                return;
            }

            SqlParam param = new SqlParam("@FID", KDDbType.udt_inttable, lstFids.ToArray());

            //更新销售结余后台表
            DynamicObjectCollection dycInsertForecastBack = null;
            //DynamicObjectCollection dycInsertForecastBack = UpdateForecastBack(param);

            //插入销售结余日志表
            DynamicObjectCollection dycInsertForecastLog = UpdateForecastLog(param);

            //调用插入方法
            JNCommonServices.UpdateForecastBackAndLog(this.Context, dycInsertForecastBack, dycInsertForecastLog);

        }

        //更新销售结余后台表
        private DynamicObjectCollection UpdateForecastBack(SqlParam param)
        {
            string sql = string.Empty;

            sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                    set (FQTY)=(select case when b.FDirection='A' then a.FQTY-c.FJNBaseUnitQty
                                       else a.FQTY+c.FJNBaseUnitQty end
                    from JN_T_SAL_ForecastBack a
                    inner join JN_T_SAL_ForecastChange b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                    and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
                    inner join JN_T_SAL_ForecastChangeEntry c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID 
                    and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FJNBaseUnitID 
                    inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                    where a.FID=t0.FID  ) ");

            DBUtils.Execute(this.Context, sql, param);

            return null;

        }

        //插入销售结余日志表
        private DynamicObjectCollection UpdateForecastLog(SqlParam param)
        {
            string sql = string.Empty;

            sql = string.Format(@"select newid() as FBILLNO,t2.FJNAUXPROP as FAUXPROPID,tm.FUNITID,(-1*t2.FJNBaseUnitQty) as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FJNSALERID as FSALERID,t1.FJNSALEORGID as FSALEORGID,t1.FJNSaleDeptId as FSaleDeptId
                    ,t2.FJNMATERIALID as FMATERIALID,tm.FID as FFORECASTID,'E' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,(case when t1.FDirection='A' then tm.FQTY+t2.FJNBaseUnitQty 
                                                          else tm.FQTY-t2.FJNBaseUnitQty end )as FBEFOREQTY,tm.FQTY as FAFTERQTY
                    ,(case when t1.FDirection='A' then 'A' else 'B' end) as FDirection 
                    from JN_T_SAL_ForecastChange t1
                    inner join TABLE(fn_StrSplit(@FID,',',1)) tb on t1.Fid=tb.Fid
                    inner join JN_T_SAL_ForecastChangeEntry t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FJNSALEORGID 
                    and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                    and tm.FUNITID=t2.FJNBaseUnitID and tm.FAUXPROPID=t2.FJNAUXPROP
                    and tm.FSaleDeptId=t1.FJNSaleDeptId 
                    ");

            return DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });
        }
    }
}
