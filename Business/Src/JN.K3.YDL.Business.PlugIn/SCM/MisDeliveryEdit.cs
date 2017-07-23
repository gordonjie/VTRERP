using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.SCM.Sal.Business.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn.SCM
{
    /// <summary>
    /// 其他出库单-表单插件
    /// </summary>
    [Description("其他出库单-表单插件")]
    public class MisDeliveryEdit : AbstractBillPlugIn
    {
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);

            /*5.2之前，6.2后运行报错进行修改，采用系统方法设置
            int contactId = 0;//联系人Id
            if (e.Key.ToString() == "FCustId")//客户
            {
                int custId = Convert.ToInt32(this.View.Model.DataObject["CustId_Id"]);                
                QueryBuilderParemeter defaultPara = new QueryBuilderParemeter();
                defaultPara.FormId = "BD_Customer";//客户                
                defaultPara.FilterClauseWihtKey = string.Format("FCUSTID={0} AND FISDEFAULT=1", custId);
                //defaultPara.SelectItems = SelectorItemInfo.CreateItems("FCONTACTID");//联系人
                defaultPara.SelectItems = SelectorItemInfo.CreateItems("FTContact");//联系人
                DynamicObjectCollection defaultDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, defaultPara);
                if (defaultDatas.Count > 0) 
                {
                    contactId = Convert.ToInt32(defaultDatas[0]["FCONTACTID"]);
                    this.View.Model.SetValue("FReceiveContact", contactId);
                }
                else
                {
                    QueryBuilderParemeter para = new QueryBuilderParemeter();
                    para.FormId = "BD_Customer";
                    para.FilterClauseWihtKey = string.Format("FCUSTID={0}", custId);
                    para.SelectItems = SelectorItemInfo.CreateItems("FCONTACTID");
                    DynamicObjectCollection datas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
                    if (datas.Count > 0)
                    {
                        contactId = Convert.ToInt32(datas[0]["FCONTACTID"]);
                        this.View.Model.SetValue("FReceiveContact", contactId);
                    }
                }
                if (contactId == 0) return;
                GetAddress(contactId);                
            }
            if (e.Key.ToString() == "FReceiveContact")//联系人
            {
                contactId = Convert.ToInt32(this.View.Model.DataObject["FReceiveContact_Id"]);
                GetAddress(contactId);  
            }*/
            if (e.Key.ToString() == "FCustId")//客户
            {
                DynamicObject loc = Common.SetDefaultHeadLoc(this, "FCustId", "F_VTR_HEADLOCID", true, 0);
                Common.SetContact(this, loc, "FReceiveContact", "FCustId");

            }
            if (e.Key.ToString() == "FReceiveContact")//联系人
            {
                DynamicObject loc = Common.SetDefaultHeadLoc(this, "FCustId", "F_VTR_HEADLOCID", true, 0);
            }

        }

        /// <summary>
        /// 获取客户地址
        /// </summary>
        /// <param name="contactId">联系人</param>
        private void GetAddress(int contactId)
        {
            QueryBuilderParemeter defaultPath = new QueryBuilderParemeter();
            defaultPath.FormId = "BD_CUSTCONTACTION";//客户地点
            defaultPath.FilterClauseWihtKey = string.Format("FTCONTACT={0} AND FISDEFAULTCONSIGNEE=1", contactId);
            defaultPath.SelectItems = SelectorItemInfo.CreateItems("FADDRESS");//收货地址
            DynamicObjectCollection defaultPathData = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, defaultPath);
            if (defaultPathData.Count > 0) this.View.Model.SetValue("FReceiveAddress", defaultPathData[0]["FADDRESS"]);
            else
            {
                QueryBuilderParemeter path = new QueryBuilderParemeter();
                path.FormId = "BD_CUSTCONTACTION";
                path.FilterClauseWihtKey = string.Format("FTCONTACT={0}", contactId);
                path.SelectItems = SelectorItemInfo.CreateItems("FADDRESS");
                DynamicObjectCollection pathData = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, path);
                if (pathData.Count > 0) this.View.Model.SetValue("FReceiveAddress", pathData[0]["FADDRESS"]);
            }
        }

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
            para.FilterClauseWihtKey = string.Format(" exists (select 1 from t_sec_User where FLinkObject=FPERSONID and FUSERID={0} )", this.Context.UserId);
            para.SelectItems = SelectorItemInfo.CreateItems(" FSTAFFID ");
            var employeeDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, para);
            if (employeeDatas != null && employeeDatas.Count > 0)
            {
                this.View.Model.SetValue("FApplicantId", Convert.ToInt64(employeeDatas[0]["FSTAFFID"]));
            }
        }
    }
}
