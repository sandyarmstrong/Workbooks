//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.CodeAnalysis
{
    /// <summary>
    /// Represents a 1-based span in a buffer with an optional file path and
    /// is blittably compatible with Monaco's <code>monaco.IRange</code>.
    /// </summary>
    public struct PositionSpan
    {
        public int StartLineNumber { get; }
        public int StartColumn { get; }
        public int EndLineNumber { get; }
        public int EndColumn { get; }
        public string FilePath { get; }

        public PositionSpan (
            int startLineNumber,
            int startColumn,
            int endLineNumber,
            int endColumn,
            string filePath = null)
        {
            StartLineNumber = startLineNumber;
            StartColumn = startColumn;
            EndLineNumber = endLineNumber;
            EndColumn = endColumn;
            FilePath = filePath;
        }

        public static PositionSpan FromRoslyn (Location location)
        {
            var span = location.GetMappedLineSpan ();
            if (!span.IsValid)
                span = location.GetLineSpan ();

            return new PositionSpan (
                span.StartLinePosition.Line + 1,
                span.StartLinePosition.Character + 1,
                span.EndLinePosition.Line + 1,
                span.EndLinePosition.Character + 1,
                span.Path);
        }
    }
}