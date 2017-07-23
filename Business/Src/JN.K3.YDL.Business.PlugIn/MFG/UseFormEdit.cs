using JN.K3.YDL.Core;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Util;
using JN.BOS.Business.BillHelper;
using Kingdee.BOS.Core.Metadata.FieldElement;
 

namespace JN.K3.YDL.Business.PlugIn.MFG
{
    /// <summary>
    /// 配方单-表单插件
    /// </summary>
    [Description("配方单-表单插件")]
    public class UseFormEdit : AbstractBillPlugIn
    {
        /// <summary>
        /// 分录菜单点击事件
        /// </summary>
        /// <param name="e"></param>
        public override void EntryBarItemClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BarItemClickEventArgs e)
        {
            base.EntryBarItemClick(e);

            #region 检验查询
            if (e.BarItemKey == "tbInspectSerch")
            {
                ShowInspectBill();
            }
            #endregion
            #region 发起请检
            if (e.BarItemKey == "tbToInspect")
            {
                ReqInspect();
            }
            #endregion
            #region 酶活维护
            if (e.BarItemKey == "tbEnzymeMainTain")
            {
                ModifyEnzyme();
            }
            #endregion
            #region 计算载体
            if (e.BarItemKey == "tbCountCarrier")
            {
                CalCarrier();
            }
            #endregion

            if (e.BarItemKey.Equals("tbQueryStock"))
            {
                QureyInvQty();
                e.Cancel = true;
            }
            #region 删除控制
            if (e.BarItemKey == "tbDeleteEntry")
            {
                int index = this.View.Model.GetEntryCurrentRowIndex("FEntity");
                decimal count = Convert.ToDecimal(this.View.Model.GetValue("FPickedQty", index));
                if (count > 0)
                {
                    this.View.ShowMessage("该物料已领数量大于0不允许删除！");
                    e.Cancel = true;
                }
            }
            #endregion
        }

