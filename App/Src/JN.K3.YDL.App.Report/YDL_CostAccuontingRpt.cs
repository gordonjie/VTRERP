using Kingdee.K3.FIN.CB.App.Report;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Report;
using JN.K3.YDL.Core;
using Kingdee.BOS.App.Data;
using Kingdee.BOS;
using Kingdee.BOS.Core.List;

namespace JN.K3.YDL.Report.PlugIn
{
    /// <summary>
    /// 成本核算报表
    /// </summary>
    [Description("成本核算报表")]
    public class YDL_CostAccuontingRpt: CostCalBillHorizontalRpt
    {
        /// <summary>
        /// 取值逻辑
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="tableName"></param>
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            //base.BuilderReportSqlAndTempTable(filter, tableName);
            //创建临时表,用于存放扩展字段数据           
            string strTempTable = AppServiceContext.DBService.CreateTemporaryTableName(this.Context);

            //获取原报表数据
            base.BuilderReportSqlAndTempTable(filter, strTempTable);

            ////组合数据,回写原报表
            //StringBuilder sb = new StringBuilder();
            //sb.AppendFormat("select a.* into {0} from {1} a ", tableName, strTempTable);
            //DBUtils.Execute(this.Context, sb.ToString());

            //创建临时表
            string[] tablenames = AppServiceContext.DBService.CreateTemporaryTableName(base.Context, 4);

            StringBuilder sb = new StringBuilder();

            //酶种
            GetMZ(sb, tablenames[0]);

            sb.AppendFormat(" select distinct * into {0} from {1} order by fmastId", tablenames[1], tablenames[0]);

            //入库酶活
            GetRKMH(sb, tablenames[2]);

            //领用酶活
            GetLYMH(sb, tablenames[3]);

            //核算表
            GetAllTableData(sb, tableName, strTempTable, tablenames);

            DBUtils.Execute(this.Context, sb.ToString());

            //删除临时表
            AppServiceContext.DBService.DeleteTemporaryTableName(base.Context, tablenames);
            AppServiceContext.DBService.DeleteTemporaryTableName(this.Context, new string[] { strTempTable });
        }

