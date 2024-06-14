import App from './App'
import React from 'react'
import { PublicClientApplication } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import { msalConfig } from 'api/AuthConfig'
import ReactDOM from 'react-dom/client'

// ========================================

const msalInstance = new PublicClientApplication(msalConfig)

// Error handling for redirect login
await msalInstance.initialize()
msalInstance.handleRedirectPromise().catch((error) => {
    console.error(error)
})

const rootElement = document.getElementById('root')
if (!rootElement) throw new Error('Failed to find the root element')
const root = ReactDOM.createRoot(rootElement)

root.render(
    <React.StrictMode>
        <MsalProvider instance={msalInstance}>
        <div style={{backgroundColor: '#f7f7f7'}}>
            <App />
        </div>
        </MsalProvider>
    </React.StrictMode>
)
