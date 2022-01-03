namespace Grover.SpecSharp;

using Microsoft.Build;
using Microsoft.Build.Evaluation;
public class CsProject : SpecSharpProject
{
    #region Constructors
    public CsProject(string filePath) : base(filePath)
    {
        var collection = new ProjectCollection();
        MsBuildProject = collection.LoadProject(filePath);
        if (MsBuildProject is not null)
        {
            Initialized = true;
        }
    }
    #endregion

    #region Properties
    public Project? MsBuildProject { get; init;}
    
    #endregion
}

