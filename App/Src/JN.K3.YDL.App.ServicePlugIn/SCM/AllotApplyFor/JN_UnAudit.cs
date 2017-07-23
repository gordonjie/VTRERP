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

namespace JN.K3.YDL.App.ServicePlugIn.SCM.AllotApplyFor
{
    /// <summary>
    /// 调拨申请单反审核插件
    /// </summary>
    [Description("调拨申请单反审核插件")]
    public class JN_UnAudit : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            //e.FieldKeys.Add("FBillNo");
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
                    string sql = string.Empty;
                    //   更新销售结余后台表 12月20日赵成杰             
                    string entityid = Convert.ToString(dycEntity["ID"]);
                    sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                set (FQTY)=(select  a.FQTY+c.FBaseQty
                from JN_T_SAL_ForecastBack a
                inner join JN_YDL_SCM_AllotApplyFor b on a.FSALEORGID=b.FSALEORGID and a.FSALERID=b.FSALESMANID 
                and a.FSaleDeptId=b.FSALEDEPTID 
                inner join JN_YDL_SCM_AllotEntry c on b.FID=c.FID and a.FMATERIALID=c.FMATERIALID  and c.FEntryID={0}
                and a.FAUXPROPID=c.FAUXPROPID and c.FBaseUnitID=a.FUnitID
                inner join t_BD_Stock d on c.FOutStockID=d.FStockId
                where a.FID=t0.FID and d.FMasterId in (100313,100328) )
                ", entityid);

                    DBUtils.Execute(this.Context, sql);
                }
            }

            if (lstFids.Count() <= 0)
            {
                return;
            }

            SqlParam param = new SqlParam("@FID", KDDbType.udt_inttable, lstFids.ToArray());

            //更新销售结余后台表 存在出现同一张单出现表体出现相同物料时导致重复创建结余后台表，停用
            //DynamicObjectCollection dycInsertForecastBack = UpdateForecastBack(dycSelectForecastBack, param);
            DynamicObjectCollection dycInsertForecastBack = null;

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
                set (FQTY)=(select  a.FQTY+c.FBaseQty
                from JN_T_SAL_ForecastBack a
                inner join JN_YDL_SCM_AllotApplyFor b on a.FSALEORGID=b.FSALEORGID and a.FSALERID=b.FSALESMANID 
                and a.FSaleDeptId=b.FSALEDEPTID  
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                inner join JN_YDL_SCM_AllotEntry c on b.FID=c.FID and a.FMATERIALID=c.FMATERIALID 
                and a.FAUXPROPID=c.FAUXPROPID and c.FBaseUnitID=a.FUnitID
                inner join t_BD_Stock d on c.FOutStockID=d.FStockId
                where a.FID=t0.FID and d.FMasterId in (100313,100328) )
                ");

            DBUtils.Execute(this.Context, sql, param);

            return null;

        }

        //插入销售结余日志表
        private DynamicObjectCollection UpdateForecastLog(SqlParam param)
        {
            string sql = string.Empty;

            sql = string.Format(@"select newid() as FBILLNO,t2.FAUXPROPID,tm.FUNITID,t2.FBaseQty as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FSALESMANID as FSALERID,t1.FSALEORGID,t1.FSALEDEPTID
                    ,t2.FMATERIALID,tm.FID as FFORECASTID,'B' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY-t2.FBaseQty as FBEFOREQTY,tm.FQTY as FAFTERQTY,'A' as FDIRECTION
                    from JN_YDL_SCM_AllotApplyFor t1
                    inner join TABLE(fn_StrSplit(@FID,',',1)) tb on t1.Fid=tb.Fid
                    inner join JN_YDL_SCM_AllotEntry t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FSALEORGID 
                    and tm.FSALERID=t1.FSALESMANID and tm.FMATERIALID=t2.FMATERIALID 
                    and tm.FUNITID=t2.FBaseUnitID and tm.FAUXPROPID=t2.FAUXPROPID
                    and tm.FSaleDeptId=t1.FSALEDEPTID 
                    inner join t_BD_Stock t3 on t2.FOutStockID=t3.FStockId
                    where t3.FMasterId in (100313,100328) 
                    ");

            return DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });
        }
    }
}
