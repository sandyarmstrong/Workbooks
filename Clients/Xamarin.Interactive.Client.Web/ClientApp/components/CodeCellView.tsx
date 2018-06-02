//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { ActionButton } from 'office-ui-fabric-react/lib/Button';
import { ProgressIndicator } from 'office-ui-fabric-react/lib/ProgressIndicator';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import {
    Dropdown,
    IDropdown,
    DropdownMenuItemType,
    IDropdownOption
} from 'office-ui-fabric-react/lib/Dropdown';

import { randomReactKey } from '../utils'
import { EditorMessage, EditorMessageType } from '../utils/EditorMessages'
import { CodeCellResult, CodeCellResultHandling, Diagnostic, CapturedOutputSegment } from '../evaluation'
import { ResultRendererRepresentation } from '../rendering'
import { ResultRendererRegistry } from '../ResultRendererRegistry'
import { CapturedOutputView } from './CapturedOutputView'

import './CodeCellView.scss'

export interface ResultRendererRepresentationMap {
    [key: string]: ResultRendererRepresentation
}

export interface CodeCellResultRendererState {
    result: CodeCellResult
    representations: ResultRendererRepresentationMap
    selectedRepresentationKey: string | null
}

export const enum CodeCellViewStatus {
    Unbound,
    Ready,
    Evaluating,
    Aborting
}

export interface CodeCellViewProps {
    rendererRegistry: ResultRendererRegistry
}

export interface CodeCellViewState {
    status: CodeCellViewStatus
    capturedOutput: CapturedOutputSegment[]
    results: CodeCellResultRendererState[]
    diagnostics: Diagnostic[]
}

