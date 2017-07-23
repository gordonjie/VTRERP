using JN.K3.YDL.App.ServicePlugIn.Bandservice;
using JN.K3.YDL.Core;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.Bandservice
{
    public class paybillband :JNBand
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FACCOUNTID");
            e.FieldKeys.Add("FPAYACCOUNTNAME");
            e.FieldKeys.Add("FPAYBANKID");
            e.FieldKeys.Add("FOPPOSITEBANKACCOUNT");
            e.FieldKeys.Add("FOPPOSITECCOUNTNAME");
            e.FieldKeys.Add("FOPPOSITEBANKNAME");
            e.FieldKeys.Add("FOpenAddressRec");//开户行地址
            e.FieldKeys.Add("FCNAPS");//联行号
            e.FieldKeys.Add("FBankTypeRec");//收款银行
            e.FieldKeys.Add("FPAYACCOUNTNAME");
            e.FieldKeys.Add("FSETTLETYPEID");//结算方式
            

            e.FieldKeys.Add("FPAYORGID");
            e.FieldKeys.Add("FBillTypeID");

            e.FieldKeys.Add("FRecType");//收款类型
            e.FieldKeys.Add("F_VTR_Bocflag");//是否跨行
            

        }

        /// <summary>
        /// 操作执行前逻辑
        /// </summary>
        /// <param name="e"></param>
        public override void BeginOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeginOperationTransactionArgs e)
        {
            JNBandPara actband = new JNBandPara();
            JNBandPara toband = new JNBandPara();
            double payamount = 0;
            var billGroups = e.DataEntitys;
            string database = this.Context.DataCenterName;
            string token ="";
            int i=0;
            foreach (var billGroup in billGroups)
            {
                DynamicObject BillType = billGroup["BillTypeID"] as DynamicObject;
                string BillTypeName = Convert.ToString(BillType["name"]);
                if (BillTypeName != "其他业务付款单")
                {

                    DynamicObjectCollection BILLENTRYDATAs = billGroup["PAYBILLENTRY"] as DynamicObjectCollection;
                    foreach (var BILLENTRYDATA in BILLENTRYDATAs)
                    {
                        DynamicObject FrACCOUNT = BILLENTRYDATA["FACCOUNTID"] as DynamicObject;
                        int SETTLETYPEID = Convert.ToInt16(BILLENTRYDATA["SETTLETYPEID_Id"]);
                        if (FrACCOUNT != null)
                        {
                            DynamicObject FrBAND = FrACCOUNT["BANKID"] as DynamicObject;
                            if (FrBAND != null && SETTLETYPEID == 4)
                            {
                                actband.bandid = Convert.ToInt32(BILLENTRYDATA["FACCOUNTID_Id"]);
                                //actband.addr = Convert.ToString(FrACCOUNT["BANKADDRESS"]);
                                actband.name = Convert.ToString(FrACCOUNT["Name"]);
                                actband.bandnum = Convert.ToString(FrACCOUNT["ACNTBRANCHNUMBER"]);
                                actband.cn = Convert.ToString(FrACCOUNT["NUMBER"]);
                                actband.bandname = Convert.ToString(FrBAND["Name"]);


                                toband.addr = Convert.ToString(BILLENTRYDATA["OpenAddressRec"]);
                                toband.cn = Convert.ToString(BILLENTRYDATA["OPPOSITEBANKACCOUNT"]);
                                toband.bandname = Convert.ToString(BILLENTRYDATA["OPPOSITEBANKNAME"]);
                                toband.name = Convert.ToString(BILLENTRYDATA["OPPOSITECCOUNTNAME"]);
                                payamount = Convert.ToDouble(BILLENTRYDATA["REALPAYAMOUNTFOR"]);
                                Int32 EntryID = Convert.ToInt32(BILLENTRYDATA["Id"]);
                                if (i == 0)//首单获取令牌
                                {
                                    token = checkin(this.Context, actband);
                                    i++;
                                }
                                //判断对公对私业务
                                int RecType = Convert.ToInt16(BILLENTRYDATA["RecType"]);
                                string result = "";
                                if (RecType == 0)//对公
                                {
                                    result = BtoBPay(this.Context, actband, toband, payamount, "", token);
                                }
                                else//对私
                                {
                                    int F_VTR_Bocflag = Convert.ToInt32(BILLENTRYDATA["F_VTR_Bocflag"]);
                                    result = BtoCPay(this.Context, actband, toband, payamount, "", F_VTR_Bocflag, token);
                                }
                                if (result.Length > 0)
                                {
                                    BusinessDataServiceHelper.SetState(this.Context, "T_AP_PAYBILLENTRY_B", "FSUBMITSTATUS", "B", "FEntryID", new object[] { EntryID });
                                    //BusinessDataServiceHelper.s
                                    string sql = string.Format("update T_AP_PAYBILLENTRY_B set F_JNobssid={0} where FEntryID={1}", result, EntryID.ToString());
                                    DBUtils.Execute(this.Context, sql);
                                }
                                else
                                {
                                    // 定义交互消息标识，以与其他交互消息区分开
                                    string spensorKey = "JNbandAudit.ServicePlugIn.Operation.S160425ShowInteractionOpPlug.ShowK3DisplayMessage";
                                    // 提示信息的列标题，以“~|~”分开两列
                                    string titleMsg = "提交银行失败";
                                    // 对应的提示信息格式，以"~|~"分开两列，以{n}进行占位
                                    string errMsg = "提交银行失败";
                                    K3DisplayerModel model = K3DisplayerModel.Create(Context, titleMsg);
                                    // 消息内容：可以添加多行
                                    string rowMsg = string.Format(errMsg, "提交银行失败");
                                    ((K3DisplayerModel)model).AddMessage(rowMsg);
                                    model.Option.SetVariableValue(K3DisplayerModel.CST_FormTitle, "本节点为最后节点，是否继续完成审批？");
                                    // 是否继续按钮
                                    model.FieldAppearances[1].Width = new LocaleValue("300");


                                    model.OKButton.Visible = true;
                                    model.OKButton.Caption = new LocaleValue("继续", Context.UserLocale.LCID);
                                    model.CancelButton.Visible = true;
                                    model.CancelButton.Caption = new LocaleValue("取消", Context.UserLocale.LCID);
                                    // 创建一个交互提示错误对象KDInteractionException：
                                    // 通过throw new KDInteractionException()的方式，向操作调用者，输出交互信息
                                    KDInteractionException ie = new KDInteractionException(this.Option, spensorKey);
                                    // 提示信息显示界面
                                    ie.InteractionContext.InteractionFormId = Kingdee.BOS.Core.FormIdConst.BOS_K3Displayer;
                                    // 提示内容
                                    ie.InteractionContext.K3DisplayerModel = model;
                                    // 是否需要交互
                                    ie.InteractionContext.IsInteractive = true;
                                    // 抛出错误，终止流程
                                    throw ie;
                                }
                            }
                        }
                    }
                    //i++;
                }
            }

            if (token.Length > 1)
            {
                checkout(this.Context, actband);
            }
            

           }
        }
}
