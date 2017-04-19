using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Windows.Forms;
using Newtonsoft.Json;
using Dynamo.Controls;
using Dynamo.ViewModels;
using Autodesk.DesignScript.Runtime;

namespace Hydra.HydraHelperFunctions
{
    [IsVisibleInDynamoLibrary(false)]
    public class HydraHelpers
    {
        // create container for all input data
        public static string[] data = new string[6];

        // get input data
        // this method is called immedietly when the node runs and passes the data to the output
        public static string[] collectData(string fileName, string fileDescription, string versionNumber, string changeLog, string fileTags, string targetFolder)
        {
            // store most current input data in global data container
            data[0] = fileName;
            data[1] = fileDescription;
            data[2] = versionNumber;
            data[3] = changeLog;
            data[4] = fileTags;
            data[5] = targetFolder;

            // return data list as output for node
            return data;
        }

        // do work (grabs latest input data from the global data container)
        public static void exportToHydra(Object model, NodeView nodeView)
        {
            // test to verify no inputs return a null value
            for (int i = 0; i < data.Length; i++)
            {
                // if null found break before doing any work
                if (data[i].Equals(null) == true)
                {
                    break;
                }
            }

            // grab all input values and store in appropriate variables
            string fileName = data[0];
            string fileDescription = data[1];
            string versionNumber = data[2];
            string changeLog = data[3];
            string fileTags = data[4];
            string targetFolder = data[5];

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

            // get current graph
            var graph = nodeView.ViewModel.DynamoViewModel.Model.CurrentWorkspace;

            // TODO all these regions should be broken down to seperate functions
            // and called from within the export to Hydra function

            #region Build Tag List
            // list for file tags
            List<string> fileTagsList = new List<string>();

            // if list is provided remove curly braces and quotes
            if (fileTags.Contains('{'))
            {
                fileTags = fileTags.Replace("{", "");
                fileTags = fileTags.Replace("}", "");
                fileTags = fileTags.Replace("\"", "");
            }
            // if newline remove
            else if (fileTags.Contains('\n'))
            {
                fileTags = fileTags.Replace(System.Environment.NewLine, ",");
                fileTags = fileTags.Replace("\"", "");
            }
            // if string remove quotes
            else if (fileTags.Contains('\"'))
            {
                fileTags = fileTags.Replace("\"", "");
            }

            // TODO Only allow comma seperated tags
            // determine delimiter used and split string into list
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
            // if no delimiter, newlines, or spaces add fileTag as single string to list
            else
            {
                fileTagsList.Add(fileTags);
            }

            // if tag list doesn't contain "Dynamo" add it
            if (fileTagsList.Contains("Dynamo") == false && fileTags.Contains("dynamo") == false)
            {
                fileTagsList.Add("Dynamo");
            }
            #endregion

            #region Build Version List
            // TODO does this have to be list or should it be single string?
            List<string> versionList = new List<string>();
            versionList.Add(versionNumber.ToString());
            #endregion

            #region Build Dictionary of Active Nodes and List of Active Packages
            // TODO rename components to nodes for Dynamo (will break if Hydra core isn't updated)
            // node dictionary (contains all nodes currently on canvas) 
            Dictionary<string, int> components = new Dictionary<string, int> { };

            foreach (var node in graph.Nodes)
            {
                // get node name as string
                string nodeString = node.NickName;
                
                // if node doesn't exist in component dictionary add it
                if (!components.Keys.Contains(nodeString))
                {
                    components.Add(nodeString, 1);
                }

                // if node does already exist increment count by 1
                else if (components.Keys.Contains(nodeString))
                {
                    components[nodeString] += 1;
                }
            }

            // TODO this should be rewritten referencing builtincategories (avoid hard coding categories)
            // here we determine if any nodes are packages (not "out of the box")
            // build list of stock node categories
            List<string> stockDependencies = new List<string>();
            stockDependencies.Add("Analyze");
            stockDependencies.Add("BuiltIn");
            stockDependencies.Add("Core");
            stockDependencies.Add("Display");
            stockDependencies.Add("Geometry");
            stockDependencies.Add("Office");
            stockDependencies.Add("Operators");
            stockDependencies.Add("Input/Output");

            // check to see if node is in a stock category
            // if not in category add as dependency node
            List<string> dependencies = new List<string>();

            foreach (var node in graph.Nodes)
            {
                // container for package related node
                string dependentCategory;

                // if node isn't in a stock category proceed
                if (stockDependencies.Any(node.Category.Contains) == false)
                {
                    // if the new dependecy category contains sub categories 
                    // slice it down to just the top level library name
                    if (node.Category.Contains('.'))
                    {
                        int index = node.Category.IndexOf('.');
                        dependentCategory = node.Category.Substring(0, index);
                    }

                    // if the new dependecy category doesn't contains sub categories
                    // grab entire library name
                    else
                    {
                        dependentCategory = node.Category;
                    }

                    // check to see if dependency has already been added to the master list
                    // and verify a blank dependency name isn't added
                    if (!dependencies.Contains(dependentCategory) && dependentCategory != "")
                    {
                        dependencies.Add(dependentCategory);
                    }
                }
            }
            #endregion

            #region Build Image List
            // TODO consider rewriting (look into why json wants a list of dictionaries?)

            // TODO enable ability to attach multiple images from outside file path
            // ex: if addImgs != null append to dictionary (addImgs new input)

            // dictionary containing canvas imagery
            Dictionary<string, string> canvasImages = new Dictionary<string, string>
            {
                {"canvas.png", "Dynamo Definition"}
            };
            // dictionary containing background preview imagery
            Dictionary<string, string> backgroundPreviewImages = new Dictionary<string, string>
            {
                {"background.png", "Dynamo Background Preview"}
            };

            // list that contains formatted images
            List<object> imageList = new List<object>();
            imageList.Add(canvasImages);
            imageList.Add(backgroundPreviewImages);
            #endregion

            #region Build Dictionary of Metadata
            // build full metadata dictionary for json
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
            #endregion

            #region Check File Paths and Write Files
            // check to see if master folder already exists
            if (Directory.Exists(newFolderPath))
            {
                // verfiy user wants to overwrite existing version
                DialogResult dialogResult = MessageBox.Show("The specified fileName/targetFolder combination already exists and will be overwritten.  Do you still wish to continue?", "Friendly Warning", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    Directory.Delete(newFolderPath, true);
                }
                else if (dialogResult == DialogResult.No)
                {
                    return;
                }
            }

            // create master folder to hold Hydra content
            Directory.CreateDirectory(newFolderPath);
            // create temporary folder to hold dynamo file before zip
            Directory.CreateDirectory(tempFolder);

            // TODO add option to save from dialog box
            // check to make sure file has been saved
            if (String.IsNullOrEmpty(nodeView.ViewModel.DynamoViewModel.Model.CurrentWorkspace.FileName) == true)
            {
                MessageBox.Show("This file has not yet been saved.  Please save to continue.");
                return;
            }

            // check to make sure the current dyn file is up to date with the canvas
            if (nodeView.ViewModel.DynamoViewModel.HomeSpace.HasUnsavedChanges == true)
            {
                // TODO provide option to save or continue from dialog box
                // alert user the canvas contained unsaved changes
                // if user proceeds imagery may not correspond with current dyn file
                MessageBox.Show("There are unsaved changes on the current canvas.  Hydra uses the last saved version of your Dynamo file.  This suggests imagery may not correspond with the last saved dyn file.");
            }

            // copy the last saved dyn file
            File.Copy(nodeView.ViewModel.DynamoViewModel.Model.CurrentWorkspace.FileName.ToString(), dynamoSavePath);

            // zip dyn
            ZipFile.CreateFromDirectory(tempFolder, zipPath);
            // delete temporary folder
            Directory.Delete(tempFolder, true);

            // save graph capture
            nodeView.ViewModel.DynamoViewModel.OnRequestSaveImage(model, new ImageSaveEventArgs(canvasSavePath));

            // TODO uncomment for version 1.4.0 (OnRequestSave3DImage not exposed in 1.2.0 or 1.3.0)
            // save background preview capture
            //nodeView.ViewModel.DynamoViewModel.OnRequestSave3DImage(model, new ImageSaveEventArgs(backgroundSavePath));

            // TODO provide option for background preview or canavs imagery for thumbnail in 1.3
            // save thumbnail
            var fullSize = System.Drawing.Image.FromFile(canvasSavePath);
            var thumbnail = fullSize.GetThumbnailImage(200, 85, () => false, IntPtr.Zero);
            thumbnail.Save(thumbNailPath);
            // dispose or process may still be running when exporting multiple times causing crash
            fullSize.Dispose();
            thumbnail.Dispose();
            
            // write json from dictionary
            string json = JsonConvert.SerializeObject(metadataDict);
            File.WriteAllText(jsonPath, json);

            // write README.md
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
            #endregion
        }
    }
}