export abstract class CodeCellView<
    TCodeCellViewProps extends CodeCellViewProps = CodeCellViewProps,
    TCodeCellViewState extends CodeCellViewState = CodeCellViewState>
    extends React.Component<TCodeCellViewProps, TCodeCellViewState> {

    protected abstract getRendererRegistry(): ResultRendererRegistry
    protected abstract abortEvaluation(): Promise<void>
    protected abstract startEvaluation(): Promise<void>
    protected abstract renderEditor(): any

    protected setStateFromResult(result: CodeCellResult, resultHandling?: CodeCellResultHandling) {
        const reps = this
            .getRendererRegistry()
            .getRenderers(result)
            .map(r => r.getRepresentations(result))

        const flatReps = reps.length === 0
            ? []
            : reps.reduce((a, b) => a.concat(b))

        const mapReps: ResultRendererRepresentationMap = {}
        flatReps.map(rep => mapReps[rep.key] = rep)

        const rendererState = {
            result: result,
            representations: mapReps,
            // TODO: Investigate why we get 2 results per submission, and first always lacks reps
            selectedRepresentationKey: flatReps[0] ? flatReps[0].key : null
        }

        if (!resultHandling)
            resultHandling = result.resultHandling

        switch (resultHandling) {
            case CodeCellResultHandling.Append:
                this.setState({
                    results: this.state.results.concat(rendererState)
                })
                break
            case CodeCellResultHandling.Replace:
            default:
                this.setState({
                    results: [rendererState]
                })
                break
        }
    }

    private renderActions() {
        switch (this.state.status) {
            case CodeCellViewStatus.Unbound:
                return null
            case CodeCellViewStatus.Evaluating:
                return (
                    <div className='actions'>
                        <ActionButton
                            className='CancelButton'
                            iconProps={{ iconName: 'Cancel' }}
                            onClick={e => this.abortEvaluation()}>
                            Cancel
                        </ActionButton>
                        <ProgressIndicator />
                     </div>
                )
            case CodeCellViewStatus.Aborting:
                return <div>Aborting...</div>
            case CodeCellViewStatus.Ready:
                return (
                    <ActionButton
                        iconProps={{ iconName: 'Play' }}
                        onClick={e => this.startEvaluation()}>
                        Run
                    </ActionButton>
                )
        }
        return <div/>
    }

    render() {
        return (
            <div className="CodeCell-container">
                <div className="CodeCell-editor-container">
                    {this.renderEditor()}
                </div>
                <div className="CodeCell-diagnostics-container">
                    <ul>
                        {this.state.diagnostics.map((diag, i) => {
                            return (
                                <li
                                    key={randomReactKey()}
                                    className={"CodeCell-diagnostic-" + diag.severity}
                                    onClick={(e) => {
                                        e.stopPropagation()
                                        this.sendEditorMessage({
                                            target: this.getBlockKey(),
                                            type: EditorMessageType.setCursor,
                                            data: {
                                                lineNumber: diag.range.startLineNumber,
                                                column: diag.range.startColumn
                                            }
                                        })
                                    }}>
                                    ({diag.range.startLineNumber},{diag.range.startColumn}):&nbsp;
                                        {diag.severity} {diag.id}: {diag.message}
                                </li>
                            )
                        })}
                    </ul>
                </div>
                {this.state.capturedOutput.length > 0 &&
                    <div className="CodeCell-captured-output-container">
                        <CapturedOutputView segments={this.state.capturedOutput} />
                    </div>}
                <div className="CodeCell-results-container">
                    {this.state.results.map((resultState, i) => {
                        const representationKeys = Object.keys(resultState.representations)
                        if (representationKeys.length === 0)
                            return
                        const dropdownOptions = representationKeys.length > 1
                            ? representationKeys.map(key => {
                                return {
                                    key: key,
                                    text: resultState.representations[key].displayName
                                }
                            })
                            : null

                        let repElem = null
                        if (resultState.selectedRepresentationKey) {
                            const rep = resultState.representations[resultState.selectedRepresentationKey]
                            repElem = <rep.component
                                key={rep.key}
                                {...rep.componentProps}/>
                        }

                        return (
                            <div
                                key={i}
                                className="CodeCell-result">
                                <div className="CodeCell-result-renderer-container">
                                    {repElem}
                                </div>
                                {dropdownOptions && <Dropdown
                                    options={dropdownOptions}
                                    defaultSelectedKey={dropdownOptions[0].key}
                                    onChanged={item => {
                                        resultState.selectedRepresentationKey = item.key as string
                                        this.setState(this.state)
                                    }}/>}
                            </div>
                        )
                    })}
                </div>
                <div className="CodeCell-actions-container">
                    {this.renderActions()}
                </div>
            </div>
        );
    }

    protected sendEditorMessage(message: EditorMessage) {
        // no-op here
    }

    protected getBlockKey(): string {
        // should be overridden
        return ""
    }
}

export interface MockedCodeCellProps extends CodeCellViewProps {
    results: CodeCellResult[]
    resultHandling?: CodeCellResultHandling
}

export class MockedCodeCellView extends CodeCellView<MockedCodeCellProps> {
    constructor(props: MockedCodeCellProps) {
        super(props)

        this.state = {
            status: CodeCellViewStatus.Ready,
            capturedOutput: [],
            results: [],
            diagnostics: []
        }
    }

    setStateFromPendingResult() {
        const result = this.props.results.shift()
        if (result)
            this.setStateFromResult(result, this.props.resultHandling)
    }

    componentDidMount() {
        this.setStateFromPendingResult()
    }

    componentDidUpdate() {
        this.setStateFromPendingResult()
    }

    protected getRendererRegistry(): ResultRendererRegistry {
        return this.props.rendererRegistry
    }

    protected async startEvaluation(): Promise<void> {
        this.setState({status: CodeCellViewStatus.Evaluating})
    }

    protected async abortEvaluation(): Promise<void> {
        this.setState({status: CodeCellViewStatus.Ready})
    }

    protected renderEditor() {
        return (
            <div style={{
                color: '#999',
                backgroundColor: '#FAFAFA',
                padding: '5px',
                fontFamily: 'monospace'
            }}>
                <div>// hello</div>
                <div>var x = 2 + 2</div>
            </div>
        )
    }
}