using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.NetworkCtrl;
using Kingdee.K3.FIN.ServiceHelper;
using Kingdee.K3.FIN.WB.Common.Core;
using Kingdee.K3.FIN.WB.ServiceHelper;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.K3.FIN.WB.Business.PlugIn.Common;

namespace VTR.K3.Bill.PlugIn.band
{
    public class JN_BillBandTrans : WithEbankServiceEdit
    {
        private Context ctx;
        private Dictionary<long, List<long>> dicFidEntrys;
        private string Entry_FEntryID;
        private string Entry_FSeq;
        private DynamicObjectCollection entryDetail;
        private string FAmount;
        private string FEXPLANATION;
        private string FTOBANKACNTID;
        private string FTOBANKACNTName;
        private List<FieldAppearance> listFieldApp;
        private List<NetworkCtrlResult> netCtrlResult;
        private List<long> selectPKID;
        private IDynamicFormView View;
        
        //private List<NetworkCtrlResult> netCtrlResult;

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            if (e.Operation.FormOperation.Operation.EqualsIgnoreCase("SubmitBank"))
            {
                e.Cancel = true;
                //this.DoActionUnderNetworkControl(this.ConvertOperationToBarItemKey(e.Operation.FormOperation.Operation), e);
            }
            if (e.Operation.FormOperation.Operation.EqualsIgnoreCase("CancelWB"))
            {
                //e.Cancel = true;
                // this.DoCancelUnderNetworkControl(e);
            }

        }

