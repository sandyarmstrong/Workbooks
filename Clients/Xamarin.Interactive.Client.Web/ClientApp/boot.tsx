//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import 'es6-shim';
import './boot.scss';
import 'draft-js/dist/Draft.css';

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { AppContainer } from 'react-hot-loader';
import { BrowserRouter } from 'react-router-dom';
import * as RoutesModule from './routes';
import { initializeMonaco } from './utils/MonacoLoader';
import { xiexports } from "./xiexports"

let routes = RoutesModule.routes;

function renderApp() {
    // This code starts up the React app when it runs in a browser. It sets up the routing
    // configuration and injects the app into a DOM element.
    const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href')!;
    ReactDOM.render(
        <AppContainer>
            <BrowserRouter children={ routes } basename={ baseUrl } />
        </AppContainer>,
        document.getElementById('react-app')
    );
}

initializeMonaco((monacoInitState) => {
    console.log("initializeMonaco returned %O: calling renderApp", monacoInitState)

    renderApp();

    // Allow Hot Module Replacement
    if (module.hot) {
        module.hot.accept('./routes', () => {
            routes = require<typeof RoutesModule>('./routes').routes;
            renderApp();
        });
    }
});

xiexports.holla = (message: string) => (window as any).webkit.messageHandlers.workbooks.postMessage("holla back! " + message);