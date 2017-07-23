using Kingdee.BOS;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.BD.Contracts;
using Kingdee.K3.Core.BD;
using Kingdee.K3.Core.BD.ServiceArgs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.ForecastBalance
{
    /// <summary>
    /// 销售预测单结余表调整结余插件
    /// </summary>
    [Description("销售预测单结余表调整结余服务端插件")]
    public class JNBalance : AbstractOperationServicePlugIn
    {
        //调整方向 A:减少 B:增加
        string sAdjustType = "";
        //符合调整的数据
        List<DynamicObject> lstEntrys;
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
        }

        public override void OnPrepareOperationServiceOption(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.OnPrepareOperationServiceEventArgs e)
        {
            base.OnPrepareOperationServiceOption(e);
            if (base.Option.TryGetVariableValue("AdjustTypeParams", out sAdjustType))
            {
                sAdjustType = base.Option.GetVariableValue<String>("AdjustTypeParams");
            }
            if (base.Option.TryGetVariableValue("DynamicObjectParams", out lstEntrys))
            {
                lstEntrys = base.Option.GetVariableValue<List<DynamicObject>>("DynamicObjectParams");
            }
        }

        public override void EndOperationTransaction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (sAdjustType == null || sAdjustType == "")
            {
                return;
            }
            if (lstEntrys == null || lstEntrys.Count() <= 0)
            {
                return;
            }
            //创建销售预测变更单
            CreateForecastChangeBill();
        }

        //创建销售预测变更单
        private void CreateForecastChangeBill()
        {
            //分组集合
            List<string> lstGroups = new List<string>();
            string sGroup = string.Empty;
            foreach (DynamicObject lstEntry in lstEntrys)
            {
                sGroup = Convert.ToString(lstEntry["FSaleOrgId_ID"]) + "+" + Convert.ToString(lstEntry["FSaleDeptId_ID"]) + "+" +
                    Convert.ToString(lstEntry["FSaleGroupId_ID"]) + "+" + Convert.ToString(lstEntry["FSalerId_ID"]);
                if (!lstGroups.Contains(sGroup))
                {
                    lstGroups.Add(sGroup);
                }
            }

            //插入数据

            //销售预测变更单的信息
            BusinessInfo businessInfo = ((FormMetadata)MetaDataServiceHelper.Load(this.Context, "JN_YDL_SAL_ForecastChange", true)).BusinessInfo;

            DynamicObject billHead = null;
            DynamicObjectCollection billEntrys = null;
            List<DynamicObject> lstBills = new List<DynamicObject>();
            List<DynamicObject> lstSelectEntrys;

            long lSaleOrgId = 0;
            long lSaleDeptId = 0;
            long lSaleGroupId = 0;
            long lSalerId = 0;

            foreach (string lstGroup in lstGroups)
            {
                //单据头
                billHead = businessInfo.GetDynamicObjectType().CreateInstance() as DynamicObject;
                //单据体
                billEntrys = billHead["FEntity"] as DynamicObjectCollection;

                if (billHead == null || billEntrys == null)
                {
                    continue;
                }

                string[] sGroupSplit = lstGroup.Split('+');

                if (sGroupSplit == null || sGroupSplit.Count() != 4)
                {
                    continue;
                }

                lSaleOrgId = Convert.ToInt64(sGroupSplit[0]);
                lSaleDeptId = Convert.ToInt64(sGroupSplit[1]);
                lSaleGroupId = Convert.ToInt64(sGroupSplit[2]);
                lSalerId = Convert.ToInt64(sGroupSplit[3]);

                billHead["FBillTypeID_Id"] = "58b2a721c7f776";
                billHead["FJNSaleOrgId_Id"] = lSaleOrgId;
                billHead["FJNSaleDeptId_Id"] = lSaleDeptId;
                billHead["FJNSaleGroupId_Id"] = lSaleGroupId;
                billHead["FJNSalerId_Id"] = lSalerId;
                billHead["FDocumentStatus"] = "A";
                billHead["FJNDate"] = DateTime.Now;
                billHead["FDirection"] = sAdjustType == "A" ? "B" : "A";
                billHead["FCreateDate"] = DateTime.Now;
                billHead["FCreatorId_Id"] = this.Context.UserId;

                lstSelectEntrys = lstEntrys.Where(p => Convert.ToInt64(p["FSaleOrgId_Id"]) == lSaleOrgId && Convert.ToInt64(p["FSaleDeptId_Id"]) == lSaleDeptId && Convert.ToInt64(p["FSaleGroupId_Id"]) == lSaleGroupId && Convert.ToInt64(p["FSalerId_Id"]) == lSalerId).ToList();

                if (lstSelectEntrys == null || lstSelectEntrys.Count() <= 0)
                {
                    continue;
                }

                int seq = 1;
                foreach (DynamicObject entry in lstSelectEntrys)
                {
                    DynamicObject billEntry = new DynamicObject(billEntrys.DynamicCollectionItemPropertyType);
                    billEntry["Seq"] = seq;
                    billEntry["FJNMaterialId_Id"] = entry["FMaterialId_Id"];
                    billEntry["FJNAUXPROP_Id"] = entry["FAuxPropId_Id"];
                    billEntry["FJNForecastQty"] = entry["FAdjustQty"];
                    billEntry["FJNUnitID_Id"] = entry["FUnitID_Id"];
                    billEntry["FJNBaseUnitID_Id"] = entry["FUnitID_Id"];
                    billEntry["FJNBaseUnitQty"] = entry["FAdjustQty"];

                    billEntry["FJNStockOrg_Id"] = this.Context.CurrentOrganizationInfo.ID;
                    billEntry["FJNSettleOrg_Id"] = this.Context.CurrentOrganizationInfo.ID;
                    billEntry["FJNSupplyOrg_Id"] = this.Context.CurrentOrganizationInfo.ID;
                    DynamicObject material = entry["FMaterialId"] as DynamicObject;
                    if (material != null)
                    {
                        string Materialname = material["Name"].ToString();
                        string sql = "";
                        if (Materialname.IndexOf("(内蒙）") > 0 || Materialname.IndexOf("(内蒙)") > 0 || Materialname.IndexOf("（内蒙）") > 0)
                        {
                            billEntry["FJNSupplyOrg_Id"] = 100063;
                            sql = string.Format(@"select t2.FWORKSHOPID  as FWORKSHOPID from T_BD_MATERIAL  t1
join T_BD_MATERIALPRODUCE t2 on t1.FMATERIALID=t2.FMATERIALID
where t1.FMASTERID in(
select FMASTERID from T_BD_MATERIAL where FMATERIALID={0})
and t1.FUSEORGID={1}", Convert.ToInt32(entry["FMaterialId_Id"]), 100063);
                        }
                        else
                        {
                            billEntry["FJNSupplyOrg_Id"] = 100062;
                            sql = string.Format(@"select t2.FWORKSHOPID  as FWORKSHOPID from T_BD_MATERIAL  t1
join T_BD_MATERIALPRODUCE t2 on t1.FMATERIALID=t2.FMATERIALID
where t1.FMASTERID in(
select FMASTERID from T_BD_MATERIAL where FMATERIALID={0})
and t1.FUSEORGID={1}", Convert.ToInt32(entry["FMaterialId_Id"]), 100062);
                        }

                        DynamicObjectCollection FWORKSHOPID = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                        if (FWORKSHOPID.Count > 0)
                        {
                            int WORKSHOP = Convert.ToInt32(FWORKSHOPID[0]["FWORKSHOPID"]);
                            billEntry["F_VTR_PrdDeptId_Id"] = WORKSHOP;
                        }
                       /* else
                        {
                            billEntry["F_VTR_PrdDeptId"] = 0;
                        }*/
                    }

                    billEntrys.Add(billEntry);
                    seq++;
                }

                lstBills.Add(billHead);
            }

            if (lstBills.Count <= 0)
            {
                return;
            }

            //生成编码
            MakeBillNo(lstBills);

            Kingdee.BOS.ServiceHelper.DBServiceHelper.LoadReferenceObject(this.Context, lstBills.ToArray(), businessInfo.GetDynamicObjectType(), false);
            DynamicObject[] billDatas = BusinessDataServiceHelper.Save(this.Context, lstBills.ToArray());

            if (billDatas == null || billDatas.Count() <= 0)
            {
                //K3DisplayerModel model = K3DisplayerModel.Create(this.Context, "调整失败，未成功创建销售预测变更单");
                //// 创建一个交互提示错误对象，并设置错误来源，相互隔离
                //KDInteractionException ie = new KDInteractionException("错误提示");
                //ie.InteractionContext.InteractionFormId = FormIdConst.BOS_K3Displayer; // 提示信息显示界面
                //ie.InteractionContext.K3DisplayerModel = model; // 提示内容
                //ie.InteractionContext.IsInteractive = true; // 是否需要交互
                //throw ie; // 抛出错误，终止流程
                this.OperationResult.OperateResult[0].SuccessStatus = false;
                this.OperationResult.OperateResult[0].Message = "创建销售预测变更单失败";
            }
            else
            {
                OperateResult result;
                foreach (var item in billDatas)
                {
                    result = new OperateResult
                    {
                        SuccessStatus = true,
                        Message = "创建销售预测变更单成功",
                        MessageType = MessageType.Normal,
                        Name = "生成销售预测变更单:" + Convert.ToString(item["FBillNo"]) + "成功",
                        PKValue = item
                    };
                    this.OperationResult.OperateResult.Add(result);
                }
            }
            this.OperationResult.IsShowMessage = true;
        }


        /// <summary>
        /// 获取单位数量
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="msterID">物料ID</param>
        /// <param name="sourceUintId">基本单位ID</param>
        /// <param name="destUnitId">转换的单位Id</param>
        /// <param name="sourceQty">基本单位数量</param>
        /// <returns></returns>
        private decimal GetConvertedQty(Context ctx, long msterID, long sourceUintId, long destUnitId, decimal sourceQty)
        {
            UnitConvert convert = Kingdee.BOS.App.ServiceHelper.GetService<IUnitConvertService>().GetUnitConvertRate(ctx,
                new GetUnitConvertRateArgs()
                {
                    MasterId = msterID,
                    SourceUnitId = sourceUintId,
                    DestUnitId = destUnitId
                });

            return convert.ConvertQty(sourceQty);
        }


        /// <summary>
        /// 生成id和编码
        /// </summary>
        /// <param name="bardCodes"></param>
        private void MakeBillNo(List<DynamicObject> lstDynamicObj)
        {
            IDBService service = Kingdee.BOS.Contracts.ServiceFactory.GetService<IDBService>(this.Context);

            long[] ids = service.GetSequenceInt64(Context, "JN_T_SAL_ForecastChange", lstDynamicObj.Count()).ToArray();

            Dictionary<string, object> options = new Dictionary<string, object>();
            options["CodeTime"] = 1;
            options["UpdateMaxNum"] = 1;

            List<BillNoInfo> billNos = Kingdee.BOS.ServiceHelper.BusinessDataServiceHelper.GetBillNo(this.Context, "JN_YDL_SAL_ForecastChange", lstDynamicObj.ToArray(), options);

            for (int i = 0; i < billNos.Count; i++)
            {
                lstDynamicObj[i]["FBillNo"] = billNos[i].BillNo;
                lstDynamicObj[i]["Id"] = ids[i];

                var entitys = lstDynamicObj[i]["FEntity"] as DynamicObjectCollection;
                if (entitys != null && entitys.Count > 0)
                {
                    long[] entryIds = DBServiceHelper.GetSequenceInt64(this.Context, "JN_T_SAL_ForecastChangeEntry", entitys.Count()).ToArray();
                    for (int j = 0; j < entitys.Count; j++)
                    {
                        entitys[j]["Id"] = entryIds[j];
                    }
                }
            }
        }
    }
}
