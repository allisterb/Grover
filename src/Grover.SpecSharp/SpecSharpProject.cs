namespace Grover.SpecSharp;

public abstract class SpecSharpProject : Runtime
{
    #region Constructors
    public SpecSharpProject(string fileName) :base() 
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"The project file {fileName} was not found.");
        }
        ProjectFile = new FileInfo(fileName);
    }
    #endregion

    #region Properties
    public FileInfo ProjectFile { get; init; }
    public List<string> FilesToCompile { get; } = new List<string>();
    #endregion
}

