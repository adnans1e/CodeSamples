﻿using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Configuration;
using System.Runtime.Serialization.Json;
using System.Net.Http;
using System.Web.Http;
using System.Net;
using System.ServiceModel.Web;

using SampleApplicationDotNet.DataTypes;

namespace SampleApplicationDotNet
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Example of calling Security.svc/getsites to display a list of sites for the 
        /// specified enterprise GUID.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void LoginUser_Authenticate(object sender, EventArgs e)
        {
            Credentials.Visible = false;
            SiteSelector.Visible = true;

            Session.Add("Password", PasswordBox.Text);
            Session.Add("Username", UserNameBox.Text);
            Session.Add("EnterpriseGUID", EnterpriseBox.Text);

            string baseurl = WebConfigurationManager.AppSettings["ETOSoftwareWS_BaseUrl"];

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseurl + "Security.svc/getsites/" + EnterpriseBox.Text);
            request.Method = "GET";

            try
            {
                WebResponse webResponse = request.GetResponse();
                DataContractJsonSerializer siteSer = new DataContractJsonSerializer(typeof(Entry[]));
                Entry[] sites = (Entry[])siteSer.ReadObject(webResponse.GetResponseStream());

                foreach (Entry site in sites)
                {
                    Label label = new Label();
                    label.Text = "<a href=\"Workbench.aspx?site=" + site.Key.ToString() + "\">" + site.Value + "</a></br>";
                    Panel1.Controls.Add(label);
                }
            }
            catch (WebException we)
            {
                string webExceptionMessage = we.Message;
            }
            catch (Exception ex)
            {
                Response.Redirect("Default.aspx");
            } 

            //using (HttpClient client = new HttpClient())
            //{
            //    HttpResponseMessage resp = client.GetAsync(baseurl + "Security.svc/getsites/" + EnterpriseBox.Text);
            //    resp.EnsureSuccessStatusCode();

            //    DataContractJsonSerializer siteSer = new DataContractJsonSerializer(typeof(Entry[]));
            //    Entry[] sites = (Entry[])siteSer.ReadObject(resp.Content.ReadAsStreamAsync());

            //    foreach (Entry site in sites)
            //    {
            //        Label label = new Label();
            //        label.Text = "<a href=\"Workbench.aspx?site=" + site.Key.ToString() + "\">" + site.Value + "</a></br>";
            //        Panel1.Controls.Add(label);
            //    }
            //}
        }
    }
}