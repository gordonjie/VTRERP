using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Util;
using JN.K3.YDL.Core;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS;
using Kingdee.BOS.ServiceHelper;


namespace JN.K3.YDL.Business.PlugIn.MFG
{
    /// <summary>
    /// 酶活维护-表单插件
    /// </summary>
    [Description("酶活维护-表单插件")]
    public class EnzymeMaintainEdit : AbstractDynamicFormPlugIn
    {
        /// <summary>
        /// 是否返回数据
        /// </summary>
        int _needReturn = 1;
        /// <summary>
        /// 创建组织
        /// </summary>
        long OrgId = 0;
        /// <summary>
        /// 单位酶活
        /// </summary>
        decimal rate = 0;
        /// <summary>
        /// 删除数据集合
        /// </summary>
        StringBuilder deleteData = new StringBuilder();
    
        /// <summary>
        /// 数据包生成后逻辑处理
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateNewData(EventArgs e)
        {
            int materialId = Convert.ToInt32(this.View.OpenParameter.GetCustomParameter("F_JN_Element"));
            decimal enzymeSum = Convert.ToDecimal(this.View.OpenParameter.GetCustomParameter("F_JN_EnzymeNeed"));
            decimal defMatch = 1.5M;//默认配比
            Boolean isMatch = false;
            OrgId = Convert.ToInt64(this.View.OpenParameter.GetCustomParameter("OrgId"));
            this.View.Model.SetValue("F_JN_Element", materialId);//所需成分
            this.View.Model.SetValue("F_JN_EnzymeNeed", enzymeSum);
            DynamicObjectCollection dataEntry = this.View.OpenParameter.GetCustomParameter("Data") as DynamicObjectCollection;
            List<DynamicObject> dataNew = new List<DynamicObject>();
            if (dataEntry != null && dataEntry.Count > 0)
            {                
                this.View.Model.SetValue("F_JN_Element", materialId);
                this.View.Model.SetValue("F_JN_EnzymeNeed", enzymeSum);               
                int row = 0;
                decimal auxQty = 0;
                foreach (DynamicObject item in dataEntry)
                {
                    if (Convert.ToInt32(item["F_JN_BeEnzyme_Id"]) == materialId)
                    {
                        int defRow = dataEntry.IndexOf(item);
                        defMatch = Convert.ToDecimal(item["F_VTR_DefMatchentity"]);
                        isMatch = true;
                        this.View.Model.CreateNewEntryRow("FEntity");
                        int materil = Convert.ToInt32(item["MaterialID_Id"]);
                        int lot = Convert.ToInt32(item["Lot_Id"]);
                        this.View.Model.SetValue("FMaterialID", item["MaterialID_Id"], row);
                        this.View.Model.SetValue("FStockID", item["StockID_Id"], row);
                        this.View.Model.SetValue("FStockLOCID", item["StockLocID_Id"], row);
                        this.View.Model.SetValue("FAuxPropID", item["AuxPropID"], row);                        
                        this.View.Model.SetValue("FLot", item["Lot"], row);
                        if (item["FJNProduceDate"] != null)
                        {
                            this.View.Model.SetValue("FJNProduceDate", item["FJNProduceDate"], row);
                        }
                        if (item["FJNExpiryDate"] != null)
                        {
                            this.View.Model.SetValue("FJNExpiryDate", item["FJNExpiryDate"], row);
                        }
                        this.View.Model.SetValue("FJNUnitEnzymes", item["FJNUnitEnzymes"], row);                        
                        this.View.Model.SetValue("FJNCheckQty", item["MustQty"], row);
                        this.View.Model.SetValue("FJNCheckAuxQty", item["F_JN_EnzymeSumQty"], row);
                        auxQty += Convert.ToDecimal(item["F_JN_EnzymeSumQty"]);
                        decimal wasQty = Convert.ToDecimal(item["PickedQty"]) - Convert.ToDecimal(item["INCDefectReturnQty"]) - Convert.ToDecimal(item["GoodReturnQty"]);
                        this.View.Model.SetValue("F_JN_WasQty", wasQty, row);
                        this.View.Model.SetValue("F_JN_FID", item["FJNLog"], row);
                        this.View.Model.SetValue("FJNQty", item["FJNStockQty"], row);
                        this.View.Model.SetValue("FExtAuxUnitQty", Convert.ToDecimal(item["FJNStockQty"]) * Convert.ToDecimal(item["FJNUnitEnzymes"]), row);
                        decimal unitEnzyme = Convert.ToDecimal(item["FJNUnitEnzymes"]);
                        decimal qty = 0;
                        if (unitEnzyme > 0) qty = enzymeSum * defMatch / unitEnzyme;
                        this.View.Model.SetValue("F_JN_AdviseQty", qty, row);
                        GetLastdate(materil,lot,row);
                        dataNew.Add(item);
                        row++;
                    }
                }
                this.View.Model.SetValue("F_JN_EnzymeSelected", auxQty);
                if(isMatch == true)this.View.Model.SetValue("F_JN_DefMatch", defMatch);
                if (enzymeSum > 0) this.View.Model.SetValue("F_JN_RealMatch", auxQty / enzymeSum);

            }
            getInvQty(dataNew, enzymeSum, defMatch);
        }

