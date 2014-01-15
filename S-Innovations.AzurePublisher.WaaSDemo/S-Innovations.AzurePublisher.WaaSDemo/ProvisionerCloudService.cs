﻿using Newtonsoft.Json;
using SInnovations.WindowsAzure.Publisher.Plugins;
using SInnovations.WindowsAzure.Publisher.Plugins.Attributes;
using SInnovations.WindowsAzure.Publisher.Plugins.Inputs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SInnovations.AzurePublisher.WaaSDemo
{
    public class BlobStorageConnectionString : WindowsAzureBlobStorageCredentional
    {

        [DeploymentSettingsConverterAttribute(SettingName = "Composite.WindowsAzure.WebRole.Storage.ConnectionString")]
        public override void ParseDeploymentSettings(string settingValue, Action<string, object> parser)
        {
            base.ParseDeploymentSettings(settingValue, parser);

        }
        [DeploymentSetting(SettingName = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", IgnoreWhenParsing = true)]
        [DeploymentSetting(SettingName = "Composite.WindowsAzure.WebRole.Storage.ConnectionString")]
        public override string ConnectionString()
        {
            return base.ConnectionString();

        }
    }
    public class DeploymentPackage
    {
        public string PackageUri { get; set; }
        public string ServiceName { get; set; }
        public string Name { get; set; }
    }

    [DisplayName("Provisioner Backend")]
    public class ProvisionerCloudService : ICloudServiceDeploymentModel
    {

        public async Task<Uri> GetPackageUriAsync(XDocument configuration, string size = "ExtraSmall")
        {

            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //Do not send values over the wire
            var clone = new XDocument(configuration);
            foreach (var el in clone.Descendants().Where(e => e.Name.LocalName.Equals("Setting")))
                el.SetAttributeValue("value", "");

            //Post a task to find the uri for this configuration settings
            ActionOfStreamContent content = new ActionOfStreamContent((stream) => clone.Save(stream));
            content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");

            HttpResponseMessage response = await client.PostAsync(
                string.Format("http://hq.s-innovations.net/api/cwaw/package/{0}/{1}", "provisioner", size), content);

            response.EnsureSuccessStatusCode();
            var package = JsonConvert.DeserializeObject<DeploymentPackage>(response.Content.ReadAsStringAsync().Result);

            //Upate the document to containe the correct service name and role name.
            XNamespace ns = "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration";
            configuration.Root.SetAttributeValue("serviceName", package.ServiceName);
            var role = configuration.Root.Element(ns + "Role");
            role.SetAttributeValue("name", package.Name);

            return new Uri(package.PackageUri);
        }

        public void ManipulateConfiguration(XDocument configuration)
        {
         
        }
        
        [DeploymentSetting(SettingName = "Microsoft.WindowsAzure.Plugins.RemoteDebugger.Connector.Enabled", IgnoreWhenParsing = true)]
        public bool RemoteDebugger()
        {
            return true;
        }



        [Required]
        [Display(Name = "SubscriptionManagementCertificate", Order = 1)]
        [DeploymentSetting(SettingName = "Composite.WindowsAzure.OpenCMS.Thumbprint", IgnoreWhenParsing = true)]
        [CloudServiceCertificateAttribute(Name = "Composite.WindowsAzure.OpenCMS.Subscription", IsManagementCertificate=true)]
        public string SubscriptionManagementCertificate { get; set; }



        [Required]
        [DeploymentSetting(SettingName = "Microsoft.WindowsAzure.Plugins.RemoteDebugger.CertificateThumbprint", IgnoreWhenParsing = true)]
        [Display(Name = "RemoteDebuggerCertificate", Order = 2)]
        [CloudServiceCertificateAttribute(Name = "Microsoft.WindowsAzure.Plugins.RemoteDebugger.TransportValidation", IsDeploymentCertificate=true)]
        public string RemoteDebuggerCertificate { get; set; }



        [Required]
        [Display(Name = "Blob Storage Connection String", Order = 3, Description="The blob storage that will host all deployments and websites")]
        public BlobStorageConnectionString ConnectionString { get; set; }




        [DeploymentSetting(SettingName = "DnsMadeEasySecret")]
        [Display(Name = "DnsMadeEasySecret", Order = 5)]
        [Required]
        public string DnsMadeEasySecret { get; set; }




        [DeploymentSetting(SettingName = "DnsMadeEasyKey")]
        [Display(Name = "DnsMadeEasyKey", Order = 5)]
        [Required]
        public string test { get; set; }




        [WindowsAzureSubscription]
        [DeploymentSetting(SettingName = "Composite.WindowsAzure.OpenCMS.SubscriptionId")]
        [Display(Name = "SubscriptionId", Order = 6, Description="This is the subscription that it will spin up new webroles on")]
        [Required]
        public string SubscriptionId { get; set; }




        [ServiceBusConnectionString]
        [DeploymentSetting(SettingName = "Microsoft.ServiceBus.ConnectionString")]
        [Display(Name = "ServiceBus ConnectionString", Order = 7,Description="The servicebus to listen for messages on")]
        [Required]
        public string ServiceBusConnectionString { get; set; }

    }
}
