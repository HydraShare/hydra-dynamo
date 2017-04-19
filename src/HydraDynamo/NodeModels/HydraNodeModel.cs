using System;
using System.Collections.Generic;
using Dynamo.Graph.Nodes;
using Dynamo.Controls;
using Dynamo.UI.Commands;
using Dynamo.Wpf;
using Autodesk.DesignScript.Runtime;
using ProtoCore.AST.AssociativeAST;
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
    [NodeDescription("Export Dynamo files to Hydra repository so that you can upload and share with the community!")]
    [NodeCategory("Hydra")]
    [InPortNames("fileName", "fileDescription", "versionNumber", "changeLog", "fileTags", "targetFolder")]
    [InPortTypes("string", "string", "string", "string", "string", "string")]
    [InPortDescriptions(
    "A name for your example file",
    "A description of your example file",
    "A version number for your example file",
    "A description of changes made to file compared to an older version",
    "A string of tags (seperated by commas) to describe your file",
    "A directory path to Hydra folder (by default this is in your documents folder)"
    )]
    [OutPortNames("readMe")]
    [OutPortTypes("string[]")]
    [OutPortDescriptions("null if incomplete")]
    [IsDesignScriptCompatible]
    public class HydraShare : NodeModel
    {
        private string message;

        // request save action.
        public Action RequestSave;

        // a message that will appear on the button
        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                // raise a property changed notification
                // to alert the UI that an element needs
                // an update.
                RaisePropertyChanged("NodeMessage");
            }
        }

        // DelegateCommand objects allow you to bind
        // UI interaction to methods on your data context.
        [IsVisibleInDynamoLibrary(false)]
        public DelegateCommand MessageCommand { get; set; }

        public HydraShare()
        {
            RegisterAllPorts();
            ArgumentLacing = LacingStrategy.Disabled;
            MessageCommand = new DelegateCommand(ShowMessage, CanShowMessage);
            // latest build hides this message from being displayed on button
            Message = "Share";
        }

        private static bool CanShowMessage(object obj)
        {
            return true;
        }

        private void ShowMessage(object obj)
        {
            // TODO this logic has to be revised for 1.3
            // API changes remove !HasConnectedInput

            // only run if all input ports are connected
            if (!HasConnectedInput(0) || !HasConnectedInput(1) || !HasConnectedInput(2) || !HasConnectedInput(3) || !HasConnectedInput(4) || !HasConnectedInput(5))
            {
                return;
            }

            else
            {
                this.RequestSave();
            }
        }

        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // the helper function must be in a seperate assembly to avoid dereferencing pointer error
            // must also specify this assembly in pkg file
            var functionCall =
                AstFactory.BuildFunctionCall(
                    new Func<string, string, string, string, string, string, string[]>(HydraHelpers.collectData),
                    new List<AssociativeNode> { inputAstNodes[0], inputAstNodes[1], inputAstNodes[2], inputAstNodes[3], inputAstNodes[4], inputAstNodes[5] });

            return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall) };
        }

        public class CustomNodeModelNodeViewCustomization : INodeViewCustomization<HydraShare>
        {
            public void CustomizeView(HydraShare model, NodeView nodeView)
            {
                var hydraControl = new HydraShareControl();
                nodeView.inputGrid.Children.Add(hydraControl);
                hydraControl.DataContext = model;
                model.RequestSave += () => HydraHelpers.exportToHydra(model, nodeView);
            }

            public void Dispose()
            {
            }
        }
    }
}
