using JN.K3.YDL.App.ServicePlugIn.Common;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.App.ServicePlugIn.SCM.SaleOrder
{
    /// <summary>
    /// 销售订单审核插件
    /// </summary>
    [Description("销售订单提交插件")]
    public class JN_Submitflow : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 添加服务插件可能操作到的字段
        /// </summary>
        /// <param name="e"></param>
        private bool startValidator = true;
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FSalerId");
           
            
        }



        private Boolean setstartValidator(string userId, SqlParam param)
        {
            /* 当前用户不等于销售员*/
            string sql = string.Format(@"select t1.FENTRYID as salser from T_BD_OPERATORENTRY  t1
join T_BD_STAFF t2 on t1.FSTAFFID=t2.FSTAFFID 
join T_SEC_user t3 on t3.FLINKOBJECT=t2.FPERSONID
where t1.FOPERATORTYPE='XSY' and t3.FType=1 and t3.FUSERID={0}", userId);
            DynamicObjectCollection userChecks = DBUtils.ExecuteDynamicObject(this.Context, sql, null, null, CommandType.Text, new SqlParam[] { param });
            if (userChecks.Count > 0)
            {
                return true;
            }
            else return false;
        }


        /// <summary>
        /// 增加操作校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            /*停用启动判断
            if (e.DataEntities == null) return;
            var billGroups = e.DataEntities;
            //List<string> sql = new List<string>();
            string userId = Convert.ToString(this.Context.UserId);
            List<long> lstFids = new List<long>();
            foreach (var billGroup in billGroups)
            {
                lstFids.Add(Convert.ToInt64(billGroup["SalerId_Id"]));                            
            }
            SqlParam param = new SqlParam("@salser", KDDbType.udt_inttable, lstFids.ToArray());
            startValidator=this.setstartValidator(userId,param);*/
            if (startValidator)
            {
                JN_AuditValidator SubmitValidator = new JN_AuditValidator();
                SubmitValidator.EntityKey = "FBillHead";
                e.Validators.Add(SubmitValidator);
            }
        }

    }
}
