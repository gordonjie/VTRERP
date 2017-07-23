using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Core
{

    public class JNBandPara
    {
        /// <summary>
        /// 银行账号Id
        /// </summary>
        public Int32 bandid
        {
            get;
            set;
        }
        /// <summary>
        /// 银行联行号
        /// </summary>
        public string bandnum
        {
            get;
            set;
        }

        /// <summary>
        /// 账户
        /// </summary>
        public string cn
        {
            get;
            set;
        }

        /// <summary>
        /// 账户名称
        /// </summary>
        public string name
        {
            get;
            set;
        }

        /// <summary>
        /// 地址
        /// </summary>
        public string addr
        {
            get;
            set;
        }

        /// <summary>
        /// 开户行名称
        /// </summary>
        public string bandname
        {
            get;
            set;
        }
        
    }
}
