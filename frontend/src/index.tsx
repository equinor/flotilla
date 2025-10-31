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

msalInstance
    .handleRedirectPromise()
    .then((tokenResponse) => {
        if (!tokenResponse) {
            const accounts = msalInstance.getAllAccounts()
            if (accounts.length === 0) {
                msalInstance.loginRedirect().catch((error) => {
                    if (error.errorCode === 'interaction_in_progress') {
                        console.log('Authentication already in progress')
                    } else {
                        console.error('Login failed:', error)
                    }
                })
            }
        } else {
            console.log('User authenticated successfully')
        }
    })
    .catch((err) => {
        if (err.errorCode === 'interaction_in_progress') {
            console.log('Authentication flow already in progress')
        } else {
            console.error('Authentication error:', err)
        }
    })

const rootElement = document.getElementById('root')
if (!rootElement) throw new Error('Failed to find the root element')
const root = ReactDOM.createRoot(rootElement)

root.render(
    <React.StrictMode>
        <MsalProvider instance={msalInstance}>
            <App />
        </MsalProvider>
    </React.StrictMode>
)