        ReportHeader header = null;
        /// <summary>
        /// 设置列名
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            header = base.GetReportHeaders(filter);
            header = new ReportHeader();
            setField();
            return header;

        }

        /// <summary>
        /// 获取酶活
        /// </summary>
        /// <param name="sb">sql语句</param>
        /// <param name="strTable">临时表名</param>
        private void GetMZ(StringBuilder sb, string strTable)
        {
            //半成品
            sb.AppendLine(" select t0.FMATERIALID fmastId, t0.FNUMBER ,t1.FNAME,t2.FNAME name  ");
            sb.AppendFormat(" into {0} ", strTable);
            sb.AppendLine(" from T_BD_MATERIAL t0  ");
            sb.AppendLine(" inner join T_BD_MATERIAL_L t1 on t0.FMATERIALID = t1.FMATERIALID ");
            sb.AppendLine(" inner join t_BD_MaterialBase t6 on t6.FMATERIALID = t1.FMATERIALID ");
            sb.AppendLine(" left join ( select t3.FMATERIALID,t3.FNUMBER ,t4.FNAME from T_BD_MATERIAL t3  ");
            sb.AppendLine(" inner join T_BD_MATERIAL_L t4 on t3.FMATERIALID = t4.FMATERIALID ");
            sb.AppendLine(" where t3.F_JN_IsENZYMEMATERIAL='1') t2 on t0.F_JN_FUNGUSCLASS=t2.FMATERIALID ");
            sb.AppendLine(" left join T_BD_MATERIALCATEGORY t5 on t6.FCategoryID=t5.FCATEGORYID ");
            sb.AppendLine(" where t5.FNUMBER='CHLB03_SYS' ");

            //成品
            sb.AppendFormat(" insert into {0} ", strTable);
            sb.AppendLine(" select t0.FMATERIALID fmastId, t6.FNUMBER,t7.FNAME,t10.FNAME name from T_ENG_BOM t0  ");
            sb.AppendLine(" inner join  T_ENG_BOMCHILD t1 on t0.FID=t1.fid  ");
            sb.AppendLine(" left join (select t2.FMATERIALID,t2.FNUMBER  from T_BD_MATERIAL t2 inner join t_BD_MaterialBase t3 on t2.FMATERIALID=t3.FMATERIALID  ");
            sb.AppendLine(" left join T_BD_MATERIALCATEGORY t5 on t3.FCategoryID=t5.FCATEGORYID ");
            sb.AppendLine(" where t5.FNUMBER='CHLB05_SYS') t4 on t4.FMATERIALID=t0.FMATERIALID  ");
            sb.AppendLine(" inner join T_BD_MATERIAL t6 on t0.FMATERIALID=t6.FMATERIALID ");
            sb.AppendLine(" inner join T_BD_MATERIAL_L t7 on t0.FMATERIALID=t7.FMATERIALID ");
            sb.AppendLine(" inner join t_BD_MaterialBase t12 on t12.FMATERIALID = t6.FMATERIALID ");
            sb.AppendLine(" left join T_BD_MATERIALCATEGORY t11 on t12.FCategoryID=t11.FCATEGORYID ");
            sb.AppendLine(" left join ( select t8.FMATERIALID,t8.FNUMBER ,t9.FNAME from T_BD_MATERIAL t8 ");
            sb.AppendLine(" inner join T_BD_MATERIAL_L t9 on t8.FMATERIALID = t9.FMATERIALID ");
            sb.AppendLine(" where t8.F_JN_IsENZYMEMATERIAL='1') t10 on t6.F_JN_FUNGUSCLASS=t10.FMATERIALID ");
            sb.AppendLine(" where t11.FNUMBER='CHLB03_SYS' ");
        }

        /// <summary>
        /// 获取入库酶活
        /// </summary>
        /// <param name="sb">sql语句</param>
        /// <param name="strTable">临时表名</param>
        private void GetRKMH(StringBuilder sb, string strTable)
        {
            //半成品(双单位)
            sb.AppendLine(" select t1.FMATERIALID,t2.FNUMBER,sum(t0.FSecRealQty) FSecRealQty  ");
            sb.AppendFormat(" into {0} ", strTable);
            sb.AppendLine(" from T_PRD_INSTOCKENTRY t0 ");
            sb.AppendLine(" inner join T_PRD_MOENTRY t1 on t0.FMATERIALID=t1.FMATERIALID ");
            sb.AppendLine(" inner join T_BD_MATERIAL t2 on t0.FMATERIALID=t2.FMATERIALID ");
            sb.AppendLine(" inner join t_BD_MaterialBase t3 on t3.FMATERIALID = t2.FMATERIALID ");
            sb.AppendLine(" left join T_BD_MATERIALCATEGORY t4 on t3.FCategoryID=t4.FCategoryID ");
            sb.AppendLine(" where t4.FNUMBER='CHLB03_SYS'  ");
            sb.AppendLine(" and t2.FISMEASURE='1' ");
            sb.AppendLine(" group by t1.FMATERIALID,t1.FLOT,t2.FNUMBER ");

            //半成品
            sb.AppendFormat(" insert into {0} ", strTable);
            sb.AppendLine(" select t1.FMATERIALID,t2.FNUMBER,sum(t0.FSecRealQty) FSecRealQty from T_PRD_INSTOCKENTRY t0  ");
            sb.AppendLine(" inner join T_PRD_MOENTRY t1 on t0.FMATERIALID=t1.FMATERIALID ");
            sb.AppendLine(" inner join T_BD_MATERIAL t2 on t0.FMATERIALID=t2.FMATERIALID ");
            sb.AppendLine(" inner join t_BD_MaterialBase t3 on t3.FMATERIALID = t2.FMATERIALID ");
            sb.AppendLine(" left join T_BD_MATERIALCATEGORY t4 on t3.FCategoryID=t4.FCategoryID ");
            sb.AppendLine(" where t4.FNUMBER='CHLB03_SYS'  ");
            sb.AppendLine(" and t2.FISMEASURE='0' ");
            sb.AppendLine(" group by t1.FMATERIALID,t2.FNUMBER ");

            //成品
            sb.AppendFormat(" insert into {0} ", strTable);
            sb.AppendLine(" select t1.FMATERIALID,t2.FNUMBER,sum(t0.FSecRealQty) FSecRealQty from T_PRD_INSTOCKENTRY t0  ");
            sb.AppendLine(" inner join T_PRD_MOENTRY t1 on t0.FMATERIALID=t1.FMATERIALID ");
            sb.AppendLine(" inner join T_BD_MATERIAL t2 on t0.FMATERIALID=t2.FMATERIALID ");
            sb.AppendLine(" inner join t_BD_MaterialBase t3 on t3.FMATERIALID = t2.FMATERIALID ");
            sb.AppendLine(" left join T_BD_MATERIALCATEGORY t4 on t3.FCategoryID=t4.FCategoryID ");
            sb.AppendLine(" where t4.FNUMBER='CHLB05_SYS' ");
            sb.AppendLine(" group by t1.FMATERIALID,t2.FNUMBER ");
        }

        /// <summary>
        /// 领用酶活
        /// </summary>
        /// <param name="sb">sql语句</param>
        /// <param name="strTable">临时表名</param>
        private void GetLYMH(StringBuilder sb, string strTable)
        {
            sb.AppendLine(" select t1.FMATERIALID,t2.FNUMBER,sum(t0.FSECACTUALQTY) FSecRealQty  ");
            sb.AppendFormat(" into {0} ", strTable);
            sb.AppendLine(" from T_PRD_PICKMTRLDATA t0 ");
            sb.AppendLine(" inner join T_PRD_MOENTRY t1 on t0.FMATERIALID=t0.FMATERIALID ");
            sb.AppendLine(" inner join T_BD_MATERIAL t2 on t0.FMATERIALID=t2.FMATERIALID ");
            sb.AppendLine(" inner join t_BD_MaterialBase t3 on t3.FMATERIALID = t2.FMATERIALID ");
            sb.AppendLine(" left join T_BD_MATERIALCATEGORY t4 on t3.FCategoryID=t4.FCategoryID ");
            sb.AppendLine(" where t4.FNUMBER='CHLB03_SYS'  ");
            sb.AppendLine(" group by t1.FMATERIALID,t2.FNUMBER ");
        }

        /// <summary>
        /// 成本核算
        /// </summary>
        /// <param name="sb">sql语句</param>
        /// <param name="strTable">临时表名</param>
        /// <param name="strTempTable">原表数据</param>
        /// <param name="tablenames">关联临时表</param>
        private void GetAllTableData(StringBuilder sb, string strTable,string strTempTable, string[] tablenames)
        {
            //半成品
            sb.AppendLine(" SELECT ROW_NUMBER() OVER(ORDER BY T0.FIDENTITYID ) FIDENTITYID, ");
            sb.AppendLine(" t0.FCOSTCENTERNUMBER F_JN_YDL_CBZXDM,t0.FCOSTCENTERNAME F_JN_YDL_CBZXMC, ");//成本中心
            sb.AppendLine(" t0.FPRODUCTID_FNUMBER F_JN_YDL_CPDM, t0.FPRODUCTID_FNAME F_JN_YDL_CPMC, ");//产品
            sb.AppendLine(" t0.FBASICUNITFIELD_FNAME F_JN_YDL_JBDW, "); //基本单位
            sb.AppendLine(" t0.FPROORDERTYPE F_JN_YDL_SCLX, "); //生产类型
            sb.AppendLine(" T0.FBATCHFIELD_FNAME F_JN_YDL_LOT, t0.FAIDPROPERTYFIELD_FNAME F_JN_YDL_FZSX, ");//批号,辅助属性
            sb.AppendLine(" t0.FCompleteQty F_JN_YDL_Qty, "); //数量
            sb.AppendLine(" t0.FCompleteAmount  F_JN_YDL_ZZCB,");//制造成本
            sb.AppendLine(" t17.name F_JN_YDL_MZ, ");
            sb.AppendLine(" t0.FSumComplete20522 F_JN_YDL_YCLCB,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t0.FSumComplete20522/(t0.FCompleteQty) END F_JN_YDL_YCLDWCB, ");//原材料
            sb.AppendLine(" t0.FSumComplete20526 F_JN_YDL_RGFCB,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t0.FSumComplete20526/(t0.FCompleteQty) END F_JN_YDL_RGFDWCB, ");//人工费
            sb.AppendLine(" t0.FSumComplete602413 F_JN_YDL_DLFCB,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t0.FSumComplete602413/(t0.FCompleteQty) END F_JN_YDL_DLFDWCB, ");//动力费
            sb.AppendLine(" t0.FSumComplete601802 F_JN_YDL_ZZFYZJCB,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t0.FSumComplete601802/(t0.FCompleteQty) END F_JN_YDL_ZZFYZJDWCB, ");//制造折旧
            sb.AppendLine(" t0.FSumComplete601808 F_JN_YDL_ZZFYQTCB,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t0.FSumComplete601808/(t0.FCompleteQty) END F_JN_YDL_ZZFYQTDWCB, ");//制造其他
            sb.AppendLine(" CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ");
            sb.AppendLine(" ELSE (t0.FSumComplete20522+t0.FSumComplete20526+t0.FSumComplete602413+t0.FSumComplete601802+t0.FSumComplete601808)/(t0.FCompleteQty) END F_JN_YDL_DWCB, ");//单位成本
            sb.AppendLine(" t18.FSecRealQty F_JN_YDL_RKMH,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t18.FSecRealQty/(t0.FCompleteQty) END F_JN_YDL_RKDWMH, ");//入库酶活
            sb.AppendLine(" t19.FSecRealQty F_JN_YDL_LYMH, ");//领用酶活
            sb.AppendLine(" CASE WHEN t19.FSecRealQty=0 THEN 0 ELSE t18.FSecRealQty/t19.FSecRealQty END F_JN_YDL_MHSL, "); //酶活收率
            sb.AppendLine(" CASE WHEN t18.FSecRealQty=0 THEN 0 ELSE (t0.FCompleteAmount)/t18.FSecRealQty*10000 END F_JN_YDL_WDWMHCB ");//万单位酶活成本
            sb.AppendFormat(" into {0} FROM {1} t0 ", strTable, strTempTable);
            sb.AppendLine(" INNER JOIN t_BD_MaterialBase t9 on t0.FPRODUCTID=t9.FMATERIALID ");
            sb.AppendLine(" inner join T_BD_MATERIALCATEGORY_L t10 on (t9.FCATEGORYID=t10.FCATEGORYID and t10.FLOCALEID=2052) ");
            sb.AppendLine(" LEFT OUTER JOIN T_BD_UNIT t12 ON t9.FBASEUNITID = t12.FUNITID ");
            sb.AppendLine(" LEFT OUTER JOIN T_BD_UNIT_L t13 ON (t9.FBASEUNITID = t13.FUNITID AND t13.FLOCALEID = 2052)  ");
            sb.AppendLine(" LEFT OUTER JOIN T_ENG_BOM t14 ON T0.FBOMID = t14.FID  ");
            sb.AppendLine(" LEFT OUTER JOIN T_BD_LOTMASTER t15 ON T0.FLOT = t15.FLOTID ");
            sb.AppendLine(" left join T_BD_MATERIALCATEGORY t16 on t16.FCategoryID=t9.FCATEGORYID ");
            sb.AppendFormat(" left join {0} t17 on t17.fmastId=t0.FMATERIALID ", tablenames[1]);
            sb.AppendFormat(" left join {0} t18 on t18.FMATERIALID=t0.FMATERIALID ", tablenames[2]);
            sb.AppendFormat(" left join {0} t19 on t19.FMATERIALID=t0.FMATERIALID ", tablenames[3]);
            sb.AppendLine(" where t16.FNUMBER='CHLB03_SYS'  ");

            //成品
            sb.AppendFormat(" insert into {0} ", strTable);
            sb.AppendLine(" SELECT ROW_NUMBER() OVER(ORDER BY T0.FIDENTITYID ) FIDENTITYID, ");
            sb.AppendLine(" t0.FCOSTCENTERNUMBER F_JN_YDL_CBZXDM,t0.FCOSTCENTERNAME F_JN_YDL_CBZXMC, ");//成本中心
            sb.AppendLine(" t0.FPRODUCTID_FNUMBER F_JN_YDL_CPDM, t0.FPRODUCTID_FNAME F_JN_YDL_CPMC, ");//产品
            sb.AppendLine(" t0.FBASICUNITFIELD_FNAME F_JN_YDL_JBDW, "); //基本单位
            sb.AppendLine(" t0.FPROORDERTYPE F_JN_YDL_SCLX, "); //生产类型
            sb.AppendLine(" T0.FBATCHFIELD_FNAME F_JN_YDL_LOT, t0.FAIDPROPERTYFIELD_FNAME F_JN_YDL_FZSX, ");//批号,辅助属性
            sb.AppendLine(" t0.FCompleteQty F_JN_YDL_Qty, "); //数量
            sb.AppendLine(" t0.FCompleteAmount  F_JN_YDL_ZZCB,");//制造成本
            sb.AppendLine(" t17.name F_JN_YDL_MZ, ");
            sb.AppendLine(" t0.FSumComplete20522 F_JN_YDL_YCLCB,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t0.FSumComplete20522/(t0.FCompleteQty) END F_JN_YDL_YCLDWCB, ");//原材料
            sb.AppendLine(" t0.FSumComplete20526 F_JN_YDL_RGFCB,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t0.FSumComplete20526/(t0.FCompleteQty) END F_JN_YDL_RGFDWCB, ");//人工费
            sb.AppendLine(" t0.FSumComplete602413 F_JN_YDL_DLFCB,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t0.FSumComplete602413/(t0.FCompleteQty) END F_JN_YDL_DLFDWCB, ");//动力费
            sb.AppendLine(" t0.FSumComplete601802 F_JN_YDL_ZZFYZJCB,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t0.FSumComplete601802/(t0.FCompleteQty) END F_JN_YDL_ZZFYZJDWCB, ");//制造折旧
            sb.AppendLine(" t0.FSumComplete601808 F_JN_YDL_ZZFYQTCB,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t0.FSumComplete601808/(t0.FCompleteQty) END F_JN_YDL_ZZFYQTDWCB, ");//制造其他
            sb.AppendLine(" CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ");
            sb.AppendLine(" ELSE (t0.FSumComplete20522+t0.FSumComplete20526+t0.FSumComplete602413+t0.FSumComplete601802+t0.FSumComplete601808)/(t0.FCompleteQty) END F_JN_YDL_DWCB, ");//单位成本
            sb.AppendLine(" t18.FSecRealQty F_JN_YDL_RKMH,CASE WHEN (t0.FCompleteQty) = 0 THEN 0 ELSE t18.FSecRealQty/(t0.FCompleteQty) END F_JN_YDL_RKDWMH, ");//入库酶活
            sb.AppendLine(" t19.FSecRealQty F_JN_YDL_LYMH, ");//领用酶活
            sb.AppendLine(" CASE WHEN t19.FSecRealQty=0 THEN 0 ELSE t18.FSecRealQty/t19.FSecRealQty END F_JN_YDL_MHSL, "); //酶活收率
            sb.AppendLine(" CASE WHEN t18.FSecRealQty=0 THEN 0 ELSE (t0.FCompleteAmount)/t18.FSecRealQty*10000 END F_JN_YDL_WDWMHCB ");//万单位酶活成本
            sb.AppendFormat(" FROM {0} t0 ", strTempTable);
            sb.AppendLine(" INNER JOIN t_BD_MaterialBase t9 on t0.FPRODUCTID=t9.FMATERIALID ");
            sb.AppendLine(" inner join T_BD_MATERIALCATEGORY_L t10 on (t9.FCATEGORYID=t10.FCATEGORYID and t10.FLOCALEID=2052) ");
            sb.AppendLine(" LEFT OUTER JOIN T_BD_UNIT t12 ON t9.FBASEUNITID = t12.FUNITID ");
            sb.AppendLine(" LEFT OUTER JOIN T_BD_UNIT_L t13 ON (t9.FBASEUNITID = t13.FUNITID AND t13.FLOCALEID = 2052)  ");
            sb.AppendLine(" LEFT OUTER JOIN T_ENG_BOM t14 ON T0.FBOMID = t14.FID  ");
            sb.AppendLine(" LEFT OUTER JOIN T_BD_LOTMASTER t15 ON T0.FLOT = t15.FLOTID ");
            sb.AppendLine(" left join T_ENG_BOM t20  on t20.FMATERIALID=t0.FMATERIALID ");
            sb.AppendLine(" left join  T_ENG_BOMCHILD t21 on t20.FID=t21.fid ");
            sb.AppendLine(" left join t_BD_MaterialBase t22 on t22.FMATERIALID=t21.FMATERIALID ");
            sb.AppendLine(" left join T_BD_MATERIALCATEGORY t16 on t16.FCategoryID=t22.FCATEGORYID ");
            sb.AppendFormat(" left join {0} t17 on t17.fmastId=t21.FMATERIALID ", tablenames[1]);
            sb.AppendFormat(" left join {0} t18 on t18.FMATERIALID=t0.FMATERIALID ", tablenames[2]);
            sb.AppendFormat(" left join {0} t19 on t19.FMATERIALID=t21.FMATERIALID ", tablenames[3]);
            sb.AppendLine(" where t16.FNUMBER='CHLB05_SYS' ");

        }

        /// <summary>
        /// 设置列
        /// </summary>
        private void setField()
        {
            setField("F_JN_YDL_CBZXDM", "成本中心代码");
            setField("F_JN_YDL_CBZXMC", "成本中心名称");
            setField("F_JN_YDL_CPDM", "产品代码");
            setField("F_JN_YDL_CPMC", "产品名称");
            setField("F_JN_YDL_MZ", "酶种");
            setField("F_JN_YDL_SCLX", "生产类型");
            setField("F_JN_YDL_FZSX", "辅助属性");
            setField("F_JN_YDL_JBDW", "基本单位");
            setField("F_JN_YDL_LOT", "批号");
            setField("F_JN_YDL_Qty", "数量");
            Dictionary<string, string> fieldDrys = new Dictionary<string, string>();
            fieldDrys.Add("F_JN_YDL_YCLCB", "成本");
            fieldDrys.Add("F_JN_YDL_YCLDWCB", "单位成本");
            setField("原材料", ref fieldDrys);
            fieldDrys.Add("F_JN_YDL_RGFCB", "成本");
            fieldDrys.Add("F_JN_YDL_RGFDWCB", "单位成本");
            setField("人工费", ref fieldDrys);
            fieldDrys.Add("F_JN_YDL_DLFCB", "成本");
            fieldDrys.Add("F_JN_YDL_DLFDWCB", "单位成本");
            setField("动力费", ref fieldDrys);
            fieldDrys.Add("F_JN_YDL_ZZFYZJCB", "成本");
            fieldDrys.Add("F_JN_YDL_ZZFYZJDWCB", "单位成本");
            setField("制造费用折旧", ref fieldDrys);
            fieldDrys.Add("F_JN_YDL_ZZFYQTCB", "成本");
            fieldDrys.Add("F_JN_YDL_ZZFYQTDWCB", "单位成本");
            setField("制造费用其它", ref fieldDrys);
            setField("F_JN_YDL_ZZCB", "制造成本");
            setField("F_JN_YDL_DWCB", "单位成本");
            setField("F_JN_YDL_RKMH", "入库酶活");
            setField("F_JN_YDL_RKDWMH", "入库单位酶活");
            setField("F_JN_YDL_LYMH", "领用酶活");
            setField("F_JN_YDL_MHSL", "酶活收率");
            setField("F_JN_YDL_WDWMHCB", "万单位酶活成本");
            fieldDrys = null;
        }

        /// <summary>
        /// 单表头
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldText"></param>
        private void setField(string fieldName, string fieldText)
        {

            header.AddChild(fieldName, new LocaleValue(fieldText, this.Context.UserLocale.LCID));
        }

        /// <summary>
        /// 双表头
        /// </summary>
        /// <param name="headerName"></param>
        /// <param name="fieldDrys"></param>
        private void setField(string headerName, ref Dictionary<string, string> fieldDrys)
        {
            //合并表头
            ListHeader lsHeader = header.AddChild();
            lsHeader.Caption = new LocaleValue(headerName);
            foreach (KeyValuePair<string, string> fieldDry in fieldDrys)
            {
                lsHeader.AddChild(fieldDry.Key, new LocaleValue(fieldDry.Value, this.Context.UserLocale.LCID));
            }
            fieldDrys.Clear();
        }
    }
}
