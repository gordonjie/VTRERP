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

namespace JN.K3.YDL.App.ServicePlugIn.SCM.SaleOrder
{
    /// <summary>
    /// 销售订单审核插件
    /// </summary>
    [Description("销售订单审核插件")]
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
                DynamicObjectCollection dycEntitys = data["SaleOrderEntry"] as DynamicObjectCollection;

                foreach (var dycEntity in dycEntitys)
                {
                    string sql = string.Empty;
                    //   更新销售结余后台表 12月20日赵成杰             
                    string entityid = Convert.ToString(dycEntity["ID"]);
                    sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                set (FQTY)=(select  a.FQTY-c.FBASEUNITQTY
                from JN_T_SAL_ForecastBack a
                inner join T_SAL_ORDER b on a.FSALEORGID=b.FSALEORGID and a.FSALERID=b.FSALERID 
                and a.FSaleDeptId=b.FSALEDEPTID 
                inner join T_SAL_ORDERENTRY c on b.FID=c.FID and a.FMATERIALID=c.FMATERIALID and c.FEntryID={0}
                and a.FAUXPROPID=c.FAUXPROPID and c.FBaseUnitID=a.FUnitID
                inner join t_BD_Stock d on c.FSTOCKID_MX=d.FStockId
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

            //查找需要更新的销售预测结余后台表 存在出现同一张单出现表体出现相同物料时导致重复创建结余后台表，停用
           // DynamicObjectCollection dycSelectForecastBack = JNCommonServices.SelectForecastBack(this.Context, param, "A");

            //更新销售结余后台表 存在出现同一张单出现表体出现相同物料时导致重复创建结余后台表，停用
            //DynamicObjectCollection dycInsertForecastBack = UpdateForecastBack(dycSelectForecastBack, param);
            DynamicObjectCollection dycInsertForecastBack = null;
            //插入销售结余日志表
            DynamicObjectCollection dycInsertForecastLog = UpdateForecastLog(param);

            //调用插入方法
            JNCommonServices.UpdateForecastBackAndLog(this.Context, dycInsertForecastBack, dycInsertForecastLog);


        }

        //更新销售结余后台表 存在出现同一张单出现表体出现相同物料时导致重复创建结余后台表，停用
        private DynamicObjectCollection UpdateForecastBack(DynamicObjectCollection dycupdateForecastBack, SqlParam param)
        {

            //逐行计算更新金额
            long oldentityid = 0;
            decimal ForecastBackQTY = 0;
            string sql = "";
            foreach (var dydata in dycupdateForecastBack)
            {
                long newentityid = Convert.ToInt64(dydata["FEntryID"]);
                if (oldentityid != newentityid)
                {
                    oldentityid = newentityid;
                    ForecastBackQTY = Convert.ToDecimal(dydata["FBASEUNITQTY"]);
                }
                if (ForecastBackQTY > 0)
                {/*逐行加
                    if (ForecastBackQTY >= Convert.ToDecimal(dydata["FQTY"]))
                    {
                        dydata["FQTY"] = 0;
                        ForecastBackQTY = ForecastBackQTY + Convert.ToDecimal(dydata["FQTY"]);
                    }
                    else
                    {
                        dydata["FQTY"] = Convert.ToDecimal(dydata["FQTY"]) - ForecastBackQTY;
                        ForecastBackQTY = 0;
                    }*/
                    //加首行（避免重复加）
                    dydata["FQTY"] = Convert.ToDecimal(dydata["FQTY"]) + ForecastBackQTY;
                    ForecastBackQTY = 0;
                }


                sql = string.Format(@"Update JN_T_SAL_ForecastBack   
                set FQTY={0}
                where FID={1}", Convert.ToString(dydata["FQTY"]), Convert.ToString(dydata["ID"]));

                DBUtils.Execute(this.Context, sql);
            }
            /*
            string sql = string.Empty;

            sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                set (FQTY)=(select  a.FQTY-c.FBASEUNITQTY
                from JN_T_SAL_ForecastBack a
                inner join T_SAL_ORDER b on a.FSALEORGID=b.FSALEORGID and a.FSALERID=b.FSALERID 
                and a.FSaleDeptId=b.FSALEDEPTID  and a.FSaleGroupId=b.FSALEGROUPID
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                inner join T_SAL_ORDERENTRY c on b.FID=c.FID and a.FMATERIALID=c.FMATERIALID 
                and a.FAUXPROPID=c.FAUXPROPID and c.FBaseUnitID=a.FUnitID
                inner join t_BD_Stock d on c.FSTOCKID_MX=d.FStockId
                where a.FID=t0.FID and d.FMasterId in (100313,100328) )
                ");

            DBUtils.Execute(this.Context, sql, param);*/

            return null;

        }

        //插入销售结余日志表
        private DynamicObjectCollection UpdateForecastLog(SqlParam param)
        {
            string sql = string.Empty;

            sql = string.Format(@"select newid() as FBILLNO,t2.FAUXPROPID,tm.FUNITID,t2.FBASEUNITQTY as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FSALERID as FSALERID,t1.FSALEORGID,t1.FSALEDEPTID
                    ,t2.FMATERIALID,tm.FID as FFORECASTID,'A' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY+t2.FBASEUNITQTY as FBEFOREQTY,tm.FQTY as FAFTERQTY,'B' as FDIRECTION
                    from T_SAL_ORDER t1                  
                    inner join TABLE(fn_StrSplit(@FID,',',1)) tb on t1.Fid=tb.Fid
                    inner join T_SAL_ORDERENTRY t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FSALEORGID 
                    and tm.FSALERID=t1.FSALERID and tm.FMATERIALID=t2.FMATERIALID 
                    and tm.FUNITID=t2.FBASEUNITID and tm.FAUXPROPID=t2.FAUXPROPID
                    and tm.FSaleDeptId=t1.FSALEDEPTID 
                    inner join t_BD_Stock t3 on t2.FSTOCKID_MX=t3.FStockId
                    where t3.FMasterId in (100313,100328) 
                    ");

            return DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });
        }

    }
}
