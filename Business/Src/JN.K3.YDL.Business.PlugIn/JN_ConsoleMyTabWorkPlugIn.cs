using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.Business.PlugIn.InfoComponent;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Workflow.Models.EnumStatus;
using Kingdee.BOS.Workflow.ServiceHelper;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Business.PlugIn.MainConsole;
using Kingdee.BOS.Workflow.Models.Assignment;


namespace JN.K3.YDL.Business.PlugIn
{
    public class JN_ConsoleMyTabWorkPlugIn : ConsoleMyTabWorkPlugIn
    {
        private Dictionary<string, JSONObject> displayMYWorkFlowDatas = new Dictionary<string, JSONObject>();
        private AbstractRelatedConsolePlugIn extrandPlugin;
        //private IndexAssign[] assigns;


        protected override void SetDynamicControlData()
        {
            base.SetDynamicControlData();
            this.SetMyWorkflowData();

        }

        public override void NaviOperAction(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.NaviOperActionArgs e)
        {
            base.NaviOperAction(e);
            if (e.ActionKey == "Click")
            {
                e.Data.GetValue<string>("style", "1");
                string btnKey = e.Data.GetValue<string>("key", "").ToUpper();
                string keyvalue = e.Data.GetValue<string>("keyvalue", "");
                //string objecttype = e.Data.GetValue<string>("objecttype", "");
                this.Linkpage(btnKey, e.Key);

            }
        }

        private void Linkpage(string btnKey, string command)
        {
            Action<FormResult> action = null;

            if (command.EqualsIgnoreCase("showweibo"))
            {

                if (this.displayMYWorkFlowDatas.ContainsKey(btnKey))
                {

                    string id = this.displayMYWorkFlowDatas[btnKey].ToString();
                    string linkobjecttype = this.displayMYWorkFlowDatas[btnKey]["objecttype"].ToString();
                    string keyvalue = this.displayMYWorkFlowDatas[btnKey]["keyvalue"].ToString();
                    if (linkobjecttype != "" && keyvalue != "")
                    {

                        BillShowParameter param = new BillShowParameter();
                        param.FormId = linkobjecttype;
                        param.Status = OperationStatus.EDIT;
                        param.PKey = keyvalue;
                        this.View.ShowForm(param);


                    }
                }

            }
            else if (command.EqualsIgnoreCase("Process"))
            {
                if (this.displayMYWorkFlowDatas.ContainsKey(btnKey))
                {

                    string id = this.displayMYWorkFlowDatas[btnKey].ToString();
                    string linkobjecttype = this.displayMYWorkFlowDatas[btnKey]["objecttype"].ToString();
                    string MESSAGEID = this.displayMYWorkFlowDatas[btnKey]["MESSAGEID"].ToString();
                    if (linkobjecttype != "" && MESSAGEID != "")
                    {
                        FormResult formresult = null;
                        BillShowParameter param = new BillShowParameter();
                        param.FormId = "WF_PASSROUNDMSG";
                        param.Status = OperationStatus.EDIT;
                        param.PKey = MESSAGEID;

                        this.View.ShowForm(param, new Action<FormResult>((result) =>
                        {
                            formresult = result;
                            this.SetMyWorkflowData();
                        }));

                    }
                }
            }

        }






        private void SetMyWorkflowData()
        {
            //this.assigns = AssignmentServiceHelper.GetIndexAssign(base.Context);

            OQLFilter ofilter = new OQLFilter();
            string str = string.Format("FRECEIVERID={0} and FType='0' and FStatus<>'1' and FStatus<>'3' and FStatus<>'4'", base.Context.UserId.ToString());
            OQLFilterHeadEntityItem item = new OQLFilterHeadEntityItem
            {
                EntityKey = "FBillHead",
                FilterString = str
            };
            ofilter.Add(item);
            DynamicObject[] colls = BusinessDataServiceHelper.Load(base.Context, "WF_PASSROUNDMSG", null, ofilter);
            if (this.IsMyInstChanged(colls))
            {
                int length = colls.Length;
                int index = 1;
                JSONObject naviBar = this.GetNaviBar("tasknavibar", "ConsoleTaskStyle");
                int top = 15;
                JSONArray items = new JSONArray();
                naviBar["data"] = items;
                naviBar["pagebuttonvisible"] = true;
                foreach (DynamicObject obj3 in from t in colls
                                               orderby t["CreateTime"] descending
                                               select t)
                {
                    if (index > this.MaxCount)
                    {
                        break;
                    }
                    string processName = obj3["Title"].ToString();
                    string MESSAGEID = obj3["Id"].ToString();

                    DynamicObject sendpersons = obj3["SENDERID"] as DynamicObject;

                    string processPersons = sendpersons["Name"].ToString();
                    string status = obj3["FStatus"].ToString();
                    string objecttype = obj3["ObjectTypeId_Id"].ToString();
                    string keyvalue = Convert.ToString(obj3["keyvalue"]);
                    //string procInstId = obj3["id"].ToString();
                    this.SetMyWorkflowData(items, processName, processPersons, top, index, status, objecttype, keyvalue, MESSAGEID);
                    top += 30;
                    index++;
                }
                this.UpdateTabItemAttathCount(5, length);
                this.SetDynamicPanelData("F_JNWorkflowmgs", naviBar, items.Count == 0, ResManager.LoadKDString("暂无流程信息", "002014030023703", SubSystemType.BOS, new object[0]));
            }

        }

