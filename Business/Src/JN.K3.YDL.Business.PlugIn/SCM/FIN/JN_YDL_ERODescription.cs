using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using JN.K3.YDL.ServiceHelper;
using System.Data;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;

namespace JN.K3.YDL.Business.PlugIn.SCM.FIN
{
    /// <summary>
    /// 付款和费用申请单单据类型说明维护表单插件
    /// </summary>
    [Description("付款和费用申请单单据类型说明维护表单插件")]
    public  class JN_YDL_ERODescription: AbstractDynamicFormPlugIn
    {     
        const string fEntity = "FEntity";
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="e"></param>
        public override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GetData(); //获取付款和费用申请单所有单据类型说明
        }
        /// <summary>
        /// 菜单按钮点击事件
        /// </summary>
        /// <param name="e"></param>
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            //base.BarItemClick(e);
            switch (e.BarItemKey.ToUpper())
            {
                case "TBSAVEBTN": //保存
                    DynamicObjectCollection dyEntrys = this.View.Model.DataObject[fEntity] as DynamicObjectCollection;
                    if (dyEntrys != null && dyEntrys.Count > 0)
                    {
                        //创建数据表
                        DataTable dt = new DataTable();
                        dt.TableName = "T_META_FORMENUMITEM_L";

                        //主键
                        DataColumn dc = new DataColumn();
                        dc.DataType = typeof(System.String);
                        dc.ColumnName = "FPKID";
                        dt.Columns.Add(dc);

                        //说明
                        dc = new DataColumn();
                        dc.DataType = typeof(System.String);
                        dc.ColumnName = "FDESCRIPTION";
                        dt.Columns.Add(dc);
                        
                        //开始填充数据
                        dt.BeginLoadData();
                        foreach (DynamicObject item in dyEntrys)
                        {
                            DataRow dr = dt.NewRow();
                            dr["FPKID"] = item["FPKID"].ToString();               //主键
                            dr["FDESCRIPTION"] = item["FDESCRIPTION"].ToString(); //说明
                            dt.Rows.Add(dr);
                        }
                        dt.EndLoadData();
                        //结束填充数据

                        if (dt.Rows.Count > 0)
                        {
                            //设置物理表名和修改的表数据
                            BatchSqlParam batchUpdateParam = new BatchSqlParam("T_META_FORMENUMITEM_L",dt);
                            //设置条件,前面表示数据表的列名,后面表示物理表的列名
                            batchUpdateParam.AddWhereExpression("FPKID", KDDbType.String, "FPKID");
                            //设置需要修改的字段,前面表示数据表的列名,后面表示物理表的列名
                            batchUpdateParam.AddSetExpression("FDESCRIPTION", KDDbType.String, "F_JN_YDL_Description_EXT");
                            int result = DBUtils.BatchUpdate(this.Context, batchUpdateParam);
                            if (result > 0)
                            {
                                this.View.ShowMessage("保存成功!");
                            }
                            else
                            {
                                this.View.ShowErrMessage("保存失败!");
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        //获取付款和费用申请单所有单据类型说明
        private void GetData()
        {
            DynamicObjectCollection dyData = YDLCommServiceHelper.GetExpenseRequestOrderEditInfo(this.View.Context, GetFilterString());
            FillData(dyData);
        }

        //过滤条件
        private string GetFilterString()
        {
            //费用申请单类型
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" FNAME = '费用申请单类型'");
            return sb.ToString();
        }

        //填充数据
        private void FillData(DynamicObjectCollection dyCountData)
        {
            int i = 1;
            this.View.Model.BeginIniti();

            this.View.Model.DeleteEntryData(fEntity);
            DynamicObjectCollection dyEntrys = this.View.Model.DataObject[fEntity] as DynamicObjectCollection;
            foreach (DynamicObject item in dyCountData)
            {
                DynamicObject dyEntry = new DynamicObject(dyEntrys.DynamicCollectionItemPropertyType);
                this.View.Model.SetEntryCurrentRowIndex(fEntity, i);
                dyEntry["SEQ"] = i;                             //序号
                dyEntry["FPKID"] = item["FPKID"];                //主键
                dyEntry["FVALUE"] = item["FVALUE"];              //单据类型
                dyEntry["FDESCRIPTION"] = item["FDESCRIPTION"];  //说明

                dyEntrys.Add(dyEntry);

                i++;
            }

            DynamicObject[] dyArray = { this.View.Model.DataObject };
            Kingdee.BOS.ServiceHelper.DBServiceHelper.LoadReferenceObject(Context, dyArray, this.View.Model.DataObject.DynamicObjectType, false);

            this.View.Model.EndIniti();

            this.View.UpdateView(fEntity);
        }

    }
}
