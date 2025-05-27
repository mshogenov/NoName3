using Autodesk.Revit.DB.Plumbing;

namespace ArrangeFixtures.Models;

public class PipeExtremums
{
    public Pipe MaxX { get; set; }
    public Pipe MinX { get; set; }
    public Pipe MaxY { get; set; }
    public Pipe MinY { get; set; }
    public Pipe MaxZ { get; set; }
    public Pipe MinZ { get; set; }
}