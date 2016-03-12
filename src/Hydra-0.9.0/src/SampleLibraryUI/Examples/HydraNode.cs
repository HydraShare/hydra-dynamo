using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;
using Dynamo.Controls;
using Dynamo.Graph.Nodes;
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

     * Args:
     *      fileName: A text name for your example file.
     *      fileDescription: A text description of your example file.  This can be a list and each item will be written as a new paragraph.
     *      versionNumber: A numerical input for the example file version.
     *      changeLog_: A text description of the changes that you have made to the file if this is a new version of an old example file.
     *      fileTags_: An optional list of test tags to decribe your example file.  This will help others search for your file easily.
     *      targetFolder_: Input a directory path here to the hydra folder on you machine if you are not using the default Github structure that places your hydra github repo in your documents folder.
     *      additionalImgs_: A list of file paths to additional images that you want to be shown in your Hydra page
     * Returns:
     *      Empty
    */

    // The NodeName attribute is what will display on to of the node in Dynamo
    [NodeName("HydraShare")]

    // The NodeCategory attribute determines how your node is organized in the library
    [NodeCategory("Hydra")]

    // The description will display in the tooltip
    [NodeDescription("Export Dynamo Files to Hydra Repository")]

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
            InPortData.Add(new PortData("fileName", "A text name for your example file"));
            InPortData.Add(new PortData("fileDescription", "A text description of your example file"));
            InPortData.Add(new PortData("versionNumber", "Enter a version number for your example file"));
            InPortData.Add(new PortData("changeLog", "A text description of changes made to file from an older version"));
            InPortData.Add(new PortData("fileTags", "A list of tags to describe your file"));
            InPortData.Add(new PortData("targetFolder", "Directory path to Hydra folder by default this is in your documents folder"));

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
            //here is where the button command will be called, we can raise another event here
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
            if (model.InPorts.Any(x => x.Connectors.Count == 0))
            {
                return;
            }
            else
            {
                var graph = nodeView.ViewModel.DynamoViewModel.Model.CurrentWorkspace;

                //_fileName Input
                var fileNamenode = model.InPorts[0].Connectors[0].Start.Owner;
                var fileNameIndex = model.InPorts[0].Connectors[0].Start.Index;
                var fileNameId = fileNamenode.GetAstIdentifierForOutputIndex(fileNameIndex).Name;
                var fileNameMirror = nodeView.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(fileNameId);
                var fileName = fileNameMirror.GetData().Data as string;

                //_fileDescription Input
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

                //changeLog_
                var changeLognode = model.InPorts[3].Connectors[0].Start.Owner;
                var changeLogIndex = model.InPorts[3].Connectors[0].Start.Index;
                var changeLogId = changeLognode.GetAstIdentifierForOutputIndex(changeLogIndex).Name;
                var changeLogMirror = nodeView.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(changeLogId);
                var changeLog = changeLogMirror.GetData().Data as string;

                //fileTags_
                var fileTagsnode = model.InPorts[4].Connectors[0].Start.Owner;
                var fileTagsIndex = model.InPorts[4].Connectors[0].Start.Index;
                var fileTagsId = fileTagsnode.GetAstIdentifierForOutputIndex(fileTagsIndex).Name;
                var fileTagsMirror = nodeView.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(fileTagsId);
                var fileTags = fileTagsMirror.GetStringData() as string;
                List<string> fileTagsList = new List<string>();

                //if list remove curly braces and quotes
                if (fileTags.Contains('{'))
                {
                    fileTags = fileTags.Replace("{", "");
                    fileTags = fileTags.Replace("}", "");
                    fileTags = fileTags.Replace("\"", "");
                }
                //if newline remove
                else if (fileTags.Contains("\n"))
                {
                    fileTags = fileTags.Replace(System.Environment.NewLine, ",");
                    fileTags = fileTags.Replace("\"", "");
                }
                //if string remove quotes
                else if (fileTags.Contains("\""))
                {
                    fileTags = fileTags.Replace("\"", "");
                }

                //determine delimiter and split string into list
                if (fileTags.Contains(','))
                {
                    fileTags = fileTags.Replace(" ", "");
                    fileTagsList = new List<string>(fileTags.Split(','));
                }
                else if (fileTags.Contains(';'))
                {
                    fileTags = fileTags.Replace(" ", "");
                    fileTagsList = new List<string>(fileTags.Split(';'));
                }
                else if (fileTags.Contains("\n"))
                {
                    fileTagsList = new List<string>(fileTags.Split('\n'));
                }
                else if (fileTags.Contains(" "))
                {
                    fileTagsList = new List<string>(fileTags.Split(' '));
                }
                // if no delimiter, newlines, or spaces add fileTag as single string to list
                else
                {
                    fileTagsList.Add(fileTags);
                }

                //if Tag list doesn't contain "Dynamo" add it
                if (fileTagsList.Contains("Dynamo") == false && fileTags.Contains("dynamo") == false)
                {
                fileTagsList.Add("Dynamo");
                }

                //_targetFolder Input
                var targetFoldernode = model.InPorts[5].Connectors[0].Start.Owner;
                var targetFolderIndex = model.InPorts[5].Connectors[0].Start.Index;
                var targetFolderId = targetFoldernode.GetAstIdentifierForOutputIndex(targetFolderIndex).Name;
                var targetFolderMirror = nodeView.ViewModel.DynamoViewModel.Model.EngineController.GetMirror(targetFolderId);
                var targetFolder = targetFolderMirror.GetData().Data as string;

                //Define Paths
                var newFolderPath = (targetFolder + "\\" + fileName);
                var dynamoSavePath = (newFolderPath + "\\" + "tempFolder" + "\\" + fileName + ".dyn");
                var tempFolder = (newFolderPath + "\\" + "tempFolder");
                var zipPath = (newFolderPath + "\\" + fileName + ".zip");
                var imageSavePath = (newFolderPath + "\\capture.png");
                var jsonPath = (newFolderPath + "\\input.json");
                var readMePath = (newFolderPath + "\\README.md");
                var thumbNailPath = (newFolderPath + "\\thumbnail.png");
                DateTime now = DateTime.Now;

                List<string> versionList = new List<string>();
                versionList.Add(versionNumber.ToString());

                //Component dictionary
                Dictionary<string, int> components = new Dictionary<string, int>
                {
                };
                
                //Create list of components
                List<string> componentList = new List<string>();
                foreach(var node in graph.Nodes)
                {
                    componentList.Add(node.NickName);
                }
                //Add component to dictionary or increment count
                foreach(string component in componentList)
                {
                    if(!components.Keys.Contains(component))
                    {
                        components.Add(component, 1);
                    }
                    else if(components.Keys.Contains(component))
                    {
                        components[component] += 1;
                    }
                }

                //create stock categories list
                //this should be rewritten referencing builtincategories
                List<string> stockDependencies = new List<string>();
                stockDependencies.Add("Analyze");
                stockDependencies.Add("BuiltIn");
                stockDependencies.Add("Core");
                stockDependencies.Add("Display");
                stockDependencies.Add("Geometry");
                stockDependencies.Add("Office");
                stockDependencies.Add("Operators");
                stockDependencies.Add("Input/Output");
                
                //check to see if node is in stock category
                //if not in category add as dependency
                List<string> dependencies = new List<string>();
                string dependentCategory;
                foreach (var node in graph.Nodes)
                {
                    if(stockDependencies.Any(node.Category.Contains) == false)
                    {
                        if (node.Category.Contains('.'))
                        {
                            int index = node.Category.IndexOf('.');
                            dependentCategory = node.Category.Substring(0, index);
                        }
                        else
                        {
                            dependentCategory = node.Category;
                        }

                        if (!dependencies.Contains(dependentCategory) && dependentCategory != "")
                        {
                            dependencies.Add(dependentCategory);
                        }
                    }
                }

                //dictionary for future image options
                Dictionary<string, string> images = new Dictionary<string, string>
                {
                    {"capture.png", "Dynamo Definition"}
                };
                List<object> imageList = new List<object>();
                imageList.Add(images);

                //metadata for JSON
                Dictionary<string, object> metaDataDict = new Dictionary<string, object>
                {
                    {"file", fileName + ".zip"},
                    {"thumbnail", "thumbnail.png"},
                    {"images", imageList},
                    //add video option
                    {"videos", "none"},
                    {"tags", fileTagsList},
                    {"components", components},
                    {"dependencies", dependencies}
                };

                //Check to see if master folder already exists
                if (Directory.Exists(newFolderPath))
                {
                    Directory.Delete(newFolderPath, true);
                }

                //Create master folder
                DirectoryInfo di = Directory.CreateDirectory(newFolderPath);
                //Create Zip Folder to prevent trying to zip the new zip folder
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
                string Tags = null;
                foreach(string item in fileTagsList)
                {
                    Tags += item + ",";
                }
                Tags = Tags.TrimEnd(',');
                string readMe = String.Join(
                    Environment.NewLine,
                    "### Description",
                    fileDescription,
                    "### Version",
                    "File Version: " + versionNumber,
                    "### Tags",
                    Tags);
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