        /*
        private void DoCancelUnderNetworkControl(BeforeDoOperationEventArgs e)
        {
            string id = base.View.BusinessInfo.GetForm().Id;
            NetworkCtrlResult result = new NetworkCtrlResult();
            FormMetadata metadata = (FormMetadata)MetaDataServiceHelper.Load(base.Context, "CN_BANKTRANSBILL", true);

            List<FormOperation> operationList = this.GetOperationList();
            List<FormOperation> formOperationList = this.GetOperationList();
          
            FormOperation formOperation = formOperationList.Find(o => o.Id == "CancelWB");
            try
            {
                NetWorkRunTimeParam param = new NetWorkRunTimeParam
                {

                    InterID = base.View.Model.GetPKValue().ToString(),
                    OperationName = formOperation.OperationName,
                    FuncDeatilID = formOperation.Operation,
                    FuncDeatilName = formOperation.OperationName,
                    OperationDesc = string.Format("{0} - {1}", metadata.Name, formOperation.OperationName),
                    BillName = metadata.Name
                };
                result = NetControlServiceHelper.GetMutexOperationNetCtlResult(base.Context, metadata, formOperation, operationList, param, base.View.Model.GetPKValue().ToString(), formOperation.Name);
                if (result.StartSuccess)
                {
                    this.Entry_FSeq = "FEntity_FSeq";
                    this.Entry_FEntryID = "FEntity_FEntryID";
                    this.FTOBANKACNTID = "FTOBANKACNTID";
                    this.FTOBANKACNTName = "FTOBANKACNTName";
                    this.FAmount = "FAmount";
                    this.FEXPLANATION = "FEXPLANATION";
                    this.ctx = base.Context;
                    this.View = base.View;
                    netCtrlResult = new List<NetworkCtrlResult> { result };
                    long num = Convert.ToInt64(base.View.Model.GetPKValue());
                    
                    //new BankTransferCancelWb(base.Context, base.View, netCtrlResult).FindEntryID(new List<long> { num });
                    this.FindEntryID(new List<long> { num });

                }
                else
                {
                    e.Cancel = true;
                    base.View.ShowWarnningMessage(result.Message, "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
                }
            }
            catch (Exception exception)
            {
                throw new KDException(ResManager.LoadKDString("操作不成功!", "003006000001954", SubSystemType.FIN, new object[0]), exception.Message);
            }
            finally
            {
                NetControlServiceHelper.CommitNetCtrl(base.Context, result);
            }

        }

        private List<FormOperation> GetOperationList()
        {
            new List<FormOperation>();
            FormMetadata metadata = (FormMetadata)MetaDataServiceHelper.Load(base.Context, "CN_BANKTRANSBILL", true);
            return metadata.BusinessInfo.GetForm().FormOperations.FindAll(delegate (FormOperation o) {
                if (!(o.Id == "CancelWB") && !(o.Id == "SubmitBank"))
                {
                    return o.Id == "9b9d6c29-8a0a-4916-90d7-3cb128435df7";
                }
                return true;
            });
        }

        public void FindEntryID(List<long> pkID)
        {
            new List<long>();
            this.selectPKID = pkID;
            StringBuilder builder = new StringBuilder();
            builder.Append("FTOBANKACNTID.FNUMBER,FTOBANKACNTID.FNAME,FAmount,FEXPLANATION");
            builder.AppendFormat(",FID,{0},{1}", this.Entry_FEntryID, this.Entry_FSeq);
            builder.Append(",FSubmitStatus");
            string str = string.Format(" FID in ({0}) AND FDOCUMENTSTATUS='C' AND FSubmitStatus ='B' AND  FBankStatus in ('D','E') ", string.Join<long>(",", pkID));
            QueryBuilderParemeter paremeter = new QueryBuilderParemeter
            {
                FormId = "CN_BANKTRANSBILL",
                SelectItems = SelectorItemInfo.CreateItems(builder.ToString()),
                FilterClauseWihtKey = str
            };
            this.entryDetail = QueryServiceHelper.GetDynamicObjectCollection(base.Context, paremeter, null);
            this.BuildK3Displayer(this.entryDetail);
        }



        
                public void FindEntryID(List<long> pkID)
                {
                    new List<long>();
                    this.selectPKID = pkID;
                    StringBuilder builder = new StringBuilder();
                    builder.Append("FTOBANKACNTID.FNUMBER,FTOBANKACNTID.FNAME,FAmount,FEXPLANATION");
                    builder.AppendFormat(",FID,{0},{1}", this.Entry_FEntryID, this.Entry_FSeq);
                    builder.Append(",FSubmitStatus");
                    string str = string.Format(" FID in ({0}) AND FDOCUMENTSTATUS='C' AND FSubmitStatus ='B' AND  FBankStatus in ('D','E') ", string.Join<long>(",", pkID));
                    QueryBuilderParemeter paremeter = new QueryBuilderParemeter
                    {
                        FormId = "CN_BANKTRANSBILL",
                        SelectItems = SelectorItemInfo.CreateItems(builder.ToString()),
                        FilterClauseWihtKey = str
                    };
                    this.entryDetail = QueryServiceHelper.GetDynamicObjectCollection(this.ctx, paremeter, null);
                    this.BuildK3Displayer(this.entryDetail);
                }




                private void BuildK3Displayer(DynamicObjectCollection result)
                {
                    if (result.Count == 0)
                    {
                        //this.View.ShowMessage(ResManager.LoadKDString("所选择的数据不存在银行处理状态为“银行交易失败”或者“银行交易未确认”的分录行，本次操作不成功", "003279000009517", SubSystemType.FIN, new object[0]), MessageBoxType.Notice);
                        //this.View.ShowMessage("所选择的数据不存在银行处理状态为“银行交易失败”或者“银行交易未确认”的分录行，本次操作不成功", MessageBoxType.Notice);
                        this.ClearNetWork();
                    }
                    else
                    {
                        K3DisplayerModel displayer = K3DisplayerModel.Create(base.Context, this.K3DisplayFields().ToArray(), null);
                        new K3DisplayerMessage();
                        foreach (DynamicObject obj2 in result)
                        {
                            bool flag = false;
                            string str = obj2[this.FEXPLANATION].IsNullOrEmptyOrWhiteSpace() ? "  " : obj2[this.FEXPLANATION].ToString();
                            string str2 = obj2["FTOBANKACNTID_FNUMBER"].IsNullOrEmptyOrWhiteSpace() ? " " : obj2["FTOBANKACNTID_FNUMBER"].ToString();
                            string str3 = obj2["FTOBANKACNTID_FNAME"].IsNullOrEmptyOrWhiteSpace() ? " " : obj2["FTOBANKACNTID_FNAME"].ToString();
                            string message = string.Format("{0}~|~{1}~|~{2}~|~{3}~|~{4}~|~{5}~|~{6}~|~{7}", new object[] { flag, str2, str3, obj2[this.FAmount], str, obj2[this.Entry_FSeq], obj2[this.Entry_FEntryID], obj2["FID"] });
                            displayer.AddMessage(message);
                        }
                        displayer.SummaryMessage = ResManager.LoadKDString("选择需要撤销的数据！银行处理状态为交易未确认的，建议通过银行状态码和银行确认后再撤销.", "003279000009518", SubSystemType.FIN, new object[0]);
                        this.View.ShowK3Displayer(displayer, new Action<FormResult>(this.SubmitBankCallBack), "WB_K3Displayer");
                    }
                }

                private void ClearNetWork()
                {
                    foreach (NetworkCtrlResult result in this.netCtrlResult)
                    {
                        NetControlServiceHelper.CommitNetCtrl(base.Context, result);
                    }
                }

                private List<FieldAppearance> K3DisplayFields()
                {
                    if ((this.listFieldApp == null) || (this.listFieldApp.Count <= 0))
                    {
                        this.listFieldApp = new List<FieldAppearance>();
                        FieldAppearance item = K3DisplayerUtil.CreateDisplayerField<CheckBoxFieldAppearance, CheckBoxField>(this.ctx, "FIsSelected", " ", "", null);
                        item.Locked = 0;
                        item.Width = new LocaleValue("20", this.ctx.UserLocale.LCID);
                        this.listFieldApp.Add(item);
                        item = K3DisplayerUtil.CreateDisplayerField<TextFieldAppearance, TextField>(this.ctx, this.FTOBANKACNTID, ResManager.LoadKDString("转入账号", "003279000007831", SubSystemType.FIN, new object[0]), "", null);
                        item.Width = new LocaleValue("80", this.ctx.UserLocale.LCID);
                        item.Locked = -1;
                        this.listFieldApp.Add(item);
                        item = K3DisplayerUtil.CreateDisplayerField<TextFieldAppearance, TextField>(this.ctx, this.FTOBANKACNTName, ResManager.LoadKDString("转入账户名称", "003279000007834", SubSystemType.FIN, new object[0]), "", null);
                        item.Width = new LocaleValue("80", this.ctx.UserLocale.LCID);
                        item.Locked = -1;
                        this.listFieldApp.Add(item);
                        item = K3DisplayerUtil.CreateDisplayerField<PriceFieldAppearance, PriceField>(this.ctx, this.FAmount, ResManager.LoadKDString("转入金额", "003279000007837", SubSystemType.FIN, new object[0]), "", null);
                        item.Locked = -1;
                        item.Width = new LocaleValue("80", this.ctx.UserLocale.LCID);
                        (item.Field as DecimalField).FieldScale = 2;
                        this.listFieldApp.Add(item);
                        item = K3DisplayerUtil.CreateDisplayerField<TextFieldAppearance, TextField>(this.ctx, this.FEXPLANATION, ResManager.LoadKDString("摘要", "003279000007840", SubSystemType.FIN, new object[0]), "", null);
                        item.Width = new LocaleValue("80", this.ctx.UserLocale.LCID);
                        item.Locked = -1;
                        this.listFieldApp.Add(item);
                        item = K3DisplayerUtil.CreateDisplayerField<IntegerFieldAppearance, IntegerField>(this.ctx, "FSeq", ResManager.LoadKDString("对应行号", "003279000007843", SubSystemType.FIN, new object[0]), "", null);
                        item.Width = new LocaleValue("2", this.ctx.UserLocale.LCID);
                        item.Visible = 0;
                        this.listFieldApp.Add(item);
                        item = K3DisplayerUtil.CreateDisplayerField<IntegerFieldAppearance, IntegerField>(this.ctx, "FEntiyID", "FEntiyID", "", null);
                        item.Width = new LocaleValue("2", this.ctx.UserLocale.LCID);
                        item.Visible = 0;
                        this.listFieldApp.Add(item);
                        item = K3DisplayerUtil.CreateDisplayerField<IntegerFieldAppearance, IntegerField>(this.ctx, "FID", "FID", "", null);
                        item.Width = new LocaleValue("2", this.ctx.UserLocale.LCID);
                        item.Visible = 0;
                        this.listFieldApp.Add(item);
                    }
                    return this.listFieldApp;
                }


                private void SubmitBankCallBack(FormResult opResult)
                {
                    if (((opResult == null) || !(opResult.ReturnData is K3DisplayerModel)) || !(opResult.ReturnData as K3DisplayerModel).IsOK)
                    {
                        this.ClearNetWork();
                    }
                    else
                    {
                        int num = 0;
                        bool flag = true;
                        decimal num2 = 0M;
                        K3DisplayerModel returnData = opResult.ReturnData as K3DisplayerModel;
                        new OperateResult();
                        new OperateResult();
                        OperateResultCollection operateResults = new OperateResultCollection();
                        this.dicFidEntrys = new Dictionary<long, List<long>>();
                        if ((returnData != null) && returnData.BarItemKey.Equals("tbOK"))
                        {
                            foreach (K3DisplayerMessage message in returnData.Messages)
                            {
                                if ((message != null) && Convert.ToBoolean(message.DataEntity["FIsSelected"]))
                                {
                                    Convert.ToInt32(message.DataEntity["FSeq"]);
                                    long item = Convert.ToInt64(message.DataEntity["FEntiyID"]);
                                    long num4 = Convert.ToInt64(message.DataEntity["FID"]);
                                    if (this.dicFidEntrys.Keys.Contains<long>(num4))
                                    {
                                        this.dicFidEntrys[num4].Add(item);
                                    }
                                    else
                                    {
                                        List<long> list = new List<long> {
                                    item
                                };
                                        this.dicFidEntrys.Add(num4, list);
                                    }
                                    num++;
                                    num2 += Convert.ToDecimal(message.DataEntity[this.FAmount]);
                                }
                            }
                        }
                        if (this.dicFidEntrys.Count < 1)
                        {
                            this.ClearNetWork();
                        }
                        else if (!flag)
                        {
                            this.BuildK3Displayer(this.entryDetail);
                            this.View.ShowOperateResult(operateResults, "BOS_BatchTips");
                        }
                        else
                        {
                            num2 = CommonCoreFuncWb.SubDecimalValue(num2, 2);
                            string msg = string.Format(ResManager.LoadKDString("总金额为{0},总笔数为{1}。是否继续？", "003279000009519", SubSystemType.FIN, new object[0]), num2, num);
                            this.View.ShowMessage(msg, MessageBoxOptions.YesNo, delegate (MessageBoxResult result) {
                                if (result.Equals(MessageBoxResult.Yes))
                                {
                                    //this.SubmitBank();
                                }
                                if (result.Equals(MessageBoxResult.No))
                                {
                                    this.BuildK3Displayer(this.entryDetail);
                                }
                            }, "", MessageBoxType.Notice);
                        }
                    }
                }


                private void SubmitBank()
                {
                    OperateResultCollection operateResults = null;
                    try
                    {
                        operateResults = PayBillSubmitToBankPayHelper.CancelTransferToBankPay(this.ctx, this.dicFidEntrys);
                    }
                    catch (Exception exception)
                    {
                        if (exception.Message.ToString().Contains(ResManager.LoadKDString("权限", "003279000009520", SubSystemType.FIN, new object[0])))
                        {
                            this.View.ShowWarnningMessage(ResManager.LoadKDString("没有银行付款单的新增权限，提交银行失败！", "003279000007846", SubSystemType.FIN, new object[0]), "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
                        }
                    }
                    finally
                    {
                        this.ClearNetWork();
                    }
                    if ((operateResults != null) && (operateResults.Count > 0))
                    {
                        if (operateResults.GetSuccessResult().Count > 0)
                        {
                            this.View.InvokeFormOperation("Refresh");
                        }
                        this.View.ShowOperateResult(operateResults, "BOS_BatchTips");
                    }
                }
                */
    }











}
