using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Core
{
    public class JNQTYRatePara
    {

        /// <summary>
        /// 物料id
        /// </summary>
       public     long MaterialId 
       {
           get;
           set;
       }


       /// <summary>
       /// 物料编码
       /// </summary>
       public string  MaterialNumber
       {
           get;
           set;
       }



       /// <summary>
       /// 组织Id
       /// </summary>
       public long OrgId
       {
           get;
           set;
       }

        /// <summary>
        /// 批号
        /// </summary>
        public   string   LotNumber
        {
            get;
            set;
        }






        /// <summary>
        /// 辅助属性
        /// </summary>
        public long AuxPropId
        {
            get;
            set;
        }





    }
}
