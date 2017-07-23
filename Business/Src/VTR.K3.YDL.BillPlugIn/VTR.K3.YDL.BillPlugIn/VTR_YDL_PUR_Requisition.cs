using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;

namespace VTR.K3.YDL.BillPlugIn
{
    public class VTR_YDL_PUR_Requisition : AbstractBillPlugIn
    {
        private DynamicObject currRow = null;
        private int rowindex = 0;
        /*
        public override void OnInitialize(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.InitializeEventArgs e)
        {
            base.OnInitialize(e);
            //拆单协作团队处理
            string srcID = this.View.Model.GetValue("FSRCID", 0).ToString();
            string srcType = this.View.Model.GetValue("FSRCFORMID", 0).ToString();
            if (srcID != "0" && srcType == "PUR_PurchaseOrder")
            {
                string sql = string.Format(@"select * from T_CRM_Allocations where FOBJECTBILLID={0}", srcID);
            
            var objs = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
            if (objs == null)
            {
                 sql = string.Format(@"select MAX (fid+1) from T_CRM_Allocations");
                 var crmid=DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                string insertsql = string.Format(@"insert into T_CRM_Allocations (fid,fformid,FOBJECTID,FOBJECTBILLID) values ('{0}','CRM_Allocations','PUR_Requisition',{1})",crmid[0].ToString(), srcID);
                DBServiceHelper.ExecuteDynamicObject(this.Context, insertsql);
                sql = string.Format(@"select FCOOPERATIONTYPE,FEMPLOYEE,FDEPT,FREAD,FMODIFY,FDELETE,FALLOCATION,FCRMCLOSE,FCRMUNCLOSE from t_CRM_AllocationsEntry t1
join t_CRM_Allocations t2 on t1.FID=t2.FID
where t2.FOBJECTID='PUR_Requisition' and t2.FOBJECTBILLID='{1}'", srcID);
                var CRMobjs = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                if (CRMobjs.Count > 0 && CRMobjs != null)
                {
                    foreach(var CRMobj in CRMobjs)
                    {
                        insertsql = string.Format(@"insert into t_CRM_AllocationsEntry (FID,FENTRYID,FCOOPERATIONTYPE,FEMPLOYEE,FDEPT,FREAD,FMODIFY,FDELETE,FALLOCATION,FCRMCLOSE,FCRMUNCLOSE) values ({0},(select MAX (FENTRYID+1) from t_CRM_AllocationsEntry),'{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}')", crmid[0].ToString(), CRMobj[1].ToString(), CRMobj[2].ToString(), CRMobj[3].ToString(), CRMobj[4].ToString(), CRMobj[5].ToString(), CRMobj[6].ToString(), CRMobj[7].ToString(), CRMobj[8].ToString());
                        DBServiceHelper.ExecuteDynamicObject(this.Context, insertsql);
                    }

                }
            }

            }


        }
         */
        public override void DataChanged(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string str = e.Field.Key.ToUpper();

            if (str == "FSUGGESTSUPPLIERID")
            {
                DateTime reqdate = Convert.ToDateTime(this.View.Model.GetValue("FApplicationDate"));
                this.Model.TryGetEntryCurrentRow("FEntity", out currRow, out rowindex);
                int index = Convert.ToInt32(currRow["SEQ"]) - 1;
                DynamicObject FMATERIALID = this.View.Model.GetValue("FMATERIALID", index) as DynamicObject;
                DynamicObject FSupplierId = this.View.Model.GetValue("FSuggestSupplierId", index) as DynamicObject;
                if (FMATERIALID == null || FSupplierId== null) return;
                var materid = FMATERIALID["id"];
                var SupplierId = FSupplierId["id"];
                var sql = string.Format(@"select top 1 t3.FID from T_PUR_ReqEntry t1 inner join t_PUR_PriceListEntry t2 on t1.FMATERIALID=t2.FMATERIALID
                                        inner join t_PUR_PriceList t3 on t3.FID=t2.FID where t2.FMATERIALID='{0}' and t3.FEFFECTIVEDATE<= '{1}' and t3.FEXPIRYDATE>='{2}' and t3.FSUPPLIERID={3} order by t3.FMODIFYDATE DESC", materid, reqdate, reqdate, SupplierId);
                var objs = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
                if (objs != null && objs.Count > 0)
                {
                    Entity entitys = this.View.BusinessInfo.GetEntity("FEntity");
                    var FEntityDatas = this.View.Model.GetEntityDataObject(entitys);
                    int pricelist =Convert.ToInt32(objs[0]["FID"]);
                    //FEntityDatas[index]["FPriceList"][""]=pricelist;
                    this.View.Model.SetValue("FPriceList", pricelist, index);
                    //FEntityDatas[index]["EvaluatePrice"] = objs[0]["FPRICE"];
                    this.View.UpdateView("FPriceList");
                    this.View.UpdateView("FEntity");
                }
            }
        }
    }
}
