using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.Common
{
    /// <summary>
    /// 服务端公用服务插件
    /// </summary>
    [Description("服务端公用服务插件")]
    public class JNCommonServices
    {


        /// <summary>
        /// 插入销售预测单结余表后台表和日志表
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="dycInsertForecastBack">销售结余后台表</param>
        /// <param name="dycInsertForecastLog">销售结余日志表</param>
        /// <returns></returns>
        public static int UpdateForecastBackAndLog(Context ctx, DynamicObjectCollection dycInsertForecastBack, DynamicObjectCollection dycInsertForecastLog)
        {
            IDBService dbservice = ServiceFactory.GetDBService(ctx);

            string sql = string.Empty;
            List<DynamicObject> insertcols = new List<DynamicObject>();
            List<DynamicObject> updatetcols = new List<DynamicObject>();

            List<string> lstsql = new List<string>();
            
            int results = 0;

            if (dycInsertForecastBack != null && dycInsertForecastBack.Count() > 0)
            {
                long[] ids = dbservice.GetSequenceInt64(ctx, "JN_T_SAL_ForecastBack", dycInsertForecastBack.Count()).ToArray();

                int index = 0;

                foreach (DynamicObject item in dycInsertForecastBack)
                {


                    if (insertcols.Any(X => X["FSALEORGID"].ToString() == item["FSALEORGID"].ToString() && X["FSALERID"].ToString() == item["FSALERID"].ToString() && X["FSaleDeptId"].ToString() == item["FSaleDeptId"].ToString()
                        && X["FMATERIALID"].ToString() == item["FMATERIALID"].ToString() && X["FUNITID"].ToString() == item["FUNITID"].ToString() && X["FAUXPROPID"].ToString() == item["FAUXPROPID"].ToString()) == false)
                    {
                        insertcols.Add(item);

                        sql = string.Format(@"/*dialect*/INSERT INTO JN_T_SAL_ForecastBack (FID,FSALEORGID,FSALERID,FSaleDeptId,FBILLNO,FMATERIALID,FQTY,FUNITID,FAUXPROPID,FDATE,FSaleGroupId) 
                       VALUES ({0},{1},{2},{3},'{4}',{5},{6},{7},{8},'{9}',0)"
                           , ids[index], item["FSALEORGID"], item["FSALERID"], item["FSaleDeptId"]
                           , item["FBILLNO"], item["FMATERIALID"], item["FQTY"], item["FUNITID"]
                           , item["FAUXPROPID"], item["FDATE"]);
                        lstsql.Add(sql);
                        index++;
                    }
                    else
                    {
                        updatetcols.Add(item);

                    }
                    
                }
                
            }
            
            if (lstsql.Count() > 0)
            {
                results = DBUtils.ExecuteBatch(ctx, lstsql, lstsql.Count());
            }

            foreach(DynamicObject item in updatetcols)
            {
                        string updatesql = string.Format(@"/*dialect*/Update JN_T_SAL_ForecastBack   
                        set FQTY=FQTY+{0} where FSALEORGID={1} and FSALERID={2} and FSaleDeptId={3} and 
                        FMATERIALID={4} and FUNITID={5} and FAUXPROPID={6}",
                        item["FQTY"], item["FSALEORGID"], item["FSALERID"], item["FSaleDeptId"]
                       , item["FMATERIALID"], item["FUNITID"]
                       , item["FAUXPROPID"]);
                        results = DBUtils.Execute(ctx, updatesql);
            }
            List<string> lstLogsql = new List<string>();

            if (dycInsertForecastLog != null && dycInsertForecastLog.Count() > 0)
            {
                long[] ids = dbservice.GetSequenceInt64(ctx, "JN_T_SAL_ForecastLog", dycInsertForecastLog.Count()).ToArray();

                int index = 0;

                foreach (DynamicObject item in dycInsertForecastLog)
                {

                    sql = string.Format(@"/*dialect*/INSERT INTO JN_T_SAL_ForecastLog (FID,FBILLNO,FAUXPROPID,FUNITID,FADJUSTQTY,FADJUSTDATE
                                        ,FSALERID,FSALEORGID,FMATERIALID,FFORECASTID,FBILLTYPE,FBILLID,FSRCBILLNO
                                        ,FENTRYID,FBEFOREQTY,FAFTERQTY,FADJUSTID,FDIRECTION,FSaleDeptId,FSALEGROUPID) 
                                        VALUES ({0},'{1}',{2},{3},{4},'{5}',{6},{7},{8},{9},'{10}',{11},'{12}',{13},{14},{15},{16},'{17}',{18},0)"
                                      , ids[index], item["FBILLNO"], item["FAUXPROPID"], item["FUNITID"], item["FADJUSTQTY"], item["FADJUSTDATE"]
                                      , item["FSALERID"], item["FSALEORGID"], item["FMATERIALID"], item["FFORECASTID"], item["FBILLTYPE"]
                                      , item["FBILLID"], item["FSRCBILLNO"], item["FENTRYID"], item["FBEFOREQTY"], item["FAFTERQTY"], ctx.UserId
                                      , item["FDIRECTION"], item["FSaleDeptId"]);


                    lstLogsql.Add(sql);
                    
                    index++;
                }
            }

            
            if (lstLogsql.Count() > 0)
            {
                results = DBUtils.ExecuteBatch(ctx, lstLogsql, lstLogsql.Count());
            }

            return results;
        }

  

        /// <summary>
        /// 检查销售预测单结余表后台表是否存在
        /// sType: A 销售订单  B 调拨申请单  C 赠品申请单  D 销售预测单 E  销售预测变更单
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="dycInsertForecastBack">销售结余后台表</param>
        /// <param name="dycInsertForecastLog">销售结余日志表</param>
        /// <returns></returns>
        /// 
        public static DynamicObjectCollection SelectForecastBack(Context ctx, SqlParam param, string sType)
        {
            string sql = string.Empty;
            switch (sType)
            {

                #region//销售订单
                case "A":

                    sql = string.Format(@"select  a.FID as ID,a.FQTY,a.FAUXPROPID,a.FUNITID,a.FSALERID,a.FMATERIALID,a.FJNSUBDATE,a.FSALEORGID,b.fid
                from JN_T_SAL_ForecastBack a
                inner join T_SAL_ORDER b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
                inner join T_SAL_ORDERENTRY c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID 
                and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FBaseUnitID
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                order by ID,FJNSUBDATE");
                    break;
                #endregion
                #region//调拨申请单
                case "B":
                    sql = string.Format(@"select  a.FID as ID,a.FQTY,a.FAUXPROPID,a.FUNITID,a.FSALERID,a.FMATERIALID,a.FJNSUBDATE,a.FSALEORGID,b.fid
                from JN_T_SAL_ForecastBack a
                inner join JN_YDL_SCM_AllotApplyFor b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
                inner join T_SAL_ORDERENTRY c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID 
                and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FBaseUnitID
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                order by ID,FJNSUBDATE");
                    break;
                #endregion
                #region//赠品申请单
                case "C":
                    sql = string.Format(@"select  a.FID as ID,a.FQTY,a.FAUXPROPID,a.FUNITID,a.FSALERID,a.FMATERIALID,a.FJNSUBDATE,a.FSALEORGID,b.fid
                from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_GiftReq b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
                inner join JN_T_SAL_GiftReqEntry c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID 
                and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FBaseUnitID
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                order by ID,FJNSUBDATE");
                    break;

                #endregion
                #region //销售预测单
                case "D":
                    sql = string.Format(@"select  a.FID as ID,a.FQTY,a.FAUXPROPID,a.FUNITID,a.FSALERID,a.FMATERIALID,a.FJNSUBDATE,a.FSALEORGID,b.FID,c.FEntryID,
                (case when  c.FBASEUNITID<>a.FUnitID then (c.FBASEUNITQTY * e.FConvertNumerator / e.FConvertDenominator)
                                  else c.FBASEUNITQTY end  )as  FBASEUNITQTY
                from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_Forecast b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
                inner join JN_T_SAL_ForecastEntity c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID 
                and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FBaseUnitID
                inner join T_BD_Material d  on a.FMATERIALID=d.FMATERIALID
                left join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=c.FBASEUNITID and e.FDESTUNITID=a.FUnitID
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                order by ID,FJNSUBDATE");
                    break;
                #endregion

                #region //销售预测变更单
                case "E":
                    sql = string.Format(@"select  a.FID as ID,a.FQTY,a.FAUXPROPID,a.FUNITID,a.FSALERID,a.FMATERIALID,a.FJNSUBDATE,a.FSALEORGID,b.fid
                from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_ForecastChange b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
                inner join JN_T_SAL_ForecastChangeEntry c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID 
                and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FBaseUnitID
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                order by ID,FJNSUBDATE");
                    break;
                #endregion
            }
            return DBUtils.ExecuteDynamicObject(ctx, sql, null, null, CommandType.Text, new SqlParam[] { param });
        }


        /// <summary>
        /// 更新销售预测单结余表后台表
        /// sType: A 销售订单  B 调拨申请单  C 赠品申请单  D 销售预测单 E  销售预测变更单
        /// IsAduit:是否审核
        /// </summary>
        public static int AuditUpdateForecastBack(Context ctx, List<long> lstFids, string sType, bool IsAduit)
        {
            IDBService dbservice = ServiceFactory.GetDBService(ctx);

            string sql = string.Empty;

            List<string> lstsql = new List<string>();

            DynamicObjectCollection dycInsert = null;

            int results = 0;

            #region //需要更新的后台表信息

            switch (sType)
            {

                #region//销售订单
                case "A":
                    sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                set (FQTY)=(select  case when  c.FBASEUNITID<>a.FUnitID then (a.FQTY{1}(c.FBASEUNITQTY * e.FConvertNumerator / e.FConvertDenominator))
                                  else (a.FQTY{1}c.FBASEUNITQTY) end 
                from JN_T_SAL_ForecastBack a
                inner join T_SAL_ORDER b on a.FSALEORGID=b.FSALEORGID and a.FSALERID=b.FSALERID 
                and a.FSaleDeptId=b.FSALEDEPTID  and a.FSaleGroupId=b.FSALEGROUPID
                inner join T_SAL_ORDERENTRY c on b.FID=c.FID and a.FMATERIALID=c.FMATERIALID and a.FAUXPROPID=c.FAUXPROPID 
                inner join T_BD_Material d  on a.FMATERIALID=d.FMATERIALID
                left join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=c.FBASEUNITID and e.FDESTUNITID=a.FUnitID
                where a.FID=t0.FID  and b.Fid in ({0}))
                ", string.Join(",", lstFids), IsAduit ? "-" : "+");
                    break;
                #endregion
                #region//调拨申请单
                case "B":
                    sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                set (FQTY)=(select  case when  c.FUNITID<>a.FUnitID then (a.FQTY{1}(c.FJNAPPROVE * e.FConvertNumerator / e.FConvertDenominator))
                                  else (a.FQTY{1}c.FJNAPPROVE) end 
                from JN_T_SAL_ForecastBack a
                inner join JN_YDL_SCM_AllotApplyFor b on a.FSALEORGID=b.FSALEORGID and a.FSALERID=b.FSALESMANID 
                and a.FSaleDeptId=b.FSALEDEPTID  and a.FSaleGroupId=b.FSALEGROUPID
                inner join JN_YDL_SCM_AllotEntry c on b.FID=c.FID and a.FMATERIALID=c.FMATERIALID and a.FAUXPROPID=c.FAUXPROPID 
                inner join T_BD_Material d  on a.FMATERIALID=d.FMATERIALID
                left join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=c.FUNITID and e.FDESTUNITID=a.FUnitID
                where a.FID=t0.FID  and b.Fid in ({0}))
                ", string.Join(",", lstFids), IsAduit ? "-" : "+");
                    break;
                #endregion
                #region//赠品申请单
                case "C":
                    sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                set (FQTY)=(select  case when  c.FBaseUnitID<>a.FUnitID then (a.FQTY{1}(c.FBaseUnitQty * e.FConvertNumerator / e.FConvertDenominator))
                                  else (a.FQTY{1}c.FBaseUnitQty) end 
                from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_GiftReq b on a.FSALEORGID=b.FSALEORGID and a.FSALERID=b.FSALESMANID 
                and a.FSaleDeptId=b.FSALEDEPTID  and a.FSaleGroupId=b.FSALEGROUPID
                inner join JN_T_SAL_GiftReqEntry c on b.FID=c.FID and a.FMATERIALID=c.FMATERIALID and a.FAUXPROPID=c.FAUXPROPID 
                inner join T_BD_Material d  on a.FMATERIALID=d.FMATERIALID
                left join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=c.FBaseUnitQty and e.FDESTUNITID=a.FUnitID
                where a.FID=t0.FID  and b.Fid in ({0}))
                ", string.Join(",", lstFids), IsAduit ? "-" : "+");
                    break;
                #endregion
                #region //销售预测单
                case "D":
                    sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                set (FQTY)=(select  case when  c.FJNUnitID<>a.FUnitID then (a.FQTY{1}(c.FJNFORECASTQTY * e.FConvertNumerator / e.FConvertDenominator))
                                  else (a.FQTY{1}c.FJNFORECASTQTY) end 
                from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_Forecast b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
                inner join JN_T_SAL_ForecastEntity c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID and a.FAUXPROPID=c.FJNAUXPROP 
                inner join T_BD_Material d  on a.FMATERIALID=d.FMATERIALID
                left join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=c.FJNUnitID and e.FDESTUNITID=a.FUnitID
                where a.FID=t0.FID  and b.Fid in ({0}))
                ", string.Join(",", lstFids), IsAduit ? "+" : "-");
                    break;
                #endregion
                #region //销售预测变更单
                case "E":
                    sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                    set (FQTY)=(select case when c.FJNUnitID<>a.FUnitID then 
                                     (case when b.FDirection='A' then a.FQTY{1}(c.FJNFORECASTQTY * e.FConvertNumerator / e.FConvertDenominator)
                                     else a.FQTY{2}(c.FJNFORECASTQTY * e.FConvertNumerator / e.FConvertDenominator) end)
                                     else (case when b.FDirection='A' then a.FQTY{1}c.FJNFORECASTQTY
                                     else a.FQTY{2}c.FJNFORECASTQTY end) end  
                    from JN_T_SAL_ForecastBack a
                    inner join JN_T_SAL_ForecastChange b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                    and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
                    inner join JN_T_SAL_ForecastChangeEntry c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID and a.FAUXPROPID=c.FJNAUXPROP 
                    inner join T_BD_Material d  on a.FMATERIALID=d.FMATERIALID
                    left join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=c.FJNUnitID and e.FDESTUNITID=a.FUnitID
                    where a.FID=t0.FID and b.Fid in ({0}))
                    ", string.Join(",", lstFids), IsAduit ? "+" : "-", IsAduit ? "-" : "+");
                    break;
                #endregion

            }

            lstsql.Add(sql);

            #region //销售预测单,销售预测变更单审核才需要插入后台表
            if (IsAduit && (sType == "D" || sType == "E"))
            {
                if (sType == "D")
                {
                    sql = string.Format(@"select t1.FJNSALEORGID,t1.FJNSALERID,t1.FJNSaleDeptId,t1.FJNSaleGroupId 
                        ,newid() as FBILLNO,t2.FJNMATERIALID,t2.FJNFORECASTQTY,t2.FJNUnitID,t2.FJNAUXPROP,getdate() as FDATE
                        from JN_T_SAL_Forecast t1
                        inner join JN_T_SAL_ForecastEntity t2 on t1.FID=t2.FID
                        where not exists(select 1  from JN_T_SAL_ForecastBack tm where tm.FSALEORGID=t1.FJNSALEORGID 
                                        and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                                        and tm.FAUXPROPID=t2.FJNAUXPROP and tm.FSaleDeptId=t1.FJNSaleDeptId and tm.FSaleGroupId=t1.FJNSaleGroupId)
	                    and  t1.Fid in ({0})", string.Join(",", lstFids));
                }
                else
                {
                    sql = string.Format(@"select t1.FJNSALEORGID,t1.FJNSALERID,t1.FJNSaleDeptId,t1.FJNSaleGroupId 
                        ,newid() as FBILLNO,t2.FJNMATERIALID,t2.FJNFORECASTQTY,t2.FJNUnitID,t2.FJNAUXPROP,getdate() as FDATE
                        from JN_T_SAL_ForecastChange t1
                        inner join JN_T_SAL_ForecastChangeEntry t2 on t1.FID=t2.FID
                        where not exists(select 1  from JN_T_SAL_ForecastBack tm where tm.FSALEORGID=t1.FJNSALEORGID 
                                        and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                                        and tm.FAUXPROPID=t2.FJNAUXPROP and tm.FSaleDeptId=t1.FJNSaleDeptId and tm.FSaleGroupId=t1.FJNSaleGroupId)
	                    and  t1.FDirection='A' and t1.Fid in ({0})", string.Join(",", lstFids));
                }

                dycInsert = DBUtils.ExecuteDynamicObject(ctx, sql);

                if (dycInsert != null && dycInsert.Count() > 0)
                {
                    long[] ids = dbservice.GetSequenceInt64(ctx, "JN_T_SAL_ForecastBack", dycInsert.Count()).ToArray();

                    int index = 0;

                    foreach (DynamicObject item in dycInsert)
                    {
                        sql = string.Format(@"INSERT INTO JN_T_SAL_ForecastBack (FID,FSALEORGID,FSALERID,FSaleDeptId,FSaleGroupId,FBILLNO,FMATERIALID,FQTY,FUNITID,FAUXPROPID,FDATE) 
                       VALUES ({0},{1},{2},{3},{4},'{5}',{6},{7},{8},{9},'{10}')"
                           , ids[index], item["FSALEORGID"], item["FSALERID"], item["FSaleDeptId"], item["FSaleGroupId"]
                           , item["FBILLNO"], item["FMATERIALID"], item["FQTY"], item["FUNITID"]
                           , item["FAUXPROPID"], item["FDATE"]);
                        lstsql.Add(sql);
                        index++;
                    }
                }
            }
            #endregion

            if (lstsql.Count() > 0)
            {
                results = DBUtils.ExecuteBatch(ctx, lstsql, lstsql.Count());
            }

            #endregion

            #region // 插入销售预测单结余表日志表

            if (results <= 0)
            {
                return results;
            }

            switch (sType)
            {
                #region //销售订单
                case "A":
                    sql = string.Format(@"select newid() as FBILLNO,t2.FAUXPROPID,tm.FUNITID,t2.FBASEUNITQTY as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FSALERID,t1.FSALEORGID,t1.FSALEDEPTID
                    ,t1.FSaleGroupId,t2.FMATERIALID,tm.FID as FFORECASTID,'{1}' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY{3}t2.FBASEUNITQTY as FBEFOREQTY,tm.FQTY as FAFTERQTY,'{2}' as FDIRECTION
                    from T_SAL_ORDER t1
                    inner join T_SAL_ORDERENTRY t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FSALEORGID 
                    and tm.FSALERID=t1.FSALERID and tm.FMATERIALID=t2.FMATERIALID 
                    and tm.FUNITID=t2.FBASEUNITID and tm.FAUXPROPID=t2.FAUXPROPID
                    and tm.FSaleDeptId=t1.FSALEDEPTID and tm.FSaleGroupId=t1.FSALEGROUPID
                    where t1.Fid in ({0})
                    union all
                    select newid() as FBILLNO,t2.FAUXPROPID,tm.FUNITID,(t2.FBASEUNITQTY* e.FConvertNumerator / e.FConvertDenominator) as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FSALERID,t1.FSALEORGID,t1.FSALEDEPTID
                    ,t1.FSaleGroupId,t2.FMATERIALID,tm.FID as FFORECASTID,'{1}' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY{3}(t2.FBASEUNITQTY* e.FConvertNumerator / e.FConvertDenominator) as FBEFOREQTY,tm.FQTY as FAFTERQTY
                    ,'{2}' as FDIRECTION
                    from T_SAL_ORDER t1
                    inner join T_SAL_ORDERENTRY t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FSALEORGID 
                    and tm.FSALERID=t1.FSALERID and tm.FMATERIALID=t2.FMATERIALID 
                    and tm.FAUXPROPID=t2.FAUXPROPID  and tm.FSaleDeptId=t1.FSALEDEPTID and tm.FSaleGroupId=t1.FSALEGROUPID
                    inner join T_BD_Material d  on t2.FMATERIALID=d.FMATERIALID
                    inner join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=t2.FBASEUNITID and e.FDESTUNITID=tm.FUnitID
                    where t1.Fid in ({0}) and tm.FUNITID<>t2.FBASEUNITID", string.Join(",", lstFids), sType, IsAduit ? "B" : "A", IsAduit ? "+" : "-");
                    break;
                #endregion
                #region //调拨申请单
                case "B":
                    sql = string.Format(@"select newid() as FBILLNO,t2.FAUXPROPID,tm.FUNITID,t2.FJNAPPROVE as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FSALESMANID as FSALERID,t1.FSALEORGID,t1.FSALEDEPTID
                    ,t1.FSaleGroupId,t2.FMATERIALID,tm.FID as FFORECASTID,'{1}' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY{3}t2.FJNAPPROVE as FBEFOREQTY,tm.FQTY as FAFTERQTY,'{2}' as FDIRECTION
                    from JN_YDL_SCM_AllotApplyFor t1
                    inner join JN_YDL_SCM_AllotEntry t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FSALEORGID 
                    and tm.FSALERID=t1.FSALESMANID and tm.FMATERIALID=t2.FMATERIALID 
                    and tm.FUNITID=t2.FUNITID and tm.FAUXPROPID=t2.FAUXPROPID
                    and tm.FSaleDeptId=t1.FSALEDEPTID and tm.FSaleGroupId=t1.FSALEGROUPID
                    where t1.Fid in ({0})
                    union all
                    select newid() as FBILLNO,t2.FAUXPROPID,tm.FUNITID,(t2.FJNAPPROVE* e.FConvertNumerator / e.FConvertDenominator)  as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FSALESMANID as FSALERID,t1.FSALEORGID,t1.FSALEDEPTID
                    ,t1.FSaleGroupId,t2.FMATERIALID,tm.FID as FFORECASTID,'{1}' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY{3}(t2.FJNAPPROVE* e.FConvertNumerator / e.FConvertDenominator) as FBEFOREQTY,tm.FQTY as FAFTERQTY
                    ,'{2}' as FDIRECTION
                    from JN_YDL_SCM_AllotApplyFor t1
                    inner join JN_YDL_SCM_AllotEntry t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FSALEORGID 
                    and tm.FSALERID=t1.FSALESMANID and tm.FMATERIALID=t2.FMATERIALID 
                    and tm.FAUXPROPID=t2.FAUXPROPID  and tm.FSaleDeptId=t1.FSALEDEPTID and tm.FSaleGroupId=t1.FSALEGROUPID
                    inner join T_BD_Material d  on t2.FMATERIALID=d.FMATERIALID
                    inner join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=t2.FUNITID and e.FDESTUNITID=tm.FUnitID
                    where t1.Fid in ({0}) and tm.FUNITID<>t2.FUNITID", string.Join(",", lstFids), sType, IsAduit ? "B" : "A", IsAduit ? "+" : "-");
                    break;
                #endregion
                #region //赠品申请单
                case "C":
                    sql = string.Format(@"select newid() as FBILLNO,t2.FAUXPROPID,tm.FUNITID,t2.FBaseUnitQty as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FSALESMANID as FSALERID,t1.FSALEORGID,t1.FSALEDEPTID
                    ,t1.FSaleGroupId,t2.FMATERIALID,tm.FID as FFORECASTID,'{1}' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY{3}t2.FBaseUnitQty as FBEFOREQTY,tm.FQTY as FAFTERQTY,'{2}' as FDIRECTION
                    from JN_T_SAL_GiftReq t1
                    inner join JN_T_SAL_GiftReqEntry t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FSALEORGID 
                    and tm.FSALERID=t1.FSALESMANID and tm.FMATERIALID=t2.FMATERIALID 
                    and tm.FUNITID=t2.FBaseUnitID and tm.FAUXPROPID=t2.FAUXPROPID
                    and tm.FSaleDeptId=t1.FSALEDEPTID and tm.FSaleGroupId=t1.FSALEGROUPID
                    where t1.Fid in ({0})
                    union all
                    select newid() as FBILLNO,t2.FAUXPROPID,tm.FUNITID,(t2.FBaseUnitQty* e.FConvertNumerator / e.FConvertDenominator) as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FSALESMANID as FSALERID,t1.FSALEORGID,t1.FSALEDEPTID
                    ,t1.FSaleGroupId,t2.FMATERIALID,tm.FID as FFORECASTID,'{1}' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY{3}(t2.FBaseUnitQty* e.FConvertNumerator / e.FConvertDenominator) as FBEFOREQTY,tm.FQTY as FAFTERQTY
                    ,'{2}' as FDIRECTION
                    from JN_T_SAL_GiftReq t1
                    inner join JN_T_SAL_GiftReqEntry t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FSALEORGID 
                    and tm.FSALERID=t1.FSALESMANID and tm.FMATERIALID=t2.FMATERIALID 
                    and tm.FAUXPROPID=t2.FAUXPROPID  and tm.FSaleDeptId=t1.FSALEDEPTID and tm.FSaleGroupId=t1.FSALEGROUPID
                    inner join T_BD_Material d  on t2.FMATERIALID=d.FMATERIALID
                    inner join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=t2.FBaseUnitID and e.FDESTUNITID=tm.FUnitID
                    where t1.Fid in ({0}) and tm.FUNITID<>t2.FBaseUnitID", string.Join(",", lstFids), sType, IsAduit ? "B" : "A", IsAduit ? "+" : "-");
                    break;
                #endregion
                #region //销售预测单
                case "D":
                    sql = string.Format(@"select newid() as FBILLNO,t2.FJNAUXPROP as FAUXPROPID,tm.FUNITID,t2.FJNFORECASTQTY as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FJNSALERID as FSALERID,t1.FJNSALEORGID as FSALEORGID,t1.FJNSaleDeptId as FSaleDeptId
                    ,t1.FJNSaleGroupId as FSaleGroupId,t2.FJNMATERIALID as FMATERIALID,tm.FID as FFORECASTID,'{1}' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY{3}t2.FJNFORECASTQTY as FBEFOREQTY,tm.FQTY as FAFTERQTY,'{2}' as FDIRECTION
                    from JN_T_SAL_Forecast t1
                    inner join JN_T_SAL_ForecastEntity t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FJNSALEORGID 
                    and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                    and tm.FUNITID=t2.FJNUnitID and tm.FAUXPROPID=t2.FJNAUXPROP
                    and tm.FSaleDeptId=t1.FJNSaleDeptId and tm.FSaleGroupId=t1.FJNSaleGroupId
                    where t1.Fid in ({0})
                    union all
                    select newid() as FBILLNO,t2.FJNAUXPROP as FAUXPROPID,tm.FUNITID,(t2.FJNFORECASTQTY* e.FConvertNumerator / e.FConvertDenominator) as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FJNSALERID as FSALERID,t1.FJNSALEORGID as FSALEORGID,t1.FJNSaleDeptId as FSaleDeptId
                    ,t1.FJNSaleGroupId as FSaleGroupId,t2.FJNMATERIALID as FMATERIALID,tm.FID as FFORECASTID,'{1}' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,tm.FQTY{3}(t2.FJNFORECASTQTY* e.FConvertNumerator / e.FConvertDenominator) as FBEFOREQTY,tm.FQTY as FAFTERQTY
                    ,'{2}' as FDIRECTION
                    from JN_T_SAL_Forecast t1
                    inner join JN_T_SAL_ForecastEntity t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FJNSALEORGID 
                    and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                    and tm.FAUXPROPID=t2.FJNAUXPROP  and tm.FSaleDeptId=t1.FJNSaleDeptId and tm.FSaleGroupId=t1.FJNSaleGroupId
                    inner join T_BD_Material d  on t2.FJNMATERIALID=d.FMATERIALID
                    inner join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=t2.FJNUnitID and e.FDESTUNITID=tm.FUnitID
                    where t1.Fid in ({0}) and tm.FUNITID<>t2.FJNUnitID", string.Join(",", lstFids), sType, IsAduit ? "A" : "B", IsAduit ? "-" : "+");
                    break;
                #endregion
                #region //销售预测变更单
                case "E":
                    sql = string.Format(@"select newid() as FBILLNO,t2.FJNAUXPROP as FAUXPROPID,tm.FUNITID,t2.FJNFORECASTQTY as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FJNSALERID as FSALERID,t1.FJNSALEORGID as FSALEORGID,t1.FJNSaleDeptId as FSaleDeptId
                    ,t1.FJNSaleGroupId as FSaleGroupId,t2.FJNMATERIALID as FMATERIALID,tm.FID as FFORECASTID,'{1}' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID,(case when t1.FDirection='A' then tm.FQTY{2}t2.FJNFORECASTQTY 
                                                          else tm.FQTY{3}t2.FJNFORECASTQTY end )as FBEFOREQTY,tm.FQTY as FAFTERQTY
                    ,(case when t1.FDirection='A' then '{4}' else '{5}' end) as FDirection 
                    from JN_T_SAL_ForecastChange t1
                    inner join JN_T_SAL_ForecastChangeEntry t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FJNSALEORGID 
                    and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                    and tm.FUNITID=t2.FJNUnitID and tm.FAUXPROPID=t2.FJNAUXPROP
                    and tm.FSaleDeptId=t1.FJNSaleDeptId and tm.FSaleGroupId=t1.FJNSaleGroupId
                    where t1.Fid in ({0})
                    union all
                    select newid() as FBILLNO,t2.FJNAUXPROP as FAUXPROPID,tm.FUNITID,(t2.FJNFORECASTQTY* e.FConvertNumerator / e.FConvertDenominator) as FADJUSTQTY
                    ,getdate() as FADJUSTDATE,t1.FJNSALERID as FSALERID,t1.FJNSALEORGID as FSALEORGID,t1.FJNSaleDeptId as FSaleDeptId
                    ,t1.FJNSaleGroupId as FSaleGroupId,t2.FJNMATERIALID as FMATERIALID,tm.FID as FFORECASTID,'{1}' as FBILLTYPE,t1.FID as FBILLID
                    ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID
                    ,(case when t1.FDirection='A' then  tm.FQTY{2}(t2.FJNFORECASTQTY* e.FConvertNumerator / e.FConvertDenominator)
                      else tm.FQTY{3}(t2.FJNFORECASTQTY* e.FConvertNumerator / e.FConvertDenominator) end ) as FBEFOREQTY,tm.FQTY as FAFTERQTY
                    ,(case when t1.FDirection='A' then '{4}' else '{5}' end) as FDirection 
                    from JN_T_SAL_ForecastChange t1
                    inner join JN_T_SAL_ForecastChangeEntry t2 on t1.FID=t2.FID 
                    inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FJNSALEORGID 
                    and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                    and tm.FAUXPROPID=t2.FJNAUXPROP  and tm.FSaleDeptId=t1.FJNSaleDeptId and tm.FSaleGroupId=t1.FJNSaleGroupId
                    inner join T_BD_Material d  on t2.FJNMATERIALID=d.FMATERIALID
                    inner join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=t2.FJNUnitID and e.FDESTUNITID=tm.FUnitID
                    where t1.Fid in ({0}) and tm.FUNITID<>t2.FJNUnitID"
                        , string.Join(",", lstFids), sType, IsAduit ? "-" : "+", IsAduit ? "+" : "-", IsAduit ? "A" : "B", IsAduit ? "B" : "A");
                    break;
                #endregion
            }

            dycInsert = DBUtils.ExecuteDynamicObject(ctx, sql);

            List<string> lstLogsql = new List<string>();

            if (dycInsert != null && dycInsert.Count() > 0)
            {
                long[] ids = dbservice.GetSequenceInt64(ctx, "JN_T_SAL_ForecastLog", dycInsert.Count()).ToArray();

                int index = 0;

                foreach (DynamicObject item in dycInsert)
                {
                    sql = string.Format(@"INSERT INTO JN_T_SAL_ForecastLog (FID,FBILLNO,FAUXPROPID,FUNITID,FADJUSTQTY,FADJUSTDATE
                                        ,FSALERID,FSALEORGID,FMATERIALID,FFORECASTID,FBILLTYPE,FBILLID,FSRCBILLNO
                                        ,FENTRYID,FBEFOREQTY,FAFTERQTY,FADJUSTID,FDIRECTION,FSaleDeptId,FSaleGroupId) 
                                        VALUES ({0},'{1}',{2},{3},{4},'{5}',{6},{7},{8},{9},'{10}',{11},'{12}',{13},{14},{15},{16},'{17}',{18},{19})"
                                      , ids[index], item["FBILLNO"], item["FAUXPROPID"], item["FUNITID"], item["FADJUSTQTY"], item["FADJUSTDATE"]
                                      , item["FSALERID"], item["FSALEORGID"], item["FMATERIALID"], item["FFORECASTID"], item["FBILLTYPE"]
                                      , item["FBILLID"], item["FSRCBILLNO"], item["FENTRYID"], item["FBEFOREQTY"], item["FAFTERQTY"], ctx.UserId
                                      , item["FDIRECTION"], item["FSaleDeptId"], item["FSaleGroupId"]);

                    lstLogsql.Add(sql);
                    index++;
                }

                if (lstLogsql.Count() > 0)
                {
                    results = DBUtils.ExecuteBatch(ctx, lstLogsql, lstLogsql.Count());
                }
            }

            #endregion

            return results;

        }

    }
}
