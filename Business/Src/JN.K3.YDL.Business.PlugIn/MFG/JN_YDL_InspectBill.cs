using JN.K3.YDL.ServiceHelper.SCM;
using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Util;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.BOS.Orm;
namespace JN.K3.YDL.Business.PlugIn.MFG
{
    [Description("检验单表单插件")]
    public class JN_YDL_InspectBill : CommonBillEdit
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (View.OpenParameter.Status == OperationStatus.ADDNEW && (base.View.OpenParameter.CreateFrom == CreateFrom.Push || base.View.OpenParameter.CreateFrom == CreateFrom.Draw))
            {
                SetComboItems();
            }
        }

        public void SetComboItems()
        {
            DynamicObject billType = this.View.Model.GetValue("FBillTypeID") as DynamicObject;
            if (billType == null) return;
            string typeId = Convert.ToString(billType["Id"]);
            string typeName = Convert.ToString(billType["Name"]);
            //样品检验单和生产过程检验单和入库检验
            if (typeId != "565be729cfbfe8" && typeId != "565be7accfc117" && typeId != "56691f0d4a90f8") return;
            //区分样品检验单和生产过程检验单和入库检验
            //string value = (typeId == "565be729cfbfe8") ? "8" : "9";
            string value = "";
            if (typeId == "565be729cfbfe8") value = "8";
            if (typeId == "565be7accfc117") value = "9";
            
            if (typeId == "56691f0d4a90f8") value = "0";
            //增加枚举
            List<EnumItem> list = new List<EnumItem>();
            EnumItem item = new EnumItem()
            {
                Value = value,
                Caption = new LocaleValue(typeName, this.Context.UserLocale.LCID)
            };
            list.Add(item);
            this.View.GetControl<ComboFieldEditor>("FBusinessType").SetComboItems(list);

            this.View.Model.SetValue("FBusinessType", list[0].Value);
            //生产过程检验需要设置使用决策,标准产品会清空使用决策。这里重新加上
            if (typeId == "565be7accfc117")
            {
                CreateNewPlicyRow();
            }
        }

        private void CreateNewPlicyRow()
        {
            int rowCount=this.View.Model.GetEntryRowCount("FEntity");
            for (int i = 0; i < rowCount; i++)
            {
                this.View.SetEntityFocusRow("FEntity", i);
                if (this.View.Model.GetEntryRowCount("FPolicyDetail") == 0)
                {
                    this.View.Model.CreateNewEntryRow("FPolicyDetail");
                }

            }
        }

        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            switch (e.Field.Key.ToUpperInvariant())
            {
                case "FPOLICYSTATUS":
             
                    //SetDefalutQty("1");///设置决策行数据 6.2版本注释
                                       ///
                    SetprintLowRemarks();

                    break;
                case "FINSPECTVALQ":
                    SetprintLowRemarks();
                    break;
                case "FINSPECTVALB":
                    SetprintLowRemarks();
                    break;
                case "FINSPECTVALT":
                    SetprintLowRemarks();
                    break;
                case "FINSPECTRESULT1":
                    SetprintLowRemarks();
                    break;
                default:
                    break;
            }
            base.DataChanged(e);
        }

        public override void AfterCreateNewEntryRow(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.CreateNewEntryEventArgs e)
        {
            base.AfterCreateNewEntryRow(e);
            if (e.Entity.Key.EqualsIgnoreCase("FPolicyDetail"))
            {
             SetFusePolicyComboItems(e.Row);//使用决策绑定

                //SetDefalutQty("2");///设置决策行数据

              
            }

        }

       

        public override void EntityRowClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.EntityRowClickEventArgs e)
        {
            if (e.Key.EqualsIgnoreCase("FPolicyDetail"))
            {
                SetFusePolicyComboItems(e.Row);//使用决策绑定

            }

            base.EntityRowClick(e);
        }

        /// <summary>
        /// 打印设置让步接收单
        /// </summary>
        /// <param name="type"></param>
        private void SetprintLowRemarks()
        {
            var Entity = this.View.BusinessInfo.GetEntity("FEntity");
            var EntityDatas = this.View.Model.GetEntityDataObject(Entity);
            int row = this.View.Model.GetEntryRowCount("FEntity");
                for(int i=0;i<row;i++)
                {

                var ItemDetails = EntityDatas[i]["ItemDetail"] as DynamicObjectCollection;
                string print = "";//不合格项
                string print2 = "";//接收条件
                string print3 = "";//打印日期
                var flot = EntityDatas[i]["lot"] as DynamicObject;
                string flotnum = Convert.ToString(flot["Number"]);
                if (flotnum.Length >= 8)
                {
                    print3 = Convert.ToString(flot["Number"]).Substring(0, 8);
                }

                
                
                foreach (var ItemDetail in ItemDetails)
                {
                    string InspectResult = Convert.ToString(ItemDetail["InspectItemId"]);
                    if (ItemDetail["InspectResult"].ToString() == "2")
                    {
                        var Items = ItemDetail["InspectItemId"] as DynamicObject;
                        string ItemDetailname = Convert.ToString(Items["name"]);
                        
                        if (ItemDetail["AnalysisMethod"].ToString() == "1")
                        {
                            string uplimit  =  ItemDetail["UpLimit"].ToString();
                            string downlimit = ItemDetail["DownLimit"].ToString();
                            string targetVal = ItemDetail["TargetValQ"].ToString();
                            string CompareSymbol = "";
                            double BaseUnitEnzymes = Convert.ToDouble(ItemDetail["TargetValQ"]);
                            double checkUnitEnzymes= Convert.ToDouble(ItemDetail["InspectValQ"]);
                            string ratio =string.Format("{0:F}",(BaseUnitEnzymes - checkUnitEnzymes) / BaseUnitEnzymes*100);
                            if (uplimit == targetVal)
                            {
                                CompareSymbol = "<=";
                            }
                            if (downlimit == targetVal)
                            {
                                CompareSymbol = ">=";
                            }

                            print = string.Format("{0}\r\n{1}标准值:{2}{3}，实测值:{4}", print, ItemDetailname, CompareSymbol, Convert.ToString(ItemDetail["TargetVal"]), Convert.ToString(ItemDetail["InspectValQ"]));
                            print2 = string.Format("{0}\r\n({1}-{2})/{3}×100%={4}%\r\n按{5}酶活折价{6}%入库。",print2, Convert.ToString(BaseUnitEnzymes), Convert.ToString(checkUnitEnzymes), Convert.ToString(BaseUnitEnzymes), ratio, Convert.ToString(checkUnitEnzymes), ratio);

                            
                        }
                        if (ItemDetail["AnalysisMethod"].ToString() == "2")
                        {
                            var InspectValB = ItemDetail["InspectValB"] as DynamicObject;
                            print = string.Format("{0}\r\n{1}标准值:{2}，实测值:{3}", print, ItemDetailname, Convert.ToString(ItemDetail["TargetVal"]), Convert.ToString(InspectValB["name"]));
                            print2 = string.Format("{0}\r\n实测{1},不合格，折价处理。", print2,Convert.ToString(InspectValB["name"]));
                        }

                        if (ItemDetail["AnalysisMethod"].ToString() == "3")
                        {
                            print = string.Format("{0}\r\n{1}标准值:{2}，实测值：{3}", print, ItemDetailname, Convert.ToString(ItemDetail["TargetVal"]), Convert.ToString(ItemDetail["InspectValT"]));
                            print2 = string.Format("{0}\r\n实测{0},不合格，折价处理。", print2,Convert.ToString(ItemDetail["InspectValT"]));
                        }
                    }
                }


                //EntityData["F_JNPrintLowRemarks"] = print;
                this.View.Model.SetValue("F_JNPrintLowRemarks", print, i);
                this.View.Model.SetValue("F_JNPrintLowexists", print2, i);
                this.View.Model.SetValue("F_JNPrintdate", print3, i);
            }
        }

        /// <summary>
        /// type:2时新增行时触发,1:状态触发触取触 发行号
        /// </summary>
        /// <param name="type"></param>
        private void SetDefalutQty(string type)
        {

            int rowIndex = this.View.Model.GetEntryCurrentRowIndex("FEntity");
            decimal inspectQty = Convert.ToDecimal(this.View.Model.GetValue("FInspectQty", rowIndex));//检验数量 
            decimal qualifiedQty = Convert.ToDecimal(this.View.Model.GetValue("FQualifiedQty", rowIndex));//合格数
            decimal unqualifiedQty = Convert.ToDecimal(this.View.Model.GetValue("FUnqualifiedQty", rowIndex));//不合格数
            DynamicObject dyObj = null;
            int row = 0;
            this.View.Model.TryGetEntryCurrentRow("FEntity", out dyObj, out row);
            if (dyObj != null)
            {
                int countRow = this.View.Model.GetEntryRowCount("FPolicyDetail");
                DynamicObjectCollection policyEntry = dyObj["PolicyDetail"] as DynamicObjectCollection;//决策单据体
                if (policyEntry != null)
                {
                    int updateRow = 0;
                    if (type.EqualsIgnoreCase("1"))
                    {
                            updateRow= this.View.Model.GetEntryCurrentRowIndex("FPolicyDetail");
                    }
                    else
                    {
                        updateRow = countRow - 1;
                    }
                    string policyStatus = Convert.ToString(this.View.Model.GetValue("FPolicyStatus", updateRow));
                    if (policyStatus.EqualsIgnoreCase("1"))
                    {
                        decimal curentQualifiedQty = policyEntry.Where(p => p != null && Convert.ToString(p["PolicyStatus"]).EqualsIgnoreCase("1"))
                                           .Sum(s => Convert.ToDecimal(s["PolicyQty"]));//当前合格总数量 
                        this.View.Model.SetValue("FPolicyQty", qualifiedQty < curentQualifiedQty ? 0 : qualifiedQty - curentQualifiedQty, updateRow);///赋值当前余下合格数
                    }
                    else if (policyStatus.EqualsIgnoreCase("2"))
                    {
                        decimal curentUnQualifiedQty = policyEntry.Where(p => p != null && Convert.ToString(p["PolicyStatus"]).EqualsIgnoreCase("2"))
                                           .Sum(s => Convert.ToDecimal(s["PolicyQty"]));//当前不合格总数量 
                        this.View.Model.SetValue("FPolicyQty", unqualifiedQty < curentUnQualifiedQty ? 0 : unqualifiedQty - curentUnQualifiedQty, updateRow);///赋值当前余下不合格数
                     
                    }


                }

            }
        }
        public override void AfterEntryBarItemClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            switch (e.BarItemKey.ToUpper())
            {
                case "TBROWAUDIT":
                    RowStatus();
                    break;
                case "TBSPLITROWAUDIT":
                    RowStatus();
                    break;
                case "TBROWUNAUDIT":
                    RowStatus();
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 行审核和反审核
        /// </summary>
        public void RowStatus()
        {
            var ItemDetailEntry = this.View.BillBusinessInfo.GetEntryEntity("FItemDetail") as SubEntryEntity;
            var Hstatus = this.View.BillBusinessInfo.GetField("FZHBillStatus") as ComboField;
            int RowIndex = this.View.GetControl<EntryGrid>("FItemDetail").GetFocusRowIndex();
            var obj = this.Model.GetEntityDataObject(ItemDetailEntry, RowIndex);
            var status = Hstatus.DynamicProperty.GetValue<string>(obj);
            if (status == "A")
            {
                this.View.Model.SetValue("FZHBillStatus", "B", RowIndex);
                this.View.GetBarItem("FItemDetail", "tbDeleteDetail2").Enabled = false;
            }
            else
            {
                this.View.Model.SetValue("FZHBillStatus", "A", RowIndex);
                this.View.GetBarItem("FItemDetail", "tbDeleteDetail2").Enabled = true;
            }
        }

        public void SetFusePolicyComboItems(int row)
        {
            ComboField field = base.View.BusinessInfo.GetField("FUsePolicy") as ComboField;
            IEnumerable<DynamicObject> enumerable = (from g in field.EnumObject.GetDynamicObjectItemValue<DynamicObjectCollection>("Items", null)
                                                     orderby g.GetDynamicObjectItemValue<int>("Seq", 0)
                                                     select g).ToList<DynamicObject>();
            DynamicObject billType = this.View.Model.GetValue("FBillTypeID") as DynamicObject;
            if (billType == null) return;
            string typeId = Convert.ToString(billType["Id"]);
            //样品检验单和生产过程检验单和入库检验
            if (typeId == "565be729cfbfe8" || typeId == "565be7accfc117" || typeId == "56691f0d4a90f8")
            {

                List<string> lstValue = new List<string>() { "A", "C", "D", "E", "G" };
                List<DynamicObject> listObj = (from p in enumerable
                                               where lstValue.Contains(p.GetDynamicObjectItemValue<string>("Value", null))
                                               select p).ToList<DynamicObject>();

                List<EnumItem> usePolicyItems = new List<EnumItem>();
                foreach (DynamicObject obj3 in listObj)
                {
                    EnumItem item = new EnumItem
                    {
                        Value = obj3.GetDynamicObjectItemValue<string>("Value", null),
                        Caption = obj3.GetDynamicObjectItemValue<LocaleValue>("Caption", null),
                        Seq = obj3.GetDynamicObjectItemValue<int>("Seq", 0)
                    };
                    usePolicyItems.Add(item);
                }
               
                this.View.GetFieldEditor<ComboFieldEditor>("FUsePolicy", row).SetComboItems(usePolicyItems);
                //this.View.Model.SetValue("FUsePolicy", usePolicyItems[0].Value, row);
                
               
            }
        }

        public override void OnPrepareNotePrintData(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePrintDataEventArgs e)
        {
            base.OnPrepareNotePrintData(e);
            var Entity = this.View.BusinessInfo.GetEntity("FEntity");
            var EntityDatas = this.View.Model.GetEntityDataObject(Entity);
            int rows = this.Model.GetEntryRowCount("FEntity");
            //循环单据体数据行
            for (int j = 0; j < rows; j++)
            {
                //逐行设置活动行
                this.Model.SetEntryCurrentRowIndex("FEntity", j);

                //打印让步接收
                //var ItemDetails = EntityDatas[j]["ItemDetail"] as DynamicObjectCollection;

                string print = "";//不合格项
                string print2 = "";//接收条件
                string print3 = "";//打印日期
                DynamicObject flot = EntityDatas[j]["lot"] as DynamicObject;
                if (flot.IsNullOrEmpty() == false)
                {
                    string flotnum = Convert.ToString(flot["Number"]);
                    if (flotnum.Length >= 8)
                    {
                        print3 = Convert.ToString(flot["Number"]).Substring(0, 8);
                    }
                }

/*不用
                foreach (var ItemDetail in ItemDetails)
                {
                    //string InspectResult = Convert.ToString(ItemDetail["InspectItemId"]);
                    if (ItemDetail["InspectResult"].ToString() == "2")
                    {
                        var Items = ItemDetail["InspectItemId"] as DynamicObject;
                        string ItemDetailname = Convert.ToString(Items["name"]);

                        if (ItemDetail["AnalysisMethod"].ToString() == "1")
                        {
                            string uplimit = ItemDetail["UpLimit"].ToString();
                            string downlimit = ItemDetail["DownLimit"].ToString();
                            string targetVal = ItemDetail["TargetValQ"].ToString();
                            string CompareSymbol = "";
                            double BaseUnitEnzymes = Convert.ToDouble(ItemDetail["TargetValQ"]);
                            double checkUnitEnzymes = Convert.ToDouble(ItemDetail["InspectValQ"]);
                            string ratio = string.Format("{0:F}", (BaseUnitEnzymes - checkUnitEnzymes) / BaseUnitEnzymes * 100);
                            if (uplimit == targetVal)
                            {
                                CompareSymbol = "<=";
                            }
                            if (downlimit == targetVal)
                            {
                                CompareSymbol = ">=";
                            }

                            print = string.Format("{0}\r\n{1}标准值:{2}{3}，实测值:{4}", print, ItemDetailname, CompareSymbol, Convert.ToString(ItemDetail["TargetVal"]), Convert.ToString(ItemDetail["InspectValQ"]));
                            print2 = string.Format("{0}\r\n({1}-{2})/{3}×100%={4}%\r\n按{5}酶活折价{6}%入库。", print2, Convert.ToString(BaseUnitEnzymes), Convert.ToString(checkUnitEnzymes), Convert.ToString(BaseUnitEnzymes), ratio, Convert.ToString(checkUnitEnzymes), ratio);


                        }
 
                    }
                }


                //EntityData["F_JNPrintLowRemarks"] = print;

*/


                //-----
                int row =this.View.Model.GetEntryRowCount("FItemDetail");
                for (int i = 0; i < row; i++)
                {



                    string fangfa = this.View.Model.GetValue("FAnalysisMethod", i).ToString();
                    string jieguo = this.View.Model.GetValue("FInspectResult1", i).ToString();
                    string FTargetVal = this.View.Model.GetValue("FTargetVal", i).ToString();

                    string uplimit = Convert.ToString(this.View.Model.GetValue("FUpLimit", i));
                    string downlimit = Convert.ToString(this.View.Model.GetValue("FDownLimit", i));
                    string targetVal = Convert.ToString(this.View.Model.GetValue("FTargetVal", i));
                    DynamicObject InspectItemId = this.View.Model.GetValue("FInspectItemId", i) as DynamicObject;
                    string Compare = Convert.ToString(this.View.Model.GetValue("FCompareSymbol", i));
                    string InspectItemname = Convert.ToString(InspectItemId["Name"]);


                    if (fangfa == "2" && jieguo == "1")
                    {
                        this.View.Model.SetValue("F_JNprintvalue", "符合标准", i);
                        this.View.Model.SetValue("F_JNPrintStd", FTargetVal, i);
                    }
                    if (fangfa == "2" && jieguo == "2")
                    {
                        this.View.Model.SetValue("F_JNprintvalue", "不符合标准", i);
                        this.View.Model.SetValue("F_JNPrintStd", FTargetVal, i);

                        DynamicObject InspectValB = this.View.Model.GetValue("FInspectValB",i) as DynamicObject;
                        print = string.Format("{0}\r\n{1}标准值:{2}，实测值:{3}", print, InspectItemname, targetVal, Convert.ToString(InspectValB["name"]));
                        print2 = string.Format("{0}\r\n实测{1},不合格，折价处理。", print2, Convert.ToString(InspectValB["name"]));
                    }

                    if (fangfa == "3" && jieguo == "2")
                    {
                        print = string.Format("{0}\r\n{1}标准值:{2}，实测值：{3}", print, InspectItemname, targetVal, Convert.ToString(this.View.Model.GetValue("FInspectValT",i)));
                        print2 = string.Format("{0}\r\n实测{0},不合格，折价处理。", print2, Convert.ToString(this.View.Model.GetValue("FInspectValT", i)));
                    }

                        if (fangfa == "1")
                    {
                        string CompareSymbol = "";
                        double BaseUnitEnzymes = Convert.ToDouble(this.View.Model.GetValue("FTargetValQ",i));
                        double checkUnitEnzymes = Convert.ToDouble(this.View.Model.GetValue("FInspectValQ",i));
                        string ratio = string.Format("{0:F}", (BaseUnitEnzymes - checkUnitEnzymes) / BaseUnitEnzymes * 100);
                        if (InspectItemname == "PH值")
                        {
                            CompareSymbol = string.Format("{0}-{1}", downlimit, uplimit);
                        }
                        else
                        {
                            switch (Compare){
                                case "2":
                                    CompareSymbol = string.Format(">{0}", FTargetVal);
                                    break;
                                case "3":
                                    CompareSymbol = string.Format(">={0}", FTargetVal);
                                    break;
                                case "4":
                                    CompareSymbol = string.Format("<{0}", FTargetVal);
                                    break;
                                case "5":
                                    CompareSymbol = string.Format("<={0}", FTargetVal);
                                    break;
                                default:
                                    if (uplimit == targetVal)
                                    {
                                        CompareSymbol = string.Format("<={0}", FTargetVal);
                                    }
                                    if (downlimit == targetVal)
                                    {
                                        CompareSymbol = string.Format(">={0}", FTargetVal);
                                    }
                                    break; }
                        }
                        if (jieguo == "2")
                        {
                            print = string.Format("{0}\r\n{1}标准值:{2}，实测值:{3}", print, InspectItemname, CompareSymbol, Convert.ToString(this.View.Model.GetValue("FInspectValQ", i)));
                            print2 = string.Format("{0}\r\n({1}-{2})/{3}×100%={4}%\r\n按{5}酶活折价{6}%入库。", print2, Convert.ToString(BaseUnitEnzymes), Convert.ToString(checkUnitEnzymes), Convert.ToString(BaseUnitEnzymes), ratio, Convert.ToString(checkUnitEnzymes), ratio);

                        }
                        this.View.Model.SetValue("F_JNPrintStd", CompareSymbol, i);

                        this.View.Model.SetValue("F_JNprintvalue", this.View.Model.GetValue("FInspectValQ", i).ToString(), i);

                     
                    }
                }

                //SetprintLowRemarks();取消，运行慢
                this.View.Model.SetValue("F_JNPrintLowRemarks", print, j);
                this.View.Model.SetValue("F_JNPrintLowexists", print2, j);
                this.View.Model.SetValue("F_JNPrintdate", print3, j);


                
            }
            this.View.Model.Save();
        }

        

        //public override void OnLoad(EventArgs e)
        //{
        //    base.OnLoad(e);
        //    var user=this.Context.UserId;
        //    DynamicObjectCollection SelectUser = SaleQuoteServiceHelper.SelectUserInspector(this.Context,user);
        //    if (SelectUser != null && SelectUser.Count > 0)
        //    {
        //        this.View.Model.SetValue("FInspectorId", SelectUser[0]["fid"]);
        //        this.View.Model.SetValue("FInspectDepId", SelectUser[0]["FDEPTID"]);
        //        this.View.UpdateView("FInspectorId");
        //        this.View.UpdateView("FInspectDepId");
        //    }
        //}

       

    }
}
