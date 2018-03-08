//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.CodeAnalysis
{
    public struct InteractiveDiagnostic
    {
        public PositionSpan Span { get; }
        public DiagnosticSeverity Severity { get; }
        public string Message { get; }
        public string Id { get; }

        public InteractiveDiagnostic (DiagnosticSeverity severity, string message)
        {
            Span = default;
            Severity = severity;
            Message = message;
            Id = null;
        }

        public InteractiveDiagnostic (
            PositionSpan span,
            DiagnosticSeverity severity,
            string message,
            string id)
        {
            Span = span;
            Severity = severity;
            Message = message;
            Id = id;
        }
    }
}