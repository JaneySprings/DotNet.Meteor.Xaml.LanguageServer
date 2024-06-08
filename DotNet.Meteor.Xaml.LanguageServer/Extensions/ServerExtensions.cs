using DotNet.Meteor.Xaml.LanguageServer.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace DotNet.Meteor.Xaml.LanguageServer.Extensions;

public static class ServerExtensions {
    public static void ShowError(this ILanguageServerFacade server, string message) {
        CurrentSessionLogger.Error(message);
        server.Window.ShowMessage(new ShowMessageParams {
            Type = MessageType.Error,
            Message = message
        });
    }
    public static void ShowInfo(this ILanguageServerFacade server, string message) {
        CurrentSessionLogger.Debug(message);
        server.Window.ShowMessage(new ShowMessageParams {
            Type = MessageType.Info,
            Message = message
        });
    }

    public static async Task<T> InvokeAsync<T>(T fallback, Func<Task<T>> action) {
        try {
            return await action.Invoke().ConfigureAwait(false);
        } catch (Exception e) {
            LogException(e);
            return fallback;
        }
    }
    public static async Task<T?> InvokeAsync<T>(Func<Task<T>> action) {
        try {
            return await action.Invoke().ConfigureAwait(false);
        } catch (Exception e) {
            LogException(e);
            return default;
        }
    }
    public static async Task InvokeAsync(Func<Task> action) {
        try {
            await action.Invoke().ConfigureAwait(false);
        } catch (Exception e) {
            LogException(e);
        }
    }
    public static T Invoke<T>(T fallback, Func<T> action) {
        try {
            return action.Invoke();
        } catch (Exception e) {
            LogException(e);
            return fallback;
        }
    }
    public static void Invoke(Action action) {
        try {
            action.Invoke();
        } catch (Exception e) {
            LogException(e);
        }
    }

    private static void LogException(Exception e) {
        if (e is TaskCanceledException || e is OperationCanceledException)
            return;
        CurrentSessionLogger.Error(e);
    }
}