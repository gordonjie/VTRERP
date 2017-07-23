using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JN.K3.YDL.Core
{
    /// <summary>
    /// 后台调用单据转换参数
    /// </summary>
    public class BillConvertOption
    {
        public BillConvertOption()
        {
            IsSave = true;
        }

        /// <summary>
        /// 源单标识
        /// </summary>
        public string sourceFormId { get; set; }

        /// <summary>
        /// 目标单标识
        /// </summary>
        public string targetFormId { get; set; }

        /// <summary>
        /// 单据转换规则KEY
        /// </summary>
        public string ConvertRuleKey { get; set; }

        /// <summary>
        /// 选中的源单行
        /// </summary>
        public ListSelectedRow[] BizSelectRows { get; set; }

        public bool IsDraft { get; set; }
        /// <summary>
        /// 保存
        /// </summary>
        public bool IsSave { get; set; }
        /// <summary>
        /// 审核
        /// </summary>
        public bool IsAudit { get; set; }
        /// <summary>
        /// 提交
        /// </summary>
        public bool IsSubmit { get; set; } 
        /// <summary>
        /// 转换参数
        /// </summary>
        public OperateOption Option { get; set; }

        public object customParams { get; set; }





        /// <summary>
        /// 单据审核后的相关信息
        /// </summary>
        public OperateOption BillStatusOptionResult { get; set; }

       
    }


}
