# Windows Azure Media Service Video Uploader
Upload .mp4 videos to windows azure media service (WAMS) for asset management, streaming downloading.

## Prerequisites

+ You have a Windows Azure Media Service Account either in Global Instance or China Mooncake.
+ Get the WAMS account, **account name** and **account key**.
+ Update the *App.config* file by adding the **account name** and **account key**

```XML
  <appSettings>
    <add key="MediaServiceAccountName" value="" />
    <add key="MediaServiceAccountKey" value="" />
    
    <add key="ClientSettingsProvider.ServiceUri" value="" />
    <add key="IsChinaAccount" value="true"/>
  </appSettings>
```

## Features
This tool support both Global Azure subscription and China Mooncake subscription, it helps you to upload .mp4 videos and .vtt closed captions to the WAMS account, encode the asset, publish asset for usage.  

+ Upload .mp4 files.
+ Upload .vtt files.
+ Generate .csv file for tracking the uploaded assets.

## Screenshot

![windows media service video uploader screenshot](https://github.com/7788wangzi/WindowsAzureMediaService_VideoUploader/blob/master/WAMS_UPloader_Screen.PNG)