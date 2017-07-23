using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Orm.Metadata.DataEntity;




namespace JN.K3.YDL.App.ServicePlugIn.FIN
{
    [Description("差旅费推付款单据下推后，根据规则拆分行")]

    public class JN_ER_ExpReimbursement_TravelToPayBill : AbstractConvertPlugIn
    {
        /// <summary>
        /// 目标单单据构建完毕，且已经创建好与源单的关联关系之后，触发此事件
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>
        /// 本事件的时机，刚好能够符合需求，
        /// 而AfterConvert事件，则在执行表单服务策略之后
        /// </remarks>
        public override void OnAfterCreateLink(CreateLinkEventArgs e)
        {
            // 目标单单据体元数据
            Entity entity = e.TargetBusinessInfo.GetEntity("FPAYBILLSRCENTRY");
            Entity es = e.SourceBusinessInfo.GetEntity("FEntity");

            // 读取已经生成的付款单
            ExtendedDataEntity[] bills = e.TargetExtendedDataEntities.FindByEntityKey("FBillHead");

            // 定义一个集合，存储新拆分出来的单据体行
            List<ExtendedDataEntity> newRows = new List<ExtendedDataEntity>();
            // 对目标单据进行循环
            foreach (var bill in bills)
            {
                // 取单据体集合
                DynamicObjectCollection rowObjs = entity.DynamicProperty.GetValue(bill.DataEntity)
                    as DynamicObjectCollection;



                // 对单据体进行循环：从后往前循环，新拆分的行，避开循环
                int rowCount = rowObjs.Count;
                int newRowCount = 1;
                double EXCHANGERATE = Convert.ToDouble(bill["EXCHANGERATE"]);
                for (int i = rowCount - 1; i >= 0; i--)
                {
                    DynamicObject rowObj = rowObjs[i];
                    DynamicObject SRCCOSTID = rowObj["SRCCOSTID"] as DynamicObject;
                    string SRCCOSTIDname = Convert.ToString(SRCCOSTID["Name"]);
                    if (SRCCOSTIDname != "路桥费")
                    {
                        double F_JNRoadAmount = Convert.ToDouble(rowObj["F_JNRoadAmount"]);
                        double PLANPAYAMOUNT = Convert.ToDouble(rowObj["PLANPAYAMOUNT"]);
                        double AFTTAXTOTALAMOUNT = Convert.ToDouble(rowObj["AFTTAXTOTALAMOUNT"]);
                        double F_JNTAXAmount = Convert.ToDouble(rowObj["F_JNTAXAmount"]);
                        DynamicObject newRowObj = null;


                        rowObj["PLANPAYAMOUNT"] = PLANPAYAMOUNT - F_JNRoadAmount - F_JNTAXAmount;
                        rowObj["AFTTAXTOTALAMOUNT"] = AFTTAXTOTALAMOUNT - F_JNRoadAmount - F_JNTAXAmount;
                        rowObj["REALPAYAMOUNT"] = rowObj["PLANPAYAMOUNT"];
                        rowObj["F_JNRoadAmount"] = 0;
                        rowObj["FPLANPAYAMOUNTLOC"] = Convert.ToDouble(rowObj["PLANPAYAMOUNT"]) * EXCHANGERATE;
                        rowObj["FREALPAYAMOUNTLOC"] = Convert.ToDouble(rowObj["REALPAYAMOUNT"]) * EXCHANGERATE;
                        rowObj["F_JNSRCNoTaxAmount"] = Convert.ToDouble(rowObj["REALPAYAMOUNT"]);


                        // 根据规则进行拆分：
                        // 示例代码略过拆分规则，强制对每行进行拆分
                        // 通过复制的方式，产生新行：确保字段值、关联关系与原行一致
                        if (F_JNRoadAmount > 0 && SRCCOSTIDname != "路桥费")
                        {
                            //var SRCCOSTID = rowObj["SRCCOSTID"] as DynamicObject;
                            DynamicObject SRCCOST = (DynamicObject)SRCCOSTID.Clone(true, false);
                            newRowObj = (DynamicObject)rowObj.Clone(false, true);
                            //DynamicObject newRowObj = rowObj.Clone(;
                            //DynamicObject newRowObj = (DynamicObject)rowObj.Clone(true, true); 
                            newRowObj["PLANPAYAMOUNT"] = F_JNRoadAmount;
                            newRowObj["AFTTAXTOTALAMOUNT"] = F_JNRoadAmount;
                            newRowObj["F_JNRoadAmount"] = 0;
                            newRowObj["REALPAYAMOUNT"] = F_JNRoadAmount;
                            newRowObj["FPLANPAYAMOUNTLOC"] = F_JNRoadAmount * EXCHANGERATE;
                            newRowObj["FREALPAYAMOUNTLOC"] = F_JNRoadAmount * EXCHANGERATE;
                            newRowObj["F_JNSRCNoTaxAmount"] = F_JNRoadAmount;
                            newRowObj["F_JNTAXAmount"] = 0;
                            newRowObj["FTAXAMOUNT"] = 0;
                            newRowObj["FTAXAMOUNTLOC"] = 0;                           
                            newRowObj["F_JNSRCTAX"] = 0;
                            //newRowObj["Seq"] = i + 1;

                            QueryBuilderParemeter queryParam = new QueryBuilderParemeter();
                            queryParam.FormId = "BD_Expense";

                            queryParam.SelectItems.Add(new SelectorItemInfo("FMasterId"));
                            queryParam.SelectItems.Add(new SelectorItemInfo("FNumber"));
                            queryParam.SelectItems.Add(new SelectorItemInfo("FName"));
                            queryParam.FilterClauseWihtKey = string.Format(" FNumber = '{0}' ", "FYXM23");
                            var expense = QueryServiceHelper.GetDynamicObjectCollection(this.Context, queryParam);
                            newRowObj["SRCCOSTID_Id"] = expense[0]["FMasterId"];
                            SRCCOST["Id"] = expense[0]["FMasterId"];
                            SRCCOST["msterId"] = expense[0]["FMasterId"];
                            SRCCOST["Name"] = expense[0]["FName"];
                            SRCCOST["Number"] = expense[0]["FNumber"];
                            newRowObj["SRCCOSTID"] = SRCCOST;


                            // 把新行，插入到单据中，排在当前行之后
                            rowObjs.Insert(i + 1, newRowObj);

                            //newRowObj["SRCCOSTID_Id"] = 131120;


                        }

                        if (F_JNTAXAmount > 0)
                        {
                            //var SRCCOSTID = rowObj["SRCCOSTID"] as DynamicObject;
                            DynamicObject SRCCOST = (DynamicObject)SRCCOSTID.Clone(true, false);

                            newRowObj = (DynamicObject)rowObj.Clone(false, true);
                            //DynamicObject newRowObj = rowObj.Clone(;
                            //DynamicObject newRowObj = (DynamicObject)rowObj.Clone(true, true); 
                            newRowObj["PLANPAYAMOUNT"] = F_JNTAXAmount;
                            newRowObj["AFTTAXTOTALAMOUNT"] = F_JNTAXAmount;
                            newRowObj["F_JNRoadAmount"] = 0;
                            newRowObj["REALPAYAMOUNT"] = F_JNTAXAmount;
                            newRowObj["FPLANPAYAMOUNTLOC"] = F_JNTAXAmount * EXCHANGERATE;
                            newRowObj["FREALPAYAMOUNTLOC"] = F_JNTAXAmount * EXCHANGERATE;
                            //newRowObj["F_JNSRCNoTaxAmount"] = F_JNTAXAmount;
                            newRowObj["F_JNSRCNoTaxAmount"] = 0;
                            //newRowObj["Seq"] = i + 1;

                            QueryBuilderParemeter queryParam = new QueryBuilderParemeter();
                            queryParam.FormId = "BD_Expense";

                            queryParam.SelectItems.Add(new SelectorItemInfo("FMasterId"));
                            queryParam.SelectItems.Add(new SelectorItemInfo("FNumber"));
                            queryParam.SelectItems.Add(new SelectorItemInfo("FName"));
                            queryParam.FilterClauseWihtKey = string.Format(" FNumber = '{0}' ", "FYXM98");
                            var expense = QueryServiceHelper.GetDynamicObjectCollection(this.Context, queryParam);
                            newRowObj["SRCCOSTID_Id"] = expense[0]["FMasterId"];
                            SRCCOST["Id"] = expense[0]["FMasterId"];
                            SRCCOST["msterId"] = expense[0]["FMasterId"];
                            SRCCOST["Name"] = expense[0]["FName"];
                            SRCCOST["Number"] = expense[0]["FNumber"];
                            newRowObj["SRCCOSTID"] = SRCCOST;


                            // 把新行，插入到单据中，排在当前行之后
                            rowObjs.Insert(i + 1, newRowObj);

                            //newRowObj["SRCCOSTID_Id"] = 131120;



                        }
                        if (newRowObj != null)
                        {
                            // 为新行创建一个ExtendedDataEntity对象，表单服务策略需要此对象
                            ExtendedDataEntity newRow = new ExtendedDataEntity(
                                newRowObj, bill.DataEntityIndex, rowCount + newRowCount);
                            newRows.Add(newRow);

                            newRowCount++;
                        }
                    }
                }
            }

            // 把新拆分出来的单据体行，加入到下推结果中
            // 特别说明：如果去掉此语句，新拆分的行，不会执行表单服务策略
            e.TargetExtendedDataEntities.AddExtendedDataEntities("FEntity", newRows.ToArray());
        }


    }
}

