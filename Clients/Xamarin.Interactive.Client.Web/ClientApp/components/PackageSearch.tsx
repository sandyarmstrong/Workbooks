import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { WorkbookSession } from '../WorkbookSession'

interface PackageSearchProps {
    session: WorkbookSession
}

interface PackageSearchState {
    query: string
    results: PackageViewModel[]
    selectedPackage?: PackageViewModel
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
            results: []
        }
    }

    public render() {
        return <div>
            <h2>Add Packages</h2>

            <input
                className="form-control"
                type="text"
                placeholder="Search NuGet"
                onChange={event => this.onSearchFieldChanged(event)} />

            <select
                className="form-control"
                size={10}
                onChange={event => this.onSelectedPackageChanged(event)}>
            {
                this.state.results.map(p => <option key={p.id} value={p.id}>{p.id}</option>)
            }
            </select>

            <button
                className="btn-primary btn-small"
                onClick={() => this.installSelectedPackage()}>Install</button>
        </div>
    }

    async onSearchFieldChanged(event: React.ChangeEvent<HTMLInputElement>) {
        // TODO: Cancellation or at least ignoring of results we no longer care about (as we type)
        let query = event.target.value.trim()

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

    installSelectedPackage() {
        if (!this.state.selectedPackage)
            return
        console.log(this.state.selectedPackage)
        this.props.session.installPackage(
            this.state.selectedPackage.id,
            this.state.selectedPackage.version)
    }
}
