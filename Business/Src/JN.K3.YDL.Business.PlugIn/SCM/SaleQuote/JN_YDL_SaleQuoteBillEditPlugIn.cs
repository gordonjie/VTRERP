using JN.BOS.Business.BillHelper;
using JN.BOS.Core;
using JN.K3.YDL.ServiceHelper;
using JN.K3.YDL.ServiceHelper.SCM;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DependencyRules;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.MFG.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata.EntityElement;

namespace JN.K3.YDL.Business.PlugIn.SCM.SaleQuote
{
    /// <summary>
    /// 销售报价单客户端插件
    /// </summary>
    [Description("销售报价单客户端插件")]
    public class JN_YDL_SaleQuoteBillEditPlugIn : CommonBillEdit
    {
        private DynamicObject _billTypeParameter = null;
        private FormMetadata _billTypeFormMeta = null;
        private const int quotationRoleId = 489044; //销售定价单核算附件

        /// <summary>
        /// 新增后
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateModelData(EventArgs e)
        {
            base.AfterCreateModelData(e);
            this.View.Model.SetValue("FSaleOrgId", Convert.ToInt32(this.Context.CurrentOrganizationInfo.ID));
            //通过当前用户对应的联系对象找到员工
            QueryBuilderParemeter para = new QueryBuilderParemeter();

            para.FormId = "BD_NEWSTAFF";
            para.FilterClauseWihtKey = string.Format(" exists (select 1 from t_sec_User where FLinkObject=FPERSONID and FUSERID={0} and FFORBIDSTATUS='A' )", this.Context.UserId);
            para.SelectItems = SelectorItemInfo.CreateItems(" FSTAFFID ");
            var employeeDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
            if (employeeDatas != null && employeeDatas.Count > 0)
            {
                this.View.Model.SetValue("FApplicantId", Convert.ToInt64(employeeDatas[0]["FSTAFFID"]));
            }


        }

        /// <summary>
        /// 初始化事件
        /// </summary>
        /// <param name="e"></param>
        public override void OnInitialize(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.InitializeEventArgs e)
        {
            base.OnInitialize(e);
            //设置页签控件页签选中事件启用 
            var tabCtl = this.View.GetControl<TabControl>("FTab1");
            tabCtl.SetFireSelChanged(true);
            //加载单据类型参数表单模型
            var strBillTypeFormId = this.View.BillBusinessInfo.GetForm().BillTypePara;
            if (string.IsNullOrWhiteSpace(strBillTypeFormId))
            {
                strBillTypeFormId = "BOS_BILLTYPEPARAMMODEL";
            }
            _billTypeFormMeta = FormMetaDataCache.GetCachedFormMetaData(this.Context, strBillTypeFormId);
            //给价格跟踪 类型设定默认值         


        }

        /// <summary>
        /// 数据包创建后事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);

            this.LoadBillTypeParaObject();

        }

        /// <summary>
        /// 数据加载后逻辑处理
        /// </summary>
        /// <param name="e"></param>
        public override void AfterLoadData(EventArgs e)
        {
            base.AfterLoadData(e);

            this.LoadBillTypeParaObject();
            bool bSupportNoMtrlQuote = this.LoadBillTypeParaValue<bool>("F_JN_NoMtrlIdQuotation", false);
            this.View.StyleManager.SetVisible("F_JN_ProductName", null, bSupportNoMtrlQuote);
            this.View.StyleManager.SetVisible("F_JN_MtrlGroupId", null, bSupportNoMtrlQuote);
            this.View.StyleManager.SetVisible("F_JN_SaleExpense", null, bSupportNoMtrlQuote);
            this.View.StyleManager.SetVisible("F_JN_ApplyPrice", null, bSupportNoMtrlQuote);

            this.View.GetControl<FieldEditor>("F_JN_ProductName").MustInput = bSupportNoMtrlQuote;
            this.View.GetControl<FieldEditor>("FMaterialId").MustInput = !bSupportNoMtrlQuote;

            bool bAutoSyncToPriceLis = this.LoadBillTypeParaValue<bool>("F_JN_AutoSyncToPriceList", false);
            this.View.StyleManager.SetVisible("F_JN_EffectiveDate", null, bAutoSyncToPriceLis);
            this.View.StyleManager.SetVisible("F_JN_ExpiryDate", null, bAutoSyncToPriceLis);

            //更新产品名称
            var Entitydatas = this.View.Model.DataObject["SAL_QUOTATIONENTRY"] as DynamicObjectCollection;
            int rows = Entitydatas.Count;
            EntryGrid grid = this.View.GetControl<EntryGrid>("FQUOTATIONENTRY");

            for (int i = 0; i < rows; i++)
            {


                if (Convert.ToInt32(Entitydatas[i]["MaterialId_id"]) != 0)
                {
                    var FMaterialId = Entitydatas[i]["MaterialId"] as DynamicObject;
                    string FMaterialId_ID = Entitydatas[i]["MaterialId_Id"].ToString();
                    string FNewMaterial = FMaterialId_ID.Substring(0, 2);
                    string DocumentStatus = FMaterialId["DocumentStatus"].ToString();
                    string Fname = FMaterialId["Name"].ToString();
                    //string Status=this.View.Model.GetValue("FDocumentStatus").ToString();
                    if (FNewMaterial != "00")
                    {
                        Entitydatas[i]["F_JN_ProductName"] = Fname;
                    }
                    if (DocumentStatus == "A" || DocumentStatus == "B" || DocumentStatus == "D")
                    {
                        this.View.Model.SetValue("FMATERIALStatus", "B", i);
                    }
                    if (DocumentStatus == "C")
                    {
                        this.View.Model.SetValue("FMATERIALStatus", "C", i);
                    }
                    this.View.UpdateView("F_JN_ProductName");
                    this.View.UpdateView("FMATERIALStatus");
                }

                string status = Convert.ToString(Entitydatas[i]["FMATERIALStatus"]);
                if (status == "A")
                {
                    // grid.SetRowBackcolor("#FFFF00", i);
                    grid.SetForecolor("FMATERIALStatus", "#FFFF00", i);
                    grid.SetBackcolor("FMATERIALStatus", "#FFFF00", i);

                }
                if (status == "B")
                {
                    //grid.SetRowBackcolor("#FF0000", i);
                    grid.SetForecolor("FMATERIALStatus", "#FF0000", i);
                    grid.SetBackcolor("FMATERIALStatus", "#FF0000", i);
                }

            }

        }

