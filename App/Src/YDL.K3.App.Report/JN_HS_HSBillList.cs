using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using YDL.K3.FIN.HS.App.Report;
using Kingdee.BOS;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.K3.FIN.App.Core.OutAcctgConfig;
using Kingdee.BOS.Core.Metadata;
using System.Data;

namespace YDL.K3.App.Report
{
    public class JN_HS_HSBillList : YDL_HS_HSBillReport
    {
        public override void BuilderReportSqlAndTempTable(Kingdee.BOS.Core.Report.IRptParams filter, string tableName)
        {
            base.BuilderReportSqlAndTempTable(filter, tableName);
            //ReportCommon.AddTableColumn(base.Context, tableName);
            this.UpdateExpandFieldbiaodun(tableName);
        }

        private void UpdateExpandFieldbiaodun(string tableName)
        {
            //throw new NotImplementedException();
            string str = YDLReportCommon.GetExpandFieldSql(base.Context, tableName, "FBILLFROMID");
            if (!string.IsNullOrEmpty(str))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat(" merge into {0} T0  using (", tableName);
                builder.AppendFormat("  {0} ", str);
                builder.AppendFormat(" ) T on ( T0.FBIllENTRYID = T.FBIllENTRYID and T0.FBILLFROMID = T.FBILLFROMID )  ", new object[0]);
                builder.AppendFormat(" when matched then update set T0.F_YDL_UNITENZYMES=T.FRATE,T0.F_YDL_EXTAUXUNITQTY=T.FSECACTUALQTY,T0.F_YDL_TON=case T.FJNTONPROPERTY when 0 then 0 else  ( ( T.FSECACTUALQTY ) / ( T.FJNTONPROPERTY * 1000 ) ) end ", new object[0]);
                DBUtils.Execute(base.Context, builder.ToString());
            }

        }
    }


    public class YDLReportCommon
    {
        public static string[] qtyFieldNames;
        static YDLReportCommon()
        {
            qtyFieldNames = new string[] { "FExtAuxUnitQty", "FSecRealQty", "FSECQTY", "FSecActualQty", "FSecStockQty" };
        }


        public static string GetExpandFieldSql(Context context, string tableName, string billFormFiedName)
        {
            IMetaDataService service = ServiceHelper.GetService<IMetaDataService>();
            string[] strArray = GetBillFormId(context, tableName, billFormFiedName);
            if (strArray.Length == 0)
            {
                return string.Empty;
            }
            List<OutAcctgSeqConfig> source = new OutInStockIndexService().GetAcctgIndexData(context, 0L, 0L, string.Format(" FBILLFROMID in ('{0}')", string.Join("','", strArray)));
            StringBuilder builder = new StringBuilder();
            string[] strArray2 = strArray;
            for (int j = 0; j < strArray2.Length; j++)
            {
                Func<OutAcctgSeqConfig, bool> predicate = null;
                string item = strArray2[j];
                FormMetadata mete = (FormMetadata)service.Load(context, item, true);
                if (predicate == null)
                {
                    predicate = i => i.BillFromId.ToUpper() == item.ToUpper();
                }
                OutAcctgSeqConfig config = source.FirstOrDefault<OutAcctgSeqConfig>(predicate);
                if (config != null)
                {
                    config.Initialization(mete, null);
                    Field extQtyField = null;
                    foreach (string str in qtyFieldNames)
                    {
                        extQtyField = mete.BusinessInfo.GetField(str);
                        if (extQtyField != null)
                        {
                            break;
                        }
                    }
                    if (extQtyField != null)
                    {
                        Field field = mete.BusinessInfo.GetField(config.QtyField);
                        Field matField = mete.BusinessInfo.GetField(config.MaterialField);
                        if (!string.IsNullOrEmpty(builder.ToString()))
                        {
                            builder.AppendLine(" union all ");
                        }
                        builder.AppendLine(GetBills(tableName, matField, field, extQtyField, billFormFiedName, item));
                    }
                }
            }
            return builder.ToString();
        }

        public static string GetBills(string tempName, Field matField, Field qtyField, Field extQtyField, string formIdField, string formId)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" SELECT DISTINCT temp.FBIllENTRYID,temp.{1},{0}.{2} as FSECACTUALQTY ,mat.FJNTonProperty, ", extQtyField.TableName, formIdField, extQtyField.FieldName);
            builder.AppendFormat("  CASE {2}.{3} WHEN 0 THEN 0 ELSE Round({0}.{1} / {2}.{3}, 10) END frate ", new object[] { extQtyField.TableName, extQtyField.FieldName, qtyField.TableName, qtyField.FieldName });
            builder.AppendFormat("        FROM  {0} temp ", tempName);
            builder.AppendFormat("        INNER JOIN {0} ON temp.FBILLENTRYID = {0}.FENTRYID  ", qtyField.TableName);
            if (qtyField.TableName != extQtyField.TableName)
            {
                builder.AppendFormat("  inner join {0} on {0}.fentryid=temp.FBILLENTRYID ", extQtyField.TableName);
            }
            if ((qtyField.TableName != matField.TableName) && (extQtyField.TableName != matField.TableName))
            {
                builder.AppendFormat("        INNER JOIN {0} ON temp.FBILLENTRYID = {0}.FENTRYID  ", matField.TableName);
            }
            builder.AppendFormat("        INNER JOIN T_BD_MATERIAL mat ", new object[0]);
            builder.AppendFormat("        ON  {0}.{1}=mat.FMATERIALID  ", matField.TableName, matField.FieldName);
            builder.AppendFormat("        WHERE  mat.fismeasure = '1' and temp.{0}='{1}' ", formIdField, formId);
            return builder.ToString();
        }

        public static string[] GetBillFormId(Context context, string tableName, string billFormFieldName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(" select distinct {0} from {1} where isnull({0},'')<>'' ", billFormFieldName, tableName);
            return (from i in DBUtils.ExecuteDynamicObject(context, builder.ToString(), null, null, CommandType.Text, new SqlParam[0]) select Convert.ToString(i[billFormFieldName])).ToArray<string>();
        }












    }
}
