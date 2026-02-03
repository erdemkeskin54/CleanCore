using System.Runtime.CompilerServices;

// Internal handler'ları test'ler görebilsin.
[assembly: InternalsVisibleTo("CleanCore.UnitTests")]

namespace CleanCore.Application;

// MediatR ve FluentValidation assembly scanning için marker.
public sealed class AssemblyReference { }
