using Kingdee.K3.FIN.CB.App.Core.ExpenseAllocate.GetWeightInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.K3.FIN.CB.App.Core.ExpenseAllocate;
using Kingdee.K3.FIN.CB.Common.BusinessEntity.ExpenseAllocate;
using Kingdee.BOS;

namespace YDL.K3.App.Core.ExpenseAllocate.GetEnzymeInfo
{
    public class EnzymeQty : CompletionQtyWeight
    {
        public EnzymeQty(Context context, ExpenseAllocateParameters parameters) : base(context, parameters)
        {
        }

        public override WeightInfos GetWeightInfos(AllocateStdSetArgs ccStdSet)
        {
            return base.GetWeightInfos(ccStdSet);
        }
    }
}
