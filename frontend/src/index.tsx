import App from './App'
import ReactDom from 'react-dom'
import React from 'react'
import { PublicClientApplication } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import { msalConfig } from 'api/AuthConfig'

// ========================================

const msalInstance = new PublicClientApplication(msalConfig)

// Error handling for redirect login
msalInstance.handleRedirectPromise().catch((error) => {
    console.error(error)
})

ReactDom.render(
    <React.StrictMode>
        <MsalProvider instance={msalInstance}>
            <App />
        </MsalProvider>
    </React.StrictMode>,
    document.getElementById('root')
)
