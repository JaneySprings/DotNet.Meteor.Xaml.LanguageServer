

using DotNet.Meteor.Xaml.LanguageServer.Extensions;

namespace DotNet.Meteor.Xaml.LanguageServer.Syntax;

public class SignatureComparer {
    private List<(string TypeName, string Name)> eventHandlerArgsSignatures;

    public SignatureComparer(List<(string TypeName, string Name)> eventHandlerArgsSignatures) {
        this.eventHandlerArgsSignatures = eventHandlerArgsSignatures;
    }

    public bool Compare(CodeBehindSyntaxWalker.MethodSignatureInfo methodSignatureInfo) {
        int count = eventHandlerArgsSignatures.Count;
        if (count != methodSignatureInfo.ArgumentTypes.Count)
            return false;
        for (int i = 0; i < count; i++) {
            string simpleTypeName = SimplifyTypeName(eventHandlerArgsSignatures[i].TypeName);
            if (simpleTypeName != methodSignatureInfo.ArgumentTypes[i])
                return false;
        }
        return true;
    }

    private static string SimplifyTypeName(string typeName) {
        string[] parts = typeName.Split('<');
        List<string> processedParts = new();
        foreach (string part in parts) {
            int lastDotIndex = part.LastIndexOf('.');
            processedParts.Add(part.Substring(lastDotIndex + 1));
        }
        return string.Join("<", processedParts);
    }
}