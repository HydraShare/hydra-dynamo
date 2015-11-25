using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.Nodes;
using Dynamo.UI.Commands;
using Dynamo.Wpf;
using SampleLibraryUI.Controls;
using Newtonsoft.Json;
using Dynamo.ViewModels;
using System.Linq;

namespace SampleLibraryUI.Examples
{
    /*
     * Hydra: A Plugin for example file sharing
     * This file is part of Hydra
     
     * Use this component to export your dyn file to your Hydra repository so that you can upload and share with the community!
     * Provided by Hydra 0.0.1

     * Args:
     *      fileName: A text name for your example file.
     *      fileDescription: A text description of your example file.  This can be a list and each item will be written as a new paragraph.
     *      versionNumber: A numerical input for the example file version.
     *      changeLog: A text description of the changes that you have made to the file if this is a new version of an old example file.
     *      fileTags: An optional list of test tags to decribe your example file.  This will help others search for your file easily.
     *      targetFolder: Input a file path here to the hydra folder on your machine (default Github structure places your hydra github repo in your documents folder)
     * Returns:
     *      Empty
    */

    // The NodeName attribute is what will display on to of the node in Dynamo
    [NodeName("HydraShare")]

    // The NodeCategory attribute determines how your node is organized in the library
    [NodeCategory("Hydra")]

    // The description will display in the tooltip
    [NodeDescription("Export Dynamo File to Hydra Repository")]

    // Add the IsDesignScriptCompatible attribute to ensure that it gets loaded in Dynamo.
    [IsDesignScriptCompatible]

    public class HydraShare : NodeModel
    {
        private string message;
        public Action RequestSave;


        #region properties

