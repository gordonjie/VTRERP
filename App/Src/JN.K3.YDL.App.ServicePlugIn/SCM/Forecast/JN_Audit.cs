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
    /// 销售预测单审核插件
    /// </summary>
    [Description("销售预测单审核插件")]
    public class JN_Audit : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FJNSubDate");
            e.FieldKeys.Add("FJNMaterialId");
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
                DynamicObjectCollection ForecastEntitys = data["SAL_ForecastEntity"] as DynamicObjectCollection;
                foreach(var ForecastEntity in ForecastEntitys)
                {
                string entityid=Convert.ToString(ForecastEntity["ID"]);
                string sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                set (FQTY)=(select a.FQTY+c.FBaseUnitQty
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

            //更新销售结余后台表
            DynamicObjectCollection dycInsertForecastBack = UpdateForecastBack(param);

            //插入销售结余日志表
            DynamicObjectCollection dycInsertForecastLog = UpdateForecastLog(param);


            //调用插入方法
            JNCommonServices.UpdateForecastBackAndLog(this.Context, dycInsertForecastBack, dycInsertForecastLog);


        }

        //更新销售结余后台表 12月13日取消销售组
        private DynamicObjectCollection UpdateForecastBack(SqlParam param)
        {
            string sql = string.Empty;
            //--存在出现同一张单出现表体出现相同物料时导致重复创建结余后台表，停用
            /*
            sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                set (FQTY)=(select a.FQTY+c.FBaseUnitQty
                from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_Forecast b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId  
                inner join JN_T_SAL_ForecastEntity c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID 
                and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FBaseUnitID 
                inner join TABLE(fn_StrSplit(@FEntryID,',',1)) tb on c.FEntryID=tb.FEntryID
                where a.FID=t0.FID )");

            DBUtils.Execute(this.Context, sql, param);*/

            sql = string.Format(@"select t1.FJNSALEORGID as FSALEORGID,t1.FJNSALERID as FSALERID,t1.FJNSaleDeptId as FSaleDeptId
                        ,newid() as FBILLNO,t2.FJNMATERIALID as FMATERIALID,t2.FBaseUnitQty as FQTY
                        ,t2.FBaseUnitID as FUNITID,t2.FJNAUXPROP as FAUXPROPID,getdate() as FDATE
                        from JN_T_SAL_Forecast t1
                        inner join TABLE(fn_StrSplit(@FID,',',1)) tb on t1.Fid=tb.Fid
                        inner join JN_T_SAL_ForecastEntity t2 on t1.FID=t2.FID
                        where not exists(select 1  from JN_T_SAL_ForecastBack tm where tm.FSALEORGID=t1.FJNSALEORGID 
                                        and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                                        and tm.FAUXPROPID=t2.FJNAUXPROP and tm.FSaleDeptId=t1.FJNSaleDeptId 
                                        and tm.FUnitID=t2.FBaseUnitID)
	                    ");

            return DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });
           

            
            /*
            string sql = string.Empty;

            sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
                set (FQTY)=(select a.FQTY+c.FBaseUnitQty
                from JN_T_SAL_ForecastBack a
                inner join JN_T_SAL_Forecast b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
                and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
                inner join JN_T_SAL_ForecastEntity c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID 
                and a.FAUXPROPID=c.FJNAUXPROP  and a.FUnitID=c.FBaseUnitID
                inner join TABLE(fn_StrSplit(@FID,',',1)) tb on b.Fid=tb.Fid
                where a.FID=t0.FID)");

            DBUtils.Execute(this.Context, sql, param);
            

            sql = string.Format(@"select t1.FJNSALEORGID as FSALEORGID,t1.FJNSALERID as FSALERID,t1.FJNSaleDeptId as FSaleDeptId
                        ,t1.FJNSaleGroupId as FSaleGroupId,newid() as FBILLNO,t2.FJNMATERIALID as FMATERIALID,t2.FBaseUnitQty as FQTY
                        ,t2.FBaseUnitID as FUNITID,t2.FJNAUXPROP as FAUXPROPID,getdate() as FDATE
                        from JN_T_SAL_Forecast t1
                        inner join TABLE(fn_StrSplit(@FID,',',1)) tb on t1.Fid=tb.Fid
                        inner join JN_T_SAL_ForecastEntity t2 on t1.FID=t2.FID
                        where not exists(select 1  from JN_T_SAL_ForecastBack tm where tm.FSALEORGID=t1.FJNSALEORGID 
                                        and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                                        and tm.FAUXPROPID=t2.FJNAUXPROP and tm.FSaleDeptId=t1.FJNSaleDeptId 
                                        and tm.FSaleGroupId=t1.FJNSaleGroupId and tm.FUnitID=t2.FBaseUnitID)
	                    ");

            return DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });*/

        }

        //插入销售结余日志表
        private DynamicObjectCollection UpdateForecastLog(SqlParam param)
        {
            string sql = string.Empty;

            //2017920未考虑第一次添加时JN_T_SAL_ForecastBack没有数据的情况
            sql = string.Format(@"select newid() as FBILLNO,t2.FJNAUXPROP as FAUXPROPID,t2.FBaseUnitID as FUNITID,t2.FBaseUnitQty as FADJUSTQTY
                                  ,getdate() as FADJUSTDATE,t1.FJNSALERID as FSALERID,t1.FJNSALEORGID as FSALEORGID,t1.FJNSaleDeptId as FSaleDeptId
                                  ,t2.FJNMATERIALID as FMATERIALID,isnull(tm.FID,0) as FFORECASTID,'D' as FBILLTYPE,t1.FID as FBILLID
                                  ,t1.FBILLNO as FSRCBILLNO,t2.FENTRYID
                                  ,(case when isnull(tm.FQTY,0)=0 then 0 else tm.FQTY end)  as FBEFOREQTY
                                  ,(case when isnull(tm.FQTY,0)=0 then t2.FBaseUnitQty else tm.FQTY+t2.FBaseUnitQty end ) as FAFTERQTY
                                  ,'A' as FDIRECTION 
                                  from JN_T_SAL_Forecast t1
                                  inner join TABLE(fn_StrSplit(@FID,',',1)) tb on t1.Fid=tb.Fid
                                  inner join JN_T_SAL_ForecastEntity t2 on t1.FID=t2.FID 
                                  left join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FJNSALEORGID 
                                  and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
                                  and tm.FUNITID=t2.FBaseUnitID and tm.FAUXPROPID=t2.FJNAUXPROP
                                  and tm.FSaleDeptId=t1.FJNSaleDeptId ");

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

        //            List<string> lstsql = new List<string>();

        //            //需要更新
        //            sql = string.Format(@"Update JN_T_SAL_ForecastBack as t0  
        //            set FQTY=(select a.FQTY+(c.FJNFORECASTQTY * e.FConvertNumerator / e.FConvertDenominator)
        //            from JN_T_SAL_ForecastBack a
        //            inner join JN_T_SAL_Forecast b on a.FSALEORGID=b.FJNSALEORGID and a.FSALERID=b.FJNSALERID 
        //            and a.FSaleDeptId=b.FJNSaleDeptId  and a.FSaleGroupId=b.FJNSaleGroupId
        //            inner join JN_T_SAL_ForecastEntity c on b.FID=c.FID and a.FMATERIALID=c.FJNMATERIALID and a.FAUXPROPID=c.FJNAUXPROP 
        //            inner join T_BD_Material d  on a.FMATERIALID=d.FMATERIALID
        //            inner join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=c.FJNUnitID and e.FDESTUNITID=a.FUnitID
        //            where a.FID=t0.FID and b.Fid in ({0}))", string.Join(",", lstFids));

        //            lstsql.Add(sql);


        //            //需要插入
        //            sql = string.Format(@"select t1.FJNSALEORGID,t1.FJNSALERID,t1.FJNSaleDeptId,t1.FJNSaleGroupId 
        //            ,newid() as FBILLNO,t2.FJNMATERIALID,t2.FJNFORECASTQTY,t2.FJNUnitID,t2.FJNAUXPROP,getdate() as FDATE
        //            from JN_T_SAL_Forecast t1
        //            inner join JN_T_SAL_ForecastEntity t2 on t1.FID=t2.FID
        //            where not exists(select 1  from JN_T_SAL_ForecastBack tm where tm.FSALEORGID=t1.FJNSALEORGID 
        //                            and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
        //                            and tm.FAUXPROPID=t2.FJNAUXPROP and tm.FSaleDeptId=t1.FJNSaleDeptId and tm.FSaleGroupId=t1.FJNSaleGroupId)
        //	        and  t1.Fid in ({0})", string.Join(",", lstFids));

        //            DynamicObjectCollection dycInsert = DBUtils.ExecuteDynamicObject(this.Context, sql);

        //            if (dycInsert != null && dycInsert.Count() > 0)
        //            {
        //                long[] ids = dbservice.GetSequenceInt64(this.Context, "JN_T_SAL_ForecastBack", dycInsert.Count()).ToArray();

        //                int index = 0;

        //                foreach (DynamicObject item in dycInsert)
        //                {
        //                    sql = string.Format(@"INSERT INTO JN_T_SAL_ForecastBack (FID,FSALEORGID,FSALERID,FSaleDeptId,FSaleGroupId,FBILLNO,FMATERIALID,FQTY,FUNITID,FAUXPROPID,FDATE) 
        //                       VALUES ({0},{1},{2},{3},{4},'{5}',{6},{7},{8},{9},'{10}')"
        //                       , ids[index], item["FJNSALEORGID"], item["FJNSALERID"], item["FJNSaleDeptId"], item["FJNSaleGroupId"]
        //                       , item["FBILLNO"], item["FJNMATERIALID"], item["FJNFORECASTQTY"], item["FJNUnitID"]
        //                       , item["FJNAUXPROP"], item["FDATE"]);
        //                    lstsql.Add(sql);
        //                    index++;
        //                }
        //            }

        //            int results = 0;

        //            if (lstsql.Count() > 0)
        //            {
        //                results = DBUtils.ExecuteBatch(this.Context, lstsql, lstsql.Count());
        //            }

        //            // 插入销售预测单结余表日志表
        //            #region
        //            if (results <= 0)
        //            {
        //                return;
        //            }

        //            sql = string.Format(@"select newid() as FBILLNO,t2.FJNAUXPROP,tm.FUNITID,t2.FJNFORECASTQTY,getdate() as FADJUSTDATE,
        //            t1.FJNSALERID,t1.FJNSALEORGID,t1.FJNSaleDeptId,t1.FJNSaleGroupId,t2.FJNMATERIALID
        //            ,tm.FID as FFORECASTID,'D' as FBILLTYPE,t1.FID as FBILLID,
        //            t1.FBILLNO,t2.FENTRYID,tm.FQTY-t2.FJNFORECASTQTY as FBEFOREQTY,tm.FQTY as FAFTERQTY
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
        //            t1.FBILLNO,t2.FENTRYID,tm.FQTY-(t2.FJNFORECASTQTY* e.FConvertNumerator / e.FConvertDenominator) as FBEFOREQTY,tm.FQTY as FAFTERQTY
        //            from JN_T_SAL_Forecast t1
        //            inner join JN_T_SAL_ForecastEntity t2 on t1.FID=t2.FID 
        //            inner join JN_T_SAL_ForecastBack tm on tm.FSALEORGID=t1.FJNSALEORGID 
        //            and tm.FSALERID=t1.FJNSALERID and tm.FMATERIALID=t2.FJNMATERIALID 
        //            and tm.FAUXPROPID=t2.FJNAUXPROP  and tm.FSaleDeptId=t1.FJNSaleDeptId and tm.FSaleGroupId=t1.FJNSaleGroupId
        //            inner join T_BD_Material d  on t2.FJNMATERIALID=d.FMATERIALID
        //            inner join T_BD_UNITCONVERTRATE e  on d.FMASTERID =e.FMASTERID and e.FCurrentUnitId=t2.FJNUnitID and e.FDESTUNITID=tm.FUnitID
        //            where t1.Fid in ({0}) and tm.FUNITID<>t2.FJNUnitID", string.Join(",", lstFids));

        //            dycInsert = DBUtils.ExecuteDynamicObject(this.Context, sql);

        //            List<string> lstLogsql = new List<string>();

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
        //                                      , item["FBILLID"], item["FBILLNO"], item["FENTRYID"], item["FBEFOREQTY"], item["FAFTERQTY"], this.Context.UserId, "A", item["FJNSaleDeptId"], item["FJNSaleGroupId"]);

        //                    lstLogsql.Add(sql);
        //                    index++;
        //                }

        //                if (lstLogsql.Count() > 0)
        //                {
        //                    results = DBUtils.ExecuteBatch(this.Context, lstLogsql, lstLogsql.Count());
        //                }
        //            }

        //            #endregion

        //        }


        //        /// <summary>
        //        /// 创建销售预测单结余表结构
        //        /// </summary>
        //        /// <param name="tmpTalbe"></param>
        //        /// <returns></returns>
        //        private static DataTable CreateDataTable(string tmpTalbe)
        //        {
        //            DataTable dt = new DataTable(tmpTalbe);
        //            DataColumn dc = new DataColumn();
        //            dc.DataType = typeof(System.Int64);
        //            dc.ColumnName = "FID";
        //            dt.Columns.Add(dc);
        //            dc = new DataColumn();
        //            dc.DataType = typeof(System.Int64);
        //            dc.ColumnName = "FSALEORGID";
        //            dt.Columns.Add(dc);
        //            dc = new DataColumn();
        //            dc.DataType = typeof(System.Int64);
        //            dc.ColumnName = "FSALERID";
        //            dt.Columns.Add(dc);
        //            dc = new DataColumn();
        //            dc.DataType = typeof(System.String);
        //            dc.ColumnName = "FBILLNO";
        //            dt.Columns.Add(dc);
        //            dc = new DataColumn();
        //            dc.DataType = typeof(System.Int64);
        //            dc.ColumnName = "FMATERIALID";
        //            dt.Columns.Add(dc);
        //            dc = new DataColumn();
        //            dc.DataType = typeof(System.Decimal);
        //            dc.ColumnName = "FQTY";
        //            dt.Columns.Add(dc);
        //            dc = new DataColumn();
        //            dc.DataType = typeof(System.Int64);
        //            dc.ColumnName = "FUNITID";
        //            dt.Columns.Add(dc);
        //            dc = new DataColumn();
        //            dc.DataType = typeof(System.Int64);
        //            dc.ColumnName = "FAUXPROPID";
        //            dt.Columns.Add(dc);
        //            dc = new DataColumn();
        //            dc.DataType = typeof(System.DateTime);
        //            dc.ColumnName = "FDATE";
        //            dt.Columns.Add(dc);
        //            return dt;
        //        }

        #endregion
    }
}
