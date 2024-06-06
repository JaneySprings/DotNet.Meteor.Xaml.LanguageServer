using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace DotNet.Meteor.Xaml.LanguageServer.Handlers.Workspace;

// public class DidChangeWatchedFilesHandler : DidChangeWatchedFilesHandlerBase {
//     private readonly WorkspaceService workspaceService;

//     public DidChangeWatchedFilesHandler(WorkspaceService workspaceService) {
//         this.workspaceService = workspaceService;
//     }

//     protected override DidChangeWatchedFilesRegistrationOptions CreateRegistrationOptions(DidChangeWatchedFilesCapability capability, ClientCapabilities clientCapabilities) {
//         return new DidChangeWatchedFilesRegistrationOptions() {
//             Watchers = new[] {
//                 new OmniSharp.Extensions.LanguageServer.Protocol.Models.FileSystemWatcher() {
//                     Kind = WatchKind.Create | WatchKind.Change | WatchKind.Delete,
//                     GlobPattern = new GlobPattern("**/*")
//                 },
//             }
//         };
//     }

//     public override Task<Unit> Handle(DidChangeWatchedFilesParams request, CancellationToken cancellationToken) {
//         return Unit.Task;
//     }
// }