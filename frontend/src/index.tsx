import App from './App'
import ReactDom from 'react-dom'
import React from 'react'
import { PublicClientApplication } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import { msalConfig } from 'authConfig'

// ========================================

const msalInstance = new PublicClientApplication(msalConfig)

ReactDom.render(
    <React.StrictMode>
        <MsalProvider instance={msalInstance}>
            <App />
        </MsalProvider>
    </React.StrictMode>,
    document.getElementById('root')
)
