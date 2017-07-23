using JN.K3.YDL.Contracts.SCM;
using Kingdee.BOS;
using Kingdee.BOS.Rpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JN.K3.YDL.Contracts
{
    /// <summary>
    /// 
    /// </summary>
    public class ServiceFactory
    {
        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<Type, string> _mapServer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        public static void CloseService(object service)
        {
            IDisposable disposable = service as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static T GetService<T>(Context ctx)
        {
            if (_mapServer == null)
            {
                RegisterService();
            }
            string sClassName = _mapServer[typeof(T)];
            RemoteCommunication communication = new RemoteCommunication(ctx.ServerUrl);
            return communication.CreateObject<T>(sClassName);
        }


     
         
        /// <summary>
        /// 
        /// </summary>
        public static void RegisterService()
        {
            _mapServer = new Dictionary<Type, string>();
            lock (_mapServer)
            {
                if (_mapServer.Count < 1)
                {
                    //_mapServer.Add(typeof(ICommonService), "JN.K3.YDL.App.Core.CommonService,JN.K3.YDL.App.Core");
                    //_mapServer.Add(typeof(ICashierService), "JN.K3.YDL.App.Core.CashierService,JN.K3.YDL.App.Core");
                    _mapServer.Add(typeof(IYDLCommService), "JN.K3.YDL.App.Core.YDLCommService,JN.K3.YDL.App.Core"); 
                    _mapServer.Add(typeof(IJNSaleQuoteService), "JN.K3.YDL.App.Core.SCM.SaleQuoteService,JN.K3.YDL.App.Core");
                }
            }
        }
    }
}
