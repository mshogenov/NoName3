using System.Diagnostics;
using SystemModelingCommands.Models;

namespace SystemModelingCommands.Services;

public class ConnectionRestorer
{
    private readonly Document _doc;
    private const int MaxIterations = 20;

    public ConnectionRestorer(Document doc)
    {
        _doc = doc ?? throw new ArgumentNullException(nameof(doc));
    }

    
}