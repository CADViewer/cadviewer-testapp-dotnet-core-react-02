# cadviewer-testapp-dotnet-core-react-02
CADViewer 7 running under Asp .NET 6 core 

The repository contains a full setup of CADViewer with CAD Converters and script controllers for Asp .Net 6 Core with a React front-end.

## This package contains

1: CADViewer script library  - npm installed into ClientApp as part of React JS front-end.

2: AutoXchange AX2023 Converter - in their preferred folder structure

3: All structures for file-conversion, sample drawings, redlines, etc. 

4: ClientApp/src/index.js invokes helper script documents with ***CADViewer.js*** (CADViewer canvas initialization), script document with helper methods for testing the API (***CADViewerHelperMethods.js***).

4a:  If needed, edit ClientApp/src/index.js to either omit or include: ***CADViewerHelperMethods.js***.

5: The folder structure for dotNet core script handlers for communication between CADViewer and the back-end AutoXchange 2023. The controlling scripts for load, save and conversion of CAD files are implemented in:  ***CADViewerController.cs***.


## This package does not contain

6: The converter folder structure contains a larger set of fonts, installed in /cadviewer/converters/ax2023/windows/fonts/, but a fuller set of fonts can be installed. 

Read the sections on installing and handling [Fonts](https://tailormade.com/ax2020techdocs/installation/fonts/) in [AutoXchange 2023 TechDocs](https://tailormade.com/ax2020techdocs/) and [TroubleShooting](https://tailormade.com/ax2020techdocs/troubleshooting/).


## How to Use

Once installed, open cadviewer.sln in Visual Studio.  The sample can be run from a web-browser. Use http://localhost:xxxxx/ as a starting point (assuming that your have installed under http://localhost:xxxxx).

7: Open ***appsettings.json**  , edit all parameters to correspond to your server settings, executable, and Url of the application. 

    "CADViewer": {
      "ServerLocation": "c:/cadviewer-testapp-dotnet-core-react-02/cadviewer/wwwroot/",
      "ServerUrl": "https://localhost:44476/",
      "fileLocation": "c:/cadviewer-testapp-dotnet-core-react-02/cadviewer/wwwroot/converters/files/",
      "fileLocationUrl": "https://localhost:44325/converters/files/",
      "converterLocation": "c:/cadviewer-testapp-dotnet-core-react-02/cadviewer/wwwroot/converters/ax2023/windows/",
      "ax2020_executable": "AX2023_W64_23_05_87.exe",
      "cvjs_debug": true,
      "licenseLocation": "c:/cadviewer-testapp-dotnet-core-react-02/cadviewer/wwwroot/converters/ax2023/windows/",
      "xpathLocation": "c:/cadviewer-testapp-dotnet-core-react-02/cadviewer/wwwroot/converters/ax2023/windows/",
      "callbackMethod": "/CADViewer/getFile",
      "MailServer": "smtp.dreamhost.com",
      "MailServerPort": 465,
      "MailUserName": "testing@cadviewer.org",
      "MailPassword": "xx"
    }

8: Open /ClientApp/src/  CADViewer.js and CADViewerHelperMethods.js and edit any Url definitions so they correspond to the server settings.  

    var ServerBackEndUrl = "https://localhost:44476/";
    var ServerUrl = "https://localhost:44476/";




## General Documentation 

-   [CADViewer Techdocs and Installation Guide](https://cadviewer.com/cadviewertechdocs/download)



## Updating CAD Converters

This repository should contain the latest converters, but in case you need to update any of the back-end converters please follow: 

* [Download **AutoXchange**](/download/) (and other converters), install (unzip) AX2023 in **cadviewer/converters/ax2023/windows** or **cadviewer/converters/ax2023/linux** or in the designated folder structure.

* Read the sections on installing and handling [Fonts](https://tailormade.com/ax2020techdocs/installation/fonts/) in [AutoXchange 2023 TechDocs](https://tailormade.com/ax2020techdocs/) and [TroubleShooting](https://tailormade.com/ax2020techdocs/troubleshooting/).

* Try out the samples and build your own application!
 