        private void SetDynamicPanelData(string dynamicPanelKey, JSONObject naviBar, bool isDataEmpty, string emptyText)
        {
            JSONObject obj2 = new JSONObject();
            JSONArray array = new JSONArray();
            obj2.Add("items", array);
            Panel control = this.View.GetControl<Panel>(dynamicPanelKey);
            if (isDataEmpty)
            {
                JSONObject item = new JSONObject();
                item["xtype"] = "label";
                item["id"] = "lb" + dynamicPanelKey;
                item["key"] = item["id"];
                JSONObject obj4 = new JSONObject();
                obj4.Put("fontSize", "30");
                obj4.Put("fontStyle", "1");
                obj4.Put("fontName", ResManager.LoadKDString("宋体", "002014030005206", SubSystemType.BOS, new object[0]));
                string str = KDObjectConverter.SerializeObject(obj4);
                item["font"] = str;
                item["forecolor"] = "#ededed";
                item["x"] = "200";
                item["y"] = "70";
                item["text"] = emptyText;
                item["width"] = 350;
                item["height"] = 50;
                array.Add(item);
            }
            else
            {
                array.Add(naviBar);
            }
         
            control.InvokeControlMethod("ReplaceChildrenData", new object[] { obj2 });
            this.View.UpdateView(dynamicPanelKey);

        }

        private void UpdateTabItemAttathCount(int index, int icount)
        {
            TabControl control = this.View.GetControl<TabControl>("FTab");
            if (icount == 0)
            {
                control.SetItemAttachValue(index, "");
            }
            else
            {
                control.SetItemAttachValue(index, string.Format("({0})", icount));
            }
            this.View.UpdateView("FTab");

        }

        private void SetMyWorkflowData(JSONArray items, string processName, string processPersons, int top, int index, string status, string objecttype, string keyvalue, string MESSAGEID)
        {
            string str = "richBtnWorkflow" + index.ToString();
            JSONObject item = new JSONObject();
            item["xtype"] = "kdbutton";
            //item["text"] = string.Format(ResManager.LoadKDString("{0}", "002014030023179", SubSystemType.BOS, new object[0]), processName);
            item["id"] = str;
            item["key"] = str;
            item["y"] = top;
            item["x"] = 10;
            item["width"] = 630;
            item["tooltip"] = "";
            item["text"] = item["tooltip"];
            item["keyvalue"] = keyvalue;
            item["MESSAGEID"] = MESSAGEID;
            item["objecttype"]=objecttype;
            JSONObject obj3 = new JSONObject();
            obj3.Add("canexecutecommand", true);
            obj3.Add("keyvalue", keyvalue);
            obj3.Add("procInstId", MESSAGEID);
            obj3.Add("objecttype", objecttype);
            obj3.Add("otherdata", string.Format(ResManager.LoadKDString("{0}（发送人：{1}）。", "002014030023179", SubSystemType.BOS, new object[0]), processName, processPersons));
            obj3.Add("stylekey", objecttype);
            item["buttonitemmodeldata"] = obj3;
            if (this.extrandPlugin != null)
            {
                this.extrandPlugin.SetConsoleMyTabWorkData("ConsoleTask", item);
            }

            items.Add(item);
            this.displayMYWorkFlowDatas[str.ToUpper()] = item;

        }

        private JSONObject GetNaviBar(string key, string style)
        {
            JSONObject obj2 = new JSONObject();
            obj2["xtype"] = "kdcustomnavibar";
            obj2["dock"] = "5";
            obj2["style"] = style;
            obj2["pagebuttonvisible"] = false;
            obj2["id"] = key;
            obj2["key"] = key;
            return obj2;

        }

        private bool IsMyInstChanged(DynamicObject[] colls)
        {
            object obj2;
            bool flag = false;
            string key = "_wfmyinstdata_";
            this.View.Session.TryGetValue(key, out obj2);
            if (obj2 != null)
            {
                DynamicObject[] objArray = obj2 as DynamicObject[];
                if ((objArray != null) && (objArray.Length == colls.Length))
                {
                    for (int i = 0; i < colls.Length; i++)
                    {
                        if (!this.IsInstObjectEqual(colls[i], objArray[i]))
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                else
                {
                    flag = true;
                }
            }
            else
            {
                flag = true;
            }
            if (flag)
            {
                this.View.Session[key] = colls;
            }
            return flag;

        }

        private bool IsInstObjectEqual(DynamicObject obj1, DynamicObject obj2)
        {
            if (!obj1["Id"].Equals(obj2["Id"]) || !obj1["FStatus"].Equals(obj2["FStatus"]))
            {
                return false;
            }
            DynamicObjectCollection objects = obj1["FEntity"] as DynamicObjectCollection;
            DynamicObjectCollection objects2 = obj2["FEntity"] as DynamicObjectCollection;
            if (objects.Count != objects2.Count)
            {
                return false;
            }
            if (objects.Count != 0)
            {
                DynamicObject obj3 = (from x in objects
                                      orderby x["FAssignCreateTime"] descending
                                      select x).First<DynamicObject>();
                DynamicObject obj4 = (from x in objects2
                                      orderby x["FAssignCreateTime"] descending
                                      select x).First<DynamicObject>();
                string str = (obj3["FACTIVITYNAME"] as LocaleValue)[base.Context.LogLocale.LCID];
                string str2 = (obj4["FACTIVITYNAME"] as LocaleValue)[base.Context.LogLocale.LCID];
                if (!str.EqualsIgnoreCase(str2))
                {
                    return false;
                }
                string str3 = "";
                string str4 = "";
                if (obj3["FReceiverNames"] != null)
                {
                    str3 = obj3["FReceiverNames"].ToString();
                }
                if (obj4["FReceiverNames"] != null)
                {
                    str4 = obj4["FReceiverNames"].ToString();
                }
                if (!str3.Equals(str4))
                {
                    return false;
                }
            }
            return true;

        }


    }
}
