using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;

namespace JN.K3.YDL.App.ServicePlugIn.FIN.ExpReimbursement
{
    /// <summary>
    /// 费用报销单保存反写检查插件
    /// </summary>
    [Description("费用报销单冲借款校验插件")]
    public class WrittenOffBorrowCheck : AbstractOperationServicePlugIn
    {
        // Methods
     
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            WrittenOffBorrow item = new WrittenOffBorrow
            {
                EntityKey = "FBillHead"
            };
            e.Validators.Add(item);

        }
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FRequestType");
            e.FieldKeys.Add("FSourceBillType");
            e.FieldKeys.Add("FSourceBillNo");
            e.FieldKeys.Add("FSRCBORROWAMOUNT");
            e.FieldKeys.Add("FExpSubmitAmount");
            e.FieldKeys.Add("FReqSubmitAmount");
            e.FieldKeys.Add("FIsFromBorrow");
            e.FieldKeys.Add("FSrcOffSetAmount");
            e.FieldKeys.Add("FSRCPAYEDAMOUNT");
            e.FieldKeys.Add("FSosurceRowID");
            e.FieldKeys.Add("FSeq");
            base.OnPreparePropertys(e);
        }


    }

    /// <summary>
    /// 费用报销单保存反写检查判断器
    /// </summary>
    internal class WrittenOffBorrow : AbstractValidator
    {
        public override void Validate(ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Context ctx)
        {
            foreach (ExtendedDataEntity entity in dataEntities)
            {
                string requestType = entity["RequestType"].ToString();
                string billStatus = Convert.ToString(entity["DocumentStatus"]);
                DynamicObjectCollection entrys = entity["ER_ExpenseReimbEntry"] as DynamicObjectCollection;
                List<string> list = new List<string>();
                list.AddRange(this.ValidateEntry(requestType, entrys, billStatus));
                if (list.Count > 0)
                {
                    string billPKID = entity["ID"].ToString();
                    foreach (string str4 in list)
                    {
                        validateContext.AddError(entity, new ValidationErrorInfo("", billPKID, entity.DataEntityIndex, entity.RowIndex, billPKID, str4, ResManager.LoadKDString("冲借款", "003831000011800", SubSystemType.FIN, new object[0]), ErrorLevel.Error));
                    }
                }

            }
        }
        private List<string> ValidateEntry(string requestType, DynamicObjectCollection entrys, string billStatus)
        {
            List<string> list = new List<string>();
            using (IEnumerator<DynamicObject> enumerator = entrys.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Func<DynamicObject, bool> predicate = null;
                    DynamicObject entry = enumerator.Current;
                    if (predicate == null)
                    {
                        predicate = p => Convert.ToInt64(p["SosurceRowID"]) == Convert.ToInt64(entry["SosurceRowID"]);
                    }
                    List<DynamicObject> source = entrys.Where<DynamicObject>(predicate).ToList<DynamicObject>();
                    bool flag = source.Any<DynamicObject>(p => !string.IsNullOrWhiteSpace(Convert.ToString(p["SourceBillNo"])));
                    bool flag2 = Convert.ToBoolean(entry["IsFromBorrow"]);
                    if (!flag || !flag2)
                    {
                        return list;
                    }
                    //源单可冲销金额
                    decimal num = source.Sum<DynamicObject>((Func<DynamicObject, decimal>)(p => Convert.ToDecimal(p["SrcBorrowAmount"])));
                    Convert.ToDecimal(entry["ExpSubmitAmount"]);//核定报销金额
                    Convert.ToDecimal(entry["ReqSubmitAmount"]);//核定付退款金额   
                    decimal num2 = source.Sum<DynamicObject>((Func<DynamicObject, decimal>)(p => Convert.ToDecimal(p["ExpSubmitAmount"])));
                    decimal num3 = source.Sum<DynamicObject>((Func<DynamicObject, decimal>)(p => Convert.ToDecimal(p["ReqSubmitAmount"])));
                    source.Sum<DynamicObject>((Func<DynamicObject, decimal>)(p => Convert.ToDecimal(p["SrcOffsetAmount"])));
                    decimal num4 = source.Sum<DynamicObject>((Func<DynamicObject, decimal>)(p => Convert.ToDecimal(p["FSRCPAYEDAMOUNT"])));//源单已付款金额
                    string str = requestType;
                    if (str == null)
                    {
                        goto Label_034C;
                    }
                    if (!(str == "1"))
                    {
                        if (str == "2")
                        {
                            goto Label_0269;
                        }
                        goto Label_034C;
                    }
                    if (num > num2)
                    {
                        if (billStatus.Equals("B"))
                        {
                            list.Add(string.Format(ResManager.LoadKDString("申请付款时，核定报销金额不能小于源单可冲销金额：{0}", "003831000013828", SubSystemType.FIN, new object[0]), num));
                        }
                        else
                        {
                            list.Add(string.Format(ResManager.LoadKDString("申请付款时，申请报销金额不能小于源单可冲销金额：{0}", "003831000012434", SubSystemType.FIN, new object[0]), num));
                        }
                    }
                    if (num > (num2 - num3 - num4))
                    {
                        if (billStatus.Equals("B"))
                        {
                            list.Add(string.Format(ResManager.LoadKDString("申请付款时，核定报销金额 - 核定付款金额 - 源单已付款金额不能小于源单可冲销金额：{0}", "003831000013829", SubSystemType.FIN, new object[0]), num));
                        }
                        else
                        {
                            list.Add(string.Format(ResManager.LoadKDString("申请付款时，申请报销金额 - 付款申请金额 - 源单已付款金额不能小于源单可冲销金额：{0}", "003831000012435", SubSystemType.FIN, new object[0]), num));
                        }
                    }
                    continue;
                Label_0269:
                    if (num < num2)
                    {
                        if (billStatus.Equals("B"))
                        {
                            list.Add(string.Format(ResManager.LoadKDString("退款申请时，核定报销金额不能大于源单未下推金额：{0}", "003831000013830", SubSystemType.FIN, new object[0]), num));
                        }
                        else
                        {
                            list.Add(string.Format(ResManager.LoadKDString("退款申请时，申请报销金额不能大于源单未下推金额：{0}", "003831000012436", SubSystemType.FIN, new object[0]), num));
                        }
                    }
                if (num < (num2 + num3 + num4))
                    {
                        if (billStatus.Equals("B"))
                        {
                            list.Add(string.Format(ResManager.LoadKDString("退款申请时，核定报销金额 + 核定退款金额  + 源单已付款金额不能大于源单可冲销金额：{0}", "003831000013831", SubSystemType.FIN, new object[0]), num));
                        }
                        else
                        {
                            list.Add(string.Format(ResManager.LoadKDString("退款申请时，申请报销金额 + 退款申请金额+ 源单已付款金额不能大于源单可冲销金额：{0}", "003831000012437", SubSystemType.FIN, new object[0]), num));
                        }
                    }
                    continue;
                Label_034C:
                    if (num < num2)
                    {
                        if (billStatus.Equals("B"))
                        {
                            list.Add(string.Format(ResManager.LoadKDString("不退不付时, 核定报销金额不能大于源单可冲销金额：{0}", "003831000013832", SubSystemType.FIN, new object[0]), num));
                            continue;
                        }
                        list.Add(string.Format(ResManager.LoadKDString("不退不付时, 申请报销金额不能大于源单可冲销金额：{0}", "003831000012438", SubSystemType.FIN, new object[0]), num));
                    }
                }
            }
            return list;
        }

  
    }
}