        /// <summary>
        /// 获取剩余酶种及时库存信息
        /// </summary>
        /// <param name="dataEntry"></param>
        /// <param name="enzymeNeed"></param>
        /// <param name="defMatch"></param>
        private void getInvQty(List<DynamicObject> dataEntry, decimal enzymeNeed, decimal defMatch)
        {
            DynamicObject enzymeDy = this.View.Model.GetValue("F_JN_Element") as DynamicObject;
            int enzymeId = Convert.ToInt32(enzymeDy["Id"]);
            DynamicObjectCollection invStock = YDLCommServiceHelper.GetInvStock(this.Context, enzymeId, OrgId);
            if (invStock != null && invStock.Count() > 0)
            {
                int i = 0;
                if (dataEntry != null && dataEntry.Count > 0)
                {
                    i = dataEntry.Count();
                }               
                foreach (DynamicObject item in invStock)
                {
                    string materilNo = item["FNUMBER"].ToString();
                    int lot = Convert.ToInt32(item["flot"]);
                    int AuxPropId = Convert.ToInt32(item["FAUXPROPID"]);
                    int stockId = Convert.ToInt32(item["FSTOCKID"]);
                    int stockLocId = Convert.ToInt32(item["FSTOCKLOCID"]);
                    bool cont = true;
                    if (dataEntry != null && dataEntry.Count > 0)
                    {
                        foreach (DynamicObject item2 in dataEntry)
                        {
                            string materilNo2 = "";
                            DynamicObject materil = item2["MaterialID"] as DynamicObject;
                            if (materil != null)
                            {
                                materilNo2 = materil["Number"].ToString();


                            }
                            int lot2 = Convert.ToInt32(item2["Lot_Id"]);
                            int AuxPropId2 = Convert.ToInt32(item2["AuxPropID_Id"]);
                            int stockId2 = Convert.ToInt32(item2["StockID_Id"]);
                            int stockLocId2 = Convert.ToInt32(item2["StockLocID_Id"]);
                            if (materilNo == materilNo2 && lot == lot2 && AuxPropId == AuxPropId2
                                && stockId == stockId2 && stockLocId == stockLocId2)
                            {
                                cont = false;
                                break;
                            }
                        }
                    }
                    decimal avbQty = Convert.ToDecimal(item["FBASEQTY"]) - Convert.ToDecimal(item["FBASELOCKQTY"]);
                    if (cont == true && avbQty > 0)
                    {
                        this.View.Model.CreateNewEntryRow("FEntity");
                        this.View.Model.SetValue("FMaterialID", item["FMATERIALID"], i);
                        this.View.Model.SetValue("FStockID", item["FSTOCKID"], i);
                        this.View.Model.SetValue("FStockLOCID", item["FSTOCKLOCID"], i);
                        this.View.Model.SetValue("FAuxPropID", item["FAUXPROPID"], i);
                        this.View.Model.SetValue("FLot", item["FLOT"], i);
                        GetRate(i);

                        int materilId = Convert.ToInt32(item["FMATERIALID"]);
                        int lotId = Convert.ToInt32(item["FLOT"]);
                        GetLastdate(materilId, lotId, i);
                        decimal defMatch2 = getdefMatch(this.Context, materilId);
                        this.View.Model.SetValue("F_VTR_DefMatchentity", defMatch2, i);
                        defMatch = defMatch2;
                        this.View.Model.SetValue("FJNQty", avbQty, i);
                        this.View.Model.SetValue("FExtAuxUnitQty", avbQty * rate, i);
                        decimal qty = 0;
                        if (rate > 0) qty = enzymeNeed * defMatch / rate;
                        this.View.Model.SetValue("F_JN_AdviseQty", qty, i);

                        i++;                       
                    } 
                }            
            }
        }

