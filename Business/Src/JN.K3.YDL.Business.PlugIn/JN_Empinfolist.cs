using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.List.PlugIn;

namespace JN.K3.YDL.Business.PlugIn
{
    [Description("员工-列表插件")]
    public class JN_Empinfolist : AbstractListPlugIn
    {
        public override void AfterDoOperation(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string opertion = e.Operation.Operation.ToString();
            if (opertion != "UpateReport") return;
            UptateReport();
        }

        private void UptateReport()
        {
            //throw new NotImplementedException();
            //更新汇报关系1
            string strSql = string.Format(@"/*dialect*/with cte as
(select t1.Fpostid,t1.FREPORTTYPE,t3.FID,FSUPERIORPOST,lvl=1 from T_ORG_POSTREPORTLINE t1
join  dbo.T_BD_STAFFTEMP t2 on t2.FPOSTID=t1.FPOSTID
join dbo.T_HR_EMPINFO t3 on t3.FID=t2.FID
where FSUPERIORPOST ='101351'    union all    
select a.Fpostid,a.FREPORTTYPE,f.FID,a.FSUPERIORPOST,lvl=b.lvl+1 from T_ORG_POSTREPORTLINE a 
join  dbo.T_BD_STAFFTEMP d on a.FPOSTID=d.FPOSTID
join dbo.T_HR_EMPINFO f on d.FID=f.FID
join cte b on a.FSUPERIORPOST=b.Fpostid  where a.FREPORTTYPE='aa4d1d1b3b184a888ee835387604e955') 

update 
T_HR_EMPINFO  set F_JNREPORT1=(
select top 1 职位 from(
select 职位=case lvl when 1 then '1级' when 2 then '2级' when 3 then '3级' when 4 then '4级' else '员工' end,FID,FREPORTTYPE from cte) c 
 where T_HR_EMPINFO.FID=c.FID
 and c.职位 is not null
 )");
            DBUtils.Execute(this.Context, strSql);
            //更新汇报关系2
            strSql = string.Format(@"/*dialect*/with cte as
(select t1.Fpostid,t1.FREPORTTYPE,t3.FID,FSUPERIORPOST,lvl=1 from T_ORG_POSTREPORTLINE t1
join  dbo.T_BD_STAFFTEMP t2 on t2.FPOSTID=t1.FPOSTID
join dbo.T_HR_EMPINFO t3 on t3.FID=t2.FID
where FSUPERIORPOST ='101351'    union all    
select a.Fpostid,a.FREPORTTYPE,f.FID,a.FSUPERIORPOST,lvl=b.lvl+1 from T_ORG_POSTREPORTLINE a 
join  dbo.T_BD_STAFFTEMP d on a.FPOSTID=d.FPOSTID
join dbo.T_HR_EMPINFO f on d.FID=f.FID
join cte b on a.FSUPERIORPOST=b.Fpostid  where a.FREPORTTYPE='56a04f2a5790f4') 

update 
T_HR_EMPINFO  set F_JNREPORT2=(
select top 1 职位 from(
select 职位=case lvl when 1 then '1级' when 2 then '2级' when 3 then '3级' when 4 then '4级' else '员工' end,FID,FREPORTTYPE from cte) c 
 where T_HR_EMPINFO.FID=c.FID
 and c.职位 is not null
 )");
            DBUtils.Execute(this.Context, strSql);
        }

    }
}
