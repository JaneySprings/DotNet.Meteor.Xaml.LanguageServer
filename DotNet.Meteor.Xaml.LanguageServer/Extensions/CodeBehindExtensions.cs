
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace DotNet.Meteor.Xaml.LanguageServer.Extensions;

internal class EventInsertionSyntaxWalker : CSharpSyntaxWalker {
    private readonly string originalEventName;
    private readonly List<MethodDeclarationSyntax> methods;

    private string coercedEventName;
    private int similarMethodCount;

    public EventInsertionSyntaxWalker(string eventName) {
        this.originalEventName = eventName;
        coercedEventName = eventName;
        methods = new List<MethodDeclarationSyntax>();

    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
        methods.Add(node);

        if (node.Identifier.ValueText == originalEventName) {
            similarMethodCount++;
            coercedEventName = $"{originalEventName}{similarMethodCount+1}";
        }

        base.VisitMethodDeclaration(node);
    }

    public Position? GetLastPrivateMethodPosition() {
        var lastPrivateMethod = methods.LastOrDefault();
        if (lastPrivateMethod == null)
            return null;

        return new Position(
            lastPrivateMethod.GetLocation().GetLineSpan().StartLinePosition.Line,
            lastPrivateMethod.GetLocation().GetLineSpan().StartLinePosition.Character
        );
    }
    public string GetCoercedEventName() {
        return coercedEventName;
    }
}