        private decimal getdefMatch(Context ctx, int materilId2)
        {
            decimal defMatch2 = 1;
            //获取当前日期
            DateTime currentTime = new System.DateTime();
            currentTime = TimeServiceHelper.GetSystemDateTime(this.Context);
            //根据剂型和酶种获取对应的领料比例
            string sql = string.Format(@"select t1.F_VTR_DEFMATCH as DEFMATCH from VTR_t_DefMatchSetEntity t1
join VTR_t_DefMatchSet t2 on t1.FID=t2.FID
join T_BD_MATERIAL t3 on t1.F_VTR_SHAPETYPE=t3.F_JNSHAPETYPE and t1.F_VTR_FUNGUSCLASS=F_JN_FUNGUSCLASS
where t2.FDOCUMENTSTATUS='C' and t2.F_VTR_EFFECTIVEDATE<='{0}' and t2.F_VTR_EXPIRYDATE>='{0}'
and t3.FMATERIALID={1} 
order by t2.F_VTR_EFFECTIVEDATE desc", currentTime.ToString(), materilId2.ToString());
            DynamicObjectCollection defMatchs=DBServiceHelper.ExecuteDynamicObject(ctx, sql);
            if (defMatchs.Count > 0)
            {
                defMatch2 = Convert.ToDecimal(defMatchs[0]["DEFMATCH"]);
            }
            
            return defMatch2;
        }

        /// <summary>
        /// 主菜单单击事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBarItemClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.EqualsIgnoreCase("tbInspectSerch"))//检验查询
            {
                InspectSerch();
            }
        }

        /// <summary>
        /// 分录菜单点击事件
        /// </summary>
        /// <param name="e"></param>
        public override void EntryBarItemClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BarItemClickEventArgs e)
        {
            base.EntryBarItemClick(e);
            
            if (e.BarItemKey.EqualsIgnoreCase( "tbDeleteEntry"))//删除分录
            {
                int row = this.View.Model.GetEntryCurrentRowIndex("FEntity");
                if (Convert.ToDecimal(this.View.Model.GetValue("F_JN_WasQty", row)) > 0)
                {
                    this.View.ShowWarnningMessage("此条数据已下推，无法删除！");
                    e.Cancel = true;
                }
            }
            if (e.BarItemKey.EqualsIgnoreCase("tbQueryStock"))//库存查询
            {
                QureyInvQty();
            }   
        }