        public override void AfterBindData(EventArgs e)
        {
            EntryGrid grid = this.View.GetControl<EntryGrid>("FQUOTATIONENTRY");
            var Entitydatas = this.View.Model.DataObject["SAL_QUOTATIONENTRY"] as DynamicObjectCollection;
            int rows = Entitydatas.Count;
            //EntryGrid grid = this.View.GetControl<EntryGrid>("FQUOTATIONENTRY");
            if (Convert.ToBoolean(this.View.Model.GetValue("F_JN_NEWPRODUCT")) == true)
            {
                this.View.GetControl<FieldEditor>("F_JNLabelName").MustInput = false;
                this.View.GetControl<FieldEditor>("F_JNLabelAdr").MustInput = true;
                this.View.GetControl<FieldEditor>("F_JNlicenses").MustInput = true;
                this.View.GetFieldEditor("F_JNLabelName", 0).MustInput = true;
                this.View.GetFieldEditor("F_JNLabelAdr", 0).MustInput = true;
                this.View.GetFieldEditor("F_JNlicenses", 0).MustInput = true;
            }

            for (int i = 0; i < rows; i++)
            {
                string status = Convert.ToString(Entitydatas[i]["FMATERIALStatus"]);
                if (status == "A")
                {
                    // grid.SetRowBackcolor("#FFFF00", i);
                    //grid.SetForecolor("FMATERIALStatus", "#FFFF00", i);
                    grid.SetBackcolor("FMATERIALStatus", "#FFFF00", i);

                }
                if (status == "B")
                {
                    //grid.SetRowBackcolor("#FF0000", i);
                    // grid.SetForecolor("FMATERIALStatus", "#FF0000", i);
                    grid.SetBackcolor("FMATERIALStatus", "#FF0000", i);
                }

                //判断是否成本附件权限，控制成本计算页签查看权限
                if (YDLCommServiceHelper.IsquotationRoleIdRole(this.Context, quotationRoleId))
                {
                    this.View.GetControl("FTab1_VTR_P").Visible = true;
                }
                else
                {
                    this.View.GetControl("FTab1_VTR_P").Visible = false;
                  
                }
            }
        }


        /// <summary>
        /// 数据绑定前事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeBindData(EventArgs e)
        {
            base.BeforeBindData(e);

            this.LoadBillTypeParaObject();
            bool bSupportNoMtrlQuote = this.LoadBillTypeParaValue<bool>("F_JN_NoMtrlIdQuotation", false);
            this.View.StyleManager.SetVisible("F_JN_ProductName", null, bSupportNoMtrlQuote);
            this.View.StyleManager.SetVisible("F_JN_MtrlGroupId", null, bSupportNoMtrlQuote);
            this.View.StyleManager.SetVisible("F_JN_SaleExpense", null, bSupportNoMtrlQuote);
            this.View.StyleManager.SetVisible("F_JN_ApplyPrice", null, bSupportNoMtrlQuote);

            this.View.GetControl<FieldEditor>("F_JN_ProductName").MustInput = bSupportNoMtrlQuote;
            this.View.GetControl<FieldEditor>("FMaterialId").MustInput = !bSupportNoMtrlQuote;

            bool bAutoSyncToPriceLis = this.LoadBillTypeParaValue<bool>("F_JN_AutoSyncToPriceList", false);
            this.View.StyleManager.SetVisible("F_JN_EffectiveDate", null, bAutoSyncToPriceLis);
            this.View.StyleManager.SetVisible("F_JN_ExpiryDate", null, bAutoSyncToPriceLis);

        }

