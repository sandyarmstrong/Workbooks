//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Interactive.CodeAnalysis
{
    interface IWorkspaceService
    {
        EvaluationContextId EvaluationContextId { get; }

        ImmutableList<CodeCellId> GetTopologicallySortedCellIds ();

        CodeCellId InsertCell (
            CodeCellBuffer buffer,
            CodeCellId previousCellId,
            CodeCellId nextCellId);

        void RemoveCell (CodeCellId cellId, CodeCellId nextCellId);

        bool IsCellComplete (CodeCellId cellId);

        bool ShouldInvalidateCellBuffer (CodeCellId cellId);

        Task<ImmutableList<InteractiveDiagnostic>> GetCellDiagnosticsAsync (
            CodeCellId cellId,
            CancellationToken cancellationToken = default);

        // FIXME: extend Compilation with Diagnostics, but this will
        // require moving more into XI that currently lives in XIC
        // -abock, 2018-03-08
        Task<(Compilation compilation, ImmutableList<InteractiveDiagnostic> diagnostics)> GetCellCompilationAsync (
            CodeCellId cellId,
            IEvaluationEnvironment evaluationEnvironment,
            CancellationToken cancellationToken = default);
    }
}