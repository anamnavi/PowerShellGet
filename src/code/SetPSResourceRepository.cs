// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PowerShell.PSResourceGet.UtilClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

using Dbg = System.Diagnostics.Debug;

namespace Microsoft.PowerShell.PSResourceGet.Cmdlets
{
    /// <summary>
    /// The Set-PSResourceRepository cmdlet is used to set information for a repository.
    /// </summary>
    [Cmdlet(VerbsCommon.Set,
        "PSResourceRepository",
        DefaultParameterSetName = NameParameterSet,
        SupportsShouldProcess = true)]
    public sealed class SetPSResourceRepository : PSCmdlet, IDynamicParameters
    {
        #region Members

        private const string NameParameterSet = "NameParameterSet";
        private const string RepositoriesParameterSet = "RepositoriesParameterSet";
        private const int DefaultPriority = -1;
        private Uri _uri;
        private CredentialProviderDynamicParameters _credentialProvider;

        #endregion

        #region Parameters

        /// <summary>
        /// Specifies the name of the repository to be set.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = NameParameterSet, HelpMessage = "Name of the repository to set properties for.")]
        [ArgumentCompleter(typeof(RepositoryNameCompleter))]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        /// Specifies the location of the repository to be set.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Uri { get; set; }

        /// <summary>
        /// Specifies a hashtable of repositories and is used to register multiple repositories at once.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = RepositoriesParameterSet, HelpMessage = "Hashtable including information on single or multiple repositories to set specified information for.")]
        [ValidateNotNullOrEmpty]
        public Hashtable[] Repository { get; set; }

        /// <summary>
        /// Specifies whether the repository should be trusted.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        public SwitchParameter Trusted
        {
            get
            { return _trusted; }

            set
            {
                _trusted = value;
                isSet = true;
            }
        }
        private SwitchParameter _trusted;
        private bool isSet;

        /// <summary>
        /// Specifies the priority ranking of the repository, such that repositories with higher ranking priority are searched
        /// before a lower ranking priority one, when searching for a repository item across multiple registered repositories.
        /// Valid priority values range from 0 to 100, such that a lower numeric value (i.e 10) corresponds
        /// to a higher priority ranking than a higher numeric value (i.e 40).
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        [ValidateNotNullOrEmpty]
        [ValidateRange(0, 100)]
        public int Priority { get; set; } = DefaultPriority;

        /// <summary>
        /// Specifies the Api version of the repository to be set.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        [ValidateSet("V2", "V3", "Local", "NugetServer", "ContainerRegistry")]
        public PSRepositoryInfo.APIVersion ApiVersion { get; set; }

        /// <summary>
        /// Specifies vault and secret names as PSCredentialInfo for the repository.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet)]
        public PSCredentialInfo CredentialInfo { get; set; }

        /// <summary>
        /// When specified, displays the successfully registered repository and its information.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        [Parameter(ParameterSetName = "Typosquatting")]
        [Parameter]
        public int DaysBack { get; set; }

        #endregion

        #region DynamicParameters

        public object GetDynamicParameters()
        {
            PSRepositoryInfo repository = RepositorySettings.Read(new[] { Name }, out string[] _).FirstOrDefault();
            // Dynamic parameter '-CredentialProvider' should not appear for PSGallery, or any container registry repository.
            // It should also not appear when using the 'Repositories' parameter set.
            if (repository is not null &&
                (repository.Name.Equals("PSGallery", StringComparison.OrdinalIgnoreCase) ||
                ParameterSetName.Equals(RepositoriesParameterSet) ||
                repository.IsContainerRegistry()))
            {
                return null;
            }

            _credentialProvider = new CredentialProviderDynamicParameters();
            return _credentialProvider;
        }

        #endregion

        #region Private methods

        protected override void BeginProcessing()
        {
            RepositorySettings.CheckRepositoryStore();
        }

