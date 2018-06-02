//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using Xamarin.Interactive.Events;

namespace Xamarin.Interactive.Client
{
    enum UserActionKind
    {
        AddPackages,
        RunAll,
        LoadWorkbook,
        SaveWorkbook
    }

    class UserAction : IEvent
    {
        [JsonIgnore]
        public ClientSession Source { get; }

        public UserActionKind Kind { get; }

        [JsonIgnore]
        public DateTime Timestamp { get; }

        object IEvent.Source => Source;

        public UserAction (ClientSession source, UserActionKind kind)
        {
            Timestamp = DateTime.UtcNow;
            Source = source;
            Kind = kind;
        }
    }

    class LoadWorkbookData
    {
        public string FileName { get; }

        public string Contents { get; }

        public LoadWorkbookData (string fileName, string workbookContents)
        {
            FileName = fileName;
            Contents = workbookContents;
        }
    }

    //sealed class SaveWorkbookAction : UserAction
    //{
    //    public 
    //}

    sealed class LoadWorkbookAction : UserAction
    {
        public LoadWorkbookData Data { get; }

        public LoadWorkbookAction (ClientSession source, string fileName, string workbookContents)
            : base (source, UserActionKind.LoadWorkbook)
        {
            Data = new LoadWorkbookData (fileName, workbookContents);
        }
    }


    sealed class ClientSessionEvent : IEvent
    {
        public ClientSession Source { get; }
        public ClientSessionEventKind Kind { get; }
        public DateTime Timestamp { get; }

        object IEvent.Source => Source;

        public ClientSessionEvent (ClientSession source, ClientSessionEventKind kind)
        {
            Timestamp = DateTime.UtcNow;
            Source = source;
            Kind = kind;
        }
    }
}