        /// <summary>
        /// 分录双击事件
        /// </summary>
        /// <param name="e"></param>
        public override void EntityRowDoubleClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            if (e.Row > -1 && this.View.Model.DataObject["DocumentStatus"].ToString() != "C")
            {
                ModifyEnzyme();
            }
        }
         
        /// <summary>
        /// 计算载体
        /// </summary> 
        private void CalCarrier()
        {
            decimal measureSum = 0;     //双计量总数
            decimal carrier = 0;            //载体个数            
            long count = Convert.ToInt64(this.View.Model.GetValue("FQty"));
            DynamicObjectCollection dataEntity = this.View.Model.DataObject["PPBomEntry"] as DynamicObjectCollection;
            foreach (DynamicObject item in dataEntity)
            {
                DynamicObject metaril = item["MaterialID"] as DynamicObject;
                if (metaril == null) continue;
                if (Convert.ToBoolean(metaril["FIsMeasure"]) && !Convert.ToBoolean(metaril["F_JN_IsEnzyme"]))
                {
                    measureSum += Convert.ToDecimal(item["MustQty"]);
                }
                if (Convert.ToBoolean(metaril["F_JN_IsCarrier"]))
                {
                    carrier += Convert.ToDecimal(metaril["F_JN_CarrierXi"]);//载体分摊系数
                }
            }   
            int i = 1;
            long stCarrier = 0;            
            foreach (DynamicObject item in dataEntity)
            {
                DynamicObject metaril = item["MaterialID"] as DynamicObject;
                if (metaril == null || !Convert.ToBoolean(metaril["F_JN_IsCarrier"])) continue;
                decimal xiShu = Convert.ToDecimal(metaril["F_JN_CarrierXi"]);//载体分摊系数                
                long carrierQty = 0;
                if (i < dataEntity.Count && xiShu > 0 && (count - measureSum) > 0)
                {
                    long qty = Convert.ToInt64(((count - measureSum) * xiShu / carrier));
                    if (qty % 10 >= 5)
                    {
                        carrierQty = qty / 10 * 10 + 10;
                    }
                    else {
                        carrierQty = qty / 10 * 10;//载体数量，精确到10位
                    }                    
                    stCarrier += carrierQty;
                }
                else if (i == dataEntity.Count && xiShu > 0 && (count - stCarrier) > 0)
                {
                    long qtyLast = count - stCarrier;
                    if (qtyLast % 10 >= 5)
                    {
                        carrierQty = qtyLast / 10 * 10 + 10;
                    }
                    else {
                        carrierQty = qtyLast / 10 * 10;
                    }                    
                }
                this.View.Model.SetValue("FMustQty", carrierQty, dataEntity.IndexOf(item));
                i++;
            }
            this.View.UpdateView("FEntity");
        }

        /// <summary>
        /// 酶活维护
        /// </summary>   
        private void ModifyEnzyme()
        {
            decimal sumQty = 0;     //酶活总量
            int materialId = 0;
            bool isEnzyme = false;  //是否双计量
            int row = this.View.Model.GetEntryCurrentRowIndex("FEntity");
            DynamicObjectCollection dataEntity = this.View.Model.DataObject["PPBomEntry"] as DynamicObjectCollection;
            if (dataEntity[row]["MaterialID"] != null)
            {
                DynamicObject material = dataEntity[row]["MaterialID"] as DynamicObject;
                materialId = Convert.ToInt32(material["Id"]);
                isEnzyme = Convert.ToBoolean(material["F_JN_IsEnzyme"].ToString());
            }
            sumQty = Convert.ToDecimal(dataEntity[row]["F_JN_EnzymeSumQty"]);
            if (materialId == 0)
            {
                this.View.ShowMessage ("请先录入子项物料！");
                return;
            }
            else if (sumQty == 0)
            {
                this.View.ShowMessage("请先计算出酶活总量，再进行此酶活维护操作！");
                return;
            }
            long OrgId = 0;
            OrgId = GetOrgId(row);
            if (this.View.Model.GetValue("F_JN_BeEnzyme", row) != null)
            {
                DynamicObject auxData = this.View.Model.GetValue("FAuxPropID", row) as DynamicObject;
                if (auxData != null && Convert.ToInt64(auxData["Id"]) == 0)
                {
                    RelatedFlexGroupField flexField = this.View.BillBusinessInfo.GetField("FAuxPropID") as RelatedFlexGroupField;
                    long auxPropId = LinusFlexUtil.GetFlexDataId(this.View.Context, auxData, flexField);
                    this.View.Model.SetValue("FAuxPropID", auxPropId, row);
                }              
                int enzymeId = Convert.ToInt32(dataEntity[row]["F_JN_BeEnzyme_Id"]);
                decimal enzymeSum = Convert.ToDecimal(this.View.Model.GetValue("F_JN_BeEnzymeQty", row));                
                DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                showParam.PageId = Guid.NewGuid().ToString();
                showParam.FormId = "JN_YDL_EnzymeMaintain";
                showParam.ShowMaxButton = true;
                showParam.OpenStyle.ShowType = ShowType.Floating;
                showParam.CustomParams.Add("F_JN_Element", enzymeId.ToString());//打开动态表单时传递的参数
                showParam.CustomParams.Add("F_JN_EnzymeNeed", enzymeSum.ToString());
                showParam.CustomComplexParams.Add("Data", dataEntity);
                showParam.CustomComplexParams.Add("OrgId", OrgId);
                this.View.ShowForm(showParam, returnData);
            }
            else
            {
                if (!isEnzyme)
                {
                    this.View.ShowMessage("该物料并非酶种物料，不能进行酶活维护操作！");
                    return;
                }
                DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                showParam.PageId = Guid.NewGuid().ToString();
                showParam.FormId = "JN_YDL_EnzymeMaintain";
                showParam.OpenStyle.ShowType = ShowType.Floating;
                showParam.CustomParams.Add("F_JN_Element", materialId.ToString());//打开动态表单时传递的参数
                showParam.CustomParams.Add("F_JN_EnzymeNeed", sumQty.ToString());
                showParam.CustomComplexParams.Add("OrgId", OrgId);
                this.View.ShowForm(showParam, returnData);
            }
        }

        /// <summary>
        /// 获取组织
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private long GetOrgId(int row)
        {
            int index = 0;
            var fld = this.View.BusinessInfo.GetField(this.View.BusinessInfo.MainOrgField.Key);
            if (!(fld.Entity is HeadEntity))
            {
                index = row;
            }
            DynamicObject obj = this.View.Model.GetValue(this.View.BusinessInfo.MainOrgField.Key, index) as DynamicObject;
            long id = 0;
            if (obj != null)
            {
                id = Convert.ToInt64(obj["Id"]);
            }
            return id;
        }

        /// <summary>
        /// 发起请检
        /// </summary>  
        private void ReqInspect()
        {
            int row = this.View.Model.GetEntryCurrentRowIndex("FEntity");//获取索引行
            DynamicObjectCollection dataEntity = this.View.Model.DataObject["PPBomEntry"] as DynamicObjectCollection;
            if (dataEntity[row]["lot"] == null)
            {
                this.View.ShowWarnningMessage("请先录入批号！");
                return;
            }

            var lot = Convert.ToInt32(dataEntity[row]["lot_Id"]);

            int FID = Convert.ToInt32(this.View.Model.DataObject["Id"]);
            int FPKID = Convert.ToInt32(dataEntity[row]["Id"]);
            int seq = Convert.ToInt32(dataEntity[row]["seq"]);
            IOperationResult salseresult = ServiceHelper.YDLCommServiceHelper.ConvertRule(this.Context, FID, FPKID, seq);
            if (!salseresult.IsSuccess)
            {
                string errorMessage = "请检单操作，失败原因：";
                foreach (Kingdee.BOS.Core.Validation.ValidationErrorInfo vr in salseresult.ValidationErrors)
                {
                    errorMessage += vr.Message + " ";
                }
                this.View.ShowErrMessage(errorMessage);
            }
            else
            {
                if (salseresult.SuccessDataEnity == null)
                {
                    string nullMessage = "请检单操作，失败原因：单据转换配置错误！";
                    this.View.ShowErrMessage(nullMessage);

                    return;
                }
                foreach (var dyhead in salseresult.SuccessDataEnity)
                {
                    this.View.ShowMessage("请检单操作成功！");
                    int id = Convert.ToInt32(dyhead["Id"]);
                    BillShowParameter paraml = new BillShowParameter { FormId = "QM_STKAPPInspect" };
                    paraml.OpenStyle.ShowType = ShowType.MainNewTabPage;
                    paraml.Status = OperationStatus.EDIT;
                    paraml.PKey = id.ToString();
                    this.View.ShowForm(paraml);
                }
            }
        }
        
        /// <summary>
        /// 显示检验情况
        /// </summary> 
        private void ShowInspectBill()
        {
            int materialId = 0;
            int row = this.View.Model.GetEntryCurrentRowIndex("FEntity");
            DynamicObjectCollection dataEntity = this.View.Model.DataObject["PPBomEntry"] as DynamicObjectCollection;
            if (dataEntity[row]["MaterialID"] != null)
            {
                DynamicObject material = dataEntity[row]["MaterialID"] as DynamicObject;
                materialId = Convert.ToInt32(material["Id"]);
            }

            var lot = Convert.ToInt32(dataEntity[row]["lot_Id"]);

            if (materialId == 0)
            {
                this.View.ShowWarnningMessage("请先录入子项物料！");
                return;
            }

            ListShowParameter para = new ListShowParameter();
            para.FormId = "QM_InspectBill";//检验单单据标示    
            para.ListFilterParameter.Filter = string.Format("FMaterialId = {0}", materialId);
            if (lot > 0)
            {
                para.ListFilterParameter.Filter = string.Format("FLot = {0}", lot);
            }
            para.IsShowApproved = true;
            para.OpenStyle.ShowType = ShowType.Modal;
            para.Height = 600;
            para.Width = 800;
            this.View.ShowForm(para);
        }

        /// <summary>
        /// 分录行创建后事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateNewEntryRow(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.CreateNewEntryEventArgs e)
        {
            base.AfterCreateNewEntryRow(e);
            int row = e.Row;
            this.View.Model.SetValue("F_JN_BOMQty", this.View.Model.GetValue("FMustQty", row), row);
            IDBService service = ServiceFactory.GetService<IDBService>(this.Context);
            long[] billIDS = service.GetSequenceInt64(this.Context, "T_PRD_PPBOMENTRY", 1).ToArray();//获取主键方法
            this.View.Model.SetValue("FJNLog", billIDS[0],row);
        }

        /// <summary>
        /// 邦定数据及状态后事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            DynamicObject workShop = this.View.Model.GetValue("FWorkshopID") as DynamicObject;
            if (workShop != null)
            {
                this.View.Model.SetValue("F_JN_HereStock", workShop["WIPStockID_Id"]);
            }
            BOMEnzyme();
        }

        /// <summary>
        /// 获取返回的参数值
        /// </summary>
        /// <param name="result">返回值</param>
        private void returnData(FormResult result)
        {
            if (result.ReturnData == null)
            {
                return;
            }          
            int row = this.View.Model.GetEntryCurrentRowIndex("FEntity");
            int oper = Convert.ToInt32(this.View.Model.GetValue("FOperID", row));//工序
            DynamicObject dataResult = result.ReturnData as DynamicObject;
            if (Convert.ToDecimal(dataResult["F_JN_EnzymeSelected"]) == 0)
            {
                return;
            }
            int enzymeId = Convert.ToInt32(dataResult["F_JN_Element_Id"]);
            decimal enzymeQty = Convert.ToDecimal(dataResult["F_JN_EnzymeNeed"]);
            string deleteData="";
            string[] del = new string[]{};
            if (dataResult["FJNDeleteData"] != null)
            {
                deleteData = dataResult["FJNDeleteData"].ToString();
            }
            if(deleteData.Length >0)del = deleteData.Substring(0,deleteData.Length-1).Split(new char[]{ ',' });
            DynamicObjectCollection dataEntry = dataResult["EnzymeEntry"] as DynamicObjectCollection;
            if (dataEntry == null || dataEntry.Count == 0)
            {
                return;
            }

            DynamicObject materil = this.View.Model.GetValue("FMaterialID2", row) as DynamicObject;
            if (Convert.ToBoolean(materil["F_JN_IsEnzyme"].ToString()))
            {
                this.View.Model.DeleteEntryRow("FEntity", row);   
            }
            List<int> list = new List<int>();
            int insertRow = row;
            DateTime dateMin = new DateTime(1991, 1, 1);
            DynamicObjectCollection materilEntry = this.View.Model.DataObject["PPBomEntry"] as DynamicObjectCollection;
            foreach (DynamicObject item in dataEntry)
            {
                if (Convert.ToInt32(item["F_JN_FID"]) == 0 && Convert.ToDecimal(item["FJNCheckQty"]) > 0)
                {
                    this.View.Model.InsertEntryRow("FEntity", insertRow);
                    this.View.Model.SetValue("FMaterialID2", item["MaterialID_Id"], insertRow);
                    this.View.Model.SetValue("FOperID", oper, insertRow);
                    this.View.Model.SetValue("FStockID", item["StockID_Id"], insertRow);
                    this.View.Model.SetValue("FStockLOCID", item["StockLOCID_Id"], insertRow);
                    this.View.Model.SetValue("FAuxPropID", item["AuxPropID"], insertRow);
                    this.View.Model.SetValue("FLot", item["Lot_Id"], insertRow);
                    this.View.Model.SetValue("F_JN_DefMatch", dataResult["F_JN_DefMatch"], insertRow);
                    this.View.Model.SetValue("F_JN_RealMatch", dataResult["F_JN_RealMatch"], insertRow);
                    if (Convert.ToDateTime(item["FJNProduceDate"]) == DateTime.MinValue)
                    {
                        this.View.Model.SetValue("FJNProduceDate", dateMin, insertRow);
                    }
                    else { this.View.Model.SetValue("FJNProduceDate", item["FJNProduceDate"], insertRow); }
                    if (Convert.ToDateTime(item["FJNProduceDate"]) == DateTime.MinValue)
                    {
                        this.View.Model.SetValue("FJNExpiryDate", dateMin, insertRow);
                    }
                    else
                    {
                        this.View.Model.SetValue("FJNExpiryDate", item["FJNExpiryDate"], insertRow);
                    }
                    this.View.Model.SetValue("FJNUnitEnzymes", item["FJNUnitEnzymes"], insertRow);
                    this.View.Model.SetValue("FMustQty", item["FJNCheckQty"], insertRow);
                    this.View.Model.SetValue("F_JN_EnzymeSumQty", item["FJNCheckAuxQty"], insertRow);
                    this.View.Model.SetValue("F_JN_BeEnzyme", enzymeId, insertRow);
                    this.View.Model.SetValue("F_JN_BeEnzymeQty", enzymeQty, insertRow);
                    this.View.Model.SetValue("FJNStockQty", item["FJNQty"], insertRow);
                    insertRow++;
                }
                else
                {     
                    foreach (DynamicObject peifang in materilEntry)
                    {
                        if (Convert.ToInt32(peifang["F_JN_BeEnzyme_Id"]) != Convert.ToInt32(dataResult["F_JN_Element_Id"]))
                        {
                            continue;
                        }
                        if (Convert.ToInt32(item["F_JN_FID"]) == Convert.ToInt32(peifang["FJNLog"]))
                        {
                            this.View.Model.SetValue("FStockID", item["StockID_Id"], materilEntry.IndexOf(peifang));
                            this.View.Model.SetValue("FStockLOCID", item["StockLOCID_Id"], materilEntry.IndexOf(peifang));
                            this.View.Model.SetValue("FAuxPropID", item["AuxPropID"], materilEntry.IndexOf(peifang));
                            this.View.Model.SetValue("FLot", item["Lot_Id"], materilEntry.IndexOf(peifang));
                            this.View.Model.SetValue("F_JN_DefMatch", dataResult["F_JN_DefMatch"], materilEntry.IndexOf(peifang));
                            this.View.Model.SetValue("F_JN_RealMatch", dataResult["F_JN_RealMatch"], materilEntry.IndexOf(peifang));
                            if (Convert.ToDateTime(item["FJNProduceDate"]) == DateTime.MinValue)
                            {
                                this.View.Model.SetValue("FJNProduceDate", dateMin, materilEntry.IndexOf(peifang));
                            }
                            else { this.View.Model.SetValue("FJNProduceDate", item["FJNProduceDate"], insertRow); }
                            if (Convert.ToDateTime(item["FJNProduceDate"]) == DateTime.MinValue)
                            {
                                this.View.Model.SetValue("FJNExpiryDate", dateMin, materilEntry.IndexOf(peifang));
                            }
                            else 
                            { 
                                this.View.Model.SetValue("FJNExpiryDate", item["FJNExpiryDate"], insertRow); 
                            }
                            this.View.Model.SetValue("FJNUnitEnzymes", item["FJNUnitEnzymes"], materilEntry.IndexOf(peifang));
                            this.View.Model.SetValue("FMustQty", item["FJNCheckQty"], materilEntry.IndexOf(peifang));
                            this.View.Model.SetValue("F_JN_EnzymeSumQty", item["FJNCheckAuxQty"], materilEntry.IndexOf(peifang));
                            this.View.Model.SetValue("F_JN_BeEnzyme", enzymeId, materilEntry.IndexOf(peifang));
                            this.View.Model.SetValue("F_JN_BeEnzymeQty", enzymeQty, materilEntry.IndexOf(peifang));
                            this.View.Model.SetValue("FJNStockQty", item["FJNQty"], materilEntry.IndexOf(peifang));
                            insertRow = materilEntry.IndexOf(peifang);
                        }                       
                    }
                }
            }
            foreach (var items in materilEntry)
            {
                if (del.Contains(items["FJNLog"].ToString()))
                {
                    list.Add(materilEntry.IndexOf(items));
                }
            }
            if (list == null || list.Count == 0)
            {
                return;
            }
            foreach(int rowIndex in list)
            {
                this.View.Model.DeleteEntryRow("FEntity", rowIndex);
            }
            this.View.UpdateView();
        }

        /// <summary>
        /// 值更新事件
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FScrapRate" || e.Field.Key == "FOperID")
            {
                BOMEnzyme();
            }
        }

        /// <summary>
        /// F7查询事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeF7Select(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.FieldKey.EqualsIgnoreCase("FLot"))
            {
                QureyInvQty(false);
                e.Cancel = true;
            }
        }

        public override void BeforeDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            if (e.Operation.FormOperation.Operation.EqualsIgnoreCase("LockStock") || e.Operation.FormOperation.Operation.EqualsIgnoreCase("UnLockStock"))
            {
                DynamicObject dyEntryData;
                int rowIndex;
                this.View.Model.TryGetEntryCurrentRow("FEntity", out dyEntryData, out rowIndex);
                if (dyEntryData == null || Convert.ToInt64(dyEntryData["Id"]) == 0)
                {
                    e.Cancel = true;
                }
                e.Option.SetVariableValue("EntryId", Convert.ToInt64(dyEntryData["Id"]));
            }
        }

        /// <summary>
        /// 酶种酶活总量
        /// </summary>
        private void BOMEnzyme()
        {
            int bomId = Convert.ToInt32(this.View.Model.DataObject["FBOMID_Id"]);
            decimal qty = Convert.ToDecimal(this.View.Model.GetValue("FQty"));
            DynamicObjectCollection entry = this.View.Model.DataObject["PPBomEntry"] as DynamicObjectCollection;
            if (entry == null || entry.Count == 0) return;
            int i = 0;
            foreach (DynamicObject item in entry)
            {
                decimal numerator = Convert.ToDecimal(entry[i]["Numerator"]);//分子
                decimal denominator = Convert.ToDecimal(entry[i]["Denominator"]);//分母
                decimal scrapRate = Convert.ToDecimal(entry[i]["ScrapRate"]);//变动损耗率
                if (denominator > 0 && Convert.ToDecimal(entry[i]["F_JN_BOMQty"]) == 0)
                {                    
                    decimal bomQty = qty * (numerator / denominator) * (1 + scrapRate/100);//BOM用量
                    this.View.Model.SetValue("F_JN_BOMQty", bomQty, i);    
                }               
                DynamicObject material = item["MaterialID"] as DynamicObject;
                if (material == null) continue;
                if (Convert.ToBoolean(material["F_JN_IsEnzyme"]))
                {              
                    int materialId = Convert.ToInt32(material["Id"]);
                    int oper = Convert.ToInt32(item["OperID"]);                    
                    DynamicObjectCollection datas = YDLCommServiceHelper.GetBom(this.Context, bomId, materialId, oper);
                    if (datas != null && datas.Count > 0)
                    {
                        decimal enzymeSum = qty * Convert.ToDecimal(datas[0]["FJNCOMPANYEA"]) * (1 + scrapRate / 100);
                        this.View.Model.SetValue("F_JN_EnzymeSumQty", enzymeSum, i);//酶种酶活总量计算                        
                    }
                    else { this.View.Model.SetValue("F_JN_EnzymeSumQty", 0, i); }
                }
                i++;
            }
        }
        
        /// <summary>
        /// 库存查询
        /// </summary>
        private void QureyInvQty(bool multiSelect=true )
        {
            int row = this.View.Model.GetEntryCurrentRowIndex("FEntity");
            long  materialId = 0;     //物料
            var matObj = this.View.Model.GetValue("FMaterialID2", row) as DynamicObject;
            if (matObj != null)
            {
                materialId = Convert.ToInt64(matObj["Id"]);
            }
            long auxPropId = 0;     //辅助属性
            DynamicObject auxData = this.View.Model.GetValue("FAuxPropID", row) as DynamicObject;
            if (auxData != null && Convert.ToInt64(auxData["Id"]) == 0)
            {
                RelatedFlexGroupField flexField = this.View.BillBusinessInfo.GetField("FAuxPropID") as RelatedFlexGroupField;
                auxPropId = LinusFlexUtil.GetFlexDataId(this.View.Context, auxData, flexField);
                this.View.Model.SetValue("FAuxPropID", auxPropId, row);
            }
            else if (auxData != null && Convert.ToInt64(auxData["Id"]) > 0)
            {
                auxPropId = Convert.ToInt64(auxData["Id"]);
            }
            var filter = "";
            if (materialId > 0)
            {
              filter=  filter.JoinFilterString(string.Format(@"FMATERIALID.FNumber In 
                                                                                (select FNumber from T_BD_MATERIAL 
                                                                                where FMATERIALID={0}
                                                                                )", materialId));
            } 
            if (auxPropId > 0)
            {
                filter = filter.JoinFilterString(string.Format(@" FAuxPropID ={0} ", auxPropId));
            }
            if (this.View.Model.GetValue("FPrdOrgId") != null)
            {
                filter = filter.JoinFilterString(string.Format(@" FStockOrgId ={0} ", this.View.Model.DataObject["PrdOrgId_Id"].ToString()));
            }

            ListShowParameter ShowPara = new ListShowParameter();
            ShowPara.ParentPageId = this.View.PageId;
            ShowPara.MultiSelect = multiSelect;
            ShowPara.FormId = "STK_Inventory";// "STK_InvJoinQuery";
            ShowPara.CustomParams.Add("NeedReturnData", "1");
            ShowPara.CustomParams.Add("enzyme", "true");
            ShowPara.CustomParams.Add("QueryMode", "1");//0 主控台进行查询， 1 单据上的查询
            ShowPara.CustomParams.Add("QueryOrgId",  this.View.Model.DataObject["PrdOrgId_Id"].ToString ());
           
            ShowPara.ListFilterParameter.Filter = filter;
            ShowPara.Height = 600;
            ShowPara.Width = 1000;
             
            this.View.ShowForm(ShowPara, DoAnalyzeQureyInvQty);
        }

        /// <summary>
        /// 应用即时库存查询返回数据
        /// </summary>
        /// <param name="ret"></param>
        private void DoAnalyzeQureyInvQty(FormResult ret)
        {
            if (   ret == null || ret.ReturnData == null)
            {
                return;
            }

            DynamicObjectCollection returnData = ret.ReturnData as DynamicObjectCollection;
            if (returnData == null || returnData.Count == 0)
            {
                return;
            }

            List<DynamicObject> beAdd = new List<DynamicObject>();
            int rowIndex = this.View.Model.GetEntryCurrentRowIndex("FEntity");
            int rowCount = this.View.Model.GetEntryRowCount("FEntity"); 
            foreach (DynamicObject item in returnData)
            {
                bool have = false;
                for (int i = rowIndex; i < rowCount; i++)
                {
                    var matObj=this.View.Model.GetValue ("FMaterialID2",i) as DynamicObject ;
                    var matObj2 = item["FNUMBER"];
                    if(matObj ==null || matObj2==null  )
                    {
                        continue ;
                    }

                    var autObj = this.View.Model.GetValue("FAuxPropID", i) as DynamicObject;
                    var autObj2 = item["FAuxPropID"];
                    var autId =autObj==null ? "0" : Convert.ToString(autObj["Id"]);
                    var autId2 = Convert.ToString(autObj2);                    

                    //物料+辅助属性相同则更新
                    if (matObj["Number"].ToString() == matObj2.ToString() && autId == autId2)
                    { 
                        this.View.Model.SetValue("FStockID", item["FSTOCKID"], i);
                        this.View.Model.SetValue("FStockLOCID", item["FSTOCKLOCID"], i);
                        this.View.Model.SetValue("FLot", item["FLOT"], i);
                        
                        if (item["FIsMeasure"]!=null && item["FIsMeasure"].ToString ()=="1" 
                            && Convert.ToDecimal(item["FQty"]) > 0)
                        {
                            var unitQty = Math.Round(Convert.ToDecimal(item["FSecQty"]) / Convert.ToDecimal(item["FQty"]), 4);
                            this.View.Model.SetValue("FJNUnitEnzymes", unitQty, i);
                        }

                        SetExpiryDate(i);

                        have = true;
                        rowIndex = i + 1;
                        break;
                    }
                }
                if (have == false)
                {
                    beAdd.Add(item);
                } 
            }
             
            foreach (var item in beAdd)
            {
                rowIndex = this.View.Model.GetEntryRowCount("FEntity");
                if (this.View.Model.GetValue("FMaterialID2", rowIndex-1) != null)
                {
                    this.View.Model.CreateNewEntryRow("FEntity");
                }
                else
                {
                    rowIndex = rowIndex - 1;
                }

                this.View.Model.SetItemValueByNumber("FMaterialID2", item["FNUMBER"].ToString(), rowIndex);
                this.View.Model.SetValue("FAuxPropID", item["FAuxPropID"], rowIndex);
                this.View.Model.SetValue("FStockID", item["FSTOCKID"], rowIndex);
                this.View.Model.SetValue("FStockLOCID", item["FSTOCKLOCID"], rowIndex);
                this.View.Model.SetValue("FLot", item["FLOT"], rowIndex);
                 
                if (item["FIsMeasure"] != null && item["FIsMeasure"].ToString() == "1"
                            && Convert.ToDecimal(item["FQty"]) > 0)
                {
                    var unitQty = Math.Round(Convert.ToDecimal(item["FSecQty"]) / Convert.ToDecimal(item["FQty"]), 4);
                    this.View.Model.SetValue("FJNUnitEnzymes", unitQty, rowIndex);
                }

                SetExpiryDate(rowIndex);
            }
             
            this.View.UpdateView("FEntity");
            this.View.GetControl<EntryGrid>("FEntity").SetFocusRowIndex(rowIndex);
        }

        /// <summary>
        /// 查询物料的生产日期、有效期至
        /// </summary>
        /// <param name="rowIndex"></param>
        private void SetExpiryDate( int rowIndex)
        { 
            var matObj = this.View.Model.GetValue("FMaterialID2", rowIndex) as DynamicObject;
            if (matObj == null)
            {
                return;
            }
            var lotNumber = "";
            var lotObj = this.View.Model.GetValue("FLot", rowIndex) ; 
            if (lotObj != null)
            {
                if (lotObj.GetType() == typeof(DynamicObject))
                {
                    lotNumber = Convert.ToString((lotObj as DynamicObject)["Number"]);
                }
                else
                {
                    lotNumber = lotObj.ToString();
                }
            }

            JNQTYRatePara para = new JNQTYRatePara();
            para.MaterialNumber = matObj["Number"].ToString ();
            para.MaterialId =Convert.ToInt64 ( matObj["Id"]);
            para.LotNumber = lotNumber;
            para.OrgId = Convert.ToInt64(this.View.Model.DataObject["PrdOrgId_Id"]);

            DynamicObjectCollection date = YDLCommServiceHelper.GetLotExpiryDate(this.Context, para);
            if (date != null && date.Count > 0)
            {
                this.View.Model.SetValue("FJNProduceDate", date[0]["FPRODUCEDATE"], rowIndex);
                this.View.Model.SetValue("FJNExpiryDate", date[0]["FEXPIRYDATE"], rowIndex);
            }   
        }  
    }    
}