        protected override void ProcessRecord()
        {
            if (ParameterSetName.Equals("Typosquatting"))
            {
                // Console.WriteLine($"in psset, with daysback: {-DaysBack}");

                int newDaysBack = -2;
                DateTime lastPublishedDateUtc = DateTime.UtcNow.AddDays(newDaysBack);
                string checkpoint = lastPublishedDateUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
                // Console.WriteLine($"checkpoint: {checkpoint}");

                string filter = $"{System.Uri.EscapeDataString($"Published gt datetime'{checkpoint}'")}";
                string inlineCount = $"allpages";
                bool includePrerelease = true;
                int downloadBatchSize = 100;
                int i = 0;

                List<string> responses = new List<string>();

                int skip = i;
                int top = downloadBatchSize;

                string requestUrlV2 = $"https://www.powershellgallery.com/api/v2/Search()?$filter={filter}&$inlinecount={inlineCount}&$skip={skip}&$top={top}&$orderby=Id+desc&includePrerelease={includePrerelease}";
                // Console.WriteLine(requestUrlV2);
                string response = HttpRequestCall(requestUrlV2, out ErrorRecord errRecord);
                responses.Add(response);
                int initialCount = GetCountFromResponse(response, out errRecord);  // count = 4
                Console.WriteLine($"Initial count from response: {initialCount}");


                // // If count is 0, early out as this means no packages matching search criteria were found
                if (initialCount == 0)
                {
                    return;
                }

                int count = (int)Math.Ceiling((double)(initialCount / 100));
                // if more than 100 count, loop and add response to list
                while (count > 0)
                {
                    // Console.WriteLine($"Count is '{count}'");
                    // skip 100
                    skip += downloadBatchSize;
                    requestUrlV2 = $"https://www.powershellgallery.com/api/v2/Search()?$filter={filter}&$inlinecount={inlineCount}&$skip={skip}&$top={top}&$orderby=Id+desc&includePrerelease={includePrerelease}";
                    response = HttpRequestCall(requestUrlV2, out errRecord);
                    if (errRecord != null)
                    {
                        Console.WriteLine($"Error in HTTP request: {errRecord.Exception.Message}");
                    }

                    responses.Add(response);
                    count--;
                }

                // process responses
                int skippedPkgs = 0;
                int totalPkgs = 0;
                List<Dictionary<string, string>> foundPkgsDictList = new List<Dictionary<string, string>>();
                foreach (string currentResponse in responses)
                {
                    Console.WriteLine("Processing response...");
                    // todo: get name, version, owners
                    var foundPkgsDictArray = ConvertResponseToXML(currentResponse, out int currentSkippedPkgs, out int currentTotalPkgs);
                    foundPkgsDictList.AddRange(foundPkgsDictArray);
                    skippedPkgs += currentSkippedPkgs;
                    totalPkgs += currentTotalPkgs;
                }

                Console.WriteLine($"Total packages skipped/processed: {skippedPkgs}/{totalPkgs}");
                foreach (var pkg in foundPkgsDictList)
                {
                    Console.WriteLine($"Package found: Name='{pkg["Name"]}', Version='{pkg["Version"]}', Owners='{pkg["Owners"]}'");
                }

                return;
            }




            // determine if either 1 of 5 values are attempting to be set: Uri, Priority, Trusted, APIVersion, CredentialInfo.
            // if none are (i.e only Name parameter was provided, write error)
            if (ParameterSetName.Equals(NameParameterSet) &&
                !MyInvocation.BoundParameters.ContainsKey(nameof(Uri)) &&
                !MyInvocation.BoundParameters.ContainsKey(nameof(Priority)) &&
                !MyInvocation.BoundParameters.ContainsKey(nameof(Trusted)) &&
                !MyInvocation.BoundParameters.ContainsKey(nameof(ApiVersion)) &&
                !MyInvocation.BoundParameters.ContainsKey(nameof(CredentialInfo)) &&
                !MyInvocation.BoundParameters.ContainsKey(nameof(CredentialProvider)))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new ArgumentException("Must set Uri, Priority, Trusted, ApiVersion, CredentialInfo, or CredentialProvider parameter"),
                    "SetRepositoryParameterBindingFailure",
                    ErrorCategory.InvalidArgument,
                    this));
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(Uri)))
            {
                if (!Utils.TryCreateValidUri(Uri, this, out _uri, out ErrorRecord errorRecord))
                {
                    ThrowTerminatingError(errorRecord);
                }
            }

            PSRepositoryInfo.APIVersion? repoApiVersion = null;
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ApiVersion)))
            {
                repoApiVersion = ApiVersion;
            }

            PSRepositoryInfo.CredentialProviderType? credentialProvider = _credentialProvider?.CredentialProvider;

            List<PSRepositoryInfo> items = new List<PSRepositoryInfo>();

            switch(ParameterSetName)
            {
                case NameParameterSet:
                    try
                    {
                        items.Add(RepositorySettings.UpdateRepositoryStore(Name,
                            _uri,
                            Priority,
                            Trusted,
                            isSet,
                            DefaultPriority,
                            repoApiVersion,
                            CredentialInfo,
                            credentialProvider,
                            this,
                            out string errorMsg));

                        if (!string.IsNullOrEmpty(errorMsg))
                        {
                            ThrowTerminatingError(new ErrorRecord(
                                new PSInvalidOperationException(errorMsg),
                                "ErrorInNameParameterSet",
                                ErrorCategory.InvalidArgument,
                                this));
                        }
                    }
                    catch (Exception e)
                    {
                        ThrowTerminatingError(new ErrorRecord(
                            new PSInvalidOperationException(e.Message),
                            "ErrorInNameParameterSet",
                            ErrorCategory.InvalidArgument,
                            this));
                    }
                    break;

                case RepositoriesParameterSet:
                    try
                    {
                        items = RepositoriesParameterSetHelper();
                    }
                    catch (Exception e)
                    {
                        ThrowTerminatingError(new ErrorRecord(
                            new PSInvalidOperationException(e.Message),
                            "ErrorInRepositoriesParameterSet",
                            ErrorCategory.InvalidArgument,
                            this));
                    }
                    break;

                default:
                    Dbg.Assert(false, "Invalid parameter set");
                    break;
            }

            if (PassThru)
            {
                foreach(PSRepositoryInfo item in items)
                {
                    WriteObject(item);
                }
            }
        }

        private List<PSRepositoryInfo> RepositoriesParameterSetHelper()
        {
            WriteDebug("In SetPSResourceRepository::RepositoriesParameterSetHelper()");
            List<PSRepositoryInfo> reposUpdatedFromHashtable = new List<PSRepositoryInfo>();
            foreach (Hashtable repo in Repository)
            {
                if (!repo.ContainsKey("Name") || repo["Name"] == null || String.IsNullOrEmpty(repo["Name"].ToString()))
                {
                    WriteError(new ErrorRecord(
                        new PSInvalidOperationException("Repository hashtable must contain Name key value pair"),
                        "NullNameForRepositoriesParameterSetRepo",
                        ErrorCategory.InvalidArgument,
                        this));

                    continue;
                }

                PSRepositoryInfo parsedRepoAdded = RepoValidationHelper(repo);
                if (parsedRepoAdded != null)
                {
                    reposUpdatedFromHashtable.Add(parsedRepoAdded);
                }
            }
            return reposUpdatedFromHashtable;
        }

        private PSRepositoryInfo RepoValidationHelper(Hashtable repo)
        {
            WriteDebug("In SetPSResourceRepository::RepoValidationHelper()");
            WriteDebug($"Parsing through repository '{repo["Name"]}'");

            Uri repoUri = null;
            if (repo.ContainsKey("Uri"))
            {
                if (String.IsNullOrEmpty(repo["Uri"].ToString()))
                {
                    WriteError(new ErrorRecord(
                        new PSInvalidOperationException("Repository Uri cannot be null if provided"),
                        "NullUriForRepositoriesParameterSetUpdate",
                        ErrorCategory.InvalidArgument,
                        this));

                    return null;
                }

                if (!Utils.TryCreateValidUri(uriString: repo["Uri"].ToString(),
                    cmdletPassedIn: this,
                    uriResult: out repoUri,
                    errorRecord: out ErrorRecord errorRecord))
                {
                    WriteError(errorRecord);

                    return null;
                }
            }

            bool repoTrusted = false;
            isSet = false;
            if (repo.ContainsKey("Trusted"))
            {
                repoTrusted = (bool) repo["Trusted"];
                isSet = true;
            }

            PSCredentialInfo repoCredentialInfo = null;
            if (repo.ContainsKey("CredentialInfo") &&
                !Utils.TryCreateValidPSCredentialInfo(credentialInfoCandidate: (PSObject) repo["CredentialInfo"],
                    cmdletPassedIn: this,
                    repoCredentialInfo: out repoCredentialInfo,
                    errorRecord: out ErrorRecord errorRecord1))
            {
                WriteError(errorRecord1);

                return null;
            }

            PSRepositoryInfo.CredentialProviderType? credentialProvider = _credentialProvider?.CredentialProvider;

            try
            {
                var updatedRepo = RepositorySettings.UpdateRepositoryStore(repo["Name"].ToString(),
                    repoUri,
                    repo.ContainsKey("Priority") ? Convert.ToInt32(repo["Priority"].ToString()) : DefaultPriority,
                    repoTrusted,
                    isSet,
                    DefaultPriority,
                    ApiVersion,
                    repoCredentialInfo,
                    credentialProvider,
                    this,
                    out string errorMsg);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    WriteError(new ErrorRecord(
                        new PSInvalidOperationException(errorMsg),
                        "ErrorSettingRepository",
                        ErrorCategory.InvalidData,
                        this));
                }

                return updatedRepo;
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(
                    new PSInvalidOperationException(e.Message),
                    "ErrorSettingIndividualRepoFromRepositories",
                    ErrorCategory.InvalidArgument,
                    this));

                return null;
            }
        }

        private string HttpRequestCall(string requestUrlV2, out ErrorRecord errRecord)
        {
            // Console.WriteLine("In V2ServerAPICalls::HttpRequestCall()");
            errRecord = null;
            string response = string.Empty;

            HttpClientHandler handler = new HttpClientHandler();
            handler.Credentials = null;
            HttpClient sessionClient = new HttpClient(handler);
            sessionClient.Timeout = TimeSpan.FromMinutes(10);

            try
            {
                // Console.WriteLine($"Request url is '{requestUrlV2}'");
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrlV2);

                response = SendV2RequestAsync(request, sessionClient).GetAwaiter().GetResult();
            }
            catch (ResourceNotFoundException e)
            {
                errRecord = new ErrorRecord(
                    exception: e,
                    "ResourceNotFound",
                    ErrorCategory.InvalidResult,
                    this);
            }
            catch (UnauthorizedException e)
            {
                errRecord = new ErrorRecord(
                    exception: e,
                    "UnauthorizedRequest",
                    ErrorCategory.InvalidResult,
                    this);
            }
            catch (HttpRequestException e)
            {
                errRecord = new ErrorRecord(
                    exception: e,
                    "HttpRequestCallFailure",
                    ErrorCategory.ConnectionError,
                    this);
            }
            catch (Exception e)
            {
                errRecord = new ErrorRecord(
                    exception: e,
                    "HttpRequestCallFailure",
                    ErrorCategory.ConnectionError,
                    this);
            }

            if (string.IsNullOrEmpty(response))
            {
                // Console.WriteLine("Response is empty");
            }

            return response;
        }

        public static async Task<string> SendV2RequestAsync(HttpRequestMessage message, HttpClient s_client)
        {
            HttpStatusCode responseStatusCode = HttpStatusCode.OK;
            try
            {
                HttpResponseMessage response = await s_client.SendAsync(message);
                responseStatusCode = response.StatusCode;
                response.EnsureSuccessStatusCode();

                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (HttpRequestException e)
            {
                if (responseStatusCode.Equals(HttpStatusCode.NotFound))
                {
                    throw new ResourceNotFoundException(Utils.FormatRequestsExceptions(e, message));
                }
                // ADO feed will return a 401 if a package does not exist on the feed, with the following message:
                // 401 (Unauthorized - No local versions of package 'NonExistentModule'; please provide authentication to access
                // versions from upstream that have not yet been saved to your feed. (DevOps Activity ID: 5E5CF528-5B3D-481D-95B5-5DDB5476D7EF))
                if (responseStatusCode.Equals(HttpStatusCode.Unauthorized))
                {
                    if (e.Message.Contains("access versions from upstream that have not yet been saved to your feed"))
                    {
                        throw new ResourceNotFoundException(Utils.FormatRequestsExceptions(e, message));
                    }

                    throw new UnauthorizedException(Utils.FormatCredentialRequestExceptions(e));
                }

                throw new HttpRequestException(Utils.FormatRequestsExceptions(e, message));
            }
            catch (ArgumentNullException e)
            {
                throw new ArgumentNullException(Utils.FormatRequestsExceptions(e, message));
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException(Utils.FormatRequestsExceptions(e, message));
            }
        }

        public int GetCountFromResponse(string httpResponse, out ErrorRecord errRecord)
        {
            errRecord = null;
            int count = 0;

            //Create the XmlDocument.
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.LoadXml(httpResponse);

                bool countSearchSucceeded = false;
                XmlNodeList elemList = doc.GetElementsByTagName("m:count");
                if (elemList.Count > 0)
                {
                    countSearchSucceeded = true;
                    XmlNode node = elemList[0];
                    if (node == null || String.IsNullOrWhiteSpace(node.InnerText))
                    {
                        countSearchSucceeded = false;
                        errRecord = new ErrorRecord(
                            new PSArgumentException("Count property from server response was empty, invalid or not present."),
                            "GetCountFromResponseFailure",
                            ErrorCategory.InvalidData,
                            this);
                    }
                    else
                    {
                        countSearchSucceeded = int.TryParse(node.InnerText, out count);
                    }
                }

                if (!countSearchSucceeded)
                {
                             // Note: not all V2 servers may have the 'count' property implemented or valid (i.e CloudSmith server), in this case try to get 'd:Id' property.
                    elemList = doc.GetElementsByTagName("d:Id");
                    if (elemList.Count > 0)
                    {
                        count = elemList.Count;
                        errRecord = null;
                    }
                    else
                    {
                        Console.WriteLine($"Property 'count' and 'd:Id' could not be found in response. This may indicate that the package could not be found");
                    }
                }
            }
            catch (XmlException e)
            {
                errRecord = new ErrorRecord(
                    exception: e,
                    "GetCountFromResponse",
                    ErrorCategory.InvalidData,
                    this);
            }

            return count;
        }

        public Dictionary<string, string>[] ConvertResponseToXML(string httpResponse, out int skippedPkgs, out int totalPkgs) {
            skippedPkgs = 0;
            totalPkgs = 0;

            //Create the XmlDocument.
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(httpResponse);

            XmlNodeList entryNode = doc.GetElementsByTagName("entry");

            List<Dictionary<string, string>> packagesFound = new List<Dictionary<string, string>>();
            for (int i = 0; i < entryNode.Count; i++)
            {
                XmlNode node = entryNode[i];
                totalPkgs++;
                string packageName = "";
                string packageVersion = "";
                string[] owners = new string[] {};
                var entryChildNodes = node.ChildNodes;
                foreach (XmlElement childNode in entryChildNodes)
                {
                    var entryKey = childNode.LocalName;
                    if (entryKey.Equals("properties"))
                    {
                        var propertyChildNodes = childNode.ChildNodes;
                        foreach (XmlElement propertyChild in propertyChildNodes)
                        {
                            var propertyKey = propertyChild.LocalName;
                            var propertyValue = propertyChild.InnerText;
                            if (propertyKey.Equals("NormalizedVersion"))
                            {
                                packageVersion = propertyValue;
                            }
                            else if (propertyKey.Equals("Id"))
                            {
                                packageName = propertyValue;
                            }
                            else if (propertyKey.Equals("Owners"))
                            {
                                owners = propertyValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            }
                        }

                        if (owners.Contains("aws-dotnet-sdk-team") ||
                            owners.Contains("azure-sdk") ||
                            owners.Contains("oci-psmodules-grp"))
                        {
                            // Console.WriteLine($"Skipping package {packageName} owned by owner {string.Join(" ", owners)}.");
                            skippedPkgs++;
                            break;
                        }

                        Dictionary<string, string> packageInfo = new Dictionary<string, string>
                        {
                            { "Name", packageName },
                            { "Version", packageVersion },
                            { "Owners", string.Join(" ", owners) }
                        };

                        packagesFound.Add(packageInfo);

                        break; // don't care about rest of the childNode's keys
                    }
                }

                // Console.WriteLine($"Package found: Name='{packageName}', Version='{packageVersion}', Owners='{string.Join(" ", owners)}'");
            }

            return packagesFound.ToArray();
        }

        #endregion
    }
}
