using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.K3.Core.SCM.STK;
using Kingdee.K3.SCM.Contracts;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Contracts;
namespace JN.K3.YDL.App.ServicePlugIn.Common
{
    [Description("锁库反锁库公用")]
    public static class LockCommon
    {
        public static string UnLockInventory(Context ctx, DynamicObjectCollection dyUnLockInfo,string formId)
        {
           
            if (dyUnLockInfo == null || dyUnLockInfo.Count < 1) return "";

            List<LockStockArgs> saveList = new List<LockStockArgs>();
            foreach (DynamicObject dynEntry in dyUnLockInfo)
            {
                LockStockArgs arg = new LockStockArgs
                {
                    FEntryID = Convert.ToInt64(dynEntry["Id"]),
                    FInvDetailID = Convert.ToString(dynEntry["FInvDetailID"]),
                    BillDetailID = Convert.ToString(dynEntry["BillDetailID"]),
                    LockSecQty = Convert.ToDecimal(dynEntry["FSECQTY"]),
                    UnLockSecQty = Convert.ToDecimal(dynEntry["FSECQTY"]),
                    LockBaseQty = Convert.ToDecimal(dynEntry["FBASEQTY"]),
                    UnLockBaseQty = Convert.ToDecimal(dynEntry["FBASEQTY"])
                };
                saveList.Add(arg);
            }
            if (saveList.Count > 0)
            {
                Kingdee.K3.SCM.Contracts.ServiceFactory.GetService<IStockLockService>(ctx).SaveUnLockInfo(ctx, saveList, formId);
               
            }
            return string.Empty;
        }

        /// <summary>
        /// 获取批次商品每行主键ID
        /// </summary>
        /// <returns></returns>
        public static string GetLockParams(DynamicObjectCollection dyns, List<long> entryIds)
        {
            List<long> paras = new List<long>();
            foreach (DynamicObject dyn in dyns)
            {
                long pkId = Convert.ToInt64(dyn["Id"]);
                if (entryIds != null && entryIds.Count > 0)
                {
                    if (entryIds.Contains(pkId))
                    {
                        paras.Add(pkId);
                    }
                }
                else
                {
                    paras.Add(pkId);
                }
            }
            return string.Join(",", paras);
        }


