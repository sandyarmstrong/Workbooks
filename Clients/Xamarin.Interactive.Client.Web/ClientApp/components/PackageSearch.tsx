import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { WorkbookSession } from '../WorkbookSession'
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner'
import { PrimaryButton, DefaultButton } from 'office-ui-fabric-react/lib/Button'
import { Dialog, DialogType, DialogFooter } from 'office-ui-fabric-react/lib/Dialog'
import { SearchBox } from 'office-ui-fabric-react/lib/SearchBox'

interface PackageSearchProps {
    session: WorkbookSession
}

interface PackageSearchState {
    query: string
    results: PackageViewModel[]
    selectedPackage?: PackageViewModel
    inProgress: boolean
}

interface PackageViewModel {
    id: string
    version: string
}

export class PackageSearch extends React.Component<PackageSearchProps, PackageSearchState> {
    constructor(props: PackageSearchProps) {
        super(props)
        this.state = {
            query: "",
            results: [],
            inProgress: false
        }
    }

    public render() {
        return <div>
            <Dialog
                hidden={false}
                dialogContentProps={{
                    type: DialogType.largeHeader,
                    title: "Add NuGet Packages",
                    subText: "some explanatory text"
                }}
                modalProps={{
                    isBlocking: true
                }}>

                <SearchBox
                    labelText="Search NuGet"
                    onChange={event => this.onSearchFieldChanged(event)} />

                <select
                    className="form-control"
                    size={this.state.query.length > 0 ? 10 : 0}
                    onChange={event => this.onSelectedPackageChanged(event)}>
                {
                    this.state.results.map(p => <option key={p.id} value={p.id}>{p.id}</option>)
                }
                </select>

                <DialogFooter>
                    <PrimaryButton
                        disabled={this.state.inProgress}
                        text="Install"
                        onClick={() => this.installSelectedPackage()}/>
                    {
                        this.state.inProgress ? <Spinner label="Installing..." /> : null
                    }
                </DialogFooter>
            </Dialog>
        </div>
    }

    async onSearchFieldChanged(input: string) {
        // TODO: Cancellation or at least ignoring of results we no longer care about (as we type)
        let query = input.trim()

        let results = []
        if (query) {
            // TODO: Add supportedFramework to query? Seems like a good idea but it doesn't seem to change results
            let result = await fetch("https://api-v2v3search-0.nuget.org/query?prerelease=false&q=" + query)
            var json = await result.json()
            results = json.data
        }
        this.setState({
            query: query,
            results: results
        })
    }

    onSelectedPackageChanged(event: React.ChangeEvent<HTMLSelectElement>) {
        let packageId = event.target.value as string
        let selectedPackage = this.state.results.filter(p => p.id === packageId)[0]
        this.setState({ selectedPackage: selectedPackage })
    }

    async installSelectedPackage() {
        if (!this.state.selectedPackage || this.state.inProgress)
            return

        this.setState({ inProgress: true })

        console.log(this.state.selectedPackage)
        await this.props.session.installPackage(
            this.state.selectedPackage.id,
            this.state.selectedPackage.version)

        this.setState({ inProgress: false })
    }
}
