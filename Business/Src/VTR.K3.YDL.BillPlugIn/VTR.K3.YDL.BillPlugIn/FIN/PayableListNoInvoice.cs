using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Util;


namespace VTR.K3.YDL.BillPlugIn.FIN
{
    [Description("列表上计算未开票金额")]
    public class PayableListNoInvoice  : AbstractListPlugIn
    {
        /// <summary>
        /// 此事件，在确定过滤方案之后，列表刷新数据之前执行
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>
        /// 在此事件中，增加列表的显示字段、过滤条件、排序等等
        /// </remarks>
        public override void PrepareFilterParameter(FilterArgs e)
        {
            base.PrepareFilterParameter(e);
            // 如果列表显示未开票金额，则强制要求列表加载价税合计、已开发票核销金额
            var fldDays = e.ColumnFields.FirstOrDefault(
                fld => fld.Key.EqualsIgnoreCase("FNoInvoiceAmount"));
            if (fldDays != null)
            {
                e.AppendLoadFieldList.Add("FALLAMOUNTFOR_D");
                e.AppendLoadFieldList.Add("FOPENAMOUNTFOR_D");
                e.AppendLoadFieldList.Add("FCURRENCYID");
            }
        }
        /// <summary>
        /// 此事件，在列表加载完毕数据，逐行、逐字段显示到表格中时执行
        /// </summary>
        /// <param name="args"></param>
        /// <remarks>
        /// 在此事件中，格式化某一个字段的显示值
        /// </remarks>
        public override void FormatCellValue(FormatCellValueArgs args)
        {
            base.FormatCellValue(args);
            if (args.Header.FieldName.EqualsIgnoreCase("FNoInvoiceAmount"))
            {

                decimal FALLAMOUNTFOR = Convert.ToDecimal(args.DataRow["FALLAMOUNTFOR_D"]);
                decimal FOPENAMOUNTFOR = Convert.ToDecimal(args.DataRow["FOPENAMOUNTFOR_D"]);
                decimal FNoInvoiceAmount = FALLAMOUNTFOR - FOPENAMOUNTFOR;
                string FCURRENCY = Convert.ToString(args.DataRow["FCURRENCYID"]);
                string NoInvoiceAmount=string.Format("{0:C}", FNoInvoiceAmount);
            
               // DateTime beginDate = (DateTime)args.DataRow["F_JD_BeginDate"];
               // DateTime endDate = (DateTime)args.DataRow["F_JD_EndDate"];
                //TimeSpan beginTS = new TimeSpan(beginDate.Ticks);
                //TimeSpan endTS = new TimeSpan(endDate.Ticks);
               // TimeSpan diffTS = endTS.Subtract(beginTS).Duration();
           
                if (FCURRENCY=="3")
                {
                    NoInvoiceAmount = NoInvoiceAmount.Replace("￥", "€");
                }

                if (FCURRENCY == "7")
                {
                    NoInvoiceAmount = NoInvoiceAmount.Replace("￥", "$");
                }


                args.FormateValue = NoInvoiceAmount;
               
                //args.FormateValue
            }
        }
    }
}



