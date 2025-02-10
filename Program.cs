using RobotOM;
using Newtonsoft.Json;

namespace RobotUpdateTimberSections
{
    class Program
    {
        /// <summary>
        /// Accepts section data in the format:
        /// {
        ///     "section": [list of bar IDs],
        ///     "180x320": [1, 2, 3],
        ///     "360x1000": [4, 5, 6],
        /// }
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new Exception("Invalid number of arguments provided");
            }

            var jsonData = args[0];
            var groupedIds = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(jsonData);
            if (groupedIds == null)
            {
                throw new Exception("Invalid member section data provided");
            }

            RobotApplication robotApp = new();
            if (robotApp.Project.FileName == null)
            {
                throw new Exception("Robot model not open.");
            }

            RobotLabelServer robotLabelServer = robotApp.Project.Structure.Labels;
            robotApp.Project.Preferences.Materials.Load("Eurocode");
            string materialName = "TIMBER";

            foreach (var (section, idsList) in groupedIds)
            {
                var splitSectionName = section.Split('x').ToList();
                double width = double.Parse(splitSectionName[0]);
                double depth = double.Parse(splitSectionName[1]);

                // Add section to the model's currently used sections
                IRobotLabel sectionLabel = robotLabelServer.Create(IRobotLabelType.I_LT_BAR_SECTION, $"Timber {section}");
                RobotBarSectionData sectionData = (RobotBarSectionData)sectionLabel.Data;
                sectionData.MaterialName = materialName;
                sectionData.Type = IRobotBarSectionType.I_BST_NS_RECT;
                sectionData.ShapeType = IRobotBarSectionShapeType.I_BSST_RECT_FILLED;
                RobotBarSectionNonstdData nonStandardSectionData = sectionData.CreateNonstd(0); // parametric section

                nonStandardSectionData.SetValue(IRobotBarSectionNonstdDataValue.I_BSNDV_RECT_B, width / 1000);
                nonStandardSectionData.SetValue(IRobotBarSectionNonstdDataValue.I_BSNDV_RECT_H, depth / 1000);
                sectionData.CalcNonstdGeometry();
                robotLabelServer.Store(sectionLabel);

                // Assign the section to the appropriate group of bars
                RobotSelection barSelection = robotApp.Project.Structure.Selections.Create(IRobotObjectType.I_OT_BAR);
                string ids = string.Join(" ", idsList);
                barSelection.FromText(ids);
                robotApp.Project.Structure.Bars.SetLabel(barSelection, IRobotLabelType.I_LT_BAR_SECTION, sectionLabel.Name);

                // Ensuring the final member material is Timber
                robotApp.Project.Structure.Bars.SetLabel(barSelection, IRobotLabelType.I_LT_MATERIAL, materialName);
            }
        }
    }
}