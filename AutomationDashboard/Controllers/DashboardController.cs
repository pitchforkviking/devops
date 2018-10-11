namespace AutomationDashboard.Controllers
{
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
    using Microsoft.Rest.Azure.Authentication;
    using Microsoft.VisualStudio.Services.WebApi;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using RestSharp;
    using RestSharp.Authenticators;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using SendGrid;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Linq;
    using SendGrid.Helpers.Mail;
    using System.Globalization;

    #region Classes - Move to Separate Files
    public class AuthResponse
    {
        public string token_type { get; set; }

        public int expires_in { get; set; }

        public int ext_expires_in { get; set; }

        public int expires_on { get; set; }

        public int enot_beforext_expires_in { get; set; }

        public string resource { get; set; }

        public string access_token { get; set; }
    }

    public class ResponseObject
    {
        public string Organization { get; set; }

        public string Environment { get; set; }

        public string RowKey { get; set; }

        public string Location { get; set; }

        public string Author { get; set; }

        public string Date { get; set; }
    }

    public class ResourceAccess
    {
        public string type { get; set; }
    }

    public class RequiredResource
    {
        public string resourceAppId { get; set; }

        public List<ResourceAccess> resourceAccess { get; set; }
    }

    public class Application
    {
        public string objectId { get; set; }

        public string appId { get; set; }

        public string objectType { get; set; }

        public string displayName { get; set; }

        public string homepage { get; set; }

        public List<string> identifierUris { get; set; }

        public List<string> replyUrls { get; set; }

        public List<RequiredResource> requiredResourceAccess { get; set; }

        //public List<PasswordCredential> passwordCredentials { get; set; }
    }

    public class ApplicationResponse
    {
        public string objectId { get; set; }

        public string appId { get; set; }

        public string displayName { get; set; }
    }

    public class ApplicationResponseCollection
    {
        public List<ApplicationResponse> value { get; set; }
    }

    public class ProvisionValueParameter
    {
        public string value { get; set; }
    }

    public class ProvisionParameters
    {
        public ProvisionValueParameter organization { get; set; }

        public ProvisionValueParameter environment { get; set; }

    }

    public class IdParameter
    {
        public string id { get; set; }
    }

    public class BuildParameters
    {
        public string Organization { get; set; }

        public string Environment { get; set; }

    }

    public class BuildDefinitionParameters
    {
        public IdParameter definition { get; set; }

        public BuildParameters parameters { get; set; }
    }

    public class InstanceReference
    {
        public string id { get; set; }

        public string name { get; set; }

        public string sourceVersion { get; set; }
    }

    public class ArtifactMetadata
    {
        public string alias { get; set; }

        public InstanceReference instanceReference { get; set; }
    }

    public class CheckInRequest
    {
        public string comment { get; set; }

        public List<ChangeRequest> changes { get; set; }
    }

    public class ChangeRequest
    {
        public int changeType { get; set; }

        public ChangeItem item { get; set; }

        public UpdatedContent newContent { get; set; }
    }

    public class ChangeItem
    {
        public string path { get; set; }

        public ContentMetadata contentMetadata { get; set; }

        public int version { get; set; }
    }

    public class UpdatedContent
    {
        public string content { get; set; }

        public int contentType { get; set; }
    }

    public class ContentMetadata
    {
        public int encoding { get; set; }
    }

    public class ReleaseStartMetadata
    {
        public int definitionId { get; set; }

        public string description { get; set; }

        public bool isDraft { get; set; }

        public string reason { get; set; }

        public List<string> manualEnvironments { get; set; }

        public PropertiesCollection properties { get; set; }

        public List<ArtifactMetadata> artifacts { get; set; }
    }

    public class TestResult
    {
        public int TotalTests { get; set; }

        public int PassedTests { get; set; }

        public int TotalBlocks { get; set; }

        public int CoveredBlocks { get; set; }

        public int TotalTime { get; set; }

        public int CodeCoverage { get; set; }

        public int PassPercentage { get; set; }
    }

    public class CustomerRequest : TableEntity
    {
        public string Name { get; set; }

        public string EmailID { get; set; }

        public string PassCode { get; set; }

        public string Organization { get; set; }

        public string Environment { get; set; }

        public string Location { get; set; }
    }

    public class ApplicationEvent : TableEntity
    {
        public string EmailID { get; set; }

        public string EventTitle { get; set; }

        public string EventDate { get; set; }

        public int Days { get; set; }

        public bool IsCountDown { get; set; }

    }

    public class VerifyRequest : TableEntity
    {
        //public string PassCode { get; set; }        

        public string Organization { get; set; }
    }

    public class ApplicationUser : TableEntity
    {
        public string EmailID { get; set; }

        public string Password { get; set; }
    }

    public class PasswordCredential
    {
        public string endDate { get; set; }

        public string startDate { get; set; }

        public string keyId { get; set; }

        public string value { get; set; }

        public string customKeyIdentifier { get; set; }
    }

    #endregion

    public class DashboardController : ApiController
    {
        #region Constants

        //To-Do: Move to Config

        private const string AdTenantId = "95a88597-35af-4a35-a625-abb16558f11f";
        private const string AdClientId = "684bf006-6bd0-4139-8a19-22f34c01386b";
        private const string AdClientSecret = "Est0srbYCE8jJGnkRWHvmGbtQ242BGkCDX6/7+abE14=";

        private const string TenantId = "85c997b9-f494-46b3-a11d-772983cf6f11";
        private const string SubscriptionId = "d1e68fbe-4700-45b8-95b7-5eb5fec223bd";
        private const string ClientId = "e29e4f23-f7b2-4ac1-9eb1-40f7271dbb69";
        private const string ClientSecret = "Mpcvc+2FQPw1/fd6F20xDC2GWREbCCWxKMlO8TfPDgA=";

        private const string CollectionUri = @"https://tfs.mindtree.com/tfs/defaultcollection/ITS-Projects%20on%20the%20Cloud";
        private const string BasicUser = "Mindtree\\M1041558";
        private const string BasicPassword = "$Mindtree@4548$";

        private const string StorageString = "DefaultEndpointsProtocol=https;AccountName=automationdashboard;AccountKey=GHFU+0DRMKUa9RNoBIbKvXp6p0pl0CNbCpp5NPOuPcrccE8Ly1jLg4/bmCh5eqsG4aSzwwzBXqrxOh3Jg/sHJg==;EndpointSuffix=core.windows.net";
        #endregion

        #region Private Methods

        private static string GetAccessToken()
        {
            RestClient restClient = new RestClient(string.Format("{0}{1}", "https://login.microsoftonline.com/", AdTenantId));

            RestRequest restRequest = new RestRequest("/oauth2/token", Method.POST);

            restRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            restRequest.AddParameter("grant_type", "client_credentials");

            //Move to Azure Key Vault
            restRequest.AddParameter("client_id", AdClientId);
            restRequest.AddParameter("client_secret", AdClientSecret);

            IRestResponse restResponse = restClient.Execute(restRequest);
            var responseContent = restResponse.Content;

            var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);

            return authResponse.access_token;
        }

        private static string VerifyCustomerRequest(CustomerRequest request)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("VerifyRequest");
            //table.CreateIfNotExists();     

            TableOperation retrieveOperation = TableOperation.Retrieve<VerifyRequest>("PassCode", request.PassCode);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result != null)
            {
                return ((VerifyRequest)retrievedResult.Result).Organization;
            }
            else
            {
                return null;
            }
        }

        private static string AddCustomerRequest(CustomerRequest request)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("CustomerRequest");
            table.CreateIfNotExists();

            string rowKey = Convert.ToString(Guid.NewGuid());
            request.PartitionKey = request.PassCode;
            request.RowKey = rowKey;

            TableOperation insertOperation = TableOperation.Insert(request);
            table.Execute(insertOperation);

            return rowKey;
        }

        private static CustomerRequest GetCustomerRequest(string partitionKey, string rowKey)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("CustomerRequest");

            TableOperation retrieveOperation = TableOperation.Retrieve<CustomerRequest>(partitionKey, rowKey);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult != null)
            {
                CustomerRequest customerRequest = (CustomerRequest)retrievedResult.Result;
                return customerRequest;
            }

            return null;
        }

        #endregion

        [HttpPost]
        public IHttpActionResult Begin(CustomerRequest request)
        {
            try
            {
                var organization = VerifyCustomerRequest(request);
                if (organization != null)
                {
                    request.Organization = organization;
                    string rowKey = AddCustomerRequest(request);
                    return Ok(rowKey);
                }
                else
                {
                    return BadRequest("No Organization Mapped to the PassCode");
                }
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }

        [HttpGet]
        public IHttpActionResult Register(string passcode, string key)
        {
            try
            {
                //To-Do: Get Customer Request from Table Storage (key)
                CustomerRequest customerRequest = GetCustomerRequest(passcode, key);

                Application application = null;

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string accessToken = GetAccessToken();

                #region Add Provider
                ResourceAccess providerResourceAccess = new ResourceAccess
                {
                    type = "Scope"
                };

                List<ResourceAccess> providerResourceAccessList = new List<ResourceAccess> { providerResourceAccess };

                RequiredResource providerRequiredResource = new RequiredResource
                {
                    //For Windows Azure Active Directory
                    resourceAppId = "00000002-0000-0000-c000-000000000000",
                    resourceAccess = providerResourceAccessList
                };

                //PasswordCredential passwordCredential = new PasswordCredential
                //{
                //    startDate = DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ss.fffZ"),
                //    endDate = DateTime.UtcNow.AddYears(2).ToString("yyyy-MM-ddThh:mm:ss.fffZ"),
                //    value = "C!iEn#$eCrEt!",
                //    keyId = Convert.ToString(Guid.NewGuid()),
                //    customKeyIdentifier = "ObJix/HDVU+3+hH5RmA+dw=="
                //};

                //List<PasswordCredential> passwordCredentialList = new List<PasswordCredential> { passwordCredential };

                List<RequiredResource> providerRequiredResourceList = new List<RequiredResource> { providerRequiredResource };

                Application providerApplication = new Application
                {
                    displayName = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "Provider"),
                    homepage = string.Format("{0}{1}{2}{3}", "https://", customerRequest.Organization.ToLower(), customerRequest.Environment.ToLower(), "provider.azurewebsites.net/"),
                    identifierUris = new List<string> { string.Format("{0}{1}{2}{3}", "https://", customerRequest.Organization.ToLower(), customerRequest.Environment.ToLower(), "provider.azurewebsites.net/"), },
                    replyUrls = new List<string> { string.Format("{0}{1}{2}{3}", "https://", customerRequest.Organization.ToLower(), customerRequest.Environment.ToLower(), "provider.azurewebsites.net/"), },
                    objectType = "Application",
                    requiredResourceAccess = providerRequiredResourceList,             
                    //passwordCredentials = passwordCredentialList
                };

                var jsonString = JsonConvert.SerializeObject(providerApplication);

                //To-Do: Refactor
                RestClient restClient = new RestClient(string.Format("{0}{1}", "https://graph.windows.net/", AdTenantId));

                RestRequest restRequest = new RestRequest("/applications?api-version=1.6", Method.POST);

                restRequest.AddHeader("Authorization", "Bearer " + accessToken);
                restRequest.AddHeader("Content-Type", "application/json");

                restRequest.AddParameter("application/json", jsonString, ParameterType.RequestBody);

                IRestResponse restResponse = restClient.Execute(restRequest);
                var responseContent = restResponse.Content;

                if (restResponse.StatusCode == HttpStatusCode.Created)
                {
                    application = JsonConvert.DeserializeObject<Application>(responseContent);
                }
                #endregion

                #region Add Provider Service Principal            

                //To-Do: Refactor
                jsonString = "{\"appId\": \"" + application.appId + "\"}";

                //To-Do: Refactor
                restClient = new RestClient(string.Format("{0}{1}", "https://graph.windows.net/", AdTenantId));

                restRequest = new RestRequest("/servicePrincipals?api-version=1.6", Method.POST);

                restRequest.AddHeader("Authorization", "Bearer " + accessToken);
                restRequest.AddHeader("Content-Type", "application/json");

                restRequest.AddParameter("application/json", jsonString, ParameterType.RequestBody);

                //To-Do: Add 'PasswordCredentials'

                restResponse = restClient.Execute(restRequest);
                responseContent = restResponse.Content;

                if (restResponse.StatusCode == HttpStatusCode.Created)
                {
                    //To-Do: Proceed                
                }
                else
                {
                    //To-Do: Handle Error
                }
                #endregion

                #region Add Consumer
                ResourceAccess consumerResourceAccess = new ResourceAccess
                {
                    type = "Scope"
                };

                List<ResourceAccess> consumerResourceAccessList = new List<ResourceAccess> { consumerResourceAccess };

                List<RequiredResource> consumerRequiredResourceList = new List<RequiredResource>();

                RequiredResource consumerRequiredResource = new RequiredResource
                {
                    resourceAppId = "00000002-0000-0000-c000-000000000000",
                    resourceAccess = consumerResourceAccessList
                };

                consumerRequiredResourceList.Add(consumerRequiredResource);

                consumerRequiredResource = new RequiredResource
                {
                    resourceAppId = application.appId,
                    resourceAccess = consumerResourceAccessList
                };

                consumerRequiredResourceList.Add(consumerRequiredResource);

                Application consumerApplication = new Application
                {
                    displayName = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "Consumer"),
                    homepage = string.Format("{0}{1}{2}{3}", "https://", customerRequest.Organization.ToLower(), customerRequest.Environment.ToLower(), "consumer.azurewebsites.net/"),
                    identifierUris = new List<string> { string.Format("{0}{1}{2}{3}", "https://", customerRequest.Organization.ToLower(), customerRequest.Environment.ToLower(), "consumer.azurewebsites.net/"), },
                    replyUrls = new List<string> { string.Format("{0}{1}{2}{3}", "https://", customerRequest.Organization.ToLower(), customerRequest.Environment.ToLower(), "consumer.azurewebsites.net/"), },
                    objectType = "Application",
                    requiredResourceAccess = consumerRequiredResourceList
                };

                jsonString = JsonConvert.SerializeObject(consumerApplication);

                //To-Do: Refactor
                restClient = new RestClient(string.Format("{0}{1}", "https://graph.windows.net/", AdTenantId));

                restRequest = new RestRequest("/applications?api-version=1.6", Method.POST);

                restRequest.AddHeader("Authorization", "Bearer " + accessToken);
                restRequest.AddHeader("Content-Type", "application/json");

                restRequest.AddParameter("application/json", jsonString, ParameterType.RequestBody);


                restResponse = restClient.Execute(restRequest);
                responseContent = restResponse.Content;

                if (restResponse.StatusCode == HttpStatusCode.Created)
                {
                    application = JsonConvert.DeserializeObject<Application>(responseContent);
                }
                #endregion

                #region Add Consumer Service Principal

                //To-Do: Refactor
                jsonString = "{\"appId\": \"" + application.appId + "\"}";

                //To-Do: Refactor
                restClient = new RestClient(string.Format("{0}{1}", "https://graph.windows.net/", AdTenantId));

                restRequest = new RestRequest("/servicePrincipals?api-version=1.6", Method.POST);

                restRequest.AddHeader("Authorization", "Bearer " + accessToken);
                restRequest.AddHeader("Content-Type", "application/json");

                restRequest.AddParameter("application/json", jsonString, ParameterType.RequestBody);

                //To-Do: Add 'PasswordCredentials'

                restResponse = restClient.Execute(restRequest);
                responseContent = restResponse.Content;

                if (restResponse.StatusCode == HttpStatusCode.Created)
                {
                    //Proceed
                }
                else
                {
                    //To-Do: Handle Error
                }
                #endregion

                stopwatch.Stop();
                return Ok(stopwatch.ElapsedMilliseconds / 1000);
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> Provision(string passcode, string key)
        {
            try
            {
                //To-Do: Get Customer Request from Table Storage (key)     
                CustomerRequest customerRequest = GetCustomerRequest(passcode, key);

                // OK
                string resourceGroupName = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "RG");
                string deploymentName = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "DM");
                string resourceGroupLocation = customerRequest.Location;

                //To-Do: Move to constants
                string pathToTemplateFile = @"\Templates\osmosis-template.json";

                //To-Do: Parse this
                ProvisionParameters parameters = new ProvisionParameters
                {
                    environment = new ProvisionValueParameter { value = customerRequest.Environment },
                    organization = new ProvisionValueParameter { value = customerRequest.Organization }
                };

                string temp = JsonConvert.SerializeObject(parameters);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(TenantId, ClientId, ClientSecret);

                JObject templateFileContents = new JObject();

                using (StreamReader file = File.OpenText(System.Web.HttpRuntime.AppDomainAppPath + pathToTemplateFile))
                {
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        templateFileContents = (JObject)JToken.ReadFrom(reader);
                    }
                }


                var resourceManagementClient = new ResourceManagementClient(serviceCreds);
                resourceManagementClient.SubscriptionId = SubscriptionId;

                if (await resourceManagementClient.ResourceGroups.CheckExistenceAsync(resourceGroupName) != true)
                {
                    var resourceGroup = new ResourceGroupInner();
                    resourceGroup.Location = resourceGroupLocation;
                    await resourceManagementClient.ResourceGroups.CreateOrUpdateAsync(resourceGroupName, resourceGroup);
                }

                var deployment = new DeploymentInner();

                deployment.Properties = new DeploymentProperties
                {
                    Mode = DeploymentMode.Incremental,
                    Template = templateFileContents,
                    Parameters = JObject.Parse(temp)
                };

                DeploymentExtendedInner deploymentResult = await resourceManagementClient.Deployments.CreateOrUpdateAsync(resourceGroupName, deploymentName, deployment);

                var outputs = deploymentResult.Properties.Outputs.ToString();

                //ParseOutput(outputs);

                if (deploymentResult.Properties.ProvisioningState == "Failed")
                {
                    await resourceManagementClient.ResourceGroups.DeleteAsync(resourceGroupName);
                    return BadRequest("Failed to Create Azure Resources");
                }

                stopwatch.Stop();
                return Ok(stopwatch.ElapsedMilliseconds / 1000);
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }

        /// <summary>
        /// Checks In Files To TFS
        /// </summary>
        /// <param name="passcode">Passcode mapped to an Organization</param>
        /// <param name="key">RowKey of the Customer Request</param>
        /// <returns></returns>
        [HttpGet]        
        public IHttpActionResult CheckIn(string passcode, string key)
        {
            try
            {
                //To-Do: Get Customer Request from Table Storage (key)     
                CustomerRequest customerRequest = GetCustomerRequest(passcode, key);

                var consumerConfigPath = @"\Configs\Consumer.config";
                var providerConfigPath = @"\Configs\Provider.config";
                var consumerConfigPathInTfs = @"$/ITS-Projects on the Cloud/trunk/MT SaaS Delivery Platform-Refactor/Automation/Osmosis/Consumer Portal/Consumer/Web.Release.config";
                var providerConfigPathInTfs = @"$/ITS-Projects on the Cloud/trunk/MT SaaS Delivery Platform-Refactor/Automation/Osmosis/Provider API/Provider/Web.Release.config";

                string resourceGroupName = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "RG");
                string deploymentName = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "DM");

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Get Outputs from Last Deployment
                var serviceCreds = ApplicationTokenProvider.LoginSilentAsync(TenantId, ClientId, ClientSecret).Result;

                var resourceManagementClient = new ResourceManagementClient(serviceCreds);
                resourceManagementClient.SubscriptionId = SubscriptionId;

                DeploymentExtendedInner deploymentResult = resourceManagementClient.Deployments.GetAsync(resourceGroupName, deploymentName).Result;

                var outputs = deploymentResult.Properties.Outputs.ToString();

                var jObject = JObject.Parse(outputs);
                var providerConnString = jObject["provider_storage"]["value"].ToString();
                var providerApi = jObject["provider_api"]["value"].ToString();

                RestClient restClient = new RestClient(CollectionUri);
                restClient.Authenticator = new HttpBasicAuthenticator(BasicUser, BasicPassword);

                //Checking In Consumer Config
                RestRequest restRequest = new RestRequest(string.Format("{0}{1}{2}", "_apis/tfvc/changesets?api-version=1.0&searchCriteria.itemPath=", consumerConfigPathInTfs, "&$top=1"), Method.GET);
                IRestResponse restResponse = restClient.Execute(restRequest);
                var responseContent = restResponse.Content;

                jObject = JObject.Parse(responseContent);
                var changesetId = jObject["value"].First["changesetId"].ToString();

                var updatedConfigContent = string.Empty;
                using (StreamReader file = File.OpenText(System.Web.HttpRuntime.AppDomainAppPath + consumerConfigPath))
                {
                    updatedConfigContent = file.ReadToEnd();
                }

                var contentString = updatedConfigContent.Replace("@ApiUrl@", providerApi);

                CheckInRequest checkInRequest = new CheckInRequest
                {
                    comment = "Checked In by Autobot",
                    changes = new List<ChangeRequest>
                    {
                        new ChangeRequest
                        {
                            changeType = 2,
                            item = new ChangeItem
                            {
                                path = consumerConfigPathInTfs,
                                contentMetadata = new ContentMetadata
                                {
                                    encoding = 65001
                                },
                                version = Convert.ToInt32(changesetId)
                            },
                            newContent = new UpdatedContent
                            {
                                content = contentString,
                                contentType = 0
                            }
                        }
                    }
                };

                restRequest = new RestRequest("_apis/tfvc/changesets?api-version=3.0-preview.2", Method.POST);
                restRequest.AddHeader("Content-Type", "application/json");
                restRequest.AddParameter("application/json", JsonConvert.SerializeObject(checkInRequest), ParameterType.RequestBody);
                restResponse = restClient.Execute(restRequest);

                //Checking In Provider Config
                restRequest = new RestRequest(string.Format("{0}{1}{2}", "_apis/tfvc/changesets?api-version=1.0&searchCriteria.itemPath=", providerConfigPathInTfs, "&$top=1"), Method.GET);
                restResponse = restClient.Execute(restRequest);
                responseContent = restResponse.Content;

                jObject = JObject.Parse(responseContent);
                changesetId = jObject["value"].First["changesetId"].ToString();

                updatedConfigContent = string.Empty;
                using (StreamReader file = File.OpenText(System.Web.HttpRuntime.AppDomainAppPath + providerConfigPath))
                {
                    updatedConfigContent = file.ReadToEnd();
                }

                contentString = updatedConfigContent.Replace("@ProviderStorage@", providerConnString);

                checkInRequest = new CheckInRequest
                {
                    comment = "Checked In by Autobot",
                    changes = new List<ChangeRequest>
                    {
                        new ChangeRequest
                        {
                            changeType = 2,
                            item = new ChangeItem
                            {
                                path = providerConfigPathInTfs,
                                contentMetadata = new ContentMetadata
                                {
                                    encoding = 65001
                                },
                                version = Convert.ToInt32(changesetId)
                            },
                            newContent = new UpdatedContent
                            {
                                content = contentString,
                                contentType = 0
                            }
                        }
                    }
                };

                restRequest = new RestRequest("_apis/tfvc/changesets?api-version=3.0-preview.2", Method.POST);
                restRequest.AddHeader("Content-Type", "application/json");
                restRequest.AddParameter("application/json", JsonConvert.SerializeObject(checkInRequest), ParameterType.RequestBody);
                restResponse = restClient.Execute(restRequest);

                stopwatch.Stop();
                return Ok(stopwatch.ElapsedMilliseconds/1000);
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }

        [HttpGet]
        public IHttpActionResult Build(string passcode, string key)
        {
            try
            {
                //To-Do: Get Customer Request from Table Storage (key)            
                CustomerRequest customerRequest = GetCustomerRequest(passcode, key);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                RestClient restClient = new RestClient(CollectionUri);
                restClient.Authenticator = new HttpBasicAuthenticator(BasicUser, BasicPassword);

                //To-Do: Form Build Parameters

                string jsonString = "{\"definition\": {\"id\": 855}}";

                RestRequest restRequest = new RestRequest("/_apis/build/builds?api-version=2.0", Method.POST);

                //restRequest.AddHeader("Authorization", string.Format("{0} {1}", "Bearer", GetAccessToken()));            
                restRequest.AddHeader("Content-Type", "application/json");

                restRequest.AddParameter("application/json", jsonString, ParameterType.RequestBody);

                IRestResponse restResponse = restClient.Execute(restRequest);
                var responseContent = restResponse.Content;

                var jObject = JObject.Parse(responseContent);
                var buildId = jObject["id"].ToString();

                restRequest = new RestRequest("/_apis/build/builds/" + buildId + "?api-version=2.0", Method.GET);
                restResponse = restClient.Execute(restRequest);
                responseContent = restResponse.Content;

                jObject = JObject.Parse(responseContent);
                var status = jObject["status"].ToString();

                while (status != "completed")
                {
                    Thread.Sleep(5000);
                    restRequest = new RestRequest("/_apis/build/builds/" + buildId + "?api-version=2.0", Method.GET);
                    restResponse = restClient.Execute(restRequest);
                    responseContent = restResponse.Content;
                    jObject = JObject.Parse(responseContent);
                    status = jObject["status"].ToString();
                }

                stopwatch.Stop();
                return Ok(stopwatch.ElapsedMilliseconds / 1000);
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }

        [HttpGet]
        public IHttpActionResult Test(string passcode, string key)
        {
            /*
            try
            {
                //To-Do: Get Customer Request from Table Storage (key)            
                CustomerRequest customerRequest = GetCustomerRequest(passcode, key);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                RestClient restClient = new RestClient(CollectionUri);
                restClient.Authenticator = new HttpBasicAuthenticator(BasicUser, BasicPassword);

                RestRequest restRequest = new RestRequest("/_apis/build/builds?definitions=855&statusFilter=completed&$top=1&api-version=2.0", Method.GET);
                IRestResponse restResponse = restClient.Execute(restRequest);
                var responseContent = restResponse.Content;

                JObject jObject = JObject.Parse(responseContent);
                var buildId = jObject["value"].First["id"].ToString();
                var buildNumber = jObject["value"].First["buildNumber"].ToString();

                restRequest = new RestRequest(string.Format("{0}{1}", "/_apis/test/codeCoverage?api-version=2.0-preview&buildId=", buildId), Method.GET);
                restResponse = restClient.Execute(restRequest);
                responseContent = restResponse.Content;

                jObject = JObject.Parse(responseContent);
                double totalBlocks = Convert.ToDouble(jObject["coverageData"].First["coverageStats"].First["total"]);
                double coveredBlocks = Convert.ToDouble(jObject["coverageData"].First["coverageStats"].First["covered"]);

                double codeCoverage = (coveredBlocks / totalBlocks) * 100;

                string jsonString = "{\"query\": \"Select* From TestRun Where BuildNumber = '" + buildNumber + "' \"}";

                restRequest = new RestRequest("_apis/test/runs/query?api-version=2.0-preview&includeRunDetails=true", Method.POST);
                restRequest.AddHeader("Content-Type", "application/json");
                restRequest.AddParameter("application/json", jsonString, ParameterType.RequestBody);
                restResponse = restClient.Execute(restRequest);
                responseContent = restResponse.Content;

                jObject = JObject.Parse(responseContent);
                double totalTests = Convert.ToDouble(jObject["value"].First["totalTests"]);
                double passedTests = Convert.ToDouble(jObject["value"].First["passedTests"]);

                double passPercentage = (passedTests / totalTests) * 100;

                stopwatch.Stop();

                TestResult testResult = new TestResult
                {
                    TotalTime = Convert.ToInt32(stopwatch.ElapsedMilliseconds / 1000),
                    PassPercentage = Convert.ToInt32(passPercentage),
                    CodeCoverage = Convert.ToInt32(codeCoverage)
                };

                return Ok(testResult);
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
            */
            return Ok("Something");
        }

        [HttpGet]
        public IHttpActionResult Release(string passcode, string key)
        {
            try
            {
                //To-Do: Get Customer Request from Table Storage (key)     
                CustomerRequest customerRequest = GetCustomerRequest(passcode, key);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                RestClient restClient = new RestClient(CollectionUri);
                restClient.Authenticator = new HttpBasicAuthenticator(BasicUser, BasicPassword);

                RestRequest restRequest = new RestRequest("/_apis/build/builds?definitions=855&statusFilter=completed&$top=1&api-version=2.0", Method.GET);

                //restRequest.AddHeader("Authorization", string.Format("{0} {1}", "Bearer", GetAccessToken()));            

                IRestResponse restResponse = restClient.Execute(restRequest);
                var responseContent = restResponse.Content;

                JObject jObject = JObject.Parse(responseContent);
                var buildId = jObject["value"].First["id"].ToString();
                var sourceVersion = jObject["value"].First["sourceVersion"].ToString();

                restRequest = new RestRequest("/_apis/release/definitions/7?api-version=1.0", Method.GET);
                restResponse = restClient.Execute(restRequest);
                responseContent = restResponse.Content;

                jObject = JObject.Parse(responseContent);
                var env = jObject["environments"].First["variables"]["Environment"]["value"].ToString();
                var org = jObject["environments"].First["variables"]["Organization"]["value"].ToString();

                responseContent = responseContent.Replace(env, customerRequest.Environment.ToLower()).Replace(org, customerRequest.Organization.ToLower());

                restRequest = new RestRequest("/_apis/release/definitions?api-version=1.0", Method.PUT);

                //restRequest.AddHeader("Authorization", string.Format("{0} {1}", "Bearer", GetAccessToken()));            
                restRequest.AddHeader("Content-Type", "application/json");

                restRequest.AddParameter("application/json", responseContent, ParameterType.RequestBody);

                restResponse = restClient.Execute(restRequest);

                //To-Do: Form Build Parameters                        
                //string jsonString = "{\"definitionId\": 7, \"description\": \"Creating Sample release\", \"isDraft\": false, \"reason\": \"none\", \"manualEnvironments\": null, \"artifacts\": [{ \"alias\": \"Test\", \"instanceReference\": { \"id\": \"20171212.8\", \"name\": \"TestScript\", \"sourceVersion\": \"724794\" }}]}";

                ReleaseStartMetadata releaseStartMetadata = new ReleaseStartMetadata
                {
                    definitionId = 7,
                    description = "Triggered by Autobot",
                    isDraft = false,
                    //manualEnvironments = null,
                    reason = "none",
                    artifacts = new List<ArtifactMetadata>
                {
                    new ArtifactMetadata
                    {
                        alias = "Test",
                        instanceReference = new InstanceReference
                        {
                            id = buildId,
                            name = "TestScript",
                            sourceVersion = sourceVersion
                        }
                    }
                }
                };

                string jsonString = JsonConvert.SerializeObject(releaseStartMetadata);

                restRequest = new RestRequest("/_apis/release/releases?api-version=1.0", Method.POST);

                //restRequest.AddHeader("Authorization", string.Format("{0} {1}", "Bearer", GetAccessToken()));            
                restRequest.AddHeader("Content-Type", "application/json");

                restRequest.AddParameter("application/json", jsonString, ParameterType.RequestBody);

                restResponse = restClient.Execute(restRequest);
                responseContent = restResponse.Content;

                jObject = JObject.Parse(responseContent);
                var releaseId = jObject["id"].ToString();

                restRequest = new RestRequest("/_apis/release/releases/" + releaseId + "?api-version=1.0", Method.GET);
                restResponse = restClient.Execute(restRequest);
                responseContent = restResponse.Content;

                jObject = JObject.Parse(responseContent);
                var status = jObject["environments"].First["status"].ToString();

                while (status != "succeeded")
                {
                    Thread.Sleep(5000);
                    restRequest = new RestRequest("/_apis/release/releases/" + releaseId + "?api-version=1.0", Method.GET);
                    restResponse = restClient.Execute(restRequest);
                    responseContent = restResponse.Content;
                    jObject = JObject.Parse(responseContent);
                    status = jObject["environments"].First["status"].ToString();
                }

                stopwatch.Stop();
                return Ok(stopwatch.ElapsedMilliseconds / 1000);
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }

        [HttpGet]
        public IHttpActionResult Seed(string passcode, string key)
        {
            try
            {
                //To-Do: Get Customer Request from Table Storage (key)                        
                CustomerRequest customerRequest = GetCustomerRequest(passcode, key);

                // OK
                string resourceGroupName = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "RG");
                string deploymentName = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "DM");

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                var serviceCreds = ApplicationTokenProvider.LoginSilentAsync(TenantId, ClientId, ClientSecret).Result;

                var resourceManagementClient = new ResourceManagementClient(serviceCreds);
                resourceManagementClient.SubscriptionId = SubscriptionId;

                DeploymentExtendedInner deploymentResult = resourceManagementClient.Deployments.GetAsync(resourceGroupName, deploymentName).Result;

                var outputs = deploymentResult.Properties.Outputs.ToString();

                var jObject = JObject.Parse(outputs);
                var connString = jObject["provider_storage"]["value"].ToString();

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                // Seed ApplicationUser Data
                CloudTable table = tableClient.GetTableReference("ApplicationUser");
                table.CreateIfNotExists();

                string rowKey = customerRequest.EmailID;
                string partitionKey = customerRequest.EmailID.Substring(0, 1);

                ApplicationUser applicationUser = new ApplicationUser
                {
                    EmailID = customerRequest.EmailID,
                    Password = "P@5$w0rD",
                    RowKey = rowKey,
                    PartitionKey = partitionKey
                };

                TableOperation insertOperation = TableOperation.Insert(applicationUser);
                table.Execute(insertOperation);

                // Seed ApplicationEvenData
                ApplicationEvent applicationEvent = new ApplicationEvent
                {
                    EventTitle = "We Have Sowed a Seed In Your Database",
                    EmailID = customerRequest.EmailID,
                    EventDate = Convert.ToString(DateTime.Now, CultureInfo.InvariantCulture),
                    Days = 1,
                    IsCountDown = false,
                    PartitionKey = customerRequest.EmailID,
                    RowKey = Convert.ToString(Guid.NewGuid())
                };

                table = tableClient.GetTableReference("ApplicationEvent");
                table.CreateIfNotExists();

                insertOperation = TableOperation.Insert(applicationEvent);
                table.Execute(insertOperation);

                applicationEvent.RowKey = Convert.ToString(Guid.NewGuid());
                applicationEvent.EventTitle = "Feel Free To Discard It, Anytime!";
                applicationEvent.Days = 3;
                applicationEvent.IsCountDown = true;

                insertOperation = TableOperation.Insert(applicationEvent);
                table.Execute(insertOperation);

                applicationEvent.RowKey = Convert.ToString(Guid.NewGuid());
                applicationEvent.EventTitle = "Or Keep It, Whatever!";
                applicationEvent.Days = 23;
                applicationEvent.IsCountDown = false;

                insertOperation = TableOperation.Insert(applicationEvent);
                table.Execute(insertOperation);

                stopwatch.Stop();
                return Ok(stopwatch.ElapsedMilliseconds / 1000);
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> Mail(string passcode, string key)
        {
            try
            {
                //To-Do: Get Customer Request from Table Storage (key)                        
                CustomerRequest customerRequest = GetCustomerRequest(passcode, key);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                var apiKey = "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";
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress("no-reply@automation.com", "Automation");
                var subject = string.Format("Your Environment is Ready!");
                var to = new EmailAddress(customerRequest.EmailID, customerRequest.Name);
                var plainTextContent = string.Format("{0}{1}{2}{3}{4}{5}-{6}-{7}{8}{9}{10}",
                                            "Hello ",
                                            customerRequest.Name,
                                            "!",
                                            "Your Environment is Ready!",
                                            "Access @ https://",
                                            customerRequest.Organization.ToLower(),
                                            customerRequest.Environment.ToLower(),
                                            "consumer.azurewebsites.net/",
                                            "EmailID:",
                                            customerRequest.EmailID,
                                            "Password: P@5$w0rD");
                var htmlContent = string.Format("{0}{1}{2}{3}{4}{5}-{6}-{7} {8}{9}{10}",
                                            "<p>Hello ",
                                            customerRequest.Name,
                                            "!</p>",
                                            "<p>Your Environment is Ready!</p>",
                                            "<p>Checkout @ https://",
                                            customerRequest.Organization.ToLower(),
                                            customerRequest.Environment.ToLower(),
                                            "consumer.azurewebsites.net/</p>",
                                            "<p>EmailID:",
                                            customerRequest.EmailID,
                                            "</p><p>Password: P@5$w0rD</p>");

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                var response = await client.SendEmailAsync(msg);


                stopwatch.Stop();
                return Ok(stopwatch.ElapsedMilliseconds / 1000);
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }

        [HttpGet]
        public IHttpActionResult Fetch(string passcode)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("CustomerRequest");

                TableQuery<CustomerRequest> rangeQuery = new TableQuery<CustomerRequest>().Where(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, passcode));

                List<ResponseObject> response = new List<ResponseObject>();
                foreach (CustomerRequest entity in table.ExecuteQuery(rangeQuery))
                {
                    response.Add(new ResponseObject { Environment = entity.Environment, Organization = entity.Organization, RowKey = entity.RowKey, Date = Convert.ToString(entity.Timestamp), Author = entity.Name, Location = entity.Location });
                }

                if (response.Count == 0)
                {
                    return BadRequest("No Deployments Found!");
                }
                else
                {
                    return Ok(response);
                }

            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> Delete(string passcode, string key)
        {
            try
            {
                //To-Do: Get Customer Request from Table Storage (key)
                CustomerRequest customerRequest = GetCustomerRequest(passcode, key);

                var providerApplication = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "Provider");
                var consumerApplication = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "Consumer");

                //Stopwatch stopwatch = new Stopwatch();
                //stopwatch.Start();

                string accessToken = GetAccessToken();

                //To-Do: Refactor
                RestClient restClient = new RestClient(string.Format("{0}{1}", "https://graph.windows.net/", AdTenantId));

                RestRequest restRequest = new RestRequest("/applications?api-version=1.6", Method.GET);

                restRequest.AddHeader("Authorization", "Bearer " + accessToken);

                IRestResponse restResponse = restClient.Execute(restRequest);
                var responseContent = restResponse.Content;

                var applicationResponseCollection = JsonConvert.DeserializeObject<ApplicationResponseCollection>(responseContent);

                var providerObjectId = applicationResponseCollection.value.Where(a => a.displayName == providerApplication).First().objectId;
                var consumerObjectId = applicationResponseCollection.value.Where(a => a.displayName == consumerApplication).First().objectId;

                var deleteUri = "/applications/" + providerObjectId + "?api-version=1.6";
                restRequest = new RestRequest(deleteUri, Method.DELETE);
                restRequest.AddHeader("Authorization", "Bearer " + accessToken);
                restResponse = restClient.Execute(restRequest);

                deleteUri = "/applications/" + consumerObjectId + "?api-version=1.6";
                restRequest = new RestRequest(deleteUri, Method.DELETE);
                restRequest.AddHeader("Authorization", "Bearer " + accessToken);
                restResponse = restClient.Execute(restRequest);

                string resourceGroupName = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "RG");
                string deploymentName = string.Format("{0}-{1}-{2}", customerRequest.Organization, customerRequest.Environment, "DM");

                var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(TenantId, ClientId, ClientSecret);

                var resourceManagementClient = new ResourceManagementClient(serviceCreds);
                resourceManagementClient.SubscriptionId = SubscriptionId;

                if (await resourceManagementClient.ResourceGroups.CheckExistenceAsync(resourceGroupName) == true)
                {
                    await resourceManagementClient.ResourceGroups.DeleteAsync(resourceGroupName);
                }

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("CustomerRequest");

                TableOperation deleteOperation = TableOperation.Delete(customerRequest);
                table.Execute(deleteOperation);

                return Ok();
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }
    }
}
