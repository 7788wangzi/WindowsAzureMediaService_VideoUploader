using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace WAMS_UPLOAD
{
    public class MediaHelper
    {
        static CloudMediaContext _context = null;

        static readonly string isChinaAccount = ConfigurationManager.AppSettings["IsChinaAccount"];
        static readonly string _mediaServiceAccountName = ConfigurationManager.AppSettings["MediaServiceAccountName"];
        static readonly string _mediaServiceAccountKey = ConfigurationManager.AppSettings["MediaServiceAccountKey"];
        static readonly string _defaultScope = "urn:WindowsAzureMediaServices";
        static readonly string _chinaApiServerUrl = "https://wamsshaclus001rest-hs.chinacloudapp.cn/API/";
        static readonly string _chinaAcsBaseAdressUrl = "https://wamsprodglobal001acs.accesscontrol.chinacloudapi.cn";
        static readonly Uri _apiServer = new Uri (_chinaApiServerUrl);

        static MediaServicesCredentials _cachedCredentials = null;
        /// <summary>
        /// cache the _context object in memory
        /// </summary>
        public static void InitContext()
        {
            if (isChinaAccount.ToLower() == "true")
            {
                // China account need more parameters
                if (_cachedCredentials == null)
                {
                    _cachedCredentials = new MediaServicesCredentials(
                                    _mediaServiceAccountName,
                                    _mediaServiceAccountKey,
                                    _defaultScope,
                                    _chinaAcsBaseAdressUrl);
                }

                _context = new CloudMediaContext(_apiServer, _cachedCredentials);
            }
            else
            {
                if (_cachedCredentials == null)
                {
                    _cachedCredentials = new MediaServicesCredentials(
                                    _mediaServiceAccountName,
                                    _mediaServiceAccountKey);
                }

                _context = new CloudMediaContext(_cachedCredentials);
            }
        }

        public static IAsset CreateAssetAndUploadSingleFile(string singleFilePath, AssetCreationOptions assetCreationOptions)
        {
            if(!File.Exists(singleFilePath))
            {
                return null;
            }

            //Get original File Name
            var assetName = Path.GetFileNameWithoutExtension(singleFilePath);
            //Format file name with adding timestamp
            string uniqueAssetName = GetTimeStamp.ToString(assetName);
            if (_context == null)
                InitContext();

            IAsset inputAsset = null;
            try
            {
                inputAsset = _context.Assets.Create(uniqueAssetName, assetCreationOptions);
                // add asset files to this asset
                var assetFile = inputAsset.AssetFiles.Create(Path.GetFileName(singleFilePath));
                assetFile.Upload(singleFilePath);
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return inputAsset;
        }

        public static IAsset EncodeToAdaptiveBitrateMP4s(IAsset asset, AssetCreationOptions assetCreationOptions)
        {
            if (_context == null)
                InitContext();

            IJob job = _context.Jobs.Create(asset.Name+"-MES_H264_Multiple_Bitrate_720p");

            //get the standard media processor
            var processor = _context.MediaProcessors.Where(p => p.Name == "Media Encoder Standard").ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            ITask task = job.Tasks.AddNew("My encoding task",
                processor,
                "H264 Multiple Bitrate 720p",
                TaskOptions.None);

            task.InputAssets.Add(asset);
            task.OutputAssets.AddNew(GetEncodedAssetNameByAssetName(asset.Name), AssetCreationOptions.None);

            job.Submit();
            job.GetExecutionProgressTask(System.Threading.CancellationToken.None).Wait();

            return job.OutputMediaAssets[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="assetsURLs">If outputURLs.Count >= 2, then outputURLs[0] is the video url, outputURLs[1]...outputURLs[n-1] are the downloadable urls.</param>
        /// <returns></returns>
        public static string PublishAssetAndGetURLs(IAsset asset, out List<string> assetsURLs)
        {
            assetsURLs = new List<string>();
            if (_context == null)
                InitContext();

            //Expires in 10 years later
            var accessPolicy = _context.AccessPolicies.Create("Streaming policy", TimeSpan.FromDays(3650), AccessPermissions.Read);                                                           

            //Create a locator, take effect immediately by setting StartTime to 5 minutes ago.
            var originLocator =  _context.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, accessPolicy, DateTime.UtcNow.AddMinutes(-5));
            
            var theMainifestFileName = asset.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".ism")).FirstOrDefault();
            var theMP4FilesNameList = asset.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".mp4")).ToList();
            var theVTTFilesNameList = asset.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".vtt")).ToList();

            string mainifestPath = null;
            // Add the mainifestFileName
            if (theMainifestFileName != null)
            {
                mainifestPath = originLocator.Path + theMainifestFileName.Name + "/manifest";
                assetsURLs.Add(mainifestPath);
            }
            foreach (var video in theMP4FilesNameList)
            {
                assetsURLs.Add(originLocator.Path + video.Name);
            }
            foreach (var vtt in theVTTFilesNameList)
            {
                assetsURLs.Add(originLocator.Path + vtt.Name);
            }
            
            return mainifestPath;

        }

        public static IAsset GetAssetByName(string assetName)
        {
            if (_context == null)
                InitContext();

            var assetInfo = _context.Assets.Where(asset => asset.Name.ToLower() == assetName.ToLower()).FirstOrDefault();
            return assetInfo;
        }

        public static string GetEncodedAssetNameByAssetName(string assetName)
        {
            string encodedAssetName = assetName + " - H264 MBR 720p";
            return encodedAssetName;
        }
    
    }
}
