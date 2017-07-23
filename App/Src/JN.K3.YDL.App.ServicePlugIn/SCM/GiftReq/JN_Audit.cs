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

namespace JN.K3.YDL.App.ServicePlugIn.SCM.GiftReq
{
    /// <summary>
    /// 赠品申请单审核插件
    /// </summary>
    [Description("赠品申请单审核插件")]
    public class JN_Audit : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
        }

        /// <summary>
        /// 增加操作校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            JN_AuditValidator AduitValidator = new JN_AuditValidator();
            AduitValidator.EntityKey = "FBillHead";
            e.Validators.Add(AduitValidator);
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
            }

            if (lstFids.Count() <= 0)
            {
                return;
            }

            SqlParam param = new SqlParam("@FID", KDDbType.udt_inttable, lstFids.ToArray());

            //更新销售结余后台表
            DynamicObjectCollection dycInsertForecastBack = UpdateForecastBack(param);

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
                set (FQTY)=(select  a.FQTY-c.FBaseUnitQty
                from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_GiftReq b on a.FSALEORGID=b.FSALEORGID and a.FSALERID=b.FSALESMANID 
                and a.FSaleDeptId=b.FSALEDEPTID  and a.FSaleGroupId=b.FSALEGROUPID
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                inner join JN_T_SAL_GiftReqEntry c on b.FID=c.FID and a.FMATERIALID=c.FMATERIALID and a.FAUXPROPID=c.FAUXPROPID 
                inner join t_BD_Stock d on c.FStockId=d.FStockId
                where a.FID=t0.FID and d.FMasterId in (100313,100328) )
                ");

            DBUtils.Execute(this.Context, sql, param);

            return null;

        }

        //插入销售结余日志表
        private DynamicObjectCollection UpdateForecastLog(SqlParam param)
        {
            string sql = string.Empty;

            sql = string.Format(@"select newid() as FBILLNO,t2.FAUXPROPID,tm.FUNITID,t2.FBaseUnitQty as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FSALESMANID as FSALERID,t1.FSALEORGID,t1.FSALEDEPTID
                    ,t1.FSaleGroupId,t2.FMATERIALID,tm.FID as FFORECASTID,'C' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY+t2.FBaseUnitQty as FBEFOREQTY,tm.FQTY as FAFTERQTY,'B' as FDIRECTION
                    from JN_T_SAL_GiftReq t1
                    inner join TABLE(fn_StrSplit(@FID,',',1)) tb on t1.Fid=tb.Fid
                    inner join JN_T_SAL_GiftReqEntry t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FSALEORGID 
                    and tm.FSALERID=t1.FSALESMANID and tm.FMATERIALID=t2.FMATERIALID 
                    and tm.FUNITID=t2.FBaseUnitID and tm.FAUXPROPID=t2.FAUXPROPID
                    and tm.FSaleDeptId=t1.FSALEDEPTID and tm.FSaleGroupId=t1.FSALEGROUPID
                    inner join t_BD_Stock d on t2.FStockId=d.FStockId
                    where d.FMasterId in (100313,100328) 
                    ");

            return DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });
        }
    }
}
