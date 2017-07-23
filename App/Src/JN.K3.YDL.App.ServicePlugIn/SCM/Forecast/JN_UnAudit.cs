using JN.K3.YDL.App.ServicePlugIn.Common;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.Forecast
{
    /// <summary>
    /// 销售预测单反审核插件
    /// </summary>
    [Description("销售预测单反审核插件")]
    public class JN_UnAudit : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            //e.FieldKeys.Add("FBillNo");
            e.FieldKeys.Add("FJNSubDate");
            e.FieldKeys.Add("FJNMaterialId");
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
                DynamicObjectCollection ForecastEntitys = data["SAL_ForecastEntity"] as DynamicObjectCollection;
                foreach (var ForecastEntity in ForecastEntitys)
                {
                    string entityid = Convert.ToString(ForecastEntity["ID"]);
                    string sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                set (FQTY)=(select a.FQTY-c.FBaseUnitQty
                from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_Forecast b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId  
                inner join JN_T_SAL_ForecastEntity c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID  and c.FEntryID={0}
                and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FBaseUnitID 
                where a.FID=t0.FID )", entityid);
                    DBUtils.Execute(this.Context, sql);
                }
            }

            if (lstFids.Count() <= 0)
            {
                return;
            }

            SqlParam param = new SqlParam("@FID", KDDbType.udt_inttable, lstFids.ToArray());

            //查找需要更新的销售预测结余后台表,存在出现同一张单出现表体出现相同物料时导致重复创建结余后台表，停用
            //DynamicObjectCollection dycSelectForecastBack = JNCommonServices.SelectForecastBack(this.Context, param, "D");
            DynamicObjectCollection dycInsertForecastBack = null;
            //更新销售结余后台表存在出现同一张单出现表体出现相同物料时导致重复创建结余后台表，停用
           // DynamicObjectCollection dycInsertForecastBack = UpdateForecastBack(dycSelectForecastBack, param);

            //插入销售结余日志表
            DynamicObjectCollection dycInsertForecastLog = UpdateForecastLog(param);

            //调用插入方法
            JNCommonServices.UpdateForecastBackAndLog(this.Context, dycInsertForecastBack, dycInsertForecastLog);

        }



        //插入销售结余日志表
        private DynamicObjectCollection UpdateForecastLog(SqlParam param)
        {
            string sql = string.Empty;

            sql = string.Format(@"select newid() as FBILLNO,t2.FJNAUXPROP as FAUXPROPID,tm.FUNITID,t2.FBaseUnitQty as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FJNSALERID as FSALERID,t1.FJNSALEORGID as FSALEORGID,t1.FJNSaleDeptId as FSaleDeptId
                    ,t1.FJNSaleGroupId as FSaleGroupId,t2.FJNMATERIALID as FMATERIALID,tm.FID as FFORECASTID,'D' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY+t2.FBaseUnitQty as FBEFOREQTY,tm.FQTY as FAFTERQTY,'B' as FDIRECTION
                    from JN_T_SAL_Forecast t1
                    inner join TABLE(fn_StrSplit(@FID,',',1)) tb on t1.Fid=tb.Fid
                    inner join JN_T_SAL_ForecastEntity t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FJNSALEORGID 
                    and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                    and tm.FUNITID=t2.FBaseUnitID and tm.FAUXPROPID=t2.FJNAUXPROP
                    and tm.FSaleDeptId=t1.FJNSaleDeptId 
                    ");

            return DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });
        }

        #region //old
        //        /// <summary>
        //        /// 更新销售预测单结余表后台表
        //        /// </summary>
        //        private void UpdateForecastBack(List<long> lstFids)
        //        {
        //            IDBService dbservice = ServiceFactory.GetDBService(this.Context);

        //            string sql = string.Empty;

        //            //需要更新
        //            sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
        //            set FQTY=(select a.FQTY-(c.FJNFORECASTQTY * e.FConvertNumerator / e.FConvertDenominator)
        //            from JN_T_SAL_ForecastBack a
        //            inner join JN_T_SAL_Forecast b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
        //            and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
        //            inner join JN_T_SAL_ForecastEntity c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID and a.FAUXPROPID=c.FJNAUXPROP 
        //            inner join T_BD_Material d  on a.FMATERIALID=d.FMATERIALID
        //            inner join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=c.FJNUnitID and e.FDESTUNITID=a.FUnitID
        //                                  where a.FID=t0.FID and b.Fid in ({0}))", string.Join(",", lstFids));


        //            int results = DBUtils.Execute(this.Context, sql);


        //            // 插入销售预测单结余表日志表
        //            #region
        //            if (results <= 0)
        //            {
        //                return;
        //            }

        //            sql = string.Format(@"select newid() as FBILLNO,t2.FJNAUXPROP,tm.FUNITID,t2.FJNFORECASTQTY,getdate() as FADJUSTDATE,
        //            t1.FJNSALERID,t1.FJNSALEORGID,t1.FJNSaleDeptId,t1.FJNSaleGroupId,t2.FJNMATERIALID
        //            ,tm.FID as FFORECASTID,'D' as FBILLTYPE,t1.FID as FBILLID,
        //            t1.FBILLNO,t2.FENTRYID,tm.FQTY+t2.FJNFORECASTQTY as FBEFOREQTY,tm.FQTY as FAFTERQTY
        //            from JN_T_SAL_Forecast t1
        //            inner join JN_T_SAL_ForecastEntity t2 on t1.FID=t2.FID 
        //            inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FJNSALEORGID 
        //            and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
        //            and tm.FUNITID=t2.FJNUnitID and tm.FAUXPROPID=t2.FJNAUXPROP
        //            and tm.FSaleDeptId=t1.FJNSaleDeptId and tm.FSaleGroupId=t1.FJNSaleGroupId
        //            where t1.Fid in ({0})
        //            union all
        //            select newid() as FBILLNO,t2.FJNAUXPROP,tm.FUNITID,t2.FJNFORECASTQTY,getdate() as FADJUSTDATE,
        //            t1.FJNSALERID,t1.FJNSALEORGID,t1.FJNSaleDeptId,t1.FJNSaleGroupId,t2.FJNMATERIALID
        //            ,tm.FID as FFORECASTID,'D' as FBILLTYPE,t1.FID as FBILLID,
        //            t1.FBILLNO,t2.FENTRYID,tm.FQTY+(t2.FJNFORECASTQTY* e.FConvertNumerator / e.FConvertDenominator) as FBEFOREQTY,tm.FQTY as FAFTERQTY
        //            from JN_T_SAL_Forecast t1
        //            inner join JN_T_SAL_ForecastEntity t2 on t1.FID=t2.FID 
        //            inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FJNSALEORGID 
        //            and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
        //            and tm.FAUXPROPID=t2.FJNAUXPROP  and tm.FSaleDeptId=t1.FJNSaleDeptId and tm.FSaleGroupId=t1.FJNSaleGroupId
        //            inner join T_BD_Material d  on t2.FJNMATERIALID=d.FMATERIALID
        //            inner join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=t2.FJNUnitID and e.FDESTUNITID=tm.FUnitID
        //            where t1.Fid in ({0}) and tm.FUNITID<>t2.FJNUnitID", string.Join(",", lstFids));

        //            DynamicObjectCollection dycInsert = DBUtils.ExecuteDynamicObject(this.Context, sql);

        //            List<string> lstsql = new List<string>();

        //            if (dycInsert != null && dycInsert.Count() > 0)
        //            {
        //                long[] ids = dbservice.GetSequenceInt64(this.Context, "JN_T_SAL_ForecastLog", dycInsert.Count()).ToArray();

        //                int index = 0;

        //                foreach (DynamicObject item in dycInsert)
        //                {
        //                    sql = string.Format(@"INSERT INTO JN_T_SAL_ForecastLog (FID,FBILLNO,FAUXPROPID,FUNITID,FADJUSTQTY,FADJUSTDATE
        //                                        ,FSALERID,FSALEORGID,FMATERIALID,FFORECASTID,FBILLTYPE,FBILLID,FSRCBILLNO
        //                                        ,FENTRYID,FBEFOREQTY,FAFTERQTY,FADJUSTID,FDIRECTION,FSaleDeptId,FSaleGroupId) 
        //                                        VALUES ({0},'{1}',{2},{3},{4},'{5}',{6},{7},{8},{9},'{10}',{11},'{12}',{13},{14},{15},{16},'{17}',{18},{19})"
        //                                      , ids[index], item["FBILLNO"], item["FJNAUXPROP"], item["FUNITID"], item["FJNFORECASTQTY"], item["FADJUSTDATE"]
        //                                      , item["FJNSALERID"], item["FJNSALEORGID"], item["FJNMATERIALID"], item["FFORECASTID"], item["FBILLTYPE"]
        //                                      , item["FBILLID"], item["FBILLNO"], item["FENTRYID"], item["FBEFOREQTY"], item["FAFTERQTY"], this.Context.UserId, "B", item["FJNSaleDeptId"], item["FJNSaleGroupId"]);
        //                    lstsql.Add(sql);
        //                    index++;
        //                }

        //                if (lstsql.Count() > 0)
        //                {
        //                    results = DBUtils.ExecuteBatch(this.Context, lstsql, lstsql.Count());
        //                }
        //            }

        //            #endregion

        //        }
        #endregion
    }
}
