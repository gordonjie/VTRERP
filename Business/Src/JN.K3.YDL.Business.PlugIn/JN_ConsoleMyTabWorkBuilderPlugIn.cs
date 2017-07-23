using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Business.PlugIn.InfoComponent;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Util;

namespace JN.K3.YDL.Business.PlugIn
{
    public class JN_ConsoleMyTabWorkBuilderPlugIn : ConsoleMyTabWorkBuilderPlugIn
    {
        public override void CreateControl(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.CreateControlEventArgs e)
        {
            string key = e.ControlAppearance.Key;
            if (e.ControlAppearance.Key.EqualsIgnoreCase("FTab"))
            {
                JSONArray arr = e.Control["items"] as JSONArray;
                e.Control["style"] = "2";
                e.Control["canattachcommand"] = true;
                if (arr != null)
                {
                    SetTabStyle(arr, "FTabMyTask");
                    SetTabStyle(arr, "FTabMyWorkflow");
                    SetTabStyle(arr, "FTabMyWarnMsg");
                    SetTabStyle(arr, "FTabmgs");
                }
            }
            base.CreateControl(e);

        }

        private static void SetTabStyle(JSONArray arr, string tabName)
        {
            JSONObject obj2 = arr.OfType<JSONObject>().FirstOrDefault<JSONObject>(x => x["id"].ToString().EqualsIgnoreCase(tabName));
            if (obj2 != null)
            {
                obj2["stylekey"] = "KDMyWorkTabItem";
            }
        }
    }
}
