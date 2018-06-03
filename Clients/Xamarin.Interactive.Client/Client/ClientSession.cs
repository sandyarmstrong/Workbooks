//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client
{
    class BrowserTaskRequest
    {
        
    }

    class BrowserTaskService : ISimplyObservable<BrowserTaskRequest>
    {
        readonly Observable<BrowserTaskRequest> observable = new Observable<BrowserTaskRequest> ();

        public IDisposable Subscribe (Action<BrowserTaskRequest> nextHandler)
            => observable.Subscribe (new Observer<BrowserTaskRequest> (nextHandler));

        public IDisposable Subscribe (IObserver<BrowserTaskRequest> observer)
            => observable.Subscribe (observer);

        //public Task<T> 
    }

    sealed class ClientSession :
        ISimplyObservable<ClientSessionEvent>,
        ISimplyObservable<UserAction>,
        IDisposable
    {
        const string TAG = nameof (ClientSession);

        sealed class QuietlyDisposeClientSessionException : Exception
        {
        }

        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();
        public CancellationToken CancellationToken => cancellationTokenSource.Token;

        readonly Observable<ClientSessionEvent> observable = new Observable<ClientSessionEvent> ();
        readonly Observable<UserAction> actionObservable = new Observable<UserAction> ();

        bool isDisposed;

        public ClientSessionUri Uri { get; }
        public ClientSessionKind SessionKind { get; }

        public BrowserTaskService BrowserTasks { get; } = new BrowserTaskService ();

        public bool CanAddPackages => SessionKind == ClientSessionKind.Workbook; // TODO: And check session ready?

        // TODO: Do we need this one?
        public bool CanEvaluate => true;
             //=> CompilationWorkspace != null &&
                //EvaluationService != null &&
                //EvaluationService.CanEvaluate;

        public ClientSession (ClientSessionUri clientSessionUri)
        {
            if (clientSessionUri == null)
                throw new ArgumentNullException (nameof (clientSessionUri));

            Uri = clientSessionUri;
            SessionKind = clientSessionUri.SessionKind;

            UpdateTitle ();
        }

        public void Dispose ()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            cancellationTokenSource.Cancel ();
            observable.Observers.OnCompleted ();
            actionObservable.Observers.OnCompleted ();
        }

        // public void InitializeViewControllers (IClientSessionViewControllers viewControllers)
        // {
        //     if (viewControllers == null)
        //         throw new ArgumentNullException (nameof (viewControllers));

        //     Action<object, string> assert = (viewController, name) => {
        //         if (viewController == null)
        //             throw new ArgumentException (
        //                 $"{nameof (viewControllers)}.{nameof (name)} must not be null",
        //                 nameof (viewControllers));
        //     };

        //     assert (viewControllers.Messages, nameof (viewControllers.Messages));
        //     assert (viewControllers.ReplHistory, nameof (viewControllers.ReplHistory));
        //     assert (viewControllers.WorkbookTargets, nameof (viewControllers.WorkbookTargets));

        //     if (ViewControllers != null)
        //         ViewControllers.WorkbookTargets.PropertyChanged -= WorkbookTargets_PropertyChanged;

        //     ViewControllers = new ViewControllersProxy (this, viewControllers);
        //     ViewControllers.WorkbookTargets.UpdateTargets (this);
        //     ViewControllers.WorkbookTargets.PropertyChanged += WorkbookTargets_PropertyChanged;
        // }

        // void WorkbookTargets_PropertyChanged (object sender, PropertyChangedEventArgs e)
        // {
        //     EvaluationService.OutdateAllCodeCells ();

        //     var selectedTarget = ViewControllers.WorkbookTargets.SelectedTarget;
        //     if (selectedTarget == null)
        //         TerminateAgentConnection ();
        //     else if (selectedTarget.AgentType != agent.Type) {
        //         agent = agent.WithAgentType (selectedTarget.AgentType);
        //         InitializeAgentConnectionAsync ().Forget ();
        //     }
        // }

        // void Workbook_PropertyChanged (object sender, PropertyChangedEventArgs e)
        // {
        //     if (e.PropertyName == nameof (WorkbookPackage.Title))
        //         UpdateTitle ();
        // }

        #region Observable

        void PostEvent (ClientSessionEventKind eventKind)
        {
            var evnt = new ClientSessionEvent (this, eventKind);
            MainThread.Post (() => observable.Observers.OnNext (evnt));
        }

        void PostEvent (IObserver<ClientSessionEvent> observer, ClientSessionEventKind eventKind)
            => MainThread.Post (() => observer.OnNext (new ClientSessionEvent (this, eventKind)));

        public IDisposable Subscribe (IObserver<ClientSessionEvent> observer)
        {
            var subscription = observable.Subscribe (observer);

            //PostEvent (observer, ClientSessionEventKind.SessionAvailable);
            //PostEvent (observer, ClientSessionEventKind.SessionTitleUpdated);

            //if (Agent.IsConnected)
            //    PostEvent (observer, ClientSessionEventKind.AgentConnected);
            //else
            //    PostEvent (observer, ClientSessionEventKind.AgentDisconnected);

            //PostEvent (observer, ClientSessionEventKind.AgentFeaturesUpdated);

            //if (CompilationWorkspace != null)
            //PostEvent (observer, ClientSessionEventKind.CompilationWorkspaceAvailable);

            return subscription;
        }

        public IDisposable Subscribe (Action<ClientSessionEvent> observer)
            => observable.Subscribe (new Observer<ClientSessionEvent> (observer));

        #endregion

        #region Action Observable

        public void PostAction (UserActionKind eventKind)
            => PostAction (new UserAction (this, eventKind));

        public void PostAction (UserAction action)
            => MainThread.Post (() => actionObservable.Observers.OnNext (action));

        void PostAction (IObserver<UserAction> observer, UserActionKind eventKind)
            => MainThread.Post (() => observer.OnNext (new UserAction (this, eventKind)));

        public IDisposable Subscribe (IObserver<UserAction> observer)
        {
            var subscription = actionObservable.Subscribe (observer);

            //PostEvent (observer, ClientSessionEventKind.SessionAvailable);
            //PostEvent (observer, ClientSessionEventKind.SessionTitleUpdated);

            //if (Agent.IsConnected)
            //    PostEvent (observer, ClientSessionEventKind.AgentConnected);
            //else
            //    PostEvent (observer, ClientSessionEventKind.AgentDisconnected);

            //PostEvent (observer, ClientSessionEventKind.AgentFeaturesUpdated);

            //if (CompilationWorkspace != null)
            //PostEvent (observer, ClientSessionEventKind.CompilationWorkspaceAvailable);

            return subscription;
        }

        public IDisposable Subscribe (Action<UserAction> observer)
            => actionObservable.Subscribe (new Observer<UserAction> (observer));

        #endregion

        // void ResetAgentConnection ()
        // {
        //     ViewControllers.Messages.ClearStatusMessages ();

        //     var agentType = AgentType.Unknown;

        //     if (agent != null) {
        //         agentType = agent.Type;
        //         ((IDisposable)agent).Dispose ();
        //     }

        //     agent = new AgentConnection (agentType);
        // }

        // public void TerminateAgentConnection ()
        //     => agent = agent.TerminateConnection ();

        // void AssertWorkbookSession ()
        // {
        //     if (SessionKind != ClientSessionKind.Workbook)
        //         throw new InvalidOperationException ("not a workbook session");
        // }

        // public async Task InitializeAsync (IWorkbookPageHost workbookPageViewHost = null)
        // {
        //     var genericLoadingMessage = SessionKind == ClientSessionKind.Workbook
        //         ? Catalog.GetString ("Loading workbook…")
        //         : Catalog.GetString ("Loading session…");

        //     var initializers = new List<ClientSessionTask> {
        //         ClientSessionTask.CreateRequired (genericLoadingMessage, LoadWorkbookAsync)
        //     };

        //     if (workbookPageViewHost != null) {
        //         initializers.AddRange (
        //             workbookPageViewHost
        //                 .GetClientSessionInitializationTasks (clientWebServerUri)
        //                 .Select (t => ClientSessionTask.CreateRequired (genericLoadingMessage, t)));

        //         Task LoadWorkbookPageViewAsync (CancellationToken cancellationToken)
        //         {
        //             var pageViewModel = workbookPageViewHost.CreatePageViewModel (this, Workbook.IndexPage);
        //             EvaluationService = pageViewModel;

        //             pageViewModel.LoadWorkbookPage ();

        //             if (pageViewModel is IObserver<ClientSessionEvent> observer)
        //                 Subscribe (observer);

        //             return Task.CompletedTask;
        //         }

        //         initializers.Add (ClientSessionTask.CreateRequired (genericLoadingMessage, LoadWorkbookPageViewAsync));
        //     }

        //     var initializeException = await RunInitializers (initializers);

        //     if (isDisposed)
        //         return;

        //     if (initializeException == null) {
        //         await InitializeAgentConnectionAsync ();

        //         try {
        //             await ConfigureNewWorkbookFeatures ();
        //         } catch (Exception e) {
        //             var message = Catalog.GetString (
        //                 "Unable to configure optional workbook features.");
        //             Log.Warning (TAG, message, e);
        //             ViewControllers.Messages.PushMessage (Message.CreateErrorStatus (
        //                 message,
        //                 e.Message));
        //         }

        //         return;
        //     }

        //     var genericErrorMessage = SessionKind == ClientSessionKind.Workbook
        //         ? Catalog.GetString ("Unable to load workbook.")
        //         : Catalog.GetString ("Unable to initialize live inspection session.");

        //     ViewControllers.Messages.PushMessage (Message.CreateErrorStatus (genericErrorMessage));

        //     ViewControllers.Messages.PushMessage (initializeException
        //         .ToAlertMessage (genericErrorMessage)
        //         .WithAction (new MessageAction (
        //             MessageActionKind.Affirmative,
        //             "close",
        //             Catalog.GetString ("Close")))
        //         .WithActionResponseHandler ((message, action) => {
        //             message.Dispose ();
        //             Dispose ();
        //         }));
        // }

        // public async Task EnsureAgentConnectionAsync ()
        // {
        //     if (!Agent.IsConnected)
        //         await InitializeAgentConnectionAsync ();
        // }

        // async Task InitializeAgentConnectionAsync ()
        // {
        //     if (EvaluationService != null) {
        //         using (EvaluationService.InhibitEvaluate ())
        //             await DoInitalizeAgentConnectionAsync ();
        //     } else {
        //         await DoInitalizeAgentConnectionAsync ();
        //     }
        // }

        // // Only call from InitializeAgentConnectionAsync
        // async Task DoInitalizeAgentConnectionAsync ()
        // {
        //     try {
        //         ResetAgentConnection ();

        //         using (ViewControllers.Messages.PushMessage (
        //             Message.CreateInfoStatus (
        //                 Catalog.GetString ("Connecting to agent…"), showSpinner: true)))
        //             await ConnectToAgentAsync (CancellationToken);

        //         using (ViewControllers.Messages.PushMessage (
        //             Message.CreateInfoStatus (
        //                 Catalog.GetString ("Preparing workspace…"), showSpinner: true))) {
        //             await InitializeCompilationWorkspaceAsync (CancellationToken);

        //             if (EvaluationService == null) {
        //                 var evaluationService = new EvaluationService (
        //                     CompilationWorkspace,
        //                     new EvaluationEnvironment (WorkingDirectory));
        //                 evaluationService.NotifyEvaluationContextManagerChanged (Agent.Api.EvaluationContextManager);
        //                 EvaluationService = evaluationService;
        //             }

        //             if (SessionKind == ClientSessionKind.Workbook)
        //                 PackageManager = new PackageManagerService (
        //                     CompilationWorkspace.Configuration.DependencyResolver,
        //                     EvaluationService,
        //                     async (refreshForAgentIntegration, cancellationToken) => {
        //                         if (refreshForAgentIntegration)
        //                             await RefreshForAgentIntegration ();
        //                         return Agent;
        //                     });
        //             else
        //                 PackageManager = null;
        //         }
        //     } catch (Exception e) {
        //         Log.Error (TAG, e);
        //         ViewControllers.Messages.PushMessage (WithReconnectSessionAction (e
        //             .ToAlertMessage (Catalog.GetString ("Unable to connect"))));

        //         return;
        //     }

        //     try {
        //         using (ViewControllers.Messages.PushMessage (
        //             Message.CreateInfoStatus (
        //                 Catalog.GetString ("Restoring packages…"), showSpinner: true)))
        //             await PackageManager?.InitializeAsync (
        //                 WorkbookApp.Sdk,
        //                 Workbook
        //                     .Pages
        //                     .SelectMany (page => page.Packages),
        //                 CancellationToken);
        //     } catch (Exception e) {
        //         Log.Error (TAG, e);
        //         ViewControllers.Messages.PushMessage (e
        //             .ToAlertMessage (Catalog.GetString ("Unable to restore packages")));
        //     }
        // }

        // async Task<Exception> RunInitializers (IEnumerable<ClientSessionTask> initializers)
        // {
        //     foreach (var initializer in initializers) {
        //         var message = Message.CreateInfoStatus (
        //             initializer.Description,
        //             showSpinner: true);

        //         message = ViewControllers.Messages.PushMessage (message);

        //         try {
        //             await initializer.Delegate (CancellationToken);
        //         } catch (QuietlyDisposeClientSessionException) {
        //             Dispose ();
        //             return null;
        //         } catch (Exception e) {
        //             Log.Error (TAG, $"InitializeAsync({initializer.Description})", e);

        //             if (initializer.IsSuccessfulCompletionRequired)
        //                 return e;

        //             if (initializer.ExceptionHandler != null) {
        //                 initializer.ExceptionHandler (e);
        //                 continue;
        //             }

        //             ViewControllers.Messages.PushMessage (Message.CreateErrorAlert (e.Message));
        //         } finally {
        //             message.Dispose ();
        //         }
        //     }

        //     return null;
        // }

        // async Task LoadWorkbookAsync (CancellationToken cancellationToken)
        // {
        //     bool cancelOpen = false;

        //     await Workbook.Open (async quarantineInfo => {
        //         var response = await ViewControllers.Messages.PushAlertMessageAsync (
        //             quarantineInfo.CreateAlert ());
        //         cancelOpen = response.Id == "cancel";
        //         return !cancelOpen;
        //     }, Uri.WorkbookPath, agent.Type);

        //     if (cancelOpen)
        //         throw new QuietlyDisposeClientSessionException ();

        //     ViewControllers.WorkbookTargets.PropertyChanged -= WorkbookTargets_PropertyChanged;
        //     ViewControllers.WorkbookTargets.UpdateTargets (this);
        //     ViewControllers.WorkbookTargets.PropertyChanged += WorkbookTargets_PropertyChanged;

        //     if (ViewControllers.WorkbookTargets.SelectedTarget != null)
        //         agent = agent.WithAgentType (ViewControllers.WorkbookTargets.SelectedTarget.AgentType);
        // }

        // async Task ConfigureNewWorkbookFeatures ()
        // {
        //     foreach (var parameter in Uri.Parameters) {
        //         switch (parameter.Key) {
        //         case "feature":
        //             if (NewWorkbookFeature.AllFeatures.TryGetValue (
        //                 parameter.Value, out var feature))
        //                 await feature.ConfigureClientSession (
        //                     this,
        //                     CancellationToken);
        //             break;
        //         }
        //     }
        // }

        // async Task ConnectToAgentAsync (CancellationToken cancellationToken)
        // {
        //     if (agent.IsConnected)
        //         agent = agent.TerminateConnection ();

        //     if (SessionKind == ClientSessionKind.Workbook)
        //         WorkbookApp = WorkbookAppInstallation.Locate (agent.Type);

        //     agent = await agent.ConnectAsync (
        //         WorkbookApp,
        //         Uri,
        //         ViewControllers.Messages,
        //         HandleAgentDisconnected,
        //         cancellationTokenSource.Token);

        //     agent.Api.EvaluationContextManager.Events.Subscribe (
        //         new Observer<ICodeCellEvent> (HandleCodeCellEvent));

        //     await agent.Api.SetLogLevelAsync (Log.GetLogLevel ());

        //     UpdateTitle ();

        //     PostEvent (ClientSessionEventKind.AgentConnected);

        //     new Telemetry.Models.AgentSession {
        //         AppSessionId = ClientApp.SharedInstance.AppSessionId,
        //         Timestamp = DateTimeOffset.UtcNow,
        //         Flavor = agent.Identity.FlavorId,
        //         Kind = (Telemetry.Models.AgentSessionKind)(int)SessionKind
        //     }.Post ();
        // }

        // void HandleCodeCellEvent (ICodeCellEvent evnt)
        // {
        //     if (evnt is Evaluation evaluation && evaluation.InitializedIntegration)
        //         RefreshForAgentIntegration ().Forget ();
        // }

        // void HandleAgentDisconnected ()
        // {
        //     var disconnectedAgent = agent;

        //     ResetAgentConnection ();
        //     EvaluationService.OutdateAllCodeCells ();

        //     PostEvent (ClientSessionEventKind.AgentDisconnected);

        //     var title = Catalog.GetString ("Session Disconnected");

        //     Message message;
        //     if (SessionKind == ClientSessionKind.Workbook)
        //         message = WithReconnectSessionAction (
        //             Message.CreateErrorAlert (title, Catalog.GetString (
        //                 "The Workbook host application has terminated.")));
        //     else
        //         message = Message.CreateInfoAlert (
        //             title,
        //             Catalog.Format (Catalog.GetString (
        //                 "Inspector is no longer connected to {0}. You may review and close " +
        //                 "the session window at your leisure. A new Inspector session may be " +
        //                 "attached via the debugger in your IDE."),
        //                 disconnectedAgent.Identity.ApplicationName));

        //     ViewControllers.Messages.PushMessage (message);
        // }

        // Message WithReconnectSessionAction (Message message) => message
        //     .WithAction (
        //         new MessageAction (
        //             MessageActionKind.Affirmative,
        //             MessageAction.RetryActionId,
        //             Catalog.GetString ("Reconnect"),
        //             Catalog.GetString ("Reconnect session")))
        //     .WithActionResponseHandler (
        //         async (m, a) => await InitializeAgentConnectionAsync ());

        // async Task InitializeCompilationWorkspaceAsync (CancellationToken cancellationToken)
        // {
        //     WorkingDirectory = Workbook.WorkingBasePath;
        //     if (!WorkingDirectory.DirectoryExists)
        //         WorkingDirectory = Uri.WorkingDirectory;
        //     if (!WorkingDirectory.DirectoryExists)
        //         WorkingDirectory = FilePath.Empty;

        //     if (agent.IsConnected) {
        //         await GacCache.InitializingTask;

        //         CompilationWorkspace = await WorkspaceServiceFactory.CreateWorkspaceServiceAsync (
        //             "csharp",
        //             await WorkspaceConfiguration.CreateAsync (
        //                 Agent.Api.EvaluationContextManager,
        //                 SessionKind,
        //                 cancellationToken),
        //             cancellationToken);
        //     }

        //     await RefreshForAgentIntegration ();

        //     if (CompilationWorkspace == null)
        //         throw new Exception ("Unable to get compilation workspace for agent.");

        //     var dependencyResolver = CompilationWorkspace.Configuration.DependencyResolver;

        //     if (WorkingDirectory.DirectoryExists) {
        //         dependencyResolver.BaseDirectory = WorkingDirectory;
        //         dependencyResolver.AddAssemblySearchPath (WorkingDirectory);
        //     }

        //     Workbook.WorkingPathChanged += (o, e) => {
        //         if (dependencyResolver != null) {
        //             dependencyResolver.RemoveAssemblySearchPath (WorkingDirectory);
        //             dependencyResolver.RemoveAssemblySearchPath (e.OldBasePath);

        //             WorkingDirectory = e.NewBasePath;
        //             dependencyResolver.BaseDirectory = WorkingDirectory;
        //             dependencyResolver.AddAssemblySearchPath (WorkingDirectory);
        //         }
        //     };

        //     PostEvent (ClientSessionEventKind.CompilationWorkspaceAvailable);
        // }

        public string Title { get; private set; }
        public string SecondaryTitle { get; private set; }

        void UpdateTitle ()
        {
            SecondaryTitle = null;

                //if (SessionKind == ClientSessionKind.Workbook) {
                //    Title = Workbook.Title;
                //    if (WorkbookApp != null)
                //        SecondaryTitle = WorkbookAppViewController.GetDisplayLabel (
                //            WorkbookApp,
                //            WorkbookAppViewController.Context.StatusBar);
                //} else {
                //    Title = agent.Identity?.ApplicationName ?? Catalog.GetString ("Live Inspect Session");

                //    var sdk = agent.Identity?.Sdk;
                //    SecondaryTitle = sdk?.Name;
                //    if (SecondaryTitle != null && sdk.Profile != null)
                //        SecondaryTitle = Catalog.Format (Catalog.GetString (
                //            "{0} ({1} Profile)",
                //            comment: "{0} is a runtime/product name like 'Xamarin.Mac'; " +
                //                "{1} is a profile name like 'Modern'"),
                //            sdk.Name,
                //            sdk.Profile);
                //}

            PostEvent (ClientSessionEventKind.SessionTitleUpdated);
        }

        // public IWorkbookSaveOperation CreateWorkbookSaveOperation ()
        // {
        //     AssertWorkbookSession ();
        //     return Workbook.CreateSaveOperation (CompilationWorkspace);
        // }

        // public void SaveWorkbook (IWorkbookSaveOperation saveOperation)
        // {
        //     AssertWorkbookSession ();
        //     Workbook.Save (saveOperation);
        //     UpdateTitle ();
        // }

        // async Task RefreshForAgentIntegration ()
        // {
        //     agent = await agent.RefreshFeaturesAsync ();
        //     PostEvent (ClientSessionEventKind.AgentFeaturesUpdated);
        // }

        #region Web Resources

        // ImmutableBidirectionalDictionary<Guid, FilePath> webResources
        //     = ImmutableBidirectionalDictionary<Guid, FilePath>.Empty;

        // public bool AddWebResource (FilePath path, out Guid guid)
        // {
        //     if (webResources.TryGetFirst (path, out guid))
        //         return false;

        //     webResources = webResources.Add (guid = Guid.NewGuid (), path);

        //     Log.Debug (TAG, $"{path} as {guid}");

        //     return true;
        // }

        // public bool TryGetWebResource (Guid guid, out FilePath path)
        //     => webResources.TryGetSecond (guid, out path);

        #endregion
    }
}