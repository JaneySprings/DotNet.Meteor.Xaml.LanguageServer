namespace DotNet.Meteor.Xaml.LanguageServer.Models;

public class Document {
    public string DocumentPath { get; }
    public string Text { get; }

    public Document(string documentPath, string text) {
        DocumentPath = documentPath;
        Text = text;
    }
}