        /// <summary>
        /// 值更新事件
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Key.ToUpper() == "F_JN_NEWPRODUCT")
            {
                //设置物料代码必填
                if (Convert.ToBoolean(this.View.Model.GetValue("F_JN_NEWPRODUCT")) == true)
                {
                    this.View.GetControl<FieldEditor>("FMaterialId").MustInput = false;
                  
                    this.View.GetControl<FieldEditor>("F_JN_NEWPRODUCT").Enabled = false;//不能再修改
                }
                else
                {
                    this.View.GetControl<FieldEditor>("FMaterialId").MustInput = true;

                }
                DynamicObjectCollection entrydatas = this.View.Model.DataObject["SAL_QUOTATIONENTRY"] as DynamicObjectCollection;
                int rows = entrydatas.Count;
                for (int i = 0; i < rows; i++)
                {
                    this.View.Model.SetValue("FMATERIALStatus", "A",i);
                }

                this.View.UpdateView("FMaterialId");
                this.View.UpdateView("FMATERIALStatus");

                
            }
            string ENZYMEMATERIAL="";
            if (e.Key.ToUpper() == "F_JN_PRODUCTNAME" || e.Key.ToUpper() == "F_JNLABELNAME" || e.Key.ToUpper() == "F_JNLABELADR" || e.Key.ToUpper() == "F_JNLICENSES" || e.Key.ToUpper() == "F_JN_ENZYMEMATERIAL" || e.Key.ToUpper() == "FJNCOMPANYEA" || e.Key.ToUpper() == "F_JNLABELSCOPE" || e.Key.ToUpper() == "F_JNLABELDOSAGE")
            {
                DynamicObjectCollection entrydatas = this.View.Model.DataObject["SAL_QUOTATIONENTRY"] as DynamicObjectCollection;
                int rows = entrydatas.Count;
                for (int i = 0; i < rows;i++ )
                {
                    DynamicObjectCollection sonentrydatas = entrydatas[i]["T_SCM_Components"] as DynamicObjectCollection;


                    foreach (var sonentrydata in sonentrydatas)
                    {
                        DynamicObject ENZYME = sonentrydata["F_JN_ENZYMEMATERIAL"] as DynamicObject;
                        if (ENZYME.IsNullOrEmpty() == false)
                        {
                            string ENZYMEname = Convert.ToString(ENZYME["name"]);
                            double ENZYMEdouble= Convert.ToDouble(sonentrydata["FJNCompanyEA"]);
                            string ENZYMEnum = "0"; 
                            var MaterialStock = ENZYME["MaterialStock"] as DynamicObjectCollection;
                            DynamicObject unit = MaterialStock[0]["AuxUnitID"] as DynamicObject;
                            string unitnum = Convert.ToString(unit["Number"]);
                            if (unitnum == "mhl")
                            { ENZYMEnum = ENZYMEdouble.ToString("#0"); }
                            else { ENZYMEnum = ENZYMEdouble.ToString("#0.0"); }
                            ENZYMEMATERIAL = string.Format("{0}{1}>={2};\r\n", ENZYMEMATERIAL, ENZYMEname, ENZYMEnum);
                        }

                    }

                    string MATERIAL = Convert.ToString(entrydatas[i]["F_JN_ProductName"]);
                    string label = string.Format("产品名称:{0}\r\n产品成份分析保证值:{1}\r\n定制企业:{2}\r\n地址:{3}\r\n生产许可证:{4}\r\n适用范围:{5}\r\n用量:{6}\r\n", MATERIAL, ENZYMEMATERIAL, Convert.ToString(entrydatas[i]["F_JNLABELNAME"]), Convert.ToString(entrydatas[i]["F_JNLABELADR"]), Convert.ToString(entrydatas[i]["F_JNlicenses"]), Convert.ToString(entrydatas[i]["F_JNLabelScope"]), Convert.ToString(entrydatas[i]["F_JNLabelDosage"]));
                    //entrydatas[i]["F_JNLabelRemarks"] = label;
                    this.View.Model.SetValue("F_JNLabelRemarks", label, i);
                    
                }
                this.View.UpdateView("F_JNLabelRemarks");
            }
            if (e.Key.ToUpper() == "FMATERIALID")
            {
                //BOM版本过滤
                DynamicObjectCollection entryDy = this.View.Model.DataObject["SAL_QUOTATIONENTRY"] as DynamicObjectCollection;
                long FMaterialId = Convert.ToInt64(entryDy[e.Row]["MaterialId_Id"]);
                if (FMaterialId == 0) return;
                QueryBuilderParemeter par = new QueryBuilderParemeter();
                par.FormId = "ENG_BOM";
                par.SelectItems = SelectorItemInfo.CreateItems("FID");
                par.FilterClauseWihtKey = string.Format(" FMATERIALID={0}", FMaterialId);
                DynamicObjectCollection FileData = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, par);
                if (FileData.Count == 0) this.View.Model.SetValue("FBOMID", 0, e.Row);
                else
                {
                    int fid = Convert.ToInt32(FileData[0]["FID"]);
                    this.View.Model.SetValue("FBOMID", fid, e.Row);
                }
                this.View.UpdateView();
            }
            //如果成本项目或者酶活发生改变，获取系数和单价
            if (e.Key.ToUpper() == "F_VTR_COSTMATERIAL" )
            {
                
                
                    int activerow = e.Row;
                    this.updatecostentity(activerow);                
            }
            /*
            //系数
            if (e.Key.ToUpper() == "F_VTR_COEFFICIENT")
            {


                int activerow = e.Row;
                this.updatecostentity(activerow);
            }

            //酶活
            if (e.Key.ToUpper() == "F_VTR_COSTENZYMEQTY")
            {


                int activerow = e.Row;
                this.updatecostentity(activerow);
            }

            //单价
            if (e.Key.ToUpper() == "F_VTR_COSTPRICE")
            {


                int activerow = e.Row;
                this.updatecostentity(activerow);
            }

            //半成品酶活
            if (e.Key.ToUpper() == "F_VTR_COSTENZYMEPREPARED")
            {


                int activerow = e.Row;
                this.updatecostentity(activerow);
            }*/
               //如果成本参数发生改变，获取系数和单价
            if (e.Key.ToUpper() == "F_VTR_QUOTATIONCOSTPARAM")
            {
                
               

                    //获取主单据体与其数据集合
                    EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FQUOTATIONENTRY");
                    DynamicObjectCollection entryRows = this.View.Model.GetEntityDataObject(entryEntity);

                    //获取子单据体
                    EntryEntity FSerialSubEntity = this.View.BusinessInfo.GetEntryEntity("F_VTR_COSTEntity");

                    // 取每行主单据体所挂的子单据体集合
                    foreach (var entryRow in entryRows)
                    {
                        DynamicObject COSTPARAM = entryRow["F_VTR_QUOTATIONCOSTPARAM"] as DynamicObject;
                        if (COSTPARAM.IsNullOrEmpty() == false)
                        {                                                  
                        DynamicObjectCollection subEntryRows = FSerialSubEntity.DynamicProperty.GetValue(
                                     entryRow) as DynamicObjectCollection;

                        foreach (var subEntryRow in subEntryRows)
                        {
                            string SHAPETYPE_ID = Convert.ToString(COSTPARAM["F_VTR_ShapeType_id"]);
                            string F_VTR_COSTMATERIAL_ID = Convert.ToString(subEntryRow["F_VTR_COSTMATERIAL_ID"]);
                           // string F_VTR_COSTENZYMEPREPARED = Convert.ToString(subEntryRow["F_VTR_COSTENZYMEPREPARED"]);
                            string billdate = Convert.ToString(this.View.Model.GetValue("FDate"));

                            string sql = string.Format(@"/*dialect*/select T2.F_VTR_COSTENZYMEQTY,t2.F_VTR_COSTPRICE,t2.F_VTR_COEFFICIENT,t2.F_VTR_MATERIALID,t2.F_VTR_SHAPETYPE,t2.F_VTR_COSTENZYMEPREPARED,t1.F_VTR_BEGINDATE from VTR_t_QUOTATIONmaterial t1
join VTR_t_QUOTATIONmaterialEntry t2 on t1.FID=t2.FID
where t1.FDOCUMENTSTATUS='C' and t1.F_VTR_BEGINDATE<='{0}' and t1.F_VTR_ENDDATE>='{0}'
and t2.F_VTR_SHAPETYPE='{1}' and t2.F_VTR_MATERIALID={2} order by t1.F_VTR_BEGINDATE DESC ", billdate, SHAPETYPE_ID, F_VTR_COSTMATERIAL_ID);

                            DynamicObjectCollection getcostitems = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                            if (getcostitems.Count > 0)
                            {
                                subEntryRow["F_VTR_COEFFICIENT"] = getcostitems[0]["F_VTR_COEFFICIENT"];
                                subEntryRow["F_VTR_COSTPrice"] = getcostitems[0]["F_VTR_COSTPrice"];
                                subEntryRow["F_VTR_COSTENZYMEPREPARED"] = getcostitems[0]["F_VTR_COSTENZYMEPREPARED"];
                                // subEntryRows[activerow]["F_VTR_COSTPrice"] = getcostitems[0]["F_VTR_COSTPrice"];
                                if (Convert.ToDecimal(subEntryRow["F_VTR_COSTEnzymePrepared"]) > 0)
                                {
                                    subEntryRow["F_VTR_weight"] = Convert.ToDecimal(subEntryRow["F_VTR_COSTEnzymeQty"]) * Convert.ToDecimal(subEntryRow["F_VTR_coefficient"]) / Convert.ToDecimal(subEntryRow["F_VTR_COSTEnzymePrepared"]);

                                }
                                DynamicObject CostMATERIAL = subEntryRow["F_VTR_CostMATERIAL"] as DynamicObject;
                                if (CostMATERIAL.IsNullOrEmpty()==false)
                                {
                                    Decimal allot = Convert.ToDecimal(CostMATERIAL["F_VTR_allot"]);
                                    if (allot > 0)
                                    {
                                        subEntryRow["F_VTR_COSTAmount"] = Convert.ToDecimal(subEntryRow["F_VTR_COSTEnzymeQty"]) / allot * Convert.ToDecimal(subEntryRow["F_VTR_coefficient"]) * Convert.ToDecimal(subEntryRow["F_VTR_COSTPrice"]);
                                    }
                                }

                            }
                            else
                            {
                                subEntryRow["F_VTR_COEFFICIENT"] = 0;
                                subEntryRow["F_VTR_COSTPrice"] = 0;
                                subEntryRow["F_VTR_COSTENZYMEPREPARED"] = 0;
                                subEntryRow["F_VTR_weight"] = 0;
                                subEntryRow["F_VTR_COSTAmount"] = 0;
                            }
                           }
                        }
                        this.View.UpdateView("F_VTR_COSTEntity");
                    }
                
            }
        }


        private void updatecostentity(int activerow)
        {
            //获取主单据体与其数据集合
            EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FQUOTATIONENTRY");
            DynamicObjectCollection entryRows = this.View.Model.GetEntityDataObject(entryEntity);

            //获取子单据体
            EntryEntity FSerialSubEntity = this.View.BusinessInfo.GetEntryEntity("F_VTR_COSTEntity");

            // 取每行主单据体所挂的子单据体集合
            foreach (var entryRow in entryRows)
            {
                DynamicObjectCollection subEntryRows = FSerialSubEntity.DynamicProperty.GetValue(
                             entryRow) as DynamicObjectCollection;
                // TODO : subEntryRows为每条主单据体行下所挂的子单据体行集合；
                // TODO : subEntryRows[0] 为第一条子单据体行
                DynamicObject COSTPARAM = entryRow["F_VTR_QUOTATIONCOSTPARAM"] as DynamicObject;
                if (COSTPARAM.IsNullOrEmpty() == false)
                {
                    string SHAPETYPE_ID = Convert.ToString(COSTPARAM["F_VTR_ShapeType_id"]);
                    string F_VTR_COSTMATERIAL_ID = Convert.ToString(subEntryRows[activerow]["F_VTR_COSTMATERIAL_ID"]);
                    // string F_VTR_COSTENZYMEPREPARED = Convert.ToString(subEntryRows[activerow]["F_VTR_COSTENZYMEPREPARED"]);
                    string billdate = Convert.ToString(this.View.Model.GetValue("FDate"));

                    string sql = string.Format(@"/*dialect*/select T2.F_VTR_COSTENZYMEQTY,t2.F_VTR_COSTPRICE,t2.F_VTR_COEFFICIENT,t2.F_VTR_MATERIALID,t2.F_VTR_SHAPETYPE,t2.F_VTR_COSTENZYMEPREPARED,t1.F_VTR_BEGINDATE from VTR_t_QUOTATIONmaterial t1
join VTR_t_QUOTATIONmaterialEntry t2 on t1.FID=t2.FID
where t1.FDOCUMENTSTATUS='C' and t1.F_VTR_BEGINDATE<='{0}' and t1.F_VTR_ENDDATE>='{0}'
and t2.F_VTR_SHAPETYPE='{1}' and t2.F_VTR_MATERIALID={2}  order by t1.F_VTR_BEGINDATE DESC ", billdate, SHAPETYPE_ID, F_VTR_COSTMATERIAL_ID);

                    DynamicObjectCollection getcostitems = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                    if (getcostitems.Count > 0)
                    {
                        subEntryRows[activerow]["F_VTR_COEFFICIENT"] = getcostitems[0]["F_VTR_COEFFICIENT"];
                        subEntryRows[activerow]["F_VTR_COSTPrice"] = getcostitems[0]["F_VTR_COSTPrice"];
                        subEntryRows[activerow]["F_VTR_COSTENZYMEPREPARED"] = getcostitems[0]["F_VTR_COSTENZYMEPREPARED"];
                        if (Convert.ToDecimal(subEntryRows[activerow]["F_VTR_COSTEnzymePrepared"])>0)
                        {
                        subEntryRows[activerow]["F_VTR_weight"] = Convert.ToDecimal(subEntryRows[activerow]["F_VTR_COSTEnzymeQty"]) * Convert.ToDecimal(subEntryRows[activerow]["F_VTR_coefficient"]) / Convert.ToDecimal(subEntryRows[activerow]["F_VTR_COSTEnzymePrepared"]);
                        
                        }
                        DynamicObject CostMATERIAL=subEntryRows[activerow]["F_VTR_CostMATERIAL"] as DynamicObject;
                        if (CostMATERIAL.IsNullOrEmpty()==false)
                        {
                            Decimal allot=Convert.ToDecimal(CostMATERIAL["F_VTR_allot"]);
                        if(allot>0)
                            {
                                subEntryRows[activerow]["F_VTR_COSTAmount"] = Convert.ToDecimal(subEntryRows[activerow]["F_VTR_COSTEnzymeQty"]) / allot * Convert.ToDecimal(subEntryRows[activerow]["F_VTR_coefficient"]) * Convert.ToDecimal(subEntryRows[activerow]["F_VTR_COSTPrice"]); 
                             }
                        }// subEntryRows[activerow]["F_VTR_COSTPrice"] = getcostitems[0]["F_VTR_COSTPrice"];

                    }
                    else
                    {
                        subEntryRows[activerow]["F_VTR_COEFFICIENT"] = 0;
                        subEntryRows[activerow]["F_VTR_COSTPrice"] = 0;
                        subEntryRows[activerow]["F_VTR_COSTENZYMEPREPARED"] = 0;
                        subEntryRows[activerow]["F_VTR_weight"] = 0;
                        subEntryRows[activerow]["F_VTR_COSTAmount"] = 0;
                    }
                }
                this.View.UpdateView("F_VTR_COSTEntity");
            }
        }
        /// <summary>
        /// F8选择前事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeF7Select(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.BaseDataField == null) return;
            if (e.BaseDataField.Key.EqualsIgnoreCase("FMaterialId"))
            {
                //如果支持无物编报价，并且产品名称不为空时                
                var strMtrlName = Convert.ToString(this.Model.GetValue("F_JN_ProductName", e.Row));
                if (this.LoadBillTypeParaValue<bool>("F_JN_NoMtrlIdQuotation", false) && !string.IsNullOrWhiteSpace(strMtrlName))
                {
                    e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(string.Format("FName=N'{0}'", strMtrlName));
                }
            }
        }

        /// <summary>
        /// F8赋值前事件
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeSetItemValueByNumber(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeSetItemValueByNumberArgs e)
        {
            base.BeforeSetItemValueByNumber(e);
            if (e.BaseDataFieldKey.EqualsIgnoreCase("FMaterialId"))
            {
                //如果支持无物编报价，并且产品名称不为空时                
                var strMtrlName = Convert.ToString(this.Model.GetValue("F_JN_ProductName", e.Row));
                if (this.LoadBillTypeParaValue<bool>("F_JN_NoMtrlIdQuotation", false) && !string.IsNullOrWhiteSpace(strMtrlName))
                {
                    e.Filter = e.Filter.JoinFilterString(string.Format("FName=N'{0}'", strMtrlName));
                }
            }
        }

        /// <summary>
        /// 单据头按钮事件处理
        /// </summary>
        /// <param name="e"></param>
        public override void BarItemClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BarItemClickEventArgs e)
        {
            switch (e.BarItemKey)
            {
                case "tbQueryPriceList_JN":
                    this.View.ShowList("BD_SAL_PriceList", null, null,
                        string.Format(@"exists(select 1 from T_SAL_PRICELIST u100 
                                                    inner join T_SAL_APPLYCUSTOMER u101 on u100.fid=u101.fid 
                                                    where u100.FLIMITCUSTOMER='1' and u101.fcustid={0} 
                                                            and u100.FCurrencyId={1} 
                                                            and u100.FIsIncludedTax='{2}'
                                                            and u100.FEFFECTIVEDATE<={3} and u100.FEXPIRYDATE>={4}
                                                            and u100.fid=fid
                                                        ) ",
                                    this.Model.DataObject["CustId_Id"],
                                    ((DynamicObjectCollection)this.Model.DataObject["SAL_QUOTATIONFIN"])[0]["SettleCurrId_Id"],
                                    ((bool)((DynamicObjectCollection)this.Model.DataObject["SAL_QUOTATIONFIN"])[0]["IsIncludedTax"]) ? "1" : "0",
                                    ((DateTime?)this.Model.DataObject["EffectiveDate"]).Value.ToKSQlFormat(),
                                    ((DateTime?)this.Model.DataObject["ExpiryDate"]).Value.ToKSQlFormat()), true);
                    break;


            }
        }
        //选中页签事件
        public override void TabItemSelectedChange(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.TabItemSelectedChangeEventArgs e)
        {
            base.TabItemSelectedChange(e);
            string DocumentStatus = Convert.ToString(this.View.Model.GetValue("FDocumentStatus"));
            if (e.TabIndex == 2 && DocumentStatus!="C")
            {
                SetDefaultValue();
                //获取明细的数据
                int row = this.View.Model.GetEntryCurrentRowIndex("FQUOTATIONENTRY");
                DynamicObject goods = this.View.Model.GetValue("FMaterialId", row) as DynamicObject;
                if (goods == null) return;
                string goodsid = Convert.ToString(goods["Id"]);
                decimal TaxPrice = Convert.ToDecimal(this.View.Model.GetValue("FTaxPrice", row) == "" ? 0 : this.View.Model.GetValue("FTaxPrice", row));//申请结算价
                decimal SettlementPrice = Convert.ToDecimal(this.View.Model.GetValue("F_JN_SettlementPrice", row) == "" ? 0 : this.View.Model.GetValue("F_JN_SettlementPrice", row));//批准结算价
                decimal ApplyPrice = Convert.ToDecimal(this.View.Model.GetValue("F_JN_ApplyPrice", row) == "" ? 0 : this.View.Model.GetValue("F_JN_ApplyPrice", row));//销售价
                decimal SaleExpense = Convert.ToDecimal(this.View.Model.GetValue("F_JN_SaleExpense", row) == "" ? 0 : this.View.Model.GetValue("F_JN_SaleExpense", row));//申请费用1
                decimal AFE1 = Convert.ToDecimal(this.View.Model.GetValue("F_JN_AFE1", row) == "" ? 0 : this.View.Model.GetValue("F_JN_AFE1", row));//批准费用1
                decimal FCommission = Convert.ToDecimal(this.View.Model.GetValue("FCommission", row) == "" ? 0 : this.View.Model.GetValue("FCommission", row));//申请费用2
                decimal AFE2 = Convert.ToDecimal(this.View.Model.GetValue("F_JN_AFE2", row) == "" ? 0 : this.View.Model.GetValue("F_JN_AFE2", row));//批准费用2
                decimal befTaxPrice = 0;//申请前结算价
                decimal befApplyPrice = 0;//申请前销售价
                decimal befAFE1 = 0;
                decimal befAFE2 = 0;
                string SALESPROMOTIONbase = Convert.ToString(this.View.Model.GetValue("F_JN_SALESPROMOTION", row));//销售促销
                string F_JN_SALESPROMOTION = string.Format("批准：{0}", SALESPROMOTIONbase);
                //显示历史销售促销                                                                 
                //根据物料id查找申请前价格
                //DynamicObjectCollection data = YDLCommServiceHelper.GetPriceFormsData(this.Context, goodsid);
                //根据物料id、客户id、币别id查找申请前价格

                DynamicObject cust = this.View.Model.GetValue("FCUSTID") as DynamicObject;
                string custid= Convert.ToString(cust["Id"]);
                DynamicObject curr = this.View.Model.GetValue("FSettleCurrId") as DynamicObject;
                string currencyid = Convert.ToString(curr["Id"]);
                DynamicObject auxprop = this.View.Model.GetValue("FAuxPropId", row) as DynamicObject;
                string auxpropid= Convert.ToString(auxprop["Id"]);

                DynamicObject sale = this.View.Model.GetValue("FSalerId") as DynamicObject;
                string saleId = Convert.ToString(sale["Id"]);


                DynamicObjectCollection data = YDLCommServiceHelper.GetPriceFormsDataByCust(this.Context, goodsid, custid, saleId,currencyid, auxpropid);
                if (data != null && data.Count != 0)
                {
                    befTaxPrice = Convert.ToDecimal(data[0]["FDOWNPRICE"]);
                    befApplyPrice = Convert.ToDecimal(data[0]["FPRICE"]);
                    befAFE1 = Convert.ToDecimal(data[0]["F_JN_SALEEXPENSE"]);
                    befAFE2 = Convert.ToDecimal(data[0]["F_JN_SALEEXPENSE2"]);
                    string SALESPROMOTION= Convert.ToString(data[0]["F_JN_SALESPROMOTION"]);
                    F_JN_SALESPROMOTION = string.Format("{0},申请前：{1}", F_JN_SALESPROMOTION, SALESPROMOTION);
                }
                //结算价
                this.View.Model.SetValue("F_JN_BefApplyPrice", befTaxPrice, 0);//申请前价格
                this.View.Model.SetValue("F_JN_ApplyForPrice", TaxPrice, 0);//申请价格
                this.View.Model.SetValue("F_JN_RatifyPrice", SettlementPrice, 0);//批准价格
                //销售价
                this.View.Model.SetValue("F_JN_BefApplyPrice", befApplyPrice, 1);//申请前价格
                this.View.Model.SetValue("F_JN_ApplyForPrice", ApplyPrice, 1);//申请价格
                this.View.Model.SetValue("F_JN_RatifyPrice", ApplyPrice, 1);//批准价格
                //费用1
                this.View.Model.SetValue("F_JN_BefApplyPrice", befAFE1, 2);//申请前费用1
                this.View.Model.SetValue("F_JN_ApplyForPrice", SaleExpense, 2);//申请费用1
                this.View.Model.SetValue("F_JN_RatifyPrice", AFE1, 2);//批准费用1
                //费用2
                this.View.Model.SetValue("F_JN_BefApplyPrice", befAFE2, 3);//申请前费用2
                this.View.Model.SetValue("F_JN_ApplyForPrice", FCommission, 3);//申请费用2
                this.View.Model.SetValue("F_JN_RatifyPrice", AFE2, 3);//批准费用2
                //销售促销                                                     
                this.View.Model.SetValue("F_JN_BefApplyPrice", 0, 4);//申请前费用2
                this.View.Model.SetValue("F_JN_ApplyForPrice", 0, 4);//申请费用2
                this.View.Model.SetValue("F_JN_RatifyPrice", 0, 4);//批准费用2
                this.View.Model.SetValue("F_JN_PriceRemack", F_JN_SALESPROMOTION, 4);//批准费用2
            

            }
        }
        /// <summary>
        /// 分录按钮事件处理
        /// </summary>
        /// <param name="e"></param>
        public override void EntryBarItemClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BarItemClickEventArgs e)
        {
            var entryEntity = this.View.BillBusinessInfo.GetEntity("FQUOTATIONENTRY");

            DynamicObject dynamicO = this.View.Model.DataObject;//全部单据信息
            switch (e.BarItemKey)
            {
                case "tbCreateBom_JN":
                    this.CreateOrUpdateBom();
                    break;
                case "tbCreateMaterial_JN":
                    if (this.LoadBillTypeParaValue<string>("F_JN_MtrlCreateTimePoint", "1") == "1")
                    {


                        //交互式生成

                        var focusRow = this.View.GetControl<EntryGrid>("FQUOTATIONENTRY").GetFocusRowIndex();
                        var focusRowObj = this.Model.GetEntityDataObject(entryEntity, focusRow);
                        if (focusRowObj != null)
                        {
                            var mtrlField = this.View.BillBusinessInfo.GetField("FMaterialId");
                            var productNameField = this.View.BillBusinessInfo.GetField("F_JN_ProductName");
                            var mtrlGroupField = this.View.BillBusinessInfo.GetField("F_JN_MtrlGroupId") as BaseDataField;
                            //var isNewMtrlField = this.View.BillBusinessInfo.GetField("F_JN_IsNewMtrl");

                            Action<FormResult> retFunc = new Action<FormResult>((ret) =>
                            {
                                if (ret.ReturnData is DynamicObject)
                                {
                                    var entryRowObjs = this.Model.GetEntityDataObject(entryEntity);
                                    var strRefProductName = productNameField.DynamicProperty.GetValue<string>(focusRowObj);

                                    var lMtrlId = (long)((DynamicObject)(ret.ReturnData))["Id"];
                                    //把当前表体里与焦点行产品名称一样的行的物料编码都回填上。
                                    foreach (var entryRow in entryRowObjs)
                                    {
                                        if (productNameField.DynamicProperty.GetValue<string>(entryRow).EqualsIgnoreCase(strRefProductName))
                                        {
                                            this.Model.SetValue(mtrlField, entryRow, lMtrlId);

                                            int rowIndex = this.Model.GetRowIndex(entryEntity, entryRow);
                                            this.View.InvokeFieldUpdateService(mtrlField.Key, rowIndex);

                                            //if (!lMtrlId.IsEmptyPrimaryKey())
                                            //{
                                            //    this.Model.SetValue(isNewMtrlField, entryRow, true);
                                            //}
                                        }
                                    }
                                }
                            });

                            //TODO:根据焦点行产品组别，获取对应的物料模板数据包
                            var strRefMtrlId = "";
                            var mtrlGroupId = mtrlGroupField.RefIDDynamicProperty.GetValue<long>(focusRowObj);

                            if (_billTypeParameter != null)
                            {
                                var mtrlTplItem = ((DynamicObjectCollection)_billTypeParameter["QuoteMtrlTplEntity"]).FirstOrDefault(o => mtrlGroupId == (long)o["F_JN_MtrlGroupId_Id"]);
                                if (mtrlTplItem == null)
                                {
                                    this.View.ShowWarnningMessage("创建产品代码失败：未找到当前产品组别所对应的物料模板，请检查销售系统参数中报价参数设置！");
                                    return;
                                }
                                strRefMtrlId = Convert.ToString(mtrlTplItem["F_JN_TplMtrlId_Id"]);
                            }
                            else
                            {
                                return;
                            }

                            if (!strRefMtrlId.IsEmptyPrimaryKey())
                            {
                                this.View.CopySingleBill("BD_MATERIAL", strRefMtrlId, "", retFunc, (showPara) =>
                                {
                                    //父级实体行对象
                                    showPara.CustomComplexParams["__ParentEntryObject__"] = focusRowObj;
                                });
                            }
                            else
                            {
                                this.View.ShowBill("BD_MATERIAL", null, retFunc, (showPara) =>
                                {
                                    //父级实体行对象
                                    showPara.CustomComplexParams["__ParentEntryObject__"] = focusRowObj;
                                });
                            }
                        }
                    }
                    else if (this.LoadBillTypeParaValue<string>("F_JN_MtrlCreateTimePoint", "1") == "3")
                    {
                        CreateMATERIALByHand();

                    }
                    //审核时生成，由审核插件处理
                    break;
                //产品成本查询
                //case "tbProductCostSelect":
                //    Kingdee.BOS.Core.Report.SysReportShowParameter param = new Kingdee.BOS.Core.Report.SysReportShowParameter();
                //    param.FormId = "CA_PRODUCTSTDCOSTRPT";
                //    param.OpenStyle.ShowType = ShowType.Default;
                //    param.IsShowFilter = true;
                //    //Kingdee.BOS.Core.CommonFilter.ListRegularFilterParameter filter = new Kingdee.BOS.Core.CommonFilter.ListRegularFilterParameter();
                //    ////Kingdee.BOS.Core.CommonFilter.FilterParameter filter = new Kingdee.BOS.Core.CommonFilter.FilterParameter();
                //    //filter.SetFieldValue("FACCTGSYSTEMID", 1);
                //    //filter.SetFieldValue("FACCTGORGID", 1);
                //    //filter.SetFieldValue("FACCTPOLICYID", 1);
                //    //QueryBuilderParemeter par = new QueryBuilderParemeter();
                //    //par.FormId = "CA_STDCOSTVERSION";
                //    //par.SelectItems = SelectorItemInfo.CreateItems("FVERSIONID");
                //    //par.FilterClauseWihtKey = " FISDEFAULT = '1' ";
                //    //DynamicObjectCollection FileData = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, par);
                //    //if (FileData.Count() > 0) filter.SetFieldValue("FVERSIONID", Convert.ToInt32(FileData[0]["FVERSIONID"]));
                //    //var Row = this.View.GetControl<EntryGrid>("FQUOTATIONENTRY").GetFocusRowIndex();
                //    //var Obj = this.Model.GetEntityDataObject(entryEntity, Row);
                //    //if (Obj != null)
                //    //{
                //    //    int materialid = Convert.ToInt32(Obj["MaterialId_Id"]);
                //    //    filter.SetFieldValue("FSTARTPRODUCTID", materialid);
                //    //}                    
                //    //param.ReportFilterParameter = filter;
                //    this.View.ShowForm(param);
                //    break;
                case "getEnzyme":
                    DynamicObjectCollection entrydatas = this.View.Model.DataObject["SAL_QUOTATIONENTRY"] as DynamicObjectCollection;
                    int rows = entrydatas.Count;
                    for (int i = 0; i < rows; i++)
                    {
                        DynamicObjectCollection sonentrydatas = entrydatas[i]["T_SCM_Components"] as DynamicObjectCollection;
                        DynamicObjectCollection COSTdatas = entrydatas[i]["F_VTR_COSTEntity"] as DynamicObjectCollection;
                        Entity billentity = this.View.BillBusinessInfo.GetEntity("F_VTR_COSTEntity");
                        
                        

                        COSTdatas.Clear();
                        foreach (var sonentrydata in sonentrydatas)
                        {
                            DynamicObject newCOSTdata = new DynamicObject(billentity.DynamicObjectType);
                            DynamicObject F_JN_ENZYMEMATERIAL = sonentrydata["F_JN_ENZYMEMATERIAL"] as DynamicObject;
                            newCOSTdata["F_VTR_CostMATERIAL_Id"] = F_JN_ENZYMEMATERIAL["id"];
                            newCOSTdata["F_VTR_CostMATERIAL"] = F_JN_ENZYMEMATERIAL;
                            newCOSTdata["F_VTR_COSTENZYMEQTY"] = sonentrydata["FJNCompanyEA"];
                            COSTdatas.Add(newCOSTdata);
                            this.updatecostentity(i);
                        }
                        this.View.UpdateView("F_VTR_COSTEntity");

                    }
                    break;
            }
        }

        public override void AfterDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            if (e.Operation.Operation.ToString() == "ProductCostSelect")
            {
                Kingdee.BOS.Core.Report.SysReportShowParameter param = new Kingdee.BOS.Core.Report.SysReportShowParameter();
                param.FormId = "CA_PRODUCTSTDCOSTRPT";
                param.OpenStyle.ShowType = ShowType.Default;
                param.IsShowFilter = true;
                this.View.ShowForm(param);
            }
            if (e.Operation.Operation.ToString() == "Audit")
            {
                this.View.Refresh();
            }
            if (e.Operation.Operation.ToString() == "Save")
            {
                string status = Convert.ToString(this.View.Model.GetValue("FDocumentStatus"));
                if (status != "Z")
                {
                    CreateMATERIALByHand();
                }
            }
        }

        // public override void beforedo



        public override void BeforeDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.BeforeDoOperationEventArgs e)
        {
            // base.BeforeDoOperation(e);
            string operation = e.Operation.ToString();
            if (e.Operation.ToString() == "Kingdee.BOS.Business.Bill.Operation.Save")
            {
                if (Convert.ToBoolean(this.Model.GetValue("F_JN_NewProduct")) == true)
                {

                    var Entitydatas = this.View.Model.DataObject["SAL_QUOTATIONENTRY"] as DynamicObjectCollection;
                    string FCUSTID = Convert.ToString(this.Model.GetValue("FCUSTID"));
                    var F_JN_ApplyCustIds = this.Model.GetValue("F_JN_ApplyCustIds") as DynamicObjectCollection;
                    if (FCUSTID == "" && F_JN_ApplyCustIds.Count == 0)
                    {
                        this.View.ShowWarnningMessage("客户和适用更多客户不能同时为空！");
                        e.Cancel = true;
                        return;
                    }



                    foreach (var Entitydata in Entitydatas)
                    {
                        //this.View.UpdateView("FSubEntity1");
                        var sonEntitydatas = Entitydata["T_SCM_Components"] as DynamicObjectCollection;
                        int row = sonEntitydatas.Count;
                        int data = row;
                        for (int i = row - 1; i >= 0; i--)
                        {
                            if (sonEntitydatas[i]["F_JN_ENZYMEMATERIAL"].IsNullOrEmptyOrWhiteSpace() && sonEntitydatas[i]["F_JNCOMPONENTSText"].IsNullOrEmptyOrWhiteSpace())
                            {


                                data = data - 1;
                            }
                        }
                        if (data < 1)
                        {

                            this.View.ShowWarnningMessage("新建产品时，申请配方必填！");
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }
        /*
            if (e.Operation.ToString() == "Kingdee.BOS.Business.Bill.Operation.Audit")
            {
                CreateMATERIALByHand();
            }*/

        }
        /// <summary>
        /// 手工创建物料
        /// </summary>
        private void CreateMATERIALByHand()
        {
            if (Convert.ToBoolean(this.View.Model.GetValue("F_JN_NEWPRODUCT")) == true)
            {
                DynamicObject dynamicO = this.View.Model.DataObject;//全部单据信息
                var entryEntity = this.View.BillBusinessInfo.GetEntity("FQUOTATIONENTRY");
                var entrydata = this.Model.GetEntityDataObject(entryEntity) as DynamicObjectCollection;

                int[] selRows = new int[entrydata.Count];
                for (int i = 0; i < entrydata.Count; i++)
                { selRows[i] = i;
                    //初始化物料分组
                    DynamicObject[] objs = BusinessDataServiceHelper.Load(this.Context, new object[] {110193},
                        (MetaDataServiceHelper.Load(this.Context, "CPCM_MATERIALGROUP") as FormMetadata).BusinessInfo.GetDynamicObjectType());

                    //QueryBuilderParemeter queryParam = new QueryBuilderParemeter();
                    //queryParam.FormId = "CPCM_MATERIALGROUP";
                    // using Kingdee.BOS.Core.Metadata;
                    //queryParam.SelectItems.Add(new SelectorItemInfo("Id"));
                    //queryParam.SelectItems.Add(new SelectorItemInfo("Number"));
                    //queryParam.SelectItems.Add(new SelectorItemInfo("Name"));
                    //queryParam.SelectItems.Add(new SelectorItemInfo("MultiLanguageText"));
                    //queryParam.SelectItems.Add(new SelectorItemInfo("FName"));
                    //queryParam.FilterClauseWihtKey = string.Format(" Fid = '{0}' ", "110193");
                    // using Kingdee.BOS.ServiceHelper;
                    //var objs = QueryServiceHelper.GetDynamicObjectCollection(this.Context, queryParam);


                    this.View.Model.SetValue("F_JN_MtrlGroupId", objs[0],i );
                    //string type = this.View.Model.GetValue("F_JN_MtrlGroupId_Id", i);
                }

                entryEntity = this.View.BillBusinessInfo.GetEntity("FQUOTATIONENTRY");//重新获取

                var needCreateRowObjs = selRows.Select(o => this.Model.GetEntityDataObject(entryEntity, o))
                            .Where(o => o != null && (string)o["FMATERIALStatus"] == "A")
                            .ToArray();


                string billno = Convert.ToString(this.View.GetView("FBillNo"));

               // string type = Convert.ToString(this.View.Model.GetValue("F_JN_MtrlGroupId", 0));
                //创建物料编码前判断产品名称必填
                foreach (var needCreateRowObj in needCreateRowObjs)
                {
                    var productname = needCreateRowObj["F_JN_ProductName"];

                    int entityindex = Convert.ToInt32(needCreateRowObj["Id"]);

                    if (productname == "" || productname == " " || productname == null)
                    {
                        return;
                    }
                    if (entityindex == 0)
                    {
                        this.View.ShowWarnningMessage("请先保存单据！");
                        return;
                    }
                }

                //--
                if (needCreateRowObjs.Any() == false)
                {
                    //this.View.ShowWarnningMessage("选中的所有行的产品编码都已生成！");
                    return;
                }
               
                var result = SaleQuoteServiceHelper.CreateProductMaterial(this.Context,
                    this.View.BillBusinessInfo,
                    needCreateRowObjs,
                    OperateOption.Create(), dynamicO);

                //TODO:回填创建完成的物料信息
                if (result.IsSuccess)
                {
                    var dctRetMtrlData = result.FuncResult as Dictionary<DynamicObject, DynamicObject>;
                    if (dctRetMtrlData != null)
                    {
                        var mtrlField = this.View.BillBusinessInfo.GetField("FMaterialId");
                        var productName = this.View.BillBusinessInfo.GetField("F_JN_ProductName");
                        var MATERIALStatus = this.View.BillBusinessInfo.GetField("FMATERIALStatus");
                        //var F_JNMATERIALID = this.View.BillBusinessInfo.GetField("F_JNMATERIALID");
                        var isNewMtrlField = this.View.BillBusinessInfo.GetField("F_JN_IsNewMtrl");
                        foreach (var kvpItem in dctRetMtrlData)
                        {
                            var lMtrlId = (long)kvpItem.Value["Id"];
                            var lMtrlName = kvpItem.Value["Name"].ToString();
                            this.Model.SetValue(mtrlField, kvpItem.Key, lMtrlId);

                            //this.Model.SetValue("", lMtrlId, 0);
                            this.Model.SetValue(MATERIALStatus, kvpItem.Key, "B");
                            this.Model.SetValue(productName, kvpItem.Key, lMtrlName);
                            //this.Model.SetValue(F_JNMATERIALID, kvpItem.Key, lMtrlId);

                            int rowIndex = this.Model.GetRowIndex(entryEntity, kvpItem.Key);
                            string entityindex = Convert.ToString(kvpItem.Key["id"]);
                            string sql = string.Format("update T_SAL_QUOTATIONENTRY set FMATERIALID={0} where FENTRYID={1}", lMtrlId, entityindex);
                            DBUtils.Execute(this.Context, sql);
                            this.View.InvokeFieldUpdateService(mtrlField.Key, rowIndex);
                            //if (!lMtrlId.IsEmptyPrimaryKey())
                            //{
                            //    this.Model.SetValue(isNewMtrlField, kvpItem.Key, true);
                            //}
                            //this.View.StyleManager.SetEnabled("F_JN_ProductName", kvpItem.Key, "F_JN_ProductName", false);
                            this.View.Refresh();
                           // this.View.Model.Save();
                        }
                    }
                }

                // this.View.ShowOperationResult(result);

            }

        }

        /// <summary>
        /// 创建或修改BOM
        /// </summary>
        private void CreateOrUpdateBom()
        {
            var focusRow = this.View.GetControl<EntryGrid>("FQUOTATIONENTRY").GetFocusRowIndex();
            var entryEntity = this.View.BillBusinessInfo.GetEntity("FQUOTATIONENTRY");
            var entryRowObj = this.Model.GetEntityDataObject(entryEntity, focusRow);
            if (entryRowObj == null) return;
            if (entryRowObj["MaterialId_Id"].IsEmptyPrimaryKey())
            {
                this.View.ShowWarnningMessage("当前行还未录入物料编码或者还未生成物料编码，不能进行物料清单维护！");
                return;
            }

            var lBomId = Convert.ToString(entryRowObj["BomId_Id"]);
            if (lBomId == "0") lBomId = "";
            //this.View.ShowBill("ENG_BOM", lBomId, (result) =>
            //    {

            //        //TODO:根据上述回调刷新当前表格行关联的BOM版本。

            //        if (result.ReturnData is DynamicObject)
            //        {
            //            this.Model.SetValue("FBomId", (result.ReturnData as DynamicObject)["Id"], focusRow);
            //        }
            //    }, (showPara) =>
            //    {
            //        showPara.CustomComplexParams["__ParentEntryObject__"] = entryRowObj;
            //    });
            //DynamicObjectCollection dynamicO = this.View.Model.DataObject["SAL_QUOTATIONENTRY"] as DynamicObjectCollection;
            //DynamicObject metarialid = dynamicO[0]["BomId"] as DynamicObject;
            if (lBomId != "")
            {
                BillShowParameter paraml = new BillShowParameter { FormId = "ENG_BOM" };
                paraml.OpenStyle.ShowType = ShowType.Default;
                paraml.Status = OperationStatus.EDIT;
                paraml.PKey = lBomId.ToString();
                this.View.ShowForm(paraml);
            }
            else
            {
                BillShowParameter param = new BillShowParameter { FormId = "ENG_BOM" };
                param.CustomParams.Add("materialId", Convert.ToString(entryRowObj["MaterialId_Id"]));
                param.OpenStyle.ShowType = ShowType.Default;
                param.Status = OperationStatus.ADDNEW;
                this.View.ShowForm(param, (result) =>
                {
                    //TODO:根据上述回调刷新当前表格行关联的BOM版本。
                    if (result.ReturnData is DynamicObject)
                    {
                        this.Model.SetValue("FBomId", (result.ReturnData as DynamicObject)["Id"], focusRow);
                    }
                });
            }
        }

        /// <summary>
        /// 读取单据类型关联的业务参数对象
        /// </summary>
        private void LoadBillTypeParaObject()
        {
            var billTypeField = this.View.BillBusinessInfo.GetBillTypeField() as BillTypeField;
            if (billTypeField != null)
            {
                var billTypeId = billTypeField.RefIDDynamicProperty.GetValue<string>(this.Model.DataObject);
                if (!string.IsNullOrWhiteSpace(billTypeId))
                {
                    _billTypeParameter = BusinessDataServiceHelper.LoadBillTypePara(this.Context, _billTypeFormMeta.BusinessInfo, billTypeId);
                }
            }
        }

        /// <summary>
        /// 读取单据类型参数（容错）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="strParaFieldKey"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        private T LoadBillTypeParaValue<T>(string strParaFieldKey, T defValue)
        {
            if (_billTypeFormMeta == null) return defValue;
            if (_billTypeParameter == null) return defValue;

            BOSDynamicRow dyRow = new BOSDynamicRow(_billTypeParameter, "FBillHead", _billTypeFormMeta.BusinessInfo);
            return (T)Convert.ChangeType(dyRow.GetFieldSimpleValue(strParaFieldKey), typeof(T));
        }

        //给价格跟踪 类型设定默认值
        private void SetDefaultValue()
        {
            this.View.Model.SetValue("F_JN_Type", "A", 0);
            this.View.Model.SetValue("F_JN_Type", "B", 1);
            this.View.Model.SetValue("F_JN_Type", "C", 2);
            this.View.Model.SetValue("F_JN_Type", "D", 3);
            this.View.Model.SetValue("F_JN_Type", "E", 4);
    }

    }
}
