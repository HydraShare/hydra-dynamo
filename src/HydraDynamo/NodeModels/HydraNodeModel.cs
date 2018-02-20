using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Dynamo.Graph.Nodes;
using Dynamo.Controls;
using Dynamo.UI.Commands;
using Dynamo.Wpf;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;
using Newtonsoft.Json;
using Hydra.HydraHelperFunctions;

namespace Hydra
{
    /*
    Hydra: A Plugin for example file sharing
     
    Use this component to export your dyn file to your Hydra repository so that you can upload and share with the community!
    Args:
         fileName: A text name for your example file.
         fileDescription: A text description of your example file.  This can be a list and each item will be written as a new paragraph.
         versionNumber: A numerical input for the example file version.
         changeLog_: A text description of the changes that you have made to the file if this is a new version of an old example file.
         fileTags_: An optional list of test tags to decribe your example file.  This will help others search for your file easily.
         targetFolder_: Input a directory path here to the hydra folder on you machine if you are not using the default Github structure that places your hydra github repo in your documents folder.
         additionalImgs_: A list of file paths to additional images that you want to be shown in your Hydra page
    Returns:
          readMe: String
    */

    /// <summary>
    /// Hydra node implementation.
    /// </summary>
    [NodeName("Hydra")]
    [NodeCategory("Hydra")]
    [NodeDescription("Export Dynamo files to Hydra repository so that you can upload and share with the community!")]
    [OutPortTypes("string")]
    [IsDesignScriptCompatible]
    public class HydraNodeModel : NodeModel
    {
        #region Private Node Properties
        private string fileName = "hydraExampleFile";
        private string description = "This is an example description.";
        private string version = "2.0.0";
        private string changeLog = "Notes:\n - New Node UI\n - Dynamo 2.0 Compliant";
        private string fileTags = "hydra, dynamo, upload, example, share, sample";
        private string targetFolder = @"C:\Users\alfarok\Desktop\HydraTest";
        private Dynamo.ViewModels.DynamoViewModel dynamoModel;
        #endregion

        #region Public Node Properties
        // Text that will appear in node text fields
        public string FileName
        {
            get { return fileName; }
            set
            {
                fileName = value;
                RaisePropertyChanged("NodeFileName");
            }
        }

        public string Description
        {
            get { return description; }
            set
            {
                description = value;
                RaisePropertyChanged("NodeDescription");
            }
        }

        public string Version
        {
            get { return version; }
            set
            {
                version = value;
                RaisePropertyChanged("NodeVersion");
            }
        }

        public string ChangeLog
        {
            get { return changeLog; }
            set
            {
                changeLog = value;
                RaisePropertyChanged("NodeChangeLog");
            }
        }

        public string FileTags
        {
            get { return fileTags; }
            set
            {
                fileTags = value;
                RaisePropertyChanged("NodeFileTags");
            }
        }

        public string TargetFolder
        {
            get { return targetFolder; }
            set
            {
                targetFolder = value;
                RaisePropertyChanged("NodeTargetFolder");
            }
        }

        [JsonIgnore]
        public Dynamo.ViewModels.DynamoViewModel DynamoModel
        {
            get { return dynamoModel; }
            set
            {
                dynamoModel = value;
                RaisePropertyChanged("NodeDynamoModel");
            }
        }

        #endregion

        #region DelegateCommand
        // Bind UI interaction to methods on data context
        [JsonIgnore]
        [IsVisibleInDynamoLibrary(false)]
        public DelegateCommand SubmitCommand { get; set; }
        #endregion

        #region Constructors
        public HydraNodeModel()
        {
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("Results", "Null if incomplete.")));
            RegisterAllPorts();
            ArgumentLacing = LacingStrategy.Disabled;
            SubmitCommand = new DelegateCommand(SubmitData, CanSubmitData);
        }

        [JsonConstructor]
        public HydraNodeModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            SubmitCommand = new DelegateCommand(SubmitData, CanSubmitData);
        }
        #endregion

        #region Command Messages
        private static bool CanSubmitData(object obj)
        {
            return true;
        }

        private void SubmitData(object obj)
        {
            // TODO this can be remove or turned off
            MessageBox.Show(
                this.FileName + "\n" +
                this.Description + "\n" +
                this.Version + "\n" +
                this.ChangeLog + "\n" +
                this.FileTags + "\n" +
                this.TargetFolder
                );

            // Wrap input data
            string[] data = new string[]
            {
                this.FileName,
                this.Description,
                this.Version,
                this.ChangeLog,
                this.FileTags,
                this.TargetFolder
            };

            // TODO remove DynamoModel parameter
            // Build local Hydra files
            HydraHelpers.exportToHydra(this, this.DynamoModel, data);

        }
        #endregion

        #region Build Output AST
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // TODO if any null inputs return null node
            if(false)
            {
                return new[]
                {
                    AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode())
                };
            }
            else
            {
                return new[]
                {
                    AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), 
                    AstFactory.BuildStringNode
                    (
                        this.FileName + "\n" +
                        this.Description + "\n" +
                        this.Version + "\n" +
                        this.ChangeLog + "\n" +
                        this.FileTags + "\n" +
                        this.TargetFolder
                        ))
                };
            }
        }
        #endregion

        #region Node View Customization
        public class CustomNodeModelNodeViewCustomization : INodeViewCustomization<HydraNodeModel>
        {
            public void CustomizeView(HydraNodeModel model, NodeView nodeView)
            {
                HydraShareControl hydraControl = new HydraShareControl();
                nodeView.inputGrid.Children.Add(hydraControl);

                // TODO - VERIFY THIS IS SAFE (COULD BE A TERRIBLE IDEA...)
                // Store a reference to the DynamoViewModel
                model.DynamoModel = nodeView.ViewModel.DynamoViewModel;

                // Set the data context for our control to be the node model.
                // Properties in this class which are data bound will raise 
                // property change notifications which will update the UI.
                hydraControl.DataContext = model;
            }

            public void Dispose()
            {
            }
        }
        #endregion
    }
}