        bool xunhuan = false;
        /// <summary>
        /// 邦定数据及状态后事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (xunhuan == true) return;
            DynamicObjectCollection entry = this.View.Model.DataObject["EnzymeEntry"] as DynamicObjectCollection;
            int row = 0;
            foreach (DynamicObject item in entry)
            {
                if (item["MaterialID"] != null)
                {
                    int lot = 0;
                    int materilid = 0;
                    materilid = Convert.ToInt32(item["MaterialID_Id"]);
                    if (item["Lot"] != null) lot = Convert.ToInt32(item["Lot_Id"]);
                    GetLastdate(materilid, lot, row);
                    row++;
                }
            }
            xunhuan = true;
            this.View.UpdateView();

        }
        
        /// <summary>
        /// 检验查询
        /// </summary>
        private void InspectSerch()
        {
            DynamicObjectCollection entry = this.View.Model.DataObject["EnzymeEntry"] as DynamicObjectCollection;

            int row = this.View.Model.GetEntryCurrentRowIndex("FEntity");
            if (row < 0)
            {
                return;
            }
            if (entry.Count == 0)
            {
                return;
            }
            int materialId = 0;     //物料
            int lot = 0;            //批号
            if (entry[row]["MaterialID"] != null)
            {
                materialId = Convert.ToInt32(entry[row]["MaterialID_Id"]);
            }
            if (entry[row]["lot"] != null)
            {
                lot = Convert.ToInt32(entry[row]["lot_Id"]);
            }
            if (materialId == 0)
            {
                this.View.ShowWarnningMessage("请先录入子项物料！");
                return;
            }
            ListShowParameter para = new ListShowParameter();
            para.FormId = "QM_InspectBill";//检验单单据标示   
            para.IsShowApproved = true;
            para.ListFilterParameter.Filter = string.Format("FMaterialId = {0}", materialId);
            if (lot > 0)
            {
                para.ListFilterParameter.Filter = string.Format("FLot = {0}", lot);
            }
            para.OpenStyle.ShowType = ShowType.Modal;
            para.Height = 600;
            para.Width = 800;
            this.View.ShowForm(para);
        }
        
        /// <summary>
        /// 库存查询
        /// </summary>
        private void QureyInvQty()
        { 
            int materialId = 0;     //物料
             
            materialId = Convert.ToInt32(this.View.Model.DataObject["F_JN_Element_Id"]);
            ListShowParameter ShowPara = new ListShowParameter();
            ShowPara.ParentPageId = this.View.PageId;
            ShowPara.MultiSelect = true;
            ShowPara.FormId = "STK_Inventory";
            ShowPara.CustomParams.Add("NeedReturnData", _needReturn.ToString());
            ShowPara.CustomParams.Add("enzyme", "true");            
            ShowPara.Height = 600;
            ShowPara.Width = 1000;
            string filter = " FMATERIALID in " + string.Format("(select FMATERIALID from T_BD_MATERIAL where F_JN_FUNGUSCLASS={0})", materialId);
            //if (lot > 0)
            //{
            //    filter += " And lot =" + lot;
            //}
            //ShowPara.CustomParams.Add("QueryFilter", filter);
            ShowPara.ListFilterParameter.Filter = filter;
            this.View.ShowForm(ShowPara, ApplyReturnData);
        }

        /// <summary>
        /// 应用即时库存查询返回数据
        /// </summary>
        /// <param name="ret"></param>
        private void ApplyReturnData(FormResult ret)
        {
            if (_needReturn == 0 || ret == null || ret.ReturnData == null)
            {
                return;
            }
            DynamicObjectCollection returnData = ret.ReturnData as DynamicObjectCollection;
            if (returnData == null || returnData.Count == 0)
            {
                return;
            }

            DynamicObjectCollection entry = this.View.Model.DataObject["EnzymeEntry"] as DynamicObjectCollection;
            int i = entry.Count;
            foreach (DynamicObject item in returnData)
            {
                this.View.Model.InsertEntryRow("FEntity",i);
                this.View.Model.SetValue("FMaterialID", item["FMATERIALID"],i);
                this.View.Model.SetValue("FStockID", item["FSTOCKID"], i);
                this.View.Model.SetValue("FStockLOCID", item["FSTOCKLOCID"], i);
                this.View.Model.SetValue("FAuxPropID", item["FAUXPROPID"], i);
                this.View.Model.SetValue("FLot", item["FLOT"], i);
                this.View.Model.SetValue("FJNQty", item["FAVBQTY"], i);
                this.View.Model.SetValue("FExtAuxUnitQty", Convert.ToDecimal(item["FAVBQTY"]) * Convert.ToDecimal(rate), i);
                this.View.UpdateView();                            
                i++;
            }
        }
        
        /// <summary>
        /// 表单操作前事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            if (e.Operation.FormOperation.Operation == "Affirm")//确认操作时
            {
                DynamicObjectCollection dataEntry = this.View.Model.DataObject["EnzymeEntry"] as DynamicObjectCollection;
                int i = 0;
                foreach (DynamicObject item in dataEntry)
                {
                    if (Convert.ToDecimal(item["FJNQty"]) < Convert.ToDecimal(item["FJNCheckQty"]))
                    {
                        this.View.ShowWarnningMessage(string.Format("第{0}行的选择数量不能大于库存数量！",i));
                        e.Cancel = true;
                        return;
                    }
                    i++;
                }
                if (Convert.ToBoolean(e.Operation.View.OpenParameter.GetCustomParameter("isPassed"))) //检验通过再次调用操作判断定制参数
                {
                    e.Operation.View.OpenParameter.SetCustomParameter("isPassed", false);
                    return;
                }
                decimal sumEnzyme = Convert.ToDecimal(this.View.Model.GetValue("F_JN_EnzymeSelected"));//已选酶活
                decimal enzymeNeed = Convert.ToDecimal(this.View.Model.GetValue("F_JN_EnzymeNeed"));//所需酶活  
                if (enzymeNeed == 0)
                {
                    this.View.ShowWarnningMessage("需酶活为必录字段！");
                    e.Cancel = true;
                    return;
                }
                if (sumEnzyme < enzymeNeed)
                {
                    this.View.ShowWarnningMessage("已选酶活总量小于配方单物料所需酶活总量", "已选酶活总量小于所需酶活总量,请问是否继续？", MessageBoxOptions.YesNoCancel, new Action<MessageBoxResult>((boxresult) =>
                    {
                        if (boxresult == MessageBoxResult.Yes)
                        {
                            e.Cancel = false;                          
                            this.View.ReturnToParentWindow(this.View.Model.DataObject);
                            e.Operation.View.OpenParameter.SetCustomParameter("isPassed", true); //设置定制参数
                            e.Operation.Execute();
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    }));
                    e.Cancel = true;
                }
                else
                {                                      
                    this.View.ReturnToParentWindow(this.View.Model.DataObject);
                }
            }
        }
        
        /// <summary>
        /// 值更新事件
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            int lot = 0;
            int materilId = 0;
            int row = this.View.Model.GetEntryCurrentRowIndex("FEntity");
            DynamicObject materil = this.View.Model.GetValue("FMaterialID", row) as DynamicObject;
            DynamicObject lotData = this.View.Model.GetValue("FLot", row) as DynamicObject;
            if (e.Field.Key == "FJNCheckAuxQty")//选择酶活总量
            {
                EnzymeSelect();
            }
            if (e.Field.Key == "FLot")
            {
                if (materil != null)
                {
                    materilId = Convert.ToInt32(materil["Id"]);
                    if (lotData != null) lot = Convert.ToInt32(lotData["Id"]);
                    GetLastdate(materilId, lot, row);
                    GetRate(row);
                }
            }
            if (e.Field.Key == "F_JN_DefMatch")
            {
               // SetdefMatch(); 不需要根据表头设置建议用量
            }
            if (e.Field.Key == "F_JN_EnzymeSelected")
            {
                decimal enzymeNeed = Convert.ToDecimal(this.View.Model.GetValue("F_JN_EnzymeNeed"));
                decimal enzymeSelected = Convert.ToDecimal(this.View.Model.GetValue("F_JN_EnzymeSelected"));
                if (enzymeSelected > 0 && enzymeNeed > 0)
                {
                    decimal realMatch = enzymeSelected / enzymeNeed;
                    this.View.Model.SetValue("F_JN_RealMatch",realMatch);
                }
            }
            if (e.Field.Key == "FJNUnitEnzymes")
            {
                SetadviseQty(e.Row);
            }
            if (e.Field.Key == "F_VTR_DefMatchentity")
            {
                SetadviseQty(e.Row);
            }

            if (e.Field.Key == "FJNCheckQty")
            {
                setbilldefMatch();
            }
        }

        private void setbilldefMatch()
        {
            DynamicObjectCollection entry = this.View.Model.DataObject["EnzymeEntry"] as DynamicObjectCollection;
            decimal billdefMatch = Convert.ToDecimal(this.View.Model.GetValue("F_JN_DefMatch"));
            foreach (DynamicObject item in entry)
            {
                if (Convert.ToDecimal(item["FJNCheckQty"]) > 0 && Convert.ToDecimal(item["F_VTR_DefMatchentity"]) < billdefMatch)
                {
                    billdefMatch = Convert.ToDecimal(item["F_VTR_DefMatchentity"]);
                    this.View.Model.SetValue("F_JN_DefMatch",billdefMatch);
                }
            }
        }

        /// <summary>
        /// 获取最后检验日期
        /// </summary>
        /// <param name="materilid">物料ID</param>
        /// <param name="lot">批号ID</param>
        /// <param name="row">行号</param>
        private void GetLastdate(int materilid, int lot, int row)
        {
            QueryBuilderParemeter par = new QueryBuilderParemeter();
            par.FormId = "QM_InspectBill";
            par.SelectItems = SelectorItemInfo.CreateItems("FDATE");            
            if (lot > 0)
            {
                par.FilterClauseWihtKey = string.Format("FMATERIALID={0} And FLOT={1} ", materilid, lot);
            }
            else
            {
                par.FilterClauseWihtKey = string.Format("FMATERIALID={0} ", materilid);
            }
            DynamicObjectCollection FileData = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, par);
            if (FileData == null || FileData.Count == 0)
            {
                this.View.Model.SetValue("FJNLastDate", null, row);
                return;
            }

            DateTime lastTime = DateTime.MinValue;
            foreach (DynamicObject file in FileData)
            {
                DateTime time = Convert.ToDateTime(file["FDATE"]);
                if (lastTime < time)
                {
                    lastTime = time;
                }
            }
            this.View.Model.SetValue("FJNLastDate", lastTime, row);
        }

        /// <summary>
        /// 获取单位酶活,日期
        /// </summary>
        /// <param name="row"></param>
        private void GetRate(int row)
        {
            long AuxPropId = 0;
            DynamicObject mat = this.View.Model.GetValue("FMaterialID", row) as DynamicObject;
            if (mat == null) return;
            DynamicObject AuxProp = this.View.Model.GetValue("FAuxPropID", row) as DynamicObject;
            if(AuxProp != null)AuxPropId = Convert.ToInt64(AuxProp["Id"]);
            string matNumber = mat["Number"].ToString();
            long matId = Convert.ToInt64(mat["Id"]);
            string lotNo = "";
            var lot = this.View.Model.GetValue("FLot", row);
            if (lot != null)
            {
                if (lot.GetType() == typeof(DynamicObject))
                {
                    lotNo = Convert.ToString((lot as DynamicObject)["Number"]);
                }
                else
                {
                    lotNo = lot.ToString();
                }
            }
            JNQTYRatePara para = new JNQTYRatePara();
            para.MaterialNumber = matNumber;
            para.MaterialId = matId;
            para.LotNumber = lotNo;
            para.OrgId = OrgId;
            para.AuxPropId = AuxPropId;
            rate = YDLCommServiceHelper.MaterialUnitEnzymes(this.Context, para);
            DynamicObjectCollection date = YDLCommServiceHelper.GetLotExpiryDate(this.Context, para);
            if (date != null && date.Count > 0)
            {
                this.View.Model.SetValue("FJNProduceDate", date[0]["FPRODUCEDATE"], row);
                this.View.Model.SetValue("FJNExpiryDate", date[0]["FEXPIRYDATE"], row);
            }            
            this.View.Model.SetValue("FJNUnitEnzymes", rate, row);
            //return rate;
        }

        /// <summary>
        /// 删除分录前事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeDeleteRow(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeDeleteRowEventArgs e)
        {
            base.BeforeDeleteRow(e);
            int row = e.Row;            
            if (this.View.Model.GetValue("F_JN_FID", row) != null && Convert.ToInt64(this.View.Model.GetValue("F_JN_FID", row)) != 0)
            {
                long fid = Convert.ToInt64(this.View.Model.GetValue("F_JN_FID", row));
                this.View.Model.SetValue("FJNDeleteData", fid.ToString() + ",");
            }
        }

        /// <summary>
        /// 删除分录后事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterDeleteRow(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterDeleteRowEventArgs e)
        {
            base.AfterDeleteRow(e);
            EnzymeSelect();
        }

        /// <summary>
        /// 值更新前事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeUpdateValue(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeUpdateValueEventArgs e)
        {
            base.BeforeUpdateValue(e);
            if (e.Key == "FJNCheckQty")
            {
                decimal checkQty = Convert.ToDecimal(e.Value);
                decimal wasQty = Convert.ToDecimal(this.View.Model.GetValue("F_JN_WasQty", e.Row));
                if (wasQty > 0 && checkQty < wasQty)
                {
                    this.View.ShowMessage("选择数量不能小于已领数量！");
                    e.Cancel = true;
                }
            }            
        }

        /// <summary>
        /// 建议数量
        /// </summary>
        /// <param name="e"></param>
        private void SetadviseQty(int row)
        {           
            decimal enzymeNeed = Convert.ToDecimal(this.View.Model.GetValue("F_JN_EnzymeNeed"));
            decimal defMatch = Convert.ToDecimal(this.View.Model.GetValue("F_VTR_DefMatchentity",row));
            decimal enzymeUnit = Convert.ToDecimal(this.View.Model.GetValue("FJNUnitEnzymes", row));
            decimal qty = 0;
            if(enzymeUnit > 0)qty = enzymeNeed * defMatch / enzymeUnit;
            this.View.Model.SetValue("F_JN_AdviseQty", qty, row);
            //this.View.UpdateView();不全部更新
        }

        /// <summary>
        /// 建议数量计算
        /// </summary>
        private void SetdefMatch()
        {
            decimal enzymeNeed = Convert.ToDecimal(this.View.Model.GetValue("F_JN_EnzymeNeed"));
            decimal defMatch = Convert.ToDecimal(this.View.Model.GetValue("F_JN_DefMatch"));
            DynamicObjectCollection entry = this.View.Model.DataObject["EnzymeEntry"] as DynamicObjectCollection;
            if (entry == null || entry.Count == 0) return;         
            foreach (DynamicObject item in entry)
            {
                int row = entry.IndexOf(item);
                decimal enzymeUnit = Convert.ToDecimal(item["FJNUnitEnzymes"]);
                decimal qty = 0;
                if (enzymeUnit > 0) qty = enzymeNeed * defMatch / enzymeUnit;
                this.View.Model.SetValue("F_JN_AdviseQty", qty, row);
                this.View.UpdateView();
            }
        }

        /// <summary>
        /// 选择酶活总量
        /// </summary>
        private void EnzymeSelect()
        {
            decimal checkAuxQty = 0;
            DynamicObjectCollection entry = this.View.Model.DataObject["EnzymeEntry"] as DynamicObjectCollection;
            foreach (var item in entry)
            {
                checkAuxQty += Convert.ToDecimal(item["FJNCheckAuxQty"]);
            }
            this.View.Model.SetValue("F_JN_EnzymeSelected", checkAuxQty);
        }
    }
}