       public static void LockInventory(Context ctx,IOperationResult oper, string dataSql,string formId)
        {
            //判断是否已有货审信息
            //List<long> lstIds = new List<long>();
          
            oper.IsSuccess = true;
            string sql = string.Format(@" select ts.*,isnull(tk.FID,'') as FINVENTORYID
                                        from ( {0} ) ts
                                        LEFT JOIN T_STK_INVENTORY tk on ts.FSTOCKORGID=tk.FSTOCKORGID and tk.FKEEPERTYPEID=ts.FKEEPERTYPEID and tk.FKEEPERID=ts.FKEEPERID
                                        and tk.FOWNERTYPEID=ts.FOWNERTYPEID and tk.FOWNERID=ts.FOWNERID and tk.FSTOCKID=ts.FSTOCKID and tk.FSTOCKLOCID=ts.FSTOCKPLACEID and tk.FAUXPROPID=ts.FAUXPROPERTYID 
                                        and tk.FSTOCKSTATUSID=ts.FSTOCKSTATUSID and tk.FLOT=ts.FLOT and tk.FBOMID=ts.FBOMID and tk.FMTONO=ts.FMTONO and tk.FPROJECTNO=ts.FPROJECTNO                                      
                                        and ISNULL(tk.FPRODUCEDATE, ISNULL(ts.FPRODUCTDATE,'')) = ISNULL(ts.FPRODUCTDATE,'') and ISNULL(tk.FEXPIRYDATE, ISNULL(ts.FVALIDATETO,'')) = ISNULL(ts.FVALIDATETO,'')  
                                        and tk.FBASEUNITID=ts.FBASEUNITID and tk.FSECUNITID=ts.FSECUNITID and tk.FMATERIALID=ts.FMATERIALID 
                                        inner join T_BD_MATERIALSTOCK MS on MS.FMATERIALID=ts.FMATERIALID and MS.FIsLockStock='1'
                                        inner join T_BD_STOCK TSK on TSK.FSTOCKID=ts.FSTOCKID AND TSK.FALLOWLOCK='1'
                                        where NOT EXISTS (SELECT 1 FROM T_PLN_RESERVELINKENTRY TKE 
                                        inner join T_PLN_RESERVELINK TKH on TKE.FID = TKH.FID WHERE TKE.FSUPPLYINTERID<>'' AND 
										TKH.FDEMANDFORMID='{1}' AND TKH.FDEMANDINTERID=ts.FID AND TKH.FDEMANDENTRYID=ts.FENTRYID )
                                ", string.Join(",", dataSql),formId);
            //配方单特殊处理
            if (formId == "PRD_PPBOM")
            {
                sql = dataSql;
            }
            DynamicObjectCollection dyLockInfo = DBUtils.ExecuteDynamicObject(ctx, sql);
            if (dyLockInfo == null || dyLockInfo.Count==0)
            {
                return;
            }
            List<LockStockArgs> saveList = new List<LockStockArgs>();


            foreach (DynamicObject dynEntry in dyLockInfo)
            {
                if (Convert.ToString(dynEntry["FINVENTORYID"]).IsNullOrEmptyOrWhiteSpace())
                {
                    oper.ValidationErrors.Add(new ValidationErrorInfo("", Convert.ToString(dynEntry["FENTRYID"]), 1, 1, Convert.ToString(dynEntry["FENTRYID"]), "单据" + dynEntry["FBillNo"].ToString() + " 第" + Convert.ToString(dynEntry["FSeq"]) + "行物料库存不足,锁库失败！", ""));
                    
                    continue;
                }
                
                 LockStockArgs item = new LockStockArgs
                {
                    //ObjectId = this.objectId,
                    //BillId-单据头ID
                    ObjectId = formId,
                    BillId = Convert.ToString(dynEntry["FID"]),
                    //BillDetailID-单据体ID
                    BillDetailID = Convert.ToString(dynEntry["FENTRYID"]),
                    //EntiryKey = this.entiryKey,
                    BillNo = dynEntry["FBILLNO"].ToString(),
                    BillSEQ = Convert.ToInt32(dynEntry["FSEQ"]),
                    BillTypeID = string.Empty,
                    DemandOrgId = 0,
                    DemandMaterialId = Convert.ToInt64(dynEntry["FMATERIALID"]),
                    StockOrgID = Convert.ToInt64(dynEntry["FSTOCKORGID"]),
                    MaterialID = Convert.ToInt64(dynEntry["FMATERIALID"]),
                    STOCKID = Convert.ToInt64(dynEntry["FSTOCKID"]),
                    LockQty = Convert.ToDecimal(dynEntry["FQTY"]),
                    Qty = Convert.ToDecimal(dynEntry["FQTY"]),
                    LockBaseQty = Convert.ToDecimal(dynEntry["FBASEQTY"]),
                    BaseQty = Convert.ToDecimal(dynEntry["FBASEQTY"]),
                    UnitID = Convert.ToInt64(dynEntry["FUNITID"]),
                    LockSecQty = Convert.ToDecimal(dynEntry["FSECQTY"]),
                    SecQty = Convert.ToDecimal(dynEntry["FSECQTY"]),
                    DemandPriority = "",
                    FInvDetailID = Convert.ToString(dynEntry["FINVENTORYID"]),
                    AuxPropId = Convert.ToInt64(dynEntry["FAUXPROPERTYID"]),
                    BOMID = Convert.ToInt64(dynEntry["FBOMID"]),
                    KeeperID = Convert.ToInt64(dynEntry["FKEEPERID"]),
                    KeeperTypeID = Convert.ToString(dynEntry["FKEEPERTYPEID"]),
                    Lot = Convert.ToInt64(dynEntry["FLOT"]),
                    MtoNo = Convert.ToString(dynEntry["FMTONO"]),
                    StockStatusID = Convert.ToInt64(dynEntry["FSTOCKSTATUSID"]),
                    StockLocID = Convert.ToInt64(dynEntry["FSTOCKPLACEID"]),
                    SecUnitID = Convert.ToInt64(dynEntry["FSECUNITID"]),
                    ProjectNo = Convert.ToString(dynEntry["FPROJECTNO"]),       
                    OwnerTypeID = Convert.ToString(dynEntry["FOWNERTYPEID"]),
                    OwnerID = Convert.ToInt64(dynEntry["FOWNERID"]),                
                    BaseUnitID = Convert.ToInt64(dynEntry["FBASEUNITID"]),
                    ProduceDate = Convert.ToDateTime(dynEntry["FPRODUCTDATE"]),
                    ExpiryDate = Convert.ToDateTime(dynEntry["FVALIDATETO"])
                };
                saveList.Add(item);
                
            }
          

            if (saveList.Count > 0)
            {
                DynamicObjectCollection objectResult= Kingdee.K3.SCM.Contracts.ServiceFactory.GetService<IStockLockService>(ctx).SaveLockInfo(ctx, saveList, formId, true);

                if (objectResult.Count > 0)
                {
                    IEnumerable<string> values = (from p in objectResult select string.Format(ResManager.LoadKDString("分录[{1}]不完全锁库(获取锁库不成功！)]原因:{2}", "004072030006502", SubSystemType.SCM, new object[0]), p["FBIZBILLNO"], p["FBILLSEQ"], p["FRESULT"])).Distinct<string>();
                    oper.IsSuccess = false;
                    oper.ValidationErrors.Add(new ValidationErrorInfo("", "", 1, 1, "1", string.Join("\n", values), ""));
                }
                if (!oper.IsSuccess && oper.ValidationErrors.Count>0)
                {
                    throw new Exception(oper.ValidationErrors[0].Message);
                }
            }


            
        }

    }
}