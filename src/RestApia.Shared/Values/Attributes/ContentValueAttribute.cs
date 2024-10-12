using System.Diagnostics.CodeAnalysis;
namespace RestApia.Shared.Values.Attributes;

[AttributeUsage(AttributeTargets.Property)]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class ContentValueAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public bool IsRequired { get; init; }
}
