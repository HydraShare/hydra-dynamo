using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Windows.Forms;
using Newtonsoft.Json;
using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Workspaces;
using Dynamo.ViewModels;
using Autodesk.DesignScript.Runtime;

namespace Hydra.HydraHelperFunctions
{
    [IsVisibleInDynamoLibrary(false)]
    public class HydraHelpers
    {
        private static string[] inputData;

        // Container for all input data
        public static string[] InputData
        {
            get { return inputData; }
            set { inputData = value; }
        }

        // Build local Hydra repository
        public static void exportToHydra(NodeModel model, DynamoViewModel dynamoViewModel, string[] data)
        {
            InputData = data;

            if(InputData.Count() != 6)
            {
                MessageBox.Show("Incorrect input count.");
                return;
            }

            // If missing any data inputs break
            for (int i = 0; i < InputData.Count(); i++)
            {
                if (InputData[i] == null || InputData[i] == "")
                {
                    MessageBox.Show("Invalid field input.");
                    return;
                }
            }

            // Get nodeTextInput()
            string fileName = InputData[0];
            string fileDescription = InputData[1];
            string versionNumber = InputData[2];
            string changeLog = InputData[3];
            string fileTags = InputData[4];
            string targetFolder = InputData[5];

            // define all file paths
            string newFolderPath = (targetFolder + "\\" + fileName);
            string dynamoSavePath = (newFolderPath + "\\" + "tempFolder" + "\\" + fileName + ".dyn");
            string tempFolder = (newFolderPath + "\\" + "tempFolder");
            string zipPath = (newFolderPath + "\\" + fileName + ".zip");
            string canvasSavePath = (newFolderPath + "\\canvas.png");
            string backgroundSavePath = (newFolderPath + "\\background.png");
            string jsonPath = (newFolderPath + "\\input.json");
            string readMePath = (newFolderPath + "\\README.md");
            string thumbNailPath = (newFolderPath + "\\thumbnail.png");
            DateTime now = DateTime.Now;

            // Get current workspace model (graph)
            WorkspaceModel graph = dynamoViewModel.Model.CurrentWorkspace;

            // TODO all these regions should be broken down to seperate functions
            // and called from within the export to Hydra function

            // Build all Hydra data
            List<string> fileTagsList = buildFileTags(fileTags);
            Dictionary<string, int> components = buildActiveNodes(graph);
            List<string> dependencies = buildDependencies(graph);
            // TODO not sure why hydra schema calls for a list here
            string[] versionList = new string[] { versionNumber };
            List<Dictionary<string, string>> imageList = buildImages();
            
            // Build full metadata dictionary for json
            Dictionary<string, object> metadataDict = new Dictionary<string, object>
            {
                {"file", fileName + ".zip"},
                {"thumbnail", "thumbnail.png"},
                { "images", imageList},
                // TODO implement video option
                {"videos", "none"},
                {"tags", fileTagsList},
                {"components", components},
                {"dependencies", dependencies}
            };

            // Check File Paths and Write Files
            try
            {
                // If master folder already exists delete it
                if (Directory.Exists(newFolderPath))
                {
                    Directory.Delete(newFolderPath, true);
                }

                // Create master folder to hold Hydra content
                Directory.CreateDirectory(newFolderPath);
                // Create temporary folder to hold dynamo file before zip
                Directory.CreateDirectory(tempFolder);

                // Save dynamo file to hydra location
                // UPDATE EVERYTHING BELOW HERE WHEN READING NODE UI DATA IS COMPLETE
                // Check to make sure file has been saved
                if (String.IsNullOrEmpty(dynamoViewModel.Model.CurrentWorkspace.FileName) == true)
                {
                    MessageBox.Show("This file has not yet been saved.  Please save to continue.");
                    return;
                }

                // Check to make sure the current dyn file is up to date with the canvas
                if (dynamoViewModel.Model.CurrentWorkspace.HasUnsavedChanges == true)
                {
                    // Alert user the canvas contained unsaved changes
                    // If user proceeds imagery may not correspond with current dyn file
                    MessageBox.Show("There are unsaved changes on the current canvas.  Hydra uses the last saved version of your Dynamo file.  This suggests imagery may not correspond with the last saved dyn file.");
                }

                // copy the last saved dyn file
                File.Copy(dynamoViewModel.Model.CurrentWorkspace.FileName.ToString(), dynamoSavePath);

                // zip dyn
                ZipFile.CreateFromDirectory(tempFolder, zipPath);
                // delete temporary folder
                Directory.Delete(tempFolder, true);

                // Save canvas imagery
                dynamoViewModel.OnRequestSaveImage("Hydra", new ImageSaveEventArgs(canvasSavePath));

                // Save background preview imagery
                dynamoViewModel.OnRequestSave3DImage("Hydra", new ImageSaveEventArgs(backgroundSavePath));

                // TODO provide option for background preview or canavs imagery for thumbnail in 1.3
                // Save thumbnail
                var fullSize = System.Drawing.Image.FromFile(canvasSavePath);
                var thumbnail = fullSize.GetThumbnailImage(200, 85, () => false, IntPtr.Zero);
                thumbnail.Save(thumbNailPath);
                // Dispose or process may still be running when exporting multiple times causing crash
                fullSize.Dispose();
                thumbnail.Dispose();

                // Write JSON from dictionary
                string json = JsonConvert.SerializeObject(metadataDict);
                File.WriteAllText(jsonPath, json);

                // Write README.md
                string Tags = null;
                foreach (string item in fileTagsList)
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

            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        #region Utility Functions
        private static List<string> buildFileTags(string fileTags)
        {
            // Output list for file tags
            List<string> fileTagsList = new List<string>();

            // If list is provided remove curly braces and quotes
            if (fileTags.Contains('['))
            {
                fileTags = fileTags.Replace("[", "");
                fileTags = fileTags.Replace("]", "");
                fileTags = fileTags.Replace("\"", "");
            }

            // If newline remove
            else if (fileTags.Contains('\n'))
            {
                fileTags = fileTags.Replace(System.Environment.NewLine, ",");
                fileTags = fileTags.Replace("\"", "");
            }

            // If string remove quotes
            else if (fileTags.Contains('\"'))
            {
                fileTags = fileTags.Replace("\"", "");
            }

            // TODO Only allow comma seperated tags
            // Determine delimiter used and split string into list
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
            else if (fileTags.Contains('\n'))
            {
                fileTagsList = new List<string>(fileTags.Split('\n'));
            }
            else if (fileTags.Contains(' '))
            {
                fileTagsList = new List<string>(fileTags.Split(' '));
            }
            // If no delimiter, newlines, or spaces add fileTag as single string to list
            else
            {
                fileTagsList.Add(fileTags);
            }

            // If tag list doesn't contain 'Dynamo' add it
            if (fileTagsList.Contains("Dynamo") == false && fileTags.Contains("dynamo") == false)
            {
                fileTagsList.Add("Dynamo");
            }

            // If tag list doesn't contain 'Hydra' add it
            if (fileTagsList.Contains("Hydra") == false && fileTags.Contains("hydra") == false)
            {
                fileTagsList.Add("Hydra");
            }

            // Return output
            return fileTagsList;
        }

        private static Dictionary<string, int> buildActiveNodes(WorkspaceModel graph)
        {
            // Key-Value pairs of all unique nodes in graph and their counts
            Dictionary<string, int> components = new Dictionary<string, int> { };
            // Unique list of all packages required by graph
            List<string> dependencies = new List<string>();

            foreach (NodeModel node in graph.Nodes)
            {
                // Current node name
                string nodeString = node.Name;

                // If node doesn't exist in component dictionary add it
                if (!components.Keys.Contains(nodeString))
                {
                    components.Add(nodeString, 1);
                }

                // If node does already exist increment count by 1
                else
                {
                    components[nodeString] += 1;
                }
            }

            // Return output
            return components;
        }

        private static List<string> buildDependencies(WorkspaceModel graph)
        {
            List<string> dependencies = new List<string>();

            // TODO remove harcoded built-in categories
            string[] builtinNodeCategories = new string[]
            {
                "Dictionary",
                "Display",
                "Geometry",
                "ImportExport",
                "Input",
                "List",
                "Math",
                "Script",
                "String"
            };

            foreach (NodeModel node in graph.Nodes)
            {
                string nodeCategory;

                // If node isn't part of a built-in category proceed
                if (builtinNodeCategories.Any(node.Category.Contains) == false)
                {
                    // If the new dependecy category contains sub-categories 
                    // slice it down to just the top level library name
                    if (node.Category.Contains('.'))
                    {
                        int index = node.Category.IndexOf('.');
                        nodeCategory = node.Category.Substring(0, index);
                    }

                    // If the new dependecy category doesn't contains sub-categories
                    // grab entire library name
                    else
                    {
                        nodeCategory = node.Category;
                    }

                    // If dependency hasn't already been included and 
                    // is not an empty string added it to master list
                    if (!dependencies.Contains(nodeCategory) && nodeCategory != "")
                    {
                        dependencies.Add(nodeCategory);
                    }
                }
            }

            // Return output
            return dependencies;
        }

        private static List<Dictionary<string, string>> buildImages()
        {
            List<Dictionary<string, string>> images = new List<Dictionary<string, string>>();

            // Canvas Imagery
            Dictionary<string, string> canvasImages = new Dictionary<string, string>
            {
                {"canvas.png", "Dynamo Definition"}
            };

            // Background Preview Imagery
            Dictionary<string, string> backgroundPreviewImages = new Dictionary<string, string>
            {
                {"background.png", "Dynamo Background Preview"}
            };

            images.Add(canvasImages);
            images.Add(backgroundPreviewImages);

            return images;
        }
        #endregion
    }
}
