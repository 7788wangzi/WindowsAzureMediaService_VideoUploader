using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace WAMS_UPLOAD
{
    public partial class Form1 : Form
    {
        public delegate void delegateHandleMP4AndVTTInFolders(List<string> folders);
        public delegate void delegateUpdateMessageInUIControls(string msg);

        //No need to encode to multi bitrate for VTTs, Let user choose handle MP4 or VTT
        public bool userChoice_HandleVTT = false; // VTT no encode needed
        public Form1()
        {
            InitializeComponent();
            textBox1.Text = @"E:\CodeLibrary\2018\WAMS_UPLOAD\Video\Test-demo.mp4";
        }

        private void StartProcessing(List<string> folders)
        {
            if (folders != null)
            {
                foreach (string folder in folders)
                {
                    //Each folder will generate one CSV file to highlight the asset information
                    StringBuilder assetInfo = new StringBuilder();
                    string currentFolderPath = (string)folder;
                    string assetInfoFileName = Guid.NewGuid().ToString() + ".csv";

                    FIO.IoHelper ioHelper = new FIO.IoHelper();
                    
                    List<string> vttMp4Files = ioHelper.FindFiles(currentFolderPath, new List<string>() { ".mp4",".vtt" });
                    foreach (string file in vttMp4Files)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        string outputFileName = ValidityCheck.FormatFileName(fileName);

                        //If changed file name to a valid file name, rename the file with new file name
                        if(outputFileName!=fileName)
                        {
                            string newFile = file.Replace(fileName, outputFileName);
                            if (File.Exists(newFile))
                                File.Delete(newFile);

                            File.Move(file, newFile);
                        }
                    }

                    //Files count that handled successfully
                    int totalCount = 0;
                    if (!userChoice_HandleVTT)
                    {
                        List<string> mp4Files = ioHelper.FindFiles(currentFolderPath, new List<string>() { ".mp4" });
                        foreach (var mp4File in mp4Files)
                        {

                            totalCount++;

                            // Get the Asset File Name
                            assetInfo.AppendLine();

                            //Updating Status Message
                            UpdateMessageInStatusLabel($"Uploading {mp4File}");

                            var videoAsset = MediaHelper.CreateAssetAndUploadSingleFile(mp4File, Microsoft.WindowsAzure.MediaServices.Client.AssetCreationOptions.None);
                            assetInfo.Append(videoAsset.Name);

                            //Encoding Status Message
                            UpdateMessageInStatusLabel($"Encoding {mp4File}");

                            var encodedAsset = MediaHelper.EncodeToAdaptiveBitrateMP4s(videoAsset, Microsoft.WindowsAzure.MediaServices.Client.AssetCreationOptions.None);

                            //Publishing Status Message
                            UpdateMessageInStatusLabel($"Publishing {mp4File}");

                            List<string> mp4URLs = null;
                            string playerUrl = MediaHelper.PublishAssetAndGetURLs(encodedAsset, out mp4URLs);

                            if (mp4URLs.Count > 0)
                            {
                                assetInfo.Append("," + encodedAsset.Name);
                                foreach (var mp4Url in mp4URLs)
                                {
                                    assetInfo.Append("," + mp4Url);
                                }
                            }
                        }
                    }
                    else
                    {
                        List<string> vttFiles = ioHelper.FindFiles(currentFolderPath, new List<string>() { ".vtt" });
                        foreach(var vttFile in vttFiles)
                        {                           

                            totalCount++;
                            assetInfo.AppendLine();

                            //Updating Status Message
                            UpdateMessageInStatusLabel($"Uploading {vttFile}");

                            var vttAsset = MediaHelper.CreateAssetAndUploadSingleFile(vttFile, Microsoft.WindowsAzure.MediaServices.Client.AssetCreationOptions.None);
                            assetInfo.Append(vttAsset.Name);

                            //publishing status message
                            UpdateMessageInStatusLabel($"Publishing {vttFile}");
                            List<string> vttURLs = null;
                            string playerUrl = MediaHelper.PublishAssetAndGetURLs(vttAsset, out vttURLs);

                            if(vttURLs.Count>0)
                            {
                                assetInfo.Append($",{vttAsset.Name}");
                                foreach (var vttUrl in vttURLs)
                                {
                                    assetInfo.Append($",{vttUrl}");
                                }
                            }
                        }
                    }

                    string assetInfoFilePath = Path.Combine(currentFolderPath, assetInfoFileName);
                    //File.Create(resultFilepath);
                    using (StreamWriter writer = new StreamWriter(assetInfoFilePath))
                    {
                        writer.WriteLine(assetInfo.ToString());
                        writer.Flush();
                    }

                    string message = $"Complete {totalCount} Files uploading in {currentFolderPath}";
                    UpdateMessageInListBox(message);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            label1.Text = "";
            listBox2.Items.Clear();

            List<string> inputFolders = new List<string>();
            foreach (var item in listBox1.Items)
            {
                inputFolders.Add((string)item);
            }

            delegateHandleMP4AndVTTInFolders dprocess = new delegateHandleMP4AndVTTInFolders(StartProcessing);
            dprocess.BeginInvoke(inputFolders, new AsyncCallback(ProcessComplete), null);

            UpdateMessageInListBox("Start.");      

        }

        private void ProcessComplete(IAsyncResult ar)
        {
            string message = "Done";
            UpdateMessageInListBox(message);
            StringBuilder sbLog = new StringBuilder();
            foreach (var item in listBox2.Items)
            {
                sbLog.AppendLine((string)item);
            }

            string LogFilename = GetTimeStamp.ToString("Log")+".txt";
            using (StreamWriter writer = new StreamWriter(LogFilename))
            {
                writer.Write(sbLog);
                writer.Flush();
            }
        }

        private void UpdateMessageInListBox(string message)
        {
            string formatMessage = string.Format("{0:d/M/yyyy HH:mm:ss} {1}",DateTime.Now, message);
            this.Invoke(new MethodInvoker(delegate ()
            {
                listBox2.Items.Add(formatMessage);
            }));
        }

        private void UpdateMessageInStatusLabel(string message)
        {
            string formatMessage = string.Format("{0:d/M/yyyy HH:mm:ss} {1}", DateTime.Now, message);
            this.Invoke(new MethodInvoker(delegate ()
            {
                label1.Text = formatMessage;
            }));
        }

        private void btn_Add_Click(object sender, EventArgs e)
        {           
            string folder = textBox1.Text.Trim();
            if (Directory.Exists(folder))
            {
                listBox1.Items.Add(folder);
                textBox1.Text = "";
                label1.Text = string.Format("Added: {0}", folder);
            }
            else
            {
                MessageBox.Show("Not a valid folder");
                textBox1.Text = "";
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            userChoice_HandleVTT = radioButton2.Checked;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            userChoice_HandleVTT = !radioButton1.Checked;
        }
    }
}