        /// <summary>
        /// A message that will appear on the button
        /// </summary>
        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                // Raise a property changed notification
                // to alert the UI that an element needs
                // an update.
                RaisePropertyChanged("NodeMessage");
            }
        }

        /// <summary>
        /// DelegateCommand objects allow you to bind
        /// UI interaction to methods on your data context.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public DelegateCommand MessageCommand { get; set; }

        #endregion

        #region constructor

        /// <summary>
        /// The constructor for a NodeModel is used to create
        /// the input and output ports and specify the argument
        /// lacing.
        /// </summary>
        public HydraShare()
        {
            //Defines all node inputs
            InPortData.Add(new PortData("fileName", "A text name for your example file"));
            InPortData.Add(new PortData("fileDescription", "A text description of your example file"));
            InPortData.Add(new PortData("versionNumber", "Enter a version number for your example file"));
            InPortData.Add(new PortData("changeLog", "A text description of changes made to file from an older version"));
            InPortData.Add(new PortData("fileTags", "An optional list of tags to describe your file"));
            InPortData.Add(new PortData("targetFolder", "File path to Hydra folder by defauly this is in your documents folder"));

            // This call is required to ensure that your ports are
            // properly created.
            RegisterAllPorts();

            // The arugment lacing is the way in which Dynamo handles
            // inputs of lists. If you don't want your node to
            // support argument lacing, you can set this to LacingStrategy.Disabled.
            ArgumentLacing = LacingStrategy.Disabled;

            // We create a DelegateCommand object which will be 
            // bound to our button in our custom UI. Clicking the button 
            // will call the ShowMessage method.
            MessageCommand = new DelegateCommand(ShowMessage, CanShowMessage);

            // Setting our property here will trigger a 
            // property change notification and the UI 
            // will be updated to reflect the new value.
            Message = "Share";
        }

        #endregion

        #region command methods

        private static bool CanShowMessage(object obj)
        {

            return true;
        }

        private void ShowMessage(object obj)
        {
            //Here is where the button command will be called, we can raise another event here
            //that will raise something inside NodeViewCustomization...
            this.RequestSave();
        }

        #endregion
    }

    /// <summary>
    ///     View customizer for CustomNodeModel Node Model.
    /// </summary>
    public class CustomNodeModelNodeViewCustomization : INodeViewCustomization<HydraShare>
    {
        /// <summary>
        /// At run-time, this method is called during the node 
        /// creation. Here you can create custom UI elements and
        /// add them to the node view, but we recommend designing
        /// your UI declaratively using xaml, and binding it to
        /// properties on this node as the DataContext.
        /// </summary>
        /// <param name="model">The NodeModel representing the node's core logic.</param>
        /// <param name="nodeView">The NodeView representing the node in the graph.</param>
        public void CustomizeView(HydraShare model, NodeView nodeView)
        {

            // The view variable is a reference to the node's view.
            // In the middle of the node is a grid called the InputGrid.
            // We reccommend putting your custom UI in this grid, as it has
            // been designed for this purpose.

            // Create an instance of our custom UI class (defined in xaml),
            // and put it into the input grid.
            var helloDynamoControl = new HelloDynamoControl();
            nodeView.inputGrid.Children.Add(helloDynamoControl);

            // Set the data context for our control to be this class.
            // Properties in this class which are data bound will raise 
            // property change notifications which will update the UI.
            helloDynamoControl.DataContext = model;
            model.RequestSave += () => exportToHydra(model, nodeView);
        }

        public void exportToHydra(NodeModel model, NodeView nodeView)
        {
            //Prevent running if any input ports are empty
            //Should be modified to skip port if recieves a null value
            if (model.InPorts.Any(x => x.Connectors.Count == 0))
            {
                return;
            }
            else
            {
                var graph = nodeView.ViewModel.DynamoViewModel.Model.CurrentWorkspace;

                //Get all node inputs as strings
                //fileName Input
                var fileNamenode = model.InPorts[0].Connectors[0].Start.Owner;
                var fileNameIndex = model.InPorts[0].Connectors[0].Start.Index;
                var fileNameId = fileNamenode.GetAstIdentifierForOutputIndex(fileNameIndex).Name;
                var fileNameMirror = nodeView.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(fileNameId);
                var fileName = fileNameMirror.GetData().Data as string;

                //fileDescription Input
                var fileDescriptionnode = model.InPorts[1].Connectors[0].Start.Owner;
                var fileDescriptionIndex = model.InPorts[1].Connectors[0].Start.Index;
                var fileDescriptionId = fileDescriptionnode.GetAstIdentifierForOutputIndex(fileDescriptionIndex).Name;
                var fileDescriptionMirror = nodeView.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(fileDescriptionId);
                var fileDescription = fileDescriptionMirror.GetData().Data as string;

                //versionNumber
                var versionNumbernode = model.InPorts[2].Connectors[0].Start.Owner;
                var versionNumberIndex = model.InPorts[2].Connectors[0].Start.Index;
                var versionNumberId = versionNumbernode.GetAstIdentifierForOutputIndex(versionNumberIndex).Name;
                var versionNumberMirror = nodeView.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(versionNumberId);
                var versionNumber = versionNumberMirror.GetData().Data as string;

                //changeLog
                var changeLognode = model.InPorts[3].Connectors[0].Start.Owner;
                var changeLogIndex = model.InPorts[3].Connectors[0].Start.Index;
                var changeLogId = changeLognode.GetAstIdentifierForOutputIndex(changeLogIndex).Name;
                var changeLogMirror = nodeView.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(changeLogId);
                var changeLog = changeLogMirror.GetData().Data as string;

                //fileTags (make sure contains Dynamo)
                var fileTagsnode = model.InPorts[4].Connectors[0].Start.Owner;
                var fileTagsIndex = model.InPorts[4].Connectors[0].Start.Index;
                var fileTagsId = fileTagsnode.GetAstIdentifierForOutputIndex(fileTagsIndex).Name;
                var fileTagsMirror = nodeView.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(fileTagsId);
                var fileTags = fileTagsMirror.GetData().Data as string;
                fileTags = fileTags.Replace(" ", "");
                List<string> fileTagList = fileTags.Split(',').ToList<string>();
                if (fileTagList.Contains("Dynamo") == false && fileTagList.Contains("dynamo") == false)
                {
                    fileTagList.Add("Dynamo");
                }

                //targetFolder Input
                var targetFoldernode = model.InPorts[5].Connectors[0].Start.Owner;
                var targetFolderIndex = model.InPorts[5].Connectors[0].Start.Index;
                var targetFolderId = targetFoldernode.GetAstIdentifierForOutputIndex(targetFolderIndex).Name;
                var targetFolderMirror = nodeView.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(targetFolderId);
                var targetFolder = targetFolderMirror.GetData().Data as string;

                //Define Paths
                string newFolderPath = (targetFolder + "\\" + fileName);
                string dynamoSavePath = (newFolderPath + "\\" + "tempFolder" + "\\" + fileName + ".dyn");
                string tempFolder = (newFolderPath + "\\" + "tempFolder");
                string zipPath = (newFolderPath + "\\" + fileName + ".zip");
                string imageSavePath = (newFolderPath + "\\capture.png");
                string jsonPath = (newFolderPath + "\\input.json");
                string readMePath = (newFolderPath + "\\README.md");
                string thumbNailPath = (newFolderPath + "\\thumbnail.png");
                DateTime now = DateTime.Now;

                //Convert version to list to follow GH formatting?
                List<string> versionList = new List<string>();
                versionList.Add(versionNumber.ToString());

                //Currently components just contain Dynamo/Hydra
                //Modify to read nodes on canvas
                Dictionary<string, int> components = new Dictionary<string, int>
                {
                    {"Dynamo", 1},
                    {"Hydra", 1 }
                };

                //metaData Dictionary for Json
                Dictionary<string, object> metaDataDict = new Dictionary<string, object>
                {
                    {"versions", versionList},
                    {"tags", fileTagList},
                    {"components", components},
                    //change to dynimg
                    {"ghimg", "capture.png"},
                    {"thumbnail", "thumbnail.png"},
                    {"file", fileName + ".zip"},
                    {"date", now},
                    //change to model view
                    {"rhinoimg", "capture.png"}
                };

                //Check to see if folder for project already exists
                //Overwrite existing (github should save old versions)
                //Consider modifying to append version #
                if (Directory.Exists(newFolderPath))
                {
                    Directory.Delete(newFolderPath, true);
                }

                //Create master folder
                DirectoryInfo di = Directory.CreateDirectory(newFolderPath);
                //Create temporary zip folder to prevent trying to zip the new zip folder
                //Will crash if otherweise
                di = Directory.CreateDirectory(tempFolder);

                //Save Graph
                graph.SaveAs(dynamoSavePath, nodeView.ViewModel.DynamoViewModel.Model.EngineController.LiveRunnerRuntimeCore);

                //Zip dyn
                ZipFile.CreateFromDirectory(tempFolder, zipPath);
                Directory.Delete(tempFolder, true);

                //Save Graph Image
                nodeView.ViewModel.DynamoViewModel.OnRequestSaveImage(nodeView, new ImageSaveEventArgs(imageSavePath));

                //Save Thumbnail
                var fullSize = System.Drawing.Image.FromFile(imageSavePath);
                var thumbnail = fullSize.GetThumbnailImage(200, 85, () => false, IntPtr.Zero);
                thumbnail.Save(thumbNailPath);

                //Write input.json output
                string json = JsonConvert.SerializeObject(metaDataDict);
                File.WriteAllText(jsonPath, json);

                //Write README.md
                string readMe = "### Description@" + fileDescription + "@### Version@" + "File Version: " + versionNumber + "@### Tags@" + fileTags;
                readMe = readMe.Replace("@", Environment.NewLine);
                File.WriteAllText(readMePath, readMe);

            }

        }
        /// <summary>
        /// Here you can do any cleanup you require if you've assigned callbacks for particular 
        /// UI events on your node.
        /// </summary>
        public void Dispose() { }
    }

}
