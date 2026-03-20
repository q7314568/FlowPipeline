using System.Reflection;
using System.Text;
using FlowPipeline.Core;

namespace FlowPipeline.Tests;

public class PublicApiApprovalTests
{
    [Fact]
    public void PublicApi_ShouldMatchApprovedSnapshot()
    {
        var assembly = typeof(PipelineBuilder).Assembly;
        var approvedPath = Path.Combine(AppContext.BaseDirectory, "PublicApi.approved.txt");
        var expected = File.ReadAllText(approvedPath).ReplaceLineEndings("\n").TrimEnd();
        var actual = BuildPublicApiSnapshot(assembly).ReplaceLineEndings("\n").TrimEnd();

        Assert.Equal(expected, actual);
    }

    private static string BuildPublicApiSnapshot(Assembly assembly)
    {
        var builder = new StringBuilder();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var type in assembly.GetExportedTypes().OrderBy(type => type.Namespace).ThenBy(type => type.Name))
        {
            builder.AppendLine(type.FullName);

            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .OrderBy(ctor => ctor.ToString(), StringComparer.Ordinal))
            {
                builder.Append("  ctor ").AppendLine(ctor.ToString());
            }

            foreach (var prop in type.GetProperties(bindingFlags).OrderBy(prop => prop.Name, StringComparer.Ordinal))
            {
                builder.Append("  prop ")
                    .Append(prop.PropertyType.Name)
                    .Append(' ')
                    .AppendLine(prop.Name);
            }

            foreach (var field in type.GetFields(bindingFlags).OrderBy(field => field.Name, StringComparer.Ordinal))
            {
                builder.Append("  field ")
                    .Append(field.FieldType.Name)
                    .Append(' ')
                    .AppendLine(field.Name);
            }

            foreach (var method in type.GetMethods(bindingFlags)
                .Where(method => !method.IsSpecialName)
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .ThenBy(method => method.ToString(), StringComparer.Ordinal))
            {
                builder.Append("  method ").AppendLine(method.ToString());
            }
        }

        return builder.ToString().TrimEnd();
    }
}
