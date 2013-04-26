using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Configuration;
using System.Web.Script.Serialization;
using System.Net.Http;
using System.ServiceModel.Web;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;

using SampleApplicationDotNet.DataTypes;

namespace SampleApplicationDotNet
{
    public partial class Workbench : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Scriptmanager1.RegisterAsyncPostBackControl(SearchText);
            Scriptmanager1.RegisterAsyncPostBackControl(SearchResults);

            if (!IsPostBack)
            {
                try
                {
                    string siteid = Request["site"].ToString();
                    Session["SessionID"] = siteid;
                    Logon(siteid);
                    LogonSite();
                    DisplaySiteInfo();
                    DisplayPrograms();
                }
                catch (Exception ex)
                {
                    Response.Redirect("Default.aspx");
                }            
            }
            else
            {
                string token = Session["SecurityToken"].ToString();
            }
        }       

        /// <summary>
        /// Example of calling the Security.svc/login method to log into ETO.
        /// </summary>
        /// <param name="siteId"></param>
        protected void Logon(string siteId)
        {
            string userName = Session["Username"].ToString();
            string password = Session["Password"].ToString();
            string enterprise = Session["EnterpriseGUID"].ToString();
            string baseurl = WebConfigurationManager.AppSettings["ETOSoftwareWS_BaseUrl"];

            string json = string.Format("{{\"security\":{{\"Email\":\"{0}\",\"Password\":\"{1}\"}}}}", userName, password);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseurl + "Security.svc/SSOAuthenticate/");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = json.Length;
            StreamWriter requestWriter = new StreamWriter(request.GetRequestStream(), System.Text.Encoding.ASCII);
            requestWriter.Write(json);
            requestWriter.Close();

            try
            {
                // get the response
                WebResponse webResponse = request.GetResponse();                

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SSOAuthenticateResponseObject));
                SSOAuthenticateResponseObject SSOAuthenticationResponse = serializer.ReadObject(webResponse.GetResponseStream()) as SSOAuthenticateResponseObject;
                Session["AuthToken"] = SSOAuthenticationResponse.SSOAuthenticateResult.SSOAuthToken;                
            }
            catch (WebException we)
            {
                string webExceptionMessage = we.Message;
            }
            catch (Exception ex)
            {
                Response.Redirect("Default.aspx");
            }
        }

        /// <summary>
        /// Example of calling Security.svc/getsiteinfo to log into
        /// a specific site.
        /// </summary>
        private void LogonSite()
        {
            string siteid = Session["SessionID"].ToString();
            string enterprise = Session["EnterpriseGUID"].ToString();
            string authtoken = Session["AuthToken"].ToString();
            string baseurl = WebConfigurationManager.AppSettings["ETOSoftwareWS_BaseUrl"];

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseurl + string.Format("Security.svc/SSOSiteLogin/{0}/{1}/{2}/5", siteid, enterprise, authtoken));
            request.Method = "GET";

            try
            {
                WebResponse webResponse = request.GetResponse();
                DataContractJsonSerializer siteSer = new DataContractJsonSerializer(typeof(string));
                string SiteLoginResponse = siteSer.ReadObject(webResponse.GetResponseStream()) as string;
                Session["SecurityToken"] = SiteLoginResponse;
            }
            catch (WebException we)
            {
                string webExceptionMessage = we.Message;
            }
            catch (Exception ex)
            {
                Response.Redirect("Default.aspx");
            } 
        }

        /// <summary>
        /// Example of calling Security.svc/getsiteinfo to display details
        /// for a specific site.
        /// </summary>
        private void DisplaySiteInfo()
        {
            string siteid = Session["SessionID"].ToString();
            string enterprise = Session["EnterpriseGUID"].ToString();
            string baseurl = WebConfigurationManager.AppSettings["ETOSoftwareWS_BaseUrl"];           

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseurl + "Security.svc/GetSiteInfo/" + siteid);
            request.Method = "GET";
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("enterpriseGuid", enterprise);
            headers.Add("securityToken", Session["SecurityToken"].ToString());            
            request.Headers = headers;

            try
            {
                WebResponse webResponse = request.GetResponse();
                DataContractJsonSerializer siteSer = new DataContractJsonSerializer(typeof(SiteInfo));
                SiteInfo siteInfo = (SiteInfo)siteSer.ReadObject(webResponse.GetResponseStream());

                SiteNameLabel.Text = siteInfo.SiteName;
                AddressLabel.Text = siteInfo.Address1 + " " + siteInfo.Address2;
                PhoneLabel.Text = siteInfo.PhoneNumber;
                ZipLabel.Text = siteInfo.ZipCode;
                DisabledLabel.Text = siteInfo.Disabled ? "Yes" : "No";
            }
            catch (WebException we)
            {
                string webExceptionMessage = we.Message;
            }
            catch (Exception ex)
            {
                Response.Redirect("Default.aspx");
            } 
        }

        /// <summary>
        /// Example of calling Form.svc/Forms/Program/GetPrograms to get a 
        /// list of programs for a specific site.
        /// </summary>
        private void DisplayPrograms()
        {
            string siteid = Session["SessionID"].ToString();
            string enterprise = Session["EnterpriseGUID"].ToString();
            string baseurl = WebConfigurationManager.AppSettings["ETOSoftwareWS_BaseUrl"];

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseurl + "Form.svc/Forms/Program/GetPrograms/" + siteid);
            request.Method = "GET";
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("enterpriseGuid", enterprise);
            headers.Add("securityToken", Session["SecurityToken"].ToString());
            request.Headers = headers;

            try
            {
                WebResponse webResponse = request.GetResponse();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ProgramInfo[]));
                ProgramInfo[] progInfo = (ProgramInfo[])ser.ReadObject(webResponse.GetResponseStream());

                foreach (ProgramInfo pinfo in progInfo)
                {
                    ProgramDropList.Items.Add(new ListItem(pinfo.Name, pinfo.ID.ToString()));
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
        }

        /// <summary>
        /// Example of calling Search.svc/Search/ to search for participants
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void SearchText_TextChanged(object sender, EventArgs e)
        {
            string siteid = Session["SessionID"].ToString();
            string enterprise = Session["EnterpriseGUID"].ToString();
            string baseurl = WebConfigurationManager.AppSettings["ETOSoftwareWS_BaseUrl"];
            string programId = ProgramDropList.SelectedValue;
            string searchtext = SearchText.Text;
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseurl + "Search.svc/Search/" + programId + "/" + searchtext);
            request.Method = "GET";
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("enterpriseGuid", enterprise);
            headers.Add("securityToken", Session["SecurityToken"].ToString());
            request.Headers = headers;

            try
            {
                WebResponse webResponse = request.GetResponse();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(SearchResult[]));
                SearchResult[] results = (SearchResult[])ser.ReadObject(webResponse.GetResponseStream());

                if (results.Length == 0)
                {
                    SearchResults.Items.Clear();
                }
                else
                {
                    foreach (SearchResult result in results)
                    {
                        SearchResults.Items.Add(new ListItem(result.FName + " " + result.LName, result.CLID.ToString()));
                    }
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
        }

        ///// <summary>
        ///// Example of calling Actor.svc/participant to get participant details
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        protected void SearchResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            string siteid = Session["SessionID"].ToString();
            string enterprise = Session["EnterpriseGUID"].ToString();
            string baseurl = WebConfigurationManager.AppSettings["ETOSoftwareWS_BaseUrl"];
            string clid = SearchResults.SelectedValue;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseurl + "Actor.svc/participant/" + clid);
            request.Method = "GET";
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("enterpriseGuid", enterprise);
            headers.Add("securityToken", Session["SecurityToken"].ToString());
            request.Headers = headers;

            try
            {
                WebResponse webResponse = request.GetResponse();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Participant));
                Participant participant = (Participant)ser.ReadObject(webResponse.GetResponseStream());

                PartNameLabel.Text = participant.FirstName + " " + participant.LastName;
                PartIdLabel.Text = participant.ID.ToString();
                PartAddrLabel.Text = participant.Address1 + " " + participant.Address2;
                PartGenderLabel.Text = participant.Gender.ToString();
            }
            catch (WebException we)
            {
                string webExceptionMessage = we.Message;
            }
            catch (Exception ex)
            {
                Response.Redirect("Default.aspx");
            } 
        //    DisplayAssessments(clid);
        }

        /// <summary>
        /// Example of calling Form.svc/Forms/Assessments/GetAllAssessementResponses
        /// to get a list of assessment responses for a participant.
        /// </summary>
        /// <param name="clid"></param>
        //private void DisplayAssessments(string clid)
        //{
        //    string siteid = Session["SessionID"].ToString();
        //    string enterprise = Session["EnterpriseGUID"].ToString();
        //    string baseurl = WebConfigurationManager.AppSettings["ETOSoftwareWS_BaseUrl"];

        //    using (HttpClient client = new HttpClient(baseurl))
        //    {
        //        RequestHeaders headers = new RequestHeaders();
        //        headers.Add("enterpriseGuid", enterprise);
        //        headers.Add("securityToken", Session["SecurityToken"].ToString());

        //        string body = "{\"CLID\":\"";
        //        body = body + clid + "\",\"surveyResponderType\":";
        //        body = body + (int)SurveyResponderType.Client + "}";

        //        HttpResponseMessage resp = client.Send(HttpMethod.POST, 
        //                                            "Form.svc/Forms/Assessments/GetAllAssessementResponses/",
        //                                            headers, HttpContent.Create(body, "application/json; charset=utf-8"));
        //        resp.EnsureStatusIsSuccessful();

        //        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AssessmentResponse[]));
        //        AssessmentResponse[] responses = (AssessmentResponse[])ser.ReadObject(resp.Content.ReadAsStream());

        //        foreach (AssessmentResponse response in responses)
        //        {
        //            Panel2.Controls.Add(new LinkButton() { Text = response.SurveyName + " " + response.SurveyDate.ToString() + " " +
        //                response.SurveyResponseID.ToString() + " " + response.SurveyTaker });
        //        }
        //    }
        //}
    }
}