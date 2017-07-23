using JN.K3.YDL.Core;
using JN.K3.YDL.ServiceHelper;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

namespace JN.K3.YDL.App.ServicePlugIn.Bandservice
{
    public  class JNBand : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 签入
        /// </summary>
        /// <param name="Context">上下文</param>
        /// <param name="actband">我方银行账户</param>
        public string checkin(Context ctx, JNBandPara actband)
        {
            DynamicObject bandID = getbandobject(ctx,actband);
            DynamicObject urlobject = getbandUrl(bandID);

            #region
            //判断是否是启用的帐套
            string database = ctx.DataCenterName;
            string F_VTR_DataBase = Convert.ToString(urlobject["F_VTR_DataBase"]);
            if (database!=F_VTR_DataBase)
            {
                return "";
            }
            #endregion


            string url = Convert.ToString(urlobject["F_JN_ServiceURL"]);
            string F_JNtermid = Convert.ToString(urlobject["F_JNtermid"]);
            string F_JNCustid = Convert.ToString(urlobject["F_JNCustid"]);
            string F_JNCusopr = Convert.ToString(urlobject["F_JNCusopr"]);
            string F_JNOprpwd = Convert.ToString(urlobject["F_JNOprpwd"]);
            DateTime currentTime = new System.DateTime();
            currentTime =TimeServiceHelper.GetSystemDateTime(ctx);
            string F_JNcustdt = string.Format("{0:yyyyMMddHHmmss}", currentTime);
            string xmlMsg = string.Format(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<bocb2e version=""100"" security=""true"" lang=""chs"">
<head>
<termid>{0}</termid>
<trnid>20060704001</trnid>
<custid>{1}</custid>
<cusopr>{2}</cusopr>
<trncod>b2e0001</trncod>
</head>
<trans>
<trn-b2e0001-rq>
<b2e0001-rq>
<custdt>{3}</custdt>
<oprpwd>{4}</oprpwd>
</b2e0001-rq>
</trn-b2e0001-rq>
</trans>
</bocb2e>
", F_JNtermid, F_JNCustid, F_JNCusopr, F_JNcustdt, F_JNOprpwd);
        string Xmldata = BandPost(url, xmlMsg, 600);
        XmlDocument xx = new XmlDocument();       
        xx.LoadXml(Xmldata);//加载xml
            XmlNode rootnote = xx.SelectSingleNode("bocb2e");//指向根节点
            XmlNode transnote = rootnote.SelectSingleNode("trans");
            XmlNode b2enote = transnote.SelectSingleNode("trn-b2e0001-rs");
            XmlNode tokennote = b2enote.SelectSingleNode("token");
            string token = tokennote.InnerText;
            return token;
        }

        /// <summary>
        /// 签出
        /// </summary>
        /// <param name="Context">上下文</param>
        /// <param name="actband">我方银行账户</param>
        public void checkout(Context ctx, JNBandPara actband)
        {
            DynamicObject bandID = getbandobject(ctx,actband);
            DynamicObject urlobject = getbandUrl(bandID);

            #region
            //判断是否是启用的帐套
            string database = ctx.DataCenterName;
            string F_VTR_DataBase = Convert.ToString(urlobject["F_VTR_DataBase"]);
            if (database != F_VTR_DataBase)
            {
                return ;
            }
            #endregion

            string url = Convert.ToString(urlobject["F_JN_ServiceURL"]);
            string F_JNtermid = Convert.ToString(urlobject["F_JNtermid"]);
            string F_JNCustid = Convert.ToString(urlobject["F_JNCustid"]);
            string F_JNCusopr = Convert.ToString(urlobject["F_JNCusopr"]);
            
            DateTime currentTime = new System.DateTime();
            currentTime = TimeServiceHelper.GetSystemDateTime(ctx);
            string F_JNcustdt = string.Format("{0:yyyyMMddHHmmss}", currentTime);
            string xmlMsg = string.Format(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<bocb2e version=""100"" security=""true"" lang=""chs"">
<head>
<termid>{0}</termid>
<trnid>20060704001</trnid>
<custid>{1}</custid>
<cusopr>{2}</cusopr>
<trncod>b2e0002</trncod>
</head>
<trans>
<trn-b2e0002-rq>
<b2e0002-rq>
<custdt>{3}</custdt>
</b2e0002-rq>
</trn-b2e0002-rq>
</trans>
</bocb2e>
", F_JNtermid, F_JNCustid, F_JNCusopr, F_JNcustdt);
            string Xmldata = BandPost(url, xmlMsg, 600);
            

        }
        /// <summary>
        /// 查询余额
        /// </summary>
        /// <param name="Context">上下文</param>
        /// <param name="actband">我方银行账户</param>
        public JNBandBalance checkamount(Context ctx, JNBandPara actband)
        {
            DynamicObject bandID = getbandobject(ctx,actband);
            DynamicObject urlobject = getbandUrl(bandID);


            string bandNUMBER = Convert.ToString(bandID["NUMBER"]);
            string ACNTBRANCHNUMBER= Convert.ToString(bandID["ACNTBRANCHNUMBER"]);
            string token = checkin(ctx,actband);
            string url = Convert.ToString(urlobject["F_JN_ServiceURL"]);
            string F_JNtermid = Convert.ToString(urlobject["F_JNtermid"]);
            string F_JNCustid = Convert.ToString(urlobject["F_JNCustid"]);
            string F_JNCusopr = Convert.ToString(urlobject["F_JNCusopr"]);

            DateTime currentTime = new System.DateTime();
            currentTime = TimeServiceHelper.GetSystemDateTime(ctx);
            string F_JNcustdt = string.Format("{0:yyyyMMddHHmmss}", currentTime);
            string xmlMsg = string.Format(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<bocb2e version=""100"" security=""true"" lang=""chs"">
<head>
<termid>{0}</termid>
<trnid>20060704001</trnid>
<custid>{1}</custid>
<cusopr>{2}</cusopr>
<trncod>b2e0005</trncod>
<token>{3}</token>
</head>
<trans>
<trn-b2e0005-rq>
<b2e0005-rq>
<account>
<ibknum>{4}</ibknum>
<actacn>{5}</actacn>
</account>
</b2e0005-rq>
</trn-b2e0005-rq>
</trans>
</bocb2e>
", F_JNtermid, F_JNCustid, F_JNCusopr, token, bandNUMBER, ACNTBRANCHNUMBER);
            string Xmldata = BandPost(url, xmlMsg, 600);
            XmlDocument xx = new XmlDocument();
            xx.LoadXml(Xmldata);//加载xml
            XmlNodeList xxList = xx.GetElementsByTagName("balance"); //取得节点名为row的XmlNode集合
            double bokbal = Convert.ToDouble( xxList[0]["bokbal"].Value);//账面余额 
            double avabal = Convert.ToDouble(xxList[0]["avabal"].Value); ;//有效余额 
            double stpamt = Convert.ToDouble(xxList[0]["stpamt"].Value); ;//圈存余额 
            double ovramt = Convert.ToDouble(xxList[0]["ovramt"].Value); ;//透资余额 


            JNBandBalance amount = new JNBandBalance();
            amount.bokbal = bokbal;
            amount.bokbal = avabal;
            amount.stpamt = stpamt;
            amount.ovramt = ovramt;
            return amount;
        }

        /// <summary>
        /// 公对公转账支付
        /// </summary>
        /// <param name="Context">上下文</param>
        /// <param name="actband">我方银行账户</param>
        /// <param name="actband">对方银行账户</param>
        /// <param name="actband">金额</param>
        /// <param name="furinfo">用途</param>
        /// <param name="token">令牌</param>
        public string BtoBPay(Context ctx, JNBandPara actband, JNBandPara toband, double payamount, string furinfo, string token)
        {
            DynamicObject bandID = getbandobject(ctx,actband);
            DynamicObject urlobject = getbandUrl(bandID);

            #region
            //判断是否是启用的帐套
            string database = ctx.DataCenterName;
            string F_VTR_DataBase = Convert.ToString(urlobject["F_VTR_DataBase"]);
            if (database != F_VTR_DataBase)
            {
                return "";
            }
            #endregion

            string bandNUMBER = Convert.ToString(bandID["NUMBER"]);
            string ACNTBRANCHNUMBER = Convert.ToString(bandID["ACNTBRANCHNUMBER"]);
            //string token = checkin(ctx,actband);
            string url = Convert.ToString(urlobject["F_JN_ServiceURL"]);
            string F_JNtermid = Convert.ToString(urlobject["F_JNtermid"]);
            string F_JNCustid = Convert.ToString(urlobject["F_JNCustid"]);
            string F_JNCusopr = Convert.ToString(urlobject["F_JNCusopr"]);

            DateTime currentTime = new System.DateTime();
            currentTime = TimeServiceHelper.GetSystemDateTime(ctx); 
            string insid = string.Format("{0:yyyyMMddHHmmssff}", currentTime);//指令ID

            //我方银行账号
            string frbandnum = actband.bandnum;
            string frcn = actband.cn;
            string frname = actband.name;
            //对方银行账号
            string tobandnum = toband.bandnum;
            string tocn = toband.cn;
            string toname = toband.name;
            string toadd = toband.addr;
            string tobandname = toband.bandname;

            string payamountstr = Convert.ToString(payamount);




            string F_JNcustdt = string.Format("{0:yyyyMMdd}", currentTime);
            //F_JNcustdt = "20170330";
            string xmlMsg = string.Format(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<bocb2e version=""100"" security=""true"" lang=""chs"">
<head>
<termid>{0}</termid>
<trnid>20060704001</trnid>
<custid>{1}</custid>
<cusopr>{2}</cusopr>
<trncod>b2e0009</trncod>
<token>{3}</token>
</head>
<trans>
<trn-b2e0009-rq>
<transtype>1</transtype>
<b2e0009-rq>
<insid>{4}</insid>
<obssid></obssid>
<fractn>
<fribkn>{5}</fribkn>
<actacn>{6}</actacn>
<actnam>{7}</actnam> 
</fractn>
<toactn>
<toibkn>{8}</toibkn>
<actacn>{9}</actacn>
<toname>{10}</toname> 
<toaddr>{11}</toaddr>
<tobknm>{12}</tobknm>
</toactn>
<trnamt>{13}</trnamt>
<trncur>001</trncur> 
<priolv>0</priolv>
<furinfo>{14}</furinfo>
<trfdate>{15}</trfdate> 
<trftime></trftime>
<comacn></comacn> 
</b2e0009-rq>
</trn-b2e0009-rq>
</trans>
</bocb2e>
", F_JNtermid, F_JNCustid, F_JNCusopr, token, insid, frbandnum, frcn, frname, tobandnum,tocn,toname,toadd,tobandname,payamountstr, furinfo, F_JNcustdt);
            string Xmldata = BandPost(url, xmlMsg, 600);
            XmlDocument xx = new XmlDocument();
            xx.LoadXml(Xmldata);//加载xml
            XmlNode rootnote = xx.SelectSingleNode("bocb2e");//指向根节点
            XmlNode transnote = rootnote.SelectSingleNode("trans");
            XmlNode b2enote = transnote.SelectSingleNode("trn-b2e0009-rs");
            if (b2enote == null) { return ""; }

            XmlNode statusnote = b2enote.SelectSingleNode("b2e0009-rs");
            XmlNode obssidnote = statusnote.SelectSingleNode("obssid");
            //XmlNode rspnote = statusnote.SelectSingleNode("rspmsg");
            string rspmsg = obssidnote.InnerText;
            //checkout(ctx,actband);
            return rspmsg;//返回银行流水号
            

        }


        /// <summary>
        /// 公对私转账支付
        /// </summary>
        ///  /// <param name="Context">上下文</param>
        /// <param name="actband">我方银行账户</param>
        /// <param name="actband">对方银行账户</param>
        /// <param name="actband">金额</param>
        /// <param name="furinfo">用途</param>
        /// <param name="F_VTR_Bocflag">是否跨行</param>
        ///  <param name="token">令牌</param>
        public string BtoCPay(Context ctx, JNBandPara actband, JNBandPara toband, double payamount, string furinfo, int F_VTR_Bocflag, string token)
        {
            DynamicObject bandID = getbandobject(ctx,actband);
            DynamicObject urlobject = getbandUrl(bandID);

            #region
            //判断是否是启用的帐套
            string database = ctx.DataCenterName;
            string F_VTR_DataBase = Convert.ToString(urlobject["F_VTR_DataBase"]);
            if (database != F_VTR_DataBase)
            {
                return "";
            }
            #endregion

            string bandNUMBER = Convert.ToString(bandID["NUMBER"]);
            string ACNTBRANCHNUMBER = Convert.ToString(bandID["ACNTBRANCHNUMBER"]);
            //string token = checkin(ctx,actband);
            string url = Convert.ToString(urlobject["F_JN_ServiceURL"]);
            string F_JNtermid = Convert.ToString(urlobject["F_JNtermid"]);
            string F_JNCustid = Convert.ToString(urlobject["F_JNCustid"]);
            string F_JNCusopr = Convert.ToString(urlobject["F_JNCusopr"]);

            DateTime currentTime = new System.DateTime();
            currentTime = TimeServiceHelper.GetSystemDateTime(ctx);
            string insid = string.Format("{0:yyyyMMddHHmmssff}", currentTime);//指令ID

            //我方银行账号
            string frbandnum = actband.bandnum;
            string frcn = actband.cn;
            string frname = actband.name;
            //对方银行账号
            string tobandnum = toband.bandnum;
            string tocn = toband.cn;
            string toname = toband.name;
            string toadd = toband.addr;
            string tobandname = toband.bandname;

            string payamountstr = Convert.ToString(payamount);




            string F_JNcustdt = string.Format("{0:yyyyMMdd}", currentTime);
            //F_JNcustdt = "20170330";
            string xmlMsg = string.Format(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<bocb2e version=""100"" security=""true"" lang=""chs"">
<head>
<termid>{0}</termid>
<trnid>20060704001</trnid>
<custid>{1}</custid>
<cusopr>{2}</cusopr>
<trncod>b2e0061</trncod>
<token>{3}</token>
</head>
<trans>
<trn-b2e0061-rq>
<transtype>1</transtype>
<b2e0061-rq>
<insid>{4}</insid>
<obssid></obssid>
<fractn>
<fribkn>{5}</fribkn>
<actacn>{6}</actacn>
<actnam>{7}</actnam> 
</fractn>
<toactn>
<toibkn>{8}</toibkn>
<actacn>{9}</actacn>
<toname>{10}</toname> 
<toaddr>{11}</toaddr>
<tobknm>{12}</tobknm>
</toactn>
<trnamt>{13}</trnamt>
<trncur>001</trncur> 
<priolv>0</priolv>
<cuspriolv>0</cuspriolv>
<furinfo>{14}</furinfo>
<trfdate>{15}</trfdate> 
<trftime></trftime>
<comacn></comacn> 
<bocflag>{16}</bocflag>
</b2e0061-rq>
</trn-b2e0061-rq>
</trans>
</bocb2e>
", F_JNtermid, F_JNCustid, F_JNCusopr, token, insid, frbandnum, frcn, frname, tobandnum, tocn, toname, toadd, tobandname, payamountstr, furinfo, F_JNcustdt, F_VTR_Bocflag);
            string Xmldata = BandPost(url, xmlMsg, 600);
            XmlDocument xx = new XmlDocument();
            xx.LoadXml(Xmldata);//加载xml
            XmlNode rootnote = xx.SelectSingleNode("bocb2e");//指向根节点
            XmlNode transnote = rootnote.SelectSingleNode("trans");
            XmlNode b2enote = transnote.SelectSingleNode("trn-b2e0061-rs");
            if (b2enote == null) { return ""; }

            XmlNode statusnote = b2enote.SelectSingleNode("b2e0061-rs");
            XmlNode obssidnote = statusnote.SelectSingleNode("obssid");
            //XmlNode rspnote = statusnote.SelectSingleNode("rspmsg");
            string rspmsg = obssidnote.InnerText;
            //checkout(ctx,actband);
            return rspmsg;//返回银行流水号


        }

       
        /// <summary>
        /// 查询交易状态
        /// </summary>
        /// <param name="Context">上下文</param>
        /// <param name="actband">我方银行账户</param>
        /// <param name="obssid">银行流水号</param>
        /// <param name="token">令牌</param>
        public string findPay(Context ctx, JNBandPara actband, string obssid, string token)
        {
            DynamicObject bandID = getbandobject(ctx, actband);
            DynamicObject urlobject = getbandUrl(bandID);

            #region
            //判断是否是启用的帐套
            string database = ctx.DataCenterName;
            string F_VTR_DataBase = Convert.ToString(urlobject["F_VTR_DataBase"]);
            if (database != F_VTR_DataBase)
            {
                return "";
            }
            #endregion


            string bandNUMBER = Convert.ToString(bandID["NUMBER"]);
            string ACNTBRANCHNUMBER = Convert.ToString(bandID["ACNTBRANCHNUMBER"]);
            //string token = checkin(ctx,actband);
            string url = Convert.ToString(urlobject["F_JN_ServiceURL"]);
            string F_JNtermid = Convert.ToString(urlobject["F_JNtermid"]);
            string F_JNCustid = Convert.ToString(urlobject["F_JNCustid"]);
            string F_JNCusopr = Convert.ToString(urlobject["F_JNCusopr"]);

            DateTime currentTime = new System.DateTime();
            currentTime = TimeServiceHelper.GetSystemDateTime(ctx);
            string F_JNcustdt = string.Format("{0:yyyyMMddHHmmss}", currentTime);
            string xmlMsg = string.Format(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<bocb2e version=""100"" security=""true"" lang=""chs"">
<head>
<termid>{0}</termid>
<trnid>20060704001</trnid>
<custid>{1}</custid>
<cusopr>{2}</cusopr>
<trncod>b2e0007</trncod>
<token>{3}</token>
</head>
<trans>
<trn-b2e0007-rq>
<b2e0007-rq>
<insid></insid> 
<obssid>{4}</obssid>
</b2e0007-rq>
</trn-b2e0007-rq>
</trans>
</bocb2e>
", F_JNtermid, F_JNCustid, F_JNCusopr, token, obssid);
            string Xmldata = BandPost(url, xmlMsg, 600);
            XmlDocument xx = new XmlDocument();
            xx.LoadXml(Xmldata);//加载xml
            XmlNode rootnote = xx.SelectSingleNode("bocb2e");//指向根节点
            XmlNode transnote = rootnote.SelectSingleNode("trans");
            XmlNode trnb2enote = transnote.SelectSingleNode("trn-b2e0007-rs");

            XmlNode b2enotes = trnb2enote.SelectSingleNode("b2e0007-rs");
            XmlNode statusnote = b2enotes.SelectSingleNode("status");
            XmlNode rspnote = statusnote.SelectSingleNode("rspmsg");
            string rspmsg = rspnote.InnerText;
            //checkout(ctx,actband);
            return rspmsg;//返回银行流水号
        }

        private DynamicObject getbandUrl(DynamicObject bandID)
        {
            DynamicObject bandUrlobject = bandID["F_JNAutoBand"] as DynamicObject;

            return bandUrlobject;
        }

        private DynamicObject getbandobject(Context ctx, JNBandPara actband)
        {
            int actbandid = actband.bandid;
            DynamicObject[] bandIDs = BusinessDataServiceHelper.Load(ctx, new object[] { actbandid }, (MetaDataServiceHelper.Load(ctx, "CN_BANKACNT") as FormMetadata).BusinessInfo.GetDynamicObjectType());
            return bandIDs.FirstOrDefault();
        }



        /// <summary>
        /// 银行系统对接交互返回报文
        /// </summary>

        /// <param name="url">银行服务地址</param>
        /// <param name="xmlMsg">XML报文请求</param>
        /// <param name="timeout">请求时间</param>
        /// <returns>XML返回报文</returns>
        public string BandPost(string url, string xmlMsg, int timeout)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            Stream reqStream = null;

            String strRet = "";

            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "POST";
                request.Timeout = timeout * 1000;


                //设置POST的数据类型和长度
                request.ContentType = "text/xml";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xmlMsg);
                request.ContentLength = data.Length;


                //往服务器写入数据
                reqStream = request.GetRequestStream();
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();

                //获取服务端返回
                response = (HttpWebResponse)request.GetResponse();

                //获取服务端返回数据
                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                strRet = sr.ReadToEnd().Trim();
                sr.Close();
            }
            catch (Exception e)
            {
                return strRet;
            }
            finally
            {
                //关闭连接和流
                if (response != null)
                {
                    response.Close();
                }
                if (request != null)
                {
                    request.Abort();
                }
            }
            return strRet;
        }

    }
}
