using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Business.PlugIn
{



    public class CommonBillEdit : AbstractBillPlugIn
    {
        public override void PreOpenForm(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreOpenFormEventArgs e)
        {
            base.PreOpenForm(e);

            //this.View.CheckLicense();
        }

        public override void OnInitialize(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.InitializeEventArgs e)
        {
            DateTime now = DateTime.Now;
            if (this.Context != null)
            {
                now = Kingdee.BOS.ServiceHelper.TimeServiceHelper.GetSystemDateTime(this.Context);
            }
            else if (e.Paramter.Context != null)
            {
                now = Kingdee.BOS.ServiceHelper.TimeServiceHelper.GetSystemDateTime(e.Paramter.Context);
            }
            /**
            if (now > new DateTime(2016, 10, 1))
            {
                throw new KDException("错误提示", "您使用的软件出现了致命错误，请联系管理员");
            }**/
            base.OnInitialize(e);
        }





    }






    [Description("试用到期提示")]
    public class CommonDynamicFormEdit : AbstractDynamicFormPlugIn
    {
        public override void PreOpenForm(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreOpenFormEventArgs e)
        {
            base.PreOpenForm(e);
            //this.View.CheckLicense();
        }

        public override void OnInitialize(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.InitializeEventArgs e)
        {
            DateTime now = DateTime.Now;
            if (this.Context != null)
            {
                now = Kingdee.BOS.ServiceHelper.TimeServiceHelper.GetSystemDateTime(this.Context);
            }
            else if (e.Paramter.Context != null)
            {
                now = Kingdee.BOS.ServiceHelper.TimeServiceHelper.GetSystemDateTime(e.Paramter.Context);
            }

            if (now > new DateTime(2016, 10, 1))
            {
                throw new KDException("错误提示", "您使用的软件出现了致命错误，请联系管理员");
            }
            base.OnInitialize(e);
        }


    }




}
